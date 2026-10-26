using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangWarOptions : ExtensibleData
    {
        public int ResponseSeconds=120,RivalMinSeconds=300,RivalMaxSeconds=900,RemoteRadius=200;
        [JsonIgnore] public bool Valid=>ResponseSeconds>=60 && ResponseSeconds<=900 && RivalMinSeconds>=60 && RivalMaxSeconds>=RivalMinSeconds && RivalMaxSeconds<=3600 && RemoteRadius>=150 && RemoteRadius<=1000;
    }
    public sealed class GangWarAlert : ExtensibleData
    {
        public string Id,Gang,Attacker;public GangTurfDefinition Area;public int Elapsed,Limit=120000;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && GangDiplomacyRules.Pair(Gang,Attacker) && Area?.Valid==true && Limit>=60000 && Limit<=900000 && Elapsed>=0 && Elapsed<=Limit;
    }
    public sealed class GangWarRecord : ExtensibleData
    {
        public string Id,Attacker,Defender,Result;public int Area;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && GangDiplomacyRules.Pair(Defender,Attacker) && Area>0 && (Result=="unanswered" || Result=="responded" || Result=="dismissed" || Result=="rival_capture" || Result=="rival_defense");
    }
    public sealed class GangWarData : ExtensibleData
    {
        public bool AttacksEnabled,RivalsEnabled,Reinforcements;public GangWarAlert Alert;
        public long Unanswered,Responded,RivalBattles;public List<GangWarRecord> History=new List<GangWarRecord>();
        [JsonIgnore] public bool Valid=>Unanswered>=0 && Responded>=0 && RivalBattles>=0 && (Alert==null || Alert.Valid) && History!=null && History.Count<=32 && History.All(r=>r?.Valid==true) && History.Select(r=>r.Id).Distinct().Count()==History.Count;
    }
    public static class GangWarRules
    {
        public static int AttackSeconds(int owned)=>owned<=1?780:owned==2?720:owned==3?660:owned==4?600:owned==5?540:owned<=7?480:owned<=9?420:owned<=11?360:owned<=13?300:240;
        public static bool Hostile(string attitude)=>attitude=="Hate" || attitude=="War";
        public static int WinChance(int attackers,int defenders)=>attackers<defenders?55:attackers>defenders?45:50;
        public static bool Reinforce(int initial,int defeated,int spawned,int total)=>initial>=1 && initial<=3 && total>initial && total<=6 && spawned==initial && defeated>=((initial+1)/2) && defeated<=initial;
        public static int Delta(int previous,int current,bool eligible)=>eligible?(int)Math.Min(1000,Math.Max(0,(long)unchecked(current-previous))):0;
        public static bool Free(GangState s)=>s!=null && (s.DriveBys==null || s.DriveBys.Valid && s.DriveBys.Pending==null) && (s.Turf==null || s.Turf.Valid && s.Turf.Pending==null) && s.Loyalty?.Pending==null && s.Strength?.Pending==null && s.Driving?.Pending==null;
        internal static void Record(GangWarData book,string id,string attacker,string defender,int area,string result)
        {book.History.Add(new GangWarRecord{Id=id,Attacker=attacker,Defender=defender,Area=area,Result=result});if(book.History.Count>32)book.History.RemoveAt(0);}
    }
    public static class GangWarTransactions
    {
        static bool Apply(GangState s,Action<GangState> save,Func<GangState,bool> change,out GangState result)
        {
            result=s;if(s==null || save==null || s.Warfare!=null && !s.Warfare.Valid || s.Turf!=null && !s.Turf.Valid)return false;
            var n=JsonConvert.DeserializeObject<GangState>(JsonConvert.SerializeObject(s));n.Warfare=n.Warfare??new GangWarData();
            if(!change(n) || !n.Warfare.Valid || n.Turf!=null && !n.Turf.Valid)return false;save(n);result=n;return true;
        }
        public static bool Configure(GangState s,string feature,bool enabled,Action<GangState> save,out GangState result)=>Apply(s,save,n=>{
            if(feature=="attacks") {n.Warfare.AttacksEnabled=enabled;if(!enabled && n.Warfare.Alert!=null){var a=n.Warfare.Alert;GangWarRules.Record(n.Warfare,a.Id,a.Attacker,a.Gang,a.Area.Id,"dismissed");n.Warfare.Alert=null;}}
            else if(feature=="rivals")n.Warfare.RivalsEnabled=enabled;else if(feature=="reinforcements")n.Warfare.Reinforcements=enabled;else return false;return true;},out result);
        public static bool Alert(GangState s,GangWarAlert alert,Action<GangState> save,out GangState result)=>Apply(s,save,n=>{
            if(alert?.Valid!=true || alert.Elapsed!=0 || !n.Warfare.AttacksEnabled || n.Warfare.Alert!=null || !GangWarRules.Free(n) || !GangCrewRules.SameGang(n.Gang,alert.Gang) || !GangCrewRules.SameGang(GangTurfRules.Owner(n.Turf,alert.Area),n.Gang) || n.Warfare.History.Any(r=>r.Id==alert.Id))return false;
            n.Warfare.Alert=JsonConvert.DeserializeObject<GangWarAlert>(JsonConvert.SerializeObject(alert));return true;},out result);
        public static bool Advance(GangState s,string id,int elapsed,Action<GangState> save,out GangState result)=>Apply(s,save,n=>{
            var a=n.Warfare.Alert;if(a==null || a.Id!=id || !n.Warfare.AttacksEnabled || !GangWarRules.Free(n) || !GangCrewRules.SameGang(n.Gang,a.Gang) || !GangCrewRules.SameGang(GangTurfRules.Owner(n.Turf,a.Area),a.Gang) || elapsed<a.Elapsed || elapsed>a.Limit)return false;
            a.Elapsed=elapsed;if(elapsed==a.Limit){if(n.Warfare.Unanswered==long.MaxValue)return false;n.Turf=n.Turf??new GangTurfData();n.Turf.Owners[a.Area.Id]=a.Attacker;n.Warfare.Unanswered++;GangWarRules.Record(n.Warfare,a.Id,a.Attacker,a.Gang,a.Area.Id,"unanswered");n.Warfare.Alert=null;}return true;},out result);
        // Called by Begin inside its single GangState save, never a separate checkpoint.
        internal static bool Respond(GangState s,GangTurfAttempt p)
        {
            var a=s.Warfare?.Alert;if(a==null || s.Warfare.Valid!=true || !s.Warfare.AttacksEnabled || a.Id!=p.WarAlertId || a.Elapsed>=a.Limit || p.Mode!=GangTurfMode.Defense || a.Area.Id!=p.Area.Id || !GangCrewRules.SameGang(a.Gang,p.Gang) || !GangCrewRules.SameGang(a.Attacker,p.Attacker) || JsonConvert.SerializeObject(a.Area)!=JsonConvert.SerializeObject(p.Area) || s.Warfare.Responded==long.MaxValue)return false;
            s.Warfare.Responded++;GangWarRules.Record(s.Warfare,a.Id,a.Attacker,a.Gang,a.Area.Id,"responded");s.Warfare.Alert=null;return true;
        }
        public static bool Rival(GangState s,GangTurfDefinition area,string attacker,string defender,bool won,string id,Action<GangState> save,out GangState result)=>Apply(s,save,n=>{
            if(!n.Warfare.RivalsEnabled || n.Warfare.Alert!=null || !GangWarRules.Free(n) || area?.Valid!=true || !GangDiplomacyRules.Pair(defender,attacker) || GangCrewRules.SameGang(n.Gang,attacker) || GangCrewRules.SameGang(n.Gang,defender) || !GangCrewRules.SameGang(GangTurfRules.Owner(n.Turf,area),defender) || n.Warfare.RivalBattles==long.MaxValue || string.IsNullOrWhiteSpace(id) || id.Length>80 || n.Warfare.History.Any(r=>r.Id==id))return false;
            n.Turf=n.Turf??new GangTurfData();if(won)n.Turf.Owners[area.Id]=attacker;n.Warfare.RivalBattles++;GangWarRules.Record(n.Warfare,id,attacker,defender,area.Id,won?"rival_capture":"rival_defense");return true;},out result);
    }
}
