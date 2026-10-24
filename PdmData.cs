using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RPCore
{
    public sealed class PdmOffer
    {
        public string Id, Model, Name;
        public int Cost, Price;
    }
    public sealed class PdmOptions : ExtensibleData
    {
        public JobPoint Office, Spawn;
        public int PurchasePrice = 25000, Capacity = 6, RepairPrice = 1000, HandoverSeconds = 3, TimeLimitSeconds = 600;
        public List<PdmOffer> Catalog = new List<PdmOffer>();
        public List<JobPoint> Customers = new List<JobPoint>();
        [JsonIgnore] public bool Valid => Office != null && Office.Valid && Spawn != null && Spawn.Valid && PurchasePrice >= 0 && PurchasePrice <= 5000000 && Capacity >= 1 && Capacity <= 20 && RepairPrice >= 1 && RepairPrice <= 100000 && HandoverSeconds >= 2 && HandoverSeconds <= 10 && TimeLimitSeconds >= 120 && TimeLimitSeconds <= 1800
            && Catalog != null && Catalog.Count > 0 && Catalog.Count <= 50 && Catalog.All(p => p != null && !string.IsNullOrWhiteSpace(p.Id) && !string.IsNullOrWhiteSpace(p.Model) && p.Model.Length <= 60 && !string.IsNullOrWhiteSpace(p.Name) && p.Cost >= 1 && p.Cost <= 1000000 && p.Price >= p.Cost && p.Price <= 2000000) && Catalog.Select(p => p.Id).Distinct().Count() == Catalog.Count
            && Customers != null && Customers.Count > 0 && Customers.Count <= 30 && Customers.All(p => p != null && p.Valid);
    }
    public static class PdmDefaults
    {
        public static PdmOptions Load()
        {
            using (var s = typeof(PdmDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.Pdm.json"))
            using (var r = new System.IO.StreamReader(s)) return JsonConvert.DeserializeObject<PdmOptions>(r.ReadToEnd());
        }
    }
    public sealed class PdmStock : ExtensibleData
    {
        public string Id, Model, Name, Plate;
        public int Cost, Price, Engine = 1000, Body = 1000;
        public bool AwaitingCollection;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(Id) && Id.Length <= 100 && !string.IsNullOrWhiteSpace(Model) && !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Plate) && Plate.Length <= 8 && Cost > 0 && Cost <= 1000000 && Price >= Cost && Price <= 2000000 && Engine >= 0 && Engine <= 1000 && Body >= 0 && Body <= 1000;
    }
    public sealed class PdmOperation : ExtensibleData
    {
        public string Id, StockId;
        public int Destination, VehicleHandle;
        public bool Financed;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(Id) && Id.Length <= 100 && !string.IsNullOrWhiteSpace(StockId) && Destination >= 0 && Destination < 30 && VehicleHandle > 0;
    }
    public sealed class PdmEntry : ExtensibleData { public string Description; public int Amount, Balance; }
    public sealed class PdmData : ExtensibleData
    {
        public bool Owned;
        public int Cash, AcquisitionPrice, Bought, Sold;
        public long Invested, Withdrawn, StockSpent, SoldCost, Revenue, Repairs;
        public List<PdmStock> Stock = new List<PdmStock>();
        public PdmOperation Active;
        public PdmCollection Collection;
        public PdmSafeTransfer SafeTransfer;
        public List<PdmLoan> Loans = new List<PdmLoan>();
        public PdmStaffData Staff = new PdmStaffData();
        public List<PdmEntry> Ledger = new List<PdmEntry>();
        public List<string> Receipts = new List<string>();
        [JsonIgnore] public bool Valid => Cash >= 0 && AcquisitionPrice >= 0 && Bought >= 0 && Sold >= 0 && Invested >= 0 && Withdrawn >= 0 && StockSpent >= 0 && SoldCost >= 0 && Revenue >= 0 && Repairs >= 0 && Stock != null && Stock.Count <= 20 && Stock.All(s => s != null && s.Valid) && Stock.Select(s => s.Id).Distinct().Count() == Stock.Count && Stock.Select(s => s.Plate).Distinct().Count() == Stock.Count
            && (Active == null || Active.Valid && Stock.Any(s => s.Id == Active.StockId)) && (Collection == null || Active == null && Collection.Valid && Collection.StockIds.All(id => Stock.Any(s => s.Id == id && s.AwaitingCollection))) && (SafeTransfer == null || SafeTransfer.Valid && Owned && Active == null && Collection == null && (long)Cash + SafeTransfer.Amount <= int.MaxValue) && Loans != null && Loans.Count <= 42 && Loans.All(l => l != null && l.Valid) && Loans.Select(l => l.Id).Distinct().Count() == Loans.Count && Loans.Count(l => !l.Complete) <= 10 && Staff != null && Staff.Valid && Ledger != null && Receipts != null;
    }
    public static class PdmTransactions
    {
        static bool Id(string id) => !string.IsNullOrWhiteSpace(id) && id.Length <= 100;
        internal static void Entry(PdmData d, string text, int amount) { d.Ledger.Add(new PdmEntry { Description = text, Amount = amount, Balance = d.Cash }); if (d.Ledger.Count > 64) d.Ledger.RemoveRange(0, d.Ledger.Count - 64); }
        internal static bool Apply(WalletData w, string id, int bankDelta, Func<PdmData, bool> mutate, Action save, out bool changed)
        {
            changed = false;
            if (w == null || w.Bank < 0 || string.IsNullOrWhiteSpace(id) || id.Length > 160 || save == null || (w.Pdm != null && !w.Pdm.Valid)) return false;
            if (w.Pdm?.Receipts.Contains(id) == true) return true;
            if ((long)w.Bank + bankDelta < 0 || (long)w.Bank + bankDelta > int.MaxValue) return false;
            var old = w.Pdm; var next = old == null ? new PdmData() : JsonConvert.DeserializeObject<PdmData>(JsonConvert.SerializeObject(old));
            if (!mutate(next) || !next.Valid) return false;
            next.Receipts.Add(id); if (next.Receipts.Count > 512) next.Receipts.RemoveRange(0, next.Receipts.Count - 512);
            int bank = w.Bank;
            try { w.Pdm = next; w.Bank = bank + bankDelta; save(); changed = true; return true; }
            catch { w.Pdm = old; w.Bank = bank; throw; }
        }
        public static bool Purchase(WalletData w, PdmOptions c, string receipt, Action save, out bool changed)
        {
            changed = false; if (c == null || !c.Valid) return false;
            return Apply(w, receipt, -c.PurchasePrice, d => { if (d.Owned) return false; d.Owned = true; d.AcquisitionPrice = c.PurchasePrice; Entry(d, "Aquisicao da empresa (banco)", 0); return true; }, save, out changed);
        }
        public static bool Transfer(WalletData w, int amount, bool deposit, string receipt, Action save, out bool changed)
        {
            changed = false; if (amount < 1 || amount > 1000000) return false;
            return Apply(w, receipt, deposit ? -amount : amount, d => {
                if (!d.Owned) return false;
                if (deposit) { if (d.Cash > int.MaxValue - amount || d.Invested > long.MaxValue - amount) return false; d.Cash += amount; d.Invested += amount; }
                else { if (d.Cash < amount || d.Withdrawn > long.MaxValue - amount) return false; d.Cash -= amount; d.Withdrawn += amount; }
                Entry(d, deposit ? "Aporte de capital" : "Saque do caixa", deposit ? amount : -amount); return true;
            }, save, out changed);
        }
        public static bool Buy(WalletData w, PdmOptions c, string offerId, string stockId, string receipt, Action save, out bool changed, bool awaitingCollection = false)
        {
            changed = false; if (c == null || !c.Valid || !Id(stockId)) return false; var p = c.Catalog.Find(x => x.Id == offerId); if (p == null) return false;
            return Apply(w, receipt, 0, d => {
                if (!d.Owned || d.Active != null || d.Collection != null || d.SafeTransfer != null || d.Cash < p.Cost || d.Stock.Count >= c.Capacity || (d.Stock.Any(s => s.Id == stockId) || d.Loans.Any(l => l.Asset.Id == stockId)) || d.Bought == int.MaxValue || d.StockSpent > long.MaxValue - p.Cost) return false;
                string plate = "PDM" + stockId.Replace("-", "").Substring(0, Math.Min(5, stockId.Replace("-", "").Length)).ToUpperInvariant();
                if (d.Stock.Any(s => s.Plate == plate) || d.Loans.Any(l => l.Asset.Plate == plate)) return false;
                d.Stock.Add(new PdmStock { Id = stockId, Model = p.Model, Name = p.Name, Plate = plate, Cost = p.Cost, Price = p.Price, AwaitingCollection = awaitingCollection });
                d.Cash -= p.Cost; d.StockSpent += p.Cost; d.Bought++; Entry(d, "Compra: " + p.Name, -p.Cost); return true;
            }, save, out changed);
        }
        public static bool Repair(WalletData w, PdmOptions c, string stockId, string receipt, Action save, out bool changed)
        {
            changed = false; if (c == null || !c.Valid) return false;
            return Apply(w, receipt, 0, d => {
                var v = d.Stock.Find(s => s.Id == stockId); int repairPrice = PdmStaffTransactions.RepairPrice(d, c.RepairPrice);
                if (!d.Owned || d.Active != null || d.Collection != null || d.SafeTransfer != null || v == null || (v.Engine == 1000 && v.Body == 1000) || d.Cash < repairPrice || d.Repairs > long.MaxValue - repairPrice) return false;
                v.Engine = v.Body = 1000; d.Cash -= repairPrice; d.Repairs += repairPrice; Entry(d, "Reparo: " + v.Name, -repairPrice); return true;
            }, save, out changed);
        }
        public static bool Begin(WalletData w, PdmOptions c, string stockId, string operationId, int destination, int handle, Action save, out bool changed)
            => Begin(w, c, stockId, operationId, destination, handle, save, out changed, false);
        public static bool Begin(WalletData w, PdmOptions c, string stockId, string operationId, int destination, int handle, Action save, out bool changed, bool financed)
        {
            changed = false; if (c == null || !c.Valid || !Id(operationId) || destination < 0 || destination >= c.Customers.Count || handle <= 0) return false;
            return Apply(w, operationId + ":begin", 0, d => {
                var v = d.Stock.Find(s => s.Id == stockId); if (!d.Owned || d.Active != null || d.Collection != null || d.SafeTransfer != null || v == null || v.AwaitingCollection || v.Engine < 300 || v.Body < 300) return false;
                d.Active = new PdmOperation { Id = operationId, StockId = stockId, Destination = destination, VehicleHandle = handle, Financed = financed }; return true;
            }, save, out changed);
        }
        static bool Health(int engine, int body) => engine >= 0 && engine <= 1000 && body >= 0 && body <= 1000;
        public static int Quote(PdmStock v, int engine, int body) => v == null || !v.Valid || !Health(engine, body) ? 0 : (int)((long)v.Price * (750 + Math.Min(v.Body, body) / 4) / 1000);
        public static bool Remember(WalletData w, string operationId, int engine, int body, string receipt, Action save, out bool changed)
        {
            changed = false; if (!Id(operationId) || !Health(engine, body)) return false;
            return Apply(w, receipt, 0, d => { if (d.Active == null || d.Active.Id != operationId) return false; var v = d.Stock.Find(s => s.Id == d.Active.StockId); if (v == null) return false; v.Engine = Math.Min(v.Engine, engine); v.Body = Math.Min(v.Body, body); return true; }, save, out changed);
        }
        public static bool Cancel(WalletData w, string operationId, int engine, int body, Action save, out bool changed)
        {
            changed = false; if (!Id(operationId) || !Health(engine, body)) return false;
            return Apply(w, operationId + ":cancel", 0, d => {
                if (d.Active == null || d.Active.Id != operationId) return false;
                var v = d.Stock.Find(s => s.Id == d.Active.StockId); if (v == null) return false;
                v.Engine = Math.Min(v.Engine, engine); v.Body = Math.Min(v.Body, body); d.Active = null; return true;
            }, save, out changed);
        }
        public static bool Sell(WalletData w, string operationId, int engine, int body, Action save, out bool changed)
        {
            changed = false; if (!Id(operationId) || !Health(engine, body) || engine < 100) return false;
            return Apply(w, operationId + ":sale", 0, d => {
                if (!d.Owned || d.Active == null || d.Active.Id != operationId || d.Active.Financed) return false;
                var v = d.Stock.Find(s => s.Id == d.Active.StockId); int quote = PdmStaffTransactions.Quote(d, v, engine, body);
                if (v == null || quote <= 0 || d.Cash > int.MaxValue - quote || d.Revenue > long.MaxValue - quote || d.SoldCost > long.MaxValue - v.Cost || d.Sold == int.MaxValue) return false;
                d.Cash += quote; d.Revenue += quote; d.SoldCost += v.Cost; d.Sold++; d.Stock.Remove(v); d.Active = null; Entry(d, "Venda: " + v.Name, quote); return true;
            }, save, out changed);
        }
    }
    public enum PdmStage { Collect, Drive, Handover, Complete, Cancelled }
    public sealed class PdmRun
    {
        public readonly string Id, StockId;
        public PdmStage Stage { get; private set; } = PdmStage.Collect;
        readonly int duration; int began;
        public PdmRun(string id, string stockId, int seconds) { if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(stockId) || seconds < 2 || seconds > 10) throw new ArgumentException(); Id = id; StockId = stockId; duration = seconds * 1000; }
        public void Enter(bool driving) { if (Stage == PdmStage.Collect && driving) Stage = PdmStage.Drive; }
        public bool Handover(int now, bool eligible) { if (Stage != PdmStage.Drive || !eligible) return false; began = now; Stage = PdmStage.Handover; return true; }
        public bool Commit(int now, bool eligible, Func<bool> sale)
        {
            if (Stage != PdmStage.Handover) return false;
            if (!eligible) { Stage = PdmStage.Drive; return false; }
            if (unchecked(now - began) < duration || !sale()) return false; Stage = PdmStage.Complete; return true;
        }
        public void Cancel() { if (Stage != PdmStage.Complete) Stage = PdmStage.Cancelled; }
    }
}
