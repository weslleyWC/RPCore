using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace RPCore.Tests
{
    public class DrivingJobTests
    {
        [Theory] [InlineData("null")] [InlineData("{")]
        public void InvalidOrNullDefinitionRemainsByteIdenticalWhileFallbackIsUsed(string json)
        {
            string root=Path.Combine(Path.GetTempPath(),"rpcore-def-"+Guid.NewGuid());Directory.CreateDirectory(root);
            try
            {
                string path=Path.Combine(root,"Uber.json");File.WriteAllText(path,json);int errors=0;
                var cfg=JsonDefinitions.Load(path,DrivingDefaults.Uber,_=>errors++);
                Assert.True(cfg.Valid);Assert.Equal(json,File.ReadAllText(path));Assert.Equal(1,errors);
            }
            finally{Directory.Delete(root,true);}
        }
        [Fact] public void ValidCustomDefinitionIsReadWithoutRewritingUnknownFields()
        {
            string root=Path.Combine(Path.GetTempPath(),"rpcore-def-"+Guid.NewGuid());Directory.CreateDirectory(root);
            try
            {
                string path=Path.Combine(root,"Uber.json");var cfg=DrivingDefaults.Uber();cfg.TripsPerShift=2;cfg.Extra=new Dictionary<string,JToken>{{"Future",true}};
                string json=JsonConvert.SerializeObject(cfg);File.WriteAllText(path,json);
                var loaded=JsonDefinitions.Load(path,DrivingDefaults.Uber);Assert.Equal(2,loaded.TripsPerShift);Assert.True((bool)loaded.Extra["Future"]);Assert.Equal(json,File.ReadAllText(path));
            }
            finally{Directory.Delete(root,true);}
        }
        [Fact] public void DefinitionIsCreatedOnlyWhenMissing()
        {
            string root=Path.Combine(Path.GetTempPath(),"rpcore-def-"+Guid.NewGuid());string path=Path.Combine(root,"Jobs","Uber.json");
            try
            {
                Assert.True(JsonDefinitions.Load(path,DrivingDefaults.Uber).Valid);string original=File.ReadAllText(path);
                var loaded=JsonDefinitions.Load<UberOptions>(path,()=>throw new Exception("Should not need defaults"));Assert.NotNull(loaded);Assert.Equal(original,File.ReadAllText(path));
            }
            finally{Directory.Delete(root,true);}
        }
        [Fact] public void RideCannotPayBeforePassengerBoardsReachesDestinationAndExits()
        {
            var r=new RideRun();Assert.False(r.Pay(()=>throw new Exception()));Assert.False(r.PassengerExited(true));
            Assert.True(r.Board());Assert.False(r.Boarded(false));Assert.Equal(RideStage.Boarding,r.Stage);Assert.True(r.Boarded(true));
            Assert.False(r.Arrived(false));Assert.False(r.PassengerExited(true));Assert.True(r.Arrived(true));Assert.False(r.PassengerExited(false));Assert.True(r.PassengerExited(true));
            int credits=0;Assert.True(r.Pay(()=>{credits++;return true;}));Assert.False(r.Pay(()=>throw new Exception()));Assert.Equal(1,credits);
        }
        [Fact] public void FailedRidePaymentCanRetryWithoutRepeatingTheTrip()
        {
            var r=AtRidePayment();Assert.False(r.Pay(()=>false));Assert.Equal(RideStage.Payment,r.Stage);
            Assert.Throws<IOException>(()=>r.Pay(()=>throw new IOException()));Assert.Equal(RideStage.Payment,r.Stage);Assert.True(r.Pay(()=>true));
        }
        [Fact] public void BoardingTimeoutRequiresANewAttemptAndDoesNotCompleteAnything()
        {var r=new RideRun();r.Board();r.BoardingTimedOut();Assert.Equal(RideStage.Pickup,r.Stage);Assert.False(r.Boarded(true));Assert.False(r.Pay(()=>true));Assert.True(r.Board());Assert.True(r.Boarded(true));}
        [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
        public void CancelAtEveryRideStagePreventsPayment(int stage)
        {
            var r=new RideRun();if(stage>0)r.Board();if(stage>1)r.Boarded(true);if(stage>2)r.Arrived(true);if(stage>3)r.PassengerExited(true);
            r.Cancel();Assert.Equal(RideStage.Cancelled,r.Stage);Assert.False(r.Pay(()=>throw new Exception()));
        }
        static RideRun AtRidePayment(){var r=new RideRun();r.Board();r.Boarded(true);r.Arrived(true);r.PassengerExited(true);return r;}
        static ValetRun AtValetPayment()
        {
            var r=new ValetRun();r.Arrived(true);r.ClientLeft(true);r.Park(true);r.RequestReturn(true);r.Collect(true);r.Return(true);r.ClientBoarded(true);return r;
        }
        [Fact] public void ValetRequiresSameCarParkLockRetrieveAndActualClientBoarding()
        {
            var r=new ValetRun();Assert.False(r.Pay(()=>throw new Exception()));Assert.False(r.Park(true));Assert.False(r.Arrived(false));Assert.True(r.Arrived(true));
            Assert.False(r.ClientLeft(false));Assert.True(r.ClientLeft(true));Assert.False(r.Park(false));Assert.True(r.Park(true));
            Assert.False(r.RequestReturn(false));Assert.True(r.RequestReturn(true));Assert.False(r.Collect(false));Assert.True(r.Collect(true));
            Assert.False(r.Return(false));Assert.True(r.Return(true));Assert.False(r.ClientBoarded(false));Assert.True(r.ClientBoarded(true));
            int credits=0;Assert.True(r.Pay(()=>{credits++;return true;}));Assert.False(r.Pay(()=>throw new Exception()));Assert.Equal(1,credits);
        }
        [Fact] public void ValetParkingAloneCannotEarnReturnPayment()
        {var r=new ValetRun();r.Arrived(true);r.ClientLeft(true);r.Park(true);Assert.False(r.Pay(()=>throw new Exception()));Assert.Equal(ValetStage.WalkToStand,r.Stage);}
        [Fact] public void ValetFailedCheckpointKeepsClientHandoverRetryable()
        {var r=AtValetPayment();Assert.False(r.Pay(()=>false));Assert.Equal(ValetStage.Payment,r.Stage);Assert.True(r.Pay(()=>true));}
        [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
        public void CancelAtEveryValetStagePreventsMoney(int stage)
        {
            var r=new ValetRun();if(stage>0)r.Arrived(true);if(stage>1)r.ClientLeft(true);if(stage>2)r.Park(true);if(stage>3)r.RequestReturn(true);if(stage>4)r.Collect(true);if(stage>5)r.Return(true);if(stage>6)r.ClientBoarded(true);
            r.Cancel();Assert.False(r.Pay(()=>throw new Exception()));Assert.Equal(ValetStage.Cancelled,r.Stage);
        }
        [Fact] public void DrivingShiftFinishesOnlyAfterConfiguredSuccessfulServices()
        {
            var s=new DrivingShift(3);var steps=new List<int>();var last=new List<bool>();
            Assert.False(s.Confirm((n,end)=>false));Assert.Equal(0,s.Completed);
            for(int i=0;i<3;i++)Assert.True(s.Confirm((n,end)=>{steps.Add(n);last.Add(end);return true;}));
            Assert.True(s.Finished);Assert.Equal(new[]{1,2,3},steps);Assert.Equal(new[]{false,false,true},last);Assert.False(s.Confirm((n,end)=>throw new Exception()));
        }
        [Fact] public void PartialShiftCancelDoesNotPayAnotherServiceOrBonus()
        {var s=new DrivingShift(3);s.Confirm((n,last)=>true);s.Cancel();Assert.False(s.Confirm((n,last)=>throw new Exception()));Assert.Equal(1,s.Completed);Assert.False(s.Finished);}
        [Theory] [InlineData(0)] [InlineData(31)]
        public void InvalidDrivingShiftSizeIsRejected(int size)=>Assert.Throws<ArgumentOutOfRangeException>(()=>new DrivingShift(size));
        [Theory] [InlineData(0,5)] [InlineData(10,5)] [InlineData(11,4)] [InlineData(75,4)] [InlineData(76,3)] [InlineData(200,3)] [InlineData(201,2)] [InlineData(400,2)] [InlineData(401,1)]
        public void RatingUsesMeasuredPeakBodyDamage(float damage,int stars)
        {var q=new DrivingQuality(1000);q.Sample(1000-damage);Assert.Equal(stars,q.Stars);q.Sample(1000);Assert.Equal(stars,q.Stars);}
        [Fact] public void RepairingDuringTripDoesNotErasePreviouslyMeasuredDamage()
        {var q=new DrivingQuality(950);q.Sample(700);q.Sample(1000);Assert.Equal(250,q.Damage);Assert.Equal(2,q.Stars);Assert.Equal(165,q.Reward(65,20,100));}
        [Fact] public void CleanTripGetsTipAndOnlyTheLastServiceGetsShiftBonus()
        {var q=new DrivingQuality(900);Assert.Equal(85,q.Reward(65,20,0));Assert.Equal(185,q.Reward(65,20,100));q.Sample(800);Assert.Equal(65,q.Reward(65,20,0));}
        [Theory] [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)] [InlineData(-1f)]
        public void InvalidBodyHealthCannotCorruptFinancialArithmetic(float health)
        {var q=new DrivingQuality(1000);q.Sample(health);Assert.Equal(0,q.Damage);Assert.Equal(5,q.Stars);}
        [Fact] public void DrivingRewardsAreBoundedAgainstOverflow()=>Assert.Equal(1000000,new DrivingQuality(1000).Reward(int.MaxValue,int.MaxValue,int.MaxValue));
        [Fact] public void AllFourJobsShareOneShiftLease()
        {var g=new ShiftGate();foreach(var job in new[]{"pizza","postop","uber","valet"}){Assert.True(g.Acquire(job));foreach(var other in new[]{"pizza","postop","uber","valet"})Assert.False(g.Acquire(other));g.Release(job);}}
        static bool Reward(WalletData w,string job,string id,int step,int rating,Action save,out bool credited)
            =>DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,job,id,step,100,true,true,step==3,save,out credited,rating);
        [Fact] public void RatingBankCreditAndReceiptAreOneCheckpointAndReplayCannotDuplicateAny()
        {
            var w=new WalletData{Bank=500};int writes=0;bool credited;
            Assert.True(Reward(w,"uber","shift",1,5,()=>writes++,out credited));Assert.True(credited);
            Assert.True(Reward(w,"uber","shift",1,1,()=>throw new Exception(),out credited));Assert.False(credited);
            var p=w.Jobs.Get("uber");Assert.Equal(600,w.Bank);Assert.Equal(1,p.RatedTrips);Assert.Equal(5,p.RatingSum);Assert.Equal(1,writes);
        }
        [Fact] public void FailedRatingSaveRollsBackExistingLedgerAndAllowsOneRetry()
        {
            var w=new WalletData{Bank=500};bool credited;Reward(w,"uber","shift",1,4,()=>{},out credited);var before=JsonConvert.SerializeObject(w);
            Assert.Throws<IOException>(()=>Reward(w,"uber","shift",2,5,()=>throw new IOException(),out credited));Assert.Equal(before,JsonConvert.SerializeObject(w));
            Assert.True(Reward(w,"uber","shift",2,5,()=>{},out credited));Assert.True(credited);Assert.Equal(2,w.Jobs.Get("uber").RatedTrips);Assert.Equal(9,w.Jobs.Get("uber").RatingSum);
        }
        [Theory] [InlineData(-1)] [InlineData(6)]
        public void InvalidRatingCannotCreateMoneyOrAReceipt(int rating)
        {var w=new WalletData();bool credited;Assert.False(Reward(w,"uber","s",1,rating,()=>throw new Exception(),out credited));Assert.Null(w.Jobs);Assert.Equal(0,w.Bank);}
        [Fact] public void OldLedgerWithoutRatingFieldsAndUnknownDataSurvivesNewAwards()
        {
            var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":500,\"Jobs\":{\"Jobs\":{\"pizza\":{\"Deliveries\":42,\"Earned\":9001,\"ExtraJob\":true}}},\"Future\":{\"keep\":7}}");bool credited;
            Reward(w,"uber","s",1,5,()=>{},out credited);Assert.Equal(42,w.Jobs.Get("pizza").Deliveries);Assert.Equal(9001,w.Jobs.Get("pizza").Earned);Assert.Equal(0,w.Jobs.Get("pizza").RatedTrips);
            var output=JObject.Parse(JsonConvert.SerializeObject(w));Assert.True((bool)output["Jobs"]["Jobs"]["pizza"]["ExtraJob"]);Assert.Equal(7,(int)output["Future"]["keep"]);
        }
        [Theory] [InlineData("uber")] [InlineData("valet")]
        public void FullThreeServiceShiftCommitsThreeCreditsOneBonusAndThreeRatings(string job)
        {
            var w=new WalletData();var shift=new DrivingShift(3);bool credited;
            for(int i=0;i<3;i++)
            {
                Func<bool> finish=()=>shift.Confirm((step,last)=>DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,job,shift.Id,step,last?200:100,true,true,last,()=>{},out credited,5));
                if(job=="uber"){var run=AtRidePayment();Assert.True(run.Pay(finish));Assert.False(run.Pay(()=>throw new Exception()));}
                else{var run=AtValetPayment();Assert.True(run.Pay(finish));Assert.False(run.Pay(()=>throw new Exception()));}
            }
            Assert.Equal(400,w.Bank);Assert.Equal(3,w.Jobs.Get(job).Deliveries);Assert.Equal(1,w.Jobs.Get(job).CompletedShifts);Assert.Equal(3,w.Jobs.Get(job).RatedTrips);Assert.Equal(15,w.Jobs.Get(job).RatingSum);
        }
        [Fact] public void ReloadedBankAndDriverRatingsRemainProtectedFromDuplicatePayment()
        {
            var w=new WalletData();bool credited;Reward(w,"uber","s",1,4,()=>{},out credited);var restored=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));
            Assert.True(Reward(restored,"uber","s",1,5,()=>throw new Exception(),out credited));Assert.False(credited);Assert.Equal(4,restored.Jobs.Get("uber").RatingSum);Assert.Equal(100,restored.Bank);
        }
        [Fact] public void EmbeddedDrivingDefaultsAreValidWithoutSidecars()
        {Assert.True(DrivingDefaults.Uber().Valid);Assert.Equal(48,DrivingDefaults.Uber().Locations.Count);Assert.True(DrivingDefaults.Valet().Valid);Assert.Equal(3,DrivingDefaults.Valet().CustomersPerShift);}
        [Fact] public void OffersStayNearDriverAndUseDistinctDestinationsWithinServiceRange()
        {
            var cfg=DrivingDefaults.Uber();var origin=cfg.Locations[0];var before=JsonConvert.SerializeObject(cfg);
            for(int i=0;i<40;i++){var offer=RideOffers.Select(cfg,origin,new Random(i));Assert.NotNull(offer);Assert.True(DeliveryRoute.Distance(origin,offer.Item1)<=cfg.MaxPickupDistance);Assert.InRange(DeliveryRoute.Distance(offer.Item1,offer.Item2),500,6000);}
            Assert.Equal(before,JsonConvert.SerializeObject(cfg));
        }
        [Fact] public void UnsupportedAreaDoesNotGenerateAnUnreachablePickup()
        {var cfg=DrivingDefaults.Uber();Assert.Null(RideOffers.Select(cfg,new JobPoint{Name="remote",X=14000,Y=14000,Z=50},new Random()));}
        [Fact] public void OfferSelectionSkipsIsolatedPickupWhenOtherValidRoutesExist()
        {
            var cfg=new UberOptions{MaxPickupDistance=8000,Locations=new List<JobPoint>{new JobPoint{Name="isolated",X=0,Y=0,Z=30},new JobPoint{Name="a",X=7000,Y=0,Z=30},new JobPoint{Name="b",X=7500,Y=0,Z=30}}};
            for(int i=0;i<30;i++)Assert.NotEqual("isolated",RideOffers.Select(cfg,new JobPoint{Name="driver",X=6000,Y=0,Z=30},new Random(i)).Item1.Name);
        }
        [Fact] public void TooDistantOnlyPairRejectsCustomUberConfiguration()
        {var cfg=new UberOptions{Locations=new List<JobPoint>{new JobPoint{Name="a",X=0,Y=0,Z=30},new JobPoint{Name="b",X=9000,Y=0,Z=30}}};Assert.False(cfg.Valid);}
        [Fact] public void InvalidValetParkingOrMissingModelsRejectCustomConfiguration()
        {var cfg=DrivingDefaults.Valet();cfg.ParkingRadius=float.NaN;Assert.False(cfg.Valid);cfg.ParkingRadius=50;cfg.Cars.Clear();Assert.False(cfg.Valid);}
        [Theory] [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)] [InlineData(-1f)]
        public void InvalidUberPayRateRejectsCustomConfiguration(float rate)
        {var cfg=DrivingDefaults.Uber();cfg.PayPerMeter=rate;Assert.False(cfg.Valid);}
    }
}
