using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
namespace RPCore
{
    internal sealed class GangCashier
    {
        const string Owner="gang_cashier",Decor="RPCore_CashierOwner";
        readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Func<IEnumerable<GangDefinition>> gangs;readonly Func<IEnumerable<GangTurfDefinition>> areas;readonly GangCashierOptions options;readonly Random random=new Random();
        Ped customer;Model model;Blip route;GangCashierClock clock;string profile,id,customerId;int player,token,last,requested,shelfIndex,stage;long elapsed,nextSave,nextAction,pathAt,started,waitUntil,inputAt;bool ready;public bool Active{get;private set;}
        GangCashierAttempt Pending=>Wallet.GangCashier?.Pending;GangTurfDefinition Current=>areas().FirstOrDefault(a=>a.Id==15);
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        static bool Tagged(Entity e,int t)=>e!=null && e.Exists() && e.Handle!=Game.Player.Character.Handle && Function.Call<bool>(Hash.DECOR_EXIST_ON,e,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,e,Decor)==t;
        bool Mark(Entity e){try{if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);return Function.Call<bool>(Hash.DECOR_SET_INT,e,Decor,token);}catch{return false;}}
        bool Safe=>Game.Player.Character!=null && Game.Player.Character.Exists() && Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && Game.Player.Wanted.WantedLevel==0 && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool Input=>Safe && !Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
        bool Owns=>Pending!=null && GangBusinessTransactions.Owns(state(),Current) && GangCrewRules.SameGang(Pending.Gang,state().Gang) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang));
        bool Near(float r)=>Safe && !Game.IsPaused && !InterfaceOptions.ModalOpen && Game.Player.Character.IsOnFoot && Game.Player.Character.Position.DistanceTo(Pos(Pending?.Options.Stand??options.Stand))<=r;
        bool Serving=>Input && stage==3 && Pending.Customer!=null && GangCashierRules.AtTill(true,Game.Player.Character.IsOnFoot,Tagged(customer,token) && customer.IsAlive && customer.IsOnFoot,Game.Player.Character.Position.DistanceTo(Pos(Pending.Options.Stand)),Tagged(customer,token)?customer.Position.DistanceTo(Pos(Pending.Options.Till)):float.MaxValue);
        public GangCashier(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs,Func<IEnumerable<GangTurfDefinition>> areas)
        {this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;this.areas=areas;options=Store.LoadDef("Gangs/Cashier.json",()=>new GangCashierOptions());if(options?.Valid!=true){options=new GangCashierOptions();Log.Write("Invalid cashier config; defaults in memory, original retained.");}}
        public void Controls(Action<string,string,Action> add,Action reopen)
        {
            var p=Pending;if(p==null)return;
            add("Turno caixa / "+(p.Ready?"pronto para salario":p.Failed?"reprovado":"pendente"),"Clientes "+p.Served+"/"+p.Options.Goal+" / salario previsto $"+(p.Served*p.Options.Wage)+" / restante "+Math.Max(0,p.Options.SecondsLimit-p.Progress.Elapsed/1000)+"s. So paga ao concluir todo turno.",null);
            if(!p.Failed)add(Active?"Suspender turno caixa":"Retomar turno caixa","A pe no caixa com posse/seguranca. Preserva clientes, cesta, itens registrados e tempo; cliente atual refaz caminho nas prateleiras.",()=>{if(Active){Suspend();reopen();}else if(!Resume())Notify.Show("Confira caixa a pe, posse, seguranca, atividade e pendencias.");});
            add("GPS: caixa pendente","Ponto do caixa salvo; atendente original preservado.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,p.Options.Stand.X,p.Options.Stand.Y);pool.HideAll();});
            add("Cancelar turno caixa","Gratis, sem salario parcial; progresso confirmado e descartado. Nao altera saldos/bonus anteriores. Pode cancelar sem posse em seguranca.",()=>{if(!Safe || Game.IsPaused || InterfaceOptions.ModalOpen || gate.Owner!=null && gate.Owner!=Owner)return;Suspend();if(!Wallet.GangCashierCancel(p.Id,out _))Notify.Show("Cancelamento nao confirmado; turno conservado.");reopen();});
        }
        public void Append(GangTurfDefinition area,Action<string,string,Action> add)
        {
            if(area.Id!=15)return;var a=GangCashierTransactions.Account(Wallet.GangCashier,state().Gang);
            add("Caixa 24/7 / turnos "+(a?.Completed??0),"Clientes historicos: "+(a?.Customers??0)+" / salarios recebidos: $"+(a?.Earned??0)+". Salario em dinheiro pessoal; nao e bonus de processamento.",null);
            add("GPS: trabalhar no caixa 24/7","Atendente original e preservado. Use espaco livre no lado do caixa.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,options.Stand.X,options.Stand.Y);pool.HideAll();});
            add("Turno caixa / "+options.Goal+" clientes / $"+(options.Goal*options.Wage),"E no caixa inicia atendimento; clientes visitam prateleiras. E registra cada item, E depois recebe pagamento. Prazo"+options.SecondsLimit+"s; E final recebe salario. END suspende.",()=>{if(!Begin(area))Notify.Show("Confira posse da area15, caixa a pe, seguranca, capacidade do cash e atividade/pendencias.");});
        }
        bool Begin(GangTurfDefinition area)
        {if(Active || gate.Owner!=null || !Near(options.StartRadius) || Wallet.Cash>int.MaxValue-options.Goal*options.Wage || !gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang)) || !gate.Acquire(Owner))return false;try{if(!Wallet.GangCashierBegin(state(),area,options,Guid.NewGuid().ToString("N"),out bool saved) || !saved){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}}
        bool Resume(){if(Active || gate.Owner!=null || Wallet.GangCashier?.Valid!=true || Pending==null || Pending.Failed || !Owns || !Wallet.GangCashierOtherFree || !Near(Pending.Options.StartRadius) || !gate.Acquire(Owner))return false;Activate();return true;}
        void Activate(){Active=true;profile=Profile.Id;id=Pending.Id;player=Game.Player.Character.Handle;token=random.Next(1,int.MaxValue);last=Game.GameTime;elapsed=nextAction=0;ready=false;stage=0;clock=Pending.Ready?null:new GangCashierClock(Game.GameTime,Pending.Options.SecondsLimit*1000,Pending.Progress);nextSave=1000;inputAt=500;pool.HideAll();route=MapBlips.Create(Pos(Pending.Options.Stand),"RPCore: turno caixa 24/7",BlipSprite.Standard,BlipColor.Green,.85f);route.ShowRoute=true;Notify.Show("Turno salvo. E no caixa inicia atendimento; END suspende. Atendente original nao e removido.");}
        bool Flush(){if(clock==null || profile!=Profile.Id || Pending?.Id!=id || Pending.Ready || Wallet.GangCashier?.Valid!=true)return true;var a=clock.Progress;var b=Pending.Progress;if(a.Elapsed==b.Elapsed && a.Faulted==b.Faulted)return true;return Wallet.GangCashierUpdate(state(),Current,id,a,out _);}
        void Order()
        {
            var c=Pending.Options;var available=c.Customers.Where(n=>new Model(n).IsInCdImage && new Model(n).IsPed).ToArray();if(available.Length==0){Suspend();return;}
            var o=new GangCashierOrder{Id=Guid.NewGuid().ToString("N"),Model=available[random.Next(available.Length)],Basket=Enumerable.Range(0,random.Next(2,8)).Select(_=>random.Next(c.Products.Count)).ToList(),Route=Enumerable.Range(0,c.Shelves.Count).OrderBy(_=>random.Next()).Take(random.Next(2,5)).ToList()};
            if(!Wallet.GangCashierCustomer(state(),Current,id,o,out bool saved) || !saved){Suspend();Notify.Show("Cesta nao gravada; turno conservado.");}
        }
        void Spawn()
        {
            var o=Pending.Customer;if(model==null){model=new Model(o.Model);if(!model.IsInCdImage || !model.IsPed){Suspend();return;}model.Request();requested=Game.GameTime;}
            if(!model.IsLoaded){if(unchecked((uint)(Game.GameTime-requested))>10000){Suspend();Notify.Show("Cliente nao carregou; cesta salva.");}return;}if(elapsed<nextAction)return;nextAction=elapsed+2000;
            var points=Pending.Options.Spawns.Where(p=>World.GetNearbyPeds(Pos(p),1.5f).Length==0 && World.GetNearbyVehicles(Pos(p),1.5f).Length==0).ToArray();if(points.Length==0)return;
            var point=points[random.Next(points.Length)];customer=World.CreatePed(model,Pos(point));if(customer==null || !customer.Exists()){customer=null;return;}if(!Mark(customer)){customer.MarkAsNoLongerNeeded();customer=null;Suspend();return;}customer.IsPersistent=true;customer.BlockPermanentEvents=true;customer.Weapons.RemoveAll();Function.Call(Hash.SET_PED_MONEY,customer,0);customer.RelationshipGroup=new RelationshipGroup((int)RelationshipGroupHash.CivilianMale);customerId=o.Id;model.MarkAsNoLongerNeeded();model=null;shelfIndex=0;started=elapsed;GoShelf();
        }
        void Walk(JobPoint p,float radius){Function.Call(Hash.CLEAR_PED_TASKS,customer);Function.Call(Hash.TASK_FOLLOW_NAV_MESH_TO_COORD,customer,p.X,p.Y,p.Z,1f,20000,radius,0,p.Heading);}
        void GoShelf(){if(shelfIndex>=Pending.Customer.Route.Count){GoTill();return;}stage=1;Walk(Pending.Options.Shelves[Pending.Customer.Route[shelfIndex]],.5f);pathAt=elapsed+5000;}
        void GoTill(){stage=2;Walk(Pending.Options.Till,.35f);pathAt=elapsed+5000;}
        void ClearCustomer(bool depart){if(Tagged(customer,token)){if(customer.IsInVehicle())customer.MarkAsNoLongerNeeded();else if(depart && customer.IsAlive){customer.BlockPermanentEvents=false;customer.Task.Wander();customer.MarkAsNoLongerNeeded();}else customer.Delete();}customer=null;customerId=null;if(model!=null)model.MarkAsNoLongerNeeded();model=null;stage=0;}
        public void Tick()
        {
            if(!Active)return;
            if(Wallet.GangCashier?.Valid!=true || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Safe || !Owns || !Wallet.GangCashierOtherFree || Game.Player.Character.Position.DistanceTo(Pos(Pending.Options.Stand))>50){Suspend();Notify.Show("Caixa suspenso. Clientes/cesta/tempo salvos; F4 retoma ou cancela.");return;}
            uint d=unchecked((uint)(Game.GameTime-last));last=Game.GameTime;if(!Input){clock?.Tick(Game.GameTime,false);return;}if(d<=2000)elapsed+=d;var p=Pending;var c=p.Options;
            if(p.Ready){GTA.UI.Screen.ShowHelpTextThisFrame("E perto do caixa: concluir turno e receber $"+(p.Served*c.Wage)+" em cash pessoal");return;}
            clock.Tick(Game.GameTime,true);if(elapsed>=nextSave){if(!Flush()){Suspend();return;}nextSave=elapsed+1000;}if(clock.Progress.Elapsed>=c.SecondsLimit*1000){Fail("Prazo esgotado.");return;}
            GTA.UI.Screen.ShowSubtitle("Caixa / "+p.Served+"/"+c.Goal+" clientes / salario previsto $"+(p.Served*c.Wage)+" / "+Math.Max(0,c.SecondsLimit-clock.Progress.Elapsed/1000)+"s / END suspende",100);
            if(!ready){if(Near(2.5f))GTA.UI.Screen.ShowHelpTextThisFrame("E: iniciar atendimento. Pode trabalhar pelo lado externo do balcao, sem entrar no posto do atendente.");return;}
            if(stage==4){if(Tagged(customer,token) && customer.IsAlive && customer.IsOnFoot && customer.Position.DistanceTo(Pos(c.Exit))>2 && elapsed<waitUntil)return;ClearCustomer(true);nextAction=elapsed+1000;return;}
            if(p.Customer==null){if(elapsed>=nextAction)Order();return;}
            if(!Tagged(customer,token)){Spawn();return;}
            if(!customer.IsAlive){Fail("Cliente indisponivel.");return;}if(!customer.IsOnFoot){Suspend();return;}
            if(stage<3 && elapsed-started>90000){Suspend();Notify.Show("Cliente nao conseguiu chegar. Cesta/registro/tempo salvos; libere caminho e retome.");return;}
            if(stage==1){var q=c.Shelves[p.Customer.Route[shelfIndex]];if(customer.Position.DistanceTo(Pos(q))<=.8f){Function.Call(Hash.CLEAR_PED_TASKS,customer);customer.Heading=q.Heading;Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE,customer,"WORLD_HUMAN_STAND_IMPATIENT",0,true);stage=5;waitUntil=elapsed+random.Next(1500,3001);}else if(elapsed>=pathAt){Walk(q,.5f);pathAt=elapsed+5000;}return;}
            if(stage==5){if(elapsed>=waitUntil){shelfIndex++;GoShelf();}return;}
            if(stage==2){if(customer.Position.DistanceTo(Pos(c.Till))<=.65f){Function.Call(Hash.CLEAR_PED_TASKS,customer);customer.Heading=c.Till.Heading;stage=3;inputAt=elapsed+300;}else if(elapsed>=pathAt){Walk(c.Till,.35f);pathAt=elapsed+5000;}return;}
            if(stage==3 && customer.Position.DistanceTo(Pos(c.Till))>1){GoTill();return;}
            if(Serving){var o=p.Customer;string item=o.Scanned<o.Basket.Count?c.Products[o.Basket[o.Scanned]].Name:"TODOS REGISTRADOS";int total=o.Basket.Sum(i=>c.Products[i].Price);GTA.UI.Screen.ShowSubtitle("Cliente "+(p.Served+1)+" / itens "+o.Scanned+"/"+o.Basket.Count+" / "+item+" / total $"+total,100);GTA.UI.Screen.ShowHelpTextThisFrame(o.Scanned<o.Basket.Count?"E: registrar proximo item":"E: receber pagamento do cliente (salario ao concluir o turno)");}
        }
        public void Interact()
        {
            if(!Active || !Input || !Owns || !Wallet.GangCashierOtherFree || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || elapsed<inputAt)return;
            if(Pending.Ready){if(Near(2.5f) && Wallet.GangCashierComplete(state(),Current,id,out bool saved)){Suspend();Notify.Show(saved?"Turno concluido! Salario creditado em dinheiro pessoal; confira F11/carteira.":"Turno ja pago; sem repeticao.");}return;}
            if(!ready){if(Near(2.5f)){ready=true;Game.Player.Character.Heading=Pending.Options.Stand.Heading;inputAt=elapsed+500;}return;}
            if(!Serving || Pending.Customer.Id!=customerId)return;if(!Flush()){Suspend();return;}var o=Pending.Customer;
            if(o.Scanned<o.Basket.Count){if(Wallet.GangCashierScan(state(),Current,id,o.Id,o.Scanned,out bool saved) && saved){inputAt=elapsed+450;Function.Call(Hash.PLAY_SOUND_FRONTEND,-1,"SELECT","HUD_FRONTEND_DEFAULT_SOUNDSET",true);}return;}
            if(!Wallet.GangCashierPay(state(),Current,id,o.Id,out bool paid) || !paid){Suspend();return;}Function.Call(Hash.PLAY_SOUND_FRONTEND,-1,"PURCHASE","HUD_LIQUOR_STORE_SOUNDSET",true);inputAt=elapsed+500;
            if(Pending.Ready){clock=null;ClearCustomer(true);Notify.Show("Cinco clientes confirmados. E no caixa conclui e recebe o salario.");return;}stage=4;Walk(Pending.Options.Exit,.5f);waitUntil=elapsed+12000;
        }
        void Fail(string reason){if(clock!=null){clock.Progress.Faulted=true;Flush();}Suspend();Notify.Show("Turno reprovado: "+reason+" Sem salario parcial; cancele pendencia no F4.");}
        public void Suspend(){try{if(Active)Flush();}finally{clock=null;ClearCustomer(false);if(route!=null && route.Exists())route.Delete();route=null;Active=false;profile=id=null;gate.Release(Owner);}}
        public void Dispose(){Suspend();}
    }
}
