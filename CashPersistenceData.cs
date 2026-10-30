using System;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class NativeCashCheckpoint : ExtensibleData
    {
        public int Version=1,Amount;
        public string Owner;
        [JsonIgnore] public bool Valid=>Version==1 && Amount>=0 && CashPersistenceRules.Stat(Owner)!=null;
    }
    public static class CashPersistenceRules
    {
        public static string Stat(string owner)=>owner=="Michael"?"SP0_TOTAL_CASH":owner=="Franklin"?"SP1_TOTAL_CASH":owner=="Trevor"?"SP2_TOTAL_CASH":null;
        public static bool Trusted(WalletData w,string owner)=>w?.NativeCash?.Valid==true && w.NativeCash.Owner==owner;
        public static void Save(WalletData w,string owner,int amount,Action save)
        {
            if(w==null || amount<0 || Stat(owner)==null || save==null)throw new ArgumentException();
            var old=w.NativeCash;int oldCash=w.Cash;
            if(old!=null && (!old.Valid || old.Owner!=owner))throw new InvalidOperationException("Native cash checkpoint belongs to another profile or is invalid; original retained.");
            var next=old==null?new NativeCashCheckpoint{Owner=owner}:JsonConvert.DeserializeObject<NativeCashCheckpoint>(JsonConvert.SerializeObject(old));
            next.Amount=amount;
            try{w.NativeCash=next;w.Cash=amount;save();}catch{w.NativeCash=old;w.Cash=oldCash;throw;}
        }
    }
    // A story save can load an older native balance than the bank checkpoint.
    // Until that checkpoint is restored, RPCore operations use the trusted amount.
    public sealed class NativeCashSession
    {
        readonly string owner;readonly Func<string,int> read;readonly Action<string,int> write;
        int pendingAmount,lastWritten;bool changed;
        public bool Pending{get;private set;}
        public NativeCashSession(WalletData w,string owner,Func<string,int> read,Action<string,int> write)
        {this.owner=owner;this.read=read;this.write=write;Pending=CashPersistenceRules.Trusted(w,owner);if(Pending)pendingAmount=w.NativeCash.Amount;}
        public int Cash
        {
            get{int value=Pending?pendingAmount:read(owner);if(value<0)throw new InvalidOperationException("Native cash unreadable.");return value;}
            set{if(value<0)throw new ArgumentOutOfRangeException();write(owner,value);if(read(owner)!=value)throw new InvalidOperationException("Native cash did not apply.");lastWritten=value;changed=true;if(Pending)pendingAmount=value;}
        }
        public int? Capture(bool ready)=>Pending?pendingAmount:ready?Cash:changed?(int?)lastWritten:null;
        public void Saved(){changed=false;}
        public bool Restore(bool ready,string activeOwner)
        {
            if(!Pending || !ready || activeOwner!=owner)return false;
            write(owner,pendingAmount);if(read(owner)!=pendingAmount)throw new InvalidOperationException("Native cash restore did not apply.");
            Pending=false;return true;
        }
    }
}
