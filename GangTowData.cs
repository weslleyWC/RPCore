using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangTowOptions : ExtensibleData
    {
        public int Goal=2,BoostMinutes=60,ReturnRadius=8;public string TruckModel="towtruck";
        public JobPoint TruckSpawn=new JobPoint{Name="Guincho do patio",X=-48.79256f,Y=-1690.796f,Z=29.41298f,Heading=294.2766f};
        public JobPoint Yard=new JobPoint{Name="Patio de recuperacao",X=-44.64281f,Y=-1683.975f,Z=29.38501f};
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public List<string> Models=new List<string>{"asea","primo","stanier","ingot","emperor","emperor2","premier","asterope","fugitive","intruder","surge","tailgater","washington","schafter2","oracle","oracle2","superd","cognoscenti","cognoscenti2","glendale","warrener","blista","dilettante","issi2","panto","prairie","rhapsody","cogcabrio","exemplar","f620","felon","felon2","jackal","sentinel","sentinel2","zion","zion2","baller","baller2","bjxl","cavalcade","cavalcade2","dubsta","dubsta2","fq2","granger","gresley","habanero","huntley","landstalker","mesa","patriot","radi","rocoto","seminole","serrano","xls","blade","buccaneer","dominator","dukes","gauntlet","hotknife","faction","phoenix","picador","ruiner","sabregt","stalion","tampa","vigero","virgo","voodoo","alpha","banshee","buffalo","buffalo2","carbonizzare","comet2","coquette","elegy2","feltzer2","furoregt","fusilade","futo","jester","khamelion","kuruma","massacro","ninef","ninef2","penumbra","rapidgt","rapidgt2","schwarzer","sultan","surano","bison","bison2","bobcatxl","rebel","rebel2","sadler","sandking","sandking2","burrito","burrito3","minivan","paradise","pony","rumpo","speedo","surfer","youga","btype","casco","coquette2","manana","monroe","pigalle","stinger","stingergt","tornado","tornado2","tornado3","ztype"};
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public List<string> Customers=new List<string>{"a_m_y_business_01","a_m_y_business_02","a_m_y_beach_01","a_m_y_hipster_01","a_m_y_hipster_02","a_m_y_vinewood_01","a_m_y_vinewood_02","a_m_y_skater_01","a_m_y_stbla_01","a_m_y_eastsa_01","a_m_m_business_01","a_m_m_bevhills_01","a_m_m_bevhills_02","a_m_m_genfat_01","a_m_m_farmer_01","a_f_y_business_01","a_f_y_business_02","a_f_y_hipster_01","a_f_y_hipster_02","a_f_y_tourist_01","a_f_y_vinewood_01","a_f_m_bevhills_01","a_f_m_business_02","a_f_m_tourist_01"};
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public List<JobPoint> Sites=new List<JobPoint>{new JobPoint{Name="Recuperacao 1",X=-546.297f,Y=-726.466f,Z=32.288f,Heading=209.393f},new JobPoint{Name="Recuperacao 2",X=-1451.083f,Y=-147.203f,Z=49.042f,Heading=219.632f},new JobPoint{Name="Recuperacao 3",X=306.354f,Y=-1687.643f,Z=28.584f,Heading=49.927f},new JobPoint{Name="Recuperacao 4",X=-1071.7f,Y=-428.036f,Z=36.122f,Heading=28.246f},new JobPoint{Name="Recuperacao 5",X=-1345.142f,Y=-352.507f,Z=36.168f,Heading=115.419f},new JobPoint{Name="Recuperacao 6",X=-1394.208f,Y=-954.684f,Z=9.361f,Heading=214.031f},new JobPoint{Name="Recuperacao 7",X=-1186.242f,Y=-1294.992f,Z=4.749f,Heading=205.097f},new JobPoint{Name="Recuperacao 8",X=-895.918f,Y=-1205.883f,Z=4.447f,Heading=300.661f},new JobPoint{Name="Recuperacao 9",X=-901.849f,Y=-246.371f,Z=39.551f,Heading=60.638f},new JobPoint{Name="Recuperacao 10",X=-919.231f,Y=77.442f,Z=51.446f,Heading=254.16f},new JobPoint{Name="Recuperacao 11",X=-622.65f,Y=253.544f,Z=81.121f,Heading=263.999f},new JobPoint{Name="Recuperacao 12",X=-109.739f,Y=60.112f,Z=70.844f,Heading=240.147f},new JobPoint{Name="Recuperacao 13",X=208.928f,Y=245.919f,Z=104.982f,Heading=162.212f},new JobPoint{Name="Recuperacao 14",X=757.57f,Y=-164.682f,Z=74.262f,Heading=242.004f},new JobPoint{Name="Recuperacao 15",X=1033.639f,Y=-422.098f,Z=65.324f,Heading=130.069f}};
        [JsonIgnore] public bool Valid=>Goal>=1 && Goal<=10 && BoostMinutes>=1 && BoostMinutes<=180 && ReturnRadius>=3 && ReturnRadius<=15 && (TruckModel=="towtruck" || TruckModel=="towtruck2") && TruckSpawn?.Valid==true && Yard?.Valid==true && DeliveryRoute.Distance(TruckSpawn,Yard)<=30 && Models!=null && Models.Count>=1 && Models.Count<=160 && Models.All(GangPresenceRules.ModelName) && Models.Distinct(StringComparer.OrdinalIgnoreCase).Count()==Models.Count && Customers!=null && Customers.Count>=1 && Customers.Count<=40 && Customers.All(GangPresenceRules.ModelName) && Customers.Distinct(StringComparer.OrdinalIgnoreCase).Count()==Customers.Count && Sites!=null && Sites.Count>=1 && Sites.Count<=30 && Sites.All(p=>p?.Valid==true) && Sites.Select(p=>p.X+"|"+p.Y+"|"+p.Z).Distinct().Count()==Sites.Count;
    }
    public sealed class GangTowDispatch : ExtensibleData
    {
        public string Model,Customer;public JobPoint Site;
        public bool Matches(GangTowOptions c)=>c?.Models!=null && c.Customers!=null && c.Sites!=null && c.Models.Contains(Model) && c.Customers.Contains(Customer) && Site?.Valid==true && c.Sites.Any(p=>p.X==Site.X && p.Y==Site.Y && p.Z==Site.Z && p.Heading==Site.Heading);
    }
    public sealed class GangTowAttempt : ExtensibleData
    {
        public string Id,Gang;public GangTurfDefinition Area;public GangTowOptions Options;public List<GangTowDispatch> Dispatches=new List<GangTowDispatch>();public List<float> Damage=new List<float>();
        [JsonIgnore] public bool Ready=>Options?.Valid==true && Damage?.Count==Options.Goal;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && !GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area?.Valid==true && Area.Id==3 && Options?.Valid==true && Dispatches!=null && Dispatches.Count==Options.Goal && Dispatches.All(d=>d!=null && d.Matches(Options)) && Damage!=null && Damage.Count<=Options.Goal && Damage.All(GangTowRules.Damage);
    }
    public sealed class GangTowAccount : ExtensibleData
    {
        public string Gang;public int Area=3;public long Completed,StartedUtcTicks,ExpiresUtcTicks;
        [JsonIgnore] public bool Valid=>!GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area==3 && Completed>0 && StartedUtcTicks>0 && ExpiresUtcTicks<=DateTime.MaxValue.Ticks && ExpiresUtcTicks>StartedUtcTicks && ExpiresUtcTicks-StartedUtcTicks<=TimeSpan.FromMinutes(180).Ticks;
    }
    public sealed class GangTowData : ExtensibleData
    {
        public GangTowAttempt Pending;public List<GangTowAccount> Accounts=new List<GangTowAccount>();public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>(Pending==null || Pending.Valid) && Accounts!=null && Accounts.Count<=640 && Accounts.All(a=>a?.Valid==true) && Accounts.Select(a=>a.Gang).Distinct(StringComparer.OrdinalIgnoreCase).Count()==Accounts.Count && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=120) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public sealed class GangTowCrane
    {
        int last;public float Height{get;private set;}
        public GangTowCrane(int now){last=now;}
        public void Step(int now,bool working,bool up,bool down)
        {uint delta=unchecked((uint)(now-last));last=now;if(working && delta<=2000 && up!=down)Height=Math.Max(0,Math.Min(1,Height+(up?1:-1)*delta*(.025f/35f)));}
    }
    public static class GangTowRules
    {
        static bool Value(float n)=>GangPresenceRules.Finite(n) && n>=0;
        public static bool Damage(float n)=>Value(n) && n<=800;
        public static string Condition(float n)=>!Damage(n)?"invalida":n<75?"excelente":n<200?"boa":"ruim";
        public static bool Hook(bool owned,bool driver,bool empty,bool free,float arm,float rearDistance,float speed)=>owned && driver && empty && free && Value(arm) && arm<=.2f && Value(rearDistance) && rearDistance<=3 && Value(speed) && speed<1;
        public static bool Drop(bool owned,bool driver,bool empty,bool attached,bool free,float arm,float truckDistance,float targetDistance,float speed)=>owned && driver && empty && attached && free && Value(arm) && arm<=.2f && Value(truckDistance) && truckDistance<=12 && Value(targetDistance) && targetDistance<=16 && Value(speed) && speed<1;
        public static bool Delivered(bool commanded,bool detached,bool settled,bool validDrop)=>commanded && detached && settled && validDrop;
        public static bool Retire(bool owned,int occupants,float speed,bool foreign)=>owned && occupants==0 && Value(speed) && speed<.5f && !foreign;
    }
    public static class GangTowTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        static bool Id(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=80;
        public static bool Blocked(WalletData w)=>w?.GangTow!=null && (!w.GangTow.Valid || w.GangTow.Pending!=null);
        public static bool OtherFree(WalletData w)=>w!=null && !GangCashRunTransactions.Blocked(w) && !GangMechanicTransactions.Blocked(w) && !GangPromotionTransactions.Blocked(w) && (w.GangBulk==null || w.GangBulk.Valid && w.GangBulk.Pending==null) && (w.GangRaid==null || w.GangRaid.Valid && w.GangRaid.Pending==null) && (w.GangBusiness==null || w.GangBusiness.Valid && w.GangBusiness.Supply==null);
        public static GangTowAccount Account(GangTowData b,string gang)=>b?.Valid==true?b.Accounts.FirstOrDefault(a=>GangCrewRules.SameGang(a.Gang,gang)):null;
        public static int Multiplier(GangTowData b,string gang,int area,DateTime utc)
        {var a=Account(b,gang);return area==3 && utc.Kind==DateTimeKind.Utc && a!=null && utc.Ticks>=a.StartedUtcTicks && utc.Ticks<a.ExpiresUtcTicks?2:1;}
        static bool Apply(WalletData w,string receipt,Func<GangTowData,bool> mutate,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || w.GangTow!=null && !w.GangTow.Valid)return false;
            var old=w.GangTow;if(old?.Receipts.Contains(receipt)==true)return true;var b=old==null?new GangTowData():Copy(old);
            if(!mutate(b) || !b.Valid)return false;b.Receipts.Add(receipt);if(b.Receipts.Count>256)b.Receipts.RemoveAt(0);
            try{w.GangTow=b;save();changed=true;return true;}catch{w.GangTow=old;throw;}
        }
        static bool Owns(GangState s,GangTurfDefinition area,GangTowAttempt p)=>p!=null && area?.Id==3 && GangBusinessTransactions.Owns(s,area) && GangCrewRules.SameGang(s.Gang,p.Gang);
        public static bool Begin(WalletData w,GangState s,GangTurfDefinition area,GangTowOptions c,IEnumerable<GangTowDispatch> dispatches,string id,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || area?.Id!=3 || !GangBusinessTransactions.Owns(s,area) || c?.Valid!=true || dispatches==null || !OtherFree(w))return false;
            var chosen=dispatches.Take(11).ToList();if(chosen.Count!=c.Goal || chosen.Any(d=>d==null || !d.Matches(c)))return false;
            return Apply(w,id+":tow-begin",b=>{if(b.Pending!=null)return false;var a=Account(b,s.Gang);if(a?.Completed==long.MaxValue || a==null && b.Accounts.Count>=640)return false;b.Pending=new GangTowAttempt{Id=id,Gang=s.Gang,Area=Copy(area),Options=Copy(c),Dispatches=Copy(chosen)};return true;},save,out changed);
        }
        public static bool Deliver(WalletData w,GangState s,GangTurfDefinition area,string id,int index,float damage,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || index<0 || index>=10 || !GangTowRules.Damage(damage) || !OtherFree(w) || !Owns(s,area,w?.GangTow?.Pending))return false;
            return Apply(w,id+":tow-deliver:"+index,b=>{var p=b.Pending;if(p?.Id!=id || p.Ready || p.Damage.Count!=index)return false;p.Damage.Add(damage);return true;},save,out changed);
        }
        public static bool Cancel(WalletData w,string id,Action save,out bool changed)
        {changed=false;if(!Id(id))return false;return Apply(w,id+":tow-cancel",b=>{if(b.Pending?.Id!=id)return false;b.Pending=null;return true;},save,out changed);}
        public static bool Complete(WalletData w,GangState s,GangTurfDefinition area,string id,DateTime utc,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || utc.Kind!=DateTimeKind.Utc || utc.Ticks==0 || area?.Id!=3 || !GangBusinessTransactions.Owns(s,area) || !OtherFree(w))return false;
            return Apply(w,id+":tow-complete",b=>{var p=b.Pending;if(p?.Id!=id || !p.Ready || !Owns(s,area,p) || utc.Ticks>DateTime.MaxValue.Ticks-TimeSpan.FromMinutes(p.Options.BoostMinutes).Ticks)return false;
                var a=Account(b,p.Gang);if(a==null){if(b.Accounts.Count>=640)return false;a=new GangTowAccount{Gang=p.Gang};b.Accounts.Add(a);}if(a.Completed==long.MaxValue)return false;
                a.Completed++;a.StartedUtcTicks=utc.Ticks;a.ExpiresUtcTicks=utc.AddMinutes(p.Options.BoostMinutes).Ticks;b.Pending=null;return true;},save,out changed);
        }
    }
}
