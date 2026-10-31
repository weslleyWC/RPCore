using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class DmvRefinementTests
    {
        static readonly Action Save=()=>{};
        static DmvOptions Options(DmvClass category)=>category==DmvClass.C?DmvDefaults.Load():category==DmvClass.M1?DmvDefaults.Motorcycle():DmvDefaults.Truck();
        static DmvData Book(WalletData wallet,DmvClass category)=>category==DmvClass.C?wallet.Dmv:category==DmvClass.M1?wallet.DmvM1:wallet.DmvA;
        static string Json(object value)=>JsonConvert.SerializeObject(value);
        static WalletData Wallet()=>new WalletData{Cash=777,Bank=10000};
        static void Theory(WalletData w,DmvClass c,string id="theory",int correct=15)
        {
            Assert.True(DmvTransactions.Begin(w,Options(c),false,id,Save,out _,c,DmvAssessmentOptions.Load()));Assert.True(DmvTransactions.Start(w,id,Save,out _,c));
            for(int i=0;i<15;i++){var q=DmvQuestions.For(c)[i];Assert.True(DmvTransactions.Answer(w,id,i,i<correct?q.Correct:(q.Correct+1)%4,Save,out _,c));}
        }
        static void Practical(WalletData w,DmvClass c){Theory(w,c);Assert.True(DmvTransactions.Begin(w,Options(c),true,"drive",Save,out _,c,DmvAssessmentOptions.Load()));Assert.True(DmvTransactions.Start(w,"drive",Save,out _,c));}
        static void Push(WalletData w,DmvClass c,DmvDrive state)=>Assert.True(DmvTransactions.Progress(w,"drive",state,Save,out _,c));
        static DmvDrive Route(WalletData w,DmvClass category)
        {
            var c=Options(category);var state=new DmvDrive();Assert.True(state.Enter(true,true));Push(w,category,state);Assert.True(state.Signal(true,false,true,true,true,c));Push(w,category,state);Assert.True(state.Signal(false,true,true,true,true,c));Push(w,category,state);
            if(category==DmvClass.A){Assert.True(state.Couple(true,true,true,true,c));Push(w,category,state);}return state;
        }
        static void Rollback(WalletData w,Action<Action> action){string before=Json(w);Assert.Throws<System.IO.IOException>(()=>action(()=>throw new System.IO.IOException()));Assert.Equal(before,Json(w));}
        [Theory] [InlineData(DmvClass.C)] [InlineData(DmvClass.M1)] [InlineData(DmvClass.A)]
        public void FullCategoryCycleRetainsReviewFaultReasonSnapshotAndIssuesOnlySelectedDocument(DmvClass category)
        {
            var w=Wallet();Practical(w,category);var book=Book(w,category);Assert.True(book.TheoryReview.Valid);Assert.Equal(15,book.TheoryReview.Score);Assert.Equal(category,book.TheoryReview.Category);Assert.True(book.Pending.Assessment.RouteIndicators);
            var state=Route(w,category);state.Seconds=20;Assert.True(DmvAssessmentRules.Record(state,DmvFaultKind.Speed));Push(w,category,state);
            for(int i=0;i<Options(category).Route.Count;i++){Assert.True(state.Checkpoint(true,true,true,true,true,Options(category)));Push(w,category,state);}
            if(category==DmvClass.A){state.Reverse(true,Options(category));Push(w,category,state);state.Detach(true,Options(category));Push(w,category,state);}state.Park(true);Push(w,category,state);state.Leave(true);Push(w,category,state);
            Rollback(w,save=>DmvTransactions.Finish(w,"drive",true,save,out _,category,"Concluido"));Assert.True(DmvTransactions.Finish(w,"drive",true,Save,out _,category,"Concluido"));
            book=Book(w,category);Assert.True(book.Licensed);Assert.True(book.PracticalReport.Valid);Assert.True(book.PracticalReport.Passed);Assert.Equal(DmvFaultKind.Speed,Assert.Single(book.PracticalReport.Drive.Events).Kind);Assert.Equal(20,book.PracticalReport.Drive.Seconds);Assert.Equal(10000-Options(category).TheoryPrice-Options(category).PracticalPrice,w.Bank);Assert.Equal(777,w.Cash);
            Assert.True(DmvTransactions.Finish(w,"drive",true,()=>throw new Exception(),out bool changed,category));Assert.False(changed);Assert.True(DmvBookRules.Matches(book,category));foreach(DmvClass other in Enum.GetValues(typeof(DmvClass)))if(other!=category)Assert.Null(Book(w,other));
        }
        [Theory] [InlineData(DmvClass.C,11)] [InlineData(DmvClass.M1,12)] [InlineData(DmvClass.A,15)]
        public void FinishedTheoryReviewIsDurableAndCannotCrossCategories(DmvClass category,int correct)
        {
            var w=Wallet();Theory(w,category,correct:correct);var loaded=JsonConvert.DeserializeObject<WalletData>(Json(w));var book=Book(loaded,category);Assert.Equal(correct,book.TheoryReview.Score);Assert.Equal(correct>=12,book.TheoryReview.Passed);Assert.Equal(15,book.TheoryReview.Answers.Count);Assert.Null(book.Pending);Assert.True(DmvBookRules.Matches(book,category));Assert.False(DmvBookRules.Matches(book,(DmvClass)(((int)category+1)%3)));
            if(!book.TheoryPassed){Assert.True(DmvTransactions.Begin(loaded,Options(category),false,"retry",Save,out _,category,DmvAssessmentOptions.Load()));Assert.Equal(correct,Book(loaded,category).TheoryReview.Score);}
        }
        [Fact] public void AssessmentPolicyIsCopiedAtPaymentAndOldAttemptsRemainUnchanged()
        {
            var w=Wallet();var policy=DmvAssessmentOptions.Load();Assert.True(policy.Valid);Assert.True(DmvTransactions.Begin(w,Options(DmvClass.C),false,"new",Save,out _,DmvClass.C,policy));policy.StopSeconds=5;Assert.Equal(2,w.Dmv.Pending.Assessment.StopSeconds);
            var old=Wallet();Assert.True(DmvTransactions.Begin(old,Options(DmvClass.C),false,"legacy",Save,out _));var loaded=JsonConvert.DeserializeObject<WalletData>(Json(old));Assert.Null(loaded.Dmv.Pending.Assessment);Assert.True(loaded.Dmv.Valid);int bank=loaded.Bank;Assert.True(DmvTransactions.Start(loaded,"legacy",Save,out _));Assert.Equal(bank,loaded.Bank);
        }
        [Theory] [InlineData(0)] [InlineData(11)] [InlineData(float.NaN)]
        public void InvalidAssessmentCannotDebitOrCreateAttempt(float grace)
        {var w=Wallet();var policy=DmvAssessmentOptions.Load();policy.SpeedGraceSeconds=grace;string before=Json(w);Assert.False(DmvTransactions.Begin(w,Options(DmvClass.C),false,"bad",Save,out _,DmvClass.C,policy));Assert.Equal(before,Json(w));}
        [Fact] public void AContinuousViolationCountsOnceAndMustRecoverBeforeAnotherFault()
        {
            var policy=DmvAssessmentOptions.Load();var monitor=new DmvFaultMonitor();int faults=0;for(int i=0;i<20;i++)faults+=monitor.Update(1,true,false,0,policy).Count;Assert.Equal(1,faults);
            monitor.Update(1,false,false,0,policy);for(int i=0;i<4;i++)faults+=monitor.Update(1,true,false,0,policy).Count;Assert.Equal(1,faults);
            monitor.Update(1,false,false,0,policy);monitor.Update(1,false,false,0,policy);for(int i=0;i<3;i++)faults+=monitor.Update(1,true,false,0,policy).Count;Assert.Equal(2,faults);
        }
        [Fact] public void FaultGraceAndCollisionCooldownDoNotCountPerFrameOrWhilePaused()
        {
            var p=DmvAssessmentOptions.Load();var monitor=new DmvFaultMonitor();Assert.Empty(monitor.Update(0,true,true,100,p));Assert.Empty(monitor.Update(float.NaN,true,true,100,p));Assert.Empty(monitor.Update(2,true,true,100,p));
            Assert.Equal(new[]{DmvFaultKind.Collision},monitor.Update(1,true,true,30,p));Assert.Empty(monitor.Update(1,true,true,30,p));var faults=monitor.Update(1,true,true,30,p);Assert.Contains(DmvFaultKind.Speed,faults);Assert.Contains(DmvFaultKind.Collision,faults);Assert.DoesNotContain(DmvFaultKind.Protection,faults);monitor.Update(1,true,true,0,p);Assert.Contains(DmvFaultKind.Protection,monitor.Update(1,true,true,0,p));
        }
        [Theory] [InlineData(3.5f,2,20,.5f,true,true,true,true)] [InlineData(3.51f,0,0,0,true,true,true,false)] [InlineData(0,2.01f,0,0,true,true,true,false)] [InlineData(0,0,21,0,true,true,true,false)] [InlineData(0,0,0,.51f,true,true,true,false)] [InlineData(0,0,0,0,false,true,true,false)] [InlineData(0,0,0,0,true,false,true,false)] [InlineData(0,0,0,0,true,true,false,false)] [InlineData(float.NaN,0,0,0,true,true,true,false)]
        public void ParkingNeedsActualBayHeightAlignmentStopAndCorrectCrew(float distance,float height,float heading,float speed,bool driver,bool examiner,bool protection,bool allowed)
        {Assert.Equal(allowed,DmvAssessmentRules.Parking(distance,height,heading,0,speed,driver,examiner,protection,DmvAssessmentOptions.Load()));}
        [Theory] [InlineData(-10,10,1)] [InlineData(10,10,-1)] [InlineData(0,20,0)] [InlineData(0,0,0)]
        public void DirectionInstructionsAreDerivedFromRouteGeometry(float nextX,float nextY,int turn)
        {var c=Options(DmvClass.C);c.Spawn.X=c.Spawn.Y=0;c.Route[0].X=0;c.Route[0].Y=10;c.Route[1].X=nextX;c.Route[1].Y=nextY;Assert.Equal(turn,DmvAssessmentRules.Turn(c,0,40));Assert.Equal(0,DmvAssessmentRules.Turn(c,c.Route.Count-1,40));}
        [Fact] public void FaultHistoryCannotBeRemovedOrRewrittenOnProgressAndFailureKeepsDetailedReport()
        {
            var w=Wallet();Practical(w,DmvClass.C);var state=Route(w,DmvClass.C);state.Seconds=10;DmvAssessmentRules.Record(state,DmvFaultKind.Collision);Push(w,DmvClass.C,state);
            var altered=JsonConvert.DeserializeObject<DmvDrive>(Json(state));altered.Events.Clear();Assert.False(DmvTransactions.Progress(w,"drive",altered,Save,out _));altered=JsonConvert.DeserializeObject<DmvDrive>(Json(state));altered.Events[0].Kind=DmvFaultKind.Speed;Assert.False(DmvTransactions.Progress(w,"drive",altered,Save,out _));
            Rollback(w,save=>DmvTransactions.Finish(w,"drive",false,save,out _,DmvClass.C,"Danos graves"));Assert.True(DmvTransactions.Finish(w,"drive",false,Save,out _,DmvClass.C,"Danos graves"));Assert.Equal("Danos graves",w.Dmv.PracticalReport.Reason);Assert.Equal(1,w.Dmv.PracticalReport.Drive.Faults);Assert.True(w.Dmv.TheoryPassed);Assert.False(w.Dmv.Licensed);
        }
        [Fact] public void ResumptionPreservesFaultProofAndPaidPolicyWithoutResettingElapsedTime()
        {
            var w=Wallet();Practical(w,DmvClass.A);var state=Route(w,DmvClass.A);state.Seconds=100;DmvAssessmentRules.Record(state,DmvFaultKind.Protection);Push(w,DmvClass.A,state);int bank=w.Bank;
            Assert.True(DmvTransactions.Restart(w,"drive",Save,out _,DmvClass.A));Assert.Equal(DmvStage.AwaitDriver,w.DmvA.Pending.Drive.Stage);Assert.False(w.DmvA.Pending.Drive.Coupled);Assert.Equal(100,w.DmvA.Pending.Drive.Seconds);Assert.Single(w.DmvA.Pending.Drive.Events);Assert.Equal(bank,w.Bank);Assert.Equal(3,w.DmvA.Pending.Assessment.ParkingSeconds);
        }
        [Fact] public void KeyboardAnswerQueueDiscardsStalePageAndDuplicateClick()
        {
            var q=new DmvTheoryAnswerQueue();Assert.True(q.Queue("one",0,2,100));Assert.False(q.Queue("one",0,1,101));Assert.Equal(2,q.Take("one",0,101));Assert.Equal(-1,q.Take("one",0,101));Assert.True(q.Queue("one",1,0,200));Assert.Equal(-1,q.Take("one",2,201));Assert.True(q.Queue("one",2,0,300));Assert.Equal(-1,q.Take("two",2,301));Assert.True(q.Queue("one",3,0,400));Assert.Equal(-1,q.Take("one",3,1401));Assert.True(q.Queue("one",3,0,1500));q.Clear();Assert.Equal(-1,q.Take("one",3,1501));
        }
        [Fact] public void NewReportsPreserveUnknownMetadataAndExistingFinancialState()
        {
            var w=Wallet();Practical(w,DmvClass.C);var j=JObject.Parse(Json(w));j["Dmv"]["Future"]="document";j["Dmv"]["Pending"]["Assessment"]["Future"]="policy";j["Dmv"]["TheoryReview"]["Future"]="review";w=j.ToObject<WalletData>();Assert.True(DmvTransactions.Finish(w,"drive",false,Save,out _,DmvClass.C,"Interrompido"));var next=JObject.Parse(Json(w));Assert.Equal("document",(string)next["Dmv"]["Future"]);Assert.Equal("review",(string)next["Dmv"]["TheoryReview"]["Future"]);Assert.Equal(777,w.Cash);Assert.Null(w.Pdm);
        }
        [Fact] public void IssuableCompletedExamSurvivesFullReloadWithoutPhysicalVehicleOrNewCharge()
        {
            var w=Wallet();Practical(w,DmvClass.C);var state=Route(w,DmvClass.C);for(int i=0;i<Options(DmvClass.C).Route.Count;i++){state.Checkpoint(true,true,true,true,true,Options(DmvClass.C));Push(w,DmvClass.C,state);}state.Park(true);Push(w,DmvClass.C,state);state.Leave(true);Push(w,DmvClass.C,state);
            var loaded=JsonConvert.DeserializeObject<WalletData>(Json(w));int bank=loaded.Bank;Assert.True(DmvTransactions.Finish(loaded,"drive",true,Save,out _));Assert.Equal(bank,loaded.Bank);Assert.True(loaded.Dmv.Licensed);Assert.True(loaded.Dmv.PracticalReport.Valid);
        }
    }
}
