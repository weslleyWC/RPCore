using System;
namespace RPCore
{
    // These transient points go through JobPoint.Valid just like configured points.
    public static class PdmSpawnPoints
    {
        public static JobPoint Tractor(float trailerX,float trailerY,float trailerZ,float heading)
        {
            double angle=heading*Math.PI/180;
            var p=new JobPoint{Name="PDM / caminhao",X=trailerX-(float)Math.Sin(angle)*19,Y=trailerY+(float)Math.Cos(angle)*19,Z=trailerZ,Heading=heading};
            if(!p.Valid)throw new ArgumentException("Invalid PDM tractor anchor.");
            return p;
        }
        public static JobPoint Cargo(JobPoint dock,int index)
        {
            if(dock==null || !dock.Valid || index<0 || index>1)throw new ArgumentException("Invalid PDM cargo anchor.");
            var p=new JobPoint{Name="PDM / encomenda "+(index+1),X=dock.X,Y=dock.Y+index*8,Z=dock.Z,Heading=dock.Heading};
            if(!p.Valid)throw new ArgumentException("Invalid PDM cargo anchor.");
            return p;
        }
    }
}
