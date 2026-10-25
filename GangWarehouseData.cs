using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangWarehouseOptions : ExtensibleData
    {
        public float Capacity=30;public int Radius=12,SaleRadius=45;
        [JsonIgnore] public bool Valid=>!float.IsNaN(Capacity) && !float.IsInfinity(Capacity) && Capacity>=1 && Capacity<=1000 && Radius>=5 && Radius<=30 && SaleRadius>=20 && SaleRadius<=100;
    }
    public sealed class GangWarehouseAccount : ExtensibleData
    {
        public string Gang;public int Area;
        public Dictionary<string,int> Stock=new Dictionary<string,int>();
        public Dictionary<string,long> Deposited=new Dictionary<string,long>(),Withdrawn=new Dictionary<string,long>(),Sold=new Dictionary<string,long>(),Supplied=new Dictionary<string,long>(),Raided=new Dictionary<string,long>();
        static bool Keys<T>(Dictionary<string,T> d)=>d!=null && d.Count<=40 && d.Keys.All(k=>!string.IsNullOrWhiteSpace(k) && k.Length<=80);
        [JsonIgnore] public bool Valid=>!GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area>0 && Keys(Stock) && Keys(Deposited) && Keys(Withdrawn) && Keys(Sold) && Keys(Supplied) && Keys(Raided) && Stock.Values.All(n=>n>=0 && n<=999) && Deposited.Values.All(n=>n>=0) && Withdrawn.Values.All(n=>n>=0) && Sold.Values.All(n=>n>=0) && Supplied.Values.All(n=>n>=0) && Raided.Values.All(n=>n>=0) && Stock.Keys.Concat(Raided.Keys).Concat(Supplied.Keys).Concat(Deposited.Keys).Concat(Withdrawn.Keys).Concat(Sold.Keys).Distinct().All(k=>(decimal)Get(Deposited,k)+Get(Supplied,k)+Get(Raided,k)-Get(Withdrawn,k)-Get(Sold,k)==Get(Stock,k));
        public static long Get(Dictionary<string,long> d,string k)=>d.TryGetValue(k,out var n)?n:0;
        public static int Get(Dictionary<string,int> d,string k)=>d.TryGetValue(k,out var n)?n:0;
    }
    public sealed class GangWarehouseData : ExtensibleData
    {
        public List<GangWarehouseAccount> Accounts=new List<GangWarehouseAccount>();public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>Accounts!=null && Accounts.Count<=640 && Accounts.All(a=>a!=null && a.Valid) && Accounts.GroupBy(a=>a.Area+"|"+a.Gang,StringComparer.OrdinalIgnoreCase).All(g=>g.Count()==1) && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=180) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public static class GangWarehouseTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        public static GangWarehouseAccount Account(GangWarehouseData b,string gang,int area)=>b!=null && b.Valid?b.Accounts.FirstOrDefault(a=>a.Area==area && GangCrewRules.SameGang(a.Gang,gang)):null;
        public static double Weight(GangWarehouseAccount a,DealerOptions c)=>a==null?0:a.Stock.Sum(p=>p.Value*(double)(c.Products.Find(v=>v.Id==p.Key)?.Weight??1));
        public static int KnownUnits(GangWarehouseData b,string gang,int area,DealerOptions c){var a=Account(b,gang,area);return a!=null && c!=null && c.Valid?a.Stock.Where(p=>c.Products.Any(v=>v.Id==p.Key)).Sum(p=>p.Value):0;}
        static void Receipt(GangWarehouseData b,string id){b.Receipts.Add(id);if(b.Receipts.Count>256)b.Receipts.RemoveAt(0);}
        public static bool Transfer(WalletData w,GangState s,GangTurfDefinition area,DealerOptions c,GangWarehouseOptions o,string product,int count,bool deposit,string id,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || !GangBusinessTransactions.Owns(s,area) || w.GangBusiness?.Supply!=null || w.GangRaid?.Pending!=null || c==null || !c.Valid || o==null || !o.Valid || count<1 || count>20 || string.IsNullOrWhiteSpace(id) || id.Length>80 || !c.Products.Any(p=>p.Id==product) || w.Dealer!=null && !w.Dealer.Valid || w.GangWarehouse!=null && !w.GangWarehouse.Valid)return false;
            string receipt=id+(deposit?":warehouse-in":":warehouse-out");if(w.GangWarehouse?.Receipts.Contains(receipt)==true)return true;
            var original=w.GangWarehouse;var dealer=w.Dealer;var book=original==null?new GangWarehouseData():Copy(original);var d=dealer==null?new DealerData():Copy(dealer);
            var a=Account(book,s.Gang,area.Id);if(a==null){if(!deposit || book.Accounts.Count>=640)return false;a=new GangWarehouseAccount{Gang=s.Gang,Area=area.Id};book.Accounts.Add(a);}
            int personal=GangWarehouseAccount.Get(d.Stock,product),stored=GangWarehouseAccount.Get(a.Stock,product);
            if(deposit)
            {
                long n=GangWarehouseAccount.Get(a.Deposited,product);if(personal<count || stored>999-count || n>long.MaxValue-count)return false;
                d.Stock[product]=personal-count;a.Stock[product]=stored+count;a.Deposited[product]=n+count;
                if(Weight(a,c)>o.Capacity+.0001)return false;
            }
            else
            {
                long n=GangWarehouseAccount.Get(a.Withdrawn,product);if(stored<count || personal>999-count || n>long.MaxValue-count)return false;
                d.Stock[product]=personal+count;a.Stock[product]=stored-count;a.Withdrawn[product]=n+count;
                if(DealerTransactions.Weight(d,c)>c.Capacity+.0001)return false;
            }
            if(!book.Valid || !d.Valid)return false;Receipt(book,receipt);
            try{w.GangWarehouse=book;w.Dealer=d;save();changed=true;return true;}catch{w.GangWarehouse=original;w.Dealer=dealer;throw;}
        }
        public static bool Sale(WalletData w,GangState s,GangTurfDefinition area,DealerOptions c,string product,int count,string zone,bool premium,int unit,bool last,string id,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || !GangBusinessTransactions.Owns(s,area) || w.GangBusiness?.Supply!=null || w.GangRaid?.Pending!=null || c==null || !c.Valid || !c.Products.Any(p=>p.Id==product) || count<1 || count>5 || string.IsNullOrWhiteSpace(id) || id.Length>160 || w.GangWarehouse==null || !w.GangWarehouse.Valid || w.Dealer!=null && !w.Dealer.Valid || w.GangIllicit!=null && !w.GangIllicit.Valid)return false;
            if(w.GangWarehouse.Receipts.Contains(id) || w.Dealer?.Receipts.Contains(id)==true || w.GangIllicit?.Receipts.Contains(id)==true)return true;
            var original=w.GangWarehouse;var book=Copy(original);var a=Account(book,s.Gang,area.Id);if(a==null)return false;
            long sold=GangWarehouseAccount.Get(a.Sold,product);if(sold>long.MaxValue-count)return false;
            var dealer=w.Dealer;var dirty=w.GangIllicit;var d=dealer==null?new DealerData():Copy(dealer);d.Stock=new Dictionary<string,int>(a.Stock);
            var shadow=new WalletData{Bank=w.Bank,Dealer=d,GangIllicit=dirty,GangBusiness=w.GangBusiness};
            if(!GangIllicitTransactions.Sale(shadow,s,area,c,product,count,zone,premium,unit,last,id,()=>{},out bool applied) || !applied)return false;
            a.Stock=shadow.Dealer.Stock;a.Sold[product]=sold+count;if(!book.Valid)return false;Receipt(book,id);
            // Keep personal packages exactly where they were. Only statistics/reputation and
            // dirty proceeds from the existing sale rules accompany the warehouse debit.
            shadow.Dealer.Stock=dealer?.Stock??new Dictionary<string,int>();
            try{w.GangWarehouse=book;w.Dealer=shadow.Dealer;w.GangIllicit=shadow.GangIllicit;save();changed=true;return true;}
            catch{w.GangWarehouse=original;w.Dealer=dealer;w.GangIllicit=dirty;throw;}
        }
    }
}
