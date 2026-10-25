using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class DmvOptions : ExtensibleData
    {
        public int TheoryPrice=30,PracticalPrice=60,PassScore=12,SpeedKmh=50,MaxFaults=5,SecondsLimit=1200;
        public bool RequireSeatbelt=true;
        public JobPoint Entry,Spawn;
        public List<JobPoint> Route=new List<JobPoint>();public List<int> Stops=new List<int>();
        [JsonIgnore] public bool Valid=>TheoryPrice>=0 && TheoryPrice<=10000 && PracticalPrice>=0 && PracticalPrice<=10000 && PassScore>=1 && PassScore<=15 && SpeedKmh>=20 && SpeedKmh<=100 && MaxFaults>=1 && MaxFaults<=20 && SecondsLimit>=120 && SecondsLimit<=7200 && Entry!=null && Entry.Valid && Spawn!=null && Spawn.Valid && Route!=null && Route.Count>=2 && Route.Count<=100 && Route.All(p=>p!=null && p.Valid) && Stops!=null && Stops.All(i=>i>=0 && i<Route.Count) && Stops.Distinct().Count()==Stops.Count;
    }
    public static class DmvDefaults
    {
        public static DmvOptions Load(){using(var s=typeof(DmvDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.Dmv.json"))using(var r=new System.IO.StreamReader(s))return JsonConvert.DeserializeObject<DmvOptions>(r.ReadToEnd());}
    }
    public sealed class DmvQuestion
    {
        public readonly string Text;public readonly string[] Answers;public readonly int Correct;
        public DmvQuestion(string text,int correct,params string[] answers){Text=text;Answers=answers;Correct=correct;}
    }
    public static class DmvQuestions
    {
        // Own game questions; no copying the PSRP question-page textures or answer layout.
        public static readonly DmvQuestion[] All={
            new DmvQuestion("Antes de sair com o carro, o que fazer?",1,"Acelerar antes de olhar","Ajustar-se, colocar cinto e observar o entorno","Ligar o pisca-alerta e acelerar","Dirigir olhando so o GPS"),
            new DmvQuestion("Semaforo vermelho exige:",2,"Buzinar e seguir","Acelerar se nao houver carro","Parar antes da faixa e aguardar sinal permitido","Passar pelo acostamento"),
            new DmvQuestion("Ao mudar de faixa, voce deve:",0,"Sinalizar, conferir espelhos/ponto cego e mudar com seguranca","Mudar e sinalizar depois","Frear no meio da faixa sem olhar","Usar o pisca-alerta"),
            new DmvQuestion("Uma ambulancia se aproxima com sirene. Como agir?",3,"Disputar espaco","Parar bruscamente em qualquer lugar","Seguir colado atras dela","Abrir passagem com seguranca"),
            new DmvQuestion("Na chuva, a conducao adequada e:",1,"Aumentar velocidade","Reduzir velocidade e aumentar distancia","Aproximar-se do carro da frente","Frear forte em toda curva"),
            new DmvQuestion("Pedestre atravessando na faixa:",0,"Tem prioridade; reduza e pare se necessario","Deve correr para o carro passar","So tem prioridade com buzina","Deve aguardar o carro acelerar"),
            new DmvQuestion("O cinto deve ser usado:",2,"Somente em rodovia","Somente pelo motorista","Por todos os ocupantes, inclusive em trajeto curto","Somente quando houver policia"),
            new DmvQuestion("Ao estacionar e sair do carro:",3,"Abra a porta sem olhar","Deixe o carro solto em declive","Pare na faixa de pedestre","Imobilize o carro e confira o trafego antes de abrir a porta"),
            new DmvQuestion("Distancia do veiculo da frente deve:",1,"Ser sempre minima","Permitir reagir e parar, aumentando com velocidade/chuva","Depender apenas do tamanho do carro","Ser compensada pela buzina"),
            new DmvQuestion("Se perder uma conversao indicada no GPS:",2,"Pare no meio do cruzamento","De marcha a re pela avenida","Siga com seguranca e refaca o caminho","Cruze todas as faixas imediatamente"),
            new DmvQuestion("Em placa de parada obrigatoria:",0,"Pare completamente e observe antes de prosseguir","Basta reduzir um pouco","Apenas buzine","Siga se estiver fazendo uma entrega"),
            new DmvQuestion("Usar o celular durante a conducao:",3,"Melhora a atencao","E seguro ao passar em cruzamentos","Dispensa observar os espelhos","Distrai; pare em local seguro para mexer nele"),
            new DmvQuestion("Antes de entrar numa curva:",1,"Acelere ao maximo","Ajuste a velocidade antes e mantenha controle","Puxe o freio de mao sempre","Feche os olhos para o sol"),
            new DmvQuestion("Ao ver obstaculo ou acidente a frente:",2,"Acelere para passar primeiro","Mude de faixa sem observar","Reduza, observe e sinalize a manobra necessaria","Fique colado no veiculo anterior"),
            new DmvQuestion("O limite de velocidade:",0,"E um teto; as condicoes podem exigir velocidade menor","Deve ser ultrapassado no exame","Nao vale quando o GPS pede pressa","Permite dirigir sem distancia de seguranca")};
        public static int Score(IList<int> answers)=>answers==null?0:answers.Select((a,i)=>i<All.Length && a==All[i].Correct?1:0).Sum();
    }
    public enum DmvStage { AwaitDriver,RightSignal,LeftSignal,Route,Parking,Exit,Report }
    public sealed class DmvDrive : ExtensibleData
    {
        public DmvStage Stage;public int Point,Faults;public float Seconds;
        public bool Valid(DmvOptions c)=>Enum.IsDefined(typeof(DmvStage),Stage) && Point>=0 && Point<=c.Route.Count && Faults>=0 && Faults<=10000 && Finite(Seconds) && Seconds>=0 && Seconds<=86400 && ((int)Stage<(int)DmvStage.Parking || Point==c.Route.Count);
        public static bool Finite(float v)=>!float.IsNaN(v) && !float.IsInfinity(v);
        public bool Failed(DmvOptions c)=>Faults>=c.MaxFaults || Seconds>=c.SecondsLimit;
        public bool Enter(bool driver,bool examiner)=>Move(DmvStage.AwaitDriver,DmvStage.RightSignal,driver && examiner);
        public bool Signal(bool right,bool left,bool belted,bool driver,bool examiner,DmvOptions c)
        {if(!driver || !examiner || c.RequireSeatbelt && !belted)return false;return Stage==DmvStage.RightSignal?Move(Stage,DmvStage.LeftSignal,right && !left):Stage==DmvStage.LeftSignal && Move(Stage,DmvStage.Route,left && !right);}
        public bool Checkpoint(bool driver,bool examiner,bool inTarget,bool speedAllowed,bool stoppedForRequiredTime,DmvOptions c)
        {if(Stage!=DmvStage.Route || !driver || !examiner || !inTarget || !speedAllowed || c.Stops.Contains(Point) && !stoppedForRequiredTime)return false;Point++;if(Point==c.Route.Count)Stage=DmvStage.Parking;return true;}
        public bool Park(bool stoppedInBayForTime)=>Move(DmvStage.Parking,DmvStage.Exit,stoppedInBayForTime);
        public bool Leave(bool outsideNearCar)=>Move(DmvStage.Exit,DmvStage.Report,outsideNearCar);
        bool Move(DmvStage from,DmvStage to,bool condition){if(Stage!=from || !condition)return false;Stage=to;return true;}
    }
    public sealed class DmvAttempt : ExtensibleData
    {
        public string Id;public bool Practical,Started;public int Price,Version=1;
        public DmvOptions Options;public List<int> Answers=new List<int>();public DmvDrive Drive=new DmvDrive();
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=100 && Price>=0 && Price<=10000 && Version==1 && Options!=null && Options.Valid && Answers!=null && Answers.Count<15 && Answers.All(a=>a>=0 && a<4) && Drive!=null && Drive.Valid(Options) && (Practical?Answers.Count==0:Drive.Point==0 && Drive.Faults==0 && Drive.Stage==DmvStage.AwaitDriver) && (Started || Answers.Count==0 && Drive.Point==0 && Drive.Faults==0 && Drive.Seconds==0 && Drive.Stage==DmvStage.AwaitDriver);
    }
    public sealed class DmvData : ExtensibleData
    {
        public bool TheoryPassed,Licensed;public int TheoryAttempts,PracticalAttempts,LastTheoryScore=-1;public string LastResult;
        public long Spent;public DmvAttempt Pending;public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>TheoryAttempts>=0 && PracticalAttempts>=0 && Spent>=0 && LastTheoryScore>=-1 && LastTheoryScore<=15 && (!Licensed || TheoryPassed) && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=120) && Receipts.Distinct().Count()==Receipts.Count && (Pending==null || Pending.Valid && Spent>=Pending.Price && !Licensed && (Pending.Practical?TheoryPassed && PracticalAttempts>0:!TheoryPassed && TheoryAttempts>0));
    }
    public static class DmvTransactions
    {
        static T Copy<T>(T value)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        static bool Apply(WalletData w,string receipt,Func<DmvData,int?> change,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || string.IsNullOrWhiteSpace(receipt) || receipt.Length>120 || w.Dmv!=null && !w.Dmv.Valid)return false;
            var old=w.Dmv;if(old?.Receipts.Contains(receipt)==true)return true;var next=old==null?new DmvData():Copy(old);int? delta=change(next);
            if(delta==null || (long)w.Bank+delta.Value<0 || (long)w.Bank+delta.Value>int.MaxValue || !next.Valid)return false;
            next.Receipts.Add(receipt);if(next.Receipts.Count>256)next.Receipts.RemoveAt(0);int bank=w.Bank;
            try{w.Dmv=next;w.Bank+=delta.Value;save();changed=true;return true;}catch{w.Dmv=old;w.Bank=bank;throw;}
        }
        public static bool Begin(WalletData w,DmvOptions c,bool practical,string id,Action save,out bool changed)
        {
            changed=false;if(c==null || !c.Valid || string.IsNullOrWhiteSpace(id) || id.Length>100)return false;int price=practical?c.PracticalPrice:c.TheoryPrice;
            return Apply(w,id+":begin",d=>{if(d.Pending!=null || d.Licensed || practical!=d.TheoryPassed || d.Spent>long.MaxValue-price || (practical?d.PracticalAttempts:d.TheoryAttempts)==int.MaxValue)return null;
                if(practical)d.PracticalAttempts++;else d.TheoryAttempts++;d.Spent+=price;d.Pending=new DmvAttempt{Id=id,Practical=practical,Price=price,Options=Copy(c)};return -price;},save,out changed);
        }
        public static bool Start(WalletData w,string id,Action save,out bool changed)=>Apply(w,id+":start",d=>{if(d.Pending==null || d.Pending.Id!=id)return null;d.Pending.Started=true;return 0;},save,out changed);
        public static bool Answer(WalletData w,string id,int question,int answer,Action save,out bool changed)
        {changed=false;if(question<0 || question>=15 || answer<0 || answer>=4)return false;return Apply(w,id+":answer:"+question,d=>{var p=d.Pending;if(p==null || p.Id!=id || p.Practical || !p.Started || p.Answers.Count!=question)return null;p.Answers.Add(answer);if(p.Answers.Count==15){d.LastTheoryScore=DmvQuestions.Score(p.Answers);d.TheoryPassed=d.LastTheoryScore>=p.Options.PassScore;d.LastResult=d.TheoryPassed?"theory_pass":"theory_fail";d.Pending=null;}return 0;},save,out changed);}
        public static bool Progress(WalletData w,string id,DmvDrive progress,Action save,out bool changed)
        {
            return Apply(w,Guid.NewGuid().ToString("N")+":progress",d=>{
                var p=d.Pending;if(p==null || p.Id!=id || !p.Practical || !p.Started || progress==null || !progress.Valid(p.Options))return null;var old=p.Drive;
                if(progress.Point<old.Point || progress.Point>old.Point+1 || progress.Faults<old.Faults || progress.Seconds<old.Seconds || (int)progress.Stage<(int)old.Stage || (int)progress.Stage>(int)old.Stage+1 || progress.Point>old.Point && old.Stage!=DmvStage.Route)return null;
                if(old.Stage==DmvStage.Route && progress.Stage==DmvStage.Parking && progress.Point!=p.Options.Route.Count)return null;
                p.Drive=Copy(progress);return 0;
            },save,out changed);
        }
        public static bool Restart(WalletData w,string id,Action save,out bool changed)=>Apply(w,Guid.NewGuid().ToString("N")+":restart",d=>{var p=d.Pending;if(p==null || p.Id!=id || !p.Practical || !p.Started || p.Drive.Stage==DmvStage.Report)return null;p.Drive.Stage=DmvStage.AwaitDriver;return 0;},save,out changed);
        public static bool Finish(WalletData w,string id,bool passed,Action save,out bool changed)=>Apply(w,id+":finish",d=>{var p=d.Pending;if(p==null || p.Id!=id || !p.Practical || !p.Started || passed && (p.Drive.Stage!=DmvStage.Report || p.Drive.Point!=p.Options.Route.Count || p.Drive.Failed(p.Options)))return null;d.Licensed=passed;d.LastResult=passed?"practical_pass":"practical_fail";d.Pending=null;return 0;},save,out changed);
        public static bool Cancel(WalletData w,string id,Action save,out bool changed)=>Apply(w,id+":cancel",d=>{var p=d.Pending;if(p==null || p.Id!=id || p.Started)return null;int amount=p.Price;d.Spent-=amount;if(p.Practical)d.PracticalAttempts--;else d.TheoryAttempts--;d.Pending=null;return amount;},save,out changed);
        public static bool Abandon(WalletData w,string id,Action save,out bool changed)=>Apply(w,id+":abandon",d=>{if(d.Pending==null || d.Pending.Id!=id || !d.Pending.Started)return null;d.LastResult="abandoned";d.Pending=null;return 0;},save,out changed);
    }
}
