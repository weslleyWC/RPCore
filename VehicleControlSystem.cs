using System;
using System.Drawing;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;

namespace RPCore
{
    public class VehicleControlSystem : ISystem
    {
        public string Name { get { return "vehiclecontrols"; } }
        readonly ObjectPool pool;
        readonly NativeMenu menu = new NativeMenu("Vehicle", "");
        readonly Func<Vehicle, bool> hasFuel;
        Vehicle controlled;
        bool engineOff, cruise, left, right, indicatorsChanged;
        int originalIndicators;
        float cruiseSpeed;
        public bool IsCruising { get { return cruise; } }

        public VehicleControlSystem(ObjectPool pool, Func<Vehicle, bool> hasFuel)
        {
            this.pool = pool; this.hasFuel = hasFuel; pool.Add(menu); Visuals.Register(menu, "vehicle");
            Add("Engine on/off", ToggleEngine); Add("Cruise control on/off", ToggleCruise);
            Add("Left indicator", () => Signal(true)); Add("Right indicator", () => Signal(false));
            Add("Hazard lights", Hazards);
            Add("Hood", () => Door(4)); Add("Trunk", () => Door(5));
        }
        void Add(string title, Action action) { var row = new NativeItem(title); row.Activated += (s, e) => { if (Cfg.EnableVehicleControls && !Game.IsMissionActive && Attach()) action(); }; menu.Add(row); }
        bool Attach()
        {
            var p = Game.Player.Character;
            var v = p.IsInVehicle() ? p.CurrentVehicle : null;
            if (p.IsDead || v == null || !v.Exists() || v.Driver != p || v.IsDead || v.Model.IsBicycle || v.Model.IsTrain || !(v.Model.IsCar || v.Model.IsBike || v.Model.IsQuadBike)) return false;
            if (controlled == null || !controlled.Exists() || controlled.Handle != v.Handle)
            {
                Release(); controlled = v;
                originalIndicators = (v.IsLeftIndicatorLightOn ? 1 : 0) | (v.IsRightIndicatorLightOn ? 2 : 0);
                left = (originalIndicators & 1) != 0; right = (originalIndicators & 2) != 0;
            }
            return true;
        }
        void ToggleEngine()
        {
            if (controlled.IsEngineRunning)
            {
                engineOff = true; cruise = false;
                Function.Call(Hash.SET_VEHICLE_ENGINE_ON, controlled, false, true, true);
                Notify.Show("Engine off.");
            }
            else
            {
                if (!hasFuel(controlled)) { Notify.Show("Tank empty. Refuel first."); return; }
                engineOff = false; Function.Call(Hash.SET_VEHICLE_ENGINE_ON, controlled, true, true, false); Notify.Show("Engine on.");
            }
        }
        void ToggleCruise()
        {
            if (cruise) { CancelCruise(); return; }
            if (!controlled.IsEngineRunning || !hasFuel(controlled) || controlled.Speed < 8 || controlled.GetSpeedVector(true).Y <= 0 || !controlled.IsOnAllWheels)
            { Notify.Show("Cruise requires forward driving above 29 km/h on the road."); return; }
            cruiseSpeed = controlled.Speed; cruise = true; Notify.Show("Cruise: " + (cruiseSpeed * 3.6f).ToString("0") + " km/h. Brake, accelerate or press " + Cfg.CruiseKey + " to cancel.");
        }
        void CancelCruise() { if (cruise) Notify.Show("Cruise cancelled."); cruise = false; }
        void Signal(bool useLeft)
        {
            bool wasOn = useLeft ? left : right; left = useLeft && !wasOn; right = !useLeft && !wasOn; SetSignals();
        }
        void Hazards() { bool on = !(left && right); left = right = on; SetSignals(); }
        void SetSignals()
        {
            Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS, controlled, 1, left);
            Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS, controlled, 0, right);
            indicatorsChanged = true;
        }
        void Door(int index)
        {
            if (controlled.Speed > .5f || !controlled.Model.IsCar) { Notify.Show("Stop the car first."); return; }
            float angle = Function.Call<float>(Hash.GET_VEHICLE_DOOR_ANGLE_RATIO, controlled, index);
            if (angle > .1f) Function.Call(Hash.SET_VEHICLE_DOOR_SHUT, controlled, index, false);
            else Function.Call(Hash.SET_VEHICLE_DOOR_OPEN, controlled, index, false, false);
        }
        public void Tick()
        {
            if (!Cfg.EnableVehicleControls || Game.IsMissionActive || !Attach()) { Release(); return; }
            if (Game.IsPaused || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE)) { cruise = false; return; }
            if (engineOff) Function.Call(Hash.SET_VEHICLE_ENGINE_ON, controlled, false, true, true);
            if (!cruise) return;
            // Supply throttle only; do not rewrite handling, native max-speed or engine-power multipliers.
            if (pool.AreAnyVisible || !controlled.IsEngineRunning || !hasFuel(controlled) || !controlled.IsOnAllWheels || controlled.HasCollided || controlled.Speed < 3 ||
                Game.GetControlValueNormalized(GTA.Control.VehicleBrake) > .05f || Game.GetControlValueNormalized(GTA.Control.VehicleHandbrake) > .05f || Game.GetControlValueNormalized(GTA.Control.VehicleAccelerate) > .1f)
            { CancelCruise(); return; }
            float throttle = Math.Max(0, Math.Min(1, .2f + (cruiseSpeed - controlled.Speed) * .18f));
            Game.SetControlValueNormalized(GTA.Control.VehicleAccelerate, throttle);
            if (!Visuals.VehicleReady) new GTA.UI.TextElement("CRUISE " + (cruiseSpeed * 3.6f).ToString("0") + " km/h", new PointF(GTA.UI.Screen.ScaledWidth - 300, GTA.UI.Screen.Height - 150), .32f, Color.LightGreen).Draw();
        }
        public void OnKey(KeyEventArgs e)
        {
            if (!Cfg.EnableVehicleControls || Game.IsMissionActive) return;
            if (e.KeyCode == Cfg.VehicleMenuKey && menu.Visible) { menu.Visible = false; return; }
            if (pool.AreAnyVisible || !Attach()) return;
            if (e.KeyCode == Cfg.VehicleMenuKey) { menu.Name = controlled.LocalizedName; menu.Visible = true; }
            else if (e.KeyCode == Cfg.EngineKey) ToggleEngine();
            else if (e.KeyCode == Cfg.CruiseKey) ToggleCruise();
            else if (e.KeyCode == Cfg.LeftSignalKey) Signal(true);
            else if (e.KeyCode == Cfg.RightSignalKey) Signal(false);
            else if (e.KeyCode == Cfg.HazardKey) Hazards();
        }
        void Release()
        {
            if (controlled != null && controlled.Exists())
            {
                if (engineOff) Function.Call(Hash.SET_VEHICLE_ENGINE_ON, controlled, controlled.IsEngineRunning, true, false);
                if (indicatorsChanged)
                {
                    Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS, controlled, 1, (originalIndicators & 1) != 0);
                    Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS, controlled, 0, (originalIndicators & 2) != 0);
                }
            }
            menu.Visible = false; controlled = null; engineOff = cruise = indicatorsChanged = false;
        }
        public void Reload() { Release(); }
        public void Save() { }
        public void Dispose() { Release(); }
    }
}
