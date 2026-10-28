using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.UI;
using LemonUI;
using LemonUI.Menus;
using System.Drawing;

namespace RPCore
{
    public class ShopSystem : ISystem
    {
        public string Name { get { return "shop"; } }

        readonly ObjectPool pool;
        readonly ShiftGate gate;
        readonly ShopGridView grid;
        public bool GridVisible=>grid.Visible;
        readonly Func<bool> suppressInteraction;readonly Func<IEnumerable<EditorPoint>> editorPoints;
        readonly InventorySystem inv;
        readonly AchievementSystem ach;
        readonly NativeMenu menu = new NativeMenu("Store", "");
        readonly List<ShopDef> shops;
        readonly List<Blip> blips = new List<Blip>();
        ShopDef current;

        static Vector3 Pos(ShopDef s) { return new Vector3(s.X, s.Y, s.Z); }

        public ShopSystem(ObjectPool pool, InventorySystem inv, AchievementSystem ach, Func<bool> suppressInteraction = null, Func<IEnumerable<EditorPoint>> editorPoints = null, ShiftGate gate = null)
        {
            this.pool = pool;this.gate=gate;
            grid=new ShopGridView(inv,Accessible,Buy);
            this.suppressInteraction = suppressInteraction;this.editorPoints=editorPoints;
            this.inv = inv;
            this.ach = ach;
            shops = ShopLocations.Resolve(Store.LoadDef("Shops.json", ShopLocations.Defaults));
            pool.Add(menu); Visuals.Register(menu, "shop");

            foreach (ShopDef s in shops)
            {
                Blip b = MapBlips.Create(Pos(s), "RPCore Store: " + s.Name, BlipSprite.Store, BlipColor.Green, .7f);
                blips.Add(b);
            }
        }

        bool Accessible()
        {
            var p=Game.Player.Character;
            if(current==null || p==null || !p.Exists() || !p.IsHuman || p.IsDead || !p.IsOnFoot || p.IsInVehicle() || p.IsInCombat || p.IsRagdoll || p.IsFalling || p.IsClimbing || p.IsSwimming || Game.IsMissionActive || Game.IsPaused || !Game.Player.CanControlCharacter || GTA.Native.Function.Call<bool>(GTA.Native.Hash.IS_CUTSCENE_ACTIVE) || GTA.Native.Function.Call<bool>(GTA.Native.Hash.IS_PED_GETTING_INTO_A_VEHICLE,p) || Profile.Detect()!=Profile.Id || p.Position.DistanceTo(Pos(current))>4 || suppressInteraction?.Invoke()==true || inv.UsingItem || gate!=null && gate.Owner!="shop")return false;
            if(shops.Contains(current))return true;
            return Game.Player.Wanted.WantedLevel==0 && p.Position.DistanceTo(Pos(current))<=1.8f && (editorPoints?.Invoke()??Enumerable.Empty<EditorPoint>()).Any(point=>point.Kind==EditorPointKind.Grocery && point.Name==current.Name && Math.Abs(point.Pose.X-current.X)<.01f && Math.Abs(point.Pose.Y-current.Y)<.01f && Math.Abs(point.Pose.Z-current.Z)<.01f);
        }
        void Open(ShopDef s)
        {
            if(gate!=null && !gate.Acquire("shop"))return;
            current=s;
            try{if(!Accessible()){Close();return;}if(InterfaceOptions.GridInventory)grid.Open(s.Name);else{Rebuild();menu.Visible=true;}}
            catch{Close();throw;}
        }
        void Close(){grid.Close();menu.Visible=false;current=null;gate?.Release("shop");}

        void Rebuild()
        {
            int idx = menu.SelectedIndex;
            menu.Clear();
            Visuals.ClearItems(menu);
            menu.Name = current.Name + "   $" + Wallet.Cash;

            foreach (ItemDef def in inv.Defs.Values.Where(i => i.Price > 0))
            {
                ItemDef item = def;
                var row = new NativeItem(item.Name, item.Desc) { AltTitle = "$" + item.Price };
                Visuals.Badge(menu, row, item.Icon??item.Id);
                row.Activated += (sender, e) => {if(!Buy(item,1))Notify.Show("Compra nao confirmada. Confira saldo, peso e RPCore.log.");};
                menu.Add(row);
            }
            menu.SelectedIndex = Math.Max(0, Math.Min(idx, menu.Items.Count - 1));
        }

        bool Buy(ItemDef item,int quantity)
        {
            if(!Accessible()){Close();return false;}
            if(!inv.Defs.TryGetValue(item.Id,out var actual) || !ReferenceEquals(item,actual) || quantity<1 || quantity>inv.PurchaseMaximum(item))return false;
            if(!inv.BuySaved(item,quantity))return false;
            try{Sfx.Play("buy");ach.Bump("items_bought",quantity);ach.Bump("cash_spent",(int)ShopCatalogRules.Total(item,quantity));}catch(Exception ex){Log.Error("purchase presentation",ex);}
            menu.Name=current.Name+"   $"+Wallet.Cash;return true;
        }

        public void Tick()
        {
            if(current!=null)
            {
                if(!Accessible() || !grid.Visible && !menu.Visible){Close();return;}
                if(grid.Visible){grid.Tick();if(!grid.Visible)Close();return;}
                return;
            }
            if (suppressInteraction?.Invoke() == true || inv.UsingItem || gate?.Owner!=null) return;
            if (InterfaceOptions.ModalOpen) return;
            Ped p = Game.Player.Character;
            Vector3 pos = p.Position;

            if (menu.Visible || pool.AreAnyVisible || p.IsInVehicle() || p.IsDead || Game.IsMissionActive) return;

            if (WorkContext.ShopDelivery.Contains(pos.X,pos.Y,pos.Z)) return;
            foreach (ShopDef s in shops.Concat((editorPoints?.Invoke()??Enumerable.Empty<EditorPoint>()).Where(p=>p.Kind==EditorPointKind.Grocery).Select(p=>new ShopDef{Name=p.Name,X=p.Pose.X,Y=p.Pose.Y,Z=p.Pose.Z})))
            {
                if(!shops.Contains(s) && Game.Player.Wanted.WantedLevel>0)continue;
                Vector3 sp = Pos(s);
                float dist = pos.DistanceTo(sp);
                if (dist > 30f) continue;

                World.DrawMarker(MarkerType.Cylinder, sp - new Vector3(0f, 0f, 1f), Vector3.Zero, Vector3.Zero,
                    new Vector3(1.2f, 1.2f, 1f), Color.FromArgb(110, 80, 200, 120));

                if (dist < 1.8f)
                {
                    Prompt.Help("Press ~INPUT_CONTEXT~ to browse " + s.Name);
                    if (Game.IsControlJustPressed(GTA.Control.Context)) Open(s);
                }
            }
        }

        public void OnKey(KeyEventArgs e) {if(grid.Visible){grid.OnKey(e);if(!grid.Visible)Close();}}
        public void Reload() {Close();}
        public void Save() { }

        public void Dispose()
        {
            Close();
            foreach (Blip b in blips) if (b != null && b.Exists()) b.Delete();
            blips.Clear();
        }
    }
}
