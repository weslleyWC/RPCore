using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using Newtonsoft.Json;

namespace RPCore
{
    public class FuelStation
    {
        public string Name;
        public float X, Y, Z, Radius = 18;
        [JsonIgnore] public Vector3 Position { get { return new Vector3(X, Y, Z); } }
    }

    public class FuelSystem : ISystem
    {
        public string Name { get { return "fuel"; } }
        readonly ObjectPool pool;
        readonly AchievementSystem ach;
        readonly List<FuelStation> stations;
        readonly HashSet<int> exclusions = new HashSet<int>();
        readonly List<Blip> blips = new List<Blip>();
        TransportData data;
        Vehicle vehicle;
        FuelTank tank;
        string activeKey;
        bool pumping, boughtFuel, warned;
        int last, nextPump;

        public FuelSystem(ObjectPool pool, AchievementSystem ach)
        {
            this.pool = pool; this.ach = ach;
            stations = Store.LoadDef("Fuel/Stations.json", DefaultStations).Where(s => s != null && s.Radius > 0 && s.Radius <= 50 && new[] { s.X, s.Y, s.Z }.All(n => !float.IsNaN(n) && !float.IsInfinity(n))).ToList();
            string excluded = Path.Combine(Cfg.SaveDir, "ExcludedGasList.json");
            if (!File.Exists(excluded)) excluded = Path.Combine(Cfg.Dir, "Legacy", "SaveData", "ExcludedGasList.json");
            if (File.Exists(excluded)) foreach (var model in JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(excluded)) ?? new List<string>())
                if (!string.IsNullOrWhiteSpace(model)) exclusions.Add(new Model(model).Hash);
            Reload();
            foreach (var s in stations)
            {
                var b = World.CreateBlip(s.Position); b.Sprite = (BlipSprite)361; b.IsShortRange = true; b.Name = s.Name; blips.Add(b);
            }
        }

        public static List<FuelStation> DefaultStations()
        {
            float[][] points = {
                new[] { 49.4f, 2778.8f, 58.0f }, new[] { 263.9f, 2606.5f, 45.0f }, new[] { 1039.9f, 2671.1f, 39.6f },
                new[] { 1207.3f, 2660.2f, 37.9f }, new[] { 2539.7f, 2594.2f, 37.9f }, new[] { 2679.8f, 3263.9f, 55.2f },
                new[] { 2005.1f, 3773.9f, 32.4f }, new[] { 1687.2f, 4929.9f, 42.1f }, new[] { 1701.3f, 6416.0f, 32.8f },
                new[] { 179.8f, 6602.8f, 31.9f }, new[] { -94.5f, 6419.1f, 31.5f }, new[] { -2554.9f, 2334.4f, 33.1f },
                new[] { -1800.4f, 803.6f, 138.7f }, new[] { -1437.6f, -276.7f, 46.2f }, new[] { -2096.2f, -320.3f, 13.2f },
                new[] { -724.6f, -935.2f, 19.2f }, new[] { -526.0f, -1211.0f, 18.2f }, new[] { -70.2f, -1761.8f, 29.5f },
                new[] { 265.6f, -1261.3f, 29.3f }, new[] { 819.7f, -1028.8f, 26.4f }, new[] { 1208.9f, -1402.6f, 35.2f },
                new[] { 1181.4f, -330.8f, 69.3f }, new[] { 620.8f, 269.0f, 103.1f }, new[] { 2581.3f, 362.0f, 108.5f },
                new[] { 1785.6f, 3330.4f, 41.4f }, new[] { -319.3f, -1471.7f, 30.5f }
            };
            return points.Select((p, i) => new FuelStation { Name = "RPCore Fuel " + (i + 1), X = p[0], Y = p[1], Z = p[2] }).ToList();
        }

