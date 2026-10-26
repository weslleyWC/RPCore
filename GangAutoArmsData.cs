using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangAutoArmsOptions : ExtensibleData
    {
        public int IntervalSeconds=180,UnitPrice=500;
        [JsonIgnore] public bool Valid=>IntervalSeconds>=30 && IntervalSeconds<=1800 && UnitPrice>=1 && UnitPrice<=100000;
    }
    public sealed class GangAutoArmsAccount : ExtensibleData
    {
        public string Gang,LastProduct;public int Area;public List<string> Products=new List<string>();public long Sales,Revenue;
        [JsonIgnore] public bool Valid=>!GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area>0 && Products!=null && Products.Count<=4 && Products.All(GangAutoArmsRules.Product) && Products.Distinct().Count()==Products.Count && Sales>=0 && Revenue>=Sales && (Sales==0?Revenue==0 && LastProduct==null:GangAutoArmsRules.Product(LastProduct));
    }
    public sealed class GangAutoArmsData : ExtensibleData
    {
        public List<GangAutoArmsAccount> Accounts=new List<GangAutoArmsAccount>();public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>Accounts!=null && Accounts.Count<=640 && Accounts.All(a=>a!=null && a.Valid) && Accounts.GroupBy(a=>a.Area+"|"+a.Gang,StringComparer.OrdinalIgnoreCase).All(g=>g.Count()==1) && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=120) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public static class GangAutoArmsRules
    {
        public static bool Product(string id)=>id=="pistol" || id=="smg" || id=="rifle" || id=="shotgun";
        public static GangAutoArmsAccount Account(GangAutoArmsData b,string gang,int area)=>b?.Valid==true?b.Accounts.FirstOrDefault(a=>a.Area==area && GangCrewRules.SameGang(a.Gang,gang)):null;
        public static List<string> Available(WalletData w,string gang,int area,GangArmsOptions c)
        {
            var settings=Account(w?.GangAutoArms,gang,area);var stock=GangArmsTransactions.Account(w?.GangArms,gang,area);
            return settings==null || stock==null || c?.Valid!=true?new List<string>():settings.Products.Where(id=>c.Products.Any(p=>p.Id==id) && GangWarehouseAccount.Get(stock.Stock,id)>0).ToList();
        }
        public static bool Free(WalletData w)=>w!=null && !GangBulkTransactions.Blocked(w) && (w.GangRaid==null || w.GangRaid.Valid && w.GangRaid.Pending==null) && (w.GangBusiness==null || w.GangBusiness.Valid && w.GangBusiness.Supply==null);
    }
    // Session only: no wall/game clock catch-up and no saved entity/timer state.
    public sealed class GangAutoArmsClock
    {
        int last;double progress;public int Milliseconds=> (int)progress;public string Receipt {get;}=Guid.NewGuid().ToString("N");
        public GangAutoArmsClock(int now){last=now;}
        public bool Step(int now,bool working,int seconds)=>Step(now,working,seconds,1);
        public bool Step(int now,bool working,int seconds,double multiplier)
        {uint delta=unchecked((uint)(now-last));last=now;if(seconds<30 || seconds>1800 || double.IsNaN(multiplier) || double.IsInfinity(multiplier) || multiplier<1 || multiplier>2)return false;if(working && delta<=2000)progress=Math.Min(seconds*1000.0,progress+delta*multiplier);return working && Milliseconds>=seconds*1000;}
    }
    public static class GangAutoArmsTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        static bool Id(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=80;
        static void Receipt(GangAutoArmsData b,string id){b.Receipts.Add(id);if(b.Receipts.Count>256)b.Receipts.RemoveAt(0);}
        public static bool Configure(WalletData w,GangState s,GangTurfDefinition area,GangArmsOptions c,string product,bool enabled,string id,Action save,out bool changed)
        {
            changed=false;if(w==null || save==null || !Id(id) || enabled && !GangAutoArmsRules.Free(w) || !GangBusinessTransactions.Owns(s,area) || !GangAutoArmsRules.Product(product) || c?.Valid!=true || !c.Products.Any(p=>p.Id==product) || w.GangAutoArms!=null && !w.GangAutoArms.Valid)return false;
            string receipt=id+":auto-config";var old=w.GangAutoArms;if(old?.Receipts.Contains(receipt)==true)return true;var book=old==null?new GangAutoArmsData():Copy(old);var a=GangAutoArmsRules.Account(book,s.Gang,area.Id);
            if(a==null){if(!enabled || book.Accounts.Count>=640)return false;a=new GangAutoArmsAccount{Gang=s.Gang,Area=area.Id};book.Accounts.Add(a);}
            if(a.Products.Contains(product)==enabled)return true;if(enabled)a.Products.Add(product);else a.Products.Remove(product);Receipt(book,receipt);if(!book.Valid)return false;
            try{w.GangAutoArms=book;save();changed=true;return true;}catch{w.GangAutoArms=old;throw;}
        }
        public static bool Stop(WalletData w,string gang,int area,string id,Action save,out bool changed)
        {
            changed=false;if(w?.GangAutoArms?.Valid!=true || save==null || !Id(id))return false;string receipt=id+":auto-stop";var old=w.GangAutoArms;if(old.Receipts.Contains(receipt))return true;
            var book=Copy(old);var a=GangAutoArmsRules.Account(book,gang,area);if(a==null)return false;if(a.Products.Count==0)return true;a.Products.Clear();Receipt(book,receipt);
            try{w.GangAutoArms=book;save();changed=true;return true;}catch{w.GangAutoArms=old;throw;}
        }
        public static bool Sale(WalletData w,GangState s,GangTurfDefinition area,GangArmsOptions catalog,GangAutoArmsOptions c,string product,string id,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || !Id(id) || !GangBusinessTransactions.Owns(s,area) || !GangAutoArmsRules.Free(w) || c?.Valid!=true || catalog?.Valid!=true || w.GangAutoArms?.Valid!=true || w.GangArms?.Valid!=true || w.GangIllicit!=null && !w.GangIllicit.Valid || !GangAutoArmsRules.Product(product))return false;
            string receipt=id+":auto-sale";if(w.GangAutoArms.Receipts.Contains(receipt))return true;
            if(!GangAutoArmsRules.Available(w,s.Gang,area.Id,catalog).Contains(product))return false;
            var old=w.GangAutoArms;var stock=w.GangArms;var dirty=w.GangIllicit;var book=Copy(old);var storage=Copy(stock);var income=dirty==null?new GangIllicitData():Copy(dirty);
            var a=GangAutoArmsRules.Account(book,s.Gang,area.Id);var arms=GangArmsTransactions.Account(storage,s.Gang,area.Id);var d=GangIllicitTransactions.Account(income,s.Gang,area.Id);
            if(a.Sales==long.MaxValue || a.Revenue>long.MaxValue-c.UnitPrice || GangWarehouseAccount.Get(arms.Sold,product)==long.MaxValue || arms.Revenue>long.MaxValue-c.UnitPrice)return false;
            if(d==null){if(income.Accounts.Count>=640)return false;d=new GangIllicitAccount{Gang=s.Gang,Area=area.Id};income.Accounts.Add(d);}if(d.Dirty>int.MaxValue-c.UnitPrice || d.Proceeds>long.MaxValue-c.UnitPrice)return false;
            arms.Stock[product]--;arms.Sold[product]=GangWarehouseAccount.Get(arms.Sold,product)+1;arms.Revenue+=c.UnitPrice;a.Sales++;a.Revenue+=c.UnitPrice;a.LastProduct=product;d.Dirty+=c.UnitPrice;d.Proceeds+=c.UnitPrice;
            Receipt(book,receipt);storage.Receipts.Add(receipt);if(storage.Receipts.Count>256)storage.Receipts.RemoveAt(0);income.Receipts.Add(receipt);if(income.Receipts.Count>256)income.Receipts.RemoveAt(0);if(!book.Valid || !storage.Valid || !income.Valid)return false;
            try{w.GangAutoArms=book;w.GangArms=storage;w.GangIllicit=income;save();changed=true;return true;}catch{w.GangAutoArms=old;w.GangArms=stock;w.GangIllicit=dirty;throw;}
        }
    }
}
