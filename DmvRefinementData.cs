using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class DmvAssessmentOptions : ExtensibleData
    {
        public float SpeedGraceSeconds=3,ProtectionGraceSeconds=5,SafeResetSeconds=2,CollisionCooldownSeconds=2,CollisionDamage=25;
        public float StopSeconds=2,ParkingSeconds=3,ParkingRadius=3.5f,ParkingHeading=20,TurnDegrees=40;
        public bool RouteIndicators=true;
        [JsonIgnore] public bool Valid=>new[]{SpeedGraceSeconds,ProtectionGraceSeconds,SafeResetSeconds,CollisionCooldownSeconds,CollisionDamage,StopSeconds,ParkingSeconds,ParkingRadius,ParkingHeading,TurnDegrees}.All(DmvDrive.Finite) && SpeedGraceSeconds>=1 && SpeedGraceSeconds<=10 && ProtectionGraceSeconds>=2 && ProtectionGraceSeconds<=15 && SafeResetSeconds>=1 && SafeResetSeconds<=5 && CollisionCooldownSeconds>=1 && CollisionCooldownSeconds<=10 && CollisionDamage>=10 && CollisionDamage<=100 && StopSeconds>=1 && StopSeconds<=5 && ParkingSeconds>=2 && ParkingSeconds<=10 && ParkingRadius>=2 && ParkingRadius<=7 && ParkingHeading>=10 && ParkingHeading<=45 && TurnDegrees>=30 && TurnDegrees<=90;
        public static DmvAssessmentOptions Load(){using(var stream=typeof(DmvAssessmentOptions).Assembly.GetManifestResourceStream("RPCore.JobDefaults.DmvRefinement.json"))using(var reader=new System.IO.StreamReader(stream))return JsonConvert.DeserializeObject<DmvAssessmentOptions>(reader.ReadToEnd());}
    }
    public enum DmvFaultKind { Collision,Speed,Protection,Indicator }
    public sealed class DmvFaultEvent : ExtensibleData
    {
        public DmvFaultKind Kind;public int Point;public float Seconds;
        [JsonIgnore] public bool Valid=>Enum.IsDefined(typeof(DmvFaultKind),Kind) && Point>=0 && Point<=100 && DmvDrive.Finite(Seconds) && Seconds>=0 && Seconds<=86400;
    }
    public sealed class DmvPracticalReport : ExtensibleData
    {
        public string Id,Reason;public bool Passed;public DmvOptions Options;public DmvDrive Drive;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=100 && Reason!=null && Reason.Length<=160 && Options?.Valid==true && Drive!=null && Drive.Valid(Options) && (!Passed || Drive.Stage==DmvStage.Report && !Drive.Failed(Options));
    }
    public sealed class DmvTheoryReview : ExtensibleData
    {
        public DmvClass Category;public List<int> Answers=new List<int>();public int Score,PassScore;public bool Passed;
        [JsonIgnore] public bool Valid=>Enum.IsDefined(typeof(DmvClass),Category) && Answers!=null && Answers.Count==15 && Answers.All(a=>a>=0 && a<4) && Score==DmvQuestions.Score(Answers,Category) && PassScore>=1 && PassScore<=15 && Passed==(Score>=PassScore);
    }
    public static class DmvAssessmentRules
    {
        public static float HeadingDifference(float a,float b)=>DmvRigRules.HeadingDifference(a,b);
        public static bool Parking(float distance,float height,float heading,float expectedHeading,float speed,bool driver,bool examiner,bool protectedDriver,DmvAssessmentOptions settings)
        {return settings?.Valid==true && DmvDrive.Finite(distance) && DmvDrive.Finite(height) && DmvDrive.Finite(speed) && distance>=0 && distance<=settings.ParkingRadius && Math.Abs(height)<=2 && HeadingDifference(heading,expectedHeading)<=settings.ParkingHeading && speed>=0 && speed<=.5f && driver && examiner && protectedDriver;}
        // Positive means left: GTA headings increase towards the west.
        public static int Turn(DmvOptions options,int point,float threshold)
        {
            if(options?.Valid!=true || point<0 || point>=options.Route.Count-1 || !DmvDrive.Finite(threshold) || threshold<30)return 0;
            var before=point==0?options.Spawn:options.Route[point-1];var at=options.Route[point];var after=options.Route[point+1];
            double ax=at.X-before.X,ay=at.Y-before.Y,bx=after.X-at.X,by=after.Y-at.Y;
            if(ax*ax+ay*ay<25 || bx*bx+by*by<25)return 0;
            double angle=Math.Atan2(ax*by-ay*bx,ax*bx+ay*by)*180/Math.PI;
            return Math.Abs(angle)<threshold || Math.Abs(angle)>150?0:angle>0?1:-1;
        }
        public static string FaultName(DmvFaultKind kind)=>kind==DmvFaultKind.Collision?"colisao / dano":kind==DmvFaultKind.Speed?"excesso de velocidade":kind==DmvFaultKind.Protection?"conducao sem cinto/capacete":"conversao sem sinalizacao correta";
        public static bool Record(DmvDrive drive,DmvFaultKind kind,int point=-1)
        {if(drive==null || !Enum.IsDefined(typeof(DmvFaultKind),kind) || drive.Faults>=10000 || drive.Events==null || drive.Events.Count>=100 || point < -1 || point>drive.Point)return false;drive.Faults++;drive.Events.Add(new DmvFaultEvent{Kind=kind,Point=point<0?drive.Point:point,Seconds=drive.Seconds});return true;}
    }
    public static class DmvBookRules
    {
        public static bool Matches(DmvData book,DmvClass category)=>book?.Valid==true && Enum.IsDefined(typeof(DmvClass),category) && (book.Pending==null || book.Pending.Options.Category==category) && (book.TheoryReview==null || book.TheoryReview.Category==category) && (book.PracticalReport==null || book.PracticalReport.Options.Category==category);
    }
    public sealed class DmvFaultMonitor
    {
        sealed class Violation{public float Unsafe,Safe;public bool Reported;}
        readonly Violation speed=new Violation(),protection=new Violation();float clock,lastCollision=-1000;
        static bool Check(Violation state,bool unsafeNow,float elapsed,float grace,float reset)
        {
            if(unsafeNow){state.Safe=0;state.Unsafe+=elapsed;if(!state.Reported && state.Unsafe>=grace){state.Reported=true;return true;}}
            else{state.Safe+=elapsed;state.Unsafe=0;if(state.Safe>=reset)state.Reported=false;}return false;
        }
        public List<DmvFaultKind> Update(float elapsed,bool speeding,bool unprotected,float damageLoss,DmvAssessmentOptions options)
        {
            var faults=new List<DmvFaultKind>();if(options?.Valid!=true || !DmvDrive.Finite(elapsed) || elapsed<=0 || elapsed>1 || !DmvDrive.Finite(damageLoss))return faults;
            clock+=elapsed;if(Check(speed,speeding,elapsed,options.SpeedGraceSeconds,options.SafeResetSeconds))faults.Add(DmvFaultKind.Speed);
            if(Check(protection,unprotected,elapsed,options.ProtectionGraceSeconds,options.SafeResetSeconds))faults.Add(DmvFaultKind.Protection);
            if(damageLoss>=options.CollisionDamage && clock-lastCollision>=options.CollisionCooldownSeconds){faults.Add(DmvFaultKind.Collision);lastCollision=clock;}return faults;
        }
    }
    public sealed class DmvTheoryAnswerQueue
    {
        string id;int question,answer,at;bool pending;
        public void Clear()=>pending=false;
        public bool Queue(string attempt,int expectedQuestion,int selected,int now)
        {if(pending || string.IsNullOrWhiteSpace(attempt) || expectedQuestion<0 || expectedQuestion>=15 || selected<0 || selected>3)return false;id=attempt;question=expectedQuestion;answer=selected;at=now;pending=true;return true;}
        public int Take(string attempt,int currentQuestion,int now)
        {if(!pending)return -1;pending=false;return id==attempt && question==currentQuestion && unchecked((uint)(now-at))<=1000?answer:-1;}
    }
}
