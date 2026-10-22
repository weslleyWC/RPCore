using System.Drawing;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using GTA.UI;

namespace RPCore
{
    public class SeatbeltSystem : ISystem
    {
        public string Name { get { return "seatbelt"; } }

        readonly AchievementSystem ach;
        bool belted, originalFlag;
        public bool IsBelted { get { return belted; } }
        Ped owner;
        Vehicle vehicle;

        public SeatbeltSystem(AchievementSystem ach) { this.ach = ach; }

        static bool HasBelt(Vehicle v)
        {
            Model m = v.Model;
            return !(m.IsBike || m.IsBicycle || m.IsBoat || m.IsHelicopter || m.IsPlane || m.IsQuadBike || m.IsTrain);
        }

        public void Tick()
        {
            Ped p = Game.Player.Character;
            if (p == null || !p.Exists() || p.IsDead) { Reset(); return; }

            Vehicle v = p.IsInVehicle() ? p.CurrentVehicle : null;
            if (v == null || !HasBelt(v) || (vehicle != null && vehicle.Handle != v.Handle) || (owner != null && owner.Handle != p.Handle))
            {
                Reset();
                return;
            }

            // config flag 32 = ped can fly through the windscreen; false while buckled
            if (belted) Function.Call(Hash.SET_PED_CONFIG_FLAG, p, 32, false);

            if (belted) Game.DisableControlThisFrame(GTA.Control.VehicleExit);
            else if (v.Speed > 8f)
                new TextElement("BUCKLE UP", new PointF(GTA.UI.Screen.ScaledWidth / 2f, 40f), 0.4f, Color.OrangeRed,
                    GTA.UI.Font.ChaletLondon, Alignment.Center).Draw();
        }

        public void OnKey(KeyEventArgs e)
        {
            if (e.KeyCode != Cfg.SeatbeltKey) return;
            Ped p = Game.Player.Character;
            if (p == null || !p.Exists() || p.IsDead || !p.IsInVehicle() || !HasBelt(p.CurrentVehicle)) return;

            if (belted) Reset();
            else
            {
                owner = p;
                vehicle = p.CurrentVehicle;
                originalFlag = Function.Call<bool>(Hash.GET_PED_CONFIG_FLAG, p, 32, true);
                belted = true;
            }
            Sfx.Play(belted ? "belt_on" : "belt_off");
            Notify.Show(belted ? "Seatbelt on" : "Seatbelt off");
            if (belted) ach.Bump("belts_on");
        }

        void Reset()
        {
            if (owner != null && owner.Exists()) Function.Call(Hash.SET_PED_CONFIG_FLAG, owner, 32, originalFlag);
            belted = false; owner = null; vehicle = null;
        }
        public void Reload() { Reset(); }
        public void Save() { }
        public void Dispose() { Reset(); }
    }
}
