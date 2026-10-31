using System;
using Xunit;
namespace RPCore.Tests
{
    public class DealershipPreviewTests
    {
        static DealershipPreviewTicket Ticket(int time=0)=>new DealershipPreviewTicket(17,42,1234,"slot_a",time);
        [Theory]
        [InlineData(9999,false,true)] [InlineData(10000,false,false)] [InlineData(119999,true,true)] [InlineData(120000,true,false)] [InlineData(-1,true,false)]
        public void LoadingAndViewingHaveBoundedLifetimes(int now,bool displayed,bool expected)
        {Assert.Equal(expected,Ticket().Current(17,42,1234,"slot_a",now,displayed,0));}
        [Theory]
        [InlineData(17,42,1234,"slot_a",true)] [InlineData(18,42,1234,"slot_a",false)] [InlineData(17,43,1234,"slot_a",false)]
        [InlineData(17,42,1235,"slot_a",false)] [InlineData(17,42,0,"slot_a",false)] [InlineData(17,42,1234,"slot_b",false)] [InlineData(17,42,1234,null,false)]
        public void RecycledActorAndProfileChangesNeverKeepPreview(int actor,int model,long address,string profile,bool expected)
        {Assert.Equal(expected,Ticket().Current(actor,model,address,profile,1,false,0));}
        [Theory] [InlineData(0,true)] [InlineData(25,true)] [InlineData(25.01,false)] [InlineData(double.NaN,false)] [InlineData(double.PositiveInfinity,false)] [InlineData(-1,false)]
        public void MovementAndNonfiniteCoordinatesCancel(double movement,bool expected)
        {Assert.Equal(expected,Ticket().Current(17,42,1234,"slot_a",1,false,movement));}
        [Fact] public void InvalidOriginalIdentityNeverAuthorizesPreview()
        {Assert.False(new DealershipPreviewTicket(0,42,1234,"a",0).Current(0,42,1234,"a",1,false,0));Assert.False(new DealershipPreviewTicket(17,42,0,"a",0).Current(17,42,0,"a",1,false,0));Assert.False(new DealershipPreviewTicket(17,42,1234,null,0).Current(17,42,1234,null,1,false,0));}
        [Fact] public void GameClockWrapIsNotInstantExpiry()
        {Assert.True(Ticket(int.MaxValue-500).Current(17,42,1234,"slot_a",unchecked(int.MaxValue+499),false,0));}
        [Theory]
        [InlineData(2,4,1.5f,true)] [InlineData(3,7,3,true)] [InlineData(3.01f,4,2,false)] [InlineData(2,7.01f,2,false)]
        [InlineData(2,4,3.01f,false)] [InlineData(0,4,2,false)] [InlineData(2,-1,2,false)] [InlineData(2,4,float.NaN,false)] [InlineData(float.PositiveInfinity,4,2,false)]
        public void LargeOrInvalidModelsCannotSpawnInShowroom(float width,float length,float height,bool expected)
        {Assert.Equal(expected,DealershipPreviewTicket.Fits(width,length,height));}
        [Theory]
        [InlineData(0,0,1)] [InlineData(2.5f,0,1)] [InlineData(2.51f,0,0)] [InlineData(4,.5f,.5f)] [InlineData(7,-2,-1)] [InlineData(7,2,1)]
        public void PedestrianEntranceOpensOnlyOnApproach(float distance,float current,float expected)
        {Assert.Equal(expected,DealershipDoorRules.OpenRatio(new DealershipDoor(1,0,0,0),distance,0,0,current));}
        [Fact] public void InvalidDoorInputsDoNotForceOpening()
        {Assert.Equal(0,DealershipDoorRules.OpenRatio(null,0,0,0,float.NaN));Assert.Equal(.2f,DealershipDoorRules.OpenRatio(null,0,0,0,.2f));Assert.Equal(.2f,DealershipDoorRules.OpenRatio(new DealershipDoor(1,0,0,0),float.NaN,0,0,.2f));}
    }
}
