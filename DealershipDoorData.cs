using System;
namespace RPCore
{
    public sealed class DealershipDoor
    {
        public readonly int Model;
        public readonly float X,Y,Z;
        public DealershipDoor(int model,float x,float y,float z){Model=model;X=x;Y=y;Z=z;}
    }
    public static class DealershipDoorRules
    {
        // v_ilev_csr_door_l/r, also present in PSRP.MultiplayerCharacter.UnlockDoors.
        // Only the showroom pedestrian entrances; never its roller/garage door.
        public static DealershipDoor[] Doors()=>new[]{
            new DealershipDoor(2059227086,-59.89302f,-1092.951f,26.88362f),
            new DealershipDoor(1417577297,-60.54582f,-1094.749f,26.88872f),
            new DealershipDoor(2059227086,-39.13366f,-1108.218f,26.7198f),
            new DealershipDoor(1417577297,-37.33113f,-1108.873f,26.7198f)};
        static bool Finite(float n)=>!float.IsNaN(n) && !float.IsInfinity(n);
        public static bool Near(DealershipDoor door,float x,float y,float z)
        {
            if(door==null || !Finite(x) || !Finite(y) || !Finite(z))return false;
            double dx=(double)x-door.X,dy=(double)y-door.Y,dz=(double)z-door.Z;
            return Math.Abs(dz)<=3 && dx*dx+dy*dy+dz*dz<=36;
        }
        public static bool ShowroomRange(float x,float y,float z,float radius)
        {
            if(!Finite(x) || !Finite(y) || !Finite(z) || !Finite(radius) || radius<0 || radius>70)return false;
            double dx=(double)x+48,dy=(double)y+1104,dz=(double)z-26.4;
            return Math.Abs(dz)<=8 && dx*dx+dy*dy+dz*dz<=radius*radius;
        }
    }
}
