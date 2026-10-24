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
    public sealed class PdmRentalSystem : ISystem, IWorkContract
    {
        public string Name => "pdmrental";
        public string Label => "PDM / pedidos da locadora";
        public bool Active => run != null;
        public Vector3 StartPosition { get; }
        readonly ObjectPool pool; readonly ShiftGate gate; readonly Func<bool> clearDisplay; readonly Action back;
        readonly AchievementSystem achievements; readonly PdmRentalOptions options; readonly PdmLogisticsOptions rig; readonly NativeMenu menu;
        readonly Dictionary<string, Vehicle> cars = new Dictionary<string, Vehicle>(); readonly HashSet<string> loaded = new HashSet<string>(), removed = new HashSet<string>(); readonly List<Blip> blips = new List<Blip>();
        bool accepted; Vehicle truck, trailer; Ped worker; Blip route; PdmRentalRun run; string profile, cancelReason; int began, retryAt, nextWalk, cancelSince = -1;
        static Vector3 Pos(JobPoint p) => new Vector3(p.X, p.Y, p.Z);
        static bool Exists(Vehicle v) => v != null && v.Exists();
        static bool Empty(Vehicle v) => !Exists(v) || v.Occupants.Length == 0 && v.Speed < .5f;
        static int Health(float n) => float.IsNaN(n) || float.IsInfinity(n) ? 0 : Math.Max(0, Math.Min(1000, (int)n));
        bool Foot => Game.Player.Character.IsHuman && !Game.Player.Character.IsDead && !Game.Player.Character.IsInVehicle() && !Game.Player.Character.IsSwimming && !Game.Player.Character.IsRagdoll && !Game.Player.Character.IsInCombat;
        bool InputAllowed => !Game.IsPaused && !InterfaceOptions.ModalOpen && !pool.AreAnyVisible;
        bool Coupled => JobVehicles.Towing(truck, trailer);
        bool Foreign => Exists(truck) && Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER, truck) && !Coupled || JobVehicles.TrailerHasOtherTractor(trailer, truck);
        public PdmRentalSystem(ObjectPool pool, ShiftGate gate, PdmOptions company, Func<bool> clearDisplay, AchievementSystem achievements, Action back)
        {
            this.pool = pool; this.gate = gate; this.clearDisplay = clearDisplay; this.achievements = achievements; this.back = back; StartPosition = Pos(company.Office);
            var defaults = PdmRentalDefaults.Load(); options = Store.LoadDef("Jobs/PdmRental.json", () => defaults);
            if (options == null || !options.Valid) { Log.Write("Invalid PdmRental.json; embedded defaults used, original retained."); options = defaults; }
            var rigDefaults = PdmLogisticsDefaults.Load(); rig = Store.LoadDef("Jobs/PdmLogistics.json", () => rigDefaults);
            if (rig == null || !rig.Valid) { Log.Write("Invalid PdmLogistics.json for rental order; embedded defaults used, original retained."); rig = rigDefaults; }
            menu = new NativeMenu("PDM / locadora", "DOIS CARROS / ENTREGA / RETORNO"); pool.Add(menu); Visuals.Register(menu, "pdm"); Recover();
        }
        void Add(string title, string text, Action action)
        { var item = new NativeItem(title, text); item.Activated += (s, e) => { try { action(); } catch (Exception ex) { Log.Error("pdm rental action", ex); Notify.Show("Pedido da locadora pendente; dados preservados."); } }; menu.Add(item); }
        public void OpenMenu()
        {
            pool.HideAll(); menu.Clear(); if (!Wallet.Pdm.Valid) { Notify.Show("Dados PDM invalidos. Original preservado."); return; }
            Add("GPS: escritorio PDM", "Inicie a pe com dois carros prontos do mesmo modelo no estoque.", () => Waypoint(StartPosition));
            Add("GPS: locadora / patio externo", "Destino da referencia fora das pistas do aeroporto. Nao entre em area restrita.", () => Waypoint(Pos(options.Delivery)));
            if (Wallet.Pdm.Rental == null && !Active)
            {
                int offers = 0;
                foreach (string model in options.Models)
                {
                    var stock = Wallet.Pdm.Stock.Where(s => !s.AwaitingCollection && s.Engine >= 300 && s.Body >= 300 && string.Equals(s.Model, model, StringComparison.OrdinalIgnoreCase)).Take(2).ToList(); if (stock.Count != 2) continue;
                    offers++; var ids = stock.Select(s => s.Id).ToList(); int quote = stock.Sum(s => PdmRentalTransactions.Quote(s, options.PricePercent));
                    Add("Fornecer 2x " + stock[0].Name + " / ate $" + quote, "Estoque proprio ja pago; desconto contratual " + options.PricePercent + "% da cotacao base apos danos. Carregue, entregue a locadora e devolva o conjunto para receber no caixa. Sem bonus de divulgacao.", () => Begin(ids));
                }
                if (offers == 0) Add("Sem dois carros prontos do mesmo modelo", "Compre/colete/repare duas unidades de um modelo aceito: " + string.Join(", ", options.Models) + ". Nada e criado gratuitamente.", () => { });
            }
            if (Wallet.Pdm.Rental != null)
            {
                var a = Wallet.Pdm.Rental;
                Add(a.Delivered ? "Entrega aceita / retorno e recebimento pendentes" : "Pedido reservado / entrega pendente", "Valor previsto/fixado: $" + a.Amount + ". Carros reservados nao podem ser vendidos em outro contrato.", () => { });
                if (Active) Add(a.Delivered ? "Suspender retorno" : "Encerrar pedido", a.Delivered ? "Mantem obrigacao de recebimento e conjunto para continuar por Recuperar. Pare e saia dos veiculos." : "Antes da entrega, libera as unidades no estoque com danos. Pare e saia dos veiculos.", () => { cancelReason = "Solicitada pelo jogador"; menu.Visible = false; });
                Add("Recuperar / continuar pedido", "Antes da entrega: devolve reserva ao estoque. Entregue: retoma retorno com conjunto reconhecido; se indisponivel apos interrupcao, recupera recebimento sem duplicar.", () => { if (Active && !a.Delivered) { cancelReason = "Recuperacao solicitada"; menu.Visible = false; } else { Recover(); menu.Visible = false; } });
            }
            Add("Voltar / resolver caixa", "Gestao PDM. Se recebimento estiver bloqueado pelo limite do caixa, pode sacar saldo disponivel no escritorio apos entrega aceita.", back); menu.Visible = true;
        }
        void Waypoint(Vector3 p) { Function.Call(Hash.SET_NEW_WAYPOINT, p.X, p.Y); menu.Visible = false; }
        void Route(Vector3 p, string label) { if (route != null && route.Exists()) route.Delete(); route = World.CreateBlip(p); route.Name = label; route.Color = BlipColor.Yellow; route.ShowRoute = true; }
        void Blip(Vehicle v, string label) { var b = v.AddBlip(); b.Name = label; b.Color = BlipColor.Blue; blips.Add(b); }
        static Vehicle Spawn(JobPoint point, string name, string plate)
        {
            var model = new Model(name);
            try
            {
                if (!model.IsValid || !model.IsInCdImage || !model.IsVehicle) return null;
                var bay = RigSpawn.FindVehicle(point, model, "PDM locadora"); if (bay == null) return null;
                var v = World.CreateVehicle(model, Pos(bay), bay.Heading); if (!Exists(v)) return null;
                v.IsPersistent = true; v.IsStolen = false; v.NeedsToBeHotwired = false;
                float capacity = v.PetrolTankVolume; v.FuelLevel = capacity > 0 && !float.IsNaN(capacity) && !float.IsInfinity(capacity) ? Math.Min(10000, capacity) : 100;
                Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, v, plate); JobVehicles.Reserve(v); return v;
            }
            finally { model.MarkAsNoLongerNeeded(); }
        }
        static PdmEntityRef Ref(Vehicle v, string model) => new PdmEntityRef { Handle = v.Handle, Model = model, Plate = Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, v).Trim() };
        static Vehicle Match(int handle, string model, string plate)
        { var v = handle > 0 ? Entity.FromHandle(handle) as Vehicle : null; return Exists(v) && v.Model.Hash == new Model(model).Hash && string.Equals(Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, v)?.Trim(), plate, StringComparison.OrdinalIgnoreCase) ? v : null; }
        void Begin(List<string> ids)
        {
            var d = Wallet.Pdm;
            if (Active || !d.Valid || !d.Owned || d.Active != null || d.Collection != null || d.SafeTransfer != null || d.Repo != null || d.Rental != null || !Foot || Game.IsMissionActive || Game.Player.Wanted.WantedLevel > 0 || Game.Player.Character.Position.DistanceTo(StartPosition) > 5)
            { Notify.Show("Inicie a pe na PDM, sem procurado/missao e sem outra operacao pendente."); return; }
            if (clearDisplay != null && !clearDisplay()) return;
            if (!gate.Acquire(Name)) { Notify.Show("Encerre a atividade de " + gate.Owner + "."); return; }
            bool ready = false;
            try
            {
                string id = Guid.NewGuid().ToString("N"); trailer = Spawn(rig.Trailer, rig.TrailerModel, "RLL" + id.Substring(0, 5)); if (!Exists(trailer)) return;
                double angle = rig.Trailer.Heading * Math.PI / 180;
                var point = new JobPoint { X = trailer.Position.X - (float)Math.Sin(angle) * 19, Y = trailer.Position.Y + (float)Math.Cos(angle) * 19, Z = trailer.Position.Z, Heading = rig.Trailer.Heading };
                truck = Spawn(point, rig.TruckModel, "RLT" + id.Substring(0, 5)); if (!Exists(truck) || !Wallet.PdmRentalBegin(options, id, ids, Ref(truck, rig.TruckModel), Ref(trailer, rig.TrailerModel), out _)) return;
                run = new PdmRentalRun(id, options.UnloadSeconds, options.ReturnSeconds); profile = Profile.Id; began = retryAt = Game.GameTime; cancelReason = null; accepted = false;
                Blip(truck, "RPCore: Packer locadora"); Blip(trailer, "RPCore: TRFlat locadora"); Route(trailer.Position, "RPCore: engate a carreta"); menu.Visible = false; ready = true;
                Notify.Show("Pedido da locadora reservado. Engate Packer/TRFlat; E auxilia. Depois carregue os dois carros azuis na PDM.");
            }
            finally { if (!ready) { JobVehicles.RetireRig(truck, trailer); truck = trailer = null; gate.Release(Name); Notify.Show("Pedido nao iniciado; estoque e caixa preservados."); } }
        }
        void Couple()
        {
            Status("Packer + TRFlat / alinhe e pare / E auxilia o engate"); float delta = Math.Abs(truck.Heading - trailer.Heading); if (delta > 180) delta = 360 - delta;
            bool driver = Game.Player.Character.IsInVehicle(truck) && truck.Driver == Game.Player.Character;
            if (TankerRules.CanCouple(driver, Foreign || Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER, truck), truck.RearPosition.DistanceTo(trailer.FrontPosition), delta, truck.Speed, trailer.Speed))
            { Prompt.Help("~INPUT_CONTEXT~: engatar carreta da locadora"); if (Game.IsControlJustPressed(GTA.Control.Context)) Function.Call(Hash.ATTACH_VEHICLE_TO_TRAILER, truck, trailer, 6f); }
        }
        void SpawnCars()
        {
            if (unchecked(Game.GameTime - retryAt) < 0 || Game.Player.Character.Position.DistanceTo(StartPosition) > 150 || trailer.Position.DistanceTo(StartPosition) > 150) return;
            retryAt = Game.GameTime + 5000; var a = Wallet.Pdm.Rental;
            for (int i = 0; i < a.Cars.Count; i++)
            {
                var item = a.Cars[i]; if (item.Handle > 0 || cars.ContainsKey(item.StockId)) continue; var stock = Wallet.Pdm.Stock.Find(s => s.Id == item.StockId);
                var v = Spawn(options.Pickups[i], stock.Model, stock.Plate); if (!Exists(v)) { Notify.Show("Vaga de retirada ocupada. Afaste o conjunto; nova tentativa em 5s."); return; }
                bool saved = false;
                try { v.EngineHealth = stock.Engine; v.BodyHealth = stock.Body; if (!Wallet.PdmRentalCar(run.Id, stock.Id, v.Handle, out _)) return; cars.Add(stock.Id, v); saved = true; Blip(v, "RPCore: carro para locadora"); }
                finally { if (!saved) { v.Delete(); JobVehicles.Release(v); } }
            }
        }
        void Load()
        {
            SpawnCars(); Status("carros azuis: aproxime da traseira da carreta / pare, saia e E carrega");
            foreach (var pair in cars)
            {
                var v = pair.Value; if (loaded.Contains(pair.Key)) continue;
                if (!Foot || !Empty(v) || truck.Speed >= .5f || trailer.Speed >= .5f || v.Position.DistanceTo(trailer.RearPosition) >= 12 || Game.Player.Character.Position.DistanceTo(v.Position) >= 5) continue;
                Prompt.Help("~INPUT_CONTEXT~: carregar carro da locadora"); var slot = rig.Slots[run.Loaded];
                if (Game.IsControlJustPressed(GTA.Control.Context) && run.Load(true, () => { v.AttachTo(trailer, new Vector3(slot.X, slot.Y, slot.Z), Vector3.Zero, false, false, false, false, EulerRotationOrder.XYZ, true, false); return v.IsAttachedTo(trailer); }))
                { v.IsCollisionEnabled = false; loaded.Add(pair.Key); if (run.Stage == PdmRentalStage.Driving) Route(Pos(options.Delivery), "RPCore: locadora / patio externo"); }
                return;
            }
        }
        bool DeliveryReady => InputAllowed && Coupled && !Foreign && truck.Speed < .5f && trailer.Speed < .5f && trailer.Position.DistanceTo(Pos(options.Delivery)) < 24 && truck.Position.DistanceTo(Pos(options.Delivery)) < 24 && Game.Player.Character.Position.DistanceTo(truck.Position) < 15 && cars.Count == 2 && cars.Values.All(v => Exists(v) && !v.IsDead && v.Occupants.Length == 0 && v.EngineHealth >= 100 && v.IsAttachedTo(trailer));
        bool WorkerReady => worker != null && worker.Exists() && !worker.IsDead && worker.Position.DistanceTo(truck.Position) < 9;
        bool ReturnReady => InputAllowed && Coupled && !Foreign && truck.Speed < .5f && trailer.Speed < .5f && trailer.Position.DistanceTo(Pos(options.Return)) < 24 && Game.Player.Character.Position.DistanceTo(truck.Position) < 15;
        void SpawnWorker()
        {
            if (worker != null || Game.Player.Character.Position.DistanceTo(Pos(options.Delivery)) > 150 || unchecked(Game.GameTime - retryAt) < 0) return;
            retryAt = Game.GameTime + 5000; var model = new Model(options.WorkerModel);
            try
            {
                if (!model.IsValid || !model.IsPed || !model.IsInCdImage || !model.Request(2500)) return;
                var point = JobActors.Sidewalk(Pos(options.Worker)); if (point == Vector3.Zero || !JobActors.Free(point, 2)) return;
                worker = World.CreatePed(model, point, options.Worker.Heading); if (worker == null || !worker.Exists()) { worker = null; return; }
                worker.IsPersistent = true; worker.BlockPermanentEvents = true; var b = worker.AddBlip(); b.Name = "RPCore: funcionario da locadora"; b.Color = BlipColor.Yellow; blips.Add(b);
            }
            finally { model.MarkAsNoLongerNeeded(); }
        }
        void CallWorker()
        {
            var point = JobActors.Sidewalk(truck.LeftPosition); if (point == Vector3.Zero) point = truck.LeftPosition;
            JobActors.Walk(worker, point); nextWalk = Game.GameTime + 5000;
        }
        public void Tick()
        {
            if (!Active) return;
            try
            {
                if (run.Stage == PdmRentalStage.Complete) { EndSession(true); return; }
                if (run.Stage == PdmRentalStage.Confirming) { BeginReturn(); return; }
                if (run.Stage == PdmRentalStage.Paying) { Pay(false); return; }
                bool delivered = Wallet.Pdm.Rental.Delivered;
                if (cancelReason != null) { if (delivered) Suspend(); else Cancel(); return; }
                if (Game.Player.Character.IsDead || Game.IsMissionActive || Game.Player.Wanted.WantedLevel > 0 || unchecked(Game.GameTime - began) > options.TimeLimitSeconds * 1000 || !Exists(truck) || !Exists(trailer) || truck.IsDead || trailer.IsDead || !delivered && (cars.Values.Any(v => !Exists(v) || v.IsDead || v.EngineHealth < 100) || loaded.Any(id => !cars[id].IsAttachedTo(trailer)) || worker != null && (!worker.Exists() || worker.IsDead)))
                { cancelReason = "Pedido interrompido"; if (delivered) Suspend(); else Cancel(); return; }
                if (!InputAllowed) { run.Deliver(Game.GameTime, false, () => false); run.Returned(Game.GameTime, false, () => false); return; }
                if (!Coupled) { run.Deliver(Game.GameTime, false, () => false); run.Returned(Game.GameTime, false, () => false); Couple(); return; }
                if (run.Stage == PdmRentalStage.Coupling) run.Couple(true);
                if (run.Stage == PdmRentalStage.Loading) { Load(); return; }
                if (run.Stage == PdmRentalStage.Driving)
                {
                    SpawnWorker(); Status("leve os dois carros ao patio externo / pare e buzine para chamar o funcionario");
                    if (DeliveryReady && worker != null && worker.Exists() && Game.Player.Character.IsInVehicle(truck) && truck.Driver == Game.Player.Character && Game.IsControlJustPressed(GTA.Control.VehicleHorn)) { run.Call(true); CallWorker(); }
                    return;
                }
                if (run.Stage == PdmRentalStage.Calling)
                {
                    Status("funcionario vindo conferir / mantenha o conjunto parado");
                    if (unchecked(Game.GameTime - nextWalk) >= 0) CallWorker(); if (DeliveryReady && WorkerReady) run.Unload(Game.GameTime, true); return;
                }
                if (run.Stage == PdmRentalStage.Unloading)
                {
                    Status("conferindo dois carros / aguarde recebimento da locadora"); if (unchecked(Game.GameTime - retryAt) < 0) { if (!DeliveryReady || !WorkerReady) run.Deliver(Game.GameTime, false, () => false); return; }
                    if (run.Deliver(Game.GameTime, DeliveryReady && WorkerReady, () => Remember() && Wallet.PdmRentalDeliver(run.Id, out _))) BeginReturn(); return;
                }
                if (run.Stage == PdmRentalStage.Returning)
                {
                    Status("pedido entregue / volte a PDM com caminhao e carreta / E encerra");
                    if (ReturnReady) { Prompt.Help("~INPUT_CONTEXT~: devolver conjunto e receber pedido"); if (Game.IsControlJustPressed(GTA.Control.Context)) run.Return(Game.GameTime, true); } return;
                }
                Status("devolvendo conjunto / aguarde confirmacao");
                if (unchecked(Game.GameTime - retryAt) < 0) { if (!ReturnReady) run.Returned(Game.GameTime, false, () => false); return; }
                if (run.Returned(Game.GameTime, ReturnReady, () => Wallet.PdmRentalReturn(run.Id, out _))) Pay(false);
            }
            catch (Exception ex) { retryAt = Game.GameTime + 5000; Log.Error("pdm rental checkpoint", ex); Notify.Show("Pedido da locadora pendente. Dados preservados; nova tentativa em 5s."); }
        }
        bool Remember(bool missingIsDamage = true)
        {
            var a = Wallet.Pdm.Rental; if (a == null || a.Delivered) return true;
            var health = new Dictionary<string, int[]>();
            foreach (var c in a.Cars)
            {
                Vehicle v; cars.TryGetValue(c.StockId, out v); var stock = Wallet.Pdm.Stock.Find(s => s.Id == c.StockId);
                health[c.StockId] = Exists(v) ? new[] { Health(v.EngineHealth), Health(v.BodyHealth) } : missingIsDamage && c.Handle > 0 && !removed.Contains(c.StockId) ? new[] { 0, 0 } : new[] { stock.Engine, stock.Body };
            }
            return Wallet.PdmRentalHealth(a.Id, health, out _);
        }
        bool DeleteCars()
        {
            if (cars.Values.Any(v => Exists(v) && (v.Occupants.Length > 0 || v.Speed > .5f && !(Exists(trailer) && trailer.Speed < .5f && v.IsAttachedTo(trailer))))) return false;
            foreach (var c in cars) if (Exists(c.Value)) { c.Value.Detach(); c.Value.IsCollisionEnabled = true; c.Value.Delete(); if (c.Value.Exists()) return false; JobVehicles.Release(c.Value); removed.Add(c.Key); }
            return true;
        }
        void BeginReturn()
        {
            accepted = true;
            if (!DeleteCars()) { Status("recebimento: pare e saia dos carros transportados"); return; }
            if (!run.BeginReturn(true)) return;
            if (worker != null && worker.Exists()) JobActors.Walk(worker, Pos(options.Worker));
            Route(Pos(options.Return), "RPCore: devolva o conjunto na PDM"); Notify.Show("Dois carros recebidos pela locadora. Volte a PDM para devolver o conjunto e receber no caixa.");
        }
        void Pay(bool recovery)
        {
            if (unchecked(Game.GameTime - retryAt) < 0) return;
            var a = Wallet.Pdm.Rental; int amount = a.Amount, cost = a.Cars.Sum(c => Wallet.Pdm.Stock.Find(s => s.Id == c.StockId).Cost);
            bool changed = false;
            bool paid = recovery ? Wallet.PdmRentalSettle(a.Id, true, out changed) : run.Pay(() => Wallet.PdmRentalSettle(a.Id, false, out changed));
            if (!paid) { retryAt = Game.GameTime + 5000; Notify.Show("Recebimento pendente: confira limite do caixa. Pode sacar saldo disponivel no escritorio e Recuperar."); return; }
            if (changed) try { achievements.Bump("pdm_sales", 2); achievements.Bump("cash_earned", Math.Max(0, amount - cost)); achievements.Save(); } catch (Exception ex) { Log.Error("pdm rental achievement after checkpoint", ex); }
            EndSession(true); Notify.Show("Locadora: +$" + amount + " no caixa da empresa. Dois carros baixados do estoque uma unica vez.");
        }
        void Cancel()
        {
            Status("encerrando pedido: pare e saia dos carros e do conjunto");
            if (!Empty(truck) || !Empty(trailer) || cars.Values.Any(v => !Empty(v)) || Foreign || unchecked(Game.GameTime - retryAt) < 0) return;
            if (!Remember() || !DeleteCars() || !Wallet.PdmRentalCancel(run.Id, out _)) { retryAt = Game.GameTime + 5000; return; }
            EndSession(true); Notify.Show("Pedido encerrado antes da entrega. Estoque pago liberado com danos, sem venda/pagamento.");
        }
        void Suspend()
        {
            Status("retorno pendente: pare e saia do conjunto / depois use Recuperar");
            if (!Empty(truck) || !Empty(trailer) || Foreign || !DeleteCars()) return;
            EndSession(false); Notify.Show("Pedido ja entregue preservado. Use Recuperar para continuar retorno ou resolver recebimento.");
        }
        void Recover()
        {
            var a = Wallet.Pdm.Rental; if (a == null || !Wallet.Pdm.Valid) return;
            if (Active) { if (a.Returned) Pay(false); else { Route(Pos(options.Return), "RPCore: devolva conjunto na PDM"); Notify.Show("Continue o retorno com o conjunto do pedido."); } return; }
            try
            {
                accepted = a.Delivered;
                truck = Match(a.Truck.Handle, a.Truck.Model, a.Truck.Plate); trailer = Match(a.Trailer.Handle, a.Trailer.Model, a.Trailer.Plate); JobVehicles.Reserve(truck); JobVehicles.Reserve(trailer);
                foreach (var c in a.Cars) { var s = Wallet.Pdm.Stock.Find(x => x.Id == c.StockId); var v = Match(c.Handle, s.Model, s.Plate); if (Exists(v)) { cars[c.StockId] = v; JobVehicles.Reserve(v); } }
                if (!Empty(truck) || !Empty(trailer) || cars.Values.Any(v => !Empty(v)) || Foreign) { Notify.Show("Pedido interrompido: pare e saia dos veiculos; use Recuperar."); return; }
                if (!Remember(false) || !DeleteCars()) return;
                if (!a.Delivered) { if (Wallet.PdmRentalCancel(a.Id, out _)) { EndSession(true); Notify.Show("Pedido interrompido antes da entrega; estoque preservado."); } return; }
                if (!a.Returned && Exists(truck) && Exists(trailer) && !truck.IsDead && !trailer.IsDead)
                {
                    if (!gate.Acquire(Name)) { Notify.Show("Encerre a atividade de " + gate.Owner + " antes de continuar o retorno."); return; }
                    run = PdmRentalRun.Resume(a.Id, options.UnloadSeconds, options.ReturnSeconds, false); profile = Profile.Id; began = retryAt = Game.GameTime; cancelReason = null;
                    Blip(truck, "RPCore: Packer locadora"); Blip(trailer, "RPCore: TRFlat locadora"); Route(Pos(options.Return), "RPCore: devolva conjunto na PDM"); Notify.Show("Entrega aceita recuperada. Devolva o conjunto para receber."); return;
                }
                retryAt = Game.GameTime; Pay(true);
            }
            catch (Exception ex) { Log.Error("pdm rental recovery", ex); Notify.Show("Pedido/recebimento pendente preservado. Use Recuperar no menu."); }
        }
        void EndSession(bool retireRig)
        {
            try { if (retireRig) JobVehicles.RetireRig(truck, trailer); if (worker != null && worker.Exists()) worker.Delete(); foreach (var b in blips) if (b != null && b.Exists()) b.Delete(); if (route != null && route.Exists()) route.Delete(); }
            finally { truck = trailer = null; worker = null; route = null; cars.Clear(); loaded.Clear(); removed.Clear(); blips.Clear(); run = null; cancelReason = null; accepted = false; gate.Release(Name); menu.Visible = false; }
        }
        void ReleaseSession()
        { bool safe = Empty(truck) && Empty(trailer) && !Foreign && cars.Values.All(Empty); if (safe) DeleteCars(); EndSession(safe && !accepted); }
        void Status(string text) { if (!Visuals.TryJobHeader("pdm", "PDM / locadora", Color.White)) Visuals.Text("PDM / locadora", Visuals.Width / 2 - 200, 24, .34f, Color.White); float x = Visuals.Width / 2 - 390; Visuals.Rect(x, 900, 780, 80, Color.FromArgb(210, 8, 12, 18)); Visuals.Text(text, x + 12, 910, .29f, Color.White); Visuals.Text("End: encerrar antes da entrega / depois: suspender retorno sem reabrir estoque", x + 12, 945, .23f, Color.White); }
        public void PollCancel() { if (!Active || !InputAllowed || !Game.IsKeyPressed(Keys.End)) { cancelSince = -1; return; } if (cancelSince < 0) cancelSince = Game.GameTime; if (unchecked(Game.GameTime - cancelSince) >= 1000) cancelReason = "Solicitada pelo jogador"; }
        public void Save() { if (Active && profile == Profile.Id && Wallet.Pdm.Rental != null && !Wallet.Pdm.Rental.Delivered) Remember(); }
        public void Reload() { ReleaseSession(); Recover(); }
        public void OnKey(KeyEventArgs e) { }
        public void Dispose() { try { Save(); } catch (Exception ex) { Log.Error("pdm rental dispose", ex); } finally { ReleaseSession(); } }
    }
}
