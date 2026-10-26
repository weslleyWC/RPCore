using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
namespace RPCore
{
    internal sealed class GangMechanic
    {
        const string Owner="gang_mechanic",Decor="RPCore_MechanicOwner",Anim="mini@repair",Clip="fixing_a_ped";
        enum Stage{Waiting,Loading,Arriving,Repairing,Departing,Ready}
        sealed class Released{public Vehicle Car;public Ped Driver;public int Token;}
        readonly List<Released> releasing=new List<Released>();
        readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Func<IEnumerable<GangDefinition>> gangs;readonly Func<IEnumerable<GangTurfDefinition>> areas;readonly GangMechanicOptions options;readonly Random random=new Random();
        Vehicle car;Ped driver,animOwner;Model vehicleModel,driverModel;Blip route;Stage stage;GangMechanicSkill skill;bool hood,animated;string profile,id;int player,token,index,last;long phaseMs,warningMs,nextPrepareMs;public bool Active{get;private set;}
        GangMechanicAttempt Pending=>Wallet.GangMechanic?.Pending;
        GangTurfDefinition Current=>areas().FirstOrDefault(a=>a.Id==2);
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        bool Safe=>Game.Player.Character!=null && Game.Player.Character.Exists() && Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && Game.Player.Wanted.WantedLevel==0 && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool Input=>Safe && !Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
        bool Owns=>Pending!=null && GangBusinessTransactions.Owns(state(),Current) && GangCrewRules.SameGang(state().Gang,Pending.Gang) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang));
        static bool Tagged(Entity e,int value)=>e!=null && e.Exists() && e.Handle!=Game.Player.Character.Handle && Function.Call<bool>(Hash.DECOR_EXIST_ON,e,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,e,Decor)==value;
        bool Mark(Entity e){try{if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);return Function.Call<bool>(Hash.DECOR_SET_INT,e,Decor,token);}catch{return false;}}
        bool OwnCar=>Tagged(car,token);bool OwnDriver=>Tagged(driver,token);
        bool PlayerIn(Vehicle v)=>v!=null && v.Exists() && (Game.Player.Character.IsInVehicle(v) || v.Occupants.Any(p=>p!=null && p.Exists() && Function.Call<bool>(Hash.IS_PED_A_PLAYER,p)));
        bool At(JobPoint p,int radius)=>Safe && !Game.IsPaused && !InterfaceOptions.ModalOpen && Game.Player.Character.IsOnFoot && Game.Player.Character.Position.DistanceTo(Pos(p))<=radius;
        bool Front=>OwnCar && OwnDriver && driver.IsAlive && car.IsDriveable && car.Speed<.5f && !PlayerIn(car) && car.Driver==driver && Game.Player.Character.IsOnFoot && car.Position.DistanceTo(Pos(Pending.Options.Bay))<=8 && Game.Player.Character.Position.DistanceTo(car.GetOffsetPosition(new Vector3(0,2.5f,0)))<=2.8f;
        public GangMechanic(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs,Func<IEnumerable<GangTurfDefinition>> areas)
        {this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;this.areas=areas;options=Store.LoadDef("Gangs/Mechanic.json",()=>new GangMechanicOptions());if(options?.Valid!=true){options=new GangMechanicOptions();Log.Write("Invalid gang mechanic config; defaults in memory, original retained.");}}
        public void Controls(Action<string,string,Action> add,Action reopen)
        {
            var p=Pending;if(p==null)return;
            add("Oficina pendente / "+p.Repaired+"/"+p.Options.Goal,"Carros concluidos salvos. Reparo do carro atual reinicia ao retomar, sem refazer clientes anteriores.",null);
            add(Active?"Suspender oficina":"Retomar oficina",p.Ready?"Todos reparos salvos. A pe na oficina, retome e E confirma bonus.":"A pe na oficina e com posse. Recria somente o proximo carro, com mesmo modelo/opcoes; acertos parciais reiniciam.",()=>{if(Active){Suspend();reopen();}else if(!Resume())Notify.Show("Confira posse, oficina a pe, seguranca, veiculos em liberacao e atividade/contratos.");});
            add("GPS: oficina pendente","Local salvo do contrato.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,p.Options.Shop.X,p.Options.Shop.Y);pool.HideAll();});
            add("Cancelar contrato da oficina","Gratis. Sem premio parcial; pode cancelar sem posse/filiacao em seguranca. Bonus anterior conserva vencimento.",()=>{if(!Safe || Game.IsPaused || InterfaceOptions.ModalOpen || gate.Owner!=null && gate.Owner!=Owner || !Wallet.GangMechanicCancel(p.Id,out _)){Notify.Show("Cancelamento nao confirmado; contrato conservado.");return;}Suspend();reopen();});
        }
        public void Append(GangTurfDefinition area,Action<string,string,Action> add)
        {
            if(area.Id!=2)return;var a=GangMechanicTransactions.Account(Wallet.GangMechanic,state().Gang);var utc=DateTime.UtcNow;
            string status=GangMechanicTransactions.Multiplier(Wallet.GangMechanic,state().Gang,2,utc)==2?"ATIVO / "+Math.Ceiling((a.ExpiresUtcTicks-utc.Ticks)/(double)TimeSpan.TicksPerMinute)+"min":"inativo";
            add("Oficina / bonus2X: "+status,"Contratos concluidos: "+(a?.Completed??0)+". Acelera automatico da area2 por tempo real,85% ao caixa igual. Nao liga automatico.",null);
            add("GPS: oficina territorial","Vaga livre; a pe a ate"+options.ReturnRadius+"m para iniciar.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,options.Shop.X,options.Shop.Y);pool.HideAll();});
            add("Atender oficina / "+options.Goal+" carros","Clientes chegam dirigindo. Na frente do carro, E abre capo; acerte E na faixa verde"+options.RequiredHits+" vezes. Aguarde saida, repita e E final libera2X/"+options.BoostMinutes+"min. Gratis.",()=>{if(!Begin(area))Notify.Show("Exige posse da area2, a pe na oficina, seguranca e nenhuma atividade/pendencia.");});
        }
        bool Begin(GangTurfDefinition area)
        {
            if(Active || gate.Owner!=null || releasing.Count>=8 || !At(options.Shop,options.ReturnRadius) || !gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang)))return false;
            var models=options.Models.Where(n=>new Model(n).IsInCdImage && new Model(n).IsCar).ToList();var d=new Model(options.DriverModel);if(models.Count==0 || !d.IsInCdImage || !d.IsPed || !gate.Acquire(Owner))return false;
            try{var chosen=Enumerable.Range(0,options.Goal).Select(_=>models[random.Next(models.Count)]).ToList();if(!Wallet.GangMechanicBegin(state(),area,options,chosen,Guid.NewGuid().ToString("N"),out bool changed) || !changed){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}
        }
        bool Resume()
        {
            if(Active || gate.Owner!=null || releasing.Count>=8 || Wallet.GangMechanic?.Valid!=true || !Owns || !Wallet.GangMechanicOtherFree || !At(Pending.Options.Shop,Pending.Options.ReturnRadius) || !gate.Acquire(Owner))return false;
            Activate();return true;
        }
        void Activate(){Active=true;profile=Profile.Id;player=Game.Player.Character.Handle;id=Pending.Id;last=Game.GameTime;Set(Pending.Ready?Stage.Ready:Stage.Waiting);pool.HideAll();Route(Pos(Pending.Options.Shop));Notify.Show("Oficina ativa. Deixe a vaga livre e aguarde o cliente. E abre capo/acerta barra; END suspende.");}
        void Set(Stage value){stage=value;phaseMs=0;warningMs=0;nextPrepareMs=2000;}
        string Status=>stage==Stage.Waiting?"aguardando cliente":stage==Stage.Loading?"preparando cliente":stage==Stage.Arriving?"cliente a caminho":stage==Stage.Repairing?"reparando":stage==Stage.Departing?"cliente saindo":"confirmar contrato";
        void Route(Vector3 p){if(route!=null && route.Exists())route.Delete();route=MapBlips.Create(p,"RPCore: oficina territorial",BlipSprite.Standard,BlipColor.Yellow,.8f);route.ShowRoute=true;}
        bool Free(Vector3 p,float radius)=>World.GetNearbyVehicles(p,radius).All(v=>v==null || !v.Exists());
        void Prepare()
        {
            var c=Pending.Options;if(!Free(Pos(c.Spawn),4) || !Free(Pos(c.Bay),3.5f)){if(phaseMs-warningMs>=15000 || warningMs==0){Notify.Show("Vaga ou ponto de chegada bloqueado. Afaste seu carro da vaga; aguardando sem remover outros veiculos.");warningMs=phaseMs;}return;}
            index=Pending.Repaired;vehicleModel=new Model(Pending.Models[index]);driverModel=new Model(c.DriverModel);
            if(!vehicleModel.IsInCdImage || !vehicleModel.IsCar || !driverModel.IsInCdImage || !driverModel.IsPed){Suspend();Notify.Show("Modelo salvo indisponivel. Contrato conservado; confira configuracao.");return;}
            vehicleModel.Request();driverModel.Request();Set(Stage.Loading);
        }
        void Spawn()
        {
            var c=Pending.Options;if(!vehicleModel.IsLoaded || !driverModel.IsLoaded){if(phaseMs>10000){Suspend();Notify.Show("Modelos nao carregaram. Contrato conservado para retomada.");}return;}
            if(!Free(Pos(c.Spawn),4) || !Free(Pos(c.Bay),3.5f)){ReleaseModels();Set(Stage.Waiting);return;}
            token=random.Next(1,int.MaxValue);car=World.CreateVehicle(vehicleModel,Pos(c.Spawn),c.Spawn.Heading);
            if(car==null || !car.Exists()){car=null;ReleaseModels();Set(Stage.Waiting);return;}
            if(!Mark(car)){car.MarkAsNoLongerNeeded();car=null;Suspend();return;}car.IsPersistent=true;JobVehicles.Reserve(car);
            driver=World.CreatePed(driverModel,car.GetOffsetPosition(new Vector3(2,0,0)));
            if(driver==null || !driver.Exists()){driver=null;Suspend();return;}
            if(!Mark(driver)){driver.MarkAsNoLongerNeeded();driver=null;Suspend();return;}
            driver.IsPersistent=true;driver.BlockPermanentEvents=true;Function.Call(Hash.SET_PED_MONEY,driver,0);car.EngineHealth=450;car.BodyHealth=550;car.PetrolTankHealth=800;
            Function.Call(Hash.SET_PED_INTO_VEHICLE,driver,car,-1);Function.Call(Hash.SET_DRIVER_ABILITY,driver,1f);Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS,driver,0f);
            JobActors.Drive(driver,car,Pos(c.Bay));ReleaseModels();Set(Stage.Arriving);Route(car.Position);Notify.Show("Cliente a caminho. Aguarde a chegada sem ocupar a vaga da oficina.");
        }
        void ReleaseModels(){if(vehicleModel!=null)vehicleModel.MarkAsNoLongerNeeded();if(driverModel!=null)driverModel.MarkAsNoLongerNeeded();vehicleModel=driverModel=null;}
        void StopAnim(){if(animated && animOwner!=null && animOwner.Exists())Function.Call(Hash.STOP_ANIM_TASK,animOwner,Anim,Clip,2f);animated=false;animOwner=null;}
        bool Animate()
        {Function.Call(Hash.REQUEST_ANIM_DICT,Anim);if(!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED,Anim))return false;if(!animated){animOwner=Game.Player.Character;Function.Call(Hash.TASK_PLAY_ANIM,animOwner,Anim,Clip,4f,-4f,-1,1,0f,false,false,false);animated=true;}return true;}
        void Meter()
        {
            const float left=.70f,width=.24f,y=.80f;Function.Call(Hash.DRAW_RECT,left+width/2,y,width,.024f,32,35,40,220);
            Function.Call(Hash.DRAW_RECT,left+width*.5f,y,width*.084f,.024f,65,205,125,255);
            Function.Call(Hash.DRAW_RECT,left+width*(float)skill.Cursor,y,.0035f,.035f,255,255,255,255);
            GTA.UI.Screen.ShowHelpTextThisFrame((GangMechanicSkill.Green(skill.Cursor)?"E AGORA / ":"Acerte E na faixa verde / ")+skill.Hits+"/"+Pending.Options.RequiredHits+" acertos / erros "+skill.Misses);
        }
        public void Tick()
        {
            Drain();if(!Active)return;
            if(Wallet.GangMechanic?.Valid!=true || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Safe || !Owns || !Wallet.GangMechanicOtherFree || Game.Player.Character.Position.DistanceTo(Pos(Pending.Options.Shop))>150)
            {Suspend();Notify.Show("Oficina suspensa. Reparos concluidos salvos; F4 retoma ou cancela.");return;}
            uint delta=unchecked((uint)(Game.GameTime-last));last=Game.GameTime;
            if(!Input){StopAnim();skill?.Step(Game.GameTime,false);return;}if(delta<=2000)phaseMs+=delta;
            var c=Pending.Options;GTA.UI.Screen.ShowSubtitle("Oficina / "+Pending.Repaired+"/"+c.Goal+" carros / "+Status+" / END suspende",100);
            if(stage==Stage.Waiting){if(phaseMs>=nextPrepareMs){nextPrepareMs=phaseMs+3000;Prepare();}return;}if(stage==Stage.Loading){Spawn();return;}
            if(stage==Stage.Ready){if(At(c.Shop,c.ReturnRadius))GTA.UI.Screen.ShowHelpTextThisFrame("E: concluir contrato e liberar bonus2X");return;}
            if(!OwnCar || !OwnDriver || car.IsDead || driver.IsDead || !car.IsDriveable || PlayerIn(car) || car.Driver!=driver){Suspend();Notify.Show("Cliente/veiculo indisponivel ou ocupado pelo jogador. Reparos anteriores conservados.");return;}
            if(route!=null && route.Exists())route.Position=car.Position;
            if(stage==Stage.Arriving)
            {
                if(car.Position.DistanceTo(Pos(c.Bay))<=5.5f && car.Speed<1.5f){Function.Call(Hash.CLEAR_PED_TASKS,driver);Function.Call(Hash.SET_VEHICLE_HANDBRAKE,car,true);car.IsEngineRunning=false;car.EngineHealth=180;hood=false;skill=null;Set(Stage.Repairing);Notify.Show("Cliente chegou. A pe na frente do carro: E abre o capo.");}
                else if(phaseMs>=c.ArrivalSeconds*1000){Suspend();Notify.Show("Cliente nao conseguiu chegar. Contrato salvo; confira acesso/vaga e retome.");}return;
            }
            if(stage==Stage.Departing)
            {if(car.Position.DistanceTo(Pos(c.Shop))>=30 || phaseMs>=c.DepartureSeconds*1000){Retire();Set(Pending.Ready?Stage.Ready:Stage.Waiting);Route(Pos(c.Shop));}return;}
            if(!Front){StopAnim();skill?.Step(Game.GameTime,false);GTA.UI.Screen.ShowHelpTextThisFrame("A pe, aproxime-se da frente do carro parado na oficina.");return;}
            if(!hood){GTA.UI.Screen.ShowHelpTextThisFrame("E: abrir o capo do cliente");return;}
            if(!Animate()){skill.Step(Game.GameTime,false);if(phaseMs>=10000){Suspend();Notify.Show("Animacao indisponivel; contrato conservado.");}return;}
            skill.Step(Game.GameTime,true);Meter();
        }
        public void Interact()
        {
            if(!Active || !Input || !Owns || !Wallet.GangMechanicOtherFree || profile!=Profile.Id || player!=Game.Player.Character.Handle || Pending?.Id!=id || gate.Owner!=Owner)return;
            var p=Pending;
            if(stage==Stage.Ready && p.Ready){if(At(p.Options.Shop,p.Options.ReturnRadius) && Wallet.GangMechanicComplete(state(),Current,id,DateTime.UtcNow,out bool changed)){Suspend();Notify.Show(changed?"Oficina concluida! Automatico2X na area2 por"+p.Options.BoostMinutes+"min reais. Ative o processamento separadamente.":"Contrato ja confirmado; bonus nao repetido.");}return;}
            if(stage!=Stage.Repairing || !Front)return;
            if(!hood){Function.Call(Hash.SET_VEHICLE_DOOR_OPEN,car,4,false,false);hood=true;skill=new GangMechanicSkill(Game.GameTime);phaseMs=0;return;}
            if(!animated)return;skill.Step(Game.GameTime,true);if(!skill.Press(out bool hit))return;
            if(!hit){Notify.Show("Fora da faixa verde. Tente novamente; sem perda financeira.");return;}if(skill.Hits<p.Options.RequiredHits)return;
            if(!Wallet.GangMechanicRepair(state(),Current,id,index,out bool repaired) || !repaired){Suspend();Notify.Show("Reparo nao confirmado; contrato conservado.");return;}
            StopAnim();car.Repair();car.EngineHealth=car.BodyHealth=car.PetrolTankHealth=1000;Function.Call(Hash.SET_VEHICLE_DOOR_SHUT,car,4,false);Function.Call(Hash.SET_VEHICLE_HANDBRAKE,car,false);car.IsEngineRunning=true;hood=false;
            JobActors.Drive(driver,car,Pos(p.Options.Exit));Set(Stage.Departing);Notify.Show("Reparo salvo / "+Pending.Repaired+"/"+p.Options.Goal+". Aguarde o cliente sair.");
        }
        void Retire(){StopAnim();if(car!=null || driver!=null)releasing.Add(new Released{Car=car,Driver=driver,Token=token});car=null;driver=null;token=0;skill=null;hood=false;Drain();}
        void Drain()
        {
            for(int i=releasing.Count-1;i>=0;i--){var r=releasing[i];bool ownCar=Tagged(r.Car,r.Token),ownDriver=Tagged(r.Driver,r.Token);if(ownCar && PlayerIn(r.Car))continue;
                if(ownCar){Function.Call(Hash.SET_VEHICLE_HANDBRAKE,r.Car,false);if(ownDriver && r.Driver.IsAlive && r.Car.Driver==r.Driver && r.Car.IsDriveable){r.Car.IsEngineRunning=true;Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER,r.Driver,r.Car,8f,786603);}r.Car.MarkAsNoLongerNeeded();}JobVehicles.Release(r.Car);
                if(ownDriver)r.Driver.MarkAsNoLongerNeeded();releasing.RemoveAt(i);}
        }
        public void Suspend(){Retire();ReleaseModels();if(route!=null && route.Exists())route.Delete();route=null;Active=false;profile=id=null;gate.Release(Owner);}
        public void Dispose(){Suspend();foreach(var r in releasing){if(Tagged(r.Car,r.Token))r.Car.MarkAsNoLongerNeeded();if(Tagged(r.Driver,r.Token))r.Driver.MarkAsNoLongerNeeded();JobVehicles.Release(r.Car);}releasing.Clear();}
    }
}
