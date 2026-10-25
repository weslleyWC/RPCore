using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangWeaponRaidOptions : ExtensibleData
    {
        public string VehicleModel="gburrito2";public int SecondsLimit=1200,Radius=120;
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public List<JobPoint> Sites=new GangCriminalSupplyOptions().Route.Suppliers;
        [JsonIgnore] public bool Valid=>GangPresenceRules.ModelName(VehicleModel) && SecondsLimit>=120 && SecondsLimit<=1800 && Radius>=40 && Radius<=150 && Sites!=null && Sites.Count>=1 && Sites.Count<=30 && Sites.All(p=>p?.Valid==true) && Sites.Select(p=>p.X+"|"+p.Y+"|"+p.Z).Distinct().Count()==Sites.Count;
    }
    public static class GangWeaponRaidRules
    {
        public static Dictionary<string,int> Loot(Random random)
        {
            if(random==null)throw new ArgumentNullException(nameof(random));var loot=new Dictionary<string,int>{{"pistol",random.Next(2,7)}};
            if(random.Next(100)<75)loot["smg"]=random.Next(1,5);if(random.Next(100)<55)loot["rifle"]=random.Next(1,4);if(random.Next(100)<50)loot["shotgun"]=random.Next(1,4);loot["ammo"]=random.Next(100,251);return loot;
        }
        public static bool ValidLoot(Dictionary<string,int> loot)=>loot!=null && loot.Count>=2 && loot.Count<=5 && loot.ContainsKey("pistol") && loot.ContainsKey("ammo") && loot.All(p=>p.Key=="pistol"?p.Value>=2 && p.Value<=6:p.Key=="ammo"?p.Value>=100 && p.Value<=250:p.Key=="smg"?p.Value>=1 && p.Value<=4:(p.Key=="rifle" || p.Key=="shotgun") && p.Value>=1 && p.Value<=3);
        public static bool CanRetire(bool owned,int occupants,float speed)=>owned && occupants==0 && GangPresenceRules.Finite(speed) && speed>=0 && speed<.5f;
    }
    public static class GangWeaponRaidTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        static bool Id(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=80;
        static void Receipt(GangRaidData b,string r){b.Receipts.Add(r);if(b.Receipts.Count>256)b.Receipts.RemoveAt(0);}
        static bool Apply(WalletData w,string receipt,Func<GangRaidData,bool> mutate,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || w.GangRaid!=null && !w.GangRaid.Valid)return false;
            var old=w.GangRaid;if(old?.Receipts.Contains(receipt)==true)return true;var book=old==null?new GangRaidData():Copy(old);
            if(!mutate(book) || !book.Valid)return false;Receipt(book,receipt);
            try{w.GangRaid=book;save();changed=true;return true;}catch{w.GangRaid=old;throw;}
        }
        static bool Fits(GangArmsData storage,GangRaidAttempt p,GangArmsOptions c)
        {
            var a=GangArmsTransactions.Account(storage,p.Gang,p.Area.Id);if(a==null && storage?.Accounts.Count>=640)return false;
            if(a!=null && (a.Stock.Keys.Union(p.Loot.Keys).Count()>40 || a.Recovered.Keys.Union(p.Loot.Keys).Count()>40))return false;
            foreach(var loot in p.Loot)if(!c.Products.Any(v=>v.Id==loot.Key) || a!=null && (GangWarehouseAccount.Get(a.Stock,loot.Key)>(int)p.Capacity-loot.Value || GangWarehouseAccount.Get(a.Recovered,loot.Key)>long.MaxValue-loot.Value) || loot.Value>p.Capacity)return false;
            return true;
        }
        public static bool Begin(WalletData w,GangState s,GangRaidAttempt p,GangArmsOptions c,Action save,out bool changed)
        {
            changed=false;if(w==null || p?.Valid!=true || p.Kind!=GangRaidKind.WeaponVan || p.Package || p.VanDelivered || p.Defeated.Count!=0 || !GangBusinessTransactions.Owns(s,p.Area) || !GangCrewRules.SameGang(s.Gang,p.Gang) || c?.Valid!=true || p.Capacity!=c.MaxStock || w.GangArms!=null && !w.GangArms.Valid || GangBulkTransactions.Blocked(w) || w.GangBusiness!=null && (!w.GangBusiness.Valid || w.GangBusiness.Supply!=null) || !Fits(w.GangArms,p,c))return false;
            return Apply(w,p.Id+":weapon-begin",b=>{if(b.Pending!=null || b.Attempts==int.MaxValue)return false;b.Pending=Copy(p);b.Attempts++;return true;},save,out changed);
        }
        public static bool Capture(WalletData w,string id,Action save,out bool changed)
        {changed=false;if(!Id(id))return false;return Apply(w,id+":weapon-capture",b=>{var p=b.Pending;if(p?.Id!=id || p.Kind!=GangRaidKind.WeaponVan || p.Package || p.Defeated.Count!=3)return false;p.Package=true;return true;},save,out changed);}
        public static bool Deliver(WalletData w,string id,Action save,out bool changed)
        {changed=false;if(!Id(id))return false;return Apply(w,id+":weapon-deliver",b=>{var p=b.Pending;if(p?.Id!=id || p.Kind!=GangRaidKind.WeaponVan || !p.Package || p.VanDelivered)return false;p.VanDelivered=true;return true;},save,out changed);}
        public static bool Restart(WalletData w,string id,Action save,out bool changed)
        {changed=false;if(!Id(id))return false;return Apply(w,Guid.NewGuid().ToString("N")+":weapon-restart",b=>{var p=b.Pending;if(p?.Id!=id || p.Kind!=GangRaidKind.WeaponVan || p.VanDelivered)return false;p.Package=false;p.Defeated.Clear();b.Receipts.RemoveAll(r=>r.StartsWith(id+":raid-defeat:",StringComparison.Ordinal) || r==id+":weapon-capture");return true;},save,out changed);}
        public static bool Complete(WalletData w,GangState s,GangTurfDefinition area,GangArmsOptions c,string id,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || !Id(id) || !GangBusinessTransactions.Owns(s,area) || w.GangRaid?.Valid!=true || w.GangArms!=null && !w.GangArms.Valid || c?.Valid!=true || GangBulkTransactions.Blocked(w) || w.GangBusiness?.Supply!=null)return false;
            string receipt=id+":weapon-complete";if(w.GangRaid.Receipts.Contains(receipt))return true;
            var old=w.GangRaid;var original=w.GangArms;var book=Copy(old);var p=book.Pending;
            if(p==null || p.Id!=id || p.Kind!=GangRaidKind.WeaponVan || !p.VanDelivered || p.Area.Id!=area.Id || !GangCrewRules.SameGang(s.Gang,p.Gang) || book.Completed==int.MaxValue || !Fits(original,p,c))return false;
            var storage=original==null?new GangArmsData():Copy(original);var a=GangArmsTransactions.Account(storage,p.Gang,p.Area.Id);if(a==null){a=new GangArmsAccount{Gang=p.Gang,Area=p.Area.Id};storage.Accounts.Add(a);}
            foreach(var loot in p.Loot){a.Stock[loot.Key]=GangWarehouseAccount.Get(a.Stock,loot.Key)+loot.Value;a.Recovered[loot.Key]=GangWarehouseAccount.Get(a.Recovered,loot.Key)+loot.Value;}
            book.Pending=null;book.Completed++;Receipt(book,receipt);storage.Receipts.Add(receipt);if(storage.Receipts.Count>256)storage.Receipts.RemoveAt(0);if(!book.Valid || !storage.Valid)return false;
            try{w.GangRaid=book;w.GangArms=storage;save();changed=true;return true;}catch{w.GangRaid=old;w.GangArms=original;throw;}
        }
    }
}
