using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using GTA;
using GTA.UI;
using LemonUI;

namespace RPCore
{
    public interface ISystem
    {
        string Name { get; }
        void Tick();
        void OnKey(KeyEventArgs e);
        void Reload();   // called after the active character profile changes
        void Save();
        void Dispose();
    }

    public class Main : Script
    {
        readonly ObjectPool pool = new ObjectPool();
        readonly List<ISystem> systems = new List<ISystem>();
        readonly HashSet<ISystem> faulted = new HashSet<ISystem>();
        int nextSave;
        readonly HashSet<Keys> heldKeys = new HashSet<Keys>();
        readonly CoreMenu core;
        readonly PhoneSystem phone;
        readonly RadialSystem radial;
        readonly SettingsSystem settings;
        readonly InventorySystem inventory;

        public Main()
        {
            Cfg.Load();
            Profile.Id = Profile.Detect();
            try { LegacyImport.MigrateFlat(Cfg.SaveDir, Profile.SafeId(Profile.Id)); }
            catch (Exception ex) { Log.Error("flat save migration (originals retained)", ex); }
            Wallet.Reload();

            var ach = new AchievementSystem(pool);
            var needs = new NeedsSystem();
            var inv = new InventorySystem(pool, needs, ach);
            inventory = inv;
            systems.Add(ach);
            systems.Add(needs);
            systems.Add(inv);
            systems.Add(new ShopSystem(pool, inv, ach));
            var shifts = new ShiftGate();
            var pizza = new PizzaJob(pool, ach, shifts); systems.Add(pizza);
            var postOp = new PostOpJob(pool, ach, shifts); systems.Add(postOp);
            var uber = new UberJob(pool, ach, shifts); systems.Add(uber);
            var valet = new ValetJob(pool, ach, shifts); systems.Add(valet);
            var supply = new StoreDeliveryJob(pool, ach, shifts); systems.Add(supply);
            var tanker = new FuelDeliveryJob(pool, ach, shifts); systems.Add(tanker);
            var prison = new PrisonTransportJob(pool, ach, shifts); systems.Add(prison);
            var pilot = new PilotJob(pool, ach, shifts); systems.Add(pilot);
            var hunting = new HuntingJob(pool, ach, shifts); systems.Add(hunting);
            var fishing = new FishingJob(pool, ach, shifts); systems.Add(fishing);
            var crafting = new CraftingSystem(pool, ach, shifts); systems.Add(crafting);
            var dealer = new DealerJob(pool, ach, shifts); systems.Add(dealer);
            var streamer = new StreamerJob(pool, ach, needs, shifts); systems.Add(streamer);
            var drag = new DragJob(pool, ach, shifts); systems.Add(drag);
            var pdm = new PdmCompany(pool, ach, shifts); systems.Add(pdm);
            var jobs = new DeliveryJobsMenu(pool, pizza, postOp, uber, valet, supply, tanker, prison, pilot, hunting, fishing, crafting, dealer, streamer, drag, pdm); systems.Add(jobs);
            var belt = new SeatbeltSystem(ach); systems.Add(belt);
            systems.Add(new StorageSystem(pool, inv, ach));
            AddOptional(() => new StockMarketSystem(pool));
            FuelSystem fuel = null;
            if (Cfg.EnableFuel) AddOptional(() => { fuel = new FuelSystem(pool, ach); return fuel; });
            VehicleControlSystem controls = null;
            if (Cfg.EnableVehicleControls) AddOptional(() => { controls = new VehicleControlSystem(pool, v => fuel == null || faulted.Contains(fuel) || fuel.CanStart(v)); return controls; });
            if (Cfg.EnableGarages) AddOptional(() => new GarageSystem(pool, ach));
            if (Cfg.EnableGarages) AddOptional(() => new DealershipSystem(pool, ach));
            GangSystem gangs = null;
            if (Cfg.EnableGangs) AddOptional(() => { gangs = new GangSystem(pool, ach); return gangs; });
            if (Cfg.EnableRadio) AddOptional(() => new RadioSystem(pool, ach));
            var visual = new VisualSystem(belt, () => controls != null && !faulted.Contains(controls) && controls.IsCruising, v => fuel == null || faulted.Contains(fuel) ? null : fuel.ViewTank(v));
            var props = new PropPresentation(inv); systems.Add(props);
            phone = new PhoneSystem(pool, DispatchSystemKey, props, inv, needs, jobs.Open, uber, streamer); systems.Add(phone);
            core = new CoreMenu(pool, ach, SaveAll, visual.Diagnose, jobs.Open); systems.Add(core);
            settings = new SettingsSystem(pool, DispatchSystemKey, needs, gangs, jobs.Open, postOp.StartPosition, valet.StartPosition, supply.StartPosition, tanker.StartPosition, prison.StartPosition, pilot.StartPosition, hunting.StartPosition, hunting.ForestPosition, fishing.StartPosition, crafting.StartPosition, dealer.StartPosition, drag.StartPosition, pdm.StartPosition); systems.Add(settings);
            radial = new RadialSystem(pool, slot => {
                switch (slot)
                {
                    case 0: DispatchSystemKey(Visuals.PhoneKey); break;
                    case 1: DispatchSystemKey(Cfg.InventoryKey); break;
                    case 2: settings.OpenEmotes(); break;
                    case 3: if (Cfg.EnableVehicleControls) DispatchSystemKey(Cfg.VehicleMenuKey); else Notify.Show("Controles do veiculo desativados."); break;
                    case 4: settings.OpenGps(); break;
                    case 5: if (Cfg.EnableGangs) DispatchSystemKey(Cfg.GangsKey); else Notify.Show("Gangues desativadas."); break;
                    case 6: settings.OpenInteractions(); break;
                    case 7: DispatchSystemKey(InterfaceOptions.SettingsKey); break;
                }
            }); systems.Add(radial);
            systems.Add(visual);
            ach.Bump("started");

            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += (s, e) => heldKeys.Remove(e.KeyCode);
            Aborted += OnAborted;
            nextSave = Game.GameTime + Cfg.AutosaveMs;
            Log.Write("RPCore 0.19.0 up, " + systems.Count + " systems, profile " + Profile.Id);
        }

