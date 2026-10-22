using System;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;

namespace RPCore
{
    public sealed class ValetJob : DrivingJobSystem
    {
        public override string Name => "valet";
        public override string Label => "Valet";
        readonly ValetOptions options;
        readonly Blip standBlip;
        Vehicle car;
        Ped client;
        ValetRun run;
        DrivingQuality quality;
        int deadline, nextClient, spawnUntil;
        bool driven;
        public Vector3 StartPosition => Position(options.Stand);
        public ValetJob(ObjectPool pool, AchievementSystem achievements, ShiftGate gate) : base(pool,achievements,gate)
        {
            var defaults=DrivingDefaults.Valet();options=Store.LoadDef("Jobs/Valet.json",()=>defaults);
            if(options==null || !options.Valid){Log.Write("Invalid Jobs/Valet.json: embedded defaults used, original retained.");options=defaults;}
            standBlip=MapBlips.Create(StartPosition,"RPCore Job: Valet",BlipSprite.SteeringWheel,BlipColor.Yellow,.75f);
        }
        protected override void BuildMenu()
        {
            Add("GPS: hotel / valet","Va ao ponto a pe para contratar.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,StartPosition.X,StartPosition.Y);Menu.Visible=false;});
            if(!Active)Add("Iniciar turno","Receba, estacione e devolva "+options.CustomersPerShift+" carros de clientes. Pagamento no banco por devolucao.",Start);
            else Add("Atendimento atual",run==null?"Aguardando cliente.":"Siga o GPS: receber, estacionar e devolver o carro.",()=>Notify.Show("Siga o GPS e as instrucoes. End encerra o turno."));
        }
        void Start()
        {
            var p=Game.Player.Character;
            if(p.IsInVehicle() || p.Position.DistanceTo(StartPosition)>3){Notify.Show("Va ao ponto de valet a pe para contratar.");return;}
            if(!BeginShift(options.CustomersPerShift))return;
            nextClient=Game.GameTime;spawnUntil=Game.GameTime+60000;run=null;
            Notify.Show("Valet: aguarde o cliente chegar de carro. Somente os carros criados para o trabalho sao usados.");
        }
        protected override void IdleTick()
        {
            if(!InputAllowed || Game.IsMissionActive || Game.Player.Character.IsDead)return;
            Marker(StartPosition);
            var p=Game.Player.Character;
            if(!p.IsInVehicle() && p.Position.DistanceTo(StartPosition)<2)
            {Prompt.Help("~INPUT_CONTEXT~: contrato de valet");if(Game.IsControlJustPressed(GTA.Control.Context))OpenMenu();}
        }
        bool SpawnClient()
        {
            var spawn=Position(options.Spawn);
            if(!JobActors.Free(spawn,5))return false;
            var model=new Model(options.Cars[Random.Next(options.Cars.Count)]);
            if(!model.IsValid || !model.IsInCdImage)model=new Model(VehicleHash.Sultan);
            try
            {
                if(!model.Request(2500))return false;
                car=World.CreateVehicle(model,spawn,options.Spawn.Heading);
            }
            finally{model.MarkAsNoLongerNeeded();}
            if(car==null || !car.Exists())return false;
            car.IsPersistent=true;car.FuelLevel=60;car.LockStatus=VehicleLockStatus.Unlocked;
            // The job explicitly permits driving this generated customer car; no hotwire/theft flag.
            car.IsStolen=false;car.NeedsToBeHotwired=false;
            Function.Call(Hash.SET_VEHICLE_HAS_BEEN_OWNED_BY_PLAYER,car,true);
            Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT,car,"VT"+Random.Next(100000,999999));
            client=JobActors.Create(spawn+new Vector3(2,2,0),Random);
            if(client==null || !client.Exists())return false;
            // Place only our newly created customer into our newly created car.
            client.SetIntoVehicle(car,VehicleSeat.Driver);JobVehicles.Reserve(car);
            JobActors.Drive(client,car,Position(options.Arrival));
            run=new ValetRun();driven=false;quality=null;deadline=Game.GameTime+90000;
            Route(Position(options.Arrival),"aguarde o cliente chegar");return true;
        }
        bool AtParking => car.Position.DistanceTo(Position(options.Parking))<=options.ParkingRadius;
        bool Empty => car.Occupants.Length==0;
        protected override void WorkTick()
        {
            var p=Game.Player.Character;
            if(run==null)
            {
                Status("aguarde o cliente");
                if(p.Position.DistanceTo(StartPosition)>150){End("Voce se afastou do hotel. Turno encerrado.");return;}
                if(unchecked(Game.GameTime-nextClient)>=0)
                {
                    if(!SpawnClient())
                    {
                        CleanupClient();nextClient=Game.GameTime+5000;
                        if(unchecked(Game.GameTime-spawnUntil)>=0)End("Entrada ocupada ou modelo indisponivel. Libere o acesso e contrate novamente.");
                    }
                }
                return;
            }
            if(car==null || !car.Exists() || car.IsDead || !car.IsDriveable || client==null || !client.Exists() || client.IsDead)
            {End("Carro ou cliente indisponivel. Atendimento cancelado, sem pagamento.");return;}
            quality?.Sample(car.BodyHealth);
            switch(run.Stage)
            {
                case ValetStage.Arriving:
                    Status("aguarde a chegada");
                    if(run.Arrived(car.Driver==client && car.Position.DistanceTo(Position(options.Arrival))<9 && car.Speed<1))
                    {JobActors.Leave(client,car);deadline=Game.GameTime+35000;}
                    else if(unchecked(Game.GameTime-deadline)>=0){End("Cliente nao conseguiu chegar. Entrada deve estar livre.");return;}
                    break;
                case ValetStage.ClientLeaving:
                    Status("aguarde o cliente sair");
                    if(run.ClientLeft(Empty && !client.IsInVehicle()))
                    {JobActors.Walk(client,StartPosition);quality=new DrivingQuality(car.BodyHealth);Route(Position(options.Parking),"area de estacionamento");Notify.Show("Leve o carro a area marcada. Pare, saia e pressione E junto ao carro para trancar.");}
                    else if(InputAllowed && unchecked(Game.GameTime-deadline)>=0)
                    {Prompt.Help("Libere a porta e pressione ~INPUT_CONTEXT~ para o cliente sair.");if(Game.IsControlJustPressed(GTA.Control.Context) && car.Speed<1){JobActors.Leave(client,car);deadline=Game.GameTime+35000;}}
                    break;
                case ValetStage.Parking:
                    Status("estacione o carro");
                    if(p.IsInVehicle(car) && car.Driver==p)driven=true;
                    // Reference uses a 50m 3D parking area around this garage, not a single stall.
                    if(p.Position.DistanceTo(Position(options.Parking))<80)Marker(new Vector3(options.Parking.X,options.Parking.Y,car.Position.Z),3);
                    if(InputAllowed && driven && AtParking && !p.IsInVehicle() && p.Position.DistanceTo(car.Position)<3 && car.Speed<.5f && Empty)
                    {
                        Prompt.Help("~INPUT_CONTEXT~: trancar carro do cliente");
                        if(Game.IsControlJustPressed(GTA.Control.Context) && run.Park(true))
                        {car.IsEngineRunning=false;car.LockStatus=VehicleLockStatus.CannotEnter;Route(StartPosition,"retorne ao cliente no hotel");}
                    }
                    break;
                case ValetStage.WalkToStand:
                    Status("retorne ao hotel a pe");Marker(StartPosition);
                    if(InputAllowed && !p.IsInVehicle() && p.Position.DistanceTo(StartPosition)<3)
                    {
                        Prompt.Help("~INPUT_CONTEXT~: cliente solicita devolucao do carro");
                        if(Game.IsControlJustPressed(GTA.Control.Context) && run.RequestReturn(true))
                        {car.LockStatus=VehicleLockStatus.Unlocked;Route(car.Position,"busque o mesmo carro / "+car.Mods.LicensePlate);Notify.Show("Busque o carro estacionado e devolva na entrada do hotel.");}
                    }
                    break;
                case ValetStage.Retrieve:
                    Status("busque o carro do cliente");
                    if(run.Collect(p.IsInVehicle(car) && car.Driver==p))Route(Position(options.Arrival),"devolva na entrada do hotel");
                    break;
                case ValetStage.ReturnCar:
                    Status("devolva na entrada");Marker(Position(options.Arrival),3);
                    if(car.Position.DistanceTo(Position(options.Arrival))<10 && car.Speed<.5f)
                    {
                        if(p.IsInVehicle(car)){if(InputAllowed)Prompt.Help("Saia do carro para devolver a chave ao cliente.");}
                        else if(InputAllowed && !p.IsInVehicle() && p.Position.DistanceTo(car.Position)<6 && Empty)
                        {
                            Prompt.Help("~INPUT_CONTEXT~: devolver chave ao cliente");
                            if(Game.IsControlJustPressed(GTA.Control.Context) && run.Return(true))
                            {car.LockStatus=VehicleLockStatus.Unlocked;JobActors.Enter(client,car,VehicleSeat.Driver);deadline=Game.GameTime+35000;}
                        }
                    }
                    break;
                case ValetStage.ClientBoarding:
                    Status("cliente recuperando carro");
                    if(!run.ClientBoarded(client.IsInVehicle(car) && car.Driver==client) && InputAllowed && unchecked(Game.GameTime-deadline)>=0)
                    {Prompt.Help("Libere o acesso e pressione ~INPUT_CONTEXT~ para o cliente entrar.");if(Game.IsControlJustPressed(GTA.Control.Context) && Empty && car.Speed<.5f){JobActors.Enter(client,car,VehicleSeat.Driver);deadline=Game.GameTime+35000;}}
                    break;
                case ValetStage.Payment:
                    Status("confirmando devolucao");
                    if(InputAllowed && client.IsInVehicle(car) && car.Driver==client && run.Pay(()=>Shift.Confirm((step,last)=>Award(step,quality.Reward(options.BasePay,options.CleanTip,last?options.ShiftBonus:0),last,quality.Stars))))
                    {
                        car.IsEngineRunning=true;JobActors.Drive(client,car,Position(options.Exit));JobActors.Retire(client,car,true);JobVehicles.Retire(car);
                        client=null;car=null;run=null;quality=null;ClearRoute();
                        if(Shift.Finished){End("Turno de valet concluido! Bonus incluido no ultimo pagamento.");return;}
                        nextClient=Game.GameTime+7000;spawnUntil=Game.GameTime+60000;
                    }
                    break;
            }
        }
        void CleanupClient()
        {
            if(client!=null && client.Exists())JobActors.Retire(client,car);
            if(car!=null && car.Exists()){car.LockStatus=VehicleLockStatus.Unlocked;if(Game.Player.Character.IsInVehicle(car))Notify.Show("Desca do carro do cliente para a empresa recolher.");JobVehicles.Retire(car);}
            client=null;car=null;run=null;quality=null;
        }
        protected override void StopOwned(){run?.Cancel();CleanupClient();}
        public override void Dispose(){try{base.Dispose();}finally{if(standBlip!=null && standBlip.Exists())standBlip.Delete();}}
    }
}
