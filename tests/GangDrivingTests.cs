using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangDrivingTests
    {
        static readonly Action<GangState> Save=s=>{};
        static GangDrivingAttempt A()=>new GangDrivingAttempt{Id="drive",Gang="Mine",CrewModel="g_m_y_famca_01",TargetModel="g_m_y_ballaeast_01",Home=new GangSpot{X=10,Y=20,Z=30},Target=GangDrivingOptions.Defaults().Targets[0],Options=GangDrivingOptions.Defaults()};
        static GangState Begin(){Assert.True(GangDrivingTransactions.Begin(new GangState(),A(),Save,out var s));return s;}
        static GangState Report(){var s=Begin();for(int i=0;i<4;i++)Assert.True(GangDrivingTransactions.Advance(s,"drive",(GangDrivingStage)i,Save,out s));return s;}
        [Fact] public void DefaultsHaveTwentyFiveReferenceTargetsAndCorrectThresholds()
        {var c=GangDrivingOptions.Defaults();Assert.True(c.Valid);Assert.Equal(25,c.Targets.Count);Assert.Equal(115,c.SpawnRadius);Assert.Equal(55,c.AttackRadius);Assert.Equal(150,c.EscapeRadius);Assert.Equal(160,c.TargetRadius);}
        [Fact] public void FullDrivingCycleRequiresAllStagesThenExplicitMembershipOnce()
        {var s=Report();Assert.Null(s.Gang);Assert.Equal(GangDrivingStage.Report,s.Driving.Pending.Stage);Assert.True(GangDrivingTransactions.Complete(s,"drive",Save,out s));Assert.Equal("Mine",s.Gang);Assert.Equal(1,s.Driving.Completed);Assert.True(GangDrivingTransactions.Complete(s,"drive",x=>throw new Exception(),out s));Assert.Equal(1,s.Driving.Completed);}
        [Fact] public void CannotSkipBoardingKillEscapeOrPhysicalReturn()
        {var s=Begin();Assert.False(GangDrivingTransactions.Advance(s,"drive",GangDrivingStage.Escape,Save,out _));Assert.False(GangDrivingTransactions.Complete(s,"drive",Save,out _));Assert.True(GangDrivingTransactions.Advance(s,"drive",GangDrivingStage.Boarding,Save,out s));Assert.False(GangDrivingTransactions.Advance(s,"drive",GangDrivingStage.Boarding,Save,out _));Assert.Equal(GangDrivingStage.Target,s.Driving.Pending.Stage);}
        [Fact] public void WrongIdAndInvalidStageCannotAdvanceOrComplete()
        {var s=Begin();Assert.False(GangDrivingTransactions.Advance(s,"other",GangDrivingStage.Boarding,Save,out _));Assert.False(GangDrivingTransactions.Advance(s,"drive",(GangDrivingStage)99,Save,out _));Assert.False(GangDrivingTransactions.Complete(s,"other",Save,out _));}
        [Theory][InlineData(1)][InlineData(2)][InlineData(3)]
        public void PartialResumeRestartsAllPhysicalProofWithSameAttemptAndRules(int stage)
        {var s=Begin();for(int i=0;i<stage;i++)GangDrivingTransactions.Advance(s,"drive",(GangDrivingStage)i,Save,out s);s=CharacterRules.Copy(s);Assert.True(GangDrivingTransactions.Restart(s,"drive",Save,out s));Assert.Equal(GangDrivingStage.Boarding,s.Driving.Pending.Stage);Assert.Equal(1,s.Driving.Attempts);Assert.Equal(150,s.Driving.Pending.Options.EscapeRadius);}
        [Fact] public void ApprovedReportSurvivesReloadAndCannotRestart()
        {var s=CharacterRules.Copy(Report());Assert.False(GangDrivingTransactions.Restart(s,"drive",Save,out _));Assert.Equal(GangDrivingStage.Report,s.Driving.Pending.Stage);}
        [Fact] public void FailedBeginSaveCannotCreateAttempt()
        {var s=new GangState();Assert.Throws<Exception>(()=>GangDrivingTransactions.Begin(s,A(),x=>throw new Exception(),out _));Assert.Null(s.Driving);}
        [Fact] public void FailedCheckpointSaveKeepsPreviousStage()
        {var s=Begin();Assert.Throws<Exception>(()=>GangDrivingTransactions.Advance(s,"drive",GangDrivingStage.Boarding,x=>throw new Exception(),out _));Assert.Equal(GangDrivingStage.Boarding,s.Driving.Pending.Stage);}
        [Fact] public void FailedMembershipSaveKeepsReportAndNoAffiliation()
        {var s=Report();Assert.Throws<Exception>(()=>GangDrivingTransactions.Complete(s,"drive",x=>throw new Exception(),out _));Assert.Null(s.Gang);Assert.Equal(GangDrivingStage.Report,s.Driving.Pending.Stage);Assert.Equal(0,s.Driving.Completed);}
        [Fact] public void ExistingAffiliationAndOtherInitiationPendingsAreProtected()
        {Assert.False(GangDrivingTransactions.Begin(new GangState{Gang="Other"},A(),Save,out _));var s=new GangState{Loyalty=new GangLoyaltyData{Pending=new GangLoyaltyAttempt()}};Assert.False(GangDrivingTransactions.Begin(s,A(),Save,out _));s=new GangState{Strength=new GangStrengthData{Pending=new GangStrengthAttempt()}};Assert.False(GangDrivingTransactions.Begin(s,A(),Save,out _));}
        [Fact] public void DrivingPendingBlocksStartingLoyaltyOrStrength()
        {var s=Begin();var l=new GangLoyaltyAttempt{Id="l",Gang="Mine",Leader=new GangSpot(),Contacts=GangLoyaltyOptions.Defaults().Contacts.Take(2).ToList()};Assert.False(GangLoyaltyTransactions.Begin(s,l,Save,out _));var f=new GangStrengthAttempt{Id="f",Gang="Mine",Center=new GangSpot(),Models=new[]{"a_m_y_business_01","a_m_y_business_01","a_m_y_business_01"},Options=new GangStrengthOptions()};Assert.False(GangStrengthTransactions.Begin(s,f,Save,out _));}
        [Fact] public void CancelFailureAndReplayDoNotTouchLaterAttemptOrExistingMetadata()
        {var s=Begin();s.Reputation=42;s.PresenceEnabled=true;GangDrivingTransactions.End(s,"drive",true,Save,out s);Assert.Equal("failed",s.Driving.LastResult);var a=A();a.Id="second";GangDrivingTransactions.Begin(s,a,Save,out s);GangDrivingTransactions.End(s,"drive",false,Save,out s);Assert.Equal("second",s.Driving.Pending.Id);Assert.Equal(42,s.Reputation);Assert.True(s.PresenceEnabled);}
        [Fact] public void CompleteReplayAfterLeaveCannotRejoin()
        {var s=Report();GangDrivingTransactions.Complete(s,"drive",Save,out s);s.Gang=null;GangDrivingTransactions.Complete(s,"drive",x=>throw new Exception(),out s);Assert.Null(s.Gang);}
        [Fact] public void TargetRulesAndCoordinatesAreDeepSnapshots()
        {var a=A();GangDrivingTransactions.Begin(new GangState(),a,Save,out var s);a.Target.X=0;a.Options.EscapeRadius=250;Assert.NotEqual(0,s.Driving.Pending.Target.X);Assert.Equal(150,s.Driving.Pending.Options.EscapeRadius);}
        [Fact] public void UnknownRootBookAttemptAndOptionsMetadataSurviveCheckpoint()
        {var j=JObject.FromObject(Begin());j["Future"]="root";j["Driving"]["Future"]="book";j["Driving"]["Pending"]["Future"]="pending";j["Driving"]["Pending"]["Options"]["Future"]="options";var s=j.ToObject<GangState>();GangDrivingTransactions.Advance(s,"drive",GangDrivingStage.Boarding,Save,out s);j=JObject.FromObject(s);Assert.Equal("root",(string)j["Future"]);Assert.Equal("book",(string)j["Driving"]["Future"]);Assert.Equal("pending",(string)j["Driving"]["Pending"]["Future"]);Assert.Equal("options",(string)j["Driving"]["Pending"]["Options"]["Future"]);}
        [Theory][InlineData(true,10,10,1,2,false,false,true)][InlineData(false,10,10,1,2,false,false,false)][InlineData(true,1,10,1,2,false,false,false)][InlineData(true,2,10,1,2,false,false,false)][InlineData(true,99,10,1,2,false,false,false)][InlineData(true,10,10,1,2,true,false,false)][InlineData(true,10,10,1,2,false,true,false)][InlineData(true,0,0,1,2,false,false,false)]
        public void OnlyActualCrewKillWithoutPlayerOrVehicleDamagePasses(bool dead,int killer,int crew,int player,int car,bool playerDamage,bool carDamage,bool pass)=>Assert.Equal(pass,GangDrivingRules.CrewKill(dead,killer,crew,player,car,playerDamage,carDamage));
        [Theory][InlineData(150,true,true,true)][InlineData(149.99f,true,true,false)][InlineData(151,false,true,false)][InlineData(151,true,false,false)][InlineData(float.NaN,true,true,false)]
        public void EscapeRequiresDistanceDriverAndSamePassenger(float distance,bool driver,bool passenger,bool pass)=>Assert.Equal(pass,GangDrivingRules.Escaped(distance,driver,passenger,GangDrivingOptions.Defaults()));
        [Theory][InlineData(15,20,0,true)][InlineData(15.1f,20,0,false)][InlineData(15,20.1f,0,false)][InlineData(15,20,.5f,false)][InlineData(float.NaN,1,0,false)]
        public void ReturnRequiresBothActorsAtHomeAndStoppedCar(float distance,float crew,float speed,bool pass)=>Assert.Equal(pass,GangDrivingRules.Returned(distance,crew,speed,true,true));
        [Fact] public void InvalidStageOptionsAndAttemptCounterAreProtected()
        {var s=Begin();s.Driving.Pending.Stage=(GangDrivingStage)99;Assert.False(GangDrivingTransactions.End(s,"drive",false,Save,out _));var a=A();a.Options.AttackRadius=200;Assert.False(a.Valid);Assert.False(GangDrivingTransactions.Begin(new GangState{Driving=new GangDrivingData{Attempts=int.MaxValue}},A(),Save,out _));}
        [Fact] public void LegacyAndSeparateProfilesDoNotAcquireDrivingStateAutomatically()
        {var old=JsonConvert.DeserializeObject<GangState>("{\"Gang\":\"Other\",\"Reputation\":42}");Assert.Null(old.Driving);var other=new GangState();var s=Report();GangDrivingTransactions.Complete(s,"drive",Save,out s);Assert.Null(other.Gang);Assert.Null(other.Driving);Assert.Equal("Other",old.Gang);Assert.Equal(42,old.Reputation);}
    }
}
