using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RPCore
{
    public class MarketData : ExtensibleData
    {
        public Dictionary<string,decimal> Prices=new Dictionary<string,decimal>{{"PIZ",25m},{"LTD",50m},{"AUTO",100m}};
    }
    public static class ExchangeRules
    {
        public static bool Valid(Portfolio p)=>p!=null && p.Cash>=0 && p.Holdings!=null && p.AvgCost!=null && p.Watchlist!=null && p.Holdings.All(h=>!string.IsNullOrWhiteSpace(h.Key) && h.Value>=0) && p.AvgCost.All(h=>!string.IsNullOrWhiteSpace(h.Key) && h.Value>=0) && p.Watchlist.All(s=>!string.IsNullOrWhiteSpace(s));
        public static bool Valid(MarketData m)=>m?.Prices!=null && m.Prices.All(q=>!string.IsNullOrWhiteSpace(q.Key) && q.Value>=0);
        public static bool Matches(Portfolio p,Portfolio expected)=>Valid(p) && Valid(expected) && JsonConvert.SerializeObject(p)==JsonConvert.SerializeObject(expected);
        public static int Maximum(Portfolio p,string symbol,decimal price,bool buy)
        {
            if(!Valid(p) || string.IsNullOrEmpty(symbol) || price<=0)return 0;
            p.Holdings.TryGetValue(symbol,out int held);
            if(buy && held>0 && !p.AvgCost.ContainsKey(symbol))return 0;
            int cap=buy?int.MaxValue-held:held;decimal funds=buy?p.Cash:decimal.MaxValue-p.Cash;
            if(cap<=0)return 0;if(funds/cap>=price)return cap;
            return (int)decimal.Floor(funds/price);
        }
        public static decimal NextPrice(decimal price,double movement)
        {
            if(price<1 || price>1000000 || double.IsNaN(movement) || double.IsInfinity(movement))return price;
            movement=Math.Max(-.02,Math.Min(.02,movement));return Math.Round(Math.Max(1,Math.Min(1000000,price*(1+(decimal)movement))),2);
        }
        public static decimal? Value(Portfolio p,MarketData m)
        {
            if(!Valid(p) || !Valid(m))return null;
            try{decimal value=0;foreach(var h in p.Holdings){if(!m.Prices.TryGetValue(h.Key,out decimal price) || price<=0){if(h.Value>0)return null;continue;}value=checked(value+price*h.Value);}return value;}
            catch(OverflowException){return null;}
        }
        public static decimal? Gain(Portfolio p,MarketData m,string symbol)
        {
            if(!Valid(p) || !Valid(m) || !p.Holdings.TryGetValue(symbol,out int held) || !p.AvgCost.TryGetValue(symbol,out decimal average) || !m.Prices.TryGetValue(symbol,out decimal price) || price<=0)return null;
            try{return checked((price-average)*held);}catch(OverflowException){return null;}
        }
        public static IEnumerable<string> List(Portfolio p,MarketData m,IEnumerable<string> symbols,string query,int sort,bool descending)
        {
            var rows=symbols.Distinct(StringComparer.Ordinal).Where(s=>s.IndexOf(query??"",StringComparison.OrdinalIgnoreCase)>=0);
            if(sort==0)return descending?rows.OrderByDescending(s=>s,StringComparer.Ordinal):rows.OrderBy(s=>s,StringComparer.Ordinal);
            Func<string,decimal?> value=s=>sort==1?(m.Prices.TryGetValue(s,out decimal price) && price>0?(decimal?)price:null):Gain(p,m,s);
            var quoted=rows.Where(s=>value(s).HasValue);var unquoted=rows.Where(s=>!value(s).HasValue).OrderBy(s=>s,StringComparer.Ordinal);
            return (descending?quoted.OrderByDescending(value).ThenBy(s=>s,StringComparer.Ordinal):quoted.OrderBy(value).ThenBy(s=>s,StringComparer.Ordinal)).Concat(unquoted);
        }
    }
    public static class ExchangeTransactions
    {
        public static bool Commit(WalletData w,Func<int> cash,Action<int> setCash,int cashDelta,Func<Portfolio,bool> change,Action save)
        {
            if(w==null || !ExchangeRules.Valid(w.Exchange) || cash==null || setCash==null || change==null || save==null)return false;
            int held=cash();long after=(long)held+cashDelta;if(held<0 || after<0 || after>int.MaxValue)return false;
            var old=w.Exchange;var next=JsonConvert.DeserializeObject<Portfolio>(JsonConvert.SerializeObject(old));
            if(!change(next) || !ExchangeRules.Valid(next))return false;
            try{w.Exchange=next;if(cashDelta!=0)setCash((int)after);save();return true;}
            catch{w.Exchange=old;if(cashDelta!=0)setCash(held);throw;}
        }
        public static bool Funds(WalletData w,Func<int> cash,Action<int> setCash,int amount,bool deposit,Action save)
        {
            if(amount<=0)return false;
            return Commit(w,cash,setCash,deposit?-amount:amount,p=>{
                if(deposit){if(p.Cash>decimal.MaxValue-amount)return false;p.Cash+=amount;}
                else{if(p.Cash<amount)return false;p.Cash-=amount;}return true;
            },save);
        }
    }
    public sealed class ExchangeClock
    {
        int elapsed;
        public bool Advance(int milliseconds,bool active)
        {if(!active || milliseconds<0 || milliseconds>1000)return false;elapsed+=milliseconds;if(elapsed<60000)return false;elapsed-=60000;return true;}
    }
}
