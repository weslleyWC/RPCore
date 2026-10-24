using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace RPCore.Tests
{
    public class PdmLogisticsTests
    {
        static readonly Action Save = () => { };
        static PdmOptions Options => PdmDefaults.Load();
        static WalletData Pending(int count = 2)
        {
            var w = new WalletData { Bank = 100000, Cash = 40 };
            Assert.True(PdmTransactions.Purchase(w, Options, "own", Save, out _));
            Assert.True(PdmTransactions.Transfer(w, 50000, true, "capital", Save, out _));
            for (int i = 0; i < count; i++) Assert.True(PdmTransactions.Buy(w, Options, "asea", "unit" + i, "order" + i, Save, out _, true));
            return w;
        }
        static PdmEntityRef Ref(int handle) => new PdmEntityRef { Model = handle == 100 ? "packer" : "trflat", Plate = "RPH" + handle, Handle = handle };
        static bool Begin(WalletData w, Action save = null) => PdmCollectionTransactions.Begin(w, "haul", w.Pdm.Stock.Select(s => s.Id).ToList(), Ref(100), Ref(200), save ?? Save, out _);
        static WalletData Started()
        { var w = Pending(); Assert.True(Begin(w)); return w; }
        static WalletData Registered()
        { var w = Started(); Assert.True(PdmCollectionTransactions.Register(w, "haul", "unit0", 300, Save, out _)); Assert.True(PdmCollectionTransactions.Register(w, "haul", "unit1", 400, Save, out _)); return w; }
        static WalletData Arrived()
        { var w = Registered(); Assert.True(PdmCollectionTransactions.Arrive(w, "haul", Save, out _)); return w; }
        [Fact] public void DefaultsUseReferencePackerTrflatTwoSlotsAndPort()
        { var c = PdmLogisticsDefaults.Load(); Assert.True(c.Valid); Assert.Equal("packer", c.TruckModel); Assert.Equal("trflat", c.TrailerModel); Assert.Equal(1280.554f, c.Dock.X); Assert.Equal(-48.12541f, c.Return.X); Assert.Equal(-50.69996f, c.Showroom.X); Assert.Equal(3, c.Slots[0].Y); Assert.Equal(-3, c.Slots[1].Y); Assert.Equal(1.055f, c.Slots[0].Z); }
        [Theory] [InlineData(0)] [InlineData(1)] [InlineData(3)]
        public void InvalidSlotCountRejected(int count)
        { var c = PdmLogisticsDefaults.Load(); c.Slots = Enumerable.Range(0, count).Select(i => new PdmLoadSlot { Y = i * 6, Z = 1 }).ToList(); Assert.False(c.Valid); }
        [Theory] [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)] [InlineData(-1f)] [InlineData(10f)]
        public void InvalidSlotHeightRejected(float z)
        { var c = PdmLogisticsDefaults.Load(); c.Slots[0].Z = z; Assert.False(c.Valid); }
        [Fact] public void OverlappingSlotsRejected()
        { var c = PdmLogisticsDefaults.Load(); c.Slots[1].Y = c.Slots[0].Y; Assert.False(c.Valid); }
        [Fact] public void OldStockAndInterruptedCustomerDeliveryRemainReady()
        { var w = Pending(1); var j = JObject.Parse(JsonConvert.SerializeObject(w)); j["Pdm"]["Stock"][0]["AwaitingCollection"].Parent.Remove(); j["Pdm"]["Collection"].Parent.Remove(); w = j.ToObject<WalletData>(); Assert.False(w.Pdm.Stock[0].AwaitingCollection); Assert.True(PdmTransactions.Begin(w, Options, "unit0", "old", 0, 987, Save, out _)); Assert.True(w.Pdm.Valid); Assert.False(Begin(w)); Assert.Equal(987, w.Pdm.Active.VehicleHandle); }
        [Fact] public void NewOrdersPaidOnceAndCannotBeSoldBeforeCollection()
        { var w = Pending(); Assert.All(w.Pdm.Stock, s => Assert.True(s.AwaitingCollection)); Assert.Equal(34000, w.Pdm.Cash); Assert.Equal(25000, w.Bank); Assert.False(PdmTransactions.Begin(w, Options, "unit0", "sale", 0, 999, Save, out _)); Assert.False(PdmTransactions.Sell(w, "sale", 1000, 1000, Save, out _)); Assert.True(PdmTransactions.Buy(w, Options, "asea", "unit0", "order0", () => throw new Exception(), out bool changed, true)); Assert.False(changed); Assert.Equal(34000, w.Pdm.Cash); }
        [Fact] public void FailedOrderSaveRestoresBothBalancesAndStock()
        { var w = Pending(1); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmTransactions.Buy(w, Options, "asea", "extra", "extra", () => throw new Exception(), out _, true)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Theory] [InlineData(1)] [InlineData(2)]
        public void CollectionBeginsWithoutAnotherCharge(int count)
        { var w = Pending(count); string money = $"{w.Bank}/{w.Cash}/{w.Pdm.Cash}/{w.Pdm.StockSpent}"; Assert.True(Begin(w)); Assert.Equal(count, w.Pdm.Collection.Cargo.Count); Assert.True(w.Pdm.Valid); Assert.Equal(money, $"{w.Bank}/{w.Cash}/{w.Pdm.Cash}/{w.Pdm.StockSpent}"); }
        [Theory] [InlineData(0)] [InlineData(3)]
        public void CannotCollectZeroOrThreeOrders(int count)
        { var w = Pending(); Assert.False(PdmCollectionTransactions.Begin(w, "haul", Enumerable.Range(0, count).Select(i => "unit" + i).ToList(), Ref(100), Ref(200), Save, out _)); Assert.Null(w.Pdm.Collection); }
        [Fact] public void CannotCollectMissingDuplicateOrReadyOrder()
        { var w = Pending(); foreach (var ids in new[] { new List<string> { "unit0", "unit0" }, new List<string> { "missing" } }) Assert.False(PdmCollectionTransactions.Begin(w, "haul", ids, Ref(100), Ref(200), Save, out _)); w.Pdm.Stock[0].AwaitingCollection = false; Assert.False(Begin(w)); }
        [Theory] [InlineData(-1, 200)] [InlineData(0, 200)] [InlineData(100, 100)]
        public void InvalidRigIdentityRejected(int truck, int trailer)
        { var w = Pending(); Assert.False(PdmCollectionTransactions.Begin(w, "haul", new List<string> { "unit0" }, Ref(truck), Ref(trailer), Save, out _)); }
        [Fact] public void FailedBeginSaveRollsBackEntireOperation()
        { var w = Pending(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => Begin(w, () => throw new Exception())); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void CannotBuyRepairOrDeliverDuringCollection()
        { var w = Started(); w.Pdm.Stock[0].Body = 500; Assert.False(PdmTransactions.Buy(w, Options, "asea", "third", "third", Save, out _, true)); Assert.False(PdmTransactions.Repair(w, Options, "unit0", "repair", Save, out _)); Assert.False(PdmTransactions.Begin(w, Options, "unit0", "sale", 0, 999, Save, out _)); }
        [Fact] public void RegisterSavesExactAssetAndReplayCannotReplaceHandle()
        { var w = Started(); Assert.True(PdmCollectionTransactions.Register(w, "haul", "unit0", 300, Save, out bool changed)); Assert.True(changed); Assert.True(PdmCollectionTransactions.Register(w, "haul", "unit0", 301, () => throw new Exception(), out changed)); Assert.False(changed); Assert.Equal(300, w.Pdm.Collection.Cargo[0].Handle); }
        [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(100)] [InlineData(200)]
        public void RegisterRejectsInvalidOrRigHandle(int handle)
        { var w = Started(); Assert.False(PdmCollectionTransactions.Register(w, "haul", "unit0", handle, Save, out _)); }
        [Fact] public void RegisterRejectsOtherOperationMissingStockAndDuplicateHandle()
        { var w = Started(); Assert.False(PdmCollectionTransactions.Register(w, "wrong", "unit0", 300, Save, out _)); Assert.False(PdmCollectionTransactions.Register(w, "haul", "unknown", 300, Save, out _)); Assert.True(PdmCollectionTransactions.Register(w, "haul", "unit0", 300, Save, out _)); Assert.False(PdmCollectionTransactions.Register(w, "haul", "unit1", 300, Save, out _)); }
        [Fact] public void FailedRegisterSaveRetainsUnspawnedCheckpoint()
        { var w = Started(); Assert.Throws<Exception>(() => PdmCollectionTransactions.Register(w, "haul", "unit0", 300, () => throw new Exception(), out _)); Assert.Equal(0, w.Pdm.Collection.Cargo[0].Handle); }
        [Fact] public void DamageUsesWorstSavedConditionAndSurvivesReload()
        { var w = Registered(); Assert.True(PdmCollectionTransactions.Remember(w, "haul", new Dictionary<string, int[]> { ["unit0"] = new[] { 500, 400 } }, "damage1", Save, out _)); w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); Assert.True(PdmCollectionTransactions.Remember(w, "haul", new Dictionary<string, int[]> { ["unit0"] = new[] { 1000, 1000 } }, "damage2", Save, out _)); Assert.Equal(400, w.Pdm.Stock[0].Body); Assert.Equal(500, w.Pdm.Stock[0].Engine); }
        [Theory] [InlineData(-1)] [InlineData(1001)]
        public void InvalidDamageRejected(int health)
        { var w = Registered(); Assert.False(PdmCollectionTransactions.Remember(w, "haul", new Dictionary<string, int[]> { ["unit0"] = new[] { health, 500 } }, "damage", Save, out _)); Assert.Equal(1000, w.Pdm.Stock[0].Engine); }
        [Fact] public void FailedDamageSaveRestoresStock()
        { var w = Registered(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmCollectionTransactions.Remember(w, "haul", new Dictionary<string, int[]> { ["unit0"] = new[] { 10, 20 } }, "damage", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void ArrivalRequiresEveryCargoIdentityAndCannotCompleteEarly()
        { var w = Started(); Assert.False(PdmCollectionTransactions.Arrive(w, "haul", Save, out _)); Assert.False(PdmCollectionTransactions.Complete(w, "haul", Save, out _)); PdmCollectionTransactions.Register(w, "haul", "unit0", 300, Save, out _); Assert.False(PdmCollectionTransactions.Arrive(w, "haul", Save, out _)); Assert.All(w.Pdm.Stock, s => Assert.True(s.AwaitingCollection)); }
        [Fact] public void ArrivalCheckpointSurvivesReloadAndCannotBeCancelled()
        { var w = Arrived(); w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); Assert.True(w.Pdm.Collection.Arrived); Assert.False(PdmCollectionTransactions.Cancel(w, "haul", Save, out _)); Assert.True(PdmCollectionTransactions.Complete(w, "haul", Save, out _)); Assert.All(w.Pdm.Stock, s => Assert.False(s.AwaitingCollection)); }
        [Fact] public void ReceiveMakesBothOrdersReadyAtomicallyWithoutRevenueOrCash()
        { var w = Arrived(); var before = new[] { w.Bank, w.Cash, w.Pdm.Cash, w.Pdm.Sold }; int calls = 0; Assert.True(PdmCollectionTransactions.Complete(w, "haul", () => { calls++; Assert.Null(w.Pdm.Collection); Assert.All(w.Pdm.Stock, s => Assert.False(s.AwaitingCollection)); }, out _)); Assert.Equal(1, calls); Assert.Equal(before, new[] { w.Bank, w.Cash, w.Pdm.Cash, w.Pdm.Sold }); Assert.Equal(0, w.Pdm.Revenue); Assert.True(PdmTransactions.Begin(w, Options, "unit0", "delivery", 0, 999, Save, out _)); }
        [Fact] public void ReceiveFailureRestoresPendingFlagsAndArrivalForRetry()
        { var w = Arrived(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmCollectionTransactions.Complete(w, "haul", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); Assert.True(PdmCollectionTransactions.Complete(w, "haul", Save, out _)); }
        [Fact] public void ArrivalFailureDoesNotConfirmReceipt()
        { var w = Registered(); Assert.Throws<Exception>(() => PdmCollectionTransactions.Arrive(w, "haul", () => throw new Exception(), out _)); Assert.False(w.Pdm.Collection.Arrived); Assert.All(w.Pdm.Stock, s => Assert.True(s.AwaitingCollection)); }
        [Fact] public void ReceiveReplayAfterReloadCannotChangeStockOrCredit()
        { var w = Arrived(); PdmCollectionTransactions.Complete(w, "haul", Save, out _); w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); string before = JsonConvert.SerializeObject(w); Assert.True(PdmCollectionTransactions.Complete(w, "haul", () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void InterruptedUnarrivedCollectionKeepsPaidPendingOrdersAndDamage()
        { var w = Registered(); w.Pdm.Stock[0].Body = 400; Assert.True(PdmCollectionTransactions.Cancel(w, "haul", Save, out _)); Assert.Null(w.Pdm.Collection); Assert.Equal(2, w.Pdm.Stock.Count); Assert.All(w.Pdm.Stock, s => Assert.True(s.AwaitingCollection)); Assert.Equal(400, w.Pdm.Stock[0].Body); Assert.Equal(34000, w.Pdm.Cash); Assert.Equal(0, w.Pdm.Revenue); }
        [Fact] public void CancellationFailureRetainsOperationForRetry()
        { var w = Registered(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmCollectionTransactions.Cancel(w, "haul", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void CollectOneDoesNotReceiveOtherPendingOrder()
        { var w = Pending(); Assert.True(PdmCollectionTransactions.Begin(w, "haul", new List<string> { "unit0" }, Ref(100), Ref(200), Save, out _)); PdmCollectionTransactions.Register(w, "haul", "unit0", 300, Save, out _); PdmCollectionTransactions.Arrive(w, "haul", Save, out _); PdmCollectionTransactions.Complete(w, "haul", Save, out _); Assert.False(w.Pdm.Stock[0].AwaitingCollection); Assert.True(w.Pdm.Stock[1].AwaitingCollection); }
        [Fact] public void UnknownWalletCompanyStockAndCollectionFieldsSurviveCheckpoints()
        { var w = Registered(); var j = JObject.Parse(JsonConvert.SerializeObject(w)); j["Future"] = 12; j["Pdm"]["Future"] = 13; j["Pdm"]["Stock"][0]["Future"] = 14; j["Pdm"]["Collection"]["Future"] = 15; w = j.ToObject<WalletData>(); PdmCollectionTransactions.Arrive(w, "haul", Save, out _); j = JObject.Parse(JsonConvert.SerializeObject(w)); Assert.Equal(12, (int)j["Future"]); Assert.Equal(13, (int)j["Pdm"]["Future"]); Assert.Equal(14, (int)j["Pdm"]["Stock"][0]["Future"]); Assert.Equal(15, (int)j["Pdm"]["Collection"]["Future"]); }
        [Fact] public void HandEditedInvalidCollectionCannotMutateWallet()
        { var w = Registered(); w.Pdm.Collection.Truck.Handle = w.Pdm.Collection.Trailer.Handle; Assert.False(w.Pdm.Valid); Assert.False(PdmCollectionTransactions.Complete(w, "haul", Save, out _)); }
        static PdmHaulRun Returning()
        { var run = new PdmHaulRun("haul", 2, 5); run.Coupled(true); Assert.True(run.Load(true, () => true)); Assert.True(run.Load(true, () => true)); return run; }
        [Fact] public void CannotLoadBeforeCouplingOrWhenAttachmentFails()
        { var run = new PdmHaulRun("haul", 2, 5); Assert.False(run.Load(true, () => throw new Exception())); run.Coupled(false); Assert.Equal(PdmHaulStage.Coupling, run.Stage); run.Coupled(true); Assert.False(run.Load(false, () => throw new Exception())); Assert.False(run.Load(true, () => false)); Assert.Equal(0, run.Loaded); }
        [Fact] public void CannotUnloadUntilAllCarsLoaded()
        { var run = new PdmHaulRun("haul", 2, 5); run.Coupled(true); run.Load(true, () => true); Assert.False(run.Unload(0, true)); run.Load(true, () => true); Assert.Equal(PdmHaulStage.Returning, run.Stage); Assert.False(run.Load(true, () => throw new Exception())); Assert.Equal(2, run.Loaded); }
        [Fact] public void UnloadNeedsFiveSecondsAndEligiblePosition()
        { var run = Returning(); Assert.False(run.Unload(0, false)); Assert.True(run.Unload(100, true)); Assert.False(run.Arrive(5099, true, () => throw new Exception())); Assert.False(run.Arrive(5100, false, () => throw new Exception())); Assert.Equal(PdmHaulStage.Returning, run.Stage); }
        [Fact] public void OpeningMenuInterruptsTimerAndRequiresNewStart()
        { var run = Returning(); run.Unload(0, true); run.Interrupt(); Assert.False(run.Arrive(10000, true, () => throw new Exception())); Assert.True(run.Unload(10000, true)); Assert.False(run.Arrive(10001, true, () => throw new Exception())); }
        [Fact] public void ArrivalAndReceiptRetryWithoutRepeatingPhysicalProgress()
        { var run = Returning(); run.Unload(0, true); Assert.False(run.Arrive(5000, true, () => false)); Assert.True(run.Arrive(6000, true, () => true)); run.Cancel(); Assert.Equal(PdmHaulStage.Confirming, run.Stage); Assert.False(run.Complete(() => false)); Assert.True(run.Complete(() => true)); Assert.False(run.Complete(() => throw new Exception())); run.Cancel(); Assert.Equal(PdmHaulStage.Complete, run.Stage); }
        [Fact] public void UnloadTimerHandlesGameClockOverflow()
        { var run = Returning(); run.Unload(int.MaxValue - 100, true); Assert.True(run.Arrive(unchecked(int.MaxValue - 100 + 5000), true, () => true)); }
        [Fact] public void CancelledRunCannotResumeOrReceive()
        { var run = Returning(); run.Cancel(); Assert.False(run.Unload(0, true)); Assert.False(run.Complete(() => throw new Exception())); Assert.False(run.Arrive(5000, true, () => throw new Exception())); }
    }
}
