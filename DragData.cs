using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RPCore
{
    public sealed class DragOptions : ExtensibleData
    {
        public JobPoint Entry, PlayerStart, RivalStart, PlayerFinish, RivalFinish;
        public int CountdownSeconds = 3, StagingSeconds = 120, RaceSeconds = 90;
        public float AiSpeed = 45, Corridor = 20;
        public List<int> Bets = new List<int>();
        public List<string> Rivals = new List<string>();
        [JsonIgnore] public bool Valid => new[] { Entry, PlayerStart, RivalStart, PlayerFinish, RivalFinish }.All(p => p != null && p.Valid)
            && DragGeometry.Length(PlayerStart, PlayerFinish) >= 100 && DragGeometry.Length(PlayerStart, PlayerFinish) <= 1500
            && DragGeometry.Length(RivalStart, RivalFinish) >= 100 && DragGeometry.Length(RivalStart, RivalFinish) <= 1500
            && CountdownSeconds >= 3 && CountdownSeconds <= 10 && StagingSeconds >= 30 && StagingSeconds <= 300 && RaceSeconds >= 30 && RaceSeconds <= 300
            && !float.IsNaN(AiSpeed) && !float.IsInfinity(AiSpeed) && AiSpeed >= 20 && AiSpeed <= 80 && !float.IsNaN(Corridor) && !float.IsInfinity(Corridor) && Corridor >= 8 && Corridor <= 40
            && Bets != null && Bets.Count > 0 && Bets.Count <= 10 && Bets.All(b => b >= 0 && b <= 10000) && Bets.Distinct().Count() == Bets.Count
            && Rivals != null && Rivals.Count > 0 && Rivals.Count <= 20 && Rivals.All(m => !string.IsNullOrWhiteSpace(m) && m.Length <= 60);
    }
    public static class DragDefaults
    {
        public static DragOptions Load()
        {
            using (var s = typeof(DragDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.Drag.json"))
            using (var r = new System.IO.StreamReader(s)) return JsonConvert.DeserializeObject<DragOptions>(r.ReadToEnd());
        }
    }
    public enum DragOutcome { None, Win, Loss, Tie, FalseStart, Cancelled, Abandoned, Void }
    public sealed class DragEscrow : ExtensibleData
    {
        public string Id;
        public int Stake, TimeMs;
        public bool Started;
        public DragOutcome Result;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(Id) && Id.Length <= 100 && Stake >= 0 && Stake <= 10000 && TimeMs >= 0 && TimeMs <= 600000 && Enum.IsDefined(typeof(DragOutcome), Result);
    }
    public sealed class DragData : ExtensibleData
    {
        public int Entered, Wins, Losses, Draws, Cancelled, FalseStarts, Voids, BestMs, LastMs;
        public long Staked, Returned;
        public string LastResult = "Sem corrida";
        public DragEscrow Pending;
        public List<string> Receipts = new List<string>();
        [JsonIgnore] public bool Valid => Entered >= 0 && Wins >= 0 && Losses >= 0 && Draws >= 0 && Cancelled >= 0 && FalseStarts >= 0 && Voids >= 0 && BestMs >= 0 && LastMs >= 0 && Staked >= 0 && Returned >= 0 && (Pending == null || Pending.Valid) && Receipts != null;
    }
    public static class DragTransactions
    {
        static bool Apply(WalletData w, string receipt, int delta, Func<DragData, bool> mutate, Action save, out bool changed)
        {
            changed = false;
            if (w == null || w.Bank < 0 || string.IsNullOrWhiteSpace(receipt) || receipt.Length > 160 || save == null || (w.Drag != null && !w.Drag.Valid)) return false;
            if (w.Drag?.Receipts.Contains(receipt) == true) return true;
            if ((long)w.Bank + delta < 0 || (long)w.Bank + delta > int.MaxValue) return false;
            var old = w.Drag; var next = old == null ? new DragData() : JsonConvert.DeserializeObject<DragData>(JsonConvert.SerializeObject(old));
            if (!mutate(next) || !next.Valid) return false;
            next.Receipts.Add(receipt); if (next.Receipts.Count > 512) next.Receipts.RemoveRange(0, next.Receipts.Count - 512);
            int bank = w.Bank;
            try { w.Drag = next; w.Bank = bank + delta; save(); changed = true; return true; }
            catch { w.Drag = old; w.Bank = bank; throw; }
        }
        static bool Id(string id) => !string.IsNullOrWhiteSpace(id) && id.Length <= 100;
        public static bool Enter(WalletData w, DragOptions c, int stake, string id, Action save, out bool changed)
        {
            changed = false; if (c == null || !c.Valid || !c.Bets.Contains(stake) || !Id(id)) return false;
            return Apply(w, id + ":enter", -stake, d => { if (d.Pending != null || d.Entered == int.MaxValue || d.Staked > long.MaxValue - stake) return false; d.Pending = new DragEscrow { Id = id, Stake = stake }; d.Entered++; d.Staked += stake; return true; }, save, out changed);
        }
        public static bool Green(WalletData w, string id, Action save, out bool changed)
        {
            changed = false; if (!Id(id)) return false;
            return Apply(w, id + ":green", 0, d => { if (d.Pending == null || d.Pending.Id != id || d.Pending.Started || d.Pending.Result != DragOutcome.None) return false; d.Pending.Started = true; return true; }, save, out changed);
        }
        public static bool Record(WalletData w, string id, DragOutcome result, int ms, Action save, out bool changed)
        {
            changed = false; if (!Id(id) || result == DragOutcome.None || !Enum.IsDefined(typeof(DragOutcome), result) || ms < 0 || ms > 600000) return false;
            return Apply(w, id + ":result", 0, d => {
                var p = d.Pending; if (p == null || p.Id != id || p.Result != DragOutcome.None) return false;
                if ((result == DragOutcome.Win || result == DragOutcome.Loss || result == DragOutcome.Tie || result == DragOutcome.Abandoned) && !p.Started) return false;
                if (result == DragOutcome.Cancelled && p.Started) return false;
                p.Result = result; p.TimeMs = ms; return true;
            }, save, out changed);
        }
        public static int Payout(DragEscrow p) => p == null ? 0 : p.Result == DragOutcome.Win ? p.Stake * 2 : p.Result == DragOutcome.Tie || p.Result == DragOutcome.Cancelled || p.Result == DragOutcome.Void ? p.Stake : 0;
        public static bool Settle(WalletData w, string id, Action save, out bool changed)
        {
            changed = false; if (!Id(id) || w == null || w.Bank < 0 || save == null || w.Drag == null || !w.Drag.Valid) return false;
            if (w.Drag.Receipts.Contains(id + ":paid")) return true;
            var pending = w.Drag.Pending;
            if (pending == null || pending.Id != id || pending.Result == DragOutcome.None) return false;
            int amount = Payout(pending);
            return Apply(w, id + ":paid", amount, d => {
                var p = d.Pending; if (p == null || p.Id != id || d.Returned > long.MaxValue - amount) return false;
                if (p.Result == DragOutcome.Win) { if (d.Wins == int.MaxValue) return false; d.Wins++; }
                else if (p.Result == DragOutcome.Tie) { if (d.Draws == int.MaxValue) return false; d.Draws++; }
                else if (p.Result == DragOutcome.Void) { if (d.Voids == int.MaxValue) return false; d.Voids++; }
                else if (p.Result == DragOutcome.Cancelled) { if (d.Cancelled == int.MaxValue) return false; d.Cancelled++; }
                else { if (d.Losses == int.MaxValue || (p.Result == DragOutcome.FalseStart && d.FalseStarts == int.MaxValue)) return false; d.Losses++; if (p.Result == DragOutcome.FalseStart) d.FalseStarts++; }
                if ((p.Result == DragOutcome.Win || p.Result == DragOutcome.Loss || p.Result == DragOutcome.Tie) && p.TimeMs > 0 && (d.BestMs == 0 || p.TimeMs < d.BestMs)) d.BestMs = p.TimeMs;
                d.LastMs = p.TimeMs; d.LastResult = p.Result.ToString(); d.Returned += amount; d.Pending = null; return true;
            }, save, out changed);
        }
    }
    public sealed class DragPosition
    {
        public readonly double X, Y, Z;
        public DragPosition(double x, double y, double z) { X = x; Y = y; Z = z; }
        public bool Valid => new[] { X, Y, Z }.All(v => !double.IsNaN(v) && !double.IsInfinity(v));
    }
    public static class DragGeometry
    {
        public static double Length(JobPoint a, JobPoint b) => Math.Sqrt((b.X - a.X) * (double)(b.X - a.X) + (b.Y - a.Y) * (double)(b.Y - a.Y));
        public static double Distance(DragPosition a, DragPosition b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z));
        public static double Progress(JobPoint a, JobPoint b, DragPosition p) => ((p.X - a.X) * (b.X - a.X) + (p.Y - a.Y) * (b.Y - a.Y)) / Length(a, b);
        public static double Side(JobPoint a, JobPoint b, DragPosition p) => Math.Abs((p.X - a.X) * (b.Y - a.Y) - (p.Y - a.Y) * (b.X - a.X)) / Length(a, b);
        public static double? Crossing(JobPoint a, JobPoint b, DragPosition previous, DragPosition current, double width = 8)
        {
            if (previous == null || current == null || !previous.Valid || !current.Valid || Length(a, b) < 1) return null;
            double before = Progress(a, b, previous), after = Progress(a, b, current), length = Length(a, b);
            if (before >= length || after < length || after <= before) return null;
            double fraction = (length - before) / (after - before);
            var cross = new DragPosition(previous.X + (current.X - previous.X) * fraction, previous.Y + (current.Y - previous.Y) * fraction, previous.Z + (current.Z - previous.Z) * fraction);
            return Side(a, b, cross) <= width && Math.Abs(cross.Z - b.Z) <= 4 ? (double?)fraction : null;
        }
    }
    public enum DragStage { Align, Countdown, Racing, Result }
    public sealed class DragRun
    {
        public readonly string Id = Guid.NewGuid().ToString("N");
        public readonly DragOptions Options;
        public DragStage Stage { get; private set; } = DragStage.Align;
        public DragOutcome Result { get; private set; }
        public int TimeMs { get; private set; }
        int countdownAt, greenAt, previousAt;
        DragPosition previousPlayer, previousRival;
        public DragRun(DragOptions options) { if (options == null || !options.Valid) throw new ArgumentException(); Options = options; }
        public bool Countdown(int now) { if (Stage != DragStage.Align) return false; countdownAt = now; Stage = DragStage.Countdown; return true; }
        public int CountdownLeft(int now) => Math.Max(0, Options.CountdownSeconds - Math.Max(0, unchecked(now - countdownAt)) / 1000);
        public bool Green(int now, DragPosition player, DragPosition rival, Func<bool> save)
        {
            if (Stage != DragStage.Countdown || CountdownLeft(now) > 0 || player == null || rival == null || !player.Valid || !rival.Valid || !save()) return false;
            greenAt = previousAt = now; previousPlayer = player; previousRival = rival; Stage = DragStage.Racing; return true;
        }
        public void Finish(DragOutcome result, int ms = 0) { if (Stage == DragStage.Result || result == DragOutcome.None || !Enum.IsDefined(typeof(DragOutcome), result)) return; Result = result; TimeMs = Math.Max(0, Math.Min(600000, ms)); Stage = DragStage.Result; }
        public void Sample(int now, DragPosition player, DragPosition rival)
        {
            if (Stage != DragStage.Racing) return;
            int elapsed = unchecked(now - greenAt), dt = unchecked(now - previousAt);
            if (player == null || rival == null || !player.Valid || !rival.Valid) { Finish(DragOutcome.Void); return; }
            if (dt <= 0) return;
            if (DragGeometry.Distance(previousPlayer, player) > 15 + dt * .1) { Finish(DragOutcome.Abandoned); return; }
            if (DragGeometry.Side(Options.PlayerStart, Options.PlayerFinish, player) > Options.Corridor || Math.Abs(player.Z - Options.PlayerStart.Z) > 15) { Finish(DragOutcome.Abandoned); return; }
            if (DragGeometry.Distance(previousRival, rival) > 15 + dt * .1) { Finish(DragOutcome.Void); return; }
            var p = DragGeometry.Crossing(Options.PlayerStart, Options.PlayerFinish, previousPlayer, player);
            var r = DragGeometry.Crossing(Options.RivalStart, Options.RivalFinish, previousRival, rival);
            if (p.HasValue || r.HasValue)
            {
                if (elapsed < 3000) { Finish(DragOutcome.Void); return; }
                int ms = p.HasValue ? unchecked(previousAt - greenAt) + (int)Math.Round(p.Value * dt) : 0;
                if (p.HasValue && r.HasValue && Math.Abs(p.Value - r.Value) * dt <= 10) Finish(DragOutcome.Tie, ms);
                else Finish(p.HasValue && (!r.HasValue || p.Value < r.Value) ? DragOutcome.Win : DragOutcome.Loss, ms);
                return;
            }
            if (elapsed >= Options.RaceSeconds * 1000) { Finish(DragOutcome.Void); return; }
            previousAt = now; previousPlayer = player; previousRival = rival;
        }
    }
}
