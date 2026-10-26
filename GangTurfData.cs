using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public enum GangTurfMode { Attack,Defense }
    public sealed class GangTurfDefinition : ExtensibleData
    {
        public int Id;public string Name,DefaultOwner;public GangSpot Center;public List<GangSpot> Posts=new List<GangSpot>();
        [JsonIgnore] public bool Valid=>Id>0 && !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(DefaultOwner) && GangPresenceRules.Position(Center) && Posts!=null && Posts.Count>=1 && Posts.Count<=100 && Posts.All(GangPresenceRules.Position);
    }
    public sealed class GangTurfOptions : ExtensibleData
    {
        public int Defenders=3,Radius=45,SecondsLimit=600;public List<GangTurfDefinition> Areas=new List<GangTurfDefinition>();
        [JsonIgnore] public bool Valid=>Defenders>=1 && Defenders<=6 && Radius>=20 && Radius<=100 && SecondsLimit>=60 && SecondsLimit<=1800 && Areas!=null && Areas.Count>=1 && Areas.Count<=100 && Areas.All(a=>a!=null && a.Valid) && Areas.Select(a=>a.Id).Distinct().Count()==Areas.Count;
        public static GangTurfOptions Defaults(){using(var s=typeof(GangTurfOptions).Assembly.GetManifestResourceStream("RPCore.JobDefaults.GangTerritories.json"))using(var r=new System.IO.StreamReader(s))return JsonConvert.DeserializeObject<GangTurfOptions>(r.ReadToEnd());}
    }
    public sealed class GangTurfAttempt : ExtensibleData
    {
        public string Id,Gang,PreviousOwner,Attacker,WarAlertId;public int InitialDefenders;public GangTurfMode Mode;public int WaveSize;public GangTurfDefinition Area;public int Radius,SecondsLimit;public string[] Models;public List<int> Defeated=new List<int>();public bool Ready;
        [JsonIgnore] public bool Valid=>Enum.IsDefined(typeof(GangTurfMode),Mode) && (WarAlertId==null || WarAlertId.Length>=1 && WarAlertId.Length<=80 && Mode==GangTurfMode.Defense) && (InitialDefenders==0 || Mode==GangTurfMode.Attack && InitialDefenders>=1 && InitialDefenders<=3 && Models!=null && Models.Length>InitialDefenders && Models.Length<=6) && (Mode==GangTurfMode.Attack || !string.IsNullOrWhiteSpace(Attacker) && Attacker.Length<=100 && !GangCrewRules.SameGang(Attacker,Gang) && WaveSize>=1 && WaveSize<=3 && Models!=null && Models.Length%WaveSize==0 && Models.Length/WaveSize<=2) && !string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && !string.IsNullOrWhiteSpace(Gang) && Gang.Length<=100 && !string.IsNullOrWhiteSpace(PreviousOwner) && Area!=null && Area.Valid && Radius>=20 && Radius<=100 && SecondsLimit>=60 && SecondsLimit<=1800 && Models!=null && Models.Length>=1 && Models.Length<=6 && Models.All(GangPresenceRules.ModelName) && Defeated!=null && Defeated.Count<=Models.Length && Defeated.All(i=>i>=0 && i<Models.Length) && Defeated.Distinct().Count()==Defeated.Count && Ready==(Defeated.Count==Models.Length);
    }
    public sealed class GangTurfData : ExtensibleData
    {
        public Dictionary<int,string> Owners=new Dictionary<int,string>();public GangTurfAttempt Pending;public int Attempts,Captures,Defenses,LostDefenses;public string LastResult;public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>Attempts>=0 && Captures>=0 && (long)Captures+Defenses+LostDefenses<=Attempts && Defenses>=0 && LostDefenses>=0 && Owners!=null && Owners.Count<=1000 && Owners.All(p=>p.Key>0 && !string.IsNullOrWhiteSpace(p.Value) && p.Value.Length<=100) && (Pending==null || Pending.Valid && Attempts>(long)Captures+Defenses+LostDefenses) && Receipts!=null && Receipts.Count<=128 && Receipts.All(s=>!string.IsNullOrWhiteSpace(s) && s.Length<=110) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public static class GangTurfRules
    {
        public static string Owner(GangTurfData book,GangTurfDefinition area)=>book?.Owners!=null && book.Owners.TryGetValue(area.Id,out var owner)?owner:area.DefaultOwner;
        public static bool Inside(float distance,int radius)=>GangPresenceRules.Finite(distance) && distance>=0 && radius>=20 && radius<=100 && distance<=radius;
    }
    public static class GangTurfTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        static bool Apply(GangState state,string receipt,Func<GangState,bool> change,Action<GangState> save,out GangState result)
        {
            result=state;if(state==null || save==null || string.IsNullOrWhiteSpace(receipt) || receipt.Length>110 || state.Turf!=null && !state.Turf.Valid)return false;if(state.Turf?.Receipts.Contains(receipt)==true)return true;
            var next=Copy(state);next.Turf=next.Turf??new GangTurfData();if(!change(next) || !next.Turf.Valid)return false;next.Turf.Receipts.Add(receipt);if(next.Turf.Receipts.Count>128)next.Turf.Receipts.RemoveAt(0);save(next);result=next;return true;
        }
        public static bool Begin(GangState state,GangTurfAttempt p,Action<GangState> save,out GangState result)
        {result=state;if(p==null || !p.Valid || p.Ready || p.Defeated.Count!=0)return false;return Apply(state,p.Id+":begin",s=>{if(!GangCrewRules.SameGang(s.Gang,p.Gang) || GangLoyaltyRules.Unaffiliated(s.Gang) || s.Turf.Pending!=null || s.Loyalty?.Pending!=null || s.Strength?.Pending!=null || s.Driving?.Pending!=null || s.Turf.Attempts==int.MaxValue || (p.Mode==GangTurfMode.Attack?GangCrewRules.SameGang(p.PreviousOwner,p.Gang):!GangCrewRules.SameGang(p.PreviousOwner,p.Gang)) || !GangCrewRules.SameGang(GangTurfRules.Owner(s.Turf,p.Area),p.PreviousOwner))return false;if(p.WarAlertId!=null && !GangWarTransactions.Respond(s,p))return false;s.Turf.Pending=Copy(p);s.Turf.Attempts++;return true;},save,out result);}
        public static bool Defeat(GangState state,string id,int index,Action<GangState> save,out GangState result)=>Apply(state,Guid.NewGuid().ToString("N")+":defeat",s=>{var p=s.Turf.Pending;if(p==null || p.Id!=id || p.Ready || !GangCrewRules.SameGang(s.Gang,p.Gang) || index<0 || index>=p.Models.Length || p.Defeated.Contains(index))return false;p.Defeated.Add(index);p.Ready=p.Defeated.Count==p.Models.Length;return true;},save,out result);
        public static bool Restart(GangState state,string id,Action<GangState> save,out GangState result)=>Apply(state,Guid.NewGuid().ToString("N")+":restart",s=>{var p=s.Turf.Pending;if(p==null || p.Id!=id || p.Ready || !GangCrewRules.SameGang(s.Gang,p.Gang) || !GangCrewRules.SameGang(GangTurfRules.Owner(s.Turf,p.Area),p.PreviousOwner))return false;p.Defeated.Clear();return true;},save,out result);
        public static bool Capture(GangState state,string id,Action<GangState> save,out GangState result)=>Apply(state,id+":capture",s=>{var p=s.Turf.Pending;if(p==null || p.Id!=id || !p.Ready || !GangCrewRules.SameGang(s.Gang,p.Gang) || !GangCrewRules.SameGang(GangTurfRules.Owner(s.Turf,p.Area),p.PreviousOwner))return false;
            if(p.Mode==GangTurfMode.Defense){if(s.Turf.Defenses==int.MaxValue || !GangCrewRules.SameGang(p.PreviousOwner,p.Gang))return false;s.Turf.Owners[p.Area.Id]=p.Gang;s.Turf.Defenses++;if(!GangProgressionRules.Grant(s,25))return false;s.Turf.LastResult="defended";}
            else{if(s.Turf.Captures==int.MaxValue)return false;s.Turf.Owners[p.Area.Id]=p.Gang;s.Turf.Captures++;if(!GangProgressionRules.Grant(s,50))return false;s.Turf.LastResult="captured";}s.Turf.Pending=null;return true;},save,out result);
        public static bool End(GangState state,string id,bool failed,Action<GangState> save,out GangState result)=>Apply(state,id+":end",s=>{var p=s.Turf.Pending;if(p==null || p.Id!=id)return false;
            if(failed && p.Mode==GangTurfMode.Defense){if(p.Ready || !GangCrewRules.SameGang(s.Gang,p.Gang) || !GangCrewRules.SameGang(GangTurfRules.Owner(s.Turf,p.Area),p.PreviousOwner) || s.Turf.LostDefenses==int.MaxValue)return false;s.Turf.Owners[p.Area.Id]=p.Attacker;s.Turf.LostDefenses++;if(!GangProgressionRules.Grant(s,-20))return false;s.Turf.LastResult="defense_lost";}
            else s.Turf.LastResult=failed?"failed":"cancelled";s.Turf.Pending=null;return true;},save,out result);
    }
}
