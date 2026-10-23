using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RPCore
{
    public sealed class FishMaterial
    {
        public string Id, Name;
        public float Weight;
        public int Price, Chance;
    }
    public sealed class FishingOptions : ExtensibleData
    {
        public JobPoint Vendor;
        public List<JobPoint> Spots = new List<JobPoint>();
        public List<FishMaterial> Fish = new List<FishMaterial>();
        public float SpotRadius = 60, BagCapacity = 20;
        public int EquipmentPrice = 350, BaitPrice = 10, FishPerTrip = 3;
        public int WaitMinMs = 8000, WaitMaxMs = 15000, HookWindowMs = 2500, ReelWindowMs = 3000;
        static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
        [JsonIgnore] public bool Valid => Vendor != null && Vendor.Valid && Spots != null && Spots.Count > 0 && Spots.Count <= 30 && Spots.All(p => p != null && p.Valid)
            && Finite(SpotRadius) && SpotRadius >= 10 && SpotRadius <= 150 && Finite(BagCapacity) && BagCapacity >= 2 && BagCapacity <= 500
            && EquipmentPrice >= 0 && EquipmentPrice <= 100000 && BaitPrice >= 1 && BaitPrice <= 10000 && FishPerTrip >= 1 && FishPerTrip <= 10
            && WaitMinMs >= 3000 && WaitMaxMs >= WaitMinMs && WaitMaxMs <= 60000 && HookWindowMs >= 1000 && HookWindowMs <= 10000 && ReelWindowMs >= 1500 && ReelWindowMs <= 10000
            && Fish != null && Fish.Count > 0 && Fish.Count <= 30 && Fish.All(f => f != null && !string.IsNullOrWhiteSpace(f.Id) && !string.IsNullOrWhiteSpace(f.Name) && Finite(f.Weight) && f.Weight > 0 && f.Weight <= 20 && f.Price >= 1 && f.Price <= 100000 && f.Chance >= 1 && f.Chance <= 1000)
            && Fish.Select(f => f.Id).Distinct().Count() == Fish.Count;
        public FishMaterial Pick(int roll)
        {
            if (!Valid || roll < 0 || roll >= Fish.Sum(f => f.Chance)) throw new ArgumentOutOfRangeException(nameof(roll));
            foreach (var f in Fish) { if (roll < f.Chance) return f; roll -= f.Chance; }
            throw new InvalidOperationException();
        }
    }
    public static class FishingDefaults
    {
        public static FishingOptions Load()
        {
            using (var s = typeof(FishingDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.Fishing.json"))
            using (var r = new System.IO.StreamReader(s)) return JsonConvert.DeserializeObject<FishingOptions>(r.ReadToEnd());
        }
    }
    public sealed class FishingData : ExtensibleData
    {
        public bool RodOwned;
        public int Bait, Catches, Trips;
        public long Earned;
        public Dictionary<string, int> Bag = new Dictionary<string, int>();
        public List<string> Receipts = new List<string>();
        [JsonIgnore] public bool Valid => Bait >= 0 && Bait <= 999 && Catches >= 0 && Trips >= 0 && Earned >= 0 && Bag != null
            && Bag.All(p => !string.IsNullOrWhiteSpace(p.Key) && p.Value >= 0 && p.Value <= 1000000) && Receipts != null;
    }
    public static class FishingTransactions
    {
        static bool Apply(WalletData w, string receipt, int delta, Func<FishingData, bool> mutate, Action save, out bool changed)
        {
            changed = false;
            if (w == null || save == null || w.Bank < 0 || string.IsNullOrWhiteSpace(receipt) || receipt.Length > 160 || (w.Fishing != null && !w.Fishing.Valid)) return false;
            if (w.Fishing?.Receipts.Contains(receipt) == true) return true;
            if ((long)w.Bank + delta < 0 || (long)w.Bank + delta > int.MaxValue) return false;
            var old = w.Fishing;
            var next = old == null ? new FishingData() : JsonConvert.DeserializeObject<FishingData>(JsonConvert.SerializeObject(old));
            if (!mutate(next) || !next.Valid) return false;
            next.Receipts.Add(receipt);
            if (next.Receipts.Count > 512) next.Receipts.RemoveRange(0, next.Receipts.Count - 512);
            int bank = w.Bank;
            try { w.Fishing = next; w.Bank = bank + delta; save(); changed = true; return true; }
            catch { w.Fishing = old; w.Bank = bank; throw; }
        }
        public static bool Equipment(WalletData w, FishingOptions c, string id, Action save, out bool changed)
        {
            changed = false; if (c == null || !c.Valid) return false;
            return Apply(w, id, -c.EquipmentPrice, f => { if (f.RodOwned) return false; f.RodOwned = true; return true; }, save, out changed);
        }
        public static bool BuyBait(WalletData w, FishingOptions c, int count, string id, Action save, out bool changed)
        {
            changed = false; if (c == null || !c.Valid || count < 1 || count > 20) return false;
            return Apply(w, id, -c.BaitPrice * count, f => { if (!f.RodOwned || f.Bait > 999 - count) return false; f.Bait += count; return true; }, save, out changed);
        }
        public static bool UseBait(WalletData w, string id, Action save, out bool changed) => Apply(w, id, 0, f => { if (!f.RodOwned || f.Bait < 1) return false; f.Bait--; return true; }, save, out changed);
        public static double Weight(FishingData f, FishingOptions c) => f.Bag.Sum(p => p.Value * (double)(c.Fish.Find(m => m.Id == p.Key)?.Weight ?? 1));
        public static bool Catch(WalletData w, FishingOptions c, string fish, string id, bool last, Action save, out bool changed)
        {
            changed = false; if (c == null || !c.Valid || !c.Fish.Any(f => f.Id == fish)) return false;
            return Apply(w, id, 0, f => {
                if (!f.RodOwned || f.Catches == int.MaxValue || (last && f.Trips == int.MaxValue)) return false;
                f.Bag.TryGetValue(fish, out int n); if (n >= 1000000) return false;
                f.Bag[fish] = n + 1;
                if (Weight(f, c) > c.BagCapacity + .0001) return false;
                f.Catches++; if (last) f.Trips++; return true;
            }, save, out changed);
        }
        public static int Quote(FishingData f, FishingOptions c)
        {
            if (f == null || !f.Valid || c == null || !c.Valid) return 0;
            long total = 0; foreach (var p in f.Bag) { var m = c.Fish.Find(i => i.Id == p.Key); if (m != null) total += (long)m.Price * p.Value; }
            return total > 1000000 ? -1 : (int)total;
        }
        public static bool Sell(WalletData w, FishingOptions c, string id, Action save, out bool changed)
        {
            changed = false; if (w == null || w.Bank < 0 || string.IsNullOrWhiteSpace(id) || id.Length > 160 || c == null || !c.Valid) return false;
            // A persisted receipt acknowledges a replay even after the stock has been sold.
            if (w.Fishing?.Valid == true && w.Fishing.Receipts.Contains(id)) return true;
            int price = Quote(w.Fishing, c); if (price <= 0) return false;
            return Apply(w, id, price, f => { if (!f.RodOwned || f.Earned > long.MaxValue - price) return false; foreach (var m in c.Fish) f.Bag.Remove(m.Id); f.Earned += price; return true; }, save, out changed);
        }
    }
    public enum FishingStage { Ready, Waiting, Hook, Reeling, Landed, Finished, Cancelled }
    public sealed class FishingRun
    {
        public readonly string Id = Guid.NewGuid().ToString("N");
        public readonly int Total;
        public FishingStage Stage { get; private set; } = FishingStage.Ready;
        public int Caught { get; private set; }
        public int Casts { get; private set; }
        public int Reels { get; private set; }
        public string Fish { get; private set; }
        public string Receipt => Id + ":catch:" + Casts;
        readonly int hookWindow, reelWindow;
        int phaseAt, wait;
        public FishingRun(int total, int hookMs, int reelMs)
        {
            if (total < 1 || total > 10 || hookMs < 1000 || hookMs > 10000 || reelMs < 1500 || reelMs > 10000) throw new ArgumentOutOfRangeException();
            Total = total; hookWindow = hookMs; reelWindow = reelMs;
        }
        public bool Cast(int now, int waitMs, string fish, Func<string, bool> consume)
        {
            if (Stage != FishingStage.Ready || waitMs < 3000 || waitMs > 60000 || string.IsNullOrWhiteSpace(fish) || Casts == int.MaxValue) return false;
            if (!consume(Id + ":bait:" + (Casts + 1))) return false;
            Casts++; Fish = fish; Reels = 0; wait = waitMs; phaseAt = now; Stage = FishingStage.Waiting; return true;
        }
        // Called before input: a late frame cannot reopen an expired hook/reel window.
        public bool Advance(int now)
        {
            int elapsed = unchecked(now - phaseAt);
            if (Stage == FishingStage.Waiting && elapsed >= wait)
            {
                if ((long)elapsed >= (long)wait + hookWindow) { Interrupt(); return true; }
                phaseAt = unchecked(phaseAt + wait); Stage = FishingStage.Hook;
            }
            else if ((Stage == FishingStage.Hook && elapsed >= hookWindow) || (Stage == FishingStage.Reeling && elapsed >= reelWindow)) { Interrupt(); return true; }
            return false;
        }
        public bool Press(int now)
        {
            Advance(now);
            if (Stage == FishingStage.Hook) { Stage = FishingStage.Reeling; phaseAt = now; return true; }
            if (Stage != FishingStage.Reeling || unchecked(now - phaseAt) < 600) return false;
            Reels++; phaseAt = now;
            if (Reels == 3) Stage = FishingStage.Landed;
            return true;
        }
        public bool Confirm(Func<string, string, bool, bool> save)
        {
            if (Stage != FishingStage.Landed || !save(Fish, Receipt, Caught + 1 == Total)) return false;
            Caught++; Stage = Caught == Total ? FishingStage.Finished : FishingStage.Ready; Fish = null; return true;
        }
        public void Interrupt()
        {
            if (Stage == FishingStage.Waiting || Stage == FishingStage.Hook || Stage == FishingStage.Reeling) { Stage = FishingStage.Ready; Fish = null; Reels = 0; }
        }
        public void Cancel() { if (Stage != FishingStage.Finished) Stage = FishingStage.Cancelled; }
    }
}
