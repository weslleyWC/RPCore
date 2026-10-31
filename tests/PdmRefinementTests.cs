using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class PdmRefinementTests
    {
        static WalletData W(int cash=50000,int bank=100000)=>new WalletData{Bank=bank,Pdm=new PdmData{Owned=true,Cash=cash}};
        static PdmStock Car(string id="car")=>new PdmStock{Id=id,Model="asea",Name="Asea",Plate=id=="car"?"PDM12345":"OTHER",Cost=8000,Price=11200};
        static PdmEmployee Employee(string role,int owed)=>new PdmEmployee{Id=role,Role=role,Name=role,Wage=100,PeriodSeconds=300,Seconds=20,Owed=owed};
        static WalletData Display(){var w=W();w.Pdm.Stock.Add(Car());w.Pdm.Showroom.Add(new PdmShowroomEntry{SlotId="one",StockId="car",Handle=42});return w;}
        static WalletData Deal(){var w=Display();Assert.True(PdmShowroomTransactions.BeginSale(w,"car","deal",true,()=>{},out _));return w;}
        static void Inspect(WalletData w)=>Assert.True(PdmNegotiationTransactions.Inspect(w,"deal",()=>{},out _));
        static PdmCommandQuote Quote(WalletData w,int now=100)=>new PdmCommandQuote(w.Pdm,w.Bank,"RP",now);
        [Fact] public void QuoteIsSingleUseEvenAfterRejectedAttempt(){var w=W();var q=Quote(w);Assert.True(q.Take(w.Pdm,w.Bank,"RP",101));Assert.False(q.Take(w.Pdm,w.Bank,"RP",102));q=Quote(w);Assert.False(q.Take(w.Pdm,w.Bank,"Other",101));Assert.False(q.Take(w.Pdm,w.Bank,"RP",102));}
        [Theory][InlineData("bank")][InlineData("cash")][InlineData("owned")][InlineData("stock")][InlineData("staff")][InlineData("upgrade")][InlineData("policy")][InlineData("safe")][InlineData("pending")]
        public void QuoteRejectsChangesToBusinessTerms(string change)
        {
            var w=W();var q=Quote(w);
            switch(change){case "bank":w.Bank--;break;case "cash":w.Pdm.Cash--;break;case "owned":w.Pdm.Owned=false;break;case "stock":w.Pdm.Stock.Add(Car());break;case "staff":w.Pdm.Staff.Employees.Add(Employee("mechanic",0));break;case "upgrade":w.Pdm.Staff.Workshop=true;break;case "policy":w.Pdm.Surveillance.Purchased=true;w.Pdm.Surveillance.Paid=15000;break;case "safe":w.Pdm.Risk.SafeOpen=true;break;case "pending":w=Deal();break;}
            Assert.False(q.Take(w.Pdm,w.Bank,"RP",101));
        }
        [Fact] public void BackgroundTimerAndHandleRefreshDoNotInvalidateUnchangedTerms()
        {
            var w=Display();w.Pdm.Staff.Employees.Add(Employee("mechanic",0));var q=Quote(w);
            w.Pdm.DirtSeconds=2;w.Pdm.Staff.Employees[0].Seconds++;w.Pdm.Showroom[0].Handle=77;
            Assert.True(w.Pdm.Valid);Assert.True(q.Take(w.Pdm,w.Bank,"RP",200));
        }
        [Fact] public void StockDamageOrPlateChangeInvalidatesQuote(){var w=Display();var q=Quote(w);w.Pdm.Stock[0].Body=999;Assert.False(q.Take(w.Pdm,w.Bank,"RP",101));q=Quote(w);w.Pdm.Stock[0].Plate="CHANGED";Assert.False(q.Take(w.Pdm,w.Bank,"RP",101));}
        [Theory][InlineData(99,false)][InlineData(100,true)][InlineData(60100,true)][InlineData(60101,false)]
        public void QuoteHasBoundedLifetimeAndRejectsBackwardTime(int now,bool valid){var w=W();Assert.Equal(valid,Quote(w).Take(w.Pdm,w.Bank,"RP",now));}
        [Fact] public void QuoteSupportsGameClockWrap(){var w=W();Assert.True(Quote(w,int.MaxValue-20).Take(w.Pdm,w.Bank,"RP",unchecked(int.MaxValue+50)));}
        [Fact] public void InvalidDataCannotAuthorizeAction(){var w=W();var q=Quote(w);w.Pdm.Stock=null;Assert.False(q.Take(w.Pdm,w.Bank,"RP",101));Assert.False(new PdmCommandQuote(null,0,"RP",0).Take(null,0,"RP",1));}
        [Theory][InlineData(true)][InlineData(false)]
        public void BusinessTransfersOverOneMillionCommitOnceAndPreservePersonalCash(bool deposit)
        {
            var w=W(3000000,4000000);w.Cash=7;int calls=0;
            Assert.True(PdmTransactions.Transfer(w,2000000,deposit,"big",()=>calls++,out bool changed));Assert.True(changed);
            Assert.Equal(deposit?5000000:1000000,w.Pdm.Cash);Assert.Equal(deposit?2000000:6000000,w.Bank);Assert.Equal(7,w.Cash);
            w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(PdmTransactions.Transfer(w,2000000,deposit,"big",()=>throw new Exception(),out changed));Assert.False(changed);Assert.Equal(1,calls);
        }
        [Theory][InlineData(true)][InlineData(false)]
        public void LargeTransferDiskFailureRestoresEveryBalanceCounterAndReceipt(bool deposit)
        {var w=W(3000000,4000000);string before=JsonConvert.SerializeObject(w);Assert.Throws<Exception>(()=>PdmTransactions.Transfer(w,2000000,deposit,"big",()=>throw new Exception("disk"),out _));Assert.Equal(before,JsonConvert.SerializeObject(w));}
        [Fact] public void MaxTransferNeverOverflowsDestinationOrSpendsReservedHeadroom()
        {
            var w=W(int.MaxValue-5,100);Assert.Equal(5,PdmBusinessRules.TransferMaximum(w.Pdm,w.Bank,true));w=W(100,int.MaxValue-7);Assert.Equal(7,PdmBusinessRules.TransferMaximum(w.Pdm,w.Bank,false));
            w=W();w.Pdm.SafeTransfer=new PdmSafeTransfer{Id="cash",Amount=1000,CollectorHandle=1,CaseHandle=2,Token=1,CollectorModel="guard",CaseModel="case",Van=new PdmEntityRef{Handle=3,Model="stockade",Plate="SAFE"}};
            w.Pdm.Cash=int.MaxValue-1005;Assert.True(w.Pdm.Valid);Assert.Equal(5,PdmBusinessRules.TransferMaximum(w.Pdm,w.Bank,true));
        }
        [Fact] public void MaxTransferRequiresValidOwnedBusinessAndNonnegativeBank(){var w=W();w.Pdm.Owned=false;Assert.Equal(0,PdmBusinessRules.TransferMaximum(w.Pdm,w.Bank,true));Assert.Equal(0,PdmBusinessRules.TransferMaximum(null,1,true));w.Pdm.Owned=true;Assert.Equal(0,PdmBusinessRules.TransferMaximum(w.Pdm,-1,false));}
        [Fact] public void PayrollSettlementIsAtomicAcrossEmployeesAndFormerDebt()
        {
            var w=W();w.Pdm.Staff.Employees.Add(Employee("mechanic",400));w.Pdm.Staff.Employees.Add(Employee("cleaner",200));w.Pdm.Staff.FormerDebt=100;
            Assert.True(PdmStaffTransactions.PayAll(w,"payroll",()=>{Assert.Equal(49300,w.Pdm.Cash);Assert.Equal(700,w.Pdm.Staff.Paid);Assert.All(w.Pdm.Staff.Employees,e=>Assert.Equal(0,e.Owed));Assert.Equal(0,w.Pdm.Staff.FormerDebt);},out _));
            Assert.Equal(100000,w.Bank);Assert.True(PdmStaffTransactions.PayAll(w,"payroll",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(49300,w.Pdm.Cash);
        }
        [Fact] public void PayrollTermsChangedAtDeadlineRequireAnotherConfirmation()
        {
            var w=W();w.Pdm.Staff.Employees.Add(Employee("mechanic",400));string before=JsonConvert.SerializeObject(w);
            Assert.False(PdmStaffTransactions.PayAll(w,"payroll",()=>throw new Exception(),out _,300));Assert.Equal(before,JsonConvert.SerializeObject(w));
            Assert.True(PdmStaffTransactions.PayAll(w,"payroll",()=>{},out _,400));Assert.Equal(49600,w.Pdm.Cash);
        }
        [Fact] public void PayrollSaveFailurePreservesUnknownFieldsAndAllDebts()
        {
            var w=W();w.Pdm.Staff.Employees.Add(Employee("mechanic",400));w.Pdm.Staff.FormerDebt=100;var j=JObject.FromObject(w);j["Pdm"]["Future"]="keep";w=j.ToObject<WalletData>();string before=JsonConvert.SerializeObject(w);
            Assert.Throws<Exception>(()=>PdmStaffTransactions.PayAll(w,"payroll",()=>throw new Exception(),out _));Assert.Equal(before,JsonConvert.SerializeObject(w));
        }
        [Fact] public void ImpossiblePayrollCannotPartiallyPayOrOverflow()
        {
            var w=W();w.Pdm.Staff.Employees.Add(Employee("mechanic",int.MaxValue));w.Pdm.Staff.Employees.Add(Employee("cleaner",int.MaxValue));
            string before=JsonConvert.SerializeObject(w);Assert.False(PdmStaffTransactions.PayAll(w,"payroll",()=>throw new Exception(),out _));Assert.Equal(before,JsonConvert.SerializeObject(w));
            w=W();Assert.False(PdmStaffTransactions.PayAll(w,"payroll",()=>throw new Exception(),out _));w.Pdm.Staff.Employees.Add(Employee("mechanic",1));w.Pdm.Staff.Paid=long.MaxValue;Assert.False(PdmStaffTransactions.PayAll(w,"payroll",()=>throw new Exception(),out _));
        }
        [Fact] public void ResultUsesActualOperatingCostsAndDoesNotCountCapitalAsRevenue()
        {
            var w=W();var d=w.Pdm;d.Revenue=1000;d.SoldCost=400;d.Repairs=50;d.Staff.Paid=100;d.Staff.HiringSpent=10;d.Staff.UpgradeSpent=20;d.Risk.Premiums=30;d.Risk.Stolen=100;d.Risk.Reimbursed=80;d.Invested=99999;d.Withdrawn=88888;
            Assert.Equal(370,PdmBusinessRules.Result(d));d.Revenue=long.MaxValue;d.SoldCost=long.MaxValue;d.Repairs=long.MaxValue;d.Staff.Paid=long.MaxValue;Assert.Equal(-(decimal)long.MaxValue*2-80,PdmBusinessRules.Result(d));
        }
        [Fact] public void DebtSummaryUsesLongWhenSeveralEmployeesExceedIntegerTotal(){var d=W().Pdm;d.Staff.Employees.Add(Employee("mechanic",int.MaxValue));d.Staff.Employees.Add(Employee("cleaner",int.MaxValue));d.Staff.FormerDebt=1;Assert.Equal((long)int.MaxValue*2+1,PdmBusinessRules.Wages(d));}
        [Fact] public void NegotiatedSaleCannotPayBeforeInspectionAndAgreement()
        {
            var w=Deal();Assert.False(PdmNegotiationTransactions.Accept(w,"deal",()=>throw new Exception(),out _));Assert.False(PdmNegotiationTransactions.CounterOffer(w,"deal",11200,()=>throw new Exception(),out _));Assert.False(PdmShowroomTransactions.Sell(w,"deal",()=>throw new Exception(),out _));Assert.Equal(50000,w.Pdm.Cash);Assert.Single(w.Pdm.Stock);
        }
        [Fact] public void AcceptOfferPaysOnlyOnKeyHandoverAndNeverTwice()
        {
            var w=Deal();Inspect(w);int price=w.Pdm.ShowroomSale.Negotiation.Offer;
            Assert.True(PdmNegotiationTransactions.Accept(w,"deal",()=>{},out _));Assert.Equal(50000,w.Pdm.Cash);Assert.Equal(price,w.Pdm.ShowroomSale.Price);
            Assert.True(PdmShowroomTransactions.Sell(w,"deal",()=>{},out _));Assert.Equal(50000+price,w.Pdm.Cash);Assert.Equal(price,w.Pdm.Revenue);Assert.Empty(w.Pdm.Stock);
            Assert.True(PdmShowroomTransactions.Sell(w,"deal",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(50000+price,w.Pdm.Cash);
        }
        [Fact] public void AcceptedCounterRespectsBuyersBudgetAndPersistsFixedPrice()
        {
            var w=Deal();Inspect(w);var n=w.Pdm.ShowroomSale.Negotiation;
            int price=n.Offer+1;if(price>n.Budget){n.Budget=price;}
            Assert.True(PdmNegotiationTransactions.CounterOffer(w,"deal",price,()=>{},out _));Assert.True(w.Pdm.ShowroomSale.Negotiation.Accepted);
            w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.Equal(price,w.Pdm.ShowroomSale.Price);Assert.True(PdmShowroomTransactions.Sell(w,"deal",()=>{},out _));Assert.Equal(50000+price,w.Pdm.Cash);
        }
        [Fact] public void RejectedCounterCannotBeAcceptedOrPaidButCancelPreservesStock()
        {
            var w=Deal();Inspect(w);var n=w.Pdm.ShowroomSale.Negotiation;n.Budget=n.Offer;
            Assert.True(PdmNegotiationTransactions.CounterOffer(w,"deal",n.Offer+1,()=>{},out _));Assert.True(w.Pdm.ShowroomSale.Negotiation.Rejected);
            Assert.False(PdmNegotiationTransactions.Accept(w,"deal",()=>throw new Exception(),out _));Assert.False(PdmShowroomTransactions.Sell(w,"deal",()=>throw new Exception(),out _));
            Assert.True(PdmShowroomTransactions.Cancel(w,"deal",()=>{},out _));Assert.Equal(50000,w.Pdm.Cash);Assert.Single(w.Pdm.Stock);Assert.Single(w.Pdm.Showroom);
        }
        [Theory][InlineData(0)][InlineData(-1)][InlineData(3000001)][InlineData(int.MaxValue)]
        public void InvalidCounterCannotMutateOffer(int price){var w=Deal();Inspect(w);string before=JsonConvert.SerializeObject(w);Assert.False(PdmNegotiationTransactions.CounterOffer(w,"deal",price,()=>throw new Exception(),out _));Assert.Equal(before,JsonConvert.SerializeObject(w));}
        [Theory][InlineData("inspect")][InlineData("accept")][InlineData("counter")]
        public void OfferCheckpointFailureRollsBackTheNegotiationAndUnknownMetadata(string step)
        {
            var w=Deal();if(step!="inspect")Inspect(w);var j=JObject.FromObject(w);j["Pdm"]["ShowroomSale"]["Negotiation"]["Future"]="keep";w=j.ToObject<WalletData>();string before=JsonConvert.SerializeObject(w);
            Assert.Throws<Exception>(()=>{if(step=="inspect")PdmNegotiationTransactions.Inspect(w,"deal",()=>throw new Exception(),out _);else if(step=="accept")PdmNegotiationTransactions.Accept(w,"deal",()=>throw new Exception(),out _);else PdmNegotiationTransactions.CounterOffer(w,"deal",11200,()=>throw new Exception(),out _);});
            Assert.Equal(before,JsonConvert.SerializeObject(w));
        }
        [Fact] public void WrongRequestCannotInspectOrNegotiateCurrentBuyer(){var w=Deal();Assert.False(PdmNegotiationTransactions.Inspect(w,"other",()=>throw new Exception(),out _));Inspect(w);Assert.False(PdmNegotiationTransactions.Accept(w,"other",()=>throw new Exception(),out _));Assert.False(PdmNegotiationTransactions.CounterOffer(w,"other",11200,()=>throw new Exception(),out _));}
        [Fact] public void LegacyPendingSaleRetainsItsExistingFixedPriceAndSchema()
        {
            var w=Display();Assert.True(PdmShowroomTransactions.BeginSale(w,"car","legacy",()=>{},out _));Assert.Null(w.Pdm.ShowroomSale.Negotiation);Assert.DoesNotContain("Negotiation",JsonConvert.SerializeObject(w.Pdm.ShowroomSale));int price=w.Pdm.ShowroomSale.Price;Assert.True(PdmShowroomTransactions.Sell(w,"legacy",()=>{},out _));Assert.Equal(50000+price,w.Pdm.Cash);
        }
        [Theory][InlineData(1)][InlineData(100)][InlineData(11200)][InlineData(3000000)]
        public void NegotiationGenerationIsBoundedAndStable(int price){var n=PdmNegotiationTransactions.Create(price,"buyer");Assert.True(n.Valid);Assert.Equal(JsonConvert.SerializeObject(n),JsonConvert.SerializeObject(PdmNegotiationTransactions.Create(price,"buyer")));Assert.InRange(n.Offer,1,price);Assert.InRange(n.Budget,n.Offer,3000000);}
        [Fact] public void InspectionRequiresContinuousEligibilityAndSupportsClockWrap()
        {
            var run=new PdmInspection();Assert.False(run.Update(100,true));Assert.False(run.Update(5000,true));Assert.False(run.Update(5100,false));Assert.False(run.Update(6000,true));Assert.False(run.Update(11999,true));Assert.True(run.Update(12000,true));
            run=new PdmInspection();Assert.False(run.Update(100,true));Assert.False(run.Update(99,true));Assert.False(run.Complete);
            run=new PdmInspection();int start=int.MaxValue-2000;Assert.False(run.Update(start,true));Assert.True(run.Update(unchecked(start+6000),true));
        }
        [Theory][InlineData(1)][InlineData(31)] public void InvalidInspectionDurationIsRejected(int seconds)=>Assert.Throws<ArgumentOutOfRangeException>(()=>new PdmInspection(seconds));
        [Theory][InlineData(42,123,999,true)][InlineData(43,123,999,false)][InlineData(42,124,999,false)][InlineData(42,123,1000,false)][InlineData(0,123,999,false)][InlineData(42,123,0,false)]
        public void EntityProofRejectsReusedHandleModelOrAddress(int handle,int model,long address,bool valid)=>Assert.Equal(valid,new PdmEntityProof(42,123,999).Matches(handle,model,address));
        [Fact] public void MissingEntityProofCannotAuthorizeAWorldOperation(){Assert.False(new PdmEntityProof(0,123,999).Matches(0,123,999));Assert.False(new PdmEntityProof(42,0,999).Matches(42,0,999));Assert.False(new PdmEntityProof(42,123,0).Matches(42,123,0));}
    }
}
