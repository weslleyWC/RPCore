using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangBmxTests
    {
        static readonly Action Save=()=>{};static readonly DateTime Utc=new DateTime(2026,10,26,12,0,0,DateTimeKind.Utc);
        static GangTurfDefinition A()=>GangTurfOptions.Defaults().Areas.Single(a=>a.Id==12);static GangState S()=>new GangState{Gang=A().DefaultOwner};static WalletData W()=>new WalletData{Bank=10000,Cash=123};
        static bool Begin(WalletData w,string id="bmx",GangBmxOptions c=null,Action save=null)=>GangBmxTransactions.Begin(w,S(),A(),c??new GangBmxOptions(),id,save??Save,out _);
        static bool Pass(WalletData w,int i=0,Action save=null)=>GangBmxTransactions.Pass(w,S(),A(),"bmx",i,true,true,w.GangBmx.Pending.Options.Checkpoints[i],save??Save,out _);
        static bool Update(WalletData w,int elapsed=10000,bool faulted=false,Action save=null)=>GangBmxTransactions.Update(w,S(),A(),"bmx",new GangBmxProgress{Elapsed=elapsed,Faulted=faulted},save??Save,out _);
        static bool Complete(WalletData w,DateTime? utc=null,Action save=null)=>GangBmxTransactions.Complete(w,S(),A(),"bmx",utc??Utc,save??Save,out _);
        static bool Cancel(WalletData w,Action save=null)=>GangBmxTransactions.Cancel(w,"bmx",save??Save,out _);
        static WalletData Started(){var w=W();Assert.True(Begin(w));return w;}
        static WalletData Ready(){var w=Started();Update(w);for(int i=0;i<11;i++)Assert.True(Pass(w,i));return w;}
        static GangBmxAccount Reward(WalletData w)=>GangBmxTransactions.Account(w.GangBmx,S().Gang);
        [Fact] public void DefaultsMatchSourceBicycleSpawnElevenCheckpointsRadiusAndDeadline()
        {var c=new GangBmxOptions();Assert.True(c.Valid);Assert.Equal("bmx",c.VehicleModel);Assert.Equal(11,c.Checkpoints.Count);Assert.Equal(-1375.902f,c.Spawn.X);Assert.Equal(-1407.651f,c.Spawn.Y);Assert.Equal(5.033009f,c.Spawn.Z);Assert.Equal(352.8152f,c.Spawn.Heading);Assert.Equal(-1375.255f,c.Checkpoints[0].X);Assert.Equal(5.6437f,c.Checkpoints[10].Z);Assert.Equal(2.5f,c.Radius);Assert.Equal(3,c.Height);Assert.Equal(75,c.SecondsLimit);Assert.Equal(60,c.BoostMinutes);}
        [Fact] public void BeginIsFreeWithoutCreatingIncomeStockOrAutomaticOptIn()
        {var w=W();w.Bank=0;Assert.True(Begin(w));Assert.Equal(0,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);Assert.Null(w.GangAutoLaunder);Assert.Empty(w.GangBmx.Accounts);}
        [Fact] public void RequiresOwnedTerritoryTwelve()
        {var w=W();var a=GangTurfOptions.Defaults().Areas[0];Assert.False(GangBmxTransactions.Begin(w,new GangState{Gang=a.DefaultOwner},a,new GangBmxOptions(),"bad",Save,out _));Assert.False(GangBmxTransactions.Begin(w,new GangState{Gang="Other"},A(),new GangBmxOptions(),"bad",Save,out _));Assert.Null(w.GangBmx);}
        [Fact] public void BeginSnapshotsEveryPointAndOptions()
        {var w=W();var c=new GangBmxOptions();Begin(w,c:c);c.Spawn.X=0;c.Checkpoints[0].Y=0;c.Checkpoints.RemoveAt(10);c.SecondsLimit=300;Assert.Equal(-1375.902f,w.GangBmx.Pending.Options.Spawn.X);Assert.Equal(-1405.455f,w.GangBmx.Pending.Options.Checkpoints[0].Y);Assert.Equal(11,w.GangBmx.Pending.Options.Checkpoints.Count);Assert.Equal(75,w.GangBmx.Pending.Options.SecondsLimit);}
        [Fact] public void CheckpointsMustBePassedInOrderWithoutSkipping()
        {var w=Started();Assert.False(Pass(w,1));Assert.True(Pass(w));Assert.False(Pass(w,2));Assert.Equal(1,w.GangBmx.Pending.Cleared);Assert.False(Complete(w));Assert.Empty(w.GangBmx.Accounts);}
        [Fact] public void ReplayedCheckpointDoesNotIncrementOrSaveAgain()
        {var w=Started();Pass(w);int saves=0;Assert.True(Pass(w,0,()=>saves++));Assert.Equal(0,saves);Assert.Equal(1,w.GangBmx.Pending.Cleared);}
        [Fact] public void EveryCheckpointRequiredAndFinalConfirmationIsSeparate()
        {var w=Started();for(int i=0;i<10;i++)Pass(w,i);Assert.False(Complete(w));Pass(w,10);Assert.True(w.GangBmx.Pending.Ready);Assert.Empty(w.GangBmx.Accounts);Assert.Null(w.GangAutoLaunder);}
        [Fact] public void LastCheckpointStillCountsJustBeforeDeadline()
        {var w=Started();for(int i=0;i<10;i++)Pass(w,i);Assert.True(Update(w,74999));Assert.True(Pass(w,10));Assert.True(w.GangBmx.Pending.Ready);Assert.False(Update(w,75000));Assert.True(Complete(w));}
        [Fact] public void ExactDeadlineRejectsCheckpointAndPersistsFailureAcrossReload()
        {var w=Started();Update(w,75000);Assert.True(w.GangBmx.Pending.Failed);Assert.True(w.GangBmx.Valid);Assert.False(Pass(w));w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(w.GangBmx.Pending.Failed);Assert.False(Update(w,0));Assert.False(Complete(w));Assert.True(Cancel(w));}
        [Fact] public void AtomicAwardRemovesPendingWithoutFinancialMutation()
        {var w=Ready();int saves=0;Assert.True(Complete(w,save:()=>{saves++;Assert.Null(w.GangBmx.Pending);Assert.Equal(1,Reward(w).Completed);}));Assert.Equal(1,saves);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);Assert.Null(w.GangAutoLaunder);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void CompletionReplayNeverCountsTwiceOrExtendsWindow()
        {var w=Ready();Complete(w);var old=w.GangBmx;int saves=0;Assert.True(Complete(w,Utc.AddMinutes(5),()=>saves++));Assert.Same(old,w.GangBmx);Assert.Equal(0,saves);Assert.Equal(1,Reward(w).Completed);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void PartialReloadPreservesClearedCountRemainingTimeAndSnapshot()
        {var w=Started();Pass(w);Pass(w,1);Pass(w,2);Update(w,20000);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(w.GangBmx.Valid);Assert.Equal(3,w.GangBmx.Pending.Cleared);Assert.Equal(20000,w.GangBmx.Pending.Progress.Elapsed);Assert.Equal(11,w.GangBmx.Pending.Options.Checkpoints.Count);Assert.True(Pass(w,3));Assert.False(Complete(w));}
        [Fact] public void ReadyReloadCanAwardWithoutVehicleOrRepeatedCourse()
        {var w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(Ready()));Assert.True(w.GangBmx.Pending.Ready);Assert.True(Complete(w));Assert.Null(w.GangBmx.Pending);}
        [Fact] public void FaultedContractCannotBeResumedByClearingFlagOrTime()
        {var w=Started();Update(w,20000,true);Assert.True(w.GangBmx.Pending.Failed);Assert.False(Update(w,20001));Assert.False(Update(w,0,true));Assert.False(Pass(w));Assert.True(Cancel(w));}
        [Fact] public void FailedBeginRestoresAbsentBook()
        {var w=W();Assert.Throws<InvalidOperationException>(()=>Begin(w,save:()=>throw new InvalidOperationException()));Assert.Null(w.GangBmx);}
        [Fact] public void FailedCheckpointRestoresPreviousIndexAndReceiptForRetry()
        {var w=Started();var old=w.GangBmx;Assert.Throws<InvalidOperationException>(()=>Pass(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangBmx);Assert.Equal(0,w.GangBmx.Pending.Cleared);Assert.DoesNotContain("bmx:bmx-pass:0",w.GangBmx.Receipts);Assert.True(Pass(w));}
        [Fact] public void FailedSampleRestoresElapsedAndExactBook()
        {var w=Started();var old=w.GangBmx;Assert.Throws<InvalidOperationException>(()=>Update(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangBmx);Assert.Equal(0,w.GangBmx.Pending.Progress.Elapsed);}
        [Fact] public void FailedAwardPreservesReadyWithoutGrantingBonus()
        {var w=Ready();var old=w.GangBmx;Assert.Throws<InvalidOperationException>(()=>Complete(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangBmx);Assert.True(w.GangBmx.Pending.Ready);Assert.Empty(w.GangBmx.Accounts);}
        [Fact] public void CancelIsFreeAndFailureRestoresPendingProgress()
        {var w=Started();Pass(w);var old=w.GangBmx;Assert.Throws<InvalidOperationException>(()=>Cancel(w,()=>throw new InvalidOperationException()));Assert.Same(old,w.GangBmx);Assert.True(Cancel(w));Assert.Null(w.GangBmx.Pending);Assert.Equal(10000,w.Bank);Assert.Empty(w.GangBmx.Accounts);Assert.True(Cancel(w));}
        [Fact] public void CancellingNewContractPreservesEarlierBonus()
        {var w=Ready();Complete(w);long expiry=Reward(w).ExpiresUtcTicks;Begin(w,"again");Assert.True(GangBmxTransactions.Cancel(w,"again",Save,out _));Assert.Equal(1,Reward(w).Completed);Assert.Equal(expiry,Reward(w).ExpiresUtcTicks);}
        [Fact] public void NewRealCompletionRefreshesWithoutStackingDuration()
        {var w=Ready();Complete(w);Begin(w,"again");for(int i=0;i<11;i++)GangBmxTransactions.Pass(w,S(),A(),"again",i,true,true,w.GangBmx.Pending.Options.Checkpoints[i],Save,out _);Assert.True(GangBmxTransactions.Complete(w,S(),A(),"again",Utc.AddMinutes(10),Save,out _));Assert.Equal(2,Reward(w).Completed);Assert.Equal(Utc.AddMinutes(70).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void BonusIsBoundToUtcGangAreaAndStrictExpiry()
        {var w=Ready();Complete(w);var b=w.GangBmx;Assert.Equal(2,GangBmxTransactions.Multiplier(b,S().Gang.ToUpperInvariant(),12,Utc));Assert.Equal(1,GangBmxTransactions.Multiplier(b,S().Gang,12,Utc.AddHours(1)));Assert.Equal(1,GangBmxTransactions.Multiplier(b,S().Gang,12,Utc.AddTicks(-1)));Assert.Equal(1,GangBmxTransactions.Multiplier(b,"Other",12,Utc));Assert.Equal(1,GangBmxTransactions.Multiplier(b,S().Gang,11,Utc));Assert.Equal(1,GangBmxTransactions.Multiplier(b,S().Gang,12,DateTime.SpecifyKind(Utc,DateTimeKind.Local)));}
        [Fact] public void LegacyQueriesDoNotCreateBookOrBonus()
        {var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":100,\"Future\":42}");Assert.Null(w.GangBmx);Assert.False(GangBmxTransactions.Blocked(w));Assert.Null(GangBmxTransactions.Account(w.GangBmx,S().Gang));Assert.Equal(1,GangBmxTransactions.Multiplier(w.GangBmx,S().Gang,12,Utc));Assert.Null(w.GangBmx);}
        [Fact] public void FutureFieldsSurviveSamplesAndCheckpointProofs()
        {var w=Started();var j=JObject.FromObject(w);j["Future"]=42;j["GangBmx"]["FutureBook"]="keep";j["GangBmx"]["Pending"]["FuturePending"]="keep";j["GangBmx"]["Pending"]["Progress"]["FutureClock"]=123;j["GangBmx"]["Pending"]["Options"]["Checkpoints"][0]["FuturePoint"]=true;w=j.ToObject<WalletData>();Update(w);Pass(w);j=JObject.FromObject(w);Assert.Equal(42,(int)j["Future"]);Assert.Equal("keep",(string)j["GangBmx"]["FutureBook"]);Assert.Equal("keep",(string)j["GangBmx"]["Pending"]["FuturePending"]);Assert.Equal(123,(int)j["GangBmx"]["Pending"]["Progress"]["FutureClock"]);Assert.True((bool)j["GangBmx"]["Pending"]["Options"]["Checkpoints"][0]["FuturePoint"]);}
        [Fact] public void InvalidClearedCountMissingProgressAndImpossibleReadyAreProtected()
        {var w=Started();w.GangBmx.Pending.Cleared=12;Assert.False(w.GangBmx.Valid);Assert.False(Cancel(w));w.GangBmx.Pending.Cleared=0;w.GangBmx.Pending.Progress=null;Assert.False(Update(w));Assert.False(Pass(w));w=Ready();w.GangBmx.Pending.Progress.Faulted=true;Assert.False(w.GangBmx.Valid);Assert.False(Complete(w));}
        [Fact] public void InvalidDuplicatePointsRadiusAndDeadlineCannotStart()
        {var w=W();var c=new GangBmxOptions();c.Checkpoints.Add(c.Checkpoints[0]);Assert.False(c.Valid);Assert.False(Begin(w,c:c));c=new GangBmxOptions{SecondsLimit=0};Assert.False(c.Valid);c=new GangBmxOptions{Radius=float.NaN};Assert.False(c.Valid);c=new GangBmxOptions();c.Checkpoints[0].X+=1000;Assert.False(c.Valid);Assert.Null(w.GangBmx);}
        [Fact] public void PendingBmxBlocksOtherEconomicsWithoutRecursiveValidation()
        {var w=Started();Assert.True(GangBulkTransactions.Blocked(w));Assert.False(GangAutoArmsRules.Free(w));Assert.True(GangBmxTransactions.OtherFree(w));Assert.False(GangDockTransactions.OtherFree(w));Assert.False(GangChauffeurTransactions.OtherFree(w));Assert.False(GangTheatreTransactions.OtherFree(w));Assert.False(GangBusinessTransactions.Trade(w,S(),A(),new GangBusinessOptions(),false,Utc,"trade",Save,out _));Assert.False(GangAutoLaunderTransactions.Configure(w,S(),A(),true,"auto",Save,out _));}
        [Fact] public void OtherInvalidBookOrPaidContractBlocksStart()
        {var w=W();w.GangDock=new GangDockData{Accounts=null};Assert.False(Begin(w));w=W();GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"paid",Save,out _);Assert.False(Begin(w));Assert.Equal(9850,w.Bank);}
        [Fact] public void LostOwnershipBlocksPassSampleAndAwardButAllowsCancel()
        {var w=Started();var s=new GangState{Gang="Other"};Assert.False(GangBmxTransactions.Update(w,s,A(),"bmx",new GangBmxProgress{Elapsed=1000},Save,out _));Assert.False(GangBmxTransactions.Pass(w,s,A(),"bmx",0,true,true,w.GangBmx.Pending.Options.Checkpoints[0],Save,out _));Assert.False(GangBmxTransactions.Complete(w,s,A(),"bmx",Utc,Save,out _));Assert.True(Cancel(w));}
        [Fact] public void CapacityHistoryAndUtcOverflowAreRefused()
        {var w=Ready();Assert.False(Complete(w,DateTime.SpecifyKind(DateTime.MaxValue,DateTimeKind.Utc)));Complete(w);Reward(w).Completed=long.MaxValue;Assert.False(Begin(w,"again"));w=W();w.GangBmx=new GangBmxData();for(int n=0;n<640;n++)w.GangBmx.Accounts.Add(new GangBmxAccount{Gang="Other"+n,Completed=1,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks});Assert.True(w.GangBmx.Valid);Assert.False(Begin(w));}
        [Fact] public void SamplesKeepReceiptsBoundedAndMoneyUntouched()
        {var w=Started();for(int n=0;n<270;n++)Assert.True(Update(w,n));Assert.Equal(256,w.GangBmx.Receipts.Count);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);}
        [Fact] public void SmallerFutureCourseSurvivesReloadWithoutDefaultMerging()
        {var w=W();var c=new GangBmxOptions();c.Checkpoints=c.Checkpoints.Take(2).ToList();Begin(w,c:c);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.Equal(2,w.GangBmx.Pending.Options.Checkpoints.Count);Pass(w);Assert.False(w.GangBmx.Pending.Ready);Pass(w,1);Assert.True(Complete(w));}
        [Fact] public void CheckpointRequiresCorrectRiderAndSafeWorld()
        {var w=Started();var pos=w.GangBmx.Pending.Options.Checkpoints[0];Assert.False(GangBmxTransactions.Pass(w,S(),A(),"bmx",0,false,true,pos,Save,out _));Assert.False(GangBmxTransactions.Pass(w,S(),A(),"bmx",0,true,false,pos,Save,out _));Assert.Equal(0,w.GangBmx.Pending.Cleared);}
        [Fact] public void HorizontalCircleAndVerticalToleranceMatchSourceBoundaries()
        {var c=new GangBmxOptions();var target=new JobPoint{Name="a",X=100,Y=100,Z=10};var p=new JobPoint{Name="b",X=102.5f,Y=100,Z=13};Assert.True(GangBmxRules.Pass(true,true,p,target,c));p.X=102.51f;Assert.False(GangBmxRules.Pass(true,true,p,target,c));p.X=100;p.Z=13.01f;Assert.False(GangBmxRules.Pass(true,true,p,target,c));p.Z=10;p.X=102;p.Y=102;Assert.False(GangBmxRules.Pass(true,true,p,target,c));}
        [Fact] public void InvalidPositionWrongIdOrIndexCannotProduceProof()
        {var w=Started();var p=w.GangBmx.Pending.Options.Checkpoints[0];Assert.False(GangBmxTransactions.Pass(w,S(),A(),"other",0,true,true,p,Save,out _));Assert.False(GangBmxTransactions.Pass(w,S(),A(),"bmx",-1,true,true,p,Save,out _));Assert.False(GangBmxTransactions.Pass(w,S(),A(),"bmx",11,true,true,p,Save,out _));Assert.False(GangBmxTransactions.Pass(w,S(),A(),"bmx",0,true,true,new JobPoint{X=float.NaN},Save,out _));Assert.Equal(0,w.GangBmx.Pending.Cleared);}
        [Fact] public void ClockPauseGapAndRolloverPreserveRemainingTime()
        {int start=int.MaxValue-200;var c=new GangBmxClock(start,75000,new GangBmxProgress{Elapsed=20000});c.Tick(unchecked(start+1000),true);Assert.Equal(21000,c.Progress.Elapsed);c.Tick(unchecked(start+2000),false);c.Tick(unchecked(start+10000),true);Assert.Equal(21000,c.Progress.Elapsed);c.Tick(unchecked(start+11000),true);Assert.Equal(22000,c.Progress.Elapsed);}
        [Fact] public void ClockCannotExceedCapturedDeadlineAndKeepsFaultState()
        {var c=new GangBmxClock(0,75000,new GangBmxProgress{Elapsed=74500,Faulted=true});c.Tick(1000,true);Assert.Equal(75000,c.Progress.Elapsed);Assert.True(c.Progress.Faulted);}
        [Fact] public void PriorDockGradedBonusAndDirtyIncomeRemainUnchanged()
        {var w=Started();w.GangDock=new GangDockData{Accounts={new GangDockAccount{Gang=S().Gang,Completed=1,Bonus=1.5,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks}}};w.GangIllicit=new GangIllicitData{Accounts={new GangIllicitAccount{Gang=S().Gang,Area=7,Dirty=400,Proceeds=400}}};string dock=JsonConvert.SerializeObject(w.GangDock),dirty=JsonConvert.SerializeObject(w.GangIllicit);for(int i=0;i<11;i++)Pass(w,i);Complete(w);Assert.Equal(dock,JsonConvert.SerializeObject(w.GangDock));Assert.Equal(dirty,JsonConvert.SerializeObject(w.GangIllicit));Assert.Equal(1.5,GangDockTransactions.Multiplier(w.GangDock,S().Gang,11,Utc));Assert.Equal(2,GangBmxTransactions.Multiplier(w.GangBmx,S().Gang,12,Utc));}
    }
}
