using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangCashRunOptions : ExtensibleData
    {
        public int BoostMinutes=60,ReturnRadius=8;
        public string VehicleModel="stockade",GuardModel="s_m_m_security_01",BagModel="prop_money_bag_01";
        public JobPoint Spawn=new JobPoint{Name="Stockade",X=-1330.259f,Y=-663.4227f,Z=26.0517f,Heading=127.8134f};
        public JobPoint Depot=new JobPoint{Name="Deposito Gruppe",X=-680.2075f,Y=-615.2822f,Z=25.30618f,Heading=264.3309f};
        public JobPoint Receiver=new JobPoint{Name="Recebedor Gruppe",X=-675.5438f,Y=-613.2014f,Z=25.30774f,Heading=131.4234f};
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public List<JobPoint> Sites=new List<JobPoint>{new JobPoint{Name="Coleta 1",X=-1345.975f,Y=-583.4302f,Z=29.42169f,Heading=118.8955f},new JobPoint{Name="Coleta 2",X=-1348.51f,Y=-636.1747f,Z=27.62371f,Heading=298.137f},new JobPoint{Name="Coleta 3",X=-1313.146f,Y=-688.2759f,Z=25.88811f,Heading=314.1575f},new JobPoint{Name="Coleta 4",X=-1276.763f,Y=-691.4911f,Z=24.22008f,Heading=147.4821f},new JobPoint{Name="Coleta 5",X=-1258.987f,Y=-759.3558f,Z=20.42819f,Heading=327.9869f}};
        [JsonIgnore] public bool Valid=>BoostMinutes>=1 && BoostMinutes<=180 && ReturnRadius>=3 && ReturnRadius<=15 && GangPresenceRules.ModelName(VehicleModel) && GangPresenceRules.ModelName(GuardModel) && GangPresenceRules.ModelName(BagModel) && Spawn?.Valid==true && Depot?.Valid==true && Receiver?.Valid==true && DeliveryRoute.Distance(Depot,Receiver)<=15 && Sites!=null && Sites.Count>=1 && Sites.Count<=10 && Sites.All(p=>p?.Valid==true) && Sites.Select(p=>p.X+"|"+p.Y+"|"+p.Z).Distinct().Count()==Sites.Count;
    }
    public sealed class GangCashRunAttempt : ExtensibleData
    {
        public string Id,Gang;public GangTurfDefinition Area;public GangCashRunOptions Options;public List<int> Loaded=new List<int>();public bool Delivered;
        [JsonIgnore] public bool Ready=>Delivered;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && !GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area?.Valid==true && Area.Id==5 && Options?.Valid==true && Loaded!=null && Loaded.Count<=Options.Sites.Count && Loaded.All(i=>i>=0 && i<Options.Sites.Count) && Loaded.Distinct().Count()==Loaded.Count && (!Delivered || Loaded.Count==Options.Sites.Count);
    }
    public sealed class GangCashRunAccount : ExtensibleData
    {
        public string Gang;public int Area=5;public long Completed,StartedUtcTicks,ExpiresUtcTicks;
        [JsonIgnore] public bool Valid=>!GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area==5 && Completed>0 && StartedUtcTicks>0 && ExpiresUtcTicks<=DateTime.MaxValue.Ticks && ExpiresUtcTicks>StartedUtcTicks && ExpiresUtcTicks-StartedUtcTicks<=TimeSpan.FromMinutes(180).Ticks;
    }
    public sealed class GangCashRunData : ExtensibleData
    {
        public GangCashRunAttempt Pending;public List<GangCashRunAccount> Accounts=new List<GangCashRunAccount>();public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>(Pending==null || Pending.Valid) && Accounts!=null && Accounts.Count<=640 && Accounts.All(a=>a?.Valid==true) && Accounts.Select(a=>a.Gang).Distinct(StringComparer.OrdinalIgnoreCase).Count()==Accounts.Count && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=120) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public static class GangCashRunRules
    {
        static bool Value(float n)=>GangPresenceRules.Finite(n) && n>=0;
        public static bool Load(bool owned,bool foot,bool carrying,bool stopped,float rearDistance,float siteDistance)=>owned && foot && carrying && stopped && Value(rearDistance) && rearDistance<=2.5f && Value(siteDistance) && siteDistance<=35;
        public static bool Arrive(bool owned,bool driver,int loaded,int goal,float distance,float speed)=>owned && driver && goal>=1 && goal<=10 && loaded==goal && Value(distance) && distance<=6 && Value(speed) && speed<.5f;
        public static bool Handover(bool owned,bool foot,bool arrived,bool loaded,float receiverDistance,float truckDistance,float speed)=>owned && foot && arrived && loaded && Value(receiverDistance) && receiverDistance<=2 && Value(truckDistance) && truckDistance<=10 && Value(speed) && speed<.5f;
    }
    public static class GangCashRunTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        static bool Id(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=80;
        public static bool Blocked(WalletData w)=>w?.GangCashRun!=null && (!w.GangCashRun.Valid || w.GangCashRun.Pending!=null);
        public static bool OtherFree(WalletData w)=>w!=null && !GangOffshoreTransactions.Blocked(w) && !GangBmxTransactions.Blocked(w) && !GangDockTransactions.Blocked(w) && !GangChauffeurTransactions.Blocked(w) && !GangTheatreTransactions.Blocked(w) && !GangMowingTransactions.Blocked(w) && !GangPopUpTransactions.Blocked(w) && !GangMedicalTransactions.Blocked(w) && !GangTowTransactions.Blocked(w) && !GangMechanicTransactions.Blocked(w) && !GangPromotionTransactions.Blocked(w) && (w.GangBulk==null || w.GangBulk.Valid && w.GangBulk.Pending==null) && (w.GangRaid==null || w.GangRaid.Valid && w.GangRaid.Pending==null) && (w.GangBusiness==null || w.GangBusiness.Valid && w.GangBusiness.Supply==null);
        public static GangCashRunAccount Account(GangCashRunData b,string gang)=>b?.Valid==true?b.Accounts.FirstOrDefault(a=>GangCrewRules.SameGang(a.Gang,gang)):null;
        public static int Multiplier(GangCashRunData b,string gang,int area,DateTime utc)
        {var a=Account(b,gang);return area==5 && utc.Kind==DateTimeKind.Utc && a!=null && utc.Ticks>=a.StartedUtcTicks && utc.Ticks<a.ExpiresUtcTicks?2:1;}
        static bool Apply(WalletData w,string receipt,Func<GangCashRunData,bool> mutate,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || w.GangCashRun!=null && !w.GangCashRun.Valid)return false;
            var old=w.GangCashRun;if(old?.Receipts.Contains(receipt)==true)return true;var b=old==null?new GangCashRunData():Copy(old);
            if(!mutate(b) || !b.Valid)return false;b.Receipts.Add(receipt);if(b.Receipts.Count>256)b.Receipts.RemoveAt(0);
            try{w.GangCashRun=b;save();changed=true;return true;}catch{w.GangCashRun=old;throw;}
        }
        static bool Owns(GangState s,GangTurfDefinition area,GangCashRunAttempt p)=>p!=null && area?.Id==5 && GangBusinessTransactions.Owns(s,area) && GangCrewRules.SameGang(s.Gang,p.Gang);
        public static bool Begin(WalletData w,GangState s,GangTurfDefinition area,GangCashRunOptions c,string id,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || area?.Id!=5 || !GangBusinessTransactions.Owns(s,area) || c?.Valid!=true || !OtherFree(w))return false;
            return Apply(w,id+":cash-run-begin",b=>{if(b.Pending!=null)return false;var a=Account(b,s.Gang);if(a?.Completed==long.MaxValue || a==null && b.Accounts.Count>=640)return false;b.Pending=new GangCashRunAttempt{Id=id,Gang=s.Gang,Area=Copy(area),Options=Copy(c)};return true;},save,out changed);
        }
        public static bool Load(WalletData w,GangState s,GangTurfDefinition area,string id,int index,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || index<0 || index>=10 || !OtherFree(w) || !Owns(s,area,w?.GangCashRun?.Pending))return false;
            return Apply(w,id+":cash-run-load:"+index,b=>{var p=b.Pending;if(p?.Id!=id || p.Delivered || index>=p.Options.Sites.Count || p.Loaded.Contains(index))return false;p.Loaded.Add(index);return true;},save,out changed);
        }
        public static bool Deliver(WalletData w,GangState s,GangTurfDefinition area,string id,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || !OtherFree(w) || !Owns(s,area,w?.GangCashRun?.Pending))return false;
            return Apply(w,id+":cash-run-deliver",b=>{var p=b.Pending;if(p?.Id!=id || p.Delivered || p.Loaded.Count!=p.Options.Sites.Count)return false;p.Delivered=true;return true;},save,out changed);
        }
        public static bool Restart(WalletData w,string id,Action save,out bool changed)
        {
            changed=false;if(!Id(id))return false;return Apply(w,Guid.NewGuid().ToString("N")+":cash-run-restart",b=>{var p=b.Pending;if(p?.Id!=id || p.Delivered)return false;p.Loaded.Clear();b.Receipts.RemoveAll(x=>x.StartsWith(id+":cash-run-load:",StringComparison.Ordinal));return true;},save,out changed);
        }
        public static bool Cancel(WalletData w,string id,Action save,out bool changed)
        {changed=false;if(!Id(id))return false;return Apply(w,id+":cash-run-cancel",b=>{if(b.Pending?.Id!=id)return false;b.Pending=null;return true;},save,out changed);}
        public static bool Complete(WalletData w,GangState s,GangTurfDefinition area,string id,DateTime utc,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || utc.Kind!=DateTimeKind.Utc || utc.Ticks==0 || area?.Id!=5 || !GangBusinessTransactions.Owns(s,area) || !OtherFree(w))return false;
            return Apply(w,id+":cash-run-complete",b=>{var p=b.Pending;if(p?.Id!=id || !p.Ready || !Owns(s,area,p) || utc.Ticks>DateTime.MaxValue.Ticks-TimeSpan.FromMinutes(p.Options.BoostMinutes).Ticks)return false;
                var a=Account(b,p.Gang);if(a==null){if(b.Accounts.Count>=640)return false;a=new GangCashRunAccount{Gang=p.Gang};b.Accounts.Add(a);}if(a.Completed==long.MaxValue)return false;
                a.Completed++;a.StartedUtcTicks=utc.Ticks;a.ExpiresUtcTicks=utc.AddMinutes(p.Options.BoostMinutes).Ticks;b.Pending=null;return true;},save,out changed);
        }
    }
}
