using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangDefenseTests
    {
        static readonly Action<GangState> Save=s=>{};
        static GangTurfAttempt A()=>new GangTurfAttempt{Id="defense",Gang="Mine",PreviousOwner="Mine",Attacker="Rival",Mode=GangTurfMode.Defense,WaveSize=3,Area=GangTurfOptions.Defaults().Areas[0],Radius=45,SecondsLimit=600,Models=Enumerable.Repeat("g_m_y_famca_01",6).ToArray()};
        static GangState Begin(){var s=new GangState{Gang="Mine",Reputation=42,Turf=new GangTurfData()};s.Turf.Owners[1]="Mine";Assert.True(GangTurfTransactions.Begin(s,A(),Save,out s));return s;}
        static GangState Ready(){var s=Begin();for(int i=0;i<6;i++)Assert.True(GangTurfTransactions.Defeat(s,"defense",i,Save,out s));return s;}
        [Fact] public void OldTerritoryAttemptDeserializesAsAttackWithNoWaveRequirement()
        {var a=A();var j=JObject.FromObject(a);j.Remove("Mode");j.Remove("WaveSize");j.Remove("Attacker");var old=j.ToObject<GangTurfAttempt>();Assert.Equal(GangTurfMode.Attack,old.Mode);Assert.True(old.Valid);Assert.Equal(0,old.WaveSize);}
        [Fact] public void TwoWavesRequireAllSixPhysicalOpponentsBeforeDefenseReady()
        {var s=Begin();for(int i=0;i<3;i++)GangTurfTransactions.Defeat(s,"defense",i,Save,out s);Assert.False(s.Turf.Pending.Ready);Assert.False(GangTurfTransactions.Capture(s,"defense",Save,out _));Assert.True(GangDefenseRules.NextWave(3,3,6,3));for(int i=3;i<6;i++)GangTurfTransactions.Defeat(s,"defense",i,Save,out s);Assert.True(s.Turf.Pending.Ready);}
        [Fact] public void VictoryKeepsOwnerAndCountsDefenseWithoutCaptureOrReputation()
        {var s=Ready();Assert.True(GangTurfTransactions.Capture(s,"defense",Save,out s));Assert.Equal("Mine",s.Turf.Owners[1]);Assert.Equal(1,s.Turf.Defenses);Assert.Equal(0,s.Turf.Captures);Assert.Equal(0,s.Turf.LostDefenses);Assert.Equal(42,s.Reputation);Assert.Equal("defended",s.Turf.LastResult);Assert.Null(s.Turf.Pending);}
        [Fact] public void LostActiveDefenseTransfersOnlyExpectedAreaToAttackerOnce()
        {var s=Begin();s.Turf.Owners[99]="FutureGang";Assert.True(GangTurfTransactions.End(s,"defense",true,Save,out s));Assert.Equal("Rival",s.Turf.Owners[1]);Assert.Equal("FutureGang",s.Turf.Owners[99]);Assert.Equal(1,s.Turf.LostDefenses);Assert.Equal(0,s.Turf.Defenses);Assert.Equal("Mine",s.Gang);Assert.Equal(42,s.Reputation);Assert.True(GangTurfTransactions.End(s,"defense",true,x=>throw new Exception(),out s));Assert.Equal(1,s.Turf.LostDefenses);}
        [Fact] public void CancellationConservesOwnerAndDoesNotCountDefeat()
        {var s=Begin();Assert.True(GangTurfTransactions.End(s,"defense",false,Save,out s));Assert.Equal("Mine",s.Turf.Owners[1]);Assert.Equal(0,s.Turf.LostDefenses);Assert.Equal(0,s.Turf.Defenses);Assert.Null(s.Turf.Pending);}
        [Fact] public void ReadyDefenseCannotBeRecordedAsLossAfterAllAttackersDefeated()
        {var s=Ready();Assert.False(GangTurfTransactions.End(s,"defense",true,Save,out _));Assert.Equal("Mine",s.Turf.Owners[1]);Assert.True(s.Turf.Pending.Ready);}
        [Fact] public void FailedLossSaveLeavesOwnershipAndPendingUntouched()
        {var s=Begin();Assert.Throws<Exception>(()=>GangTurfTransactions.End(s,"defense",true,x=>throw new Exception(),out _));Assert.Equal("Mine",s.Turf.Owners[1]);Assert.Equal(0,s.Turf.LostDefenses);Assert.NotNull(s.Turf.Pending);}
        [Fact] public void FailedWinSaveLeavesReadyDefenseAndNoCounterChange()
        {var s=Ready();Assert.Throws<Exception>(()=>GangTurfTransactions.Capture(s,"defense",x=>throw new Exception(),out _));Assert.Equal(0,s.Turf.Defenses);Assert.True(s.Turf.Pending.Ready);Assert.Equal("Mine",s.Turf.Owners[1]);}
        [Fact] public void ExternallyChangedOwnerOrAffiliationCannotBeOverwrittenByLoss()
        {var s=Begin();s.Turf.Owners[1]="External";Assert.False(GangTurfTransactions.End(s,"defense",true,Save,out _));Assert.Equal("External",s.Turf.Owners[1]);s=Begin();s.Gang="Other";Assert.False(GangTurfTransactions.End(s,"defense",true,Save,out _));Assert.Equal("Mine",s.Turf.Owners[1]);}
        [Fact] public void CanOnlyDefendOwnAreaAgainstAnotherGang()
        {var s=new GangState{Gang="Mine"};Assert.False(GangTurfTransactions.Begin(s,A(),Save,out _));var a=A();a.Attacker="Mine";Assert.False(a.Valid);a=A();a.PreviousOwner="Other";s.Turf=new GangTurfData();s.Turf.Owners[1]="Other";Assert.False(GangTurfTransactions.Begin(s,a,Save,out _));}
        [Fact] public void PartialResumeResetsBothWavesButNotAttemptOrAttacker()
        {var s=Begin();for(int i=0;i<3;i++)GangTurfTransactions.Defeat(s,"defense",i,Save,out s);s=CharacterRules.Copy(s);Assert.True(GangTurfTransactions.Restart(s,"defense",Save,out s));Assert.Empty(s.Turf.Pending.Defeated);Assert.Equal(6,s.Turf.Pending.Models.Length);Assert.Equal("Rival",s.Turf.Pending.Attacker);Assert.Equal(1,s.Turf.Attempts);}
        [Fact] public void ReadyApprovalAndModePersistAcrossReload()
        {var s=CharacterRules.Copy(Ready());Assert.Equal(GangTurfMode.Defense,s.Turf.Pending.Mode);Assert.False(GangTurfTransactions.Restart(s,"defense",Save,out _));Assert.True(GangTurfTransactions.Capture(s,"defense",Save,out s));Assert.Equal(1,s.Turf.Defenses);}
        [Fact] public void LostAreaCanBeRecapturedUsingOldAttackFlow()
        {var s=Begin();GangTurfTransactions.End(s,"defense",true,Save,out s);var a=A();a.Id="recapture";a.Mode=GangTurfMode.Attack;a.PreviousOwner="Rival";a.Models=a.Models.Take(3).ToArray();GangTurfTransactions.Begin(s,a,Save,out s);for(int i=0;i<3;i++)GangTurfTransactions.Defeat(s,"recapture",i,Save,out s);Assert.True(GangTurfTransactions.Capture(s,"recapture",Save,out s));Assert.Equal("Mine",s.Turf.Owners[1]);Assert.Equal(1,s.Turf.Captures);Assert.Equal(1,s.Turf.LostDefenses);}
        [Fact] public void UnknownNewAndOldMetadataSurvivesDefenseResult()
        {var j=JObject.FromObject(Ready());j["Future"]="root";j["Turf"]["Future"]="book";j["Turf"]["Pending"]["Future"]="pending";var s=j.ToObject<GangState>();GangTurfTransactions.Capture(s,"defense",Save,out s);j=JObject.FromObject(s);Assert.Equal("root",(string)j["Future"]);Assert.Equal("book",(string)j["Turf"]["Future"]);}
        [Theory][InlineData(3,3,6,3,true)][InlineData(3,2,6,3,false)][InlineData(6,6,6,3,false)][InlineData(2,2,4,2,true)][InlineData(1,1,2,1,true)][InlineData(0,0,6,3,false)]
        public void NewWaveOnlyStartsAfterAllCurrentlySpawnedAttackersAreDefeated(int spawned,int defeated,int total,int size,bool next)=>Assert.Equal(next,GangDefenseRules.NextWave(spawned,defeated,total,size));
        [Theory][InlineData(2,3,true)][InlineData(1,1,true)][InlineData(0,3,false)][InlineData(3,3,false)][InlineData(2,4,false)]
        public void DefenseConfigLimitsWavesAndActors(int waves,int size,bool valid)=>Assert.Equal(valid,new GangDefenseOptions{Waves=waves,PerWave=size}.Valid);
        [Fact] public void InvalidModesWaveSizesAndCountersAreProtected()
        {var a=A();a.Mode=(GangTurfMode)99;Assert.False(a.Valid);a=A();a.WaveSize=0;Assert.False(a.Valid);a=A();a.WaveSize=1;Assert.False(a.Valid);var s=Begin();s.Turf.Defenses=int.MaxValue;Assert.False(GangTurfTransactions.Capture(s,"defense",Save,out _));}
        [Fact] public void DefenseResultAndOwnershipAreIsolatedByProfile()
        {var a=Begin();var b=Begin();GangTurfTransactions.End(a,"defense",true,Save,out a);Assert.Equal("Rival",a.Turf.Owners[1]);Assert.Equal("Mine",b.Turf.Owners[1]);Assert.Equal(0,b.Turf.LostDefenses);}
    }
}
