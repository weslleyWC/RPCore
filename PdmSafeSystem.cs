using System;
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
    public sealed class PdmSafeSystem : ISystem, IWorkContract
    {
        readonly PersonalServiceMenu ui;
        public string Name => "pdmsafe";
        public string Label => "PDM / coleta de valores";
        public bool Active => run != null || settled;
        public Vector3 StartPosition => Pos(options.Handover);
        readonly ObjectPool pool; readonly ShiftGate gate; readonly Func<bool> office; readonly Action back;
        readonly NativeMenu menu; readonly PdmSafeOptions options;
        Vehicle van; Ped collector, animPlayer; Prop briefcase; Blip vanBlip, pedBlip;
        PdmSafeRun run; string cancelReason;
        int began, stageAt, retryAt, cancelSince = -1; bool driveStarted, pose, animRequested, settled;
        const string Decor = "RPCoreSafe", Anim = "mp_common";
        static Vector3 Pos(JobPoint p) => new Vector3(p.X, p.Y, p.Z);
        static bool Exists(Entity e) => e != null && e.Exists();
        bool InputAllowed => !Game.IsPaused && !InterfaceOptions.ModalOpen && !pool.AreAnyVisible;
        bool SafeFoot => Game.Player.Character.IsHuman && !Game.Player.Character.IsDead && !Game.Player.Character.IsInVehicle() && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsSwimming && !Game.Player.Character.IsRagdoll;
        bool HandEligible => InputAllowed && SafeFoot && Game.Player.Character.Weapons.Current.Hash == WeaponHash.Unarmed && Exists(collector) && !collector.IsDead && !collector.IsInVehicle() && !collector.IsInCombat && collector.Position.DistanceTo(StartPosition) < 8 && Game.Player.Character.Position.DistanceTo(collector.Position) < 2.5f && Exists(van) && van.Speed < .5f;
        public PdmSafeSystem(ObjectPool pool, ShiftGate gate, Func<bool> office, Action back)
        {
            this.pool = pool; this.gate = gate; this.office = office; this.back = back;
            var defaults = PdmSafeDefaults.Load(); options = Store.LoadDef("Jobs/PdmSafe.json", () => defaults);
            if (options == null || !options.Valid) { Log.Write("Invalid PdmSafe.json; embedded defaults used, original retained."); options = defaults; }
            menu = new NativeMenu("PDM / valores", "COLETA / ENTREGA / COMPENSACAO"); ui=new PersonalServiceMenu(menu,ex=>{ui.Close();Log.Error(Name+" menu dispatch",ex);Notify.Show("Menu PDM interrompido; dados preservados. Tente abrir novamente.");}); pool.Add(menu); Visuals.Register(menu, "pdm"); Recover();
        }
        void Add(string title, string text, Action action)
        {
            ui.Add(title, text, () => { try { action(); } catch (Exception ex) { ui.Close(); Log.Error("pdm safe action", ex); Notify.Show("Coleta pendente; dados preservados. Veja RPCore.log."); } });
        }
        public void OpenMenu()
        {
            pool.HideAll(); ui.Begin("pdm",null,false); if (!Wallet.Pdm.Valid) { Notify.Show("Dados PDM invalidos; arquivo preservado."); return; }
            menu.Name = "Caixa $" + Wallet.Pdm.Cash + " / em coleta $" + (Wallet.Pdm.SafeTransfer?.Amount ?? 0);
            Add("GPS: entrega de valores PDM", "Inicie a pe no escritorio externo da empresa.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, StartPosition.X, StartPosition.Y); menu.Visible = false; });
            if (Wallet.Pdm.SafeTransfer == null && !Active)
            {
                foreach (int amount in new[] { 1000, 10000, 50000 }) { int selected = amount; Add("Solicitar coleta: $" + selected, "Reserva do caixa; seguranca chega de Stockade. Passe os valores com E para transferir ao seu banco, sem bonus.", () => Begin(selected)); }
                int total = Math.Min(1000000, Wallet.Pdm.Cash);
                Add("Coletar saldo: $" + total, "Ate um milhao por coleta; saldo reservado nao pode ser gasto novamente.", () => Begin(total));
            }
            if (Active && Wallet.Pdm.SafeTransfer?.Handed == false) Add("Cancelar antes da entrega", "Valores reservados voltam ao caixa depois de liberar os recursos proprios.", () => { cancelReason = "Cancelada pelo jogador"; menu.Visible = false; });
            if (Wallet.Pdm.SafeTransfer != null) Add("Recuperar / concluir coleta pendente", "Antes da entrega: devolve ao caixa. Depois da entrega confirmada: compensa no banco uma unica vez. Pare e saia do carro-forte se estiver nele.", () => { if (Active && Wallet.Pdm.SafeTransfer.Handed == false) { cancelReason = "Recuperacao solicitada"; menu.Visible = false; } else { Recover(); OpenMenu(); } });
            Add("Voltar", "Gestao PDM.", back); menu.Visible = true;
        }
        static void Mark(Entity e, int token)
        {
            if (!Exists(e)) return;
            try { if (!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE, Decor, 3)) Function.Call(Hash.DECOR_REGISTER, Decor, 3); Function.Call<bool>(Hash.DECOR_SET_INT, e, Decor, token); }
            catch (Exception ex) { Log.Error("pdm safe identity marker", ex); }
        }
        static bool Marked(Entity e, int token) => Exists(e) && Function.Call<bool>(Hash.DECOR_EXIST_ON, e, Decor) && Function.Call<int>(Hash.DECOR_GET_INT, e, Decor) == token;
        bool Create(string id, out PdmSafeTransfer transfer, int amount)
        {
            transfer = null; var vehicleModel = new Model(options.VehicleModel); var pedModel = new Model(options.CollectorModel); var propModel = new Model(options.CaseModel);
            try
            {
                if (!vehicleModel.IsValid || !vehicleModel.IsInCdImage || !vehicleModel.IsVehicle || !pedModel.IsValid || !pedModel.IsPed || !pedModel.IsInCdImage || !propModel.IsValid || !propModel.IsInCdImage || !pedModel.Request(2500) || !propModel.Request(2500)) return false;
                var bay = RigSpawn.FindVehicle(options.Spawn, vehicleModel, "PDM valores"); if (bay == null) return false;
                van = World.CreateVehicle(vehicleModel, Pos(bay), bay.Heading); if (!Exists(van)) return false;
                van.IsPersistent = true; van.IsStolen = false; van.NeedsToBeHotwired = false; JobVehicles.Reserve(van);
                float capacity = van.PetrolTankVolume; van.FuelLevel = capacity > 0 && !float.IsNaN(capacity) && !float.IsInfinity(capacity) ? Math.Min(10000, capacity) : 100;
                string plate = "PSF" + id.Substring(0, 5).ToUpperInvariant(); Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, van, plate);
                var sidewalk = JobActors.Sidewalk(van.Position); if (sidewalk == Vector3.Zero) return false;
                collector = World.CreatePed(pedModel, sidewalk); if (!Exists(collector)) return false;
                collector.IsPersistent = true; collector.BlockPermanentEvents = true; collector.SetIntoVehicle(van, VehicleSeat.Driver);
                briefcase = World.CreateProp(propModel, sidewalk, false, false); if (!Exists(briefcase)) return false;
                briefcase.IsPersistent = true; briefcase.IsVisible = false; briefcase.IsCollisionEnabled = false;
                int token = BitConverter.ToInt32(Guid.Parse(id).ToByteArray(), 0); if (token == 0) token = 1;
                Mark(collector, token); Mark(briefcase, token);
                transfer = new PdmSafeTransfer { Id = id, Amount = amount, CollectorHandle = collector.Handle, CollectorModel = options.CollectorModel, CaseHandle = briefcase.Handle, CaseModel = options.CaseModel, Token = token,
                    Van = new PdmEntityRef { Handle = van.Handle, Model = options.VehicleModel, Plate = plate } }; return true;
            }
            finally { vehicleModel.MarkAsNoLongerNeeded(); pedModel.MarkAsNoLongerNeeded(); propModel.MarkAsNoLongerNeeded(); }
        }
        void Begin(int amount)
        {
            if (Active || !office() || !Wallet.Pdm.Valid || !Wallet.Pdm.Owned || Wallet.Pdm.SafeTransfer != null || amount < 1 || amount > 1000000 || Wallet.Pdm.Cash < amount) { Notify.Show("Confira caixa e contratos pendentes; inicie no escritorio externo PDM."); return; }
            if (!gate.Acquire(Name)) { Notify.Show("Encerre a atividade de " + gate.Owner + "."); return; }
            bool ready = false;
            try
            {
                string id = Guid.NewGuid().ToString("N");
                if (!Create(id, out var transfer, amount) || !Wallet.PdmSafeBegin(transfer, out _)) { Notify.Show("Coleta indisponivel: confira vaga/modelos. Caixa preservado."); return; }
                run = new PdmSafeRun(id, options.HandoverSeconds, options.ClearingSeconds); cancelReason = null; driveStarted = false; began = stageAt = retryAt = Game.GameTime;
                vanBlip = van.AddBlip(); vanBlip.Name = "RPCore: carro-forte PDM"; vanBlip.Color = BlipColor.Blue;
                JobActors.Drive(collector, van, Pos(options.Parking)); menu.Visible = false; ready = true;
                Notify.Show("Coleta solicitada: $" + amount + " reservados. Aguarde o seguranca na entrada externa PDM.");
            }
            finally { if (!ready) { Cleanup(); EndSession(); } }
        }
        void Walk()
        {
            if (collector.IsInVehicle(van)) JobActors.Leave(collector, van);
            else
            {
                JobActors.Walk(collector, StartPosition); briefcase.IsVisible = true;
                briefcase.AttachTo(collector.Bones[Bone.SkelRightHand], new Vector3(.13f, 0, -.01f), new Vector3(-30, 280, 0));
                if (pedBlip == null) { pedBlip = collector.AddBlip(); pedBlip.Name = "RPCore: seguranca / entrega de valores"; pedBlip.Color = BlipColor.Yellow; }
            }
        }
        public void Tick()
        {
            ui.Tick(PdmMenuRuntime.Allowed);
            if (!Active) return;
            try
            {
                if (unchecked(Game.GameTime - retryAt) < 0) return;
                if (settled) { Complete(); return; }
                bool handed = Wallet.Pdm.SafeTransfer?.Handed == true;
                if (cancelReason != null && !handed) { Cancel(); return; }
                if (!Exists(van) || van.IsDead || !Exists(collector) || collector.IsDead || Game.Player.Character.IsDead || Game.IsMissionActive || Game.Player.Wanted.WantedLevel > 0)
                { if (handed) Settle(); else { cancelReason = "Coleta interrompida"; Cancel(); } return; }
                if (!handed && unchecked(Game.GameTime - began) > options.ArrivalTimeoutSeconds * 1000) { cancelReason = "Tempo de coleta esgotado"; Cancel(); return; }
                if (run.Stage == PdmSafeStage.Arriving)
                {
                    Status("aguarde o carro-forte / seguranca na entrada PDM");
                    if (run.Arrive(van.Position.DistanceTo(Pos(options.Parking)) < 12 && van.Speed < 1)) { stageAt = Game.GameTime; Walk(); }
                    return;
                }
                if (run.Stage == PdmSafeStage.Walking)
                {
                    Status("a pe e desarmado / aproxime-se do seguranca / E passa os valores");
                    if (collector.IsInVehicle(van)) { if (unchecked(Game.GameTime - stageAt) > 3000) { Walk(); stageAt = Game.GameTime; } return; }
                    if (collector.Position.DistanceTo(StartPosition) > 3 && unchecked(Game.GameTime - stageAt) > 5000) { Walk(); stageAt = Game.GameTime; }
                    if (HandEligible) { Prompt.Help("~INPUT_CONTEXT~: entregar valores ao seguranca"); if (Game.IsControlJustPressed(GTA.Control.Context)) { run.Handover(Game.GameTime, true); Function.Call(Hash.REQUEST_ANIM_DICT, Anim); animRequested = true; } }
                    return;
                }
                if (run.Stage == PdmSafeStage.Handover)
                {
                    if (!HandEligible) { StopPose(); run.Hand(Game.GameTime, false, () => false); return; }
                    if (!pose && Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, Anim)) { animPlayer = Game.Player.Character; Play(animPlayer, "givetake1_b"); Play(collector, "givetake1_a"); pose = true; }
                    Status("passando valores / aguarde confirmacao");
                    if (run.Hand(Game.GameTime, true, () => Wallet.PdmSafeHand(run.Id, out _))) { StopPose(); stageAt = Game.GameTime; JobActors.Enter(collector, van, VehicleSeat.Driver); Notify.Show("Valores entregues. Aguarde saida e compensacao; nao pode mais cancelar esta transferencia."); }
                    return;
                }
                if (run.Stage == PdmSafeStage.Boarding)
                {
                    Status("seguranca embarcando / aguarde a saida do carro-forte");
                    if (collector.IsInVehicle(van) && van.Driver == collector)
                    {
                        if (!driveStarted) { JobActors.Drive(collector, van, Pos(options.Bank)); driveStarted = true; }
                        if (run.Depart(Game.GameTime, van.Speed > .5f && van.Position.DistanceTo(Pos(options.Parking)) > 10)) { if (Exists(briefcase)) briefcase.Delete(); }
                    }
                    if (unchecked(Game.GameTime - stageAt) > 45000) { Status("saida bloqueada / libere o carro-forte ou use Recuperar no menu"); }
                    return;
                }
                Status("valores em compensacao / aguarde " + options.ClearingSeconds + " segundos ativos");
                bool attempted = false;
                if (run.Credit(Game.GameTime, InputAllowed, () => { attempted = true; return Wallet.PdmSafeCredit(run.Id, out _); })) Complete();
                else if (attempted) { retryAt = Game.GameTime + 5000; Notify.Show("Compensacao pendente: confira limite do banco/dados. Valor preservado; nova tentativa em 5s."); }
            }
            catch (Exception ex) { retryAt = Game.GameTime + 5000; Log.Error("pdm safe checkpoint pending", ex); Notify.Show("Coleta pendente; valor preservado. Nova tentativa em 5s."); }
        }
        static void Play(Ped p, string clip) { if (Exists(p)) Function.Call(Hash.TASK_PLAY_ANIM, p, Anim, clip, 4f, -4f, -1, 49, 0f, false, false, false); }
        void StopPose()
        {
            if (pose) { if (Exists(animPlayer)) Function.Call(Hash.STOP_ANIM_TASK, animPlayer, Anim, "givetake1_b", 2f); if (Exists(collector)) Function.Call(Hash.STOP_ANIM_TASK, collector, Anim, "givetake1_a", 2f); }
            pose = false; animPlayer = null; if (animRequested) { Function.Call(Hash.REMOVE_ANIM_DICT, Anim); animRequested = false; }
        }
        bool OtherOccupants => Exists(van) && van.Occupants.Any(p => !Exists(collector) || p.Handle != collector.Handle);
        void Cancel()
        {
            StopPose(); Status("cancelamento: pare e saia do carro-forte / valor reservado sera devolvido");
            if (OtherOccupants) return;
            if (Exists(van) && van.Speed > .5f) { if (Exists(collector) && collector.IsInVehicle(van)) JobActors.Leave(collector, van); return; }
            Cleanup();
            if (!run.Cancel(() => Wallet.PdmSafeRefund(run.Id, out _))) { retryAt = Game.GameTime + 5000; return; }
            EndSession(); Notify.Show("Coleta cancelada antes da entrega. Valor devolvido ao caixa PDM.");
        }
        void Settle()
        {
            if (!Wallet.PdmSafeCredit(run.Id, out _)) { retryAt = Game.GameTime + 5000; Notify.Show("Compensacao pendente: confira limite do banco. Valor preservado."); return; }
            Complete();
        }
        void Complete()
        {
            settled = true; // Financial checkpoint already succeeded; cleanup retries must never refund or credit again.
            StopPose(); if (Exists(briefcase)) briefcase.Delete(); briefcase = null;
            if (Exists(collector)) JobActors.Retire(collector, van, Exists(van) && collector.IsInVehicle(van) && van.Driver == collector);
            collector = null; if (Exists(van)) JobVehicles.Retire(van); van = null;
            EndSession(); Notify.Show("Coleta compensada no banco pessoal. Valor transferido uma unica vez, sem bonus.");
        }
        static Vehicle MatchVan(PdmEntityRef r)
        {
            var v = Entity.FromHandle(r.Handle) as Vehicle;
            return Exists(v) && v.Model.Hash == new Model(r.Model).Hash && string.Equals(Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, v)?.Trim(), r.Plate, StringComparison.OrdinalIgnoreCase) ? v : null;
        }
        void Recover()
        {
            var t = Wallet.Pdm.SafeTransfer; if (t == null || !Wallet.Pdm.Valid) return;
            try
            {
                van = MatchVan(t.Van); JobVehicles.Reserve(van);
                var remembered = Entity.FromHandle(t.CollectorHandle) as Ped;
                collector = Exists(remembered) && remembered.Handle != Game.Player.Character.Handle && remembered.Model.Hash == new Model(t.CollectorModel).Hash && (Marked(remembered, t.Token) || Exists(van) && remembered.IsInVehicle(van)) ? remembered : null;
                var prop = Entity.FromHandle(t.CaseHandle) as Prop;
                briefcase = Exists(prop) && prop.Model.Hash == new Model(t.CaseModel).Hash && (Marked(prop, t.Token) || Exists(collector) && prop.IsAttachedTo(collector)) ? prop : null;
                if (t.Handed)
                {
                    if (!Wallet.PdmSafeCredit(t.Id, out _)) { Notify.Show("Coleta entregue: compensacao pendente no banco. Use Recuperar depois de liberar saldo/limite."); return; }
                    Complete(); return;
                }
                if (OtherOccupants || Exists(van) && van.Speed > .5f) { Notify.Show("Coleta interrompida: pare e saia do carro-forte. Use Recuperar no menu."); if (!OtherOccupants && Exists(collector)) JobActors.Leave(collector, van); return; }
                Cleanup(); if (!Wallet.PdmSafeRefund(t.Id, out _)) return;
                EndSession(); Notify.Show("Coleta interrompida antes da entrega. Valor preservado no caixa PDM.");
            }
            catch (Exception ex) { Log.Error("pdm safe recovery", ex); Notify.Show("Coleta pendente preservada. Use Recuperar no menu."); }
        }
        void Cleanup()
        {
            StopPose(); if (Exists(briefcase)) briefcase.Delete(); briefcase = null;
            if (Exists(collector)) collector.Delete(); collector = null;
            if (Exists(van)) { if (van.Occupants.Length == 0 && van.Speed < .5f) { van.Delete(); JobVehicles.Release(van); } else JobVehicles.Retire(van); } van = null;
        }
        void EndSession()
        {
            try { if (vanBlip != null && vanBlip.Exists()) vanBlip.Delete(); if (pedBlip != null && pedBlip.Exists()) pedBlip.Delete(); }
            finally { vanBlip = pedBlip = null; run = null; cancelReason = null; settled = false; gate.Release(Name); menu.Visible = false; }
        }
        void Status(string text) { if (!Visuals.TryJobHeader("pdm", "PDM / valores", Color.White)) Visuals.Text("PDM / valores", Visuals.Width / 2 - 200, 24, .34f, Color.White); float x = Visuals.Width / 2 - 390; Visuals.Rect(x, 900, 780, 80, Color.FromArgb(210, 8, 12, 18)); Visuals.Text(text, x + 12, 910, .29f, Color.White); Visuals.Text("End: cancelar antes da entrega / depois: aguarde banco ou Recuperar", x + 12, 945, .23f, Color.White); }
        public void PollCancel() { if (!Active || !InputAllowed || !Game.IsKeyPressed(Keys.End) || Wallet.Pdm.SafeTransfer?.Handed == true) { cancelSince = -1; return; } if (cancelSince < 0) cancelSince = Game.GameTime; if (unchecked(Game.GameTime - cancelSince) >= 1000) cancelReason = "Cancelada pelo jogador"; }
        public void OnKey(KeyEventArgs e) { }
        public void Save() { }
        public void Reload() { ui.Reset(); Cleanup(); EndSession(); Recover(); }
        public void Dispose() { ui.Reset(); Cleanup(); EndSession(); }
    }
}
