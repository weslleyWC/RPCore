using System;
using Newtonsoft.Json;
using Xunit;

namespace RPCore.Tests
{
    public class PdmTests
    {
        static PdmOptions Options()=>PdmDefaults.Load();
        static WalletData Funded()
        {var w=new WalletData{Bank=50000,Cash=500};Assert.True(PdmTransactions.Purchase(w,Options(),"purchase",()=>{},out _));Assert.True(PdmTransactions.Transfer(w,10000,true,"capital",()=>{},out _));return w;}
        static WalletData Stock()
        {var w=Funded();Assert.True(PdmTransactions.Buy(w,Options(),"asea","stockA","buy",()=>{},out _));return w;}
        static void Begin(WalletData w)=>Assert.True(PdmTransactions.Begin(w,Options(),"stockA","delivery",0,123,()=>{},out _));
        [Fact] public void DefaultsContainPublicOfficeReferencePickupAndTenClients()
        {var c=Options();Assert.True(c.Valid);Assert.Equal(25000,c.PurchasePrice);Assert.Equal(3,c.Catalog.Count);Assert.Equal(10,c.Customers.Count);Assert.Equal(-63.2041f,c.Office.X);Assert.Equal(-25.2498f,c.Spawn.X);}
        [Fact] public void OwnershipCostsBankOnceAndDoesNotGrantFreeStockOrCapital()
        {var w=new WalletData{Bank=35000,Cash=100};Assert.True(PdmTransactions.Purchase(w,Options(),"p",()=>{},out _));Assert.Equal(10000,w.Bank);Assert.Equal(100,w.Cash);Assert.True(w.Pdm.Owned);Assert.Equal(0,w.Pdm.Cash);Assert.Empty(w.Pdm.Stock);Assert.False(PdmTransactions.Purchase(w,Options(),"another",()=>throw new Exception(),out _));Assert.True(PdmTransactions.Purchase(w,Options(),"p",()=>throw new Exception(),out bool changed));Assert.False(changed);}
        [Fact] public void FailedOwnershipSaveRestoresNullFieldAndBank()
        {var w=new WalletData{Bank=35000};string old=JsonConvert.SerializeObject(w);Assert.Throws<Exception>(()=>PdmTransactions.Purchase(w,Options(),"p",()=>throw new Exception(),out _));Assert.Equal(old,JsonConvert.SerializeObject(w));}
        [Fact] public void InsufficientOwnershipFundsDoNotCreateCompany()
        {var w=new WalletData{Bank=24999};Assert.False(PdmTransactions.Purchase(w,Options(),"p",()=>throw new Exception(),out _));Assert.Null(w.Pdm);}
        [Fact] public void CapitalTransferIsOneCheckpointAcrossBothBalances()
        {var w=Funded();Assert.Equal(15000,w.Bank);Assert.Equal(10000,w.Pdm.Cash);int calls=0;Assert.True(PdmTransactions.Transfer(w,1000,false,"withdraw",()=>{calls++;Assert.Equal(16000,w.Bank);Assert.Equal(9000,w.Pdm.Cash);},out _));Assert.Equal(1,calls);Assert.Equal(1000,w.Pdm.Withdrawn);}
        [Theory] [InlineData(true)] [InlineData(false)]
        public void FailedTransferRestoresBothBalancesAndLedger(bool deposit)
        {var w=Funded();string old=JsonConvert.SerializeObject(w);Assert.Throws<Exception>(()=>PdmTransactions.Transfer(w,1000,deposit,"t",()=>throw new Exception(),out _));Assert.Equal(old,JsonConvert.SerializeObject(w));}
        [Fact] public void TransferReplaySurvivesReloadWithoutDuplication()
        {var w=Funded();PdmTransactions.Transfer(w,1000,false,"t",()=>{},out _);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(PdmTransactions.Transfer(w,1000,false,"t",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(16000,w.Bank);Assert.Equal(9000,w.Pdm.Cash);}
        [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(1000001)]
        public void InvalidTransfersCannotMoveFunds(int amount)
        {var w=Funded();Assert.False(PdmTransactions.Transfer(w,amount,true,"t",()=>throw new Exception(),out _));Assert.Equal(10000,w.Pdm.Cash);}
        [Fact] public void OwnershipAndAvailableBalancesAreRequiredForTransfer()
        {var w=new WalletData{Bank=50000};Assert.False(PdmTransactions.Transfer(w,1000,true,"t",()=>{},out _));w=Funded();Assert.False(PdmTransactions.Transfer(w,20000,false,"t",()=>{},out _));Assert.False(PdmTransactions.Transfer(w,20000,true,"t",()=>{},out _));}
        [Fact] public void BuyingStockSpendsCompanyCashAndKeepsPersonalBank()
        {var w=Stock();Assert.Equal(15000,w.Bank);Assert.Equal(2000,w.Pdm.Cash);Assert.Single(w.Pdm.Stock);Assert.Equal(8000,w.Pdm.StockSpent);Assert.Equal(11200,w.Pdm.Stock[0].Price);Assert.StartsWith("PDM",w.Pdm.Stock[0].Plate);}
        [Fact] public void FailedStockSaveDoesNotLoseCompanyMoney()
        {var w=Funded();string old=JsonConvert.SerializeObject(w);Assert.Throws<Exception>(()=>PdmTransactions.Buy(w,Options(),"asea","stockA","b",()=>throw new Exception(),out _));Assert.Equal(old,JsonConvert.SerializeObject(w));}
        [Fact] public void StockReplayAndPlateCollisionCannotDuplicateAsset()
        {var w=Stock();Assert.True(PdmTransactions.Buy(w,Options(),"asea","stockA","buy",()=>throw new Exception(),out bool changed));Assert.False(changed);PdmTransactions.Transfer(w,10000,true,"more",()=>{},out _);Assert.False(PdmTransactions.Buy(w,Options(),"asea","stockAB","different",()=>throw new Exception(),out _));Assert.Single(w.Pdm.Stock);Assert.Equal(12000,w.Pdm.Cash);}
        [Fact] public void StockCapacityRejectsWholePurchase()
        {var w=Funded();var c=Options();c.Capacity=1;Assert.True(PdmTransactions.Buy(w,c,"asea","aaaaa1","b1",()=>{},out _));PdmTransactions.Transfer(w,10000,true,"more",()=>{},out _);Assert.False(PdmTransactions.Buy(w,c,"asea","bbbbb2","b2",()=>throw new Exception(),out _));Assert.Equal(12000,w.Pdm.Cash);Assert.Single(w.Pdm.Stock);}
        [Fact] public void UnknownOfferCannotDebitFunds()
        {var w=Funded();Assert.False(PdmTransactions.Buy(w,Options(),"unknown","stock","b",()=>throw new Exception(),out _));Assert.Equal(10000,w.Pdm.Cash);}
        [Fact] public void BeginReservesExactlyOneCompanyStockUnitWithoutPaying()
        {var w=Stock();Begin(w);Assert.Equal("stockA",w.Pdm.Active.StockId);Assert.Equal(123,w.Pdm.Active.VehicleHandle);Assert.Single(w.Pdm.Stock);Assert.Equal(2000,w.Pdm.Cash);Assert.False(PdmTransactions.Begin(w,Options(),"stockA","other",0,456,()=>{},out _));}
        [Fact] public void FailedBeginKeepsPaidStockAndNoActiveOperation()
        {var w=Stock();Assert.Throws<Exception>(()=>PdmTransactions.Begin(w,Options(),"stockA","delivery",0,123,()=>throw new Exception(),out _));Assert.Null(w.Pdm.Active);Assert.Single(w.Pdm.Stock);}
        [Fact] public void SaleCannotHappenWithoutMatchingDelivery()
        {var w=Stock();Assert.False(PdmTransactions.Sell(w,"delivery",1000,1000,()=>throw new Exception(),out _));Begin(w);Assert.False(PdmTransactions.Sell(w,"other",1000,1000,()=>{},out _));Assert.Equal(2000,w.Pdm.Cash);}
        [Fact] public void SaleRemovesStockCreditsCompanyAndClosesContractAtomically()
        {var w=Stock();Begin(w);int calls=0;Assert.True(PdmTransactions.Sell(w,"delivery",1000,1000,()=>{calls++;Assert.Empty(w.Pdm.Stock);Assert.Null(w.Pdm.Active);Assert.Equal(13200,w.Pdm.Cash);},out _));Assert.Equal(1,calls);Assert.Equal(15000,w.Bank);Assert.Equal(1,w.Pdm.Sold);Assert.Equal(11200,w.Pdm.Revenue);Assert.Equal(8000,w.Pdm.SoldCost);}
        [Fact] public void FailedSaleRestoresStockOperationAndCompanyCash()
        {var w=Stock();Begin(w);var old=w.Pdm;string before=JsonConvert.SerializeObject(w);Assert.Throws<Exception>(()=>PdmTransactions.Sell(w,"delivery",1000,1000,()=>throw new Exception(),out _));Assert.Same(old,w.Pdm);Assert.Equal(before,JsonConvert.SerializeObject(w));}
        [Fact] public void SaleReplayAfterReloadCannotPayOrRemoveTwice()
        {var w=Stock();Begin(w);PdmTransactions.Sell(w,"delivery",1000,1000,()=>{},out _);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(PdmTransactions.Sell(w,"delivery",1000,1000,()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(13200,w.Pdm.Cash);Assert.Equal(1,w.Pdm.Sold);}
        [Fact] public void SaleDiscountUsesWorstSavedOrCurrentBodyCondition()
        {var w=Stock();Begin(w);Assert.Equal(9800,PdmTransactions.Quote(w.Pdm.Stock[0],500,500));Assert.True(PdmTransactions.Remember(w,"delivery",500,500,"damage",()=>{},out _));Assert.Equal(9800,PdmTransactions.Quote(w.Pdm.Stock[0],1000,1000));PdmTransactions.Sell(w,"delivery",1000,1000,()=>{},out _);Assert.Equal(11800,w.Pdm.Cash);}
        [Fact] public void CancelKeepsPaidAssetWithDamageAndNoPayout()
        {var w=Stock();Begin(w);Assert.True(PdmTransactions.Cancel(w,"delivery",500,400,()=>{},out _));Assert.Null(w.Pdm.Active);Assert.Single(w.Pdm.Stock);Assert.Equal(400,w.Pdm.Stock[0].Body);Assert.Equal(2000,w.Pdm.Cash);Assert.Equal(0,w.Pdm.Sold);}
        [Fact] public void CancelFailureAndRetryCannotLoseStockOrResetDamage()
        {var w=Stock();Begin(w);string old=JsonConvert.SerializeObject(w);Assert.Throws<Exception>(()=>PdmTransactions.Cancel(w,"delivery",300,400,()=>throw new Exception(),out _));Assert.Equal(old,JsonConvert.SerializeObject(w));Assert.True(PdmTransactions.Cancel(w,"delivery",300,400,()=>{},out _));Assert.True(PdmTransactions.Cancel(w,"delivery",300,400,()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(300,w.Pdm.Stock[0].Engine);}
        [Fact] public void DamageCheckpointSurvivesReloadBeforePhysicalCleanup()
        {var w=Stock();Begin(w);Assert.True(PdmTransactions.Remember(w,"delivery",200,500,"before-delete",()=>{},out _));w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.Equal(200,w.Pdm.Stock[0].Engine);Assert.NotNull(w.Pdm.Active);Assert.True(PdmTransactions.Cancel(w,"delivery",w.Pdm.Stock[0].Engine,w.Pdm.Stock[0].Body,()=>{},out _));Assert.Equal(200,w.Pdm.Stock[0].Engine);}
        [Fact] public void RepairChargesCompanyOnceAndNeverRepairsPersonalCars()
        {var w=Stock();Begin(w);PdmTransactions.Cancel(w,"delivery",100,100,()=>{},out _);Assert.True(PdmTransactions.Repair(w,Options(),"stockA","repair",()=>{},out _));Assert.Equal(1000,w.Pdm.Stock[0].Engine);Assert.Equal(1000,w.Pdm.Stock[0].Body);Assert.Equal(1000,w.Pdm.Cash);Assert.Equal(1000,w.Pdm.Repairs);Assert.False(PdmTransactions.Repair(w,Options(),"stockA","again",()=>throw new Exception(),out _));Assert.Equal(15000,w.Bank);}
        [Fact] public void FailedRepairPreservesDamageAndCash()
        {var w=Stock();w.Pdm.Stock[0].Engine=100;string old=JsonConvert.SerializeObject(w);Assert.Throws<Exception>(()=>PdmTransactions.Repair(w,Options(),"stockA","repair",()=>throw new Exception(),out _));Assert.Equal(old,JsonConvert.SerializeObject(w));}
        [Fact] public void DamagedCarMustBeRepairedBeforeAnotherDelivery()
        {var w=Stock();w.Pdm.Stock[0].Engine=100;Assert.False(PdmTransactions.Begin(w,Options(),"stockA","delivery",0,123,()=>throw new Exception(),out _));Assert.Null(w.Pdm.Active);}
        [Fact] public void FullBusinessCycleProducesOnlyRealMargin()
        {var w=Stock();Begin(w);PdmTransactions.Sell(w,"delivery",1000,1000,()=>{},out _);Assert.True(PdmTransactions.Transfer(w,13200,false,"withdraw",()=>{},out _));Assert.Equal(28200,w.Bank);Assert.Equal(0,w.Pdm.Cash);Assert.Equal(3200,w.Pdm.Revenue-w.Pdm.SoldCost-w.Pdm.Repairs);Assert.Equal(500,w.Cash);}
        [Fact] public void BalanceOverflowCannotDeleteStockOrMoveMoney()
        {var w=Stock();Begin(w);w.Pdm.Cash=int.MaxValue;Assert.False(PdmTransactions.Sell(w,"delivery",1000,1000,()=>throw new Exception(),out _));Assert.Single(w.Pdm.Stock);Assert.NotNull(w.Pdm.Active);w.Bank=int.MaxValue;Assert.False(PdmTransactions.Transfer(w,1000,false,"withdraw",()=>{},out _));Assert.Equal(int.MaxValue,w.Pdm.Cash);}
        [Fact] public void InvalidSavedCompanyStateIsPreservedRatherThanReset()
        {var w=Funded();w.Pdm.Cash=-1;string old=JsonConvert.SerializeObject(w);Assert.False(PdmTransactions.Transfer(w,1000,true,"t",()=>{},out _));Assert.Equal(old,JsonConvert.SerializeObject(w));}
        [Fact] public void PersonalVehiclesLicensesJobsAndUnknownFieldsSurviveCompanyCycle()
        {var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":50000,\"Future\":true,\"Pdm\":{\"FutureCompany\":42},\"Transport\":{\"Vehicles\":[{\"Id\":\"personal\",\"Plate\":\"KEEP\",\"EngineHealth\":800}]},\"Hunting\":{\"Licensed\":true},\"Fishing\":{\"RodOwned\":true},\"Crafting\":{\"Stock\":{\"leather\":1}},\"Dealer\":{\"Stock\":{\"weed\":2}},\"Streamer\":{\"Followers\":42},\"Drag\":{\"Wins\":2},\"Jobs\":{\"Jobs\":{\"pilot_training\":{\"CompletedShifts\":1}}}}");string vehicles=JsonConvert.SerializeObject(w.Transport),jobs=JsonConvert.SerializeObject(w.Jobs),stream=JsonConvert.SerializeObject(w.Streamer),hunt=JsonConvert.SerializeObject(w.Hunting),fish=JsonConvert.SerializeObject(w.Fishing),craft=JsonConvert.SerializeObject(w.Crafting),dealer=JsonConvert.SerializeObject(w.Dealer),drag=JsonConvert.SerializeObject(w.Drag);PdmTransactions.Purchase(w,Options(),"purchase",()=>{},out _);PdmTransactions.Transfer(w,10000,true,"capital",()=>{},out _);PdmTransactions.Buy(w,Options(),"asea","stockA","buy",()=>{},out _);Begin(w);PdmTransactions.Sell(w,"delivery",1000,1000,()=>{},out _);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.Equal(42,(int)w.Pdm.Extra["FutureCompany"]);Assert.True((bool)w.Extra["Future"]);Assert.Equal(vehicles,JsonConvert.SerializeObject(w.Transport));Assert.Equal(jobs,JsonConvert.SerializeObject(w.Jobs));Assert.Equal(stream,JsonConvert.SerializeObject(w.Streamer));Assert.Equal(hunt,JsonConvert.SerializeObject(w.Hunting));Assert.Equal(fish,JsonConvert.SerializeObject(w.Fishing));Assert.Equal(craft,JsonConvert.SerializeObject(w.Crafting));Assert.Equal(dealer,JsonConvert.SerializeObject(w.Dealer));Assert.Equal(drag,JsonConvert.SerializeObject(w.Drag));Assert.True(FlightRules.Qualified(w.Jobs));}
        [Fact] public void CatalogChangesDoNotRewriteAlreadyPaidVehiclePrice()
        {var w=Stock();var c=Options();c.Catalog[0].Price=99999;Begin(w);PdmTransactions.Sell(w,"delivery",1000,1000,()=>{},out _);Assert.Equal(11200,w.Pdm.Revenue);}
        [Fact] public void DuplicateCatalogOrNonfinitePointIsInvalid()
        {var c=Options();c.Catalog[1].Id=c.Catalog[0].Id;Assert.False(c.Valid);c=Options();c.Office.X=float.NaN;Assert.False(c.Valid);}
        [Fact] public void HandoverNeedsCorrectCarThenFullEligibleDuration()
        {var r=new PdmRun("delivery","stock",3);Assert.False(r.Handover(0,true));r.Enter(false);Assert.Equal(PdmStage.Collect,r.Stage);r.Enter(true);Assert.True(r.Handover(0,true));Assert.False(r.Commit(2999,true,()=>throw new Exception()));Assert.True(r.Commit(3000,true,()=>true));Assert.False(r.Commit(4000,true,()=>throw new Exception()));}
        [Fact] public void InterruptedKeysRequireAnotherHandoverWithoutPaying()
        {var r=new PdmRun("delivery","stock",3);r.Enter(true);r.Handover(0,true);Assert.False(r.Commit(3000,false,()=>throw new Exception()));Assert.Equal(PdmStage.Drive,r.Stage);Assert.True(r.Handover(4000,true));Assert.False(r.Commit(6999,true,()=>throw new Exception()));Assert.True(r.Commit(7000,true,()=>true));}
        [Fact] public void FailedOrThrowingSaleKeepsTimerRetryable()
        {var r=new PdmRun("delivery","stock",3);r.Enter(true);r.Handover(0,true);Assert.False(r.Commit(3000,true,()=>false));Assert.Throws<Exception>(()=>r.Commit(4000,true,()=>throw new Exception()));Assert.Equal(PdmStage.Handover,r.Stage);Assert.True(r.Commit(9000,true,()=>true));}
        [Fact] public void CancellationCannotCreateSale()
        {var r=new PdmRun("delivery","stock",3);r.Enter(true);r.Handover(0,true);r.Cancel();Assert.False(r.Commit(10000,true,()=>throw new Exception()));Assert.Equal(PdmStage.Cancelled,r.Stage);}
        [Fact] public void HandoverClockHandlesOverflow()
        {int start=int.MaxValue-1000;var r=new PdmRun("delivery","stock",3);r.Enter(true);r.Handover(start,true);Assert.True(r.Commit(unchecked(start+3000),true,()=>true));}
    }
}
