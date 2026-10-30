using System;
namespace RPCore
{
    public sealed class VehicleServiceIdentity
    {
        readonly string profile,key;readonly int actor,actorModel,car,carModel;readonly long actorAddress,carAddress;
        public VehicleServiceIdentity(string profile,int actor,int actorModel,long actorAddress,int car,int carModel,long carAddress,string key)
        {this.profile=profile;this.actor=actor;this.actorModel=actorModel;this.actorAddress=actorAddress;this.car=car;this.carModel=carModel;this.carAddress=carAddress;this.key=key;}
        public bool Matches(string p,int a,int am,long aa,int v,int vm,long va,string k)
            =>!string.IsNullOrEmpty(profile) && actor!=0 && car!=0 && actorAddress!=0 && carAddress!=0 && profile==p && actor==a && actorModel==am && actorAddress==aa && car==v && carModel==vm && carAddress==va && key==k;
        public bool OwnsActor(int a,int model,long address)=>actor!=0 && actor==a && actorModel==model && actorAddress!=0 && actorAddress==address;
    }
    // A session bills the ceiling of the total delivered value, not a new rounding surcharge per pulse.
    public sealed class FuelOrder
    {
        readonly int target,price;int delivered,paid;bool busy;
        public float Delivered=>delivered/100f;
        public float Requested=>target/100f;
        public int Paid=>paid;
        public bool Finished=>delivered>=target;
        public FuelOrder(float liters,int price)
        {
            if(float.IsNaN(liters) || float.IsInfinity(liters) || liters<.01f || liters>10000 || price<1 || price>1000)throw new ArgumentOutOfRangeException();
            target=(int)Math.Floor((decimal)liters*100);this.price=price;
        }
        public static bool ActivityAllows(string owner)=>owner!="characters" && owner!="inventory_use" && owner!="needs_sleep" && owner!="wardrobe" && owner!="barber" && owner!="grooming" && owner!="housing" && owner!="homestorage";
        public static bool ParseLiters(string text,out float liters)
        {
            liters=0;text=text?.Trim();if(text==null || !System.Text.RegularExpressions.Regex.IsMatch(text,@"^\d{1,5}([.,]\d{1,2})?$"))return false;
            if(!decimal.TryParse(text.Replace(',','.'),System.Globalization.NumberStyles.AllowDecimalPoint,System.Globalization.CultureInfo.InvariantCulture,out decimal value) || value<.01m || value>10000)return false;
            liters=(float)value;return true;
        }
        public FuelQuote Next(float step,float space,int cash)
        {
            if(Finished || float.IsNaN(step) || float.IsInfinity(step) || step<=0 || float.IsNaN(space) || float.IsInfinity(space) || space<=0 || cash<0)return new FuelQuote();
            decimal amount=Math.Min(Math.Min((decimal)Math.Min(10000,step),(decimal)Math.Min(10000,space)),(target-delivered)/100m);
            amount=Math.Min(amount,Math.Max(0,((decimal)paid+cash)/price-delivered/100m));
            int cents=(int)Math.Floor(amount*100);if(cents<=0)return new FuelQuote();
            int cost=(int)Math.Ceiling((delivered+cents)/100m*price)-paid;
            return new FuelQuote{Liters=cents/100f,Cost=cost};
        }
        public bool Deliver(FuelTank tank,float step,Func<int> cash,Action<int> setCash,Action save)
        {
            if(busy || tank==null || cash==null || setCash==null || save==null)return false;tank.Validate();
            int money=cash();var quote=Next(step,tank.Capacity-tank.Liters,money);if(quote.Liters<=0)return false;
            float before=tank.Liters;bool charged=false;busy=true;
            try{
                if(quote.Cost>0){charged=true;setCash(money-quote.Cost);}
                tank.Liters=Math.Min(tank.Capacity,before+quote.Liters);save();
                delivered+=(int)Math.Round(quote.Liters*100);paid+=quote.Cost;return true;
            }catch(Exception ex){
                tank.Liters=before;
                if(charged)try{setCash(money);}catch(Exception rollback){throw new AggregateException("Fuel payment rollback failed.",ex,rollback);}
                throw;
            }finally{busy=false;}
        }
    }
}
namespace RPCore
{
    public sealed class SeatbeltOverride
    {
        public readonly bool Original;bool written;
        public SeatbeltOverride(bool original){Original=original;}
        public void Applied(){written=true;}
        public bool CanRestore(bool sameActor,bool currentFlag)=>written && sameActor && !currentFlag;
    }
}
