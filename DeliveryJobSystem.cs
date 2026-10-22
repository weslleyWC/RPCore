using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using LemonUI;
using LemonUI.Menus;

namespace RPCore
{
    public abstract class DeliveryJobSystem : ISystem
    {
        public string Name => pizza ? "pizza" : "postop";
        public string Label => pizza ? "Pizza" : "Post OP";
        public bool Active => run != null;
        protected readonly ObjectPool pool;
        readonly bool pizza;
        readonly ShiftGate gate;
        readonly AchievementSystem achievements;
        readonly NativeMenu menu;
        readonly DeliveryCargo cargo = new DeliveryCargo();
        readonly Random random = new Random();
        readonly DeliveryOptions options;
        readonly Blip startBlip;
        Vehicle vehicle;
        Blip targetBlip, vehicleBlip;
        DeliveryRun run;
        List<JobPoint> route;
        PizzaStats legacy;
        Vector3 destination;
        double distance;
        int deadline, handoverUntil, nextPaymentAttempt;
        bool supplied, quick, cargoDoorsOpen;
        int cancelHeldAt = -1;
        public static readonly Vector3 PizzaLocation = new Vector3(536.945f, 100.5983f, 96.51432f);
        public Vector3 StartPosition => pizza ? PizzaLocation : Position(options.Start);
        Vector3 Depot => pizza ? PizzaLocation : Position(options.Depot);
        static Vector3 Position(JobPoint point) => new Vector3(point.X, point.Y, point.Z);
        static JobPoint Point(Vector3 p) => new JobPoint { Name = "position", X = p.X, Y = p.Y, Z = p.Z };

