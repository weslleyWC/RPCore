using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangPierTests
    {
        static readonly Action Save=()=>{};static readonly DateTime Utc=new DateTime(2026,10,26,12,0,0,DateTimeKind.Utc);
        static GangTurfDefinition A()=>GangTurfOptions.Defaults().Areas.Single(a=>a.Id==14);static GangState S()=>new GangState{Gang=A().DefaultOwner};static WalletData W()=>new WalletData{Bank=10000,Cash=123};
        static bool Begin(WalletData w,string id="pier",GangPierOptions c=null,Action save=null)=>GangPierTransactions.Begin(w,S(),A(),c??new GangPierOptions(),id,save??Save,out _);
        static bool Pass(WalletData w,int i=0,Action save=null)=>GangPierTransactions.Work(w,S(),A(),"pier",i,true,true,0,w.GangPier.Pending.Options.TaskMilliseconds,save??Save,out _);
        static bool Update(WalletData w,int elapsed=10000,bool faulted=false,Action save=null)=>GangPierTransactions.Update(w,S(),A(),"pier",new GangPierProgress{Elapsed=elapsed,Faulted=faulted},save??Save,out _);
        static bool Complete(WalletData w,DateTime? utc=null,Action save=null)=>GangPierTransactions.Complete(w,S(),A(),"pier",utc??Utc,save??Save,out _);
        static bool Cancel(WalletData w,Action save=null)=>GangPierTransactions.Cancel(w,"pier",save??Save,out _);
        static WalletData Started(){var w=W();Assert.True(Begin(w));return w;}
        static WalletData Ready(){var w=Started();Update(w);for(int i=0;i<21;i++)Assert.True(Pass(w,i));return w;}
        static GangPierAccount Reward(WalletData w)=>GangPierTransactions.Account(w.GangPier,S().Gang);
        [Fact] public void DefaultsMatchTwentyOneSourcePointsHeadingsTwoRestocksNineteenWaterings()
        {var c=new GangPierOptions();Assert.True(c.Valid);Assert.Equal("prop_wateringcan",c.WaterModel);Assert.Equal(21,c.Points.Count);Assert.Equal(3000,c.TaskMilliseconds);Assert.Equal(300,c.SecondsLimit);Assert.Equal(-1637.986f,c.Points[0].X);Assert.Equal(308.62f,c.Points[0].Heading);Assert.Equal(-1659.828f,c.Points[20].X);Assert.Equal(54.10996f,c.Points[20].Heading);Assert.Equal(19,c.Points.Count-2);}
        [Fact] public void BeginIsFreeWithoutCreatingIncomeStockOrAutomaticOptIn()
        {var w=W();w.Bank=0;Assert.True(Begin(w));Assert.Equal(0,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);Assert.Null(w.GangAutoLaunder);Assert.Empty(w.GangPier.Accounts);}
        [Fact] public void RequiresOwnedTerritoryTwelve()
        {var w=W();var a=GangTurfOptions.Defaults().Areas[0];Assert.False(GangPierTransactions.Begin(w,new GangState{Gang=a.DefaultOwner},a,new GangPierOptions(),"bad",Save,out _));Assert.False(GangPierTransactions.Begin(w,new GangState{Gang="Other"},A(),new GangPierOptions(),"bad",Save,out _));Assert.Null(w.GangPier);}
        [Fact] public void BeginSnapshotsAllPointsHeadingDurationAndDeadline()
        {var w=W();var c=new GangPierOptions();Begin(w,c:c);c.Points[0].X=0;c.Points[0].Heading=0;c.Points.RemoveAt(20);c.TaskMilliseconds=5000;c.SecondsLimit=900;Assert.Equal(-1637.986f,w.GangPier.Pending.Options.Points[0].X);Assert.Equal(308.62f,w.GangPier.Pending.Options.Points[0].Heading);Assert.Equal(21,w.GangPier.Pending.Options.Points.Count);Assert.Equal(3000,w.GangPier.Pending.Options.TaskMilliseconds);Assert.Equal(300,w.GangPier.Pending.Options.SecondsLimit);}
        [Fact] public void PointsMustBePassedInOrderWithoutSkipping()
        {var w=Started();Assert.False(Pass(w,1));Assert.True(Pass(w));Assert.False(Pass(w,2));Assert.Equal(1,w.GangPier.Pending.Done);Assert.False(Complete(w));Assert.Empty(w.GangPier.Accounts);}
        [Fact] public void ReplayedCheckpointDoesNotIncrementOrSaveAgain()
        {var w=Started();Pass(w);int saves=0;Assert.True(Pass(w,0,()=>saves++));Assert.Equal(0,saves);Assert.Equal(1,w.GangPier.Pending.Done);}
        [Fact] public void EveryCheckpointRequiredAndFinalConfirmationIsSeparate()
        {var w=Started();for(int i=0;i<20;i++)Pass(w,i);Assert.False(Complete(w));Pass(w,20);Assert.True(w.GangPier.Pending.Ready);Assert.Empty(w.GangPier.Accounts);Assert.Null(w.GangAutoLaunder);}
        [Fact] public void LastCheckpointStillCountsJustBeforeDeadline()
        {var w=Started();for(int i=0;i<20;i++)Pass(w,i);Assert.True(Update(w,299999));Assert.True(Pass(w,20));Assert.True(w.GangPier.Pending.Ready);Assert.False(Update(w,300000));Assert.True(Complete(w));}
        [Fact] public void ExactDeadlineRejectsCheckpointAndPersistsFailureAcrossReload()
        {var w=Started();Update(w,300000);Assert.True(w.GangPier.Pending.Failed);Assert.True(w.GangPier.Valid);Assert.False(Pass(w));w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(w.GangPier.Pending.Failed);Assert.False(Update(w,0));Assert.False(Complete(w));Assert.True(Cancel(w));}
        [Fact] public void AtomicAwardRemovesPendingWithoutFinancialMutation()
        {var w=Ready();int saves=0;Assert.True(Complete(w,save:()=>{saves++;Assert.Null(w.GangPier.Pending);Assert.Equal(1,Reward(w).Completed);}));Assert.Equal(1,saves);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);Assert.Null(w.GangAutoLaunder);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void CompletionReplayNeverCountsTwiceOrExtendsWindow()
        {var w=Ready();Complete(w);var old=w.GangPier;int saves=0;Assert.True(Complete(w,Utc.AddMinutes(5),()=>saves++));Assert.Same(old,w.GangPier);Assert.Equal(0,saves);Assert.Equal(1,Reward(w).Completed);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void PartialReloadPreservesDoneCountRemainingTimeAndSnapshot()
        {var w=Started();Pass(w);Pass(w,1);Pass(w,2);Update(w,20000);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(w.GangPier.Valid);Assert.Equal(3,w.GangPier.Pending.Done);Assert.Equal(20000,w.GangPier.Pending.Progress.Elapsed);Assert.Equal(21,w.GangPier.Pending.Options.Points.Count);Assert.True(Pass(w,3));Assert.False(Complete(w));}
        [Fact] public void ReadyReloadCanAwardWithoutVehicleOrRepeatedCourse()
        {var w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(Ready()));Assert.True(w.GangPier.Pending.Ready);Assert.True(Complete(w));Assert.Null(w.GangPier.Pending);}
        [Fact] public void FaultedContractCannotBeResumedByClearingFlagOrTime()
        {var w=Started();Update(w,20000,true);Assert.True(w.GangPier.Pending.Failed);Assert.False(Update(w,20001));Assert.False(Update(w,0,true));Assert.False(Pass(w));Assert.True(Cancel(w));}
        [Fact] public void FailedBeginRestoresAbsentBook()
        {var w=W();Assert.Throws<InvalidOperationException>(()=>Begin(w,save:()=>throw new InvalidOperationException()));Assert.Null(w.GangPier);}
        [Fact] public void FailedCheckpointRestoresPreviousIndexAndReceiptForRetry()
        {var w=Started();var old=w.GangPier;Assert.Throws<InvalidOperationException>(()=>Pass(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangPier);Assert.Equal(0,w.GangPier.Pending.Done);Assert.DoesNotContain("bmx:bmx-pass:0",w.GangPier.Receipts);Assert.True(Pass(w));}
        [Fact] public void FailedSampleRestoresElapsedAndExactBook()
        {var w=Started();var old=w.GangPier;Assert.Throws<InvalidOperationException>(()=>Update(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangPier);Assert.Equal(0,w.GangPier.Pending.Progress.Elapsed);}
        [Fact] public void FailedAwardPreservesReadyWithoutGrantingBonus()
        {var w=Ready();var old=w.GangPier;Assert.Throws<InvalidOperationException>(()=>Complete(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangPier);Assert.True(w.GangPier.Pending.Ready);Assert.Empty(w.GangPier.Accounts);}
        [Fact] public void CancelIsFreeAndFailureRestoresPendingProgress()
        {var w=Started();Pass(w);var old=w.GangPier;Assert.Throws<InvalidOperationException>(()=>Cancel(w,()=>throw new InvalidOperationException()));Assert.Same(old,w.GangPier);Assert.True(Cancel(w));Assert.Null(w.GangPier.Pending);Assert.Equal(10000,w.Bank);Assert.Empty(w.GangPier.Accounts);Assert.True(Cancel(w));}
        [Fact] public void CancellingNewContractPreservesEarlierBonus()
        {var w=Ready();Complete(w);long expiry=Reward(w).ExpiresUtcTicks;Begin(w,"again");Assert.True(GangPierTransactions.Cancel(w,"again",Save,out _));Assert.Equal(1,Reward(w).Completed);Assert.Equal(expiry,Reward(w).ExpiresUtcTicks);}
        [Fact] public void NewRealCompletionRefreshesWithoutStackingDuration()
        {var w=Ready();Complete(w);Begin(w,"again");for(int i=0;i<21;i++)GangPierTransactions.Work(w,S(),A(),"again",i,true,true,0,3000,Save,out _);Assert.True(GangPierTransactions.Complete(w,S(),A(),"again",Utc.AddMinutes(10),Save,out _));Assert.Equal(2,Reward(w).Completed);Assert.Equal(Utc.AddMinutes(70).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void BonusIsBoundToUtcGangAreaAndStrictExpiry()
        {var w=Ready();Complete(w);var b=w.GangPier;Assert.Equal(2,GangPierTransactions.Multiplier(b,S().Gang.ToUpperInvariant(),14,Utc));Assert.Equal(1,GangPierTransactions.Multiplier(b,S().Gang,14,Utc.AddHours(1)));Assert.Equal(1,GangPierTransactions.Multiplier(b,S().Gang,14,Utc.AddTicks(-1)));Assert.Equal(1,GangPierTransactions.Multiplier(b,"Other",14,Utc));Assert.Equal(1,GangPierTransactions.Multiplier(b,S().Gang,13,Utc));Assert.Equal(1,GangPierTransactions.Multiplier(b,S().Gang,14,DateTime.SpecifyKind(Utc,DateTimeKind.Local)));}
        [Fact] public void LegacyQueriesDoNotCreateBookOrBonus()
        {var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":100,\"Future\":42}");Assert.Null(w.GangPier);Assert.False(GangPierTransactions.Blocked(w));Assert.Null(GangPierTransactions.Account(w.GangPier,S().Gang));Assert.Equal(1,GangPierTransactions.Multiplier(w.GangPier,S().Gang,14,Utc));Assert.Null(w.GangPier);}
        [Fact] public void FutureFieldsSurviveSamplesAndCheckpointProofs()
        {var w=Started();var j=JObject.FromObject(w);j["Future"]=42;j["GangPier"]["FutureBook"]="keep";j["GangPier"]["Pending"]["FuturePending"]="keep";j["GangPier"]["Pending"]["Progress"]["FutureClock"]=123;j["GangPier"]["Pending"]["Options"]["Points"][0]["FuturePoint"]=true;w=j.ToObject<WalletData>();Update(w);Pass(w);j=JObject.FromObject(w);Assert.Equal(42,(int)j["Future"]);Assert.Equal("keep",(string)j["GangPier"]["FutureBook"]);Assert.Equal("keep",(string)j["GangPier"]["Pending"]["FuturePending"]);Assert.Equal(123,(int)j["GangPier"]["Pending"]["Progress"]["FutureClock"]);Assert.True((bool)j["GangPier"]["Pending"]["Options"]["Points"][0]["FuturePoint"]);}
        [Fact] public void InvalidDoneCountMissingProgressAndImpossibleReadyAreProtected()
        {var w=Started();w.GangPier.Pending.Done=22;Assert.False(w.GangPier.Valid);Assert.False(Cancel(w));w.GangPier.Pending.Done=0;w.GangPier.Pending.Progress=null;Assert.False(Update(w));Assert.False(Pass(w));w=Ready();w.GangPier.Pending.Progress.Faulted=true;Assert.False(w.GangPier.Valid);Assert.False(Complete(w));}
        [Fact] public void InvalidDuplicatePointsModelAndDurationCannotStart()
        {var w=W();var c=new GangPierOptions();c.Points.Add(c.Points[0]);Assert.False(c.Valid);Assert.False(Begin(w,c:c));Assert.False(new GangPierOptions{TaskMilliseconds=0}.Valid);Assert.False(new GangPierOptions{WaterModel=""}.Valid);Assert.Null(w.GangPier);}
        [Fact] public void PendingPierBlocksOtherEconomicsWithoutRecursiveValidation()
        {var w=Started();Assert.True(GangBulkTransactions.Blocked(w));Assert.False(GangAutoArmsRules.Free(w));Assert.True(GangPierTransactions.OtherFree(w));Assert.False(GangOffshoreTransactions.OtherFree(w));Assert.False(GangChauffeurTransactions.OtherFree(w));Assert.False(GangTheatreTransactions.OtherFree(w));Assert.False(GangBusinessTransactions.Trade(w,S(),A(),new GangBusinessOptions(),false,Utc,"trade",Save,out _));Assert.False(GangAutoLaunderTransactions.Configure(w,S(),A(),true,"auto",Save,out _));}
        [Fact] public void OtherInvalidBookOrPaidContractBlocksStart()
        {var w=W();w.GangDock=new GangDockData{Accounts=null};Assert.False(Begin(w));w=W();GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"paid",Save,out _);Assert.False(Begin(w));Assert.Equal(9850,w.Bank);}
        [Fact] public void LostOwnershipBlocksWorkSampleAwardButAllowsCancel()
        {var w=Started();var state=new GangState{Gang="Other"};Assert.False(GangPierTransactions.Work(w,state,A(),"pier",0,true,true,0,3000,Save,out _));Assert.False(GangPierTransactions.Update(w,state,A(),"pier",new GangPierProgress{Elapsed=1000},Save,out _));Assert.False(GangPierTransactions.Complete(w,state,A(),"pier",Utc,Save,out _));Assert.True(Cancel(w));}
        [Fact] public void CapacityHistoryAndUtcOverflowAreRefused()
        {var w=Ready();Assert.False(Complete(w,DateTime.SpecifyKind(DateTime.MaxValue,DateTimeKind.Utc)));Complete(w);Reward(w).Completed=long.MaxValue;Assert.False(Begin(w,"again"));w=W();w.GangPier=new GangPierData();for(int n=0;n<640;n++)w.GangPier.Accounts.Add(new GangPierAccount{Gang="Other"+n,Completed=1,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks});Assert.True(w.GangPier.Valid);Assert.False(Begin(w));}
        [Fact] public void SamplesKeepReceiptsBoundedAndMoneyUntouched()
        {var w=Started();for(int n=0;n<270;n++)Assert.True(Update(w,n));Assert.Equal(256,w.GangPier.Receipts.Count);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);}
        [Fact] public void ShorterFutureCourseKeepsSnapshotWithoutMergingDefaults()
        {var w=W();var c=new GangPierOptions();c.Points=c.Points.Take(3).ToList();Assert.True(Begin(w,c:c));w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.Equal(3,w.GangPier.Pending.Options.Points.Count);Pass(w);Pass(w,1);Assert.False(w.GangPier.Pending.Ready);Pass(w,2);Assert.True(Complete(w));}
        [Fact] public void WorkRequiresSafeFootRangeAndCompleteCapturedDuration()
        {var w=Started();Assert.False(GangPierTransactions.Work(w,S(),A(),"pier",0,false,true,0,3000,Save,out _));Assert.False(GangPierTransactions.Work(w,S(),A(),"pier",0,true,false,0,3000,Save,out _));Assert.False(GangPierTransactions.Work(w,S(),A(),"pier",0,true,true,2.01f,3000,Save,out _));Assert.False(GangPierTransactions.Work(w,S(),A(),"pier",0,true,true,0,2999,Save,out _));Assert.True(GangPierTransactions.Work(w,S(),A(),"pier",0,true,true,2,3000,Save,out _));}
        [Fact] public void InvalidObservationIdIndexAndNonfiniteDistanceCannotConfirmWork()
        {var w=Started();Assert.False(GangPierTransactions.Work(w,S(),A(),"other",0,true,true,0,3000,Save,out _));Assert.False(GangPierTransactions.Work(w,S(),A(),"pier",21,true,true,0,3000,Save,out _));Assert.False(GangPierTransactions.Work(w,S(),A(),"pier",0,true,true,float.NaN,3000,Save,out _));Assert.Equal(0,w.GangPier.Pending.Done);}
        [Fact] public void ThreeContinuousSecondsRequiredAndInterruptionResetsWork()
        {var t=new GangPierTask(0);Assert.False(t.Step(1000,true,3000));Assert.False(t.Step(2000,true,3000));Assert.True(t.Step(3000,true,3000));Assert.False(t.Step(4000,false,3000));Assert.Equal(0,t.Milliseconds);Assert.False(t.Step(10000,true,3000));Assert.Equal(0,t.Milliseconds);}
        [Fact] public void ClockPauseGapAndRolloverPreserveRemainingTime()
        {int start=int.MaxValue-200;var c=new GangPierClock(start,300000,new GangPierProgress{Elapsed=20000});c.Tick(unchecked(start+1000),true);Assert.Equal(21000,c.Progress.Elapsed);c.Tick(unchecked(start+2000),false);c.Tick(unchecked(start+10000),true);Assert.Equal(21000,c.Progress.Elapsed);c.Tick(unchecked(start+11000),true);Assert.Equal(22000,c.Progress.Elapsed);}
        [Fact] public void ClockCannotExceedCapturedDeadlineAndKeepsFaultState()
        {var c=new GangPierClock(0,300000,new GangPierProgress{Elapsed=299500,Faulted=true});c.Tick(1000,true);Assert.Equal(300000,c.Progress.Elapsed);Assert.True(c.Progress.Faulted);}
        [Fact] public void PriorDockGradedBonusAndDirtyIncomeRemainUnchanged()
        {var w=Started();w.GangDock=new GangDockData{Accounts={new GangDockAccount{Gang=S().Gang,Completed=1,Bonus=1.5,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks}}};w.GangIllicit=new GangIllicitData{Accounts={new GangIllicitAccount{Gang=S().Gang,Area=7,Dirty=400,Proceeds=400}}};string dock=JsonConvert.SerializeObject(w.GangDock),dirty=JsonConvert.SerializeObject(w.GangIllicit);for(int i=0;i<21;i++)Pass(w,i);Complete(w);Assert.Equal(dock,JsonConvert.SerializeObject(w.GangDock));Assert.Equal(dirty,JsonConvert.SerializeObject(w.GangIllicit));Assert.Equal(1.5,GangDockTransactions.Multiplier(w.GangDock,S().Gang,11,Utc));Assert.Equal(2,GangPierTransactions.Multiplier(w.GangPier,S().Gang,14,Utc));}
    }
}
