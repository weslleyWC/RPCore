using System;
using System.IO;
using Xunit;
namespace RPCore.Tests
{
 public sealed class CharacterStartupTests
 {
  static void Step(CharacterStartupStability s,int count=1,bool ready=true,int actor=1,int model=2,string profile="Franklin",float x=0){for(int i=0;i<count;i++)s.Observe(1000,ready,actor,model,profile,x,0,0);}
  [Fact] public void OneSecondIsNotEnoughDuringStoryStartup(){var s=new CharacterStartupStability();Step(s);Assert.False(s.Ready);Step(s,28);Assert.False(s.Ready);Step(s);Assert.True(s.Ready);}
  [Fact] public void ReportedRPCoreToFranklinAndHouseSequenceMustSettle(){var s=new CharacterStartupStability();Step(s,2,actor:10,model:20,profile:"RP_saved");Step(s,1,actor:11,model:21,x:1000);Assert.False(s.Ready);Step(s,26,actor:11,model:21,x:1000);Assert.False(s.Ready);Step(s,1,actor:11,model:21,x:1000);Assert.True(s.Ready);}
  [Fact] public void PlayerSwitchOrLoadingAfterStartupResetsTenSecondWindow(){var s=new CharacterStartupStability();Step(s,30);Assert.True(s.Ready);Step(s,1,false);Assert.False(s.Ready);Step(s,9);Assert.False(s.Ready);Step(s);Assert.True(s.Ready);}
  [Theory][InlineData(3,2,"Franklin")][InlineData(1,3,"Franklin")][InlineData(1,2,"Other")]
  public void ActorModelOrProfileChangeCannotReuseReadyWindow(int actor,int model,string profile){var s=new CharacterStartupStability();Step(s,30);Step(s,actor:actor,model:model,profile:profile);Assert.False(s.Ready);Step(s,9,actor:actor,model:model,profile:profile);Assert.False(s.Ready);Step(s,actor:actor,model:model,profile:profile);Assert.True(s.Ready);}
  [Fact] public void TeleportResetsStabilityWithoutMovingAnyActor(){var s=new CharacterStartupStability();Step(s,30);Step(s,x:1000);Assert.False(s.Ready);Step(s,10,x:1000);Assert.True(s.Ready);}
  [Fact] public void NormalMovementDoesNotRequireTenSecondsStandingStill(){var s=new CharacterStartupStability();for(int i=0;i<30;i++)Step(s,x:i);Assert.True(s.Ready);}
  [Theory][InlineData(-1)][InlineData(1001)][InlineData(int.MaxValue)] public void ClockJumpIsNotOfflineReadiness(int delta){var s=new CharacterStartupStability();Step(s,30);s.Observe(delta,true,1,2,"Franklin",0,0,0);Assert.False(s.Ready);Step(s,9);Assert.False(s.Ready);Step(s);Assert.True(s.Ready);}
  [Theory][InlineData(0,2,"Franklin")][InlineData(1,0,"Franklin")][InlineData(1,2,null)] public void MissingIdentityNeverSettles(int actor,int model,string profile){var s=new CharacterStartupStability();Step(s,60,actor:actor,model:model,profile:profile);Assert.False(s.Ready);}
  [Theory][InlineData(float.NaN)][InlineData(float.PositiveInfinity)] public void InvalidPositionNeverSettles(float x){var s=new CharacterStartupStability();Step(s,60,x:x);Assert.False(s.Ready);}
  [Fact] public void PausedClockDoesNotSatisfyStartupDelay(){var s=new CharacterStartupStability();for(int i=0;i<100;i++)s.Observe(0,true,1,2,"Franklin",0,0,0);Assert.False(s.Ready);}
  [Fact] public void EntryOnceGateWaitsForStabilityInsteadOfConsumingDuringLoad(){var s=new CharacterStartupStability();var entry=new CharacterEntryGate();for(int i=0;i<29;i++){Step(s);Assert.False(entry.Advance(1000,s.Ready));}Step(s);Assert.True(entry.Advance(1000,s.Ready));Assert.False(entry.Advance(1000,true));}
  [Theory][InlineData(6001,"begin")][InlineData(6005,"wait-actor")][InlineData(6009,"capture-commit")][InlineData(6020,"set-player-model")][InlineData(6022,"wallet-reload")][InlineData(6030,"create-ped")][InlineData(6031,"stage-ped")][InlineData(6032,"change-player-ped")][InlineData(6033,"handoff-returned")]
  public void PreviousSessionResolvesDetailedCharacterStages(int code,string name){string dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));try{using(var trace=new RuntimeTrace(dir,"v",new[]{"characters"})){trace.Mark(code);}using(var next=new RuntimeTrace(dir,"new",new[]{"characters"})){Assert.Equal("character:"+name,next.Previous.Stage);Assert.False(next.Previous.AbortedHookReached);}}finally{Directory.Delete(dir,true);}}
 }
}
