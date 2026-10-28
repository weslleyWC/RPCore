using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
using Newtonsoft.Json;

namespace RPCore
{
    public class GarageDefinition
    {
        public string Id, Name;
        public float X, Y, Z, SpawnX, SpawnY, SpawnZ, Heading;
        public int Price = 10000, Capacity = 3;
        [JsonIgnore] public Vector3 Position { get { return new Vector3(X, Y, Z); } }
        [JsonIgnore] public Vector3 Spawn { get { return new Vector3(SpawnX, SpawnY, SpawnZ); } }
    }

    public class GarageSystem : ISystem
    {
        public string Name { get { return "garages"; } }
        public IEnumerable<GpsDestination> GpsPoints=>garages.Select(p=>new GpsDestination{Name=p.Name,Kind="Garage",X=p.X,Y=p.Y,Z=p.Z}).Concat(garages.Where(p=>data.Garages.Contains(p.Id)).Select(p=>new GpsDestination{Name="Oficina / "+p.Name,Kind="Workshop",X=p.X,Y=p.Y,Z=p.Z}));
        readonly ObjectPool pool;
        readonly WorkshopSystem workshop;
        readonly NativeMenu menu = new NativeMenu("Garages", "");
        readonly AchievementSystem ach;
        readonly List<GarageDefinition> garages;
        readonly List<Blip> blips = new List<Blip>();
        readonly Dictionary<string, Vehicle> live = new Dictionary<string, Vehicle>();
        readonly Dictionary<string, Vehicle> created = new Dictionary<string, Vehicle>();
        TransportData data;
        GarageDefinition current, pendingGarage;Vector3? customPoint;Vehicle customCar;Ped customActor;string customProfile;
        Vehicle pending;
        int timeout;
        bool dirty;

