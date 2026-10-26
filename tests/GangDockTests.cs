using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangDockTests
    {
        static readonly Action Save=()=>{};static readonly DateTime Utc=new DateTime(2026,10,26,12,0,0,DateTimeKind.Utc);
        static GangTurfDefinition A()=>GangTurfOptions.Defaults().Areas.Single(a=>a.Id==11);static GangState S()=>new GangState{Gang=A().DefaultOwner};static WalletData W()=>new WalletData{Bank=10000,Cash=123};
        static bool Begin(WalletData w,string id="dock",GangDockOptions c=null,Action save=null)=>GangDockTransactions.Begin(w,S(),A(),c??new GangDockOptions(),id,save??Save,out _);
        static bool Load(WalletData w,int i=0,Action save=null)=>GangDockTransactions.Load(w,S(),A(),"dock",i,save??Save,out _);
        static bool Unload(WalletData w,int i=0,Action save=null)=>GangDockTransactions.Unload(w,S(),A(),"dock",i,save??Save,out _);
        static bool Update(WalletData w,int elapsed=10000,float body=1000,bool faulted=false,Action save=null)=>GangDockTransactions.Update(w,S(),A(),"dock",new GangDockProgress{Elapsed=elapsed,BodyHealth=body,Faulted=faulted},save??Save,out _);
        static bool Complete(WalletData w,DateTime? utc=null,Action save=null)=>GangDockTransactions.Complete(w,S(),A(),"dock",utc??Utc,save??Save,out _);
        static bool Cancel(WalletData w,Action save=null)=>GangDockTransactions.Cancel(w,"dock",save??Save,out _);
        static WalletData Started(){var w=W();Assert.True(Begin(w));return w;}
        static WalletData Loaded(){var w=Started();for(int i=0;i<7;i++)Assert.True(Load(w,i));return w;}
        static WalletData Ready(float body=1000){var w=Loaded();Assert.True(Update(w,body:body));for(int i=0;i<7;i++)Assert.True(Unload(w,i));return w;}
        static GangDockAccount Reward(WalletData w)=>GangDockTransactions.Account(w.GangDock,S().Gang);
        [Fact] public void DefaultsMatchSourceRigPointsSevenCargoSlotsAndTenMinuteLimit()
        {var c=new GangDockOptions();Assert.True(c.Valid);Assert.Equal("phantom",c.TruckModel);Assert.Equal("trflat",c.TrailerModel);Assert.Equal("prop_cementbags01",c.CargoModelA);Assert.Equal("prop_conc_blocks01a",c.CargoModelB);Assert.Equal(7,c.Slots.Count);Assert.Equal(3.2f,c.Slots[0].Y);Assert.Equal(-5.2f,c.Slots[6].Y);Assert.Equal(.4f,c.Slots[6].Z);Assert.Equal(-779.6579f,c.Truck.X);Assert.Equal(59.62956f,c.Truck.Heading);Assert.Equal(-816.874f,c.Trailer.X);Assert.Equal(239.2787f,c.Trailer.Heading);Assert.Equal(-765.6149f,c.Bay.X);Assert.Equal(1377.557f,c.Destination.X);Assert.Equal(600,c.SecondsLimit);}
        [Fact] public void BeginIsFreeWithoutStockCashDirtyOrAutomaticOptIn()
        {var w=W();w.Bank=0;Assert.True(Begin(w));Assert.Equal(0,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);Assert.Null(w.GangAutoLaunder);Assert.Empty(w.GangDock.Accounts);}
        [Fact] public void StartRequiresOwnedTerritoryEleven()
        {var w=W();var a=GangTurfOptions.Defaults().Areas[0];Assert.False(GangDockTransactions.Begin(w,new GangState{Gang=a.DefaultOwner},a,new GangDockOptions(),"bad",Save,out _));Assert.False(GangDockTransactions.Begin(w,new GangState{Gang="Other"},A(),new GangDockOptions(),"bad",Save,out _));Assert.Null(w.GangDock);}
        [Fact] public void SnapshotCapturesSlotsModelsCoordinatesAndDeadline()
        {var w=W();var c=new GangDockOptions();Begin(w,c:c);c.Truck.X=0;c.Destination.Y=0;c.Slots[0].Y=0;c.Slots.RemoveAt(6);c.SecondsLimit=120;c.TruckModel="other";Assert.Equal(-779.6579f,w.GangDock.Pending.Options.Truck.X);Assert.Equal(-743.3366f,w.GangDock.Pending.Options.Destination.Y);Assert.Equal(3.2f,w.GangDock.Pending.Options.Slots[0].Y);Assert.Equal(7,w.GangDock.Pending.Options.Slots.Count);Assert.Equal("phantom",w.GangDock.Pending.Options.TruckModel);Assert.Equal(600,w.GangDock.Pending.Options.SecondsLimit);}
        [Fact] public void CannotUnloadBeforeFullLoadOrSkipIndices()
        {var w=Started();Assert.False(Unload(w));Assert.False(Load(w,1));Load(w);Assert.False(Unload(w));Assert.False(Load(w,2));Assert.Equal(1,w.GangDock.Pending.Loaded);Assert.Equal(0,w.GangDock.Pending.Unloaded);Assert.False(Complete(w));}
        [Fact] public void EachLoadAndUnloadConfirmsOneSequentialPalletWithoutPartialPayment()
        {var w=Loaded();Assert.Equal(7,w.GangDock.Pending.Loaded);Assert.False(w.GangDock.Pending.Ready);Assert.False(Unload(w,1));Unload(w);Assert.Equal(1,w.GangDock.Pending.Unloaded);Assert.False(Complete(w));Assert.Empty(w.GangDock.Accounts);Assert.Equal(10000,w.Bank);Assert.Null(w.GangIllicit);}
        [Fact] public void LoadAndUnloadReplayDoNotIncrementOrSaveTwice()
        {var w=Loaded();Unload(w);int saves=0;Assert.True(Load(w,0,()=>saves++));Assert.True(Unload(w,0,()=>saves++));Assert.Equal(0,saves);Assert.Equal(7,w.GangDock.Pending.Loaded);Assert.Equal(1,w.GangDock.Pending.Unloaded);}
        [Fact] public void AllSevenUnloadedProofsStillNeedExplicitFinalAward()
        {var w=Ready();Assert.True(w.GangDock.Pending.Ready);Assert.Empty(w.GangDock.Accounts);Assert.Null(w.GangAutoLaunder);Assert.False(Load(w,7));Assert.False(Unload(w,7));}
        [Fact] public void FinalAwardAndPendingRemovalShareOneSaveWithoutFinancialMutation()
        {var w=Ready();int saves=0;Assert.True(Complete(w,save:()=>{saves++;Assert.Null(w.GangDock.Pending);Assert.Equal(2,Reward(w).Bonus);}));Assert.Equal(1,saves);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);Assert.Null(w.GangAutoLaunder);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);}
        [Theory] [InlineData(1000,2)] [InlineData(700,2)] [InlineData(699,1.5)] [InlineData(100,1.5)] [InlineData(99,1.25)] [InlineData(0,1.25)]
        public void SourceScaledConditionDeterminesThreeQualityTiers(float body,double expected)
        {var w=Ready(body);Assert.True(Complete(w));Assert.Equal(expected,Reward(w).Bonus);}
        [Fact] public void SourceConditionScalesDamageByOneThirdRatherThanRawBodyPercent()
        {Assert.Equal(100,GangDockRules.Condition(1000));Assert.Equal(90,GangDockRules.Condition(700));Assert.Equal(70,GangDockRules.Condition(100));Assert.Equal(66.66667f,GangDockRules.Condition(0),3);Assert.Equal(100,GangDockRules.Condition(1200));Assert.Equal(0,GangDockRules.Condition(float.NaN));}
        [Fact] public void CompletionReplayNeverCountsTwiceOrExtendsExpiry()
        {var w=Ready();Complete(w);var old=w.GangDock;int saves=0;Assert.True(Complete(w,Utc.AddMinutes(5),()=>saves++));Assert.Same(old,w.GangDock);Assert.Equal(0,saves);Assert.Equal(1,Reward(w).Completed);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void SamplesCannotHealRewindTimeOrClearFaulted()
        {var w=Started();Update(w,body:700);Assert.False(Update(w,elapsed:9999,body:700));Assert.False(Update(w,elapsed:11000,body:701));Assert.True(Update(w,elapsed:11000,body:700,faulted:true));Assert.False(Update(w,elapsed:12000,body:700));Assert.True(w.GangDock.Pending.Failed);}
        [Fact] public void PartialLoadReloadRetainsCountsTimeDamageAndAllSlots()
        {var w=Started();Load(w);Load(w,1);Update(w,elapsed:45000,body:800);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(w.GangDock.Valid);Assert.Equal(2,w.GangDock.Pending.Loaded);Assert.Equal(0,w.GangDock.Pending.Unloaded);Assert.Equal(45000,w.GangDock.Pending.Progress.Elapsed);Assert.Equal(800,w.GangDock.Pending.Progress.BodyHealth);Assert.Equal(7,w.GangDock.Pending.Options.Slots.Count);Assert.True(Load(w,2));}
        [Fact] public void PartialUnloadReloadPreservesRemainingCargoWithoutRepeatingCredit()
        {var w=Loaded();Unload(w);Unload(w,1);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.Equal(5,w.GangDock.Pending.Loaded-w.GangDock.Pending.Unloaded);Assert.False(Complete(w));for(int i=2;i<7;i++)Unload(w,i);Assert.True(Complete(w));Assert.Null(w.GangIllicit);}
        [Fact] public void ReadyReloadFinalizesWithoutNativeHandlesOrTripReplay()
        {var w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(Ready(600)));Assert.True(w.GangDock.Pending.Ready);Assert.True(Complete(w));Assert.Equal(1.5,Reward(w).Bonus);}
        [Fact] public void DeadlineAndExplicitEquipmentFailurePersistAndRequireCancellation()
        {var w=Loaded();Update(w,elapsed:600000);Assert.True(w.GangDock.Pending.Failed);Assert.True(w.GangDock.Valid);Assert.False(Unload(w));Assert.False(Complete(w));Assert.False(Update(w,elapsed:0));Assert.True(Cancel(w));w=Started();Update(w,faulted:true);Assert.False(Load(w));Assert.True(Cancel(w));}
        [Fact] public void FailedBeginRestoresAbsentBook()
        {var w=W();Assert.Throws<InvalidOperationException>(()=>Begin(w,save:()=>throw new InvalidOperationException()));Assert.Null(w.GangDock);}
        [Fact] public void FailedSampleRestoresExactBookAndCondition()
        {var w=Started();var old=w.GangDock;Assert.Throws<InvalidOperationException>(()=>Update(w,body:700,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangDock);Assert.Equal(1000,w.GangDock.Pending.Progress.BodyHealth);}
        [Fact] public void FailedLoadAndUnloadLeavePreviousProofsForRetry()
        {var w=Started();var old=w.GangDock;Assert.Throws<InvalidOperationException>(()=>Load(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangDock);Assert.Equal(0,w.GangDock.Pending.Loaded);w=Loaded();old=w.GangDock;Assert.Throws<InvalidOperationException>(()=>Unload(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangDock);Assert.Equal(0,w.GangDock.Pending.Unloaded);Assert.True(Unload(w));}
        [Fact] public void FailedFinalAwardPreservesReadyWithoutNewBonus()
        {var w=Ready();var old=w.GangDock;Assert.Throws<InvalidOperationException>(()=>Complete(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangDock);Assert.True(w.GangDock.Pending.Ready);Assert.Empty(w.GangDock.Accounts);}
        [Fact] public void CancelIsFreeAndFailedCancelKeepsPhysicalProof()
        {var w=Loaded();Unload(w);var old=w.GangDock;Assert.Throws<InvalidOperationException>(()=>Cancel(w,()=>throw new InvalidOperationException()));Assert.Same(old,w.GangDock);Assert.True(Cancel(w));Assert.Null(w.GangDock.Pending);Assert.Equal(10000,w.Bank);Assert.Empty(w.GangDock.Accounts);Assert.True(Cancel(w));}
        [Fact] public void CancelAnotherContractKeepsPreviousBonusAndWindow()
        {var w=Ready(500);Complete(w);long expiry=Reward(w).ExpiresUtcTicks;Begin(w,"again");Assert.True(GangDockTransactions.Cancel(w,"again",Save,out _));Assert.Equal(1.5,Reward(w).Bonus);Assert.Equal(expiry,Reward(w).ExpiresUtcTicks);}
        [Fact] public void NewRealCompletionRefreshesQualityWithoutStackingDuration()
        {var w=Ready();Complete(w);Begin(w,"again");for(int i=0;i<7;i++)GangDockTransactions.Load(w,S(),A(),"again",i,Save,out _);GangDockTransactions.Update(w,S(),A(),"again",new GangDockProgress{Elapsed=10000,BodyHealth=0},Save,out _);for(int i=0;i<7;i++)GangDockTransactions.Unload(w,S(),A(),"again",i,Save,out _);Assert.True(GangDockTransactions.Complete(w,S(),A(),"again",Utc.AddMinutes(10),Save,out _));Assert.Equal(2,Reward(w).Completed);Assert.Equal(1.25,Reward(w).Bonus);Assert.Equal(Utc.AddMinutes(70).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void BonusIsBoundToUtcGangAreaAndStrictExpiry()
        {var w=Ready(500);Complete(w);var b=w.GangDock;Assert.Equal(1.5,GangDockTransactions.Multiplier(b,S().Gang.ToUpperInvariant(),11,Utc));Assert.Equal(1,GangDockTransactions.Multiplier(b,S().Gang,11,Utc.AddHours(1)));Assert.Equal(1,GangDockTransactions.Multiplier(b,S().Gang,11,Utc.AddTicks(-1)));Assert.Equal(1,GangDockTransactions.Multiplier(b,"Other",11,Utc));Assert.Equal(1,GangDockTransactions.Multiplier(b,S().Gang,10,Utc));Assert.Equal(1,GangDockTransactions.Multiplier(b,S().Gang,11,DateTime.SpecifyKind(Utc,DateTimeKind.Local)));}
        [Fact] public void LegacyQueriesDoNotCreateBookOrBonus()
        {var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":100,\"Future\":42}");Assert.Null(w.GangDock);Assert.False(GangDockTransactions.Blocked(w));Assert.Null(GangDockTransactions.Account(w.GangDock,S().Gang));Assert.Equal(1,GangDockTransactions.Multiplier(w.GangDock,S().Gang,11,Utc));Assert.Null(w.GangDock);}
        [Fact] public void FutureFieldsSurviveSamplesAndLoadUnloadProofs()
        {var w=Started();var j=JObject.FromObject(w);j["Future"]=42;j["GangDock"]["FutureBook"]="keep";j["GangDock"]["Pending"]["Progress"]["FutureScore"]=123;j["GangDock"]["Pending"]["Options"]["Slots"][0]["FutureSlot"]=true;w=j.ToObject<WalletData>();Update(w,body:800);for(int i=0;i<7;i++)Load(w,i);Unload(w);j=JObject.FromObject(w);Assert.Equal(42,(int)j["Future"]);Assert.Equal("keep",(string)j["GangDock"]["FutureBook"]);Assert.Equal(123,(int)j["GangDock"]["Pending"]["Progress"]["FutureScore"]);Assert.True((bool)j["GangDock"]["Pending"]["Options"]["Slots"][0]["FutureSlot"]);}
        [Fact] public void InvalidCountsAndMissingProgressAreProtectedWithoutExceptions()
        {var w=Started();w.GangDock.Pending.Unloaded=1;Assert.False(w.GangDock.Valid);Assert.False(Cancel(w));w.GangDock.Pending.Unloaded=0;w.GangDock.Pending.Progress=null;Assert.False(w.GangDock.Valid);Assert.False(Update(w));}
        [Fact] public void InvalidSlotsAndDeadlineCannotStart()
        {var w=W();var c=new GangDockOptions();c.Slots.Add(c.Slots[0]);Assert.False(c.Valid);Assert.False(Begin(w,c:c));c=new GangDockOptions{SecondsLimit=0};Assert.False(c.Valid);c=new GangDockOptions();c.Slots[0].Y=float.NaN;Assert.False(c.Valid);Assert.Null(w.GangDock);}
        [Fact] public void PendingDockBlocksAllOtherEconomicsWithoutRecursion()
        {var w=Started();Assert.True(GangBulkTransactions.Blocked(w));Assert.False(GangAutoArmsRules.Free(w));Assert.True(GangDockTransactions.OtherFree(w));Assert.False(GangChauffeurTransactions.OtherFree(w));Assert.False(GangTheatreTransactions.OtherFree(w));Assert.False(GangBusinessTransactions.Trade(w,S(),A(),new GangBusinessOptions(),false,Utc,"trade",Save,out _));Assert.False(GangAutoLaunderTransactions.Configure(w,S(),A(),true,"auto",Save,out _));}
        [Fact] public void OtherInvalidBookOrPaidContractBlocksStart()
        {var w=W();w.GangChauffeur=new GangChauffeurData{Accounts=null};Assert.False(Begin(w));w=W();GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"paid",Save,out _);Assert.False(Begin(w));Assert.Equal(9850,w.Bank);}
        [Fact] public void LostOwnershipBlocksLoadUnloadUpdateAwardButNotCancel()
        {var w=Loaded();var s=new GangState{Gang="Other"};Assert.False(GangDockTransactions.Update(w,s,A(),"dock",new GangDockProgress{Elapsed=1000},Save,out _));Assert.False(GangDockTransactions.Load(w,s,A(),"dock",0,Save,out _));Assert.False(GangDockTransactions.Unload(w,s,A(),"dock",0,Save,out _));Assert.False(GangDockTransactions.Complete(w,s,A(),"dock",Utc,Save,out _));Assert.True(Cancel(w));}
        [Fact] public void CapacityOverflowAndInvalidUtcRefuseAwardOrNewStart()
        {var w=Ready();Assert.False(Complete(w,DateTime.SpecifyKind(DateTime.MaxValue,DateTimeKind.Utc)));Complete(w);Reward(w).Completed=long.MaxValue;Assert.False(Begin(w,"again"));w=W();w.GangDock=new GangDockData();for(int n=0;n<640;n++)w.GangDock.Accounts.Add(new GangDockAccount{Gang="Other"+n,Completed=1,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks});Assert.True(w.GangDock.Valid);Assert.False(Begin(w));}
        [Fact] public void SamplesKeepReceiptsBoundedAndDoNotMoveMoney()
        {var w=Started();for(int n=0;n<270;n++)Assert.True(Update(w,elapsed:n));Assert.Equal(256,w.GangDock.Receipts.Count);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);}
        [Fact] public void ReadyProofCannotBeChangedIntoFailedOrResampled()
        {var w=Ready();Assert.False(Update(w));w.GangDock.Pending.Progress.Faulted=true;Assert.False(w.GangDock.Valid);Assert.False(Complete(w));}
        [Fact] public void CustomSmallerCargoListSurvivesReloadWithoutDefaultMerging()
        {var w=W();var c=new GangDockOptions();c.Slots=c.Slots.Take(2).ToList();Begin(w,c:c);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.Equal(2,w.GangDock.Pending.Options.Slots.Count);Load(w);Load(w,1);Unload(w);Assert.False(w.GangDock.Pending.Ready);Unload(w,1);Assert.True(Complete(w));}
        [Fact] public void StationRequiresOwnDriverCorrectCouplingAndBothSpeedsBelowHalfMeter()
        {Assert.True(GangDockRules.Station(true,true,true,5,.49f,.49f,false));Assert.False(GangDockRules.Station(true,false,true,0,0,0,false));Assert.False(GangDockRules.Station(true,true,false,0,0,0,false));Assert.False(GangDockRules.Station(false,true,true,0,0,0,false));Assert.False(GangDockRules.Station(true,true,true,0,.5f,0,false));Assert.False(GangDockRules.Station(true,true,true,0,0,.5f,false));Assert.False(GangDockRules.Station(true,true,true,float.NaN,0,0,false));}
        [Fact] public void LoadingAndUnloadingUseDifferentSourceTrailerCenterRadii()
        {Assert.False(GangDockRules.Station(true,true,true,5.01f,0,0,false));Assert.True(GangDockRules.Station(true,true,true,9,0,0,true));Assert.False(GangDockRules.Station(true,true,true,9.01f,0,0,true));}
        [Theory] [InlineData(false,3000)] [InlineData(true,2000)]
        public void AutomaticTransferRequiresFullSourceInterval(bool unloading,int duration)
        {var c=new GangDockTransfer(0);for(int t=1000;t<duration;t+=1000)Assert.False(c.Step(t,true,unloading));Assert.True(c.Step(duration,true,unloading));}
        [Fact] public void TransferInterruptionOrLongGapResetsUnconfirmedWork()
        {var c=new GangDockTransfer(0);c.Step(1000,true,false);c.Step(2000,false,false);Assert.Equal(0,c.Milliseconds);Assert.False(c.Step(3000,true,false));Assert.False(c.Step(10000,true,false));Assert.Equal(0,c.Milliseconds);}
        [Fact] public void ClockPreservesConditionTimeAndHandlesPauseAndRollover()
        {int start=int.MaxValue-200;var c=new GangDockClock(start,600000,new GangDockProgress{Elapsed=10000,BodyHealth=800});c.Tick(unchecked(start+1000),true,900);Assert.Equal(11000,c.Progress.Elapsed);Assert.Equal(800,c.Progress.BodyHealth);c.Tick(unchecked(start+2000),false,700);c.Tick(unchecked(start+10000),true,700);Assert.Equal(11000,c.Progress.Elapsed);Assert.Equal(800,c.Progress.BodyHealth);c.Tick(unchecked(start+11000),true,700);Assert.Equal(12000,c.Progress.Elapsed);Assert.Equal(700,c.Progress.BodyHealth);}
        [Fact] public void ClockCannotAdvanceBeyondCapturedDeadlineOrHealAfterResume()
        {var c=new GangDockClock(0,600000,new GangDockProgress{Elapsed=599500,BodyHealth=500});c.Tick(1000,true,1000);Assert.Equal(600000,c.Progress.Elapsed);Assert.Equal(500,c.Progress.BodyHealth);}
    }
}