        void AddOptional(Func<ISystem> create)
        {
            try { systems.Add(create()); }
            catch (Exception ex) { Log.Error("system initialization", ex); Notify.Show("A system could not start. See RPCore.log."); }
        }

        void OnTick(object sender, EventArgs e)
        {
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;
            if (core.RequestedProfile != null)
            {
                Cfg.CustomProfile = Profile.SafeId(core.RequestedProfile); core.RequestedProfile = null;
                try { new JsonFiles().Write(System.IO.Path.Combine(Cfg.Dir, "ActiveProfile.json"), Cfg.CustomProfile); }
                catch (Exception ex) { Log.Error("profile selection save", ex); }
            }
            string pid = Profile.Detect();
            if (pid != Profile.Id)
            {
                SaveAll();            // writes to the old profile
                Profile.Id = pid;
                Wallet.Reload();
                foreach (ISystem s in systems)
                {
                    try { s.Reload(); }
                    catch (Exception ex) { faulted.Add(s); Log.Error(s.Name + ".reload", ex); }
                }
                Notify.Show("Profile: " + pid);
                Log.Write("profile -> " + pid);
            }

            // World Context is polled by shops before the late radial/phone draw ticks.
            // Publish modal state first so E and controller input cannot open a shop underneath a custom view.
            InterfaceOptions.ModalOpen = inventory.GridVisible || phone.Visible || radial.Visible;
            if (InterfaceOptions.ModalOpen && pool.AreAnyVisible) pool.HideAll();
            try { pool.Process(); }
            catch (Exception ex) { Log.Error("menu dispatch", ex); pool.HideAll(); }

            foreach (ISystem s in systems)
            {
                if (faulted.Contains(s)) continue;
                try { s.Tick(); }
                catch (Exception ex)
                {
                    faulted.Add(s); // one bad system doesn't take the script down
                    try { s.Dispose(); } catch (Exception cleanup) { Log.Error(s.Name + ".cleanup", cleanup); }
                    Log.Error(s.Name, ex);
                    Notify.Show("~r~" + s.Name + " crashed, see scripts\\RPCore\\RPCore.log");
                }
            }

            if (unchecked(Game.GameTime - nextSave) >= 0)
            {
                SaveAll();
                nextSave = Game.GameTime + Cfg.AutosaveMs;
            }
        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!heldKeys.Add(e.KeyCode) || Game.IsPaused) return;
            var character = Game.Player.Character;
            if (character == null || !character.Exists() || character.IsDead) return;
            if (inventory.GridVisible && !faulted.Contains(inventory))
            {
                try { inventory.OnKey(e); }
                catch (Exception ex) { faulted.Add(inventory); Log.Error("inventory.grid.key", ex); inventory.Dispose(); }
                return;
            }
            if (!phone.Visible && !faulted.Contains(radial) && (radial.Visible || e.KeyCode == InterfaceOptions.RadialKey))
            {
                try { radial.OnKey(e); }
                catch (Exception ex) { faulted.Add(radial); Log.Error("radial.key", ex); radial.Dispose(); }
                return;
            }
            if (phone.Visible || (Visuals.Enabled && Visuals.PhoneEnabled && e.KeyCode == Visuals.PhoneKey))
            {
                if (!faulted.Contains(phone))
                {
                    try { phone.OnKey(e); }
                    catch (Exception ex) { faulted.Add(phone); Log.Error("phone.key", ex); try { phone.Dispose(); } catch { } }
                }
                return;
            }
            if (e.KeyCode == Cfg.DumpKey)
            {
                var p = Game.Player.Character;
                string s = string.Format(CultureInfo.InvariantCulture, "{0:F2}, {1:F2}, {2:F2}  h={3:F1}",
                    p.Position.X, p.Position.Y, p.Position.Z, p.Heading);
                Log.Write("POS " + s);
                Notify.Show("pos " + s);
                return;
            }

            DispatchSystemKey(e.KeyCode);
        }

        void DispatchSystemKey(Keys key)
        {
            var e = new KeyEventArgs(key);
            if (key == InterfaceOptions.SettingsKey)
            {
                if (!faulted.Contains(settings))
                    try { settings.OnKey(e); } catch (Exception ex) { faulted.Add(settings); Log.Error("settings.key", ex); settings.Dispose(); }
                return;
            }
            foreach (ISystem s in systems)
            {
                if (faulted.Contains(s)) continue;
                try { s.OnKey(e); }
                catch (Exception ex) { faulted.Add(s); Log.Error(s.Name + ".key", ex); try { s.Dispose(); } catch { } }
            }
        }

        void SaveAll()
        {
            foreach (ISystem s in systems)
            {
                if (faulted.Contains(s)) continue;
                try { s.Save(); }
                catch (Exception ex) { Log.Error(s.Name + ".save", ex); }
            }
            try { Wallet.Save(); } catch (Exception ex) { Log.Error("wallet.save", ex); }
        }

        void OnAborted(object sender, EventArgs e)
        {
            InterfaceOptions.ModalOpen = false;
            SaveAll();
            foreach (ISystem s in systems)
            {
                try { s.Dispose(); }
                catch (Exception ex) { Log.Error(s.Name + ".dispose", ex); }
            }
            Sfx.Dispose();
        }
    }
}
