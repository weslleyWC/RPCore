using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class PdmSurveillanceTests
    {
        static readonly Action Save = () => { };
        static PdmSurveillanceOptions C => PdmSurveillanceDefaults.Load();
        static WalletData Company()
        { var w = new WalletData { Bank = 100000, Cash = 55 }; Assert.True(PdmTransactions.Purchase(w, PdmDefaults.Load(), "company", Save, out _)); Assert.True(PdmTransactions.Transfer(w, 50000, true, "capital", Save, out _)); return w; }
        [Fact] public void DefaultsHaveThreeReferenceMountPositionsRotationsModelAndPrice()
        { var c = C; Assert.True(c.Valid); Assert.Equal(15000, c.Price); Assert.Equal("prop_cctv_cam_04c", c.Model); Assert.Equal(3, c.Cameras.Count); Assert.Equal(-39.0766f, c.Cameras[0].Mount.X); Assert.Equal(-108.5868f, c.Cameras[0].Mount.Heading); Assert.Equal(-56.6277f, c.Cameras[1].Mount.X); Assert.Equal(-111.5288f, c.Cameras[1].Mount.Heading); Assert.Equal(-23.2955f, c.Cameras[2].Mount.X); Assert.Equal(155.1891f, c.Cameras[2].Mount.Heading); }
        [Fact] public void OldSaveMissingSurveillanceReceivesNoAutomaticPurchaseOrCharge()
        { var w = Company(); var j = JObject.Parse(JsonConvert.SerializeObject(w)); j["Pdm"]["Surveillance"].Parent.Remove(); w = j.ToObject<WalletData>(); Assert.True(w.Pdm.Valid); Assert.False(w.Pdm.Surveillance.Purchased); Assert.Equal(0, w.Pdm.Surveillance.Paid); Assert.Equal(50000, w.Pdm.Cash); }
        [Fact] public void PurchaseUsesCompanyCashAndExistingUpgradeAccountingOnce()
        { var w = Company(); Assert.True(PdmSurveillanceTransactions.Purchase(w, C, "cameras", Save, out _)); Assert.True(w.Pdm.Surveillance.Purchased); Assert.Equal(15000, w.Pdm.Surveillance.Paid); Assert.Equal(35000, w.Pdm.Cash); Assert.Equal(15000, w.Pdm.Staff.UpgradeSpent); Assert.Equal(25000, w.Bank); Assert.Equal(55, w.Cash); Assert.Equal(-15000, w.Pdm.Ledger.Last().Amount); Assert.Equal(0, w.Pdm.Revenue); Assert.Equal(0, w.Pdm.Sold); }
        [Fact] public void SaveFailureRestoresCashUpgradeOwnershipAndReceipt()
        { var w = Company(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmSurveillanceTransactions.Purchase(w, C, "cameras", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void ReloadAndReceiptReplayNeverDoubleChargeOrReinstallPurchase()
        { var w = Company(); PdmSurveillanceTransactions.Purchase(w, C, "cameras", Save, out _); w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); string before = JsonConvert.SerializeObject(w); Assert.True(PdmSurveillanceTransactions.Purchase(w, C, "cameras", () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Equal(before, JsonConvert.SerializeObject(w)); Assert.False(PdmSurveillanceTransactions.Purchase(w, C, "second", Save, out _)); }
        [Fact] public void PurchaseNeedsOwnedIdleCompanyAndEnoughCash()
        { var w = new WalletData(); Assert.False(PdmSurveillanceTransactions.Purchase(w, C, "cameras", Save, out _)); w = Company(); w.Pdm.Cash = 14999; Assert.False(PdmSurveillanceTransactions.Purchase(w, C, "cameras", Save, out _)); Assert.False(w.Pdm.Surveillance.Purchased); }
        [Fact] public void PurchaseIsBlockedDuringCustomerHandover()
        { var w = Company(); PdmTransactions.Buy(w, PdmDefaults.Load(), "asea", "AA111stock", "buy", Save, out _); Assert.True(PdmTransactions.Begin(w, PdmDefaults.Load(), "AA111stock", "delivery", 0, 200, Save, out _)); Assert.False(PdmSurveillanceTransactions.Purchase(w, C, "cameras", Save, out _)); Assert.NotNull(w.Pdm.Active); Assert.False(w.Pdm.Surveillance.Purchased); }
        [Fact] public void PurchaseIsBlockedDuringPhysicalMaintenance()
        { var w = Company(); PdmTransactions.Buy(w, PdmDefaults.Load(), "asea", "AA111stock", "buy", Save, out _); PdmShowroomTransactions.Assign(w, PdmShowroomDefaults.Load(), "AA111stock", "lot1", 111, "assign", Save, out _); var staff = PdmPremisesDefaults.Merge(PdmStaffDefaults.Load(), PdmPremisesDefaults.Load()); PdmStaffTransactions.Hire(w, staff, "cleaner", "maid", Save, out _); w.Pdm.Stock[0].Dirt = 1; Assert.True(PdmPremisesTransactions.Begin(w, PdmDefaults.Load(), "AA111stock", "clean", "service", Save, out _)); Assert.False(PdmSurveillanceTransactions.Purchase(w, C, "cameras", Save, out _)); }
        [Fact] public void CounterOverflowCannotConsumeMoneyOrSetOwnership()
        { var w = Company(); w.Pdm.Staff.UpgradeSpent = long.MaxValue - 14999; Assert.False(PdmSurveillanceTransactions.Purchase(w, C, "cameras", Save, out _)); Assert.Equal(50000, w.Pdm.Cash); Assert.False(w.Pdm.Surveillance.Purchased); }
        [Fact] public void ExactCashAndCounterHeadroomAreAllowed()
        { var w = Company(); w.Pdm.Cash = 15000; w.Pdm.Staff.UpgradeSpent = long.MaxValue - 15000; Assert.True(PdmSurveillanceTransactions.Purchase(w, C, "cameras", Save, out _)); Assert.Equal(0, w.Pdm.Cash); Assert.Equal(long.MaxValue, w.Pdm.Staff.UpgradeSpent); }
        [Fact] public void SavedPriceIsNotRewrittenWhenConfigurationChanges()
        { var w = Company(); PdmSurveillanceTransactions.Purchase(w, C, "cameras", Save, out _); var c = C; c.Price = 22000; Assert.False(PdmSurveillanceTransactions.Purchase(w, c, "upgrade", Save, out _)); Assert.Equal(15000, w.Pdm.Surveillance.Paid); Assert.Equal(35000, w.Pdm.Cash); }
        [Fact] public void ExistingStockConditionPayrollAndGuardEligibilityArePreserved()
        { var w = Company(); PdmTransactions.Buy(w, PdmDefaults.Load(), "asea", "AA111stock", "buy", Save, out _); w.Pdm.Stock[0].Dirt = 3; var staff = PdmSecurityDefaults.Merge(PdmStaffDefaults.Load(), PdmSecurityDefaults.Load()); PdmStaffTransactions.Hire(w, staff, "security1", "guard", Save, out _); string stock = JsonConvert.SerializeObject(w.Pdm.Stock), employees = JsonConvert.SerializeObject(w.Pdm.Staff.Employees); PdmSurveillanceTransactions.Purchase(w, C, "cameras", Save, out _); Assert.Equal(stock, JsonConvert.SerializeObject(w.Pdm.Stock)); Assert.Equal(employees, JsonConvert.SerializeObject(w.Pdm.Staff.Employees)); Assert.True(PdmSecurityPolicy.Paid(w.Pdm, "security1")); }
        [Fact] public void UnknownCompanyAndSurveillanceFieldsSurvivePurchase()
        { var w = Company(); var j = JObject.Parse(JsonConvert.SerializeObject(w)); j["Pdm"]["Future"] = 1; j["Pdm"]["Surveillance"]["Future"] = 2; w = j.ToObject<WalletData>(); Assert.True(PdmSurveillanceTransactions.Purchase(w, C, "cameras", Save, out _)); j = JObject.Parse(JsonConvert.SerializeObject(w)); Assert.Equal(1, (int)j["Pdm"]["Future"]); Assert.Equal(2, (int)j["Pdm"]["Surveillance"]["Future"]); }
        [Theory] [InlineData(0)] [InlineData(1000001)]
        public void InvalidPurchasePriceCannotMutateWallet(int price)
        { var w = Company(); var c = C; c.Price = price; Assert.False(c.Valid); Assert.False(PdmSurveillanceTransactions.Purchase(w, c, "cameras", Save, out _)); Assert.Equal(50000, w.Pdm.Cash); }
        [Fact] public void InvalidStateCannotOverwriteOriginalWallet()
        { var w = Company(); w.Pdm.Surveillance.Purchased = true; Assert.False(w.Pdm.Valid); Assert.False(PdmSurveillanceTransactions.Purchase(w, C, "cameras", Save, out _)); }
        [Fact] public void BadCoordinatesDuplicateIdsAndDistantViewAreRejected()
        { var c = C; c.Cameras[0].Mount.X = float.NaN; Assert.False(c.Valid); c = C; c.Cameras[1].Id = c.Cameras[0].Id; Assert.False(c.Valid); c = C; c.Cameras[0].View.X += 100; Assert.False(c.Valid); c = C; c.Cameras[0].LookAt = c.Cameras[0].View; Assert.False(c.Valid); }
        [Fact] public void MonitorCyclesBothDirectionsAndRejectsInvalidSwitchWithoutRestartingTimeout()
        { var r = new PdmMonitorSession(3, 0, 100, 120); Assert.True(r.Switch(-1)); Assert.Equal(2, r.Index); Assert.True(r.Switch(1)); Assert.Equal(0, r.Index); Assert.False(r.Switch(2)); Assert.True(r.Continue(120099, true)); Assert.False(r.Continue(120100, true)); Assert.False(r.Switch(1)); Assert.Equal(0, r.Index); }
        [Fact] public void MonitorInterruptionIsFinalAndDoesNotNeedFinancialMutation()
        { var r = new PdmMonitorSession(3, 1, 0, 120); Assert.False(r.Continue(1, false)); Assert.False(r.Continue(2, true)); Assert.False(r.Switch(1)); r.Close(); Assert.False(r.Active); }
        [Fact] public void MonitorTimeHandlesGameTimeOverflowAndClosingIsIdempotent()
        { int start = int.MaxValue - 100; var r = new PdmMonitorSession(3, 2, start, 30); Assert.True(r.Continue(unchecked(start + 29999), true)); Assert.False(r.Continue(unchecked(start + 30000), true)); r.Close(); r.Close(); Assert.False(r.Active); }
        [Fact] public void MonitorAllowsInitialRenderFrameButDoesNotWaitForever()
        { var r = new PdmMonitorSession(3,0,100,120); Assert.True(r.Rendering(100,false,false)); Assert.True(r.Rendering(599,false,false)); Assert.False(r.Rendering(600,false,false)); }
        [Fact] public void AnotherCameraOrLossAfterConfirmationStopsOurSessionImmediately()
        { var r = new PdmMonitorSession(3,0,0,120); Assert.False(r.Rendering(1,false,true)); Assert.True(r.Rendering(2,true,false)); Assert.False(r.Rendering(3,false,false)); r.Close(); Assert.False(r.Rendering(4,true,false)); }
        [Theory] [InlineData(0,0,120)] [InlineData(7,0,120)] [InlineData(3,-1,120)] [InlineData(3,3,120)] [InlineData(3,0,29)] [InlineData(3,0,301)]
        public void InvalidMonitorSessionsRejected(int count, int index, int seconds) { Assert.Throws<ArgumentException>(() => new PdmMonitorSession(count,index,0,seconds)); }
    }
}
