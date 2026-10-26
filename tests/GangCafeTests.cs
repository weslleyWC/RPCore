using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangCafeTests
    {
        static readonly Action Save=()=>{};static readonly DateTime Utc=new DateTime(2026,10,25,12,0,0,DateTimeKind.Utc);
        static GangTurfDefinition Area()=>GangTurfOptions.Defaults().Areas.Single(a=>a.Id==4);
        static GangState State()=>new GangState{Gang=Area().DefaultOwner};
        static WalletData W()=>new WalletData{Bank=10000,Cash=123};
        static bool Begin(WalletData w,string id="cafe",Action save=null,GangCafeOptions c=null)=>GangCafeTransactions.Begin(w,State(),Area(),c??new GangCafeOptions(),id,save??Save,out _);
        static bool Complete(WalletData w,DateTime? now=null,string id="cafe",Action save=null)=>GangCafeTransactions.Complete(w,State(),Area(),id,now??Utc,save??Save,out _);
        static WalletData Ready(string id="cafe",GangCafeOptions c=null)
        {var w=W();Assert.True(Begin(w,id,c:c));for(int i=0;i<3;i++)Assert.True(GangSupplyTransactions.Load(w,id,i,Save,out _));for(int n=1;n<=3;n++)Assert.True(GangSupplyTransactions.Deliver(w,id,n,Save,out _));return w;}
        static GangCafeAccount Bonus(WalletData w)=>GangCafeTransactions.Account(w.GangCafe,State().Gang,4);
        [Fact] public void DefaultsMatchReferenceThreePickupsDoorParkingAndHour()
        {var c=new GangCafeOptions();Assert.True(c.Valid);Assert.Equal(60,c.BoostMinutes);Assert.Equal(3,c.Route.Suppliers.Count);Assert.Equal(55.15895f,c.Route.Suppliers[0].X);Assert.Equal(-854.6947f,c.Route.Suppliers[1].Y);Assert.Equal(40.10313f,c.Route.Suppliers[2].Z);Assert.Equal(-1270.415f,c.Parking.X);Assert.Equal(-1200.205f,c.Door.Y);}
        [Fact] public void BeginIsFreeWithoutStockIncomeOrBonusAndDoesNotEnableProcessing()
        {var w=W();w.Bank=0;Assert.True(Begin(w));Assert.Equal(0,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangCafe);Assert.Null(w.GangTreasury);Assert.Null(w.GangAutoLaunder);Assert.Empty(w.GangBusiness.Accounts);Assert.Equal(GangSupplyCargo.Cafe,w.GangBusiness.Supply.CargoKind);Assert.Equal(0,w.GangBusiness.Supply.Cost);}
        [Fact] public void RequiresOwnedTerritoryFourAndValidOptions()
        {var w=W();var a=GangTurfOptions.Defaults().Areas[0];Assert.False(GangCafeTransactions.Begin(w,new GangState{Gang=a.DefaultOwner},a,new GangCafeOptions(),"x",Save,out _));Assert.False(GangCafeTransactions.Begin(w,new GangState{Gang="Other"},Area(),new GangCafeOptions(),"x",Save,out _));Assert.False(Begin(w,c:new GangCafeOptions{BoostMinutes=0}));Assert.Null(w.GangBusiness);}
        [Fact] public void SnapshotCannotBeChangedThroughCallerOptionsOrArea()
        {var w=W();var c=new GangCafeOptions();Assert.True(Begin(w,c:c));c.BoostMinutes=1;c.Route.Suppliers.Clear();c.Door.X=100;Assert.Equal(60,w.GangBusiness.Supply.Cafe.BoostMinutes);Assert.Equal(3,w.GangBusiness.Supply.Options.Suppliers.Count);Assert.Equal(-1275.385f,w.GangBusiness.Supply.Cafe.Door.X);}
        [Fact] public void PartialCargoCannotAwardAndDistinctSequentialCheckpointsAreRequired()
        {var w=W();Begin(w);Assert.False(Complete(w));Assert.False(GangSupplyTransactions.Deliver(w,"cafe",1,Save,out _));GangSupplyTransactions.Load(w,"cafe",0,Save,out _);GangSupplyTransactions.Load(w,"cafe",0,Save,out _);Assert.Single(w.GangBusiness.Supply.Loaded);Assert.False(Complete(w));Assert.Null(w.GangCafe);}
        [Fact] public void ThreeDeliveriesAndFinalAwardAreOneAtomicSave()
        {var w=Ready();int saves=0;Assert.True(Complete(w,save:()=>{saves++;Assert.Null(w.GangBusiness.Supply);Assert.Equal(1,Bonus(w).Completed);}));Assert.Equal(1,saves);Assert.Equal(Utc.Ticks,Bonus(w).StartedUtcTicks);Assert.Equal(Utc.AddHours(1).Ticks,Bonus(w).ExpiresUtcTicks);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Empty(w.GangBusiness.Accounts);Assert.Null(w.GangTreasury);Assert.Null(w.GangAutoLaunder);}
        [Fact] public void OrdinarySupplyCompletionCannotMisclassifyCafeAsPurchasedGoods()
        {var w=Ready();Assert.False(GangSupplyTransactions.Complete(w,State(),Area(),"cafe",Save,out _));Assert.True(w.GangBusiness.Supply.Ready);Assert.Empty(w.GangBusiness.Accounts);Assert.Null(w.GangCafe);}
        [Fact] public void ReplayCannotRepeatOrExtendBonusOrSave()
        {var w=Ready();Complete(w);var book=w.GangCafe;Assert.True(Complete(w,Utc.AddMinutes(15),save:()=>throw new Exception()));Assert.Same(book,w.GangCafe);Assert.Equal(Utc.AddHours(1).Ticks,Bonus(w).ExpiresUtcTicks);Assert.Equal(1,Bonus(w).Completed);}
        [Fact] public void RestartPartialResetsProofsButKeepsRouteAndNoReward()
        {var w=W();Begin(w);GangSupplyTransactions.Load(w,"cafe",0,Save,out _);Assert.True(GangSupplyTransactions.Restart(w,"cafe",Save,out _));Assert.Empty(w.GangBusiness.Supply.Loaded);Assert.True(GangSupplyTransactions.Load(w,"cafe",0,Save,out bool changed));Assert.True(changed);Assert.Null(w.GangCafe);Assert.Equal(GangSupplyCargo.Cafe,w.GangBusiness.Supply.CargoKind);}
        [Fact] public void ReadyRestartIsRefusedWithoutErasingFinalDeliveryProof()
        {var w=Ready();Assert.False(GangSupplyTransactions.Restart(w,"cafe",Save,out _));Assert.True(w.GangBusiness.Supply.Ready);Assert.True(Complete(w));}
        [Fact] public void CancelIsFreeAndAvailableWithoutOwnershipAndNeverAwards()
        {var w=Ready();Assert.True(GangSupplyTransactions.Refund(w,"cafe",Save,out _));Assert.Equal(10000,w.Bank);Assert.Null(w.GangBusiness.Supply);Assert.Null(w.GangCafe);Assert.False(Complete(w));Assert.True(GangSupplyTransactions.Refund(w,"cafe",()=>throw new Exception(),out bool changed));Assert.False(changed);}
        [Fact] public void CancellingAnotherRunPreservesPreviouslyEarnedExpiry()
        {var w=Ready();Complete(w);var old=w.GangCafe;Assert.True(Begin(w,"second"));Assert.True(GangSupplyTransactions.Refund(w,"second",Save,out _));Assert.Same(old,w.GangCafe);Assert.Equal(1,Bonus(w).Completed);}
        [Fact] public void AnotherCompletedRouteRefreshesHourRatherThanStacking()
        {var w=Ready();Complete(w);Begin(w,"second");for(int i=0;i<3;i++)GangSupplyTransactions.Load(w,"second",i,Save,out _);for(int n=1;n<=3;n++)GangSupplyTransactions.Deliver(w,"second",n,Save,out _);Assert.True(Complete(w,Utc.AddMinutes(10),"second"));Assert.Equal(2,Bonus(w).Completed);Assert.Equal(Utc.AddMinutes(70).Ticks,Bonus(w).ExpiresUtcTicks);}
        [Fact] public void LostOwnerOrGangCannotCompleteButKeepsReadyCargo()
        {var w=Ready();Assert.False(GangCafeTransactions.Complete(w,new GangState{Gang="Other"},Area(),"cafe",Utc,Save,out _));Assert.True(w.GangBusiness.Supply.Ready);Assert.Null(w.GangCafe);}
        [Fact] public void BeginFailureRestoresOriginalBookAndAllBalances()
        {var w=W();Assert.Throws<Exception>(()=>Begin(w,save:()=>throw new Exception()));Assert.Null(w.GangBusiness);Assert.Null(w.GangCafe);Assert.Equal(10000,w.Bank);}
        [Fact] public void CompletionFailureRestoresBothOriginalBooksAndReadyRoute()
        {var w=Ready();var business=w.GangBusiness;Assert.Throws<Exception>(()=>Complete(w,save:()=>throw new Exception()));Assert.Same(business,w.GangBusiness);Assert.Null(w.GangCafe);Assert.True(w.GangBusiness.Supply.Ready);Assert.True(Complete(w));}
        [Fact] public void RefreshFailureKeepsOriginalEarnedBonusAndReadyRoute()
        {var w=Ready();Complete(w);var original=w.GangCafe;Begin(w,"second");for(int i=0;i<3;i++)GangSupplyTransactions.Load(w,"second",i,Save,out _);for(int n=1;n<=3;n++)GangSupplyTransactions.Deliver(w,"second",n,Save,out _);var business=w.GangBusiness;Assert.Throws<Exception>(()=>Complete(w,Utc.AddMinutes(10),"second",()=>throw new Exception()));Assert.Same(original,w.GangCafe);Assert.Same(business,w.GangBusiness);}
        [Fact] public void ExpiryUsesUtcAndDoesNotProcessOfflineOrCreateBooks()
        {var w=Ready();Complete(w);Assert.Equal(2,GangCafeTransactions.Multiplier(w.GangCafe,State().Gang,4,Utc));Assert.Equal(2,GangCafeTransactions.Multiplier(w.GangCafe,State().Gang,4,Utc.AddHours(1).AddTicks(-1)));Assert.Equal(1,GangCafeTransactions.Multiplier(w.GangCafe,State().Gang,4,Utc.AddHours(1)));Assert.Equal(1,GangCafeTransactions.Multiplier(w.GangCafe,State().Gang,4,Utc.AddDays(2)));Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);}
        [Fact] public void BackwardClockBeforeAwardAndNonUtcCannotActivateBonus()
        {var w=Ready();Complete(w);Assert.Equal(1,GangCafeTransactions.Multiplier(w.GangCafe,State().Gang,4,Utc.AddTicks(-1)));Assert.Equal(1,GangCafeTransactions.Multiplier(w.GangCafe,State().Gang,4,DateTime.SpecifyKind(Utc,DateTimeKind.Unspecified)));}
        [Fact] public void BonusIsIsolatedByAreaGangAndProfile()
        {var w=Ready();Complete(w);Assert.Equal(1,GangCafeTransactions.Multiplier(w.GangCafe,"Other",4,Utc));Assert.Equal(1,GangCafeTransactions.Multiplier(w.GangCafe,State().Gang,1,Utc));Assert.Equal(1,GangCafeTransactions.Multiplier(W().GangCafe,State().Gang,4,Utc));Assert.Equal(2,GangCafeTransactions.Multiplier(w.GangCafe,State().Gang.ToUpperInvariant(),4,Utc));}
        [Fact] public void LegacyOrdersMissingKindStayLegalAndLegacyWalletDoesNotEnableBonus()
        {var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":10000,\"Future\":42}");Assert.Null(w.GangCafe);Assert.Equal(1,GangCafeTransactions.Multiplier(w.GangCafe,"Any",4,Utc));Assert.Equal(GangSupplyCargo.Legal,JsonConvert.DeserializeObject<GangSupplyOrder>("{}").CargoKind);Assert.Null(w.GangBusiness);}
        [Fact] public void UnknownFieldsRoundTripAcrossAwardAndRefreshBooks()
        {var w=Ready();var obj=JObject.Parse(JsonConvert.SerializeObject(w));obj["Future"]=42;obj["GangBusiness"]["FutureBook"]="keep";obj["GangBusiness"]["Supply"]["Cafe"]["FutureSnapshot"]="keep";w=obj.ToObject<WalletData>();Assert.True(Complete(w));var result=JObject.Parse(JsonConvert.SerializeObject(w));Assert.Equal(42,(int)result["Future"]);Assert.Equal("keep",(string)result["GangBusiness"]["FutureBook"]);var roundtrip=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.Equal(2,GangCafeTransactions.Multiplier(roundtrip.GangCafe,State().Gang,4,Utc));}
        [Fact] public void InvalidBooksAreProtectedAndQueriesDoNotCreatePreferences()
        {var w=W();w.GangCafe=new GangCafeData{Accounts=null};Assert.False(Begin(w));Assert.Null(w.GangBusiness);Assert.Equal(1,GangCafeTransactions.Multiplier(w.GangCafe,State().Gang,4,Utc));Assert.Null(GangCafeTransactions.Account(null,State().Gang,4));}
        [Fact] public void InvalidOptionsAndRewardTimestampsCannotBeSaved()
        {Assert.False(new GangCafeOptions{BoostMinutes=181}.Valid);Assert.False(new GangCafeOptions{Door=new JobPoint{Name="Far",X=1000}}.Valid);Assert.False(new GangCafeOptions{Route=null}.Valid);var w=Ready();Assert.False(Complete(w,DateTime.MaxValue));Assert.False(Complete(w,new DateTime(DateTime.MaxValue.Ticks,DateTimeKind.Utc)));Assert.Null(w.GangCafe);Assert.True(w.GangBusiness.Supply.Ready);}
        [Fact] public void BonusHistoryOverflowRefusesWithoutRemovingReadyCargo()
        {var w=Ready();w.GangCafe=new GangCafeData();w.GangCafe.Accounts.Add(new GangCafeAccount{Gang=State().Gang,Area=4,Completed=long.MaxValue,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks});Assert.True(w.GangCafe.Valid);Assert.False(Complete(w));Assert.True(w.GangBusiness.Supply.Ready);}
        [Fact] public void CafeSharesPendingSlotAndBlocksTradeAndAutomaticModes()
        {var w=W();Begin(w);Assert.False(GangAutoArmsRules.Free(w));Assert.False(Begin(w,"other"));Assert.False(GangSupplyTransactions.Begin(w,State(),Area(),new GangBusinessOptions(),new GangSupplyOptions(),"legal",Save,out _));Assert.False(GangBusinessTransactions.Trade(w,State(),Area(),new GangBusinessOptions(),false,Utc,"buy",Save,out _));Assert.Equal(10000,w.Bank);}
        [Fact] public void ExistingPaidFreightCannotBeOverwrittenByCafe()
        {var w=W();Assert.True(GangSupplyTransactions.Begin(w,State(),Area(),new GangBusinessOptions(),new GangSupplyOptions(),"legal",Save,out _));var original=w.GangBusiness;Assert.False(Begin(w));Assert.Same(original,w.GangBusiness);Assert.Equal(9850,w.Bank);Assert.Equal("legal",w.GangBusiness.Supply.Id);}
        [Fact] public void BonusDoublesActiveClockWithoutChangingBatchValueOrNetFee()
        {var w=Ready();Complete(w);w.GangIllicit=new GangIllicitData();w.GangIllicit.Accounts.Add(new GangIllicitAccount{Gang=State().Gang,Area=4,Dirty=1500,Proceeds=1500});Assert.True(GangAutoLaunderTransactions.Configure(w,State(),Area(),true,"on",Save,out _));var clock=new GangAutoArmsClock(0);for(int n=1;n<30;n++)Assert.False(clock.Step(n*1000,true,60,GangCafeTransactions.Multiplier(w.GangCafe,State().Gang,4,Utc.AddSeconds(n))));Assert.True(clock.Step(30000,true,60,2));Assert.True(GangAutoLaunderTransactions.Process(w,State(),Area(),new GangAutoLaunderOptions(),Utc,"process",Save,out _));Assert.Equal(1000,w.GangIllicit.Accounts[0].Dirty);Assert.Equal(425,w.GangTreasury.Accounts[State().Gang].Balance);Assert.Equal(75,w.GangIllicit.Accounts[0].Fees);}
        [Fact] public void NoBonusKeepsMinuteAndExpiredBonusStopsAccelerating()
        {var clock=new GangAutoArmsClock(0);for(int n=1;n<=15;n++)Assert.False(clock.Step(n*1000,true,60,2));for(int n=16;n<45;n++)Assert.False(clock.Step(n*1000,true,60,1));Assert.True(clock.Step(45000,true,60,1));var normal=new GangAutoArmsClock(0);for(int n=1;n<60;n++)Assert.False(normal.Step(n*1000,true,60));Assert.True(normal.Step(60000,true,60));}
        [Fact] public void BoostedClockFreezesMenusRejectsGapsInvalidMultiplierAndReload()
        {var clock=new GangAutoArmsClock(0);clock.Step(1000,true,60,2);Assert.Equal(2000,clock.Milliseconds);Assert.False(clock.Step(2000,false,60,2));Assert.Equal(2000,clock.Milliseconds);Assert.False(clock.Step(20000,true,60,2));Assert.Equal(2000,clock.Milliseconds);Assert.False(clock.Step(21000,true,60,3));Assert.Equal(2000,clock.Milliseconds);Assert.Equal(0,new GangAutoArmsClock(22000).Milliseconds);}
        [Fact] public void RewardBookRejectsDuplicatesImpossibleExpiryAndInvalidOrderMetadata()
        {var w=Ready();var p=w.GangBusiness.Supply;p.Cafe=null;Assert.False(p.Valid);Assert.False(Complete(w));w=Ready();Complete(w);var a=Bonus(w);w.GangCafe.Accounts.Add(a);Assert.False(w.GangCafe.Valid);w.GangCafe.Accounts.RemoveAt(1);a.ExpiresUtcTicks=Utc.AddMinutes(181).Ticks;Assert.False(w.GangCafe.Valid);}
        [Fact] public void ReceiptCapsAndMetadataArePreservedOnAward()
        {var w=Ready();w.GangCafe=new GangCafeData{Receipts=Enumerable.Range(0,256).Select(i=>"old"+i).ToList()};w.GangBusiness.Receipts=Enumerable.Range(0,256).Select(i=>"b"+i).ToList();Assert.True(Complete(w));Assert.Equal(256,w.GangCafe.Receipts.Count);Assert.DoesNotContain("old0",w.GangCafe.Receipts);Assert.Equal(256,w.GangBusiness.Receipts.Count);}
        [Fact] public void CannotBeginUnfinishableRewardWhenCounterOrAccountCapacityIsFull()
        {var w=Ready();Complete(w);Bonus(w).Completed=long.MaxValue;Assert.False(Begin(w,"second"));Assert.Null(w.GangBusiness.Supply);w=W();w.GangCafe=new GangCafeData();for(int n=0;n<640;n++)w.GangCafe.Accounts.Add(new GangCafeAccount{Gang="Foreign"+n,Area=4,Completed=1,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks});Assert.True(w.GangCafe.Valid);Assert.False(Begin(w));Assert.Null(w.GangBusiness);}
        [Fact] public void RewardAccountAndBookUnknownFieldsSurviveActualRefresh()
        {var w=Ready();Complete(w);var json=JObject.Parse(JsonConvert.SerializeObject(w));json["GangCafe"]["FutureBook"]="keep";json["GangCafe"]["Accounts"][0]["FutureAccount"]=42;w=json.ToObject<WalletData>();Assert.True(Begin(w,"again"));for(int i=0;i<3;i++)GangSupplyTransactions.Load(w,"again",i,Save,out _);for(int n=1;n<=3;n++)GangSupplyTransactions.Deliver(w,"again",n,Save,out _);Assert.True(Complete(w,Utc.AddMinutes(2),"again"));var result=JObject.Parse(JsonConvert.SerializeObject(w));Assert.Equal("keep",(string)result["GangCafe"]["FutureBook"]);Assert.Equal(42,(int)result["GangCafe"]["Accounts"][0]["FutureAccount"]);Assert.Equal(2,Bonus(w).Completed);}
    }
}
