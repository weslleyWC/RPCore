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
    internal sealed class GangDock
    {
        const string Owner="gang_dock",Decor="RPCore_DockOwner";
        sealed class Retired{public Vehicle Truck,Trailer;public int Token;}
        readonly List<Retired> retiring=new List<Retired>();readonly Dictionary<int,Prop> cargo=new Dictionary<int,Prop>();readonly Dictionary<string,Model> models=new Dictionary<string,Model>(StringComparer.OrdinalIgnoreCase);
        readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Func<IEnumerable<GangDefinition>> gangs;readonly Func<IEnumerable<GangTurfDefinition>> areas;readonly GangDockOptions options;readonly Random random=new Random();
        Vehicle truck,trailer;Blip route;GangDockClock clock;GangDockTransfer transfer;string profile,id;int player,token,last,requested,routeStage;long elapsed,retry,nextSave;bool spawned,render;public bool Active{get;private set;}
        GangDockAttempt Pending=>Wallet.GangDock?.Pending;GangTurfDefinition Current=>areas().FirstOrDefault(a=>a.Id==11);
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        static bool Tagged(Entity e,int t)=>e!=null && e.Exists() && e.Handle!=Game.Player.Character.Handle && Function.Call<bool>(Hash.DECOR_EXIST_ON,e,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,e,Decor)==t;
        bool Mark(Entity e){try{if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);return Function.Call<bool>(Hash.DECOR_SET_INT,e,Decor,token);}catch{return false;}}
        bool Safe=>Game.Player.Character!=null && Game.Player.Character.Exists() && Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && Game.Player.Wanted.WantedLevel==0 && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool Input=>Safe && !Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
        bool Owns=>Pending!=null && GangBusinessTransactions.Owns(state(),Current) && GangCrewRules.SameGang(Pending.Gang,state().Gang) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang));
        bool Near(JobPoint p,float r)=>Safe && !Game.IsPaused && !InterfaceOptions.ModalOpen && Game.Player.Character.IsOnFoot && Game.Player.Character.Position.DistanceTo(Pos(p))<=r;
        bool OwnTruck=>Tagged(truck,token);bool OwnTrailer=>Tagged(trailer,token);bool Driver=>OwnTruck && Game.Player.Character.IsInVehicle(truck) && truck.Driver==Game.Player.Character;
        bool Coupled=>OwnTruck && OwnTrailer && JobVehicles.Towing(truck,trailer);
        static bool Foreign(Vehicle t,Vehicle r)
        {bool a=t!=null && t.Exists(),b=r!=null && r.Exists();return a && (Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,t) || Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER,t) && !JobVehicles.Towing(t,r)) || b && (JobVehicles.TrailerHasOtherTractor(r,t) || Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,r) && !JobVehicles.Towing(t,r));}
        public GangDock(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs,Func<IEnumerable<GangTurfDefinition>> areas)
        {this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;this.areas=areas;options=Store.LoadDef("Gangs/DockCargo.json",()=>new GangDockOptions());if(options?.Valid!=true){options=new GangDockOptions();Log.Write("Invalid dock config; defaults in memory, original retained.");}}
        public void Controls(Action<string,string,Action> add,Action reopen)
        {
            var p=Pending;if(p==null)return;
            add("Frete do porto / "+(p.Ready?"entregue":p.Failed?"reprovado":"pendente"),"Carregados "+p.Loaded+" / descarregados "+p.Unloaded+" de"+p.Options.Slots.Count+". Condicao "+(int)GangDockRules.Condition(p.Progress.BodyHealth)+"% / restante "+Math.Max(0,p.Options.SecondsLimit-p.Progress.Elapsed/1000)+"s.",null);
            if(!p.Failed)add(Active?"Suspender frete do porto":"Retomar frete do porto",p.Ready?"A pe no destino, retome e E confirma bonus; nenhuma viagem repetida.":"Saia e pare o conjunto anterior para liberar. Retome a pe no inicio: reconstroi carga restante, conserva contagens/tempo/danos; refaz engate e deslocamento.",()=>{if(Active){Suspend();reopen();}else if(!Resume())Notify.Show("Confira local a pe, posse, seguranca, conjunto anterior liberado e atividade/pendencias.");});
            add("GPS: frete do porto pendente","Inicio ou destino final salvo.",()=>{var q=p.Ready?p.Options.Destination:p.Options.Truck;Function.Call(Hash.SET_NEW_WAYPOINT,q.X,q.Y);pool.HideAll();});
            add("Cancelar frete do porto","Gratis/sem premio parcial, conserva bonus anterior. Reprovado exige cancelar para novo contrato. Conjunto ocupado/movendo/conectado a terceiros e preservado.",()=>{if(!Safe || Game.IsPaused || InterfaceOptions.ModalOpen || gate.Owner!=null && gate.Owner!=Owner){Notify.Show("Confira seguranca e atividade.");return;}Suspend();if(!Wallet.GangDockCancel(p.Id,out _))Notify.Show("Cancelamento nao confirmado; contrato conservado.");reopen();});
        }
        public void Append(GangTurfDefinition area,Action<string,string,Action> add)
        {
            if(area.Id!=11)return;var a=GangDockTransactions.Account(Wallet.GangDock,state().Gang);var utc=DateTime.UtcNow;double mult=GangDockTransactions.Multiplier(Wallet.GangDock,state().Gang,11,utc);
            add("Porto / bonus: "+(mult>1?mult.ToString("0.##")+"X / "+Math.Ceiling((a.ExpiresUtcTicks-utc.Ticks)/(double)TimeSpan.TicksPerMinute)+"min":"inativo"),"Fretes concluidos: "+(a?.Completed??0)+". Condicao da carreta determina1,25X/1,5X/2X; nao paga dinheiro pessoal nem liga automatico.",null);
            add("GPS: caminhao do porto","A pe a ate"+options.StartRadius+"m para iniciar, com vagas livres.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,options.Truck.X,options.Truck.Y);pool.HideAll();});
            add("Transportar carga / "+options.Slots.Count+" paletes","Retire Phantom, engate carreta e pare na baia. Carrega automaticamente3s/palete, entrega e descarrega2s/palete. Prazo "+options.SecondsLimit+"s; E final confirma bonus. END suspende.",()=>{if(!Begin(area))Notify.Show("Confira posse da area11, inicio a pe, seguranca, conjunto anterior liberado e atividade/pendencias.");});
        }
        bool Begin(GangTurfDefinition a)
        {if(Active || gate.Owner!=null || retiring.Count!=0 || !Near(options.Truck,options.StartRadius) || !gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang)) || !gate.Acquire(Owner))return false;try{if(!Wallet.GangDockBegin(state(),a,options,Guid.NewGuid().ToString("N"),out bool saved) || !saved){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}}
        bool Resume(){if(Active || gate.Owner!=null || retiring.Count!=0 || Wallet.GangDock?.Valid!=true || Pending==null || Pending.Failed || !Owns || !Wallet.GangDockOtherFree || !Near(Pending.Ready?Pending.Options.Destination:Pending.Options.Truck,Pending.Ready?9:Pending.Options.StartRadius) || !gate.Acquire(Owner))return false;Activate();return true;}
        void Activate(){Active=true;profile=Profile.Id;id=Pending.Id;player=Game.Player.Character.Handle;token=random.Next(1,int.MaxValue);last=requested=Game.GameTime;elapsed=retry=0;spawned=false;render=Visuals.Enabled && Visuals.UseProps;clock=null;transfer=new GangDockTransfer(Game.GameTime);routeStage=-1;pool.HideAll();if(!Pending.Ready){var c=Pending.Options;foreach(string name in new[]{c.TruckModel,c.TrailerModel}.Concat(render?new[]{c.CargoModelA,c.CargoModelB}:Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase)){var m=new Model(name);models[name]=m;if(!m.IsInCdImage || (name==c.TruckModel || name==c.TrailerModel) && !m.IsVehicle){Suspend();Notify.Show("Modelo salvo indisponivel; contrato conservado.");return;}m.Request();}}Route(Pending.Ready?Pos(Pending.Options.Destination):Pos(Pending.Options.Truck));Notify.Show(Pending.Ready?"Descarga salva. E no destino confirma bonus.":"Frete ativo. Afaste-se4m e deixe as vagas livres. END suspende; contagens/tempo/danos preservados.");}
        void Route(Vector3 pos){if(route!=null && route.Exists())route.Delete();route=MapBlips.Create(pos,"RPCore: frete do porto",BlipSprite.Standard,BlipColor.Green,.85f);route.ShowRoute=true;}
        bool Free(Vector3 p,float radius)=>World.GetNearbyVehicles(p,radius).Length==0 && World.GetNearbyPeds(p,3).Length==0;
        void Prepare()
        {
            if(!models.Values.All(m=>m.IsLoaded)){if(unchecked((uint)(Game.GameTime-requested))>10000){Suspend();Notify.Show("Modelos nao carregaram; contrato conservado.");}return;}
            if(elapsed<retry)return;retry=elapsed+3000;var c=Pending.Options;if(Game.Player.Character.Position.DistanceTo(Pos(c.Truck))<4 || !Free(Pos(c.Truck),7) || !Free(Pos(c.Trailer),9)){Notify.Show("Vagas do caminhao/carreta ocupadas ou voce muito perto. Afaste-se4m; aguardando sem apagar outros veiculos.");return;}
            truck=World.CreateVehicle(models[c.TruckModel],Pos(c.Truck),c.Truck.Heading);if(truck==null || !truck.Exists()){truck=null;return;}if(!Mark(truck)){truck.MarkAsNoLongerNeeded();truck=null;Suspend();return;}truck.IsPersistent=true;JobVehicles.Reserve(truck);
            trailer=World.CreateVehicle(models[c.TrailerModel],Pos(c.Trailer),c.Trailer.Heading);if(trailer==null || !trailer.Exists()){trailer=null;Suspend();return;}if(!Mark(trailer)){trailer.MarkAsNoLongerNeeded();trailer=null;Suspend();return;}trailer.IsPersistent=true;JobVehicles.Reserve(trailer);trailer.BodyHealth=Pending.Progress.BodyHealth;
            for(int i=0;i<Pending.Loaded-Pending.Unloaded;i++)if(!Cargo(i)){Suspend();Notify.Show("Carga salva nao carregou; contrato conservado.");return;}
            spawned=true;clock=new GangDockClock(Game.GameTime,c.SecondsLimit*1000,Pending.Progress);nextSave=elapsed+1000;transfer=new GangDockTransfer(Game.GameTime);Notify.Show("Entre no caminhao marcado. Engate a carreta correta dando re; E ajuda quando alinhado e parado.");
        }
        bool Cargo(int index)
        {
            if(!render)return true;if(cargo.TryGetValue(index,out var existing))return Tagged(existing,token);
            var c=Pending.Options;var offset=Pos(c.Slots[index]);var m=models[index%2==0?c.CargoModelA:c.CargoModelB];var prop=World.CreateProp(m,trailer.GetOffsetPosition(offset),false,false);if(prop==null || !prop.Exists())return false;if(!Mark(prop)){prop.MarkAsNoLongerNeeded();return false;}prop.IsPersistent=true;prop.IsCollisionEnabled=false;prop.AttachTo(trailer,offset,Vector3.Zero);cargo[index]=prop;return true;
        }
        bool Flush(){if(clock==null || profile!=Profile.Id || Pending?.Id!=id || Pending.Ready || Wallet.GangDock?.Valid!=true)return true;var a=clock.Progress;var b=Pending.Progress;if(a.Elapsed==b.Elapsed && a.BodyHealth==b.BodyHealth && a.Faulted==b.Faulted)return true;return Wallet.GangDockUpdate(state(),Current,id,a,out _);}
        public void Tick()
        {
            Drain();if(!Active)return;
            if(Wallet.GangDock?.Valid!=true || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Safe || !Owns || !Wallet.GangDockOtherFree){Suspend();Notify.Show("Frete suspenso. Progresso salvo; F4 retoma ou cancela.");return;}
            uint d=unchecked((uint)(Game.GameTime-last));last=Game.GameTime;if(!Input){clock?.Tick(Game.GameTime,false,OwnTrailer?trailer.BodyHealth:1000);transfer?.Step(Game.GameTime,false,false);return;}if(d<=2000)elapsed+=d;var p=Pending;var c=p.Options;
            if(p.Ready){GTA.UI.Screen.ShowHelpTextThisFrame("E no destino: confirmar descarga e bonus");return;}
            if(!spawned){Prepare();return;}
            if(!OwnTruck || !OwnTrailer || truck.IsDead || !truck.IsDriveable || trailer.IsDead){Fail("Equipamento indisponivel.");return;}
            if(Foreign(truck,trailer) || truck.Occupants.Any(x=>x.Handle!=player) || trailer.Occupants.Length!=0 || Game.Player.Character.Position.DistanceTo(truck.Position)>150){Suspend();Notify.Show("Conjunto conectado/ocupado por terceiros ou afastado; progresso conservado.");return;}
            clock.Tick(Game.GameTime,true,trailer.BodyHealth);if(elapsed>=nextSave){if(!Flush()){Suspend();return;}nextSave=elapsed+1000;}
            if(clock.Progress.Elapsed>=c.SecondsLimit*1000 || GangDockRules.Condition(clock.Progress.BodyHealth)<50){Fail("Prazo esgotado ou carreta abaixo de50%.");return;}
            if(render)foreach(var prop in cargo.Values)if(!Tagged(prop,token) || !Function.Call<bool>(Hash.IS_ENTITY_ATTACHED_TO_ENTITY,prop,trailer)){Suspend();Notify.Show("Palete proprio perdido; progresso salvo para retomar.");return;}
            GTA.UI.Screen.ShowSubtitle("Porto / carga "+p.Loaded+"/"+c.Slots.Count+" / descarga "+p.Unloaded+"/"+c.Slots.Count+" / "+Math.Max(0,c.SecondsLimit-clock.Progress.Elapsed/1000)+"s / condicao "+(int)GangDockRules.Condition(clock.Progress.BodyHealth)+"% / END suspende",100);
            int stage=!Driver?0:!Coupled?1:p.Loaded<c.Slots.Count?2:3;
            if(stage!=routeStage){routeStage=stage;Route(stage==0?truck.Position:stage==1?trailer.Position:Pos(stage==2?c.Bay:c.Destination));transfer=new GangDockTransfer(Game.GameTime);}
            if(stage<=1){if(route!=null && route.Exists())route.Position=stage==0?truck.Position:trailer.Position;transfer.Step(Game.GameTime,false,false);GTA.UI.Screen.ShowHelpTextThisFrame(stage==0?"Entre como motorista no caminhao marcado.":"De re, alinhe o caminhao com a carreta correta. E engata quando perto, alinhado e parado.");return;}
            bool unloading=stage==3;var point=Pos(unloading?c.Destination:c.Bay);float radius=unloading?9:5;
            World.DrawMarker(MarkerType.Cylinder,point,Vector3.Zero,Vector3.Zero,new Vector3(radius*2,radius*2,.4f),Color.FromArgb(130,91,219,135));
            bool valid=GangDockRules.Station(true,Driver,Coupled,trailer.Position.DistanceTo(point),truck.Speed,trailer.Speed,unloading);
            GTA.UI.Screen.ShowHelpTextThisFrame(valid?(unloading?"Descarregando / mantenha o conjunto parado (2s/palete).":"Carregando / mantenha o conjunto parado (3s/palete)."):(unloading?"Estacione a CARRETA a ate9m da entrega, com caminhao engatado e parado.":"Estacione a CARRETA a ate5m da baia, com caminhao engatado e parado."));
            if(!transfer.Step(Game.GameTime,valid,unloading))return;
            if(!Flush()){Suspend();return;}
            if(unloading)
            {
                int slot=p.Loaded-p.Unloaded-1;if(!Wallet.GangDockUnload(state(),Current,id,p.Unloaded,out bool saved) || !saved){Suspend();return;}
                if(cargo.TryGetValue(slot,out var prop)){if(Tagged(prop,token))prop.Delete();cargo.Remove(slot);}
                if(Pending.Ready){clock=null;if(route!=null && route.Exists())route.ShowRoute=false;Notify.Show(c.Slots.Count+" paletes descarregados e salvos! E aqui confirma bonus de"+GangDockRules.Reward(Pending.Progress.BodyHealth).ToString("0.##")+"X.");}
            }
            else{if(!Cargo(p.Loaded) || !Wallet.GangDockLoad(state(),Current,id,p.Loaded,out bool saved) || !saved){Suspend();Notify.Show("Carga nao confirmada; contagem anterior conservada.");return;}if(Pending.Loaded==c.Slots.Count)Notify.Show("Carga completa. Siga GPS e descarregue mantendo a carreta parada no destino.");}
            transfer=new GangDockTransfer(Game.GameTime);
        }
        public void Interact()
        {
            if(!Active || !Input || !Owns || !Wallet.GangDockOtherFree || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner)return;
            if(Pending.Ready){if(!(Near(Pending.Options.Destination,9) || Driver && Coupled && trailer.Position.DistanceTo(Pos(Pending.Options.Destination))<=9 && truck.Speed<.5f && trailer.Speed<.5f))return;double bonus=GangDockRules.Reward(Pending.Progress.BodyHealth);int mins=Pending.Options.BoostMinutes;if(Wallet.GangDockComplete(state(),Current,id,DateTime.UtcNow,out bool saved)){Suspend();Notify.Show(saved?"Frete concluido! Processamento"+bonus.ToString("0.##")+"X na area11 por"+mins+"min reais. Ative separadamente.":"Contrato ja confirmado.");}return;}
            if(!Driver || !OwnTrailer || Coupled || Foreign(truck,trailer))return;
            float delta=Math.Abs(truck.Heading-trailer.Heading)%360;delta=Math.Min(delta,360-delta);
            if(TankerRules.CanCouple(true,Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER,truck),truck.RearPosition.DistanceTo(trailer.FrontPosition),delta,truck.Speed,trailer.Speed))Function.Call(Hash.ATTACH_VEHICLE_TO_TRAILER,truck,trailer,6f);
        }
        void Fail(string reason){if(clock!=null){clock.Progress.Faulted=true;Flush();}Suspend();Notify.Show("Frete reprovado: "+reason+" Cancele pendencia no F4 para novo contrato; sem premio.");}
        void Drain()
        {
            for(int i=retiring.Count-1;i>=0;i--)
            {var r=retiring[i];bool a=Tagged(r.Truck,r.Token),b=Tagged(r.Trailer,r.Token);
                if(!a && !b){JobVehicles.Release(r.Truck);JobVehicles.Release(r.Trailer);retiring.RemoveAt(i);continue;}
                if(a && !b && Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER,r.Truck) || b && !a && JobVehicles.TrailerHasOtherTractor(r.Trailer,null))continue;
                if(a && (r.Truck.Occupants.Length!=0 || r.Truck.Speed>=.5f) || b && (r.Trailer.Occupants.Length!=0 || r.Trailer.Speed>=.5f) || Foreign(r.Truck,r.Trailer))continue;
                if(a && b && JobVehicles.Towing(r.Truck,r.Trailer))Function.Call(Hash.DETACH_VEHICLE_FROM_TRAILER,r.Truck);
                JobVehicles.Release(r.Truck);JobVehicles.Release(r.Trailer);if(b)r.Trailer.Delete();if(a)r.Truck.Delete();retiring.RemoveAt(i);
            }
        }
        public void Suspend()
        {
            try{if(Active)Flush();}finally{clock=null;transfer=null;foreach(var prop in cargo.Values)if(Tagged(prop,token))prop.Delete();cargo.Clear();if(truck!=null || trailer!=null)retiring.Add(new Retired{Truck=truck,Trailer=trailer,Token=token});truck=trailer=null;foreach(var m in models.Values)m.MarkAsNoLongerNeeded();models.Clear();if(route!=null && route.Exists())route.Delete();route=null;Active=false;profile=id=null;gate.Release(Owner);Drain();}
        }
        public void Dispose(){Suspend();foreach(var r in retiring){if(Tagged(r.Truck,r.Token))r.Truck.MarkAsNoLongerNeeded();if(Tagged(r.Trailer,r.Token))r.Trailer.MarkAsNoLongerNeeded();JobVehicles.Release(r.Truck);JobVehicles.Release(r.Trailer);}retiring.Clear();}
    }
}
