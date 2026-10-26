using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangMechanicOptions : ExtensibleData
    {
        public int Goal=3,RequiredHits=3,BoostMinutes=60,ReturnRadius=5,ArrivalSeconds=90,DepartureSeconds=45;
        public string DriverModel="a_m_y_business_01";
        public JobPoint Shop=new JobPoint{Name="Oficina",X=542.1745f,Y=-187.7679f,Z=54.50751f};
        public JobPoint Spawn=new JobPoint{Name="Chegada do cliente",X=514.5953f,Y=-279.2764f,Z=46.28008f,Heading=350};
        public JobPoint Bay=new JobPoint{Name="Vaga de reparo",X=540.6617f,Y=-188.0396f,Z=53.80798f,Heading=92};
        public JobPoint Exit=new JobPoint{Name="Saida do cliente",X=542f,Y=-260f,Z=54.3f};
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public List<string> Models=new List<string>{"asea","primo","stanier","ingot","emperor","emperor2","intruder","premier","asterope","fugitive","surge","tailgater","washington","schafter2","oracle","oracle2","superd","glendale","warrener","blista","dilettante","issi2","panto","prairie","rhapsody","cogcabrio","exemplar","f620","felon","felon2","jackal","sentinel","sentinel2","zion","zion2","baller","baller2","bjxl","cavalcade","cavalcade2","dubsta","dubsta2","fq2","granger","gresley","habanero","huntley","landstalker","mesa","patriot","radi","rocoto","seminole","serrano","blade","buccaneer","dominator","dukes","faction","gauntlet","phoenix","picador","ruiner","sabregt","stalion","vigero","virgo","voodoo","alpha","banshee","buffalo","buffalo2","carbonizzare","comet2","coquette","elegy2","feltzer2","furoregt","fusilade","futo","jester","khamelion","kuruma","massacro","ninef","ninef2","penumbra","rapidgt","rapidgt2","schwarzer","sultan","surano","bison","bobcatxl","rebel","rebel2","sadler","sandking","sandking2","burrito","burrito3","minivan","paradise","pony","rumpo","speedo","surfer","youga","btype","casco","coquette2","manana","monroe","pigalle","stinger","stingergt","tornado","tornado2","tornado3","ztype"};
        [JsonIgnore] public bool Valid=>Goal>=1 && Goal<=10 && RequiredHits>=1 && RequiredHits<=10 && BoostMinutes>=1 && BoostMinutes<=180 && ReturnRadius>=2 && ReturnRadius<=12 && ArrivalSeconds>=30 && ArrivalSeconds<=180 && DepartureSeconds>=15 && DepartureSeconds<=90 && GangPresenceRules.ModelName(DriverModel) && Shop?.Valid==true && Spawn?.Valid==true && Bay?.Valid==true && Exit?.Valid==true && DeliveryRoute.Distance(Shop,Bay)<=20 && DeliveryRoute.Distance(Shop,Spawn)>=20 && DeliveryRoute.Distance(Shop,Spawn)<=500 && DeliveryRoute.Distance(Shop,Exit)>=20 && DeliveryRoute.Distance(Shop,Exit)<=500 && Models!=null && Models.Count>=1 && Models.Count<=160 && Models.All(GangPresenceRules.ModelName) && Models.Distinct(StringComparer.OrdinalIgnoreCase).Count()==Models.Count;
    }
    public sealed class GangMechanicAttempt : ExtensibleData
    {
        public string Id,Gang;public GangTurfDefinition Area;public GangMechanicOptions Options;public List<string> Models=new List<string>();public int Repaired;
        [JsonIgnore] public bool Ready=>Options?.Valid==true && Repaired==Options.Goal;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && !GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area?.Valid==true && Area.Id==2 && Options?.Valid==true && Models!=null && Models.Count==Options.Goal && Models.All(m=>Options.Models.Contains(m)) && Repaired>=0 && Repaired<=Options.Goal;
    }
    public sealed class GangMechanicAccount : ExtensibleData
    {
        public string Gang;public int Area=2;public long Completed,StartedUtcTicks,ExpiresUtcTicks;
        [JsonIgnore] public bool Valid=>!GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area==2 && Completed>0 && StartedUtcTicks>0 && ExpiresUtcTicks<=DateTime.MaxValue.Ticks && ExpiresUtcTicks>StartedUtcTicks && ExpiresUtcTicks-StartedUtcTicks<=TimeSpan.FromMinutes(180).Ticks;
    }
    public sealed class GangMechanicData : ExtensibleData
    {
        public GangMechanicAttempt Pending;public List<GangMechanicAccount> Accounts=new List<GangMechanicAccount>();public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>(Pending==null || Pending.Valid) && Accounts!=null && Accounts.Count<=640 && Accounts.All(a=>a?.Valid==true) && Accounts.Select(a=>a.Gang).Distinct(StringComparer.OrdinalIgnoreCase).Count()==Accounts.Count && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=120) && Receipts.Distinct().Count()==Receipts.Count;
    }
    // Skill progress is session only. Only completed vehicles enter the saved contract.
    public sealed class GangMechanicSkill
    {
        int last,since,cooldown=500;long phase;public int Hits{get;private set;}public int Misses{get;private set;}
        public double Cursor=>(Math.Sin(phase*.0035)+1)/2;
        public static bool Green(double cursor)=>!double.IsNaN(cursor) && cursor>=.458 && cursor<=.542;
        public GangMechanicSkill(int now){last=now;}
        public void Step(int now,bool working)
        {uint delta=unchecked((uint)(now-last));last=now;if(working && delta<=2000){phase=Math.Min(1000000000,phase+delta);since=(int)Math.Min(int.MaxValue,(long)since+delta);}}
        public bool Press(out bool hit)
        {hit=false;if(since<cooldown)return false;hit=Green(Cursor);if(hit){if(Hits==int.MaxValue)return false;Hits++;}else{if(Misses==int.MaxValue)return false;Misses++;}phase=0;since=0;cooldown=400;return true;}
    }
    public static class GangMechanicTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        static bool Id(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=80;
        public static bool Blocked(WalletData w)=>w?.GangMechanic!=null && (!w.GangMechanic.Valid || w.GangMechanic.Pending!=null);
        public static bool OtherFree(WalletData w)=>w!=null && !GangDockTransactions.Blocked(w) && !GangChauffeurTransactions.Blocked(w) && !GangTheatreTransactions.Blocked(w) && !GangMowingTransactions.Blocked(w) && !GangPopUpTransactions.Blocked(w) && !GangMedicalTransactions.Blocked(w) && !GangCashRunTransactions.Blocked(w) && !GangTowTransactions.Blocked(w) && !GangPromotionTransactions.Blocked(w) && (w.GangBulk==null || w.GangBulk.Valid && w.GangBulk.Pending==null) && (w.GangRaid==null || w.GangRaid.Valid && w.GangRaid.Pending==null) && (w.GangBusiness==null || w.GangBusiness.Valid && w.GangBusiness.Supply==null);
        public static GangMechanicAccount Account(GangMechanicData b,string gang)=>b?.Valid==true?b.Accounts.FirstOrDefault(a=>GangCrewRules.SameGang(a.Gang,gang)):null;
        public static int Multiplier(GangMechanicData b,string gang,int area,DateTime utc)
        {var a=Account(b,gang);return area==2 && utc.Kind==DateTimeKind.Utc && a!=null && utc.Ticks>=a.StartedUtcTicks && utc.Ticks<a.ExpiresUtcTicks?2:1;}
        static bool Apply(WalletData w,string receipt,Func<GangMechanicData,bool> mutate,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || w.GangMechanic!=null && !w.GangMechanic.Valid)return false;
            var old=w.GangMechanic;if(old?.Receipts.Contains(receipt)==true)return true;var b=old==null?new GangMechanicData():Copy(old);
            if(!mutate(b) || !b.Valid)return false;b.Receipts.Add(receipt);if(b.Receipts.Count>256)b.Receipts.RemoveAt(0);
            try{w.GangMechanic=b;save();changed=true;return true;}catch{w.GangMechanic=old;throw;}
        }
        static bool Owns(GangState s,GangTurfDefinition area,GangMechanicAttempt p)=>p!=null && area?.Id==2 && GangBusinessTransactions.Owns(s,area) && GangCrewRules.SameGang(s.Gang,p.Gang);
        public static bool Begin(WalletData w,GangState s,GangTurfDefinition area,GangMechanicOptions c,IEnumerable<string> models,string id,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || area?.Id!=2 || !GangBusinessTransactions.Owns(s,area) || c?.Valid!=true || models==null || !OtherFree(w))return false;
            var chosen=models.Take(11).ToList();if(chosen.Count!=c.Goal || chosen.Any(m=>!c.Models.Contains(m)))return false;
            return Apply(w,id+":mechanic-begin",b=>{if(b.Pending!=null)return false;var a=Account(b,s.Gang);if(a?.Completed==long.MaxValue || a==null && b.Accounts.Count>=640)return false;b.Pending=new GangMechanicAttempt{Id=id,Gang=s.Gang,Area=Copy(area),Options=Copy(c),Models=chosen};return true;},save,out changed);
        }
        public static bool Repair(WalletData w,GangState s,GangTurfDefinition area,string id,int index,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || index<0 || index>=10 || !OtherFree(w) || !Owns(s,area,w?.GangMechanic?.Pending))return false;
            return Apply(w,id+":mechanic-repair:"+index,b=>{var p=b.Pending;if(p?.Id!=id || p.Ready || p.Repaired!=index)return false;p.Repaired++;return true;},save,out changed);
        }
        public static bool Cancel(WalletData w,string id,Action save,out bool changed)
        {changed=false;if(!Id(id))return false;return Apply(w,id+":mechanic-cancel",b=>{if(b.Pending?.Id!=id)return false;b.Pending=null;return true;},save,out changed);}
        public static bool Complete(WalletData w,GangState s,GangTurfDefinition area,string id,DateTime utc,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || utc.Kind!=DateTimeKind.Utc || utc.Ticks==0 || area?.Id!=2 || !GangBusinessTransactions.Owns(s,area) || !OtherFree(w))return false;
            return Apply(w,id+":mechanic-complete",b=>{var p=b.Pending;if(p?.Id!=id || !p.Ready || !Owns(s,area,p) || utc.Ticks>DateTime.MaxValue.Ticks-TimeSpan.FromMinutes(p.Options.BoostMinutes).Ticks)return false;
                var a=Account(b,p.Gang);if(a==null){if(b.Accounts.Count>=640)return false;a=new GangMechanicAccount{Gang=p.Gang};b.Accounts.Add(a);}if(a.Completed==long.MaxValue)return false;
                a.Completed++;a.StartedUtcTicks=utc.Ticks;a.ExpiresUtcTicks=utc.AddMinutes(p.Options.BoostMinutes).Ticks;b.Pending=null;return true;},save,out changed);
        }
    }
}
