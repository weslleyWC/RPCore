using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
namespace RPCore
{
    internal enum UrbanStage { AwaitVehicle,Loading,NeedPackage,Carrying,Handover,Returning }
    internal sealed partial class UrbanDeliverySystem
    {
        Vehicle car;Ped client;Prop gift;Blip target,carBlip;Model requestedModel;UrbanStage stage;UrbanDeliveryContract pendingPlan;
        bool preparing,supplied;int preparedAt,last,saveAt,recipientAt,recipientNext,departureAt,cancelAt=-1,handoverIndex;long pendingMs;int pendingQuality;float actionSeconds,damageBaseline;
        int clientIndex=-1;
        bool ClientFor(UrbanDeliveryContract c)=>ClientValid && departureAt==0 && clientIndex==c.Index;
        int actorHandle,actorModel;long actorAddress;readonly List<VehicleDoorIndex> opened=new List<VehicleDoorIndex>();
        bool CarValid=>car!=null && PdmAssets.Valid(car);bool ClientValid=>client!=null && PdmAssets.Valid(client) && !client.IsDead;
        bool Bound=>profile==Profile.Id && actorHandle==Game.Player.Character.Handle && actorModel==Game.Player.Character.Model.Hash && actorAddress==Game.Player.Character.MemoryAddress.ToInt64();
        bool Input=>Consult && !pool.AreAnyVisible;
        bool Driver=>CarValid && car.Driver==Game.Player.Character && Game.Player.Character.IsInVehicle(car);
        Vector3 Rear=>CarValid?car.GetOffsetPosition(new Vector3(0,car.Model.IsBike || Pizza?-1.4f:-3.2f,0)):Vector3.Zero;
        UrbanDeliveryContract Plan()
        {
            int count=Math.Min(selectedCount,options.Addresses.Select(p=>p.X+":"+p.Y+":"+p.Z).Distinct().Count());var points=Freight?FreightDefaults.Route(freightOptions,random):DeliveryRoute.Select(options.Addresses,office,count,random);
            var plan=new UrbanDeliveryContract{Id=Guid.NewGuid().ToString("N"),Job=job,Office=CharacterRules.Copy(office),Depot=CharacterRules.Copy(depot),LoadSeconds=Freight?freightOptions.LoadSeconds:3,ReturnBonus=Pizza?Math.Min(1000000,Cfg.PizzaShiftBonus):options.ReturnBonus,PersonalVehicle=job=="postop" && personal};plan.Office.Name=Label;plan.Depot.Name=Label+" / deposito";
            var from=plan.Depot;foreach(var point in points){double distance=DeliveryRoute.Distance(from,point);plan.Stops.Add(new UrbanStop{Address=CharacterRules.Copy(point),Pay=DeliveryRoute.Pay(Pizza?Cfg.PizzaBase:options.BasePay,Pizza?Cfg.PizzaPerMeter:options.PayPerMeter,distance),TimelyBonus=Pizza?Math.Min(1000000,Cfg.PizzaOnTimeBonus):0,SecondsAllowed=(int)Math.Min(3600,Math.Max(60,distance/8+45))});from=point;}return plan;
        }
        void Prepare(bool resume)
        {
            if(Active || quickActive())return;if(!Consult || !AtOffice || Game.Player.Wanted.WantedLevel>0){Notify.Show(Label+": va a pe e desarmado ao atendimento, sem procurado/missao.");return;}
            if(!TryStats(out var stats) || resume!=(stats.UrbanContract!=null)){Notify.Show("Confira a rota pendente antes de iniciar.");return;}
            if(!gate.Acquire(job)){Notify.Show("Encerre a atividade de "+gate.Owner+".");return;}
            try{
                pendingPlan=resume?CharacterRules.Copy(stats.UrbanContract):Plan();id=pendingPlan.Id;profile=Profile.Id;var player=Game.Player.Character;actorHandle=player.Handle;actorModel=player.Model.Hash;actorAddress=player.MemoryAddress.ToInt64();last=Game.GameTime;pendingMs=0;pendingQuality=0;actionSeconds=0;
                if(pendingPlan.PersonalVehicle){CommitPlan(resume);stage=UrbanStage.AwaitVehicle;ui.Close();Notify.Show("Entre como condutor no seu veiculo para identifica-lo para o Post OP.");return;}
                requestedModel=new Model(Freight?"mule2":Pizza?"faggio":"boxville2");if(!requestedModel.IsValid || !requestedModel.IsInCdImage){Suspend(false);Notify.Show("Modelo indisponivel; nenhuma rota nova criada.");return;}
                Function.Call(Hash.REQUEST_MODEL,requestedModel.Hash);preparing=true;preparedAt=Game.GameTime;ui.Close();Notify.Show(Label+": preparando veiculo em vaga livre. Aguarde no atendimento.");
            }catch{Suspend(false);throw;}
        }
        void CommitPlan(bool resume)
        {
            bool ok=resume?UrbanDeliveryTransactions.ReloadCargo(Wallet.DeliveryWallet(),job,id,Wallet.Save,out _):UrbanDeliveryTransactions.Begin(Wallet.DeliveryWallet(),pendingPlan,Wallet.Save,out _);if(!ok)throw new InvalidOperationException("Delivery plan not saved.");pendingPlan=null;Announce(resume?"Rota retomada com destinos/pagamentos anteriores preservados.":"Nova rota de "+Contract.Stops.Count+" destinos contratada; carregue os volumes na base.");
        }
        void Spawn()
        {
            var anchor=CharacterRules.Copy(pendingPlan.Depot);var road=JobActors.Road(anchor);if(road!=Vector3.Zero && road.DistanceTo(Pos(anchor))<=45){anchor.X=road.X;anchor.Y=road.Y;anchor.Z=road.Z;}
            var bay=RigSpawn.FindVehicle(anchor,requestedModel,Label);if(bay==null){Suspend(false);Notify.Show("Saida ocupada ou piso indisponivel. Rota anterior preservada.");return;}
            car=PdmAssets.Track(World.CreateVehicle(requestedModel,Pos(bay),bay.Heading));if(!CarValid)throw new InvalidOperationException("Delivery vehicle unavailable.");supplied=true;car.IsPersistent=true;car.IsStolen=false;car.NeedsToBeHotwired=false;car.FuelLevel=Math.Max(1,car.PetrolTankVolume);Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT,car,"UD"+id.Substring(0,6).ToUpperInvariant());JobVehicles.Reserve(car);carBlip=car.AddBlip();carBlip.Name=Label+" / veiculo";carBlip.Color=BlipColor.Blue;
            bool resume=Contract!=null;CommitPlan(resume);preparing=false;requestedModel.MarkAsNoLongerNeeded();requestedModel=default(Model);stage=UrbanStage.AwaitVehicle;damageBaseline=car.EngineHealth+car.BodyHealth;Notify.Show("Retire o veiculo marcado, estacione na base e carregue pela traseira. Sem entrega automatica ao passar pelo endereco.");SetRoute();
        }
        void SetRoute()
        {
            var c=Contract;if(c==null)return;var point=stage==UrbanStage.Loading || stage==UrbanStage.AwaitVehicle || c.Returning?c.Depot:c.Stops[c.Index].Address;
            if(target!=null && target.Exists())target.Delete();target=World.CreateBlip(Pos(point));target.Name=Label+" / "+(c.Returning?"devolucao":stage==UrbanStage.Loading || stage==UrbanStage.AwaitVehicle?"retirar e carregar":point.Name);target.Color=BlipColor.Yellow;target.ShowRoute=true;
            if(!c.Returning && stage!=UrbanStage.Loading && stage!=UrbanStage.AwaitVehicle)WorkContext.ShopDelivery.Set(job,c.Stops[c.Index].Address,5);else WorkContext.ShopDelivery.Clear(job);
        }
        public void Tick()
        {
            try{
                ui.Tick(Consult);DrawInfo();
                if(!Active){if(Input && !quickActive()){DrawMarker(Pos(office),1.5f,Color.FromArgb(100,80,170,230));if(Foot && Game.Player.Character.Position.DistanceTo(Pos(office))<=2){Prompt.Help("~INPUT_CONTEXT~: "+Label+" / contratos");if(Game.IsControlJustPressed(GTA.Control.Context))OpenMenu();}}return;}
                if(!Bound){Suspend(false);return;}
                var player=Game.Player.Character;
                if(preparing){if(!Consult || !AtOffice || Game.Player.Wanted.WantedLevel>0 || Game.GameTime-preparedAt>15000){Suspend(false);Notify.Show("Preparacao interrompida; rota salva conservada.");return;}if(requestedModel.IsLoaded && Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY,player))Spawn();return;}
                var c=Contract;if(c==null){Suspend(false);return;}
                if(player.IsDead || Game.IsMissionActive){Suspend(true);return;}
                int delta=unchecked(Game.GameTime-last);last=Game.GameTime;float dt=Math.Max(0,Math.Min(1000,delta))/1000f;
                if(stage==UrbanStage.AwaitVehicle && c.PersonalVehicle && !CarValid){
                    if(Input && player.IsInVehicle() && player.CurrentVehicle.Driver==player && !JobVehicles.Contains(player.CurrentVehicle) && (player.CurrentVehicle.Model.IsCar || player.CurrentVehicle.Model.IsBike)){
                        car=PdmAssets.Track(player.CurrentVehicle);supplied=false;JobVehicles.Reserve(car);damageBaseline=car.EngineHealth+car.BodyHealth;carBlip=car.AddBlip();carBlip.Name="Post OP / seu veiculo de entrega";carBlip.Color=BlipColor.Blue;SetRoute();
                    }else Prompt.Help("Entre como motorista no seu carro/moto para identifica-lo. End segurado suspende.");return;
                }
                if(!CarValid || car.IsDead){pendingQuality=Math.Min(100,pendingQuality+30);Suspend(true);return;}
                if(!Consult || pool.AreAnyVisible){actionSeconds=0;if(stage==UrbanStage.Handover)stage=UrbanStage.Carrying;cargo.Stop();return;}
                pendingMs+=(long)(dt*1000);float health=car.EngineHealth+car.BodyHealth;if(damageBaseline-health>=25){pendingQuality=Math.Min(100,pendingQuality+10);damageBaseline=health;Notify.Show(Label+": colisao reduziu a qualidade da carga.");}else if(health>damageBaseline)damageBaseline=health;
                if(Game.GameTime>=saveAt && (pendingMs>=1000 || pendingQuality>0)){Save();saveAt=Game.GameTime+5000;}
                if(carBlip!=null && carBlip.Exists()){carBlip.Position=car.Position;carBlip.Alpha=Driver?0:255;}
                if(opened.Count>0 && player.IsInVehicle(car))CloseDoors();
                if((stage==UrbanStage.Carrying || stage==UrbanStage.Handover) && !Foot){cargo.Stop();stage=UrbanStage.NeedPackage;actionSeconds=0;Notify.Show("Pacote retornou a carga; retire novamente, a pe e desarmado.");}
                if(stage==UrbanStage.Carrying && !cargo.Started)cargo.Start(player,Pizza);cargo.Tick();if(stage==UrbanStage.Carrying && cargo.Failed){cargo.Stop();stage=UrbanStage.NeedPackage;Notify.Show("Carga visual indisponivel; retire novamente. Nenhuma entrega foi registrada.");}RecipientTick(c);DrawStatus(c);
                if(stage==UrbanStage.AwaitVehicle){if(Driver){stage=c.Returning?UrbanStage.Returning:UrbanStage.Loading;SetRoute();Notify.Show(c.Returning?"Devolva o veiculo na base para encerrar.":"Carregue: estacione na base, saia e use E junto a traseira.");}return;}
                if(stage==UrbanStage.Loading){LoadTick(c,dt);return;}if(c.Returning){ReturnTick(c);return;}
                var door=Pos(c.Stops[c.Index].Address);DrawMarker(door,1.5f,Color.FromArgb(120,255,200,80));
                if(stage==UrbanStage.NeedPackage){if(player.Position.DistanceTo(door)<50){DrawMarker(Rear,1,Color.FromArgb(100,80,170,230));if(Foot && car.Speed<.5f && player.Position.DistanceTo(Rear)<2.5f){Prompt.Help("~INPUT_CONTEXT~: retirar "+(Pizza?"pizza":"caixa")+" da carga");if(Game.IsControlJustPressed(GTA.Control.Context)){OpenDoors();cargo.Start(player,Pizza);stage=UrbanStage.Carrying;}}}return;}
                var handover=ClientFor(c)?client.Position:door;bool doorAllowed=!Pizza || ClientFor(c) || Game.GameTime-recipientAt>=30000;
                if(stage==UrbanStage.Carrying && player.Position.DistanceTo(handover)<2.5f){Prompt.Help(doorAllowed?"~INPUT_CONTEXT~: "+(ClientFor(c)?"entregar ao cliente":"confirmar entrega na porta"):"Aguardando cliente; entrega na porta disponivel apos 30s.");if(doorAllowed && cargo.Ready && Game.IsControlJustPressed(GTA.Control.Context)){stage=UrbanStage.Handover;handoverIndex=c.Index;actionSeconds=0;cargo.Give();}}
                if(stage==UrbanStage.Handover){if(!Foot || player.Speed>1.5f || player.Position.DistanceTo(handover)>3){stage=UrbanStage.Carrying;actionSeconds=0;return;}actionSeconds+=dt;if(actionSeconds>=1.5f)Deliver(c,handoverIndex);}
            }catch(Exception ex){Fault(ex);}
        }
        void LoadTick(UrbanDeliveryContract c,float dt)
        {
            DrawMarker(Pos(c.Depot),3,Color.FromArgb(100,80,170,230));bool ready=Foot && car.Speed<.5f && car.Position.DistanceTo(Pos(c.Depot))<30 && Game.Player.Character.Position.DistanceTo(Rear)<3;
            if(!ready){actionSeconds=0;cargo.Stop();Prompt.Help("Estacione na base, saia e aproxime-se da traseira com a arma guardada.");return;}
            if(actionSeconds<=0){Prompt.Help("~INPUT_CONTEXT~: carregar "+(c.Stops.Count-c.Index)+" volumes restantes");if(Game.IsControlJustPressed(GTA.Control.Context)){OpenDoors();cargo.Start(Game.Player.Character,Pizza);actionSeconds=.001f;}return;}
            cargo.Tick();actionSeconds+=dt;Prompt.Help("Carregando "+actionSeconds.ToString("0.0")+"/"+c.LoadSeconds+"s / permaneça junto a carga");
            if(actionSeconds>=c.LoadSeconds && !cargo.Ready){cargo.Stop();actionSeconds=0;Notify.Show("Modelo da carga indisponivel; tente carregar novamente. Rota preservada.");return;}
            if(actionSeconds>=c.LoadSeconds && UrbanDeliveryTransactions.Load(Wallet.DeliveryWallet(),job,id,Wallet.Save,out _)){cargo.Stop();actionSeconds=0;stage=UrbanStage.NeedPackage;SetRoute();Notify.Show("Carga pronta. Siga o GPS e retire um volume em cada endereco.");}
        }
        void Deliver(UrbanDeliveryContract c,int index)
        {
            Save();int amount=UrbanDeliveryRules.Quote(Contract);if(!Wallet.UrbanDrop(job,id,index,false,out bool changed)){Notify.Show("Pagamento pendente; confira limite de saldo e tente novamente.");stage=UrbanStage.Carrying;actionSeconds=0;return;}
            if(changed){updated();Bump(false,amount);Announce("Entrega "+(index+1)+" confirmada. Recebido $"+amount+(c.Bank?" no banco.":" na carteira."));Notify.Show("Entrega confirmada / +$"+amount+(c.Bank?" no banco":" na carteira"));if(ClientFor(c)){gift=cargo.PassTo(client);client.Task.StandStill(4000);departureAt=Game.GameTime+4000;client.PlayAmbientSpeech("GENERIC_THANKS");}else cargo.Stop();}
            stage=Contract.Returning?UrbanStage.Returning:UrbanStage.NeedPackage;actionSeconds=0;recipientAt=0;SetRoute();
        }
        void Skip(int index)
        {
            if(!Active || stage!=UrbanStage.NeedPackage || !AtRest() || Contract?.Index!=index){Notify.Show("Pare e recolha a carga antes de devolver um pacote.");return;}
            Save();if(!Wallet.UrbanDrop(job,id,index,true,out _))throw new InvalidOperationException("Skip not saved.");ReleaseRecipient();stage=Contract.Returning?UrbanStage.Returning:UrbanStage.NeedPackage;recipientAt=0;SetRoute();OpenMenu();
        }
        bool AtRest()=>CarValid && car.Speed<.5f && Game.Player.Wanted.WantedLevel==0;
        void ReturnTick(UrbanDeliveryContract c)
        {
            DrawMarker(Pos(c.Depot),3,Color.FromArgb(120,80,200,130));if(!Input || !Driver || !AtRest() || car.Position.DistanceTo(Pos(c.Depot))>18)return;Prompt.Help("~INPUT_CONTEXT~: encerrar rota e "+(supplied?"devolver veiculo":"liberar seu veiculo"));
            if(!Game.IsControlJustPressed(GTA.Control.Context))return;Save();int bonus=UrbanDeliveryRules.Bonus(Contract);if(!Wallet.UrbanReturn(job,id,out bool changed)){Notify.Show("Recebimento pendente; confira saldo/gravacao antes de repetir.");return;}if(changed){updated();if(c.Delivered>0)Bump(true,bonus);Announce("Rota concluida: "+c.Delivered+" entregas e "+c.Skipped+" devolucoes. Bonus $"+bonus+"; comprovante no menu.");}bool company=supplied;Suspend(false);Notify.Show("Turno concluido. Bonus de retorno $"+bonus+"; comprovante no menu."+(company?" Desca para devolver o veiculo da empresa.":" Seu veiculo foi liberado."));
        }
        void ContinuePizza()
        {
            var c=Contract;if(!Pizza || !Active || c?.Returning!=true || !Driver || !AtRest() || car.Position.DistanceTo(Pos(c.Depot))>18){Notify.Show("Retorne a base, pare e permaneça como condutor para continuar.");return;}
            Save();var next=Plan();int bonus=UrbanDeliveryRules.Bonus(Contract);if(!Wallet.UrbanReturn(job,id,out bool changed,next))throw new InvalidOperationException("Continuation not saved.");if(!changed)return;updated();Bump(true,bonus);ReleaseRecipient();cargo.Stop();id=next.Id;stage=UrbanStage.Loading;actionSeconds=0;recipientAt=0;SetRoute();ui.Close();Notify.Show("Lote anterior pago. Desca e recarregue as pizzas da nova rota na mesma scooter.");
        }
        void Bump(bool complete,int amount){try{if(complete)achievements.Bump(Freight?"supply_shifts":Pizza?"shifts_done":"postop_shifts");else achievements.Bump(Freight?"supply_drops":Pizza?"pizza_drops":"postop_drops");if(amount>0)achievements.Bump("cash_earned",amount);achievements.Save();Sfx.Play("delivered");}catch(Exception ex){Log.Error(job+" achievement after committed payment",ex);}}
        void RecipientTick(UrbanDeliveryContract c)
        {
            if(departureAt>0){if(Game.GameTime>=departureAt){ReleaseRecipient();departureAt=0;}return;}
            if(!Pizza || c.Returning || stage==UrbanStage.Loading || stage==UrbanStage.AwaitVehicle || Game.Player.Character.Position.DistanceTo(Pos(c.Stops[c.Index].Address))>65)return;
            if(recipientAt==0)recipientAt=Game.GameTime;if(ClientValid || Game.GameTime<recipientNext)return;recipientNext=Game.GameTime+3000;
            var point=JobActors.Sidewalk(Pos(c.Stops[c.Index].Address));if(point==Vector3.Zero || point.DistanceTo(Pos(c.Stops[c.Index].Address))>10 || !JobActors.Free(point,1.5f))return;
            if(client!=null)ReleaseRecipient();client=PdmStreaming.Buyer(point,id+":"+c.Index);if(ClientValid){clientIndex=c.Index;client.Task.StandStill(30000);client.Task.LookAt(Game.Player.Character);}
        }
        void OpenDoors(){if(!CarValid || Pizza)return;foreach(var door in new[]{VehicleDoorIndex.BackLeftDoor,VehicleDoorIndex.BackRightDoor,VehicleDoorIndex.Trunk})if(Function.Call<bool>(Hash.GET_IS_DOOR_VALID,car,(int)door) && !car.Doors[door].IsOpen){car.Doors[door].Open(false,false);opened.Add(door);}}
        void CloseDoors(){if(CarValid)foreach(var door in opened)if(Function.Call<bool>(Hash.GET_IS_DOOR_VALID,car,(int)door))car.Doors[door].Close(false);opened.Clear();}
        void ReleaseRecipient(){if(gift!=null && PdmAssets.Valid(gift)){gift.Detach();gift.Delete();}gift=null;if(client!=null && PdmAssets.Valid(client))JobActors.Retire(client);client=null;clientIndex=-1;departureAt=0;}
        void DrawMarker(Vector3 at,float size,Color color){if(Game.Player.Character.Position.DistanceTo(at)<70)World.DrawMarker(MarkerType.Cylinder,at-new Vector3(0,0,.9f),Vector3.Zero,Vector3.Zero,new Vector3(size,size,.6f),color);}
        void DrawStatus(UrbanDeliveryContract c)
        {
            string task=stage==UrbanStage.AwaitVehicle?"retire o veiculo":stage==UrbanStage.Loading?"carregue na base":c.Returning?"retorne para encerrar":stage==UrbanStage.NeedPackage?"retire um volume na traseira":stage==UrbanStage.Carrying?"leve o volume a pe":"entregando / mantenha-se parado";
            Visuals.TryJobHeader(job,Label+" / "+c.Index+"/"+c.Stops.Count,Color.White);float x=(Visuals.Width-760)/2;Visuals.Rect(x,900,760,85,Color.FromArgb(220,10,18,28));Visuals.Label(task+" / qualidade "+Math.Max(0,c.Quality-pendingQuality)+"% / End segurado: suspender",x+12,910,.31f,Color.White,wrap:736);
            Visuals.Text(c.Returning?"Bonus previsto $"+UrbanDeliveryRules.Bonus(c):c.Stops[c.Index].Address.Name+" / entrega prevista $"+UrbanDeliveryRules.Quote(c)+(Pizza?" / "+Math.Max(0,c.Stops[c.Index].SecondsAllowed-c.StopSeconds)+"s para pontualidade":""),x+12,955,.27f,Color.FromArgb(140,210,240));
        }
        public void Save()
        {
            if(!Active || profile!=Profile.Id || Contract?.Id!=id)return;while(pendingMs>=1000 || pendingQuality>0){int seconds=(int)Math.Min(30,pendingMs/1000),loss=pendingQuality;if(!UrbanDeliveryTransactions.Advance(Wallet.DeliveryWallet(),job,id,seconds,loss,Wallet.Save,out _))throw new InvalidOperationException("Delivery time checkpoint refused.");pendingMs-=seconds*1000;pendingQuality=0;}
        }
        void Cancel(){var c=Contract;if(c==null)return;Save();if(!UrbanDeliveryTransactions.Cancel(Wallet.DeliveryWallet(),job,c.Id,"Cancelado pelo jogador; pagamentos anteriores mantidos",Wallet.Save,out _))throw new InvalidOperationException("Delivery cancel not saved.");Suspend(false);OpenMenu();}
        public void PollCancel(){if(!Active || !Input || !Game.IsKeyPressed(System.Windows.Forms.Keys.End)){cancelAt=-1;return;}if(cancelAt<0)cancelAt=Game.GameTime;if(Game.GameTime-cancelAt>=1000)Suspend(true);}
        void Suspend(bool notify)
        {
            bool saved=Contract!=null;try{Save();}finally{
                cargo.Stop();CloseDoors();ReleaseRecipient();if(target!=null && target.Exists())target.Delete();target=null;if(carBlip!=null && carBlip.Exists())carBlip.Delete();carBlip=null;
                if(CarValid){if(supplied)JobVehicles.Retire(car);else JobVehicles.Release(car);}car=null;supplied=false;if(requestedModel.Hash!=0)requestedModel.MarkAsNoLongerNeeded();requestedModel=default(Model);preparing=false;pendingPlan=null;WorkContext.ShopDelivery.Clear(job);gate.Release(job);id=profile=null;pendingMs=0;pendingQuality=0;actionSeconds=0;recipientAt=recipientNext=0;cancelAt=-1;ui.Close();
            }if(notify)Notify.Show(saved?Label+": turno suspenso. Retome a rota salva no atendimento; entregas confirmadas permanecem.":Label+": preparacao encerrada; nenhuma rota nova foi gravada.");
        }
        public void Reload(){Suspend(false);ui.Reset();}public void Dispose(){Suspend(false);ui.Reset();}
    }
}
