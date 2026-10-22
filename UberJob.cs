using System;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;

namespace RPCore
{
    public sealed class UberJob : DrivingJobSystem
    {
        public override string Name => "uber";
        public override string Label => "Uber";
        readonly UberOptions options;
        Vehicle vehicle;
        VehicleLockStatus originalLock;
        bool unlocked;
        Ped passenger;
        RideRun ride;
        DrivingQuality quality;
        JobPoint pickupPoint, dropPoint;
        Vector3 pickup, drop;
        int quote, nextOffer, offerUntil, tripUntil, taskUntil, driverAway = -1, spawnRetry, spawnFailures;
        bool offered;
        public event Action<string,string> Activity;
        public UberJob(ObjectPool pool, AchievementSystem achievements, ShiftGate gate) : base(pool,achievements,gate)
        {
            var defaults=DrivingDefaults.Uber();options=Store.LoadDef("Jobs/Uber.json",()=>defaults);
            if(options==null || !options.Valid){Log.Write("Invalid Jobs/Uber.json: embedded defaults used, original retained.");options=defaults;}
        }
        void Record(string body) { try { Activity?.Invoke("Uber",body); } catch(Exception ex){Log.Error("Uber local history",ex);} }
        string Summary => !Active ? "Fora de servico" : offered ? "Solicitacao disponivel" : ride == null ? "Aguardando solicitacao" :
            ride.Stage == RideStage.Pickup ? "Buscando passageiro" : ride.Stage == RideStage.Boarding ? "Embarcando" : ride.Stage == RideStage.Riding ? "Em viagem" : ride.Stage == RideStage.Exiting ? "Desembarcando" : "Confirmando pagamento";
        public string[] PhoneRows()
        {
            var s=Wallet.JobProgress(Name);string rating=s.RatedTrips>0?((double)s.RatingSum/s.RatedTrips).ToString("0.0"):"--";
            return new[]{ !Active?"Entrar em servico":offered?"Aceitar: $"+quote:"Status: "+Summary,
                offered?pickupPoint.Name+" > "+dropPoint.Name:"GPS do objetivo",
                offered?"Recusar solicitacao":"Cancelar corrida atual",
                Active?"Encerrar turno":"Menu / como trabalhar",
                "Corridas: "+s.Deliveries+" / $"+s.Earned,"Avaliacao: "+rating+" / 5" };
        }
        public void PhoneAction(int index)
        {
            if(index==0){if(!Active)Start();else if(offered)Accept();else Notify.Show(Summary);}
            else if(index==1)Gps();
            else if(index==2){if(Active)CancelRide("Solicitacao/corrida cancelada, sem pagamento.");}
            else if(index==3){if(Active)End("Turno Uber encerrado.");else OpenMenu();}
        }
        protected override void BuildMenu()
        {
            if(!Active)Add("Entrar em servico","Seja motorista de um carro parado, vazio e com banco de passageiro livre. Use seu carro; ele sera preservado.",Start);
            else
            {
                Add(Summary,"O turno termina ao completar "+options.TripsPerShift+" corridas, ou ao encerrar pelo menu/End.",()=>Notify.Show(Summary));
                if(offered)Add("Aceitar: $"+quote,pickupPoint.Name+" > "+dropPoint.Name,Accept);
                if(offered || ride!=null)Add("Cancelar solicitacao/corrida","Sem pagamento para o atendimento cancelado.",()=>CancelRide("Corrida cancelada."));
            }
            var p=Wallet.JobProgress(Name);
            Add("Corridas e avaliacao",p.Deliveries+" corridas / "+(p.RatedTrips>0?((double)p.RatingSum/p.RatedTrips).ToString("0.0"):"--")+" estrelas. Gorjeta depende do estado do carro.",()=>Notify.Show("Pagamento no banco por corrida concluida."));
        }
        void Start()
        {
            var p=Game.Player.Character;var v=p.IsInVehicle()?p.CurrentVehicle:null;
            if(v==null || !v.Exists() || v.IsDead || !v.IsDriveable || !v.Model.IsCar || v.Driver!=p || v.PassengerCount!=0 || !v.IsSeatFree(VehicleSeat.Passenger) || v.Speed>.5f || JobVehicles.Contains(v))
            {Notify.Show("Pare um carro vazio com banco de passageiro e fique como motorista.");return;}
            if(!BeginShift(options.TripsPerShift))return;
            vehicle=v;originalLock=v.LockStatus;unlocked=false;driverAway=-1;JobVehicles.Reserve(v);
            if(originalLock!=VehicleLockStatus.Unlocked){v.LockStatus=VehicleLockStatus.Unlocked;unlocked=true;}
            nextOffer=Game.GameTime+2000;offered=false;ride=null;Record("Entrou em servico.");Notify.Show("Uber online. Aguarde a solicitacao. F1 no carro parado ou F10 para gerenciar; End encerra.");
        }
        bool Offer()
        {
            var chosen=RideOffers.Select(options,Point(vehicle.Position),Random);
            if(chosen==null){Notify.Show("Sem pontos Uber proximos nesta area. Volte a area atendida ou encerre o turno.");return false;}
            var a=JobActors.Road(chosen.Item1);var b=JobActors.Road(chosen.Item2);
            if(a==Vector3.Zero || b==Vector3.Zero || a.DistanceTo2D(b)<400)return false;
            pickupPoint=chosen.Item1;dropPoint=chosen.Item2;pickup=a;drop=b;
            quote=DeliveryRoute.Pay(options.BaseFare,options.PayPerMeter,a.DistanceTo2D(b));
            offered=true;offerUntil=Game.GameTime+30000;Route(pickup,"solicitacao: "+pickupPoint.Name,BlipSprite.Friend);
            Notify.Show("Uber: "+pickupPoint.Name+" > "+dropPoint.Name+" / $"+quote+" + gorjeta. Pare e pressione E ou aceite no celular.");
            Record("Solicitacao: "+pickupPoint.Name+" > "+dropPoint.Name+" / $"+quote);if(Menu.Visible)OpenMenu();return true;
        }
        void Accept()
        {
            if(!Active || !offered || unchecked(Game.GameTime-offerUntil)>=0)return;
            if(vehicle==null || !vehicle.Exists() || vehicle.Driver!=Game.Player.Character || vehicle.Speed>.5f){Notify.Show("Pare o carro de trabalho para aceitar.");return;}
            ride=new RideRun();offered=false;spawnRetry=0;spawnFailures=0;tripUntil=Game.GameTime+600000;
            Route(pickup,"buscar passageiro: "+pickupPoint.Name,BlipSprite.Friend);Menu.Visible=false;Record("Corrida aceita.");Notify.Show("Siga o GPS. Pare perto do passageiro e pressione E/buzina para embarcar.");
        }
        void CancelRide(string message)
        {
            ride?.Cancel();RetirePassenger();ride=null;offered=false;quality=null;ClearRoute();nextOffer=Game.GameTime+7000;Menu.Visible=false;Notify.Show(message);
        }
        void RetirePassenger() { if(passenger!=null && passenger.Exists())JobActors.Retire(passenger);passenger=null; }
        protected override void StopOwned()
        {
            RetirePassenger();ride=null;offered=false;quality=null;
            if(vehicle!=null && vehicle.Exists() && unlocked && vehicle.LockStatus==VehicleLockStatus.Unlocked)vehicle.LockStatus=originalLock;
            JobVehicles.Release(vehicle);vehicle=null;unlocked=false;
        }
        protected override void WorkTick()
        {
            var p=Game.Player.Character;
            if(vehicle==null || !vehicle.Exists() || vehicle.IsDead || !vehicle.IsDriveable){End("Carro indisponivel. Turno Uber encerrado.");return;}
            if(!p.IsInVehicle(vehicle) || vehicle.Driver!=p)
            {
                if(driverAway<0)driverAway=Game.GameTime;
                if(unchecked(Game.GameTime-driverAway)>60000){End("Voce abandonou o carro de trabalho.");return;}
                Status("retorne ao seu carro");return;
            }
            driverAway=-1;
            if(ride==null)
            {
                Status(offered?"solicitacao: $"+quote:"aguardando solicitacao");
                if(offered)
                {
                    if(unchecked(Game.GameTime-offerUntil)>=0){offered=false;ClearRoute();nextOffer=Game.GameTime+7000;if(Menu.Visible)OpenMenu();return;}
                    if(InputAllowed && vehicle.Speed<.5f){Prompt.Help("~INPUT_CONTEXT~: aceitar corrida Uber de $"+quote);if(Game.IsControlJustPressed(GTA.Control.Context))Accept();}
                }
                else if(unchecked(Game.GameTime-nextOffer)>=0 && vehicle.PassengerCount==0){if(!Offer())nextOffer=Game.GameTime+15000;}
                return;
            }
            if(passenger!=null && (!passenger.Exists() || passenger.IsDead)){CancelRide("Passageiro indisponivel. Corrida cancelada.");return;}
            if(ride.Stage==RideStage.Pickup && unchecked(Game.GameTime-tripUntil)>=0){CancelRide("Tempo de busca esgotado.");return;}
            if(quality!=null)quality.Sample(vehicle.BodyHealth);
            switch(ride.Stage)
            {
                case RideStage.Pickup:
                    Status("busque o passageiro");Marker(pickup,2);
                    if(passenger==null && vehicle.Position.DistanceTo(pickup)<100 && unchecked(Game.GameTime-spawnRetry)>=0)
                    {
                        var safe=JobActors.Sidewalk(pickup);
                        if(safe==Vector3.Zero || World.GetNearbyPeds(safe,1.2f).Length>0)
                        {
                            spawnRetry=Game.GameTime+1000;
                            if(vehicle.Position.DistanceTo(pickup)<50 && ++spawnFailures>=10)CancelRide("Calcada indisponivel/ocupada. Nova solicitacao em breve.");
                            return;
                        }
                        passenger=JobActors.Create(safe,Random);
                        if(passenger==null || !passenger.Exists()){CancelRide("Nao foi possivel gerar o passageiro.");return;}
                        Function.Call(Hash.TASK_STAND_STILL,passenger,-1);Route(safe,"passageiro",BlipSprite.Friend);
                    }
                    if(passenger!=null && InputAllowed && vehicle.Speed<.5f && passenger.Position.DistanceTo(vehicle.Position)<18)
                    {
                        Prompt.Help("~INPUT_CONTEXT~ / buzina: embarcar passageiro");
                        if((Game.IsControlJustPressed(GTA.Control.Context) || Game.IsControlJustPressed(GTA.Control.VehicleHorn)) && vehicle.IsSeatFree(VehicleSeat.Passenger))
                        {ride.Board();JobActors.Enter(passenger,vehicle,VehicleSeat.Passenger);taskUntil=Game.GameTime+35000;}
                    }
                    break;
                case RideStage.Boarding:
                    Status("aguarde o embarque");
                    if(ride.Boarded(passenger.IsInVehicle(vehicle) && vehicle.GetPedOnSeat(VehicleSeat.Passenger)==passenger))
                    {quality=new DrivingQuality(vehicle.BodyHealth);Route(drop,"destino: "+dropPoint.Name);Notify.Show("Passageiro embarcou. Siga o destino e preserve o carro.");}
                    else if(unchecked(Game.GameTime-taskUntil)>=0){ride.BoardingTimedOut();Function.Call(Hash.TASK_STAND_STILL,passenger,-1);Notify.Show("Embarque bloqueado. Aproxime o carro, destrave/libere a porta e tente E novamente.");}
                    break;
                case RideStage.Riding:
                    Status("leve o passageiro");Marker(drop,3);
                    if(!passenger.IsInVehicle(vehicle)){CancelRide("Passageiro saiu antes do destino. Sem pagamento.");return;}
                    if(InputAllowed && vehicle.Position.DistanceTo(drop)<12 && vehicle.Speed<.5f)
                    {
                        Prompt.Help("~INPUT_CONTEXT~: desembarcar no destino");
                        if(Game.IsControlJustPressed(GTA.Control.Context) && ride.Arrived(true)){JobActors.Leave(passenger,vehicle);taskUntil=Game.GameTime+35000;}
                    }
                    break;
                case RideStage.Exiting:
                    Status("aguarde o desembarque");
                    if(ride.PassengerExited(!passenger.IsInVehicle() && passenger.Position.DistanceTo(drop)<40 && vehicle.Position.DistanceTo(drop)<25))
                    {JobActors.Walk(passenger,passenger.Position+new Vector3(5,3,0));}
                    else if(InputAllowed && unchecked(Game.GameTime-taskUntil)>=0)
                    {Prompt.Help("Pare no destino e pressione ~INPUT_CONTEXT~ para tentar desembarcar novamente.");if(Game.IsControlJustPressed(GTA.Control.Context) && vehicle.Position.DistanceTo(drop)<15 && vehicle.Speed<.5f){JobActors.Leave(passenger,vehicle);taskUntil=Game.GameTime+35000;}}
                    break;
                case RideStage.Payment:
                    Status("confirmando pagamento");
                    if(InputAllowed && ride.Pay(()=>Shift.Confirm((step,last)=>Award(step,quality.Reward(quote,options.CleanTip,last?options.ShiftBonus:0),last,quality.Stars))))
                    {
                        Record("Corrida concluida: "+quality.Stars+" estrelas. Pagamento no banco.");RetirePassenger();ride=null;quality=null;ClearRoute();
                        if(Shift.Finished){End("Turno Uber concluido. Bonus de turno incluido no ultimo pagamento.");return;}
                        nextOffer=Game.GameTime+7000;
                    }
                    break;
            }
        }
    }
}
