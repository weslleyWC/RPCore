using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RPCore
{
    public sealed class GangBusinessOptions : ExtensibleData
    {
        public int UnitCost=20, UnitPrice=35, PurchaseUnits=10, SaleUnits=5, Capacity=100, ServiceSeconds=45, Radius=12;
        [JsonIgnore] public bool Valid=>UnitCost>=1 && UnitCost<=10000 && UnitPrice>=UnitCost && UnitPrice<=20000 && PurchaseUnits>=1 && PurchaseUnits<=100 && SaleUnits>=1 && SaleUnits<=100 && Capacity>=PurchaseUnits && Capacity>=SaleUnits && Capacity<=1000 && ServiceSeconds>=10 && ServiceSeconds<=600 && Radius>=5 && Radius<=30;
    }
    public sealed class GangBusinessAccount : ExtensibleData
    {
        public string Gang; public int Area,Stock; public long Purchased,Sold,Revenue;
        [JsonIgnore] public bool Valid=>!GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area>0 && Stock>=0 && Stock<=1000 && Purchased>=0 && Sold>=0 && Purchased>=Sold && Purchased-Sold==Stock && Revenue>=0;
    }
    public sealed class GangBusinessData : ExtensibleData
    {
        public List<GangBusinessAccount> Accounts=new List<GangBusinessAccount>();
        public List<string> Receipts=new List<string>();
        public GangSupplyOrder Supply;
        [JsonIgnore] public bool Valid=>(Supply==null || Supply.Valid) && Accounts!=null && Accounts.Count<=640 && Accounts.All(a=>a!=null && a.Valid) && Accounts.GroupBy(a=>a.Area+"|"+a.Gang,StringComparer.OrdinalIgnoreCase).All(g=>g.Count()==1) && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=110) && Receipts.Distinct().Count()==Receipts.Count;
    }
    // A runtime-only shift: reload, cancellation and unsafe conditions discard time, never stock.
    public sealed class GangBusinessService
    {
        int last; public int Milliseconds { get; private set; }
        public GangBusinessService(int gameTime){last=gameTime;}
        public bool Ready(int seconds)=>seconds>=10 && seconds<=600 && Milliseconds>=seconds*1000;
        public void Step(int gameTime,bool working)
        {
            uint elapsed=unchecked((uint)(gameTime-last));last=gameTime;
            // Long frame gaps, menus and suspension cannot count as serving customers.
            if(working && elapsed<=2000)Milliseconds=(int)Math.Min(600000L,(long)Milliseconds+elapsed);
        }
    }
    public static class GangBusinessTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        public static GangBusinessAccount Account(GangBusinessData b,string gang,int area)=>b!=null && b.Valid?b.Accounts.FirstOrDefault(a=>a.Area==area && GangCrewRules.SameGang(a.Gang,gang)):null;
        public static bool Owns(GangState s,GangTurfDefinition area)=>s!=null && GangProgressionRules.Valid(s) && !GangLoyaltyRules.Unaffiliated(s.Gang) && area!=null && area.Valid && (s.Turf==null || s.Turf.Valid) && s.Turf?.Pending==null && s.Loyalty?.Pending==null && s.Strength?.Pending==null && s.Driving?.Pending==null && GangCrewRules.SameGang(s.Gang,GangTurfRules.Owner(s.Turf,area));
        public static bool Trade(WalletData w,GangState s,GangTurfDefinition area,GangBusinessOptions c,bool sale,DateTime now,string id,Action save,out bool changed)
        {
            changed=false;
            if(w==null || GangBulkTransactions.Blocked(w) || w.Bank<0 || w.GangBusiness?.Supply!=null || w.GangRaid?.Pending!=null || !Owns(s,area) || c==null || !c.Valid || save==null || string.IsNullOrWhiteSpace(id) || id.Length>80 || w.GangBusiness!=null && !w.GangBusiness.Valid || w.GangTreasury!=null && !w.GangTreasury.Valid)return false;
            string receipt=id+(sale?":sale":":stock");
            var original=w.GangBusiness;var treasury=w.GangTreasury;
            if(original?.Receipts.Contains(receipt)==true)return true;
            var book=original==null?new GangBusinessData():Copy(original);
            var a=Account(book,s.Gang,area.Id);
            if(a==null){if(sale || book.Accounts.Count>=640)return false;a=new GangBusinessAccount{Gang=s.Gang,Area=area.Id};book.Accounts.Add(a);}
            GangTreasuryData funds=treasury;
            int originalBank=w.Bank,bank=w.Bank;
            if(sale)
            {
                int revenue=c.SaleUnits*c.UnitPrice;
                if(a.Stock<c.SaleUnits || a.Sold>long.MaxValue-c.SaleUnits || a.Revenue>long.MaxValue-revenue)return false;
                funds=treasury==null?new GangTreasuryData():Copy(treasury);
                var account=GangTreasuryTransactions.Account(funds,s.Gang);
                if(account==null){if(funds.Accounts.Count>=64)return false;funds.Accounts[s.Gang]=account=new GangTreasuryAccount{LastPaydayTicks=now.Ticks};}
                if(account.Balance>int.MaxValue-revenue || account.BusinessIncome>long.MaxValue-revenue)return false;
                a.Stock-=c.SaleUnits;a.Sold+=c.SaleUnits;a.Revenue+=revenue;
                account.Balance+=revenue;account.BusinessIncome+=revenue;
            }
            else
            {
                int cost=c.PurchaseUnits*c.UnitCost;
                if(w.Bank<cost || a.Stock>c.Capacity-c.PurchaseUnits || a.Purchased>long.MaxValue-c.PurchaseUnits)return false;
                a.Stock+=c.PurchaseUnits;a.Purchased+=c.PurchaseUnits;bank-=cost;
            }
            if(!book.Valid || funds!=null && !funds.Valid)return false;
            book.Receipts.Add(receipt);if(book.Receipts.Count>256)book.Receipts.RemoveAt(0);
            try{w.GangBusiness=book;w.GangTreasury=funds;w.Bank=bank;save();changed=true;return true;}
            catch{w.GangBusiness=original;w.GangTreasury=treasury;w.Bank=originalBank;throw;}
        }
    }
}
