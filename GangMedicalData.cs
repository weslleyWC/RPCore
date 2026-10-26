using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangMedicalOptions : ExtensibleData
    {
        public int BoostMinutes=60,StartRadius=8,HandoverMilliseconds=1500;
        public string DoctorModel="s_m_m_doctor_01",DocumentModel="prop_cs_documents_01";
        public JobPoint Doctor=new JobPoint{Name="Coleta de documentos",X=-940.9167f,Y=-337.9953f,Z=38.97752f,Heading=109.513f};
        public JobPoint Destination=new JobPoint{Name="Centro medico",X=342.6898f,Y=-1397.448f,Z=32.50928f};
        [JsonIgnore] public bool Valid=>BoostMinutes>=1 && BoostMinutes<=180 && StartRadius>=3 && StartRadius<=15 && HandoverMilliseconds>=500 && HandoverMilliseconds<=5000 && GangPresenceRules.ModelName(DoctorModel) && GangPresenceRules.ModelName(DocumentModel) && Doctor?.Valid==true && Destination?.Valid==true && DeliveryRoute.Distance(Doctor,Destination)>=20;
    }
    public sealed class GangMedicalAttempt : ExtensibleData
    {
        public string Id,Gang;public GangTurfDefinition Area;public GangMedicalOptions Options;public bool Collected,Delivered;
        [JsonIgnore] public bool Ready=>Delivered;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && !GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area?.Valid==true && Area.Id==6 && Options?.Valid==true && (!Delivered || Collected);
    }
    public sealed class GangMedicalAccount : ExtensibleData
    {
        public string Gang;public int Area=6;public long Completed,StartedUtcTicks,ExpiresUtcTicks;
        [JsonIgnore] public bool Valid=>!GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area==6 && Completed>0 && StartedUtcTicks>0 && ExpiresUtcTicks<=DateTime.MaxValue.Ticks && ExpiresUtcTicks>StartedUtcTicks && ExpiresUtcTicks-StartedUtcTicks<=TimeSpan.FromMinutes(180).Ticks;
    }
    public sealed class GangMedicalData : ExtensibleData
    {
        public GangMedicalAttempt Pending;public List<GangMedicalAccount> Accounts=new List<GangMedicalAccount>();public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>(Pending==null || Pending.Valid) && Accounts!=null && Accounts.Count<=640 && Accounts.All(a=>a?.Valid==true) && Accounts.Select(a=>a.Gang).Distinct(StringComparer.OrdinalIgnoreCase).Count()==Accounts.Count && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=120) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public static class GangMedicalRules
    {
        public static bool Interact(bool safe,bool foot,bool expectedStage,float distance)=>safe && foot && expectedStage && GangPresenceRules.Finite(distance) && distance>=0 && distance<=2;
    }
    public static class GangMedicalTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        static bool Id(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=80;
        public static bool Blocked(WalletData w)=>w?.GangMedical!=null && (!w.GangMedical.Valid || w.GangMedical.Pending!=null);
        public static bool OtherFree(WalletData w)=>w!=null && !GangDockTransactions.Blocked(w) && !GangChauffeurTransactions.Blocked(w) && !GangTheatreTransactions.Blocked(w) && !GangMowingTransactions.Blocked(w) && !GangPopUpTransactions.Blocked(w) && !GangCashRunTransactions.Blocked(w) && !GangTowTransactions.Blocked(w) && !GangMechanicTransactions.Blocked(w) && !GangPromotionTransactions.Blocked(w) && (w.GangBulk==null || w.GangBulk.Valid && w.GangBulk.Pending==null) && (w.GangRaid==null || w.GangRaid.Valid && w.GangRaid.Pending==null) && (w.GangBusiness==null || w.GangBusiness.Valid && w.GangBusiness.Supply==null);
        public static GangMedicalAccount Account(GangMedicalData b,string gang)=>b?.Valid==true?b.Accounts.FirstOrDefault(a=>GangCrewRules.SameGang(a.Gang,gang)):null;
        public static int Multiplier(GangMedicalData b,string gang,int area,DateTime utc)
        {var a=Account(b,gang);return area==6 && utc.Kind==DateTimeKind.Utc && a!=null && utc.Ticks>=a.StartedUtcTicks && utc.Ticks<a.ExpiresUtcTicks?2:1;}
        static bool Apply(WalletData w,string receipt,Func<GangMedicalData,bool> mutate,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || w.GangMedical!=null && !w.GangMedical.Valid)return false;
            var old=w.GangMedical;if(old?.Receipts.Contains(receipt)==true)return true;var b=old==null?new GangMedicalData():Copy(old);
            if(!mutate(b) || !b.Valid)return false;b.Receipts.Add(receipt);if(b.Receipts.Count>256)b.Receipts.RemoveAt(0);
            try{w.GangMedical=b;save();changed=true;return true;}catch{w.GangMedical=old;throw;}
        }
        static bool Owns(GangState s,GangTurfDefinition area,GangMedicalAttempt p)=>p!=null && area?.Id==6 && GangBusinessTransactions.Owns(s,area) && GangCrewRules.SameGang(s.Gang,p.Gang);
        public static bool Begin(WalletData w,GangState s,GangTurfDefinition area,GangMedicalOptions c,string id,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || area?.Id!=6 || !GangBusinessTransactions.Owns(s,area) || c?.Valid!=true || !OtherFree(w))return false;
            return Apply(w,id+":medical-begin",b=>{if(b.Pending!=null)return false;var a=Account(b,s.Gang);if(a?.Completed==long.MaxValue || a==null && b.Accounts.Count>=640)return false;b.Pending=new GangMedicalAttempt{Id=id,Gang=s.Gang,Area=Copy(area),Options=Copy(c)};return true;},save,out changed);
        }
        public static bool Collect(WalletData w,GangState s,GangTurfDefinition area,string id,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || !OtherFree(w) || !Owns(s,area,w?.GangMedical?.Pending))return false;
            return Apply(w,id+":medical-collect",b=>{var p=b.Pending;if(p?.Id!=id || p.Collected || p.Delivered)return false;p.Collected=true;return true;},save,out changed);
        }
        public static bool Deliver(WalletData w,GangState s,GangTurfDefinition area,string id,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || !OtherFree(w) || !Owns(s,area,w?.GangMedical?.Pending))return false;
            return Apply(w,id+":medical-deliver",b=>{var p=b.Pending;if(p?.Id!=id || !p.Collected || p.Delivered)return false;p.Delivered=true;return true;},save,out changed);
        }
        public static bool Cancel(WalletData w,string id,Action save,out bool changed)
        {changed=false;if(!Id(id))return false;return Apply(w,id+":medical-cancel",b=>{if(b.Pending?.Id!=id)return false;b.Pending=null;return true;},save,out changed);}
        public static bool Complete(WalletData w,GangState s,GangTurfDefinition area,string id,DateTime utc,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || utc.Kind!=DateTimeKind.Utc || utc.Ticks==0 || area?.Id!=6 || !GangBusinessTransactions.Owns(s,area) || !OtherFree(w))return false;
            return Apply(w,id+":medical-complete",b=>{var p=b.Pending;if(p?.Id!=id || !p.Ready || !Owns(s,area,p) || utc.Ticks>DateTime.MaxValue.Ticks-TimeSpan.FromMinutes(p.Options.BoostMinutes).Ticks)return false;
                var a=Account(b,p.Gang);if(a==null){if(b.Accounts.Count>=640)return false;a=new GangMedicalAccount{Gang=p.Gang};b.Accounts.Add(a);}if(a.Completed==long.MaxValue)return false;
                a.Completed++;a.StartedUtcTicks=utc.Ticks;a.ExpiresUtcTicks=utc.AddMinutes(p.Options.BoostMinutes).Ticks;b.Pending=null;return true;},save,out changed);
        }
    }
}
