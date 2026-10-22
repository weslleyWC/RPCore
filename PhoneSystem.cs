using System;
using System.Drawing;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using LemonUI;

namespace RPCore
{
    public class PhoneSystem : ISystem
    {
        public string Name { get { return "phone"; } }
        readonly ObjectPool pool;
        readonly Action<Keys> dispatch;
        readonly PropPresentation props;
        readonly string[] names = { "Carteira", "Inventario", "Garagens", "Bolsa", "Radio", "Gangues", "Pizza", "Config.", "Fechar" };
        readonly string[] icons = { "WalletIcon", "IconID", "IconCar", "StockIconPhone", "MusicIcon", "GangsIcon", "JobsIcon", "SettingsIcon", "IconHome" };
        int selection;
        public bool Visible { get; private set; }
        public PhoneSystem(ObjectPool pool, Action<Keys> dispatch, PropPresentation props) { this.pool = pool; this.dispatch = dispatch; this.props = props; }
        void Close()
        {
            Visible = false; props?.ClosePhone();
            Game.DisableControlThisFrame(GTA.Control.FrontendPause); Game.DisableControlThisFrame(GTA.Control.FrontendPauseAlternate);
        }
        void Activate()
        {
            if (selection == 8) { Close(); return; }
            if ((selection == 2 && !Cfg.EnableGarages) || (selection == 4 && !Cfg.EnableRadio) || (selection == 5 && !Cfg.EnableGangs)) { Notify.Show("This system is disabled in your configuration."); return; }
            Keys key = selection == 0 ? Cfg.CoreKey : selection == 1 ? Cfg.InventoryKey : selection == 2 ? Cfg.GarageKey : selection == 3 ? Cfg.StocksKey : selection == 4 ? Cfg.RadioKey : selection == 5 ? Cfg.GangsKey : selection == 6 ? Cfg.JobKey : InterfaceOptions.SettingsKey;
            Close(); dispatch(key);
        }
        void Navigate(int dx, int dy) { selection = HudMath.MoveGrid(selection, dx, dy, 3, names.Length); }
        public void OnKey(KeyEventArgs e)
        {
            if (e.KeyCode == Visuals.PhoneKey)
            {
                if (Visible) { Close(); return; }
                var p = Game.Player.Character;
                if (!Visuals.Enabled || !Visuals.PhoneEnabled || pool.AreAnyVisible || p.IsDead || p.IsInVehicle() || Game.IsMissionActive) return;
                Visible = true; props?.ShowPhone(); return;
            }
            if (!Visible) return;
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Back) Close();
            else if (e.KeyCode == Keys.Left) Navigate(-1, 0);
            else if (e.KeyCode == Keys.Right) Navigate(1, 0);
            else if (e.KeyCode == Keys.Up) Navigate(0, -1);
            else if (e.KeyCode == Keys.Down) Navigate(0, 1);
            else if (e.KeyCode == Keys.Enter) Activate();
        }
        public void Tick()
        {
            if (!Visible) return;
            var p = Game.Player.Character;
            if (!Visuals.Enabled || !Visuals.PhoneEnabled || p.IsDead || p.IsInVehicle() || Game.IsMissionActive || pool.AreAnyVisible || Game.IsPaused || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE)) { Close(); return; }
            Game.DisableControlThisFrame(GTA.Control.Attack); Game.DisableControlThisFrame(GTA.Control.Aim); Game.DisableControlThisFrame(GTA.Control.Phone);
            Game.DisableControlThisFrame(GTA.Control.FrontendPause); Game.DisableControlThisFrame(GTA.Control.FrontendPauseAlternate);
            if (!Function.Call<bool>(Hash.IS_USING_KEYBOARD_AND_MOUSE, 0))
            {
                if (Game.IsControlJustPressed(GTA.Control.FrontendLeft)) Navigate(-1, 0);
                if (Game.IsControlJustPressed(GTA.Control.FrontendRight)) Navigate(1, 0);
                if (Game.IsControlJustPressed(GTA.Control.FrontendUp)) Navigate(0, -1);
                if (Game.IsControlJustPressed(GTA.Control.FrontendDown)) Navigate(0, 1);
                if (Game.IsControlJustPressed(GTA.Control.FrontendAccept)) { Activate(); return; }
                if (Game.IsControlJustPressed(GTA.Control.FrontendCancel)) { Close(); return; }
            }
            Draw();
        }
        void Draw()
        {
            float s = Visuals.Scale, x = Visuals.Width - Visuals.InsetX - 260 * s, y = 1080 - Visuals.InsetY - 460 * s;
            // The source phone/wallpaper occupy (1662,621)-(1904,1062) within their full 1920x1080 textures.
            float fullX = x - 1662 * s, fullY = y - 621 * s;
            // PhoneTexture has an opaque black screen, so draw it BEFORE the wallpaper and apps.
            Visuals.Sprite("PSRP_PHONE", "PhoneTexture", fullX, fullY, 1920 * s, 1080 * s, Color.White);
            Visuals.Rect(x + 11 * s, y + 9 * s, 220 * s, 424 * s, Color.FromArgb(245, 18, 23, 36));
            bool wallpaper = Visuals.Sprite("PSRP_PHONE", "PhoneWallpaper1", fullX, fullY, 1920 * s, 1080 * s, Color.White);
            Visuals.Rect(x + 11 * s, y + 9 * s, 220 * s, 424 * s, Color.FromArgb(wallpaper ? 100 : 230, 5, 8, 18));
            Visuals.Text("RPCore 0.4.0", x + 23 * s, y + 24 * s, .25f, Color.White);
            Visuals.Text(GTA.Chrono.GameClock.Hour.ToString("00") + ":" + GTA.Chrono.GameClock.Minute.ToString("00"), x + 174 * s, y + 24 * s, .23f, Color.White);
            for (int i = 0; i < names.Length; i++)
            {
                float ix = x + (24 + (i % 3) * 65) * s, iy = y + (69 + (i / 3) * 86) * s;
                if (i == selection) Visuals.Rect(ix - 4 * s, iy - 5 * s, 59 * s, 77 * s, Color.FromArgb(180, 80, 145, 205));
                if (!Visuals.Sprite("PSRP_PHONE", icons[i], ix + 4 * s, iy, 42 * s, 42 * s, Color.White)) Visuals.Text((i + 1).ToString(), ix + 16 * s, iy + 5 * s, .4f, Color.White);
                Visuals.Text(names[i], ix - 1 * s, iy + 47 * s, .19f, Color.White);
            }
            Visuals.Text("$" + Wallet.Cash.ToString("N0"), x + 29 * s, y + 349 * s, .28f, Color.LightGreen);
            Visuals.Text("Setas + Enter / Esc", x + 29 * s, y + 384 * s, .20f, Color.White);
        }
        public void Reload() { Close(); }
        public void Save() { }
        public void Dispose() { Close(); }
    }
}
