using System;
using Newtonsoft.Json.Linq;
using Xunit;

namespace RPCore.Tests
{
    public class GangSupplyTests
    {
        static readonly Action Save=()=>{};
        static GangTurfDefinition Area()=>GangTurfOptions.Defaults().Areas[0];
        static GangState S()=>new GangState{Gang=Area().DefaultOwner};
        static WalletData W()=>new WalletData{Bank=10000,Cash=123};
        static bool Begin(WalletData w,Action save=null)=>GangSupplyTransactions.Begin(w,S(),Area(),new GangBusinessOptions(),new GangSupplyOptions(),"run",save??Save,out _);
        static WalletData Reserved(){var w=W();Assert.True(Begin(w));return w;}
        static WalletData Loaded(){var w=Reserved();for(int i=0;i<3;i++)Assert.True(GangSupplyTransactions.Load(w,"run",i,Save,out _));return w;}
        static WalletData Ready(){var w=Loaded();for(int n=1;n<=3;n++)Assert.True(GangSupplyTransactions.Deliver(w,"run",n,Save,out _));return w;}
        static bool Complete(WalletData w,Action save=null)=>GangSupplyTransactions.Complete(w,S(),Area(),"run",save??Save,out _);
        [Fact] public void ThreeDefaultSuppliersMatchCafeReferenceCoordinates()
        {var c=new GangSupplyOptions();Assert.True(c.Valid);Assert.Equal(3,c.Suppliers.Count);Assert.Equal(55.15895f,c.Suppliers[0].X);Assert.Equal(-854.6947f,c.Suppliers[1].Y);Assert.Equal(40.10313f,c.Suppliers[2].Z);Assert.Equal(25,c.DiscountPercent);}
        [Fact] public void ReservationDebitsDiscountedPriceWithoutGrantingStockOrRevenue()
        {var w=Reserved();Assert.Equal(9850,w.Bank);Assert.Equal(123,w.Cash);Assert.Empty(w.GangBusiness.Accounts);Assert.Null(w.GangTreasury);Assert.Equal(150,w.GangBusiness.Supply.Cost);Assert.Equal(10,w.GangBusiness.Supply.Units);}
        [Fact] public void InsufficientBankCannotCreateOrderOrChargePartially()
        {var w=W();w.Bank=149;Assert.False(Begin(w));Assert.Null(w.GangBusiness);Assert.Equal(149,w.Bank);}
        [Fact] public void FilledCapacityCannotReserveAnotherLot()
        {var w=W();GangBusinessTransactions.Trade(w,S(),Area(),new GangBusinessOptions{PurchaseUnits=100},false,DateTime.Now,"full",Save,out _);int bank=w.Bank;Assert.False(Begin(w));Assert.Null(w.GangBusiness.Supply);Assert.Equal(bank,w.Bank);}
        [Fact] public void OrderSnapshotsOptionsAreaAndQuantityRatherThanCallerReferences()
        {var w=W();var a=Area();var c=new GangSupplyOptions();var b=new GangBusinessOptions();Assert.True(GangSupplyTransactions.Begin(w,S(),a,b,c,"run",Save,out _));a.Name="changed";c.Suppliers.Clear();b.PurchaseUnits=99;Assert.NotEqual("changed",w.GangBusiness.Supply.Area.Name);Assert.Equal(3,w.GangBusiness.Supply.Options.Suppliers.Count);Assert.Equal(10,w.GangBusiness.Supply.Units);}
        [Fact] public void CannotReserveSecondOrderOrBuyOrSellAroundPendingFreight()
        {var w=Reserved();Assert.False(GangSupplyTransactions.Begin(w,S(),Area(),new GangBusinessOptions(),new GangSupplyOptions(),"second",Save,out _));Assert.False(GangBusinessTransactions.Trade(w,S(),Area(),new GangBusinessOptions(),false,DateTime.Now,"buy",Save,out _));Assert.False(GangBusinessTransactions.Trade(w,S(),Area(),new GangBusinessOptions(),true,DateTime.Now,"sell",Save,out _));Assert.Equal(9850,w.Bank);}
        [Fact] public void LoadingAndDeliveringDoNotGrantPartialStock()
        {var w=Loaded();Assert.False(Complete(w));Assert.True(GangSupplyTransactions.Deliver(w,"run",1,Save,out _));Assert.True(GangSupplyTransactions.Deliver(w,"run",2,Save,out _));Assert.False(Complete(w));Assert.Empty(w.GangBusiness.Accounts);Assert.Equal(9850,w.Bank);}
        [Fact] public void RequiresThreeDistinctLoadsAndSequentialDeliveries()
        {var w=Reserved();Assert.False(GangSupplyTransactions.Deliver(w,"run",1,Save,out _));Assert.False(GangSupplyTransactions.Load(w,"run",3,Save,out _));GangSupplyTransactions.Load(w,"run",0,Save,out _);GangSupplyTransactions.Load(w,"run",0,Save,out bool changed);Assert.False(changed);Assert.Single(w.GangBusiness.Supply.Loaded);GangSupplyTransactions.Load(w,"run",1,Save,out _);GangSupplyTransactions.Load(w,"run",2,Save,out _);Assert.False(GangSupplyTransactions.Deliver(w,"run",2,Save,out _));Assert.False(GangSupplyTransactions.Deliver(w,"run",0,Save,out _));}
        [Fact] public void CompletedRouteAddsStockOnceWithoutSecondDebitOrTreasuryIncome()
        {var w=Ready();Assert.True(Complete(w));Assert.Null(w.GangBusiness.Supply);Assert.Equal(10,w.GangBusiness.Accounts[0].Stock);Assert.Equal(10,w.GangBusiness.Accounts[0].Purchased);Assert.Equal(9850,w.Bank);Assert.Null(w.GangTreasury);Assert.True(Complete(w,()=>throw new Exception()));Assert.Equal(10,w.GangBusiness.Accounts[0].Stock);Assert.False(GangSupplyTransactions.Refund(w,"run",Save,out _));}
        [Fact] public void DeliveredMerchandiseCanBeSoldUsingExistingTreasuryCycle()
        {var w=Ready();Complete(w);Assert.True(GangBusinessTransactions.Trade(w,S(),Area(),new GangBusinessOptions(),true,new DateTime(2013,9,17),"sale",Save,out _));Assert.Equal(5,w.GangBusiness.Accounts[0].Stock);Assert.Equal(175,GangTreasuryTransactions.Account(w.GangTreasury,S().Gang).BusinessIncome);Assert.Equal(9850,w.Bank);}
        [Fact] public void ReservationReplayAfterReloadDoesNotDebitTwice()
        {var w=CharacterRules.Copy(Reserved());Assert.True(Begin(w,()=>throw new Exception()));Assert.Equal(9850,w.Bank);Assert.Empty(w.GangBusiness.Supply.Loaded);}
        [Fact] public void RestartClearsPhysicalCheckpointsAndTheirReceiptsWithoutChargingAgain()
        {var w=Loaded();GangSupplyTransactions.Deliver(w,"run",1,Save,out _);Assert.True(GangSupplyTransactions.Restart(w,"run",Save,out _));Assert.Empty(w.GangBusiness.Supply.Loaded);Assert.Equal(0,w.GangBusiness.Supply.Delivered);Assert.Equal(9850,w.Bank);for(int i=0;i<3;i++){Assert.True(GangSupplyTransactions.Load(w,"run",i,Save,out bool changed));Assert.True(changed);}for(int n=1;n<=3;n++){Assert.True(GangSupplyTransactions.Deliver(w,"run",n,Save,out bool changed));Assert.True(changed);}Assert.True(Complete(w));}
        [Fact] public void ReadyOrderSurvivesReloadAndCannotBeRestarted()
        {var w=CharacterRules.Copy(Ready());Assert.True(w.GangBusiness.Supply.Ready);Assert.False(GangSupplyTransactions.Restart(w,"run",Save,out _));Assert.True(Complete(w));}
        [Theory][InlineData(0)][InlineData(1)][InlineData(3)]
        public void CancelRestoresOnlyReservedCostAtEveryDeliveryStage(int delivered)
        {var w=Loaded();for(int n=1;n<=delivered;n++)GangSupplyTransactions.Deliver(w,"run",n,Save,out _);Assert.True(GangSupplyTransactions.Refund(w,"run",Save,out _));Assert.Null(w.GangBusiness.Supply);Assert.Empty(w.GangBusiness.Accounts);Assert.Equal(10000,w.Bank);Assert.True(GangSupplyTransactions.Refund(w,"run",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(10000,w.Bank);Assert.False(Complete(w));}
        [Fact] public void OwnershipLossPreventsCompletionButDoesNotPreventRefund()
        {var w=Ready();var s=S();s.Turf=new GangTurfData();s.Turf.Owners[Area().Id]="Rival";Assert.False(GangSupplyTransactions.Complete(w,s,Area(),"run",Save,out _));Assert.Empty(w.GangBusiness.Accounts);Assert.True(GangSupplyTransactions.Refund(w,"run",Save,out _));Assert.Equal(10000,w.Bank);}
        [Fact] public void ChangedGangCannotAppropriateReservedStock()
        {var w=Ready();var s=new GangState{Gang="Other",Turf=new GangTurfData()};s.Turf.Owners[Area().Id]="Other";Assert.False(GangSupplyTransactions.Complete(w,s,Area(),"run",Save,out _));Assert.Equal(S().Gang,w.GangBusiness.Supply.Gang);}
        [Fact] public void RefundOverflowRetainsReservationUntilBankHasRoom()
        {var w=Reserved();w.Bank=int.MaxValue;var book=w.GangBusiness;Assert.False(GangSupplyTransactions.Refund(w,"run",Save,out _));Assert.Same(book,w.GangBusiness);Assert.NotNull(w.GangBusiness.Supply);}
        [Fact] public void FailedBeginSaveRestoresBankAbsentBookAndOtherReferences()
        {var w=W();w.Dmv=new DmvData();var dmv=w.Dmv;Assert.Throws<Exception>(()=>Begin(w,()=>throw new Exception()));Assert.Null(w.GangBusiness);Assert.Equal(10000,w.Bank);Assert.Same(dmv,w.Dmv);}
        [Fact] public void FailedLoadSaveDoesNotClaimBoxOrPublishBook()
        {var w=Reserved();var b=w.GangBusiness;Assert.Throws<Exception>(()=>GangSupplyTransactions.Load(w,"run",0,()=>throw new Exception(),out _));Assert.Same(b,w.GangBusiness);Assert.Empty(b.Supply.Loaded);}
        [Fact] public void FailedDeliverySaveKeepsUnconfirmedBoxAndCount()
        {var w=Loaded();var b=w.GangBusiness;Assert.Throws<Exception>(()=>GangSupplyTransactions.Deliver(w,"run",1,()=>throw new Exception(),out _));Assert.Same(b,w.GangBusiness);Assert.Equal(0,b.Supply.Delivered);}
        [Fact] public void FailedRestartSaveKeepsOldCheckpointsAndReceipts()
        {var w=Loaded();var b=w.GangBusiness;Assert.Throws<Exception>(()=>GangSupplyTransactions.Restart(w,"run",()=>throw new Exception(),out _));Assert.Same(b,w.GangBusiness);Assert.Equal(3,b.Supply.Loaded.Count);Assert.Contains("run:supply-load:0",b.Receipts);}
        [Fact] public void FailedCompleteSaveKeepsReadyOrderAndBankWithoutStock()
        {var w=Ready();var b=w.GangBusiness;Assert.Throws<Exception>(()=>Complete(w,()=>throw new Exception()));Assert.Same(b,w.GangBusiness);Assert.True(b.Supply.Ready);Assert.Empty(b.Accounts);Assert.Equal(9850,w.Bank);}
        [Fact] public void FailedRefundSaveRestoresOriginalBookAndBank()
        {var w=Reserved();var b=w.GangBusiness;Assert.Throws<Exception>(()=>GangSupplyTransactions.Refund(w,"run",()=>throw new Exception(),out _));Assert.Same(b,w.GangBusiness);Assert.NotNull(b.Supply);Assert.Equal(9850,w.Bank);}
        [Fact] public void BadPendingDataIsProtectedAcrossFreightAndCommerce()
        {var w=Reserved();w.GangBusiness.Supply.Delivered=3;Assert.False(w.GangBusiness.Valid);Assert.False(GangSupplyTransactions.Refund(w,"run",Save,out _));Assert.False(GangSupplyTransactions.Load(w,"run",0,Save,out _));Assert.Equal(9850,w.Bank);}
        [Fact] public void CompletionCapacityOrLifetimeCounterOverflowKeepsReadyReservation()
        {var w=Ready();w.GangBusiness.Accounts.Add(new GangBusinessAccount{Gang=S().Gang,Area=1,Stock=100,Purchased=100});Assert.False(Complete(w));Assert.True(w.GangBusiness.Supply.Ready);w.GangBusiness.Accounts[0].Stock=0;w.GangBusiness.Accounts[0].Purchased=w.GangBusiness.Accounts[0].Sold=long.MaxValue;Assert.False(Complete(w));Assert.Equal(0,w.GangBusiness.Accounts[0].Stock);}
        [Fact] public void FutureMetadataSurvivesCheckpointsAndCompletion()
        {var w=Reserved();var j=JObject.FromObject(w);j["Future"]="wallet";j["GangBusiness"]["Future"]="book";j["GangBusiness"]["Supply"]["Future"]="order";j["GangBusiness"]["Supply"]["Options"]["Future"]="options";w=j.ToObject<WalletData>();GangSupplyTransactions.Load(w,"run",0,Save,out _);j=JObject.FromObject(w);Assert.Equal("order",(string)j["GangBusiness"]["Supply"]["Future"]);Assert.Equal("options",(string)j["GangBusiness"]["Supply"]["Options"]["Future"]);GangSupplyTransactions.Refund(w,"run",Save,out _);j=JObject.FromObject(w);Assert.Equal("wallet",(string)j["Future"]);Assert.Equal("book",(string)j["GangBusiness"]["Future"]);}
        [Fact] public void ProfileWalletsNeverShareOrderOrReservedFunds()
        {var w=Reserved();var other=W();Assert.False(GangSupplyTransactions.Load(other,"run",0,Save,out _));Assert.False(GangSupplyTransactions.Refund(other,"run",Save,out _));Assert.Null(other.GangBusiness);Assert.Equal(10000,other.Bank);Assert.Equal(9850,w.Bank);}
        [Fact] public void WrongIdsCannotLoadUnloadCompleteOrRefund()
        {var w=Ready();Assert.False(GangSupplyTransactions.Load(w,"other",0,Save,out _));Assert.False(GangSupplyTransactions.Deliver(w,"other",1,Save,out _));Assert.False(GangSupplyTransactions.Complete(w,S(),Area(),"other",Save,out _));Assert.False(GangSupplyTransactions.Refund(w,"other",Save,out _));Assert.True(w.GangBusiness.Supply.Ready);}
        [Fact] public void InvalidConfigAndUnownedAreaCannotReserveFunds()
        {var w=W();Assert.False(GangSupplyTransactions.Begin(w,S(),Area(),new GangBusinessOptions(),new GangSupplyOptions{DiscountPercent=99},"bad",Save,out _));Assert.False(GangSupplyTransactions.Begin(w,new GangState(),Area(),new GangBusinessOptions(),new GangSupplyOptions(),"bad",Save,out _));Assert.Null(w.GangBusiness);}
        [Fact] public void ConfigAndOrderReloadDoNotAppendDefaultSuppliersAgain()
        {var c=CharacterRules.Copy(new GangSupplyOptions());Assert.True(c.Valid);Assert.Equal(3,c.Suppliers.Count);var w=CharacterRules.Copy(Reserved());Assert.True(w.GangBusiness.Valid);Assert.Equal(3,w.GangBusiness.Supply.Options.Suppliers.Count);}
        [Fact] public void RepeatedSupplierCoordinatesAreRejected()
        {var c=new GangSupplyOptions();c.Suppliers[1]=CharacterRules.Copy(c.Suppliers[0]);Assert.False(c.Valid);}
        [Fact] public void LegacyBusinessWithoutFreightRemainsValidAndUnchanged()
        {var b=JObject.Parse("{Accounts:[{Gang:'Mine',Area:1,Stock:5,Purchased:10,Sold:5,Revenue:175}],Receipts:['old'],Future:'keep'}").ToObject<GangBusinessData>();Assert.True(b.Valid);Assert.Null(b.Supply);Assert.Equal("keep",(string)JObject.FromObject(b)["Future"]);}
    }
}
