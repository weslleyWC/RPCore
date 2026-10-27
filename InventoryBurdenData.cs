using System;
using System.Collections.Generic;

namespace RPCore
{
    public static class InventoryBurdenRules
    {
        public static double Total(IDictionary<string,int> items,Func<string,float> weight)
        {
            if(items==null)return 0;double total=0;
            foreach(var p in items)
            {
                if(p.Value<=0)continue;float unit=weight==null ? .1f : weight(p.Key);
                if(float.IsNaN(unit) || float.IsInfinity(unit) || unit<0)unit=.1f;
                total+=(double)unit*p.Value;
            }
            return total;
        }
        public static float MoveRate(double kilograms,double capacity)
        {
            if(double.IsNaN(kilograms) || double.IsInfinity(kilograms) || kilograms<0 || double.IsNaN(capacity) || double.IsInfinity(capacity) || capacity<=0 || kilograms<=capacity+.001)return 1;
            return (float)Math.Max(.45,.8-.35*(kilograms/capacity-1));
        }
        public static bool Safe(bool human,bool alive,bool foot,bool vehicle,bool boarding,bool swimming,bool airborne,bool ragdoll,bool climbing,bool paused,bool mission,bool cutscene,bool control,bool sameProfile)
            =>human && alive && foot && !vehicle && !boarding && !swimming && !airborne && !ragdoll && !climbing && !paused && !mission && !cutscene && control && sameProfile;
    }
}
