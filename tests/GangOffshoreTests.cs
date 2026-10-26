using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangOffshoreTests
    {
        static readonly Action Save=()=>{};static readonly DateTime Utc=new DateTime(2026,10,26,12,0,0,DateTimeKind.Utc);
        static GangTurfDefinition A()=>GangTurfOptions.Defaults().Areas.Single(a=>a.Id==13);static GangState S()=>new GangState{Gang=A().DefaultOwner};static WalletData W()=>new WalletData{Bank=10000,Cash=123};
        static bool Begin(WalletData w,string id="sea",GangOffshoreOptions c=null,Action save=null)=>GangOffshoreTransactions.Begin(w,S(),A(),c??new GangOffshoreOptions(),id,save??Save,out _);
        static bool Collect(WalletData w,Action save=null)=>GangOffshoreTransactions.Collect(w,S(),A(),"sea",save??Save,out _);
        static bool Board(WalletData w,Action save=null)=>GangOffshoreTransactions.Board(w,S(),A(),"sea",save??Save,out _);
        static bool Deliver(WalletData w,Action save=null)=>GangOffshoreTransactions.Deliver(w,S(),A(),"sea",save??Save,out _);
        static bool Update(WalletData w,int elapsed=10000,bool faulted=false,Action save=null)=>GangOffshoreTransactions.Update(w,S(),A(),"sea",new GangOffshoreProgress{Elapsed=elapsed,Faulted=faulted},save??Save,out _);
        static bool Complete(WalletData w,DateTime? utc=null,Action save=null)=>GangOffshoreTransactions.Complete(w,S(),A(),"sea",utc??Utc,save??Save,out _);
        static bool Cancel(WalletData w,Action save=null)=>GangOffshoreTransactions.Cancel(w,"sea",save??Save,out _);
        static WalletData Started(){var w=W();Assert.True(Begin(w));return w;}
        static WalletData Ready(){var w=Started();Assert.True(Collect(w));Assert.True(Board(w));Assert.True(Update(w));Assert.True(Deliver(w));return w;}
        static GangOffshoreAccount Reward(WalletData w)=>GangOffshoreTransactions.Account(w.GangOffshore,S().Gang);
        [Fact] public void DefaultsMatchSourceModelsThreePositionsHeadingsAndDeadline()
        {var c=new GangOffshoreOptions();Assert.True(c.Valid);Assert.Equal("seashark",c.JetModel);Assert.Equal("squalo",c.BoatModel);Assert.Equal("a_m_y_business_01",c.BuyerModel);Assert.Equal(-1065.109f,c.Pickup.X);Assert.Equal(232.1834f,c.Pickup.Heading);Assert.Equal(-1103.408f,c.Jet.X);Assert.Equal(-.5149324f,c.Jet.Z);Assert.Equal(27.82469f,c.Jet.Heading);Assert.Equal(-2279.427f,c.Buyer.X);Assert.Equal(36.92299f,c.Buyer.Heading);Assert.Equal(300,c.SecondsLimit);Assert.Equal(60,c.BoostMinutes);}
        [Fact] public void BeginIsFreeWithoutMoneyStockDirtyOrAutomaticOptIn()
        {var w=W();w.Bank=0;Assert.True(Begin(w));Assert.Equal(0,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);Assert.Null(w.GangAutoLaunder);Assert.Empty(w.GangOffshore.Accounts);}
        [Fact] public void RequiresOwnedTerritoryThirteen()
        {var w=W();var a=GangTurfOptions.Defaults().Areas[0];Assert.False(GangOffshoreTransactions.Begin(w,new GangState{Gang=a.DefaultOwner},a,new GangOffshoreOptions(),"bad",Save,out _));Assert.False(GangOffshoreTransactions.Begin(w,new GangState{Gang="Other"},A(),new GangOffshoreOptions(),"bad",Save,out _));Assert.Null(w.GangOffshore);}
        [Fact] public void SnapshotCapturesModelsPointsAndDeadline()
        {var w=W();var c=new GangOffshoreOptions();Begin(w,c:c);c.Jet.X=0;c.Buyer.Y=0;c.SecondsLimit=900;c.BoatModel="other";Assert.Equal(-1103.408f,w.GangOffshore.Pending.Options.Jet.X);Assert.Equal(-959.0692f,w.GangOffshore.Pending.Options.Buyer.Y);Assert.Equal(300,w.GangOffshore.Pending.Options.SecondsLimit);Assert.Equal("squalo",w.GangOffshore.Pending.Options.BoatModel);}
        [Fact] public void MustCollectThenBoardThenDeliverBeforeAward()
        {var w=Started();Assert.False(Board(w));Assert.False(Deliver(w));Assert.False(Complete(w));Collect(w);Assert.False(Deliver(w));Board(w);Assert.False(Complete(w));Deliver(w);Assert.True(w.GangOffshore.Pending.Ready);Assert.Empty(w.GangOffshore.Accounts);}
        [Fact] public void ReplayedCollectionBoardAndDeliveryDoNotSaveAgain()
        {var w=Ready();int saves=0;Assert.True(Collect(w,()=>saves++));Assert.True(Board(w,()=>saves++));Assert.True(Deliver(w,()=>saves++));Assert.Equal(0,saves);Assert.True(w.GangOffshore.Pending.Ready);}
        [Fact] public void AwardAndPendingRemovalAreAtomicWithoutMoney()
        {var w=Ready();int saves=0;Assert.True(Complete(w,save:()=>{saves++;Assert.Null(w.GangOffshore.Pending);Assert.Equal(1,Reward(w).Completed);}));Assert.Equal(1,saves);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);Assert.Null(w.GangAutoLaunder);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void CompletionReplayNeverRefreshesExpiryOrCountsTwice()
        {var w=Ready();Complete(w);var old=w.GangOffshore;int saves=0;Assert.True(Complete(w,Utc.AddMinutes(5),()=>saves++));Assert.Same(old,w.GangOffshore);Assert.Equal(0,saves);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void CollectedAndBoardedReloadPreservesTimeAndPackageProof()
        {var w=Started();Collect(w);Board(w);Update(w,50000);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(w.GangOffshore.Valid);Assert.True(w.GangOffshore.Pending.Collected);Assert.True(w.GangOffshore.Pending.Boarded);Assert.Equal(50000,w.GangOffshore.Pending.Progress.Elapsed);Assert.False(w.GangOffshore.Pending.Delivered);Assert.True(Deliver(w));}
        [Fact] public void DeliveredReloadCanAwardWithoutNativeEntities()
        {var w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(Ready()));Assert.True(w.GangOffshore.Pending.Ready);Assert.True(Complete(w));}
        [Fact] public void DeadlineAndFaultCannotBeRewoundOrCleared()
        {var w=Started();Collect(w);Board(w);Update(w,300000);Assert.True(w.GangOffshore.Valid);Assert.True(w.GangOffshore.Pending.Failed);Assert.False(Deliver(w));Assert.False(Update(w,0));Assert.True(Cancel(w));w=Started();Update(w,10000,true);Assert.False(Update(w,10001));Assert.False(Collect(w));Assert.True(Cancel(w));}
        [Fact] public void DeliveryJustBeforeDeadlineFreezesSavedReadyProof()
        {var w=Started();Collect(w);Board(w);Update(w,299999);Assert.True(Deliver(w));Assert.False(Update(w,300000));Assert.True(Complete(w));}
        [Fact] public void FailedBeginRestoresAbsentBook()
        {var w=W();Assert.Throws<InvalidOperationException>(()=>Begin(w,save:()=>throw new InvalidOperationException()));Assert.Null(w.GangOffshore);}
        [Fact] public void FailedCollectionAndBoardRestorePreviousProofs()
        {var w=Started();var old=w.GangOffshore;Assert.Throws<InvalidOperationException>(()=>Collect(w,()=>throw new InvalidOperationException()));Assert.Same(old,w.GangOffshore);Assert.False(w.GangOffshore.Pending.Collected);Collect(w);old=w.GangOffshore;Assert.Throws<InvalidOperationException>(()=>Board(w,()=>throw new InvalidOperationException()));Assert.Same(old,w.GangOffshore);Assert.False(w.GangOffshore.Pending.Boarded);}
        [Fact] public void FailedSampleAndDeliveryPreservePreviousState()
        {var w=Started();Collect(w);Board(w);var old=w.GangOffshore;Assert.Throws<InvalidOperationException>(()=>Update(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangOffshore);Assert.Throws<InvalidOperationException>(()=>Deliver(w,()=>throw new InvalidOperationException()));Assert.Same(old,w.GangOffshore);Assert.False(w.GangOffshore.Pending.Delivered);}
        [Fact] public void FailedAwardKeepsReadyAndDoesNotGrantBonus()
        {var w=Ready();var old=w.GangOffshore;Assert.Throws<InvalidOperationException>(()=>Complete(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangOffshore);Assert.True(w.GangOffshore.Pending.Ready);Assert.Empty(w.GangOffshore.Accounts);}
        [Fact] public void CancelIsFreeAndFailedSaveKeepsPackageProof()
        {var w=Started();Collect(w);var old=w.GangOffshore;Assert.Throws<InvalidOperationException>(()=>Cancel(w,()=>throw new InvalidOperationException()));Assert.Same(old,w.GangOffshore);Assert.True(Cancel(w));Assert.Null(w.GangOffshore.Pending);Assert.Empty(w.GangOffshore.Accounts);Assert.Equal(10000,w.Bank);Assert.True(Cancel(w));}
        [Fact] public void CancelAnotherContractPreservesPreviousBonus()
        {var w=Ready();Complete(w);long expiry=Reward(w).ExpiresUtcTicks;Begin(w,"again");GangOffshoreTransactions.Cancel(w,"again",Save,out _);Assert.Equal(1,Reward(w).Completed);Assert.Equal(expiry,Reward(w).ExpiresUtcTicks);}
        [Fact] public void NewRealCompletionRefreshesWithoutStackingDuration()
        {var w=Ready();Complete(w);Begin(w,"again");GangOffshoreTransactions.Collect(w,S(),A(),"again",Save,out _);GangOffshoreTransactions.Board(w,S(),A(),"again",Save,out _);GangOffshoreTransactions.Deliver(w,S(),A(),"again",Save,out _);Assert.True(GangOffshoreTransactions.Complete(w,S(),A(),"again",Utc.AddMinutes(10),Save,out _));Assert.Equal(2,Reward(w).Completed);Assert.Equal(Utc.AddMinutes(70).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void BonusUsesStrictUtcGangAreaAndWindow()
        {var w=Ready();Complete(w);var b=w.GangOffshore;Assert.Equal(2,GangOffshoreTransactions.Multiplier(b,S().Gang.ToUpperInvariant(),13,Utc));Assert.Equal(1,GangOffshoreTransactions.Multiplier(b,S().Gang,13,Utc.AddHours(1)));Assert.Equal(1,GangOffshoreTransactions.Multiplier(b,S().Gang,13,Utc.AddTicks(-1)));Assert.Equal(1,GangOffshoreTransactions.Multiplier(b,"Other",13,Utc));Assert.Equal(1,GangOffshoreTransactions.Multiplier(b,S().Gang,12,Utc));Assert.Equal(1,GangOffshoreTransactions.Multiplier(b,S().Gang,13,DateTime.SpecifyKind(Utc,DateTimeKind.Local)));}
        [Fact] public void LegacyQueriesDoNotCreateBookOrBonus()
        {var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":100}");Assert.Null(w.GangOffshore);Assert.False(GangOffshoreTransactions.Blocked(w));Assert.Equal(1,GangOffshoreTransactions.Multiplier(w.GangOffshore,S().Gang,13,Utc));Assert.Null(w.GangOffshore);}
        [Fact] public void FutureFieldsSurviveSamplesAndAllStages()
        {var w=Started();var j=JObject.FromObject(w);j["Future"]=42;j["GangOffshore"]["FutureBook"]="keep";j["GangOffshore"]["Pending"]["Progress"]["FutureClock"]=123;j["GangOffshore"]["Pending"]["Options"]["Buyer"]["FuturePoint"]=true;w=j.ToObject<WalletData>();Update(w);Collect(w);Board(w);Deliver(w);j=JObject.FromObject(w);Assert.Equal(42,(int)j["Future"]);Assert.Equal("keep",(string)j["GangOffshore"]["FutureBook"]);Assert.Equal(123,(int)j["GangOffshore"]["Pending"]["Progress"]["FutureClock"]);Assert.True((bool)j["GangOffshore"]["Pending"]["Options"]["Buyer"]["FuturePoint"]);}
        [Fact] public void ImpossibleBoardingDeliveryMissingProgressAndInvalidOptionsProtected()
        {var w=Started();w.GangOffshore.Pending.Boarded=true;Assert.False(w.GangOffshore.Valid);Assert.False(Cancel(w));w.GangOffshore.Pending.Boarded=false;w.GangOffshore.Pending.Progress=null;Assert.False(Update(w));var c=new GangOffshoreOptions{SecondsLimit=0};Assert.False(c.Valid);c=new GangOffshoreOptions();c.Buyer.X=c.Jet.X;c.Buyer.Y=c.Jet.Y;Assert.False(c.Valid);}
        [Fact] public void PendingOffshoreBlocksOtherEconomicsWithoutRecursion()
        {var w=Started();Assert.True(GangBulkTransactions.Blocked(w));Assert.False(GangAutoArmsRules.Free(w));Assert.True(GangOffshoreTransactions.OtherFree(w));Assert.False(GangBmxTransactions.OtherFree(w));Assert.False(GangDockTransactions.OtherFree(w));Assert.False(GangBusinessTransactions.Trade(w,S(),A(),new GangBusinessOptions(),false,Utc,"trade",Save,out _));Assert.False(GangAutoLaunderTransactions.Configure(w,S(),A(),true,"auto",Save,out _));}
        [Fact] public void OtherInvalidBookAndPaidContractPreventStart()
        {var w=W();w.GangBmx=new GangBmxData{Accounts=null};Assert.False(Begin(w));w=W();GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"paid",Save,out _);Assert.False(Begin(w));Assert.Equal(9850,w.Bank);}
        [Fact] public void LostOwnershipPreventsCollectionAndAwardButAllowsCancel()
        {var w=Started();var s=new GangState{Gang="Other"};Assert.False(GangOffshoreTransactions.Collect(w,s,A(),"sea",Save,out _));Assert.False(GangOffshoreTransactions.Complete(w,s,A(),"sea",Utc,Save,out _));Assert.True(Cancel(w));}
        [Fact] public void HistoryCapacityAndUtcOverflowAreRefused()
        {var w=Ready();Assert.False(Complete(w,DateTime.SpecifyKind(DateTime.MaxValue,DateTimeKind.Utc)));Complete(w);Reward(w).Completed=long.MaxValue;Assert.False(Begin(w,"again"));w=W();w.GangOffshore=new GangOffshoreData();for(int n=0;n<640;n++)w.GangOffshore.Accounts.Add(new GangOffshoreAccount{Gang="Other"+n,Completed=1,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks});Assert.True(w.GangOffshore.Valid);Assert.False(Begin(w));}
        [Fact] public void RepeatedSamplesKeepReceiptsBounded()
        {var w=Started();for(int n=0;n<270;n++)Assert.True(Update(w,n));Assert.Equal(256,w.GangOffshore.Receipts.Count);Assert.Equal(10000,w.Bank);Assert.Null(w.GangIllicit);}
        [Fact] public void PickupRequiresSafeFootWithinTwoMeters()
        {Assert.True(GangOffshoreRules.Pickup(true,true,2));Assert.False(GangOffshoreRules.Pickup(true,true,2.01f));Assert.False(GangOffshoreRules.Pickup(true,false,1));Assert.False(GangOffshoreRules.Pickup(false,true,1));Assert.False(GangOffshoreRules.Pickup(true,true,float.NaN));}
        [Fact] public void HandoverRequiresOwnDriverBuyerFiveMeterRadiusAndLowSpeed()
        {Assert.True(GangOffshoreRules.Handover(true,true,true,5,1.49f));Assert.False(GangOffshoreRules.Handover(true,true,true,5.01f,0));Assert.False(GangOffshoreRules.Handover(true,true,true,1,1.5f));Assert.False(GangOffshoreRules.Handover(true,false,true,1,0));Assert.False(GangOffshoreRules.Handover(true,true,false,1,0));Assert.False(GangOffshoreRules.Handover(false,true,true,1,0));}
        [Fact] public void FiveContinuousSecondsRequiredAndInterruptionsResetTransfer()
        {var h=new GangOffshoreHandover(0);for(int t=1000;t<5000;t+=1000)Assert.False(h.Step(t,true));Assert.True(h.Step(5000,true));Assert.False(h.Step(6000,false));Assert.Equal(0,h.Milliseconds);Assert.False(h.Step(10000,true));Assert.Equal(0,h.Milliseconds);}
        [Fact] public void TimerPreservesElapsedThroughPauseGapRolloverAndResume()
        {int start=int.MaxValue-200;var c=new GangOffshoreClock(start,300000,new GangOffshoreProgress{Elapsed=50000});c.Tick(unchecked(start+1000),true);c.Tick(unchecked(start+2000),false);c.Tick(unchecked(start+10000),true);Assert.Equal(51000,c.Progress.Elapsed);c.Tick(unchecked(start+11000),true);Assert.Equal(52000,c.Progress.Elapsed);}
        [Fact] public void ClockCapsAtDeadlineAndPreservesFaultFlag()
        {var c=new GangOffshoreClock(0,300000,new GangOffshoreProgress{Elapsed=299500,Faulted=true});c.Tick(1000,true);Assert.Equal(300000,c.Progress.Elapsed);Assert.True(c.Progress.Faulted);}
    }
}
