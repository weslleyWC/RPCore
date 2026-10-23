using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;

namespace RPCore.Tests
{
    public class HuntingTests
    {
        static WalletData Licensed(int bank=2000)=>new WalletData{Bank=bank,Hunting=new HuntingData{Licensed=true,Bait=5}};
        [Fact] public void LicenseDebitsBankAndSurvivesReloadWithoutTouchingCash()
        {var c=HuntingDefaults.Load();var w=new WalletData{Bank=1000,Cash=500};Assert.True(HuntingTransactions.License(w,c,"license",()=>{},out bool changed));Assert.True(changed);Assert.Equal(250,w.Bank);Assert.Equal(500,w.Cash);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(w.Hunting.Licensed);Assert.True(HuntingTransactions.License(w,c,"license",()=>throw new Exception(),out changed));Assert.False(changed);Assert.Equal(250,w.Bank);}
        [Fact] public void CannotPurchaseSecondLicenseOrSpendWithoutFunds()
        {var c=HuntingDefaults.Load();var w=new WalletData{Bank=100};Assert.False(HuntingTransactions.License(w,c,"l",()=>throw new Exception(),out _));Assert.Null(w.Hunting);w=Licensed();Assert.False(HuntingTransactions.License(w,c,"l2",()=>throw new Exception(),out _));Assert.Equal(2000,w.Bank);}
        [Fact] public void FailedLicenseSaveRollsBackEntireWalletAndEntitlement()
        {var w=new WalletData{Bank=1000};string before=JsonConvert.SerializeObject(w);Assert.Throws<InvalidOperationException>(()=>HuntingTransactions.License(w,HuntingDefaults.Load(),"l",()=>throw new InvalidOperationException(),out _));Assert.Equal(before,JsonConvert.SerializeObject(w));}
        [Fact] public void BaitPurchaseAndConsumptionAreAtomicAndReplaySafe()
        {
            var w=Licensed();var c=HuntingDefaults.Load();Assert.True(HuntingTransactions.BuyBait(w,c,5,"buy",()=>{},out _));Assert.Equal(10,w.Hunting.Bait);Assert.Equal(1900,w.Bank);
            Assert.True(HuntingTransactions.UseBait(w,"use",()=>{},out bool changed));Assert.True(changed);Assert.Equal(9,w.Hunting.Bait);
            Assert.True(HuntingTransactions.UseBait(w,"use",()=>throw new Exception(),out changed));Assert.False(changed);Assert.Equal(9,w.Hunting.Bait);
        }
        [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(21)]
        public void InvalidBaitQuantityCannotDebitWallet(int qty)
        {var w=Licensed();Assert.False(HuntingTransactions.BuyBait(w,HuntingDefaults.Load(),qty,"b",()=>throw new Exception(),out _));Assert.Equal(2000,w.Bank);Assert.Equal(5,w.Hunting.Bait);}
        [Fact] public void NoLicenseOrNoBaitCannotStartBaitTransaction()
        {var w=new WalletData{Bank=1000};Assert.False(HuntingTransactions.BuyBait(w,HuntingDefaults.Load(),5,"b",()=>throw new Exception(),out _));Assert.False(HuntingTransactions.UseBait(w,"u",()=>throw new Exception(),out _));w=Licensed();w.Hunting.Bait=0;Assert.False(HuntingTransactions.UseBait(w,"u",()=>throw new Exception(),out _));}
        [Fact] public void FailedBaitCheckpointKeepsBaitAndReceiptUnchanged()
        {var w=Licensed();string before=JsonConvert.SerializeObject(w);Assert.Throws<InvalidOperationException>(()=>HuntingTransactions.UseBait(w,"u",()=>throw new InvalidOperationException(),out _));Assert.Equal(before,JsonConvert.SerializeObject(w));}
        [Theory] [InlineData(HuntSpecies.Deer,4)] [InlineData(HuntSpecies.Boar,4)] [InlineData(HuntSpecies.Rabbit,2)]
        public void SpeciesLootIncludesMeatHideAndApplicableTrophy(HuntSpecies species,int meat)
        {var loot=HuntingLoot.Create(species,3);Assert.Equal(meat,loot[species==HuntSpecies.Deer?"deer_meat":species==HuntSpecies.Boar?"boar_meat":"rabbit_meat"]);Assert.Equal(1,loot["hide_perfect"]);if(species==HuntSpecies.Deer)Assert.Equal(1,loot["antler"]);if(species==HuntSpecies.Rabbit)Assert.Equal(1,loot["rabbit_foot"]);}
        [Theory] [InlineData(1,"hide_rough")] [InlineData(2,"hide_good")] [InlineData(3,"hide_perfect")]
        public void QualityChangesHideAndValue(int quality,string hide)
        {Assert.True(HuntingLoot.Create(HuntSpecies.Deer,quality).ContainsKey(hide));Assert.Empty(HuntingLoot.Create(HuntSpecies.Deer,0));}
        [Fact] public void HarvestIsSavedWithReceiptAndCannotDuplicateAfterReload()
        {
            var w=Licensed();var c=HuntingDefaults.Load();var loot=HuntingLoot.Create(HuntSpecies.Deer,3);
            Assert.True(HuntingTransactions.Harvest(w,c,loot,"animal",false,()=>{},out bool changed));Assert.True(changed);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));
            Assert.True(HuntingTransactions.Harvest(w,c,loot,"animal",false,()=>throw new Exception(),out changed));Assert.False(changed);Assert.Equal(4,w.Hunting.Bag["deer_meat"]);Assert.Equal(1,w.Hunting.Harvests);Assert.Equal(2000,w.Bank);
        }
        [Fact] public void BagCapacityRejectsWholeBundleWithoutLosingCurrentMaterials()
        {var w=Licensed();var c=HuntingDefaults.Load();c.BagCapacity=5;w.Hunting.Bag["deer_meat"]=5;string before=JsonConvert.SerializeObject(w);Assert.False(HuntingTransactions.Harvest(w,c,HuntingLoot.Create(HuntSpecies.Deer,3),"full",false,()=>throw new Exception(),out _));Assert.Equal(before,JsonConvert.SerializeObject(w));}
        [Fact] public void FailedHarvestSaveDoesNotAddMaterialsOrReceipt()
        {var w=Licensed();string before=JsonConvert.SerializeObject(w);Assert.Throws<InvalidOperationException>(()=>HuntingTransactions.Harvest(w,HuntingDefaults.Load(),HuntingLoot.Create(HuntSpecies.Boar,2),"a",true,()=>throw new InvalidOperationException(),out _));Assert.Equal(before,JsonConvert.SerializeObject(w));}
        [Fact] public void SaleClearsMaterialsAndCreditsBankInSameCheckpoint()
        {
            var w=Licensed();var c=HuntingDefaults.Load();HuntingTransactions.Harvest(w,c,HuntingLoot.Create(HuntSpecies.Deer,3),"a",true,()=>{},out _);int quote=HuntingTransactions.Quote(w.Hunting,c);
            Assert.Equal(330,quote);Assert.True(HuntingTransactions.Sell(w,c,"sale",()=>{Assert.Empty(w.Hunting.Bag);Assert.Equal(2330,w.Bank);},out bool changed));
            Assert.True(changed);Assert.Equal(330,w.Hunting.Earned);Assert.Equal(1,w.Hunting.Trips);Assert.False(HuntingTransactions.Sell(w,c,"sale",()=>throw new Exception(),out _));Assert.Equal(2330,w.Bank);
        }
        [Fact] public void FailedSaleCheckpointRestoresBagBankAndLifetimeEarnings()
        {var w=Licensed();var c=HuntingDefaults.Load();HuntingTransactions.Harvest(w,c,HuntingLoot.Create(HuntSpecies.Deer,3),"a",false,()=>{},out _);string before=JsonConvert.SerializeObject(w);Assert.Throws<InvalidOperationException>(()=>HuntingTransactions.Sell(w,c,"sale",()=>throw new InvalidOperationException(),out _));Assert.Equal(before,JsonConvert.SerializeObject(w));}
        [Fact] public void UnknownMaterialsAndAllOtherSaveDataRemainIntact()
        {
            var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":100,\"Future\":true,\"Jobs\":{\"Jobs\":{\"pilot_training\":{\"CompletedShifts\":1},\"prison\":{\"Deliveries\":5}}},\"Hunting\":{\"Licensed\":true,\"Bag\":{\"deer_meat\":2,\"custom_mod\":3},\"Unknown\":42}}");
            string jobs=JsonConvert.SerializeObject(w.Jobs);HuntingTransactions.Sell(w,HuntingDefaults.Load(),"sale",()=>{},out _);
            Assert.Equal(190,w.Bank);Assert.Equal(3,w.Hunting.Bag["custom_mod"]);Assert.Equal(jobs,JsonConvert.SerializeObject(w.Jobs));Assert.Equal(42,(int)w.Hunting.Extra["Unknown"]);Assert.True((bool)w.Extra["Future"]);Assert.True(FlightRules.Qualified(w.Jobs));
        }
        [Fact] public void BankOverflowCannotRemoveGoods()
        {var w=Licensed(int.MaxValue);w.Hunting.Bag["deer_meat"]=2;Assert.False(HuntingTransactions.Sell(w,HuntingDefaults.Load(),"sale",()=>throw new Exception(),out _));Assert.Equal(2,w.Hunting.Bag["deer_meat"]);Assert.Equal(int.MaxValue,w.Bank);}
        [Fact] public void CorruptHuntingStockIsNotSilentlyOverwritten()
        {var w=Licensed();w.Hunting.Bag["deer_meat"]=-1;string before=JsonConvert.SerializeObject(w);Assert.False(HuntingTransactions.BuyBait(w,HuntingDefaults.Load(),1,"b",()=>throw new Exception(),out _));Assert.Equal(before,JsonConvert.SerializeObject(w));}
        [Fact] public void HarvestNeedsDeadAnimalClosePlayerAndFullDuration()
        {var r=new HuntingRun(3,5);Assert.False(r.BeginHarvest(0,true));Assert.True(r.Track());Assert.False(r.FoundCarcass(false));Assert.True(r.FoundCarcass(true));Assert.False(r.BeginHarvest(0,false));r.BeginHarvest(0,true);Assert.False(r.Commit(4999,true,(_,_)=>throw new Exception()));Assert.True(r.Commit(5000,true,(_,_)=>true));Assert.Equal(1,r.Collected);Assert.Equal(HuntingStage.Ready,r.Stage);}
        [Fact] public void InterruptedHarvestResetsWithoutCreditingOrLosingCarcass()
        {var r=new HuntingRun(3,5);r.Track();r.FoundCarcass(true);r.BeginHarvest(0,true);Assert.False(r.Commit(5000,false,(_,_)=>throw new Exception()));Assert.Equal(HuntingStage.Carcass,r.Stage);r.BeginHarvest(6000,true);Assert.False(r.Commit(10999,true,(_,_)=>throw new Exception()));Assert.True(r.Commit(11000,true,(_,_)=>true));}
        [Fact] public void FailedCollectionIsRetryableAndThirdCollectionCompletesTripOnce()
        {var r=new HuntingRun(3,5);for(int i=0;i<3;i++){r.Track();r.FoundCarcass(true);r.BeginHarvest(0,true);Assert.False(r.Commit(5000,true,(_,_)=>false));Assert.True(r.Commit(5000,true,(n,last)=>{Assert.Equal(i+1,n);Assert.Equal(i==2,last);return true;}));}Assert.Equal(HuntingStage.Finished,r.Stage);Assert.False(r.Commit(6000,true,(_,_)=>throw new Exception()));}
        [Theory] [InlineData(HuntingStage.Ready)] [InlineData(HuntingStage.Tracking)] [InlineData(HuntingStage.Carcass)] [InlineData(HuntingStage.Harvesting)]
        public void CancelStopsCollectionAtEveryActiveStage(HuntingStage stage)
        {var r=new HuntingRun(3,5);if(stage>=HuntingStage.Tracking)r.Track();if(stage>=HuntingStage.Carcass)r.FoundCarcass(true);if(stage>=HuntingStage.Harvesting)r.BeginHarvest(0,true);r.Cancel();Assert.False(r.Commit(10000,true,(_,_)=>throw new Exception()));Assert.False(r.Track());Assert.Equal(0,r.Collected);}
        [Fact] public void LostTrackedAnimalOrCarcassReleasesNextBaitWithoutFakeCollection()
        {var r=new HuntingRun(3,5);r.Track();r.LostAnimal();Assert.Equal(HuntingStage.Ready,r.Stage);r.Track();r.FoundCarcass(true);r.LostAnimal();Assert.Equal(HuntingStage.Ready,r.Stage);Assert.Equal(0,r.Collected);}
        [Fact] public void HarvestClockHandlesOverflow()
        {var r=new HuntingRun(3,5);r.Track();r.FoundCarcass(true);r.BeginHarvest(int.MaxValue-2500,true);Assert.True(r.Commit(unchecked(int.MaxValue+2500),true,(_,_)=>true));}
        [Fact] public void DefaultsContainCompleteMaterialCatalogAndReferenceButcher()
        {var c=HuntingDefaults.Load();Assert.True(c.Valid);Assert.Equal(8,c.Materials.Count);Assert.Equal(-42.14478f,c.Vendor.X);Assert.Equal(-1474.272f,c.Vendor.Y);Assert.Equal(3,c.AnimalsPerTrip);}
        [Fact] public void NineActivitiesShareExclusiveLease()
        {var g=new ShiftGate();foreach(var n in new[]{"pizza","postop","uber","valet","supply","tanker","prison","pilot","hunting"}){Assert.True(g.Acquire(n));Assert.False(g.Acquire("other"));g.Release(n);}}
    }
}
