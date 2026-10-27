using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
namespace RPCore
{
    internal sealed partial class GangTerritories : IDisposable
    {
        const string OwnerKey="gang_turf";readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Action<GangState> save;readonly Func<IEnumerable<GangDefinition>> gangs;readonly GangTurfOptions options;readonly GangDefenseOptions defense;readonly NativeMenu menu;
        readonly List<Ped> defenders=new List<Ped>();readonly List<Blip> enemyBlips=new List<Blip>();readonly Dictionary<int,Blip> map=new Dictionary<int,Blip>();Model[] models;GangTurfAttempt draft;Ped player;string profile,signature;bool active,preparing;int started,last;float seconds;Blip objective;
        GangTurfAttempt Pending=>state().Turf?.Pending;
        public bool HasPending=>Pending!=null;
        public bool Active=>active || preparing || convoyPreparing || convoyMoving;
        public Action OpenWarfare;
        static Vector3 Pos(GangSpot p)=>new Vector3(p.X,p.Y,p.Z);
        bool Safe=>Game.Player.Character.IsHuman && !Game.IsMissionActive && !Game.Player.Character.IsDead && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool InArea(GangTurfAttempt p)=>GangTurfRules.Inside(Game.Player.Character.Position.DistanceTo(Pos(p.Area.Center)),p.Radius);
        public GangTerritories(ShiftGate gate,ObjectPool pool,Func<GangState> state,Action<GangState> save,Func<IEnumerable<GangDefinition>> gangs)
        {this.gate=gate;this.pool=pool;this.state=state;this.save=save;this.gangs=gangs;options=Store.LoadDef("Gangs/Territories.json",GangTurfOptions.Defaults);if(options==null || !options.Valid){options=GangTurfOptions.Defaults();Log.Write("Invalid territory config; original retained, embedded defaults used.");}defense=Store.LoadDef("Gangs/Defense.json",()=>new GangDefenseOptions());if(defense==null || !defense.Valid){defense=new GangDefenseOptions();Log.Write("Invalid territory defense config; original retained, defaults used.");}menu=new NativeMenu("Territorios","MAPA / DISPUTAS");pool.Add(menu);Visuals.Register(menu,"gangs");InitAssault();}
        void Add(string title,string description,Action action,bool enabled=true)
        {var row=new NativeItem(title,description){Enabled=enabled};row.Activated+=(s,e)=>{try{action();}catch(Exception ex){Suspend();Log.Error("territory menu",ex);Notify.Show("Acao suspensa; dados gravados conservados.");}};menu.Add(row);}
        public void Open()
        {
            pool.HideAll();menu.Clear();
            if(state().Turf!=null && !state().Turf.Valid){Add("Dados de territorio protegidos","Confira GangState.json/backup; nenhum dono sera substituido.",()=>{});menu.Visible=true;return;}
            if(InterfaceOptions.AdvancedMenus)Add("Mapa de territorios: "+(state().TerritoriesEnabled?"LIGADO":"DESLIGADO"),"15 pontos da referencia; opcional por perfil. Desligar suspende a disputa sem apagar donos.",()=>{var next=CharacterRules.Copy(state());next.TerritoriesEnabled=!next.TerritoriesEnabled;save(next);if(!next.TerritoriesEnabled)Suspend();SyncMap();Open();});
            Add("Guerras / alertas e reforcos","Ataques automaticos opcionais, mapa dinamico e reforcos nas capturas.",()=>OpenWarfare?.Invoke());
            Add("Assalto organizado / comboio","Quatro carros e equipe propria: partir de area sob seu controle, escoltar e capturar alvo rival.",OpenAssault);
            Add("Capturas: "+(state().Turf?.Captures??0)+" / defesas: "+(state().Turf?.Defenses??0)+" / perdas: "+(state().Turf?.LostDefenses??0),"Contadores deste perfil; sem renda ou premio automatico.",()=>{});
            if(Pending!=null){Add((Active?"Suspender":"Retomar")+" disputa: "+Pending.Area.Name,"A pe dentro da area. Luta parcial refaz defensores; territorio pronto pode confirmar E apos reload.",()=>{if(Active)Suspend();else if(!Resume())Notify.Show("Confira gangue, area, mapa e atividade atual.");menu.Visible=false;});Add("Cancelar disputa pendente","Conserva dono anterior; nao cobra ou estorna dinheiro.",()=>{if(GangTurfTransactions.End(state(),Pending.Id,false,save,out _)){Suspend();Open();}});}
            foreach(var a in options.Areas){var area=a;string owner=GangTurfRules.Owner(state().Turf,area);bool own=GangCrewRules.SameGang(state().Gang,owner);
                Add("GPS: "+area.Name+" / "+owner,"Localizacao e dono atual deste perfil.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,area.Center.X,area.Center.Y);menu.Visible=false;});
                Add("Defender: "+area.Name,"Inicia "+defense.Waves+" ondas de "+defense.PerWave+" atacantes. MORTE/VEICULO/ABANDONO/TEMPO transferem posse ao atacante. Suspender/cancelar conserva dono. A pe na area, mapa ligado.",()=>{if(!BeginDefense(area))Notify.Show("Confira posse, gangues rivais/modelos, mapa, area e pendencias.");else menu.Visible=false;},own && !HasPending);
                Add((own?"Sob controle: ":"Disputar: ")+area.Name,"Afiliado, a pe dentro de "+options.Radius+"m, mapa ligado, sem procurado/atividade. "+options.Defenders+" defensores proprios; depois confirme E.",()=>{if(!Begin(area))Notify.Show("Confira filiacao, dono/modelos, proximidade, mapa e pendencias.");else menu.Visible=false;},!own && !HasPending);
            }
            Add("Fechar","Voltar ao jogo.",()=>menu.Visible=false);menu.Visible=true;
        }
        bool CanStart(GangTurfAttempt p)=>gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang)) && state().TerritoriesEnabled && Safe && !Game.IsPaused && !InterfaceOptions.ModalOpen && Game.Player.Character.IsOnFoot && !Game.Player.Character.IsInCombat && Game.Player.Wanted.WantedLevel==0 && InArea(p) && GangCrewRules.SameGang(state().Gang,p.Gang);
        bool Begin(GangTurfDefinition area)
        {
            if(Active || HasPending || state().Loyalty?.Pending!=null || state().Strength?.Pending!=null || state().Driving?.Pending!=null || GangLoyaltyRules.Unaffiliated(state().Gang))return false;
            var own=gangs().FirstOrDefault(g=>GangCrewRules.SameGang(g.Name,state().Gang));var owner=GangTurfRules.Owner(state().Turf,area);var rival=gangs().FirstOrDefault(g=>GangCrewRules.SameGang(g.Name,owner));if(own==null || rival==null || GangCrewRules.SameGang(owner,state().Gang))return false;
            var names=(rival.PedModels??Array.Empty<string>()).Where(GangPresenceRules.ModelName).Where(n=>{var m=new Model(n);return m.IsInCdImage && m.IsPed;}).ToArray();if(names.Length==0)return false;
            var p=new GangTurfAttempt{Id=Guid.NewGuid().ToString("N"),Gang=state().Gang,PreviousOwner=owner,Area=CharacterRules.Copy(area),Radius=options.Radius,SecondsLimit=options.SecondsLimit,InitialDefenders=state().Warfare?.Valid==true && state().Warfare.Reinforcements?Math.Min(3,options.Defenders):0,Models=Enumerable.Range(0,state().Warfare?.Valid==true && state().Warfare.Reinforcements?Math.Min(3,options.Defenders)+3:options.Defenders).Select(i=>names[i%names.Length]).ToArray()};
            return CanStart(p) && Prepare(p);
        }
        bool BeginDefense(GangTurfDefinition area)
        {
            if(Active || HasPending || state().Loyalty?.Pending!=null || state().Strength?.Pending!=null || state().Driving?.Pending!=null || GangLoyaltyRules.Unaffiliated(state().Gang) || !GangCrewRules.SameGang(GangTurfRules.Owner(state().Turf,area),state().Gang))return false;
            var rivals=gangs().Where(g=>!GangCrewRules.SameGang(g.Name,state().Gang)).OrderBy(g=>GangCrewRules.SameGang(g.Name,area.DefaultOwner)?0:1);
            foreach(var rival in rivals){var names=(rival.PedModels??Array.Empty<string>()).Where(GangPresenceRules.ModelName).Where(n=>{var m=new Model(n);return m.IsInCdImage && m.IsPed;}).ToArray();if(names.Length==0)continue;
                var p=new GangTurfAttempt{Id=Guid.NewGuid().ToString("N"),Gang=state().Gang,PreviousOwner=state().Gang,Attacker=rival.Name,Mode=GangTurfMode.Defense,WaveSize=defense.PerWave,Area=CharacterRules.Copy(area),Radius=options.Radius,SecondsLimit=options.SecondsLimit,Models=Enumerable.Range(0,defense.PerWave*defense.Waves).Select(i=>names[i%names.Length]).ToArray()};return CanStart(p) && Prepare(p);
            }return false;
        }
        public bool RespondToAlert()
        {
            var a=state().Warfare?.Alert;
            if(a?.Valid!=true || Active || HasPending || !state().Warfare.AttacksEnabled || state().Warfare.Valid!=true)return false;
            var rival=gangs().FirstOrDefault(g=>GangCrewRules.SameGang(g.Name,a.Attacker));
            var names=(rival?.PedModels??Array.Empty<string>()).Where(GangPresenceRules.ModelName).Where(n=>{var m=new Model(n);return m.IsInCdImage && m.IsPed;}).ToArray();if(names.Length==0)return false;
            var p=new GangTurfAttempt{Id=Guid.NewGuid().ToString("N"),WarAlertId=a.Id,Gang=a.Gang,PreviousOwner=a.Gang,Attacker=a.Attacker,Mode=GangTurfMode.Defense,WaveSize=defense.PerWave,Area=CharacterRules.Copy(a.Area),Radius=options.Radius,SecondsLimit=options.SecondsLimit,Models=Enumerable.Range(0,defense.PerWave*defense.Waves).Select(i=>names[i%names.Length]).ToArray()};
            return CanStart(p) && Prepare(p);
        }
        bool Resume()
        {
            if(!Active && Pending?.Assault!=null && !Pending.Ready)return ResumeAssault();
            if(Active || Pending==null || state().Turf.Valid!=true || !CanStart(Pending) || state().Loyalty?.Pending!=null || state().Strength?.Pending!=null || state().Driving?.Pending!=null)return false;
            if(Pending.Ready){if(!gate.Acquire(OwnerKey))return false;active=true;profile=Profile.Id;menu.Visible=false;Route();return true;}
            return Prepare(CharacterRules.Copy(Pending));
        }
        bool Prepare(GangTurfAttempt p)
        {if(!p.Valid || !gate.Acquire(OwnerKey))return false;draft=p;profile=Profile.Id;player=Game.Player.Character;models=p.Models.Select(n=>new Model(n)).ToArray();if(models.Any(m=>!m.IsInCdImage || !m.IsPed)){Suspend();return false;}foreach(var m in models)Function.Call(Hash.REQUEST_MODEL,m.Hash);preparing=true;started=Game.GameTime;pool.HideAll();return true;}
        Vector3? Spot(int index)
        {
            if(draft.Mode==GangTurfMode.Defense){
                float radius=Math.Max(10,draft.Radius-8);for(int n=0;n<12;n++){
                    double a=(index*2+n)*Math.PI/6;var p=Pos(draft.Area.Center)+new Vector3((float)Math.Cos(a)*radius,(float)Math.Sin(a)*radius,0);float z;
                    if(!World.GetGroundHeight(p+new Vector3(0,0,2),out z,GetGroundHeightMode.Normal) || Math.Abs(z-p.Z)>2 || !JobActors.Free(p,2) || !GangPresence.Space(new Vector3(p.X,p.Y,z)) || defenders.Any(d=>d.Exists() && d.Position.DistanceTo(p)<3))continue;return new Vector3(p.X,p.Y,z+.05f);
                }return null;
            }
            foreach(var point in draft.Area.Posts.Skip(index).Concat(draft.Area.Posts.Take(index))){var p=Pos(point);float floor;if(!GangTurfRules.Inside(p.DistanceTo(Pos(draft.Area.Center)),draft.Radius) || !World.GetGroundHeight(p+new Vector3(0,0,2),out floor,GetGroundHeightMode.Normal) || Math.Abs(floor-p.Z)>2 || !JobActors.Free(p,2) || !GangPresence.Space(new Vector3(p.X,p.Y,floor)))continue;if(defenders.Any(d=>d.Position.DistanceTo(p)<2.5f))continue;return new Vector3(p.X,p.Y,floor+.05f);}return null;
        }
        void SpawnBatch(int start,int count)
        {
            for(int i=start;i<start+count;i++){
                var point=Spot(i);if(!point.HasValue)throw new InvalidOperationException("No safe territory wave slot.");
                var p=World.CreatePed(models[i],point.Value);if(p==null || !p.Exists())throw new InvalidOperationException("Territory defender unavailable.");defenders.Add(p);if(!p.IsHuman)throw new InvalidOperationException("Territory defender is not human.");
                p.IsPersistent=true;p.BlockPermanentEvents=true;p.Health=200;p.Armor=0;p.Weapons.RemoveAll();p.RelationshipGroup=new RelationshipGroup((int)RelationshipGroupHash.CivilianMale);p.Task.StandStill(-1);p.Accuracy=35;Function.Call(Hash.SET_PED_MONEY,p,0);Function.Call(Hash.SET_PED_DROPS_WEAPONS_WHEN_DEAD,p,false);Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES,p,(int)CombatAttributes.CanCommandeerVehicles,false);
                var b=p.AddBlip();enemyBlips.Add(b);b.Color=BlipColor.Red;b.Sprite=BlipSprite.Enemy;b.Name=(draft.Mode==GangTurfMode.Defense?"RPCore: atacante ":"RPCore: defensor ")+(i+1);
            }
        }
        void FightBatch(int start){foreach(var p in defenders.Skip(start)){p.Weapons.Give(WeaponHash.Pistol,120,true,true);Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES,p,46,true);Function.Call(Hash.TASK_COMBAT_PED,p,player,0,16);}}
        void Spawn()
        {
            SpawnBatch(0,draft.Mode==GangTurfMode.Defense?draft.WaveSize:draft.InitialDefenders>0?draft.InitialDefenders:models.Length);
            if(Pending==null){if(!GangTurfTransactions.Begin(state(),draft,save,out _))throw new InvalidOperationException("Territory attempt not saved.");}else if(!GangTurfTransactions.Restart(state(),Pending.Id,save,out _))throw new InvalidOperationException("Territory restart not saved.");
            preparing=false;active=true;last=Game.GameTime;seconds=0;FightBatch(0);if(defenders.Count==draft.Models.Length)ReleaseModels();Route();Notify.Show(draft.Mode==GangTurfMode.Defense?"Defesa contra "+draft.Attacker+" iniciada. Elimine as ondas; perder transfere posse ao atacante. END suspende.":"Disputa iniciada: elimine os defensores e confirme E na area. END suspende.");
        }
        void Route(){if(objective!=null && objective.Exists())objective.Delete();objective=World.CreateBlip(Pos(Pending.Area.Center));objective.Name="RPCore disputa: "+Pending.Area.Name;objective.Color=BlipColor.Yellow;objective.ShowRoute=true;}
        void ReleaseModels(){if(models!=null)foreach(var m in models)m.MarkAsNoLongerNeeded();models=null;}
        void ClearDefenders(){foreach(var b in enemyBlips)if(b!=null && b.Exists())b.Delete();enemyBlips.Clear();foreach(var p in defenders)if(p!=null && p.Exists()){if(p.IsInVehicle()){p.BlockPermanentEvents=false;p.MarkAsNoLongerNeeded();}else{p.Weapons.RemoveAll();p.Delete();}}defenders.Clear();ReleaseModels();}
        public void Suspend(){try{StopAssault();}finally{ClearDefenders();if(objective!=null && objective.Exists())objective.Delete();objective=null;draft=null;player=null;profile=null;active=preparing=false;gate.Release(OwnerKey);}}
        void Fail(string reason){bool loss=Pending?.Mode==GangTurfMode.Defense;string attacker=Pending?.Attacker;if(Pending!=null && !GangTurfTransactions.End(state(),Pending.Id,true,save,out _))throw new InvalidOperationException("Territory failure not saved.");Suspend();signature=null;SyncMap();Notify.Show("Disputa encerrada: "+reason+(loss?". Territorio transferido a "+attacker+".":". Dono anterior conservado."));}
        public void Interact(){if(!active || Pending?.Ready!=true || !Safe || Game.IsPaused || !Game.Player.Character.IsOnFoot || !InArea(Pending) || pool.AreAnyVisible || InterfaceOptions.ModalOpen)return;bool defended=Pending.Mode==GangTurfMode.Defense;if(!GangTurfTransactions.Capture(state(),Pending.Id,save,out _))throw new InvalidOperationException("Territory ownership not saved.");Suspend();signature=null;SyncMap();Notify.Show((defended?"Defesa confirmada; territorio mantido por ":"Territorio capturado e gravado para ")+state().Gang+". Sem pagamento automatico.");}
        BlipColor ColorFor(string owner){var g=gangs().FirstOrDefault(a=>GangCrewRules.SameGang(a.Name,owner));return g?.GangColor=="Green"?BlipColor.Green:g?.GangColor=="Purple"?BlipColor.Purple:g?.GangColor=="Yellow"?BlipColor.Yellow:BlipColor.Blue;}
        void ClearMap(){foreach(var b in map.Values)if(b!=null && b.Exists())b.Delete();map.Clear();signature=null;}
        void SyncMap()
        {
            if(!state().TerritoriesEnabled || state().Turf!=null && !state().Turf.Valid){ClearMap();return;}
            string key=Profile.Id+"|"+string.Join("|",options.Areas.Select(a=>a.Id+":"+GangTurfRules.Owner(state().Turf,a)));if(key==signature && map.Count==options.Areas.Count && map.Values.All(b=>b!=null && b.Exists()))return;ClearMap();
            foreach(var a in options.Areas){var owner=GangTurfRules.Owner(state().Turf,a);map[a.Id]=MapBlips.Create(Pos(a.Center),"RPCore: "+a.Name+" / "+owner,BlipSprite.GangHighlight,ColorFor(owner),.65f);}signature=key;
        }
        public void Tick()
        {
            DrainAssault();SyncMap();if(!Active)return;
            if(convoyPreparing || convoyMoving){TickAssault();return;}
            if(profile!=Profile.Id || !state().TerritoriesEnabled || Pending!=null && !GangCrewRules.SameGang(state().Gang,Pending.Gang)){Suspend();return;}
            if(Game.IsPaused){last=Game.GameTime;assaultLast=Game.GameTime;assaultClockEligible=false;return;}
            if(preparing){if(!CanStart(draft) || player!=Game.Player.Character || unchecked(Game.GameTime-started)>10000){Suspend();return;}if(models.All(m=>m.IsLoaded))Spawn();return;}
            if(Pending==null){Suspend();return;}
            if(!GangCrewRules.SameGang(GangTurfRules.Owner(state().Turf,Pending.Area),Pending.PreviousOwner)){Suspend();return;}
            if(Pending.Ready){if(!Safe || !InArea(Pending) || !Game.Player.Character.IsOnFoot){Suspend();return;}if(!pool.AreAnyVisible && !InterfaceOptions.ModalOpen)GTA.UI.Screen.ShowHelpTextThisFrame(Pending.Mode==GangTurfMode.Defense?"E: confirmar defesa vitoriosa":"E: confirmar captura do territorio");return;}
            if(Game.IsMissionActive || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) || player!=null && player!=Game.Player.Character){Suspend();return;}
            if(!Safe || !InArea(Pending) || !Game.Player.Character.IsOnFoot || player!=null && player!=Game.Player.Character){Fail("morte/missao, veiculo, troca de ped ou abandono da area");return;}
            TickAssaultBattle();if(!active)return;
            if(Pending.Assault!=null)seconds=assaultProgress.BattleElapsed/1000f;else seconds+=Math.Min(1,Math.Max(0,unchecked(Game.GameTime-last)/1000f));last=Game.GameTime;if(seconds>=Pending.SecondsLimit){Fail("limite de tempo");return;}
            for(int i=0;i<defenders.Count;i++){if(Pending.Defeated.Contains(i))continue;var p=defenders[i];if(p==null || !p.Exists()){Suspend();Notify.Show("NPC indisponivel; disputa suspensa sem mudar a posse.");return;}if(p.IsDead){if(!GangTurfTransactions.Defeat(state(),Pending.Id,i,save,out _))throw new InvalidOperationException("Territory defeat not saved.");if(enemyBlips[i]!=null && enemyBlips[i].Exists())enemyBlips[i].Delete();}}
            if(Pending.Mode==GangTurfMode.Attack && GangWarRules.Reinforce(Pending.InitialDefenders,Pending.Defeated.Count,defenders.Count,Pending.Models.Length)){
                int first=defenders.Count;SpawnBatch(first,Pending.Models.Length-first);FightBatch(first);ReleaseModels();Notify.Show("Reforcos rivais chegaram: elimine o segundo grupo antes de capturar.");
            }
            if(Pending.Mode==GangTurfMode.Defense && GangDefenseRules.NextWave(defenders.Count,Pending.Defeated.Count,Pending.Models.Length,Pending.WaveSize)){
                int first=defenders.Count;SpawnBatch(first,Math.Min(Pending.WaveSize,Pending.Models.Length-first));FightBatch(first);if(defenders.Count==Pending.Models.Length)ReleaseModels();Notify.Show("Nova onda de "+Pending.Attacker+" aproximando-se.");
            }
            if(Pending.Ready){ClearDefenders();Notify.Show("Adversarios vencidos. Permaneça na area e pressione E para gravar o resultado.");return;}
            GTA.UI.Screen.ShowSubtitle(Pending.Area.Name+(Pending.Mode==GangTurfMode.Defense?" / defesa / atacantes ":" / defensores ")+Pending.Defeated.Count+"/"+Pending.Models.Length+" / area "+Pending.Radius+"m / END suspende",100);
            World.DrawMarker(MarkerType.Cylinder,Pos(Pending.Area.Center)-new Vector3(0,0,1),Vector3.Zero,Vector3.Zero,new Vector3(3,3,1),Color.FromArgb(90,180,60,60));
        }
        public void Reload(){Suspend();ClearMap();menu.Visible=false;}
        public void Dispose(){Reload();DisposeAssault();}
    }
}
