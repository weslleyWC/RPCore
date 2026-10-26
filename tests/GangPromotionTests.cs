using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangPromotionTests
    {
        static readonly Action Save=()=>{};static readonly DateTime Utc=new DateTime(2026,10,25,15,0,0,DateTimeKind.Utc);
        static GangTurfDefinition A()=>GangTurfOptions.Defaults().Areas.Single(a=>a.Id==1);
        static GangState S()=>new GangState{Gang=A().DefaultOwner};
        static WalletData W()=>new WalletData{Bank=10000,Cash=321};
        static bool Begin(WalletData w,string id="promo",Action save=null,GangPromotionOptions c=null)=>GangPromotionTransactions.Begin(w,S(),A(),c??new GangPromotionOptions(),id,save??Save,out _);
        static bool Give(WalletData w,string customer,string id="promo",Action save=null)=>GangPromotionTransactions.Give(w,S(),A(),id,customer,save??Save,out _);
        static bool Complete(WalletData w,DateTime? utc=null,string id="promo",Action save=null)=>GangPromotionTransactions.Complete(w,S(),A(),id,utc??Utc,save??Save,out _);
        static WalletData Ready(string id="promo"){var w=W();Assert.True(Begin(w,id));for(int n=0;n<5;n++)Assert.True(Give(w,"customer"+n,id));return w;}
        static GangPromotionAccount Reward(WalletData w)=>GangPromotionTransactions.Account(w.GangPromotion,S().Gang);
        static GangPromotionCustomer Customer()=>new GangPromotionCustomer{Human=true,Alive=true,OnFoot=true,Ambient=true,PedType=4,ShopDistance=75,PlayerDistance=12};
        [Fact] public void DefaultsMatchSourceFiveFlyersShopRadiusAndHour()
        {var c=new GangPromotionOptions();Assert.True(c.Valid);Assert.Equal(5,c.Goal);Assert.Equal(60,c.BoostMinutes);Assert.Equal(120,c.SearchRadius);Assert.Equal(50,c.MinDistance);Assert.Equal(5,c.ReturnRadius);Assert.Equal(2.2f,c.InteractionDistance);Assert.Equal(1320.043f,c.Shop.X);Assert.Equal(-1647.478f,c.Shop.Y);}
        [Fact] public void BeginDoesNotChargeCreateIncomeOrEnableAutomaticProcessing()
        {var w=W();w.Bank=0;Assert.True(Begin(w));Assert.Equal(0,w.Bank);Assert.Equal(321,w.Cash);Assert.Empty(w.GangPromotion.Accounts);Assert.Null(w.GangCafe);Assert.Null(w.GangTreasury);Assert.Null(w.GangAutoLaunder);Assert.Null(w.GangIllicit);Assert.Null(w.GangBusiness);}
        [Fact] public void RequiresAreaOneAndOwnershipAndValidOptions()
        {var w=W();var other=GangTurfOptions.Defaults().Areas[3];Assert.False(GangPromotionTransactions.Begin(w,new GangState{Gang=other.DefaultOwner},other,new GangPromotionOptions(),"x",Save,out _));Assert.False(GangPromotionTransactions.Begin(w,new GangState{Gang="Other"},A(),new GangPromotionOptions(),"x",Save,out _));Assert.False(Begin(w,c:new GangPromotionOptions{Goal=0}));Assert.Null(w.GangPromotion);}
        [Fact] public void SnapshotsAreIndependentFromCallerMutableOptions()
        {var w=W();var c=new GangPromotionOptions();Assert.True(Begin(w,c:c));c.Goal=1;c.Shop.X=0;Assert.Equal(5,w.GangPromotion.Pending.Options.Goal);Assert.Equal(1320.043f,w.GangPromotion.Pending.Options.Shop.X);}
        [Fact] public void PartialFlyersCannotGrantRewardAndDuplicateCannotIncreaseCount()
        {var w=W();Begin(w);Assert.True(Give(w,"one"));Assert.True(Give(w,"one",save:()=>throw new Exception()));Assert.Single(w.GangPromotion.Pending.Customers);Assert.False(Complete(w));Assert.Empty(w.GangPromotion.Accounts);}
        [Fact] public void FiveDistinctCustomersStillRequireFinalReturnConfirmation()
        {var w=Ready();Assert.True(w.GangPromotion.Pending.Ready);Assert.Empty(w.GangPromotion.Accounts);Assert.False(Give(w,"sixth"));Assert.Equal(10000,w.Bank);Assert.Equal(321,w.Cash);Assert.Null(w.GangTreasury);}
        [Fact] public void FinalSaveRemovesPendingAndAwardsOnceWithoutFinancialEffects()
        {var w=Ready();int saves=0;Assert.True(Complete(w,save:()=>{saves++;Assert.Null(w.GangPromotion.Pending);Assert.Equal(1,Reward(w).Completed);}));Assert.Equal(1,saves);Assert.Equal(Utc.Ticks,Reward(w).StartedUtcTicks);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);Assert.Null(w.GangAutoLaunder);Assert.Equal(10000,w.Bank);Assert.Equal(321,w.Cash);}
        [Fact] public void CompletionReplayCannotSaveExtendOrAwardAgain()
        {var w=Ready();Complete(w);var book=w.GangPromotion;Assert.True(Complete(w,Utc.AddMinutes(15),save:()=>throw new Exception()));Assert.Same(book,w.GangPromotion);Assert.Equal(1,Reward(w).Completed);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void BeginReplayCannotResetCurrentProgress()
        {var w=W();Begin(w);Give(w,"first");Assert.True(Begin(w,save:()=>throw new Exception()));Assert.Single(w.GangPromotion.Pending.Customers);Assert.False(Begin(w,"second"));}
        [Fact] public void PartialResumeResetsPhysicalEvidenceAndOldGiveReceipts()
        {var w=W();Begin(w);Give(w,"first");Assert.True(GangPromotionTransactions.Restart(w,"promo",Save,out _));Assert.Empty(w.GangPromotion.Pending.Customers);Assert.DoesNotContain("promo:promotion-give:first",w.GangPromotion.Receipts);Assert.True(Give(w,"first"));Assert.Single(w.GangPromotion.Pending.Customers);Assert.Empty(w.GangPromotion.Accounts);}
        [Fact] public void ReadyResumeCannotEraseDeliveredProof()
        {var w=Ready();Assert.False(GangPromotionTransactions.Restart(w,"promo",Save,out _));Assert.True(w.GangPromotion.Pending.Ready);Assert.True(Complete(w));}
        [Fact] public void CancelIsFreeNeedsNoOwnerAndDoesNotAward()
        {var w=Ready();Assert.True(GangPromotionTransactions.Cancel(w,"promo",Save,out _));Assert.Null(w.GangPromotion.Pending);Assert.Empty(w.GangPromotion.Accounts);Assert.True(GangPromotionTransactions.Cancel(w,"promo",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(10000,w.Bank);Assert.False(Complete(w));}
        [Fact] public void CancellingLaterAttemptPreservesExistingExpiry()
        {var w=Ready();Complete(w);long expiry=Reward(w).ExpiresUtcTicks;Begin(w,"again");Assert.True(GangPromotionTransactions.Cancel(w,"again",Save,out _));Assert.Equal(expiry,Reward(w).ExpiresUtcTicks);Assert.Equal(1,Reward(w).Completed);}
        [Fact] public void AnotherActualMissionRefreshesWithoutStackingDuration()
        {var w=Ready();Complete(w);Begin(w,"second");for(int n=0;n<5;n++)Give(w,"new"+n,"second");Assert.True(Complete(w,Utc.AddMinutes(10),"second"));Assert.Equal(2,Reward(w).Completed);Assert.Equal(Utc.AddMinutes(70).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void LostOwnerAndForeignGangCannotGiveOrClaimButCanCancel()
        {var w=Ready();Assert.False(GangPromotionTransactions.Complete(w,new GangState{Gang="Other"},A(),"promo",Utc,Save,out _));Assert.True(w.GangPromotion.Pending.Ready);Assert.True(GangPromotionTransactions.Cancel(w,"promo",Save,out _));w=W();Begin(w);Assert.False(GangPromotionTransactions.Give(w,new GangState{Gang="Other"},A(),"promo","stranger",Save,out _));Assert.Empty(w.GangPromotion.Pending.Customers);}
        [Fact] public void FailedBeginRestoresAbsentBook()
        {var w=W();Assert.Throws<Exception>(()=>Begin(w,save:()=>throw new Exception()));Assert.Null(w.GangPromotion);Assert.Equal(10000,w.Bank);}
        [Fact] public void FailedGiveRestoresOriginalProofsAndAllowsRetry()
        {var w=W();Begin(w);var old=w.GangPromotion;Assert.Throws<Exception>(()=>Give(w,"first",save:()=>throw new Exception()));Assert.Same(old,w.GangPromotion);Assert.Empty(w.GangPromotion.Pending.Customers);Assert.True(Give(w,"first"));}
        [Fact] public void FailedCompletionKeepsReadyAndPreviousBookReference()
        {var w=Ready();var old=w.GangPromotion;Assert.Throws<Exception>(()=>Complete(w,save:()=>throw new Exception()));Assert.Same(old,w.GangPromotion);Assert.True(w.GangPromotion.Pending.Ready);Assert.Empty(w.GangPromotion.Accounts);Assert.True(Complete(w));}
        [Fact] public void FailedCancelAndRestartRestoreExactOriginalReference()
        {var w=W();Begin(w);Give(w,"first");var old=w.GangPromotion;Assert.Throws<Exception>(()=>GangPromotionTransactions.Cancel(w,"promo",()=>throw new Exception(),out _));Assert.Same(old,w.GangPromotion);Assert.Throws<Exception>(()=>GangPromotionTransactions.Restart(w,"promo",()=>throw new Exception(),out _));Assert.Same(old,w.GangPromotion);Assert.Single(w.GangPromotion.Pending.Customers);}
        [Fact] public void ExpiryUsesStrictUtcWindowWithNoOfflineIncome()
        {var w=Ready();Complete(w);Assert.Equal(2,GangPromotionTransactions.Multiplier(w.GangPromotion,S().Gang,1,Utc));Assert.Equal(2,GangPromotionTransactions.Multiplier(w.GangPromotion,S().Gang,1,Utc.AddHours(1).AddTicks(-1)));Assert.Equal(1,GangPromotionTransactions.Multiplier(w.GangPromotion,S().Gang,1,Utc.AddHours(1)));Assert.Equal(1,GangPromotionTransactions.Multiplier(w.GangPromotion,S().Gang,1,Utc.AddTicks(-1)));Assert.Equal(1,GangPromotionTransactions.Multiplier(w.GangPromotion,S().Gang,1,DateTime.SpecifyKind(Utc,DateTimeKind.Unspecified)));Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);}
        [Fact] public void BonusIsIsolatedByAreaGangAndProfile()
        {var w=Ready();Complete(w);Assert.Equal(1,GangPromotionTransactions.Multiplier(w.GangPromotion,S().Gang,4,Utc));Assert.Equal(1,GangPromotionTransactions.Multiplier(w.GangPromotion,"Other",1,Utc));Assert.Equal(1,GangPromotionTransactions.Multiplier(W().GangPromotion,S().Gang,1,Utc));Assert.Equal(2,GangPromotionTransactions.Multiplier(w.GangPromotion,S().Gang.ToUpperInvariant(),1,Utc));}
        [Fact] public void LegacyWalletQueriesDoNotCreateBookOrEnableBonus()
        {var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":500,\"Future\":42}");Assert.Null(w.GangPromotion);Assert.False(GangPromotionTransactions.Blocked(w));Assert.Null(GangPromotionTransactions.Account(w.GangPromotion,S().Gang));Assert.Equal(1,GangPromotionTransactions.Multiplier(w.GangPromotion,S().Gang,1,Utc));Assert.Null(w.GangPromotion);}
        [Fact] public void MetadataSurvivesCheckpointAndRewardAndSerialization()
        {var w=W();Begin(w);var j=JObject.Parse(JsonConvert.SerializeObject(w));j["Future"]=42;j["GangPromotion"]["FutureBook"]="keep";j["GangPromotion"]["Pending"]["Options"]["FutureSnapshot"]="keep";w=j.ToObject<WalletData>();Give(w,"first");var midway=JObject.Parse(JsonConvert.SerializeObject(w));Assert.Equal("keep",(string)midway["GangPromotion"]["Pending"]["Options"]["FutureSnapshot"]);for(int n=1;n<5;n++)Give(w,"next"+n);Complete(w);var end=JObject.Parse(JsonConvert.SerializeObject(w));Assert.Equal(42,(int)end["Future"]);Assert.Equal("keep",(string)end["GangPromotion"]["FutureBook"]);Assert.True(JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)).GangPromotion.Valid);}
        [Fact] public void InvalidBooksAreProtectedAndPreventParallelAutomaticTrade()
        {var w=W();w.GangPromotion=new GangPromotionData{Accounts=null};Assert.False(Begin(w));Assert.True(GangBulkTransactions.Blocked(w));Assert.False(GangAutoArmsRules.Free(w));Assert.Equal(1,GangPromotionTransactions.Multiplier(w.GangPromotion,S().Gang,1,Utc));}
        [Fact] public void PendingPromotionBlocksCommerceFreightCafeArmsAndAutoConfiguration()
        {var w=W();Begin(w);Assert.True(GangBulkTransactions.Blocked(w));Assert.False(GangAutoArmsRules.Free(w));Assert.True(GangPromotionTransactions.OtherFree(w));Assert.False(GangBusinessTransactions.Trade(w,S(),A(),new GangBusinessOptions(),false,Utc,"buy",Save,out _));Assert.False(GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"freight",Save,out _));Assert.False(GangArmsTransactions.Buy(w,S(),A(),new GangArmsOptions(),"pistol",1,"arms",Save,out _));Assert.False(GangAutoLaunderTransactions.Configure(w,S(),A(),true,"auto",Save,out _));Assert.Null(w.GangBusiness);Assert.Equal(10000,w.Bank);}
        [Fact] public void ExistingFreightCannotBeOverwrittenByPromotion()
        {var w=W();Assert.True(GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"freight",Save,out _));Assert.False(Begin(w));Assert.Null(w.GangPromotion);Assert.Equal(9850,w.Bank);Assert.Equal("freight",w.GangBusiness.Supply.Id);}
        [Fact] public void InvalidOrPendingOtherBooksBlockBeginAndFinalClaim()
        {var w=W();w.GangRaid=new GangRaidData{Receipts=null};Assert.False(Begin(w));w=Ready();w.GangBulk=new GangBulkData{Receipts=null};Assert.False(Complete(w));Assert.True(w.GangPromotion.Pending.Ready);Assert.True(GangPromotionTransactions.Cancel(w,"promo",Save,out _));}
        [Fact] public void OverflowAndInvalidExpiryRefuseWithoutConsumingReadyMission()
        {var w=Ready();Assert.False(Complete(w,new DateTime(DateTime.MaxValue.Ticks,DateTimeKind.Utc)));Assert.False(Complete(w,DateTime.SpecifyKind(Utc,DateTimeKind.Local)));Assert.True(w.GangPromotion.Pending.Ready);Complete(w);Reward(w).Completed=long.MaxValue;Assert.False(Begin(w,"other"));}
        [Fact] public void FullAccountBookCannotStartUnfinishableMission()
        {var w=W();w.GangPromotion=new GangPromotionData();for(int n=0;n<640;n++)w.GangPromotion.Accounts.Add(new GangPromotionAccount{Gang="Other"+n,Area=1,Completed=1,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks});Assert.True(w.GangPromotion.Valid);Assert.False(Begin(w));Assert.Null(w.GangPromotion.Pending);}
        [Fact] public void ReceiptsRemainBoundedAndInvalidCustomerCannotEnterSave()
        {var w=W();Begin(w);Assert.False(Give(w,""));Assert.False(Give(w,new string('a',81)));w.GangPromotion.Receipts=Enumerable.Range(0,256).Select(n=>"old"+n).ToList();Give(w,"first");Assert.Equal(256,w.GangPromotion.Receipts.Count);Assert.DoesNotContain("old0",w.GangPromotion.Receipts);}
        [Fact] public void ModelNeutralCandidateRulesRejectPoliceAnimalsGangAndMissionActors()
        {var c=new GangPromotionOptions();var p=Customer();Assert.True(GangPromotionTargetRules.Eligible(p,c,true));p.PedType=6;Assert.False(GangPromotionTargetRules.Eligible(p,c,true));p=Customer();p.Human=false;Assert.False(GangPromotionTargetRules.Eligible(p,c,true));p=Customer();p.KnownGang=true;Assert.False(GangPromotionTargetRules.Eligible(p,c,true));p=Customer();p.Ambient=false;Assert.False(GangPromotionTargetRules.Eligible(p,c,true));p=Customer();p.Grouped=true;Assert.False(GangPromotionTargetRules.Eligible(p,c,true));p=Customer();p.PedType=5;Assert.True(GangPromotionTargetRules.Eligible(p,c,true));}
        [Fact] public void CandidateMustBeAliveCalmFootAndUnused()
        {var c=new GangPromotionOptions();foreach(string field in new[]{"Alive","OnFoot","Combat","Ragdoll","Used"}){var p=Customer();typeof(GangPromotionCustomer).GetField(field).SetValue(p,field=="Combat" || field=="Ragdoll" || field=="Used");Assert.False(GangPromotionTargetRules.Eligible(p,c,false));}}
        [Fact] public void SearchAnnulusAndEightMeterInitialDistanceHaveInclusiveBoundaries()
        {var c=new GangPromotionOptions();var p=Customer();p.ShopDistance=50;p.PlayerDistance=8;Assert.True(GangPromotionTargetRules.Eligible(p,c,true));p.ShopDistance=120;Assert.True(GangPromotionTargetRules.Eligible(p,c,true));p.ShopDistance=49.99;Assert.False(GangPromotionTargetRules.Eligible(p,c,true));p.ShopDistance=120.01;Assert.False(GangPromotionTargetRules.Eligible(p,c,true));p.ShopDistance=75;p.PlayerDistance=2.2;Assert.False(GangPromotionTargetRules.Eligible(p,c,true));Assert.True(GangPromotionTargetRules.Eligible(p,c,false));p.PlayerDistance=double.NaN;Assert.False(GangPromotionTargetRules.Eligible(p,c,false));}
        [Fact] public void HandoverRequiresFullActiveDurationAndPauseDoesNotCount()
        {var clock=new GangPromotionClock(0);Assert.False(clock.Step(500,true,1500));Assert.False(clock.Step(1000,false,1500));Assert.Equal(500,clock.Milliseconds);Assert.False(clock.Step(1500,true,1500));Assert.True(clock.Step(2000,true,1500));Assert.False(clock.Step(2500,false,1500));}
        [Fact] public void HandoverIgnoresLongGapsAndHandlesGameTimeWrap()
        {var clock=new GangPromotionClock(0);Assert.False(clock.Step(5000,true,1500));Assert.Equal(0,clock.Milliseconds);Assert.False(clock.Step(5500,true,499));Assert.False(clock.Step(6000,true,5001));clock=new GangPromotionClock(int.MaxValue-499);Assert.False(clock.Step(unchecked(int.MaxValue+1),true,1500));Assert.Equal(500,clock.Milliseconds);}
        [Fact] public void EarnedBoostFeedsExistingProcessingWithUnchangedRateAndFee()
        {var w=Ready();Complete(w);w.GangIllicit=new GangIllicitData();w.GangIllicit.Accounts.Add(new GangIllicitAccount{Gang=S().Gang,Area=1,Dirty=1500,Proceeds=1500});Assert.True(GangAutoLaunderTransactions.Configure(w,S(),A(),true,"on",Save,out _));var clock=new GangAutoArmsClock(0);for(int n=1;n<30;n++)Assert.False(clock.Step(n*1000,true,60,Math.Max(GangCafeTransactions.Multiplier(w.GangCafe,S().Gang,1,Utc),GangPromotionTransactions.Multiplier(w.GangPromotion,S().Gang,1,Utc))));Assert.True(clock.Step(30000,true,60,2));Assert.True(GangAutoLaunderTransactions.Process(w,S(),A(),new GangAutoLaunderOptions(),Utc,"process",Save,out _));Assert.Equal(1000,w.GangIllicit.Accounts[0].Dirty);Assert.Equal(425,w.GangTreasury.Accounts[S().Gang].Balance);Assert.Equal(75,w.GangIllicit.Accounts[0].Fees);}
        [Fact] public void CafeAndPromotionBooksDoNotOverwriteOrStackMultipliers()
        {var w=Ready();w.GangCafe=new GangCafeData();w.GangCafe.Accounts.Add(new GangCafeAccount{Gang=S().Gang,Area=4,Completed=1,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks});var cafe=w.GangCafe;Complete(w);Assert.Same(cafe,w.GangCafe);Assert.Equal(2,Math.Max(GangCafeTransactions.Multiplier(w.GangCafe,S().Gang,4,Utc),GangPromotionTransactions.Multiplier(w.GangPromotion,S().Gang,4,Utc)));Assert.Equal(2,Math.Max(GangCafeTransactions.Multiplier(w.GangCafe,S().Gang,1,Utc),GangPromotionTransactions.Multiplier(w.GangPromotion,S().Gang,1,Utc)));}
        [Fact] public void BookValidationRejectsDuplicateCustomersAccountsAndImpossibleWindows()
        {var w=W();Begin(w);Give(w,"first");w.GangPromotion.Pending.Customers.Add("first");Assert.False(w.GangPromotion.Valid);Assert.False(Complete(w));w=Ready();Complete(w);var a=Reward(w);w.GangPromotion.Accounts.Add(a);Assert.False(w.GangPromotion.Valid);w.GangPromotion.Accounts.RemoveAt(1);a.ExpiresUtcTicks=Utc.AddMinutes(181).Ticks;Assert.False(w.GangPromotion.Valid);}
        [Fact] public void ConfigurationBoundsRejectInvalidRadiusGoalDurationAndPosition()
        {Assert.False(new GangPromotionOptions{Goal=21}.Valid);Assert.False(new GangPromotionOptions{MinDistance=120}.Valid);Assert.False(new GangPromotionOptions{SearchRadius=59}.Valid);Assert.False(new GangPromotionOptions{InteractionDistance=float.NaN}.Valid);Assert.False(new GangPromotionOptions{Shop=null}.Valid);Assert.False(new GangPromotionOptions{GiveMilliseconds=499}.Valid);Assert.False(new GangPromotionOptions{BoostMinutes=181}.Valid);}
    }
}
