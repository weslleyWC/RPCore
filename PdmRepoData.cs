using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class PdmRepoOptions : ExtensibleData
    {
        public int DefaultPercent = 20, UnloadSeconds = 5, TimeLimitSeconds = 1800;
        public JobPoint Return;
        public List<JobPoint> Locations = new List<JobPoint>();
        [JsonIgnore] public bool Valid => DefaultPercent >= 0 && DefaultPercent <= 100 && UnloadSeconds >= 2 && UnloadSeconds <= 15 && TimeLimitSeconds >= 300 && TimeLimitSeconds <= 3600 && Return != null && Return.Valid && Locations != null && Locations.Count >= 1 && Locations.Count <= 30 && Locations.All(p => p != null && p.Valid);
    }
    public static class PdmRepoDefaults
    {
        public static PdmRepoOptions Load()
        { using (var s = typeof(PdmRepoDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.PdmRepo.json")) using (var r = new System.IO.StreamReader(s)) return JsonConvert.DeserializeObject<PdmRepoOptions>(r.ReadToEnd()); }
    }
    public sealed class PdmRepoOperation : ExtensibleData
    {
        public string Id, LoanId;
        public int Location, VehicleHandle;
        public PdmEntityRef Truck, Trailer;
        public bool Arrived;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(Id) && Id.Length <= 100 && !string.IsNullOrWhiteSpace(LoanId) && Location >= 0 && Location < 30 && VehicleHandle >= 0 && Truck != null && Truck.Valid && Trailer != null && Trailer.Valid && Truck.Handle != Trailer.Handle && VehicleHandle != Truck.Handle && VehicleHandle != Trailer.Handle && (!Arrived || VehicleHandle > 0);
    }
    public static class PdmRepoTransactions
    {
        public static bool Begin(WalletData w, PdmOptions c, PdmRepoOptions r, PdmRepoOperation operation, Action save, out bool changed)
        {
            changed = false; if (c == null || !c.Valid || r == null || !r.Valid || operation == null || !operation.Valid || operation.Arrived || operation.VehicleHandle != 0 || operation.Location >= r.Locations.Count) return false;
            return PdmTransactions.Apply(w, operation.Id + ":repo", 0, d => {
                var loan = d.Loans.Find(l => l.Id == operation.LoanId);
                if (!d.Owned || d.Active != null || d.Collection != null || d.SafeTransfer != null || d.Repo != null || d.Rental != null || d.ShowroomSale != null || d.PremisesService != null || d.Stock.Count >= c.Capacity || loan == null || !loan.Defaulted || loan.Settled) return false;
                d.Repo = JsonConvert.DeserializeObject<PdmRepoOperation>(JsonConvert.SerializeObject(operation)); return true;
            }, save, out changed);
        }
        public static bool Register(WalletData w, string id, int handle, Action save, out bool changed)
        {
            changed = false; if (handle <= 0) return false;
            return PdmTransactions.Apply(w, id + ":repo-car", 0, d => { var a = d.Repo; if (a == null || a.Id != id || a.Arrived || a.VehicleHandle != 0 || handle == a.Truck.Handle || handle == a.Trailer.Handle) return false; a.VehicleHandle = handle; return true; }, save, out changed);
        }
        public static bool Remember(WalletData w, string id, int engine, int body, string receipt, Action save, out bool changed)
        {
            changed = false; if (engine < 0 || engine > 1000 || body < 0 || body > 1000) return false;
            return PdmTransactions.Apply(w, receipt, 0, d => { var a = d.Repo; if (a == null || a.Id != id) return false; var loan = d.Loans.Find(l => l.Id == a.LoanId); if (loan == null) return false; loan.Asset.Engine = Math.Min(loan.Asset.Engine, engine); loan.Asset.Body = Math.Min(loan.Asset.Body, body); return true; }, save, out changed);
        }
        public static bool Arrive(WalletData w, string id, Action save, out bool changed)
        { return PdmTransactions.Apply(w, id + ":repo-arrived", 0, d => { var a = d.Repo; if (a == null || a.Id != id || a.Arrived || a.VehicleHandle <= 0) return false; a.Arrived = true; return true; }, save, out changed); }
        public static bool Complete(WalletData w, PdmOptions c, string id, Action save, out bool changed)
        {
            changed = false; if (c == null || !c.Valid) return false;
            return PdmTransactions.Apply(w, id + ":repo-received", 0, d => {
                var a = d.Repo; if (a == null || a.Id != id || !a.Arrived || d.Stock.Count >= c.Capacity) return false;
                var loan = d.Loans.Find(l => l.Id == a.LoanId); if (loan == null || !loan.Defaulted || loan.Settled || d.Stock.Any(s => s.Id == loan.Asset.Id || s.Plate == loan.Asset.Plate) || d.SoldCost < loan.Asset.Cost || d.FinanceWriteOff > long.MaxValue - (loan.Total - loan.Paid)) return false;
                d.Stock.Add(JsonConvert.DeserializeObject<PdmStock>(JsonConvert.SerializeObject(loan.Asset)));
                d.SoldCost -= loan.Asset.Cost; // The recovered original cost belongs to inventory again, not to sold goods twice.
                d.FinanceWriteOff += loan.Total - loan.Paid; loan.Repossessed = true; loan.Elapsed = 0; d.Repo = null;
                PdmTransactions.Entry(d, "Retomada / estoque: " + loan.Asset.Name, 0); return true;
            }, save, out changed);
        }
        public static bool Cancel(WalletData w, string id, Action save, out bool changed)
        { return PdmTransactions.Apply(w, id + ":repo-cancel", 0, d => { if (d.Repo == null || d.Repo.Id != id || d.Repo.Arrived) return false; d.Repo = null; return true; }, save, out changed); }
    }
}
