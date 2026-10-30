using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class WardrobeOptions : ExtensibleData
    {
        public JobPoint Point;
        public int Capacity = 30;
        [JsonIgnore] public bool Valid => Point != null && Point.Valid && Capacity >= 1 && Capacity <= 100;
    }
    public static class WardrobeDefaults
    {
        public static WardrobeOptions Load()
        { using (var s = typeof(WardrobeDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.Wardrobe.json")) using (var r = new System.IO.StreamReader(s)) return JsonConvert.DeserializeObject<WardrobeOptions>(r.ReadToEnd()); }
    }
    public sealed class WardrobePiece : ExtensibleData
    {
        public int Slot, Drawable, Texture, Palette;
    }
    public sealed class WardrobeOutfit : ExtensibleData
    {
        public string Id, Name;
        public int Model;
        [JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)] public long Revision;
        public List<WardrobePiece> Components = new List<WardrobePiece>(), Props = new List<WardrobePiece>();
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(Id) && Id.Length <= 100 && WardrobeRules.ValidName(Name) && Model != 0 && Revision>=0 && Components != null && Components.Count == WardrobeRules.ComponentSlots.Length && Components.All(p => p != null && WardrobeRules.ComponentSlots.Contains(p.Slot) && p.Drawable >= 0 && p.Drawable <= 10000 && p.Texture >= 0 && p.Texture <= 10000 && p.Palette >= 0 && p.Palette <= 3) && Components.Select(p => p.Slot).Distinct().Count() == Components.Count
            && Props != null && Props.Count == WardrobeRules.PropSlots.Length && Props.All(p => p != null && WardrobeRules.PropSlots.Contains(p.Slot) && p.Drawable >= -1 && p.Drawable <= 10000 && p.Texture >= 0 && p.Texture <= 10000 && p.Palette == 0 && (p.Drawable >= 0 || p.Texture == 0)) && Props.Select(p => p.Slot).Distinct().Count() == Props.Count;
    }
    public sealed class WardrobeData : ExtensibleData
    {
        public List<WardrobeOutfit> Outfits = new List<WardrobeOutfit>();
        public List<string> Receipts = new List<string>();
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)] public List<WardrobeOutfit> Equipped;
        [JsonIgnore] public bool Valid => (Equipped==null || Equipped.Count<=100 && Equipped.All(o=>o!=null && o.Valid && o.Revision>0) && Equipped.Select(o=>o.Model).Distinct().Count()==Equipped.Count) && Outfits != null && Outfits.Count <= 100 && Outfits.All(o => o != null && o.Valid) && Outfits.Select(o => o.Id).Distinct().Count() == Outfits.Count && Outfits.GroupBy(o => o.Model).All(g => g.Select(o => o.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == g.Count()) && Receipts != null && Receipts.Count <= 128 && Receipts.All(r => !string.IsNullOrWhiteSpace(r) && r.Length <= 100) && Receipts.Distinct().Count() == Receipts.Count;
    }
    public static class WardrobeRules
    {
        public static readonly int[] ComponentSlots = { 1,3,4,5,6,7,8,9,10,11 }, PropSlots = { 0,1,2,6,7 };
        public static bool ValidName(string name) => !string.IsNullOrWhiteSpace(name) && name.Length <= 32 && name.Trim() == name && !name.Any(char.IsControl);
        public static bool DrawableAvailable(WardrobePiece p,bool prop,int count)
            =>p!=null && count>=0 && (prop && p.Drawable==-1 || p.Drawable>=0 && (p.Drawable<count || !prop && count==0 && p.Drawable==0));
        public static bool Available(WardrobePiece p, bool prop, int drawables, int textures)
        {
            if (p == null || drawables < 0 || textures < 0) return false;
            if (prop && p.Drawable == -1) return p.Texture == 0;
            return p.Drawable >= 0 && p.Texture >= 0 && (p.Drawable < drawables || !prop && drawables == 0 && p.Drawable == 0) && (p.Texture < textures || !prop && textures == 0 && p.Texture == 0);
        }
        public static int Cycle(int current, int delta, int count, bool prop)
        {
            if (count < 0 || count > 10000 || delta != 1 && delta != -1) throw new ArgumentException();
            int minimum = prop ? -1 : 0, choices = prop ? count+1 : Math.Max(1,count);
            if (current < minimum || current >= minimum+choices) throw new ArgumentException();
            return (current-minimum+delta+choices)%choices+minimum;
        }
    }
    public static class WardrobeTransactions
    {
        static T Copy<T>(T value) => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        static bool Apply(WalletData w, string receipt, Func<WardrobeData,bool> change, Action save, out bool changed)
        {
            changed = false; if (w == null || save == null || string.IsNullOrWhiteSpace(receipt) || receipt.Length > 100 || w.Wardrobe != null && !w.Wardrobe.Valid) return false;
            var old = w.Wardrobe; if (old?.Receipts.Contains(receipt) == true) return true;
            var next = old == null ? new WardrobeData() : Copy(old); if (!change(next) || !next.Valid) return false;
            next.Receipts.Add(receipt); if (next.Receipts.Count > 128) next.Receipts.RemoveAt(0);
            try { w.Wardrobe = next; save(); changed = true; return true; } catch { w.Wardrobe = old; throw; }
        }
        static List<WardrobePiece> ReplacePieces(List<WardrobePiece> old, List<WardrobePiece> source)
        { return source.Select(p => { var copy = Copy(old.Find(v => v.Slot == p.Slot)); copy.Drawable = p.Drawable; copy.Texture = p.Texture; copy.Palette = p.Palette; return copy; }).ToList(); }
        public static bool Save(WalletData w, WardrobeOptions c, WardrobeOutfit outfit, bool replace, string receipt, Action save, out bool changed,bool equip=false)
        {
            changed = false; if (c == null || !c.Valid || outfit == null || !outfit.Valid) return false;
            return Apply(w,receipt,d => {
                var old = d.Outfits.Find(o => o.Id == outfit.Id);
                if (replace ? old == null || old.Model != outfit.Model : old != null || d.Outfits.Count >= c.Capacity) return false;
                if (d.Outfits.Any(o => o.Id != outfit.Id && o.Model == outfit.Model && string.Equals(o.Name,outfit.Name,StringComparison.OrdinalIgnoreCase))) return false;
                if (replace) { var row = Copy(old); row.Name = outfit.Name; row.Components = ReplacePieces(old.Components,outfit.Components); row.Props = ReplacePieces(old.Props,outfit.Props); d.Outfits[d.Outfits.IndexOf(old)] = row; }
                else d.Outfits.Add(Copy(outfit)); return !equip || SetEquipped(d,outfit);
            },save,out changed);
        }
        static bool SetEquipped(WardrobeData d,WardrobeOutfit outfit)
        {
            var old=d.Equipped?.Find(o=>o.Model==outfit.Model);if(old?.Revision==long.MaxValue)return false;
            var next=Copy(outfit);next.Revision=(old?.Revision??0)+1;
            if(old!=null){
                next.Extra=old.Extra;
                foreach(var p in next.Components)p.Extra=Copy(old.Components.Find(x=>x.Slot==p.Slot)?.Extra);
                foreach(var p in next.Props)p.Extra=Copy(old.Props.Find(x=>x.Slot==p.Slot)?.Extra);
            }
            if(d.Equipped==null)d.Equipped=new List<WardrobeOutfit>();
            if(old==null){if(d.Equipped.Count>=100)return false;d.Equipped.Add(next);}else d.Equipped[d.Equipped.IndexOf(old)]=next;
            return true;
        }
        public static bool Equip(WalletData w,WardrobeOutfit outfit,string receipt,Action save,out bool changed)
        {changed=false;return outfit!=null && outfit.Valid && Apply(w,receipt,d=>SetEquipped(d,outfit),save,out changed);}
        public static WardrobeOutfit Equipped(WardrobeData data,int model)=>data?.Equipped?.Find(o=>o.Model==model);
        public static bool PreferEquipped(WardrobeOutfit equipped,CharacterCard card)=>equipped!=null && equipped.Valid && card!=null && CharacterRules.ModelHash(card.Model)==equipped.Model && (card.Body==null || equipped.Revision>card.BodyClothingRevision);
        public static bool Rename(WalletData w, string id, string name, string receipt, Action save, out bool changed)
        { changed = false; if (!WardrobeRules.ValidName(name)) return false; return Apply(w,receipt,d => { var row = d.Outfits.Find(o => o.Id == id); if (row == null || d.Outfits.Any(o => o.Id != id && o.Model == row.Model && string.Equals(o.Name,name,StringComparison.OrdinalIgnoreCase))) return false; row.Name = name; return true; },save,out changed); }
        public static bool Delete(WalletData w, string id, string receipt, Action save, out bool changed)
        { return Apply(w,receipt,d => { var row = d.Outfits.Find(o => o.Id == id); if (row == null) return false; d.Outfits.Remove(row); return true; },save,out changed); }
    }
}
