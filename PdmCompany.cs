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
    public sealed partial class PdmCompany : ISystem, IWorkContract
    {
        readonly PdmMenuView ui;
        public Action OpenCollection, OpenStaff, SyncStaff, OpenSafe, OpenFinanceContracts, SyncFinance, OpenRepo, OpenRental, OpenShowroom, OpenServices, SyncPremises, OpenSecurity, OpenSurveillance, OpenRisk, SyncRisk;
        public Func<System.Collections.Generic.IEnumerable<PdmLocation>> ConnectedLocations;
        public Func<string,bool> PrepareStock;
        public Func<bool> AccountsReady;
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
        bool Driver => car != null && PdmAssets.Valid(car) && Game.Player.Character.IsInVehicle(car) && car.Driver == Game.Player.Character;
        bool InputAllowed => PdmMenuRuntime.Allowed && !pool.AreAnyVisible;
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
            menu = new NativeMenu("PDM / empresa", "CAIXA / ESTOQUE / CLIENTES"); ui=new PdmMenuView(pool,menu,ex=>ui.Problem("Falha no atendimento","Confira caixa, estoque e pedidos antes de repetir a operacao.",ex,OpenMenu)); ui.DefaultRefresh=OpenMenu; pool.Add(menu); Visuals.Register(menu, Name);
            officeBlip = MapBlips.Create(StartPosition, "RPCore: PDM / empresa", BlipSprite.PersonalVehicleCar, BlipColor.Orange, .8f); Recover();
        }
        void Add(string title, string text, Action action) { ui.Add(title, text, () => { try { action(); } catch (Exception ex) { ui.Problem("Falha no atendimento","Confira caixa, estoque e pedidos antes de repetir a operacao.",ex,OpenMenu); } }); }
        internal bool FlushAccounts()
        {
            SyncStaff?.Invoke();SyncFinance?.Invoke();SyncPremises?.Invoke();SyncRisk?.Invoke();
            return AccountsReady==null || AccountsReady();
        }
        internal bool OfficeAction()
        {
            if(!FlushAccounts()){Notify.Show("Atualizando contas pendentes; aguarde e confira novamente.");return false;}
            if (!SafeFoot || Game.IsMissionActive || Game.Player.Wanted.WantedLevel > 0 || Game.Player.Character.Position.DistanceTo(StartPosition) > 5 || Active || Wallet.Pdm.Active != null || Wallet.Pdm.Collection != null || Wallet.Pdm.SafeTransfer != null || Wallet.Pdm.Repo != null || Wallet.Pdm.Rental != null || Wallet.Pdm.ShowroomSale != null || Wallet.Pdm.PremisesService != null)
            { Notify.Show("Va a pe ao escritorio externo PDM, sem procurado/missao/combate. Encerre/recupere a entrega pendente."); return false; } return true;
        }
        bool DataValid() { if (Wallet.Pdm.Valid) return true; Log.Write("Invalid Wallet.json/Pdm; original retained."); Notify.Show("Dados da empresa invalidos. Arquivo preservado."); return false; }
        void Page(string key,string title,Action back=null,Action refresh=null)
        {ui.HideAll();ui.Begin("company:"+key,back,true,refresh??OpenMenu);menu.Name=title;}
        static string Money(decimal value)=>"$"+value.ToString("N0",System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
        public void OpenMenu()
        {
            if(!PdmMenuRuntime.Allowed){Notify.Show("Feche a outra interface e aguarde controle do personagem.");return;}
            if(!DataValid())return;var d=Wallet.Pdm;
            Page("root","PDM / EMPRESA");
            Add("Resumo da empresa",PdmBusinessRules.Pending(d)+". Caixa, estoque, salarios, contratos e resultado operacional.",OpenOverview);
            if(!d.Owned)ui.AddConfirm("Adquirir empresa / "+Money(options.PurchasePrice),"Debita o banco pessoal. Nao inclui capital, estoque ou carros pessoais.",()=>{
                if(OfficeAction()){if(!Wallet.PdmPurchase(options,Guid.NewGuid().ToString("N"),out bool changed))Notify.Show("Banco insuficiente ou empresa ja adquirida.");else if(changed)Bump("pdm_owned");OpenMenu();}
            },Wallet.Bank>=options.PurchasePrice);
            else{
                Add("Financas / caixa / historico","Aportar, sacar, consultar recebiveis e movimentacoes.",OpenFinance);
                Add("Comprar carros / fornecedor","Compra para a empresa, pelo caixa empresarial. Encomende no escritorio e depois colete no porto.",OpenCatalog);
                Add("Estoque / showroom / vendas",d.Stock.Count+"/"+options.Capacity+" unidades; "+d.Stock.Count(v=>v.AwaitingCollection)+" no porto. Carros da empresa, separados dos pessoais.",OpenInventory);
                Add("Equipe / oficina / melhorias","Contratacoes, salarios, manutencao e beneficios.",OpenPeople);
                Add("Seguranca / cameras / seguro","Postos presenciais, monitoramento e protecao do caixa.",OpenProtection);
                Add("Trabalhos / contratos / pendencias",PdmBusinessRules.Pending(d)+". Entregas, locadora, retomada e transporte de valores.",OpenWork);
            }
            Add("Locais e GPS","Escritorio, compra de estoque, retirada, porto, devolucao, showroom, locadora e equipe.",OpenLocations);
            Add("Fechar","Voltar ao jogo.",ui.Close);ui.Show();
            Log.Write("PDM menu shown: "+menu.Items.Count+" rows, profile "+Profile.Id);
        }
        void OpenOverview()
        {
            var d=Wallet.Pdm;if(!DataValid())return;Page("overview","PDM / visao geral",OpenMenu,OpenOverview);
            ui.Add("Caixa empresarial: "+Money(d.Cash),"Banco pessoal: "+Money(Wallet.Bank)+". Os saldos sao separados.",()=>{},false);
            ui.Add("Estoque "+d.Stock.Count+"/"+options.Capacity+" / "+d.Showroom.Count+" expostos","Custo em estoque: "+Money(d.Stock.Sum(v=>(long)v.Cost))+". "+d.Stock.Count(v=>v.AwaitingCollection)+" pedidos aguardando coleta.",()=>{},false);
            ui.Add("Resultado operacional: "+Money(PdmBusinessRules.Result(d)),"Receitas menos custo vendido, reparos, equipe, melhorias e perdas liquidas. Aportes/saques nao sao lucro.",()=>{},false);
            ui.Add("Receitas "+Money(d.Revenue)+" / "+d.Sold+" vendidos","Custo dos vendidos "+Money(d.SoldCost)+"; reparos "+Money(d.Repairs)+".",()=>{},false);
            ui.Add("Salarios/acertos: "+Money(PdmBusinessRules.Wages(d)),d.Staff.Employees.Count+" funcionarios. Salarios pagos: "+Money(d.Staff.Paid)+".",()=>{},false);
            ui.Add("Contratos a receber: "+Money(PdmBusinessRules.Receivable(d)),d.Loans.Count(l=>l.Collecting)+" ativos; "+d.Loans.Count(l=>l.Defaulted && !l.Settled)+" inadimplentes. Nao e saldo disponivel.",()=>{},false);
            ui.Add(PdmBusinessRules.Pending(d),"Recupere uma operacao interrompida no servico correspondente antes de iniciar outra.",()=>{},false);
            Add("Atualizar","Consultar dados atuais.",OpenOverview);Add("Voltar","Empresa.",OpenMenu);ui.Show();
        }
        void OpenInventory()
        {
            Page("inventory","PDM / estoque e fornecedor",OpenMenu,OpenInventory);
            Add("Estoque / detalhes / entrega","Condicao, exposicao, reparo e entrega ao cliente.",OpenStock);
            Add("Fornecedor / encomendar","Revise custo e venda prevista antes de pagar com caixa empresarial.",OpenCatalog);
            if(OpenShowroom!=null)Add("Showroom / atender compradores","Escolher vaga, expor estoque e negociar com o cliente.",OpenShowroom);
            if(OpenCollection!=null)Add("Coleta no porto / selecionar pedidos","Transportar unidades ja pagas para disponibilizar no estoque.",OpenCollection);
            if(ClearDisplay!=null)ui.AddConfirm("Recolher carros expostos","Libera vagas e recolhe apenas veiculos reconhecidos; nao vende nem remove o estoque.",()=>{if(OfficeAction())ClearDisplay();OpenInventory();});
            Add("Voltar","Empresa.",OpenMenu);ui.Show();
        }
        void OpenPeople()
        {
            Page("people","PDM / equipe e melhorias",OpenMenu,OpenPeople);
            if(OpenStaff!=null)Add("Equipe / salarios / melhorias","Contratar, pagar, demitir e consultar acertos.",OpenStaff);
            if(OpenServices!=null)Add("Mecanico / zeladoria","Atendimento presencial nos carros expostos, com salarios em dia.",OpenServices);
            if(OpenSecurity!=null)Add("Segurancas / postos","Efetivo e defesa local.",OpenSecurity);
            Add("Voltar","Empresa.",OpenMenu);ui.Show();
        }
        void OpenProtection()
        {
            Page("protection","PDM / protecao",OpenMenu,OpenProtection);
            if(OpenSecurity!=null)Add("Segurancas / postos","Defesa local e situacao dos salarios.",OpenSecurity);
            if(OpenSurveillance!=null)Add("Cameras / monitoramento","Consultar equipamentos e imagens ao vivo; sair restaura visao/controles.",OpenSurveillance);
            if(OpenRisk!=null)Add("Cofre / apolice / ocorrencias","Protecao do caixa empresarial, premios e historico de perdas.",OpenRisk);
            Add("Voltar","Empresa.",OpenMenu);ui.Show();
        }
        void OpenWork()
        {
            Page("work","PDM / contratos",OpenMenu,OpenWork);var d=Wallet.Pdm;
            ui.Add(PdmBusinessRules.Pending(d),"Uma operacao empresarial por vez. Pagamentos entram no caixa; coleta de valores transfere ao banco.",()=>{},false);
            Add("Entregas a cliente / financiadas","Selecione uma unidade pronta no estoque.",OpenStock);
            if(OpenCollection!=null)Add("Coleta de estoque no porto","Escolher uma ou duas unidades pagas.",OpenCollection);
            if(OpenRental!=null)Add("Locadora / pedido de dois carros","Mesmo modelo, transporte, conferencia e devolucao do conjunto.",OpenRental);
            if(OpenRepo!=null)Add("Retomada / inadimplentes","Recuperar patrimonio proprio; nao paga premio de dinheiro.",OpenRepo);
            if(OpenSafe!=null)Add("Transporte de valores","Reserva caixa, entrega ao seguranca e compensacao no banco.",OpenSafe);
            if(OpenFinanceContracts!=null)Add("Financiamentos / recebiveis","Parcelas, inadimplencia e contratos encerrados.",OpenFinanceContracts);
            if(Active)ui.AddConfirm("Encerrar entrega atual","Pare e saia do carro; retorna ao estoque com danos, sem venda.",()=>RequestCancel("Entrega cancelada"));
            if(d.Active!=null && !Active)ui.AddConfirm("Recuperar entrega interrompida","Encerra a intencao e preserva o estoque. Pare e saia do veiculo anterior.",()=>{Recover();OpenWork();});
            Add("Voltar","Empresa.",OpenMenu);ui.Show();
        }
        void OpenFinance()
        {
            Page("cash","Caixa "+Money(Wallet.Pdm.Cash)+" / banco "+Money(Wallet.Bank),OpenMenu,OpenFinance);
            Add("Aportar valor...","Transferir do banco pessoal para a empresa.",()=>AskTransfer(true));
            Add("Sacar valor...","Transferir do caixa para o banco pessoal.",()=>AskTransfer(false));
            Add("Aportar o maximo / "+Money(PdmBusinessRules.TransferMaximum(Wallet.Pdm,Wallet.Bank,true)),"Limitado pelos saldos, reservas e espaco do destino.",()=>TransferConfirmation(PdmBusinessRules.TransferMaximum(Wallet.Pdm,Wallet.Bank,true),true));
            Add("Sacar o maximo / "+Money(PdmBusinessRules.TransferMaximum(Wallet.Pdm,Wallet.Bank,false)),"Sem limite artificial por operacao; respeita o saldo e o limite numerico do destino.",()=>TransferConfirmation(PdmBusinessRules.TransferMaximum(Wallet.Pdm,Wallet.Bank,false),false));
            if(OpenFinanceContracts!=null)Add("Financiamentos / a receber","Nao confunde recebiveis com caixa disponivel.",OpenFinanceContracts);
            Add("Extrato / movimentacoes","Historico do caixa, salarios, compras e recebimentos.",OpenLedger);
            Add("Resumo financeiro","Receitas, custos, resultado e obrigacoes.",OpenOverview);
            Add("Voltar","Empresa.",OpenMenu);ui.Show();
        }
        bool RentalWithdrawalAction()
        {
            if(!FlushAccounts()){Notify.Show("Atualizando contas pendentes; aguarde.");return false;}
            if(!SafeFoot || Game.IsMissionActive || Game.Player.Wanted.WantedLevel>0 || Game.Player.Character.Position.DistanceTo(StartPosition)>5 || Active || Wallet.Pdm.Active!=null || Wallet.Pdm.Collection!=null || Wallet.Pdm.Repo!=null || Wallet.Pdm.SafeTransfer!=null || Wallet.Pdm.Rental?.Delivered!=true){Notify.Show("Va ao escritorio apos a entrega aceita pela locadora para liberar espaco no caixa.");return false;}return true;
        }
        void AskTransfer(bool deposit)
        {
            if(!(deposit || Wallet.Pdm.Rental?.Delivered!=true?OfficeAction():RentalWithdrawalAction()))return;
            string profile=Profile.Id;int actor=Game.Player.Character.Handle;string text=Game.GetUserInput(WindowTitle.EnterMessage60,"1000",14);
            if(!PdmMenuRuntime.Allowed || Profile.Id!=profile || Game.Player.Character.Handle!=actor)return;
            if(!PhoneBankRules.Amount(text,out int amount)){if(!string.IsNullOrWhiteSpace(text))Notify.Show("Informe um valor inteiro positivo.");return;}TransferConfirmation(amount,deposit);
        }
        void TransferConfirmation(int amount,bool deposit)
        {
            if(amount<1 || amount>PdmBusinessRules.TransferMaximum(Wallet.Pdm,Wallet.Bank,deposit)){Notify.Show("Saldo insuficiente ou limite do destino. Nenhum valor transferido.");OpenFinance();return;}
            ui.Confirm((deposit?"Aportar ":"Sacar ")+Money(amount),"Banco apos: "+Money((long)Wallet.Bank+(deposit?-amount:amount))+". Caixa apos: "+Money((long)Wallet.Pdm.Cash+(deposit?amount:-amount))+".",()=>Transfer(amount,deposit),OpenFinance);
        }
        void Transfer(int amount,bool deposit)
        {
            if(!(!deposit && Wallet.Pdm.Rental?.Delivered==true?RentalWithdrawalAction():OfficeAction()))return;
            if(!Wallet.PdmTransfer(amount,deposit,Guid.NewGuid().ToString("N"),out _))Notify.Show("Transferencia recusada; confira saldos e reservas.");OpenFinance();
        }
        void OpenCatalog()
        {
            Page("catalog","Fornecedor / "+Wallet.Pdm.Stock.Count+"/"+options.Capacity,OpenInventory,OpenCatalog);
            Add("GPS: escritorio / comprar estoque","Encomenda feita aqui no menu, a pe no escritorio; retirada dos pedidos pagos e no porto.",()=>ui.Waypoint(StartPosition,"escritorio / fornecedor PDM"));
            if(OpenCollection!=null)Add("Retirar pedidos / porto","Pedidos pagos nao aparecem so por marcar o GPS. Inicie a retirada no escritorio ou no marcador do porto.",OpenCollection);
            foreach(var p in options.Catalog){var offer=p;ui.AddConfirm(offer.Name+" / custo "+Money(offer.Cost),"Paga pelo CAIXA DA EMPRESA: "+Money(Wallet.Pdm.Cash)+" -> "+Money(Math.Max(0,Wallet.Pdm.Cash-offer.Cost))+". Banco/carteira pessoal nao mudam. Depois: Retirar pedidos > iniciar retirada no escritorio ou marcador do porto.",()=>{
                if(!OfficeAction())return;var model=new Model(offer.Model);if(!model.IsValid || !model.IsInCdImage || !model.IsCar){Notify.Show("Modelo indisponivel. Nada comprado.");return;}
                int before=Wallet.Pdm.Cash;
                if(!Wallet.PdmOrder(options,offer.Id,Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"),out bool changed)){ui.Problem("Encomenda recusada","Confira caixa da empresa, capacidade e operacao pendente: "+PdmBusinessRules.Pending(Wallet.Pdm)+". Nenhum pedido novo.",null,OpenCatalog);return;}
                Notify.Show(changed?"Pedido salvo: "+offer.Name+". Caixa da empresa $"+before+" -> $"+Wallet.Pdm.Cash+". Abra Retirar pedidos e INICIE a retirada no escritorio ou porto.":"Pedido ja registrado; nenhum debito novo.");OpenCatalog();
            },Wallet.Pdm.Cash>=offer.Cost && Wallet.Pdm.Stock.Count<options.Capacity);}
            Add("Voltar","Estoque e fornecedor.",OpenInventory);ui.Show();
        }
        void OpenStock()
        {
            Page("stock","Estoque / "+Wallet.Pdm.Stock.Count+" carros",OpenInventory,OpenStock);
            foreach(var s in Wallet.Pdm.Stock){string id=s.Id;string state=Wallet.Pdm.Showroom.Any(e=>e.StockId==id)?"EXPOSTO":Wallet.Pdm.Rental?.Cars.Any(c=>c.StockId==id)==true?"LOCADORA":s.AwaitingCollection?"PORTO":"PRONTO";
                Add("["+state+"] "+s.Name+" / "+s.Plate,"Motor "+s.Engine+"/1000; carroceria "+s.Body+"/1000; sujeira "+s.Dirt+"/15; venda prevista "+Money(PdmStaffTransactions.Quote(Wallet.Pdm,s,s.Engine,s.Body))+".",()=>OpenStockItem(id));}
            if(Wallet.Pdm.Stock.Count==0)ui.Add("Estoque vazio","Encomende e colete unidades no fornecedor.",()=>{},false);
            Add("Voltar","Estoque e fornecedor.",OpenInventory);ui.Show();
        }
        void OpenStockItem(string id)
        {
            var stock=Wallet.Pdm.Stock.Find(v=>v.Id==id);if(stock==null){OpenStock();return;}
            Page("stock:"+id,stock.Name+" / "+stock.Plate,OpenStock,()=>OpenStockItem(id));
            ui.Add("Custo "+Money(stock.Cost)+" / venda prevista "+Money(PdmStaffTransactions.Quote(Wallet.Pdm,stock,stock.Engine,stock.Body)),"Motor "+stock.Engine+"; carroceria "+stock.Body+"; sujeira "+stock.Dirt+". O valor final da entrega considera a condicao recebida.",()=>{},false);
            if(stock.AwaitingCollection){if(OpenCollection!=null)Add("Retirar encomenda paga pela empresa","Abra a lista e INICIE a retirada; GPS sozinho nao cria o carro. Pode iniciar no escritorio ou marcador do porto. Sem debito novo.",OpenCollection);}
            else{
                if(DisplayStock!=null)ui.AddConfirm("Expor em vaga externa","Continua no estoque; nao vende nem transforma em carro pessoal.",()=>ui.Stream(new[]{stock.Model},()=>{if(OfficeAction())DisplayStock(id);OpenStockItem(id);},()=>OpenStockItem(id)));
                int total=PdmFinanceTransactions.Quote(Wallet.Pdm,Financing,stock,stock.Engine,stock.Body);
                ui.AddConfirm("Entrega financiada / entrada prevista "+Money(PdmFinanceTransactions.Entry(Financing,total)),"Total previsto "+Money(total)+"; "+Financing.DownPercent+"% entrada, "+Financing.Installments+" parcelas a cada "+Financing.PeriodSeconds+"s ativos. Risco por parcela "+Repossession.DefaultPercent+"%.",()=>ui.Stream(new[]{stock.Model},()=>Begin(id,true),()=>OpenStockItem(id)));
                ui.AddConfirm("Entregar ao cliente","Retirar, dirigir ao destino e entregar as chaves. Pagamento so ao concluir.",()=>ui.Stream(new[]{stock.Model},()=>Begin(id),()=>OpenStockItem(id)));
            }
            int repair=PdmStaffTransactions.RepairPrice(Wallet.Pdm,options.RepairPrice);
            ui.AddConfirm("Reparar no estoque / "+Money(repair),"Debita o caixa e restaura motor/carroceria do item empresarial.",()=>{
                if(!OfficeAction())return;if(repair!=PdmStaffTransactions.RepairPrice(Wallet.Pdm,options.RepairPrice)){Notify.Show("Preco atualizado; confira novamente.");OpenStockItem(id);return;}
                if(PrepareStock!=null?!PrepareStock(id):ClearDisplay?.Invoke()==false)return;
                if(!Wallet.PdmRepair(options,id,Guid.NewGuid().ToString("N"),out _))Notify.Show("Reparo recusado; confira danos, caixa e operacao pendente.");OpenStockItem(id);
            },stock.Engine<1000 || stock.Body<1000);
            Add("Voltar","Estoque.",OpenStock);ui.Show();
        }
        void OpenLedger()
        {
            Page("ledger","Movimentacoes / caixa empresarial",OpenFinance,OpenLedger);
            foreach(var e in Wallet.Pdm.Ledger.Where(e=>e!=null).Reverse())ui.Add(e.Description+" / "+Money(e.Amount),"Caixa apos: "+Money(e.Balance),()=>{},false);
            if(Wallet.Pdm.Ledger.Count==0)ui.Add("Sem movimentacoes","Operacoes futuras aparecerao aqui.",()=>{},false);
            Add("Voltar","Financas.",OpenFinance);ui.Show();
        }
        void Begin(string stockId, bool financed = false)
        {
            if (!OfficeAction() || (PrepareStock != null ? !PrepareStock(stockId) : ClearDisplay?.Invoke() == false)) return; var stock = Wallet.Pdm.Stock.Find(s => s.Id == stockId); if (stock == null || stock.AwaitingCollection || stock.Engine < 300 || stock.Body < 300) { Notify.Show("Repare o carro no estoque antes da entrega."); return; }
            if (financed && Wallet.Pdm.Loans.Count(l => !l.Settled) >= Financing.MaxActive) { Notify.Show("Limite de financiamentos ativos. Aguarde quitacao antes de outro contrato."); return; }
            if (!gate.Acquire(Name)) { Notify.Show("Encerre a atividade de " + gate.Owner + " antes de entregar."); return; }
            bool ready = false; var model = new Model(stock.Model);
            try
            {
                if (!model.IsValid || !model.IsInCdImage || !model.IsCar || !PdmStreaming.Ready(model)) { Notify.Show("Modelo ainda indisponivel; estoque preservado."); return; }
                var bay = RigSpawn.FindVehicle(options.Spawn, model, "PDM"); if (bay == null) { Notify.Show("Sem vaga livre para retirada. Estoque e caixa preservados."); return; }
                car = PdmAssets.Track(World.CreateVehicle(model, Pos(bay), bay.Heading)); if (car == null || !PdmAssets.Valid(car)) return;
                car.IsPersistent = true; car.EngineHealth = stock.Engine; car.BodyHealth = stock.Body; car.DirtLevel = stock.Dirt; Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, car, stock.Plate); JobVehicles.Reserve(car);
                int selected = random.Next(options.Customers.Count); string id = Guid.NewGuid().ToString("N");
                if (!(financed ? Wallet.PdmBeginFinanced(options, stockId, id, selected, car.Handle, out _) : Wallet.PdmBegin(options, stockId, id, selected, car.Handle, out _))) { Notify.Show("Entrega nao confirmada. Estoque preservado."); return; }
                run = new PdmRun(id, stockId, options.HandoverSeconds); ownerProfile = Profile.Id; destination = options.Customers[selected]; cancelReason = null; cancelEngine = cancelBody = -1; began = nextSave = nextBuyer = Game.GameTime;
                carBlip = World.CreateBlip(car.Position); carBlip.Name = "RPCore: carro da empresa"; carBlip.Color = BlipColor.Blue;
                targetBlip = World.CreateBlip(Pos(destination)); targetBlip.Name = "RPCore: cliente PDM"; targetBlip.Color = BlipColor.Yellow; targetBlip.ShowRoute = true;
                menu.Visible = false; ready = true; Notify.Show("Retire o carro azul da empresa e leve ao cliente amarelo. Ao chegar, pare, saia e entregue as chaves.");
            }
            finally { PdmStreaming.Release(model); if (!ready) { CleanupWorld(); run = null; gate.Release(Name); Recover(); } }
        }
        bool SpawnBuyer()
        {
            var p = JobActors.Sidewalk(Pos(destination)); if (p == Vector3.Zero) { var road = JobActors.Road(destination); if (road != Vector3.Zero) p = JobActors.Sidewalk(road); }
            if (p == Vector3.Zero || !JobActors.Free(p, 2)) return false;
            buyer = PdmStreaming.Buyer(p, run.Id); if (buyer == null || !PdmAssets.Valid(buyer)) return false;
            if (targetBlip != null && targetBlip.Exists()) targetBlip.Position = buyer.Position; return true;
        }
        bool HandoverEligible => InputAllowed && SafeFoot && Game.Player.Character.Weapons.Current.Hash == WeaponHash.Unarmed && car != null && PdmAssets.Valid(car) && car.Occupants.Length == 0 && car.Speed < .5f && buyer != null && PdmAssets.Valid(buyer) && !buyer.IsDead && car.Position.DistanceTo(buyer.Position) < 10 && Game.Player.Character.Position.DistanceTo(buyer.Position) < 3;
        public void Tick()
        {
            ui.Tick(PdmMenuRuntime.Allowed);
            if (!Active) { if (InputAllowed && SafeFoot && Game.Player.Character.Position.DistanceTo(StartPosition) < 3) { Prompt.Help("~INPUT_CONTEXT~: PDM / empresa / caixa / estoque"); if (Game.IsControlJustPressed(GTA.Control.Context)) OpenMenu(); } return; }
            if (run.Stage == PdmStage.Complete) { Depart(); return; }
            if (cancelReason != null) { if (unchecked(Game.GameTime - nextSave) >= 0) CompleteCancel(); return; }
            if (Game.Player.Character.IsDead || Game.IsMissionActive || Game.Player.Wanted.WantedLevel > 0 || car == null || !PdmAssets.Valid(car) || car.IsDead || car.EngineHealth < 100 || (buyer != null && (!PdmAssets.Valid(buyer) || buyer.IsDead)) || unchecked(Game.GameTime - began) > options.TimeLimitSeconds * 1000)
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
            if (!animStarted && Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, AnimDict)) { animOwner = PdmAssets.Track(Game.Player.Character); Function.Call(Hash.TASK_PLAY_ANIM, animOwner, AnimDict, AnimClip, 4f, -4f, -1, 49, 0f, false, false, false); animStarted = true; }
            Status("entregando chaves / aguarde confirmacao"); if (unchecked(Game.GameTime - nextSave) < 0) return;
            try
            {
                if(!FlushAccounts()){nextSave=Game.GameTime+1000;Status("atualizando contas antes da venda");return;}
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
            if (car != null && PdmAssets.Valid(car) && buyer != null && PdmAssets.Valid(buyer) && car.Driver == buyer)
            {
                Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER, buyer, car, 14f, 786603); JobActors.Retire(buyer, car, true); JobVehicles.Retire(car); buyer = null; car = null; ClearSession(); return;
            }
            if (unchecked(Game.GameTime - departureAt) > 20000) { CleanupWorld(); ClearSession(); }
        }
        void RequestCancel(string reason) { if (!Active) return; if (run.Stage == PdmStage.Complete) { Depart(); return; } cancelReason = reason; StopPose(); nextSave = Game.GameTime; CompleteCancel(); }
        void CompleteCancel()
        {
            if (car != null && PdmAssets.Valid(car) && car.Occupants.Length > 0) { Status("cancelamento: pare e saia do carro da empresa"); Prompt.Help("Pare e saia do carro para devolver o pedido ao estoque."); return; }
            try
            {
                int engine = car != null && PdmAssets.Valid(car) ? Health(car.EngineHealth) : Math.Max(0, cancelEngine), body = car != null && PdmAssets.Valid(car) ? Health(car.BodyHealth) : Math.Max(0, cancelBody);
                cancelEngine = engine; cancelBody = body;
                if (car != null && PdmAssets.Valid(car) && car.Speed > .5f) return;
                if (car != null && PdmAssets.Valid(car)) { if (!Wallet.PdmRemember(run.Id, engine, body, run.Id + ":cancelhealth", out _)) return; car.Delete(); if (PdmAssets.Valid(car)) return; }
                if (!Wallet.PdmCancel(run.Id, engine, body, out _)) { nextSave = Game.GameTime + 5000; Notify.Show("Devolucao pendente. Estoque preservado; confira dados."); return; }
                CleanupWorld(); ClearSession(); Notify.Show("Entrega devolvida ao estoque com danos. Repare antes de tentar novamente, se necessario.");
            }
            catch (Exception ex) { nextSave = Game.GameTime + 5000; Log.Error("pdm cancellation checkpoint", ex); Notify.Show("Devolucao pendente; nova tentativa em 5s."); }
        }
        bool Matches(Vehicle v, PdmStock stock) => v != null && PdmAssets.Valid(v) && v.Model.Hash == new Model(stock.Model).Hash && string.Equals(Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, v)?.Trim(), stock.Plate, StringComparison.OrdinalIgnoreCase);
        void Recover()
        {
            try
            {
                var a = Wallet.Pdm.Active; if (a == null || !Wallet.Pdm.Valid) return; var stock = Wallet.Pdm.Stock.Find(s => s.Id == a.StockId); if (stock == null) return;
                var remembered = Entity.FromHandle(a.VehicleHandle) as Vehicle; int engine = stock.Engine, body = stock.Body;
                if (Matches(remembered, stock))
                {
                    PdmAssets.Track(remembered); JobVehicles.Reserve(remembered); engine = Math.Min(engine, Health(remembered.EngineHealth)); body = Math.Min(body, Health(remembered.BodyHealth));
                    if (remembered.Occupants.Length > 0 || remembered.Speed > .5f) { Notify.Show("Entrega PDM interrompida. Pare, saia do carro da empresa e use Recuperar no menu."); return; }
                    if (!Wallet.PdmRemember(a.Id, engine, body, a.Id + ":recoverhealth", out _)) return; remembered.Delete(); if (PdmAssets.Valid(remembered)) return; JobVehicles.Release(remembered);
                }
                if (Wallet.PdmCancel(a.Id, engine, body, out _)) Notify.Show("Entrega PDM interrompida recuperada. Carro mantido no estoque.");
            }
            catch (Exception ex) { Log.Error("pdm recovery", ex); Notify.Show("Entrega PDM pendente preservada. Resolva pelo menu."); }
        }
        void Status(string text) { if (!Visuals.TryJobHeader(Name, "PDM / entrega", Color.White)) Visuals.Text("PDM / entrega", Visuals.Width / 2 - 200, 24, .34f, Color.White); float x = Visuals.Width / 2 - 390; Visuals.Rect(x, 900, 780, 80, Color.FromArgb(210, 8, 12, 18)); Visuals.Text(text, x + 12, 910, .31f, Color.White); Visuals.Text("End (segurar): devolver ao estoque / pagamentos no caixa empresarial", x + 12, 945, .23f, Color.White); }
        void StopPose() { try { if (animStarted && animOwner != null && PdmAssets.Valid(animOwner)) Function.Call(Hash.STOP_ANIM_TASK, animOwner, AnimDict, AnimClip, 2f); } finally { animStarted = false; animOwner = null; if (animRequested) { Function.Call(Hash.REMOVE_ANIM_DICT, AnimDict); animRequested = false; } } }
        void ClearSession() { StopPose(); if (carBlip != null && carBlip.Exists()) carBlip.Delete(); carBlip = null; if (targetBlip != null && targetBlip.Exists()) targetBlip.Delete(); targetBlip = null; run = null; cancelReason = null; gate.Release(Name); menu.Visible = false; }
        void CleanupWorld() { StopPose(); if (buyer != null && PdmAssets.Valid(buyer)) buyer.Delete(); buyer = null; if (car != null && PdmAssets.Valid(car)) { if (car.Occupants.Length == 0 && car.Speed < .5f) { car.Delete(); JobVehicles.Release(car); } else JobVehicles.Retire(car); } car = null; }
        void Bump(string stat, int n = 1) { try { achievements.Bump(stat, n); achievements.Save(); } catch (Exception ex) { Log.Error("pdm achievement after checkpoint", ex); } }
        public void PollCancel() { if (!Active || !InputAllowed || !Game.IsKeyPressed(Keys.End)) { cancelSince = -1; return; } if (cancelSince < 0) cancelSince = Game.GameTime; if (unchecked(Game.GameTime - cancelSince) >= 1000) RequestCancel("Cancelada pelo jogador"); }
        public void OnKey(KeyEventArgs e) { }
        public void Save() { if (Active && run.Stage != PdmStage.Complete && ownerProfile == Profile.Id && car != null && PdmAssets.Valid(car)) Wallet.PdmRemember(run.Id, Health(car.EngineHealth), Health(car.BodyHealth), Guid.NewGuid().ToString("N"), out _); }
        public void Reload() { ui.Reset(); try { CleanupWorld(); } finally { ClearSession(); } Recover(); }
        public void Dispose() { ui.Reset(); try { Save(); CleanupWorld(); } catch (Exception ex) { Log.Error("pdm dispose", ex); } finally { ClearSession(); if (officeBlip != null && officeBlip.Exists()) officeBlip.Delete(); } }
    }
}
