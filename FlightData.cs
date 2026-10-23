using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RPCore
{
    public enum FlightMode { Training,Plane,Helicopter }
    public sealed class PilotOptions : ExtensibleData
    {
        public JobPoint Start,PlaneSpawn,PlaneDestination,HeliSpawn;
        public List<JobPoint> Helipads=new List<JobPoint>();
        public string PlaneModel="velum",HeliModel="frogger";
        public int TimedHelipadIndex=0,HeliTimeLimitSeconds=240;
        public bool KeepLegacyLsiaBase;
        public int PlanePassengers=2,HeliPassengers=1,PlaneBasePay=1400,HeliBasePay=3500,PlaneReturnBonus=500,HeliReturnBonus=750;
        public float PlanePerMeter=.1f,HeliPerMeter=.25f,MinimumHealth=400;
        [JsonIgnore] public bool Valid=>new[]{Start,PlaneSpawn,PlaneDestination,HeliSpawn}.All(p=>p!=null && p.Valid) && Helipads!=null && Helipads.Count>=1 && Helipads.Count<=100 && Helipads.All(p=>p!=null && p.Valid) &&
            TimedHelipadIndex>=-1 && TimedHelipadIndex<Helipads.Count && HeliTimeLimitSeconds>=0 && HeliTimeLimitSeconds<=1800 && new[]{PlaneModel,HeliModel}.All(s=>!string.IsNullOrWhiteSpace(s) && s.Length<=64) && PlanePassengers>=1 && PlanePassengers<=8 && HeliPassengers>=1 && HeliPassengers<=3 &&
            new[]{PlaneBasePay,HeliBasePay,PlaneReturnBonus,HeliReturnBonus}.All(n=>n>=0 && n<=100000) && new[]{PlanePerMeter,HeliPerMeter}.All(n=>!float.IsNaN(n) && !float.IsInfinity(n) && n>=0 && n<=10) && !float.IsNaN(MinimumHealth) && !float.IsInfinity(MinimumHealth) && MinimumHealth>=0 && MinimumHealth<=900;
    }
    public static class PilotDefaults
    {
        public static PilotOptions Load()
        {
            using(var stream=typeof(PilotDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.Pilot.json"))
            using(var reader=new System.IO.StreamReader(stream))return JsonConvert.DeserializeObject<PilotOptions>(reader.ReadToEnd());
        }
    }
    public static class PilotAccess
    {
        static bool Same(JobPoint p,float x,float y,float z,float heading)=>p!=null && p.Valid && Math.Abs(p.X-x)<.01 && Math.Abs(p.Y-y)<.01 && Math.Abs(p.Z-z)<.01 && Math.Abs(p.Heading-heading)<.01;
        public static bool LegacyBase(PilotOptions c)=>c!=null && Same(c.Start,-1143.238f,-2705.508f,13.95856f,0) && Same(c.PlaneSpawn,-1023.876f,-2903.852f,13.96453f,148.8467f) && Same(c.HeliSpawn,-1111.612f,-2883.941f,13.94604f,146.4589f);
        public static PilotOptions Resolve(PilotOptions local,out bool migrated)
        {
            if(local==null || !local.Valid)throw new ArgumentException("Invalid pilot options.");
            migrated=LegacyBase(local) && !local.KeepLegacyLsiaBase;
            if(!migrated)return local;
            // Read-only migration of recognized shipped coordinates. Never rewrite the local JSON or license.
            var result=JsonConvert.DeserializeObject<PilotOptions>(JsonConvert.SerializeObject(local));
            var defaults=PilotDefaults.Load();result.Start=defaults.Start;result.PlaneSpawn=defaults.PlaneSpawn;result.HeliSpawn=defaults.HeliSpawn;
            if(result.TimedHelipadIndex==0 && result.HeliTimeLimitSeconds==150)result.HeliTimeLimitSeconds=defaults.HeliTimeLimitSeconds;
            return result;
        }
    }
    public static class FlightRules
    {
        public static bool Landed(bool pilot,bool airborne,double height,double speed,double horizontalDistance,double verticalDistance,double radius)
        {return pilot && !airborne && new[]{height,speed,horizontalDistance,verticalDistance,radius}.All(v=>!double.IsNaN(v) && !double.IsInfinity(v) && v>=0) && height<4 && speed<.5 && horizontalDistance<=radius && verticalDistance<=8 && radius>0 && radius<=60;}
        public static bool InFlight(bool pilot,bool airborne,double height,double speed,bool helicopter)
        {return pilot && airborne && new[]{height,speed}.All(v=>!double.IsNaN(v) && !double.IsInfinity(v) && v>=0) && height>=(helicopter?10:20) && speed>=(helicopter?3:15);}
        public static bool Airworthy(float engine,float body,float minimum)
        {return new[]{engine,body,minimum}.All(n=>!float.IsNaN(n) && !float.IsInfinity(n) && n>=0) && engine>=minimum && body>=minimum;}
        public static bool CanRetire(bool empty,bool airborne,double height,double speed)=>empty && !airborne && new[]{height,speed}.All(v=>!double.IsNaN(v) && !double.IsInfinity(v) && v>=0) && height<4 && speed<.5;
        public static bool Expired(int now,int began,int seconds){if(seconds<0 || seconds>1800)throw new ArgumentOutOfRangeException(nameof(seconds));return seconds>0 && unchecked(now-began)>=seconds*1000;}
        public static bool Qualified(JobLedger ledger)=>ledger?.Jobs!=null && ledger.Jobs.TryGetValue("pilot_training",out var p) && p!=null && p.CompletedShifts>0;
    }
    public enum FlightStage { AwaitPilot,AwaitTakeoff,EnRoute,Exiting,PaymentPending,Returning,Finished,Cancelled }
    public sealed class FlightRun
    {
        public readonly string Id=Guid.NewGuid().ToString("N");
        public readonly FlightMode Mode;
        public readonly int Passengers;
        public FlightStage Stage {get;private set;}=FlightStage.AwaitPilot;
        public bool ReturnFlightConfirmed {get;private set;}
        bool observing;
        int flightAt;
        int RequiredMilliseconds=>Mode==FlightMode.Helicopter?6000:10000;
        public FlightRun(FlightMode mode,int passengers)
        {if(!Enum.IsDefined(typeof(FlightMode),mode) || passengers<0 || passengers>8 || (mode==FlightMode.Training?passengers!=0:passengers==0))throw new ArgumentOutOfRangeException();Mode=mode;Passengers=passengers;}
        public bool Prepare(bool stoppedPilot,int seatedAlive)
        {if(Stage!=FlightStage.AwaitPilot || !stoppedPilot || seatedAlive!=Passengers)return false;Stage=FlightStage.AwaitTakeoff;return true;}
        public bool ObserveFlight(int now,bool qualifiedAirborne)
        {
            if(Stage!=FlightStage.AwaitTakeoff && Stage!=FlightStage.Returning)return false;
            if(!qualifiedAirborne){observing=false;return false;}
            if(!observing){observing=true;flightAt=now;return false;}
            if(unchecked(now-flightAt)<RequiredMilliseconds)return false;
            if(Stage==FlightStage.Returning){ReturnFlightConfirmed=true;return true;}
            Stage=FlightStage.EnRoute;observing=false;return true;
        }
        public bool Arrive(bool landed)
        {if(Stage!=FlightStage.EnRoute || !landed)return false;Stage=Passengers==0?FlightStage.PaymentPending:FlightStage.Exiting;return true;}
        public bool Disembarked(bool landed,int outsideAlive)
        {if(Stage!=FlightStage.Exiting || !landed || outsideAlive!=Passengers)return false;Stage=FlightStage.PaymentPending;return true;}
        public bool PayArrival(bool validHandover,Func<int,bool> pay)
        {if(Stage!=FlightStage.PaymentPending || !validHandover || !pay(1))return false;Stage=Mode==FlightMode.Training?FlightStage.Finished:FlightStage.Returning;observing=false;return true;}
        public bool Return(bool landedEmpty,Func<int,bool> pay)
        {if(Stage!=FlightStage.Returning || !ReturnFlightConfirmed || !landedEmpty || !pay(2))return false;Stage=FlightStage.Finished;return true;}
        public void Cancel(){if(Stage!=FlightStage.Finished)Stage=FlightStage.Cancelled;}
    }
}
