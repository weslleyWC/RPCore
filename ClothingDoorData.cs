using System;
using System.Collections.Generic;
using System.Linq;

namespace RPCore
{
    public static class ClothingDoorRules
    {
        // Only the four clothing-door models from PSRP's broader UnlockDoors routine.
        public static int[] Models()=>new[]{-1922281023,1780022985,868499217,-1148826190};
        public static JobPoint[] Entrances()=>new[]{
            new JobPoint{Name="Textile City",X=418.1723f,Y=-806.8669f,Z=29.41125f},
            new JobPoint{Name="Vespucci",X=-817.7585f,Y=-1079.574f,Z=11.13251f},
            new JobPoint{Name="Del Perro",X=-1201.608f,Y=-777.7211f,Z=17.33837f},
            new JobPoint{Name="Harmony",X=618.1323f,Y=2750.428f,Z=42.08915f},
            new JobPoint{Name="Hawick",X=127.2596f,Y=-211.0862f,Z=54.53087f},
            new JobPoint{Name="Chumash",X=-3168.231f,Y=1056.463f,Z=20.85899f},
            new JobPoint{Name="Rockford Hills",X=-716.6546f,Y=-156.5079f,Z=37.17977f},
            new JobPoint{Name="Burton",X=-156.4434f,Y=-306.0515f,Z=39.51084f},
            new JobPoint{Name="Morningwood",X=-1455.821f,Y=-232.1667f,Z=49.78724f},
            new JobPoint{Name="Strawberry",X=82.80602f,Y=-1391.531f,Z=29.40647f},
            new JobPoint{Name="Grapeseed",X=1686.582f,Y=4820.392f,Z=41.97805f},
            new JobPoint{Name="Route 68",X=-1095.509f,Y=2705.738f,Z=19.08479f},
            new JobPoint{Name="Grand Senora",X=1197.869f,Y=2702.819f,Z=38.15607f},
            new JobPoint{Name="Paleto Bay",X=-1.200041f,Y=6516.809f,Z=31.86993f}
        };
        static bool Finite(float v)=>!float.IsNaN(v) && !float.IsInfinity(v);
        public static bool Near(JobPoint door,float x,float y,float z)
        {
            if(door==null || !door.Valid || !Finite(x) || !Finite(y) || !Finite(z))return false;
            double dx=(double)x-door.X,dy=(double)y-door.Y,dz=(double)z-door.Z;
            return Math.Abs(dz)<=3 && dx*dx+dy*dy+dz*dz<=36;
        }
        public static bool Active(JobPoint door,IEnumerable<JobPoint> shops)
        {
            if(door==null || !door.Valid || shops==null)return false;
            return shops.Any(p=>p?.Valid==true && Math.Abs(p.Z-door.Z)<=3 &&
                ((double)p.X-door.X)*(p.X-door.X)+((double)p.Y-door.Y)*(p.Y-door.Y)<=25*25);
        }
    }
}