        public GarageSystem(ObjectPool pool, AchievementSystem ach, ShiftGate gate = null)
        {
            this.pool = pool; this.ach = ach; workshop = new WorkshopSystem(pool, gate);
            garages = Store.LoadDef("Garages/Locations.json", DefaultGarages).Where(ValidDefinition).GroupBy(g => g.Id).Select(g => g.First()).ToList();
            Reload();
            foreach (var g in garages)
            {
                var b = MapBlips.Create(g.Position, "RPCore Garage: " + g.Name, BlipSprite.Garage, BlipColor.Blue, .8f); blips.Add(b);
            }
            pool.Add(menu); Visuals.Register(menu, "garage");
        }
        public static List<GarageDefinition> DefaultGarages()
        {
            return new List<GarageDefinition> {
                new GarageDefinition { Id = "strawberry", Name = "Strawberry Garage", X = -79.8f, Y = -1826.4f, Z = 26.9f, SpawnX = -73.3f, SpawnY = -1834.0f, SpawnZ = 26.8f, Heading = 138, Price = 10000, Capacity = 3 },
                new GarageDefinition { Id = "burton", Name = "Burton Garage", X = -354.5f, Y = -134.0f, Z = 39.0f, SpawnX = -365.3f, SpawnY = -124.2f, SpawnZ = 38.7f, Heading = 70, Price = 25000, Capacity = 6 },
                new GarageDefinition { Id = "sandy", Name = "Sandy Shores Garage", X = 1175.0f, Y = 2640.4f, Z = 37.8f, SpawnX = 1179.3f, SpawnY = 2633.3f, SpawnZ = 37.8f, Heading = 0, Price = 8000, Capacity = 2 }
            };
        }
        public static bool ValidDefinition(GarageDefinition g)
        {
            return g != null && !string.IsNullOrWhiteSpace(g.Id) && g.Price >= 0 && g.Capacity > 0 && g.Capacity <= 50 && new[] { g.X, g.Y, g.Z, g.SpawnX, g.SpawnY, g.SpawnZ, g.Heading }.All(n => !float.IsNaN(n) && !float.IsInfinity(n));
        }
        void Add(string title, string desc, Action action)
        {
            var row = new NativeItem(title, desc);
            row.Activated += (s, e) => { action(); dirty = true; }; menu.Add(row);
        }
        void Rebuild()
        {
            customPoint=null;customCar=null;customActor=null;menu.Clear(); var p = Game.Player.Character;
            current = garages.OrderBy(g => p.Position.DistanceTo(g.Position)).FirstOrDefault(g => p.Position.DistanceTo(g.Position) < 15);
            if (current == null)
            {
                menu.Name = "Visit a garage";
                foreach (var entry in garages)
                {
                    var g = entry;
                    Add("GPS: " + g.Name, (data.Garages.Contains(g.Id) ? "Owned" : "$" + g.Price) + " / " + g.Capacity + " vehicles", () => { Function.Call(Hash.SET_NEW_WAYPOINT, g.X, g.Y); menu.Visible = false; });
                }
                dirty = false; return;
            }
            var selected = current;
            menu.Name = current.Name + " / cash $" + Wallet.Cash;
            if (!data.Garages.Contains(current.Id)) Add("Buy garage: $" + current.Price, current.Capacity + " vehicle slots. Purchase requires being nearby.", () => Buy(selected));
            else
            {
                Add("Personalizar veiculo", "Pecas, desempenho, rodas, cores e equipamentos; pagamento pelo banco.", () => workshop.Open(() => Nearby(selected) && data.Garages.Contains(selected.Id)));
                Add("Store current vehicle", "Stop in the garage area as driver. RPCore asks you to exit before storing. Mission vehicles are protected by default.", () => BeginStore(selected));
                Add("Repair current vehicle: $500", "At this garage, stopped in the driver seat. Fuel is preserved.", () => Service(false));
                Add("Wash current vehicle: $25", "At this garage, stopped in the driver seat.", () => Service(true));
                foreach (var record in data.Vehicles.Where(v => v.GarageId == current.Id).ToList())
                {
                    var car = record;
                    Add((car.Stored ? "Retrieve: " : "Locate / recover: ") + (car.Name ?? car.Model.ToString()) + " [" + car.Plate + "]", "Restores saved equipment, paint, fuel and damage. The exit must be clear.", () => Retrieve(selected, car));
                }
                menu.Name += " / " + data.Vehicles.Count(v => v.GarageId == current.Id) + "/" + current.Capacity;
            }
            dirty = false;
        }
        bool Nearby(GarageDefinition garage)
        {
            var p = Game.Player.Character;
            return Cfg.EnableGarages && !Game.IsMissionActive && !p.IsDead && p.Position.DistanceTo(garage.Position) < 15;
        }
        void Buy(GarageDefinition garage)
        {
            if (!Nearby(garage) || data.Garages.Contains(garage.Id)) return;
            if (!Wallet.Debit(garage.Price)) { Notify.Show("Not enough cash."); return; }
            data.Garages.Add(garage.Id);
            try { Wallet.Save(); }
            catch { data.Garages.Remove(garage.Id); Wallet.Credit(garage.Price); throw; }
            ach.Bump("garages_bought"); ach.Save(); Notify.Show("Garage purchased: " + garage.Name);
        }

