using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RPCore;
using Xunit;
namespace RPCoreTests
{
    public class LeisureRefinementTests
    {
        static WalletData Wallet()=>new WalletData{Bank=77,Exchange=new Portfolio{Cash=1000,Extra=new Dictionary<string,JToken>{{"Future","keep"}}}};
        [Fact] public void ExchangeTradeIsCloneSavedBeforeAssigningAndPreservesOtherAccounts()
        {
            var w=Wallet();var old=w.Exchange;int cash=100;
            Assert.True(ExchangeTransactions.Commit(w,()=>cash,n=>cash=n,0,p=>p.Buy("PIZ",10,25),()=>Assert.NotSame(old,w.Exchange)));
            Assert.Equal(750,w.Exchange.Cash);Assert.Equal(1000,old.Cash);Assert.Equal(100,cash);Assert.Equal(77,w.Bank);Assert.Equal("keep",(string)w.Exchange.Extra["Future"]);
        }
        [Theory][InlineData(true)][InlineData(false)] public void ExchangeFundingFailuresRollBackCashAndExactOriginalPortfolio(bool deposit)
        {
            var w=Wallet();var old=w.Exchange;int cash=100;string before=JsonConvert.SerializeObject(w);
            Assert.Throws<IOException>(()=>ExchangeTransactions.Funds(w,()=>cash,n=>cash=n,50,deposit,()=>throw new IOException()));
            Assert.Same(old,w.Exchange);Assert.Equal(100,cash);Assert.Equal(before,JsonConvert.SerializeObject(w));
        }
        [Fact] public void ExchangeFundingPreservesFractionsAndConservesTotal()
        {
            var w=Wallet();w.Exchange.Cash=1000.25m;int cash=100;decimal total=cash+w.Exchange.Cash;
            Assert.True(ExchangeTransactions.Funds(w,()=>cash,n=>cash=n,1000,false,()=>{}));Assert.Equal(.25m,w.Exchange.Cash);Assert.Equal(total,cash+w.Exchange.Cash);
            Assert.False(ExchangeTransactions.Funds(w,()=>cash,n=>cash=n,1,false,()=>throw new Exception()));Assert.Equal(.25m,w.Exchange.Cash);
        }
        [Fact] public void InsufficientOrOverflowingFundingDoesNotCallSave()
        {
            var w=Wallet();int cash=100;Assert.False(ExchangeTransactions.Funds(w,()=>cash,n=>cash=n,101,true,()=>throw new Exception()));
            cash=int.MaxValue;Assert.False(ExchangeTransactions.Funds(w,()=>cash,n=>cash=n,1,false,()=>throw new Exception()));
            w.Exchange.Cash=decimal.MaxValue;Assert.False(ExchangeTransactions.Funds(w,()=>cash,n=>cash=n,1,true,()=>throw new Exception()));
        }
        [Fact] public void RejectedTradeLeavesTheOriginalEvenIfTheCandidateWasMutated()
        {var w=Wallet();var old=w.Exchange;Assert.False(ExchangeTransactions.Commit(w,()=>0,n=>{},0,p=>{p.Cash=0;return false;},()=>throw new Exception()));Assert.Same(old,w.Exchange);Assert.Equal(1000,w.Exchange.Cash);}
        [Fact] public void MaximumOrdersHandleTinyPricesAndDecimalMaxWithoutOverflow()
        {
            var p=new Portfolio{Cash=decimal.MaxValue};Assert.Equal(int.MaxValue,ExchangeRules.Maximum(p,"tiny",.01m,true));
            p.Holdings["tiny"]=100;Assert.Equal(0,ExchangeRules.Maximum(p,"tiny",.01m,false));
            p.Cash=10;p.AvgCost["tiny"]=.01m;Assert.Equal(1000,ExchangeRules.Maximum(p,"tiny",.01m,true));
        }
        [Fact] public void WeightedAverageDoesNotMultiplyAnExistingLargeCostByShareCount()
        {
            var p=new Portfolio{Cash=100,Holdings={{"BIG",2}},AvgCost={{"BIG",decimal.MaxValue}}};
            Assert.True(p.Buy("BIG",1,1));Assert.Equal(3,p.Holdings["BIG"]);Assert.Equal(99,p.Cash);Assert.True(p.AvgCost["BIG"]>0);
            p=new Portfolio{Cash=100,Holdings={{"unknown",1}}};Assert.False(p.Buy("unknown",1,1));Assert.Equal(100,p.Cash);
        }
        [Fact] public void StaleQuoteProofIncludesFutureMetadataAndHoldings()
        {
            var p=Wallet().Exchange;var expected=JsonConvert.DeserializeObject<Portfolio>(JsonConvert.SerializeObject(p));Assert.True(ExchangeRules.Matches(p,expected));p.Extra["Future"]="changed";Assert.False(ExchangeRules.Matches(p,expected));
        }
        [Fact] public void FirstPurchaseUsesTheQuoteEvenWithAnOldOrphanCostRecord()
        {var p=new Portfolio{Cash=1,AvgCost={{"tiny",decimal.MaxValue}}};Assert.True(p.Buy("tiny",1,.01m));Assert.Equal(.01m,p.AvgCost["tiny"]);Assert.Equal(.99m,p.Cash);}
        [Fact] public void DelistedAndOverflowingPositionsStayOwnedWithoutInventingValuation()
        {
            var p=new Portfolio{Holdings={{"lost",10}}};Assert.Null(ExchangeRules.Value(p,new MarketData()));Assert.Equal(10,p.Holdings["lost"]);
            var market=new MarketData();market.Prices["lost"]=decimal.MaxValue;Assert.Null(ExchangeRules.Value(p,market));Assert.Equal(10,p.Holdings["lost"]);
        }
        [Fact] public void InvalidPortfolioIsNeitherRepairedNorCommitted()
        {var w=Wallet();w.Exchange.Watchlist=null;Assert.False(ExchangeRules.Valid(w.Exchange));Assert.False(ExchangeTransactions.Commit(w,()=>0,n=>{},0,p=>true,()=>throw new Exception()));Assert.Null(w.Exchange.Watchlist);}
        [Fact] public void MarketClockDoesNotMintPausedOfflineOrStalledTicks()
        {
            var clock=new ExchangeClock();Assert.False(clock.Advance(60000,true));Assert.False(clock.Advance(1000,false));for(int i=0;i<59;i++)Assert.False(clock.Advance(1000,true));Assert.True(clock.Advance(1000,true));Assert.False(clock.Advance(0,true));
            Assert.Equal(.01m,ExchangeRules.NextPrice(.01m,.02));Assert.Equal(decimal.MaxValue,ExchangeRules.NextPrice(decimal.MaxValue,.02));Assert.Equal(102m,ExchangeRules.NextPrice(100,.9));
        }
        [Fact] public void ListsCanSearchAndSortByActualQuoteOrGainWithoutHidingUnknownHoldings()
        {
            var p=new Portfolio{Holdings={{"PIZ",2},{"LTD",1},{"OLD",3}},AvgCost={{"PIZ",20},{"LTD",10}}};var m=new MarketData();
            Assert.Equal(new[]{"LTD","PIZ","OLD"},ExchangeRules.List(p,m,p.Holdings.Keys,"",2,true));
            Assert.Equal(new[]{"PIZ","LTD","OLD"},ExchangeRules.List(p,m,p.Holdings.Keys,"",1,false));
            Assert.Equal(new[]{"PIZ"},ExchangeRules.List(p,m,p.Holdings.Keys,"pi",0,false));Assert.Equal(3,p.Holdings["OLD"]);
        }
        static List<RadioStation> Stations(string xml)
        {string path=Path.GetTempFileName();try{File.WriteAllText(path,xml);return RadioRules.ReadStations(path);}finally{File.Delete(path);}}
        [Fact] public void RadioReadsLegacyAttributeAndNamespacedReferenceContractWithoutOverwriting()
        {
            var a=Stations("<RadioSettings><Stations><Station name='Legacy'>https://example.com/live</Station><Station>file:///tmp/a</Station></Stations></RadioSettings>");Assert.Single(a);Assert.Equal("Legacy",a[0].Name);
            var b=Stations("<RadioSettings xmlns='urn:radio'><Stations><RadioStation><Name>Contract</Name><Url>https://example.com/mp3</Url></RadioStation></Stations></RadioSettings>");Assert.Single(b);Assert.Equal("Contract",b[0].Name);
        }
        [Fact] public void RadioRejectsMalformedXmlAndExternalEntities()
        {Assert.ThrowsAny<Exception>(()=>Stations("<RadioSettings>"));Assert.ThrowsAny<Exception>(()=>Stations("<!DOCTYPE x [<!ENTITY ext SYSTEM 'file:///tmp/never'>]><RadioSettings>&ext;</RadioSettings>"));}
        sealed class Playback : IRadioPlayback
        {
            public int State{get;set;}=3;public bool HasError{get;set;}public int Volume{get;set;}
            public int Pauses,Resumes,Stops,Disposed;public bool FailStart,FailStop;
            public void Play(string url){if(FailStart)throw new IOException();}
            public void Pause(){Pauses++;State=2;}public void Resume(){Resumes++;State=3;}
            public void Stop(){Stops++;if(FailStop)throw new IOException();}public void Dispose(){Disposed++;}
        }
        [Fact] public void RadioSwitchDisposesOnlyThePreviousOwnedPlayerAndKeepsVolume()
        {var s=new RadioSession();var a=new Playback();var b=new Playback();Assert.True(s.Start("a",57,()=>a));Assert.True(s.Start("b",70,()=>b));Assert.Equal(1,a.Stops);Assert.Equal(1,a.Disposed);Assert.Equal(0,b.Disposed);Assert.Equal(70,b.Volume);s.Stop();s.Stop();Assert.Equal(1,b.Disposed);}
        [Fact] public void RadioCountsOnlyActualFirstPlaybackNotPauseResumeOrBuffering()
        {
            var s=new RadioSession();var p=new Playback{State=6};s.Start("a",50,()=>p);Assert.False(s.Tick(1000,false));p.State=3;Assert.True(s.Tick(1000,false));Assert.False(s.Tick(1000,false));s.Toggle();Assert.Equal(RadioStatus.Paused,s.Status);s.Toggle();Assert.False(s.Tick(1000,false));
        }
        [Fact] public void GamePauseResumesAutomaticPauseButDoesNotUndoManualPause()
        {
            var s=new RadioSession();var p=new Playback();s.Start("a",50,()=>p);s.Tick(1,false);s.Tick(1,true);Assert.Equal(1,p.Pauses);s.Tick(1,false);Assert.Equal(1,p.Resumes);s.Toggle();s.Tick(1,true);s.Tick(1,false);Assert.Equal(RadioStatus.Paused,s.Status);Assert.Equal(1,p.Resumes);
        }
        [Fact] public void RadioTimeoutUsesActiveDeltasAndReleasesFailedPlayback()
        {
            var s=new RadioSession();var p=new Playback{State=6};s.Start("a",50,()=>p);s.Tick(60000,false);Assert.Equal(RadioStatus.Connecting,s.Status);for(int i=0;i<30;i++)s.Tick(1000,false);Assert.Equal(RadioStatus.Failed,s.Status);Assert.Equal(1,p.Disposed);
        }
        [Fact] public void PlayerStartAndStopFailuresStillReleaseOwnedResources()
        {var s=new RadioSession();var p=new Playback{FailStart=true,FailStop=true};Assert.False(s.Start("a",50,()=>p));Assert.Equal(1,p.Disposed);Assert.Equal(RadioStatus.Failed,s.Status);}
        [Fact] public void EndedRadioDoesNotAutomaticallyStartAnotherUrl()
        {var s=new RadioSession();var p=new Playback();s.Start("a",50,()=>p);s.Tick(1,false);p.State=8;s.Tick(1,false);Assert.Equal(RadioStatus.Stopped,s.Status);Assert.Equal(1,p.Disposed);}
        static AchDef[] Defs()=>new[]{new AchDef{Id="one",Name="One",Stat="earned",Target=10},new AchDef{Id="legacy",Name="Old",Target=1}};
        [Fact] public void AchievementProgressIsDurableBeforeNewUnlockAndPreservesUnknownRecords()
        {
            var d=new AchData{Unlocked={"future"},Extra=new Dictionary<string,JToken>{{"Future",true}}};var old=d;
            var result=AchievementRules.Bump(ref d,Defs(),"earned",10,next=>{Assert.Same(old,d);Assert.Contains("one",next.Unlocked);});
            Assert.Single(result);Assert.Contains("future",d.Unlocked);Assert.True((bool)d.Extra["Future"]);Assert.Empty(AchievementRules.Bump(ref d,Defs(),"earned",1,n=>{}));
        }
        [Fact] public void AchievementSaveFailureLeavesEntireOriginalAndDoesNotUnlock()
        {var d=new AchData();var old=d;Assert.Throws<IOException>(()=>AchievementRules.Bump(ref d,Defs(),"earned",10,n=>throw new IOException()));Assert.Same(old,d);Assert.Empty(d.Stats);Assert.Empty(d.Unlocked);}
        [Fact] public void AchievementFiltersDoNotCountUnknownDuplicateIdsAsKnownCompletion()
        {
            var d=new AchData{Unlocked={"future","future","one"}};Assert.Equal(1,AchievementRules.Completed(Defs(),d));Assert.Single(AchievementRules.Filter(Defs(),d,"legacy"));Assert.Empty(AchievementRules.Filter(Defs(),d,"pending"));Assert.Single(AchievementRules.Filter(Defs(),d,"done"));
        }
        [Fact] public void AchievementCountersSaturateAndInvalidLegacyListsRemainUntouched()
        {
            var d=new AchData{Stats={{"earned",int.MaxValue-1}}};AchievementRules.Bump(ref d,Defs(),"earned",10,n=>{});Assert.Equal(int.MaxValue,d.Stats["earned"]);
            d.Stats=null;Assert.False(AchievementRules.Valid(d));Assert.Empty(AchievementRules.Bump(ref d,Defs(),"earned",1,n=>throw new Exception()));Assert.Null(d.Stats);
        }
    }
}
