using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RPCore
{
    // Independent of the prison-transport job. Stored in the same atomic Wallet checkpoint.
    public sealed class CustodyOptions : ExtensibleData
    {
        public int SecondsPerStar=60, BailPerStar=500;
        public JobPoint Cell=new JobPoint{Name="Mission Row / cela",X=459.85f,Y=-994.38f,Z=24.91f,Heading=90};
        public JobPoint Exit=new JobPoint{Name="Mission Row / saida",X=425.12f,Y=-979.56f,Z=30.71f,Heading=90};
        public float Radius=5;
        [JsonIgnore] public bool Valid=>SecondsPerStar>=15 && SecondsPerStar<=600 && BailPerStar>=100 && BailPerStar<=10000 && Cell?.Valid==true && Exit?.Valid==true && GangPresenceRules.Finite(Radius) && Radius>=2 && Radius<=8 && Math.Abs(Cell.X-Exit.X)+Math.Abs(Cell.Y-Exit.Y)>20;
        public static CustodyOptions Defaults(){using(var s=typeof(CustodyOptions).Assembly.GetManifestResourceStream("RPCore.JobDefaults.Custody.json"))using(var r=new System.IO.StreamReader(s))return JsonConvert.DeserializeObject<CustodyOptions>(r.ReadToEnd());}
    }
    public sealed class CustodyCase : ExtensibleData
    {
        public string Id,Outcome="pending";public int Stars,TotalMilliseconds,RemainingMilliseconds,Bail;
        public CustodyOptions Options;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && Stars>=1 && Stars<=5 && Options?.Valid==true && TotalMilliseconds==Stars*Options.SecondsPerStar*1000 && Bail==Stars*Options.BailPerStar && RemainingMilliseconds>=0 && RemainingMilliseconds<=TotalMilliseconds && (Outcome=="pending" || Outcome=="served" && RemainingMilliseconds==0 || Outcome=="bailed");
    }
    public sealed class CustodyData : ExtensibleData
    {
        public bool Enabled;public long Booked,Served,Bailed,BailPaid;public CustodyCase Pending;
        public List<CustodyCase> History=new List<CustodyCase>();
        [JsonIgnore] public bool Valid=>Booked>=0 && Served>=0 && Bailed>=0 && BailPaid>=0 && (decimal)Served+Bailed+(Pending==null?0:1)==Booked && History!=null && (decimal)BailPaid>=History.Where(c=>c?.Outcome=="bailed").Sum(c=>(decimal)c.Bail) && BailPaid>=(decimal)Bailed*100 && (Pending==null || Pending.Valid && Pending.Outcome=="pending") && History!=null && History.Count<=32 && History.All(c=>c?.Valid==true && c.Outcome!="pending" && c.Id!=Pending?.Id) && History.Select(c=>c.Id).Distinct().Count()==History.Count && Served>=History.Count(c=>c.Outcome=="served") && Bailed>=History.Count(c=>c.Outcome=="bailed");
    }
    public static class CustodyRules
    {
        public static bool Surrender(bool human,bool alive,bool onFoot,bool unarmed,bool shooting,bool ragdoll,bool mission,int stars,float speed,bool copSeen)=>human && alive && onFoot && unarmed && !shooting && !ragdoll && !mission && stars>=1 && stars<=5 && GangPresenceRules.Finite(speed) && speed>=0 && speed<.5f && copSeen;
        public static int Credit(int delta,bool eligible)=>eligible && delta>=0 && delta<=1000?delta:0;
    }
    public static class CustodyTransactions
    {
        static T Copy<T>(T x)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(x));
        static bool Apply(WalletData w,Action save,Func<CustodyData,bool> change,out bool changed,int cost=0)
        {
            changed=false;if(w==null || save==null || cost<0 || w.Bank<cost || w.Custody!=null && !w.Custody.Valid)return false;
            var old=w.Custody;int bank=w.Bank;var n=old==null?new CustodyData():Copy(old);if(!change(n) || !n.Valid)return false;
            try{w.Custody=n;w.Bank-=cost;save();changed=true;return true;}catch{w.Custody=old;w.Bank=bank;throw;}
        }
        public static bool Configure(WalletData w,bool enabled,Action save,out bool changed)=>Apply(w,save,b=>{if(!enabled && b.Pending!=null)return false;b.Enabled=enabled;return true;},out changed);
        public static bool Book(WalletData w,string id,int stars,CustodyOptions options,Action save,out bool changed)
        {
            changed=false;if(options?.Valid!=true || stars<1 || stars>5 || string.IsNullOrWhiteSpace(id) || id.Length>80)return false;
            return Apply(w,save,b=>{if(!b.Enabled || b.Pending!=null || b.Booked==long.MaxValue || b.History.Any(c=>c.Id==id))return false;b.Pending=new CustodyCase{Id=id,Stars=stars,TotalMilliseconds=stars*options.SecondsPerStar*1000,RemainingMilliseconds=stars*options.SecondsPerStar*1000,Bail=stars*options.BailPerStar,Options=Copy(options)};b.Booked++;return true;},out changed);
        }
        // Absolute, monotone checkpoints make replay/reload unable to subtract time twice.
        public static bool Checkpoint(WalletData w,string id,int remaining,Action save,out bool changed)
        {
            changed=false;var c=w?.Custody?.Pending;if(w?.Custody?.Valid!=true || c==null || c.Id!=id || remaining<0 || remaining>c.RemainingMilliseconds)return false;if(remaining==c.RemainingMilliseconds)return true;
            return Apply(w,save,b=>{b.Pending.RemainingMilliseconds=remaining;return true;},out changed);
        }
        public static bool Release(WalletData w,string id,bool bail,Action save,out bool changed)
        {
            changed=false;if(w?.Custody?.Valid!=true || string.IsNullOrWhiteSpace(id))return false;string outcome=bail?"bailed":"served";var previous=w.Custody.History.FirstOrDefault(c=>c.Id==id);if(previous!=null)return previous.Outcome==outcome;
            var c=w.Custody.Pending;if(c?.Id!=id || (!bail && c.RemainingMilliseconds!=0) || bail && (w.Custody.Bailed==long.MaxValue || w.Custody.BailPaid>long.MaxValue-c.Bail) || !bail && w.Custody.Served==long.MaxValue)return false;
            return Apply(w,save,b=>{var done=b.Pending;done.Outcome=outcome;b.Pending=null;b.History.Add(done);if(b.History.Count>32)b.History.RemoveAt(0);if(bail){b.Bailed++;b.BailPaid+=done.Bail;}else b.Served++;return true;},out changed,bail?c.Bail:0);
        }
    }
}
