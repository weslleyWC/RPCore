using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RPCore;
using Xunit;

namespace RPCoreTests
{
    public class PersonalServiceTests
    {
        static WardrobeOutfit Outfit()=>new WardrobeOutfit{
            Id="one",Name="Social",Model=123,
            Components=WardrobeRules.ComponentSlots.Select(s=>new WardrobePiece{Slot=s}).ToList(),
            Props=WardrobeRules.PropSlots.Select(s=>new WardrobePiece{Slot=s,Drawable=-1}).ToList()
        };
        static T Copy<T>(T value)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));

        [Fact] public void ActivationIsDeferredAndConsumedExactlyOnce()
        {
            var q=new ServiceActionQueue();int calls=0;
            Assert.True(q.Enqueue(()=>calls++,1,123,"Franklin",100));Assert.Equal(0,calls);
            Assert.True(q.Run(true,true,1,123,"Franklin",101));
            Assert.False(q.Run(true,true,1,123,"Franklin",102));Assert.Equal(1,calls);
        }
        [Fact] public void HeldConfirmationCannotRepeatAcrossRebuiltPages()
        {
            var q=new ServiceActionQueue();int calls=0;
            q.Enqueue(()=>{calls++;q.NewPage();},1,123,"F",0);
            q.Run(true,true,1,123,"F",1);q.Release(true);
            Assert.False(q.Enqueue(()=>calls++,1,123,"F",2));
            q.Release(false);Assert.True(q.Enqueue(()=>calls++,1,123,"F",3));
            q.Run(true,true,1,123,"F",4);Assert.Equal(2,calls);
        }
        [Fact] public void OpeningWithAButtonAlreadyHeldWaitsForRelease()
        {
            var q=new ServiceActionQueue();q.Hold();q.NewPage();
            Assert.False(q.Enqueue(()=>{},1,123,"F",0));q.Release(false);
            Assert.True(q.Enqueue(()=>{},1,123,"F",1));
        }
        [Theory]
        [InlineData(false,true,1,123,"F",1)]
        [InlineData(true,false,1,123,"F",1)]
        [InlineData(true,true,2,123,"F",1)]
        [InlineData(true,true,1,456,"F",1)]
        [InlineData(true,true,1,123,"M",1)]
        [InlineData(true,true,1,123,"F",1001)]
        [InlineData(true,true,1,123,"F",-1)]
        public void StaleOrUnsafeServiceActionsAreDiscarded(bool visible,bool allowed,int ped,int model,string profile,int now)
        {
            var q=new ServiceActionQueue();int calls=0;q.Enqueue(()=>calls++,1,123,"F",0);
            Assert.False(q.Run(visible,allowed,ped,model,profile,now));
            Assert.False(q.Run(true,true,1,123,"F",2));Assert.Equal(0,calls);
        }
        [Fact] public void PageReplacementInvalidatesPendingAction()
        {
            var q=new ServiceActionQueue();int version=q.Revision;
            q.Enqueue(()=>throw new Exception("stale"),1,123,"F",0);q.NewPage();
            Assert.NotEqual(version,q.Revision);Assert.False(q.Run(true,true,1,123,"F",1));
        }
        [Fact] public void SecondActivationCannotReplaceAQueuedPurchase()
        {
            var q=new ServiceActionQueue();string result=null;
            q.Enqueue(()=>result="first",1,123,"F",0);q.Release(false);
            Assert.False(q.Enqueue(()=>result="second",1,123,"F",1));
            q.Run(true,true,1,123,"F",2);Assert.Equal("first",result);
        }
        [Fact] public void ThrowingActionCannotRemainQueuedForAnotherCharge()
        {
            var q=new ServiceActionQueue();q.Enqueue(()=>throw new InvalidOperationException(),1,123,"F",0);
            Assert.Throws<InvalidOperationException>(()=>q.Run(true,true,1,123,"F",1));
            Assert.False(q.Run(true,true,1,123,"F",2));
        }
        [Fact] public void NativeGameTimeWrapDoesNotDropAFreshActivation()
        {
            var q=new ServiceActionQueue();int now=int.MaxValue-5;
            q.Enqueue(()=>{},1,123,"F",now);
            Assert.True(q.Run(true,true,1,123,"F",unchecked(now+10)));
        }
        [Theory][InlineData(0,123,"F")][InlineData(1,0,"F")][InlineData(1,123,"")][InlineData(1,123,null)]
        public void InvalidServiceOwnerCannotQueue(int actor,int model,string profile)
        {Assert.False(new ServiceActionQueue().Enqueue(()=>{},actor,model,profile,0));}
        [Fact] public void ClothingLocationsMatchTheFourteenReferenceMarkersAndAreIndependent()
        {
            var c=PersonalServiceRules.ClothingDefaults();Assert.True(c.Valid);Assert.Equal(14,c.Shops.Count);
            Assert.Equal(427.162f,c.Shops[0].X);Assert.Equal(-800.7566f,c.Shops[0].Y);
            Assert.Equal(6515.144f,c.Shops[13].Y);Assert.All(c.Shops,p=>Assert.True(p.Valid));
            c.Shops.Clear();Assert.Equal(14,PersonalServiceRules.ClothingDefaults().Shops.Count);
        }
        [Fact] public void InvalidCustomClothingLocationsAreNotTreatedAsUsable()
        {
            Assert.False(new ClothingOptions().Valid);
            var c=PersonalServiceRules.ClothingDefaults();c.Shops.Add(null);Assert.False(c.Valid);
            c=PersonalServiceRules.ClothingDefaults();c.Shops[0].X=float.NaN;Assert.False(c.Valid);
        }
        [Fact] public void ConfirmationRequiresTheSameCompleteSavedOutfit()
        {
            var current=Outfit();var expected=Copy(current);Assert.True(PersonalServiceRules.Matches(current,expected));
            current.Name="Mudou";Assert.False(PersonalServiceRules.Matches(current,expected));
            current=Copy(expected);current.Components[0].Texture=1;Assert.False(PersonalServiceRules.Matches(current,expected));
            Assert.False(PersonalServiceRules.Matches(null,expected));
        }
        [Fact] public void AutosaveClonesRemainValidButFutureMetadataChangesInvalidateConfirmation()
        {
            var current=Outfit();var j=JObject.Parse(JsonConvert.SerializeObject(current));j["Future"]=42;
            current=j.ToObject<WardrobeOutfit>();var expected=Copy(current);
            Assert.True(PersonalServiceRules.Matches(Copy(current),expected));
            current.Extra["Future"]=43;Assert.False(PersonalServiceRules.Matches(current,expected));
        }
        [Fact] public void PreviewComparisonIgnoresLibraryIdentityAndSlotOrderingButProtectsActualClothes()
        {
            var current=Outfit();var preview=Copy(current);preview.Id="preview";preview.Name="Previa";
            preview.Components.Reverse();preview.Props.Reverse();Assert.True(PersonalServiceRules.SameClothes(current,preview));
            preview.Props[0].Drawable=1;Assert.False(PersonalServiceRules.SameClothes(current,preview));
            preview=Copy(current);preview.Model=456;Assert.False(PersonalServiceRules.SameClothes(current,preview));
        }
        [Theory][InlineData("wardrobe",false)][InlineData("barber",false)][InlineData("grooming",false)]
        [InlineData(null,true)][InlineData("postop",true)][InlineData("characters",true)]
        public void CharacterAutosaveCannotPersistUnconfirmedServicePreviews(string activity,bool allowed)
        {Assert.Equal(allowed,PersonalServiceRules.CanSnapshot(activity));}
        [Theory][InlineData(255,1,3,0)][InlineData(0,-1,3,255)][InlineData(2,1,3,255)]
        [InlineData(255,-1,3,2)][InlineData(255,1,0,255)]
        public void OverlayNoneUsesTheNativeSentinel(int current,int delta,int count,int expected)
        {Assert.Equal(expected,PersonalServiceRules.OverlayStyle(current,delta,count));}
    }
}
