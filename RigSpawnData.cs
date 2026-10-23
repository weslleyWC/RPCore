using System;
using System.Collections.Generic;
using System.Linq;

namespace RPCore
{
    // Oriented model bounds; parked vehicles beside a bay do not occupy the bay itself.
    public sealed class SpawnBox
    {
        public readonly double X,Y,Z,RightX,RightY,ForwardX,ForwardY,HalfWidth,HalfLength,HalfHeight;
        public SpawnBox(JobPoint origin,double minX,double maxX,double minY,double maxY,double minZ,double maxZ)
        {
            if(origin==null || !origin.Valid || new[]{minX,maxX,minY,maxY,minZ,maxZ}.Any(v=>double.IsNaN(v)||double.IsInfinity(v)) || maxX<=minX || maxY<=minY || maxZ<=minZ)throw new ArgumentException("Invalid spawn bounds.");
            double a=origin.Heading*Math.PI/180;RightX=Math.Cos(a);RightY=Math.Sin(a);ForwardX=-RightY;ForwardY=RightX;
            X=origin.X+RightX*(minX+maxX)/2+ForwardX*(minY+maxY)/2;
            Y=origin.Y+RightY*(minX+maxX)/2+ForwardY*(minY+maxY)/2;Z=origin.Z+(minZ+maxZ)/2;
            HalfWidth=(maxX-minX)/2;HalfLength=(maxY-minY)/2;HalfHeight=(maxZ-minZ)/2;
        }
        public bool Overlaps(SpawnBox b,double clearance=.5)
        {
            if(b==null || double.IsNaN(clearance) || double.IsInfinity(clearance) || clearance<0)throw new ArgumentException();
            if(Math.Abs(Z-b.Z)>HalfHeight+b.HalfHeight+clearance)return false;
            return Axis(b,RightX,RightY,clearance) && Axis(b,ForwardX,ForwardY,clearance) && Axis(b,b.RightX,b.RightY,clearance) && Axis(b,b.ForwardX,b.ForwardY,clearance);
        }
        bool Axis(SpawnBox b,double ax,double ay,double margin)
        {
            double ours=HalfWidth*Math.Abs(RightX*ax+RightY*ay)+HalfLength*Math.Abs(ForwardX*ax+ForwardY*ay);
            double theirs=b.HalfWidth*Math.Abs(b.RightX*ax+b.RightY*ay)+b.HalfLength*Math.Abs(b.ForwardX*ax+b.ForwardY*ay);
            return Math.Abs((X-b.X)*ax+(Y-b.Y)*ay)<=ours+theirs+margin;
        }
    }
    public sealed class RigSpawnSlot
    {
        public JobPoint Trailer,Truck;
    }
    public static class RigSpawnSearch
    {
        public static IEnumerable<RigSpawnSlot> Candidates(JobPoint anchor)
        {
            if(anchor==null || !anchor.Valid)throw new ArgumentException("Invalid rig anchor.");
            double a=anchor.Heading*Math.PI/180,rx=Math.Cos(a),ry=Math.Sin(a),fx=-ry,fy=rx;
            var offsets=new List<Tuple<int,int>>();
            for(int side=-3;side<=3;side++)for(int along=-2;along<=2;along++)offsets.Add(Tuple.Create(side*8,along*16));
            foreach(var offset in offsets.OrderBy(o=>o.Item1*o.Item1+o.Item2*o.Item2))
            {
                float x=(float)(anchor.X+rx*offset.Item1+fx*offset.Item2),y=(float)(anchor.Y+ry*offset.Item1+fy*offset.Item2);
                yield return new RigSpawnSlot {
                    Trailer=new JobPoint{Name="tanker spawn",X=x,Y=y,Z=anchor.Z,Heading=anchor.Heading},
                    Truck=new JobPoint{Name="tractor spawn",X=x+(float)fx*19,Y=y+(float)fy*19,Z=anchor.Z,Heading=anchor.Heading}
                };
            }
        }
        public static RigSpawnSlot Find(JobPoint anchor,Func<RigSpawnSlot,bool> available)
        {if(available==null)throw new ArgumentNullException(nameof(available));return Candidates(anchor).FirstOrDefault(available);}
    }
}
