using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using GTA;
using GTA.UI;
using LemonUI;
using LemonUI.Menus;

namespace RPCore
{
    public class ItemDef
    {
        public string Id;
        public string Name;
        public string Desc = "";
        public float Weight = 0.1f;
        public int Price;          // 0 = not sold in shops
        public float Hunger;
        public float Thirst;
        public int Health;
    }

    public class InvData : ExtensibleData
    {
        public Dictionary<string, int> Items = new Dictionary<string, int>();
        public Dictionary<string, Dictionary<string, int>> Containers = new Dictionary<string, Dictionary<string, int>>();
    }

    public class InventorySystem : ISystem
    {
        public string Name { get { return "inventory"; } }
        public readonly Dictionary<string, ItemDef> Defs;

        readonly ObjectPool pool;
        readonly NeedsSystem needs;
        readonly AchievementSystem ach;
        readonly NativeMenu menu = new NativeMenu("Inventory", "");
        InvData d;
        bool dirty;
        public event Action<ItemDef> Used;

        static List<ItemDef> DefaultItems()
        {
            return new List<ItemDef>
            {
                new ItemDef { Id = "water",    Name = "Water Bottle", Desc = "Cold and wet.",          Weight = 0.5f, Price = 3,  Thirst = 35f },
                new ItemDef { Id = "soda",     Name = "Soda",         Desc = "Sugar and bubbles.",     Weight = 0.4f, Price = 4,  Thirst = 20f, Hunger = 4f },
                new ItemDef { Id = "burger",   Name = "Burger",       Desc = "Questionable, filling.", Weight = 0.4f, Price = 8,  Hunger = 35f },
                new ItemDef { Id = "sandwich", Name = "Sandwich",     Desc = "Gas station classic.",   Weight = 0.3f, Price = 6,  Hunger = 25f },
                new ItemDef { Id = "chips",    Name = "Chips",        Desc = "Crunchy.",               Weight = 0.2f, Price = 3,  Hunger = 12f },
                new ItemDef { Id = "bandage",  Name = "Bandage",      Desc = "Patches you up a bit.",  Weight = 0.1f, Price = 15, Health = 40 },
            };
        }

        public InventorySystem(ObjectPool pool, NeedsSystem needs, AchievementSystem ach)
        {
            this.pool = pool;
            this.needs = needs;
            this.ach = ach;

            var items = Store.LoadDef("Items.json", DefaultItems);
            string packs = Path.Combine(Cfg.Dir, "Inventory");
            if (Directory.Exists(packs)) foreach (string pack in Directory.GetFiles(packs, "*.json").OrderBy(p => p))
            {
                try { items.AddRange(new JsonFiles().Read(pack, () => new List<ItemDef>())); }
                catch (Exception ex) { Log.Error("item pack " + Path.GetFileName(pack), ex); }
            }
            Defs = items.Where(i => i != null && !string.IsNullOrEmpty(i.Id) && i.Weight >= 0 && !float.IsNaN(i.Weight) && !float.IsInfinity(i.Weight)).GroupBy(i => i.Id).ToDictionary(g => g.Key, g => g.Last());
            pool.Add(menu); Visuals.Register(menu, "inventory");
            Reload();
        }

        public void Reload()
        {
            d = Store.Load("Inventory.json", () => new InvData());
            if (d.Items == null) d.Items = new Dictionary<string, int>();
            if (d.Containers == null) d.Containers = new Dictionary<string, Dictionary<string, int>>();
            // Unknown item IDs remain in the save, so temporarily removed custom definitions do not delete inventory.
            if (menu.Visible) menu.Visible = false;
        }

        public float Weight
        {
            get { return d.Items.Where(kv => kv.Value > 0).Sum(kv => (Defs.ContainsKey(kv.Key) ? Defs[kv.Key].Weight : 0.1f) * kv.Value); }
        }

        public int Count(string id)
        {
            int n;
            return d.Items.TryGetValue(id, out n) ? n : 0;
        }

        public bool Add(string id, int qty = 1)
        {
            ItemDef def;
            if (!Defs.TryGetValue(id, out def) || qty <= 0) return false;
            if (Count(id) > int.MaxValue - qty || Weight + def.Weight * qty > Cfg.MaxWeight) return false;
            d.Items[id] = Count(id) + qty;
            dirty = true;
            return true;
        }

        public bool Remove(string id, int qty = 1)
        {
            int have = Count(id);
            if (qty <= 0 || have < qty) return false;
            if (have == qty) d.Items.Remove(id); else d.Items[id] = have - qty;
            dirty = true;
            return true;
        }

        void Use(ItemDef def)
        {
            if (def.Hunger == 0 && def.Thirst == 0 && def.Health == 0) return;
            if (Game.Player.Character.IsDead || !Remove(def.Id)) return;
            needs.Feed(def.Hunger, def.Thirst);
            if (def.Health > 0)
            {
                Ped p = Game.Player.Character;
                p.Health = Math.Min(p.MaxHealth, p.Health + def.Health);
            }
            Notify.Show("Used " + def.Name);
            ach.Bump("items_used");
            if (def.Health > 0) ach.Bump("first_aid");
            Used?.Invoke(def);
        }

        void Rebuild()
        {
            int idx = menu.SelectedIndex;
            menu.Clear();
            Visuals.ClearItems(menu);
            menu.Name = string.Format("{0:0.0} / {1:0} kg", Weight, Cfg.MaxWeight);

            if (d.Items.Count == 0)
            {
                menu.Add(new NativeItem("Empty", "Nothing on you."));
                return;
            }

            foreach (var kv in d.Items.Where(k => k.Value > 0).OrderBy(k => k.Key).ToList())
            {
                ItemDef def;
                if (!Defs.TryGetValue(kv.Key, out def))
                {
                    menu.Add(new NativeItem(kv.Key, "Definition missing; item preserved.") { AltTitle = "x" + kv.Value, Enabled = false });
                    continue;
                }
                var item = new NativeItem(def.Name, def.Desc) { AltTitle = "x" + kv.Value };
                Visuals.Badge(menu, item, def.Id);
                item.Activated += (s, e) => { Use(def); };
                menu.Add(item);
            }
            menu.SelectedIndex = Math.Max(0, Math.Min(idx, menu.Items.Count - 1));
        }

        public void Tick()
        {
            // rebuild outside LemonUI's event dispatch, not inside the Activated handler
            if (dirty && menu.Visible) Rebuild();
            dirty = false;
        }

        public void OnKey(KeyEventArgs e)
        {
            if (e.KeyCode != Cfg.InventoryKey) return;
            if (menu.Visible) { menu.Visible = false; return; }
            if (pool.AreAnyVisible) return;
            Rebuild();
            menu.Visible = true;
        }

        public void Save() { Store.Write("Inventory.json", d); }
        public Dictionary<string, int> Snapshot() { return new Dictionary<string, int>(d.Items); }
        public Dictionary<string, int> Container(string id)
        {
            Dictionary<string, int> contents;
            if (!d.Containers.TryGetValue(id, out contents) || contents == null) d.Containers[id] = contents = new Dictionary<string, int>();
            return contents;
        }
        public bool Transfer(string container, string id, bool deposit, float capacity)
        {
            var box = Container(container);
            bool moved = ItemTransfer.Move(deposit ? d.Items : box, deposit ? box : d.Items, id, 1,
                key => Defs.ContainsKey(key) ? Defs[key].Weight : 0.1f, deposit ? capacity : Cfg.MaxWeight);
            if (moved) { dirty = true; Save(); }
            return moved;
        }
        public void Dispose() { }
    }
}
