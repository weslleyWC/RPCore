using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangLoyaltyOptions : ExtensibleData
    {
        public List<GangSpot> Contacts=new List<GangSpot>();
        [JsonIgnore] public bool Valid=>Contacts!=null && Contacts.Count>=2 && Contacts.Count<=100 && Contacts.All(GangPresenceRules.Position) && Contacts.Any(a=>Contacts.Any(b=>GangPresenceRules.Distance(a,b)>=3)) && Contacts.Select(p=>p.X+":"+p.Y+":"+p.Z).Distinct().Count()==Contacts.Count;
        public static GangLoyaltyOptions Defaults(){using(var s=typeof(GangLoyaltyOptions).Assembly.GetManifestResourceStream("RPCore.JobDefaults.GangLoyalty.json"))using(var r=new System.IO.StreamReader(s))return JsonConvert.DeserializeObject<GangLoyaltyOptions>(r.ReadToEnd());}
    }
    public sealed class GangLoyaltyAttempt : ExtensibleData
    {
        public string Id,Gang;public int Collected;public GangSpot Leader;public List<GangSpot> Contacts;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && !string.IsNullOrWhiteSpace(Gang) && Gang.Length<=100 && Collected>=0 && Collected<=2 && GangPresenceRules.Position(Leader) && Contacts!=null && Contacts.Count==2 && Contacts.All(GangPresenceRules.Position) && GangPresenceRules.Distance(Contacts[0],Contacts[1])>=3;
    }
    public sealed class GangLoyaltyData : ExtensibleData
    {
        public GangLoyaltyAttempt Pending;public int Completed;public string LastResult;public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>Completed>=0 && (Pending==null || Pending.Valid) && Receipts!=null && Receipts.Count<=128 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=100) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public static class GangLoyaltyRules
    {
        public static bool Unaffiliated(string gang)=>string.IsNullOrWhiteSpace(gang) || string.Equals(gang,"None",StringComparison.OrdinalIgnoreCase) || string.Equals(gang,"Unassigned",StringComparison.OrdinalIgnoreCase);
        public static bool Handover(float distance,float seconds,bool eligible)=>eligible && GangPresenceRules.Finite(distance) && distance>=0 && distance<=2.5f && GangPresenceRules.Finite(seconds) && seconds>=2.5f;
    }
    public static class GangLoyaltyTransactions
    {
        static T Copy<T>(T value)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        static bool Apply(GangState current,string receipt,Func<GangState,bool> change,Action<GangState> save,out GangState result)
        {
            result=current;if(current==null || save==null || string.IsNullOrWhiteSpace(receipt) || receipt.Length>100 || current.Loyalty!=null && !current.Loyalty.Valid)return false;
            if(current.Loyalty?.Receipts.Contains(receipt)==true)return true;
            var next=Copy(current);next.Loyalty=next.Loyalty??new GangLoyaltyData();if(!change(next) || !next.Loyalty.Valid)return false;
            next.Loyalty.Receipts.Add(receipt);if(next.Loyalty.Receipts.Count>128)next.Loyalty.Receipts.RemoveAt(0);
            save(next);result=next;return true;
        }
        public static bool Begin(GangState state,GangLoyaltyAttempt attempt,Action<GangState> save,out GangState result)
        {result=state;if(attempt==null || !attempt.Valid || attempt.Collected!=0)return false;return Apply(state,attempt.Id+":begin",s=>{if(!GangLoyaltyRules.Unaffiliated(s.Gang) || s.Loyalty.Pending!=null)return false;s.Loyalty.Pending=Copy(attempt);return true;},save,out result);}
        public static bool Collect(GangState state,string id,int index,Action<GangState> save,out GangState result)
        {result=state;if(index<0 || index>1)return false;return Apply(state,id+":pickup:"+index,s=>{var p=s.Loyalty.Pending;if(p==null || p.Id!=id || p.Collected!=index || !GangLoyaltyRules.Unaffiliated(s.Gang))return false;p.Collected++;return true;},save,out result);}
        public static bool Complete(GangState state,string id,Action<GangState> save,out GangState result)
            =>Apply(state,id+":complete",s=>{var p=s.Loyalty.Pending;if(p==null || p.Id!=id || p.Collected!=2 || !GangLoyaltyRules.Unaffiliated(s.Gang) || s.Loyalty.Completed==int.MaxValue)return false;s.Gang=p.Gang;s.Reputation=0;s.Loyalty.Pending=null;s.Loyalty.Completed++;s.Loyalty.LastResult="completed";return true;},save,out result);
        public static bool Cancel(GangState state,string id,Action<GangState> save,out GangState result)
            =>Apply(state,id+":cancel",s=>{if(s.Loyalty.Pending==null || s.Loyalty.Pending.Id!=id)return false;s.Loyalty.Pending=null;s.Loyalty.LastResult="cancelled";return true;},save,out result);
    }
}
