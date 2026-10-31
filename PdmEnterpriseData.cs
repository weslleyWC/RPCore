using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class PdmFacilityPart
    {
        public string Model;public JobPoint Point;public float Pitch,Roll,Yaw;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Model) && Point?.Valid==true && new[]{Pitch,Roll,Yaw}.All(v=>!float.IsNaN(v) && !float.IsInfinity(v));
    }
    public sealed class PdmFacility : ExtensibleData
    {
        public string Id,Name,Model;public int Price;public JobPoint Point;
        public float Pitch,Roll,Yaw;public List<PdmFacilityPart> Parts=new List<PdmFacilityPart>();
        [JsonIgnore] public bool Valid=>Parts!=null && Parts.Count<=30 && Parts.All(p=>p?.Valid==true) && !string.IsNullOrWhiteSpace(Id) && !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Model) && Price>0 && Price<=1000000 && Point?.Valid==true && new[]{Pitch,Roll,Yaw}.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v));
    }
    public sealed class PdmEnterpriseOptions : ExtensibleData
    {
        public int DaySeconds=1800,SaleSeconds=900,SellerSeconds=1380,SourceSeconds=1380,CardCount=10,CardCost=100,AutoPickupFee=250;
        public PdmStaffOffer Collector=new PdmStaffOffer{Id="collector",Name="Coletor de veiculos",HiringCost=2300,Wage=2300,SaleBonusPercent=0};
        public List<PdmFacility> Facilities=new List<PdmFacility>();
        public JobPoint ServiceBay,SourceArrival,SellerCar,SellerPed,Dumpster;
        public List<JobPoint> Trash=new List<JobPoint>();
        [JsonIgnore] public bool Valid=>DaySeconds>=300 && DaySeconds<=86400 && SaleSeconds>=60 && SaleSeconds<=86400 && SellerSeconds>=60 && SellerSeconds<=86400 && SourceSeconds>=60 && SourceSeconds<=86400 && CardCount>=1 && CardCount<=50 && CardCost>=0 && CardCost<=100000 && AutoPickupFee>=0 && AutoPickupFee<=10000 && Collector?.Valid==true && Collector.Id=="collector" && Collector.SaleBonusPercent==0 && new[]{ServiceBay,SourceArrival,SellerCar,SellerPed,Dumpster}.All(p=>p?.Valid==true) && Facilities!=null && Facilities.Count>0 && Facilities.All(p=>p?.Valid==true) && Facilities.Select(p=>p.Id).Distinct().Count()==Facilities.Count && Trash!=null && Trash.Count==4 && Trash.All(p=>p?.Valid==true);
    }
    public static class PdmEnterpriseDefaults
    {
        public static PdmEnterpriseOptions Load(){using(var s=typeof(PdmEnterpriseDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.PdmEnterprise.json"))using(var r=new System.IO.StreamReader(s))return JsonConvert.DeserializeObject<PdmEnterpriseOptions>(r.ReadToEnd());}
        public static PdmStaffOptions Merge(PdmStaffOptions staff,PdmEnterpriseOptions c)
        {if(staff?.Valid!=true || c?.Valid!=true)throw new ArgumentException("Invalid enterprise staff configuration.");var copy=JsonConvert.DeserializeObject<PdmStaffOptions>(JsonConvert.SerializeObject(staff));if(!copy.Roles.Any(r=>r.Id=="collector"))copy.Roles.Add(c.Collector);return copy;}
    }
    public sealed class PdmAutoDelivery : ExtensibleData
    {
        public string Id;public List<string> StockIds=new List<string>();public PdmStock SourcedCar;
        public int Seconds,Delivered,Fee;public bool AtGate;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=100 && Seconds>=0 && Seconds<=86400 && (!AtGate || Seconds>=120) && Delivered>=0 && Fee>=0 && StockIds!=null && ((SourcedCar!=null && SourcedCar.Valid && StockIds.Count==0 && Delivered==0) || (SourcedCar==null && StockIds!=null && StockIds.Count>=1 && StockIds.Count<=6 && StockIds.Distinct().Count()==StockIds.Count && StockIds.All(id=>!string.IsNullOrWhiteSpace(id)) && Delivered<StockIds.Count));
    }
    public sealed class PdmPrivateSeller : ExtensibleData
    {
        public string Id;public PdmStock Car;public int Asking,Minimum,Counter;public bool Inspected,Countered,Rejected;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Car?.Valid==true && Minimum>0 && Asking>=Minimum && Asking<=1000000 && Counter>=0 && Counter<=1000000;
    }
    public sealed class PdmRentalRequest : ExtensibleData
    {
        public string Id,Model,Name;public int Amount=2;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && !string.IsNullOrWhiteSpace(Model) && !string.IsNullOrWhiteSpace(Name) && Amount==2;
    }
    public sealed class PdmCustomerOrder : ExtensibleData
    {
        public string Id,StockId;public int Destination;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=100 && !string.IsNullOrWhiteSpace(StockId) && Destination>=0 && Destination<30;
    }
    public sealed class PdmEnterpriseData : ExtensibleData
    {
        public int Reputation,Mood=100,Cleanliness=100,Day,DayElapsed,SaleElapsed,SellerElapsed,SourceElapsed,TrashElapsed;
        public List<int> TrashSlots=new List<int>();
        [JsonIgnore] public int TrashCount=>TrashSlots.Count;
        public int CardsLeft,CardsGiven,CharityPending,SponsorRemaining,SponsorPayment,ProcurementBudget=50000;
        public bool Open=true;public string CardSession;
        public List<string> CardTargets=new List<string>();
        public long Expenses,Donations,SponsorSpent,ServicesSpent;
        public List<string> Upgrades=new List<string>();
        public int RentalGeneratedDay=-1;
        public List<PdmRentalRequest> RentalOrders=new List<PdmRentalRequest>();
        public PdmAutoDelivery Auto;public PdmPrivateSeller Seller;
        public List<PdmCustomerOrder> CustomerOrders=new List<PdmCustomerOrder>();
        [JsonIgnore] public bool Valid=>Reputation>=0 && Reputation<=100 && Mood>=0 && Mood<=100 && Cleanliness>=0 && Cleanliness<=100 && Day>=0 && DayElapsed>=0 && DayElapsed<=86400 && new[]{SaleElapsed,SellerElapsed,SourceElapsed,TrashElapsed}.All(v=>v>=0 && v<=86400) && TrashSlots!=null && TrashSlots.Count<=4 && TrashSlots.Distinct().Count()==TrashSlots.Count && TrashSlots.All(i=>i>=0 && i<4) && CardsLeft>=0 && CardsLeft<=50 && (CardsLeft==0 || !string.IsNullOrWhiteSpace(CardSession)) && CardsGiven>=0 && CharityPending>=0 && CharityPending<=5 && SponsorRemaining>=0 && SponsorRemaining<=864000 && SponsorPayment>=0 && SponsorPayment<=604800 && ProcurementBudget>=0 && ProcurementBudget<=1000000 && CardTargets!=null && CardTargets.Count<=50 && CardTargets.Distinct().Count()==CardTargets.Count && Expenses>=0 && Donations>=0 && SponsorSpent>=0 && ServicesSpent>=0 && Upgrades!=null && Upgrades.Count<=30 && Upgrades.Distinct().Count()==Upgrades.Count && Upgrades.All(s=>!string.IsNullOrWhiteSpace(s)) && RentalGeneratedDay>=-1 && RentalGeneratedDay<=Day && RentalOrders!=null && RentalOrders.Count<=10 && RentalOrders.All(r=>r?.Valid==true) && RentalOrders.Select(r=>r.Id).Distinct().Count()==RentalOrders.Count && CustomerOrders!=null && CustomerOrders.Count<=20 && CustomerOrders.All(o=>o?.Valid==true) && CustomerOrders.Select(o=>o.StockId).Distinct().Count()==CustomerOrders.Count && CustomerOrders.Select(o=>o.Id).Distinct().Count()==CustomerOrders.Count && (Auto==null || Auto.Valid) && (Seller==null || Seller.Valid);
    }
    public static class PdmEnterpriseRules
    {
        public static PdmEnterpriseData View(PdmData d)=>d?.Enterprise??new PdmEnterpriseData();
        public static bool LegacyIdle(PdmData d)=>d!=null && d.Valid && d.Owned && d.Active==null && d.Collection==null && d.SafeTransfer==null && d.Repo==null && d.Rental==null && d.ShowroomSale==null && d.PremisesService==null;
        public static bool Idle(PdmData d)=>LegacyIdle(d) && View(d).Auto==null;
        public static bool Paid(PdmData d,string role)=>d?.Staff!=null && d.Staff.FormerDebt==0 && d.Staff.Employees.Any(e=>e.Role==role && e.Owed==0);
        public static int Capacity(PdmData d)=>View(d).Upgrades.Contains("carrier6") && View(d).Upgrades.Contains("truck")?6:2;
        public static PdmLoadSlot Slot(int capacity,int index)
        {if(capacity!=6 || index<0 || index>=6)throw new ArgumentException("Invalid six-car slot.");return new PdmLoadSlot{X=0,Y=5-index%3*5,Z=index<3?3:1};}
        public static int SellingPrice(int cost,int rep)=>Math.Min(2000000,(int)((long)cost*(115+Math.Max(0,Math.Min(100,rep))/10)/100));
        public static int ReputationBonus(PdmData d)=>View(d).Reputation/10;
        public static bool Reserved(PdmData d,string id)=>View(d).Auto?.StockIds.Contains(id)==true || View(d).CustomerOrders.Any(o=>o.StockId==id);
        public static long Valuation(PdmData d)=>d==null?0:d.AcquisitionPrice+d.Cash+d.Stock.Sum(s=>(long)s.Cost)*12/100+View(d).Upgrades.Sum(u=>u=="truck"?200000L:u=="carrier2"?65000L:u=="carrier6"?150000L:0);
        public static int Seed(string text){uint hash=2166136261;foreach(char c in text??string.Empty)hash=unchecked((hash^c)*16777619);return (int)(hash&0x7fffffff);}
        public static int OfferPrice(int kind,int mileage,int roll)
        {
            int[] low={100000,35000,45000,19000,25000,19000,32000,19000},high={300000,85000,85000,40000,85000,40000,85000,50000};
            if(kind<0 || kind>=8 || mileage<0 || mileage>200000 || roll<0 || roll>100)throw new ArgumentException();
            int ceiling=high[kind]*(85+roll*15/100)/100;int discount=(int)((long)ceiling*Math.Min(450,mileage*(kind==0?450:kind==1?400:350)/25000)/1000);
            return Math.Max(low[kind],ceiling-discount);
        }
    }
}
