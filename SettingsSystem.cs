using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;

namespace RPCore
{
    public sealed partial class SettingsSystem : ISystem
    {
        public string Name { get { return "settings"; } }
        public Action GarageRequested,DealerRequested,VehicleRequested,PdmRequested,PdmGpsRequested;
        readonly ObjectPool pool;
        readonly Action<Keys> dispatch;
        readonly NeedsSystem needs;
        readonly GangSystem gangs;
        readonly Action jobs;
        readonly Vector3 postOp, valet, supply, tanker, prison, pilot, hunting, huntingForest, fishing, crafting, dealer, drag, pdm;
        readonly WaypointTeleport teleport = new WaypointTeleport();
        readonly List<NativeMenu> menus = new List<NativeMenu>();
        readonly NativeMenu root, gps, emotes, interactions,gpsNearest,gpsJobs,gpsPersonal,pinDetails,pinConfirm;
        readonly List<Action> refreshOptions=new List<Action>();
        readonly List<Blip> pins = new List<Blip>();
        List<MapPin> map;
        string animDict, animName;
        Ped animated;
        int animDeadline;
        bool animStarted;
        bool radarHiddenByUs;

        NativeMenu Menu(string title)
        {
            var menu = new NativeMenu("", title.ToUpperInvariant()) { Width = 420, Offset = new PointF(40,8), SafeZoneAware = InterfaceOptions.RespectSafeZone, NameFont = GTA.UI.Font.ChaletComprimeCologne };
            pool.Add(menu); menus.Add(menu); Visuals.Register(menu,title=="Fast GPS"||title=="Emotes"||title=="Interactions" ? "reference-actions" : "reference-settings"); return menu;
        }
        static void Add(NativeMenu menu, string title, string description, Action action)
        {
            var row = new NativeItem(title, description);
            row.Activated += (s, e) => { try { action(); } catch (Exception ex) { Log.Error("settings action: " + title, ex); Notify.Show("Nao foi possivel concluir. Veja RPCore.log."); } };
            menu.Add(row);
        }
        void Link(NativeMenu parent, NativeMenu child)
        {
            var row = parent.AddSubMenu(child, ">>>");
            row.Title = child.Name == "UI SETTINGS" ? "UI Settings" : System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(child.Name.ToLowerInvariant());
        }
        void Shortcut(NativeMenu menu, string label, Func<Keys> key, Func<bool> enabled = null,Func<Action> direct = null)
        {
            Add(menu, label, "Abre o sistema RPCore existente.", () => {
                if (enabled != null && !enabled()) { Notify.Show("Sistema desativado na configuracao."); return; }
                pool.HideAll();var action=direct?.Invoke();if(action!=null)action();else dispatch(key());
            });
        }
        void Toggle(NativeMenu menu, string title, Func<bool> get, Action<bool> set)
        {
            var row=new NativeCheckboxItem(title,get());bool updating=false;
            Action refresh=()=>{updating=true;try{row.Checked=get();}finally{updating=false;}};
            row.CheckboxChanged+=(s,e)=>{if(updating)return;try{set(row.Checked);}catch(Exception ex){Log.Error("setting: "+title,ex);Notify.Show("Preferencia nao gravada; valor anterior conservado.");}finally{refresh();}};
            refreshOptions.Add(refresh);menu.Add(row);
        }
        void Rate(NativeMenu menu,string title,Func<float> get,Action<float> set,float step)
        {
            var values=MenuFileRules.Choices(get(),step);
            var row=new NativeListItem<float>(title,"Esquerda/direita ajustam; valores personalizados existentes sao preservados.",values);if(values.Contains(get()))row.SelectedItem=get();bool updating=false;
            Action refresh=()=>{updating=true;try{float current=get();if(!float.IsNaN(current) && !float.IsInfinity(current) && current>=0){if(!row.Items.Contains(current))row.Add(current);row.SelectedItem=current;}}finally{updating=false;}};
            row.ItemChanged+=(s,e)=>{if(updating)return;try{set(row.SelectedItem);}catch(Exception ex){Log.Error("setting rate: "+title,ex);Notify.Show("Ajuste nao gravado; valor anterior conservado.");}finally{refresh();}};refreshOptions.Add(refresh);menu.Add(row);
        }
        void VisualToggle(NativeMenu menu, string title, string key, Func<bool> get, Action<bool> set)
        { Toggle(menu, title, get, v => { InterfaceOptions.Set("Visuals.ini", "Visuals", key, v); set(v); }); }

