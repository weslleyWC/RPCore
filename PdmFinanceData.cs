using System;
using System.Linq;
using Newtonsoft.Json;

namespace RPCore
{
    public sealed class PdmFinanceOptions : ExtensibleData
    {
        public int DownPercent = 25, InterestPercent = 10, Installments = 4, PeriodSeconds = 120, MaxActive = 6;
        [JsonIgnore] public bool Valid => DownPercent >= 10 && DownPercent <= 80 && InterestPercent >= 0 && InterestPercent <= 30 && Installments >= 2 && Installments <= 12 && PeriodSeconds >= 60 && PeriodSeconds <= 3600 && MaxActive >= 1 && MaxActive <= 10;
    }
    public static class PdmFinanceDefaults
    {
        public static PdmFinanceOptions Load()
        {
            using (var s = typeof(PdmFinanceDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.PdmFinance.json"))
            using (var r = new System.IO.StreamReader(s)) return JsonConvert.DeserializeObject<PdmFinanceOptions>(r.ReadToEnd());
        }
    }
    public sealed class PdmLoan : ExtensibleData
    {
        public string Id;
        public PdmStock Asset;
        public int Customer, Total, DownPayment, Paid, Installments, Payments, PeriodSeconds, Elapsed, InterestPercent, DownPercent;
        [JsonIgnore] public bool Complete => Paid == Total;
        [JsonIgnore] public int Payment => Payments == Installments - 1 ? Total - Paid : (Total - DownPayment) / Installments;
        [JsonIgnore] public int Profit => Math.Max(0, Paid - (Asset?.Cost ?? Paid));
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(Id) && Id.Length <= 100 && Asset != null && Asset.Valid && !Asset.AwaitingCollection && Customer >= 0 && Customer < 30
            && Total >= 1 && Total <= 4000000 && DownPayment >= 1 && DownPayment < Total && Paid >= DownPayment && Paid <= Total && Installments >= 2 && Installments <= 12 && Total - DownPayment >= Installments
            && Payments >= 0 && Payments <= Installments && PeriodSeconds >= 60 && PeriodSeconds <= 3600 && Elapsed >= 0 && Elapsed <= PeriodSeconds && InterestPercent >= 0 && InterestPercent <= 30 && DownPercent >= 10 && DownPercent <= 80
            && (Complete ? Payments == Installments && Elapsed == 0 : Payments < Installments && Paid == DownPayment + Payments * ((Total - DownPayment) / Installments));
    }
    public static class PdmFinanceTransactions
    {
        public static int Quote(PdmData d, PdmFinanceOptions c, PdmStock stock, int engine, int body)
        { if (c == null || !c.Valid) return 0; return (int)((long)PdmStaffTransactions.Quote(d, stock, engine, body) * (100 + c.InterestPercent) / 100); }
        public static int Entry(PdmFinanceOptions c, int total) => c == null || !c.Valid || total < 1 ? 0 : (int)((long)total * c.DownPercent / 100);
        public static bool Open(WalletData w, PdmFinanceOptions c, string id, int engine, int body, Action save, out bool changed)
        {
            changed = false;
            if (c == null || !c.Valid || string.IsNullOrWhiteSpace(id) || id.Length > 100 || engine < 100 || engine > 1000 || body < 0 || body > 1000) return false;
            return PdmTransactions.Apply(w, id + ":finance", 0, d => {
                var a = d.Active; if (!d.Owned || a == null || a.Id != id || !a.Financed || d.Loans.Any(l => l.Id == id) || d.Loans.Count(l => !l.Complete) >= c.MaxActive) return false;
                var stock = d.Stock.Find(s => s.Id == a.StockId);
                int total = Quote(d, c, stock, engine, body), entry = Entry(c, total);
                if (stock == null || stock.AwaitingCollection || entry < 1 || total - entry < c.Installments || d.Cash > int.MaxValue - entry || d.Revenue > long.MaxValue - entry || d.SoldCost > long.MaxValue - stock.Cost || d.Sold == int.MaxValue) return false;
                while (d.Loans.Count >= 42) { var old = d.Loans.Find(l => l.Complete); if (old == null) return false; d.Loans.Remove(old); }
                var asset = JsonConvert.DeserializeObject<PdmStock>(JsonConvert.SerializeObject(stock)); asset.Engine = Math.Min(asset.Engine, engine); asset.Body = Math.Min(asset.Body, body);
                d.Loans.Add(new PdmLoan { Id = id, Asset = asset, Customer = a.Destination, Total = total, DownPayment = entry, Paid = entry, Installments = c.Installments, PeriodSeconds = c.PeriodSeconds, InterestPercent = c.InterestPercent, DownPercent = c.DownPercent });
                d.Cash += entry; d.Revenue += entry; d.SoldCost += stock.Cost; d.Sold++; d.Stock.Remove(stock); d.Active = null;
                PdmTransactions.Entry(d, "Entrada financiamento: " + stock.Name, entry); return true;
            }, save, out changed);
        }
        public static bool Work(WalletData w, int seconds, string receipt, Action save, out bool changed)
        {
            changed = false; if (seconds < 1 || seconds > 60) return false;
            return PdmTransactions.Apply(w, receipt, 0, d => {
                if (!d.Owned || !d.Loans.Any(l => !l.Complete)) return false;
                foreach (var loan in d.Loans.Where(l => !l.Complete))
                {
                    loan.Elapsed = Math.Min(loan.PeriodSeconds, loan.Elapsed + seconds); if (loan.Elapsed < loan.PeriodSeconds) continue;
                    int payment = loan.Payment; long headroom = int.MaxValue - (long)d.Cash - (d.SafeTransfer?.Amount ?? 0);
                    if (payment < 1 || headroom < payment || d.Revenue > long.MaxValue - payment) continue;
                    d.Cash += payment; d.Revenue += payment; loan.Paid += payment; loan.Payments++; loan.Elapsed = 0;
                    PdmTransactions.Entry(d, "Parcela " + loan.Payments + "/" + loan.Installments + ": " + loan.Asset.Name, payment);
                }
                return true;
            }, save, out changed);
        }
    }
}
