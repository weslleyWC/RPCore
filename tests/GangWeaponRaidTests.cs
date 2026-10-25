using System;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangWeaponRaidTests
    {
        static readonly Action Save=()=>{};
        static GangTurfDefinition A()=>GangTurfOptions.Defaults().Areas[0];
        static GangState S()=>new GangState{Gang=A().DefaultOwner,Reputation=800,Rank=3};
        static GangArmsOptions C()=>new GangArmsOptions();
        static WalletData W()=>new WalletData{Bank=10000,Cash=123,Dealer=new DealerData{Stock={{"weed",5}}}};
        static GangRaidAttempt P()=>new GangRaidAttempt{Kind=GangRaidKind.WeaponVan,Id="weapons",Gang=S().Gang,Rival="Other",Area=A(),Site=new GangWeaponRaidOptions().Sites[0],WeaponOptions=new GangWeaponRaidOptions(),Capacity=999,Models=new[]{"g_m_y_famca_01","g_m_y_famca_01","g_m_y_famca_01"},Loot={{"pistol",5},{"ammo",150}}};
        static bool Begin(WalletData w,GangRaidAttempt p=null,Action save=null,GangArmsOptions c=null)=>GangWeaponRaidTransactions.Begin(w,S(),p??P(),c??C(),save??Save,out _);
        static WalletData Started(){var w=W();Assert.True(Begin(w));return w;}
        static WalletData Defeated(){var w=Started();for(int i=0;i<3;i++)Assert.True(GangRaidTransactions.Defeat(w,"weapons",i,Save,out _));return w;}
        static WalletData Captured(){var w=Defeated();Assert.True(GangWeaponRaidTransactions.Capture(w,"weapons",Save,out _));return w;}
        static WalletData Delivered(){var w=Captured();Assert.True(GangWeaponRaidTransactions.Deliver(w,"weapons",Save,out _));return w;}
        static bool Complete(WalletData w,Action save=null,GangArmsOptions c=null)=>GangWeaponRaidTransactions.Complete(w,S(),A(),c??C(),"weapons",save??Save,out _);
        [Fact] public void DefaultsHaveVanThreeReferenceSitesAndBoundedTime()
        {var c=new GangWeaponRaidOptions();Assert.True(c.Valid);Assert.Equal("gburrito2",c.VehicleModel);Assert.Equal(3,c.Sites.Count);Assert.Equal(1200,c.SecondsLimit);Assert.Equal(120,c.Radius);Assert.True(P().Valid);Assert.Null(P().Options);}
        [Fact] public void GeneratedLootAlwaysHasPistolsAmmoAndBoundedOptionalCategories()
        {for(int seed=0;seed<300;seed++){var loot=GangWeaponRaidRules.Loot(new Random(seed));Assert.True(GangWeaponRaidRules.ValidLoot(loot));Assert.InRange(loot["pistol"],2,6);Assert.InRange(loot["ammo"],100,250);}Assert.Throws<ArgumentNullException>(()=>GangWeaponRaidRules.Loot(null));}
        [Fact] public void InvalidLootKindProofOrCapacityCannotBecomeValidRaid()
        {var p=P();p.Loot["weed"]=1;Assert.False(p.Valid);p=P();p.Loot["ammo"]=251;Assert.False(p.Valid);p=P();p.Loot.Remove("pistol");Assert.False(p.Valid);p=P();p.Capacity=999.5f;Assert.False(p.Valid);p=P();p.Kind=(GangRaidKind)2;Assert.False(p.Valid);p=P();p.VanDelivered=true;Assert.False(p.Valid);}
        [Fact] public void StartIsFreeAndDoesNotCreateAnyStockOrChangeMoney()
        {var w=Started();Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Equal(5,w.Dealer.Stock["weed"]);Assert.Null(w.GangArms);Assert.Null(w.GangWarehouse);Assert.Null(w.GangIllicit);Assert.Null(w.GangDiplomacy);Assert.Null(w.GangTreasury);Assert.Equal(1,w.GangRaid.Attempts);}
        [Fact] public void CallerCannotAlterCapturedLootModelsLocationOrOptions()
        {var w=W();var p=P();Begin(w,p);p.Loot["ammo"]=250;p.Models[0]="changed";p.Site.X=0;p.WeaponOptions.VehicleModel="asea";Assert.Equal(150,w.GangRaid.Pending.Loot["ammo"]);Assert.NotEqual("changed",w.GangRaid.Pending.Models[0]);Assert.NotEqual(0,w.GangRaid.Pending.Site.X);Assert.Equal("gburrito2",w.GangRaid.Pending.WeaponOptions.VehicleModel);}
        [Fact] public void StartRequiresCurrentOwnerCapacityAndKnownLootCatalog()
        {var w=W();var s=S();s.Gang="Other";Assert.False(GangWeaponRaidTransactions.Begin(w,s,P(),C(),Save,out _));var p=P();p.Capacity=998;Assert.False(Begin(w,p));var c=C();c.Products.RemoveAll(v=>v.Id=="ammo");Assert.False(Begin(w,c:c));Assert.Null(w.GangRaid);}
        [Fact] public void InsufficientExistingCapacityRefusesBeforeAttemptWithoutDeletingStock()
        {var w=W();w.GangArms=new GangArmsData();w.GangArms.Accounts.Add(new GangArmsAccount{Gang=S().Gang,Area=A().Id,Stock={{"ammo",900}},Purchased={{"ammo",900}}});var old=w.GangArms;Assert.False(Begin(w));Assert.Same(old,w.GangArms);Assert.Null(w.GangRaid);}
        [Fact] public void ThreeDistinctGuardDefeatsAreRequiredBeforeCapture()
        {var w=Started();Assert.False(GangWeaponRaidTransactions.Capture(w,"weapons",Save,out _));GangRaidTransactions.Defeat(w,"weapons",0,Save,out _);GangRaidTransactions.Defeat(w,"weapons",1,Save,out _);Assert.False(GangWeaponRaidTransactions.Capture(w,"weapons",Save,out _));GangRaidTransactions.Defeat(w,"weapons",2,Save,out _);Assert.True(GangWeaponRaidTransactions.Capture(w,"weapons",Save,out _));Assert.Null(w.GangArms);Assert.Null(w.GangDiplomacy);}
        [Fact] public void VanCaptureAndPhysicalDeliveryAreSeparateRequiredCheckpoints()
        {var w=Defeated();Assert.False(GangWeaponRaidTransactions.Deliver(w,"weapons",Save,out _));Assert.False(Complete(w));GangWeaponRaidTransactions.Capture(w,"weapons",Save,out _);Assert.False(Complete(w));GangWeaponRaidTransactions.Deliver(w,"weapons",Save,out _);Assert.True(Complete(w));}
        [Fact] public void CompletionAddsOnlyArsenalRecoveredAndNoMoneyOrRelationReward()
        {var w=Delivered();var dealer=w.Dealer;Assert.True(Complete(w));Assert.Null(w.GangRaid.Pending);Assert.Equal(1,w.GangRaid.Completed);var a=w.GangArms.Accounts[0];Assert.Equal(5,a.Stock["pistol"]);Assert.Equal(150,a.Stock["ammo"]);Assert.Equal(5,a.Recovered["pistol"]);Assert.Equal(150,a.Recovered["ammo"]);Assert.Empty(a.Purchased);Assert.Empty(a.Sold);Assert.Equal(0,a.Spent);Assert.Equal(0,a.Revenue);Assert.True(a.Valid);Assert.Same(dealer,w.Dealer);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangWarehouse);Assert.Null(w.GangIllicit);Assert.Null(w.GangDiplomacy);Assert.Null(w.GangTreasury);}
        [Fact] public void CompletionPreservesExistingPurchasedStockAndDiplomacyReferences()
        {var w=Delivered();w.GangArms=new GangArmsData();w.GangArms.Accounts.Add(new GangArmsAccount{Gang=S().Gang,Area=A().Id,Stock={{"pistol",2}},Purchased={{"pistol",2}},Spent=1800});w.GangDiplomacy=new GangDiplomacyData();w.GangDiplomacy.Accounts.Add(new GangDiplomacyAccount{Gang=S().Gang,Rival="Other",Attitude="War",StolenLoads=2});var d=w.GangDiplomacy;Assert.True(Complete(w));Assert.Equal(7,w.GangArms.Accounts[0].Stock["pistol"]);Assert.Equal(2,w.GangArms.Accounts[0].Purchased["pistol"]);Assert.Equal(1800,w.GangArms.Accounts[0].Spent);Assert.Same(d,w.GangDiplomacy);Assert.Equal(2,d.Accounts[0].StolenLoads);}
        [Fact] public void CaptureDeliverAndCompleteReplaysAfterReloadDoNotDuplicate()
        {var w=Delivered();w=CharacterRules.Copy(w);Assert.True(GangWeaponRaidTransactions.Capture(w,"weapons",()=>throw new Exception(),out bool capture));Assert.False(capture);Assert.True(GangWeaponRaidTransactions.Deliver(w,"weapons",()=>throw new Exception(),out bool deliver));Assert.False(deliver);Assert.True(Complete(w));w=CharacterRules.Copy(w);Assert.True(Complete(w,()=>throw new Exception()));Assert.Equal(5,w.GangArms.Accounts[0].Stock["pistol"]);Assert.Equal(1,w.GangRaid.Completed);}
        [Fact] public void RestartEvenAfterCaptureRequiresFreshFightAndDrivingWithoutReroll()
        {var w=Captured();Assert.True(GangWeaponRaidTransactions.Restart(w,"weapons",Save,out _));Assert.False(w.GangRaid.Pending.Package);Assert.Empty(w.GangRaid.Pending.Defeated);Assert.Equal(150,w.GangRaid.Pending.Loot["ammo"]);Assert.Equal(1,w.GangRaid.Attempts);Assert.False(GangWeaponRaidTransactions.Capture(w,"weapons",Save,out _));for(int i=0;i<3;i++)Assert.True(GangRaidTransactions.Defeat(w,"weapons",i,Save,out bool changed) && changed);Assert.True(GangWeaponRaidTransactions.Capture(w,"weapons",Save,out bool captured) && captured);}
        [Fact] public void DeliveredStateSurvivesReloadAndCannotRestartOrRequireVanAgain()
        {var w=CharacterRules.Copy(Delivered());Assert.True(w.GangRaid.Pending.VanDelivered);Assert.False(GangWeaponRaidTransactions.Restart(w,"weapons",Save,out _));Assert.True(Complete(w));}
        [Theory][InlineData(false)][InlineData(true)]
        public void CancelOrFailAfterPhysicalReturnDoesNotCreateLootOrEraseOldStock(bool failed)
        {var w=Delivered();w.GangArms=new GangArmsData();w.GangArms.Accounts.Add(new GangArmsAccount{Gang=S().Gang,Area=A().Id,Stock={{"pistol",1}},Purchased={{"pistol",1}}});var old=w.GangArms;Assert.True(GangRaidTransactions.End(w,"weapons",failed,Save,out _));Assert.Same(old,w.GangArms);Assert.Equal(1,old.Accounts[0].Stock["pistol"]);Assert.Equal(failed?1:0,w.GangRaid.Failed);Assert.False(Complete(w));}
        [Fact] public void FailedBeginRestoresAbsentBookAndNoStock()
        {var w=W();Assert.Throws<Exception>(()=>Begin(w,save:()=>throw new Exception()));Assert.Null(w.GangRaid);Assert.Null(w.GangArms);Assert.Equal(10000,w.Bank);}
        [Theory][InlineData("capture")][InlineData("deliver")][InlineData("restart")]
        public void FailedCheckpointRestoresOriginalReference(string step)
        {var w=step=="capture"?Defeated():Captured();var old=w.GangRaid;Assert.Throws<Exception>(()=>{if(step=="capture")GangWeaponRaidTransactions.Capture(w,"weapons",()=>throw new Exception(),out _);if(step=="deliver")GangWeaponRaidTransactions.Deliver(w,"weapons",()=>throw new Exception(),out _);if(step=="restart")GangWeaponRaidTransactions.Restart(w,"weapons",()=>throw new Exception(),out _);});Assert.Same(old,w.GangRaid);Assert.Null(w.GangArms);}
        [Fact] public void FailedFinalSaveRestoresRaidArmsReferencesAndOldBalances()
        {var w=Delivered();var old=w.GangRaid;Assert.Throws<Exception>(()=>Complete(w,()=>{Assert.Null(w.GangRaid.Pending);Assert.Equal(150,w.GangArms.Accounts[0].Stock["ammo"]);throw new Exception();}));Assert.Same(old,w.GangRaid);Assert.Null(w.GangArms);Assert.True(w.GangRaid.Pending.VanDelivered);Assert.Equal(10000,w.Bank);}
        [Fact] public void FailedFinalSaveRestoresExistingRecoveredHistory()
        {var w=Delivered();w.GangArms=new GangArmsData();w.GangArms.Accounts.Add(new GangArmsAccount{Gang=S().Gang,Area=A().Id,Stock={{"pistol",2}},Recovered={{"pistol",2}}});var old=w.GangArms;Assert.Throws<Exception>(()=>Complete(w,()=>throw new Exception()));Assert.Same(old,w.GangArms);Assert.Equal(2,old.Accounts[0].Recovered["pistol"]);}
        [Fact] public void CurrentOwnerAndAreaMustMatchWhileCancelRemainsAvailable()
        {var w=Delivered();var s=S();s.Gang="Other";Assert.False(GangWeaponRaidTransactions.Complete(w,s,A(),C(),"weapons",Save,out _));Assert.False(GangWeaponRaidTransactions.Complete(w,S(),GangTurfOptions.Defaults().Areas[1],C(),"weapons",Save,out _));Assert.True(w.GangRaid.Pending.VanDelivered);Assert.True(GangRaidTransactions.End(w,"weapons",false,Save,out _));}
        [Fact] public void MissingSkuOrNewlyFullStockBlocksCreditWithoutDiscardingReturnProof()
        {var w=Delivered();var c=C();c.Products.RemoveAll(p=>p.Id=="ammo");Assert.False(Complete(w,c:c));w.GangArms=new GangArmsData();w.GangArms.Accounts.Add(new GangArmsAccount{Gang=S().Gang,Area=A().Id,Stock={{"ammo",900}},Purchased={{"ammo",900}}});Assert.False(Complete(w));Assert.True(w.GangRaid.Pending.VanDelivered);}
        [Fact] public void CapturedCapacityDoesNotChangeAfterEditingArmsConfig()
        {var w=Delivered();var c=C();c.MaxStock=10;Assert.True(Complete(w,c:c));Assert.Equal(150,w.GangArms.Accounts[0].Stock["ammo"]);}
        [Fact] public void RecoveredHistoryOverflowRefusesWithoutPartialStock()
        {var w=Delivered();w.GangArms=new GangArmsData();w.GangArms.Accounts.Add(new GangArmsAccount{Gang=S().Gang,Area=A().Id,Stock={{"ammo",0}},Recovered={{"ammo",long.MaxValue}},Sold={{"ammo",long.MaxValue}}});Assert.True(w.GangArms.Valid);Assert.False(Complete(w));Assert.Equal(0,w.GangArms.Accounts[0].Stock["ammo"]);Assert.True(w.GangRaid.Pending.VanDelivered);}
        [Fact] public void DrugHandlersCannotCollectRestartOrCreditWeaponRaid()
        {var w=Defeated();Assert.False(GangRaidTransactions.Collect(w,"weapons",Save,out _));Assert.False(GangRaidTransactions.Restart(w,"weapons",Save,out _));Assert.False(GangRaidTransactions.Complete(w,S(),A(),DealerDefaults.Load(),"weapons",Save,out _));Assert.Null(w.GangDiplomacy);Assert.Null(w.GangWarehouse);Assert.False(GangRaidTransactions.Begin(W(),S(),P(),DealerDefaults.Load(),Save,out _));}
        [Fact] public void WeaponHandlersRefuseDrugRaidAndDoNotUseItsBodySearchProof()
        {var p=P();p.Kind=GangRaidKind.Drugs;p.Options=new GangRaidOptions();p.Capacity=30;p.Loot=new System.Collections.Generic.Dictionary<string,int>{{"weed",40},{"mdma",50}};var w=W();Assert.True(GangRaidTransactions.Begin(w,S(),p,DealerDefaults.Load(),Save,out _));for(int i=0;i<3;i++)GangRaidTransactions.Defeat(w,"weapons",i,Save,out _);Assert.False(GangWeaponRaidTransactions.Capture(w,"weapons",Save,out _));Assert.False(GangWeaponRaidTransactions.Deliver(w,"weapons",Save,out _));Assert.False(GangWeaponRaidTransactions.Restart(w,"weapons",Save,out _));}
        [Fact] public void ExistingCargoSlotBlocksWeaponRaidAndWeaponRaidBlocksOtherContracts()
        {var w=W();w.GangBulk=new GangBulkData{Attempts=-1};Assert.False(Begin(w));w=W();w.GangBusiness=new GangBusinessData{Supply=new GangSupplyOrder()};Assert.False(Begin(w));w=Started();Assert.False(GangArmsTransactions.Buy(w,S(),A(),C(),"pistol",1,"buy",Save,out _));Assert.False(GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"freight",Save,out _));var next=P();next.Id="another";Assert.False(Begin(w,next));}
        [Fact] public void RecoveredWeaponsCanUseExistingPaidSaleAndProcessingCycle()
        {var w=Delivered();Complete(w);var p=new GangBulkAttempt{Kind=GangBulkKind.Arms,ArmsLimit=10,Id="sell",Gang=S().Gang,Buyer="Customer",Product="pistol",Units=5,Value=6000,Model="a_m_m_bevhills_02",Area=A(),Site=C().Delivery.Sites[0],Options=C().Delivery};Assert.True(GangArmsTransactions.BeginDelivery(w,S(),p,C(),Save,out _));GangBulkTransactions.Load(w,"sell",Save,out _);GangBulkTransactions.Ready(w,"sell",Save,out _);Assert.True(GangArmsTransactions.CompleteDelivery(w,S(),A(),C(),"sell",Save,out _));var a=w.GangArms.Accounts[0];Assert.Equal(0,a.Stock["pistol"]);Assert.Equal(5,a.Recovered["pistol"]);Assert.Equal(5,a.Sold["pistol"]);Assert.Empty(a.Purchased);Assert.Equal(6000,w.GangIllicit.Accounts[0].Dirty);Assert.True(a.Valid);}
        [Fact] public void LegacyArmsWithoutRecoveredAndDrugRaidWithoutKindStillLoad()
        {var a=JObject.Parse("{Gang:'Mine',Area:1,Stock:{pistol:5},Purchased:{pistol:5},Sold:{}}").ToObject<GangArmsAccount>();Assert.True(a.Valid);Assert.Empty(a.Recovered);var p=P();p.Kind=GangRaidKind.Drugs;p.Options=new GangRaidOptions();p.Capacity=30;p.Loot=new System.Collections.Generic.Dictionary<string,int>{{"weed",40},{"mdma",50}};var j=JObject.FromObject(p);j.Remove("Kind");j.Remove("WeaponOptions");j.Remove("VanDelivered");p=j.ToObject<GangRaidAttempt>();Assert.Equal(GangRaidKind.Drugs,p.Kind);Assert.True(p.Valid);}
        [Fact] public void UnknownFieldsSurviveSnapshotAndFinalTwoBookUpdate()
        {var w=Delivered();var j=JObject.FromObject(w);j["Future"]="wallet";j["GangRaid"]["Future"]="raid";j["GangRaid"]["Pending"]["WeaponOptions"]["Future"]="options";w=j.ToObject<WalletData>();Assert.Equal("options",(string)JObject.FromObject(w)["GangRaid"]["Pending"]["WeaponOptions"]["Future"]);w.GangArms=JObject.Parse("{Accounts:[],Receipts:[],Future:'arms'}").ToObject<GangArmsData>();Complete(w);j=JObject.FromObject(w);Assert.Equal("wallet",(string)j["Future"]);Assert.Equal("raid",(string)j["GangRaid"]["Future"]);Assert.Equal("arms",(string)j["GangArms"]["Future"]);}
        [Fact] public void ProfilesHaveIndependentFreeCargoAndStock()
        {var w=Delivered();var other=W();Complete(w);Assert.Null(other.GangRaid);Assert.Null(other.GangArms);Assert.Equal(10000,other.Bank);}
        [Fact] public void ConfigSitesReplaceDefaultsAndBoundsAreProtected()
        {var c=JObject.Parse("{VehicleModel:'gburrito2',SecondsLimit:1200,Radius:120,Sites:[{Name:'Only',X:100,Y:100,Z:30}]}").ToObject<GangWeaponRaidOptions>();Assert.True(c.Valid);Assert.Single(c.Sites);c.Radius=151;Assert.False(c.Valid);c.Radius=120;c.SecondsLimit=119;Assert.False(c.Valid);c.SecondsLimit=1200;c.VehicleModel="bad/name";Assert.False(c.Valid);}
        [Theory][InlineData(true,0,0,true)][InlineData(true,1,0,false)][InlineData(true,0,1,false)][InlineData(false,0,0,false)][InlineData(true,0,-1,false)]
        public void RetirementRequiresOwnEmptyStoppedVan(bool own,int occupants,float speed,bool expected)
        {Assert.Equal(expected,GangWeaponRaidRules.CanRetire(own,occupants,speed));Assert.False(GangWeaponRaidRules.CanRetire(true,0,float.NaN));Assert.False(GangWeaponRaidRules.CanRetire(true,0,float.PositiveInfinity));}
    }
}
