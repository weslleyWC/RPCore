using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangPopUpProduct : ExtensibleData
    {
        public string Name;public int Price;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Name) && Name.Length<=80 && Price>=10 && Price<=100000;
    }
    public sealed class GangPopUpProp : ExtensibleData
    {
        public int Model;public JobPoint Point;public float Pitch,Roll,Yaw;
        [JsonIgnore] public bool Valid=>Model!=0 && Point?.Valid==true && GangPresenceRules.Finite(Pitch) && GangPresenceRules.Finite(Roll) && GangPresenceRules.Finite(Yaw) && Math.Abs(Pitch)<=360 && Math.Abs(Roll)<=360 && Math.Abs(Yaw)<=360;
    }
    public sealed class GangPopUpOptions : ExtensibleData
    {
        public int Goal=10,BoostMinutes=60,StartRadius=8,ArrivalSeconds=90;
        public JobPoint Shop=new JobPoint{Name="Loja temporaria Legion",X=210.3288f,Y=-933.1739f,Z=30.69181f};
        public JobPoint CustomerSpot=new JobPoint{Name="Balcao",X=208.0823f,Y=-932.3f,Z=30.69178f,Heading=230.0376f};
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public List<GangPopUpProduct> Products=new List<GangPopUpProduct>{new GangPopUpProduct{Name="GRAPHIC T-SHIRT",Price=40},new GangPopUpProduct{Name="HOODIE",Price=85},new GangPopUpProduct{Name="BASEBALL CAP",Price=25},new GangPopUpProduct{Name="SWEATSHIRT",Price=65},new GangPopUpProduct{Name="STREETWEAR",Price=120},new GangPopUpProduct{Name="CARGO PANTS",Price=75},new GangPopUpProduct{Name="DENIM JEANS",Price=90},new GangPopUpProduct{Name="TRACKSUIT",Price=150},new GangPopUpProduct{Name="BOMBER JACKET",Price=180},new GangPopUpProduct{Name="PUFFER JACKET",Price=220},new GangPopUpProduct{Name="DENIM JACKET",Price=130},new GangPopUpProduct{Name="LEATHER JACKET",Price=250},new GangPopUpProduct{Name="TANK TOP",Price=25},new GangPopUpProduct{Name="POLO SHIRT",Price=55},new GangPopUpProduct{Name="FLANNEL SHIRT",Price=70},new GangPopUpProduct{Name="LONG SLEEVE TEE",Price=50},new GangPopUpProduct{Name="JOGGERS",Price=65},new GangPopUpProduct{Name="SWEATPANTS",Price=55},new GangPopUpProduct{Name="CARGO SHORTS",Price=60},new GangPopUpProduct{Name="BASKETBALL SHORTS",Price=45},new GangPopUpProduct{Name="BUCKET HAT",Price=30},new GangPopUpProduct{Name="BEANIE",Price=20},new GangPopUpProduct{Name="SNAPBACK CAP",Price=35},new GangPopUpProduct{Name="SUNGLASSES",Price=80},new GangPopUpProduct{Name="SNEAKERS",Price=140},new GangPopUpProduct{Name="HIGH TOPS",Price=160},new GangPopUpProduct{Name="SLIDERS",Price=35},new GangPopUpProduct{Name="WORK BOOTS",Price=120},new GangPopUpProduct{Name="CHAIN NECKLACE",Price=95},new GangPopUpProduct{Name="CROSSBODY BAG",Price=75}};
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public List<string> Customers=new List<string>{"a_m_y_business_01","a_m_y_business_02","a_m_y_hipster_01","a_m_y_hipster_02","a_m_y_hipster_03","a_m_y_skater_01","a_m_y_skater_02","a_m_y_stwhi_01","a_m_y_stwhi_02","a_m_y_beach_01","a_m_y_beach_02","a_m_y_genstreet_01","a_m_y_genstreet_02","a_m_y_downtown_01","a_m_y_eastsa_01","a_m_y_eastsa_02","a_m_y_soucent_01","a_m_y_soucent_02","a_m_y_vinewood_01","a_m_y_vinewood_02","a_m_m_bevhills_01","a_m_m_bevhills_02","a_m_m_business_01","a_m_m_eastsa_01","a_m_m_eastsa_02","a_m_m_genfat_01","a_m_m_socenlat_01","a_m_m_soucent_01","a_m_m_soucent_02","a_f_y_business_01","a_f_y_business_02","a_f_y_business_03","a_f_y_hipster_01","a_f_y_hipster_02","a_f_y_hipster_03","a_f_y_hipster_04","a_f_y_beach_01","a_f_y_eastsa_01","a_f_y_eastsa_02","a_f_y_soucent_01","a_f_y_soucent_02","a_f_y_soucent_03","a_f_y_tourist_01","a_f_y_vinewood_01","a_f_y_vinewood_02","a_f_m_bevhills_01","a_f_m_bevhills_02","a_f_m_business_02","a_f_m_eastsa_01","a_f_m_eastsa_02","a_f_m_soucent_01","a_f_m_soucent_02"};
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public List<GangPopUpProp> Props=new List<GangPopUpProp>{new GangPopUpProp{Model=-1478135602,Point=new JobPoint{Name="Banca 1",X=211.7501f,Y=-932.0195f,Z=29.69485f},Pitch=0f,Roll=0f,Yaw=-77.6011f},new GangPopUpProp{Model=-1184096195,Point=new JobPoint{Name="Banca 2",X=209.0232f,Y=-933.0248f,Z=30.13969f},Pitch=0f,Roll=0f,Yaw=54.75916f},new GangPopUpProp{Model=-1251029815,Point=new JobPoint{Name="Banca 3",X=210.3468f,Y=-934.1182f,Z=29.83701f},Pitch=0f,Roll=0f,Yaw=-125.7955f},new GangPopUpProp{Model=296207441,Point=new JobPoint{Name="Banca 4",X=211.0929f,Y=-933.1302f,Z=29.69367f},Pitch=0f,Roll=0f,Yaw=-122.5539f},new GangPopUpProp{Model=-1478135602,Point=new JobPoint{Name="Banca 5",X=211.0302f,Y=-931.5827f,Z=29.69485f},Pitch=0f,Roll=0f,Yaw=-28.80613f},new GangPopUpProp{Model=1560277278,Point=new JobPoint{Name="Banca 6",X=210.4808f,Y=-932.674f,Z=30.38482f},Pitch=0f,Roll=0f,Yaw=-174.2936f},new GangPopUpProp{Model=-1184096195,Point=new JobPoint{Name="Banca 7",X=209.8883f,Y=-931.795f,Z=30.13969f},Pitch=0f,Roll=0f,Yaw=54.65314f},new GangPopUpProp{Model=172569958,Point=new JobPoint{Name="Banca 8",X=209.3963f,Y=-935.4008f,Z=29.76275f},Pitch=0f,Roll=0f,Yaw=77.43171f},new GangPopUpProp{Model=-962367694,Point=new JobPoint{Name="Banca 9",X=209.9902f,Y=-931.6437f,Z=30.69781f},Pitch=0f,Roll=0f,Yaw=-96.66871f}};
        [JsonIgnore] public bool Valid=>Goal>=1 && Goal<=50 && BoostMinutes>=1 && BoostMinutes<=180 && StartRadius>=3 && StartRadius<=15 && ArrivalSeconds>=30 && ArrivalSeconds<=180 && Shop?.Valid==true && CustomerSpot?.Valid==true && DeliveryRoute.Distance(Shop,CustomerSpot)<=10 && Products!=null && Products.Count>=1 && Products.Count<=100 && Products.All(p=>p?.Valid==true) && Products.Select(p=>p.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()==Products.Count && Customers!=null && Customers.Count>=1 && Customers.Count<=100 && Customers.All(GangPresenceRules.ModelName) && Customers.Distinct(StringComparer.OrdinalIgnoreCase).Count()==Customers.Count && Props!=null && Props.Count<=20 && Props.All(p=>p?.Valid==true && DeliveryRoute.Distance(Shop,p.Point)<=15);
    }
    public sealed class GangPopUpOrder : ExtensibleData
    {
        public string Id,Model;public int Item,Discount;public float Target;
        public bool Matches(GangPopUpOptions c)=>c!=null && !string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && c.Customers.Contains(Model) && Item>=0 && Item<c.Products.Count && (Discount==0 || Discount==5 || Discount==10 || Discount==15) && GangPresenceRules.Finite(Target) && Target>=.2f && Target<=.8f;
    }
    public sealed class GangPopUpAttempt : ExtensibleData
    {
        public string Id,Gang;public GangTurfDefinition Area;public GangPopUpOptions Options;public int Sales;public long Revenue;public GangPopUpOrder Customer;
        [JsonIgnore] public bool Ready=>Options?.Valid==true && Sales==Options.Goal && Customer==null;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && !GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area?.Valid==true && Area.Id==7 && Options?.Valid==true && Sales>=0 && Sales<=Options.Goal && Revenue>=Sales && (Sales!=0 || Revenue==0) && (Customer==null || Sales<Options.Goal && Customer.Matches(Options));
    }
    public sealed class GangPopUpAccount : ExtensibleData
    {
        public string Gang;public int Area=7;public long Contracts,Sales,Revenue,StartedUtcTicks,ExpiresUtcTicks;
        [JsonIgnore] public bool Valid=>!GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area==7 && Contracts>=0 && Sales>=Contracts && Revenue>=Sales && (Sales!=0 || Revenue==0) && (Contracts==0?StartedUtcTicks==0 && ExpiresUtcTicks==0:StartedUtcTicks>0 && ExpiresUtcTicks<=DateTime.MaxValue.Ticks && ExpiresUtcTicks>StartedUtcTicks && ExpiresUtcTicks-StartedUtcTicks<=TimeSpan.FromMinutes(180).Ticks);
    }
    public sealed class GangPopUpData : ExtensibleData
    {
        public GangPopUpAttempt Pending;public List<GangPopUpAccount> Accounts=new List<GangPopUpAccount>();public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>(Pending==null || Pending.Valid) && Accounts!=null && Accounts.Count<=640 && Accounts.All(a=>a?.Valid==true) && Accounts.Select(a=>a.Gang).Distinct(StringComparer.OrdinalIgnoreCase).Count()==Accounts.Count && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=180) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public sealed class GangPopUpCheckout
    {
        int last;long time,phase,until=350;public int Step{get;private set;}public int Item{get;private set;}public int Discount{get;private set;}public bool Locked{get;private set;}public bool Success{get;private set;}
        public double Cursor{get{double p=phase*.00085%2;return p<=1?p:2-p;}}
        public GangPopUpCheckout(int now){last=now;}
        public void Tick(int now,bool working){uint d=unchecked((uint)(now-last));last=now;if(working && d<=2000 && !Locked){time=Math.Min(1000000000,time+d);if(Step==2)phase=Math.Min(1000000000,phase+d);}}
        public bool Move(int direction,int count){if(Locked || Step>=2 || time<until || count<1 || count>100 || direction!=1 && direction!=-1)return false;if(Step==0)Item=(Item+direction+count)%count;else Discount=Math.Max(0,Math.Min(20,Discount+direction*5));until=time+180;return true;}
        public static bool Hit(double cursor,float target)=>!double.IsNaN(cursor) && cursor>=0 && cursor<=1 && GangPresenceRules.Finite(target) && target>=.2f && target<=.8f && Math.Abs(cursor-target)<=.09+1e-8;
        public bool Confirm(GangPopUpOrder order,int count,out string feedback)
        {
            feedback="";if(Locked || time<until || order==null || count<1 || count>100 || order.Item<0 || order.Item>=count)return false;
            if(Step==0){if(Item!=order.Item){feedback="Peca incorreta; confira pedido.";until=time+300;return false;}Step=1;}
            else if(Step==1){if(Discount!=order.Discount){feedback="Confira o cupom do cliente.";until=time+300;return false;}Step=2;phase=0;}
            else{Success=Hit(Cursor,order.Target);Locked=true;Step=3;}
            until=time+350;return true;
        }
    }
    public static class GangPopUpTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        static bool Id(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=80;
        static void Receipt(GangPopUpData b,string r){b.Receipts.Add(r);if(b.Receipts.Count>256)b.Receipts.RemoveAt(0);}
        public static bool Blocked(WalletData w)=>w?.GangPopUp!=null && (!w.GangPopUp.Valid || w.GangPopUp.Pending!=null);
        public static bool OtherFree(WalletData w)=>w!=null && !GangChauffeurTransactions.Blocked(w) && !GangTheatreTransactions.Blocked(w) && !GangMowingTransactions.Blocked(w) && !GangMedicalTransactions.Blocked(w) && !GangCashRunTransactions.Blocked(w) && !GangTowTransactions.Blocked(w) && !GangMechanicTransactions.Blocked(w) && !GangPromotionTransactions.Blocked(w) && (w.GangBulk==null || w.GangBulk.Valid && w.GangBulk.Pending==null) && (w.GangRaid==null || w.GangRaid.Valid && w.GangRaid.Pending==null) && (w.GangBusiness==null || w.GangBusiness.Valid && w.GangBusiness.Supply==null);
        public static GangPopUpAccount Account(GangPopUpData b,string gang)=>b?.Valid==true?b.Accounts.FirstOrDefault(a=>GangCrewRules.SameGang(a.Gang,gang)):null;
        public static int Multiplier(GangPopUpData b,string gang,int area,DateTime utc){var a=Account(b,gang);return area==7 && utc.Kind==DateTimeKind.Utc && a!=null && a.Contracts>0 && utc.Ticks>=a.StartedUtcTicks && utc.Ticks<a.ExpiresUtcTicks?2:1;}
        public static int Quote(GangPopUpOptions c,GangPopUpOrder o)=>c?.Valid==true && o?.Matches(c)==true?c.Products[o.Item].Price*(100-o.Discount)/100:0;
        static bool Apply(WalletData w,string r,Func<GangPopUpData,bool> mutate,Action save,out bool changed)
        {changed=false;if(w==null || w.Bank<0 || save==null || w.GangPopUp!=null && !w.GangPopUp.Valid)return false;var old=w.GangPopUp;if(old?.Receipts.Contains(r)==true)return true;var b=old==null?new GangPopUpData():Copy(old);if(!mutate(b) || !b.Valid)return false;Receipt(b,r);try{w.GangPopUp=b;save();changed=true;return true;}catch{w.GangPopUp=old;throw;}}
        static bool Owns(GangState s,GangTurfDefinition area,GangPopUpAttempt p)=>p!=null && area?.Id==7 && GangBusinessTransactions.Owns(s,area) && GangCrewRules.SameGang(s.Gang,p.Gang);
        public static bool Begin(WalletData w,GangState s,GangTurfDefinition area,GangPopUpOptions c,string id,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || area?.Id!=7 || !GangBusinessTransactions.Owns(s,area) || c?.Valid!=true || !OtherFree(w) || w.GangIllicit!=null && !w.GangIllicit.Valid)return false;
            long maximum=(long)c.Goal*c.Products.Max(p=>p.Price);var dirty=GangIllicitTransactions.Account(w.GangIllicit,s.Gang,7);if((dirty?.Dirty??0)>int.MaxValue-maximum || (dirty?.Proceeds??0)>long.MaxValue-maximum || dirty==null && w.GangIllicit?.Accounts.Count>=640)return false;
            return Apply(w,id+":pop-begin",b=>{if(b.Pending!=null)return false;var a=Account(b,s.Gang);if(a==null && b.Accounts.Count>=640 || a!=null && (a.Contracts==long.MaxValue || a.Sales>long.MaxValue-c.Goal || a.Revenue>long.MaxValue-maximum))return false;b.Pending=new GangPopUpAttempt{Id=id,Gang=s.Gang,Area=Copy(area),Options=Copy(c)};return true;},save,out changed);
        }
        public static bool Customer(WalletData w,GangState s,GangTurfDefinition area,string id,GangPopUpOrder order,Action save,out bool changed)
        {changed=false;if(!Id(id) || order==null || !Id(order.Id) || !OtherFree(w) || !Owns(s,area,w?.GangPopUp?.Pending))return false;return Apply(w,order.Id+":pop-customer",b=>{var p=b.Pending;if(p.Id!=id || p.Ready || p.Customer!=null || !order.Matches(p.Options))return false;p.Customer=Copy(order);return true;},save,out changed);}
        public static bool Fail(WalletData w,string id,string customer,Action save,out bool changed)
        {changed=false;if(!Id(id) || !Id(customer))return false;return Apply(w,customer+":pop-fail",b=>{if(b.Pending?.Id!=id || b.Pending.Customer?.Id!=customer)return false;b.Pending.Customer=null;return true;},save,out changed);}
        public static bool Sale(WalletData w,GangState s,GangTurfDefinition area,string id,string customer,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || !Id(id) || !Id(customer) || !OtherFree(w) || !Owns(s,area,w.GangPopUp?.Pending) || w.GangPopUp?.Valid!=true || w.GangIllicit!=null && !w.GangIllicit.Valid)return false;
            string receipt=customer+":pop-sale";if(w.GangPopUp.Receipts.Contains(receipt))return true;
            var old=w.GangPopUp;var original=w.GangIllicit;var b=Copy(old);var p=b.Pending;if(p.Id!=id || p.Customer?.Id!=customer || p.Ready)return false;int value=Quote(p.Options,p.Customer);if(value<1 || p.Revenue>long.MaxValue-value)return false;
            var a=Account(b,p.Gang);if(a==null){if(b.Accounts.Count>=640)return false;a=new GangPopUpAccount{Gang=p.Gang};b.Accounts.Add(a);}if(a.Sales==long.MaxValue || a.Revenue>long.MaxValue-value)return false;
            var funds=original==null?new GangIllicitData():Copy(original);var income=GangIllicitTransactions.Account(funds,p.Gang,7);if(income==null){if(funds.Accounts.Count>=640)return false;income=new GangIllicitAccount{Gang=p.Gang,Area=7};funds.Accounts.Add(income);}if(income.Dirty>int.MaxValue-value || income.Proceeds>long.MaxValue-value)return false;
            p.Sales++;p.Revenue+=value;p.Customer=null;a.Sales++;a.Revenue+=value;income.Dirty+=value;income.Proceeds+=value;Receipt(b,receipt);funds.Receipts.Add(receipt);if(funds.Receipts.Count>256)funds.Receipts.RemoveAt(0);if(!b.Valid || !funds.Valid)return false;
            try{w.GangPopUp=b;w.GangIllicit=funds;save();changed=true;return true;}catch{w.GangPopUp=old;w.GangIllicit=original;throw;}
        }
        public static bool Cancel(WalletData w,string id,Action save,out bool changed){changed=false;if(!Id(id))return false;return Apply(w,id+":pop-cancel",b=>{if(b.Pending?.Id!=id)return false;b.Pending=null;return true;},save,out changed);}
        public static bool Complete(WalletData w,GangState s,GangTurfDefinition area,string id,DateTime utc,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || area?.Id!=7 || !GangBusinessTransactions.Owns(s,area) || utc.Kind!=DateTimeKind.Utc || utc.Ticks==0 || !OtherFree(w))return false;
            return Apply(w,id+":pop-complete",b=>{var p=b.Pending;if(p?.Id!=id || !p.Ready || !Owns(s,area,p) || utc.Ticks>DateTime.MaxValue.Ticks-TimeSpan.FromMinutes(p.Options.BoostMinutes).Ticks)return false;var a=Account(b,p.Gang);if(a==null || a.Contracts==long.MaxValue)return false;a.Contracts++;a.StartedUtcTicks=utc.Ticks;a.ExpiresUtcTicks=utc.AddMinutes(p.Options.BoostMinutes).Ticks;b.Pending=null;return true;},save,out changed);
        }
    }
}
