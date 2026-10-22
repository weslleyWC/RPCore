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
            systems.Add(ach);
            systems.Add(needs);
            systems.Add(inv);
            systems.Add(new ShopSystem(pool, inv, ach));
            systems.Add(new PizzaJob(ach));
            systems.Add(new SeatbeltSystem(ach));
            systems.Add(new StorageSystem(pool, inv, ach));
            AddOptional(() => new StockMarketSystem(pool));
            if (Cfg.EnableGangs) AddOptional(() => new GangSystem(pool, ach));
            if (Cfg.EnableRadio) AddOptional(() => new RadioSystem(pool, ach));
            core = new CoreMenu(pool, ach, SaveAll); systems.Add(core);
            ach.Bump("started");

            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += (s, e) => heldKeys.Remove(e.KeyCode);
            Aborted += OnAborted;
            nextSave = Game.GameTime + Cfg.AutosaveMs;
            Log.Write("RPCore up, " + systems.Count + " systems, profile " + Profile.Id);
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
            if (e.KeyCode == Cfg.DumpKey)
            {
                var p = Game.Player.Character;
                string s = string.Format(CultureInfo.InvariantCulture, "{0:F2}, {1:F2}, {2:F2}  h={3:F1}",
                    p.Position.X, p.Position.Y, p.Position.Z, p.Heading);
                Log.Write("POS " + s);
                Notify.Show("pos " + s);
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
