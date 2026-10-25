using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangStrengthOptions : ExtensibleData
    {
        public int FighterHealth=150,YieldHealth=100,ArenaRadius=35,SecondsLimit=300;
        [JsonIgnore] public bool Valid=>YieldHealth>=50 && YieldHealth<=150 && FighterHealth>YieldHealth && FighterHealth<=300 && ArenaRadius>=15 && ArenaRadius<=60 && SecondsLimit>=60 && SecondsLimit<=1200;
    }
    public sealed class GangStrengthAttempt : ExtensibleData
    {
        public string Id,Gang;public GangSpot Center;public string[] Models;public GangStrengthOptions Options;
        public List<int> Defeated=new List<int>();public bool Report;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && !string.IsNullOrWhiteSpace(Gang) && Gang.Length<=100 && GangPresenceRules.Position(Center) && Models!=null && Models.Length==3 && Models.All(GangPresenceRules.ModelName) && Options!=null && Options.Valid && Defeated!=null && Defeated.Count<=3 && Defeated.All(i=>i>=0 && i<3) && Defeated.Distinct().Count()==Defeated.Count && Report==(Defeated.Count==3);
    }
    public sealed class GangStrengthData : ExtensibleData
    {
        public int Attempts,Completed;public GangStrengthAttempt Pending;public string LastResult;public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>Attempts>=0 && Completed>=0 && Completed<=Attempts && (Pending==null || Pending.Valid && Attempts>Completed) && Receipts!=null && Receipts.Count<=128 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=110) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public static class GangStrengthRules
    {
        public static bool PlayerCanFight(int health,float distance,bool unarmed,bool onFoot,bool safe,GangStrengthOptions c)=>c!=null && c.Valid && health>100 && GangPresenceRules.Finite(distance) && distance>=0 && distance<=c.ArenaRadius && unarmed && onFoot && safe;
        public static bool Defeated(int health,bool dead,bool playerDamage,bool unarmed,GangStrengthOptions c)=>c!=null && c.Valid && (dead || health<=c.YieldHealth) && playerDamage && unarmed;
    }
    public static class GangStrengthTransactions
    {
        static T Copy<T>(T value)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        static bool Apply(GangState state,string receipt,Func<GangState,bool> change,Action<GangState> save,out GangState result)
        {
            result=state;if(state==null || save==null || string.IsNullOrWhiteSpace(receipt) || receipt.Length>110 || state.Strength!=null && !state.Strength.Valid)return false;
            if(state.Strength?.Receipts.Contains(receipt)==true)return true;
            var next=Copy(state);next.Strength=next.Strength??new GangStrengthData();if(!change(next) || !next.Strength.Valid)return false;
            next.Strength.Receipts.Add(receipt);if(next.Strength.Receipts.Count>128)next.Strength.Receipts.RemoveAt(0);save(next);result=next;return true;
        }
        public static bool Begin(GangState state,GangStrengthAttempt p,Action<GangState> save,out GangState result)
        {result=state;if(p==null || !p.Valid || p.Report || p.Defeated.Count!=0)return false;return Apply(state,p.Id+":begin",s=>{if(!GangLoyaltyRules.Unaffiliated(s.Gang) || s.Strength.Pending!=null || s.Loyalty?.Pending!=null || s.Driving?.Pending!=null || s.Strength.Attempts==int.MaxValue)return false;s.Strength.Pending=Copy(p);s.Strength.Attempts++;return true;},save,out result);}
        public static bool Defeat(GangState state,string id,int index,Action<GangState> save,out GangState result)
        {result=state;if(index<0 || index>2)return false;return Apply(state,Guid.NewGuid().ToString("N")+":defeat",s=>{var p=s.Strength.Pending;if(p==null || p.Id!=id || p.Report || p.Defeated.Contains(index) || !GangLoyaltyRules.Unaffiliated(s.Gang))return false;p.Defeated.Add(index);p.Report=p.Defeated.Count==3;return true;},save,out result);}
        public static bool Restart(GangState state,string id,Action<GangState> save,out GangState result)=>Apply(state,Guid.NewGuid().ToString("N")+":restart",s=>{var p=s.Strength.Pending;if(p==null || p.Id!=id || p.Report || !GangLoyaltyRules.Unaffiliated(s.Gang))return false;p.Defeated.Clear();return true;},save,out result);
        public static bool Complete(GangState state,string id,Action<GangState> save,out GangState result)=>Apply(state,id+":complete",s=>{var p=s.Strength.Pending;if(p==null || p.Id!=id || !p.Report || !GangLoyaltyRules.Unaffiliated(s.Gang) || s.Strength.Completed==int.MaxValue)return false;s.Gang=p.Gang;s.Reputation=0;s.Strength.Pending=null;s.Strength.Completed++;s.Strength.LastResult="completed";return true;},save,out result);
        public static bool End(GangState state,string id,bool failed,Action<GangState> save,out GangState result)=>Apply(state,id+":end",s=>{if(s.Strength.Pending==null || s.Strength.Pending.Id!=id)return false;s.Strength.Pending=null;s.Strength.LastResult=failed?"failed":"cancelled";return true;},save,out result);
    }
}
