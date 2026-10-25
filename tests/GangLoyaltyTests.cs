using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangLoyaltyTests
    {
        static readonly Action<GangState> Save=s=>{};
        static GangLoyaltyAttempt A()=>new GangLoyaltyAttempt{Id="attempt",Gang="Mine",Leader=new GangSpot{X=10,Y=20,Z=30},Contacts=GangLoyaltyOptions.Defaults().Contacts.Take(2).ToList()};
        static GangState Begin(){Assert.True(GangLoyaltyTransactions.Begin(new GangState(),A(),Save,out var s));return s;}
        static GangState Both(){var s=Begin();Assert.True(GangLoyaltyTransactions.Collect(s,"attempt",0,Save,out s));Assert.True(GangLoyaltyTransactions.Collect(s,"attempt",1,Save,out s));return s;}
        [Fact] public void DefaultContactsAreFifteenValidReferenceLocations()
        {var c=GangLoyaltyOptions.Defaults();Assert.True(c.Valid);Assert.Equal(15,c.Contacts.Count);Assert.Equal(-1543.608f,c.Contacts[0].X);Assert.Equal(61.04391f,c.Contacts[14].Heading);}
        [Fact] public void LegacyStateHasNoMandatoryLoyaltyRecordOrAutomaticAffiliation()
        {var s=JsonConvert.DeserializeObject<GangState>("{\"Gang\":null,\"Reputation\":17,\"PresenceEnabled\":true,\"Future\":7}");Assert.Null(s.Loyalty);Assert.Null(s.Gang);Assert.Equal(17,s.Reputation);Assert.Equal(7,(int)JObject.FromObject(s)["Future"]);}
        [Fact] public void TwoPackagesThenReturnAwardsOnlyAffiliationOnce()
        {var s=Both();Assert.Null(s.Gang);Assert.True(GangLoyaltyTransactions.Complete(s,"attempt",Save,out s));Assert.Equal("Mine",s.Gang);Assert.Equal(0,s.Reputation);Assert.Null(s.Loyalty.Pending);Assert.Equal(1,s.Loyalty.Completed);Assert.True(GangLoyaltyTransactions.Complete(s,"attempt",x=>throw new Exception(),out var replay));Assert.Same(s,replay);}
        [Fact] public void BeginSnapshotsCoordinatesAndDoesNotMutateCaller()
        {var initial=new GangState{Reputation=15,PresenceEnabled=true};var a=A();Assert.True(GangLoyaltyTransactions.Begin(initial,a,Save,out var s));a.Contacts[0].X=0;a.Leader.X=0;Assert.NotEqual(0,s.Loyalty.Pending.Contacts[0].X);Assert.Equal(10,s.Loyalty.Pending.Leader.X);Assert.Null(initial.Loyalty);Assert.Null(s.Gang);Assert.True(s.PresenceEnabled);Assert.Equal(15,s.Reputation);}
        [Theory][InlineData(null)][InlineData("")][InlineData("None")][InlineData("Unassigned")][InlineData("  ")]
        public void UnaffiliatedLegacyValuesCanStart(string affiliation)
        {Assert.True(GangLoyaltyTransactions.Begin(new GangState{Gang=affiliation},A(),Save,out _));}
        [Fact] public void ExistingGangCannotBeOverwrittenByInitiation()
        {var s=new GangState{Gang="Other",Reputation=42};Assert.False(GangLoyaltyTransactions.Begin(s,A(),Save,out _));Assert.Equal("Other",s.Gang);Assert.Equal(42,s.Reputation);}
        [Fact] public void WrongAttemptAndOutOfOrderPackagesCannotAdvance()
        {var s=Begin();Assert.False(GangLoyaltyTransactions.Collect(s,"other",0,Save,out _));Assert.False(GangLoyaltyTransactions.Collect(s,"attempt",1,Save,out _));Assert.False(GangLoyaltyTransactions.Collect(s,"attempt",-1,Save,out _));Assert.False(GangLoyaltyTransactions.Collect(s,"attempt",2,Save,out _));Assert.Equal(0,s.Loyalty.Pending.Collected);}
        [Fact] public void ReloadAndPackageReplayCannotCollectTwice()
        {var s=Begin();GangLoyaltyTransactions.Collect(s,"attempt",0,Save,out s);s=CharacterRules.Copy(s);Assert.True(GangLoyaltyTransactions.Collect(s,"attempt",0,x=>throw new Exception(),out var next));Assert.Same(s,next);Assert.Equal(1,next.Loyalty.Pending.Collected);}
        [Fact] public void CompletionCannotSkipEitherPackage()
        {var s=Begin();Assert.False(GangLoyaltyTransactions.Complete(s,"attempt",Save,out _));GangLoyaltyTransactions.Collect(s,"attempt",0,Save,out s);Assert.False(GangLoyaltyTransactions.Complete(s,"attempt",Save,out _));Assert.Null(s.Gang);}
        [Fact] public void FailedBeginSaveLeavesNoAttemptInOriginal()
        {var s=new GangState();Assert.Throws<Exception>(()=>GangLoyaltyTransactions.Begin(s,A(),x=>throw new Exception(),out _));Assert.Null(s.Loyalty);Assert.Null(s.Gang);}
        [Fact] public void FailedPickupSaveKeepsUncollectedPendingForRetry()
        {var s=Begin();Assert.Throws<Exception>(()=>GangLoyaltyTransactions.Collect(s,"attempt",0,x=>throw new Exception(),out _));Assert.Equal(0,s.Loyalty.Pending.Collected);Assert.DoesNotContain("attempt:pickup:0",s.Loyalty.Receipts);}
        [Fact] public void FailedMembershipSaveKeepsBothPackagesAndNoGang()
        {var s=Both();Assert.Throws<Exception>(()=>GangLoyaltyTransactions.Complete(s,"attempt",x=>throw new Exception(),out _));Assert.Null(s.Gang);Assert.Equal(2,s.Loyalty.Pending.Collected);Assert.Equal(0,s.Loyalty.Completed);Assert.True(GangLoyaltyTransactions.Complete(s,"attempt",Save,out s));Assert.Equal("Mine",s.Gang);}
        [Fact] public void CancelOnlyClearsAttemptAndOldCancelReplayCannotClearNewAttempt()
        {var s=Begin();s.Reputation=42;s.PresenceEnabled=true;Assert.True(GangLoyaltyTransactions.Cancel(s,"attempt",Save,out s));Assert.Null(s.Loyalty.Pending);Assert.Equal(42,s.Reputation);Assert.True(s.PresenceEnabled);var a=A();a.Id="second";GangLoyaltyTransactions.Begin(s,a,Save,out s);Assert.True(GangLoyaltyTransactions.Cancel(s,"attempt",Save,out s));Assert.Equal("second",s.Loyalty.Pending.Id);}
        [Fact] public void DuplicateBeginCannotReplacePartiallyCollectedAttempt()
        {var s=Begin();GangLoyaltyTransactions.Collect(s,"attempt",0,Save,out s);var a=A();a.Gang="Wrong";Assert.True(GangLoyaltyTransactions.Begin(s,a,x=>throw new Exception(),out s));Assert.Equal("Mine",s.Loyalty.Pending.Gang);Assert.Equal(1,s.Loyalty.Pending.Collected);a.Id="other";Assert.False(GangLoyaltyTransactions.Begin(s,a,Save,out _));}
        [Fact] public void CompletionReplayAfterLeavingDoesNotRejoinGang()
        {var s=Both();GangLoyaltyTransactions.Complete(s,"attempt",Save,out s);s.Gang=null;Assert.True(GangLoyaltyTransactions.Complete(s,"attempt",x=>throw new Exception(),out s));Assert.Null(s.Gang);Assert.Equal(1,s.Loyalty.Completed);}
        [Fact] public void AffiliationChangedExternallyBlocksPickupAndCompletion()
        {var s=Both();s.Gang="Other";Assert.False(GangLoyaltyTransactions.Complete(s,"attempt",Save,out _));var t=Begin();t.Gang="Other";Assert.False(GangLoyaltyTransactions.Collect(t,"attempt",0,Save,out _));}
        [Fact] public void UnknownStateAttemptCoordinatesAndBookFieldsSurviveCheckpoint()
        {var s=Begin();var j=JObject.FromObject(s);j["Future"]="state";j["Loyalty"]["Future"]="book";j["Loyalty"]["Pending"]["Future"]="pending";j["Loyalty"]["Pending"]["Contacts"][0]["Future"]="spot";s=j.ToObject<GangState>();GangLoyaltyTransactions.Collect(s,"attempt",0,Save,out s);j=JObject.FromObject(s);Assert.Equal("state",(string)j["Future"]);Assert.Equal("book",(string)j["Loyalty"]["Future"]);Assert.Equal("pending",(string)j["Loyalty"]["Pending"]["Future"]);Assert.Equal("spot",(string)j["Loyalty"]["Pending"]["Contacts"][0]["Future"]);}
        [Theory][InlineData(-1)][InlineData(3)]
        public void InvalidCollectedCountsAreProtected(int count)
        {var s=Begin();s.Loyalty.Pending.Collected=count;Assert.False(GangLoyaltyTransactions.Cancel(s,"attempt",Save,out _));Assert.False(GangLoyaltyTransactions.Collect(s,"attempt",0,Save,out _));}
        [Fact] public void InvalidCoordinatesRepeatedContactsAndMissingFieldsCannotStart()
        {var a=A();a.Contacts[1]=a.Contacts[0];Assert.False(GangLoyaltyTransactions.Begin(new GangState(),a,Save,out _));a=A();a.Leader.X=float.NaN;Assert.False(a.Valid);a=A();a.Id=null;Assert.False(a.Valid);}
        [Theory][InlineData(2.5f,2.5f,true,true)][InlineData(2.51f,3,true,false)][InlineData(1,2.49f,true,false)][InlineData(1,3,false,false)][InlineData(float.NaN,3,true,false)][InlineData(1,float.NaN,true,false)][InlineData(-1,3,true,false)]
        public void HandoverNeedsProximityTimeAndEligibleContext(float distance,float seconds,bool eligible,bool allowed)=>Assert.Equal(allowed,GangLoyaltyRules.Handover(distance,seconds,eligible));
        [Fact] public void CorruptReceiptListAndCompletedCounterAreProtected()
        {var s=Begin();s.Loyalty.Receipts=null;Assert.False(GangLoyaltyTransactions.Cancel(s,"attempt",Save,out _));s=Both();s.Loyalty.Completed=int.MaxValue;Assert.False(GangLoyaltyTransactions.Complete(s,"attempt",Save,out _));}
        [Fact] public void SeparateProfilesHaveIndependentAttemptsAndAffiliation()
        {var a=Both();var b=new GangState();GangLoyaltyTransactions.Complete(a,"attempt",Save,out a);Assert.Equal("Mine",a.Gang);Assert.Null(b.Gang);Assert.Null(b.Loyalty);}
    }
}
