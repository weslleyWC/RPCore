using System;
using System.Collections.Generic;
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
    public class StorageLocation
    {
        public string Id, Name;
        public float X, Y, Z, Capacity = 200;
    }

    public class StorageSystem : ISystem
    {
        public string Name { get { return "storage"; } }
        readonly InventorySystem inventory;
        readonly AchievementSystem ach;
        readonly ObjectPool pool;
        readonly NativeMenu menu = new NativeMenu("Storage", "");
        readonly List<StorageLocation> locations;
        readonly List<Blip> blips = new List<Blip>();
        Vehicle vehicle;
        string container;
        Vector3 origin;
        float capacity;
        bool dirty;

        public StorageSystem(ObjectPool pool, InventorySystem inventory, AchievementSystem ach)
        {
            this.pool = pool; this.inventory = inventory; this.ach = ach; pool.Add(menu); Visuals.Register(menu, "storage");
            locations = Store.LoadDef("Storage/Locations.json", () => new List<StorageLocation> {
                new StorageLocation { Id = "franklin_house", Name = "Strawberry stash", X = -14.1f, Y = -1440.2f, Z = 31.1f }
            });
            foreach (var stash in locations.Where(s => s != null && !string.IsNullOrWhiteSpace(s.Id) && s.Capacity > 0))
                blips.Add(MapBlips.Create(new Vector3(stash.X, stash.Y, stash.Z), "RPCore Storage: " + stash.Name, BlipSprite.Package, BlipColor.Blue, .7f));
        }

        public void OnKey(KeyEventArgs e)
        {
            if (e.KeyCode != Cfg.StorageKey) return;
            if (menu.Visible) { menu.Visible = false; return; }
            if (pool.AreAnyVisible) return;
            Ped p = Game.Player.Character;
            if (p.IsDead || p.IsInVehicle()) return;
            vehicle = null; container = null;
            var stash = locations.FirstOrDefault(s => s != null && !string.IsNullOrWhiteSpace(s.Id) && s.Capacity > 0 && p.Position.DistanceTo(new Vector3(s.X, s.Y, s.Z)) < 3);
            if (stash != null)
            {
                container = "stash:" + stash.Id; capacity = stash.Capacity; origin = new Vector3(stash.X, stash.Y, stash.Z);
            }
            else
            {
                vehicle = World.GetNearbyVehicles(p, 6).Where(v => v.Exists() && v.Model.IsCar && p.Position.DistanceTo(v.GetOffsetPosition(new Vector3(0, -2.5f, 0))) < 3).OrderBy(v => p.Position.DistanceTo(v.Position)).FirstOrDefault();
                if (JobVehicles.Contains(vehicle)) { Notify.Show("Veiculo de trabalho: use seu veiculo pessoal para guardar itens."); return; }
                if (vehicle == null) { Notify.Show("Stand near a trunk or a configured stash."); return; }
                if (Function.Call<int>(Hash.GET_VEHICLE_DOOR_LOCK_STATUS, vehicle) > 1) { Notify.Show("Vehicle locked."); return; }
                string plate = Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, vehicle).Trim();
                if (plate.Length == 0) { Notify.Show("A license plate is required for persistent trunk storage."); return; }
                container = "trunk:" + vehicle.Model.Hash + ":" + plate; capacity = 100;
            }
            Rebuild(); menu.Visible = true; ach.Bump("storage_used");
        }

        void Rebuild()
        {
            int selected = menu.SelectedIndex; menu.Clear(); menu.Name = container;
            Visuals.ClearItems(menu);
            AddRows(inventory.Snapshot(), true);
            AddRows(new Dictionary<string, int>(inventory.Container(container)), false);
            if (menu.Items.Count == 0) menu.Add(new NativeItem("Empty", "Buy items at a store first.") { Enabled = false });
            menu.SelectedIndex = Math.Max(0, Math.Min(selected, menu.Items.Count - 1)); dirty = false;
        }

        void AddRows(Dictionary<string, int> items, bool deposit)
        {
            foreach (var pair in items.Where(x => x.Value > 0))
            {
                string id = pair.Key;
                var row = new NativeItem((deposit ? "Store: " : "Take: ") + (inventory.Defs.ContainsKey(id) ? inventory.Defs[id].Name : id), "Transfer one item. Inventory and storage are saved together.") { AltTitle = "x" + pair.Value };
                Visuals.Badge(menu, row, id);
                row.Activated += (s, e) => {
                    if (!Accessible()) { menu.Visible = false; return; }
                    if (!inventory.Transfer(container, id, deposit, capacity)) Notify.Show("Not enough space or item unavailable.");
                    dirty = true;
                };
                menu.Add(row);
            }
        }

        bool Accessible()
        {
            var p = Game.Player.Character;
            if (p.IsDead || p.IsInVehicle()) return false;
            if (vehicle == null) return p.Position.DistanceTo(origin) < 4;
            return vehicle.Exists() && !vehicle.IsDead && vehicle.Speed < 1 && Function.Call<int>(Hash.GET_VEHICLE_DOOR_LOCK_STATUS, vehicle) <= 1 && p.Position.DistanceTo(vehicle.GetOffsetPosition(new Vector3(0, -2.5f, 0))) < 3;
        }
        public void Tick() { if (menu.Visible && !Accessible()) menu.Visible = false; if (menu.Visible && dirty) Rebuild(); }
        public void Reload() { menu.Visible = false; vehicle = null; }
        public void Save() { }
        public void Dispose() { menu.Visible = false; foreach (var b in blips) if (b.Exists()) b.Delete(); blips.Clear(); }
    }
}