        bool MayStore(Vehicle v, GarageDefinition garage, bool driverRequired)
        {
            var p = Game.Player.Character;
            if (JobVehicles.Contains(v)) return false;
            if (!Nearby(garage) || !data.Garages.Contains(garage.Id) || v == null || !v.Exists() || v.IsDead || v.Speed > .5f || v.Position.DistanceTo(garage.Position) > 15 || !(v.Model.IsCar || v.Model.IsBike || v.Model.IsQuadBike) || v.Model.IsBicycle) return false;
            if (driverRequired && v.Driver != p) return false;
            if (v.PassengerCount > 0 || (v.Driver != null && v.Driver.Exists() && v.Driver != p) || Function.Call<bool>(Hash.IS_ENTITY_ATTACHED, v)) return false;
            var insuredRecord=data.FindIdentity(v.Model.Hash, Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT,v));
            if(Wallet.VehicleInsurance.Pending!=null && insuredRecord?.Id==Wallet.VehicleInsurance.Pending.VehicleId)return false;
            bool owned = data.FindIdentity(v.Model.Hash, Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, v)) != null;
            if (!owned && !Cfg.AllowMissionStorage && Function.Call<bool>(Hash.IS_ENTITY_A_MISSION_ENTITY, v)) return false;
            return true;
        }
        void BeginStore(GarageDefinition garage)
        {
            var p = Game.Player.Character; var v = p.IsInVehicle() ? p.CurrentVehicle : null;
            if (!MayStore(v, garage, true)) { Notify.Show("Use a stopped road vehicle as driver, without passengers or attachments. Mission vehicles are protected."); return; }
            var existing = data.FindIdentity(v.Model.Hash, Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, v));
            var preview = new SavedVehicle { Model = v.Model.Hash, Plate = Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, v), Id = existing?.Id ?? Guid.NewGuid().ToString("N") };
            if (!data.CanStore(garage.Id, garage.Capacity, preview) || (existing != null && existing.Stored && !existing.PendingRemoval)) { Notify.Show("Garage full, vehicle has no plate, or this identity is already stored."); return; }
            pending = v; pendingGarage = garage; timeout = Game.GameTime + 15000; menu.Visible = false;
            // Native leave task lets the player exit deliberately even if the belt was previously engaged.
            Function.Call(Hash.TASK_LEAVE_VEHICLE, p, v, 0);
            Notify.Show("Exit the vehicle and stay near the garage to store it.");
        }
        void FinishStore()
        {
            var p = Game.Player.Character;
            if (pending == null || !pending.Exists()) { pending = null; return; }
            if (unchecked(Game.GameTime - timeout) >= 0 || !MayStore(pending, pendingGarage, false) || p.Position.DistanceTo(pending.Position) > 12)
            { pending = null; Notify.Show("Storage cancelled. Vehicle remains in the world."); return; }
            if (p.IsInVehicle() || pending.Occupants.Length > 0) return;
            var snapshot = VehicleSnapshot.Capture(pending);
            var old = data.FindIdentity(snapshot.Model, snapshot.Plate);
            snapshot.Id = old?.Id ?? snapshot.Id; snapshot.GarageId = pendingGarage.Id; snapshot.Stored = true; snapshot.PendingRemoval = true; snapshot.Extra = old?.Extra;
            if (!data.CanStore(pendingGarage.Id, pendingGarage.Capacity, snapshot)) { pending = null; Notify.Show("Garage capacity changed. Storage cancelled."); return; }
            int index = old == null ? -1 : data.Vehicles.IndexOf(old);
            if (index < 0) data.Vehicles.Add(snapshot); else data.Vehicles[index] = snapshot;
            try { Wallet.Save(); }
            catch { if (index < 0) data.Vehicles.Remove(snapshot); else data.Vehicles[index] = old; pending = null; throw; }
            // A durable snapshot exists before taking/removing the world entity.
            Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, pending, true, true);
            pending.Delete();
            if (pending.Exists())
            {
                snapshot.Stored = false; snapshot.PendingRemoval = false; live[snapshot.Id] = pending;
                Notify.Show("Vehicle could not be removed. It remains outside; use Locate.");
            }
            else { snapshot.PendingRemoval = false; live.Remove(snapshot.Id); Notify.Show("Vehicle stored."); }
            pending = null; Wallet.Save();
        }

        Vehicle FindLive(SavedVehicle record)
        {
            Vehicle v;
            if (live.TryGetValue(record.Id, out v) && v != null && v.Exists() && VehicleIdentity.Key(v.Model.Hash, Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, v)) == VehicleIdentity.Key(record.Model, record.Plate)) return v;
            string key = VehicleIdentity.Key(record.Model, record.Plate);
            v = World.GetAllVehicles().FirstOrDefault(c => c.Exists() && VehicleIdentity.Key(c.Model.Hash, Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, c)) == key);
            if (v != null) live[record.Id] = v;
            return v;
        }
        void Retrieve(GarageDefinition garage, SavedVehicle record)
        {
            if(Wallet.VehicleInsurance.Pending?.VehicleId==record.Id){Notify.Show("Conclua ou encerre o pedido de seguro antes de retirar este veiculo.");return;}
            if (!Nearby(garage) || !data.Garages.Contains(garage.Id) || pending != null) return;
            var existing = FindLive(record);
            if (existing != null)
            {
                Function.Call(Hash.SET_NEW_WAYPOINT, existing.Position.X, existing.Position.Y);
                record.Stored = false; record.PendingRemoval = false; Wallet.Save();
                Notify.Show("Vehicle already in the world. GPS set; no duplicate created."); return;
            }
            if (World.GetNearbyVehicles(garage.Spawn, 5).Length > 0 || World.GetNearbyPeds(garage.Spawn, 2).Length > 0)
            { Notify.Show("Garage exit blocked. Clear the spawn area first."); return; }
            Model model = new Model(record.Model);
            if (!model.IsValid || !model.IsInCdImage || !model.IsVehicle || !model.Request(3000)) { Notify.Show("Saved vehicle model unavailable. Restore the required vehicle mod first."); return; }
            Vehicle spawned = null;
            bool wasStored = record.Stored, wasPending = record.PendingRemoval;
            try
            {
                spawned = World.CreateVehicle(model, garage.Spawn, garage.Heading);
                if (spawned == null || !spawned.Exists()) { Notify.Show("Vehicle creation failed; garage record preserved."); return; }
                VehicleSnapshot.Apply(spawned, record); spawned.IsPersistent = true;
                record.Stored = false; record.PendingRemoval = false;
                try { Wallet.Save(); }
                catch { record.Stored = wasStored; record.PendingRemoval = wasPending; spawned.Delete(); throw; }
                live[record.Id] = spawned; created[VehicleIdentity.Key(record.Model, record.Plate)] = spawned; menu.Visible = false;
                Notify.Show("Vehicle retrieved. Enter it at the garage exit.");
            }
            catch
            {
                if (spawned != null && spawned.Exists() && !live.ContainsKey(record.Id)) spawned.Delete();
                throw;
            }
            finally { model.MarkAsNoLongerNeeded(); }
        }
        bool CustomAccess=>Cfg.EnableGarages && customPoint.HasValue && customProfile==Profile.Id && customActor==Game.Player.Character && customActor!=null && customActor.Exists() && customActor.IsAlive && customCar!=null && customCar.Exists() && customCar.Driver==customActor && customActor.CurrentVehicle==customCar && (customCar.Model.IsCar || customCar.Model.IsBike && !customCar.Model.IsBicycle) && customCar.Speed<.5f && customCar.Position.DistanceTo(customPoint.Value)<=4.5f && !JobVehicles.Contains(customCar) && !Game.IsMissionActive && Game.Player.Wanted.WantedLevel==0;
        public void OpenCustomWorkshop(JobPoint point)
        {
            customPoint=new Vector3(point.X,point.Y,point.Z);customProfile=Profile.Id;customActor=Game.Player.Character;customCar=customActor.CurrentVehicle;current=null;dirty=false;
            if(!CustomAccess){menu.Visible=false;Notify.Show("Pare como motorista de carro pessoal, sem procurado/missao.");return;}pool.HideAll();menu.Clear();menu.Name=point.Name;
            Add("Personalizar veiculo","Pecas, pintura e equipamentos; banco.",()=>{var point=customPoint.Value;workshop.Open(()=>Game.Player.Character.Position.DistanceTo(point)<=4.5f);});
            Add("Reparar: $500","Servico existente; preserva combustivel.",()=>Service(false));Add("Lavar: $25","Servico existente de lavagem.",()=>Service(true));Add("Fechar","Voltar ao jogo.",()=>menu.Visible=false);menu.Visible=true;
        }
        void Service(bool wash)
        {
            var p = Game.Player.Character; var v = p.IsInVehicle() ? p.CurrentVehicle : null;
            if (customPoint.HasValue ? !CustomAccess : current == null || !MayStore(v, current, true)) { Notify.Show("Stop in your garage as driver. Mission vehicles are protected."); return; }
            int cost = wash ? 25 : 500;
            if (!Wallet.Debit(cost)) { Notify.Show("Not enough cash."); return; }
            try
            {
                if (wash) v.DirtLevel = 0;
                else { float fuel = v.FuelLevel; v.Repair(); v.FuelLevel = fuel; }
                Wallet.Save(); Notify.Show(wash ? "Vehicle washed." : "Vehicle repaired.");
            }
            catch { Wallet.Credit(cost); throw; }
        }

        public void Tick()
        {
            workshop.Tick();
            if (!Cfg.EnableGarages || Game.IsMissionActive) { pending = null; menu.Visible = false; return; }
            if (Game.IsPaused) return;
            if(customPoint.HasValue && menu.Visible && !CustomAccess)menu.Visible=false;
            if (pending != null) FinishStore();
            if (menu.Visible && current != null && !Nearby(current)) menu.Visible = false;
            if (dirty && menu.Visible){if(customPoint.HasValue)dirty=false;else Rebuild();}
            var p = Game.Player.Character;
            foreach (var g in garages.Where(g => p.Position.DistanceTo(g.Position) < 35))
            {
                World.DrawMarker(MarkerType.Cylinder, g.Position - new Vector3(0, 0, 1), Vector3.Zero, Vector3.Zero, new Vector3(3, 3, 1), Color.FromArgb(90, 60, 140, 220));
                if (!pool.AreAnyVisible && p.Position.DistanceTo(g.Position) < 15) Prompt.Help(Cfg.GarageKey + ": " + g.Name + (data.Garages.Contains(g.Id) ? " (owned)" : " $" + g.Price));
            }
        }
        public void OnKey(KeyEventArgs e)
        {
            if (e.KeyCode != Cfg.GarageKey || !Cfg.EnableGarages || Game.IsMissionActive) return;
            if (menu.Visible) menu.Visible = false;
            else if (!pool.AreAnyVisible && pending == null) { Rebuild(); menu.Visible = true; }
        }
        public void Reload() { workshop.Close(); customPoint=null;customCar=null;customActor=null;menu.Visible=false; pending = null; ReleaseCreated(); live.Clear(); data = Wallet.Transport; }
        public void Save()
        {
            if (pending != null) return;
            foreach (var record in data.Vehicles.Where(v => !v.Stored && !v.PendingRemoval && Wallet.VehicleInsurance.Pending?.VehicleId!=v.Id).ToList())
            {
                var v = FindLive(record); if (v == null || v.IsDead) continue;
                var snapshot = VehicleSnapshot.Capture(v); snapshot.Id = record.Id; snapshot.GarageId = record.GarageId; snapshot.Extra = record.Extra;
                data.Vehicles[data.Vehicles.IndexOf(record)] = snapshot;
            }
            Wallet.Save();
        }
        public void Dispose()
        {
            workshop.Close();
            menu.Visible = false; pending = null;
            // Leave occupied vehicles and other mods' entities in place during a script reload.
            ReleaseCreated();
            live.Clear(); foreach (var b in blips) if (b.Exists()) b.Delete();
        }
        void ReleaseCreated()
        {
            foreach (var pair in created)
                if (pair.Value != null && pair.Value.Exists() && VehicleIdentity.Key(pair.Value.Model.Hash, Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, pair.Value)) == pair.Key) pair.Value.MarkAsNoLongerNeeded();
            created.Clear();
        }
    }
}
