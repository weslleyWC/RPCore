using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangLeaderTests
    {
        [Theory][InlineData("0",0)][InlineData("1",1)][InlineData("2",2)][InlineData("3",-1)][InlineData("-1",-1)][InlineData("\"Loyalty\"",0)][InlineData("\"Strength\"",1)][InlineData("\"Driving\"",2)][InlineData("\"driving\"",2)][InlineData("null",-1)][InlineData("true",-1)][InlineData("99999999999999999999999999999999",-1)]
        public void SourceInitiationFieldIsReadWithoutRewritingLegacyDefinition(string value,int expected){string json="{\"Name\":\"Mine\",\"InitiationType\":"+value+",\"Future\":7}";var g=JsonConvert.DeserializeObject<GangDefinition>(json);string before=JsonConvert.SerializeObject(g);Assert.Equal(expected,GangLeaderRules.Initiation(g));Assert.Equal(before,JsonConvert.SerializeObject(g));Assert.Equal(7,g.Extra["Future"].Value<int>());}
        [Fact] public void CreatedGangWithoutTypeUsesLoyaltyAndNullGangStaysInvalid(){Assert.Equal(0,GangLeaderRules.Initiation(new GangDefinition{Name="Created"}));Assert.Equal(-1,GangLeaderRules.Initiation(null));}
        [Theory][InlineData(1.5f,false,true)][InlineData(1.6f,false,false)][InlineData(4,true,true)][InlineData(4.1f,true,false)][InlineData(-1,false,false)]public void EntryAndCloseDistancesMatchSource(float distance,bool opened,bool expected)=>Assert.Equal(expected,GangLeaderRules.Near(distance,opened));
        [Fact] public void InvalidDistanceCannotOpenDialogue(){Assert.False(GangLeaderRules.Near(float.NaN,false));Assert.False(GangLeaderRules.Near(float.PositiveInfinity,true));}
        static GangState S()=>new GangState{Gang="Mine",Reputation=800,Rank=3,Turf=new GangTurfData{Owners={{99,"Mine"}}},Warfare=new GangWarData{RivalsEnabled=true}};
        [Fact] public void LeavingUsesExistingResetButKeepsTerritoryAndWarPreferences(){var s=S();Assert.True(GangLeaderRules.Leave(s,"Mine",n=>{},out var n));Assert.Null(n.Gang);Assert.Equal(0,n.Reputation);Assert.Equal(1,n.Rank);Assert.Equal("Mine",n.Turf.Owners[99]);Assert.True(n.Warfare.RivalsEnabled);Assert.Equal("Mine",s.Gang);Assert.Equal(800,s.Reputation);}
        [Fact] public void OtherGangOrUnaffiliatedCannotLeaveThroughThisLeader(){var s=S();Assert.False(GangLeaderRules.Leave(s,"Other",n=>{},out _));s.Gang=null;Assert.False(GangLeaderRules.CanLeave(s,"Mine"));}
        [Fact] public void PendingInitiationOrDriveByOrAlertBlocksLeaderLeave(){var s=S();s.Loyalty=new GangLoyaltyData{Pending=new GangLoyaltyAttempt()};Assert.False(GangLeaderRules.CanLeave(s,"Mine"));s=S();s.DriveBys=new GangDriveByData{Enabled=true,Started=1,Pending=new GangDriveByRun{Id="ambush",Gang="Mine",Rival="Enemy",Options=new GangDriveByOptions(),Models=new[]{"g_m_y_famca_01","g_m_y_famca_01","g_m_y_famca_01","g_m_y_famca_01"}}};Assert.False(GangLeaderRules.CanLeave(s,"Mine"));s=S();s.Warfare.Alert=new GangWarAlert{Id="warning",Gang="Mine",Attacker="Enemy",Area=GangTurfOptions.Defaults().Areas[0]};Assert.False(GangLeaderRules.CanLeave(s,"Mine"));}
        [Fact] public void LeaveSaveFailurePreservesMembershipAndUnknownFields(){var s=S();var j=JObject.FromObject(s);j["FutureRoot"]=42;s=j.ToObject<GangState>();string before=JsonConvert.SerializeObject(s);Assert.Throws<Exception>(()=>GangLeaderRules.Leave(s,"Mine",n=>throw new Exception(),out _));Assert.Equal(before,JsonConvert.SerializeObject(s));Assert.True(GangLeaderRules.Leave(s,"Mine",n=>{},out s));Assert.Equal(42,JObject.FromObject(s)["FutureRoot"].Value<int>());}
        [Fact] public void LeaveCannotBeRepeatedForAnotherReset(){var s=S();GangLeaderRules.Leave(s,"Mine",n=>{},out s);Assert.False(GangLeaderRules.Leave(s,"Mine",n=>throw new Exception(),out _));}
    }
}
