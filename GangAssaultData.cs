using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangAssaultOrigin : ExtensibleData
    {
        public int Area;public List<GangSpot> Spawns=new List<GangSpot>();
        [JsonIgnore] public bool Valid=>Area>0 && Spawns!=null && Spawns.Count==4 && Spawns.All(GangPresenceRules.Position) && Spawns.Select(p=>p.X+"|"+p.Y+"|"+p.Z).Distinct().Count()==4;
    }
    public sealed class GangAssaultOptions : ExtensibleData
    {
        public string VehicleModel="granger";public int TravelSeconds=600,ArrivalRadius=65,EscortRadius=100;
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public List<GangAssaultOrigin> Origins=new List<GangAssaultOrigin>();
        [JsonIgnore] public bool Valid=>GangPresenceRules.ModelName(VehicleModel) && TravelSeconds>=120 && TravelSeconds<=1800 && ArrivalRadius>=45 && ArrivalRadius<=100 && EscortRadius>=ArrivalRadius && EscortRadius<=200 && Origins!=null && Origins.Count>=1 && Origins.Count<=100 && Origins.All(o=>o?.Valid==true) && Origins.Select(o=>o.Area).Distinct().Count()==Origins.Count;
        public static GangAssaultOptions Defaults(){using(var s=typeof(GangAssaultOptions).Assembly.GetManifestResourceStream("RPCore.JobDefaults.GangAssault.json"))using(var r=new System.IO.StreamReader(s))return JsonConvert.DeserializeObject<GangAssaultOptions>(r.ReadToEnd());}
    }
    public sealed class GangAssaultProgress : ExtensibleData
    {
        public int TravelElapsed,BattleElapsed;public bool Arrived;public List<int> LostCrew=new List<int>(),LostCars=new List<int>();
        [JsonIgnore] public bool Valid=>TravelElapsed>=0 && TravelElapsed<=1800000 && BattleElapsed>=0 && BattleElapsed<=1800000 && (Arrived || BattleElapsed==0) && ValidIndices(LostCrew,15) && ValidIndices(LostCars,4);
        static bool ValidIndices(List<int> xs,int max)=>xs!=null && xs.Count<=max && xs.All(i=>i>=0 && i<max) && xs.Distinct().Count()==xs.Count;
    }
    public sealed class GangAssaultPlan : ExtensibleData
    {
        public GangTurfDefinition Origin;public GangAssaultOptions Options;public GangAssaultOrigin Bay;public string[] CrewModels;public GangAssaultProgress Progress=new GangAssaultProgress();
        [JsonIgnore] public bool Valid=>Origin?.Valid==true && Options?.Valid==true && Bay?.Valid==true && Bay.Area==Origin.Id && Options.Origins.Any(o=>o.Area==Bay.Area) && CrewModels!=null && CrewModels.Length==15 && CrewModels.All(GangPresenceRules.ModelName) && Progress?.Valid==true && Progress.TravelElapsed<=Options.TravelSeconds*1000;
    }
    public static class GangAssaultRules
    {
        public static int Car(int crew)=>crew<0 || crew>=15?-1:crew<3?0:1+(crew-3)/4;
        // Seats: player drives car0; three passengers. Each escort has driver+three passengers.
        public static int Seat(int crew)=>crew<0 || crew>=15?-99:crew<3?crew:(crew-3)%4-1;
        public static bool Driver(int crew)=>crew==3 || crew==7 || crew==11;
        public static bool Fresh(GangAssaultPlan p)=>p?.Valid==true && !p.Progress.Arrived && p.Progress.TravelElapsed==0 && p.Progress.BattleElapsed==0 && p.Progress.LostCrew.Count==0 && p.Progress.LostCars.Count==0;
        public static bool Arrive(GangTurfAttempt p,bool ownLeadDriver,GangSpot lead,float speed,bool escortDriverNearby)=>p?.Assault?.Valid==true && !p.Assault.Progress.Arrived && ownLeadDriver && escortDriverNearby && GangPresenceRules.Position(lead) && GangPresenceRules.Finite(speed) && speed>=0 && speed<1.5f && GangPresenceRules.Distance(lead,p.Area.Center)<=p.Assault.Options.ArrivalRadius && Math.Abs(lead.Z-p.Area.Center.Z)<=8 && !p.Assault.Progress.LostCars.Contains(0) && !NoEscort(p.Assault.Progress) && p.Assault.Progress.TravelElapsed<p.Assault.Options.TravelSeconds*1000;
        public static bool NoEscort(GangAssaultProgress p)=>p!=null && new[]{3,7,11}.All(i=>p.LostCrew.Contains(i) || p.LostCars.Contains(Car(i)));
        public static bool Advance(GangAssaultProgress old,GangAssaultProgress next)=>old?.Valid==true && next?.Valid==true && old.Arrived==next.Arrived && next.TravelElapsed>=old.TravelElapsed && next.BattleElapsed>=old.BattleElapsed && (!old.Arrived?next.BattleElapsed==0:next.TravelElapsed==old.TravelElapsed) && old.LostCrew.All(next.LostCrew.Contains) && old.LostCars.All(next.LostCars.Contains);
    }
    public static class GangAssaultTransactions
    {
        static bool Apply(GangState s,string id,Action<GangState> save,Func<GangTurfAttempt,bool> change,out GangState result)
        {
            result=s;var p=s?.Turf?.Pending;if(s?.Turf?.Valid!=true || p?.Id!=id || p.Assault?.Valid!=true || p.Ready || save==null || !GangCrewRules.SameGang(s.Gang,p.Gang) || !GangCrewRules.SameGang(GangTurfRules.Owner(s.Turf,p.Area),p.PreviousOwner))return false;
            var n=JsonConvert.DeserializeObject<GangState>(JsonConvert.SerializeObject(s));if(!change(n.Turf.Pending) || !n.Turf.Valid)return false;save(n);result=n;return true;
        }
        public static bool Sample(GangState s,string id,GangAssaultProgress progress,Action<GangState> save,out GangState result)=>Apply(s,id,save,p=>{if(!GangAssaultRules.Advance(p.Assault.Progress,progress) || progress.BattleElapsed>p.SecondsLimit*1000 || progress.TravelElapsed>p.Assault.Options.TravelSeconds*1000)return false;p.Assault.Progress=JsonConvert.DeserializeObject<GangAssaultProgress>(JsonConvert.SerializeObject(progress));return true;},out result);
        public static bool Arrive(GangState s,string id,bool ownLeadDriver,GangSpot lead,float speed,bool escortDriverNearby,Action<GangState> save,out GangState result)=>Apply(s,id,save,p=>{if(!GangAssaultRules.Arrive(p,ownLeadDriver,lead,speed,escortDriverNearby))return false;p.Assault.Progress.Arrived=true;return true;},out result);
    }
}
