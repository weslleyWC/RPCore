using System;
namespace RPCore
{
    public static class DmvRigRules
    {
        public static float HeadingDifference(float a,float b)
        {if(!DmvDrive.Finite(a) || !DmvDrive.Finite(b))return float.PositiveInfinity;return Math.Abs(((a-b)%360+540)%360-180);}
        public static bool InBay(float distance,float vertical,float heading,float expectedHeading,float truckSpeed,float trailerSpeed)
        {return DmvDrive.Finite(distance) && DmvDrive.Finite(vertical) && DmvDrive.Finite(truckSpeed) && DmvDrive.Finite(trailerSpeed) && distance>=0 && distance<=4 && Math.Abs(vertical)<=3 && HeadingDifference(heading,expectedHeading)<=20 && truckSpeed>=0 && truckSpeed<.5f && trailerSpeed>=0 && trailerSpeed<.5f;}
        public static float ReverseDistance(float accumulated,float moved,float forwardSpeed,bool coupled,bool driver,bool protectedDriver)
        {if(!DmvDrive.Finite(accumulated) || accumulated<0)throw new ArgumentException("Invalid reverse distance.");if(!coupled || !driver || !protectedDriver || !DmvDrive.Finite(moved) || moved<0 || moved>10 || !DmvDrive.Finite(forwardSpeed) || forwardSpeed>=-.25f)return accumulated;return Math.Min(1000,accumulated+moved);}
    }
}
