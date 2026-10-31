using System;
using System.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class PdmSpawnPointTests
    {
        [Fact] public void ReproduceLoggedAnonymousPointThenPassCorrectedPointThroughActualSearch()
        {
            var missingName=new JobPoint{X=-67.85416f,Y=-1073.517f,Z=27.17052f,Heading=70.04548f};
            Assert.False(missingName.Valid);Assert.Throws<ArgumentException>(()=>RigSpawnSearch.Candidates(missingName).ToArray());
            var point=PdmSpawnPoints.Tractor(-50,-1080,27.17052f,70.04548f);
            Assert.True(point.Valid);var candidates=RigSpawnSearch.Candidates(point).ToList();Assert.Equal(35,candidates.Count);
            var first=RigSpawnSearch.Find(point,_=>true);Assert.NotNull(first);Assert.True(first.Truck.Valid);Assert.True(first.Trailer.Valid);
            Assert.NotNull(new SpawnBox(first.Truck,-1.5,1.5,-6,6,-1,3));
        }
        [Theory] [InlineData(0,0,19)] [InlineData(90,-19,0)] [InlineData(180,0,-19)] [InlineData(270,19,0)]
        public void TractorKeepsOriginalNineteenMetreOffsetAndValidNames(float heading,float dx,float dy)
        {var p=PdmSpawnPoints.Tractor(100,200,30,heading);Assert.True(p.Valid);Assert.InRange(p.X,100+dx-.001,100+dx+.001);Assert.InRange(p.Y,200+dy-.001,200+dy+.001);Assert.Equal(30,p.Z);Assert.Equal(heading,p.Heading);Assert.Equal(35,RigSpawnSearch.Candidates(p).Count());}
        [Theory] [InlineData(0)] [InlineData(1)]
        public void BothPortOrdersPassTheSameSearchAndBoundsConsumerAsVehicles(int index)
        {
            var dock=PdmLogisticsDefaults.Load().Dock;var before=new[]{dock.X,dock.Y,dock.Z,dock.Heading};var p=PdmSpawnPoints.Cargo(dock,index);
            Assert.True(p.Valid);Assert.False(string.IsNullOrWhiteSpace(p.Name));Assert.Equal(dock.X,p.X);Assert.Equal(dock.Y+index*8,p.Y);Assert.Equal(dock.Z,p.Z);Assert.Equal(dock.Heading,p.Heading);
            var chosen=RigSpawnSearch.Find(p,_=>true);Assert.NotNull(chosen);Assert.NotNull(new SpawnBox(chosen.Trailer,-1,1,-2.5,2.5,-.5,1.5));Assert.Equal(before,new[]{dock.X,dock.Y,dock.Z,dock.Heading});
        }
        [Theory] [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)] [InlineData(float.NegativeInfinity)]
        public void RealInvalidCoordinatesRemainRejected(float value)=>Assert.Throws<ArgumentException>(()=>PdmSpawnPoints.Tractor(value,0,20,0));
        [Theory] [InlineData(-1)] [InlineData(2)]
        public void CargoSlotBoundsAreNotRelaxedToBypassTheError(int index)=>Assert.Throws<ArgumentException>(()=>PdmSpawnPoints.Cargo(PdmLogisticsDefaults.Load().Dock,index));
        [Fact] public void NullOrMalformedDockDoesNotCreateInvalidCandidates()
        {Assert.Throws<ArgumentException>(()=>PdmSpawnPoints.Cargo(null,0));Assert.Throws<ArgumentException>(()=>PdmSpawnPoints.Cargo(new JobPoint{X=1,Y=2,Z=3},0));}
    }
}
