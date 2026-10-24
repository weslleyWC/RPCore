using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class PdmRentalTests
    {
        static readonly Action Save = () => { };
        static PdmRentalOptions C => PdmRentalDefaults.Load();
        static readonly List<string> Ids = new List<string> { "AA111stock", "BB222stock" };
        static WalletData Stock()
        { var w = new WalletData { Bank = 100000, Cash = 55 }; PdmTransactions.Purchase(w, PdmDefaults.Load(), "company", Save, out _); PdmTransactions.Transfer(w, 50000, true, "capital", Save, out _); foreach (string id in Ids) Assert.True(PdmTransactions.Buy(w, PdmDefaults.Load(), "asea", id, "buy-" + id, Save, out _)); return w; }
        static PdmEntityRef Ref(int handle) => new PdmEntityRef { Handle = handle, Model = handle == 100 ? "packer" : "trflat", Plate = "RL" + handle };
        static bool Begin(WalletData w, Action save = null) => PdmRentalTransactions.Begin(w, C, "rental", new List<string>(Ids), Ref(100), Ref(101), save ?? Save, out _);
        static WalletData Started() { var w = Stock(); Assert.True(Begin(w)); return w; }
        static WalletData Registered()
        { var w = Started(); Assert.True(PdmRentalTransactions.Register(w, "rental", Ids[0], 200, Save, out _)); Assert.True(PdmRentalTransactions.Register(w, "rental", Ids[1], 201, Save, out _)); return w; }
        static WalletData Delivered()
        { var w = Registered(); Assert.True(PdmRentalTransactions.Deliver(w, "rental", Save, out _)); return w; }
        static WalletData Returned()
        { var w = Delivered(); Assert.True(PdmRentalTransactions.Return(w, "rental", Save, out _)); return w; }
        [Fact] public void DefaultsUseReferenceAirportPickupReturnWorkerAndOwnCatalogueModels()
        { var c = C; Assert.True(c.Valid); Assert.Equal(-911.405f, c.Delivery.X); Assert.Equal(-905.882f, c.Worker.X); Assert.Equal(-48.12541f, c.Return.X); Assert.Equal(2, c.Pickups.Count); Assert.Equal(-20.6272f, c.Pickups[0].X); Assert.Equal(-14.3133f, c.Pickups[1].X); Assert.Equal("cs_carbuyer", c.WorkerModel); Assert.Equal(95, c.PricePercent); Assert.Contains("asea", c.Models); }
        [Theory] [InlineData(79)] [InlineData(121)]
        public void InvalidRateRejected(int rate) { var c = C; c.PricePercent = rate; Assert.False(c.Valid); }
        [Fact] public void MissingPickupOrDuplicateModelRejected()
        { var c = C; c.Pickups.RemoveAt(0); Assert.False(c.Valid); c = C; c.Models.Add("ASEA"); Assert.False(c.Valid); }
        [Fact] public void OldSaveWithoutRentalRetainsStockCashAndOtherBusinessState()
        { var w = Stock(); var j = JObject.Parse(JsonConvert.SerializeObject(w)); j["Pdm"]["Rental"].Parent.Remove(); w = j.ToObject<WalletData>(); Assert.Null(w.Pdm.Rental); Assert.True(w.Pdm.Valid); Assert.Equal(2, w.Pdm.Stock.Count); Assert.Equal(34000, w.Pdm.Cash); Assert.Empty(w.Pdm.Loans); }
        [Fact] public void ReserveTwoPaidCarsWithoutChargingAgainOrCreatingMoney()
        { var w = Started(); Assert.Equal(34000, w.Pdm.Cash); Assert.Equal(25000, w.Bank); Assert.Equal(55, w.Cash); Assert.Equal(21280, w.Pdm.Rental.Amount); Assert.Equal(2, w.Pdm.Stock.Count); Assert.Equal(0, w.Pdm.Sold); Assert.True(w.Pdm.Valid); }
        [Fact] public void CannotReserveOneDuplicateMissingDifferentPendingOrBrokenCar()
        {
            var w = Stock(); Assert.False(PdmRentalTransactions.Begin(w, C, "rental", new List<string> { Ids[0] }, Ref(100), Ref(101), Save, out _));
            Assert.False(PdmRentalTransactions.Begin(w, C, "rental", new List<string> { Ids[0], Ids[0] }, Ref(100), Ref(101), Save, out _)); Assert.False(PdmRentalTransactions.Begin(w, C, "rental", new List<string> { Ids[0], "missing" }, Ref(100), Ref(101), Save, out _));
            w.Pdm.Stock[1].Model = "blista"; Assert.False(Begin(w)); w.Pdm.Stock[1].Model = "asea"; w.Pdm.Stock[1].AwaitingCollection = true; Assert.False(Begin(w)); w.Pdm.Stock[1].AwaitingCollection = false; w.Pdm.Stock[1].Engine = 299; Assert.False(Begin(w));
        }
        [Fact] public void RigRefsAreClonedAndCannotBeChangedByCaller()
        { var w = Stock(); var truck = Ref(100); Assert.True(PdmRentalTransactions.Begin(w, C, "rental", Ids, truck, Ref(101), Save, out _)); truck.Plate = "OTHER"; Assert.Equal("RL100", w.Pdm.Rental.Truck.Plate); }
        [Fact] public void FailedReserveRollsBackAndReplayDoesNotReserveTwice()
        { var w = Stock(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => Begin(w, () => throw new Exception())); Assert.Equal(before, JsonConvert.SerializeObject(w)); Assert.True(Begin(w)); Assert.True(PdmRentalTransactions.Begin(w, C, "rental", Ids, Ref(100), Ref(101), () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Equal(34000, w.Pdm.Cash); }
        [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(100)] [InlineData(101)]
        public void RegisterRejectsInvalidOrRigHandle(int handle)
        { var w = Started(); Assert.False(PdmRentalTransactions.Register(w, "rental", Ids[0], handle, Save, out _)); }
        [Fact] public void CargoHandlesUniqueSavedOnceAndRetryKeepsIdentity()
        { var w = Started(); Assert.True(PdmRentalTransactions.Register(w, "rental", Ids[0], 200, Save, out _)); Assert.False(PdmRentalTransactions.Register(w, "rental", Ids[1], 200, Save, out _)); Assert.True(PdmRentalTransactions.Register(w, "rental", Ids[0], 202, () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Equal(200, w.Pdm.Rental.Cars[0].Handle); }
        [Fact] public void RegisterSaveFailureKeepsUnspawnedState()
        { var w = Started(); Assert.Throws<Exception>(() => PdmRentalTransactions.Register(w, "rental", Ids[0], 200, () => throw new Exception(), out _)); Assert.Equal(0, w.Pdm.Rental.Cars[0].Handle); }
        [Fact] public void CannotDeliverWithoutBothCargoCheckpointsOrPayBeforeDelivery()
        { var w = Started(); Assert.False(PdmRentalTransactions.Deliver(w, "rental", Save, out _)); Assert.False(PdmRentalTransactions.Settle(w, "rental", true, Save, out _)); Assert.False(PdmRentalTransactions.Return(w, "rental", Save, out _)); PdmRentalTransactions.Register(w, "rental", Ids[0], 200, Save, out _); Assert.False(PdmRentalTransactions.Deliver(w, "rental", Save, out _)); }
        [Fact] public void DamageIsMonotonicAndDeliveryLocksDiscountedPriceWithoutPaying()
        { var w = Registered(); Assert.True(PdmRentalTransactions.Remember(w, "rental", new Dictionary<string, int[]> { [Ids[0]] = new[] { 500, 500 } }, "damage", Save, out _)); Assert.True(PdmRentalTransactions.Remember(w, "rental", new Dictionary<string, int[]> { [Ids[0]] = new[] { 1000, 1000 } }, "later", Save, out _)); Assert.Equal(500, w.Pdm.Stock[0].Body); Assert.True(PdmRentalTransactions.Deliver(w, "rental", Save, out _)); Assert.Equal(19950, w.Pdm.Rental.Amount); Assert.Equal(34000, w.Pdm.Cash); Assert.Equal(0, w.Pdm.Revenue); }
        [Fact] public void DeadEngineCannotBeAcceptedAndDamageSaveFailureRestoresCondition()
        { var w = Registered(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmRentalTransactions.Remember(w, "rental", new Dictionary<string, int[]> { [Ids[0]] = new[] { 0, 0 } }, "damage", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); w.Pdm.Stock[0].Engine = 0; Assert.False(PdmRentalTransactions.Deliver(w, "rental", Save, out _)); }
        [Fact] public void AcceptedPriceCannotIncreaseWithChangedDefaultsOrFurtherStockChanges()
        { var w = Registered(); PdmRentalTransactions.Remember(w, "rental", new Dictionary<string, int[]> { [Ids[0]] = new[] { 500, 500 } }, "damage", Save, out _); PdmRentalTransactions.Deliver(w, "rental", Save, out _); var c = C; c.PricePercent = 120; w.Pdm.Stock[0].Body = 1000; Assert.False(PdmRentalTransactions.Remember(w, "rental", new Dictionary<string, int[]>(), "after", Save, out _)); Assert.Equal(19950, w.Pdm.Rental.Amount); PdmRentalTransactions.Return(w, "rental", Save, out _); PdmRentalTransactions.Settle(w, "rental", false, Save, out _); Assert.Equal(19950, w.Pdm.Revenue); }
        [Fact] public void NormalPaymentRequiresReturnCheckpoint()
        { var w = Delivered(); Assert.False(PdmRentalTransactions.Settle(w, "rental", false, Save, out _)); Assert.Equal(2, w.Pdm.Stock.Count); Assert.Equal(0, w.Pdm.Revenue); Assert.True(PdmRentalTransactions.Return(w, "rental", Save, out _)); Assert.True(PdmRentalTransactions.Settle(w, "rental", false, Save, out _)); }
        [Fact] public void CompletedOrderPaysCompanyAndRemovesExactlyTwoStockUnitsAtomically()
        { var w = Stock(); PdmTransactions.Buy(w, PdmDefaults.Load(), "asea", "CC333stock", "third", Save, out _); Begin(w); PdmRentalTransactions.Register(w, "rental", Ids[0], 200, Save, out _); PdmRentalTransactions.Register(w, "rental", Ids[1], 201, Save, out _); PdmRentalTransactions.Deliver(w, "rental", Save, out _); PdmRentalTransactions.Return(w, "rental", Save, out _); int saves = 0; Assert.True(PdmRentalTransactions.Settle(w, "rental", false, () => { saves++; Assert.Null(w.Pdm.Rental); Assert.Single(w.Pdm.Stock); Assert.Equal(47280, w.Pdm.Cash); Assert.Equal(21280, w.Pdm.Revenue); Assert.Equal(16000, w.Pdm.SoldCost); }, out _)); Assert.Equal(1, saves); Assert.Equal("CC333stock", w.Pdm.Stock[0].Id); Assert.Equal(2, w.Pdm.Sold); Assert.Equal(25000, w.Bank); Assert.Equal(55, w.Cash); }
        [Fact] public void FailedDeliveryReturnAndSettlementRestoreTheirPriorCheckpoint()
        { var w = Registered(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmRentalTransactions.Deliver(w, "rental", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); PdmRentalTransactions.Deliver(w, "rental", Save, out _); before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmRentalTransactions.Return(w, "rental", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); PdmRentalTransactions.Return(w, "rental", Save, out _); before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmRentalTransactions.Settle(w, "rental", false, () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void SettlementReplayAfterReloadCannotPayOrRemoveAnotherCar()
        { var w = Returned(); PdmRentalTransactions.Settle(w, "rental", false, Save, out _); w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); string before = JsonConvert.SerializeObject(w); Assert.True(PdmRentalTransactions.Settle(w, "rental", false, () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Equal(before, JsonConvert.SerializeObject(w)); Assert.Equal(55280, w.Pdm.Cash); }
        [Fact] public void BeforeDeliveryCancelKeepsPaidStockWithDamageAndNoReward()
        { var w = Registered(); PdmRentalTransactions.Remember(w, "rental", new Dictionary<string, int[]> { [Ids[0]] = new[] { 200, 400 } }, "damage", Save, out _); Assert.True(PdmRentalTransactions.Cancel(w, "rental", Save, out _)); Assert.Null(w.Pdm.Rental); Assert.Equal(2, w.Pdm.Stock.Count); Assert.Equal(200, w.Pdm.Stock[0].Engine); Assert.Equal(34000, w.Pdm.Cash); Assert.Equal(0, w.Pdm.Sold); }
        [Fact] public void AcceptedGoodsCannotBeCancelledBackToUsableStockAndWorldRecoverySettlesOnce()
        { var w = Delivered(); w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); Assert.False(PdmRentalTransactions.Cancel(w, "rental", Save, out _)); Assert.True(PdmRentalTransactions.Settle(w, "rental", true, Save, out _)); Assert.Empty(w.Pdm.Stock); Assert.Equal(55280, w.Pdm.Cash); Assert.True(PdmRentalTransactions.Settle(w, "rental", true, () => throw new Exception(), out bool changed)); Assert.False(changed); }
        [Fact] public void CashLimitKeepsOrderPendingAndAvailableBalanceWithdrawalAllowsSettlement()
        { var w = Returned(); w.Pdm.Cash = int.MaxValue - 21279; Assert.False(PdmRentalTransactions.Settle(w, "rental", false, Save, out _)); Assert.NotNull(w.Pdm.Rental); Assert.True(PdmTransactions.Transfer(w, 10000, false, "withdraw", Save, out _)); Assert.True(PdmRentalTransactions.Settle(w, "rental", false, Save, out _)); Assert.Equal(int.MaxValue - 9999, w.Pdm.Cash); Assert.Equal(35000, w.Bank); }
        [Fact] public void RevenueAndSoldCostLimitsDoNotLoseAcceptedGoods()
        { var w = Returned(); w.Pdm.Revenue = long.MaxValue - 21279; Assert.False(PdmRentalTransactions.Settle(w, "rental", false, Save, out _)); w.Pdm.Revenue = 0; w.Pdm.SoldCost = long.MaxValue - 15999; Assert.False(PdmRentalTransactions.Settle(w, "rental", false, Save, out _)); Assert.NotNull(w.Pdm.Rental); Assert.Equal(2, w.Pdm.Stock.Count); }
        [Fact] public void ReservedStockCannotBeSoldRepairedOrUsedForAnotherBusinessOperation()
        { var w = Started(); w.Pdm.Stock[0].Body = 500; Assert.False(PdmTransactions.Begin(w, PdmDefaults.Load(), Ids[0], "sale", 0, 999, Save, out _)); Assert.False(PdmTransactions.Repair(w, PdmDefaults.Load(), Ids[0], "repair", Save, out _)); Assert.False(PdmTransactions.Buy(w, PdmDefaults.Load(), "asea", "other", "buy", Save, out _)); Assert.False(PdmStaffTransactions.Hire(w, PdmStaffDefaults.Load(), "junior", "j", Save, out _)); Assert.False(PdmStaffTransactions.Upgrade(w, PdmStaffDefaults.Load(), "workshop", "u", Save, out _)); }
        [Fact] public void WrongIdCannotRegisterDeliverReturnPayOrCancel()
        { var w = Registered(); Assert.False(PdmRentalTransactions.Register(w, "wrong", Ids[0], 999, Save, out _)); Assert.False(PdmRentalTransactions.Deliver(w, "wrong", Save, out _)); Assert.False(PdmRentalTransactions.Return(w, "wrong", Save, out _)); Assert.False(PdmRentalTransactions.Settle(w, "wrong", true, Save, out _)); Assert.False(PdmRentalTransactions.Cancel(w, "wrong", Save, out _)); }
        [Fact] public void UnknownCompanyStockAndOperationFieldsSurviveDeliveryAndSale()
        { var w = Registered(); var j = JObject.Parse(JsonConvert.SerializeObject(w)); j["Pdm"]["Future"] = 1; j["Pdm"]["Stock"][0]["Future"] = 2; j["Pdm"]["Rental"]["Future"] = 3; w = j.ToObject<WalletData>(); PdmRentalTransactions.Deliver(w, "rental", Save, out _); j = JObject.Parse(JsonConvert.SerializeObject(w)); Assert.Equal(2, (int)j["Pdm"]["Stock"][0]["Future"]); Assert.Equal(3, (int)j["Pdm"]["Rental"]["Future"]); PdmRentalTransactions.Return(w, "rental", Save, out _); PdmRentalTransactions.Settle(w, "rental", false, Save, out _); j = JObject.Parse(JsonConvert.SerializeObject(w)); Assert.Equal(1, (int)j["Pdm"]["Future"]); }
        static PdmRentalRun Driving()
        { var r = new PdmRentalRun("rental", 5, 3); r.Couple(true); r.Load(true, () => true); r.Load(true, () => true); return r; }
        static PdmRentalRun Unloading()
        { var r = Driving(); Assert.True(r.Call(true)); Assert.True(r.Unload(100, true)); return r; }
        [Fact] public void MachineRequiresTwoAttachmentsAndStoppedHornCallBeforeUnloading()
        { var r = new PdmRentalRun("rental", 5, 3); Assert.False(r.Load(true, () => throw new Exception())); r.Couple(true); Assert.False(r.Load(true, () => false)); r.Load(true, () => true); Assert.False(r.Call(true)); r.Load(true, () => true); Assert.False(r.Call(false)); Assert.False(r.Unload(0, true)); Assert.True(r.Call(true)); Assert.True(r.Unload(100, true)); }
        [Fact] public void UnloadingWaitsFiveSecondsAndInterruptsOnInvalidPositionOrInput()
        { var r = Unloading(); Assert.False(r.Deliver(5099, true, () => throw new Exception())); Assert.False(r.Deliver(5100, false, () => throw new Exception())); Assert.Equal(PdmRentalStage.Driving, r.Stage); }
        [Fact] public void DeliveryRetriesSaveAndCannotReturnUntilCargoCleanup()
        { var r = Unloading(); Assert.False(r.Deliver(5100, true, () => false)); Assert.True(r.Deliver(5200, true, () => true)); Assert.False(r.Cancel(() => throw new Exception())); Assert.False(r.BeginReturn(false)); Assert.True(r.BeginReturn(true)); Assert.Equal(PdmRentalStage.Returning, r.Stage); }
        [Fact] public void ReturnAndPaymentNeedTheirOwnCheckpointAndEligibleTime()
        { var r = PdmRentalRun.Resume("rental", 5, 3, false); Assert.False(r.Pay(() => throw new Exception())); Assert.True(r.Return(100, true)); Assert.False(r.Returned(3099, true, () => throw new Exception())); Assert.False(r.Returned(3100, false, () => throw new Exception())); Assert.Equal(PdmRentalStage.Returning, r.Stage); r.Return(5000, true); Assert.False(r.Returned(8000, true, () => false)); Assert.True(r.Returned(8100, true, () => true)); Assert.False(r.Pay(() => false)); Assert.True(r.Pay(() => true)); Assert.False(r.Pay(() => throw new Exception())); }
        [Fact] public void ReturnTimerHandlesOverflowAndResumeReturnedOrderOnlyRetriesPayment()
        { var r = PdmRentalRun.Resume("rental", 5, 3, false); int start = int.MaxValue - 100; r.Return(start, true); Assert.True(r.Returned(unchecked(start + 3000), true, () => true)); r = PdmRentalRun.Resume("rental", 5, 3, true); Assert.Equal(PdmRentalStage.Paying, r.Stage); Assert.True(r.Pay(() => true)); }
        [Fact] public void CancellationRequiresSaveAndCannotResumeLoadingAfterward()
        { var r = Driving(); Assert.False(r.Cancel(() => false)); Assert.True(r.Cancel(() => true)); Assert.False(r.Call(true)); Assert.False(r.Cancel(() => throw new Exception())); }
    }
}
