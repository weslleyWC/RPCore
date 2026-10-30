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
    public sealed class PdmPremisesSystem : ISystem, IWorkContract
    {
        readonly PersonalServiceMenu ui;
        sealed class Employee { public string Id; public Ped Ped; public Blip Blip; }
        public string Name => "pdmpremises";
        public string Label => "PDM / equipe presencial e servicos";
        public bool Active => run != null;
        public IEnumerable<Ped> StaffPeds => actors.Values.Select(a => a.Ped);
        public Vector3 StartPosition { get; }
        readonly ObjectPool pool; readonly ShiftGate gate; readonly Func<bool> office; readonly Func<string, Vehicle> display; readonly Action back;
        readonly PdmOptions company; readonly PdmPremisesOptions options; readonly NativeMenu menu;
        readonly Dictionary<string, Employee> actors = new Dictionary<string, Employee>(); readonly Dictionary<string, int> respawn = new Dictionary<string, int>();
        PdmServiceRun run; Vehicle target; Employee worker; Prop sponge; string profile, stepId, cancelReason, animDict, animClip;
        int last, retryAt, nextActors, nextWalk, began, stepSeconds, cancelSince = -1; long pending; bool requested, pose;
        static Vector3 Pos(JobPoint p) => new Vector3(p.X, p.Y, p.Z);
        static bool Exists(Vehicle v) => v != null && v.Exists();
        bool InputAllowed => !Game.IsPaused && !InterfaceOptions.ModalOpen && !pool.AreAnyVisible;
        bool EmptyTarget => Exists(target) && target.Occupants.Length == 0 && target.Speed < .5f;
        public PdmPremisesSystem(ObjectPool pool, ShiftGate gate, PdmOptions company, PdmPremisesOptions options, Func<bool> office, Func<string, Vehicle> display, Action back)
        {
            this.pool = pool; this.gate = gate; this.company = company; this.options = options; this.office = office; this.display = display; this.back = back; StartPosition = Pos(company.Office);
            menu = new NativeMenu("PDM / servicos", "MECANICO / ZELADORIA / VITRINE"); ui=new PersonalServiceMenu(menu,ex=>{ui.Close();Log.Error(Name+" menu dispatch",ex);Notify.Show("Menu PDM interrompido; dados preservados. Tente abrir novamente.");}); pool.Add(menu); Visuals.Register(menu, "pdm"); profile = Profile.Id; last = Game.GameTime; Recover();
        }
        void Sample()
        {
            int now = Game.GameTime, elapsed = unchecked(now - last); last = now;
            if (profile != Profile.Id || !options.EnableDirt || !Wallet.Pdm.Valid || !Wallet.Pdm.Owned || Wallet.Pdm.Showroom.Count == 0) { pending = 0; stepId = null; return; }
            if (!Game.IsPaused && !Game.IsMissionActive && !Game.Player.Character.IsDead && elapsed > 0) pending += Math.Min(2000, elapsed);
        }
        public void Flush()
        {
            Sample(); if (profile != Profile.Id || !options.EnableDirt || !Wallet.Pdm.Valid) return;
            while (pending >= 1000)
            {
                if (stepId == null) { stepId = Guid.NewGuid().ToString("N"); stepSeconds = (int)Math.Min(60, pending / 1000); }
                if (!Wallet.PdmDirtWork(options, stepSeconds, stepId, out _)) throw new InvalidOperationException("PDM dirt checkpoint refused; time retained.");
                pending -= stepSeconds * 1000; stepId = null;
            }
        }
        void Add(string title, string text, Action action)
        { ui.Add(title, text, () => { try { action(); } catch (Exception ex) { ui.Close(); Log.Error("pdm premises action", ex); Notify.Show("Servico nao confirmado; dados preservados."); } }); }
        public void OpenMenu()
        {
            Flush(); pool.HideAll(); ui.Begin("pdm",null,false); if (!Wallet.Pdm.Valid) { Notify.Show("Dados PDM invalidos; original preservado."); return; }
            Add("GPS: equipe PDM", "Contrate/pague em Equipe / salarios. Servicos presenciais atendem apenas os carros expostos da empresa.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, StartPosition.X, StartPosition.Y); menu.Visible = false; });
            foreach (var e in Wallet.Pdm.Showroom)
            {
                var stock = Wallet.Pdm.Stock.Find(s => s.Id == e.StockId); string id = stock.Id;
                Add(stock.Name + " / sujeira " + stock.Dirt + "/15", "Motor " + stock.Engine + " / carroceria " + stock.Body + ". Sujeira reduz ate 7,5% da cotacao; nao altera contratos ja fechados.", () => OpenStock(id));
            }
            if (Active) Add("Cancelar servico", "Antes da confirmacao nao debita dinheiro nem altera condicao do estoque.", () => { cancelReason = "Cancelado"; menu.Visible = false; });
            if (!Active && Wallet.Pdm.PremisesService != null) Add("Recuperar servico interrompido", "Encerra intencao nao confirmada, sem cobrar/reparar/limpar parcialmente.", () => { Recover(); OpenMenu(); });
            Add("Voltar", "Empresa PDM / equipe.", back); menu.Visible = true;
        }
        void OpenStock(string id)
        {
            pool.HideAll(); ui.Begin("pdm",null,false); var stock = Wallet.Pdm.Stock.Find(s => s.Id == id); if (stock == null) return;
            menu.Name = stock.Name + " / servicos";
            Add("Solicitar mecanico: $" + PdmStaffTransactions.RepairPrice(Wallet.Pdm, company.RepairPrice), "Mecanico contratado/sem atraso vai ao carro e repara. Oficina adquirida mantem desconto. Paga so ao concluir, sem recolher a vaga.", () => Begin(id, "repair"));
            Add("Solicitar zeladoria: sem taxa por servico", "Zeladoria contratada/sem atraso limpa sujeira e recupera cotacao. Salario continua devido pelo tempo trabalhado.", () => Begin(id, "clean"));
            Add("Voltar", "Carros expostos.", OpenMenu); menu.Visible = true;
        }
        Vector3 FreePoint(JobPoint preferred)
        {
            foreach (var p in new[] { Pos(preferred), StartPosition + new Vector3(4, -3, 0), StartPosition + new Vector3(-4, -3, 0), StartPosition + new Vector3(0, 5, 0) })
            { var safe = JobActors.Sidewalk(p); if (safe != Vector3.Zero && safe.DistanceTo(StartPosition) < 80 && JobActors.Free(safe, 2)) return safe; }
            return Vector3.Zero;
        }
        void Actors()
        {
            bool near = Wallet.Pdm.Owned && !Game.IsMissionActive && Game.Player.Character.Position.DistanceTo(StartPosition) < 180;
            foreach (string role in new[] { "mechanic", "cleaner" })
            {
                var saved = Wallet.Pdm.Staff.Employees.Find(e => e.Role == role); Employee a; actors.TryGetValue(role, out a);
                if (a != null && (saved == null || saved.Id != a.Id || !near || a.Ped == null || !a.Ped.Exists() || a.Ped.IsDead))
                {
                    if (worker == a && Active) { cancelReason = "Funcionario indisponivel"; continue; }
                    if (a.Ped != null && a.Ped.Exists() && a.Ped.IsDead) respawn[role] = Game.GameTime + 30000;
                    DeleteActor(a); actors.Remove(role); a = null;
                }
                int due; if (!near || saved == null || a != null || respawn.TryGetValue(role, out due) && unchecked(Game.GameTime - due) < 0) continue;
                var model = new Model(role == "mechanic" ? options.MechanicModel : options.CleanerModel);
                try
                {
                    if (!model.IsValid || !model.IsPed || !model.IsInCdImage || !model.Request(2500)) continue;
                    var point = FreePoint(role == "mechanic" ? options.Mechanic : options.Cleaner); if (point == Vector3.Zero) continue;
                    var ped = World.CreatePed(model, point); if (ped == null || !ped.Exists()) continue;
                    ped.IsPersistent = true; ped.BlockPermanentEvents = true; Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, ped, "WORLD_HUMAN_CLIPBOARD", 0, true);
                    var blip = ped.AddBlip(); blip.Name = role == "mechanic" ? "RPCore: mecanico PDM" : "RPCore: zeladoria PDM"; blip.Color = BlipColor.Blue;
                    actors[role] = new Employee { Id = saved.Id, Ped = ped, Blip = blip };
                }
                finally { model.MarkAsNoLongerNeeded(); }
            }
        }
        static void DeleteActor(Employee a) { if (a.Blip != null && a.Blip.Exists()) a.Blip.Delete(); if (a.Ped != null && a.Ped.Exists()) a.Ped.Delete(); }
        void Begin(string stockId, string kind)
        {
            Flush(); if (Active || !office()) return;
            string role = kind == "repair" ? "mechanic" : "cleaner"; Employee a;
            if (!PdmPremisesTransactions.Working(Wallet.Pdm, role) || !actors.TryGetValue(role, out a) || a.Ped == null || !a.Ped.Exists() || a.Ped.IsDead) { Notify.Show("Contrate/pague o funcionario e aguarde a chegada perto da PDM."); return; }
            var vehicle = display(stockId); if (!Exists(vehicle) || vehicle.Occupants.Length > 0 || vehicle.Speed >= .5f) { Notify.Show("Carro exposto indisponivel/ocupado. Aguarde a vaga e deixe o veiculo vazio."); return; }
            if (!gate.Acquire(Name)) { Notify.Show("Encerre a atividade de " + gate.Owner + "."); return; }
            bool ready = false;
            try
            {
                string id = Guid.NewGuid().ToString("N"); if (!Wallet.PdmServiceBegin(company, stockId, kind, id, out _)) { Notify.Show("Confira danos/sujeira, caixa, salario ou operacao pendente. Nada cobrado."); return; }
                run = new PdmServiceRun(id, stockId, kind, kind == "repair" ? options.RepairSeconds : options.CleanSeconds); target = vehicle; worker = a; profile = Profile.Id;
                began = retryAt = Game.GameTime; cancelReason = null; animDict = kind == "repair" ? "mini@repair" : "amb@world_human_maid_clean@idle_a"; animClip = kind == "repair" ? "fixing_a_ped" : "idle_a";
                Function.Call(Hash.REQUEST_ANIM_DICT, animDict); requested = true; Walk(); menu.Visible = false; ready = true; Notify.Show("Funcionario a caminho. Aguarde perto do carro; cobranca/efeito apenas ao concluir.");
            }
            finally { if (!ready) gate.Release(Name); }
        }
        bool WorkerAlive => worker != null && worker.Ped != null && worker.Ped.Exists() && !worker.Ped.IsDead;
        bool Eligible => InputAllowed && EmptyTarget && WorkerAlive && !worker.Ped.IsInCombat && !worker.Ped.IsRagdoll && worker.Ped.Position.DistanceTo(target.Position) < 4 && Game.Player.Character.Position.DistanceTo(target.Position) < 30;
        void Walk()
        { if (!WorkerAlive || !Exists(target)) return; var p = JobActors.Sidewalk(target.RightPosition); if (p == Vector3.Zero || p.DistanceTo(target.Position) > 4) p = target.RightPosition - new Vector3(0, 0, .5f); JobActors.Walk(worker.Ped, p); nextWalk = Game.GameTime + 5000; }
        bool StartPose()
        {
            if (!requested) { Function.Call(Hash.REQUEST_ANIM_DICT, animDict); requested = true; }
            if (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, animDict)) return false;
            if (!pose)
            {
                worker.Ped.Heading = (target.Position - worker.Ped.Position).ToHeading();
                Function.Call(Hash.TASK_PLAY_ANIM, worker.Ped, animDict, animClip, 4f, -4f, -1, 1, 0f, false, false, false); pose = true;
                if (run.Kind == "clean")
                {
                    var model = new Model("prop_sponge_01");
                    try { if (model.IsValid && model.IsInCdImage && model.Request(1000)) { sponge = World.CreateProp(model, worker.Ped.Position, false, false); if (sponge != null && sponge.Exists()) { sponge.IsCollisionEnabled = false; sponge.AttachTo(worker.Ped.Bones[Bone.SkelRightHand], new Vector3(.05f, 0, 0), Vector3.Zero); } } }
                    finally { model.MarkAsNoLongerNeeded(); }
                }
            }
            return true;
        }
        void ServiceTick()
        {
            if (run.Stage == PdmServiceStage.Complete) { FinishWorld(); return; }
            if (cancelReason != null) { Cancel(); return; }
            var op = Wallet.Pdm.PremisesService;
            if (!WorkerAlive || !Exists(target) || target.IsDead || Game.Player.Character.IsDead || Game.IsMissionActive || Game.Player.Wanted.WantedLevel > 0 || Game.Player.Character.Position.DistanceTo(StartPosition) > 100 || unchecked(Game.GameTime - began) > options.TimeoutSeconds * 1000 || !PdmPremisesTransactions.Working(Wallet.Pdm, run.Kind == "repair" ? "mechanic" : "cleaner"))
            { cancelReason = "Servico interrompido"; Cancel(); return; }
            Status(run.Kind == "repair" ? "mecanico / aguarde reparo no proprio carro exposto" : "zeladoria / aguarde limpeza no proprio carro exposto");
            if (run.Stage == PdmServiceStage.Approaching)
            {
                if (Eligible && StartPose()) run.Start(Game.GameTime, true);
                else if (!pose && unchecked(Game.GameTime - nextWalk) >= 0) Walk();
                return;
            }
            if (!Eligible) { StopPose(); run.Commit(Game.GameTime, false, () => false); return; }
            if (unchecked(Game.GameTime - retryAt) < 0) return;
            bool attempted = false;
            if (run.Commit(Game.GameTime, true, () => { attempted = true; return Wallet.PdmServiceComplete(run.Id, out _); })) { Notify.Show(op.Kind == "repair" ? "Reparo concluido: -$" + op.Price + " do caixa empresarial." : "Limpeza concluida, sem taxa adicional; cotacao recuperada."); FinishWorld(); }
            else if (attempted) { retryAt = Game.GameTime + 5000; Notify.Show("Servico pendente: confira caixa/salario/dados. Pode cancelar sem cobrar."); }
        }
        void FinishWorld()
        {
            StopPose(); var stock = Wallet.Pdm.Stock.Find(s => s.Id == run.StockId);
            if (EmptyTarget && stock != null)
            { if (run.Kind == "repair") target.Repair(); target.EngineHealth = stock.Engine; target.BodyHealth = stock.Body; target.DirtLevel = stock.Dirt; target.IsPositionFrozen = true; target.IsInvincible = true; Function.Call(Hash.SET_VEHICLE_DOORS_LOCKED, target, 2); }
            ClearService();
        }
        void Cancel()
        {
            StopPose(); if (unchecked(Game.GameTime - retryAt) < 0) return;
            if (!run.Cancel(() => Wallet.PdmServiceCancel(run.Id, out _))) { retryAt = Game.GameTime + 5000; return; }
            ClearService(); Notify.Show("Servico cancelado sem cobranca nem reparo/limpeza parcial. Estoque/vaga preservados.");
        }
        void StopPose()
        {
            if (pose && WorkerAlive) Function.Call(Hash.STOP_ANIM_TASK, worker.Ped, animDict, animClip, 2f); pose = false;
            if (sponge != null && sponge.Exists()) sponge.Delete(); sponge = null;
            if (requested) { Function.Call(Hash.REMOVE_ANIM_DICT, animDict); requested = false; }
        }
        void ClearService()
        {
            try { if (WorkerAlive) JobActors.Walk(worker.Ped, StartPosition); }
            finally { run = null; worker = null; target = null; cancelReason = null; gate.Release(Name); menu.Visible = false; }
        }
        void Recover()
        { var a = Wallet.Pdm.PremisesService; if (a == null || !Wallet.Pdm.Valid || Active) return; try { if (Wallet.PdmServiceCancel(a.Id, out _)) Notify.Show("Servico interrompido recuperado sem cobranca; estoque/vaga mantidos."); } catch (Exception ex) { Log.Error("pdm service recovery", ex); } }
        public void Tick()
        {
            ui.Tick(PdmMenuRuntime.Allowed);
            try
            {
                Sample(); if (pending >= 5000 && unchecked(Game.GameTime - retryAt) >= 0) Flush();
                if (Wallet.Pdm.Valid && unchecked(Game.GameTime - nextActors) >= 0) { nextActors = Game.GameTime + 5000; Actors(); }
                if (Active) ServiceTick();
            }
            catch (Exception ex) { retryAt = Game.GameTime + 5000; Log.Error("pdm premises checkpoint", ex); }
        }
        void ReleaseWorld()
        { StopPose(); foreach (var a in actors.Values) DeleteActor(a); actors.Clear(); respawn.Clear(); run = null; worker = null; target = null; cancelReason = null; gate.Release(Name); menu.Visible = false; }
        void Status(string text) { if (!Visuals.TryJobHeader("pdm", "PDM / manutencao", Color.White)) Visuals.Text("PDM / manutencao", Visuals.Width / 2 - 200, 24, .34f, Color.White); float x = Visuals.Width / 2 - 390; Visuals.Rect(x, 900, 780, 80, Color.FromArgb(210, 8, 12, 18)); Visuals.Text(text, x + 12, 910, .29f, Color.White); Visuals.Text("End: cancelar antes da confirmacao / pague salarios para manter atendimento", x + 12, 945, .23f, Color.White); }
        public void PollCancel() { if (!Active || !InputAllowed || !Game.IsKeyPressed(Keys.End)) { cancelSince = -1; return; } if (cancelSince < 0) cancelSince = Game.GameTime; if (unchecked(Game.GameTime - cancelSince) >= 1000) cancelReason = "Cancelado pelo jogador"; }
        public void Save() { if (profile == Profile.Id) Flush(); }
        public void Reload() { ui.Reset(); ReleaseWorld(); profile = Profile.Id; pending = 0; stepId = null; last = nextActors = Game.GameTime; Recover(); }
        public void OnKey(KeyEventArgs e) { }
        public void Dispose() { ui.Reset(); try { Save(); if (Active && profile == Profile.Id && run.Stage != PdmServiceStage.Complete) Wallet.PdmServiceCancel(run.Id, out _); } finally { ReleaseWorld(); } }
    }
}
