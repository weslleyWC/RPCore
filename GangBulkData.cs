using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public enum GangBulkKind { Drugs=0,Arms=1 }
    public sealed class GangBulkOptions : ExtensibleData
    {
        public int MaxValue=75000,SecondsLimit=1200,HandoverSeconds=3;
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public List<int> Quantities=new List<int>{25,50,100,200,500};
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public List<JobPoint> Sites=new List<JobPoint>{
            new JobPoint{Name="Encontro oeste",X=-1919.756f,Y=2052.753f,Z=140.5457f,Heading=257.0092f},
            new JobPoint{Name="Encontro Sandy Shores",X=1624.158f,Y=3876.18f,Z=32.75102f,Heading=245.2432f},
            new JobPoint{Name="Encontro Grapeseed",X=2355.005f,Y=4873.825f,Z=41.3113f,Heading=44.09565f},
            new JobPoint{Name="Encontro Paleto",X=120.0075f,Y=6651.063f,Z=31.11201f,Heading=135.8579f},
            new JobPoint{Name="Encontro Vinewood",X=-411.7521f,Y=1236.108f,Z=325.1439f,Heading=231.1451f},
            new JobPoint{Name="Encontro deserto",X=1221.537f,Y=1829.018f,Z=78.83425f,Heading=26.25417f}};
        [JsonIgnore] public bool Valid=>MaxValue>=1 && MaxValue<=75000 && SecondsLimit>=120 && SecondsLimit<=1800 && HandoverSeconds>=2 && HandoverSeconds<=10 && Quantities!=null && Quantities.Count>=1 && Quantities.Count<=10 && Quantities.All(q=>q>=1 && q<=500) && Quantities.Distinct().Count()==Quantities.Count && Sites!=null && Sites.Count>=1 && Sites.Count<=30 && Sites.All(p=>p!=null && p.Valid) && Sites.Select(p=>p.X+"|"+p.Y+"|"+p.Z).Distinct().Count()==Sites.Count;
    }
    public sealed class GangBulkAttempt : ExtensibleData
    {
        public GangBulkKind Kind;public int ArmsLimit;
        public string Id,Gang,Buyer,Product,Model,InitialAttitude="Neutral";public int Units,Value,RelationRoll;
        public GangTurfDefinition Area;public JobPoint Site;public GangBulkOptions Options;public bool Loaded,Ready;
        [JsonIgnore] public bool Valid=>(Kind==GangBulkKind.Drugs || Kind==GangBulkKind.Arms && ArmsLimit>=1 && ArmsLimit<=150 && (Product=="ammo"?(Units+99)/100:Units)<=ArmsLimit) && !string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && GangDiplomacyRules.Pair(Gang,Buyer) && !string.IsNullOrWhiteSpace(Product) && Product.Length<=80 && GangPresenceRules.ModelName(Model) && GangDiplomacyRules.Known(InitialAttitude) && Units>=1 && Units<=500 && Value>=1 && Value<=75000 && RelationRoll>=0 && RelationRoll<100 && Area!=null && Area.Valid && Site!=null && Site.Valid && Options!=null && Options.Valid && Value<=Options.MaxValue && Options.Quantities.Contains(Units) && (!Ready || Loaded);
    }
    public sealed class GangBulkData : ExtensibleData
    {
        public GangBulkAttempt Pending;public int Attempts,Completed,Cancelled;public long Revenue;
        public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>Attempts>=0 && Completed>=0 && Cancelled>=0 && Revenue>=0 && (long)Completed+Cancelled<=Attempts && (Pending==null || Pending.Valid && (long)Completed+Cancelled<Attempts) && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=120) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public static class GangBulkTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        static bool Id(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=80;
        // Locked in place until confirmation: no escrow debit/refund or hidden stock creation.
        public static bool Blocked(WalletData w)=>GangDockTransactions.Blocked(w) || GangChauffeurTransactions.Blocked(w) || GangTheatreTransactions.Blocked(w) || GangMowingTransactions.Blocked(w) || GangPopUpTransactions.Blocked(w) || GangMedicalTransactions.Blocked(w) || GangCashRunTransactions.Blocked(w) || GangTowTransactions.Blocked(w) || GangMechanicTransactions.Blocked(w) || GangPromotionTransactions.Blocked(w) || w?.GangBulk!=null && (!w.GangBulk.Valid || w.GangBulk.Pending!=null);
        public static int Quote(DealerOptions c,string product,int units)
        {var p=c?.Valid==true?c.Products.Find(v=>v.Id==product):null;long n=p==null || units<1 || units>500?0:(long)p.Sell*units*3/2;return n>=1 && n<=75000?(int)n:0;}
        static bool Apply(WalletData w,string receipt,Func<GangBulkData,bool> mutate,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || w.GangBulk!=null && !w.GangBulk.Valid)return false;
            var old=w.GangBulk;if(old?.Receipts.Contains(receipt)==true)return true;var next=old==null?new GangBulkData():Copy(old);
            if(!mutate(next) || !next.Valid)return false;next.Receipts.Add(receipt);if(next.Receipts.Count>256)next.Receipts.RemoveAt(0);
            try{w.GangBulk=next;save();changed=true;return true;}catch{w.GangBulk=old;throw;}
        }
        public static bool Begin(WalletData w,GangState s,GangBulkAttempt p,DealerOptions c,Action save,out bool changed)
        {
            changed=false;if(w==null || GangDockTransactions.Blocked(w) || GangChauffeurTransactions.Blocked(w) || GangTheatreTransactions.Blocked(w) || GangMowingTransactions.Blocked(w) || GangPopUpTransactions.Blocked(w) || GangMedicalTransactions.Blocked(w) || GangCashRunTransactions.Blocked(w) || GangTowTransactions.Blocked(w) || GangMechanicTransactions.Blocked(w) || GangPromotionTransactions.Blocked(w) || p?.Valid!=true || p.Kind!=GangBulkKind.Drugs || p.Loaded || p.Ready || !GangBusinessTransactions.Owns(s,p.Area) || !GangCrewRules.SameGang(s.Gang,p.Gang) || w.GangBusiness!=null && (!w.GangBusiness.Valid || w.GangBusiness.Supply!=null) || w.GangRaid!=null && (!w.GangRaid.Valid || w.GangRaid.Pending!=null) || w.GangWarehouse?.Valid!=true || w.GangIllicit!=null && !w.GangIllicit.Valid || w.GangDiplomacy!=null && !w.GangDiplomacy.Valid || Quote(c,p.Product,p.Units)!=p.Value || p.Value>p.Options.MaxValue)return false;
            var a=GangWarehouseTransactions.Account(w.GangWarehouse,p.Gang,p.Area.Id);if(a==null || GangWarehouseAccount.Get(a.Stock,p.Product)<p.Units)return false;
            return Apply(w,p.Id+":bulk-begin",b=>{if(b.Pending!=null || b.Attempts==int.MaxValue)return false;b.Pending=Copy(p);b.Attempts++;return true;},save,out changed);
        }
        public static bool Load(WalletData w,string id,Action save,out bool changed)
        {changed=false;if(!Id(id))return false;return Apply(w,id+":bulk-load",b=>{if(b.Pending?.Id!=id || b.Pending.Loaded)return false;b.Pending.Loaded=true;return true;},save,out changed);}
        public static bool Ready(WalletData w,string id,Action save,out bool changed)
        {changed=false;if(!Id(id))return false;return Apply(w,id+":bulk-ready",b=>{if(b.Pending?.Id!=id || !b.Pending.Loaded || b.Pending.Ready)return false;b.Pending.Ready=true;return true;},save,out changed);}
        public static bool Restart(WalletData w,string id,Action save,out bool changed)
        {changed=false;if(!Id(id))return false;return Apply(w,Guid.NewGuid().ToString("N")+":bulk-restart",b=>{if(b.Pending?.Id!=id || b.Pending.Ready)return false;b.Pending.Loaded=false;b.Receipts.RemoveAll(r=>r==id+":bulk-load");return true;},save,out changed);}
        public static bool Cancel(WalletData w,string id,Action save,out bool changed)
        {changed=false;if(!Id(id))return false;return Apply(w,id+":bulk-cancel",b=>{if(b.Pending?.Id!=id || b.Cancelled==int.MaxValue)return false;b.Pending=null;b.Cancelled++;return true;},save,out changed);}
        public static bool Complete(WalletData w,GangState s,GangTurfDefinition area,DealerOptions c,string id,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || !Id(id) || !GangBusinessTransactions.Owns(s,area) || w.GangBulk?.Valid!=true || w.GangWarehouse?.Valid!=true || w.GangIllicit!=null && !w.GangIllicit.Valid || w.GangDiplomacy!=null && !w.GangDiplomacy.Valid || c?.Valid!=true || w.GangBusiness?.Supply!=null || w.GangRaid?.Pending!=null)return false;
            string receipt=id+":bulk-complete";if(w.GangBulk.Receipts.Contains(receipt))return true;
            var old=w.GangBulk;var stock=w.GangWarehouse;var dirty=w.GangIllicit;var dip=w.GangDiplomacy;var book=Copy(old);var p=book.Pending;
            if(p==null || p.Kind!=GangBulkKind.Drugs || p.Id!=id || !p.Ready || p.Area.Id!=area.Id || !GangCrewRules.SameGang(p.Gang,s.Gang) || !c.Products.Any(v=>v.Id==p.Product) || book.Completed==int.MaxValue || book.Revenue>long.MaxValue-p.Value)return false;
            var storage=Copy(stock);var a=GangWarehouseTransactions.Account(storage,p.Gang,p.Area.Id);
            if(a==null || GangWarehouseAccount.Get(a.Stock,p.Product)<p.Units || GangWarehouseAccount.Get(a.Sold,p.Product)>long.MaxValue-p.Units)return false;
            a.Stock[p.Product]-=p.Units;a.Sold[p.Product]=GangWarehouseAccount.Get(a.Sold,p.Product)+p.Units;
            var income=dirty==null?new GangIllicitData():Copy(dirty);var d=GangIllicitTransactions.Account(income,p.Gang,p.Area.Id);
            if(d==null){if(income.Accounts.Count>=640)return false;d=new GangIllicitAccount{Gang=p.Gang,Area=p.Area.Id};income.Accounts.Add(d);}
            if(d.Dirty>int.MaxValue-p.Value || d.Proceeds>long.MaxValue-p.Value)return false;d.Dirty+=p.Value;d.Proceeds+=p.Value;
            var diplomacy=GangDiplomacyRules.Copy(dip);var r=GangDiplomacyRules.Ensure(diplomacy,p.Gang,p.Buyer,p.InitialAttitude);
            if(r==null || r.BulkDeals==long.MaxValue)return false;r.BulkDeals++;if(p.RelationRoll<30)r.Attitude=GangDiplomacyRules.Improve(r.Attitude);
            book.Pending=null;book.Completed++;book.Revenue+=p.Value;
            book.Receipts.Add(receipt);if(book.Receipts.Count>256)book.Receipts.RemoveAt(0);storage.Receipts.Add(receipt);if(storage.Receipts.Count>256)storage.Receipts.RemoveAt(0);income.Receipts.Add(receipt);if(income.Receipts.Count>256)income.Receipts.RemoveAt(0);GangDiplomacyRules.Receipt(diplomacy,receipt);
            if(!book.Valid || !storage.Valid || !income.Valid || !diplomacy.Valid)return false;
            try{w.GangBulk=book;w.GangWarehouse=storage;w.GangIllicit=income;w.GangDiplomacy=diplomacy;save();changed=true;return true;}
            catch{w.GangBulk=old;w.GangWarehouse=stock;w.GangIllicit=dirty;w.GangDiplomacy=dip;throw;}
        }
    }
}
