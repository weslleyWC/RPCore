using System;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangCriminalSupplyTests
    {
        static readonly Action Save=()=>{};static GangTurfDefinition A()=>GangTurfOptions.Defaults().Areas[0];static GangState S()=>new GangState{Gang=A().DefaultOwner};static DealerOptions C()=>DealerDefaults.Load();
        static WalletData W()=>new WalletData{Bank=10000,Cash=123,Dealer=new DealerData{Stock={{"weed",5}}}};
        static bool Begin(WalletData w,Action save=null)=>GangSupplyTransactions.BeginCriminal(w,S(),A(),C(),new GangWarehouseOptions(),new GangCriminalSupplyOptions(),"weed","run",save??Save,out _);
        static WalletData Reserved(){var w=W();Assert.True(Begin(w));return w;}
        static WalletData Ready(){var w=Reserved();for(int i=0;i<3;i++)Assert.True(GangSupplyTransactions.Load(w,"run",i,Save,out _));for(int n=1;n<=3;n++)Assert.True(GangSupplyTransactions.Deliver(w,"run",n,Save,out _));return w;}
        static bool Complete(WalletData w,Action save=null,DealerOptions catalog=null)=>GangSupplyTransactions.Complete(w,S(),A(),"run",save??Save,out _,catalog??C());
        [Fact] public void DefaultCriminalSuppliersMatchFirstReferenceSupplyLocations()
        {var c=new GangCriminalSupplyOptions();Assert.True(c.Valid);Assert.Equal(10,c.Units);Assert.Equal(10,c.DiscountPercent);Assert.Equal(887.2639f,c.Route.Suppliers[0].X);Assert.Equal(-354.4539f,c.Route.Suppliers[1].Y);Assert.Equal(57.98261f,c.Route.Suppliers[2].Z);}
        [Fact] public void ReservationPaysDiscountedCostWithoutGrantingAnyStock()
        {var w=Reserved();var p=w.GangBusiness.Supply;Assert.Equal(9640,w.Bank);Assert.Equal(123,w.Cash);Assert.Equal(5,w.Dealer.Stock["weed"]);Assert.Null(w.GangWarehouse);Assert.Empty(w.GangBusiness.Accounts);Assert.Equal(GangSupplyCargo.Criminal,p.CargoKind);Assert.Equal("weed",p.ProductId);Assert.Equal(360,p.Cost);Assert.Equal(10,p.Units);}
        [Theory][InlineData("weed",360)][InlineData("cocaine",900)][InlineData("mdma",540)][InlineData("lean",720)][InlineData("crack",675)]
        public void EachCatalogProductUsesItsOwnPaidPrice(string product,int expected)
        {var w=W();Assert.True(GangSupplyTransactions.BeginCriminal(w,S(),A(),C(),new GangWarehouseOptions(),new GangCriminalSupplyOptions(),product,"run",Save,out _));Assert.Equal(expected,w.GangBusiness.Supply.Cost);Assert.Equal(10000-expected,w.Bank);}
        [Fact] public void InsufficientBankLeavesBothBooksAbsent()
        {var w=W();w.Bank=359;Assert.False(Begin(w));Assert.Null(w.GangBusiness);Assert.Null(w.GangWarehouse);Assert.Equal(359,w.Bank);}
        [Fact] public void WeightOrSkuCapacityRefusesReservationWithoutDebit()
        {var w=W();Assert.False(GangSupplyTransactions.BeginCriminal(w,S(),A(),C(),new GangWarehouseOptions{Capacity=1},new GangCriminalSupplyOptions{Units=20},"weed","run",Save,out _));w.GangWarehouse=new GangWarehouseData();w.GangWarehouse.Accounts.Add(new GangWarehouseAccount{Gang=S().Gang,Area=1,Stock={{"weed",995}},Deposited={{"weed",995}}});Assert.False(Begin(w));Assert.Equal(10000,w.Bank);Assert.Null(w.GangBusiness);}
        [Fact] public void CompleteAddsOnlyCriminalStockAndSuppliedCounter()
        {var w=Ready();Assert.Null(w.GangWarehouse);Assert.True(Complete(w));Assert.Null(w.GangBusiness.Supply);Assert.Empty(w.GangBusiness.Accounts);var a=w.GangWarehouse.Accounts[0];Assert.Equal(10,a.Stock["weed"]);Assert.Equal(10,a.Supplied["weed"]);Assert.Empty(a.Deposited);Assert.True(a.Valid);Assert.Equal(5,w.Dealer.Stock["weed"]);Assert.Equal(9640,w.Bank);Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);}
        [Fact] public void PartialLoadOrUnloadCannotGrantPackages()
        {var w=Reserved();Assert.False(Complete(w));for(int i=0;i<3;i++)GangSupplyTransactions.Load(w,"run",i,Save,out _);GangSupplyTransactions.Deliver(w,"run",1,Save,out _);GangSupplyTransactions.Deliver(w,"run",2,Save,out _);Assert.False(Complete(w));Assert.Null(w.GangWarehouse);}
        [Fact] public void RepeatedReservationAndCompleteAfterReloadNeverDebitOrCreditTwice()
        {var w=CharacterRules.Copy(Reserved());Assert.True(Begin(w,()=>throw new Exception()));Assert.Equal(9640,w.Bank);w=Ready();Complete(w);w=CharacterRules.Copy(w);Assert.True(Complete(w,()=>throw new Exception()));Assert.Equal(10,w.GangWarehouse.Accounts[0].Stock["weed"]);Assert.Equal(9640,w.Bank);Assert.False(GangSupplyTransactions.Refund(w,"run",Save,out _));}
        [Fact] public void PartialResumeKeepsKindPriceAndProductButRequiresNewPhysicalProof()
        {var w=Reserved();GangSupplyTransactions.Load(w,"run",0,Save,out _);Assert.True(GangSupplyTransactions.Restart(w,"run",Save,out _));Assert.Empty(w.GangBusiness.Supply.Loaded);Assert.Equal(GangSupplyCargo.Criminal,w.GangBusiness.Supply.CargoKind);Assert.Equal("weed",w.GangBusiness.Supply.ProductId);Assert.Equal(360,w.GangBusiness.Supply.Cost);Assert.True(GangSupplyTransactions.Load(w,"run",0,Save,out bool changed));Assert.True(changed);Assert.Equal(9640,w.Bank);}
        [Fact] public void ReadyOrderSurvivesReloadWithoutRequiringRecapture()
        {var w=CharacterRules.Copy(Ready());Assert.True(w.GangBusiness.Supply.Ready);Assert.Equal(3,w.GangBusiness.Supply.Options.Suppliers.Count);Assert.False(GangSupplyTransactions.Restart(w,"run",Save,out _));Assert.True(Complete(w));}
        [Theory][InlineData(false)][InlineData(true)]
        public void CancellationRefundsReservedPriceWithoutPackages(bool ready)
        {var w=ready?Ready():Reserved();Assert.True(GangSupplyTransactions.Refund(w,"run",Save,out _));Assert.Equal(10000,w.Bank);Assert.Null(w.GangWarehouse);Assert.Null(w.GangBusiness.Supply);Assert.Equal(5,w.Dealer.Stock["weed"]);Assert.True(GangSupplyTransactions.Refund(w,"run",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.False(Complete(w));}
        [Fact] public void FailedBeginSaveRestoresBankAndExistingBookReferences()
        {var w=W();var warehouse=w.GangWarehouse=new GangWarehouseData();var dealer=w.Dealer;Assert.Throws<Exception>(()=>Begin(w,()=>throw new Exception()));Assert.Null(w.GangBusiness);Assert.Same(warehouse,w.GangWarehouse);Assert.Same(dealer,w.Dealer);Assert.Equal(10000,w.Bank);}
        [Fact] public void FailedCompleteSaveRestoresBothBooksAndReadyReservation()
        {var w=Ready();var warehouse=w.GangWarehouse=new GangWarehouseData();var book=w.GangBusiness;Assert.Throws<Exception>(()=>Complete(w,()=>throw new Exception()));Assert.Same(book,w.GangBusiness);Assert.Same(warehouse,w.GangWarehouse);Assert.True(book.Supply.Ready);Assert.Empty(warehouse.Accounts);Assert.Equal(9640,w.Bank);}
        [Fact] public void FailedFirstCompleteDoesNotPublishWarehouseOrEraseReadyOrder()
        {var w=Ready();Assert.Throws<Exception>(()=>Complete(w,()=>throw new Exception()));Assert.Null(w.GangWarehouse);Assert.True(w.GangBusiness.Supply.Ready);}
        [Fact] public void OwnershipLossOrRivalCannotReceiveOriginalGangShipment()
        {var w=Ready();var s=S();s.Turf=new GangTurfData();s.Turf.Owners[1]="Rival";Assert.False(GangSupplyTransactions.Complete(w,s,A(),"run",Save,out _,C()));s.Gang="Rival";Assert.False(GangSupplyTransactions.Complete(w,s,A(),"run",Save,out _,C()));Assert.Null(w.GangWarehouse);Assert.True(w.GangBusiness.Supply.Ready);Assert.True(GangSupplyTransactions.Refund(w,"run",Save,out _));Assert.Equal(10000,w.Bank);}
        [Fact] public void DifferentAreaCannotReceiveShipment()
        {var w=Ready();Assert.False(GangSupplyTransactions.Complete(w,S(),GangTurfOptions.Defaults().Areas[1],"run",Save,out _,C()));Assert.Null(w.GangWarehouse);}
        [Fact] public void SupplierPriceAndQuantityAreSnapshotsAndNeverChargedAgain()
        {var w=Ready();var c=C();c.Products.Find(p=>p.Id=="weed").Buy=60;Assert.True(Complete(w,catalog:c));Assert.Equal(9640,w.Bank);Assert.Equal(10,w.GangWarehouse.Accounts[0].Stock["weed"]);}
        [Fact] public void CurrentWeightRecheckKeepsReadyOrderWhenCatalogChanges()
        {var w=Ready();var c=C();c.Products.Find(p=>p.Id=="weed").Weight=5;Assert.False(Complete(w,catalog:c));Assert.True(w.GangBusiness.Supply.Ready);Assert.Null(w.GangWarehouse);Assert.True(GangSupplyTransactions.Refund(w,"run",Save,out _));Assert.Equal(10000,w.Bank);}
        [Fact] public void RemovedSkuIsPreservedAndCountedWithConservativeWeight()
        {var w=Ready();var c=C();c.Products.RemoveAll(p=>p.Id=="weed");Assert.True(Complete(w,catalog:c));Assert.Equal(10,w.GangWarehouse.Accounts[0].Stock["weed"]);Assert.Equal(10,GangWarehouseTransactions.Weight(w.GangWarehouse.Accounts[0],c));}
        [Fact] public void SuppliedLifetimeOverflowRefusesCreditAndKeepsReservation()
        {var w=Ready();w.GangWarehouse=new GangWarehouseData();w.GangWarehouse.Accounts.Add(new GangWarehouseAccount{Gang=S().Gang,Area=1,Supplied={{"weed",long.MaxValue}},Withdrawn={{"weed",long.MaxValue}}});Assert.True(w.GangWarehouse.Valid);Assert.False(Complete(w));Assert.True(w.GangBusiness.Supply.Ready);Assert.Empty(w.GangWarehouse.Accounts[0].Stock);}
        [Fact] public void OneSharedSlotPreventsLegalAndCriminalDoubleReservation()
        {var w=Reserved();Assert.False(GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"legal",Save,out _));Assert.Equal(9640,w.Bank);w=W();GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"legal",Save,out _);Assert.False(Begin(w));Assert.Equal(9850,w.Bank);Assert.Equal(GangSupplyCargo.Legal,w.GangBusiness.Supply.CargoKind);}
        [Fact] public void PendingShipmentBlocksWarehouseTransfersAndSales()
        {var w=Reserved();Assert.False(GangWarehouseTransactions.Transfer(w,S(),A(),C(),new GangWarehouseOptions(),"weed",1,true,"move",Save,out _));Assert.Equal(5,w.Dealer.Stock["weed"]);}
        [Fact] public void CompletedShipmentCanBeWithdrawnSoldAndProcessedByExistingSystems()
        {var w=Ready();Complete(w);Assert.True(GangWarehouseTransactions.Transfer(w,S(),A(),C(),new GangWarehouseOptions(),"weed",1,false,"out",Save,out _));Assert.True(GangWarehouseTransactions.Sale(w,S(),A(),C(),"weed",1,"DAVIS",false,70,false,"sale",Save,out _));Assert.Equal(8,w.GangWarehouse.Accounts[0].Stock["weed"]);Assert.Equal(6,w.Dealer.Stock["weed"]);Assert.True(GangIllicitTransactions.Process(w,S(),A(),new GangLaunderingOptions(),70,new DateTime(2013,9,17),"process",Save,out _));Assert.Equal(59,GangTreasuryTransactions.Account(w.GangTreasury,S().Gang).Balance);}
        [Fact] public void ConfigAndOrderRoundtripKeepExactlyThreeSuppliers()
        {var c=CharacterRules.Copy(new GangCriminalSupplyOptions());Assert.True(c.Valid);Assert.Equal(3,c.Route.Suppliers.Count);var w=CharacterRules.Copy(Reserved());Assert.True(w.GangBusiness.Valid);Assert.Equal(3,w.GangBusiness.Supply.Options.Suppliers.Count);}
        [Fact] public void UnknownCargoKindProtectsRatherThanReinterpretsOrder()
        {var w=Reserved();w.GangBusiness.Supply.CargoKind=(GangSupplyCargo)99;Assert.False(w.GangBusiness.Valid);Assert.False(GangSupplyTransactions.Refund(w,"run",Save,out _));Assert.False(Complete(w));Assert.Equal(9640,w.Bank);}
        [Fact] public void LegacyLegalOrderAndWarehouseMissingNewFieldsRemainValid()
        {var w=W();GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"legal",Save,out _);var j=JObject.FromObject(w.GangBusiness.Supply);foreach(string key in new[]{"CargoKind","ProductId","ProductWeight","WarehouseCapacity","StartRadius"})j.Remove(key);var p=j.ToObject<GangSupplyOrder>();Assert.True(p.Valid);Assert.Equal(GangSupplyCargo.Legal,p.CargoKind);var a=JObject.Parse("{Gang:'Mine',Area:1,Stock:{weed:4},Deposited:{weed:10},Withdrawn:{weed:3},Sold:{weed:3}}").ToObject<GangWarehouseAccount>();Assert.True(a.Valid);Assert.Empty(a.Supplied);}
        [Fact] public void MetadataSurvivesCheckpointsAndBothBookCompletion()
        {var w=Ready();var j=JObject.FromObject(w);j["Future"]="wallet";j["GangBusiness"]["Future"]="business";j["GangBusiness"]["Supply"]["Future"]="contract";j["GangBusiness"]["Supply"]["Options"]["Future"]="route";w=j.ToObject<WalletData>();Assert.Equal("contract",(string)JObject.FromObject(w)["GangBusiness"]["Supply"]["Future"]);Complete(w);j=JObject.FromObject(w);Assert.Equal("wallet",(string)j["Future"]);Assert.Equal("business",(string)j["GangBusiness"]["Future"]);Assert.Equal(10,w.GangWarehouse.Accounts[0].Supplied["weed"]);}
        [Fact] public void InvalidConfigCatalogOrProductCannotReserveMoney()
        {var w=W();Assert.False(GangSupplyTransactions.BeginCriminal(w,S(),A(),C(),new GangWarehouseOptions(),new GangCriminalSupplyOptions{Units=0},"weed","bad",Save,out _));Assert.False(GangSupplyTransactions.BeginCriminal(w,S(),A(),C(),new GangWarehouseOptions(),new GangCriminalSupplyOptions(),null,"bad",Save,out _));Assert.Null(w.GangBusiness);Assert.Equal(10000,w.Bank);}
    }
}
