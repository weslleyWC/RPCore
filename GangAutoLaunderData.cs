using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangAutoLaunderOptions : ExtensibleData
    {
        public int BatchAmount=500,IntervalSeconds=60;
        [JsonIgnore] public bool Valid=>BatchAmount>=1 && BatchAmount<=10000 && IntervalSeconds>=30 && IntervalSeconds<=1800;
    }
    public sealed class GangAutoLaunderAccount : ExtensibleData
    {
        public string Gang;public int Area;public bool Enabled;public long Batches,Processed,Net,Fees;
        [JsonIgnore] public bool Valid=>!GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area>0 && Batches>=0 && Processed>=0 && Batches<=Processed && Net>=0 && Fees>=0 && (decimal)Net+Fees==Processed && (Batches!=0 || Processed==0);
    }
    public sealed class GangAutoLaunderData : ExtensibleData
    {
        public List<GangAutoLaunderAccount> Accounts=new List<GangAutoLaunderAccount>();public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>Accounts!=null && Accounts.Count<=640 && Accounts.All(a=>a!=null && a.Valid) && Accounts.GroupBy(a=>a.Area+"|"+a.Gang,StringComparer.OrdinalIgnoreCase).All(g=>g.Count()==1) && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=120) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public static class GangAutoLaunderTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        static bool Id(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=80;
        static void Receipt(GangAutoLaunderData b,string r){b.Receipts.Add(r);if(b.Receipts.Count>256)b.Receipts.RemoveAt(0);}
        public static GangAutoLaunderAccount Account(GangAutoLaunderData b,string gang,int area)=>b?.Valid==true?b.Accounts.FirstOrDefault(a=>a.Area==area && GangCrewRules.SameGang(a.Gang,gang)):null;
        public static bool Configure(WalletData w,GangState s,GangTurfDefinition area,bool enabled,string id,Action save,out bool changed)
        {
            changed=false;if(w==null || save==null || !Id(id) || !GangBusinessTransactions.Owns(s,area) || enabled && !GangAutoArmsRules.Free(w) || w.GangAutoLaunder!=null && !w.GangAutoLaunder.Valid)return false;
            string receipt=id+":auto-launder-config";var old=w.GangAutoLaunder;if(old?.Receipts.Contains(receipt)==true)return true;var book=old==null?new GangAutoLaunderData():Copy(old);var a=Account(book,s.Gang,area.Id);
            if(a==null){if(!enabled || book.Accounts.Count>=640)return false;a=new GangAutoLaunderAccount{Gang=s.Gang,Area=area.Id};book.Accounts.Add(a);}
            if(a.Enabled==enabled)return true;a.Enabled=enabled;Receipt(book,receipt);
            try{w.GangAutoLaunder=book;save();changed=true;return true;}catch{w.GangAutoLaunder=old;throw;}
        }
        public static bool Stop(WalletData w,string gang,int area,string id,Action save,out bool changed)
        {
            changed=false;if(w?.GangAutoLaunder?.Valid!=true || !Id(id) || save==null)return false;string receipt=id+":auto-launder-stop";var old=w.GangAutoLaunder;if(old.Receipts.Contains(receipt))return true;
            var book=Copy(old);var a=Account(book,gang,area);if(a==null)return false;if(!a.Enabled)return true;a.Enabled=false;Receipt(book,receipt);
            try{w.GangAutoLaunder=book;save();changed=true;return true;}catch{w.GangAutoLaunder=old;throw;}
        }
        public static bool Process(WalletData w,GangState s,GangTurfDefinition area,GangAutoLaunderOptions c,DateTime now,string id,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || !Id(id) || !GangBusinessTransactions.Owns(s,area) || !GangAutoArmsRules.Free(w) || c?.Valid!=true || w.GangAutoLaunder?.Valid!=true || w.GangIllicit?.Valid!=true || w.GangTreasury!=null && !w.GangTreasury.Valid)return false;
            string receipt=id+":auto-launder";var original=w.GangAutoLaunder;if(original.Receipts.Contains(receipt))return true;
            var a=Account(original,s.Gang,area.Id);var income=GangIllicitTransactions.Account(w.GangIllicit,s.Gang,area.Id);
            if(a?.Enabled!=true || income==null || income.Dirty<1)return false;int amount=Math.Min(income.Dirty,c.BatchAmount);
            if(a.Batches==long.MaxValue || a.Processed>long.MaxValue-amount)return false;
            // Reuse the single existing cumulative85% calculation; publish all three books
            // only in the real Wallet save. Interleaved manual processing stays consistent.
            var shadow=new WalletData{Bank=w.Bank,GangIllicit=w.GangIllicit,GangTreasury=w.GangTreasury,GangBusiness=w.GangBusiness,GangBulk=w.GangBulk,GangRaid=w.GangRaid};
            var batch=new GangLaunderingOptions{BatchAmount=c.BatchAmount};
            if(!GangIllicitTransactions.Process(shadow,s,area,batch,amount,now,id,()=>{},out bool applied) || !applied)return false;
            var result=GangIllicitTransactions.Account(shadow.GangIllicit,s.Gang,area.Id);long credit=result.Net-income.Net,fee=amount-credit;
            if(credit<0 || fee<0 || a.Net>long.MaxValue-credit || a.Fees>long.MaxValue-fee)return false;
            var book=Copy(original);a=Account(book,s.Gang,area.Id);a.Batches++;a.Processed+=amount;a.Net+=credit;a.Fees+=fee;Receipt(book,receipt);if(!book.Valid)return false;
            var dirty=w.GangIllicit;var treasury=w.GangTreasury;
            try{w.GangAutoLaunder=book;w.GangIllicit=shadow.GangIllicit;w.GangTreasury=shadow.GangTreasury;save();changed=true;return true;}
            catch{w.GangAutoLaunder=original;w.GangIllicit=dirty;w.GangTreasury=treasury;throw;}
        }
    }
}
