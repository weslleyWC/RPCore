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
    internal sealed class GangOffshore
    {
        const string Owner="gang_offshore",Decor="RPCore_SeaOwner";
        sealed class Retired{public Vehicle Jet,Boat;public Ped Buyer;public int Token;}
        readonly List<Retired> retiring=new List<Retired>();readonly Dictionary<string,Model> models=new Dictionary<string,Model>(StringComparer.OrdinalIgnoreCase);
        readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Func<IEnumerable<GangDefinition>> gangs;readonly Func<IEnumerable<GangTurfDefinition>> areas;readonly GangOffshoreOptions options;readonly Random random=new Random();
        Vehicle jet,boat;Ped buyer;Blip route;GangOffshoreClock clock;GangOffshoreHandover hand;string profile,id;int player,token,last,requested,routeStage=-1;long elapsed,retry,nextSave;bool spawned,buyerSpawned,boarded;public bool Active{get;private set;}
        GangOffshoreAttempt Pending=>Wallet.GangOffshore?.Pending;GangTurfDefinition Current=>areas().FirstOrDefault(a=>a.Id==13);
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        static bool Tagged(Entity e,int t)=>e!=null && e.Exists() && e.Handle!=Game.Player.Character.Handle && Function.Call<bool>(Hash.DECOR_EXIST_ON,e,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,e,Decor)==t;
        bool Mark(Entity e){try{if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);return Function.Call<bool>(Hash.DECOR_SET_INT,e,Decor,token);}catch{return false;}}
        bool Safe=>Game.Player.Character!=null && Game.Player.Character.Exists() && Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && Game.Player.Wanted.WantedLevel==0 && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool Input=>Safe && !Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
        bool Owns=>Pending!=null && GangBusinessTransactions.Owns(state(),Current) && GangCrewRules.SameGang(Pending.Gang,state().Gang) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang));
        bool Near(JobPoint p,float r)=>Safe && !Game.IsPaused && !InterfaceOptions.ModalOpen && Game.Player.Character.IsOnFoot && Game.Player.Character.Position.DistanceTo(Pos(p))<=r;
        bool OwnJet=>Tagged(jet,token);bool OwnBoat=>Tagged(boat,token);bool OwnBuyer=>Tagged(buyer,token);bool Driver=>OwnJet && Game.Player.Character.IsInVehicle(jet) && jet.Driver==Game.Player.Character;
        public GangOffshore(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs,Func<IEnumerable<GangTurfDefinition>> areas)
        {this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;this.areas=areas;options=Store.LoadDef("Gangs/Offshore.json",()=>new GangOffshoreOptions());if(options?.Valid!=true){options=new GangOffshoreOptions();Log.Write("Invalid offshore config; defaults in memory, original retained.");}}
        public void Controls(Action<string,string,Action> add,Action reopen)
        {
            var p=Pending;if(p==null)return;
            add("Entrega offshore / "+(p.Ready?"entregue":p.Failed?"reprovada":p.Collected?"pacote coletado":"aguardando coleta"),"Restante "+Math.Max(0,p.Options.SecondsLimit-p.Progress.Elapsed/1000)+"s. Pacote e carga abstrata do contrato, nao item pessoal.",null);
            if(!p.Failed)add(Active?"Suspender entrega offshore":"Retomar entrega offshore",p.Ready?"A pe na coleta em terra, retome e E confirma bonus sem repetir viagem.":"Saia do Seashark anterior e deixe-o parado para liberar. Retome a pe no ponto de coleta; pacote e tempo salvos, refaz embarque e viagem.",()=>{if(Active){Suspend();reopen();}else if(!Resume())Notify.Show("Confira coleta a pe, posse, seguranca, equipamento anterior liberado e atividade/pendencias.");});
            add("GPS: offshore pendente","Ponto de coleta/retomada em terra salvo.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,p.Options.Pickup.X,p.Options.Pickup.Y);pool.HideAll();});
            add("Cancelar entrega offshore","Gratis/sem premio parcial, pode sem posse/filiacao em seguranca. Bonus anterior permanece; reprovada exige cancelar para novo contrato.",()=>{if(!Safe || Game.IsPaused || InterfaceOptions.ModalOpen || gate.Owner!=null && gate.Owner!=Owner)return;Suspend();if(!Wallet.GangOffshoreCancel(p.Id,out _))Notify.Show("Cancelamento nao confirmado; contrato conservado.");reopen();});
        }
        public void Append(GangTurfDefinition area,Action<string,string,Action> add)
        {
            if(area.Id!=13)return;var a=GangOffshoreTransactions.Account(Wallet.GangOffshore,state().Gang);var utc=DateTime.UtcNow;int mult=GangOffshoreTransactions.Multiplier(Wallet.GangOffshore,state().Gang,13,utc);
            add("Offshore / bonus2X: "+(mult==2?"ATIVO / "+Math.Ceiling((a.ExpiresUtcTicks-utc.Ticks)/(double)TimeSpan.TicksPerMinute)+"min":"inativo"),"Entregas concluidas: "+(a?.Completed??0)+". Acelera processamento na area13; nao paga dinheiro pessoal nem liga automatico.",null);
            add("GPS: pacote Vespucci","A pe a ate"+options.StartRadius+"m para iniciar.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,options.Pickup.X,options.Pickup.Y);pool.HideAll();});
            add("Entregar pacote em alto-mar","E coleta pacote, use Seashark proprio e pare a ate5m do comprador por5s continuos. Prazo "+options.SecondsLimit+"s ativos; E final confirma2X/"+options.BoostMinutes+"min.",()=>{if(!Begin(area))Notify.Show("Confira posse da area13, coleta a pe, seguranca, equipamentos liberados e atividade/pendencias.");});
        }
        bool Begin(GangTurfDefinition a)
        {if(Active || gate.Owner!=null || retiring.Count!=0 || !Near(options.Pickup,options.StartRadius) || !gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang)) || !gate.Acquire(Owner))return false;try{if(!Wallet.GangOffshoreBegin(state(),a,options,Guid.NewGuid().ToString("N"),out bool saved) || !saved){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}}
        bool Resume(){if(Active || gate.Owner!=null || retiring.Count!=0 || Wallet.GangOffshore?.Valid!=true || Pending==null || Pending.Failed || !Owns || !Wallet.GangOffshoreOtherFree || !Near(Pending.Options.Pickup,Pending.Ready?2:Pending.Options.StartRadius) || !gate.Acquire(Owner))return false;Activate();return true;}
        void Activate(){Active=true;profile=Profile.Id;id=Pending.Id;player=Game.Player.Character.Handle;token=random.Next(1,int.MaxValue);last=requested=Game.GameTime;elapsed=retry=0;spawned=buyerSpawned=boarded=false;clock=null;hand=new GangOffshoreHandover(Game.GameTime);routeStage=-1;pool.HideAll();if(!Pending.Ready){var c=Pending.Options;foreach(string n in new[]{c.JetModel,c.BoatModel,c.BuyerModel}.Distinct(StringComparer.OrdinalIgnoreCase)){var m=new Model(n);models[n]=m;if(!m.IsInCdImage || n==c.JetModel && !m.IsVehicle || n==c.BoatModel && !m.IsBoat || n==c.BuyerModel && !m.IsPed){Suspend();Notify.Show("Modelo salvo indisponivel; contrato conservado.");return;}m.Request();}}Route(Pos(Pending.Options.Pickup));Notify.Show(Pending.Ready?"Entrega salva. E no ponto de coleta em terra confirma bonus.":"Offshore ativo. E coleta pacote; Seashark marcado leva ao comprador. END suspende sem reiniciar prazo.");}
        void Route(Vector3 p){if(route!=null && route.Exists())route.Delete();route=MapBlips.Create(p,"RPCore: entrega offshore",BlipSprite.Standard,BlipColor.Green,.85f);route.ShowRoute=true;}
        bool Free(Vector3 p,float radius)=>World.GetNearbyVehicles(p,radius).Length==0 && World.GetNearbyPeds(p,2).Length==0;
        void Prepare()
        {
            if(!models.Values.All(m=>m.IsLoaded)){if(unchecked((uint)(Game.GameTime-requested))>10000){Suspend();Notify.Show("Modelos nao carregaram; contrato conservado.");}return;}
            if(elapsed<retry)return;retry=elapsed+3000;var c=Pending.Options;if(!Free(Pos(c.Jet),4)){Notify.Show("Ponto do Seashark ocupado; aguardando sem remover outras embarcacoes.");return;}
            jet=World.CreateVehicle(models[c.JetModel],Pos(c.Jet),c.Jet.Heading);if(jet==null || !jet.Exists()){jet=null;return;}if(!Mark(jet)){jet.MarkAsNoLongerNeeded();jet=null;Suspend();return;}jet.IsPersistent=true;JobVehicles.Reserve(jet);spawned=true;clock=new GangOffshoreClock(Game.GameTime,c.SecondsLimit*1000,Pending.Progress);nextSave=elapsed+1000;retry=0;
        }
        void Buyer()
        {
            if(buyerSpawned || elapsed<retry || Game.Player.Character.Position.DistanceTo(Pos(Pending.Options.Buyer))>300)return;retry=elapsed+3000;var c=Pending.Options;if(!Free(Pos(c.Buyer),7)){GTA.UI.Screen.ShowHelpTextThisFrame("Ponto do comprador ocupado. Deixe espaco livre; contrato conservado.");return;}
            boat=World.CreateVehicle(models[c.BoatModel],Pos(c.Buyer),c.Buyer.Heading);if(boat==null || !boat.Exists()){boat=null;return;}if(!Mark(boat)){boat.MarkAsNoLongerNeeded();boat=null;Suspend();return;}boat.IsPersistent=true;JobVehicles.Reserve(boat);buyer=World.CreatePed(models[c.BuyerModel],Pos(c.Buyer)+new Vector3(0,0,2));if(buyer==null || !buyer.Exists()){buyer=null;Suspend();return;}if(!Mark(buyer)){buyer.MarkAsNoLongerNeeded();buyer=null;Suspend();return;}buyer.IsPersistent=true;buyer.BlockPermanentEvents=true;buyer.Weapons.RemoveAll();Function.Call(Hash.SET_PED_MONEY,buyer,0);buyer.RelationshipGroup=new RelationshipGroup((int)RelationshipGroupHash.CivilianMale);buyer.SetIntoVehicle(boat,VehicleSeat.Driver);boat.IsEngineRunning=false;boat.IsPositionFrozen=true;buyerSpawned=true;
        }
        bool Flush(){if(clock==null || profile!=Profile.Id || Pending?.Id!=id || Pending.Ready || Wallet.GangOffshore?.Valid!=true)return true;var a=clock.Progress;var b=Pending.Progress;if(a.Elapsed==b.Elapsed && a.Faulted==b.Faulted)return true;return Wallet.GangOffshoreUpdate(state(),Current,id,a,out _);}
        public void Tick()
        {
            Drain();if(!Active)return;
            if(Wallet.GangOffshore?.Valid!=true || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Safe || !Owns || !Wallet.GangOffshoreOtherFree){Suspend();Notify.Show("Offshore suspenso. Coleta/tempo/entrega salvos; F4 retoma ou cancela.");return;}
            uint d=unchecked((uint)(Game.GameTime-last));last=Game.GameTime;if(!Input){clock?.Tick(Game.GameTime,false);hand?.Step(Game.GameTime,false);return;}if(d<=2000)elapsed+=d;var p=Pending;var c=p.Options;
            if(p.Ready){GTA.UI.Screen.ShowHelpTextThisFrame("E: confirmar entrega e bonus perto do comprador, ou no ponto de coleta em terra.");return;}
            if(!spawned){Prepare();return;}
            if(!OwnJet || jet.IsDead || buyerSpawned && (!OwnBoat || !OwnBuyer || boat.IsDead || !buyer.IsAlive || !buyer.IsInVehicle(boat))){Fail("Equipamento ou comprador indisponivel.");return;}
            if(Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,jet) || jet.Occupants.Any(x=>x.Handle!=player) || Game.Player.Character.Position.DistanceTo(jet.Position)>150 || buyerSpawned && (Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,boat) || boat.Occupants.Any(x=>x.Handle!=buyer.Handle))){Suspend();Notify.Show("Embarcacao ocupada/conectada ou Seashark afastado; progresso conservado.");return;}
            clock.Tick(Game.GameTime,true);if(elapsed>=nextSave){if(!Flush()){Suspend();return;}nextSave=elapsed+1000;}if(clock.Progress.Elapsed>=c.SecondsLimit*1000){Fail("Prazo esgotado.");return;}
            Buyer();if(!Active)return;
            GTA.UI.Screen.ShowSubtitle("Offshore / "+(p.Collected?"pacote coletado":"coletar pacote")+" / "+Math.Max(0,c.SecondsLimit-clock.Progress.Elapsed/1000)+"s / END suspende",100);
            if(p.Collected && Driver && !boarded){if(!Flush() || !Wallet.GangOffshoreBoard(state(),Current,id,out _)){Suspend();return;}boarded=true;}
            int stage=!p.Collected?0:!boarded?1:2;if(routeStage!=stage){routeStage=stage;Route(stage==0?Pos(c.Pickup):stage==1?jet.Position:buyerSpawned?boat.Position:Pos(c.Buyer));}else if(stage==1 && route!=null && route.Exists())route.Position=jet.Position;
            if(stage==0){World.DrawMarker(MarkerType.Cylinder,Pos(c.Pickup),Vector3.Zero,Vector3.Zero,new Vector3(1.8f,1.8f,.5f),Color.FromArgb(150,91,219,135));if(GangOffshoreRules.Pickup(true,Game.Player.Character.IsOnFoot,Game.Player.Character.Position.DistanceTo(Pos(c.Pickup))))GTA.UI.Screen.ShowHelpTextThisFrame("E: coletar pacote do contrato");hand.Step(Game.GameTime,false);return;}
            if(stage==1){GTA.UI.Screen.ShowHelpTextThisFrame("Entre como motorista no Seashark fornecido.");hand.Step(Game.GameTime,false);return;}
            if(!buyerSpawned){GTA.UI.Screen.ShowHelpTextThisFrame("Siga ao comprador em alto-mar; mantenha o ponto de encontro livre.");hand.Step(Game.GameTime,false);return;}
            if(route!=null && route.Exists())route.Position=boat.Position;
            World.DrawMarker(MarkerType.Cylinder,boat.Position+new Vector3(0,0,1),Vector3.Zero,Vector3.Zero,new Vector3(7,7,1),Color.FromArgb(120,91,219,135));
            bool valid=GangOffshoreRules.Handover(true,Driver,OwnBuyer && buyer.IsInVehicle(boat),jet.Position.DistanceTo(boat.Position),jet.Speed);
            GTA.UI.Screen.ShowHelpTextThisFrame(valid?"Transferindo pacote / mantenha-se a ate5m e abaixo de1,5m/s por5s.":"Aproxime o Seashark do barco do comprador e reduza a velocidade.");
            if(!hand.Step(Game.GameTime,valid))return;
            if(!Flush() || !Wallet.GangOffshoreDeliver(state(),Current,id,out bool saved) || !saved){Suspend();return;}clock=null;Notify.Show("Pacote entregue e salvo! E aqui confirma processamento2X.");
        }
        public void Interact()
        {
            if(!Active || !Input || !Owns || !Wallet.GangOffshoreOtherFree || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner)return;var c=Pending.Options;
            if(Pending.Ready){if(!(Near(c.Pickup,2) || Driver && OwnBoat && jet.Position.DistanceTo(boat.Position)<=5 && jet.Speed<1.5f))return;if(Wallet.GangOffshoreComplete(state(),Current,id,DateTime.UtcNow,out bool saved)){Suspend();Notify.Show(saved?"Entrega concluida! Processamento2X na area13 por"+c.BoostMinutes+"min reais. Ative separadamente.":"Contrato ja confirmado.");}return;}
            if(!spawned || Pending.Collected || !GangOffshoreRules.Pickup(true,Game.Player.Character.IsOnFoot,Game.Player.Character.Position.DistanceTo(Pos(c.Pickup))))return;
            if(!Flush() || !Wallet.GangOffshoreCollect(state(),Current,id,out bool changed) || !changed){Suspend();return;}Notify.Show("Pacote coletado e salvo. Entre no Seashark marcado.");
        }
        void Fail(string reason){if(clock!=null){clock.Progress.Faulted=true;Flush();}Suspend();Notify.Show("Offshore reprovado: "+reason+" Cancele pendencia no F4 para nova entrega.");}
        void Drain()
        {
            for(int i=retiring.Count-1;i>=0;i--){var r=retiring[i];bool ownBoat=Tagged(r.Boat,r.Token),ownBuyer=Tagged(r.Buyer,r.Token);
                if(ownBoat){r.Boat.IsPositionFrozen=false;if(r.Boat.Occupants.Any(p=>!ownBuyer || p.Handle!=r.Buyer.Handle) || Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,r.Boat))continue;r.Boat.IsPositionFrozen=false;if(ownBuyer){r.Buyer.BlockPermanentEvents=false;r.Buyer.MarkAsNoLongerNeeded();r.Buyer=null;}r.Boat.MarkAsNoLongerNeeded();JobVehicles.Release(r.Boat);r.Boat=null;}
                else if(ownBuyer){if(r.Buyer.IsInVehicle())r.Buyer.MarkAsNoLongerNeeded();else r.Buyer.Delete();r.Buyer=null;}
                if(Tagged(r.Jet,r.Token)){if(r.Jet.Occupants.Length!=0 || r.Jet.Speed>=1.5f || r.Jet.IsInAir || Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,r.Jet))continue;r.Jet.Delete();}JobVehicles.Release(r.Jet);JobVehicles.Release(r.Boat);retiring.RemoveAt(i);}
        }
        public void Suspend(){try{if(Active)Flush();}finally{clock=null;hand=null;if(jet!=null || boat!=null || buyer!=null)retiring.Add(new Retired{Jet=jet,Boat=boat,Buyer=buyer,Token=token});jet=boat=null;buyer=null;foreach(var m in models.Values)m.MarkAsNoLongerNeeded();models.Clear();if(route!=null && route.Exists())route.Delete();route=null;Active=false;profile=id=null;gate.Release(Owner);Drain();}}
        public void Dispose(){try{Suspend();}finally{foreach(var r in retiring){if(Tagged(r.Jet,r.Token))r.Jet.MarkAsNoLongerNeeded();if(Tagged(r.Boat,r.Token)){r.Boat.IsPositionFrozen=false;r.Boat.MarkAsNoLongerNeeded();}if(Tagged(r.Buyer,r.Token))r.Buyer.MarkAsNoLongerNeeded();JobVehicles.Release(r.Jet);JobVehicles.Release(r.Boat);}retiring.Clear();}}
    }
}
