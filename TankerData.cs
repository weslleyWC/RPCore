using System;
using System.Linq;
using Newtonsoft.Json;

namespace RPCore
{
    public class TankerOptions : DeliveryOptions
    {
        public JobPoint Trailer = new JobPoint();
        public int CandidateStations = 8, UnloadSeconds = 6;
        [JsonIgnore] public bool ValidTanker => Valid && Drops == 1 && Trailer != null && Trailer.Valid && CandidateStations >= 1 && CandidateStations <= 100 && UnloadSeconds >= 3 && UnloadSeconds <= 30;
    }
    public static class TankerDefaults
    {
        public static TankerOptions Load()
        {
            using (var s = typeof(TankerDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.FuelDelivery.json"))
            using (var r = new System.IO.StreamReader(s)) return JsonConvert.DeserializeObject<TankerOptions>(r.ReadToEnd());
        }
        public static JobPoint Select(TankerOptions options, Random random)
        {
            if (options == null || !options.ValidTanker) throw new ArgumentException("Invalid tanker configuration.");
            var candidates = options.Addresses.GroupBy(p => new {p.X,p.Y,p.Z}).Select(g => g.First()).OrderBy(p => DeliveryRoute.Distance(options.Start,p)).Take(options.CandidateStations).ToArray();
            return candidates[random.Next(candidates.Length)];
        }
    }
    public enum TankerStage { AwaitCoupling, Delivering, Unloading, PaymentPending, Returning, Finished, Cancelled }
    // Only native observations supplied by the runtime can enable unloading/return.
    public static class TankerRules
    {
        public static bool Stopped(bool driver, bool coupled, bool engineOff, double truckSpeed, double trailerSpeed, double truckDistance, double trailerDistance, double radius)
        {
            var values = new[] {truckSpeed,trailerSpeed,truckDistance,trailerDistance,radius};
            return values.All(v => !double.IsNaN(v) && !double.IsInfinity(v) && v >= 0) && radius > 0 && radius <= 60 &&
                driver && coupled && engineOff && truckSpeed < .5 && trailerSpeed < .5 && truckDistance <= radius && trailerDistance <= radius;
        }
        public static bool CanCouple(bool driver, bool hasTrailer, double distance, double headingDelta, double truckSpeed, double trailerSpeed)
        {
            return !hasTrailer && driver && new[] {distance,headingDelta,truckSpeed,trailerSpeed}.All(v => !double.IsNaN(v) && !double.IsInfinity(v) && v >= 0) &&
                distance <= 6 && headingDelta <= 35 && truckSpeed < .5 && trailerSpeed < .5;
        }
    }
    public sealed class TankerRun
    {
        public readonly string Id = Guid.NewGuid().ToString("N");
        public TankerStage Stage {get;private set;} = TankerStage.AwaitCoupling;
        readonly int duration;
        int began;
        public TankerRun(int seconds) { if(seconds < 3 || seconds > 30)throw new ArgumentOutOfRangeException(nameof(seconds));duration=seconds*1000; }
        public bool ObserveCoupling(bool coupled)
        {
            if(Stage != TankerStage.AwaitCoupling || !coupled)return false;
            Stage=TankerStage.Delivering;return true;
        }
        public bool BeginUnload(int now, bool eligible)
        {if(Stage!=TankerStage.Delivering || !eligible)return false;began=now;Stage=TankerStage.Unloading;return true;}
        public void Interrupt() {if(Stage==TankerStage.Unloading)Stage=TankerStage.Delivering;}
        public bool Advance(int now, bool eligible)
        {
            if(Stage!=TankerStage.Unloading)return false;
            if(!eligible){Interrupt();return false;}
            if(unchecked(now-began)<duration)return false;
            Stage=TankerStage.PaymentPending;return true;
        }
        public bool PayUnload(bool eligible, Func<int,bool> pay)
        {if(Stage!=TankerStage.PaymentPending || !eligible || !pay(1))return false;Stage=TankerStage.Returning;return true;}
        public bool Return(bool eligible, Func<int,bool> pay)
        {if(Stage!=TankerStage.Returning || !eligible || !pay(2))return false;Stage=TankerStage.Finished;return true;}
        public void Cancel(){if(Stage!=TankerStage.Finished)Stage=TankerStage.Cancelled;}
    }
}
