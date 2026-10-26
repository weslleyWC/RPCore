using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
namespace RPCore
{
    internal sealed class GangDriveBy : IDisposable
    {
        const string Owner="gang_driveby",Decor="RPCore_DriveByOwner";
        sealed class Retired{public Vehicle Car;public Ped[] Crew;public int Token,Last;}
        readonly List<Retired> retired=new List<Retired>();readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Action<GangState> save;readonly Func<IEnumerable<GangDefinition>> gangs;
        readonly GangTurfOptions areas;readonly GangDriveByOptions options;readonly NativeMenu menu;readonly Random random=new Random();readonly Ped[] crew=new Ped[4];
        GangDriveByRun run;GangDriveByClock clock;Model[] models;Vehicle car;Blip blip;Ped target;string profile,recoveryAttempt,scheduleProfile;int token,requested,last,schedule,next,taskAt;bool preparing,eligibleBefore;
        GangDriveByData Book=>state().DriveBys;public bool Active=>preparing || clock!=null;
        static Vector3 Pos(GangSpot p)=>new Vector3(p.X,p.Y,p.Z);
        static bool Tagged(Entity e,int t)=>e!=null && e.Exists() && e.Handle!=Game.Player.Character.Handle && Function.Call<bool>(Hash.DECOR_EXIST_ON,e,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,e,Decor)==t;
        bool Mark(Entity e){if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);return Function.Call<bool>(Hash.DECOR_SET_INT,e,Decor,token);}
        public GangDriveBy(ShiftGate gate,ObjectPool pool,Func<GangState> state,Action<GangState> save,Func<IEnumerable<GangDefinition>> gangs)
        {
            this.gate=gate;this.pool=pool;this.state=state;this.save=save;this.gangs=gangs;
            options=Store.LoadDef("Gangs/DriveBy.json",()=>new GangDriveByOptions());if(options?.Valid!=true){options=new GangDriveByOptions();Log.Write("Invalid drive-by options retained; defaults used in memory.");}
            areas=Store.LoadDef("Gangs/Territories.json",GangTurfOptions.Defaults);if(areas?.Valid!=true)areas=GangTurfOptions.Defaults();
            menu=new NativeMenu("Gangues","EMBOSCADAS RIVAIS DE CARRO");pool.Add(menu);Visuals.Register(menu,"gangs");ResetSchedule();
        }
        void Add(string title,string description,Action action,bool enabled=true){var item=new NativeItem(title,description){Enabled=enabled};item.Activated+=(s,e)=>{try{action();}catch(Exception ex){Close();Log.Error("gang drive-by menu",ex);Notify.Show("Acao nao concluida; dados gravados conservados.");}};menu.Add(item);}
        public void Open()
        {
            pool.HideAll();menu.Clear();if(Book!=null && !Book.Valid){Add("Dados de emboscada protegidos","Confira GangState.json/backup.",()=>{});menu.Visible=true;return;}
            Add("Emboscadas: "+(Book?.Enabled==true?"LIGADAS":"DESLIGADAS"),"OPCIONAL: inimigos podem atirar em voce fora de HQs/territorios. Sem imunidade policial. Intervalo ativo5-15min/85% por padrao. Comeca desligado nos saves antigos.",()=>{bool enable=Book?.Enabled!=true;if(GangDriveByTransactions.Configure(state(),enable,save,out _)){if(!enable)Close();ResetSchedule();Open();}});
            Add("Solicitar emboscada agora (teste)","Ataque REAL: exige opcao ligada, rival hostil/Guerra, rua fora de HQ40m/territorios, zero procurado e nenhuma atividade/pendencia. Um carro, motorista e tres atiradores; sem recompensa.",()=>{if(Begin())menu.Visible=false;else Notify.Show("Confira opcao, rival hostil, rua, afastamento de HQ/territorios e pendencias.");},!Active && Book?.Enabled==true);
            if(Active)Add("Encerrar encontro atual","Interrompe este evento e recolhe apenas recursos proprios. Nao concede dinheiro ou reputacao.",()=>{Close();Open();});
            if(!Active && Book?.Pending!=null)Add("Encerrar registro interrompido","Reload nao recria o ataque. Marca apenas este registro como interrompido.",()=>{GangDriveByTransactions.Finish(state(),Book.Pending.Id,"interrupted",save,out _);Open();});
            Add("Encontros: "+(Book?.Started??0)+" / repelidos: "+(Book?.Repelled??0),"Fugas: "+(Book?.Escaped??0)+" / interrompidos: "+(Book?.Interrupted??0)+" / mortes: "+(Book?.Deaths??0)+". Sem alterar donos, saldos ou relacoes diplomaticas.",()=>{});
            foreach(var h in (Book?.History??new List<GangDriveByRecord>()).AsEnumerable().Reverse().Take(8)){var r=h;Add(r.Rival+" / "+Label(r.Outcome),"Alvo: "+r.Gang,()=>{});}
            Add("Fechar","Voltar ao jogo.",()=>menu.Visible=false);menu.Visible=true;
        }
        static string Label(string s)=>s=="repelled"?"repelida":s=="escaped"?"fuga":s=="death"?"morte":"interrompida";
        bool Outdoor(Ped p)=>p!=null && p.Exists() && GangDriveByRules.Outdoor(p.IsHuman,!p.IsDead,Function.Call<int>(Hash.GET_INTERIOR_FROM_ENTITY,p)!=0,p.IsInAir,p.IsInWater);
        bool OutsideZones(Ped p)=>areas.Areas.All(a=>GangDriveByRules.Outside(p.Position.DistanceTo(Pos(a.Center)),areas.Radius)) && gangs().Where(g=>GangPresenceRules.Position(GangPresenceRules.HQ(g))).All(g=>GangDriveByRules.Outside(p.Position.DistanceTo(g.Position),40));
        bool WorldFree=>state().Turf==null || state().Turf.Valid && state().Turf.Pending==null;
        bool Eligible=>Book?.Enabled==true && Book.Valid && Book.Pending==null && GangWarRules.Free(state()) && state().Warfare?.Alert==null && (state().Warfare==null || state().Warfare.Valid) && Wallet.GangWarEconomyFree && (Wallet.GangDiplomacy==null || Wallet.GangDiplomacy.Valid) && gate.Owner==null && Outdoor(Game.Player.Character) && OutsideZones(Game.Player.Character) && Game.Player.Wanted.WantedLevel==0 && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && !Game.IsPaused && !InterfaceOptions.ModalOpen && !GangLoyaltyRules.Unaffiliated(state().Gang) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang));
        bool Begin()
        {
            if(Active || !Eligible || retired.Count!=0)return false;
            var choices=gangs().Where(g=>GangDiplomacyRules.Pair(state().Gang,g.Name) && GangWarRules.Hostile(GangDiplomacyRules.Attitude(Wallet.GangDiplomacy,state().Gang,g))).Select(g=>new{Gang=g,Models=(g.PedModels??Array.Empty<string>()).Where(GangPresenceRules.ModelName).Where(n=>{var m=new Model(n);return m.IsInCdImage && m.IsPed;}).ToArray()}).Where(g=>g.Models.Length>0).ToArray();
            if(choices.Length==0)return false;var rival=choices[random.Next(choices.Length)];var c=CharacterRules.Copy(options);var vehicle=new Model(c.VehicleModel);if(!vehicle.IsInCdImage || !vehicle.IsCar || !gate.Acquire(Owner))return false;
            profile=Profile.Id;target=Game.Player.Character;run=new GangDriveByRun{Id=Guid.NewGuid().ToString("N"),Gang=state().Gang,Rival=rival.Gang.Name,Options=c,Models=Enumerable.Range(0,4).Select(i=>rival.Models[i%rival.Models.Length]).ToArray()};token=random.Next(1,int.MaxValue);models=run.Models.Select(n=>new Model(n)).Append(vehicle).ToArray();foreach(var m in models)m.Request();preparing=true;requested=Game.GameTime;pool.HideAll();return true;
        }
        GangSpot SpawnPoint()
        {
            GangSpot fallback=null;
            for(int i=0;i<20;i++){
                double a=random.NextDouble()*Math.PI*2;int radius=random.Next(90,141);var q=target.Position+new Vector3((float)Math.Cos(a)*radius,(float)Math.Sin(a)*radius,0);var node=new OutputArgument();var heading=new OutputArgument();
                if(!Function.Call<bool>(Hash.GET_CLOSEST_VEHICLE_NODE_WITH_HEADING,q.X,q.Y,q.Z,node,heading,1,3f,0))continue;var p=node.GetResult<Vector3>();
                if(!GangDriveByRules.Spawn(p.DistanceTo(target.Position),p.Z-target.Position.Z) || !JobActors.Free(p,5) || !GangPresence.Space(p))continue;
                float floor;if(!World.GetGroundHeight(p+new Vector3(0,0,3),out floor,GetGroundHeightMode.Normal) || Math.Abs(floor-p.Z)>2)continue;
                var spot=new GangSpot{X=p.X,Y=p.Y,Z=floor+.1f,Heading=heading.GetResult<float>()};if(!Function.Call<bool>(Hash.IS_SPHERE_VISIBLE,p.X,p.Y,p.Z,4f))return spot;if(fallback==null)fallback=spot;
            }return fallback;
        }
        void Spawn()
        {
            var point=SpawnPoint();if(point==null){Close();Notify.Show("Nenhuma rua livre para emboscada; encontro nao iniciado.");return;}
            car=World.CreateVehicle(models.Last(),Pos(point),point.Heading);if(car==null || !car.Exists())throw new InvalidOperationException("Drive-by vehicle unavailable.");if(!Mark(car)){car.MarkAsNoLongerNeeded();car=null;throw new InvalidOperationException("Drive-by vehicle ownership unavailable.");}
            if(car.PassengerCapacity<3)throw new InvalidOperationException("Drive-by vehicle requires three passenger seats.");car.IsPersistent=true;Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY,car);car.IsEngineRunning=true;JobVehicles.Reserve(car);
            for(int i=0;i<4;i++){
                var p=World.CreatePed(models[i],car.Position+new Vector3(0,0,2));crew[i]=p;if(p==null || !p.Exists())throw new InvalidOperationException("Drive-by NPC unavailable.");if(!p.IsHuman || !Mark(p)){p.Delete();crew[i]=null;throw new InvalidOperationException("Drive-by NPC ownership unavailable.");}
                p.IsPersistent=true;p.BlockPermanentEvents=true;Function.Call(Hash.SET_PED_KEEP_TASK,p,true);p.RelationshipGroup=new RelationshipGroup((int)RelationshipGroupHash.CivilianMale);p.Weapons.RemoveAll();p.Accuracy=45;Function.Call(Hash.SET_PED_MONEY,p,0);Function.Call(Hash.SET_PED_DROPS_WEAPONS_WHEN_DEAD,p,false);Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES,p,(int)CombatAttributes.CanCommandeerVehicles,false);p.SetIntoVehicle(car,(VehicleSeat)(i-1));if(!p.IsInVehicle(car))throw new InvalidOperationException("Drive-by seat unavailable.");if(i>0){Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES,p,2,true);Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES,p,3,false);p.Weapons.Give(WeaponHash.MicroSMG,350,true,true);}
            }
            if(!GangDriveByTransactions.Begin(state(),run,save,out _))throw new InvalidOperationException("Drive-by event not saved.");
            foreach(var m in models)m.MarkAsNoLongerNeeded();models=null;preparing=false;clock=new GangDriveByClock(run.Options,Game.GameTime);taskAt=0;blip=car.AddBlip();blip.Color=BlipColor.Red;blip.Name="RPCore: emboscada "+run.Rival;
            Notify.Show("Emboscada rival: "+run.Rival+"! Afaste-se ou neutralize os atacantes. END encerra este teste.");
        }
        bool SafeActive=>profile==Profile.Id && target==Game.Player.Character && Outdoor(target) && Book?.Valid==true && Book.Enabled && Book.Pending?.Id==run?.Id && GangCrewRules.SameGang(state().Gang,run.Gang) && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && !InterfaceOptions.ModalOpen && OutsideZones(target) && gate.Owner==Owner && WorldFree && state().Loyalty?.Pending==null && state().Strength?.Pending==null && state().Driving?.Pending==null && state().Warfare?.Alert==null && Wallet.GangWarEconomyFree;
        public void Tick()
        {
            Drain();
            if(!Active){
                if(scheduleProfile!=Profile.Id)ResetSchedule();
                if(Book?.Valid==true && Book.Pending!=null && recoveryAttempt!=Profile.Id+"|"+Book.Pending.Id){recoveryAttempt=Profile.Id+"|"+Book.Pending.Id;GangDriveByTransactions.Finish(state(),Book.Pending.Id,"interrupted",save,out _);}
                bool eligible=Eligible && !pool.AreAnyVisible;int delta=GangWarRules.Delta(last,Game.GameTime,eligible && eligibleBefore);last=Game.GameTime;eligibleBefore=eligible;
                if(!eligible)return;schedule+=delta;if(schedule>=next){ResetSchedule();if(random.Next(100)<options.Chance)Begin();}return;
            }
            if(profile==Profile.Id && target==Game.Player.Character && target.IsDead){End("death");return;}
            if(preparing){
                if(gate.Owner!=Owner || profile!=Profile.Id || target!=Game.Player.Character || !Outdoor(target) || !OutsideZones(target) || Book?.Enabled!=true || Book.Valid!=true || !Wallet.GangWarEconomyFree || !WorldFree || !GangCrewRules.SameGang(state().Gang,run.Gang) || state().Loyalty?.Pending!=null || state().Strength?.Pending!=null || state().Driving?.Pending!=null || state().Warfare?.Alert!=null || Game.IsMissionActive || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) || Game.Player.Wanted.WantedLevel!=0 || unchecked(Game.GameTime-requested)>10000){Close();return;}
                if(Game.IsPaused)return;if(models.All(m=>m.IsLoaded))Spawn();return;
            }
            if(!SafeActive){Close();return;}
            if(!Tagged(car,token) || crew.Any(p=>!Tagged(p,token))){Close();return;}
            if(Connected(car) || car.Occupants.Any(p=>!crew.Any(c=>Tagged(c,token) && c==p))){Close();return;}
            bool driver=!crew[0].IsDead && !car.IsDead && car.Driver==crew[0];bool gunner=crew.Skip(1).Any(p=>!p.IsDead && p.IsInVehicle(car));
            var stage=clock.Stage;clock.Tick(Game.GameTime,!Game.IsPaused,car.Position.DistanceTo(target.Position),driver,gunner);
            if(clock.Stage==GangDriveByStage.Finished){End(clock.Repelled?"repelled":"escaped");return;}
            if(Game.IsPaused)return;
            if(stage!=clock.Stage){taskAt=0;if(clock.Stage==GangDriveByStage.Escape)StopGunners();}
            if(taskAt==0 || unchecked(Game.GameTime-taskAt)>=0){taskAt=Game.GameTime+6000;Drive();if(clock.Stage==GangDriveByStage.Shooting)Shoot();}
        }
        void Shoot(){foreach(var p in crew.Skip(1).Where(p=>Tagged(p,token) && !p.IsDead && p.IsInVehicle(car))){Function.Call(Hash.SET_PED_FIRING_PATTERN,p,3337513804u);Function.Call(Hash.TASK_DRIVE_BY,p,target,0,0f,0f,0f,100f,100,true,0);}}
        void Drive()
        {
            if(!Tagged(car,token) || !Tagged(crew[0],token) || crew[0].IsDead || car.Driver!=crew[0])return;
            var q=target.Position;float speed=30;
            if(clock.Stage==GangDriveByStage.Escape){var direction=car.Position-target.Position;if(direction.Length()<1)direction=new Vector3(1,0,0);direction.Normalize();q=car.Position+direction*350;speed=30;}
            var road=World.GetNextPositionOnStreet(q,false);if(road==Vector3.Zero)return;Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE,crew[0],car,road.X,road.Y,road.Z,speed,786603,10f);
        }
        void StopGunners(){foreach(var p in crew.Skip(1).Where(p=>Tagged(p,token) && !p.IsDead)){Function.Call(Hash.CLEAR_DRIVEBY_TASK_UNDERNEATH_DRIVING_TASK,p);p.Weapons.RemoveAll();}}
        void End(string outcome)
        {
            try{if(profile==Profile.Id && Book?.Pending?.Id==run?.Id)GangDriveByTransactions.Finish(state(),run.Id,outcome,save,out _);}finally{Cleanup();}
        }
        static bool Connected(Vehicle v){var trailer=new OutputArgument();return Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,v) || Function.Call<bool>(Hash.GET_VEHICLE_TRAILER_VEHICLE,v,trailer);}
        void Cleanup()
        {
            StopGunners();if(Tagged(car,token) && Tagged(crew[0],token) && !crew[0].IsDead && car.Driver==crew[0] && !car.Occupants.Any(p=>!crew.Any(c=>Tagged(c,token) && c==p)))Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER,crew[0],car,12f,786603);
            if(car!=null || crew.Any(p=>p!=null))retired.Add(new Retired{Car=car,Crew=crew.ToArray(),Token=token,Last=Game.GameTime-4000});car=null;Array.Clear(crew,0,4);if(blip!=null && blip.Exists())blip.Delete();blip=null;if(models!=null)foreach(var m in models)m.MarkAsNoLongerNeeded();models=null;run=null;clock=null;target=null;profile=null;preparing=false;gate.Release(Owner);ResetSchedule();
        }
        void Drain()
        {
            for(int i=retired.Count-1;i>=0;i--){var r=retired[i];bool ownCar=Tagged(r.Car,r.Token);bool foreign=ownCar && r.Car.Occupants.Any(p=>!r.Crew.Any(c=>Tagged(c,r.Token) && c==p));
                if(ownCar && !foreign && !Connected(r.Car) && r.Car.Position.DistanceTo(Game.Player.Character.Position)>220){foreach(var p in r.Crew)if(Tagged(p,r.Token)){Function.Call(Hash.SET_PED_KEEP_TASK,p,false);p.BlockPermanentEvents=false;p.MarkAsNoLongerNeeded();}r.Car.MarkAsNoLongerNeeded();JobVehicles.Release(r.Car);retired.RemoveAt(i);continue;}
                bool waiting=false;bool retry=unchecked(Game.GameTime-r.Last)>=4000;
                foreach(var p in r.Crew){if(!Tagged(p,r.Token))continue;if(p.IsInVehicle()){if(p.IsDead){p.MarkAsNoLongerNeeded();continue;}waiting=true;if(ownCar && p.IsInVehicle(r.Car) && r.Car.Speed<.5f && retry)JobActors.Leave(p,r.Car);continue;}p.Weapons.RemoveAll();p.Delete();}
                if(ownCar){if(r.Car.Occupants.Length>0 && r.Car.Occupants.All(p=>p.IsDead && Tagged(p,r.Token)) && r.Car.Speed<.5f && !r.Car.IsInAir && !Connected(r.Car)){r.Car.MarkAsNoLongerNeeded();JobVehicles.Release(r.Car);}else if(r.Car.Occupants.Length!=0 || r.Car.Speed>=.5f || r.Car.IsInAir || Connected(r.Car))waiting=true;else{JobVehicles.Release(r.Car);r.Car.Delete();}}else JobVehicles.Release(r.Car);
                if(retry)r.Last=Game.GameTime;if(!waiting)retired.RemoveAt(i);
            }
        }
        void ResetSchedule(){scheduleProfile=Profile.Id;schedule=0;next=random.Next(options.MinIntervalSeconds,options.MaxIntervalSeconds+1)*1000;last=Game.GameTime;eligibleBefore=false;}
        public void Close(){menu.Visible=false;if(Active)End("interrupted");else ResetSchedule();}
        public void Dispose(){Close();foreach(var r in retired){foreach(var p in r.Crew)if(Tagged(p,r.Token)){p.Weapons.RemoveAll();Function.Call(Hash.SET_PED_KEEP_TASK,p,false);p.BlockPermanentEvents=false;p.MarkAsNoLongerNeeded();}if(Tagged(r.Car,r.Token))r.Car.MarkAsNoLongerNeeded();JobVehicles.Release(r.Car);}retired.Clear();}
    }
}
