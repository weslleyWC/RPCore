using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
namespace RPCore
{
    public sealed class DmvSystem : ISystem
    {
        public string Name=>"dmv";
        readonly ObjectPool pool;readonly ShiftGate gate;readonly Func<bool> belted;readonly DmvOptions options;readonly NativeMenu menu;readonly Blip office;
        Vehicle car;Ped examiner;Blip route;Model carModel,examinerModel;bool preparing,lease;string owner,attempt;
        DmvDrive drive;DmvOptions examOptions;int last,saveAt,prepareAt,answerAt;float stopTime,speedTime,beltTime,damage;Vector3 previous;DmvStage routedStage;int routedPoint=-1;
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        bool Calm=>!Game.IsPaused && !Game.IsMissionActive && !InterfaceOptions.ModalOpen && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && !Game.Player.Character.IsDead && !Game.Player.Character.IsInCombat && Game.Player.Wanted.WantedLevel==0;
        bool AtDesk=>Calm && !Game.Player.Character.IsInVehicle() && Game.Player.Character.Position.DistanceTo(Pos(options.Entry))<=3;
        bool Driver=>car!=null && car.Exists() && car.Driver==Game.Player.Character && Game.Player.Character.IsInVehicle(car);
        bool ExaminerSeated=>examiner!=null && examiner.Exists() && !examiner.IsDead && examiner.IsInVehicle(car) && car.GetPedOnSeat(VehicleSeat.Passenger)==examiner;
        bool Running=>preparing || drive!=null;
        public DmvSystem(ObjectPool pool,ShiftGate gate,Func<bool> belted)
        {
            this.pool=pool;this.gate=gate;this.belted=belted;var defaults=DmvDefaults.Load();options=Store.LoadDef("Jobs/Dmv.json",()=>defaults);
            if(options==null || !options.Valid){Log.Write("Invalid Dmv.json; embedded defaults used, original retained.");options=defaults;}
            menu=new NativeMenu("DMV / habilitacao C","TEORIA / EXAME PRATICO");pool.Add(menu);Visuals.Register(menu,"core");office=MapBlips.Create(Pos(options.Entry),"RPCore: DMV / habilitacao C",BlipSprite.PropertyBikers,BlipColor.White,.8f);
        }
        void Add(string title,string description,Action action)
        {var item=new NativeItem(title,description);item.Activated+=(s,e)=>{try{action();}catch(Exception ex){EndRuntime();menu.Visible=false;Log.Error("dmv action",ex);Notify.Show("Exame nao concluido. Dados/pedido preservados; retome no DMV sem nova taxa.");}};menu.Add(item);}
        bool Acquire(){if(!gate.Acquire(Name)){Notify.Show("Encerre a atividade de "+gate.Owner+".");return false;}lease=true;owner=Profile.Id;return true;}
        public void OpenMenu()
        {
            if(!Wallet.Dmv.Valid){Notify.Show("Documentos DMV invalidos; original preservado.");return;}
            pool.HideAll();menu.Clear();menu.Name="Classe C / banco $"+Wallet.Bank;
            Add("GPS: DMV","Atendimento presencial. Nao bloqueia seus trabalhos/veiculos existentes por falta de documento.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,options.Entry.X,options.Entry.Y);menu.Visible=false;});
            if(Running){
                if(drive?.Stage==DmvStage.Report)Add("Receber habilitacao aprovada","A pe no DMV, sem nova taxa.",()=>Issue(attempt));
                Add("Continuar exame","Fecha menu e continua a avaliacao.",()=>menu.Visible=false);
                if(!Cfg.EnableVehicleControls){Add("Demonstrar seta direita","Motorista do carro de exame, parado.",()=>Signal(false));Add("Demonstrar seta esquerda","Motorista do carro de exame, parado.",()=>Signal(true));}
                Add("Suspender / retomar depois","Conserva pagamento/progresso ja gravados. Libera o carro somente quando vazio; retomada presencial prepara outro carro.",()=>{Save();EndRuntime();menu.Visible=false;});
            }
            else{
                var p=Wallet.Dmv.Pending;
                if(p!=null){string id=p.Id;Add(p.Practical?(p.Drive.Stage==DmvStage.Report?"Receber habilitacao aprovada":"Retomar exame pratico pago"):"Retomar prova teorica paga","No DMV, a pe. Sem cobrar outra vez.",()=>{if(!AtDesk){Notify.Show("Va a pe ao DMV, sem procurado/missao.");return;}if(p.Practical){if(p.Drive.Stage==DmvStage.Report)Issue(id);else Prepare();}else Theory(id);});
                    if(!p.Started)Add("Cancelar e estornar $"+p.Price,"Antes da autorizacao de inicio; devolve taxa e tentativa.",()=>{if(!AtDesk)return;if(!Wallet.DmvCancel(id,out _))Notify.Show("Confira estado e limite do banco.");OpenMenu();});
                    else Add("Abandonar tentativa sem estorno","Abre confirmacao; perde a tentativa pendente, mantendo certificado anterior.",()=>ConfirmAbandon(id));
                }
                else if(!Wallet.Dmv.TheoryPassed)Add("Prova teorica C / $"+options.TheoryPrice,"15 questoes proprias; minimo "+options.PassScore+" acertos. Taxa do banco. Atendimento a pe no DMV.",()=>{
                    if(!AtDesk){Notify.Show("Va a pe ao DMV.");return;}if(!Acquire())return;string id=Guid.NewGuid().ToString("N");
                    if(!Wallet.DmvBegin(options,false,id,out _)){EndRuntime();Notify.Show("Confira saldo/pendencia. Nada iniciado.");return;}Theory(id);
                });
                else if(!Wallet.Dmv.Licensed)Add("Exame pratico C / $"+options.PracticalPrice,"Asterope e examinadora, setas/cinto, "+options.Route.Count+" pontos, paradas e estacionamento. Taxa apenas com veiculo/equipe preparados.",Prepare);
            }
            Add("Teoria: "+(Wallet.Dmv.TheoryPassed?"APROVADA":"pendente")+" / C: "+(Wallet.Dmv.Licensed?"EMITIDA":"pendente"),"Tentativas teoria "+Wallet.Dmv.TheoryAttempts+", pratico "+Wallet.Dmv.PracticalAttempts+" / taxas $"+Wallet.Dmv.Spent+". Ultima nota: "+Wallet.Dmv.LastTheoryScore+"/15.",()=>{});
            Add("Fechar","Prova teorica em andamento fica retomavel no DMV.",()=>{if(drive==null && !preparing)EndRuntime();menu.Visible=false;});menu.Visible=true;
        }
        void Theory(string id)
        {
            if(!AtDesk)return;if(!lease && !Acquire())return;attempt=id;
            if(!Wallet.Dmv.Pending.Started && !Wallet.DmvStart(id,out _))throw new InvalidOperationException("Theory start not saved.");Quiz();
        }
        void Quiz()
        {
            var p=Wallet.Dmv.Pending;if(p==null || p.Practical){EndRuntime();OpenMenu();return;}
            int index=p.Answers.Count;var q=DmvQuestions.All[index];menu.Clear();menu.Name="Questao "+(index+1)+"/15 / "+q.Text;
            for(int i=0;i<4;i++){int answer=i;Add(((char)('A'+i))+": "+q.Answers[i],q.Text,()=>{
                if(!AtDesk || unchecked(Game.GameTime-answerAt)<500)return;
                if(!Wallet.DmvAnswer(p.Id,index,answer,out _))throw new InvalidOperationException("Answer not saved.");answerAt=Game.GameTime;
                if(Wallet.Dmv.Pending==null){int score=Wallet.Dmv.LastTheoryScore;bool pass=Wallet.Dmv.TheoryPassed;EndRuntime();OpenMenu();Notify.Show("Teoria: "+score+"/15 / "+(pass?"aprovado; pratico liberado.":"reprovado; nova tentativa tem taxa."));}else Quiz();
            });}
            Add("Suspender prova","Guarda respostas ja confirmadas; retome no DMV sem cobrar novamente.",()=>{EndRuntime();menu.Visible=false;});menu.Visible=true;
        }
        void Prepare()
        {
            if(Running || !AtDesk || !Wallet.Dmv.Valid || !Wallet.Dmv.TheoryPassed || Wallet.Dmv.Licensed){Notify.Show("Conclua teoria e contrate a pe no DMV.");return;}
            var p=Wallet.Dmv.Pending;if(p!=null && !p.Practical)return;if(p==null && Wallet.Bank<options.PracticalPrice){Notify.Show("Saldo bancario insuficiente.");return;}
            if(!Acquire())return;examOptions=p?.Options??options;attempt=p?.Id??Guid.NewGuid().ToString("N");
            carModel=new Model("asterope");examinerModel=new Model("a_f_y_bevhills_01");
            if(!carModel.IsInCdImage || !examinerModel.IsInCdImage){EndRuntime();Notify.Show("Modelos de exame indisponiveis; nenhuma taxa nova.");return;}
            Function.Call(Hash.REQUEST_MODEL,carModel.Hash);Function.Call(Hash.REQUEST_MODEL,examinerModel.Hash);preparing=true;prepareAt=Game.GameTime;menu.Visible=false;
            Notify.Show("Preparando exame; aguarde a pe no DMV. Nao cobra se a area/modelo estiver indisponivel.");
        }
        void Spawn()
        {
            Vector3 spawn=Pos(examOptions.Spawn);
            if(!JobActors.Free(spawn,5)){EndRuntime();Notify.Show("Vaga do exame ocupada. Libere a area; nenhuma taxa nova.");return;}
            car=World.CreateVehicle(carModel,spawn,examOptions.Spawn.Heading);if(car==null || !car.Exists())throw new InvalidOperationException("Exam car failed.");car.IsPersistent=true;JobVehicles.Reserve(car);
            Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT,car,"RPDM"+Guid.NewGuid().ToString("N").Substring(0,4).ToUpperInvariant());
            if(!car.PlaceOnGround())throw new InvalidOperationException("Exam parking ground not ready.");Function.Call(Hash.SET_VEHICLE_ENGINE_ON,car,false,true,false);car.FuelLevel=Math.Max(10,Math.Min(10000,car.PetrolTankVolume));
            examiner=World.CreatePed(examinerModel,spawn+new Vector3(1,0,1));if(examiner==null || !examiner.Exists())throw new InvalidOperationException("Examiner failed.");examiner.IsPersistent=true;examiner.BlockPermanentEvents=true;examiner.RelationshipGroup=Game.Player.Character.RelationshipGroup;examiner.SetIntoVehicle(car,VehicleSeat.Passenger);if(!ExaminerSeated)throw new InvalidOperationException("Examiner not seated.");
            if(Wallet.Dmv.Pending==null && !Wallet.DmvBegin(examOptions,true,attempt,out _)){EndRuntime();Notify.Show("Saldo/dados mudaram; exame nao cobrado.");return;}
            if(!Wallet.Dmv.Pending.Started){if(!Wallet.DmvStart(attempt,out _))throw new InvalidOperationException("Practical start not saved.");}
            else if(!Wallet.DmvRestart(attempt,out _))throw new InvalidOperationException("Practical resume not saved.");
            drive=CharacterRules.Copy(Wallet.Dmv.Pending.Drive);preparing=false;last=Game.GameTime;saveAt=last+1000;previous=car.Position;damage=car.BodyHealth+car.EngineHealth;routedPoint=-1;
            Route();Notify.Show("Entre como motorista, cinto "+Cfg.SeatbeltKey+", seta direita "+Cfg.RightSignalKey+", depois esquerda "+Cfg.LeftSignalKey+". Motor: "+Cfg.EngineKey+". END suspende.");
        }
        Vector3 Target=>drive==null?Pos(options.Entry):drive.Stage==DmvStage.Report?Pos(options.Entry):drive.Stage==DmvStage.Route && drive.Point<examOptions.Route.Count?Pos(examOptions.Route[drive.Point]):Pos(examOptions.Spawn);
        void Route(){if(route!=null && route.Exists())route.Delete();route=World.CreateBlip(Target);route.Name="RPCore DMV: "+(drive.Stage==DmvStage.Route?"etapa "+(drive.Point+1):(drive.Stage==DmvStage.AwaitDriver?"entrar no carro":drive.Stage==DmvStage.RightSignal?"seta direita":drive.Stage==DmvStage.LeftSignal?"seta esquerda":drive.Stage==DmvStage.Parking?"estacionar":drive.Stage==DmvStage.Exit?"desembarcar":"retornar ao DMV"));route.Color=BlipColor.Yellow;route.ShowRoute=true;routedStage=drive.Stage;routedPoint=drive.Point;}
        void Signal(bool left){if(!Driver || car.Speed>.5f)return;Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS,car,0,!left);Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS,car,1,left);menu.Visible=false;}
        void ConfirmAbandon(string id){menu.Clear();menu.Name="Abandonar sem estorno?";Add("Sim, abandonar tentativa","Conserva certificado/licencas ja emitidos, sem devolver taxa desta tentativa.",()=>{if(!AtDesk)return;if(!Wallet.DmvAbandon(id,out _))throw new InvalidOperationException("Abandon not saved.");EndRuntime();OpenMenu();});Add("Voltar","Manter pendencia.",OpenMenu);menu.Visible=true;}
        void Issue(string id){if(!AtDesk)return;if(!Wallet.DmvFinish(id,true,out _) || !Wallet.Dmv.Licensed)throw new InvalidOperationException("License not saved.");EndRuntime();OpenMenu();Notify.Show("Habilitacao classe C emitida neste perfil. Sem nova cobranca.");}
        void Fail(string reason){if(!Wallet.DmvFinish(attempt,false,out _))throw new InvalidOperationException("Failed result not saved.");EndRuntime();Notify.Show("Exame reprovado: "+reason+". Certificado teorico mantido; nova tentativa tem taxa.");}
        void DriveTick()
        {
            if(owner!=Profile.Id){EndRuntime();return;}
            if(Game.IsPaused){last=Game.GameTime;return;}
            if(Game.IsMissionActive || Game.Player.Wanted.WantedLevel>0 || Game.Player.Character.IsDead || car==null || !car.Exists() || car.IsDead || examiner==null || !examiner.Exists() || examiner.IsDead){Fail("exame interrompido/veiculo ou examinadora indisponivel");return;}
            float dt=Math.Min(1,Math.Max(0,unchecked(Game.GameTime-last)/1000f));last=Game.GameTime;drive.Seconds+=dt;
            if(car.Position.DistanceTo(previous)>Math.Max(80,car.Speed*dt+30)){Fail("deslocamento discontinuo do veiculo");return;}previous=car.Position;
            if(drive.Stage!=DmvStage.Report && Game.Player.Character.Position.DistanceTo(car.Position)>100){Fail("abandono do veiculo de exame");return;}
            if((int)drive.Stage>=(int)DmvStage.RightSignal && (int)drive.Stage<=(int)DmvStage.Parking && (!ExaminerSeated || Game.Player.Character.IsInVehicle() && !Driver)){Fail("use o carro correto com a examinadora no banco passageiro");return;}
            bool fault=false;float currentDamage=car.BodyHealth+car.EngineHealth;if(damage-currentDamage>=25){drive.Faults++;fault=true;damage=currentDamage;}if(currentDamage>damage)damage=currentDamage;
            if(Driver && drive.Stage!=DmvStage.Report){
                if(car.Speed*3.6f>examOptions.SpeedKmh+5)speedTime+=dt;else speedTime=0;
                if(examOptions.RequireSeatbelt && !belted())beltTime+=dt;else beltTime=0;
                if(speedTime>=3){drive.Faults++;speedTime=0;fault=true;}
                if(beltTime>=5){drive.Faults++;beltTime=0;fault=true;}
            }
            if(car.EngineHealth<400 || car.BodyHealth<500){Fail("danos graves");return;}
            if(drive.Failed(examOptions)){Save();Fail("limite de faltas ou tempo");return;}
            var stage=drive.Stage;int point=drive.Point;
            bool inTarget=car.Position.DistanceTo2D(Target)<=7 && Math.Abs(car.Position.Z-Target.Z)<=4;
            stopTime=Driver && inTarget && car.Speed<=.5f?stopTime+dt:0;
            switch(drive.Stage){
                case DmvStage.AwaitDriver:drive.Enter(Driver,ExaminerSeated);break;
                case DmvStage.RightSignal:case DmvStage.LeftSignal:drive.Signal(car.IsRightIndicatorLightOn,car.IsLeftIndicatorLightOn,belted(),Driver,ExaminerSeated,examOptions);break;
                case DmvStage.Route:if(drive.Point==examOptions.Route.Count)drive.Stage=DmvStage.Parking;else drive.Checkpoint(Driver,ExaminerSeated,inTarget && (!examOptions.RequireSeatbelt || belted()),car.Speed*3.6f<=examOptions.SpeedKmh,stopTime>=1,examOptions);break;
                case DmvStage.Parking:drive.Park(stopTime>=2);break;
                case DmvStage.Exit:drive.Leave(!Game.Player.Character.IsInVehicle() && car.Speed<.5f && Game.Player.Character.Position.DistanceTo(car.Position)<12);break;
                case DmvStage.Report:if(AtDesk && !pool.AreAnyVisible){GTA.UI.Screen.ShowHelpTextThisFrame("F10 > DMV > Receber habilitacao aprovada");}break;
            }
            if(stage!=drive.Stage || point!=drive.Point){stopTime=0;Save();Route();}
            else if(fault || unchecked(Game.GameTime-saveAt)>=0)Save();
            if(routedStage!=drive.Stage || routedPoint!=drive.Point)Route();
            string task=drive.Stage==DmvStage.AwaitDriver?"entre como motorista":drive.Stage==DmvStage.RightSignal?"cinto e seta DIREITA":drive.Stage==DmvStage.LeftSignal?"seta ESQUERDA":drive.Stage==DmvStage.Parking?"retorne e estacione por 2s":drive.Stage==DmvStage.Exit?"retire cinto e saia do carro":drive.Stage==DmvStage.Report?"volte a pe ao DMV para emitir":examOptions.Stops.Contains(drive.Point)?"pare por 1s no ponto marcado":"siga o ponto marcado";
            GTA.UI.Screen.ShowSubtitle("DMV C / "+task+" / "+drive.Point+"/"+examOptions.Route.Count+" / faltas "+drive.Faults+"/"+examOptions.MaxFaults+" / limite "+examOptions.SpeedKmh+" km/h",100);
            if(drive.Stage==DmvStage.Route || drive.Stage==DmvStage.Parking)World.DrawMarker(MarkerType.Cylinder,Target-new Vector3(0,0,.8f),Vector3.Zero,Vector3.Zero,new Vector3(5,5,1),Color.FromArgb(100,255,210,0));
        }
        public void Tick()
        {
            try{
                if(preparing){if(owner!=Profile.Id || !AtDesk || unchecked(Game.GameTime-prepareAt)>10000){EndRuntime();Notify.Show("Preparacao interrompida; nenhuma taxa nova.");return;}if(carModel.IsLoaded && examinerModel.IsLoaded)Spawn();return;}
                if(drive!=null){DriveTick();return;}
                if(lease && Wallet.Dmv.Pending?.Practical==false && (!menu.Visible || !AtDesk)){EndRuntime();menu.Visible=false;}
                if(Calm && !pool.AreAnyVisible && Game.Player.Character.Position.DistanceTo(Pos(options.Entry))<3)GTA.UI.Screen.ShowHelpTextThisFrame("F10 > DMV / habilitacao C");
            }catch(Exception ex){EndRuntime();menu.Visible=false;Log.Error("dmv tick",ex);Notify.Show("Exame suspenso por falha. Pedido/progresso gravado conservado; retome no DMV.");}
        }
        public void Save(){if(drive==null || owner!=Profile.Id)return;if(!Wallet.DmvProgress(attempt,drive,out _))throw new InvalidOperationException("Practical progress not saved.");saveAt=Game.GameTime+1000;}
        void EndRuntime(){if(route!=null && route.Exists())route.Delete();route=null;if(examiner!=null && examiner.Exists())JobActors.Retire(examiner);examiner=null;if(car!=null && car.Exists())JobVehicles.RetireRig(car,null);car=null;if(carModel.Hash!=0)carModel.MarkAsNoLongerNeeded();if(examinerModel.Hash!=0)examinerModel.MarkAsNoLongerNeeded();carModel=examinerModel=default(Model);drive=null;preparing=false;attempt=null;owner=null;stopTime=speedTime=beltTime=0;if(lease)gate.Release(Name);lease=false;}
        public void OnKey(KeyEventArgs e){if(e.KeyCode==Keys.End && Running){try{Save();}finally{EndRuntime();menu.Visible=false;}Notify.Show("Exame suspenso; retome no DMV sem nova taxa.");}}
        public void Reload(){EndRuntime();menu.Visible=false;}
        public void Dispose()
        {
            // The global job cleanup may already have run during script abort. Release only our actors here.
            var ownCar=car;
            if(examiner!=null && examiner.Exists())examiner.Delete();examiner=null;
            EndRuntime();menu.Visible=false;
            if(ownCar!=null && ownCar.Exists()){
                if(ownCar.Occupants.Length==0 && ownCar.Speed<.5f && !Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,ownCar) && !Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER,ownCar))ownCar.Delete();
                if(ownCar.Exists())ownCar.MarkAsNoLongerNeeded();
            }
            if(office!=null && office.Exists())office.Delete();
        }
    }
}
