using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangCashRunTests
    {
        static readonly Action Save=()=>{};static readonly DateTime Utc=new DateTime(2026,10,25,22,0,0,DateTimeKind.Utc);
        static GangTurfDefinition A()=>GangTurfOptions.Defaults().Areas.Single(a=>a.Id==5);
        static GangState S()=>new GangState{Gang=A().DefaultOwner};static WalletData W()=>new WalletData{Bank=10000,Cash=123};
        static bool Begin(WalletData w,string id="cash",GangCashRunOptions c=null,Action save=null)=>GangCashRunTransactions.Begin(w,S(),A(),c??new GangCashRunOptions(),id,save??Save,out _);
        static bool Load(WalletData w,int n,string id="cash",Action save=null)=>GangCashRunTransactions.Load(w,S(),A(),id,n,save??Save,out _);
        static bool Deliver(WalletData w,string id="cash",Action save=null)=>GangCashRunTransactions.Deliver(w,S(),A(),id,save??Save,out _);
        static bool Complete(WalletData w,string id="cash",DateTime? utc=null,Action save=null)=>GangCashRunTransactions.Complete(w,S(),A(),id,utc??Utc,save??Save,out _);
        static WalletData Loaded(){var w=W();Assert.True(Begin(w));for(int n=0;n<5;n++)Assert.True(Load(w,n));return w;}
        static WalletData Ready(){var w=Loaded();Assert.True(Deliver(w));return w;}
        static GangCashRunAccount Reward(WalletData w)=>GangCashRunTransactions.Account(w.GangCashRun,S().Gang);
        [Fact] public void DefaultsMatchSourceFiveSitesModelsAndDepotCoordinates()
        {var c=new GangCashRunOptions();Assert.True(c.Valid);Assert.Equal(5,c.Sites.Count);Assert.Equal(60,c.BoostMinutes);Assert.Equal("stockade",c.VehicleModel);Assert.Equal("s_m_m_security_01",c.GuardModel);Assert.Equal("prop_money_bag_01",c.BagModel);Assert.Equal(-1330.259f,c.Spawn.X);Assert.Equal(127.8134f,c.Spawn.Heading);Assert.Equal(-615.2822f,c.Depot.Y);Assert.Equal(131.4234f,c.Receiver.Heading);Assert.Equal(-1345.975f,c.Sites[0].X);Assert.Equal(327.9869f,c.Sites[4].Heading);}
        [Fact] public void FreeBeginDoesNotCreditMoneyCreateStockOrEnableAutomaticProcessing()
        {var w=W();w.Bank=0;Assert.True(Begin(w));Assert.Equal(0,w.Bank);Assert.Equal(123,w.Cash);Assert.Empty(w.GangCashRun.Accounts);Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);Assert.Null(w.GangBusiness);Assert.Null(w.GangAutoLaunder);}
        [Fact] public void RequiresOwnedAreaFiveAndValidOptions()
        {var w=W();var a=GangTurfOptions.Defaults().Areas[0];Assert.False(GangCashRunTransactions.Begin(w,new GangState{Gang=a.DefaultOwner},a,new GangCashRunOptions(),"bad",Save,out _));Assert.False(GangCashRunTransactions.Begin(w,new GangState{Gang="Other"},A(),new GangCashRunOptions(),"bad",Save,out _));Assert.False(Begin(w,c:new GangCashRunOptions{BoostMinutes=0}));Assert.Null(w.GangCashRun);}
        [Fact] public void CapturedOptionsAndSitesDoNotAliasCallerObjects()
        {var w=W();var c=new GangCashRunOptions();Begin(w,c:c);c.Sites[0].X=0;c.Sites.Clear();c.Depot.X=0;Assert.Equal(5,w.GangCashRun.Pending.Options.Sites.Count);Assert.Equal(-1345.975f,w.GangCashRun.Pending.Options.Sites[0].X);Assert.Equal(-680.2075f,w.GangCashRun.Pending.Options.Depot.X);}
        [Fact] public void EachSiteCanLoadOnlyOnceInAnyOrderAndPartialCannotDeliver()
        {var w=W();Begin(w);Assert.True(Load(w,4));Assert.True(Load(w,1));Assert.True(Load(w,4,save:()=>throw new Exception()));Assert.Equal(new[]{4,1},w.GangCashRun.Pending.Loaded);Assert.False(Load(w,-1));Assert.False(Load(w,5));Assert.False(Deliver(w));Assert.False(Complete(w));Assert.Empty(w.GangCashRun.Accounts);}
        [Fact] public void FiveLoadedStillRequireActualDepotDeliveryAndFinalConfirmation()
        {var w=Loaded();Assert.False(w.GangCashRun.Pending.Delivered);Assert.False(Complete(w));Assert.True(Deliver(w));Assert.True(w.GangCashRun.Pending.Ready);Assert.Empty(w.GangCashRun.Accounts);Assert.Null(w.GangTreasury);}
        [Fact] public void FinalAwardRemovesPendingAndGrantsUtcHourInOneSave()
        {var w=Ready();int saves=0;Assert.True(Complete(w,save:()=>{saves++;Assert.Null(w.GangCashRun.Pending);Assert.Equal(1,Reward(w).Completed);}));Assert.Equal(1,saves);Assert.Equal(Utc.Ticks,Reward(w).StartedUtcTicks);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);Assert.Null(w.GangAutoLaunder);}
        [Fact] public void ReplayCannotSaveExtendOrGrantAgain()
        {var w=Ready();Complete(w);var old=w.GangCashRun;Assert.True(Complete(w,utc:Utc.AddMinutes(10),save:()=>throw new Exception()));Assert.Same(old,w.GangCashRun);Assert.Equal(1,Reward(w).Completed);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void BeginReplayDoesNotEraseCargo()
        {var w=W();Begin(w);Load(w,1);var old=w.GangCashRun;Assert.True(Begin(w,save:()=>throw new Exception()));Assert.Same(old,w.GangCashRun);Assert.Single(w.GangCashRun.Pending.Loaded);Assert.False(Begin(w,"again"));}
        [Fact] public void DeliveryReplayCannotRepeatProofOrSave()
        {var w=Ready();var old=w.GangCashRun;Assert.True(Deliver(w,save:()=>throw new Exception()));Assert.Same(old,w.GangCashRun);Assert.True(w.GangCashRun.Pending.Delivered);Assert.True(GangCashRunTransactions.Load(w,S(),A(),"cash",0,()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(5,w.GangCashRun.Pending.Loaded.Count);}
        [Fact] public void PartialRestartClearsPhysicalCargoAndLoadReceiptsWithoutChangingContract()
        {var w=W();Begin(w);Load(w,2);Load(w,4);Assert.True(GangCashRunTransactions.Restart(w,"cash",Save,out _));Assert.Empty(w.GangCashRun.Pending.Loaded);Assert.Equal(5,w.GangCashRun.Pending.Options.Sites.Count);Assert.DoesNotContain("cash:cash-run-load:2",w.GangCashRun.Receipts);Assert.True(Load(w,2));Assert.Single(w.GangCashRun.Pending.Loaded);Assert.Empty(w.GangCashRun.Accounts);}
        [Fact] public void LoadedButNotDeliveredRestartsAndCannotBecomeReadyAfterReload()
        {var w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(Loaded()));Assert.False(w.GangCashRun.Pending.Ready);Assert.True(GangCashRunTransactions.Restart(w,"cash",Save,out _));Assert.Empty(w.GangCashRun.Pending.Loaded);Assert.False(Complete(w));}
        [Fact] public void DeliveredReloadKeepsReadyAndRefusesRestart()
        {var w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(Ready()));Assert.True(w.GangCashRun.Pending.Ready);Assert.False(GangCashRunTransactions.Restart(w,"cash",Save,out _));Assert.True(Complete(w));}
        [Fact] public void CancelIsFreeAndIndependentOfGangOwnership()
        {var w=Ready();Assert.True(GangCashRunTransactions.Cancel(w,"cash",Save,out _));Assert.Null(w.GangCashRun.Pending);Assert.Empty(w.GangCashRun.Accounts);Assert.Equal(10000,w.Bank);Assert.True(GangCashRunTransactions.Cancel(w,"cash",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.False(Complete(w));}
        [Fact] public void CancellingLaterAttemptKeepsOlderBonusExpiry()
        {var w=Ready();Complete(w);long expiry=Reward(w).ExpiresUtcTicks;Begin(w,"again");Assert.True(GangCashRunTransactions.Cancel(w,"again",Save,out _));Assert.Equal(expiry,Reward(w).ExpiresUtcTicks);}
        [Fact] public void RealSecondMissionRefreshesWithoutAddingRemainingDuration()
        {var w=Ready();Complete(w);Begin(w,"again");for(int n=0;n<5;n++)Load(w,n,"again");Deliver(w,"again");Assert.True(Complete(w,"again",Utc.AddMinutes(10)));Assert.Equal(2,Reward(w).Completed);Assert.Equal(Utc.AddMinutes(70).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void LostOwnerCannotLoadDeliverOrClaimButCanCancel()
        {var w=W();Begin(w);var other=new GangState{Gang="Other"};Assert.False(GangCashRunTransactions.Load(w,other,A(),"cash",0,Save,out _));w=Loaded();Assert.False(GangCashRunTransactions.Deliver(w,other,A(),"cash",Save,out _));w=Ready();Assert.False(GangCashRunTransactions.Complete(w,other,A(),"cash",Utc,Save,out _));Assert.True(w.GangCashRun.Pending.Ready);Assert.True(GangCashRunTransactions.Cancel(w,"cash",Save,out _));}
        [Fact] public void FailedBeginRestoresAbsentBook()
        {var w=W();Assert.Throws<Exception>(()=>Begin(w,save:()=>throw new Exception()));Assert.Null(w.GangCashRun);Assert.Equal(10000,w.Bank);}
        [Fact] public void FailedLoadRestoresPreviousProofAndAllowsRetry()
        {var w=W();Begin(w);var old=w.GangCashRun;Assert.Throws<Exception>(()=>Load(w,0,save:()=>throw new Exception()));Assert.Same(old,w.GangCashRun);Assert.Empty(w.GangCashRun.Pending.Loaded);Assert.True(Load(w,0));}
        [Fact] public void FailedDeliveryDoesNotPromoteLoadedCargoToReady()
        {var w=Loaded();var old=w.GangCashRun;Assert.Throws<Exception>(()=>Deliver(w,save:()=>throw new Exception()));Assert.Same(old,w.GangCashRun);Assert.False(w.GangCashRun.Pending.Delivered);Assert.True(Deliver(w));}
        [Fact] public void FailedAwardCancelAndRestartKeepOriginalReferences()
        {var w=Ready();var old=w.GangCashRun;Assert.Throws<Exception>(()=>Complete(w,save:()=>throw new Exception()));Assert.Same(old,w.GangCashRun);Assert.Throws<Exception>(()=>GangCashRunTransactions.Cancel(w,"cash",()=>throw new Exception(),out _));Assert.Same(old,w.GangCashRun);w=Loaded();old=w.GangCashRun;Assert.Throws<Exception>(()=>GangCashRunTransactions.Restart(w,"cash",()=>throw new Exception(),out _));Assert.Same(old,w.GangCashRun);Assert.Equal(5,w.GangCashRun.Pending.Loaded.Count);}
        [Fact] public void UtcAwardAndExactExpirationWindowAreEnforced()
        {var w=Ready();Assert.False(Complete(w,utc:DateTime.SpecifyKind(Utc,DateTimeKind.Unspecified)));Assert.False(Complete(w,utc:new DateTime(DateTime.MaxValue.Ticks,DateTimeKind.Utc)));Complete(w);Assert.Equal(2,GangCashRunTransactions.Multiplier(w.GangCashRun,S().Gang,5,Utc));Assert.Equal(2,GangCashRunTransactions.Multiplier(w.GangCashRun,S().Gang,5,Utc.AddHours(1).AddTicks(-1)));Assert.Equal(1,GangCashRunTransactions.Multiplier(w.GangCashRun,S().Gang,5,Utc.AddHours(1)));Assert.Equal(1,GangCashRunTransactions.Multiplier(w.GangCashRun,S().Gang,5,Utc.AddTicks(-1)));}
        [Fact] public void BonusIsProfileGangAreaScopedAndProducesNothingOffline()
        {var w=Ready();Complete(w);Assert.Equal(1,GangCashRunTransactions.Multiplier(w.GangCashRun,"Other",5,Utc));Assert.Equal(1,GangCashRunTransactions.Multiplier(w.GangCashRun,S().Gang,3,Utc));Assert.Equal(1,GangCashRunTransactions.Multiplier(W().GangCashRun,S().Gang,5,Utc));Assert.Equal(1,GangCashRunTransactions.Multiplier(w.GangCashRun,S().Gang,5,Utc.AddDays(2)));Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);}
        [Fact] public void LegacyQueriesDoNotCreateBookOrOptIn()
        {var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":100,\"Future\":42}");Assert.Null(w.GangCashRun);Assert.False(GangCashRunTransactions.Blocked(w));Assert.Null(GangCashRunTransactions.Account(w.GangCashRun,S().Gang));Assert.Equal(1,GangCashRunTransactions.Multiplier(w.GangCashRun,S().Gang,5,Utc));Assert.Null(w.GangCashRun);}
        [Fact] public void FutureFieldsSurviveCargoProofDeliveryAwardAndRefresh()
        {var w=W();Begin(w);var j=JObject.FromObject(w);j["Future"]=42;j["GangCashRun"]["FutureBook"]="keep";j["GangCashRun"]["Pending"]["Options"]["Sites"][0]["FutureSite"]="keep";w=j.ToObject<WalletData>();for(int n=0;n<5;n++)Load(w,n);Assert.Equal("keep",(string)JObject.FromObject(w)["GangCashRun"]["Pending"]["Options"]["Sites"][0]["FutureSite"]);Deliver(w);Complete(w);j=JObject.FromObject(w);j["GangCashRun"]["Accounts"][0]["FutureAccount"]=true;w=j.ToObject<WalletData>();Begin(w,"again");for(int n=0;n<5;n++)Load(w,n,"again");Deliver(w,"again");Complete(w,"again",Utc.AddMinutes(2));j=JObject.FromObject(w);Assert.Equal(42,(int)j["Future"]);Assert.Equal("keep",(string)j["GangCashRun"]["FutureBook"]);Assert.True((bool)j["GangCashRun"]["Accounts"][0]["FutureAccount"]);}
        [Fact] public void InvalidBookIsProtectedAndBlocksOtherTerritorialFlows()
        {var w=W();w.GangCashRun=new GangCashRunData{Accounts=null};Assert.False(Begin(w));Assert.True(GangBulkTransactions.Blocked(w));Assert.False(GangAutoArmsRules.Free(w));Assert.False(GangTowTransactions.OtherFree(w));Assert.False(GangMechanicTransactions.OtherFree(w));Assert.False(GangPromotionTransactions.OtherFree(w));}
        [Fact] public void PendingCashRunBlocksTradeFreightArmsAndAutoEnabling()
        {var w=W();Begin(w);Assert.True(GangBulkTransactions.Blocked(w));Assert.False(GangAutoArmsRules.Free(w));Assert.True(GangCashRunTransactions.OtherFree(w));Assert.False(GangBusinessTransactions.Trade(w,S(),A(),new GangBusinessOptions(),false,Utc,"buy",Save,out _));Assert.False(GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"freight",Save,out _));Assert.False(GangArmsTransactions.Buy(w,S(),A(),new GangArmsOptions(),"pistol",1,"arms",Save,out _));Assert.False(GangAutoLaunderTransactions.Configure(w,S(),A(),true,"on",Save,out _));Assert.Equal(10000,w.Bank);}
        [Fact] public void OtherInvalidOrPendingBooksBlockWithoutRecursiveValidation()
        {var w=W();w.GangTow=new GangTowData{Accounts=null};Assert.False(Begin(w));Assert.Null(w.GangCashRun);w=Ready();w.GangMechanic=new GangMechanicData{Accounts=null};Assert.False(Complete(w));Assert.True(w.GangCashRun.Pending.Ready);Assert.True(GangCashRunTransactions.Cancel(w,"cash",Save,out _));}
        [Fact] public void ExistingPaidFreightCannotBeOverwrittenByCashRun()
        {var w=W();Assert.True(GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"paid",Save,out _));Assert.False(Begin(w));Assert.Null(w.GangCashRun);Assert.Equal(9850,w.Bank);}
        [Fact] public void FullCounterOrAccountCapacityCannotStartUnfinishableRun()
        {var w=Ready();Complete(w);Reward(w).Completed=long.MaxValue;Assert.False(Begin(w,"again"));w=W();w.GangCashRun=new GangCashRunData();for(int n=0;n<640;n++)w.GangCashRun.Accounts.Add(new GangCashRunAccount{Gang="Foreign"+n,Completed=1,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks});Assert.True(w.GangCashRun.Valid);Assert.False(Begin(w));}
        [Fact] public void ReceiptsRemainBoundedAndLoadReplayCannotDuplicateCargo()
        {var w=W();Begin(w);w.GangCashRun.Receipts=Enumerable.Range(0,256).Select(n=>"old"+n).ToList();Load(w,0);Assert.Equal(256,w.GangCashRun.Receipts.Count);Assert.DoesNotContain("old0",w.GangCashRun.Receipts);Assert.True(Load(w,0,save:()=>throw new Exception()));Assert.Single(w.GangCashRun.Pending.Loaded);}
        [Fact] public void DuplicateIndicesAndDeliveredWithoutAllCargoAreInvalid()
        {var w=W();Begin(w);w.GangCashRun.Pending.Loaded.AddRange(new[]{1,1});Assert.False(w.GangCashRun.Valid);w=W();Begin(w);w.GangCashRun.Pending.Delivered=true;Assert.False(w.GangCashRun.Valid);Assert.False(Complete(w));}
        [Fact] public void LoadRuleRequiresOwnTruckFootCarriedBagAndBothDistances()
        {Assert.True(GangCashRunRules.Load(true,true,true,true,2.5f,35));Assert.False(GangCashRunRules.Load(false,true,true,true,0,0));Assert.False(GangCashRunRules.Load(true,false,true,true,0,0));Assert.False(GangCashRunRules.Load(true,true,false,true,0,0));Assert.False(GangCashRunRules.Load(true,true,true,false,0,0));Assert.False(GangCashRunRules.Load(true,true,true,true,2.51f,0));Assert.False(GangCashRunRules.Load(true,true,true,true,0,35.01f));Assert.False(GangCashRunRules.Load(true,true,true,true,float.NaN,0));}
        [Fact] public void ArrivalRequiresActualDriverFullLoadAndStoppedDepotPosition()
        {Assert.True(GangCashRunRules.Arrive(true,true,5,5,6,.49f));Assert.False(GangCashRunRules.Arrive(true,false,5,5,0,0));Assert.False(GangCashRunRules.Arrive(true,true,4,5,0,0));Assert.False(GangCashRunRules.Arrive(true,true,5,5,6.01f,0));Assert.False(GangCashRunRules.Arrive(true,true,5,5,0,.5f));Assert.False(GangCashRunRules.Arrive(false,true,5,5,0,0));}
        [Fact] public void HandoverRequiresArrivalFootWorkerDistanceAndSameStationaryTruck()
        {Assert.True(GangCashRunRules.Handover(true,true,true,true,2,10,.49f));Assert.False(GangCashRunRules.Handover(true,false,true,true,0,0,0));Assert.False(GangCashRunRules.Handover(true,true,false,true,0,0,0));Assert.False(GangCashRunRules.Handover(true,true,true,false,0,0,0));Assert.False(GangCashRunRules.Handover(true,true,true,true,2.01f,0,0));Assert.False(GangCashRunRules.Handover(true,true,true,true,0,10.01f,0));Assert.False(GangCashRunRules.Handover(true,true,true,true,0,0,.5f));}
        [Fact] public void HandoverClockRequiresFullActiveTimeAndCannotUseMenuOrLongGap()
        {var c=new GangPromotionClock(0);Assert.False(c.Step(500,true,1500));Assert.False(c.Step(1000,false,1500));Assert.False(c.Step(5000,true,1500));Assert.False(c.Step(5500,true,1500));Assert.True(c.Step(6000,true,1500));Assert.False(c.Step(6500,false,1500));}
        [Fact] public void EarnedBonusFeedsExistingFiveHundredBatchAndEightyFivePercent()
        {var w=Ready();Complete(w);w.GangIllicit=new GangIllicitData();w.GangIllicit.Accounts.Add(new GangIllicitAccount{Gang=S().Gang,Area=5,Dirty=1500,Proceeds=1500});Assert.True(GangAutoLaunderTransactions.Configure(w,S(),A(),true,"on",Save,out _));var c=new GangAutoArmsClock(0);for(int n=1;n<30;n++)Assert.False(c.Step(n*1000,true,60,GangCashRunTransactions.Multiplier(w.GangCashRun,S().Gang,5,Utc)));Assert.True(c.Step(30000,true,60,2));Assert.True(GangAutoLaunderTransactions.Process(w,S(),A(),new GangAutoLaunderOptions(),Utc,"process",Save,out _));Assert.Equal(1000,w.GangIllicit.Accounts[0].Dirty);Assert.Equal(425,w.GangTreasury.Accounts[S().Gang].Balance);}
        [Fact] public void TowBonusIsPreservedAndMultipliersUseMaximumNotProduct()
        {var w=Ready();w.GangTow=new GangTowData();w.GangTow.Accounts.Add(new GangTowAccount{Gang=S().Gang,Completed=1,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks});var old=w.GangTow;Complete(w);Assert.Same(old,w.GangTow);Assert.Equal(2,Math.Max(GangCashRunTransactions.Multiplier(w.GangCashRun,S().Gang,5,Utc),GangTowTransactions.Multiplier(w.GangTow,S().Gang,5,Utc)));Assert.Equal(2,Math.Max(GangCashRunTransactions.Multiplier(w.GangCashRun,S().Gang,3,Utc),GangTowTransactions.Multiplier(w.GangTow,S().Gang,3,Utc)));}
        [Fact] public void InvalidSavedOptionsCannotBeMadeValidByLoadedCargo()
        {var w=Loaded();w.GangCashRun.Pending.Options.BoostMinutes=0;Assert.False(w.GangCashRun.Valid);Assert.False(Deliver(w));Assert.False(Complete(w));}
        [Fact] public void ConfigRejectsDuplicateSitesBadModelsAndImpossibleDepotReceiverDistance()
        {var c=new GangCashRunOptions();c.Sites.Add(c.Sites[0]);Assert.False(c.Valid);Assert.False(new GangCashRunOptions{Sites=null}.Valid);Assert.False(new GangCashRunOptions{BagModel="bad/model"}.Valid);Assert.False(new GangCashRunOptions{BoostMinutes=181}.Valid);Assert.False(new GangCashRunOptions{Receiver=new JobPoint{Name="Far",X=1000}}.Valid);}
    }
}
