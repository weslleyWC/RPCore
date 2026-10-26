using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangMedicalTests
    {
        static readonly Action Save=()=>{};static readonly DateTime Utc=new DateTime(2026,10,26,12,0,0,DateTimeKind.Utc);
        static GangTurfDefinition A()=>GangTurfOptions.Defaults().Areas.Single(a=>a.Id==6);
        static GangState S()=>new GangState{Gang=A().DefaultOwner};static WalletData W()=>new WalletData{Bank=10000,Cash=123};
        static bool Begin(WalletData w,string id="med",GangMedicalOptions c=null,Action save=null)=>GangMedicalTransactions.Begin(w,S(),A(),c??new GangMedicalOptions(),id,save??Save,out _);
        static bool Collect(WalletData w,string id="med",Action save=null)=>GangMedicalTransactions.Collect(w,S(),A(),id,save??Save,out _);
        static bool Deliver(WalletData w,string id="med",Action save=null)=>GangMedicalTransactions.Deliver(w,S(),A(),id,save??Save,out _);
        static bool Complete(WalletData w,string id="med",DateTime? utc=null,Action save=null)=>GangMedicalTransactions.Complete(w,S(),A(),id,utc??Utc,save??Save,out _);
        static WalletData Collected(){var w=W();Assert.True(Begin(w));Assert.True(Collect(w));return w;}
        static WalletData Ready(){var w=Collected();Assert.True(Deliver(w));return w;}
        static GangMedicalAccount Reward(WalletData w)=>GangMedicalTransactions.Account(w.GangMedical,S().Gang);
        [Fact] public void DefaultsMatchSourceDoctorDocumentsAndBothHospitalLocations()
        {var c=new GangMedicalOptions();Assert.True(c.Valid);Assert.Equal(60,c.BoostMinutes);Assert.Equal(1500,c.HandoverMilliseconds);Assert.Equal("s_m_m_doctor_01",c.DoctorModel);Assert.Equal("prop_cs_documents_01",c.DocumentModel);Assert.Equal(-940.9167f,c.Doctor.X);Assert.Equal(-337.9953f,c.Doctor.Y);Assert.Equal(109.513f,c.Doctor.Heading);Assert.Equal(342.6898f,c.Destination.X);Assert.Equal(-1397.448f,c.Destination.Y);}
        [Fact] public void BeginIsFreeWithoutMoneyStockOrAutomaticEnabling()
        {var w=W();w.Bank=0;Assert.True(Begin(w));Assert.Equal(0,w.Bank);Assert.Equal(123,w.Cash);Assert.Empty(w.GangMedical.Accounts);Assert.Null(w.GangBusiness);Assert.Null(w.GangTreasury);Assert.Null(w.GangIllicit);Assert.Null(w.GangAutoLaunder);}
        [Fact] public void RequiresOwnedAreaSixAndValidOptions()
        {var w=W();var a=GangTurfOptions.Defaults().Areas[0];Assert.False(GangMedicalTransactions.Begin(w,new GangState{Gang=a.DefaultOwner},a,new GangMedicalOptions(),"bad",Save,out _));Assert.False(GangMedicalTransactions.Begin(w,new GangState{Gang="Other"},A(),new GangMedicalOptions(),"bad",Save,out _));Assert.False(Begin(w,c:new GangMedicalOptions{BoostMinutes=0}));Assert.Null(w.GangMedical);}
        [Fact] public void SnapshotDoesNotShareCallerOptionsOrCoordinates()
        {var w=W();var c=new GangMedicalOptions();Begin(w,c:c);c.Doctor.X=0;c.Destination.Y=0;c.HandoverMilliseconds=500;Assert.Equal(-940.9167f,w.GangMedical.Pending.Options.Doctor.X);Assert.Equal(-1397.448f,w.GangMedical.Pending.Options.Destination.Y);Assert.Equal(1500,w.GangMedical.Pending.Options.HandoverMilliseconds);}
        [Fact] public void CannotDeliverOrAwardBeforeDocumentCollection()
        {var w=W();Begin(w);Assert.False(Deliver(w));Assert.False(Complete(w));Assert.False(w.GangMedical.Pending.Collected);Assert.Empty(w.GangMedical.Accounts);}
        [Fact] public void CollectionHasNoPartialRewardAndNeedsDelivery()
        {var w=Collected();Assert.True(w.GangMedical.Pending.Collected);Assert.False(w.GangMedical.Pending.Delivered);Assert.False(Complete(w));Assert.Empty(w.GangMedical.Accounts);Assert.Null(w.GangTreasury);Assert.Equal(10000,w.Bank);}
        [Fact] public void SavedDeliveryStillRequiresExplicitFinalConfirmation()
        {var w=Ready();Assert.True(w.GangMedical.Pending.Ready);Assert.Empty(w.GangMedical.Accounts);Assert.Null(w.GangAutoLaunder);Assert.Null(w.GangIllicit);}
        [Fact] public void FinalAwardAndPendingRemovalShareOneSaveWithoutCashOrStock()
        {var w=Ready();int saves=0;Assert.True(Complete(w,save:()=>{saves++;Assert.Null(w.GangMedical.Pending);Assert.Equal(1,Reward(w).Completed);}));Assert.Equal(1,saves);Assert.Equal(Utc.Ticks,Reward(w).StartedUtcTicks);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangBusiness);Assert.Null(w.GangTreasury);}
        [Fact] public void FinalReplayCannotSaveExtendOrAwardAgain()
        {var w=Ready();Complete(w);var old=w.GangMedical;Assert.True(Complete(w,utc:Utc.AddMinutes(10),save:()=>throw new Exception()));Assert.Same(old,w.GangMedical);Assert.Equal(1,Reward(w).Completed);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void CollectionReplayCannotRepeatProofOrSave()
        {var w=Collected();var old=w.GangMedical;Assert.True(GangMedicalTransactions.Collect(w,S(),A(),"med",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Same(old,w.GangMedical);Assert.True(w.GangMedical.Pending.Collected);}
        [Fact] public void DeliveryReplayCannotRepeatProofOrSave()
        {var w=Ready();var old=w.GangMedical;Assert.True(GangMedicalTransactions.Deliver(w,S(),A(),"med",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Same(old,w.GangMedical);}
        [Fact] public void BeginReplayDoesNotEraseCollectedProgress()
        {var w=Collected();var old=w.GangMedical;Assert.True(Begin(w,save:()=>throw new Exception()));Assert.Same(old,w.GangMedical);Assert.True(w.GangMedical.Pending.Collected);Assert.False(Begin(w,"again"));}
        [Fact] public void CollectedProgressSurvivesReloadWithoutPhysicalHandles()
        {var w=Collected();string text=JsonConvert.SerializeObject(w);w=JsonConvert.DeserializeObject<WalletData>(text);Assert.True(w.GangMedical.Valid);Assert.True(w.GangMedical.Pending.Collected);Assert.False(w.GangMedical.Pending.Delivered);Assert.DoesNotContain("Handle",text);Assert.True(Deliver(w));}
        [Fact] public void DeliveredReloadCanConfirmWithoutDoctorOrDocumentRespawn()
        {var w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(Ready()));Assert.True(w.GangMedical.Pending.Ready);Assert.True(Complete(w));Assert.Equal(1,Reward(w).Completed);}
        [Fact] public void CancelIsFreeAndIndependentOfOwnership()
        {var w=Collected();Assert.True(GangMedicalTransactions.Cancel(w,"med",Save,out _));Assert.Null(w.GangMedical.Pending);Assert.Empty(w.GangMedical.Accounts);Assert.Equal(10000,w.Bank);Assert.True(GangMedicalTransactions.Cancel(w,"med",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.False(Complete(w));}
        [Fact] public void CancellingLaterAttemptPreservesPriorBonusExpiry()
        {var w=Ready();Complete(w);long expiry=Reward(w).ExpiresUtcTicks;Begin(w,"again");Assert.True(GangMedicalTransactions.Cancel(w,"again",Save,out _));Assert.Equal(expiry,Reward(w).ExpiresUtcTicks);}
        [Fact] public void RealSecondContractRefreshesWithoutStackingTime()
        {var w=Ready();Complete(w);Begin(w,"again");Collect(w,"again");Deliver(w,"again");Assert.True(Complete(w,"again",Utc.AddMinutes(10)));Assert.Equal(2,Reward(w).Completed);Assert.Equal(Utc.AddMinutes(70).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void LostOwnerCannotCollectDeliverOrClaimButCanCancel()
        {var w=W();Begin(w);var other=new GangState{Gang="Other"};Assert.False(GangMedicalTransactions.Collect(w,other,A(),"med",Save,out _));w=Collected();Assert.False(GangMedicalTransactions.Deliver(w,other,A(),"med",Save,out _));w=Ready();Assert.False(GangMedicalTransactions.Complete(w,other,A(),"med",Utc,Save,out _));Assert.True(w.GangMedical.Pending.Ready);Assert.True(GangMedicalTransactions.Cancel(w,"med",Save,out _));}
        [Fact] public void FailedBeginRestoresAbsentBook()
        {var w=W();Assert.Throws<Exception>(()=>Begin(w,save:()=>throw new Exception()));Assert.Null(w.GangMedical);Assert.Equal(10000,w.Bank);}
        [Fact] public void FailedCollectRestoresPreviousBookAndAllowsRetry()
        {var w=W();Begin(w);var old=w.GangMedical;Assert.Throws<Exception>(()=>Collect(w,save:()=>throw new Exception()));Assert.Same(old,w.GangMedical);Assert.False(w.GangMedical.Pending.Collected);Assert.True(Collect(w));}
        [Fact] public void FailedDeliverKeepsCollectedCheckpointWithoutReadyPromotion()
        {var w=Collected();var old=w.GangMedical;Assert.Throws<Exception>(()=>Deliver(w,save:()=>throw new Exception()));Assert.Same(old,w.GangMedical);Assert.True(w.GangMedical.Pending.Collected);Assert.False(w.GangMedical.Pending.Delivered);Assert.True(Deliver(w));}
        [Fact] public void FailedAwardAndCancelKeepReadyAndOriginalReference()
        {var w=Ready();var old=w.GangMedical;Assert.Throws<Exception>(()=>Complete(w,save:()=>throw new Exception()));Assert.Same(old,w.GangMedical);Assert.Throws<Exception>(()=>GangMedicalTransactions.Cancel(w,"med",()=>throw new Exception(),out _));Assert.Same(old,w.GangMedical);Assert.True(w.GangMedical.Pending.Ready);}
        [Fact] public void AwardNeedsUtcAndExpiresExactlyAtHour()
        {var w=Ready();Assert.False(Complete(w,utc:DateTime.SpecifyKind(Utc,DateTimeKind.Unspecified)));Assert.False(Complete(w,utc:new DateTime(DateTime.MaxValue.Ticks,DateTimeKind.Utc)));Complete(w);Assert.Equal(2,GangMedicalTransactions.Multiplier(w.GangMedical,S().Gang,6,Utc));Assert.Equal(2,GangMedicalTransactions.Multiplier(w.GangMedical,S().Gang,6,Utc.AddHours(1).AddTicks(-1)));Assert.Equal(1,GangMedicalTransactions.Multiplier(w.GangMedical,S().Gang,6,Utc.AddHours(1)));Assert.Equal(1,GangMedicalTransactions.Multiplier(w.GangMedical,S().Gang,6,Utc.AddTicks(-1)));}
        [Fact] public void BonusIsProfileGangAreaScopedWithNoOfflineIncome()
        {var w=Ready();Complete(w);Assert.Equal(1,GangMedicalTransactions.Multiplier(w.GangMedical,"Other",6,Utc));Assert.Equal(1,GangMedicalTransactions.Multiplier(w.GangMedical,S().Gang,5,Utc));Assert.Equal(1,GangMedicalTransactions.Multiplier(W().GangMedical,S().Gang,6,Utc));Assert.Equal(1,GangMedicalTransactions.Multiplier(w.GangMedical,S().Gang,6,Utc.AddDays(2)));Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);}
        [Fact] public void LegacyQueriesDoNotCreateBookOrOptIn()
        {var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":100,\"Future\":42}");Assert.Null(w.GangMedical);Assert.False(GangMedicalTransactions.Blocked(w));Assert.Null(GangMedicalTransactions.Account(w.GangMedical,S().Gang));Assert.Equal(1,GangMedicalTransactions.Multiplier(w.GangMedical,S().Gang,6,Utc));Assert.Null(w.GangMedical);}
        [Fact] public void FutureFieldsSurviveCollectDeliverAwardAndRefresh()
        {var w=W();Begin(w);var j=JObject.FromObject(w);j["Future"]=42;j["GangMedical"]["FutureBook"]="keep";j["GangMedical"]["Pending"]["Options"]["Doctor"]["FutureDoctor"]="keep";w=j.ToObject<WalletData>();Collect(w);Assert.Equal("keep",(string)JObject.FromObject(w)["GangMedical"]["Pending"]["Options"]["Doctor"]["FutureDoctor"]);Deliver(w);Complete(w);j=JObject.FromObject(w);j["GangMedical"]["Accounts"][0]["FutureAccount"]=true;w=j.ToObject<WalletData>();Begin(w,"again");Collect(w,"again");Deliver(w,"again");Complete(w,"again",Utc.AddMinutes(2));j=JObject.FromObject(w);Assert.Equal(42,(int)j["Future"]);Assert.Equal("keep",(string)j["GangMedical"]["FutureBook"]);Assert.True((bool)j["GangMedical"]["Accounts"][0]["FutureAccount"]);}
        [Fact] public void InvalidBookProtectsOtherTerritorialFlows()
        {var w=W();w.GangMedical=new GangMedicalData{Accounts=null};Assert.False(Begin(w));Assert.True(GangBulkTransactions.Blocked(w));Assert.False(GangAutoArmsRules.Free(w));Assert.False(GangCashRunTransactions.OtherFree(w));Assert.False(GangTowTransactions.OtherFree(w));Assert.False(GangMechanicTransactions.OtherFree(w));Assert.False(GangPromotionTransactions.OtherFree(w));}
        [Fact] public void PendingMedicalBlocksCommerceFreightArmsAndAutomaticEnabling()
        {var w=W();Begin(w);Assert.True(GangBulkTransactions.Blocked(w));Assert.False(GangAutoArmsRules.Free(w));Assert.True(GangMedicalTransactions.OtherFree(w));Assert.False(GangBusinessTransactions.Trade(w,S(),A(),new GangBusinessOptions(),false,Utc,"buy",Save,out _));Assert.False(GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"freight",Save,out _));Assert.False(GangArmsTransactions.Buy(w,S(),A(),new GangArmsOptions(),"pistol",1,"arms",Save,out _));Assert.False(GangAutoLaunderTransactions.Configure(w,S(),A(),true,"on",Save,out _));Assert.Equal(10000,w.Bank);}
        [Fact] public void OtherInvalidBooksBlockWithoutRecursiveValidation()
        {var w=W();w.GangCashRun=new GangCashRunData{Accounts=null};Assert.False(Begin(w));Assert.Null(w.GangMedical);w=Ready();w.GangTow=new GangTowData{Accounts=null};Assert.False(Complete(w));Assert.True(w.GangMedical.Pending.Ready);Assert.True(GangMedicalTransactions.Cancel(w,"med",Save,out _));}
        [Fact] public void ExistingPaidFreightCannotBeOverwritten()
        {var w=W();Assert.True(GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"paid",Save,out _));Assert.False(Begin(w));Assert.Null(w.GangMedical);Assert.Equal(9850,w.Bank);}
        [Fact] public void FullCounterAndAccountCapacityRefuseUnfinishableStart()
        {var w=Ready();Complete(w);Reward(w).Completed=long.MaxValue;Assert.False(Begin(w,"again"));w=W();w.GangMedical=new GangMedicalData();for(int n=0;n<640;n++)w.GangMedical.Accounts.Add(new GangMedicalAccount{Gang="Foreign"+n,Completed=1,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks});Assert.True(w.GangMedical.Valid);Assert.False(Begin(w));}
        [Fact] public void ReceiptsAreBoundedAndReplayDoesNotRepeatCollection()
        {var w=W();Begin(w);w.GangMedical.Receipts=Enumerable.Range(0,256).Select(n=>"old"+n).ToList();Collect(w);Assert.Equal(256,w.GangMedical.Receipts.Count);Assert.DoesNotContain("old0",w.GangMedical.Receipts);Assert.True(Collect(w,save:()=>throw new Exception()));Assert.True(w.GangMedical.Pending.Collected);}
        [Fact] public void DeliveredWithoutCollectedIsInvalidAndCannotGrantBonus()
        {var w=W();Begin(w);w.GangMedical.Pending.Delivered=true;Assert.False(w.GangMedical.Valid);Assert.False(Complete(w));Assert.Empty(w.GangMedical.Accounts);}
        [Fact] public void InteractionRequiresFootSafeCorrectStageAndTwoMeterDistance()
        {Assert.True(GangMedicalRules.Interact(true,true,true,2));Assert.False(GangMedicalRules.Interact(false,true,true,0));Assert.False(GangMedicalRules.Interact(true,false,true,0));Assert.False(GangMedicalRules.Interact(true,true,false,0));Assert.False(GangMedicalRules.Interact(true,true,true,2.01f));Assert.False(GangMedicalRules.Interact(true,true,true,float.NaN));Assert.False(GangMedicalRules.Interact(true,true,true,-1));}
        [Fact] public void HandoverClockRequiresFullActiveTimeAndIgnoresMenuAndLongGap()
        {var c=new GangPromotionClock(0);Assert.False(c.Step(500,true,1500));Assert.False(c.Step(1000,false,1500));Assert.False(c.Step(5000,true,1500));Assert.False(c.Step(5500,true,1500));Assert.True(c.Step(6000,true,1500));Assert.False(c.Step(6500,false,1500));}
        [Fact] public void EarnedBonusFeedsExistingFiveHundredBatchAndEightyFivePercent()
        {var w=Ready();Complete(w);w.GangIllicit=new GangIllicitData();w.GangIllicit.Accounts.Add(new GangIllicitAccount{Gang=S().Gang,Area=6,Dirty=1500,Proceeds=1500});Assert.True(GangAutoLaunderTransactions.Configure(w,S(),A(),true,"on",Save,out _));var c=new GangAutoArmsClock(0);for(int n=1;n<30;n++)Assert.False(c.Step(n*1000,true,60,GangMedicalTransactions.Multiplier(w.GangMedical,S().Gang,6,Utc)));Assert.True(c.Step(30000,true,60,2));Assert.True(GangAutoLaunderTransactions.Process(w,S(),A(),new GangAutoLaunderOptions(),Utc,"process",Save,out _));Assert.Equal(1000,w.GangIllicit.Accounts[0].Dirty);Assert.Equal(425,w.GangTreasury.Accounts[S().Gang].Balance);}
        [Fact] public void CashRunBonusRemainsSeparateAndMaximumDoesNotMultiply()
        {var w=Ready();w.GangCashRun=new GangCashRunData();w.GangCashRun.Accounts.Add(new GangCashRunAccount{Gang=S().Gang,Completed=1,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks});var old=w.GangCashRun;Complete(w);Assert.Same(old,w.GangCashRun);Assert.Equal(2,Math.Max(GangMedicalTransactions.Multiplier(w.GangMedical,S().Gang,6,Utc),GangCashRunTransactions.Multiplier(w.GangCashRun,S().Gang,6,Utc)));Assert.Equal(2,Math.Max(GangMedicalTransactions.Multiplier(w.GangMedical,S().Gang,5,Utc),GangCashRunTransactions.Multiplier(w.GangCashRun,S().Gang,5,Utc)));}
        [Fact] public void InvalidSavedOptionsStayProtectedEvenAfterCollection()
        {var w=Collected();w.GangMedical.Pending.Options.HandoverMilliseconds=0;Assert.False(w.GangMedical.Valid);Assert.False(Deliver(w));Assert.False(Complete(w));}
        [Fact] public void ConfigurationBoundsRejectBadModelsAndImpossibleRoute()
        {Assert.False(new GangMedicalOptions{DoctorModel="bad/model"}.Valid);Assert.False(new GangMedicalOptions{BoostMinutes=181}.Valid);Assert.False(new GangMedicalOptions{HandoverMilliseconds=499}.Valid);Assert.False(new GangMedicalOptions{StartRadius=16}.Valid);Assert.False(new GangMedicalOptions{Destination=new JobPoint{Name="Same",X=-940.9167f,Y=-337.9953f,Z=38.97752f}}.Valid);Assert.False(new GangMedicalOptions{Doctor=null}.Valid);}
        [Fact] public void RefreshSaveFailurePreservesPreviouslyEarnedExpiryAndReadyProof()
        {var w=Ready();Complete(w);long expiry=Reward(w).ExpiresUtcTicks;Begin(w,"again");Collect(w,"again");Deliver(w,"again");var old=w.GangMedical;Assert.Throws<Exception>(()=>Complete(w,"again",Utc.AddMinutes(10),()=>throw new Exception()));Assert.Same(old,w.GangMedical);Assert.True(w.GangMedical.Pending.Ready);Assert.Equal(expiry,Reward(w).ExpiresUtcTicks);Assert.Equal(1,Reward(w).Completed);}
    }
}
