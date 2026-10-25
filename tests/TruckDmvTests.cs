using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class TruckDmvTests
    {
        static readonly Action Save=()=>{};
        static DmvOptions A()=>DmvDefaults.Truck();
        static WalletData W()=>new WalletData{Bank=1000,Cash=99};
        static void Begin(WalletData w,bool practical=false,string id="a")=>Assert.True(DmvTransactions.Begin(w,A(),practical,id,Save,out _,DmvClass.A));
        static void Start(WalletData w,string id="a")=>Assert.True(DmvTransactions.Start(w,id,Save,out _,DmvClass.A));
        static void Theory(WalletData w){Begin(w);Start(w);for(int i=0;i<15;i++)Assert.True(DmvTransactions.Answer(w,"a",i,TruckQuestions.All[i].Correct,Save,out _,DmvClass.A));}
        static void Push(WalletData w,DmvDrive s)=>Assert.True(DmvTransactions.Progress(w,"p",s,Save,out _,DmvClass.A));
        static DmvDrive Road(WalletData w){var s=new DmvDrive();s.Enter(true,true);Push(w,s);s.Signal(true,false,true,true,true,A());Push(w,s);s.Signal(false,true,true,true,true,A());Push(w,s);Assert.True(s.Couple(true,true,true,true,A()));Push(w,s);return s;}
        static DmvDrive Parking(WalletData w){var s=Road(w);for(int i=0;i<A().Route.Count;i++){Assert.True(s.Checkpoint(true,true,true,true,true,A()));Push(w,s);}return s;}
        static DmvDrive Report(WalletData w){var s=Parking(w);Assert.True(s.Reverse(true,A()));Push(w,s);Assert.True(s.Detach(true,A()));Push(w,s);s.Park(true);Push(w,s);s.Leave(true);Push(w,s);return s;}
        [Fact] public void TruckDefaultsAreIndependentAndHaveIndustrialRouteAndRequiredBelt()
        {var a=A();Assert.True(a.Valid);Assert.Equal(DmvClass.A,a.Category);Assert.Equal(30,a.TheoryPrice);Assert.Equal(120,a.PracticalPrice);Assert.True(a.RequireSeatbelt);Assert.False(a.RequireHelmet);Assert.True(a.SnapRouteToRoad);Assert.Equal(16,a.Route.Count);Assert.Equal(5,a.ReverseMeters);Assert.NotEqual(DmvDefaults.Load().Entry.X,a.Entry.X);}
        [Fact] public void TruckTheoryHasFifteenOwnFourChoiceQuestions()
        {Assert.Same(TruckQuestions.All,DmvQuestions.For(DmvClass.A));Assert.Equal(15,TruckQuestions.All.Length);Assert.Equal(15,DmvQuestions.Score(TruckQuestions.All.Select(q=>q.Correct).ToList(),DmvClass.A));foreach(var q in TruckQuestions.All){Assert.Equal(4,q.Answers.Distinct().Count());Assert.InRange(q.Correct,0,3);}Assert.DoesNotContain(TruckQuestions.All[0].Text,DmvQuestions.All.Select(q=>q.Text));}
        [Fact] public void FullRigCycleChargesOnceAndIssuesOnlyA()
        {var w=W();Theory(w);Begin(w,true,"p");Start(w,"p");Report(w);Assert.True(DmvTransactions.Finish(w,"p",true,Save,out _,DmvClass.A));Assert.True(w.DmvA.Licensed);Assert.Null(w.Dmv);Assert.Null(w.DmvM1);Assert.Equal(850,w.Bank);Assert.Equal(99,w.Cash);Assert.Equal(150,w.DmvA.Spent);Assert.True(DmvTransactions.Finish(w,"p",true,()=>throw new Exception(),out bool changed,DmvClass.A));Assert.False(changed);}
        [Theory][InlineData(11,false)][InlineData(12,true)][InlineData(15,true)]
        public void TruckTheoryUsesItsOwnThresholdAndDoesNotIssueLicense(int score,bool pass)
        {var w=W();Begin(w);Start(w);for(int i=0;i<15;i++)DmvTransactions.Answer(w,"a",i,i<score?TruckQuestions.All[i].Correct:(TruckQuestions.All[i].Correct+1)%4,Save,out _,DmvClass.A);Assert.Equal(pass,w.DmvA.TheoryPassed);Assert.False(w.DmvA.Licensed);Assert.Equal(score,w.DmvA.LastTheoryScore);}
        [Fact] public void OlderCarAndMotorcycleLicensesCannotSkipTruckTheory()
        {var w=W();w.Dmv=new DmvData{TheoryPassed=true,Licensed=true};w.DmvM1=new DmvData{TheoryPassed=true,Licensed=true};Assert.False(DmvTransactions.Begin(w,A(),true,"p",Save,out _,DmvClass.A));Begin(w);Assert.True(w.Dmv.Licensed);Assert.True(w.DmvM1.Licensed);Assert.False(w.DmvA.TheoryPassed);}
        [Fact] public void ThreeBooksCanUseSameIdsWithoutSharingAnswersOrDebits()
        {var w=W();Begin(w);DmvTransactions.Begin(w,DmvDefaults.Load(),false,"a",Save,out _);DmvTransactions.Begin(w,DmvDefaults.Motorcycle(),false,"a",Save,out _,DmvClass.M1);Start(w);DmvTransactions.Answer(w,"a",0,0,Save,out _,DmvClass.A);Assert.Empty(w.Dmv.Pending.Answers);Assert.Empty(w.DmvM1.Pending.Answers);Assert.Single(w.DmvA.Pending.Answers);Assert.Equal(910,w.Bank);}
        [Theory][InlineData(false,true,true,true)][InlineData(true,false,true,true)][InlineData(true,true,false,true)][InlineData(true,true,true,false)]
        public void CouplingRequiresOwnRigDriverExaminerAndBelt(bool driver,bool examiner,bool belt,bool towing)
        {var s=new DmvDrive{Stage=DmvStage.Route};Assert.False(s.Couple(driver,examiner,belt,towing,A()));Assert.False(s.Checkpoint(true,true,true,true,true,A()));Assert.Equal(0,s.Point);}
        [Fact] public void TruckCannotSkipCouplingReverseOrDetachForReport()
        {var w=W();Theory(w);Begin(w,true,"p");Start(w,"p");var s=Parking(w);var unsafeExit=CharacterRules.Copy(s);unsafeExit.Park(true);Assert.False(unsafeExit.Valid(A()));Assert.False(DmvTransactions.Progress(w,"p",unsafeExit,Save,out _,DmvClass.A));Assert.False(s.Detach(true,A()));Assert.False(DmvTransactions.Finish(w,"p",true,Save,out _,DmvClass.A));}
        [Fact] public void NewRigAfterSuspensionRequiresNewPhysicalProofButKeepsPaidProgress()
        {var w=W();Theory(w);Begin(w,true,"p");Start(w,"p");var s=Parking(w);s.Reverse(true,A());Push(w,s);s.Seconds=105;s.Faults=2;Push(w,s);w=CharacterRules.Copy(w);Assert.True(DmvTransactions.Restart(w,"p",Save,out _,DmvClass.A));var d=w.DmvA.Pending.Drive;Assert.False(d.Coupled);Assert.False(d.Reversed);Assert.False(d.Detached);Assert.Equal(16,d.Point);Assert.Equal(2,d.Faults);Assert.Equal(105,d.Seconds);Assert.Equal(DmvStage.AwaitDriver,d.Stage);Assert.Equal(120,w.DmvA.Pending.Price);Assert.Equal(850,w.Bank);}
        [Fact] public void ApprovedReportCannotRestartOrEraseProof()
        {var w=W();Theory(w);Begin(w,true,"p");Start(w,"p");Report(w);Assert.False(DmvTransactions.Restart(w,"p",Save,out _,DmvClass.A));Assert.True(w.DmvA.Pending.Drive.Detached);}
        [Fact] public void RigProofCannotGoBackwardsOrBeInjectedDuringPreparation()
        {var w=W();Theory(w);Begin(w,true,"p");Start(w,"p");Assert.False(DmvTransactions.Progress(w,"p",new DmvDrive{Coupled=true},Save,out _,DmvClass.A));var s=Road(w);s.Coupled=false;Assert.False(DmvTransactions.Progress(w,"p",s,Save,out _,DmvClass.A));}
        [Fact] public void FailedTruckIssueSaveLeavesApprovedPendingAndOtherBookReferencesIntact()
        {var w=W();w.Dmv=new DmvData{Licensed=true,TheoryPassed=true};var old=w.Dmv;Theory(w);Begin(w,true,"p");Start(w,"p");Report(w);Assert.Throws<Exception>(()=>DmvTransactions.Finish(w,"p",true,()=>throw new Exception(),out _,DmvClass.A));Assert.Same(old,w.Dmv);Assert.False(w.DmvA.Licensed);Assert.True(w.DmvA.Pending.Drive.Detached);Assert.Equal(850,w.Bank);}
        [Fact] public void FailedTruckDebitRollsBackAbsentBookAndSharedBank()
        {var w=W();Assert.Throws<Exception>(()=>DmvTransactions.Begin(w,A(),false,"a",()=>throw new Exception(),out _,DmvClass.A));Assert.Equal(1000,w.Bank);Assert.Null(w.DmvA);}
        [Fact] public void CancelRefundsOnlyBeforeStartAndAbandonKeepsTheory()
        {var w=W();Theory(w);Begin(w,true,"p");Assert.True(DmvTransactions.Cancel(w,"p",Save,out _,DmvClass.A));Assert.Equal(970,w.Bank);Begin(w,true,"p2");Start(w,"p2");Assert.False(DmvTransactions.Cancel(w,"p2",Save,out _,DmvClass.A));Assert.True(DmvTransactions.Abandon(w,"p2",Save,out _,DmvClass.A));Assert.True(w.DmvA.TheoryPassed);Assert.Equal(850,w.Bank);}
        [Fact] public void PaidTruckRulesAndUnknownFieldsSurviveReloadAndProgress()
        {var w=W();Theory(w);var a=A();DmvTransactions.Begin(w,a,true,"p",Save,out _,DmvClass.A);a.Route[0].X=0;a.ReverseMeters=10;Assert.Equal(5,w.DmvA.Pending.Options.ReverseMeters);Assert.NotEqual(0,w.DmvA.Pending.Options.Route[0].X);var j=JObject.FromObject(w);j["DmvA"]["Future"]="book";j["DmvA"]["Pending"]["Options"]["Future"]="options";w=j.ToObject<WalletData>();Start(w,"p");Road(w);j=JObject.FromObject(w);Assert.Equal("book",(string)j["DmvA"]["Future"]);Assert.Equal("options",(string)j["DmvA"]["Pending"]["Options"]["Future"]);}
        [Theory][InlineData(-1)][InlineData(0)][InlineData(2)][InlineData(21)]
        public void InvalidReverseRequirementsCannotCharge(int meters)
        {var a=A();a.ReverseMeters=meters;Assert.False(a.Valid);Assert.False(DmvTransactions.Begin(W(),a,false,"x",Save,out _,DmvClass.A));}
        [Fact] public void TruckRequiresTrailerSpawnAndCannotUseMotorcycleProtection()
        {var a=A();a.TrailerSpawn=null;Assert.False(a.Valid);a=A();a.RequireSeatbelt=false;Assert.False(a.Valid);a=A();a.RequireHelmet=true;Assert.False(a.Valid);}
        [Theory][InlineData(0,359,1)][InlineData(350,10,20)][InlineData(-10,370,20)][InlineData(0,180,180)]
        public void BayHeadingWrapsAcrossNorth(float a,float b,float difference)=>Assert.Equal(difference,DmvRigRules.HeadingDifference(a,b));
        [Theory][InlineData(0,0,0,0,0,0,true)][InlineData(4,3,20,0,.49f,.49f,true)][InlineData(4.01f,0,0,0,0,0,false)][InlineData(0,3.01f,0,0,0,0,false)][InlineData(0,0,21,0,0,0,false)][InlineData(0,0,0,0,.5f,0,false)][InlineData(0,0,0,0,0,.5f,false)][InlineData(float.NaN,0,0,0,0,0,false)]
        public void BayRequiresPositionHeightHeadingAndBothVehiclesStopped(float distance,float vertical,float heading,float expected,float truckSpeed,float trailerSpeed,bool allowed)=>Assert.Equal(allowed,DmvRigRules.InBay(distance,vertical,heading,expected,truckSpeed,trailerSpeed));
        [Theory][InlineData(2,-1,true,true,true,2)][InlineData(2,1,true,true,true,0)][InlineData(0,-1,true,true,true,0)][InlineData(11,-1,true,true,true,0)][InlineData(2,-1,false,true,true,0)][InlineData(2,-1,true,false,true,0)][InlineData(2,-1,true,true,false,0)][InlineData(float.NaN,-1,true,true,true,0)]
        public void ReverseMeasuresPhysicalMovementOnlyWithCoupledCorrectProtectedDriver(float moved,float forward,bool coupled,bool driver,bool belt,float added)=>Assert.Equal(3+added,DmvRigRules.ReverseDistance(3,moved,forward,coupled,driver,belt));
        [Fact] public void CarAndMotorcycleSavedDrivesCannotCarryTruckProof()
        {Assert.False(new DmvDrive{Coupled=true}.Valid(DmvDefaults.Load()));Assert.False(new DmvDrive{Coupled=true}.Valid(DmvDefaults.Motorcycle()));}
    }
}
