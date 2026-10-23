using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using LemonUI;
using LemonUI.Menus;

namespace RPCore
{
    public sealed class PilotJob : ISystem,IWorkContract
    {
        public string Name=>"pilot";
        public string Label=>"Piloto";
        public bool Active=>run!=null;
        public Vector3 StartPosition=>Pos(options.Start);
        readonly ObjectPool pool;
        readonly AchievementSystem achievements;
        readonly ShiftGate gate;
        readonly PilotOptions options;
        readonly NativeMenu menu;
        readonly Blip startBlip;
        readonly List<Ped> passengers=new List<Ped>();
        readonly Random random=new Random();
        Vehicle aircraft;
        Blip aircraftBlip,routeBlip;
        FlightRun run;
        JobPoint destination;
        Vector3 departure;
        int cancelSince=-1,awaySince=-1,nextPay,exitAt,exitUntil,flightBegan;
        bool timed;
        bool exitsOrdered;
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        bool Helicopter=>run!=null && run.Mode==FlightMode.Helicopter;
        bool Pilot=>aircraft!=null && aircraft.Exists() && Game.Player.Character.IsInVehicle(aircraft) && aircraft.Driver==Game.Player.Character;
        bool InputAllowed=>!Game.IsPaused && !InterfaceOptions.ModalOpen && !pool.AreAnyVisible;
        bool Qualified=>Wallet.PilotQualified;
        string Ledger=>run.Mode==FlightMode.Training?"pilot_training":Helicopter?"pilot_heli":"pilot_plane";
        int Seated=>passengers.Where((p,i)=>p.Exists() && !p.IsDead && p.IsInVehicle(aircraft) && aircraft.GetPedOnSeat((VehicleSeat)i)==p).Count();
        int Outside=>passengers.Count(p=>p.Exists() && !p.IsDead && !p.IsInVehicle() && p.Position.DistanceTo(Pos(destination))<40 && p.Position.DistanceTo(aircraft.Position)<30);
        Vector3 Target=>!Active?StartPosition:run.Stage==FlightStage.AwaitPilot || run.Stage==FlightStage.Returning?departure:Pos(destination);
        bool Landed(Vector3 point,float radius)=>FlightRules.Landed(Pilot,aircraft.IsInAir,aircraft.HeightAboveGround,aircraft.Speed,aircraft.Position.DistanceTo2D(point),Math.Abs(aircraft.Position.Z-point.Z),radius);
        float DestinationRadius=>Helicopter?12:30;
        public PilotJob(ObjectPool pool,AchievementSystem achievements,ShiftGate gate)
        {
            this.pool=pool;this.achievements=achievements;this.gate=gate;
            var defaults=PilotDefaults.Load();options=Store.LoadDef("Jobs/Pilot.json",()=>defaults);
            if(options==null || !options.Valid){Log.Write("Invalid Pilot.json; embedded defaults used, original retained.");options=defaults;}
            bool migrated;options=PilotAccess.Resolve(options,out migrated);
            if(migrated)Log.Write("Pilot access: legacy LSIA base moved to Grapeseed in memory; local Pilot.json and saves retained.");
            menu=new NativeMenu(Label,"AVIACAO");pool.Add(menu);Visuals.Register(menu,Name);
            startBlip=MapBlips.Create(StartPosition,"RPCore Job: piloto",BlipSprite.Plane,BlipColor.Yellow,.8f);
        }
        void Add(string title,string description,Action action)
        {
            var row=new NativeItem(title,description);row.Activated+=(s,e)=>{try{action();}catch(Exception ex){Log.Error("pilot action",ex);Notify.Show("Acao nao concluida. Veja RPCore.log.");}};menu.Add(row);
        }
        public void OpenMenu()
        {
            pool.HideAll();menu.Clear();menu.Name="Piloto / "+(Qualified?"qualificado":"qualificacao pendente");
            Add("GPS: base de aviacao","Contratacao a pe na base de aviacao.",()=>{Waypoint(StartPosition);menu.Visible=false;});
            if(!Active)
            {
                Add("Voo de qualificacao", "Velum: decole, voe ate Sandy Shores e pouse parado. Salva qualificacao por perfil.",()=>Begin(FlightMode.Training));
                Add("Transporte de aviao",Qualified?options.PlanePassengers+" passageiros ate "+options.PlaneDestination.Name+", desembarque e retorno a base.":"Conclua o voo de qualificacao primeiro.",()=>Begin(FlightMode.Plane));
                Add("Transporte de helicoptero",Qualified?"Passageiro ate um dos cinco helipontos e retorno a base.":"Conclua o voo de qualificacao primeiro.",()=>Begin(FlightMode.Helicopter));
                Add("Carreira","Transportes e turnos confirmados neste perfil.",()=>Notify.Show("Aviao: "+Wallet.JobProgress("pilot_plane").Deliveries+" / Helicoptero: "+Wallet.JobProgress("pilot_heli").Deliveries));
            }
            else
            {
                Add("GPS: objetivo atual","Aeronave, pista/heliponto ou retorno.",()=>{Waypoint(Target);menu.Visible=false;});
                Add("Motor da aeronave: ligar/desligar", "Alternativa a tecla "+Cfg.EngineKey+" quando parado em terra.",()=>{ToggleEngine();menu.Visible=false;});
                Add("Encerrar contrato","Mantem etapas ja pagas; nao paga bonus pendente.",()=>End("Voo cancelado. Etapas confirmadas mantidas."));
            }
            menu.Visible=true;
        }
        static void Waypoint(Vector3 p)=>Function.Call(Hash.SET_NEW_WAYPOINT,p.X,p.Y);
        void Begin(FlightMode mode)
        {
            var p=Game.Player.Character;
            if(Active)return;
            if(mode!=FlightMode.Training && !Qualified){Notify.Show("Conclua primeiro o voo de qualificacao.");return;}
            if(p.IsDead || !p.IsHuman || p.IsInVehicle() || p.IsRagdoll || p.IsInCombat || p.Position.DistanceTo(StartPosition)>6 || Game.IsMissionActive || Game.IsPaused || Game.Player.Wanted.WantedLevel>0)
            {Notify.Show("Contrate a pe na base de aviacao, sem procurado, combate ou missao.");return;}
            if(!gate.Acquire(Name)){Notify.Show("Encerre o trabalho de "+gate.Owner+" antes de iniciar outro.");return;}
            bool started=false,heli=mode==FlightMode.Helicopter;
            var model=new Model(heli?options.HeliModel:options.PlaneModel);
            try
            {
                if(!model.IsValid || !model.IsInCdImage || (heli?!model.IsHelicopter:!model.IsPlane)){Notify.Show("Modelo de aeronave invalido na configuracao.");return;}
                var spawn=RigSpawn.FindVehicle(heli?options.HeliSpawn:options.PlaneSpawn,model,"Pilot aircraft");
                if(spawn==null){Notify.Show("Sem vaga livre para a aeronave. Veja Pilot aircraft spawn no RPCore.log.");return;}
                aircraft=World.CreateVehicle(model,Pos(spawn),spawn.Heading);
                if(aircraft==null || !aircraft.Exists()){Notify.Show("Nao foi possivel criar a aeronave.");return;}
                aircraft.IsPersistent=true;aircraft.IsStolen=false;aircraft.NeedsToBeHotwired=false;aircraft.LockStatus=VehicleLockStatus.Unlocked;
                Function.Call(Hash.SET_VEHICLE_HAS_BEEN_OWNED_BY_PLAYER,aircraft,true);
                Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT,aircraft,"RP"+random.Next(100000,999999));
                float capacity=aircraft.PetrolTankVolume;aircraft.FuelLevel=capacity>0 && !float.IsNaN(capacity) && !float.IsInfinity(capacity)?Math.Min(capacity,10000):100;
                int count=mode==FlightMode.Training?0:heli?options.HeliPassengers:options.PlanePassengers;
                if(aircraft.PassengerCapacity<count){Notify.Show("Aeronave instalada sem assentos suficientes.");return;}
                if(p!=Game.Player.Character || p.IsDead || Game.IsMissionActive)return;
                destination=heli?options.Helipads[random.Next(options.Helipads.Count)]:options.PlaneDestination;departure=aircraft.Position;
                run=new FlightRun(mode,count);cancelSince=awaySince=-1;nextPay=0;exitsOrdered=false;
                JobVehicles.Reserve(aircraft);aircraftBlip=World.CreateBlip(aircraft.Position);aircraftBlip.Name="RPCore: aeronave de trabalho";aircraftBlip.Sprite=heli?BlipSprite.Helicopter:BlipSprite.Plane;aircraftBlip.Color=BlipColor.Blue;
                Route("retirar aeronave");menu.Visible=false;started=true;
                Notify.Show("Entre como piloto na aeronave azul, mantenha parada e pressione E para preparar o voo.");
            }
            finally{model.MarkAsNoLongerNeeded();if(!started){try{Cleanup();}finally{run=null;gate.Release(Name);}}}
        }
        bool FillPassengers()
        {
            var model=new Model(PedHash.Business01AMM);
            try
            {
                if(!model.Request(2500))return false;
                for(int i=0;i<run.Passengers;i++)
                {
                    if(!aircraft.IsSeatFree((VehicleSeat)i))return false;
                    var passenger=World.CreatePed(model,aircraft.GetOffsetPosition(new Vector3(5,0,1)));
                    if(passenger==null || !passenger.Exists())return false;
                    passengers.Add(passenger);passenger.IsPersistent=true;passenger.BlockPermanentEvents=true;passenger.Weapons.RemoveAll();passenger.CanBeDraggedOutOfVehicle=false;passenger.StaysInVehicleWhenJacked=true;
                    Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH,passenger,(int)RelationshipGroupHash.CivilianMale);
                    passenger.SetIntoVehicle(aircraft,(VehicleSeat)i);
                }
                return true;
            }
            finally{model.MarkAsNoLongerNeeded();}
        }
        public void Tick()
        {
            if(!Active)
            {
                if(Game.Player.Character.IsDead || Game.IsMissionActive || !InputAllowed)return;
                Marker(StartPosition,2);
                if(!Game.Player.Character.IsInVehicle() && Game.Player.Character.Position.DistanceTo(StartPosition)<3){Prompt.Help("~INPUT_CONTEXT~: trabalhos de piloto");if(Game.IsControlJustPressed(GTA.Control.Context))OpenMenu();}
                return;
            }
            if(Game.Player.Character.IsDead || Game.IsMissionActive || !aircraft.Exists() || aircraft.IsDead || !FlightRules.Airworthy(aircraft.EngineHealth,aircraft.BodyHealth,options.MinimumHealth))
            {Log.Write("Pilot failure: playerDead="+Game.Player.Character.IsDead+" mission="+Game.IsMissionActive+" aircraftExists="+aircraft.Exists()+" engine="+(aircraft.Exists()?aircraft.EngineHealth.ToString():"n/a")+" body="+(aircraft.Exists()?aircraft.BodyHealth.ToString():"n/a")+" wanted="+Game.Player.Wanted.WantedLevel);End("Voo encerrado: piloto ou aeronave indisponivel/danificada.");return;}
            if(passengers.Any(p=>!p.Exists() || p.IsDead)){End("Passageiro perdido. Voo encerrado sem novo pagamento.");return;}
            if(Game.IsPaused || InterfaceOptions.ModalOpen || pool.AreAnyVisible){run.ObserveFlight(Game.GameTime,false);return;}
            if(aircraftBlip!=null && aircraftBlip.Exists()){aircraftBlip.Position=aircraft.Position;aircraftBlip.Alpha=Pilot?0:255;}
            if(!Pilot)
            {
                run.ObserveFlight(Game.GameTime,false);if(awaySince<0)awaySince=Game.GameTime;
                if(unchecked(Game.GameTime-awaySince)>60000){End("Aeronave abandonada. Voo encerrado.");return;}
                Status("retorne ao assento do piloto");return;
            }
            awaySince=-1;
            if(run.Stage==FlightStage.AwaitTakeoff || run.Stage==FlightStage.EnRoute)
                if(Seated!=run.Passengers){End("Passageiro saiu antes do destino. Voo encerrado.");return;}
            run.ObserveFlight(Game.GameTime,FlightRules.InFlight(Pilot,aircraft.IsInAir,aircraft.HeightAboveGround,aircraft.Speed,Helicopter));
            if(timed && (run.Stage==FlightStage.AwaitTakeoff || run.Stage==FlightStage.EnRoute) && FlightRules.Expired(Game.GameTime,flightBegan,options.HeliTimeLimitSeconds)){End("Prazo do voo executivo esgotado. Sem pagamento.");return;}
            switch(run.Stage)
            {
                case FlightStage.AwaitPilot:
                    Status("prepare o voo na aeronave parada");
                    if(Landed(departure,35))
                    {Prompt.Help("~INPUT_CONTEXT~: preparar voo e conferir passageiros");if(Confirm())
                        {if(run.Passengers>0 && !FillPassengers()){End("Falha no manifesto de passageiros.");return;}if(!run.Prepare(Landed(departure,35),Seated)){End("Assentos nao confirmados. Voo encerrado.");return;}timed=Helicopter && options.HeliTimeLimitSeconds>0 && options.TimedHelipadIndex>=0 && ReferenceEquals(destination,options.Helipads[options.TimedHelipadIndex]);flightBegan=Game.GameTime;Route(destination.Name);Notify.Show("Voo pronto. Decole e siga o destino: "+destination.Name);}}
                    break;
                case FlightStage.AwaitTakeoff:
                    Status("decole e mantenha voo por "+(Helicopter?6:10)+" segundos");
                    break;
                case FlightStage.EnRoute:
                    Status("pouse e pare em "+destination.Name);Marker(Pos(destination),Helicopter?10:25);
                    if(Landed(Pos(destination),DestinationRadius))
                    {Prompt.Help("~INPUT_CONTEXT~: confirmar pouso"+(run.Passengers>0?" e desembarque":" da qualificacao"));if(Confirm() && run.Arrive(true)){exitsOrdered=false;exitAt=0;exitUntil=Game.GameTime+120000;}}
                    break;
                case FlightStage.Exiting:
                    Status("aguarde motor parar e passageiros sairem "+Outside+"/"+run.Passengers);
                    if(!Landed(Pos(destination),DestinationRadius)){exitAt=0;Prompt.Help("Pouse e pare no destino para concluir o desembarque.");return;}
                    if(aircraft.IsEngineRunning)
                    {Function.Call(Hash.SET_VEHICLE_ENGINE_ON,aircraft,false,true,true);exitAt=Game.GameTime+(Helicopter?6000:2000);}
                    if(exitAt==0)exitAt=Game.GameTime+(Helicopter?6000:2000);
                    if(unchecked(Game.GameTime-exitAt)<0)return;
                    if(!exitsOrdered){OrderExit();exitsOrdered=true;}
                    if(run.Disembarked(true,Outside))break;
                    if(unchecked(Game.GameTime-exitUntil)>=0){End("Desembarque nao concluido no prazo. Sem novo pagamento.");return;}
                    Prompt.Help("Aguarde a saida. ~INPUT_CONTEXT~: tentar desembarque novamente");if(Confirm())OrderExit();
                    break;
                case FlightStage.PaymentPending:
                    Status("confirmando qualificacao/pagamento");
                    bool training=run.Mode==FlightMode.Training;
                    int pay=training?0:DeliveryRoute.Pay(Helicopter?options.HeliBasePay:options.PlaneBasePay,Helicopter?options.HeliPerMeter:options.PlanePerMeter,DeliveryRoute.Distance(Helicopter?options.HeliSpawn:options.PlaneSpawn,destination));
                    if(run.PayArrival(Landed(Pos(destination),DestinationRadius) && (training || Outside==run.Passengers),step=>Pay(step,pay,!training,training)))
                    {
                        if(training){End("Qualificacao salva! Voos comerciais liberados. Retorne a base de aviacao; a aeronave ocupada continua disponivel para voltar.");Waypoint(StartPosition);return;}
                        ReleaseArrivals();Route("retornar aeronave a base");Notify.Show("Passageiros entregues. Religue o motor com "+Cfg.EngineKey+" ou pelo menu e retorne para o bonus.");
                    }
                    break;
                case FlightStage.Returning:
                    Status("retorne e pouse na base");Marker(departure,Helicopter?10:30);
                    if(Landed(departure,Helicopter?18:40) && aircraft.PassengerCount==0)
                    {Prompt.Help("~INPUT_CONTEXT~: devolver aeronave e receber bonus");if(Confirm() && run.Return(true,step=>Pay(step,Helicopter?options.HeliReturnBonus:options.PlaneReturnBonus,false,true)))End("Voo concluido. Saia da aeronave para recolha.");}
                    break;
            }
        }
        void OrderExit(){foreach(var p in passengers)if(p.Exists() && p.IsInVehicle(aircraft))JobActors.Leave(p,aircraft);}
        bool Confirm()=>InputAllowed && Game.IsControlJustPressed(GTA.Control.Context);
        bool Pay(int step,int amount,bool delivery,bool complete)
        {
            if(unchecked(Game.GameTime-nextPay)<0)return false;
            bool credited;
            try{if(!Wallet.PayDelivery(Ledger,run.Id,step,amount,true,delivery,complete,out credited)){nextPay=Game.GameTime+3000;Notify.Show("Limite de saldo/progresso. Confirmacao pendente.");return false;}}
            catch(Exception ex){nextPay=Game.GameTime+5000;Log.Error("pilot checkpoint",ex);Notify.Show("Falha ao salvar. Permaneça no ponto para tentar novamente.");return false;}
            if(credited)
            {
                if(amount>0)Notify.Show("~g~+$"+amount+" no banco~s~ / piloto");
                try{Sfx.Play("delivered");if(run.Mode==FlightMode.Training)achievements.Bump("pilot_qualified");if(delivery)achievements.Bump("pilot_deliveries");if(complete && run.Mode!=FlightMode.Training)achievements.Bump("pilot_returns");if(amount>0)achievements.Bump("cash_earned",amount);achievements.Save();}
                catch(Exception ex){Log.Error("pilot presentation after checkpoint",ex);}
            }
            return true;
        }
        void ReleaseArrivals()
        {
            foreach(var p in passengers)if(p.Exists()){p.CanBeDraggedOutOfVehicle=true;p.StaysInVehicleWhenJacked=false;Function.Call(Hash.TASK_STAND_STILL,p,-1);JobActors.Retire(p);}
            passengers.Clear();
        }
        void Route(string name){ClearRoute();routeBlip=World.CreateBlip(Target);routeBlip.Name="RPCore Piloto: "+name;routeBlip.Sprite=Helicopter?BlipSprite.Helicopter:BlipSprite.Plane;routeBlip.Color=BlipColor.Yellow;routeBlip.Scale=.8f;routeBlip.ShowRoute=true;}
        void ClearRoute(){if(routeBlip!=null && routeBlip.Exists())routeBlip.Delete();routeBlip=null;}
        void Cleanup()
        {
            // Cancellation must not order passengers to jump from an airborne aircraft.
            foreach(var p in passengers)if(p.Exists())p.Delete();passengers.Clear();ClearRoute();
            if(aircraftBlip!=null && aircraftBlip.Exists())aircraftBlip.Delete();aircraftBlip=null;
            JobVehicles.RetireAircraft(aircraft);aircraft=null;destination=null;
        }
        void End(string message){run?.Cancel();try{Cleanup();}finally{run=null;gate.Release(Name);menu.Visible=false;}Notify.Show(message);}
        void Status(string text){if(timed && run!=null && (run.Stage==FlightStage.AwaitTakeoff || run.Stage==FlightStage.EnRoute))text+=" / "+Math.Max(0,options.HeliTimeLimitSeconds-Math.Max(0,unchecked(Game.GameTime-flightBegan))/1000)+"s";if(!Visuals.TryJobHeader(Name,"Piloto / "+text,Color.White))new TextElement("Piloto / "+text,new PointF(GTA.UI.Screen.ScaledWidth/2,20),.36f,Color.White,GTA.UI.Font.ChaletLondon,Alignment.Center).Draw();}
        static void Marker(Vector3 p,float size){if(Game.Player.Character.Position.DistanceTo(p)<600)World.DrawMarker(MarkerType.Cylinder,p-new Vector3(0,0,1),Vector3.Zero,Vector3.Zero,new Vector3(size,size,2),Color.FromArgb(110,255,200,0));}
        public void PollCancel(){if(!Active || !InputAllowed || !Game.IsKeyPressed(Keys.End)){cancelSince=-1;return;}if(cancelSince<0)cancelSince=Game.GameTime;if(unchecked(Game.GameTime-cancelSince)>=1000)End("Voo cancelado. Etapas confirmadas mantidas.");}
        void ToggleEngine()
        {
            if(!Active || !Pilot || aircraft.IsInAir || aircraft.Speed>=.5f){Notify.Show("Pare como piloto em terra para alternar o motor.");return;}
            if(run.Stage==FlightStage.Exiting || run.Stage==FlightStage.PaymentPending){Notify.Show("Aguarde a confirmacao do desembarque.");return;}
            bool on=!aircraft.IsEngineRunning;Function.Call(Hash.SET_VEHICLE_ENGINE_ON,aircraft,on,true,!on);
            Notify.Show(on?"Motor da aeronave ligado.":"Motor da aeronave desligado.");
        }
        public void OnKey(KeyEventArgs e){if(Active && Pilot && InputAllowed && e.KeyCode==Cfg.EngineKey)ToggleEngine();}
        public void Reload(){if(Active)End("Voo encerrado na troca de perfil.");menu.Visible=false;}
        public void Save(){}
        public void Dispose(){try{Cleanup();}finally{run=null;gate.Release(Name);menu.Visible=false;if(startBlip!=null && startBlip.Exists())startBlip.Delete();}}
    }
}
