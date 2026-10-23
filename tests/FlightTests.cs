using System;
using Newtonsoft.Json;
using Xunit;

namespace RPCore.Tests
{
    public class FlightTests
    {
        static FlightRun Prepared(FlightMode mode)
        {var r=new FlightRun(mode,mode==FlightMode.Training?0:mode==FlightMode.Plane?2:1);Assert.True(r.Prepare(true,r.Passengers));return r;}
        static FlightRun Flying(FlightMode mode)
        {var r=Prepared(mode);r.ObserveFlight(0,true);Assert.True(r.ObserveFlight(mode==FlightMode.Helicopter?6000:10000,true));return r;}
        static FlightRun Arrived(FlightMode mode)
        {var r=Flying(mode);r.Arrive(true);if(r.Passengers>0)r.Disembarked(true,r.Passengers);return r;}
        [Fact] public void TrainingCannotCompleteFromTaxiingWithoutFlight()
        {var r=Prepared(FlightMode.Training);Assert.False(r.Arrive(true));r.ObserveFlight(0,false);r.ObserveFlight(12000,false);Assert.False(r.Arrive(true));Assert.False(r.PayArrival(true,_=>throw new Exception()));}
        [Fact] public void RequiresContinuousObservedFlightAndRestartsAfterInterruption()
        {
            var r=Prepared(FlightMode.Plane);r.ObserveFlight(0,true);Assert.False(r.ObserveFlight(9999,true));r.ObserveFlight(10000,false);
            Assert.False(r.ObserveFlight(20000,true));Assert.False(r.ObserveFlight(29999,true));Assert.True(r.ObserveFlight(30000,true));Assert.Equal(FlightStage.EnRoute,r.Stage);
        }
        [Fact] public void HelicopterRequiresSixSecondsAndTimerSurvivesOverflow()
        {var r=Prepared(FlightMode.Helicopter);r.ObserveFlight(int.MaxValue-3000,true);Assert.False(r.ObserveFlight(unchecked(int.MaxValue+2999),true));Assert.True(r.ObserveFlight(unchecked(int.MaxValue+3000),true));}
        [Fact] public void MissingPassengerOrWrongPilotPreventsPreparation()
        {var r=new FlightRun(FlightMode.Plane,2);Assert.False(r.Prepare(false,2));Assert.False(r.Prepare(true,1));Assert.True(r.Prepare(true,2));Assert.False(r.Prepare(true,2));}
        [Fact] public void TrainingQualificationIsAtomicPersistentAndPaysNoCash()
        {
            var w=new WalletData{Cash=500,Bank=900};var r=Arrived(FlightMode.Training);bool credited;
            Assert.False(FlightRules.Qualified(w.Jobs));
            Assert.True(r.PayArrival(true,s=>DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,"pilot_training",r.Id,s,0,true,false,true,()=>{},out credited)));
            Assert.Equal(FlightStage.Finished,r.Stage);Assert.True(FlightRules.Qualified(w.Jobs));Assert.Equal(900,w.Bank);Assert.Equal(500,w.Cash);Assert.Equal(0,w.Jobs.Get("pilot_training").Deliveries);
            w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(FlightRules.Qualified(w.Jobs));Assert.False(r.PayArrival(true,_=>throw new Exception()));
        }
        [Fact] public void FailedQualificationSaveDoesNotUnlockCommercialFlights()
        {
            var w=new WalletData{Bank=50};var r=Arrived(FlightMode.Training);bool credited;
            Assert.Throws<InvalidOperationException>(()=>r.PayArrival(true,s=>DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,"pilot_training",r.Id,s,0,true,false,true,()=>throw new InvalidOperationException(),out credited)));
            Assert.False(FlightRules.Qualified(w.Jobs));Assert.Equal(FlightStage.PaymentPending,r.Stage);Assert.Equal(50,w.Bank);
        }
        [Theory] [InlineData(FlightMode.Plane)] [InlineData(FlightMode.Helicopter)]
        public void FullCommercialFlightPaysArrivalAndOnlyOneReturnBonus(FlightMode mode)
        {
            var r=Flying(mode);var w=new WalletData{Cash=100,Bank=50};bool credited;string job=mode==FlightMode.Plane?"pilot_plane":"pilot_heli";
            Assert.False(r.Arrive(false));Assert.True(r.Arrive(true));Assert.False(r.PayArrival(true,_=>throw new Exception()));Assert.False(r.Disembarked(false,r.Passengers));Assert.False(r.Disembarked(true,0));Assert.True(r.Disembarked(true,r.Passengers));
            Assert.True(r.PayArrival(true,s=>DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,job,r.Id,s,2000,true,true,false,()=>{},out credited)));
            Assert.False(r.PayArrival(true,_=>throw new Exception()));Assert.False(r.Return(true,_=>throw new Exception()));
            r.ObserveFlight(20000,true);r.ObserveFlight(30000,true);Assert.True(r.ReturnFlightConfirmed);
            Assert.False(r.Return(false,_=>throw new Exception()));Assert.True(r.Return(true,s=>DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,job,r.Id,s,500,true,false,true,()=>{},out credited)));
            Assert.False(r.Return(true,_=>throw new Exception()));Assert.Equal(2550,w.Bank);Assert.Equal(100,w.Cash);Assert.Equal(1,w.Jobs.Get(job).Deliveries);Assert.Equal(1,w.Jobs.Get(job).CompletedShifts);
        }
        [Fact] public void FailedArrivalPaymentRetainsPassengersAndCanRetry()
        {var r=Arrived(FlightMode.Plane);Assert.False(r.PayArrival(false,_=>throw new Exception()));Assert.False(r.PayArrival(true,_=>false));Assert.Equal(FlightStage.PaymentPending,r.Stage);Assert.True(r.PayArrival(true,_=>true));}
        [Fact] public void FailedReturnCheckpointRemainsRetryable()
        {var r=Arrived(FlightMode.Plane);r.PayArrival(true,_=>true);r.ObserveFlight(0,true);r.ObserveFlight(10000,true);Assert.False(r.Return(true,_=>false));Assert.Equal(FlightStage.Returning,r.Stage);Assert.True(r.Return(true,_=>true));}
        [Theory] [InlineData(FlightStage.AwaitPilot)] [InlineData(FlightStage.AwaitTakeoff)] [InlineData(FlightStage.EnRoute)] [InlineData(FlightStage.Exiting)] [InlineData(FlightStage.PaymentPending)] [InlineData(FlightStage.Returning)]
        public void CancelStopsAllFurtherPayments(FlightStage stage)
        {
            var r=new FlightRun(FlightMode.Plane,2);
            if(stage>=FlightStage.AwaitTakeoff)r.Prepare(true,2);
            if(stage>=FlightStage.EnRoute){r.ObserveFlight(0,true);r.ObserveFlight(10000,true);}
            if(stage>=FlightStage.Exiting)r.Arrive(true);
            if(stage>=FlightStage.PaymentPending)r.Disembarked(true,2);
            if(stage>=FlightStage.Returning)r.PayArrival(true,_=>true);
            Assert.Equal(stage,r.Stage);r.Cancel();Assert.False(r.PayArrival(true,_=>throw new Exception()));Assert.False(r.Return(true,_=>throw new Exception()));Assert.False(r.Arrive(true));
        }
        [Theory]
        [InlineData(true,false,1,0,10,2,12,true)]
        [InlineData(false,false,1,0,10,2,12,false)]
        [InlineData(true,true,1,0,10,2,12,false)]
        [InlineData(true,false,4,0,10,2,12,false)]
        [InlineData(true,false,1,.5,10,2,12,false)]
        [InlineData(true,false,1,0,13,2,12,false)]
        [InlineData(true,false,1,0,10,100,12,false)]
        [InlineData(true,false,double.NaN,0,10,2,12,false)]
        public void HoveringAndStreetBelowRoofNeverCountAsLanded(bool pilot,bool air,double height,double speed,double xy,double z,double radius,bool expected)
        {Assert.Equal(expected,FlightRules.Landed(pilot,air,height,speed,xy,z,radius));}
        [Theory]
        [InlineData(true,true,20,15,false,true)] [InlineData(true,true,19,15,false,false)]
        [InlineData(true,true,20,14,false,false)] [InlineData(false,true,30,25,false,false)]
        [InlineData(true,false,30,25,false,false)] [InlineData(true,true,10,3,true,true)]
        [InlineData(true,true,9,3,true,false)]
        public void FlightObservationRequiresRealAltitudePilotAndSpeed(bool pilot,bool air,double height,double speed,bool heli,bool expected)
        {Assert.Equal(expected,FlightRules.InFlight(pilot,air,height,speed,heli));}
        [Theory]
        [InlineData(true,false,1,0,true)] [InlineData(false,false,1,0,false)] [InlineData(true,true,1,0,false)]
        [InlineData(true,false,1,1,false)] [InlineData(true,false,100,0,false)]
        public void RetiredAircraftIsCollectedOnlyEmptyStoppedAndOnGround(bool empty,bool airborne,double height,double speed,bool expected)
        {Assert.Equal(expected,FlightRules.CanRetire(empty,airborne,height,speed));}
        [Theory] [InlineData(1000,1000,true)] [InlineData(399,1000,false)] [InlineData(1000,399,false)] [InlineData(float.NaN,1000,false)]
        public void SeverelyDamagedAircraftCannotComplete(float engine,float body,bool expected)
        {Assert.Equal(expected,FlightRules.Airworthy(engine,body,400));}
        [Fact] public void TimedExecutiveFlightDeadlineSupportsDisabledOptionAndOverflow()
        {Assert.False(FlightRules.Expired(149999,0,150));Assert.True(FlightRules.Expired(150000,0,150));Assert.False(FlightRules.Expired(999999,0,0));Assert.True(FlightRules.Expired(unchecked(int.MaxValue+1000),int.MaxValue-149000,150));}
        [Fact] public void EmbeddedDefaultsContainReferenceAirportsAndFiveRoofHelipads()
        {var c=PilotDefaults.Load();Assert.True(c.Valid);Assert.Equal(5,c.Helipads.Count);Assert.Equal(-1143.238f,c.Start.X);Assert.Equal(1698.431f,c.PlaneDestination.X);Assert.Equal(3249.649f,c.PlaneDestination.Y);Assert.Equal(211.775f,c.Helipads[0].Z);Assert.Equal("velum",c.PlaneModel);Assert.Equal("frogger",c.HeliModel);}
        [Fact] public void InvalidConfigCannotRequestUnavailableTimedHelipadOrImpossibleManifest()
        {var c=PilotDefaults.Load();c.TimedHelipadIndex=5;Assert.False(c.Valid);c.TimedHelipadIndex=-1;Assert.True(c.Valid);c.HeliPassengers=4;Assert.False(c.Valid);}
        [Fact] public void ReplayDoesNotRepayAndEarlierJobsRemainUnchanged()
        {
            var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":50,\"Future\":true,\"Jobs\":{\"Jobs\":{\"prison\":{\"Deliveries\":1,\"Earned\":2400},\"tanker\":{\"CompletedShifts\":2}}}}");
            string old=JsonConvert.SerializeObject(w.Jobs);bool credited;DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,"pilot_plane","flight",1,2000,true,true,false,()=>{},out credited);
            Assert.True(credited);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,"pilot_plane","flight",1,2000,true,true,false,()=>throw new Exception(),out credited);
            Assert.False(credited);Assert.Equal(2050,w.Bank);Assert.Equal(2400,w.Jobs.Get("prison").Earned);Assert.Equal(2,w.Jobs.Get("tanker").CompletedShifts);Assert.True((bool)w.Extra["Future"]);
        }
        [Fact] public void EighthContractStillUsesExclusiveSharedLease()
        {var g=new ShiftGate();var jobs=new[]{"pizza","postop","uber","valet","supply","tanker","prison","pilot"};foreach(var job in jobs){Assert.True(g.Acquire(job));foreach(var other in jobs)Assert.False(g.Acquire(other));g.Release(job);}}
    }
}
