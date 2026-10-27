using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class PassengerOptions : ExtensibleData
    {
        public string TaxiModel="taxi",DriverModel="a_m_y_business_01",LimoModel="stretch",LimoDriverModel="s_m_m_highsec_01";
        public int TaxiBudget=500,LimoBudget=2000,MaxMinutes=30,PickupSeconds=180;
        [JsonIgnore]public bool Valid=>new[]{TaxiModel,DriverModel,LimoModel,LimoDriverModel}.All(GangPresenceRules.ModelName) && TaxiBudget>=2 && TaxiBudget<=1000000 && LimoBudget>=150 && LimoBudget<=1000000 && MaxMinutes>=1 && MaxMinutes<=60 && PickupSeconds>=30 && PickupSeconds<=600;
        public static PassengerOptions Defaults(){using(var s=typeof(PassengerOptions).Assembly.GetManifestResourceStream("RPCore.JobDefaults.Passenger.json"))using(var r=new System.IO.StreamReader(s))return JsonConvert.DeserializeObject<PassengerOptions>(r.ReadToEnd());}
    }
    public sealed class PassengerAttempt : ExtensibleData
    {
        public string Id;public bool Limo,Boarded;public int Budget,Elapsed;public PassengerOptions Options;
        [JsonIgnore]public int Fare=>!Boarded?0:PassengerRules.Fare(Limo,Elapsed,Budget);
        [JsonIgnore]public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && Options?.Valid==true && Budget==(Limo?Options.LimoBudget:Options.TaxiBudget) && Elapsed>=0 && Elapsed<=Options.MaxMinutes*60000 && (Boarded || Elapsed==0);
    }
    public sealed class PassengerRecord : ExtensibleData
    {public string Id,Outcome;public bool Limo;public int Fare,Elapsed;[JsonIgnore]public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && new[]{"completed","cancelled","interrupted"}.Contains(Outcome) && Fare>=0 && Fare<=1000000 && Elapsed>=0 && Elapsed<=3600000;}
    public sealed class PassengerData : ExtensibleData
    {
        public PassengerAttempt Pending;public SavedVehicle CustomTaxi;public long Trips,Spent;public List<PassengerRecord> History=new List<PassengerRecord>();
        [JsonIgnore]public bool Valid=>Trips>=0 && Spent>=0 && (Pending==null || Pending.Valid) && (CustomTaxi==null || VehicleInsuranceRules.Snapshot(CustomTaxi)) && History!=null && History.Count<=32 && History.All(h=>h?.Valid==true && h.Id!=Pending?.Id) && History.Select(h=>h.Id).Distinct().Count()==History.Count && Trips>=History.Count && (decimal)Spent>=History.Sum(h=>(decimal)h.Fare);
    }
    public static class PassengerRules
    {
        public static int Fare(bool limo,int milliseconds,int budget)
        {if(milliseconds<0 || milliseconds>3600000 || budget<(limo?150:2))throw new ArgumentOutOfRangeException();return Math.Min(budget,(limo?150:2)+(milliseconds/15000)*(limo?1:5));}
        public static int Delta(int now,int previous,bool active){int delta=unchecked(now-previous);return active && delta>=0 && delta<=1000?delta:0;}
        public static bool Arrival(bool passenger,bool usable,float speed,float distance)=>passenger && usable && GangPresenceRules.Finite(speed) && speed>=0 && speed<.5f && GangPresenceRules.Finite(distance) && distance>=0 && distance<=10;
    }
    public static class PassengerTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        static bool Apply(WalletData w,Action save,Func<PassengerData,int?> change)
        {
            if(w==null || w.Bank<0 || save==null || w.Passenger!=null && !w.Passenger.Valid)return false;
            var old=w.Passenger;var next=old==null?new PassengerData():Copy(old);int? delta=change(next);if(!delta.HasValue || (long)w.Bank+delta.Value<0 || (long)w.Bank+delta.Value>int.MaxValue || !next.Valid)return false;
            int bank=w.Bank;try{w.Passenger=next;w.Bank+=delta.Value;save();return true;}catch{w.Passenger=old;w.Bank=bank;throw;}
        }
        public static bool Begin(WalletData w,string id,bool limo,PassengerOptions c,Action save)
        {
            if(string.IsNullOrWhiteSpace(id) || id.Length>80 || c?.Valid!=true)return false;
            if(w?.Passenger?.Valid==true && w.Passenger.Pending?.Id==id)return w.Passenger.Pending.Limo==limo;
            return Apply(w,save,b=>{if(b.Pending!=null || b.History.Any(h=>h.Id==id))return null;b.Pending=new PassengerAttempt{Id=id,Limo=limo,Budget=limo?c.LimoBudget:c.TaxiBudget,Options=Copy(c)};return -b.Pending.Budget;});
        }
        public static bool Board(WalletData w,string id,Action save)
        {if(string.IsNullOrWhiteSpace(id) || id.Length>80)return false;if(w?.Passenger?.Valid==true && w.Passenger.Pending?.Id==id && w.Passenger.Pending.Boarded)return true;return Apply(w,save,b=>{if(b.Pending?.Id!=id)return null;b.Pending.Boarded=true;return 0;});}
        public static bool Checkpoint(WalletData w,string id,int elapsed,Action save)
        {
            if(string.IsNullOrWhiteSpace(id) || id.Length>80)return false;var p=w?.Passenger?.Pending;if(w?.Passenger?.Valid!=true || p?.Id!=id || !p.Boarded || elapsed<p.Elapsed || elapsed>p.Options.MaxMinutes*60000 || elapsed-p.Elapsed!=0 && elapsed-p.Elapsed!=15000)return false;
            if(elapsed==p.Elapsed)return true;return Apply(w,save,b=>{b.Pending.Elapsed=elapsed;return 0;});
        }
        public static bool Finish(WalletData w,string id,string outcome,Action save)
        {
            if(string.IsNullOrWhiteSpace(id) || id.Length>80 || w?.Passenger?.Valid!=true || !new[]{"completed","cancelled","interrupted"}.Contains(outcome))return false;
            var old=w.Passenger.History.FirstOrDefault(h=>h.Id==id);if(old!=null)return old.Outcome==outcome;
            return Apply(w,save,b=>{var p=b.Pending;if(p?.Id!=id || outcome=="completed" && !p.Boarded || b.Trips==long.MaxValue || b.Spent>long.MaxValue-p.Fare)return null;b.History.Add(new PassengerRecord{Id=id,Limo=p.Limo,Elapsed=p.Elapsed,Fare=p.Fare,Outcome=outcome});if(b.History.Count>32)b.History.RemoveAt(0);b.Trips++;b.Spent+=p.Fare;b.Pending=null;return p.Budget-p.Fare;});
        }
        public static bool Template(WalletData w,SavedVehicle car,Action save)
        {if(car!=null && !VehicleInsuranceRules.Snapshot(car))return false;return Apply(w,save,b=>{if(b.Pending!=null)return null;b.CustomTaxi=Copy(car);return 0;});}
    }
}
