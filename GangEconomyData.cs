using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public static class GangProgressionRules
    {
        public static bool Valid(GangState s)=>s!=null && s.Reputation>=0 && s.Rank>=0 && s.Rank<=4;
        public static int ThresholdRank(int reputation)=>reputation>=1500?4:reputation>=750?3:reputation>=250?2:1;
        public static int Rank(GangState s)=>Math.Max(s?.Rank>=1 && s.Rank<=4?s.Rank:1,ThresholdRank(s?.Reputation??0));
        public static int Share(int rank)=>rank==4?35:rank==3?20:rank==2?10:5;
        public static string Name(int rank)=>rank==4?"Lider":rank==3?"Subchefe":rank==2?"Soldado":"Recruta";
        public static bool Grant(GangState s,int delta)
        {if(!Valid(s) || GangLoyaltyRules.Unaffiliated(s.Gang))return false;int before=Rank(s);s.Reputation=(int)Math.Max(0,Math.Min(int.MaxValue,(long)s.Reputation+delta));s.Rank=Math.Max(before,ThresholdRank(s.Reputation));return true;}
    }
    public sealed class GangTreasuryOptions : ExtensibleData
    {
        public int PaydayHours=24,MaxTransfer=1000000;
        [JsonIgnore] public bool Valid=>PaydayHours>=1 && PaydayHours<=168 && MaxTransfer>=100 && MaxTransfer<=10000000;
    }
    public sealed class GangTreasuryAccount : ExtensibleData
    {
        public int Balance;public long LastPaydayTicks,Deposited,Withdrawn,Salary,BusinessIncome;
        [JsonIgnore] public bool Valid=>Balance>=0 && LastPaydayTicks>=DateTime.MinValue.Ticks && LastPaydayTicks<=DateTime.MaxValue.Ticks && Deposited>=0 && Withdrawn>=0 && Salary>=0 && BusinessIncome>=0 && (decimal)Deposited+BusinessIncome-Withdrawn-Salary==Balance;
    }
    public sealed class GangTreasuryData : ExtensibleData
    {
        public Dictionary<string,GangTreasuryAccount> Accounts=new Dictionary<string,GangTreasuryAccount>();public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>Accounts!=null && Accounts.Count<=64 && Accounts.All(p=>!GangLoyaltyRules.Unaffiliated(p.Key) && p.Key.Length<=100 && p.Value!=null && p.Value.Valid) && Accounts.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count()==Accounts.Count && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=110) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public static class GangTreasuryTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        static string Key(GangTreasuryData b,string gang)=>b.Accounts.Keys.FirstOrDefault(k=>string.Equals(k,gang,StringComparison.OrdinalIgnoreCase))??gang;
        public static GangTreasuryAccount Account(GangTreasuryData b,string gang)=>b!=null && b.Valid && !string.IsNullOrWhiteSpace(gang) && b.Accounts.TryGetValue(Key(b,gang),out var account)?account:null;
        static bool Apply(WalletData w,string gang,string receipt,GangTreasuryOptions c,Func<GangTreasuryAccount,int?> mutate,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || c==null || !c.Valid || save==null || GangLoyaltyRules.Unaffiliated(gang) || gang.Length>100 || string.IsNullOrWhiteSpace(receipt) || receipt.Length>110 || w.GangTreasury!=null && !w.GangTreasury.Valid)return false;
            var original=w.GangTreasury;if(original?.Receipts.Contains(receipt)==true)return true;var next=original==null?new GangTreasuryData():Copy(original);string key=Key(next,gang);bool exists=next.Accounts.TryGetValue(key,out var a);if(!exists){if(next.Accounts.Count>=64)return false;next.Accounts[key]=a=new GangTreasuryAccount();}
            var delta=mutate(a);if(delta==null || (long)w.Bank+delta.Value<0 || (long)w.Bank+delta.Value>int.MaxValue || !next.Valid)return false;next.Receipts.Add(receipt);if(next.Receipts.Count>256)next.Receipts.RemoveAt(0);int bank=w.Bank;
            try{w.GangTreasury=next;w.Bank+=delta.Value;save();changed=true;return true;}catch{w.GangTreasury=original;w.Bank=bank;throw;}
        }
        static bool Id(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=80;
        public static bool Deposit(WalletData w,string gang,int amount,DateTime now,GangTreasuryOptions c,string id,Action save,out bool changed)
        {changed=false;if(!Id(id) || c==null || !c.Valid || amount<1 || amount>c.MaxTransfer)return false;bool fresh=Account(w?.GangTreasury,gang)==null;return Apply(w,gang,id+":deposit",c,a=>{if(a.Balance>int.MaxValue-amount || a.Deposited>long.MaxValue-amount)return null;a.Balance+=amount;a.Deposited+=amount;if(fresh)a.LastPaydayTicks=now.Ticks;return -amount;},save,out changed);}
        public static bool Withdraw(WalletData w,string gang,int amount,GangTreasuryOptions c,string id,Action save,out bool changed)
        {changed=false;if(!Id(id) || c==null || !c.Valid || amount<1 || amount>c.MaxTransfer)return false;return Apply(w,gang,id+":withdraw",c,a=>{if(a.Balance<amount || a.Withdrawn>long.MaxValue-amount)return null;a.Balance-=amount;a.Withdrawn+=amount;return amount;},save,out changed);}
        public static bool Payday(WalletData w,string gang,int rank,DateTime now,GangTreasuryOptions c,string id,Action save,out bool changed)
        {changed=false;if(!Id(id) || rank<1 || rank>4)return false;return Apply(w,gang,id+":payday",c,a=>{if(now.Ticks<a.LastPaydayTicks || now.Ticks-a.LastPaydayTicks<(long)c.PaydayHours*TimeSpan.TicksPerHour)return null;int amount=(int)((long)a.Balance*GangProgressionRules.Share(rank)/100);if(amount<=0 || a.Salary>long.MaxValue-amount)return null;a.Balance-=amount;a.Salary+=amount;a.LastPaydayTicks=now.Ticks;return amount;},save,out changed);}
    }
}
