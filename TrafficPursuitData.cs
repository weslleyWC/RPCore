using System;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class TrafficPursuitOptions : ExtensibleData
    {
        public int VehicleSeconds=35,FootSeconds=45,BackupStars=3;public float EscapeDistance=300;
        [JsonIgnore] public bool Valid=>VehicleSeconds>=15 && VehicleSeconds<=180 && FootSeconds>=15 && FootSeconds<=180 && BackupStars>=1 && BackupStars<=3 && GangPresenceRules.Finite(EscapeDistance) && EscapeDistance>=150 && EscapeDistance<=500;
        public static TrafficPursuitOptions Defaults(){using(var s=typeof(TrafficPursuitOptions).Assembly.GetManifestResourceStream("RPCore.JobDefaults.TrafficPursuit.json"))using(var r=new System.IO.StreamReader(s))return JsonConvert.DeserializeObject<TrafficPursuitOptions>(r.ReadToEnd());}
    }
    public sealed class TrafficPursuitClock
    {
        public int VehicleElapsed{get;private set;}public int FootElapsed{get;private set;}public bool OnFoot{get;private set;}
        public void EnterFoot(){OnFoot=true;}
        // Only active movement away counts. Waiting safely for a stuck officer never raises stars.
        public bool Advance(int delta,bool fleeing,TrafficPursuitOptions options)
        {
            if(options?.Valid!=true || delta<0 || delta>1000 || !fleeing)return false;
            if(OnFoot)FootElapsed=Math.Min(options.FootSeconds*1000,FootElapsed+delta);else VehicleElapsed=Math.Min(options.VehicleSeconds*1000,VehicleElapsed+delta);
            return OnFoot?FootElapsed>=options.FootSeconds*1000:VehicleElapsed>=options.VehicleSeconds*1000;
        }
    }
    public sealed class TrafficBehaviourMeter
    {
        int vehicle;public int HornMilliseconds{get;private set;}
        public void Observe(int handle,bool horn,int delta,bool eligible)
        {
            if(!eligible || handle==0 || !horn || delta<0 || delta>1000){Reset();return;}
            if(vehicle!=handle){vehicle=handle;HornMilliseconds=0;return;}HornMilliseconds=Math.Min(60000,HornMilliseconds+delta);
        }
        public void Reset(){vehicle=0;HornMilliseconds=0;}
    }
    public static class TrafficPursuitRules
    {
        public static int BackupLevel(int current,int maximum,int requested)=>current==0 && maximum>=1 && maximum<=5 && requested>=1 && requested<=3?Math.Min(maximum,requested):0;
        public static bool FootInspection(bool onFoot,bool unarmed,float speed,float distance)=>onFoot && unarmed && GangPresenceRules.Finite(speed) && speed>=0 && speed<.5f && GangPresenceRules.Finite(distance) && distance>=0 && distance<=2.5f;
        public static bool Tailgate(float speed,float distance,float alignment,float height)=>GangPresenceRules.Finite(speed) && speed>=0 && speed*2.2369363>=10 && GangPresenceRules.Finite(distance) && distance>0 && distance<=10 && GangPresenceRules.Finite(alignment) && alignment>=.65f && alignment<=1.001f && GangPresenceRules.Finite(height) && Math.Abs(height)<=2.5f;
    }
}
