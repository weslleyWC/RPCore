using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Xunit;

namespace RPCore.Tests
{
    public class CraftingTests
    {
        static CraftingOptions Options() => CraftingDefaults.Load();
        static WalletData Materials() => new WalletData { Bank=2000,Cash=100,Hunting=new HuntingData { Licensed=true,Bait=3,Bag={ ["deer_meat"]=20,["boar_meat"]=10,["hide_rough"]=15,["hide_good"]=10,["hide_perfect"]=5 } },Fishing=new FishingData { RodOwned=true,Bait=3,Bag={ ["sardine"]=10,["mackerel"]=10,["seabass"]=5 } } };
        static bool Craft(WalletData w,string recipe,string receipt="craft",int count=1) => CraftingTransactions.Craft(w,Options(),recipe,count,receipt,()=>{},out _,out _);
        [Fact] public void DefaultsAreCompleteAndValid()
        {var c=Options();Assert.True(c.Valid);Assert.Equal(9,c.Recipes.Count);Assert.Equal(7,c.Products.Count);Assert.Equal(25,c.Capacity);}
        [Theory] [InlineData("dry_venison","venison_jerky")] [InlineData("dry_boar","boar_jerky")] [InlineData("smoke_sardine","smoked_sardine")] [InlineData("smoke_mackerel","smoked_mackerel")] [InlineData("smoke_seabass","smoked_seabass")] [InlineData("tan_rough","leather")] [InlineData("tan_good","leather")] [InlineData("tan_perfect","leather")]
        public void EachRawRecipeConsumesExactIngredientsAndProducesOne(string recipe,string product)
        {var w=Materials();var c=Options();var i=c.Recipes.Find(r=>r.Id==recipe).Ingredients[0];int had=CraftingTransactions.Available(w,i);Assert.True(Craft(w,recipe));Assert.Equal(had-i.Quantity,CraftingTransactions.Available(w,i));Assert.Equal(1,w.Crafting.Stock[product]);Assert.Equal(1,w.Crafting.Batches);Assert.Equal(1,w.Crafting.Made);Assert.Equal(2000,w.Bank);Assert.Equal(100,w.Cash);}
        [Fact] public void PreviewDoesNotAssignOrConsumeAnything()
        {var w=Materials();string old=JsonConvert.SerializeObject(w);Assert.True(CraftingTransactions.Preview(w,Options(),"dry_venison",5,out _));Assert.Equal(old,JsonConvert.SerializeObject(w));Assert.Null(w.Crafting);}
        [Fact] public void MissingIngredientsDoNotRemoveOtherAvailableIngredients()
        {var w=Materials();w.Crafting=new CraftingData { Stock={ ["venison_jerky"]=1,["smoked_sardine"]=1 } };string old=JsonConvert.SerializeObject(w);Assert.False(Craft(w,"field_pack"));Assert.Equal(old,JsonConvert.SerializeObject(w));}
        [Fact] public void EmptyLegacyWalletCannotGenerateFreeProducts()
        {var w=new WalletData{Bank=2000};Assert.False(Craft(w,"dry_venison"));Assert.Null(w.Crafting);Assert.Null(w.Hunting);Assert.Null(w.Fishing);}
        [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(6)] [InlineData(int.MaxValue)]
        public void InvalidBatchSizeDoesNotMutate(int count)
        {var w=Materials();string old=JsonConvert.SerializeObject(w);Assert.False(Craft(w,"dry_venison",count:count));Assert.Equal(old,JsonConvert.SerializeObject(w));}
        [Fact] public void FiveLotsConsumeAndProduceAllAtOnce()
        {var w=Materials();int checkpoints=0;Assert.True(CraftingTransactions.Craft(w,Options(),"dry_venison",5,"b",()=>{checkpoints++;Assert.Equal(10,w.Hunting.Bag["deer_meat"]);Assert.Equal(5,w.Crafting.Stock["venison_jerky"]);},out _,out _));Assert.Equal(1,checkpoints);Assert.Equal(5,w.Crafting.Batches);}
        [Fact] public void FailedCraftSaveRestoresAllReferencesIncludingNullStock()
        {var w=Materials();var h=w.Hunting;var f=w.Fishing;string old=JsonConvert.SerializeObject(w);Assert.Throws<InvalidOperationException>(()=>CraftingTransactions.Craft(w,Options(),"dry_venison",1,"c",()=>throw new InvalidOperationException(),out _,out _));Assert.Null(w.Crafting);Assert.Same(h,w.Hunting);Assert.Same(f,w.Fishing);Assert.Equal(old,JsonConvert.SerializeObject(w));Assert.True(Craft(w,"dry_venison","c"));Assert.Equal(18,w.Hunting.Bag["deer_meat"]);}
        [Fact] public void MultipleSourceIngredientsCommitAndRollbackTogether()
        {
            var c=Options();var r=c.Recipes.Find(x=>x.Id=="dry_venison");r.Ingredients.Add(new CraftIngredient{Source="fishing",Id="sardine",Name="Sardinha",Quantity=2});var w=Materials();string old=JsonConvert.SerializeObject(w);
            Assert.Throws<Exception>(()=>CraftingTransactions.Craft(w,c,r.Id,1,"mix",()=>{Assert.Equal(18,w.Hunting.Bag["deer_meat"]);Assert.Equal(8,w.Fishing.Bag["sardine"]);Assert.Equal(1,w.Crafting.Stock["venison_jerky"]);throw new Exception();},out _,out _));Assert.Equal(old,JsonConvert.SerializeObject(w));
            Assert.True(CraftingTransactions.Craft(w,c,r.Id,1,"mix",()=>{},out _,out _));Assert.Equal(18,w.Hunting.Bag["deer_meat"]);Assert.Equal(8,w.Fishing.Bag["sardine"]);
        }
        [Fact] public void ReplayAfterReloadDoesNotConsumeTwiceEvenWithoutRemainingIngredients()
        {var w=Materials();w.Hunting.Bag["deer_meat"]=2;Assert.True(Craft(w,"dry_venison","c"));w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(CraftingTransactions.Craft(w,Options(),"dry_venison",1,"c",()=>throw new Exception(),out bool changed,out _));Assert.False(changed);Assert.False(w.Hunting.Bag.ContainsKey("deer_meat"));Assert.Equal(1,w.Crafting.Stock["venison_jerky"]);}
        [Fact] public void FullStockRejectsProductAndKeepsAllInputs()
        {var w=Materials();w.Crafting=new CraftingData{Stock={ ["field_pack"]=20 } };string old=JsonConvert.SerializeObject(w);Assert.False(Craft(w,"dry_venison",count:5));Assert.Equal(old,JsonConvert.SerializeObject(w));}
        [Fact] public void ChainedRecipeFreesInputWeightBeforeCapacityCheck()
        {var w=Materials();Assert.True(Craft(w,"dry_venison","d"));Assert.True(Craft(w,"smoke_sardine","f"));Assert.True(Craft(w,"tan_perfect","l"));Assert.True(Craft(w,"field_pack","pack"));Assert.Single(w.Crafting.Stock);Assert.Equal(1,w.Crafting.Stock["field_pack"]);Assert.Equal(4,w.Crafting.Made);Assert.Equal(400,CraftingTransactions.Quote(w.Crafting,Options()));}
        [Fact] public void FailedChainedSaveRestoresInputProductsAndDoesNotCreateKit()
        {var w=Materials();w.Crafting=new CraftingData{Stock={ ["venison_jerky"]=1,["smoked_sardine"]=1,["leather"]=1 } };string old=JsonConvert.SerializeObject(w);Assert.Throws<Exception>(()=>CraftingTransactions.Craft(w,Options(),"field_pack",1,"p",()=>throw new Exception(),out _,out _));Assert.Equal(old,JsonConvert.SerializeObject(w));}
        [Fact] public void SaleCommitsStockAndBankTogetherAndCannotPayTwice()
        {var w=Materials();Craft(w,"dry_venison");int calls=0;Assert.True(CraftingTransactions.Sell(w,Options(),"sale",()=>{calls++;Assert.Empty(w.Crafting.Stock);Assert.Equal(2125,w.Bank);},out bool changed));Assert.True(changed);Assert.Equal(1,calls);Assert.Equal(125,w.Crafting.Earned);Assert.Equal(1,w.Crafting.Sales);Assert.True(CraftingTransactions.Sell(w,Options(),"sale",()=>throw new Exception(),out changed));Assert.False(changed);Assert.False(CraftingTransactions.Sell(w,Options(),"new",()=>throw new Exception(),out _));Assert.Equal(2125,w.Bank);}
        [Fact] public void SaleFailureRestoresStockReferenceAndBank()
        {var w=Materials();Craft(w,"smoke_sardine");var stock=w.Crafting;string old=JsonConvert.SerializeObject(w);Assert.Throws<Exception>(()=>CraftingTransactions.Sell(w,Options(),"s",()=>throw new Exception(),out _));Assert.Same(stock,w.Crafting);Assert.Equal(old,JsonConvert.SerializeObject(w));}
        [Fact] public void BankAndEarningsLimitsCannotLoseGoods()
        {var w=Materials();Craft(w,"dry_venison");w.Bank=int.MaxValue;Assert.False(CraftingTransactions.Sell(w,Options(),"s",()=>throw new Exception(),out _));Assert.Equal(1,w.Crafting.Stock["venison_jerky"]);w.Bank=100;w.Crafting.Earned=long.MaxValue;Assert.False(CraftingTransactions.Sell(w,Options(),"s",()=>{},out _));Assert.Equal(100,w.Bank);}
        [Fact] public void InvalidSourceDataIsPreservedRatherThanReset()
        {var w=Materials();w.Hunting.Bag["deer_meat"]=-1;string old=JsonConvert.SerializeObject(w);Assert.False(Craft(w,"dry_venison"));Assert.Equal(old,JsonConvert.SerializeObject(w));}
        [Fact] public void UnrelatedInvalidBagDoesNotBlockOtherRecipeOrGetModified()
        {var w=Materials();w.Fishing.Bait=-1;var old=w.Fishing;Assert.True(Craft(w,"dry_venison"));Assert.Same(old,w.Fishing);Assert.Equal(-1,w.Fishing.Bait);}
        [Fact] public void UnknownStockMaterialsAndOldFieldsSurviveCraftAndSale()
        {
            var w=Materials();w.Extra=new Dictionary<string,JToken>{{"Future",42}};w.Hunting.Extra=new Dictionary<string,JToken>{{"HuntFuture",true}};w.Fishing.Extra=new Dictionary<string,JToken>{{"FishFuture",true}};w.Hunting.Bag["mod_meat"]=3;
            w.Crafting=new CraftingData{Stock={ ["mod_product"]=2 }};w.Crafting.Extra=new Dictionary<string,JToken>{{"CraftFuture","keep"}};string fishing=JsonConvert.SerializeObject(w.Fishing);
            Assert.True(Craft(w,"dry_venison"));Assert.True(CraftingTransactions.Sell(w,Options(),"s",()=>{},out _));w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.Equal(2,w.Crafting.Stock["mod_product"]);Assert.Equal("keep",(string)w.Crafting.Extra["CraftFuture"]);Assert.Equal(42,(int)w.Extra["Future"]);Assert.True((bool)w.Hunting.Extra["HuntFuture"]);Assert.Equal(3,w.Hunting.Bag["mod_meat"]);Assert.Equal(fishing,JsonConvert.SerializeObject(w.Fishing));Assert.True(w.Hunting.Licensed);Assert.Equal(3,w.Hunting.Bait);
        }
        [Fact] public void JobsAndPilotLicenseAreNotChanged()
        {var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":1000,\"Jobs\":{\"Jobs\":{\"pilot_training\":{\"CompletedShifts\":1},\"postop\":{\"Deliveries\":20}}},\"Fishing\":{\"RodOwned\":true,\"Bag\":{\"sardine\":2}}}");string old=JsonConvert.SerializeObject(w.Jobs);Assert.True(Craft(w,"smoke_sardine"));Assert.Equal(old,JsonConvert.SerializeObject(w.Jobs));Assert.True(FlightRules.Qualified(w.Jobs));Assert.True(w.Fishing.RodOwned);}
        [Fact] public void InvalidConfigDuplicateIngredientsCannotConsumePartialInputs()
        {var c=Options();c.Recipes[0].Ingredients.Add(c.Recipes[0].Ingredients[0]);Assert.False(c.Valid);var w=Materials();string old=JsonConvert.SerializeObject(w);Assert.False(CraftingTransactions.Craft(w,c,"dry_venison",1,"c",()=>throw new Exception(),out _,out _));Assert.Equal(old,JsonConvert.SerializeObject(w));c=Options();c.Products[0].Weight=float.NaN;Assert.False(c.Valid);c=Options();c.Recipes[0].Product="missing";Assert.False(c.Valid);}
        [Fact] public void CountersAndStockCannotOverflow()
        {var w=Materials();w.Crafting=new CraftingData{Made=int.MaxValue};Assert.False(Craft(w,"dry_venison"));Assert.Equal(20,w.Hunting.Bag["deer_meat"]);w.Crafting=new CraftingData{Batches=int.MaxValue};Assert.False(Craft(w,"dry_venison"));}
        [Fact] public void AvailabilityForEmptyLegacyBagsIsZero()
        {var w=new WalletData();foreach(string source in new[]{"hunting","fishing","crafting"})Assert.Equal(0,CraftingTransactions.Available(w,new CraftIngredient{Source=source,Id="missing"}));}
        [Fact] public void ReceiptHistoryIsBounded()
        {var w=Materials();w.Hunting.Bag["deer_meat"]=1200;for(int n=0;n<600;n++){Assert.True(Craft(w,"dry_venison","c"+n));Assert.True(CraftingTransactions.Sell(w,Options(),"s"+n,()=>{},out _));}Assert.Equal(512,w.Crafting.Receipts.Count);Assert.Equal(600,w.Crafting.Made);}
        [Fact] public void TimerRequiresFullDurationAndEligibilityBeforeCommit()
        {var r=new CraftingRun("dry_venison",1,6,0);Assert.Equal(6,r.RemainingSeconds(0));Assert.False(r.Commit(5999,true,()=>throw new Exception()));Assert.Equal(1,r.RemainingSeconds(5999));Assert.True(r.Commit(6000,true,()=>true));Assert.Equal(CraftingStage.Finished,r.Stage);Assert.False(r.Commit(6001,true,()=>throw new Exception()));}
        [Fact] public void FiveLotsTakeFiveTimesTheRecipeDuration()
        {var r=new CraftingRun("dry_venison",5,6,0);Assert.False(r.Commit(6000,true,()=>throw new Exception()));Assert.True(r.Commit(30000,true,()=>true));}
        [Fact] public void CancelOrLostEligibilityCannotConsumeIngredients()
        {var r=new CraftingRun("dry_venison",1,6,0);Assert.False(r.Commit(6000,false,()=>throw new Exception()));Assert.Equal(CraftingStage.Cancelled,r.Stage);Assert.False(r.Commit(7000,true,()=>throw new Exception()));}
        [Fact] public void FailedOrThrowingCheckpointRetainsSameOperationForRetry()
        {var r=new CraftingRun("dry_venison",1,6,0);string id=r.Receipt;Assert.False(r.Commit(6000,true,()=>false));Assert.Throws<Exception>(()=>r.Commit(7000,true,()=>throw new Exception()));Assert.Equal(id,r.Receipt);Assert.Equal(CraftingStage.Working,r.Stage);Assert.True(r.Commit(12000,true,()=>true));}
        [Fact] public void TimerHandlesClockOverflow()
        {int start=int.MaxValue-3000;var r=new CraftingRun("dry_venison",1,6,start);Assert.True(r.Commit(unchecked(start+6000),true,()=>true));Assert.Equal(0,r.RemainingSeconds(unchecked(start+6000)));}
        [Fact] public void CancellationAfterCommitCannotReverseConfirmedProduct()
        {var r=new CraftingRun("dry_venison",1,6,0);r.Commit(6000,true,()=>true);r.Cancel();Assert.Equal(CraftingStage.Finished,r.Stage);}
        [Fact] public void ElevenActivitiesShareExclusiveLease()
        {var g=new ShiftGate();foreach(var n in new[]{"pizza","postop","uber","valet","supply","tanker","prison","pilot","hunting","fishing","crafting"}){Assert.True(g.Acquire(n));Assert.False(g.Acquire("other"));g.Release(n);}}
    }
}
