using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class HousingOptions : ExtensibleData
    {
        public string Id = "eclipse", Name = "Eclipse Towers", Ipl = "apa_v_mp_h_01_c";
        public int Price = 1425000, LoadSeconds = 15;
        public JobPoint Entrance, Interior, Exit;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(Id) && Id.Length <= 40 && !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Ipl) && Ipl.Length <= 80 && Price > 0 && Price <= 100000000 && LoadSeconds >= 5 && LoadSeconds <= 30 && Entrance != null && Entrance.Valid && Interior != null && Interior.Valid && Exit != null && Exit.Valid;
    }
    public static class HousingDefaults
    {
        public static HousingOptions Load()
        { using (var s = typeof(HousingDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.Housing.json")) using (var r = new System.IO.StreamReader(s)) return JsonConvert.DeserializeObject<HousingOptions>(r.ReadToEnd()); }
    }
    public sealed class HousingProperty : ExtensibleData
    {
        public string Id, Name;
        public int Paid;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(Id) && Id.Length <= 40 && !string.IsNullOrWhiteSpace(Name) && Paid > 0 && Paid <= 100000000;
    }
    public sealed class HousingData : ExtensibleData
    {
        public List<HousingProperty> Properties = new List<HousingProperty>();
        public List<string> Receipts = new List<string>();
        [JsonIgnore] public bool Valid => Properties != null && Properties.Count <= 20 && Properties.All(p => p != null && p.Valid) && Properties.Select(p => p.Id).Distinct().Count() == Properties.Count && Receipts != null && Receipts.Count <= 128 && Receipts.All(r => !string.IsNullOrWhiteSpace(r) && r.Length <= 100) && Receipts.Distinct().Count() == Receipts.Count;
        public bool Owns(string id) => Properties.Any(p => p.Id == id);
    }
    public static class HousingTransactions
    {
        public static bool Purchase(WalletData w, HousingOptions c, string receipt, Action save, out bool changed)
        {
            changed = false;
            if (w == null || c == null || !c.Valid || save == null || string.IsNullOrWhiteSpace(receipt) || receipt.Length > 100 || w.Bank < 0 || w.Housing != null && !w.Housing.Valid) return false;
            var old = w.Housing;
            if (old?.Receipts.Contains(receipt) == true) return true;
            if (w.Bank < c.Price || old?.Owns(c.Id) == true || old?.Properties.Count >= 20) return false;
            var next = old == null ? new HousingData() : JsonConvert.DeserializeObject<HousingData>(JsonConvert.SerializeObject(old));
            next.Properties.Add(new HousingProperty { Id = c.Id, Name = c.Name, Paid = c.Price }); next.Receipts.Add(receipt);
            if (next.Receipts.Count > 128) next.Receipts.RemoveAt(0);
            int bank = w.Bank;
            try { w.Housing = next; w.Bank -= c.Price; save(); changed = true; return true; }
            catch { w.Housing = old; w.Bank = bank; throw; }
        }
    }
    public static class HousingPlacement
    {
        public static bool Floor(float targetZ, float hitZ, float normalZ) => Finite(targetZ) && Finite(hitZ) && Finite(normalZ) && targetZ - hitZ >= .2f && targetZ - hitZ <= 1.8f && normalZ >= .6f;
        static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
    }
    public enum HousingTransitStage { Waiting, Complete, Cancelled }
    public sealed class HousingTransit
    {
        public HousingTransitStage Stage { get; private set; }
        readonly int began, timeout;
        public HousingTransit(int now, int seconds) { if (seconds < 5 || seconds > 30) throw new ArgumentException(); began = now; timeout = seconds * 1000; }
        public bool Update(int now, bool eligible, bool ready, Action move)
        {
            if (Stage != HousingTransitStage.Waiting) return false;
            if (!eligible || unchecked(now - began) >= timeout) { Stage = HousingTransitStage.Cancelled; return false; }
            if (!ready) return false;
            move(); Stage = HousingTransitStage.Complete; return true;
        }
    }
}
