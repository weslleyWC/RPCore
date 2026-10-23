using System;
using System.Linq;
using Newtonsoft.Json;

namespace RPCore
{
    public sealed class PrisonOptions : ExtensibleData
    {
        public JobPoint Start,Spawn,Destination;
        public int Prisoners=4,LoadSeconds=4,BasePay=1600,ReturnBonus=400;
        public float PayPerMeter=.12f;
        [JsonIgnore] public bool Valid => new[]{Start,Spawn,Destination}.All(p=>p!=null && p.Valid) && Prisoners>=1 && Prisoners<=8 && LoadSeconds>=2 && LoadSeconds<=15 && BasePay>=0 && BasePay<=100000 && ReturnBonus>=0 && ReturnBonus<=100000 && !float.IsNaN(PayPerMeter) && !float.IsInfinity(PayPerMeter) && PayPerMeter>=0 && PayPerMeter<=10;
    }
    public static class PrisonDefaults
    {
        public static PrisonOptions Load()
        {
            using(var stream=typeof(PrisonDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.PrisonTransport.json"))
            using(var reader=new System.IO.StreamReader(stream))return JsonConvert.DeserializeObject<PrisonOptions>(reader.ReadToEnd());
        }
    }
    public enum PrisonStage { AwaitDriver,Loading,Transporting,Unloading,PaymentPending,Returning,Finished,Cancelled }
    public sealed class PrisonRun
    {
        public readonly string Id=Guid.NewGuid().ToString("N");
        public readonly int Count;
        readonly int duration;
        int began;
        bool ready;
        public PrisonStage Stage {get;private set;}=PrisonStage.AwaitDriver;
        public PrisonRun(int count,int loadSeconds)
        {if(count<1 || count>8 || loadSeconds<2 || loadSeconds>15)throw new ArgumentOutOfRangeException();Count=count;duration=loadSeconds*1000;}
        public bool BeginLoading(int now,bool eligible)
        {if(Stage!=PrisonStage.AwaitDriver || !eligible)return false;began=now;ready=false;Stage=PrisonStage.Loading;return true;}
        public void InterruptLoading(){if(Stage==PrisonStage.Loading){ready=false;Stage=PrisonStage.AwaitDriver;}}
        public bool LoadReady(int now,bool eligible)
        {if(Stage!=PrisonStage.Loading)return false;if(!eligible){InterruptLoading();return false;}ready=unchecked(now-began)>=duration;return ready;}
        public bool ManifestConfirmed(bool eligible,int seated)
        {if(Stage!=PrisonStage.Loading || !ready || !eligible || seated!=Count)return false;Stage=PrisonStage.Transporting;return true;}
        public bool BeginUnload(bool atDestination,int seated)
        {if(Stage!=PrisonStage.Transporting || !atDestination || seated!=Count)return false;Stage=PrisonStage.Unloading;return true;}
        public bool ConfirmUnloaded(bool atDestination,int outsideAlive)
        {if(Stage!=PrisonStage.Unloading || !atDestination || outsideAlive!=Count)return false;Stage=PrisonStage.PaymentPending;return true;}
        public bool PayCustody(bool allDelivered,Func<int,bool> commit)
        {if(Stage!=PrisonStage.PaymentPending || !allDelivered || !commit(1))return false;Stage=PrisonStage.Returning;return true;}
        public bool Return(bool atBaseEmpty,Func<int,bool> commit)
        {if(Stage!=PrisonStage.Returning || !atBaseEmpty || !commit(2))return false;Stage=PrisonStage.Finished;return true;}
        public void Cancel(){if(Stage!=PrisonStage.Finished)Stage=PrisonStage.Cancelled;}
    }
    public static class PrisonRules
    {
        public static bool Stopped(bool driver,double speed,double distance,double radius)
        {return driver && new[]{speed,distance,radius}.All(v=>!double.IsNaN(v) && !double.IsInfinity(v) && v>=0) && speed<.5 && radius>0 && radius<=60 && distance<=radius;}
    }
}
