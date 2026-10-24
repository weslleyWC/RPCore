using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class BarberTests
    {
        static readonly Action Save=()=>{};
        static BarberLook Before(int model=-123)=>new BarberLook{Model=model,Hair=1,Texture=0,Palette=2};
        static BarberLook After(int model=-123)=>new BarberLook{Model=model,Hair=2,Texture=0,Palette=0};
        static WalletData W()=>new WalletData{Bank=1000,Cash=123};
        static bool Pay(WalletData w,string receipt="service",Action save=null)=>BarberTransactions.Confirm(w,Before(),After(),150,receipt,save??Save,out _);
        [Fact] public void DefaultsMatchSevenReferenceShopsAndOwnFixedFee()
        {var c=BarberDefaults.Load();Assert.True(c.Valid);Assert.Equal(7,c.Shops.Count);Assert.Equal(150,c.Price);Assert.Equal(137.8555f,c.Shops[0].X);Assert.Equal(-813.2672f,c.Shops[1].X);Assert.Equal(-277.2849f,c.Shops[6].X);}
        [Fact] public void OldSaveDoesNotCreateHairOrChargeAutomatically()
        {var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":1000,\"Cash\":123}");Assert.Null(w.Barber);Assert.Equal(1000,w.Bank);Assert.Empty(new BarberData().Looks);}
        [Fact] public void ConfirmChargesBankAndSavesOnlyAppearanceRecord()
        {var w=W();w.Housing=new HousingData();w.Wardrobe=new WardrobeData();var house=w.Housing;var clothes=w.Wardrobe;Assert.True(Pay(w));Assert.Equal(850,w.Bank);Assert.Equal(123,w.Cash);Assert.Same(house,w.Housing);Assert.Same(clothes,w.Wardrobe);Assert.Equal(150,w.Barber.Spent);Assert.Equal(1,w.Barber.Services);Assert.Equal(2,Assert.Single(w.Barber.Looks).Hair);}
        [Fact] public void SavedLookIsDeepCopiedAndOriginalInputRemainsIntact()
        {var w=W();var after=After();Assert.True(BarberTransactions.Confirm(w,Before(),after,150,"service",Save,out _));after.Hair=7;Assert.Equal(2,w.Barber.Looks[0].Hair);}
        [Fact] public void FailedSaveRestoresBankAndNullOriginalBook()
        {var w=W();string before=JsonConvert.SerializeObject(w);Assert.Throws<Exception>(()=>Pay(w,save:()=>throw new Exception()));Assert.Equal(before,JsonConvert.SerializeObject(w));Assert.Null(w.Barber);}
        [Fact] public void ReloadAndReceiptReplayNeverChargeOrCountTwice()
        {var w=W();Assert.True(Pay(w));w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(BarberTransactions.Confirm(w,Before(),After(),150,"service",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(850,w.Bank);Assert.Equal(1,w.Barber.Services);Assert.Single(w.Barber.Looks);}
        [Fact] public void UnchangedPreviewDoesNotChargeOrCreateRecord()
        {var w=W();Assert.False(BarberTransactions.Confirm(w,Before(),Before(),150,"none",()=>throw new Exception(),out _));Assert.Equal(1000,w.Bank);Assert.Null(w.Barber);}
        [Theory] [InlineData(0)] [InlineData(149)]
        public void InsufficientBankDoesNotSaveOrOverdraw(int bank)
        {var w=W();w.Bank=bank;Assert.False(Pay(w));Assert.Equal(bank,w.Bank);Assert.Null(w.Barber);}
        [Fact] public void ExactBalanceAndFreeConfiguredServiceAreSupported()
        {var w=W();w.Bank=150;Assert.True(Pay(w));Assert.Equal(0,w.Bank);w=W();Assert.True(BarberTransactions.Confirm(w,Before(),After(),0,"free",Save,out _));Assert.Equal(1000,w.Bank);Assert.Equal(0,w.Barber.Spent);Assert.Equal(1,w.Barber.Services);}
        [Fact] public void DifferentModelsAndDifferentEyeSupportCannotCrossCommit()
        {var w=W();Assert.False(BarberTransactions.Confirm(w,Before(),After(456),150,"model",Save,out _));var a=After();a.HasEye=true;a.Eye=3;Assert.False(BarberTransactions.Confirm(w,Before(),a,150,"eye",Save,out _));Assert.Null(w.Barber);}
        [Fact] public void HairTexturePaletteAndSupportedEyeAllParticipateInChangeDetection()
        {var b=Before();var a=Before();a.Texture=1;Assert.False(b.Same(a));a=Before();a.Palette=0;Assert.False(b.Same(a));b.HasEye=true;b.Eye=1;a=Before();a.HasEye=true;a.Eye=2;var w=W();Assert.True(BarberTransactions.Confirm(w,b,a,150,"eyes",Save,out _));Assert.Equal(2,w.Barber.Looks[0].Eye);}
        [Fact] public void ASecondServiceUpdatesSameModelWithoutDuplicatingRecord()
        {var w=W();Pay(w);var b=After();var a=After();a.Hair=3;Assert.True(BarberTransactions.Confirm(w,b,a,150,"second",Save,out _));Assert.Single(w.Barber.Looks);Assert.Equal(3,w.Barber.Looks[0].Hair);Assert.Equal(2,w.Barber.Services);Assert.Equal(700,w.Bank);}
        [Fact] public void OtherModelsKeepTheirOwnSavedLook()
        {var w=W();Pay(w);Assert.True(BarberTransactions.Confirm(w,Before(456),After(456),150,"other",Save,out _));Assert.Equal(2,w.Barber.Looks.Count);Assert.Equal(-123,w.Barber.Looks[0].Model);Assert.Equal(456,w.Barber.Looks[1].Model);}
        [Fact] public void ExistingDataAndUnknownFieldsSurviveLaterService()
        {var w=W();Pay(w);var j=JObject.Parse(JsonConvert.SerializeObject(w));j["Future"]=1;j["Barber"]["Future"]=2;j["Barber"]["Looks"][0]["Future"]=3;w=j.ToObject<WalletData>();var a=After();a.Texture=1;Assert.True(BarberTransactions.Confirm(w,After(),a,150,"second",Save,out _));j=JObject.Parse(JsonConvert.SerializeObject(w));Assert.Equal(1,(int)j["Future"]);Assert.Equal(2,(int)j["Barber"]["Future"]);Assert.Equal(3,(int)j["Barber"]["Looks"][0]["Future"]);}
        [Fact] public void FailedLaterServiceRestoresExactBookReferenceAndTotals()
        {var w=W();Pay(w);var book=w.Barber;string before=JsonConvert.SerializeObject(w);var a=After();a.Hair=3;Assert.Throws<Exception>(()=>BarberTransactions.Confirm(w,After(),a,150,"second",()=>throw new Exception(),out _));Assert.Same(book,w.Barber);Assert.Equal(before,JsonConvert.SerializeObject(w));}
        [Theory] [InlineData(-1)] [InlineData(10001)]
        public void InvalidFeeCannotChangeWallet(int price)
        {var w=W();Assert.False(BarberTransactions.Confirm(w,Before(),After(),price,"bad",Save,out _));Assert.Equal(1000,w.Bank);}
        [Fact] public void InvalidModelIndicesPaletteAndUnsupportedEyesAreRejected()
        {var a=After();a.Model=0;Assert.False(a.Valid);a=After();a.Hair=-1;Assert.False(a.Valid);a=After();a.Palette=4;Assert.False(a.Valid);a=After();a.Eye=3;Assert.False(a.Valid);a.HasEye=true;Assert.True(a.Valid);a.Eye=32;Assert.False(a.Valid);}
        [Fact] public void InvalidBookCannotBeOverwritten()
        {var w=W();w.Barber=new BarberData{Spent=-1};Assert.False(Pay(w));Assert.Equal(1000,w.Bank);Assert.Equal(-1,w.Barber.Spent);}
        [Fact] public void CounterOverflowDoesNotChargeOrDiscardSavedLook()
        {var w=W();Pay(w);w.Barber.Services=int.MaxValue;string before=JsonConvert.SerializeObject(w);Assert.False(Pay(w,"full"));Assert.Equal(before,JsonConvert.SerializeObject(w));w.Barber.Services=1;w.Barber.Spent=long.MaxValue-149;Assert.False(Pay(w,"spent"));Assert.Equal(850,w.Bank);}
        [Fact] public void BoundedModelBookStillAllowsUpdatingExistingModel()
        {var w=W();w.Barber=new BarberData();for(int i=1;i<=32;i++)w.Barber.Looks.Add(Before(i));Assert.False(Pay(w));Assert.True(BarberTransactions.Confirm(w,Before(1),After(1),150,"existing",Save,out _));Assert.Equal(32,w.Barber.Looks.Count);}
        [Fact] public void ReceiptHistoryIsBoundedWhileServiceTotalsRemainComplete()
        {var w=W();w.Bank=50000;for(int i=0;i<130;i++)Assert.True(BarberTransactions.Confirm(w,Before(),After(),150,"service"+i,Save,out _));Assert.Equal(128,w.Barber.Receipts.Count);Assert.Equal(130,w.Barber.Services);Assert.Equal(19500,w.Barber.Spent);Assert.Single(w.Barber.Looks);}
        [Fact] public void SeparateProfilesDoNotShareConfirmedAppearance()
        {var a=W();var b=W();Pay(a);Assert.Null(b.Barber);Assert.Equal(1000,b.Bank);}
        [Fact] public void InvalidShopConfigurationIsRejected()
        {var c=BarberDefaults.Load();c.Shops[0].Z=float.NaN;Assert.False(c.Valid);c=BarberDefaults.Load();c.Price=-1;Assert.False(c.Valid);}
    }
}
