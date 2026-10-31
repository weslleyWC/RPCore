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
    public sealed class PdmRepoSystem : ISystem, IWorkContract
    {
        readonly PdmMenuView ui;
        public Action Back;
        public System.Collections.Generic.IEnumerable<PdmLocation> Locations
        {
            get{
                yield return PdmLocation.At("repo:return","Contratos","Retomada / retorno","Recebimento do patrimonio recuperado. Inicie uma retomada pelo contrato inadimplente.",options.Return);
                var saved=Wallet.Pdm.Repo;
                if(saved!=null && saved.Location>=0 && saved.Location<options.Locations.Count)
                    yield return PdmLocation.At("repo:active","Contrato atual","Retomada / veiculo atual","Destino da retomada ja iniciada; recuperar somente o veiculo identificado pelo contrato.",options.Locations[saved.Location]);
            }
        }
        public string Name => "pdmrepo";
        public string Label => "PDM / retomada";
        public bool Active => run != null;
        public Vector3 StartPosition { get; }
        readonly ObjectPool pool; readonly ShiftGate gate; readonly PdmOptions company; readonly PdmRepoOptions options; readonly PdmLogisticsOptions rig;
        readonly AchievementSystem achievements; readonly NativeMenu menu; readonly Random random = new Random();
        Vehicle truck, trailer, car; Blip route, carBlip, truckBlip, trailerBlip; PdmHaulRun run;
        string profile, cancelReason, focusedLoan; int began, retryAt, cancelSince = -1; bool removed;
        static Vector3 Pos(JobPoint p) => new Vector3(p.X, p.Y, p.Z);
        static bool Exists(Vehicle v) => v != null && PdmAssets.Valid(v);
        static bool Empty(Vehicle v) => !Exists(v) || v.Occupants.Length == 0 && v.Speed < .5f;
        static int Health(float n) => float.IsNaN(n) || float.IsInfinity(n) ? 0 : Math.Max(0, Math.Min(1000, (int)n));
        bool Foot => Game.Player.Character.IsHuman && !Game.Player.Character.IsDead && !Game.Player.Character.IsInVehicle() && !Game.Player.Character.IsSwimming && !Game.Player.Character.IsRagdoll && !Game.Player.Character.IsInCombat;
        bool InputAllowed => PdmMenuRuntime.Allowed && !pool.AreAnyVisible;
        bool Coupled => JobVehicles.Towing(truck, trailer);
        bool Foreign => Exists(truck) && Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER, truck) && !Coupled || JobVehicles.TrailerHasOtherTractor(trailer, truck);
        PdmLoan Loan => Wallet.Pdm.Loans.Find(l => l.Id == Wallet.Pdm.Repo?.LoanId);
        public PdmRepoSystem(ObjectPool pool, ShiftGate gate, PdmOptions company, PdmRepoOptions options, AchievementSystem achievements)
        {
            this.pool = pool; this.gate = gate; this.company = company; this.options = options; this.achievements = achievements; StartPosition = Pos(company.Office);
            var defaults = PdmLogisticsDefaults.Load(); rig = Store.LoadDef("Jobs/PdmLogistics.json", () => defaults);
            if (rig == null || !rig.Valid) { Log.Write("Invalid PdmLogistics.json for repossession; embedded defaults used, original retained."); rig = defaults; }
            menu = new NativeMenu("PDM / retomada", "CONTRATOS EM ATRASO / RECUPERACAO"); ui=new PdmMenuView(pool,menu,ex=>{ui.Close();Log.Error(Name+" menu dispatch",ex);Notify.Show("Menu PDM interrompido; dados preservados. Tente abrir novamente.");}); ui.DefaultRefresh=OpenMenu; pool.Add(menu); Visuals.Register(menu, "pdm"); Recover();
        }
        void Add(string title, string text, Action action)
        { ui.Add(title, text, () => { try { action(); } catch (Exception ex) { ui.Close(); Log.Error("pdm repossession action", ex); Notify.Show("Retomada pendente; dados preservados."); } }); }
        public void OpenForLoan(string id) { focusedLoan = id; OpenMenu(); }
        public void OpenMenu()
        {
            ui.HideAll(); ui.Begin(Name,Back,true,OpenMenu);menu.Name="Retomadas / "+Wallet.Pdm.Loans.Count(l=>l.Defaulted && !l.Settled)+" em atraso"; if (!Wallet.Pdm.Valid) { Notify.Show("Dados PDM invalidos; arquivo preservado."); return; }
            Add("GPS: escritorio PDM", "Comece a pe no escritorio externo. Precisa de uma vaga livre no estoque.", () => ui.Waypoint(StartPosition,"escritorio PDM"));
            foreach (var l in Wallet.Pdm.Loans.Where(l => l.Defaulted && !l.Settled).OrderBy(l => l.Id == focusedLoan ? 0 : 1))
            { var loan = l; ui.AddConfirm("Retomar " + loan.Asset.Name + " / " + loan.Asset.Plate, "Recebido $" + loan.Paid + " / saldo em atraso $" + loan.Receivable + ". Packer/TRFlat: busque o proprio carro, carregue com E, volte e descarregue. Sem bonus por recuperar patrimonio proprio.", () => ui.Stream(new[]{rig.TruckModel,rig.TrailerModel},()=>Begin(loan.Id),OpenMenu)); }
            Add("Recebiveis encerrados: $" + Wallet.Pdm.FinanceWriteOff, "Valor contratual que deixou de ser cobrado apos retomadas; nao e dinheiro pago nem um segundo desconto de caixa.", () => { });
            if (Active) ui.AddConfirm("Encerrar retomada", "Pare e saia dos veiculos. Contrato continua em atraso, com condicao preservada para nova tentativa.", () => { cancelReason = "Cancelada"; menu.Visible = false; });
            if (Wallet.Pdm.Repo != null && !Active) ui.AddConfirm("Recuperar retomada interrompida", "Chegada salva conclui estoque. Antes da chegada, encerra operacao e mantem contrato em atraso. Pare/saia dos veiculos.", () => { Recover(); OpenMenu(); });
            if(Back!=null)Add("Voltar","Empresa PDM.",Back);ui.Show();
        }
        static Vehicle Spawn(JobPoint point, string name, string plate)
        {
            var model = new Model(name);
            try
            {
                if (!model.IsValid || !model.IsInCdImage || !model.IsVehicle || !PdmStreaming.Ready(model)) return null;
                var bay = RigSpawn.FindVehicle(point, model, "PDM retomada"); if (bay == null) return null;
                var v = PdmAssets.Track(World.CreateVehicle(model, Pos(bay), bay.Heading)); if (!Exists(v)) return null;
                v.IsPersistent = true; v.IsStolen = false; v.NeedsToBeHotwired = false;
                float capacity = v.PetrolTankVolume; v.FuelLevel = capacity > 0 && !float.IsNaN(capacity) && !float.IsInfinity(capacity) ? Math.Min(10000, capacity) : 100;
                Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, v, plate); JobVehicles.Reserve(v); return v;
            }
            finally { PdmStreaming.Release(model); }
        }
        static PdmEntityRef Ref(Vehicle v, string model) => new PdmEntityRef { Handle = v.Handle, Model = model, Plate = Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, v).Trim() };
        static Vehicle Match(int handle, string model, string plate)
        { var v = handle > 0 ? Entity.FromHandle(handle) as Vehicle : null; return Exists(v) && v.Model.Hash == new Model(model).Hash && string.Equals(Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, v)?.Trim(), plate, StringComparison.OrdinalIgnoreCase) ? PdmAssets.Track(v) : null; }
        void Begin(string loanId)
        {
            if (Active || !Wallet.Pdm.Valid || !Wallet.Pdm.Owned || Wallet.Pdm.Active != null || Wallet.Pdm.Collection != null || Wallet.Pdm.SafeTransfer != null || Wallet.Pdm.Repo != null || Wallet.Pdm.Rental != null || Wallet.Pdm.ShowroomSale != null || Wallet.Pdm.PremisesService != null || !Foot || Game.IsMissionActive || Game.Player.Wanted.WantedLevel > 0 || Game.Player.Character.Position.DistanceTo(StartPosition) > 5)
            { Notify.Show("Inicie a pe no escritorio, sem procurado/missao e sem outra operacao PDM pendente."); return; }
            var loan = Wallet.Pdm.Loans.Find(l => l.Id == loanId); if (loan == null || !loan.Defaulted || loan.Settled || Wallet.Pdm.Stock.Count >= company.Capacity) { Notify.Show("Confira contrato em atraso e vaga livre no estoque."); return; }
            if (Exists(Match(loan.DeliveredHandle, loan.Asset.Model, loan.Asset.Plate))) { Notify.Show("O carro da entrega original ainda esta no mundo. Libere-o e aguarde a retirada antes de iniciar retomada."); return; }
            if (!gate.Acquire(Name)) { Notify.Show("Encerre a atividade de " + gate.Owner + "."); return; }
            bool ready = false;
            try
            {
                string id = Guid.NewGuid().ToString("N"); trailer = Spawn(rig.Trailer, rig.TrailerModel, "PRL" + id.Substring(0, 5)); if (!Exists(trailer)) return;
                double angle = rig.Trailer.Heading * Math.PI / 180;
                var anchor = new JobPoint { X = trailer.Position.X - (float)Math.Sin(angle) * 19, Y = trailer.Position.Y + (float)Math.Cos(angle) * 19, Z = trailer.Position.Z, Heading = rig.Trailer.Heading };
                truck = Spawn(anchor, rig.TruckModel, "PRT" + id.Substring(0, 5)); if (!Exists(truck)) return;
                var op = new PdmRepoOperation { Id = id, LoanId = loanId, Location = random.Next(options.Locations.Count), Truck = Ref(truck, rig.TruckModel), Trailer = Ref(trailer, rig.TrailerModel) };
                if (!Wallet.PdmRepoBegin(company, options, op, out _)) return;
                run = new PdmHaulRun(id, 1, options.UnloadSeconds); profile = Profile.Id; began = retryAt = Game.GameTime; removed = false; cancelReason = null;
                truckBlip = truck.AddBlip(); truckBlip.Name = "RPCore: Packer retomada"; truckBlip.Color = BlipColor.Blue;
                trailerBlip = trailer.AddBlip(); trailerBlip.Name = "RPCore: TRFlat retomada"; trailerBlip.Color = BlipColor.Blue;
                Route(trailer.Position, "RPCore: engate a carreta"); ready = true; menu.Visible = false;
                Notify.Show("Retomada iniciada. Entre no Packer e engate a TRFlat; E auxilia o engate alinhado/parado.");
            }
            finally { if (!ready) { JobVehicles.RetireRig(truck, trailer); truck = trailer = null; gate.Release(Name); Notify.Show("Conjunto indisponivel. Contrato e estoque preservados."); } }
        }
        void Route(Vector3 p, string text)
        { if (route != null && route.Exists()) route.Delete(); route = World.CreateBlip(p); route.Name = text; route.Color = BlipColor.Yellow; route.ShowRoute = true; }
        void Couple()
        {
            Status("entre no Packer / engate a TRFlat / E auxilia o engate");
            float delta = Math.Abs(truck.Heading - trailer.Heading); if (delta > 180) delta = 360 - delta;
            bool driver = Game.Player.Character.IsInVehicle(truck) && truck.Driver == Game.Player.Character;
            if (TankerRules.CanCouple(driver, Foreign || Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER, truck), truck.RearPosition.DistanceTo(trailer.FrontPosition), delta, truck.Speed, trailer.Speed))
            { Prompt.Help("~INPUT_CONTEXT~: engatar carreta de retomada"); if (Game.IsControlJustPressed(GTA.Control.Context)) Function.Call(Hash.ATTACH_VEHICLE_TO_TRAILER, truck, trailer, 6f); }
        }
        void SpawnCar()
        {
            var a = Wallet.Pdm.Repo;
            if (a.VehicleHandle > 0 || unchecked(Game.GameTime - retryAt) < 0) return;
            var point = options.Locations[a.Location]; if (Game.Player.Character.Position.DistanceTo(Pos(point)) > 150 || trailer.Position.DistanceTo(Pos(point)) > 150) return;
            retryAt = Game.GameTime + 5000; var loan = Loan; var v = Spawn(point, loan.Asset.Model, loan.Asset.Plate); if (!Exists(v)) { Notify.Show("Vaga do alvo ocupada. Afaste o conjunto; nova tentativa em 5s."); return; }
            bool saved = false;
            try { v.EngineHealth = loan.Asset.Engine; v.BodyHealth = loan.Asset.Body; v.DirtLevel = loan.Asset.Dirt; if (!Wallet.PdmRepoCar(run.Id, v.Handle, out _)) return; car = v; saved = true; carBlip = car.AddBlip(); carBlip.Name = "RPCore: alvo da retomada"; carBlip.Color = BlipColor.Blue; }
            finally { if (!saved) { v.Delete(); JobVehicles.Release(v); } }
        }
        bool UnloadReady => InputAllowed && Coupled && !Foreign && Exists(car) && car.Occupants.Length == 0 && car.IsAttachedTo(trailer) && truck.Speed < .5f && trailer.Speed < .5f && trailer.Position.DistanceTo(Pos(options.Return)) < 24 && Game.Player.Character.Position.DistanceTo(trailer.Position) < 30;
        public void Tick()
        {
            ui.Tick(PdmMenuRuntime.Allowed);
            if (!Active) return;
            try
            {
                if (run.Stage == PdmHaulStage.Complete) { EndSession(); return; }
                if (run.Stage == PdmHaulStage.Confirming) { Receive(); return; }
                if (cancelReason != null) { Cancel(); return; }
                if (Game.Player.Character.IsDead || Game.IsMissionActive || Game.Player.Wanted.WantedLevel > 0 || unchecked(Game.GameTime - began) > options.TimeLimitSeconds * 1000 || !Exists(truck) || !Exists(trailer) || truck.IsDead || trailer.IsDead || Wallet.Pdm.Repo.VehicleHandle > 0 && !Exists(car) || run.Loaded > 0 && !car.IsAttachedTo(trailer))
                { cancelReason = "Retomada interrompida"; Cancel(); return; }
                if (!InputAllowed) { run.Interrupt(); return; }
                if (!Coupled) { run.Interrupt(); Couple(); return; }
                if (run.Stage == PdmHaulStage.Coupling) { run.Coupled(true); Route(Pos(options.Locations[Wallet.Pdm.Repo.Location]), "RPCore: ultimo local do carro"); }
                if (run.Stage == PdmHaulStage.Loading)
                {
                    SpawnCar(); Status("busque o carro azul / aproxime-o da traseira da carreta / saia e E carrega");
                    if (Exists(car) && Foot && Empty(car) && truck.Speed < .5f && trailer.Speed < .5f && car.Position.DistanceTo(trailer.RearPosition) < 12 && Game.Player.Character.Position.DistanceTo(car.Position) < 5)
                    {
                        Prompt.Help("~INPUT_CONTEXT~: carregar alvo / guincho assistido"); var slot = rig.Slots[0];
                        if (Game.IsControlJustPressed(GTA.Control.Context) && run.Load(true, () => { car.AttachTo(trailer, new Vector3(slot.X, slot.Y, slot.Z), Vector3.Zero, false, false, false, false, EulerRotationOrder.XYZ, true, false); return car.IsAttachedTo(trailer); })) { car.IsCollisionEnabled = false; Route(Pos(options.Return), "RPCore: retorno de retomada PDM"); }
                    }
                    return;
                }
                if (run.Stage == PdmHaulStage.Returning)
                { Status("volte a PDM / pare o conjunto / E descarrega e encerra contrato em atraso"); if (UnloadReady) { Prompt.Help("~INPUT_CONTEXT~: receber carro retomado na PDM"); if (Game.IsControlJustPressed(GTA.Control.Context)) run.Unload(Game.GameTime, true); } return; }
                Status("recebendo carro retomado / mantenha conjunto parado"); if (unchecked(Game.GameTime - retryAt) < 0) { if (!UnloadReady) run.Interrupt(); return; }
                if (run.Arrive(Game.GameTime, UnloadReady, () => Remember() && Wallet.PdmRepoArrive(run.Id, out _))) Receive();
            }
            catch (Exception ex) { retryAt = Game.GameTime + 5000; Log.Error("pdm repossession checkpoint", ex); Notify.Show("Retomada pendente; contrato preservado. Nova tentativa em 5s."); }
        }
        bool Remember(bool missingIsDamage = true)
        {
            var a = Wallet.Pdm.Repo; if (a == null) return false; var l = Loan;
            int engine = Exists(car) ? Health(car.EngineHealth) : missingIsDamage && a.VehicleHandle > 0 && !removed ? 0 : l.Asset.Engine;
            int body = Exists(car) ? Health(car.BodyHealth) : missingIsDamage && a.VehicleHandle > 0 && !removed ? 0 : l.Asset.Body;
            return Wallet.PdmRepoHealth(a.Id, engine, body, out _);
        }
        bool DeleteCar()
        {
            if (Exists(car))
            {
                if (car.Occupants.Length > 0 || car.Speed > .5f) return false;
                car.Detach(); car.IsCollisionEnabled = true; car.Delete(); if (PdmAssets.Valid(car)) return false; JobVehicles.Release(car);
            }
            removed = true; return true;
        }
        void Receive()
        {
            if (unchecked(Game.GameTime - retryAt) < 0) return;
            if (!DeleteCar()) { Status("recebimento: pare e saia dos carros transportados"); return; }
            var loan = Loan; int profit = Math.Min(loan.Asset.Cost, loan.Paid);
            if (!run.Complete(() => Wallet.PdmRepoReceive(company, run.Id, out _))) { retryAt = Game.GameTime + 5000; Notify.Show("Recebimento pendente. Confira capacidade/configuracao de estoque."); return; }
            Bump(profit); EndSession(); Notify.Show("Carro retomado no estoque com danos. Saldo a receber encerrado; sem pagamento extra.");
        }
        void Cancel()
        {
            Status("cancelamento: pare e saia do conjunto e do carro alvo");
            if (!Empty(truck) || !Empty(trailer) || !Empty(car) || Foreign || unchecked(Game.GameTime - retryAt) < 0) return;
            if (!Remember() || !DeleteCar() || !Wallet.PdmRepoCancel(run.Id, out _)) { retryAt = Game.GameTime + 5000; return; }
            EndSession(); Notify.Show("Retomada encerrada. Contrato continua em atraso, sem bonus nem estoque duplicado.");
        }
        void Recover()
        {
            var a = Wallet.Pdm.Repo; if (a == null || !Wallet.Pdm.Valid) return;
            try
            {
                truck = Match(a.Truck.Handle, a.Truck.Model, a.Truck.Plate); trailer = Match(a.Trailer.Handle, a.Trailer.Model, a.Trailer.Plate); var loan = Loan; car = Match(a.VehicleHandle, loan.Asset.Model, loan.Asset.Plate);
                JobVehicles.Reserve(truck); JobVehicles.Reserve(trailer); JobVehicles.Reserve(car);
                if (!Empty(truck) || !Empty(trailer) || !Empty(car) || Foreign) { Notify.Show("Retomada interrompida: pare e saia dos veiculos. Use Recuperar no menu."); return; }
                if (!Remember(false) || !DeleteCar()) return;
                int profit = Math.Min(loan.Asset.Cost, loan.Paid);
                bool changed;
                if (!(a.Arrived ? Wallet.PdmRepoReceive(company, a.Id, out changed) : Wallet.PdmRepoCancel(a.Id, out changed))) return;
                if (a.Arrived && changed) Bump(profit); EndSession(); Notify.Show(a.Arrived ? "Retomada recebida recuperada; carro no estoque." : "Retomada interrompida; contrato em atraso preservado.");
            }
            catch (Exception ex) { Log.Error("pdm repossession recovery", ex); }
        }
        void Bump(int profit) { try { achievements.Bump("pdm_repossessions"); if (profit > 0) achievements.Bump("cash_earned", profit); achievements.Save(); } catch (Exception ex) { Log.Error("pdm repossession achievement after checkpoint", ex); } }
        void EndSession(bool retireRig = true)
        {
            if (retireRig) JobVehicles.RetireRig(truck, trailer); truck = trailer = car = null;
            try { foreach (var b in new[] { route, carBlip, truckBlip, trailerBlip }) if (b != null && b.Exists()) b.Delete(); }
            finally { route = carBlip = truckBlip = trailerBlip = null; run = null; cancelReason = null; gate.Release(Name); menu.Visible = false; }
        }
        void ReleaseSession()
        { bool safe = Empty(truck) && Empty(trailer) && Empty(car) && !Foreign; if (safe) DeleteCar(); EndSession(safe); }
        void Status(string text) { if (!Visuals.TryJobHeader("pdm", "PDM / retomada", Color.White)) Visuals.Text("PDM / retomada", Visuals.Width / 2 - 200, 24, .34f, Color.White); float x = Visuals.Width / 2 - 390; Visuals.Rect(x, 900, 780, 80, Color.FromArgb(210, 8, 12, 18)); Visuals.Text(text, x + 12, 910, .29f, Color.White); Visuals.Text("End (segurar): encerrar / contrato permanece em atraso ate retomar", x + 12, 945, .23f, Color.White); }
        public void PollCancel() { if (!Active || !InputAllowed || !Game.IsKeyPressed(Keys.End)) { cancelSince = -1; return; } if (cancelSince < 0) cancelSince = Game.GameTime; if (unchecked(Game.GameTime - cancelSince) >= 1000) cancelReason = "Cancelada pelo jogador"; }
        public void Save() { if (Active && profile == Profile.Id && Wallet.Pdm.Repo != null) Remember(); }
        public void Reload() { ui.Reset(); ReleaseSession(); Recover(); }
        public void OnKey(KeyEventArgs e) { }
        public void Dispose() { ui.Reset(); try { Save(); } catch (Exception ex) { Log.Error("pdm repossession dispose", ex); } finally { ReleaseSession(); } }
    }
}
