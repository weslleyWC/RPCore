using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace RPCore.Tests
{
    public class FreightTests
    {
        [Fact] public void TruckCannotCollectOrDeliverBeforeLoadingAtBase()
        {
            var r=new DeliveryRun(5,true,true);Assert.False(r.LoadCargo());Assert.True(r.EnterVehicle());Assert.Equal(DeliveryStage.AwaitLoading,r.Stage);
            Assert.False(r.TakePackage());Assert.False(r.BeginHandover());Assert.False(r.CommitDelivery(_=>throw new Exception()));Assert.False(r.ReturnToDepot(_=>throw new Exception()));
            Assert.True(r.LoadCargo());Assert.False(r.LoadCargo());Assert.Equal(DeliveryStage.NeedPackage,r.Stage);Assert.True(r.TakePackage());
        }
        [Fact] public void ExistingPizzaAndPostOpRunsStillStartWithoutBaseLoading()
        {var r=new DeliveryRun(5,true);Assert.True(r.EnterVehicle());Assert.Equal(DeliveryStage.NeedPackage,r.Stage);Assert.False(r.LoadCargo());Assert.True(r.TakePackage());}
        [Fact] public void FiveStoreRunPaysFiveUnloadsAndOnlyOneReturnBonus()
        {
            var r=new DeliveryRun(5,true,true);var w=new WalletData{Bank=100,Cash=700};bool credited;
            r.EnterVehicle();r.LoadCargo();
            for(int i=0;i<5;i++)
            {
                r.TakePackage();r.BeginHandover();Assert.True(r.CommitDelivery(step=>DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,"supply",r.Id,step,180,true,true,false,()=>{},out credited)));
                Assert.False(r.CommitDelivery(_=>throw new Exception("Duplicate unload")));
            }
            Assert.Equal(5,r.Delivered);Assert.Equal(DeliveryStage.Returning,r.Stage);Assert.Equal(1000,w.Bank);Assert.Equal(700,w.Cash);Assert.Equal(0,w.Jobs.Get("supply").CompletedShifts);
            Assert.True(r.ReturnToDepot(step=>DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,"supply",r.Id,step,300,true,false,true,()=>{},out credited)));
            Assert.False(r.ReturnToDepot(_=>throw new Exception("Duplicate bonus")));Assert.Equal(1300,w.Bank);Assert.Equal(5,w.Jobs.Get("supply").Deliveries);Assert.Equal(1,w.Jobs.Get("supply").CompletedShifts);
            Assert.Equal(0,w.Jobs.Get("supply").RatedTrips);
        }
        [Fact] public void RecoveringACrateCannotGenerateASecondPaymentOrConsumeStock()
        {
            var r=new DeliveryRun(2,true,true);r.EnterVehicle();r.LoadCargo();r.TakePackage();r.RecoverPackage();Assert.Equal(0,r.Delivered);Assert.True(r.TakePackage());
            r.BeginHandover();r.InterruptHandover();Assert.False(r.CommitDelivery(_=>throw new Exception()));r.BeginHandover();Assert.True(r.CommitDelivery(_=>true));Assert.Equal(1,r.Delivered);Assert.Equal(DeliveryStage.NeedPackage,r.Stage);
        }
        [Fact] public void FailedUnloadCheckpointKeepsCargoAndRouteStepRetryable()
        {var r=new DeliveryRun(2,true,true);r.EnterVehicle();r.LoadCargo();r.TakePackage();r.BeginHandover();Assert.False(r.CommitDelivery(_=>false));Assert.Equal(0,r.Delivered);Assert.Equal(DeliveryStage.Handover,r.Stage);Assert.True(r.CommitDelivery(_=>true));Assert.Equal(1,r.Delivered);}
        [Fact] public void CancelBeforeLoadingCannotCreditAnything()
        {var r=new DeliveryRun(5,true,true);r.EnterVehicle();r.Cancel();Assert.False(r.LoadCargo());Assert.False(r.TakePackage());Assert.False(r.ReturnToDepot(_=>throw new Exception()));Assert.Equal(0,r.Delivered);}
        [Fact] public void CancelPartialFreightKeepsOnlyPreviouslyConfirmedUnloads()
        {
            var r=new DeliveryRun(5,true,true);var w=new WalletData();bool credited;r.EnterVehicle();r.LoadCargo();r.TakePackage();r.BeginHandover();
            r.CommitDelivery(step=>DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,"supply",r.Id,step,180,true,true,false,()=>{},out credited));r.Cancel();
            Assert.False(r.ReturnToDepot(_=>throw new Exception()));Assert.Equal(180,w.Bank);Assert.Equal(1,w.Jobs.Get("supply").Deliveries);Assert.Equal(0,w.Jobs.Get("supply").CompletedShifts);
        }
        [Fact] public void FreightLedgerDoesNotChangeExistingJobsOrWalletExtensions()
        {
            var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":100,\"Jobs\":{\"Jobs\":{\"pizza\":{\"Deliveries\":20,\"Earned\":700},\"uber\":{\"Deliveries\":3,\"Earned\":900,\"RatingSum\":15,\"RatedTrips\":3}}},\"Future\":{\"keep\":true}}");
            var before=JsonConvert.SerializeObject(w.Jobs.Jobs["uber"]);bool credited;DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,"supply","s",1,180,true,true,false,()=>{},out credited);
            Assert.Equal(before,JsonConvert.SerializeObject(w.Jobs.Jobs["uber"]));Assert.Equal(20,w.Jobs.Get("pizza").Deliveries);Assert.True((bool)w.Extra["Future"]["keep"]);
        }
        [Fact] public void StoreDeliveryDefaultsHaveTwentyValidStoresAndTheReferenceDepot()
        {var c=FreightDefaults.Load();Assert.True(c.ValidFreight);Assert.Equal(20,c.Addresses.Count);Assert.Equal(30.10219f,c.Depot.X);Assert.Equal(-1761.373f,c.Depot.Y);Assert.Equal(5,c.Drops);Assert.Equal(3,c.LoadSeconds);}
        [Fact] public void DefaultRoutesStayInTheNearestEightStoresWithoutDuplicatesOrMutation()
        {
            var c=FreightDefaults.Load();var before=JsonConvert.SerializeObject(c);var near=c.Addresses.OrderBy(p=>DeliveryRoute.Distance(c.Start,p)).Take(c.CandidateStores).ToList();
            for(int seed=0;seed<30;seed++){var route=FreightDefaults.Route(c,new Random(seed));Assert.Equal(5,route.Count);Assert.Equal(5,route.Distinct().Count());Assert.All(route,p=>Assert.Contains(p,near));}
            Assert.Equal(before,JsonConvert.SerializeObject(c));
        }
        [Fact] public void DuplicatedCustomAddressesDoNotDuplicateRouteStops()
        {var c=FreightDefaults.Load();c.Addresses.AddRange(c.Addresses.ToArray());var route=FreightDefaults.Route(c,new Random(7));Assert.Equal(5,route.Select(p=>new {p.X,p.Y,p.Z}).Distinct().Count());}
        [Theory] [InlineData(0)] [InlineData(1)] [InlineData(21)]
        public void InvalidLoadingDurationRejectsCustomJobConfig(int seconds)
        {var c=FreightDefaults.Load();c.LoadSeconds=seconds;Assert.False(c.ValidFreight);Assert.Throws<ArgumentException>(()=>FreightDefaults.Route(c,new Random()));}
        [Fact] public void TooSmallCandidatePoolCannotStartAnImpossibleShift()
        {var c=FreightDefaults.Load();c.CandidateStores=4;Assert.False(c.ValidFreight);}
        [Fact] public void ContextClaimOnlySuppressesBuyingNearTheActiveDelivery()
        {
            var claim=new ContextReservation();var p=new JobPoint{Name="store",X=-54.7f,Y=-1749.089f,Z=29.421f};
            Assert.False(claim.Contains(p.X,p.Y,p.Z));claim.Set("supply",p,4.5f);
            Assert.True(claim.Contains(p.X,p.Y,p.Z));Assert.True(claim.Contains(p.X+4,p.Y,p.Z));Assert.False(claim.Contains(p.X+5,p.Y,p.Z));Assert.False(claim.Contains(p.X,p.Y,p.Z+5));
            claim.Clear("postop");Assert.True(claim.Contains(p.X,p.Y,p.Z));claim.Clear("supply");Assert.False(claim.Contains(p.X,p.Y,p.Z));
        }
        [Fact] public void MovingToNextStoreReleasesPreviousShopAndCopyProtectsCoordinates()
        {
            var claim=new ContextReservation();var first=new JobPoint{Name="first",X=10,Y=10,Z=30};claim.Set("supply",first,4.5f);first.X=999;
            Assert.True(claim.Contains(10,10,30));claim.Set("supply",new JobPoint{Name="second",X=100,Y=100,Z=30},4.5f);Assert.False(claim.Contains(10,10,30));Assert.True(claim.Contains(100,100,30));
        }
        [Theory] [InlineData(float.NaN)] [InlineData(-1f)] [InlineData(0f)] [InlineData(31f)]
        public void InvalidInteractionReservationCannotBlockShops(float radius)
        {var claim=new ContextReservation();Assert.Throws<ArgumentException>(()=>claim.Set("supply",new JobPoint{Name="a",X=10,Y=20,Z=30},radius));Assert.False(claim.Contains(10,20,30));}
        [Theory]
        [InlineData(113,113,122,true,79)] // F2 phone and old F2 garage -> O
        [InlineData(122,113,122,true,79)] // F11 settings and old F11 garage -> O
        [InlineData(119,113,122,true,119)] // Custom F8 kept
        [InlineData(113,113,122,false,113)] // Disabled phone does not reserve F2
        [InlineData(79,79,122,true,113)] // Custom phone O -> alternate F2
        [InlineData(113,113,79,true,96)] // Phone F2 + settings O -> NumPad0
        [InlineData(79,113,122,true,79)] // Non-conflicting garage O kept
        [InlineData(113,112,122,true,113)] // Explicit phone F1 override kept
        public void PhoneAndGarageKeyRoutingPreservesCustomValuesAndAvoidsConflicts(int garage,int phone,int settings,bool enabled,int expected)
        {Assert.Equal(expected,KeyBindingRules.Garage(garage,phone,settings,enabled));}
        [Fact] public void AllFiveJobsShareTheSameExclusiveShiftLease()
        {var g=new ShiftGate();foreach(var job in new[]{"pizza","postop","uber","valet","supply"}){Assert.True(g.Acquire(job));foreach(var other in new[]{"pizza","postop","uber","valet","supply"})Assert.False(g.Acquire(other));g.Release(job);}}
    }
}
