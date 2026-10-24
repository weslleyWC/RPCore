using System;
using Newtonsoft.Json;

namespace RPCore
{
    public sealed class PdmSafeOptions : ExtensibleData
    {
        public JobPoint Spawn, Parking, Handover, Bank;
        public string VehicleModel = "stockade", CollectorModel = "s_m_m_armoured_01", CaseModel = "prop_security_case_01";
        public int HandoverSeconds = 3, ClearingSeconds = 10, ArrivalTimeoutSeconds = 120;
        [JsonIgnore] public bool Valid => Spawn != null && Spawn.Valid && Parking != null && Parking.Valid && Handover != null && Handover.Valid && Bank != null && Bank.Valid
            && Model(VehicleModel) && Model(CollectorModel) && Model(CaseModel) && HandoverSeconds >= 2 && HandoverSeconds <= 10 && ClearingSeconds >= 5 && ClearingSeconds <= 60 && ArrivalTimeoutSeconds >= 60 && ArrivalTimeoutSeconds <= 300;
        static bool Model(string n) => !string.IsNullOrWhiteSpace(n) && n.Length <= 60;
    }
    public static class PdmSafeDefaults
    {
        public static PdmSafeOptions Load()
        {
            using (var s = typeof(PdmSafeDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.PdmSafe.json"))
            using (var r = new System.IO.StreamReader(s)) return JsonConvert.DeserializeObject<PdmSafeOptions>(r.ReadToEnd());
        }
    }
    public sealed class PdmSafeTransfer : ExtensibleData
    {
        public string Id, CollectorModel, CaseModel;
        public int Amount, CollectorHandle, Token, CaseHandle;
        public PdmEntityRef Van;
        public bool Handed;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(Id) && Id.Length <= 100 && Amount >= 1 && Amount <= 1000000 && CollectorHandle > 0 && Token != 0 && !string.IsNullOrWhiteSpace(CollectorModel) && CollectorModel.Length <= 60 && CaseHandle > 0 && !string.IsNullOrWhiteSpace(CaseModel) && CaseModel.Length <= 60 && Van != null && Van.Valid && CollectorHandle != Van.Handle && CaseHandle != Van.Handle && CollectorHandle != CaseHandle;
    }
    public static class PdmSafeTransactions
    {
        static bool Id(string id) => !string.IsNullOrWhiteSpace(id) && id.Length <= 100;
        public static bool Begin(WalletData w, PdmSafeTransfer transfer, Action save, out bool changed)
        {
            changed = false; if (transfer == null || !transfer.Valid || transfer.Handed) return false;
            return PdmTransactions.Apply(w, transfer.Id + ":safe-reserve", 0, d => {
                if (!d.Owned || d.Active != null || d.Collection != null || d.SafeTransfer != null || d.Cash < transfer.Amount) return false;
                d.Cash -= transfer.Amount;
                // Do not retain references owned by the caller after a checkpoint.
                d.SafeTransfer = JsonConvert.DeserializeObject<PdmSafeTransfer>(JsonConvert.SerializeObject(transfer));
                PdmTransactions.Entry(d, "Coleta de valores / reservado", -transfer.Amount); return true;
            }, save, out changed);
        }
        public static bool Hand(WalletData w, string id, Action save, out bool changed)
        {
            changed = false; if (!Id(id)) return false;
            return PdmTransactions.Apply(w, id + ":safe-hand", 0, d => { if (d.SafeTransfer == null || d.SafeTransfer.Id != id || d.SafeTransfer.Handed) return false; d.SafeTransfer.Handed = true; return true; }, save, out changed);
        }
        public static bool Refund(WalletData w, string id, Action save, out bool changed)
        {
            changed = false; if (!Id(id)) return false;
            return PdmTransactions.Apply(w, id + ":safe-refund", 0, d => {
                var t = d.SafeTransfer; if (t == null || t.Id != id || t.Handed || d.Cash > int.MaxValue - t.Amount) return false;
                d.Cash += t.Amount; d.SafeTransfer = null; PdmTransactions.Entry(d, "Coleta interrompida / devolucao", t.Amount); return true;
            }, save, out changed);
        }
        public static bool Credit(WalletData w, string id, Action save, out bool changed)
        {
            changed = false; if (!Id(id)) return false;
            int amount = w?.Pdm?.SafeTransfer?.Amount ?? 0;
            return PdmTransactions.Apply(w, id + ":safe-bank", amount, d => {
                var t = d.SafeTransfer; if (t == null || t.Id != id || !t.Handed || d.Withdrawn > long.MaxValue - t.Amount) return false;
                d.Withdrawn += t.Amount; d.SafeTransfer = null; PdmTransactions.Entry(d, "Coleta de valores / banco pessoal", 0); return true;
            }, save, out changed);
        }
    }
    public enum PdmSafeStage { Arriving, Walking, Handover, Boarding, Clearing, Complete, Cancelled }
    public sealed class PdmSafeRun
    {
        public readonly string Id;
        public PdmSafeStage Stage { get; private set; } = PdmSafeStage.Arriving;
        readonly int handDuration, clearDuration; int began;
        public PdmSafeRun(string id, int handSeconds, int clearingSeconds)
        { if (string.IsNullOrWhiteSpace(id) || handSeconds < 2 || handSeconds > 10 || clearingSeconds < 5 || clearingSeconds > 60) throw new ArgumentException(); Id = id; handDuration = handSeconds * 1000; clearDuration = clearingSeconds * 1000; }
        public bool Arrive(bool stopped) { if (Stage != PdmSafeStage.Arriving || !stopped) return false; Stage = PdmSafeStage.Walking; return true; }
        public bool Handover(int now, bool eligible) { if (Stage != PdmSafeStage.Walking || !eligible) return false; began = now; Stage = PdmSafeStage.Handover; return true; }
        public bool Hand(int now, bool eligible, Func<bool> save)
        {
            if (Stage != PdmSafeStage.Handover) return false;
            if (!eligible) { Stage = PdmSafeStage.Walking; return false; }
            if (unchecked(now - began) < handDuration || !save()) return false; Stage = PdmSafeStage.Boarding; return true;
        }
        public bool Depart(int now, bool departing) { if (Stage != PdmSafeStage.Boarding || !departing) return false; began = now; Stage = PdmSafeStage.Clearing; return true; }
        public bool Credit(int now, bool active, Func<bool> save)
        { if (Stage != PdmSafeStage.Clearing) return false; if (!active) { began = now; return false; } if (unchecked(now - began) < clearDuration || !save()) return false; Stage = PdmSafeStage.Complete; return true; }
        public bool Cancel(Func<bool> refund)
        { if (Stage == PdmSafeStage.Boarding || Stage == PdmSafeStage.Clearing || Stage == PdmSafeStage.Complete || Stage == PdmSafeStage.Cancelled || !refund()) return false; Stage = PdmSafeStage.Cancelled; return true; }
    }
}
