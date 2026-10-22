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

namespace RPCore
{
    public class VehicleOffer
    {
        public string Model, Name;
        public int Price;
    }

    public class DealershipSystem : ISystem
    {
        public string Name { get { return "dealership"; } }
        readonly ObjectPool pool;
        readonly NativeMenu menu = new NativeMenu("RPCore Motors", "");
        readonly AchievementSystem ach;
        readonly List<VehicleOffer> offers;
        readonly List<GarageDefinition> garages;
        readonly Dictionary<string, Vehicle> created = new Dictionary<string, Vehicle>();
        readonly Random random = new Random();
        readonly Blip blip;
        readonly Vector3 entrance = new Vector3(-56.8f, -1096.7f, 26.4f);
        readonly Vector3 delivery = new Vector3(-28.5f, -1083.5f, 26.6f);
        TransportData data;
        bool dirty;

        public DealershipSystem(ObjectPool pool, AchievementSystem ach)
        {
            this.pool = pool; this.ach = ach; data = Wallet.Transport;
            offers = Store.LoadDef("Dealership/Vehicles.json", DefaultOffers).Where(o => o != null && !string.IsNullOrWhiteSpace(o.Model) && o.Price > 0).ToList();
            garages = Store.LoadDef("Garages/Locations.json", GarageSystem.DefaultGarages).Where(GarageSystem.ValidDefinition).GroupBy(g => g.Id).Select(g => g.First()).ToList();
            pool.Add(menu); blip = World.CreateBlip(entrance); blip.Sprite = (BlipSprite)225; blip.Name = "RPCore Motors"; blip.IsShortRange = true;
        }
        public static List<VehicleOffer> DefaultOffers()
        {
            return new List<VehicleOffer> {
                new VehicleOffer { Model = "faggio", Name = "Pegassi Faggio", Price = 3500 },
                new VehicleOffer { Model = "asea", Name = "Declasse Asea", Price = 12000 },
                new VehicleOffer { Model = "blista", Name = "Dinka Blista", Price = 18000 },
                new VehicleOffer { Model = "dominator", Name = "Vapid Dominator", Price = 45000 },
                new VehicleOffer { Model = "sultan", Name = "Karin Sultan", Price = 55000 }
            };
        }
        bool Nearby()
        {
            var p = Game.Player.Character;
            return Cfg.EnableGarages && !p.IsDead && !Game.IsMissionActive && p.Position.DistanceTo(entrance) < 20;
        }
        void Rebuild()
        {
            menu.Clear(); menu.Name = "Cash $" + Wallet.Cash + " / requires an owned garage slot";
            if (!Nearby())
            {
                var gps = new NativeItem("GPS: RPCore Motors", "Visit Premium Deluxe Motorsport to buy a vehicle.");
                gps.Activated += (s, e) => { Function.Call(Hash.SET_NEW_WAYPOINT, entrance.X, entrance.Y); menu.Visible = false; }; menu.Add(gps); return;
            }
            foreach (var item in offers)
            {
                var offer = item;
                var row = new NativeItem(offer.Name ?? offer.Model, "Buy and register in your first owned garage with a free slot. Delivery is outside the dealership.") { AltTitle = "$" + offer.Price };
                row.Activated += (s, e) => { Buy(offer); dirty = true; }; menu.Add(row);
            }
            dirty = false;
        }
        void Buy(VehicleOffer offer)
        {
            if (!Nearby()) return;
            var garage = garages.FirstOrDefault(g => data.Garages.Contains(g.Id) && data.Vehicles.Count(v => v.GarageId == g.Id) < g.Capacity);
            if (garage == null) { Notify.Show("Buy a garage with a free vehicle slot first. " + Cfg.GarageKey + " opens garages."); return; }
            if (Wallet.Cash < offer.Price) { Notify.Show("Not enough cash."); return; }
            if (World.GetNearbyVehicles(delivery, 5).Length > 0 || World.GetNearbyPeds(delivery, 2).Length > 0) { Notify.Show("Delivery area blocked. Clear it before purchasing."); return; }
            var model = new Model(offer.Model);
            if (!model.IsValid || !model.IsInCdImage || !model.IsVehicle || !(model.IsCar || model.IsBike || model.IsQuadBike) || !model.Request(3000))
            { Notify.Show("Vehicle model unavailable; no payment taken."); return; }
            Vehicle car = null; bool paid = false; SavedVehicle record = null; string profile = Profile.Id;
            try
            {
                if (!Nearby() || Profile.Detect() != profile) return;
                car = World.CreateVehicle(model, delivery, 70);
                if (car == null || !car.Exists()) { Notify.Show("Vehicle creation failed; no payment taken."); return; }
                string plate = null;
                var worldKeys = new HashSet<string>(World.GetAllVehicles().Select(v => VehicleIdentity.Key(v.Model.Hash, Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, v))));
                for (int attempt = 0; attempt < 100; attempt++)
                {
                    string candidate = "RP" + random.Next(100000, 1000000);
                    if (data.FindIdentity(model.Hash, candidate) == null && !worldKeys.Contains(VehicleIdentity.Key(model.Hash, candidate)) && !data.Tanks.ContainsKey(VehicleIdentity.Key(model.Hash, candidate))) { plate = candidate; break; }
                }
                if (plate == null) throw new InvalidOperationException("Unable to allocate a unique plate.");
                Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, car, plate);
                float capacity = car.PetrolTankVolume; car.FuelLevel = capacity > 0 && capacity < 10000 ? capacity : 60;
                Function.Call(Hash.SET_VEHICLE_ENGINE_ON, car, false, true, false);
                record = VehicleSnapshot.Capture(car); record.GarageId = garage.Id; record.Name = offer.Name ?? offer.Model;
                if (!data.CanStore(garage.Id, garage.Capacity, record) || !Wallet.Debit(offer.Price)) { car.Delete(); Notify.Show("Purchase cancelled; funds or garage slot unavailable."); return; }
                paid = true; data.Vehicles.Add(record);
                try { Wallet.Save(); }
                catch { data.Vehicles.Remove(record); Wallet.Credit(offer.Price); paid = false; throw; }
                car.IsPersistent = true; created[VehicleIdentity.Key(record.Model, record.Plate)] = car;
                ach.Bump("cars_bought"); ach.Save(); menu.Visible = false;
                Notify.Show("Purchased " + record.Name + ". Collect it outside; assigned garage: " + garage.Name);
            }
            catch
            {
                if (!paid && car != null && car.Exists()) car.Delete();
                throw;
            }
            finally { model.MarkAsNoLongerNeeded(); }
        }
        public void Tick()
        {
            if (!Cfg.EnableGarages || Game.IsMissionActive) { menu.Visible = false; return; }
            if (menu.Visible && Game.Player.Character.Position.DistanceTo(entrance) > 30) menu.Visible = false;
            if (dirty && menu.Visible) Rebuild();
            if (Game.Player.Character.Position.DistanceTo(entrance) < 35)
                World.DrawMarker(MarkerType.Cylinder, entrance - new Vector3(0, 0, 1), Vector3.Zero, Vector3.Zero, new Vector3(2, 2, 1), Color.FromArgb(90, 80, 200, 100));
        }
        public void OnKey(KeyEventArgs e)
        {
            if (e.KeyCode != Cfg.DealerKey || !Cfg.EnableGarages || Game.IsMissionActive) return;
            if (menu.Visible) menu.Visible = false;
            else if (!pool.AreAnyVisible) { Rebuild(); menu.Visible = true; }
        }
        void ReleaseCreated()
        {
            foreach (var pair in created) if (pair.Value != null && pair.Value.Exists() && VehicleIdentity.Key(pair.Value.Model.Hash, Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, pair.Value)) == pair.Key) pair.Value.MarkAsNoLongerNeeded();
            created.Clear();
        }
        public void Reload() { menu.Visible = false; ReleaseCreated(); data = Wallet.Transport; }
        public void Save() { }
        public void Dispose() { menu.Visible = false; ReleaseCreated(); if (blip != null && blip.Exists()) blip.Delete(); }
    }
}
