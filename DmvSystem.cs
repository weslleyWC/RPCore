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
    public sealed partial class DmvSystem : ISystem
    {
        public string Name=>"dmv";
        readonly ObjectPool pool;readonly ShiftGate gate;readonly Func<bool> belted;readonly DmvOptions carOptions,motorcycleOptions,truckOptions;DmvClass category;readonly NativeMenu menu;readonly PersonalServiceMenu ui;readonly DmvAssessmentOptions refinement;readonly Blip office,truckOffice;
        Vehicle car,trailer;JobPoint bay;Model trailerModel;float reverseDistance,disconnected;Vector3 previousTrailer;Ped examiner;Blip route;Model carModel,examinerModel;bool preparing,lease;string owner,attempt;
        DmvDrive drive;DmvOptions examOptions;int last,saveAt,prepareAt,answerAt;float stopTime,damage;Vector3 previous;DmvStage routedStage;int routedPoint=-1;
        DmvData Book=>category==DmvClass.C?Wallet.Dmv:category==DmvClass.M1?Wallet.DmvM1:Wallet.DmvA;
        DmvOptions options=>Book.Pending?.Options?.Valid==true && Book.Pending.Options.Category==category?Book.Pending.Options:Config(category);
        bool Truck=>category==DmvClass.A;
        bool Towing=>JobVehicles.Towing(car,trailer);
        bool InBay=>trailer!=null && PdmAssets.Valid(trailer) && bay!=null && DmvRigRules.InBay(trailer.Position.DistanceTo2D(Pos(bay)),trailer.Position.Z-bay.Z,trailer.Heading,bay.Heading,car.Speed,trailer.Speed);
        bool Moto=>category==DmvClass.M1;
        string ClassName=>Truck?"A":Moto?"M1":"C";
        bool Protection=>Moto?Function.Call<bool>(Hash.IS_PED_WEARING_HELMET,Game.Player.Character):belted();
        float SpeedLimit=>examOptions!=null && drive!=null && drive.Stage==DmvStage.Route?examOptions.SpeedAt(drive.Point):examOptions?.SpeedKmh??options.SpeedKmh;
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        bool Calm=>Game.Player.Character.IsHuman && !Game.Player.Character.IsRagdoll && !Game.Player.Character.IsFalling && !Game.Player.Character.IsSwimming && !Game.Player.Character.IsClimbing && GameplayContext.BackgroundReady && Game.Player.CanControlCharacter && Profile.Detect()==Profile.Id && !GameplayContext.SceneBlocking && !Game.IsPaused && !Game.IsMissionActive && !InterfaceOptions.ModalOpen && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && !Game.Player.Character.IsDead && !Game.Player.Character.IsInCombat && Game.Player.Wanted.WantedLevel==0;
        bool AtDesk=>Calm && !Game.Player.Character.IsInVehicle() && Game.Player.Character.Position.DistanceTo(Pos(options.Entry))<=3;
        bool Driver=>car!=null && PdmAssets.Valid(car) && car.Driver==Game.Player.Character && Game.Player.Character.IsInVehicle(car);
        bool ExaminerSeated=>examiner!=null && PdmAssets.Valid(examiner) && !examiner.IsDead && examiner.IsInVehicle(car) && car.GetPedOnSeat(VehicleSeat.Passenger)==examiner;
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
            menu=new NativeMenu("DMV / habilitacoes","TEORIA / EXAME PRATICO");ui=new PersonalServiceMenu(menu,MenuError);ui.CanNavigate=()=>Calm;pool.Add(menu);Visuals.Register(menu,"core");var policy=DmvAssessmentOptions.Load();refinement=Store.LoadDef("Jobs/DmvRefinement.json",()=>policy);if(refinement?.Valid!=true){Log.Write("Invalid DmvRefinement.json; embedded defaults used, original retained.");refinement=policy;}office=MapBlips.Create(Pos(options.Entry),"RPCore: DMV / habilitacoes C e M1",BlipSprite.PropertyBikers,BlipColor.White,.8f);
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
                trailer=PdmAssets.Track(World.CreateVehicle(trailerModel,Pos(bay),bay.Heading));if(trailer==null || !trailer.Exists())throw new InvalidOperationException("Exam trailer failed.");trailer.IsPersistent=true;JobVehicles.Reserve(trailer);
                if(!trailer.PlaceOnGround())throw new InvalidOperationException("Trailer ground unavailable.");bay.Z=trailer.Position.Z;
            }
            if(!Truck){var free=RigSpawn.FindVehicle(examOptions.Spawn,carModel,"DMV "+ClassName);if(free==null){EndRuntime();Notify.Show("Vaga ocupada ou sem piso seguro. Nenhuma taxa nova.");return;}examOptions.Spawn=free;}
            Vector3 spawn=Pos(examOptions.Spawn);
            if(!JobActors.Free(spawn,5)){EndRuntime();Notify.Show("Vaga do exame ocupada. Libere a area; nenhuma taxa nova.");return;}
            car=PdmAssets.Track(World.CreateVehicle(carModel,spawn,examOptions.Spawn.Heading));if(car==null || !car.Exists())throw new InvalidOperationException("Exam car failed.");car.IsPersistent=true;JobVehicles.Reserve(car);
            Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT,car,"RPDM"+Guid.NewGuid().ToString("N").Substring(0,4).ToUpperInvariant());
            if(!car.PlaceOnGround())throw new InvalidOperationException("Exam parking ground not ready.");Function.Call(Hash.SET_VEHICLE_ENGINE_ON,car,false,true,false);car.FuelLevel=Math.Max(10,Math.Min(10000,car.PetrolTankVolume));
            examiner=PdmAssets.Track(World.CreatePed(examinerModel,spawn+new Vector3(1,0,1)));if(examiner==null || !examiner.Exists())throw new InvalidOperationException("Examiner failed.");examiner.IsPersistent=true;examiner.BlockPermanentEvents=true;examiner.RelationshipGroup=Game.Player.Character.RelationshipGroup;examiner.SetIntoVehicle(car,VehicleSeat.Passenger);if(!ExaminerSeated)throw new InvalidOperationException("Examiner not seated.");
            if(Moto)Function.Call(Hash.GIVE_PED_HELMET,examiner,false,4096,0);
            if(Book.Pending==null && !Wallet.DmvBeginRefined(examOptions,refinement,true,attempt,out _,category)){EndRuntime();Notify.Show("Saldo/dados mudaram; exame nao cobrado.");return;}
            if(!Book.Pending.Started){if(!Wallet.DmvStart(attempt,out _,category))throw new InvalidOperationException("Practical start not saved.");}
            else if(!Wallet.DmvRestart(attempt,out _,category))throw new InvalidOperationException("Practical resume not saved.");
            drive=CharacterRules.Copy(Book.Pending.Drive);legacyAttempt=Book.Pending.Assessment==null;examPolicy=CharacterRules.Copy(Book.Pending.Assessment??refinement);if(legacyAttempt){examPolicy.RouteIndicators=false;examPolicy.StopSeconds=1;examPolicy.ParkingSeconds=2;}faultMonitor=new DmvFaultMonitor();preparing=false;last=Game.GameTime;saveAt=last+1000;previous=car.Position;damage=car.BodyHealth+car.EngineHealth+(Truck?trailer.BodyHealth:0);if(Truck)previousTrailer=trailer.Position;routedPoint=-1;
            Route();Notify.Show("Entre como condutor, "+(Moto?"capacete (aguarde no banco ou use F10)":"cinto "+Cfg.SeatbeltKey)+", seta direita "+Cfg.RightSignalKey+", depois esquerda "+Cfg.LeftSignalKey+". Motor: "+Cfg.EngineKey+". END suspende.");
        }
        Vector3 Target=>drive==null?Pos(options.Entry):drive.Stage==DmvStage.Report?Pos(options.Entry):Truck && drive.Stage==DmvStage.Route && !drive.Coupled?trailer.Position:Truck && drive.Stage==DmvStage.Parking?Pos(bay):drive.Stage==DmvStage.Route && drive.Point<examOptions.Route.Count?Pos(examOptions.Route[drive.Point]):Pos(examOptions.Spawn);
        void Route(){if(route!=null && route.Exists())route.Delete();route=World.CreateBlip(Target);route.Name="RPCore DMV: "+(drive.Stage==DmvStage.Route?"etapa "+(drive.Point+1):(drive.Stage==DmvStage.AwaitDriver?"entrar no carro":drive.Stage==DmvStage.RightSignal?"seta direita":drive.Stage==DmvStage.LeftSignal?"seta esquerda":drive.Stage==DmvStage.Parking?"estacionar":drive.Stage==DmvStage.Exit?"desembarcar":"retornar ao DMV"));route.Color=BlipColor.Yellow;route.ShowRoute=true;routedStage=drive.Stage;routedPoint=drive.Point;}
        void Uncouple(){if(!Truck || drive==null || !drive.Reversed || !Driver || !ExaminerSeated || !Protection || !InBay || !Towing){Notify.Show("Conclua a re e pare com a carreta alinhada na vaga.");return;}Function.Call(Hash.DETACH_VEHICLE_FROM_TRAILER,car);menu.Visible=false;}
        void Signal(bool left){if(!Driver || car.Speed>.5f)return;Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS,car,0,!left);Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS,car,1,left);menu.Visible=false;}
        Ped helmetOwner;long helmetAddress;int helmetModel,originalHat,originalTexture,loanHat,loanTexture;
        void EquipHelmet()
        {
            if(!Moto || !Driver || car.Speed>.5f){Notify.Show("Pare como condutor da moto de exame.");return;}
            if(Protection){Notify.Show("Capacete ja equipado.");return;}
            RestoreHelmet();var p=Game.Player.Character;
            int before=Function.Call<int>(Hash.GET_PED_PROP_INDEX,p,0),texture=before<0?0:Function.Call<int>(Hash.GET_PED_PROP_TEXTURE_INDEX,p,0);
            Function.Call(Hash.GIVE_PED_HELMET,p,false,4096,0);
            int given=Function.Call<int>(Hash.GET_PED_PROP_INDEX,p,0),givenTexture=given<0?0:Function.Call<int>(Hash.GET_PED_PROP_TEXTURE_INDEX,p,0);
            if(!Protection || given<0){RestoreHead(p,before,texture);Notify.Show("Capacete nao confirmado neste modelo. Selecao anterior restaurada.");return;}
            helmetOwner=p;helmetAddress=p.MemoryAddress.ToInt64();helmetModel=p.Model.Hash;originalHat=before;originalTexture=texture;loanHat=given;loanTexture=givenTexture;menu.Visible=false;
        }
        static void RestoreHead(Ped p,int prop,int texture)
        {Function.Call(Hash.REMOVE_PED_HELMET,p,true);if(prop<0)Function.Call(Hash.CLEAR_PED_PROP,p,0);else Function.Call(Hash.SET_PED_PROP_INDEX,p,0,prop,texture,true);}
        void RestoreHelmet()
        {
            var p=helmetOwner;helmetOwner=null;
            if(p==null || !p.Exists() || p!=Game.Player.Character || p.Model.Hash!=helmetModel || p.MemoryAddress.ToInt64()!=helmetAddress)return;
            if(Function.Call<int>(Hash.GET_PED_PROP_INDEX,p,0)==loanHat && Function.Call<int>(Hash.GET_PED_PROP_TEXTURE_INDEX,p,0)==loanTexture)RestoreHead(p,originalHat,originalTexture);
        }
        public CharacterBody FilterCharacterSnapshot(CharacterBody body)
        {
            var p=helmetOwner;
            return p!=null && p.Exists() && p==Game.Player.Character && p.Model.Hash==helmetModel && p.MemoryAddress.ToInt64()==helmetAddress?DmvHelmetSnapshot.Filter(body,helmetModel,loanHat,loanTexture,originalHat,originalTexture):body;
        }
        void Issue(string id){if(!AtDesk)return;if(!Wallet.DmvFinish(id,true,out _,category) || !Book.Licensed)throw new InvalidOperationException("License not saved.");EndRuntime();OpenMenu();Notify.Show("Habilitacao classe "+ClassName+" emitida neste perfil. Sem nova cobranca.");}
        void Fail(string reason){Save();if(!Wallet.DmvFinish(attempt,false,out _,category,reason))throw new InvalidOperationException("Failed result not saved.");EndRuntime();Notify.Show("Exame reprovado: "+reason+". Certificado teorico mantido; nova tentativa tem taxa.");}
        public void Tick()
        {
            try{
                ui.Tick(Calm);
                DrawLesson();
                if(stopTheory){stopTheory=false;SuspendTheory();return;}
                if(ConsumesKeyboard){var pending=Book.Pending;int answer=answers.Take(pending.Id,pending.Answers.Count,Game.GameTime);if(answer>=0)SubmitAnswer(pending.Id,pending.Answers.Count,answer);DrawTheoryCard();}
                if(preparing){if(!ActorBound || !AtDesk || unchecked(Game.GameTime-prepareAt)>15000){EndRuntime();Notify.Show("Preparacao interrompida; nenhuma taxa nova.");return;}if(carModel.IsLoaded && examinerModel.IsLoaded && (!Truck || trailerModel.IsLoaded))Spawn();return;}
                if(drive!=null){DriveTick();return;}
                if(lease && Book.Pending?.Practical==false && (!menu.Visible || !AtDesk || !ActorBound)){EndRuntime();ui.Close();}
                if(Calm && !pool.AreAnyVisible){
                    var nearest=new[]{DmvClass.C,DmvClass.M1,DmvClass.A}.OrderBy(c=>Game.Player.Character.Position.DistanceTo(Pos(Desk(c)))).First();var point=Desk(nearest);float distance=Game.Player.Character.Position.DistanceTo(Pos(point));
                    if(distance<25)World.DrawMarker(MarkerType.Cylinder,Pos(point)-new Vector3(0,0,.9f),Vector3.Zero,Vector3.Zero,new Vector3(1.4f,1.4f,.5f),Color.FromArgb(110,80,170,240));
                    if(distance<=3 && PersonalServiceMenu.SafeFoot){Prompt.Help("~INPUT_CONTEXT~: DMV / habilitacoes e provas");if(Game.IsControlJustPressed(GTA.Control.Context))OpenClass(nearest);}
                }
            }catch(Exception ex){EndRuntime();ui.Close();Log.Error("dmv tick",ex);Notify.Show("Exame suspenso por falha. Pedido/progresso gravado conservado; retome no DMV.");}
        }
        public void Save(){if(drive==null || owner!=Profile.Id)return;if(!Wallet.DmvProgress(attempt,drive,out _,category))throw new InvalidOperationException("Practical progress not saved.");saveAt=Game.GameTime+1000;}
        void EndRuntime(){answers.Clear();stopTheory=false;RestoreHelmet();if(route!=null && route.Exists())route.Delete();route=null;if(examiner!=null && PdmAssets.Valid(examiner))JobActors.Retire(examiner);examiner=null;if(car!=null && PdmAssets.Valid(car))JobVehicles.RetireRig(car,trailer);else if(trailer!=null && PdmAssets.Valid(trailer))JobVehicles.RetireRig(null,trailer);car=null;trailer=null;bay=null;if(trailerModel.Hash!=0)trailerModel.MarkAsNoLongerNeeded();trailerModel=default(Model);reverseDistance=disconnected=0;if(carModel.Hash!=0)carModel.MarkAsNoLongerNeeded();if(examinerModel.Hash!=0)examinerModel.MarkAsNoLongerNeeded();carModel=examinerModel=default(Model);drive=null;preparing=false;attempt=null;owner=null;stopTime=0;if(lease)gate.Release(Name);lease=false;}
        public void OnKey(KeyEventArgs e)
        {
            if(ConsumesKeyboard){if(e.KeyCode==Keys.End || e.KeyCode==Keys.Escape){stopTheory=true;return;}if(e.Modifiers==Keys.None && e.KeyCode>=Keys.A && e.KeyCode<=Keys.D){var pending=Book.Pending;answers.Queue(pending.Id,pending.Answers.Count,(int)e.KeyCode-(int)Keys.A,Game.GameTime);}return;}
            if(e.KeyCode==Keys.End && Running){try{Save();}finally{EndRuntime();ui.Close();}Notify.Show("Exame suspenso; retome no DMV sem nova taxa.");}
        }
        public void Reload(){EndRuntime();ui.Reset();}
        public void Dispose()
        {
            // The global job cleanup may already have run during script abort. Release only our actors here.
            ui.Reset();var ownCar=car;var ownTrailer=trailer;
            if(examiner!=null && PdmAssets.Valid(examiner))examiner.Delete();examiner=null;
            EndRuntime();menu.Visible=false;
            if(ownCar!=null && PdmAssets.Valid(ownCar)){
                if(ownCar.Occupants.Length==0 && ownCar.Speed<.5f && !Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,ownCar) && !Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER,ownCar))ownCar.Delete();
                if(PdmAssets.Valid(ownCar))ownCar.MarkAsNoLongerNeeded();
            }
            if(ownTrailer!=null && PdmAssets.Valid(ownTrailer)){
                if(ownTrailer.Occupants.Length==0 && ownTrailer.Speed<.5f && !Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,ownTrailer) && !JobVehicles.TrailerHasOtherTractor(ownTrailer,null) && (ownCar==null || !JobVehicles.Towing(ownCar,ownTrailer)))ownTrailer.Delete();
                if(PdmAssets.Valid(ownTrailer))ownTrailer.MarkAsNoLongerNeeded();
            }
            if(truckOffice!=null && truckOffice.Exists())truckOffice.Delete();
            if(office!=null && office.Exists())office.Delete();
        }
    }
}
