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
    public partial class InventorySystem : ISystem
    {
        public string Name { get { return "inventory"; } }
        public readonly Dictionary<string, ItemDef> Defs;

        readonly ObjectPool pool;
        readonly NeedsSystem needs;
        readonly AchievementSystem ach;
        readonly NativeMenu menu = new NativeMenu("Inventory", "");
        readonly InventoryGridView grid;
        public bool GridVisible { get { return grid.Visible; } }
        internal List<string> Layout { get { return d.GridSlots; } set { d.GridSlots = value; } }
        InvData d;
        bool dirty;int consumptionRetry;
        public event Action<ItemDef> Used;
        public Action PhoneRequested;
        public readonly List<InventoryRecipe> Recipes;
        public bool StorageGridVisible=>grid.StorageOpen;

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

        public InventorySystem(ObjectPool pool, NeedsSystem needs, AchievementSystem ach, ShiftGate gate = null)
        {
            this.pool = pool;
            this.needs = needs;
            this.ach = ach;
            armoryGate = gate;

            var items = Store.LoadDef("Items.json", DefaultItems);
            Store.LoadDef("Inventory/ReferenceItems.json",()=>InventoryReference.Load<List<ItemDef>>("Items"));
            string packs = Path.Combine(Cfg.Dir, "Inventory");
            if (Directory.Exists(packs)) foreach (string pack in Directory.GetFiles(packs, "*.json").OrderBy(p => p))
            {
                try { items.AddRange(new JsonFiles().Read(pack, () => new List<ItemDef>())); }
                catch (Exception ex) { Log.Error("item pack " + Path.GetFileName(pack), ex); }
            }
            Defs = items.Where(i => i != null && !string.IsNullOrEmpty(i.Id) && i.Weight >= 0 && !float.IsNaN(i.Weight) && !float.IsInfinity(i.Weight)).GroupBy(i => i.Id).ToDictionary(g => g.Key, g => g.Last());
            RegisterWeapons();
            Recipes=Store.LoadDef("InventoryRecipes.json",()=>InventoryReference.Load<List<InventoryRecipe>>("Recipes")).Where(r=>r?.Valid==true && Defs.ContainsKey(r.Result) && r.Ingredients.Keys.All(Defs.ContainsKey)).ToList();
            pool.Add(menu); Visuals.Register(menu, "inventory");
            grid = new InventoryGridView(this, needs);
            Reload();
        }

        public void Reload()
        {
            CleanupUseProps();
            StopUsing();
            weightWarned=false;
            d = Store.Load("Inventory.json", () => new InvData());
            ResetArmory();
            if (d.Items == null) d.Items = new Dictionary<string, int>();
            if (d.Containers == null) d.Containers = new Dictionary<string, Dictionary<string, int>>();
            // Unknown item IDs remain in the save, so temporarily removed custom definitions do not delete inventory.
            if (menu.Visible) menu.Visible = false;
            grid.Close(false);
            ReconcileLayout();RecoverConsumption();
        }
        internal void ReconcileLayout() { d.GridSlots = InventoryLayout.Reconcile(d.GridSlots, d.Items.Where(p => p.Value > 0).Select(p => p.Key),RoleplayInventoryLayout.PageSize); }
        internal void UseSelected(string id) { ItemDef def; if (id != null && Defs.TryGetValue(id, out def)) { Use(def); } }

        internal bool DropSaved(string id,int quantity)
        {try{if(!PrepareArmoryEdit(id))return false;bool ok=InventoryEdits.Drop(ref d,id,quantity,n=>Store.Write("Inventory.json",n));dirty|=ok;if(ok)ApplyArmoryEdit(id);return ok;}catch(Exception ex){Log.Error("inventory discard",ex);Notify.Show("Confira o inventario e o log; descarte nao concluido.");return false;}}
        internal bool MoveSlotSaved(int from,int to)
        {try{bool ok=InventoryEdits.Move(ref d,from,to,n=>Store.Write("Inventory.json",n));dirty|=ok;return ok;}catch(Exception ex){Log.Error("inventory move",ex);Notify.Show("Posicao anterior conservada; movimento nao foi gravado.");return false;}}

        public float Weight
        {
            get { return (float)Math.Min(float.MaxValue,InventoryBurdenRules.Total(d.Items,id=>Defs.TryGetValue(id,out var def)?def.Weight:.1f)); }
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
        {try{if(def==null || Count(def.Id)<=0)return;if(!CanUseItem()){Notify.Show("Aguarde: use itens a pe, fora de menus/atividade e com controle do personagem.");return;}if(def.Weapon!=null){EquipInventoryWeapon(def);return;}if(def.Id.StartsWith("ammo_",StringComparison.Ordinal)){Notify.Show(def.Desc);return;}if(def.Action=="phone"){grid.Close();menu.Visible=false;PhoneRequested?.Invoke();return;}if(def.Hunger==0 && def.Thirst==0 && def.Health==0 && Energy(def)==0){Notify.Show("Este item e material ou ferramenta; consulte a receita/atividade correspondente.");return;}Consume(def);}catch(Exception ex){StopUsing();Log.Error("inventory use",ex);Notify.Show("Uso nao concluido. Confira arquivos/permissoes.");}}
        void Consume(ItemDef def)
        {
            if(def==null || Game.Player.Character.IsDead)return;
            if(Game.Player.Character.IsInCombat || Game.Player.Character.IsShooting){Notify.Show("Guarde o item e saia do combate para consumir.");return;}
            if(!RecoverConsumption())return;
            string id=Guid.NewGuid().ToString("N");var receipt=needs.PrepareFood(id,def.Hunger,def.Thirst,Energy(def));
            var effective=Newtonsoft.Json.JsonConvert.DeserializeObject<ItemDef>(Newtonsoft.Json.JsonConvert.SerializeObject(def));
            if(effective.ReturnItem==null && InventoryReference.Returns.TryGetValue(def.Id,out var returned))effective.ReturnItem=returned;
            if(!string.IsNullOrEmpty(effective.ReturnItem) && !Defs.ContainsKey(effective.ReturnItem))throw new InvalidOperationException("Return item definition unavailable.");
            var next=NeedsRules.Consume(d,effective,receipt);if(next==null)return;
            if(armoryGate!=null && !armoryGate.Acquire("inventory_use"))return;
            // Commit removal and its recovery receipt before applying any benefit.
            Store.Write("Inventory.json",next);d=next;dirty=true;
            if(!RecoverConsumption()){StopUsing();return;}
            if(def.Health>0){var p=Game.Player.Character;p.Health=(int)Math.Min((long)p.MaxHealth,(long)p.Health+Math.Min(def.Health,10000));}
            Notify.Show("Used " + def.Name);
            try{BeginUsing(def);}catch(Exception ex){Log.Error("item presentation",ex);StopUsing();}
            try{ach.Bump("items_used");if(def.Health>0)ach.Bump("first_aid");}catch(Exception ex){Log.Error("consumption achievement",ex);}
            if(Used!=null)foreach(Action<ItemDef> callback in Used.GetInvocationList())try{callback(def);}catch(Exception ex){Log.Error("consumption presentation",ex);}
        }

        bool RecoverConsumption()
        {
            if(d.Consumption==null)return true;
            try{if(!needs.ApplyConsumption(d.Consumption))throw new InvalidOperationException("Recibo de consumo invalido.");return true;}
            catch(Exception ex){Log.Error("inventory consumption recovery",ex);return false;}
        }

        void Rebuild()
        {
            int idx = menu.SelectedIndex;
            menu.Clear();
            Visuals.ClearItems(menu);
            menu.Name = string.Format("{0:0.0} / {1:0} kg", Weight, Cfg.MaxWeight);

            if (!d.Items.Any(k=>k.Value>0))
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
                var item = new NativeItem(def.Name, Describe(def)) { AltTitle = "x" + kv.Value };
                Visuals.Badge(menu, item, def.Icon ?? def.Id);
                item.Activated += (s, e) => { Use(def); };
                menu.Add(item);
            }
            menu.SelectedIndex = Math.Max(0, Math.Min(idx, menu.Items.Count - 1));
        }

        public void Tick()
        {
            CleanupUseProps();
            try{UsingTick();}catch(Exception ex){Log.Error("item presentation tick",ex);StopUsing();}BlockNumberControls();
            ArmoryTick();
            BurdenTick();
            if(d.Consumption!=null && !needs.Consumed(d.Consumption.Id) && unchecked(Game.GameTime-consumptionRetry)>=0){consumptionRetry=Game.GameTime+5000;RecoverConsumption();}
            // rebuild outside LemonUI's event dispatch, not inside the Activated handler
            if (dirty && menu.Visible) Rebuild();
            if (dirty) ReconcileLayout();
            grid.Tick();
            GroundTick();
            dirty = false;
        }

        public void OnKey(KeyEventArgs e)
        {
            if (grid.Visible) { grid.OnKey(e); return; }
            if (e.KeyCode != Cfg.InventoryKey) return;
            if (menu.Visible) { menu.Visible = false; return; }
            if (pool.AreAnyVisible) return;
            if (InterfaceOptions.GridInventory) { ReconcileLayout(); grid.Open(); return; }
            Rebuild();
            menu.Visible = true;
        }

        public void Save() { var p=Game.Player.Character;if(armoryTracking && !armoryApplyPending && p!=null && armoryActor==p.Handle && ArmorySafe())TrackAmmo();ReconcileLayout(); Store.Write("Inventory.json", d); }
        public Dictionary<string, int> Snapshot() { return new Dictionary<string, int>(d.Items); }
        public Dictionary<string, int> Container(string id)
        {
            Dictionary<string, int> contents;
            if (!d.Containers.TryGetValue(id, out contents) || contents == null) d.Containers[id] = contents = new Dictionary<string, int>();
            return contents;
        }
        public Dictionary<string,int> ContainerSnapshot(string id) => InventoryTransfers.Snapshot(d,id);
        public double ContainerWeight(string id) => InventoryTransfers.Weight(ContainerSnapshot(id), key => Defs.ContainsKey(key)?Defs[key].Weight:.1f);
        public int TransferFits(string container,string id,bool deposit,float capacity)=>InventoryTransfers.Fits(d,container,id,deposit,capacity,Cfg.MaxWeight,key=>Defs.TryGetValue(key,out var def)?def.Weight:.1f);
        public bool Transfer(string container,string id,bool deposit,float capacity) => Transfer(container,id,deposit,capacity,1);
        public bool Transfer(string container,string id,bool deposit,float capacity,int quantity)
        {
            if(!PrepareArmoryEdit(id))return false;
            bool moved=InventoryTransfers.Move(d,container,id,quantity,deposit,capacity,Cfg.MaxWeight,key=>Defs.ContainsKey(key)?Defs[key].Weight:.1f,
                next=>Store.Write("Inventory.json",next),out var result);
            if(moved){d=result;dirty=true;ApplyArmoryEdit(id);}return moved;
        }
        public void Dispose() { StopUsing();grid.Close(false); menu.Visible = false; }
    }
}
