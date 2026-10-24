using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RPCore
{
    public sealed class PdmStaffOffer
    {
        public string Id, Name, Requires;
        public int HiringCost, Wage, SaleBonusPercent;
        public bool Valid => new[] { "mechanic", "junior", "senior", "director", "cleaner" }.Contains(Id) && !string.IsNullOrWhiteSpace(Name) && HiringCost >= 0 && HiringCost <= 100000 && Wage > 0 && Wage <= 100000 && SaleBonusPercent >= 0 && SaleBonusPercent <= 10 && (Id != "cleaner" || SaleBonusPercent == 0);
    }
    public sealed class PdmStaffOptions : ExtensibleData
    {
        public int PeriodSeconds = 300, WorkshopCost = 5000, AdvertisingCost = 3000, Severance = 250;
        public List<PdmStaffOffer> Roles = new List<PdmStaffOffer>();
        [JsonIgnore] public bool Valid => PeriodSeconds >= 60 && PeriodSeconds <= 3600 && WorkshopCost > 0 && WorkshopCost <= 1000000 && AdvertisingCost > 0 && AdvertisingCost <= 1000000 && Severance >= 0 && Severance <= 100000
            && Roles != null && Roles.Count >= 4 && Roles.Count <= 5 && Roles.All(r => r != null && r.Valid) && Roles.Select(r => r.Id).Distinct().Count() == Roles.Count && new[] { "mechanic", "junior", "senior", "director" }.All(id => Roles.Any(r => r.Id == id))
            && Roles.All(r => r.Requires == (r.Id == "senior" ? "junior" : r.Id == "director" ? "senior" : null));
    }
    public static class PdmStaffDefaults
    {
        public static PdmStaffOptions Load()
        {
            using (var s = typeof(PdmStaffDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.PdmStaff.json"))
            using (var r = new System.IO.StreamReader(s)) return JsonConvert.DeserializeObject<PdmStaffOptions>(r.ReadToEnd());
        }
    }
    public sealed class PdmEmployee : ExtensibleData
    {
        public string Id, Role, Name;
        public int Wage, PeriodSeconds, Seconds, Owed, BonusPercent, Severance;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(Id) && new[] { "mechanic", "junior", "senior", "director", "cleaner" }.Contains(Role) && !string.IsNullOrWhiteSpace(Name)
            && Wage > 0 && Wage <= 100000 && PeriodSeconds >= 60 && PeriodSeconds <= 3600 && Seconds >= 0 && Seconds < PeriodSeconds && Owed >= 0 && BonusPercent >= 0 && BonusPercent <= 10 && Severance >= 0 && Severance <= 100000 && (Role != "cleaner" || BonusPercent == 0);
    }
    public sealed class PdmStaffData : ExtensibleData
    {
        public List<PdmEmployee> Employees = new List<PdmEmployee>();
        public int FormerDebt;
        public long Paid, HiringSpent, UpgradeSpent;
        public bool Workshop, Advertising;
        [JsonIgnore] public bool Valid => Employees != null && Employees.Count <= 5 && Employees.All(e => e != null && e.Valid) && Employees.Select(e => e.Role).Distinct().Count() == Employees.Count && Employees.Select(e => e.Id).Distinct().Count() == Employees.Count
            && FormerDebt >= 0 && Paid >= 0 && HiringSpent >= 0 && UpgradeSpent >= 0;
    }
    public static class PdmStaffTransactions
    {
        static bool Idle(PdmData d) => d.Owned && d.Active == null && d.Collection == null && d.SafeTransfer == null && d.Repo == null && d.Rental == null && d.ShowroomSale == null && d.PremisesService == null;
        static bool Working(PdmData d, string role) => d?.Staff != null && d.Staff.FormerDebt == 0 && d.Staff.Employees.Any(e => e.Role == role && e.Owed == 0);
        public static int RepairPrice(PdmData d, int price) => d?.Staff?.Workshop == true && Working(d, "mechanic") ? Math.Max(1, price / 2) : price;
        public static int BonusPercent(PdmData d)
        {
            if (d?.Staff == null || d.Staff.FormerDebt > 0) return 0;
            int n = d.Staff.Employees.Where(e => e.Role != "mechanic" && e.Owed == 0).Select(e => e.BonusPercent).DefaultIfEmpty(0).Max();
            return n > 0 ? n + (d.Staff.Advertising ? 2 : 0) : 0;
        }
        public static int Quote(PdmData d, PdmStock v, int engine, int body) => (int)((long)PdmTransactions.Quote(v, engine, body) * (100 + BonusPercent(d)) / 100);
        public static bool Hire(WalletData w, PdmStaffOptions c, string role, string id, Action save, out bool changed)
        {
            changed = false; if (c == null || !c.Valid || string.IsNullOrWhiteSpace(id) || id.Length > 100) return false;
            var offer = c.Roles.Find(r => r.Id == role); if (offer == null) return false;
            return PdmTransactions.Apply(w, id + ":hire", 0, d => {
                var s = d.Staff;
                if (!Idle(d) || s.FormerDebt > 0 || s.Employees.Any(e => e.Role == role || e.Owed > 0) || (offer.Requires != null && !Working(d, offer.Requires)) || d.Cash < offer.HiringCost || s.HiringSpent > long.MaxValue - offer.HiringCost) return false;
                s.Employees.Add(new PdmEmployee { Id = id, Role = role, Name = offer.Name, Wage = offer.Wage, PeriodSeconds = c.PeriodSeconds, BonusPercent = offer.SaleBonusPercent, Severance = c.Severance });
                d.Cash -= offer.HiringCost; s.HiringSpent += offer.HiringCost; PdmTransactions.Entry(d, "Contratacao: " + offer.Name, -offer.HiringCost); return true;
            }, save, out changed);
        }
        public static bool Work(WalletData w, int seconds, string receipt, Action save, out bool changed)
        {
            changed = false; if (seconds < 1 || seconds > 60) return false;
            return PdmTransactions.Apply(w, receipt, 0, d => {
                if (!d.Owned || d.Staff.Employees.Count == 0) return false;
                foreach (var e in d.Staff.Employees)
                {
                    int total = e.Seconds + seconds; long owed = (long)e.Owed + (long)(total / e.PeriodSeconds) * e.Wage;
                    if (owed > int.MaxValue) return false; e.Owed = (int)owed; e.Seconds = total % e.PeriodSeconds;
                }
                return true;
            }, save, out changed);
        }
        public static bool Pay(WalletData w, string role, string receipt, Action save, out bool changed)
        {
            return PdmTransactions.Apply(w, receipt, 0, d => {
                if (!Idle(d)) return false; var s = d.Staff; var e = s.Employees.Find(x => x.Role == role);
                int amount = role == "former" ? s.FormerDebt : e?.Owed ?? 0;
                if (amount <= 0 || d.Cash < amount || s.Paid > long.MaxValue - amount) return false;
                d.Cash -= amount; s.Paid += amount; if (role == "former") s.FormerDebt = 0; else e.Owed = 0;
                PdmTransactions.Entry(d, role == "former" ? "Acerto de ex-funcionarios" : "Salario: " + e.Name, -amount); return true;
            }, save, out changed);
        }
        public static bool Fire(WalletData w, string employeeId, string receipt, Action save, out bool changed)
        {
            return PdmTransactions.Apply(w, receipt, 0, d => {
                if (!Idle(d)) return false; var s = d.Staff; var e = s.Employees.Find(x => x.Id == employeeId); if (e == null) return false;
                if (e.Role == "junior" && s.Employees.Any(x => x.Role == "senior" || x.Role == "director") || e.Role == "senior" && s.Employees.Any(x => x.Role == "director")) return false;
                long amount = (long)s.FormerDebt + e.Owed + ((long)e.Seconds * e.Wage + e.PeriodSeconds - 1) / e.PeriodSeconds + e.Severance;
                if (amount > int.MaxValue) return false; s.FormerDebt = (int)amount; s.Employees.Remove(e);
                PdmTransactions.Entry(d, "Demissao / acerto pendente: " + e.Name, 0); return true;
            }, save, out changed);
        }
        public static bool Upgrade(WalletData w, PdmStaffOptions c, string upgrade, string receipt, Action save, out bool changed)
        {
            changed = false; if (c == null || !c.Valid || upgrade != "workshop" && upgrade != "advertising") return false;
            return PdmTransactions.Apply(w, receipt, 0, d => {
                var s = d.Staff; int amount = upgrade == "workshop" ? c.WorkshopCost : c.AdvertisingCost;
                if (!Idle(d) || (upgrade == "workshop" ? s.Workshop : s.Advertising) || d.Cash < amount || s.UpgradeSpent > long.MaxValue - amount) return false;
                d.Cash -= amount; s.UpgradeSpent += amount; if (upgrade == "workshop") s.Workshop = true; else s.Advertising = true;
                PdmTransactions.Entry(d, "Melhoria: " + upgrade, -amount); return true;
            }, save, out changed);
        }
    }
}
