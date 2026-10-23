using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RPCore
{
    public sealed class DealerProduct
    {
        public string Id, Name;
        public float Weight;
        public int Buy, Sell;
    }
    public sealed class DealerOptions : ExtensibleData
    {
        public JobPoint Supplier;
        public int SalesPerSession = 3, BuyerSeconds = 120, HandoverSeconds = 3, PolicePercent = 8, RefusePercent = 10, PremiumRefusePercent = 35;
        public float Capacity = 10;
        public List<DealerProduct> Products = new List<DealerProduct>();
        [JsonIgnore] public bool Valid => Supplier != null && Supplier.Valid && SalesPerSession >= 1 && SalesPerSession <= 10 && BuyerSeconds >= 30 && BuyerSeconds <= 600 && HandoverSeconds >= 2 && HandoverSeconds <= 10
            && PolicePercent >= 0 && PolicePercent <= 40 && RefusePercent >= 0 && RefusePercent <= 60 && PremiumRefusePercent >= RefusePercent && PremiumRefusePercent <= 80
            && !float.IsNaN(Capacity) && !float.IsInfinity(Capacity) && Capacity >= 1 && Capacity <= 100
            && Products != null && Products.Count > 0 && Products.Count <= 20 && Products.All(p => p != null && !string.IsNullOrWhiteSpace(p.Id) && !string.IsNullOrWhiteSpace(p.Name) && !float.IsNaN(p.Weight) && !float.IsInfinity(p.Weight) && p.Weight > 0 && p.Weight <= 5 && p.Buy >= 1 && p.Buy <= 10000 && p.Sell >= p.Buy && p.Sell <= 20000)
            && Products.Select(p => p.Id).Distinct().Count() == Products.Count;
    }
    public static class DealerDefaults
    {
        public static DealerOptions Load()
        {
            using (var s = typeof(DealerDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.Dealer.json"))
            using (var r = new System.IO.StreamReader(s)) return JsonConvert.DeserializeObject<DealerOptions>(r.ReadToEnd());
        }
    }
    public sealed class DealerData : ExtensibleData
    {
        public int Sales, Units, Sessions, Refused, Incidents;
        public long Earned;
        public Dictionary<string, int> Stock = new Dictionary<string, int>();
        public Dictionary<string, int> ZoneRep = new Dictionary<string, int>();
        public List<string> Receipts = new List<string>();
        [JsonIgnore] public bool Valid => Sales >= 0 && Units >= 0 && Sessions >= 0 && Refused >= 0 && Incidents >= 0 && Earned >= 0 && Stock != null && Stock.All(p => !string.IsNullOrWhiteSpace(p.Key) && p.Value >= 0 && p.Value <= 999)
            && ZoneRep != null && ZoneRep.All(p => DealerRules.ZoneValid(p.Key) && p.Value >= 0 && p.Value <= 100) && Receipts != null;
    }
    public enum DealerOutcome { Sale, Refused, Police }
    public static class DealerRules
    {
        public static bool ZoneValid(string zone) => !string.IsNullOrWhiteSpace(zone) && zone.Length <= 24;
        public static int Reputation(DealerData d, string zone) => d?.ZoneRep != null && zone != null && d.ZoneRep.TryGetValue(zone, out int n) ? Math.Max(0, Math.Min(100, n)) : 0;
        public static int UnitPrice(DealerProduct p, int rep, bool premium)
        {
            if (p == null || p.Sell < 1 || p.Sell > 20000 || rep < 0 || rep > 100) throw new ArgumentOutOfRangeException();
            int regular = p.Sell + p.Sell * rep / 1000;
            return premium ? regular + regular / 5 : regular;
        }
        public static DealerOutcome Outcome(DealerOptions c, int rep, bool premium, int policeRoll, int refusalRoll)
        {
            if (c == null || !c.Valid || rep < 0 || rep > 100 || policeRoll < 0 || policeRoll >= 100 || refusalRoll < 0 || refusalRoll >= 100) throw new ArgumentOutOfRangeException();
            int police = c.PolicePercent + (premium && c.PolicePercent > 0 ? 4 : 0);
            if (policeRoll < police) return DealerOutcome.Police;
            int refusal = Math.Max(0, (premium ? c.PremiumRefusePercent : c.RefusePercent) - rep / (premium ? 10 : 20));
            return refusalRoll < refusal ? DealerOutcome.Refused : DealerOutcome.Sale;
        }
    }
    public static class DealerTransactions
    {
        static bool Apply(WalletData w, string id, int delta, Func<DealerData, bool> mutate, Action save, out bool changed)
        {
            changed = false;
            if (w == null || w.Bank < 0 || string.IsNullOrWhiteSpace(id) || id.Length > 160 || save == null || (w.Dealer != null && !w.Dealer.Valid)) return false;
            if (w.Dealer?.Receipts.Contains(id) == true) return true;
            if ((long)w.Bank + delta < 0 || (long)w.Bank + delta > int.MaxValue) return false;
            var old = w.Dealer;
            var next = old == null ? new DealerData() : JsonConvert.DeserializeObject<DealerData>(JsonConvert.SerializeObject(old));
            if (!mutate(next) || !next.Valid) return false;
            next.Receipts.Add(id); if (next.Receipts.Count > 512) next.Receipts.RemoveRange(0, next.Receipts.Count - 512);
            int bank = w.Bank;
            try { w.Dealer = next; w.Bank = bank + delta; save(); changed = true; return true; }
            catch { w.Dealer = old; w.Bank = bank; throw; }
        }
        public static double Weight(DealerData d, DealerOptions c) => d.Stock.Sum(p => p.Value * (double)(c.Products.Find(m => m.Id == p.Key)?.Weight ?? 1));
        public static int KnownUnits(DealerData d, DealerOptions c) => d != null && d.Valid && c != null && c.Valid ? d.Stock.Where(p => c.Products.Any(m => m.Id == p.Key)).Sum(p => p.Value) : 0;
        public static bool Buy(WalletData w, DealerOptions c, string product, int count, string id, Action save, out bool changed)
        {
            changed = false; if (c == null || !c.Valid || count < 1 || count > 20) return false; var p = c.Products.Find(m => m.Id == product); if (p == null) return false;
            return Apply(w, id, -p.Buy * count, d => { d.Stock.TryGetValue(product, out int n); if (n > 999 - count) return false; d.Stock[product] = n + count; return Weight(d, c) <= c.Capacity + .0001; }, save, out changed);
        }
        public static bool Sale(WalletData w, DealerOptions c, string product, int count, string zone, bool premium, int quotedUnit, bool last, string id, Action save, out bool changed)
        {
            changed = false; if (w == null || w.Bank < 0 || save == null || string.IsNullOrWhiteSpace(id) || id.Length > 160 || c == null || !c.Valid || !DealerRules.ZoneValid(zone) || count < 1 || count > 5) return false;
            if (w?.Dealer?.Valid == true && !string.IsNullOrWhiteSpace(id) && id.Length <= 160 && w.Dealer.Receipts.Contains(id)) return true;
            var p = c.Products.Find(m => m.Id == product); if (p == null || quotedUnit != DealerRules.UnitPrice(p, DealerRules.Reputation(w?.Dealer, zone), premium)) return false;
            int total = quotedUnit * count;
            return Apply(w, id, total, d => {
                if (!d.Stock.TryGetValue(product, out int held) || held < count || d.Sales == int.MaxValue || d.Units > int.MaxValue - count || (last && d.Sessions == int.MaxValue) || d.Earned > long.MaxValue - total) return false;
                if (held == count) d.Stock.Remove(product); else d.Stock[product] = held - count;
                d.Sales++; d.Units += count; if (last) d.Sessions++; d.Earned += total; d.ZoneRep[zone] = Math.Min(100, DealerRules.Reputation(d, zone) + 5); return true;
            }, save, out changed);
        }
        public static bool Encounter(WalletData w, DealerOutcome outcome, string zone, string id, Action save, out bool changed)
        {
            changed = false; if (!DealerRules.ZoneValid(zone) || (outcome != DealerOutcome.Refused && outcome != DealerOutcome.Police)) return false;
            return Apply(w, id, 0, d => {
                if ((outcome == DealerOutcome.Refused && d.Refused == int.MaxValue) || (outcome == DealerOutcome.Police && d.Incidents == int.MaxValue)) return false;
                if (outcome == DealerOutcome.Refused) d.Refused++; else d.Incidents++;
                d.ZoneRep[zone] = Math.Max(0, DealerRules.Reputation(d, zone) - (outcome == DealerOutcome.Police ? 3 : 1)); return true;
            }, save, out changed);
        }
    }
    public enum DealerStage { Waiting, Offer, Handover, Finished, Cancelled }
    public sealed class DealerRun
    {
        public readonly string Id = Guid.NewGuid().ToString("N");
        public readonly int Total, BuyerMs, HandoverMs;
        public DealerStage Stage { get; private set; } = DealerStage.Waiting;
        public int Sold { get; private set; }
        public int Offers { get; private set; }
        public string Product { get; private set; }
        public string Zone { get; private set; }
        public int Reputation { get; private set; }
        public bool Premium { get; private set; }
        int began, expiresBegan, policeRoll, refusalRoll;
        public string Receipt => Id + ":offer:" + Offers;
        public DealerRun(int total, int buyerSeconds, int handoverSeconds)
        {
            if (total < 1 || total > 10 || buyerSeconds < 30 || buyerSeconds > 600 || handoverSeconds < 2 || handoverSeconds > 10) throw new ArgumentOutOfRangeException();
            Total = total; BuyerMs = buyerSeconds * 1000; HandoverMs = handoverSeconds * 1000;
        }
        public bool Offer(string product, string zone, int rep, int now, int police, int refusal)
        {
            if (Stage != DealerStage.Waiting || Offers >= 12 || string.IsNullOrWhiteSpace(product) || !DealerRules.ZoneValid(zone) || rep < 0 || rep > 100 || police < 0 || police >= 100 || refusal < 0 || refusal >= 100) return false;
            Product = product; Zone = zone; Reputation = rep; began = expiresBegan = now; policeRoll = police; refusalRoll = refusal; Offers++; Stage = DealerStage.Offer; return true;
        }
        public bool Expired(int now) => (Stage == DealerStage.Offer || Stage == DealerStage.Handover) && unchecked(now - expiresBegan) >= BuyerMs;
        public bool Choose(DealerOptions c, bool premium, int now, Func<DealerOutcome, bool> encounter)
        {
            if (Stage != DealerStage.Offer || Expired(now)) return false;
            var outcome = DealerRules.Outcome(c, Reputation, premium, policeRoll, refusalRoll);
            if (outcome != DealerOutcome.Sale && !encounter(outcome)) return false;
            Premium = premium;
            if (outcome == DealerOutcome.Sale) { Stage = DealerStage.Handover; began = now; }
            else Stage = outcome == DealerOutcome.Police ? DealerStage.Cancelled : DealerStage.Waiting;
            return true;
        }
        public bool Commit(int now, bool eligible, Func<bool, bool> sale)
        {
            if (Stage != DealerStage.Handover || !eligible || Expired(now) || unchecked(now - began) < HandoverMs || !sale(Sold + 1 == Total)) return false;
            Sold++; Stage = Sold == Total ? DealerStage.Finished : DealerStage.Waiting; return true;
        }
        public void Skip() { if (Stage == DealerStage.Offer || Stage == DealerStage.Handover) Stage = DealerStage.Waiting; }
        public void Cancel() { if (Stage != DealerStage.Finished) Stage = DealerStage.Cancelled; }
    }
}
