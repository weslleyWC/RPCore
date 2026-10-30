using System;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public class VehicleOffer
    {
        public string Model, Name;
        public int Price;
    }
    // One confirmation authorizes exactly one attempt against the quoted wallet/garage.
    public sealed class VehiclePurchase
    {
        readonly TransportData transport;
        readonly int cash,model,price,capacity;
        readonly string garage;
        bool consumed;
        public VehiclePurchase(TransportData transport,int cash,int model,int price,string garage,int capacity)
        {this.transport=transport;this.cash=cash;this.model=model;this.price=price;this.garage=garage;this.capacity=capacity;}
        public bool Current(TransportData current,int balance)
        {
            if(consumed || transport==null || !ReferenceEquals(current,transport) || balance!=cash || price<=0 || cash<price || model==0 || capacity<=0 || string.IsNullOrWhiteSpace(garage))return false;
            transport.Validate();
            return transport.Garages.Contains(garage) && transport.Vehicles.Count(v=>v.GarageId==garage)<capacity;
        }
        public bool Commit(TransportData current,SavedVehicle record,float tankCapacity,Func<int> readCash,Action<int> writeCash,Action save)
        {
            if(readCash==null || writeCash==null || save==null || !Current(current,readCash()))return false;
            consumed=true;
            if(!VehicleInsuranceRules.Snapshot(record) || record.Model!=model || record.GarageId!=garage || record.Stored || record.PendingRemoval || record.EngineHealth<900 || record.BodyHealth<900 || record.TankHealth<900 || !VehicleInsuranceRules.Finite(tankCapacity) || tankCapacity<=0 || tankCapacity>10000 || record.Fuel>tankCapacity || transport.Vehicles.Any(v=>v.Id==record.Id) || transport.FindIdentity(record.Model,record.Plate)!=null)return false;
            string key=VehicleIdentity.Key(record.Model,record.Plate);
            if(transport.Tanks.ContainsKey(key))return false;
            var saved=JsonConvert.DeserializeObject<SavedVehicle>(JsonConvert.SerializeObject(record));
            var tank=new FuelTank{Capacity=tankCapacity,Liters=record.Fuel};
            bool inserted=false;
            try{
                writeCash(cash-price);transport.Vehicles.Add(saved);transport.Tanks.Add(key,tank);inserted=true;save();return true;
            }catch(Exception failure){
                transport.Vehicles.Remove(saved);if(inserted || transport.Tanks.TryGetValue(key,out var t) && ReferenceEquals(t,tank))transport.Tanks.Remove(key);
                try{writeCash(cash);}catch(Exception rollback){throw new AggregateException("Purchase failed and cash restoration failed.",failure,rollback);}throw;
            }
        }
    }
    // Confirmation binds a specific policy and pending request; a new request never inherits an old click.
    public sealed class InsuranceConfirmation
    {
        readonly string vehicle,plate,request;readonly int model,excess,bank;readonly bool started;
        bool used;
        public InsuranceConfirmation(VehiclePolicy policy,VehicleRecovery pending,int bank)
        {vehicle=policy.VehicleId;plate=VehicleIdentity.Key(policy.Model,policy.Plate);model=policy.Model;excess=policy.Excess;request=pending?.Id;started=pending?.Started==true;this.bank=bank;}
        public bool Take(VehicleInsuranceData data,int currentBank)
        {
            if(used)return false;used=true;
            if(data==null || !data.Valid || currentBank!=bank)return false;
            var policy=data.Policies.Find(p=>p.VehicleId==vehicle);
            return policy!=null && policy.Model==model && policy.Excess==excess && VehicleIdentity.Key(policy.Model,policy.Plate)==plate && data.Pending?.Id==request && (data.Pending?.Started==true)==started;
        }
    }
}
