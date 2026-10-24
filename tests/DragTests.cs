using System;
using Newtonsoft.Json;
using Xunit;

namespace RPCore.Tests
{
    public class DragTests
    {
        static DragOptions Options()=>DragDefaults.Load();
        static WalletData Entered(int stake=250,int bank=2000)
        {var w=new WalletData{Bank=bank,Cash=100};Assert.True(DragTransactions.Enter(w,Options(),stake,"race",()=>{},out _));return w;}
        static void Green(WalletData w)=>Assert.True(DragTransactions.Green(w,"race",()=>{},out _));
        static DragOptions Straight()
        {var c=Options();c.PlayerStart=new JobPoint{Name="p",X=0,Y=0,Z=0};c.PlayerFinish=new JobPoint{Name="pf",X=400,Y=0,Z=0};c.RivalStart=new JobPoint{Name="r",X=0,Y=10,Z=0};c.RivalFinish=new JobPoint{Name="rf",X=400,Y=10,Z=0};return c;}
        static DragRun Racing()
        {var r=new DragRun(Straight());r.Countdown(0);Assert.True(r.Green(3000,new DragPosition(0,0,0),new DragPosition(0,10,0),()=>true));return r;}
        [Fact] public void DefaultsUseReferenceGridAndFinish()
        {var c=Options();Assert.True(c.Valid);Assert.Equal(1711.913f,c.PlayerStart.X);Assert.Equal(1275.354f,c.PlayerFinish.X);Assert.InRange(DragGeometry.Length(c.PlayerStart,c.PlayerFinish),400,500);Assert.Contains(0,c.Bets);}
        [Fact] public void InvalidTrackAndBetsAreRejected()
        {var c=Options();c.PlayerFinish=c.PlayerStart;Assert.False(c.Valid);c=Options();c.Bets.Add(250);Assert.False(c.Valid);c=Options();c.AiSpeed=float.NaN;Assert.False(c.Valid);}
        [Fact] public void EntryReservesBankAndPersistsEscrowOnce()
        {var w=Entered();Assert.Equal(1750,w.Bank);Assert.Equal(100,w.Cash);Assert.Equal(250,w.Drag.Pending.Stake);Assert.Equal(1,w.Drag.Entered);Assert.False(w.Drag.Pending.Started);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(DragTransactions.Enter(w,Options(),250,"race",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(1750,w.Bank);}
        [Fact] public void PracticeIsFreeButHasRecoverableSession()
        {var w=Entered(0);Assert.Equal(2000,w.Bank);Assert.Equal(0,w.Drag.Pending.Stake);Green(w);DragTransactions.Record(w,"race",DragOutcome.Win,12000,()=>{},out _);Assert.True(DragTransactions.Settle(w,"race",()=>{},out _));Assert.Equal(2000,w.Bank);Assert.Equal(1,w.Drag.Wins);}
        [Fact] public void FailedEntryRestoresNullDataAndBank()
        {var w=new WalletData{Bank=2000};string old=JsonConvert.SerializeObject(w);Assert.Throws<Exception>(()=>DragTransactions.Enter(w,Options(),250,"r",()=>throw new Exception(),out _));Assert.Equal(old,JsonConvert.SerializeObject(w));}
        [Fact] public void InsufficientFundsOrUnlistedStakeCannotCreateEscrow()
        {var w=new WalletData{Bank=100};Assert.False(DragTransactions.Enter(w,Options(),250,"r",()=>throw new Exception(),out _));Assert.False(DragTransactions.Enter(w,Options(),123,"r",()=>{},out _));Assert.Null(w.Drag);}
        [Fact] public void ExistingEscrowBlocksASecondEntry()
        {var w=Entered();Assert.False(DragTransactions.Enter(w,Options(),500,"second",()=>throw new Exception(),out _));Assert.Equal(1750,w.Bank);Assert.Equal("race",w.Drag.Pending.Id);}
        [Fact] public void GreenCheckpointIsRetryableWithoutAnotherCharge()
        {var w=Entered();Assert.Throws<Exception>(()=>DragTransactions.Green(w,"race",()=>throw new Exception(),out _));Assert.False(w.Drag.Pending.Started);Green(w);Assert.True(w.Drag.Pending.Started);Assert.True(DragTransactions.Green(w,"race",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(1750,w.Bank);}
        [Theory] [InlineData(DragOutcome.Win,2250)] [InlineData(DragOutcome.Loss,1750)] [InlineData(DragOutcome.Tie,2000)] [InlineData(DragOutcome.Abandoned,1750)] [InlineData(DragOutcome.Void,2000)]
        public void ResultsSettleCorrectReservedAmount(DragOutcome outcome,int bank)
        {var w=Entered();Green(w);Assert.True(DragTransactions.Record(w,"race",outcome,12000,()=>{},out _));Assert.Equal(1750,w.Bank);Assert.True(DragTransactions.Settle(w,"race",()=>{},out _));Assert.Equal(bank,w.Bank);Assert.Null(w.Drag.Pending);Assert.Equal(outcome.ToString(),w.Drag.LastResult);}
        [Fact] public void CancelBeforeGreenRefundsButFalseStartForfeits()
        {var w=Entered();Assert.True(DragTransactions.Record(w,"race",DragOutcome.Cancelled,0,()=>{},out _));DragTransactions.Settle(w,"race",()=>{},out _);Assert.Equal(2000,w.Bank);Assert.Equal(1,w.Drag.Cancelled);w=Entered();Assert.True(DragTransactions.Record(w,"race",DragOutcome.FalseStart,0,()=>{},out _));DragTransactions.Settle(w,"race",()=>{},out _);Assert.Equal(1750,w.Bank);Assert.Equal(1,w.Drag.FalseStarts);Assert.Equal(1,w.Drag.Losses);}
        [Fact] public void CancelAfterGreenCannotClaimARefund()
        {var w=Entered();Green(w);Assert.False(DragTransactions.Record(w,"race",DragOutcome.Cancelled,0,()=>throw new Exception(),out _));Assert.True(DragTransactions.Record(w,"race",DragOutcome.Abandoned,0,()=>{},out _));DragTransactions.Settle(w,"race",()=>{},out _);Assert.Equal(1750,w.Bank);}
        [Fact] public void WinCannotBeRecordedBeforeGreen()
        {var w=Entered();Assert.False(DragTransactions.Record(w,"race",DragOutcome.Win,10000,()=>throw new Exception(),out _));Assert.False(DragTransactions.Settle(w,"race",()=>throw new Exception(),out _));Assert.NotNull(w.Drag.Pending);}
        [Fact] public void FailedResultWriteLeavesUnmodifiedEscrow()
        {var w=Entered();Green(w);var old=w.Drag;Assert.Throws<Exception>(()=>DragTransactions.Record(w,"race",DragOutcome.Win,12000,()=>throw new Exception(),out _));Assert.Same(old,w.Drag);Assert.Equal(DragOutcome.None,w.Drag.Pending.Result);}
        [Fact] public void ResultIsPersistedBeforePayoutAndSurvivesReload()
        {var w=Entered();Green(w);DragTransactions.Record(w,"race",DragOutcome.Win,12000,()=>{},out _);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.Equal(DragOutcome.Win,w.Drag.Pending.Result);Assert.True(DragTransactions.Settle(w,"race",()=>{},out _));Assert.Equal(2250,w.Bank);Assert.Equal(12000,w.Drag.BestMs);}
        [Fact] public void FailedPayoutRestoresReservedWinnerForRetry()
        {var w=Entered();Green(w);DragTransactions.Record(w,"race",DragOutcome.Win,12000,()=>{},out _);string old=JsonConvert.SerializeObject(w);Assert.Throws<Exception>(()=>DragTransactions.Settle(w,"race",()=>throw new Exception(),out _));Assert.Equal(old,JsonConvert.SerializeObject(w));Assert.True(DragTransactions.Settle(w,"race",()=>{},out _));Assert.Equal(2250,w.Bank);}
        [Fact] public void PayoutReplayCannotReturnStakeOrCountWinTwice()
        {var w=Entered();Green(w);DragTransactions.Record(w,"race",DragOutcome.Win,12000,()=>{},out _);DragTransactions.Settle(w,"race",()=>{},out _);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(DragTransactions.Settle(w,"race",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(2250,w.Bank);Assert.Equal(1,w.Drag.Wins);Assert.Equal(500,w.Drag.Returned);}
        [Fact] public void BankOverflowKeepsRecordedWinnerPending()
        {var w=Entered();Green(w);DragTransactions.Record(w,"race",DragOutcome.Win,12000,()=>{},out _);w.Bank=int.MaxValue;Assert.False(DragTransactions.Settle(w,"race",()=>throw new Exception(),out _));Assert.Equal(DragOutcome.Win,w.Drag.Pending.Result);w.Bank=0;Assert.True(DragTransactions.Settle(w,"race",()=>{},out _));Assert.Equal(500,w.Bank);}
        [Theory] [InlineData(false,2000)] [InlineData(true,1750)]
        public void InterruptedSessionRecoveryRefundsOnlyBeforeSignal(bool started,int bank)
        {var w=Entered();if(started)Green(w);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(DragTransactions.Record(w,"race",w.Drag.Pending.Started?DragOutcome.Abandoned:DragOutcome.Cancelled,0,()=>{},out _));Assert.True(DragTransactions.Settle(w,"race",()=>{},out _));Assert.Equal(bank,w.Bank);}
        [Fact] public void UnknownFieldsAndPriorActivitiesArePreserved()
        {var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":2000,\"Future\":true,\"Drag\":{\"FutureDrag\":42},\"Hunting\":{\"Licensed\":true},\"Fishing\":{\"RodOwned\":true},\"Crafting\":{\"Stock\":{\"leather\":1}},\"Dealer\":{\"Stock\":{\"weed\":2}},\"Streamer\":{\"Followers\":42},\"Jobs\":{\"Jobs\":{\"pilot_training\":{\"CompletedShifts\":1}}}}");string h=JsonConvert.SerializeObject(w.Hunting),f=JsonConvert.SerializeObject(w.Fishing),c=JsonConvert.SerializeObject(w.Crafting),d=JsonConvert.SerializeObject(w.Dealer),s=JsonConvert.SerializeObject(w.Streamer),j=JsonConvert.SerializeObject(w.Jobs);DragTransactions.Enter(w,Options(),250,"race",()=>{},out _);Green(w);DragTransactions.Record(w,"race",DragOutcome.Win,12000,()=>{},out _);DragTransactions.Settle(w,"race",()=>{},out _);Assert.Equal(42,(int)w.Drag.Extra["FutureDrag"]);Assert.True((bool)w.Extra["Future"]);Assert.Equal(h,JsonConvert.SerializeObject(w.Hunting));Assert.Equal(f,JsonConvert.SerializeObject(w.Fishing));Assert.Equal(c,JsonConvert.SerializeObject(w.Crafting));Assert.Equal(d,JsonConvert.SerializeObject(w.Dealer));Assert.Equal(s,JsonConvert.SerializeObject(w.Streamer));Assert.Equal(j,JsonConvert.SerializeObject(w.Jobs));Assert.True(FlightRules.Qualified(w.Jobs));}
        [Fact] public void InvalidSavedEscrowIsNotReset()
        {var w=Entered();w.Drag.Pending.Stake=-1;string old=JsonConvert.SerializeObject(w);Assert.False(DragTransactions.Green(w,"race",()=>{},out _));Assert.False(DragTransactions.Settle(w,"race",()=>{},out _));Assert.Equal(old,JsonConvert.SerializeObject(w));}
        [Fact] public void FinishPlaneDetectsOvershootWithInterpolatedFraction()
        {var c=Straight();double? t=DragGeometry.Crossing(c.PlayerStart,c.PlayerFinish,new DragPosition(390,0,0),new DragPosition(410,0,0));Assert.True(t.HasValue);Assert.Equal(.5,t.Value,8);}
        [Theory] [InlineData(20,0)] [InlineData(0,10)]
        public void WrongLaneOrFlyingDoesNotCrossFinish(double side,double height)
        {var c=Straight();Assert.Null(DragGeometry.Crossing(c.PlayerStart,c.PlayerFinish,new DragPosition(390,side,height),new DragPosition(410,side,height)));}
        [Fact] public void ReverseCrossingOrAlreadyPastFinishCannotWin()
        {var c=Straight();Assert.Null(DragGeometry.Crossing(c.PlayerStart,c.PlayerFinish,new DragPosition(410,0,0),new DragPosition(390,0,0)));Assert.Null(DragGeometry.Crossing(c.PlayerStart,c.PlayerFinish,new DragPosition(410,0,0),new DragPosition(420,0,0)));}
        [Fact] public void GreenRequiresCountdownAndConfirmedCheckpoint()
        {var r=new DragRun(Straight());var p=new DragPosition(0,0,0);Assert.False(r.Green(3000,p,p,()=>throw new Exception()));r.Countdown(0);Assert.False(r.Green(2999,p,p,()=>throw new Exception()));Assert.False(r.Green(3000,p,p,()=>false));Assert.Equal(DragStage.Countdown,r.Stage);Assert.True(r.Green(8000,p,p,()=>true));}
        [Fact] public void FirstPlayerCrossingWinsWithoutWaitingForNpcTeleport()
        {var r=Racing();for(int i=1;i<=20;i++)r.Sample(3000+i*500,new DragPosition(i*20,0,0),new DragPosition(i*15,10,0));Assert.Equal(DragOutcome.Win,r.Result);Assert.Equal(10000,r.TimeMs);}
        [Fact] public void FirstNpcCrossingLosesAndDoesNotSetFakePlayerTime()
        {var r=Racing();for(int i=1;i<=20;i++)r.Sample(3000+i*500,new DragPosition(i*15,0,0),new DragPosition(i*20,10,0));Assert.Equal(DragOutcome.Loss,r.Result);Assert.Equal(0,r.TimeMs);}
        [Fact] public void SameFrameCrossingsUseFractionsInsteadOfTickOrder()
        {var r=Racing();for(int i=1;i<=19;i++)r.Sample(3000+i*500,new DragPosition(i*20,0,0),new DragPosition(i*20,10,0));r.Sample(13000,new DragPosition(410,0,0),new DragPosition(420,10,0));Assert.Equal(DragOutcome.Loss,r.Result);}
        [Fact] public void NearSimultaneousCrossingsAreATie()
        {var r=Racing();for(int i=1;i<=20;i++)r.Sample(3000+i*500,new DragPosition(i*20,0,0),new DragPosition(i*20,10,0));Assert.Equal(DragOutcome.Tie,r.Result);}
        [Fact] public void TeleportAndCorridorDepartureCannotWin()
        {var r=Racing();r.Sample(3100,new DragPosition(410,0,0),new DragPosition(0,10,0));Assert.Equal(DragOutcome.Abandoned,r.Result);r=Racing();r.Sample(3100,new DragPosition(0,21,0),new DragPosition(0,10,0));Assert.Equal(DragOutcome.Abandoned,r.Result);}
        [Fact] public void RivalTeleportOrTimeoutVoidsAndRefundsInsteadOfInventingWinner()
        {var r=Racing();r.Sample(3100,new DragPosition(0,0,0),new DragPosition(410,10,0));Assert.Equal(DragOutcome.Void,r.Result);r=Racing();r.Sample(93000,new DragPosition(0,0,0),new DragPosition(0,10,0));Assert.Equal(DragOutcome.Void,r.Result);}
        [Fact] public void FalseStartAndSavedResultCannotBeReplacedByCancellation()
        {var r=new DragRun(Straight());r.Countdown(0);r.Finish(DragOutcome.FalseStart);r.Finish(DragOutcome.Cancelled);Assert.Equal(DragOutcome.FalseStart,r.Result);}
        [Fact] public void RaceClockHandlesOverflow()
        {var r=new DragRun(Straight());int start=int.MaxValue-4000;r.Countdown(start);Assert.True(r.Green(unchecked(start+3000),new DragPosition(0,0,0),new DragPosition(0,10,0),()=>true));for(int i=1;i<=20;i++)r.Sample(unchecked(start+3000+i*500),new DragPosition(i*20,0,0),new DragPosition(i*15,10,0));Assert.Equal(DragOutcome.Win,r.Result);Assert.Equal(10000,r.TimeMs);}
        [Fact] public void PhysicalActivityGateIsExclusiveWhileStreamerRemainsIndependent()
        {var g=new ShiftGate();Assert.True(g.Acquire("drag"));Assert.False(g.Acquire("postop"));var stream=new StreamerRun(180,30);stream.Advance(1000,true);Assert.Equal("drag",g.Owner);g.Release("drag");Assert.True(g.Acquire("postop"));}
    }
}
