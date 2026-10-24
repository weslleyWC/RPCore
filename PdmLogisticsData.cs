using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RPCore
{
    public sealed class PdmLoadSlot
    {
        public float X, Y, Z;
        public bool Valid => new[] { X, Y, Z }.All(n => !float.IsNaN(n) && !float.IsInfinity(n)) && Math.Abs(X) <= 5 && Math.Abs(Y) <= 10 && Z >= .1f && Z <= 5;
    }
    public sealed class PdmLogisticsOptions : ExtensibleData
    {
        public JobPoint Trailer, Dock, Return, Showroom;
        public string TruckModel = "packer", TrailerModel = "trflat";
        public int UnloadSeconds = 5, TimeLimitSeconds = 1800;
        public List<PdmLoadSlot> Slots = new List<PdmLoadSlot>();
        [JsonIgnore] public bool Valid => new[] { Trailer, Dock, Return, Showroom }.All(p => p != null && p.Valid) && !string.IsNullOrWhiteSpace(TruckModel) && !string.IsNullOrWhiteSpace(TrailerModel) && TruckModel.Length <= 60 && TrailerModel.Length <= 60
            && UnloadSeconds >= 2 && UnloadSeconds <= 15 && TimeLimitSeconds >= 300 && TimeLimitSeconds <= 3600 && Slots != null && Slots.Count == 2 && Slots.All(s => s != null && s.Valid) && Math.Abs(Slots[0].Y - Slots[1].Y) >= 5;
    }
    public static class PdmLogisticsDefaults
    {
        public static PdmLogisticsOptions Load()
        {
            using (var s = typeof(PdmLogisticsDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.PdmLogistics.json"))
            using (var r = new System.IO.StreamReader(s)) return JsonConvert.DeserializeObject<PdmLogisticsOptions>(r.ReadToEnd());
        }
    }
    public sealed class PdmEntityRef : ExtensibleData
    {
        public string Model, Plate; public int Handle;
        [JsonIgnore] public bool Valid => Handle > 0 && !string.IsNullOrWhiteSpace(Model) && Model.Length <= 60 && !string.IsNullOrWhiteSpace(Plate) && Plate.Length <= 8;
    }
    public sealed class PdmCargoRef : ExtensibleData
    {
        public string StockId; public int Handle;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(StockId) && Handle >= 0;
    }
    public sealed class PdmCollection : ExtensibleData
    {
        public string Id;
        public List<string> StockIds = new List<string>();
        public PdmEntityRef Truck, Trailer;
        public List<PdmCargoRef> Cargo = new List<PdmCargoRef>();
        public bool Arrived;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(Id) && Id.Length <= 100 && StockIds != null && StockIds.Count >= 1 && StockIds.Count <= 2 && StockIds.All(s => !string.IsNullOrWhiteSpace(s)) && StockIds.Distinct().Count() == StockIds.Count
            && Truck != null && Truck.Valid && Trailer != null && Trailer.Valid && Truck.Handle != Trailer.Handle && Cargo != null && Cargo.Count == StockIds.Count && Cargo.All(c => c != null && c.Valid && StockIds.Contains(c.StockId)) && Cargo.Select(c => c.StockId).Distinct().Count() == Cargo.Count && Cargo.Where(c => c.Handle > 0).Select(c => c.Handle).Distinct().Count() == Cargo.Count(c => c.Handle > 0) && Cargo.All(c => c.Handle == 0 || c.Handle != Truck.Handle && c.Handle != Trailer.Handle) && (!Arrived || Cargo.All(c => c.Handle > 0));
    }
    public static class PdmCollectionTransactions
    {
        public static bool Begin(WalletData w, string id, List<string> stockIds, PdmEntityRef truck, PdmEntityRef trailer, Action save, out bool changed)
        {
            changed = false;
            if (string.IsNullOrWhiteSpace(id) || id.Length > 100 || stockIds == null || stockIds.Count < 1 || stockIds.Count > 2 || stockIds.Distinct().Count() != stockIds.Count || truck == null || !truck.Valid || trailer == null || !trailer.Valid || truck.Handle == trailer.Handle) return false;
            return PdmTransactions.Apply(w, id + ":collection", 0, d => {
                if (!d.Owned || d.Active != null || d.Collection != null || d.SafeTransfer != null || d.Repo != null || stockIds.Any(s => !d.Stock.Any(v => v.Id == s && v.AwaitingCollection && v.Engine >= 300 && v.Body >= 300))) return false;
                d.Collection = new PdmCollection { Id = id, StockIds = new List<string>(stockIds), Truck = truck, Trailer = trailer, Cargo = stockIds.Select(s => new PdmCargoRef { StockId = s }).ToList() }; return true;
            }, save, out changed);
        }
        public static bool Register(WalletData w, string id, string stockId, int handle, Action save, out bool changed)
        {
            changed = false; if (handle <= 0 || string.IsNullOrWhiteSpace(id) || id.Length > 100) return false;
            return PdmTransactions.Apply(w, id + ":cargo:" + stockId, 0, d => {
                var c = d.Collection; var item = c?.Cargo.Find(x => x.StockId == stockId);
                if (c == null || c.Id != id || c.Arrived || item == null || item.Handle != 0 || handle == c.Truck.Handle || handle == c.Trailer.Handle || c.Cargo.Any(x => x.Handle == handle)) return false;
                item.Handle = handle; return true;
            }, save, out changed);
        }
        public static bool Remember(WalletData w, string id, Dictionary<string, int[]> health, string receipt, Action save, out bool changed)
        {
            changed = false; if (health == null || health.Any(p => p.Value == null || p.Value.Length != 2 || p.Value.Any(n => n < 0 || n > 1000))) return false;
            return PdmTransactions.Apply(w, receipt, 0, d => {
                var c = d.Collection; if (c == null || c.Id != id || health.Keys.Any(k => !c.StockIds.Contains(k))) return false;
                foreach (var pair in health) { var v = d.Stock.Find(s => s.Id == pair.Key); if (v == null) return false; v.Engine = Math.Min(v.Engine, pair.Value[0]); v.Body = Math.Min(v.Body, pair.Value[1]); } return true;
            }, save, out changed);
        }
        public static bool Arrive(WalletData w, string id, Action save, out bool changed)
        {
            return PdmTransactions.Apply(w, id + ":arrived", 0, d => { var c = d.Collection; if (c == null || c.Id != id || c.Arrived || c.Cargo.Any(x => x.Handle <= 0)) return false; c.Arrived = true; return true; }, save, out changed);
        }
        public static bool Complete(WalletData w, string id, Action save, out bool changed)
        {
            return PdmTransactions.Apply(w, id + ":received", 0, d => { var c = d.Collection; if (c == null || c.Id != id || !c.Arrived) return false; foreach (string s in c.StockIds) d.Stock.Find(v => v.Id == s).AwaitingCollection = false; d.Collection = null; return true; }, save, out changed);
        }
        public static bool Cancel(WalletData w, string id, Action save, out bool changed)
        {
            return PdmTransactions.Apply(w, id + ":collection-cancel", 0, d => { if (d.Collection == null || d.Collection.Id != id || d.Collection.Arrived) return false; d.Collection = null; return true; }, save, out changed);
        }
    }
    public enum PdmHaulStage { Coupling, Loading, Returning, Unloading, Confirming, Complete, Cancelled }
    public sealed class PdmHaulRun
    {
        public readonly string Id; public readonly int Total;
        public int Loaded { get; private set; }
        public PdmHaulStage Stage { get; private set; } = PdmHaulStage.Coupling;
        readonly int duration; int began;
        public PdmHaulRun(string id, int total, int seconds) { if (string.IsNullOrWhiteSpace(id) || total < 1 || total > 2 || seconds < 2 || seconds > 15) throw new ArgumentException(); Id = id; Total = total; duration = seconds * 1000; }
        public void Coupled(bool coupled) { if (Stage == PdmHaulStage.Coupling && coupled) Stage = PdmHaulStage.Loading; }
        public bool Load(bool eligible, Func<bool> attach) { if (Stage != PdmHaulStage.Loading || !eligible || !attach()) return false; Loaded++; if (Loaded == Total) Stage = PdmHaulStage.Returning; return true; }
        public bool Unload(int now, bool eligible) { if (Stage != PdmHaulStage.Returning || !eligible) return false; began = now; Stage = PdmHaulStage.Unloading; return true; }
        public void Interrupt() { if (Stage == PdmHaulStage.Unloading) Stage = PdmHaulStage.Returning; }
        public bool Arrive(int now, bool eligible, Func<bool> save) { if (Stage != PdmHaulStage.Unloading) return false; if (!eligible) { Interrupt(); return false; } if (unchecked(now - began) < duration || !save()) return false; Stage = PdmHaulStage.Confirming; return true; }
        public bool Complete(Func<bool> save) { if (Stage != PdmHaulStage.Confirming || !save()) return false; Stage = PdmHaulStage.Complete; return true; }
        public void Cancel() { if (Stage != PdmHaulStage.Complete && Stage != PdmHaulStage.Confirming) Stage = PdmHaulStage.Cancelled; }
    }
}
