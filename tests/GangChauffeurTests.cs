using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangChauffeurTests
    {
        static readonly Action Save=()=>{};static readonly DateTime Utc=new DateTime(2026,10,26,12,0,0,DateTimeKind.Utc);
        static GangTurfDefinition A()=>GangTurfOptions.Defaults().Areas.Single(a=>a.Id==10);static GangState S()=>new GangState{Gang=A().DefaultOwner};static WalletData W()=>new WalletData{Bank=10000,Cash=123};
        static bool Begin(WalletData w,string id="vip",GangChauffeurOptions c=null,Action save=null)=>GangChauffeurTransactions.Begin(w,S(),A(),c??new GangChauffeurOptions(),id,"stinger","a_m_y_business_01",0,0,save??Save,out _);
        static bool Board(WalletData w,Action save=null)=>GangChauffeurTransactions.Board(w,S(),A(),"vip",save??Save,out _);
        static bool Update(WalletData w,int elapsed=10000,float sat=100,float body=1000,Action save=null)=>GangChauffeurTransactions.Update(w,S(),A(),"vip",new GangChauffeurProgress{Elapsed=elapsed,Satisfaction=sat,BodyHealth=body},save??Save,out _);
        static bool Deliver(WalletData w,Action save=null)=>GangChauffeurTransactions.Deliver(w,S(),A(),"vip",save??Save,out _);
        static bool Complete(WalletData w,DateTime? utc=null,Action save=null)=>GangChauffeurTransactions.Complete(w,S(),A(),"vip",utc??Utc,save??Save,out _);
        static bool Cancel(WalletData w,Action save=null)=>GangChauffeurTransactions.Cancel(w,"vip",save??Save,out _);
        static WalletData Started(){var w=W();Assert.True(Begin(w));return w;}
        static WalletData Ready(float sat=100,float body=1000){var w=Started();Assert.True(Board(w));Assert.True(Update(w,sat:sat,body:body));Assert.True(Deliver(w));return w;}
        static GangChauffeurAccount Reward(WalletData w)=>GangChauffeurTransactions.Account(w.GangChauffeur,S().Gang);
        [Fact] public void DefaultsMatchSevenCarsTenPassengerModelsFifteenDestinationsAndSpawns()
        {var c=new GangChauffeurOptions();Assert.True(c.Valid);Assert.Equal(7,c.Vehicles.Count);Assert.Equal(10,c.Passengers.Count);Assert.Equal(15,c.Destinations.Count);Assert.Equal(-83.87819f,c.Spawn.X);Assert.Equal(905.1129f,c.Spawn.Y);Assert.Equal(41.74263f,c.Spawn.Heading);Assert.Equal(-132.5137f,c.Pickup.X);Assert.Equal(160.8435f,c.Pickup.Heading);Assert.Equal("Eclipse Lounge",c.Destinations[0].Name);Assert.Equal("Rockford Hills Tennis Club",c.Destinations[14].Name);Assert.Equal(60,c.BoostMinutes);}
        [Fact] public void StartIsFreeAndDoesNotCreateIncomeOrOptIn()
        {var w=W();w.Bank=0;Assert.True(Begin(w));Assert.Equal(0,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);Assert.Null(w.GangAutoLaunder);Assert.Empty(w.GangChauffeur.Accounts);}
        [Fact] public void StartRequiresOwnedTerritoryTen()
        {var w=W();var a=GangTurfOptions.Defaults().Areas[0];Assert.False(GangChauffeurTransactions.Begin(w,new GangState{Gang=a.DefaultOwner},a,new GangChauffeurOptions(),"bad","stinger","a_m_y_business_01",0,0,Save,out _));Assert.False(GangChauffeurTransactions.Begin(w,new GangState{Gang="Other"},A(),new GangChauffeurOptions(),"bad","stinger","a_m_y_business_01",0,0,Save,out _));Assert.Null(w.GangChauffeur);}
        [Fact] public void InvalidChosenModelsDestinationOrPersonalityCannotStart()
        {var w=W();var c=new GangChauffeurOptions();Assert.False(GangChauffeurTransactions.Begin(w,S(),A(),c,"bad","adder","invalid",0,0,Save,out _));Assert.False(GangChauffeurTransactions.Begin(w,S(),A(),c,"bad","invalid",c.Passengers[0],0,0,Save,out _));Assert.False(GangChauffeurTransactions.Begin(w,S(),A(),c,"bad","adder",c.Passengers[0],15,0,Save,out _));Assert.False(GangChauffeurTransactions.Begin(w,S(),A(),c,"bad","adder",c.Passengers[0],0,3,Save,out _));Assert.Null(w.GangChauffeur);}
        [Fact] public void ContractCapturesModelsDestinationsAndConfiguration()
        {var w=W();var c=new GangChauffeurOptions();Begin(w,c:c);c.Spawn.X=0;c.Pickup.Z=0;c.Destinations[0].Name="edited";c.Vehicles.Clear();c.BoostMinutes=1;Assert.Equal(-83.87819f,w.GangChauffeur.Pending.Options.Spawn.X);Assert.Equal(236.0194f,w.GangChauffeur.Pending.Options.Pickup.Z);Assert.Equal("Eclipse Lounge",w.GangChauffeur.Pending.Options.Destinations[0].Name);Assert.Equal(7,w.GangChauffeur.Pending.Options.Vehicles.Count);Assert.Equal(60,w.GangChauffeur.Pending.Options.BoostMinutes);}
        [Fact] public void LimitFollowsSourceDistanceClampAndLatePassengerReduction()
        {var from=new JobPoint{Name="a",X=100,Y=100,Z=100};var near=new JobPoint{Name="b",X=101,Y=100,Z=100};var far=new JobPoint{Name="c",X=5000,Y=100,Z=100};Assert.Equal(180000,GangChauffeurRules.Limit(from,near,0));Assert.Equal(160000,GangChauffeurRules.Limit(from,near,2));Assert.Equal(300000,GangChauffeurRules.Limit(from,far,1));Assert.Equal(280000,GangChauffeurRules.Limit(from,far,2));Assert.Equal(0,GangChauffeurRules.Limit(null,far,0));}
        [Fact] public void MustBoardAndDeliverBeforeReward()
        {var w=Started();Assert.False(Deliver(w));Assert.False(Complete(w));Assert.True(Board(w));Assert.False(Complete(w));Assert.False(w.GangChauffeur.Pending.Ready);Assert.Empty(w.GangChauffeur.Accounts);}
        [Fact] public void BoardAndDeliveryReplayDoNotSaveAgain()
        {var w=Ready();int saves=0;Assert.True(Board(w,()=>saves++));Assert.True(Deliver(w,()=>saves++));Assert.Equal(0,saves);Assert.True(w.GangChauffeur.Pending.Ready);}
        [Fact] public void FinalRewardAndRemovalUseSingleSaveWithoutMoney()
        {var w=Ready();int saves=0;Assert.True(Complete(w,save:()=>{saves++;Assert.Null(w.GangChauffeur.Pending);Assert.Equal(2,Reward(w).Bonus);}));Assert.Equal(1,saves);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);Assert.Null(w.GangAutoLaunder);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);}
        [Theory] [InlineData(85,850,2)] [InlineData(84,850,1.5)] [InlineData(85,849,1.5)] [InlineData(60,650,1.5)] [InlineData(59,650,1.25)] [InlineData(60,649,1.25)] [InlineData(16,451,1.25)]
        public void RewardUsesSourceSatisfactionAndConditionThresholds(float sat,float body,double expected)
        {var w=Ready(sat,body);Assert.True(Complete(w));Assert.Equal(expected,Reward(w).Bonus);}
        [Fact] public void CompletionReplayNeverRefreshesWindowOrCountsTwice()
        {var w=Ready();Complete(w);var old=w.GangChauffeur;int saves=0;Assert.True(Complete(w,Utc.AddMinutes(5),()=>saves++));Assert.Same(old,w.GangChauffeur);Assert.Equal(0,saves);Assert.Equal(1,Reward(w).Completed);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void SnapshotsCannotHealImproveSatisfactionOrRewindElapsed()
        {var w=Started();Assert.True(Update(w,sat:80,body:900));Assert.False(Update(w,elapsed:9999,sat:80,body:900));Assert.False(Update(w,elapsed:11000,sat:81,body:900));Assert.False(Update(w,elapsed:11000,sat:80,body:901));Assert.Equal(10000,w.GangChauffeur.Pending.Progress.Elapsed);Assert.Equal(80,w.GangChauffeur.Pending.Progress.Satisfaction);}
        [Fact] public void ReloadPreservesChoiceTimeDamageScoreAndBoardingHistory()
        {var w=Started();Board(w);Update(w,elapsed:40000,sat:78,body:890);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(w.GangChauffeur.Valid);Assert.True(w.GangChauffeur.Pending.Boarded);Assert.Equal(40000,w.GangChauffeur.Pending.Progress.Elapsed);Assert.Equal(78,w.GangChauffeur.Pending.Progress.Satisfaction);Assert.Equal(890,w.GangChauffeur.Pending.Progress.BodyHealth);Assert.Equal("stinger",w.GangChauffeur.Pending.Vehicle);Assert.Equal(0,w.GangChauffeur.Pending.Destination);Assert.False(w.GangChauffeur.Pending.Ready);}
        [Fact] public void DeliveredProofReloadFinalizesWithoutNativeHandles()
        {var w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(Ready(75,800)));Assert.True(w.GangChauffeur.Pending.Ready);Assert.True(Complete(w));Assert.Equal(1.5,Reward(w).Bonus);}
        [Theory] [InlineData(15,1000)] [InlineData(100,450)]
        public void FailedConditionPersistsBlocksDeliveryAndCanCancel(float sat,float body)
        {var w=Started();Board(w);Assert.True(Update(w,sat:sat,body:body));Assert.True(w.GangChauffeur.Valid);Assert.True(w.GangChauffeur.Pending.Failed);Assert.False(Deliver(w));Assert.False(Complete(w));Assert.True(Cancel(w));Assert.Empty(w.GangChauffeur.Accounts);}
        [Fact] public void DeadlineFailureIsProtectedAgainstReloadAndRewind()
        {var w=Started();Assert.True(Update(w,elapsed:w.GangChauffeur.Pending.Limit));Assert.True(w.GangChauffeur.Pending.Failed);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(w.GangChauffeur.Pending.Failed);Assert.False(Update(w,elapsed:0));Assert.False(Board(w));Assert.True(Cancel(w));}
        [Fact] public void FailedBeginRestoresAbsentBook()
        {var w=W();Assert.Throws<InvalidOperationException>(()=>Begin(w,save:()=>throw new InvalidOperationException()));Assert.Null(w.GangChauffeur);}
        [Fact] public void FailedSampleRestoresExactScoreAndBook()
        {var w=Started();var old=w.GangChauffeur;Assert.Throws<InvalidOperationException>(()=>Update(w,sat:80,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangChauffeur);Assert.Equal(100,w.GangChauffeur.Pending.Progress.Satisfaction);}
        [Fact] public void FailedBoardAndDeliveryPreservePreviousProofs()
        {var w=Started();var old=w.GangChauffeur;Assert.Throws<InvalidOperationException>(()=>Board(w,()=>throw new InvalidOperationException()));Assert.Same(old,w.GangChauffeur);Assert.False(w.GangChauffeur.Pending.Boarded);Board(w);old=w.GangChauffeur;Assert.Throws<InvalidOperationException>(()=>Deliver(w,()=>throw new InvalidOperationException()));Assert.Same(old,w.GangChauffeur);Assert.False(w.GangChauffeur.Pending.Delivered);}
        [Fact] public void FailedAwardRestoresDeliveredAndPriorBonus()
        {var w=Ready();var old=w.GangChauffeur;Assert.Throws<InvalidOperationException>(()=>Complete(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangChauffeur);Assert.True(w.GangChauffeur.Pending.Ready);Assert.Empty(w.GangChauffeur.Accounts);}
        [Fact] public void CancelIsFreeAndFailedSavePreservesContract()
        {var w=Started();Update(w,sat:50);var old=w.GangChauffeur;Assert.Throws<InvalidOperationException>(()=>Cancel(w,()=>throw new InvalidOperationException()));Assert.Same(old,w.GangChauffeur);Assert.True(Cancel(w));Assert.Null(w.GangChauffeur.Pending);Assert.Equal(10000,w.Bank);Assert.Empty(w.GangChauffeur.Accounts);Assert.True(Cancel(w));}
        [Fact] public void CancelAnotherContractKeepsPriorBonusAndExpiry()
        {var w=Ready(75,800);Complete(w);long expiry=Reward(w).ExpiresUtcTicks;Begin(w,"again");Assert.True(GangChauffeurTransactions.Cancel(w,"again",Save,out _));Assert.Equal(1.5,Reward(w).Bonus);Assert.Equal(expiry,Reward(w).ExpiresUtcTicks);}
        [Fact] public void NewRealCompletionRefreshesQualityWithoutAddingDuration()
        {var w=Ready();Complete(w);Begin(w,"again");GangChauffeurTransactions.Board(w,S(),A(),"again",Save,out _);GangChauffeurTransactions.Update(w,S(),A(),"again",new GangChauffeurProgress{Elapsed=5000,Satisfaction=50,BodyHealth=800},Save,out _);GangChauffeurTransactions.Deliver(w,S(),A(),"again",Save,out _);Assert.True(GangChauffeurTransactions.Complete(w,S(),A(),"again",Utc.AddMinutes(10),Save,out _));Assert.Equal(2,Reward(w).Completed);Assert.Equal(1.25,Reward(w).Bonus);Assert.Equal(Utc.AddMinutes(70).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void BonusUsesStrictUtcGangAreaAndWindow()
        {var w=Ready(75,800);Complete(w);var b=w.GangChauffeur;Assert.Equal(1.5,GangChauffeurTransactions.Multiplier(b,S().Gang.ToUpperInvariant(),10,Utc));Assert.Equal(1,GangChauffeurTransactions.Multiplier(b,S().Gang,10,Utc.AddHours(1)));Assert.Equal(1,GangChauffeurTransactions.Multiplier(b,S().Gang,10,Utc.AddTicks(-1)));Assert.Equal(1,GangChauffeurTransactions.Multiplier(b,"Other",10,Utc));Assert.Equal(1,GangChauffeurTransactions.Multiplier(b,S().Gang,9,Utc));Assert.Equal(1,GangChauffeurTransactions.Multiplier(b,S().Gang,10,DateTime.SpecifyKind(Utc,DateTimeKind.Local)));}
        [Fact] public void LegacyQueriesDoNotCreateBonusOrBook()
        {var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":100,\"Future\":42}");Assert.Null(w.GangChauffeur);Assert.False(GangChauffeurTransactions.Blocked(w));Assert.Null(GangChauffeurTransactions.Account(w.GangChauffeur,S().Gang));Assert.Equal(1,GangChauffeurTransactions.Multiplier(w.GangChauffeur,S().Gang,10,Utc));Assert.Null(w.GangChauffeur);}
        [Fact] public void UnknownFieldsSurviveSamplesAndFutureConfiguration()
        {var w=Started();var j=JObject.FromObject(w);j["Future"]=42;j["GangChauffeur"]["FutureBook"]="keep";j["GangChauffeur"]["Pending"]["FuturePending"]="keep";j["GangChauffeur"]["Pending"]["Progress"]["FutureScore"]=123;j["GangChauffeur"]["Pending"]["Options"]["Destinations"][0]["FuturePoint"]=true;w=j.ToObject<WalletData>();Update(w,sat:80);j=JObject.FromObject(w);Assert.Equal(42,(int)j["Future"]);Assert.Equal("keep",(string)j["GangChauffeur"]["FutureBook"]);Assert.Equal("keep",(string)j["GangChauffeur"]["Pending"]["FuturePending"]);Assert.Equal(123,(int)j["GangChauffeur"]["Pending"]["Progress"]["FutureScore"]);Assert.True((bool)j["GangChauffeur"]["Pending"]["Options"]["Destinations"][0]["FuturePoint"]);}
        [Fact] public void InvalidOptionsAndProgressAreProtectedWithoutExceptions()
        {var w=W();var c=new GangChauffeurOptions();c.Vehicles.Add(c.Vehicles[0]);Assert.False(c.Valid);Assert.False(Begin(w,c:c));w=Started();w.GangChauffeur.Pending.Progress=null;Assert.False(w.GangChauffeur.Valid);Assert.False(Update(w));Assert.False(Cancel(w));}
        [Fact] public void PendingVipBlocksOtherTerritorialOperations()
        {var w=Started();Assert.True(GangBulkTransactions.Blocked(w));Assert.False(GangAutoArmsRules.Free(w));Assert.True(GangChauffeurTransactions.OtherFree(w));Assert.False(GangTheatreTransactions.OtherFree(w));Assert.False(GangMowingTransactions.OtherFree(w));Assert.False(GangBusinessTransactions.Trade(w,S(),A(),new GangBusinessOptions(),false,Utc,"trade",Save,out _));Assert.False(GangAutoLaunderTransactions.Configure(w,S(),A(),true,"auto",Save,out _));}
        [Fact] public void OtherInvalidBookOrPaidContractBlocksStart()
        {var w=W();w.GangTheatre=new GangTheatreData{Accounts=null};Assert.False(Begin(w));w=W();GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"paid",Save,out _);Assert.False(Begin(w));Assert.Equal(9850,w.Bank);}
        [Fact] public void LostOwnershipPreventsProgressAndAwardButNotCancel()
        {var w=Started();var s=new GangState{Gang="Other"};Assert.False(GangChauffeurTransactions.Update(w,s,A(),"vip",new GangChauffeurProgress{Elapsed=1000},Save,out _));Assert.False(GangChauffeurTransactions.Board(w,s,A(),"vip",Save,out _));Assert.False(GangChauffeurTransactions.Complete(w,s,A(),"vip",Utc,Save,out _));Assert.True(Cancel(w));}
        [Fact] public void HistoryOverflowCapacityAndUtcOverflowAreRefused()
        {var w=Ready();Assert.False(Complete(w,DateTime.SpecifyKind(DateTime.MaxValue,DateTimeKind.Utc)));Assert.True(Complete(w));Reward(w).Completed=long.MaxValue;Assert.False(Begin(w,"again"));w=W();w.GangChauffeur=new GangChauffeurData();for(int n=0;n<640;n++)w.GangChauffeur.Accounts.Add(new GangChauffeurAccount{Gang="Other"+n,Completed=1,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks});Assert.True(w.GangChauffeur.Valid);Assert.False(Begin(w));}
        [Fact] public void RepeatedSamplesKeepReceiptsBoundedAndMoneyUnchanged()
        {var w=Started();for(int n=0;n<270;n++)Assert.True(Update(w,elapsed:n));Assert.Equal(256,w.GangChauffeur.Receipts.Count);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);}
        [Fact] public void DeliveredCannotBeCreatedWithoutBoardingOrAfterFailure()
        {var w=Started();w.GangChauffeur.Pending.Delivered=true;Assert.False(w.GangChauffeur.Valid);w.GangChauffeur.Pending.Boarded=true;Assert.True(w.GangChauffeur.Valid);w.GangChauffeur.Pending.Progress.Satisfaction=15;Assert.False(w.GangChauffeur.Valid);}
        [Fact] public void MeterPauseAndLongGapsDoNotSpendTimeOrSatisfaction()
        {var m=new GangChauffeurMeter(0,0,300000,new GangChauffeurProgress());m.Tick(1000,false,900,60,1000,true,false);m.Tick(10000,true,900,60,1000,true,false);Assert.Equal(0,m.Progress.Elapsed);Assert.Equal(100,m.Progress.Satisfaction);Assert.Equal(1000,m.Progress.BodyHealth);}
        [Fact] public void SourceDamagePenaltyScalesForDemandingPassenger()
        {var normal=new GangChauffeurMeter(0,0,300000,new GangChauffeurProgress());var strict=new GangChauffeurMeter(0,1,300000,new GangChauffeurProgress());normal.Tick(1000,true,900,0,10,true,false);strict.Tick(1000,true,900,0,10,true,false);Assert.Equal(88,normal.Progress.Satisfaction);Assert.Equal(82,strict.Progress.Satisfaction);Assert.Equal(900,strict.Progress.BodyHealth);}
        [Fact] public void SourceWaitingPenaltyBeginsAfterThirtySeconds()
        {var normal=new GangChauffeurMeter(0,0,300000,new GangChauffeurProgress{Elapsed=30000});normal.Tick(1000,true,1000,0,100,false,false);Assert.Equal(99.2f,normal.Progress.Satisfaction,3);var strict=new GangChauffeurMeter(0,1,300000,new GangChauffeurProgress{Elapsed=30000});strict.Tick(1000,true,1000,0,100,false,false);Assert.Equal(98.5f,strict.Progress.Satisfaction,3);}
        [Fact] public void SourceOverspeedAndLateIdlePenaltiesDependOnPersonality()
        {var strict=new GangChauffeurMeter(0,1,300000,new GangChauffeurProgress());strict.Tick(1000,true,1000,22,10,true,false);Assert.Equal(97.8f,strict.Progress.Satisfaction,3);var normal=new GangChauffeurMeter(0,0,300000,new GangChauffeurProgress());normal.Tick(1000,true,1000,22,10,true,false);Assert.Equal(100,normal.Progress.Satisfaction);var late=new GangChauffeurMeter(0,2,280000,new GangChauffeurProgress());late.Tick(1000,true,1000,0,100,true,false);Assert.Equal(99.5f,late.Progress.Satisfaction,3);}
        [Fact] public void SourceAbruptDrivingSampleUsesQuarterSecondAndDestinationExemption()
        {var m=new GangChauffeurMeter(0,1,300000,new GangChauffeurProgress());m.Tick(250,true,1000,3,100,true,false);Assert.Equal(98.2f,m.Progress.Satisfaction,3);m.Tick(500,true,1000,0,20,true,false);Assert.Equal(98.2f,m.Progress.Satisfaction,3);m.Tick(750,true,1000,3,100,true,true);Assert.Equal(98.2f,m.Progress.Satisfaction,3);}
        [Fact] public void MeterNeverHealsAcrossResumesOrRepairs()
        {var m=new GangChauffeurMeter(0,0,300000,new GangChauffeurProgress{Elapsed=50000,Satisfaction=75,BodyHealth=800});m.Tick(1000,true,1000,0,10,true,false);Assert.Equal(800,m.Progress.BodyHealth);Assert.Equal(75,m.Progress.Satisfaction);Assert.Equal(51000,m.Progress.Elapsed);}
        [Fact] public void MeterRolloverAndDeadlineAreBounded()
        {int start=int.MaxValue-200;var m=new GangChauffeurMeter(start,0,180000,new GangChauffeurProgress{Elapsed=179500});m.Tick(unchecked(start+1000),true,1000,0,10,true,false);Assert.Equal(180000,m.Progress.Elapsed);}
        [Theory] [InlineData(1.25,48000)] [InlineData(1.5,40000)] [InlineData(2,30000)]
        public void FractionalProcessingClockMatchesSourceBoostDurations(double multiplier,int duration)
        {var c=new GangAutoArmsClock(0);for(int t=1000;t<duration;t+=1000)Assert.False(c.Step(t,true,60,multiplier));Assert.True(c.Step(duration,true,60,multiplier));Assert.Equal(60000,c.Milliseconds);}
        [Fact] public void FractionalClockRetainsSubMillisecondProgressAndRejectsInvalidFactors()
        {var c=new GangAutoArmsClock(0);for(int t=1;t<=4;t++)c.Step(t,true,60,1.25);Assert.Equal(5,c.Milliseconds);Assert.False(c.Step(5,true,60,double.NaN));Assert.False(c.Step(6,true,60,double.PositiveInfinity));Assert.False(c.Step(7,true,60,2.01));Assert.Equal(5,c.Milliseconds);}
        [Fact] public void FractionalClockPauseAndGapDoNotProduceOfflineIncome()
        {var c=new GangAutoArmsClock(0);c.Step(1000,true,60,1.5);c.Step(2000,false,60,1.5);c.Step(20000,true,60,1.5);Assert.Equal(1500,c.Milliseconds);}
    }
}
