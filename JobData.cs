using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RPCore
{
    public class PizzaStats : ExtensibleData { public int Deliveries, Earned; }
    public class JobPoint : ExtensibleData
    {
        public string Name;
        public float X, Y, Z, Heading;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(Name) && Finite(X) && Finite(Y) && Finite(Z) && Finite(Heading) && Math.Abs(X) < 15000 && Math.Abs(Y) < 15000 && Z > -100 && Z < 2000;
        static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
    }
    public class DeliveryOptions : ExtensibleData
    {
        public int Drops = 5, BasePay = 90, ReturnBonus = 150;
        public float PayPerMeter = .04f;
        public JobPoint Start = new JobPoint { Name = "Post OP", X = -230.7383f, Y = -913.2202f, Z = 32.3108f };
        public JobPoint Depot = new JobPoint { Name = "Post OP depot", X = -282.6413f, Y = -914.9976f, Z = 30.89793f, Heading = 250 };
        public List<JobPoint> Addresses = new List<JobPoint>();
        public bool Valid => Drops >= 1 && Drops <= 30 && BasePay >= 0 && BasePay <= 100000 && ReturnBonus >= 0 && ReturnBonus <= 100000 &&
            !float.IsNaN(PayPerMeter) && !float.IsInfinity(PayPerMeter) && PayPerMeter >= 0 && PayPerMeter <= 10 && Start != null && Start.Valid && Depot != null && Depot.Valid &&
            Addresses != null && Addresses.Count >= Drops && Addresses.All(p => p != null && p.Valid) && Addresses.Select(p => p.X + ":" + p.Y + ":" + p.Z).Distinct().Count() >= Drops;
    }
    public static class JobDefaults
    {
        public static DeliveryOptions PostOp()
        {
            using (var stream = typeof(JobDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.PostOp.json"))
            using (var reader = new System.IO.StreamReader(stream)) return JsonConvert.DeserializeObject<DeliveryOptions>(reader.ReadToEnd());
        }
    }
    public static class DeliveryRoute
    {
        public static List<JobPoint> Select(IEnumerable<JobPoint> points, JobPoint origin, int count, Random random)
        {
            var pool = points.Where(p => p != null && p.Valid).GroupBy(p => new { p.X, p.Y, p.Z }).Select(g => g.First()).ToList();
            if (count < 1 || count > pool.Count || origin == null || !origin.Valid) throw new ArgumentException("Not enough valid delivery addresses.");
            for (int i = pool.Count - 1; i > 0; i--) { int j = random.Next(i + 1); var t = pool[i]; pool[i] = pool[j]; pool[j] = t; }
            pool = pool.Take(count).ToList(); var route = new List<JobPoint>(); var current = origin;
            while (pool.Count > 0) { var next = pool.OrderBy(p => Distance(current, p)).First(); route.Add(next); pool.Remove(next); current = next; }
            return route;
        }
        public static double Distance(JobPoint a, JobPoint b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        public static int Pay(int basePay, float perMeter, double distance, int bonus = 0)
        {
            if (basePay < 0 || bonus < 0 || perMeter < 0 || float.IsNaN(perMeter) || float.IsInfinity(perMeter) || double.IsNaN(distance) || double.IsInfinity(distance) || distance < 0) throw new ArgumentOutOfRangeException();
            // Rates are currency coefficients: discard binary float noise before flooring dollars.
            double rate = Math.Round((double)perMeter, 6, MidpointRounding.AwayFromZero);
            return (int)Math.Min(1000000, basePay + Math.Min(1000000, Math.Floor(distance * rate)) + (double)bonus);
        }
    }
    public sealed class ShiftGate
    {
        public string Owner { get; private set; }
        public bool Acquire(string owner) { if (string.IsNullOrEmpty(owner) || Owner != null) return false; Owner = owner; return true; }
        public void Release(string owner) { if (Owner == owner) Owner = null; }
    }
    public enum DeliveryStage { AwaitVehicle, NeedPackage, Carrying, Handover, Returning, Finished, Cancelled }
    public sealed class DeliveryRun
    {
        public readonly string Id = Guid.NewGuid().ToString("N");
        public readonly int Total;
        public readonly bool ReturnRequired;
        public int Delivered { get; private set; }
        public DeliveryStage Stage { get; private set; } = DeliveryStage.AwaitVehicle;
        public int ReceiptStep => Delivered + 1;
        public DeliveryRun(int total, bool returnRequired) { if (total < 1 || total > 100) throw new ArgumentOutOfRangeException(nameof(total)); Total = total; ReturnRequired = returnRequired; }
        public bool EnterVehicle() { if (Stage != DeliveryStage.AwaitVehicle) return false; Stage = DeliveryStage.NeedPackage; return true; }
        public bool TakePackage() { if (Stage != DeliveryStage.NeedPackage) return false; Stage = DeliveryStage.Carrying; return true; }
        public bool BeginHandover() { if (Stage != DeliveryStage.Carrying) return false; Stage = DeliveryStage.Handover; return true; }
        public void RecoverPackage() { if (Stage == DeliveryStage.Carrying || Stage == DeliveryStage.Handover) Stage = DeliveryStage.NeedPackage; }
        public void InterruptHandover() { if (Stage == DeliveryStage.Handover) Stage = DeliveryStage.Carrying; }
        public bool CommitDelivery(Func<int, bool> pay)
        {
            if (Stage != DeliveryStage.Handover || !pay(ReceiptStep)) return false;
            Delivered++; Stage = Delivered == Total ? (ReturnRequired ? DeliveryStage.Returning : DeliveryStage.Finished) : DeliveryStage.NeedPackage; return true;
        }
        public bool ReturnToDepot(Func<int, bool> pay)
        {
            if (Stage != DeliveryStage.Returning || !pay(Total + 1)) return false;
            Stage = DeliveryStage.Finished; return true;
        }
        public void Cancel() { if (Stage != DeliveryStage.Finished) Stage = DeliveryStage.Cancelled; }
    }
    public class DeliveryProgress : ExtensibleData
    {
        public int Deliveries, CompletedShifts;
        public long Earned;
        public Dictionary<string, int> Receipts = new Dictionary<string, int>();
        public List<string> RecentSessions = new List<string>();
    }
    public class JobLedger : ExtensibleData
    {
        public Dictionary<string, DeliveryProgress> Jobs = new Dictionary<string, DeliveryProgress>();
        public DeliveryProgress Get(string job)
        {
            if (Jobs == null) Jobs = new Dictionary<string, DeliveryProgress>();
            DeliveryProgress value;
            if (!Jobs.TryGetValue(job, out value) || value == null) Jobs[job] = value = new DeliveryProgress();
            return value;
        }
    }
    public static class DeliveryPayments
    {
        // Receipt, bank/MP cash and lifetime totals share the same atomic Wallet.json checkpoint.
        // Native protagonist cash is external GTA state; rollback covers write failures in this session.
        public static bool Commit(WalletData wallet, Func<int> cash, Action<int> setCash, string job, string session, int step, int amount, bool bank, bool delivery, bool complete, Action save, out bool credited)
        {
            credited = false;
            if (string.IsNullOrWhiteSpace(job) || string.IsNullOrWhiteSpace(session) || step < 1 || step > 101 || amount < 0 || amount > 1000000) return false;
            var old = wallet.Jobs;
            var next = old == null ? new JobLedger() : JsonConvert.DeserializeObject<JobLedger>(JsonConvert.SerializeObject(old));
            var stats = next.Get(job);
            if (stats.Receipts == null) stats.Receipts = new Dictionary<string, int>();
            if (stats.RecentSessions == null) stats.RecentSessions = new List<string>();
            int paid; if (stats.Receipts.TryGetValue(session, out paid) && paid >= step) return true;
            if (step != (stats.Receipts.TryGetValue(session, out paid) ? paid : 0) + 1) return false;
            int before = bank ? wallet.Bank : cash();
            if (before < 0 || before > int.MaxValue - amount || stats.Earned < 0 || stats.Earned > long.MaxValue - amount || stats.Deliveries < 0 || (delivery && stats.Deliveries == int.MaxValue) || stats.CompletedShifts < 0 || (complete && stats.CompletedShifts == int.MaxValue)) return false;
            if (!stats.Receipts.ContainsKey(session)) stats.RecentSessions.Add(session);
            stats.Receipts[session] = step;
            while (stats.RecentSessions.Count > 64) { stats.Receipts.Remove(stats.RecentSessions[0]); stats.RecentSessions.RemoveAt(0); }
            stats.Earned += amount; if (delivery) stats.Deliveries++; if (complete) stats.CompletedShifts++;
            try
            {
                wallet.Jobs = next;
                if (bank) wallet.Bank = before + amount; else setCash(before + amount);
                save(); credited = true; return true;
            }
            catch
            {
                wallet.Jobs = old;
                if (bank) wallet.Bank = before; else setCash(before);
                throw;
            }
        }
    }
}
