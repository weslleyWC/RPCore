using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class PdmInsuranceOffer : ExtensibleData
    {
        public string Id, Name;
        public int Premium, Limit;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(Id) && Id.Length <= 40 && !string.IsNullOrWhiteSpace(Name) && Premium > 0 && Premium <= 100000 && Limit > 0 && Limit <= 200000000;
    }
    public sealed class PdmRiskOptions : ExtensibleData
    {
        public int PeriodSeconds = 1800, ExposureSeconds = 120, RiskPercent = 15, LossPercent = 20, MinimumCash = 50000;
        public List<PdmInsuranceOffer> Plans = new List<PdmInsuranceOffer>();
        [JsonIgnore] public bool Valid => PeriodSeconds >= 300 && PeriodSeconds <= 7200 && ExposureSeconds >= 60 && ExposureSeconds <= 3600 && RiskPercent >= 0 && RiskPercent <= 100 && LossPercent >= 1 && LossPercent <= 50 && MinimumCash >= 0 && MinimumCash <= 1000000 && Plans != null && Plans.Count >= 1 && Plans.Count <= 10 && Plans.All(p => p != null && p.Valid) && Plans.Select(p => p.Id).Distinct().Count() == Plans.Count;
    }
    public static class PdmRiskDefaults
    {
        public static PdmRiskOptions Load()
        { using (var s = typeof(PdmRiskDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.PdmRisk.json")) using (var r = new System.IO.StreamReader(s)) return JsonConvert.DeserializeObject<PdmRiskOptions>(r.ReadToEnd()); }
    }
    public sealed class PdmInsurancePolicy : ExtensibleData
    {
        public string PlanId, Name;
        public int Premium, Limit, PeriodSeconds, Seconds;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(PlanId) && !string.IsNullOrWhiteSpace(Name) && Premium > 0 && Premium <= 100000 && Limit > 0 && Limit <= 200000000 && PeriodSeconds >= 300 && PeriodSeconds <= 7200 && Seconds >= 0 && Seconds < PeriodSeconds;
    }
    public sealed class PdmRiskIncident : ExtensibleData
    {
        public string Id, PolicyName;
        public int Loss, Reimbursed, Limit, Security;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(Id) && Id.Length <= 160 && Loss > 0 && Reimbursed >= 0 && (Reimbursed == 0 || Reimbursed == Loss && Limit >= Loss) && Limit >= 0 && Limit <= 200000000 && Security >= 0 && Security <= 100;
    }
    public sealed class PdmRiskData : ExtensibleData
    {
        public bool SafeOpen;
        public int ExposedSeconds;
        public long Checks, Premiums, Stolen, Reimbursed;
        public PdmInsurancePolicy Policy;
        public List<PdmRiskIncident> Incidents = new List<PdmRiskIncident>();
        [JsonIgnore] public bool Valid => ExposedSeconds >= 0 && ExposedSeconds < 3600 && (SafeOpen || ExposedSeconds == 0) && Checks >= 0 && Premiums >= 0 && Stolen >= 0 && Reimbursed >= 0 && Reimbursed <= Stolen && (Policy == null || Policy.Valid) && Incidents != null && Incidents.Count <= 20 && Incidents.All(i => i != null && i.Valid) && Incidents.Select(i => i.Id).Distinct().Count() == Incidents.Count;
    }
    public static class PdmRiskTransactions
    {
        public static int Security(PdmData d) => (d.Surveillance.Purchased ? 30 : 0) + (PdmSecurityPolicy.Paid(d, "security1") ? 35 : 0) + (PdmSecurityPolicy.Paid(d, "security2") ? 35 : 0);
        public static int Chance(PdmData d, PdmRiskOptions c) => (c.RiskPercent * (100 - Security(d)) + 99) / 100;
        public static int Roll(string receipt) { uint h = 2166136261; foreach (char ch in receipt) h = unchecked((h ^ ch) * 16777619); return (int)(h % 100); }
        public static bool SetSafe(WalletData w, bool open, string receipt, Action save, out bool changed)
        { return PdmTransactions.Apply(w, receipt, 0, d => { if (!PdmShowroomTransactions.Idle(d) || d.Risk.SafeOpen == open) return false; d.Risk.SafeOpen = open; d.Risk.ExposedSeconds = 0; PdmTransactions.Entry(d, open ? "Cofre administrativo aberto / risco habilitado" : "Cofre administrativo fechado", 0); return true; }, save, out changed); }
        public static bool ReturnToPremises(WalletData w, string receipt, Action save, out bool changed)
        { return PdmTransactions.Apply(w, receipt, 0, d => { if (!d.Owned || d.Risk.ExposedSeconds == 0) return false; d.Risk.ExposedSeconds = 0; return true; }, save, out changed); }
        public static bool Insure(WalletData w, PdmRiskOptions c, string plan, string receipt, Action save, out bool changed)
        {
            changed = false; if (c == null || !c.Valid) return false; var offer = c.Plans.Find(p => p.Id == plan); if (offer == null) return false;
            return PdmTransactions.Apply(w, receipt, 0, d => {
                if (!PdmShowroomTransactions.Idle(d) || d.Risk.Policy?.PlanId == plan || d.Cash < offer.Premium || d.Risk.Premiums > long.MaxValue - offer.Premium) return false;
                d.Cash -= offer.Premium; d.Risk.Premiums += offer.Premium;
                d.Risk.Policy = new PdmInsurancePolicy { PlanId = offer.Id, Name = offer.Name, Premium = offer.Premium, Limit = offer.Limit, PeriodSeconds = c.PeriodSeconds };
                PdmTransactions.Entry(d, "Seguro contratado: " + offer.Name, -offer.Premium); return true;
            }, save, out changed);
        }
        public static bool Cancel(WalletData w, string receipt, Action save, out bool changed)
        { return PdmTransactions.Apply(w, receipt, 0, d => { if (!PdmShowroomTransactions.Idle(d) || d.Risk.Policy == null) return false; d.Risk.Policy = null; PdmTransactions.Entry(d, "Seguro cancelado / sem estorno", 0); return true; }, save, out changed); }
        public static bool Work(WalletData w, PdmRiskOptions c, int seconds, bool away, string receipt, Action save, out bool changed)
        {
            changed = false; if (c == null || !c.Valid || seconds < 1 || seconds > 60) return false;
            return PdmTransactions.Apply(w, receipt, 0, d => {
                if (!d.Owned || !d.Risk.SafeOpen && d.Risk.Policy == null) return false;
                var r = d.Risk; var policy = r.Policy;
                if (policy != null)
                {
                    int elapsed = policy.Seconds + seconds;
                    if (elapsed >= policy.PeriodSeconds)
                    {
                        if (d.Cash >= policy.Premium && r.Premiums <= long.MaxValue - policy.Premium)
                        { d.Cash -= policy.Premium; r.Premiums += policy.Premium; policy.Seconds = elapsed % policy.PeriodSeconds; PdmTransactions.Entry(d, "Renovacao do seguro: " + policy.Name, -policy.Premium); }
                        else { r.Policy = null; PdmTransactions.Entry(d, "Seguro encerrado / caixa insuficiente ou limite contabil", 0); }
                    }
                    else policy.Seconds = elapsed;
                }
                if (!r.SafeOpen || !away || !PdmShowroomTransactions.Idle(d) || d.Cash <= c.MinimumCash) { r.ExposedSeconds = 0; return true; }
                int exposure = Math.Min(r.ExposedSeconds, c.ExposureSeconds - 1) + seconds;
                if (exposure < c.ExposureSeconds) { r.ExposedSeconds = exposure; return true; }
                r.ExposedSeconds = exposure % c.ExposureSeconds;
                if (r.Checks == long.MaxValue) { r.SafeOpen = false; r.ExposedSeconds = 0; PdmTransactions.Entry(d, "Risco suspenso / limite contabil", 0); return true; }
                int security = Security(d), chance = (c.RiskPercent * (100 - security) + 99) / 100; r.Checks++;
                if (Roll(receipt) >= chance) return true;
                int loss = (int)((long)d.Cash * c.LossPercent / 100); if (loss <= 0) return true;
                int reimbursement = r.Policy != null && r.Policy.Limit >= loss ? loss : 0;
                if (r.Stolen > long.MaxValue - loss || r.Reimbursed > long.MaxValue - reimbursement) { r.SafeOpen = false; r.ExposedSeconds = 0; PdmTransactions.Entry(d, "Risco suspenso / limite contabil", 0); return true; }
                // Debit and insured reimbursement share the same checkpoint: never pay without a real loss.
                d.Cash -= loss; r.Stolen += loss; PdmTransactions.Entry(d, "Ocorrencia: cofre aberto e empresa desassistida", -loss);
                if (reimbursement > 0) { d.Cash += reimbursement; r.Reimbursed += reimbursement; PdmTransactions.Entry(d, "Indenizacao: " + r.Policy.Name, reimbursement); }
                r.Incidents.Add(new PdmRiskIncident { Id = receipt, Loss = loss, Reimbursed = reimbursement, Limit = r.Policy?.Limit ?? 0, PolicyName = r.Policy?.Name, Security = security });
                if (r.Incidents.Count > 20) r.Incidents.RemoveAt(0);
                r.SafeOpen = false; r.ExposedSeconds = 0; return true;
            }, save, out changed);
        }
    }
}
