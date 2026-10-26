using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
namespace RPCore
{
    internal sealed partial class GangTerritories
    {
        const string AssaultDecor="RPCore_AssaultOwner";
        sealed class AssaultRetired {public Vehicle[] Cars;public Ped[] Crew;public int Token,Last;}
        readonly List<AssaultRetired> assaultRetired=new List<AssaultRetired>();
        readonly Vehicle[] assaultCars=new Vehicle[4];readonly Ped[] assaultCrew=new Ped[15];readonly List<Blip> assaultBlips=new List<Blip>();
        GangAssaultOptions assaultOptions;GangTurfAttempt assaultDraft;GangAssaultProgress assaultProgress;Model[] assaultModels;
        bool convoyPreparing,convoyMoving,assaultClockEligible;int assaultToken,assaultRequested,assaultLast,assaultTaskAt,assaultNextSave;
        void InitAssault(){assaultOptions=Store.LoadDef("Gangs/Assault.json",GangAssaultOptions.Defaults);if(assaultOptions?.Valid!=true){assaultOptions=GangAssaultOptions.Defaults();Log.Write("Invalid assault options retained; embedded defaults used.");}}
        static bool AssaultTagged(Entity e,int token)=>e!=null && e.Exists() && e.Handle!=Game.Player.Character.Handle && Function.Call<bool>(Hash.DECOR_EXIST_ON,e,AssaultDecor) && Function.Call<int>(Hash.DECOR_GET_INT,e,AssaultDecor)==token;
        bool MarkAssault(Entity e){if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,AssaultDecor,3))Function.Call(Hash.DECOR_REGISTER,AssaultDecor,3);return Function.Call<bool>(Hash.DECOR_SET_INT,e,AssaultDecor,assaultToken);}
        public void OpenAssault()
        {
            pool.HideAll();menu.Clear();
            if(state().Turf!=null && !state().Turf.Valid){Add("Dados protegidos","Confira GangState.json/backup.",()=>{});menu.Visible=true;return;}
            if(Pending?.Assault!=null){Add((Active?"Suspender":"Retomar")+" comboio / "+Pending.Area.Name,Pending.Ready?"Pronto: confirmeE na area, sem novo comboio.":Pending.Assault.Progress.Arrived?"Chegada salva: retome a pe no alvo; somente sobreviventes voltam.":"Retome a pe na origem; veiculos/equipe sobreviventes sao recriados, tempo gasto nao volta.",()=>{if(Active)Suspend();else if(!Resume())Notify.Show("Confira origem/alvo, posse, modelos, vagas e atividade.");menu.Visible=false;});Add("Cancelar assalto","Conserva donos, dinheiro e estoque.",()=>{if(GangTurfTransactions.End(state(),Pending.Id,false,save,out _)){Suspend();OpenAssault();}});}
            Add("Preparar quatro veiculos","Escolha alvo rival. Lance a pe dentro de uma area propria; mapa ligado e sem outra atividade/pendencia. Carro lider + tres escoltas,15 aliados no maximo, sem taxa ou pagamento.",()=>{});
            foreach(var a in options.Areas.Where(a=>!GangCrewRules.SameGang(GangTurfRules.Owner(state().Turf,a),state().Gang))){var target=a;Add("Atacar: "+target.Name+" / "+GangTurfRules.Owner(state().Turf,target),"Saira da area propria mais proxima. Dirija o lider ate o alvo e espere uma escolta; pare, desembarque e avance a pe.10min ativos de viagem, combate com prazo proprio; END suspende.",()=>{if(!BeginAssault(target))Notify.Show("Confira area propria, mapa, gangue/modelos, pendencias e vagas livres. Saia dos carros da tentativa anterior.");else menu.Visible=false;},!HasPending && !Active);}
            Add("Voltar aos territorios","GPS, posse, defesas e capturas.",Open);menu.Visible=true;
        }
        bool AssaultSafe=>Safe && !Game.Player.Character.IsRagdoll && player==Game.Player.Character && profile==Profile.Id && state().TerritoriesEnabled && gate.Owner==OwnerKey;
        string[] AvailableAssaultModels(GangDefinition g)=>(g?.PedModels??Array.Empty<string>()).Where(GangPresenceRules.ModelName).Where(n=>{var m=new Model(n);return m.IsInCdImage && m.IsPed;}).ToArray();
        bool BeginAssault(GangTurfDefinition target)
        {
            if(Active || HasPending || !GangWarRules.Free(state()) || !Wallet.GangWarEconomyFree || state().Warfare?.Alert!=null || !state().TerritoriesEnabled || !Safe || Game.IsPaused || InterfaceOptions.ModalOpen || !Game.Player.Character.IsOnFoot || Game.Player.Character.IsInCombat || Game.Player.Wanted.WantedLevel!=0 || assaultRetired.Count!=0)return false;
            var origin=options.Areas.Where(a=>GangCrewRules.SameGang(GangTurfRules.Owner(state().Turf,a),state().Gang) && assaultOptions.Origins.Any(o=>o.Area==a.Id)).OrderBy(a=>Game.Player.Character.Position.DistanceTo(Pos(a.Center))).FirstOrDefault();
            if(origin==null || Game.Player.Character.Position.DistanceTo(Pos(origin.Center))>options.Radius)return false;
            var own=gangs().FirstOrDefault(g=>GangCrewRules.SameGang(g.Name,state().Gang));var enemy=GangTurfRules.Owner(state().Turf,target);var rival=gangs().FirstOrDefault(g=>GangCrewRules.SameGang(g.Name,enemy));
            var crew=AvailableAssaultModels(own);var enemies=AvailableAssaultModels(rival);if(crew.Length==0 || enemies.Length==0 || GangCrewRules.SameGang(enemy,state().Gang))return false;
            bool reinforced=state().Warfare?.Valid==true && state().Warfare.Reinforcements;int initial=reinforced?Math.Min(3,options.Defenders):0;
            var p=new GangTurfAttempt{Id=Guid.NewGuid().ToString("N"),Gang=state().Gang,PreviousOwner=enemy,Area=CharacterRules.Copy(target),Radius=options.Radius,SecondsLimit=options.SecondsLimit,InitialDefenders=initial,Models=Enumerable.Range(0,reinforced?initial+3:options.Defenders).Select(i=>enemies[i%enemies.Length]).ToArray(),Assault=new GangAssaultPlan{Origin=CharacterRules.Copy(origin),Options=CharacterRules.Copy(assaultOptions),Bay=CharacterRules.Copy(assaultOptions.Origins.First(o=>o.Area==origin.Id)),CrewModels=Enumerable.Range(0,15).Select(i=>crew[i%crew.Length]).ToArray()}};
            return PrepareAssault(p);
        }
        bool ResumeAssault()
        {
            var p=Pending;if(Active || p?.Assault?.Valid!=true || state().Turf.Valid!=true || !Wallet.GangWarEconomyFree || state().Loyalty?.Pending!=null || state().Strength?.Pending!=null || state().Driving?.Pending!=null || !state().TerritoriesEnabled || !Safe || !Game.Player.Character.IsOnFoot || Game.Player.Character.IsInCombat || Game.Player.Wanted.WantedLevel!=0 || Game.IsPaused || InterfaceOptions.ModalOpen || !GangCrewRules.SameGang(p.Gang,state().Gang) || !GangCrewRules.SameGang(GangTurfRules.Owner(state().Turf,p.Area),p.PreviousOwner) || assaultRetired.Count!=0)return false;
            var point=p.Assault.Progress.Arrived?p.Area.Center:p.Assault.Origin.Center;
            if(Game.Player.Character.Position.DistanceTo(Pos(point))>p.Radius || !p.Assault.Progress.Arrived && !GangCrewRules.SameGang(GangTurfRules.Owner(state().Turf,p.Assault.Origin),p.Gang))return false;
            if(p.Assault.Progress.TravelElapsed>=p.Assault.Options.TravelSeconds*1000 || p.Assault.Progress.BattleElapsed>=p.SecondsLimit*1000 || !p.Assault.Progress.Arrived && (p.Assault.Progress.LostCars.Contains(0) || GangAssaultRules.NoEscort(p.Assault.Progress)) ){Notify.Show("Assalto esgotado/sem lider ou motoristas; cancele a pendencia para preparar outro.");return false;}
            return PrepareAssault(CharacterRules.Copy(p));
        }
        bool PrepareAssault(GangTurfAttempt p)
        {
            if(!p.Valid || !gate.Acquire(OwnerKey))return false;
            assaultDraft=p;draft=p;profile=Profile.Id;player=Game.Player.Character;assaultProgress=CharacterRules.Copy(p.Assault.Progress);assaultToken=new Random().Next(1,int.MaxValue);
            assaultModels=p.Assault.CrewModels.Concat(p.Models).Append(p.Assault.Options.VehicleModel).Select(n=>new Model(n)).ToArray();
            if(assaultModels.Take(15+p.Models.Length).Any(m=>!m.IsInCdImage || !m.IsPed) || !assaultModels.Last().IsInCdImage || !assaultModels.Last().IsCar){Suspend();return false;}
            foreach(var m in assaultModels)m.Request();convoyPreparing=true;assaultRequested=assaultLast=Game.GameTime;assaultClockEligible=false;pool.HideAll();return true;
        }
        GangSpot AssaultBay(GangSpot spot,List<Vector3> used)
        {
            for(int i=0;i<40;i++){
                float heading=spot.Heading;var p=Pos(spot);
                if(i>0){var query=p+new Vector3((float)Math.Cos(i*2.4)*(8+i*2),(float)Math.Sin(i*2.4)*(8+i*2),0);var node=new OutputArgument();var angle=new OutputArgument();if(!Function.Call<bool>(Hash.GET_CLOSEST_VEHICLE_NODE_WITH_HEADING,query.X,query.Y,query.Z,node,angle,1,3f,0))continue;p=node.GetResult<Vector3>();heading=angle.GetResult<float>();}
                if(p==Vector3.Zero || p.DistanceTo(Pos(spot))>100 || Math.Abs(p.Z-spot.Z)>6 || used.Any(q=>q.DistanceTo(p)<9) || !JobActors.Free(p,5))continue;
                float z;if(!World.GetGroundHeight(p+new Vector3(0,0,3),out z,GetGroundHeightMode.Normal) || Math.Abs(z-p.Z)>2)continue;return new GangSpot{X=p.X,Y=p.Y,Z=z+.1f,Heading=heading};
            }return null;
        }
        Vector3? AssaultFootSpot(int index,GangTurfDefinition area)
        {
            for(int i=0;i<40;i++){
                double angle=(index*4+i)*2.4;var center=Pos(area.Center);var candidate=center+new Vector3((float)Math.Cos(angle)*(Math.Min(assaultDraft.Radius*.9f,assaultDraft.Radius*.65f+i*.2f)),(float)Math.Sin(angle)*(Math.Min(assaultDraft.Radius*.9f,assaultDraft.Radius*.65f+i*.2f)),0);Vector3 p;
                if(!World.GetSafePositionForPed(candidate,out p,GetSafePositionFlags.OnlyPavement|GetSafePositionFlags.NotInterior|GetSafePositionFlags.NotWater) || p.DistanceTo(center)>assaultDraft.Radius-3 || Math.Abs(p.Z-center.Z)>3 || p.DistanceTo(player.Position)<3 || !JobActors.Free(p,1.5f))continue;return p;
            }return null;
        }
        void SpawnAssault()
        {
            bool arrived=assaultProgress.Arrived;var p=assaultDraft;
            if(!arrived){
                var used=new List<Vector3>();for(int i=0;i<4;i++){
                    if(assaultProgress.LostCars.Contains(i))continue;var point=AssaultBay(p.Assault.Bay.Spawns[i],used);if(point==null)throw new InvalidOperationException("No safe convoy vehicle slots; ambient vehicles preserved.");used.Add(Pos(point));
                    var car=World.CreateVehicle(assaultModels.Last(),Pos(point),point.Heading);assaultCars[i]=car;if(car==null || !car.Exists())throw new InvalidOperationException("Convoy vehicle unavailable.");if(!MarkAssault(car)){car.MarkAsNoLongerNeeded();assaultCars[i]=null;throw new InvalidOperationException("Convoy vehicle ownership unavailable.");}
                    if(car.PassengerCapacity<3)throw new InvalidOperationException("Convoy vehicle needs three passenger seats.");car.IsPersistent=true;car.IsEngineRunning=true;JobVehicles.Reserve(car);
                    var b=car.AddBlip();assaultBlips.Add(b);b.Color=BlipColor.Green;b.Sprite=BlipSprite.PersonalVehicleCar;b.Name=i==0?"RPCore: lider do comboio":"RPCore: escolta "+i;
                }
            }
            for(int i=0;i<15;i++){
                if(assaultProgress.LostCrew.Contains(i))continue;bool foot=arrived || assaultProgress.LostCars.Contains(GangAssaultRules.Car(i));
                Vector3? point=foot?AssaultFootSpot(i,arrived?p.Area:p.Assault.Origin):assaultCars[GangAssaultRules.Car(i)].Position+new Vector3(0,0,2);
                if(!point.HasValue)throw new InvalidOperationException("No safe surviving assault crew slot.");var ped=World.CreatePed(assaultModels[i],point.Value);assaultCrew[i]=ped;if(ped==null || !ped.Exists())throw new InvalidOperationException("Convoy crew unavailable.");if(!ped.IsHuman || !MarkAssault(ped)){ped.Delete();assaultCrew[i]=null;throw new InvalidOperationException("Convoy crew ownership unavailable.");}
                ped.IsPersistent=true;ped.BlockPermanentEvents=true;ped.Health=200;ped.Armor=75;ped.Accuracy=55;ped.RelationshipGroup=player.RelationshipGroup;ped.Weapons.RemoveAll();ped.Task.StandStill(-1);
                Function.Call(Hash.SET_PED_MONEY,ped,0);Function.Call(Hash.SET_PED_DROPS_WEAPONS_WHEN_DEAD,ped,false);Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES,ped,(int)CombatAttributes.CanCommandeerVehicles,false);
                if(!foot){var car=assaultCars[GangAssaultRules.Car(i)];ped.SetIntoVehicle(car,(VehicleSeat)GangAssaultRules.Seat(i));if(!ped.IsInVehicle(car))throw new InvalidOperationException("Convoy seat unavailable.");}
            }
            if(Pending==null && !GangTurfTransactions.Begin(state(),p,save,out _))throw new InvalidOperationException("Assault plan not saved.");
            convoyPreparing=false;convoyMoving=true;assaultLast=Game.GameTime;assaultTaskAt=0;assaultNextSave=assaultProgress.TravelElapsed+assaultProgress.BattleElapsed+1000;Route();
            if(arrived)PrepareAssaultBattle();else Notify.Show("Comboio pronto: dirija o carro verde LIDER. Tres escoltas seguem; pare perto do alvo e espere uma delas. END suspende.");
        }
        bool FlushAssault()
        {
            if(assaultProgress==null || profile!=Profile.Id || Pending?.Id!=assaultDraft?.Id || Pending.Ready)return true;
            if(JsonEqual(assaultProgress,Pending.Assault.Progress))return true;
            return GangAssaultTransactions.Sample(state(),Pending.Id,assaultProgress,save,out _);
        }
        static bool JsonEqual(object a,object b)=>Newtonsoft.Json.JsonConvert.SerializeObject(a)==Newtonsoft.Json.JsonConvert.SerializeObject(b);
        bool SampleAssaultActors()
        {
            for(int i=0;i<15;i++)if(!assaultProgress.LostCrew.Contains(i)){var ped=assaultCrew[i];if(!AssaultTagged(ped,assaultToken))return false;if(ped.IsDead)assaultProgress.LostCrew.Add(i);}
            for(int i=0;i<4;i++)if(assaultCars[i]!=null && !assaultProgress.LostCars.Contains(i)){var car=assaultCars[i];if(!AssaultTagged(car,assaultToken))return false;if(car.IsDead)assaultProgress.LostCars.Add(i);}
            return true;
        }
        bool FlushAssaultCasualties()
        {
            var saved=Pending?.Assault?.Progress;
            return saved==null || assaultProgress==null || saved.LostCrew.Count==assaultProgress.LostCrew.Count && saved.LostCars.Count==assaultProgress.LostCars.Count || FlushAssault();
        }
        void AssaultClock(bool battle)
        {
            bool eligible=!Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
            int delta=GangWarRules.Delta(assaultLast,Game.GameTime,eligible && assaultClockEligible);assaultLast=Game.GameTime;assaultClockEligible=eligible;
            if(battle)assaultProgress.BattleElapsed=Math.Min(Pending.SecondsLimit*1000,assaultProgress.BattleElapsed+delta);else assaultProgress.TravelElapsed=Math.Min(assaultDraft.Assault.Options.TravelSeconds*1000,assaultProgress.TravelElapsed+delta);
        }
        void PrepareAssaultBattle()
        {
            if(!Pending.Assault.Progress.Arrived || !Game.Player.Character.IsOnFoot || !InArea(Pending))return;
            convoyMoving=false;draft=CharacterRules.Copy(Pending);models=draft.Models.Select(n=>new Model(n)).ToArray();foreach(var m in models)m.Request();preparing=true;started=Game.GameTime;assaultLast=Game.GameTime;assaultClockEligible=false;
            Notify.Show("Chegada confirmada. Equipe desembarcando; elimine defensores e confirmeE para capturar.");
        }
        void TickAssault()
        {
            if(profile==Profile.Id && player==Game.Player.Character && player.IsDead && Pending?.Id==assaultDraft?.Id){Fail("morte durante o comboio");return;}
            if(!AssaultSafe || Pending!=null && !GangCrewRules.SameGang(Pending.Gang,state().Gang) || !Wallet.GangWarEconomyFree || !GangCrewRules.SameGang(GangTurfRules.Owner(state().Turf,assaultDraft.Area),assaultDraft.PreviousOwner)){Suspend();return;}
            if(Game.IsPaused || pool.AreAnyVisible || InterfaceOptions.ModalOpen){assaultLast=Game.GameTime;assaultClockEligible=false;return;}
            if(convoyPreparing){if(!Game.Player.Character.IsOnFoot || Game.Player.Wanted.WantedLevel!=0 || Game.Player.Character.IsInCombat || Game.Player.Character.Position.DistanceTo(Pos(assaultDraft.Assault.Progress.Arrived?assaultDraft.Area.Center:assaultDraft.Assault.Origin.Center))>assaultDraft.Radius || unchecked(Game.GameTime-assaultRequested)>10000){Suspend();return;}if(assaultModels.All(m=>m.IsLoaded))SpawnAssault();return;}
            if(Pending?.Id!=assaultDraft.Id){Suspend();return;}
            if(!SampleAssaultActors()){Suspend();Notify.Show("Ator do comboio indisponivel. Pendencia e baixas confirmadas conservadas.");return;}
            if(!FlushAssaultCasualties()){Suspend();return;}
            if(!assaultProgress.Arrived)AssaultClock(false);else{assaultLast=Game.GameTime;assaultClockEligible=false;}
            if(!assaultProgress.Arrived && (assaultProgress.TravelElapsed>=Pending.Assault.Options.TravelSeconds*1000 || assaultProgress.LostCars.Contains(0) || GangAssaultRules.NoEscort(assaultProgress))){FlushAssault();Fail("comboio perdeu lider/motoristas ou prazo de viagem");return;}
            if(assaultProgress.TravelElapsed+assaultProgress.BattleElapsed>=assaultNextSave){if(!FlushAssault()){Suspend();return;}assaultNextSave=assaultProgress.TravelElapsed+assaultProgress.BattleElapsed+1000;}
            var lead=assaultCars[0];if(!AssaultTagged(lead,assaultToken)){Suspend();return;}
            if(assaultCars.Where(c=>AssaultTagged(c,assaultToken)).Any(c=>c.Occupants.Any(p=>p!=player && !assaultCrew.Any(a=>AssaultTagged(a,assaultToken) && a==p)) || AssaultConnected(c))){Suspend();Notify.Show("Comboio ocupado/conectado por terceiros; retomada conservada.");return;}
            bool driver=player.CurrentVehicle==lead && lead.Driver==player;
            if(!assaultProgress.Arrived){
                bool escort=Enumerable.Range(1,3).Any(i=>AssaultTagged(assaultCars[i],assaultToken) && AssaultTagged(assaultCrew[3+(i-1)*4],assaultToken) && !assaultCars[i].IsDead && !assaultCrew[3+(i-1)*4].IsDead && assaultCars[i].Driver==assaultCrew[3+(i-1)*4] && assaultCars[i].Position.DistanceTo(Pos(Pending.Area.Center))<=Pending.Assault.Options.EscortRadius);
                if(GangAssaultRules.Arrive(Pending,driver,new GangSpot{X=lead.Position.X,Y=lead.Position.Y,Z=lead.Position.Z},lead.Speed,escort)){
                    if(!FlushAssault() || !GangAssaultTransactions.Arrive(state(),Pending.Id,driver,new GangSpot{X=lead.Position.X,Y=lead.Position.Y,Z=lead.Position.Z},lead.Speed,escort,save,out _)){Suspend();return;}
                    assaultProgress=CharacterRules.Copy(Pending.Assault.Progress);Notify.Show("Comboio chegou. Saia do lider e avance A PE ate o centro marcado para iniciar o assalto.");assaultTaskAt=0;
                }
            }
            if(unchecked(Game.GameTime-assaultTaskAt)>=0){assaultTaskAt=Game.GameTime+4500;OrderAssaultEscort(driver);}
            if(assaultProgress.Arrived && player.IsOnFoot && InArea(Pending)){PrepareAssaultBattle();return;}
            if(player.Position.DistanceTo(lead.Position)>200){Suspend();Notify.Show("Comboio suspenso por afastamento do lider; retome na origem.");return;}
            GTA.UI.Screen.ShowSubtitle("ASSALTO / "+(assaultProgress.Arrived?"DESEMBARQUE E AVANCE A PE":driver?"CONDUZA O COMBOIO":"ENTRE COMO MOTORISTA NO LIDER VERDE")+" / "+Math.Max(0,Pending.Assault.Options.TravelSeconds-assaultProgress.TravelElapsed/1000)+"s / END suspende",100);
        }
        void OrderAssaultEscort(bool playerDriving)
        {
            for(int i=1;i<4;i++){
                var car=assaultCars[i];var driver=assaultCrew[3+(i-1)*4];if(!AssaultTagged(car,assaultToken) || !AssaultTagged(driver,assaultToken) || driver.IsDead || assaultProgress.LostCars.Contains(i))continue;
                if(assaultProgress.Arrived){if(car.Speed<1.5f)foreach(var p in assaultCrew.Where(p=>AssaultTagged(p,assaultToken) && !p.IsDead && p.IsInVehicle(car)))JobActors.Leave(p,car);else if(car.Driver==driver)JobActors.Drive(driver,car,assaultCars[0].Position);continue;}
                if(!playerDriving)continue;if(car.Driver!=driver){if(driver.IsOnFoot)JobActors.Enter(driver,car,VehicleSeat.Driver);continue;}
                Function.Call(Hash.SET_DRIVER_ABILITY,driver,1f);Function.Call(Hash.TASK_VEHICLE_ESCORT,driver,car,assaultCars[0],-1,22f,786603,9f+(i-1)*4,8,5f);
            }
            if(assaultProgress.Arrived && assaultCars[0]!=null && assaultCars[0].Speed<1.5f)foreach(var p in assaultCrew.Take(3).Where(p=>AssaultTagged(p,assaultToken) && !p.IsDead && p.IsInVehicle()))JobActors.Leave(p,p.CurrentVehicle);
        }
        void TickAssaultBattle()
        {
            if(Pending?.Assault==null || assaultProgress==null)return;
            if(!SampleAssaultActors() || !FlushAssaultCasualties()){Suspend();return;}AssaultClock(true);
            if(assaultProgress.BattleElapsed>=Pending.SecondsLimit*1000){FlushAssault();Fail("prazo de combate esgotado");return;}
            if(assaultProgress.TravelElapsed+assaultProgress.BattleElapsed>=assaultNextSave){if(!FlushAssault()){Suspend();return;}assaultNextSave=assaultProgress.TravelElapsed+assaultProgress.BattleElapsed+1000;}
            if(Game.IsPaused || pool.AreAnyVisible || InterfaceOptions.ModalOpen || unchecked(Game.GameTime-assaultTaskAt)<0)return;assaultTaskAt=Game.GameTime+2500;
            foreach(var p in assaultCrew.Where(p=>AssaultTagged(p,assaultToken) && !p.IsDead)){
                if(p.IsInVehicle()){var car=p.CurrentVehicle;if(AssaultTagged(car,assaultToken) && car.Speed<1.5f)JobActors.Leave(p,car);continue;}
                var enemy=defenders.Where(d=>d!=null && d.Exists() && !d.IsDead).OrderBy(d=>d.Position.DistanceTo(p.Position)).FirstOrDefault();if(enemy==null || enemy.Position.DistanceTo(p.Position)>150)continue;
                if(!Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON,p,(uint)WeaponHash.CarbineRifle,false))p.Weapons.Give(WeaponHash.CarbineRifle,120,true,true);Function.Call(Hash.TASK_COMBAT_PED,p,enemy,0,16);
            }
        }
        static bool AssaultConnected(Vehicle car){var trailer=new OutputArgument();return Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,car) || Function.Call<bool>(Hash.GET_VEHICLE_TRAILER_VEHICLE,car,trailer);}
        void StopAssault()
        {
            try{if(!convoyPreparing){if(profile==Profile.Id && Pending?.Id==assaultDraft?.Id && Pending?.Ready!=true && assaultProgress!=null)SampleAssaultActors();FlushAssault();}}finally{
                if(assaultCars.Any(c=>c!=null) || assaultCrew.Any(p=>p!=null))assaultRetired.Add(new AssaultRetired{Cars=assaultCars.ToArray(),Crew=assaultCrew.ToArray(),Token=assaultToken,Last=Game.GameTime-4000});
                Array.Clear(assaultCars,0,4);Array.Clear(assaultCrew,0,15);foreach(var b in assaultBlips)if(b!=null && b.Exists())b.Delete();assaultBlips.Clear();if(assaultModels!=null)foreach(var m in assaultModels)m.MarkAsNoLongerNeeded();assaultModels=null;assaultDraft=null;assaultProgress=null;convoyMoving=convoyPreparing=false;assaultClockEligible=false;
            }
        }
        void DrainAssault()
        {
            for(int i=assaultRetired.Count-1;i>=0;i--){var r=assaultRetired[i];bool waiting=false;bool retry=unchecked(Game.GameTime-r.Last)>=4000;
                foreach(var p in r.Crew){if(!AssaultTagged(p,r.Token))continue;if(p.IsInVehicle()){if(p.IsDead){p.MarkAsNoLongerNeeded();continue;}waiting=true;var car=p.CurrentVehicle;if(AssaultTagged(car,r.Token) && car.Speed<.5f && retry)JobActors.Leave(p,car);continue;}p.Weapons.RemoveAll();p.Delete();}
                foreach(var car in r.Cars){if(!AssaultTagged(car,r.Token)){JobVehicles.Release(car);continue;}if(car.Occupants.Length>0 && car.Occupants.All(p=>p.IsDead && AssaultTagged(p,r.Token)) && car.Speed<.5f && !car.IsInAir && !AssaultConnected(car)){car.MarkAsNoLongerNeeded();JobVehicles.Release(car);continue;}if(car.Occupants.Length!=0 || car.Speed>=.5f || car.IsInAir || AssaultConnected(car)){waiting=true;continue;}JobVehicles.Release(car);car.Delete();}
                if(retry)r.Last=Game.GameTime;if(!waiting)assaultRetired.RemoveAt(i);
            }
        }
        void DisposeAssault(){foreach(var r in assaultRetired){foreach(var p in r.Crew)if(AssaultTagged(p,r.Token)){p.BlockPermanentEvents=false;p.MarkAsNoLongerNeeded();}foreach(var c in r.Cars){if(AssaultTagged(c,r.Token))c.MarkAsNoLongerNeeded();JobVehicles.Release(c);}}assaultRetired.Clear();}
    }
}
