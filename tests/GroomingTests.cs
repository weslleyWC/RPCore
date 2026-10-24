using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GroomingTests
    {
        static readonly Action Save=()=>{};
        static WalletData W()=>new WalletData{Bank=1000,Cash=55};
        static GroomingLook Hair(int color=4)=>new GroomingLook{Model=-123,Hair=true,HairColor=color,Highlight=2};
        static GroomingLook Beard()=>new GroomingLook{Model=-123,Beard=true,BeardStyle=2,BeardColor=3,BeardOpacity=80};
        static void Begin(WalletData w,GroomingLook look=null,string id="service")=>Assert.True(GroomingTransactions.Begin(w,look??Hair(),120,id,Save,out _));
        static void Done(WalletData w,string id="service"){Assert.True(GroomingTransactions.Start(w,id,Save,out _));Assert.True(GroomingTransactions.Complete(w,id,Save,out _));}
        [Fact] public void DefaultsAndOldSaveDoNotChargeOrAssignAnyCosmetics()
        {Assert.Equal(120,GroomingDefaults.Load().Price);var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":1000,\"Cash\":55}");Assert.Null(w.Grooming);Assert.Equal(1000,w.Bank);Assert.Empty(new GroomingData().Looks);}
        [Fact] public void ConfirmationReservesFeeAndCompleteAppliesRecordWithoutSecondDebit()
        {var w=W();Begin(w);Assert.Equal(880,w.Bank);Assert.Equal(120,w.Grooming.Spent);Assert.Equal(1,w.Grooming.Services);Assert.False(w.Grooming.Pending.Started);Assert.Empty(w.Grooming.Looks);Done(w);Assert.Null(w.Grooming.Pending);Assert.Equal(4,Assert.Single(w.Grooming.Looks).HairColor);Assert.Equal(880,w.Bank);Assert.Equal(55,w.Cash);}
        [Fact] public void FailedReservationRestoresBankAndNullBook()
        {var w=W();Assert.Throws<Exception>(()=>GroomingTransactions.Begin(w,Hair(),120,"service",()=>throw new Exception(),out _));Assert.Equal(1000,w.Bank);Assert.Null(w.Grooming);}
        [Fact] public void FailedStartKeepsOrderRefundableAndUnstarted()
        {var w=W();Begin(w);Assert.Throws<Exception>(()=>GroomingTransactions.Start(w,"service",()=>throw new Exception(),out _));Assert.False(w.Grooming.Pending.Started);Assert.True(GroomingTransactions.Refund(w,"service",Save,out _));Assert.Equal(1000,w.Bank);}
        [Fact] public void FailedCompletionKeepsPaidStartedOrderForRetryWithoutExtraCharge()
        {var w=W();Begin(w);GroomingTransactions.Start(w,"service",Save,out _);Assert.Throws<Exception>(()=>GroomingTransactions.Complete(w,"service",()=>throw new Exception(),out _));Assert.True(w.Grooming.Pending.Started);Assert.Empty(w.Grooming.Looks);Assert.Equal(880,w.Bank);Assert.True(GroomingTransactions.Complete(w,"service",Save,out _));Assert.Equal(880,w.Bank);}
        [Fact] public void ReloadAndReceiptReplaysCannotChargeRefundOrMergeTwice()
        {var w=W();Begin(w);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(GroomingTransactions.Begin(w,Hair(),120,"service",()=>throw new Exception(),out bool changed));Assert.False(changed);Done(w);Assert.True(GroomingTransactions.Complete(w,"service",()=>throw new Exception(),out changed));Assert.False(changed);Assert.Single(w.Grooming.Looks);Assert.Equal(880,w.Bank);Assert.Equal(1,w.Grooming.Services);}
        [Fact] public void UnstartedCancellationRefundsExactlyOnceAndRemovesServiceCount()
        {var w=W();Begin(w);Assert.True(GroomingTransactions.Refund(w,"service",Save,out _));Assert.Null(w.Grooming.Pending);Assert.Equal(1000,w.Bank);Assert.Equal(0,w.Grooming.Spent);Assert.Equal(0,w.Grooming.Services);Assert.True(GroomingTransactions.Refund(w,"service",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(1000,w.Bank);}
        [Fact] public void RefundFailureRestoresPendingAndBalanceForRetry()
        {var w=W();Begin(w);string before=JsonConvert.SerializeObject(w);Assert.Throws<Exception>(()=>GroomingTransactions.Refund(w,"service",()=>throw new Exception(),out _));Assert.Equal(before,JsonConvert.SerializeObject(w));}
        [Fact] public void StartedOrderCannotRefundButCanBeExplicitlyAbandonedWithoutMoney()
        {var w=W();Begin(w);GroomingTransactions.Start(w,"service",Save,out _);Assert.False(GroomingTransactions.Refund(w,"service",Save,out _));Assert.True(GroomingTransactions.Abandon(w,"service",Save,out _));Assert.Null(w.Grooming.Pending);Assert.Empty(w.Grooming.Looks);Assert.Equal(880,w.Bank);Assert.Equal(120,w.Grooming.Spent);}
        [Fact] public void CannotCompleteBeforeStartOrAbandonRefundableOrder()
        {var w=W();Begin(w);Assert.False(GroomingTransactions.Complete(w,"service",Save,out _));Assert.False(GroomingTransactions.Abandon(w,"service",Save,out _));Assert.NotNull(w.Grooming.Pending);}
        [Fact] public void WrongOrderIdCannotMutateCurrentPending()
        {var w=W();Begin(w);string before=JsonConvert.SerializeObject(w);Assert.False(GroomingTransactions.Start(w,"wrong",Save,out _));Assert.False(GroomingTransactions.Refund(w,"wrong",Save,out _));Assert.False(GroomingTransactions.Complete(w,"wrong",Save,out _));Assert.Equal(before,JsonConvert.SerializeObject(w));}
        [Fact] public void PendingBlocksNewPaidOrderAndInsufficientBankNeverOverdraws()
        {var w=W();w.Bank=119;Assert.False(GroomingTransactions.Begin(w,Hair(),120,"low",Save,out _));Assert.Null(w.Grooming);w.Bank=1000;Begin(w);Assert.False(GroomingTransactions.Begin(w,Beard(),120,"other",Save,out _));Assert.Equal(880,w.Bank);}
        [Fact] public void RefundBankOverflowRetainsFundsAndCanRetryAfterFreeingCapacity()
        {var w=W();Begin(w);w.Bank=int.MaxValue;Assert.False(GroomingTransactions.Refund(w,"service",Save,out _));Assert.NotNull(w.Grooming.Pending);w.Bank-=120;Assert.True(GroomingTransactions.Refund(w,"service",Save,out _));Assert.Equal(int.MaxValue,w.Bank);}
        [Fact] public void ZeroFeeServiceStillHasExactLifecycleWithoutNegativeCounters()
        {var w=W();Assert.True(GroomingTransactions.Begin(w,Hair(),0,"free",Save,out _));Assert.Equal(1000,w.Bank);Assert.True(GroomingTransactions.Refund(w,"free",Save,out _));Assert.Equal(0,w.Grooming.Services);Assert.Equal(0,w.Grooming.Spent);}
        [Fact] public void PartialPatchMergesOnlySelectedGroupsAndPreservesOtherConfirmedFeatures()
        {var old=Hair();old.Brows=true;old.BrowsStyle=7;old.BrowsColor=4;var merged=GroomingTransactions.Merge(old,Beard());Assert.True(merged.Hair);Assert.Equal(4,merged.HairColor);Assert.True(merged.Brows);Assert.Equal(7,merged.BrowsStyle);Assert.True(merged.Beard);Assert.Equal(2,merged.BeardStyle);Assert.False(old.Beard);}
        [Fact] public void SuccessiveServicesRetainSameModelRecordAndUntouchedGroupValues()
        {var w=W();Begin(w);Done(w);Begin(w,Beard(),"beard");Done(w,"beard");Assert.Single(w.Grooming.Looks);Assert.True(w.Grooming.Looks[0].Hair);Assert.True(w.Grooming.Looks[0].Beard);Assert.Equal(760,w.Bank);Assert.Equal(2,w.Grooming.Services);}
        [Fact] public void KnownSelectedValuesDoNotCreateAnotherPaidOrder()
        {var w=W();Begin(w);Done(w);Assert.True(GroomingTransactions.SameSelected(w.Grooming.Looks[0],Hair()));Assert.False(GroomingTransactions.Begin(w,Hair(),120,"same",Save,out _));Assert.Equal(880,w.Bank);Assert.True(GroomingTransactions.Begin(w,Hair(5),120,"newcolor",Save,out _));}
        [Fact] public void NoGroupSelectionAndInvalidColorsStylesOpacityCannotBeCharged()
        {var w=W();var p=Hair();p.Hair=false;Assert.False(p.Valid);Assert.False(GroomingTransactions.Begin(w,p,120,"none",Save,out _));p=Hair();p.HairColor=64;Assert.False(p.Valid);p=Beard();p.BeardStyle=256;Assert.False(p.Valid);p=Beard();p.BeardOpacity=-1;Assert.False(p.Valid);Assert.Equal(1000,w.Bank);}
        [Fact] public void RemovingOverlayIsAnExplicitSelectedNonePatch()
        {var old=Beard();var p=Beard();p.BeardStyle=255;p.BeardOpacity=0;Assert.True(p.Valid);var next=GroomingTransactions.Merge(old,p);Assert.True(next.Beard);Assert.Equal(255,next.BeardStyle);Assert.Equal(0,next.BeardOpacity);Assert.False(next.Hair);}
        [Fact] public void OtherModelsRemainIndependentAndCannotMergeAcrossModels()
        {var other=Hair();other.Model=456;Assert.Throws<ArgumentException>(()=>GroomingTransactions.Merge(Hair(),other));var w=W();Begin(w);Done(w);Begin(w,other,"other");Done(w,"other");Assert.Equal(2,w.Grooming.Looks.Count);}
        [Fact] public void PriceAndPatchAreSnapshotsUntilPendingCompletes()
        {var w=W();var p=Hair();Begin(w,p);p.HairColor=20;Assert.Equal(4,w.Grooming.Pending.Look.HairColor);Assert.Equal(120,w.Grooming.Pending.Price);Done(w);Assert.Equal(4,w.Grooming.Looks[0].HairColor);}
        [Fact] public void UnknownWalletBookPendingAndLookMetadataSurviveCheckpoints()
        {var w=W();Begin(w);var j=JObject.Parse(JsonConvert.SerializeObject(w));j["Future"]=1;j["Grooming"]["Future"]=2;j["Grooming"]["Pending"]["Future"]=3;j["Grooming"]["Pending"]["Look"]["Future"]=4;w=j.ToObject<WalletData>();GroomingTransactions.Start(w,"service",Save,out _);Assert.Equal(3,(int)JObject.Parse(JsonConvert.SerializeObject(w))["Grooming"]["Pending"]["Future"]);GroomingTransactions.Complete(w,"service",Save,out _);j=JObject.Parse(JsonConvert.SerializeObject(w));Assert.Equal(1,(int)j["Future"]);Assert.Equal(2,(int)j["Grooming"]["Future"]);Assert.Equal(4,(int)j["Grooming"]["Looks"][0]["Future"]);}
        [Fact] public void ExistingLookUnknownMetadataAndOtherSystemsRemainUntouched()
        {var w=W();w.Housing=new HousingData();w.Barber=new BarberData();var home=w.Housing;var barber=w.Barber;Begin(w);Done(w);var j=JObject.Parse(JsonConvert.SerializeObject(w));j["Grooming"]["Looks"][0]["Future"]=7;w=j.ToObject<WalletData>();Begin(w,Beard(),"beard");Done(w,"beard");Assert.Equal(7,(int)JObject.Parse(JsonConvert.SerializeObject(w))["Grooming"]["Looks"][0]["Future"]);Assert.Empty(w.Housing.Properties);Assert.Empty(w.Barber.Looks);Assert.Equal(55,w.Cash);}
        [Fact] public void InvalidSavedBookOrCounterOverflowCannotSpendMoney()
        {var w=W();w.Grooming=new GroomingData{Spent=-1};Assert.False(GroomingTransactions.Begin(w,Hair(),120,"invalid",Save,out _));w.Grooming=new GroomingData{Services=int.MaxValue};Assert.False(GroomingTransactions.Begin(w,Hair(),120,"full",Save,out _));Assert.Equal(1000,w.Bank);}
        [Fact] public void ProfileSwitchDoesNotShareAppearanceOrPendingFees()
        {var a=W();var b=W();Begin(a);Assert.Null(b.Grooming);Assert.Equal(1000,b.Bank);Assert.Equal(880,a.Bank);}
    }
}
