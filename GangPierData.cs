using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangPierOptions : ExtensibleData
    {
        public int BoostMinutes=60,StartRadius=8,SecondsLimit=300,TaskMilliseconds=3000;public string WaterModel="prop_wateringcan";
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public List<JobPoint> Points=new List<JobPoint>{
            new JobPoint{Name="Reposicao 1",X=-1637.986f,Y=-1093.15f,Z=13.0274f,Heading=308.62f},
            new JobPoint{Name="Reposicao 2",X=-1679.894f,Y=-1111.392f,Z=13.15217f,Heading=280.1763f},
            new JobPoint{Name="Rega 3",X=-1673.646f,Y=-1104.703f,Z=13.15219f,Heading=247.9179f},
            new JobPoint{Name="Rega 4",X=-1675.787f,Y=-1107.587f,Z=13.15217f,Heading=217.7727f},
            new JobPoint{Name="Rega 5",X=-1677.606f,Y=-1104.132f,Z=13.1522f,Heading=111.9972f},
            new JobPoint{Name="Rega 6",X=-1681.477f,Y=-1100.813f,Z=13.15234f,Heading=137.6671f},
            new JobPoint{Name="Rega 7",X=-1685.44f,Y=-1097.102f,Z=13.15276f,Heading=155.5525f},
            new JobPoint{Name="Rega 8",X=-1689.478f,Y=-1093.397f,Z=13.15322f,Heading=140.8481f},
            new JobPoint{Name="Rega 9",X=-1692.891f,Y=-1089.6f,Z=13.15311f,Heading=147.5786f},
            new JobPoint{Name="Rega 10",X=-1698.596f,Y=-1084.718f,Z=13.15249f,Heading=145.3136f},
            new JobPoint{Name="Rega 11",X=-1705.739f,Y=-1093.047f,Z=13.1524f,Heading=146.9452f},
            new JobPoint{Name="Rega 12",X=-1699.719f,Y=-1098.387f,Z=13.15232f,Heading=118.5997f},
            new JobPoint{Name="Rega 13",X=-1696.068f,Y=-1101.364f,Z=13.1523f,Heading=158.5522f},
            new JobPoint{Name="Rega 14",X=-1691.963f,Y=-1104.18f,Z=13.1523f,Heading=158.8917f},
            new JobPoint{Name="Rega 15",X=-1687.307f,Y=-1107.357f,Z=13.15227f,Heading=139.8329f},
            new JobPoint{Name="Rega 16",X=-1683.039f,Y=-1110.641f,Z=13.15226f,Heading=155.8277f},
            new JobPoint{Name="Rega 17",X=-1679.263f,Y=-1114.181f,Z=13.15215f,Heading=128.1939f},
            new JobPoint{Name="Rega 18",X=-1681.876f,Y=-1044.293f,Z=13.1345f,Heading=56.52179f},
            new JobPoint{Name="Rega 19",X=-1674.349f,Y=-1035.357f,Z=13.10045f,Heading=56.9208f},
            new JobPoint{Name="Rega 20",X=-1667.229f,Y=-1026.127f,Z=13.03714f,Heading=57.7917f},
            new JobPoint{Name="Rega 21",X=-1659.828f,Y=-1017.255f,Z=13.0174f,Heading=54.10996f}};
        [JsonIgnore] public bool Valid=>BoostMinutes>=1 && BoostMinutes<=180 && StartRadius>=3 && StartRadius<=15 && SecondsLimit>=120 && SecondsLimit<=900 && TaskMilliseconds>=1000 && TaskMilliseconds<=5000 && GangPresenceRules.ModelName(WaterModel) && Points!=null && Points.Count>=3 && Points.Count<=50 && Points.All(p=>p?.Valid==true && DeliveryRoute.Distance(Points[0],p)<=200) && Points.Select(p=>p.X+"|"+p.Y+"|"+p.Z).Distinct().Count()==Points.Count;
    }
    public sealed class GangPierProgress : ExtensibleData
    {
        public int Elapsed;public bool Faulted;
        [JsonIgnore] public bool Valid=>Elapsed>=0 && Elapsed<=900000;
    }
    public sealed class GangPierAttempt : ExtensibleData
    {
        public string Id,Gang;public GangTurfDefinition Area;public GangPierOptions Options;public GangPierProgress Progress=new GangPierProgress();public int Done;
        [JsonIgnore] public bool Failed=>Progress?.Valid!=true || Options?.Valid!=true || Progress.Faulted || Progress.Elapsed>=Options.SecondsLimit*1000;
        [JsonIgnore] public bool Ready=>Options?.Valid==true && Done==Options.Points.Count && !Failed;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && !GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area?.Valid==true && Area.Id==14 && Options?.Valid==true && Progress?.Valid==true && Done>=0 && Done<=Options.Points.Count && (Done<Options.Points.Count || !Failed);
    }
    public sealed class GangPierAccount : ExtensibleData
    {
        public string Gang;public int Area=14;public long Completed,StartedUtcTicks,ExpiresUtcTicks;
        [JsonIgnore] public bool Valid=>!GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area==14 && Completed>0 && StartedUtcTicks>0 && ExpiresUtcTicks<=DateTime.MaxValue.Ticks && ExpiresUtcTicks>StartedUtcTicks && ExpiresUtcTicks-StartedUtcTicks<=TimeSpan.FromMinutes(180).Ticks;
    }
    public sealed class GangPierData : ExtensibleData
    {
        public GangPierAttempt Pending;public List<GangPierAccount> Accounts=new List<GangPierAccount>();public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>(Pending==null || Pending.Valid) && Accounts!=null && Accounts.Count<=640 && Accounts.All(a=>a?.Valid==true) && Accounts.Select(a=>a.Gang).Distinct(StringComparer.OrdinalIgnoreCase).Count()==Accounts.Count && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=120) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public static class GangPierRules
    {
        public static bool Near(bool safe,bool foot,float distance)=>safe && foot && GangPresenceRules.Finite(distance) && distance>=0 && distance<=2;
    }
    public sealed class GangPierTask
    {
        int last;public int Milliseconds{get;private set;}
        public GangPierTask(int now){last=now;}
        public bool Step(int now,bool valid,int duration){uint delta=unchecked((uint)(now-last));last=now;if(!valid || delta>2000 || duration<1000 || duration>5000){Milliseconds=0;return false;}Milliseconds=(int)Math.Min(duration,(long)Milliseconds+delta);return Milliseconds>=duration;}
    }
    public sealed class GangPierClock
    {
        int last;readonly int limit;public GangPierProgress Progress{get;}
        public GangPierClock(int now,int limit,GangPierProgress p){last=now;this.limit=limit;Progress=JsonConvert.DeserializeObject<GangPierProgress>(JsonConvert.SerializeObject(p));}
        public void Tick(int now,bool working){uint delta=unchecked((uint)(now-last));last=now;if(!working || delta>2000)return;Progress.Elapsed=(int)Math.Min(limit,(long)Progress.Elapsed+delta);}
    }
    public static class GangPierTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));static bool Id(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=80;
        public static bool Blocked(WalletData w)=>w?.GangPier!=null && (!w.GangPier.Valid || w.GangPier.Pending!=null);
        public static bool OtherFree(WalletData w)=>w!=null && !GangOffshoreTransactions.Blocked(w) && !GangBmxTransactions.Blocked(w) && !GangDockTransactions.Blocked(w) && !GangChauffeurTransactions.Blocked(w) && !GangTheatreTransactions.Blocked(w) && !GangMowingTransactions.Blocked(w) && !GangPopUpTransactions.Blocked(w) && !GangMedicalTransactions.Blocked(w) && !GangCashRunTransactions.Blocked(w) && !GangTowTransactions.Blocked(w) && !GangMechanicTransactions.Blocked(w) && !GangPromotionTransactions.Blocked(w) && (w.GangBulk==null || w.GangBulk.Valid && w.GangBulk.Pending==null) && (w.GangRaid==null || w.GangRaid.Valid && w.GangRaid.Pending==null) && (w.GangBusiness==null || w.GangBusiness.Valid && w.GangBusiness.Supply==null);
        public static GangPierAccount Account(GangPierData b,string gang)=>b?.Valid==true?b.Accounts.FirstOrDefault(a=>GangCrewRules.SameGang(a.Gang,gang)):null;
        public static int Multiplier(GangPierData b,string gang,int area,DateTime utc){var a=Account(b,gang);return area==14 && utc.Kind==DateTimeKind.Utc && a!=null && utc.Ticks>=a.StartedUtcTicks && utc.Ticks<a.ExpiresUtcTicks?2:1;}
        static bool Apply(WalletData w,string receipt,Func<GangPierData,bool> mutate,Action save,out bool changed)
        {changed=false;if(w==null || w.Bank<0 || save==null || w.GangPier!=null && !w.GangPier.Valid)return false;var old=w.GangPier;if(old?.Receipts.Contains(receipt)==true)return true;var b=old==null?new GangPierData():Copy(old);if(!mutate(b) || !b.Valid)return false;b.Receipts.Add(receipt);if(b.Receipts.Count>256)b.Receipts.RemoveAt(0);try{w.GangPier=b;save();changed=true;return true;}catch{w.GangPier=old;throw;}}
        static bool Owns(GangState s,GangTurfDefinition area,GangPierAttempt p)=>p!=null && area?.Id==14 && GangBusinessTransactions.Owns(s,area) && GangCrewRules.SameGang(s.Gang,p.Gang);
        public static bool Begin(WalletData w,GangState s,GangTurfDefinition area,GangPierOptions c,string id,Action save,out bool changed)
        {changed=false;if(!Id(id) || area?.Id!=14 || !GangBusinessTransactions.Owns(s,area) || c?.Valid!=true || !OtherFree(w))return false;return Apply(w,id+":pier-begin",b=>{if(b.Pending!=null)return false;var a=Account(b,s.Gang);if(a?.Completed==long.MaxValue || a==null && b.Accounts.Count>=640)return false;b.Pending=new GangPierAttempt{Id=id,Gang=s.Gang,Area=Copy(area),Options=Copy(c)};return true;},save,out changed);}
        public static bool Update(WalletData w,GangState s,GangTurfDefinition area,string id,GangPierProgress progress,Action save,out bool changed)
        {changed=false;var p=w?.GangPier?.Pending;if(!Id(id) || p?.Id!=id || w.GangPier.Valid!=true || p.Ready || !Owns(s,area,p) || !OtherFree(w) || progress?.Valid!=true || progress.Elapsed<p.Progress.Elapsed || progress.Elapsed>p.Options.SecondsLimit*1000 || p.Progress.Faulted && !progress.Faulted)return false;return Apply(w,Guid.NewGuid().ToString("N")+":pier-sample",b=>{b.Pending.Progress.Elapsed=progress.Elapsed;b.Pending.Progress.Faulted=progress.Faulted;return true;},save,out changed);}
        public static bool Work(WalletData w,GangState s,GangTurfDefinition area,string id,int index,bool safe,bool foot,float distance,int milliseconds,Action save,out bool changed)
        {changed=false;var p=w?.GangPier?.Pending;if(!Id(id) || p?.Id!=id || w.GangPier.Valid!=true || index<0 || index>=p.Options.Points.Count || !Owns(s,area,p) || !OtherFree(w) || !GangPierRules.Near(safe,foot,distance) || milliseconds<p.Options.TaskMilliseconds || milliseconds>5000)return false;return Apply(w,id+":pier-task:"+index,b=>{if(b.Pending.Failed || b.Pending.Done!=index)return false;b.Pending.Done++;return true;},save,out changed);}
        public static bool Cancel(WalletData w,string id,Action save,out bool changed){changed=false;if(!Id(id))return false;return Apply(w,id+":pier-cancel",b=>{if(b.Pending?.Id!=id)return false;b.Pending=null;return true;},save,out changed);}
        public static bool Complete(WalletData w,GangState s,GangTurfDefinition area,string id,DateTime utc,Action save,out bool changed)
        {changed=false;if(!Id(id) || area?.Id!=14 || !GangBusinessTransactions.Owns(s,area) || utc.Kind!=DateTimeKind.Utc || utc.Ticks==0 || !OtherFree(w))return false;return Apply(w,id+":pier-complete",b=>{var p=b.Pending;if(p?.Id!=id || !p.Ready || !Owns(s,area,p) || utc.Ticks>DateTime.MaxValue.Ticks-TimeSpan.FromMinutes(p.Options.BoostMinutes).Ticks)return false;var a=Account(b,p.Gang);if(a==null){if(b.Accounts.Count>=640)return false;a=new GangPierAccount{Gang=p.Gang};b.Accounts.Add(a);}if(a.Completed==long.MaxValue)return false;a.Completed++;a.StartedUtcTicks=utc.Ticks;a.ExpiresUtcTicks=utc.AddMinutes(p.Options.BoostMinutes).Ticks;b.Pending=null;return true;},save,out changed);}
    }
}
