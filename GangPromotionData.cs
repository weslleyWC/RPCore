using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangPromotionOptions : ExtensibleData
    {
        public int Goal=5,BoostMinutes=60,GiveMilliseconds=1500,SearchRadius=120,MinDistance=50,ReturnRadius=5;
        public float InteractionDistance=2.2f;
        public JobPoint Shop=new JobPoint{Name="Loja de tatuagem",X=1320.043f,Y=-1647.478f,Z=52.14559f};
        [JsonIgnore] public bool Valid=>Goal>=1 && Goal<=20 && BoostMinutes>=1 && BoostMinutes<=180 && GiveMilliseconds>=500 && GiveMilliseconds<=5000 && SearchRadius>=60 && SearchRadius<=250 && MinDistance>=10 && MinDistance<SearchRadius && ReturnRadius>=2 && ReturnRadius<=12 && GangPresenceRules.Finite(InteractionDistance) && InteractionDistance>=1 && InteractionDistance<=4 && Shop?.Valid==true;
    }
    public sealed class GangPromotionAttempt : ExtensibleData
    {
        public string Id,Gang;public GangTurfDefinition Area;public GangPromotionOptions Options;
        public List<string> Customers=new List<string>();
        [JsonIgnore] public bool Ready=>Options?.Valid==true && Customers?.Count==Options.Goal;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && !GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area?.Valid==true && Area.Id==1 && Options?.Valid==true && Customers!=null && Customers.Count<=Options.Goal && Customers.All(x=>!string.IsNullOrWhiteSpace(x) && x.Length<=80) && Customers.Distinct().Count()==Customers.Count;
    }
    public sealed class GangPromotionAccount : ExtensibleData
    {
        public string Gang;public int Area;public long Completed,StartedUtcTicks,ExpiresUtcTicks;
        [JsonIgnore] public bool Valid=>!GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area==1 && Completed>0 && StartedUtcTicks>0 && ExpiresUtcTicks<=DateTime.MaxValue.Ticks && ExpiresUtcTicks>StartedUtcTicks && ExpiresUtcTicks-StartedUtcTicks<=TimeSpan.FromMinutes(180).Ticks;
    }
    public sealed class GangPromotionData : ExtensibleData
    {
        public GangPromotionAttempt Pending;
        public List<GangPromotionAccount> Accounts=new List<GangPromotionAccount>();public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>(Pending==null || Pending.Valid) && Accounts!=null && Accounts.Count<=640 && Accounts.All(a=>a?.Valid==true) && Accounts.GroupBy(a=>a.Area+"|"+a.Gang,StringComparer.OrdinalIgnoreCase).All(g=>g.Count()==1) && Receipts!=null && Receipts.Count<=256 && Receipts.All(x=>!string.IsNullOrWhiteSpace(x) && x.Length<=180) && Receipts.Distinct().Count()==Receipts.Count;
    }
    // Transient observations only; no entity handle or ambient-ped ownership enters saves.
    public sealed class GangPromotionCustomer
    {
        public bool Human,Alive,OnFoot,Combat,Ragdoll,Ambient,Grouped,KnownGang,Used;
        public int PedType;public double ShopDistance,PlayerDistance;
    }
    public static class GangPromotionTargetRules
    {
        public static bool Eligible(GangPromotionCustomer p,GangPromotionOptions c,bool selecting)=>p!=null && c?.Valid==true && p.Human && p.Alive && p.OnFoot && !p.Combat && !p.Ragdoll && p.Ambient && !p.Grouped && !p.KnownGang && !p.Used && (p.PedType==4 || p.PedType==5) && !double.IsNaN(p.ShopDistance) && !double.IsInfinity(p.ShopDistance) && !double.IsNaN(p.PlayerDistance) && !double.IsInfinity(p.PlayerDistance) && p.ShopDistance>=c.MinDistance && p.ShopDistance<=c.SearchRadius && p.PlayerDistance>=0 && (!selecting || p.PlayerDistance>=8);
    }
    public sealed class GangPromotionClock
    {
        int last;public int Milliseconds{get;private set;}
        public GangPromotionClock(int now){last=now;}
        public bool Step(int now,bool working,int duration)
        {uint delta=unchecked((uint)(now-last));last=now;if(duration<500 || duration>5000)return false;if(working && delta<=2000)Milliseconds=(int)Math.Min(duration,(long)Milliseconds+delta);return working && Milliseconds>=duration;}
    }
    public static class GangPromotionTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        static bool Id(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=80;
        static void Receipt(GangPromotionData b,string id){b.Receipts.Add(id);if(b.Receipts.Count>256)b.Receipts.RemoveAt(0);}
        public static bool Blocked(WalletData w)=>w?.GangPromotion!=null && (!w.GangPromotion.Valid || w.GangPromotion.Pending!=null);
        // Do not call the shared territorial blocker here: it deliberately includes this contract.
        public static bool OtherFree(WalletData w)=>w!=null && !GangBmxTransactions.Blocked(w) && !GangDockTransactions.Blocked(w) && !GangChauffeurTransactions.Blocked(w) && !GangTheatreTransactions.Blocked(w) && !GangMowingTransactions.Blocked(w) && !GangPopUpTransactions.Blocked(w) && !GangMedicalTransactions.Blocked(w) && !GangCashRunTransactions.Blocked(w) && !GangTowTransactions.Blocked(w) && !GangMechanicTransactions.Blocked(w) && (w.GangBulk==null || w.GangBulk.Valid && w.GangBulk.Pending==null) && (w.GangRaid==null || w.GangRaid.Valid && w.GangRaid.Pending==null) && (w.GangBusiness==null || w.GangBusiness.Valid && w.GangBusiness.Supply==null);
        public static GangPromotionAccount Account(GangPromotionData b,string gang)=>b?.Valid==true?b.Accounts.FirstOrDefault(a=>GangCrewRules.SameGang(a.Gang,gang)):null;
        public static int Multiplier(GangPromotionData b,string gang,int area,DateTime utc)
        {var a=Account(b,gang);return area==1 && utc.Kind==DateTimeKind.Utc && a!=null && utc.Ticks>=a.StartedUtcTicks && utc.Ticks<a.ExpiresUtcTicks?2:1;}
        static bool Apply(WalletData w,string receipt,Func<GangPromotionData,bool> mutate,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || w.GangPromotion!=null && !w.GangPromotion.Valid)return false;
            var old=w.GangPromotion;if(old?.Receipts.Contains(receipt)==true)return true;var book=old==null?new GangPromotionData():Copy(old);
            if(!mutate(book) || !book.Valid)return false;Receipt(book,receipt);
            try{w.GangPromotion=book;save();changed=true;return true;}catch{w.GangPromotion=old;throw;}
        }
        static bool Owns(GangState s,GangTurfDefinition area,GangPromotionAttempt p)=>p!=null && area?.Id==1 && GangBusinessTransactions.Owns(s,area) && GangCrewRules.SameGang(s.Gang,p.Gang);
        public static bool Begin(WalletData w,GangState s,GangTurfDefinition area,GangPromotionOptions c,string id,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || c?.Valid!=true || area?.Id!=1 || !GangBusinessTransactions.Owns(s,area) || !OtherFree(w))return false;
            return Apply(w,id+":promotion-begin",b=>{if(b.Pending!=null)return false;var a=Account(b,s.Gang);if(a?.Completed==long.MaxValue || a==null && b.Accounts.Count>=640)return false;b.Pending=new GangPromotionAttempt{Id=id,Gang=s.Gang,Area=Copy(area),Options=Copy(c)};return true;},save,out changed);
        }
        public static bool Give(WalletData w,GangState s,GangTurfDefinition area,string id,string customer,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || !Id(customer) || !OtherFree(w) || !Owns(s,area,w?.GangPromotion?.Pending))return false;
            return Apply(w,id+":promotion-give:"+customer,b=>{var p=b.Pending;if(p==null || p.Id!=id || p.Ready || p.Customers.Contains(customer))return false;p.Customers.Add(customer);return true;},save,out changed);
        }
        public static bool Restart(WalletData w,string id,Action save,out bool changed)
        {
            changed=false;if(!Id(id))return false;
            return Apply(w,Guid.NewGuid().ToString("N")+":promotion-restart",b=>{if(b.Pending?.Id!=id || b.Pending.Ready)return false;b.Pending.Customers.Clear();b.Receipts.RemoveAll(x=>x.StartsWith(id+":promotion-give:",StringComparison.Ordinal));return true;},save,out changed);
        }
        public static bool Cancel(WalletData w,string id,Action save,out bool changed)
        {changed=false;if(!Id(id))return false;return Apply(w,id+":promotion-cancel",b=>{if(b.Pending?.Id!=id)return false;b.Pending=null;return true;},save,out changed);}
        public static bool Complete(WalletData w,GangState s,GangTurfDefinition area,string id,DateTime utc,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || utc.Kind!=DateTimeKind.Utc || utc.Ticks==0 || area?.Id!=1 || !GangBusinessTransactions.Owns(s,area) || !OtherFree(w))return false;
            return Apply(w,id+":promotion-complete",b=>{
                var p=b.Pending;if(p==null || p.Id!=id || !p.Ready || !Owns(s,area,p) || utc.Ticks>DateTime.MaxValue.Ticks-TimeSpan.FromMinutes(p.Options.BoostMinutes).Ticks)return false;
                var a=Account(b,p.Gang);if(a==null){if(b.Accounts.Count>=640)return false;a=new GangPromotionAccount{Gang=p.Gang,Area=1};b.Accounts.Add(a);}if(a.Completed==long.MaxValue)return false;
                a.Completed++;a.StartedUtcTicks=utc.Ticks;a.ExpiresUtcTicks=utc.AddMinutes(p.Options.BoostMinutes).Ticks;b.Pending=null;return true;
            },save,out changed);
        }
    }
}
