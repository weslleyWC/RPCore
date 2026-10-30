using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
namespace RPCore
{
    public sealed class PdmSurveillanceSystem : ISystem
    {
        readonly PersonalServiceMenu ui;
        public string Name => "pdmsurveillance";
        public bool Viewing => session != null;
        readonly ObjectPool pool; readonly ShiftGate gate; readonly Vector3 office; readonly Func<bool> officeAction; readonly Action back;
        readonly PdmSurveillanceOptions options; readonly NativeMenu menu; readonly Dictionary<string, Prop> props = new Dictionary<string, Prop>();
        PdmMonitorSession session; Camera camera; bool renderRequested; string profile; int nextProps, health, armor;
        static Vector3 Pos(JobPoint p) => new Vector3(p.X, p.Y, p.Z);
        public PdmSurveillanceSystem(ObjectPool pool, ShiftGate gate, Vector3 office, Func<bool> officeAction, Action back)
        {
            this.pool = pool; this.gate = gate; this.office = office; this.officeAction = officeAction; this.back = back;
            var defaults = PdmSurveillanceDefaults.Load(); options = Store.LoadDef("Jobs/PdmSurveillance.json", () => defaults);
            if (options == null || !options.Valid) { Log.Write("Invalid PdmSurveillance.json; embedded defaults used, original retained."); options = defaults; }
            menu = new NativeMenu("PDM / cameras", "EQUIPAMENTOS / MONITORAMENTO"); ui=new PersonalServiceMenu(menu,ex=>{ui.Close();Log.Error(Name+" menu dispatch",ex);Notify.Show("Menu PDM interrompido; dados preservados. Tente abrir novamente.");}); pool.Add(menu); Visuals.Register(menu, "pdm"); profile = Profile.Id;
        }
        void Add(string title, string description, Action action)
        { ui.Add(title, description, () => { try { action(); } catch (Exception ex) { ui.Close(); Close(); Log.Error("pdm surveillance action", ex); Notify.Show("Monitoramento indisponivel; compra/saves preservados."); } }); }
        public void OpenMenu()
        {
            Close(); pool.HideAll(); ui.Begin("pdm",null,false);
            if (!Wallet.Pdm.Valid) { Notify.Show("Dados PDM invalidos; original preservado."); return; }
            if (!Wallet.Pdm.Surveillance.Purchased)
                Add("Instalar cameras: $" + options.Price, "Compra unica pelo caixa empresarial, sem mensalidade. Nao inclui seguro/indenizacao.", () => { if (!officeAction()) return; if (!Wallet.PdmSurveillancePurchase(options, Guid.NewGuid().ToString("N"), out _)) Notify.Show("Confira caixa/empresa/operacao pendente. Nada confirmado."); OpenMenu(); });
            else
            {
                Add("Equipamentos adquiridos / $" + Wallet.Pdm.Surveillance.Paid, "Tres pontos de referencia; apenas nossos props sao recolhidos. Recursos equivalentes do mapa sao conservados.", () => { });
                for (int i = 0; i < options.Cameras.Count; i++) { int index = i; Add("Ver: " + options.Cameras[i].Name, "No escritorio, a pe e fora de atividade. Setas esquerda/direita trocam; Backspace/Esc sai. Limite " + options.MaxViewSeconds + "s.", () => Begin(index)); }
            }
            Add("GPS: escritorio PDM", "Monitoramento disponivel apenas junto ao escritorio externo.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, office.X, office.Y); menu.Visible = false; });
            Add("Voltar", "Empresa PDM.", back); menu.Visible = true;
        }
        bool NearbyEquivalent(PdmCameraPoint point) => World.GetNearbyProps(Pos(point.Mount), .6f, new Model(options.Model)).Any(p => p.Exists());
        bool Hardware(PdmCameraPoint point) { Prop p; return props.TryGetValue(point.Id, out p) && p.Exists() || NearbyEquivalent(point); }
        void SpawnProps()
        {
            var model = new Model(options.Model);
            try
            {
                if (!model.IsValid || !model.IsInCdImage || !model.Request(1500)) return;
                foreach (var point in options.Cameras)
                {
                    Prop old; if (props.TryGetValue(point.Id, out old)) { if (old.Exists()) continue; props.Remove(point.Id); }
                    if (NearbyEquivalent(point)) continue;
                    var p = World.CreateProp(model, Pos(point.Mount), false, false); if (p == null || !p.Exists()) continue;
                    props[point.Id] = p; p.Rotation = new Vector3(0, 0, point.Mount.Heading); p.IsPersistent = true; p.IsPositionFrozen = true; p.IsCollisionEnabled = false;
                }
            }
            finally { model.MarkAsNoLongerNeeded(); }
        }
        bool SafeFoot => !Game.IsPaused && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && !Game.Player.Character.IsDead && Game.Player.Character.IsHuman && !Game.Player.Character.IsInVehicle() && !Game.Player.Character.IsRagdoll && !Game.Player.Character.IsInCombat && Game.Player.Wanted.WantedLevel == 0 && Game.Player.Character.Position.DistanceTo(office) <= 5;
        bool OwnCamera => camera != null && camera.Exists() && ScriptCameraDirector.RenderingCam?.Handle == camera.Handle;
        void Begin(int index)
        {
            if (!Wallet.Pdm.Valid || !Wallet.Pdm.Surveillance.Purchased || !SafeFoot || !officeAction()) return;
            if (!Function.Call<bool>(Hash.IS_GAMEPLAY_CAM_RENDERING) || InterfaceOptions.ModalOpen) { Notify.Show("Feche a outra camera/interface antes de usar o monitoramento."); return; }
            SpawnProps(); if (!Hardware(options.Cameras[index])) { Notify.Show("Camera ainda indisponivel; aguarde carregamento do equipamento."); return; }
            if (!gate.Acquire(Name)) { Notify.Show("Encerre a atividade de " + gate.Owner + "."); return; }
            bool ready = false;
            try
            {
                session = new PdmMonitorSession(options.Cameras.Count, index, Game.GameTime, options.MaxViewSeconds); profile = Profile.Id;
                health = Game.Player.Character.Health; armor = Game.Player.Character.Armor;
                camera = Camera.Create(ScriptedCameraNameHash.DefaultScriptedCamera, true);
                if (camera == null || !camera.Exists()) return;
                Aim(); pool.HideAll(); renderRequested = true; ScriptCameraDirector.StartRendering(); InterfaceOptions.ModalOpen = true; ready = true;
            }
            finally { if (!ready) Close(); }
        }
        void Aim() { var point = options.Cameras[session.Index]; camera.Position = Pos(point.View); camera.FieldOfView = options.FieldOfView; camera.PointAt(Pos(point.LookAt)); }
        void Close()
        {
            try
            {
                var current = ScriptCameraDirector.RenderingCam;
                if (renderRequested && (current == null || current.Handle == camera?.Handle)) ScriptCameraDirector.StopRendering();
            }
            finally
            {
                try { if (camera != null && camera.Exists()) camera.Delete(); }
                finally { camera = null; renderRequested = false; session?.Close(); session = null; gate.Release(Name); }
            }
        }
        void ClearProps() { foreach (var p in props.Values) if (p != null && p.Exists()) p.Delete(); props.Clear(); }
        public void Tick()
        {
            ui.Tick(PdmMenuRuntime.Allowed);
            try
            {
                bool installed = profile == Profile.Id && Wallet.Pdm.Valid && Wallet.Pdm.Owned && Wallet.Pdm.Surveillance.Purchased;
                if (!installed || Game.Player.Character.Position.DistanceTo(office) > 180) { Close(); ClearProps(); return; }
                if (Viewing)
                {
                    var current = ScriptCameraDirector.RenderingCam;
                    bool rendering = session.Rendering(Game.GameTime, OwnCamera, current != null && current.Handle != camera?.Handle);
                    bool eligible = SafeFoot && rendering && !pool.AreAnyVisible && Hardware(options.Cameras[session.Index]) && Game.Player.Character.Health >= health && Game.Player.Character.Armor >= armor;
                    if (!session.Continue(Game.GameTime, eligible)) { Close(); return; }
                    Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, 0); Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, 199, true); Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, 200, true);
                    Visuals.Rect(20, 900, 1050, 80, Color.FromArgb(220, 8, 12, 18));
                    Visuals.Text("PDM / AO VIVO / " + options.Cameras[session.Index].Name, 35, 910, .32f, Color.White);
                    Visuals.Text("Esquerda/Direita: trocar camera   Backspace/Esc: sair", 35, 945, .26f, Color.White);
                }
                if (!Game.IsMissionActive && !Game.IsPaused && unchecked(Game.GameTime - nextProps) >= 0) { nextProps = Game.GameTime + 5000; SpawnProps(); }
            }
            catch (Exception ex) { Close(); nextProps = Game.GameTime + 5000; Log.Error("pdm surveillance", ex); }
        }
        public void OnKey(KeyEventArgs e)
        {
            if (!Viewing) return;
            try
            {
                if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Back || e.KeyCode == Keys.End || e.KeyCode == Visuals.PhoneKey || e.KeyCode == InterfaceOptions.SettingsKey || e.KeyCode == InterfaceOptions.RadialKey) { Close(); return; }
                int direction = e.KeyCode == Keys.Left ? -1 : e.KeyCode == Keys.Right ? 1 : 0;
                if (direction != 0 && session.Switch(direction)) { if (!Hardware(options.Cameras[session.Index])) { Close(); Notify.Show("Camera indisponivel; visao normal restaurada."); } else Aim(); }
            }
            catch (Exception ex) { Close(); Log.Error("pdm surveillance key", ex); }
        }
        public void Save() { }
        public void Reload() { ui.Reset(); Close(); ClearProps(); profile = Profile.Id; nextProps = Game.GameTime; menu.Visible = false; }
        public void Dispose() { ui.Reset(); try { Close(); } finally { ClearProps(); menu.Visible = false; } }
    }
}