        protected DeliveryJobSystem(ObjectPool pool, AchievementSystem achievements, ShiftGate gate, bool pizza)
        {
            this.pool = pool; this.achievements = achievements; this.gate = gate; this.pizza = pizza;
            var defaults = JobDefaults.PostOp();
            options = Store.LoadDef("Jobs/PostOp.json", () => defaults);
            if (options == null || !options.Valid) { Log.Write("Jobs/PostOp.json invalid; using embedded defaults without overwriting user file."); options = defaults; }
            menu = new NativeMenu(Label, "TRABALHO DE ENTREGAS"); pool.Add(menu); Visuals.Register(menu, Name);
            startBlip = MapBlips.Create(StartPosition, "RPCore Job: " + Label, pizza ? BlipSprite.LuckyWheel : BlipSprite.Package, BlipColor.Blue, .8f);
            LoadLegacy();
        }
        void LoadLegacy()
        {
            if (!pizza) return;
            legacy = Store.Load("PizzaStats.json", () => new PizzaStats()); Wallet.SeedPizzaStats(legacy); MirrorLegacy();
        }
        void MirrorLegacy()
        {
            if (!pizza) return;
            var stats = Wallet.JobProgress(Name); legacy.Deliveries = Math.Max(legacy.Deliveries, stats.Deliveries); legacy.Earned = (int)Math.Min(int.MaxValue, Math.Max(legacy.Earned, stats.Earned));
        }
        void Add(string title, string description, Action action)
        {
            var row = new NativeItem(title, description);
            row.Activated += (s, e) => { try { action(); } catch (Exception ex) { Log.Error(Name + " menu", ex); Notify.Show("Nao foi possivel executar. Veja RPCore.log."); } };
            menu.Add(row);
        }
        public void OpenMenu()
        {
            pool.HideAll(); menu.Clear();
            var progress = Wallet.JobProgress(Name);
            menu.Name = Label + " / " + progress.Deliveries + " entregas / $" + progress.Earned;
            Add("GPS: " + Label, "Local de contratacao. Pressione E no marcador.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, StartPosition.X, StartPosition.Y); menu.Visible = false; });
            if (!Active)
            {
                Add("Iniciar contrato", pizza ? "Retire a scooter, leve a pizza a pe e retorne a pizzaria. Pagamento em dinheiro." : "Retire a van, entregue as caixas e retorne a base. Pagamento no banco por entrega.", () => Begin(false));
                if (pizza) Add("Turno rapido (" + Cfg.JobKey + ")", "Modo anterior: inicia de qualquer lugar e entrega ao parar perto do destino.", () => Begin(true));
            }
            else
            {
                Add("GPS: objetivo atual", "Entregas: " + run.Delivered + "/" + run.Total + ". Segure End por um segundo para cancelar fora do menu.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, destination.X, destination.Y); menu.Visible = false; });
                Add("Cancelar turno", "Mantem entregas ja pagas. Nao recebe bonus de retorno. Veiculo pessoal e seus itens sao preservados.", () => End("Turno cancelado."));
            }
            menu.Visible = true;
        }
        bool ReadyToStart(bool rapid)
        {
            var p = Game.Player.Character;
            if (Active) { Notify.Show("Voce ja esta neste turno."); return false; }
            if (p.IsDead || Game.IsMissionActive || p.IsRagdoll || p.IsInCombat || Game.IsPaused) return false;
            if (gate.Owner != null) { Notify.Show("Encerre o turno de " + gate.Owner + " antes de iniciar outro."); return false; }
            if (!rapid && (p.IsInVehicle() || p.Position.DistanceTo(StartPosition) > 3f || Game.Player.Wanted.WantedLevel > 0)) { Notify.Show("Va ao marcador a pe, sem procurado, para contratar."); return false; }
            if (p.IsInVehicle() && JobVehicles.Contains(p.CurrentVehicle)) { Notify.Show("Desca para devolver o veiculo do turno anterior."); return false; }
            if (p.IsInVehicle() && p.CurrentVehicle.Driver != p) { Notify.Show("O turno rapido exige que voce seja o motorista."); return false; }
            return true;
        }
        void Begin(bool rapid)
        {
            if (!ReadyToStart(rapid) || !gate.Acquire(Name)) return;
            bool started = false;
            try
            {
                quick = rapid; supplied = false;
                int count = pizza ? Cfg.PizzaDrops : options.Drops;
                int available = options.Addresses.GroupBy(p => new { p.X, p.Y, p.Z }).Count();
                if (!rapid) count = Math.Min(count, available);
                route = DeliveryRoute.Select(options.Addresses, Point(StartPosition), Math.Min(count, available), random);
                var p = Game.Player.Character;
                if (rapid && p.IsInVehicle()) vehicle = p.CurrentVehicle;
                else
                {
                    var spawn = FindSpawn(rapid ? p.Position : pizza ? StartPosition : Depot);
                    if (spawn == Vector3.Zero) { Notify.Show("Saida ocupada. Libere a area do veiculo e tente novamente."); return; }
                    var model = new Model(pizza ? VehicleHash.Faggio : VehicleHash.Boxville2);
                    try
                    {
                        if (!model.IsValid || !model.IsInCdImage || !model.Request(2500)) { Notify.Show("Modelo do veiculo indisponivel."); return; }
                        vehicle = World.CreateVehicle(model, spawn, pizza ? p.Heading : options.Depot.Heading);
                    }
                    finally { model.MarkAsNoLongerNeeded(); }
                    if (vehicle == null || !vehicle.Exists()) { Notify.Show("Nao foi possivel retirar o veiculo."); return; }
                    supplied = true; vehicle.IsPersistent = true;
                    if (!p.Exists() || p != Game.Player.Character || Game.IsMissionActive || p.IsDead) return;
                    float tank = vehicle.PetrolTankVolume; vehicle.FuelLevel = !float.IsNaN(tank) && tank > 0 ? tank : 60;
                    Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, vehicle, (pizza ? "PZ" : "PO") + random.Next(100000,999999));
                    if (rapid) p.SetIntoVehicle(vehicle, VehicleSeat.Driver);
                }
                JobVehicles.Reserve(vehicle);
                vehicleBlip = World.CreateBlip(vehicle.Position); vehicleBlip.Sprite = pizza ? BlipSprite.PersonalVehicleBike : BlipSprite.PersonalVehicleCar;
                vehicleBlip.Color = BlipColor.Blue; vehicleBlip.Name = Label + ": veiculo de trabalho";
                run = new DeliveryRun(count, !rapid); nextPaymentAttempt = 0; cancelHeldAt = -1;
                if (rapid) run.EnterVehicle();
                NextDestination(); if (!Active) return; menu.Visible = false; started = true;
                Notify.Show(Label + ": turno iniciado. " + (rapid ? "Entregue como antes. " : "Entre no veiculo marcado. ") + "Segure End para cancelar.");
            }
            finally { if (!started) { try { Cleanup(); } finally { run = null; gate.Release(Name); } } }
        }
        Vector3 FindSpawn(Vector3 origin)
        {
            // Do not clear an area, delete traffic or place the van on top of parked cars.
            for (int i = 0; i < 8; i++)
            {
                var point = World.GetNextPositionOnStreet(origin + new Vector3((i % 3 - 1) * 7, (i / 3 - 1) * 7, 0), true);
                if (point == Vector3.Zero || point.DistanceTo(origin) > 45) continue;
                if (World.GetNearbyVehicles(point, 5).Length == 0 && World.GetNearbyPeds(point, 3).Length == 0) return point;
            }
            return Vector3.Zero;
        }
        void NextDestination()
        {
            if (targetBlip != null && targetBlip.Exists()) targetBlip.Delete(); targetBlip = null;
            if (run.Stage == DeliveryStage.Returning) destination = Depot;
            else if (quick)
            {
                var p = Game.Player.Character; destination = Vector3.Zero;
                for (int i = 0; i < 8; i++)
                {
                    float radius = Cfg.PizzaMinDist + (float)random.NextDouble() * (Cfg.PizzaMaxDist - Cfg.PizzaMinDist);
                    var point = World.GetNextPositionOnStreet(p.Position.Around(radius), true);
                    if (point != Vector3.Zero && point.DistanceTo2D(p.Position) >= 50) { destination = point; break; }
                }
                if (destination == Vector3.Zero) { End("Nenhuma rua de entrega disponivel."); return; }
            }
            else destination = Position(route[run.Delivered]);
            distance = Game.Player.Character.Position.DistanceTo2D(destination);
            deadline = Game.GameTime + (int)Math.Min(1800000, distance / 8 * 1000 + 45000);
            targetBlip = World.CreateBlip(destination); targetBlip.Sprite = run.Stage == DeliveryStage.Returning ? BlipSprite.Garage : BlipSprite.Package;
            targetBlip.Color = BlipColor.Yellow; targetBlip.Scale = .8f; targetBlip.ShowRoute = true;
            targetBlip.Name = run.Stage == DeliveryStage.Returning ? Label + ": retornar a base" : Label + ": " + (quick ? "entrega " + (run.Delivered + 1) : route[run.Delivered].Name);
        }
        bool InputAllowed => !InterfaceOptions.ModalOpen && !pool.AreAnyVisible && !Game.IsPaused;
        bool SafeOnFoot(Ped p) => !p.IsInVehicle() && !p.IsRagdoll && !p.IsInCombat && p.Weapons.Current.Hash == WeaponHash.Unarmed;
        void Marker(Vector3 point, Color color, float size = 1.2f)
        {
            if (Game.Player.Character.Position.DistanceTo(point) < 60) World.DrawMarker(MarkerType.Cylinder, point - new Vector3(0, 0, 1), Vector3.Zero, Vector3.Zero, new Vector3(size, size, 1), color);
        }
        public void Tick()
        {
            var p = Game.Player.Character;
            if (!Active)
            {
                if (!p.IsDead && !Game.IsMissionActive && InputAllowed)
                {
                    Marker(StartPosition, Color.FromArgb(110,80,160,255));
                    if (!p.IsInVehicle() && p.Position.DistanceTo(StartPosition) < 2)
                    { Prompt.Help("~INPUT_CONTEXT~: " + Label + " / contratos"); if (Game.IsControlJustPressed(GTA.Control.Context)) OpenMenu(); }
                }
                return;
            }
            if (p.IsDead || Game.IsMissionActive || vehicle == null || !vehicle.Exists() || vehicle.IsDead)
            { End(p.IsDead ? "Voce morreu. Turno encerrado." : Game.IsMissionActive ? "Turno encerrado ao iniciar uma missao." : "Veiculo de trabalho perdido. Turno encerrado."); return; }
            if (cargoDoorsOpen && p.IsInVehicle(vehicle)) CloseCargoDoors();
            if (vehicleBlip != null && vehicleBlip.Exists()) { vehicleBlip.Position = vehicle.Position; vehicleBlip.Alpha = p.IsInVehicle(vehicle) ? 0 : 255; }
            if (!quick && (run.Stage == DeliveryStage.Carrying || run.Stage == DeliveryStage.Handover) && !SafeOnFoot(p)) { cargo.Stop(); run.RecoverPackage(); Notify.Show("Pacote devolvido a carga. Retire novamente no veiculo."); }
            if (InterfaceOptions.ModalOpen || Game.IsPaused) { cargo.Stop(); if (run.Stage == DeliveryStage.Handover) run.InterruptHandover(); return; }
            if (!quick && run.Stage == DeliveryStage.Carrying && !cargo.Started) cargo.Start(p, pizza);
            cargo.Tick();
            DrawStatus();
            if (run.Stage == DeliveryStage.AwaitVehicle)
            {
                if (p.IsInVehicle(vehicle) && vehicle.Driver == p) { run.EnterVehicle(); Notify.Show("Siga o GPS. No endereco, pare e retire a carga do veiculo com E."); }
                else if (InputAllowed) Prompt.Help("Entre como motorista no veiculo azul de " + Label + ".");
                return;
            }
            if (run.Stage == DeliveryStage.Returning) { TickReturn(p); return; }
            Marker(destination, Color.FromArgb(110,255,200,0));
            if (quick) { TickQuick(p); return; }
            if (!InputAllowed) { if (run.Stage == DeliveryStage.Handover) run.InterruptHandover(); return; }
            if (run.Stage == DeliveryStage.NeedPackage)
            {
                if (p.Position.DistanceTo(destination) > 45) return;
                var rear = vehicle.GetOffsetPosition(new Vector3(0, pizza ? -1.4f : -3.2f, 0));
                Marker(rear, Color.FromArgb(100,80,160,255), .8f);
                if (!p.IsInVehicle() && p.Position.DistanceTo(rear) < 2.2f && vehicle.Speed < .5f)
                {
                    Prompt.Help(SafeOnFoot(p) ? "~INPUT_CONTEXT~: retirar " + (pizza ? "pizza" : "caixa") + " do veiculo" : "Guarde a arma para retirar a carga.");
                    if (SafeOnFoot(p) && Game.IsControlJustPressed(GTA.Control.Context))
                    {
                        if (!pizza) { vehicle.Doors[VehicleDoorIndex.BackLeftDoor].Open(false, false); vehicle.Doors[VehicleDoorIndex.BackRightDoor].Open(false, false); cargoDoorsOpen = true; }
                        run.TakePackage(); cargo.Start(p, pizza);
                    }
                }
                return;
            }
            if (run.Stage == DeliveryStage.Carrying && p.Position.DistanceTo(destination) < 2.5f)
            {
                Prompt.Help("~INPUT_CONTEXT~: entregar " + (pizza ? "pizza" : "caixa"));
                if (Game.IsControlJustPressed(GTA.Control.Context)) { run.BeginHandover(); cargo.Give(); handoverUntil = Game.GameTime + 1000; }
            }
            if (run.Stage == DeliveryStage.Handover)
            {
                if (p.Position.DistanceTo(destination) > 3 || p.Speed > 1.5f) { run.InterruptHandover(); cargo.Stop(); return; }
                if (unchecked(Game.GameTime - handoverUntil) >= 0) CommitDrop();
            }
        }
        void DrawStatus()
        {
            string stage = run.Stage == DeliveryStage.AwaitVehicle ? "retire o veiculo" : run.Stage == DeliveryStage.Returning ? "retorne a base" : run.Stage == DeliveryStage.Carrying ? "leve a carga a pe" : "siga o GPS";
            string text = Label + "  " + run.Delivered + "/" + run.Total + "  " + stage;
            if (pizza && quick) text += "  " + ((deadline - Game.GameTime) / 1000) + "s";
            bool banner = Visuals.TryJobHeader(Name, text, Color.White);
            if (!banner) new TextElement(text, new PointF(GTA.UI.Screen.ScaledWidth / 2f, 20), .4f, Color.White, GTA.UI.Font.ChaletLondon, Alignment.Center).Draw();
            if (!quick && run.Stage != DeliveryStage.Returning && run.Stage != DeliveryStage.AwaitVehicle)
            {
                if (banner)
                {
                    float width = 320 * Visuals.Scale, x = (Visuals.Width-width)/2, y = Visuals.InsetY+118*Visuals.Scale;
                    Visuals.Rect(x,y,width,30*Visuals.Scale,Color.FromArgb(180,8,12,18));
                    Visuals.Label(route[run.Delivered].Name,x+width/2,y+2,.27f*Visuals.Scale,Color.White,GTA.UI.Font.ChaletLondon,Alignment.Center,width-12);
                }
                else new TextElement(route[run.Delivered].Name,new PointF(GTA.UI.Screen.ScaledWidth/2f,46),.32f,Color.White,GTA.UI.Font.ChaletLondon,Alignment.Center).Draw();
            }
        }
        void TickQuick(Ped p)
        {
            if (!InputAllowed || p.Position.DistanceTo(destination) > 5) return;
            Entity mover = p.IsInVehicle() ? (Entity)p.CurrentVehicle : p;
            if (mover.Velocity.Length() > 3) { Prompt.Help("Pare para entregar a pizza."); return; }
            run.TakePackage(); run.BeginHandover(); CommitDrop();
        }
        bool Pay(int step, int amount, bool delivery, bool complete)
        {
            if (unchecked(Game.GameTime - nextPaymentAttempt) < 0) return false;
            bool credited;
            try
            {
                if (!Wallet.PayDelivery(Name, run.Id, step, amount, !pizza, delivery, complete, out credited))
                { nextPaymentAttempt = Game.GameTime + 3000; Notify.Show("Limite de saldo/progresso. Libere saldo ou cancele o turno."); return false; }
            }
            catch (Exception ex) { nextPaymentAttempt = Game.GameTime + 5000; Log.Error(Name + " payment; run not advanced", ex); Notify.Show("Falha ao salvar pagamento. Entrega nao avancou; tente novamente."); return false; }
            if (credited)
            {
                MirrorLegacy();
                Notify.Show("~g~+$" + amount + (pizza ? " em dinheiro" : " no banco") + "~s~ / " + Label);
                Sfx.Play("delivered");
                try
                {
                    if (delivery) achievements.Bump(pizza ? "pizza_drops" : "postop_drops");
                    achievements.Bump("cash_earned", amount); if (complete) achievements.Bump(pizza ? "shifts_done" : "postop_shifts");
                    achievements.Save();
                }
                catch (Exception ex) { Log.Error(Name + " achievements; payment already committed", ex); }
            }
            return true;
        }
        void CommitDrop()
        {
            int bonus = pizza && unchecked(deadline - Game.GameTime) >= 0 ? Cfg.PizzaOnTimeBonus : 0;
            int amount = DeliveryRoute.Pay(pizza ? Cfg.PizzaBase : options.BasePay, pizza ? Cfg.PizzaPerMeter : options.PayPerMeter, distance, bonus);
            if (pizza && unchecked(deadline - Game.GameTime) < 0) amount /= 2;
            if (quick && run.Delivered == run.Total - 1) amount = (int)Math.Min(1000000L, (long)amount + Cfg.PizzaShiftBonus);
            if (!run.CommitDelivery(step => Pay(step, amount, true, quick && run.Delivered == run.Total - 1))) return;
            cargo.Stop();
            if (run.Stage == DeliveryStage.Finished) { End("Turno concluido!", false); return; }
            NextDestination();
            if (run != null && run.Stage == DeliveryStage.Returning) Notify.Show("Entregas concluidas. Retorne a base com o veiculo para receber o bonus.");
        }
        void TickReturn(Ped p)
        {
            Marker(Depot, Color.FromArgb(110,80,160,255), 3);
            if (!InputAllowed || !p.IsInVehicle(vehicle) || vehicle.Driver != p || vehicle.Position.DistanceTo(Depot) > (pizza ? 12 : 8) || vehicle.Speed > .5f) return;
            Prompt.Help("~INPUT_CONTEXT~: devolver veiculo e encerrar turno");
            if (Game.IsControlJustPressed(GTA.Control.Context) && run.ReturnToDepot(step => Pay(step, pizza ? Math.Min(1000000,Cfg.PizzaShiftBonus) : options.ReturnBonus, false, true))) End("Turno concluido. Bonus de retorno recebido!", false);
        }
        public void OnKey(KeyEventArgs e)
        {
            if (!pizza || e.KeyCode != Cfg.JobKey) return;
            if (Active) End("Turno de pizza cancelado."); else if (!pool.AreAnyVisible) Begin(true);
        }
        // End is held in Tick through the shared hub; this avoids collisions with existing function keys.
        internal void PollCancel()
        {
            if (!Active || !InputAllowed || !Game.IsKeyPressed(Keys.End)) { cancelHeldAt = -1; return; }
            if (cancelHeldAt < 0) cancelHeldAt = Game.GameTime;
            if (unchecked(Game.GameTime - cancelHeldAt) >= 1000) End("Turno cancelado. Entregas ja pagas foram mantidas.");
        }
        void End(string message, bool cancel = true)
        {
            if (run != null && cancel) run.Cancel();
            bool returningVehicle = supplied && vehicle != null && vehicle.Exists() && Game.Player.Character.IsInVehicle(vehicle);
            try { Cleanup(); }
            finally { run = null; menu.Visible = false; gate.Release(Name); }
            Notify.Show(message + (returningVehicle ? " Desca para devolver o veiculo da empresa." : ""));
        }
        void CloseCargoDoors()
        {
            if (cargoDoorsOpen && vehicle != null && vehicle.Exists())
            { vehicle.Doors[VehicleDoorIndex.BackLeftDoor].Close(false); vehicle.Doors[VehicleDoorIndex.BackRightDoor].Close(false); }
            cargoDoorsOpen = false;
        }
        void Cleanup()
        {
            cargo.Stop(); CloseCargoDoors();
            if (targetBlip != null && targetBlip.Exists()) targetBlip.Delete(); targetBlip = null;
            if (vehicleBlip != null && vehicleBlip.Exists()) vehicleBlip.Delete(); vehicleBlip = null;
            if (supplied) JobVehicles.Retire(vehicle); else JobVehicles.Release(vehicle);
            vehicle = null; supplied = false;
        }
        public void Reload()
        {
            if (Active) End("Turno encerrado na troca de perfil.");
            menu.Visible = false; LoadLegacy();
        }
        public void Save() { if (pizza) { MirrorLegacy(); Store.Write("PizzaStats.json", legacy); } }
        public void Dispose()
        {
            try { Cleanup(); }
            finally { run = null; gate.Release(Name); menu.Visible = false; if (startBlip != null && startBlip.Exists()) startBlip.Delete(); }
        }
    }
    public sealed class PostOpJob : DeliveryJobSystem
    {
        public PostOpJob(ObjectPool pool, AchievementSystem achievements, ShiftGate gate) : base(pool, achievements, gate, false) { }
    }
}
