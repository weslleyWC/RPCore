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
    public sealed class PdmCompany : ISystem, IWorkContract
    {
        public Action OpenCollection, OpenStaff, SyncStaff, OpenSafe, OpenFinanceContracts, SyncFinance, OpenRepo, OpenRental, OpenShowroom;
        public Func<string,bool> PrepareStock;
        internal PdmOptions CompanyOptions => options;
        public PdmRepoOptions Repossession { get; }
        public PdmFinanceOptions Financing { get; }
        public Func<string,bool> DisplayStock;
        public Func<bool> ClearDisplay;
        public string Name => "pdm";
        public string Label => "PDM / empresa e entregas";
        public bool Active => run != null;
        public Vector3 StartPosition => Pos(options.Office);
        readonly ObjectPool pool; readonly AchievementSystem achievements; readonly ShiftGate gate; readonly PdmOptions options; readonly NativeMenu menu; readonly Blip officeBlip;
        readonly Random random = new Random();
        PdmRun run; Vehicle car; Ped buyer, animOwner; Blip carBlip, targetBlip;
        JobPoint destination; string ownerProfile, cancelReason; bool animRequested, animStarted;
        int began, nextSave, nextBuyer, departureAt, cancelSince = -1, cancelEngine = -1, cancelBody = -1;
        const string AnimDict = "mp_common", AnimClip = "givetake1_a";
        static Vector3 Pos(JobPoint p) => new Vector3(p.X, p.Y, p.Z);
        static int Health(float n) => float.IsNaN(n) || float.IsInfinity(n) ? 0 : Math.Max(0, Math.Min(1000, (int)n));
        bool Driver => car != null && car.Exists() && Game.Player.Character.IsInVehicle(car) && car.Driver == Game.Player.Character;
        bool InputAllowed => !Game.IsPaused && !InterfaceOptions.ModalOpen && !pool.AreAnyVisible;
        bool SafeFoot => Game.Player.Character.IsHuman && !Game.Player.Character.IsDead && !Game.Player.Character.IsInVehicle() && !Game.Player.Character.IsSwimming && !Game.Player.Character.IsRagdoll && !Game.Player.Character.IsInCombat;
        public PdmCompany(ObjectPool pool, AchievementSystem achievements, ShiftGate gate)
        {
            this.pool = pool; this.achievements = achievements; this.gate = gate;
            var defaults = PdmDefaults.Load(); options = Store.LoadDef("Jobs/Pdm.json", () => defaults);
            if (options == null || !options.Valid) { Log.Write("Invalid Pdm.json; embedded defaults used, original retained."); options = defaults; }
            var financeDefaults = PdmFinanceDefaults.Load(); Financing = Store.LoadDef("Jobs/PdmFinance.json", () => financeDefaults);
            if (Financing == null || !Financing.Valid) { Log.Write("Invalid PdmFinance.json; embedded defaults used, original retained."); Financing = financeDefaults; }
            var repoDefaults = PdmRepoDefaults.Load(); Repossession = Store.LoadDef("Jobs/PdmRepo.json", () => repoDefaults);
            if (Repossession == null || !Repossession.Valid) { Log.Write("Invalid PdmRepo.json; embedded defaults used, original retained."); Repossession = repoDefaults; }
            menu = new NativeMenu("PDM / empresa", "CAIXA / ESTOQUE / CLIENTES"); pool.Add(menu); Visuals.Register(menu, Name);
            officeBlip = MapBlips.Create(StartPosition, "RPCore: PDM / empresa", BlipSprite.PersonalVehicleCar, BlipColor.Orange, .8f); Recover();
        }
        void Add(string title, string text, Action action) { var item = new NativeItem(title, text); item.Activated += (s, e) => { try { action(); } catch (Exception ex) { Log.Error("pdm action", ex); Notify.Show("Acao PDM nao confirmada. Veja RPCore.log."); } }; menu.Add(item); }
        internal bool OfficeAction()
        {
            SyncStaff?.Invoke(); SyncFinance?.Invoke();
            if (!SafeFoot || Game.IsMissionActive || Game.Player.Wanted.WantedLevel > 0 || Game.Player.Character.Position.DistanceTo(StartPosition) > 5 || Active || Wallet.Pdm.Active != null || Wallet.Pdm.Collection != null || Wallet.Pdm.SafeTransfer != null || Wallet.Pdm.Repo != null || Wallet.Pdm.Rental != null || Wallet.Pdm.ShowroomSale != null)
            { Notify.Show("Va a pe ao escritorio externo PDM, sem procurado/missao/combate. Encerre/recupere a entrega pendente."); return false; } return true;
        }
        bool DataValid() { if (Wallet.Pdm.Valid) return true; Log.Write("Invalid Wallet.json/Pdm; original retained."); Notify.Show("Dados da empresa invalidos. Arquivo preservado."); return false; }
        public void OpenMenu()
        {
            pool.HideAll(); menu.Clear(); if (!DataValid()) return; var d = Wallet.Pdm;
            menu.Name = "PDM / caixa $" + d.Cash + " / " + d.Stock.Count + " carros";
            Add("GPS: escritorio externo PDM", "Empresa separada da compra de carro pessoal/garagem. Nao precisa entrar em interior.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, StartPosition.X, StartPosition.Y); menu.Visible = false; });
            if (!d.Owned) Add("Adquirir empresa: $" + options.PurchasePrice, "Debita o banco pessoal; nao inclui capital nem estoque. Valor proprio do RPCore.", () => { if (OfficeAction()) { if (!Wallet.PdmPurchase(options, Guid.NewGuid().ToString("N"), out bool changed)) Notify.Show("Saldo bancario insuficiente ou empresa ja adquirida."); else if (changed) Bump("pdm_owned"); OpenMenu(); } });
            else
            {
                if (OpenShowroom != null) Add("Showroom / vagas / compradores", "Expor estoque em vagas persistentes e atender comprador na PDM.", OpenShowroom);
                if (OpenStaff != null) Add("Equipe / salarios / melhorias", "Contratar, quitar salarios, demitir e melhorar oficina/divulgacao.", OpenStaff);
                if (OpenRental != null) Add("Pedidos da locadora / dois carros", "Fornecer duas unidades prontas do mesmo modelo ao patio externo e devolver o conjunto.", OpenRental);
                if (OpenRepo != null) Add("Retomada / contratos em atraso", "Recuperar carro inadimplente com Packer/TRFlat e retornar ao estoque.", OpenRepo);
                if (OpenFinanceContracts != null) Add("Financiamentos / parcelas", "Contratos do estoque vendido a prazo; recebimentos no caixa empresarial.", OpenFinanceContracts);
                if (OpenSafe != null) Add("Coleta de valores / carro-forte", "Transferir caixa da empresa ao banco pessoal com coleta do seguranca.", OpenSafe);
                Add("Caixa / aportar / sacar", "Saldo empresarial separado do banco pessoal; transferencias sao um checkpoint unico.", OpenFinance);
                Add("Comprar estoque / fornecedor", "Pedido pago com caixa da empresa; retire no porto antes de vender.", OpenCatalog);
                if (OpenCollection != null) Add("Coleta no porto / transportador", "Retirar um ou dois pedidos pagos com Packer e TRFlat.", OpenCollection);
                if (ClearDisplay != null) Add("Recolher todos os carros expostos", "Recolhe apenas os temporarios expostos e libera suas vagas; estoque preservado.", () => ClearDisplay());
                Add("Estoque / entregas / reparar", "Escolha um carro pago para entregar ao cliente. Veiculos da empresa nao sao carros pessoais.", OpenStock);
                Add("Resultado: $" + (d.Revenue - d.SoldCost - d.Repairs - d.Staff.Paid - d.Staff.HiringSpent - d.Staff.UpgradeSpent), "Receita: $" + d.Revenue + " / custo dos vendidos: $" + d.SoldCost + " / reparos: $" + d.Repairs + " / vendidos: " + d.Sold, OpenLedger);
            }
            if (Active) Add("Encerrar entrega", "Pare e saia do carro da empresa. Pedido retorna ao estoque com os danos, sem venda ou pagamento.", () => RequestCancel("Entrega cancelada"));
            if (d.Active != null && !Active) Add("Recuperar entrega interrompida", "Se o carro anterior ainda existir, saia dele e deixe-o parado antes de devolver o pedido ao estoque.", () => { Recover(); OpenMenu(); });
            menu.Visible = true;
        }
        void OpenFinance()
        {
            pool.HideAll(); menu.Clear(); var d = Wallet.Pdm; menu.Name = "Caixa $" + d.Cash + " / banco $" + Wallet.Bank;
            foreach (int amount in new[] { 1000, 10000, 50000 }) { int value = amount; Add("Aportar $" + value, "Transfere do banco pessoal para o caixa.", () => Transfer(value, true)); }
            foreach (int amount in new[] { 1000, 10000 }) { int value = amount; Add("Sacar $" + value, "Transfere do caixa para seu banco.", () => Transfer(value, false)); }
            Add("Sacar saldo (ate $1000000)", "Nao duplica nem paga receita novamente; apenas transfere o saldo ja existente.", () => Transfer(Math.Min(1000000, Wallet.Pdm.Cash), false)); Add("Voltar", "Empresa.", OpenMenu); menu.Visible = true;
        }
        bool RentalWithdrawalAction()
        {
            SyncStaff?.Invoke(); SyncFinance?.Invoke();
            if (!SafeFoot || Game.IsMissionActive || Game.Player.Wanted.WantedLevel > 0 || Game.Player.Character.Position.DistanceTo(StartPosition) > 5 || Active || Wallet.Pdm.Active != null || Wallet.Pdm.Collection != null || Wallet.Pdm.Repo != null || Wallet.Pdm.SafeTransfer != null || Wallet.Pdm.Rental?.Delivered != true)
            { Notify.Show("Para liberar limite do caixa, va a pe ao escritorio apos a entrega aceita pela locadora."); return false; }
            return true;
        }
        void Transfer(int amount, bool deposit) { if (!( !deposit && Wallet.Pdm.Rental?.Delivered == true ? RentalWithdrawalAction() : OfficeAction())) return; if (!Wallet.PdmTransfer(amount, deposit, Guid.NewGuid().ToString("N"), out _)) Notify.Show("Saldo, valor ou limite de caixa/banco insuficiente."); OpenFinance(); }
        void OpenCatalog()
        {
            pool.HideAll(); menu.Clear(); menu.Name = "Fornecedor / " + Wallet.Pdm.Stock.Count + "/" + options.Capacity;
            foreach (var p in options.Catalog) { var offer = p; Add(p.Name + " / custo $" + p.Cost, "Preco de venda sem danos: $" + p.Price + ". Compra com caixa empresarial; pedido aguardara coleta no porto.", () => {
                if (!OfficeAction()) return; var model = new Model(offer.Model); if (!model.IsValid || !model.IsInCdImage || !model.IsCar) { Notify.Show("Modelo indisponivel. Nenhuma compra confirmada."); return; }
                if (!Wallet.PdmOrder(options, offer.Id, Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), out _)) Notify.Show("Caixa/capacidade insuficiente ou entrega pendente."); OpenCatalog();
            }); } Add("Voltar", "Empresa.", OpenMenu); menu.Visible = true;
        }
        void OpenStock()
        {
            pool.HideAll(); menu.Clear(); menu.Name = "Estoque / " + Wallet.Pdm.Stock.Count + " carros";
            foreach (var s in Wallet.Pdm.Stock) { var stock = s; Add((Wallet.Pdm.Showroom.Any(e => e.StockId == s.Id) ? "[EXPOSTO] " : Wallet.Pdm.Rental?.Cars.Any(c => c.StockId == s.Id) == true ? "[LOCADORA] " : s.AwaitingCollection ? "[PORTO] " : "[PRONTO] ") + s.Name + " [" + s.Plate + "]", "Motor " + s.Engine + "/1000 / carroceria " + s.Body + "/1000 / venda estimada $" + PdmStaffTransactions.Quote(Wallet.Pdm, s, s.Engine, s.Body), () => {
                pool.HideAll(); menu.Clear(); menu.Name = stock.Name;
                if (stock.AwaitingCollection) { if (OpenCollection != null) Add("Coletar pedido no porto", "Este carro ja esta pago. Conclua a coleta antes da venda.", OpenCollection); }
                else {
                if (DisplayStock != null) Add("Expor em vaga externa", "Exibe um carro pronto, sem vende-lo ou transforma-lo em carro pessoal.", () => { if (OfficeAction()) DisplayStock(stock.Id); });
                int financedTotal = PdmFinanceTransactions.Quote(Wallet.Pdm, Financing, stock, stock.Engine, stock.Body);
                Add("Entregar financiado / entrada estimada $" + PdmFinanceTransactions.Entry(Financing, financedTotal), "Total estimado $" + financedTotal + "; entrada " + Financing.DownPercent + "%, saldo em " + Financing.Installments + " parcelas a cada " + Financing.PeriodSeconds + "s ativos. Risco por parcela: " + Repossession.DefaultPercent + "%. Condicoes finais na passagem das chaves.", () => Begin(stock.Id, true));
                Add("Entregar ao cliente", "Retire o carro da empresa, dirija ao destino, pare, saia e entregue as chaves com E.", () => Begin(stock.Id));
                }
                int repairQuote = PdmStaffTransactions.RepairPrice(Wallet.Pdm, options.RepairPrice);
                Add("Reparar no estoque: $" + repairQuote, "Debita caixa e restaura motor/carroceria do item empresarial; nao altera carros pessoais.", () => { if (!OfficeAction()) return; if (repairQuote != PdmStaffTransactions.RepairPrice(Wallet.Pdm, options.RepairPrice)) { Notify.Show("Beneficio/custo de reparo mudou. Confira o preco atualizado."); OpenStock(); return; } if (PrepareStock != null ? !PrepareStock(stock.Id) : ClearDisplay?.Invoke() == false) return; if (!Wallet.PdmRepair(options, stock.Id, Guid.NewGuid().ToString("N"), out _)) Notify.Show("Sem caixa, sem danos ou entrega ativa."); OpenStock(); });
                Add("Voltar", "Estoque.", OpenStock); menu.Visible = true;
            }); } Add("Voltar", "Empresa.", OpenMenu); menu.Visible = true;
        }
        void OpenLedger() { pool.HideAll(); menu.Clear(); menu.Name = "Movimentacoes da empresa"; foreach (var e in Wallet.Pdm.Ledger.Where(e => e != null).Reverse()) Add(e.Description + " / " + e.Amount, "Caixa apos operacao: $" + e.Balance, () => { }); Add("Voltar", "Empresa.", OpenMenu); menu.Visible = true; }
        void Begin(string stockId, bool financed = false)
        {
            if (!OfficeAction() || (PrepareStock != null ? !PrepareStock(stockId) : ClearDisplay?.Invoke() == false)) return; var stock = Wallet.Pdm.Stock.Find(s => s.Id == stockId); if (stock == null || stock.AwaitingCollection || stock.Engine < 300 || stock.Body < 300) { Notify.Show("Repare o carro no estoque antes da entrega."); return; }
            if (financed && Wallet.Pdm.Loans.Count(l => !l.Settled) >= Financing.MaxActive) { Notify.Show("Limite de financiamentos ativos. Aguarde quitacao antes de outro contrato."); return; }
            if (!gate.Acquire(Name)) { Notify.Show("Encerre a atividade de " + gate.Owner + " antes de entregar."); return; }
            bool ready = false; var model = new Model(stock.Model);
            try
            {
                if (!model.IsValid || !model.IsInCdImage || !model.IsCar) { Notify.Show("Modelo indisponivel; estoque preservado."); return; }
                var bay = RigSpawn.FindVehicle(options.Spawn, model, "PDM"); if (bay == null) { Notify.Show("Sem vaga livre para retirada. Estoque e caixa preservados."); return; }
                car = World.CreateVehicle(model, Pos(bay), bay.Heading); if (car == null || !car.Exists()) return;
                car.IsPersistent = true; car.EngineHealth = stock.Engine; car.BodyHealth = stock.Body; Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, car, stock.Plate); JobVehicles.Reserve(car);
                int selected = random.Next(options.Customers.Count); string id = Guid.NewGuid().ToString("N");
                if (!(financed ? Wallet.PdmBeginFinanced(options, stockId, id, selected, car.Handle, out _) : Wallet.PdmBegin(options, stockId, id, selected, car.Handle, out _))) { Notify.Show("Entrega nao confirmada. Estoque preservado."); return; }
                run = new PdmRun(id, stockId, options.HandoverSeconds); ownerProfile = Profile.Id; destination = options.Customers[selected]; cancelReason = null; cancelEngine = cancelBody = -1; began = nextSave = nextBuyer = Game.GameTime;
                carBlip = World.CreateBlip(car.Position); carBlip.Name = "RPCore: carro da empresa"; carBlip.Color = BlipColor.Blue;
                targetBlip = World.CreateBlip(Pos(destination)); targetBlip.Name = "RPCore: cliente PDM"; targetBlip.Color = BlipColor.Yellow; targetBlip.ShowRoute = true;
                menu.Visible = false; ready = true; Notify.Show("Retire o carro azul da empresa e leve ao cliente amarelo. Ao chegar, pare, saia e entregue as chaves.");
            }
            finally { model.MarkAsNoLongerNeeded(); if (!ready) { CleanupWorld(); run = null; gate.Release(Name); Recover(); } }
        }
        bool SpawnBuyer()
        {
            var p = JobActors.Sidewalk(Pos(destination)); if (p == Vector3.Zero) { var road = JobActors.Road(destination); if (road != Vector3.Zero) p = JobActors.Sidewalk(road); }
            if (p == Vector3.Zero || !JobActors.Free(p, 2)) return false;
            buyer = JobActors.Create(p, random); if (buyer == null || !buyer.Exists()) return false;
            if (targetBlip != null && targetBlip.Exists()) targetBlip.Position = buyer.Position; return true;
        }
        bool HandoverEligible => InputAllowed && SafeFoot && Game.Player.Character.Weapons.Current.Hash == WeaponHash.Unarmed && car != null && car.Exists() && car.Occupants.Length == 0 && car.Speed < .5f && buyer != null && buyer.Exists() && !buyer.IsDead && car.Position.DistanceTo(buyer.Position) < 10 && Game.Player.Character.Position.DistanceTo(buyer.Position) < 3;
        public void Tick()
        {
            if (!Active) { if (InputAllowed && SafeFoot && Game.Player.Character.Position.DistanceTo(StartPosition) < 3) { Prompt.Help("~INPUT_CONTEXT~: PDM / empresa / caixa / estoque"); if (Game.IsControlJustPressed(GTA.Control.Context)) OpenMenu(); } return; }
            if (run.Stage == PdmStage.Complete) { Depart(); return; }
            if (cancelReason != null) { if (unchecked(Game.GameTime - nextSave) >= 0) CompleteCancel(); return; }
            if (Game.Player.Character.IsDead || Game.IsMissionActive || Game.Player.Wanted.WantedLevel > 0 || car == null || !car.Exists() || car.IsDead || car.EngineHealth < 100 || (buyer != null && (!buyer.Exists() || buyer.IsDead)) || unchecked(Game.GameTime - began) > options.TimeLimitSeconds * 1000)
            { RequestCancel("Entrega interrompida"); return; }
            if (Game.IsPaused) { StopPose(); run.Commit(Game.GameTime, false, () => false); return; }
            if (carBlip != null && carBlip.Exists()) carBlip.Position = car.Position;
            run.Enter(Driver);
            if (run.Stage == PdmStage.Collect) { Status("entre no carro azul da empresa"); return; }
            if (buyer == null && Game.Player.Character.Position.DistanceTo(Pos(destination)) < 160 && unchecked(Game.GameTime - nextBuyer) >= 0) { nextBuyer = Game.GameTime + 5000; SpawnBuyer(); }
            if (run.Stage == PdmStage.Drive)
            {
                Status(buyer == null ? "siga o GPS amarelo ate o cliente" : "pare perto do cliente / saia e E entrega as chaves");
                if (HandoverEligible) { Prompt.Help("~INPUT_CONTEXT~: passar as chaves"); if (Game.IsControlJustPressed(GTA.Control.Context)) { run.Handover(Game.GameTime, true); Function.Call(Hash.REQUEST_ANIM_DICT, AnimDict); animRequested = true; } } return;
            }
            if (!HandoverEligible) { StopPose(); run.Commit(Game.GameTime, false, () => false); return; }
            if (!animStarted && Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, AnimDict)) { animOwner = Game.Player.Character; Function.Call(Hash.TASK_PLAY_ANIM, animOwner, AnimDict, AnimClip, 4f, -4f, -1, 49, 0f, false, false, false); animStarted = true; }
            Status("entregando chaves / aguarde confirmacao"); if (unchecked(Game.GameTime - nextSave) < 0) return;
            try
            {
                SyncStaff?.Invoke(); SyncFinance?.Invoke();
                bool financed = Wallet.Pdm.Active?.Financed == true;
                var stock = Wallet.Pdm.Stock.Find(s => s.Id == run.StockId);
                int quoted = financed ? PdmFinanceTransactions.Entry(Financing, PdmFinanceTransactions.Quote(Wallet.Pdm, Financing, stock, Health(car.EngineHealth), Health(car.BodyHealth))) : PdmStaffTransactions.Quote(Wallet.Pdm, stock, Health(car.EngineHealth), Health(car.BodyHealth)); bool changed = false, attempted = false;
                if (run.Commit(Game.GameTime, true, () => { attempted = true; return financed ? Wallet.PdmFinanceOpenRisky(Financing, run.Id, Health(car.EngineHealth), Health(car.BodyHealth), Repossession.DefaultPercent, out changed) : Wallet.PdmSell(run.Id, Health(car.EngineHealth), Health(car.BodyHealth), out changed); }))
                {
                    StopPose(); if (changed) { Notify.Show((financed ? "Entrada: +$" : "Venda: +$") + quoted + " no caixa da empresa." + (financed ? " Acompanhe parcelas em Financiamentos." : " Saque no escritorio.")); Bump("pdm_sales"); Bump("cash_earned", Math.Max(0, quoted - (stock?.Cost ?? quoted))); }
                    departureAt = Game.GameTime; JobActors.Enter(buyer, car, VehicleSeat.Driver);
                }
                else if (run.Stage == PdmStage.Handover) { nextSave = Game.GameTime + (attempted ? 5000 : 500); if (attempted) Notify.Show("Venda pendente: limite de caixa/dados. Pode cancelar, devolver ao estoque e resolver o caixa."); }
            }
            catch (Exception ex) { nextSave = Game.GameTime + 5000; Log.Error("pdm sale checkpoint / customer departure", ex); Notify.Show(run.Stage == PdmStage.Complete ? "Venda salva no caixa; aguardando retirada do carro pelo cliente." : "Venda nao salva; carro/estoque/caixa preservados. Nova tentativa em 5s."); }
        }
        void Depart()
        {
            if (car != null && car.Exists() && buyer != null && buyer.Exists() && car.Driver == buyer)
            {
                Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER, buyer, car, 14f, 786603); JobActors.Retire(buyer, car, true); JobVehicles.Retire(car); buyer = null; car = null; ClearSession(); return;
            }
            if (unchecked(Game.GameTime - departureAt) > 20000) { CleanupWorld(); ClearSession(); }
        }
        void RequestCancel(string reason) { if (!Active) return; if (run.Stage == PdmStage.Complete) { Depart(); return; } cancelReason = reason; StopPose(); nextSave = Game.GameTime; CompleteCancel(); }
        void CompleteCancel()
        {
            if (car != null && car.Exists() && car.Occupants.Length > 0) { Status("cancelamento: pare e saia do carro da empresa"); Prompt.Help("Pare e saia do carro para devolver o pedido ao estoque."); return; }
            try
            {
                int engine = car != null && car.Exists() ? Health(car.EngineHealth) : Math.Max(0, cancelEngine), body = car != null && car.Exists() ? Health(car.BodyHealth) : Math.Max(0, cancelBody);
                cancelEngine = engine; cancelBody = body;
                if (car != null && car.Exists() && car.Speed > .5f) return;
                if (car != null && car.Exists()) { if (!Wallet.PdmRemember(run.Id, engine, body, run.Id + ":cancelhealth", out _)) return; car.Delete(); if (car.Exists()) return; }
                if (!Wallet.PdmCancel(run.Id, engine, body, out _)) { nextSave = Game.GameTime + 5000; Notify.Show("Devolucao pendente. Estoque preservado; confira dados."); return; }
                CleanupWorld(); ClearSession(); Notify.Show("Entrega devolvida ao estoque com danos. Repare antes de tentar novamente, se necessario.");
            }
            catch (Exception ex) { nextSave = Game.GameTime + 5000; Log.Error("pdm cancellation checkpoint", ex); Notify.Show("Devolucao pendente; nova tentativa em 5s."); }
        }
        bool Matches(Vehicle v, PdmStock stock) => v != null && v.Exists() && v.Model.Hash == new Model(stock.Model).Hash && string.Equals(Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, v)?.Trim(), stock.Plate, StringComparison.OrdinalIgnoreCase);
        void Recover()
        {
            try
            {
                var a = Wallet.Pdm.Active; if (a == null || !Wallet.Pdm.Valid) return; var stock = Wallet.Pdm.Stock.Find(s => s.Id == a.StockId); if (stock == null) return;
                var remembered = Entity.FromHandle(a.VehicleHandle) as Vehicle; int engine = stock.Engine, body = stock.Body;
                if (Matches(remembered, stock))
                {
                    JobVehicles.Reserve(remembered); engine = Math.Min(engine, Health(remembered.EngineHealth)); body = Math.Min(body, Health(remembered.BodyHealth));
                    if (remembered.Occupants.Length > 0 || remembered.Speed > .5f) { Notify.Show("Entrega PDM interrompida. Pare, saia do carro da empresa e use Recuperar no menu."); return; }
                    if (!Wallet.PdmRemember(a.Id, engine, body, a.Id + ":recoverhealth", out _)) return; remembered.Delete(); if (remembered.Exists()) return; JobVehicles.Release(remembered);
                }
                if (Wallet.PdmCancel(a.Id, engine, body, out _)) Notify.Show("Entrega PDM interrompida recuperada. Carro mantido no estoque.");
            }
            catch (Exception ex) { Log.Error("pdm recovery", ex); Notify.Show("Entrega PDM pendente preservada. Resolva pelo menu."); }
        }
        void Status(string text) { if (!Visuals.TryJobHeader(Name, "PDM / entrega", Color.White)) Visuals.Text("PDM / entrega", Visuals.Width / 2 - 200, 24, .34f, Color.White); float x = Visuals.Width / 2 - 390; Visuals.Rect(x, 900, 780, 80, Color.FromArgb(210, 8, 12, 18)); Visuals.Text(text, x + 12, 910, .31f, Color.White); Visuals.Text("End (segurar): devolver ao estoque / pagamentos no caixa empresarial", x + 12, 945, .23f, Color.White); }
        void StopPose() { try { if (animStarted && animOwner != null && animOwner.Exists()) Function.Call(Hash.STOP_ANIM_TASK, animOwner, AnimDict, AnimClip, 2f); } finally { animStarted = false; animOwner = null; if (animRequested) { Function.Call(Hash.REMOVE_ANIM_DICT, AnimDict); animRequested = false; } } }
        void ClearSession() { StopPose(); if (carBlip != null && carBlip.Exists()) carBlip.Delete(); carBlip = null; if (targetBlip != null && targetBlip.Exists()) targetBlip.Delete(); targetBlip = null; run = null; cancelReason = null; gate.Release(Name); menu.Visible = false; }
        void CleanupWorld() { StopPose(); if (buyer != null && buyer.Exists()) buyer.Delete(); buyer = null; if (car != null && car.Exists()) { if (car.Occupants.Length == 0 && car.Speed < .5f) { car.Delete(); JobVehicles.Release(car); } else JobVehicles.Retire(car); } car = null; }
        void Bump(string stat, int n = 1) { try { achievements.Bump(stat, n); achievements.Save(); } catch (Exception ex) { Log.Error("pdm achievement after checkpoint", ex); } }
        public void PollCancel() { if (!Active || !InputAllowed || !Game.IsKeyPressed(Keys.End)) { cancelSince = -1; return; } if (cancelSince < 0) cancelSince = Game.GameTime; if (unchecked(Game.GameTime - cancelSince) >= 1000) RequestCancel("Cancelada pelo jogador"); }
        public void OnKey(KeyEventArgs e) { }
        public void Save() { if (Active && run.Stage != PdmStage.Complete && ownerProfile == Profile.Id && car != null && car.Exists()) Wallet.PdmRemember(run.Id, Health(car.EngineHealth), Health(car.BodyHealth), Guid.NewGuid().ToString("N"), out _); }
        public void Reload() { try { CleanupWorld(); } finally { ClearSession(); } Recover(); }
        public void Dispose() { try { Save(); CleanupWorld(); } catch (Exception ex) { Log.Error("pdm dispose", ex); } finally { ClearSession(); if (officeBlip != null && officeBlip.Exists()) officeBlip.Delete(); } }
    }
}
