using System.Linq;
using RPCore;
using Xunit;

namespace RPCoreTests
{
    public class ClothingDoorTests
    {
        [Fact] public void EveryClothingStoreHasItsPhysicalEntranceInsteadOfItsInteriorMenuPoint()
        {
            var shops=PersonalServiceRules.ClothingDefaults().Shops;var doors=ClothingDoorRules.Entrances();
            Assert.Equal(14,doors.Length);
            for(int i=0;i<doors.Length;i++)
            {
                Assert.True(doors[i].Valid);Assert.True(ClothingDoorRules.Active(doors[i],new[]{shops[i]}));
                Assert.NotEqual(shops[i].X,doors[i].X);
            }
            Assert.Equal(418.1723f,doors[0].X);Assert.Equal(-806.8669f,doors[0].Y);
        }
        [Fact] public void ModelWhitelistExcludesGunStoresBarbersDealershipsAndGarageDoors()
        {
            Assert.Equal(new[]{-1922281023,1780022985,868499217,-1148826190},ClothingDoorRules.Models());
            foreach(int model in new[]{-8873588,97297972,-1844444717,543652229,-1663512092,145369505,-550347177,270330101,2059227086,1417577297})
                Assert.DoesNotContain(model,ClothingDoorRules.Models());
        }
        [Theory][InlineData(0,0,true)][InlineData(5.9f,0,true)][InlineData(6.1f,0,false)]
        [InlineData(0,3.1f,false)][InlineData(0,-3.1f,false)][InlineData(0,float.NaN,false)]
        public void UnlockIsRestrictedToTheImmediateEntranceAndFloor(float x,float z,bool expected)
        {Assert.Equal(expected,ClothingDoorRules.Near(new JobPoint{Name="door",Z=10},x,0,10+z));}
        [Fact] public void RemovedOrMovedCustomStoreDoesNotUnlockItsPreviousEntrance()
        {
            var entrance=ClothingDoorRules.Entrances()[0];var shops=PersonalServiceRules.ClothingDefaults().Shops;
            shops.RemoveAt(0);Assert.False(ClothingDoorRules.Active(entrance,shops));
            Assert.False(ClothingDoorRules.Active(entrance,null));
            Assert.False(ClothingDoorRules.Active(entrance,new[]{new JobPoint{Name="other floor",X=entrance.X,Y=entrance.Y,Z=entrance.Z+10}}));
        }
        [Fact] public void InvalidCoordinatesAndUnknownPlacesNeverTriggerDoorUpdates()
        {
            Assert.False(ClothingDoorRules.Near(null,0,0,0));
            Assert.False(ClothingDoorRules.Near(new JobPoint{Name="door"},float.PositiveInfinity,0,0));
            Assert.False(ClothingDoorRules.Active(new JobPoint{Name="door",X=float.NaN},PersonalServiceRules.ClothingDefaults().Shops));
        }
    }
}
