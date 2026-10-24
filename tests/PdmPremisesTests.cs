using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class PdmPremisesTests
    {
        static readonly Action Save = () => { };
        static PdmPremisesOptions C => PdmPremisesDefaults.Load();
        static PdmStaffOptions Staff => PdmPremisesDefaults.Merge(PdmStaffDefaults.Load(), C);
        static WalletData Stock()
        { var w = new WalletData { Bank = 100000, Cash = 55 }; PdmTransactions.Purchase(w, PdmDefaults.Load(), "company", Save, out _); PdmTransactions.Transfer(w, 50000, true, "capital", Save, out _); PdmTransactions.Buy(w, PdmDefaults.Load(), "asea", "AA111stock", "buy", Save, out _); PdmShowroomTransactions.Assign(w, PdmShowroomDefaults.Load(), "AA111stock", "lot1", 111, "assign", Save, out _); return w; }
        static void Hire(WalletData w, string role) => Assert.True(PdmStaffTransactions.Hire(w, Staff, role, role + "-employee", Save, out _));
        static WalletData Dirty()
        { var w = Stock(); Hire(w, "cleaner"); w.Pdm.Stock[0].Dirt = 5; return w; }
        static WalletData Cleaning()
        { var w = Dirty(); Assert.True(PdmPremisesTransactions.Begin(w, PdmDefaults.Load(), "AA111stock", "clean", "service", Save, out _)); return w; }
        static WalletData Repairing(bool workshop = false)
        { var w = Stock(); Hire(w, "mechanic"); if (workshop) PdmStaffTransactions.Upgrade(w, Staff, "workshop", "upgrade", Save, out _); w.Pdm.Stock[0].Engine = 500; w.Pdm.Stock[0].Body = 400; Assert.True(PdmPremisesTransactions.Begin(w, PdmDefaults.Load(), "AA111stock", "repair", "service", Save, out _)); return w; }
        [Fact] public void DefaultsContainReferenceMechanicMaidAndPhysicalServiceTimes()
        { var c = C; Assert.True(c.Valid); Assert.Equal(-37.51725f, c.Mechanic.X); Assert.Equal(-43.24272f, c.Cleaner.X); Assert.Equal("s_m_y_xmech_01", c.MechanicModel); Assert.Equal("s_f_m_maid_01", c.CleanerModel); Assert.Equal(8, c.RepairSeconds); Assert.Equal(6, c.CleanSeconds); Assert.Equal(60, c.DirtIntervalSeconds); }
        [Fact] public void MergeAddsCleanerWithoutMutatingExistingFourRoleConfiguration()
        { var old = PdmStaffDefaults.Load(); string before = JsonConvert.SerializeObject(old); var merged = PdmPremisesDefaults.Merge(old, C); Assert.True(merged.Valid); Assert.Equal(5, merged.Roles.Count); Assert.Equal(before, JsonConvert.SerializeObject(old)); Assert.Equal(4, old.Roles.Count); Assert.Equal(130, merged.Roles.Single(r => r.Id == "cleaner").Wage); var again = PdmPremisesDefaults.Merge(merged, C); Assert.Equal(5, again.Roles.Count); }
        [Fact] public void ExistingCleanerCustomizationIsNotReplacedByMerge()
        { var staff = Staff; staff.Roles.Single(r => r.Id == "cleaner").Wage = 200; Assert.Equal(200, PdmPremisesDefaults.Merge(staff, C).Roles.Single(r => r.Id == "cleaner").Wage); }
        [Fact] public void LegacyStockAndCompanyMissingNewFieldsRemainCleanAndUncharged()
        { var w = Stock(); var j = JObject.Parse(JsonConvert.SerializeObject(w)); j["Pdm"]["DirtSeconds"].Parent.Remove(); j["Pdm"]["PremisesService"].Parent.Remove(); j["Pdm"]["Stock"][0]["Dirt"].Parent.Remove(); w = j.ToObject<WalletData>(); Assert.True(w.Pdm.Valid); Assert.Equal(0, w.Pdm.Stock[0].Dirt); Assert.Null(w.Pdm.PremisesService); Assert.Equal(11200, PdmTransactions.Quote(w.Pdm.Stock[0], 1000, 1000)); Assert.Equal(42000, w.Pdm.Cash); }
        [Fact] public void CleanerHireUsesCompanyCashAndExistingPayrollAndSeverance()
        { var w = Stock(); Hire(w, "cleaner"); Assert.Equal(41350, w.Pdm.Cash); Assert.Equal(25000, w.Bank); var employee = Assert.Single(w.Pdm.Staff.Employees); Assert.Equal(300, employee.PeriodSeconds); for (int i = 0; i < 5; i++) PdmStaffTransactions.Work(w, 60, "time" + i, Save, out _); Assert.Equal(130, w.Pdm.Staff.Employees[0].Owed); Assert.False(PdmPremisesTransactions.Working(w.Pdm, "cleaner")); Assert.True(PdmStaffTransactions.Pay(w, "cleaner", "pay", Save, out _)); Assert.True(PdmPremisesTransactions.Working(w.Pdm, "cleaner")); Assert.True(PdmStaffTransactions.Fire(w, employee.Id, "fire", Save, out _)); Assert.Equal(250, w.Pdm.Staff.FormerDebt); }
        [Fact] public void DirtAccruesAtMinuteBoundaryOnlyForExposedStockAndDoesNotMoveMoney()
        { var w = Stock(); PdmTransactions.Buy(w, PdmDefaults.Load(), "asea", "BB222stock", "second", Save, out _); int cash = w.Pdm.Cash; Assert.True(PdmPremisesTransactions.Work(w, C, 59, "first", Save, out _)); Assert.Equal(0, w.Pdm.Stock[0].Dirt); Assert.True(PdmPremisesTransactions.Work(w, C, 1, "due", Save, out _)); Assert.Equal(1, w.Pdm.Stock[0].Dirt); Assert.Equal(0, w.Pdm.Stock[1].Dirt); Assert.Equal(0, w.Pdm.DirtSeconds); Assert.Equal(cash, w.Pdm.Cash); Assert.Equal(11144, PdmTransactions.Quote(w.Pdm.Stock[0], 1000, 1000)); }
        [Fact] public void DirtAndPricePenaltyAreBoundedAndComposeWithSavedDamage()
        { var w = Stock(); for (int i = 0; i < 30; i++) PdmPremisesTransactions.Work(w, C, 60, "step" + i, Save, out _); Assert.Equal(15, w.Pdm.Stock[0].Dirt); Assert.Equal(10360, PdmTransactions.Quote(w.Pdm.Stock[0], 1000, 1000)); Assert.Equal(9065, PdmTransactions.Quote(w.Pdm.Stock[0], 500, 500)); }
        [Theory] [InlineData(-1)] [InlineData(0)] [InlineData(61)]
        public void InvalidElapsedTimeCannotAdvanceDirt(int seconds)
        { var w = Stock(); Assert.False(PdmPremisesTransactions.Work(w, C, seconds, "bad", Save, out _)); Assert.Equal(0, w.Pdm.DirtSeconds); }
        [Fact] public void DisabledDirtLeavesStockUntouched()
        { var w = Stock(); var c = C; c.EnableDirt = false; Assert.False(PdmPremisesTransactions.Work(w, c, 60, "disabled", Save, out _)); Assert.Equal(0, w.Pdm.Stock[0].Dirt); }
        [Fact] public void FailedDirtSaveAndReplayCannotDuplicateProgress()
        { var w = Stock(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmPremisesTransactions.Work(w, C, 60, "due", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); Assert.True(PdmPremisesTransactions.Work(w, C, 60, "due", Save, out _)); w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); Assert.True(PdmPremisesTransactions.Work(w, C, 60, "due", () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Equal(1, w.Pdm.Stock[0].Dirt); }
        [Fact] public void ChangingIntervalDoesNotRetroactivelyAddDozensOfDirtLevels()
        { var w = Stock(); w.Pdm.DirtSeconds = 3500; Assert.True(PdmPremisesTransactions.Work(w, C, 1, "interval", Save, out _)); Assert.Equal(1, w.Pdm.Stock[0].Dirt); }
        [Fact] public void ServiceRequiresHiredPaidRolePhysicalAssignmentAndActualNeed()
        { var w = Stock(); w.Pdm.Stock[0].Dirt = 2; Assert.False(PdmPremisesTransactions.Begin(w, PdmDefaults.Load(), "AA111stock", "clean", "service", Save, out _)); Hire(w, "cleaner"); w.Pdm.Stock[0].Dirt = 0; Assert.False(PdmPremisesTransactions.Begin(w, PdmDefaults.Load(), "AA111stock", "clean", "service", Save, out _)); w.Pdm.Stock[0].Dirt = 2; PdmShowroomTransactions.Track(w, "AA111stock", 0, "far", Save, out _); Assert.False(PdmPremisesTransactions.Begin(w, PdmDefaults.Load(), "AA111stock", "clean", "service", Save, out _)); }
        [Fact] public void BeginningServiceReservesIntentWithoutChargingOrChangingCondition()
        { var w = Cleaning(); Assert.Equal(41350, w.Pdm.Cash); Assert.Equal(5, w.Pdm.Stock[0].Dirt); Assert.Equal(0, w.Pdm.PremisesService.Price); Assert.Equal(111, w.Pdm.PremisesService.Handle); Assert.Single(w.Pdm.Showroom); Assert.Equal("cleaner-employee", w.Pdm.PremisesService.EmployeeId); }
        [Fact] public void FailedBeginRestoresNoIntentAndReplayCannotStartSecondService()
        { var w = Dirty(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmPremisesTransactions.Begin(w, PdmDefaults.Load(), "AA111stock", "clean", "service", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); PdmPremisesTransactions.Begin(w, PdmDefaults.Load(), "AA111stock", "clean", "service", Save, out _); Assert.True(PdmPremisesTransactions.Begin(w, PdmDefaults.Load(), "AA111stock", "clean", "service", () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.False(PdmPremisesTransactions.Begin(w, PdmDefaults.Load(), "AA111stock", "clean", "other", Save, out _)); }
        [Fact] public void CleaningRestoresValueWithoutFeeAndKeepsSameStockAndSlot()
        { var w = Cleaning(); int cash = w.Pdm.Cash; Assert.Equal(10920, PdmTransactions.Quote(w.Pdm.Stock[0], 1000, 1000)); Assert.True(PdmPremisesTransactions.Complete(w, "service", Save, out _)); Assert.Null(w.Pdm.PremisesService); Assert.Equal(0, w.Pdm.Stock[0].Dirt); Assert.Equal(11200, PdmTransactions.Quote(w.Pdm.Stock[0], 1000, 1000)); Assert.Equal(cash, w.Pdm.Cash); Assert.Equal(0, w.Pdm.Repairs); Assert.Single(w.Pdm.Showroom); Assert.Equal(111, w.Pdm.Showroom[0].Handle); Assert.Equal(0, w.Pdm.Sold); }
        [Theory] [InlineData(false,1000)] [InlineData(true,500)]
        public void RepairChargesRealQuotedCostOnceAndKeepsVitrineAssignment(bool workshop, int cost)
        { var w = Repairing(workshop); int cash = w.Pdm.Cash; Assert.Equal(cost, w.Pdm.PremisesService.Price); Assert.True(PdmPremisesTransactions.Complete(w, "service", Save, out _)); Assert.Equal(cash - cost, w.Pdm.Cash); Assert.Equal(cost, w.Pdm.Repairs); Assert.Equal(1000, w.Pdm.Stock[0].Engine); Assert.Equal(1000, w.Pdm.Stock[0].Body); Assert.Single(w.Pdm.Showroom); Assert.Equal(25000, w.Bank); }
        [Fact] public void FailedCompletionRestoresQuoteCashAndStockForRetry()
        { var w = Repairing(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmPremisesTransactions.Complete(w, "service", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void CompletionReplayAfterReloadDoesNotChargeAgain()
        { var w = Repairing(); PdmPremisesTransactions.Complete(w, "service", Save, out _); w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); string before = JsonConvert.SerializeObject(w); Assert.True(PdmPremisesTransactions.Complete(w, "service", () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void WagesDueDuringServiceSuspendCompletionUntilCancelAndPay()
        { var w = Cleaning(); for (int i = 0; i < 5; i++) PdmStaffTransactions.Work(w, 60, "time" + i, Save, out _); Assert.False(PdmPremisesTransactions.Complete(w, "service", Save, out _)); Assert.Equal(5, w.Pdm.Stock[0].Dirt); Assert.True(PdmPremisesTransactions.Cancel(w, "service", Save, out _)); Assert.True(PdmStaffTransactions.Pay(w, "cleaner", "pay", Save, out _)); Assert.Equal(0, w.Pdm.Staff.Employees[0].Owed); }
        [Fact] public void CancelOrReloadBeforeCompletionDoesNotPartiallyRepairOrCharge()
        { var w = Repairing(); int cash = w.Pdm.Cash; w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); Assert.True(PdmPremisesTransactions.Cancel(w, "service", Save, out _)); Assert.Equal(500, w.Pdm.Stock[0].Engine); Assert.Equal(400, w.Pdm.Stock[0].Body); Assert.Equal(cash, w.Pdm.Cash); Assert.Equal(0, w.Pdm.Repairs); Assert.Single(w.Pdm.Showroom); Assert.False(PdmPremisesTransactions.Complete(w, "service", Save, out _)); }
        [Fact] public void ActiveServiceBlocksHandleReplacementRemovalCustomerAndOtherContracts()
        { var w = Cleaning(); Assert.False(PdmShowroomTransactions.Track(w, "AA111stock", 222, "track", Save, out _)); Assert.False(PdmShowroomTransactions.Remove(w, "AA111stock", "remove", Save, out _)); Assert.False(PdmShowroomTransactions.BeginSale(w, "AA111stock", "buyer", Save, out _)); Assert.False(PdmTransactions.Buy(w, PdmDefaults.Load(), "asea", "BB222stock", "buy2", Save, out _)); Assert.False(PdmStaffTransactions.Fire(w, "cleaner-employee", "fire", Save, out _)); }
        [Fact] public void DirtClockSkipsUnitUnderServiceOrAcceptedBuyerOffer()
        { var w = Cleaning(); Assert.True(PdmPremisesTransactions.Work(w, C, 60, "dirt", Save, out _)); Assert.Equal(5, w.Pdm.Stock[0].Dirt); PdmPremisesTransactions.Cancel(w, "service", Save, out _); PdmShowroomTransactions.BeginSale(w, "AA111stock", "buyer", Save, out _); PdmPremisesTransactions.Work(w, C, 60, "dirt2", Save, out _); Assert.Equal(5, w.Pdm.Stock[0].Dirt); }
        [Fact] public void InsufficientCashOrRepairCounterLimitKeepsIntentUnpaid()
        { var w = Repairing(); w.Pdm.Cash = 999; Assert.False(PdmPremisesTransactions.Complete(w, "service", Save, out _)); w.Pdm.Cash = 1000; w.Pdm.Repairs = long.MaxValue - 999; Assert.False(PdmPremisesTransactions.Complete(w, "service", Save, out _)); Assert.Equal(500, w.Pdm.Stock[0].Engine); Assert.NotNull(w.Pdm.PremisesService); }
        [Fact] public void WrongServiceCannotCompleteOrCancelCurrentIntent()
        { var w = Cleaning(); Assert.False(PdmPremisesTransactions.Complete(w, "wrong", Save, out _)); Assert.False(PdmPremisesTransactions.Cancel(w, "wrong", Save, out _)); Assert.Equal("service", w.Pdm.PremisesService.Id); }
        [Fact] public void NewDirtDoesNotRewriteExistingFinanceContract()
        { var w = Stock(); PdmShowroomTransactions.Remove(w, "AA111stock", "remove", Save, out _); PdmTransactions.Begin(w, PdmDefaults.Load(), "AA111stock", "loan", 0, 200, Save, out _, true); PdmFinanceTransactions.Open(w, PdmFinanceDefaults.Load(), "loan", 1000, 1000, Save, out _); int total = w.Pdm.Loans[0].Total; Assert.Equal(12320, total); Assert.Equal(0, w.Pdm.Loans[0].Asset.Dirt); }
        [Fact] public void UnknownCompanyStockAndServiceFieldsSurviveCheckpoints()
        { var w = Cleaning(); var j = JObject.Parse(JsonConvert.SerializeObject(w)); j["Pdm"]["Future"] = 1; j["Pdm"]["Stock"][0]["Future"] = 2; j["Pdm"]["PremisesService"]["Future"] = 3; w = j.ToObject<WalletData>(); PdmPremisesTransactions.Work(w, C, 1, "clock", Save, out _); j = JObject.Parse(JsonConvert.SerializeObject(w)); Assert.Equal(3, (int)j["Pdm"]["PremisesService"]["Future"]); PdmPremisesTransactions.Complete(w, "service", Save, out _); j = JObject.Parse(JsonConvert.SerializeObject(w)); Assert.Equal(1, (int)j["Pdm"]["Future"]); Assert.Equal(2, (int)j["Pdm"]["Stock"][0]["Future"]); }
        [Fact] public void InvalidDirtOrServiceHandleCannotMutateWallet()
        { var w = Cleaning(); w.Pdm.PremisesService.Handle = 222; Assert.False(w.Pdm.Valid); Assert.False(PdmPremisesTransactions.Complete(w, "service", Save, out _)); w = Stock(); w.Pdm.Stock[0].Dirt = 16; Assert.False(w.Pdm.Valid); Assert.Equal(0, PdmTransactions.Quote(w.Pdm.Stock[0], 1000, 1000)); }
        [Fact] public void ServiceMachineNeedsEligibleDurationAndRestartsOnInterruption()
        { var r = new PdmServiceRun("service", "stock", "clean", 6); Assert.False(r.Start(0, false)); Assert.True(r.Start(100, true)); Assert.False(r.Commit(6099, true, () => throw new Exception())); Assert.False(r.Commit(6100, false, () => throw new Exception())); Assert.Equal(PdmServiceStage.Approaching, r.Stage); Assert.True(r.Start(10000, true)); Assert.False(r.Commit(10001, true, () => throw new Exception())); }
        [Fact] public void ServiceMachineRetriesWithoutCompletingOrCancellingTwice()
        { var r = new PdmServiceRun("service", "stock", "repair", 8); r.Start(0, true); Assert.False(r.Commit(8000, true, () => false)); Assert.True(r.Commit(8100, true, () => true)); Assert.False(r.Commit(9000, true, () => throw new Exception())); Assert.False(r.Cancel(() => throw new Exception())); }
        [Fact] public void ServiceTimerHandlesOverflowAndCancelNeedsCheckpoint()
        { var r = new PdmServiceRun("service", "stock", "clean", 6); int began = int.MaxValue - 100; r.Start(began, true); Assert.True(r.Commit(unchecked(began + 6000), true, () => true)); r = new PdmServiceRun("service2", "stock", "repair", 8); Assert.False(r.Cancel(() => false)); Assert.True(r.Cancel(() => true)); Assert.False(r.Start(0, true)); }
    }
}
