using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangArmsProduct
    {
        public string Id,Name;public int Buy,Sell;
    }
    public sealed class GangArmsOptions : ExtensibleData
    {
        public int MaxStock=999;
        public GangBulkOptions Delivery=new GangBulkOptions{Quantities=new List<int>{1,5,10,25,50}};
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public List<GangArmsProduct> Products=new List<GangArmsProduct>{
            new GangArmsProduct{Id="pistol",Name="Pistolas",Buy=900,Sell=1200},new GangArmsProduct{Id="smg",Name="SMGs",Buy=1800,Sell=2500},
            new GangArmsProduct{Id="rifle",Name="Fuzis",Buy=3300,Sell=4500},new GangArmsProduct{Id="shotgun",Name="Escopetas",Buy=2400,Sell=3200},new GangArmsProduct{Id="ammo",Name="Municao",Buy=2,Sell=3}};
        [JsonIgnore] public bool Valid=>MaxStock>=1 && MaxStock<=999 && Delivery?.Valid==true && Products!=null && Products.Count>=1 && Products.Count<=20 && Products.All(p=>p!=null && GangPresenceRules.ModelName(p.Id) && p.Id.Length<=80 && !string.IsNullOrWhiteSpace(p.Name) && p.Name.Length<=100 && p.Buy>=1 && p.Buy<=100000 && p.Sell>=p.Buy && p.Sell<=100000) && Products.Select(p=>p.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count()==Products.Count;
    }
    public sealed class GangArmsAccount : ExtensibleData
    {
        public string Gang;public int Area;
        public Dictionary<string,int> Stock=new Dictionary<string,int>();
        public Dictionary<string,long> Purchased=new Dictionary<string,long>(),Recovered=new Dictionary<string,long>(),Sold=new Dictionary<string,long>();
        public long Spent,Revenue;
        static bool Keys<T>(Dictionary<string,T> d)=>d!=null && d.Count<=40 && d.Keys.All(k=>!string.IsNullOrWhiteSpace(k) && k.Length<=80);
        [JsonIgnore] public bool Valid=>!GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area>0 && Keys(Stock) && Keys(Purchased) && Keys(Recovered) && Keys(Sold) && Stock.Values.All(n=>n>=0 && n<=999) && Purchased.Values.All(n=>n>=0) && Recovered.Values.All(n=>n>=0) && Sold.Values.All(n=>n>=0) && Spent>=0 && Revenue>=0 && Stock.Keys.Concat(Purchased.Keys).Concat(Recovered.Keys).Concat(Sold.Keys).Distinct().All(k=>(decimal)GangWarehouseAccount.Get(Purchased,k)+GangWarehouseAccount.Get(Recovered,k)-GangWarehouseAccount.Get(Sold,k)==GangWarehouseAccount.Get(Stock,k));
    }
    public sealed class GangArmsData : ExtensibleData
    {
        public List<GangArmsAccount> Accounts=new List<GangArmsAccount>();public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>Accounts!=null && Accounts.Count<=640 && Accounts.All(a=>a!=null && a.Valid) && Accounts.GroupBy(a=>a.Area+"|"+a.Gang,StringComparer.OrdinalIgnoreCase).All(g=>g.Count()==1) && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=120) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public static class GangArmsTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        static bool Id(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=80;
        static void Receipt(GangArmsData b,string r){b.Receipts.Add(r);if(b.Receipts.Count>256)b.Receipts.RemoveAt(0);}
        public static GangArmsAccount Account(GangArmsData b,string gang,int area)=>b?.Valid==true?b.Accounts.FirstOrDefault(a=>a.Area==area && GangCrewRules.SameGang(a.Gang,gang)):null;
        static bool Available(WalletData w,GangState s,GangTurfDefinition area,GangArmsOptions c,Action save)=>w!=null && w.Bank>=0 && save!=null && GangBusinessTransactions.Owns(s,area) && c?.Valid==true && (w.GangArms==null || w.GangArms.Valid) && (w.GangBusiness==null || w.GangBusiness.Valid && w.GangBusiness.Supply==null) && (w.GangRaid==null || w.GangRaid.Valid && w.GangRaid.Pending==null);
        public static int Quote(GangArmsOptions c,string product,int units)
        {var p=c?.Valid==true?c.Products.Find(v=>v.Id==product):null;long value=p==null || units<1 || units>500?0:(long)p.Sell*units;return value>=1 && value<=c.Delivery.MaxValue?(int)value:0;}
        public static bool Buy(WalletData w,GangState s,GangTurfDefinition area,GangArmsOptions c,string product,int units,string id,Action save,out bool changed)
        {
            changed=false;if(!Available(w,s,area,c,save) || !Id(id) || GangBulkTransactions.Blocked(w) || units<1 || units>500 || !c.Delivery.Quantities.Contains(units))return false;
            var item=c.Products.Find(p=>p.Id==product);if(item==null)return false;long cost=(long)item.Buy*units;
            string receipt=id+":arms-buy";var old=w.GangArms;if(old?.Receipts.Contains(receipt)==true)return true;if(cost>w.Bank)return false;var book=old==null?new GangArmsData():Copy(old);var a=Account(book,s.Gang,area.Id);
            if(a==null){if(book.Accounts.Count>=640)return false;a=new GangArmsAccount{Gang=s.Gang,Area=area.Id};book.Accounts.Add(a);}
            if(GangWarehouseAccount.Get(a.Stock,product)>c.MaxStock-units || GangWarehouseAccount.Get(a.Purchased,product)>long.MaxValue-units || a.Spent>long.MaxValue-cost)return false;
            a.Stock[product]=GangWarehouseAccount.Get(a.Stock,product)+units;a.Purchased[product]=GangWarehouseAccount.Get(a.Purchased,product)+units;a.Spent+=cost;Receipt(book,receipt);if(!book.Valid)return false;int bank=w.Bank;
            try{w.GangArms=book;w.Bank-=(int)cost;save();changed=true;return true;}catch{w.GangArms=old;w.Bank=bank;throw;}
        }
        public static bool BeginDelivery(WalletData w,GangState s,GangBulkAttempt p,GangArmsOptions c,Action save,out bool changed)
        {
            changed=false;if(p?.Valid!=true || GangChauffeurTransactions.Blocked(w) || GangTheatreTransactions.Blocked(w) || GangMowingTransactions.Blocked(w) || GangPopUpTransactions.Blocked(w) || GangMedicalTransactions.Blocked(w) || GangCashRunTransactions.Blocked(w) || GangTowTransactions.Blocked(w) || GangMechanicTransactions.Blocked(w) || GangPromotionTransactions.Blocked(w) || p.Kind!=GangBulkKind.Arms || p.Loaded || p.Ready || !Available(w,s,p.Area,c,save) || !GangCrewRules.SameGang(s.Gang,p.Gang) || Quote(c,p.Product,p.Units)!=p.Value || !c.Delivery.Quantities.Contains(p.Units) || w.GangBulk!=null && !w.GangBulk.Valid)return false;
            var a=Account(w.GangArms,p.Gang,p.Area.Id);if(a==null || GangWarehouseAccount.Get(a.Stock,p.Product)<p.Units)return false;
            var old=w.GangBulk;string receipt=p.Id+":bulk-begin";if(old?.Receipts.Contains(receipt)==true)return true;var book=old==null?new GangBulkData():Copy(old);
            if(book.Pending!=null || book.Attempts==int.MaxValue)return false;book.Pending=Copy(p);book.Attempts++;book.Receipts.Add(receipt);if(book.Receipts.Count>256)book.Receipts.RemoveAt(0);if(!book.Valid)return false;
            try{w.GangBulk=book;save();changed=true;return true;}catch{w.GangBulk=old;throw;}
        }
        public static bool CompleteDelivery(WalletData w,GangState s,GangTurfDefinition area,GangArmsOptions c,string id,Action save,out bool changed)
        {
            changed=false;if(!Available(w,s,area,c,save) || !Id(id) || w.GangArms?.Valid!=true || w.GangBulk?.Valid!=true || w.GangIllicit!=null && !w.GangIllicit.Valid)return false;
            string receipt=id+":arms-complete";if(w.GangArms.Receipts.Contains(receipt))return true;
            var old=w.GangBulk;var original=w.GangArms;var dirty=w.GangIllicit;var book=Copy(old);var p=book.Pending;
            if(p==null || p.Id!=id || GangChauffeurTransactions.Blocked(w) || GangTheatreTransactions.Blocked(w) || GangMowingTransactions.Blocked(w) || GangPopUpTransactions.Blocked(w) || GangMedicalTransactions.Blocked(w) || GangCashRunTransactions.Blocked(w) || GangTowTransactions.Blocked(w) || GangMechanicTransactions.Blocked(w) || GangPromotionTransactions.Blocked(w) || p.Kind!=GangBulkKind.Arms || !p.Ready || p.Area.Id!=area.Id || !GangCrewRules.SameGang(s.Gang,p.Gang) || !c.Products.Any(v=>v.Id==p.Product) || book.Completed==int.MaxValue || book.Revenue>long.MaxValue-p.Value)return false;
            var storage=Copy(original);var a=Account(storage,p.Gang,p.Area.Id);if(a==null || GangWarehouseAccount.Get(a.Stock,p.Product)<p.Units || GangWarehouseAccount.Get(a.Sold,p.Product)>long.MaxValue-p.Units || a.Revenue>long.MaxValue-p.Value)return false;
            a.Stock[p.Product]-=p.Units;a.Sold[p.Product]=GangWarehouseAccount.Get(a.Sold,p.Product)+p.Units;a.Revenue+=p.Value;
            var income=dirty==null?new GangIllicitData():Copy(dirty);var d=GangIllicitTransactions.Account(income,p.Gang,p.Area.Id);if(d==null){if(income.Accounts.Count>=640)return false;d=new GangIllicitAccount{Gang=p.Gang,Area=p.Area.Id};income.Accounts.Add(d);}
            if(d.Dirty>int.MaxValue-p.Value || d.Proceeds>long.MaxValue-p.Value)return false;d.Dirty+=p.Value;d.Proceeds+=p.Value;
            book.Pending=null;book.Completed++;book.Revenue+=p.Value;book.Receipts.Add(receipt);if(book.Receipts.Count>256)book.Receipts.RemoveAt(0);Receipt(storage,receipt);income.Receipts.Add(receipt);if(income.Receipts.Count>256)income.Receipts.RemoveAt(0);
            if(!book.Valid || !storage.Valid || !income.Valid)return false;
            try{w.GangBulk=book;w.GangArms=storage;w.GangIllicit=income;save();changed=true;return true;}catch{w.GangBulk=old;w.GangArms=original;w.GangIllicit=dirty;throw;}
        }
    }
}
