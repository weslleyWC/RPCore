using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangDriveByOptions : ExtensibleData
    {
        public string VehicleModel="granger";public int MinIntervalSeconds=300,MaxIntervalSeconds=900,Chance=85,ApproachSeconds=45,ShootingMilliseconds=7500,EscapeSeconds=20,ShootDistance=35;
        [JsonIgnore] public bool Valid=>GangPresenceRules.ModelName(VehicleModel) && MinIntervalSeconds>=60 && MaxIntervalSeconds>=MinIntervalSeconds && MaxIntervalSeconds<=3600 && Chance>=0 && Chance<=100 && ApproachSeconds>=10 && ApproachSeconds<=120 && ShootingMilliseconds>=1000 && ShootingMilliseconds<=15000 && EscapeSeconds>=5 && EscapeSeconds<=60 && ShootDistance>=10 && ShootDistance<=60;
    }
    public sealed class GangDriveByRun : ExtensibleData
    {
        public string Id,Gang,Rival;public GangDriveByOptions Options;public string[] Models;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && GangDiplomacyRules.Pair(Gang,Rival) && Options?.Valid==true && Models!=null && Models.Length==4 && Models.All(GangPresenceRules.ModelName);
    }
    public sealed class GangDriveByRecord : ExtensibleData
    {
        public string Id,Gang,Rival,Outcome;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && GangDiplomacyRules.Pair(Gang,Rival) && GangDriveByRules.Outcome(Outcome);
    }
    public sealed class GangDriveByData : ExtensibleData
    {
        public bool Enabled;public GangDriveByRun Pending;public long Started,Repelled,Escaped,Interrupted,Deaths;public List<GangDriveByRecord> History=new List<GangDriveByRecord>();
        [JsonIgnore] public bool Valid=>Started>=0 && Repelled>=0 && Escaped>=0 && Interrupted>=0 && Deaths>=0 && (decimal)Repelled+Escaped+Interrupted+Deaths<=Started && (Pending==null || Pending.Valid && (decimal)Repelled+Escaped+Interrupted+Deaths<Started) && History!=null && History.Count<=32 && History.All(r=>r?.Valid==true) && History.Select(r=>r.Id).Distinct().Count()==History.Count;
    }
    public static class GangDriveByRules
    {
        public static bool Outcome(string outcome)=>outcome=="repelled" || outcome=="escaped" || outcome=="interrupted" || outcome=="death";
        public static bool Outdoor(bool human,bool alive,bool interior,bool airborne,bool water)=>human && alive && !interior && !airborne && !water;
        public static bool Outside(float distance,float radius)=>GangPresenceRules.Finite(distance) && distance>=0 && radius>=0 && distance>radius;
        public static bool Spawn(float distance,float height)=>GangPresenceRules.Finite(distance) && GangPresenceRules.Finite(height) && distance>=75 && distance<=170 && Math.Abs(height)<=8;
        public static bool Shoot(float distance,bool driver,bool gunner,GangDriveByOptions c)=>c?.Valid==true && GangPresenceRules.Finite(distance) && distance>=0 && distance<=c.ShootDistance && driver && gunner;
    }
    public enum GangDriveByStage { Approach,Shooting,Escape,Finished }
    public sealed class GangDriveByClock
    {
        readonly GangDriveByOptions options;int last;bool previousEligible;
        public GangDriveByStage Stage{get;private set;}public int Elapsed{get;private set;}public bool Repelled{get;private set;}
        public GangDriveByClock(GangDriveByOptions c,int now){if(c?.Valid!=true)throw new ArgumentException("Invalid drive-by clock options.");options=JsonConvert.DeserializeObject<GangDriveByOptions>(JsonConvert.SerializeObject(c));last=now;}
        public void Tick(int now,bool eligible,float distance,bool driver,bool gunner)
        {
            int delta=GangWarRules.Delta(last,now,eligible && previousEligible);last=now;previousEligible=eligible;if(!eligible || Stage==GangDriveByStage.Finished)return;
            if(!driver){Repelled=true;Stage=GangDriveByStage.Finished;return;}
            Elapsed+=delta;
            if(Stage==GangDriveByStage.Escape){if(Elapsed>=options.EscapeSeconds*1000 || GangDriveByRules.Outside(distance,220))Stage=GangDriveByStage.Finished;return;}
            if(!gunner){Repelled=true;Stage=GangDriveByStage.Escape;Elapsed=0;return;}
            if(Stage==GangDriveByStage.Approach && Elapsed>=options.ApproachSeconds*1000){Stage=GangDriveByStage.Escape;Elapsed=0;return;}
            if(Stage==GangDriveByStage.Approach && GangDriveByRules.Shoot(distance,driver,gunner,options)){Stage=GangDriveByStage.Shooting;Elapsed=0;return;}
            if(Stage==GangDriveByStage.Shooting && Elapsed>=options.ShootingMilliseconds){Stage=GangDriveByStage.Escape;Elapsed=0;}
        }
    }
    public static class GangDriveByTransactions
    {
        static bool Apply(GangState s,Action<GangState> save,Func<GangDriveByData,bool> change,out GangState result)
        {
            result=s;if(s==null || save==null || s.DriveBys!=null && !s.DriveBys.Valid)return false;
            var n=JsonConvert.DeserializeObject<GangState>(JsonConvert.SerializeObject(s));n.DriveBys=n.DriveBys??new GangDriveByData();if(!change(n.DriveBys) || !n.DriveBys.Valid)return false;save(n);result=n;return true;
        }
        public static bool Configure(GangState s,bool enabled,Action<GangState> save,out GangState result)=>Apply(s,save,b=>{b.Enabled=enabled;return true;},out result);
        public static bool Begin(GangState s,GangDriveByRun run,Action<GangState> save,out GangState result)=>Apply(s,save,b=>{
            if(run?.Valid!=true || !b.Enabled || b.Pending!=null || b.Started==long.MaxValue || !GangCrewRules.SameGang(s.Gang,run.Gang) || !GangWarRules.Free(s) || s.Warfare!=null && (!s.Warfare.Valid || s.Warfare.Alert!=null) || b.History.Any(r=>r.Id==run.Id))return false;
            b.Pending=JsonConvert.DeserializeObject<GangDriveByRun>(JsonConvert.SerializeObject(run));b.Started++;return true;},out result);
        public static bool Finish(GangState s,string id,string outcome,Action<GangState> save,out GangState result)
        {
            result=s;if(!GangDriveByRules.Outcome(outcome) || s?.DriveBys?.Valid!=true || string.IsNullOrWhiteSpace(id))return false;
            var old=s.DriveBys.History.FirstOrDefault(r=>r.Id==id);if(old!=null)return old.Outcome==outcome;
            return Apply(s,save,b=>{var p=b.Pending;if(p?.Id!=id)return false;
                if(outcome=="repelled"){if(b.Repelled==long.MaxValue)return false;b.Repelled++;}else if(outcome=="escaped"){if(b.Escaped==long.MaxValue)return false;b.Escaped++;}else if(outcome=="death"){if(b.Deaths==long.MaxValue)return false;b.Deaths++;}else{if(b.Interrupted==long.MaxValue)return false;b.Interrupted++;}
                b.History.Add(new GangDriveByRecord{Id=p.Id,Gang=p.Gang,Rival=p.Rival,Outcome=outcome});if(b.History.Count>32)b.History.RemoveAt(0);b.Pending=null;return true;},out result);
        }
    }
}
