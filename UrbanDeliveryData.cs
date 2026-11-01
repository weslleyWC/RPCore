using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public enum UrbanStopResult { Pending,Delivered,Skipped }
    public sealed class UrbanStop : ExtensibleData
    {
        public JobPoint Address;public int Pay,TimelyBonus,SecondsAllowed;public UrbanStopResult Result;
        [JsonIgnore] public bool Valid=>Address?.Valid==true && Pay>=0 && Pay<=1000000 && TimelyBonus>=0 && TimelyBonus<=1000000 && SecondsAllowed>=30 && SecondsAllowed<=3600 && Enum.IsDefined(typeof(UrbanStopResult),Result);
    }
    public sealed class UrbanDeliveryContract : ExtensibleData
    {
        public string Id,Job;public JobPoint Office,Depot;public List<UrbanStop> Stops=new List<UrbanStop>();
        public int Index,Delivered,Skipped,Quality=100,StopSeconds,ElapsedSeconds,LoadSeconds=3,ReturnBonus;
        public long Paid;public bool Bank=true,Loaded,PersonalVehicle;
        [JsonIgnore] public bool Returning=>Stops!=null && Index==Stops.Count;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=100 && UrbanDeliveryRules.Job(Job) && (!PersonalVehicle || Job=="postop") && Office?.Valid==true && Depot?.Valid==true && Stops!=null && Stops.Count>=1 && Stops.Count<=(Job=="pizza"?100:30) && Stops.All(s=>s?.Valid==true) && Stops.Select(s=>s.Address.X+":"+s.Address.Y+":"+s.Address.Z).Distinct().Count()==Stops.Count && Index>=0 && Index<=Stops.Count && Delivered>=0 && Skipped>=0 && Delivered+Skipped==Index && Stops.Take(Index).All(s=>s.Result!=UrbanStopResult.Pending) && Stops.Skip(Index).All(s=>s.Result==UrbanStopResult.Pending) && Stops.Count(s=>s.Result==UrbanStopResult.Delivered)==Delivered && Stops.Count(s=>s.Result==UrbanStopResult.Skipped)==Skipped && (Job=="postop" || Skipped==0) && Quality>=0 && Quality<=100 && StopSeconds>=0 && StopSeconds<=86400 && ElapsedSeconds>=StopSeconds && ElapsedSeconds<=10000000 && LoadSeconds>=2 && LoadSeconds<=20 && ReturnBonus>=0 && ReturnBonus<=1000000 && Paid>=0 && Paid<=(long)Index*1000000;
    }
    public sealed class UrbanDeliveryReport : ExtensibleData
    {
        public string Id,Job,Reason;public int Delivered,Skipped,Total,Seconds,Quality;public long Paid;public bool Bank=true,Completed;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && UrbanDeliveryRules.Job(Job) && Reason!=null && Reason.Length<=160 && Total>=1 && Total<=100 && Delivered>=0 && Skipped>=0 && Delivered+Skipped<=Total && Seconds>=0 && Quality>=0 && Quality<=100 && Paid>=0 && Paid<=(long)(Total+1)*1000000 && (!Completed || Delivered+Skipped==Total);
    }
    public static class UrbanDeliveryRules
    {
        public static bool Job(string job)=>job=="pizza" || job=="postop" || job=="supply";
        public static int Quote(UrbanDeliveryContract c)
        {if(c?.Valid!=true || c.Returning)return 0;var stop=c.Stops[c.Index];long value=(long)stop.Pay*(50+c.Quality/2)/100;bool late=c.StopSeconds>stop.SecondsAllowed;if(c.Job=="pizza"){if(late)value/=2;else value+=(long)stop.TimelyBonus*c.Quality/100;}return (int)Math.Min(1000000,value);}
        public static int Bonus(UrbanDeliveryContract c)=>c?.Valid!=true || !c.Returning?0:(int)((long)c.ReturnBonus*c.Delivered/c.Stops.Count*(50+c.Quality/2)/100);
        public static UrbanDeliveryReport Report(UrbanDeliveryContract c,bool completed,string reason)=>new UrbanDeliveryReport{Id=c.Id,Job=c.Job,Delivered=c.Delivered,Skipped=c.Skipped,Total=c.Stops.Count,Seconds=c.ElapsedSeconds,Quality=c.Quality,Paid=c.Paid,Completed=completed,Bank=c.Bank,Reason=reason};
    }
    public static class UrbanDeliveryTransactions
    {
        static T Copy<T>(T value)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        public static bool Read(WalletData wallet,string job,out DeliveryProgress stats)
        {
            stats=null;if(wallet==null || !UrbanDeliveryRules.Job(job) || wallet.Jobs!=null && wallet.Jobs.Jobs==null)return false;
            if(wallet.Jobs==null || !wallet.Jobs.Jobs.TryGetValue(job,out stats)){stats=new DeliveryProgress();return true;}
            return stats!=null && stats.Deliveries>=0 && stats.CompletedShifts>=0 && stats.Earned>=0 && stats.Receipts!=null && stats.RecentSessions!=null && (stats.UrbanContract==null || stats.UrbanContract.Valid && stats.UrbanContract.Job==job && (!stats.Receipts.TryGetValue(stats.UrbanContract.Id,out var paid)?stats.UrbanContract.Index==0:paid==stats.UrbanContract.Index)) && (stats.UrbanReport==null || stats.UrbanReport.Valid && stats.UrbanReport.Job==job);
        }
        static bool Apply(WalletData w,string job,Func<DeliveryProgress,bool> mutate,Action save,out bool changed)
        {
            changed=false;if(save==null || !Read(w,job,out _))return false;var before=w.Jobs;var next=before==null?new JobLedger():Copy(before);var stats=next.Get(job);if(!mutate(stats))return false;
            var candidate=new WalletData{Jobs=next};if(!Read(candidate,job,out _))return false;
            try{w.Jobs=next;save();changed=true;return true;}catch{w.Jobs=before;throw;}
        }
        public static bool Begin(WalletData w,UrbanDeliveryContract plan,Action save,out bool changed)
        {changed=false;if(plan?.Valid!=true || plan.Index!=0 || plan.Loaded || plan.Paid!=0 || plan.ElapsedSeconds!=0 || plan.StopSeconds!=0)return false;return Apply(w,plan.Job,s=>{if(s.UrbanContract!=null || s.Receipts.ContainsKey(plan.Id))return false;s.UrbanContract=Copy(plan);return true;},save,out changed);}
        public static bool Load(WalletData w,string job,string id,Action save,out bool changed)=>Apply(w,job,s=>{var c=s.UrbanContract;if(c==null || c.Id!=id || c.Returning || c.Loaded)return false;c.Loaded=true;return true;},save,out changed);
        public static bool ReloadCargo(WalletData w,string job,string id,Action save,out bool changed)=>Apply(w,job,s=>{var c=s.UrbanContract;if(c==null || c.Id!=id)return false;c.Loaded=false;return true;},save,out changed);
        public static bool Advance(WalletData w,string job,string id,int seconds,int qualityLoss,Action save,out bool changed)
        {changed=false;if(seconds<0 || seconds>30 || qualityLoss<0 || qualityLoss>100 || seconds==0 && qualityLoss==0)return false;return Apply(w,job,s=>{var c=s.UrbanContract;if(c==null || c.Id!=id)return false;c.ElapsedSeconds=Math.Min(10000000,c.ElapsedSeconds+seconds);if(c.Loaded && !c.Returning)c.StopSeconds=Math.Min(86400,c.StopSeconds+seconds);c.Quality=Math.Max(0,c.Quality-qualityLoss);return true;},save,out changed);}
        public static bool Drop(WalletData w,Func<int> cash,Action<int> setCash,string job,string id,int expectedIndex,bool skip,Action save,out bool changed)
        {
            changed=false;if(string.IsNullOrWhiteSpace(id) || expectedIndex<0 || expectedIndex>=100 || save==null || !Read(w,job,out var s))return false;if(s.Receipts.TryGetValue(id,out int prior) && prior>=expectedIndex+1)return true;var c=s.UrbanContract;if(c==null || c.Id!=id || c.Index!=expectedIndex || !c.Loaded || c.Returning || skip && job!="postop")return false;
            int amount=skip?0:UrbanDeliveryRules.Quote(c),step=c.Index+1;var next=Copy(c);next.Stops[next.Index].Result=skip?UrbanStopResult.Skipped:UrbanStopResult.Delivered;next.Index++;if(skip)next.Skipped++;else next.Delivered++;next.Paid+=amount;next.StopSeconds=0;
            return DeliveryPayments.Commit(w,cash,setCash,job,id,step,amount,c.Bank,!skip,false,()=>{w.Jobs.Get(job).UrbanContract=next;save();},out changed);
        }
        public static bool Return(WalletData w,Func<int> cash,Action<int> setCash,string job,string id,Action save,out bool changed,UrbanDeliveryContract nextRoute=null)
        {
            changed=false;if(string.IsNullOrWhiteSpace(id) || save==null || !Read(w,job,out var s) || nextRoute!=null && (job!="pizza" || !nextRoute.Valid || nextRoute.Job!=job || nextRoute.Index!=0 || nextRoute.Paid!=0 || nextRoute.Loaded || nextRoute.ElapsedSeconds!=0 || nextRoute.StopSeconds!=0 || s.Receipts.ContainsKey(nextRoute.Id) || nextRoute.Id==id))return false;var c=s.UrbanContract;if(c==null || c.Id!=id || !c.Returning)return false;int amount=UrbanDeliveryRules.Bonus(c);var next=Copy(c);next.Paid+=amount;
            return DeliveryPayments.Commit(w,cash,setCash,job,id,c.Stops.Count+1,amount,c.Bank,false,c.Delivered>0,()=>{var stats=w.Jobs.Get(job);stats.UrbanReport=UrbanDeliveryRules.Report(next,true,"Veiculo devolvido; rota encerrada");stats.UrbanContract=nextRoute==null?null:Copy(nextRoute);save();},out changed);
        }
        public static bool Cancel(WalletData w,string job,string id,string reason,Action save,out bool changed)
        {changed=false;if(reason==null || reason.Length>160)return false;return Apply(w,job,s=>{var c=s.UrbanContract;if(c==null || c.Id!=id)return false;s.UrbanReport=UrbanDeliveryRules.Report(c,false,reason);s.UrbanContract=null;return true;},save,out changed);}
    }
}
