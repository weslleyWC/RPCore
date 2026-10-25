using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangCrewTests
    {
        [Fact] public void OwnNearbyMemberCanBeRecruitedWithoutAnyNewSaveOrFee()
        {Assert.True(GangCrewRules.Recruit("Mine","mine",0,3,true,false,false,true,true,true));Assert.Equal(3,GangCrewRules.Limit);}
        [Theory][InlineData(3,0)][InlineData(4,0)][InlineData(-1,0)][InlineData(0,3.01f)][InlineData(0,-1)][InlineData(0,float.NaN)]
        public void CapacityAndPhysicalDistanceCannotBeBypassed(int count,float distance)=>Assert.False(GangCrewRules.Recruit("Mine","Mine",count,distance,true,false,false,true,true,true));
        [Theory][InlineData(false,false,false,true,true,true)][InlineData(true,true,false,true,true,true)][InlineData(true,false,true,true,true,true)][InlineData(true,false,false,false,true,true)][InlineData(true,false,false,true,false,true)][InlineData(true,false,false,true,true,false)]
        public void RecruitmentRejectsForeignLeaderDuplicateDeadVehicleAndUnsafeContexts(bool owned,bool leader,bool already,bool alive,bool foot,bool calm)=>Assert.False(GangCrewRules.Recruit("Mine","Mine",0,1,owned,leader,already,alive,foot,calm));
        [Theory][InlineData(null,"Mine")][InlineData("","Mine")][InlineData(" ","Mine")][InlineData("Other","Mine")][InlineData("Mine",null)]
        public void AffiliationIsRequiredAndCannotRecruitRivalNPCs(string affiliation,string gang)=>Assert.False(GangCrewRules.Recruit(affiliation,gang,0,1,true,false,false,true,true,true));
        [Fact] public void FollowersRemainAliveFarFromHQBasedOnPlayerProximity()
        {Assert.False(GangPresenceRules.Keep(500,false,new GangPresenceOptions()));Assert.True(GangCrewRules.Keep("Mine","Mine",2,true));}
        [Theory][InlineData(180,true,true)][InlineData(180.1f,true,false)][InlineData(2,false,false)][InlineData(float.PositiveInfinity,true,false)][InlineData(-1,true,false)]
        public void CrewRetentionIsBoundedWithoutTeleportOrReplacement(float distance,bool alive,bool keep)=>Assert.Equal(keep,GangCrewRules.Keep("Mine","Mine",distance,alive));
        [Fact] public void LeaveOrSwitchGangEndsCrewEligibility()
        {Assert.False(GangCrewRules.Keep(null,"Mine",1,true));Assert.False(GangCrewRules.Keep("Other","Mine",1,true));Assert.True(GangCrewRules.Keep("MINE","mine",1,true));}
        [Theory][InlineData(true,true,false,true,0,25,true)][InlineData(false,true,false,true,0,1,false)][InlineData(true,false,false,true,0,1,false)][InlineData(true,true,true,true,0,1,false)][InlineData(true,true,false,false,0,1,false)][InlineData(true,true,false,true,.5f,1,false)][InlineData(true,true,false,true,0,25.01f,false)][InlineData(true,true,false,true,float.NaN,1,false)]
        public void BoardingRequiresPlayerDriverStoppedSuitableCarAndNoCompanyReservation(bool driver,bool car,bool job,bool alive,float speed,float distance,bool allowed)=>Assert.Equal(allowed,GangCrewRules.Board(driver,car,job,alive,speed,distance));
        [Fact] public void SeatSelectionCannotTakeDriverOccupiedReservedOrInvalidSeat()
        {Assert.Equal(2,GangCrewRules.Seat(new[]{-1,0,1,2,8},new[]{0,1}));Assert.Equal(-2,GangCrewRules.Seat(new[]{-1,8},null));Assert.Equal(-2,GangCrewRules.Seat(new[]{0,1},new[]{0,1}));Assert.Equal(-2,GangCrewRules.Seat(null,null));}
        [Fact] public void ThreeCrewMembersReserveDifferentPassengerSeatsBeforeActualBoarding()
        {var reserved=new List<int>();for(int i=0;i<3;i++){int seat=GangCrewRules.Seat(new[]{0,1,2},reserved);Assert.Equal(i,seat);reserved.Add(seat);}Assert.Equal(-2,GangCrewRules.Seat(new[]{0,1,2},reserved));Assert.Equal(3,reserved.Distinct().Count());}
        [Theory][InlineData(0,true)][InlineData(.49f,true)][InlineData(.5f,false)][InlineData(25,false)][InlineData(-1,false)][InlineData(float.NaN,false)]
        public void OrdersNeverRequireExitingAMovingVehicle(float speed,bool allowed)=>Assert.Equal(allowed,GangCrewRules.CanExit(speed));
        [Fact] public void LegacyGangStateAndUnknownFieldsDoNotBecomeSavedLiveCrewHandles()
        {var state=JsonConvert.DeserializeObject<GangState>("{\"Gang\":\"Mine\",\"Reputation\":42,\"PresenceEnabled\":true,\"Future\":7}");var before=JsonConvert.SerializeObject(state);Assert.True(GangCrewRules.Recruit(state.Gang,"Mine",0,2,true,false,false,true,true,true));Assert.Equal(before,JsonConvert.SerializeObject(state));Assert.Equal(42,state.Reputation);Assert.True(state.PresenceEnabled);Assert.Equal(7,(int)JObject.FromObject(state)["Future"]);}
    }
}
