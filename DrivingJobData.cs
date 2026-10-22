using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RPCore
{
    public interface IWorkContract
    {
        string Name { get; }
        string Label { get; }
        bool Active { get; }
        void OpenMenu();
        void PollCancel();
    }
    public class UberOptions : ExtensibleData
    {
        public int TripsPerShift = 3, BaseFare = 65, CleanTip = 20, ShiftBonus = 100;
        public float PayPerMeter = .1f, MaxPickupDistance = 2500;
        public List<JobPoint> Locations = new List<JobPoint>();
        public bool Valid => TripsPerShift >= 1 && TripsPerShift <= 30 && BaseFare >= 0 && BaseFare <= 100000 && CleanTip >= 0 && CleanTip <= 100000 && ShiftBonus >= 0 && ShiftBonus <= 100000 &&
            Finite(PayPerMeter) && PayPerMeter >= 0 && PayPerMeter <= 10 && Finite(MaxPickupDistance) && MaxPickupDistance >= 300 && MaxPickupDistance <= 8000 &&
            Locations != null && Locations.Count >= 2 && Locations.All(p => p != null && p.Valid) && Locations.Any(a => Locations.Any(b => DeliveryRoute.Distance(a,b) >= 500 && DeliveryRoute.Distance(a,b) <= 6000));
        static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
    }
    public class ValetOptions : ExtensibleData
    {
        public int CustomersPerShift = 3, BasePay = 120, CleanTip = 40, ShiftBonus = 100;
        public float ParkingRadius = 50;
        public JobPoint Stand, Arrival, Spawn, Parking, Exit;
        public List<string> Cars = new List<string>();
        public bool Valid => CustomersPerShift >= 1 && CustomersPerShift <= 30 && BasePay >= 0 && BasePay <= 100000 && CleanTip >= 0 && CleanTip <= 100000 && ShiftBonus >= 0 && ShiftBonus <= 100000 &&
            !float.IsNaN(ParkingRadius) && !float.IsInfinity(ParkingRadius) && ParkingRadius >= 5 && ParkingRadius <= 60 &&
            new[] { Stand, Arrival, Spawn, Parking, Exit }.All(p => p != null && p.Valid) && Cars != null && Cars.Count > 0 && Cars.All(s => !string.IsNullOrWhiteSpace(s) && s.Length <= 64);
    }
    public static class DrivingDefaults
    {
        static T Read<T>(string name)
        {
            using (var stream = typeof(DrivingDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults." + name + ".json"))
            using (var reader = new System.IO.StreamReader(stream)) return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
        }
        public static UberOptions Uber() => Read<UberOptions>("Uber");
        public static ValetOptions Valet() => Read<ValetOptions>("Valet");
    }
    public static class RideOffers
    {
        public static Tuple<JobPoint,JobPoint> Select(UberOptions config, JobPoint origin, Random random)
        {
            if (!config.Valid || origin == null || !origin.Valid) return null;
            var near = config.Locations.Where(p => DeliveryRoute.Distance(origin,p) <= config.MaxPickupDistance && config.Locations.Any(d => DeliveryRoute.Distance(p,d) >= 500 && DeliveryRoute.Distance(p,d) <= 6000)).OrderBy(p => DeliveryRoute.Distance(origin,p)).Take(8).ToList();
            if (near.Count == 0) return null;
            var pickup = near[random.Next(near.Count)];
            var drops = config.Locations.Where(p => DeliveryRoute.Distance(pickup,p) >= 500 && DeliveryRoute.Distance(pickup,p) <= 6000).ToList();
            return drops.Count == 0 ? null : Tuple.Create(pickup,drops[random.Next(drops.Count)]);
        }
    }
    public sealed class DrivingShift
    {
        public readonly string Id = Guid.NewGuid().ToString("N");
        public readonly int Total;
        public int Completed { get; private set; }
        public bool Cancelled { get; private set; }
        public bool Finished => Completed == Total;
        public DrivingShift(int count) { if (count < 1 || count > 30) throw new ArgumentOutOfRangeException(nameof(count)); Total = count; }
        public bool Confirm(Func<int,bool,bool> payment)
        {
            if (Cancelled || Finished || !payment(Completed + 1, Completed + 1 == Total)) return false;
            Completed++; return true;
        }
        public void Cancel() { Cancelled = true; }
    }
    public enum RideStage { Pickup, Boarding, Riding, Exiting, Payment, Complete, Cancelled }
    public sealed class RideRun
    {
        public RideStage Stage { get; private set; } = RideStage.Pickup;
        public bool Board() { if (Stage != RideStage.Pickup) return false; Stage = RideStage.Boarding; return true; }
        public bool Boarded(bool correctVehicle) { if (Stage != RideStage.Boarding || !correctVehicle) return false; Stage = RideStage.Riding; return true; }
        public void BoardingTimedOut() { if (Stage == RideStage.Boarding) Stage = RideStage.Pickup; }
        public bool Arrived(bool stoppedAtDestination) { if (Stage != RideStage.Riding || !stoppedAtDestination) return false; Stage = RideStage.Exiting; return true; }
        public bool PassengerExited(bool outsideAndAlive) { if (Stage != RideStage.Exiting || !outsideAndAlive) return false; Stage = RideStage.Payment; return true; }
        public bool Pay(Func<bool> commit) { if (Stage != RideStage.Payment || !commit()) return false; Stage = RideStage.Complete; return true; }
        public void Cancel() { if (Stage != RideStage.Complete) Stage = RideStage.Cancelled; }
    }
    public enum ValetStage { Arriving, ClientLeaving, Parking, WalkToStand, Retrieve, ReturnCar, ClientBoarding, Payment, Complete, Cancelled }
    public sealed class ValetRun
    {
        public ValetStage Stage { get; private set; } = ValetStage.Arriving;
        bool Move(ValetStage from, ValetStage to, bool condition) { if (Stage != from || !condition) return false; Stage = to; return true; }
        public bool Arrived(bool stopped) => Move(ValetStage.Arriving,ValetStage.ClientLeaving,stopped);
        public bool ClientLeft(bool emptyCar) => Move(ValetStage.ClientLeaving,ValetStage.Parking,emptyCar);
        public bool Park(bool inAreaEmptyAndStopped) => Move(ValetStage.Parking,ValetStage.WalkToStand,inAreaEmptyAndStopped);
        public bool RequestReturn(bool atStandOnFoot) => Move(ValetStage.WalkToStand,ValetStage.Retrieve,atStandOnFoot);
        public bool Collect(bool drivingCorrectCar) => Move(ValetStage.Retrieve,ValetStage.ReturnCar,drivingCorrectCar);
        public bool Return(bool emptyAtHotel) => Move(ValetStage.ReturnCar,ValetStage.ClientBoarding,emptyAtHotel);
        public bool ClientBoarded(bool clientDriving) => Move(ValetStage.ClientBoarding,ValetStage.Payment,clientDriving);
        public bool Pay(Func<bool> commit) { if (Stage != ValetStage.Payment || !commit()) return false; Stage = ValetStage.Complete; return true; }
        public void Cancel() { if (Stage != ValetStage.Complete) Stage = ValetStage.Cancelled; }
    }
    public sealed class DrivingQuality
    {
        readonly float initial;
        public float Damage { get; private set; }
        public DrivingQuality(float bodyHealth) { initial = Valid(bodyHealth) ? bodyHealth : 1000; }
        static bool Valid(float n) => !float.IsNaN(n) && !float.IsInfinity(n) && n >= 0;
        public void Sample(float health) { if (Valid(health)) Damage = Math.Max(Damage,Math.Max(0,initial-health)); }
        public int Stars => Damage <= 10 ? 5 : Damage <= 75 ? 4 : Damage <= 200 ? 3 : Damage <= 400 ? 2 : 1;
        public int Reward(int quote, int tip, int shiftBonus)
        {
            if (quote < 0 || tip < 0 || shiftBonus < 0) throw new ArgumentOutOfRangeException();
            return (int)Math.Min(1000000L,(long)quote + (Stars >= 4 ? tip : 0) + shiftBonus);
        }
    }
}
