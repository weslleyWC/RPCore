using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
namespace RPCore
{
    internal sealed class GangDriving : IDisposable
    {
        const string Owner="gang_driving";readonly ShiftGate gate;readonly ObjectPool pool;readonly GangPresence presence;readonly Func<GangState> state;readonly Action<GangState> save;readonly GangDrivingOptions options;readonly Random random=new Random();
        Ped crew,target,player;Vehicle car,boarding;Model crewModel,targetModel;Blip route,crewBlip,targetBlip;GangDrivingAttempt draft;bool active,preparing;int started,last,nextTask,boardUntil,retryAt,warnAt,crewGroup,targetGroup;float seconds;string profile;
        GangDrivingAttempt Pending=>state().Driving?.Pending;
        public bool Active=>active || preparing;
        public bool HasPending=>Pending!=null;
        bool Alive(Ped p)=>p!=null && p.Exists() && !p.IsDead;
        bool Safe=>!Game.IsMissionActive && !Game.Player.Character.IsDead && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool Driver=>car!=null && car.Exists() && car.Driver==Game.Player.Character && Game.Player.Character.IsInVehicle(car);
        bool Passenger=>Alive(crew) && car!=null && car.Exists() && crew.IsInVehicle(car) && car.GetPedOnSeat(VehicleSeat.Passenger)==crew;
        static Vector3 Pos(GangSpot p)=>new Vector3(p.X,p.Y,p.Z);
        bool AtLeader(string gang){var p=Game.Player.Character;var leader=presence.Leader(gang);return Safe && !Game.IsPaused && Game.Player.Wanted.WantedLevel==0 && !p.IsInCombat && p.IsOnFoot && !InterfaceOptions.ModalOpen && leader!=null && p.Position.DistanceTo(leader.Position)<=3;}
        public GangDriving(ShiftGate gate,ObjectPool pool,GangPresence presence,Func<GangState> state,Action<GangState> save)
        {this.gate=gate;this.pool=pool;this.presence=presence;this.state=state;this.save=save;options=Store.LoadDef("Gangs/Driving.json",GangDrivingOptions.Defaults);if(options==null || !options.Valid){options=GangDrivingOptions.Defaults();Log.Write("Invalid gang driving config; original retained, embedded defaults used.");}}
        static string[] Models(GangDefinition g)=>(g.PedModels??Array.Empty<string>()).Where(GangPresenceRules.ModelName).Where(n=>{var m=new Model(n);return m.IsInCdImage && m.IsPed;}).ToArray();
        public bool Begin(GangDefinition gang,IEnumerable<GangDefinition> gangs)
        {
            if(Active || HasPending || state().Turf?.Pending!=null || state().Loyalty?.Pending!=null || state().Strength?.Pending!=null || !GangLoyaltyRules.Unaffiliated(state().Gang) || !AtLeader(gang.Name))return false;
            var own=Models(gang);var rivals=gangs.Where(g=>!string.Equals(g.Name,gang.Name,StringComparison.OrdinalIgnoreCase)).SelectMany(Models).ToArray();if(own.Length==0 || rivals.Length==0)return false;
            var p=new GangDrivingAttempt{Id=Guid.NewGuid().ToString("N"),Gang=gang.Name,CrewModel=own[random.Next(own.Length)],TargetModel=rivals[random.Next(rivals.Length)],Home=GangPresenceRules.HQ(gang),Target=options.Targets[random.Next(options.Targets.Count)],Options=CharacterRules.Copy(options)};
            return Prepare(CharacterRules.Copy(p));
        }
        public bool Resume()
        {
            if(Active || Pending==null || state().Turf?.Pending!=null || state().Driving.Valid!=true || state().Loyalty?.Pending!=null || state().Strength?.Pending!=null || !GangLoyaltyRules.Unaffiliated(state().Gang) || !AtLeader(Pending.Gang))return false;
            if(Pending.Stage==GangDrivingStage.Report){if(!gate.Acquire(Owner))return false;active=true;profile=Profile.Id;pool.HideAll();Route();return true;}
            return Prepare(CharacterRules.Copy(Pending));
        }
        bool Prepare(GangDrivingAttempt p)
        {
            if(!p.Valid || !gate.Acquire(Owner))return false;draft=p;profile=Profile.Id;player=Game.Player.Character;crewModel=new Model(p.CrewModel);targetModel=new Model(p.TargetModel);
            if(!crewModel.IsInCdImage || !crewModel.IsPed || !targetModel.IsInCdImage || !targetModel.IsPed){Suspend();return false;}
            Function.Call(Hash.REQUEST_MODEL,crewModel.Hash);Function.Call(Hash.REQUEST_MODEL,targetModel.Hash);preparing=true;started=Game.GameTime;pool.HideAll();return true;
        }
        Vector3? FreePoint(Vector3 origin,float radius)
        {
            for(int i=0;i<(radius==0?1:8);i++){
                var p=origin+new Vector3((float)Math.Cos(i*Math.PI/4)*radius,(float)Math.Sin(i*Math.PI/4)*radius,0);float z;
                if(World.GetGroundHeight(p+new Vector3(0,0,2),out z,GetGroundHeightMode.Normal) && Math.Abs(z-p.Z)<=2 && JobActors.Free(p,2) && GangPresence.Space(new Vector3(p.X,p.Y,z)))return new Vector3(p.X,p.Y,z+.05f);
            }return null;
        }
        void SpawnCrew()
        {
            var p=FreePoint(player.Position,4);if(!p.HasValue){Suspend();Notify.Show("Sem espaco seguro para o integrante. Nenhuma tentativa nova.");return;}
            string tag=Guid.NewGuid().ToString("N").Substring(0,8);crewGroup=World.AddRelationshipGroup("RPDC"+tag).Hash;targetGroup=World.AddRelationshipGroup("RPDT"+tag).Hash;
            // Hostility exists only between these two private groups; vanilla groups are untouched.
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS,5,crewGroup,targetGroup);
            crew=World.CreatePed(crewModel,p.Value);if(!Alive(crew) || !crew.IsHuman)throw new InvalidOperationException("Driving crew unavailable.");Setup(crew,crewGroup);crew.Armor=100;crew.Accuracy=65;
            crewBlip=crew.AddBlip();crewBlip.Name="RPCore: passageiro da iniciacao";crewBlip.Color=BlipColor.Blue;crewBlip.Sprite=BlipSprite.Friend;
            Function.Call(Hash.SET_PED_CONFIG_FLAG,crew,(int)PedConfigFlagToggles.PreventAutoShuffleToDriversSeat,true);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES,crew,(int)CombatAttributes.CanCommandeerVehicles,false);
            if(Pending==null){if(!GangDrivingTransactions.Begin(state(),draft,save,out _))throw new InvalidOperationException("Driving initiation not saved.");}
            else if(!GangDrivingTransactions.Restart(state(),Pending.Id,save,out _))throw new InvalidOperationException("Driving restart not saved.");
            preparing=false;active=true;last=Game.GameTime;seconds=0;nextTask=0;Route();Notify.Show("Entre como motorista num carro parado, com banco dianteiro livre. Aguarde seu integrante embarcar.");
        }
        void Setup(Ped p,int group){p.IsPersistent=true;p.BlockPermanentEvents=true;p.Health=200;p.Armor=0;p.Weapons.RemoveAll();p.RelationshipGroup=new RelationshipGroup(group);Function.Call(Hash.SET_PED_DROPS_WEAPONS_WHEN_DEAD,p,false);Function.Call(Hash.SET_PED_MONEY,p,0);p.Task.StandStill(-1);}
        void Route()
        {
            if(route!=null && route.Exists())route.Delete();var p=Pending;var leader=presence.Leader(p.Gang);
            var point=p.Stage==GangDrivingStage.Boarding && Alive(crew)?crew.Position:(int)p.Stage>=(int)GangDrivingStage.Return?(p.Stage==GangDrivingStage.Report && leader!=null?leader.Position:Pos(p.Home)):Pos(p.Target);
            route=World.CreateBlip(point);route.Name="RPCore direcao: "+(p.Stage==GangDrivingStage.Boarding?"embarcar integrante":p.Stage==GangDrivingStage.Target?"alvo":p.Stage==GangDrivingStage.Escape?"afaste-se "+p.Options.EscapeRadius+"m":"retornar a gangue");route.Color=BlipColor.Yellow;route.ShowRoute=true;
        }
        void Advance(){if(!GangDrivingTransactions.Advance(state(),Pending.Id,Pending.Stage,save,out _))throw new InvalidOperationException("Driving checkpoint not saved.");Route();}
        void DeleteNpc(Ped p){if(p==null || !p.Exists())return;p.Task.ClearAll();p.Weapons.RemoveAll();p.RelationshipGroup=new RelationshipGroup((int)RelationshipGroupHash.CivilianMale);if(p.IsInVehicle() && p.CurrentVehicle.Speed>=.5f)p.MarkAsNoLongerNeeded();else p.Delete();}
        void ClearActors(){DeleteNpc(crew);DeleteNpc(target);crew=target=null;foreach(var b in new[]{crewBlip,targetBlip})if(b!=null && b.Exists())b.Delete();crewBlip=targetBlip=null;if(crewGroup!=0)Function.Call(Hash.REMOVE_RELATIONSHIP_GROUP,crewGroup);if(targetGroup!=0)Function.Call(Hash.REMOVE_RELATIONSHIP_GROUP,targetGroup);crewGroup=targetGroup=0;}
        public void Suspend(){if(route!=null && route.Exists())route.Delete();route=null;ClearActors();if(crewModel.Hash!=0)crewModel.MarkAsNoLongerNeeded();if(targetModel.Hash!=0)targetModel.MarkAsNoLongerNeeded();crewModel=targetModel=default(Model);car=boarding=null;draft=null;player=null;active=preparing=false;profile=null;gate.Release(Owner);}
        public bool Cancel(){if(Pending==null)return false;if(!GangDrivingTransactions.End(state(),Pending.Id,false,save,out _))return false;Suspend();return true;}
        void Fail(string reason){if(Pending!=null && !GangDrivingTransactions.End(state(),Pending.Id,true,save,out _))throw new InvalidOperationException("Driving failure not saved.");Suspend();Notify.Show("Iniciacao reprovada: "+reason+". Filiacao preservada.");}
        public void Interact(){if(!active || Pending?.Stage!=GangDrivingStage.Report || !AtLeader(Pending.Gang) || pool.AreAnyVisible)return;if(!GangDrivingTransactions.Complete(state(),Pending.Id,save,out _))throw new InvalidOperationException("Driving membership not saved.");Suspend();Notify.Show("Iniciacao de direcao concluida; filiacao gravada em "+state().Gang+".");}
        void Board()
        {
            var p=Game.Player.Character;var v=p.CurrentVehicle;
            if(crew.IsInVehicle()){
                var own=crew.CurrentVehicle;if(v==own && own.Model.IsCar && !JobVehicles.Contains(own) && own.IsDriveable && !own.IsDead && own.Driver==p && own.GetPedOnSeat(VehicleSeat.Passenger)==crew){car=own;boarding=null;crew.Weapons.Give(WeaponHash.MicroSMG,1500,true,true);Advance();return;}
                if(own.Speed<.5f && unchecked(Game.GameTime-nextTask)>=0){JobActors.Leave(crew,own);nextTask=Game.GameTime+5000;}return;
            }
            if(boarding!=null && (!boarding.Exists() || v!=boarding || boarding.Speed>=.5f || unchecked(Game.GameTime-boardUntil)>=0 || !boarding.IsSeatFree(VehicleSeat.Passenger) && boarding.GetPedOnSeat(VehicleSeat.Passenger)!=crew)){crew.Task.ClearAll();boarding=null;nextTask=0;}
            if(unchecked(Game.GameTime-nextTask)<0 || boarding!=null)return;
            if(v!=null && v.Exists() && GangCrewRules.Board(v.Driver==p,v.Model.IsCar,JobVehicles.Contains(v),!v.IsDead && v.IsDriveable,v.Speed,crew.Position.DistanceTo(v.Position)) && v.IsSeatFree(VehicleSeat.Passenger)){
                boarding=v;boardUntil=Game.GameTime+15000;Function.Call(Hash.TASK_ENTER_VEHICLE,crew,v,15000,0,1f,(int)(EnterVehicleFlags.DontJackAnyone|EnterVehicleFlags.BlockSeatShuffling),0);nextTask=Game.GameTime+1000;
            }else if(!p.IsInVehicle() && crew.Position.DistanceTo(p.Position)>3){Function.Call(Hash.TASK_FOLLOW_TO_OFFSET_OF_ENTITY,crew,p,1.5f,-2f,0f,2f,-1,2f,true);nextTask=Game.GameTime+3000;}
        }
        public void Tick()
        {
            if(!Active)return;
            if(profile!=Profile.Id || !state().PresenceEnabled || !GangLoyaltyRules.Unaffiliated(state().Gang)){Suspend();return;}
            if(Game.IsPaused){last=Game.GameTime;return;}
            if(preparing){if(!Safe || !AtLeader(draft.Gang) || player!=Game.Player.Character || player.IsInCombat || !player.IsOnFoot || Game.Player.Wanted.WantedLevel>0 || unchecked(Game.GameTime-started)>10000){Suspend();return;}if(crewModel.IsLoaded && targetModel.IsLoaded)SpawnCrew();return;}
            if(Pending==null){Suspend();return;}
            if(Pending.Stage==GangDrivingStage.Report){if(!Safe){Suspend();return;}if(AtLeader(Pending.Gang) && !pool.AreAnyVisible)GTA.UI.Screen.ShowHelpTextThisFrame("E: confirmar filiacao apos a iniciacao de direcao");return;}
            if(!Safe || player!=Game.Player.Character || !Alive(crew)){Fail("jogador/integrante indisponivel ou missao iniciada");return;}
            seconds+=Math.Min(1,Math.Max(0,unchecked(Game.GameTime-last)/1000f));last=Game.GameTime;if(seconds>=Pending.Options.SecondsLimit || player.Position.DistanceTo(crew.Position)>180){Fail("tempo ou abandono do integrante");return;}
            if(crewBlip!=null && crewBlip.Exists())crewBlip.Position=crew.Position;
            if(Pending.Stage==GangDrivingStage.Boarding){Board();}
            else{
                if(car==null || !car.Exists() || car.IsDead || !car.IsDriveable || !Driver || !Passenger){Fail("use o mesmo carro como motorista com o integrante no banco dianteiro");return;}
                float distance=player.Position.DistanceTo(Pos(Pending.Target));
                if(Pending.Stage==GangDrivingStage.Target){
                    if(target==null && distance<=Pending.Options.SpawnRadius && unchecked(Game.GameTime-retryAt)>=0){
                        var point=FreePoint(Pos(Pending.Target),0);if(!point.HasValue){retryAt=Game.GameTime+3000;if(unchecked(Game.GameTime-warnAt)>=0){Notify.Show("Ponto do alvo sem piso/espaco seguro. Afaste o carro do marcador e aguarde; pode suspender/cancelar no F4.");warnAt=Game.GameTime+15000;}}else{
                            target=World.CreatePed(targetModel,point.Value);if(!Alive(target) || !target.IsHuman)throw new InvalidOperationException("Driving target unavailable.");Setup(target,targetGroup);Function.Call(Hash.CLEAR_ENTITY_LAST_DAMAGE_ENTITY,target);targetBlip=target.AddBlip();targetBlip.Name="RPCore: alvo rival da iniciacao";targetBlip.Color=BlipColor.Red;
                        }
                    }
                    if(target!=null){
                        if(!target.Exists() || distance>Pending.Options.TargetRadius){Fail("alvo desapareceu ou abandono da area");return;}
                        bool pd=Function.Call<bool>(Hash.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY,target,player,true),vd=Function.Call<bool>(Hash.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY,target,car,true);
                        if(pd || vd){Fail("o motorista atingiu o alvo");return;}
                        if(target.IsDead){int killer=Function.Call<int>(Hash.GET_PED_SOURCE_OF_DEATH,target);if(!GangDrivingRules.CrewKill(true,killer,crew.Handle,player.Handle,car.Handle,pd,vd)){Fail("alvo nao eliminado pelo integrante");return;}crew.Task.ClearAll();crew.Weapons.RemoveAll();Advance();if(targetBlip!=null && targetBlip.Exists())targetBlip.Delete();Notify.Show("Alvo eliminado pelo integrante. Afaste-se "+Pending.Options.EscapeRadius+"m e retorne com ele.");}
                        else if(distance<=Pending.Options.AttackRadius && unchecked(Game.GameTime-nextTask)>=0){Function.Call(Hash.TASK_VEHICLE_SHOOT_AT_PED,crew,target,20f);nextTask=Game.GameTime+1500;}
                    }
                }else if(Pending.Stage==GangDrivingStage.Escape){if(GangDrivingRules.Escaped(distance,Driver,Passenger,Pending.Options))Advance();}
                else if(Pending.Stage==GangDrivingStage.Return && Game.Player.Wanted.WantedLevel==0 && GangDrivingRules.Returned(player.Position.DistanceTo(Pos(Pending.Home)),crew.Position.DistanceTo(Pos(Pending.Home)),car.Speed,Driver,Passenger)){Advance();ClearActors();Notify.Show("Retorno confirmado. Desembarque e confirme filiacao no lider com E.");}
            }
            if(Pending!=null)GTA.UI.Screen.ShowSubtitle("Direcao / "+Pending.Gang+" / "+Pending.Stage+" / "+(Pending.Stage==GangDrivingStage.Escape?"afaste-se do alvo":Pending.Stage==GangDrivingStage.Return?"volte a HQ e perca o procurado":"voce dirige; o integrante atira")+" / END suspende",100);
        }
        public void Dispose()=>Suspend();
    }
}
