using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangEconomyTests
    {
        static readonly Action Save=()=>{};static readonly DateTime Now=new DateTime(2013,9,17,12,0,0);
        static GangTreasuryOptions C()=>new GangTreasuryOptions();
        static WalletData W()=>new WalletData{Bank=10000,Cash=77};
        static WalletData Fund(){var w=W();Assert.True(GangTreasuryTransactions.Deposit(w,"Mine",1000,Now,C(),"fund",Save,out _));return w;}
        [Theory][InlineData(0,1,5)][InlineData(249,1,5)][InlineData(250,2,10)][InlineData(749,2,10)][InlineData(750,3,20)][InlineData(1499,3,20)][InlineData(1500,4,35)]
        public void ReferenceRankThresholdsAndSalaryShares(int reputation,int rank,int share)
        {Assert.Equal(rank,GangProgressionRules.ThresholdRank(reputation));Assert.Equal(share,GangProgressionRules.Share(rank));}
        [Fact] public void LegacyReputationAboveOneHundredIsPreservedAndCanPromote()
        {var s=JsonConvert.DeserializeObject<GangState>("{\"Gang\":\"Mine\",\"Reputation\":245,\"Future\":7}");Assert.True(GangProgressionRules.Grant(s,50));Assert.Equal(295,s.Reputation);Assert.Equal(2,s.Rank);Assert.Equal(7,(int)JObject.FromObject(s)["Future"]);}
        [Fact] public void ReputationLossDoesNotDemoteKnownOrLegacyImplicitRank()
        {var s=new GangState{Gang="Mine",Reputation=1510};GangProgressionRules.Grant(s,-20);Assert.Equal(1490,s.Reputation);Assert.Equal(4,s.Rank);s.Reputation=3;GangProgressionRules.Grant(s,-20);Assert.Equal(0,s.Reputation);Assert.Equal(4,s.Rank);}
        [Fact] public void ReputationArithmeticSaturatesWithoutOverflowAndNeedsAffiliation()
        {var s=new GangState{Gang="Mine",Reputation=int.MaxValue-1};GangProgressionRules.Grant(s,50);Assert.Equal(int.MaxValue,s.Reputation);Assert.Equal(4,s.Rank);Assert.False(GangProgressionRules.Grant(new GangState(),50));}
        [Fact] public void FirstDepositMovesOnlyBankIntoGangAccountAndStartsGameClock()
        {var w=Fund();var a=GangTreasuryTransactions.Account(w.GangTreasury,"Mine");Assert.Equal(9000,w.Bank);Assert.Equal(1000,a.Balance);Assert.Equal(1000,a.Deposited);Assert.Equal(Now.Ticks,a.LastPaydayTicks);Assert.Equal(77,w.Cash);Assert.True(a.Valid);}
        [Fact] public void WithdrawMovesAvailableTreasuryBackToBankWithoutClockReset()
        {var w=Fund();Assert.True(GangTreasuryTransactions.Withdraw(w,"Mine",400,C(),"out",Save,out _));Assert.Equal(9400,w.Bank);var a=GangTreasuryTransactions.Account(w.GangTreasury,"Mine");Assert.Equal(600,a.Balance);Assert.Equal(Now.Ticks,a.LastPaydayTicks);Assert.Equal(400,a.Withdrawn);}
        [Theory][InlineData(1,50)][InlineData(2,100)][InlineData(3,200)][InlineData(4,350)]
        public void PaydayIsFundedByBalanceAndUsesReferenceShare(int rank,int expected)
        {var w=Fund();Assert.True(GangTreasuryTransactions.Payday(w,"Mine",rank,Now.AddHours(24),C(),"pay",Save,out _));Assert.Equal(9000+expected,w.Bank);var a=GangTreasuryTransactions.Account(w.GangTreasury,"Mine");Assert.Equal(1000-expected,a.Balance);Assert.Equal(expected,a.Salary);Assert.Equal(10000,w.Bank+a.Balance);}
        [Fact] public void CannotCollectBeforeIntervalRepeatImmediatelyOrExploitRewind()
        {var w=Fund();Assert.False(GangTreasuryTransactions.Payday(w,"Mine",4,Now.AddHours(23.9),C(),"early",Save,out _));Assert.True(GangTreasuryTransactions.Payday(w,"Mine",4,Now.AddHours(24),C(),"pay",Save,out _));Assert.False(GangTreasuryTransactions.Payday(w,"Mine",4,Now.AddHours(24),C(),"again",Save,out _));Assert.False(GangTreasuryTransactions.Payday(w,"Mine",4,Now,C(),"rewind",Save,out _));Assert.Equal(9350,w.Bank);}
        [Fact] public void LongAbsencePaysSingleShareRatherThanCompoundingOfflinePeriods()
        {var w=Fund();GangTreasuryTransactions.Payday(w,"Mine",1,Now.AddDays(100),C(),"late",Save,out _);Assert.Equal(9050,w.Bank);Assert.Equal(950,GangTreasuryTransactions.Account(w.GangTreasury,"Mine").Balance);}
        [Fact] public void EmptyTreasuryCannotGenerateMoneyOrCreateSalaryLedger()
        {var w=W();Assert.False(GangTreasuryTransactions.Payday(w,"Mine",4,Now,C(),"empty",Save,out _));Assert.Null(w.GangTreasury);Assert.Equal(10000,w.Bank);}
        [Fact] public void ReloadAndReceiptReplayNeverDebitOrCreditTwice()
        {var w=CharacterRules.Copy(Fund());Assert.True(GangTreasuryTransactions.Deposit(w,"Mine",1000,Now,C(),"fund",()=>throw new Exception(),out bool changed));Assert.False(changed);GangTreasuryTransactions.Payday(w,"Mine",2,Now.AddDays(1),C(),"pay",Save,out _);Assert.True(GangTreasuryTransactions.Payday(w,"Mine",2,Now.AddDays(5),C(),"pay",()=>throw new Exception(),out changed));Assert.False(changed);Assert.Equal(9100,w.Bank);}
        [Fact] public void FailedDepositSaveRestoresBankAndAbsentBookWithoutTouchingOtherWalletReferences()
        {var w=W();w.Dmv=new DmvData{TheoryPassed=true,Licensed=true};var dmv=w.Dmv;Assert.Throws<Exception>(()=>GangTreasuryTransactions.Deposit(w,"Mine",1000,Now,C(),"fund",()=>throw new Exception(),out _));Assert.Equal(10000,w.Bank);Assert.Null(w.GangTreasury);Assert.Same(dmv,w.Dmv);}
        [Fact] public void FailedWithdrawalAndPaydaySaveRestoreOriginalBookReference()
        {var w=Fund();var book=w.GangTreasury;Assert.Throws<Exception>(()=>GangTreasuryTransactions.Withdraw(w,"Mine",500,C(),"out",()=>throw new Exception(),out _));Assert.Same(book,w.GangTreasury);Assert.Equal(9000,w.Bank);Assert.Throws<Exception>(()=>GangTreasuryTransactions.Payday(w,"Mine",4,Now.AddDays(1),C(),"pay",()=>throw new Exception(),out _));Assert.Same(book,w.GangTreasury);Assert.Equal(1000,GangTreasuryTransactions.Account(book,"Mine").Balance);}
        [Fact] public void BankOverflowAndInsufficientFundsCannotPublishAnyTransfer()
        {var w=Fund();w.Bank=int.MaxValue;Assert.False(GangTreasuryTransactions.Withdraw(w,"Mine",1,C(),"overflow",Save,out _));Assert.False(GangTreasuryTransactions.Payday(w,"Mine",1,Now.AddDays(1),C(),"overflow-pay",Save,out _));w=W();w.Bank=99;Assert.False(GangTreasuryTransactions.Deposit(w,"Mine",100,Now,C(),"poor",Save,out _));Assert.Null(w.GangTreasury);}
        [Fact] public void NamesAreCaseInsensitiveAndGangSwitchDoesNotShareBalance()
        {var w=Fund();Assert.True(GangTreasuryTransactions.Withdraw(w,"MINE",100,C(),"out",Save,out _));Assert.Single(w.GangTreasury.Accounts);Assert.Null(GangTreasuryTransactions.Account(w.GangTreasury,"Other"));Assert.False(GangTreasuryTransactions.Withdraw(w,"Other",100,C(),"other",Save,out _));}
        [Fact] public void UnknownWalletBookAccountMetadataSurvivesPayday()
        {var j=JObject.FromObject(Fund());j["Future"]="root";j["GangTreasury"]["Future"]="book";j["GangTreasury"]["Accounts"]["Mine"]["Future"]="account";var w=j.ToObject<WalletData>();GangTreasuryTransactions.Payday(w,"Mine",1,Now.AddDays(1),C(),"pay",Save,out _);j=JObject.FromObject(w);Assert.Equal("root",(string)j["Future"]);Assert.Equal("book",(string)j["GangTreasury"]["Future"]);Assert.Equal("account",(string)j["GangTreasury"]["Accounts"]["Mine"]["Future"]);}
        [Fact] public void CorruptConservationOrReceiptStateIsProtected()
        {var w=Fund();w.GangTreasury.Accounts["Mine"].Balance=1001;Assert.False(w.GangTreasury.Valid);Assert.False(GangTreasuryTransactions.Payday(w,"Mine",4,Now.AddDays(1),C(),"bad",Save,out _));Assert.Equal(9000,w.Bank);w=Fund();w.GangTreasury.Receipts=null;Assert.False(GangTreasuryTransactions.Withdraw(w,"Mine",1,C(),"bad",Save,out _));}
        [Theory][InlineData(0)][InlineData(-1)][InlineData(1000001)]
        public void InvalidAmountsCannotDebit(int amount)
        {var w=W();Assert.False(GangTreasuryTransactions.Deposit(w,"Mine",amount,Now,C(),"bad",Save,out _));Assert.Equal(10000,w.Bank);}
        [Fact] public void InvalidRankNameOptionsAndMissingIdAreRejected()
        {var w=Fund();Assert.False(GangTreasuryTransactions.Payday(w,"Mine",99,Now.AddDays(1),C(),"bad",Save,out _));Assert.False(GangTreasuryTransactions.Deposit(w,"None",100,Now,C(),"bad",Save,out _));Assert.False(GangTreasuryTransactions.Withdraw(w,"Mine",100,new GangTreasuryOptions{PaydayHours=0},"bad",Save,out _));Assert.False(GangTreasuryTransactions.Withdraw(w,"Mine",100,C(),null,Save,out _));}
        [Fact] public void SeparateProfilesCannotCopyAccountsOrPaydays()
        {var a=Fund();var b=W();GangTreasuryTransactions.Payday(a,"Mine",4,Now.AddDays(1),C(),"pay",Save,out _);Assert.Null(b.GangTreasury);Assert.Equal(10000,b.Bank);}
        [Fact] public void ShortenedTestIntervalStillRequiresActualGameTime()
        {var w=Fund();var c=new GangTreasuryOptions{PaydayHours=1};Assert.False(GangTreasuryTransactions.Payday(w,"Mine",1,Now.AddMinutes(59),c,"soon",Save,out _));Assert.True(GangTreasuryTransactions.Payday(w,"Mine",1,Now.AddHours(1),c,"test",Save,out _));}
    }
}
