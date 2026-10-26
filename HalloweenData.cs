using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class HalloweenOptions : ExtensibleData
    {
        public int SecondsLimit=180,AwaitDriverSeconds=120,PulseCooldownSeconds=8,MaxAttackers=4,MaxSpawns=12,SpawnSeconds=10,RetaskMilliseconds=1500,RamCooldownMilliseconds=2800;
        public float PulseRadius=12,PulseSpeed=27,RamRange=36;
        public string HearseModel="romero",DriverModel="a_m_m_skidrow_01";
        public JobPoint Start,Hearse,Destination;
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)]public List<string> AttackerModels=new List<string>();
        [JsonIgnore]public bool Valid=>SecondsLimit>=60 && SecondsLimit<=600 && AwaitDriverSeconds>=30 && AwaitDriverSeconds<=300 && PulseCooldownSeconds>=4 && PulseCooldownSeconds<=30 && MaxAttackers>=1 && MaxAttackers<=6 && MaxSpawns>=MaxAttackers && MaxSpawns<=24 && SpawnSeconds>=5 && SpawnSeconds<=30 && RetaskMilliseconds>=500 && RetaskMilliseconds<=4000 && RamCooldownMilliseconds>=1500 && RamCooldownMilliseconds<=10000 && GangPresenceRules.Finite(PulseRadius) && PulseRadius>=4 && PulseRadius<=20 && GangPresenceRules.Finite(PulseSpeed) && PulseSpeed>=5 && PulseSpeed<=35 && GangPresenceRules.Finite(RamRange) && RamRange>=20 && RamRange<=50 && GangPresenceRules.ModelName(HearseModel) && GangPresenceRules.ModelName(DriverModel) && new[]{Start,Hearse,Destination}.All(p=>p?.Valid==true) && AttackerModels!=null && AttackerModels.Count>=1 && AttackerModels.Count<=8 && AttackerModels.All(GangPresenceRules.ModelName) && AttackerModels.Distinct(StringComparer.OrdinalIgnoreCase).Count()==AttackerModels.Count;
        public static HalloweenOptions Defaults(){using(var s=typeof(HalloweenOptions).Assembly.GetManifestResourceStream("RPCore.JobDefaults.Halloween.json"))using(var r=new System.IO.StreamReader(s))return JsonConvert.DeserializeObject<HalloweenOptions>(r.ReadToEnd());}
    }
    public sealed class HalloweenProgress : ExtensibleData
    {
        public bool Driving;public int Elapsed,Pulses,NextPulse;
        public bool Valid(HalloweenOptions c)=>c?.Valid==true && Elapsed>=0 && Elapsed<=c.SecondsLimit*1000 && Pulses>=0 && Pulses<=Elapsed/(c.PulseCooldownSeconds*1000)+1 && (Pulses==0?NextPulse==0:NextPulse>=c.PulseCooldownSeconds*1000 && NextPulse<=Elapsed+c.PulseCooldownSeconds*1000) && (Driving || Elapsed==0 && Pulses==0 && NextPulse==0);
    }
    public enum HalloweenStage { AwaitDriver,Running,Delivered,Failed }
    public sealed class HalloweenRun
    {
        public readonly string Id;public readonly HalloweenOptions Options;public HalloweenProgress Progress{get;private set;}public HalloweenStage Stage{get;private set;}
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        public HalloweenRun(string id,HalloweenOptions options,HalloweenProgress progress)
        {if(string.IsNullOrWhiteSpace(id) || options?.Valid!=true || progress?.Valid(options)!=true)throw new ArgumentException();Id=id;Options=Copy(options);Progress=Copy(progress);Stage=progress.Driving?HalloweenStage.Running:HalloweenStage.AwaitDriver;}
        public bool StartDriving(bool ownDriver){if(Stage!=HalloweenStage.AwaitDriver || !ownDriver)return false;Progress.Driving=true;Stage=HalloweenStage.Running;return true;}
        public void Tick(int delta,bool active){if(Stage!=HalloweenStage.Running || !active || delta<0 || delta>1000)return;Progress.Elapsed=Math.Min(Options.SecondsLimit*1000,Progress.Elapsed+delta);if(Progress.Elapsed>=Options.SecondsLimit*1000)Stage=HalloweenStage.Failed;}
        public bool Pulse(){if(Stage!=HalloweenStage.Running || Progress.Elapsed<Progress.NextPulse)return false;Progress.Pulses++;Progress.NextPulse=Progress.Elapsed+Options.PulseCooldownSeconds*1000;return true;}
        public bool Deliver(bool ownDriver,bool usable,float speed,float distance){if(Stage!=HalloweenStage.Running || !HalloweenRules.Arrival(ownDriver,usable,speed,distance))return false;Stage=HalloweenStage.Delivered;return true;}
    }
    public static class HalloweenRules
    {
        public static bool Arrival(bool driver,bool usable,float speed,float distance)=>driver && usable && GangPresenceRules.Finite(speed) && speed>=0 && speed<.5f && GangPresenceRules.Finite(distance) && distance>=0 && distance<=5;
        public static float PulseSpeed(HalloweenOptions c,float distance)=>c?.Valid==true && GangPresenceRules.Finite(distance) && distance>=0 && distance<=c.PulseRadius?c.PulseSpeed*(1-.35f*distance/c.PulseRadius):0;
        public static bool PulseTarget(bool ownCar,bool ownLivingDriver,bool driverSeated,bool foreignOccupants,bool connected,bool airborne,float distance,HalloweenOptions c)=>ownCar && ownLivingDriver && driverSeated && !foreignOccupants && !connected && !airborne && PulseSpeed(c,distance)>0;
        public static float Condition(float body,float engine)=>!GangPresenceRules.Finite(body) || !GangPresenceRules.Finite(engine)?0:Math.Max(0,Math.Min(100,Math.Min(body,engine)/10));
    }
    public sealed class HalloweenAttempt : ExtensibleData
    {
        public string Id;public HalloweenOptions Options;public HalloweenProgress Progress=new HalloweenProgress();
        [JsonIgnore]public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && Options?.Valid==true && Progress?.Valid(Options)==true;
    }
    public sealed class HalloweenRecord : ExtensibleData
    {
        public string Id,Outcome;public int Elapsed,Pulses;public float Condition;
        [JsonIgnore]public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && (Outcome=="completed" || Outcome=="failed" || Outcome=="interrupted") && Elapsed>=0 && Elapsed<=600000 && Pulses>=0 && Pulses<=151 && GangPresenceRules.Finite(Condition) && Condition>=0 && Condition<=100;
    }
    public sealed class HalloweenData : ExtensibleData
    {
        public bool Scares=true;public long Started,Completed,Failed,Interrupted,TotalPulses;public HalloweenAttempt Pending;public List<HalloweenRecord> History=new List<HalloweenRecord>();
        [JsonIgnore]public bool Valid=>Started>=0 && Completed>=0 && Failed>=0 && Interrupted>=0 && TotalPulses>=0 && (decimal)Started==(decimal)Completed+Failed+Interrupted+(Pending==null?0:1) && (Pending==null || Pending.Valid) && History!=null && History.Count<=32 && History.All(h=>h?.Valid==true && h.Id!=Pending?.Id) && History.Select(h=>h.Id).Distinct().Count()==History.Count && Completed>=History.Count(h=>h.Outcome=="completed") && Failed>=History.Count(h=>h.Outcome=="failed") && Interrupted>=History.Count(h=>h.Outcome=="interrupted") && (decimal)TotalPulses>=History.Sum(h=>(decimal)h.Pulses)+(Pending?.Progress.Pulses??0);
    }
    public static class HalloweenTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        static bool Apply(WalletData w,Action save,Func<HalloweenData,bool> change,out bool changed)
        {changed=false;if(w==null || save==null || w.Halloween!=null && !w.Halloween.Valid)return false;var old=w.Halloween;var n=old==null?new HalloweenData():Copy(old);if(!change(n) || !n.Valid)return false;try{w.Halloween=n;save();changed=true;return true;}catch{w.Halloween=old;throw;}}
        public static bool Configure(WalletData w,bool scares,Action save,out bool changed)=>Apply(w,save,b=>{if(b.Pending!=null)return false;b.Scares=scares;return true;},out changed);
        public static bool Begin(WalletData w,string id,HalloweenOptions options,Action save,out bool changed)
        {changed=false;if(string.IsNullOrWhiteSpace(id) || id.Length>80 || options?.Valid!=true)return false;return Apply(w,save,b=>{if(b.Pending!=null || b.Started==long.MaxValue || b.History.Any(h=>h.Id==id))return false;b.Pending=new HalloweenAttempt{Id=id,Options=Copy(options)};b.Started++;return true;},out changed);}
        public static bool Checkpoint(WalletData w,string id,HalloweenProgress progress,Action save,out bool changed)
        {
            changed=false;var p=w?.Halloween?.Pending;if(w?.Halloween?.Valid!=true || p==null || p.Id!=id || progress?.Valid(p.Options)!=true)return false;var old=p.Progress;int pulse=progress.Pulses-old.Pulses;
            if(old.Driving && !progress.Driving || progress.Elapsed<old.Elapsed || pulse<0 || pulse>1 || pulse==0 && progress.NextPulse!=old.NextPulse || pulse==1 && (progress.Elapsed>=p.Options.SecondsLimit*1000 || old.NextPulse>progress.Elapsed || progress.NextPulse!=progress.Elapsed+p.Options.PulseCooldownSeconds*1000) || w.Halloween.TotalPulses>long.MaxValue-pulse)return false;
            if(JsonConvert.SerializeObject(old)==JsonConvert.SerializeObject(progress))return true;
            return Apply(w,save,b=>{b.Pending.Progress=Copy(progress);b.TotalPulses+=pulse;return true;},out changed);
        }
        public static bool Finish(WalletData w,string id,string outcome,float condition,HalloweenRun proof,Action save,out bool changed)
        {
            changed=false;if(w?.Halloween?.Valid!=true || !GangPresenceRules.Finite(condition) || condition<0 || condition>100 || (outcome!="completed" && outcome!="failed" && outcome!="interrupted"))return false;var previous=w.Halloween.History.FirstOrDefault(h=>h.Id==id);if(previous!=null)return previous.Outcome==outcome;
            var p=w.Halloween.Pending;if(p==null || p.Id!=id || outcome=="completed" && (proof?.Id!=id || proof.Stage!=HalloweenStage.Delivered || !p.Progress.Driving || p.Progress.Elapsed>=p.Options.SecondsLimit*1000 || JsonConvert.SerializeObject(proof.Progress)!=JsonConvert.SerializeObject(p.Progress)))return false;
            return Apply(w,save,b=>{if(outcome=="completed"){if(b.Completed==long.MaxValue)return false;b.Completed++;}else if(outcome=="failed"){if(b.Failed==long.MaxValue)return false;b.Failed++;}else{if(b.Interrupted==long.MaxValue)return false;b.Interrupted++;}b.History.Add(new HalloweenRecord{Id=id,Outcome=outcome,Elapsed=p.Progress.Elapsed,Pulses=p.Progress.Pulses,Condition=condition});if(b.History.Count>32)b.History.RemoveAt(0);b.Pending=null;return true;},out changed);
        }
    }
}
