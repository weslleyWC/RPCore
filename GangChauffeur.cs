using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
namespace RPCore
{
    internal sealed class GangChauffeur
    {
        const string Owner="gang_chauffeur",Decor="RPCore_ChauffeurOwner";
        sealed class Retired{public Vehicle Car;public Ped Vip;public int Token,Last;}
        readonly List<Retired> retiring=new List<Retired>();readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Func<IEnumerable<GangDefinition>> gangs;readonly Func<IEnumerable<GangTurfDefinition>> areas;readonly GangChauffeurOptions options;readonly Random random=new Random();
        Vehicle car;Ped vip;Model carModel,pedModel;Blip route;GangChauffeurMeter meter;string profile,id;int player,token,last,requested;long elapsed,retry,exitAt=-1,nextSave;bool spawned,collected,boarded,dropping;public bool Active{get;private set;}
        GangChauffeurAttempt Pending=>Wallet.GangChauffeur?.Pending;GangTurfDefinition Current=>areas().FirstOrDefault(a=>a.Id==10);
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        static bool Tagged(Entity e,int t)=>e!=null && e.Exists() && e.Handle!=Game.Player.Character.Handle && Function.Call<bool>(Hash.DECOR_EXIST_ON,e,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,e,Decor)==t;
        bool Mark(Entity e){try{if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);return Function.Call<bool>(Hash.DECOR_SET_INT,e,Decor,token);}catch{return false;}}
        bool Safe=>Game.Player.Character!=null && Game.Player.Character.Exists() && Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && Game.Player.Wanted.WantedLevel==0 && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool Input=>Safe && !Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
        bool Owns=>Pending!=null && GangBusinessTransactions.Owns(state(),Current) && GangCrewRules.SameGang(Pending.Gang,state().Gang) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang));
        bool Near(JobPoint p,float r)=>Safe && !Game.IsPaused && !InterfaceOptions.ModalOpen && Game.Player.Character.IsOnFoot && Game.Player.Character.Position.DistanceTo(Pos(p))<=r;
        bool OwnCar=>Tagged(car,token);bool OwnVip=>Tagged(vip,token);bool Driver=>OwnCar && Game.Player.Character.IsInVehicle(car) && car.Driver==Game.Player.Character;
        bool Foreign(Vehicle v,Ped allowed)=>v!=null && v.Exists() && (Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,v) || Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER,v) || v.Occupants.Any(p=>p!=null && p.Exists() && p.Handle!=player && (allowed==null || p.Handle!=allowed.Handle)));
        public GangChauffeur(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs,Func<IEnumerable<GangTurfDefinition>> areas)
        {this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;this.areas=areas;options=Store.LoadDef("Gangs/Chauffeur.json",()=>new GangChauffeurOptions());if(options?.Valid!=true){options=new GangChauffeurOptions();Log.Write("Invalid chauffeur config; defaults in memory, original retained.");}}
        public void Controls(Action<string,string,Action> add,Action reopen)
        {
            var p=Pending;if(p==null)return;
            add("VIP pendente / "+(p.Ready?"entregue":p.Failed?"reprovado":"em aberto"),"Destino: "+p.Options.Destinations[p.Destination].Name+" / satisfacao "+(int)p.Progress.Satisfaction+"% / carro "+(int)(p.Progress.BodyHealth/10)+"% / restante "+Math.Max(0,(p.Limit-p.Progress.Elapsed)/1000)+"s.",null);
            if(!p.Failed)add(Active?"Suspender corrida VIP":"Retomar corrida VIP",p.Ready?"A pe no destino, retome e E confirma bonus sem repetir viagem.":"Saia do carro anterior e deixe-o parado para liberar. Retome a pe no carro inicial: mesmo VIP/destino/perfil, tempo e nota preservados; embarque e viagem devem ser refeitos.",()=>{if(Active){Suspend();reopen();}else if(!Resume())Notify.Show("Confira local a pe, posse, seguranca, veiculo anterior liberado e atividade/pendencias.");});
            add("GPS: VIP pendente","Inicio ou destino de confirmacao.",()=>{var q=p.Ready?p.Options.Destinations[p.Destination]:p.Options.Spawn;Function.Call(Hash.SET_NEW_WAYPOINT,q.X,q.Y);pool.HideAll();});
            add("Cancelar contrato VIP","Gratis/sem premio parcial. Reprovado exige cancelar antes de iniciar outro; bonus anterior permanece. Nao ejeta jogador nem apaga veiculo ocupado.",()=>{if(!Safe || Game.IsPaused || InterfaceOptions.ModalOpen || gate.Owner!=null && gate.Owner!=Owner){Notify.Show("Confira seguranca e atividade.");return;}Suspend();if(!Wallet.GangChauffeurCancel(p.Id,out _))Notify.Show("Cancelamento nao confirmado; contrato conservado.");reopen();});
        }
        public void Append(GangTurfDefinition area,Action<string,string,Action> add)
        {
            if(area.Id!=10)return;var a=GangChauffeurTransactions.Account(Wallet.GangChauffeur,state().Gang);var utc=DateTime.UtcNow;double mult=GangChauffeurTransactions.Multiplier(Wallet.GangChauffeur,state().Gang,10,utc);
            add("Motorista VIP / bonus: "+(mult>1?mult.ToString("0.##")+"X / "+Math.Ceiling((a.ExpiresUtcTicks-utc.Ticks)/(double)TimeSpan.TicksPerMinute)+"min":"inativo"),"Corridas: "+(a?.Completed??0)+". Satisfacao e estado do carro determinam1,25X/1,5X/2X. Nao paga dinheiro pessoal nem ativa automatico.",null);
            add("GPS: carro de luxo VIP","A pe a ate"+options.StartRadius+"m do carro inicial, com vaga livre.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,options.Spawn.X,options.Spawn.Y);pool.HideAll();});
            add("Iniciar motorista VIP","Retire carro, busque VIP e leve ao destino no prazo. Exigente tolera75km/h, normal105, atrasado135. Evite batidas, aceleracao/freadas bruscas. Aguarde desembarque e E final.",()=>{if(!Begin(area))Notify.Show("Confira posse da area10, inicio a pe, seguranca, atividade/pendencias e carro anterior liberado.");});
        }
        bool Begin(GangTurfDefinition a)
        {if(Active || gate.Owner!=null || retiring.Count!=0 || !Near(options.Spawn,options.StartRadius) || !gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang)))return false;var cars=options.Vehicles.Where(n=>new Model(n).IsInCdImage && new Model(n).IsCar).ToArray();var peds=options.Passengers.Where(n=>new Model(n).IsInCdImage && new Model(n).IsPed).ToArray();if(cars.Length==0 || peds.Length==0 || !gate.Acquire(Owner))return false;try{if(!Wallet.GangChauffeurBegin(state(),a,options,Guid.NewGuid().ToString("N"),cars[random.Next(cars.Length)],peds[random.Next(peds.Length)],random.Next(options.Destinations.Count),random.Next(3),out bool saved) || !saved){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}}
        bool Resume(){if(Active || gate.Owner!=null || retiring.Count!=0 || Wallet.GangChauffeur?.Valid!=true || Pending==null || Pending.Failed || !Owns || !Wallet.GangChauffeurOtherFree || !Near(Pending.Ready?Pending.Options.Destinations[Pending.Destination]:Pending.Options.Spawn,Pending.Ready?20:Pending.Options.StartRadius) || !gate.Acquire(Owner))return false;Activate();return true;}
        void Activate(){Active=true;profile=Profile.Id;id=Pending.Id;player=Game.Player.Character.Handle;token=random.Next(1,int.MaxValue);last=requested=Game.GameTime;elapsed=retry=0;exitAt=-1;spawned=collected=boarded=dropping=false;meter=null;pool.HideAll();if(!Pending.Ready){carModel=new Model(Pending.Vehicle);pedModel=new Model(Pending.Passenger);if(!carModel.IsInCdImage || !carModel.IsCar || !pedModel.IsInCdImage || !pedModel.IsPed){Suspend();Notify.Show("Modelos salvos indisponiveis; contrato conservado.");return;}carModel.Request();pedModel.Request();}Route(Pending.Ready?Pos(Pending.Options.Destinations[Pending.Destination]):Pos(Pending.Options.Spawn));Notify.Show(Pending.Ready?"VIP entregue salvo. E no destino confirma bonus.":"Corrida VIP: libere vaga e afaste-se3m para o carro aparecer. END suspende; tempo/nota nao reiniciam.");}
        void Route(Vector3 point){if(route!=null && route.Exists())route.Delete();route=MapBlips.Create(point,"RPCore: motorista VIP",BlipSprite.Standard,BlipColor.Green,.75f);route.ShowRoute=true;}
        bool Free(Vector3 p,float r)=>World.GetNearbyVehicles(p,r).Length==0 && World.GetNearbyPeds(p,1.5f).Length==0;
        void Prepare()
        {
            if(!carModel.IsLoaded || !pedModel.IsLoaded){if(unchecked((uint)(Game.GameTime-requested))>10000){Suspend();Notify.Show("Modelos nao carregaram; contrato conservado.");}return;}
            if(elapsed<retry)return;retry=elapsed+2000;var c=Pending.Options;
            if(Game.Player.Character.Position.DistanceTo(Pos(c.Spawn))<3 || !Free(Pos(c.Spawn),4) || !Free(Pos(c.Pickup),1.5f)){Notify.Show("Deixe carro e ponto do VIP livres; afaste-se3m da vaga. Nao remove outros veiculos/pedestres.");return;}
            car=World.CreateVehicle(carModel,Pos(c.Spawn),c.Spawn.Heading);if(car==null || !car.Exists()){car=null;return;}if(!Mark(car)){car.MarkAsNoLongerNeeded();car=null;Suspend();return;}car.IsPersistent=true;JobVehicles.Reserve(car);car.BodyHealth=Pending.Progress.BodyHealth;
            vip=World.CreatePed(pedModel,Pos(c.Pickup),c.Pickup.Heading);if(vip==null || !vip.Exists()){vip=null;Suspend();return;}if(!Mark(vip)){vip.MarkAsNoLongerNeeded();vip=null;Suspend();return;}vip.IsPersistent=true;vip.BlockPermanentEvents=true;vip.Weapons.RemoveAll();Function.Call(Hash.SET_PED_MONEY,vip,0);vip.RelationshipGroup=new RelationshipGroup((int)RelationshipGroupHash.CivilianMale);
            ReleaseModels();spawned=true;meter=new GangChauffeurMeter(Game.GameTime,Pending.Personality,Pending.Limit,Pending.Progress);nextSave=elapsed+1000;retry=elapsed+1000;Notify.Show("VIP "+(Pending.Personality==1?"EXIGENTE":Pending.Personality==2?"ATRASADO":"NORMAL")+" / destino: "+Pending.Options.Destinations[Pending.Destination].Name+". Entre no carro marcado.");
        }
        void ReleaseModels(){if(carModel!=null)carModel.MarkAsNoLongerNeeded();if(pedModel!=null)pedModel.MarkAsNoLongerNeeded();carModel=pedModel=null;}
        bool Flush()
        {
            if(meter==null || profile!=Profile.Id || Pending?.Id!=id || Pending.Delivered || Wallet.GangChauffeur?.Valid!=true)return true;
            var a=meter.Progress;var b=Pending.Progress;if(a.Elapsed==b.Elapsed && a.Satisfaction==b.Satisfaction && a.BodyHealth==b.BodyHealth)return true;
            return Wallet.GangChauffeurUpdate(state(),Current,id,a,out _);
        }
        public void Tick()
        {
            Drain();if(!Active)return;
            if(Wallet.GangChauffeur?.Valid!=true || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Safe || !Owns || !Wallet.GangChauffeurOtherFree){Suspend();Notify.Show("Corrida VIP suspensa. Progresso salvo; F4 retoma ou cancela.");return;}
            uint d=unchecked((uint)(Game.GameTime-last));last=Game.GameTime;if(!Input){meter?.Tick(Game.GameTime,false,OwnCar?car.BodyHealth:1000,OwnCar?car.Speed:0,0,boarded,dropping);return;}if(d<=2000)elapsed+=d;
            var p=Pending;var destination=Pos(p.Options.Destinations[p.Destination]);
            if(p.Ready){GTA.UI.Screen.ShowHelpTextThisFrame("E no destino: confirmar servico VIP e bonus");return;}
            if(!spawned){Prepare();return;}
            if(!OwnCar || !OwnVip || car.IsDead || !car.IsDriveable || !vip.IsAlive){Fail("Carro ou VIP indisponivel.");return;}
            if(Foreign(car,vip) || Game.Player.Character.Position.DistanceTo(car.Position)>150){Suspend();Notify.Show("Carro ocupado/conectado ou afastado; contrato salvo.");return;}
            meter.Tick(Game.GameTime,true,car.BodyHealth,car.Speed,car.Position.DistanceTo(destination),boarded,dropping);
            if(elapsed>=nextSave){if(!Flush()){Suspend();Notify.Show("Avaliacao nao gravada; corrida suspensa.");return;}nextSave=elapsed+1000;}
            var score=meter.Progress;
            GTA.UI.Screen.ShowSubtitle("VIP / "+(Pending.Personality==1?"exigente":Pending.Personality==2?"atrasado":"normal")+" / "+Math.Max(0,(p.Limit-score.Elapsed)/1000)+"s / satisfacao "+(int)score.Satisfaction+"% / carro "+(int)(score.BodyHealth/10)+"% / END suspende",100);
            if(score.Elapsed>=p.Limit || score.Satisfaction<=15 || score.BodyHealth<=450){Flush();Suspend();Notify.Show("VIP reprovado por prazo, satisfacao ou danos. Cancele a pendencia no F4 para nova corrida; sem bonus.");return;}
            if(!collected){if(Driver){collected=true;Route(vip.Position);}else GTA.UI.Screen.ShowHelpTextThisFrame("Entre como motorista no carro de luxo marcado.");return;}
            if(!boarded)
            {
                if(route!=null && route.Exists())route.Position=vip.Position;
                if(Driver && car.Position.DistanceTo(vip.Position)<=12 && car.Speed<1.5f && elapsed>=retry){vip.Task.EnterVehicle(car,VehicleSeat.Passenger,15000,1f,EnterVehicleFlags.None);retry=elapsed+5000;}
                if(vip.IsInVehicle(car) && vip.SeatIndex==VehicleSeat.Passenger){if(!Wallet.GangChauffeurBoard(state(),Current,id,out _)){Suspend();return;}boarded=true;Route(destination);}
                GTA.UI.Screen.ShowHelpTextThisFrame("Pare a ate12m do VIP e aguarde embarque pela porta do passageiro.");return;
            }
            if(!dropping)
            {
                if(!vip.IsInVehicle(car)){Fail("VIP saiu antes da entrega.");return;}
                World.DrawMarker(MarkerType.Cylinder,destination,Vector3.Zero,Vector3.Zero,new Vector3(12,12,.7f),Color.FromArgb(125,91,219,135));
                if(Driver && car.Position.DistanceTo(destination)<=20 && car.Speed<1){dropping=true;retry=elapsed+4000;Function.Call(Hash.TASK_LEAVE_VEHICLE,vip,car,0);Notify.Show("Aguarde o VIP desembarcar. Mantenha o carro parado.");}return;
            }
            if(car.Position.DistanceTo(destination)>22){Fail("Voce se afastou antes do desembarque.");return;}
            if(vip.IsInVehicle()) {if(elapsed>=retry){Function.Call(Hash.TASK_LEAVE_VEHICLE,vip,car,0);retry=elapsed+4000;}return;}
            if(exitAt<0){exitAt=elapsed;vip.Task.Wander();}
            if(elapsed-exitAt<5000)return;
            if(!Flush() || !Wallet.GangChauffeurDeliver(state(),Current,id,out bool saved) || !saved){Suspend();return;}meter=null;Notify.Show("VIP entregue e avaliacao salva. E no destino confirma bonus de"+GangChauffeurRules.Reward(Pending.Progress.Satisfaction,Pending.Progress.BodyHealth).ToString("0.##")+"X.");
        }
        public void Interact()
        {if(!Active || !Input || !Owns || !Wallet.GangChauffeurOtherFree || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Pending.Ready || Game.Player.Character.Position.DistanceTo(Pos(Pending.Options.Destinations[Pending.Destination]))>20 || !(Game.Player.Character.IsOnFoot || Driver && car.Speed<1))return;double mult=GangChauffeurRules.Reward(Pending.Progress.Satisfaction,Pending.Progress.BodyHealth);int mins=Pending.Options.BoostMinutes;if(Wallet.GangChauffeurComplete(state(),Current,id,DateTime.UtcNow,out bool saved)){if(saved && OwnVip && vip.IsOnFoot){vip.BlockPermanentEvents=false;vip.MarkAsNoLongerNeeded();vip=null;}Suspend();Notify.Show(saved?"VIP concluido! Processamento"+mult.ToString("0.##")+"X na area10 por"+mins+"min reais. Ative separadamente.":"Contrato ja confirmado.");}}
        void Drain()
        {
            for(int i=retiring.Count-1;i>=0;i--)
            {var r=retiring[i];bool ownCar=Tagged(r.Car,r.Token),ownVip=Tagged(r.Vip,r.Token);
                if(ownVip && r.Vip.IsInVehicle()){if(ownCar && r.Vip.IsInVehicle(r.Car) && r.Car.Speed<.5f && unchecked((uint)(Game.GameTime-r.Last))>=4000){Function.Call(Hash.TASK_LEAVE_VEHICLE,r.Vip,r.Car,0);r.Last=Game.GameTime;}continue;}
                if(ownVip){r.Vip.Delete();r.Vip=null;}
                if(ownCar && (r.Car.Occupants.Length!=0 || r.Car.Speed>=.5f || Foreign(r.Car,null)))continue;
                JobVehicles.Release(r.Car);if(ownCar)r.Car.Delete();retiring.RemoveAt(i);
            }
        }
        void Fail(string reason){if(meter!=null){meter.Progress.Satisfaction=0;Flush();}Suspend();Notify.Show("VIP reprovado: "+reason+" Cancele a pendencia no F4 para nova corrida; sem bonus.");}
        public void Suspend()
        {
            try{if(Active)Flush();}finally{meter=null;if(car!=null || vip!=null)retiring.Add(new Retired{Car=car,Vip=vip,Token=token,Last=Game.GameTime});car=null;vip=null;ReleaseModels();if(route!=null && route.Exists())route.Delete();route=null;Active=false;profile=id=null;gate.Release(Owner);Drain();}
        }
        public void Dispose(){Suspend();foreach(var r in retiring){if(Tagged(r.Car,r.Token))r.Car.MarkAsNoLongerNeeded();if(Tagged(r.Vip,r.Token))r.Vip.MarkAsNoLongerNeeded();JobVehicles.Release(r.Car);}retiring.Clear();}
    }
}
