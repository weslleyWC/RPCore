using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class VehicleInsuranceOptions : ExtensibleData
    {
        public int Excess=10000,MaxPolicies=64;
        [JsonIgnore] public bool Valid=>Excess>=0 && Excess<=1000000 && MaxPolicies>=1 && MaxPolicies<=128;
    }
    public static class VehicleInsuranceDefaults
    {
        public static VehicleInsuranceOptions Load(){using(var s=typeof(VehicleInsuranceDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.VehicleInsurance.json"))using(var r=new System.IO.StreamReader(s))return JsonConvert.DeserializeObject<VehicleInsuranceOptions>(r.ReadToEnd());}
    }
    public sealed class VehiclePolicy : ExtensibleData
    {
        public string VehicleId,Plate;public int Model,Excess;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(VehicleId) && VehicleId.Length<=100 && Model!=0 && !string.IsNullOrWhiteSpace(Plate) && Plate.Trim().Length<=8 && Excess>=0 && Excess<=1000000;
    }
    public sealed class VehicleRecovery : ExtensibleData
    {
        public string Id,VehicleId;public int Handle,Excess;public bool Started;public SavedVehicle Original;public float Oil;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=100 && !string.IsNullOrWhiteSpace(VehicleId) && Handle!=0 && Excess>=0 && Excess<=1000000 && Original!=null && Original.Id==VehicleId && VehicleInsuranceRules.Snapshot(Original) && VehicleInsuranceRules.Finite(Oil) && Oil>=0 && Oil<=10000;
    }
    public sealed class VehicleInsuranceData : ExtensibleData
    {
        public List<VehiclePolicy> Policies=new List<VehiclePolicy>();public List<string> Receipts=new List<string>();public VehicleRecovery Pending;public int Claims;public long Spent;
        [JsonIgnore] public bool Valid=>Policies!=null && Policies.Count<=128 && Policies.All(p=>p!=null && p.Valid) && Policies.Select(p=>p.VehicleId).Distinct().Count()==Policies.Count && Policies.Select(p=>VehicleIdentity.Key(p.Model,p.Plate)).Distinct().Count()==Policies.Count && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=120) && Receipts.Distinct().Count()==Receipts.Count && Claims>=0 && Spent>=0 && (Pending==null || Pending.Valid && Spent>=Pending.Excess && Policies.Any(p=>p.VehicleId==Pending.VehicleId && p.Excess==Pending.Excess && VehicleInsuranceRules.Same(p,Pending.Original)));
    }
    public static class VehicleInsuranceRules
    {
        public static bool Finite(float n)=>!float.IsNaN(n) && !float.IsInfinity(n);
        public static bool Snapshot(SavedVehicle v)=>v!=null && v.Model!=0 && !string.IsNullOrWhiteSpace(v.Id) && !string.IsNullOrWhiteSpace(v.Plate) && v.Plate.Trim().Length<=8 && new[]{v.EngineHealth,v.BodyHealth,v.TankHealth,v.Fuel,v.X,v.Y,v.Z,v.Heading}.All(Finite) && v.Fuel>=0 && v.Fuel<=10000;
        public static bool Same(VehiclePolicy p,SavedVehicle v)=>p!=null && v!=null && p.VehicleId==v.Id && VehicleIdentity.Key(p.Model,p.Plate)==VehicleIdentity.Key(v.Model,v.Plate);
        public static bool Destroyed(bool dead,bool considered,float engine,float tank)=>Finite(engine) && Finite(tank) && (dead || considered || engine<=0 || tank<=0);
        public static SavedVehicle Owned(WalletData w,string id)=>w?.Transport?.Vehicles?.FirstOrDefault(v=>v!=null && v.Id==id);
        public static bool Registry(WalletData w,string id)
        {if(w?.Transport==null)return false;try{w.Transport.Validate();}catch{return false;}var v=Owned(w,id);return v!=null && w.Transport.Vehicles.Count(c=>c!=null && c.Id==id)==1 && Snapshot(v) && w.Transport.Garages!=null && w.Transport.Garages.Contains(v.GarageId) && w.Transport.Vehicles.Count(c=>c!=null && VehicleIdentity.Key(c.Model,c.Plate)==VehicleIdentity.Key(v.Model,v.Plate))==1;}
    }
    public static class VehicleInsuranceTransactions
    {
        static T Copy<T>(T value)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        static bool Apply(WalletData w,string receipt,Func<VehicleInsuranceData,int?> mutation,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || string.IsNullOrWhiteSpace(receipt) || receipt.Length>120 || w.VehicleInsurance!=null && !w.VehicleInsurance.Valid)return false;
            var old=w.VehicleInsurance;if(old?.Receipts.Contains(receipt)==true)return true;var next=old==null?new VehicleInsuranceData():Copy(old);int? delta=mutation(next);
            if(delta==null || (long)w.Bank+delta.Value<0 || (long)w.Bank+delta.Value>int.MaxValue || !next.Valid)return false;
            next.Receipts.Add(receipt);if(next.Receipts.Count>256)next.Receipts.RemoveAt(0);int bank=w.Bank;
            try{w.VehicleInsurance=next;w.Bank+=delta.Value;save();changed=true;return true;}catch{w.VehicleInsurance=old;w.Bank=bank;throw;}
        }
        public static bool Enroll(WalletData w,VehicleInsuranceOptions options,string vehicleId,Action save,out bool changed)
        {
            changed=false;if(options==null || !options.Valid || !VehicleInsuranceRules.Registry(w,vehicleId))return false;var v=VehicleInsuranceRules.Owned(w,vehicleId);
            return Apply(w,Guid.NewGuid().ToString("N")+":enroll",d=>{
                if(d.Policies.Count>=options.MaxPolicies || d.Policies.Any(p=>p.VehicleId==vehicleId || VehicleIdentity.Key(p.Model,p.Plate)==VehicleIdentity.Key(v.Model,v.Plate)))return null;
                d.Policies.Add(new VehiclePolicy{VehicleId=v.Id,Model=v.Model,Plate=v.Plate.Trim(),Excess=options.Excess});return 0;
            },save,out changed);
        }
        public static bool Cancel(WalletData w,string vehicleId,Action save,out bool changed)
        {return Apply(w,Guid.NewGuid().ToString("N")+":cancel",d=>{var p=d.Policies.Find(v=>v.VehicleId==vehicleId);if(p==null || d.Pending?.VehicleId==vehicleId)return null;d.Policies.Remove(p);return 0;},save,out changed);}
        public static bool Begin(WalletData w,string vehicleId,SavedVehicle original,int handle,float oil,bool destroyed,string id,Action save,out bool changed)
        {
            changed=false;if(!destroyed || !VehicleInsuranceRules.Registry(w,vehicleId) || !VehicleInsuranceRules.Snapshot(original) || original.Id!=vehicleId || handle==0 || !VehicleInsuranceRules.Finite(oil) || oil<0 || oil>10000 || string.IsNullOrWhiteSpace(id) || id.Length>100)return false;
            return Apply(w,id+":begin",d=>{
                var p=d.Policies.Find(v=>v.VehicleId==vehicleId);var owned=VehicleInsuranceRules.Owned(w,vehicleId);
                if(d.Pending!=null || p==null || !VehicleInsuranceRules.Same(p,owned) || !VehicleInsuranceRules.Same(p,original) || owned.Stored || owned.PendingRemoval || d.Spent>long.MaxValue-p.Excess || d.Claims==int.MaxValue)return null;
                d.Pending=new VehicleRecovery{Id=id,VehicleId=vehicleId,Handle=handle,Original=Copy(original),Oil=oil,Excess=p.Excess};d.Spent+=p.Excess;return -p.Excess;
            },save,out changed);
        }
        public static bool Start(WalletData w,string id,Action save,out bool changed)=>Apply(w,id+":start",d=>{if(d.Pending==null || d.Pending.Id!=id)return null;d.Pending.Started=true;return 0;},save,out changed);
        public static bool Complete(WalletData w,string id,SavedVehicle repaired,Action save,out bool changed)
        {
            changed=false;if(!VehicleInsuranceRules.Snapshot(repaired) || !VehicleInsuranceRules.Registry(w,repaired.Id) || repaired.EngineHealth<900 || repaired.BodyHealth<900 || repaired.TankHealth<900)return false;
            var transport=w.Transport;var old=VehicleInsuranceRules.Owned(w,repaired.Id);int index=transport.Vehicles.IndexOf(old);var replacement=Copy(repaired);replacement.Extra=Copy(old.Extra);replacement.GarageId=old.GarageId;replacement.Stored=false;replacement.PendingRemoval=false;
            return Apply(w,id+":complete",d=>{
                var p=d.Pending;if(p==null || !p.Started || p.Id!=id || p.VehicleId!=repaired.Id || old.Stored || old.PendingRemoval || !VehicleInsuranceRules.Same(d.Policies.Find(v=>v.VehicleId==p.VehicleId),old) || VehicleIdentity.Key(p.Original.Model,p.Original.Plate)!=VehicleIdentity.Key(repaired.Model,repaired.Plate) || repaired.Fuel>p.Original.Fuel+.05f || transport.Tanks.TryGetValue(VehicleIdentity.Key(repaired.Model,repaired.Plate),out var tank) && repaired.Fuel>tank.Liters+.05f || d.Claims==int.MaxValue)return null;
                d.Claims++;d.Pending=null;return 0;
            },()=>{transport.Vehicles[index]=replacement;try{save();}catch{transport.Vehicles[index]=old;throw;}},out changed);
        }
        public static bool Refund(WalletData w,string id,Action save,out bool changed)=>Apply(w,id+":refund",d=>{var p=d.Pending;if(p==null || p.Id!=id || p.Started)return null;d.Spent-=p.Excess;int amount=p.Excess;d.Pending=null;return amount;},save,out changed);
        public static bool Abandon(WalletData w,string id,Action save,out bool changed)=>Apply(w,id+":abandon",d=>{if(d.Pending==null || d.Pending.Id!=id || !d.Pending.Started)return null;d.Pending=null;return 0;},save,out changed);
    }
}
