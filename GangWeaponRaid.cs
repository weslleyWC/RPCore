using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
namespace RPCore
{
    internal sealed class GangWeaponRaid
    {
        const string Owner="gang_weapon_raid",Decor="RPCore_WeaponRaidOwner";
        readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Func<IEnumerable<GangDefinition>> gangs;readonly Func<IEnumerable<GangTurfDefinition>> areas;
        readonly GangWeaponRaidOptions options;readonly GangArmsOptions arms;readonly GangWarehouseOptions warehouse;readonly Random random=new Random();
        readonly List<Ped> guards=new List<Ped>();readonly List<Blip> enemyBlips=new List<Blip>();
        sealed class Retiring {public Vehicle Vehicle;public int Token;}
        readonly List<Retiring> retiring=new List<Retiring>();
        Vehicle van;Model vanModel;Model[] guardModels;Blip objective;int token,player,last,elapsed,prepareAt;string profile,id;bool preparing,spawned,entered;
        public bool Active {get;private set;}
        GangRaidAttempt Pending=>Wallet.GangRaid?.Pending?.Kind==GangRaidKind.WeaponVan?Wallet.GangRaid.Pending:null;
        GangTurfDefinition CurrentArea=>Pending==null?null:areas().FirstOrDefault(a=>a.Id==Pending.Area.Id);
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        static Vector3 Pos(GangTurfDefinition p)=>new Vector3(p.Center.X,p.Center.Y,p.Center.Z);
        bool WorldSafe=>Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool Input=>!Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
        bool Safe=>WorldSafe && Input && Game.Player.Wanted.WantedLevel==0 && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll;
        bool Owned=>Pending!=null && GangCrewRules.SameGang(state().Gang,Pending.Gang) && GangBusinessTransactions.Owns(state(),CurrentArea) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang));
        bool Mark(Entity e){try{if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);return Function.Call<bool>(Hash.DECOR_SET_INT,e,Decor,token);}catch{return false;}}
        static bool Tagged(Entity e,int value)=>e!=null && e.Exists() && e.Handle!=Game.Player.Character.Handle && Function.Call<bool>(Hash.DECOR_EXIST_ON,e,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,e,Decor)==value;
        bool OwnVan=>Tagged(van,token);
        static bool ValidPed(string name){if(!GangPresenceRules.ModelName(name))return false;var m=new Model(name);return m.IsInCdImage && m.IsPed;}
        public GangWeaponRaid(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs,Func<IEnumerable<GangTurfDefinition>> areas)
        {
            this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;this.areas=areas;
            options=Store.LoadDef("Gangs/WeaponRaids.json",()=>new GangWeaponRaidOptions());if(options?.Valid!=true){options=new GangWeaponRaidOptions();Log.Write("Invalid weapon raid config; original retained, defaults in memory.");}
            arms=Store.LoadDef("Gangs/Arms.json",()=>new GangArmsOptions());if(arms?.Valid!=true)arms=new GangArmsOptions();
            warehouse=Store.LoadDef("Gangs/Warehouse.json",()=>new GangWarehouseOptions());if(warehouse?.Valid!=true)warehouse=new GangWarehouseOptions();
        }
        public void Controls(Action<string,string,Action> add,Action reopen)
        {
            var p=Pending;if(p==null)return;
            add("Van de armas pendente: "+p.Rival+" / "+p.Area.Name,p.VanDelivered?"Van devolvida salva. Retome a pe na area e E confirma arsenal.":"Guardas "+p.Defeated.Count+"/3 / "+(p.Package?"van tomada; dirija de volta":"recupere a van propria do encontro")+". Retomada parcial refaz encontro, sem trocar carga.",null);
            add("GPS do roubo de armas",p.VanDelivered?"Destino para confirmar carga devolvida.":"Volte a sua area para retomar o encontro parcial.",()=>{var a=Pos(p.Area);Function.Call(Hash.SET_NEW_WAYPOINT,a.X,a.Y);pool.HideAll();});
            add(Active?"Suspender roubo de armas":"Retomar roubo de armas","A pe e seguro na sua area. Parcial recria van/guardas e refaz provas; van ja devolvida permite confirmar sem respawn.",()=>{if(Active){Suspend();reopen();}else if(!Resume())Notify.Show("Confira posse, seguranca a pe na area, atividade e dados.");});
            add("Cancelar roubo de armas","Descarta somente carga nao creditada. Sem dinheiro/estorno. Arsenal anterior permanece, inclusive apos perder posse/filiacao.",()=>{if(gate.Owner!=null && gate.Owner!=Owner || !Wallet.GangRaidEnd(p.Id,false,out _)){Notify.Show("Cancelamento nao confirmado; contrato conservado.");return;}Suspend();reopen();});
        }
        public void AddStart(GangTurfDefinition area,Action<string,string,Action> add)
        {add("Roubar van de armas: "+area.Name,"Tres guardas hostis e uma van propria estacionada. Elimine os tres com prova, tome o volante e devolva a van para abastecer o arsenal. Sem taxa, premio financeiro ou armas pessoais.",()=>{if(!Begin(area))Notify.Show("Confira posse, area a pe, outro grupo/modelos, arsenal com espaco e contratos/atividade.");});}
        bool AtHome(GangTurfDefinition area)=>WorldSafe && !Game.IsPaused && !InterfaceOptions.ModalOpen && Game.Player.Character.IsOnFoot && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && Game.Player.Wanted.WantedLevel==0 && Game.Player.Character.Position.DistanceTo(Pos(area))<=warehouse.Radius;
        bool Begin(GangTurfDefinition area)
        {
            if(Active || Wallet.GangRaid?.Pending!=null || gate.Owner!=null || retiring.Count>=8 || !AtHome(area) || !GangBusinessTransactions.Owns(state(),area) || !gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang)))return false;
            var rival=gangs().Where(g=>!GangCrewRules.SameGang(g.Name,state().Gang)).OrderBy(g=>GangCrewRules.SameGang(g.Name,area.DefaultOwner)?0:1).FirstOrDefault(g=>(g.PedModels??Array.Empty<string>()).Any(ValidPed));if(rival==null)return false;
            var names=rival.PedModels.Where(ValidPed).ToArray();var p=new GangRaidAttempt{Kind=GangRaidKind.WeaponVan,Id=Guid.NewGuid().ToString("N"),Gang=state().Gang,Rival=rival.Name,Area=CharacterRules.Copy(area),Site=CharacterRules.Copy(options.Sites[random.Next(options.Sites.Count)]),WeaponOptions=CharacterRules.Copy(options),Capacity=arms.MaxStock,Models=Enumerable.Range(0,3).Select(i=>names[i%names.Length]).ToArray(),Loot=GangWeaponRaidRules.Loot(random)};
            if(!gate.Acquire(Owner))return false;try{if(!Wallet.GangWeaponRaidBegin(state(),p,arms,out bool changed) || !changed){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}
        }
        bool Resume()
        {
            if(Active || Pending==null || Wallet.GangRaid?.Valid!=true || gate.Owner!=null || retiring.Count>=8 || !Owned || !AtHome(Pending.Area))return false;
            if(!gate.Acquire(Owner))return false;try{if(!Pending.VanDelivered && !Wallet.GangWeaponRaidRestart(Pending.Id,out _)){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}
        }
        void Activate(){Active=true;profile=Profile.Id;id=Pending.Id;player=Game.Player.Character.Handle;last=Game.GameTime;elapsed=0;token=random.Next(1,int.MaxValue);spawned=preparing=entered=false;pool.HideAll();Route(Pending.VanDelivered?Pos(Pending.Area):Pos(Pending.Site));Notify.Show(Pending.VanDelivered?"Van devolvida salva. E no ponto da area confirma arsenal.":"Siga o GPS. Tres guardas defendem a van de armas. END suspende; policia nativa ativa.");}
        void Route(Vector3 p){if(objective!=null && objective.Exists())objective.Delete();objective=MapBlips.Create(p,"RPCore: van de armas",BlipSprite.Standard,BlipColor.Yellow,.8f);objective.ShowRoute=true;}
        void Prepare()
        {
            vanModel=new Model(Pending.WeaponOptions.VehicleModel);guardModels=Pending.Models.Select(n=>new Model(n)).ToArray();
            if(!vanModel.IsInCdImage || !vanModel.IsCar || guardModels.Any(m=>!m.IsInCdImage || !m.IsPed)){Suspend();Notify.Show("Modelos indisponiveis; contrato conservado.");return;}
            vanModel.Request();foreach(var m in guardModels)m.Request();preparing=true;prepareAt=Game.GameTime;
        }
        Vector3? Ground(Vector3 p,float expected,float radius)
        {float z;if(!World.GetGroundHeight(p+new Vector3(0,0,2),out z,GetGroundHeightMode.Normal) || Math.Abs(z-expected)>5 || !JobActors.Free(new Vector3(p.X,p.Y,z),radius))return null;return new Vector3(p.X,p.Y,z+.1f);}
        void Spawn()
        {
            var site=Pos(Pending.Site);var road=World.GetNextPositionOnStreet(site);if(road==Vector3.Zero || road.DistanceTo(site)>60){Suspend();Notify.Show("Rua de encontro indisponivel; contrato conservado.");return;}
            var pos=Ground(road,site.Z,5);if(pos==null){Suspend();Notify.Show("Sem espaco/chao para van. Retome ou cancele pelo F4.");return;}
            var spots=new List<Vector3>();for(int i=0;i<3;i++){Vector3? chosen=null;for(int n=0;n<12;n++){double angle=(i*4+n)*Math.PI/6;var candidate=pos.Value+new Vector3((float)Math.Cos(angle)*7,(float)Math.Sin(angle)*7,0);var point=Ground(candidate,pos.Value.Z,2);if(point!=null && spots.All(p=>p.DistanceTo(point.Value)>=2.5f)){chosen=point;break;}}if(chosen==null){Suspend();Notify.Show("Sem espaco para escolta; contrato conservado.");return;}spots.Add(chosen.Value);}
            try{
                var created=World.CreateVehicle(vanModel,pos.Value,Pending.Site.Heading);if(created==null || !created.Exists())throw new InvalidOperationException("Weapon van unavailable");if(!Mark(created)){created.Delete();throw new InvalidOperationException("Weapon van tag unavailable");}van=created;van.IsPersistent=true;JobVehicles.Reserve(van);Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY,van,5f);Function.Call(Hash.SET_VEHICLE_DOORS_LOCKED,van,1);
                for(int i=0;i<3;i++){var p=World.CreatePed(guardModels[i],spots[i],Pending.Site.Heading);if(p==null || !p.Exists())throw new InvalidOperationException("Weapon guard unavailable");if(!Mark(p)){p.Delete();throw new InvalidOperationException("Weapon guard tag unavailable");}guards.Add(p);p.IsPersistent=true;p.BlockPermanentEvents=true;p.Health=200;p.Armor=50;p.Accuracy=35;p.RelationshipGroup=new RelationshipGroup((int)RelationshipGroupHash.CivilianMale);p.Weapons.RemoveAll();p.Weapons.Give(WeaponHash.Pistol,60,true,true);Function.Call(Hash.SET_PED_MONEY,p,0);Function.Call(Hash.SET_PED_DROPS_WEAPONS_WHEN_DEAD,p,false);Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES,p,(int)CombatAttributes.CanCommandeerVehicles,false);Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES,p,46,true);var b=p.AddBlip();b.Name="RPCore: guarda da van";b.Color=BlipColor.Red;b.Sprite=BlipSprite.Enemy;b.Scale=.65f;enemyBlips.Add(b);}
                foreach(var p in guards)Function.Call(Hash.TASK_COMBAT_PED,p,Game.Player.Character,0,16);spawned=true;preparing=false;Route(van.Position);
            }catch{Suspend();throw;}
        }
        void Fail(string reason){if(Pending!=null && !Wallet.GangRaidEnd(Pending.Id,true,out _))throw new InvalidOperationException("Weapon raid failure not saved");Suspend();Notify.Show("Roubo de armas falhou: "+reason+". Arsenal anterior conservado.");}
        public void Tick()
        {
            CleanupRetiring();if(!Active)return;
            if(Pending==null || Wallet.GangRaid?.Valid!=true || Pending.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Owned || Game.IsMissionActive || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE)){Suspend();return;}
            if(Game.Player.Character.IsDead){Fail("morte");return;}if(!Game.Player.Character.IsHuman){Suspend();return;}
            uint delta=unchecked((uint)(Game.GameTime-last));last=Game.GameTime;if(Input && delta<=2000)elapsed+=(int)delta;
            if(!Pending.VanDelivered && elapsed>=Pending.WeaponOptions.SecondsLimit*1000){Fail("prazo ativo");return;}
            var actor=Game.Player.Character;var context=actor.Position;WorkContext.ShopDelivery.Set(Owner,new JobPoint{Name="weapon raid",X=context.X,Y=context.Y,Z=context.Z},5);
            if(Pending.VanDelivered){if(Input)GTA.UI.Screen.ShowHelpTextThisFrame("Van devolvida / E a pe no ponto confirma arsenal / END suspende");return;}
            if(Game.IsPaused)return;
            if(!spawned && !preparing && Input && actor.Position.DistanceTo(Pos(Pending.Site))<70){Prepare();return;}
            if(preparing){if(!Input){prepareAt=Game.GameTime;return;}if(unchecked(Game.GameTime-prepareAt)>10000){Suspend();Notify.Show("Preparacao da van suspensa; retome pelo F4.");return;}if(vanModel.IsLoaded && guardModels.All(m=>m.IsLoaded))Spawn();return;}
            if(!spawned)return;
            if(!OwnVan){Suspend();Notify.Show("Van desapareceu ou perdeu identidade. Retome para refazer encontro.");return;}if(van.IsDead || !van.IsDriveable){Fail("van destruida/inutilizada");return;}
            float distance=actor.Position.DistanceTo(van.Position);if(distance<=Pending.WeaponOptions.Radius)entered=true;
            if(!Pending.Package && entered && distance>Pending.WeaponOptions.Radius){Fail("abandono da van antes da tomada");return;}
            if(!Pending.Package)
            {
                if(guards.Count!=3 || guards.Any(p=>!Tagged(p,token))){Suspend();Notify.Show("Escolta desapareceu; retome para refazer provas.");return;}
                for(int i=0;i<3;i++)if(!Pending.Defeated.Contains(i) && guards[i].IsDead){if(!Function.Call<bool>(Hash.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY,guards[i],actor,true)){Suspend();Notify.Show("Abate sem prova do jogador; encontro suspenso.");return;}if(!Wallet.GangRaidDefeat(id,i,out _))throw new InvalidOperationException("Weapon guard proof not saved");}
                if(Input && Pending.Defeated.Count==3 && van.Driver==actor && actor.IsInVehicle(van)){if(!Wallet.GangWeaponRaidCapture(id,out _))throw new InvalidOperationException("Weapon van capture not saved");ClearGuards();Route(Pos(Pending.Area));Notify.Show("Van tomada. Despiste a policia e devolva dirigindo a ate15m da sua area.");}
            }
            if(Pending.Package && Safe && actor.IsInVehicle(van) && van.Driver==actor && van.Speed<.5f && van.Position.DistanceTo(Pos(Pending.Area))<=15){if(!Wallet.GangWeaponRaidDeliver(id,out _))throw new InvalidOperationException("Weapon van delivery not saved");Route(Pos(Pending.Area));Notify.Show("Van devolvida e prova salva. Saia e E no ponto da area confirma o arsenal.");return;}
            if(Input)GTA.UI.Screen.ShowSubtitle(Pending.Package?"Dirija a van roubada de volta / pare sem procurado a ate15m / END suspende":"Guardas "+Pending.Defeated.Count+"/3 / tome o volante da van apos os tres / END suspende",100);
        }
        public void Interact()
        {
            if(!Active || !Safe || !Owned || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Pending.VanDelivered || !Game.Player.Character.IsOnFoot || Game.Player.Character.Position.DistanceTo(Pos(Pending.Area))>3)return;
            string loot=string.Join(", ",Pending.Loot.Select(p=>p.Value+"x "+p.Key));if(Wallet.GangWeaponRaidComplete(state(),CurrentArea,arms,id,out bool changed)){Suspend();Notify.Show(changed?"Arsenal abastecido: "+loot+". Sem dinheiro ou armas pessoais.":"Carga ja creditada; sem estoque repetido.");}else Notify.Show("Credito nao confirmado. Confira catalogo/espaco/limites; van devolvida permanece salva.");
        }
        void ClearGuards(){foreach(var b in enemyBlips)if(b!=null && b.Exists())b.Delete();enemyBlips.Clear();foreach(var p in guards)if(Tagged(p,token))p.Delete();guards.Clear();}
        void CleanupRetiring()
        {
            for(int i=retiring.Count-1;i>=0;i--){var r=retiring[i];if(!Tagged(r.Vehicle,r.Token)){JobVehicles.Release(r.Vehicle);retiring.RemoveAt(i);continue;}if(GangWeaponRaidRules.CanRetire(true,r.Vehicle.Occupants.Length,r.Vehicle.Speed)){JobVehicles.Release(r.Vehicle);r.Vehicle.Delete();retiring.RemoveAt(i);}}
        }
        public void Suspend()
        {
            ClearGuards();if(OwnVan)retiring.Add(new Retiring{Vehicle=van,Token=token});else JobVehicles.Release(van);van=null;CleanupRetiring();
            if(vanModel!=null)vanModel.MarkAsNoLongerNeeded();vanModel=null;if(guardModels!=null)foreach(var m in guardModels)m.MarkAsNoLongerNeeded();guardModels=null;
            if(objective!=null && objective.Exists())objective.Delete();objective=null;WorkContext.ShopDelivery.Clear(Owner);Active=preparing=spawned=entered=false;profile=id=null;token=0;gate.Release(Owner);
        }
        public void Dispose(){Suspend();foreach(var r in retiring){if(Tagged(r.Vehicle,r.Token))r.Vehicle.MarkAsNoLongerNeeded();JobVehicles.Release(r.Vehicle);}retiring.Clear();}
    }
}
