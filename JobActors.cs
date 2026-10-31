using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;

namespace RPCore
{
    internal static class JobActors
    {
        sealed class Retired { public Ped Ped; public Vehicle Car; public int End, WanderAt; public bool Departing, Wandering; }
        static readonly List<Retired> retired=new List<Retired>();
        public static Ped Create(Vector3 position, Random random)
        {
            string[] names={"a_m_y_business_02","a_f_y_business_01","a_m_m_bevhills_01"};
            var model=new Model(names[random.Next(names.Length)]);
            try
            {
                if(!model.IsValid || !model.IsInCdImage || !model.Request(2500))return null;
                var p=World.CreatePed(model,position);
                if(p!=null && PdmAssets.Valid(p)){p.IsPersistent=true;p.BlockPermanentEvents=true;}
                return p;
            }
            finally {model.MarkAsNoLongerNeeded();}
        }
        public static void Enter(Ped p, Vehicle v, VehicleSeat seat) => Function.Call(Hash.TASK_ENTER_VEHICLE,p,v,30000,(int)seat,1f,1,0);
        public static void Leave(Ped p, Vehicle v) => Function.Call(Hash.TASK_LEAVE_VEHICLE,p,v,0);
        public static void Walk(Ped p, Vector3 point) => Function.Call(Hash.TASK_FOLLOW_NAV_MESH_TO_COORD,p,point.X,point.Y,point.Z,1f,20000,1f,0,0f);
        public static void Drive(Ped p, Vehicle v, Vector3 point) => Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE,p,v,point.X,point.Y,point.Z,8f,786603,4f);
        public static void Retire(Ped p, Vehicle companyCar=null, bool departing=false)
        {
            if(p==null || !PdmAssets.Valid(p) || retired.Any(r=>r.Ped.Handle==p.Handle))return;
            if(!departing && p.IsInVehicle())Leave(p,p.CurrentVehicle);
            retired.Add(new Retired{Ped=p,Car=companyCar,Departing=departing,WanderAt=Game.GameTime+8000,End=Game.GameTime+(departing?45000:12000)});
        }
        public static void Tick()
        {
            for(int i=retired.Count-1;i>=0;i--)
            {
                var r=retired[i];var p=r.Ped;
                if(!PdmAssets.Valid(p)){retired.RemoveAt(i);continue;}
                if(r.Departing && !r.Wandering && r.Car!=null && PdmAssets.Valid(r.Car) && p.IsInVehicle(r.Car) && r.Car.Driver==p && unchecked(Game.GameTime-r.WanderAt)>=0)
                {
                    // Continue beyond the reference's departure point so the next customer's spawn remains free.
                    Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER,p,r.Car,10f,786603);r.Wandering=true;
                }
                bool far=p.Position.DistanceTo(Game.Player.Character.Position)>100;
                if(far || unchecked(Game.GameTime-r.End)>=0 || p.IsDead)
                {
                    // Only our NPC is deleted. Company cars are reclaimed by JobVehicles once empty.
                    // A player's car is never registered as CompanyCar.
                    p.Delete();if(!PdmAssets.Valid(p))retired.RemoveAt(i);
                }
            }
        }
        public static void Dispose()
        {
            foreach(var r in retired)if(PdmAssets.Valid(r.Ped))r.Ped.Delete();retired.Clear();
        }
        public static bool Free(Vector3 point,float radius=4) => World.GetNearbyVehicles(point,radius).Length==0 && World.GetNearbyPeds(point,2).Length==0;
        public static Vector3 Road(JobPoint p)
        {
            var origin=new Vector3(p.X,p.Y,p.Z);var road=World.GetNextPositionOnStreet(origin,false);
            return road!=Vector3.Zero && road.DistanceTo(origin)<150?road:Vector3.Zero;
        }
        public static Vector3 Sidewalk(Vector3 road)
        {
            Vector3 point;
            if(!World.GetSafePositionForPed(road,out point,GetSafePositionFlags.OnlyPavement|GetSafePositionFlags.NotIsolated|GetSafePositionFlags.NotInterior|GetSafePositionFlags.NotWater))return Vector3.Zero;
            return point!=Vector3.Zero && point.DistanceTo2D(road)<35 && Math.Abs(point.Z-road.Z)<5?point:Vector3.Zero;
        }
    }
}
