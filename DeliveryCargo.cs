using GTA;
using GTA.Math;
using GTA.Native;

namespace RPCore
{
    // Only owns its prop, model requests and named secondary animations.
    internal sealed class DeliveryCargo
    {
        const string CarryDictionary = "anim@heists@box_carry@";
        const string GiveDictionary = "mp_common";
        Ped owner;
        Prop prop;
        Model model;
        bool waiting, carryingAnimation, givingAnimation;
        int timeout;
        public bool Started => owner != null;
        public void Start(Ped player, bool pizza)
        {
            Stop(); owner = player;
            if (!Visuals.UseProps || !Visuals.Enabled) return;
            model = new Model(pizza ? "prop_pizza_box_02" : "ups_box");
            if (!model.IsValid || !model.IsInCdImage) model = new Model("prop_cs_cardbox_01");
            if (!model.IsValid || !model.IsInCdImage) return;
            model.Request(); Function.Call(Hash.REQUEST_ANIM_DICT, CarryDictionary);
            Function.Call(Hash.REQUEST_ANIM_DICT, GiveDictionary);
            waiting = true; timeout = Game.GameTime + 2500;
        }
        public void Tick()
        {
            if (owner == null) return;
            if (!owner.Exists() || owner.IsDead || owner != Game.Player.Character || owner.IsInVehicle()) { Stop(); return; }
            if (!waiting) return;
            if (unchecked(Game.GameTime - timeout) >= 0) { model.MarkAsNoLongerNeeded(); waiting = false; return; }
            if (!model.IsLoaded || !Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, CarryDictionary)) return;
            try
            {
                prop = World.CreateProp(model, owner.Position, false, false);
                if (prop != null && prop.Exists())
                {
                    prop.IsCollisionEnabled = false;
                    prop.AttachTo(owner.Bones[(Bone)28422], new Vector3(0, -.05f, -.12f), Vector3.Zero);
                    Function.Call(Hash.TASK_PLAY_ANIM, owner, CarryDictionary, "idle", 4f, -4f, -1, 49, 0f, false, false, false);
                    carryingAnimation = true;
                }
            }
            finally { model.MarkAsNoLongerNeeded(); waiting = false; }
        }
        public void Give()
        {
            if (owner == null || !owner.Exists()) return;
            StopCarry();
            if (Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, GiveDictionary))
            {
                Function.Call(Hash.TASK_PLAY_ANIM, owner, GiveDictionary, "givetake1_a", 4f, -4f, 1000, 49, 0f, false, false, false);
                givingAnimation = true;
            }
        }
        void StopCarry()
        {
            if (carryingAnimation && owner != null && owner.Exists()) Function.Call(Hash.STOP_ANIM_TASK, owner, CarryDictionary, "idle", 2f);
            carryingAnimation = false;
        }
        public void Stop()
        {
            if (prop != null && prop.Exists()) prop.Delete(); prop = null;
            StopCarry();
            if (givingAnimation && owner != null && owner.Exists()) Function.Call(Hash.STOP_ANIM_TASK, owner, GiveDictionary, "givetake1_a", 2f);
            if (waiting) model.MarkAsNoLongerNeeded();
            if (owner != null) { Function.Call(Hash.REMOVE_ANIM_DICT, CarryDictionary); Function.Call(Hash.REMOVE_ANIM_DICT, GiveDictionary); }
            owner = null; waiting = givingAnimation = false;
        }
    }
    internal static class JobVehicles
    {
        static Vehicle reserved;
        static readonly System.Collections.Generic.List<Vehicle> retired = new System.Collections.Generic.List<Vehicle>();
        public static bool Contains(Vehicle vehicle) => vehicle != null && ((reserved != null && reserved.Exists() && vehicle.Handle == reserved.Handle) || retired.Exists(v => v.Exists() && v.Handle == vehicle.Handle));
        public static void Reserve(Vehicle vehicle) { reserved = vehicle; }
        public static void Release(Vehicle vehicle) { if (reserved == vehicle) reserved = null; }
        public static void Retire(Vehicle vehicle) { Release(vehicle); if (vehicle != null && vehicle.Exists()) retired.Add(vehicle); }
        public static void Tick()
        {
            for (int i = retired.Count - 1; i >= 0; i--)
            {
                var v = retired[i];
                if (!v.Exists()) { retired.RemoveAt(i); continue; }
                if (v.Occupants.Length == 0) { v.Delete(); if (!v.Exists()) retired.RemoveAt(i); }
            }
        }
        public static void Dispose()
        {
            Tick(); foreach (var v in retired) if (v.Exists()) v.MarkAsNoLongerNeeded(); retired.Clear(); reserved = null;
        }
    }
}
