using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangOffshoreOptions : ExtensibleData
    {
        public int BoostMinutes=60,StartRadius=8,SecondsLimit=300;
        public string JetModel="seashark",BoatModel="squalo",BuyerModel="a_m_y_business_01";
        public JobPoint Pickup=new JobPoint{Name="Pacote Vespucci",X=-1065.109f,Y=-1158.42f,Z=1.383517f,Heading=232.1834f};
        public JobPoint Jet=new JobPoint{Name="Seashark",X=-1103.408f,Y=-1152.185f,Z=-.5149324f,Heading=27.82469f};
        public JobPoint Buyer=new JobPoint{Name="Comprador em alto-mar",X=-2279.427f,Y=-959.0692f,Z=-.5984672f,Heading=36.92299f};
        [JsonIgnore] public bool Valid=>BoostMinutes>=1 && BoostMinutes<=180 && StartRadius>=3 && StartRadius<=15 && SecondsLimit>=60 && SecondsLimit<=900 && GangPresenceRules.ModelName(JetModel) && GangPresenceRules.ModelName(BoatModel) && GangPresenceRules.ModelName(BuyerModel) && Pickup?.Valid==true && Jet?.Valid==true && Buyer?.Valid==true && DeliveryRoute.Distance(Pickup,Jet)>=10 && DeliveryRoute.Distance(Pickup,Jet)<=150 && DeliveryRoute.Distance(Jet,Buyer)>=200 && DeliveryRoute.Distance(Jet,Buyer)<=5000;
    }
    public sealed class GangOffshoreProgress : ExtensibleData
    {
        public int Elapsed;public bool Faulted;
        [JsonIgnore] public bool Valid=>Elapsed>=0 && Elapsed<=900000;
    }
    public sealed class GangOffshoreAttempt : ExtensibleData
    {
        public string Id,Gang;public GangTurfDefinition Area;public GangOffshoreOptions Options;public GangOffshoreProgress Progress=new GangOffshoreProgress();public bool Collected,Boarded,Delivered;
        [JsonIgnore] public bool Failed=>Progress?.Valid!=true || Options?.Valid!=true || Progress.Faulted || Progress.Elapsed>=Options.SecondsLimit*1000;
        [JsonIgnore] public bool Ready=>Options?.Valid==true && Delivered && !Failed;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && !GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area?.Valid==true && Area.Id==13 && Options?.Valid==true && Progress?.Valid==true && (!Boarded || Collected) && (!Delivered || Collected && Boarded && !Failed);
    }
    public sealed class GangOffshoreAccount : ExtensibleData
    {
        public string Gang;public int Area=13;public long Completed,StartedUtcTicks,ExpiresUtcTicks;
        [JsonIgnore] public bool Valid=>!GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area==13 && Completed>0 && StartedUtcTicks>0 && ExpiresUtcTicks<=DateTime.MaxValue.Ticks && ExpiresUtcTicks>StartedUtcTicks && ExpiresUtcTicks-StartedUtcTicks<=TimeSpan.FromMinutes(180).Ticks;
    }
    public sealed class GangOffshoreData : ExtensibleData
    {
        public GangOffshoreAttempt Pending;public List<GangOffshoreAccount> Accounts=new List<GangOffshoreAccount>();public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>(Pending==null || Pending.Valid) && Accounts!=null && Accounts.Count<=640 && Accounts.All(a=>a?.Valid==true) && Accounts.Select(a=>a.Gang).Distinct(StringComparer.OrdinalIgnoreCase).Count()==Accounts.Count && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=120) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public static class GangOffshoreRules
    {
        public static bool Pickup(bool safe,bool foot,float distance)=>safe && foot && GangPresenceRules.Finite(distance) && distance>=0 && distance<=2;
        public static bool Handover(bool safe,bool ownDriver,bool ownBuyer,float distance,float speed)=>safe && ownDriver && ownBuyer && GangPresenceRules.Finite(distance) && distance>=0 && distance<=5 && GangPresenceRules.Finite(speed) && speed>=0 && speed<1.5f;
    }
    public sealed class GangOffshoreHandover
    {
        int last;public int Milliseconds{get;private set;}
        public GangOffshoreHandover(int now){last=now;}
        public bool Step(int now,bool valid){uint delta=unchecked((uint)(now-last));last=now;if(!valid || delta>2000){Milliseconds=0;return false;}Milliseconds=(int)Math.Min(5000,(long)Milliseconds+delta);return Milliseconds>=5000;}
    }
    public sealed class GangOffshoreClock
    {
        int last;readonly int limit;public GangOffshoreProgress Progress{get;}
        public GangOffshoreClock(int now,int limit,GangOffshoreProgress p){last=now;this.limit=limit;Progress=JsonConvert.DeserializeObject<GangOffshoreProgress>(JsonConvert.SerializeObject(p));}
        public void Tick(int now,bool working){uint delta=unchecked((uint)(now-last));last=now;if(!working || delta>2000)return;Progress.Elapsed=(int)Math.Min(limit,(long)Progress.Elapsed+delta);}
    }
    public static class GangOffshoreTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));static bool Id(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=80;
        public static bool Blocked(WalletData w)=>w?.GangOffshore!=null && (!w.GangOffshore.Valid || w.GangOffshore.Pending!=null);
        public static bool OtherFree(WalletData w)=>w!=null && !GangPierTransactions.Blocked(w) && !GangBmxTransactions.Blocked(w) && !GangDockTransactions.Blocked(w) && !GangChauffeurTransactions.Blocked(w) && !GangTheatreTransactions.Blocked(w) && !GangMowingTransactions.Blocked(w) && !GangPopUpTransactions.Blocked(w) && !GangMedicalTransactions.Blocked(w) && !GangCashRunTransactions.Blocked(w) && !GangTowTransactions.Blocked(w) && !GangMechanicTransactions.Blocked(w) && !GangPromotionTransactions.Blocked(w) && (w.GangBulk==null || w.GangBulk.Valid && w.GangBulk.Pending==null) && (w.GangRaid==null || w.GangRaid.Valid && w.GangRaid.Pending==null) && (w.GangBusiness==null || w.GangBusiness.Valid && w.GangBusiness.Supply==null);
        public static GangOffshoreAccount Account(GangOffshoreData b,string gang)=>b?.Valid==true?b.Accounts.FirstOrDefault(a=>GangCrewRules.SameGang(a.Gang,gang)):null;
        public static int Multiplier(GangOffshoreData b,string gang,int area,DateTime utc){var a=Account(b,gang);return area==13 && utc.Kind==DateTimeKind.Utc && a!=null && utc.Ticks>=a.StartedUtcTicks && utc.Ticks<a.ExpiresUtcTicks?2:1;}
        static bool Apply(WalletData w,string receipt,Func<GangOffshoreData,bool> mutate,Action save,out bool changed)
        {changed=false;if(w==null || w.Bank<0 || save==null || w.GangOffshore!=null && !w.GangOffshore.Valid)return false;var old=w.GangOffshore;if(old?.Receipts.Contains(receipt)==true)return true;var b=old==null?new GangOffshoreData():Copy(old);if(!mutate(b) || !b.Valid)return false;b.Receipts.Add(receipt);if(b.Receipts.Count>256)b.Receipts.RemoveAt(0);try{w.GangOffshore=b;save();changed=true;return true;}catch{w.GangOffshore=old;throw;}}
        static bool Owns(GangState s,GangTurfDefinition area,GangOffshoreAttempt p)=>p!=null && area?.Id==13 && GangBusinessTransactions.Owns(s,area) && GangCrewRules.SameGang(s.Gang,p.Gang);
        public static bool Begin(WalletData w,GangState s,GangTurfDefinition area,GangOffshoreOptions c,string id,Action save,out bool changed)
        {changed=false;if(!Id(id) || area?.Id!=13 || !GangBusinessTransactions.Owns(s,area) || c?.Valid!=true || !OtherFree(w))return false;return Apply(w,id+":sea-begin",b=>{if(b.Pending!=null)return false;var a=Account(b,s.Gang);if(a?.Completed==long.MaxValue || a==null && b.Accounts.Count>=640)return false;b.Pending=new GangOffshoreAttempt{Id=id,Gang=s.Gang,Area=Copy(area),Options=Copy(c)};return true;},save,out changed);}
        public static bool Update(WalletData w,GangState s,GangTurfDefinition area,string id,GangOffshoreProgress progress,Action save,out bool changed)
        {changed=false;var p=w?.GangOffshore?.Pending;if(!Id(id) || p?.Id!=id || w.GangOffshore.Valid!=true || p.Ready || !Owns(s,area,p) || !OtherFree(w) || progress?.Valid!=true || progress.Elapsed<p.Progress.Elapsed || progress.Elapsed>p.Options.SecondsLimit*1000 || p.Progress.Faulted && !progress.Faulted)return false;return Apply(w,Guid.NewGuid().ToString("N")+":sea-sample",b=>{b.Pending.Progress.Elapsed=progress.Elapsed;b.Pending.Progress.Faulted=progress.Faulted;return true;},save,out changed);}
        public static bool Collect(WalletData w,GangState s,GangTurfDefinition area,string id,Action save,out bool changed)
        {changed=false;if(!Id(id) || !Owns(s,area,w?.GangOffshore?.Pending) || !OtherFree(w))return false;return Apply(w,id+":sea-collect",b=>{var p=b.Pending;if(p.Id!=id || p.Failed || p.Collected)return false;p.Collected=true;return true;},save,out changed);}
        public static bool Board(WalletData w,GangState s,GangTurfDefinition area,string id,Action save,out bool changed)
        {changed=false;if(!Id(id) || !Owns(s,area,w?.GangOffshore?.Pending) || !OtherFree(w))return false;return Apply(w,id+":sea-board",b=>{var p=b.Pending;if(p.Id!=id || p.Failed || !p.Collected)return false;p.Boarded=true;return true;},save,out changed);}
        public static bool Deliver(WalletData w,GangState s,GangTurfDefinition area,string id,Action save,out bool changed)
        {changed=false;if(!Id(id) || !Owns(s,area,w?.GangOffshore?.Pending) || !OtherFree(w))return false;return Apply(w,id+":sea-deliver",b=>{var p=b.Pending;if(p.Id!=id || p.Failed || !p.Collected || !p.Boarded)return false;p.Delivered=true;return true;},save,out changed);}
        public static bool Cancel(WalletData w,string id,Action save,out bool changed){changed=false;if(!Id(id))return false;return Apply(w,id+":sea-cancel",b=>{if(b.Pending?.Id!=id)return false;b.Pending=null;return true;},save,out changed);}
        public static bool Complete(WalletData w,GangState s,GangTurfDefinition area,string id,DateTime utc,Action save,out bool changed)
        {changed=false;if(!Id(id) || area?.Id!=13 || !GangBusinessTransactions.Owns(s,area) || utc.Kind!=DateTimeKind.Utc || utc.Ticks==0 || !OtherFree(w))return false;return Apply(w,id+":sea-complete",b=>{var p=b.Pending;if(p?.Id!=id || !p.Ready || !Owns(s,area,p) || utc.Ticks>DateTime.MaxValue.Ticks-TimeSpan.FromMinutes(p.Options.BoostMinutes).Ticks)return false;var a=Account(b,p.Gang);if(a==null){if(b.Accounts.Count>=640)return false;a=new GangOffshoreAccount{Gang=p.Gang};b.Accounts.Add(a);}if(a.Completed==long.MaxValue)return false;a.Completed++;a.StartedUtcTicks=utc.Ticks;a.ExpiresUtcTicks=utc.AddMinutes(p.Options.BoostMinutes).Ticks;b.Pending=null;return true;},save,out changed);}
    }
}
