using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangTowTests
    {
        static readonly Action Save=()=>{};static readonly DateTime Utc=new DateTime(2026,10,25,21,0,0,DateTimeKind.Utc);
        static GangTurfDefinition A()=>GangTurfOptions.Defaults().Areas.Single(a=>a.Id==3);
        static GangState S()=>new GangState{Gang=A().DefaultOwner};static WalletData W()=>new WalletData{Bank=10000,Cash=123};
        static GangTowDispatch[] Dispatches(GangTowOptions c)=>Enumerable.Range(0,c.Goal).Select(i=>new GangTowDispatch{Model=c.Models[i%c.Models.Count],Customer=c.Customers[i%c.Customers.Count],Site=c.Sites[i%c.Sites.Count]}).ToArray();
        static bool Begin(WalletData w,string id="tow",GangTowOptions c=null,Action save=null){c=c??new GangTowOptions();return GangTowTransactions.Begin(w,S(),A(),c,Dispatches(c),id,save??Save,out _);}
        static bool Deliver(WalletData w,int index,float damage=0,string id="tow",Action save=null)=>GangTowTransactions.Deliver(w,S(),A(),id,index,damage,save??Save,out _);
        static bool Complete(WalletData w,string id="tow",DateTime? utc=null,Action save=null)=>GangTowTransactions.Complete(w,S(),A(),id,utc??Utc,save??Save,out _);
        static WalletData Ready(){var w=W();Assert.True(Begin(w));Assert.True(Deliver(w,0,50));Assert.True(Deliver(w,1,100));return w;}
        static GangTowAccount Reward(WalletData w)=>GangTowTransactions.Account(w.GangTow,S().Gang);
        [Fact] public void DefaultsFollowActualTwoRecoveryLogicAndFullSourceCatalogs()
        {var c=new GangTowOptions();Assert.True(c.Valid);Assert.Equal(2,c.Goal);Assert.Equal(60,c.BoostMinutes);Assert.Equal("towtruck",c.TruckModel);Assert.Equal(126,c.Models.Count);Assert.Equal(24,c.Customers.Count);Assert.Equal(15,c.Sites.Count);Assert.Equal(-48.79256f,c.TruckSpawn.X);Assert.Equal(294.2766f,c.TruckSpawn.Heading);Assert.Equal(-1683.975f,c.Yard.Y);Assert.Equal(-546.297f,c.Sites[0].X);Assert.Equal(130.069f,c.Sites[14].Heading);}
        [Fact] public void BeginIsFreeAndDoesNotGrantFinancialBooksStockOrBonus()
        {var w=W();w.Bank=0;Assert.True(Begin(w));Assert.Equal(0,w.Bank);Assert.Equal(123,w.Cash);Assert.Empty(w.GangTow.Accounts);Assert.Null(w.GangTreasury);Assert.Null(w.GangIllicit);Assert.Null(w.GangBusiness);Assert.Null(w.GangAutoLaunder);}
        [Fact] public void RequiresOwnedTerritoryThreeAndValidConfiguration()
        {var w=W();var other=GangTurfOptions.Defaults().Areas[0];Assert.False(GangTowTransactions.Begin(w,new GangState{Gang=other.DefaultOwner},other,new GangTowOptions(),Dispatches(new GangTowOptions()),"bad",Save,out _));Assert.False(GangTowTransactions.Begin(w,new GangState{Gang="Other"},A(),new GangTowOptions(),Dispatches(new GangTowOptions()),"bad",Save,out _));Assert.False(Begin(w,c:new GangTowOptions{Goal=0}));Assert.Null(w.GangTow);}
        [Fact] public void DispatchModelCustomerAndLocationMustMatchCapturedCatalog()
        {var w=W();var c=new GangTowOptions();var d=Dispatches(c);d[0].Model="unknown";Assert.False(GangTowTransactions.Begin(w,S(),A(),c,d,"bad",Save,out _));d=Dispatches(c);d[0].Customer="not-a-customer";Assert.False(GangTowTransactions.Begin(w,S(),A(),c,d,"bad",Save,out _));d=Dispatches(c);d[0].Site=new JobPoint{Name="Unknown",X=10};Assert.False(GangTowTransactions.Begin(w,S(),A(),c,d,"bad",Save,out _));Assert.False(GangTowTransactions.Begin(w,S(),A(),c,Dispatches(c).Take(1),"bad",Save,out _));Assert.Null(w.GangTow);}
        [Fact] public void CapturedDispatchesAndOptionsDoNotAliasCallerObjects()
        {var w=W();var c=new GangTowOptions();var d=Dispatches(c);Assert.True(GangTowTransactions.Begin(w,S(),A(),c,d,"tow",Save,out _));d[0].Model="changed";c.Sites[0].X=0;c.Models.Clear();c.Goal=1;Assert.Equal(2,w.GangTow.Pending.Options.Goal);Assert.Equal(126,w.GangTow.Pending.Options.Models.Count);Assert.Equal(-546.297f,w.GangTow.Pending.Dispatches[0].Site.X);Assert.Equal("asea",w.GangTow.Pending.Dispatches[0].Model);}
        [Fact] public void OnlySequentialDeliveryIndicesAdvanceAndNoPartialReward()
        {var w=W();Begin(w);Assert.False(Deliver(w,1));Assert.False(Deliver(w,-1));Assert.True(Deliver(w,0,74));Assert.True(Deliver(w,0,74,save:()=>throw new Exception()));Assert.Single(w.GangTow.Pending.Damage);Assert.False(Complete(w));Assert.Empty(w.GangTow.Accounts);}
        [Fact] public void TwoDeliveriesRequireFinalConfirmationAndDoNotCreateThird()
        {var w=Ready();Assert.True(w.GangTow.Pending.Ready);Assert.Equal(2,w.GangTow.Pending.Damage.Count);Assert.False(Deliver(w,2));Assert.Empty(w.GangTow.Accounts);Assert.Null(w.GangTreasury);}
        [Fact] public void FinalAwardIsOneSaveWithPendingRemovalAndNoDirectPayment()
        {var w=Ready();int saves=0;Assert.True(Complete(w,save:()=>{saves++;Assert.Null(w.GangTow.Pending);Assert.Equal(1,Reward(w).Completed);}));Assert.Equal(1,saves);Assert.Equal(Utc.Ticks,Reward(w).StartedUtcTicks);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangTreasury);Assert.Null(w.GangAutoLaunder);}
        [Fact] public void ReplayCannotExtendExpirySaveOrAwardTwice()
        {var w=Ready();Complete(w);var old=w.GangTow;Assert.True(Complete(w,utc:Utc.AddMinutes(10),save:()=>throw new Exception()));Assert.Same(old,w.GangTow);Assert.Equal(1,Reward(w).Completed);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void BeginReplayCannotResetSavedDeliveryOrRerollDispatch()
        {var w=W();Begin(w);Deliver(w,0,80);var old=w.GangTow;Assert.True(Begin(w,save:()=>throw new Exception()));Assert.Same(old,w.GangTow);Assert.Single(w.GangTow.Pending.Damage);Assert.False(Begin(w,"again"));}
        [Fact] public void ReloadKeepsDeliveredQualityAndCapturedNextCallWithoutHandles()
        {var w=W();Begin(w);Deliver(w,0,80);string text=JsonConvert.SerializeObject(w);w=JsonConvert.DeserializeObject<WalletData>(text);Assert.True(w.GangTow.Valid);Assert.Equal(80,w.GangTow.Pending.Damage[0]);Assert.Equal("primo",w.GangTow.Pending.Dispatches[1].Model);Assert.DoesNotContain("Handle",text);Assert.True(Deliver(w,1,10));}
        [Fact] public void ReadyReloadCanConfirmWithoutCreatingTruckOrAnotherCall()
        {var w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(Ready()));Assert.True(w.GangTow.Pending.Ready);Assert.True(Complete(w));Assert.Equal(1,Reward(w).Completed);}
        [Fact] public void CancelIsFreeAndDoesNotRequireOwnerOrGrantPartialBonus()
        {var w=Ready();Assert.True(GangTowTransactions.Cancel(w,"tow",Save,out _));Assert.Null(w.GangTow.Pending);Assert.Empty(w.GangTow.Accounts);Assert.Equal(10000,w.Bank);Assert.True(GangTowTransactions.Cancel(w,"tow",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.False(Complete(w));}
        [Fact] public void CancelNewAttemptKeepsPreviouslyEarnedExpiry()
        {var w=Ready();Complete(w);long expiry=Reward(w).ExpiresUtcTicks;Begin(w,"again");Assert.True(GangTowTransactions.Cancel(w,"again",Save,out _));Assert.Equal(expiry,Reward(w).ExpiresUtcTicks);}
        [Fact] public void AnotherCompletedContractRefreshesRatherThanStacksDuration()
        {var w=Ready();Complete(w);Begin(w,"again");Deliver(w,0,0,"again");Deliver(w,1,0,"again");Assert.True(Complete(w,"again",Utc.AddMinutes(10)));Assert.Equal(2,Reward(w).Completed);Assert.Equal(Utc.AddMinutes(70).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void LostOwnerCannotDeliverOrClaimButCanCancel()
        {var w=W();Begin(w);Assert.False(GangTowTransactions.Deliver(w,new GangState{Gang="Other"},A(),"tow",0,0,Save,out _));Assert.Empty(w.GangTow.Pending.Damage);w=Ready();Assert.False(GangTowTransactions.Complete(w,new GangState{Gang="Other"},A(),"tow",Utc,Save,out _));Assert.True(w.GangTow.Pending.Ready);Assert.True(GangTowTransactions.Cancel(w,"tow",Save,out _));}
        [Fact] public void FailedBeginRestoresAbsentBook()
        {var w=W();Assert.Throws<Exception>(()=>Begin(w,save:()=>throw new Exception()));Assert.Null(w.GangTow);Assert.Equal(10000,w.Bank);}
        [Fact] public void FailedDeliveryKeepsSameReferenceProofAndAllowsRetry()
        {var w=W();Begin(w);var old=w.GangTow;Assert.Throws<Exception>(()=>Deliver(w,0,75,save:()=>throw new Exception()));Assert.Same(old,w.GangTow);Assert.Empty(w.GangTow.Pending.Damage);Assert.True(Deliver(w,0,75));}
        [Fact] public void FailedAwardAndCancelKeepReadyOriginalReference()
        {var w=Ready();var old=w.GangTow;Assert.Throws<Exception>(()=>Complete(w,save:()=>throw new Exception()));Assert.Same(old,w.GangTow);Assert.True(w.GangTow.Pending.Ready);Assert.Throws<Exception>(()=>GangTowTransactions.Cancel(w,"tow",()=>throw new Exception(),out _));Assert.Same(old,w.GangTow);}
        [Fact] public void UtcOnlyAwardAndExactHourWindowAreEnforced()
        {var w=Ready();Assert.False(Complete(w,utc:DateTime.SpecifyKind(Utc,DateTimeKind.Unspecified)));Assert.False(Complete(w,utc:new DateTime(DateTime.MaxValue.Ticks,DateTimeKind.Utc)));Assert.True(w.GangTow.Pending.Ready);Complete(w);Assert.Equal(2,GangTowTransactions.Multiplier(w.GangTow,S().Gang,3,Utc));Assert.Equal(2,GangTowTransactions.Multiplier(w.GangTow,S().Gang,3,Utc.AddHours(1).AddTicks(-1)));Assert.Equal(1,GangTowTransactions.Multiplier(w.GangTow,S().Gang,3,Utc.AddHours(1)));Assert.Equal(1,GangTowTransactions.Multiplier(w.GangTow,S().Gang,3,Utc.AddTicks(-1)));}
        [Fact] public void BonusIsIsolatedByProfileGangAreaAndNoOfflineIncome()
        {var w=Ready();Complete(w);Assert.Equal(1,GangTowTransactions.Multiplier(w.GangTow,"Other",3,Utc));Assert.Equal(1,GangTowTransactions.Multiplier(w.GangTow,S().Gang,2,Utc));Assert.Equal(1,GangTowTransactions.Multiplier(W().GangTow,S().Gang,3,Utc));Assert.Equal(1,GangTowTransactions.Multiplier(w.GangTow,S().Gang,3,Utc.AddDays(2)));Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);}
        [Fact] public void LegacyQueryDoesNotCreateBookOrOptIn()
        {var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":123,\"Future\":42}");Assert.Null(w.GangTow);Assert.False(GangTowTransactions.Blocked(w));Assert.Null(GangTowTransactions.Account(w.GangTow,S().Gang));Assert.Equal(1,GangTowTransactions.Multiplier(w.GangTow,S().Gang,3,Utc));Assert.Null(w.GangTow);}
        [Fact] public void UnknownFieldsSurviveCheckpointAwardAndRefresh()
        {var w=W();Begin(w);var j=JObject.FromObject(w);j["Future"]=42;j["GangTow"]["FutureBook"]="keep";j["GangTow"]["Pending"]["Dispatches"][0]["FutureCall"]="keep";w=j.ToObject<WalletData>();Deliver(w,0);Assert.Equal("keep",(string)JObject.FromObject(w)["GangTow"]["Pending"]["Dispatches"][0]["FutureCall"]);Deliver(w,1);Complete(w);j=JObject.FromObject(w);j["GangTow"]["Accounts"][0]["FutureAccount"]=true;w=j.ToObject<WalletData>();Begin(w,"again");Deliver(w,0,0,"again");Deliver(w,1,0,"again");Complete(w,"again",Utc.AddMinutes(2));j=JObject.FromObject(w);Assert.Equal(42,(int)j["Future"]);Assert.Equal("keep",(string)j["GangTow"]["FutureBook"]);Assert.True((bool)j["GangTow"]["Accounts"][0]["FutureAccount"]);}
        [Fact] public void InvalidBookProtectsAllTerritorialFlows()
        {var w=W();w.GangTow=new GangTowData{Accounts=null};Assert.False(Begin(w));Assert.True(GangBulkTransactions.Blocked(w));Assert.False(GangAutoArmsRules.Free(w));Assert.False(GangMechanicTransactions.OtherFree(w));Assert.False(GangPromotionTransactions.OtherFree(w));}
        [Fact] public void PendingTowBlocksCommerceFreightArmsAndAutoConfiguration()
        {var w=W();Begin(w);Assert.True(GangBulkTransactions.Blocked(w));Assert.False(GangAutoArmsRules.Free(w));Assert.True(GangTowTransactions.OtherFree(w));Assert.False(GangBusinessTransactions.Trade(w,S(),A(),new GangBusinessOptions(),false,Utc,"buy",Save,out _));Assert.False(GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"supply",Save,out _));Assert.False(GangArmsTransactions.Buy(w,S(),A(),new GangArmsOptions(),"pistol",1,"arms",Save,out _));Assert.False(GangAutoLaunderTransactions.Configure(w,S(),A(),true,"auto",Save,out _));Assert.Equal(10000,w.Bank);}
        [Fact] public void OtherPendingOrInvalidBooksBlockTowWithoutRecursiveValidation()
        {var w=W();w.GangMechanic=new GangMechanicData{Accounts=null};Assert.False(Begin(w));w=Ready();w.GangPromotion=new GangPromotionData{Accounts=null};Assert.False(Complete(w));Assert.True(w.GangTow.Pending.Ready);Assert.True(GangTowTransactions.Cancel(w,"tow",Save,out _));}
        [Fact] public void FullCounterOrAccountCapacityCannotStartUnfinishableContract()
        {var w=Ready();Complete(w);Reward(w).Completed=long.MaxValue;Assert.False(Begin(w,"again"));w=W();w.GangTow=new GangTowData();for(int n=0;n<640;n++)w.GangTow.Accounts.Add(new GangTowAccount{Gang="Other"+n,Completed=1,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks});Assert.True(w.GangTow.Valid);Assert.False(Begin(w));}
        [Fact] public void DamageMustBeFiniteBoundedAndCannotEnterInvalidSavedHistory()
        {var w=W();Begin(w);foreach(float n in new[]{-1,float.NaN,float.PositiveInfinity,801})Assert.False(Deliver(w,0,n));Assert.Empty(w.GangTow.Pending.Damage);Assert.True(Deliver(w,0,800));}
        [Fact] public void ReceiptCapAndDeliveryReplayAreStable()
        {var w=W();Begin(w);w.GangTow.Receipts=Enumerable.Range(0,256).Select(n=>"old"+n).ToList();Deliver(w,0);Assert.Equal(256,w.GangTow.Receipts.Count);Assert.DoesNotContain("old0",w.GangTow.Receipts);Assert.True(Deliver(w,0,save:()=>throw new Exception()));Assert.Single(w.GangTow.Pending.Damage);}
        [Fact] public void DamageClassificationMatchesSourceSeventyFiveAndTwoHundred()
        {Assert.Equal("excelente",GangTowRules.Condition(74.99f));Assert.Equal("boa",GangTowRules.Condition(75));Assert.Equal("boa",GangTowRules.Condition(199.99f));Assert.Equal("ruim",GangTowRules.Condition(200));Assert.Equal("ruim",GangTowRules.Condition(800));Assert.Equal("invalida",GangTowRules.Condition(float.NaN));}
        [Fact] public void HookRequiresOwnDriverEmptyExactPairArmAndRearDistance()
        {Assert.True(GangTowRules.Hook(true,true,true,true,.2f,3,.99f));Assert.False(GangTowRules.Hook(false,true,true,true,0,0,0));Assert.False(GangTowRules.Hook(true,false,true,true,0,0,0));Assert.False(GangTowRules.Hook(true,true,false,true,0,0,0));Assert.False(GangTowRules.Hook(true,true,true,false,0,0,0));Assert.False(GangTowRules.Hook(true,true,true,true,.201f,0,0));Assert.False(GangTowRules.Hook(true,true,true,true,0,3.01f,0));Assert.False(GangTowRules.Hook(true,true,true,true,0,0,1));Assert.False(GangTowRules.Hook(true,true,true,true,float.NaN,0,0));}
        [Fact] public void DropChecksBothVehiclesYardDistancesAndNativeConnection()
        {Assert.True(GangTowRules.Drop(true,true,true,true,true,.2f,12,16,.99f));Assert.False(GangTowRules.Drop(true,true,true,false,true,0,0,0,0));Assert.False(GangTowRules.Drop(true,true,true,true,false,0,0,0,0));Assert.False(GangTowRules.Drop(true,true,true,true,true,0,12.01f,0,0));Assert.False(GangTowRules.Drop(true,true,true,true,true,0,0,16.01f,0));Assert.False(GangTowRules.Drop(true,true,true,true,true,0,0,0,1));Assert.False(GangTowRules.Drop(true,true,false,true,true,0,0,0,0));}
        [Fact] public void AccidentalDetachAndMovingCargoCannotBecomeDeliveryProof()
        {Assert.True(GangTowRules.Delivered(true,true,true,true));Assert.False(GangTowRules.Delivered(false,true,true,true));Assert.False(GangTowRules.Delivered(true,false,true,true));Assert.False(GangTowRules.Delivered(true,true,false,true));Assert.False(GangTowRules.Delivered(true,true,true,false));}
        [Fact] public void CleanupCannotDeleteOccupiedMovingForeignOrUntaggedVehicles()
        {Assert.True(GangTowRules.Retire(true,0,.49f,false));Assert.False(GangTowRules.Retire(false,0,0,false));Assert.False(GangTowRules.Retire(true,1,0,false));Assert.False(GangTowRules.Retire(true,0,.5f,false));Assert.False(GangTowRules.Retire(true,0,0,true));Assert.False(GangTowRules.Retire(true,0,float.NaN,false));}
        [Fact] public void CraneFollowsSourceRateAndClampsAtBothEnds()
        {var c=new GangTowCrane(0);c.Step(350,true,true,false);Assert.Equal(.25f,c.Height,5);c.Step(1750,true,true,false);Assert.Equal(1,c.Height);c.Step(3150,true,false,true);Assert.Equal(0,c.Height,5);}
        [Fact] public void CraneIgnoresMenusLongGapsBothKeysAndGameTimeWrapIsSafe()
        {var c=new GangTowCrane(0);c.Step(350,false,true,false);Assert.Equal(0,c.Height);c.Step(5000,true,true,false);Assert.Equal(0,c.Height);c.Step(5350,true,true,true);Assert.Equal(0,c.Height);c=new GangTowCrane(int.MaxValue-174);c.Step(unchecked(int.MaxValue+176),true,true,false);Assert.Equal(.25f,c.Height,5);}
        [Fact] public void EarnedTowBoostUsesExistingBatchAndEightyFivePercentTreasury()
        {var w=Ready();Complete(w);w.GangIllicit=new GangIllicitData();w.GangIllicit.Accounts.Add(new GangIllicitAccount{Gang=S().Gang,Area=3,Dirty=1500,Proceeds=1500});Assert.True(GangAutoLaunderTransactions.Configure(w,S(),A(),true,"on",Save,out _));var c=new GangAutoArmsClock(0);for(int n=1;n<30;n++)Assert.False(c.Step(n*1000,true,60,GangTowTransactions.Multiplier(w.GangTow,S().Gang,3,Utc)));Assert.True(c.Step(30000,true,60,2));Assert.True(GangAutoLaunderTransactions.Process(w,S(),A(),new GangAutoLaunderOptions(),Utc,"process",Save,out _));Assert.Equal(1000,w.GangIllicit.Accounts[0].Dirty);Assert.Equal(425,w.GangTreasury.Accounts[S().Gang].Balance);}
        [Fact] public void OtherAreaBonusesAreNotOverwrittenOrMultiplied()
        {var w=Ready();w.GangMechanic=new GangMechanicData();w.GangMechanic.Accounts.Add(new GangMechanicAccount{Gang=S().Gang,Completed=1,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks});var m=w.GangMechanic;Complete(w);Assert.Same(m,w.GangMechanic);Assert.Equal(2,Math.Max(GangMechanicTransactions.Multiplier(m,S().Gang,2,Utc),GangTowTransactions.Multiplier(w.GangTow,S().Gang,2,Utc)));Assert.Equal(2,Math.Max(GangMechanicTransactions.Multiplier(m,S().Gang,3,Utc),GangTowTransactions.Multiplier(w.GangTow,S().Gang,3,Utc)));}
        [Fact] public void InvalidSnapshotAndOptionsAreProtected()
        {var w=W();Begin(w);w.GangTow.Pending.Dispatches[0].Model="unknown";Assert.False(w.GangTow.Valid);Assert.False(Deliver(w,0));Assert.False(new GangTowOptions{TruckModel="asea"}.Valid);Assert.False(new GangTowOptions{Goal=11}.Valid);Assert.False(new GangTowOptions{BoostMinutes=181}.Valid);Assert.False(new GangTowOptions{Sites=null}.Valid);Assert.False(new GangTowOptions{Customers=null}.Valid);}
        [Fact] public void MatchingDispatchCannotMakeInvalidSavedOptionsUsable()
        {var w=W();Begin(w);w.GangTow.Pending.Options.BoostMinutes=0;Assert.False(w.GangTow.Valid);Assert.False(Deliver(w,0));Assert.False(Complete(w));Assert.Empty(w.GangTow.Pending.Damage);}
    }
}
