using System;
using System.Drawing;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using LemonUI;

namespace RPCore
{
    // Own input/action controller; artwork is supplied by the original OIV.
    public sealed class RadialSystem : ISystem
    {
        public string Name { get { return "radial"; } }
        readonly ObjectPool pool;
        readonly Action<int> activate;
        static readonly string[] labels = ReferenceInterface.RadialLabels;
        static readonly string[] icons = ReferenceInterface.RadialIcons;
        static readonly PointF[] centers = { new PointF(966,360), new PointF(1090,420), new PointF(1136,545), new PointF(1086,675), new PointF(952,720), new PointF(825,665), new PointF(780,525), new PointF(836,410) };
        int selected;
        public bool Visible { get; private set; }
        public RadialSystem(ObjectPool pool, Action<int> activate) { this.pool = pool; this.activate = activate; }
        void Close() { Visible = false; }
        void Select() { int action = selected; Close(); activate(action); }
        public void OnKey(KeyEventArgs e)
        {
            if (e.KeyCode == InterfaceOptions.RadialKey)
            {
                if (Visible) Close();
                else if (InterfaceOptions.RadialEnabled && !pool.AreAnyVisible && !Game.IsMissionActive) { Visible = true; selected = 0; }
                return;
            }
            if (!Visible) return;
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Back) Close();
            else if (e.KeyCode == Keys.Q || e.KeyCode == Keys.Left) selected = HudMath.MoveRadial(selected, -1);
            else if (e.KeyCode == Keys.E || e.KeyCode == Keys.Right) selected = HudMath.MoveRadial(selected, 1);
            else if (e.KeyCode == Keys.Enter) Select();
        }
        public void Tick()
        {
            if (!Visible) return;
            var p = Game.Player.Character;
            if (!InterfaceOptions.RadialEnabled || Game.IsPaused || p.IsDead || Game.IsMissionActive || pool.AreAnyVisible || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE)) { Close(); return; }
            foreach (var control in new[] { GTA.Control.Attack, GTA.Control.Aim, GTA.Control.SelectWeapon, GTA.Control.Cover, GTA.Control.Context, GTA.Control.Phone, GTA.Control.FrontendPause, GTA.Control.FrontendPauseAlternate }) Game.DisableControlThisFrame(control);
            if (!Function.Call<bool>(Hash.IS_USING_KEYBOARD_AND_MOUSE, 0))
            {
                if (Game.IsControlJustPressed(GTA.Control.FrontendLeft)) selected = HudMath.MoveRadial(selected, -1);
                if (Game.IsControlJustPressed(GTA.Control.FrontendRight)) selected = HudMath.MoveRadial(selected, 1);
                if (Game.IsControlJustPressed(GTA.Control.FrontendAccept)) { Select(); return; }
                if (Game.IsControlJustPressed(GTA.Control.FrontendCancel)) { Close(); return; }
            }
            float s = Math.Min(Visuals.Scale, 1.2f), x = (Visuals.Width - 1920 * s) / 2, y = (1080 - 1080 * s) / 2;
            bool ready = Visuals.Has("PSRP_Inventory", "RadMenu8");
            if (ready)
            {
                for (int i = 0; i < 8; i++) Visuals.Sprite("PSRP_Inventory", "RadMenu" + (i + 1), x, y, 1920 * s, 1080 * s, i == selected ? Color.FromArgb(210,255,0,0) : Color.FromArgb(170,0,0,0));
                Visuals.Sprite("PSRP_Inventory", "RadMiddle", x, y, 1920 * s, 1080 * s, Color.FromArgb(225,0,0,0));
            }
            else Visuals.Rect(Visuals.Width / 2 - 280 * s, 280, 560 * s, 490 * s, Color.FromArgb(190,0,0,0));
            for (int i = 0; i < 8; i++)
            {
                float ix = x + (centers[i].X - 35) * s, iy = y + (centers[i].Y - 35) * s;
                if (!ready || !Visuals.Sprite("PSRP_Inventory", icons[i], ix, iy, 70 * s, 70 * s, Color.White))
                    Visuals.Text((i == selected ? "~r~" : "") + labels[i], ix - 20 * s, iy + 15 * s, .23f, Color.White);
            }
            new GTA.UI.TextElement(labels[selected], new PointF(Visuals.Width / 3, (y + 510 * s) * 2 / 3), .5f * s, Color.White, GTA.UI.Font.Pricedown, GTA.UI.Alignment.Center).ScaledDraw();
            new GTA.UI.TextElement("Q / E: selecionar~n~Enter: abrir~n~" + InterfaceOptions.RadialKey + " / Esc: fechar", new PointF(Visuals.Width / 3, (y + 800 * s) * 2 / 3), .35f * s, Color.White, GTA.UI.Font.ChaletLondon, GTA.UI.Alignment.Center).ScaledDraw();
        }
        public void Reload() { Close(); }
        public void Save() { }
        public void Dispose() { Close(); }
    }
}
