using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangLaunderingOptions : ExtensibleData
    {
        public int BatchAmount=500,Seconds=60,Radius=12,SaleRadius=45;
        [JsonIgnore] public bool Valid=>BatchAmount>=1 && BatchAmount<=10000 && Seconds>=10 && Seconds<=600 && Radius>=5 && Radius<=30 && SaleRadius>=20 && SaleRadius<=100;
    }
    public sealed class GangIllicitAccount : ExtensibleData
    {
        public string Gang;public int Area,Dirty;public long Proceeds,Processed,Net,Fees;
        [JsonIgnore] public bool Valid=>!GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area>0 && Dirty>=0 && Proceeds>=0 && Processed>=0 && Net>=0 && Fees>=0 && Proceeds>=Processed && Proceeds-Processed==Dirty && Net==(long)decimal.Floor((decimal)Processed*85/100) && (decimal)Net+Fees==Processed;
    }
    public sealed class GangIllicitData : ExtensibleData
    {
        public List<GangIllicitAccount> Accounts=new List<GangIllicitAccount>();public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>Accounts!=null && Accounts.Count<=640 && Accounts.All(a=>a!=null && a.Valid) && Accounts.GroupBy(a=>a.Area+"|"+a.Gang,StringComparer.OrdinalIgnoreCase).All(g=>g.Count()==1) && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=180) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public static class GangIllicitTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        public static GangIllicitAccount Account(GangIllicitData b,string gang,int area)=>b!=null && b.Valid?b.Accounts.FirstOrDefault(a=>a.Area==area && GangCrewRules.SameGang(a.Gang,gang)):null;
        static void Receipt(GangIllicitData b,string id){b.Receipts.Add(id);if(b.Receipts.Count>256)b.Receipts.RemoveAt(0);}
        public static bool Sale(WalletData w,GangState state,GangTurfDefinition area,DealerOptions c,string product,int count,string zone,bool premium,int unit,bool last,string id,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || !GangBusinessTransactions.Owns(state,area) || w.GangBusiness?.Supply!=null || string.IsNullOrWhiteSpace(id) || id.Length>160 || w.GangIllicit!=null && !w.GangIllicit.Valid || w.Dealer!=null && !w.Dealer.Valid)return false;
            if(w.Dealer?.Receipts.Contains(id)==true || w.GangIllicit?.Receipts.Contains(id)==true)return true;
            // Reuse validated stock, quote, statistics and reputation rules without publishing
            // the legacy bank credit. Both books are committed by the real Wallet save below.
            var shadow=new WalletData{Bank=0,Dealer=w.Dealer};
            if(!DealerTransactions.Sale(shadow,c,product,count,zone,premium,unit,last,id,()=>{},out bool sold) || !sold)return false;
            var original=w.GangIllicit;var book=original==null?new GangIllicitData():Copy(original);
            var a=Account(book,state.Gang,area.Id);if(a==null){if(book.Accounts.Count>=640)return false;a=new GangIllicitAccount{Gang=state.Gang,Area=area.Id};book.Accounts.Add(a);}
            int total=shadow.Bank;if(a.Dirty>int.MaxValue-total || a.Proceeds>long.MaxValue-total)return false;
            a.Dirty+=total;a.Proceeds+=total;Receipt(book,id);if(!book.Valid)return false;
            var dealer=w.Dealer;
            try{w.GangIllicit=book;w.Dealer=shadow.Dealer;save();changed=true;return true;}
            catch{w.GangIllicit=original;w.Dealer=dealer;throw;}
        }
        public static bool Process(WalletData w,GangState state,GangTurfDefinition area,GangLaunderingOptions c,int amount,DateTime now,string id,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || !GangBusinessTransactions.Owns(state,area) || w.GangBusiness?.Supply!=null || c==null || !c.Valid || amount<1 || amount>c.BatchAmount || string.IsNullOrWhiteSpace(id) || id.Length>80 || w.GangIllicit==null || !w.GangIllicit.Valid || w.GangTreasury!=null && !w.GangTreasury.Valid)return false;
            string receipt=id+":launder";if(w.GangIllicit.Receipts.Contains(receipt))return true;
            var original=w.GangIllicit;var treasury=w.GangTreasury;var book=Copy(original);var a=Account(book,state.Gang,area.Id);
            if(a==null || a.Dirty<amount || a.Processed>long.MaxValue-amount)return false;
            long processed=a.Processed+amount,net=(long)decimal.Floor((decimal)processed*85/100);int credit=(int)(net-a.Net);
            var funds=treasury;
            if(credit>0)
            {
                funds=treasury==null?new GangTreasuryData():Copy(treasury);var t=GangTreasuryTransactions.Account(funds,state.Gang);
                if(t==null){if(funds.Accounts.Count>=64)return false;t=new GangTreasuryAccount{LastPaydayTicks=now.Ticks};funds.Accounts[state.Gang]=t;}
                if(t.Balance>int.MaxValue-credit || t.LaunderedIncome>long.MaxValue-credit)return false;
                t.Balance+=credit;t.LaunderedIncome+=credit;
            }
            a.Dirty-=amount;a.Processed=processed;a.Net=net;a.Fees=processed-net;Receipt(book,receipt);
            if(!book.Valid || funds!=null && !funds.Valid)return false;
            try{w.GangIllicit=book;w.GangTreasury=funds;save();changed=true;return true;}
            catch{w.GangIllicit=original;w.GangTreasury=treasury;throw;}
        }
    }
}
