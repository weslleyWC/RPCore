using System;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangAutoLaunderTests
    {
        static readonly Action Save=()=>{};static readonly DateTime Now=new DateTime(2013,9,17,12,0,0);
        static GangTurfDefinition A()=>GangTurfOptions.Defaults().Areas[0];
        static GangState S()=>new GangState{Gang=A().DefaultOwner,Reputation=800,Rank=3};
        static WalletData W(int dirty=1500){var w=new WalletData{Bank=10000,Cash=123,Dealer=new DealerData{Stock={{"weed",5}}},GangIllicit=new GangIllicitData()};w.GangIllicit.Accounts.Add(new GangIllicitAccount{Gang=S().Gang,Area=A().Id,Dirty=dirty,Proceeds=dirty});return w;}
        static bool Set(WalletData w,bool enabled=true,string id="set",Action save=null)=>GangAutoLaunderTransactions.Configure(w,S(),A(),enabled,id,save??Save,out _);
        static WalletData Enabled(int dirty=1500){var w=W(dirty);Assert.True(Set(w));return w;}
        static bool Process(WalletData w,string id="process",GangAutoLaunderOptions c=null,Action save=null)=>GangAutoLaunderTransactions.Process(w,S(),A(),c??new GangAutoLaunderOptions(),Now,id,save??Save,out _);
        static GangAutoLaunderAccount Auto(WalletData w)=>GangAutoLaunderTransactions.Account(w.GangAutoLaunder,S().Gang,A().Id);
        static GangIllicitAccount Income(WalletData w)=>GangIllicitTransactions.Account(w.GangIllicit,S().Gang,A().Id);
        [Fact] public void DefaultsAreFiveHundredPerMinuteAndInitiallyAbsent()
        {var c=new GangAutoLaunderOptions();Assert.True(c.Valid);Assert.Equal(500,c.BatchAmount);Assert.Equal(60,c.IntervalSeconds);var w=W();Assert.Null(w.GangAutoLaunder);Assert.False(Process(w));Assert.Null(w.GangAutoLaunder);Assert.Null(w.GangTreasury);}
        [Fact] public void ConfigurationDoesNotMoveDirtyCashOrCreateTreasury()
        {var w=W();var dirty=w.GangIllicit;var dealer=w.Dealer;Assert.True(Set(w));Assert.True(Auto(w).Enabled);Assert.Equal(0,Auto(w).Batches);Assert.Same(dirty,w.GangIllicit);Assert.Same(dealer,w.Dealer);Assert.Null(w.GangTreasury);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);}
        [Fact] public void ConfigureReplayAndAlreadyEnabledDoNotSaveAgain()
        {var w=Enabled();Assert.True(Set(w,save:()=>throw new Exception()));Assert.True(Set(w,id:"same",save:()=>throw new Exception()));Assert.Single(w.GangAutoLaunder.Accounts);}
        [Fact] public void ConfigureCanWaitForFutureIncomeWithoutCreatingMoney()
        {var w=W(0);w.GangIllicit=null;Assert.True(Set(w));Assert.False(Process(w));Assert.Null(w.GangTreasury);Assert.Null(w.GangIllicit);}
        [Fact] public void OneCycleConsumesOnlyExistingDirtyAndCreditsEightyFivePercent()
        {var w=Enabled();var dealer=w.Dealer;Assert.True(Process(w));Assert.Equal(1000,Income(w).Dirty);Assert.Equal(500,Income(w).Processed);Assert.Equal(425,Income(w).Net);Assert.Equal(75,Income(w).Fees);Assert.Equal(1,Auto(w).Batches);Assert.Equal(500,Auto(w).Processed);Assert.Equal(425,Auto(w).Net);Assert.Equal(75,Auto(w).Fees);var t=GangTreasuryTransactions.Account(w.GangTreasury,S().Gang);Assert.Equal(425,t.Balance);Assert.Equal(425,t.LaunderedIncome);Assert.Equal(Now.Ticks,t.LastPaydayTicks);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Same(dealer,w.Dealer);Assert.Null(w.GangArms);Assert.Null(w.GangWarehouse);Assert.Null(w.GangDiplomacy);}
        [Fact] public void RepeatedDistinctCyclesNeverExceedAvailableDirty()
        {var w=Enabled();for(int i=0;i<3;i++)Assert.True(Process(w,"process"+i));Assert.False(Process(w,"empty"));Assert.Equal(0,Income(w).Dirty);Assert.Equal(1500,Auto(w).Processed);Assert.Equal(1275,Auto(w).Net);Assert.Equal(225,Auto(w).Fees);Assert.Equal(1275,GangTreasuryTransactions.Account(w.GangTreasury,S().Gang).Balance);}
        [Fact] public void SmallRemainderUsesMinDirtyRatherThanFullBatch()
        {var w=Enabled(125);Assert.True(Process(w));Assert.Equal(0,Income(w).Dirty);Assert.Equal(125,Auto(w).Processed);Assert.Equal(106,Auto(w).Net);Assert.Equal(19,Auto(w).Fees);Assert.Equal(106,GangTreasuryTransactions.Account(w.GangTreasury,S().Gang).Balance);}
        [Fact] public void ReplayAfterReloadDoesNotDebitOrCreditAgain()
        {var w=Enabled();Process(w);w=CharacterRules.Copy(w);Assert.True(Process(w,save:()=>throw new Exception()));Assert.Equal(1000,Income(w).Dirty);Assert.Equal(425,GangTreasuryTransactions.Account(w.GangTreasury,S().Gang).Balance);Assert.Equal(1,Auto(w).Batches);}
        [Fact] public void ManualAndAutomaticShareCumulativeRoundingWithoutExtraLoss()
        {var w=Enabled(20);Assert.True(GangIllicitTransactions.Process(w,S(),A(),new GangLaunderingOptions(),1,Now,"manual",Save,out _));Assert.Null(w.GangTreasury);Assert.True(Process(w,c:new GangAutoLaunderOptions{BatchAmount=1}));Assert.Equal(1,Auto(w).Processed);Assert.Equal(1,Auto(w).Net);Assert.Equal(0,Auto(w).Fees);Assert.Equal(2,Income(w).Processed);Assert.Equal(1,Income(w).Net);for(int i=0;i<18;i++)Assert.True(Process(w,"small"+i,new GangAutoLaunderOptions{BatchAmount=1}));Assert.Equal(17,Income(w).Net);Assert.Equal(3,Income(w).Fees);Assert.Equal(17,GangTreasuryTransactions.Account(w.GangTreasury,S().Gang).Balance);Assert.True(w.GangAutoLaunder.Valid);}
        [Fact] public void OneDollarCycleCanChargeOnlyFeeUntilCumulativeRemainderPays()
        {var w=Enabled(2);Assert.True(Process(w,c:new GangAutoLaunderOptions{BatchAmount=1}));Assert.Equal(1,Auto(w).Processed);Assert.Equal(0,Auto(w).Net);Assert.Equal(1,Auto(w).Fees);Assert.Null(w.GangTreasury);Assert.True(Process(w,"next",new GangAutoLaunderOptions{BatchAmount=1}));Assert.Equal(1,Auto(w).Net);Assert.Equal(1,GangTreasuryTransactions.Account(w.GangTreasury,S().Gang).Balance);}
        [Fact] public void ExistingTreasuryBalanceAndPaydayAnchorArePreserved()
        {var w=Enabled();w.GangTreasury=new GangTreasuryData();w.GangTreasury.Accounts[S().Gang]=new GangTreasuryAccount{Balance=1000,Deposited=1000,LastPaydayTicks=Now.AddDays(-1).Ticks};Assert.True(Process(w));var t=GangTreasuryTransactions.Account(w.GangTreasury,S().Gang);Assert.Equal(1425,t.Balance);Assert.Equal(Now.AddDays(-1).Ticks,t.LastPaydayTicks);Assert.Equal(1000,t.Deposited);}
        [Fact] public void StopPreservesBalancesAndWorksWithoutCurrentOwnershipParameter()
        {var w=Enabled();Process(w);var dirty=w.GangIllicit;var treasury=w.GangTreasury;Assert.True(GangAutoLaunderTransactions.Stop(w,S().Gang,A().Id,"stop",Save,out _));Assert.False(Auto(w).Enabled);Assert.False(Process(w,"later"));Assert.Same(dirty,w.GangIllicit);Assert.Same(treasury,w.GangTreasury);Assert.Equal(425,Auto(w).Net);}
        [Fact] public void StopReplayAndAlreadyDisabledDoNotSaveAgain()
        {var w=Enabled();Assert.True(GangAutoLaunderTransactions.Stop(w,S().Gang,A().Id,"stop",Save,out _));Assert.True(GangAutoLaunderTransactions.Stop(w,S().Gang,A().Id,"stop",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.True(GangAutoLaunderTransactions.Stop(w,S().Gang,A().Id,"same",()=>throw new Exception(),out changed));Assert.False(changed);}
        [Fact] public void LossOfOwnerOrDifferentAreaPreventsProcessingWithoutDeletingDirty()
        {var w=Enabled();var s=S();s.Gang="Other";Assert.False(GangAutoLaunderTransactions.Process(w,s,A(),new GangAutoLaunderOptions(),Now,"owner",Save,out _));Assert.False(GangAutoLaunderTransactions.Process(w,S(),GangTurfOptions.Defaults().Areas[1],new GangAutoLaunderOptions(),Now,"area",Save,out _));Assert.Equal(1500,Income(w).Dirty);Assert.Null(w.GangTreasury);}
        [Fact] public void PendingBulkRaidOrFreightBlocksProcessingAndNewActivation()
        {var w=Enabled();w.GangRaid=new GangRaidData{Attempts=-1};Assert.False(Process(w));Assert.False(Set(w,id:"again"));w.GangRaid=null;w.GangBusiness=new GangBusinessData{Supply=new GangSupplyOrder()};Assert.False(Process(w));w.GangBusiness=null;w.GangBulk=new GangBulkData{Attempts=-1};Assert.False(Process(w));Assert.True(GangAutoLaunderTransactions.Stop(w,S().Gang,A().Id,"stop",Save,out _));}
        [Fact] public void FailedFirstSettingSaveRestoresAbsentBook()
        {var w=W();Assert.Throws<Exception>(()=>Set(w,save:()=>throw new Exception()));Assert.Null(w.GangAutoLaunder);Assert.Equal(1500,Income(w).Dirty);}
        [Fact] public void FailedSettingAndStopRestoreOriginalReference()
        {var w=Enabled();var old=w.GangAutoLaunder;Assert.Throws<Exception>(()=>Set(w,false,"off",()=>throw new Exception()));Assert.Same(old,w.GangAutoLaunder);Assert.Throws<Exception>(()=>GangAutoLaunderTransactions.Stop(w,S().Gang,A().Id,"stop",()=>throw new Exception(),out _));Assert.Same(old,w.GangAutoLaunder);Assert.True(Auto(w).Enabled);}
        [Fact] public void FailedFinalSaveRestoresAllThreeBooksWithoutMoneyMovement()
        {var w=Enabled();var auto=w.GangAutoLaunder;var dirty=w.GangIllicit;var dealer=w.Dealer;Assert.Throws<Exception>(()=>Process(w,save:()=>{Assert.Equal(1000,Income(w).Dirty);Assert.Equal(425,w.GangTreasury.Accounts[S().Gang].Balance);Assert.Equal(1,Auto(w).Batches);throw new Exception();}));Assert.Same(auto,w.GangAutoLaunder);Assert.Same(dirty,w.GangIllicit);Assert.Same(dealer,w.Dealer);Assert.Null(w.GangTreasury);Assert.Equal(1500,Income(w).Dirty);Assert.Equal(0,Auto(w).Batches);Assert.Equal(10000,w.Bank);}
        [Fact] public void FailedFinalSaveRestoresExistingTreasuryReferenceAndAnchor()
        {var w=Enabled();w.GangTreasury=new GangTreasuryData();w.GangTreasury.Accounts[S().Gang]=new GangTreasuryAccount{Balance=100,Deposited=100,LastPaydayTicks=Now.Ticks};var old=w.GangTreasury;Assert.Throws<Exception>(()=>Process(w,save:()=>throw new Exception()));Assert.Same(old,w.GangTreasury);Assert.Equal(100,old.Accounts[S().Gang].Balance);}
        [Fact] public void FullTreasuryRejectsWholeCycleWithoutConsumingDirty()
        {var w=Enabled();w.GangTreasury=new GangTreasuryData();w.GangTreasury.Accounts[S().Gang]=new GangTreasuryAccount{Balance=int.MaxValue,Deposited=int.MaxValue};Assert.True(w.GangTreasury.Valid);Assert.False(Process(w));Assert.Equal(1500,Income(w).Dirty);Assert.Equal(0,Auto(w).Processed);}
        [Fact] public void AutomaticHistoryOverflowRejectsBeforePublishingShadowBooks()
        {var w=Enabled();var a=Auto(w);a.Batches=1;a.Processed=long.MaxValue;a.Net=long.MaxValue;Assert.True(w.GangAutoLaunder.Valid);var dirty=w.GangIllicit;Assert.False(Process(w));Assert.Same(dirty,w.GangIllicit);Assert.Null(w.GangTreasury);}
        [Fact] public void DuplicateUnderlyingManualReceiptDoesNotInventAutomaticHistory()
        {var w=Enabled();Assert.True(GangIllicitTransactions.Process(w,S(),A(),new GangLaunderingOptions(),500,Now,"shared",Save,out _));Assert.False(Process(w,"shared"));Assert.Equal(0,Auto(w).Batches);Assert.Equal(1000,Income(w).Dirty);Assert.Equal(425,w.GangTreasury.Accounts[S().Gang].Balance);}
        [Fact] public void CorruptSettingsIncomeOrTreasuryAreProtected()
        {var w=Enabled();Auto(w).Fees=-1;var old=w.GangAutoLaunder;Assert.False(Process(w));Assert.False(Set(w));Assert.Same(old,w.GangAutoLaunder);w=Enabled();Income(w).Dirty=999;Assert.False(Process(w));w=Enabled();w.GangTreasury=new GangTreasuryData{Accounts=null};Assert.False(Process(w));}
        [Fact] public void UnknownWalletAutoIncomeAndTreasuryMetadataSurvive()
        {var w=Enabled();w.GangTreasury=JObject.Parse("{Accounts:{},Receipts:[],Future:'treasury'}").ToObject<GangTreasuryData>();var j=JObject.FromObject(w);j["Future"]="wallet";j["GangAutoLaunder"]["Future"]="book";j["GangAutoLaunder"]["Accounts"][0]["Future"]="auto";j["GangIllicit"]["Accounts"][0]["Future"]="income";w=j.ToObject<WalletData>();Process(w);j=JObject.FromObject(w);Assert.Equal("wallet",(string)j["Future"]);Assert.Equal("book",(string)j["GangAutoLaunder"]["Future"]);Assert.Equal("auto",(string)j["GangAutoLaunder"]["Accounts"][0]["Future"]);Assert.Equal("income",(string)j["GangIllicit"]["Accounts"][0]["Future"]);Assert.Equal("treasury",(string)j["GangTreasury"]["Future"]);}
        [Fact] public void LegacyWalletRemainsUnconfiguredAndOtherProfilesOrAreasStaySeparate()
        {var w=JObject.Parse("{Bank:42,Future:'keep'}").ToObject<WalletData>();Assert.Null(w.GangAutoLaunder);w=Enabled();Assert.Null(W().GangAutoLaunder);Assert.Null(GangAutoLaunderTransactions.Account(w.GangAutoLaunder,S().Gang,2));Assert.Null(GangAutoLaunderTransactions.Account(w.GangAutoLaunder,"Other",A().Id));}
        [Fact] public void InvalidConfigurationIsRefusedWithoutSideEffects()
        {var w=Enabled();var c=new GangAutoLaunderOptions{BatchAmount=0};Assert.False(c.Valid);Assert.False(Process(w,c:c));c.BatchAmount=10001;Assert.False(c.Valid);c.BatchAmount=500;c.IntervalSeconds=29;Assert.False(c.Valid);c.IntervalSeconds=1801;Assert.False(c.Valid);Assert.Equal(1500,Income(w).Dirty);}
        [Fact] public void WrongIdsOrStopOfUnknownAreaDoNotCreateData()
        {var w=Enabled();Assert.False(Process(w,new string('x',81)));Assert.False(GangAutoLaunderTransactions.Stop(w,"Other",99,"stop",Save,out _));Assert.Single(w.GangAutoLaunder.Accounts);Assert.Equal(0,Auto(w).Batches);}
        [Fact] public void ReceiptListIsBoundedAndHistoryKeepsConfirmedProcessing()
        {var w=Enabled();for(int i=0;i<256;i++)w.GangAutoLaunder.Receipts.Add("past"+i);w.GangAutoLaunder.Receipts.RemoveAt(0);Assert.True(w.GangAutoLaunder.Valid);Assert.True(Process(w));Assert.Equal(256,w.GangAutoLaunder.Receipts.Count);Assert.Equal(500,Auto(w).Processed);}
        [Fact] public void AutomaticArmsProceedsCanFeedAutomaticProcessingWithoutTouchingBank()
        {var w=Enabled(0);w.GangArms=new GangArmsData();w.GangArms.Accounts.Add(new GangArmsAccount{Gang=S().Gang,Area=A().Id,Stock={{"pistol",1}},Recovered={{"pistol",1}}});Assert.True(GangAutoArmsTransactions.Configure(w,S(),A(),new GangArmsOptions(),"pistol",true,"arms-enable",Save,out _));Assert.True(GangAutoArmsTransactions.Sale(w,S(),A(),new GangArmsOptions(),new GangAutoArmsOptions(),"pistol","arms-sale",Save,out _));Assert.True(Process(w));Assert.Equal(0,w.GangArms.Accounts[0].Stock["pistol"]);Assert.Equal(0,Income(w).Dirty);Assert.Equal(425,w.GangTreasury.Accounts[S().Gang].Balance);Assert.Equal(10000,w.Bank);}
        [Fact] public void SessionClockWaitsFullMinuteAndNeverProcessesInsideMenu()
        {var clock=new GangAutoArmsClock(0);for(int t=1000;t<60000;t+=1000)Assert.False(clock.Step(t,true,60));Assert.False(clock.Step(60000,false,60));Assert.Equal(59000,clock.Milliseconds);Assert.True(clock.Step(61000,true,60));Assert.False(clock.Step(62000,false,60));var reload=new GangAutoArmsClock(900000);Assert.False(reload.Step(901000,true,60));Assert.Equal(1000,reload.Milliseconds);}
    }
}
