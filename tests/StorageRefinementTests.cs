using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
 public sealed class StorageRefinementTests
 {
  [Fact] public void LegacyTrunkKeyPreservesCaseModelAndTrim(){Assert.Equal("trunk:-123:MiX 123",StorageRules.TrunkKey(-123,"  MiX 123  "));Assert.NotEqual(StorageRules.TrunkKey(1,"abc"),StorageRules.TrunkKey(1,"ABC"));}
  [Theory][InlineData(null)][InlineData("")][InlineData("  ")][InlineData("BAD\nPLATE")][InlineData("abcdefghijklmnopqrstuvwxyz123456789")]
  public void InvalidPlateCannotBindStorage(string plate)=>Assert.Null(StorageRules.TrunkKey(1,plate));
  [Fact] public void ZeroModelHasNoPersistentTrunk()=>Assert.Null(StorageRules.TrunkKey(0,"ABC"));
  [Fact] public void IdentityRejectsActorModelProfileHandleOrPlateChanges(){var p=new StorageIdentity("RP_test",10,20,30,"trunk:40:ABC");Assert.True(p.Matches("RP_test",10,20,30,"trunk:40:ABC"));Assert.False(p.Matches("Other",10,20,30,"trunk:40:ABC"));Assert.False(p.Matches("RP_test",11,20,30,"trunk:40:ABC"));Assert.False(p.Matches("RP_test",10,21,30,"trunk:40:ABC"));Assert.False(p.Matches("RP_test",10,20,31,"trunk:40:ABC"));Assert.False(p.Matches("RP_test",10,20,30,"trunk:40:NEW"));}
  [Fact] public void StashIdentityCannotBeReusedForVehicleOrMissingActor(){var p=new StorageIdentity("Franklin",10,20);Assert.True(p.Matches("Franklin",10,20));Assert.False(p.Matches("Franklin",10,20,5,"trunk:1:ABC"));Assert.False(new StorageIdentity("Franklin",0,20).Matches("Franklin",0,20));}
  [Theory][InlineData(float.NaN)][InlineData(float.PositiveInfinity)][InlineData(float.NegativeInfinity)] public void NonFiniteLocationsCannotCreateWorldBlips(float value){Assert.False(new StorageLocation{Id="test",X=value}.Valid);Assert.False(new StorageLocation{Id="test",Y=value}.Valid);Assert.False(new StorageLocation{Id="test",Z=value}.Valid);Assert.False(new StorageLocation{Id="test",Capacity=value}.Valid);}
  [Fact] public void CustomFiniteCoordinatesAndCapacityRemainValid(){Assert.True(new StorageLocation{Id="my_old_stash",Name="My stash",X=-100,Y=200,Z=50,Capacity=20000}.Valid);Assert.False(new StorageLocation{Id="test",Capacity=0}.Valid);Assert.False(new StorageLocation{Id=" "}.Valid);}
  [Fact] public void QuantitiesDistinctIncludeMaximumFitAndNeverInventItems(){Assert.Equal(new[]{1,5,20,7},StorageRules.Quantities(20,7));Assert.Equal(new[]{1},StorageRules.Quantities(1,100));Assert.Empty(StorageRules.Quantities(0,100));Assert.DoesNotContain(0,StorageRules.Quantities(20,0));}
  static StorageMouseTransfer Gesture(bool storage=false){var g=new StorageMouseTransfer();g.Begin(storage,0,"water",10,new PointF(150,250));g.Move(new PointF(1100,250));return g;}
  [Theory][InlineData(false,false,false,1)][InlineData(false,true,false,10)][InlineData(false,false,true,3)][InlineData(true,false,false,1)][InlineData(true,true,false,10)][InlineData(true,true,true,3)]
  public void BothDirectionsSupportOneStackOrFitExactlyOnce(bool fromStorage,bool stack,bool fit,int expected){var g=Gesture(fromStorage);int writes=0;Assert.True(g.Release(!fromStorage,true,"water",10,3,stack,fit,(id,deposit,n)=>{Assert.Equal("water",id);Assert.Equal(!fromStorage,deposit);Assert.Equal(expected,n);writes++;return true;}));Assert.False(g.Release(!fromStorage,true,"water",10,3,stack,fit,(i,d,n)=>throw new Exception()));Assert.Equal(1,writes);Assert.False(g.Active);}
  [Fact] public void ClickOnlyDoesNotTransfer(){var g=new StorageMouseTransfer();g.Begin(false,0,"water",10,new PointF(150,250));g.Move(new PointF(152,251));Assert.False(g.Release(true,true,"water",10,10,false,false,(i,d,n)=>throw new Exception()));}
  [Theory][InlineData("other",10)][InlineData("water",9)][InlineData(null,10)][InlineData("water",0)] public void ChangedSourceCancelsGesture(string id,int quantity){var g=Gesture();Assert.False(g.Release(true,true,id,quantity,10,false,false,(i,d,n)=>throw new Exception()));Assert.False(g.Active);}
  [Fact] public void OutsideSameSideAndCancelledGesturesNeverCommit(){var g=Gesture();Assert.False(g.Release(true,false,"water",10,10,false,false,(i,d,n)=>throw new Exception()));g=Gesture();Assert.False(g.Release(false,true,"water",10,10,false,false,(i,d,n)=>throw new Exception()));g=Gesture();g.Cancel();Assert.False(g.Release(true,true,"water",10,10,false,false,(i,d,n)=>throw new Exception()));}
  [Theory][InlineData(0)][InlineData(-1)] public void NothingFitsCannotCommit(int fitting){var g=Gesture();Assert.False(g.Release(true,true,"water",10,fitting,false,true,(i,d,n)=>throw new Exception()));}
  [Fact] public void TransferFailureDoesNotRetry(){var g=Gesture();int calls=0;Assert.False(g.Release(true,true,"water",10,10,false,false,(i,d,n)=>{calls++;return false;}));Assert.False(g.Active);Assert.Equal(1,calls);}
  [Fact] public void IOFailureCancelsGestureAndPreservesBothSides(){var data=new InvData{Items=new Dictionary<string,int>{{"water",10}}};string original=JsonConvert.SerializeObject(data);var g=Gesture();Assert.Throws<IOException>(()=>g.Release(true,true,"water",10,10,true,false,(id,deposit,n)=>InventoryTransfers.Move(data,"trunk:1:ABC",id,n,deposit,100,30,_=>.5f,_=>throw new IOException(),out _)));Assert.Equal(original,JsonConvert.SerializeObject(data));Assert.False(g.Active);}
  [Theory][InlineData("stash:franklin_house")][InlineData("trunk:1:ABC")][InlineData("home:eclipse")]
  public void GestureRoundTripSurvivesReloadAndKeepsOtherData(string container){var data=JObject.Parse("{\"Items\":{\"water\":10},\"Containers\":{\"trunk:legacy\":{\"custom\":99}},\"Future\":{\"keep\":true}}").ToObject<InvData>();bool Move(string id,bool deposit,int n){bool moved=InventoryTransfers.Move(data,container,id,n,deposit,100,30,_=>.5f,_=>{},out var next);if(moved)data=next;return moved;}Assert.True(Gesture().Release(true,true,"water",10,10,true,false,Move));data=JsonConvert.DeserializeObject<InvData>(JsonConvert.SerializeObject(data));Assert.True(Gesture(true).Release(false,true,"water",10,10,true,false,Move));Assert.Equal(10,data.Items["water"]);Assert.Empty(data.Containers[container]);Assert.Equal(99,data.Containers["trunk:legacy"]["custom"]);Assert.True((bool)JObject.Parse(JsonConvert.SerializeObject(data))["Future"]["keep"]);}
  [Fact] public void PublicStorageAllowsArmoryButDoesNotUnlockJobs(){Assert.True(InventoryArmoryRules.ActivityAllows("storage"));Assert.True(InventoryArmoryRules.ActivityAllows("homestorage"));Assert.False(InventoryArmoryRules.ActivityAllows("pizza"));}
 }
}
