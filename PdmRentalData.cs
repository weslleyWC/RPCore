using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class PdmRentalOptions : ExtensibleData
    {
        public JobPoint Delivery, Worker, Return;
        public List<JobPoint> Pickups = new List<JobPoint>();
        public List<string> Models = new List<string>();
        public string WorkerModel = "cs_carbuyer";
        public int PricePercent = 95, UnloadSeconds = 5, ReturnSeconds = 3, TimeLimitSeconds = 1800;
        [JsonIgnore] public bool Valid => new[] { Delivery, Worker, Return }.All(p => p != null && p.Valid) && Pickups != null && Pickups.Count == 2 && Pickups.All(p => p != null && p.Valid)
            && Models != null && Models.Count >= 1 && Models.Count <= 50 && Models.All(m => !string.IsNullOrWhiteSpace(m) && m.Length <= 60) && Models.Distinct(StringComparer.OrdinalIgnoreCase).Count() == Models.Count
            && !string.IsNullOrWhiteSpace(WorkerModel) && WorkerModel.Length <= 60 && PricePercent >= 80 && PricePercent <= 120 && UnloadSeconds >= 2 && UnloadSeconds <= 15 && ReturnSeconds >= 2 && ReturnSeconds <= 15 && TimeLimitSeconds >= 300 && TimeLimitSeconds <= 3600;
    }
    public static class PdmRentalDefaults
    {
        public static PdmRentalOptions Load()
        { using (var s = typeof(PdmRentalDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.PdmRental.json")) using (var r = new System.IO.StreamReader(s)) return JsonConvert.DeserializeObject<PdmRentalOptions>(r.ReadToEnd()); }
    }
    public sealed class PdmRentalCar : ExtensibleData
    {
        public string StockId;
        public int Handle, Price;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(StockId) && Handle >= 0 && Price >= 1 && Price <= 2400000;
    }
    public sealed class PdmRentalOperation : ExtensibleData
    {
        public string Id;
        public int PricePercent;
        public PdmEntityRef Truck, Trailer;
        public List<PdmRentalCar> Cars = new List<PdmRentalCar>();
        public bool Delivered, Returned;
        [JsonIgnore] public int Amount => Cars.Sum(c => c.Price);
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(Id) && Id.Length <= 100 && PricePercent >= 80 && PricePercent <= 120 && Truck != null && Truck.Valid && Trailer != null && Trailer.Valid && Truck.Handle != Trailer.Handle
            && Cars != null && Cars.Count == 2 && Cars.All(c => c != null && c.Valid && c.Handle != Truck.Handle && c.Handle != Trailer.Handle) && Cars.Select(c => c.StockId).Distinct().Count() == 2
            && Cars.Where(c => c.Handle > 0).Select(c => c.Handle).Distinct().Count() == Cars.Count(c => c.Handle > 0) && (!Delivered || Cars.All(c => c.Handle > 0)) && (!Returned || Delivered);
    }
    public static class PdmRentalTransactions
    {
        public static int Quote(PdmStock stock, int rate) => rate < 80 || rate > 120 ? 0 : (int)((long)PdmTransactions.Quote(stock, stock?.Engine ?? 0, stock?.Body ?? 0) * rate / 100);
        public static bool Begin(WalletData w, PdmRentalOptions c, string id, List<string> ids, PdmEntityRef truck, PdmEntityRef trailer, Action save, out bool changed)
        {
            changed = false; if (c == null || !c.Valid || string.IsNullOrWhiteSpace(id) || id.Length > 100 || ids == null || ids.Count != 2 || ids.Distinct().Count() != 2 || truck == null || !truck.Valid || trailer == null || !trailer.Valid || truck.Handle == trailer.Handle) return false;
            return PdmTransactions.Apply(w, id + ":rental", 0, d => {
                if (!d.Owned || d.Active != null || d.Collection != null || d.Repo != null || d.SafeTransfer != null || d.Rental != null || d.ShowroomSale != null) return false;
                var cars = ids.Select(x => d.Stock.Find(s => s.Id == x)).ToList();
                if (cars.Any(s => s == null || d.Showroom.Any(e => e.StockId == s.Id) || s.AwaitingCollection || s.Engine < 300 || s.Body < 300 || !c.Models.Contains(s.Model, StringComparer.OrdinalIgnoreCase) || Quote(s, c.PricePercent) < 1) || !string.Equals(cars[0].Model, cars[1].Model, StringComparison.OrdinalIgnoreCase)) return false;
                d.Rental = new PdmRentalOperation { Id = id, PricePercent = c.PricePercent, Truck = truck, Trailer = trailer, Cars = cars.Select(s => new PdmRentalCar { StockId = s.Id, Price = Quote(s, c.PricePercent) }).ToList() };
                d.Rental = JsonConvert.DeserializeObject<PdmRentalOperation>(JsonConvert.SerializeObject(d.Rental)); return true;
            }, save, out changed);
        }
        public static bool Register(WalletData w, string id, string stockId, int handle, Action save, out bool changed)
        {
            changed = false; if (handle <= 0) return false;
            return PdmTransactions.Apply(w, id + ":rental-car:" + stockId, 0, d => { var a = d.Rental; var car = a?.Cars.Find(c => c.StockId == stockId); if (a == null || a.Id != id || a.Delivered || car == null || car.Handle > 0 || handle == a.Truck.Handle || handle == a.Trailer.Handle || a.Cars.Any(c => c.Handle == handle)) return false; car.Handle = handle; return true; }, save, out changed);
        }
        public static bool Remember(WalletData w, string id, Dictionary<string, int[]> health, string receipt, Action save, out bool changed)
        {
            changed = false; if (health == null || health.Any(p => p.Value == null || p.Value.Length != 2 || p.Value.Any(n => n < 0 || n > 1000))) return false;
            return PdmTransactions.Apply(w, receipt, 0, d => {
                var a = d.Rental; if (a == null || a.Id != id || a.Delivered || health.Keys.Any(k => !a.Cars.Any(c => c.StockId == k))) return false;
                foreach (var p in health) { var s = d.Stock.Find(x => x.Id == p.Key); s.Engine = Math.Min(s.Engine, p.Value[0]); s.Body = Math.Min(s.Body, p.Value[1]); }
                return true;
            }, save, out changed);
        }
        public static bool Deliver(WalletData w, string id, Action save, out bool changed)
        {
            return PdmTransactions.Apply(w, id + ":rental-delivered", 0, d => {
                var a = d.Rental; if (a == null || a.Id != id || a.Delivered || a.Cars.Any(c => c.Handle <= 0)) return false;
                foreach (var c in a.Cars) { var stock = d.Stock.Find(s => s.Id == c.StockId); if (stock.Engine < 100) return false; c.Price = Math.Min(c.Price, Quote(stock, a.PricePercent)); if (c.Price < 1) return false; }
                a.Delivered = true; return true;
            }, save, out changed);
        }
        public static bool Return(WalletData w, string id, Action save, out bool changed)
        { return PdmTransactions.Apply(w, id + ":rental-returned", 0, d => { var a = d.Rental; if (a == null || a.Id != id || !a.Delivered || a.Returned) return false; a.Returned = true; return true; }, save, out changed); }
        public static bool Settle(WalletData w, string id, bool worldRecovery, Action save, out bool changed)
        {
            return PdmTransactions.Apply(w, id + ":rental-paid", 0, d => {
                var a = d.Rental; if (a == null || a.Id != id || !a.Delivered || !a.Returned && !worldRecovery) return false;
                int amount = a.Amount; long cost = a.Cars.Sum(c => (long)d.Stock.Find(s => s.Id == c.StockId).Cost);
                if (d.Cash > int.MaxValue - amount || d.Revenue > long.MaxValue - amount || d.SoldCost > long.MaxValue - cost || d.Sold > int.MaxValue - 2) return false;
                d.Cash += amount; d.Revenue += amount; d.SoldCost += cost; d.Sold += 2;
                d.Stock.RemoveAll(s => a.Cars.Any(c => c.StockId == s.Id)); d.Rental = null;
                PdmTransactions.Entry(d, worldRecovery ? "Locadora / recebimento recuperado" : "Locadora / dois carros entregues", amount); return true;
            }, save, out changed);
        }
        public static bool Cancel(WalletData w, string id, Action save, out bool changed)
        { return PdmTransactions.Apply(w, id + ":rental-cancel", 0, d => { var a = d.Rental; if (a == null || a.Id != id || a.Delivered) return false; d.Rental = null; return true; }, save, out changed); }
    }
    public enum PdmRentalStage { Coupling, Loading, Driving, Calling, Unloading, Confirming, Returning, Completing, Paying, Complete, Cancelled }
    public sealed class PdmRentalRun
    {
        public readonly string Id; public int Loaded { get; private set; }
        public PdmRentalStage Stage { get; private set; } = PdmRentalStage.Coupling;
        readonly int unloadTime, returnTime; int began;
        public PdmRentalRun(string id, int unloadSeconds, int returnSeconds)
        { if (string.IsNullOrWhiteSpace(id) || unloadSeconds < 2 || unloadSeconds > 15 || returnSeconds < 2 || returnSeconds > 15) throw new ArgumentException(); Id = id; unloadTime = unloadSeconds * 1000; returnTime = returnSeconds * 1000; }
        public static PdmRentalRun Resume(string id, int unloadSeconds, int returnSeconds, bool returned)
        { return new PdmRentalRun(id, unloadSeconds, returnSeconds) { Loaded = 2, Stage = returned ? PdmRentalStage.Paying : PdmRentalStage.Returning }; }
        public void Couple(bool coupled) { if (Stage == PdmRentalStage.Coupling && coupled) Stage = PdmRentalStage.Loading; }
        public bool Load(bool eligible, Func<bool> attach) { if (Stage != PdmRentalStage.Loading || !eligible || !attach()) return false; if (++Loaded == 2) Stage = PdmRentalStage.Driving; return true; }
        public bool Call(bool eligible) { if (Stage != PdmRentalStage.Driving || !eligible) return false; Stage = PdmRentalStage.Calling; return true; }
        public bool Unload(int now, bool eligible) { if (Stage != PdmRentalStage.Calling || !eligible) return false; began = now; Stage = PdmRentalStage.Unloading; return true; }
        public bool Deliver(int now, bool eligible, Func<bool> save)
        { if (Stage != PdmRentalStage.Unloading) return false; if (!eligible) { Stage = PdmRentalStage.Driving; return false; } if (unchecked(now - began) < unloadTime || !save()) return false; Stage = PdmRentalStage.Confirming; return true; }
        public bool BeginReturn(bool cargoRemoved) { if (Stage != PdmRentalStage.Confirming || !cargoRemoved) return false; Stage = PdmRentalStage.Returning; return true; }
        public bool Return(int now, bool eligible) { if (Stage != PdmRentalStage.Returning || !eligible) return false; began = now; Stage = PdmRentalStage.Completing; return true; }
        public bool Returned(int now, bool eligible, Func<bool> save)
        { if (Stage != PdmRentalStage.Completing) return false; if (!eligible) { Stage = PdmRentalStage.Returning; return false; } if (unchecked(now - began) < returnTime || !save()) return false; Stage = PdmRentalStage.Paying; return true; }
        public bool Pay(Func<bool> save) { if (Stage != PdmRentalStage.Paying || !save()) return false; Stage = PdmRentalStage.Complete; return true; }
        public bool Cancel(Func<bool> save)
        { if (Stage >= PdmRentalStage.Confirming || !save()) return false; Stage = PdmRentalStage.Cancelled; return true; }
    }
}
