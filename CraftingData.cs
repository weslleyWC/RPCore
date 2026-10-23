using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RPCore
{
    public sealed class CraftIngredient
    {
        public string Source, Id, Name;
        public int Quantity;
    }
    public sealed class CraftProduct
    {
        public string Id, Name;
        public float Weight;
        public int Price;
    }
    public sealed class CraftRecipe
    {
        public string Id, Name, Product;
        public int Quantity = 1, Seconds = 6;
        public List<CraftIngredient> Ingredients = new List<CraftIngredient>();
    }
    public sealed class CraftingOptions : ExtensibleData
    {
        public JobPoint Station;
        public float Capacity = 25;
        public List<CraftProduct> Products = new List<CraftProduct>();
        public List<CraftRecipe> Recipes = new List<CraftRecipe>();
        [JsonIgnore] public bool Valid => Station != null && Station.Valid && !float.IsNaN(Capacity) && !float.IsInfinity(Capacity) && Capacity >= 2 && Capacity <= 500
            && Products != null && Products.Count > 0 && Products.Count <= 100 && Products.All(p => p != null && !string.IsNullOrWhiteSpace(p.Id) && !string.IsNullOrWhiteSpace(p.Name) && !float.IsNaN(p.Weight) && !float.IsInfinity(p.Weight) && p.Weight > 0 && p.Weight <= 20 && p.Price >= 1 && p.Price <= 100000)
            && Products.Select(p => p.Id).Distinct().Count() == Products.Count
            && Recipes != null && Recipes.Count > 0 && Recipes.Count <= 100 && Recipes.All(r => r != null && !string.IsNullOrWhiteSpace(r.Id) && !string.IsNullOrWhiteSpace(r.Name) && Products.Any(p => p.Id == r.Product) && r.Quantity >= 1 && r.Quantity <= 10 && r.Seconds >= 2 && r.Seconds <= 30
                && r.Ingredients != null && r.Ingredients.Count > 0 && r.Ingredients.Count <= 10 && r.Ingredients.All(i => i != null && (i.Source == "hunting" || i.Source == "fishing" || i.Source == "crafting") && !string.IsNullOrWhiteSpace(i.Id) && !string.IsNullOrWhiteSpace(i.Name) && i.Quantity >= 1 && i.Quantity <= 100 && (i.Source != "crafting" || Products.Any(p => p.Id == i.Id)))
                && r.Ingredients.Select(i => i.Source + ":" + i.Id).Distinct().Count() == r.Ingredients.Count)
            && Recipes.Select(r => r.Id).Distinct().Count() == Recipes.Count;
    }
    public static class CraftingDefaults
    {
        public static CraftingOptions Load()
        {
            using (var s = typeof(CraftingDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.Crafting.json"))
            using (var r = new System.IO.StreamReader(s)) return JsonConvert.DeserializeObject<CraftingOptions>(r.ReadToEnd());
        }
    }
    public sealed class CraftingData : ExtensibleData
    {
        public int Batches, Made, Sales;
        public long Earned;
        public Dictionary<string, int> Stock = new Dictionary<string, int>();
        public List<string> Receipts = new List<string>();
        [JsonIgnore] public bool Valid => Batches >= 0 && Made >= 0 && Sales >= 0 && Earned >= 0 && Stock != null && Stock.All(p => !string.IsNullOrWhiteSpace(p.Key) && p.Value >= 0 && p.Value <= 1000000) && Receipts != null;
    }
    public static class CraftingTransactions
    {
        static T Clone<T>(T value) => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        static bool IdValid(string id) => !string.IsNullOrWhiteSpace(id) && id.Length <= 160;
        static void Receipt(CraftingData c, string id) { c.Receipts.Add(id); if (c.Receipts.Count > 512) c.Receipts.RemoveRange(0, c.Receipts.Count - 512); }
        public static double Weight(CraftingData d, CraftingOptions c) => d.Stock.Sum(p => p.Value * (double)(c.Products.Find(m => m.Id == p.Key)?.Weight ?? 1));
        static Dictionary<string, int> Bag(string source, CraftingData c, HuntingData h, FishingData f) => source == "crafting" ? c?.Stock : source == "hunting" ? h?.Bag : f?.Bag;
        public static int Available(WalletData w, CraftIngredient i)
        {
            if (w == null || i == null) return 0;
            var bag = Bag(i.Source, w.Crafting, w.Hunting, w.Fishing);
            return bag != null && bag.TryGetValue(i.Id, out int n) ? Math.Max(0, n) : 0;
        }
        static bool Prepare(WalletData w, CraftingOptions c, string recipe, int count, out CraftingData craft, out HuntingData hunt, out FishingData fish, out string reason)
        {
            craft = null; hunt = null; fish = null; reason = "Dados ou receita invalidos.";
            if (w == null || w.Bank < 0 || c == null || !c.Valid || count < 1 || count > 5 || (w.Crafting != null && !w.Crafting.Valid)) return false;
            var r = c.Recipes.Find(p => p.Id == recipe); if (r == null) return false;
            bool useHunt = r.Ingredients.Any(i => i.Source == "hunting"), useFish = r.Ingredients.Any(i => i.Source == "fishing");
            if ((useHunt && w.Hunting != null && !w.Hunting.Valid) || (useFish && w.Fishing != null && !w.Fishing.Valid)) return false;
            craft = w.Crafting == null ? new CraftingData() : Clone(w.Crafting);
            hunt = useHunt ? (w.Hunting == null ? new HuntingData() : Clone(w.Hunting)) : w.Hunting;
            fish = useFish ? (w.Fishing == null ? new FishingData() : Clone(w.Fishing)) : w.Fishing;
            foreach (var i in r.Ingredients)
            {
                var bag = Bag(i.Source, craft, hunt, fish); int required = i.Quantity * count;
                if (bag == null || !bag.TryGetValue(i.Id, out int held) || held < required) { reason = "Falta " + i.Name + " x" + required + " (" + i.Source + ")."; return false; }
                if (held == required) bag.Remove(i.Id); else bag[i.Id] = held - required;
            }
            craft.Stock.TryGetValue(r.Product, out int existing); int made = r.Quantity * count;
            if (existing > 1000000 - made || craft.Batches > int.MaxValue - count || craft.Made > int.MaxValue - made) { reason = "Limite de estoque/progresso atingido."; return false; }
            craft.Stock[r.Product] = existing + made;
            if (Weight(craft, c) > c.Capacity + .0001) { reason = "Estoque da bancada cheio. Venda produtos na feira."; return false; }
            craft.Batches += count; craft.Made += made; reason = null; return true;
        }
        public static bool Preview(WalletData w, CraftingOptions c, string recipe, int count, out string reason) => Prepare(w, c, recipe, count, out _, out _, out _, out reason);
        public static bool Craft(WalletData w, CraftingOptions c, string recipe, int count, string id, Action save, out bool changed, out string reason)
        {
            changed = false; reason = "Recibo ou salvamento invalido.";
            if (!IdValid(id) || save == null || w == null || w.Bank < 0) return false;
            if (w.Crafting?.Valid == true && w.Crafting.Receipts.Contains(id)) { reason = null; return true; }
            if (!Prepare(w, c, recipe, count, out var next, out var hunt, out var fish, out reason)) return false;
            Receipt(next, id);
            var oldCraft = w.Crafting; var oldHunt = w.Hunting; var oldFish = w.Fishing;
            try { w.Crafting = next; w.Hunting = hunt; w.Fishing = fish; save(); changed = true; return true; }
            catch { w.Crafting = oldCraft; w.Hunting = oldHunt; w.Fishing = oldFish; throw; }
        }
        public static int Quote(CraftingData d, CraftingOptions c)
        {
            if (d == null || !d.Valid || c == null || !c.Valid) return 0;
            long total = 0; foreach (var p in d.Stock) { var m = c.Products.Find(i => i.Id == p.Key); if (m != null) total += (long)m.Price * p.Value; }
            return total > 1000000 ? -1 : (int)total;
        }
        public static bool Sell(WalletData w, CraftingOptions c, string id, Action save, out bool changed)
        {
            changed = false; if (w == null || w.Bank < 0 || !IdValid(id) || save == null || w.Crafting == null || !w.Crafting.Valid || c == null || !c.Valid) return false;
            if (w.Crafting.Receipts.Contains(id)) return true;
            int quote = Quote(w.Crafting, c);
            if (quote <= 0 || (long)w.Bank + quote > int.MaxValue || w.Crafting.Earned > long.MaxValue - quote || w.Crafting.Sales == int.MaxValue) return false;
            var old = w.Crafting; int bank = w.Bank; var next = Clone(old);
            foreach (var p in c.Products) next.Stock.Remove(p.Id);
            next.Earned += quote; next.Sales++; Receipt(next, id);
            try { w.Crafting = next; w.Bank = bank + quote; save(); changed = true; return true; }
            catch { w.Crafting = old; w.Bank = bank; throw; }
        }
    }
    public enum CraftingStage { Working, Finished, Cancelled }
    public sealed class CraftingRun
    {
        public readonly string Receipt = Guid.NewGuid().ToString("N");
        public readonly string Recipe;
        public readonly int Count, DurationMs;
        readonly int began;
        public CraftingStage Stage { get; private set; } = CraftingStage.Working;
        public CraftingRun(string recipe, int count, int seconds, int now)
        {
            if (string.IsNullOrWhiteSpace(recipe) || count < 1 || count > 5 || seconds < 2 || seconds > 30) throw new ArgumentOutOfRangeException();
            Recipe = recipe; Count = count; DurationMs = count * seconds * 1000; began = now;
        }
        public int RemainingSeconds(int now) => Math.Max(0, (DurationMs - Math.Max(0, unchecked(now - began)) + 999) / 1000);
        public bool Commit(int now, bool eligible, Func<bool> save)
        {
            if (Stage != CraftingStage.Working) return false;
            if (!eligible) { Cancel(); return false; }
            if (unchecked(now - began) < DurationMs || !save()) return false;
            Stage = CraftingStage.Finished; return true;
        }
        public void Cancel() { if (Stage == CraftingStage.Working) Stage = CraftingStage.Cancelled; }
    }
}
