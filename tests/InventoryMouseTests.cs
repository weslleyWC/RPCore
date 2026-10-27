using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Xunit;

namespace RPCore.Tests
{
    public class InventoryMouseTests
    {
        [Theory][InlineData(1920,0,0,1,false)][InlineData(1920,96,54,.8f,false)][InlineData(2560,128,54,1,true)][InlineData(1280,64,54,.8f,true)]
        public void PointerUsesSameScaledCanvasAsDraw(float width,float ix,float iy,float requested,bool storage)
        {
            float s=InventoryMouseLayout.Scale(storage,width,ix,iy,requested);float x=(storage?1040:140)+20,y=250;
            Assert.True(InventoryMouseLayout.Point((ix+x*s)/width,(iy+y*s)/1080,width,ix,iy,s,out var p));Assert.Equal(0,InventoryMouseLayout.Hit(p,storage,0,96));
        }
        [Theory][InlineData(139,250)][InlineData(940,250)][InlineData(200,229)][InlineData(200,770)][InlineData(1050,250)]
        public void OutsidePersonalGridIsNotADropTarget(float x,float y)=>Assert.Equal(-1,InventoryMouseLayout.Hit(new PointF(x,y),false,0,96));
        [Fact]public void OverlapBelongsToLastDrawnCell(){Assert.Equal(1,InventoryMouseLayout.Hit(new PointF(275,250),false,0,96));Assert.Equal(6,InventoryMouseLayout.Hit(new PointF(150,365),false,0,96));}
        [Fact]public void ScrolledWindowReturnsAbsoluteSlot(){Assert.Equal(42,InventoryMouseLayout.Hit(new PointF(150,250),false,7,96));}
        [Fact]public void CellBeyondPartialInventoryCannotBeSelected()=>Assert.Equal(-1,InventoryMouseLayout.Hit(new PointF(800,630),false,11,89));
        [Theory][InlineData(float.NaN,0,1)][InlineData(-.1f,0,1)][InlineData(1.1f,0,1)][InlineData(0,0,0)][InlineData(0,0,float.NaN)]
        public void InvalidCoordinatesNeverProduceDropPoint(float x,float y,float scale)=>Assert.False(InventoryMouseLayout.Point(x,y,1920,0,0,scale,out _));
        [Fact]public void ClickingOnlySelectsAndDoesNotSaveOrUseItem(){var d=new InventoryMouseDrag();d.Begin(0,"water",new PointF(150,250));d.Move(new PointF(152,251));Assert.False(d.Drop(new[]{"water",null},1,(a,b)=>throw new Exception()));Assert.False(d.Active);}
        [Fact]public void DragSavesExactlyOnceAndSwapsWholePositions(){var data=new InvData{Items=new Dictionary<string,int>{{"water",5},{"burger",2}},GridSlots=new List<string>{"water","burger"}};var d=new InventoryMouseDrag();d.Begin(0,"water",new PointF(150,250));d.Move(new PointF(300,250));int writes=0;Assert.True(d.Drop(data.GridSlots,1,(a,b)=>InventoryEdits.Move(ref data,a,b,n=>writes++)));Assert.False(d.Drop(data.GridSlots,0,(a,b)=>throw new Exception()));Assert.Equal(1,writes);Assert.Equal("water",data.GridSlots[1]);Assert.Equal("burger",data.GridSlots[0]);Assert.Equal(5,data.Items["water"]);Assert.Equal(2,data.Items["burger"]);}
        [Fact]public void EmptySlotReceivesItemWithoutChangingQuantity(){var data=new InvData{Items=new Dictionary<string,int>{{"water",5}},GridSlots=new List<string>{"water",null}};var d=Drag();Assert.True(d.Drop(data.GridSlots,1,(a,b)=>InventoryEdits.Move(ref data,a,b,n=>{})));Assert.Null(data.GridSlots[0]);Assert.Equal("water",data.GridSlots[1]);Assert.Equal(5,data.Items["water"]);}
        static InventoryMouseDrag Drag(){var d=new InventoryMouseDrag();d.Begin(0,"water",new PointF(150,250));d.Move(new PointF(300,250));return d;}
        [Fact]public void OutsideReleaseCancelsWithoutMutation(){var slots=new[]{"water","burger"};var d=Drag();Assert.False(d.Drop(slots,-1,(a,b)=>throw new Exception()));Assert.Equal("water",slots[0]);Assert.False(d.Active);}
        [Fact]public void ReturningToSameSlotDoesNotWrite(){var d=Drag();Assert.False(d.Drop(new[]{"water"},0,(a,b)=>throw new Exception()));}
        [Fact]public void ChangedSourceCannotMoveAnotherItem(){var d=Drag();Assert.False(d.Drop(new[]{"burger",null},1,(a,b)=>throw new Exception()));Assert.False(d.Active);}
        [Fact]public void CancelledGestureCannotCommitOnLaterRelease(){var d=Drag();d.Cancel();Assert.False(d.Drop(new[]{"water",null},1,(a,b)=>throw new Exception()));}
        [Fact]public void SaveFailureKeepsBothPositionsAndClearsGesture(){var data=new InvData{Items=new Dictionary<string,int>{{"water",5},{"burger",2}},GridSlots=new List<string>{"water","burger"}};var d=Drag();Assert.Throws<IOException>(()=>d.Drop(data.GridSlots,1,(a,b)=>InventoryEdits.Move(ref data,a,b,n=>throw new IOException())));Assert.Equal("water",data.GridSlots[0]);Assert.Equal("burger",data.GridSlots[1]);Assert.False(d.Active);}
        [Fact]public void ScrollingDuringDragRetainsSourceAndSupportsAnotherPage(){var nav=new InventoryNavigation();var d=Drag();nav.Scroll(5,96);int target=InventoryMouseLayout.Hit(new PointF(150,250),false,nav.FirstRow,96);var slots=Enumerable.Repeat<string>(null,96).ToList();slots[0]="water";Assert.Equal(30,target);Assert.True(d.Drop(slots,target,(a,b)=>InventoryLayout.Swap(slots,a,b)));Assert.Equal("water",slots[30]);Assert.Null(slots[0]);}
        [Fact]public void ScrollClampsAtBothEndsAndEmptyInventory(){var n=new InventoryNavigation();n.Scroll(500,96);Assert.Equal(12,n.FirstRow);n.Scroll(-500,96);Assert.Equal(0,n.FirstRow);n.Scroll(100,0);Assert.Equal(0,n.FirstRow);Assert.Equal(0,n.Selected);}
    }
}
