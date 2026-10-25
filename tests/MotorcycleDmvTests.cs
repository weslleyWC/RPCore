using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class MotorcycleDmvTests
    {
        static readonly Action Save=()=>{};
        static WalletData W()=>new WalletData{Bank=1000,Cash=55};
        static DmvOptions M()=>DmvDefaults.Motorcycle();
        static void Begin(WalletData w,bool practical=false,string id="moto")=>Assert.True(DmvTransactions.Begin(w,M(),practical,id,Save,out _,DmvClass.M1));
        static void Start(WalletData w,string id="moto")=>Assert.True(DmvTransactions.Start(w,id,Save,out _,DmvClass.M1));
        static void Theory(WalletData w){Begin(w);Start(w);for(int i=0;i<15;i++)Assert.True(DmvTransactions.Answer(w,"moto",i,MotorcycleQuestions.All[i].Correct,Save,out _,DmvClass.M1));}
        static void Push(WalletData w,DmvDrive s)=>Assert.True(DmvTransactions.Progress(w,"practical",s,Save,out _,DmvClass.M1));
        static void Report(WalletData w)
        {var c=M();var s=new DmvDrive();s.Enter(true,true);Push(w,s);s.Signal(true,false,true,true,true,c);Push(w,s);s.Signal(false,true,true,true,true,c);Push(w,s);for(int i=0;i<c.Route.Count;i++){s.Checkpoint(true,true,true,true,true,c);Push(w,s);}s.Park(true);Push(w,s);s.Leave(true);Push(w,s);}
        [Fact] public void OldClassCConfigAndPendingDeserializeAsClassCWithoutHelmetOrSpeedOverrides()
        {var old=JsonConvert.DeserializeObject<DmvOptions>(JsonConvert.SerializeObject(DmvDefaults.Load()));Assert.Equal(DmvClass.C,old.Category);Assert.False(old.RequireHelmet);Assert.True(old.RequireSeatbelt);Assert.Empty(old.PointSpeeds);Assert.Equal(50,old.SpeedAt(0));var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":1000,\"Dmv\":{\"TheoryPassed\":true,\"Licensed\":true,\"Future\":1}}");Assert.True(w.Dmv.Valid);Assert.Null(w.DmvM1);Begin(w);Assert.True(w.Dmv.Licensed);}
        [Fact] public void MotorcycleDefaultsRequireHelmetWithoutImpossibleSeatbeltRequirement()
        {var c=M();Assert.True(c.Valid);Assert.Equal(DmvClass.M1,c.Category);Assert.True(c.RequireHelmet);Assert.False(c.RequireSeatbelt);Assert.Equal(30,c.TheoryPrice);Assert.Equal(60,c.PracticalPrice);Assert.Equal(36,c.Route.Count);Assert.Equal(40,c.SpeedKmh);Assert.Equal(15,c.SpeedAt(0));Assert.Equal(15,c.SpeedAt(1));Assert.Equal(15,c.SpeedAt(2));Assert.Equal(40,c.SpeedAt(3));}
        [Theory][InlineData(0)][InlineData(1)][InlineData(2)]
        public void LowSpeedPointsAreExplicitAndOtherPointsKeepRoadLimit(int point)
        {Assert.Equal(15,M().SpeedAt(point));Assert.Equal(40,M().SpeedAt(35));Assert.Equal(50,DmvDefaults.Load().SpeedAt(point));}
        [Fact] public void MotorcycleQuestionsAreOwnDistinctBankAndAllHaveFourValidChoices()
        {Assert.Equal(15,MotorcycleQuestions.All.Length);Assert.Same(MotorcycleQuestions.All,DmvQuestions.For(DmvClass.M1));Assert.Same(DmvQuestions.All,DmvQuestions.For(DmvClass.C));foreach(var q in MotorcycleQuestions.All){Assert.Equal(4,q.Answers.Length);Assert.InRange(q.Correct,0,3);}Assert.DoesNotContain(MotorcycleQuestions.All[0].Text,DmvQuestions.All.Select(q=>q.Text));}
        [Fact] public void MotorcycleScoringCannotUseTheCarAnswerKey()
        {var answers=MotorcycleQuestions.All.Select(q=>q.Correct).ToList();Assert.Equal(15,DmvQuestions.Score(answers,DmvClass.M1));Assert.NotEqual(15,DmvQuestions.Score(answers,DmvClass.C));}
        [Theory][InlineData(11,false)][InlineData(12,true)][InlineData(15,true)]
        public void MotorcycleThresholdAwardsOnlyItsTheoryCertificate(int correct,bool pass)
        {var w=W();Begin(w);Start(w);for(int i=0;i<15;i++){int a=MotorcycleQuestions.All[i].Correct;DmvTransactions.Answer(w,"moto",i,i<correct?a:(a+1)%4,Save,out _,DmvClass.M1);}Assert.Equal(pass,w.DmvM1.TheoryPassed);Assert.Equal(correct,w.DmvM1.LastTheoryScore);Assert.False(w.DmvM1.Licensed);Assert.Null(w.Dmv);Assert.Equal(970,w.Bank);}
        [Fact] public void FullMotorcycleTheoryAndPracticalIssueOnlyM1WithTwoFees()
        {var w=W();Theory(w);Begin(w,true,"practical");Start(w,"practical");Report(w);Assert.True(DmvTransactions.Finish(w,"practical",true,Save,out _,DmvClass.M1));Assert.True(w.DmvM1.Licensed);Assert.True(w.DmvM1.TheoryPassed);Assert.Null(w.Dmv);Assert.Equal(910,w.Bank);Assert.Equal(90,w.DmvM1.Spent);Assert.Equal(55,w.Cash);}
        [Fact] public void SameAttemptIdsAndReceiptsRemainIndependentAcrossClasses()
        {var w=W();Assert.True(DmvTransactions.Begin(w,DmvDefaults.Load(),false,"same",Save,out _));Assert.True(DmvTransactions.Begin(w,M(),false,"same",Save,out _,DmvClass.M1));Assert.Equal(940,w.Bank);Assert.True(DmvTransactions.Start(w,"same",Save,out _));Assert.True(DmvTransactions.Start(w,"same",Save,out _,DmvClass.M1));Assert.True(DmvTransactions.Answer(w,"same",0,DmvQuestions.All[0].Correct,Save,out _));Assert.Empty(w.DmvM1.Pending.Answers);Assert.True(DmvTransactions.Answer(w,"same",0,MotorcycleQuestions.All[0].Correct,Save,out _,DmvClass.M1));Assert.Single(w.Dmv.Pending.Answers);Assert.Single(w.DmvM1.Pending.Answers);}
        [Fact] public void CarLicenseDoesNotGrantMotorcycleTheoryOrSkipItsPractical()
        {var w=W();w.Dmv=new DmvData{TheoryPassed=true,Licensed=true};Assert.False(DmvTransactions.Begin(w,M(),true,"early",Save,out _,DmvClass.M1));Assert.Null(w.DmvM1);Begin(w);Assert.True(w.Dmv.Licensed);Assert.False(w.DmvM1.TheoryPassed);}
        [Fact] public void WrongClassSelectionCannotChargeOrModifyAnExistingAttempt()
        {var w=W();Assert.False(DmvTransactions.Begin(w,M(),false,"wrong",Save,out _));Assert.False(DmvTransactions.Begin(w,DmvDefaults.Load(),false,"wrong",Save,out _,DmvClass.M1));Assert.Equal(1000,w.Bank);Assert.Null(w.Dmv);Assert.Null(w.DmvM1);Begin(w);Assert.False(DmvTransactions.Start(w,"moto",Save,out _));Assert.False(w.DmvM1.Pending.Started);}
        [Fact] public void InvalidClassIsRejectedBeforeAnyBookAssignment()
        {var w=W();Assert.False(DmvTransactions.Start(w,"x",Save,out _,(DmvClass)99));Assert.False(DmvTransactions.Cancel(w,"x",Save,out _,(DmvClass)99));Assert.Null(w.DmvM1);Assert.Equal(1000,w.Bank);}
        [Fact] public void FailedMotorcycleSaveRollsBackOnlyItsBookAndBank()
        {var w=W();w.Dmv=new DmvData{TheoryPassed=true,Licensed=true};var car=w.Dmv;Assert.Throws<Exception>(()=>DmvTransactions.Begin(w,M(),false,"moto",()=>throw new Exception(),out _,DmvClass.M1));Assert.Null(w.DmvM1);Assert.Same(car,w.Dmv);Assert.True(car.Licensed);Assert.Equal(1000,w.Bank);}
        [Fact] public void FailedMotorcycleAnswerPreservesPreviousAnswersAndCarBookReference()
        {var w=W();w.Dmv=new DmvData{TheoryPassed=true,Licensed=true};var car=w.Dmv;Begin(w);Start(w);Assert.Throws<Exception>(()=>DmvTransactions.Answer(w,"moto",0,2,()=>throw new Exception(),out _,DmvClass.M1));Assert.Empty(w.DmvM1.Pending.Answers);Assert.Same(car,w.Dmv);Assert.Equal(970,w.Bank);}
        [Fact] public void ReloadResumeAndReceiptReplayCannotChargeMotorcycleTwice()
        {var w=W();Begin(w);Start(w);DmvTransactions.Answer(w,"moto",0,2,Save,out _,DmvClass.M1);w=CharacterRules.Copy(w);Assert.True(DmvTransactions.Begin(w,M(),false,"moto",()=>throw new Exception(),out bool changed,DmvClass.M1));Assert.False(changed);Assert.True(DmvTransactions.Answer(w,"moto",0,2,()=>throw new Exception(),out changed,DmvClass.M1));Assert.False(changed);Assert.Single(w.DmvM1.Pending.Answers);Assert.Equal(970,w.Bank);}
        [Fact] public void MotorcycleRefundBeforeStartDoesNotTouchCarCertificateOrFees()
        {var w=W();w.Dmv=new DmvData{TheoryPassed=true,Spent=30,TheoryAttempts=1};var car=w.Dmv;Begin(w);Assert.True(DmvTransactions.Cancel(w,"moto",Save,out _,DmvClass.M1));Assert.Equal(1000,w.Bank);Assert.Equal(0,w.DmvM1.Spent);Assert.Same(car,w.Dmv);Assert.Equal(30,car.Spent);}
        [Fact] public void MotorcycleStartedAttemptHasNoRefundAndAbandonDoesNotEraseCarLicense()
        {var w=W();w.Dmv=new DmvData{TheoryPassed=true,Licensed=true};Begin(w);Start(w);Assert.False(DmvTransactions.Cancel(w,"moto",Save,out _,DmvClass.M1));Assert.True(DmvTransactions.Abandon(w,"moto",Save,out _,DmvClass.M1));Assert.Null(w.DmvM1.Pending);Assert.True(w.Dmv.Licensed);Assert.Equal(970,w.Bank);}
        [Fact] public void MotorcyclePracticalResumeKeepsTimeFaultsAndPriceSnapshot()
        {var w=W();Theory(w);Begin(w,true,"practical");Start(w,"practical");var progress=new DmvDrive{Faults=2,Seconds=90};progress.Enter(true,true);Push(w,progress);Assert.True(DmvTransactions.Restart(w,"practical",Save,out _,DmvClass.M1));Assert.Equal(2,w.DmvM1.Pending.Drive.Faults);Assert.Equal(90,w.DmvM1.Pending.Drive.Seconds);Assert.Equal(60,w.DmvM1.Pending.Price);Assert.Equal(910,w.Bank);}
        [Fact] public void MotorcycleFailurePreservesItsTheoryAndTheEntireCarBook()
        {var w=W();w.Dmv=new DmvData{TheoryPassed=true,Licensed=true};string before=JsonConvert.SerializeObject(w.Dmv);Theory(w);Begin(w,true,"practical");Start(w,"practical");Assert.True(DmvTransactions.Finish(w,"practical",false,Save,out _,DmvClass.M1));Assert.True(w.DmvM1.TheoryPassed);Assert.False(w.DmvM1.Licensed);Assert.Equal(before,JsonConvert.SerializeObject(w.Dmv));}
        [Fact] public void HelmetGateReplacesSeatbeltAndStillRequiresExclusiveSignalsAndExaminer()
        {var c=M();var s=new DmvDrive();Assert.True(s.Enter(true,true));Assert.False(s.Signal(true,false,false,true,true,c));Assert.False(s.Signal(true,true,true,true,true,c));Assert.False(s.Signal(true,false,true,true,false,c));Assert.True(s.Signal(true,false,true,true,true,c));Assert.True(s.Signal(false,true,true,true,true,c));Assert.Equal(DmvStage.Route,s.Stage);}
        [Theory][InlineData(true,true)][InlineData(false,false)]
        public void MotorcycleConfigCannotDemandCarBeltOrDisableHelmet(bool belt,bool helmet)
        {var c=M();c.RequireSeatbelt=belt;c.RequireHelmet=helmet;Assert.False(c.Valid);}
        [Theory][InlineData(-1,15)][InlineData(36,15)][InlineData(0,0)][InlineData(0,41)]
        public void InvalidPointSpeedOverridesCannotStartOrCharge(int point,int speed)
        {var c=M();c.PointSpeeds.Clear();c.PointSpeeds[point]=speed;Assert.False(c.Valid);Assert.False(DmvTransactions.Begin(W(),c,false,"bad",Save,out _,DmvClass.M1));}
        [Fact] public void PaidMotorcycleRulesAreDeepCopiesIncludingPointLimitsAndCategory()
        {var w=W();var c=M();DmvTransactions.Begin(w,c,false,"moto",Save,out _,DmvClass.M1);c.PointSpeeds[0]=30;c.RequireHelmet=false;c.Route[0].X=0;Assert.Equal(15,w.DmvM1.Pending.Options.SpeedAt(0));Assert.True(w.DmvM1.Pending.Options.RequireHelmet);Assert.NotEqual(0,w.DmvM1.Pending.Options.Route[0].X);Assert.Equal(DmvClass.M1,w.DmvM1.Pending.Options.Category);}
        [Fact] public void UnknownClassCAndM1FieldsSurviveM1Transactions()
        {var w=W();w.Dmv=new DmvData{TheoryPassed=true,Licensed=true};Begin(w);var j=JObject.FromObject(w);j["Dmv"]["Future"]="car";j["DmvM1"]["Future"]="moto";j["DmvM1"]["Pending"]["Future"]="pending";j["DmvM1"]["Pending"]["Options"]["Future"]="config";w=j.ToObject<WalletData>();Start(w);j=JObject.FromObject(w);Assert.Equal("car",(string)j["Dmv"]["Future"]);Assert.Equal("moto",(string)j["DmvM1"]["Future"]);Assert.Equal("pending",(string)j["DmvM1"]["Pending"]["Future"]);Assert.Equal("config",(string)j["DmvM1"]["Pending"]["Options"]["Future"]);}
        [Fact] public void ClassMismatchedPendingDoesNotLetTransactionsMutateWrongBook()
        {var w=W();Begin(w);w.Dmv=w.DmvM1;w.DmvM1=null;Assert.False(DmvTransactions.Start(w,"moto",Save,out _));Assert.False(w.Dmv.Pending.Started);Assert.Equal(970,w.Bank);}
        [Fact] public void CompletedMotorcycleLicenseCannotBeIssuedOrChargedTwice()
        {var w=W();Theory(w);Begin(w,true,"practical");Start(w,"practical");Report(w);DmvTransactions.Finish(w,"practical",true,Save,out _,DmvClass.M1);Assert.True(DmvTransactions.Finish(w,"practical",true,()=>throw new Exception(),out bool changed,DmvClass.M1));Assert.False(changed);Assert.False(DmvTransactions.Begin(w,M(),true,"repeat",Save,out _,DmvClass.M1));Assert.Equal(910,w.Bank);}
        [Fact] public void MotorcycleProgressCannotSkipRouteToLicenseReport()
        {var w=W();Theory(w);Begin(w,true,"practical");Start(w,"practical");Assert.False(DmvTransactions.Progress(w,"practical",new DmvDrive{Stage=DmvStage.Report,Point=36},Save,out _,DmvClass.M1));Assert.False(DmvTransactions.Finish(w,"practical",true,Save,out _,DmvClass.M1));Assert.False(w.DmvM1.Licensed);}
        [Fact] public void MotorcycleIssueSaveFailureKeepsPaidApprovedReportForRetry()
        {var w=W();Theory(w);Begin(w,true,"practical");Start(w,"practical");Report(w);Assert.Throws<Exception>(()=>DmvTransactions.Finish(w,"practical",true,()=>throw new Exception(),out _,DmvClass.M1));Assert.False(w.DmvM1.Licensed);Assert.Equal(DmvStage.Report,w.DmvM1.Pending.Drive.Stage);Assert.Equal(910,w.Bank);}
        static CharacterBody HelmetBody()=>new CharacterBody{Model=123,Health=175,MaxHealth=200,Armor=20,SelectedWeapon=CharacterRules.Unarmed,Components=Enumerable.Range(0,12).Select(i=>new WardrobePiece{Slot=i}).ToList(),Props=WardrobeRules.PropSlots.Select(i=>new WardrobePiece{Slot=i,Drawable=i==0?7:-1,Texture=0}).ToList()};
        [Fact] public void TemporaryHelmetDoesNotEnterSavedOutfitOrMutateLiveSnapshot()
        {var live=HelmetBody();var saved=DmvHelmetSnapshot.Filter(live,123,7,0,2,1);Assert.Equal(2,saved.Props.Find(p=>p.Slot==0).Drawable);Assert.Equal(1,saved.Props.Find(p=>p.Slot==0).Texture);Assert.Equal(7,live.Props.Find(p=>p.Slot==0).Drawable);Assert.Equal(175,saved.Health);Assert.Equal(20,saved.Armor);}
        [Fact] public void OriginalNoHatIsNormalizedAndOtherPropsRemainUntouched()
        {var live=HelmetBody();live.Props.Find(p=>p.Slot==1).Drawable=3;var saved=DmvHelmetSnapshot.Filter(live,123,7,0,-1,0);Assert.Equal(-1,saved.Props.Find(p=>p.Slot==0).Drawable);Assert.Equal(0,saved.Props.Find(p=>p.Slot==0).Texture);Assert.Equal(3,saved.Props.Find(p=>p.Slot==1).Drawable);}
        [Fact] public void ChangedModelOrExternallyChangedHeadPropBypassesHelmetFilter()
        {var live=HelmetBody();Assert.Same(live,DmvHelmetSnapshot.Filter(live,456,7,0,2,1));live.Props.Find(p=>p.Slot==0).Drawable=9;Assert.Same(live,DmvHelmetSnapshot.Filter(live,123,7,0,2,1));}
        [Fact] public void HelmetSnapshotFilterRetainsUnknownBodyAndHeadMetadata()
        {var j=JObject.FromObject(HelmetBody());j["Future"]="body";j["Props"][0]["Future"]="hat";var saved=DmvHelmetSnapshot.Filter(j.ToObject<CharacterBody>(),123,7,0,2,1);j=JObject.FromObject(saved);Assert.Equal("body",(string)j["Future"]);Assert.Equal("hat",(string)j["Props"][0]["Future"]);}
        [Fact] public void SeparatePlayerProfilesDoNotCopyEitherLicense()
        {var a=W();var b=W();Theory(a);Assert.True(a.DmvM1.TheoryPassed);Assert.Null(b.DmvM1);Assert.Null(b.Dmv);Assert.Equal(1000,b.Bank);}
    }
}
