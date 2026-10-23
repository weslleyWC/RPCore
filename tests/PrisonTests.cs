using System;
using Newtonsoft.Json;
using Xunit;

namespace RPCore.Tests
{
    public class PrisonTests
    {
        static PrisonRun Loaded()
        {var r=new PrisonRun(4,4);r.BeginLoading(0,true);r.LoadReady(4000,true);Assert.True(r.ManifestConfirmed(true,4));return r;}
        static PrisonRun Delivered()
        {var r=Loaded();r.BeginUnload(true,4);r.ConfirmUnloaded(true,4);return r;}
        [Fact] public void ManifestCannotBeAcceptedBeforeLoadingElapsed()
        {
            var r=new PrisonRun(4,4);Assert.False(r.ManifestConfirmed(true,4));Assert.False(r.BeginLoading(0,false));Assert.True(r.BeginLoading(0,true));
            Assert.False(r.ManifestConfirmed(true,4));Assert.False(r.LoadReady(3999,true));Assert.False(r.ManifestConfirmed(true,4));Assert.True(r.LoadReady(4000,true));Assert.True(r.ManifestConfirmed(true,4));
        }
        [Theory] [InlineData(0)] [InlineData(1)] [InlineData(3)] [InlineData(5)]
        public void PartialOrWrongManifestCannotStartTransport(int seated)
        {var r=new PrisonRun(4,4);r.BeginLoading(0,true);r.LoadReady(4000,true);Assert.False(r.ManifestConfirmed(true,seated));Assert.Equal(PrisonStage.Loading,r.Stage);}
        [Fact] public void LoadingNeedsStoppedDriverAndRestartsAfterInterruption()
        {
            var r=new PrisonRun(4,4);r.BeginLoading(0,true);Assert.False(r.LoadReady(4000,false));Assert.Equal(PrisonStage.AwaitDriver,r.Stage);
            Assert.True(r.BeginLoading(5000,true));Assert.False(r.LoadReady(8999,true));r.InterruptLoading();Assert.False(r.ManifestConfirmed(true,4));
        }
        [Fact] public void TimerWorksAcrossSignedClockOverflow()
        {var r=new PrisonRun(4,4);r.BeginLoading(int.MaxValue-2000,true);Assert.False(r.LoadReady(unchecked(int.MaxValue+1999),true));Assert.True(r.LoadReady(unchecked(int.MaxValue+2000),true));}
        [Fact] public void UnloadRequiresAllPrisonersAndStoppedDriverAtDestination()
        {var r=Loaded();Assert.False(r.BeginUnload(false,4));Assert.False(r.BeginUnload(true,3));Assert.True(r.BeginUnload(true,4));Assert.False(r.ConfirmUnloaded(false,4));Assert.False(r.ConfirmUnloaded(true,3));Assert.True(r.ConfirmUnloaded(true,4));}
        [Fact] public void CannotPayForPrisonersStillInsideOrLostBeforeHandover()
        {var r=Loaded();Assert.False(r.PayCustody(true,_=>throw new Exception()));r.BeginUnload(true,4);Assert.False(r.PayCustody(true,_=>throw new Exception()));r.ConfirmUnloaded(true,4);Assert.False(r.PayCustody(false,_=>throw new Exception()));}
        [Fact] public void CustodyPaysOnceThenEmptyBusReturnPaysSingleBonus()
        {
            var r=Delivered();var w=new WalletData{Cash=200,Bank=100};bool credited;
            Assert.True(r.PayCustody(true,s=>DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,"prison",r.Id,s,2000,true,true,false,()=>{},out credited)));
            Assert.False(r.PayCustody(true,_=>throw new Exception()));Assert.Equal(2100,w.Bank);Assert.Equal(1,w.Jobs.Get("prison").Deliveries);Assert.Equal(0,w.Jobs.Get("prison").CompletedShifts);
            Assert.False(r.Return(false,_=>throw new Exception()));Assert.True(r.Return(true,s=>DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,"prison",r.Id,s,400,true,false,true,()=>{},out credited)));
            Assert.False(r.Return(true,_=>throw new Exception()));Assert.Equal(2500,w.Bank);Assert.Equal(200,w.Cash);Assert.Equal(1,w.Jobs.Get("prison").CompletedShifts);Assert.Equal(0,w.Jobs.Get("prison").RatedTrips);
        }
        [Fact] public void SaveFailureKeepsCustodyPendingAndRetryableWithoutRepaying()
        {
            var r=Delivered();var w=new WalletData{Bank=100};bool credited;string old=JsonConvert.SerializeObject(w);
            Assert.Throws<InvalidOperationException>(()=>r.PayCustody(true,s=>DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,"prison",r.Id,s,2000,true,true,false,()=>throw new InvalidOperationException(),out credited)));
            Assert.Equal(PrisonStage.PaymentPending,r.Stage);Assert.Equal(old,JsonConvert.SerializeObject(w));Assert.True(r.PayCustody(true,s=>DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,"prison",r.Id,s,2000,true,true,false,()=>{},out credited)));
            Assert.Equal(2100,w.Bank);
        }
        [Fact] public void FailedBonusCanRetryAndDoesNotCompleteShiftEarly()
        {var r=Delivered();r.PayCustody(true,_=>true);Assert.False(r.Return(true,_=>false));Assert.Equal(PrisonStage.Returning,r.Stage);Assert.True(r.Return(true,_=>true));Assert.Equal(PrisonStage.Finished,r.Stage);}
        [Theory] [InlineData(PrisonStage.AwaitDriver)] [InlineData(PrisonStage.Loading)] [InlineData(PrisonStage.Transporting)] [InlineData(PrisonStage.Unloading)] [InlineData(PrisonStage.PaymentPending)] [InlineData(PrisonStage.Returning)]
        public void CancelBlocksPaymentsFromEveryActiveStage(PrisonStage stage)
        {
            var r=new PrisonRun(4,4);
            if(stage>=PrisonStage.Loading)r.BeginLoading(0,true);
            if(stage>=PrisonStage.Transporting){r.LoadReady(4000,true);r.ManifestConfirmed(true,4);}
            if(stage>=PrisonStage.Unloading)r.BeginUnload(true,4);
            if(stage>=PrisonStage.PaymentPending)r.ConfirmUnloaded(true,4);
            if(stage>=PrisonStage.Returning)r.PayCustody(true,_=>true);
            Assert.Equal(stage,r.Stage);r.Cancel();Assert.Equal(PrisonStage.Cancelled,r.Stage);Assert.False(r.PayCustody(true,_=>throw new Exception()));Assert.False(r.Return(true,_=>throw new Exception()));Assert.False(r.BeginLoading(8000,true));
        }
        [Fact] public void LedgerAdditionAndReceiptReplayPreserveEarlierJobsAndUnknownFields()
        {
            var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":50,\"ExtraSave\":true,\"Jobs\":{\"Jobs\":{\"tanker\":{\"Deliveries\":2,\"CompletedShifts\":2,\"Earned\":3000},\"supply\":{\"Deliveries\":5},\"uber\":{\"RatingSum\":15,\"RatedTrips\":3}}}}");
            string tanker=JsonConvert.SerializeObject(w.Jobs.Get("tanker"));bool credited;Assert.True(DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,"prison","run",1,2000,true,true,false,()=>{},out credited));
            w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,"prison","run",1,2000,true,true,false,()=>throw new Exception(),out credited));
            Assert.False(credited);Assert.Equal(2050,w.Bank);Assert.Equal(tanker,JsonConvert.SerializeObject(w.Jobs.Get("tanker")));Assert.Equal(5,w.Jobs.Get("supply").Deliveries);Assert.Equal(15,w.Jobs.Get("uber").RatingSum);Assert.True((bool)w.Extra["ExtraSave"]);
        }
        [Fact] public void DefaultsUseReferenceMissionRowSpawnAndPrisonGate()
        {var c=PrisonDefaults.Load();Assert.True(c.Valid);Assert.Equal(4,c.Prisoners);Assert.Equal(449.2777f,c.Spawn.X);Assert.Equal(-1017.897f,c.Spawn.Y);Assert.Equal(1851.775f,c.Destination.X);Assert.Equal(2608.431f,c.Destination.Y);Assert.Equal(424.1417f,c.Start.X);}
        [Theory] [InlineData(0,4)] [InlineData(9,4)] [InlineData(4,1)] [InlineData(4,16)]
        public void ImpossibleManifestAndLoadingConfigRejected(int count,int seconds)
        {var c=PrisonDefaults.Load();c.Prisoners=count;c.LoadSeconds=seconds;Assert.False(c.Valid);Assert.Throws<ArgumentOutOfRangeException>(()=>new PrisonRun(count,seconds));}
        [Theory] [InlineData(true,0,18,18,true)] [InlineData(false,0,0,18,false)] [InlineData(true,.5,0,18,false)] [InlineData(true,0,19,18,false)] [InlineData(true,double.NaN,0,18,false)] [InlineData(true,0,double.PositiveInfinity,18,false)]
        public void StationaryDriverAndRadiusGateAllPhysicalTransfers(bool driver,double speed,double distance,double radius,bool expected)
        {Assert.Equal(expected,PrisonRules.Stopped(driver,speed,distance,radius));}
        [Fact] public void SeventhContractStillSharesExclusiveGateWithAllPreviousJobs()
        {var g=new ShiftGate();var jobs=new[]{"pizza","postop","uber","valet","supply","tanker","prison"};foreach(string job in jobs){Assert.True(g.Acquire(job));foreach(string other in jobs)Assert.False(g.Acquire(other));g.Release(job);}}
    }
}
