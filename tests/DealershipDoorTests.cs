using System.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class DealershipDoorTests
    {
        [Fact] public void OnlyPedestrianShowroomDoorsAreEligibleNotGarageOrOtherStoreModels()
        {
            var doors=DealershipDoorRules.Doors();Assert.Equal(4,doors.Length);Assert.Equal(2,doors.Count(d=>d.Model==2059227086));Assert.Equal(2,doors.Count(d=>d.Model==1417577297));
            Assert.DoesNotContain(doors,d=>d.Model==1693207013);Assert.All(doors,d=>Assert.DoesNotContain(d.Model,ClothingDoorRules.Models()));
            Assert.All(doors,d=>Assert.True(DealershipDoorRules.ShowroomRange(d.X,d.Y,d.Z,35)));
        }
        [Theory][InlineData(0,0,true)][InlineData(5.9f,0,true)][InlineData(6.1f,0,false)][InlineData(0,3,true)][InlineData(0,3.1f,false)][InlineData(0,-3.1f,false)][InlineData(float.NaN,0,false)][InlineData(float.PositiveInfinity,0,false)]
        public void DoorNativeUpdatesStayWithinImmediateDoorAndFloor(float x,float z,bool expected)
        {var d=new DealershipDoor(2059227086,0,0,10);Assert.Equal(expected,DealershipDoorRules.Near(d,x,0,10+z));}
        [Fact] public void NullDoorIsNeverEligible()=>Assert.False(DealershipDoorRules.Near(null,0,0,0));
        [Theory][InlineData(0,0,35,true)][InlineData(34,0,35,true)][InlineData(36,0,35,false)][InlineData(69,0,70,true)][InlineData(71,0,70,false)][InlineData(0,9,70,false)][InlineData(0,-9,70,false)][InlineData(float.NaN,0,35,false)][InlineData(float.PositiveInfinity,0,35,false)][InlineData(0,0,-1,false)][InlineData(0,0,71,false)]
        public void ShowroomStreamingIsRestrictedToItsOwnBlockAndFloor(float offset,float z,float radius,bool expected)
        {Assert.Equal(expected,DealershipDoorRules.ShowroomRange(-48+offset,-1104,26.4f+z,radius));}
        [Fact] public void UnrelatedStoresNeverStreamShowroom(){Assert.False(DealershipDoorRules.ShowroomRange(418,-807,29,70));Assert.False(DealershipDoorRules.ShowroomRange(-716,-156,37,70));}
    }
}
