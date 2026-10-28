using System;
using System.Drawing;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
namespace RPCore
{
    public sealed class HousingSystem : ISystem
    {
        public string Name => "housing";
        public Action OpenWardrobe, OpenStorage;
        public bool CanUseInterior => Inside && SafeFoot && transit == null && Wallet.Housing.Valid && Wallet.Housing.Owns(options.Id);
        public bool CanUseWardrobe => CanUseInterior;
        readonly ObjectPool pool; readonly ShiftGate gate; readonly NativeMenu menu; readonly HousingOptions options; readonly Blip blip;
        readonly PersonalServiceMenu ui;HousingTransit transit; Ped owner; Vector3 origin; bool exiting; string profile; int nextProbe;
        static Vector3 Pos(JobPoint p) => new Vector3(p.X, p.Y, p.Z);
        bool Inside => Game.Player.Character.Position.DistanceTo(Pos(options.Interior)) < 45 && Math.Abs(Game.Player.Character.Position.Z - options.Interior.Z) < 12;
        bool SafeFoot => PersonalServiceMenu.SafeFoot;
        bool ExitFoot => !Game.IsPaused && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && Game.Player.CanControlCharacter && Profile.Detect()==Profile.Id && !InterfaceOptions.ModalOpen && !Game.Player.Character.IsDead && !Game.Player.Character.IsInVehicle() && !Game.Player.Character.IsRagdoll && !Game.Player.Character.IsFalling;
        public HousingSystem(ObjectPool pool, ShiftGate gate)
        {
            this.pool = pool; this.gate = gate; var defaults = HousingDefaults.Load(); options = Store.LoadDef("Jobs/Housing.json", () => defaults);
            if (options == null || !options.Valid) { Log.Write("Invalid Housing.json; embedded defaults used, original retained."); options = defaults; }
            menu = new NativeMenu("Moradia", options.Name); pool.Add(menu); Visuals.Register(menu, "core");ui=new PersonalServiceMenu(menu,ex=>{Release();ui.Close();Log.Error("housing action",ex);Notify.Show("Acao de moradia nao confirmada; dados preservados.");});
            blip = MapBlips.Create(Pos(options.Entrance), "RPCore: " + options.Name, BlipSprite.Safehouse, BlipColor.Orange, .8f); profile = Profile.Id;
        }
        void Add(string title, string description, Action action)
        {ui.Add(title,description,action);}
        public void OpenMenu()
        {
            pool.HideAll(); ui.Begin("root"); if (!Wallet.Housing.Valid) { Notify.Show("Dados de moradia invalidos; original preservado."); return; }
            Add("GPS: " + options.Name, "Entrada publica do predio. Compra e entrada apenas a pe junto ao local.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, options.Entrance.X, options.Entrance.Y); ui.Close(); });
            if (!Wallet.Housing.Owns(options.Id)) Add("Comprar apartamento: $" + options.Price, "Compra unica pelo banco RPCore, sem aluguel recorrente. Nao compra veiculo, garagem ou mobiliario.", ConfirmBuy);
            else { var home = Wallet.Housing.Properties.Find(p => p.Id == options.Id); Add("Apartamento adquirido / $" + home.Paid, "Propriedade do perfil ativo. Sem nova cobranca para entrar ou sair.", () => { }); Add("Entrar no apartamento", "Requer interior e piso carregados. Se indisponiveis, conserva posicao e propriedade.", () => Begin(false)); }
            if (OpenStorage != null && Wallet.Housing.Owns(options.Id)) Add("Armazenamento do apartamento", "Guardar e retirar itens do inventario, apenas dentro do seu apartamento.", OpenStorage);
            if (OpenWardrobe != null && Wallet.Housing.Owns(options.Id)) Add("Guardarroupa / roupas salvas", "Entre no apartamento e aproxime-se do guardarroupa no andar superior.", OpenWardrobe);
            if (Inside) Add("Sair para a entrada publica", "Saida disponivel mesmo se o perfil atual nao for proprietario.", () => Begin(true));
            if (transit != null) Add("Cancelar carregamento", "Nao muda dinheiro nem propriedade e conserva sua posicao.", Release);
            Add("Fechar", "Retornar ao jogo.", () => ui.Close()); menu.Visible = true;
        }
        bool AtEntrance => Game.Player.Character.Position.DistanceTo(Pos(options.Entrance)) <= 3;
        void ConfirmBuy()
        {
            if(!SafeFoot || !AtEntrance || transit!=null || gate.Owner!=null){Notify.Show("Va a pe a entrada, sem atividade.");return;}
            ui.Begin("purchase",OpenMenu,false);menu.Name="Comprar "+options.Name+" / $"+options.Price;
            Add("Cancelar","Manter saldo e propriedade atuais.",OpenMenu);
            Add("Confirmar compra: $"+options.Price,"Pagamento unico pelo banco RPCore; nao inclui garagem ou veiculo.",Buy);menu.Visible=true;
        }
        void Buy()
        {
            if (!SafeFoot || !AtEntrance || transit != null || gate.Owner != null) { Notify.Show("Va a pe a entrada, sem procurado/missao/atividade em andamento."); return; }
            if (!Wallet.HousingPurchase(options, Guid.NewGuid().ToString("N"), out _)) { Notify.Show("Confira banco RPCore, propriedade existente ou dados de moradia."); return; }
            Notify.Show("Apartamento adquirido. Escolha Entrar para carregar o interior."); OpenMenu();
        }
        void Begin(bool exit)
        {
            if (transit != null) { Release(); return; }
            if (!(exit ? ExitFoot : SafeFoot) || (exit ? !Inside : !AtEntrance || !Wallet.Housing.Valid || !Wallet.Housing.Owns(options.Id))) { Notify.Show("Use a entrada/saida correta a pe, sem procurado/missao/combate."); return; }
            if (!gate.Acquire(Name)) { Notify.Show("Encerre a atividade de " + gate.Owner + "."); return; }
            owner = Game.Player.Character; origin = owner.Position; profile = Profile.Id; exiting = exit; transit = new HousingTransit(Game.GameTime, options.LoadSeconds); nextProbe = Game.GameTime;
            ui.Close(); if (!exit) Function.Call(Hash.REQUEST_IPL, options.Ipl); Notify.Show("Carregando destino. Aguarde parado; End cancela. Sem nova cobranca.");
        }
        bool Ready(JobPoint destination, out Vector3 safe)
        {
            safe = Vector3.Zero; var point = Pos(destination); Function.Call(Hash.REQUEST_COLLISION_AT_COORD, point.X, point.Y, point.Z);
            if (!exiting)
            {
                if (!Function.Call<bool>(Hash.IS_IPL_ACTIVE, options.Ipl)) return false;
                int room = Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS, point.X, point.Y, point.Z);
                if (room == 0 || !Function.Call<bool>(Hash.IS_INTERIOR_READY, room)) return false;
            }
            var hit = World.Raycast(point + new Vector3(0,0,.3f), point - new Vector3(0,0,2.2f), IntersectFlags.Map | IntersectFlags.Objects, owner);
            if (!hit.DidHit || !HousingPlacement.Floor(point.Z, hit.HitPosition.Z, hit.SurfaceNormal.Z)) return false;
            foreach (var ped in World.GetNearbyPeds(point, 1)) if (ped != owner && ped.Exists() && Math.Abs(ped.Position.Z-point.Z) < 2) return false;
            foreach (var v in World.GetNearbyVehicles(point, 2)) if (v.Exists() && Math.Abs(v.Position.Z-point.Z) < 3) return false;
            safe = new Vector3(point.X,point.Y,hit.HitPosition.Z+1); return true;
        }
        void TransitTick()
        {
            bool eligible = owner != null && owner.Exists() && owner == Game.Player.Character && profile == Profile.Id && !InterfaceOptions.ModalOpen && !pool.AreAnyVisible && (exiting ? ExitFoot : SafeFoot && Wallet.Housing.Valid && Wallet.Housing.Owns(options.Id)) && owner.Position.DistanceTo(origin) < 3;
            if (Game.IsKeyPressed(Keys.End)) eligible = false;
            if (!eligible) { Release(); Notify.Show("Acesso cancelado; posicao e propriedade preservadas."); return; }
            if (unchecked(Game.GameTime-nextProbe) < 0) return; nextProbe = Game.GameTime + 150;
            var destination = exiting ? options.Exit : options.Interior; Vector3 safe;
            bool ready = Ready(destination, out safe);
            if (transit.Update(Game.GameTime, true, ready, () => { owner.Position = safe; owner.Heading = destination.Heading; owner.Velocity = Vector3.Zero; }))
            { Release(); Notify.Show("Acesso concluido."); }
            else if (transit.Stage == HousingTransitStage.Cancelled) { Release(); Notify.Show("Destino ainda indisponivel/ocupado. Voce nao foi movido; tente novamente."); }
        }
        void Release() { transit = null; owner = null; gate.Release(Name); }
        public void Tick()
        {
            try
            {
                ui.Tick(Inside?ExitFoot:SafeFoot);
                if (transit != null) { TransitTick(); return; }
                if (!(Inside ? ExitFoot : SafeFoot) || InterfaceOptions.ModalOpen || pool.AreAnyVisible) return;
                var point = Inside ? Pos(options.Interior) : Pos(options.Entrance);
                float distance = Game.Player.Character.Position.DistanceTo(point); if (distance > 25) return;
                World.DrawMarker(MarkerType.Cylinder, point-new Vector3(0,0,1), Vector3.Zero,Vector3.Zero,new Vector3(1,1,.2f),Color.FromArgb(130,70,150,240));
                if (distance <= 2)
                { GTA.UI.Screen.ShowHelpTextThisFrame(Inside ? "Pressione ~INPUT_CONTEXT~ para sair do apartamento" : "Pressione ~INPUT_CONTEXT~ para acessar Eclipse Towers"); if (Game.IsControlJustPressed(GTA.Control.Context)) { if (Inside) Begin(true); else OpenMenu(); } }
            }
            catch (Exception ex) { Release(); Log.Error("housing access", ex); }
        }
        public void OnKey(KeyEventArgs e) { }
        public void Save() { }
        public void Reload() { Release(); profile = Profile.Id; ui.Reset(); }
        public void Dispose() { Release(); ui.Reset(); if (blip != null && blip.Exists()) blip.Delete(); }
    }
}
