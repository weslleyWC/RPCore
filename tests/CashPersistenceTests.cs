using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RPCore;
using Xunit;
namespace RPCoreTests
{
    public class CashPersistenceTests
    {
        [Theory][InlineData("Michael")][InlineData("Franklin")][InlineData("Trevor")]
        public void Depositing400kThenLoadingTheOlderGtaSaveDoesNotDuplicateCash(string owner)
        {
            var native=new Dictionary<string,int>{{owner,400000}};var w=new WalletData{Cash=0,Bank=0};
            var first=new NativeCashSession(w,owner,o=>native[o],(o,n)=>native[o]=n);
            string disk=null;
            Assert.True(BankTransfer.Commit(w,()=>first.Cash,n=>first.Cash=n,400000,true,()=>CashPersistenceRules.Save(w,owner,first.Cash,()=>disk=JsonConvert.SerializeObject(w))));
            Assert.Equal(0,native[owner]);Assert.Equal(400000,w.Bank);
            // Quit/reopen: GTA restores its old story-save money; RPCore loads its own checkpoint.
            native[owner]=400000;var loaded=JsonConvert.DeserializeObject<WalletData>(disk);
            var next=new NativeCashSession(loaded,owner,o=>native[o],(o,n)=>native[o]=n);
            Assert.Equal(0,next.Cash);Assert.False(next.Restore(false,owner));Assert.Equal(400000,native[owner]);
            Assert.True(next.Restore(true,owner));Assert.Equal(0,native[owner]);Assert.Equal(400000,loaded.Bank);
            Assert.False(BankTransfer.Commit(loaded,()=>next.Cash,n=>next.Cash=n,400000,true,()=>throw new Exception("must not save")));
            Assert.Equal(400000,(long)next.Cash+loaded.Bank);
        }
        [Fact] public void WithdrawalAndRepeatedFullReloadsPreserveTheSameTotal()
        {
            var w=new WalletData{Bank=400000,NativeCash=new NativeCashCheckpoint{Owner="Franklin",Amount=0}};int native=400000;
            string disk=JsonConvert.SerializeObject(w);
            for(int i=0;i<5;i++){
                w=JsonConvert.DeserializeObject<WalletData>(disk);var s=new NativeCashSession(w,"Franklin",o=>native,(o,n)=>native=n);s.Restore(true,"Franklin");
                if(i==0)Assert.True(BankTransfer.Commit(w,()=>s.Cash,n=>s.Cash=n,150000,false,()=>CashPersistenceRules.Save(w,"Franklin",s.Cash,()=>disk=JsonConvert.SerializeObject(w))));
                Assert.Equal(400000,(long)s.Cash+w.Bank);Assert.Equal(150000,s.Cash);Assert.Equal(250000,w.Bank);
                native=400000; // old GTA save again; no additional banking operation.
            }
        }
        [Fact] public void LegacyCashFieldWithoutOwnerProofIsNeverRestoredAsZero()
        {
            var w=new WalletData{Cash=0,Bank=400000};int native=123456;
            var s=new NativeCashSession(w,"Franklin",o=>native,(o,n)=>native=n);
            Assert.False(s.Pending);Assert.False(s.Restore(true,"Franklin"));Assert.Equal(123456,s.Cash);
            CashPersistenceRules.Save(w,"Franklin",s.Cash,()=>{});Assert.Equal(123456,w.NativeCash.Amount);Assert.Equal(400000,w.Bank);
        }
        [Fact] public void FullOnePointEightMillionWithdrawalSurvivesGtaRestoringItsOld400kCash()
        {
            var w=new WalletData{Bank=1800000,NativeCash=new NativeCashCheckpoint{Owner="Franklin",Amount=0}};int native=400000;string disk=null;
            var first=new NativeCashSession(w,"Franklin",o=>native,(o,n)=>native=n);first.Restore(true,"Franklin");
            Assert.True(BankTransfer.Commit(w,()=>first.Cash,n=>first.Cash=n,1800000,false,()=>CashPersistenceRules.Save(w,"Franklin",first.Cash,()=>disk=JsonConvert.SerializeObject(w))));
            Assert.Equal(0,w.Bank);Assert.Equal(1800000,native);
            native=400000;var loaded=JsonConvert.DeserializeObject<WalletData>(disk);var next=new NativeCashSession(loaded,"Franklin",o=>native,(o,n)=>native=n);
            Assert.True(next.Restore(true,"Franklin"));Assert.Equal(1800000,native);Assert.Equal(0,loaded.Bank);
        }
        [Fact] public void QuitOrLoadingCannotOverwriteACommittedCashCheckpointWithResetGtaCash()
        {
            var w=new WalletData{Bank=0,NativeCash=new NativeCashCheckpoint{Owner="Franklin",Amount=1800000}};int native=400000;
            var s=new NativeCashSession(w,"Franklin",o=>native,(o,n)=>native=n);s.Restore(true,"Franklin");s.Saved();
            native=400000;Assert.Null(s.Capture(false));Assert.Equal(1800000,w.NativeCash.Amount);Assert.Equal(400000,s.Capture(true));
        }
        [Fact] public void ExplicitCashWriteWhileUnreadyStillSavesItsVerifiedValueAndLegacyDoesNotSeed()
        {
            int native=400000;var s=new NativeCashSession(new WalletData(),"Franklin",o=>native,(o,n)=>native=n);
            Assert.Null(s.Capture(false));s.Cash=1800000;native=400000;Assert.Equal(1800000,s.Capture(false));s.Saved();Assert.Null(s.Capture(false));
        }
        [Fact] public void ProfileSwitchSavesAndWritesTheNamedAccountNotTheCurrentlyVisibleActor()
        {
            var balances=new Dictionary<string,int>{{"Michael",100},{"Franklin",200},{"Trevor",300}};
            var w=new WalletData{NativeCash=new NativeCashCheckpoint{Owner="Franklin",Amount=25}};
            var s=new NativeCashSession(w,"Franklin",o=>balances[o],(o,n)=>balances[o]=n);
            Assert.False(s.Restore(true,"Michael"));Assert.Equal(100,balances["Michael"]);
            Assert.True(s.Restore(true,"Franklin"));s.Cash=50;Assert.Equal(100,balances["Michael"]);Assert.Equal(50,balances["Franklin"]);Assert.Equal(300,balances["Trevor"]);
        }
        [Fact] public void BankingBeforePendingRestoreUsesAndRestoresTheNewCheckpointAmount()
        {
            var w=new WalletData{Bank=0,NativeCash=new NativeCashCheckpoint{Owner="Franklin",Amount=200}};int native=999;
            var s=new NativeCashSession(w,"Franklin",o=>native,(o,n)=>native=n);
            Assert.True(BankTransfer.Commit(w,()=>s.Cash,n=>s.Cash=n,150,true,()=>CashPersistenceRules.Save(w,"Franklin",s.Cash,()=>{})));
            native=999;Assert.True(s.Restore(true,"Franklin"));Assert.Equal(50,native);Assert.Equal(150,w.Bank);
        }
        [Fact] public void SaveFailureRestoresBothBalancesAndTheExactPreviousCheckpoint()
        {
            var w=new WalletData{Cash=100,Bank=50,NativeCash=new NativeCashCheckpoint{Owner="Franklin",Amount=100,Extra=new Dictionary<string,JToken>{{"Future",true}}}};var old=w.NativeCash;int native=100;
            var s=new NativeCashSession(w,"Franklin",o=>native,(o,n)=>native=n);s.Restore(true,"Franklin");string before=JsonConvert.SerializeObject(w);
            Assert.Throws<IOException>(()=>BankTransfer.Commit(w,()=>s.Cash,n=>s.Cash=n,100,true,()=>CashPersistenceRules.Save(w,"Franklin",s.Cash,()=>throw new IOException())));
            Assert.Equal(100,native);Assert.Same(old,w.NativeCash);Assert.Equal(before,JsonConvert.SerializeObject(w));
        }
        [Fact] public void NativeEarningsAreCapturedAfterTheOneTimeRestoreInsteadOfBeingRevertedEveryFrame()
        {
            int native=100;var w=new WalletData{NativeCash=new NativeCashCheckpoint{Owner="Franklin",Amount=50}};
            var s=new NativeCashSession(w,"Franklin",o=>native,(o,n)=>native=n);s.Restore(true,"Franklin");native+=25;
            Assert.False(s.Restore(true,"Franklin"));Assert.Equal(75,s.Cash);CashPersistenceRules.Save(w,"Franklin",s.Cash,()=>{});Assert.Equal(75,w.NativeCash.Amount);
        }
        [Theory][InlineData("Michael","SP0_TOTAL_CASH")][InlineData("Franklin","SP1_TOTAL_CASH")][InlineData("Trevor","SP2_TOTAL_CASH")][InlineData("Default",null)]
        public void StatisticMappingMatchesTheInstalledShvdnPlayerMoneyImplementation(string owner,string stat)=>Assert.Equal(stat,CashPersistenceRules.Stat(owner));
        [Fact] public void InvalidOrCrossProfileCheckpointsAreRetainedAndCannotBeSilentlyRewritten()
        {
            var w=new WalletData{Cash=10,NativeCash=new NativeCashCheckpoint{Owner="Michael",Amount=20}};
            Assert.False(CashPersistenceRules.Trusted(w,"Franklin"));string before=JsonConvert.SerializeObject(w);
            Assert.Throws<InvalidOperationException>(()=>CashPersistenceRules.Save(w,"Franklin",50,()=>throw new Exception()));Assert.Equal(before,JsonConvert.SerializeObject(w));
            w.NativeCash.Version=999;Assert.False(CashPersistenceRules.Trusted(w,"Michael"));
        }
        [Fact] public void MPSavesKeepTheirWalletCashAndDoNotAcquireAProtagonistCheckpoint()
        {var w=new WalletData{Cash=400000};Assert.Null(CashPersistenceRules.Stat("custom_profile"));Assert.True(BankTransfer.Commit(w,()=>w.Cash,n=>w.Cash=n,400000,true,()=>{}));var reload=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.Equal(0,reload.Cash);Assert.Equal(400000,reload.Bank);Assert.Null(reload.NativeCash);}
    }
}
