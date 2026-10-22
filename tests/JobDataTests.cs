using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace RPCore.Tests
{
    public class JobDataTests
    {
        static DeliveryRun AtDoor(int count = 2, bool returns = true)
        {
            var run = new DeliveryRun(count, returns); Assert.True(run.EnterVehicle()); Assert.True(run.TakePackage()); Assert.True(run.BeginHandover()); return run;
        }
        [Fact] public void NormalShiftRequiresVehicleCargoDeliveryAndReturn()
        {
            var run = new DeliveryRun(2,true); var receipts = new List<int>();
            Assert.False(run.TakePackage()); Assert.False(run.BeginHandover()); Assert.False(run.ReturnToDepot(_=>throw new Exception()));
            for(int i=0;i<2;i++)
            {
                if(i==0) Assert.True(run.EnterVehicle());
                Assert.True(run.TakePackage());Assert.True(run.BeginHandover());Assert.True(run.CommitDelivery(step=>{receipts.Add(step);return true;}));
                Assert.False(run.CommitDelivery(_=>throw new Exception()));
            }
            Assert.Equal(DeliveryStage.Returning,run.Stage);Assert.Equal(2,run.Delivered);
            Assert.True(run.ReturnToDepot(step=>{receipts.Add(step);return true;}));
            Assert.False(run.ReturnToDepot(_=>throw new Exception()));Assert.Equal(new[]{1,2,3},receipts);Assert.Equal(DeliveryStage.Finished,run.Stage);
        }
        [Fact] public void QuickPizzaCompletesWithoutReturnBonusOperation()
        { var run=AtDoor(1,false);Assert.True(run.CommitDelivery(_=>true));Assert.Equal(DeliveryStage.Finished,run.Stage);Assert.False(run.ReturnToDepot(_=>throw new Exception())); }
        [Fact] public void FailedPaymentDoesNotLoseCargoOrAdvanceStop()
        { var run=AtDoor();Assert.False(run.CommitDelivery(_=>false));Assert.Equal(0,run.Delivered);Assert.Equal(DeliveryStage.Handover,run.Stage);Assert.True(run.CommitDelivery(_=>true));Assert.Equal(1,run.Delivered); }
        [Fact] public void ExceptionDuringPaymentLeavesDeliveryRetryable()
        { var run=AtDoor();Assert.Throws<IOException>(()=>run.CommitDelivery(_=>throw new IOException()));Assert.Equal(0,run.Delivered);Assert.Equal(1,run.ReceiptStep);Assert.True(run.CommitDelivery(_=>true)); }
        [Fact] public void InterruptedHandoverRequiresNewInputAndRecoveredPackageMustBeCollectedAgain()
        { var run=AtDoor();run.InterruptHandover();Assert.Equal(DeliveryStage.Carrying,run.Stage);Assert.False(run.CommitDelivery(_=>true));run.RecoverPackage();Assert.Equal(DeliveryStage.NeedPackage,run.Stage);Assert.False(run.BeginHandover());Assert.True(run.TakePackage()); }
        [Theory] [InlineData(DeliveryStage.Carrying)] [InlineData(DeliveryStage.Handover)]
        public void CancelPreventsBothDeliveryAndReturnRewards(DeliveryStage stage)
        { var run=AtDoor();if(stage==DeliveryStage.Carrying)run.InterruptHandover();run.Cancel();Assert.Equal(DeliveryStage.Cancelled,run.Stage);Assert.False(run.CommitDelivery(_=>throw new Exception()));Assert.False(run.ReturnToDepot(_=>throw new Exception())); }
        [Fact] public void ReturnPaymentFailureDoesNotConsumeCompletion()
        { var run=AtDoor(1);run.CommitDelivery(_=>true);Assert.False(run.ReturnToDepot(_=>false));Assert.Equal(DeliveryStage.Returning,run.Stage);Assert.True(run.ReturnToDepot(_=>true)); }
        [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(101)]
        public void InvalidShiftSizeCannotBegin(int count) => Assert.Throws<ArgumentOutOfRangeException>(()=>new DeliveryRun(count,true));
        [Fact] public void OnlyOwningJobCanReleaseShiftLease()
        { var gate=new ShiftGate();Assert.True(gate.Acquire("pizza"));Assert.False(gate.Acquire("postop"));Assert.False(gate.Acquire("pizza"));gate.Release("postop");Assert.Equal("pizza",gate.Owner);gate.Release("pizza");Assert.True(gate.Acquire("postop")); }
        [Fact] public void SeparateRunsCannotReuseTheSamePaymentIdentity() => Assert.NotEqual(new DeliveryRun(1,true).Id,new DeliveryRun(1,true).Id);

        static bool Pay(WalletData wallet,string job,string session,int step,int amount,bool bank,bool delivery,bool complete,Action save,out bool credited)
            => DeliveryPayments.Commit(wallet,()=>wallet.Cash,v=>wallet.Cash=v,job,session,step,amount,bank,delivery,complete,save,out credited);
        [Theory] [InlineData(true)] [InlineData(false)]
        public void PaymentAndReceiptCommitTogetherAndReplayNeverCreditsAgain(bool bank)
        {
            var wallet=new WalletData{Cash=500,Bank=1000};int writes=0;bool credited;
            Assert.True(Pay(wallet,"postop","session",1,90,bank,true,false,()=>writes++,out credited));Assert.True(credited);
            Assert.True(Pay(wallet,"postop","session",1,999,bank,true,false,()=>writes++,out credited));Assert.False(credited);
            Assert.Equal(bank?1090:1000,wallet.Bank);Assert.Equal(bank?500:590,wallet.Cash);Assert.Equal(1,writes);
            var stats=wallet.Jobs.Get("postop");Assert.Equal(1,stats.Deliveries);Assert.Equal(90,stats.Earned);
        }
        [Theory] [InlineData(true)] [InlineData(false)]
        public void WriteFailureRollsBackMoneyReceiptAndTotalsThenRetryPaysOnce(bool bank)
        {
            var wallet=new WalletData{Cash=500,Bank=1000};bool credited;
            Assert.Throws<IOException>(()=>Pay(wallet,"postop","session",1,90,bank,true,false,()=>throw new IOException("disk full"),out credited));
            Assert.Equal(500,wallet.Cash);Assert.Equal(1000,wallet.Bank);Assert.Null(wallet.Jobs);
            Assert.True(Pay(wallet,"postop","session",1,90,bank,true,false,()=>{},out credited));Assert.True(credited);Assert.Equal(1,wallet.Jobs.Get("postop").Deliveries);
        }
        [Fact] public void NativeCashDelegateIsRestoredWhenCheckpointFails()
        {
            int nativeCash=500;var wallet=new WalletData{Cash=12};bool credited;
            Assert.Throws<IOException>(()=>DeliveryPayments.Commit(wallet,()=>nativeCash,v=>nativeCash=v,"pizza","s",1,100,false,true,false,()=>throw new IOException(),out credited));
            Assert.Equal(500,nativeCash);Assert.Equal(12,wallet.Cash);Assert.Null(wallet.Jobs);
        }
        [Fact] public void AtomicReloadRetainsReceiptsAndUnknownWalletData()
        {
            var root=Path.Combine(Path.GetTempPath(),"rpcore-job-"+Guid.NewGuid());Directory.CreateDirectory(root);
            try
            {
                string file=Path.Combine(root,"Wallet.json");var files=new JsonFiles();
                var wallet=JsonConvert.DeserializeObject<WalletData>("{\"Cash\":500,\"Bank\":1000,\"CustomMod\":{\"Keep\":true},\"Jobs\":{\"FutureJob\":7}}");bool credited;
                Assert.True(Pay(wallet,"postop","s",1,120,true,true,false,()=>files.Write(file,wallet),out credited));
                var loaded=files.Read(file,()=>new WalletData());
                Assert.True(Pay(loaded,"postop","s",1,120,true,true,false,()=>throw new Exception("duplicate save"),out credited));Assert.False(credited);
                Assert.Equal(1120,loaded.Bank);Assert.True((bool)loaded.Extra["CustomMod"]["Keep"]);Assert.Equal(7,(int)loaded.Jobs.Extra["FutureJob"]);
            }
            finally{Directory.Delete(root,true);}
        }
        [Fact] public void DamagedWalletRemainsProtectedAndCannotAcquireAPayment()
        {
            var root=Path.Combine(Path.GetTempPath(),"rpcore-job-"+Guid.NewGuid());Directory.CreateDirectory(root);
            try
            {
                var file=Path.Combine(root,"Wallet.json");File.WriteAllText(file,"{");var files=new JsonFiles();Assert.ThrowsAny<Exception>(()=>files.Read(file,()=>new WalletData()));
                var fallback=new WalletData{Bank=30};bool credited;
                Assert.Throws<IOException>(()=>Pay(fallback,"postop","s",1,90,true,true,false,()=>files.Write(file,fallback),out credited));
                Assert.Equal(30,fallback.Bank);Assert.Null(fallback.Jobs);Assert.Equal("{",File.ReadAllText(file));
            }
            finally{Directory.Delete(root,true);}
        }
        [Fact] public void OutOfOrderReceiptCannotPayReturnBonusBeforeDeliveries()
        { var wallet=new WalletData();bool credited;Assert.False(Pay(wallet,"postop","s",6,150,true,false,true,()=>throw new Exception(),out credited));Assert.False(credited);Assert.Null(wallet.Jobs); }
        [Fact] public void CompletionRewardCannotCountAsAnotherDeliveredParcel()
        {
            var wallet=new WalletData();bool credited;
            Assert.True(Pay(wallet,"postop","s",1,90,true,true,false,()=>{},out credited));
            Assert.True(Pay(wallet,"postop","s",2,150,true,false,true,()=>{},out credited));
            Assert.True(Pay(wallet,"postop","s",2,150,true,false,true,()=>throw new Exception(),out credited));Assert.False(credited);
            Assert.Equal(1,wallet.Jobs.Get("postop").Deliveries);Assert.Equal(1,wallet.Jobs.Get("postop").CompletedShifts);Assert.Equal(240,wallet.Bank);
        }
        [Theory] [InlineData(true)] [InlineData(false)]
        public void BalanceOverflowCannotConsumeDeliveryOrReceipt(bool bank)
        { var wallet=new WalletData{Cash=int.MaxValue,Bank=int.MaxValue};bool credited;Assert.False(Pay(wallet,"pizza","s",1,1,bank,true,false,()=>throw new Exception(),out credited));Assert.Null(wallet.Jobs); }
        [Fact] public void NewProfileCannotReceiveAnotherProfilesReceiptOrStatistics()
        {
            var a=new WalletData();var b=new WalletData();bool credited;
            Pay(a,"postop","a",1,90,true,true,false,()=>{},out credited);
            Assert.Null(b.Jobs);Assert.Equal(0,b.Bank);Pay(b,"postop","b",1,120,true,true,false,()=>{},out credited);
            Assert.Equal(90,a.Bank);Assert.Equal(120,b.Bank);Assert.DoesNotContain("a",b.Jobs.Get("postop").Receipts.Keys);
        }
        [Fact] public void ReceiptCacheStaysBoundedAndKeepsCurrentSessionReplayProtection()
        {
            var wallet=new WalletData();bool credited;
            for(int i=0;i<100;i++)Assert.True(Pay(wallet,"postop","s"+i,1,1,true,true,false,()=>{},out credited));
            Assert.Equal(64,wallet.Jobs.Get("postop").Receipts.Count);Assert.Equal(64,wallet.Jobs.Get("postop").RecentSessions.Count);
            Assert.True(Pay(wallet,"postop","s99",1,1,true,true,false,()=>throw new Exception(),out credited));Assert.False(credited);Assert.Equal(100,wallet.Bank);
        }
        [Fact] public void LegacyPizzaCountersCanCoexistWithAdditiveJobFields()
        {
            var dto=JsonConvert.DeserializeObject<PizzaStats>("{\"Deliveries\":12,\"Earned\":345,\"Future\":true}");
            var wallet=new WalletData{Jobs=new JobLedger()};var progress=wallet.Jobs.Get("pizza");progress.Deliveries=dto.Deliveries;progress.Earned=dto.Earned;bool credited;
            Assert.True(Pay(wallet,"pizza","s",1,90,false,true,false,()=>{},out credited));
            Assert.Equal(13,wallet.Jobs.Get("pizza").Deliveries);Assert.Equal(435,wallet.Jobs.Get("pizza").Earned);Assert.True((bool)JObject.Parse(JsonConvert.SerializeObject(dto))["Future"]);
        }
        [Theory] [InlineData(-1)] [InlineData(1000001)]
        public void InvalidRewardCannotMutateTheWallet(int amount)
        { var wallet=new WalletData{Bank=100};bool credited;Assert.False(Pay(wallet,"postop","s",1,amount,true,true,false,()=>throw new Exception(),out credited));Assert.Equal(100,wallet.Bank);Assert.Null(wallet.Jobs); }
        [Fact] public void ShippedRoutesAreValidAndNearestOrderingDoesNotMutateSource()
        {
            var options=JsonConvert.DeserializeObject<DeliveryOptions>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Jobs","PostOp.json")));
            Assert.True(options.Valid);Assert.Equal(40,options.Addresses.Count);var before=JsonConvert.SerializeObject(options);
            var route=DeliveryRoute.Select(options.Addresses,options.Start,5,new Random(42));
            Assert.Equal(5,route.Count);Assert.Equal(5,route.Distinct().Count());var current=options.Start;
            for(int i=0;i<route.Count;i++){Assert.Equal(route.Skip(i).Min(p=>DeliveryRoute.Distance(current,p)),DeliveryRoute.Distance(current,route[i]));current=route[i];}
            Assert.Equal(before,JsonConvert.SerializeObject(options));
        }
        [Fact] public void RouteRejectsDuplicateCoordinatesAndTooFewAddresses()
        { var p=new JobPoint{Name="x",X=10,Y=10,Z=30};Assert.Throws<ArgumentException>(()=>DeliveryRoute.Select(new[]{p,p},p,2,new Random())); }
        [Theory] [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)] [InlineData(-1f)]
        public void MalformedPayConfigurationCannotProduceMoney(float perMeter) => Assert.Throws<ArgumentOutOfRangeException>(()=>DeliveryRoute.Pay(90,perMeter,100));
        [Fact] public void EmbeddedAddressesAreAvailableWithoutASeparateConfigurationFile()
        { var options=JobDefaults.PostOp();Assert.True(options.Valid);Assert.Equal(40,options.Addresses.Count);Assert.Equal(-230.7383f,options.Start.X); }
        [Fact] public void FullPostOpRouteHasOnePaymentPerStopAndOneReturnBonus()
        {
            var run=new DeliveryRun(5,true);run.EnterVehicle();var wallet=new WalletData{Cash=700};bool credited;
            for(int i=0;i<5;i++)
            {
                run.TakePackage();run.BeginHandover();
                Assert.True(run.CommitDelivery(step=>Pay(wallet,"postop",run.Id,step,90,true,true,false,()=>{},out credited)));
                Assert.False(run.CommitDelivery(step=>throw new Exception("repeated input")));
            }
            Assert.Equal(450,wallet.Bank);Assert.Equal(700,wallet.Cash);Assert.Equal(0,wallet.Jobs.Get("postop").CompletedShifts);
            Assert.True(run.ReturnToDepot(step=>Pay(wallet,"postop",run.Id,step,150,true,false,true,()=>{},out credited)));
            Assert.Equal(600,wallet.Bank);Assert.Equal(5,wallet.Jobs.Get("postop").Deliveries);Assert.Equal(1,wallet.Jobs.Get("postop").CompletedShifts);
        }
        [Fact] public void CancelAfterPartialRouteKeepsOnlyAlreadyPaidDeliveries()
        {
            var run=AtDoor(5);var wallet=new WalletData();bool credited;
            run.CommitDelivery(step=>Pay(wallet,"postop",run.Id,step,90,true,true,false,()=>{},out credited));run.Cancel();
            Assert.False(run.ReturnToDepot(_=>throw new Exception("cancel bonus")));Assert.Equal(90,wallet.Bank);Assert.Equal(1,wallet.Jobs.Get("postop").Deliveries);Assert.Equal(0,wallet.Jobs.Get("postop").CompletedShifts);
        }
        [Theory] [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)] [InlineData(15001f)]
        public void InvalidAddressCoordinatesRejectCustomJobConfiguration(float x)
        { var options=JobDefaults.PostOp();options.Addresses[0].X=x;Assert.False(options.Valid); }
        [Fact] public void MissingAddressesOrImpossibleDropCountRejectConfiguration()
        { var options=JobDefaults.PostOp();options.Drops=31;Assert.False(options.Valid);options.Drops=5;options.Addresses=null;Assert.False(options.Valid); }
        [Fact] public void ExcessivePayIsBoundedAndNormalPayIsCalculatedFromDistance()
        { Assert.Equal(130,DeliveryRoute.Pay(90,.04f,1000));Assert.Equal(1000000,DeliveryRoute.Pay(int.MaxValue,1,float.MaxValue,int.MaxValue)); }
    }
}
