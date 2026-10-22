using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Native;

namespace RPCore
{
    public class VisualSystem : ISystem
    {
        public string Name { get { return "visuals"; } }
        readonly SeatbeltSystem belt;
        readonly Func<bool> cruise;
        readonly Func<Vehicle, FuelTank> fuel;
        int diagnosticEnd, nextDiagnostic;
        readonly int started;
        bool warned;
        public VisualSystem(SeatbeltSystem belt, Func<bool> cruise, Func<Vehicle, FuelTank> fuel)
        {
            this.belt = belt; this.cruise = cruise; this.fuel = fuel; Visuals.Running = true; started = Game.GameTime;
        }
        public void Diagnose()
        {
            diagnosticEnd = Game.GameTime + 6000; nextDiagnostic = Game.GameTime;
            Notify.Show("Checking PSRP textures/models. Result in RPCore.log in 6 seconds.");
        }
        void Diagnostics()
        {
            if (diagnosticEnd == 0) return;
            if (unchecked(Game.GameTime - nextDiagnostic) >= 0)
            {
                nextDiagnostic = Game.GameTime + 1000;
                if (Visuals.Catalog.Dictionaries != null) foreach (var dictionary in Visuals.Catalog.Dictionaries.Keys) Visuals.Ready(dictionary);
            }
            if (unchecked(Game.GameTime - diagnosticEnd) < 0) return;
            diagnosticEnd = 0; int loaded = 0, registered = 0;
            if (Visuals.Catalog.Dictionaries != null) foreach (var dictionary in Visuals.Catalog.Dictionaries.Keys)
            {
                bool ready = Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, dictionary); if (ready) loaded++;
                Log.Write("VISUAL dict " + dictionary + ": " + (ready ? "loaded" : "not loaded; check OIV in mods/update/update.rpf"));
            }
            if (Visuals.Catalog.Models != null) foreach (var name in Visuals.Catalog.Models)
            {
                Model m = new Model(name); bool valid = m.IsValid && m.IsInCdImage; if (valid) registered++;
                Log.Write("VISUAL model " + name + ": " + (valid ? "registered (render not tested)" : "unavailable; check psrp_props DLC registration"));
            }
            Log.Write("VISUAL enabled=" + Visuals.Enabled + ", active=" + Visuals.Active + ", aspect=" + GTA.UI.Screen.AspectRatio);
            Notify.Show("PSRP: " + loaded + "/8 dictionaries; " + registered + "/27 models registered. See RPCore.log.");
        }
        public void Tick()
        {
            Diagnostics(); Visuals.TickMenus();
            if (Visuals.Active && !warned && unchecked(Game.GameTime - started) > 10000 && !Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, "PSRP"))
            {
                warned = true; Log.Write("VISUAL PSRP texture dictionary not loaded: install PSRP.oiv into OpenIV mods. Basic HUD retained.");
                Notify.Show("PSRP visual assets not loaded. Basic HUD active. F10 > Visual assets: diagnose.");
            }
            if (!Visuals.Active || Game.IsPaused || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) || Function.Call<bool>(Hash.IS_HUD_HIDDEN)) return;
            var p = Game.Player.Character; if (p.IsDead) return;
            if (Visuals.WalletHud && Visuals.Has("PSRP", "MoneyIcon"))
            {
                float x = Visuals.Width - Visuals.InsetX - 244 * Visuals.Scale, y = Visuals.InsetY + Visuals.NeedsOffsetY + 92 * Visuals.Scale;
                Visuals.Rect(x - 5, y - 3, 230 * Visuals.Scale, 58 * Visuals.Scale, Color.FromArgb(150, 12, 16, 23));
                Visuals.Sprite("PSRP", "MoneyIcon", x, y, 25 * Visuals.Scale, 25 * Visuals.Scale, Color.LightGreen);
                Visuals.Text("$" + Wallet.Cash.ToString("N0"), x + 34 * Visuals.Scale, y, .3f, Color.White);
                Visuals.Text("Bank  $" + Wallet.Bank.ToString("N0"), x + 34 * Visuals.Scale, y + 27 * Visuals.Scale, .25f, Color.LightBlue);
            }
            if (!Visuals.VehicleReady || !p.IsInVehicle()) return;
            var v = p.CurrentVehicle;
            if (v == null || v.Driver != p || !(v.Model.IsCar || v.Model.IsBike || v.Model.IsQuadBike) || v.Model.IsBicycle) return;
            DrawVehicle(v);
        }
        void DrawVehicle(Vehicle v)
        {
            float s = Visuals.Scale, x = Visuals.Width - Visuals.InsetX - 360 * s, y = 1080 - Visuals.InsetY - 225 * s;
            Visuals.Rect(x + 8 * s, y + 8 * s, 320 * s, 144 * s, Color.FromArgb(155, 8, 12, 18));
            Visuals.Sprite("PSRP", "RectangleBorder", x, y, 336 * s, 163 * s, Color.FromArgb(210, 240, 245, 255));
            Visuals.Text((v.Speed * 3.6f).ToString("0"), x + 35 * s, y + 20 * s, .9f, Color.White);
            Visuals.Text("km/h", x + 145 * s, y + 55 * s, .32f, Color.White);
            Visuals.Text(v.GetSpeedVector(true).Y < -.5f ? "R" : v.CurrentGear > 0 ? v.CurrentGear.ToString() : "N", x + 255 * s, y + 32 * s, .6f, Color.LightBlue);
            float rpm = Math.Max(0, Math.Min(1, v.CurrentRPM));
            Visuals.Rect(x + 34 * s, y + 106 * s, 250 * s * rpm, 4 * s, rpm > .85f ? Color.OrangeRed : Color.LightBlue);
            Visuals.Sprite("PSRP", "SpeedoLine", x + 34 * s, y + 97 * s, 250 * s, 16 * s, Color.White);
            FuelTank tank = fuel(v);
            if (tank != null)
            {
                Visuals.Sprite("PSRP", "CarFuelIcon", x + 14 * s, y + 173 * s, 25 * s, 25 * s, tank.Liters / tank.Capacity < .1f ? Color.OrangeRed : Color.White);
                Visuals.Text(tank.Liters.ToString("0.0") + "/" + tank.Capacity.ToString("0") + " L | " + tank.DistanceKm.ToString("0.0") + " km", x + 47 * s, y + 173 * s, .27f, Color.White);
            }
            float row = y + 124 * s;
            Visuals.Sprite("PSRP", "CarSeatbeltIcon", x + 37 * s, row, 24 * s, 24 * s, belt.IsBelted ? Color.LightGreen : Color.OrangeRed);
            Visuals.Sprite("PSRP", "CarCruiseIcon", x + 88 * s, row, 24 * s, 24 * s, cruise() ? Color.LightGreen : Color.Gray);
            Visuals.Sprite("PSRP", "CarHeadLightIcon", x + 139 * s, row, 24 * s, 24 * s, v.AreLightsOn ? Color.LightGreen : Color.Gray);
            Visuals.Sprite("PSRP", "CarOilIcon", x + 190 * s, row, 24 * s, 24 * s, v.EngineHealth < 300 ? Color.OrangeRed : Color.Gray);
            bool flat = new[] { 0, 1, 2, 3, 4, 5, 45, 47 }.Any(i => Function.Call<bool>(Hash.IS_VEHICLE_TYRE_BURST, v, i, false));
            Visuals.Sprite("PSRP", "FlatTireIcon", x + 241 * s, row, 24 * s, 24 * s, flat ? Color.OrangeRed : Color.Gray);
            if ((Game.GameTime / 500) % 2 == 0)
            {
                if (v.IsLeftIndicatorLightOn) Visuals.Sprite("PSRP", "LBlinker", x + 5 * s, y + 55 * s, 24 * s, 24 * s, Color.LightGreen);
                if (v.IsRightIndicatorLightOn) Visuals.Sprite("PSRP", "Rblinker", x + 301 * s, y + 55 * s, 24 * s, 24 * s, Color.LightGreen);
            }
        }
        public void OnKey(KeyEventArgs e) { }
        public void Reload() { }
        public void Save() { }
        public void Dispose() { Visuals.Dispose(); }
    }
}
