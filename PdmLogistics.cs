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
    // Physical assets are temporary representations of paid company stock, never personal cars.
    public sealed partial class PdmLogistics : ISystem, IWorkContract
    {
        readonly PdmMenuView ui;
        public Action Back;
        public System.Collections.Generic.IEnumerable<PdmLocation> Locations=>new[]{
            PdmLocation.At("haul:trailer","Logistica","Retirada do conjunto / carreta","Retirada iniciada no escritorio usa este patio. Ao iniciar pelo porto, o conjunto e preparado la.",options.Trailer),
            PdmLocation.At("haul:dock","Logistica","Porto / coleta dos pedidos","Pressione E no marcador para selecionar pedidos pagos e iniciar a retirada. GPS sozinho nao inicia a coleta.",options.Dock),
            PdmLocation.At("haul:return","Logistica","PDM / recebimento de estoque","Volte com o conjunto carregado para descarregar os pedidos.",options.Return)};
        public string Name => "pdmhaul";
        public string Label => "PDM / coleta no porto";
        public bool Active => run != null;
        public Vector3 StartPosition { get; }
        readonly ObjectPool pool; readonly ShiftGate gate; readonly PdmLogisticsOptions options; readonly NativeMenu menu;
        readonly Dictionary<string, Vehicle> cargo = new Dictionary<string, Vehicle>();
        readonly HashSet<string> selected=new HashSet<string>(),removedCargo=new HashSet<string>();
        readonly HashSet<string> loaded = new HashSet<string>(); readonly List<Blip> cargoBlips = new List<Blip>();
        Vehicle truck, trailer, display; Blip route; PdmHaulRun run;
        string ownerProfile, cancelReason; int began, retryAt, cancelSince = -1;
        static Vector3 Pos(JobPoint p) => new Vector3(p.X, p.Y, p.Z);
        static bool Exists(Vehicle v) => v != null && PdmAssets.Valid(v);
        static bool Empty(Vehicle v) => !Exists(v) || v.Occupants.Length == 0 && v.Speed < .5f;
        static int Health(float n) => float.IsNaN(n) || float.IsInfinity(n) ? 0 : Math.Max(0, Math.Min(1000, (int)n));
        bool InputAllowed => PdmMenuRuntime.Allowed && !pool.AreAnyVisible;
        bool Foot => !Game.Player.Character.IsInVehicle() && !Game.Player.Character.IsDead && !Game.Player.Character.IsRagdoll && !Game.Player.Character.IsInCombat;
        bool Coupled => JobVehicles.Towing(truck, trailer);
        bool Foreign => Exists(truck) && Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER, truck) && !Coupled || JobVehicles.TrailerHasOtherTractor(trailer, truck);
        public PdmLogistics(ObjectPool pool, ShiftGate gate, Vector3 office)
        {
            this.pool = pool; this.gate = gate; StartPosition = office;
            var defaults = PdmLogisticsDefaults.Load(); options = Store.LoadDef("Jobs/PdmLogistics.json", () => defaults);
            if (options == null || !options.Valid) { Log.Write("Invalid PdmLogistics.json; embedded defaults used, original retained."); options = defaults; }
            menu = new NativeMenu("PDM / coleta", "PEDIDOS PAGOS / PORTO / RECEBIMENTO"); ui=new PdmMenuView(pool,menu,ex=>ui.Problem("Falha na retirada","Confira os pedidos e a operacao atual antes de tentar de novo.",ex,OpenMenu)); ui.DefaultRefresh=OpenMenu; pool.Add(menu); Visuals.Register(menu, "pdm"); Recover();
        }
        void Add(string title, string text, Action action)
        {
            ui.Add(title, text, () => { try { action(); } catch (Exception ex) { ui.Problem("Falha na retirada","Confira os pedidos e a operacao atual antes de tentar de novo.",ex,OpenMenu); } });
        }
        public void OpenMenu()
        {
            ui.HideAll(); ui.Begin(Name,Back,true,OpenMenu);
            Add("GPS: escritorio PDM", "Inicie a pe no escritorio externo da empresa.", () => ui.Waypoint(StartPosition,"escritorio PDM"));
            Add("GPS: porto / marcador de retirada", "No porto, pressione E no marcador e inicie a retirada. Apenas marcar GPS nao cria carros.", () => ui.Waypoint(Pos(options.Dock),"porto / coleta de estoque PDM"));
            if (!Wallet.Pdm.Valid) { Notify.Show("Dados PDM invalidos, arquivo preservado."); return; }
            var pending=Wallet.Pdm.Stock.Where(v=>v.AwaitingCollection).ToList();selected.RemoveWhere(id=>!pending.Any(v=>v.Id==id));
            menu.Name="RETIRADA / "+pending.Count+" ENCOMENDAS";
            ui.Add("1. Escolher pedido / 2. Iniciar retirada","Inicie a pe no escritorio ou no marcador do porto. O conjunto e criado no local escolhido; depois engate, carregue os carros e devolva a PDM. Nao existe nova compra na coleta.",()=>{},false);
            foreach(var stock in pending){string id=stock.Id;
                Add((selected.Contains(id)?"[X] ":"[ ] ")+stock.Name+" / "+stock.Plate,"Pago pelo caixa da empresa: $"+stock.Cost+". Aguarda retirada; so aparece fisicamente depois de iniciar a coleta. Motor "+stock.Engine+"; carroceria "+stock.Body+". Selecione uma ou duas unidades.",()=>{
                    if(!selected.Remove(id)){if(selected.Count>=2){Notify.Show("A TRFlat transporta ate dois pedidos por coleta.");return;}selected.Add(id);}OpenMenu();
                });}
            var chosen=pending.Where(v=>selected.Contains(v.Id)).Select(v=>v.Id).ToList();
            ui.AddConfirm("Iniciar retirada dos selecionados / "+chosen.Count,"No escritorio: retire o conjunto na PDM e va ao porto. No porto: o conjunto e preparado no patio local. Depois engate/carregue e devolva os dois. Sem novo debito.",()=>PreparePickup(chosen),chosen.Count>=1 && chosen.Count<=2);
            ui.AddConfirm("Retirar primeiro pedido","Seleciona a primeira encomenda em boas condicoes e inicia no escritorio ou no porto, conforme onde voce esta. Caixa, banco e carteira nao sao debitados.",()=>PreparePickup(pending.Where(v=>v.Engine>=300 && v.Body>=300).Take(1).Select(v=>v.Id).ToList()),pending.Any(v=>v.Engine>=300 && v.Body>=300));
            if(pending.Count==0)ui.Add("Nenhum pedido aguardando","Encomende uma unidade no fornecedor da PDM.",()=>{},false);
            if (Active) ui.AddConfirm("Encerrar coleta", "Pare e saia dos veiculos. Pedidos pagos continuam no porto, sem pagamento extra.", () => { cancelReason = "Cancelada"; menu.Visible = false; });
            if (Wallet.Pdm.Collection != null && !Active) ui.AddConfirm("Recuperar coleta interrompida", "Pare e saia do conjunto e dos carros. Recebimento ja confirmado e concluido, demais pedidos continuam no porto.", () => { Recover(); OpenMenu(); });
            if(Back!=null)Add("Voltar","Empresa PDM.",Back);ui.Show();
        }
        void Route(Vector3 p, string label)
        {
            if (route != null && route.Exists()) route.Delete(); route = World.CreateBlip(p); route.Name = label; route.Color = BlipColor.Yellow; route.ShowRoute = true;
        }
        static Vehicle Spawn(JobPoint anchor, string modelName, string plate,bool exact=false)
        {
            var model = new Model(modelName);
            try
            {
                if (!model.IsValid || !model.IsInCdImage || !model.IsVehicle || !PdmStreaming.Ready(model)) return null;
                var bay = exact?anchor:RigSpawn.FindVehicle(anchor, model, "PDM coleta"); if (bay == null) return null;
                var v = PdmAssets.Track(World.CreateVehicle(model, Pos(bay), bay.Heading)); if (!Exists(v)) return null;
                v.IsPersistent = true; v.IsStolen = false; v.NeedsToBeHotwired = false;
                float capacity = v.PetrolTankVolume; v.FuelLevel = capacity > 0 && !float.IsNaN(capacity) && !float.IsInfinity(capacity) ? Math.Min(10000, capacity) : 100;
                Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, v, plate); JobVehicles.Reserve(v); return v;
            }
            finally { PdmStreaming.Release(model); }
        }
        PdmEntityRef Reference(Vehicle v, string model) => new PdmEntityRef { Model = model, Plate = Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, v).Trim(), Handle = v.Handle };
        void Begin(int count,List<string> selection=null,bool atPort=false)
        {
            var ids=selection==null?Wallet.Pdm.Stock.Where(s=>s.AwaitingCollection && s.Engine>=300 && s.Body>=300).Take(count).Select(s=>s.Id).ToList():new List<string>(selection);
            var plan=PickupPlan(ids);
            if(Active || !PersonalServiceMenu.SafeFoot || !plan.Allowed || count!=ids.Count || plan.AtPort!=atPort){ui.Problem("Retirada nao iniciada",plan.Reason??"Local ou contexto mudou. Fique a pe no escritorio ou no marcador do porto e confira novamente.",null,OpenMenu);return;}
            if (!ClearDisplay()) return;
            if (!gate.Acquire(Name)) { Notify.Show("Encerre a atividade de " + gate.Owner + "."); return; }
            bool confirmed = false;
            try
            {
                string id=Guid.NewGuid().ToString("N");JobPoint trailerPoint=options.Trailer,truckPoint=null;
                if(atPort){
                    var pair=RigSpawn.FindForModels(PdmPickupRules.PortRigAnchor(options.Dock),new Model(options.TruckModel),new Model(options.TrailerModel),"PDM porto");
                    if(pair==null){ui.Problem("Patio do porto ocupado","Nao foi encontrado espaco/chao livre para o Packer e a TRFlat. Afaste seu carro da area de retirada e tente novamente. Os pedidos continuam salvos; nenhum novo debito.",null,OpenMenu);return;}
                    trailerPoint=pair.Trailer;truckPoint=pair.Truck;
                }
                trailer=Spawn(trailerPoint,options.TrailerModel,"PHL"+id.Substring(0,5),atPort);
                if(!Exists(trailer)){ui.Problem("Carreta indisponivel","Modelo/chao/vaga indisponivel para "+options.TrailerModel+". Pedidos preservados; nenhum debito novo. Confira RPCore.log.",null,OpenMenu);return;}
                if(truckPoint==null){double heading=options.Trailer.Heading*Math.PI/180;truckPoint=new JobPoint{X=trailer.Position.X-(float)Math.Sin(heading)*19,Y=trailer.Position.Y+(float)Math.Cos(heading)*19,Z=trailer.Position.Z,Heading=options.Trailer.Heading};}
                truck=Spawn(truckPoint,options.TruckModel,"PHT"+id.Substring(0,5),atPort);
                if (!Exists(truck) || !Wallet.PdmCollect(id, ids, Reference(truck, options.TruckModel), Reference(trailer, options.TrailerModel), out _)) { ui.Problem("Conjunto nao confirmado","Caminhao/vaga indisponivel ou gravacao recusada. Pedidos e caixa preservados; nenhum debito novo. Confira RPCore.log.",null,OpenMenu); return; }
                run = new PdmHaulRun(id, count, options.UnloadSeconds); ownerProfile = Profile.Id; began = Game.GameTime; retryAt = began+1000; cancelReason = null;worldProblemReported=false;
                foreach (var v in new[] { truck, trailer }) { var b = v.AddBlip(); b.Name = v == truck ? "RPCore: Packer PDM" : "RPCore: TRFlat PDM"; b.Color = BlipColor.Blue; cargoBlips.Add(b); }
                Route(trailer.Position, "RPCore: carreta PDM"); menu.Visible = false; confirmed = true;selected.Clear();removedCargo.Clear();
                Log.Write("PDM pickup started / id="+id+" / origin="+(atPort?"port":"office")+" / orders="+string.Join(",",ids)+" / profile="+Profile.Id);
                Notify.Show("Retirada iniciada. Os carros aparecem em sequencia no porto. Entre no Packer, aproxime a traseira da frente da TRFlat e aperte E para engatar.");
            }
            finally { if (!confirmed) { JobVehicles.RetireRig(truck, trailer); truck = trailer = null; gate.Release(Name); } }
        }
        void Couple()
        {
            Status("entre no Packer / engate a TRFlat / E auxilia o engate");
            bool driver = Game.Player.Character.IsInVehicle(truck) && truck.Driver == Game.Player.Character;
            float delta = Math.Abs(truck.Heading - trailer.Heading); if (delta > 180) delta = 360 - delta;
            bool ready = TankerRules.CanCouple(driver, Foreign || Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER, truck), truck.RearPosition.DistanceTo(trailer.FrontPosition), delta, truck.Speed, trailer.Speed);
            if (ready) { Prompt.Help("~INPUT_CONTEXT~: engatar carreta PDM"); if (Game.IsControlJustPressed(GTA.Control.Context)) Function.Call(Hash.ATTACH_VEHICLE_TO_TRAILER, truck, trailer, 6f); }
        }
        void SpawnCargo()
        {
            var operation = Wallet.Pdm.Collection;
            if (operation == null || unchecked(Game.GameTime - retryAt) < 0 || Game.Player.Character.Position.DistanceTo(Pos(options.Dock)) > 150 || trailer.Position.DistanceTo(Pos(options.Dock)) > 150) return;
            retryAt = Game.GameTime + 5000;
            foreach (var item in operation.Cargo)
            {
                if (cargo.ContainsKey(item.StockId) || item.Handle > 0) continue;
                var stock = Wallet.Pdm.Stock.Find(s => s.Id == item.StockId); var anchor = new JobPoint { X = options.Dock.X, Y = options.Dock.Y + cargo.Count * 8, Z = options.Dock.Z, Heading = options.Dock.Heading };
                var model=new Model(stock.Model);
                if(!model.IsValid || !model.IsInCdImage || !model.IsCar){ui.Problem("Modelo da encomenda indisponivel","Carro "+stock.Name+" / modelo "+stock.Model+" nao esta disponivel no GTA. Pedido preservado; nenhuma nova cobranca.",null,OpenMenu);break;}
                if(!PdmStreaming.Ready(model)){Notify.Show("Carregando encomenda: "+stock.Name+". Aguarde a proxima tentativa; nenhum debito novo.");break;}
                var v = Spawn(anchor, stock.Model, stock.Plate); if (!Exists(v)) { Notify.Show("Vaga do pedido "+stock.Name+" ocupada/sem chao carregado. Afaste o conjunto do marcador; nova tentativa em 5s."); break; }
                bool saved = false;
                try
                {
                    v.EngineHealth = stock.Engine; v.BodyHealth = stock.Body; v.DirtLevel = stock.Dirt;
                    if (!Wallet.PdmCargo(run.Id, stock.Id, v.Handle, out _)) break;
                    cargo.Add(stock.Id, v); saved = true; var b = v.AddBlip(); b.Name = "RPCore: pedido " + stock.Name; b.Color = BlipColor.Blue; cargoBlips.Add(b);break;
                }
                finally { if (!saved) { v.Delete(); JobVehicles.Release(v); } }
            }
        }
        void Load()
        {
            SpawnCargo(); Status("porto: dirija cada carro azul ate a traseira da carreta / pare, saia e E carrega");
            foreach (var pair in cargo)
            {
                if (loaded.Contains(pair.Key)) continue; var v = pair.Value;
                bool ready = Foot && Empty(v) && v.Position.DistanceTo(trailer.RearPosition) < 12 && Game.Player.Character.Position.DistanceTo(v.Position) < 5 && truck.Speed < .5f && trailer.Speed < .5f;
                if (!ready) continue; Prompt.Help("~INPUT_CONTEXT~: carregar pedido na TRFlat");
                if (!Game.IsControlJustPressed(GTA.Control.Context)) return;
                var slot = options.Slots[run.Loaded];
                if (run.Load(true, () => { v.AttachTo(trailer, new Vector3(slot.X, slot.Y, slot.Z), Vector3.Zero, false, false, false, false, EulerRotationOrder.XYZ, true, false); return v.IsAttachedTo(trailer); }))
                { v.IsCollisionEnabled = false; loaded.Add(pair.Key); if (run.Stage == PdmHaulStage.Returning) Route(Pos(options.Return), "RPCore: recebimento PDM"); }
                return;
            }
        }
        bool UnloadReady => InputAllowed && Coupled && !Foreign && truck.Speed < .5f && trailer.Speed < .5f && trailer.Position.DistanceTo(Pos(options.Return)) < 24 && Game.Player.Character.Position.DistanceTo(trailer.Position) < 30 && loaded.Count == run.Total && cargo.Values.All(v => Exists(v) && v.Occupants.Length == 0 && v.IsAttachedTo(trailer));
        public void Tick()
        {
            try{PortTick();}catch(Exception ex){if(!portReported){portReported=true;Log.Error("PDM port marker",ex);}}ui.Tick(PdmMenuRuntime.Allowed);
            if (!Active) return;
            try
            {
                if (run.Stage == PdmHaulStage.Confirming) { FinishArrival(); return; }
                if (cancelReason != null) { Cancel(); return; }
                if (Game.Player.Character.IsDead || Game.IsMissionActive || Game.Player.Wanted.WantedLevel > 0 || unchecked(Game.GameTime - began) > options.TimeLimitSeconds * 1000 || !Exists(truck) || !Exists(trailer) || truck.IsDead || trailer.IsDead || cargo.Values.Any(v => !Exists(v) || v.IsDead) || loaded.Any(id => !cargo[id].IsAttachedTo(trailer)))
                { cancelReason = "Coleta interrompida"; Cancel(); return; }
                if (!InputAllowed) { run.Interrupt(); return; }
                if(run.Stage==PdmHaulStage.Coupling)SpawnCargo();
                if (!Coupled) { run.Interrupt(); Couple(); return; }
                if (run.Stage == PdmHaulStage.Coupling) { run.Coupled(true); Route(Pos(options.Dock), "RPCore: pedidos no porto"); }
                if (run.Stage == PdmHaulStage.Loading) { Load(); return; }
                if (run.Stage == PdmHaulStage.Returning)
                {
                    Status("volte a PDM / pare o conjunto no recebimento / E descarrega");
                    if (UnloadReady) { Prompt.Help("~INPUT_CONTEXT~: descarregar pedidos na PDM"); if (Game.IsControlJustPressed(GTA.Control.Context)) run.Unload(Game.GameTime, true); }
                    return;
                }
                Status("recebendo carros / mantenha o conjunto parado");
                if (unchecked(Game.GameTime - retryAt) < 0) { if (!UnloadReady) run.Interrupt(); return; }
                if (run.Arrive(Game.GameTime, UnloadReady, () => Remember() && Wallet.PdmArrive(run.Id, out _))) FinishArrival();
            }
            catch (Exception ex) { retryAt = Game.GameTime + 5000; Log.Error("pdm logistics checkpoint", ex); if(!worldProblemReported){worldProblemReported=true;ui.Problem("Coleta pendente","Confira a coleta atual e os pedidos antes de iniciar outra operacao. A gravacao sera tentada novamente; veja o erro abaixo.",ex,OpenMenu);} }
        }
        bool Remember(bool missingIsDamage=true)
        {
            var operation=Wallet.Pdm.Collection;if(operation==null)return false;var health=new Dictionary<string,int[]>();
            foreach(var item in operation.Cargo){cargo.TryGetValue(item.StockId,out var vehicle);var stock=Wallet.Pdm.Stock.Find(v=>v.Id==item.StockId);
                bool lost=missingIsDamage && !operation.Arrived && item.Handle>0 && !removedCargo.Contains(item.StockId);
                health[item.StockId]=Exists(vehicle)?new[]{Health(vehicle.EngineHealth),Health(vehicle.BodyHealth)}:lost?new[]{0,0}:new[]{stock.Engine,stock.Body};
            }return Wallet.PdmCargoHealth(operation.Id,health,out _);
        }
        bool DeleteCargo()
        {
            if (cargo.Values.Any(v => !Empty(v) && !(Exists(v) && v.IsAttachedTo(trailer) && v.Occupants.Length == 0 && (!Exists(trailer) || trailer.Speed < .5f)))) return false;
            foreach (var pair in cargo) {var v=pair.Value;if(!Exists(v))continue;v.Detach();v.IsCollisionEnabled=true;v.Delete();if(PdmAssets.Valid(v))return false;JobVehicles.Release(v);removedCargo.Add(pair.Key);}
            return true;
        }
        void FinishArrival()
        {
            if (unchecked(Game.GameTime - retryAt) < 0) return;
            if (!DeleteCargo()) { Status("recebimento: pare o conjunto e saia dos carros transportados"); return; }
            if (!run.Complete(() => Wallet.PdmReceive(run.Id, out _))) { retryAt = Game.GameTime + 5000; return; }
            EndSession(); Notify.Show("Pedidos recebidos! Carros disponiveis no estoque para exposicao ou entrega. Sem nova cobranca.");
        }
        void Cancel()
        {
            Status("cancelamento: pare e saia do conjunto e dos carros");
            if (!Empty(truck) || !Empty(trailer) || cargo.Values.Any(v => Exists(v) && v.Occupants.Length > 0) || Foreign || unchecked(Game.GameTime - retryAt) < 0) return;
            if (!Remember() || !DeleteCargo() || !Wallet.PdmCollectCancel(run.Id, out _)) { retryAt = Game.GameTime + 5000; return; }
            EndSession(); Notify.Show("Coleta encerrada. Pedidos pagos continuam no porto com os danos registrados.");
        }
        static Vehicle Match(int handle, string model, string plate)
        {
            var v = handle > 0 ? Entity.FromHandle(handle) as Vehicle : null;
            return Exists(v) && v.Model.Hash == new Model(model).Hash && string.Equals(Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, v)?.Trim(), plate, StringComparison.OrdinalIgnoreCase) ? PdmAssets.Track(v) : null;
        }
        void Recover()
        {
            var c = Wallet.Pdm.Collection; if (c == null || !Wallet.Pdm.Valid) return;
            try
            {
                truck = Match(c.Truck.Handle, c.Truck.Model, c.Truck.Plate); trailer = Match(c.Trailer.Handle, c.Trailer.Model, c.Trailer.Plate); JobVehicles.Reserve(truck); JobVehicles.Reserve(trailer);
                foreach (var item in c.Cargo) { var s = Wallet.Pdm.Stock.Find(v => v.Id == item.StockId); var v = Match(item.Handle, s.Model, s.Plate); if (Exists(v)) { cargo[item.StockId] = v; JobVehicles.Reserve(v); } }
                if (!Empty(truck) || !Empty(trailer) || Foreign || cargo.Values.Any(v => v.Occupants.Length > 0 || v.Speed > .5f)) { Notify.Show("Coleta PDM interrompida: pare, saia dos veiculos e use Recuperar no menu."); return; }
                if (!Remember(false) || !DeleteCargo()) return;
                if (!(c.Arrived ? Wallet.PdmReceive(c.Id, out _) : Wallet.PdmCollectCancel(c.Id, out _))) return;
                EndSession(); Notify.Show(c.Arrived ? "Recebimento PDM recuperado; estoque pronto." : "Coleta interrompida; pedidos pagos preservados no porto.");
            }
            catch (Exception ex) { Log.Error("pdm logistics recovery", ex); }
        }
        public bool Display(string stockId)
        {
            var stock = Wallet.Pdm.Stock.Find(s => s.Id == stockId);
            if (Active || Wallet.Pdm.Active != null || Wallet.Pdm.Collection != null || Wallet.Pdm.SafeTransfer != null || Wallet.Pdm.Repo != null || Wallet.Pdm.Rental != null || Wallet.Pdm.ShowroomSale != null || Wallet.Pdm.PremisesService != null || stock == null || stock.AwaitingCollection || !ClearDisplay()) return false;
            display = Spawn(options.Showroom, stock.Model, stock.Plate);
            if (!Exists(display)) { Notify.Show("Vaga de exposicao ocupada; estoque preservado."); return false; }
            display.IsInvincible = true; display.EngineHealth = stock.Engine; display.BodyHealth = stock.Body; display.IsPositionFrozen = true; Function.Call(Hash.SET_VEHICLE_DOORS_LOCKED, display, 2); Notify.Show("Carro exposto no patio externo. Continua no estoque da empresa."); return true;
        }
        public bool ClearDisplay()
        {
            if (Exists(display)) { if (display.Occupants.Length > 0) { Notify.Show("Saia do carro de exposicao antes de recolher."); return false; } display.IsPositionFrozen = false; display.Delete(); if (PdmAssets.Valid(display)) return false; JobVehicles.Release(display); }
            display = null; return true;
        }
        void EndSession(bool retireRig = true)
        {
            if (retireRig) JobVehicles.RetireRig(truck, trailer); truck = trailer = null; cargo.Clear(); loaded.Clear(); foreach (var b in cargoBlips) if (b.Exists()) b.Delete(); cargoBlips.Clear();
            if (route != null && route.Exists()) route.Delete(); route = null; run = null; cancelReason = null; gate.Release(Name); menu.Visible = false;
        }
        void Status(string text) { if (!Visuals.TryJobHeader("pdm", "PDM / coleta", Color.White)) Visuals.Text("PDM / coleta", Visuals.Width / 2 - 200, 24, .34f, Color.White); float x = Visuals.Width / 2 - 390; Visuals.Rect(x, 900, 780, 80, Color.FromArgb(210, 8, 12, 18)); Visuals.Text(text, x + 12, 910, .29f, Color.White); Visuals.Text("End (segurar): encerrar / pedidos pagos permanecem no estoque", x + 12, 945, .23f, Color.White); }
        public void PollCancel() { if (!Active || !InputAllowed || !Game.IsKeyPressed(Keys.End)) { cancelSince = -1; return; } if (cancelSince < 0) cancelSince = Game.GameTime; if (unchecked(Game.GameTime - cancelSince) >= 1000) cancelReason = "Cancelada pelo jogador"; }
        public void OnKey(KeyEventArgs e) { }
        public void Save() { if (Active && ownerProfile == Profile.Id && Wallet.Pdm.Collection != null) Remember(); }
        void ReleaseSession()
        {
            // Keep persisted handles when an occupied asset cannot be removed. Recovery verifies identity.
            bool safe = Empty(truck) && Empty(trailer) && !Foreign && cargo.Values.All(Empty);
            if (safe) DeleteCargo();
            EndSession(safe); if (!ClearDisplay() && Exists(display)) { display.IsPositionFrozen = false; JobVehicles.Retire(display); display = null; }
        }
        public void Reload() { ClearPort();selected.Clear();ui.Reset(); ReleaseSession(); Recover(); }
        public void Dispose() { ClearPort();ui.Reset(); try { Save(); } catch (Exception ex) { Log.Error("pdm logistics dispose", ex); } finally { ReleaseSession(); } }
    }
}
