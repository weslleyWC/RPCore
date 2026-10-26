using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangTheatreTests
    {
        static readonly Action Save=()=>{};static readonly DateTime Utc=new DateTime(2026,10,26,12,0,0,DateTimeKind.Utc);
        static GangTurfDefinition A()=>GangTurfOptions.Defaults().Areas.Single(a=>a.Id==9);
        static GangState S()=>new GangState{Gang=A().DefaultOwner};static WalletData W()=>new WalletData{Bank=10000,Cash=123};
        static bool Begin(WalletData w,string id="show",GangTheatreOptions c=null,Action save=null)=>GangTheatreTransactions.Begin(w,S(),A(),c??new GangTheatreOptions(),id,save??Save,out _);
        static bool Collect(WalletData w,int i=0,string id="show",Action save=null)=>GangTheatreTransactions.Collect(w,S(),A(),id,i,save??Save,out _);
        static bool Install(WalletData w,int i=0,string id="show",Action save=null)=>GangTheatreTransactions.Install(w,S(),A(),id,i,save??Save,out _);
        static bool Complete(WalletData w,string id="show",DateTime? utc=null,Action save=null)=>GangTheatreTransactions.Complete(w,S(),A(),id,utc??Utc,save??Save,out _);
        static bool Cancel(WalletData w,string id="show",Action save=null)=>GangTheatreTransactions.Cancel(w,id,save??Save,out _);
        static WalletData Started(){var w=W();Assert.True(Begin(w));return w;}
        static WalletData Ready(){var w=Started();for(int i=0;i<11;i++){Assert.True(Collect(w,i));Assert.True(Install(w,i));}return w;}
        static GangTheatreAccount Reward(WalletData w)=>GangTheatreTransactions.Account(w.GangTheatre,S().Gang);
        [Fact] public void DefaultsMatchElevenSourceEquipmentPiecesAndCrate()
        {var c=new GangTheatreOptions();Assert.True(c.Valid);Assert.Equal(11,c.Items.Count);Assert.Equal("prop_mb_crate_01a",c.CrateModel);Assert.Equal(308.4233f,c.Crate.X);Assert.Equal(197.2859f,c.Crate.Y);Assert.Equal(-88.3205f,c.Crate.Heading);Assert.Equal("prop_studio_light_02",c.Items[0].Model);Assert.Equal(305.6196f,c.Items[0].Point.X);Assert.Equal("Red Carpet",c.Items[10].Name);Assert.Equal("prop_premier_fence_01",c.Items[10].Model);Assert.Equal(343.0669f,c.Items[10].Point.Heading);Assert.Equal(2.3f,c.InteractionDistance);Assert.Equal(60,c.BoostMinutes);}
        [Fact] public void BeginIsFreeWithoutIncomeStockOrAutomaticOptIn()
        {var w=W();w.Bank=0;Assert.True(Begin(w));Assert.Equal(0,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);Assert.Null(w.GangBusiness);Assert.Null(w.GangAutoLaunder);Assert.Empty(w.GangTheatre.Accounts);}
        [Fact] public void StartRequiresOwnedTerritoryNine()
        {var w=W();var a=GangTurfOptions.Defaults().Areas[0];Assert.False(GangTheatreTransactions.Begin(w,new GangState{Gang=a.DefaultOwner},a,new GangTheatreOptions(),"bad",Save,out _));Assert.False(GangTheatreTransactions.Begin(w,new GangState{Gang="Other"},A(),new GangTheatreOptions(),"bad",Save,out _));Assert.Null(w.GangTheatre);}
        [Fact] public void ContractCapturesAllItemsCoordinatesAndOptions()
        {var w=W();var c=new GangTheatreOptions();Begin(w,c:c);c.Crate.X=0;c.Items[0].Point.Y=0;c.Items[0].Model="other";c.Items.RemoveAt(10);c.BoostMinutes=1;Assert.Equal(308.4233f,w.GangTheatre.Pending.Options.Crate.X);Assert.Equal(190.47f,w.GangTheatre.Pending.Options.Items[0].Point.Y);Assert.Equal("prop_studio_light_02",w.GangTheatre.Pending.Options.Items[0].Model);Assert.Equal(11,w.GangTheatre.Pending.Options.Items.Count);Assert.Equal(60,w.GangTheatre.Pending.Options.BoostMinutes);}
        [Fact] public void CannotInstallWithoutCollectingOrSkipEquipmentOrder()
        {var w=Started();Assert.False(Install(w));Assert.False(Collect(w,1));Assert.True(Collect(w));Assert.False(Install(w,1));Assert.Equal(0,w.GangTheatre.Pending.Installed);Assert.True(w.GangTheatre.Pending.Carrying);}
        [Fact] public void CollectionDoesNotAdvanceInstalledCountOrGivePartialReward()
        {var w=Started();Assert.True(Collect(w));Assert.Equal(0,w.GangTheatre.Pending.Installed);Assert.True(w.GangTheatre.Pending.Carrying);Assert.False(Complete(w));Assert.Empty(w.GangTheatre.Accounts);Assert.Null(w.GangIllicit);}
        [Fact] public void CollectReplayDoesNotSaveOrDuplicateCrates()
        {var w=Started();Collect(w);int saves=0;Assert.True(GangTheatreTransactions.Collect(w,S(),A(),"show",0,()=>saves++,out bool changed));Assert.False(changed);Assert.Equal(0,saves);Assert.True(w.GangTheatre.Pending.Carrying);Assert.Equal(0,w.GangTheatre.Pending.Installed);}
        [Fact] public void InstallationConsumesCarriedProofAndMovesToNextPiece()
        {var w=Started();Collect(w);Assert.True(Install(w));Assert.Equal(1,w.GangTheatre.Pending.Installed);Assert.False(w.GangTheatre.Pending.Carrying);Assert.False(Install(w,1));Assert.True(Collect(w,1));}
        [Fact] public void InstallReplayDoesNotAdvanceOrSaveTwice()
        {var w=Started();Collect(w);Install(w);int saves=0;Assert.True(GangTheatreTransactions.Install(w,S(),A(),"show",0,()=>saves++,out bool changed));Assert.False(changed);Assert.Equal(0,saves);Assert.Equal(1,w.GangTheatre.Pending.Installed);Assert.False(w.GangTheatre.Pending.Carrying);}
        [Fact] public void ElevenDistinctOrderedInstallationsRequiredAndFinalConfirmationIsSeparate()
        {var w=Started();for(int i=0;i<10;i++){Collect(w,i);Install(w,i);}Assert.False(Complete(w));Collect(w,10);Assert.False(Complete(w));Install(w,10);Assert.True(w.GangTheatre.Pending.Ready);Assert.Empty(w.GangTheatre.Accounts);Assert.Null(w.GangAutoLaunder);}
        [Fact] public void AtomicFinalAwardDoesNotCreditMoneyOrStock()
        {var w=Ready();int saves=0;Assert.True(Complete(w,save:()=>{saves++;Assert.Null(w.GangTheatre.Pending);Assert.Equal(1,Reward(w).Completed);}));Assert.Equal(1,saves);Assert.Equal(Utc.Ticks,Reward(w).StartedUtcTicks);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangTreasury);Assert.Null(w.GangIllicit);Assert.Null(w.GangAutoLaunder);}
        [Fact] public void CompletionReplayDoesNotExtendBonus()
        {var w=Ready();Complete(w);var old=w.GangTheatre;int saves=0;Assert.True(Complete(w,utc:Utc.AddMinutes(5),save:()=>saves++));Assert.Same(old,w.GangTheatre);Assert.Equal(0,saves);Assert.Equal(1,Reward(w).Completed);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void CollectedCurrentPieceAndPreviousInstallationsSurviveReload()
        {var w=Started();Collect(w);Install(w);Collect(w,1);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(w.GangTheatre.Valid);Assert.Equal(1,w.GangTheatre.Pending.Installed);Assert.True(w.GangTheatre.Pending.Carrying);Assert.True(Install(w,1));Assert.Equal(2,w.GangTheatre.Pending.Installed);}
        [Fact] public void ReadyProofSurvivesReloadWithoutAnyNativeHandles()
        {var w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(Ready()));Assert.True(w.GangTheatre.Pending.Ready);Assert.True(Complete(w));Assert.Null(w.GangTheatre.Pending);}
        [Fact] public void FailedBeginRestoresAbsentBook()
        {var w=W();Assert.Throws<InvalidOperationException>(()=>Begin(w,save:()=>throw new InvalidOperationException()));Assert.Null(w.GangTheatre);}
        [Fact] public void FailedCollectRestoresProgressAndAllowsRetry()
        {var w=Started();var old=w.GangTheatre;Assert.Throws<InvalidOperationException>(()=>Collect(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangTheatre);Assert.False(w.GangTheatre.Pending.Carrying);Assert.True(Collect(w));}
        [Fact] public void FailedInstallKeepsCarriedEquipmentAndExactBook()
        {var w=Started();Collect(w);var old=w.GangTheatre;Assert.Throws<InvalidOperationException>(()=>Install(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangTheatre);Assert.True(w.GangTheatre.Pending.Carrying);Assert.Equal(0,w.GangTheatre.Pending.Installed);Assert.True(Install(w));}
        [Fact] public void FailedAwardKeepsReadyProofWithoutNewBonus()
        {var w=Ready();var old=w.GangTheatre;Assert.Throws<InvalidOperationException>(()=>Complete(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangTheatre);Assert.True(w.GangTheatre.Pending.Ready);Assert.Empty(w.GangTheatre.Accounts);Assert.True(Complete(w));}
        [Fact] public void CancelPartialIsFreeAndDoesNotAward()
        {var w=Started();Collect(w);Assert.True(Cancel(w));Assert.Null(w.GangTheatre.Pending);Assert.Empty(w.GangTheatre.Accounts);Assert.Equal(10000,w.Bank);Assert.Null(w.GangIllicit);Assert.True(Cancel(w));}
        [Fact] public void FailedCancelRestoresCarriedProof()
        {var w=Started();Collect(w);var old=w.GangTheatre;Assert.Throws<InvalidOperationException>(()=>Cancel(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangTheatre);Assert.True(w.GangTheatre.Pending.Carrying);}
        [Fact] public void CancelAnotherContractPreservesPriorBonusWindow()
        {var w=Ready();Complete(w);long expiry=Reward(w).ExpiresUtcTicks;Begin(w,"again");Collect(w,id:"again");Cancel(w,"again");Assert.Equal(expiry,Reward(w).ExpiresUtcTicks);Assert.Equal(1,Reward(w).Completed);}
        [Fact] public void NewRealCompletionRefreshesInsteadOfStackingDuration()
        {var w=Ready();Complete(w);Begin(w,"again");for(int i=0;i<11;i++){Collect(w,i,"again");Install(w,i,"again");}Complete(w,"again",Utc.AddMinutes(10));Assert.Equal(2,Reward(w).Completed);Assert.Equal(Utc.AddMinutes(70).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void BonusIsBoundToUtcGangAreaAndStrictExpiry()
        {var w=Ready();Complete(w);var b=w.GangTheatre;Assert.Equal(2,GangTheatreTransactions.Multiplier(b,S().Gang.ToUpperInvariant(),9,Utc));Assert.Equal(2,GangTheatreTransactions.Multiplier(b,S().Gang,9,Utc.AddMinutes(59)));Assert.Equal(1,GangTheatreTransactions.Multiplier(b,S().Gang,9,Utc.AddHours(1)));Assert.Equal(1,GangTheatreTransactions.Multiplier(b,S().Gang,9,Utc.AddTicks(-1)));Assert.Equal(1,GangTheatreTransactions.Multiplier(b,"Other",9,Utc));Assert.Equal(1,GangTheatreTransactions.Multiplier(b,S().Gang,8,Utc));Assert.Equal(1,GangTheatreTransactions.Multiplier(b,S().Gang,9,DateTime.SpecifyKind(Utc,DateTimeKind.Local)));}
        [Fact] public void LostOwnershipBlocksInstallCollectAndAwardButNotCancellation()
        {var w=Started();Collect(w);var s=new GangState{Gang="Other"};Assert.False(GangTheatreTransactions.Collect(w,s,A(),"show",0,Save,out _));Assert.False(GangTheatreTransactions.Install(w,s,A(),"show",0,Save,out _));Assert.False(GangTheatreTransactions.Complete(w,s,A(),"show",Utc,Save,out _));Assert.True(Cancel(w));}
        [Fact] public void LegacyQueriesDoNotCreateBookOrBonus()
        {var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":100,\"Future\":42}");Assert.Null(w.GangTheatre);Assert.False(GangTheatreTransactions.Blocked(w));Assert.Null(GangTheatreTransactions.Account(w.GangTheatre,S().Gang));Assert.Equal(1,GangTheatreTransactions.Multiplier(w.GangTheatre,S().Gang,9,Utc));Assert.Null(w.GangTheatre);}
        [Fact] public void FutureFieldsSurviveCollectAndInstall()
        {var w=Started();var j=JObject.FromObject(w);j["Future"]=42;j["GangTheatre"]["FutureBook"]="keep";j["GangTheatre"]["Pending"]["FuturePending"]="keep";j["GangTheatre"]["Pending"]["Options"]["Items"][0]["FutureItem"]=true;j["GangTheatre"]["Pending"]["Options"]["Items"][0]["Point"]["FuturePoint"]=123;w=j.ToObject<WalletData>();Collect(w);Install(w);j=JObject.FromObject(w);Assert.Equal(42,(int)j["Future"]);Assert.Equal("keep",(string)j["GangTheatre"]["FutureBook"]);Assert.Equal("keep",(string)j["GangTheatre"]["Pending"]["FuturePending"]);Assert.True((bool)j["GangTheatre"]["Pending"]["Options"]["Items"][0]["FutureItem"]);Assert.Equal(123,(int)j["GangTheatre"]["Pending"]["Options"]["Items"][0]["Point"]["FuturePoint"]);}
        [Fact] public void InvalidInstalledCountAndImpossibleCarryAreProtected()
        {var w=Started();w.GangTheatre.Pending.Installed=12;Assert.False(w.GangTheatre.Valid);Assert.False(Cancel(w));w.GangTheatre.Pending.Installed=11;w.GangTheatre.Pending.Carrying=true;Assert.False(w.GangTheatre.Valid);Assert.False(Complete(w));w.GangTheatre.Pending.Installed=-1;Assert.False(w.GangTheatre.Valid);}
        [Fact] public void InvalidOptionsDuplicateItemsAndDistantPointsCannotStart()
        {var w=W();var c=new GangTheatreOptions();c.Items.Add(c.Items[0]);Assert.False(c.Valid);Assert.False(Begin(w,c:c));c=new GangTheatreOptions{InteractionDistance=float.NaN};Assert.False(c.Valid);c=new GangTheatreOptions();c.Items[0].Point.X+=1000;Assert.False(c.Valid);c=new GangTheatreOptions{HandoverMilliseconds=0};Assert.False(c.Valid);Assert.Null(w.GangTheatre);}
        [Fact] public void InteractionRequiresSafeFootAndFiniteDistanceWithinSourceRadius()
        {var c=new GangTheatreOptions();Assert.True(GangTheatreRules.Interact(true,true,2.3f,c));Assert.False(GangTheatreRules.Interact(true,true,2.31f,c));Assert.False(GangTheatreRules.Interact(false,true,1,c));Assert.False(GangTheatreRules.Interact(true,false,1,c));Assert.False(GangTheatreRules.Interact(true,true,float.NaN,c));Assert.False(GangTheatreRules.Interact(true,true,-1,c));}
        [Fact] public void WrongIdOrIndexCannotCreateInstallationProof()
        {var w=Started();Assert.False(Collect(w,-1));Assert.False(Collect(w,30));Assert.False(Collect(w,id:"other"));Collect(w);Assert.False(Install(w,11));Assert.False(Install(w,id:"other"));Assert.Equal(0,w.GangTheatre.Pending.Installed);}
        [Fact] public void TheatrePendingBlocksOtherEconomicsWithoutRecursiveValidation()
        {var w=Started();Assert.True(GangBulkTransactions.Blocked(w));Assert.False(GangAutoArmsRules.Free(w));Assert.True(GangTheatreTransactions.OtherFree(w));Assert.False(GangMowingTransactions.OtherFree(w));Assert.False(GangPopUpTransactions.OtherFree(w));Assert.False(GangMedicalTransactions.OtherFree(w));Assert.False(GangBusinessTransactions.Trade(w,S(),A(),new GangBusinessOptions(),false,Utc,"trade",Save,out _));Assert.False(GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"supply",Save,out _));Assert.False(GangAutoLaunderTransactions.Configure(w,S(),A(),true,"auto",Save,out _));w=W();w.GangTheatre=new GangTheatreData{Accounts=null};Assert.True(GangBulkTransactions.Blocked(w));Assert.False(Begin(w));}
        [Fact] public void AnotherPendingOrInvalidBookPreventsTheatreStart()
        {var w=W();w.GangMowing=new GangMowingData{Accounts=null};Assert.False(Begin(w));Assert.Null(w.GangTheatre);w=W();GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"paid",Save,out _);Assert.False(Begin(w));Assert.Equal(9850,w.Bank);}
        [Fact] public void HistoryOverflowAndAccountCapacityRefuseNewContract()
        {var w=Ready();Complete(w);Reward(w).Completed=long.MaxValue;Assert.False(Begin(w,"again"));w=W();w.GangTheatre=new GangTheatreData();for(int n=0;n<640;n++)w.GangTheatre.Accounts.Add(new GangTheatreAccount{Gang="Other"+n,Completed=1,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks});Assert.True(w.GangTheatre.Valid);Assert.False(Begin(w));}
        [Fact] public void ReceiptsAreBoundedAfterFinalAward()
        {var w=Ready();w.GangTheatre.Receipts=Enumerable.Range(0,256).Select(n=>"old"+n).ToList();Assert.True(Complete(w));Assert.Equal(256,w.GangTheatre.Receipts.Count);Assert.DoesNotContain("old0",w.GangTheatre.Receipts);}
        [Fact] public void InvalidUtcAndDateOverflowCannotAward()
        {var w=Ready();Assert.False(Complete(w,utc:DateTime.SpecifyKind(Utc,DateTimeKind.Unspecified)));Assert.False(Complete(w,utc:DateTime.SpecifyKind(DateTime.MaxValue,DateTimeKind.Utc)));Assert.True(w.GangTheatre.Pending.Ready);Assert.Empty(w.GangTheatre.Accounts);}
        [Fact] public void ExistingMowingBonusAndDirtyIncomeRemainUnchanged()
        {var w=Started();w.GangMowing=new GangMowingData{Accounts={new GangMowingAccount{Gang=S().Gang,Completed=1,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks}}};w.GangIllicit=new GangIllicitData{Accounts={new GangIllicitAccount{Gang=S().Gang,Area=7,Dirty=400,Proceeds=400}}};string mowing=JsonConvert.SerializeObject(w.GangMowing),dirty=JsonConvert.SerializeObject(w.GangIllicit);for(int i=0;i<11;i++){Collect(w,i);Install(w,i);}Complete(w);Assert.Equal(mowing,JsonConvert.SerializeObject(w.GangMowing));Assert.Equal(dirty,JsonConvert.SerializeObject(w.GangIllicit));Assert.Equal(2,GangMowingTransactions.Multiplier(w.GangMowing,S().Gang,8,Utc));Assert.Equal(2,GangTheatreTransactions.Multiplier(w.GangTheatre,S().Gang,9,Utc));}
        [Fact] public void SmallerFutureCatalogUsesSnapshotWithoutDefaultListMerging()
        {var w=W();var c=new GangTheatreOptions();c.Items=c.Items.Take(2).ToList();Assert.True(Begin(w,c:c));w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.Equal(2,w.GangTheatre.Pending.Options.Items.Count);Collect(w);Install(w);Assert.False(w.GangTheatre.Pending.Ready);Collect(w,1);Install(w,1);Assert.True(Complete(w));}
        [Fact] public void AlreadyActiveIplIsBorrowedAndNeverRemoved()
        {var lease=new GangTheatreIplLease();Assert.False(lease.Celebrate(true,0));Assert.False(lease.Owned);for(int t=1000;t<=700000;t+=1000)Assert.False(lease.Tick(t,true));Assert.False(lease.Release());}
        [Fact] public void InactiveIplIsOwnedAndRemovedOnceAfterTenActiveMinutes()
        {var lease=new GangTheatreIplLease();Assert.True(lease.Celebrate(false,0));Assert.True(lease.Owned);for(int t=1000;t<600000;t+=1000)Assert.False(lease.Tick(t,true));Assert.True(lease.Tick(600000,true));Assert.False(lease.Owned);Assert.False(lease.Tick(601000,true));Assert.False(lease.Release());}
        [Fact] public void IplClockDoesNotCountPauseOrLongTimeGaps()
        {var lease=new GangTheatreIplLease();lease.Celebrate(false,0);Assert.False(lease.Tick(500000,false));Assert.False(lease.Tick(1000000,true));for(int t=1001000;t<1600000;t+=1000)Assert.False(lease.Tick(t,true));Assert.True(lease.Tick(1600000,true));}
        [Fact] public void RenewingOwnSceneRestartsTenMinutesWithoutDuplicateRequest()
        {var lease=new GangTheatreIplLease();lease.Celebrate(false,0);for(int t=1000;t<=300000;t+=1000)Assert.False(lease.Tick(t,true));Assert.False(lease.Celebrate(true,300000));for(int t=301000;t<900000;t+=1000)Assert.False(lease.Tick(t,true));Assert.True(lease.Tick(900000,true));}
        [Fact] public void ReleasingOwnedSceneIsIdempotentAndBorrowedSceneIsUntouched()
        {var lease=new GangTheatreIplLease();lease.Celebrate(false,0);Assert.True(lease.Release());Assert.False(lease.Release());Assert.False(lease.Tick(1000,true));Assert.False(lease.Celebrate(true,2000));Assert.False(lease.Release());}
        [Fact] public void IplClockHandlesGameTimeRolloverWithoutEarlyExpiry()
        {var lease=new GangTheatreIplLease();int now=int.MaxValue-500;lease.Celebrate(false,now);for(int n=0;n<599;n++){now=unchecked(now+1000);Assert.False(lease.Tick(now,true));}now=unchecked(now+1000);Assert.True(lease.Tick(now,true));}
    }
}
