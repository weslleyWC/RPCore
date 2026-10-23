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
        sealed class Rig { public Vehicle Truck, Trailer; }
        static readonly System.Collections.Generic.List<Vehicle> reserved = new System.Collections.Generic.List<Vehicle>();
        static readonly System.Collections.Generic.List<Vehicle> retired = new System.Collections.Generic.List<Vehicle>();
        static readonly System.Collections.Generic.List<Rig> rigs = new System.Collections.Generic.List<Rig>();
        static bool Same(Vehicle a, Vehicle b) => a != null && b != null && a.Exists() && b.Exists() && a.Handle == b.Handle;
        static bool Exists(Vehicle v) => v != null && v.Exists();
        public static bool Contains(Vehicle vehicle) => Exists(vehicle) && (reserved.Exists(v => Same(v,vehicle)) || retired.Exists(v => Same(v,vehicle)) || rigs.Exists(r => Same(r.Truck,vehicle) || Same(r.Trailer,vehicle)));
        public static void Reserve(Vehicle vehicle) { if (Exists(vehicle) && !reserved.Exists(v => Same(v,vehicle))) reserved.Add(vehicle); }
        public static void Release(Vehicle vehicle) { reserved.RemoveAll(v => !Exists(v) || Same(v,vehicle)); }
        public static void Retire(Vehicle vehicle) { Release(vehicle); if (Exists(vehicle) && !retired.Exists(v => Same(v,vehicle))) retired.Add(vehicle); }
        public static bool Towing(Vehicle truck, Vehicle trailer)
        {
            if (!Exists(truck) || !Exists(trailer)) return false;
            var result = new OutputArgument();
            return Function.Call<bool>(Hash.GET_VEHICLE_TRAILER_VEHICLE,truck,result) && result.GetResult<int>() == trailer.Handle;
        }
        public static void RetireRig(Vehicle truck, Vehicle trailer)
        {
            Release(truck); Release(trailer);
            if (!Exists(truck) && !Exists(trailer)) return;
            if (!rigs.Exists(r => Same(r.Truck,truck) || Same(r.Trailer,trailer))) rigs.Add(new Rig {Truck=truck,Trailer=trailer});
        }
        static bool EmptyStopped(Vehicle v) => !Exists(v) || (v.Occupants.Length == 0 && v.Speed < .5f);
        public static bool TrailerHasOtherTractor(Vehicle trailer, Vehicle ownTruck)
        {
            if (Exists(trailer)) foreach (var other in World.GetNearbyVehicles(trailer.Position,30))
                if (!Same(other,ownTruck) && Towing(other,trailer)) return true;
            return false;
        }
        static bool ForeignConnection(Rig r)
        {
            if (Exists(r.Truck) && Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER,r.Truck) && !Towing(r.Truck,r.Trailer)) return true;
            return TrailerHasOtherTractor(r.Trailer,r.Truck);
        }
        public static void Tick()
        {
            for (int i = retired.Count - 1; i >= 0; i--)
            {
                var v = retired[i];
                if (!v.Exists()) { retired.RemoveAt(i); continue; }
                if (v.Occupants.Length == 0) { v.Delete(); if (!v.Exists()) retired.RemoveAt(i); }
            }
            // A tanker must never disappear while its tractor is occupied or either half is moving.
            for (int i=rigs.Count-1; i>=0; i--)
            {
                var r=rigs[i];
                if (!Exists(r.Truck) && !Exists(r.Trailer)) {rigs.RemoveAt(i);continue;}
                if (!EmptyStopped(r.Truck) || !EmptyStopped(r.Trailer) || ForeignConnection(r)) continue;
                if (Towing(r.Truck,r.Trailer)) Function.Call(Hash.DETACH_VEHICLE_FROM_TRAILER,r.Truck);
                if (Exists(r.Trailer)) r.Trailer.Delete();
                if (Exists(r.Truck)) r.Truck.Delete();
                if (!Exists(r.Truck) && !Exists(r.Trailer)) rigs.RemoveAt(i);
            }
        }
        public static void Dispose()
        {
            Tick(); foreach (var v in retired) if (v.Exists()) v.MarkAsNoLongerNeeded(); retired.Clear();
            foreach (var r in rigs) { if(Exists(r.Truck))r.Truck.MarkAsNoLongerNeeded(); if(Exists(r.Trailer))r.Trailer.MarkAsNoLongerNeeded(); }
            rigs.Clear(); reserved.Clear();
        }
    }
}
