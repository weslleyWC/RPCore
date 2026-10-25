using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangRaidOptions : ExtensibleData
    {
        public int MinLoot=35,MaxLoot=90,SecondsLimit=900,SearchSeconds=2,Radius=100;
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)]
        public List<JobPoint> Sites=new GangCriminalSupplyOptions().Route.Suppliers;
        [JsonIgnore] public bool Valid=>MinLoot>=1 && MaxLoot>=MinLoot && MaxLoot<=90 && SecondsLimit>=120 && SecondsLimit<=1800 && SearchSeconds>=2 && SearchSeconds<=10 && Radius>=40 && Radius<=150 && Sites!=null && Sites.Count>=1 && Sites.Count<=30 && Sites.All(p=>p!=null && p.Valid) && Sites.Select(p=>p.X+"|"+p.Y+"|"+p.Z).Distinct().Count()==Sites.Count;
    }
    public enum GangRaidKind { Drugs=0,WeaponVan=1 }
    public sealed class GangRaidAttempt : ExtensibleData
    {
        public GangRaidKind Kind;public GangWeaponRaidOptions WeaponOptions;public bool VanDelivered;
        public string Id,Gang,Rival;public GangTurfDefinition Area;public JobPoint Site;public GangRaidOptions Options;public float Capacity;
        public string[] Models;public Dictionary<string,int> Loot=new Dictionary<string,int>();public List<int> Defeated=new List<int>();public bool Package;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && !GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && !GangLoyaltyRules.Unaffiliated(Rival) && Rival.Length<=100 && !GangCrewRules.SameGang(Gang,Rival) && Area!=null && Area.Valid && Site!=null && Site.Valid && (Kind==GangRaidKind.Drugs?Options!=null && Options.Valid && !VanDelivered:Kind==GangRaidKind.WeaponVan && WeaponOptions?.Valid==true && (!VanDelivered || Package) && Capacity<=999 && Capacity==(int)Capacity) && GangPresenceRules.Finite(Capacity) && Capacity>=1 && Capacity<=1000 && Models!=null && Models.Length==3 && Models.All(GangPresenceRules.ModelName) && (Kind==GangRaidKind.Drugs?Loot!=null && Loot.Count==2 && Loot.All(p=>!string.IsNullOrWhiteSpace(p.Key) && p.Key.Length<=80 && p.Value>=1 && p.Value<=90):GangWeaponRaidRules.ValidLoot(Loot)) && Defeated!=null && Defeated.Count<=3 && Defeated.All(i=>i>=0 && i<3) && Defeated.Distinct().Count()==Defeated.Count && (!Package || Defeated.Count==3);
    }
    public sealed class GangRaidData : ExtensibleData
    {
        public GangRaidAttempt Pending;public int Attempts,Completed,Failed;public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>Attempts>=0 && Completed>=0 && Failed>=0 && (long)Completed+Failed<=Attempts && (Pending==null || Pending.Valid && (long)Completed+Failed<Attempts) && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=120) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public sealed class GangRaidSearch
    {
        int last,elapsed;public bool Searching {get;private set;}
        public void Start(int now){last=now;elapsed=0;Searching=true;}
        public bool Step(int now,bool eligible,int seconds)
        {if(!Searching)return false;uint delta=unchecked((uint)(now-last));last=now;if(!eligible || seconds<2 || seconds>10){Searching=false;elapsed=0;return false;}if(delta<=2000)elapsed+=(int)delta;if(elapsed<seconds*1000)return false;Searching=false;return true;}
    }
    public static class GangRaidTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        static bool Id(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=80;
        static bool Apply(WalletData w,string receipt,Func<GangRaidData,bool> mutate,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || w.GangRaid!=null && !w.GangRaid.Valid)return false;
            var original=w.GangRaid;if(original?.Receipts.Contains(receipt)==true)return true;var book=original==null?new GangRaidData():Copy(original);
            if(!mutate(book) || !book.Valid)return false;book.Receipts.Add(receipt);if(book.Receipts.Count>256)book.Receipts.RemoveAt(0);
            try{w.GangRaid=book;save();changed=true;return true;}catch{w.GangRaid=original;throw;}
        }
        public static bool Begin(WalletData w,GangState s,GangRaidAttempt p,DealerOptions c,Action save,out bool changed)
        {
            changed=false;if(w==null || GangBulkTransactions.Blocked(w) || p==null || !p.Valid || p.Kind!=GangRaidKind.Drugs || p.Package || p.Defeated.Count!=0 || !GangBusinessTransactions.Owns(s,p.Area) || !GangCrewRules.SameGang(s.Gang,p.Gang) || w.GangBusiness?.Supply!=null || c==null || !c.Valid || w.GangWarehouse!=null && !w.GangWarehouse.Valid)return false;
            if(!Fits(w.GangWarehouse,p,c))return false;
            return Apply(w,p.Id+":raid-begin",b=>{if(b.Pending!=null || b.Attempts==int.MaxValue)return false;b.Pending=Copy(p);b.Attempts++;return true;},save,out changed);
        }
        static bool Fits(GangWarehouseData storage,GangRaidAttempt p,DealerOptions c)
        {
            var a=GangWarehouseTransactions.Account(storage,p.Gang,p.Area.Id);if(a==null && storage?.Accounts.Count>=640)return false;
            if(a!=null && (a.Stock.Keys.Union(p.Loot.Keys).Count()>40 || a.Raided.Keys.Union(p.Loot.Keys).Count()>40))return false;
            double weight=GangWarehouseTransactions.Weight(a,c);
            foreach(var loot in p.Loot){var item=c.Products.Find(v=>v.Id==loot.Key);if(item==null || a!=null && GangWarehouseAccount.Get(a.Raided,loot.Key)>long.MaxValue-loot.Value || (a==null?0:GangWarehouseAccount.Get(a.Stock,loot.Key))>999-loot.Value)return false;weight+=loot.Value*(double)item.Weight;}
            return weight<=p.Capacity+.0001;
        }
        public static bool Defeat(WalletData w,string id,int index,Action save,out bool changed)
        {changed=false;if(!Id(id) || index<0 || index>=3)return false;return Apply(w,id+":raid-defeat:"+index,b=>{var p=b.Pending;if(p==null || p.Id!=id || p.Package || p.Defeated.Contains(index))return false;p.Defeated.Add(index);return true;},save,out changed);}
        public static bool Collect(WalletData w,string id,Action save,out bool changed)=>Collect(w,id,save,out changed,"Neutral");
        public static bool Collect(WalletData w,string id,Action save,out bool changed,string initial)
        {
            changed=false;if(!Id(id) || w==null || w.Bank<0 || save==null || w.GangRaid?.Valid!=true || w.GangDiplomacy!=null && !w.GangDiplomacy.Valid || !GangDiplomacyRules.Known(initial))return false;
            string receipt=id+":raid-package";if(w.GangRaid.Receipts.Contains(receipt))return true;
            var original=w.GangRaid;var old=w.GangDiplomacy;var book=Copy(original);var p=book.Pending;
            if(p==null || p.Kind!=GangRaidKind.Drugs || p.Id!=id || p.Package || p.Defeated.Count!=3)return false;
            var diplomacy=GangDiplomacyRules.Stolen(old,p.Gang,p.Rival,initial);if(diplomacy==null)return false;
            p.Package=true;book.Receipts.Add(receipt);if(book.Receipts.Count>256)book.Receipts.RemoveAt(0);GangDiplomacyRules.Receipt(diplomacy,receipt);
            try{w.GangRaid=book;w.GangDiplomacy=diplomacy;save();changed=true;return true;}catch{w.GangRaid=original;w.GangDiplomacy=old;throw;}
        }
        public static bool Restart(WalletData w,string id,Action save,out bool changed)
        {changed=false;if(!Id(id))return false;return Apply(w,Guid.NewGuid().ToString("N")+":raid-restart",b=>{var p=b.Pending;if(p==null || p.Kind!=GangRaidKind.Drugs || p.Id!=id || p.Package)return false;p.Defeated.Clear();b.Receipts.RemoveAll(r=>r.StartsWith(id+":raid-defeat:",StringComparison.Ordinal));return true;},save,out changed);}
        public static bool End(WalletData w,string id,bool failed,Action save,out bool changed)
        {changed=false;if(!Id(id))return false;return Apply(w,id+":raid-end",b=>{if(b.Pending?.Id!=id || failed && b.Failed==int.MaxValue)return false;b.Pending=null;if(failed)b.Failed++;return true;},save,out changed);}
        public static bool Complete(WalletData w,GangState s,GangTurfDefinition area,DealerOptions c,string id,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || !Id(id) || !GangBusinessTransactions.Owns(s,area) || w.GangBusiness?.Supply!=null || w.GangRaid?.Valid!=true || w.GangWarehouse!=null && !w.GangWarehouse.Valid || c==null || !c.Valid)return false;
            string receipt=id+":raid-complete";if(w.GangRaid.Receipts.Contains(receipt))return true;
            var original=w.GangRaid;var old=w.GangWarehouse;var book=Copy(original);var p=book.Pending;
            if(p==null || p.Kind!=GangRaidKind.Drugs || p.Id!=id || !p.Package || p.Area.Id!=area.Id || !GangCrewRules.SameGang(p.Gang,s.Gang) || book.Completed==int.MaxValue || !Fits(old,p,c))return false;
            var storage=old==null?new GangWarehouseData():Copy(old);var a=GangWarehouseTransactions.Account(storage,p.Gang,p.Area.Id);
            if(a==null){a=new GangWarehouseAccount{Gang=p.Gang,Area=p.Area.Id};storage.Accounts.Add(a);}
            foreach(var loot in p.Loot){long raided=GangWarehouseAccount.Get(a.Raided,loot.Key);if(raided>long.MaxValue-loot.Value)return false;a.Stock[loot.Key]=GangWarehouseAccount.Get(a.Stock,loot.Key)+loot.Value;a.Raided[loot.Key]=raided+loot.Value;}
            book.Pending=null;book.Completed++;book.Receipts.Add(receipt);if(book.Receipts.Count>256)book.Receipts.RemoveAt(0);storage.Receipts.Add(receipt);if(storage.Receipts.Count>256)storage.Receipts.RemoveAt(0);
            if(!book.Valid || !storage.Valid)return false;
            try{w.GangRaid=book;w.GangWarehouse=storage;save();changed=true;return true;}catch{w.GangRaid=original;w.GangWarehouse=old;throw;}
        }
    }
}
