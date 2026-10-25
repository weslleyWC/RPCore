using System;
using Newtonsoft.Json.Linq;
using Xunit;

namespace RPCore.Tests
{
    public class GangBusinessTests
    {
        static readonly DateTime Now=new DateTime(2013,9,17,12,0,0);
        static readonly Action Save=()=>{};
        static GangBusinessOptions C()=>new GangBusinessOptions();
        static GangTurfDefinition Area(int id=1){var a=GangTurfOptions.Defaults().Areas[id-1];return a;}
        static GangState S()=>new GangState{Gang=Area().DefaultOwner};
        static WalletData W()=>new WalletData{Bank=10000,Cash=123};
        static bool Trade(WalletData w,bool sale,string id="op",GangState s=null,int area=1,Action save=null)=>GangBusinessTransactions.Trade(w,s??S(),Area(area),C(),sale,Now,id,save??Save,out _);
        static WalletData Stock(){var w=W();Assert.True(Trade(w,false,"buy"));return w;}
        [Fact] public void BuyingCostsActualBankAndCreatesOnlyLocalStock()
        {var w=Stock();Assert.Equal(9800,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangTreasury);var a=GangBusinessTransactions.Account(w.GangBusiness,S().Gang,1);Assert.Equal(10,a.Stock);Assert.Equal(10,a.Purchased);Assert.Null(GangBusinessTransactions.Account(w.GangBusiness,S().Gang,2));}
        [Fact] public void SaleConsumesFiveAndCreditsRevenueToTreasuryRatherThanBank()
        {var w=Stock();Assert.True(Trade(w,true));var a=w.GangBusiness.Accounts[0];Assert.Equal(5,a.Stock);Assert.Equal(5,a.Sold);Assert.Equal(175,a.Revenue);Assert.Equal(9800,w.Bank);var t=GangTreasuryTransactions.Account(w.GangTreasury,S().Gang);Assert.Equal(175,t.Balance);Assert.Equal(175,t.BusinessIncome);Assert.Equal(Now.Ticks,t.LastPaydayTicks);Assert.True(t.Valid);}
        [Fact] public void ExistingDepositsAndNewSalesBothFundWithdrawalAndRankPayday()
        {var w=Stock();GangTreasuryTransactions.Deposit(w,S().Gang,1000,Now.AddHours(-24),new GangTreasuryOptions(),"fund",Save,out _);Trade(w,true);var t=GangTreasuryTransactions.Account(w.GangTreasury,S().Gang);Assert.Equal(1175,t.Balance);Assert.Equal(Now.AddHours(-24).Ticks,t.LastPaydayTicks);Assert.True(GangTreasuryTransactions.Payday(w,S().Gang,2,Now,new GangTreasuryOptions(),"pay",Save,out _));Assert.Equal(8917,w.Bank);Assert.Equal(1058,GangTreasuryTransactions.Account(w.GangTreasury,S().Gang).Balance);Assert.True(GangTreasuryTransactions.Withdraw(w,S().Gang,1058,new GangTreasuryOptions(),"out",Save,out _));Assert.Equal(9975,w.Bank);Assert.True(w.GangTreasury.Valid);}
        [Fact] public void TwoBatchesCannotSellMoreThanPurchased()
        {var w=Stock();Assert.True(Trade(w,true,"one"));Assert.True(Trade(w,true,"two"));Assert.False(Trade(w,true,"three"));Assert.Equal(0,w.GangBusiness.Accounts[0].Stock);Assert.Equal(350,GangTreasuryTransactions.Account(w.GangTreasury,S().Gang).Balance);}
        [Fact] public void NoStockMeansNoFreeRevenueOrEmptyBookCreation()
        {var w=W();Assert.False(Trade(w,true));Assert.Null(w.GangBusiness);Assert.Null(w.GangTreasury);Assert.Equal(10000,w.Bank);}
        [Fact] public void PurchaseCapacityAndInsufficientBankRejectWithoutPartialDebit()
        {var w=Stock();for(int i=1;i<10;i++)Assert.True(Trade(w,false,"buy"+i));Assert.False(Trade(w,false,"full"));Assert.Equal(8000,w.Bank);w=W();w.Bank=199;Assert.False(Trade(w,false));Assert.Null(w.GangBusiness);Assert.Equal(199,w.Bank);}
        [Fact] public void ReplayAfterReloadCannotRepeatStockCostOrSaleRevenue()
        {var w=CharacterRules.Copy(Stock());Assert.True(Trade(w,false,"buy",save:()=>throw new Exception()));Assert.Equal(9800,w.Bank);Trade(w,true,"sold");w=CharacterRules.Copy(w);Assert.True(Trade(w,true,"sold",save:()=>throw new Exception()));Assert.Equal(175,w.GangBusiness.Accounts[0].Revenue);}
        [Fact] public void FailedPurchaseSaveRestoresAbsentBookBankAndOtherReferences()
        {var w=W();w.Dmv=new DmvData();var d=w.Dmv;Assert.Throws<Exception>(()=>Trade(w,false,save:()=>throw new Exception()));Assert.Equal(10000,w.Bank);Assert.Null(w.GangBusiness);Assert.Null(w.GangTreasury);Assert.Same(d,w.Dmv);}
        [Fact] public void FailedSaleSaveRestoresBothOriginalBooksAndBank()
        {var w=Stock();GangTreasuryTransactions.Deposit(w,S().Gang,100,Now,new GangTreasuryOptions(),"fund",Save,out _);var b=w.GangBusiness;var t=w.GangTreasury;Assert.Throws<Exception>(()=>Trade(w,true,save:()=>throw new Exception()));Assert.Same(b,w.GangBusiness);Assert.Same(t,w.GangTreasury);Assert.Equal(9700,w.Bank);Assert.Equal(10,b.Accounts[0].Stock);Assert.Equal(100,t.Accounts[S().Gang].Balance);}
        [Fact] public void FailedFirstSaleDoesNotPublishTreasuryOrConsumeStock()
        {var w=Stock();Assert.Throws<Exception>(()=>Trade(w,true,save:()=>throw new Exception()));Assert.Null(w.GangTreasury);Assert.Equal(10,w.GangBusiness.Accounts[0].Stock);}
        [Fact] public void ChangedOwnerOrAffiliationBlocksTradeAndRetainsOldStock()
        {var w=Stock();var s=S();s.Turf=new GangTurfData();s.Turf.Owners[1]="Rival";Assert.False(Trade(w,true,s:s));Assert.False(Trade(w,false,"new",s));s.Gang="Rival";Assert.False(Trade(w,true,"rival",s));Assert.Equal(10,w.GangBusiness.Accounts[0].Stock);Assert.Null(w.GangTreasury);}
        [Fact] public void GangAndAreaAccountsAreIndependentAndNamesIgnoreCase()
        {var w=Stock();Assert.True(Trade(w,false,"other-area",area:2));var s=S();s.Gang=s.Gang.ToUpperInvariant();Assert.True(Trade(w,true,"case",s));Assert.Equal(2,w.GangBusiness.Accounts.Count);Assert.Equal(5,GangBusinessTransactions.Account(w.GangBusiness,s.Gang,1).Stock);Assert.Equal(10,GangBusinessTransactions.Account(w.GangBusiness,s.Gang,2).Stock);}
        [Fact] public void ProfileWalletsNeverShareStockRevenueOrReceipts()
        {var w=Stock();var other=W();Trade(w,true);Assert.False(Trade(other,true));Assert.Null(other.GangBusiness);Assert.Null(other.GangTreasury);}
        [Fact] public void InvalidOwnershipAndPendingDisputeCannotStartTrade()
        {var w=W();Assert.False(Trade(w,false,s:new GangState()));var s=S();s.Turf=new GangTurfData{Pending=new GangTurfAttempt()};Assert.False(Trade(w,false,s:s));Assert.Null(w.GangBusiness);}
        [Fact] public void InvalidBusinessOrTreasuryLedgerIsProtected()
        {var w=Stock();w.GangBusiness.Accounts[0].Stock=11;Assert.False(Trade(w,true));Assert.Null(w.GangTreasury);w=Stock();w.GangTreasury=new GangTreasuryData();w.GangTreasury.Accounts[S().Gang]=new GangTreasuryAccount{Balance=1};Assert.False(Trade(w,true));Assert.Equal(10,w.GangBusiness.Accounts[0].Stock);}
        [Fact] public void DuplicateCaseVariantAndNullAccountsAreInvalid()
        {var w=Stock();var copy=CharacterRules.Copy(w.GangBusiness.Accounts[0]);copy.Gang=copy.Gang.ToUpperInvariant();w.GangBusiness.Accounts.Add(copy);Assert.False(w.GangBusiness.Valid);w=Stock();w.GangBusiness.Accounts.Add(null);Assert.False(w.GangBusiness.Valid);}
        [Fact] public void TreasuryCapacityOverflowCannotDestroyStock()
        {var w=Stock();w.GangTreasury=new GangTreasuryData();w.GangTreasury.Accounts[S().Gang]=new GangTreasuryAccount{Balance=int.MaxValue,Deposited=int.MaxValue};Assert.False(Trade(w,true));Assert.Equal(10,w.GangBusiness.Accounts[0].Stock);}
        [Fact] public void LifetimeIncomeOverflowIsRejectedEvenWithSmallBalance()
        {var w=Stock();w.GangTreasury=new GangTreasuryData();w.GangTreasury.Accounts[S().Gang]=new GangTreasuryAccount{Balance=0,BusinessIncome=long.MaxValue,Withdrawn=long.MaxValue};Assert.True(w.GangTreasury.Valid);Assert.False(Trade(w,true));Assert.Equal(10,w.GangBusiness.Accounts[0].Stock);}
        [Fact] public void UnknownMetadataSurvivesPurchaseAndSaleInBothBooks()
        {var w=Stock();Trade(w,true,"first");var j=JObject.FromObject(w);j["Future"]="wallet";j["GangBusiness"]["Future"]="business";j["GangBusiness"]["Accounts"][0]["Future"]="stock";j["GangTreasury"]["Accounts"][S().Gang]["Future"]="treasury";w=j.ToObject<WalletData>();Trade(w,false,"second");Trade(w,true,"third");j=JObject.FromObject(w);Assert.Equal("wallet",(string)j["Future"]);Assert.Equal("business",(string)j["GangBusiness"]["Future"]);Assert.Equal("stock",(string)j["GangBusiness"]["Accounts"][0]["Future"]);Assert.Equal("treasury",(string)j["GangTreasury"]["Accounts"][S().Gang]["Future"]);}
        [Fact] public void LegacyTreasuryMissingBusinessIncomeRemainsConserved()
        {var t=JObject.Parse("{Balance:950,Deposited:1000,Withdrawn:0,Salary:50}").ToObject<GangTreasuryAccount>();Assert.True(t.Valid);Assert.Equal(0,t.BusinessIncome);}
        [Fact] public void CombinedLifetimeCountersDoNotOverflowDuringValidation()
        {var t=new GangTreasuryAccount{Deposited=long.MaxValue,BusinessIncome=long.MaxValue,Withdrawn=long.MaxValue,Salary=long.MaxValue};Assert.True(t.Valid);t.BusinessIncome=-1;Assert.False(t.Valid);}
        [Fact] public void InvalidConfigAndOperationIdCannotCreateBook()
        {var w=W();Assert.False(GangBusinessTransactions.Trade(w,S(),Area(),new GangBusinessOptions{SaleUnits=0},false,Now,"bad",Save,out _));Assert.False(Trade(w,false,id:null));Assert.Null(w.GangBusiness);Assert.Equal(10000,w.Bank);}
        [Fact] public void StockReceiptsAreBoundedAndReplayDoesNotSave()
        {var w=Stock();for(int i=0;i<256;i++)w.GangBusiness.Receipts.Add("old"+i);w.GangBusiness.Receipts.RemoveAt(0);Assert.True(Trade(w,false,"next"));Assert.Equal(256,w.GangBusiness.Receipts.Count);Assert.True(GangBusinessTransactions.Trade(w,S(),Area(),C(),false,Now,"next",()=>throw new Exception(),out bool changed));Assert.False(changed);}
        [Fact] public void ServiceRequiresActualAccumulatedTimeAndExplicitReadyThreshold()
        {var s=new GangBusinessService(0);for(int n=1000;n<=44000;n+=1000)s.Step(n,true);Assert.False(s.Ready(45));s.Step(45000,true);Assert.True(s.Ready(45));}
        [Fact] public void MenusPauseAndLongFrameGapsCannotCompleteService()
        {var s=new GangBusinessService(0);s.Step(1000,true);s.Step(46000,true);s.Step(47000,false);s.Step(48000,true);Assert.Equal(2000,s.Milliseconds);Assert.False(s.Ready(45));}
        [Fact] public void RuntimeRestartNeverRestoresUnconfirmedServiceTime()
        {var s=new GangBusinessService(0);for(int n=1000;n<=45000;n+=1000)s.Step(n,true);Assert.True(s.Ready(45));s=new GangBusinessService(45000);Assert.False(s.Ready(45));Assert.Equal(0,s.Milliseconds);}
        [Fact] public void TimerHandlesGameTimeWrapAndCapsAccumulation()
        {var s=new GangBusinessService(int.MaxValue-500);s.Step(unchecked(int.MaxValue+500),true);Assert.Equal(1000,s.Milliseconds);var timer=new GangBusinessService(0);for(int i=1000;i<=700000;i+=1000)timer.Step(i,true);Assert.Equal(600000,timer.Milliseconds);}
        [Theory][InlineData(0)][InlineData(9)][InlineData(601)]
        public void InvalidServiceDurationsNeverReady(int duration){Assert.False(new GangBusinessService(0).Ready(duration));}
    }
}
