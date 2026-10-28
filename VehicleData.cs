using System;
using System.Collections.Generic;
using System.Linq;

namespace RPCore
{
    public static class VehicleIdentity
    {
        public static string Key(int model, string plate)
        {
            return string.IsNullOrWhiteSpace(plate) ? null : model + ":" + plate.Trim().ToUpperInvariant();
        }
    }

    public class FuelTank : ExtensibleData
    {
        public float Liters;
        public float Capacity = 60;
        public double DistanceKm;

        public void Validate()
        {
            if (!Finite(Capacity) || Capacity <= 0 || Capacity > 10000 || !Finite(Liters) || Liters < 0 || Liters > Capacity + .01f || double.IsNaN(DistanceKm) || double.IsInfinity(DistanceKm) || DistanceKm < 0)
                throw new InvalidOperationException("Invalid saved fuel tank; original data must be preserved.");
        }
        static bool Finite(float n) { return !float.IsNaN(n) && !float.IsInfinity(n); }
        public void Drive(float seconds, float metersPerSecond, float litersPer100Km, float idleLitersPerHour)
        {
            Validate();
            if (!Finite(seconds) || !Finite(metersPerSecond) || !Finite(litersPer100Km) || !Finite(idleLitersPerHour) || seconds <= 0 || metersPerSecond < 0 || litersPer100Km < 0 || idleLitersPerHour < 0) return;
            seconds = Math.Min(seconds, 1); // no drain for time spent loading, paused or stalled
            double km = metersPerSecond * (double)seconds / 1000;
            DistanceKm += km;
            Liters = (float)Math.Max(0, Liters - km * litersPer100Km / 100 - idleLitersPerHour * seconds / 3600);
        }
        public static FuelQuote Quote(float space, float requested, int cash, int pricePerLiter)
        {
            if (!Finite(space) || !Finite(requested) || space <= 0 || space > 10000 || requested <= 0 || cash <= 0 || pricePerLiter <= 0) return new FuelQuote();
            decimal liters = Math.Min(Math.Min((decimal)space, (decimal)requested), (decimal)cash / pricePerLiter);
            liters = Math.Floor(liters * 100) / 100;
            if (liters < .01m) return new FuelQuote();
            return new FuelQuote { Liters = (float)liters, Cost = (int)Math.Ceiling(liters * pricePerLiter) };
        }
    }
    public class FuelQuote { public float Liters; public int Cost; }

    public class SavedVehicle : ExtensibleData
    {
        public string Id = Guid.NewGuid().ToString("N"), GarageId, Plate, Name;
        public int Model, PlateStyle, PrimaryColor, SecondaryColor, PearlColor, WheelColor, WheelType, WindowTint, Livery;
        public int DashboardColor, InteriorColor;
        public float EngineHealth, BodyHealth, TankHealth, Dirt, Fuel;
        public float X, Y, Z, Heading;
        public bool Stored, PendingRemoval;
        public bool? TiresCanBurst;
        public int[] CustomPrimary, CustomSecondary, NeonColor, SmokeColor;
        public Dictionary<int, int> Mods = new Dictionary<int, int>();
        public Dictionary<int, bool> Variations = new Dictionary<int, bool>();
        public Dictionary<int, bool> Toggles = new Dictionary<int, bool>();
        public Dictionary<int, bool> Extras = new Dictionary<int, bool>();
        public Dictionary<int, bool> Neons = new Dictionary<int, bool>();
        public List<int> BurstTires = new List<int>(), BrokenWindows = new List<int>(), BrokenDoors = new List<int>();
    }

    public class TransportData : ExtensibleData
    {
        public Dictionary<string,int> NeonEquipment;
        public Dictionary<string, FuelTank> Tanks = new Dictionary<string, FuelTank>();
        public HashSet<string> Garages = new HashSet<string>();
        public List<SavedVehicle> Vehicles = new List<SavedVehicle>();

        public void Validate()
        {
            if (Tanks == null || Garages == null || Vehicles == null) throw new InvalidOperationException("Incomplete vehicle save; original data preserved.");
            foreach (var tank in Tanks.Values) { if (tank == null) throw new InvalidOperationException("Missing saved fuel tank."); tank.Validate(); }
            if (Vehicles.Any(v => v == null || string.IsNullOrEmpty(v.Id)) || Vehicles.GroupBy(v => v.Id).Any(g => g.Count() > 1)) throw new InvalidOperationException("Duplicate or missing vehicle IDs.");
        }
        public SavedVehicle FindIdentity(int model, string plate)
        {
            string key = VehicleIdentity.Key(model, plate);
            return key == null ? null : Vehicles.FirstOrDefault(v => VehicleIdentity.Key(v.Model, v.Plate) == key);
        }
        public bool CanStore(string garageId, int capacity, SavedVehicle vehicle)
        {
            if (vehicle == null || !Garages.Contains(garageId) || capacity <= 0 || VehicleIdentity.Key(vehicle.Model, vehicle.Plate) == null) return false;
            var duplicate = FindIdentity(vehicle.Model, vehicle.Plate);
            if (duplicate != null && duplicate.Id != vehicle.Id) return false;
            return Vehicles.Count(v => v.GarageId == garageId && v.Id != vehicle.Id) < capacity;
        }
    }
}
