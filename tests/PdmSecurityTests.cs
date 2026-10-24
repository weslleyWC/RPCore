using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class PdmSecurityTests
    {
        static readonly Action Save = () => { };
        static PdmSecurityOptions Config => PdmSecurityDefaults.Load();
        static PdmStaffOptions Staff => PdmSecurityDefaults.Merge(PdmPremisesDefaults.Merge(PdmStaffDefaults.Load(), PdmPremisesDefaults.Load()), Config);
        static WalletData Company()
        { var w = new WalletData { Bank = 100000, Cash = 55 }; Assert.True(PdmTransactions.Purchase(w, PdmDefaults.Load(), "company", Save, out _)); Assert.True(PdmTransactions.Transfer(w, 50000, true, "capital", Save, out _)); return w; }
        static void Hire(WalletData w, string role) => Assert.True(PdmStaffTransactions.Hire(w, Staff, role, role + "-employee", Save, out _));
        static void Period(WalletData w) { for (int i = 0; i < 5; i++) Assert.True(PdmStaffTransactions.Work(w, 60, "work" + i, Save, out _)); }
        [Fact] public void DefaultsMatchBothReferencePostsModelsAndTwentyMeterDefense()
        { var c = Config; Assert.True(c.Valid); Assert.Equal(2, c.Posts.Count); Assert.Equal(-36.68868f, c.Posts[0].Point.X); Assert.Equal(147.7108f, c.Posts[0].Point.Heading); Assert.Equal(-60.66545f, c.Posts[1].Point.X); Assert.Equal(89.74267f, c.Posts[1].Point.Heading); Assert.Equal("s_m_m_highsec_02", c.Posts[0].Model); Assert.Equal("s_m_m_highsec_01", c.Posts[1].Model); Assert.Equal(20, c.DefenseRadius); }
        [Fact] public void ExistingFourOrFiveRoleFilesAreMergedWithoutRewriteOrDuplicate()
        { var old = PdmStaffDefaults.Load(); string before = JsonConvert.SerializeObject(old); var first = PdmSecurityDefaults.Merge(old, Config); Assert.Equal(before, JsonConvert.SerializeObject(old)); Assert.Equal(6, first.Roles.Count); Assert.True(first.Valid); Assert.Equal(7, Staff.Roles.Count); Assert.Equal(7, PdmSecurityDefaults.Merge(Staff, Config).Roles.Count); }
        [Fact] public void CustomizedGuardOffersAndUnknownConfigFieldsSurviveMerge()
        { var staff = Staff; staff.Roles.Single(r => r.Id == "security1").Wage = 777; var j = JObject.Parse(JsonConvert.SerializeObject(staff)); j["Future"] = 42; staff = PdmSecurityDefaults.Merge(j.ToObject<PdmStaffOptions>(), Config); Assert.Equal(777, staff.Roles.Single(r => r.Id == "security1").Wage); Assert.Equal(42, (int)JObject.Parse(JsonConvert.SerializeObject(staff))["Future"]); }
        [Fact] public void OldSaveReceivesNoAutomaticGuardsOrCharges()
        { var w = Company(); string before = JsonConvert.SerializeObject(w); Assert.False(PdmSecurityPolicy.Paid(w.Pdm, "security1")); Assert.Empty(w.Pdm.Staff.Employees); Assert.Equal(before, JsonConvert.SerializeObject(w)); Assert.Equal(50000, w.Pdm.Cash); }
        [Fact] public void BothGuardsUseCompanyCashAndIndependentPersistentContracts()
        { var w = Company(); Hire(w, "security1"); Hire(w, "security2"); Assert.Equal(47400, w.Pdm.Cash); Assert.Equal(25000, w.Bank); Assert.Equal(55, w.Cash); Assert.Equal(2600, w.Pdm.Staff.HiringSpent); Assert.Equal(2, w.Pdm.Staff.Employees.Count); Assert.All(w.Pdm.Staff.Employees, e => { Assert.Equal(260, e.Wage); Assert.Equal(300, e.PeriodSeconds); }); w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); Assert.True(PdmSecurityPolicy.Paid(w.Pdm, "security1")); Assert.True(PdmSecurityPolicy.Paid(w.Pdm, "security2")); }
        [Fact] public void AllSevenStaffRolesCanCoexistWithoutChangingMarketing()
        { var w = Company(); foreach (var role in new[] { "mechanic", "junior", "senior", "director", "cleaner", "security1", "security2" }) Hire(w, role); Assert.Equal(7, w.Pdm.Staff.Employees.Count); Assert.True(w.Pdm.Valid); Assert.Equal(6, PdmStaffTransactions.BonusPercent(w.Pdm)); }
        [Fact] public void FailedGuardHireRestoresEverythingAndReplayCannotChargeTwice()
        { var w = Company(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmStaffTransactions.Hire(w, Staff, "security1", "guard", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); Assert.True(PdmStaffTransactions.Hire(w, Staff, "security1", "guard", Save, out _)); int cash = w.Pdm.Cash; Assert.True(PdmStaffTransactions.Hire(w, Staff, "security1", "guard", () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Single(w.Pdm.Staff.Employees); Assert.Equal(cash, w.Pdm.Cash); }
        [Fact] public void DueWagesSuspendOnlyUnpaidGuardUntilPaid()
        { var w = Company(); Hire(w, "security1"); Hire(w, "security2"); Period(w); Assert.False(PdmSecurityPolicy.Paid(w.Pdm, "security1")); Assert.False(PdmSecurityPolicy.Paid(w.Pdm, "security2")); Assert.True(PdmStaffTransactions.Pay(w, "security1", "pay", Save, out _)); Assert.True(PdmSecurityPolicy.Paid(w.Pdm, "security1")); Assert.False(PdmSecurityPolicy.Paid(w.Pdm, "security2")); Assert.Equal(47140, w.Pdm.Cash); }
        [Fact] public void FailedPayDoesNotRearmAndSuccessfulReplayCannotDoublePay()
        { var w = Company(); Hire(w, "security1"); Period(w); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmStaffTransactions.Pay(w, "security1", "pay", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); Assert.False(PdmSecurityPolicy.Paid(w.Pdm, "security1")); Assert.True(PdmStaffTransactions.Pay(w, "security1", "pay", Save, out _)); int cash = w.Pdm.Cash; Assert.True(PdmStaffTransactions.Pay(w, "security1", "pay", () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Equal(cash, w.Pdm.Cash); }
        [Fact] public void FiringTransfersProratedWageAndSuspendsRemainingGuardForFormerDebt()
        { var w = Company(); Hire(w, "security1"); Hire(w, "security2"); PdmStaffTransactions.Work(w, 60, "minute", Save, out _); Assert.True(PdmStaffTransactions.Fire(w, "security1-employee", "fire", Save, out _)); Assert.Equal(302, w.Pdm.Staff.FormerDebt); Assert.False(PdmSecurityPolicy.Paid(w.Pdm, "security1")); Assert.False(PdmSecurityPolicy.Paid(w.Pdm, "security2")); Assert.True(PdmStaffTransactions.Pay(w, "former", "acerto", Save, out _)); Assert.True(PdmSecurityPolicy.Paid(w.Pdm, "security2")); }
        [Fact] public void GuardOffersCannotGrantSaleBonusOrRequireMarketingHierarchy()
        { var s = Staff; s.Roles.Single(r => r.Id == "security1").SaleBonusPercent = 1; Assert.False(s.Valid); s = Staff; s.Roles.Single(r => r.Id == "security2").Requires = "junior"; Assert.False(s.Valid); var c = Config; c.Posts[0].Offer.Requires = "junior"; Assert.False(c.Valid); }
        [Fact] public void GuardsDoNotDiscountRepairsOrIncreaseQuotes()
        { var w = Company(); Hire(w, "security1"); Hire(w, "security2"); w.Pdm.Staff.Workshop = w.Pdm.Staff.Advertising = true; Assert.Equal(0, PdmStaffTransactions.BonusPercent(w.Pdm)); Assert.Equal(1000, PdmStaffTransactions.RepairPrice(w.Pdm, 1000)); }
        [Fact] public void InsufficientFundsOrFormerDebtCannotCreateEmployment()
        { var w = Company(); w.Pdm.Cash = 1299; Assert.False(PdmStaffTransactions.Hire(w, Staff, "security1", "guard", Save, out _)); w.Pdm.Cash = 50000; w.Pdm.Staff.FormerDebt = 1; Assert.False(PdmStaffTransactions.Hire(w, Staff, "security2", "guard", Save, out _)); Assert.Empty(w.Pdm.Staff.Employees); }
        [Fact] public void ContractWagesRemainSnapshotAfterOfferChanges()
        { var w = Company(); Hire(w, "security1"); var s = Staff; s.Roles.Single(r => r.Id == "security1").Wage = 777; Period(w); Assert.Equal(260, w.Pdm.Staff.Employees[0].Owed); Assert.Equal(260, w.Pdm.Staff.Employees[0].Wage); }
        [Fact] public void InvalidOrUnownedCompanyAndNonGuardRoleCannotBeOnSecurityDuty()
        { Assert.False(PdmSecurityPolicy.Paid(null, "security1")); var w = Company(); Hire(w, "mechanic"); Assert.False(PdmSecurityPolicy.Paid(w.Pdm, "mechanic")); Hire(w, "security1"); w.Pdm.Owned = false; Assert.False(PdmSecurityPolicy.Paid(w.Pdm, "security1")); w.Pdm.Owned = true; w.Pdm.Cash = -1; Assert.False(PdmSecurityPolicy.Paid(w.Pdm, "security1")); }
        [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)] [InlineData(10)]
        public void DefenseRequiresEveryConditionAndExcludesPlayersPoliceMissionsCrewAndBystanders(int failed)
        { Assert.False(PdmSecurityPolicy.CanDefend(failed != 0, failed != 1, failed != 2, failed != 3, failed != 4, failed == 5, failed == 6, failed == 7, failed == 8, failed != 9, failed != 10, 10, 10, 20)); }
        [Theory] [InlineData(21, 10)] [InlineData(10, 21)] [InlineData(-1, 10)] [InlineData(10, -1)] [InlineData(float.NaN,10)] [InlineData(10,float.PositiveInfinity)]
        public void TargetOrGuardOutsideLeashNeverStartsOrContinuesDefense(float target, float guard)
        { Assert.False(PdmSecurityPolicy.CanDefend(true,true,true,true,true,false,false,false,false,true,true,target,guard,20)); }
        [Fact] public void ConfirmedVisibleLocalAttackerCanBeDefendedAtBoundary()
        { Assert.True(PdmSecurityPolicy.CanDefend(true,true,true,true,true,false,false,false,false,true,true,20,20,20)); }
        [Theory] [InlineData(9)] [InlineData(31)]
        public void InvalidDefenseRadiusRejected(int radius) { var c = Config; c.DefenseRadius = radius; Assert.False(c.Valid); }
        [Fact] public void DuplicatePostOrInvalidRespawnRejected()
        { var c = Config; c.Posts[1].Offer.Id = "security1"; Assert.False(c.Valid); c = Config; c.RespawnSeconds = 1; Assert.False(c.Valid); }
        [Fact] public void UnknownStockCompanyAndEmploymentFieldsSurvivePayroll()
        { var w = Company(); Hire(w, "security1"); var j = JObject.Parse(JsonConvert.SerializeObject(w)); j["Pdm"]["Future"] = 11; j["Pdm"]["Staff"]["Employees"][0]["Future"] = 12; w = j.ToObject<WalletData>(); Period(w); j = JObject.Parse(JsonConvert.SerializeObject(w)); Assert.Equal(11, (int)j["Pdm"]["Future"]); Assert.Equal(12, (int)j["Pdm"]["Staff"]["Employees"][0]["Future"]); }
    }
}
