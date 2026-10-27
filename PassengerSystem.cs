using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
namespace RPCore
{
    public sealed class PassengerSystem : ISystem
    {
        public string Name=>"passenger_service";
        const string Decor="RPCore_Passenger";
        readonly ObjectPool pool;readonly ShiftGate gate;readonly Func<bool> otherFree;readonly PassengerOptions defaults;
        readonly NativeMenu menu=new NativeMenu("Transporte","Taxi / limousine");readonly Random random=new Random();
        readonly List<Tuple<Vehicle,Ped,int>> retired=new List<Tuple<Vehicle,Ped,int>>();
        Vehicle car;Ped driver,actor;Blip blip;Model carModel,pedModel;string profile,id;int token,last,wait,meter,retask;bool loading,boarded,arrived,fast,stopping;
        Vector3 pickup,destination;bool hasDestination;
        public PassengerSystem(ObjectPool pool,ShiftGate gate,Func<bool> otherFree)
        {this.pool=pool;this.gate=gate;this.otherFree=otherFree;defaults=Store.LoadDef("Services/Taxi.json",PassengerOptions.Defaults);pool.Add(menu);Visuals.Register(menu,"garage");if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);Reload();}
        bool Calm=>!Game.IsMissionActive && !Game.Player.Character.IsDead && Game.Player.Wanted.WantedLevel==0 && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool Own(Entity e,int key)=>e!=null && e.Exists() && Function.Call<bool>(Hash.DECOR_EXIST_ON,e,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,e,Decor)==key;
        void Mark(Entity e){if(e==null || !e.Exists())throw new InvalidOperationException("Falha na criacao do transporte.");Function.Call(Hash.DECOR_SET_INT,e,Decor,token);e.IsPersistent=true;}
        bool Pair=>Own(car,token) && Own(driver,token) && driver.IsAlive && car.IsAlive && car.Driver==driver && !Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,car) && !Function.Call<bool>(Hash.IS_ENTITY_IN_AIR,car) && car.Occupants.All(p=>p==driver || p==actor);
        void Add(string title,string desc,Action action){var row=new NativeItem(title,desc);row.Activated+=(s,e)=>{try{action();}catch(Exception ex){Log.Error("passenger action",ex);Notify.Show("Operacao nao concluida; saldo/reserva preservados. Veja o log.");}};menu.Add(row);}
        public void OpenMenu()
        {
            pool.HideAll();menu.Clear();var book=Wallet.Passenger;
            if(book!=null && !book.Valid){Add("Registro protegido","Dados de transporte invalidos; arquivo original preservado.",()=>{});menu.Visible=true;return;}
            if(book?.Pending!=null)
            {
                var p=book.Pending;Add("Taximetro $"+p.Fare+" / reserva $"+p.Budget,"Saldo nao utilizado e devolvido ao banco ao encerrar. Nao ha divida automatica.",()=>{});
                Add("Encerrar viagem / devolver saldo","Para o motorista proprio e encerra a reserva; nao expulsa nem teleporta o jogador.",()=>{End("cancelled");OpenMenu();});
                if(id!=null)Add(fast?"Velocidade normal":"Pedir mais rapidez","20 ou30m/s, com regras de transito; sem teleporte.",()=>{fast=!fast;retask=0;menu.Visible=false;});
            }
            else
            {
                Add("Chamar taxi / reservar $"+defaults.TaxiBudget,"Banco. Tarifa $2 +$5 a cada15s embarcado; devolve reserva nao gasta. Marque um waypoint de rua.",()=>Begin(false));
                Add("Chamar limousine / reservar $"+defaults.LimoBudget,"Banco. Tarifa $150 +$1 a cada15s embarcado; devolve reserva nao gasta.",()=>Begin(true));
                Add("Usar este carro como modelo de taxi","Motorista, parado e sem passageiros/missao. Salva copia visual; nao transfere nem remove o carro pessoal.",Template);
                Add("Restaurar modelo de taxi padrao","Proximas chamadas usam Services/Taxi.json.",()=>{Wallet.PassengerTemplate(null);OpenMenu();});
            }
            foreach(var h in (book?.History??new List<PassengerRecord>()).AsEnumerable().Reverse().Take(6))Add((h.Limo?"Limo":"Taxi")+" / "+h.Outcome+" / $"+h.Fare,"Duracao "+(h.Elapsed/1000)+"s. Recibo unico; sem repetir cobranca na recarga.",()=>{});
            Add("Fechar","Voltar ao jogo.",()=>menu.Visible=false);menu.Visible=true;
        }
        void Template()
        {
            var p=Game.Player.Character;var v=p.CurrentVehicle;
            if(!Calm || gate.Owner!=null || !(otherFree?.Invoke()??true) || v==null || !v.Exists() || v.Driver!=p || v.Speed>.5f || v.PassengerCount>0 || !v.Model.IsCar || v.PassengerCapacity<1 || JobVehicles.Contains(v) || Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,v)){Notify.Show("Pare como motorista de carro pessoal com banco de passageiro, fora de atividades.");return;}
            if(Wallet.PassengerTemplate(VehicleSnapshot.Capture(v)))Notify.Show("Modelo visual do taxi salvo neste perfil. Chamadas criam outra placa.");OpenMenu();
        }
        void Begin(bool limo)
        {
            if(defaults?.Valid!=true || !Calm || Game.Player.Character.IsInVehicle() || !(otherFree?.Invoke()??true) || Wallet.Passenger?.Pending!=null || !gate.Acquire(Name)){Notify.Show("Chame a pe, sem procurado/missao/atividade e com registro de transporte livre.");return;}
            try
            {
                string attempt=Guid.NewGuid().ToString("N");if(!Wallet.PassengerBegin(attempt,limo,defaults)){gate.Release(Name);Notify.Show("Saldo bancario insuficiente para a reserva ou registro protegido.");return;}
                profile=Profile.Id;id=attempt;actor=Game.Player.Character;token=random.Next(1,int.MaxValue);loading=true;boarded=arrived=fast=stopping=false;hasDestination=false;wait=meter=retask=0;last=Game.GameTime;pickup=World.GetNextPositionOnStreet(actor.Position,false);
                if(pickup==Vector3.Zero || pickup.DistanceTo(actor.Position)>80 || Math.Abs(pickup.Z-actor.Position.Z)>6)throw new InvalidOperationException("Sem rua proxima acessivel.");
                var custom=!limo?Wallet.Passenger.CustomTaxi:null;
                carModel=custom!=null?new Model(custom.Model):new Model(limo?defaults.LimoModel:defaults.TaxiModel);pedModel=new Model(limo?defaults.LimoDriverModel:defaults.DriverModel);
                if(!carModel.IsValid || !carModel.IsInCdImage || !carModel.IsCar || !pedModel.IsValid || !pedModel.IsInCdImage || !pedModel.IsPed)throw new InvalidOperationException("Modelo de taxi/motorista indisponivel. Restaure o padrao se necessario.");
                carModel.Request();pedModel.Request();pool.HideAll();Notify.Show("Transporte solicitado. Aguarde o veiculo marcado; END encerra e devolve saldo nao gasto.");
            }
            catch{try{End("interrupted");}catch{Close();}throw;}
        }
        void Spawn()
        {
            if(!carModel.IsLoaded || !pedModel.IsLoaded){if(wait>=10000)End("interrupted");return;}
            Vector3 point=Vector3.Zero;float heading=0;
            for(int i=0;i<16;i++)
            {
                double angle=i*2.4;var guess=pickup+new Vector3((float)Math.Cos(angle)*55,(float)Math.Sin(angle)*55,0);var pos=new OutputArgument();var h=new OutputArgument();
                if(!Function.Call<bool>(Hash.GET_CLOSEST_VEHICLE_NODE_WITH_HEADING,guess.X,guess.Y,guess.Z,pos,h,1,3f,0))continue;
                var p=pos.GetResult<Vector3>();float floor;float d=p.DistanceTo(actor.Position);if(d<25 || d>100 || Math.Abs(p.Z-pickup.Z)>6 || !JobActors.Free(p,6) || !World.GetGroundHeight(p+new Vector3(0,0,3),out floor,GetGroundHeightMode.Normal) || Math.Abs(floor-p.Z)>2)continue;
                point=new Vector3(p.X,p.Y,floor+.1f);heading=h.GetResult<float>();break;
            }
            if(point==Vector3.Zero){if(wait>=15000)End("interrupted");return;}
            car=World.CreateVehicle(carModel,point,heading);Mark(car);JobVehicles.Reserve(car);var template=!Wallet.Passenger.Pending.Limo?Wallet.Passenger.CustomTaxi:null;
            if(template!=null){var copy=Newtonsoft.Json.JsonConvert.DeserializeObject<SavedVehicle>(Newtonsoft.Json.JsonConvert.SerializeObject(template));copy.Plate="RT"+token.ToString("D10").Substring(4);if(Wallet.Transport.FindIdentity(copy.Model,copy.Plate)!=null)throw new InvalidOperationException("Placa de transporte ja cadastrada; solicite outra chamada.");VehicleSnapshot.Apply(car,copy);}
            if(car.PassengerCapacity<1)throw new InvalidOperationException("Modelo sem lugar para passageiro.");
            Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY,car);car.IsEngineRunning=true;
            driver=World.CreatePed(pedModel,point+new Vector3(0,0,2));Mark(driver);if(!driver.IsHuman)throw new InvalidOperationException("Motorista precisa ser humano.");driver.BlockPermanentEvents=true;driver.Weapons.RemoveAll();Function.Call(Hash.SET_PED_MONEY,driver,0);driver.SetIntoVehicle(car,VehicleSeat.Driver);Function.Call(Hash.SET_PED_KEEP_TASK,driver,true);driver.SetConfigFlag(PedConfigFlagToggles.AICanDrivePlayerAsRearPassenger,true);
            if(!Pair)throw new InvalidOperationException("Motorista nao assumiu o veiculo.");
            blip=car.AddBlip();blip.Sprite=BlipSprite.PersonalVehicleCar;blip.Color=BlipColor.Yellow;blip.Name=Wallet.Passenger.Pending.Limo?"RPCore Limousine":"RPCore Taxi";
            loading=false;wait=retask=0;Drive(pickup);Notify.Show("Transporte a caminho. Entre como passageiro; E solicita embarque quando estiver parado perto.");
        }
        void Drive(Vector3 point)
        {if(!Pair)return;Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE,driver,car,point.X,point.Y,point.Z,fast?30f:20f,786603,5f);retask=5000;}
        void Halt(){if(Pair){Function.Call(Hash.CLEAR_PED_TASKS,driver);Function.Call(Hash.TASK_VEHICLE_TEMP_ACTION,driver,car,27,600000);}retask=5000;}
        public void Tick()
        {
            ReleaseRetired();int now=Game.GameTime;int dt=PassengerRules.Delta(now,last,!Game.IsPaused);last=now;if(id==null)return;
            try
            {
                if(profile!=Profile.Id || actor!=Game.Player.Character){Close();return;}
                if(!Calm || actor.IsDead || gate.Owner!=Name){End("interrupted");return;}
                if(Game.IsPaused)return;
                if(loading){wait+=dt;Spawn();return;}
                if(!Pair){End("interrupted");return;}
                retask=Math.Max(0,retask-dt);
                var pending=Wallet.Passenger?.Pending;if(pending?.Id!=id){Close();return;}
                bool passenger=actor.IsInVehicle(car) && car.Driver!=actor;
                if(!boarded)
                {
                    wait+=dt;if(wait>=pending.Options.PickupSeconds*1000 || actor.Position.DistanceTo(car.Position)>250){End("cancelled");return;}
                    if(passenger){if(!Wallet.PassengerBoard(id)){End("interrupted");return;}boarded=true;wait=meter=0;Halt();Notify.Show("Marque um waypoint para o motorista. A tarifa embarcada ja esta ativa.");}
                    else if(actor.IsInVehicle()){End("cancelled");return;}
                    else if(car.Position.DistanceTo(pickup)<10 && car.Speed<1){if(retask==0)Halt();if(actor.Position.DistanceTo(car.Position)<5 && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen){GTA.UI.Screen.ShowHelpTextThisFrame("~INPUT_CONTEXT~: embarcar no transporte");if(Game.IsControlJustPressed(GTA.Control.Context)){var seat=car.IsSeatFree(VehicleSeat.RightRear)?VehicleSeat.RightRear:VehicleSeat.Passenger;JobActors.Enter(actor,car,seat);}}}
                    else if(retask==0)Drive(pickup);
                    return;
                }
                if(!passenger){End("completed");return;}
                meter+=dt;if(meter>=15000)
                {int elapsed=Math.Min(pending.Options.MaxMinutes*60000,pending.Elapsed+15000);if(!Wallet.PassengerCheckpoint(id,elapsed))throw new InvalidOperationException("Falha ao gravar taximetro.");meter-=15000;pending=Wallet.Passenger.Pending;}
                if(stopping || pending.Fare>=pending.Budget || pending.Elapsed>=pending.Options.MaxMinutes*60000)
                {if(!stopping){Halt();stopping=true;}GTA.UI.Screen.ShowSubtitle("Limite da viagem: saia para encerrar. F11 > Transporte tambem encerra.");return;}
                GTA.UI.Screen.ShowSubtitle("Taximetro: $"+pending.Fare+" / reserva $"+pending.Budget+" | END encerra");
                if(!Game.IsWaypointActive){if(hasDestination || retask==0)Halt();hasDestination=false;arrived=false;GTA.UI.Screen.ShowHelpTextThisFrame("Marque um waypoint de rua no mapa ou saia para encerrar.");return;}
                var road=World.GetNextPositionOnStreet(World.WaypointPosition,false);
                if(road==Vector3.Zero || road.DistanceTo2D(World.WaypointPosition)>100 || Math.Abs(road.Z-World.WaypointPosition.Z)>30){if(retask==0)Halt();GTA.UI.Screen.ShowHelpTextThisFrame("Destino sem acesso rodoviario proximo. Marque outro waypoint.");return;}
                if(!hasDestination || destination.DistanceTo(road)>10){destination=road;hasDestination=true;arrived=false;Drive(destination);}
                if(car.Position.DistanceTo(destination)<=10){if(!arrived && retask==0)Halt();arrived=PassengerRules.Arrival(passenger,Pair,car.Speed,car.Position.DistanceTo(destination));GTA.UI.Screen.ShowHelpTextThisFrame(arrived?"Destino alcancado. Saia para pagar ou marque outro waypoint.":"Aguarde o motorista parar no destino.");}
                else if(!arrived && retask==0)Drive(destination);
            }
            catch(Exception ex){Log.Error("passenger tick",ex);try{End("interrupted");}catch{Close();}}
        }
        void End(string outcome)
        {
            var pending=Wallet.Passenger?.Pending;
            if(pending!=null && (id==null || pending.Id==id) && (id==null || profile==Profile.Id))
            {if(!Wallet.PassengerFinish(pending.Id,outcome)){Halt();stopping=true;Notify.Show("Reserva pendente. Libere espaco no saldo bancario e encerre pelo menu.");return;}Notify.Show("Transporte encerrado; tarifa $"+pending.Fare+". Devolucao $"+(pending.Budget-pending.Fare)+" ao banco.");}
            Halt();Close();
        }
        void Close()
        {
            if(blip!=null && blip.Exists())blip.Delete();blip=null;
            if(car!=null || driver!=null)retired.Add(Tuple.Create(car,driver,token));car=null;driver=null;actor=null;id=null;loading=false;gate.Release(Name);
            if(carModel.Hash!=0)carModel.MarkAsNoLongerNeeded();if(pedModel.Hash!=0)pedModel.MarkAsNoLongerNeeded();carModel=default;pedModel=default;
        }
        void ReleaseRetired()
        {
            for(int i=retired.Count-1;i>=0;i--){var r=retired[i];var v=r.Item1;var d=r.Item2;
                if(Own(v,r.Item3) && (v.Occupants.Any(p=>p!=d) || v.Speed>.5f || Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,v) || Function.Call<bool>(Hash.IS_ENTITY_IN_AIR,v)))continue;
                if(Own(v,r.Item3) && Own(d,r.Item3) && d.IsAlive && v.Driver==d)Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER,d,v,10f,786603);
                if(Own(d,r.Item3))d.MarkAsNoLongerNeeded();if(Own(v,r.Item3)){JobVehicles.Release(v);v.MarkAsNoLongerNeeded();}retired.RemoveAt(i);}
        }
        public void OnKey(KeyEventArgs e){if(e.KeyCode==Keys.End && id!=null)End("cancelled");}
        public void Reload(){Close();menu.Visible=false;var p=Wallet.Passenger?.Pending;if(p!=null && Wallet.Passenger.Valid)try{Wallet.PassengerFinish(p.Id,"interrupted");}catch(Exception ex){Log.Error("passenger recovery; escrow retained",ex);}last=Game.GameTime;}
        public void Save(){ }
        public void Dispose(){try{if(id!=null && profile==Profile.Id)End("interrupted");}finally{Close();ReleaseRetired();foreach(var r in retired){if(Own(r.Item2,r.Item3))r.Item2.MarkAsNoLongerNeeded();if(Own(r.Item1,r.Item3)){JobVehicles.Release(r.Item1);r.Item1.MarkAsNoLongerNeeded();}}retired.Clear();menu.Visible=false;}}
    }
}