        bool Supported(Vehicle v)
        {
            return v != null && v.Exists() && !v.IsDead && (v.Model.IsCar || v.Model.IsBike || v.Model.IsQuadBike) && !v.Model.IsBicycle && !v.Model.IsElectricVehicle && !exclusions.Contains(v.Model.Hash);
        }
        string Key(Vehicle v) { return VehicleIdentity.Key(v.Model.Hash, Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, v)); }
        void Attach(Vehicle v)
        {
            if (vehicle != null && vehicle.Exists() && vehicle.Handle == v.Handle && tank != null && Key(v) == activeKey) return;
            StopPump(); vehicle = v; tank = null; warned = false;
            string key = Key(v);
            activeKey = key;
            if (key == null) return; // anonymous vehicles cannot safely share a persistent tank
            if (!data.Tanks.TryGetValue(key, out tank))
            {
                float capacity = v.PetrolTankVolume;
                if (float.IsNaN(capacity) || float.IsInfinity(capacity) || capacity <= 0 || capacity > 10000) capacity = 60;
                float nativeFuel = v.FuelLevel;
                if (float.IsNaN(nativeFuel) || float.IsInfinity(nativeFuel)) nativeFuel = capacity;
                tank = new FuelTank { Capacity = capacity, Liters = Math.Max(0, Math.Min(capacity, nativeFuel)) };
                data.Tanks[key] = tank;
            }
            tank.Validate(); v.FuelLevel = tank.Liters;
        }

        public bool CanStart(Vehicle v)
        {
            if (!Cfg.EnableFuel || Game.IsMissionActive || !Supported(v)) return true;
            string key = Key(v); FuelTank fuel;
            return key == null || !data.Tanks.TryGetValue(key, out fuel) || fuel.Liters > .001f;
        }
        public FuelTank ViewTank(Vehicle v)
        {
            FuelTank value;
            string key = Supported(v) ? Key(v) : null;
            return Cfg.EnableFuel && key != null && data.Tanks.TryGetValue(key, out value) ? value : null;
        }

        public void Tick()
        {
            int now = Game.GameTime; float seconds = Math.Min(1, Math.Max(0, unchecked(now - last)) / 1000f); last = now;
            if (!Cfg.EnableFuel || Game.IsPaused || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) || Game.IsMissionActive)
            { StopPump(); return; }
            Ped p = Game.Player.Character;
            if (p.IsDead) { StopPump(); return; }
            if (pumping)
            {
                if (!PumpAccessible(p)) { StopPump(); return; }
                if (unchecked(now - nextPump) >= 0) { nextPump = now + 1000; Pump(); }
                Draw(); return;
            }
            var current = p.IsInVehicle() ? p.CurrentVehicle : null;
            if (!Supported(current) || current.Driver != p) return;
            Attach(current); if (tank == null) return;
            if (current.IsEngineRunning) tank.Drive(seconds, current.Speed, Cfg.FuelPer100Km, Cfg.IdleFuelPerHour);
            current.FuelLevel = tank.Liters;
            if (tank.Liters <= .001f)
            {
                Function.Call(Hash.SET_VEHICLE_ENGINE_ON, current, false, true, false);
                Game.DisableControlThisFrame(GTA.Control.VehicleAccelerate);
            }
            if (!warned && tank.Liters / tank.Capacity < .1f) { warned = true; Notify.Show("~o~Low fuel. Stop at a station and press " + Cfg.FuelKey + " with the engine off."); }
            if (current.Speed < .5f && stations.Any(s => p.Position.DistanceTo(s.Position) < s.Radius)) Prompt.Help("Engine off, then " + Cfg.FuelKey + " to refuel. $" + Cfg.FuelPrice + "/L.");
            Draw();
        }

        bool PumpAccessible(Ped p)
        {
            if (!Supported(vehicle) || vehicle.Speed >= .5f || vehicle.IsEngineRunning || p.Position.DistanceTo(vehicle.Position) > 5) return false;
            if (p.IsInVehicle() && (p.CurrentVehicle != vehicle || vehicle.Driver != p)) return false;
            if (vehicle.Driver != null && vehicle.Driver.Exists() && vehicle.Driver != p) return false;
            return stations.Any(s => vehicle.Position.DistanceTo(s.Position) < s.Radius);
        }
        void Pump()
        {
            var quote = FuelTank.Quote(tank.Capacity - tank.Liters, Cfg.PumpLitersPerSecond, Wallet.Cash, Cfg.FuelPrice);
            if (quote.Cost <= 0 || !Wallet.Debit(quote.Cost)) { StopPump(); return; }
            float before = tank.Liters; tank.Liters = Math.Min(tank.Capacity, tank.Liters + quote.Liters);
            try { Wallet.Save(); }
            catch { tank.Liters = before; Wallet.Credit(quote.Cost); StopPump(); throw; }
            vehicle.FuelLevel = tank.Liters;
            if (!boughtFuel) { ach.Bump("gas_filled"); ach.Save(); }
            boughtFuel = true;
            if (tank.Capacity - tank.Liters < .01f) StopPump();
        }
        void StopPump()
        {
            if (!pumping) return;
            pumping = false; Sfx.Stop("fuel");
            if (boughtFuel) Notify.Show("Refueling finished.");
            boughtFuel = false;
        }
        void Draw()
        {
            if (tank == null) return;
            if (Visuals.VehicleReady)
            {
                if (pumping) Visuals.Text("Refueling: " + Cfg.FuelKey + " to stop", Visuals.Width - Visuals.InsetX - 360 * Visuals.Scale, 1080 - Visuals.InsetY - 260 * Visuals.Scale, .28f, Color.White);
                return;
            }
            string label = string.Format("FUEL {0:0.0}/{1:0} L  |  {2:0.0} km{3}", tank.Liters, tank.Capacity, tank.DistanceKm, pumping ? "  [" + Cfg.FuelKey + ": stop]" : "");
            new GTA.UI.TextElement(label, new PointF(GTA.UI.Screen.ScaledWidth - 390, GTA.UI.Screen.Height - 125), .32f, tank.Liters / tank.Capacity < .1f ? Color.OrangeRed : Color.White).Draw();
        }
        public void OnKey(KeyEventArgs e)
        {
            if (e.KeyCode != Cfg.FuelKey || !Cfg.EnableFuel) return;
            if (pumping) { StopPump(); return; }
            if (pool.AreAnyVisible || Game.IsMissionActive) return;
            var p = Game.Player.Character;
            var v = p.IsInVehicle() ? p.CurrentVehicle : World.GetNearbyVehicles(p, 5).Where(Supported).OrderBy(c => p.Position.DistanceTo(c.Position)).FirstOrDefault();
            if (!Supported(v)) { Notify.Show("Use a petrol vehicle with a license plate."); return; }
            Attach(v);
            if (tank == null || !PumpAccessible(p)) { Notify.Show("Stop near a fuel station, turn the engine off, and stay close to your vehicle."); return; }
            pumping = true; boughtFuel = false; nextPump = Game.GameTime; Sfx.Play("fuel");
        }
        public void Reload() { StopPump(); data = Wallet.Transport; vehicle = null; tank = null; activeKey = null; last = Game.GameTime; }
        public void Save() { Wallet.Save(); }
        public void Dispose() { StopPump(); foreach (var b in blips) if (b.Exists()) b.Delete(); }
    }
}
