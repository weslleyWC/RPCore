using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class PdmShowroomSlot
    {
        public string Id, Name;
        public JobPoint Point;
        public bool Interior, Enabled = true;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(Id) && Id.Length <= 40 && !string.IsNullOrWhiteSpace(Name) && Point != null && Point.Valid;
    }
    public sealed class PdmShowroomOptions : ExtensibleData
    {
        public List<PdmShowroomSlot> Slots = new List<PdmShowroomSlot>();
        public int HandoverSeconds = 3, BuyerTimeoutSeconds = 120;
        [JsonIgnore] public bool Valid => Slots != null && Slots.Count >= 1 && Slots.Count <= 24 && Slots.All(s => s != null && s.Valid) && Slots.Select(s => s.Id).Distinct().Count() == Slots.Count && HandoverSeconds >= 2 && HandoverSeconds <= 10 && BuyerTimeoutSeconds >= 60 && BuyerTimeoutSeconds <= 300;
    }
    public static class PdmShowroomDefaults
    {
        public static PdmShowroomOptions Load()
        { using (var s = typeof(PdmShowroomDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.PdmShowroom.json")) using (var r = new System.IO.StreamReader(s)) return JsonConvert.DeserializeObject<PdmShowroomOptions>(r.ReadToEnd()); }
    }
    public sealed class PdmShowroomEntry : ExtensibleData
    {
        public string SlotId, StockId;
        public int Handle;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(SlotId) && SlotId.Length <= 40 && !string.IsNullOrWhiteSpace(StockId) && Handle >= 0;
    }
    public sealed class PdmShowroomSale : ExtensibleData
    {
        public string Id, StockId;
        public int Handle, Price;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(Id) && Id.Length <= 100 && !string.IsNullOrWhiteSpace(StockId) && Handle > 0 && Price >= 1 && Price <= 3000000;
    }
    public static class PdmShowroomTransactions
    {
        public static bool Idle(PdmData d) => d.Owned && d.Active == null && d.Collection == null && d.SafeTransfer == null && d.Repo == null && d.Rental == null && d.ShowroomSale == null && d.PremisesService == null;
        public static bool Assign(WalletData w, PdmShowroomOptions c, string stockId, string slotId, int handle, string receipt, Action save, out bool changed)
        {
            changed = false; if (c == null || !c.Valid || handle <= 0 || !c.Slots.Any(s => s.Id == slotId && s.Enabled)) return false;
            return PdmTransactions.Apply(w, receipt, 0, d => {
                var stock = d.Stock.Find(s => s.Id == stockId);
                if (!Idle(d) || stock == null || stock.AwaitingCollection || stock.Engine < 300 || stock.Body < 300 || d.Showroom.Any(e => e.StockId == stockId || e.SlotId == slotId || e.Handle == handle) || d.Showroom.Count >= 24) return false;
                d.Showroom.Add(new PdmShowroomEntry { SlotId = slotId, StockId = stockId, Handle = handle }); return true;
            }, save, out changed);
        }
        public static bool Track(WalletData w, string stockId, int handle, string receipt, Action save, out bool changed)
        {
            changed = false; if (handle < 0) return false;
            return PdmTransactions.Apply(w, receipt, 0, d => { var e = d.Showroom.Find(x => x.StockId == stockId); if (e == null || d.ShowroomSale?.StockId == stockId || d.PremisesService?.StockId == stockId || handle > 0 && d.Showroom.Any(x => x.StockId != stockId && x.Handle == handle)) return false; e.Handle = handle; return true; }, save, out changed);
        }
        public static bool Remove(WalletData w, string stockId, string receipt, Action save, out bool changed)
        { return PdmTransactions.Apply(w, receipt, 0, d => { if (!Idle(d) || !d.Showroom.Any(e => e.StockId == stockId)) return false; d.Showroom.RemoveAll(e => e.StockId == stockId); return true; }, save, out changed); }
        public static bool BeginSale(WalletData w, string stockId, string id, Action save, out bool changed)
        {
            changed = false; if (string.IsNullOrWhiteSpace(id) || id.Length > 100) return false;
            return PdmTransactions.Apply(w, id + ":showroom", 0, d => {
                var e = d.Showroom.Find(x => x.StockId == stockId); var stock = d.Stock.Find(s => s.Id == stockId);
                if (!Idle(d) || e == null || e.Handle <= 0 || stock == null || stock.AwaitingCollection || stock.Engine < 300 || stock.Body < 300) return false;
                int price = PdmStaffTransactions.Quote(d, stock, stock.Engine, stock.Body); if (price < 1) return false;
                d.ShowroomSale = new PdmShowroomSale { Id = id, StockId = stockId, Handle = e.Handle, Price = price }; return true;
            }, save, out changed);
        }
        public static bool Sell(WalletData w, string id, Action save, out bool changed)
        {
            return PdmTransactions.Apply(w, id + ":showroom-sale", 0, d => {
                var sale = d.ShowroomSale; if (sale == null || sale.Id != id) return false; var stock = d.Stock.Find(s => s.Id == sale.StockId);
                if (stock == null || d.Cash > int.MaxValue - sale.Price || d.Revenue > long.MaxValue - sale.Price || d.SoldCost > long.MaxValue - stock.Cost || d.Sold == int.MaxValue) return false;
                d.Cash += sale.Price; d.Revenue += sale.Price; d.SoldCost += stock.Cost; d.Sold++;
                d.Showroom.RemoveAll(e => e.StockId == stock.Id); d.Stock.Remove(stock); d.ShowroomSale = null;
                PdmTransactions.Entry(d, "Venda showroom: " + stock.Name, sale.Price); return true;
            }, save, out changed);
        }
        public static bool Cancel(WalletData w, string id, Action save, out bool changed)
        { return PdmTransactions.Apply(w, id + ":showroom-cancel", 0, d => { if (d.ShowroomSale == null || d.ShowroomSale.Id != id) return false; d.ShowroomSale = null; return true; }, save, out changed); }
    }
    public enum PdmShowroomStage { Waiting, Handover, Complete, Cancelled }
    public sealed class PdmShowroomRun
    {
        public readonly string Id, StockId;
        public PdmShowroomStage Stage { get; private set; } = PdmShowroomStage.Waiting;
        readonly int duration; int began;
        public PdmShowroomRun(string id, string stockId, int seconds) { if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(stockId) || seconds < 2 || seconds > 10) throw new ArgumentException(); Id = id; StockId = stockId; duration = seconds * 1000; }
        public bool Handover(int now, bool eligible) { if (Stage != PdmShowroomStage.Waiting || !eligible) return false; began = now; Stage = PdmShowroomStage.Handover; return true; }
        public bool Commit(int now, bool eligible, Func<bool> save) { if (Stage != PdmShowroomStage.Handover) return false; if (!eligible) { Stage = PdmShowroomStage.Waiting; return false; } if (unchecked(now - began) < duration || !save()) return false; Stage = PdmShowroomStage.Complete; return true; }
        public bool Cancel(Func<bool> save) { if (Stage == PdmShowroomStage.Complete || Stage == PdmShowroomStage.Cancelled || !save()) return false; Stage = PdmShowroomStage.Cancelled; return true; }
    }
}
