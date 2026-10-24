using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class PdmShowroomTests
    {
        static readonly Action Save = () => { };
        static PdmShowroomOptions C => PdmShowroomDefaults.Load();
        static WalletData Stock()
        { var w = new WalletData { Bank = 100000, Cash = 55 }; PdmTransactions.Purchase(w, PdmDefaults.Load(), "company", Save, out _); PdmTransactions.Transfer(w, 50000, true, "capital", Save, out _); Assert.True(PdmTransactions.Buy(w, PdmDefaults.Load(), "asea", "AA111stock", "buy", Save, out _)); return w; }
        static WalletData Displayed()
        { var w = Stock(); Assert.True(PdmShowroomTransactions.Assign(w, C, "AA111stock", "lot1", 111, "assign", Save, out _)); return w; }
        static WalletData Buyer()
        { var w = Displayed(); Assert.True(PdmShowroomTransactions.BeginSale(w, "AA111stock", "buyer", Save, out _)); return w; }
        [Fact] public void DefaultsHaveNineteenReferenceSlotsAndSevenNumberedParkingSpaces()
        { var c = C; Assert.True(c.Valid); Assert.Equal(19, c.Slots.Count); Assert.Equal(7, c.Slots.Count(s => s.Id.StartsWith("lot"))); Assert.Equal(12, c.Slots.Count(s => s.Id.StartsWith("room"))); Assert.Equal(-45.05214f, c.Slots.Single(s => s.Id == "lot1").Point.X); Assert.Equal(3.350951f, c.Slots.Single(s => s.Id == "lot1").Point.Heading); Assert.Equal(-36.24031f, c.Slots.Single(s => s.Id == "room1").Point.X); }
        [Fact] public void DuplicateSlotsAndInvalidTimingRejected()
        { var c = C; c.Slots.Add(c.Slots[0]); Assert.False(c.Valid); c = C; c.HandoverSeconds = 1; Assert.False(c.Valid); c = C; c.BuyerTimeoutSeconds = 59; Assert.False(c.Valid); }
        [Fact] public void OldCompanyWithoutShowroomFieldsKeepsPaidStockAndCash()
        { var w = Stock(); var j = JObject.Parse(JsonConvert.SerializeObject(w)); j["Pdm"]["Showroom"].Parent.Remove(); j["Pdm"]["ShowroomSale"].Parent.Remove(); w = j.ToObject<WalletData>(); Assert.True(w.Pdm.Valid); Assert.Empty(w.Pdm.Showroom); Assert.Null(w.Pdm.ShowroomSale); Assert.Single(w.Pdm.Stock); Assert.Equal(42000, w.Pdm.Cash); }
        [Fact] public void AssignmentKeepsCarInStockAndDoesNotChargeOrSellIt()
        { var w = Displayed(); Assert.Single(w.Pdm.Stock); Assert.Single(w.Pdm.Showroom); Assert.Equal(111, w.Pdm.Showroom[0].Handle); Assert.Equal("lot1", w.Pdm.Showroom[0].SlotId); Assert.Equal(42000, w.Pdm.Cash); Assert.Equal(25000, w.Bank); Assert.Equal(0, w.Pdm.Sold); Assert.Equal(0, w.Pdm.Revenue); }
        [Fact] public void CannotAssignDuplicateStockSlotHandleOrDisabledSlot()
        { var w = Displayed(); PdmTransactions.Buy(w, PdmDefaults.Load(), "asea", "BB222stock", "second", Save, out _); Assert.False(PdmShowroomTransactions.Assign(w, C, "AA111stock", "lot2", 112, "dup-stock", Save, out _)); Assert.False(PdmShowroomTransactions.Assign(w, C, "BB222stock", "lot1", 112, "dup-slot", Save, out _)); Assert.False(PdmShowroomTransactions.Assign(w, C, "BB222stock", "lot2", 111, "dup-handle", Save, out _)); var c = C; c.Slots.Single(s => s.Id == "lot2").Enabled = false; Assert.False(PdmShowroomTransactions.Assign(w, c, "BB222stock", "lot2", 112, "disabled", Save, out _)); }
        [Fact] public void CannotExposePendingOrBrokenStockOrMissingSlot()
        { var w = Stock(); w.Pdm.Stock[0].AwaitingCollection = true; Assert.False(PdmShowroomTransactions.Assign(w, C, "AA111stock", "lot1", 111, "a", Save, out _)); w.Pdm.Stock[0].AwaitingCollection = false; w.Pdm.Stock[0].Engine = 299; Assert.False(PdmShowroomTransactions.Assign(w, C, "AA111stock", "lot1", 111, "a", Save, out _)); w.Pdm.Stock[0].Engine = 1000; Assert.False(PdmShowroomTransactions.Assign(w, C, "AA111stock", "missing", 111, "a", Save, out _)); }
        [Fact] public void FailedAssignmentRestoresStockAndReplayCannotAddTwice()
        { var w = Stock(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmShowroomTransactions.Assign(w, C, "AA111stock", "lot1", 111, "a", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); Assert.True(PdmShowroomTransactions.Assign(w, C, "AA111stock", "lot1", 111, "a", Save, out _)); Assert.True(PdmShowroomTransactions.Assign(w, C, "AA111stock", "lot1", 111, "a", () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Single(w.Pdm.Showroom); }
        [Fact] public void TrackAllowsDespawnAndNewHandleWithoutChangingAssignmentOrMoney()
        { var w = Displayed(); Assert.True(PdmShowroomTransactions.Track(w, "AA111stock", 0, "far", Save, out _)); Assert.Equal(0, w.Pdm.Showroom[0].Handle); w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); Assert.True(PdmShowroomTransactions.Track(w, "AA111stock", 222, "near", Save, out _)); Assert.Equal(222, w.Pdm.Showroom[0].Handle); Assert.Equal("lot1", w.Pdm.Showroom[0].SlotId); Assert.Equal(42000, w.Pdm.Cash); }
        [Fact] public void TrackFailureKeepsOldHandleAndRejectsNegativeOrMissingEntry()
        { var w = Displayed(); Assert.Throws<Exception>(() => PdmShowroomTransactions.Track(w, "AA111stock", 222, "near", () => throw new Exception(), out _)); Assert.Equal(111, w.Pdm.Showroom[0].Handle); Assert.False(PdmShowroomTransactions.Track(w, "AA111stock", -1, "bad", Save, out _)); Assert.False(PdmShowroomTransactions.Track(w, "missing", 222, "missing", Save, out _)); }
        [Fact] public void RemoveReleasesOnlySlotAndPreservesPaidCar()
        { var w = Displayed(); Assert.True(PdmShowroomTransactions.Remove(w, "AA111stock", "remove", Save, out _)); Assert.Empty(w.Pdm.Showroom); Assert.Single(w.Pdm.Stock); Assert.Equal(42000, w.Pdm.Cash); Assert.True(PdmTransactions.Begin(w, PdmDefaults.Load(), "AA111stock", "delivery", 0, 222, Save, out _)); }
        [Fact] public void DisplayedStockMustBeRecollectedBeforeDeliveryOrRepair()
        { var w = Displayed(); w.Pdm.Stock[0].Body = 500; Assert.False(PdmTransactions.Begin(w, PdmDefaults.Load(), "AA111stock", "delivery", 0, 222, Save, out _)); Assert.False(PdmTransactions.Repair(w, PdmDefaults.Load(), "AA111stock", "repair", Save, out _)); Assert.Single(w.Pdm.Showroom); }
        [Fact] public void RentalCannotReserveDisplayedUnitAmongTwoMatchingCars()
        { var w = Displayed(); PdmTransactions.Buy(w, PdmDefaults.Load(), "asea", "BB222stock", "second", Save, out _); Assert.False(PdmRentalTransactions.Begin(w, PdmRentalDefaults.Load(), "rental", new System.Collections.Generic.List<string> { "AA111stock", "BB222stock" }, new PdmEntityRef { Handle = 200, Model = "packer", Plate = "TRUCK" }, new PdmEntityRef { Handle = 201, Model = "trflat", Plate = "TRAILER" }, Save, out _)); Assert.Null(w.Pdm.Rental); }
        [Fact] public void PreparingOneStockUnitDoesNotClearOtherSlots()
        { var w = Displayed(); PdmTransactions.Buy(w, PdmDefaults.Load(), "asea", "BB222stock", "second", Save, out _); PdmShowroomTransactions.Assign(w, C, "BB222stock", "lot2", 112, "second-slot", Save, out _); Assert.True(PdmShowroomTransactions.Remove(w, "AA111stock", "remove", Save, out _)); Assert.Single(w.Pdm.Showroom); Assert.Equal("BB222stock", w.Pdm.Showroom[0].StockId); Assert.Equal(2, w.Pdm.Stock.Count); }
        [Fact] public void FailedRemoveDoesNotForgetSlotOrCar()
        { var w = Displayed(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmShowroomTransactions.Remove(w, "AA111stock", "remove", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void BuyerRequiresAssignedPhysicalCarAndOwnsNoMoneyUntilHandover()
        { var w = Stock(); Assert.False(PdmShowroomTransactions.BeginSale(w, "AA111stock", "buyer", Save, out _)); w = Displayed(); PdmShowroomTransactions.Track(w, "AA111stock", 0, "far", Save, out _); Assert.False(PdmShowroomTransactions.BeginSale(w, "AA111stock", "buyer", Save, out _)); w = Buyer(); Assert.Equal(11200, w.Pdm.ShowroomSale.Price); Assert.Equal(42000, w.Pdm.Cash); Assert.Equal(0, w.Pdm.Revenue); }
        [Fact] public void BuyerOfferSnapshotsDamageAndMarketingPrice()
        { var w = Displayed(); PdmStaffTransactions.Hire(w, PdmStaffDefaults.Load(), "junior", "j", Save, out _); w.Pdm.Stock[0].Body = 500; Assert.True(PdmShowroomTransactions.BeginSale(w, "AA111stock", "buyer", Save, out _)); Assert.Equal(9996, w.Pdm.ShowroomSale.Price); for (int i = 0; i < 5; i++) PdmStaffTransactions.Work(w, 60, "time" + i, Save, out _); Assert.Equal(0, PdmStaffTransactions.BonusPercent(w.Pdm)); Assert.True(PdmShowroomTransactions.Sell(w, "buyer", Save, out _)); Assert.Equal(9996, w.Pdm.Revenue); }
        [Fact] public void FailedBuyerIntentAndReplayCannotCreateTwoClients()
        { var w = Displayed(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmShowroomTransactions.BeginSale(w, "AA111stock", "buyer", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); PdmShowroomTransactions.BeginSale(w, "AA111stock", "buyer", Save, out _); Assert.True(PdmShowroomTransactions.BeginSale(w, "AA111stock", "buyer", () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.False(PdmShowroomTransactions.BeginSale(w, "AA111stock", "second", Save, out _)); }
        [Fact] public void ActiveBuyerPreventsAssignmentRemovalOrHandleReplacement()
        { var w = Buyer(); Assert.False(PdmShowroomTransactions.Remove(w, "AA111stock", "r", Save, out _)); Assert.False(PdmShowroomTransactions.Track(w, "AA111stock", 222, "track", Save, out _)); Assert.False(PdmTransactions.Buy(w, PdmDefaults.Load(), "asea", "BB222stock", "buy2", Save, out _)); Assert.False(PdmStaffTransactions.Hire(w, PdmStaffDefaults.Load(), "junior", "j", Save, out _)); Assert.Equal(111, w.Pdm.Showroom[0].Handle); }
        [Fact] public void SellAtomicallyCreditsCashAndRemovesExactStockSlotAndIntent()
        { var w = Buyer(); int saves = 0; Assert.True(PdmShowroomTransactions.Sell(w, "buyer", () => { saves++; Assert.Empty(w.Pdm.Stock); Assert.Empty(w.Pdm.Showroom); Assert.Null(w.Pdm.ShowroomSale); Assert.Equal(53200, w.Pdm.Cash); Assert.Equal(11200, w.Pdm.Revenue); Assert.Equal(8000, w.Pdm.SoldCost); }, out _)); Assert.Equal(1, saves); Assert.Equal(1, w.Pdm.Sold); Assert.Equal(25000, w.Bank); Assert.Equal(55, w.Cash); Assert.Equal(8000, w.Pdm.StockSpent); }
        [Fact] public void FailedSaleKeepsStockSlotClientAndBalancesForRetry()
        { var w = Buyer(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmShowroomTransactions.Sell(w, "buyer", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void SaleReplayAfterReloadCannotCreditOrRemoveAnotherStockUnit()
        { var w = Buyer(); PdmShowroomTransactions.Sell(w, "buyer", Save, out _); PdmTransactions.Buy(w, PdmDefaults.Load(), "asea", "BB222stock", "second", Save, out _); w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); string before = JsonConvert.SerializeObject(w); Assert.True(PdmShowroomTransactions.Sell(w, "buyer", () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Equal(before, JsonConvert.SerializeObject(w)); Assert.Single(w.Pdm.Stock); }
        [Fact] public void CashRevenueAndCostLimitsKeepSaleUnpaidAndCancellable()
        { var w = Buyer(); w.Pdm.Cash = int.MaxValue - 11199; Assert.False(PdmShowroomTransactions.Sell(w, "buyer", Save, out _)); w.Pdm.Cash = 0; w.Pdm.Revenue = long.MaxValue - 11199; Assert.False(PdmShowroomTransactions.Sell(w, "buyer", Save, out _)); w.Pdm.Revenue = 0; w.Pdm.SoldCost = long.MaxValue - 7999; Assert.False(PdmShowroomTransactions.Sell(w, "buyer", Save, out _)); Assert.True(PdmShowroomTransactions.Cancel(w, "buyer", Save, out _)); Assert.Single(w.Pdm.Stock); Assert.Single(w.Pdm.Showroom); }
        [Fact] public void CancelBuyerLeavesStockAndSlotWithoutIncome()
        { var w = Buyer(); Assert.True(PdmShowroomTransactions.Cancel(w, "buyer", Save, out _)); Assert.Null(w.Pdm.ShowroomSale); Assert.Single(w.Pdm.Stock); Assert.Single(w.Pdm.Showroom); Assert.Equal(42000, w.Pdm.Cash); Assert.Equal(0, w.Pdm.Revenue); }
        [Fact] public void CancellationFailurePreservesIntentAndReloadRecoveryCannotSellTwice()
        { var w = Buyer(); Assert.Throws<Exception>(() => PdmShowroomTransactions.Cancel(w, "buyer", () => throw new Exception(), out _)); Assert.NotNull(w.Pdm.ShowroomSale); w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); Assert.True(PdmShowroomTransactions.Cancel(w, "buyer", Save, out _)); Assert.False(PdmShowroomTransactions.Sell(w, "buyer", Save, out _)); Assert.Equal(42000, w.Pdm.Cash); }
        [Fact] public void WrongIntentCannotSellOrCancelAnotherBuyer()
        { var w = Buyer(); Assert.False(PdmShowroomTransactions.Sell(w, "wrong", Save, out _)); Assert.False(PdmShowroomTransactions.Cancel(w, "wrong", Save, out _)); Assert.Equal("buyer", w.Pdm.ShowroomSale.Id); }
        [Fact] public void UnknownSlotIntentStockAndCompanyFieldsSurviveCheckpoints()
        { var w = Buyer(); var j = JObject.Parse(JsonConvert.SerializeObject(w)); j["Pdm"]["Future"] = 1; j["Pdm"]["Stock"][0]["Future"] = 2; j["Pdm"]["Showroom"][0]["Future"] = 3; j["Pdm"]["ShowroomSale"]["Future"] = 4; w = j.ToObject<WalletData>(); PdmStaffTransactions.Work(w, 1, "no-staff", Save, out _); j = JObject.Parse(JsonConvert.SerializeObject(w)); Assert.Equal(4, (int)j["Pdm"]["ShowroomSale"]["Future"]); PdmShowroomTransactions.Cancel(w, "buyer", Save, out _); j = JObject.Parse(JsonConvert.SerializeObject(w)); Assert.Equal(2, (int)j["Pdm"]["Stock"][0]["Future"]); Assert.Equal(3, (int)j["Pdm"]["Showroom"][0]["Future"]); Assert.Equal(1, (int)j["Pdm"]["Future"]); }
        [Fact] public void MalformedAssignmentOrMismatchedIntentHandleCannotMutateWallet()
        { var w = Buyer(); w.Pdm.ShowroomSale.Handle = 222; Assert.False(w.Pdm.Valid); string before = JsonConvert.SerializeObject(w); Assert.False(PdmShowroomTransactions.Sell(w, "buyer", Save, out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void MachineRequiresThreeSecondsEligibilityAndRestartsAfterInterruption()
        { var r = new PdmShowroomRun("buyer", "stock", 3); Assert.False(r.Commit(9999, true, () => throw new Exception())); Assert.False(r.Handover(100, false)); Assert.True(r.Handover(100, true)); Assert.False(r.Commit(3099, true, () => throw new Exception())); Assert.False(r.Commit(3100, false, () => throw new Exception())); Assert.Equal(PdmShowroomStage.Waiting, r.Stage); Assert.True(r.Handover(5000, true)); Assert.False(r.Commit(5001, true, () => throw new Exception())); }
        [Fact] public void MachineRetriesSaleAndNeverAllowsCancellationAfterPayment()
        { var r = new PdmShowroomRun("buyer", "stock", 3); r.Handover(0, true); Assert.False(r.Commit(3000, true, () => false)); Assert.True(r.Commit(3100, true, () => true)); Assert.False(r.Commit(4000, true, () => throw new Exception())); Assert.False(r.Cancel(() => throw new Exception())); Assert.Equal(PdmShowroomStage.Complete, r.Stage); }
        [Fact] public void MachineCancellationRequiresCheckpointAndCannotResume()
        { var r = new PdmShowroomRun("buyer", "stock", 3); Assert.False(r.Cancel(() => false)); Assert.True(r.Cancel(() => true)); Assert.False(r.Handover(0, true)); Assert.False(r.Cancel(() => throw new Exception())); }
        [Fact] public void MachineTimerHandlesGameClockOverflow()
        { var r = new PdmShowroomRun("buyer", "stock", 3); int began = int.MaxValue - 100; r.Handover(began, true); Assert.True(r.Commit(unchecked(began + 3000), true, () => true)); }
    }
}
