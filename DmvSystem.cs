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
        readonly ObjectPool pool;readonly ShiftGate gate;readonly Func<bool> belted;readonly DmvOptions carOptions,motorcycleOptions,truckOptions;DmvClass category;readonly NativeMenu menu;readonly Blip office,truckOffice;
        Vehicle car,trailer;JobPoint bay;Model trailerModel;float reverseDistance,disconnected;Vector3 previousTrailer;Ped examiner;Blip route;Model carModel,examinerModel;bool preparing,lease;string owner,attempt;
        DmvDrive drive;DmvOptions examOptions;int last,saveAt,prepareAt,answerAt;float stopTime,speedTime,beltTime,damage;Vector3 previous;DmvStage routedStage;int routedPoint=-1;
        DmvData Book=>category==DmvClass.C?Wallet.Dmv:category==DmvClass.M1?Wallet.DmvM1:Wallet.DmvA;
        DmvOptions options=>category==DmvClass.C?carOptions:category==DmvClass.M1?motorcycleOptions:truckOptions;
        bool Truck=>category==DmvClass.A;
        bool Towing=>JobVehicles.Towing(car,trailer);
        bool InBay=>trailer!=null && trailer.Exists() && bay!=null && DmvRigRules.InBay(trailer.Position.DistanceTo2D(Pos(bay)),trailer.Position.Z-bay.Z,trailer.Heading,bay.Heading,car.Speed,trailer.Speed);
        bool Moto=>category==DmvClass.M1;
        string ClassName=>Truck?"A":Moto?"M1":"C";
        bool Protection=>Moto?Function.Call<bool>(Hash.IS_PED_WEARING_HELMET,Game.Player.Character):belted();
        float SpeedLimit=>examOptions!=null && drive!=null && drive.Stage==DmvStage.Route?examOptions.SpeedAt(drive.Point):examOptions?.SpeedKmh??options.SpeedKmh;
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        bool Calm=>!Game.IsPaused && !Game.IsMissionActive && !InterfaceOptions.ModalOpen && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && !Game.Player.Character.IsDead && !Game.Player.Character.IsInCombat && Game.Player.Wanted.WantedLevel==0;
        bool AtDesk=>Calm && !Game.Player.Character.IsInVehicle() && Game.Player.Character.Position.DistanceTo(Pos(options.Entry))<=3;
        bool Driver=>car!=null && car.Exists() && car.Driver==Game.Player.Character && Game.Player.Character.IsInVehicle(car);
        bool ExaminerSeated=>examiner!=null && examiner.Exists() && !examiner.IsDead && examiner.IsInVehicle(car) && car.GetPedOnSeat(VehicleSeat.Passenger)==examiner;
        bool Running=>preparing || drive!=null;
        public DmvSystem(ObjectPool pool,ShiftGate gate,Func<bool> belted)
        {
            this.pool=pool;this.gate=gate;this.belted=belted;
            var defaults=DmvDefaults.Load();carOptions=Store.LoadDef("Jobs/Dmv.json",()=>defaults);
            if(carOptions==null || !carOptions.Valid || carOptions.Category!=DmvClass.C){Log.Write("Invalid Dmv.json; embedded defaults used, original retained.");carOptions=defaults;}
            var motoDefaults=DmvDefaults.Motorcycle();motorcycleOptions=Store.LoadDef("Jobs/DmvM1.json",()=>motoDefaults);
            if(motorcycleOptions==null || !motorcycleOptions.Valid || motorcycleOptions.Category!=DmvClass.M1){Log.Write("Invalid DmvM1.json; embedded defaults used, original retained.");motorcycleOptions=motoDefaults;}
            var truckDefaults=DmvDefaults.Truck();truckOptions=Store.LoadDef("Jobs/DmvA.json",()=>truckDefaults);
            if(truckOptions==null || !truckOptions.Valid || truckOptions.Category!=DmvClass.A){Log.Write("Invalid DmvA.json; embedded defaults used, original retained.");truckOptions=truckDefaults;}
            truckOffice=MapBlips.Create(Pos(truckOptions.Entry),"RPCore: DMV A / caminhoes",BlipSprite.PropertyBikers,BlipColor.White,.8f);
            menu=new NativeMenu("DMV / habilitacoes","TEORIA / EXAME PRATICO");pool.Add(menu);Visuals.Register(menu,"core");office=MapBlips.Create(Pos(options.Entry),"RPCore: DMV / habilitacoes C e M1",BlipSprite.PropertyBikers,BlipColor.White,.8f);
        }
        void Add(string title,string description,Action action)
        {var item=new NativeItem(title,description);item.Activated+=(s,e)=>{try{action();}catch(Exception ex){EndRuntime();menu.Visible=false;Log.Error("dmv action",ex);Notify.Show("Exame nao concluido. Dados/pedido preservados; retome no DMV sem nova taxa.");}};menu.Add(item);}
        bool Acquire(){if(!gate.Acquire(Name)){Notify.Show("Encerre a atividade de "+gate.Owner+".");return false;}lease=true;owner=Profile.Id;return true;}
        public void OpenMenu()
        {
            pool.HideAll();menu.Clear();menu.Name="Classe "+ClassName+" / banco $"+Wallet.Bank;
            if(!Running && !lease)Add("Trocar para classe "+(category==DmvClass.C?"M1 / motos":Moto?"A / caminhoes":"C / carros"),"Documentos, provas e pagamentos separados; mantem pendencias da outra classe.",()=>{category=category==DmvClass.C?DmvClass.M1:Moto?DmvClass.A:DmvClass.C;OpenMenu();});
            if(!Book.Valid || Book.Pending!=null && Book.Pending.Options.Category!=category){
                Add("Documentos desta classe protegidos","Dados invalidos conservados. Pode trocar de classe sem alterar estes documentos.",()=>Notify.Show("Confira Wallet.json e backup; documentos preservados."));
                Add("Fechar","Voltar ao jogo.",()=>menu.Visible=false);menu.Visible=true;return;
            }
            Add("GPS: DMV","Atendimento presencial. Nao bloqueia seus trabalhos/veiculos existentes por falta de documento.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,options.Entry.X,options.Entry.Y);menu.Visible=false;});
            if(Running){
                if(Truck)Add("Desengatar carreta do exame","Apos concluir a re e alinhar na vaga, conjunto parado. Atua apenas na carreta deste exame.",Uncouple);
                if(Moto)Add("Vestir capacete de exame","Condutor da moto, parado. Opcional: guarda seu adereco atual e restaura ao encerrar se ele nao foi trocado por outro sistema.",EquipHelmet);
                if(drive?.Stage==DmvStage.Report)Add("Receber habilitacao aprovada","A pe no DMV, sem nova taxa.",()=>Issue(attempt));
                Add("Continuar exame","Fecha menu e continua a avaliacao.",()=>menu.Visible=false);
                if(!Cfg.EnableVehicleControls){Add("Demonstrar seta direita","Condutor do veiculo de exame, parado.",()=>Signal(false));Add("Demonstrar seta esquerda","Condutor do veiculo de exame, parado.",()=>Signal(true));}
                Add("Suspender / retomar depois","Conserva pagamento/progresso ja gravados. Libera o carro somente quando vazio; retomada presencial prepara outro carro.",()=>{Save();EndRuntime();menu.Visible=false;});
            }
            else{
                var p=Book.Pending;
                if(p!=null){string id=p.Id;Add(p.Practical?(p.Drive.Stage==DmvStage.Report?"Receber habilitacao aprovada":"Retomar exame pratico pago"):"Retomar prova teorica paga","No DMV, a pe. Sem cobrar outra vez.",()=>{if(!AtDesk){Notify.Show("Va a pe ao DMV, sem procurado/missao.");return;}if(p.Practical){if(p.Drive.Stage==DmvStage.Report)Issue(id);else Prepare();}else Theory(id);});
                    if(!p.Started)Add("Cancelar e estornar $"+p.Price,"Antes da autorizacao de inicio; devolve taxa e tentativa.",()=>{if(!AtDesk)return;if(!Wallet.DmvCancel(id,out _,category))Notify.Show("Confira estado e limite do banco.");OpenMenu();});
                    else Add("Abandonar tentativa sem estorno","Abre confirmacao; perde a tentativa pendente, mantendo certificado anterior.",()=>ConfirmAbandon(id));
                }
                else if(!Book.TheoryPassed)Add("Prova teorica "+ClassName+" / $"+options.TheoryPrice,"15 questoes proprias; minimo "+options.PassScore+" acertos. Taxa do banco. Atendimento a pe no DMV.",()=>{
                    if(!AtDesk){Notify.Show("Va a pe ao DMV.");return;}if(!Acquire())return;string id=Guid.NewGuid().ToString("N");
                    if(!Wallet.DmvBegin(options,false,id,out _,category)){EndRuntime();Notify.Show("Confira saldo/pendencia. Nada iniciado.");return;}Theory(id);
                });
                else if(!Book.Licensed)Add("Exame pratico "+ClassName+" / $"+options.PracticalPrice,(Truck?"Phantom e carreta, engate manual, cinto/setas, re, alinhamento e desengate, ":Moto?"PCJ 600 e examinadora, capacete/setas/baixa velocidade, ":"Asterope e examinadora, setas/cinto, ")+options.Route.Count+" pontos, paradas e estacionamento. Taxa apenas com veiculo/equipe preparados.",Prepare);
            }
            Add("Teoria: "+(Book.TheoryPassed?"APROVADA":"pendente")+" / "+ClassName+": "+(Book.Licensed?"EMITIDA":"pendente"),"Tentativas teoria "+Book.TheoryAttempts+", pratico "+Book.PracticalAttempts+" / taxas $"+Book.Spent+". Ultima nota: "+Book.LastTheoryScore+"/15.",()=>{});
            Add("Fechar","Prova teorica em andamento fica retomavel no DMV.",()=>{if(drive==null && !preparing)EndRuntime();menu.Visible=false;});menu.Visible=true;
        }
        void Theory(string id)
        {
            if(!AtDesk)return;if(!lease && !Acquire())return;attempt=id;
            if(!Book.Pending.Started && !Wallet.DmvStart(id,out _,category))throw new InvalidOperationException("Theory start not saved.");Quiz();
        }
        void Quiz()
        {
            var p=Book.Pending;if(p==null || p.Practical){EndRuntime();OpenMenu();return;}
            int index=p.Answers.Count;var q=DmvQuestions.For(category)[index];menu.Clear();menu.Name="Questao "+(index+1)+"/15 / "+q.Text;
            for(int i=0;i<4;i++){int answer=i;Add(((char)('A'+i))+": "+q.Answers[i],q.Text,()=>{
                if(!AtDesk || unchecked(Game.GameTime-answerAt)<500)return;
                if(!Wallet.DmvAnswer(p.Id,index,answer,out _,category))throw new InvalidOperationException("Answer not saved.");answerAt=Game.GameTime;
                if(Book.Pending==null){int score=Book.LastTheoryScore;bool pass=Book.TheoryPassed;EndRuntime();OpenMenu();Notify.Show("Teoria: "+score+"/15 / "+(pass?"aprovado; pratico liberado.":"reprovado; nova tentativa tem taxa."));}else Quiz();
            });}
            Add("Suspender prova","Guarda respostas ja confirmadas; retome no DMV sem cobrar novamente.",()=>{EndRuntime();menu.Visible=false;});menu.Visible=true;
        }
        void Prepare()
        {
            if(Running || !AtDesk || !Book.Valid || !Book.TheoryPassed || Book.Licensed){Notify.Show("Conclua teoria e contrate a pe no DMV.");return;}
            var p=Book.Pending;if(p!=null && !p.Practical)return;if(p==null && Wallet.Bank<options.PracticalPrice){Notify.Show("Saldo bancario insuficiente.");return;}
            if(!Acquire())return;examOptions=CharacterRules.Copy(p?.Options??options);attempt=p?.Id??Guid.NewGuid().ToString("N");
            carModel=new Model(Truck?"phantom":Moto?"pcj":"asterope");if(Truck)trailerModel=new Model("trailers2");examinerModel=new Model("a_f_y_bevhills_01");
            if(!carModel.IsInCdImage || !examinerModel.IsInCdImage || Truck && !trailerModel.IsInCdImage){EndRuntime();Notify.Show("Modelos de exame indisponiveis; nenhuma taxa nova.");return;}
            if(Truck)Function.Call(Hash.REQUEST_MODEL,trailerModel.Hash);Function.Call(Hash.REQUEST_MODEL,carModel.Hash);Function.Call(Hash.REQUEST_MODEL,examinerModel.Hash);preparing=true;prepareAt=Game.GameTime;menu.Visible=false;
            Notify.Show("Preparando exame; aguarde a pe no DMV. Nao cobra se a area/modelo estiver indisponivel.");
        }
        void Spawn()
        {
            if(Truck){
                if(examOptions.SnapRouteToRoad){
                    foreach(var point in examOptions.Route){var node=World.GetNextPositionOnStreet(Pos(point));if(node==Vector3.Zero || node.DistanceTo2D(Pos(point))>80 || Math.Abs(node.Z-point.Z)>40)throw new InvalidOperationException("Truck route node unavailable.");point.X=node.X;point.Y=node.Y;point.Z=node.Z;}
                    examOptions.SnapRouteToRoad=false;
                }
                var slot=RigSpawn.FindForModels(examOptions.TrailerSpawn,carModel,trailerModel,"DMV A");
                if(slot==null){EndRuntime();Notify.Show("Patio de exame ocupado/sem piso seguro. Nenhuma taxa nova.");return;}
                bay=slot.Trailer;examOptions.Spawn=slot.Truck;
                trailer=World.CreateVehicle(trailerModel,Pos(bay),bay.Heading);if(trailer==null || !trailer.Exists())throw new InvalidOperationException("Exam trailer failed.");trailer.IsPersistent=true;JobVehicles.Reserve(trailer);
                if(!trailer.PlaceOnGround())throw new InvalidOperationException("Trailer ground unavailable.");bay.Z=trailer.Position.Z;
            }
            Vector3 spawn=Pos(examOptions.Spawn);
            if(!JobActors.Free(spawn,5)){EndRuntime();Notify.Show("Vaga do exame ocupada. Libere a area; nenhuma taxa nova.");return;}
            car=World.CreateVehicle(carModel,spawn,examOptions.Spawn.Heading);if(car==null || !car.Exists())throw new InvalidOperationException("Exam car failed.");car.IsPersistent=true;JobVehicles.Reserve(car);
            Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT,car,"RPDM"+Guid.NewGuid().ToString("N").Substring(0,4).ToUpperInvariant());
            if(!car.PlaceOnGround())throw new InvalidOperationException("Exam parking ground not ready.");Function.Call(Hash.SET_VEHICLE_ENGINE_ON,car,false,true,false);car.FuelLevel=Math.Max(10,Math.Min(10000,car.PetrolTankVolume));
            examiner=World.CreatePed(examinerModel,spawn+new Vector3(1,0,1));if(examiner==null || !examiner.Exists())throw new InvalidOperationException("Examiner failed.");examiner.IsPersistent=true;examiner.BlockPermanentEvents=true;examiner.RelationshipGroup=Game.Player.Character.RelationshipGroup;examiner.SetIntoVehicle(car,VehicleSeat.Passenger);if(!ExaminerSeated)throw new InvalidOperationException("Examiner not seated.");
            if(Moto)Function.Call(Hash.GIVE_PED_HELMET,examiner,false,4096,0);
            if(Book.Pending==null && !Wallet.DmvBegin(examOptions,true,attempt,out _,category)){EndRuntime();Notify.Show("Saldo/dados mudaram; exame nao cobrado.");return;}
            if(!Book.Pending.Started){if(!Wallet.DmvStart(attempt,out _,category))throw new InvalidOperationException("Practical start not saved.");}
            else if(!Wallet.DmvRestart(attempt,out _,category))throw new InvalidOperationException("Practical resume not saved.");
            drive=CharacterRules.Copy(Book.Pending.Drive);preparing=false;last=Game.GameTime;saveAt=last+1000;previous=car.Position;damage=car.BodyHealth+car.EngineHealth+(Truck?trailer.BodyHealth:0);if(Truck)previousTrailer=trailer.Position;routedPoint=-1;
            Route();Notify.Show("Entre como condutor, "+(Moto?"capacete (aguarde no banco ou use F10)":"cinto "+Cfg.SeatbeltKey)+", seta direita "+Cfg.RightSignalKey+", depois esquerda "+Cfg.LeftSignalKey+". Motor: "+Cfg.EngineKey+". END suspende.");
        }
        Vector3 Target=>drive==null?Pos(options.Entry):drive.Stage==DmvStage.Report?Pos(options.Entry):Truck && drive.Stage==DmvStage.Route && !drive.Coupled?trailer.Position:Truck && drive.Stage==DmvStage.Parking?Pos(bay):drive.Stage==DmvStage.Route && drive.Point<examOptions.Route.Count?Pos(examOptions.Route[drive.Point]):Pos(examOptions.Spawn);
        void Route(){if(route!=null && route.Exists())route.Delete();route=World.CreateBlip(Target);route.Name="RPCore DMV: "+(drive.Stage==DmvStage.Route?"etapa "+(drive.Point+1):(drive.Stage==DmvStage.AwaitDriver?"entrar no carro":drive.Stage==DmvStage.RightSignal?"seta direita":drive.Stage==DmvStage.LeftSignal?"seta esquerda":drive.Stage==DmvStage.Parking?"estacionar":drive.Stage==DmvStage.Exit?"desembarcar":"retornar ao DMV"));route.Color=BlipColor.Yellow;route.ShowRoute=true;routedStage=drive.Stage;routedPoint=drive.Point;}
        void Uncouple(){if(!Truck || drive==null || !drive.Reversed || !Driver || !ExaminerSeated || !Protection || !InBay || !Towing){Notify.Show("Conclua a re e pare com a carreta alinhada na vaga.");return;}Function.Call(Hash.DETACH_VEHICLE_FROM_TRAILER,car);menu.Visible=false;}
        void Signal(bool left){if(!Driver || car.Speed>.5f)return;Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS,car,0,!left);Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS,car,1,left);menu.Visible=false;}
        Ped helmetOwner;int helmetModel,originalHat,originalTexture,loanHat,loanTexture;
        void EquipHelmet()
        {
            if(!Moto || !Driver || car.Speed>.5f){Notify.Show("Pare como condutor da moto de exame.");return;}
            if(Protection){Notify.Show("Capacete ja equipado.");return;}
            RestoreHelmet();var p=Game.Player.Character;
            int before=Function.Call<int>(Hash.GET_PED_PROP_INDEX,p,0),texture=before<0?0:Function.Call<int>(Hash.GET_PED_PROP_TEXTURE_INDEX,p,0);
            Function.Call(Hash.GIVE_PED_HELMET,p,false,4096,0);
            int given=Function.Call<int>(Hash.GET_PED_PROP_INDEX,p,0),givenTexture=given<0?0:Function.Call<int>(Hash.GET_PED_PROP_TEXTURE_INDEX,p,0);
            if(!Protection || given<0){RestoreHead(p,before,texture);Notify.Show("Capacete nao confirmado neste modelo. Selecao anterior restaurada.");return;}
            helmetOwner=p;helmetModel=p.Model.Hash;originalHat=before;originalTexture=texture;loanHat=given;loanTexture=givenTexture;menu.Visible=false;
        }
        static void RestoreHead(Ped p,int prop,int texture)
        {Function.Call(Hash.REMOVE_PED_HELMET,p,true);if(prop<0)Function.Call(Hash.CLEAR_PED_PROP,p,0);else Function.Call(Hash.SET_PED_PROP_INDEX,p,0,prop,texture,true);}
        void RestoreHelmet()
        {
            var p=helmetOwner;helmetOwner=null;
            if(p==null || !p.Exists() || p!=Game.Player.Character || p.Model.Hash!=helmetModel)return;
            if(Function.Call<int>(Hash.GET_PED_PROP_INDEX,p,0)==loanHat && Function.Call<int>(Hash.GET_PED_PROP_TEXTURE_INDEX,p,0)==loanTexture)RestoreHead(p,originalHat,originalTexture);
        }
        public CharacterBody FilterCharacterSnapshot(CharacterBody body)
        {
            var p=helmetOwner;
            return p!=null && p.Exists() && p==Game.Player.Character && p.Model.Hash==helmetModel?DmvHelmetSnapshot.Filter(body,helmetModel,loanHat,loanTexture,originalHat,originalTexture):body;
        }
        void ConfirmAbandon(string id){menu.Clear();menu.Name="Abandonar sem estorno?";Add("Sim, abandonar tentativa","Conserva certificado/licencas ja emitidos, sem devolver taxa desta tentativa.",()=>{if(!AtDesk)return;if(!Wallet.DmvAbandon(id,out _,category))throw new InvalidOperationException("Abandon not saved.");EndRuntime();OpenMenu();});Add("Voltar","Manter pendencia.",OpenMenu);menu.Visible=true;}
        void Issue(string id){if(!AtDesk)return;if(!Wallet.DmvFinish(id,true,out _,category) || !Book.Licensed)throw new InvalidOperationException("License not saved.");EndRuntime();OpenMenu();Notify.Show("Habilitacao classe "+ClassName+" emitida neste perfil. Sem nova cobranca.");}
        void Fail(string reason){if(!Wallet.DmvFinish(attempt,false,out _,category))throw new InvalidOperationException("Failed result not saved.");EndRuntime();Notify.Show("Exame reprovado: "+reason+". Certificado teorico mantido; nova tentativa tem taxa.");}
        void DriveTick()
        {
            if(owner!=Profile.Id){EndRuntime();return;}
            if(Game.IsPaused){last=Game.GameTime;return;}
            if(Game.IsMissionActive || Game.Player.Wanted.WantedLevel>0 || Game.Player.Character.IsDead || car==null || !car.Exists() || car.IsDead || examiner==null || !examiner.Exists() || examiner.IsDead){Fail("exame interrompido/veiculo ou examinadora indisponivel");return;}
            if(Truck && (trailer==null || !trailer.Exists() || trailer.IsDead || trailer.BodyHealth<500 || JobVehicles.TrailerHasOtherTractor(trailer,car) || Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER,car) && !Towing)){Fail("carreta indisponivel ou engate incorreto");return;}
            float dt=Math.Min(1,Math.Max(0,unchecked(Game.GameTime-last)/1000f));last=Game.GameTime;drive.Seconds+=dt;
            if(car.Position.DistanceTo(previous)>Math.Max(80,car.Speed*dt+30)){Fail("deslocamento discontinuo do veiculo");return;}float moved=car.Position.DistanceTo(previous);previous=car.Position;
            if(Truck){
                if(trailer.Position.DistanceTo(previousTrailer)>Math.Max(80,trailer.Speed*dt+30)){Fail("deslocamento discontinuo da carreta");return;}previousTrailer=trailer.Position;
                if(drive.Coupled && !drive.Reversed && !Towing)disconnected+=dt;else disconnected=0;
                if(disconnected>=5){Fail("carreta desacoplada durante o percurso");return;}
                if(drive.Stage==DmvStage.Parking)reverseDistance=DmvRigRules.ReverseDistance(reverseDistance,moved,car.GetSpeedVector(true).Y,Towing,Driver && ExaminerSeated && trailer.Position.DistanceTo2D(Pos(bay))<=40,Protection);
                if(drive.Couple(Driver,ExaminerSeated,Protection,Towing,examOptions)){Save();Route();}
            }
            if(drive.Stage!=DmvStage.Report && Game.Player.Character.Position.DistanceTo(car.Position)>100){Fail("abandono do veiculo de exame");return;}
            if((int)drive.Stage>=(int)DmvStage.RightSignal && (int)drive.Stage<=(int)DmvStage.Parking && (!ExaminerSeated || Game.Player.Character.IsInVehicle() && !Driver)){Fail("use o carro correto com a examinadora no banco passageiro");return;}
            bool fault=false;float currentDamage=car.BodyHealth+car.EngineHealth+(Truck?trailer.BodyHealth:0);if(damage-currentDamage>=25){drive.Faults++;fault=true;damage=currentDamage;}if(currentDamage>damage)damage=currentDamage;
            if(Driver && drive.Stage!=DmvStage.Report){
                if(car.Speed*3.6f>SpeedLimit+5)speedTime+=dt;else speedTime=0;
                if((examOptions.RequireSeatbelt || examOptions.RequireHelmet) && !Protection)beltTime+=dt;else beltTime=0;
                if(speedTime>=3){drive.Faults++;speedTime=0;fault=true;}
                if(beltTime>=5){drive.Faults++;beltTime=0;fault=true;}
            }
            if(car.EngineHealth<400 || car.BodyHealth<500){Fail("danos graves");return;}
            if(drive.Failed(examOptions)){Save();Fail("limite de faltas ou tempo");return;}
            var stage=drive.Stage;int point=drive.Point;
            bool inTarget=car.Position.DistanceTo2D(Target)<=7 && Math.Abs(car.Position.Z-Target.Z)<=4;
            if(!Truck || drive.Stage!=DmvStage.Parking)stopTime=Driver && inTarget && car.Speed<=.5f?stopTime+dt:0;
            switch(drive.Stage){
                case DmvStage.AwaitDriver:drive.Enter(Driver,ExaminerSeated);break;
                case DmvStage.RightSignal:case DmvStage.LeftSignal:drive.Signal(car.IsRightIndicatorLightOn,car.IsLeftIndicatorLightOn,Protection,Driver,ExaminerSeated,examOptions);break;
                case DmvStage.Route:if(drive.Point==examOptions.Route.Count && (!Truck || drive.Coupled))drive.Stage=DmvStage.Parking;else drive.Checkpoint(Driver,ExaminerSeated,inTarget && (!(examOptions.RequireSeatbelt || examOptions.RequireHelmet) || Protection),car.Speed*3.6f<=SpeedLimit && (!Truck || Towing),stopTime>=1,examOptions);break;
                case DmvStage.Parking:
                    if(!Truck)drive.Park(stopTime>=2);
                    else{
                        stopTime=InBay && Driver && ExaminerSeated && Protection?stopTime+dt:0;
                        if(drive.Reverse(Towing && reverseDistance>=examOptions.ReverseMeters && stopTime>=2,examOptions))Save();
                        if(drive.Detach(!Towing && InBay && Driver && ExaminerSeated && Protection,examOptions))Save();
                        if(drive.Detached && !Towing && InBay)drive.Park(true);
                    }break;
                case DmvStage.Exit:drive.Leave(!Game.Player.Character.IsInVehicle() && car.Speed<.5f && Game.Player.Character.Position.DistanceTo(car.Position)<12);break;
                case DmvStage.Report:if(AtDesk && !pool.AreAnyVisible){GTA.UI.Screen.ShowHelpTextThisFrame("F10 > DMV > Receber habilitacao aprovada");}break;
            }
            if(stage!=drive.Stage || point!=drive.Point){stopTime=0;Save();Route();}
            else if(fault || unchecked(Game.GameTime-saveAt)>=0)Save();
            if(routedStage!=drive.Stage || routedPoint!=drive.Point)Route();
            string task=drive.Stage==DmvStage.AwaitDriver?"entre como motorista":drive.Stage==DmvStage.RightSignal?(Moto?"capacete e seta DIREITA":"cinto e seta DIREITA"):drive.Stage==DmvStage.LeftSignal?"seta ESQUERDA":drive.Stage==DmvStage.Parking?(Truck?(drive.Reversed?"carreta alinhada; desengate pelo menu F10":"re "+reverseDistance.ToString("0.0")+"/"+examOptions.ReverseMeters+"m; alinhe a carreta na vaga por 2s"):"retorne e estacione por 2s"):drive.Stage==DmvStage.Exit?(Moto?"pare e desembarque da moto":"retire cinto e saia do carro"):drive.Stage==DmvStage.Report?"volte a pe ao DMV para emitir":Truck && !drive.Coupled?"de re, engate a carreta marcada":examOptions.Stops.Contains(drive.Point)?"pare por 1s no ponto marcado":"siga o ponto marcado";
            GTA.UI.Screen.ShowSubtitle("DMV "+ClassName+" / "+task+" / "+drive.Point+"/"+examOptions.Route.Count+" / faltas "+drive.Faults+"/"+examOptions.MaxFaults+" / limite "+SpeedLimit+" km/h",100);
            if(drive.Stage==DmvStage.Route || drive.Stage==DmvStage.Parking)World.DrawMarker(MarkerType.Cylinder,Target-new Vector3(0,0,.8f),Vector3.Zero,Vector3.Zero,new Vector3(5,5,1),Color.FromArgb(100,255,210,0));
        }
        public void Tick()
        {
            try{
                if(preparing){if(owner!=Profile.Id || !AtDesk || unchecked(Game.GameTime-prepareAt)>10000){EndRuntime();Notify.Show("Preparacao interrompida; nenhuma taxa nova.");return;}if(carModel.IsLoaded && examinerModel.IsLoaded && (!Truck || trailerModel.IsLoaded))Spawn();return;}
                if(drive!=null){DriveTick();return;}
                if(lease && Book.Pending?.Practical==false && (!menu.Visible || !AtDesk)){EndRuntime();menu.Visible=false;}
                if(Calm && !pool.AreAnyVisible && Game.Player.Character.Position.DistanceTo(Pos(options.Entry))<3)GTA.UI.Screen.ShowHelpTextThisFrame("F10 > DMV / habilitacoes C, M1 e A");
            }catch(Exception ex){EndRuntime();menu.Visible=false;Log.Error("dmv tick",ex);Notify.Show("Exame suspenso por falha. Pedido/progresso gravado conservado; retome no DMV.");}
        }
        public void Save(){if(drive==null || owner!=Profile.Id)return;if(!Wallet.DmvProgress(attempt,drive,out _,category))throw new InvalidOperationException("Practical progress not saved.");saveAt=Game.GameTime+1000;}
        void EndRuntime(){RestoreHelmet();if(route!=null && route.Exists())route.Delete();route=null;if(examiner!=null && examiner.Exists())JobActors.Retire(examiner);examiner=null;if(car!=null && car.Exists())JobVehicles.RetireRig(car,trailer);else if(trailer!=null && trailer.Exists())JobVehicles.RetireRig(null,trailer);car=null;trailer=null;bay=null;if(trailerModel.Hash!=0)trailerModel.MarkAsNoLongerNeeded();trailerModel=default(Model);reverseDistance=disconnected=0;if(carModel.Hash!=0)carModel.MarkAsNoLongerNeeded();if(examinerModel.Hash!=0)examinerModel.MarkAsNoLongerNeeded();carModel=examinerModel=default(Model);drive=null;preparing=false;attempt=null;owner=null;stopTime=speedTime=beltTime=0;if(lease)gate.Release(Name);lease=false;}
        public void OnKey(KeyEventArgs e){if(e.KeyCode==Keys.End && Running){try{Save();}finally{EndRuntime();menu.Visible=false;}Notify.Show("Exame suspenso; retome no DMV sem nova taxa.");}}
        public void Reload(){EndRuntime();menu.Visible=false;}
        public void Dispose()
        {
            // The global job cleanup may already have run during script abort. Release only our actors here.
            var ownCar=car;var ownTrailer=trailer;
            if(examiner!=null && examiner.Exists())examiner.Delete();examiner=null;
            EndRuntime();menu.Visible=false;
            if(ownCar!=null && ownCar.Exists()){
                if(ownCar.Occupants.Length==0 && ownCar.Speed<.5f && !Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,ownCar) && !Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER,ownCar))ownCar.Delete();
                if(ownCar.Exists())ownCar.MarkAsNoLongerNeeded();
            }
            if(ownTrailer!=null && ownTrailer.Exists()){
                if(ownTrailer.Occupants.Length==0 && ownTrailer.Speed<.5f && !Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,ownTrailer) && !JobVehicles.TrailerHasOtherTractor(ownTrailer,null) && (ownCar==null || !JobVehicles.Towing(ownCar,ownTrailer)))ownTrailer.Delete();
                if(ownTrailer.Exists())ownTrailer.MarkAsNoLongerNeeded();
            }
            if(truckOffice!=null && truckOffice.Exists())truckOffice.Delete();
            if(office!=null && office.Exists())office.Delete();
        }
    }
}
