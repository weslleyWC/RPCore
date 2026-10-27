using System;
using System.Collections.Generic;
using Xunit;
namespace RPCore.Tests
{
    public class InventoryBurdenTests
    {
        [Theory][InlineData(0)][InlineData(29)][InlineData(30)][InlineData(30.0005)]public void AtOrBelowCapacityDoesNotSlow(double kg)=>Assert.Equal(1,InventoryBurdenRules.MoveRate(kg,30));
        [Fact]public void OverweightProgressivelySlowsButCannotImmobilize(){float a=InventoryBurdenRules.MoveRate(31,30),b=InventoryBurdenRules.MoveRate(45,30),c=InventoryBurdenRules.MoveRate(90,30);Assert.InRange(a,.45f,.8f);Assert.True(b<a);Assert.True(c<b);Assert.Equal(.45f,c);Assert.Equal(.45f,InventoryBurdenRules.MoveRate(double.MaxValue,30));}
        [Fact]public void RemovingWeightImmediatelyRestoresUnmodifiedRate(){Assert.True(InventoryBurdenRules.MoveRate(50,30)<1);Assert.Equal(1,InventoryBurdenRules.MoveRate(29,30));}
        [Theory][InlineData(double.NaN,30)][InlineData(double.PositiveInfinity,30)][InlineData(-1,30)][InlineData(30,0)][InlineData(30,double.NaN)][InlineData(30,double.PositiveInfinity)]public void InvalidMeasuresDoNotIssueUnsafeNativeRates(double kg,double limit)=>Assert.Equal(1,InventoryBurdenRules.MoveRate(kg,limit));
        [Fact]public void QuantitiesAndUnknownItemsCountButContainerStockDoesNot(){var items=new Dictionary<string,int>{{"water",2},{"ammo",100},{"unknown",3},{"empty",0}};Assert.Equal(6.3,InventoryBurdenRules.Total(items,id=>id=="water" ? .5f : id=="ammo" ? .05f : .1f),5);}
        [Fact]public void LargeFiniteCountsCannotOverflowToInfinity(){double kg=InventoryBurdenRules.Total(new Dictionary<string,int>{{"large",int.MaxValue}},_=>float.MaxValue);Assert.False(double.IsInfinity(kg));Assert.Equal(.45f,InventoryBurdenRules.MoveRate(kg,30));}
        [Fact]public void FloatingPointItemSumsAtLimitDoNotCreateFalseOverweight(){double kg=InventoryBurdenRules.Total(new Dictionary<string,int>{{"item",300}},_=>.1f);Assert.Equal(1,InventoryBurdenRules.MoveRate(kg,30));}
        [Theory][InlineData("human")][InlineData("alive")][InlineData("foot")][InlineData("vehicle")][InlineData("boarding")][InlineData("swimming")][InlineData("airborne")][InlineData("ragdoll")][InlineData("climbing")][InlineData("paused")][InlineData("mission")][InlineData("cutscene")][InlineData("control")][InlineData("profile")]
        public void UnsafeContextsDoNotAffectMovement(string reason)=>Assert.False(InventoryBurdenRules.Safe(reason!="human",reason!="alive",reason!="foot",reason=="vehicle",reason=="boarding",reason=="swimming",reason=="airborne",reason=="ragdoll",reason=="climbing",reason=="paused",reason=="mission",reason=="cutscene",reason!="control",reason!="profile"));
        [Fact]public void SafeFootContextAllowsBurden()=>Assert.True(InventoryBurdenRules.Safe(true,true,true,false,false,false,false,false,false,false,false,false,true,true));
        [Fact]public void OffloadFittingAmountIsAcceptedWithoutDiscardingRemainingItems(){var d=new InvData{Items=new Dictionary<string,int>{{"ammo",10000}}};int fit=InventoryTransfers.Fits(d,"home:test","ammo",true,200,30,_=>.05f);Assert.InRange(fit,3999,4000);Assert.True(InventoryTransfers.Move(d,"home:test","ammo",fit,true,200,30,_=>.05f,n=>{},out d));Assert.Equal(10000-fit,d.Items["ammo"]);Assert.Equal(fit,d.Containers["home:test"]["ammo"]);Assert.Equal(0,InventoryTransfers.Fits(d,"home:test","ammo",true,200,30,_=>.05f));}
        [Fact]public void CannotWithdrawMoreIntoAlreadyOverweightInventory(){var d=new InvData{Items=new Dictionary<string,int>{{"ammo",1000}},Containers=new Dictionary<string,Dictionary<string,int>>{{"home:test",new Dictionary<string,int>{{"ammo",100}}}}};Assert.Equal(0,InventoryTransfers.Fits(d,"home:test","ammo",false,200,30,_=>.05f));}
        [Fact]public void FittingWithdrawalRespectsBothAvailableCountAndWeight(){var d=new InvData{Items=new Dictionary<string,int>{{"water",1}},Containers=new Dictionary<string,Dictionary<string,int>>{{"home:test",new Dictionary<string,int>{{"water",20}}}}};Assert.Equal(3,InventoryTransfers.Fits(d,"home:test","water",false,200,2,_=>.5f));}
        [Fact]public void FittingZeroWeightItemsStillChecksCounterOverflow(){var d=new InvData{Items=new Dictionary<string,int>{{"item",5}},Containers=new Dictionary<string,Dictionary<string,int>>{{"home:test",new Dictionary<string,int>{{"item",int.MaxValue}}}}};Assert.Equal(0,InventoryTransfers.Fits(d,"home:test","item",true,200,30,_=>0));}
    }
}