        public SettingsSystem(ObjectPool pool, Action<Keys> dispatch, NeedsSystem needs, GangSystem gangs, Action jobs = null, Vector3 postOp = default, Vector3 valet = default, Vector3 supply = default, Vector3 tanker = default, Vector3 prison = default, Vector3 pilot = default, Vector3 hunting = default, Vector3 huntingForest = default, Vector3 fishing = default, Vector3 crafting = default, Vector3 dealer = default, Vector3 drag = default, Vector3 pdm = default, Action traffic = null, Action studio = null, Action halloween = null, Action editor = null, Action passenger = null, Action world = null)
        {
            this.pool = pool; this.dispatch = dispatch; this.needs = needs; this.gangs = gangs; this.jobs = jobs; this.postOp = postOp; this.valet = valet; this.supply = supply; this.tanker = tanker; this.prison = prison; this.pilot = pilot; this.hunting = hunting; this.huntingForest = huntingForest; this.fishing = fishing; this.crafting = crafting; this.dealer = dealer; this.drag = drag; this.pdm = pdm;
            root = Menu("Settings Menu");
            var gang = Menu("Gang Creation"); Link(root, gang);
            Add(gang, "Criar gangue neste local", "Define nome e HQ. Cadastro proprio; nao cria guerras ou NPCs.", () => {
                if (this.gangs == null || !Cfg.EnableGangs) { Notify.Show("Ative Gangs em Settings.ini e reinicie o script."); return; }
                string name = Game.GetUserInput(WindowTitle.EnterCustomTeamName, "Minha Gangue", 32);
                if (!string.IsNullOrWhiteSpace(name)) this.gangs.Create(name.Trim());
            });
            Shortcut(gang, "Gangues / afiliacao / GPS", () => Cfg.GangsKey, () => Cfg.EnableGangs);
            var ped = Menu("Ped Settings"); Link(root, ped);
            Add(ped, "Status do personagem", "Mostra o perfil e os valores atuais.", () => Notify.Show(Profile.Id + " | HP " + Game.Player.Character.Health + " | Armor " + Game.Player.Character.Armor));
            Shortcut(ped, "Perfis / importar saves / banco", () => Cfg.CoreKey);
            Add(ped, "Salvar posicao do personagem", "Grava somente um marcador RPCore; nao altera o save nativo.", SavePin);
            var survival = Menu("Survival Settings"); Link(root, survival);
            var technical=Menu("Advanced Settings");
            Add(survival,"Estado das necessidades","Os mesmos valores do HUD e do inventario.",()=>Notify.Show("Fome "+needs.Hunger.ToString("0")+" / sede "+needs.Thirst.ToString("0")+" / energia "+needs.Sleep.ToString("0")));
            Rate(technical, "Fome por minuto (passo 0.5)", () => Cfg.HungerPerMin, v => { InterfaceOptions.Set("Settings.ini", "Needs", "HungerPerMin", v); Cfg.HungerPerMin=v; }, .5f);
            Rate(technical, "Sede por minuto (passo 0.5)", () => Cfg.ThirstPerMin, v => { InterfaceOptions.Set("Settings.ini", "Needs", "ThirstPerMin", v); Cfg.ThirstPerMin=v; }, .5f);
            Add(survival, "Descansar / parar descanso", "A pe, parado e fora de combate. Recupera sono gradualmente sem adiantar o relogio.", () => { pool.HideAll(); needs.ToggleRest(); });
            var vehicle = Menu("Vehicle Settings"); Link(root, vehicle);
            Toggle(vehicle,"Velocimetro em km/h",()=>InterfaceOptions.MetricSpeed,v=> { InterfaceOptions.Set("Interface.ini","VehicleHud","Metric",v); InterfaceOptions.MetricSpeed=v; });
            Shortcut(vehicle, "Controles do veiculo", () => Cfg.VehicleMenuKey, () => Cfg.EnableVehicleControls,()=>VehicleRequested);
            Shortcut(vehicle, "Garagens", () => Cfg.GarageKey, () => Cfg.EnableGarages,()=>GarageRequested);
            Shortcut(vehicle, "Concessionaria", () => Cfg.DealerKey, () => Cfg.EnableGarages,()=>DealerRequested);
            Rate(technical, "Combustivel L/100km (passo 4)", () => Cfg.FuelPer100Km, v => { InterfaceOptions.Set("Vehicles.ini", "Fuel", "LitersPer100Km", v); Cfg.FuelPer100Km=v; }, 4);
            var ui = Menu("UI Settings"); Link(root, ui);
            Link(ui,technical);
            Toggle(technical,"Mostrar opcoes tecnicas nos sistemas",()=>InterfaceOptions.AdvancedMenus,v=>{InterfaceOptions.Set("Interface.ini","Interface","AdvancedMenus",v); InterfaceOptions.AdvancedMenus=v;});
            Toggle(ui,"Inventario em grade",()=>InterfaceOptions.GridInventory,v=> { InterfaceOptions.Set("Interface.ini","Interface","GridInventory",v); InterfaceOptions.GridInventory=v; });
            Toggle(ui,"Zona segura de tela",()=>InterfaceOptions.RespectSafeZone,v=> { InterfaceOptions.Set("Interface.ini","Layout","RespectSafeZone",v); InterfaceOptions.RespectSafeZone=v; foreach(var menu in menus) menu.SafeZoneAware=v; });
            VisualToggle(ui, "HUD de necessidades", "NeedsHud", () => Visuals.NeedsHud, v => Visuals.NeedsHud = v);
            VisualToggle(ui, "HUD do veiculo", "VehicleHud", () => Visuals.VehicleHud, v => Visuals.VehicleHud = v);
            VisualToggle(ui, "Dinheiro / banco", "WalletHud", () => Visuals.WalletHud, v => Visuals.WalletHud = v);
            Toggle(ui, "Ocultar minimapa a pe", () => InterfaceOptions.HideRadarOnFoot, v => { InterfaceOptions.Set("Interface.ini", "Interface", "HideRadarOnFoot", v); InterfaceOptions.HideRadarOnFoot=v; });
            Add(ui, "Escala do HUD", "Alterna 0.8 / 1.0 / 1.2.", () => { float scale=Visuals.Scale < .9f ? 1 : Visuals.Scale < 1.1f ? 1.2f : .8f; InterfaceOptions.Set("Visuals.ini", "Layout", "Scale",scale);Visuals.Scale=scale; Notify.Show("Escala: " + Visuals.Scale); });
            var game = Menu("Game Settings"); Link(root, game);
            Toggle(game,"Ocultar roda de armas do GTA",()=>InterfaceOptions.HideWeaponWheel,v=>{InterfaceOptions.Set("Interface.ini","Interface","HideWeaponWheel",v);InterfaceOptions.HideWeaponWheel=v;});
            if(studio!=null) Add(game,"Contratos / assalto ao estudio","The Hollywood Act: cinco preparacoes, final e pagamento persistente.",()=>{pool.HideAll();studio();});
            if(halloween!=null) Add(game,"Halloween / The Last Ride","GPS/inicio, sustos opcionais e corrida do Romero com pulso defensivo.",()=>{pool.HideAll();halloween();});
            if(traffic!=null) Add(game,"Policia / transito e multas","Fiscalizacao opcional, patrulha observadora e autuacoes com pagamento bancario.",()=>{pool.HideAll();traffic();});
            if(world!=null)Add(game,"Mundo / clima e horario","Modos manual/dinamico/real opcionais e relogio.",()=>{pool.HideAll();world();});
            if(passenger!=null)Add(game,"Transporte / taxi e limousine","Contratar motorista como passageiro; reserva bancaria e taximetro.",()=>{pool.HideAll();passenger();});
            Add(game,"Empresa PDM","Painel empresarial: consultas, estoque, contratos e GPS. Operacoes presenciais exigem estar no escritorio.",()=>{pool.HideAll();PdmRequested?.Invoke();});
            if (jobs != null) Add(game, "Trabalhos / contratos", "Contratos de entregas e motoristas: contratar, GPS e cancelar.", () => { pool.HideAll(); jobs(); });
            Shortcut(game, "Salvar / configurar / diagnosticar", () => Cfg.CoreKey);
            Shortcut(game, "Radio", () => Cfg.RadioKey, () => Cfg.EnableRadio);
            Shortcut(game, "Conquistas", () => Cfg.AchKey);
            var cheats = Menu("Cheat Menu"); Link(root, cheats);
            Add(cheats, "Teleportar para o waypoint", "Marque o destino no mapa. A pe ou como motorista em terra, parado. Selecione novamente para cancelar.", () => { pool.HideAll(); teleport.Begin(); });
            Add(cheats, "Recuperar saude", "Altera a saude somente quando solicitado, fora de missao.", () => Cheat(() => Game.Player.Character.Health = Game.Player.Character.MaxHealth));
            Add(cheats, "Colete 100", "Entrega colete quando solicitado, fora de missao.", () => Cheat(() => Game.Player.Character.Armor = 100));
            Add(cheats, "Adicionar $1000", "Altera a carteira RPCore/nativa do perfil ativo.", () => Cheat(() => { if (Wallet.Cash <= int.MaxValue - 1000) { Wallet.Credit(1000); Wallet.Save(); } }));
            var builder = Menu("Map Builder"); Link(root, builder);
            if(editor!=null) Add(builder,"Map Builder / objetos e locais","Criar/editar/salvar/carregar/importar/exportar mapas por perfil.",()=>{pool.HideAll();editor();});
            Add(builder, "Salvar marcador aqui", "Marcador pessoal simples. Objetos e locais estao no editor acima.", SavePin);
            gps = Menu("Fast GPS");gpsNearest=Menu("Nearby Services");gpsJobs=Menu("Jobs GPS");gpsPersonal=Menu("My Places");pinDetails=Menu("Saved Place");pinConfirm=Menu("Confirm Delete");
            Link(gps,gpsNearest);Link(gps,gpsJobs);Link(gps,gpsPersonal);
            Add(builder, "Meus pontos / GPS / excluir", "Somente pontos criados pelo RPCore.", () => { builder.Visible = false; OpenGps(); });
            emotes = Menu("Emotes");
            foreach(var gesture in ReferenceEmotes.All){var emote=gesture;Add(emotes,emote.Name,"Gesto curto de4s; encerra ao andar, combater ou entrar em veiculo. Pode parar pelo menu.",()=>Play(emote.Dictionary,emote.Clip));}
            Add(emotes, "Parar gesto RPCore", "Para apenas a animacao iniciada por este menu.", StopAnimation);
            interactions = Menu("Interactions");
            Shortcut(interactions, "Armazenamento / porta-malas", () => Cfg.StorageKey);
            Shortcut(interactions, "Inventario", () => Cfg.InventoryKey);
            if(jobs!=null)Add(interactions,"Trabalhos / contratos","Consultar trabalhos e destinos.",()=>{pool.HideAll();jobs();});
            Add(interactions, "Descansar / parar", "Recupera sono enquanto permanece parado a pe.", () => { pool.HideAll(); needs.ToggleRest(); });
            Reload();
        }
        void Cheat(Action action) { if (Game.IsMissionActive) Notify.Show("Indisponivel durante missao."); else action(); }
        public void OpenGps()
        {
            BuildNearbyGps();BuildPersonalGps();gpsJobs.Clear();
            Add(gpsJobs, "GPS: trabalho de pizza", "Pizzaria. Inicie/encerre o turno com " + Cfg.JobKey + ".", () => { Function.Call(Hash.SET_NEW_WAYPOINT, PizzaJob.StartLocation.X, PizzaJob.StartLocation.Y); pool.HideAll(); });
            if (jobs != null)
            {
                Add(gpsJobs, "Trabalhos / contratos", "Gerenciar os contratos de entregas e motoristas.", () => { pool.HideAll(); jobs(); });
                Add(gpsJobs, "PDM / locais e GPS", "Destinos da empresa e explicacao de compra/coleta. Nao confunde com carros pessoais.", () => {pool.HideAll();if(PdmGpsRequested!=null)PdmGpsRequested();else Function.Call(Hash.SET_NEW_WAYPOINT,pdm.X,pdm.Y);});
                Add(gpsJobs, "GPS: arrancada / Sandy Shores", "Encontro e inscricao com carro proprio.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, drag.X, drag.Y); pool.HideAll(); });
                Add(gpsJobs, "GPS: contato / trafico", "Contato externo para comprar estoque da atividade.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, dealer.X, dealer.Y); pool.HideAll(); });
                Add(gpsJobs, "GPS: bancada / crafting / feira", "Receitas e venda de produtos das bolsas de caca/pesca.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, crafting.X, crafting.Y); pool.HideAll(); });
                Add(gpsJobs, "GPS: pesca / equipamento", "Banca de pesca: vara, iscas e venda.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, fishing.X, fishing.Y); pool.HideAll(); });
                Add(gpsJobs, "GPS: caca / acougue", "Licenca, equipamento e venda de materiais.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, hunting.X, hunting.Y); pool.HideAll(); });
                Add(gpsJobs, "GPS: floresta de caca", "Area de isca e rastreamento em Paleto Forest.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, huntingForest.X, huntingForest.Y); pool.HideAll(); });
                Add(gpsJobs, "GPS: piloto / base de aviacao", "Qualificacao e voos de aviao/helicoptero.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, pilot.X, pilot.Y); pool.HideAll(); });
                Add(gpsJobs, "GPS: transporte prisional", "Mission Row: retire o onibus e leve detentos a Bolingbroke.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, prison.X, prison.Y); pool.HideAll(); });
                Add(gpsJobs, "GPS: transporte de combustivel", "LS GAS: cavalo mecanico, tanque e posto.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, tanker.X, tanker.Y); pool.HideAll(); });
                Add(gpsJobs, "GPS: reposicao de lojas", "Caminhao, carga, lojas e retorno a base.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, supply.X, supply.Y); pool.HideAll(); });
                Add(gpsJobs, "GPS: trabalho Valet", "Contrate no hotel e cuide dos carros dos clientes.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, valet.X, valet.Y); pool.HideAll(); });
                Add(gpsJobs, "GPS: trabalho Post OP", "Contrate a pe, retire a van e entregue as caixas.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, postOp.X, postOp.Y); pool.HideAll(); });
            }
            gps.Visible=true;
        }
        public void OpenEmotes() {if(CanOpenActions())emotes.Visible=true;}
        public void OpenInteractions() {if(CanOpenActions())interactions.Visible=true;}
        void Play(string dict, string name)
        {
            var p = Game.Player.Character;
            if(!CanOpenActions() || p.IsInVehicle() || p.IsInCombat || p.IsRagdoll || p.IsFalling || p.IsSwimming || p.IsClimbing){Notify.Show("Use gestos a pe e em local seguro.");return;}
            if(actionGate?.Owner!=null && actionGate.Owner!="emote"){Notify.Show("Encerre a atividade antes de usar gestos.");return;}
            StopAnimation();if(actionGate!=null && !actionGate.Acquire("emote"))return; animated = p; animDict = dict; animName = name; animDeadline = Game.GameTime + 2500;
            Function.Call(Hash.REQUEST_ANIM_DICT, dict); pool.HideAll();
        }
        void StopAnimation()
        {
            try{if(animStarted && animated!=null && animated.Exists())Function.Call(Hash.STOP_ANIM_TASK,animated,animDict,animName,2f);}
            finally
            {
                try{if(animDict!=null)Function.Call(Hash.REMOVE_ANIM_DICT,animDict);}
                finally{animated=null;animDict=animName=null;animStarted=false;actionGate?.Release("emote");}
            }
        }
        public void Tick()
        {
            teleport.Tick();
            if(menus.Any(m=>m.Visible)){if(!CanOpenActions()){foreach(var m in menus)m.Visible=false;}else foreach(var refresh in refreshOptions)refresh();}
            var p = Game.Player.Character;
            bool hideRadar = InterfaceOptions.HideRadarOnFoot && !p.IsInVehicle() && !p.IsDead && !Game.IsMissionActive && !Game.IsPaused && !GameplayContext.SceneBlocking;
            if (hideRadar && !Function.Call<bool>(Hash.IS_RADAR_HIDDEN)) { Function.Call(Hash.DISPLAY_RADAR, false); radarHiddenByUs = true; }
            else if (!hideRadar && radarHiddenByUs) { Function.Call(Hash.DISPLAY_RADAR, true); radarHiddenByUs = false; }
            if (animDict == null) return;
            if (!CanOpenActions() || p.IsInVehicle() || p.IsInCombat || p.IsRagdoll || p.IsFalling || p.IsSwimming || p.IsClimbing || p.IsShooting || p.Speed>.6f || animated.Handle != p.Handle) { StopAnimation(); return; }
            if (!animStarted)
            {
                if (Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, animDict)) { Function.Call(Hash.TASK_PLAY_ANIM, p, animDict, animName, 4f, -4f, 4000, animDict=="random@arrests"?1:49, 0f, false, false, false); animStarted = true; animDeadline = Game.GameTime + 4500; }
                else if (unchecked(Game.GameTime - animDeadline) >= 0) StopAnimation();
            }
            else if (unchecked(Game.GameTime - animDeadline) >= 0) StopAnimation();
        }
        public void OnKey(KeyEventArgs e)
        {
            if (e.KeyCode != InterfaceOptions.SettingsKey) return;
            if (menus.Any(m => m.Visible)) { foreach (var m in menus) m.Visible = false; }
            else if (!pool.AreAnyVisible && CanOpenActions()) root.Visible = true;
        }
        public void Reload() { teleport.Cancel(false); foreach (var m in menus) m.Visible = false; StopAnimation(); map = Store.Load("MapPins.json", () => new List<MapPin>());if(!MapPinRules.Valid(map))Log.Write("Invalid MapPins data retained; marker edits blocked."); RefreshPins(); }
        public void Save() {if(!MapPinRules.Valid(map))throw new InvalidOperationException("Invalid MapPins retained.");Store.Write("MapPins.json",map);}
        public void Dispose() { teleport.Cancel(false); StopAnimation(); if (radarHiddenByUs) Function.Call(Hash.DISPLAY_RADAR, true); radarHiddenByUs = false; foreach (var m in menus) m.Visible = false; foreach (var b in pins) if (b.Exists()) b.Delete(); pins.Clear(); }
    }
}
