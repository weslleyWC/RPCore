using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;

namespace RPCore
{
    internal static class RigSpawn
    {
        static JobPoint Point(Entity e) => new JobPoint{Name="obstacle",X=e.Position.X,Y=e.Position.Y,Z=e.Position.Z,Heading=e.Heading};
        static SpawnBox Bounds(JobPoint point,Model model)
        {
            Vector3 min,max;model.GetDimensions(out min,out max);
            return new SpawnBox(point,min.X,max.X,min.Y,max.Y,min.Z,max.Z);
        }
        static void AddPropBounds(List<SpawnBox> obstacles,Prop prop)
        {
            try{obstacles.Add(Bounds(Point(prop),prop.Model));}
            catch(ArgumentException){/* Some streamed map props expose empty bounds; the world probes still test their collision. */}
        }
        public static RigSpawnSlot Find(JobPoint anchor)
        {
            var tanker=new Model(VehicleHash.Tanker);var hauler=new Model(VehicleHash.Hauler);
            try
            {
                if(!tanker.Request(2500) || !hauler.Request(2500)){Log.Write("LS GAS spawn: vehicle model request failed.");return null;}
                var center=new Vector3(anchor.X,anchor.Y,anchor.Z);
                var obstacles=new List<SpawnBox>();
                foreach(var v in World.GetNearbyVehicles(center,100))if(v.Exists())obstacles.Add(Bounds(Point(v),v.Model));
                foreach(var p in World.GetNearbyPeds(center,100))if(p.Exists())obstacles.Add(new SpawnBox(Point(p),-.5,.5,-.5,.5,-1,1));
                foreach(var prop in World.GetNearbyProps(center,100))if(prop.Exists() && prop.IsCollisionEnabled)AddPropBounds(obstacles,prop);
                int occupied=0,ground=0,world=0;
                var chosen=RigSpawnSearch.Find(anchor,slot=>{
                    if(!Ground(slot.Trailer,tanker) || !Ground(slot.Truck,hauler)){ground++;return false;}
                    var tankBox=Bounds(slot.Trailer,tanker);var truckBox=Bounds(slot.Truck,hauler);
                    if(Math.Abs((tankBox.Z-tankBox.HalfHeight)-(truckBox.Z-truckBox.HalfHeight))>.6){ground++;return false;}
                    if(tankBox.Overlaps(truckBox) || obstacles.Exists(b=>tankBox.Overlaps(b)||truckBox.Overlaps(b))){occupied++;return false;}
                    if(!WorldClear(tankBox) || !WorldClear(truckBox)){world++;return false;}
                    return true;
                });
                Log.Write("LS GAS spawn: candidates=35 occupied="+occupied+" ground="+ground+" world="+world+" selected="+(chosen==null?"none":chosen.Trailer.X+","+chosen.Trailer.Y+","+chosen.Trailer.Z));
                return chosen;
            }
            finally{tanker.MarkAsNoLongerNeeded();hauler.MarkAsNoLongerNeeded();}
        }
        // Single vehicle variant for the prison bus; the validated LS GAS pair search stays unchanged.
        public static JobPoint FindVehicle(JobPoint anchor,Model model,string label)
        {
            if(!model.Request(2500)){Log.Write(label+" spawn: model request failed.");return null;}
            var center=new Vector3(anchor.X,anchor.Y,anchor.Z);var obstacles=new List<SpawnBox>();
            foreach(var v in World.GetNearbyVehicles(center,100))if(v.Exists())obstacles.Add(Bounds(Point(v),v.Model));
            foreach(var p in World.GetNearbyPeds(center,100))if(p.Exists())obstacles.Add(new SpawnBox(Point(p),-.5,.5,-.5,.5,-1,1));
            foreach(var prop in World.GetNearbyProps(center,100))if(prop.Exists() && prop.IsCollisionEnabled)AddPropBounds(obstacles,prop);
            int occupied=0,ground=0,world=0;
            foreach(var slot in RigSpawnSearch.Candidates(anchor))
            {
                var candidate=slot.Trailer;
                if(!Ground(candidate,model)){ground++;continue;}
                var box=Bounds(candidate,model);
                if(obstacles.Exists(b=>box.Overlaps(b))){occupied++;continue;}
                if(!WorldClear(box)){world++;continue;}
                Log.Write(label+" spawn: selected="+candidate.X+","+candidate.Y+","+candidate.Z);return candidate;
            }
            Log.Write(label+" spawn: no bay occupied="+occupied+" ground="+ground+" world="+world);return null;
        }
        // Exact showroom bay: never search into another numbered slot or delete ambient vehicles.
        public static JobPoint FindDisplayVehicle(JobPoint anchor, Model model, bool interior)
        {
            if (!model.Request(2500)) return null;
            var p = new JobPoint { Name = anchor.Name, X = anchor.X, Y = anchor.Y, Z = anchor.Z, Heading = anchor.Heading };
            if (interior)
            {
                int room = GTA.Native.Function.Call<int>(GTA.Native.Hash.GET_INTERIOR_AT_COORDS, p.X, p.Y, p.Z + 1);
                if (room == 0 || !GTA.Native.Function.Call<bool>(GTA.Native.Hash.IS_INTERIOR_READY, room)) return null;
                var probe = ShapeTest.StartExpensiveSyncTestLOSProbe(new Vector3(p.X, p.Y, p.Z + 1.5f), new Vector3(p.X, p.Y, p.Z - 1.5f), IntersectFlags.Map | IntersectFlags.Objects);
                ShapeTestResult hit;
                if (probe.GetResult(out hit) != ShapeTestStatus.Ready || !hit.DidHit || hit.SurfaceNormal.Z < .85f) return null;
                Vector3 min, max; model.GetDimensions(out min, out max); p.Z = hit.HitPosition.Z - min.Z + .06f;
            }
            else if (!Ground(p, model)) return null;
            var box = Bounds(p, model); var center = new Vector3(p.X, p.Y, p.Z);
            foreach (var v in World.GetNearbyVehicles(center, 30)) if (v.Exists() && box.Overlaps(Bounds(Point(v), v.Model))) return null;
            foreach (var ped in World.GetNearbyPeds(center, 15)) if (ped.Exists() && box.Overlaps(new SpawnBox(Point(ped), -.5, .5, -.5, .5, -1, 1))) return null;
            double bottom = box.Z - box.HalfHeight;
            foreach (double fraction in new[] { .25, .5, .8 })
            {
                double height = bottom + box.HalfHeight * 2 * fraction;
                foreach (double side in new[] { -box.HalfWidth, 0, box.HalfWidth }) if (!ProbeClear(At(box, side, -box.HalfLength, height), At(box, side, box.HalfLength, height), IntersectFlags.Map | IntersectFlags.Objects)) return null;
                foreach (double along in new[] { -box.HalfLength, 0, box.HalfLength }) if (!ProbeClear(At(box, -box.HalfWidth, along, height), At(box, box.HalfWidth, along, height), IntersectFlags.Map | IntersectFlags.Objects)) return null;
            }
            return p;
        }
        public static bool ClearForTeleport(JobPoint point,Model model,Entity moving,Ped player)
        {
            var box=Bounds(point,model);var position=new Vector3(point.X,point.Y,point.Z);
            foreach(var v in World.GetNearbyVehicles(position,30))
                if(v.Exists() && v.Handle!=moving.Handle && box.Overlaps(Bounds(Point(v),v.Model)))return false;
            foreach(var p in World.GetNearbyPeds(position,20))
                if(p.Exists() && p.Handle!=player.Handle && !(moving is Vehicle car && p.IsInVehicle(car)) && box.Overlaps(new SpawnBox(Point(p),-.5,.5,-.5,.5,-1,1)))return false;
            return WorldClear(box,moving);
        }
        static bool Ground(JobPoint p,Model model)
        {
            float height;
            if(!World.GetGroundHeight(new Vector3(p.X,p.Y,p.Z+4),out height) || Math.Abs(height-p.Z)>2)return false;
            var box=Bounds(p,model);
            // Check footprint corners as well as center: reject ledges, roofs and steep uneven ground.
            foreach(double sx in new[]{-1.0,1.0})foreach(double sy in new[]{-1.0,1.0})
            {
                var corner=new Vector3((float)(box.X+sx*box.HalfWidth*box.RightX+sy*box.HalfLength*box.ForwardX),(float)(box.Y+sx*box.HalfWidth*box.RightY+sy*box.HalfLength*box.ForwardY),height+3);
                float z;if(!World.GetGroundHeight(corner,out z) || Math.Abs(z-height)>.6)return false;
            }
            Vector3 min,max;model.GetDimensions(out min,out max);
            p.Z=height-min.Z+.12f;return true;
        }
        static Vector3 At(SpawnBox b,double side,double along,double z) => new Vector3((float)(b.X+b.RightX*side+b.ForwardX*along),(float)(b.Y+b.RightY*side+b.ForwardY*along),(float)z);
        static bool ProbeClear(Vector3 start,Vector3 end,IntersectFlags flags,Entity ignore=null)
        {
            // This lookup runs once per contract: use completed synchronous results, never a pending async result as "clear".
            var handle=ShapeTest.StartExpensiveSyncTestLOSProbe(start,end,flags,ignore);
            ShapeTestResult result;
            return handle.GetResult(out result)==ShapeTestStatus.Ready && !result.DidHit;
        }
        static bool WorldClear(SpawnBox b,Entity ignore=null)
        {
            // Elevated probes avoid the ground while detecting walls, pillars and static map props.
            var flags=IntersectFlags.Map|IntersectFlags.Objects;
            double bottom=b.Z-b.HalfHeight;
            foreach(double z in new[]{bottom+.8,bottom+2.1,bottom+3.2})
            {
                foreach(double side in new[]{-b.HalfWidth,0,b.HalfWidth})
                    if(!ProbeClear(At(b,side,-b.HalfLength,z),At(b,side,b.HalfLength,z),flags,ignore))return false;
                foreach(double along in new[]{-b.HalfLength,0,b.HalfLength})
                    if(!ProbeClear(At(b,-b.HalfWidth,along,z),At(b,b.HalfWidth,along,z),flags,ignore))return false;
            }
            return true;
        }
    }
}
