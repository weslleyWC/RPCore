using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;

namespace RPCore.Tests
{
    public class FishingTests
    {
        static FishingOptions Options() => FishingDefaults.Load();
        static WalletData Ready(int bank=2000) => new WalletData { Bank=bank, Cash=500, Fishing=new FishingData { RodOwned=true, Bait=5 } };
        static FishingRun Run() => new FishingRun(3,2500,3000);
        static void Land(FishingRun r,int start=0) { Assert.True(r.Cast(start,8000,"sardine",_=>true)); r.Advance(start+8000); Assert.True(r.Press(start+8000)); for(int i=1;i<=3;i++)Assert.True(r.Press(start+8000+i*600)); Assert.Equal(FishingStage.Landed,r.Stage); }
        [Fact] public void DefaultsAreValidAndAllFishHavePricesWeightsAndChances()
        {var c=Options();Assert.True(c.Valid);Assert.Equal(3,c.Spots.Count);Assert.Equal(3,c.Fish.Count);Assert.Equal(350,c.EquipmentPrice);Assert.Equal(10,c.BaitPrice);}
        [Theory] [InlineData(0,"sardine")] [InlineData(54,"sardine")] [InlineData(55,"mackerel")] [InlineData(84,"mackerel")] [InlineData(85,"seabass")] [InlineData(99,"seabass")]
        public void WeightedCatalogUsesStableBoundaries(int roll,string fish) => Assert.Equal(fish,Options().Pick(roll).Id);
        [Theory] [InlineData(-1)] [InlineData(100)]
        public void InvalidRollIsRejected(int roll) => Assert.Throws<ArgumentOutOfRangeException>(()=>Options().Pick(roll));
        [Fact] public void InvalidConfigurationIsRejectedWithoutMutation()
        {var c=Options();c.Fish[0].Weight=float.NaN;var w=Ready();string old=JsonConvert.SerializeObject(w);Assert.False(FishingTransactions.BuyBait(w,c,5,"b",()=>throw new Exception(),out _));Assert.Equal(old,JsonConvert.SerializeObject(w));c=Options();c.WaitMaxMs=2000;Assert.False(c.Valid);c=Options();c.Fish[1].Id=c.Fish[0].Id;Assert.False(c.Valid);}
        [Fact] public void EquipmentIsPermanentBankCheckpointAndReplaySurvivesReload()
        {
            var w=new WalletData { Bank=400,Cash=100 };Assert.True(FishingTransactions.Equipment(w,Options(),"kit",()=>{},out bool changed));Assert.True(changed);Assert.Equal(50,w.Bank);Assert.Equal(100,w.Cash);Assert.True(w.Fishing.RodOwned);
            w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(FishingTransactions.Equipment(w,Options(),"kit",()=>throw new Exception(),out changed));Assert.False(changed);Assert.False(FishingTransactions.Equipment(w,Options(),"kit2",()=>throw new Exception(),out _));
        }
        [Fact] public void InsufficientFundsCannotCreateEquipmentOrBait()
        {var w=new WalletData {Bank=349};Assert.False(FishingTransactions.Equipment(w,Options(),"kit",()=>throw new Exception(),out _));Assert.Null(w.Fishing);w=Ready(0);Assert.False(FishingTransactions.BuyBait(w,Options(),5,"b",()=>throw new Exception(),out _));Assert.Equal(5,w.Fishing.Bait);}
        [Fact] public void EquipmentSaveFailureRestoresNullFieldAndBank()
        {var w=new WalletData { Bank=1000 };var old=JsonConvert.SerializeObject(w);Assert.Throws<InvalidOperationException>(()=>FishingTransactions.Equipment(w,Options(),"kit",()=>throw new InvalidOperationException(),out _));Assert.Equal(old,JsonConvert.SerializeObject(w));}
        [Fact] public void BuyingAndUsingBaitIsAtomicAndReplaySafe()
        {var w=Ready();Assert.True(FishingTransactions.BuyBait(w,Options(),5,"buy",()=>{},out _));Assert.Equal(1950,w.Bank);Assert.Equal(10,w.Fishing.Bait);Assert.True(FishingTransactions.UseBait(w,"use",()=>{},out _));Assert.Equal(9,w.Fishing.Bait);Assert.True(FishingTransactions.UseBait(w,"use",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(9,w.Fishing.Bait);}
        [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(21)]
        public void InvalidBaitQuantityDoesNotCharge(int n)
        {var w=Ready();Assert.False(FishingTransactions.BuyBait(w,Options(),n,"b",()=>throw new Exception(),out _));Assert.Equal(2000,w.Bank);}
        [Fact] public void BaitCannotBeUsedWithoutEquipmentOrStockOrBeyondLimit()
        {var w=Ready();w.Fishing.Bait=999;Assert.False(FishingTransactions.BuyBait(w,Options(),1,"b",()=>{},out _));w.Fishing.Bait=0;Assert.False(FishingTransactions.UseBait(w,"u",()=>{},out _));w.Fishing.RodOwned=false;w.Fishing.Bait=5;Assert.False(FishingTransactions.UseBait(w,"u2",()=>{},out _));}
        [Fact] public void FailedBaitSaveRestoresOldReference()
        {var w=Ready();var old=w.Fishing;Assert.Throws<Exception>(()=>FishingTransactions.UseBait(w,"u",()=>throw new Exception(),out _));Assert.Same(old,w.Fishing);Assert.Equal(5,w.Fishing.Bait);}
        [Fact] public void CatchCheckpointAwardsOnceAndCountsTripOnlyOnLastFish()
        {var w=Ready();Assert.True(FishingTransactions.Catch(w,Options(),"sardine","catch1",false,()=>{},out _));Assert.Equal(1,w.Fishing.Catches);Assert.Equal(0,w.Fishing.Trips);Assert.True(FishingTransactions.Catch(w,Options(),"sardine","catch1",false,()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(1,w.Fishing.Bag["sardine"]);Assert.True(FishingTransactions.Catch(w,Options(),"seabass","catch2",true,()=>{},out _));Assert.Equal(1,w.Fishing.Trips);Assert.Equal(2000,w.Bank);}
        [Fact] public void FullBagRejectsWholeCatchWithoutCounterOrStockMutation()
        {var w=Ready();w.Fishing.Bag["seabass"]=13;var before=JsonConvert.SerializeObject(w);Assert.False(FishingTransactions.Catch(w,Options(),"mackerel","c",true,()=>throw new Exception(),out _));Assert.Equal(before,JsonConvert.SerializeObject(w));}
        [Fact] public void FailedCatchSaveRestoresEntirePreviousStateForRetry()
        {var w=Ready();var old=w.Fishing;Assert.Throws<Exception>(()=>FishingTransactions.Catch(w,Options(),"sardine","c",true,()=>throw new Exception(),out _));Assert.Same(old,w.Fishing);Assert.True(FishingTransactions.Catch(w,Options(),"sardine","c",true,()=>{},out _));Assert.Equal(1,w.Fishing.Catches);}
        [Fact] public void UnknownFishCannotBeMinted()
        {var w=Ready();Assert.False(FishingTransactions.Catch(w,Options(),"unknown","c",false,()=>throw new Exception(),out _));Assert.Empty(w.Fishing.Bag);}
        [Fact] public void SaleCommitsMoneyAndStockInSameCheckpointAndReplayDoesNotPayAgain()
        {
            var w=Ready();w.Fishing.Bag["sardine"]=2;w.Fishing.Bag["seabass"]=1;int calls=0;
            Assert.True(FishingTransactions.Sell(w,Options(),"sale",()=>{calls++;Assert.Equal(2240,w.Bank);Assert.Empty(w.Fishing.Bag);},out bool changed));Assert.True(changed);Assert.Equal(1,calls);Assert.Equal(240,w.Fishing.Earned);
            Assert.True(FishingTransactions.Sell(w,Options(),"sale",()=>throw new Exception(),out changed));Assert.False(changed);Assert.False(FishingTransactions.Sell(w,Options(),"new",()=>throw new Exception(),out _));Assert.Equal(2240,w.Bank);
        }
        [Fact] public void FailedSaleRestoresBankAndGoods()
        {var w=Ready();w.Fishing.Bag["sardine"]=2;string old=JsonConvert.SerializeObject(w);Assert.Throws<Exception>(()=>FishingTransactions.Sell(w,Options(),"s",()=>throw new Exception(),out _));Assert.Equal(old,JsonConvert.SerializeObject(w));}
        [Fact] public void BankOrEarningsOverflowCannotRemoveFish()
        {var w=Ready(int.MaxValue);w.Fishing.Bag["sardine"]=2;Assert.False(FishingTransactions.Sell(w,Options(),"s",()=>throw new Exception(),out _));Assert.Equal(2,w.Fishing.Bag["sardine"]);w.Bank=100;w.Fishing.Earned=long.MaxValue;Assert.False(FishingTransactions.Sell(w,Options(),"s",()=>{},out _));Assert.Equal(100,w.Bank);}
        [Fact] public void OldJobsHuntingLicenseAndUnknownFieldsSurviveFishingSaleAndReload()
        {
            var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":1000,\"Future\":true,\"Hunting\":{\"Licensed\":true,\"Bait\":4,\"Bag\":{\"deer_meat\":2}},\"Jobs\":{\"Jobs\":{\"pilot_training\":{\"CompletedShifts\":1}}},\"Fishing\":{\"RodOwned\":true,\"Bag\":{\"sardine\":2,\"mod_fish\":3},\"FutureFish\":42}}");
            string h=JsonConvert.SerializeObject(w.Hunting),j=JsonConvert.SerializeObject(w.Jobs);Assert.True(FishingTransactions.Sell(w,Options(),"s",()=>{},out _));w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.Equal(1080,w.Bank);Assert.Equal(3,w.Fishing.Bag["mod_fish"]);Assert.Equal(42,(int)w.Fishing.Extra["FutureFish"]);Assert.True((bool)w.Extra["Future"]);Assert.Equal(h,JsonConvert.SerializeObject(w.Hunting));Assert.Equal(j,JsonConvert.SerializeObject(w.Jobs));Assert.True(FlightRules.Qualified(w.Jobs));
        }
        [Fact] public void InvalidSavedFishingDataIsPreservedRatherThanReset()
        {var w=Ready();w.Fishing.Bait=-1;string before=JsonConvert.SerializeObject(w);Assert.False(FishingTransactions.BuyBait(w,Options(),1,"b",()=>{},out _));Assert.Equal(before,JsonConvert.SerializeObject(w));}
        [Fact] public void ReceiptHistoryIsBounded()
        {var w=Ready();w.Fishing.Bait=999;for(int i=0;i<600;i++)Assert.True(FishingTransactions.UseBait(w,"use"+i,()=>{},out _));Assert.Equal(512,w.Fishing.Receipts.Count);Assert.Equal(399,w.Fishing.Bait);}
        [Fact] public void FailedCastCheckpointDoesNotStartTimerOrChangeSequence()
        {var r=Run();Assert.False(r.Cast(0,8000,"sardine",_=>false));Assert.Equal(FishingStage.Ready,r.Stage);Assert.Equal(0,r.Casts);Assert.Throws<Exception>(()=>r.Cast(0,8000,"sardine",_=>throw new Exception()));Assert.Equal(0,r.Casts);}
        [Fact] public void WaitingDoesNotAwardFishOrAcceptEarlyHook()
        {var r=Run();r.Cast(0,8000,"sardine",_=>true);Assert.False(r.Press(7999));Assert.False(r.Confirm((_,_,_)=>throw new Exception()));Assert.Equal(0,r.Caught);Assert.Equal(FishingStage.Waiting,r.Stage);}
        [Fact] public void HookAndThreeSpacedReelsAreRequired()
        {var r=Run();r.Cast(0,8000,"sardine",_=>true);r.Advance(8000);Assert.Equal(FishingStage.Hook,r.Stage);Assert.True(r.Press(8000));Assert.False(r.Press(8599));Assert.True(r.Press(8600));Assert.False(r.Press(8600));Assert.True(r.Press(9200));Assert.True(r.Press(9800));Assert.Equal(FishingStage.Landed,r.Stage);Assert.False(r.Press(10400));Assert.Equal(0,r.Caught);}
        [Fact] public void ExpiredHookCannotBeRevivedByLateFrameOrInput()
        {var r=Run();r.Cast(0,8000,"sardine",_=>true);Assert.False(r.Press(10500));Assert.Equal(FishingStage.Ready,r.Stage);Assert.Equal(0,r.Caught);r.Cast(11000,8000,"sardine",_=>true);Assert.True(r.Advance(22000));Assert.Equal(FishingStage.Ready,r.Stage);}
        [Fact] public void ReelTimeoutLosesFishAndAllowsNextCastWithDistinctReceipt()
        {var r=Run();r.Cast(0,8000,"sardine",_=>true);string first=r.Receipt;r.Press(8000);Assert.False(r.Press(11000));Assert.Equal(FishingStage.Ready,r.Stage);r.Cast(12000,8000,"seabass",_=>true);Assert.NotEqual(first,r.Receipt);Assert.Equal(0,r.Caught);}
        [Fact] public void SaveFailureRetainsSameLandedFishAndReceiptForRetry()
        {var r=Run();Land(r);string receipt=r.Receipt;Assert.False(r.Confirm((_,_,_)=>false));Assert.Equal(FishingStage.Landed,r.Stage);Assert.Throws<Exception>(()=>r.Confirm((_,_,_)=>throw new Exception()));Assert.Equal(receipt,r.Receipt);Assert.Equal("sardine",r.Fish);Assert.True(r.Confirm((fish,id,last)=>fish=="sardine" && id==receipt && !last));Assert.Equal(1,r.Caught);Assert.False(r.Confirm((_,_,_)=>throw new Exception()));}
        [Fact] public void ThirdConfirmedFishCompletesOnceWithoutFinishBonus()
        {var r=Run();var w=Ready();for(int i=0;i<3;i++){Land(r,i*20000);Assert.True(r.Confirm((fish,id,last)=>FishingTransactions.Catch(w,Options(),fish,id,last,()=>{},out _)));}Assert.Equal(FishingStage.Finished,r.Stage);Assert.Equal(3,w.Fishing.Catches);Assert.Equal(1,w.Fishing.Trips);Assert.Equal(2000,w.Bank);Assert.False(r.Confirm((_,_,_)=>throw new Exception()));Assert.False(r.Cast(99999,8000,"sardine",_=>throw new Exception()));}
        [Theory] [InlineData(FishingStage.Waiting)] [InlineData(FishingStage.Hook)] [InlineData(FishingStage.Reeling)]
        public void InterruptResetsCastWithoutCreditingFish(FishingStage stage)
        {var r=Run();r.Cast(0,8000,"sardine",_=>true);if(stage>=FishingStage.Hook)r.Advance(8000);if(stage>=FishingStage.Reeling)r.Press(8000);r.Interrupt();Assert.Equal(FishingStage.Ready,r.Stage);Assert.Null(r.Fish);Assert.Equal(0,r.Caught);}
        [Fact] public void ModalInterruptionCannotEraseLandedPendingCheckpoint()
        {var r=Run();Land(r);r.Interrupt();Assert.Equal(FishingStage.Landed,r.Stage);Assert.True(r.Confirm((_,_,_)=>true));}
        [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
        public void CancelAtEveryActiveStageCannotAwardFish(int phase)
        {var r=Run();if(phase>0)r.Cast(0,8000,"sardine",_=>true);if(phase>1)r.Advance(8000);if(phase>2)r.Press(8000);if(phase>3)for(int i=1;i<=3;i++)r.Press(8000+i*600);r.Cancel();Assert.Equal(FishingStage.Cancelled,r.Stage);Assert.False(r.Confirm((_,_,_)=>throw new Exception()));Assert.Equal(0,r.Caught);}
        [Fact] public void TimingSurvivesGameClockOverflow()
        {var r=Run();int start=int.MaxValue-4000;Land(r,start);Assert.True(r.Confirm((_,_,_)=>true));Assert.Equal(1,r.Caught);}
        [Fact] public void TenActivitiesShareExclusiveShiftGate()
        {var g=new ShiftGate();foreach(var n in new[]{"pizza","postop","uber","valet","supply","tanker","prison","pilot","hunting","fishing"}){Assert.True(g.Acquire(n));Assert.False(g.Acquire("fishing"));g.Release(n);}}
    }
}
