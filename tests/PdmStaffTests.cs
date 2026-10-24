using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class PdmStaffTests
    {
        static readonly Action Save = () => { };
        static PdmStaffOptions C => PdmStaffDefaults.Load();
        static WalletData Company()
        {
            var w = new WalletData { Bank = 100000, Cash = 75 };
            Assert.True(PdmTransactions.Purchase(w, PdmDefaults.Load(), "company", Save, out _));
            Assert.True(PdmTransactions.Transfer(w, 50000, true, "capital", Save, out _)); return w;
        }
        static void Hire(WalletData w, string role) => Assert.True(PdmStaffTransactions.Hire(w, C, role, role + "-employee", Save, out _));
        static void Work(WalletData w, int seconds)
        { while (seconds > 0) { int n = Math.Min(60, seconds); Assert.True(PdmStaffTransactions.Work(w, n, Guid.NewGuid().ToString("N"), Save, out _)); seconds -= n; } }
        static void Stock(WalletData w) => Assert.True(PdmTransactions.Buy(w, PdmDefaults.Load(), "asea", "unit123", "buy", Save, out _));
        [Fact] public void DefaultsHaveFourRolesHierarchyAndFiveMinutePeriods()
        { Assert.True(C.Valid); Assert.Equal(4, C.Roles.Count); Assert.Equal(300, C.PeriodSeconds); Assert.Equal("junior", C.Roles.Single(r => r.Id == "senior").Requires); Assert.Equal("senior", C.Roles.Single(r => r.Id == "director").Requires); }
        [Theory] [InlineData(0)] [InlineData(59)] [InlineData(3601)]
        public void InvalidPeriodRejected(int n)
        { var c = C; c.PeriodSeconds = n; Assert.False(c.Valid); }
        [Fact] public void InvalidRoleHierarchyOrDuplicateRejected()
        { var c = C; c.Roles[2].Requires = null; Assert.False(c.Valid); c = C; c.Roles[0].Id = "junior"; Assert.False(c.Valid); }
        [Fact] public void OldCompanyWithoutStaffFieldPreservesCashStockAndActiveDelivery()
        { var w = Company(); Stock(w); PdmTransactions.Begin(w, PdmDefaults.Load(), "unit123", "delivery", 0, 99, Save, out _); var j = JObject.Parse(JsonConvert.SerializeObject(w)); j["Pdm"]["Staff"].Parent.Remove(); w = j.ToObject<WalletData>(); Assert.True(w.Pdm.Valid); Assert.Empty(w.Pdm.Staff.Employees); Assert.Equal(42000, w.Pdm.Cash); Assert.Equal("delivery", w.Pdm.Active.Id); Assert.Equal(11200, PdmStaffTransactions.Quote(w.Pdm, w.Pdm.Stock[0], 1000, 1000)); }
        [Fact] public void HireDebitsCompanyOnlyAndSnapshotsTerms()
        { var w = Company(); Hire(w, "mechanic"); var e = Assert.Single(w.Pdm.Staff.Employees); Assert.Equal(48500, w.Pdm.Cash); Assert.Equal(25000, w.Bank); Assert.Equal(75, w.Cash); Assert.Equal(250, e.Wage); Assert.Equal(300, e.PeriodSeconds); Assert.Equal(1500, w.Pdm.Staff.HiringSpent); Assert.Equal(0, e.Owed); }
        [Fact] public void CannotHireTwiceOrWithoutOwnershipFundsOrRequiredRole()
        { var w = Company(); Assert.False(PdmStaffTransactions.Hire(w, C, "senior", "s", Save, out _)); Assert.False(PdmStaffTransactions.Hire(w, C, "director", "d", Save, out _)); Hire(w, "junior"); Assert.False(PdmStaffTransactions.Hire(w, C, "junior", "again", Save, out _)); w.Pdm.Cash = 0; Assert.False(PdmStaffTransactions.Hire(w, C, "mechanic", "m", Save, out _)); w.Pdm.Owned = false; Assert.False(PdmStaffTransactions.Hire(w, C, "mechanic", "m", Save, out _)); }
        [Fact] public void FailedHireSaveRestoresWalletAndReplayCannotChargeAgain()
        { var w = Company(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmStaffTransactions.Hire(w, C, "mechanic", "m", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); Assert.True(PdmStaffTransactions.Hire(w, C, "mechanic", "m", Save, out _)); Assert.True(PdmStaffTransactions.Hire(w, C, "mechanic", "m", () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Equal(48500, w.Pdm.Cash); }
        [Fact] public void PayrollAccruesAtExactBoundaryWithoutDebitingMoney()
        { var w = Company(); Hire(w, "junior"); int cash = w.Pdm.Cash; Work(w, 299); Assert.Equal(0, w.Pdm.Staff.Employees[0].Owed); Work(w, 1); Assert.Equal(100, w.Pdm.Staff.Employees[0].Owed); Assert.Equal(0, w.Pdm.Staff.Employees[0].Seconds); Assert.Equal(cash, w.Pdm.Cash); Assert.Equal(0, PdmStaffTransactions.BonusPercent(w.Pdm)); }
        [Fact] public void NewEmployeeDoesNotInheritAnotherEmployeesWorkTime()
        { var w = Company(); Hire(w, "junior"); Work(w, 250); Hire(w, "mechanic"); Work(w, 50); Assert.Equal(100, w.Pdm.Staff.Employees.Single(e => e.Role == "junior").Owed); var m = w.Pdm.Staff.Employees.Single(e => e.Role == "mechanic"); Assert.Equal(50, m.Seconds); Assert.Equal(0, m.Owed); }
        [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(61)]
        public void InvalidTimeCannotAccrue(int seconds)
        { var w = Company(); Hire(w, "mechanic"); Assert.False(PdmStaffTransactions.Work(w, seconds, "work", Save, out _)); Assert.Equal(0, w.Pdm.Staff.Employees[0].Seconds); }
        [Fact] public void FailedPayrollSaveRetainsTimeAndDebtSnapshot()
        { var w = Company(); Hire(w, "junior"); Work(w, 299); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmStaffTransactions.Work(w, 1, "work", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); Assert.True(PdmStaffTransactions.Work(w, 1, "work", Save, out _)); w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); Assert.True(PdmStaffTransactions.Work(w, 1, "work", () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Equal(100, w.Pdm.Staff.Employees[0].Owed); }
        [Fact] public void PayrollDebtOverflowRejectsWholeCheckpoint()
        { var w = Company(); Hire(w, "junior"); Hire(w, "mechanic"); Work(w, 299); w.Pdm.Staff.Employees[1].Owed = int.MaxValue; string before = JsonConvert.SerializeObject(w); Assert.False(PdmStaffTransactions.Work(w, 1, "overflow", Save, out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void WageSnapshotSurvivesConfigurationChangeAndReload()
        { var w = Company(); Hire(w, "junior"); var c = C; c.Roles[1].Wage = 999; c.PeriodSeconds = 60; w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); Work(w, 300); Assert.Equal(100, w.Pdm.Staff.Employees[0].Owed); Assert.Equal(300, w.Pdm.Staff.Employees[0].PeriodSeconds); }
        [Fact] public void PayExactAvailableCashWorksAndReactivatesBenefit()
        { var w = Company(); Hire(w, "junior"); Work(w, 300); w.Pdm.Cash = 100; Assert.True(PdmStaffTransactions.Pay(w, "junior", "pay", Save, out _)); Assert.Equal(0, w.Pdm.Cash); Assert.Equal(100, w.Pdm.Staff.Paid); Assert.Equal(0, w.Pdm.Staff.Employees[0].Owed); Assert.Equal(2, PdmStaffTransactions.BonusPercent(w.Pdm)); Assert.False(PdmStaffTransactions.Pay(w, "junior", "again", Save, out _)); Assert.Equal(25000, w.Bank); }
        [Fact] public void InsufficientCashAndFailedPayDoNotEraseDebt()
        { var w = Company(); Hire(w, "junior"); Work(w, 300); w.Pdm.Cash = 99; Assert.False(PdmStaffTransactions.Pay(w, "junior", "p", Save, out _)); w.Pdm.Cash = 100; string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmStaffTransactions.Pay(w, "junior", "p", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void PaidSalaryReplayAfterReloadCannotDebitAgain()
        { var w = Company(); Hire(w, "junior"); Work(w, 300); Assert.True(PdmStaffTransactions.Pay(w, "junior", "p", Save, out _)); w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); string before = JsonConvert.SerializeObject(w); Assert.True(PdmStaffTransactions.Pay(w, "junior", "p", () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void FireKeepsOverduePlusProportionalWageAndSeverance()
        { var w = Company(); Hire(w, "junior"); Work(w, 450); int cash = w.Pdm.Cash; Assert.True(PdmStaffTransactions.Fire(w, "junior-employee", "fire", Save, out _)); Assert.Empty(w.Pdm.Staff.Employees); Assert.Equal(400, w.Pdm.Staff.FormerDebt); Assert.Equal(cash, w.Pdm.Cash); Assert.False(PdmStaffTransactions.Hire(w, C, "junior", "new", Save, out _)); Assert.True(PdmStaffTransactions.Pay(w, "former", "settlement", Save, out _)); Assert.Equal(0, w.Pdm.Staff.FormerDebt); Assert.Equal(cash - 400, w.Pdm.Cash); Assert.True(PdmStaffTransactions.Hire(w, C, "junior", "new", Save, out _)); }
        [Fact] public void FireProportionalRoundsUpAndCannotEraseDebtByRehiring()
        { var w = Company(); Hire(w, "junior"); Work(w, 1); Assert.True(PdmStaffTransactions.Fire(w, "junior-employee", "fire", Save, out _)); Assert.Equal(251, w.Pdm.Staff.FormerDebt); Assert.False(PdmStaffTransactions.Hire(w, C, "junior", "new", Save, out _)); }
        [Fact] public void HierarchyRequiresFiringDirectorThenSeniorThenJunior()
        { var w = Company(); Hire(w, "junior"); Hire(w, "senior"); Hire(w, "director"); Assert.False(PdmStaffTransactions.Fire(w, "junior-employee", "j", Save, out _)); Assert.False(PdmStaffTransactions.Fire(w, "senior-employee", "s", Save, out _)); Assert.True(PdmStaffTransactions.Fire(w, "director-employee", "d", Save, out _)); Assert.True(PdmStaffTransactions.Fire(w, "senior-employee", "s", Save, out _)); Assert.True(PdmStaffTransactions.Fire(w, "junior-employee", "j", Save, out _)); Assert.Equal(750, w.Pdm.Staff.FormerDebt); }
        [Fact] public void FailedFireSaveAndOverflowPreserveEmploymentAndDebt()
        { var w = Company(); Hire(w, "mechanic"); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmStaffTransactions.Fire(w, "mechanic-employee", "fire", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); w.Pdm.Staff.FormerDebt = int.MaxValue; Assert.False(PdmStaffTransactions.Fire(w, "mechanic-employee", "fire", Save, out _)); Assert.Single(w.Pdm.Staff.Employees); }
        [Theory] [InlineData("workshop",5000)] [InlineData("advertising",3000)]
        public void UpgradeCostsCompanyOnceAndSurvivesReload(string upgrade, int price)
        { var w = Company(); Assert.True(PdmStaffTransactions.Upgrade(w, C, upgrade, "upgrade", Save, out _)); Assert.Equal(50000 - price, w.Pdm.Cash); Assert.Equal(price, w.Pdm.Staff.UpgradeSpent); w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); Assert.False(PdmStaffTransactions.Upgrade(w, C, upgrade, "new", Save, out _)); Assert.True(PdmStaffTransactions.Upgrade(w, C, upgrade, "upgrade", () => throw new Exception(), out bool changed)); Assert.False(changed); }
        [Fact] public void FailedUpgradeSaveRestoresFundsAndFlag()
        { var w = Company(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmStaffTransactions.Upgrade(w, C, "workshop", "u", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); Assert.False(PdmStaffTransactions.Upgrade(w, C, "unknown", "u", Save, out _)); }
        [Fact] public void MechanicDiscountRequiresWorkshopAndPaidStaff()
        { var w = Company(); Assert.Equal(1000, PdmStaffTransactions.RepairPrice(w.Pdm, 1000)); Hire(w, "mechanic"); Assert.Equal(1000, PdmStaffTransactions.RepairPrice(w.Pdm, 1000)); PdmStaffTransactions.Upgrade(w, C, "workshop", "u", Save, out _); Assert.Equal(500, PdmStaffTransactions.RepairPrice(w.Pdm, 1000)); Work(w, 300); Assert.Equal(1000, PdmStaffTransactions.RepairPrice(w.Pdm, 1000)); PdmStaffTransactions.Pay(w, "mechanic", "p", Save, out _); Assert.Equal(500, PdmStaffTransactions.RepairPrice(w.Pdm, 1000)); }
        [Fact] public void ExistingRepairUsesRealDiscountWithoutChangingPersonalAssets()
        { var w = Company(); Stock(w); Hire(w, "mechanic"); PdmStaffTransactions.Upgrade(w, C, "workshop", "u", Save, out _); w.Pdm.Stock[0].Body = 300; int cash = w.Pdm.Cash; Assert.True(PdmTransactions.Repair(w, PdmDefaults.Load(), "unit123", "repair", Save, out _)); Assert.Equal(cash - 500, w.Pdm.Cash); Assert.Equal(500, w.Pdm.Repairs); Assert.Equal(1000, w.Pdm.Stock[0].Body); Assert.Equal(25000, w.Bank); }
        [Fact] public void MarketingUsesHighestPaidLevelAndAdvertisingNeedsEmployee()
        { var w = Company(); PdmStaffTransactions.Upgrade(w, C, "advertising", "u", Save, out _); Assert.Equal(0, PdmStaffTransactions.BonusPercent(w.Pdm)); Hire(w, "junior"); Assert.Equal(4, PdmStaffTransactions.BonusPercent(w.Pdm)); Hire(w, "senior"); Assert.Equal(6, PdmStaffTransactions.BonusPercent(w.Pdm)); Hire(w, "director"); Assert.Equal(8, PdmStaffTransactions.BonusPercent(w.Pdm)); Work(w, 300); Assert.Equal(0, PdmStaffTransactions.BonusPercent(w.Pdm)); PdmStaffTransactions.Pay(w, "junior", "p", Save, out _); Assert.Equal(4, PdmStaffTransactions.BonusPercent(w.Pdm)); }
        [Fact] public void SaleCreditsActualBonusOnceAndRemovesOnlyDeliveredUnit()
        { var w = Company(); Stock(w); Hire(w, "junior"); Assert.Equal(11424, PdmStaffTransactions.Quote(w.Pdm, w.Pdm.Stock[0], 1000, 1000)); Assert.True(PdmTransactions.Begin(w, PdmDefaults.Load(), "unit123", "sale", 0, 99, Save, out _)); int cash = w.Pdm.Cash; Assert.True(PdmTransactions.Sell(w, "sale", 1000, 1000, Save, out _)); Assert.Equal(cash + 11424, w.Pdm.Cash); Assert.Equal(11424, w.Pdm.Revenue); Assert.Empty(w.Pdm.Stock); Assert.True(PdmTransactions.Sell(w, "sale", 1000, 1000, () => throw new Exception(), out bool changed)); Assert.False(changed); }
        [Fact] public void DamageDiscountComposesWithMarketingAndSaleFailureRollsBack()
        { var w = Company(); Stock(w); Hire(w, "junior"); Assert.Equal(9996, PdmStaffTransactions.Quote(w.Pdm, w.Pdm.Stock[0], 500, 500)); PdmTransactions.Begin(w, PdmDefaults.Load(), "unit123", "sale", 0, 99, Save, out _); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmTransactions.Sell(w, "sale", 500, 500, () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void StaffManagementBlockedDuringDeliveryButPayrollKeepsAccruing()
        { var w = Company(); Hire(w, "junior"); Stock(w); PdmTransactions.Begin(w, PdmDefaults.Load(), "unit123", "sale", 0, 99, Save, out _); Work(w, 300); Assert.Equal(100, w.Pdm.Staff.Employees[0].Owed); Assert.False(PdmStaffTransactions.Pay(w, "junior", "p", Save, out _)); Assert.False(PdmStaffTransactions.Fire(w, "junior-employee", "f", Save, out _)); Assert.False(PdmStaffTransactions.Upgrade(w, C, "workshop", "u", Save, out _)); Assert.False(PdmStaffTransactions.Hire(w, C, "mechanic", "m", Save, out _)); Assert.Equal("sale", w.Pdm.Active.Id); }
        [Fact] public void FormerDebtSuspendsAllBenefitsAndUnpaidStaffBlocksHiring()
        { var w = Company(); Hire(w, "junior"); Hire(w, "mechanic"); PdmStaffTransactions.Upgrade(w, C, "workshop", "u", Save, out _); PdmStaffTransactions.Fire(w, "junior-employee", "f", Save, out _); Assert.Equal(1000, PdmStaffTransactions.RepairPrice(w.Pdm, 1000)); Assert.False(PdmStaffTransactions.Hire(w, C, "junior", "new", Save, out _)); }
        [Fact] public void CollectionBlocksStaffMutationsWithoutErasingCargoOrPayroll()
        {
            var w = Company(); Hire(w, "junior"); Stock(w); w.Pdm.Stock[0].AwaitingCollection = true;
            Assert.True(PdmCollectionTransactions.Begin(w, "haul", new System.Collections.Generic.List<string> { "unit123" }, new PdmEntityRef { Handle = 11, Model = "packer", Plate = "TRUCK" }, new PdmEntityRef { Handle = 12, Model = "trflat", Plate = "TRAILER" }, Save, out _));
            Work(w, 300);
            Assert.False(PdmStaffTransactions.Pay(w, "junior", "p", Save, out _)); Assert.False(PdmStaffTransactions.Fire(w, "junior-employee", "f", Save, out _)); Assert.False(PdmStaffTransactions.Hire(w, C, "mechanic", "m", Save, out _)); Assert.False(PdmStaffTransactions.Upgrade(w, C, "workshop", "u", Save, out _));
            Assert.Equal("haul", w.Pdm.Collection.Id); Assert.True(w.Pdm.Stock[0].AwaitingCollection); Assert.Equal(100, w.Pdm.Staff.Employees[0].Owed);
        }
        [Fact] public void FormerDebtPaymentFailureCannotEraseLiabilityOrChargeCash()
        {
            var w = Company(); Hire(w, "junior"); PdmStaffTransactions.Fire(w, "junior-employee", "fire", Save, out _); string before = JsonConvert.SerializeObject(w);
            Assert.Throws<Exception>(() => PdmStaffTransactions.Pay(w, "former", "p", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w));
            Assert.True(PdmStaffTransactions.Pay(w, "former", "p", Save, out _)); Assert.True(PdmStaffTransactions.Pay(w, "former", "p", () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Equal(250, w.Pdm.Staff.Paid); Assert.Equal(0, w.Pdm.Staff.FormerDebt);
        }
        [Fact] public void UnknownFieldsOfCompanyEmployeeAndStaffSurvivePayroll()
        { var w = Company(); Hire(w, "junior"); var j = JObject.Parse(JsonConvert.SerializeObject(w)); j["Future"] = "keep"; j["Pdm"]["Future"] = 1; j["Pdm"]["Staff"]["Future"] = 2; j["Pdm"]["Staff"]["Employees"][0]["Future"] = 3; w = j.ToObject<WalletData>(); Work(w, 60); j = JObject.Parse(JsonConvert.SerializeObject(w)); Assert.Equal("keep", (string)j["Future"]); Assert.Equal(1, (int)j["Pdm"]["Future"]); Assert.Equal(2, (int)j["Pdm"]["Staff"]["Future"]); Assert.Equal(3, (int)j["Pdm"]["Staff"]["Employees"][0]["Future"]); }
        [Fact] public void InvalidStaffStateIsPreservedAndCannotMutateWallet()
        { var w = Company(); Hire(w, "junior"); w.Pdm.Staff.Employees[0].Seconds = 300; Assert.False(w.Pdm.Valid); string before = JsonConvert.SerializeObject(w); Assert.False(PdmStaffTransactions.Pay(w, "junior", "p", Save, out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
    }
}
