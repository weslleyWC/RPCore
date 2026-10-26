using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace RPCore
{
    public static class GangLeaderRules
    {
        // Keep the original extension field intact, including invalid/custom legacy values.
        public static int Initiation(GangDefinition gang)
        {
            if(gang==null)return -1;if(gang.Extra==null || !gang.Extra.TryGetValue("InitiationType",out var value))return 0;
            if(value==null)return -1;
            if(value.Type==JTokenType.Integer){return long.TryParse(value.ToString(),out long n) && n>=0 && n<=2?(int)n:-1;}
            string text=value.Type==JTokenType.String?value.Value<string>():null;
            return string.Equals(text,"Loyalty",StringComparison.OrdinalIgnoreCase)?0:string.Equals(text,"Strength",StringComparison.OrdinalIgnoreCase)?1:string.Equals(text,"Driving",StringComparison.OrdinalIgnoreCase)?2:-1;
        }
        public static bool Near(float distance,bool opened)=>GangPresenceRules.Finite(distance) && distance>=0 && distance<=(opened?4f:1.5f);
        public static bool CanLeave(GangState s,string leader)=>s!=null && GangCrewRules.SameGang(s.Gang,leader) && GangWarRules.Free(s) && (s.Warfare==null || s.Warfare.Valid && s.Warfare.Alert==null) && (s.DriveBys==null || s.DriveBys.Valid && s.DriveBys.Pending==null);
        public static bool Leave(GangState s,string leader,Action<GangState> save,out GangState result)
        {
            result=s;if(save==null || !CanLeave(s,leader))return false;
            var n=JsonConvert.DeserializeObject<GangState>(JsonConvert.SerializeObject(s));n.Gang=null;n.Reputation=0;n.Rank=1;save(n);result=n;return true;
        }
    }
}
