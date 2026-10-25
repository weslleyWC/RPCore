using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public enum GangDrivingStage { Boarding,Target,Escape,Return,Report }
    public sealed class GangDrivingOptions : ExtensibleData
    {
        public int SpawnRadius=115,AttackRadius=55,TargetRadius=160,EscapeRadius=150,SecondsLimit=1200;
        public List<GangSpot> Targets=new List<GangSpot>();
        [JsonIgnore] public bool Valid=>SpawnRadius>=60 && SpawnRadius<=140 && AttackRadius>=15 && AttackRadius<SpawnRadius && TargetRadius>SpawnRadius && TargetRadius<=250 && EscapeRadius>=100 && EscapeRadius<=500 && SecondsLimit>=180 && SecondsLimit<=3600 && Targets!=null && Targets.Count>=1 && Targets.Count<=100 && Targets.All(GangPresenceRules.Position);
        public static GangDrivingOptions Defaults(){using(var s=typeof(GangDrivingOptions).Assembly.GetManifestResourceStream("RPCore.JobDefaults.GangDriving.json"))using(var r=new System.IO.StreamReader(s))return JsonConvert.DeserializeObject<GangDrivingOptions>(r.ReadToEnd());}
    }
    public sealed class GangDrivingAttempt : ExtensibleData
    {
        public string Id,Gang,CrewModel,TargetModel;public GangSpot Home,Target;public GangDrivingOptions Options;public GangDrivingStage Stage;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && !string.IsNullOrWhiteSpace(Gang) && Gang.Length<=100 && GangPresenceRules.ModelName(CrewModel) && GangPresenceRules.ModelName(TargetModel) && GangPresenceRules.Position(Home) && GangPresenceRules.Position(Target) && Options!=null && Options.Valid && Enum.IsDefined(typeof(GangDrivingStage),Stage);
    }
    public sealed class GangDrivingData : ExtensibleData
    {
        public int Attempts,Completed;public GangDrivingAttempt Pending;public string LastResult;public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>Attempts>=0 && Completed>=0 && Completed<=Attempts && (Pending==null || Pending.Valid && Attempts>Completed) && Receipts!=null && Receipts.Count<=128 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=110) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public static class GangDrivingRules
    {
        public static bool CrewKill(bool dead,int killer,int crew,int player,int vehicle,bool playerDamage,bool vehicleDamage)=>dead && crew!=0 && crew!=player && crew!=vehicle && killer==crew && !playerDamage && !vehicleDamage;
        public static bool Escaped(float distance,bool driver,bool passenger,GangDrivingOptions c)=>c!=null && c.Valid && GangPresenceRules.Finite(distance) && distance>=c.EscapeRadius && driver && passenger;
        public static bool Returned(float distance,float crewDistance,float speed,bool driver,bool passenger)=>GangPresenceRules.Finite(distance) && GangPresenceRules.Finite(crewDistance) && GangCrewRules.CanExit(speed) && distance>=0 && distance<=15 && crewDistance>=0 && crewDistance<=20 && driver && passenger;
    }
    public static class GangDrivingTransactions
    {
        static T Copy<T>(T value)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        static bool Apply(GangState state,string receipt,Func<GangState,bool> change,Action<GangState> save,out GangState result)
        {
            result=state;if(state==null || save==null || string.IsNullOrWhiteSpace(receipt) || receipt.Length>110 || state.Driving!=null && !state.Driving.Valid)return false;
            if(state.Driving?.Receipts.Contains(receipt)==true)return true;
            var next=Copy(state);next.Driving=next.Driving??new GangDrivingData();if(!change(next) || !next.Driving.Valid)return false;
            next.Driving.Receipts.Add(receipt);if(next.Driving.Receipts.Count>128)next.Driving.Receipts.RemoveAt(0);save(next);result=next;return true;
        }
        public static bool Begin(GangState state,GangDrivingAttempt p,Action<GangState> save,out GangState result)
        {result=state;if(p==null || !p.Valid || p.Stage!=GangDrivingStage.Boarding)return false;return Apply(state,p.Id+":begin",s=>{if(!GangLoyaltyRules.Unaffiliated(s.Gang) || s.Driving.Pending!=null || s.Loyalty?.Pending!=null || s.Turf?.Pending!=null || s.Strength?.Pending!=null || s.Driving.Attempts==int.MaxValue)return false;s.Driving.Pending=Copy(p);s.Driving.Attempts++;return true;},save,out result);}
        public static bool Advance(GangState state,string id,GangDrivingStage expected,Action<GangState> save,out GangState result)=>Apply(state,Guid.NewGuid().ToString("N")+":advance",s=>{var p=s.Driving.Pending;if(p==null || p.Id!=id || p.Stage!=expected || !Enum.IsDefined(typeof(GangDrivingStage),expected) || expected==GangDrivingStage.Report || !GangLoyaltyRules.Unaffiliated(s.Gang))return false;p.Stage=(GangDrivingStage)((int)p.Stage+1);return true;},save,out result);
        public static bool Restart(GangState state,string id,Action<GangState> save,out GangState result)=>Apply(state,Guid.NewGuid().ToString("N")+":restart",s=>{var p=s.Driving.Pending;if(p==null || p.Id!=id || p.Stage==GangDrivingStage.Report || !GangLoyaltyRules.Unaffiliated(s.Gang))return false;p.Stage=GangDrivingStage.Boarding;return true;},save,out result);
        public static bool Complete(GangState state,string id,Action<GangState> save,out GangState result)=>Apply(state,id+":complete",s=>{var p=s.Driving.Pending;if(p==null || p.Id!=id || p.Stage!=GangDrivingStage.Report || !GangLoyaltyRules.Unaffiliated(s.Gang) || s.Driving.Completed==int.MaxValue)return false;s.Gang=p.Gang;s.Reputation=0;s.Rank=1;s.Driving.Pending=null;s.Driving.Completed++;s.Driving.LastResult="completed";return true;},save,out result);
        public static bool End(GangState state,string id,bool failed,Action<GangState> save,out GangState result)=>Apply(state,id+":end",s=>{if(s.Driving.Pending==null || s.Driving.Pending.Id!=id)return false;s.Driving.Pending=null;s.Driving.LastResult=failed?"failed":"cancelled";return true;},save,out result);
    }
}
