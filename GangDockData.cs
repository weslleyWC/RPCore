using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangDockOptions : ExtensibleData
    {
        public int BoostMinutes=60,StartRadius=8,SecondsLimit=600;
        public string TruckModel="phantom",TrailerModel="trflat",CargoModelA="prop_cementbags01",CargoModelB="prop_conc_blocks01a";
        public JobPoint Truck=new JobPoint{Name="Caminhao do porto",X=-779.6579f,Y=-2662.85f,Z=13.88363f,Heading=59.62956f};
        public JobPoint Trailer=new JobPoint{Name="Carreta do porto",X=-816.874f,Y=-2627.552f,Z=13.88067f,Heading=239.2787f};
        public JobPoint Bay=new JobPoint{Name="Baia de carregamento",X=-765.6149f,Y=-2603.296f,Z=13.31192f};
        public JobPoint Destination=new JobPoint{Name="Entrega de materiais",X=1377.557f,Y=-743.3366f,Z=66.7329f};
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public List<JobPoint> Slots=new List<JobPoint>{
            new JobPoint{Name="Palete 1",X=0f,Y=3.2f,Z=0.4f},
            new JobPoint{Name="Palete 2",X=0f,Y=1.8f,Z=0.4f},
            new JobPoint{Name="Palete 3",X=0f,Y=0.4f,Z=0.4f},
            new JobPoint{Name="Palete 4",X=0f,Y=-1f,Z=0.4f},
            new JobPoint{Name="Palete 5",X=0f,Y=-2.4f,Z=0.4f},
            new JobPoint{Name="Palete 6",X=0f,Y=-3.8f,Z=0.4f},
            new JobPoint{Name="Palete 7",X=0f,Y=-5.2f,Z=0.4f}};
        [JsonIgnore] public bool Valid=>BoostMinutes>=1 && BoostMinutes<=180 && StartRadius>=3 && StartRadius<=15 && SecondsLimit>=120 && SecondsLimit<=1200 && GangPresenceRules.ModelName(TruckModel) && GangPresenceRules.ModelName(TrailerModel) && GangPresenceRules.ModelName(CargoModelA) && GangPresenceRules.ModelName(CargoModelB) && Truck?.Valid==true && Trailer?.Valid==true && Bay?.Valid==true && Destination?.Valid==true && DeliveryRoute.Distance(Truck,Trailer)>=15 && DeliveryRoute.Distance(Truck,Trailer)<=150 && DeliveryRoute.Distance(Truck,Bay)<=150 && DeliveryRoute.Distance(Bay,Destination)>=200 && Slots!=null && Slots.Count>=1 && Slots.Count<=20 && Slots.All(p=>p?.Valid==true && Math.Abs(p.X)<=5 && Math.Abs(p.Y)<=10 && p.Z>=0 && p.Z<=5) && Slots.Select(p=>p.X+"|"+p.Y+"|"+p.Z).Distinct().Count()==Slots.Count;
    }
    public sealed class GangDockProgress : ExtensibleData
    {
        public int Elapsed;public float BodyHealth=1000;public bool Faulted;
        [JsonIgnore] public bool Valid=>Elapsed>=0 && Elapsed<=1200000 && GangPresenceRules.Finite(BodyHealth) && BodyHealth>=0 && BodyHealth<=1000;
    }
    public sealed class GangDockAttempt : ExtensibleData
    {
        public string Id,Gang;public GangTurfDefinition Area;public GangDockOptions Options;public GangDockProgress Progress=new GangDockProgress();public int Loaded,Unloaded;
        [JsonIgnore] public bool Failed=>Progress?.Valid!=true || Options?.Valid!=true || Progress.Faulted || Progress.Elapsed>=Options.SecondsLimit*1000 || GangDockRules.Condition(Progress.BodyHealth)<50;
        [JsonIgnore] public bool Ready=>Options?.Valid==true && Unloaded==Options.Slots.Count && !Failed;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && !GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area?.Valid==true && Area.Id==11 && Options?.Valid==true && Progress?.Valid==true && Loaded>=0 && Loaded<=Options.Slots.Count && Unloaded>=0 && Unloaded<=Loaded && (Unloaded==0 || Loaded==Options.Slots.Count) && (Unloaded<Options.Slots.Count || !Failed);
    }
    public sealed class GangDockAccount : ExtensibleData
    {
        public string Gang;public int Area=11;public long Completed,StartedUtcTicks,ExpiresUtcTicks;public double Bonus=2;
        [JsonIgnore] public bool Valid=>!GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area==11 && Completed>0 && (Bonus==1.25 || Bonus==1.5 || Bonus==2) && StartedUtcTicks>0 && ExpiresUtcTicks<=DateTime.MaxValue.Ticks && ExpiresUtcTicks>StartedUtcTicks && ExpiresUtcTicks-StartedUtcTicks<=TimeSpan.FromMinutes(180).Ticks;
    }
    public sealed class GangDockData : ExtensibleData
    {
        public GangDockAttempt Pending;public List<GangDockAccount> Accounts=new List<GangDockAccount>();public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>(Pending==null || Pending.Valid) && Accounts!=null && Accounts.Count<=640 && Accounts.All(a=>a?.Valid==true) && Accounts.Select(a=>a.Gang).Distinct(StringComparer.OrdinalIgnoreCase).Count()==Accounts.Count && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=120) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public static class GangDockRules
    {
        // Source scales body damage by one third before computing the percentage.
        public static float Condition(float body)=>!GangPresenceRules.Finite(body)?0:Math.Max(0,Math.Min(100,100-(1000-Math.Max(0,Math.Min(1000,body)))/30f));
        public static double Reward(float body)=>Condition(body)>=90?2:Condition(body)>=70?1.5:1.25;
        public static bool Station(bool safe,bool driver,bool correctCoupling,float distance,float truckSpeed,float trailerSpeed,bool unloading)=>safe && driver && correctCoupling && GangPresenceRules.Finite(distance) && distance>=0 && distance<=(unloading?9:5) && GangPresenceRules.Finite(truckSpeed) && truckSpeed>=0 && truckSpeed<.5f && GangPresenceRules.Finite(trailerSpeed) && trailerSpeed>=0 && trailerSpeed<.5f;
    }
    public sealed class GangDockClock
    {
        int last;readonly int limit;public GangDockProgress Progress{get;}
        public GangDockClock(int now,int limit,GangDockProgress p){last=now;this.limit=limit;Progress=JsonConvert.DeserializeObject<GangDockProgress>(JsonConvert.SerializeObject(p));}
        public void Tick(int now,bool working,float body){uint delta=unchecked((uint)(now-last));last=now;if(!working || delta>2000 || !GangPresenceRules.Finite(body))return;Progress.Elapsed=(int)Math.Min(limit,(long)Progress.Elapsed+delta);Progress.BodyHealth=Math.Min(Progress.BodyHealth,Math.Max(0,Math.Min(1000,body)));}
    }
    public sealed class GangDockTransfer
    {
        int last;public int Milliseconds{get;private set;}
        public GangDockTransfer(int now){last=now;}
        public bool Step(int now,bool valid,bool unloading){uint delta=unchecked((uint)(now-last));last=now;if(!valid || delta>2000){Milliseconds=0;return false;}int duration=unloading?2000:3000;Milliseconds=(int)Math.Min(duration,(long)Milliseconds+delta);return Milliseconds>=duration;}
    }
    public static class GangDockTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));static bool Id(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=80;
        public static bool Blocked(WalletData w)=>w?.GangDock!=null && (!w.GangDock.Valid || w.GangDock.Pending!=null);
        public static bool OtherFree(WalletData w)=>w!=null && !GangCashierTransactions.Blocked(w) && !GangPierTransactions.Blocked(w) && !GangOffshoreTransactions.Blocked(w) && !GangBmxTransactions.Blocked(w) && !GangChauffeurTransactions.Blocked(w) && !GangTheatreTransactions.Blocked(w) && !GangMowingTransactions.Blocked(w) && !GangPopUpTransactions.Blocked(w) && !GangMedicalTransactions.Blocked(w) && !GangCashRunTransactions.Blocked(w) && !GangTowTransactions.Blocked(w) && !GangMechanicTransactions.Blocked(w) && !GangPromotionTransactions.Blocked(w) && (w.GangBulk==null || w.GangBulk.Valid && w.GangBulk.Pending==null) && (w.GangRaid==null || w.GangRaid.Valid && w.GangRaid.Pending==null) && (w.GangBusiness==null || w.GangBusiness.Valid && w.GangBusiness.Supply==null);
        public static GangDockAccount Account(GangDockData b,string gang)=>b?.Valid==true?b.Accounts.FirstOrDefault(a=>GangCrewRules.SameGang(a.Gang,gang)):null;
        public static double Multiplier(GangDockData b,string gang,int area,DateTime utc){var a=Account(b,gang);return area==11 && utc.Kind==DateTimeKind.Utc && a!=null && utc.Ticks>=a.StartedUtcTicks && utc.Ticks<a.ExpiresUtcTicks?a.Bonus:1;}
        static bool Apply(WalletData w,string receipt,Func<GangDockData,bool> mutate,Action save,out bool changed)
        {changed=false;if(w==null || w.Bank<0 || save==null || w.GangDock!=null && !w.GangDock.Valid)return false;var old=w.GangDock;if(old?.Receipts.Contains(receipt)==true)return true;var b=old==null?new GangDockData():Copy(old);if(!mutate(b) || !b.Valid)return false;b.Receipts.Add(receipt);if(b.Receipts.Count>256)b.Receipts.RemoveAt(0);try{w.GangDock=b;save();changed=true;return true;}catch{w.GangDock=old;throw;}}
        static bool Owns(GangState s,GangTurfDefinition area,GangDockAttempt p)=>p!=null && area?.Id==11 && GangBusinessTransactions.Owns(s,area) && GangCrewRules.SameGang(s.Gang,p.Gang);
        public static bool Begin(WalletData w,GangState s,GangTurfDefinition area,GangDockOptions c,string id,Action save,out bool changed)
        {changed=false;if(!Id(id) || area?.Id!=11 || !GangBusinessTransactions.Owns(s,area) || c?.Valid!=true || !OtherFree(w))return false;return Apply(w,id+":dock-begin",b=>{if(b.Pending!=null)return false;var a=Account(b,s.Gang);if(a?.Completed==long.MaxValue || a==null && b.Accounts.Count>=640)return false;b.Pending=new GangDockAttempt{Id=id,Gang=s.Gang,Area=Copy(area),Options=Copy(c)};return true;},save,out changed);}
        public static bool Update(WalletData w,GangState s,GangTurfDefinition area,string id,GangDockProgress progress,Action save,out bool changed)
        {changed=false;var p=w?.GangDock?.Pending;if(!Id(id) || p?.Id!=id || w.GangDock.Valid!=true || p.Ready || !Owns(s,area,p) || !OtherFree(w) || progress?.Valid!=true || progress.Elapsed<p.Progress.Elapsed || progress.Elapsed>p.Options.SecondsLimit*1000 || progress.BodyHealth>p.Progress.BodyHealth || p.Progress.Faulted && !progress.Faulted)return false;return Apply(w,Guid.NewGuid().ToString("N")+":dock-sample",b=>{b.Pending.Progress.Elapsed=progress.Elapsed;b.Pending.Progress.BodyHealth=progress.BodyHealth;b.Pending.Progress.Faulted=progress.Faulted;return true;},save,out changed);}
        public static bool Load(WalletData w,GangState s,GangTurfDefinition area,string id,int index,Action save,out bool changed)
        {changed=false;if(!Id(id) || index<0 || index>=20 || !Owns(s,area,w?.GangDock?.Pending) || !OtherFree(w))return false;return Apply(w,id+":dock-load:"+index,b=>{var p=b.Pending;if(p.Id!=id || p.Failed || p.Loaded!=index || p.Loaded>=p.Options.Slots.Count || p.Unloaded!=0)return false;p.Loaded++;return true;},save,out changed);}
        public static bool Unload(WalletData w,GangState s,GangTurfDefinition area,string id,int index,Action save,out bool changed)
        {changed=false;if(!Id(id) || index<0 || index>=20 || !Owns(s,area,w?.GangDock?.Pending) || !OtherFree(w))return false;return Apply(w,id+":dock-unload:"+index,b=>{var p=b.Pending;if(p.Id!=id || p.Failed || p.Loaded!=p.Options.Slots.Count || p.Unloaded!=index || p.Unloaded>=p.Loaded)return false;p.Unloaded++;return true;},save,out changed);}
        public static bool Cancel(WalletData w,string id,Action save,out bool changed){changed=false;if(!Id(id))return false;return Apply(w,id+":dock-cancel",b=>{if(b.Pending?.Id!=id)return false;b.Pending=null;return true;},save,out changed);}
        public static bool Complete(WalletData w,GangState s,GangTurfDefinition area,string id,DateTime utc,Action save,out bool changed)
        {changed=false;if(!Id(id) || area?.Id!=11 || !GangBusinessTransactions.Owns(s,area) || utc.Kind!=DateTimeKind.Utc || utc.Ticks==0 || !OtherFree(w))return false;return Apply(w,id+":dock-complete",b=>{var p=b.Pending;if(p?.Id!=id || !p.Ready || !Owns(s,area,p) || utc.Ticks>DateTime.MaxValue.Ticks-TimeSpan.FromMinutes(p.Options.BoostMinutes).Ticks)return false;var a=Account(b,p.Gang);if(a==null){if(b.Accounts.Count>=640)return false;a=new GangDockAccount{Gang=p.Gang};b.Accounts.Add(a);}if(a.Completed==long.MaxValue)return false;a.Completed++;a.Bonus=GangDockRules.Reward(p.Progress.BodyHealth);a.StartedUtcTicks=utc.Ticks;a.ExpiresUtcTicks=utc.AddMinutes(p.Options.BoostMinutes).Ticks;b.Pending=null;return true;},save,out changed);}
    }
}
