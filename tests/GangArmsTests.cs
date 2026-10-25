using System;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangArmsTests
    {
        static readonly Action Save=()=>{};
        static GangTurfDefinition A()=>GangTurfOptions.Defaults().Areas[0];
        static GangState S()=>new GangState{Gang=A().DefaultOwner,Reputation=800,Rank=3};
        static GangArmsOptions C()=>new GangArmsOptions();
        static WalletData W()=>new WalletData{Bank=100000,Cash=123,Dealer=new DealerData{Stock={{"weed",5}}}};
        static bool Buy(WalletData w,int units=5,string product="pistol",string id="buy",Action save=null,GangArmsOptions c=null)=>GangArmsTransactions.Buy(w,S(),A(),c??C(),product,units,id,save??Save,out _);
        static GangBulkAttempt P(string product="pistol",int units=5)=>new GangBulkAttempt{Kind=GangBulkKind.Arms,ArmsLimit=10,Id="arms",Gang=S().Gang,Buyer="Comprador particular",Product=product,Units=units,Value=GangArmsTransactions.Quote(C(),product,units),Model="a_m_m_bevhills_02",Area=A(),Site=C().Delivery.Sites[0],Options=C().Delivery};
        static WalletData Bought(){var w=W();Assert.True(Buy(w));return w;}
        static bool Begin(WalletData w,GangBulkAttempt p=null,Action save=null)=>GangArmsTransactions.BeginDelivery(w,S(),p??P(),C(),save??Save,out _);
        static WalletData Started(){var w=Bought();Assert.True(Begin(w));return w;}
        static WalletData Ready(){var w=Started();Assert.True(GangBulkTransactions.Load(w,"arms",Save,out _));Assert.True(GangBulkTransactions.Ready(w,"arms",Save,out _));return w;}
        static bool Complete(WalletData w,Action save=null,GangArmsOptions c=null)=>GangArmsTransactions.CompleteDelivery(w,S(),A(),c??C(),"arms",save??Save,out _);
        static GangArmsAccount Account(WalletData w)=>GangArmsTransactions.Account(w.GangArms,S().Gang,A().Id);
        [Fact] public void DefaultsMatchFiveReferencePricesAndShipmentAmounts()
        {var c=C();Assert.True(c.Valid);Assert.Equal(5,c.Products.Count);Assert.Equal(new[]{1,5,10,25,50},c.Delivery.Quantities);Assert.Equal(1200,c.Products.Find(p=>p.Id=="pistol").Sell);Assert.Equal(2500,c.Products.Find(p=>p.Id=="smg").Sell);Assert.Equal(4500,c.Products.Find(p=>p.Id=="rifle").Sell);Assert.Equal(3200,c.Products.Find(p=>p.Id=="shotgun").Sell);Assert.Equal(3,c.Products.Find(p=>p.Id=="ammo").Sell);Assert.Equal(6,c.Delivery.Sites.Count);}
        [Fact] public void PaidPurchaseDebitsOnlyBankAndCreatesSeparateConservedStock()
        {var w=W();var dealer=w.Dealer;Assert.True(Buy(w));Assert.Equal(95500,w.Bank);Assert.Equal(123,w.Cash);Assert.Same(dealer,w.Dealer);Assert.Null(w.GangWarehouse);Assert.Null(w.GangIllicit);Assert.Null(w.GangDiplomacy);Assert.Null(w.GangBulk);Assert.Equal(5,Account(w).Stock["pistol"]);Assert.Equal(5,Account(w).Purchased["pistol"]);Assert.Equal(4500,Account(w).Spent);Assert.True(w.GangArms.Valid);}
        [Fact] public void BuyReplayAfterReloadDoesNotDebitOrSaveAgainEvenWithInsufficientRemainingBank()
        {var w=Bought();w.Bank=0;w=CharacterRules.Copy(w);Assert.True(Buy(w,save:()=>throw new Exception()));Assert.Equal(0,w.Bank);Assert.Equal(5,Account(w).Stock["pistol"]);}
        [Fact] public void InsufficientFundsInvalidQuantitySkuOrOwnershipNeverCreatesStock()
        {var w=W();w.Bank=899;Assert.False(Buy(w,1));Assert.False(Buy(W(),2));Assert.False(Buy(W(),product:"missing"));var s=S();s.Gang="Other";Assert.False(GangArmsTransactions.Buy(w,s,A(),C(),"pistol",1,"other",Save,out _));Assert.Null(w.GangArms);}
        [Fact] public void MaxStockAndLifetimePurchaseOverflowRefuseWithoutDebit()
        {var w=Bought();var c=C();c.MaxStock=5;Assert.False(Buy(w,1,id:"full",c:c));var a=Account(w);a.Purchased["pistol"]=long.MaxValue-2;a.Sold["pistol"]=long.MaxValue-7;Assert.True(w.GangArms.Valid);Assert.False(Buy(w,id:"overflow"));Assert.Equal(95500,w.Bank);Assert.Equal(5,a.Stock["pistol"]);}
        [Fact] public void SpendingOverflowPreservesStockAndBank()
        {var w=Bought();Account(w).Spent=long.MaxValue;Assert.False(Buy(w,id:"overflow"));Assert.Equal(95500,w.Bank);Assert.Equal(5,Account(w).Stock["pistol"]);}
        [Fact] public void FailedFirstPurchaseRestoresAbsentBookAndBank()
        {var w=W();Assert.Throws<Exception>(()=>Buy(w,save:()=>throw new Exception()));Assert.Null(w.GangArms);Assert.Equal(100000,w.Bank);}
        [Fact] public void FailedAdditionalPurchaseRestoresOriginalReference()
        {var w=Bought();var old=w.GangArms;Assert.Throws<Exception>(()=>Buy(w,id:"new",save:()=>throw new Exception()));Assert.Same(old,w.GangArms);Assert.Equal(95500,w.Bank);Assert.Equal(5,Account(w).Stock["pistol"]);}
        [Fact] public void InvalidArmsBookIsProtectedAndNeverReplaced()
        {var w=Bought();w.GangArms.Accounts[0].Stock["pistol"]=999;var old=w.GangArms;Assert.False(Buy(w,id:"new"));Assert.False(Begin(w));Assert.Same(old,w.GangArms);Assert.Equal(95500,w.Bank);}
        [Fact] public void QuoteHasNoDrugMultiplierAndRespectsDeliveryCap()
        {Assert.Equal(6000,GangArmsTransactions.Quote(C(),"pistol",5));Assert.Equal(150,GangArmsTransactions.Quote(C(),"ammo",50));Assert.Equal(0,GangArmsTransactions.Quote(C(),"rifle",50));Assert.Equal(0,GangArmsTransactions.Quote(null,"pistol",1));Assert.Equal(0,GangArmsTransactions.Quote(C(),"missing",1));}
        [Fact] public void ArmsShipmentHasExplicitKindAndTenWeaponSlotsForOneTerritory()
        {var p=P();Assert.True(p.Valid);p.Units=25;p.Value=30000;Assert.False(p.Valid);p.ArmsLimit=30;Assert.True(p.Valid);p.ArmsLimit=0;Assert.False(p.Valid);p.ArmsLimit=151;Assert.False(p.Valid);}
        [Fact] public void AmmoUsesCeilingOfUnitsPerHundredForSlotLimit()
        {var p=P("ammo",50);p.ArmsLimit=1;Assert.True(p.Valid);p.Units=101;p.Value=303;p.Options.Quantities.Add(101);Assert.False(p.Valid);p.ArmsLimit=2;Assert.True(p.Valid);}
        [Fact] public void BeginCommitsStockLogicallyWithoutTakingAnotherPayment()
        {var w=Bought();var arms=w.GangArms;Assert.True(Begin(w));Assert.Same(arms,w.GangArms);Assert.Equal(5,Account(w).Stock["pistol"]);Assert.Equal(95500,w.Bank);Assert.Null(w.GangWarehouse);Assert.Null(w.GangIllicit);Assert.True(GangBulkTransactions.Blocked(w));Assert.Equal(GangBulkKind.Arms,w.GangBulk.Pending.Kind);}
        [Fact] public void BeginRequiresStoredWeaponsAndValidCapturedQuote()
        {var w=W();Assert.False(Begin(w));w=Bought();var p=P();p.Value++;Assert.False(Begin(w,p));p=P();p.Kind=GangBulkKind.Drugs;Assert.False(Begin(w,p));p=P();p.Loaded=true;Assert.False(Begin(w,p));Assert.Null(w.GangBulk);}
        [Fact] public void StartSnapshotCannotBeAlteredByCaller()
        {var w=Bought();var p=P();Begin(w,p);p.Kind=GangBulkKind.Drugs;p.ArmsLimit=150;p.Value=1;p.Options.HandoverSeconds=9;Assert.Equal(GangBulkKind.Arms,w.GangBulk.Pending.Kind);Assert.Equal(10,w.GangBulk.Pending.ArmsLimit);Assert.Equal(6000,w.GangBulk.Pending.Value);Assert.Equal(3,w.GangBulk.Pending.Options.HandoverSeconds);}
        [Fact] public void FailedStartRestoresNoPendingContract()
        {var w=Bought();Assert.Throws<Exception>(()=>Begin(w,save:()=>throw new Exception()));Assert.Null(w.GangBulk);Assert.Equal(5,Account(w).Stock["pistol"]);}
        [Fact] public void BothPhysicalCheckpointsAreRequiredBeforeArmsRevenue()
        {var w=Started();Assert.False(Complete(w));Assert.False(GangBulkTransactions.Ready(w,"arms",Save,out _));GangBulkTransactions.Load(w,"arms",Save,out _);Assert.False(Complete(w));GangBulkTransactions.Ready(w,"arms",Save,out _);Assert.True(Complete(w));}
        [Fact] public void CompleteConsumesOnlyArmsCreditsDirtyAndDoesNotCreateDiplomacy()
        {var w=Ready();var dealer=w.Dealer;Assert.True(Complete(w));Assert.Equal(0,Account(w).Stock["pistol"]);Assert.Equal(5,Account(w).Sold["pistol"]);Assert.Equal(6000,Account(w).Revenue);Assert.Equal(6000,w.GangIllicit.Accounts[0].Dirty);Assert.Equal(6000,w.GangIllicit.Accounts[0].Proceeds);Assert.Null(w.GangBulk.Pending);Assert.Equal(1,w.GangBulk.Completed);Assert.Equal(6000,w.GangBulk.Revenue);Assert.Equal(95500,w.Bank);Assert.Equal(123,w.Cash);Assert.Same(dealer,w.Dealer);Assert.Null(w.GangWarehouse);Assert.Null(w.GangTreasury);Assert.Null(w.GangDiplomacy);Assert.True(w.GangArms.Valid);}
        [Fact] public void ExistingDiplomacyReferenceAndCountersAreExactlyUntouched()
        {var w=Ready();w.GangDiplomacy=new GangDiplomacyData();w.GangDiplomacy.Accounts.Add(new GangDiplomacyAccount{Gang=S().Gang,Rival="Other",Attitude="War",BulkDeals=2});var old=w.GangDiplomacy;Complete(w);Assert.Same(old,w.GangDiplomacy);Assert.Equal(2,old.Accounts[0].BulkDeals);Assert.Equal("War",old.Accounts[0].Attitude);}
        [Fact] public void ArmsCompletionReplayAfterReloadDoesNotDebitPayOrSaveAgain()
        {var w=Ready();Complete(w);w=CharacterRules.Copy(w);Assert.True(Complete(w,()=>throw new Exception()));Assert.Equal(6000,w.GangIllicit.Accounts[0].Dirty);Assert.Equal(5,Account(w).Sold["pistol"]);Assert.Equal(1,w.GangBulk.Completed);}
        [Fact] public void WrongTransactionCannotConsumeOtherCargoKindEvenWithOverlappingSku()
        {var w=Ready();var c=DealerDefaults.Load();c.Products.Add(new DealerProduct{Id="pistol",Name="Wrong",Weight=1,Buy=1,Sell=1200});w.GangWarehouse=new GangWarehouseData();w.GangWarehouse.Accounts.Add(new GangWarehouseAccount{Gang=S().Gang,Area=A().Id,Stock={{"pistol",5}},Deposited={{"pistol",5}}});Assert.False(GangBulkTransactions.Complete(w,S(),A(),c,"arms",Save,out _));Assert.Equal(5,w.GangWarehouse.Accounts[0].Stock["pistol"]);Assert.Equal(5,Account(w).Stock["pistol"]);Assert.True(Complete(w));Assert.Equal(5,w.GangWarehouse.Accounts[0].Stock["pistol"]);}
        [Fact] public void DrugTransactionCannotBeginArmsAttempt()
        {var w=Bought();Assert.False(GangBulkTransactions.Begin(w,S(),P(),DealerDefaults.Load(),Save,out _));Assert.Null(w.GangBulk);}
        [Fact] public void ArmsCompletionRefusesDrugContract()
        {var w=Ready();w.GangBulk.Pending.Kind=GangBulkKind.Drugs;Assert.True(w.GangBulk.Valid);Assert.False(Complete(w));Assert.Null(w.GangIllicit);Assert.Equal(5,Account(w).Stock["pistol"]);}
        [Theory][InlineData(false)][InlineData(true)]
        public void CancelBeforeOrAfterHandoverUnlocksUnconsumedArmsWithoutRefund(bool ready)
        {var w=ready?Ready():Started();var old=w.GangArms;Assert.True(GangBulkTransactions.Cancel(w,"arms",Save,out _));Assert.Same(old,w.GangArms);Assert.Equal(5,Account(w).Stock["pistol"]);Assert.Equal(95500,w.Bank);Assert.Null(w.GangIllicit);Assert.False(GangBulkTransactions.Blocked(w));}
        [Fact] public void PartialRestartRetainsKindLimitPriceAndRequiresNewLoad()
        {var w=Started();GangBulkTransactions.Load(w,"arms",Save,out _);Assert.True(GangBulkTransactions.Restart(w,"arms",Save,out _));Assert.False(w.GangBulk.Pending.Loaded);Assert.Equal(GangBulkKind.Arms,w.GangBulk.Pending.Kind);Assert.Equal(10,w.GangBulk.Pending.ArmsLimit);Assert.Equal(6000,w.GangBulk.Pending.Value);Assert.True(GangBulkTransactions.Load(w,"arms",Save,out bool changed));Assert.True(changed);}
        [Fact] public void ReadyResumeNeedsNoDealerCatalogOrDrugWarehouse()
        {var w=CharacterRules.Copy(Ready());w.Dealer=null;Assert.Null(w.GangWarehouse);Assert.False(GangBulkTransactions.Restart(w,"arms",Save,out _));Assert.True(Complete(w));}
        [Fact] public void ChangedCatalogPriceOrLimitDoesNotRepriceReadyContract()
        {var w=Ready();var c=C();c.Products.Find(p=>p.Id=="pistol").Sell=2000;c.MaxStock=1;c.Delivery.MaxValue=100;Assert.True(Complete(w,c:c));Assert.Equal(6000,w.GangIllicit.Accounts[0].Dirty);}
        [Fact] public void RemovedSkuOrLostOwnershipPreservesReadyDelivery()
        {var w=Ready();var c=C();c.Products.RemoveAll(p=>p.Id=="pistol");Assert.False(Complete(w,c:c));var s=S();s.Gang="Other";Assert.False(GangArmsTransactions.CompleteDelivery(w,s,A(),C(),"arms",Save,out _));Assert.True(w.GangBulk.Pending.Ready);Assert.True(GangBulkTransactions.Cancel(w,"arms",Save,out _));}
        [Fact] public void StockShortageIsRecheckedWithoutNegativeOrPartialPayment()
        {var w=Ready();Account(w).Stock["pistol"]=0;Account(w);w.GangArms.Accounts[0].Purchased["pistol"]=0;Assert.True(w.GangArms.Valid);Assert.False(Complete(w));Assert.Null(w.GangIllicit);Assert.True(w.GangBulk.Pending.Ready);}
        [Fact] public void FailedFinalSaveRollsBackAllThreeBooksAndOtherReferences()
        {var w=Ready();var b=w.GangBulk;var a=w.GangArms;var dealer=w.Dealer;Assert.Throws<Exception>(()=>Complete(w,()=>{Assert.Null(w.GangBulk.Pending);Assert.Equal(0,w.GangArms.Accounts[0].Stock["pistol"]);Assert.Equal(6000,w.GangIllicit.Accounts[0].Dirty);throw new Exception();}));Assert.Same(b,w.GangBulk);Assert.Same(a,w.GangArms);Assert.Same(dealer,w.Dealer);Assert.Null(w.GangIllicit);Assert.True(w.GangBulk.Pending.Ready);Assert.Equal(5,Account(w).Stock["pistol"]);Assert.Equal(95500,w.Bank);}
        [Fact] public void FailedFinalSaveRestoresExistingDirtyBook()
        {var w=Ready();w.GangIllicit=new GangIllicitData();w.GangIllicit.Accounts.Add(new GangIllicitAccount{Gang=S().Gang,Area=A().Id,Dirty=20,Proceeds=20});var d=w.GangIllicit;Assert.Throws<Exception>(()=>Complete(w,()=>throw new Exception()));Assert.Same(d,w.GangIllicit);Assert.Equal(20,d.Accounts[0].Dirty);}
        [Fact] public void DirtyOrArmsRevenueOverflowDoesNotConsumeStock()
        {var w=Ready();w.GangIllicit=new GangIllicitData();w.GangIllicit.Accounts.Add(new GangIllicitAccount{Gang=S().Gang,Area=A().Id,Dirty=int.MaxValue,Proceeds=int.MaxValue});Assert.False(Complete(w));w=Ready();Account(w).Revenue=long.MaxValue;Assert.False(Complete(w));Assert.Equal(5,Account(w).Stock["pistol"]);Assert.Null(w.GangIllicit);}
        [Fact] public void ArmsLifetimeSaleOverflowDoesNotDebitStock()
        {var w=Ready();var a=Account(w);a.Purchased["pistol"]=long.MaxValue;a.Sold["pistol"]=long.MaxValue-5;Assert.True(w.GangArms.Valid);Assert.True(Complete(w));w=Ready();a=Account(w);a.Stock["pistol"]=0;a.Purchased["pistol"]=long.MaxValue;a.Sold["pistol"]=long.MaxValue;Assert.True(w.GangArms.Valid);Assert.False(Complete(w));}
        [Fact] public void PendingDrugOrArmsShipmentBlocksWeaponPurchaseAndNewDelivery()
        {var w=Started();Assert.False(Buy(w,id:"during"));var p=P();p.Id="other";Assert.False(Begin(w,p));w.GangBulk.Pending.Kind=GangBulkKind.Drugs;Assert.False(Buy(w,id:"drug"));Assert.False(Begin(w,p));}
        [Fact] public void PendingFreightOrRaidBlocksArmsCommerce()
        {var w=Bought();w.GangBusiness=new GangBusinessData{Supply=new GangSupplyOrder()};Assert.False(Buy(w,id:"freight"));Assert.False(Begin(w));w.GangBusiness=null;w.GangRaid=new GangRaidData{Attempts=-1};Assert.False(Buy(w,id:"raid"));Assert.False(Begin(w));}
        [Fact] public void ArmsProceedsUseExistingEightyFivePercentProcessing()
        {var w=Ready();Complete(w);Assert.True(GangIllicitTransactions.Process(w,S(),A(),new GangLaunderingOptions(),500,new DateTime(2013,9,17),"process",Save,out _));Assert.Equal(5500,w.GangIllicit.Accounts[0].Dirty);Assert.Equal(425,GangTreasuryTransactions.Account(w.GangTreasury,S().Gang).Balance);Assert.Equal(95500,w.Bank);}
        [Fact] public void ProfilesAndAreasHaveSeparateArmsStock()
        {var w=Bought();Assert.Null(W().GangArms);Assert.Null(GangArmsTransactions.Account(w.GangArms,S().Gang,2));Assert.Null(GangArmsTransactions.Account(w.GangArms,"Other",A().Id));}
        [Fact] public void UnknownWalletBookAccountAndContractFieldsSurvive()
        {var w=Ready();var j=JObject.FromObject(w);j["Future"]="wallet";j["GangArms"]["Future"]="book";j["GangArms"]["Accounts"][0]["Future"]="stock";j["GangBulk"]["Pending"]["Future"]="contract";w=j.ToObject<WalletData>();Assert.Equal("contract",(string)JObject.FromObject(w)["GangBulk"]["Pending"]["Future"]);Complete(w);j=JObject.FromObject(w);Assert.Equal("wallet",(string)j["Future"]);Assert.Equal("book",(string)j["GangArms"]["Future"]);Assert.Equal("stock",(string)j["GangArms"]["Accounts"][0]["Future"]);}
        [Fact] public void LegacyBulkWithoutKindRemainsDrugAndWalletArmsStayAbsent()
        {var j=JObject.FromObject(P());j.Remove("Kind");j.Remove("ArmsLimit");var p=j.ToObject<GangBulkAttempt>();Assert.Equal(GangBulkKind.Drugs,p.Kind);Assert.True(p.Valid);var w=JObject.Parse("{Bank:42,Future:'keep'}").ToObject<WalletData>();Assert.Null(w.GangArms);}
        [Fact] public void InvalidConfigFailsWithoutReplacingOrCreatingData()
        {var c=C();c.Products.Add(new GangArmsProduct{Id="PISTOL",Name="Duplicate",Buy=1,Sell=1});Assert.False(c.Valid);Assert.False(Buy(W(),c:c));c=C();c.MaxStock=1000;Assert.False(c.Valid);c=C();c.Products[0].Buy=0;Assert.False(c.Valid);}
        [Fact] public void ConfigProductsAndNestedDeliveryArraysReplaceDefaults()
        {var c=JObject.Parse("{MaxStock:20,Products:[{Id:'pistol',Name:'One',Buy:900,Sell:1200}],Delivery:{Quantities:[1],Sites:[{Name:'Only',X:100,Y:100,Z:30}]}}").ToObject<GangArmsOptions>();Assert.True(c.Valid);Assert.Single(c.Products);Assert.Single(c.Delivery.Quantities);Assert.Single(c.Delivery.Sites);}
    }
}
