using System;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class SleepRecovery : ExtensibleData
    {
        public string Id;public int Remaining=10000;
        [JsonIgnore]public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && Remaining>=0 && Remaining<=10000;
    }
    public static class SleepRecoveryRules
    {
        public static bool Safe(bool human,bool alive,bool foot,bool vehicle,bool entering,bool water,bool air,bool combat,bool ragdoll,bool mission,bool wanted,bool menu,bool control,float speed)=>human && alive && foot && !vehicle && !entering && !water && !air && !combat && !ragdoll && !mission && !wanted && !menu && control && GangPresenceRules.Finite(speed) && speed>=0 && speed<2;
        public static bool Begin(ref NeedsData d,string id,Action<NeedsData> save)
        {
            if(d==null || d.Sleep>0 || d.SleepRecovery!=null || string.IsNullOrWhiteSpace(id) || id.Length>80 || save==null)return false;
            var n=Copy(d);n.SleepRecovery=new SleepRecovery{Id=id};save(n);d=n;return true;
        }
        public static void Advance(NeedsData d,int delta,bool active)
        {if(d?.SleepRecovery?.Valid!=true || !active || delta<0 || delta>1000)return;d.SleepRecovery.Remaining=Math.Max(0,d.SleepRecovery.Remaining-delta);}
        public static bool Wake(ref NeedsData d,Action<NeedsData> save,bool nativeBed=false)
        {
            if(d==null || save==null || !nativeBed && (d.SleepRecovery?.Valid!=true || d.SleepRecovery.Remaining!=0) || d.SleepRecovery!=null && !d.SleepRecovery.Valid)return false;
            var n=Copy(d);n.Sleep=100;n.Stamina=100;n.StaminaCooldown=0;n.SleepRecovery=null;save(n);d=n;return true;
        }
        static NeedsData Copy(NeedsData d)=>JsonConvert.DeserializeObject<NeedsData>(JsonConvert.SerializeObject(d));
    }
    public sealed class NativeBedSleepMeter
    {
        int previous=-1,armed;bool beforeNear;
        public void Reset(){previous=-1;armed=0;beforeNear=false;}
        public bool Observe(int minute,int delta,bool nearHome,bool sleepScene,bool excluded)
        {
            if(minute<0 || minute>=1440 || excluded || delta<0 || delta>120000){Reset();return false;}
            int jump=previous<0?0:(minute-previous+1440)%1440;
            bool slept=beforeNear && nearHome && (armed>0 || sleepScene) && jump>=240 && jump<=720;
            armed=Math.Max(0,armed-delta);if(nearHome && sleepScene)armed=120000;
            previous=minute;beforeNear=nearHome;if(slept)armed=0;return slept;
        }
    }
}
