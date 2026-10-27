using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class TrafficBribe : ExtensibleData
    {
        public int Price,Roll,Chance,NativeStars;public bool Accepted;
        public bool Valid(TrafficTicket t)=>t!=null && Price==PoliceResolutionRules.Price(t.Total) && Roll>=0 && Roll<10000 && Chance==PoliceResolutionRules.Chance(t.Total,t.BoloAtStop || t.Lines.Any(l=>l.Code==TrafficOffence.AimingWeapon)) && Accepted==(Roll<Chance) && NativeStars>=0 && NativeStars<=2 && (!Accepted || NativeStars==0 && !t.Paid && t.Outcome=="served") && (Accepted || t.Outcome=="evaded") && (NativeStars==0 || t.Pursuit!=null && NativeStars<=t.Pursuit.BackupStars);
    }
    public sealed class TrafficBolo : ExtensibleData
    {
        public string TicketId,Plate,Reason;public int Model;public bool Active=true;
        [JsonIgnore]public bool Valid=>!string.IsNullOrWhiteSpace(TicketId) && TicketId.Length<=80 && !string.IsNullOrWhiteSpace(Plate) && Plate.Length<=16 && Model!=0 && (Reason=="evasion" || Reason=="bribery");
    }
    public static class PoliceResolutionRules
    {
        public static int Price(int total)=>total>0 && total<=1000000?(int)Math.Round(total*1.5,MidpointRounding.ToEven):0;
        public static int Chance(int total,bool serious)=>Math.Max(500,(serious?1500:6000)*(total>=300?75:100)/100);
        public static bool Settled(TrafficTicket t)=>t!=null && (t.Paid || t.Bribe?.Accepted==true);
        public static bool Bolo(TrafficData b,int model,string plate)=>b?.Bolos?.Any(a=>a.Active && VehicleIdentity.Key(a.Model,a.Plate)==VehicleIdentity.Key(model,plate))==true;
        public static bool Mark(TrafficData b,TrafficTicket t,string reason)
        {
            if(!b.BoloEnabled)return true;if(b.Bolos==null)b.Bolos=new List<TrafficBolo>();if(b.Bolos.Any(a=>a.TicketId==t.Id))return true;
            if(b.Bolos.Count==256){var old=b.Bolos.FirstOrDefault(a=>!a.Active);if(old==null)return false;b.Bolos.Remove(old);}
            b.Bolos.Add(new TrafficBolo{TicketId=t.Id,Model=t.Model,Plate=t.Plate,Reason=reason});return true;
        }
    }
    public static class PoliceResolutionTransactions
    {
        static TrafficData Copy(TrafficData b)=>b==null?new TrafficData():JsonConvert.DeserializeObject<TrafficData>(JsonConvert.SerializeObject(b));
        public static bool Configure(WalletData w,bool bribes,bool bolo,Action save)
        {
            if(w==null || save==null || w.Traffic!=null && (!w.Traffic.Valid || w.Traffic.Pending!=null))return false;
            var old=w.Traffic;var next=Copy(old);next.BribesEnabled=bribes;next.BoloEnabled=bolo;try{w.Traffic=next;save();return true;}catch{w.Traffic=old;throw;}
        }
        public static bool Bribe(WalletData w,string id,int roll,int stars,Action save,out bool changed)
        {
            changed=false;if(string.IsNullOrWhiteSpace(id) || w?.Traffic?.Valid!=true || save==null || w.Bank<0 || roll<0 || roll>=10000 || stars<0 || stars>2)return false;
            var t=w.Traffic.Tickets.FirstOrDefault(a=>a.Id==id);if(t==null)return false;if(t.Bribe!=null)return true;
            int price=PoliceResolutionRules.Price(t.Total);if(!w.Traffic.Enabled || !w.Traffic.BribesEnabled || w.Traffic.Pending!=id || t.Outcome!="open" || t.Paid || price<=0 || w.Bank<price || stars>0 && (t.Pursuit==null || !w.Traffic.PursuitEnabled || stars>t.Pursuit.BackupStars))return false;
            var old=w.Traffic;var n=Copy(old);var ticket=n.Tickets.First(a=>a.Id==id);int chance=PoliceResolutionRules.Chance(t.Total,t.BoloAtStop || t.Lines.Any(l=>l.Code==TrafficOffence.AimingWeapon));bool accepted=roll<chance;
            ticket.Bribe=new TrafficBribe{Price=price,Roll=roll,Chance=chance,Accepted=accepted,NativeStars=accepted?0:stars};ticket.Outcome=accepted?"served":"evaded";n.Pending=null;
            if(!accepted && !PoliceResolutionRules.Mark(n,ticket,"bribery") || !n.Valid)return false;
            int bank=w.Bank;try{w.Traffic=n;if(accepted)w.Bank-=price;save();changed=true;return true;}catch{w.Traffic=old;w.Bank=bank;throw;}
        }
        public static bool ClearBolo(WalletData w,int model,string plate,Action save)
        {
            if(w?.Traffic?.Valid!=true || save==null || w.Traffic.Pending!=null || string.IsNullOrWhiteSpace(plate))return false;
            var active=w.Traffic.Bolos?.Where(a=>a.Active && VehicleIdentity.Key(a.Model,a.Plate)==VehicleIdentity.Key(model,plate)).ToList();if(active==null || active.Count==0)return true;
            var ids=new HashSet<string>(active.Select(a=>a.TicketId));if(w.Traffic.Tickets.Any(t=>ids.Contains(t.Id) && !PoliceResolutionRules.Settled(t)))return false;
            var old=w.Traffic;var n=Copy(old);foreach(var b in n.Bolos.Where(a=>ids.Contains(a.TicketId)))b.Active=false;
            try{w.Traffic=n;save();return true;}catch{w.Traffic=old;throw;}
        }
    }
}
