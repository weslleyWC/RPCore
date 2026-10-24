using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class HomeStorageTests
    {
        static InvData Data() => new InvData { Items=new Dictionary<string,int>{{"water",10},{"burger",2}},Containers=new Dictionary<string,Dictionary<string,int>>{{"trunk:old",new Dictionary<string,int>{{"custom",7}}}},GridSlots=new List<string>{"water","burger"} };
        static float Weight(string id)=>id=="water"?.5f:id=="burger"?.4f:.1f;
        static bool Move(InvData d,int n,bool deposit,float capacity,out InvData next,Action<InvData> save=null) => InventoryTransfers.Move(d,"home:eclipse","water",n,deposit,capacity,30,Weight,save??(_=>{}),out next);
        [Fact] public void DefaultsUseSeparatePrivateContainerWithoutRewritingExistingHousing()
        { var c=HomeStorageDefaults.Load();Assert.True(c.Valid);Assert.Equal("eclipse",c.PropertyId);Assert.Equal("home:eclipse",c.ContainerId);Assert.Equal(200,c.Capacity);Assert.Equal(187.9136f,c.Point.Z); }
        [Fact] public void OpeningEmptyContainerSnapshotDoesNotCreateOrSaveIt()
        { var d=Data();string before=JsonConvert.SerializeObject(d);var box=InventoryTransfers.Snapshot(d,"home:eclipse");Assert.Empty(box);box["water"]=99;Assert.Equal(before,JsonConvert.SerializeObject(d)); }
        [Fact] public void DepositSavesBothSidesTogetherAndOnlyPublishesCandidateAfterSave()
        { var d=Data();InvData saved=null;Assert.True(Move(d,5,true,200,out var next,c=>{saved=c;Assert.Equal(5,c.Items["water"]);Assert.Equal(5,c.Containers["home:eclipse"]["water"]);Assert.Equal(10,d.Items["water"]);}));Assert.Same(saved,next);Assert.NotSame(d,next);Assert.Equal(10,d.Items["water"]);Assert.False(d.Containers.ContainsKey("home:eclipse")); }
        [Fact] public void FailedDepositDoesNotMutateLiveInventoryOrCreateContainer()
        { var d=Data();string before=JsonConvert.SerializeObject(d);Assert.Throws<Exception>(()=>Move(d,5,true,200,out _,_=>throw new Exception()));Assert.Equal(before,JsonConvert.SerializeObject(d)); }
        [Fact] public void FailedWithdrawalLeavesBothSidesAndGridExactlyOriginal()
        { var d=Data();Move(d,10,true,200,out d);string before=JsonConvert.SerializeObject(d);Assert.Throws<Exception>(()=>Move(d,5,false,200,out _,_=>throw new Exception()));Assert.Equal(before,JsonConvert.SerializeObject(d)); }
        [Fact] public void WholeStackRoundTripConservesTotalAndSurvivesReload()
        { var d=Data();Assert.True(Move(d,10,true,200,out d));Assert.False(d.Items.ContainsKey("water"));d=JsonConvert.DeserializeObject<InvData>(JsonConvert.SerializeObject(d));Assert.True(Move(d,10,false,200,out d));Assert.Equal(10,d.Items["water"]);Assert.False(d.Containers["home:eclipse"].ContainsKey("water"));Assert.Contains("water",d.GridSlots);Assert.Equal(7,d.Containers["trunk:old"]["custom"]); }
        [Theory] [InlineData(1)] [InlineData(5)] [InlineData(10)]
        public void QuantityOptionsMoveExactlyRequestedNumber(int quantity)
        { var d=Data();Assert.True(Move(d,quantity,true,200,out var next));Assert.Equal(quantity,next.Containers["home:eclipse"]["water"]);Assert.Equal(10-quantity,next.Items.TryGetValue("water",out int count)?count:0); }
        [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(11)]
        public void InvalidOrStaleQuantityNeverSavesOrPartiallyMoves(int quantity)
        { var d=Data();string before=JsonConvert.SerializeObject(d);Assert.False(Move(d,quantity,true,200,out var next,_=>throw new Exception()));Assert.Same(d,next);Assert.Equal(before,JsonConvert.SerializeObject(d)); }
        [Fact] public void FullContainerRejectsStackWithoutPartialTransferAndAcceptsExactFit()
        { var d=Data();Assert.False(Move(d,10,true,4.9f,out var next,_=>throw new Exception()));Assert.Same(d,next);Assert.True(Move(d,10,true,5,out next));Assert.Equal(10,next.Containers["home:eclipse"]["water"]); }
        [Fact] public void WithdrawalChecksBackpackWeightIncludingOtherItems()
        { var d=Data();Move(d,10,true,200,out d);Assert.False(InventoryTransfers.Move(d,"home:eclipse","water",10,false,200,5,Weight,_=>throw new Exception(),out var next));Assert.Same(d,next);Assert.True(InventoryTransfers.Move(d,"home:eclipse","water",5,false,200,5,Weight,_=>{},out next));Assert.Equal(5,next.Items["water"]); }
        [Fact] public void ExistingOverweightContainerCanWithdrawButCannotAcceptMore()
        { var d=Data();Move(d,5,true,200,out d);Assert.True(Move(d,1,false,1,out var next));Assert.Equal(4,next.Containers["home:eclipse"]["water"]);Assert.False(Move(d,1,true,1,out _)); }
        [Fact] public void UnknownItemsRemainTransferableWithExistingFallbackWeight()
        { var d=Data();d.Items["unknown_mod"]=7;Assert.True(InventoryTransfers.Move(d,"home:eclipse","unknown_mod",7,true,200,30,Weight,_=>{},out d));Assert.Equal(7,d.Containers["home:eclipse"]["unknown_mod"]);Assert.True(InventoryTransfers.Move(d,"home:eclipse","unknown_mod",7,false,200,30,Weight,_=>{},out d));Assert.Equal(7,d.Items["unknown_mod"]); }
        [Fact] public void UnknownRootFieldsOtherContainersAndLayoutPositionsAreRetained()
        { var d=Data();var j=JObject.Parse(JsonConvert.SerializeObject(d));j["Future"]=JObject.Parse("{\"retain\":true}");d=j.ToObject<InvData>();Move(d,1,true,200,out var next);var result=JObject.Parse(JsonConvert.SerializeObject(next));Assert.True((bool)result["Future"]["retain"]);Assert.True(JToken.DeepEquals(j["Containers"]["trunk:old"],result["Containers"]["trunk:old"]));Assert.Equal("water",next.GridSlots[0]);Assert.Equal("burger",next.GridSlots[1]); }
        [Fact] public void EntireStackRemovalUpdatesGridWithoutChangingOtherItems()
        { var d=Data();Move(d,10,true,200,out var next);Assert.DoesNotContain("water",next.GridSlots);Assert.Contains("burger",next.GridSlots);Assert.Equal(2,next.Items["burger"]);Assert.Equal(25,next.GridSlots.Count); }
        [Theory] [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)] [InlineData(-1)]
        public void InvalidWeightOrCapacityNeverMutates(float value)
        { var d=Data();Assert.False(Move(d,1,true,value,out _,_=>throw new Exception()));Assert.False(InventoryTransfers.Move(d,"home:eclipse","water",1,true,200,30,_=>value,_=>throw new Exception(),out _));Assert.Equal(10,d.Items["water"]); }
        [Fact] public void WeightLookupExceptionCannotPublishModifiedCounts()
        { var d=Data();string before=JsonConvert.SerializeObject(d);Assert.Throws<Exception>(()=>InventoryTransfers.Move(d,"home:eclipse","water",1,true,200,30,_=>throw new Exception(),_=>{},out _));Assert.Equal(before,JsonConvert.SerializeObject(d)); }
        [Fact] public void CountOverflowRefusesEvenZeroWeightItems()
        { var d=Data();d.Containers["home:eclipse"]=new Dictionary<string,int>{{"water",int.MaxValue}};Assert.False(InventoryTransfers.Move(d,"home:eclipse","water",1,true,200,30,_=>0,_=>throw new Exception(),out _));Assert.Equal(int.MaxValue,d.Containers["home:eclipse"]["water"]);Assert.Equal(10,d.Items["water"]); }
        [Fact] public void NegativeCountsAreNotSilentlyRepairedOrSaved()
        { var d=Data();d.Items["custom"]=-1;string before=JsonConvert.SerializeObject(d);Assert.False(Move(d,1,true,200,out _,_=>throw new Exception()));Assert.Equal(before,JsonConvert.SerializeObject(d)); }
        [Fact] public void SeparateProfilesKeepSeparateContainersAndQuantities()
        { var a=Data();var b=Data();Move(a,5,true,200,out a);Assert.Equal(5,a.Containers["home:eclipse"]["water"]);Assert.False(b.Containers.ContainsKey("home:eclipse"));Assert.Equal(10,b.Items["water"]); }
        [Fact] public void LegacyTrunkAndStashTransfersUseSameConservingSavePath()
        { var d=Data();Assert.True(InventoryTransfers.Move(d,"stash:franklin_house","water",1,true,200,30,Weight,_=>{},out d));Assert.Equal(1,d.Containers["stash:franklin_house"]["water"]);Assert.True(InventoryTransfers.Move(d,"trunk:old","water",1,true,100,30,Weight,_=>{},out d));Assert.Equal(1,d.Containers["trunk:old"]["water"]);Assert.Equal(8,d.Items["water"]);Assert.Equal(7,d.Containers["trunk:old"]["custom"]); }
        [Fact] public void NullTargetFromLegacyFileIsOnlyInitializedOnSuccessfulTransfer()
        { var d=Data();d.Containers["home:eclipse"]=null;Assert.False(Move(d,1,false,200,out _));Assert.Null(d.Containers["home:eclipse"]);Assert.True(Move(d,1,true,200,out var next));Assert.Equal(1,next.Containers["home:eclipse"]["water"]); }
        [Fact] public void PrivateConfigRejectsTrunkIdsBadCapacityAndMissingPoint()
        { var c=HomeStorageDefaults.Load();c.ContainerId="trunk:old";Assert.False(c.Valid);c=HomeStorageDefaults.Load();c.Capacity=float.NaN;Assert.False(c.Valid);c=HomeStorageDefaults.Load();c.Point=null;Assert.False(c.Valid); }
        [Theory] [InlineData("")] [InlineData(" ")]
        public void EmptyContainerOrItemIdsCannotChangeInventory(string id)
        { var d=Data();Assert.False(InventoryTransfers.Move(d,id,"water",1,true,200,30,Weight,_=>throw new Exception(),out _));Assert.False(InventoryTransfers.Move(d,"home:eclipse",id,1,true,200,30,Weight,_=>throw new Exception(),out _));Assert.Equal(10,d.Items["water"]); }
    }
}
