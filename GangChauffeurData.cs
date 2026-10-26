using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangChauffeurOptions : ExtensibleData
    {
        public int BoostMinutes=60,StartRadius=8;
        public JobPoint Spawn=new JobPoint{Name="Carro VIP",X=-83.87819f,Y=905.1129f,Z=234.9008f,Heading=41.74263f};
        public JobPoint Pickup=new JobPoint{Name="Passageiro VIP",X=-132.5137f,Y=963.5354f,Z=236.0194f,Heading=160.8435f};
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public List<string> Vehicles=new List<string>{"stingergt","stinger","Ztype","f620","adder","bullet","monroe"};
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public List<string> Passengers=new List<string>{"a_m_y_business_01","a_m_m_business_01","a_m_y_business_02","a_f_y_business_01","a_f_y_business_02","a_f_m_business_02","a_m_y_vinewood_01","a_m_y_vinewood_02","a_m_y_vinewood_03","a_m_y_vinewood_04"};
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public List<JobPoint> Destinations=new List<JobPoint>{
            new JobPoint{Name="Eclipse Lounge",X=-84.96f,Y=235.74f,Z=100.56f},
            new JobPoint{Name="Gentry Manor Hotel",X=-53.93f,Y=356.92f,Z=113.06f},
            new JobPoint{Name="Vinewood Gardens Hotel",X=328.71f,Y=-70.77f,Z=72.25f},
            new JobPoint{Name="Richman Hotel",X=-1274.5f,Y=313.97f,Z=65.51f},
            new JobPoint{Name="Los Santos Golf Club",X=-1367.97f,Y=56.55f,Z=53.83f},
            new JobPoint{Name="Tequi-La-La",X=-567.94f,Y=274.83f,Z=83.02f},
            new JobPoint{Name="Pitchers On Vinewood",X=225.0f,Y=337.6f,Z=105.6f},
            new JobPoint{Name="Eclipse Medical",X=-675.5f,Y=312.0f,Z=83.1f},
            new JobPoint{Name="Vinewood Cafe",X=-628.2f,Y=239.0f,Z=81.9f},
            new JobPoint{Name="Haute Restaurant",X=4.5f,Y=242.0f,Z=109.6f},
            new JobPoint{Name="The Pink Sandwich",X=101.0f,Y=210.1f,Z=107.8f},
            new JobPoint{Name="Split Sides West",X=-423.71f,Y=259.76f,Z=83.1f},
            new JobPoint{Name="Oriental Theater",X=292.3f,Y=192.13f,Z=104.37f},
            new JobPoint{Name="Luxury Car Dealer",X=-69.3f,Y=62.9f,Z=71.9f},
            new JobPoint{Name="Rockford Hills Tennis Club",X=-1255.72f,Y=371.67f,Z=75.87f}};
        [JsonIgnore] public bool Valid=>BoostMinutes>=1 && BoostMinutes<=180 && StartRadius>=3 && StartRadius<=15 && Spawn?.Valid==true && Pickup?.Valid==true && DeliveryRoute.Distance(Spawn,Pickup)>=10 && DeliveryRoute.Distance(Spawn,Pickup)<=150 && Vehicles!=null && Vehicles.Count>=1 && Vehicles.Count<=50 && Vehicles.All(GangPresenceRules.ModelName) && Vehicles.Distinct(StringComparer.OrdinalIgnoreCase).Count()==Vehicles.Count && Passengers!=null && Passengers.Count>=1 && Passengers.Count<=50 && Passengers.All(GangPresenceRules.ModelName) && Passengers.Distinct(StringComparer.OrdinalIgnoreCase).Count()==Passengers.Count && Destinations!=null && Destinations.Count>=1 && Destinations.Count<=50 && Destinations.All(p=>p?.Valid==true && DeliveryRoute.Distance(Spawn,p)>=200 && DeliveryRoute.Distance(Spawn,p)<=10000) && Destinations.Select(p=>p.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()==Destinations.Count;
    }
    public sealed class GangChauffeurProgress : ExtensibleData
    {
        public int Elapsed;public float Satisfaction=100,BodyHealth=1000;
        [JsonIgnore] public bool Valid=>Elapsed>=0 && Elapsed<=300000 && GangPresenceRules.Finite(Satisfaction) && Satisfaction>=0 && Satisfaction<=100 && GangPresenceRules.Finite(BodyHealth) && BodyHealth>=0 && BodyHealth<=1000;
    }
    public sealed class GangChauffeurAttempt : ExtensibleData
    {
        public string Id,Gang,Vehicle,Passenger;public GangTurfDefinition Area;public GangChauffeurOptions Options;public int Destination,Personality;public GangChauffeurProgress Progress=new GangChauffeurProgress();public bool Boarded,Delivered;
        [JsonIgnore] public int Limit=>GangChauffeurRules.Limit(Options?.Spawn,Options?.Destinations!=null && Destination>=0 && Destination<Options.Destinations.Count?Options.Destinations[Destination]:null,Personality);
        [JsonIgnore] public bool Failed=>Progress?.Valid!=true || Progress.Elapsed>=Limit || Progress.Satisfaction<=15 || Progress.BodyHealth<=450;
        [JsonIgnore] public bool Ready=>Delivered && !Failed;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && !GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area?.Valid==true && Area.Id==10 && Options?.Valid==true && Options.Vehicles.Contains(Vehicle,StringComparer.OrdinalIgnoreCase) && Options.Passengers.Contains(Passenger,StringComparer.OrdinalIgnoreCase) && Destination>=0 && Destination<Options.Destinations.Count && Personality>=0 && Personality<=2 && Progress?.Valid==true && (!Delivered || Boarded && !Failed);
    }
    public sealed class GangChauffeurAccount : ExtensibleData
    {
        public string Gang;public int Area=10;public long Completed,StartedUtcTicks,ExpiresUtcTicks;public double Bonus=2;
        [JsonIgnore] public bool Valid=>!GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area==10 && Completed>0 && (Bonus==1.25 || Bonus==1.5 || Bonus==2) && StartedUtcTicks>0 && ExpiresUtcTicks<=DateTime.MaxValue.Ticks && ExpiresUtcTicks>StartedUtcTicks && ExpiresUtcTicks-StartedUtcTicks<=TimeSpan.FromMinutes(180).Ticks;
    }
    public sealed class GangChauffeurData : ExtensibleData
    {
        public GangChauffeurAttempt Pending;public List<GangChauffeurAccount> Accounts=new List<GangChauffeurAccount>();public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>(Pending==null || Pending.Valid) && Accounts!=null && Accounts.Count<=640 && Accounts.All(a=>a?.Valid==true) && Accounts.Select(a=>a.Gang).Distinct(StringComparer.OrdinalIgnoreCase).Count()==Accounts.Count && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=120) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public static class GangChauffeurRules
    {
        public static int Limit(JobPoint from,JobPoint to,int personality)
        {if(from?.Valid!=true || to?.Valid!=true || personality<0 || personality>2)return 0;double x=(double)from.X-to.X,y=(double)from.Y-to.Y,z=(double)from.Z-to.Z;int ms=(int)Math.Min(300000,Math.Max(180000,150000+Math.Sqrt(x*x+y*y+z*z)*95));return personality==2?Math.Max(150000,ms-20000):ms;}
        public static double Reward(float satisfaction,float body)=>!GangPresenceRules.Finite(satisfaction) || !GangPresenceRules.Finite(body) || satisfaction<=15 || satisfaction>100 || body<=450 || body>1000?1:satisfaction>=85 && body>=850?2:satisfaction>=60 && body>=650?1.5:1.25;
    }
    // Session observations sampled at source250ms/1s, with snapshots persisted separately.
    public sealed class GangChauffeurMeter
    {
        int last,damageAt,speedAt;float damageBody,lastSpeed;readonly int personality,limit;public GangChauffeurProgress Progress{get;}
        public GangChauffeurMeter(int now,int personality,int limit,GangChauffeurProgress p){last=now;this.personality=personality;this.limit=limit;Progress=JsonConvert.DeserializeObject<GangChauffeurProgress>(JsonConvert.SerializeObject(p));damageBody=p.BodyHealth;}
        public void Tick(int now,bool working,float body,float speed,float distance,bool boarded,bool dropping)
        {
            uint delta=unchecked((uint)(now-last));last=now;if(!working || delta>2000 || !GangPresenceRules.Finite(body) || !GangPresenceRules.Finite(speed) || !GangPresenceRules.Finite(distance) || body<0 || speed<0 || distance<0){damageAt=speedAt=0;lastSpeed=speed;return;}
            Progress.Elapsed=(int)Math.Min(limit,(long)Progress.Elapsed+delta);Progress.BodyHealth=Math.Min(Progress.BodyHealth,Math.Min(1000,body));damageAt+=(int)delta;speedAt+=(int)delta;
            if(damageAt>=1000)
            {float seconds=Math.Min(2,damageAt/1000f),loss=Math.Max(0,damageBody-Progress.BodyHealth);damageBody=Progress.BodyHealth;damageAt=0;if(loss>=8)Progress.Satisfaction-=loss*.12f*(personality==1?1.5f:1);
                if(!boarded && Progress.Elapsed>30000)Progress.Satisfaction-=(personality==1?1.5f:.8f)*seconds;
                else if(boarded && !dropping){if(speed*3.6f>(personality==1?75:personality==2?135:105))Progress.Satisfaction-=(personality==1?2.2f:1)*seconds;if(personality==2 && speed*3.6f<3 && distance>35)Progress.Satisfaction-=.5f*seconds;}}
            if(speedAt>=250){if(boarded && !dropping && Math.Abs(speed-lastSpeed)/(speedAt/1000f)>8 && distance>25)Progress.Satisfaction-=personality==1?1.8f:.8f;lastSpeed=speed;speedAt=0;}
            Progress.Satisfaction=Math.Max(0,Math.Min(100,Progress.Satisfaction));
        }
    }
    public static class GangChauffeurTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));static bool Id(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=80;
        public static bool Blocked(WalletData w)=>w?.GangChauffeur!=null && (!w.GangChauffeur.Valid || w.GangChauffeur.Pending!=null);
        public static bool OtherFree(WalletData w)=>w!=null && !GangPierTransactions.Blocked(w) && !GangOffshoreTransactions.Blocked(w) && !GangBmxTransactions.Blocked(w) && !GangDockTransactions.Blocked(w) && !GangTheatreTransactions.Blocked(w) && !GangMowingTransactions.Blocked(w) && !GangPopUpTransactions.Blocked(w) && !GangMedicalTransactions.Blocked(w) && !GangCashRunTransactions.Blocked(w) && !GangTowTransactions.Blocked(w) && !GangMechanicTransactions.Blocked(w) && !GangPromotionTransactions.Blocked(w) && (w.GangBulk==null || w.GangBulk.Valid && w.GangBulk.Pending==null) && (w.GangRaid==null || w.GangRaid.Valid && w.GangRaid.Pending==null) && (w.GangBusiness==null || w.GangBusiness.Valid && w.GangBusiness.Supply==null);
        public static GangChauffeurAccount Account(GangChauffeurData b,string gang)=>b?.Valid==true?b.Accounts.FirstOrDefault(a=>GangCrewRules.SameGang(a.Gang,gang)):null;
        public static double Multiplier(GangChauffeurData b,string gang,int area,DateTime utc){var a=Account(b,gang);return area==10 && utc.Kind==DateTimeKind.Utc && a!=null && utc.Ticks>=a.StartedUtcTicks && utc.Ticks<a.ExpiresUtcTicks?a.Bonus:1;}
        static bool Apply(WalletData w,string receipt,Func<GangChauffeurData,bool> mutate,Action save,out bool changed)
        {changed=false;if(w==null || w.Bank<0 || save==null || w.GangChauffeur!=null && !w.GangChauffeur.Valid)return false;var old=w.GangChauffeur;if(old?.Receipts.Contains(receipt)==true)return true;var b=old==null?new GangChauffeurData():Copy(old);if(!mutate(b) || !b.Valid)return false;b.Receipts.Add(receipt);if(b.Receipts.Count>256)b.Receipts.RemoveAt(0);try{w.GangChauffeur=b;save();changed=true;return true;}catch{w.GangChauffeur=old;throw;}}
        static bool Owns(GangState s,GangTurfDefinition area,GangChauffeurAttempt p)=>p!=null && area?.Id==10 && GangBusinessTransactions.Owns(s,area) && GangCrewRules.SameGang(s.Gang,p.Gang);
        public static bool Begin(WalletData w,GangState s,GangTurfDefinition area,GangChauffeurOptions c,string id,string car,string ped,int destination,int personality,Action save,out bool changed)
        {changed=false;if(!Id(id) || area?.Id!=10 || !GangBusinessTransactions.Owns(s,area) || c?.Valid!=true || !OtherFree(w))return false;return Apply(w,id+":vip-begin",b=>{if(b.Pending!=null)return false;var a=Account(b,s.Gang);if(a?.Completed==long.MaxValue || a==null && b.Accounts.Count>=640)return false;b.Pending=new GangChauffeurAttempt{Id=id,Gang=s.Gang,Area=Copy(area),Options=Copy(c),Vehicle=car,Passenger=ped,Destination=destination,Personality=personality};return b.Pending.Valid;},save,out changed);}
        public static bool Update(WalletData w,GangState s,GangTurfDefinition area,string id,GangChauffeurProgress progress,Action save,out bool changed)
        {changed=false;var p=w?.GangChauffeur?.Pending;if(!Id(id) || p?.Id!=id || w.GangChauffeur.Valid!=true || p.Delivered || !Owns(s,area,p) || !OtherFree(w) || progress?.Valid!=true || progress.Elapsed<p.Progress.Elapsed || progress.Elapsed>p.Limit || progress.Satisfaction>p.Progress.Satisfaction || progress.BodyHealth>p.Progress.BodyHealth)return false;return Apply(w,Guid.NewGuid().ToString("N")+":vip-sample",b=>{b.Pending.Progress.Elapsed=progress.Elapsed;b.Pending.Progress.Satisfaction=progress.Satisfaction;b.Pending.Progress.BodyHealth=progress.BodyHealth;return true;},save,out changed);}
        public static bool Board(WalletData w,GangState s,GangTurfDefinition area,string id,Action save,out bool changed)
        {changed=false;if(!Id(id) || !Owns(s,area,w?.GangChauffeur?.Pending) || !OtherFree(w))return false;return Apply(w,id+":vip-board",b=>{if(b.Pending.Id!=id || b.Pending.Failed || b.Pending.Delivered)return false;b.Pending.Boarded=true;return true;},save,out changed);}
        public static bool Deliver(WalletData w,GangState s,GangTurfDefinition area,string id,Action save,out bool changed)
        {changed=false;if(!Id(id) || !Owns(s,area,w?.GangChauffeur?.Pending) || !OtherFree(w))return false;return Apply(w,id+":vip-deliver",b=>{if(b.Pending.Id!=id || b.Pending.Failed || !b.Pending.Boarded)return false;b.Pending.Delivered=true;return true;},save,out changed);}
        public static bool Cancel(WalletData w,string id,Action save,out bool changed){changed=false;if(!Id(id))return false;return Apply(w,id+":vip-cancel",b=>{if(b.Pending?.Id!=id)return false;b.Pending=null;return true;},save,out changed);}
        public static bool Complete(WalletData w,GangState s,GangTurfDefinition area,string id,DateTime utc,Action save,out bool changed)
        {changed=false;if(!Id(id) || area?.Id!=10 || !GangBusinessTransactions.Owns(s,area) || utc.Kind!=DateTimeKind.Utc || utc.Ticks==0 || !OtherFree(w))return false;return Apply(w,id+":vip-complete",b=>{var p=b.Pending;if(p?.Id!=id || !p.Ready || !Owns(s,area,p) || utc.Ticks>DateTime.MaxValue.Ticks-TimeSpan.FromMinutes(p.Options.BoostMinutes).Ticks)return false;var a=Account(b,p.Gang);if(a==null){if(b.Accounts.Count>=640)return false;a=new GangChauffeurAccount{Gang=p.Gang};b.Accounts.Add(a);}if(a.Completed==long.MaxValue)return false;a.Completed++;a.Bonus=GangChauffeurRules.Reward(p.Progress.Satisfaction,p.Progress.BodyHealth);a.StartedUtcTicks=utc.Ticks;a.ExpiresUtcTicks=utc.AddMinutes(p.Options.BoostMinutes).Ticks;b.Pending=null;return true;},save,out changed);}
    }
}
