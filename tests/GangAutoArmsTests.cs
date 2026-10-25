using System;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangAutoArmsTests
    {
        static readonly Action Save=()=>{};
        static GangTurfDefinition A()=>GangTurfOptions.Defaults().Areas[0];
        static GangState S()=>new GangState{Gang=A().DefaultOwner,Reputation=800,Rank=3};
        static GangArmsOptions C()=>new GangArmsOptions();
        static WalletData W(){var w=new WalletData{Bank=10000,Cash=123,Dealer=new DealerData{Stock={{"weed",5}}},GangArms=new GangArmsData()};w.GangArms.Accounts.Add(new GangArmsAccount{Gang=S().Gang,Area=A().Id,Stock={{"pistol",3},{"rifle",2},{"ammo",100}},Purchased={{"pistol",3},{"rifle",2},{"ammo",100}}});return w;}
        static bool Set(WalletData w,string product="pistol",bool on=true,string id="set",Action save=null)=>GangAutoArmsTransactions.Configure(w,S(),A(),C(),product,on,id,save??Save,out _);
        static WalletData Enabled(){var w=W();Assert.True(Set(w));return w;}
        static bool Sale(WalletData w,string product="pistol",string id="sale",Action save=null,GangAutoArmsOptions c=null)=>GangAutoArmsTransactions.Sale(w,S(),A(),C(),c??new GangAutoArmsOptions(),product,id,save??Save,out _);
        static GangAutoArmsAccount Auto(WalletData w)=>GangAutoArmsRules.Account(w.GangAutoArms,S().Gang,A().Id);
        [Fact] public void DefaultsMatchReferenceThreeMinutesAndFiveHundred()
        {var c=new GangAutoArmsOptions();Assert.True(c.Valid);Assert.Equal(180,c.IntervalSeconds);Assert.Equal(500,c.UnitPrice);Assert.False(GangAutoArmsRules.Product("ammo"));Assert.True(GangAutoArmsRules.Product("pistol"));Assert.True(GangAutoArmsRules.Product("smg"));Assert.True(GangAutoArmsRules.Product("rifle"));Assert.True(GangAutoArmsRules.Product("shotgun"));}
        [Fact] public void QueryingLegacyWalletDoesNotEnableAutomationOrCreateBook()
        {var w=W();Assert.Empty(GangAutoArmsRules.Available(w,S().Gang,A().Id,C()));Assert.Null(w.GangAutoArms);Assert.True(GangAutoArmsRules.Free(w));Assert.False(Sale(w));Assert.Null(w.GangAutoArms);}
        [Fact] public void EnablingOnlyChangesSettingsAndDoesNotTouchStockOrMoney()
        {var w=W();var arms=w.GangArms;var d=w.Dealer;Assert.True(Set(w));Assert.Equal(new[]{"pistol"},Auto(w).Products);Assert.Same(arms,w.GangArms);Assert.Same(d,w.Dealer);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);Assert.Equal(0,Auto(w).Sales);}
        [Fact] public void EnableReplayAndAlreadyEnabledAreNoOpsWithoutSave()
        {var w=Enabled();Assert.True(Set(w,save:()=>throw new Exception()));Assert.True(Set(w,id:"same-setting",save:()=>throw new Exception()));Assert.Single(Auto(w).Products);}
        [Fact] public void AmmoUnknownSkuAndInvalidOwnerCannotEnable()
        {var w=W();Assert.False(Set(w,"ammo"));Assert.False(Set(w,"future"));var s=S();s.Gang="Other";Assert.False(GangAutoArmsTransactions.Configure(w,s,A(),C(),"pistol",true,"owner",Save,out _));Assert.Null(w.GangAutoArms);}
        [Fact] public void ConfigurationNeedsKnownCatalogButDoesNotRequireStockYet()
        {var w=W();w.GangArms=null;Assert.True(Set(w));Assert.Empty(GangAutoArmsRules.Available(w,S().Gang,A().Id,C()));var c=C();c.Products.RemoveAll(p=>p.Id=="rifle");Assert.False(GangAutoArmsTransactions.Configure(w,S(),A(),c,"rifle",true,"missing",Save,out _));}
        [Fact] public void SelectionControlsExactlyWhichProductsCanBeSold()
        {var w=Enabled();Assert.Equal(new[]{"pistol"},GangAutoArmsRules.Available(w,S().Gang,A().Id,C()));Assert.False(Sale(w,"rifle"));Assert.False(Sale(w,"ammo"));Assert.True(Set(w,"rifle",id:"rifle"));Assert.Equal(2,GangAutoArmsRules.Available(w,S().Gang,A().Id,C()).Count);}
        [Fact] public void OneSaleConsumesOneCommercialWeaponAndCreditsOnlyDirtyProceeds()
        {var w=Enabled();var dealer=w.Dealer;Assert.True(Sale(w));var arms=w.GangArms.Accounts[0];Assert.Equal(2,arms.Stock["pistol"]);Assert.Equal(1,arms.Sold["pistol"]);Assert.Equal(500,arms.Revenue);Assert.Equal(2,arms.Stock["rifle"]);Assert.Equal(100,arms.Stock["ammo"]);Assert.Equal(1,Auto(w).Sales);Assert.Equal(500,Auto(w).Revenue);Assert.Equal("pistol",Auto(w).LastProduct);Assert.Equal(500,w.GangIllicit.Accounts[0].Dirty);Assert.Equal(500,w.GangIllicit.Accounts[0].Proceeds);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Same(dealer,w.Dealer);Assert.Null(w.GangTreasury);Assert.Null(w.GangDiplomacy);Assert.Null(w.GangWarehouse);Assert.True(w.GangArms.Valid);}
        [Fact] public void RifleAlsoUsesFixedPriceRatherThanItsManualQuote()
        {var w=Enabled();Set(w,"rifle",id:"rifle");Assert.True(Sale(w,"rifle"));Assert.Equal(500,Auto(w).Revenue);Assert.Equal(1,w.GangArms.Accounts[0].Stock["rifle"]);Assert.Equal(4500,C().Products.Find(p=>p.Id=="rifle").Sell);}
        [Fact] public void SaleReplayAfterReloadDoesNotConsumeOrPayAgain()
        {var w=Enabled();Sale(w);w=CharacterRules.Copy(w);Assert.True(Sale(w,save:()=>throw new Exception()));Assert.Equal(2,w.GangArms.Accounts[0].Stock["pistol"]);Assert.Equal(1,Auto(w).Sales);Assert.Equal(500,w.GangIllicit.Accounts[0].Dirty);}
        [Fact] public void EmptySelectedStockCannotGenerateRevenueOrNegativeStock()
        {var w=Enabled();for(int i=0;i<3;i++)Assert.True(Sale(w,id:"sale"+i));Assert.False(Sale(w,id:"empty"));Assert.Equal(0,w.GangArms.Accounts[0].Stock["pistol"]);Assert.Equal(1500,w.GangIllicit.Accounts[0].Dirty);Assert.Empty(GangAutoArmsRules.Available(w,S().Gang,A().Id,C()));}
        [Fact] public void DisabledCategoryStopsSalesAndKeepsHistoryAndStock()
        {var w=Enabled();Sale(w);Assert.True(Set(w,on:false,id:"off"));Assert.False(Sale(w,id:"later"));Assert.Empty(Auto(w).Products);Assert.Equal(1,Auto(w).Sales);Assert.Equal(2,w.GangArms.Accounts[0].Stock["pistol"]);}
        [Fact] public void StopWorksWithoutOwnerAffiliationAndPreservesHistory()
        {var w=Enabled();Set(w,"rifle",id:"rifle");Sale(w);Assert.True(GangAutoArmsTransactions.Stop(w,S().Gang,A().Id,"stop",Save,out _));Assert.Empty(Auto(w).Products);Assert.Equal(500,Auto(w).Revenue);Assert.True(GangAutoArmsTransactions.Stop(w,S().Gang,A().Id,"stop",()=>throw new Exception(),out bool changed));Assert.False(changed);}
        [Fact] public void OwnerChangeOrWrongAreaBlocksSaleButNotExistingStopControl()
        {var w=Enabled();var s=S();s.Gang="Other";Assert.False(GangAutoArmsTransactions.Sale(w,s,A(),C(),new GangAutoArmsOptions(),"pistol","owner",Save,out _));Assert.False(GangAutoArmsTransactions.Sale(w,S(),GangTurfOptions.Defaults().Areas[1],C(),new GangAutoArmsOptions(),"pistol","area",Save,out _));Assert.True(GangAutoArmsTransactions.Stop(w,S().Gang,A().Id,"stop",Save,out _));Assert.Equal(3,w.GangArms.Accounts[0].Stock["pistol"]);}
        [Fact] public void PendingRaidFreightOrBulkBlocksSaleAndEnablingMoreCategories()
        {var w=Enabled();w.GangRaid=new GangRaidData{Attempts=-1};Assert.False(Sale(w));Assert.False(Set(w,"rifle",id:"enable"));w.GangRaid=null;w.GangBusiness=new GangBusinessData{Supply=new GangSupplyOrder()};Assert.False(Sale(w));w.GangBusiness=null;w.GangBulk=new GangBulkData{Attempts=-1};Assert.False(Sale(w));Assert.True(GangAutoArmsTransactions.Stop(w,S().Gang,A().Id,"off",Save,out _));}
        [Fact] public void MissingCatalogOrCorruptStockCannotBeSilentlyReplaced()
        {var w=Enabled();var c=C();c.Products.RemoveAll(p=>p.Id=="pistol");Assert.Empty(GangAutoArmsRules.Available(w,S().Gang,A().Id,c));Assert.False(GangAutoArmsTransactions.Sale(w,S(),A(),c,new GangAutoArmsOptions(),"pistol","missing",Save,out _));w.GangArms.Accounts[0].Stock["pistol"]=999;var old=w.GangArms;Assert.False(Sale(w));Assert.Same(old,w.GangArms);}
        [Fact] public void RecoveredWeaponsCanBeSoldWithoutCreatingPurchaseHistory()
        {var w=Enabled();var a=w.GangArms.Accounts[0];a.Purchased["pistol"]=0;a.Recovered["pistol"]=3;Assert.True(w.GangArms.Valid);Assert.True(Sale(w));Assert.Equal(3,w.GangArms.Accounts[0].Recovered["pistol"]);Assert.Equal(0,w.GangArms.Accounts[0].Purchased["pistol"]);Assert.Equal(2,w.GangArms.Accounts[0].Stock["pistol"]);}
        [Fact] public void FailedFirstConfigurationRestoresAbsentBook()
        {var w=W();Assert.Throws<Exception>(()=>Set(w,save:()=>throw new Exception()));Assert.Null(w.GangAutoArms);Assert.Equal(3,w.GangArms.Accounts[0].Stock["pistol"]);}
        [Fact] public void FailedConfigurationAndStopRestoreOriginalSettingsReference()
        {var w=Enabled();var old=w.GangAutoArms;Assert.Throws<Exception>(()=>Set(w,"rifle",id:"new",save:()=>throw new Exception()));Assert.Same(old,w.GangAutoArms);Assert.Throws<Exception>(()=>GangAutoArmsTransactions.Stop(w,S().Gang,A().Id,"stop",()=>throw new Exception(),out _));Assert.Same(old,w.GangAutoArms);Assert.Equal(new[]{"pistol"},Auto(w).Products);}
        [Fact] public void FailedSaleRollsBackAllThreeBooksAndUnrelatedReferences()
        {var w=Enabled();var auto=w.GangAutoArms;var arms=w.GangArms;var dealer=w.Dealer;Assert.Throws<Exception>(()=>Sale(w,save:()=>{Assert.Equal(2,w.GangArms.Accounts[0].Stock["pistol"]);Assert.Equal(500,w.GangIllicit.Accounts[0].Dirty);Assert.Equal(1,Auto(w).Sales);throw new Exception();}));Assert.Same(auto,w.GangAutoArms);Assert.Same(arms,w.GangArms);Assert.Same(dealer,w.Dealer);Assert.Null(w.GangIllicit);Assert.Equal(3,arms.Accounts[0].Stock["pistol"]);Assert.Equal(0,Auto(w).Sales);}
        [Fact] public void FailedSaleRestoresExistingDirtyBook()
        {var w=Enabled();w.GangIllicit=new GangIllicitData();w.GangIllicit.Accounts.Add(new GangIllicitAccount{Gang=S().Gang,Area=A().Id,Dirty=100,Proceeds=100});var old=w.GangIllicit;Assert.Throws<Exception>(()=>Sale(w,save:()=>throw new Exception()));Assert.Same(old,w.GangIllicit);Assert.Equal(100,old.Accounts[0].Dirty);}
        [Fact] public void DirtyBalanceAndHistoryOverflowRefuseWithoutConsumingWeapon()
        {var w=Enabled();w.GangIllicit=new GangIllicitData();w.GangIllicit.Accounts.Add(new GangIllicitAccount{Gang=S().Gang,Area=A().Id,Dirty=int.MaxValue,Proceeds=int.MaxValue});Assert.False(Sale(w));Assert.Equal(3,w.GangArms.Accounts[0].Stock["pistol"]);w=Enabled();Auto(w).Revenue=long.MaxValue;Assert.False(Sale(w));w=Enabled();w.GangArms.Accounts[0].Revenue=long.MaxValue;Assert.False(Sale(w));}
        [Fact] public void ChangedConfiguredPriceOnlyAffectsFutureTransactions()
        {var w=Enabled();Sale(w);Assert.True(Sale(w,id:"new",c:new GangAutoArmsOptions{UnitPrice=700}));Assert.Equal(1200,Auto(w).Revenue);Assert.Equal(1200,w.GangIllicit.Accounts[0].Dirty);}
        [Fact] public void ProceedsUseExistingEightyFivePercentProcessing()
        {var w=Enabled();Sale(w);Assert.True(GangIllicitTransactions.Process(w,S(),A(),new GangLaunderingOptions(),500,new DateTime(2013,9,17),"process",Save,out _));Assert.Equal(425,GangTreasuryTransactions.Account(w.GangTreasury,S().Gang).Balance);Assert.Equal(0,w.GangIllicit.Accounts[0].Dirty);Assert.Equal(10000,w.Bank);}
        [Fact] public void InvalidAutomationSettingsAreProtectedRatherThanReset()
        {var w=Enabled();Auto(w).Products.Add("ammo");var old=w.GangAutoArms;Assert.False(w.GangAutoArms.Valid);Assert.False(Sale(w));Assert.False(Set(w));Assert.False(GangAutoArmsTransactions.Stop(w,S().Gang,A().Id,"off",Save,out _));Assert.Same(old,w.GangAutoArms);}
        [Fact] public void UnknownWalletBookAccountAndStockFieldsSurviveSale()
        {var w=Enabled();var j=JObject.FromObject(w);j["Future"]="wallet";j["GangAutoArms"]["Future"]="auto";j["GangAutoArms"]["Accounts"][0]["Future"]="config";j["GangArms"]["Accounts"][0]["Future"]="stock";w=j.ToObject<WalletData>();Sale(w);j=JObject.FromObject(w);Assert.Equal("wallet",(string)j["Future"]);Assert.Equal("auto",(string)j["GangAutoArms"]["Future"]);Assert.Equal("config",(string)j["GangAutoArms"]["Accounts"][0]["Future"]);Assert.Equal("stock",(string)j["GangArms"]["Accounts"][0]["Future"]);}
        [Fact] public void AreasAndProfilesHaveIndependentOptInAndHistories()
        {var w=Enabled();Assert.Null(W().GangAutoArms);Assert.Null(GangAutoArmsRules.Account(w.GangAutoArms,S().Gang,2));Assert.Null(GangAutoArmsRules.Account(w.GangAutoArms,"Other",A().Id));}
        [Fact] public void ConfigBoundsRejectZeroNegativeOrExcessivePricesAndIntervals()
        {var c=new GangAutoArmsOptions();c.UnitPrice=0;Assert.False(c.Valid);c.UnitPrice=100001;Assert.False(c.Valid);c.UnitPrice=500;c.IntervalSeconds=29;Assert.False(c.Valid);c.IntervalSeconds=1801;Assert.False(c.Valid);}
        [Fact] public void ClockRequiresWholeActiveIntervalAndStableReceipt()
        {var c=new GangAutoArmsClock(0);string receipt=c.Receipt;for(int t=1000;t<180000;t+=1000)Assert.False(c.Step(t,true,180));Assert.True(c.Step(180000,true,180));Assert.Equal(180000,c.Milliseconds);Assert.Equal(receipt,c.Receipt);}
        [Fact] public void ClockPausesForMenusAndSkipsLongGapsWithoutCatchUp()
        {var c=new GangAutoArmsClock(0);Assert.False(c.Step(1000,true,180));Assert.False(c.Step(2000,false,180));Assert.False(c.Step(100000,true,180));Assert.Equal(1000,c.Milliseconds);Assert.False(c.Step(101000,true,180));Assert.Equal(2000,c.Milliseconds);}
        [Fact] public void ReadyClockCannotTriggerSaleWhileMenuIsOpen()
        {var c=new GangAutoArmsClock(0);for(int t=1000;t<=30000;t+=1000)c.Step(t,true,30);Assert.False(c.Step(31000,false,30));Assert.True(c.Step(32000,true,30));}
        [Fact] public void NewSessionStartsFromZeroInsteadOfReplayingOfflineIntervals()
        {var old=new GangAutoArmsClock(0);for(int t=1000;t<=30000;t+=1000)old.Step(t,true,30);var fresh=new GangAutoArmsClock(900000);Assert.Equal(0,fresh.Milliseconds);Assert.False(fresh.Step(901000,true,30));Assert.NotEqual(old.Receipt,fresh.Receipt);}
        [Fact] public void ClockSupportsIntWrapAndRejectsBackwardOrInvalidElapsed()
        {var c=new GangAutoArmsClock(int.MaxValue-500);Assert.False(c.Step(unchecked(int.MaxValue+500),true,30));Assert.Equal(1000,c.Milliseconds);Assert.False(c.Step(123,true,0));var r=new GangAutoArmsClock(5000);Assert.False(r.Step(4000,true,30));Assert.Equal(0,r.Milliseconds);}
        [Fact] public void ReceiptsRemainBoundedWithoutLosingHistory()
        {var w=Enabled();for(int i=0;i<256;i++)w.GangAutoArms.Receipts.Add("past"+i);w.GangAutoArms.Receipts.RemoveAt(0);Assert.True(w.GangAutoArms.Valid);Assert.True(Sale(w));Assert.Equal(256,w.GangAutoArms.Receipts.Count);Assert.Equal(1,Auto(w).Sales);}
    }
}
