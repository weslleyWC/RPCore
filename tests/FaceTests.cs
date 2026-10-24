using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class FaceTests
    {
        static readonly Action Save=()=>{};
        static WalletData W()=>new WalletData{Bank=1000,Cash=55};
        static FaceHeritage Heritage()=>new FaceHeritage{ShapeFirst=3,ShapeSecond=21,SkinFirst=5,SkinSecond=24,ShapeMix=40,SkinMix=60};
        static GroomingLook H()=>new GroomingLook{Model=-123,Heritage=Heritage()};
        static GroomingLook M(int index=0,int value=-40)=>new GroomingLook{Model=-123,Morphs=new List<FaceMorph>{new FaceMorph{Index=index,Value=value}}};
        static void Begin(WalletData w,GroomingLook p,string id="face")=>Assert.True(GroomingTransactions.Begin(w,p,120,id,Save,out _));
        static void Done(WalletData w,string id="face"){Assert.True(GroomingTransactions.Start(w,id,Save,out _));Assert.True(GroomingTransactions.Complete(w,id,Save,out _));}
        [Fact] public void OldLookAndOldPendingHaveNoImplicitFaceOrMorphs()
        {var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":880,\"Grooming\":{\"Services\":1,\"Spent\":120,\"Pending\":{\"Id\":\"face\",\"Price\":120,\"Look\":{\"Model\":-123,\"Hair\":true}}}}");Assert.True(w.Grooming.Valid);Done(w);var l=Assert.Single(w.Grooming.Looks);Assert.Null(l.Heritage);Assert.Empty(l.Morphs);Assert.Equal(880,w.Bank);}
        [Theory]
        [InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)][InlineData(7)][InlineData(8)][InlineData(9)][InlineData(10)][InlineData(11)][InlineData(12)][InlineData(13)][InlineData(14)][InlineData(15)][InlineData(16)][InlineData(17)][InlineData(18)][InlineData(19)]
        public void EachMorphSupportsBothSignsAndCanBeTheOnlySelectedGroup(int index)
        {var w=W();var p=M(index,-100);Assert.True(p.Valid);Assert.False(string.IsNullOrWhiteSpace(FaceRules.Label(index)));Begin(w,p);Done(w);Assert.Equal(-100,Assert.Single(w.Grooming.Looks[0].Morphs).Value);Assert.Null(w.Grooming.Looks[0].Heritage);Begin(w,M(index,100),"positive");Done(w,"positive");Assert.Equal(100,Assert.Single(w.Grooming.Looks[0].Morphs).Value);Assert.Equal(760,w.Bank);}
        [Theory][InlineData(-1,0)][InlineData(20,0)][InlineData(0,-101)][InlineData(0,101)]
        public void InvalidMorphCannotCharge(int index,int value)
        {var w=W();Assert.False(M(index,value).Valid);Assert.False(GroomingTransactions.Begin(w,M(index,value),120,"bad",Save,out _));Assert.Null(w.Grooming);Assert.Equal(1000,w.Bank);}
        [Fact] public void NullDuplicateAndTooManyMorphsAreInvalid()
        {var p=M();p.Morphs.Add(new FaceMorph{Index=0});Assert.False(p.Valid);p.Morphs=new List<FaceMorph>{null};Assert.False(p.Valid);p.Morphs=null;Assert.False(p.Valid);p.Morphs=Enumerable.Range(0,21).Select(i=>new FaceMorph{Index=i}).ToList();Assert.False(p.Valid);}
        [Theory][InlineData(-1,0,50,50)][InlineData(256,0,50,50)][InlineData(0,256,50,50)][InlineData(0,0,-1,50)][InlineData(0,0,101,50)][InlineData(0,0,50,-1)][InlineData(0,0,50,101)]
        public void InvalidHeritageCannotCharge(int shape,int skin,int shapeMix,int skinMix)
        {var p=H();p.Heritage.ShapeFirst=shape;p.Heritage.SkinSecond=skin;p.Heritage.ShapeMix=shapeMix;p.Heritage.SkinMix=skinMix;Assert.False(p.Valid);Assert.False(GroomingTransactions.Begin(W(),p,120,"bad",Save,out _));}
        [Fact] public void PureHeritageAndFullMixEndpointsAreExplicitValidChoices()
        {var p=H();p.Heritage.ShapeMix=0;p.Heritage.SkinMix=100;p.Heritage.ShapeSecond=p.Heritage.ShapeFirst;Assert.True(p.Valid);var w=W();Begin(w,p);Done(w);Assert.Equal(0,w.Grooming.Looks[0].Heritage.ShapeMix);Assert.Equal(100,w.Grooming.Looks[0].Heritage.SkinMix);}
        [Fact] public void AllTwentyMorphsHeritageAndCosmeticsStillUseOneFee()
        {var p=H();p.Morphs=Enumerable.Range(0,20).Select(i=>new FaceMorph{Index=i,Value=i-10}).ToList();p.Hair=p.Beard=p.Brows=true;p.Overlays.Add(new CosmeticOverlay{Id=4,Style=2});var w=W();Begin(w,p);Done(w);Assert.Equal(20,w.Grooming.Looks[0].Morphs.Count);Assert.Equal(1,w.Grooming.Services);Assert.Equal(120,w.Grooming.Spent);Assert.Equal(880,w.Bank);Assert.Equal(55,w.Cash);}
        [Fact] public void PartialMorphMergePreservesOtherMorphsHeritageAndAllCosmetics()
        {var old=H();old.Morphs=M().Morphs;old.Hair=true;old.HairColor=17;old.Overlays.Add(new CosmeticOverlay{Id=8,Style=3});string before=JsonConvert.SerializeObject(old);var next=GroomingTransactions.Merge(old,M(19,60));Assert.Equal(before,JsonConvert.SerializeObject(old));Assert.Equal(2,next.Morphs.Count);Assert.Equal(-40,next.Morphs[0].Value);Assert.True(next.Heritage.Same(old.Heritage));Assert.Equal(17,next.HairColor);Assert.Equal(8,Assert.Single(next.Overlays).Id);}
        [Fact] public void HeritageOnlyDoesNotDropRecordedMorphsOrTint()
        {var old=M();old.Hair=true;old.HairColor=12;var next=GroomingTransactions.Merge(old,H());Assert.Equal(-40,Assert.Single(next.Morphs).Value);Assert.Equal(12,next.HairColor);Assert.NotNull(next.Heritage);Assert.Null(old.Heritage);}
        [Fact] public void UpdateExistingFaceValuesPreservesUnknownMetadata()
        {var p=H();p.Morphs=M().Morphs;var j=JObject.FromObject(p);j["Heritage"]["Future"]=4;j["Morphs"][0]["Future"]=7;p=j.ToObject<GroomingLook>();var patch=H();patch.Heritage.ShapeMix=90;patch.Morphs=M(0,90).Morphs;var next=GroomingTransactions.Merge(p,patch);j=JObject.FromObject(next);Assert.Equal(4,(int)j["Heritage"]["Future"]);Assert.Equal(7,(int)j["Morphs"][0]["Future"]);Assert.Equal(90,next.Morphs[0].Value);Assert.Equal(90,next.Heritage.ShapeMix);}
        [Fact] public void PendingOwnsDeepCopyOfHeritageAndMorphChoicesAcrossReload()
        {var p=H();p.Morphs=M().Morphs;var w=W();Begin(w,p);p.Heritage.ShapeFirst=99;p.Morphs[0].Value=99;w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.Equal(3,w.Grooming.Pending.Look.Heritage.ShapeFirst);Assert.Equal(-40,w.Grooming.Pending.Look.Morphs[0].Value);Done(w);Assert.Equal(880,w.Bank);}
        [Fact] public void RecordedFaceValuesAreFreeOnlyWhenEverySelectedFieldMatches()
        {var known=H();known.Morphs=M().Morphs;Assert.True(GroomingTransactions.SameSelected(known,H()));Assert.True(GroomingTransactions.SameSelected(known,M()));var patch=H();patch.Heritage.SkinFirst++;Assert.False(GroomingTransactions.SameSelected(known,patch));Assert.False(GroomingTransactions.SameSelected(known,M(0,10)));Assert.False(GroomingTransactions.SameSelected(known,M(1)));}
        [Fact] public void FailedSaveBeforeAuthorizationRefundsAndFailureAfterStartKeepsPaidResume()
        {var w=W();Assert.Throws<Exception>(()=>GroomingTransactions.Begin(w,H(),120,"face",()=>throw new Exception(),out _));Assert.Equal(1000,w.Bank);Assert.Null(w.Grooming);Begin(w,H());Assert.True(GroomingTransactions.Refund(w,"face",Save,out _));Assert.Equal(1000,w.Bank);Begin(w,M(),"morph");Assert.True(GroomingTransactions.Start(w,"morph",Save,out _));Assert.False(GroomingTransactions.Refund(w,"morph",Save,out _));Assert.Throws<Exception>(()=>GroomingTransactions.Complete(w,"morph",()=>throw new Exception(),out _));Assert.True(w.Grooming.Pending.Started);Assert.Empty(w.Grooming.Looks);Done(w,"morph");Assert.Equal(880,w.Bank);}
        [Fact] public void NativeParentRangesAreSortedDeduplicatedAndPermitGaps()
        {Assert.Equal(new[]{0,1,2,21,22,45},FaceRules.Parents(new[]{new FaceHeadRange(21,2),new FaceHeadRange(0,3),new FaceHeadRange(45,1),new FaceHeadRange(1,1),new FaceHeadRange(-1,0)}));}
        [Theory][InlineData(-1,1)][InlineData(0,-1)][InlineData(255,2)][InlineData(0,257)][InlineData(int.MaxValue,int.MaxValue)]
        public void CorruptNativeRangeProducesNoGuessedParents(int first,int count)
        {Assert.Empty(FaceRules.Parents(new[]{new FaceHeadRange(0,3),new FaceHeadRange(first,count)}));}
        [Fact] public void NoNativeParentsMeansNoHeritageApplication()
        {Assert.Empty(FaceRules.Parents(null));Assert.False(FaceRules.Compatible(Heritage(),Array.Empty<int>()));Assert.False(FaceRules.Compatible(Heritage(),new[]{3,21,5}));Assert.True(FaceRules.Compatible(Heritage(),new[]{3,21,5,24}));Assert.Throws<ArgumentException>(()=>FaceRules.CycleParent(0,1,Array.Empty<int>()));}
        [Fact] public void CyclingParentsWrapsGapsAndRecoversUnavailableRecordedIndex()
        {var a=new[]{0,21,45};Assert.Equal(45,FaceRules.CycleParent(0,-1,a));Assert.Equal(0,FaceRules.CycleParent(45,1,a));Assert.Equal(21,FaceRules.CycleParent(0,1,a));Assert.Equal(0,FaceRules.CycleParent(77,-1,a));}
        [Theory][InlineData(95,10,true,100)][InlineData(0,-5,true,0)][InlineData(-95,-10,false,-100)][InlineData(0,-10,false,-10)][InlineData(99,10,false,100)][InlineData(0,int.MinValue,false,-100)][InlineData(100,int.MaxValue,true,100)]
        public void StepClampsMixAndSignedMorphWithoutOverflow(int current,int delta,bool mix,int expected)=>Assert.Equal(expected,FaceRules.Step(current,delta,mix));
        [Theory][InlineData(true,true,true,false,0,FaceWait.Waiting)][InlineData(true,true,true,true,0,FaceWait.Waiting)][InlineData(true,true,true,true,1,FaceWait.Ready)][InlineData(true,true,true,false,9999,FaceWait.Waiting)][InlineData(true,true,true,true,10000,FaceWait.Interrupted)][InlineData(false,true,true,true,1,FaceWait.Interrupted)][InlineData(true,false,true,true,1,FaceWait.Interrupted)][InlineData(true,true,false,true,1,FaceWait.Interrupted)]
        public void AsyncBlendWaitsForNextTickAndCannotFinishAfterCancelOrTimeout(bool owner,bool access,bool visible,bool ready,uint elapsed,FaceWait expected)=>Assert.Equal(expected,FaceRules.Wait(owner,access,visible,ready,elapsed));
        [Fact] public void DifferentProfilesAndModelsNeverShareFaceOrPendingMoney()
        {var a=W();var b=W();Begin(a,H());Assert.Null(b.Grooming);Assert.Equal(1000,b.Bank);var other=M();other.Model=42;Assert.Throws<ArgumentException>(()=>GroomingTransactions.Merge(H(),other));}
    }
}
