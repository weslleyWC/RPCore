using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class PdmSafeTests
    {
        static readonly Action Save = () => { };
        static WalletData Company()
        {
            var w = new WalletData { Bank = 100000, Cash = 55 };
            Assert.True(PdmTransactions.Purchase(w, PdmDefaults.Load(), "own", Save, out _));
            Assert.True(PdmTransactions.Transfer(w, 50000, true, "capital", Save, out _)); return w;
        }
        static PdmSafeTransfer Transfer(int amount = 10000, string id = "safe") => new PdmSafeTransfer { Id = id, Amount = amount, CollectorHandle = 201, CollectorModel = "s_m_m_armoured_01", CaseHandle = 202, CaseModel = "prop_security_case_01", Token = 123456, Van = new PdmEntityRef { Handle = 200, Model = "stockade", Plate = "PSF12345" } };
        static WalletData Reserved()
        { var w = Company(); Assert.True(PdmSafeTransactions.Begin(w, Transfer(), Save, out _)); return w; }
        static WalletData Handed()
        { var w = Reserved(); Assert.True(PdmSafeTransactions.Hand(w, "safe", Save, out _)); return w; }
        static long Total(WalletData w) => (long)w.Bank + w.Cash + w.Pdm.Cash + (w.Pdm.SafeTransfer?.Amount ?? 0);
        [Fact] public void DefaultsUseReferenceStockadeSecurityCaseAndFourPoints()
        { var c = PdmSafeDefaults.Load(); Assert.True(c.Valid); Assert.Equal("stockade", c.VehicleModel); Assert.Equal("s_m_m_armoured_01", c.CollectorModel); Assert.Equal("prop_security_case_01", c.CaseModel); Assert.Equal(-53.67139f, c.Spawn.X); Assert.Equal(-77.51719f, c.Parking.X); Assert.Equal(-63.2041f, c.Handover.X); Assert.Equal(1182.303f, c.Bank.X); Assert.Equal(10, c.ClearingSeconds); }
        [Theory] [InlineData(1)] [InlineData(11)]
        public void InvalidHandoverDurationRejected(int n)
        { var c = PdmSafeDefaults.Load(); c.HandoverSeconds = n; Assert.False(c.Valid); }
        [Theory] [InlineData(4)] [InlineData(61)]
        public void InvalidClearingDurationRejected(int n)
        { var c = PdmSafeDefaults.Load(); c.ClearingSeconds = n; Assert.False(c.Valid); }
        [Fact] public void OldCompanyLacksTransferButRetainsCashStaffAndStock()
        { var w = Company(); PdmStaffTransactions.Hire(w, PdmStaffDefaults.Load(), "junior", "j", Save, out _); PdmTransactions.Buy(w, PdmDefaults.Load(), "asea", "unit123", "buy", Save, out _); var j = JObject.Parse(JsonConvert.SerializeObject(w)); j["Pdm"]["SafeTransfer"].Parent.Remove(); w = j.ToObject<WalletData>(); Assert.True(w.Pdm.Valid); Assert.Null(w.Pdm.SafeTransfer); Assert.Equal(41500, w.Pdm.Cash); Assert.Single(w.Pdm.Staff.Employees); Assert.Single(w.Pdm.Stock); }
        [Fact] public void ReserveMovesOnlyAvailableCompanyCashIntoEscrow()
        { var w = Company(); long total = Total(w); Assert.True(PdmSafeTransactions.Begin(w, Transfer(), Save, out bool changed)); Assert.True(changed); Assert.Equal(40000, w.Pdm.Cash); Assert.Equal(10000, w.Pdm.SafeTransfer.Amount); Assert.Equal(25000, w.Bank); Assert.Equal(55, w.Cash); Assert.Equal(0, w.Pdm.Withdrawn); Assert.Equal(total, Total(w)); Assert.True(w.Pdm.Valid); }
        [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(1000001)]
        public void InvalidAmountCannotReserve(int amount)
        { var w = Company(); Assert.False(PdmSafeTransactions.Begin(w, Transfer(amount), Save, out _)); Assert.Equal(50000, w.Pdm.Cash); }
        [Fact] public void InsufficientFundsOwnershipOrExistingTransferRejectWithoutMutation()
        { var w = Company(); Assert.False(PdmSafeTransactions.Begin(w, Transfer(50001), Save, out _)); w.Pdm.Owned = false; Assert.False(PdmSafeTransactions.Begin(w, Transfer(), Save, out _)); w.Pdm.Owned = true; PdmSafeTransactions.Begin(w, Transfer(), Save, out _); Assert.False(PdmSafeTransactions.Begin(w, Transfer(1000, "second"), Save, out _)); Assert.Equal(40000, w.Pdm.Cash); }
        [Fact] public void BeginCopiesCallerIdentityInsteadOfRetainingMutableReference()
        { var w = Company(); var t = Transfer(); Assert.True(PdmSafeTransactions.Begin(w, t, Save, out _)); t.Amount = 1; t.Van.Plate = "OTHER"; Assert.Equal(10000, w.Pdm.SafeTransfer.Amount); Assert.Equal("PSF12345", w.Pdm.SafeTransfer.Van.Plate); }
        [Fact] public void CannotBeginWithPreconfirmedOrCollidingEntityIdentity()
        { var w = Company(); var t = Transfer(); t.Handed = true; Assert.False(PdmSafeTransactions.Begin(w, t, Save, out _)); t.Handed = false; t.CaseHandle = t.Van.Handle; Assert.False(PdmSafeTransactions.Begin(w, t, Save, out _)); t = Transfer(); t.CollectorHandle = t.CaseHandle; Assert.False(t.Valid); }
        [Fact] public void FailedReserveSaveRestoresCashAndNoTransfer()
        { var w = Company(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmSafeTransactions.Begin(w, Transfer(), () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void ReserveReplayAfterReloadDoesNotReserveTwice()
        { var w = Reserved(); w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); Assert.True(PdmSafeTransactions.Begin(w, Transfer(), () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Equal(40000, w.Pdm.Cash); }
        [Fact] public void CannotCreditBeforePhysicalHandoverCheckpoint()
        { var w = Reserved(); Assert.False(PdmSafeTransactions.Credit(w, "safe", Save, out _)); Assert.Equal(25000, w.Bank); Assert.False(w.Pdm.SafeTransfer.Handed); }
        [Fact] public void HandingOverConfirmsWithoutCreditingBankYet()
        { var w = Reserved(); long total = Total(w); Assert.True(PdmSafeTransactions.Hand(w, "safe", Save, out _)); Assert.True(w.Pdm.SafeTransfer.Handed); Assert.Equal(25000, w.Bank); Assert.Equal(total, Total(w)); Assert.False(PdmSafeTransactions.Refund(w, "safe", Save, out _)); }
        [Fact] public void FailedHandoverSavePreservesRefundableState()
        { var w = Reserved(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmSafeTransactions.Hand(w, "safe", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); Assert.True(PdmSafeTransactions.Refund(w, "safe", Save, out _)); }
        [Fact] public void CreditMovesEscrowToPersonalBankAndClosesCheckpointTogether()
        { var w = Handed(); long total = Total(w); int saves = 0; Assert.True(PdmSafeTransactions.Credit(w, "safe", () => { saves++; Assert.Null(w.Pdm.SafeTransfer); Assert.Equal(35000, w.Bank); Assert.Equal(10000, w.Pdm.Withdrawn); }, out _)); Assert.Equal(1, saves); Assert.Equal(total, Total(w)); Assert.Equal(40000, w.Pdm.Cash); Assert.Equal(0, w.Pdm.Revenue); Assert.Equal(0, w.Pdm.Sold); }
        [Fact] public void FailedBankSaveRestoresEscrowAndBothBalances()
        { var w = Handed(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmSafeTransactions.Credit(w, "safe", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void CreditReplayAfterReloadCannotPayTwice()
        { var w = Handed(); PdmSafeTransactions.Credit(w, "safe", Save, out _); w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); string before = JsonConvert.SerializeObject(w); Assert.True(PdmSafeTransactions.Credit(w, "safe", () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Equal(before, JsonConvert.SerializeObject(w)); Assert.False(PdmSafeTransactions.Refund(w, "safe", Save, out _)); }
        [Fact] public void BankOverflowRetainsPendingMoneyUntilRoomExists()
        { var w = Handed(); w.Bank = int.MaxValue - 9999; Assert.False(PdmSafeTransactions.Credit(w, "safe", Save, out _)); Assert.NotNull(w.Pdm.SafeTransfer); w.Bank = int.MaxValue - 10000; Assert.True(PdmSafeTransactions.Credit(w, "safe", Save, out _)); Assert.Equal(int.MaxValue, w.Bank); }
        [Fact] public void WithdrawalCounterOverflowCannotLoseEscrow()
        { var w = Handed(); w.Pdm.Withdrawn = long.MaxValue - 9999; Assert.False(PdmSafeTransactions.Credit(w, "safe", Save, out _)); Assert.Equal(10000, w.Pdm.SafeTransfer.Amount); Assert.Equal(25000, w.Bank); }
        [Fact] public void RefundRestoresCompanyCashWithoutIncomeOrBankChange()
        { var w = Reserved(); long total = Total(w); Assert.True(PdmSafeTransactions.Refund(w, "safe", Save, out _)); Assert.Equal(50000, w.Pdm.Cash); Assert.Equal(25000, w.Bank); Assert.Null(w.Pdm.SafeTransfer); Assert.Equal(0, w.Pdm.Withdrawn); Assert.Equal(total, Total(w)); }
        [Fact] public void FailedRefundAndReplayCannotEraseOrDuplicateCash()
        { var w = Reserved(); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmSafeTransactions.Refund(w, "safe", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); Assert.True(PdmSafeTransactions.Refund(w, "safe", Save, out _)); Assert.True(PdmSafeTransactions.Refund(w, "safe", () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Equal(50000, w.Pdm.Cash); Assert.False(PdmSafeTransactions.Hand(w, "safe", Save, out _)); Assert.False(PdmSafeTransactions.Credit(w, "safe", Save, out _)); }
        [Fact] public void WrongOperationCannotHandRefundOrCredit()
        { var w = Reserved(); Assert.False(PdmSafeTransactions.Hand(w, "wrong", Save, out _)); Assert.False(PdmSafeTransactions.Refund(w, "wrong", Save, out _)); PdmSafeTransactions.Hand(w, "safe", Save, out _); Assert.False(PdmSafeTransactions.Credit(w, "wrong", Save, out _)); Assert.Equal(25000, w.Bank); }
        [Fact] public void CompanyCashInvariantAlwaysLeavesSpaceToRefundReservedMoney()
        { var w = Reserved(); w.Pdm.Cash = int.MaxValue - 10000; w.Bank = 1; Assert.True(w.Pdm.Valid); Assert.False(PdmTransactions.Transfer(w, 1, true, "deposit", Save, out _)); Assert.Equal(1, w.Bank); Assert.True(PdmSafeTransactions.Refund(w, "safe", Save, out _)); Assert.Equal(int.MaxValue, w.Pdm.Cash); }
        [Fact] public void OldCreditReceiptCannotCreditAmountOfNewPendingCollection()
        { var w = Handed(); PdmSafeTransactions.Credit(w, "safe", Save, out _); PdmSafeTransactions.Begin(w, Transfer(1000, "second"), Save, out _); int bank = w.Bank; Assert.True(PdmSafeTransactions.Credit(w, "safe", () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Equal(bank, w.Bank); Assert.Equal("second", w.Pdm.SafeTransfer.Id); Assert.Equal(1000, w.Pdm.SafeTransfer.Amount); }
        [Fact] public void CannotStartSafeWhileCustomerDeliveryIsActive()
        { var w = Company(); PdmTransactions.Buy(w, PdmDefaults.Load(), "asea", "unit123", "buy", Save, out _); PdmTransactions.Begin(w, PdmDefaults.Load(), "unit123", "delivery", 0, 99, Save, out _); Assert.False(PdmSafeTransactions.Begin(w, Transfer(), Save, out _)); Assert.Equal("delivery", w.Pdm.Active.Id); }
        [Fact] public void SafeBlocksNewStockDeliveryRepairStaffAndCollectionOperations()
        {
            var w = Company(); PdmTransactions.Buy(w, PdmDefaults.Load(), "asea", "unit123", "buy", Save, out _); w.Pdm.Stock[0].Body = 500; PdmSafeTransactions.Begin(w, Transfer(), Save, out _);
            Assert.False(PdmTransactions.Begin(w, PdmDefaults.Load(), "unit123", "delivery", 0, 99, Save, out _)); Assert.False(PdmTransactions.Repair(w, PdmDefaults.Load(), "unit123", "repair", Save, out _)); Assert.False(PdmTransactions.Buy(w, PdmDefaults.Load(), "asea", "unit456", "buy2", Save, out _)); Assert.False(PdmStaffTransactions.Hire(w, PdmStaffDefaults.Load(), "junior", "j", Save, out _)); Assert.False(PdmStaffTransactions.Upgrade(w, PdmStaffDefaults.Load(), "workshop", "u", Save, out _));
            w.Pdm.Stock[0].AwaitingCollection = true;
            Assert.False(PdmCollectionTransactions.Begin(w, "haul", new System.Collections.Generic.List<string> { "unit123" }, new PdmEntityRef { Handle = 11, Model = "packer", Plate = "T1" }, new PdmEntityRef { Handle = 12, Model = "trflat", Plate = "T2" }, Save, out _)); Assert.Equal("safe", w.Pdm.SafeTransfer.Id);
        }
        [Fact] public void PayrollAccruesDuringTransferWithoutSpendingEscrow()
        { var w = Company(); PdmStaffTransactions.Hire(w, PdmStaffDefaults.Load(), "junior", "j", Save, out _); PdmSafeTransactions.Begin(w, Transfer(), Save, out _); for (int i = 0; i < 5; i++) Assert.True(PdmStaffTransactions.Work(w, 60, "work" + i, Save, out _)); Assert.Equal(100, w.Pdm.Staff.Employees[0].Owed); Assert.Equal(10000, w.Pdm.SafeTransfer.Amount); Assert.False(PdmStaffTransactions.Pay(w, "junior", "p", Save, out _)); Assert.Equal(25000, w.Bank); }
        [Fact] public void UnknownTransferAndWalletFieldsSurviveHandover()
        { var w = Reserved(); var j = JObject.Parse(JsonConvert.SerializeObject(w)); j["Future"] = "keep"; j["Pdm"]["Future"] = 1; j["Pdm"]["SafeTransfer"]["Future"] = 2; j["Pdm"]["SafeTransfer"]["Van"]["Future"] = 3; w = j.ToObject<WalletData>(); PdmSafeTransactions.Hand(w, "safe", Save, out _); j = JObject.Parse(JsonConvert.SerializeObject(w)); Assert.Equal("keep", (string)j["Future"]); Assert.Equal(1, (int)j["Pdm"]["Future"]); Assert.Equal(2, (int)j["Pdm"]["SafeTransfer"]["Future"]); Assert.Equal(3, (int)j["Pdm"]["SafeTransfer"]["Van"]["Future"]); }
        [Fact] public void InvalidTransferDataCannotMutateWallet()
        { var w = Reserved(); w.Pdm.SafeTransfer.Token = 0; Assert.False(w.Pdm.Valid); string before = JsonConvert.SerializeObject(w); Assert.False(PdmSafeTransactions.Refund(w, "safe", Save, out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        static PdmSafeRun Handover()
        { var r = new PdmSafeRun("safe", 3, 10); Assert.True(r.Arrive(true)); Assert.True(r.Handover(100, true)); return r; }
        static PdmSafeRun Boarding()
        { var r = Handover(); Assert.True(r.Hand(3100, true, () => true)); return r; }
        [Fact] public void CannotHandoverBeforeStoppedArrivalOrWithoutEligibility()
        { var r = new PdmSafeRun("safe", 3, 10); Assert.False(r.Handover(0, true)); Assert.False(r.Arrive(false)); Assert.True(r.Arrive(true)); Assert.False(r.Handover(0, false)); Assert.Equal(PdmSafeStage.Walking, r.Stage); }
        [Fact] public void HandoverRequiresFullDurationAndInterruptsOnMenuMovement()
        { var r = Handover(); Assert.False(r.Hand(3099, true, () => throw new Exception())); Assert.False(r.Hand(3100, false, () => throw new Exception())); Assert.Equal(PdmSafeStage.Walking, r.Stage); Assert.True(r.Handover(5000, true)); Assert.False(r.Hand(5001, true, () => throw new Exception())); }
        [Fact] public void HandoverSaveFailureRetainsStageForRetry()
        { var r = Handover(); Assert.False(r.Hand(3100, true, () => false)); Assert.Equal(PdmSafeStage.Handover, r.Stage); Assert.True(r.Hand(3200, true, () => true)); Assert.Equal(PdmSafeStage.Boarding, r.Stage); }
        [Fact] public void CannotCancelAfterHandedOrCreditBeforeDeparture()
        { var r = Boarding(); Assert.False(r.Cancel(() => throw new Exception())); Assert.False(r.Credit(50000, true, () => throw new Exception())); Assert.False(r.Depart(10000, false)); Assert.Equal(PdmSafeStage.Boarding, r.Stage); }
        [Fact] public void ClearingNeedsTenSecondsAndCreditRetryCannotCompleteTwice()
        { var r = Boarding(); Assert.True(r.Depart(5000, true)); Assert.False(r.Credit(14999, true, () => throw new Exception())); Assert.False(r.Credit(15000, true, () => false)); Assert.True(r.Credit(16000, true, () => true)); Assert.False(r.Credit(20000, true, () => throw new Exception())); Assert.Equal(PdmSafeStage.Complete, r.Stage); }
        [Fact] public void ClearingPauseRestartsEligibleTime()
        { var r = Boarding(); r.Depart(5000, true); Assert.False(r.Credit(10000, false, () => throw new Exception())); Assert.False(r.Credit(19999, true, () => throw new Exception())); Assert.True(r.Credit(20000, true, () => true)); }
        [Fact] public void TimerHandlesGameClockOverflow()
        { var r = Boarding(); int start = int.MaxValue - 100; r.Depart(start, true); Assert.True(r.Credit(unchecked(start + 10000), true, () => true)); }
        [Fact] public void CancelRequiresRefundCheckpointAndCannotResume()
        { var r = Handover(); Assert.False(r.Cancel(() => false)); Assert.Equal(PdmSafeStage.Handover, r.Stage); Assert.True(r.Cancel(() => true)); Assert.False(r.Hand(3100, true, () => throw new Exception())); Assert.False(r.Cancel(() => throw new Exception())); Assert.Equal(PdmSafeStage.Cancelled, r.Stage); }
    }
}
