using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangDiplomacyOptions : ExtensibleData
    {
        public int AgreementPrice=1000,CooldownHours=24;
        [JsonIgnore] public bool Valid=>AgreementPrice>=1 && AgreementPrice<=1000000 && CooldownHours>=1 && CooldownHours<=168;
    }
    // Directed attitude: Rival's opinion of Gang. Never rewrites shared gang definitions.
    public sealed class GangDiplomacyAccount : ExtensibleData
    {
        public string Gang,Rival,Attitude="Neutral";
        public long StolenLoads,Paid,LastAgreementTicks,BulkDeals;public int Agreements,Declarations;
        [JsonIgnore] public bool Valid=>GangDiplomacyRules.Pair(Gang,Rival) && GangDiplomacyRules.Known(Attitude) && StolenLoads>=0 && BulkDeals>=0 && Declarations>=0 && Agreements>=0 && Paid>=Agreements && LastAgreementTicks>=0 && LastAgreementTicks<=DateTime.MaxValue.Ticks && (Agreements!=0 || Paid==0 && LastAgreementTicks==0);
    }
    public sealed class GangDiplomacyData : ExtensibleData
    {
        public List<GangDiplomacyAccount> Accounts=new List<GangDiplomacyAccount>();
        public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>Accounts!=null && Accounts.Count<=1024 && Accounts.All(a=>a!=null && a.Valid) && Accounts.Select(a=>JsonConvert.SerializeObject(new[]{a.Gang.ToUpperInvariant(),a.Rival.ToUpperInvariant()})).Distinct().Count()==Accounts.Count && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=120) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public static class GangDiplomacyRules
    {
        public static bool Pair(string gang,string rival)=>!GangLoyaltyRules.Unaffiliated(gang) && gang.Length<=100 && !GangLoyaltyRules.Unaffiliated(rival) && rival.Length<=100 && !GangCrewRules.SameGang(gang,rival);
        public static bool Known(string value)=>value=="Respect" || value=="Neutral" || value=="Hate" || value=="War";
        public static string Worsen(string value)=>value=="Respect"?"Neutral":value=="Hate" || value=="War"?"War":"Hate";
        public static string Improve(string value)=>value=="War"?"Hate":value=="Hate"?"Neutral":value=="Neutral" || value=="Respect"?"Respect":"Neutral";
        public static string Label(string value)=>value=="Respect"?"Respeito":value=="Hate"?"Hostilidade":value=="War"?"Guerra":"Neutro";
        public static string Initial(GangDefinition rival,string gang)
        {
            var matches=rival?.Relationships?.Where(p=>GangCrewRules.SameGang(p.Key,gang)).ToList();
            return matches?.Count==1 && Known(matches[0].Value)?matches[0].Value:"Neutral";
        }
        public static GangDiplomacyAccount Account(GangDiplomacyData book,string gang,string rival)=>book?.Valid==true && Pair(gang,rival)?book.Accounts.FirstOrDefault(a=>GangCrewRules.SameGang(a.Gang,gang) && GangCrewRules.SameGang(a.Rival,rival)):null;
        public static string Attitude(GangDiplomacyData book,string gang,GangDefinition rival)=>Account(book,gang,rival?.Name)?.Attitude??Initial(rival,gang);
        internal static GangDiplomacyData Copy(GangDiplomacyData book)=>book==null?new GangDiplomacyData():JsonConvert.DeserializeObject<GangDiplomacyData>(JsonConvert.SerializeObject(book));
        internal static GangDiplomacyAccount Ensure(GangDiplomacyData book,string gang,string rival,string initial)
        {var a=Account(book,gang,rival);if(a!=null)return a;if(book.Accounts.Count>=1024)return null;a=new GangDiplomacyAccount{Gang=gang,Rival=rival,Attitude=initial};book.Accounts.Add(a);return a;}
        internal static void Receipt(GangDiplomacyData book,string id){book.Receipts.Add(id);if(book.Receipts.Count>256)book.Receipts.RemoveAt(0);}
        // Called inside the raid checkpoint's single Wallet save; never saves independently.
        internal static GangDiplomacyData Stolen(GangDiplomacyData original,string gang,string rival,string initial)
        {
            if(!Pair(gang,rival) || !Known(initial) || original!=null && !original.Valid)return null;
            var next=Copy(original);var a=Ensure(next,gang,rival,initial);if(a==null || a.StolenLoads==long.MaxValue)return null;
            a.Attitude=Worsen(a.Attitude);a.StolenLoads++;return next.Valid?next:null;
        }
    }
    public static class GangDiplomacyTransactions
    {
        public static bool DeclareWar(WalletData w,GangState state,string rival,string initial,string id,Action save,out bool changed)
        {
            changed=false;
            if(w==null || state==null || !GangProgressionRules.Valid(state) || !GangWarRules.Free(state) || !GangDiplomacyRules.Pair(state.Gang,rival) || !GangDiplomacyRules.Known(initial) || save==null || string.IsNullOrWhiteSpace(id) || id.Length>80 || w.GangDiplomacy!=null && !w.GangDiplomacy.Valid)return false;
            var old=w.GangDiplomacy;string receipt=id+":declaration";if(old?.Receipts.Contains(receipt)==true)return true;
            if(GangBulkTransactions.Blocked(w) || w.GangRaid!=null && (!w.GangRaid.Valid || w.GangRaid.Pending!=null) || w.GangBusiness!=null && (!w.GangBusiness.Valid || w.GangBusiness.Supply!=null))return false;
            var next=GangDiplomacyRules.Copy(old);var a=GangDiplomacyRules.Ensure(next,state.Gang,rival,initial);if(a==null || a.Attitude=="War" || a.Declarations==int.MaxValue)return false;
            a.Attitude="War";a.Declarations++;GangDiplomacyRules.Receipt(next,receipt);if(!next.Valid)return false;
            try{w.GangDiplomacy=next;save();changed=true;return true;}catch{w.GangDiplomacy=old;throw;}
        }
        public static bool Negotiate(WalletData w,GangState state,string rival,string initial,GangDiplomacyOptions c,DateTime now,string id,Action save,out bool changed)
        {
            changed=false;
            if(w==null || w.Bank<0 || state==null || !GangProgressionRules.Valid(state) || !GangDiplomacyRules.Pair(state.Gang,rival) || !GangDiplomacyRules.Known(initial) || c?.Valid!=true || save==null || string.IsNullOrWhiteSpace(id) || id.Length>80 || w.GangDiplomacy!=null && !w.GangDiplomacy.Valid)return false;
            string receipt=id+":agreement";var old=w.GangDiplomacy;if(old?.Receipts.Contains(receipt)==true)return true;
            if(GangBulkTransactions.Blocked(w) || w.GangRaid!=null && (!w.GangRaid.Valid || w.GangRaid.Pending!=null) || w.GangBusiness!=null && (!w.GangBusiness.Valid || w.GangBusiness.Supply!=null) || w.Bank<c.AgreementPrice)return false;
            var next=GangDiplomacyRules.Copy(old);var a=GangDiplomacyRules.Ensure(next,state.Gang,rival,initial);
            if(a==null || a.Attitude=="Respect" || a.Agreements==int.MaxValue || a.Paid>long.MaxValue-c.AgreementPrice || a.Agreements>0 && (now.Ticks<a.LastAgreementTicks || now.Ticks-a.LastAgreementTicks<(long)c.CooldownHours*TimeSpan.TicksPerHour))return false;
            a.Attitude=GangDiplomacyRules.Improve(a.Attitude);a.Agreements++;a.Paid+=c.AgreementPrice;a.LastAgreementTicks=now.Ticks;GangDiplomacyRules.Receipt(next,receipt);
            if(!next.Valid)return false;int bank=w.Bank;
            try{w.GangDiplomacy=next;w.Bank-=c.AgreementPrice;save();changed=true;return true;}catch{w.GangDiplomacy=old;w.Bank=bank;throw;}
        }
    }
}
