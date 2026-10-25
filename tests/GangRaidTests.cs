using System;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangRaidTests
    {
        static readonly Action Save=()=>{};static GangTurfDefinition A()=>GangTurfOptions.Defaults().Areas[0];static GangState S()=>new GangState{Gang=A().DefaultOwner,Reputation=42};static DealerOptions C()=>DealerDefaults.Load();
        static WalletData W()=>new WalletData{Bank=10000,Cash=123,Dealer=new DealerData{Stock={{"weed",5}}}};
        static GangRaidAttempt P()=>new GangRaidAttempt{Id="raid",Gang=S().Gang,Rival="Other",Area=A(),Options=new GangRaidOptions(),Site=new GangRaidOptions().Sites[0],Capacity=30,Models=new[]{"g_m_y_famca_01","g_m_y_famca_01","g_m_y_famca_01"},Loot={{"weed",40},{"mdma",50}}};
        static bool Begin(WalletData w,Action save=null)=>GangRaidTransactions.Begin(w,S(),P(),C(),save??Save,out _);
        static WalletData Started(){var w=W();Assert.True(Begin(w));return w;}
        static WalletData Defeated(){var w=Started();for(int i=0;i<3;i++)Assert.True(GangRaidTransactions.Defeat(w,"raid",i,Save,out _));return w;}
        static WalletData Collected(){var w=Defeated();Assert.True(GangRaidTransactions.Collect(w,"raid",Save,out _));return w;}
        static bool Complete(WalletData w,Action save=null)=>GangRaidTransactions.Complete(w,S(),A(),C(),"raid",save??Save,out _);
        [Fact] public void DefaultsAndSnapshotHaveBoundedOwnEncounterAndLoot()
        {var c=new GangRaidOptions();Assert.True(c.Valid);Assert.Equal(3,c.Sites.Count);Assert.Equal(35,c.MinLoot);Assert.Equal(90,c.MaxLoot);Assert.Equal(2,c.SearchSeconds);Assert.True(P().Valid);Assert.Equal(3,P().Models.Length);}
        [Fact] public void BeginDoesNotChargeMoneyCreateStockOrChangeOtherWalletBooks()
        {var w=W();var d=w.Dealer;Assert.True(Begin(w));Assert.Equal(1,w.GangRaid.Attempts);Assert.NotNull(w.GangRaid.Pending);Assert.Null(w.GangWarehouse);Assert.Same(d,w.Dealer);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);}
        [Fact] public void CallerCannotChangeSnapshotLootAreaOrModelsAfterBegin()
        {var w=W();var p=P();GangRaidTransactions.Begin(w,S(),p,C(),Save,out _);p.Loot["weed"]=1;p.Models[0]="changed";p.Area.Name="changed";Assert.Equal(40,w.GangRaid.Pending.Loot["weed"]);Assert.NotEqual("changed",w.GangRaid.Pending.Models[0]);Assert.NotEqual("changed",w.GangRaid.Pending.Area.Name);}
        [Fact] public void AllThreeDistinctDefeatsAndPackageAreRequiredBeforeStock()
        {var w=Started();Assert.False(GangRaidTransactions.Collect(w,"raid",Save,out _));Assert.False(Complete(w));GangRaidTransactions.Defeat(w,"raid",0,Save,out _);GangRaidTransactions.Defeat(w,"raid",1,Save,out _);Assert.False(GangRaidTransactions.Collect(w,"raid",Save,out _));GangRaidTransactions.Defeat(w,"raid",2,Save,out _);Assert.False(Complete(w));Assert.Null(w.GangWarehouse);Assert.True(GangRaidTransactions.Collect(w,"raid",Save,out _));Assert.True(Complete(w));}
        [Fact] public void WrongOrRepeatedDefeatCannotCreateExtraProof()
        {var w=Started();Assert.False(GangRaidTransactions.Defeat(w,"raid",3,Save,out _));Assert.False(GangRaidTransactions.Defeat(w,"other",0,Save,out _));GangRaidTransactions.Defeat(w,"raid",0,Save,out _);Assert.True(GangRaidTransactions.Defeat(w,"raid",0,()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Single(w.GangRaid.Pending.Defeated);}
        [Fact] public void CompletionAddsOnlyTwoSnapshotProductsAndRaidedCounters()
        {var w=Collected();var dealer=w.Dealer;Assert.True(Complete(w));Assert.Null(w.GangRaid.Pending);Assert.Equal(1,w.GangRaid.Completed);Assert.Equal(0,w.GangRaid.Failed);var a=w.GangWarehouse.Accounts[0];Assert.Equal(40,a.Stock["weed"]);Assert.Equal(50,a.Stock["mdma"]);Assert.Equal(40,a.Raided["weed"]);Assert.Empty(a.Deposited);Assert.Empty(a.Supplied);Assert.True(a.Valid);Assert.Same(dealer,w.Dealer);Assert.Equal(5,w.Dealer.Stock["weed"]);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);}
        [Fact] public void CompletionReplayAfterReloadCannotCreditTwice()
        {var w=Collected();Complete(w);w=CharacterRules.Copy(w);Assert.True(Complete(w,()=>throw new Exception()));Assert.Equal(1,w.GangRaid.Completed);Assert.Equal(40,w.GangWarehouse.Accounts[0].Stock["weed"]);Assert.False(GangRaidTransactions.End(w,"raid",true,Save,out _));}
        [Fact] public void PartialRestartRequiresNewProofWithoutRerollOrNewAttempt()
        {var w=Defeated();Assert.True(GangRaidTransactions.Restart(w,"raid",Save,out _));Assert.Empty(w.GangRaid.Pending.Defeated);Assert.Equal(1,w.GangRaid.Attempts);Assert.Equal(40,w.GangRaid.Pending.Loot["weed"]);Assert.False(GangRaidTransactions.Collect(w,"raid",Save,out _));Assert.True(GangRaidTransactions.Defeat(w,"raid",0,Save,out bool changed));Assert.True(changed);}
        [Fact] public void CollectedPackageSurvivesReloadAndCannotRestartFight()
        {var w=CharacterRules.Copy(Collected());Assert.True(w.GangRaid.Pending.Package);Assert.False(GangRaidTransactions.Restart(w,"raid",Save,out _));Assert.True(Complete(w));}
        [Theory][InlineData(false)][InlineData(true)]
        public void CancelOrFailureAfterCollectionNeverGrantsLootOrTakesOldStock(bool failed)
        {var w=Collected();w.GangWarehouse=new GangWarehouseData();w.GangWarehouse.Accounts.Add(new GangWarehouseAccount{Gang=S().Gang,Area=1,Stock={{"weed",5}},Deposited={{"weed",5}}});Assert.True(GangRaidTransactions.End(w,"raid",failed,Save,out _));Assert.Null(w.GangRaid.Pending);Assert.Equal(failed?1:0,w.GangRaid.Failed);Assert.Equal(5,w.GangWarehouse.Accounts[0].Stock["weed"]);Assert.False(Complete(w));Assert.True(GangRaidTransactions.End(w,"raid",failed,()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(10000,w.Bank);}
        [Fact] public void InsufficientWeightCapacityRejectsBeforeAttemptIsCreated()
        {var w=W();var p=P();p.Capacity=1;Assert.False(GangRaidTransactions.Begin(w,S(),p,C(),Save,out _));Assert.Null(w.GangRaid);Assert.Equal(10000,w.Bank);}
        [Fact] public void FullProductCapacityRejectsBeforeAttempt()
        {var w=W();w.GangWarehouse=new GangWarehouseData();w.GangWarehouse.Accounts.Add(new GangWarehouseAccount{Gang=S().Gang,Area=1,Stock={{"weed",990}},Deposited={{"weed",990}}});Assert.False(Begin(w));Assert.Null(w.GangRaid);}
        [Fact] public void MissingOrRivalOwnershipCannotBeginOrReceiveLoot()
        {var w=W();Assert.False(GangRaidTransactions.Begin(w,new GangState(),P(),C(),Save,out _));w=Collected();var s=S();s.Turf=new GangTurfData();s.Turf.Owners[1]="Rival";Assert.False(GangRaidTransactions.Complete(w,s,A(),C(),"raid",Save,out _));s.Gang="Rival";Assert.False(GangRaidTransactions.Complete(w,s,A(),C(),"raid",Save,out _));Assert.Null(w.GangWarehouse);Assert.True(w.GangRaid.Pending.Package);Assert.True(GangRaidTransactions.End(w,"raid",false,Save,out _));}
        [Fact] public void AnotherAreaCannotReceiveSnapshotCargo()
        {var w=Collected();Assert.False(GangRaidTransactions.Complete(w,S(),GangTurfOptions.Defaults().Areas[1],C(),"raid",Save,out _));Assert.True(w.GangRaid.Pending.Package);}
        [Fact] public void ChangedCatalogWeightOrMissingSkuKeepsCollectedCargoUncredited()
        {var w=Collected();var c=C();c.Products.Find(p=>p.Id=="weed").Weight=5;Assert.False(GangRaidTransactions.Complete(w,S(),A(),c,"raid",Save,out _));c=C();c.Products.RemoveAll(p=>p.Id=="weed");Assert.False(GangRaidTransactions.Complete(w,S(),A(),c,"raid",Save,out _));Assert.Null(w.GangWarehouse);Assert.True(w.GangRaid.Pending.Package);}
        [Fact] public void ExistingSourceHistoryAndOtherBooksSurviveRaidCompletion()
        {var w=Collected();w.GangWarehouse=new GangWarehouseData();w.GangWarehouse.Accounts.Add(new GangWarehouseAccount{Gang=S().Gang,Area=1,Stock={{"weed",7}},Deposited={{"weed",5}},Supplied={{"weed",2}}});var dirty=w.GangIllicit=new GangIllicitData();var treasury=w.GangTreasury=new GangTreasuryData();Complete(w);var a=w.GangWarehouse.Accounts[0];Assert.Equal(47,a.Stock["weed"]);Assert.Equal(5,a.Deposited["weed"]);Assert.Equal(2,a.Supplied["weed"]);Assert.Equal(40,a.Raided["weed"]);Assert.Same(dirty,w.GangIllicit);Assert.Same(treasury,w.GangTreasury);}
        [Fact] public void RaidLootCanUseWarehouseSaleAndExistingProcessing()
        {var w=Collected();Complete(w);Assert.True(GangWarehouseTransactions.Sale(w,S(),A(),C(),"weed",1,"DAVIS",false,70,false,"sale",Save,out _));Assert.Equal(39,w.GangWarehouse.Accounts[0].Stock["weed"]);Assert.Equal(40,w.GangWarehouse.Accounts[0].Raided["weed"]);Assert.True(GangIllicitTransactions.Process(w,S(),A(),new GangLaunderingOptions(),70,new DateTime(2013,9,17),"process",Save,out _));Assert.Equal(59,GangTreasuryTransactions.Account(w.GangTreasury,S().Gang).Balance);}
        [Fact] public void FreightAndRaidCannotCreateOverlappingPendingOperations()
        {var w=W();GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"supply",Save,out _);Assert.False(Begin(w));w=Started();Assert.False(GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"supply",Save,out _));Assert.False(GangSupplyTransactions.BeginCriminal(w,S(),A(),C(),new GangWarehouseOptions(),new GangCriminalSupplyOptions(),"weed","supply",Save,out _));Assert.Equal(10000,w.Bank);}
        [Fact] public void PendingRaidBlocksWarehouseAndTerritorialEconomicOperations()
        {var w=Started();Assert.False(GangWarehouseTransactions.Transfer(w,S(),A(),C(),new GangWarehouseOptions(),"weed",1,true,"in",Save,out _));Assert.False(GangIllicitTransactions.Sale(w,S(),A(),C(),"weed",1,"DAVIS",false,70,false,"sale",Save,out _));Assert.False(GangBusinessTransactions.Trade(w,S(),A(),new GangBusinessOptions(),false,DateTime.Now,"business",Save,out _));Assert.Equal(5,w.Dealer.Stock["weed"]);}
        [Fact] public void FailedBeginSaveRestoresAbsentBookWithoutTouchingBank()
        {var w=W();var dealer=w.Dealer;Assert.Throws<Exception>(()=>Begin(w,()=>throw new Exception()));Assert.Null(w.GangRaid);Assert.Same(dealer,w.Dealer);Assert.Equal(10000,w.Bank);}
        [Fact] public void FailedDefeatSaveRetainsOriginalProofBook()
        {var w=Started();var b=w.GangRaid;Assert.Throws<Exception>(()=>GangRaidTransactions.Defeat(w,"raid",0,()=>throw new Exception(),out _));Assert.Same(b,w.GangRaid);Assert.Empty(b.Pending.Defeated);}
        [Fact] public void FailedSearchSaveDoesNotClaimPackage()
        {var w=Defeated();var b=w.GangRaid;Assert.Throws<Exception>(()=>GangRaidTransactions.Collect(w,"raid",()=>throw new Exception(),out _));Assert.Same(b,w.GangRaid);Assert.False(b.Pending.Package);Assert.Equal(3,b.Pending.Defeated.Count);}
        [Fact] public void FailedRestartAndEndPreserveOriginalContractAndCounters()
        {var w=Defeated();var b=w.GangRaid;Assert.Throws<Exception>(()=>GangRaidTransactions.Restart(w,"raid",()=>throw new Exception(),out _));Assert.Same(b,w.GangRaid);Assert.Equal(3,b.Pending.Defeated.Count);Assert.Throws<Exception>(()=>GangRaidTransactions.End(w,"raid",true,()=>throw new Exception(),out _));Assert.Same(b,w.GangRaid);Assert.Equal(0,b.Failed);}
        [Fact] public void FailedCompletionRestoresBothBooksAndCollectedCargo()
        {var w=Collected();var raid=w.GangRaid;var storage=w.GangWarehouse=new GangWarehouseData();Assert.Throws<Exception>(()=>Complete(w,()=>throw new Exception()));Assert.Same(raid,w.GangRaid);Assert.Same(storage,w.GangWarehouse);Assert.True(raid.Pending.Package);Assert.Empty(storage.Accounts);Assert.Equal(0,raid.Completed);}
        [Fact] public void FailedFirstCompletionDoesNotPublishWarehouse()
        {var w=Collected();Assert.Throws<Exception>(()=>Complete(w,()=>throw new Exception()));Assert.Null(w.GangWarehouse);Assert.True(w.GangRaid.Pending.Package);}
        [Fact] public void InvalidProofOrBookIsProtectedWithoutDiscardingContract()
        {var w=Started();w.GangRaid.Pending.Package=true;Assert.False(w.GangRaid.Valid);Assert.False(Complete(w));Assert.False(GangRaidTransactions.End(w,"raid",false,Save,out _));Assert.NotNull(w.GangRaid.Pending);}
        [Fact] public void LifetimeRaidedOverflowRejectsBeforeAnyReward()
        {var w=W();w.GangWarehouse=new GangWarehouseData();w.GangWarehouse.Accounts.Add(new GangWarehouseAccount{Gang=S().Gang,Area=1,Raided={{"weed",long.MaxValue}},Withdrawn={{"weed",long.MaxValue}}});Assert.True(w.GangWarehouse.Valid);Assert.False(Begin(w));Assert.Null(w.GangRaid);}
        [Fact] public void SourceCounterValidationAvoidsOverflowAtLongLimits()
        {var a=new GangWarehouseAccount{Gang="Mine",Area=1,Deposited={{"weed",long.MaxValue}},Raided={{"weed",long.MaxValue}},Withdrawn={{"weed",long.MaxValue}},Sold={{"weed",long.MaxValue}}};Assert.True(a.Valid);a.Stock["weed"]=1;Assert.False(a.Valid);}
        [Fact] public void UnknownMetadataAndLegacyMissingRaidedRemainPreserved()
        {var w=Collected();var j=JObject.FromObject(w);j["Future"]="wallet";j["GangRaid"]["Future"]="book";j["GangRaid"]["Pending"]["Future"]="attempt";w=j.ToObject<WalletData>();Assert.Equal("attempt",(string)JObject.FromObject(w)["GangRaid"]["Pending"]["Future"]);Complete(w);j=JObject.FromObject(w);Assert.Equal("wallet",(string)j["Future"]);Assert.Equal("book",(string)j["GangRaid"]["Future"]);var a=JObject.Parse("{Gang:'Mine',Area:1,Stock:{weed:10},Supplied:{weed:10}}").ToObject<GangWarehouseAccount>();Assert.True(a.Valid);Assert.Empty(a.Raided);}
        [Fact] public void ConfigAndContractReloadDoNotAppendDefaultSites()
        {var c=CharacterRules.Copy(new GangRaidOptions());Assert.True(c.Valid);Assert.Equal(3,c.Sites.Count);var w=CharacterRules.Copy(Started());Assert.Equal(3,w.GangRaid.Pending.Options.Sites.Count);Assert.True(w.GangRaid.Valid);}
        [Fact] public void ProfilesHaveIndependentContractsAndLoot()
        {var w=Collected();var other=W();Assert.False(Complete(other));Assert.Null(other.GangRaid);Complete(w);Assert.Null(other.GangWarehouse);Assert.Equal(40,w.GangWarehouse.Accounts[0].Stock["weed"]);}
        [Fact] public void SearchRequiresElapsedNearBodyTimeAndStopsAfterSuccess()
        {var s=new GangRaidSearch();s.Start(0);Assert.False(s.Step(1000,true,2));Assert.True(s.Step(2000,true,2));Assert.False(s.Searching);Assert.False(s.Step(3000,true,2));}
        [Fact] public void InterruptedSearchAndLongGapsCannotCollectPackage()
        {var s=new GangRaidSearch();s.Start(0);s.Step(1000,true,2);Assert.False(s.Step(2000,false,2));Assert.False(s.Searching);Assert.False(s.Step(3000,true,2));s.Start(0);Assert.False(s.Step(60000,true,2));Assert.False(s.Step(61000,true,2));Assert.True(s.Step(62000,true,2));}
        [Fact] public void SearchHandlesGameTimeWrapWithoutInstantCompletion()
        {var s=new GangRaidSearch();s.Start(int.MaxValue-500);Assert.False(s.Step(unchecked(int.MaxValue+500),true,2));Assert.True(s.Step(unchecked(int.MaxValue+1500),true,2));}
    }
}
