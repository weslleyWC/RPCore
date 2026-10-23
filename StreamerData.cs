using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RPCore
{
    public sealed class StreamerOptions : ExtensibleData
    {
        public int DurationSeconds = 180, TickSeconds = 30, BaseRevenue = 5, SubscriptionValue = 7, BitsValue = 1, DonationMin = 10, DonationMax = 60;
        public bool Overlay = true;
        [JsonIgnore] public int Blocks => DurationSeconds / Math.Max(1, TickSeconds);
        [JsonIgnore] public bool Valid => DurationSeconds >= 30 && DurationSeconds <= 1800 && TickSeconds >= 5 && TickSeconds <= 60 && DurationSeconds >= TickSeconds && DurationSeconds % TickSeconds == 0
            && BaseRevenue >= 0 && BaseRevenue <= 1000 && SubscriptionValue >= 0 && SubscriptionValue <= 1000 && BitsValue >= 0 && BitsValue <= 100 && DonationMin >= 1 && DonationMax >= DonationMin && DonationMax <= 10000;
    }
    public static class StreamerDefaults
    {
        public static StreamerOptions Load()
        {
            using (var s = typeof(StreamerDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.Streamer.json"))
            using (var r = new System.IO.StreamReader(s)) return JsonConvert.DeserializeObject<StreamerOptions>(r.ReadToEnd());
        }
    }
    public static class StreamerText
    {
        public static bool ChannelValid(string value) => value != null && value.Length >= 3 && value.Length <= 20 && value.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_');
        public static bool TitleValid(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 60 && value.All(c => !char.IsControl(c) && c != '~');
    }
    public sealed class StreamSummary : ExtensibleData
    {
        public string Id, Title, Reason = "Interrompida antes do limite";
        public int Blocks, Seconds, Followers, Subscribers, Bits, Donations, Income, Peak;
        public bool Closed, Completed;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(Id) && Id.Length <= 100 && StreamerText.TitleValid(Title) && Blocks >= 0 && Seconds >= 0 && Followers >= 0 && Subscribers >= 0 && Bits >= 0 && Donations >= 0 && Income >= 0 && Peak >= 0 && Peak <= 60000;
    }
    public sealed class StreamerData : ExtensibleData
    {
        public string Channel = "RPCoreLive", Title = "Passeio por Los Santos";
        public int Followers, Subscribers, Sessions, Completed, Blocks, Peak;
        public long Bits, Donations, Earned, Seconds;
        public StreamSummary Last;
        public List<string> Receipts = new List<string>();
        [JsonIgnore] public bool Valid => StreamerText.ChannelValid(Channel) && StreamerText.TitleValid(Title) && Followers >= 0 && Followers <= 10000000 && Subscribers >= 0 && Subscribers <= 10000000
            && Sessions >= 0 && Completed >= 0 && Blocks >= 0 && Peak >= 0 && Peak <= 60000 && Bits >= 0 && Donations >= 0 && Earned >= 0 && Seconds >= 0 && (Last == null || Last.Valid) && Receipts != null;
    }
    public enum StreamContext { Idle, Walking, Driving, Working, Swimming, Injured, Police, Rain }
    public enum StreamEvent { Normal, Subscription, Bits, Donation }
    public sealed class StreamReward
    {
        public readonly StreamContext Context;
        public readonly StreamEvent Event;
        public readonly int Followers, Viewers, Amount;
        public StreamReward(StreamContext context, StreamEvent kind, int followers, int viewers, int amount) { Context = context; Event = kind; Followers = followers; Viewers = viewers; Amount = amount; }
        public bool Valid(StreamerOptions c) => c != null && c.Valid && Enum.IsDefined(typeof(StreamContext), Context) && Enum.IsDefined(typeof(StreamEvent), Event) && Followers >= 1 && Followers <= 3 && Viewers >= 1 && Viewers <= 60000
            && (Event == StreamEvent.Normal ? Amount == 0 : Event == StreamEvent.Subscription ? Amount == 1 : Event == StreamEvent.Bits ? Amount >= 10 && Amount <= 50 : Amount >= c.DonationMin && Amount <= c.DonationMax);
        public int Revenue(StreamerOptions c) => c.BaseRevenue + (Event == StreamEvent.Subscription ? c.SubscriptionValue : Event == StreamEvent.Bits ? Amount * c.BitsValue : Event == StreamEvent.Donation ? Amount : 0);
    }
    public static class StreamerRules
    {
        public static int Viewers(int followers, StreamContext context, int roll)
        {
            if (followers < 0 || followers > 10000000 || !Enum.IsDefined(typeof(StreamContext), context) || roll < 0 || roll >= 100) throw new ArgumentOutOfRangeException();
            int boost = context == StreamContext.Working ? 5 : context == StreamContext.Police ? 6 : context == StreamContext.Idle ? 0 : 3;
            return Math.Min(60000, 2 + followers / 3 + boost + roll % 5);
        }
        public static StreamReward Create(StreamerOptions c, int followers, StreamContext context, int eventRoll, int followerRoll, int giftRoll, int viewerRoll)
        {
            if (c == null || !c.Valid || eventRoll < 0 || eventRoll >= 100 || followerRoll < 0 || followerRoll >= 100 || giftRoll < 0 || giftRoll > 1000000) throw new ArgumentOutOfRangeException();
            var kind = eventRoll < 10 ? StreamEvent.Subscription : eventRoll < 30 ? StreamEvent.Bits : eventRoll < 50 ? StreamEvent.Donation : StreamEvent.Normal;
            int amount = kind == StreamEvent.Subscription ? 1 : kind == StreamEvent.Bits ? 10 + giftRoll % 41 : kind == StreamEvent.Donation ? c.DonationMin + giftRoll % (c.DonationMax - c.DonationMin + 1) : 0;
            return new StreamReward(context, kind, followerRoll < 70 ? 1 : followerRoll < 95 ? 2 : 3, Viewers(followers, context, viewerRoll), amount);
        }
    }
    public static class StreamerTransactions
    {
        static bool Apply(WalletData w, string receipt, int revenue, Func<StreamerData, bool> mutate, Action save, out bool changed)
        {
            changed = false;
            if (w == null || w.Bank < 0 || revenue < 0 || string.IsNullOrWhiteSpace(receipt) || receipt.Length > 160 || save == null || (w.Streamer != null && !w.Streamer.Valid)) return false;
            if (w.Streamer?.Receipts.Contains(receipt) == true) return true;
            if ((long)w.Bank + revenue > int.MaxValue) return false;
            var old = w.Streamer;
            var next = old == null ? new StreamerData() : JsonConvert.DeserializeObject<StreamerData>(JsonConvert.SerializeObject(old));
            if (!mutate(next) || !next.Valid) return false;
            next.Receipts.Add(receipt); if (next.Receipts.Count > 512) next.Receipts.RemoveRange(0, next.Receipts.Count - 512);
            int bank = w.Bank;
            try { w.Streamer = next; w.Bank = bank + revenue; save(); changed = true; return true; }
            catch { w.Streamer = old; w.Bank = bank; throw; }
        }
        public static bool Settings(WalletData w, string channel, string title, string receipt, Action save, out bool changed)
        {
            changed = false; if (!StreamerText.ChannelValid(channel) || !StreamerText.TitleValid(title)) return false;
            return Apply(w, receipt, 0, d => { d.Channel = channel; d.Title = title; return true; }, save, out changed);
        }
        public static bool Start(WalletData w, string id, Action save, out bool changed)
        {
            changed = false; if (string.IsNullOrWhiteSpace(id) || id.Length > 100) return false;
            return Apply(w, id + ":start", 0, d => {
                if (d.Sessions == int.MaxValue) return false;
                d.Last = new StreamSummary { Id = id, Title = d.Title, Extra = d.Last?.Extra }; d.Sessions++; return true;
            }, save, out changed);
        }
        public static bool Block(WalletData w, StreamerOptions c, StreamReward reward, string id, int step, bool last, Action save, out bool changed)
        {
            changed = false;
            if (c == null || !c.Valid || reward == null || !reward.Valid(c) || string.IsNullOrWhiteSpace(id) || id.Length > 100 || step < 1 || step > c.Blocks || last != (step == c.Blocks)) return false;
            int paid = reward.Revenue(c);
            return Apply(w, id + ":block:" + step, paid, d => {
                var s = d.Last;
                int bits = reward.Event == StreamEvent.Bits ? reward.Amount : 0, donations = reward.Event == StreamEvent.Donation ? reward.Amount : 0, subs = reward.Event == StreamEvent.Subscription ? 1 : 0;
                if (s == null || s.Id != id || s.Closed || step != s.Blocks + 1 || d.Followers > 10000000 - reward.Followers || d.Subscribers > 10000000 - subs || d.Blocks == int.MaxValue || (last && d.Completed == int.MaxValue)
                    || d.Bits > long.MaxValue - bits || d.Donations > long.MaxValue - donations || d.Earned > long.MaxValue - paid || d.Seconds > long.MaxValue - c.TickSeconds
                    || s.Income > int.MaxValue - paid || s.Seconds > int.MaxValue - c.TickSeconds || s.Followers > int.MaxValue - reward.Followers || s.Bits > int.MaxValue - bits || s.Donations > int.MaxValue - donations || s.Subscribers > int.MaxValue - subs) return false;
                d.Followers += reward.Followers; d.Subscribers += subs; d.Blocks++; d.Bits += bits; d.Donations += donations; d.Earned += paid; d.Seconds += c.TickSeconds; d.Peak = Math.Max(d.Peak, reward.Viewers);
                s.Blocks++; s.Seconds += c.TickSeconds; s.Followers += reward.Followers; s.Subscribers += subs; s.Bits += bits; s.Donations += donations; s.Income += paid; s.Peak = Math.Max(s.Peak, reward.Viewers);
                if (last) { d.Completed++; s.Closed = s.Completed = true; s.Reason = "Tempo concluido"; } return true;
            }, save, out changed);
        }
        public static bool Stop(WalletData w, string id, string reason, Action save, out bool changed)
        {
            changed = false; if (string.IsNullOrWhiteSpace(id) || id.Length > 100 || !StreamerText.TitleValid(reason)) return false;
            return Apply(w, id + ":stop", 0, d => { if (d.Last == null || d.Last.Id != id || d.Last.Closed) return false; d.Last.Closed = true; d.Last.Reason = reason; return true; }, save, out changed);
        }
    }
    public enum StreamerStage { Live, Finished, Cancelled }
    public sealed class StreamerRun
    {
        public readonly string Id = Guid.NewGuid().ToString("N");
        public readonly int Total, TickMs;
        public int ElapsedMs { get; private set; }
        public int Blocks { get; private set; }
        public StreamerStage Stage { get; private set; } = StreamerStage.Live;
        public bool Due => Stage == StreamerStage.Live && Blocks < Total && ElapsedMs >= (Blocks + 1) * TickMs;
        public StreamerRun(int durationSeconds, int tickSeconds)
        {
            var c = new StreamerOptions { DurationSeconds = durationSeconds, TickSeconds = tickSeconds };
            if (!c.Valid) throw new ArgumentOutOfRangeException(); Total = c.Blocks; TickMs = tickSeconds * 1000;
        }
        public void Advance(int milliseconds, bool eligible)
        {
            if (Stage != StreamerStage.Live || !eligible || Due) return;
            ElapsedMs = Math.Min(Total * TickMs, ElapsedMs + Math.Max(0, Math.Min(1000, milliseconds)));
        }
        public bool Confirm(Func<int, bool, bool> checkpoint)
        {
            if (!Due || !checkpoint(Blocks + 1, Blocks + 1 == Total)) return false;
            Blocks++; if (Blocks == Total) Stage = StreamerStage.Finished; return true;
        }
        public void Cancel() { if (Stage == StreamerStage.Live) Stage = StreamerStage.Cancelled; }
    }
}
