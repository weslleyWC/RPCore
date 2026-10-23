using System;
using Newtonsoft.Json;
using Xunit;

namespace RPCore.Tests
{
    public class StreamerTests
    {
        static StreamerOptions Options()=>StreamerDefaults.Load();
        static WalletData Started(int bank=2000) {var w=new WalletData{Bank=bank,Cash=100};Assert.True(StreamerTransactions.Start(w,"live",()=>{},out _));return w;}
        static StreamReward Normal()=>new StreamReward(StreamContext.Idle,StreamEvent.Normal,1,3,0);
        static bool Block(WalletData w,int step=1,bool last=false)=>StreamerTransactions.Block(w,Options(),Normal(),"live",step,last,()=>{},out _);
        [Fact] public void DefaultsDefineSixThirtySecondBlocks()
        {var c=Options();Assert.True(c.Valid);Assert.Equal(180,c.DurationSeconds);Assert.Equal(30,c.TickSeconds);Assert.Equal(6,c.Blocks);}
        [Theory] [InlineData("RPCoreLive",true)] [InlineData("LS_123",true)] [InlineData("ab",false)] [InlineData("bad channel",false)] [InlineData("~r~test",false)]
        public void ChannelValidationProtectsMenuAndSave(string channel,bool valid)=>Assert.Equal(valid,StreamerText.ChannelValid(channel));
        [Fact] public void TitleRejectsEmptyFormattingCodesAndControlCharacters()
        {Assert.True(StreamerText.TitleValid("Passeio em Los Santos!"));Assert.False(StreamerText.TitleValid(" "));Assert.False(StreamerText.TitleValid("a\nb"));Assert.False(StreamerText.TitleValid("~r~abc"));Assert.False(StreamerText.TitleValid(new string('a',61)));}
        [Fact] public void SettingsPersistAndReplayWithoutChangingMoney()
        {var w=new WalletData{Bank=2000};Assert.True(StreamerTransactions.Settings(w,"CanalLS","Passeio novo","settings",()=>{},out _));w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.Equal("CanalLS",w.Streamer.Channel);Assert.Equal("Passeio novo",w.Streamer.Title);Assert.Equal(2000,w.Bank);Assert.True(StreamerTransactions.Settings(w,"CanalLS","Passeio novo","settings",()=>throw new Exception(),out bool changed));Assert.False(changed);}
        [Fact] public void SettingsFailureRestoresNullField()
        {var w=new WalletData{Bank=100};Assert.Throws<Exception>(()=>StreamerTransactions.Settings(w,"CanalLS","Titulo","s",()=>throw new Exception(),out _));Assert.Null(w.Streamer);}
        [Fact] public void StartIsSavedOnceWithTitleSnapshotAndNoFee()
        {var w=new WalletData{Bank=2000};Assert.True(StreamerTransactions.Start(w,"live",()=>{},out _));Assert.Equal(1,w.Streamer.Sessions);Assert.Equal("Passeio por Los Santos",w.Streamer.Last.Title);Assert.Equal(2000,w.Bank);Assert.True(StreamerTransactions.Start(w,"live",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(1,w.Streamer.Sessions);}
        [Fact] public void FailedStartDoesNotCreateSession()
        {var w=new WalletData{Bank=100};Assert.Throws<Exception>(()=>StreamerTransactions.Start(w,"live",()=>throw new Exception(),out _));Assert.Null(w.Streamer);}
        [Theory] [InlineData(0,StreamEvent.Subscription)] [InlineData(9,StreamEvent.Subscription)] [InlineData(10,StreamEvent.Bits)] [InlineData(29,StreamEvent.Bits)] [InlineData(30,StreamEvent.Donation)] [InlineData(49,StreamEvent.Donation)] [InlineData(50,StreamEvent.Normal)] [InlineData(99,StreamEvent.Normal)]
        public void RewardRollBoundariesAreDeterministic(int roll,StreamEvent kind)
        {var r=StreamerRules.Create(Options(),0,StreamContext.Idle,roll,0,123,0);Assert.Equal(kind,r.Event);Assert.True(r.Valid(Options()));}
        [Theory] [InlineData(0,1)] [InlineData(69,1)] [InlineData(70,2)] [InlineData(94,2)] [InlineData(95,3)]
        public void FollowerRewardsHaveBoundedGrowth(int roll,int expected)=>Assert.Equal(expected,StreamerRules.Create(Options(),0,StreamContext.Working,99,roll,0,0).Followers);
        [Fact] public void ViewersReactToContextAndNeverExceedCap()
        {Assert.True(StreamerRules.Viewers(0,StreamContext.Working,0)>StreamerRules.Viewers(0,StreamContext.Idle,0));Assert.True(StreamerRules.Viewers(0,StreamContext.Driving,0)>StreamerRules.Viewers(0,StreamContext.Idle,0));Assert.Equal(60000,StreamerRules.Viewers(10000000,StreamContext.Working,99));}
        [Fact] public void BlockCreditsBankAndAllStatsInOneCheckpoint()
        {var w=Started();int calls=0;var r=new StreamReward(StreamContext.Working,StreamEvent.Bits,2,10,40);Assert.True(StreamerTransactions.Block(w,Options(),r,"live",1,false,()=>{calls++;Assert.Equal(2045,w.Bank);Assert.Equal(2,w.Streamer.Followers);Assert.Equal(40,w.Streamer.Bits);Assert.Equal(30,w.Streamer.Seconds);Assert.Equal(45,w.Streamer.Last.Income);},out _));Assert.Equal(1,calls);Assert.Equal(100,w.Cash);}
        [Theory] [InlineData(StreamEvent.Normal,0,5)] [InlineData(StreamEvent.Subscription,1,12)] [InlineData(StreamEvent.Bits,10,15)] [InlineData(StreamEvent.Donation,20,25)]
        public void EachEventUsesConfiguredRevenue(StreamEvent kind,int amount,int expected)
        {var w=Started();var r=new StreamReward(StreamContext.Idle,kind,1,3,amount);Assert.True(StreamerTransactions.Block(w,Options(),r,"live",1,false,()=>{},out _));Assert.Equal(2000+expected,w.Bank);Assert.Equal(expected,w.Streamer.Earned);Assert.Equal(kind==StreamEvent.Subscription?1:0,w.Streamer.Subscribers);Assert.Equal(kind==StreamEvent.Donation?amount:0,w.Streamer.Donations);}
        [Fact] public void FailedBlockSaveRestoresBankAndOldReferenceForSameRewardRetry()
        {var w=Started();var old=w.Streamer;string before=JsonConvert.SerializeObject(w);var r=Normal();Assert.Throws<Exception>(()=>StreamerTransactions.Block(w,Options(),r,"live",1,false,()=>throw new Exception(),out _));Assert.Same(old,w.Streamer);Assert.Equal(before,JsonConvert.SerializeObject(w));Assert.True(StreamerTransactions.Block(w,Options(),r,"live",1,false,()=>{},out _));Assert.Equal(2005,w.Bank);}
        [Fact] public void ReplayAfterReloadCannotDuplicateIncomeOrFollowers()
        {var w=Started();Block(w);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(StreamerTransactions.Block(w,Options(),Normal(),"live",1,false,()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(2005,w.Bank);Assert.Equal(1,w.Streamer.Followers);}
        [Fact] public void OutOfOrderWrongSessionAndEarlyCompletionCannotPay()
        {var w=Started();Assert.False(Block(w,2));Assert.False(Block(w,1,true));Assert.False(StreamerTransactions.Block(w,Options(),Normal(),"other",1,false,()=>throw new Exception(),out _));Assert.Equal(2000,w.Bank);}
        [Fact] public void InvalidRewardCannotMintMoney()
        {var w=Started();Assert.False(StreamerTransactions.Block(w,Options(),new StreamReward(StreamContext.Idle,StreamEvent.Donation,1,3,999999),"live",1,false,()=>throw new Exception(),out _));Assert.False(StreamerTransactions.Block(w,Options(),new StreamReward(StreamContext.Idle,StreamEvent.Normal,9999,3,0),"live",1,false,()=>{},out _));Assert.Equal(2000,w.Bank);}
        [Fact] public void SixBlocksCloseSessionOnceWithNoExtraCompletionBonus()
        {var w=Started();for(int i=1;i<=6;i++)Assert.True(Block(w,i,i==6));Assert.Equal(2030,w.Bank);Assert.Equal(6,w.Streamer.Followers);Assert.Equal(180,w.Streamer.Seconds);Assert.Equal(1,w.Streamer.Completed);Assert.True(w.Streamer.Last.Closed);Assert.True(w.Streamer.Last.Completed);Assert.Equal("Tempo concluido",w.Streamer.Last.Reason);Assert.True(Block(w,6,true));Assert.Equal(2030,w.Bank);}
        [Fact] public void ManualStopKeepsPaidBlocksAndCannotPayRemainingTime()
        {var w=Started();Block(w);Assert.True(StreamerTransactions.Stop(w,"live","Encerrada pelo jogador",()=>{},out _));Assert.Equal(2005,w.Bank);Assert.False(Block(w,2));Assert.Equal(0,w.Streamer.Completed);Assert.Equal(30,w.Streamer.Last.Seconds);Assert.True(StreamerTransactions.Stop(w,"live","Encerrada pelo jogador",()=>throw new Exception(),out bool changed));Assert.False(changed);}
        [Fact] public void FailedStopSavePreservesPaidHistoryForRetry()
        {var w=Started();Block(w);string old=JsonConvert.SerializeObject(w);Assert.Throws<Exception>(()=>StreamerTransactions.Stop(w,"live","Encerrada",()=>throw new Exception(),out _));Assert.Equal(old,JsonConvert.SerializeObject(w));Assert.True(StreamerTransactions.Stop(w,"live","Encerrada",()=>{},out _));Assert.Equal(2005,w.Bank);}
        [Fact] public void NewSessionKeepsAggregateStatsWithoutResumingOldRun()
        {var w=Started();Block(w);Assert.True(StreamerTransactions.Start(w,"new",()=>{},out _));Assert.Equal(2,w.Streamer.Sessions);Assert.Equal(1,w.Streamer.Followers);Assert.Equal(5,w.Streamer.Earned);Assert.Equal(0,w.Streamer.Last.Blocks);Assert.False(Block(w,2));}
        [Fact] public void BankOrFollowerOverflowCannotPartiallyAddStats()
        {var w=Started(int.MaxValue);Assert.False(Block(w));Assert.Equal(0,w.Streamer.Followers);w.Bank=100;w.Streamer.Followers=10000000;Assert.False(Block(w));Assert.Equal(100,w.Bank);}
        [Fact] public void InvalidStreamerStateIsPreservedRatherThanReset()
        {var w=Started();w.Streamer.Followers=-1;string old=JsonConvert.SerializeObject(w);Assert.False(Block(w));Assert.Equal(old,JsonConvert.SerializeObject(w));}
        [Fact] public void OldActivitiesAndUnknownFieldsSurviveStreamCredit()
        {
            var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":2000,\"Future\":true,\"Streamer\":{\"FutureStream\":42},\"Hunting\":{\"Licensed\":true},\"Fishing\":{\"RodOwned\":true,\"Bag\":{\"sardine\":2}},\"Crafting\":{\"Stock\":{\"leather\":1}},\"Dealer\":{\"Stock\":{\"weed\":3}},\"Jobs\":{\"Jobs\":{\"pilot_training\":{\"CompletedShifts\":1}}}}");
            string h=JsonConvert.SerializeObject(w.Hunting),f=JsonConvert.SerializeObject(w.Fishing),c=JsonConvert.SerializeObject(w.Crafting),d=JsonConvert.SerializeObject(w.Dealer),j=JsonConvert.SerializeObject(w.Jobs);StreamerTransactions.Start(w,"live",()=>{},out _);Block(w);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.Equal(42,(int)w.Streamer.Extra["FutureStream"]);Assert.True((bool)w.Extra["Future"]);Assert.Equal(h,JsonConvert.SerializeObject(w.Hunting));Assert.Equal(f,JsonConvert.SerializeObject(w.Fishing));Assert.Equal(c,JsonConvert.SerializeObject(w.Crafting));Assert.Equal(d,JsonConvert.SerializeObject(w.Dealer));Assert.Equal(j,JsonConvert.SerializeObject(w.Jobs));Assert.True(FlightRules.Qualified(w.Jobs));
        }
        [Fact] public void InvalidDurationOrEconomicSettingsAreRejected()
        {var c=Options();c.DurationSeconds=181;Assert.False(c.Valid);c=Options();c.BitsValue=-1;Assert.False(c.Valid);Assert.Throws<ArgumentOutOfRangeException>(()=>new StreamerRun(181,30));}
        [Fact] public void TimeIsCountedOnlyWhenGameplayIsEligible()
        {var r=new StreamerRun(180,30);for(int i=0;i<100;i++)r.Advance(1000,false);Assert.Equal(0,r.ElapsedMs);for(int i=0;i<29;i++)r.Advance(1000,true);Assert.False(r.Due);Assert.False(r.Confirm((_,_)=>throw new Exception()));r.Advance(1000,true);Assert.True(r.Due);}
        [Fact] public void LoadingGapCannotGenerateCatchupMoney()
        {var r=new StreamerRun(180,30);r.Advance(600000,true);Assert.Equal(1000,r.ElapsedMs);r.Advance(-100,true);Assert.Equal(1000,r.ElapsedMs);}
        [Fact] public void FailedCheckpointFreezesProgressAndRetainsSameStep()
        {var r=new StreamerRun(180,30);for(int i=0;i<30;i++)r.Advance(1000,true);Assert.False(r.Confirm((step,last)=>false));r.Advance(1000,true);Assert.Equal(30000,r.ElapsedMs);Assert.Throws<Exception>(()=>r.Confirm((_,_)=>throw new Exception()));Assert.True(r.Confirm((step,last)=>step==1 && !last));Assert.Equal(1,r.Blocks);}
        [Fact] public void AllTimedBlocksCompleteOnce()
        {var r=new StreamerRun(180,30);for(int step=1;step<=6;step++){for(int i=0;i<30;i++)r.Advance(1000,true);Assert.True(r.Confirm((n,last)=>n==step && last==(step==6)));}Assert.Equal(StreamerStage.Finished,r.Stage);Assert.False(r.Confirm((_,_)=>throw new Exception()));r.Advance(1000,true);Assert.Equal(180000,r.ElapsedMs);}
        [Fact] public void CancellationDiscardsOnlyUnpaidPartialTime()
        {var r=new StreamerRun(180,30);for(int i=0;i<15;i++)r.Advance(1000,true);r.Cancel();Assert.False(r.Confirm((_,_)=>throw new Exception()));r.Advance(1000,true);Assert.Equal(15000,r.ElapsedMs);}
    }
}
