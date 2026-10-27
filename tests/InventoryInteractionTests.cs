using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace RPCore.Tests
{
    public class InventoryInteractionTests
    {
        static InvData Data()=>new InvData{Items=new Dictionary<string,int>{{"water",10},{"psrp_shaker",2},{"psrp_proteinstrawberry",2}},GridSlots=new List<string>{null,"water",null,"psrp_shaker",null,"psrp_proteinstrawberry"},Extra=new Dictionary<string,JToken>{{"Future",true}}};
        static InventoryRecipe Recipe()=>new InventoryRecipe{Id="shake",Result="psrp_shakerstrawberry",Ingredients=new Dictionary<string,int>{{"water",1},{"psrp_shaker",1},{"psrp_proteinstrawberry",1}}};
        [Theory][InlineData(49,0)][InlineData(50,1)][InlineData(51,2)][InlineData(52,3)][InlineData(53,4)][InlineData(54,5)]
        public void TopRowKeysMapExactlyToFirstSixSlots(int key,int slot)=>Assert.Equal(slot,InventoryShortcuts.Slot(key));
        [Theory][InlineData(48)][InlineData(55)][InlineData(97)][InlineData(98)][InlineData(99)][InlineData(100)][InlineData(101)][InlineData(102)]
        public void NumpadAndOtherDigitsAreNotInventoryShortcuts(int key)=>Assert.Equal(-1,InventoryShortcuts.Slot(key));
        [Theory][InlineData(49)][InlineData(50)][InlineData(51)][InlineData(52)][InlineData(53)][InlineData(54)]
        public void ModifierDoesNotActivateAQuickSlot(int key)=>Assert.Equal(-1,InventoryShortcuts.Slot(key,true));
        [Fact]public void EmptySlotDoesNotFallThroughToNextItem(){var d=Data();Assert.Null(InventoryShortcuts.Item(d,0));Assert.Equal("water",InventoryShortcuts.Item(d,1));Assert.Null(InventoryShortcuts.Item(d,2));}
        [Fact]public void MovingItemChangesShortcutWithoutDuplicatingCounts(){var d=Data();InventoryEdits.Move(ref d,0,1,n=>{});Assert.Equal("water",InventoryShortcuts.Item(d,0));Assert.Null(InventoryShortcuts.Item(d,1));Assert.Equal(10,d.Items["water"]);}
        [Fact]public void ShortcutNeverUsesAnotherPage(){var d=Data();d.GridSlots=Enumerable.Repeat<string>(null,48).ToList();d.GridSlots[24]="water";Assert.Null(InventoryShortcuts.Item(d,0));Assert.Null(InventoryShortcuts.Item(d,24));}
        [Fact]public void ExhaustedStackHasNoShortcut(){var d=Data();d.Items["water"]=0;Assert.Null(InventoryShortcuts.Item(d,1));}
        [Fact]public void DownFromFourthRowScrollsExactlyOneRow(){var n=new InventoryNavigation();n.Select(18,96);Assert.Equal(0,n.FirstRow);n.Move(0,1,96);Assert.Equal(24,n.Selected);Assert.Equal(1,n.FirstRow);}
        [Fact]public void HorizontalMovementDoesNotWrapRows(){var n=new InventoryNavigation();n.Select(5,96);n.Move(1,0,96);Assert.Equal(5,n.Selected);n.Select(6,96);n.Move(-1,0,96);Assert.Equal(6,n.Selected);}
        [Fact]public void ShrinkingAndEmptyLayoutsKeepNavigationInBounds(){var n=new InventoryNavigation();n.Select(95,96);n.Select(n.Selected,24);Assert.Equal(23,n.Selected);Assert.Equal(0,n.FirstRow);n.Select(100,0);Assert.Equal(0,n.Selected);Assert.Equal(0,n.FirstRow);}
        [Fact]public void DownAtBottomRetainsColumn(){var n=new InventoryNavigation();n.Select(90,96);n.Move(0,1,96);Assert.Equal(90,n.Selected);}
        [Fact]public void RecipeSelectionOnlyMatchesExactIngredients(){var r=Recipe();Assert.True(PersonalCrafting.Matches(r,new[]{"water","psrp_shaker","psrp_proteinstrawberry"}));Assert.False(PersonalCrafting.Matches(r,new[]{"water","water","psrp_shaker"}));Assert.False(PersonalCrafting.Matches(r,new[]{"water","psrp_shaker"}));}
        [Fact]public void CraftCommitsAllIngredientsAndResultTogether(){var d=Data();var r=Recipe();Assert.Equal(2,PersonalCrafting.Maximum(d,r));Assert.True(PersonalCrafting.Craft(ref d,r,2,100,_=>.1f,n=>{}));Assert.Equal(8,d.Items["water"]);Assert.False(d.Items.ContainsKey("psrp_shaker"));Assert.Equal(2,d.Items[r.Result]);Assert.True((bool)d.Extra["Future"]);}
        [Fact]public void CraftFailurePreservesAllMaterialsAndLayout(){var d=Data();string before=JsonConvert.SerializeObject(d);Assert.Throws<IOException>(()=>PersonalCrafting.Craft(ref d,Recipe(),1,100,_=>.1f,n=>throw new IOException()));Assert.Equal(before,JsonConvert.SerializeObject(d));}
        [Fact]public void ReusableToolsAreRequiredButNotConsumedPerBatch(){var d=Data();d.Items["grinder"]=1;var r=Recipe();r.Ingredients=new Dictionary<string,int>{{"water",1},{"grinder",1}};r.Reusable.Add("grinder");Assert.Equal(10,PersonalCrafting.Maximum(d,r));Assert.True(PersonalCrafting.Craft(ref d,r,5,100,_=>.1f,n=>{}));Assert.Equal(1,d.Items["grinder"]);Assert.Equal(5,d.Items[r.Result]);}
        [Fact]public void ReusableOnlyRecipeCannotMintFreeItems(){var r=Recipe();r.Reusable=new HashSet<string>(r.Ingredients.Keys);Assert.False(r.Valid);Assert.Equal(0,PersonalCrafting.Maximum(Data(),r));}
        [Theory][InlineData(0)][InlineData(-1)][InlineData(3)][InlineData(101)]public void InvalidBatchDoesNotPartiallyConsume(int count){var d=Data();Assert.False(PersonalCrafting.Craft(ref d,Recipe(),count,100,_=>.1f,n=>throw new Exception()));Assert.Equal(10,d.Items["water"]);}
        [Fact]public void OverweightResultIsRefusedBeforeWriting(){var d=Data();var r=Recipe();Assert.False(PersonalCrafting.Craft(ref d,r,1,5,id=>id==r.Result?100:.1f,n=>throw new Exception()));Assert.Equal(2,d.Items["psrp_shaker"]);}
        [Fact]public void WashingCanReduceAnAlreadyOverweightInventory(){var d=Data();d.Items["psrp_shakerdirty"]=1;var r=new InventoryRecipe{Id="wash",Result="psrp_shaker",Ingredients=new Dictionary<string,int>{{"psrp_shakerdirty",1},{"water",1}}};Assert.True(PersonalCrafting.Craft(ref d,r,1,.1f,_=>1,n=>{}));Assert.Equal(3,d.Items["psrp_shaker"]);}
        [Fact]public void EmbeddedRecipesHaveKnownResultsAndIngredients(){var recipes=InventoryReference.Load<List<InventoryRecipe>>("Recipes");var items=InventoryReference.Load<List<ItemDef>>("Items");var ids=new HashSet<string>(items.Select(i=>i.Id)){"water","milk"};Assert.Equal(9,recipes.Count);Assert.All(recipes,r=>{Assert.True(r.Valid);Assert.Contains(r.Result,ids);Assert.All(r.Ingredients.Keys,id=>Assert.Contains(id,ids));});}
        [Fact]public void ReturningContainerIsPartOfSameConsumptionCandidate(){var d=Data();var item=new ItemDef{Id="water",ReturnItem="bottle"};var receipt=NeedsRules.Food(new NeedsData(),"one",0,20);var n=NeedsRules.Consume(d,item,receipt);Assert.Equal(9,n.Items["water"]);Assert.Equal(1,n.Items["bottle"]);Assert.Equal(10,d.Items["water"]);Assert.False(d.Items.ContainsKey("bottle"));}
        [Fact]public void ContainerOverflowCannotConsumeTheDrink(){var d=Data();d.Items["bottle"]=int.MaxValue;Assert.Null(NeedsRules.Consume(d,new ItemDef{Id="water",ReturnItem="bottle"},NeedsRules.Food(new NeedsData(),"one",0,20)));Assert.Equal(10,d.Items["water"]);}
        [Fact]public void EnergyAddsToCurrentSleepOnceWithoutUndoingRest(){var d=new NeedsData{Sleep=40};var r=NeedsRules.Food(d,"coffee",0,20,12);d.Sleep=80;Assert.True(NeedsRules.Apply(ref d,r,n=>{}));Assert.Equal(92,d.Sleep);Assert.True(NeedsRules.Apply(ref d,r,n=>{}));Assert.Equal(92,d.Sleep);}
        [Fact]public void EnergyRecoveryFailureIsRetryableAndDoesNotAlterSleep(){var d=new NeedsData{Sleep=40};var r=NeedsRules.Food(d,"coffee",0,20,12);Assert.Throws<IOException>(()=>NeedsRules.Apply(ref d,r,n=>throw new IOException()));Assert.Equal(40,d.Sleep);Assert.True(NeedsRules.Apply(ref d,r,n=>{}));Assert.Equal(52,d.Sleep);}
        [Fact]public void OldReceiptCannotResetRecoveredSleep(){var d=new NeedsData{Sleep=100};var old=JsonConvert.DeserializeObject<NeedsConsumption>("{\"Id\":\"old\",\"Hunger\":80,\"Thirst\":80,\"Sleep\":0}");Assert.True(NeedsRules.Apply(ref d,old,n=>{}));Assert.Equal(100,d.Sleep);}
        [Fact]public void StorageRoundTripKeepsAHighSavedSlotAndSixColumnPadding(){var d=Data();d.GridSlots=Enumerable.Repeat<string>(null,120).ToList();d.GridSlots[113]="water";Assert.True(InventoryTransfers.Move(d,"home:test","psrp_shaker",1,true,200,200,_=>.1f,n=>{},out d));Assert.Equal("water",d.GridSlots[113]);Assert.Equal(0,d.GridSlots.Count%24);}
        [Fact]public void FailedStorageWriteKeepsHotbarAndArmoryMetadata(){var d=Data();d.Armory=new InventoryArmory();d.Armory.Extra=new Dictionary<string,JToken>{{"Keep",true}};string before=JsonConvert.SerializeObject(d);Assert.Throws<IOException>(()=>InventoryTransfers.Move(d,"home:test","water",1,true,200,200,_=>.1f,n=>throw new IOException(),out _));Assert.Equal(before,JsonConvert.SerializeObject(d));}
        [Fact]public void PurchaseIsSavedImmediatelyAndDoesNotGrantBeforeBothWrites(){var d=Data();int cash=100,writes=0;var item=new ItemDef{Id="water",Price=3};Assert.True(InventoryPurchases.Buy(ref d,item,2,100,_=>.1f,()=>cash,c=>{cash-=c;return true;},c=>cash+=c,()=>writes++,n=>{Assert.Equal(10,d.Items["water"]);Assert.Equal(12,n.Items["water"]);}));Assert.Equal(94,cash);Assert.Equal(12,d.Items["water"]);Assert.Equal(1,writes);}
        [Theory][InlineData(true)][InlineData(false)]public void OrdinaryPurchaseWriteFailureRefundsCashAndKeepsItems(bool walletFailure){var d=Data();int cash=100,calls=0;Assert.Throws<IOException>(()=>InventoryPurchases.Buy(ref d,new ItemDef{Id="water",Price=3},1,100,_=>.1f,()=>cash,c=>{cash-=c;return true;},c=>cash+=c,()=>{if(walletFailure && calls++==0)throw new IOException();},n=>throw new IOException()));Assert.Equal(100,cash);Assert.Equal(10,d.Items["water"]);}
        [Fact]public void PurchaseRejectsOverflowAndFullInventoryWithoutDebit(){var d=Data();bool debited=false;Assert.False(InventoryPurchases.Buy(ref d,new ItemDef{Id="water",Price=int.MaxValue},2,100,_=>.1f,()=>int.MaxValue,c=>{debited=true;return true;},c=>{},()=>{},n=>{}));Assert.False(InventoryPurchases.Buy(ref d,new ItemDef{Id="water",Price=3},1,0,_=>.1f,()=>100,c=>{debited=true;return true;},c=>{},()=>{},n=>{}));Assert.False(debited);}
        [Fact]public void C4PresentationUsesTheActualReferenceTexture()=>Assert.Equal("c4",InventoryReference.Weapons["StickyBomb"][1]);
    }
}
