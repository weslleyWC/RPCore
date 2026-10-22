using System;
using System.Drawing;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.UI;

namespace RPCore
{
    public class PizzaStats : ExtensibleData
    {
        public int Deliveries;
        public int Earned;
    }

    public class PizzaJob : ISystem
    {
        public string Name { get { return "pizza"; } }

        readonly Random rng = new Random();
        readonly AchievementSystem ach;
        PizzaStats stats;

        bool active, spawned;
        Vehicle ride;
        Blip blip;
        Vector3 dest;
        int remaining, deadline;
        float legDist;

        public PizzaJob(AchievementSystem ach)
        {
            this.ach = ach;
            stats = Store.Load("PizzaStats.json", () => new PizzaStats());
        }

        public void Reload()
        {
            if (active) End("Shift ended, character switched.", false);
            stats = Store.Load("PizzaStats.json", () => new PizzaStats());
        }

        public void OnKey(KeyEventArgs e)
        {
            if (e.KeyCode != Cfg.JobKey) return;
            if (active) End("Shift ended."); else Start();
        }

        void Start()
        {
            Ped p = Game.Player.Character;
            if (p.IsDead) return;

            if (p.IsInVehicle())
            {
                ride = p.CurrentVehicle;
                spawned = false;
            }
            else
            {
                Model m = new Model(VehicleHash.Faggio);
                if (!m.Request(3000)) { Notify.Show("~r~Couldn't load the scooter model."); return; }
                ride = World.CreateVehicle(m, p.GetOffsetPosition(new Vector3(0f, 3f, 0f)), p.Heading);
                m.MarkAsNoLongerNeeded();
                if (ride == null) { Notify.Show("~r~Scooter spawn failed."); return; }
                p.SetIntoVehicle(ride, VehicleSeat.Driver);
                spawned = true;
            }

            remaining = Cfg.PizzaDrops;
            active = true;
            Notify.Show("~y~Pizza shift started~s~, " + remaining + " drops.");
            NextLeg();
        }

        void NextLeg()
        {
            Ped p = Game.Player.Character;
            float r = Cfg.PizzaMinDist + (float)rng.NextDouble() * (Cfg.PizzaMaxDist - Cfg.PizzaMinDist);
            Vector3 target = Vector3.Zero;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                var candidate = World.GetNextPositionOnStreet(p.Position.Around(r), true);
                if (candidate != Vector3.Zero && p.Position.DistanceTo2D(candidate) >= 50) { target = candidate; break; }
            }
            if (target == Vector3.Zero) { End("No valid delivery road found. Try starting in the city."); return; }

            dest = target;
            legDist = p.Position.DistanceTo2D(dest);
            deadline = Game.GameTime + (int)(legDist / 8f * 1000f) + 45000; // ~8 m/s average plus grace

            if (blip != null && blip.Exists()) blip.Delete();
            blip = World.CreateBlip(dest);
            blip.Color = BlipColor.Yellow;
            blip.ShowRoute = true;
            blip.Name = "Pizza drop";
        }

        public void Tick()
        {
            if (!active) return;

            Ped p = Game.Player.Character;
            if (p.IsDead) { End("~r~You died. Shift over."); return; }
            if (ride == null || !ride.Exists() || ride.IsDead) { End("~r~Your ride is wrecked. Shift over."); return; }

            int left = deadline - Game.GameTime;

            World.DrawMarker(MarkerType.Cylinder, dest, Vector3.Zero, Vector3.Zero,
                new Vector3(4f, 4f, 2.5f), Color.FromArgb(110, 255, 200, 0));

            new TextElement(
                string.Format("PIZZA   {0} left   {1}{2}s", remaining, left < 0 ? "-" : "", Math.Abs(left) / 1000),
                new PointF(GTA.UI.Screen.ScaledWidth / 2f, 14f), 0.45f,
                left < 0 ? Color.OrangeRed : Color.White,
                GTA.UI.Font.ChaletLondon, Alignment.Center).Draw();

            if (p.Position.DistanceTo(dest) > 5f) return;

            Entity mover = p.IsInVehicle() ? (Entity)p.CurrentVehicle : p;
            if (mover.Velocity.Length() > 3f) { Prompt.Help("Slow down to hand over the pizza."); return; }

            Deliver(left);
        }

        void Deliver(int left)
        {
            int pay = Cfg.PizzaBase + (int)(legDist * Cfg.PizzaPerMeter);
            string tag;
            if (left >= 0) { pay += Cfg.PizzaOnTimeBonus; tag = "on time"; }
            else { pay /= 2; tag = "late"; }

            remaining--;
            stats.Deliveries++;
            if (remaining <= 0) pay += Cfg.PizzaShiftBonus;
            stats.Earned += pay;
            Wallet.Credit(pay);
            Notify.Show("~g~+$" + pay + "~s~ (" + tag + ")");
            Sfx.Play("delivered");
            ach.Bump("pizza_drops");
            ach.Bump("cash_earned", pay);

            if (remaining <= 0)
            {
                ach.Bump("shifts_done");
                End("~y~Shift complete.~s~ Lifetime: " + stats.Deliveries + " drops, $" + stats.Earned);
                return;
            }
            NextLeg();
        }

        void End(string msg, bool save = true)
        {
            active = false;
            if (blip != null && blip.Exists()) blip.Delete();
            blip = null;
            if (spawned && ride != null && ride.Exists()) ride.MarkAsNoLongerNeeded();
            ride = null;
            Notify.Show(msg);
            if (save) Save();
        }

        public void Save() { Store.Write("PizzaStats.json", stats); }

        public void Dispose()
        {
            if (blip != null && blip.Exists()) blip.Delete();
            if (spawned && ride != null && ride.Exists()) ride.MarkAsNoLongerNeeded();
        }
    }
}
