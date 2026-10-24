using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class PdmFinanceTests
    {
        static readonly Action Save = () => { };
        static PdmFinanceOptions C => PdmFinanceDefaults.Load();
        static WalletData Company()
        { var w = new WalletData { Bank = 1000000, Cash = 55 }; Assert.True(PdmTransactions.Purchase(w, PdmDefaults.Load(), "company", Save, out _)); Assert.True(PdmTransactions.Transfer(w, 500000, true, "capital", Save, out _)); return w; }
        static void Dispatch(WalletData w, int index = 0, bool financed = true)
        { string stock = "S" + index.ToString("D4") + "unit"; Assert.True(PdmTransactions.Buy(w, PdmDefaults.Load(), "asea", stock, "buy" + index, Save, out _)); Assert.True(PdmTransactions.Begin(w, PdmDefaults.Load(), stock, "loan" + index, 0, 100 + index, Save, out _, financed)); }
        static WalletData Loan()
        { var w = Company(); Dispatch(w); Assert.True(PdmFinanceTransactions.Open(w, C, "loan0", 1000, 1000, Save, out _)); return w; }
        static void Work(WalletData w, int seconds)
        { while (seconds > 0) { int n = Math.Min(60, seconds); Assert.True(PdmFinanceTransactions.Work(w, n, Guid.NewGuid().ToString("N"), Save, out _)); seconds -= n; } }
        [Fact] public void DefaultsContainEntryInterestFourTwoMinuteInstallmentsAndSixActiveLoans()
        { Assert.True(C.Valid); Assert.Equal(25, C.DownPercent); Assert.Equal(10, C.InterestPercent); Assert.Equal(4, C.Installments); Assert.Equal(120, C.PeriodSeconds); Assert.Equal(6, C.MaxActive); }
        [Theory] [InlineData(0)] [InlineData(9)] [InlineData(81)]
        public void InvalidEntryRateRejected(int n)
        { var c = C; c.DownPercent = n; Assert.False(c.Valid); }
        [Theory] [InlineData(-1)] [InlineData(31)]
        public void InvalidInterestRejected(int n)
        { var c = C; c.InterestPercent = n; Assert.False(c.Valid); }
        [Theory] [InlineData(1)] [InlineData(13)]
        public void InvalidInstallmentCountRejected(int n)
        { var c = C; c.Installments = n; Assert.False(c.Valid); }
        [Fact] public void OldCompanyWithoutLoansAndOldActiveDeliveryRetainsNormalSaleMode()
        { var w = Company(); Dispatch(w, 0, false); var j = JObject.Parse(JsonConvert.SerializeObject(w)); j["Pdm"]["Loans"].Parent.Remove(); j["Pdm"]["Active"]["Financed"].Parent.Remove(); w = j.ToObject<WalletData>(); Assert.True(w.Pdm.Valid); Assert.Empty(w.Pdm.Loans); Assert.False(w.Pdm.Active.Financed); Assert.True(PdmTransactions.Sell(w, "loan0", 1000, 1000, Save, out _)); Assert.Equal(11200, w.Pdm.Revenue); }
        [Fact] public void CannotCreateLoanWithoutPhysicalDeliveryOrForCashSale()
        { var w = Company(); Assert.False(PdmFinanceTransactions.Open(w, C, "loan0", 1000, 1000, Save, out _)); Dispatch(w, 0, false); Assert.False(PdmFinanceTransactions.Open(w, C, "loan0", 1000, 1000, Save, out _)); Assert.NotNull(w.Pdm.Active); Assert.Empty(w.Pdm.Loans); }
        [Fact] public void FinancedDeliveryCannotBeSettledAsImmediateFullSale()
        { var w = Company(); Dispatch(w); int cash = w.Pdm.Cash; Assert.False(PdmTransactions.Sell(w, "loan0", 1000, 1000, Save, out _)); Assert.Equal(cash, w.Pdm.Cash); Assert.True(w.Pdm.Active.Financed); Assert.Single(w.Pdm.Stock); }
        [Fact] public void OpeningLoanRemovesExactStockAndCreditsOnlyEntryAtomically()
        { var w = Company(); Dispatch(w); int calls = 0; Assert.True(PdmFinanceTransactions.Open(w, C, "loan0", 1000, 1000, () => { calls++; Assert.Null(w.Pdm.Active); Assert.Empty(w.Pdm.Stock); Assert.Single(w.Pdm.Loans); Assert.Equal(495080, w.Pdm.Cash); }, out _)); var l = w.Pdm.Loans[0]; Assert.Equal(1, calls); Assert.Equal(12320, l.Total); Assert.Equal(3080, l.DownPayment); Assert.Equal(3080, l.Paid); Assert.Equal(2310, l.Payment); Assert.Equal(3080, w.Pdm.Revenue); Assert.Equal(8000, w.Pdm.SoldCost); Assert.Equal(1, w.Pdm.Sold); Assert.Equal(475000, w.Bank); Assert.Equal(55, w.Cash); }
        [Fact] public void FailedOpeningSaveRestoresDeliveryStockCashAndNoLoan()
        { var w = Company(); Dispatch(w); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmFinanceTransactions.Open(w, C, "loan0", 1000, 1000, () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void LoanOpeningReplayAfterReloadCannotPayEntryOrSellAgain()
        { var w = Loan(); w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); string before = JsonConvert.SerializeObject(w); Assert.True(PdmFinanceTransactions.Open(w, C, "loan0", 1000, 1000, () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Equal(before, JsonConvert.SerializeObject(w)); Assert.Equal(1, w.Pdm.Sold); }
        [Fact] public void CancelBeforeCustomerHandoverKeepsStockAndCreatesNoReceivable()
        { var w = Company(); Dispatch(w); int cash = w.Pdm.Cash; Assert.True(PdmTransactions.Cancel(w, "loan0", 700, 500, Save, out _)); Assert.Single(w.Pdm.Stock); Assert.Equal(500, w.Pdm.Stock[0].Body); Assert.Empty(w.Pdm.Loans); Assert.Equal(cash, w.Pdm.Cash); Assert.Equal(0, w.Pdm.Revenue); }
        [Fact] public void InstallmentOnlyPaysAtExactTwoMinuteBoundary()
        { var w = Loan(); Work(w, 119); Assert.Equal(3080, w.Pdm.Loans[0].Paid); Assert.Equal(119, w.Pdm.Loans[0].Elapsed); Work(w, 1); Assert.Equal(5390, w.Pdm.Loans[0].Paid); Assert.Equal(1, w.Pdm.Loans[0].Payments); Assert.Equal(0, w.Pdm.Loans[0].Elapsed); Assert.Equal(5390, w.Pdm.Revenue); }
        [Fact] public void FourPaymentsCloseLoanWithoutCreatingExtraMoneyOrAnotherSale()
        { var w = Loan(); Work(w, 480); var l = w.Pdm.Loans[0]; Assert.True(l.Complete); Assert.Equal(12320, l.Paid); Assert.Equal(4, l.Payments); Assert.Equal(12320, w.Pdm.Revenue); Assert.Equal(504320, w.Pdm.Cash); Assert.Equal(1, w.Pdm.Sold); Assert.Equal(8000, w.Pdm.SoldCost); Assert.Equal(4320, l.Profit); Assert.False(PdmFinanceTransactions.Work(w, 60, "after", Save, out _)); Assert.Equal(475000, w.Bank); }
        [Fact] public void LastInstallmentIncludesRoundingRemainderExactly()
        { var w = Company(); Dispatch(w); w.Pdm.Stock[0].Price = 11201; PdmFinanceTransactions.Open(w, C, "loan0", 1000, 1000, Save, out _); var l = w.Pdm.Loans[0]; Assert.Equal(12321, l.Total); Work(w, 360); l = w.Pdm.Loans[0]; Assert.Equal(10010, l.Paid); Assert.Equal(2311, l.Payment); Work(w, 120); Assert.Equal(12321, w.Pdm.Loans[0].Paid); Assert.True(w.Pdm.Loans[0].Complete); }
        [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(61)]
        public void InvalidTimeCannotAdvanceLoan(int seconds)
        { var w = Loan(); Assert.False(PdmFinanceTransactions.Work(w, seconds, "bad", Save, out _)); Assert.Equal(0, w.Pdm.Loans[0].Elapsed); }
        [Fact] public void FailedInstallmentSaveRollsBackAllProgressCashAndRevenue()
        { var w = Loan(); Work(w, 119); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmFinanceTransactions.Work(w, 1, "pay", () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void PaymentReplayAfterReloadCannotAdvanceTimeOrCreditTwice()
        { var w = Loan(); Work(w, 119); Assert.True(PdmFinanceTransactions.Work(w, 1, "pay", Save, out _)); w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); string before = JsonConvert.SerializeObject(w); Assert.True(PdmFinanceTransactions.Work(w, 1, "pay", () => throw new Exception(), out bool changed)); Assert.False(changed); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void TermsPriceHealthAndTimeSurviveChangesToDefaults()
        { var w = Company(); Dispatch(w); PdmFinanceTransactions.Open(w, C, "loan0", 800, 500, Save, out _); Work(w, 60); var c = C; c.PeriodSeconds = 60; c.DownPercent = 80; c.InterestPercent = 30; w = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); var l = w.Pdm.Loans[0]; Assert.Equal(10780, l.Total); Assert.Equal(2695, l.DownPayment); Assert.Equal(120, l.PeriodSeconds); Assert.Equal(25, l.DownPercent); Assert.Equal(10, l.InterestPercent); Assert.Equal(500, l.Asset.Body); Assert.Equal(800, l.Asset.Engine); Assert.Equal(60, l.Elapsed); Work(w, 60); Assert.Equal(1, w.Pdm.Loans[0].Payments); }
        [Fact] public void MarketingBonusIncludedOnceInSnapshotNotRecalculatedEveryPayment()
        { var w = Company(); PdmStaffTransactions.Hire(w, PdmStaffDefaults.Load(), "junior", "j", Save, out _); Dispatch(w); PdmFinanceTransactions.Open(w, C, "loan0", 1000, 1000, Save, out _); int total = w.Pdm.Loans[0].Total; Assert.Equal(12566, total); PdmStaffTransactions.Fire(w, "j", "f", Save, out _); Work(w, 480); Assert.Equal(total, w.Pdm.Loans[0].Paid); Assert.Equal(total, w.Pdm.Revenue); }
        [Fact] public void CashLimitLeavesPaymentDueUntilCapacityIsFreed()
        { var w = Loan(); w.Pdm.Cash = int.MaxValue - 2309; Work(w, 120); Assert.Equal(3080, w.Pdm.Loans[0].Paid); Assert.Equal(120, w.Pdm.Loans[0].Elapsed); w.Pdm.Cash = int.MaxValue - 2310; Work(w, 1); Assert.Equal(int.MaxValue, w.Pdm.Cash); Assert.Equal(1, w.Pdm.Loans[0].Payments); Assert.Equal(0, w.Pdm.Loans[0].Elapsed); }
        [Fact] public void RevenueLimitKeepsPaymentDueWithoutErasingBalance()
        { var w = Loan(); w.Pdm.Revenue = long.MaxValue - 2309; Work(w, 120); Assert.Equal(120, w.Pdm.Loans[0].Elapsed); Assert.Equal(3080, w.Pdm.Loans[0].Paid); w.Pdm.Revenue = long.MaxValue - 2310; Work(w, 1); Assert.Equal(long.MaxValue, w.Pdm.Revenue); }
        [Fact] public void ReceiptsRespectSpaceReservedForSafeRefund()
        { var w = Loan(); var t = new PdmSafeTransfer { Id = "safe", Amount = 10000, CollectorHandle = 201, CollectorModel = "s_m_m_armoured_01", CaseHandle = 202, CaseModel = "prop_security_case_01", Token = 5, Van = new PdmEntityRef { Handle = 200, Model = "stockade", Plate = "PSF00001" } }; Assert.True(PdmSafeTransactions.Begin(w, t, Save, out _)); w.Pdm.Cash = int.MaxValue - 10000; Work(w, 120); Assert.Equal(3080, w.Pdm.Loans[0].Paid); Assert.True(w.Pdm.Valid); Assert.True(PdmSafeTransactions.Refund(w, "safe", Save, out _)); Assert.Equal(int.MaxValue, w.Pdm.Cash); }
        [Fact] public void ActiveLoanLimitRejectsOpeningWithoutConsumingDelivery()
        { var w = Company(); var c = C; c.MaxActive = 1; Dispatch(w); Assert.True(PdmFinanceTransactions.Open(w, c, "loan0", 1000, 1000, Save, out _)); Dispatch(w, 1); int cash = w.Pdm.Cash; Assert.False(PdmFinanceTransactions.Open(w, c, "loan1", 1000, 1000, Save, out _)); Assert.Equal(cash, w.Pdm.Cash); Assert.Equal("loan1", w.Pdm.Active.Id); Assert.Single(w.Pdm.Stock); Work(w, 480); Assert.True(PdmFinanceTransactions.Open(w, c, "loan1", 1000, 1000, Save, out _)); Assert.Equal(2, w.Pdm.Loans.Count); }
        [Fact] public void SeveralLoansKeepIndependentTimeAndPayments()
        { var w = Loan(); Work(w, 60); Dispatch(w, 1); PdmFinanceTransactions.Open(w, C, "loan1", 1000, 1000, Save, out _); Work(w, 60); Assert.Equal(1, w.Pdm.Loans[0].Payments); Assert.Equal(0, w.Pdm.Loans[1].Payments); Assert.Equal(60, w.Pdm.Loans[1].Elapsed); }
        [Fact] public void LoanHistoryPrunesCompletedOnlyAndRetainsAggregateCounters()
        { var w = Company(); for (int i = 0; i < 43; i++) { Dispatch(w, i); Assert.True(PdmFinanceTransactions.Open(w, C, "loan" + i, 1000, 1000, Save, out _)); Work(w, 480); } Assert.Equal(42, w.Pdm.Loans.Count); Assert.DoesNotContain(w.Pdm.Loans, l => l.Id == "loan0"); Assert.Equal(43, w.Pdm.Sold); Assert.Equal(43L * 12320, w.Pdm.Revenue); Assert.Equal(43L * 8000, w.Pdm.SoldCost); }
        [Fact] public void FailedFullHistoryOpeningDoesNotPruneHistoryOrLoseStock()
        { var w = Company(); for (int i = 0; i < 42; i++) { Dispatch(w, i); PdmFinanceTransactions.Open(w, C, "loan" + i, 1000, 1000, Save, out _); Work(w, 480); } Dispatch(w, 42); string before = JsonConvert.SerializeObject(w); Assert.Throws<Exception>(() => PdmFinanceTransactions.Open(w, C, "loan42", 1000, 1000, () => throw new Exception(), out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
        [Fact] public void BankCashPayrollAndPersonalTransportAreNotDebitedByInstallments()
        { var w = Loan(); PdmStaffTransactions.Hire(w, PdmStaffDefaults.Load(), "junior", "j", Save, out _); string transport = JsonConvert.SerializeObject(w.Transport); Work(w, 120); Assert.Equal(475000, w.Bank); Assert.Equal(55, w.Cash); Assert.Equal(0, w.Pdm.Staff.Employees[0].Owed); Assert.Equal(transport, JsonConvert.SerializeObject(w.Transport)); }
        [Fact] public void DeliveredAssetIdentityCannotBePurchasedAgainWhileInLoanHistory()
        { var w = Loan(); Assert.False(PdmTransactions.Buy(w, PdmDefaults.Load(), "asea", "S0000unit", "again", Save, out _)); Assert.Empty(w.Pdm.Stock); Assert.Single(w.Pdm.Loans); }
        [Fact] public void UnknownFieldsSurviveOpeningAndInstallmentCheckpoints()
        { var w = Company(); Dispatch(w); var j = JObject.Parse(JsonConvert.SerializeObject(w)); j["Future"] = "keep"; j["Pdm"]["Future"] = 1; j["Pdm"]["Stock"][0]["Future"] = 2; w = j.ToObject<WalletData>(); PdmFinanceTransactions.Open(w, C, "loan0", 1000, 1000, Save, out _); j = JObject.Parse(JsonConvert.SerializeObject(w)); j["Pdm"]["Loans"][0]["Future"] = 3; w = j.ToObject<WalletData>(); Work(w, 120); j = JObject.Parse(JsonConvert.SerializeObject(w)); Assert.Equal("keep", (string)j["Future"]); Assert.Equal(1, (int)j["Pdm"]["Future"]); Assert.Equal(2, (int)j["Pdm"]["Loans"][0]["Asset"]["Future"]); Assert.Equal(3, (int)j["Pdm"]["Loans"][0]["Future"]); }
        [Fact] public void InvalidLoanProgressCannotMutateWallet()
        { var w = Loan(); w.Pdm.Loans[0].Payments = 2; Assert.False(w.Pdm.Valid); string before = JsonConvert.SerializeObject(w); Assert.False(PdmFinanceTransactions.Work(w, 60, "invalid", Save, out _)); Assert.Equal(before, JsonConvert.SerializeObject(w)); }
    }
}
