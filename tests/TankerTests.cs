using System;
using System.Linq;
using Newtonsoft.Json;
using Xunit;

namespace RPCore.Tests
{
    public class TankerTests
    {
        [Fact] public void MustObserveCouplingBeforeUnloadOrReturn()
        {
            var r=new TankerRun(6);Assert.False(r.BeginUnload(0,true));Assert.False(r.ObserveCoupling(false));
            Assert.False(r.PayUnload(true,_=>throw new Exception()));Assert.False(r.Return(true,_=>throw new Exception()));
            Assert.True(r.ObserveCoupling(true));Assert.False(r.ObserveCoupling(true));Assert.Equal(TankerStage.Delivering,r.Stage);
        }
        [Fact] public void FullRunPaysOneUnloadAndOneReturnWithoutRatingOrCashMutation()
        {
            var r=new TankerRun(6);var w=new WalletData{Cash=700,Bank=100};bool credited;
            r.ObserveCoupling(true);Assert.True(r.BeginUnload(100,true));Assert.False(r.Advance(6099,true));Assert.True(r.Advance(6100,true));
            Assert.True(r.PayUnload(true,s=>DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,"tanker",r.Id,s,600,true,true,false,()=>{},out credited)));
            Assert.False(r.PayUnload(true,_=>throw new Exception()));Assert.Equal(700,w.Bank);Assert.Equal(1,w.Jobs.Get("tanker").Deliveries);Assert.Equal(0,w.Jobs.Get("tanker").CompletedShifts);
            Assert.True(r.Return(true,s=>DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,"tanker",r.Id,s,250,true,false,true,()=>{},out credited)));
            Assert.False(r.Return(true,_=>throw new Exception()));Assert.Equal(950,w.Bank);Assert.Equal(700,w.Cash);Assert.Equal(1,w.Jobs.Get("tanker").CompletedShifts);Assert.Equal(0,w.Jobs.Get("tanker").RatedTrips);
        }
        [Fact] public void InterruptResetsTimerAndDoesNotAllowPrematurePayment()
        {
            var r=new TankerRun(6);r.ObserveCoupling(true);r.BeginUnload(0,true);Assert.False(r.Advance(5999,true));
            Assert.False(r.Advance(6000,false));Assert.Equal(TankerStage.Delivering,r.Stage);Assert.False(r.PayUnload(true,_=>throw new Exception()));
            Assert.True(r.BeginUnload(6001,true));Assert.False(r.Advance(12000,true));Assert.True(r.Advance(12001,true));
        }
        [Fact] public void OpeningModalDuringUnloadRequiresRestart()
        {var r=new TankerRun(6);r.ObserveCoupling(true);r.BeginUnload(0,true);r.Interrupt();Assert.False(r.Advance(9000,true));Assert.Equal(TankerStage.Delivering,r.Stage);}
        [Fact] public void FailedUnloadPaymentKeepsPendingAndCanRetryWithoutAnotherUnload()
        {
            var r=new TankerRun(6);r.ObserveCoupling(true);r.BeginUnload(0,true);r.Advance(6000,true);
            Assert.False(r.PayUnload(true,_=>false));Assert.Equal(TankerStage.PaymentPending,r.Stage);r.Interrupt();Assert.False(r.BeginUnload(7000,true));
            Assert.False(r.PayUnload(false,_=>throw new Exception()));Assert.True(r.PayUnload(true,_=>true));Assert.Equal(TankerStage.Returning,r.Stage);
        }
        [Fact] public void FailedReturnCanRetryOnlyWithStoppedCompleteRig()
        {var r=ReadyToReturn();Assert.False(r.Return(false,_=>throw new Exception()));Assert.False(r.Return(true,_=>false));Assert.Equal(TankerStage.Returning,r.Stage);Assert.True(r.Return(true,_=>true));}
        static TankerRun ReadyToReturn(){var r=new TankerRun(6);r.ObserveCoupling(true);r.BeginUnload(0,true);r.Advance(6000,true);r.PayUnload(true,_=>true);return r;}
        [Theory] [InlineData(TankerStage.AwaitCoupling)] [InlineData(TankerStage.Delivering)] [InlineData(TankerStage.Unloading)] [InlineData(TankerStage.PaymentPending)] [InlineData(TankerStage.Returning)]
        public void CancellationBlocksFuturePaymentsAtEveryActiveStage(TankerStage stage)
        {
            var r=new TankerRun(6);
            if(stage>=TankerStage.Delivering)r.ObserveCoupling(true);
            if(stage>=TankerStage.Unloading)r.BeginUnload(0,true);
            if(stage>=TankerStage.PaymentPending)r.Advance(6000,true);
            if(stage>=TankerStage.Returning)r.PayUnload(true,_=>true);
            Assert.Equal(stage,r.Stage);r.Cancel();Assert.False(r.Advance(9000,true));Assert.False(r.PayUnload(true,_=>throw new Exception()));Assert.False(r.Return(true,_=>throw new Exception()));
        }
        [Fact] public void PaymentRollbackAndReceiptReplayDoNotLoseOldProgressOrPayTwice()
        {
            var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":20,\"Future\":true,\"Jobs\":{\"Jobs\":{\"supply\":{\"Deliveries\":5,\"Earned\":1000},\"uber\":{\"RatedTrips\":3,\"RatingSum\":15}}}}");
            string old=JsonConvert.SerializeObject(w);bool credited;
            Assert.Throws<InvalidOperationException>(()=>DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,"tanker","trip",1,500,true,true,false,()=>throw new InvalidOperationException(),out credited));
            Assert.Equal(old,JsonConvert.SerializeObject(w));Assert.True(DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,"tanker","trip",1,500,true,true,false,()=>{},out credited));Assert.True(credited);
            w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,"tanker","trip",1,500,true,true,false,()=>throw new Exception(),out credited));
            Assert.False(credited);Assert.Equal(520,w.Bank);Assert.Equal(5,w.Jobs.Get("supply").Deliveries);Assert.Equal(15,w.Jobs.Get("uber").RatingSum);Assert.True((bool)w.Extra["Future"]);
        }
        [Fact] public void TimerHandlesGameClockOverflow()
        {var r=new TankerRun(6);r.ObserveCoupling(true);r.BeginUnload(int.MaxValue-3000,true);Assert.False(r.Advance(unchecked(int.MaxValue+2999),true));Assert.True(r.Advance(unchecked(int.MaxValue+3000),true));}
        [Theory]
        [InlineData(true,true,true,0,0,25,25,true)]
        [InlineData(false,true,true,0,0,1,1,false)]
        [InlineData(true,false,true,0,0,1,1,false)]
        [InlineData(true,true,false,0,0,1,1,false)]
        [InlineData(true,true,true,.5,0,1,1,false)]
        [InlineData(true,true,true,0,.5,1,1,false)]
        [InlineData(true,true,true,0,0,26,1,false)]
        [InlineData(true,true,true,0,0,1,26,false)]
        [InlineData(true,true,true,0,0,double.NaN,1,false)]
        [InlineData(true,true,true,0,0,double.PositiveInfinity,1,false)]
        public void UnloadRequiresDriverExactCouplingEngineOffAndBothVehiclesStoppedAtStation(bool driver,bool coupled,bool off,double ts,double rs,double td,double rd,bool expected)
        {Assert.Equal(expected,TankerRules.Stopped(driver,coupled,off,ts,rs,td,rd,25));}
        [Theory]
        [InlineData(true,false,6,35,0,0,true)]
        [InlineData(false,false,1,0,0,0,false)]
        [InlineData(true,true,1,0,0,0,false)]
        [InlineData(true,false,7,0,0,0,false)]
        [InlineData(true,false,1,36,0,0,false)]
        [InlineData(true,false,1,0,.5,0,false)]
        [InlineData(true,false,1,0,0,.5,false)]
        [InlineData(true,false,double.NaN,0,0,0,false)]
        public void CouplingAssistanceRequiresAlignmentNearStoppedOwnTrailer(bool driver,bool hasTrailer,double distance,double heading,double ts,double rs,bool expected)
        {Assert.Equal(expected,TankerRules.CanCouple(driver,hasTrailer,distance,heading,ts,rs));}
        [Fact] public void EmbeddedConfigHasTwentyOneReferenceStationsAndDepot()
        {var c=TankerDefaults.Load();Assert.True(c.ValidTanker);Assert.Equal(21,c.Addresses.Count);Assert.Equal(481.9995f,c.Start.X);Assert.Equal(-2172.934f,c.Start.Y);Assert.Equal(524.8837f,c.Trailer.X);Assert.Equal(170.0806f,c.Trailer.Heading);}
        [Fact] public void StationSelectionUsesNearestCandidatesAndRetainsConfig()
        {
            var c=TankerDefaults.Load();string before=JsonConvert.SerializeObject(c);var nearest=c.Addresses.OrderBy(p=>DeliveryRoute.Distance(c.Start,p)).Take(c.CandidateStations).ToList();
            for(int i=0;i<40;i++)Assert.Contains(TankerDefaults.Select(c,new Random(i)),nearest);
            Assert.Equal(before,JsonConvert.SerializeObject(c));
        }
        [Theory] [InlineData(0,6,1)] [InlineData(101,6,1)] [InlineData(8,2,1)] [InlineData(8,31,1)] [InlineData(8,6,2)]
        public void InvalidCustomConfigCannotStartImpossibleContract(int candidates,int seconds,int drops)
        {var c=TankerDefaults.Load();c.CandidateStations=candidates;c.UnloadSeconds=seconds;c.Drops=drops;Assert.False(c.ValidTanker);Assert.Throws<ArgumentException>(()=>TankerDefaults.Select(c,new Random()));}
        [Fact] public void AllSixContractsUseExclusiveLease()
        {var g=new ShiftGate();var jobs=new[]{"pizza","postop","uber","valet","supply","tanker"};foreach(string job in jobs){Assert.True(g.Acquire(job));foreach(string other in jobs)Assert.False(g.Acquire(other));g.Release(job);}}
    }
}
