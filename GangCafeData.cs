using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangCafeOptions : ExtensibleData
    {
        public int BoostMinutes=60;
        public JobPoint Parking=new JobPoint{Name="Patio do cafe",X=-1270.415f,Y=-1211.753f,Z=4.90133f};
        public JobPoint Door=new JobPoint{Name="Entrega do cafe",X=-1275.385f,Y=-1200.205f,Z=5.370624f};
        public GangSupplyOptions Route=new GangSupplyOptions();
        [JsonIgnore] public bool Valid=>BoostMinutes>=1 && BoostMinutes<=180 && Parking?.Valid==true && Door?.Valid==true && Route?.Valid==true && Math.Pow(Parking.X-Door.X,2)+Math.Pow(Parking.Y-Door.Y,2)+Math.Pow(Parking.Z-Door.Z,2)<=400;
    }
    public sealed class GangCafeAccount : ExtensibleData
    {
        public string Gang;public int Area;public long Completed,StartedUtcTicks,ExpiresUtcTicks;
        [JsonIgnore] public bool Valid=>!GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area>0 && Completed>0 && StartedUtcTicks>0 && ExpiresUtcTicks<=DateTime.MaxValue.Ticks && ExpiresUtcTicks>StartedUtcTicks && ExpiresUtcTicks-StartedUtcTicks<=TimeSpan.FromMinutes(180).Ticks;
    }
    public sealed class GangCafeData : ExtensibleData
    {
        public List<GangCafeAccount> Accounts=new List<GangCafeAccount>();public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>Accounts!=null && Accounts.Count<=640 && Accounts.All(a=>a?.Valid==true) && Accounts.GroupBy(a=>a.Area+"|"+a.Gang,StringComparer.OrdinalIgnoreCase).All(g=>g.Count()==1) && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=110) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public static class GangCafeTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        static bool Id(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=80;
        public static GangCafeAccount Account(GangCafeData b,string gang,int area)=>b?.Valid==true?b.Accounts.FirstOrDefault(a=>a.Area==area && GangCrewRules.SameGang(a.Gang,gang)):null;
        // Wall-clock expiry matches PSRP. Production itself still requires active safe play.
        // A clock before the original award cannot activate the reward.
        public static int Multiplier(GangCafeData b,string gang,int area,DateTime utc)
        {var a=Account(b,gang,area);return utc.Kind==DateTimeKind.Utc && a!=null && utc.Ticks>=a.StartedUtcTicks && utc.Ticks<a.ExpiresUtcTicks?2:1;}
        public static bool Begin(WalletData w,GangState s,GangTurfDefinition area,GangCafeOptions c,string id,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || !Id(id) || c?.Valid!=true || area?.Id!=4 || !GangBusinessTransactions.Owns(s,area) || w.GangCafe!=null && !w.GangCafe.Valid || w.GangBusiness!=null && !w.GangBusiness.Valid)return false;
            var old=w.GangBusiness;string receipt=id+":cafe-begin";if(old?.Receipts.Contains(receipt)==true)return true;if(!GangAutoArmsRules.Free(w))return false;
            var reward=Account(w.GangCafe,s.Gang,area.Id);if(reward?.Completed==long.MaxValue || reward==null && w.GangCafe?.Accounts.Count>=640)return false;
            var book=old==null?new GangBusinessData():Copy(old);book.Supply=new GangSupplyOrder{Id=id,Gang=s.Gang,Area=Copy(area),Options=Copy(c.Route),Cafe=Copy(c),CargoKind=GangSupplyCargo.Cafe,Cost=0,Units=3,Capacity=3};
            book.Receipts.Add(receipt);if(book.Receipts.Count>256)book.Receipts.RemoveAt(0);if(!book.Valid)return false;
            try{w.GangBusiness=book;save();changed=true;return true;}catch{w.GangBusiness=old;throw;}
        }
        public static bool Complete(WalletData w,GangState s,GangTurfDefinition area,string id,DateTime utc,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || !Id(id) || utc.Kind!=DateTimeKind.Utc || utc.Ticks==0 || area?.Id!=4 || !GangBusinessTransactions.Owns(s,area) || w.GangBusiness?.Valid!=true || w.GangCafe!=null && !w.GangCafe.Valid || GangBulkTransactions.Blocked(w) || w.GangRaid!=null && (!w.GangRaid.Valid || w.GangRaid.Pending!=null))return false;
            string receipt=id+":cafe-complete";if(w.GangCafe?.Receipts.Contains(receipt)==true)return true;
            var old=w.GangBusiness;var original=w.GangCafe;var p=old.Supply;
            if(p==null || p.Id!=id || p.CargoKind!=GangSupplyCargo.Cafe || !p.Ready || p.Area.Id!=area.Id || !GangCrewRules.SameGang(p.Gang,s.Gang) || utc.Ticks>DateTime.MaxValue.Ticks-TimeSpan.FromMinutes(p.Cafe.BoostMinutes).Ticks)return false;
            var book=Copy(old);var bonuses=original==null?new GangCafeData():Copy(original);var a=Account(bonuses,s.Gang,area.Id);
            if(a==null){if(bonuses.Accounts.Count>=640)return false;a=new GangCafeAccount{Gang=s.Gang,Area=area.Id};bonuses.Accounts.Add(a);}if(a.Completed==long.MaxValue)return false;
            a.Completed++;a.StartedUtcTicks=utc.Ticks;a.ExpiresUtcTicks=utc.AddMinutes(p.Cafe.BoostMinutes).Ticks; // Refresh, never stack remaining time.
            bonuses.Receipts.Add(receipt);if(bonuses.Receipts.Count>256)bonuses.Receipts.RemoveAt(0);book.Supply=null;book.Receipts.Add(receipt);if(book.Receipts.Count>256)book.Receipts.RemoveAt(0);
            if(!bonuses.Valid || !book.Valid)return false;
            try{w.GangBusiness=book;w.GangCafe=bonuses;save();changed=true;return true;}catch{w.GangBusiness=old;w.GangCafe=original;throw;}
        }
    }
}
