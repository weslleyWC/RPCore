using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class WardrobeTests
    {
        static readonly Action Save = () => { };
        static WardrobeOptions C => WardrobeDefaults.Load();
        static WardrobeOutfit Outfit(string id="one",string name="Social",int model=-123)
        { return new WardrobeOutfit { Id=id,Name=name,Model=model,Components=WardrobeRules.ComponentSlots.Select(s=>new WardrobePiece{Slot=s,Drawable=0,Texture=0}).ToList(),Props=WardrobeRules.PropSlots.Select(s=>new WardrobePiece{Slot=s,Drawable=-1,Texture=0}).ToList() }; }
        static WalletData W() => new WalletData { Bank=2000000,Cash=123 };
        static void Put(WalletData w,WardrobeOutfit o=null) => Assert.True(WardrobeTransactions.Save(w,C,o??Outfit(),false,"save",Save,out _));
        [Fact] public void DefaultsMatchReferenceWardrobePositionAndKeepHeadAndHairOut()
        { var c=C; Assert.True(c.Valid); Assert.Equal(-797.761f,c.Point.X); Assert.Equal(328.302f,c.Point.Y); Assert.Equal(190.7137f,c.Point.Z); Assert.Equal(30,c.Capacity); Assert.DoesNotContain(0,WardrobeRules.ComponentSlots); Assert.DoesNotContain(2,WardrobeRules.ComponentSlots); Assert.Equal(10,WardrobeRules.ComponentSlots.Length); Assert.Equal(new[]{0,1,2,6,7},WardrobeRules.PropSlots); }
        [Fact] public void MissingOldWardrobeDoesNotAutoCreateOutfitsOrChangeMoney()
        { var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":42,\"Cash\":21}"); Assert.Null(w.Wardrobe); Assert.Empty(new WardrobeData().Outfits); Assert.Equal(42,w.Bank); }
        [Fact] public void SavingClonesCompleteSnapshotWithoutChangingAnyAccountOrProperty()
        { var w=W(); w.Housing=new HousingData(); var home=w.Housing; var o=Outfit(); Put(w,o); Assert.NotSame(o,w.Wardrobe.Outfits[0]); o.Components[0].Drawable=22; Assert.Equal(0,w.Wardrobe.Outfits[0].Components[0].Drawable); Assert.Same(home,w.Housing); Assert.Equal(2000000,w.Bank); Assert.Equal(123,w.Cash); Assert.True(w.Wardrobe.Valid); }
        [Fact] public void FailedSaveRestoresNullLibraryAndAllWalletData()
        { var w=W(); string before=JsonConvert.SerializeObject(w); Assert.Throws<Exception>(()=>WardrobeTransactions.Save(w,C,Outfit(),false,"save",()=>throw new Exception(),out _)); Assert.Null(w.Wardrobe); Assert.Equal(before,JsonConvert.SerializeObject(w)); }
        [Fact] public void ReceiptReplayAfterReloadNeverDuplicatesOutfit()
        { var w=W(); Put(w); w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); Assert.True(WardrobeTransactions.Save(w,C,Outfit(),false,"save",()=>throw new Exception(),out bool changed)); Assert.False(changed); Assert.Single(w.Wardrobe.Outfits); }
        [Fact] public void DuplicateNameForSameModelRequiresExplicitReplacement()
        { var w=W(); Put(w); Assert.False(WardrobeTransactions.Save(w,C,Outfit("two","social"),false,"duplicate",Save,out _)); Assert.False(WardrobeTransactions.Save(w,C,Outfit(),false,"sameid",Save,out _)); Assert.Single(w.Wardrobe.Outfits); }
        [Fact] public void SameNameOnAnotherModelIsAllowedWithoutConvertingModel()
        { var w=W(); Put(w); Assert.True(WardrobeTransactions.Save(w,C,Outfit("two","Social",456),false,"different",Save,out _)); Assert.Equal(2,w.Wardrobe.Outfits.Count); Assert.Equal(-123,w.Wardrobe.Outfits[0].Model); Assert.Equal(456,w.Wardrobe.Outfits[1].Model); }
        [Fact] public void ExplicitReplacementPreservesIdAndUpdatesOnlyChosenOutfit()
        { var w=W(); Put(w); var replace=Outfit(); replace.Components[0].Drawable=3; Assert.True(WardrobeTransactions.Save(w,C,replace,true,"replace",Save,out _)); Assert.Single(w.Wardrobe.Outfits); Assert.Equal("one",w.Wardrobe.Outfits[0].Id); Assert.Equal(3,w.Wardrobe.Outfits[0].Components[0].Drawable); }
        [Fact] public void ReplacementCannotChangeModelOrCreateMissingId()
        { var w=W(); Put(w); Assert.False(WardrobeTransactions.Save(w,C,Outfit(model:456),true,"wrongmodel",Save,out _)); Assert.False(WardrobeTransactions.Save(w,C,Outfit("missing"),true,"missing",Save,out _)); Assert.Equal(-123,w.Wardrobe.Outfits[0].Model); }
        [Fact] public void LoweredCapacityPreservesLibraryAndAllowsReplaceRenameDelete()
        { var w=W(); Put(w); var c=C; c.Capacity=1; Assert.False(WardrobeTransactions.Save(w,c,Outfit("two","Casual"),false,"full",Save,out _)); Assert.True(WardrobeTransactions.Save(w,c,Outfit(),true,"replace",Save,out _)); Assert.True(WardrobeTransactions.Rename(w,"one","Novo","rename",Save,out _)); Assert.True(WardrobeTransactions.Delete(w,"one","delete",Save,out _)); Assert.Empty(w.Wardrobe.Outfits); }
        [Fact] public void RenameRejectsDuplicateNamesButAllowsCaseChangeOnSameRecord()
        { var w=W(); Put(w); WardrobeTransactions.Save(w,C,Outfit("two","Casual"),false,"second",Save,out _); Assert.False(WardrobeTransactions.Rename(w,"one","casual","bad",Save,out _)); Assert.True(WardrobeTransactions.Rename(w,"one","SOCIAL","rename",Save,out _)); Assert.Equal("SOCIAL",w.Wardrobe.Outfits[0].Name); }
        [Fact] public void RenameDeleteAndReplaceFailuresRollBackExactExistingObject()
        { var w=W(); Put(w); var old=w.Wardrobe; string before=JsonConvert.SerializeObject(w); Assert.Throws<Exception>(()=>WardrobeTransactions.Rename(w,"one","Novo","r",()=>throw new Exception(),out _)); Assert.Same(old,w.Wardrobe); Assert.Throws<Exception>(()=>WardrobeTransactions.Delete(w,"one","d",()=>throw new Exception(),out _)); Assert.Throws<Exception>(()=>WardrobeTransactions.Save(w,C,Outfit(),true,"s",()=>throw new Exception(),out _)); Assert.Equal(before,JsonConvert.SerializeObject(w)); }
        [Fact] public void DeleteReplayCannotRemoveAnotherRecord()
        { var w=W(); Put(w); WardrobeTransactions.Save(w,C,Outfit("two","Casual"),false,"second",Save,out _); Assert.True(WardrobeTransactions.Delete(w,"one","delete",Save,out _)); Assert.True(WardrobeTransactions.Delete(w,"two","delete",()=>throw new Exception(),out bool changed)); Assert.False(changed); Assert.Equal("two",Assert.Single(w.Wardrobe.Outfits).Id); }
        [Fact] public void UnknownWalletLibraryOutfitAndPieceFieldsSurviveReplacement()
        { var w=W(); Put(w); var j=JObject.Parse(JsonConvert.SerializeObject(w)); j["Future"]=1; j["Wardrobe"]["Future"]=2; j["Wardrobe"]["Outfits"][0]["Future"]=3; j["Wardrobe"]["Outfits"][0]["Components"][0]["Future"]=4; w=j.ToObject<WalletData>(); var replace=Outfit(); replace.Components[0].Drawable=2; WardrobeTransactions.Save(w,C,replace,true,"replace",Save,out _); j=JObject.Parse(JsonConvert.SerializeObject(w)); Assert.Equal(1,(int)j["Future"]); Assert.Equal(2,(int)j["Wardrobe"]["Future"]); Assert.Equal(3,(int)j["Wardrobe"]["Outfits"][0]["Future"]); Assert.Equal(4,(int)j["Wardrobe"]["Outfits"][0]["Components"][0]["Future"]); }
        [Theory] [InlineData("")] [InlineData("   ")] [InlineData(" trailing ")] [InlineData("line\nbreak")]
        public void InvalidNamesCannotCreateOrRename(string name)
        { var w=W(); Assert.False(WardrobeTransactions.Save(w,C,Outfit(name:name),false,"save",Save,out _)); Assert.False(WardrobeTransactions.Rename(w,"one",name,"rename",Save,out _)); Assert.Null(w.Wardrobe); }
        [Fact] public void SnapshotRequiresAllDistinctSupportedSlotsAndValidPalette()
        { var o=Outfit(); Assert.True(o.Valid); o.Components[0].Slot=0; Assert.False(o.Valid); o=Outfit(); o.Components[0].Slot=o.Components[1].Slot; Assert.False(o.Valid); o=Outfit(); o.Props.RemoveAt(0); Assert.False(o.Valid); o=Outfit(); o.Components[0].Palette=4; Assert.False(o.Valid); o=Outfit(); o.Model=0; Assert.False(o.Valid); }
        [Fact] public void NoPropHasCanonicalZeroTextureAndCannotUseOtherNegativeDrawables()
        { var o=Outfit(); o.Props[0].Texture=-1; Assert.False(o.Valid); o=Outfit(); o.Props[0].Drawable=-2; Assert.False(o.Valid); Assert.True(WardrobeRules.Available(new WardrobePiece{Drawable=-1},true,0,0)); }
        [Fact] public void AvailabilityChecksDrawableAndTextureLimitsBeforeApplication()
        { var p=new WardrobePiece{Drawable=2,Texture=1}; Assert.True(WardrobeRules.Available(p,false,3,2)); Assert.False(WardrobeRules.Available(p,false,2,2)); Assert.False(WardrobeRules.Available(p,false,3,1)); Assert.False(WardrobeRules.Available(p,true,0,0)); Assert.True(WardrobeRules.Available(new WardrobePiece(),false,0,0)); }
        [Fact] public void EditorCyclesFromNoneToFirstPropAndBackWithoutInvalidIndex()
        { Assert.Equal(0,WardrobeRules.Cycle(-1,1,3,true)); Assert.Equal(2,WardrobeRules.Cycle(-1,-1,3,true)); Assert.Equal(-1,WardrobeRules.Cycle(2,1,3,true)); Assert.Equal(0,WardrobeRules.Cycle(0,1,1,false)); Assert.Equal(2,WardrobeRules.Cycle(0,-1,3,false)); Assert.Equal(-1,WardrobeRules.Cycle(-1,1,0,true)); }
        [Theory] [InlineData(3,1,3,false)] [InlineData(-2,1,3,true)] [InlineData(0,0,3,false)] [InlineData(0,1,-1,false)]
        public void BadEditorCountsOrIndicesAreRejected(int current,int delta,int count,bool prop)
        { Assert.Throws<ArgumentException>(()=>WardrobeRules.Cycle(current,delta,count,prop)); }
        [Fact] public void InvalidExistingLibraryRemainsUntouched()
        { var w=W(); w.Wardrobe=new WardrobeData(); w.Wardrobe.Outfits.Add(Outfit()); w.Wardrobe.Outfits.Add(Outfit()); string before=JsonConvert.SerializeObject(w); Assert.False(WardrobeTransactions.Delete(w,"one","delete",Save,out _)); Assert.Equal(before,JsonConvert.SerializeObject(w)); }
        [Fact] public void SeparateProfilesDoNotShareOutfits()
        { var a=W(); var b=W(); Put(a); Assert.Null(b.Wardrobe); Assert.Equal(2000000,b.Bank); }
    }
}
