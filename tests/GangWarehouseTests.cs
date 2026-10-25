using System;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangWarehouseTests
    {
        static readonly Action Save=()=>{};static GangTurfDefinition A()=>GangTurfOptions.Defaults().Areas[0];static GangState S()=>new GangState{Gang=A().DefaultOwner};
        static DealerOptions C()=>DealerDefaults.Load();static GangWarehouseOptions O()=>new GangWarehouseOptions();
        static WalletData W()=>new WalletData{Bank=10000,Cash=123,Dealer=new DealerData{Stock={{"weed",5},{"mdma",5}}}};
        static bool Move(WalletData w,bool deposit=true,int count=5,string id="move",Action save=null)=>GangWarehouseTransactions.Transfer(w,S(),A(),C(),O(),"weed",count,deposit,id,save??Save,out _);
        static WalletData Stored(){var w=W();Assert.True(Move(w));return w;}
        static int Quote(WalletData w)=>DealerRules.UnitPrice(C().Products.Find(p=>p.Id=="weed"),DealerRules.Reputation(w.Dealer,"DAVIS"),false);
        static bool Sale(WalletData w,string id="sale",int count=1,Action save=null)=>GangWarehouseTransactions.Sale(w,S(),A(),C(),"weed",count,"DAVIS",false,Quote(w),false,id,save??Save,out _);
        [Fact] public void DepositMovesExistingPackagesWithoutMoneyOrReputationReward()
        {var w=Stored();Assert.Equal(0,w.Dealer.Stock["weed"]);var a=w.GangWarehouse.Accounts[0];Assert.Equal(5,a.Stock["weed"]);Assert.Equal(5,a.Deposited["weed"]);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Equal(0,w.Dealer.Sales);Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);}
        [Fact] public void WithdrawalReversesTransferAndConservesPackages()
        {var w=Stored();Assert.True(Move(w,false,2,"out"));var a=w.GangWarehouse.Accounts[0];Assert.Equal(3,a.Stock["weed"]);Assert.Equal(2,a.Withdrawn["weed"]);Assert.Equal(2,w.Dealer.Stock["weed"]);Assert.True(a.Valid);Assert.Equal(10000,w.Bank);}
        [Fact] public void WarehouseSaleUsesLocalStockAndKeepsPersonalStockReference()
        {var w=Stored();var stock=w.Dealer.Stock;Assert.True(Sale(w));Assert.Same(stock,w.Dealer.Stock);Assert.Equal(0,w.Dealer.Stock["weed"]);Assert.Equal(5,w.Dealer.Stock["mdma"]);Assert.Equal(4,w.GangWarehouse.Accounts[0].Stock["weed"]);Assert.Equal(1,w.GangWarehouse.Accounts[0].Sold["weed"]);Assert.Equal(70,w.GangIllicit.Accounts[0].Dirty);Assert.Equal(1,w.Dealer.Sales);Assert.Equal(5,w.Dealer.ZoneRep["DAVIS"]);Assert.Equal(10000,w.Bank);}
        [Fact] public void SaleNeverFallsBackToPersonalPackagesIfWarehouseIsShort()
        {var w=W();Assert.True(Move(w,count:1));Assert.Equal(4,w.Dealer.Stock["weed"]);Assert.False(Sale(w,count:2));Assert.Equal(1,w.GangWarehouse.Accounts[0].Stock["weed"]);Assert.Equal(4,w.Dealer.Stock["weed"]);Assert.Null(w.GangIllicit);}
        [Fact] public void MissingWarehouseCannotCreateSaleProceeds()
        {var w=W();Assert.False(Sale(w));Assert.Null(w.GangWarehouse);Assert.Null(w.GangIllicit);Assert.Equal(5,w.Dealer.Stock["weed"]);}
        [Fact] public void DepositCannotCreateStockWithoutOwnedPersonalPackages()
        {var w=W();w.Dealer.Stock.Clear();Assert.False(Move(w));Assert.Null(w.GangWarehouse);Assert.Equal(10000,w.Bank);}
        [Fact] public void CapacityRejectsWholeTransferWithoutRemovingPersonalPackages()
        {var w=W();w.Dealer.Stock["weed"]=20;Assert.False(GangWarehouseTransactions.Transfer(w,S(),A(),C(),new GangWarehouseOptions{Capacity=1},"weed",20,true,"full",Save,out _));Assert.Equal(20,w.Dealer.Stock["weed"]);Assert.Null(w.GangWarehouse);}
        [Fact] public void PersonalWeightCapacityBlocksWithdrawalWithoutLosingLocalStock()
        {var w=Stored();w.Dealer.Stock["mdma"]=100;Assert.False(Move(w,false,1,"out"));Assert.Equal(5,w.GangWarehouse.Accounts[0].Stock["weed"]);Assert.Equal(0,w.Dealer.Stock["weed"]);}
        [Fact] public void ProductLimit999AppliesToBothTransferDirections()
        {var w=Stored();var a=w.GangWarehouse.Accounts[0];a.Stock["weed"]=999;a.Deposited["weed"]=999;w.Dealer.Stock["weed"]=1;Assert.False(Move(w,count:1,id:"full"));w=Stored();w.Dealer.Stock["weed"]=999;Assert.False(Move(w,false,1,"full-personal"));Assert.Equal(5,w.GangWarehouse.Accounts[0].Stock["weed"]);}
        [Fact] public void DepositAndWithdrawalReplayAfterReloadNeverMoveTwice()
        {var w=CharacterRules.Copy(Stored());Assert.True(Move(w,save:()=>throw new Exception()));Assert.Equal(5,w.GangWarehouse.Accounts[0].Stock["weed"]);Move(w,false,2,"out");w=CharacterRules.Copy(w);Assert.True(Move(w,false,2,"out",()=>throw new Exception()));Assert.Equal(2,w.Dealer.Stock["weed"]);Assert.Equal(3,w.GangWarehouse.Accounts[0].Stock["weed"]);}
        [Fact] public void SaleReplayAfterReloadCannotConsumeOrCreditTwice()
        {var w=Stored();Sale(w);w=CharacterRules.Copy(w);Assert.True(Sale(w,save:()=>throw new Exception()));Assert.Equal(4,w.GangWarehouse.Accounts[0].Stock["weed"]);Assert.Equal(70,w.GangIllicit.Accounts[0].Dirty);}
        [Fact] public void WarehouseReceiptCannotBeReusedToPayPersonalBank()
        {var w=Stored();Sale(w);Assert.True(DealerTransactions.Sale(w,C(),"weed",1,"DAVIS",false,Quote(w),false,"sale",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(10000,w.Bank);Assert.Equal(70,w.GangIllicit.Accounts[0].Dirty);}
        [Fact] public void PersonalTerritorialReceiptCannotBeReusedToDebitWarehouse()
        {var w=Stored();w.Dealer.Stock["weed"]=1;Assert.True(GangIllicitTransactions.Sale(w,S(),A(),C(),"weed",1,"DAVIS",false,Quote(w),false,"sale",Save,out _));Assert.True(Sale(w));Assert.Equal(5,w.GangWarehouse.Accounts[0].Stock["weed"]);Assert.Equal(70,w.GangIllicit.Accounts[0].Dirty);}
        [Fact] public void WarehouseRevenueFeedsExisting85PercentProcessing()
        {var w=Stored();Sale(w);Assert.True(GangIllicitTransactions.Process(w,S(),A(),new GangLaunderingOptions(),70,new DateTime(2013,9,17),"process",Save,out _));Assert.Equal(0,w.GangIllicit.Accounts[0].Dirty);Assert.Equal(59,w.GangIllicit.Accounts[0].Net);Assert.Equal(11,w.GangIllicit.Accounts[0].Fees);Assert.Equal(59,GangTreasuryTransactions.Account(w.GangTreasury,S().Gang).Balance);}
        [Fact] public void FailedDepositSaveRestoresBothReferencesAndAbsentBook()
        {var w=W();var d=w.Dealer;Assert.Throws<Exception>(()=>Move(w,save:()=>throw new Exception()));Assert.Same(d,w.Dealer);Assert.Null(w.GangWarehouse);Assert.Equal(5,w.Dealer.Stock["weed"]);Assert.Equal(10000,w.Bank);}
        [Fact] public void FailedWithdrawalSaveRestoresDealerAndWarehouseReferences()
        {var w=Stored();var d=w.Dealer;var b=w.GangWarehouse;Assert.Throws<Exception>(()=>Move(w,false,1,"out",()=>throw new Exception()));Assert.Same(d,w.Dealer);Assert.Same(b,w.GangWarehouse);Assert.Equal(5,b.Accounts[0].Stock["weed"]);}
        [Fact] public void FailedSaleSaveRestoresAllThreeBooksAndOtherReferences()
        {var w=Stored();Sale(w,"old");var b=w.GangWarehouse;var d=w.Dealer;var dirty=w.GangIllicit;var t=w.GangTreasury=new GangTreasuryData();Assert.Throws<Exception>(()=>Sale(w,"next",save:()=>throw new Exception()));Assert.Same(b,w.GangWarehouse);Assert.Same(d,w.Dealer);Assert.Same(dirty,w.GangIllicit);Assert.Same(t,w.GangTreasury);Assert.Equal(4,b.Accounts[0].Stock["weed"]);Assert.Equal(70,dirty.Accounts[0].Dirty);}
        [Fact] public void FailedFirstSaleDoesNotPublishDirtyAccountOrStatistics()
        {var w=Stored();Assert.Throws<Exception>(()=>Sale(w,save:()=>throw new Exception()));Assert.Null(w.GangIllicit);Assert.Equal(0,w.Dealer.Sales);Assert.Equal(5,w.GangWarehouse.Accounts[0].Stock["weed"]);}
        [Fact] public void LostOwnerOrRivalCannotWithdrawOrSellOldStock()
        {var w=Stored();var s=S();s.Turf=new GangTurfData();s.Turf.Owners[1]="Rival";Assert.False(GangWarehouseTransactions.Transfer(w,s,A(),C(),O(),"weed",1,false,"out",Save,out _));Assert.False(GangWarehouseTransactions.Sale(w,s,A(),C(),"weed",1,"DAVIS",false,Quote(w),false,"sale",Save,out _));s.Gang="Rival";Assert.False(GangWarehouseTransactions.Sale(w,s,A(),C(),"weed",1,"DAVIS",false,Quote(w),false,"rival",Save,out _));Assert.Equal(5,w.GangWarehouse.Accounts[0].Stock["weed"]);}
        [Fact] public void ProfilesAreasAndGangsHaveIndependentWarehouses()
        {var w=Stored();var other=W();Assert.False(Sale(other));Assert.Null(other.GangWarehouse);Assert.Null(GangWarehouseTransactions.Account(w.GangWarehouse,"Other",1));Assert.Null(GangWarehouseTransactions.Account(w.GangWarehouse,S().Gang,2));Assert.Equal(0,GangWarehouseTransactions.KnownUnits(w.GangWarehouse,S().Gang,2,C()));}
        [Fact] public void GangNameCaseDoesNotDuplicateAccount()
        {var w=Stored();var s=S();s.Gang=s.Gang.ToUpperInvariant();Assert.True(GangWarehouseTransactions.Transfer(w,s,A(),C(),O(),"weed",1,false,"case",Save,out _));Assert.Single(w.GangWarehouse.Accounts);Assert.Equal(4,w.GangWarehouse.Accounts[0].Stock["weed"]);}
        [Fact] public void InvalidQuoteAndMissingProductCannotConsumeWarehouseStock()
        {var w=Stored();Assert.False(GangWarehouseTransactions.Sale(w,S(),A(),C(),"weed",1,"DAVIS",false,999,false,"bad",Save,out _));Assert.False(GangWarehouseTransactions.Sale(w,S(),A(),C(),null,1,"DAVIS",false,70,false,"null",Save,out _));Assert.Equal(5,w.GangWarehouse.Accounts[0].Stock["weed"]);}
        [Fact] public void LifetimeDepositOverflowRejectsWithoutLosingPersonalStock()
        {var w=Stored();var a=w.GangWarehouse.Accounts[0];a.Deposited["weed"]=long.MaxValue;a.Withdrawn["weed"]=long.MaxValue-5;w.Dealer.Stock["weed"]=1;Assert.True(a.Valid);Assert.False(Move(w,count:1,id:"overflow"));Assert.Equal(1,w.Dealer.Stock["weed"]);}
        [Fact] public void DirtyCashOverflowRejectsWithoutConsumingWarehouseStock()
        {var w=Stored();w.GangIllicit=new GangIllicitData();w.GangIllicit.Accounts.Add(new GangIllicitAccount{Gang=S().Gang,Area=1,Dirty=int.MaxValue,Proceeds=int.MaxValue});Assert.False(Sale(w));Assert.Equal(5,w.GangWarehouse.Accounts[0].Stock["weed"]);}
        [Fact] public void InvalidConservationOrDuplicatedAccountProtectsStoredGoods()
        {var w=Stored();w.GangWarehouse.Accounts[0].Stock["weed"]=6;Assert.False(Move(w,false,1,"bad"));Assert.False(Sale(w));w=Stored();w.GangWarehouse.Accounts.Add(CharacterRules.Copy(w.GangWarehouse.Accounts[0]));Assert.False(w.GangWarehouse.Valid);Assert.False(Sale(w));}
        [Fact] public void FutureMetadataSurvivesTransferAndSaleAcrossAllBooks()
        {var w=Stored();var j=JObject.FromObject(w);j["Future"]="wallet";j["GangWarehouse"]["Future"]="book";j["GangWarehouse"]["Accounts"][0]["Future"]="account";j["Dealer"]["Future"]="dealer";w=j.ToObject<WalletData>();Move(w,false,1,"out");Sale(w);j=JObject.FromObject(w);Assert.Equal("wallet",(string)j["Future"]);Assert.Equal("book",(string)j["GangWarehouse"]["Future"]);Assert.Equal("account",(string)j["GangWarehouse"]["Accounts"][0]["Future"]);Assert.Equal("dealer",(string)j["Dealer"]["Future"]);}
        [Fact] public void UnknownStoredProductsRemainIntactAndCountConservativeWeight()
        {var w=Stored();var a=w.GangWarehouse.Accounts[0];a.Stock["future-item"]=2;a.Deposited["future-item"]=2;Assert.True(a.Valid);Assert.InRange(GangWarehouseTransactions.Weight(a,C()),2.49,2.51);Sale(w);Assert.Equal(2,w.GangWarehouse.Accounts[0].Stock["future-item"]);Assert.False(GangWarehouseTransactions.Transfer(w,S(),A(),C(),O(),"future-item",1,false,"unknown",Save,out _));}
        [Fact] public void FreightReservationBlocksTransfersAndWarehouseSales()
        {var w=Stored();GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"freight",Save,out _);Assert.False(Move(w,false,1,"out"));Assert.False(Sale(w));Assert.Equal(5,w.GangWarehouse.Accounts[0].Stock["weed"]);}
        [Theory][InlineData(0)][InlineData(-1)][InlineData(21)]
        public void InvalidTransferAmountsNeverMoveStock(int amount){var w=W();Assert.False(Move(w,count:amount));Assert.Null(w.GangWarehouse);Assert.Equal(5,w.Dealer.Stock["weed"]);}
        [Fact] public void InvalidCapacityOrMissingReceiptNeverCreatesBook()
        {var w=W();Assert.False(GangWarehouseTransactions.Transfer(w,S(),A(),C(),new GangWarehouseOptions{Capacity=float.NaN},"weed",1,true,"bad",Save,out _));Assert.False(Move(w,id:null));Assert.Null(w.GangWarehouse);}
        [Fact] public void LegacyWalletDoesNotCreateOrMigrateWarehouseOnLoad()
        {var w=JObject.Parse("{Bank:100,Dealer:{Stock:{weed:5}},Future:'keep'}").ToObject<WalletData>();Assert.Null(w.GangWarehouse);Assert.Equal(5,w.Dealer.Stock["weed"]);Assert.Equal("keep",(string)JObject.FromObject(w)["Future"]);}
    }
}
