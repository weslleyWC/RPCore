using System;
using System.Collections.Generic;
using System.Linq;
namespace RPCore
{
    public static class GangCrewRules
    {
        public const int Limit=3;
        static bool Range(float distance,float max)=>GangPresenceRules.Finite(distance) && distance>=0 && distance<=max;
        public static bool SameGang(string affiliation,string gang)=>!string.IsNullOrWhiteSpace(affiliation) && !string.IsNullOrWhiteSpace(gang) && string.Equals(affiliation,gang,StringComparison.OrdinalIgnoreCase);
        public static bool Recruit(string affiliation,string gang,int count,float distance,bool owned,bool leader,bool already,bool alive,bool onFoot,bool calm)
            =>SameGang(affiliation,gang) && count>=0 && count<Limit && Range(distance,3) && owned && !leader && !already && alive && onFoot && calm;
        public static bool Keep(string affiliation,string gang,float distance,bool alive)=>alive && SameGang(affiliation,gang) && Range(distance,180);
        public static bool Board(bool driver,bool car,bool company,bool alive,float speed,float distance)=>driver && car && !company && alive && GangPresenceRules.Finite(speed) && speed>=0 && speed<.5f && Range(distance,25);
        public static int Seat(IEnumerable<int> available,IEnumerable<int> reserved)
        {if(available==null)return -2;var held=new HashSet<int>(reserved??Enumerable.Empty<int>());return available.Where(i=>i>=0 && i<8 && !held.Contains(i)).OrderBy(i=>i).DefaultIfEmpty(-2).First();}
        public static bool CanExit(float speed)=>GangPresenceRules.Finite(speed) && speed>=0 && speed<.5f;
    }
}
