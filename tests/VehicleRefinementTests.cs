using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class VehicleRefinementTests
    {
        [Theory][InlineData(VehicleAction.Hood,4)][InlineData(VehicleAction.Trunk,5)][InlineData(VehicleAction.DoorFrontLeft,0)][InlineData(VehicleAction.DoorFrontRight,1)][InlineData(VehicleAction.DoorRearLeft,2)][InlineData(VehicleAction.DoorRearRight,3)][InlineData(VehicleAction.Engine,-1)]
        public void SourceDoorActionsMapOnlyToTheCorrectNativeIndex(VehicleAction action,int expected)=>Assert.Equal(expected,VehicleUiRules.Door(action));
        [Theory][InlineData(VehicleAction.WindowFrontLeft,0)][InlineData(VehicleAction.WindowFrontRight,1)][InlineData(VehicleAction.WindowRearLeft,2)][InlineData(VehicleAction.WindowRearRight,3)][InlineData(VehicleAction.WindowsUp,-1)]
        public void IndividualWindowMappingDoesNotConfuseBatchActions(VehicleAction action,int expected)=>Assert.Equal(expected,VehicleUiRules.Window(action));
        [Theory][InlineData(true,0,true)][InlineData(true,.5f,true)][InlineData(true,.51f,false)][InlineData(false,0,false)][InlineData(true,float.NaN,false)][InlineData(true,float.PositiveInfinity,false)][InlineData(true,-1,false)]
        public void BodyCommandsRequireAStoppedCar(bool car,float speed,bool expected)=>Assert.Equal(expected,VehicleUiRules.BodySafe(car,speed));
        [Fact]public void MenuOwnerProofRejectsAnotherActorVehicleProfileOrPassenger(){Assert.True(VehicleUiRules.SameOwner(1,1,2,2,"Franklin","Franklin",true));Assert.False(VehicleUiRules.SameOwner(1,3,2,2,"Franklin","Franklin",true));Assert.False(VehicleUiRules.SameOwner(1,1,2,9,"Franklin","Franklin",true));Assert.False(VehicleUiRules.SameOwner(1,1,2,2,"Franklin","Michael",true));Assert.False(VehicleUiRules.SameOwner(1,1,2,2,"Franklin","Franklin",false));}
        [Theory][InlineData("255,128,0")][InlineData("255;128;0")][InlineData("255 128 0")]
        public void RgbInputAcceptsThreeBoundedIntegerChannels(string text){Assert.True(VehicleUiRules.Rgb(text,out var rgb));Assert.Equal(new[]{255,128,0},rgb);}
        [Theory][InlineData("256,0,0")][InlineData("-1,0,0")][InlineData("1.5,0,0")][InlineData("1,2")][InlineData("1,2,3,4")][InlineData("1e2,0,0")][InlineData("NaN,0,0")][InlineData("")]
        public void InvalidRgbCannotReachNativePurchase(string text){Assert.False(VehicleUiRules.Rgb(text,out var rgb));Assert.Null(rgb);}
        [Fact]public void StoreRemovalUsesTheOriginalHandleIdentityNotTheInvalidatedWrapper(){Assert.True(VehicleUiRules.Removed(false,123,"car",0,null));Assert.False(VehicleUiRules.Removed(true,123,"car",123,"car"));Assert.True(VehicleUiRules.Removed(true,123,"car",456,"other"));Assert.True(VehicleUiRules.Removed(true,123,"car",123,"other"));}
        [Fact]public void ResolvingByIdUsesTheLatestRecordAfterAutosaveReplacesTheObject(){var old=new SavedVehicle{Id="owned",GarageId="home",Fuel=5};var data=new TransportData();data.Vehicles.Add(old);data.Vehicles[0]=new SavedVehicle{Id="owned",GarageId="home",Fuel=25,Extra=new Dictionary<string,JToken>{{"Future",true}}};int index=VehicleUiRules.Resolve(data,old.Id,"home");Assert.Equal(25,data.Vehicles[index].Fuel);Assert.Equal(-1,VehicleUiRules.Resolve(data,old.Id,"other-garage"));Assert.Equal(-1,VehicleUiRules.Resolve(data,"missing","home"));}
        [Fact]public void NeonProofIsSavedBeforeTurningOffAndSurvivesReloadWithoutGrantingNewSides(){var d=new TransportData();int saved=0;Assert.Equal(5,VehicleUiRules.RememberNeons(d,"123:ABC",5,()=>saved++));Assert.Equal(1,saved);d=JsonConvert.DeserializeObject<TransportData>(JsonConvert.SerializeObject(d));Assert.Equal(5,VehicleUiRules.RememberNeons(d,"123:ABC",0,()=>throw new Exception()));Assert.Equal(0,VehicleUiRules.RememberNeons(d,"456:OTHER",0,()=>throw new Exception()));Assert.Equal(7,VehicleUiRules.RememberNeons(d,"123:ABC",2,()=>saved++));Assert.Equal(2,saved);Assert.Equal(7,d.NeonEquipment["123:ABC"]);}
        [Fact]public void NeonProofSaveFailureRestoresTheEntireOldDictionary(){var d=new TransportData{NeonEquipment=new Dictionary<string,int>{{"old",1}}};var old=d.NeonEquipment;Assert.Throws<IOException>(()=>VehicleUiRules.RememberNeons(d,"new",4,()=>throw new IOException()));Assert.Same(old,d.NeonEquipment);Assert.Single(d.NeonEquipment);Assert.Equal(1,d.NeonEquipment["old"]);}
        [Fact]public void CorruptNeonProofIsNotRepairedOrUsedAsAnInstallationGrant(){var d=new TransportData{NeonEquipment=new Dictionary<string,int>{{"future",99}}};Assert.Equal(-1,VehicleUiRules.RememberNeons(d,"car",1,()=>throw new Exception()));Assert.Equal(99,d.NeonEquipment["future"]);}
        [Fact]public void NeonProofDoesNotTouchFuelVehicleEquipmentOrUnknownTransportFields(){var d=new TransportData{Extra=new Dictionary<string,JToken>{{"Future","keep"}}};d.Tanks["car"]=new FuelTank{Liters=17};d.Vehicles.Add(new SavedVehicle{Id="owned",Mods={{11,2}},CustomPrimary=new[]{1,2,3}});Assert.Equal(8,VehicleUiRules.RememberNeons(d,"car",8,()=>{}));Assert.Equal(17,d.Tanks["car"].Liters);Assert.Equal(2,d.Vehicles[0].Mods[11]);Assert.Equal(new[]{1,2,3},d.Vehicles[0].CustomPrimary);Assert.Equal("keep",(string)d.Extra["Future"]);}
        [Fact]public void PerformanceCategoryExcludesCosmeticAndWheelSlots(){Assert.True(VehicleUiRules.Performance(11));Assert.True(VehicleUiRules.Performance(15));Assert.False(VehicleUiRules.Performance(0));Assert.False(VehicleUiRules.Performance(23));}
        [Fact]public void RgbPurchaseUsesExistingRollbackAndChargesOnlyAfterNativeVerification(){var w=new WalletData{Bank=500};int[] live={0,0,0};VehicleUiRules.Rgb("255,128,0",out var value);Assert.Throws<IOException>(()=>WorkshopRules.Buy(w,300,()=>true,()=>false,()=>live=value,()=>true,()=>live=new[]{0,0,0},()=>throw new IOException()));Assert.Equal(500,w.Bank);Assert.Equal(new[]{0,0,0},live);}
    }
}
