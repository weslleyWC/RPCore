using System;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class PdmPremisesOptions : ExtensibleData
    {
        public JobPoint Mechanic, Cleaner;
        public string MechanicModel = "s_m_y_xmech_01", CleanerModel = "s_f_m_maid_01";
        public PdmStaffOffer CleanerOffer = new PdmStaffOffer { Id = "cleaner", Name = "Zeladoria", HiringCost = 650, Wage = 130 };
        public int RepairSeconds = 8, CleanSeconds = 6, TimeoutSeconds = 120, DirtIntervalSeconds = 60;
        public bool EnableDirt = true;
        [JsonIgnore] public bool Valid => Mechanic != null && Mechanic.Valid && Cleaner != null && Cleaner.Valid && !string.IsNullOrWhiteSpace(MechanicModel) && MechanicModel.Length <= 60 && !string.IsNullOrWhiteSpace(CleanerModel) && CleanerModel.Length <= 60 && CleanerOffer != null && CleanerOffer.Valid && CleanerOffer.Id == "cleaner" && CleanerOffer.Requires == null && CleanerOffer.SaleBonusPercent == 0
            && RepairSeconds >= 3 && RepairSeconds <= 30 && CleanSeconds >= 3 && CleanSeconds <= 30 && TimeoutSeconds >= 60 && TimeoutSeconds <= 300 && DirtIntervalSeconds >= 60 && DirtIntervalSeconds <= 3600;
    }
    public static class PdmPremisesDefaults
    {
        public static PdmPremisesOptions Load()
        { using (var s = typeof(PdmPremisesDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.PdmPremises.json")) using (var r = new System.IO.StreamReader(s)) return JsonConvert.DeserializeObject<PdmPremisesOptions>(r.ReadToEnd()); }
        public static PdmStaffOptions Merge(PdmStaffOptions staff, PdmPremisesOptions premises)
        {
            if (staff == null || !staff.Valid || premises == null || !premises.Valid) throw new ArgumentException();
            var copy = JsonConvert.DeserializeObject<PdmStaffOptions>(JsonConvert.SerializeObject(staff));
            if (!copy.Roles.Any(r => r.Id == "cleaner")) copy.Roles.Add(JsonConvert.DeserializeObject<PdmStaffOffer>(JsonConvert.SerializeObject(premises.CleanerOffer)));
            return copy;
        }
    }
    public sealed class PdmPremisesService : ExtensibleData
    {
        public string Id, StockId, Kind, EmployeeId;
        public int Handle, Price;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(Id) && Id.Length <= 100 && !string.IsNullOrWhiteSpace(StockId) && (Kind == "repair" || Kind == "clean") && !string.IsNullOrWhiteSpace(EmployeeId) && Handle > 0 && Price >= 0 && Price <= 100000 && (Kind != "clean" || Price == 0);
    }
    public static class PdmPremisesTransactions
    {
        public static bool Working(PdmData d, string role) => d.Owned && d.Staff.FormerDebt == 0 && d.Staff.Employees.Any(e => e.Role == role && e.Owed == 0);
        public static bool Idle(PdmData d) => d.Owned && d.Active == null && d.Collection == null && d.Repo == null && d.SafeTransfer == null && d.Rental == null && d.ShowroomSale == null && d.PremisesService == null;
        public static bool Begin(WalletData w, PdmOptions c, string stockId, string kind, string id, Action save, out bool changed)
        {
            changed = false; if (c == null || !c.Valid || string.IsNullOrWhiteSpace(id) || id.Length > 100 || kind != "repair" && kind != "clean") return false;
            return PdmTransactions.Apply(w, id + ":service", 0, d => {
                string role = kind == "repair" ? "mechanic" : "cleaner"; var employee = d.Staff.Employees.Find(e => e.Role == role); var stock = d.Stock.Find(s => s.Id == stockId); var entry = d.Showroom.Find(e => e.StockId == stockId);
                if (!Idle(d) || !Working(d, role) || employee == null || stock == null || entry == null || entry.Handle <= 0 || (kind == "repair" ? stock.Engine == 1000 && stock.Body == 1000 : stock.Dirt == 0)) return false;
                int price = kind == "repair" ? PdmStaffTransactions.RepairPrice(d, c.RepairPrice) : 0; if (d.Cash < price) return false;
                d.PremisesService = new PdmPremisesService { Id = id, StockId = stockId, Kind = kind, EmployeeId = employee.Id, Handle = entry.Handle, Price = price }; return true;
            }, save, out changed);
        }
        public static bool Complete(WalletData w, string id, Action save, out bool changed)
        {
            return PdmTransactions.Apply(w, id + ":service-complete", 0, d => {
                var a = d.PremisesService; if (a == null || a.Id != id || d.Cash < a.Price || d.Repairs > long.MaxValue - a.Price) return false;
                string role = a.Kind == "repair" ? "mechanic" : "cleaner"; if (!Working(d, role) || !d.Staff.Employees.Any(e => e.Id == a.EmployeeId && e.Role == role)) return false;
                var stock = d.Stock.Find(s => s.Id == a.StockId); if (stock == null) return false;
                d.Cash -= a.Price; d.Repairs += a.Price;
                if (a.Kind == "repair") stock.Engine = stock.Body = 1000; else stock.Dirt = 0;
                PdmTransactions.Entry(d, a.Kind == "repair" ? "Oficina presencial: " + stock.Name : "Zeladoria: " + stock.Name, -a.Price); d.PremisesService = null; return true;
            }, save, out changed);
        }
        public static bool Cancel(WalletData w, string id, Action save, out bool changed)
        { return PdmTransactions.Apply(w, id + ":service-cancel", 0, d => { if (d.PremisesService == null || d.PremisesService.Id != id) return false; d.PremisesService = null; return true; }, save, out changed); }
        public static bool Work(WalletData w, PdmPremisesOptions c, int seconds, string receipt, Action save, out bool changed)
        {
            changed = false; if (c == null || !c.Valid || !c.EnableDirt || seconds < 1 || seconds > 60) return false;
            return PdmTransactions.Apply(w, receipt, 0, d => {
                if (!d.Owned || d.Showroom.Count == 0) return false;
                int elapsed = Math.Min(d.DirtSeconds, c.DirtIntervalSeconds - 1) + seconds, steps = elapsed / c.DirtIntervalSeconds; d.DirtSeconds = elapsed % c.DirtIntervalSeconds;
                if (steps > 0) foreach (var entry in d.Showroom) { if (d.PremisesService?.StockId == entry.StockId || d.ShowroomSale?.StockId == entry.StockId) continue; var stock = d.Stock.Find(s => s.Id == entry.StockId); stock.Dirt = Math.Min(15, stock.Dirt + steps); }
                return true;
            }, save, out changed);
        }
    }
    public enum PdmServiceStage { Approaching, Working, Complete, Cancelled }
    public sealed class PdmServiceRun
    {
        public readonly string Id, StockId, Kind;
        public PdmServiceStage Stage { get; private set; } = PdmServiceStage.Approaching;
        readonly int duration; int began;
        public PdmServiceRun(string id, string stockId, string kind, int seconds) { if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(stockId) || kind != "repair" && kind != "clean" || seconds < 3 || seconds > 30) throw new ArgumentException(); Id = id; StockId = stockId; Kind = kind; duration = seconds * 1000; }
        public bool Start(int now, bool eligible) { if (Stage != PdmServiceStage.Approaching || !eligible) return false; began = now; Stage = PdmServiceStage.Working; return true; }
        public bool Commit(int now, bool eligible, Func<bool> save) { if (Stage != PdmServiceStage.Working) return false; if (!eligible) { Stage = PdmServiceStage.Approaching; return false; } if (unchecked(now - began) < duration || !save()) return false; Stage = PdmServiceStage.Complete; return true; }
        public bool Cancel(Func<bool> save) { if (Stage == PdmServiceStage.Complete || Stage == PdmServiceStage.Cancelled || !save()) return false; Stage = PdmServiceStage.Cancelled; return true; }
    }
}
