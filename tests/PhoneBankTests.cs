using System;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RPCore;
using Xunit;

namespace RPCoreTests
{
    public class PhoneBankTests
    {
        [Theory][InlineData("1",1)][InlineData("1000000",1000000)][InlineData("1.000.000",1000000)]
        [InlineData("2.147.483.647",int.MaxValue)][InlineData(" 500000000 ",500000000)]
        public void AmountAcceptsAnyAvailableWholeAmountWithoutPresetOrDailyCap(string input,int expected)
        {Assert.True(PhoneBankRules.Amount(input,out int amount));Assert.Equal(expected,amount);}
        [Theory][InlineData("")][InlineData(null)][InlineData("-1")][InlineData("+10")][InlineData("0")]
        [InlineData("1,50")][InlineData("1.50")][InlineData("1.0000")][InlineData("1..000")]
        [InlineData(".100")][InlineData("1000.000")][InlineData("2147483648")][InlineData("1e6")]
        public void InvalidOrOverflowingInputCannotCreateMoney(string input)=>Assert.False(PhoneBankRules.Amount(input,out _));
        [Theory][InlineData(true)][InlineData(false)]
        public void TransfersAboveAMillionUseTheSameWalletAndConserveTotal(bool deposit)
        {
            var w=new WalletData{Bank=deposit?0:2000000000,Extra=new System.Collections.Generic.Dictionary<string,JToken>{{"Future","keep"}}};
            int cash=deposit?2000000000:0;
            var op=PhoneBankOperation.Create("Franklin",1,123,cash,w.Bank,1500000000,deposit);
            Assert.NotNull(op);Assert.Equal(deposit?1500000000:500000000,op.BankAfter);
            long total=(long)cash+w.Bank;int saves=0;
            Assert.Equal(PhoneBankResult.Completed,op.Commit("Franklin",1,123,cash,w.Bank,(amount,d)=>BankTransfer.Commit(w,()=>cash,n=>cash=n,amount,d,()=>saves++)));
            Assert.Equal(total,(long)cash+w.Bank);Assert.Equal(op.BankAfter,w.Bank);Assert.Equal(op.CashAfter,cash);
            Assert.Equal(1,saves);Assert.Equal("keep",(string)w.Extra["Future"]);
        }
        [Fact] public void ConfirmationIsOneShotEvenWhenTheSameButtonRepeats()
        {
            var op=PhoneBankOperation.Create("F",1,123,100,0,100,true);int calls=0;
            Assert.Equal(PhoneBankResult.Completed,op.Commit("F",1,123,100,0,(a,d)=>{calls++;return true;}));
            Assert.Equal(PhoneBankResult.Used,op.Commit("F",1,123,100,0,(a,d)=>{calls++;return true;}));Assert.Equal(1,calls);
        }
        [Theory][InlineData("M",1,123,100,0)][InlineData("F",2,123,100,0)]
        [InlineData("F",1,456,100,0)][InlineData("F",1,123,101,0)][InlineData("F",1,123,100,1)]
        public void ChangedProfileActorModelOrBalancesCannotCommitAStaleConfirmation(string profile,int actor,int model,int cash,int bank)
        {
            var op=PhoneBankOperation.Create("F",1,123,100,0,100,true);
            Assert.Equal(PhoneBankResult.Stale,op.Commit(profile,actor,model,cash,bank,(a,d)=>throw new Exception("must not transfer")));
            Assert.Equal(PhoneBankResult.Used,op.Commit("F",1,123,100,0,(a,d)=>throw new Exception("must not replay")));
        }
        [Fact] public void FailedDurableWriteRollsBackBothBalancesAndTheQuoteCannotReplay()
        {
            var w=new WalletData{Bank=500};int cash=100;string before=JsonConvert.SerializeObject(w);
            var op=PhoneBankOperation.Create("F",1,123,cash,w.Bank,100,true);
            Assert.Throws<IOException>(()=>op.Commit("F",1,123,cash,w.Bank,(a,d)=>BankTransfer.Commit(w,()=>cash,n=>cash=n,a,d,()=>throw new IOException("disk"))));
            Assert.Equal(before,JsonConvert.SerializeObject(w));Assert.Equal(100,cash);
            Assert.Equal(PhoneBankResult.Used,op.Commit("F",1,123,cash,w.Bank,(a,d)=>throw new Exception()));
        }
        [Fact] public void RejectedTransferDoesNotBecomeASuccessfulReceiptOrReplay()
        {
            var op=PhoneBankOperation.Create("F",1,123,100,0,100,true);
            Assert.Equal(PhoneBankResult.Rejected,op.Commit("F",1,123,100,0,(a,d)=>false));
            Assert.Equal(PhoneBankResult.Used,op.Commit("F",1,123,100,0,(a,d)=>true));
        }
        [Fact] public void AllAvailableCanReachTheNativeNumericMaximumWithoutOverflow()
        {
            int amount=PhoneViewRules.BankMaximum(int.MaxValue,0,true);
            var op=PhoneBankOperation.Create("F",1,123,int.MaxValue,0,amount,true);
            Assert.NotNull(op);Assert.Equal(int.MaxValue,op.BankAfter);Assert.Equal(0,op.CashAfter);
            Assert.Null(PhoneBankOperation.Create("F",1,123,100,int.MaxValue,1,true));
            Assert.Null(PhoneBankOperation.Create("F",1,123,int.MaxValue,100,1,false));
        }
        [Fact] public void MissingOwnerInsufficientFundsZeroAndNegativeBalancesHaveNoQuote()
        {
            Assert.Null(PhoneBankOperation.Create(null,1,123,100,0,100,true));
            Assert.Null(PhoneBankOperation.Create("F",0,123,100,0,100,true));
            Assert.Null(PhoneBankOperation.Create("F",1,0,100,0,100,true));
            Assert.Null(PhoneBankOperation.Create("F",1,123,100,0,101,true));
            Assert.Null(PhoneBankOperation.Create("F",1,123,100,0,1,false));
            Assert.Null(PhoneBankOperation.Create("F",1,123,-1,0,1,true));
            Assert.Null(PhoneBankOperation.Create("F",1,123,100,0,0,true));
        }
        [Fact] public void BankShortcutHasItsOwnHitAreaAndAllTwelveOriginalAppsKeepTheirPositions()
        {
            Assert.Equal(PhoneBankRules.App,PhoneBankRules.HomeHit(new PointF(24,86)));
            Assert.Equal(PhoneBankRules.App,PhoneBankRules.HomeHit(new PointF(218,133)));
            Assert.Equal(-1,PhoneBankRules.HomeHit(new PointF(219,134)));
            for(int i=0;i<12;i++)Assert.Equal(i,PhoneBankRules.HomeHit(new PointF(40+i%3*65,160+i/3*65)));
            Assert.Equal(-1,PhoneBankRules.HomeHit(new PointF(90,160)));
        }
        [Fact] public void KeyboardCanReachTheBankAndReturnToAllOriginalApps()
        {
            Assert.Equal(12,PhoneBankRules.MoveHome(1,0,-1));Assert.Equal(0,PhoneBankRules.MoveHome(12,0,1));
            Assert.Equal(12,PhoneBankRules.MoveHome(12,1,0));Assert.Equal(4,PhoneBankRules.MoveHome(1,0,1));
        }
        [Fact] public void BankRowsAndConfirmationIgnoreFooterAndGaps()
        {
            Assert.Equal(3,PhoneViewRules.RowHit(new PointF(30,340),0,4,220));
            Assert.Equal(-1,PhoneViewRules.RowHit(new PointF(30,256),0,4,220));
            Assert.Equal(1,PhoneViewRules.RowHit(new PointF(30,345),0,2,303));
            Assert.Equal(-1,PhoneViewRules.RowHit(new PointF(30,401),0,2,303));
        }
        [Fact] public void MoneyFormattingShowsTheWholeBalanceAndNoRoundedKAbbreviation()
        {Assert.Equal("$ 1.500.000.000",PhoneBankRules.Money(1500000000));}
    }
}
