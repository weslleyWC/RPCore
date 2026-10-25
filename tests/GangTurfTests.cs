using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangTurfTests
    {
        static readonly Action<GangState> Save=s=>{};
        static GangTurfAttempt A()=>new GangTurfAttempt{Id="turf",Gang="Mine",PreviousOwner="3rd Avenue Hustlas",Area=GangTurfOptions.Defaults().Areas[0],Radius=45,SecondsLimit=600,Models=new[]{"g_m_y_famca_01","g_m_y_famca_01","g_m_y_famca_01"}};
        static GangState Begin(){Assert.True(GangTurfTransactions.Begin(new GangState{Gang="Mine",Reputation=42},A(),Save,out var s));return s;}
        static GangState Ready(){var s=Begin();for(int i=0;i<3;i++)Assert.True(GangTurfTransactions.Defeat(s,"turf",i,Save,out s));return s;}
        [Fact] public void FifteenTerritoriesHaveReferenceDefaultsPostsAndOwnerBands()
        {var c=GangTurfOptions.Defaults();Assert.True(c.Valid);Assert.Equal(15,c.Areas.Count);Assert.Equal(45,c.Radius);Assert.Equal(3,c.Defenders);Assert.All(c.Areas,a=>Assert.NotEmpty(a.Posts));Assert.All(c.Areas.Take(5),a=>Assert.Equal("3rd Avenue Hustlas",a.DefaultOwner));Assert.All(c.Areas.Skip(5).Take(5),a=>Assert.Equal("Eastside Kings",a.DefaultOwner));Assert.All(c.Areas.Skip(10),a=>Assert.Equal("Blacktop Locos",a.DefaultOwner));}
        [Fact] public void LegacyStateDoesNotAutoEnableMapOrWriteDefaultOwnership()
        {var s=JsonConvert.DeserializeObject<GangState>("{\"Gang\":\"Mine\",\"Reputation\":42,\"Future\":7}");Assert.False(s.TerritoriesEnabled);Assert.Null(s.Turf);Assert.Equal("3rd Avenue Hustlas",GangTurfRules.Owner(s.Turf,A().Area));Assert.Null(s.Turf);Assert.Equal(7,(int)JObject.FromObject(s)["Future"]);}
        [Fact] public void AllDefendersThenExplicitCaptureChangesOnlyOneOwner()
        {var s=Ready();Assert.True(s.Turf.Pending.Ready);Assert.Empty(s.Turf.Owners);Assert.True(GangTurfTransactions.Capture(s,"turf",Save,out s));Assert.Equal("Mine",s.Turf.Owners[1]);Assert.Equal(1,s.Turf.Captures);Assert.Null(s.Turf.Pending);Assert.Equal(92,s.Reputation);Assert.Equal("3rd Avenue Hustlas",GangTurfRules.Owner(s.Turf,GangTurfOptions.Defaults().Areas[1]));}
        [Fact] public void CaptureReplayAfterChangingGangDoesNotAssignOwnershipAgain()
        {var s=Ready();GangTurfTransactions.Capture(s,"turf",Save,out s);s.Gang="Other";Assert.True(GangTurfTransactions.Capture(s,"turf",x=>throw new Exception(),out s));Assert.Equal("Mine",s.Turf.Owners[1]);Assert.Equal(1,s.Turf.Captures);}
        [Fact] public void CannotCaptureWithoutAffiliationOrAttackOwnTerritory()
        {Assert.False(GangTurfTransactions.Begin(new GangState(),A(),Save,out _));var s=new GangState{Gang="Mine",Turf=new GangTurfData()};s.Turf.Owners[1]="Mine";Assert.False(GangTurfTransactions.Begin(s,A(),Save,out _));Assert.Empty(s.Turf.Receipts);}
        [Fact] public void CannotReplaceChangedOwnerOrOtherGangMembership()
        {var s=Ready();s.Turf.Owners[1]="ThirdParty";Assert.False(GangTurfTransactions.Capture(s,"turf",Save,out _));Assert.Equal("ThirdParty",s.Turf.Owners[1]);s=Ready();s.Gang="Other";Assert.False(GangTurfTransactions.Capture(s,"turf",Save,out _));Assert.Empty(s.Turf.Owners);}
        [Fact] public void DuplicateMissingOrWrongDefenderCannotGrantCapture()
        {var s=Begin();Assert.False(GangTurfTransactions.Defeat(s,"other",0,Save,out _));Assert.False(GangTurfTransactions.Defeat(s,"turf",-1,Save,out _));Assert.False(GangTurfTransactions.Defeat(s,"turf",3,Save,out _));GangTurfTransactions.Defeat(s,"turf",0,Save,out s);Assert.False(GangTurfTransactions.Defeat(s,"turf",0,Save,out _));Assert.False(GangTurfTransactions.Capture(s,"turf",Save,out _));Assert.Single(s.Turf.Pending.Defeated);}
        [Fact] public void PartialResumeRequiresNewPhysicalDefeatsAndKeepsAttempt()
        {var s=Begin();GangTurfTransactions.Defeat(s,"turf",0,Save,out s);s=CharacterRules.Copy(s);Assert.True(GangTurfTransactions.Restart(s,"turf",Save,out s));Assert.Empty(s.Turf.Pending.Defeated);Assert.Equal(1,s.Turf.Attempts);Assert.Equal("3rd Avenue Hustlas",s.Turf.Pending.PreviousOwner);}
        [Fact] public void ReadyProofSurvivesReloadWithoutRespawningDefenders()
        {var s=CharacterRules.Copy(Ready());Assert.False(GangTurfTransactions.Restart(s,"turf",Save,out _));Assert.True(s.Turf.Pending.Ready);Assert.True(GangTurfTransactions.Capture(s,"turf",Save,out s));}
        [Fact] public void FailedBeginSaveLeavesOriginalOwnershipAndAttemptUntouched()
        {var s=new GangState{Gang="Mine"};Assert.Throws<Exception>(()=>GangTurfTransactions.Begin(s,A(),x=>throw new Exception(),out _));Assert.Null(s.Turf);}
        [Fact] public void FailedFinalDefeatSaveDoesNotPublishReadyFlag()
        {var s=Begin();GangTurfTransactions.Defeat(s,"turf",0,Save,out s);GangTurfTransactions.Defeat(s,"turf",1,Save,out s);Assert.Throws<Exception>(()=>GangTurfTransactions.Defeat(s,"turf",2,x=>throw new Exception(),out _));Assert.False(s.Turf.Pending.Ready);Assert.Equal(2,s.Turf.Pending.Defeated.Count);}
        [Fact] public void FailedOwnershipSaveKeepsApprovalAndPreviousOwnerForRetry()
        {var s=Ready();Assert.Throws<Exception>(()=>GangTurfTransactions.Capture(s,"turf",x=>throw new Exception(),out _));Assert.Empty(s.Turf.Owners);Assert.True(s.Turf.Pending.Ready);Assert.Equal(0,s.Turf.Captures);}
        [Fact] public void CancelFailureAndOldReplayCannotClearNewAttemptOrOwners()
        {var s=Begin();s.Turf.Owners[99]="FutureGang";GangTurfTransactions.End(s,"turf",true,Save,out s);Assert.Equal("failed",s.Turf.LastResult);var a=A();a.Id="next";GangTurfTransactions.Begin(s,a,Save,out s);GangTurfTransactions.End(s,"turf",false,Save,out s);Assert.Equal("next",s.Turf.Pending.Id);Assert.Equal("FutureGang",s.Turf.Owners[99]);Assert.Equal(2,s.Turf.Attempts);}
        [Fact] public void OtherInitiationPendingBlocksAttack()
        {var s=new GangState{Gang="Mine",Loyalty=new GangLoyaltyData{Pending=new GangLoyaltyAttempt()}};Assert.False(GangTurfTransactions.Begin(s,A(),Save,out _));s.Loyalty=null;s.Strength=new GangStrengthData{Pending=new GangStrengthAttempt()};Assert.False(GangTurfTransactions.Begin(s,A(),Save,out _));s.Strength=null;s.Driving=new GangDrivingData{Pending=new GangDrivingAttempt()};Assert.False(GangTurfTransactions.Begin(s,A(),Save,out _));}
        [Fact] public void TerritoryPendingBlocksAllThreeNewInitiations()
        {var s=Begin();s.Gang=null;var l=new GangLoyaltyAttempt{Id="l",Gang="Mine",Leader=new GangSpot(),Contacts=GangLoyaltyOptions.Defaults().Contacts.Take(2).ToList()};Assert.False(GangLoyaltyTransactions.Begin(s,l,Save,out _));var f=new GangStrengthAttempt{Id="f",Gang="Mine",Center=new GangSpot(),Models=new[]{"a_m_y_business_01","a_m_y_business_01","a_m_y_business_01"},Options=new GangStrengthOptions()};Assert.False(GangStrengthTransactions.Begin(s,f,Save,out _));var d=new GangDrivingAttempt{Id="d",Gang="Mine",Home=new GangSpot(),Target=new GangSpot(),CrewModel="a_m_y_business_01",TargetModel="a_m_y_business_01",Options=GangDrivingOptions.Defaults()};Assert.False(GangDrivingTransactions.Begin(s,d,Save,out _));}
        [Fact] public void RulesAreaPostsAndModelsAreDeepSnapshots()
        {var a=A();GangTurfTransactions.Begin(new GangState{Gang="Mine"},a,Save,out var s);a.Area.Center.X=0;a.Area.Posts[0].X=0;a.Models[0]="changed";Assert.NotEqual(0,s.Turf.Pending.Area.Center.X);Assert.NotEqual(0,s.Turf.Pending.Area.Posts[0].X);Assert.Equal("g_m_y_famca_01",s.Turf.Pending.Models[0]);}
        [Fact] public void UnknownStateBookAreaPostAndOtherInitiationMetadataSurvive()
        {var j=JObject.FromObject(Begin());j["Future"]="root";j["Turf"]["Future"]="book";j["Turf"]["Pending"]["Area"]["Future"]="area";j["Turf"]["Pending"]["Area"]["Posts"][0]["Future"]="post";j["Loyalty"]=new JObject{{"Completed",2},{"Future","loyalty"}};var s=j.ToObject<GangState>();GangTurfTransactions.Defeat(s,"turf",0,Save,out s);j=JObject.FromObject(s);Assert.Equal("root",(string)j["Future"]);Assert.Equal("book",(string)j["Turf"]["Future"]);Assert.Equal("area",(string)j["Turf"]["Pending"]["Area"]["Future"]);Assert.Equal("post",(string)j["Turf"]["Pending"]["Area"]["Posts"][0]["Future"]);Assert.Equal("loyalty",(string)j["Loyalty"]["Future"]);}
        [Theory][InlineData(45,45,true)][InlineData(45.01f,45,false)][InlineData(-1,45,false)][InlineData(float.NaN,45,false)][InlineData(10,19,false)]
        public void AreaRequiresFiniteDistanceAndConfiguredRadius(float distance,int radius,bool inside)=>Assert.Equal(inside,GangTurfRules.Inside(distance,radius));
        [Fact] public void InvalidDefaultsProofsAndOverflowCannotModifyOwnership()
        {var c=GangTurfOptions.Defaults();c.Areas.Add(c.Areas[0]);Assert.False(c.Valid);var s=Begin();s.Turf.Pending.Defeated.AddRange(new[]{0,0});Assert.False(GangTurfTransactions.End(s,"turf",false,Save,out _));Assert.False(GangTurfTransactions.Begin(new GangState{Gang="Mine",Turf=new GangTurfData{Attempts=int.MaxValue}},A(),Save,out _));}
        [Fact] public void CapturedOwnershipIsPerProfileAndRemainsWithGangAfterLeave()
        {var s=Ready();GangTurfTransactions.Capture(s,"turf",Save,out s);s.Gang=null;var other=new GangState();Assert.Equal("Mine",GangTurfRules.Owner(s.Turf,A().Area));Assert.Equal("3rd Avenue Hustlas",GangTurfRules.Owner(other.Turf,A().Area));Assert.Null(other.Turf);}
    }
}
