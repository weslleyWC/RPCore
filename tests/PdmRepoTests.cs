using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class PdmRepoTests
    {
        static readonly Action Save = () => { };
        static PdmOptions C => PdmDefaults.Load();
        static PdmRepoOptions R => PdmRepoDefaults.Load();
        static WalletData Company()
        { var w = new WalletData { Bank = 100000, Cash = 25 }; PdmTransactions.Purchase(w, C, "company", Save, out _); PdmTransactions.Transfer(w, 50000, true, "capital", Save, out _); return w; }
        static WalletData Loan(int risk = 100)
        { var w = Company(); PdmTransactions.Buy(w, C, "asea", "asset1", "buy", Save, out _); PdmTransactions.Begin(w, C, "asset1", "loan", 0, 999, Save, out _, true); Assert.True(PdmFinanceTransactions.OpenRisky(w, PdmFinanceDefaults.Load(), "loan", 1000, 1000, risk, Save, out _)); return w; }
        static void Work(WalletData w, int seconds)
        { while (seconds > 0) { int n = Math.Min(60, seconds); Assert.True(PdmFinanceTransactions.Work(w, n, Guid.NewGuid().ToString("N"), Save, out _)); seconds -= n; } }
        static WalletData Defaulted()
        { var w = Loan(); Work(w, 120); Assert.True(w.Pdm.Loans[0].Defaulted); return w; }
        static PdmEntityRef Ref(int handle) => new PdmEntityRef { Handle = handle, Model = handle == 10 ? "packer" : "trflat", Plate = "PR" + handle };
        static PdmRepoOperation Op() => new PdmRepoOperation { Id = "repo", LoanId = "loan", Location = 0, Truck = Ref(10), Trailer = Ref(11) };
        static WalletData Started()
        { var w = Defaulted(); Assert.True(PdmRepoTransactions.Begin(w, C, R, Op(), Save, out _)); return w; }
        static WalletData Registered()
        { var w = Started(); Assert.True(PdmRepoTransactions.Register(w, "repo", 12, Save, out _)); return w; }
        static WalletData Arrived()
        { var w = Registered(); Assert.True(PdmRepoTransactions.Arrive(w, "repo", Save, out _)); return w; }
        [Fact] public void DefaultsContainElevenReferenceNormalLocationsAndTwentyPercentRisk()
        { var r = R; Assert.True(r.Valid); Assert.Equal(20, r.DefaultPercent); Assert.Equal(11, r.Locations.Count); Assert.Equal(-1078.539f, r.Locations[0].X); Assert.Equal(29.11063f, r.Locations[0].Heading); Assert.Equal(-48.12541f, r.Return.X); }
        [Theory] [InlineData(-1)] [InlineData(101)]
        public void InvalidRiskRejected(int percent)
        { var r = R; r.DefaultPercent = percent; Assert.False(r.Valid); var w = Company(); Assert.False(PdmFinanceTransactions.OpenRisky(w, PdmFinanceDefaults.Load(), "loan", 1000, 1000, percent, Save, out _)); }
        [Fact] public void OldLoanMissingRiskAndRepoFieldsRemainsRegular()
        { var w = Loan(0); var j = JObject.Parse(JsonConvert.SerializeObject(w)); j["Pdm"]["Repo"].Parent.Remove(); j["Pdm"]["FinanceWriteOff"].Parent.Remove(); foreach (string key in new[] { "DefaultPercent", "Defaulted", "Repossessed", "DeliveredHandle" }) j["Pdm"]["Loans"][0][key].Parent.Remove(); w = j.ToObject<WalletData>(); Assert.True(w.Pdm.Valid); Assert.Null(w.Pdm.Repo); Assert.Equal(0, w.Pdm.FinanceWriteOff); Work(w, 480); Assert.True(w.Pdm.Loans[0].Complete); Assert.False(w.Pdm.Loans[0].Defaulted); }
        [Fact] public void DefaultOccursAtDueBoundaryAndDoesNotCreditMissedInstallment()
        { var w = Loan(); int cash = w.Pdm.Cash; Work(w, 119); Assert.False(w.Pdm.Loans[0].Defaulted); Work(w, 1); var l = w.Pdm.Loans[0]; Assert.True(l.Defaulted); Assert.False(l.Settled); Assert.False(l.Collecting); Assert.Equal(3080, l.Paid); Assert.Equal(120, l.Elapsed); Assert.Equal(0, l.Payments); Assert.Equal(9240, l.Receivable); Assert.Equal(cash, w.Pdm.Cash); Assert.Equal(3080, w.Pdm.Revenue); }
        [Fact] public void DefaultedBorrowerDoesNotResumeAutomaticPayments()
        { var w = Defaulted(); string before = JsonConvert.SerializeObject(w); Assert.False(PdmFinanceTransactions.Work(w, 60, "later", Save, out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void DeterministicRiskIsStableAcrossReloadAndMatchesThreshold()
        { var w = Loan(20); int roll = PdmFinanceTransactions.Roll("loan", 0); w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); Work(w, 120); Assert.Equal(roll < 20, w.Pdm.Loans[0].Defaulted); Assert.Equal(roll, PdmFinanceTransactions.Roll("loan", 0)); Assert.InRange(roll, 0, 99); }
        [Fact] public void FailedDefaultSaveRestoresLoanTimerThenRetryUsesSameOutcome()
        { var w = Loan(); Work(w, 119); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmFinanceTransactions.Work(w, 1, "due", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); Assert.True(PdmFinanceTransactions.Work(w, 1, "due", Save, out _)); Assert.True(w.Pdm.Loans[0].Defaulted); Assert.True(PdmFinanceTransactions.Work(w, 1, "due", () => throw new Exception(), out bool changed)); Assert.False(changed); }
        [Fact] public void RepossessionRequiresDefaultOwnershipAndStockCapacity()
        { var w = Loan(0); Assert.False(PdmRepoTransactions.Begin(w, C, R, Op(), Save, out _)); w = Defaulted(); w.Pdm.Owned = false; Assert.False(PdmRepoTransactions.Begin(w, C, R, Op(), Save, out _)); w.Pdm.Owned = true; var c = C; c.Capacity = 1; PdmTransactions.Buy(w, c, "asea", "other", "b", Save, out _); Assert.False(PdmRepoTransactions.Begin(w, c, R, Op(), Save, out _)); }
        [Fact] public void BeginReservesOnlyOperationWithoutInventingStockOrMoney()
        { var w = Defaulted(); int cash = w.Pdm.Cash; Assert.True(PdmRepoTransactions.Begin(w, C, R, Op(), Save, out _)); Assert.Equal(cash, w.Pdm.Cash); Assert.Equal(25000, w.Bank); Assert.Equal(25, w.Cash); Assert.Empty(w.Pdm.Stock); Assert.Equal(0, w.Pdm.Repo.VehicleHandle); Assert.True(w.Pdm.Valid); }
        [Fact] public void CallerCannotMutateSavedRigIdentity()
        { var w = Defaulted(); var op = Op(); PdmRepoTransactions.Begin(w, C, R, op, Save, out _); op.Truck.Plate = "OTHER"; Assert.Equal("PR10", w.Pdm.Repo.Truck.Plate); }
        [Fact] public void FailedBeginRestoresNoActiveOperationAndDebt()
        { var w = Defaulted(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmRepoTransactions.Begin(w, C, R, Op(), () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(10)] [InlineData(11)]
        public void RegisterRejectsInvalidOrRigHandle(int handle)
        { var w = Started(); Assert.False(PdmRepoTransactions.Register(w, "repo", handle, Save, out _)); }
        [Fact] public void CargoIdentitySavedOnceAndReplayCannotReplaceIt()
        { var w = Registered(); Assert.Equal(12, w.Pdm.Repo.VehicleHandle); Assert.True(PdmRepoTransactions.Register(w, "repo", 13, () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Equal(12, w.Pdm.Repo.VehicleHandle); }
        [Fact] public void FailedCargoSaveRetainsUnspawnedCheckpoint()
        { var w = Started(); Assert.Throws<Exception>(() => PdmRepoTransactions.Register(w, "repo", 12, () => throw new Exception(), out _)); Assert.Equal(0, w.Pdm.Repo.VehicleHandle); }
        [Fact] public void ArrivalRequiresRegisteredCargoAndNoEarlyReturnToStock()
        { var w = Started(); Assert.False(PdmRepoTransactions.Arrive(w, "repo", Save, out _)); Assert.False(PdmRepoTransactions.Complete(w, C, "repo", Save, out _)); Assert.Empty(w.Pdm.Stock); Assert.False(w.Pdm.Loans[0].Repossessed); }
        [Fact] public void RememberDamageIsMonotonicAndDoesNotEraseDebt()
        { var w = Registered(); Assert.True(PdmRepoTransactions.Remember(w, "repo", 200, 400, "damage", Save, out _)); Assert.True(PdmRepoTransactions.Remember(w, "repo", 1000, 1000, "later", Save, out _)); var l = w.Pdm.Loans[0]; Assert.Equal(200, l.Asset.Engine); Assert.Equal(400, l.Asset.Body); Assert.True(l.Defaulted); Assert.Equal(9240, l.Receivable); }
        [Theory] [InlineData(-1)] [InlineData(1001)]
        public void InvalidDamageCannotModifyAsset(int engine)
        { var w = Registered(); Assert.False(PdmRepoTransactions.Remember(w, "repo", engine, 1000, "bad", Save, out _)); Assert.Equal(1000, w.Pdm.Loans[0].Asset.Engine); }
        [Fact] public void DamageSaveFailureRestoresOriginalAssetCondition()
        { var w = Registered(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmRepoTransactions.Remember(w, "repo", 0, 0, "damage", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void ReturnRestoresOriginalInventoryBasisAndClosesReceivableWithoutCash()
        { var w = Arrived(); int cash = w.Pdm.Cash; long revenue = w.Pdm.Revenue; Assert.True(PdmRepoTransactions.Complete(w, C, "repo", Save, out _)); var l = w.Pdm.Loans[0]; Assert.True(l.Settled); Assert.True(l.Repossessed); Assert.False(l.Complete); Assert.Equal(0, l.Receivable); Assert.Equal(3080, l.Paid); Assert.Equal(9240, w.Pdm.FinanceWriteOff); Assert.Single(w.Pdm.Stock); Assert.Equal("asset1", w.Pdm.Stock[0].Id); Assert.Equal(8000, w.Pdm.Stock[0].Cost); Assert.Equal(0, w.Pdm.SoldCost); Assert.Equal(cash, w.Pdm.Cash); Assert.Equal(revenue, w.Pdm.Revenue); Assert.Null(w.Pdm.Repo); Assert.Equal(1, w.Pdm.Sold); }
        [Fact] public void ReturnedDamagedCarStillRequiresStockRepair()
        { var w = Registered(); PdmRepoTransactions.Remember(w, "repo", 0, 100, "damage", Save, out _); PdmRepoTransactions.Arrive(w, "repo", Save, out _); PdmRepoTransactions.Complete(w, C, "repo", Save, out _); Assert.Equal(0, w.Pdm.Stock[0].Engine); Assert.False(PdmTransactions.Begin(w, C, "asset1", "sale", 0, 99, Save, out _)); Assert.True(PdmTransactions.Repair(w, C, "asset1", "repair", Save, out _)); Assert.True(PdmTransactions.Begin(w, C, "asset1", "sale", 0, 99, Save, out _)); }
        [Fact] public void ResaleAfterRecoveryCountsOriginalCostOnceAndRetainsReceivedEntry()
        { var w = Arrived(); PdmRepoTransactions.Complete(w, C, "repo", Save, out _); PdmTransactions.Begin(w, C, "asset1", "sale", 0, 99, Save, out _); Assert.True(PdmTransactions.Sell(w, "sale", 1000, 1000, Save, out _)); Assert.Equal(14280, w.Pdm.Revenue); Assert.Equal(8000, w.Pdm.SoldCost); Assert.Equal(6280, w.Pdm.Revenue - w.Pdm.SoldCost); Assert.Equal(2, w.Pdm.Sold); Assert.Equal(8000, w.Pdm.StockSpent); }
        [Fact] public void FailedCompletionRestoresStockCounterDebtAndOperation()
        { var w = Arrived(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmRepoTransactions.Complete(w, C, "repo", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void CompletionReplayAfterReloadCannotReturnTwoCarsOrWriteOffTwice()
        { var w = Arrived(); PdmRepoTransactions.Complete(w, C, "repo", Save, out _); w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); string before = JsonConvert.SerializeObject(w); Assert.True(PdmRepoTransactions.Complete(w, C, "repo", () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Equal(before, JsonConvert.SerializeObject(w)); Assert.False(PdmRepoTransactions.Cancel(w, "repo", Save, out _)); }
        [Fact] public void CancelBeforeArrivalPreservesDefaultAndDoesNotReturnUnrecoveredCar()
        { var w = Registered(); Assert.True(PdmRepoTransactions.Cancel(w, "repo", Save, out _)); Assert.Null(w.Pdm.Repo); Assert.Empty(w.Pdm.Stock); Assert.True(w.Pdm.Loans[0].Defaulted); Assert.False(w.Pdm.Loans[0].Settled); Assert.Equal(8000, w.Pdm.SoldCost); Assert.Equal(0, w.Pdm.FinanceWriteOff); }
        [Fact] public void ConfirmedArrivalCannotBeCancelledAndRecoversAsStockAfterReload()
        { var w = Arrived(); w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); Assert.False(PdmRepoTransactions.Cancel(w, "repo", Save, out _)); Assert.True(PdmRepoTransactions.Complete(w, C, "repo", Save, out _)); Assert.Single(w.Pdm.Stock); }
        [Fact] public void FailedArrivalAndCancelKeepTheirRespectivePreviousState()
        { var w = Registered(); Assert.Throws<Exception>(() => PdmRepoTransactions.Arrive(w, "repo", () => throw new Exception(), out _)); Assert.False(w.Pdm.Repo.Arrived); Assert.Throws<Exception>(() => PdmRepoTransactions.Cancel(w, "repo", () => throw new Exception(), out _)); Assert.NotNull(w.Pdm.Repo); }
        [Fact] public void WrongOperationCannotRegisterRememberArriveCompleteOrCancel()
        { var w = Registered(); Assert.False(PdmRepoTransactions.Register(w, "wrong", 13, Save, out _)); Assert.False(PdmRepoTransactions.Remember(w, "wrong", 100, 100, "x", Save, out _)); Assert.False(PdmRepoTransactions.Arrive(w, "wrong", Save, out _)); Assert.False(PdmRepoTransactions.Complete(w, C, "wrong", Save, out _)); Assert.False(PdmRepoTransactions.Cancel(w, "wrong", Save, out _)); }
        [Fact] public void BlockedCapacityOrAccountingOverflowPreservesArrivalForRetry()
        { var w = Arrived(); w.Pdm.FinanceWriteOff = long.MaxValue - 9240 + 1; Assert.False(PdmRepoTransactions.Complete(w, C, "repo", Save, out _)); Assert.NotNull(w.Pdm.Repo); w.Pdm.FinanceWriteOff = 0; w.Pdm.SoldCost = 7999; Assert.False(PdmRepoTransactions.Complete(w, C, "repo", Save, out _)); Assert.Empty(w.Pdm.Stock); }
        [Fact] public void RepossessionBlocksConflictingBusinessActions()
        { var w = Started(); Assert.False(PdmTransactions.Buy(w, C, "asea", "other", "b", Save, out _)); Assert.False(PdmStaffTransactions.Hire(w, PdmStaffDefaults.Load(), "junior", "j", Save, out _)); Assert.False(PdmStaffTransactions.Upgrade(w, PdmStaffDefaults.Load(), "workshop", "u", Save, out _)); Assert.False(PdmRepoTransactions.Begin(w, C, R, OpWithNewId(), Save, out _)); Assert.Equal("repo", w.Pdm.Repo.Id); }
        static PdmRepoOperation OpWithNewId() { var op = Op(); op.Id = "second"; return op; }
        [Fact] public void LoweredStockCapacityPreservesConfirmedReturnUntilConfigurationAllowsIt()
        {
            var w = Defaulted(); PdmTransactions.Buy(w, C, "asea", "other", "other-buy", Save, out _); var c = C; c.Capacity = 2;
            Assert.True(PdmRepoTransactions.Begin(w, c, R, Op(), Save, out _)); PdmRepoTransactions.Register(w, "repo", 12, Save, out _); PdmRepoTransactions.Arrive(w, "repo", Save, out _);
            c.Capacity = 1; Assert.False(PdmRepoTransactions.Complete(w, c, "repo", Save, out _)); Assert.True(w.Pdm.Repo.Arrived); Assert.False(w.Pdm.Loans[0].Repossessed);
            c.Capacity = 2; Assert.True(PdmRepoTransactions.Complete(w, c, "repo", Save, out _)); Assert.Equal(2, w.Pdm.Stock.Count);
        }
        [Fact] public void LateDefaultKeepsPriorInstallmentsAndWritesOffOnlyRemainingBalance()
        {
            string id = Enumerable.Range(0, 10000).Select(n => "seed" + n).First(x => Enumerable.Range(0, 3).All(k => PdmFinanceTransactions.Roll(x, k) >= 20) && PdmFinanceTransactions.Roll(x, 3) < 20);
            var w = Company(); PdmTransactions.Buy(w, C, "asea", "asset1", "buy", Save, out _); PdmTransactions.Begin(w, C, "asset1", id, 0, 999, Save, out _, true);
            Assert.True(PdmFinanceTransactions.OpenRisky(w, PdmFinanceDefaults.Load(), id, 1000, 1000, 20, Save, out _)); Work(w, 480);
            Assert.True(w.Pdm.Loans[0].Defaulted); Assert.Equal(10010, w.Pdm.Loans[0].Paid); var op = Op(); op.LoanId = id;
            PdmRepoTransactions.Begin(w, C, R, op, Save, out _); PdmRepoTransactions.Register(w, "repo", 12, Save, out _); PdmRepoTransactions.Arrive(w, "repo", Save, out _);
            int cash = w.Pdm.Cash; Assert.True(PdmRepoTransactions.Complete(w, C, "repo", Save, out _)); Assert.Equal(cash, w.Pdm.Cash); Assert.Equal(2310, w.Pdm.FinanceWriteOff); Assert.Equal(10010, w.Pdm.Revenue); Assert.Equal(0, w.Pdm.SoldCost);
            PdmTransactions.Begin(w, C, "asset1", "resale", 0, 998, Save, out _); PdmTransactions.Sell(w, "resale", 1000, 1000, Save, out _);
            Assert.Equal(13210, w.Pdm.Revenue - w.Pdm.SoldCost); Assert.Equal(63210, w.Pdm.Cash);
        }
        [Fact] public void UnknownFieldsOfLoanAssetAndOperationSurviveReturnToStock()
        { var w = Registered(); var j = JObject.Parse(JsonConvert.SerializeObject(w)); j["Pdm"]["Future"] = 1; j["Pdm"]["Loans"][0]["Future"] = 2; j["Pdm"]["Loans"][0]["Asset"]["Future"] = 3; j["Pdm"]["Repo"]["Future"] = 4; w = j.ToObject<WalletData>(); PdmRepoTransactions.Arrive(w, "repo", Save, out _); j = JObject.Parse(JsonConvert.SerializeObject(w)); Assert.Equal(4, (int)j["Pdm"]["Repo"]["Future"]); PdmRepoTransactions.Complete(w, C, "repo", Save, out _); j = JObject.Parse(JsonConvert.SerializeObject(w)); Assert.Equal(1, (int)j["Pdm"]["Future"]); Assert.Equal(2, (int)j["Pdm"]["Loans"][0]["Future"]); Assert.Equal(3, (int)j["Pdm"]["Stock"][0]["Future"]); }
        [Fact] public void InvalidRigOrLoanStateCannotMutateWallet()
        { var w = Registered(); w.Pdm.Repo.Truck.Handle = w.Pdm.Repo.Trailer.Handle; Assert.False(w.Pdm.Valid); Assert.False(PdmRepoTransactions.Cancel(w, "repo", Save, out _)); }
    }
}
