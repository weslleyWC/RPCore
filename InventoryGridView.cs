using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Native;

namespace RPCore
{
    // Rendering/controller only: quantities, use effects and transfers remain in InventorySystem.
    public sealed class InventoryGridView
    {
        readonly InventorySystem inventory;
        readonly NeedsSystem needs;
        int selection, moving = -1, dropStarted;
        string dropping;
        public bool Visible { get; private set; }
        public InventoryGridView(InventorySystem inventory, NeedsSystem needs) { this.inventory = inventory; this.needs = needs; }
        string Selected { get { return selection >= 0 && selection < inventory.Layout.Count ? inventory.Layout[selection] : null; } }
        public void Open() { Visible = true; selection = Math.Min(selection, inventory.Layout.Count - 1); moving = -1; dropping = null; }
        public void Close(bool save = true) { Visible = false; dropping = null; moving = -1; if (save) inventory.Save(); }
        void Move(int dx, int dy) { selection = HudMath.MoveGrid(selection, dx, dy, 5, inventory.Layout.Count); }
        void Use() { if (Selected != null) inventory.UseSelected(Selected); }
        void Relocate()
        {
            if (moving < 0) { if (Selected != null) moving = selection; return; }
            InventoryLayout.Swap(inventory.Layout, moving, selection); moving = -1; inventory.Save();
        }
        void Discard(bool stack)
        {
            string id = dropping; dropping = null;
            if (id == null) return;
            int amount = stack ? inventory.Count(id) : 1;
            if (inventory.Remove(id, amount)) { inventory.Save(); Notify.Show("Discarded " + amount + " x " + id); }
        }
        public void OnKey(KeyEventArgs e)
        {
            if (e.KeyCode == Cfg.InventoryKey || e.KeyCode == Keys.Back || e.KeyCode == Keys.Escape) { Close(); return; }
            if (dropping != null) return;
            if (e.KeyCode == Keys.Left) Move(-1, 0);
            else if (e.KeyCode == Keys.Right) Move(1, 0);
            else if (e.KeyCode == Keys.Up) Move(0, -1);
            else if (e.KeyCode == Keys.Down) Move(0, 1);
            else if (e.KeyCode == Keys.PageDown) selection = Math.Min(inventory.Layout.Count - 1, selection + 25);
            else if (e.KeyCode == Keys.PageUp) selection = Math.Max(0, selection - 25);
            else if (e.KeyCode == Keys.Enter && moving < 0) Use();
            else if (e.KeyCode == Keys.C || (e.KeyCode == Keys.Enter && moving >= 0)) Relocate();
            else if (e.KeyCode == Keys.Delete && Selected != null) { dropping = Selected; dropStarted = Game.GameTime; }
        }
        public void Tick()
        {
            if (!Visible) return;
            var p = Game.Player.Character;
            selection = Math.Max(0,Math.Min(selection,inventory.Layout.Count-1));
            if (moving >= inventory.Layout.Count) moving = -1;
            if (p.IsDead || Game.IsPaused || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE)) { Close(); return; }
            foreach (var control in new[] { GTA.Control.Attack, GTA.Control.Aim, GTA.Control.SelectWeapon, GTA.Control.Context, GTA.Control.Phone, GTA.Control.FrontendPause, GTA.Control.FrontendPauseAlternate }) Game.DisableControlThisFrame(control);
            if (dropping != null)
            {
                if (!Game.IsKeyPressed(Keys.Delete)) Discard(false);
                else if (unchecked(Game.GameTime - dropStarted) >= 900) Discard(true);
            }
            if (!Function.Call<bool>(Hash.IS_USING_KEYBOARD_AND_MOUSE, 0))
            {
                if (Game.IsControlJustPressed(GTA.Control.FrontendLeft)) Move(-1, 0);
                if (Game.IsControlJustPressed(GTA.Control.FrontendRight)) Move(1, 0);
                if (Game.IsControlJustPressed(GTA.Control.FrontendUp)) Move(0, -1);
                if (Game.IsControlJustPressed(GTA.Control.FrontendDown)) Move(0, 1);
                if (Game.IsControlJustPressed(GTA.Control.FrontendAccept)) { if (moving < 0) Use(); else Relocate(); }
                if (Game.IsControlJustPressed(GTA.Control.FrontendX)) Relocate();
                if (Game.IsControlJustPressed(GTA.Control.FrontendCancel)) { Close(); return; }
            }
            Draw();
        }
        void Draw()
        {
            // 5 x 5 cells as in download (3).png; the DLL's 6 x 4 crafting layout is a different revision.
            float s = Math.Min(Math.Min(1, Visuals.Scale),Math.Min((Visuals.Width-2*Visuals.InsetX)/1180,(1080-2*Visuals.InsetY)/1080)), ox = Visuals.InsetX, oy = Visuals.InsetY;
            Action<string,float,float,float,Color,GTA.UI.Alignment,float> text = (t,x,y,z,c,a,w) => Visuals.Label(t,ox+x*s,oy+y*s,z*s,c,GTA.UI.Font.ChaletLondon,a,w*s);
            var panel = new RectangleF(ox + 18*s,oy + 84*s,850*s,982*s);
            if (Visuals.Has("PSRP_Inventory", "Background")) Visuals.SpriteVisible("PSRP_Inventory", "Background", panel, ReferenceLayout.InventoryBackgroundBounds, Color.FromArgb(245,17,17,20));
            else Visuals.Rect(panel.X,panel.Y,panel.Width,panel.Height,Color.FromArgb(235,17,17,20));
            text("Total Weight: " + inventory.Weight.ToString("0.##") + "kg / " + Cfg.MaxWeight.ToString("0.##") + "kg",34,116,.32f,Color.White,GTA.UI.Alignment.Left,0);
            text("Hydration: " + HudMath.Percent(needs.Thirst),846,96,.26f,Color.White,GTA.UI.Alignment.Right,0);
            text("Hunger: " + HudMath.Percent(needs.Hunger),846,115,.26f,Color.White,GTA.UI.Alignment.Right,0);
            text("Player Cash: ~y~$" + Wallet.Cash,846,134,.26f,Color.White,GTA.UI.Alignment.Right,0);
            int page = selection / 25;
            for (int i = 0; i < 25; i++)
            {
                int index = page*25+i; var cell = ReferenceLayout.Cell(i); string id = index < inventory.Layout.Count ? inventory.Layout[index] : null;
                var rect = new RectangleF(ox+cell.X*s,oy+cell.Y*s,cell.Width*s,cell.Height*s);
                Color color = index == selection ? Color.FromArgb(245,0,215,0) : id == null ? Color.FromArgb(165,43,43,105) : Color.FromArgb(190,0,0,0);
                if (Visuals.Has("PSRP_Inventory","InventoryBox")) Visuals.SpriteVisible("PSRP_Inventory","InventoryBox",rect,ReferenceLayout.CellBounds,color);
                else Visuals.Rect(rect.X,rect.Y,rect.Width,rect.Height,color);
                if (id == null) continue;
                ItemDef def; inventory.Defs.TryGetValue(id,out def);
                text(def?.Name ?? id,cell.X+78,cell.Y+5,.28f,Color.White,GTA.UI.Alignment.Center,145);
                string icon; if (Visuals.Bindings.ItemIcons == null || !Visuals.Bindings.ItemIcons.TryGetValue(id,out icon)) icon = id;
                if (!Visuals.Sprite("PSRP_Inventory",icon,ox+(cell.X+22)*s,oy+(cell.Y+32)*s,112*s,112*s,Color.White))
                    text("?",cell.X+78,cell.Y+58,.7f,Color.Gray,GTA.UI.Alignment.Center,0);
                text("x"+inventory.Count(id),cell.X+7,cell.Y+130,.23f,Color.White,GTA.UI.Alignment.Left,0);
                text(((def?.Weight ?? .1f)*inventory.Count(id)).ToString("0.##")+"kg",cell.X+149,cell.Y+130,.23f,Color.White,GTA.UI.Alignment.Right,0);
                if (moving == index) Visuals.Rect(rect.X,rect.Y+rect.Height-4*s,rect.Width,4*s,Color.Yellow);
            }
            string current = Selected;
            if (current != null)
            {
                ItemDef def; inventory.Defs.TryGetValue(current,out def);
                Visuals.Rect(ox+886*s,oy+164*s,270*s,280*s,Color.FromArgb(200,0,0,0));
                text(def?.Name ?? current,900,177,.32f,Color.White,GTA.UI.Alignment.Left,246);
                text("Amount in Inventory: "+inventory.Count(current),900,211,.28f,Color.White,GTA.UI.Alignment.Left,246);
                text("Durability: n/a",900,239,.28f,Color.White,GTA.UI.Alignment.Left,246);
                text("Description: " + (def?.Desc ?? "Definition missing; item preserved."),900,275,.28f,Color.White,GTA.UI.Alignment.Left,246);
                if (def != null) text("~y~+ "+def.Thirst.ToString("0.#")+" hydration~n~+ "+def.Hunger.ToString("0.#")+" hunger~n~+ "+def.Health+" health",900,359,.27f,Color.White,GTA.UI.Alignment.Left,246);
            }
            text("Press ~y~ENTER~s~ to use~n~Press ~y~DELETE~s~ to delete (HOLD FOR STACK)",60,993,.25f,Color.White,GTA.UI.Alignment.Left,0);
            text("Press ~y~BACKSPACE~s~ to close~n~Press ~y~C~s~ to move item",452,993,.25f,Color.White,GTA.UI.Alignment.Left,0);
            if (moving >= 0) text("Choose a slot, then C / Enter",452,1040,.23f,Color.Yellow,GTA.UI.Alignment.Left,0);
            if (inventory.Layout.Count > 25) text("Page "+(page+1)+" / "+(inventory.Layout.Count/25)+"  PgUp/PgDn",60,1040,.23f,Color.White,GTA.UI.Alignment.Left,0);
        }
    }
}
