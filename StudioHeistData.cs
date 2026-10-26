using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class StudioHeistOptions : ExtensibleData
    {
        public int SecondsLimit=1800,MinReward=200000,MaxReward=450000;
        public string VanModel="burrito3",DocumentsModel="jugular",GetawayModel="locust",TruckModel="bison",TrailerModel="boattrailer2",DirectorCarModel="schafter5",EscapeBoatModel="dinghy3",BuyerBoatModel="longfin",GuardModel="s_m_m_security_01",DirectorModel="s_m_y_shop_mask",ContactModel="a_m_y_genstreet_01",BuyerModel="a_f_o_salton_01";
        public JobPoint Van,Lockup,Documents,DocumentsDrop,Getaway,Garage,Truck,BoatDrop,BagContact,Plant,PreparedCar,Gate,Parking,DirectorWalk,DirectorCar,EscapeBoat,BuyerBoat;
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)]public List<JobPoint> Directors=new List<JobPoint>();
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)]public List<JobPoint> SupplyGuards=new List<JobPoint>(),DockGuards=new List<JobPoint>(),StudioGuards=new List<JobPoint>();
        [JsonIgnore]public bool Valid=>SecondsLimit>=600 && SecondsLimit<=3600 && MinReward>=1000 && MaxReward>=MinReward && MaxReward<=2000000 && new[]{VanModel,DocumentsModel,GetawayModel,TruckModel,TrailerModel,DirectorCarModel,EscapeBoatModel,BuyerBoatModel,GuardModel,DirectorModel,ContactModel,BuyerModel}.All(GangPresenceRules.ModelName) && new[]{Van,Lockup,Documents,DocumentsDrop,Getaway,Garage,Truck,BoatDrop,BagContact,Plant,PreparedCar,Gate,Parking,DirectorWalk,DirectorCar,EscapeBoat,BuyerBoat}.All(p=>p?.Valid==true) && Directors!=null && Directors.Count>=1 && Directors.Count<=8 && Directors.All(p=>p?.Valid==true) && new[]{SupplyGuards,DockGuards,StudioGuards}.All(list=>list!=null && list.Count>=1 && list.Count<=24 && list.All(p=>p?.Valid==true));
        public static StudioHeistOptions Defaults(){using(var s=typeof(StudioHeistOptions).Assembly.GetManifestResourceStream("RPCore.JobDefaults.StudioHeist.json"))using(var r=new System.IO.StreamReader(s))return JsonConvert.DeserializeObject<StudioHeistOptions>(r.ReadToEnd());}
    }
    public enum StudioObjective { TakeVan,DeliverVan,Lockpick,TakeDocuments,DeliverDocuments,TakeGetaway,DeliverGetaway,TakeKeys,TakeTruck,DeliverTrailer,TakeBag,PlantBag,TakePreparedCar,GatePass,Park,FindDirector,FollowDirector,QuietTakedown,CollectKeys,TakeDevice,EscapeStudio,LosePolice,BoardEscapeBoat,DeliverDevice,Ready }
    public sealed class StudioHeistFlow
    {
        public readonly string Id;public readonly int Act;int step;
        static readonly StudioObjective[][] Steps={new[]{StudioObjective.TakeVan,StudioObjective.DeliverVan},new[]{StudioObjective.Lockpick,StudioObjective.TakeDocuments,StudioObjective.DeliverDocuments},new[]{StudioObjective.TakeGetaway,StudioObjective.DeliverGetaway},new[]{StudioObjective.TakeKeys,StudioObjective.TakeTruck,StudioObjective.DeliverTrailer},new[]{StudioObjective.TakeBag,StudioObjective.PlantBag},new[]{StudioObjective.TakePreparedCar,StudioObjective.GatePass,StudioObjective.Park,StudioObjective.FindDirector,StudioObjective.FollowDirector,StudioObjective.QuietTakedown,StudioObjective.CollectKeys,StudioObjective.TakeDevice,StudioObjective.EscapeStudio,StudioObjective.LosePolice,StudioObjective.BoardEscapeBoat,StudioObjective.DeliverDevice}};
        public StudioHeistFlow(string id,int act){if(string.IsNullOrWhiteSpace(id) || act<0 || act>5)throw new ArgumentException();Id=id;Act=act;}
        public StudioObjective Objective=>step<Steps[Act].Length?Steps[Act][step]:StudioObjective.Ready;
        public bool Advance(StudioObjective expected,bool verified){if(!verified || Objective!=expected || Objective==StudioObjective.Ready)return false;step++;return true;}
    }
    public sealed class StudioLockpick
    {
        public int Solved{get;private set;}public int Failures{get;private set;}public int Elapsed{get;private set;}
        public bool Complete=>Solved==3;public bool Failed=>Failures>=5;
        public float Position=>Math.Min(1,Elapsed/(5000f-Solved*1000));
        public void Tick(int delta,bool eligible){if(Complete || Failed || !eligible || delta<0 || delta>1000)return;Elapsed+=delta;if(Position>=1)Miss();}
        void Miss(){Failures++;Solved=Elapsed=0;}
        public bool Press(){if(Complete || Failed)return false;if(Position>=.80f && Position<=.88f){Solved++;Elapsed=0;return true;}Miss();return false;}
    }
    public static class StudioHeistRules
    {
        static bool Number(float n)=>GangPresenceRules.Finite(n) && n>=0;
        public static bool Night(int hour)=>hour>=0 && hour<=23 && (hour>=21 || hour<5);
        public static bool Foot(bool ownActor,bool onFoot,float speed,float distance,float radius=2.5f)=>ownActor && onFoot && Number(speed) && speed<.5f && Number(distance) && Number(radius) && radius>0 && radius<=35 && distance<=radius;
        public static bool Delivery(bool ownDriver,bool ownVehicle,bool policeFree,float speed,float distance)=>ownDriver && ownVehicle && policeFree && Number(speed) && speed<.5f && Number(distance) && distance<=5;
        public static bool Sea(bool ownDriver,bool ownBoat,bool ownBuyer,bool policeFree,float speed,float distance)=>ownDriver && ownBoat && ownBuyer && policeFree && Number(speed) && speed<1.5f && Number(distance) && distance<=12;
    }
    public sealed class StudioHeistAttempt : ExtensibleData
    {
        public string Id;public int Act,Reward,Elapsed;public bool Ready,DiversionRequested,DiversionUsed;public StudioHeistOptions Options;
        [JsonIgnore]public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && Act>=0 && Act<=5 && Options?.Valid==true && Elapsed>=0 && Elapsed<=Options.SecondsLimit*1000 && (!DiversionUsed || DiversionRequested) && (Act==5 || !DiversionRequested && !DiversionUsed) && (!Ready || Elapsed<Options.SecondsLimit*1000) && (Act==5?Reward>=Options.MinReward && Reward<=Options.MaxReward:Reward==0);
    }
    public sealed class StudioHeistReceipt : ExtensibleData
    {
        public string Id,Outcome;public int Act,Amount;
        [JsonIgnore]public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && Act>=0 && Act<=5 && (Outcome=="prepared" && Act<5 && Amount==0 || Outcome=="paid" && Act==5 && Amount>=1000 && Amount<=2000000 || (Outcome=="failed" || Outcome=="cancelled") && Amount==0);
    }
    public sealed class StudioHeistData : ExtensibleData
    {
        public int Setups;public long Started,Completed,Failed,Cancelled,Heists,Earned;public StudioHeistAttempt Pending;
        public List<StudioHeistReceipt> History=new List<StudioHeistReceipt>();
        [JsonIgnore]public bool Valid=>Setups>=0 && Setups<=5 && Started>=0 && Completed>=0 && Failed>=0 && Cancelled>=0 && Heists>=0 && Earned>=(decimal)Heists*1000 && (decimal)Completed==6*(decimal)Heists+Setups && (decimal)Started==(decimal)Completed+Failed+Cancelled+(Pending==null?0:1) && (Pending==null || Pending.Valid && Pending.Act==Setups) && History!=null && History.Count<=32 && History.All(h=>h?.Valid==true && h.Id!=Pending?.Id) && History.Select(h=>h.Id).Distinct().Count()==History.Count && Completed>=History.Count(h=>h.Outcome=="prepared" || h.Outcome=="paid") && Heists>=History.Count(h=>h.Outcome=="paid") && Earned>=History.Sum(h=>(decimal)h.Amount) && Failed>=History.Count(h=>h.Outcome=="failed") && Cancelled>=History.Count(h=>h.Outcome=="cancelled");
    }
    public static class StudioHeistTransactions
    {
        static T Copy<T>(T value)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        static bool Apply(WalletData w,Action save,Func<StudioHeistData,bool> change,out bool changed,int credit=0)
        {
            changed=false;if(w==null || w.Bank<0 || credit<0 || w.Bank>int.MaxValue-credit || save==null || w.StudioHeist!=null && !w.StudioHeist.Valid)return false;var old=w.StudioHeist;int bank=w.Bank;var n=old==null?new StudioHeistData():Copy(old);if(!change(n) || !n.Valid)return false;
            try{w.StudioHeist=n;w.Bank+=credit;save();changed=true;return true;}catch{w.StudioHeist=old;w.Bank=bank;throw;}
        }
        public static bool Begin(WalletData w,string id,int act,StudioHeistOptions c,int reward,Action save,out bool changed)
        {changed=false;if(c?.Valid!=true || string.IsNullOrWhiteSpace(id) || id.Length>80 || act<0 || act>5 || (act==5?reward<c.MinReward || reward>c.MaxReward:reward!=0))return false;return Apply(w,save,b=>{if(b.Pending!=null || b.Setups!=act || b.Started==long.MaxValue || b.History.Any(h=>h.Id==id))return false;b.Pending=new StudioHeistAttempt{Id=id,Act=act,Reward=reward,Options=Copy(c)};b.Started++;return true;},out changed);}
        public static bool Checkpoint(WalletData w,string id,int elapsed,Action save,out bool changed)
        {changed=false;var p=w?.StudioHeist?.Pending;if(w?.StudioHeist?.Valid!=true || p==null || p.Id!=id || p.Ready || elapsed<p.Elapsed || elapsed>p.Options.SecondsLimit*1000)return false;if(elapsed==p.Elapsed)return true;return Apply(w,save,b=>{b.Pending.Elapsed=elapsed;return true;},out changed);}
        public static bool Ready(WalletData w,StudioHeistFlow flow,Action save,out bool changed)
        {changed=false;var p=w?.StudioHeist?.Pending;if(w?.StudioHeist?.Valid!=true || p==null || flow==null || flow.Id!=p.Id || flow.Act!=p.Act || flow.Objective!=StudioObjective.Ready || p.Elapsed>=p.Options.SecondsLimit*1000)return false;if(p.Ready)return true;return Apply(w,save,b=>{b.Pending.Ready=true;return true;},out changed);}
        public static bool Diversion(WalletData w,StudioHeistFlow flow,bool complete,Action save,out bool changed)
        {
            changed=false;var p=w?.StudioHeist?.Pending;if(w?.StudioHeist?.Valid!=true || p==null || p.Ready || p.Act!=5 || flow?.Id!=p.Id || flow.Act!=5 || (flow.Objective!=StudioObjective.EscapeStudio && flow.Objective!=StudioObjective.LosePolice))return false;
            if(complete?p.DiversionUsed:p.DiversionRequested)return true;if(complete && !p.DiversionRequested)return false;
            return Apply(w,save,b=>{if(complete)b.Pending.DiversionUsed=true;else b.Pending.DiversionRequested=true;return true;},out changed);
        }
        public static bool Finish(WalletData w,string id,string outcome,Action save,out bool changed)
        {
            changed=false;if(w?.StudioHeist?.Valid!=true || string.IsNullOrWhiteSpace(id))return false;var old=w.StudioHeist.History.FirstOrDefault(h=>h.Id==id);if(old!=null)return old.Outcome==outcome;
            var p=w.StudioHeist.Pending;if(p==null || p.Id!=id || (outcome=="prepared"?p.Act==5 || !p.Ready:outcome=="paid"?p.Act!=5 || !p.Ready:outcome!="failed" && outcome!="cancelled"))return false;
            int reward=outcome=="paid"?p.Reward:0;
            return Apply(w,save,b=>{if(outcome=="prepared" || outcome=="paid"){if(b.Completed==long.MaxValue || outcome=="paid" && (b.Heists==long.MaxValue || b.Earned>long.MaxValue-reward))return false;b.Completed++;if(outcome=="paid"){b.Heists++;b.Earned+=reward;b.Setups=0;}else b.Setups++;}else if(outcome=="failed"){if(b.Failed==long.MaxValue)return false;b.Failed++;}else{if(b.Cancelled==long.MaxValue)return false;b.Cancelled++;}b.History.Add(new StudioHeistReceipt{Id=id,Act=p.Act,Outcome=outcome,Amount=reward});if(b.History.Count>32)b.History.RemoveAt(0);b.Pending=null;return true;},out changed,reward);
        }
    }
}
