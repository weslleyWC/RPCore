using System;
using System.Linq;
using Xunit;

namespace RPCore.Tests
{
    public class RigSpawnTests
    {
        static JobPoint P(float x=0,float y=0,float z=6,float h=0)=>new JobPoint{Name="test",X=x,Y=y,Z=z,Heading=h};
        static SpawnBox Box(JobPoint p)=>new SpawnBox(p,-1.5,1.5,-7,7,-1,3);
        [Fact] public void AdjacentParkedVehicleInsideOldTwelveMeterCircleDoesNotBlockBay()
        {Assert.False(Box(P()).Overlaps(new SpawnBox(P(6,0),-1,1,-2,2,-1,1)));}
        [Theory] [InlineData(0,0,0)] [InlineData(1,0,0)] [InlineData(0,5,0)] [InlineData(0,0,90)]
        public void OverlappingVehicleStillBlocksSpawn(float x,float y,float heading)
        {Assert.True(Box(P()).Overlaps(Box(P(x,y,6,heading))));}
        [Fact] public void RotatedBayUsesBothOrientedAxes()
        {Assert.False(Box(P(0,0,6,90)).Overlaps(Box(P(0,6,6,90))));Assert.True(Box(P(0,0,6,90)).Overlaps(Box(P(6,0,6,90))));}
        [Fact] public void DifferentHeightsDoNotBlockAndActualFootprintPlayerDoes()
        {Assert.False(Box(P()).Overlaps(Box(P(0,0,20))));Assert.True(Box(P()).Overlaps(new SpawnBox(P(),-.5,.5,-.5,.5,-1,1)));}
        [Fact] public void AsymmetricModelBoundsAreRotatedAroundModelOrigin()
        {var b=new SpawnBox(P(10,20,6,90),-1,3,-6,2,-1,3);Assert.Equal(12,b.X,5);Assert.Equal(21,b.Y,5);Assert.Equal(7,b.Z);}
        [Fact] public void SearchStartsAtConfiguredAnchorWithoutMutatingUserConfig()
        {var p=P(524,-2140,6,170);var slot=RigSpawnSearch.Find(p,_=>true);Assert.Equal(p.X,slot.Trailer.X);Assert.Equal(p.Y,slot.Trailer.Y);Assert.NotSame(p,slot.Trailer);Assert.Equal(6,p.Z);}
        [Fact] public void BlockedOriginalBayUsesNextAvailablePair()
        {int calls=0;var p=P();var slot=RigSpawnSearch.Find(p,s=>++calls==3);Assert.Equal(3,calls);Assert.NotNull(slot);Assert.True(slot.Trailer.X!=p.X || slot.Trailer.Y!=p.Y);}
        [Fact] public void AllBlockedBaysFailAfterBoundedSearch()
        {int calls=0;Assert.Null(RigSpawnSearch.Find(P(),_=>{calls++;return false;}));Assert.Equal(35,calls);}
        [Fact] public void SearchRequiresCompletePairAndRetainsNineteenMeterSeparation()
        {foreach(var s in RigSpawnSearch.Candidates(P(500,-2100,6,170))){Assert.InRange(DeliveryRoute.Distance(s.Truck,s.Trailer),18.999,19.001);Assert.Equal(s.Truck.Heading,s.Trailer.Heading);}}
        [Fact] public void CandidatesAreUniqueNearbyAndNearestFirst()
        {var p=P();var slots=RigSpawnSearch.Candidates(p).ToList();Assert.Equal(35,slots.Select(s=>new{s.Trailer.X,s.Trailer.Y}).Distinct().Count());double last=-1;foreach(var s in slots){double d=DeliveryRoute.Distance(p,s.Trailer);Assert.InRange(d,last,40.001);last=d;}}
        [Fact] public void NearbyNonOverlappingPedDoesNotInvalidateWholeTwelveMeterArea()
        {Assert.False(Box(P()).Overlaps(new SpawnBox(P(5,0),-.5,.5,-.5,.5,-1,1)));}
    }
}
