using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangTheatreItem : ExtensibleData
    {
        public string Name,Model;public JobPoint Point;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Name) && Name.Length<=80 && GangPresenceRules.ModelName(Model) && Point?.Valid==true;
    }
    public sealed class GangTheatreOptions : ExtensibleData
    {
        public int BoostMinutes=60,StartRadius=8,HandoverMilliseconds=1500;
        public float InteractionDistance=2.3f;
        public string CrateModel="prop_mb_crate_01a";
        public JobPoint Crate=new JobPoint{Name="Equipamentos da estreia",X=308.4233f,Y=197.2859f,Z=103.1828f,Heading=-88.3205f};
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public List<GangTheatreItem> Items=new List<GangTheatreItem>{
            new GangTheatreItem{Name="Studio light 1",Model="prop_studio_light_02",Point=new JobPoint{Name="Studio light 1",X=305.6196f,Y=190.47f,Z=103.226f,Heading=-86.9607f}},
            new GangTheatreItem{Name="Studio light 2",Model="prop_studio_light_02",Point=new JobPoint{Name="Studio light 2",X=313.2229f,Y=193.113f,Z=103.3259f,Heading=-115.9403f}},
            new GangTheatreItem{Name="Entrance fence 1",Model="prop_premier_fence_02",Point=new JobPoint{Name="Entrance fence 1",X=302.5866f,Y=183.7614f,Z=104.277f,Heading=-20.2221f}},
            new GangTheatreItem{Name="Entrance fence 2",Model="prop_premier_fence_02",Point=new JobPoint{Name="Entrance fence 2",X=301.5858f,Y=185.3394f,Z=104.2858f,Heading=-106.5602f}},
            new GangTheatreItem{Name="Entrance fence 3",Model="prop_premier_fence_02",Point=new JobPoint{Name="Entrance fence 3",X=302.3372f,Y=187.743f,Z=104.2681f,Heading=-107.4486f}},
            new GangTheatreItem{Name="Fence 1",Model="prop_premier_fence_01",Point=new JobPoint{Name="Fence 1",X=296.0051f,Y=201.9328f,Z=103.5298f,Heading=-110.0f}},
            new GangTheatreItem{Name="Fence 2",Model="prop_premier_fence_01",Point=new JobPoint{Name="Fence 2",X=295.2214f,Y=199.7796f,Z=103.5298f,Heading=-110.0f}},
            new GangTheatreItem{Name="Fence 3",Model="prop_premier_fence_01",Point=new JobPoint{Name="Fence 3",X=294.4396f,Y=197.6318f,Z=103.5275f,Heading=-110.0f}},
            new GangTheatreItem{Name="Fence 4",Model="prop_premier_fence_01",Point=new JobPoint{Name="Fence 4",X=293.6559f,Y=195.4787f,Z=103.526f,Heading=-110.0f}},
            new GangTheatreItem{Name="Fence 5",Model="prop_premier_fence_01",Point=new JobPoint{Name="Fence 5",X=292.8725f,Y=193.326f,Z=103.5099f,Heading=-110.0f}},
            new GangTheatreItem{Name="Red Carpet",Model="prop_premier_fence_01",Point=new JobPoint{Name="Red Carpet",X=300.7819f,Y=200.0613f,Z=104.3407f,Heading=343.0669f}}};
        [JsonIgnore] public bool Valid=>BoostMinutes>=1 && BoostMinutes<=180 && StartRadius>=3 && StartRadius<=15 && HandoverMilliseconds>=500 && HandoverMilliseconds<=5000 && GangPresenceRules.Finite(InteractionDistance) && InteractionDistance>=1 && InteractionDistance<=3 && GangPresenceRules.ModelName(CrateModel) && Crate?.Valid==true && Items!=null && Items.Count>=1 && Items.Count<=30 && Items.All(p=>p?.Valid==true && DeliveryRoute.Distance(Crate,p.Point)>=3 && DeliveryRoute.Distance(Crate,p.Point)<=100) && Items.Select(p=>p.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()==Items.Count && Items.Select(p=>p.Point.X+"|"+p.Point.Y+"|"+p.Point.Z).Distinct().Count()==Items.Count;
    }
    public sealed class GangTheatreAttempt : ExtensibleData
    {
        public string Id,Gang;public GangTurfDefinition Area;public GangTheatreOptions Options;public int Installed;public bool Carrying;
        [JsonIgnore] public bool Ready=>Options?.Valid==true && Installed==Options.Items.Count && !Carrying;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && !GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area?.Valid==true && Area.Id==9 && Options?.Valid==true && Installed>=0 && Installed<=Options.Items.Count && (!Carrying || Installed<Options.Items.Count);
    }
    public sealed class GangTheatreAccount : ExtensibleData
    {
        public string Gang;public int Area=9;public long Completed,StartedUtcTicks,ExpiresUtcTicks;
        [JsonIgnore] public bool Valid=>!GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area==9 && Completed>0 && StartedUtcTicks>0 && ExpiresUtcTicks<=DateTime.MaxValue.Ticks && ExpiresUtcTicks>StartedUtcTicks && ExpiresUtcTicks-StartedUtcTicks<=TimeSpan.FromMinutes(180).Ticks;
    }
    public sealed class GangTheatreData : ExtensibleData
    {
        public GangTheatreAttempt Pending;public List<GangTheatreAccount> Accounts=new List<GangTheatreAccount>();public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>(Pending==null || Pending.Valid) && Accounts!=null && Accounts.Count<=640 && Accounts.All(a=>a?.Valid==true) && Accounts.Select(a=>a.Gang).Distinct(StringComparer.OrdinalIgnoreCase).Count()==Accounts.Count && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=120) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public static class GangTheatreRules
    {
        public static bool Interact(bool safe,bool foot,float distance,GangTheatreOptions c)=>safe && foot && c?.Valid==true && GangPresenceRules.Finite(distance) && distance>=0 && distance<=c.InteractionDistance;
    }
    // A session-only lease: an already active IPL is borrowed, never removed by this activity.
    public sealed class GangTheatreIplLease
    {
        public bool Owned{get;private set;}int last;long remaining;
        public bool Celebrate(bool alreadyActive,int now)
        {if(alreadyActive && !Owned)return false;Owned=true;last=now;remaining=600000;return !alreadyActive;}
        public bool Tick(int now,bool running)
        {uint delta=unchecked((uint)(now-last));last=now;if(!Owned)return false;if(running && delta<=2000)remaining-=delta;if(remaining>0)return false;Owned=false;return true;}
        public bool Release(){bool owned=Owned;Owned=false;remaining=0;return owned;}
    }
    public static class GangTheatreTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        static bool Id(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=80;
        public static bool Blocked(WalletData w)=>w?.GangTheatre!=null && (!w.GangTheatre.Valid || w.GangTheatre.Pending!=null);
        public static bool OtherFree(WalletData w)=>w!=null && !GangPierTransactions.Blocked(w) && !GangOffshoreTransactions.Blocked(w) && !GangBmxTransactions.Blocked(w) && !GangDockTransactions.Blocked(w) && !GangChauffeurTransactions.Blocked(w) && !GangMowingTransactions.Blocked(w) && !GangPopUpTransactions.Blocked(w) && !GangMedicalTransactions.Blocked(w) && !GangCashRunTransactions.Blocked(w) && !GangTowTransactions.Blocked(w) && !GangMechanicTransactions.Blocked(w) && !GangPromotionTransactions.Blocked(w) && (w.GangBulk==null || w.GangBulk.Valid && w.GangBulk.Pending==null) && (w.GangRaid==null || w.GangRaid.Valid && w.GangRaid.Pending==null) && (w.GangBusiness==null || w.GangBusiness.Valid && w.GangBusiness.Supply==null);
        public static GangTheatreAccount Account(GangTheatreData b,string gang)=>b?.Valid==true?b.Accounts.FirstOrDefault(a=>GangCrewRules.SameGang(a.Gang,gang)):null;
        public static int Multiplier(GangTheatreData b,string gang,int area,DateTime utc)
        {var a=Account(b,gang);return area==9 && utc.Kind==DateTimeKind.Utc && a!=null && utc.Ticks>=a.StartedUtcTicks && utc.Ticks<a.ExpiresUtcTicks?2:1;}
        static bool Apply(WalletData w,string receipt,Func<GangTheatreData,bool> mutate,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || w.GangTheatre!=null && !w.GangTheatre.Valid)return false;
            var old=w.GangTheatre;if(old?.Receipts.Contains(receipt)==true)return true;var b=old==null?new GangTheatreData():Copy(old);
            if(!mutate(b) || !b.Valid)return false;b.Receipts.Add(receipt);if(b.Receipts.Count>256)b.Receipts.RemoveAt(0);
            try{w.GangTheatre=b;save();changed=true;return true;}catch{w.GangTheatre=old;throw;}
        }
        static bool Owns(GangState s,GangTurfDefinition area,GangTheatreAttempt p)=>p!=null && area?.Id==9 && GangBusinessTransactions.Owns(s,area) && GangCrewRules.SameGang(s.Gang,p.Gang);
        public static bool Begin(WalletData w,GangState s,GangTurfDefinition area,GangTheatreOptions c,string id,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || area?.Id!=9 || !GangBusinessTransactions.Owns(s,area) || c?.Valid!=true || !OtherFree(w))return false;
            return Apply(w,id+":theatre-begin",b=>{if(b.Pending!=null)return false;var a=Account(b,s.Gang);if(a?.Completed==long.MaxValue || a==null && b.Accounts.Count>=640)return false;b.Pending=new GangTheatreAttempt{Id=id,Gang=s.Gang,Area=Copy(area),Options=Copy(c)};return true;},save,out changed);
        }
        public static bool Collect(WalletData w,GangState s,GangTurfDefinition area,string id,int index,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || index<0 || index>=30 || !OtherFree(w) || !Owns(s,area,w?.GangTheatre?.Pending))return false;
            return Apply(w,id+":theatre-collect:"+index,b=>{var p=b.Pending;if(p.Id!=id || p.Ready || p.Carrying || p.Installed!=index)return false;p.Carrying=true;return true;},save,out changed);
        }
        public static bool Install(WalletData w,GangState s,GangTurfDefinition area,string id,int index,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || index<0 || index>=30 || !OtherFree(w) || !Owns(s,area,w?.GangTheatre?.Pending))return false;
            return Apply(w,id+":theatre-install:"+index,b=>{var p=b.Pending;if(p.Id!=id || p.Ready || !p.Carrying || p.Installed!=index)return false;p.Carrying=false;p.Installed++;return true;},save,out changed);
        }
        public static bool Cancel(WalletData w,string id,Action save,out bool changed)
        {changed=false;if(!Id(id))return false;return Apply(w,id+":theatre-cancel",b=>{if(b.Pending?.Id!=id)return false;b.Pending=null;return true;},save,out changed);}
        public static bool Complete(WalletData w,GangState s,GangTurfDefinition area,string id,DateTime utc,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || utc.Kind!=DateTimeKind.Utc || utc.Ticks==0 || area?.Id!=9 || !GangBusinessTransactions.Owns(s,area) || !OtherFree(w))return false;
            return Apply(w,id+":theatre-complete",b=>{var p=b.Pending;if(p?.Id!=id || !p.Ready || !Owns(s,area,p) || utc.Ticks>DateTime.MaxValue.Ticks-TimeSpan.FromMinutes(p.Options.BoostMinutes).Ticks)return false;
                var a=Account(b,p.Gang);if(a==null){if(b.Accounts.Count>=640)return false;a=new GangTheatreAccount{Gang=p.Gang};b.Accounts.Add(a);}if(a.Completed==long.MaxValue)return false;
                a.Completed++;a.StartedUtcTicks=utc.Ticks;a.ExpiresUtcTicks=utc.AddMinutes(p.Options.BoostMinutes).Ticks;b.Pending=null;return true;},save,out changed);
        }
    }
}
