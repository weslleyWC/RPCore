using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangBmxOptions : ExtensibleData
    {
        public int BoostMinutes=60,StartRadius=8,SecondsLimit=75;public string VehicleModel="bmx";public float Radius=2.5f,Height=3;
        public JobPoint Spawn=new JobPoint{Name="BMX skatepark",X=-1375.902f,Y=-1407.651f,Z=5.033009f,Heading=352.8152f};
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public List<JobPoint> Checkpoints=new List<JobPoint>{
            new JobPoint{Name="Checkpoint 1",X=-1375.255f,Y=-1405.455f,Z=5.033881f},
            new JobPoint{Name="Checkpoint 2",X=-1373.548f,Y=-1390.427f,Z=5.033914f},
            new JobPoint{Name="Checkpoint 3",X=-1370.229f,Y=-1377.641f,Z=6.193571f},
            new JobPoint{Name="Checkpoint 4",X=-1357.752f,Y=-1380.219f,Z=6.007689f},
            new JobPoint{Name="Checkpoint 5",X=-1367.616f,Y=-1412.108f,Z=5.663543f},
            new JobPoint{Name="Checkpoint 6",X=-1368.318f,Y=-1401.326f,Z=4.904123f},
            new JobPoint{Name="Checkpoint 7",X=-1367.027f,Y=-1393.302f,Z=4.902225f},
            new JobPoint{Name="Checkpoint 8",X=-1378.457f,Y=-1383.765f,Z=4.494459f},
            new JobPoint{Name="Checkpoint 9",X=-1391.214f,Y=-1383.377f,Z=3.79951f},
            new JobPoint{Name="Checkpoint 10",X=-1380.912f,Y=-1397.67f,Z=5.564175f},
            new JobPoint{Name="Checkpoint 11",X=-1375.862f,Y=-1408.597f,Z=5.6437f}};
        [JsonIgnore] public bool Valid=>BoostMinutes>=1 && BoostMinutes<=180 && StartRadius>=3 && StartRadius<=15 && SecondsLimit>=30 && SecondsLimit<=300 && GangPresenceRules.ModelName(VehicleModel) && GangPresenceRules.Finite(Radius) && Radius>=1 && Radius<=4 && GangPresenceRules.Finite(Height) && Height>=1 && Height<=4 && Spawn?.Valid==true && Checkpoints!=null && Checkpoints.Count>=1 && Checkpoints.Count<=50 && Checkpoints.All(p=>p?.Valid==true && DeliveryRoute.Distance(Spawn,p)<=150) && Checkpoints.Select(p=>p.X+"|"+p.Y+"|"+p.Z).Distinct().Count()==Checkpoints.Count;
    }
    public sealed class GangBmxProgress : ExtensibleData
    {
        public int Elapsed;public bool Faulted;
        [JsonIgnore] public bool Valid=>Elapsed>=0 && Elapsed<=300000;
    }
    public sealed class GangBmxAttempt : ExtensibleData
    {
        public string Id,Gang;public GangTurfDefinition Area;public GangBmxOptions Options;public GangBmxProgress Progress=new GangBmxProgress();public int Cleared;
        [JsonIgnore] public bool Failed=>Progress?.Valid!=true || Options?.Valid!=true || Progress.Faulted || Progress.Elapsed>=Options.SecondsLimit*1000;
        [JsonIgnore] public bool Ready=>Options?.Valid==true && Cleared==Options.Checkpoints.Count && !Failed;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && !GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area?.Valid==true && Area.Id==12 && Options?.Valid==true && Progress?.Valid==true && Cleared>=0 && Cleared<=Options.Checkpoints.Count && (Cleared<Options.Checkpoints.Count || !Failed);
    }
    public sealed class GangBmxAccount : ExtensibleData
    {
        public string Gang;public int Area=12;public long Completed,StartedUtcTicks,ExpiresUtcTicks;
        [JsonIgnore] public bool Valid=>!GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area==12 && Completed>0 && StartedUtcTicks>0 && ExpiresUtcTicks<=DateTime.MaxValue.Ticks && ExpiresUtcTicks>StartedUtcTicks && ExpiresUtcTicks-StartedUtcTicks<=TimeSpan.FromMinutes(180).Ticks;
    }
    public sealed class GangBmxData : ExtensibleData
    {
        public GangBmxAttempt Pending;public List<GangBmxAccount> Accounts=new List<GangBmxAccount>();public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>(Pending==null || Pending.Valid) && Accounts!=null && Accounts.Count<=640 && Accounts.All(a=>a?.Valid==true) && Accounts.Select(a=>a.Gang).Distinct(StringComparer.OrdinalIgnoreCase).Count()==Accounts.Count && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=120) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public static class GangBmxRules
    {
        public static bool Pass(bool safe,bool ownRider,JobPoint position,JobPoint checkpoint,GangBmxOptions c)
        {if(!safe || !ownRider || c?.Valid!=true || position?.Valid!=true || checkpoint?.Valid!=true)return false;double x=(double)position.X-checkpoint.X,y=(double)position.Y-checkpoint.Y;return x*x+y*y<=c.Radius*c.Radius && Math.Abs(position.Z-checkpoint.Z)<=c.Height;}
    }
    public sealed class GangBmxClock
    {
        int last;readonly int limit;public GangBmxProgress Progress{get;}
        public GangBmxClock(int now,int limit,GangBmxProgress p){last=now;this.limit=limit;Progress=JsonConvert.DeserializeObject<GangBmxProgress>(JsonConvert.SerializeObject(p));}
        public void Tick(int now,bool working){uint delta=unchecked((uint)(now-last));last=now;if(!working || delta>2000)return;Progress.Elapsed=(int)Math.Min(limit,(long)Progress.Elapsed+delta);}
    }
    public static class GangBmxTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));static bool Id(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=80;
        public static bool Blocked(WalletData w)=>w?.GangBmx!=null && (!w.GangBmx.Valid || w.GangBmx.Pending!=null);
        public static bool OtherFree(WalletData w)=>w!=null && !GangCashierTransactions.Blocked(w) && !GangPierTransactions.Blocked(w) && !GangOffshoreTransactions.Blocked(w) && !GangDockTransactions.Blocked(w) && !GangChauffeurTransactions.Blocked(w) && !GangTheatreTransactions.Blocked(w) && !GangMowingTransactions.Blocked(w) && !GangPopUpTransactions.Blocked(w) && !GangMedicalTransactions.Blocked(w) && !GangCashRunTransactions.Blocked(w) && !GangTowTransactions.Blocked(w) && !GangMechanicTransactions.Blocked(w) && !GangPromotionTransactions.Blocked(w) && (w.GangBulk==null || w.GangBulk.Valid && w.GangBulk.Pending==null) && (w.GangRaid==null || w.GangRaid.Valid && w.GangRaid.Pending==null) && (w.GangBusiness==null || w.GangBusiness.Valid && w.GangBusiness.Supply==null);
        public static GangBmxAccount Account(GangBmxData b,string gang)=>b?.Valid==true?b.Accounts.FirstOrDefault(a=>GangCrewRules.SameGang(a.Gang,gang)):null;
        public static int Multiplier(GangBmxData b,string gang,int area,DateTime utc){var a=Account(b,gang);return area==12 && utc.Kind==DateTimeKind.Utc && a!=null && utc.Ticks>=a.StartedUtcTicks && utc.Ticks<a.ExpiresUtcTicks?2:1;}
        static bool Apply(WalletData w,string receipt,Func<GangBmxData,bool> mutate,Action save,out bool changed)
        {changed=false;if(w==null || w.Bank<0 || save==null || w.GangBmx!=null && !w.GangBmx.Valid)return false;var old=w.GangBmx;if(old?.Receipts.Contains(receipt)==true)return true;var b=old==null?new GangBmxData():Copy(old);if(!mutate(b) || !b.Valid)return false;b.Receipts.Add(receipt);if(b.Receipts.Count>256)b.Receipts.RemoveAt(0);try{w.GangBmx=b;save();changed=true;return true;}catch{w.GangBmx=old;throw;}}
        static bool Owns(GangState s,GangTurfDefinition area,GangBmxAttempt p)=>p!=null && area?.Id==12 && GangBusinessTransactions.Owns(s,area) && GangCrewRules.SameGang(s.Gang,p.Gang);
        public static bool Begin(WalletData w,GangState s,GangTurfDefinition area,GangBmxOptions c,string id,Action save,out bool changed)
        {changed=false;if(!Id(id) || area?.Id!=12 || !GangBusinessTransactions.Owns(s,area) || c?.Valid!=true || !OtherFree(w))return false;return Apply(w,id+":bmx-begin",b=>{if(b.Pending!=null)return false;var a=Account(b,s.Gang);if(a?.Completed==long.MaxValue || a==null && b.Accounts.Count>=640)return false;b.Pending=new GangBmxAttempt{Id=id,Gang=s.Gang,Area=Copy(area),Options=Copy(c)};return true;},save,out changed);}
        public static bool Update(WalletData w,GangState s,GangTurfDefinition area,string id,GangBmxProgress progress,Action save,out bool changed)
        {changed=false;var p=w?.GangBmx?.Pending;if(!Id(id) || p?.Id!=id || w.GangBmx.Valid!=true || p.Ready || !Owns(s,area,p) || !OtherFree(w) || progress?.Valid!=true || progress.Elapsed<p.Progress.Elapsed || progress.Elapsed>p.Options.SecondsLimit*1000 || p.Progress.Faulted && !progress.Faulted)return false;return Apply(w,Guid.NewGuid().ToString("N")+":bmx-sample",b=>{b.Pending.Progress.Elapsed=progress.Elapsed;b.Pending.Progress.Faulted=progress.Faulted;return true;},save,out changed);}
        public static bool Pass(WalletData w,GangState s,GangTurfDefinition area,string id,int index,bool safe,bool ownRider,JobPoint position,Action save,out bool changed)
        {changed=false;var p=w?.GangBmx?.Pending;if(!Id(id) || p?.Id!=id || w.GangBmx.Valid!=true || index<0 || index>=p.Options.Checkpoints.Count || !Owns(s,area,p) || !OtherFree(w) || !GangBmxRules.Pass(safe,ownRider,position,p.Options.Checkpoints[index],p.Options))return false;return Apply(w,id+":bmx-pass:"+index,b=>{if(b.Pending.Failed || b.Pending.Cleared!=index)return false;b.Pending.Cleared++;return true;},save,out changed);}
        public static bool Cancel(WalletData w,string id,Action save,out bool changed){changed=false;if(!Id(id))return false;return Apply(w,id+":bmx-cancel",b=>{if(b.Pending?.Id!=id)return false;b.Pending=null;return true;},save,out changed);}
        public static bool Complete(WalletData w,GangState s,GangTurfDefinition area,string id,DateTime utc,Action save,out bool changed)
        {changed=false;if(!Id(id) || area?.Id!=12 || !GangBusinessTransactions.Owns(s,area) || utc.Kind!=DateTimeKind.Utc || utc.Ticks==0 || !OtherFree(w))return false;return Apply(w,id+":bmx-complete",b=>{var p=b.Pending;if(p?.Id!=id || !p.Ready || !Owns(s,area,p) || utc.Ticks>DateTime.MaxValue.Ticks-TimeSpan.FromMinutes(p.Options.BoostMinutes).Ticks)return false;var a=Account(b,p.Gang);if(a==null){if(b.Accounts.Count>=640)return false;a=new GangBmxAccount{Gang=p.Gang};b.Accounts.Add(a);}if(a.Completed==long.MaxValue)return false;a.Completed++;a.StartedUtcTicks=utc.Ticks;a.ExpiresUtcTicks=utc.AddMinutes(p.Options.BoostMinutes).Ticks;b.Pending=null;return true;},save,out changed);}
    }
}
