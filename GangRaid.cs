using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
namespace RPCore
{
    internal sealed class GangRaid
    {
        const string Owner="gang_raid",Decor="RPCore_RaidOwner";int token;readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Func<IEnumerable<GangDefinition>> gangs;readonly Func<IEnumerable<GangTurfDefinition>> areas;
        readonly GangRaidOptions options;readonly GangWarehouseOptions warehouse;readonly Random random=new Random();readonly List<Ped> actors=new List<Ped>();readonly List<Blip> blips=new List<Blip>();readonly GangRaidSearch search=new GangRaidSearch();
        const string SearchDict="anim@gangops@facility@servers@bodysearch@",SearchClip="player_search";Ped animationOwner;bool animRequested,animPlaying;
        Model[] models;Blip objective;string profile,id;int player,last,elapsed,prepareAt;bool preparing,spawned;public DealerJob Dealer;public bool Active {get;private set;}
        GangRaidAttempt Pending=>Wallet.GangRaid?.Pending?.Kind==GangRaidKind.Drugs?Wallet.GangRaid.Pending:null;
        GangTurfDefinition CurrentArea=>Pending==null?null:areas().FirstOrDefault(a=>a.Id==Pending.Area.Id);
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);static Vector3 Pos(GangTurfDefinition a)=>new Vector3(a.Center.X,a.Center.Y,a.Center.Z);
        bool WorldSafe=>Game.Player.Character.IsHuman && !Game.Player.Character.IsDead && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool Owned=>Pending!=null && GangCrewRules.SameGang(Pending.Gang,state().Gang) && GangBusinessTransactions.Owns(state(),CurrentArea) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang));
        bool Input=>!Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
        public GangRaid(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs,Func<IEnumerable<GangTurfDefinition>> areas)
        {this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;this.areas=areas;options=Store.LoadDef("Gangs/Raids.json",()=>new GangRaidOptions());if(options==null || !options.Valid){options=new GangRaidOptions();Log.Write("Invalid raid config; original retained, defaults used.");}warehouse=Store.LoadDef("Gangs/Warehouse.json",()=>new GangWarehouseOptions());if(warehouse==null || !warehouse.Valid)warehouse=new GangWarehouseOptions();}
        public void Controls(Action<string,string,Action> add,Action reopen)
        {
            add("Roubos concluidos: "+(Wallet.GangRaid?.Completed??0)+" / falhas: "+(Wallet.GangRaid?.Failed??0),"Somente estoque confirmado no retorno conta como conclusao. Cancelar/suspender conserva estoque anterior.",null);
            var p=Pending;if(p==null)return;
            add("Roubo pendente: "+p.Rival+" / "+p.Area.Name,p.Package?"Carga recuperada. Retome em seguranca a pe e volte ao territorio; entrega exige sem procurado.":"Rivais abatidos: "+p.Defeated.Count+"/3. Retomada parcial a pe no seu territorio refaz os tres, sem sortear outro lote.",null);
            add(Active?"Suspender roubo":"Retomar roubo", "Conserva contrato e carga confirmada; nao concede estoque ao suspender.",()=>{if(Active){Suspend();reopen();}else if(!Resume())Notify.Show("Confira posse, gangue, seguranca/proximidade, catalogo e atividade.");});
            add("Cancelar roubo pendente","Descarta apenas carga de missao ainda nao entregue. Sem taxa/estorno; estoque anterior permanece.",()=>{if(gate.Owner!=null && gate.Owner!=Owner || !Wallet.GangRaidEnd(p.Id,false,out _)){Notify.Show("Cancelamento nao confirmado; dados conservados.");return;}Suspend();reopen();});
        }
        public void AddStart(GangTurfDefinition area,Action<string,string,Action> add)
        {add("Roubar suprimentos: "+area.Name,"Um traficante e dois guardas rivais proprios. Elimine os tres, E revista o corpo por "+options.SearchSeconds+"s e devolva dois produtos ao armazem. Sem taxa; policia nativa ativa. Morte/abandono/prazo perdem carga nao entregue.",()=>{if(!Begin(area))Notify.Show("Confira posse, gangue rival/modelos, catalogo, espaco para dois produtos e seguranca a pe no territorio.");});}
        bool AtHome(GangTurfDefinition area)=>WorldSafe && !Game.IsPaused && !InterfaceOptions.ModalOpen && Game.Player.Character.IsOnFoot && !Game.Player.Character.IsInCombat && Game.Player.Wanted.WantedLevel==0 && Game.Player.Character.Position.DistanceTo(Pos(area))<=warehouse.Radius;
        bool Begin(GangTurfDefinition area)
        {
            var c=Dealer?.Catalog;if(Active || Wallet.GangRaid?.Pending!=null || gate.Owner!=null || Wallet.GangBusiness?.Supply!=null || !GangBusinessTransactions.Owns(state(),area) || !gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang)) || !AtHome(area) || c==null || !c.Valid)return false;
            var rival=gangs().Where(g=>!GangCrewRules.SameGang(g.Name,state().Gang)).OrderBy(g=>GangCrewRules.SameGang(g.Name,area.DefaultOwner)?0:1).FirstOrDefault(g=>(g.PedModels??Array.Empty<string>()).Any(ValidModel));
            var products=c.Products.Where(p=>p.Id.Length<=80).ToList();if(rival==null || products.Count<2)return false;
            var names=rival.PedModels.Where(ValidModel).ToArray();var loot=products.OrderBy(p=>random.Next()).Take(2).ToDictionary(p=>p.Id,p=>random.Next(options.MinLoot,options.MaxLoot+1));
            var attempt=new GangRaidAttempt{Id=Guid.NewGuid().ToString("N"),Gang=state().Gang,Rival=rival.Name,Area=CharacterRules.Copy(area),Site=CharacterRules.Copy(options.Sites[random.Next(options.Sites.Count)]),Options=CharacterRules.Copy(options),Capacity=warehouse.Capacity,Models=Enumerable.Range(0,3).Select(i=>names[i%names.Length]).ToArray(),Loot=loot};
            if(!gate.Acquire(Owner))return false;
            try{if(!Wallet.GangRaidBegin(state(),attempt,c,out bool changed) || !changed){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}
        }
        bool Mark(Ped p){try{if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);return Function.Call<bool>(Hash.DECOR_SET_INT,p,Decor,token);}catch{return false;}}
        bool OwnedActor(Ped p)=>p!=null && p.Exists() && p.Handle!=(Game.Player.Character?.Handle??0) && Function.Call<bool>(Hash.DECOR_EXIST_ON,p,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,p,Decor)==token;
        static bool ValidModel(string name){if(!GangPresenceRules.ModelName(name))return false;var m=new Model(name);return m.IsInCdImage && m.IsPed;}
        bool Resume()
        {
            if(Active || Pending==null || Wallet.GangRaid?.Valid!=true || gate.Owner!=null || !Owned || !WorldSafe || Game.IsPaused || InterfaceOptions.ModalOpen || !Game.Player.Character.IsOnFoot || Game.Player.Character.IsInCombat || Game.Player.Wanted.WantedLevel>0 || Dealer?.Catalog==null || !Pending.Package && !AtHome(Pending.Area))return false;
            if(!gate.Acquire(Owner))return false;
            try{if(!Pending.Package && !Wallet.GangRaidRestart(Pending.Id,out _)){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}
        }
        void Activate()
        {Active=true;profile=Profile.Id;id=Pending.Id;player=Game.Player.Character.Handle;last=Game.GameTime;elapsed=0;token=random.Next(1,int.MaxValue);preparing=spawned=false;pool.HideAll();Route(Pending.Package?Pos(Pending.Area):Pos(Pending.Site),Pending.Package?"Devolver carga roubada":"Suprimentos rivais");Notify.Show(Pending.Package?"Carga recuperada salva. Volte a pe ao ponto sem procurado para E entregar.":"Siga o GPS. Um traficante e dois guardas hostis defendem a carga. END suspende.");}
        void Route(Vector3 target,string name)
        {if(objective!=null && objective.Exists())objective.Delete();objective=MapBlips.Create(target,"RPCore: "+name,BlipSprite.Standard,BlipColor.Yellow,.8f);objective.ShowRoute=true;}
        Vector3? Spot(int index,List<Vector3> chosen)
        {
            var center=Pos(Pending.Site);for(int n=0;n<12;n++){double angle=(index*4+n)*Math.PI/6;var point=center+new Vector3((float)Math.Cos(angle)*(2+index*2),(float)Math.Sin(angle)*(2+index*2),0);float z;
                if(!World.GetGroundHeight(point+new Vector3(0,0,2),out z,GetGroundHeightMode.Normal) || Math.Abs(z-center.Z)>3 || !JobActors.Free(new Vector3(point.X,point.Y,z),2) || chosen.Any(p=>p.DistanceTo(point)<2.5f))continue;return new Vector3(point.X,point.Y,z+.05f);}return null;
        }
        void Spawn()
        {
            var positions=new List<Vector3>();for(int i=0;i<3;i++){var p=Spot(i,positions);if(p==null){Suspend();Notify.Show("Sem espaco/chao seguro no encontro. Roubo suspenso; retome ou cancele no F4.");return;}positions.Add(p.Value);}
            try{for(int i=0;i<3;i++){var p=World.CreatePed(models[i],positions[i]);if(p==null || !p.Exists())throw new InvalidOperationException("Raid actor unavailable");if(!Mark(p)){p.Delete();throw new InvalidOperationException("Raid actor ownership tag unavailable");}actors.Add(p);p.IsPersistent=true;p.BlockPermanentEvents=true;p.Health=200;p.Weapons.RemoveAll();p.RelationshipGroup=new RelationshipGroup((int)RelationshipGroupHash.CivilianMale);Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES,p,(int)CombatAttributes.CanCommandeerVehicles,false);Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES,p,46,true);p.Armor=i==0?25:35;p.Accuracy=30;p.Weapons.Give(WeaponHash.Pistol,45,true,true);Function.Call(Hash.SET_PED_DROPS_WEAPONS_WHEN_DEAD,p,false);Function.Call(Hash.SET_PED_MONEY,p,0);var b=p.AddBlip();b.Name=i==0?"RPCore: traficante rival":"RPCore: guarda rival";b.Color=BlipColor.Red;b.Sprite=BlipSprite.Enemy;b.Scale=.65f;blips.Add(b);}
                foreach(var p in actors)Function.Call(Hash.TASK_COMBAT_PED,p,Game.Player.Character,0,16);spawned=true;preparing=false;
            }catch{Suspend();throw;}
        }
        void Fail(string reason)
        {if(Pending!=null && !Wallet.GangRaidEnd(Pending.Id,true,out _))throw new InvalidOperationException("Raid failure not saved");Suspend();Notify.Show("Roubo falhou: "+reason+". Carga nao entregue perdida; estoque anterior conservado.");}
        bool SearchEligible=>spawned && actors.Count==3 && OwnedActor(actors[0]) && actors[0].IsDead && Game.Player.Character.IsOnFoot && !Game.Player.Character.IsRagdoll && Game.Player.Character.Position.DistanceTo(actors[0].Position)<=2.5f && Input;
        public void Tick()
        {
            if(!Active)return;
            if(Pending==null || Wallet.GangRaid?.Valid!=true || Pending.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Owned || Game.IsMissionActive || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE)){Suspend();return;}
            if(Game.Player.Character.IsDead){Fail("morte");return;}if(!Game.Player.Character.IsHuman){Suspend();return;}
            uint delta=unchecked((uint)(Game.GameTime-last));last=Game.GameTime;if(Input && delta<=2000)elapsed+=(int)delta;
            if(elapsed>=Pending.Options.SecondsLimit*1000){Fail("prazo ativo encerrado");return;}
            var point=Game.Player.Character.Position;WorkContext.ShopDelivery.Set(Owner,new JobPoint{Name="gang raid",X=point.X,Y=point.Y,Z=point.Z},5);
            if(Game.IsPaused)return;
            if(Pending.Package){if(Input)GTA.UI.Screen.ShowHelpTextThisFrame(Game.Player.Wanted.WantedLevel>0?"Despiste a policia antes de entregar a carga":"Volte a pe ao ponto da sua area / E entrega / END suspende");return;}
            if(!spawned && !preparing && Input && point.DistanceTo(Pos(Pending.Site))<70){models=Pending.Models.Select(n=>new Model(n)).ToArray();if(models.Any(m=>!m.IsInCdImage || !m.IsPed)){Suspend();Notify.Show("Modelo rival indisponivel. Contrato conservado.");return;}foreach(var m in models)Function.Call(Hash.REQUEST_MODEL,m.Hash);preparing=true;prepareAt=Game.GameTime;}
            if(preparing){if(!Input){prepareAt=Game.GameTime;return;}if(unchecked(Game.GameTime-prepareAt)>10000){Suspend();Notify.Show("Preparacao do encontro suspensa; retome no F4.");return;}if(models.All(m=>m.IsLoaded))Spawn();return;}
            if(!spawned)return;
            if(point.DistanceTo(Pos(Pending.Site))>Pending.Options.Radius){Fail("abandono do encontro antes de recuperar carga");return;}
            if(actors.Any(p=>!OwnedActor(p))){Suspend();Notify.Show("Rival/corpo desapareceu. Retomada parcial refaz encontro; nada concedido.");return;}
            for(int i=0;i<3;i++)if(!Pending.Defeated.Contains(i) && actors[i].IsDead){if(!Function.Call<bool>(Hash.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY,actors[i],Game.Player.Character,true)){Suspend();Notify.Show("Abate sem prova do jogador. Retome para refazer encontro.");return;}if(!Wallet.GangRaidDefeat(id,i,out _))throw new InvalidOperationException("Raid proof not saved");if(Pending.Defeated.Count==3)Route(actors[0].Position,"Revistar traficante rival");}
            if(Pending.Defeated.Count==3)
            {
                if(search.Searching && SearchEligible && animRequested && !animPlaying && Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED,SearchDict)){Function.Call(Hash.TASK_PLAY_ANIM,animationOwner,SearchDict,SearchClip,4f,-4f,Pending.Options.SearchSeconds*1000,49,0f,false,false,false);animPlaying=true;}
                if(search.Step(Game.GameTime,SearchEligible,Pending.Options.SearchSeconds)){if(!Wallet.GangRaidCollect(id,out _,GangDiplomacyRules.Initial(gangs().FirstOrDefault(g=>GangCrewRules.SameGang(g.Name,Pending.Rival)),Pending.Gang)))throw new InvalidOperationException("Raid package not saved");StopAnimation();ClearActors();Route(Pos(Pending.Area),"Devolver carga roubada");Notify.Show("Carga recuperada: "+string.Join(", ",Pending.Loot.Select(p=>p.Value+"x "+p.Key))+". Relacao rival piorou; despiste a policia e volte a sua area.");return;}
                if(!search.Searching)StopAnimation();
                if(SearchEligible)GTA.UI.Screen.ShowHelpTextThisFrame(search.Searching?"Revistando... permaneca junto do corpo":"E: revistar traficante e recuperar suprimentos");
            }
            if(Input)GTA.UI.Screen.ShowSubtitle("Roubo / rivais "+Pending.Defeated.Count+"/3 / END suspende",100);
        }
        public void Interact()
        {
            if(!Active || !Input || !Owned || !WorldSafe || Pending?.Id!=id || gate.Owner!=Owner || profile!=Profile.Id || player!=Game.Player.Character.Handle)return;
            if(Pending.Package){if(Game.Player.Character.IsOnFoot && !Game.Player.Character.IsInCombat && Game.Player.Wanted.WantedLevel==0 && Game.Player.Character.Position.DistanceTo(Pos(Pending.Area))<=3){if(Wallet.GangRaidComplete(state(),CurrentArea,Dealer?.Catalog,id,out bool changed)){Suspend();Notify.Show(changed?"Suprimentos roubados entregues ao armazem. Estoque pessoal/dinheiro anteriores conservados.":"Entrega ja confirmada, sem estoque repetido.");}else Notify.Show("Entrega nao confirmada; confira espaco/catalogo e dados. Carga continua salva.");}return;}
            if(Pending.Defeated.Count==3 && SearchEligible && !search.Searching){StopAnimation();animationOwner=Game.Player.Character;Function.Call(Hash.REQUEST_ANIM_DICT,SearchDict);animRequested=true;search.Start(Game.GameTime);}
        }
        void StopAnimation(){if(animPlaying && animationOwner!=null && animationOwner.Exists())Function.Call(Hash.STOP_ANIM_TASK,animationOwner,SearchDict,SearchClip,2f);if(animRequested)Function.Call(Hash.REMOVE_ANIM_DICT,SearchDict);animationOwner=null;animPlaying=animRequested=false;}
        void ClearActors(){foreach(var b in blips)if(b!=null && b.Exists())b.Delete();blips.Clear();foreach(var p in actors)if(OwnedActor(p))p.Delete();actors.Clear();if(models!=null)foreach(var m in models)m.MarkAsNoLongerNeeded();models=null;spawned=preparing=false;}
        public void Suspend(){StopAnimation();ClearActors();if(objective!=null && objective.Exists())objective.Delete();objective=null;Active=false;token=0;profile=id=null;search.Step(Game.GameTime,false,2);WorkContext.ShopDelivery.Clear(Owner);gate.Release(Owner);}
    }
}
