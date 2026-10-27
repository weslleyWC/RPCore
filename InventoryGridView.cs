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
        public void Close(bool save = true) { Visible = false; dropping = null; moving = -1; if(save)try{inventory.Save();}catch(Exception ex){Log.Error("inventory close save",ex);Notify.Show("Nao foi possivel gravar o inventario; confira o log.");} }
        void Move(int dx, int dy) { selection = HudMath.MoveGrid(selection, dx, dy, RoleplayInventoryLayout.Columns, inventory.Layout.Count); }
        void Use() { if (Selected != null) inventory.UseSelected(Selected); }
        void Relocate()
        {
            if (moving < 0) { if (Selected != null) moving = selection; return; }
            inventory.MoveSlotSaved(moving,selection);moving=-1;
        }
        void Discard(bool stack)
        {
            string id = dropping; dropping = null;
            if (id == null) return;
            int amount = stack ? inventory.Count(id) : 1;
            if (inventory.DropSaved(id, amount)) { Notify.Show("Discarded " + amount + " x " + id); }
        }
        public void OnKey(KeyEventArgs e)
        {
            if (e.KeyCode == Cfg.InventoryKey || e.KeyCode == Keys.Back || e.KeyCode == Keys.Escape) { Close(); return; }
            if (dropping != null) return;
            if (e.KeyCode == Keys.Left) Move(-1, 0);
            else if (e.KeyCode == Keys.Right) Move(1, 0);
            else if (e.KeyCode == Keys.Up) Move(0, -1);
            else if (e.KeyCode == Keys.Down) Move(0, 1);
            else if (e.KeyCode == Keys.PageDown) selection = Math.Min(inventory.Layout.Count - 1, selection + 24);
            else if (e.KeyCode == Keys.PageUp) selection = Math.Max(0, selection - 24);
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
            // Current PSRP NewInventorySystem: six columns, four visible rows; saved item order is retained.
            float s = Math.Min(Math.Min(1, Visuals.Scale),Math.Min((Visuals.Width-2*Visuals.InsetX)/1320,(1080-2*Visuals.InsetY)/1080)), ox = Visuals.InsetX, oy = Visuals.InsetY;
            Action<string,float,float,float,Color,GTA.UI.Alignment,float> text = (t,x,y,z,c,a,w) => Visuals.Label(t,ox+x*s,oy+y*s,z*s,c,GTA.UI.Font.ChaletLondon,a,w*s);
            if(!Visuals.Sprite("PSRP_Inventory","Background",ox,oy,1920*s,1080*s,Color.FromArgb(245,17,17,20)))Visuals.Rect(ox+107*s,oy+126*s,853*s,758*s,Color.FromArgb(235,17,17,20));
            Visuals.Sprite("PSRP_Inventory","Top",ox,oy,1920*s,1080*s,Color.FromArgb(255,25,25,31));
            text("Inventory / "+inventory.Weight.ToString("0.##")+"kg / "+Cfg.MaxWeight.ToString("0.##")+"kg",140,165,.32f,Color.White,GTA.UI.Alignment.Left,650);
            text("Hydration: "+HudMath.Percent(needs.Thirst),930,173,.26f,Color.White,GTA.UI.Alignment.Right,0);
            text("Hunger: "+HudMath.Percent(needs.Hunger),930,196,.26f,Color.White,GTA.UI.Alignment.Right,0);
            text("Cash: $"+Wallet.Cash,930,219,.26f,Color.White,GTA.UI.Alignment.Right,0);
            int page = selection / 24;
            for (int i = 0; i < 24; i++)
            {
                int index = page*24+i; var cell = RoleplayInventoryLayout.Cell(i); string id = index < inventory.Layout.Count ? inventory.Layout[index] : null;
                var rect = new RectangleF(ox+cell.X*s,oy+cell.Y*s,cell.Width*s,cell.Height*s);
                Color color = index == selection ? Color.FromArgb(150,60,120,255) : Color.FromArgb(255,24,27,36);
                if (Visuals.Has("PSRP_Inventory","InventoryBox")) Visuals.Sprite("PSRP_Inventory","InventoryBox",rect.X,rect.Y,rect.Width,rect.Height,color);
                else Visuals.Rect(rect.X,rect.Y,rect.Width,rect.Height,color);
                if (id == null) continue;
                ItemDef def; inventory.Defs.TryGetValue(id,out def);
                text(def?.Name ?? id,cell.X+67.5f,cell.Y+14.5f,.28f,Color.White,GTA.UI.Alignment.Center,125);
                string icon; if (Visuals.Bindings.ItemIcons == null || !Visuals.Bindings.ItemIcons.TryGetValue(id,out icon)) icon = def?.Icon ?? id;
                if (!Visuals.Sprite("PSRP_Inventory",icon,ox+(cell.X+28)*s,oy+(cell.Y+34)*s,80*s,80*s,Color.White))
                    text("?",cell.X+67.5f,cell.Y+58,.7f,Color.Gray,GTA.UI.Alignment.Center,0);
                text("x"+inventory.Count(id),cell.X+17,cell.Y+107,.23f,Color.White,GTA.UI.Alignment.Left,0);
                text(((def?.Weight ?? .1f)*inventory.Count(id)).ToString("0.##")+"kg",cell.X+122,cell.Y+107,.23f,Color.White,GTA.UI.Alignment.Right,0);
                if (moving == index) Visuals.Rect(rect.X,rect.Y+rect.Height-4*s,rect.Width,4*s,Color.Yellow);
            }
            string current = Selected;
            if (current != null)
            {
                ItemDef def; inventory.Defs.TryGetValue(current,out def);
                Visuals.Rect(ox+1000*s,oy+230*s,270*s,280*s,Color.FromArgb(200,0,0,0));
                text(def?.Name ?? current,1014,245,.32f,Color.White,GTA.UI.Alignment.Left,246);
                text("Amount in Inventory: "+inventory.Count(current),1014,279,.28f,Color.White,GTA.UI.Alignment.Left,246);
                text(def?.Weapon!=null ? "ENTER: equipar / guardar" : "Durability: n/a",1014,307,.28f,Color.White,GTA.UI.Alignment.Left,246);
                text("Description: " + (def?.Desc ?? "Definition missing; item preserved."),1014,343,.28f,Color.White,GTA.UI.Alignment.Left,246);
                if (def != null && def.Weapon==null && !current.StartsWith("ammo_",StringComparison.Ordinal)) text("~y~+ "+def.Thirst.ToString("0.#")+" hydration~n~+ "+def.Hunger.ToString("0.#")+" hunger~n~+ "+def.Health+" health",1014,427,.27f,Color.White,GTA.UI.Alignment.Left,246);
            }
            text("Press ~y~ENTER~s~ to use~n~Press ~y~DELETE~s~ to delete (HOLD FOR STACK)",140,805,.25f,Color.White,GTA.UI.Alignment.Left,0);
            text("Press ~y~BACKSPACE~s~ to close~n~Press ~y~C~s~ to move item",540,805,.25f,Color.White,GTA.UI.Alignment.Left,0);
            if (moving >= 0) text("Choose a slot, then C / Enter",540,855,.23f,Color.Yellow,GTA.UI.Alignment.Left,0);
            if (inventory.Layout.Count > 24) text("Page "+(page+1)+" / "+(inventory.Layout.Count/24)+"  PgUp/PgDn",140,855,.23f,Color.White,GTA.UI.Alignment.Left,0);
        }
    }
}
