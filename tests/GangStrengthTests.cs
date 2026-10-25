using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangStrengthTests
    {
        static readonly Action<GangState> Save=s=>{};
        static GangStrengthAttempt A()=>new GangStrengthAttempt{Id="fight",Gang="Mine",Center=new GangSpot{X=10,Y=20,Z=30},Models=new[]{"g_m_y_famca_01","g_m_y_famca_01","g_m_y_famca_01"},Options=new GangStrengthOptions()};
        static GangState Begin(){Assert.True(GangStrengthTransactions.Begin(new GangState(),A(),Save,out var s));return s;}
        static GangState Report(){var s=Begin();for(int i=0;i<3;i++)Assert.True(GangStrengthTransactions.Defeat(s,"fight",i,Save,out s));return s;}
        [Fact] public void LegacySavesHaveNoMandatoryStrengthBookOrAutomaticMembership()
        {var s=JsonConvert.DeserializeObject<GangState>("{\"Gang\":null,\"Reputation\":42,\"Future\":7}");Assert.Null(s.Strength);Assert.Equal(42,s.Reputation);Assert.Null(s.Gang);Assert.Equal(7,(int)JObject.FromObject(s)["Future"]);}
        [Fact] public void ThreeDistinctDefeatsPersistReportThenExplicitMembershipOnlyOnce()
        {var s=Report();Assert.True(s.Strength.Pending.Report);Assert.Null(s.Gang);Assert.Equal(0,s.Strength.Completed);Assert.True(GangStrengthTransactions.Complete(s,"fight",Save,out s));Assert.Equal("Mine",s.Gang);Assert.Equal(1,s.Strength.Completed);Assert.Equal(1,s.Strength.Attempts);Assert.Null(s.Strength.Pending);Assert.True(GangStrengthTransactions.Complete(s,"fight",x=>throw new Exception(),out s));Assert.Equal(1,s.Strength.Completed);}
        [Fact] public void RepeatedDefeatOrWrongAttemptCannotCountSameFighterAgain()
        {var s=Begin();Assert.False(GangStrengthTransactions.Defeat(s,"other",0,Save,out _));Assert.True(GangStrengthTransactions.Defeat(s,"fight",1,Save,out s));Assert.False(GangStrengthTransactions.Defeat(s,"fight",1,Save,out _));Assert.Single(s.Strength.Pending.Defeated);Assert.False(GangStrengthTransactions.Complete(s,"fight",Save,out _));}
        [Theory][InlineData(-1)][InlineData(3)]
        public void InvalidFighterIndexIsRejected(int index)=>Assert.False(GangStrengthTransactions.Defeat(Begin(),"fight",index,Save,out _));
        [Fact] public void PartialFightResumeResetsAllPhysicalDefeatsWithoutNewAttemptOrFee()
        {var s=Begin();GangStrengthTransactions.Defeat(s,"fight",0,Save,out s);s=CharacterRules.Copy(s);Assert.True(GangStrengthTransactions.Restart(s,"fight",Save,out s));Assert.Empty(s.Strength.Pending.Defeated);Assert.False(s.Strength.Pending.Report);Assert.Equal(1,s.Strength.Attempts);Assert.Equal("fight",s.Strength.Pending.Id);}
        [Fact] public void FullyApprovedFightCannotRestartOrLoseApprovalOnReload()
        {var s=CharacterRules.Copy(Report());Assert.False(GangStrengthTransactions.Restart(s,"fight",Save,out _));Assert.Equal(3,s.Strength.Pending.Defeated.Count);Assert.True(s.Strength.Pending.Report);}
        [Fact] public void FailedBeginSaveLeavesOriginalStateUnchanged()
        {var s=new GangState();Assert.Throws<Exception>(()=>GangStrengthTransactions.Begin(s,A(),x=>throw new Exception(),out _));Assert.Null(s.Strength);}
        [Fact] public void FailedLastKnockoutSaveCannotPublishApproval()
        {var s=Begin();GangStrengthTransactions.Defeat(s,"fight",0,Save,out s);GangStrengthTransactions.Defeat(s,"fight",1,Save,out s);Assert.Throws<Exception>(()=>GangStrengthTransactions.Defeat(s,"fight",2,x=>throw new Exception(),out _));Assert.False(s.Strength.Pending.Report);Assert.Equal(2,s.Strength.Pending.Defeated.Count);}
        [Fact] public void FailedMembershipSaveKeepsApprovedReportForRetry()
        {var s=Report();Assert.Throws<Exception>(()=>GangStrengthTransactions.Complete(s,"fight",x=>throw new Exception(),out _));Assert.Null(s.Gang);Assert.True(s.Strength.Pending.Report);Assert.Equal(0,s.Strength.Completed);}
        [Fact] public void ExistingAffiliationCannotBeOverwritten()
        {Assert.False(GangStrengthTransactions.Begin(new GangState{Gang="Other"},A(),Save,out _));var s=Report();s.Gang="Other";Assert.False(GangStrengthTransactions.Complete(s,"fight",Save,out _));Assert.Equal("Other",s.Gang);}
        [Fact] public void LoyaltyAndStrengthPendingAttemptsAreMutuallyExclusive()
        {var a=new GangLoyaltyAttempt{Id="loyalty",Gang="Mine",Leader=new GangSpot(),Contacts=GangLoyaltyOptions.Defaults().Contacts.Take(2).ToList()};GangLoyaltyTransactions.Begin(new GangState(),a,Save,out var loyalty);Assert.False(GangStrengthTransactions.Begin(loyalty,A(),Save,out _));var strength=Begin();Assert.False(GangLoyaltyTransactions.Begin(strength,a,Save,out _));Assert.Equal(0,loyalty.Loyalty.Pending.Collected);Assert.Empty(strength.Strength.Pending.Defeated);}
        [Fact] public void FailureOrCancellationPreservesOtherGangDataAndDoesNotJoin()
        {var s=Begin();s.Reputation=42;s.PresenceEnabled=true;Assert.True(GangStrengthTransactions.End(s,"fight",true,Save,out s));Assert.Equal("failed",s.Strength.LastResult);Assert.Null(s.Gang);Assert.Equal(42,s.Reputation);Assert.True(s.PresenceEnabled);Assert.Null(s.Strength.Pending);}
        [Fact] public void CancelReplayCannotCancelLaterAttempt()
        {var s=Begin();GangStrengthTransactions.End(s,"fight",false,Save,out s);var a=A();a.Id="second";GangStrengthTransactions.Begin(s,a,Save,out s);Assert.True(GangStrengthTransactions.End(s,"fight",false,Save,out s));Assert.Equal("second",s.Strength.Pending.Id);Assert.Equal(2,s.Strength.Attempts);}
        [Fact] public void CompletionReplayAfterLeaveDoesNotRejoin()
        {var s=Report();GangStrengthTransactions.Complete(s,"fight",Save,out s);s.Gang=null;GangStrengthTransactions.Complete(s,"fight",x=>throw new Exception(),out s);Assert.Null(s.Gang);}
        [Fact] public void RulesModelsAndCoordinatesAreDeepSnapshotsWithoutMutatingSource()
        {var a=A();var old=new GangState();GangStrengthTransactions.Begin(old,a,Save,out var s);a.Options.FighterHealth=200;a.Center.X=0;a.Models[0]="changed";Assert.Equal(150,s.Strength.Pending.Options.FighterHealth);Assert.Equal(10,s.Strength.Pending.Center.X);Assert.Equal("g_m_y_famca_01",s.Strength.Pending.Models[0]);Assert.Null(old.Strength);}
        [Fact] public void UnknownStateBookAttemptOptionAndCenterMetadataSurviveCheckpoint()
        {var j=JObject.FromObject(Begin());j["Future"]="state";j["Strength"]["Future"]="book";j["Strength"]["Pending"]["Future"]="attempt";j["Strength"]["Pending"]["Options"]["Future"]="options";j["Strength"]["Pending"]["Center"]["Future"]="center";var s=j.ToObject<GangState>();GangStrengthTransactions.Defeat(s,"fight",0,Save,out s);j=JObject.FromObject(s);Assert.Equal("state",(string)j["Future"]);Assert.Equal("book",(string)j["Strength"]["Future"]);Assert.Equal("attempt",(string)j["Strength"]["Pending"]["Future"]);Assert.Equal("options",(string)j["Strength"]["Pending"]["Options"]["Future"]);Assert.Equal("center",(string)j["Strength"]["Pending"]["Center"]["Future"]);}
        [Theory][InlineData(101,35,true,true,true,true)][InlineData(100,0,true,true,true,false)][InlineData(200,35.01f,true,true,true,false)][InlineData(200,float.NaN,true,true,true,false)][InlineData(200,0,false,true,true,false)][InlineData(200,0,true,false,true,false)][InlineData(200,0,true,true,false,false)]
        public void FightRequiresPlayerHealthUnarmedFootSafetyAndArena(int health,float distance,bool unarmed,bool foot,bool safe,bool eligible)=>Assert.Equal(eligible,GangStrengthRules.PlayerCanFight(health,distance,unarmed,foot,safe,new GangStrengthOptions()));
        [Theory][InlineData(100,false,true,true,true)][InlineData(101,false,true,true,false)][InlineData(90,false,false,true,false)][InlineData(0,true,false,true,false)][InlineData(90,false,true,false,false)][InlineData(0,true,true,true,true)]
        public void KnockoutNeedsPlayerDamageAndFistsRatherThanThirdPartyDeath(int health,bool dead,bool player,bool unarmed,bool count)=>Assert.Equal(count,GangStrengthRules.Defeated(health,dead,player,unarmed,new GangStrengthOptions()));
        [Fact] public void InvalidProofListAndInvalidOptionsAreProtected()
        {var s=Begin();s.Strength.Pending.Defeated.AddRange(new[]{0,0});Assert.False(s.Strength.Valid);Assert.False(GangStrengthTransactions.End(s,"fight",false,Save,out _));var a=A();a.Options.FighterHealth=100;Assert.False(a.Valid);a=A();a.Report=true;Assert.False(a.Valid);}
        [Fact] public void NoCounterOverflowOrMissingModelCanStartAttempt()
        {var s=new GangState{Strength=new GangStrengthData{Attempts=int.MaxValue}};Assert.False(GangStrengthTransactions.Begin(s,A(),Save,out _));var a=A();a.Models[1]=null;Assert.False(a.Valid);}
        [Fact] public void ProfilesDoNotSharePhysicalWinsOrMembership()
        {var a=Report();var b=Begin();GangStrengthTransactions.Complete(a,"fight",Save,out a);Assert.Equal("Mine",a.Gang);Assert.Null(b.Gang);Assert.Empty(b.Strength.Pending.Defeated);}
    }
}
