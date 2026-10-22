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
    public class InterfaceDataTests
    {
        VisualCatalog Catalog() { return JsonConvert.DeserializeObject<VisualCatalog>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Visuals","Catalog.json"))); }
        [Fact] public void GridMigrationAndMovingDoNotChangeOldQuantitiesContainersOrUnknownFields()
        {
            const string old = "{\"Items\":{\"water\":3,\"custom_unknown_item\":7},\"Containers\":{\"trunk:RP123\":{\"burger\":5}},\"FutureSystem\":{\"protected\":true}}";
            var state = JsonConvert.DeserializeObject<InvData>(old); var original = JObject.Parse(old);
            state.GridSlots = InventoryLayout.Reconcile(state.GridSlots,state.Items.Keys);
            int water = state.GridSlots.IndexOf("water"); Assert.True(InventoryLayout.Swap(state.GridSlots,water,24));
            var roundtrip = JObject.Parse(JsonConvert.SerializeObject(state));
            Assert.True(JToken.DeepEquals(original["Items"],roundtrip["Items"]));
            Assert.True(JToken.DeepEquals(original["Containers"],roundtrip["Containers"]));
            Assert.True(JToken.DeepEquals(original["FutureSystem"],roundtrip["FutureSystem"]));
            Assert.Equal("water",JsonConvert.DeserializeObject<InvData>(roundtrip.ToString()).GridSlots[24]);
        }
        [Theory] [InlineData(0,25)] [InlineData(25,25)] [InlineData(26,50)] [InlineData(51,75)] [InlineData(130,150)]
        public void EveryDistinctItemGetsASlotEvenBeyondFirstPage(int count,int expected)
        {
            var ids = Enumerable.Range(0,count).Select(i=>"item"+i).ToArray();
            var slots = InventoryLayout.Reconcile(null,ids);
            Assert.Equal(expected,slots.Count); Assert.Equal(count,slots.Count(id=>id!=null));
            Assert.Equal(ids.OrderBy(id=>id),slots.Where(id=>id!=null).OrderBy(id=>id));
        }
        [Fact] public void DuplicateAndStaleLayoutEntriesAreRepairedWithoutDroppingUnknownItems()
        {
            var slots = InventoryLayout.Reconcile(new[] {"water","water","obsolete",null,"custom"},new[] {"water","custom","added"});
            Assert.Equal("water",slots[0]); Assert.Equal("custom",slots[4]); Assert.Single(slots,id=>id=="water");
            Assert.Contains("added",slots); Assert.DoesNotContain("obsolete",slots);
            Assert.Equal(slots,InventoryLayout.Reconcile(slots,new[] {"water","custom","added"}));
        }
        [Fact] public void GridSwapHandlesEmptyDestinationAndRejectsInvalidIndices()
        {
            var slots = InventoryLayout.Reconcile(null,new[] {"water"});
            Assert.False(InventoryLayout.Swap(slots,-1,2)); Assert.False(InventoryLayout.Swap(slots,0,25));
            Assert.True(InventoryLayout.Swap(slots,0,24)); Assert.Null(slots[0]); Assert.Equal("water",slots[24]);
            Assert.Equal(slots,InventoryLayout.Reconcile(slots,new[] {"water"}));
        }
        [Fact] public void TwentyFiveCellsFitThePanelWithoutOverlappingFooterOrEachOther()
        {
            var cells = Enumerable.Range(0,25).Select(ReferenceLayout.Cell).ToArray();
            foreach(var rect in cells) { Assert.True(rect.X>=18 && rect.Right<=868);Assert.True(rect.Y>=164 && rect.Bottom<993); }
            for(int i=0;i<cells.Length;i++) for(int j=i+1;j<cells.Length;j++) Assert.False(cells[i].IntersectsWith(cells[j]));
            Assert.Equal(5,cells.Select(c=>c.X).Distinct().Count());Assert.Equal(5,cells.Select(c=>c.Y).Distinct().Count());
        }
        [Fact] public void PaddedTextureTransformMapsMeasuredAlphaBoundsExactlyToTarget()
        {
            var target = new RectangleF(18,84,850,982);var bounds = ReferenceLayout.InventoryBackgroundBounds;
            var full = ReferenceLayout.FitTexture(target,bounds,1920,1080);
            Assert.InRange(Math.Abs(full.X+bounds.X*full.Width/1920-target.X),0,.001f);
            Assert.InRange(Math.Abs(full.Y+bounds.Y*full.Height/1080-target.Y),0,.001f);
            Assert.InRange(Math.Abs(bounds.Width*full.Width/1920-target.Width),0,.001f);
            Assert.InRange(Math.Abs(bounds.Height*full.Height/1080-target.Height),0,.001f);
            Assert.Equal(RectangleF.Empty,ReferenceLayout.FitTexture(target,RectangleF.Empty,128,128));
        }
        [Fact] public void ScreenshotPhoneArtAndAllElevenHomeIconsExistInOriginalOiv()
        {
            var c=Catalog(); Assert.Equal(11,ReferenceLayout.PhoneIcons.Length); Assert.Equal(11,ReferenceLayout.PhoneLabels.Length);
            foreach(var icon in ReferenceLayout.PhoneIcons) Assert.NotNull(c.Find("PSRP_PHONE",icon));
            for(int i=1;i<=10;i++) Assert.NotNull(c.Find("PSRP_PHONE","PhoneWallpaper"+i));
            foreach(string icon in new[] {"Background","InventoryBox"}) Assert.NotNull(c.Find("PSRP_Inventory",icon));
            Assert.Equal(1920,c.Find("PSRP_Inventory","Background").Width); Assert.Equal(128,c.Find("PSRP_Inventory","InventoryBox").Width);
        }
        [Theory] [InlineData("125*4","500")] [InlineData("1,5+2,5","4")] [InlineData("-5--2","-3")] [InlineData("1e-2+3","3.01")]
        public void CalculatorSupportsTypedArithmeticWithoutExecution(string input,string expected) { string answer;Assert.True(PhoneMath.Calculate(input,out answer));Assert.Equal(expected,answer); }
        [Theory] [InlineData("1/0")] [InlineData("1e308*100")] [InlineData("System.IO.File.Delete()")]
        public void InvalidCalculatorInputDoesNotExecuteOrReturnNonfiniteValues(string input) { string answer;Assert.False(PhoneMath.Calculate(input,out answer)); }
        [Fact] public void PhoneNotesHistoryAndFutureFieldsSurviveSaveRoundtrip()
        {
            var d=JsonConvert.DeserializeObject<PhoneData>("{\"Notes\":[\"remember pizza\"],\"History\":[{\"Title\":\"Inventory\",\"Body\":\"Water\",\"FutureItem\":7}],\"Unknown\":true}");
            Assert.Equal(3,d.Wallpaper);var saved=JObject.Parse(JsonConvert.SerializeObject(d));
            Assert.True((bool)saved["Unknown"]);Assert.Equal(7,(int)saved["History"][0]["FutureItem"]);Assert.Equal("remember pizza",(string)saved["Notes"][0]);
        }
        [Fact] public void SpeedConversionAndCompactCashMatchReferenceUnits()
        { Assert.InRange(ReferenceLayout.Speed(10,false),22.369f,22.37f);Assert.Equal(36,ReferenceLayout.Speed(10,true));Assert.Equal(0,ReferenceLayout.Speed(-1,true));Assert.Equal("39.00",HudMath.CompactCash(39));Assert.Equal("1K",HudMath.CompactCash(1000)); }
        [Fact] public void SixStatusCirclesHaveTheMeasuredRightMarginAtDifferentAspectRatios()
        {
            Assert.Equal(1635,ReferenceLayout.StatusX(1920,1,0));
            foreach(float width in new[] {1920f,2560f,3840f})
                Assert.Equal(width-10,ReferenceLayout.StatusX(width,1,0)+5*47+40);
        }
    }
}
