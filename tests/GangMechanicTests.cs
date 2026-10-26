using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangMechanicTests
    {
        static readonly Action Save=()=>{};static readonly DateTime Utc=new DateTime(2026,10,25,18,0,0,DateTimeKind.Utc);
        static GangTurfDefinition A()=>GangTurfOptions.Defaults().Areas.Single(a=>a.Id==2);
        static GangState S()=>new GangState{Gang=A().DefaultOwner};
        static WalletData W()=>new WalletData{Bank=10000,Cash=123};
        static bool Begin(WalletData w,string id="mechanic",GangMechanicOptions c=null,Action save=null)=>GangMechanicTransactions.Begin(w,S(),A(),c??new GangMechanicOptions(),Enumerable.Repeat("asea",(c??new GangMechanicOptions()).Goal),id,save??Save,out _);
        static bool Repair(WalletData w,int n,string id="mechanic",Action save=null)=>GangMechanicTransactions.Repair(w,S(),A(),id,n,save??Save,out _);
        static bool Complete(WalletData w,string id="mechanic",DateTime? utc=null,Action save=null)=>GangMechanicTransactions.Complete(w,S(),A(),id,utc??Utc,save??Save,out _);
        static WalletData Ready(){var w=W();Assert.True(Begin(w));for(int n=0;n<3;n++)Assert.True(Repair(w,n));return w;}
        static GangMechanicAccount Reward(WalletData w)=>GangMechanicTransactions.Account(w.GangMechanic,S().Gang);
        [Fact] public void DefaultsMatchReferenceThreeCarsThreeHitsSourcePositionsAndFullCatalog()
        {var c=new GangMechanicOptions();Assert.True(c.Valid);Assert.Equal(3,c.Goal);Assert.Equal(3,c.RequiredHits);Assert.Equal(60,c.BoostMinutes);Assert.Equal(90,c.ArrivalSeconds);Assert.Equal(45,c.DepartureSeconds);Assert.Equal(120,c.Models.Count);Assert.Equal(120,c.Models.Distinct().Count());Assert.Equal(542.1745f,c.Shop.X);Assert.Equal(-279.2764f,c.Spawn.Y);Assert.Equal(540.6617f,c.Bay.X);Assert.Equal(92,c.Bay.Heading);Assert.Equal(-260,c.Exit.Y);Assert.Equal("a_m_y_business_01",c.DriverModel);}
        [Fact] public void FreeBeginDoesNotCreateMoneyStockOrEnableProcessing()
        {var w=W();w.Bank=0;Assert.True(Begin(w));Assert.Equal(0,w.Bank);Assert.Equal(123,w.Cash);Assert.Empty(w.GangMechanic.Accounts);Assert.Null(w.GangTreasury);Assert.Null(w.GangIllicit);Assert.Null(w.GangAutoLaunder);Assert.Null(w.GangBusiness);}
        [Fact] public void RequiresOwnedTerritoryTwoAndValidOptions()
        {var w=W();var other=GangTurfOptions.Defaults().Areas[0];Assert.False(GangMechanicTransactions.Begin(w,new GangState{Gang=other.DefaultOwner},other,new GangMechanicOptions(),new[]{"asea","asea","asea"},"bad",Save,out _));Assert.False(GangMechanicTransactions.Begin(w,new GangState{Gang="Other"},A(),new GangMechanicOptions(),new[]{"asea","asea","asea"},"bad",Save,out _));Assert.False(Begin(w,c:new GangMechanicOptions{RequiredHits=0}));Assert.Null(w.GangMechanic);}
        [Fact] public void ModelChoicesMustMatchGoalAndCapturedCatalog()
        {var w=W();var c=new GangMechanicOptions();Assert.False(GangMechanicTransactions.Begin(w,S(),A(),c,new[]{"asea"},"bad",Save,out _));Assert.False(GangMechanicTransactions.Begin(w,S(),A(),c,new[]{"asea","invalid-car","asea"},"bad",Save,out _));Assert.Null(w.GangMechanic);}
        [Fact] public void SnapshotDoesNotShareOptionsPositionsOrSelectedList()
        {var w=W();var c=new GangMechanicOptions();var models=new[]{"asea","primo","stanier"};Assert.True(GangMechanicTransactions.Begin(w,S(),A(),c,models,"mechanic",Save,out _));c.Goal=1;c.Bay.X=0;c.Models.Clear();models[0]="buffalo";Assert.Equal(3,w.GangMechanic.Pending.Options.Goal);Assert.Equal(540.6617f,w.GangMechanic.Pending.Options.Bay.X);Assert.Equal(120,w.GangMechanic.Pending.Options.Models.Count);Assert.Equal("asea",w.GangMechanic.Pending.Models[0]);}
        [Fact] public void OnlySequentialDistinctVehicleIndicesCountAndPartialDoesNotReward()
        {var w=W();Begin(w);Assert.False(Repair(w,1));Assert.False(Repair(w,-1));Assert.True(Repair(w,0));Assert.True(Repair(w,0,save:()=>throw new Exception()));Assert.Equal(1,w.GangMechanic.Pending.Repaired);Assert.False(Complete(w));Assert.Empty(w.GangMechanic.Accounts);}
        [Fact] public void ThreeRepairsStillNeedFinalConfirmation()
        {var w=Ready();Assert.True(w.GangMechanic.Pending.Ready);Assert.False(Repair(w,3));Assert.Empty(w.GangMechanic.Accounts);Assert.Equal(10000,w.Bank);Assert.Null(w.GangTreasury);}
        [Fact] public void FinalSingleSaveRemovesPendingAndAwardsWithoutFinancialEffects()
        {var w=Ready();int saves=0;Assert.True(Complete(w,save:()=>{saves++;Assert.Null(w.GangMechanic.Pending);Assert.Equal(1,Reward(w).Completed);}));Assert.Equal(1,saves);Assert.Equal(Utc.Ticks,Reward(w).StartedUtcTicks);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);Assert.Null(w.GangAutoLaunder);}
        [Fact] public void FinalReplayDoesNotSaveExtendOrAwardAgain()
        {var w=Ready();Complete(w);var old=w.GangMechanic;Assert.True(Complete(w,utc:Utc.AddMinutes(10),save:()=>throw new Exception()));Assert.Same(old,w.GangMechanic);Assert.Equal(1,Reward(w).Completed);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void BeginReplayCannotResetAlreadyRepairedCars()
        {var w=W();Begin(w);Repair(w,0);Assert.True(Begin(w,save:()=>throw new Exception()));Assert.Equal(1,w.GangMechanic.Pending.Repaired);Assert.False(Begin(w,"second"));}
        [Fact] public void ReloadPreservesRepairedCarsAndCapturedNextModelWithoutHandles()
        {var w=W();Begin(w);Repair(w,0);string before=JsonConvert.SerializeObject(w);w=JsonConvert.DeserializeObject<WalletData>(before);Assert.True(w.GangMechanic.Valid);Assert.Equal(1,w.GangMechanic.Pending.Repaired);Assert.Equal("asea",w.GangMechanic.Pending.Models[1]);Assert.DoesNotContain("Handle",before);Assert.True(Repair(w,1));Assert.Equal(2,w.GangMechanic.Pending.Repaired);}
        [Fact] public void ReadyReloadNeedsNoAdditionalVehicleRepair()
        {var w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(Ready()));Assert.True(w.GangMechanic.Pending.Ready);Assert.True(Complete(w));Assert.Equal(1,Reward(w).Completed);}
        [Fact] public void CancellationIsFreeAndNeedsNoOwnershipOrAffiliation()
        {var w=Ready();Assert.True(GangMechanicTransactions.Cancel(w,"mechanic",Save,out _));Assert.Null(w.GangMechanic.Pending);Assert.Empty(w.GangMechanic.Accounts);Assert.Equal(10000,w.Bank);Assert.True(GangMechanicTransactions.Cancel(w,"mechanic",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.False(Complete(w));}
        [Fact] public void CancelLaterAttemptPreservesPreviouslyEarnedBonus()
        {var w=Ready();Complete(w);long expiry=Reward(w).ExpiresUtcTicks;Begin(w,"again");Assert.True(GangMechanicTransactions.Cancel(w,"again",Save,out _));Assert.Equal(expiry,Reward(w).ExpiresUtcTicks);Assert.Equal(1,Reward(w).Completed);}
        [Fact] public void RealSecondContractRefreshesHourRatherThanStacking()
        {var w=Ready();Complete(w);Begin(w,"second");for(int n=0;n<3;n++)Repair(w,n,"second");Assert.True(Complete(w,"second",Utc.AddMinutes(10)));Assert.Equal(2,Reward(w).Completed);Assert.Equal(Utc.AddMinutes(70).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void LostOwnerCannotRepairOrClaimButMayCancel()
        {var w=W();Begin(w);Assert.False(GangMechanicTransactions.Repair(w,new GangState{Gang="Other"},A(),"mechanic",0,Save,out _));Assert.Equal(0,w.GangMechanic.Pending.Repaired);w=Ready();Assert.False(GangMechanicTransactions.Complete(w,new GangState{Gang="Other"},A(),"mechanic",Utc,Save,out _));Assert.True(w.GangMechanic.Pending.Ready);Assert.True(GangMechanicTransactions.Cancel(w,"mechanic",Save,out _));}
        [Fact] public void FailedBeginRestoresAbsentBook()
        {var w=W();Assert.Throws<Exception>(()=>Begin(w,save:()=>throw new Exception()));Assert.Null(w.GangMechanic);Assert.Equal(10000,w.Bank);}
        [Fact] public void FailedRepairRestoresReferenceAndAllowsSameIndexRetry()
        {var w=W();Begin(w);var old=w.GangMechanic;Assert.Throws<Exception>(()=>Repair(w,0,save:()=>throw new Exception()));Assert.Same(old,w.GangMechanic);Assert.Equal(0,w.GangMechanic.Pending.Repaired);Assert.True(Repair(w,0));}
        [Fact] public void FailedCompletionAndCancelKeepReadyAndOriginalReference()
        {var w=Ready();var old=w.GangMechanic;Assert.Throws<Exception>(()=>Complete(w,save:()=>throw new Exception()));Assert.Same(old,w.GangMechanic);Assert.True(w.GangMechanic.Pending.Ready);Assert.Throws<Exception>(()=>GangMechanicTransactions.Cancel(w,"mechanic",()=>throw new Exception(),out _));Assert.Same(old,w.GangMechanic);Assert.True(Complete(w));}
        [Fact] public void RewardRequiresValidUtcAndExpiresExactlyAtHour()
        {var w=Ready();Assert.False(Complete(w,utc:DateTime.SpecifyKind(Utc,DateTimeKind.Unspecified)));Assert.False(Complete(w,utc:new DateTime(DateTime.MaxValue.Ticks,DateTimeKind.Utc)));Assert.True(w.GangMechanic.Pending.Ready);Complete(w);Assert.Equal(2,GangMechanicTransactions.Multiplier(w.GangMechanic,S().Gang,2,Utc));Assert.Equal(2,GangMechanicTransactions.Multiplier(w.GangMechanic,S().Gang,2,Utc.AddHours(1).AddTicks(-1)));Assert.Equal(1,GangMechanicTransactions.Multiplier(w.GangMechanic,S().Gang,2,Utc.AddHours(1)));Assert.Equal(1,GangMechanicTransactions.Multiplier(w.GangMechanic,S().Gang,2,Utc.AddTicks(-1)));}
        [Fact] public void BonusIsIsolatedByProfileGangAreaAndDoesNotCreateOfflineIncome()
        {var w=Ready();Complete(w);Assert.Equal(1,GangMechanicTransactions.Multiplier(w.GangMechanic,"Other",2,Utc));Assert.Equal(1,GangMechanicTransactions.Multiplier(w.GangMechanic,S().Gang,1,Utc));Assert.Equal(1,GangMechanicTransactions.Multiplier(W().GangMechanic,S().Gang,2,Utc));Assert.Equal(1,GangMechanicTransactions.Multiplier(w.GangMechanic,S().Gang,2,Utc.AddDays(2)));Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);}
        [Fact] public void LegacyWalletQueriesDoNotCreateNewBookOrBonus()
        {var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":100,\"Future\":42}");Assert.Null(w.GangMechanic);Assert.False(GangMechanicTransactions.Blocked(w));Assert.Null(GangMechanicTransactions.Account(w.GangMechanic,S().Gang));Assert.Equal(1,GangMechanicTransactions.Multiplier(w.GangMechanic,S().Gang,2,Utc));Assert.Null(w.GangMechanic);}
        [Fact] public void FutureFieldsSurviveCheckpointRewardAndRefresh()
        {var w=W();Begin(w);var j=JObject.Parse(JsonConvert.SerializeObject(w));j["Future"]=42;j["GangMechanic"]["FutureBook"]="keep";j["GangMechanic"]["Pending"]["Options"]["FutureSnapshot"]="keep";w=j.ToObject<WalletData>();Repair(w,0);Assert.Equal("keep",(string)JObject.FromObject(w)["GangMechanic"]["Pending"]["Options"]["FutureSnapshot"]);Repair(w,1);Repair(w,2);Complete(w);j=JObject.FromObject(w);j["GangMechanic"]["Accounts"][0]["FutureAccount"]=true;w=j.ToObject<WalletData>();Begin(w,"again");for(int n=0;n<3;n++)Repair(w,n,"again");Complete(w,"again",Utc.AddMinutes(2));j=JObject.FromObject(w);Assert.Equal(42,(int)j["Future"]);Assert.Equal("keep",(string)j["GangMechanic"]["FutureBook"]);Assert.True((bool)j["GangMechanic"]["Accounts"][0]["FutureAccount"]);}
        [Fact] public void InvalidBookIsProtectedAndBlocksSharedTerritorialActions()
        {var w=W();w.GangMechanic=new GangMechanicData{Accounts=null};Assert.False(Begin(w));Assert.True(GangBulkTransactions.Blocked(w));Assert.False(GangAutoArmsRules.Free(w));Assert.False(GangPromotionTransactions.OtherFree(w));Assert.Equal(1,GangMechanicTransactions.Multiplier(w.GangMechanic,S().Gang,2,Utc));}
        [Fact] public void PendingMechanicBlocksCommerceFreightArmsAndAutomaticEnabling()
        {var w=W();Begin(w);Assert.True(GangBulkTransactions.Blocked(w));Assert.False(GangAutoArmsRules.Free(w));Assert.True(GangMechanicTransactions.OtherFree(w));Assert.False(GangBusinessTransactions.Trade(w,S(),A(),new GangBusinessOptions(),false,Utc,"buy",Save,out _));Assert.False(GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"freight",Save,out _));Assert.False(GangArmsTransactions.Buy(w,S(),A(),new GangArmsOptions(),"pistol",1,"arms",Save,out _));Assert.False(GangAutoLaunderTransactions.Configure(w,S(),A(),true,"auto",Save,out _));Assert.Equal(10000,w.Bank);}
        [Fact] public void PromotionAndMechanicCannotCreateOverlappingPendingContracts()
        {var w=W();var area1=GangTurfOptions.Defaults().Areas[0];var gang1=new GangState{Gang=area1.DefaultOwner};Assert.True(GangPromotionTransactions.Begin(w,gang1,area1,new GangPromotionOptions(),"promo",Save,out _));Assert.False(Begin(w));Assert.Null(w.GangMechanic);w=W();Begin(w);Assert.False(GangPromotionTransactions.Begin(w,gang1,area1,new GangPromotionOptions(),"promo",Save,out _));Assert.Null(w.GangPromotion);}
        [Fact] public void ExistingPaidFreightCannotBeOverwritten()
        {var w=W();Assert.True(GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"paid",Save,out _));Assert.False(Begin(w));Assert.Null(w.GangMechanic);Assert.Equal(9850,w.Bank);Assert.Equal("paid",w.GangBusiness.Supply.Id);}
        [Fact] public void OtherInvalidBooksBlockFinalClaimWithoutDestroyingReady()
        {var w=Ready();w.GangRaid=new GangRaidData{Receipts=null};Assert.False(Complete(w));Assert.True(w.GangMechanic.Pending.Ready);Assert.True(GangMechanicTransactions.Cancel(w,"mechanic",Save,out _));}
        [Fact] public void FullHistoryOrAccountCapacityCannotStartUnfinishableMission()
        {var w=Ready();Complete(w);Reward(w).Completed=long.MaxValue;Assert.False(Begin(w,"again"));w=W();w.GangMechanic=new GangMechanicData();for(int n=0;n<640;n++)w.GangMechanic.Accounts.Add(new GangMechanicAccount{Gang="Foreign"+n,Completed=1,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks});Assert.True(w.GangMechanic.Valid);Assert.False(Begin(w));}
        [Fact] public void ReceiptsAreBoundedAndRepeatedRepairCannotRepeatCount()
        {var w=W();Begin(w);w.GangMechanic.Receipts=Enumerable.Range(0,256).Select(n=>"old"+n).ToList();Repair(w,0);Assert.Equal(256,w.GangMechanic.Receipts.Count);Assert.DoesNotContain("old0",w.GangMechanic.Receipts);Assert.True(Repair(w,0,save:()=>throw new Exception()));Assert.Equal(1,w.GangMechanic.Pending.Repaired);}
        [Fact] public void SkillMatchesSineGreenZoneAndThreeSeparateSuccessfulInputs()
        {var s=new GangMechanicSkill(0);Assert.Equal(.5,s.Cursor,8);for(int n=1;n<=3;n++){s.Step(n*900,true);Assert.True(GangMechanicSkill.Green(s.Cursor));Assert.True(s.Press(out bool hit));Assert.True(hit);}Assert.Equal(3,s.Hits);Assert.Equal(0,s.Misses);}
        [Fact] public void SkillInitialCooldownAndInputSpamCannotAwardHits()
        {var s=new GangMechanicSkill(0);Assert.False(s.Press(out _));s.Step(399,true);Assert.False(s.Press(out _));s.Step(500,true);Assert.True(s.Press(out bool hit));Assert.False(hit);Assert.Equal(1,s.Misses);Assert.False(s.Press(out _));s.Step(899,true);Assert.False(s.Press(out _));Assert.Equal(0,s.Hits);}
        [Fact] public void SkillPauseLongGapsAndReentryCannotCreateTimingCredit()
        {var s=new GangMechanicSkill(0);s.Step(5000,true);Assert.Equal(.5,s.Cursor,8);Assert.False(s.Press(out _));s.Step(5900,false);Assert.Equal(.5,s.Cursor,8);s.Step(6800,true);Assert.True(s.Press(out bool hit));Assert.True(hit);}
        [Fact] public void SkillGreenBoundariesAndInvalidNumbersAreHandled()
        {Assert.True(GangMechanicSkill.Green(.458));Assert.True(GangMechanicSkill.Green(.542));Assert.False(GangMechanicSkill.Green(.457999));Assert.False(GangMechanicSkill.Green(.542001));Assert.False(GangMechanicSkill.Green(double.NaN));Assert.False(GangMechanicSkill.Green(double.PositiveInfinity));}
        [Fact] public void SkillGameTimeWrapIsSafeAndNewCarStartsWithoutOldHits()
        {var s=new GangMechanicSkill(int.MaxValue-449);s.Step(unchecked(int.MaxValue+451),true);Assert.True(s.Press(out bool hit));Assert.True(hit);var next=new GangMechanicSkill(0);Assert.Equal(0,next.Hits);Assert.Equal(0,next.Misses);Assert.False(next.Press(out _));}
        [Fact] public void EarnedBoostUsesExistingFiveHundredBatchAndEightyFivePercent()
        {var w=Ready();Complete(w);w.GangIllicit=new GangIllicitData();w.GangIllicit.Accounts.Add(new GangIllicitAccount{Gang=S().Gang,Area=2,Dirty=1500,Proceeds=1500});Assert.True(GangAutoLaunderTransactions.Configure(w,S(),A(),true,"on",Save,out _));var c=new GangAutoArmsClock(0);for(int n=1;n<30;n++)Assert.False(c.Step(n*1000,true,60,GangMechanicTransactions.Multiplier(w.GangMechanic,S().Gang,2,Utc)));Assert.True(c.Step(30000,true,60,2));Assert.True(GangAutoLaunderTransactions.Process(w,S(),A(),new GangAutoLaunderOptions(),Utc,"process",Save,out _));Assert.Equal(1000,w.GangIllicit.Accounts[0].Dirty);Assert.Equal(425,w.GangTreasury.Accounts[S().Gang].Balance);Assert.Equal(75,w.GangIllicit.Accounts[0].Fees);}
        [Fact] public void ExistingCafeAndPromotionBonusesArePreservedWithoutMultiplication()
        {var w=Ready();w.GangCafe=new GangCafeData();w.GangCafe.Accounts.Add(new GangCafeAccount{Gang=S().Gang,Area=4,Completed=1,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks});w.GangPromotion=new GangPromotionData();w.GangPromotion.Accounts.Add(new GangPromotionAccount{Gang=S().Gang,Area=1,Completed=1,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks});var cafe=w.GangCafe;var promo=w.GangPromotion;Complete(w);Assert.Same(cafe,w.GangCafe);Assert.Same(promo,w.GangPromotion);for(int area=1;area<=4;area++)Assert.Equal(area==3?1:2,Math.Max(GangMechanicTransactions.Multiplier(w.GangMechanic,S().Gang,area,Utc),Math.Max(GangCafeTransactions.Multiplier(w.GangCafe,S().Gang,area,Utc),GangPromotionTransactions.Multiplier(w.GangPromotion,S().Gang,area,Utc))));}
        [Fact] public void InvalidSavedModelsCountsAndWindowsAreProtected()
        {var w=W();Begin(w);w.GangMechanic.Pending.Models[0]="unknown";Assert.False(w.GangMechanic.Valid);Assert.False(Repair(w,0));w=Ready();Complete(w);Reward(w).ExpiresUtcTicks=Utc.AddMinutes(181).Ticks;Assert.False(w.GangMechanic.Valid);}
        [Fact] public void OptionValidationRejectsInvalidRoutesCatalogAndSkillLimits()
        {Assert.False(new GangMechanicOptions{Goal=11}.Valid);Assert.False(new GangMechanicOptions{RequiredHits=0}.Valid);Assert.False(new GangMechanicOptions{Models=null}.Valid);var c=new GangMechanicOptions();c.Models.Add("ASEA");Assert.False(c.Valid);Assert.False(new GangMechanicOptions{Spawn=new JobPoint{Name="Invalid",X=float.NaN}}.Valid);Assert.False(new GangMechanicOptions{Bay=new JobPoint{Name="Far",X=1000}}.Valid);Assert.False(new GangMechanicOptions{BoostMinutes=181}.Valid);}
    }
}
