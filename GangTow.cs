using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
namespace RPCore
{
    internal sealed class GangTow
    {
        const string Owner="gang_tow",Decor="RPCore_TowOwner";
        enum Stage{Truck,TakeTruck,Dispatch,Target,Return,Ready}
        sealed class Retiring{public Vehicle Truck,Target;public Ped Customer;public int Token;}
        readonly List<Retiring> retiring=new List<Retiring>();readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Func<IEnumerable<GangDefinition>> gangs;readonly Func<IEnumerable<GangTurfDefinition>> areas;readonly GangTowOptions options;readonly Random random=new Random();
        Vehicle truck,target;Ped customer;Model truckModel,targetModel,customerModel;Blip route;GangTowCrane crane;Stage stage;bool hooked,dropRequested;string profile,id;int player,token,index,last;long phaseMs,retryMs,dropMs;public bool Active{get;private set;}
        GangTowAttempt Pending=>Wallet.GangTow?.Pending;GangTurfDefinition Current=>areas().FirstOrDefault(a=>a.Id==3);
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        bool Safe=>Game.Player.Character!=null && Game.Player.Character.Exists() && Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && Game.Player.Wanted.WantedLevel==0 && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool Input=>Safe && !Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
        bool Owns=>Pending!=null && GangBusinessTransactions.Owns(state(),Current) && GangCrewRules.SameGang(state().Gang,Pending.Gang) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang));
        static bool Tagged(Entity e,int tag)=>e!=null && e.Exists() && e.Handle!=Game.Player.Character.Handle && Function.Call<bool>(Hash.DECOR_EXIST_ON,e,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,e,Decor)==tag;
        bool Mark(Entity e){try{if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);return Function.Call<bool>(Hash.DECOR_SET_INT,e,Decor,token);}catch{return false;}}
        bool Truck=>Tagged(truck,token);bool Target=>Tagged(target,token);bool Driver=>Truck && truck.Driver==Game.Player.Character && Game.Player.Character.IsInVehicle(truck);
        static bool Attached(Vehicle t,Vehicle v)=>t!=null && t.Exists() && v!=null && v.Exists() && Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TOW_TRUCK,t,v);
        static bool Foreign(Vehicle t,Vehicle v)
        {
            if(t!=null && t.Exists()){int other=Function.Call<int>(Hash.GET_ENTITY_ATTACHED_TO_TOW_TRUCK,t);if(other!=0 && (v==null || !v.Exists() || other!=v.Handle))return true;}
            if(v!=null && v.Exists())foreach(var other in World.GetNearbyVehicles(v.Position,40))if((t==null || !t.Exists() || other.Handle!=t.Handle) && Attached(other,v))return true;
            return false;
        }
        bool Empty=>Target && target.Occupants.Length==0;
        bool Drop(bool attached)=>GangTowRules.Drop(Truck && Target,Driver,Empty,attached,!Foreign(truck,target),crane.Height,truck.Position.DistanceTo(Pos(Pending.Options.Yard)),target.Position.DistanceTo(Pos(Pending.Options.Yard)),truck.Speed);
        bool At(GangTowOptions c)=>Safe && !Game.IsPaused && !InterfaceOptions.ModalOpen && Game.Player.Character.IsOnFoot && Game.Player.Character.Position.DistanceTo(Pos(c.Yard))<=c.ReturnRadius;
        public GangTow(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs,Func<IEnumerable<GangTurfDefinition>> areas)
        {this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;this.areas=areas;options=Store.LoadDef("Gangs/TowRecovery.json",()=>new GangTowOptions());if(options?.Valid!=true){options=new GangTowOptions();Log.Write("Invalid territorial tow config; defaults in memory, original retained.");}}
        public void Controls(Action<string,string,Action> add,Action reopen)
        {
            var p=Pending;if(p==null)return;
            add("Guincho pendente / "+p.Damage.Count+"/"+p.Options.Goal,"Recuperacoes entregues salvas. Atual refaz guincho/engate com mesmo destino/modelo; sem handles salvos.",null);
            add(Active?"Suspender guincho":"Retomar guincho",p.Ready?"A pe no patio: retome e E confirma bonus, sem novos veiculos.":"A pe no patio, com posse. Recria somente veiculos da recuperacao atual; entregas anteriores conservadas. Saia do guincho antigo para liberar a vaga.",()=>{if(Active){Suspend();reopen();}else if(!Resume())Notify.Show("Confira patio a pe/posse/seguranca, atividade e veiculos antigos ocupados.");});
            add("GPS: patio do guincho","Patio salvo do contrato.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,p.Options.Yard.X,p.Options.Yard.Y);pool.HideAll();});
            add("Cancelar contrato do guincho","Gratis, sem premio parcial. Pode cancelar sem filiacao/posse em seguranca. Bonus anterior conserva validade.",()=>{if(!Safe || Game.IsPaused || InterfaceOptions.ModalOpen || gate.Owner!=null && gate.Owner!=Owner || !Wallet.GangTowCancel(p.Id,out _)){Notify.Show("Cancelamento nao confirmado; contrato conservado.");return;}Suspend();reopen();});
        }
        public void Append(GangTurfDefinition area,Action<string,string,Action> add)
        {
            if(area.Id!=3)return;var a=GangTowTransactions.Account(Wallet.GangTow,state().Gang);var utc=DateTime.UtcNow;string status=GangTowTransactions.Multiplier(Wallet.GangTow,state().Gang,3,utc)==2?"ATIVO / "+Math.Ceiling((a.ExpiresUtcTicks-utc.Ticks)/(double)TimeSpan.TicksPerMinute)+"min":"inativo";
            add("Guincho / bonus2X: "+status,"Contratos concluidos: "+(a?.Completed??0)+". Acelera automatico da area3; taxa85% igual. Expira em tempo real; nao ativa automatico sozinho.",null);
            add("GPS: patio de recuperacao","Vaga livre e a pe no patio para iniciar.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,options.Yard.X,options.Yard.Y);pool.HideAll();});
            add("Recuperar veiculos / "+options.Goal+" chamados","Guincho fornecido. NUM8 sobe/NUM2 desce braco, E engata carro proprio da missao, E parado no patio descarrega. Depois de todas entregas, E a pe confirma2X/"+options.BoostMinutes+"min.",()=>{if(!Begin(area))Notify.Show("Exige posse da area3, patio a pe/seguro e nenhuma atividade/contrato pendente.");});
        }
        bool Begin(GangTurfDefinition area)
        {
            if(Active || gate.Owner!=null || retiring.Count>=7 || !At(options) || !gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang)))return false;
            var models=options.Models.Where(n=>new Model(n).IsCar && new Model(n).IsInCdImage).ToArray();var people=options.Customers.Where(n=>new Model(n).IsPed && new Model(n).IsInCdImage).ToArray();if(models.Length==0 || people.Length==0 || !gate.Acquire(Owner))return false;
            try{var sites=options.Sites.OrderBy(_=>random.Next()).ToList();var d=Enumerable.Range(0,options.Goal).Select(i=>new GangTowDispatch{Model=models[random.Next(models.Length)],Customer=people[random.Next(people.Length)],Site=sites[i%sites.Count]}).ToList();if(!Wallet.GangTowBegin(state(),area,options,d,Guid.NewGuid().ToString("N"),out bool changed) || !changed){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}
        }
        bool Resume(){if(Active || gate.Owner!=null || retiring.Count>=7 || Wallet.GangTow?.Valid!=true || !Owns || !Wallet.GangTowOtherFree || !At(Pending.Options) || !gate.Acquire(Owner))return false;Activate();return true;}
        void Activate(){Active=true;profile=Profile.Id;id=Pending.Id;player=Game.Player.Character.Handle;token=random.Next(1,int.MaxValue);last=Game.GameTime;crane=new GangTowCrane(last);hooked=dropRequested=false;Set(Pending.Ready?Stage.Ready:Stage.Truck);pool.HideAll();Route(Pending.Ready?Pos(Pending.Options.Yard):Pos(Pending.Options.TruckSpawn));Notify.Show("Guincho ativo: NUM8/NUM2 controla braco, E engata/descarrega, END suspende. H dos alertas preservado.");}
        void Set(Stage s){stage=s;phaseMs=0;retryMs=0;}
        void Route(Vector3 p){if(route!=null && route.Exists())route.Delete();route=MapBlips.Create(p,"RPCore: recuperacao de veiculo",BlipSprite.Standard,BlipColor.Yellow,.8f);route.ShowRoute=true;}
        static bool Free(Vector3 p,float radius)=>World.GetNearbyVehicles(p,radius).All(v=>v==null || !v.Exists());
        void SpawnTruck()
        {
            var c=Pending.Options;if(truckModel==null){if(!Free(Pos(c.TruckSpawn),4.5f)){Notify.Show("Vaga do guincho ocupada. Afaste seu carro da vaga; nenhum veiculo sera removido.");retryMs=phaseMs+15000;return;}truckModel=new Model(c.TruckModel);if(!truckModel.IsInCdImage || !truckModel.IsCar){Suspend();Notify.Show("Modelo do guincho indisponivel; contrato conservado.");return;}truckModel.Request();retryMs=phaseMs+10000;}
            if(!truckModel.IsLoaded){if(phaseMs>=retryMs){Suspend();Notify.Show("Guincho nao carregou; contrato conservado.");}return;}
            if(!Free(Pos(c.TruckSpawn),4.5f)){ReleaseModels();retryMs=phaseMs+3000;return;}
            truck=World.CreateVehicle(truckModel,Pos(c.TruckSpawn),c.TruckSpawn.Heading);if(truck==null || !truck.Exists()){truck=null;ReleaseModels();retryMs=phaseMs+3000;return;}
            if(!Mark(truck)){truck.MarkAsNoLongerNeeded();truck=null;Suspend();Notify.Show("Falha ao identificar guincho; contrato conservado.");return;}truck.IsPersistent=true;JobVehicles.Reserve(truck);ReleaseModels();Set(Stage.TakeTruck);Route(truck.Position);Notify.Show("Entre no banco do motorista do guincho marcado.");
        }
        void SpawnTarget()
        {
            var d=Pending.Dispatches[Pending.Damage.Count];if(Game.Player.Character.Position.DistanceTo(Pos(d.Site))>120)return;
            if(targetModel==null){if(!Free(Pos(d.Site),5)){Notify.Show("Ponto do chamado ocupado. Aguardando sem remover veiculos.");retryMs=phaseMs+15000;return;}targetModel=new Model(d.Model);customerModel=new Model(d.Customer);if(!targetModel.IsInCdImage || !targetModel.IsCar || !customerModel.IsInCdImage || !customerModel.IsPed){Suspend();Notify.Show("Modelo salvo indisponivel; contrato conservado.");return;}targetModel.Request();customerModel.Request();retryMs=phaseMs+10000;}
            if(!targetModel.IsLoaded || !customerModel.IsLoaded){if(phaseMs>=retryMs){Suspend();Notify.Show("Modelos nao carregaram; contrato conservado.");}return;}
            Vector3 sidewalk=JobActors.Sidewalk(Pos(d.Site));if(sidewalk==Vector3.Zero || World.GetNearbyPeds(sidewalk,2).Length!=0 || !Free(Pos(d.Site),5)){ReleaseModels();retryMs=phaseMs+3000;return;}
            index=Pending.Damage.Count;target=World.CreateVehicle(targetModel,Pos(d.Site),d.Site.Heading);if(target==null || !target.Exists()){target=null;ReleaseModels();retryMs=phaseMs+3000;return;}
            if(!Mark(target)){target.MarkAsNoLongerNeeded();target=null;Suspend();return;}target.IsPersistent=true;JobVehicles.Reserve(target);target.EngineHealth=200;target.BodyHealth=800;target.IsEngineRunning=false;Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS,target,0,true);Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS,target,1,true);
            customer=World.CreatePed(customerModel,sidewalk,180);if(customer==null || !customer.Exists()){customer=null;Suspend();return;}if(!Mark(customer)){customer.MarkAsNoLongerNeeded();customer=null;Suspend();return;}customer.IsPersistent=true;customer.BlockPermanentEvents=true;Function.Call(Hash.SET_PED_MONEY,customer,0);customer.Task.StandStill(-1);ReleaseModels();Set(Stage.Target);Route(target.Position);Notify.Show("Alinhe a traseira do guincho perto do carro avariado, braco baixo. E engata.");
        }
        void ReleaseCustomer(){if(Tagged(customer,token)){if(customer.IsAlive && customer.IsOnFoot)customer.Task.Wander();customer.MarkAsNoLongerNeeded();}customer=null;}
        void ReleaseModels(){foreach(var m in new[]{truckModel,targetModel,customerModel})if(m!=null)m.MarkAsNoLongerNeeded();truckModel=targetModel=customerModel=null;}
        public void Tick()
        {
            Drain();if(!Active)return;
            if(Wallet.GangTow?.Valid!=true || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Safe || !Owns || !Wallet.GangTowOtherFree){Suspend();Notify.Show("Guincho suspenso. Entregas concluidas salvas; F4 retoma ou cancela.");return;}
            uint delta=unchecked((uint)(Game.GameTime-last));last=Game.GameTime;crane.Step(Game.GameTime,Input && Driver,Game.IsKeyPressed(System.Windows.Forms.Keys.NumPad8),Game.IsKeyPressed(System.Windows.Forms.Keys.NumPad2));
            if(!Input)return;if(delta<=2000){phaseMs+=delta;if(dropRequested)dropMs+=delta;}
            if(Driver){Game.DisableControlThisFrame(GTA.Control.Context);Function.Call(Hash.SET_VEHICLE_TOW_TRUCK_ARM_POSITION,truck,crane.Height);}
            var c=Pending.Options;GTA.UI.Screen.ShowSubtitle("Guincho / "+Pending.Damage.Count+"/"+c.Goal+" entregas / braco "+(int)(crane.Height*100)+"% / NUM8-NUM2 / E / END suspende",100);
            if(stage==Stage.Ready){if(At(c))GTA.UI.Screen.ShowHelpTextThisFrame("E a pe: concluir contrato e liberar bonus2X");return;}
            if(stage==Stage.Truck){if(truckModel!=null || phaseMs>=retryMs)SpawnTruck();return;}
            if(!Truck || truck.IsDead || !truck.IsDriveable || Game.Player.Character.Position.DistanceTo(truck.Position)>150){Suspend();Notify.Show("Guincho indisponivel/abandonado. Entregas anteriores conservadas.");return;}
            if(Foreign(truck,target)){Suspend();Notify.Show("Engate com veiculo externo detectado. Contrato suspenso; nenhuma conexao externa sera desfeita.");return;}
            if(stage==Stage.TakeTruck){if(Driver){Set(Stage.Dispatch);Route(Pos(Pending.Dispatches[Pending.Damage.Count].Site));}return;}
            if(stage==Stage.Dispatch){if(retiring.Count>=7){GTA.UI.Screen.ShowHelpTextThisFrame("Aguardando liberacao segura dos veiculos anteriores.");return;}if(targetModel!=null || phaseMs>=retryMs)SpawnTarget();return;}
            if(!Target || target.IsDead || target.BodyHealth<=0 || !GangPresenceRules.Finite(target.BodyHealth) || target.Occupants.Length!=0){Suspend();Notify.Show("Carro do chamado perdido/ocupado. Chamado e entregas anteriores conservados para retomada.");return;}
            bool attached=Attached(truck,target);
            if(attached && !hooked){hooked=true;dropRequested=false;ReleaseCustomer();stage=Stage.Return;Route(Pos(c.Yard));Notify.Show("Engate confirmado. Leve o carro ao patio, pare e baixe o braco para descarregar com E.");}
            if(hooked && !attached)
            {
                if(dropRequested)
                {
                    if(GangTowRules.Delivered(true,true,target.Speed<1,Drop(true)))
                    {
                        float loss=Math.Max(0,800-target.BodyHealth);if(!Wallet.GangTowDeliver(state(),Current,id,index,loss,out bool saved) || !saved){Suspend();Notify.Show("Entrega nao confirmada; contrato conservado.");return;}
                        Notify.Show("Recuperacao salva / condicao "+GangTowRules.Condition(loss)+" / "+Pending.Damage.Count+"/"+c.Goal);retiring.Add(new Retiring{Target=target,Customer=customer,Token=token});target=null;customer=null;hooked=dropRequested=false;Drain();
                        Set(Pending.Ready?Stage.Ready:Stage.Dispatch);Route(Pending.Ready?Pos(c.Yard):Pos(Pending.Dispatches[Pending.Damage.Count].Site));if(Pending.Ready)Notify.Show("Todas entregas salvas. Saia do guincho e E a pe no patio confirma o bonus.");return;
                    }
                    if(dropMs<10000)return;
                }
                hooked=dropRequested=false;stage=Stage.Target;Route(target.Position);Notify.Show("Engate solto sem entrega valida. Reengate o mesmo carro e descarregue no patio com E.");
            }
            if(stage==Stage.Target){if(route!=null && route.Exists())route.Position=target.Position;GTA.UI.Screen.ShowHelpTextThisFrame("Alinhe traseira / parado / NUM2 braco baixo / E engata");}
            else if(Driver && Drop(attached))GTA.UI.Screen.ShowHelpTextThisFrame("E: descarregar carro no patio e confirmar recuperacao");
        }
        public void Interact()
        {
            if(!Active || !Input || !Owns || !Wallet.GangTowOtherFree || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner)return;
            if(stage==Stage.Ready && Pending.Ready){var p=Pending;if(At(p.Options) && Wallet.GangTowComplete(state(),Current,id,DateTime.UtcNow,out bool changed)){Suspend();Notify.Show(changed?"Guincho concluido! Automatico2X na area3 por"+p.Options.BoostMinutes+"min reais. Ative processamento separadamente.":"Contrato ja confirmado; bonus nao repetido.");}return;}
            if(!Truck || !Target || !Driver || target.IsDead || target.BodyHealth<=0 || dropRequested)return;
            if(stage==Stage.Target && GangTowRules.Hook(true,true,Empty,!Foreign(truck,target),crane.Height,truck.GetOffsetPosition(new Vector3(0,-4,0)).DistanceTo(target.Position),truck.Speed))Function.Call(Hash.ATTACH_VEHICLE_TO_TOW_TRUCK,truck,target,false,0f,0f,0f);
            else if(stage==Stage.Return && hooked && Drop(Attached(truck,target))){Function.Call(Hash.DETACH_VEHICLE_FROM_TOW_TRUCK,truck,target);dropRequested=true;dropMs=0;}
        }
        void Drain()
        {
            for(int i=retiring.Count-1;i>=0;i--){var r=retiring[i];bool t=Tagged(r.Truck,r.Token),v=Tagged(r.Target,r.Token);bool foreign=Foreign(t?r.Truck:null,v?r.Target:null);
                if(t && !GangTowRules.Retire(true,r.Truck.Occupants.Length,r.Truck.Speed,foreign) || v && !GangTowRules.Retire(true,r.Target.Occupants.Length,r.Target.Speed,foreign))continue;
                if(t && v && Attached(r.Truck,r.Target))Function.Call(Hash.DETACH_VEHICLE_FROM_TOW_TRUCK,r.Truck,r.Target);
                if(t){JobVehicles.Release(r.Truck);r.Truck.Delete();}else JobVehicles.Release(r.Truck);if(v){JobVehicles.Release(r.Target);r.Target.Delete();}else JobVehicles.Release(r.Target);
                if(Tagged(r.Customer,r.Token))r.Customer.MarkAsNoLongerNeeded();retiring.RemoveAt(i);}
        }
        public void Suspend(){if(truck!=null || target!=null || customer!=null)retiring.Add(new Retiring{Truck=truck,Target=target,Customer=customer,Token=token});truck=target=null;customer=null;ReleaseModels();if(route!=null && route.Exists())route.Delete();route=null;Active=hooked=dropRequested=false;profile=id=null;gate.Release(Owner);Drain();}
        public void Dispose(){Suspend();foreach(var r in retiring){if(Tagged(r.Truck,r.Token))r.Truck.MarkAsNoLongerNeeded();if(Tagged(r.Target,r.Token))r.Target.MarkAsNoLongerNeeded();if(Tagged(r.Customer,r.Token))r.Customer.MarkAsNoLongerNeeded();JobVehicles.Release(r.Truck);JobVehicles.Release(r.Target);}retiring.Clear();}
    }
}
