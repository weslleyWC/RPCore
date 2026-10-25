using System;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangIllicitTests
    {
        static readonly DateTime Now=new DateTime(2013,9,17,12,0,0);static readonly Action Save=()=>{};
        static GangTurfDefinition A()=>GangTurfOptions.Defaults().Areas[0];
        static GangState S()=>new GangState{Gang=A().DefaultOwner};
        static DealerOptions C()=>DealerDefaults.Load();
        static WalletData W()=>new WalletData{Bank=10000,Cash=123,Dealer=new DealerData{Stock={{"weed",5},{"mdma",5}}}};
        static int Quote(WalletData w)=>DealerRules.UnitPrice(C().Products.Find(p=>p.Id=="weed"),DealerRules.Reputation(w.Dealer,"DAVIS"),false);
        static bool Sale(WalletData w,string id="sale",Action save=null)=>GangIllicitTransactions.Sale(w,S(),A(),C(),"weed",1,"DAVIS",false,Quote(w),false,id,save??Save,out _);
        static WalletData Dirty(int amount=1000){var w=W();w.GangIllicit=new GangIllicitData();w.GangIllicit.Accounts.Add(new GangIllicitAccount{Gang=S().Gang,Area=1,Dirty=amount,Proceeds=amount});return w;}
        static bool Process(WalletData w,int amount=500,string id="batch",Action save=null)=>GangIllicitTransactions.Process(w,S(),A(),new GangLaunderingOptions(),amount,Now,id,save??Save,out _);
        [Fact] public void TerritorialSaleConsumesExistingStockAndCreditsOnlyDirtyAccount()
        {var w=W();Assert.True(Sale(w));Assert.Equal(4,w.Dealer.Stock["weed"]);Assert.Equal(1,w.Dealer.Sales);Assert.Equal(70,w.Dealer.Earned);Assert.Equal(5,w.Dealer.ZoneRep["DAVIS"]);Assert.Equal(70,w.GangIllicit.Accounts[0].Dirty);Assert.Equal(70,w.GangIllicit.Accounts[0].Proceeds);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangTreasury);}
        [Fact] public void NormalDealerSaleRetainsLegacyBankPaymentAndNoDirtyBook()
        {var w=W();Assert.True(DealerTransactions.Sale(w,C(),"weed",1,"DAVIS",false,Quote(w),true,"normal",Save,out _));Assert.Equal(10070,w.Bank);Assert.Null(w.GangIllicit);Assert.Equal(1,w.Dealer.Sessions);}
        [Fact] public void SaleWorksAtFullBankBecauseItDoesNotCreditBank()
        {var w=W();w.Bank=int.MaxValue;Assert.True(Sale(w));Assert.Equal(int.MaxValue,w.Bank);Assert.Equal(70,w.GangIllicit.Accounts[0].Dirty);}
        [Fact] public void SaleReplayAfterReloadCannotConsumeOrCreditTwice()
        {var w=W();Sale(w);w=CharacterRules.Copy(w);Assert.True(Sale(w,save:()=>throw new Exception()));Assert.Equal(4,w.Dealer.Stock["weed"]);Assert.Equal(70,w.GangIllicit.Accounts[0].Dirty);}
        [Fact] public void PersonalSaleReceiptCannotBeReusedToCreateDirtyRevenue()
        {var w=W();DealerTransactions.Sale(w,C(),"weed",1,"DAVIS",false,Quote(w),false,"sale",Save,out _);Assert.True(Sale(w));Assert.Null(w.GangIllicit);Assert.Equal(10070,w.Bank);}
        [Fact] public void TerritorialReceiptCannotLaterCreditPersonalBank()
        {var w=W();Sale(w);Assert.True(DealerTransactions.Sale(w,C(),"weed",1,"DAVIS",false,Quote(w),false,"sale",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(10000,w.Bank);Assert.Equal(70,w.GangIllicit.Accounts[0].Dirty);}
        [Fact] public void FreightReservationBlocksBothTerritorialSaleAndProcessing()
        {var w=Dirty();Assert.True(GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"freight",Save,out _));Assert.False(Sale(w));Assert.False(Process(w));Assert.Equal(5,w.Dealer.Stock["weed"]);Assert.Equal(1000,w.GangIllicit.Accounts[0].Dirty);}
        [Fact] public void InvalidQuoteOrMissingStockCannotCreateRevenue()
        {var w=W();Assert.False(GangIllicitTransactions.Sale(w,S(),A(),C(),"weed",1,"DAVIS",false,999,false,"bad",Save,out _));w.Dealer.Stock.Clear();Assert.False(Sale(w));Assert.Null(w.GangIllicit);}
        [Fact] public void MissingAffiliationOrWrongOwnerCannotSellForGang()
        {var w=W();Assert.False(GangIllicitTransactions.Sale(w,new GangState(),A(),C(),"weed",1,"DAVIS",false,Quote(w),false,"bad",Save,out _));var s=S();s.Turf=new GangTurfData();s.Turf.Owners[1]="Rival";Assert.False(GangIllicitTransactions.Sale(w,s,A(),C(),"weed",1,"DAVIS",false,Quote(w),false,"bad",Save,out _));Assert.Equal(5,w.Dealer.Stock["weed"]);}
        [Fact] public void DirtyBalanceOverflowRejectsSaleWithoutConsumingInventory()
        {var w=Dirty(int.MaxValue);Assert.False(Sale(w));Assert.Equal(5,w.Dealer.Stock["weed"]);Assert.Equal(int.MaxValue,w.GangIllicit.Accounts[0].Dirty);}
        [Fact] public void FailedSaleRestoresDealerAndDirtyBookReferencesAndBank()
        {var w=Dirty();var d=w.Dealer;var b=w.GangIllicit;var other=w.GangBusiness=new GangBusinessData();Assert.Throws<Exception>(()=>Sale(w,save:()=>throw new Exception()));Assert.Same(d,w.Dealer);Assert.Same(b,w.GangIllicit);Assert.Same(other,w.GangBusiness);Assert.Equal(5,d.Stock["weed"]);Assert.Equal(1000,b.Accounts[0].Dirty);Assert.Equal(10000,w.Bank);}
        [Fact] public void FailedFirstSaleDoesNotCreateDirtyBook()
        {var w=W();Assert.Throws<Exception>(()=>Sale(w,save:()=>throw new Exception()));Assert.Null(w.GangIllicit);Assert.Equal(5,w.Dealer.Stock["weed"]);}
        [Fact] public void BatchTransfers85PercentToTreasuryAndAccountsFor15PercentFee()
        {var w=Dirty();Assert.True(Process(w));var a=w.GangIllicit.Accounts[0];Assert.Equal(500,a.Dirty);Assert.Equal(500,a.Processed);Assert.Equal(425,a.Net);Assert.Equal(75,a.Fees);var t=GangTreasuryTransactions.Account(w.GangTreasury,S().Gang);Assert.Equal(425,t.Balance);Assert.Equal(425,t.LaunderedIncome);Assert.Equal(Now.Ticks,t.LastPaydayTicks);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);}
        [Fact] public void NoDirtySourceMeansNoFreeCleanMoney()
        {var w=W();Assert.False(Process(w));Assert.Null(w.GangTreasury);Assert.Null(w.GangIllicit);w=Dirty(0);Assert.False(Process(w,1));Assert.Null(w.GangTreasury);}
        [Fact] public void DirtyAndProcessedBooksRemainConservedAcrossBatches()
        {var w=Dirty();Process(w);Assert.True(Process(w,id:"two"));var a=w.GangIllicit.Accounts[0];Assert.Equal(0,a.Dirty);Assert.Equal(1000,a.Processed);Assert.Equal(850,a.Net);Assert.Equal(150,a.Fees);Assert.Equal(850,GangTreasuryTransactions.Account(w.GangTreasury,S().Gang).Balance);Assert.False(Process(w,1,"empty"));}
        [Fact] public void CumulativeRoundingGivesSameNetForOneDollarAndWholeBatches()
        {var w=Dirty(100);for(int i=0;i<100;i++)Assert.True(Process(w,1,"one"+i));Assert.Equal(85,w.GangIllicit.Accounts[0].Net);Assert.Equal(15,w.GangIllicit.Accounts[0].Fees);Assert.Equal(85,GangTreasuryTransactions.Account(w.GangTreasury,S().Gang).Balance);var whole=Dirty(100);Process(whole,100);Assert.Equal(whole.GangIllicit.Accounts[0].Net,w.GangIllicit.Accounts[0].Net);}
        [Fact] public void FirstOneDollarRoundsDownWithoutCreatingEmptyTreasury()
        {var w=Dirty(1);Assert.True(Process(w,1));Assert.Null(w.GangTreasury);Assert.Equal(1,w.GangIllicit.Accounts[0].Fees);Assert.Equal(0,w.GangIllicit.Accounts[0].Dirty);}
        [Fact] public void ProcessReplayAfterReloadCannotChargeOrCreditTwice()
        {var w=Dirty();Process(w);w=CharacterRules.Copy(w);Assert.True(Process(w,save:()=>throw new Exception()));Assert.Equal(500,w.GangIllicit.Accounts[0].Dirty);Assert.Equal(425,GangTreasuryTransactions.Account(w.GangTreasury,S().Gang).Balance);}
        [Fact] public void CleanIncomeCanFundLegacyWithdrawalAndRankShare()
        {var w=Dirty();Process(w);Assert.True(GangTreasuryTransactions.Withdraw(w,S().Gang,100,new GangTreasuryOptions(),"out",Save,out _));Assert.True(GangTreasuryTransactions.Payday(w,S().Gang,2,Now.AddDays(1),new GangTreasuryOptions(),"pay",Save,out _));Assert.Equal(10132,w.Bank);Assert.Equal(293,GangTreasuryTransactions.Account(w.GangTreasury,S().Gang).Balance);Assert.True(w.GangTreasury.Valid);}
        [Fact] public void ExistingPaydayAnchorAndOtherIncomeArePreserved()
        {var w=Dirty();GangTreasuryTransactions.Deposit(w,S().Gang,1000,Now.AddHours(-24),new GangTreasuryOptions(),"fund",Save,out _);Process(w);var t=GangTreasuryTransactions.Account(w.GangTreasury,S().Gang);Assert.Equal(1425,t.Balance);Assert.Equal(1000,t.Deposited);Assert.Equal(Now.AddHours(-24).Ticks,t.LastPaydayTicks);}
        [Fact] public void FailedProcessRestoresBothBooksAndOtherWalletReferences()
        {var w=Dirty();GangTreasuryTransactions.Deposit(w,S().Gang,100,Now,new GangTreasuryOptions(),"fund",Save,out _);var b=w.GangIllicit;var t=w.GangTreasury;var dealer=w.Dealer;Assert.Throws<Exception>(()=>Process(w,save:()=>throw new Exception()));Assert.Same(b,w.GangIllicit);Assert.Same(t,w.GangTreasury);Assert.Same(dealer,w.Dealer);Assert.Equal(1000,b.Accounts[0].Dirty);Assert.Equal(9900,w.Bank);Assert.Equal(100,t.Accounts[S().Gang].Balance);}
        [Fact] public void FailedFirstProcessKeepsTreasuryAbsentAndDirtyUntouched()
        {var w=Dirty();Assert.Throws<Exception>(()=>Process(w,save:()=>throw new Exception()));Assert.Null(w.GangTreasury);Assert.Equal(1000,w.GangIllicit.Accounts[0].Dirty);}
        [Fact] public void FullTreasuryRejectsProcessWithoutBurningDirtyCash()
        {var w=Dirty();w.GangTreasury=new GangTreasuryData();w.GangTreasury.Accounts[S().Gang]=new GangTreasuryAccount{Balance=int.MaxValue,Deposited=int.MaxValue};Assert.False(Process(w));Assert.Equal(1000,w.GangIllicit.Accounts[0].Dirty);}
        [Fact] public void LostOwnershipOrChangedAffiliationLocksSourceWithoutDeletingIt()
        {var w=Dirty();var s=S();s.Turf=new GangTurfData();s.Turf.Owners[1]="Rival";Assert.False(GangIllicitTransactions.Process(w,s,A(),new GangLaunderingOptions(),500,Now,"bad",Save,out _));s.Gang="Rival";Assert.False(GangIllicitTransactions.Process(w,s,A(),new GangLaunderingOptions(),500,Now,"bad",Save,out _));Assert.Equal(1000,w.GangIllicit.Accounts[0].Dirty);Assert.Null(w.GangTreasury);}
        [Fact] public void AreasGangsAndProfilesNeverShareDirtyAccounts()
        {var w=Dirty();var other=GangTurfOptions.Defaults().Areas[1];Assert.False(GangIllicitTransactions.Process(w,S(),other,new GangLaunderingOptions(),1,Now,"other-area",Save,out _));Assert.Null(GangIllicitTransactions.Account(w.GangIllicit,"Other",1));Assert.False(Process(W(),1));Assert.Equal(1000,w.GangIllicit.Accounts[0].Dirty);}
        [Fact] public void CaseInsensitiveGangNameDoesNotDuplicateSourceOrTreasury()
        {var w=Dirty();var s=S();s.Gang=s.Gang.ToUpperInvariant();Assert.True(GangIllicitTransactions.Process(w,s,A(),new GangLaunderingOptions(),500,Now,"case",Save,out _));Assert.Single(w.GangIllicit.Accounts);Assert.Single(w.GangTreasury.Accounts);}
        [Fact] public void UnknownFieldsSurviveSaleAndProcessAcrossBooks()
        {var w=Dirty();var j=JObject.FromObject(w);j["Future"]="wallet";j["GangIllicit"]["Future"]="book";j["GangIllicit"]["Accounts"][0]["Future"]="account";j["Dealer"]["Future"]="dealer";w=j.ToObject<WalletData>();Sale(w);Process(w);j=JObject.FromObject(w);Assert.Equal("wallet",(string)j["Future"]);Assert.Equal("book",(string)j["GangIllicit"]["Future"]);Assert.Equal("account",(string)j["GangIllicit"]["Accounts"][0]["Future"]);Assert.Equal("dealer",(string)j["Dealer"]["Future"]);}
        [Fact] public void InvalidSourceConservationOrFeeCannotBeProcessed()
        {var w=Dirty();w.GangIllicit.Accounts[0].Dirty=1001;Assert.False(Process(w));Assert.False(Sale(w));w=Dirty();w.GangIllicit.Accounts[0].Fees=1;Assert.False(Process(w));Assert.Null(w.GangTreasury);}
        [Fact] public void DuplicateSourceKeysAndInvalidReceiptBookAreProtected()
        {var w=Dirty();w.GangIllicit.Accounts.Add(CharacterRules.Copy(w.GangIllicit.Accounts[0]));Assert.False(w.GangIllicit.Valid);Assert.False(Process(w));w=Dirty();w.GangIllicit.Receipts=null;Assert.False(Process(w));}
        [Fact] public void LegacyTreasuryMissingNewIncomeFieldRemainsValid()
        {var a=JObject.Parse("{Balance:1125,Deposited:1000,BusinessIncome:175,Salary:50}").ToObject<GangTreasuryAccount>();Assert.True(a.Valid);Assert.Equal(0,a.LaunderedIncome);}
        [Theory][InlineData(0)][InlineData(-1)][InlineData(501)]
        public void InvalidBatchAmountCannotDebit(int amount){var w=Dirty();Assert.False(Process(w,amount));Assert.Equal(1000,w.GangIllicit.Accounts[0].Dirty);}
        [Fact] public void BadConfigOrMissingReceiptDoesNotCreateTreasury()
        {var w=Dirty();Assert.False(GangIllicitTransactions.Process(w,S(),A(),new GangLaunderingOptions{Seconds=0},1,Now,"bad",Save,out _));Assert.False(Process(w,1,id:null));Assert.Null(w.GangTreasury);}
    }
}
