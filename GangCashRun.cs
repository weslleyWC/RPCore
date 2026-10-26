using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
namespace RPCore
{
    internal sealed class GangCashRun
    {
        const string Owner="gang_cash_run",Decor="RPCore_CashRunOwner",Anim="mp_common";
        enum Stage{Truck,TakeTruck,Collect,Load,Depot,Ready}
        sealed class Retiring{public Vehicle Truck;public Ped Receiver;public List<Prop> Bags;public int Token,Last;public long Age;public bool Depart,Entering;}
        readonly List<Retiring> retiring=new List<Retiring>();readonly List<Prop> bags=new List<Prop>();
        readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Func<IEnumerable<GangDefinition>> gangs;readonly Func<IEnumerable<GangTurfDefinition>> areas;readonly GangCashRunOptions options;readonly Random random=new Random();
        Vehicle truck;Ped guard,receiver,handActor,animOwner;Prop held;Model vehicleModel,pedModel,bagModel;Blip route;GangPromotionClock hand;Stage stage;bool render,arrived,animated,delivering;string profile,id,playerClip,actorClip;int token,player,next,last,requestAt,sceneAt;long elapsed,retry;public bool Active{get;private set;}
        GangCashRunAttempt Pending=>Wallet.GangCashRun?.Pending;GangTurfDefinition Current=>areas().FirstOrDefault(a=>a.Id==5);
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        static bool Tagged(Entity e,int value)=>e!=null && e.Exists() && e.Handle!=Game.Player.Character.Handle && Function.Call<bool>(Hash.DECOR_EXIST_ON,e,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,e,Decor)==value;
        bool Mark(Entity e){try{if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);return Function.Call<bool>(Hash.DECOR_SET_INT,e,Decor,token);}catch{return false;}}
        bool Truck=>Tagged(truck,token);bool Driver=>Truck && truck.Driver==Game.Player.Character && Game.Player.Character.IsInVehicle(truck);
        bool Stopped=>Truck && truck.Speed<.5f && truck.Occupants.All(p=>p==Game.Player.Character) && (truck.Driver==null || truck.Driver==Game.Player.Character);
        Vector3 Rear=>truck.GetOffsetPosition(new Vector3(0,-3.8f,0));
        bool Safe=>Game.Player.Character!=null && Game.Player.Character.Exists() && Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && Game.Player.Wanted.WantedLevel==0 && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool Input=>Safe && !Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
        bool Owns=>Pending!=null && GangBusinessTransactions.Owns(state(),Current) && GangCrewRules.SameGang(Pending.Gang,state().Gang) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang));
        bool At(JobPoint p,int radius)=>Safe && !Game.IsPaused && !InterfaceOptions.ModalOpen && Game.Player.Character.IsOnFoot && Game.Player.Character.Position.DistanceTo(Pos(p))<=radius;
        public GangCashRun(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs,Func<IEnumerable<GangTurfDefinition>> areas)
        {this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;this.areas=areas;options=Store.LoadDef("Gangs/CashRun.json",()=>new GangCashRunOptions());if(options?.Valid!=true){options=new GangCashRunOptions();Log.Write("Invalid territorial cash run config; defaults in memory, original retained.");}}
        public void Controls(Action<string,string,Action> add,Action reopen)
        {
            var p=Pending;if(p==null)return;
            add("Valores pendentes / "+p.Loaded.Count+"/"+p.Options.Sites.Count+" malotes",p.Delivered?"Entrega no recebedor salva. Retome a pe no deposito e confirme bonus.":"Malotes no blindado nao sao dinheiro pessoal. Parcial refaz coletas ao retomar; pronto entregue conserva prova.",null);
            add(Active?"Suspender transporte de valores":"Retomar transporte de valores",p.Delivered?"A pe a ate2m do ponto do recebedor, sem recriar blindado.":"A pe no ponto inicial do Stockade, com posse. Refaz malotes/carga, mesmos pontos/opcoes e sem pagar novamente. Saia do blindado antigo para liberar vaga.",()=>{if(Active){Suspend();reopen();}else if(!Resume())Notify.Show("Confira ponto de retomada a pe, posse, seguranca, atividade e blindado antigo ocupado.");});
            add("GPS: transporte pendente","Retomada parcial no inicio; entregue no ponto do recebedor.",()=>{var q=p.Delivered?p.Options.Receiver:p.Options.Spawn;Function.Call(Hash.SET_NEW_WAYPOINT,q.X,q.Y);pool.HideAll();});
            add("Cancelar transporte de valores","Gratis, sem pagamento/estoque/bonus parcial. Pode cancelar sem posse/filiacao em seguranca; bonus anterior conserva validade.",()=>{if(!Safe || Game.IsPaused || InterfaceOptions.ModalOpen || gate.Owner!=null && gate.Owner!=Owner || !Wallet.GangCashRunCancel(p.Id,out _)){Notify.Show("Cancelamento nao confirmado; contrato conservado.");return;}Suspend();reopen();});
        }
        public void Append(GangTurfDefinition area,Action<string,string,Action> add)
        {
            if(area.Id!=5)return;var a=GangCashRunTransactions.Account(Wallet.GangCashRun,state().Gang);var utc=DateTime.UtcNow;string status=GangCashRunTransactions.Multiplier(Wallet.GangCashRun,state().Gang,5,utc)==2?"ATIVO / "+Math.Ceiling((a.ExpiresUtcTicks-utc.Ticks)/(double)TimeSpan.TicksPerMinute)+"min":"inativo";
            add("Valores / bonus2X: "+status,"Contratos concluidos: "+(a?.Completed??0)+". Acelera automatico da area5;85% ao caixa igual. Validade real, nao liga automatico.",null);
            add("GPS: Stockade Gruppe","Va a pe ao blindado para iniciar; vaga livre.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,options.Spawn.X,options.Spawn.Y);pool.HideAll();});
            add("Transportar valores / "+options.Sites.Count+" malotes","Stockade fornecido. E com seguranca pega malote; E na traseira carrega. Leve todos ao deposito, fale com recebedor e confirme bonus2X/"+options.BoostMinutes+"min. Sem dinheiro pessoal.",()=>{if(!Begin(area))Notify.Show("Exige posse da area5, ponto inicial a pe/seguro e nenhuma atividade/contrato pendente.");});
        }
        bool Begin(GangTurfDefinition area)
        {if(Active || gate.Owner!=null || retiring.Count>=7 || !At(options.Spawn,options.ReturnRadius) || !gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang)) || !gate.Acquire(Owner))return false;try{if(!Wallet.GangCashRunBegin(state(),area,options,Guid.NewGuid().ToString("N"),out bool changed) || !changed){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}}
        bool Resume()
        {if(Active || gate.Owner!=null || retiring.Count>=7 || Wallet.GangCashRun?.Valid!=true || !Owns || !Wallet.GangCashRunOtherFree || !At(Pending.Delivered?Pending.Options.Receiver:Pending.Options.Spawn,Pending.Delivered?2:Pending.Options.ReturnRadius) || !gate.Acquire(Owner))return false;try{if(!Pending.Delivered && !Wallet.GangCashRunRestart(Pending.Id,out _)){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}}
        void Activate(){Active=true;profile=Profile.Id;id=Pending.Id;player=Game.Player.Character.Handle;token=random.Next(1,int.MaxValue);last=Game.GameTime;elapsed=retry=0;arrived=false;render=Visuals.Enabled && Visuals.UseProps;stage=Pending.Delivered?Stage.Ready:Stage.Truck;pool.HideAll();Route(Pos(Pending.Delivered?Pending.Options.Receiver:Pending.Options.Spawn));Notify.Show("Transporte ativo: E com seguranca pega malote, E na traseira carrega. END suspende.");}
        void Route(Vector3 p){if(route!=null && route.Exists())route.Delete();route=MapBlips.Create(p,"RPCore: transporte de valores",BlipSprite.ArmoredTruck,BlipColor.Yellow,.8f);route.ShowRoute=true;}
        void Choose(){next=Enumerable.Range(0,Pending.Options.Sites.Count).Where(i=>!Pending.Loaded.Contains(i)).OrderBy(i=>Pos(Pending.Options.Sites[i]).DistanceTo(truck.Position)).First();stage=Stage.Collect;Route(Pos(Pending.Options.Sites[next]));}
        static bool Foreign(Vehicle v)=>v!=null && v.Exists() && (Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,v) || World.GetNearbyVehicles(v.Position,40).Any(t=>t.Handle!=v.Handle && Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TOW_TRUCK,t,v)));
        void Idle(Ped p){if(Tagged(p,token) && p.IsAlive && p.IsOnFoot)Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE,p,"WORLD_HUMAN_GUARD_STAND",0,true);}
        bool PrepareModels()
        {
            var c=Pending.Options;if(pedModel==null){pedModel=new Model(c.GuardModel);vehicleModel=new Model(c.VehicleModel);if(render)bagModel=new Model(c.BagModel);if(!pedModel.IsInCdImage || !pedModel.IsPed || !vehicleModel.IsInCdImage || !vehicleModel.IsCar || render && !bagModel.IsInCdImage){Suspend();Notify.Show("Modelo salvo indisponivel; contrato conservado.");return false;}pedModel.Request();vehicleModel.Request();if(render)bagModel.Request();requestAt=Game.GameTime;}
            if(pedModel.IsLoaded && vehicleModel.IsLoaded && (!render || bagModel.IsLoaded))return true;
            if(unchecked((uint)(Game.GameTime-requestAt))>10000){Suspend();Notify.Show("Modelos nao carregaram; contrato conservado para retomada.");}return false;
        }
        Ped SpawnPed(JobPoint q)
        {
            var point=Pos(q);if(Game.Player.Character.Position.DistanceTo(point)<1.5f || World.GetNearbyPeds(point,1.2f).Any(p=>p.Handle!=player) || World.GetNearbyVehicles(point,1).Length!=0)return null;
            var p=World.CreatePed(pedModel,point,q.Heading);if(p==null || !p.Exists())return null;if(!Mark(p)){p.MarkAsNoLongerNeeded();return null;}p.IsPersistent=true;p.BlockPermanentEvents=true;p.Weapons.RemoveAll();p.RelationshipGroup=new RelationshipGroup((int)RelationshipGroupHash.CivilianMale);Function.Call(Hash.SET_PED_MONEY,p,0);Idle(p);return p;
        }
        void StopHand()
        {if(animated){if(animOwner!=null && animOwner.Exists())Function.Call(Hash.STOP_ANIM_TASK,animOwner,Anim,playerClip,2f);if(Tagged(handActor,token))Function.Call(Hash.STOP_ANIM_TASK,handActor,Anim,actorClip,2f);Idle(handActor);}animated=false;hand=null;handActor=animOwner=null;}
        bool HandoverReady()=>Truck && Stopped && GangCashRunRules.Handover(true,Game.Player.Character.IsOnFoot,arrived,Pending.Loaded.Count==Pending.Options.Sites.Count,Game.Player.Character.Position.DistanceTo(receiver.Position),truck.Position.DistanceTo(Pos(Pending.Options.Depot)),truck.Speed);
        void StartHand(Ped p,bool delivery){handActor=p;delivering=delivery;playerClip=delivery?"givetake1_a":"givetake1_b";actorClip=delivery?"givetake1_b":"givetake1_a";hand=new GangPromotionClock(Game.GameTime);sceneAt=Game.GameTime;Function.Call(Hash.REQUEST_ANIM_DICT,Anim);}
        void TickHand()
        {
            bool valid=Input && Game.Player.Character.IsOnFoot && Tagged(handActor,token) && handActor.IsAlive && handActor.IsOnFoot && (delivering?Tagged(receiver,token) && HandoverReady():Truck && Stopped && Game.Player.Character.Position.DistanceTo(handActor.Position)<=2 && truck.Position.DistanceTo(Pos(Pending.Options.Sites[next]))<=35);
            if(!valid){StopHand();return;}
            if(!animated){if(!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED,Anim)){if(unchecked((uint)(Game.GameTime-sceneAt))>10000){StopHand();Notify.Show("Animacao indisponivel; nenhuma transferencia confirmada.");}return;}Function.Call(Hash.CLEAR_PED_TASKS,handActor);animOwner=Game.Player.Character;Function.Call(Hash.TASK_PLAY_ANIM,animOwner,Anim,playerClip,4f,-4f,1500,48,0f,false,false,false);Function.Call(Hash.TASK_PLAY_ANIM,handActor,Anim,actorClip,4f,-4f,1500,48,0f,false,false,false);animated=true;hand=new GangPromotionClock(Game.GameTime);}
            if(!hand.Step(Game.GameTime,true,1500))return;bool delivery=delivering;StopHand();
            if(delivery){if(!Wallet.GangCashRunDeliver(state(),Current,id,out bool saved) || !saved){Suspend();Notify.Show("Entrega nao confirmada; contrato conservado.");return;}stage=Stage.Ready;Route(Pos(Pending.Options.Receiver));Notify.Show("Malotes entregues e prova salva. E no recebedor confirma o bonus.");return;}
            if(render){held=World.CreateProp(bagModel,Game.Player.Character.Position,false,false);if(held==null || !held.Exists()){held=null;return;}if(!Mark(held)){held.MarkAsNoLongerNeeded();held=null;Suspend();return;}held.IsPersistent=true;held.IsCollisionEnabled=false;held.AttachTo(Game.Player.Character.Bones[(Bone)28422],new Vector3(.45f,-.05f,0),new Vector3(90,90,-190));}
            stage=Stage.Load;Route(Rear);Notify.Show("Malote em maos. A pe ate a traseira do Stockade e E para carregar.");
        }
        public void Tick()
        {
            Drain();if(!Active)return;
            if(Wallet.GangCashRun?.Valid!=true || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Safe || !Owns || !Wallet.GangCashRunOtherFree){Suspend();Notify.Show("Transporte suspenso. Contrato salvo; F4 retoma ou cancela.");return;}
            uint delta=unchecked((uint)(Game.GameTime-last));last=Game.GameTime;if(!Input){StopHand();return;}if(delta<=2000)elapsed+=delta;
            var c=Pending.Options;GTA.UI.Screen.ShowSubtitle("Valores / "+Pending.Loaded.Count+"/"+c.Sites.Count+" malotes / END suspende",100);
            if(stage==Stage.Ready){if(At(c.Receiver,2))GTA.UI.Screen.ShowHelpTextThisFrame("E: confirmar transporte concluido e liberar bonus2X");return;}
            if(!PrepareModels())return;
            if(stage==Stage.Truck)
            {
                if(Game.Player.Character.Position.DistanceTo(Pos(c.Spawn))<4){GTA.UI.Screen.ShowHelpTextThisFrame("Afaste-se4m da vaga do Stockade para a chegada segura.");return;}if(elapsed<retry)return;retry=elapsed+3000;if(World.GetNearbyVehicles(Pos(c.Spawn),4).Length!=0 || World.GetNearbyPeds(Pos(c.Spawn),3).Any(p=>p.Handle!=player)){Notify.Show("Vaga do Stockade ocupada. Deixe espaco para o blindado; nada sera removido.");retry=elapsed+15000;return;}
                truck=World.CreateVehicle(vehicleModel,Pos(c.Spawn),c.Spawn.Heading);if(truck==null || !truck.Exists()){truck=null;return;}if(!Mark(truck)){truck.MarkAsNoLongerNeeded();truck=null;Suspend();return;}truck.IsPersistent=true;JobVehicles.Reserve(truck);stage=Stage.TakeTruck;Route(truck.Position);return;
            }
            if(!Truck || truck.IsDead || !truck.IsDriveable || Foreign(truck) || truck.Occupants.Any(p=>p!=Game.Player.Character) || Game.Player.Character.Position.DistanceTo(truck.Position)>150 || stage==Stage.Load && Game.Player.Character.IsInVehicle())
            {Suspend();Notify.Show("Blindado indisponivel/abandonado ou entrada com malote na mao. Contrato conservado.");return;}
            if(render && (bags.Any(p=>!Tagged(p,token) || !Function.Call<bool>(Hash.IS_ENTITY_ATTACHED_TO_ENTITY,p,truck)) || stage==Stage.Load && !Tagged(held,token))){Suspend();Notify.Show("Carga visual perdida. Contrato conservado; retomada refaz malotes.");return;}
            if(stage==Stage.TakeTruck){if(Driver)Choose();return;}
            if(stage==Stage.Collect && !Tagged(guard,token) && Game.Player.Character.Position.DistanceTo(Pos(c.Sites[next]))<70 && elapsed>=retry){retry=elapsed+3000;guard=SpawnPed(c.Sites[next]);if(guard==null)GTA.UI.Screen.ShowHelpTextThisFrame("Deixe livre o ponto do seguranca e afaste-se2m para chegada.");}
            if(stage==Stage.Depot && !Tagged(receiver,token) && Game.Player.Character.Position.DistanceTo(Pos(c.Receiver))<70 && elapsed>=retry){retry=elapsed+3000;receiver=SpawnPed(c.Receiver);if(receiver==null)GTA.UI.Screen.ShowHelpTextThisFrame("Afaste-se2m do ponto do recebedor e deixe acesso livre.");}
            if(Tagged(guard,token) && !guard.IsAlive || Tagged(receiver,token) && !receiver.IsAlive){Suspend();Notify.Show("Seguranca indisponivel. Contrato conservado.");return;}
            if(stage==Stage.Depot && GangCashRunRules.Arrive(true,Driver,Pending.Loaded.Count,c.Sites.Count,truck.Position.DistanceTo(Pos(c.Depot)),truck.Speed))arrived=true;
            if(arrived && truck.Position.DistanceTo(Pos(c.Depot))>10)arrived=false;
            if(hand!=null){TickHand();return;}
            if(stage==Stage.Load){if(route!=null && route.Exists())route.Position=Rear;GTA.UI.Screen.ShowHelpTextThisFrame("E a pe na traseira do blindado parado: guardar malote");}
            else if(stage==Stage.Collect)GTA.UI.Screen.ShowHelpTextThisFrame(Tagged(guard,token)?"Estacione o Stockade perto. E a ate2m do seguranca: receber malote":"Aguardando seguranca: deixe o ponto livre e afaste-se2m.");
            else if(stage==Stage.Depot)GTA.UI.Screen.ShowHelpTextThisFrame(!arrived?"Dirija o Stockade carregado ao ponto do deposito e pare a ate6m":Tagged(receiver,token)?"Saia e E a ate2m do recebedor: entregar blindado":"Aguardando recebedor: deixe o ponto livre e afaste-se2m.");
        }
        public void Interact()
        {
            if(!Active || !Input || !Owns || !Wallet.GangCashRunOtherFree || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Game.Player.Character.IsOnFoot || hand!=null)return;
            var c=Pending.Options;
            if(stage==Stage.Ready){if(At(c.Receiver,2) && Wallet.GangCashRunComplete(state(),Current,id,DateTime.UtcNow,out bool saved)){Finish(saved);Notify.Show(saved?"Transporte concluido! Automatico2X na area5 por"+c.BoostMinutes+"min reais. Ative processamento separadamente.":"Contrato ja confirmado; bonus nao repetido.");}return;}
            if(!Truck || !Stopped)return;
            if(stage==Stage.Collect && Tagged(guard,token) && guard.IsAlive && guard.IsOnFoot && Game.Player.Character.Position.DistanceTo(guard.Position)<=2 && truck.Position.DistanceTo(Pos(c.Sites[next]))<=35)StartHand(guard,false);
            else if(stage==Stage.Load && GangCashRunRules.Load(true,true,true,true,Game.Player.Character.Position.DistanceTo(Rear),truck.Position.DistanceTo(Pos(c.Sites[next]))))
            {
                if(render && !Tagged(held,token))return;if(!Wallet.GangCashRunLoad(state(),Current,id,next,out bool saved) || !saved){Suspend();Notify.Show("Carga nao confirmada; contrato conservado.");return;}
                if(render){int slot=bags.Count;held.AttachTo(truck,new Vector3(slot%2==0?-.45f:.45f,-1.1f-(slot/2)*.55f,.45f),Vector3.Zero);bags.Add(held);held=null;}
                ClearPed(ref guard);if(Pending.Loaded.Count==c.Sites.Count){stage=Stage.Depot;Route(Pos(c.Depot));}else Choose();Notify.Show("Malote carregado / "+Pending.Loaded.Count+"/"+c.Sites.Count);
            }
            else if(stage==Stage.Depot && Tagged(receiver,token) && receiver.IsAlive && HandoverReady())StartHand(receiver,true);
        }
        void ClearPed(ref Ped p){if(Tagged(p,token)){if(p.IsInVehicle())p.MarkAsNoLongerNeeded();else p.Delete();}p=null;}
        static void ClearBags(IEnumerable<Prop> props,int value){foreach(var p in props)if(Tagged(p,value))p.Delete();}
        void Finish(bool depart)
        {StopHand();ClearPed(ref guard);if(Tagged(held,token))held.Delete();held=null;if(truck!=null)retiring.Add(new Retiring{Truck=truck,Receiver=receiver,Bags=new List<Prop>(bags),Token=token,Depart=depart,Last=Game.GameTime});else ClearPed(ref receiver);truck=null;receiver=null;bags.Clear();CloseState();Drain();}
        void CloseState(){foreach(var m in new[]{vehicleModel,pedModel,bagModel})if(m!=null)m.MarkAsNoLongerNeeded();vehicleModel=pedModel=bagModel=null;if(route!=null && route.Exists())route.Delete();route=null;Active=false;profile=id=null;gate.Release(Owner);}
        void Drain()
        {
            for(int i=retiring.Count-1;i>=0;i--){var r=retiring[i];uint delta=unchecked((uint)(Game.GameTime-r.Last));r.Last=Game.GameTime;if(!Game.IsPaused && delta<=2000)r.Age+=delta;bool own=Tagged(r.Truck,r.Token),ped=Tagged(r.Receiver,r.Token);
                if(own && (Foreign(r.Truck) || r.Truck.Occupants.Any(p=>!ped || p!=r.Receiver)))continue;
                if(own && r.Depart && ped && r.Receiver.IsAlive)
                {
                    if(r.Truck.Driver==r.Receiver){Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER,r.Receiver,r.Truck,8f,786603);foreach(var p in r.Bags)if(Tagged(p,r.Token))p.MarkAsNoLongerNeeded();r.Receiver.MarkAsNoLongerNeeded();r.Truck.MarkAsNoLongerNeeded();JobVehicles.Release(r.Truck);retiring.RemoveAt(i);continue;}
                    if(!r.Entering){JobActors.Enter(r.Receiver,r.Truck,VehicleSeat.Driver);r.Entering=true;}if(r.Age<30000)continue;
                }
                if(own && (r.Truck.Occupants.Length!=0 || r.Truck.Speed>=.5f))continue;ClearBags(r.Bags,r.Token);if(own)r.Truck.Delete();JobVehicles.Release(r.Truck);if(ped)r.Receiver.MarkAsNoLongerNeeded();retiring.RemoveAt(i);
            }
        }
        public void Suspend(){StopHand();ClearPed(ref guard);Finish(false);}
        public void Dispose(){Suspend();foreach(var r in retiring){if(Tagged(r.Truck,r.Token))r.Truck.MarkAsNoLongerNeeded();if(Tagged(r.Receiver,r.Token))r.Receiver.MarkAsNoLongerNeeded();foreach(var p in r.Bags)if(Tagged(p,r.Token))p.MarkAsNoLongerNeeded();JobVehicles.Release(r.Truck);}retiring.Clear();}
    }
}
