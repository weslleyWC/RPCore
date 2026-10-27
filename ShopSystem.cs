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
        readonly Func<bool> suppressInteraction;readonly Func<IEnumerable<EditorPoint>> editorPoints;
        readonly InventorySystem inv;
        readonly AchievementSystem ach;
        readonly NativeMenu menu = new NativeMenu("Store", "");
        readonly List<ShopDef> shops;
        readonly List<Blip> blips = new List<Blip>();
        ShopDef current;

        static Vector3 Pos(ShopDef s) { return new Vector3(s.X, s.Y, s.Z); }

        public ShopSystem(ObjectPool pool, InventorySystem inv, AchievementSystem ach, Func<bool> suppressInteraction = null, Func<IEnumerable<EditorPoint>> editorPoints = null)
        {
            this.pool = pool;
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

        void Open(ShopDef s)
        {
            current = s;
            Rebuild();
            menu.Visible = true;
        }

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
                row.Activated += (sender, e) => Buy(item);
                menu.Add(row);
            }
            menu.SelectedIndex = Math.Max(0, Math.Min(idx, menu.Items.Count - 1));
        }

        void Buy(ItemDef item)
        {
            if(current!=null && !shops.Contains(current)){
                var actor=Game.Player.Character;bool here=(editorPoints?.Invoke()??Enumerable.Empty<EditorPoint>()).Any(p=>p.Kind==EditorPointKind.Grocery && p.Name==current.Name && Math.Abs(p.Pose.X-current.X)<.01f && Math.Abs(p.Pose.Y-current.Y)<.01f && Math.Abs(p.Pose.Z-current.Z)<.01f);
                if(!here || actor.IsInVehicle() || actor.IsDead || Game.IsMissionActive || Game.Player.Wanted.WantedLevel>0 || actor.Position.DistanceTo(Pos(current))>1.8f){menu.Visible=false;Notify.Show("Retorne a loja do mapa ativo, a pe e sem procurado.");return;}
            }
            var player=Game.Player.Character;
            if(current==null || player.IsDead || player.IsInVehicle() || Game.IsMissionActive || Game.IsPaused || Profile.Detect()!=Profile.Id || player.Position.DistanceTo(Pos(current))>4){menu.Visible=false;return;}
            if(!inv.BuySaved(item)){Notify.Show("Compra nao confirmada. Confira saldo, peso e RPCore.log.");return;}
            try{Sfx.Play("buy");ach.Bump("items_bought");ach.Bump("cash_spent", item.Price);}catch(Exception ex){Log.Error("purchase presentation",ex);}
            menu.Name = current.Name + "   $" + Wallet.Cash;
        }

        public void Tick()
        {
            if (suppressInteraction?.Invoke() == true) { menu.Visible = false; return; }
            if (InterfaceOptions.ModalOpen) return;
            Ped p = Game.Player.Character;
            Vector3 pos = p.Position;

            if (menu.Visible && current != null && pos.DistanceTo(Pos(current)) > 4f) menu.Visible = false;
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

        public void OnKey(KeyEventArgs e) { }
        public void Reload() { menu.Visible=false;current=null; }
        public void Save() { }

        public void Dispose()
        {
            foreach (Blip b in blips) if (b != null && b.Exists()) b.Delete();
            blips.Clear();
        }
    }
}
