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
    internal sealed class GangBmx
    {
        const string Owner="gang_bmx",Decor="RPCore_BmxOwner";
        sealed class Retired{public Vehicle Bike;public int Token;}
        readonly List<Retired> retiring=new List<Retired>();readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Func<IEnumerable<GangDefinition>> gangs;readonly Func<IEnumerable<GangTurfDefinition>> areas;readonly GangBmxOptions options;readonly Random random=new Random();
        Vehicle bike;Model model;Blip route;GangBmxClock clock;string profile,id;int player,token,last,requested,routeIndex=-2;long elapsed,retry,nextSave;bool spawned;public bool Active{get;private set;}
        GangBmxAttempt Pending=>Wallet.GangBmx?.Pending;GangTurfDefinition Current=>areas().FirstOrDefault(a=>a.Id==12);
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        static JobPoint Point(Vector3 p)=>new JobPoint{Name="Ciclista",X=p.X,Y=p.Y,Z=p.Z};
        static bool Tagged(Entity e,int t)=>e!=null && e.Exists() && e.Handle!=Game.Player.Character.Handle && Function.Call<bool>(Hash.DECOR_EXIST_ON,e,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,e,Decor)==t;
        bool Mark(Entity e){try{if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);return Function.Call<bool>(Hash.DECOR_SET_INT,e,Decor,token);}catch{return false;}}
        bool Safe=>Game.Player.Character!=null && Game.Player.Character.Exists() && Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && Game.Player.Wanted.WantedLevel==0 && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool Input=>Safe && !Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
        bool Owns=>Pending!=null && GangBusinessTransactions.Owns(state(),Current) && GangCrewRules.SameGang(Pending.Gang,state().Gang) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang));
        bool Near(JobPoint p,float r)=>Safe && !Game.IsPaused && !InterfaceOptions.ModalOpen && Game.Player.Character.IsOnFoot && Game.Player.Character.Position.DistanceTo(Pos(p))<=r;
        bool OwnBike=>Tagged(bike,token);bool Riding=>OwnBike && Game.Player.Character.IsInVehicle(bike) && bike.Driver==Game.Player.Character;
        public GangBmx(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs,Func<IEnumerable<GangTurfDefinition>> areas)
        {this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;this.areas=areas;options=Store.LoadDef("Gangs/Bmx.json",()=>new GangBmxOptions());if(options?.Valid!=true){options=new GangBmxOptions();Log.Write("Invalid BMX config; defaults in memory, original retained.");}}
        public void Controls(Action<string,string,Action> add,Action reopen)
        {
            var p=Pending;if(p==null)return;
            add("Circuito BMX / "+(p.Ready?"concluido":p.Failed?"reprovado":"pendente"),"Checkpoints "+p.Cleared+"/"+p.Options.Checkpoints.Count+" / restante "+Math.Max(0,p.Options.SecondsLimit-p.Progress.Elapsed/1000)+"s. Tempo e etapas confirmadas salvos.",null);
            if(!p.Failed)add(Active?"Suspender circuito BMX":"Retomar circuito BMX",p.Ready?"A pe no ultimo checkpoint, retome e E confirma bonus sem repetir percurso.":"Saia da bicicleta anterior e deixe-a parada para liberar. Retome a pe no inicio; reconstroi bicicleta propria, conserva checkpoints e tempo restante.",()=>{if(Active){Suspend();reopen();}else if(!Resume())Notify.Show("Confira inicio/ultimo ponto a pe, posse, seguranca, bicicleta anterior liberada e atividade/pendencias.");});
            add("GPS: BMX pendente","Inicio ou confirmacao final salva.",()=>{var q=p.Ready?p.Options.Checkpoints.Last():p.Options.Spawn;Function.Call(Hash.SET_NEW_WAYPOINT,q.X,q.Y);pool.HideAll();});
            add("Cancelar circuito BMX","Gratis/sem premio parcial; pode sem posse/filiacao em seguranca. Reprovado exige cancelar para novo contrato; bonus anterior preservado.",()=>{if(!Safe || Game.IsPaused || InterfaceOptions.ModalOpen || gate.Owner!=null && gate.Owner!=Owner){Notify.Show("Confira seguranca e atividade.");return;}Suspend();if(!Wallet.GangBmxCancel(p.Id,out _))Notify.Show("Cancelamento nao confirmado; contrato conservado.");reopen();});
        }
        public void Append(GangTurfDefinition area,Action<string,string,Action> add)
        {
            if(area.Id!=12)return;var a=GangBmxTransactions.Account(Wallet.GangBmx,state().Gang);var utc=DateTime.UtcNow;int mult=GangBmxTransactions.Multiplier(Wallet.GangBmx,state().Gang,12,utc);
            add("BMX / bonus2X: "+(mult==2?"ATIVO / "+Math.Ceiling((a.ExpiresUtcTicks-utc.Ticks)/(double)TimeSpan.TicksPerMinute)+"min":"inativo"),"Circuitos concluidos: "+(a?.Completed??0)+". Acelera processamento na area12, sem dinheiro pessoal ou automatico ligado sozinho.",null);
            add("GPS: BMX skatepark","A pe a ate"+options.StartRadius+"m para iniciar, deixe ponto da bicicleta livre.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,options.Spawn.X,options.Spawn.Y);pool.HideAll();});
            add("Circuito BMX / "+options.Checkpoints.Count+" pontos","Use a bicicleta fornecida, passe em ordem pelos aneis antes de"+options.SecondsLimit+"s ativos. E final libera2X/"+options.BoostMinutes+"min. END suspende sem reiniciar tempo.",()=>{if(!Begin(area))Notify.Show("Confira posse da area12, inicio a pe, seguranca, bicicleta anterior liberada e atividade/pendencias.");});
        }
        bool Begin(GangTurfDefinition area)
        {if(Active || gate.Owner!=null || retiring.Count!=0 || !Near(options.Spawn,options.StartRadius) || !gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang)) || !gate.Acquire(Owner))return false;try{if(!Wallet.GangBmxBegin(state(),area,options,Guid.NewGuid().ToString("N"),out bool saved) || !saved){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}}
        bool Resume(){if(Active || gate.Owner!=null || retiring.Count!=0 || Wallet.GangBmx?.Valid!=true || Pending==null || Pending.Failed || !Owns || !Wallet.GangBmxOtherFree || !Near(Pending.Ready?Pending.Options.Checkpoints.Last():Pending.Options.Spawn,Pending.Ready?Pending.Options.Radius:Pending.Options.StartRadius) || !gate.Acquire(Owner))return false;Activate();return true;}
        void Activate(){Active=true;profile=Profile.Id;id=Pending.Id;player=Game.Player.Character.Handle;token=random.Next(1,int.MaxValue);last=requested=Game.GameTime;elapsed=retry=0;spawned=false;clock=null;routeIndex=-2;pool.HideAll();if(!Pending.Ready){model=new Model(Pending.Options.VehicleModel);if(!model.IsInCdImage || !model.IsBicycle){Suspend();Notify.Show("Modelo de bicicleta salvo indisponivel; contrato conservado.");return;}model.Request();}Route(Pending.Ready?Pos(Pending.Options.Checkpoints.Last()):Pos(Pending.Options.Spawn));Notify.Show(Pending.Ready?"Percurso concluido salvo. E no ultimo checkpoint confirma bonus.":"BMX: afaste-se2m e deixe o ponto livre. Entre na bicicleta fornecida; END suspende, tempo nao reinicia.");}
        void Route(Vector3 p){if(route!=null && route.Exists())route.Delete();route=MapBlips.Create(p,"RPCore: "+(Pending.Ready?"confirmar BMX":routeIndex<0?"retirar BMX":"BMX checkpoint "+(routeIndex+1)),BlipSprite.Standard,BlipColor.Green,.85f);route.ShowRoute=true;}
        void Prepare()
        {
            if(!model.IsLoaded){if(unchecked((uint)(Game.GameTime-requested))>10000){Suspend();Notify.Show("BMX nao carregou; contrato conservado.");}return;}
            if(elapsed<retry)return;retry=elapsed+2000;var p=Pos(Pending.Options.Spawn);
            if(Game.Player.Character.Position.DistanceTo(p)<2 || World.GetNearbyVehicles(p,2).Length!=0 || World.GetNearbyPeds(p,1.5f).Length!=0){Notify.Show("Ponto da BMX ocupado ou voce muito perto. Afaste-se2m; aguardando sem apagar outros veiculos.");return;}
            bike=World.CreateVehicle(model,p,Pending.Options.Spawn.Heading);if(bike==null || !bike.Exists()){bike=null;return;}if(!Mark(bike)){bike.MarkAsNoLongerNeeded();bike=null;Suspend();return;}bike.IsPersistent=true;JobVehicles.Reserve(bike);model.MarkAsNoLongerNeeded();model=null;spawned=true;clock=new GangBmxClock(Game.GameTime,Pending.Options.SecondsLimit*1000,Pending.Progress);nextSave=elapsed+1000;Notify.Show("BMX pronta! O prazo restante ja esta contando. Monte e passe pelos aneis em ordem.");
        }
        bool Flush(){if(clock==null || profile!=Profile.Id || Pending?.Id!=id || Pending.Ready || Wallet.GangBmx?.Valid!=true)return true;var a=clock.Progress;var b=Pending.Progress;if(a.Elapsed==b.Elapsed && a.Faulted==b.Faulted)return true;return Wallet.GangBmxUpdate(state(),Current,id,a,out _);}
        public void Tick()
        {
            Drain();if(!Active)return;
            if(Wallet.GangBmx?.Valid!=true || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Safe || !Owns || !Wallet.GangBmxOtherFree){Suspend();Notify.Show("BMX suspensa. Tempo e checkpoints salvos; F4 retoma ou cancela.");return;}
            uint d=unchecked((uint)(Game.GameTime-last));last=Game.GameTime;if(!Input){clock?.Tick(Game.GameTime,false);return;}if(d<=2000)elapsed+=d;var p=Pending;var c=p.Options;
            if(p.Ready){GTA.UI.Screen.ShowHelpTextThisFrame("E no ultimo ponto: confirmar circuito e bonus2X");return;}
            if(!spawned){Prepare();return;}
            if(!OwnBike || bike.IsDead){Fail("Bicicleta indisponivel.");return;}
            if(Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,bike) || bike.Occupants.Any(x=>x.Handle!=player) || Game.Player.Character.Position.DistanceTo(bike.Position)>100 || Game.Player.Character.Position.DistanceTo(Pos(c.Spawn))>250){Suspend();Notify.Show("Bicicleta conectada/ocupada por terceiros ou afastada; progresso conservado.");return;}
            clock.Tick(Game.GameTime,true);if(elapsed>=nextSave){if(!Flush()){Suspend();return;}nextSave=elapsed+1000;}
            if(clock.Progress.Elapsed>=c.SecondsLimit*1000){Fail("Prazo esgotado.");return;}
            GTA.UI.Screen.ShowSubtitle("BMX / "+p.Cleared+"/"+c.Checkpoints.Count+" checkpoints / "+Math.Max(0,c.SecondsLimit-clock.Progress.Elapsed/1000)+"s / END suspende",100);
            int n=Riding?p.Cleared:-1;if(routeIndex!=n){routeIndex=n;Route(n<0?bike.Position:Pos(c.Checkpoints[n]));}else if(n<0 && route!=null && route.Exists())route.Position=bike.Position;
            World.DrawMarker(MarkerType.Ring,Pos(c.Checkpoints[p.Cleared]),Vector3.Zero,Vector3.Zero,new Vector3(1.5f,1.5f,1.5f),Color.FromArgb(160,91,219,135));
            if(!Riding){GTA.UI.Screen.ShowHelpTextThisFrame("Monte na BMX fornecida. A pe ou em outro veiculo nao conta; tempo continua.");return;}
            var position=Point(Game.Player.Character.Position);if(!GangBmxRules.Pass(true,true,position,c.Checkpoints[p.Cleared],c))return;
            if(!Flush() || !Wallet.GangBmxPass(state(),Current,id,p.Cleared,true,true,position,out bool saved) || !saved){Suspend();Notify.Show("Checkpoint nao confirmado; progresso anterior conservado.");return;}
            if(Pending.Ready){clock=null;Route(Pos(c.Checkpoints.Last()));Notify.Show("Todos os checkpoints confirmados! Pare e E no ultimo ponto confirma bonus2X.");}
        }
        public void Interact()
        {if(!Active || !Input || !Owns || !Wallet.GangBmxOtherFree || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Pending.Ready || !(Game.Player.Character.IsOnFoot || Riding && bike.Speed<1))return;var c=Pending.Options;if(!GangBmxRules.Pass(true,true,Point(Game.Player.Character.Position),c.Checkpoints.Last(),c))return;if(Wallet.GangBmxComplete(state(),Current,id,DateTime.UtcNow,out bool saved)){Suspend();Notify.Show(saved?"Circuito concluido! Processamento2X na area12 por"+c.BoostMinutes+"min reais. Ative separadamente.":"Contrato ja confirmado.");}}
        void Fail(string reason){if(clock!=null){clock.Progress.Faulted=true;Flush();}Suspend();Notify.Show("BMX reprovada: "+reason+" Cancele pendencia no F4 para novo circuito; sem premio.");}
        void Drain()
        {
            for(int i=retiring.Count-1;i>=0;i--){var r=retiring[i];if(!Tagged(r.Bike,r.Token)){JobVehicles.Release(r.Bike);retiring.RemoveAt(i);continue;}if(r.Bike.Occupants.Length!=0 || r.Bike.Speed>=.5f || r.Bike.IsInAir || Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,r.Bike))continue;JobVehicles.Release(r.Bike);r.Bike.Delete();retiring.RemoveAt(i);}
        }
        public void Suspend()
        {try{if(Active)Flush();}finally{clock=null;if(bike!=null)retiring.Add(new Retired{Bike=bike,Token=token});bike=null;if(model!=null)model.MarkAsNoLongerNeeded();model=null;if(route!=null && route.Exists())route.Delete();route=null;Active=false;profile=id=null;gate.Release(Owner);Drain();}}
        public void Dispose(){Suspend();foreach(var r in retiring){if(Tagged(r.Bike,r.Token))r.Bike.MarkAsNoLongerNeeded();JobVehicles.Release(r.Bike);}retiring.Clear();}
    }
}
