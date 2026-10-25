using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class DmvTests
    {
        static readonly Action Save=()=>{};
        static WalletData W()=>new WalletData{Bank=1000,Cash=55};
        static DmvOptions C()=>DmvDefaults.Load();
        static void Theory(WalletData w){Assert.True(DmvTransactions.Begin(w,C(),false,"theory",Save,out _));Assert.True(DmvTransactions.Start(w,"theory",Save,out _));for(int i=0;i<15;i++)Assert.True(DmvTransactions.Answer(w,"theory",i,DmvQuestions.All[i].Correct,Save,out _));}
        static void Practical(WalletData w){Theory(w);Assert.True(DmvTransactions.Begin(w,C(),true,"drive",Save,out _));Assert.True(DmvTransactions.Start(w,"drive",Save,out _));}
        static void Push(WalletData w,DmvDrive state)=>Assert.True(DmvTransactions.Progress(w,"drive",state,Save,out _));
        static DmvDrive Report(WalletData w)
        {var s=new DmvDrive();var c=w.Dmv.Pending.Options;s.Enter(true,true);Push(w,s);s.Signal(true,false,true,true,true,c);Push(w,s);s.Signal(false,true,true,true,true,c);Push(w,s);for(int i=0;i<c.Route.Count;i++){Assert.True(s.Checkpoint(true,true,true,true,true,c));Push(w,s);}s.Park(true);Push(w,s);s.Leave(true);Push(w,s);return s;}
        [Fact] public void DefaultsUseReferenceFees36PointsAndNoAutomaticDocuments()
        {var c=C();Assert.True(c.Valid);Assert.Equal(30,c.TheoryPrice);Assert.Equal(60,c.PracticalPrice);Assert.Equal(36,c.Route.Count);Assert.Equal(-544.5625f,c.Entry.X);Assert.Equal(-485.3018f,c.Spawn.X);Assert.Equal(12,c.PassScore);var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":1000}");Assert.Null(w.Dmv);Assert.Equal(1000,w.Bank);}
        [Fact] public void AllOwnQuestionsHaveFourChoicesAndOneValidCorrectAnswer()
        {Assert.Equal(15,DmvQuestions.All.Length);foreach(var q in DmvQuestions.All){Assert.False(string.IsNullOrWhiteSpace(q.Text));Assert.Equal(4,q.Answers.Length);Assert.Equal(4,q.Answers.Distinct().Count());Assert.InRange(q.Correct,0,3);}Assert.Equal(15,DmvQuestions.Score(DmvQuestions.All.Select(q=>q.Correct).ToList()));}
        [Fact] public void TheoryChargesOnceAndSuccessfulExamUnlocksPracticalNotLicense()
        {var w=W();Theory(w);Assert.Equal(970,w.Bank);Assert.Equal(55,w.Cash);Assert.True(w.Dmv.TheoryPassed);Assert.False(w.Dmv.Licensed);Assert.Equal(15,w.Dmv.LastTheoryScore);Assert.Null(w.Dmv.Pending);Assert.Equal(1,w.Dmv.TheoryAttempts);}
        [Theory][InlineData(11,false)][InlineData(12,true)][InlineData(15,true)]
        public void TheoryThresholdIsExact(int correct,bool pass)
        {var w=W();DmvTransactions.Begin(w,C(),false,"theory",Save,out _);DmvTransactions.Start(w,"theory",Save,out _);for(int i=0;i<15;i++){int a=DmvQuestions.All[i].Correct;DmvTransactions.Answer(w,"theory",i,i<correct?a:(a+1)%4,Save,out _);}Assert.Equal(pass,w.Dmv.TheoryPassed);Assert.Equal(correct,w.Dmv.LastTheoryScore);Assert.False(w.Dmv.Licensed);Assert.Equal(970,w.Bank);}
        [Fact] public void PracticalRequiresTheoryAndOwnedDocumentsCannotBePurchasedAgain()
        {var w=W();Assert.False(DmvTransactions.Begin(w,C(),true,"early",Save,out _));Theory(w);Assert.False(DmvTransactions.Begin(w,C(),false,"again",Save,out _));DmvTransactions.Begin(w,C(),true,"drive",Save,out _);DmvTransactions.Start(w,"drive",Save,out _);Report(w);Assert.True(DmvTransactions.Finish(w,"drive",true,Save,out _));Assert.False(DmvTransactions.Begin(w,C(),true,"again",Save,out _));Assert.Equal(910,w.Bank);}
        [Fact] public void All36PointsParkingExitAndReportAreRequiredBeforeLicense()
        {var w=W();Practical(w);Assert.False(DmvTransactions.Finish(w,"drive",true,Save,out _));var state=Report(w);Assert.Equal(36,state.Point);Assert.Equal(DmvStage.Report,state.Stage);Assert.True(DmvTransactions.Finish(w,"drive",true,Save,out _));Assert.True(w.Dmv.Licensed);Assert.True(w.Dmv.TheoryPassed);Assert.Null(w.Dmv.Pending);Assert.Equal(90,w.Dmv.Spent);Assert.Equal(910,w.Bank);}
        [Fact] public void FailedPracticalKeepsTheoryCertificateAndNoLicenseOrRefund()
        {var w=W();Practical(w);Assert.True(DmvTransactions.Finish(w,"drive",false,Save,out _));Assert.True(w.Dmv.TheoryPassed);Assert.False(w.Dmv.Licensed);Assert.Equal(910,w.Bank);Assert.Equal("practical_fail",w.Dmv.LastResult);Assert.True(DmvTransactions.Begin(w,C(),true,"retake",Save,out _));Assert.Equal(850,w.Bank);}
        [Fact] public void FailedBeginAndInsufficientBalanceDoNotChargeOrCreateAttempt()
        {var w=W();w.Bank=29;Assert.False(DmvTransactions.Begin(w,C(),false,"low",Save,out _));Assert.Null(w.Dmv);w.Bank=1000;Assert.Throws<Exception>(()=>DmvTransactions.Begin(w,C(),false,"fail",()=>throw new Exception(),out _));Assert.Null(w.Dmv);Assert.Equal(1000,w.Bank);}
        [Fact] public void StartFailureLeavesPaidAttemptRefundable()
        {var w=W();DmvTransactions.Begin(w,C(),false,"theory",Save,out _);Assert.Throws<Exception>(()=>DmvTransactions.Start(w,"theory",()=>throw new Exception(),out _));Assert.False(w.Dmv.Pending.Started);Assert.True(DmvTransactions.Cancel(w,"theory",Save,out _));Assert.Equal(1000,w.Bank);Assert.Equal(0,w.Dmv.TheoryAttempts);Assert.Equal(0,w.Dmv.Spent);}
        [Fact] public void StartedAttemptHasResumeOrExplicitAbandonWithoutRefund()
        {var w=W();DmvTransactions.Begin(w,C(),false,"theory",Save,out _);DmvTransactions.Start(w,"theory",Save,out _);Assert.False(DmvTransactions.Cancel(w,"theory",Save,out _));Assert.True(DmvTransactions.Abandon(w,"theory",Save,out _));Assert.Equal(970,w.Bank);Assert.Null(w.Dmv.Pending);Assert.False(w.Dmv.TheoryPassed);}
        [Fact] public void RefundingAtBankIntegerLimitKeepsAttemptInsteadOfDiscardingFunds()
        {var w=W();DmvTransactions.Begin(w,C(),false,"theory",Save,out _);w.Bank=int.MaxValue;Assert.False(DmvTransactions.Cancel(w,"theory",Save,out _));Assert.NotNull(w.Dmv.Pending);w.Bank-=30;Assert.True(DmvTransactions.Cancel(w,"theory",Save,out _));Assert.Equal(int.MaxValue,w.Bank);}
        [Fact] public void AnswerIsDurableSequentialAndDuplicateCallbackCannotAdvanceTwice()
        {var w=W();DmvTransactions.Begin(w,C(),false,"theory",Save,out _);DmvTransactions.Start(w,"theory",Save,out _);Assert.False(DmvTransactions.Answer(w,"theory",1,0,Save,out _));Assert.True(DmvTransactions.Answer(w,"theory",0,1,Save,out _));w=CharacterRules.Copy(w);Assert.True(DmvTransactions.Answer(w,"theory",0,1,()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Single(w.Dmv.Pending.Answers);Assert.Equal(970,w.Bank);}
        [Fact] public void AnswerSaveFailurePreservesPreviousAnswersForRetry()
        {var w=W();DmvTransactions.Begin(w,C(),false,"theory",Save,out _);DmvTransactions.Start(w,"theory",Save,out _);Assert.Throws<Exception>(()=>DmvTransactions.Answer(w,"theory",0,1,()=>throw new Exception(),out _));Assert.Empty(w.Dmv.Pending.Answers);Assert.Equal(970,w.Bank);}
        [Fact] public void FinalAnswerSaveFailureDoesNotLosePendingOrIssueCertificate()
        {var w=W();DmvTransactions.Begin(w,C(),false,"theory",Save,out _);DmvTransactions.Start(w,"theory",Save,out _);for(int i=0;i<14;i++)DmvTransactions.Answer(w,"theory",i,DmvQuestions.All[i].Correct,Save,out _);Assert.Throws<Exception>(()=>DmvTransactions.Answer(w,"theory",14,DmvQuestions.All[14].Correct,()=>throw new Exception(),out _));Assert.False(w.Dmv.TheoryPassed);Assert.Equal(14,w.Dmv.Pending.Answers.Count);Assert.True(DmvTransactions.Answer(w,"theory",14,DmvQuestions.All[14].Correct,Save,out _));Assert.True(w.Dmv.TheoryPassed);}
        [Fact] public void ConfigAndPriceAreDeepSnapshotsAcrossPaidResume()
        {var w=W();var c=C();DmvTransactions.Begin(w,c,false,"theory",Save,out _);c.TheoryPrice=500;c.PassScore=1;c.Route[0].X=0;Assert.Equal(30,w.Dmv.Pending.Price);Assert.Equal(12,w.Dmv.Pending.Options.PassScore);Assert.NotEqual(0,w.Dmv.Pending.Options.Route[0].X);}
        [Fact] public void CannotSkipCheckpointOrPreparationStageWithProgressSave()
        {var w=W();Practical(w);Assert.False(DmvTransactions.Progress(w,"drive",new DmvDrive{Stage=DmvStage.Route,Point=3},Save,out _));Assert.False(DmvTransactions.Progress(w,"drive",new DmvDrive{Stage=DmvStage.Report,Point=36},Save,out _));Assert.Equal(DmvStage.AwaitDriver,w.Dmv.Pending.Drive.Stage);}
        [Fact] public void PracticalProgressSaveFailureRollsBackCheckpointAndKeepsPaidAttempt()
        {var w=W();Practical(w);var next=new DmvDrive();next.Enter(true,true);Assert.Throws<Exception>(()=>DmvTransactions.Progress(w,"drive",next,()=>throw new Exception(),out _));Assert.Equal(DmvStage.AwaitDriver,w.Dmv.Pending.Drive.Stage);Assert.Equal(910,w.Bank);Push(w,next);Assert.Equal(DmvStage.RightSignal,w.Dmv.Pending.Drive.Stage);}
        [Fact] public void CannotReduceFaultsElapsedOrCompletedPoints()
        {var w=W();Practical(w);var s=new DmvDrive{Faults=2,Seconds=50};s.Enter(true,true);Push(w,s);var wrong=CharacterRules.Copy(s);wrong.Faults=1;Assert.False(DmvTransactions.Progress(w,"drive",wrong,Save,out _));wrong=CharacterRules.Copy(s);wrong.Seconds=49;Assert.False(DmvTransactions.Progress(w,"drive",wrong,Save,out _));}
        [Fact] public void ResumeRestartsPreparationWithoutResettingFaultsTimeOrCharging()
        {var w=W();Practical(w);var s=new DmvDrive{Faults=2,Seconds=50};s.Enter(true,true);Push(w,s);Assert.True(DmvTransactions.Restart(w,"drive",Save,out _));Assert.Equal(DmvStage.AwaitDriver,w.Dmv.Pending.Drive.Stage);Assert.Equal(2,w.Dmv.Pending.Drive.Faults);Assert.Equal(50,w.Dmv.Pending.Drive.Seconds);Assert.Equal(910,w.Bank);Assert.Equal(1,w.Dmv.PracticalAttempts);}
        [Fact] public void FinishedResultReplayCannotGrantDuplicateOrDebitTwice()
        {var w=W();Practical(w);Report(w);DmvTransactions.Finish(w,"drive",true,Save,out _);Assert.True(DmvTransactions.Finish(w,"drive",true,()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(910,w.Bank);Assert.Equal(1,w.Dmv.PracticalAttempts);}
        [Fact] public void FailedIssueSaveKeepsApprovedReportReadyForRetry()
        {var w=W();Practical(w);Report(w);Assert.Throws<Exception>(()=>DmvTransactions.Finish(w,"drive",true,()=>throw new Exception(),out _));Assert.False(w.Dmv.Licensed);Assert.Equal(DmvStage.Report,w.Dmv.Pending.Drive.Stage);Assert.True(DmvTransactions.Finish(w,"drive",true,Save,out _));}
        [Fact] public void DriverExaminerBeltAndExclusiveSignalAreRequired()
        {var c=C();var s=new DmvDrive();Assert.False(s.Enter(false,true));Assert.False(s.Enter(true,false));Assert.True(s.Enter(true,true));Assert.False(s.Signal(true,true,true,true,true,c));Assert.False(s.Signal(true,false,false,true,true,c));Assert.True(s.Signal(true,false,true,true,true,c));Assert.False(s.Signal(true,false,true,true,true,c));Assert.True(s.Signal(false,true,true,true,true,c));Assert.Equal(DmvStage.Route,s.Stage);}
        [Fact] public void EveryCheckpointRequiresDriverExaminerLocationAndSpeed()
        {var s=new DmvDrive{Stage=DmvStage.Route};var c=C();Assert.False(s.Checkpoint(false,true,true,true,true,c));Assert.False(s.Checkpoint(true,false,true,true,true,c));Assert.False(s.Checkpoint(true,true,false,true,true,c));Assert.False(s.Checkpoint(true,true,true,false,true,c));Assert.True(s.Checkpoint(true,true,true,true,true,c));Assert.Equal(1,s.Point);}
        [Theory][InlineData(4)][InlineData(16)][InlineData(28)]
        public void RequiredStopCannotBeSkippedByDrivingThrough(int point)
        {var c=C();var s=new DmvDrive{Stage=DmvStage.Route,Point=point};Assert.False(s.Checkpoint(true,true,true,true,false,c));Assert.Equal(point,s.Point);Assert.True(s.Checkpoint(true,true,true,true,true,c));Assert.Equal(point+1,s.Point);}
        [Fact] public void RouteEndThenParkingThenExitAreSeparateStages()
        {var c=C();var s=new DmvDrive{Stage=DmvStage.Route,Point=35};Assert.True(s.Checkpoint(true,true,true,true,true,c));Assert.Equal(DmvStage.Parking,s.Stage);Assert.False(s.Park(false));Assert.True(s.Park(true));Assert.False(s.Leave(false));Assert.True(s.Leave(true));Assert.Equal(DmvStage.Report,s.Stage);}
        [Theory][InlineData(4,1199,false)][InlineData(5,100,true)][InlineData(0,1200,true)]
        public void FaultAndTimeBoundariesAreExact(int faults,int seconds,bool failed)=>Assert.Equal(failed,new DmvDrive{Faults=faults,Seconds=seconds}.Failed(C()));
        [Fact] public void ReadyReportWithTooManyFaultsCannotIssueLicense()
        {var w=W();Practical(w);var state=Report(w);state.Faults=5;Push(w,state);Assert.False(DmvTransactions.Finish(w,"drive",true,Save,out _));Assert.False(w.Dmv.Licensed);Assert.NotNull(w.Dmv.Pending);}
        [Fact] public void UnsupportedVersionOrInvalidSavedDocumentsCannotCharge()
        {var w=W();w.Dmv=new DmvData{Licensed=true,TheoryPassed=false};Assert.False(DmvTransactions.Begin(w,C(),false,"bad",Save,out _));w=W();DmvTransactions.Begin(w,C(),false,"theory",Save,out _);w.Dmv.Pending.Version=2;Assert.False(w.Dmv.Valid);Assert.False(DmvTransactions.Start(w,"theory",Save,out _));Assert.Equal(970,w.Bank);}
        [Fact] public void UnknownWalletBookAttemptConfigAndProgressMetadataSurvive()
        {var w=W();Practical(w);var j=JObject.FromObject(w);j["Future"]=1;j["Dmv"]["Future"]=2;j["Dmv"]["Pending"]["Future"]=3;j["Dmv"]["Pending"]["Options"]["Future"]=4;j["Dmv"]["Pending"]["Drive"]["Future"]=5;w=j.ToObject<WalletData>();var state=CharacterRules.Copy(w.Dmv.Pending.Drive);state.Enter(true,true);Push(w,state);j=JObject.FromObject(w);Assert.Equal(1,(int)j["Future"]);Assert.Equal(2,(int)j["Dmv"]["Future"]);Assert.Equal(3,(int)j["Dmv"]["Pending"]["Future"]);Assert.Equal(4,(int)j["Dmv"]["Pending"]["Options"]["Future"]);Assert.Equal(5,(int)j["Dmv"]["Pending"]["Drive"]["Future"]);}
        [Fact] public void WrongAttemptIdAndInvalidAnswersNeverMutate()
        {var w=W();DmvTransactions.Begin(w,C(),false,"theory",Save,out _);DmvTransactions.Start(w,"theory",Save,out _);Assert.False(DmvTransactions.Answer(w,"wrong",0,1,Save,out _));Assert.False(DmvTransactions.Answer(w,"theory",0,4,Save,out _));Assert.False(DmvTransactions.Answer(w,"theory",-1,1,Save,out _));Assert.False(DmvTransactions.Abandon(w,"wrong",Save,out _));Assert.Empty(w.Dmv.Pending.Answers);}
        [Fact] public void ZeroFeeAndSeparateProfilesDoNotCopyDocumentsOrMoney()
        {var a=W();var b=W();var c=C();c.TheoryPrice=0;Assert.True(DmvTransactions.Begin(a,c,false,"free",Save,out _));Assert.Equal(1000,a.Bank);Assert.Null(b.Dmv);Assert.Equal(1000,b.Bank);}
        [Fact] public void CounterOverflowAndBadConfigCannotBegin()
        {var w=W();w.Dmv=new DmvData{TheoryAttempts=int.MaxValue};Assert.False(DmvTransactions.Begin(w,C(),false,"max",Save,out _));var c=C();c.PassScore=16;Assert.False(DmvTransactions.Begin(W(),c,false,"bad",Save,out _));c=C();c.Stops.Add(99);Assert.False(c.Valid);}
    }
}
