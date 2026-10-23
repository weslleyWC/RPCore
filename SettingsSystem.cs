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
    public class MapPin : ExtensibleData { public string Name; public float X, Y, Z; }
    public sealed class SettingsSystem : ISystem
    {
        public string Name { get { return "settings"; } }
        readonly ObjectPool pool;
        readonly Action<Keys> dispatch;
        readonly NeedsSystem needs;
        readonly GangSystem gangs;
        readonly Action jobs;
        readonly Vector3 postOp, valet, supply, tanker, prison, pilot, hunting, huntingForest, fishing;
        readonly WaypointTeleport teleport = new WaypointTeleport();
        readonly List<NativeMenu> menus = new List<NativeMenu>();
        readonly NativeMenu root, gps, emotes, interactions;
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
        void Shortcut(NativeMenu menu, string label, Func<Keys> key, Func<bool> enabled = null)
        {
            Add(menu, label, "Abre o sistema RPCore existente.", () => {
                if (enabled != null && !enabled()) { Notify.Show("Sistema desativado na configuracao."); return; }
                pool.HideAll(); dispatch(key());
            });
        }
        void Toggle(NativeMenu menu, string title, Func<bool> get, Action<bool> set)
        {
            var row = new NativeItem(title) { AltTitle = get() ? "ON" : "OFF" };
            row.Activated += (s, e) => { set(!get()); row.AltTitle = get() ? "ON" : "OFF"; }; menu.Add(row);
        }
        void Rate(NativeMenu menu, string title, Func<float> get, Action<float> set, float step)
        {
            var row = new NativeItem(title, "Enter alterna entre 0, 1, 2, 3, 4 e 5 vezes o passo indicado.") { AltTitle = get().ToString("0.##") };
            row.Activated += (s, e) => { set(get() >= step * 5 ? 0 : get() + step); row.AltTitle = get().ToString("0.##"); }; menu.Add(row);
        }
        void VisualToggle(NativeMenu menu, string title, string key, Func<bool> get, Action<bool> set)
        { Toggle(menu, title, get, v => { InterfaceOptions.Set("Visuals.ini", "Visuals", key, v); set(v); }); }

        public SettingsSystem(ObjectPool pool, Action<Keys> dispatch, NeedsSystem needs, GangSystem gangs, Action jobs = null, Vector3 postOp = default, Vector3 valet = default, Vector3 supply = default, Vector3 tanker = default, Vector3 prison = default, Vector3 pilot = default, Vector3 hunting = default, Vector3 huntingForest = default, Vector3 fishing = default)
        {
            this.pool = pool; this.dispatch = dispatch; this.needs = needs; this.gangs = gangs; this.jobs = jobs; this.postOp = postOp; this.valet = valet; this.supply = supply; this.tanker = tanker; this.prison = prison; this.pilot = pilot; this.hunting = hunting; this.huntingForest = huntingForest; this.fishing = fishing;
            root = Menu("Settings Menu");
            Add(root, "Teleportar para o waypoint", "Marque o destino no mapa. A pe ou como motorista em terra, parado. Selecione novamente para cancelar.", () => { pool.HideAll(); teleport.Begin(); });
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
            Rate(survival, "Fome por minuto (passo 0.5)", () => Cfg.HungerPerMin, v => { Cfg.HungerPerMin = v; InterfaceOptions.Set("Settings.ini", "Needs", "HungerPerMin", v); }, .5f);
            Rate(survival, "Sede por minuto (passo 0.5)", () => Cfg.ThirstPerMin, v => { Cfg.ThirstPerMin = v; InterfaceOptions.Set("Settings.ini", "Needs", "ThirstPerMin", v); }, .5f);
            Add(survival, "Descansar / parar descanso", "A pe, parado e fora de combate. Recupera sono gradualmente sem adiantar o relogio.", () => { pool.HideAll(); needs.ToggleRest(); });
            var vehicle = Menu("Vehicle Settings"); Link(root, vehicle);
            Toggle(vehicle,"Velocimetro em km/h",()=>InterfaceOptions.MetricSpeed,v=> { InterfaceOptions.MetricSpeed=v; InterfaceOptions.Set("Interface.ini","VehicleHud","Metric",v); });
            Shortcut(vehicle, "Controles do veiculo", () => Cfg.VehicleMenuKey, () => Cfg.EnableVehicleControls);
            Shortcut(vehicle, "Garagens", () => Cfg.GarageKey, () => Cfg.EnableGarages);
            Shortcut(vehicle, "Concessionaria", () => Cfg.DealerKey, () => Cfg.EnableGarages);
            Rate(vehicle, "Combustivel L/100km (passo 4)", () => Cfg.FuelPer100Km, v => { Cfg.FuelPer100Km = v; InterfaceOptions.Set("Vehicles.ini", "Fuel", "LitersPer100Km", v); }, 4);
            var ui = Menu("UI Settings"); Link(root, ui);
            Toggle(ui,"Inventario em grade",()=>InterfaceOptions.GridInventory,v=> { InterfaceOptions.GridInventory=v; InterfaceOptions.Set("Interface.ini","Interface","GridInventory",v); });
            Toggle(ui,"Zona segura de tela",()=>InterfaceOptions.RespectSafeZone,v=> { InterfaceOptions.RespectSafeZone=v; InterfaceOptions.Set("Interface.ini","Layout","RespectSafeZone",v); foreach(var menu in menus) menu.SafeZoneAware=v; });
            VisualToggle(ui, "HUD de necessidades", "NeedsHud", () => Visuals.NeedsHud, v => Visuals.NeedsHud = v);
            VisualToggle(ui, "HUD do veiculo", "VehicleHud", () => Visuals.VehicleHud, v => Visuals.VehicleHud = v);
            VisualToggle(ui, "Dinheiro / banco", "WalletHud", () => Visuals.WalletHud, v => Visuals.WalletHud = v);
            Toggle(ui, "Ocultar minimapa a pe", () => InterfaceOptions.HideRadarOnFoot, v => { InterfaceOptions.HideRadarOnFoot = v; InterfaceOptions.Set("Interface.ini", "Interface", "HideRadarOnFoot", v); });
            Add(ui, "Escala do HUD", "Alterna 0.8 / 1.0 / 1.2.", () => { Visuals.Scale = Visuals.Scale < .9f ? 1 : Visuals.Scale < 1.1f ? 1.2f : .8f; InterfaceOptions.Set("Visuals.ini", "Layout", "Scale", Visuals.Scale); Notify.Show("Escala: " + Visuals.Scale); });
            var game = Menu("Game Settings"); Link(root, game);
            if (jobs != null) Add(game, "Trabalhos / contratos", "Contratos de entregas e motoristas: contratar, GPS e cancelar.", () => { pool.HideAll(); jobs(); });
            Shortcut(game, "Salvar / configurar / diagnosticar", () => Cfg.CoreKey);
            Shortcut(game, "Radio", () => Cfg.RadioKey, () => Cfg.EnableRadio);
            Shortcut(game, "Conquistas", () => Cfg.AchKey);
            var cheats = Menu("Cheat Menu"); Link(root, cheats);
            Add(cheats, "Recuperar saude", "Altera a saude somente quando solicitado, fora de missao.", () => Cheat(() => Game.Player.Character.Health = Game.Player.Character.MaxHealth));
            Add(cheats, "Colete 100", "Entrega colete quando solicitado, fora de missao.", () => Cheat(() => Game.Player.Character.Armor = 100));
            Add(cheats, "Adicionar $1000", "Altera a carteira RPCore/nativa do perfil ativo.", () => Cheat(() => { if (Wallet.Cash <= int.MaxValue - 1000) { Wallet.Credit(1000); Wallet.Save(); } }));
            var builder = Menu("Map Builder"); Link(root, builder);
            Add(builder, "Salvar marcador aqui", "Editor inicial de pontos nomeados e GPS; ainda nao coloca objetos ou edificios.", SavePin);
            gps = Menu("Fast GPS");
            Add(builder, "Meus pontos / GPS / excluir", "Somente pontos criados pelo RPCore.", () => { builder.Visible = false; OpenGps(); });
            emotes = Menu("Emotes");
            Add(emotes, "Acenar", "Gesto curto com a parte superior do corpo.", () => Play("gestures@m@standing@casual", "gesture_hello"));
            Add(emotes, "Aplaudir", "Animacao curta; nao interrompe tarefas de outros mods ao fechar.", () => Play("anim@mp_player_intcelebrationmale@slow_clap", "slow_clap"));
            Add(emotes, "Parar gesto RPCore", "Para apenas a animacao iniciada por este menu.", StopAnimation);
            interactions = Menu("Interactions");
            Shortcut(interactions, "Armazenamento / porta-malas", () => Cfg.StorageKey);
            Shortcut(interactions, "Inventario", () => Cfg.InventoryKey);
            Shortcut(interactions, "Turno de pizza: iniciar / cancelar", () => Cfg.JobKey);
            Add(interactions, "Descansar / parar", "Recupera sono enquanto permanece parado a pe.", () => { pool.HideAll(); needs.ToggleRest(); });
            Reload();
        }
        void Cheat(Action action) { if (Game.IsMissionActive) Notify.Show("Indisponivel durante missao."); else action(); }
        void SavePin()
        {
            if (map.Count >= 100) { Notify.Show("Limite de 100 pontos por perfil."); return; }
            string name = Game.GetUserInput(WindowTitle.EnterCustomTeamName, "Ponto " + (map.Count + 1), 32);
            if (string.IsNullOrWhiteSpace(name)) return;
            var p = Game.Player.Character.Position; map.Add(new MapPin { Name = name.Trim(), X = p.X, Y = p.Y, Z = p.Z }); Save(); RefreshPins(); Notify.Show("Ponto salvo.");
        }
        void RefreshPins()
        {
            foreach (var b in pins) if (b.Exists()) b.Delete(); pins.Clear();
            foreach (var point in map) { var b = World.CreateBlip(new Vector3(point.X, point.Y, point.Z)); b.Name = point.Name; b.IsShortRange = true; b.Color = BlipColor.Blue; pins.Add(b); }
        }
        public void OpenGps()
        {
            gps.Clear();
            Shortcut(gps, "Garagens / GPS", () => Cfg.GarageKey, () => Cfg.EnableGarages);
            Shortcut(gps, "Concessionaria / GPS", () => Cfg.DealerKey, () => Cfg.EnableGarages);
            Shortcut(gps, "Gangues / GPS", () => Cfg.GangsKey, () => Cfg.EnableGangs);
            Add(gps, "GPS: trabalho de pizza", "Pizzaria. Inicie/encerre o turno com " + Cfg.JobKey + ".", () => { Function.Call(Hash.SET_NEW_WAYPOINT, PizzaJob.StartLocation.X, PizzaJob.StartLocation.Y); gps.Visible = false; });
            if (jobs != null)
            {
                Add(gps, "Trabalhos / contratos", "Gerenciar os contratos de entregas e motoristas.", () => { gps.Visible = false; jobs(); });
                Add(gps, "GPS: pesca / equipamento", "Banca de pesca: vara, iscas e venda.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, fishing.X, fishing.Y); gps.Visible = false; });
                Add(gps, "GPS: caca / acougue", "Licenca, equipamento e venda de materiais.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, hunting.X, hunting.Y); gps.Visible = false; });
                Add(gps, "GPS: floresta de caca", "Area de isca e rastreamento em Paleto Forest.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, huntingForest.X, huntingForest.Y); gps.Visible = false; });
                Add(gps, "GPS: piloto / base de aviacao", "Qualificacao e voos de aviao/helicoptero.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, pilot.X, pilot.Y); gps.Visible = false; });
                Add(gps, "GPS: transporte prisional", "Mission Row: retire o onibus e leve detentos a Bolingbroke.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, prison.X, prison.Y); gps.Visible = false; });
                Add(gps, "GPS: transporte de combustivel", "LS GAS: cavalo mecanico, tanque e posto.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, tanker.X, tanker.Y); gps.Visible = false; });
                Add(gps, "GPS: reposicao de lojas", "Caminhao, carga, lojas e retorno a base.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, supply.X, supply.Y); gps.Visible = false; });
                Add(gps, "GPS: trabalho Valet", "Contrate no hotel e cuide dos carros dos clientes.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, valet.X, valet.Y); gps.Visible = false; });
                Add(gps, "GPS: trabalho Post OP", "Contrate a pe, retire a van e entregue as caixas.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, postOp.X, postOp.Y); gps.Visible = false; });
            }
            foreach (var pin in map.ToList())
            {
                var point = pin;
                Add(gps, "GPS: " + point.Name, "Marca a rota ate este ponto.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, point.X, point.Y); gps.Visible = false; });
                Add(gps, "Excluir: " + point.Name, "Remove somente este ponto RPCore.", () => { map.Remove(point); Save(); RefreshPins(); gps.Visible = false; });
            }
            gps.Visible = true;
        }
        public void OpenEmotes() { emotes.Visible = true; }
        public void OpenInteractions() { interactions.Visible = true; }
        void Play(string dict, string name)
        {
            var p = Game.Player.Character;
            if (p.IsInVehicle() || p.IsInCombat || p.IsDead || Game.IsMissionActive) return;
            StopAnimation(); animated = p; animDict = dict; animName = name; animDeadline = Game.GameTime + 2500;
            Function.Call(Hash.REQUEST_ANIM_DICT, dict); pool.HideAll();
        }
        void StopAnimation()
        {
            if (animStarted && animated != null && animated.Exists()) Function.Call(Hash.STOP_ANIM_TASK, animated, animDict, animName, 2f);
            if (animDict != null) Function.Call(Hash.REMOVE_ANIM_DICT, animDict);
            animated = null; animDict = animName = null; animStarted = false;
        }
        public void Tick()
        {
            teleport.Tick();
            var p = Game.Player.Character;
            bool hideRadar = InterfaceOptions.HideRadarOnFoot && !p.IsInVehicle() && !Game.IsMissionActive && !Game.IsPaused;
            if (hideRadar && !Function.Call<bool>(Hash.IS_RADAR_HIDDEN)) { Function.Call(Hash.DISPLAY_RADAR, false); radarHiddenByUs = true; }
            else if (!hideRadar && radarHiddenByUs) { Function.Call(Hash.DISPLAY_RADAR, true); radarHiddenByUs = false; }
            if (animDict == null) return;
            if (p.IsDead || p.IsInVehicle() || p.IsInCombat || Game.IsMissionActive || animated.Handle != p.Handle) { StopAnimation(); return; }
            if (!animStarted)
            {
                if (Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, animDict)) { Function.Call(Hash.TASK_PLAY_ANIM, p, animDict, animName, 4f, -4f, 4000, 48, 0f, false, false, false); animStarted = true; animDeadline = Game.GameTime + 4500; }
                else if (unchecked(Game.GameTime - animDeadline) >= 0) StopAnimation();
            }
            else if (unchecked(Game.GameTime - animDeadline) >= 0) StopAnimation();
        }
        public void OnKey(KeyEventArgs e)
        {
            if (e.KeyCode != InterfaceOptions.SettingsKey) return;
            if (menus.Any(m => m.Visible)) { foreach (var m in menus) m.Visible = false; }
            else if (!pool.AreAnyVisible) root.Visible = true;
        }
        public void Reload() { teleport.Cancel(false); foreach (var m in menus) m.Visible = false; StopAnimation(); map = Store.Load("MapPins.json", () => new List<MapPin>()).Where(p => p != null && !string.IsNullOrWhiteSpace(p.Name) && !float.IsNaN(p.X) && !float.IsNaN(p.Y) && !float.IsNaN(p.Z) && !float.IsInfinity(p.X) && !float.IsInfinity(p.Y) && !float.IsInfinity(p.Z)).Take(100).ToList(); RefreshPins(); }
        public void Save() { Store.Write("MapPins.json", map); }
        public void Dispose() { teleport.Cancel(false); StopAnimation(); if (radarHiddenByUs) Function.Call(Hash.DISPLAY_RADAR, true); radarHiddenByUs = false; foreach (var m in menus) m.Visible = false; foreach (var b in pins) if (b.Exists()) b.Delete(); pins.Clear(); }
    }
}
