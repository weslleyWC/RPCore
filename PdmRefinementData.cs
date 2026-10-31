using System;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    // Timers may advance while reading a confirmation. Only changes affecting its terms invalidate it.
    public sealed class PdmCommandQuote
    {
        readonly string profile,stamp;readonly int began;bool used;
        public PdmCommandQuote(PdmData data,int bank,string profile,int now){this.profile=profile;began=now;stamp=Stamp(data,bank);}
        public bool Matches(PdmData data,int bank,string owner,int now)=>!used && !string.IsNullOrEmpty(profile) && owner==profile && unchecked((uint)(now-began))<=60000 && stamp!=null && stamp==Stamp(data,bank);
        public bool Take(PdmData data,int bank,string owner,int now){bool valid=Matches(data,bank,owner,now);used=true;return valid;}
        public static string Stamp(PdmData d,int bank)
        {
            if(d==null || !d.Valid || bank<0)return null;
            return JsonConvert.SerializeObject(new{
                bank,d.Owned,d.Cash,d.Bought,d.Sold,
                Stock=d.Stock.Select(s=>new{s.Id,s.Model,s.Plate,s.Cost,s.Price,s.Engine,s.Body,s.Dirt,s.AwaitingCollection}),
                d.Active,d.Collection,d.SafeTransfer,d.Repo,d.Rental,d.ShowroomSale,d.PremisesService,
                Showroom=d.Showroom.Select(s=>new{s.SlotId,s.StockId}),
                Staff=d.Staff.Employees.Select(e=>new{e.Id,e.Role,e.Owed,e.Wage,e.PeriodSeconds,e.Severance,e.BonusPercent}),
                d.Staff.FormerDebt,d.Staff.Workshop,d.Staff.Advertising,d.Surveillance.Purchased,d.Risk.SafeOpen,
                Policy=d.Risk.Policy==null?null:new{d.Risk.Policy.PlanId,d.Risk.Policy.Premium,d.Risk.Policy.Limit},
                Loans=d.Loans.Select(l=>new{l.Id,l.Paid,l.Total,l.Defaulted,l.Repossessed})});
        }
    }
    public static class PdmBusinessRules
    {
        public static int TransferMaximum(PdmData d,int bank,bool deposit)
        {
            if(d==null || !d.Valid || !d.Owned || bank<0)return 0;
            long room=deposit?(long)int.MaxValue-d.Cash-(d.SafeTransfer?.Amount??0):(long)int.MaxValue-bank;
            return (int)Math.Max(0,Math.Min(deposit?bank:d.Cash,room));
        }
        public static decimal Result(PdmData d)=>d==null?0:(decimal)d.Revenue-d.SoldCost-d.Repairs-d.Staff.Paid-d.Staff.HiringSpent-d.Staff.UpgradeSpent-d.Risk.Premiums-d.Risk.Stolen+d.Risk.Reimbursed;
        public static long Wages(PdmData d)=>d==null?0:(long)d.Staff.FormerDebt+d.Staff.Employees.Sum(e=>(long)e.Owed);
        public static long Receivable(PdmData d)=>d==null?0:d.Loans.Sum(l=>(long)l.Receivable);
        public static string Pending(PdmData d)
        {
            if(d.Active!=null)return "Entrega a cliente";if(d.Collection!=null)return d.Collection.Arrived?"Recebimento de estoque":"Coleta no porto";
            if(d.SafeTransfer!=null)return d.SafeTransfer.Handed?"Valores em compensacao":"Coleta de valores";
            if(d.Repo!=null)return "Retomada";if(d.Rental!=null)return d.Rental.Delivered?"Locadora / retorno e recebimento":"Locadora / entrega";
            if(d.ShowroomSale!=null)return "Atendimento no showroom";if(d.PremisesService!=null)return "Servico de manutencao";return "Nenhuma operacao pendente";
        }
    }
    public sealed class PdmEntityProof
    {
        readonly int handle,model;readonly long address;
        public PdmEntityProof(int handle,int model,long address){this.handle=handle;this.model=model;this.address=address;}
        public bool Matches(int h,int m,long p)=>handle!=0 && model!=0 && address!=0 && h==handle && m==model && p==address;
    }
    public sealed class PdmInspection
    {
        readonly int duration;int began;bool inspecting;
        public bool Complete{get;private set;}
        public PdmInspection(int seconds=6){if(seconds<2 || seconds>30)throw new ArgumentOutOfRangeException();duration=seconds*1000;}
        public bool Update(int now,bool eligible)
        {
            if(Complete)return true;if(!eligible){inspecting=false;return false;}
            if(!inspecting){began=now;inspecting=true;}
            if(unchecked(now-began)>=duration)Complete=true;return Complete;
        }
    }
}
