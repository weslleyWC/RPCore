using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public enum TrafficOffence { Speeding,NoSeatbelt,NoHeadlights,NoHelmet,Burnout,SmokingEngine,BurstTyre }
    public sealed class TrafficOptions : ExtensibleData
    {
        public string CruiserModel="police3",OfficerModel="s_m_y_cop_01";
        public int WitnessRadius=50,SampleMilliseconds=2000,StopSeconds=60,ObserverSeconds=90,CooldownSeconds=120,InspectionMilliseconds=7500;
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public Dictionary<string,int> SpeedLimits=new Dictionary<string,int>();
        [JsonIgnore] public bool Valid=>GangPresenceRules.ModelName(CruiserModel) && GangPresenceRules.ModelName(OfficerModel) && WitnessRadius>=20 && WitnessRadius<=80 && SampleMilliseconds>=500 && SampleMilliseconds<=10000 && StopSeconds>=30 && StopSeconds<=180 && ObserverSeconds>=30 && ObserverSeconds<=300 && CooldownSeconds>=15 && CooldownSeconds<=1800 && InspectionMilliseconds>=3000 && InspectionMilliseconds<=15000 && SpeedLimits!=null && SpeedLimits.Count<=1000 && SpeedLimits.All(p=>!string.IsNullOrWhiteSpace(p.Key) && p.Key.Length<=100 && p.Value>=10 && p.Value<=200) && SpeedLimits.Keys.Select(k=>k.ToUpperInvariant()).Distinct().Count()==SpeedLimits.Count;
        public static TrafficOptions Defaults(){using(var s=typeof(TrafficOptions).Assembly.GetManifestResourceStream("RPCore.JobDefaults.Traffic.json"))using(var r=new System.IO.StreamReader(s))return JsonConvert.DeserializeObject<TrafficOptions>(r.ReadToEnd());}
    }
    public sealed class TrafficFrame
    {
        public bool Car,Bike,Belt,Helmet,Lights,Night,Burnout,Burst;public float Speed,Engine;public string Street;
    }
    public sealed class TrafficWitnessMeter
    {
        string signature;public int Elapsed{get;private set;}
        public bool Observe(string key,int delta,int required)
        {
            if(string.IsNullOrWhiteSpace(key) || delta<0 || delta>1000 || required<500 || required>10000){Reset();return false;}
            if(signature!=key){signature=key;Elapsed=0;return false;}Elapsed=Math.Min(required,Elapsed+delta);return Elapsed>=required;
        }
        public void Reset(){signature=null;Elapsed=0;}
    }
    public sealed class TrafficLine : ExtensibleData
    {
        public TrafficOffence Code;public int Amount;public double Value;
        [JsonIgnore] public bool Valid=>Enum.IsDefined(typeof(TrafficOffence),Code) && !double.IsNaN(Value) && !double.IsInfinity(Value) && Math.Abs(Value)<=100000 && (Code!=TrafficOffence.Speeding || Value>0) && (Code!=TrafficOffence.SmokingEngine || Value<=300) && Amount==TrafficRules.Cost(Code,Value);
    }
    public static class TrafficRules
    {
        public static int Cost(TrafficOffence code,double value=0)=>code==TrafficOffence.Speeding?(value>=30?250:150):code==TrafficOffence.NoSeatbelt?150:code==TrafficOffence.NoHelmet?120:code==TrafficOffence.SmokingEngine?75:100;
        public static string Label(TrafficOffence code)=>code==TrafficOffence.Speeding?"Excesso de velocidade":code==TrafficOffence.NoSeatbelt?"Sem cinto":code==TrafficOffence.NoHeadlights?"Farois apagados a noite":code==TrafficOffence.NoHelmet?"Sem capacete":code==TrafficOffence.Burnout?"Burnout":code==TrafficOffence.SmokingEngine?"Motor em estado critico":"Pneu estourado";
        public static int Limit(TrafficOptions c,string street)=>c?.Valid==true && street!=null?c.SpeedLimits.Where(p=>string.Equals(p.Key,street,StringComparison.OrdinalIgnoreCase)).Select(p=>p.Value).FirstOrDefault():0;
        public static List<TrafficLine> Evaluate(TrafficFrame f,TrafficOptions c)
        {
            var lines=new List<TrafficLine>();if(f==null || c?.Valid!=true || !GangPresenceRules.Finite(f.Speed) || f.Speed<0 || !GangPresenceRules.Finite(f.Engine) || !(f.Car || f.Bike))return lines;
            Action<TrafficOffence,double> add=(code,value)=>lines.Add(new TrafficLine{Code=code,Value=value,Amount=Cost(code,value)});
            double mph=f.Speed*2.2369363;bool moving=mph>=10;int limit=Limit(c,f.Street);
            if(limit>0 && mph>limit)add(TrafficOffence.Speeding,mph-limit);
            if(f.Car && moving && !f.Belt)add(TrafficOffence.NoSeatbelt,0);
            if(f.Night && moving && !f.Lights)add(TrafficOffence.NoHeadlights,0);
            if(f.Bike && !f.Helmet)add(TrafficOffence.NoHelmet,0);
            if(f.Burnout)add(TrafficOffence.Burnout,0);
            if(f.Engine<=300)add(TrafficOffence.SmokingEngine,f.Engine);
            if(f.Burst)add(TrafficOffence.BurstTyre,0);return lines;
        }
        public static bool Stopped(bool sameDriver,float playerSpeed,float patrolSpeed,float distance)=>sameDriver && GangPresenceRules.Finite(playerSpeed) && GangPresenceRules.Finite(patrolSpeed) && GangPresenceRules.Finite(distance) && playerSpeed>=0 && playerSpeed<.5f && patrolSpeed>=0 && patrolSpeed<1.5f && distance>=0 && distance<=18;
        public static bool Inspect(bool sameDriver,bool alive,float speed,float distance)=>sameDriver && alive && GangPresenceRules.Finite(speed) && speed>=0 && speed<.5f && GangPresenceRules.Finite(distance) && distance>=0 && distance<=2.5f;
        public static bool Outcome(string s)=>s=="open" || s=="served" || s=="evaded" || s=="interrupted";
    }
    public sealed class TrafficTicket : ExtensibleData
    {
        public string Id,Street,Plate,Outcome="open";public int Model,LimitMph;public List<TrafficLine> Lines=new List<TrafficLine>();public bool Paid;
        [JsonIgnore] public int Total=>Lines?.Sum(l=>l.Amount)??0;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && !string.IsNullOrWhiteSpace(Street) && Street.Length<=100 && !string.IsNullOrWhiteSpace(Plate) && Plate.Length<=16 && Model!=0 && LimitMph>=0 && LimitMph<=200 && TrafficRules.Outcome(Outcome) && (!Paid || Outcome!="open") && Lines!=null && Lines.Count>=1 && Lines.Count<=7 && Lines.All(l=>l?.Valid==true) && Lines.Select(l=>l.Code).Distinct().Count()==Lines.Count && (!Lines.Any(l=>l.Code==TrafficOffence.Speeding) || LimitMph>0);
    }
    public sealed class TrafficData : ExtensibleData
    {
        public bool Enabled;public string Pending;public long Issued,Paid,Collected;public List<TrafficTicket> Tickets=new List<TrafficTicket>();
        [JsonIgnore] public bool Valid=>Issued>=0 && Paid>=0 && Paid<=Issued && Collected>=Paid && Tickets!=null && Tickets.Count<=256 && Tickets.All(t=>t?.Valid==true) && Tickets.Select(t=>t.Id).Distinct().Count()==Tickets.Count && Issued>=Tickets.Count && Paid>=Tickets.Count(t=>t.Paid) && (decimal)Collected>=Tickets.Where(t=>t.Paid).Sum(t=>(decimal)t.Total) && (Pending==null?Tickets.All(t=>t.Outcome!="open"):Tickets.Count(t=>t.Id==Pending && t.Outcome=="open" && !t.Paid)==1 && Tickets.Count(t=>t.Outcome=="open")==1);
    }
    public static class TrafficTransactions
    {
        static TrafficData Copy(TrafficData b)=>b==null?new TrafficData():JsonConvert.DeserializeObject<TrafficData>(JsonConvert.SerializeObject(b));
        static bool Apply(WalletData w,Action save,Func<TrafficData,bool> change,out bool changed)
        {
            changed=false;if(w==null || save==null || w.Bank<0 || w.Traffic!=null && !w.Traffic.Valid)return false;var old=w.Traffic;var n=Copy(old);if(!change(n) || !n.Valid)return false;
            try{w.Traffic=n;save();changed=true;return true;}catch{w.Traffic=old;throw;}
        }
        public static bool Configure(WalletData w,bool enabled,Action save,out bool changed)=>Apply(w,save,b=>{b.Enabled=enabled;return true;},out changed);
        public static bool Begin(WalletData w,TrafficTicket ticket,Action save,out bool changed)=>Apply(w,save,b=>{
            if(!b.Enabled || b.Pending!=null || ticket?.Valid!=true || ticket.Paid || ticket.Outcome!="open" || b.Issued==long.MaxValue || b.Tickets.Any(t=>t.Id==ticket.Id))return false;
            if(b.Tickets.Count==256){var remove=b.Tickets.FirstOrDefault(t=>t.Paid);if(remove==null)return false;b.Tickets.Remove(remove);}
            b.Tickets.Add(JsonConvert.DeserializeObject<TrafficTicket>(JsonConvert.SerializeObject(ticket)));b.Pending=ticket.Id;b.Issued++;return true;},out changed);
        public static bool Finish(WalletData w,string id,string outcome,Action save,out bool changed)
        {
            changed=false;if(outcome=="open" || !TrafficRules.Outcome(outcome) || w?.Traffic?.Valid!=true)return false;var ticket=w.Traffic.Tickets.FirstOrDefault(t=>t.Id==id);if(ticket==null)return false;if(ticket.Outcome!="open")return ticket.Outcome==outcome;
            return Apply(w,save,b=>{if(b.Pending!=id)return false;b.Tickets.First(t=>t.Id==id).Outcome=outcome;b.Pending=null;return true;},out changed);
        }
        public static bool Pay(WalletData w,string id,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || w.Traffic?.Valid!=true)return false;var t=w.Traffic.Tickets.FirstOrDefault(a=>a.Id==id);if(t==null || t.Outcome=="open")return false;if(t.Paid)return true;
            if(w.Bank<t.Total || w.Traffic.Paid==long.MaxValue || w.Traffic.Collected>long.MaxValue-t.Total)return false;var old=w.Traffic;int bank=w.Bank;var n=Copy(old);var next=n.Tickets.First(a=>a.Id==id);next.Paid=true;n.Paid++;n.Collected+=next.Total;if(!n.Valid)return false;
            try{w.Traffic=n;w.Bank-=next.Total;save();changed=true;return true;}catch{w.Traffic=old;w.Bank=bank;throw;}
        }
    }
}
