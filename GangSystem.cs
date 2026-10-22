using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using LemonUI;
using LemonUI.Menus;

namespace RPCore
{
    public class GangDefinition : ExtensibleData
    {
        public string Name, Description, GangColor, LeaderModel;
        public float HQX, HQY, HQZ, LeaderSpawnHeading;
        public GangSpot LeaderSpawnPostion;
        public string[] PedModels;
        public List<GangSpot> PedSpawnSpots;
        public Vector3 Position { get { return new Vector3(HQX, HQY, HQZ); } }
    }
    public class GangSpot { public float X, Y, Z, Heading; }
    public class GangState : ExtensibleData { public string Gang; public int Reputation; }

    public class GangSystem : ISystem
    {
        public string Name { get { return "gangs"; } }
        readonly ObjectPool pool;
        readonly NativeMenu menu = new NativeMenu("Gangs", "");
        readonly List<GangDefinition> gangs;
        readonly List<Blip> blips = new List<Blip>();
        readonly AchievementSystem ach;
        GangState state;
        bool dirty;

        public GangSystem(ObjectPool pool, AchievementSystem ach)
        {
            this.pool = pool; this.ach = ach; pool.Add(menu);
            gangs = Store.LoadDef("Gangs/CustomGangs.json", () => new List<GangDefinition>()).Where(g => g != null && !string.IsNullOrWhiteSpace(g.Name)).GroupBy(g => g.Name).Select(g => g.First()).ToList();
            foreach (var gang in gangs)
            {
                var b = World.CreateBlip(gang.Position); b.Name = gang.Name; b.IsShortRange = true;
                b.Color = gang.GangColor == "Purple" ? BlipColor.Purple : gang.GangColor == "Green" ? BlipColor.Green : BlipColor.Yellow;
                blips.Add(b);
            }
            ach.Progress += Progress; Reload();
        }

        void Progress(string stat, int amount)
        {
            if (stat != "pizza_drops" || string.IsNullOrEmpty(state.Gang) || !gangs.Any(g => g.Name == state.Gang)) return;
            state.Reputation = Math.Min(100, state.Reputation + amount);
            dirty = true;
        }

        void Rebuild()
        {
            menu.Clear(); menu.Name = (state.Gang ?? "Unaffiliated") + " / rep " + state.Reputation;
            foreach (var g in gangs)
            {
                var gang = g;
                var route = new NativeItem("GPS: " + gang.Name, gang.Description ?? "");
                route.Activated += (s, e) => { Function.Call(Hash.SET_NEW_WAYPOINT, gang.HQX, gang.HQY); menu.Visible = false; };
                menu.Add(route);
                var join = new NativeItem("Join " + gang.Name, "Visit the HQ on foot. Complete pizza deliveries to earn reputation in RPCore.");
                join.Activated += (s, e) => {
                    var p = Game.Player.Character;
                    if (p.IsDead || p.IsInVehicle() || p.Position.DistanceTo(gang.Position) > 5) { Notify.Show("Visit the gang HQ on foot to join."); return; }
                    if (state.Gang == gang.Name) return;
                    state.Gang = gang.Name; state.Reputation = 0; Save(); dirty = true;
                    Notify.Show("Joined " + gang.Name);
                };
                menu.Add(join);
            }
            var leave = new NativeItem("Leave current gang");
            leave.Activated += (s, e) => { state.Gang = null; state.Reputation = 0; Save(); dirty = true; }; menu.Add(leave);
            dirty = false;
        }

        public void Tick()
        {
            if (menu.Visible && dirty) Rebuild();
            var p = Game.Player.Character;
            if (p.IsDead) return;
            foreach (var g in gangs.Where(g => p.Position.DistanceTo(g.Position) < 35))
                World.DrawMarker(MarkerType.Cylinder, g.Position - new Vector3(0, 0, 1), Vector3.Zero, Vector3.Zero, new Vector3(2, 2, 1), Color.FromArgb(90, 160, 80, 220));
        }
        public void OnKey(KeyEventArgs e)
        {
            if (e.KeyCode != Cfg.GangsKey) return;
            if (menu.Visible) menu.Visible = false;
            else if (!pool.AreAnyVisible) { Rebuild(); menu.Visible = true; }
        }
        public void Reload() { state = Store.Load("GangState.json", () => new GangState()); state.Reputation = Math.Max(0, Math.Min(100, state.Reputation)); menu.Visible = false; }
        public void Save() { Store.Write("GangState.json", state); }
        public void Dispose() { ach.Progress -= Progress; menu.Visible = false; foreach (var b in blips) if (b.Exists()) b.Delete(); }
    }
}
