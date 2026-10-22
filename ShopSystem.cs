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
        readonly InventorySystem inv;
        readonly AchievementSystem ach;
        readonly NativeMenu menu = new NativeMenu("Store", "");
        readonly List<ShopDef> shops;
        readonly List<Blip> blips = new List<Blip>();
        ShopDef current;

        static Vector3 Pos(ShopDef s) { return new Vector3(s.X, s.Y, s.Z); }

        public ShopSystem(ObjectPool pool, InventorySystem inv, AchievementSystem ach)
        {
            this.pool = pool;
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
                Visuals.Badge(menu, row, item.Id);
                row.Activated += (sender, e) => Buy(item);
                menu.Add(row);
            }
            menu.SelectedIndex = Math.Max(0, Math.Min(idx, menu.Items.Count - 1));
        }

        void Buy(ItemDef item)
        {
            if (Wallet.Cash < item.Price) { Notify.Show("~r~Not enough cash."); return; }
            if (!inv.Add(item.Id)) { Notify.Show("~r~Too heavy, can't carry that."); return; }
            if (!Wallet.Debit(item.Price)) { inv.Remove(item.Id); return; }
            Sfx.Play("buy");
            ach.Bump("items_bought");
            ach.Bump("cash_spent", item.Price);
            menu.Name = current.Name + "   $" + Wallet.Cash;
        }

        public void Tick()
        {
            if (InterfaceOptions.ModalOpen) return;
            Ped p = Game.Player.Character;
            Vector3 pos = p.Position;

            if (menu.Visible && current != null && pos.DistanceTo(Pos(current)) > 4f) menu.Visible = false;
            if (menu.Visible || pool.AreAnyVisible || p.IsInVehicle() || p.IsDead || Game.IsMissionActive) return;

            foreach (ShopDef s in shops)
            {
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
        public void Reload() { }
        public void Save() { }

        public void Dispose()
        {
            foreach (Blip b in blips) if (b != null && b.Exists()) b.Delete();
            blips.Clear();
        }
    }
}
