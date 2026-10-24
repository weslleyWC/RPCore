using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class PdmRiskTests
    {
        static readonly Action Save = () => { };
        static PdmRiskOptions C => PdmRiskDefaults.Load();
        static PdmRiskOptions Forced() { var c = C; c.RiskPercent = 100; c.ExposureSeconds = 60; return c; }
        static WalletData Company()
        { var w = new WalletData { Bank = 300000, Cash = 55 }; PdmTransactions.Purchase(w,PdmDefaults.Load(),"company",Save,out _); PdmTransactions.Transfer(w,200000,true,"capital",Save,out _); return w; }
        static void Open(WalletData w) => Assert.True(PdmRiskTransactions.SetSafe(w,true,"open",Save,out _));
        static void Insure(WalletData w) => Assert.True(PdmRiskTransactions.Insure(w,C,"tier1","policy",Save,out _));
        [Fact] public void DefaultsHaveFivePremiumsCapsAndSafeExposureRules()
        { var c=C; Assert.True(c.Valid); Assert.Equal(new[]{10000,20000,35000,55000,80000},c.Plans.Select(p=>p.Premium)); Assert.Equal(new[]{100000,200000,500000,1000000,2000000},c.Plans.Select(p=>p.Limit)); Assert.Equal(1800,c.PeriodSeconds); Assert.Equal(120,c.ExposureSeconds); Assert.Equal(15,c.RiskPercent); Assert.Equal(20,c.LossPercent); Assert.Equal(50000,c.MinimumCash); }
        [Fact] public void LegacySaveMissingRiskHasClosedSafeNoPolicyNoCharges()
        { var w=Company(); var j=JObject.Parse(JsonConvert.SerializeObject(w)); j["Pdm"]["Risk"].Parent.Remove(); w=j.ToObject<WalletData>(); Assert.True(w.Pdm.Valid); Assert.False(w.Pdm.Risk.SafeOpen); Assert.Null(w.Pdm.Risk.Policy); Assert.False(PdmRiskTransactions.Work(w,Forced(),60,true,"time",Save,out _)); Assert.Equal(200000,w.Pdm.Cash); }
        [Fact] public void OpeningClosingOnlyChangesRiskStateAndLedger()
        { var w=Company(); Open(w); Assert.Equal(200000,w.Pdm.Cash); Assert.Equal(0,w.Pdm.Risk.Stolen); PdmRiskTransactions.Work(w,C,60,true,"time",Save,out _); Assert.Equal(60,w.Pdm.Risk.ExposedSeconds); Assert.True(PdmRiskTransactions.SetSafe(w,false,"close",Save,out _)); Assert.Equal(0,w.Pdm.Risk.ExposedSeconds); Assert.False(w.Pdm.Risk.SafeOpen); Assert.Equal(200000,w.Pdm.Cash); }
        [Fact] public void SafeStateChangeRequiresOwnedIdleCompanyAndRollsBackOnSaveFailure()
        { var w=Company(); string before=JsonConvert.SerializeObject(w); Assert.Throws<Exception>(()=>PdmRiskTransactions.SetSafe(w,true,"open",()=>throw new Exception(),out _)); Assert.Equal(before,JsonConvert.SerializeObject(w)); Assert.False(PdmRiskTransactions.SetSafe(new WalletData(),true,"open",Save,out _)); }
        [Fact] public void PolicyChargesCompanyNowAndSnapshotsCoverageAndRenewalTerms()
        { var w=Company(); Insure(w); Assert.Equal(190000,w.Pdm.Cash); Assert.Equal(75000,w.Bank); Assert.Equal(55,w.Cash); Assert.Equal(10000,w.Pdm.Risk.Premiums); Assert.Equal(100000,w.Pdm.Risk.Policy.Limit); Assert.Equal(1800,w.Pdm.Risk.Policy.PeriodSeconds); Assert.Equal(0,w.Pdm.Risk.Policy.Seconds); Assert.Equal(0,w.Pdm.Staff.UpgradeSpent); }
        [Fact] public void FailedPolicySaveAndReplayedPurchaseCannotDoubleCharge()
        { var w=Company(); string before=JsonConvert.SerializeObject(w); Assert.Throws<Exception>(()=>PdmRiskTransactions.Insure(w,C,"tier1","policy",()=>throw new Exception(),out _)); Assert.Equal(before,JsonConvert.SerializeObject(w)); Insure(w); int cash=w.Pdm.Cash; w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); Assert.True(PdmRiskTransactions.Insure(w,C,"tier1","policy",()=>throw new Exception(),out bool changed)); Assert.False(changed); Assert.Equal(cash,w.Pdm.Cash); Assert.False(PdmRiskTransactions.Insure(w,C,"tier1","other",Save,out _)); }
        [Fact] public void CancelEndsCoverageAndFutureChargesWithoutRefund()
        { var w=Company(); Insure(w); Assert.True(PdmRiskTransactions.Cancel(w,"cancel",Save,out _)); Assert.Null(w.Pdm.Risk.Policy); Assert.Equal(190000,w.Pdm.Cash); Assert.False(PdmRiskTransactions.Work(w,C,60,false,"time",Save,out _)); Assert.False(PdmRiskTransactions.Cancel(w,"again",Save,out _)); }
        [Fact] public void SwitchingPolicyPaysNewFullPremiumAndUsesNewSnapshot()
        { var w=Company(); Insure(w); Assert.True(PdmRiskTransactions.Insure(w,C,"tier2","switch",Save,out _)); Assert.Equal(170000,w.Pdm.Cash); Assert.Equal(30000,w.Pdm.Risk.Premiums); Assert.Equal(200000,w.Pdm.Risk.Policy.Limit); }
        [Fact] public void NearPlayerOrClosedSafeCannotLoseMoneyEvenWithCertainRoll()
        { var w=Company(); Open(w); for(int i=0;i<3;i++)Assert.True(PdmRiskTransactions.Work(w,Forced(),60,false,"near"+i,Save,out _)); Assert.Equal(200000,w.Pdm.Cash); Assert.Equal(0,w.Pdm.Risk.Checks); Assert.Equal(0,w.Pdm.Risk.ExposedSeconds); PdmRiskTransactions.SetSafe(w,false,"close",Save,out _); Assert.False(PdmRiskTransactions.Work(w,Forced(),60,true,"closed",Save,out _)); }
        [Theory] [InlineData(0)] [InlineData(49999)] [InlineData(50000)]
        public void CashAtOrBelowThresholdIsNotExposed(int cash)
        { var w=Company(); w.Pdm.Cash=cash; Open(w); Assert.True(PdmRiskTransactions.Work(w,Forced(),60,true,"time",Save,out _)); Assert.Equal(cash,w.Pdm.Cash); Assert.Equal(0,w.Pdm.Risk.Checks); }
        [Fact] public void ExposureNeedsConsecutiveAwayTimeAndResetsOnReturn()
        { var w=Company(); Open(w); PdmRiskTransactions.Work(w,C,60,true,"a",Save,out _); PdmRiskTransactions.Work(w,C,1,false,"near",Save,out _); Assert.Equal(0,w.Pdm.Risk.ExposedSeconds); PdmRiskTransactions.Work(w,C,60,true,"b",Save,out _); Assert.Equal(0,w.Pdm.Risk.Checks); Assert.Equal(60,w.Pdm.Risk.ExposedSeconds); }
        [Fact] public void UninsuredIncidentDebitsRealLossAndClosesSafeOnce()
        { var w=Company(); Open(w); Assert.True(PdmRiskTransactions.Work(w,Forced(),60,true,"loss",Save,out _)); Assert.Equal(160000,w.Pdm.Cash); Assert.Equal(40000,w.Pdm.Risk.Stolen); Assert.Equal(0,w.Pdm.Risk.Reimbursed); Assert.False(w.Pdm.Risk.SafeOpen); Assert.Equal(0,w.Pdm.Risk.ExposedSeconds); Assert.Equal(40000,Assert.Single(w.Pdm.Risk.Incidents).Loss); Assert.Equal(0,w.Pdm.Revenue); Assert.Equal(0,w.Pdm.Withdrawn); Assert.Equal(75000,w.Bank); }
        [Fact] public void CoveredIncidentRefundsOnlyActualLossInSameCheckpoint()
        { var w=Company(); Insure(w); Open(w); int cash=w.Pdm.Cash; Assert.True(PdmRiskTransactions.Work(w,Forced(),60,true,"loss",Save,out _)); Assert.Equal(cash,w.Pdm.Cash); Assert.Equal(38000,w.Pdm.Risk.Stolen); Assert.Equal(38000,w.Pdm.Risk.Reimbursed); var i=Assert.Single(w.Pdm.Risk.Incidents); Assert.Equal("Nivel 1",i.PolicyName); Assert.Equal(i.Loss,i.Reimbursed); Assert.Equal(0,w.Pdm.Revenue); }
        [Fact] public void OverLimitIncidentHasNoPartialPayout()
        { var w=Company(); Insure(w); w.Pdm.Risk.Policy.Limit=1000; Open(w); PdmRiskTransactions.Work(w,Forced(),60,true,"loss",Save,out _); Assert.Equal(152000,w.Pdm.Cash); Assert.Equal(0,w.Pdm.Risk.Reimbursed); Assert.Equal(1000,w.Pdm.Risk.Incidents[0].Limit); }
        [Fact] public void ClaimFailureRestoresLossPayoutSafeAndPremiumForRetry()
        { var w=Company(); Insure(w); Open(w); string before=JsonConvert.SerializeObject(w); Assert.Throws<Exception>(()=>PdmRiskTransactions.Work(w,Forced(),60,true,"loss",()=>throw new Exception(),out _)); Assert.Equal(before,JsonConvert.SerializeObject(w)); Assert.True(PdmRiskTransactions.Work(w,Forced(),60,true,"loss",Save,out _)); }
        [Fact] public void IncidentReceiptReplayAfterReloadCannotLoseOrPayAgain()
        { var w=Company(); Insure(w); Open(w); PdmRiskTransactions.Work(w,Forced(),60,true,"loss",Save,out _); w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); string before=JsonConvert.SerializeObject(w); Assert.True(PdmRiskTransactions.Work(w,Forced(),60,true,"loss",()=>throw new Exception(),out bool changed)); Assert.False(changed); Assert.Equal(before,JsonConvert.SerializeObject(w)); }
        [Fact] public void FullPaidSecurityEliminatesRiskWithoutGeneratingMoney()
        { var w=Company(); PdmSurveillanceTransactions.Purchase(w,PdmSurveillanceDefaults.Load(),"cams",Save,out _); var staff=PdmSecurityDefaults.Merge(PdmStaffDefaults.Load(),PdmSecurityDefaults.Load()); PdmStaffTransactions.Hire(w,staff,"security1","g1",Save,out _); PdmStaffTransactions.Hire(w,staff,"security2","g2",Save,out _); Assert.Equal(100,PdmRiskTransactions.Security(w.Pdm)); Assert.Equal(0,PdmRiskTransactions.Chance(w.Pdm,Forced())); Open(w); int cash=w.Pdm.Cash; PdmRiskTransactions.Work(w,Forced(),60,true,"time",Save,out _); Assert.Equal(cash,w.Pdm.Cash); Assert.True(w.Pdm.Risk.SafeOpen); Assert.Empty(w.Pdm.Risk.Incidents); }
        [Fact] public void UnpaidGuardOrFormerDebtRemovesGuardProtectionButKeepsCameras()
        { var w=Company(); PdmSurveillanceTransactions.Purchase(w,PdmSurveillanceDefaults.Load(),"cams",Save,out _); var staff=PdmSecurityDefaults.Merge(PdmStaffDefaults.Load(),PdmSecurityDefaults.Load()); PdmStaffTransactions.Hire(w,staff,"security1","g1",Save,out _); Assert.Equal(65,PdmRiskTransactions.Security(w.Pdm)); Assert.Equal(6,PdmRiskTransactions.Chance(w.Pdm,C)); w.Pdm.Staff.Employees[0].Owed=260; Assert.Equal(30,PdmRiskTransactions.Security(w.Pdm)); Assert.Equal(11,PdmRiskTransactions.Chance(w.Pdm,C)); w.Pdm.Staff.Employees[0].Owed=0; w.Pdm.Staff.FormerDebt=1; Assert.Equal(30,PdmRiskTransactions.Security(w.Pdm)); }
        [Fact] public void RenewalAtExactDeadlineUsesSavedPremiumNotChangedOptions()
        { var w=Company(); Insure(w); w.Pdm.Risk.Policy.Seconds=1799; var c=C; c.PeriodSeconds=300; c.Plans[0].Premium=20000; c.Plans[0].Limit=1; Assert.True(PdmRiskTransactions.Work(w,c,1,false,"renew",Save,out _)); Assert.Equal(180000,w.Pdm.Cash); Assert.Equal(20000,w.Pdm.Risk.Premiums); Assert.Equal(0,w.Pdm.Risk.Policy.Seconds); Assert.Equal(1800,w.Pdm.Risk.Policy.PeriodSeconds); Assert.Equal(100000,w.Pdm.Risk.Policy.Limit); }
        [Fact] public void InsufficientRenewalCashLapsesBeforeIncidentAndNeverOverdraws()
        { var w=Company(); Insure(w); var c=Forced(); c.MinimumCash=0; w.Pdm.Risk.Policy.Seconds=1799; w.Pdm.Cash=9999; Open(w); w.Pdm.Risk.ExposedSeconds=59; PdmRiskTransactions.Work(w,c,1,true,"due",Save,out _); Assert.Null(w.Pdm.Risk.Policy); Assert.Equal(8000,w.Pdm.Cash); Assert.Equal(1999,w.Pdm.Risk.Stolen); Assert.Equal(0,w.Pdm.Risk.Reimbursed); }
        [Fact] public void FailedRenewalAndReplayCannotChargeTwice()
        { var w=Company(); Insure(w); w.Pdm.Risk.Policy.Seconds=1799; string before=JsonConvert.SerializeObject(w); Assert.Throws<Exception>(()=>PdmRiskTransactions.Work(w,C,1,false,"renew",()=>throw new Exception(),out _)); Assert.Equal(before,JsonConvert.SerializeObject(w)); PdmRiskTransactions.Work(w,C,1,false,"renew",Save,out _); Assert.True(PdmRiskTransactions.Work(w,C,1,false,"renew",()=>throw new Exception(),out bool changed)); Assert.False(changed); Assert.Equal(180000,w.Pdm.Cash); }
        [Fact] public void BusyPdmDeliverySuppressesExposureButPolicyTimeContinues()
        { var w=Company(); Insure(w); Open(w); PdmTransactions.Buy(w,PdmDefaults.Load(),"asea","AA111stock","buy",Save,out _); PdmTransactions.Begin(w,PdmDefaults.Load(),"AA111stock","delivery",0,200,Save,out _); Assert.True(PdmRiskTransactions.Work(w,Forced(),60,true,"time",Save,out _)); Assert.Equal(60,w.Pdm.Risk.Policy.Seconds); Assert.Equal(0,w.Pdm.Risk.ExposedSeconds); Assert.Equal(0,w.Pdm.Risk.Stolen); Assert.False(PdmRiskTransactions.Cancel(w,"cancel",Save,out _)); }
        [Fact] public void CoveredLossNearIntegerCashLimitCannotOverflow()
        { var w=Company(); Insure(w); w.Pdm.Cash=int.MaxValue; w.Pdm.Risk.Policy.Limit=200000000; var c=Forced(); c.LossPercent=1; Open(w); PdmRiskTransactions.Work(w,c,60,true,"loss",Save,out _); Assert.Equal(int.MaxValue,w.Pdm.Cash); Assert.Equal(21474836,w.Pdm.Risk.Reimbursed); }
        [Fact] public void AccountingOverflowClosesSafeWithoutLossAndDoesNotBlockFutureManagement()
        { var w=Company(); Open(w); w.Pdm.Risk.Stolen=long.MaxValue-39999; Assert.True(PdmRiskTransactions.Work(w,Forced(),60,true,"loss",Save,out _)); Assert.Equal(200000,w.Pdm.Cash); Assert.False(w.Pdm.Risk.SafeOpen); Assert.Empty(w.Pdm.Risk.Incidents); }
        [Fact] public void IncidentHistoryIsBoundedWhileTotalsStayComplete()
        { var w=Company(); var c=Forced(); for(int i=0;i<23;i++){w.Pdm.Cash=200000; PdmRiskTransactions.SetSafe(w,true,"open"+i,Save,out _); Assert.True(PdmRiskTransactions.Work(w,c,60,true,"loss"+i,Save,out _));} Assert.Equal(20,w.Pdm.Risk.Incidents.Count); Assert.Equal(920000,w.Pdm.Risk.Stolen); Assert.Equal("loss3",w.Pdm.Risk.Incidents[0].Id); }
        [Fact] public void UnknownRiskPolicyAndWalletFieldsSurviveCheckpoints()
        { var w=Company(); Insure(w); var j=JObject.Parse(JsonConvert.SerializeObject(w)); j["Pdm"]["Future"]=1; j["Pdm"]["Risk"]["Future"]=2; j["Pdm"]["Risk"]["Policy"]["Future"]=3; w=j.ToObject<WalletData>(); PdmRiskTransactions.Work(w,C,60,false,"time",Save,out _); j=JObject.Parse(JsonConvert.SerializeObject(w)); Assert.Equal(1,(int)j["Pdm"]["Future"]); Assert.Equal(2,(int)j["Pdm"]["Risk"]["Future"]); Assert.Equal(3,(int)j["Pdm"]["Risk"]["Policy"]["Future"]); }
        [Fact] public void ReducingExposureConfigCannotRetroactivelyApplyManyEvents()
        { var w=Company(); Open(w); w.Pdm.Risk.ExposedSeconds=3500; var c=Forced(); c.RiskPercent=0; PdmRiskTransactions.Work(w,c,1,true,"config",Save,out _); Assert.Equal(1,w.Pdm.Risk.Checks); Assert.Equal(0,w.Pdm.Risk.ExposedSeconds); Assert.Equal(0,w.Pdm.Risk.Stolen); }
        [Theory] [InlineData(-1)] [InlineData(0)] [InlineData(61)]
        public void InvalidElapsedCannotChargeOrLose(int seconds) { var w=Company(); Insure(w); Assert.False(PdmRiskTransactions.Work(w,C,seconds,true,"bad",Save,out _)); Assert.Equal(190000,w.Pdm.Cash); }
        [Fact] public void InvalidOptionsOrRiskStateCannotOverwriteSave()
        { var w=Company(); var c=C; c.RiskPercent=101; Assert.False(c.Valid); Assert.False(PdmRiskTransactions.Insure(w,c,"tier1","policy",Save,out _)); w.Pdm.Risk.Reimbursed=1; Assert.False(w.Pdm.Valid); Assert.False(PdmRiskTransactions.SetSafe(w,true,"open",Save,out _)); }
        [Fact] public void EvenBriefReturnResetsExposureWithoutAdvancingPolicyOrCharging()
        { var w=Company(); Insure(w); Open(w); PdmRiskTransactions.Work(w,C,60,true,"away",Save,out _); int cash=w.Pdm.Cash, time=w.Pdm.Risk.Policy.Seconds; Assert.True(PdmRiskTransactions.ReturnToPremises(w,"return",Save,out _)); Assert.Equal(0,w.Pdm.Risk.ExposedSeconds); Assert.Equal(time,w.Pdm.Risk.Policy.Seconds); Assert.Equal(cash,w.Pdm.Cash); Assert.True(w.Pdm.Risk.SafeOpen); }
        [Fact] public void FailedReturnResetPreservesExposureForRetryWithoutRepeatingLoss()
        { var w=Company(); Open(w); PdmRiskTransactions.Work(w,C,60,true,"away",Save,out _); Assert.Throws<Exception>(()=>PdmRiskTransactions.ReturnToPremises(w,"return",()=>throw new Exception(),out _)); Assert.Equal(60,w.Pdm.Risk.ExposedSeconds); Assert.True(PdmRiskTransactions.ReturnToPremises(w,"return",Save,out _)); Assert.Equal(0,w.Pdm.Risk.Stolen); }
        [Fact] public void RiskRollIsStableAcrossRetriesAndNotDependentOnRuntimeHashCode()
        { Assert.Equal(PdmRiskTransactions.Roll("same-receipt"),PdmRiskTransactions.Roll("same-receipt")); Assert.All(Enumerable.Range(0,100).Select(i=>PdmRiskTransactions.Roll("receipt"+i)),r=>Assert.InRange(r,0,99)); }
    }
}
