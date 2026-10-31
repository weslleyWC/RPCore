using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        int nextSave,lastFrameAt,nextContextAt;
        string lastContext;bool wasNearPdm;readonly GTA.Math.Vector3 pdmOffice;
        RuntimeTrace trace;readonly Dictionary<ISystem,int> traceIds=new Dictionary<ISystem,int>();
        readonly HashSet<Keys> heldKeys = new HashSet<Keys>();
        readonly CoreMenu core;
        readonly PhoneSystem phone;
        readonly RadialSystem radial;
        readonly SettingsSystem settings;
        readonly NeedsSystem survival;
        readonly PdmSurveillanceSystem surveillance;
        readonly InventorySystem inventory;
        readonly ShopSystem shop;
        readonly MapEditorSystem editor;

        public Main()
        {
            Cfg.Load();
            Profile.Id = Profile.Detect();
            try { LegacyImport.MigrateFlat(Cfg.SaveDir, Profile.SafeId(Profile.Id)); }
            catch (Exception ex) { Log.Error("flat save migration (originals retained)", ex); }
            Wallet.Reload();

            var ach = new AchievementSystem(pool);
            var shifts = new ShiftGate();
            var needs = new NeedsSystem(shifts,()=>pool.AreAnyVisible);survival=needs;
            var inv = new InventorySystem(pool, needs, ach, shifts);
            inventory = inv;
            systems.Add(ach);
            systems.Add(needs);
            systems.Add(inv);
            shop=new ShopSystem(pool, inv, ach, () => shifts.Owner == "gang_cashier" || editor?.Editing==true, () => editor?.ServicePoints??System.Linq.Enumerable.Empty<EditorPoint>(),shifts);systems.Add(shop);
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
            systems.Add(new PdmStreaming());
            Func<bool> riskSynced=()=>true;
            var pdm = new PdmCompany(pool, ach, shifts); systems.Add(pdm);pdmOffice=pdm.StartPosition;
            var finance = new PdmFinanceSystem(pool, pdm.Financing, ach, pdm.OpenMenu); systems.Add(finance); pdm.OpenFinanceContracts = finance.OpenMenu; pdm.SyncFinance = finance.Flush;
            var staff = new PdmStaffSystem(pool, pdm.OfficeAction, pdm.OpenMenu); systems.Add(staff);
            pdm.OpenStaff = staff.OpenMenu; pdm.SyncStaff = staff.Flush;
            var haul = new PdmLogistics(pool, shifts, pdm.StartPosition); systems.Add(haul);
            haul.Back=pdm.OpenMenu; pdm.OpenCollection = haul.OpenMenu; pdm.DisplayStock = haul.Display; pdm.ClearDisplay = haul.ClearDisplay;
            var safe = new PdmSafeSystem(pool, shifts, pdm.OfficeAction, pdm.OpenMenu); systems.Add(safe); pdm.OpenSafe = safe.OpenMenu;
            var repo = new PdmRepoSystem(pool, shifts, pdm.CompanyOptions, pdm.Repossession, ach); systems.Add(repo); repo.Back=pdm.OpenMenu; pdm.OpenRepo = repo.OpenMenu; finance.OpenRepo = repo.OpenForLoan; finance.NewDefaultPercent = pdm.Repossession.DefaultPercent;
            var rental = new PdmRentalSystem(pool, shifts, pdm.CompanyOptions, haul.ClearDisplay, ach, pdm.OpenMenu); systems.Add(rental); pdm.OpenRental = rental.OpenMenu;
            var showroom = new PdmShowroomSystem(pool, shifts, pdm.CompanyOptions.Office, pdm.OfficeAction, haul.ClearDisplay, ach, pdm.OpenMenu); systems.Add(showroom);
            pdm.OpenShowroom = showroom.OpenMenu; pdm.DisplayStock = showroom.DisplayDefault; pdm.PrepareStock = showroom.Prepare; pdm.ClearDisplay = showroom.ClearAll;
            var premises = new PdmPremisesSystem(pool, shifts, pdm.CompanyOptions, staff.Premises, pdm.OfficeAction, showroom.GetDisplayVehicle, pdm.OpenMenu); systems.Add(premises);
            pdm.AccountsReady=()=>staff.Synced && finance.Synced && premises.Synced && riskSynced();
            pdm.OpenServices = premises.OpenMenu; pdm.SyncPremises = premises.Flush; staff.OpenServices = premises.OpenMenu;
            var security = new PdmSecuritySystem(pool, staff.Security, pdm.StartPosition, () => premises.StaffPeds, ()=>{staff.Flush();if(!staff.Synced)throw new InvalidOperationException("Payroll catch-up pending; defense suspended.");}, staff.OpenMenu, pdm.OpenMenu); systems.Add(security);
            pdm.OpenSecurity = security.OpenMenu; staff.OpenSecurity = security.OpenMenu;
            surveillance = new PdmSurveillanceSystem(pool, shifts, pdm.StartPosition, pdm.OfficeAction, pdm.OpenMenu); systems.Add(surveillance); pdm.OpenSurveillance = surveillance.OpenMenu;
            var risk = new PdmRiskSystem(pool, pdm.StartPosition, pdm.OfficeAction, pdm.OpenMenu); systems.Add(risk); pdm.OpenRisk = risk.OpenMenu; pdm.SyncRisk = risk.Flush;riskSynced=()=>risk.Synced;showroom.SyncAccounts=pdm.FlushAccounts;premises.SyncAccounts=pdm.FlushAccounts;
            pdm.ConnectedLocations=()=>haul.Locations.Concat(rental.Locations).Concat(showroom.Locations).Concat(premises.Locations).Concat(security.Locations).Concat(surveillance.Locations).Concat(safe.Locations).Concat(repo.Locations);
            var jobs = new DeliveryJobsMenu(pool, pizza, postOp, uber, valet, supply, tanker, prison, pilot, hunting, fishing, crafting, dealer, streamer, drag, pdm, haul, safe, repo, rental, showroom, premises); systems.Add(jobs);
            var belt = new SeatbeltSystem(ach); systems.Add(belt);
            var storage = new StorageSystem(pool, inv, ach, shifts); systems.Add(storage);
            AddOptional(() => new StockMarketSystem(pool));
            FuelSystem fuel = null;
            if (Cfg.EnableFuel) AddOptional(() => { fuel = new FuelSystem(pool, ach, shifts, () => editor?.ServicePoints??System.Linq.Enumerable.Empty<EditorPoint>()); return fuel; });
            VehicleControlSystem controls = null;
            if (Cfg.EnableVehicleControls) AddOptional(() => { controls = new VehicleControlSystem(pool, v => fuel == null || faulted.Contains(fuel) || fuel.CanStart(v)); return controls; });
            GarageSystem garages=null;
            if (Cfg.EnableGarages) AddOptional(() => {garages=new GarageSystem(pool, ach, shifts);return garages;});
            if (Cfg.EnableGarages) AddOptional(() => new DealershipSystem(pool, ach, shifts));
            GangSystem gangs = null;
            if (Cfg.EnableGangs) AddOptional(() => { gangs = new GangSystem(pool, ach, shifts); return gangs; });
            gangs?.ConfigureDealer(dealer);
            if (Cfg.EnableRadio) AddOptional(() => new RadioSystem(pool, ach));
            controls?.ConfigureSeatbelt(()=>belt.OnKey(new KeyEventArgs(Cfg.SeatbeltKey)),()=>belt.IsBelted);
            var visual = new VisualSystem(belt, () => controls != null && !faulted.Contains(controls) && controls.IsCruising, v => fuel == null || faulted.Contains(fuel) ? null : fuel.ViewTank(v));
            var props = new PropPresentation(inv); systems.Add(props);
            editor = new MapEditorSystem(pool, shifts, () => gangs==null || !gangs.HasPendingActivity, point => {if(garages!=null)garages.OpenCustomWorkshop(point,()=>editor.ServicePoints.Any(p=>p.Kind==EditorPointKind.Workshop && p.Name==point.Name && Math.Abs(p.Pose.X-point.X)<.01f && Math.Abs(p.Pose.Y-point.Y)<.01f && Math.Abs(p.Pose.Z-point.Z)<.01f));else Notify.Show("Ative Garages em Settings.ini para reparo/lavagem.");});systems.Add(editor);
            var studio = new StudioHeistSystem(pool, shifts, () => gangs==null || !gangs.HasPendingActivity); systems.Add(studio);
            var halloween = new HalloweenSystem(pool, shifts, () => gangs==null || !gangs.HasPendingActivity); systems.Add(halloween);
            var passenger = new PassengerSystem(pool, shifts, () => gangs==null || !gangs.HasPendingActivity);systems.Add(passenger);
            var world = new WorldSystem(pool, shifts);world.ClockChanged+=needs.IgnoreNativeBedClock;systems.Add(world);
            phone = new PhoneSystem(pool, DispatchSystemKey, props, inv, needs, jobs.Open, uber, streamer, studio.OpenMenu, passenger.OpenMenu); systems.Add(phone);phone.BankTransferred=()=>{ach.Bump("bank_used");if(Wallet.Bank>=1000000)ach.Bump("bank_million");ach.Save();};
            inv.PhoneRequested=()=>phone.OnKey(new KeyEventArgs(Visuals.PhoneKey));
            var housing = new HousingSystem(pool, shifts); systems.Add(housing);
            var wardrobe = new WardrobeSystem(pool, shifts, () => housing.CanUseWardrobe, housing.OpenMenu); systems.Add(wardrobe); housing.OpenWardrobe = wardrobe.OpenMenu;
            var homeStorage = new HomeStorageSystem(pool, shifts, storage, () => housing.CanUseInterior); systems.Add(homeStorage); housing.OpenStorage = homeStorage.OpenMenu;
            var barber = new BarberSystem(pool, shifts); systems.Add(barber);
            var grooming = new GroomingSystem(pool, shifts, () => barber.CanUseService, barber.OpenMenu); systems.Add(grooming); barber.OpenDetails = grooming.OpenMenu;
            var characters = new CharacterSystem(pool, shifts, SwitchCharacter); systems.Add(characters);
            var insurance = new VehicleInsuranceSystem(pool, shifts); systems.Add(insurance);
            var dmv = new DmvSystem(pool, shifts, () => belt.IsBelted); systems.Add(dmv); characters.SnapshotFilter = dmv.FilterCharacterSnapshot;
            var custody = new CustodySystem(pool, shifts, () => gangs==null || !gangs.HasPendingActivity); systems.Add(custody);
            var traffic = new TrafficSystem(pool, shifts, () => belt.IsBelted, () => gangs==null || !gangs.HasPendingActivity, custody.OpenMenu); systems.Add(traffic);
            systems.Add(new RoleplayBootstrap(shifts));
            core = new CoreMenu(pool, ach, SaveAll, visual.Diagnose, jobs.Open, housing.OpenMenu, barber.OpenMenu, characters.OpenMenu, insurance.OpenMenu, dmv.OpenMenu, traffic.OpenMenu, studio.OpenMenu, halloween.OpenMenu, editor.OpenMenu, passenger.OpenMenu, world.OpenMenu); systems.Add(core);
            settings = new SettingsSystem(pool, DispatchSystemKey, needs, gangs, jobs.Open, postOp.StartPosition, valet.StartPosition, supply.StartPosition, tanker.StartPosition, prison.StartPosition, pilot.StartPosition, hunting.StartPosition, hunting.ForestPosition, fishing.StartPosition, crafting.StartPosition, dealer.StartPosition, drag.StartPosition, pdm.StartPosition, traffic.OpenMenu, studio.OpenMenu, halloween.OpenMenu, editor.OpenMenu, passenger.OpenMenu, world.OpenMenu); systems.Add(settings);
            Func<IEnumerable<GpsDestination>> navPoints=()=>
                (!faulted.Contains(shop)?shop.GpsPoints:Enumerable.Empty<GpsDestination>())
                .Concat(fuel!=null && Cfg.EnableFuel && !faulted.Contains(fuel)?fuel.GpsPoints:Enumerable.Empty<GpsDestination>())
                .Concat(garages!=null && Cfg.EnableGarages && !faulted.Contains(garages)?garages.GpsPoints:Enumerable.Empty<GpsDestination>())
                .Concat(!faulted.Contains(barber)?barber.GpsPoints:Enumerable.Empty<GpsDestination>())
                .Concat(!faulted.Contains(wardrobe)?wardrobe.GpsPoints:Enumerable.Empty<GpsDestination>())
                .Concat(editor.ServicePoints.Where(p=>p.Kind==EditorPointKind.Grocery || p.Kind==EditorPointKind.Fuel && Cfg.EnableFuel || p.Kind==EditorPointKind.Workshop && Cfg.EnableGarages).Select(p=>new GpsDestination{Name=p.Name,Kind=p.Kind==EditorPointKind.Grocery?"Grocery":p.Kind==EditorPointKind.Fuel?"Fuel":"Workshop",X=p.Pose.X,Y=p.Pose.Y,Z=p.Pose.Z}))
                .Concat(!faulted.Contains(pdm)?new[]{new GpsDestination{Name="PDM / empresa",Kind="PDM",X=pdm.StartPosition.X,Y=pdm.StartPosition.Y,Z=pdm.StartPosition.Z}}:Enumerable.Empty<GpsDestination>());
            settings.ConfigureNavigation(navPoints,shifts);
            settings.PdmRequested=()=>OpenPhoneService(pdm,pdm.OpenMenu);
            settings.PdmGpsRequested=()=>OpenPhoneService(pdm,pdm.OpenLocations);
            settings.GarageRequested=()=>OpenPhoneSystem(garages,Cfg.GarageKey);
            settings.DealerRequested=()=>OpenPhoneSystem(systems.OfType<DealershipSystem>().FirstOrDefault(),Cfg.DealerKey);
            settings.VehicleRequested=()=>OpenPhoneService(controls,()=>controls.OpenMenu());
            phone.ConfigureServices(new Dictionary<int,Action>{
                {1,()=>OpenPhoneSystem(inv,Cfg.InventoryKey)},
                {2,()=>OpenPhoneSystem(garages,Cfg.GarageKey)},
                {3,()=>OpenPhoneSystem(systems.OfType<StockMarketSystem>().FirstOrDefault(),Cfg.StocksKey)},
                {4,()=>OpenPhoneSystem(systems.OfType<RadioSystem>().FirstOrDefault(),Cfg.RadioKey)},
                {5,()=>OpenPhoneSystem(gangs,Cfg.GangsKey)},
                {6,()=>OpenPhoneSystem(pizza,Cfg.JobKey)},{7,()=>OpenPhoneSystem(settings,InterfaceOptions.SettingsKey)},{8,()=>OpenPhoneService(jobs,jobs.Open)}});
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
            });radial.Configure(inv,wardrobe.OpenMenu,navPoints);radial.ConfigureVehicle(()=>Game.Player.Character.IsInVehicle(),action=>OpenPhoneService(controls,()=>controls.Execute(action)),()=>belt.OnKey(new KeyEventArgs(Cfg.SeatbeltKey))); systems.Add(radial);
            systems.Add(visual);
            ach.Bump("started");

            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += (s, e) => heldKeys.Remove(e.KeyCode);
            Aborted += OnAborted;
            nextSave = Game.GameTime + Cfg.AutosaveMs;
            for(int i=0;i<systems.Count;i++)traceIds[systems[i]]=i;
            try{
                trace=new RuntimeTrace(Cfg.Dir,GetType().Assembly.GetName().Version.ToString(),systems.Select(x=>x.Name).ToArray());
                if(trace.Previous!=null)Log.Write("Previous runtime stage: "+trace.Previous.Stage+"; OnAborted hook="+trace.Previous.AbortedHookReached+". Context only, not proof of crash cause.");
            }catch(Exception ex){Log.Error("optional runtime trace",ex);}
            Log.Write("RPCore 0.107.5 up, " + systems.Count + " systems, profile " + Profile.Id);
        }

        void OpenPhoneService(ISystem system,Action action){if(system==null || faulted.Contains(system)){Notify.Show("Servico indisponivel. Confira RPCore.log.");return;}action();}
        void OpenPhoneSystem(ISystem system,Keys key)=>OpenPhoneService(system,()=>system.OnKey(new KeyEventArgs(key)));

        void AddOptional(Func<ISystem> create)
        {
            try { systems.Add(create()); }
            catch (Exception ex) { Log.Error("system initialization", ex); Notify.Show("A system could not start. See RPCore.log."); }
        }

        void Stage(int code)
        {
            try{trace?.Mark(code);}catch(Exception ex){try{trace?.Dispose();}catch{}trace=null;Log.Error("runtime trace disabled",ex);}
        }
        void Stage(ISystem system,int group){if(traceIds.TryGetValue(system,out int index))Stage(group*1000+index);}
        void OnTick(object sender, EventArgs e)
        {
            Stage(1);
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;
            lastFrameAt=Game.GameTime;
            bool nearPdm=player.Position.DistanceTo(pdmOffice)<220 || player.Position.DistanceTo(new GTA.Math.Vector3(-48,-1104,26.4f))<220;
            try{GameplayContext.Update(player,nearPdm);TraceGameplay("area",false);}catch(Exception ex){Log.Error("gameplay context sample",ex);}
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
                    try { Stage(s,4);s.Reload(); }
                    catch (Exception ex) { faulted.Add(s); Log.Error(s.Name + ".reload", ex); }
                }
                Notify.Show("Profile: " + pid);
                Log.Write("profile -> " + pid);
            }

            Stage(2);Wallet.SynchronizeCash();Stage(4);
            // World Context is polled by shops before the late radial/phone draw ticks.
            // Publish modal state first so E and controller input cannot open a shop underneath a custom view.
            InterfaceOptions.ModalOpen = inventory.GridVisible || shop.GridVisible || phone.Visible || radial.Visible || surveillance.Viewing || editor.Manipulating;
            if (InterfaceOptions.ModalOpen && pool.AreAnyVisible) pool.HideAll();
            Stage(3);
            try { PdmMenuGroup.BeforeProcess(pool);pool.Process(); }
            catch (Exception ex) {
                Log.Error("menu dispatch", ex);
                foreach(var item in pool){try{item.Visible=false;}catch(Exception cleanup){Log.Error("menu dispatch close: "+item.GetType().Name,cleanup);}}
            }
            finally{Stage(4);}

            foreach (ISystem s in systems)
            {
                if (faulted.Contains(s)) continue;
                try { Stage(s,1);s.Tick(); }
                catch (Exception ex)
                {
                    faulted.Add(s); // one bad system doesn't take the script down
                    try { Stage(s,5);s.Dispose(); } catch (Exception cleanup) { Log.Error(s.Name + ".cleanup", cleanup); }
                    Log.Error(s.Name, ex);
                    Notify.Show("~r~" + s.Name + " crashed, see scripts\\RPCore\\RPCore.log");
                }
                finally{Stage(4);}
            }

            // Input policy is independent of armory IO, activity gates and inventory faults.
            Stage(8);try { inventory.EnforceWeaponControls(); } catch (Exception ex) { Log.Error("inventory input policy", ex); }finally{Stage(4);}

            if (unchecked(Game.GameTime - nextSave) >= 0)
            {
                SaveAll();
                nextSave = Game.GameTime + Cfg.AutosaveMs;
            }
            Stage(4);try{trace?.Heartbeat();}catch(Exception ex){Log.Error("runtime trace heartbeat",ex);}
        }

        void OnKeyDown(object sender,KeyEventArgs e)
        {Stage(5);try{HandleKeyDown(sender,e);}finally{Stage(4);}}
        void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if (!heldKeys.Add(e.KeyCode) || Game.IsPaused) return;
            var character = Game.Player.Character;
            if (character == null || !character.Exists() || character.IsDead) return;
            if(Profile.Detect()!=Profile.Id)return;
            if(survival.Sleeping)return;
            if(editor.Manipulating){editor.OnKey(e);return;}
            if (surveillance.Viewing) { surveillance.OnKey(e); return; }
            if(shop.GridVisible && !faulted.Contains(shop)){try{shop.OnKey(e);}catch(Exception ex){faulted.Add(shop);Log.Error("shop.grid.key",ex);shop.Dispose();}return;}
            if (inventory.GridVisible && !faulted.Contains(inventory))
            {
                try { inventory.OnKey(e); }
                catch (Exception ex) { faulted.Add(inventory); Log.Error("inventory.grid.key", ex); inventory.Dispose(); }
                return;
            }
            if(inventory.UsingItem)return;
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
            if(!pool.AreAnyVisible && !faulted.Contains(inventory))try{if(inventory.TryQuickUse(e))return;}catch(Exception ex){Log.Error("inventory.quick.key",ex);return;}
            if (e.KeyCode == Cfg.DumpKey)
            {
                var p = Game.Player.Character;
                string s = string.Format(CultureInfo.InvariantCulture, "{0:F2}, {1:F2}, {2:F2}  h={3:F1}",
                    p.Position.X, p.Position.Y, p.Position.Z, p.Heading);
                Log.Write("POS " + s);
                TraceGameplay("F9",true);
                Notify.Show("pos " + s);
                return;
            }

            DispatchSystemKey(e.KeyCode);
        }

        void DispatchSystemKey(Keys key)
        {
            var e = new KeyEventArgs(key);
            if(key==InterfaceOptions.SettingsKey || key==Cfg.CoreKey || key==Visuals.PhoneKey || key==InterfaceOptions.RadialKey)TraceGameplay("key:"+key,true);
            if (key == InterfaceOptions.SettingsKey)
            {
                if (!faulted.Contains(settings))
                    try { settings.OnKey(e); } catch (Exception ex) { faulted.Add(settings); Log.Error("settings.key", ex); settings.Dispose(); }
                return;
            }
            foreach (ISystem s in systems)
            {
                if (faulted.Contains(s)) continue;
                try { Stage(s,3);s.OnKey(e); }
                catch (Exception ex) { faulted.Add(s); Log.Error(s.Name + ".key", ex); try { Stage(s,5);s.Dispose(); } catch { } }
            }
        }

        void TraceGameplay(string reason,bool force)
        {
            if(!force && unchecked(Game.GameTime-nextContextAt)<0)return;nextContextAt=Game.GameTime+1000;
            try{
                var ped=Game.Player.Character;var position=ped.Position;
                bool near=position.DistanceTo(pdmOffice)<220 || position.DistanceTo(new GTA.Math.Vector3(-48,-1104,26.4f))<220;
                if(!force && !near && !wasNearPdm)return;
                string visible=string.Join(" | ",pool.OfType<LemonUI.Menus.NativeMenu>().Where(m=>m.Visible).Select(m=>m.Name));
                var state=new{
                    nearPdm=near,mission=Game.IsMissionActive,cutsceneActive=GameplayContext.Active,cutscenePlaying=GameplayContext.Playing,
                    preparedOnly=GameplayContext.PreparedOnly,sceneBlocking=GameplayContext.SceneBlocking,control=GameplayContext.Control,
                    hudHidden=GTA.Native.Function.Call<bool>(GTA.Native.Hash.IS_HUD_HIDDEN),paused=Game.IsPaused,
                    fadedIn=GTA.UI.Screen.IsFadedIn,gameplayCamera=GTA.Native.Function.Call<bool>(GTA.Native.Hash.IS_GAMEPLAY_CAM_RENDERING),
                    terrainReady=GameplayContext.TerrainReady,frozen=ped.IsPositionFrozen,visibleMenus=visible,
                    modal=InterfaceOptions.ModalOpen,phone=phone.Visible,radial=radial.Visible,inventory=inventory.GridVisible,
                    shop=shop.GridVisible,monitor=surveillance.Viewing,editor=editor.Manipulating,sleep=survival.Sleeping,
                    failed=string.Join(",",faulted.Select(x=>x.Name)),profile=Profile.Id,detected=Profile.Detect()
                };
                string key=Newtonsoft.Json.JsonConvert.SerializeObject(state);
                if(force || key!=lastContext || near!=wasNearPdm){
                    Log.Write("UI context ["+reason+"] "+key+" / frameAgeMs="+unchecked(Game.GameTime-lastFrameAt)+" / pos="+position.X+","+position.Y+","+position.Z);
                    lastContext=key;
                }wasNearPdm=near;
            }catch(Exception ex){Log.Error("UI context diagnostic",ex);}
        }
        void SwitchCharacter(string target,int model)
        {
            if(!CharacterRules.CanTransition(target,model))throw new InvalidOperationException("Invalid character model/profile transition.");
            // Unlike routine autosave, a failed flush aborts an explicit model/profile transition.
            foreach(var system in systems)if(!faulted.Contains(system))system.Save();
            Wallet.Save();
            bool custom=CharacterRules.ProtagonistHash(target)==0;
            string previous=Cfg.CustomProfile,activePath=System.IO.Path.Combine(Cfg.Dir,"ActiveProfile.json");
            // Prove the selected profile can be persisted before replacing the player ped.
            if(custom)new JsonFiles().Write(activePath,target);
            try{
                GTA.Native.Function.Call(GTA.Native.Hash.SET_PLAYER_MODEL,Game.Player,model);
                if(Game.Player.Character.Model.Hash!=model)throw new InvalidOperationException("Player model did not change.");
            }
            catch{
                if(custom)try{new JsonFiles().Write(activePath,previous);}catch(Exception ex){Log.Error("active character rollback",ex);}
                throw;
            }
            if(custom)Cfg.CustomProfile=target;
            Profile.Id=target;Wallet.Reload();
            foreach(var system in systems){try{system.Reload();}catch(Exception ex){faulted.Add(system);Log.Error(system.Name+".character.reload",ex);}}
            Log.Write("character model/profile -> "+target);
        }

        void SaveAll()
        {
            foreach (ISystem s in systems)
            {
                if (faulted.Contains(s)) continue;
                try { Stage(s,2);s.Save(); }
                catch (Exception ex) { Log.Error(s.Name + ".save", ex); }
            }
            Stage(6);try { Wallet.Save(); } catch (Exception ex) { Log.Error("wallet.save", ex); }finally{Stage(4);}
        }

        void OnAborted(object sender, EventArgs e)
        {
            try{trace?.Aborted();}catch{}
            InterfaceOptions.ModalOpen = false;
            SaveAll();
            foreach (ISystem s in systems)
            {
                try { Stage(s,5);s.Dispose(); }
                catch (Exception ex) { Log.Error(s.Name + ".dispose", ex); }
            }
            try{Sfx.Dispose();}finally{try{trace?.Aborted();trace?.Dispose();}catch(Exception ex){Log.Error("runtime trace close",ex);}trace=null;}
        }
    }
}
