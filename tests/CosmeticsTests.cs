using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class CosmeticsTests
    {
        static readonly Action Save=()=>{};
        static GroomingLook Patch(int id=4,int style=2)=>new GroomingLook{Model=-123,Overlays=new List<CosmeticOverlay>{new CosmeticOverlay{Id=id,Style=style,Color=3,Opacity=70}}};
        static WalletData Wallet()=>new WalletData{Bank=1000,Cash=55};
        static void Done(WalletData w,string id){Assert.True(GroomingTransactions.Start(w,id,Save,out _));Assert.True(GroomingTransactions.Complete(w,id,Save,out _));}
        [Theory]
        [InlineData(0,0)][InlineData(3,0)][InlineData(4,2)][InlineData(5,2)][InlineData(6,0)][InlineData(7,0)][InlineData(8,2)][InlineData(9,0)][InlineData(10,1)][InlineData(11,0)][InlineData(12,0)]
        public void SupportedGroupsHaveExplicitPaletteAndIndependentRecords(int id,int type)
        {var p=Patch(id);Assert.True(p.Valid);Assert.Equal(type,CosmeticRules.ColorType(id));Assert.False(string.IsNullOrWhiteSpace(CosmeticRules.Label(id)));var w=Wallet();Assert.True(GroomingTransactions.Begin(w,p,120,"service",Save,out _));Done(w,"service");Assert.Equal(id,Assert.Single(Assert.Single(w.Grooming.Looks).Overlays).Id);Assert.Equal(880,w.Bank);Assert.False(w.Grooming.Looks[0].Hair);Assert.False(w.Grooming.Looks[0].Beard);}
        [Theory][InlineData(-1)][InlineData(1)][InlineData(2)][InlineData(13)][InlineData(255)]
        public void RejectsUnknownGroupsAndDuplicateLegacyBeardBrows(int id)
        {var w=Wallet();Assert.False(Patch(id).Valid);Assert.False(GroomingTransactions.Begin(w,Patch(id),120,"bad",Save,out _));Assert.Null(w.Grooming);Assert.Equal(1000,w.Bank);}
        [Fact] public void EmptyLegacyAppearanceDoesNotBecomeValidAndOldHairLoadsWithoutOverlays()
        {Assert.False(new GroomingLook{Model=1}.Valid);var old=JsonConvert.DeserializeObject<GroomingLook>("{\"Model\":-123,\"Hair\":true,\"HairColor\":4,\"Future\":1}");Assert.True(old.Valid);Assert.Empty(old.Overlays);Assert.True(GroomingTransactions.Merge(old,Patch()).Hair);}
        [Fact] public void SelectionCannotHaveNullOrDuplicateGroups()
        {var p=Patch();p.Overlays.Add(new CosmeticOverlay{Id=4});Assert.False(p.Valid);p.Overlays=new List<CosmeticOverlay>{null};Assert.False(p.Valid);p.Overlays=null;Assert.False(p.Valid);}
        [Theory][InlineData(-1,70,3)][InlineData(256,70,3)][InlineData(2,-1,3)][InlineData(2,101,3)][InlineData(2,70,-1)][InlineData(2,70,64)]
        public void InvalidStyleOpacityOrColorCannotBePaid(int style,int opacity,int color)
        {var p=Patch();p.Overlays[0].Style=style;p.Overlays[0].Opacity=opacity;p.Overlays[0].Color=color;Assert.False(p.Valid);}
        [Fact] public void MergeLeavesOldObjectAndUnselectedGroupsUntouched()
        {var old=Patch(4);old.Hair=true;old.HairColor=9;old.Overlays.Add(Patch(5).Overlays[0]);string before=JsonConvert.SerializeObject(old);var next=GroomingTransactions.Merge(old,Patch(8));Assert.Equal(before,JsonConvert.SerializeObject(old));Assert.Equal(3,next.Overlays.Count);Assert.Equal(9,next.HairColor);Assert.Equal(2,next.Overlays.Find(o=>o.Id==5).Style);Assert.False(next.Beard);}
        [Fact] public void SelectedUpdateReplacesOnlyItsValuesAndKeepsUnknownMetadata()
        {var j=JObject.FromObject(Patch());j["Overlays"][0]["Future"]=7;var old=j.ToObject<GroomingLook>();var patch=Patch(4,6);var next=GroomingTransactions.Merge(old,patch);Assert.Single(next.Overlays);Assert.Equal(6,next.Overlays[0].Style);Assert.Equal(7,(int)JObject.FromObject(next)["Overlays"][0]["Future"]);}
        [Fact] public void NewGroupUnknownFieldsArePreservedAndPatchIsDeepCopied()
        {var j=JObject.FromObject(Patch(8));j["Overlays"][0]["Future"]=8;var p=j.ToObject<GroomingLook>();var next=GroomingTransactions.Merge(Patch(),p);p.Overlays[0].Style=17;Assert.Equal(2,next.Overlays.Find(o=>o.Id==8).Style);Assert.Equal(8,(int)JObject.FromObject(next)["Overlays"][1]["Future"]);}
        [Fact] public void FullGroupCombinationCostsOneFeeAndCanResumeAfterReload()
        {var p=Patch();p.Overlays=CosmeticRules.Ids.Select(id=>new CosmeticOverlay{Id=id,Style=255,Opacity=0}).ToList();p.Hair=true;p.Beard=true;p.Brows=true;var w=Wallet();Assert.True(GroomingTransactions.Begin(w,p,120,"all",Save,out _));w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Done(w,"all");Assert.Equal(880,w.Bank);Assert.Equal(1,w.Grooming.Services);Assert.Equal(11,w.Grooming.Looks[0].Overlays.Count);Assert.True(GroomingTransactions.Complete(w,"all",Save,out bool changed));Assert.False(changed);Assert.Equal(880,w.Bank);}
        [Fact] public void PendingSnapshotPreservesChoiceWhenDraftChanges()
        {var p=Patch();var w=Wallet();Assert.True(GroomingTransactions.Begin(w,p,120,"snap",Save,out _));p.Overlays[0].Color=31;p.Overlays.Clear();Assert.Equal(3,w.Grooming.Pending.Look.Overlays[0].Color);Done(w,"snap");Assert.Single(w.Grooming.Looks[0].Overlays);}
        [Fact] public void SameSelectedChecksOnlyRequestedGroupsButChecksColoredValues()
        {var known=Patch();known.Overlays.Add(Patch(8).Overlays[0]);Assert.True(GroomingTransactions.SameSelected(known,Patch()));var p=Patch();p.Overlays[0].Color++;Assert.False(GroomingTransactions.SameSelected(known,p));p=Patch();p.Overlays[0].Opacity--;Assert.False(GroomingTransactions.SameSelected(known,p));Assert.False(GroomingTransactions.SameSelected(known,Patch(5)));}
        [Fact] public void NonTintedGroupsIgnoreUnusedColorForFreeReapply()
        {var old=Patch(0);var patch=Patch(0);patch.Overlays[0].Color=31;Assert.True(GroomingTransactions.SameSelected(old,patch));}
        [Fact] public void FailureBeforeAndAfterStartKeepsExistingGroupsAndFeeState()
        {var w=Wallet();Assert.True(GroomingTransactions.Begin(w,Patch(),120,"first",Save,out _));Done(w,"first");Assert.Throws<Exception>(()=>GroomingTransactions.Begin(w,Patch(8),120,"bad",()=>throw new Exception(),out _));Assert.Equal(880,w.Bank);Assert.Null(w.Grooming.Pending);Assert.True(GroomingTransactions.Begin(w,Patch(8),120,"second",Save,out _));Assert.True(GroomingTransactions.Start(w,"second",Save,out _));Assert.Throws<Exception>(()=>GroomingTransactions.Complete(w,"second",()=>throw new Exception(),out _));Assert.Single(w.Grooming.Looks[0].Overlays);Assert.Equal(760,w.Bank);Assert.NotNull(w.Grooming.Pending);Done(w,"second");Assert.Equal(2,w.Grooming.Looks[0].Overlays.Count);Assert.Equal(760,w.Bank);}
        [Fact] public void LegacyPaidPendingResumesWithoutApplyingOrAddingNewGroups()
        {var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":880,\"Grooming\":{\"Services\":1,\"Spent\":120,\"Pending\":{\"Id\":\"old\",\"Price\":120,\"Look\":{\"Model\":-123,\"Beard\":true,\"BeardStyle\":2}}}}");Assert.True(w.Grooming.Valid);Done(w,"old");Assert.True(w.Grooming.Looks[0].Beard);Assert.Empty(w.Grooming.Looks[0].Overlays);Assert.Equal(880,w.Bank);}
        [Fact] public void ExplicitNoneRemainsASelectedGroupAndDoesNotRemoveOtherRecords()
        {var old=Patch();old.Overlays.Add(Patch(8).Overlays[0]);var patch=Patch(4,255);patch.Overlays[0].Opacity=0;var next=GroomingTransactions.Merge(old,patch);Assert.Equal(255,next.Overlays.Find(o=>o.Id==4).Style);Assert.Equal(2,next.Overlays.Find(o=>o.Id==8).Style);Assert.True(next.Valid);}
    }
}
