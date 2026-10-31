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
    public sealed class PdmShowroomSystem : ISystem, IWorkContract
    {
        readonly PdmMenuView ui;
        public Func<bool> SyncAccounts;
        sealed class Display { public string StockId, Model, Plate; public Vehicle Car; }
        public System.Collections.Generic.IEnumerable<PdmLocation> Locations=>options.Slots.Where(s=>s.Enabled).Select(s=>PdmLocation.At("showroom:"+s.Id,"Showroom",s.Name,s.Interior?"Vaga interna: precisa de interior/chao carregados; exposicao iniciada no menu.":"Vaga externa: expor um carro pago do estoque no menu.",s.Point));
        public string Name => "pdmshowroom";
        public string Label => "PDM / showroom e atendimento";
        public bool Active => run != null;
        public Vector3 StartPosition { get; }
        readonly ObjectPool pool; readonly ShiftGate gate; readonly Func<bool> office, clearLegacy; readonly Action back; readonly AchievementSystem achievements;
        readonly PdmShowroomOptions options; readonly NativeMenu menu; readonly Dictionary<string, Display> displays = new Dictionary<string, Display>(); readonly Random random = new Random();
        PdmShowroomRun run;PdmInspection inspection;int nextInspectWalk;bool inspectionPose; Display saleCar; Ped buyer, animPlayer; Blip buyerBlip; string profile, cancelReason; int began, retryAt, nextWorld, departureAt, cancelSince = -1; bool pose, requested, enterIssued;
        const string Anim = "mp_common";
        static Vector3 Pos(JobPoint p) => new Vector3(p.X, p.Y, p.Z);
        static bool Exists(Vehicle v) => v != null && PdmAssets.Valid(v);
        static bool Matches(Display d) => d != null && Exists(d.Car) && d.Car.Model.Hash == new Model(d.Model).Hash && string.Equals(Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, d.Car)?.Trim(), d.Plate, StringComparison.OrdinalIgnoreCase);
        static bool Empty(Display d) => !Matches(d) || d.Car.Occupants.Length == 0 && d.Car.Speed < .5f;
        bool InputAllowed => PdmMenuRuntime.Allowed && !pool.AreAnyVisible;
        bool Foot => Game.Player.Character.IsHuman && !Game.Player.Character.IsDead && !Game.Player.Character.IsInVehicle() && !Game.Player.Character.IsSwimming && !Game.Player.Character.IsRagdoll && !Game.Player.Character.IsInCombat;
        public PdmShowroomSystem(ObjectPool pool, ShiftGate gate, JobPoint point, Func<bool> office, Func<bool> clearLegacy, AchievementSystem achievements, Action back)
        {
            this.pool = pool; this.gate = gate; this.office = office; this.clearLegacy = clearLegacy; this.achievements = achievements; this.back = back; StartPosition = Pos(point);
            var defaults = PdmShowroomDefaults.Load(); options = Store.LoadDef("Jobs/PdmShowroom.json", () => defaults);
            if (options == null || !options.Valid) { Log.Write("Invalid PdmShowroom.json; embedded defaults used, original retained."); options = defaults; }
            menu = new NativeMenu("PDM / showroom", "VAGAS / ESTOQUE EXPOSTO / ATENDIMENTO"); ui=new PdmMenuView(pool,menu,ex=>{ui.Close();Log.Error(Name+" menu dispatch",ex);Notify.Show("Menu PDM interrompido; dados preservados. Tente abrir novamente.");}); ui.DefaultRefresh=OpenMenu; pool.Add(menu); Visuals.Register(menu, "pdm"); profile = Profile.Id; RecoverIntent();
        }
        void Add(string title, string text, Action action)
        { ui.Add(title, text, () => { try { action(); } catch (Exception ex) { ui.Close(); Log.Error("pdm showroom action", ex); Notify.Show("Showroom: operacao nao confirmada. Dados preservados."); } }); }
        public void OpenMenu()
        {
            ui.HideAll(); ui.Begin(Name,back,true,OpenMenu);menu.Name="Showroom / "+Wallet.Pdm.Showroom.Count+" vagas ocupadas"; if (!Wallet.Pdm.Valid) { Notify.Show("Dados PDM invalidos; original preservado."); return; }
            Add("GPS: escritorio / showroom PDM", "Gerencie vagas a pe no escritorio externo. Vagas internas precisam de interior/chao carregados na sua instalacao.", () => ui.Waypoint(StartPosition,"escritorio PDM"));
            foreach (var s in options.Slots)
            {
                var slot = s; var entry = Wallet.Pdm.Showroom.Find(e => e.SlotId == slot.Id); var stock = entry == null ? null : Wallet.Pdm.Stock.Find(v => v.Id == entry.StockId);
                Add(slot.Name + (slot.Enabled ? "" : " [DESATIVADA]") + (stock == null ? " / livre" : " / " + stock.Name), "Posicao fixa, sem mover carros do ambiente. " + (slot.Interior ? "Salao: acesso/portas conforme recursos instalados." : "Patio externo."), () => OpenSlot(slot.Id));
            }
            ui.AddConfirm("Recolher todos os carros expostos", "Remove apenas temporarios reconhecidos e libera as atribuicoes; estoque/dinheiro permanecem.", () => { if (office()) ClearAll(); OpenMenu(); });
            if (Active) ui.AddConfirm("Cancelar atendimento", "Antes do pagamento: cliente sai, carro continua no estoque e na vaga.", () => { cancelReason = "Cancelado"; menu.Visible = false; });
            if (!Active && Wallet.Pdm.ShowroomSale != null) ui.AddConfirm("Recuperar atendimento interrompido", "Limpa apenas a intencao de venda anterior, sem cobrar ou dar baixa no bem.", () => { RecoverIntent(); OpenMenu(); });
            Add("Voltar", "Empresa PDM.", back); ui.Show();
        }
        void OpenSlot(string slotId)
        {
            ui.HideAll(); ui.Begin(Name+":"+slotId,OpenMenu,true,()=>OpenSlot(slotId)); var slot = options.Slots.Find(s => s.Id == slotId); var entry = Wallet.Pdm.Showroom.Find(e => e.SlotId == slotId); menu.Name = slot.Name;
            if (entry != null)
            {
                var id = entry.StockId; var stock = Wallet.Pdm.Stock.Find(s => s.Id == id);
                ui.AddConfirm("Atender comprador / $" + PdmStaffTransactions.Quote(Wallet.Pdm, stock, stock.Engine, stock.Body), "Cliente aguarda no escritorio externo. E passa as chaves; venda usa o proprio carro exposto. Preco fixado ao iniciar atendimento.", () => ui.Stream(new[]{PdmStreaming.BuyerModel(id)},()=>BeginSale(id),()=>OpenSlot(slotId)));
                ui.AddConfirm("Recolher " + stock.Name, "Mantem bem no estoque. Veiculo ocupado/andando precisa ficar livre/parado.", () => { if (office()) Prepare(id); OpenSlot(slotId); });
            }
            else foreach (var s in Wallet.Pdm.Stock.Where(s => !s.AwaitingCollection && s.Engine >= 300 && s.Body >= 300 && !Wallet.Pdm.Showroom.Any(e => e.StockId == s.Id)))
            { var stock = s; ui.AddConfirm("Expor " + stock.Name + " / " + stock.Plate, "Carro pago do estoque, sem compra/pagamento. Se vaga bloqueada/interior indisponivel, nao confirma atribuicao.", () => { if (office()) Assign(stock.Id, slotId); OpenSlot(slotId); }); }
            Add("Voltar", "Vagas.", OpenMenu); ui.Show();
        }
        Display Find(string id)
        {
            Display d; if (displays.TryGetValue(id, out d) && Matches(d)) return d;
            var entry = Wallet.Pdm.Showroom.Find(e => e.StockId == id); var stock = Wallet.Pdm.Stock.Find(s => s.Id == id);
            if (entry == null || stock == null || entry.Handle <= 0) return null;
            var v = Entity.FromHandle(entry.Handle) as Vehicle; d = new Display { StockId = id, Model = stock.Model, Plate = stock.Plate, Car = v };
            if (!Matches(d)) return null; PdmAssets.Track(v); displays[id] = d; JobVehicles.Reserve(v); return d;
        }
        Display Spawn(PdmStock stock, PdmShowroomSlot slot,bool background=false)
        {
            if (!slot.Enabled) return null; var model = new Model(stock.Model);
            try
            {
                if (!model.IsValid || !model.IsInCdImage || !model.IsCar || !PdmStreaming.Ready(model)) return null;
                if(background && !PdmStreaming.ClaimSpawn())return null;
                var point = RigSpawn.FindDisplayVehicle(slot.Point, model, slot.Interior); if (point == null) return null;
                var v = PdmAssets.Track(World.CreateVehicle(model, Pos(point), point.Heading)); if (!Exists(v)) return null;
                v.IsPersistent = true; v.IsStolen = false; v.NeedsToBeHotwired = false; v.EngineHealth = stock.Engine; v.BodyHealth = stock.Body;
                Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, v, stock.Plate); v.DirtLevel = stock.Dirt; v.IsPositionFrozen = true; v.IsInvincible = true; Function.Call(Hash.SET_VEHICLE_DOORS_LOCKED, v, 2); JobVehicles.Reserve(v);
                return new Display { StockId = stock.Id, Model = stock.Model, Plate = stock.Plate, Car = v };
            }
            finally { PdmStreaming.Release(model); }
        }
        static bool Delete(Display d)
        {
            if (!Matches(d)) return true; if (!Empty(d)) return false;
            d.Car.IsPositionFrozen = false; d.Car.IsInvincible = false; d.Car.Delete(); if (PdmAssets.Valid(d.Car)) return false; JobVehicles.Release(d.Car); return true;
        }
        bool Assign(string stockId, string slotId, bool quiet = false)
        {
            if (clearLegacy?.Invoke() == false) return false; var stock = Wallet.Pdm.Stock.Find(s => s.Id == stockId); var slot = options.Slots.Find(s => s.Id == slotId);
            if (!Wallet.Pdm.Valid || !PdmShowroomTransactions.Idle(Wallet.Pdm) || stock == null || stock.AwaitingCollection || stock.Engine < 300 || stock.Body < 300 || slot == null || Wallet.Pdm.Showroom.Any(e => e.SlotId == slotId || e.StockId == stockId)) return false;
            var d = Spawn(stock, slot); if (d == null) { if (!quiet) Notify.Show("Vaga indisponivel/ocupada ou interior nao carregado. Estoque preservado."); return false; }
            bool saved = false;
            try { if (!Wallet.PdmShowroomAssign(options, stockId, slotId, d.Car.Handle, out _)) return false; displays[stockId] = d; saved = true; Notify.Show(stock.Name + " exposto em " + slot.Name + ". Continua pertencendo ao estoque."); return true; }
            finally { if (!saved) Delete(d); }
        }
        public bool DisplayDefault(string stockId)
        {
            if (Wallet.Pdm.Showroom.Any(e => e.StockId == stockId)) { Notify.Show("Este carro ja tem vaga. Gerencie em Showroom / vagas."); return true; }
            var stock = Wallet.Pdm.Stock.Find(s => s.Id == stockId);
            if (stock == null || stock.AwaitingCollection || stock.Engine < 300 || stock.Body < 300) { Notify.Show("Colete/repare o carro antes de expor."); return false; }
            foreach (var slot in options.Slots.Where(s => s.Enabled && !s.Interior && !Wallet.Pdm.Showroom.Any(e => e.SlotId == s.Id))) if (Assign(stockId, slot.Id, true)) return true;
            Notify.Show("Sem vaga externa fisicamente livre. Escolha outra pelo menu Showroom."); return false;
        }
        public bool Prepare(string stockId)
        {
            if (!Wallet.Pdm.Showroom.Any(e => e.StockId == stockId)) return true;
            if (!PdmShowroomTransactions.Idle(Wallet.Pdm)) return false;
            var d = Find(stockId); if (!Delete(d)) { Notify.Show("Pare e saia do carro exposto antes de recolher."); return false; }
            if (!Wallet.PdmShowroomRemove(stockId, out _)) return false; displays.Remove(stockId); return true;
        }
        public bool ClearAll()
        {
            if (clearLegacy?.Invoke() == false) return false;
            if (!PdmShowroomTransactions.Idle(Wallet.Pdm) || Wallet.Pdm.Showroom.Any(e => !Empty(Find(e.StockId)))) { Notify.Show("Encerre o atendimento/contrato e libere os carros expostos."); return false; }
            foreach (var id in Wallet.Pdm.Showroom.Select(e => e.StockId).ToList()) if (!Prepare(id)) return false; return true;
        }
        public Vehicle GetDisplayVehicle(string id) => Find(id)?.Car;
        bool RecoverIntent()
        {
            if (!Wallet.Pdm.Valid || Active) return false; var sale = Wallet.Pdm.ShowroomSale; if (sale == null) return true;
            try { if (!Wallet.PdmShowroomCancel(sale.Id, out _)) return false; Notify.Show("Atendimento interrompido encerrado; carro e vaga preservados sem venda."); return true; }
            catch (Exception ex) { Log.Error("pdm showroom intent recovery", ex); return false; }
        }
        void SyncWorld()
        {
            if (!GameplayContext.BackgroundReady || !PdmMenuRuntime.WorldAllowed || !Wallet.Pdm.Valid || Wallet.Pdm.ShowroomSale != null && !Active) return;
            bool near = Game.Player.Character.Position.DistanceTo(StartPosition) < 180;
            foreach (var entry in Wallet.Pdm.Showroom.ToList())
            {
                if (Active && run.StockId == entry.StockId || Wallet.Pdm.PremisesService?.StockId == entry.StockId) continue;
                var d = Find(entry.StockId);
                if (!near)
                { if (d != null && Delete(d)) { displays.Remove(entry.StockId); if (entry.Handle != 0) Wallet.PdmShowroomTrack(entry.StockId, 0, out _); } continue; }
                if (d != null) { var condition = Wallet.Pdm.Stock.Find(s => s.Id == entry.StockId); if (Empty(d)) { d.Car.EngineHealth = condition.Engine; d.Car.BodyHealth = condition.Body; d.Car.DirtLevel = condition.Dirt; } continue; } var slot = options.Slots.Find(s => s.Id == entry.SlotId); if (slot == null || !slot.Enabled) continue;
                var stock = Wallet.Pdm.Stock.Find(s => s.Id == entry.StockId); var created = Spawn(stock, slot,true); if (created == null) continue;
                bool saved = false;
                try { if (!Wallet.PdmShowroomTrack(stock.Id, created.Car.Handle, out _)) continue; displays[stock.Id] = created; saved = true; }
                finally { if (!saved) Delete(created); }
            }
        }
        void BeginSale(string stockId)
        {
            if (!office() || Active || !PdmShowroomTransactions.Idle(Wallet.Pdm)) return;
            var d = Find(stockId); if (!Matches(d) || !Empty(d)) { Notify.Show("Carro exposto indisponivel/ocupado; aguarde a vaga carregar e libere o veiculo."); return; }
            if (!gate.Acquire(Name)) { Notify.Show("Encerre a atividade de " + gate.Owner + "."); return; }
            bool ready = false;
            try
            {
                Vector3 point = Vector3.Zero;
                foreach (var offset in new[] { new Vector3(4, 0, 0), new Vector3(-4, 0, 0), new Vector3(0, 4, 0), new Vector3(0, -4, 0), Vector3.Zero })
                {
                    var candidate = JobActors.Sidewalk(StartPosition + offset);
                    if (candidate != Vector3.Zero && candidate.DistanceTo(StartPosition) < 12 && JobActors.Free(candidate, 2)) { point = candidate; break; }
                }
                if (point == Vector3.Zero) { Notify.Show("Sem ponto livre para comprador. Afaste-se um pouco da entrada e tente novamente."); return; }
                buyer = PdmStreaming.Buyer(point, stockId); if (buyer == null || !PdmAssets.Valid(buyer)) return; JobActors.Walk(buyer, StartPosition);
                string id = Guid.NewGuid().ToString("N"); if (!Wallet.PdmShowroomBeginNegotiated(stockId, id, out _)) return;
                saleCar = d; run = new PdmShowroomRun(id, stockId, options.HandoverSeconds); profile = Profile.Id; began = retryAt = Game.GameTime; cancelReason = null; enterIssued = false;inspection=new PdmInspection();inspectionPose=false;nextInspectWalk=Game.GameTime;
                buyerBlip = buyer.AddBlip(); buyerBlip.Name = "RPCore: comprador do showroom"; buyerBlip.Color = BlipColor.Yellow;
                menu.Visible = false; ready = true; Notify.Show("Cliente vai inspecionar o carro exposto. Acompanhe, negocie a oferta e depois entregue as chaves.");
            }
            finally { if (!ready) { if (buyer != null && PdmAssets.Valid(buyer)) buyer.Delete(); buyer = null; gate.Release(Name); } }
        }
        bool Eligible => InputAllowed && Foot && Game.Player.Character.Weapons.Current.Hash == WeaponHash.Unarmed && buyer != null && PdmAssets.Valid(buyer) && !buyer.IsDead && !buyer.IsInCombat && buyer.Position.DistanceTo(StartPosition) < 8 && Game.Player.Character.Position.DistanceTo(buyer.Position) < 3 && Matches(saleCar) && Empty(saleCar);
        void SaleTick()
        {
            if (run.Stage == PdmShowroomStage.Complete) { Depart(); return; }
            if (cancelReason != null) { Cancel(); return; }
            if (Game.Player.Character.IsDead || Game.IsMissionActive || Game.Player.Wanted.WantedLevel > 0 || !Matches(saleCar) || saleCar.Car.IsDead || buyer == null || !PdmAssets.Valid(buyer) || buyer.IsDead || unchecked(Game.GameTime - began) > options.BuyerTimeoutSeconds * 1000 || Game.Player.Character.Position.DistanceTo(StartPosition) > 100)
            { cancelReason = "Atendimento interrompido"; Cancel(); return; }
            var negotiation=Wallet.Pdm.ShowroomSale?.Negotiation;
            if(negotiation!=null && !negotiation.Accepted){InspectAndOffer(negotiation);return;}
            Status("preco combinado / a pe e desarmado / E passa as chaves");
            if (run.Stage == PdmShowroomStage.Waiting) { if (Eligible) { Prompt.Help("~INPUT_CONTEXT~: fechar venda do showroom / passar chaves"); if (Game.IsControlJustPressed(GTA.Control.Context)) { run.Handover(Game.GameTime, true); Function.Call(Hash.REQUEST_ANIM_DICT, Anim); requested = true; } } return; }
            if (!Eligible) { StopPose(); run.Commit(Game.GameTime, false, () => false); return; }
            if (!pose && Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, Anim)) { animPlayer = PdmAssets.Track(Game.Player.Character); Play(animPlayer, "givetake1_b"); Play(buyer, "givetake1_a"); pose = true; }
            if (unchecked(Game.GameTime - retryAt) < 0) return;
            if(SyncAccounts!=null && !SyncAccounts()){retryAt=Game.GameTime+1000;return;}
            var sale = Wallet.Pdm.ShowroomSale; var stock = Wallet.Pdm.Stock.Find(s => s.Id == run.StockId); bool changed = false, attempted = false;
            if (run.Commit(Game.GameTime, true, () => { attempted = true; return Wallet.PdmShowroomSell(run.Id, out changed); }))
            {
                departureAt = Game.GameTime; StopPose(); if (changed) { Notify.Show("Showroom: +$" + sale.Price + " no caixa. Carro vendido uma unica vez."); Bump(Math.Max(0, sale.Price - stock.Cost)); } Depart();
            }
            else if (attempted) { retryAt = Game.GameTime + 5000; Notify.Show("Venda pendente: limite de caixa/dados. Pode cancelar atendimento para resolver caixa e tentar novamente."); }
        }
        void InspectAndOffer(PdmNegotiation offer)
        {
            if(offer.Rejected){cancelReason="Contraproposta recusada";Cancel();return;}
            if(!offer.Inspected){
                Status("cliente inspecionando o carro / acompanhe sem entrar no veiculo");
                bool near=buyer.Position.DistanceTo(saleCar.Car.Position)<5;
                if(!near && !Game.IsPaused && unchecked(Game.GameTime-nextInspectWalk)>=0){
                    var point=JobActors.Sidewalk(saleCar.Car.RightPosition);if(point==Vector3.Zero || point.DistanceTo(saleCar.Car.Position)>5)point=saleCar.Car.RightPosition;
                    JobActors.Walk(buyer,point);nextInspectWalk=Game.GameTime+5000;
                }
                if(near && !inspectionPose){Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE,buyer,"WORLD_HUMAN_CLIPBOARD",0,true);inspectionPose=true;}
                bool eligible=InputAllowed && near && Empty(saleCar) && !buyer.IsInCombat && !buyer.IsRagdoll && Game.Player.Character.Position.DistanceTo(buyer.Position)<30;
                if(inspection.Update(Game.GameTime,eligible) && unchecked(Game.GameTime-retryAt)>=0 && !Wallet.PdmShowroomInspect(run.Id,out _))retryAt=Game.GameTime+5000;
                return;
            }
            Status("oferta $"+offer.Offer+" / aproxime-se do comprador e E negocia");
            if(InputAllowed && Foot && Game.Player.Character.Weapons.Current.Hash==WeaponHash.Unarmed && Game.Player.Character.Position.DistanceTo(buyer.Position)<3){
                Prompt.Help("~INPUT_CONTEXT~: conversar sobre a oferta do comprador");if(Game.IsControlJustPressed(GTA.Control.Context))OpenNegotiation(run.Id);
            }
        }
        bool CanNegotiate(string id)=>Active && run.Id==id && Wallet.Pdm.ShowroomSale?.Id==id && PdmMenuRuntime.Allowed && Foot && profile==Profile.Id && PdmAssets.Valid(buyer) && !buyer.IsDead && !buyer.IsInCombat && Matches(saleCar) && Empty(saleCar) && Game.Player.Character.Position.DistanceTo(buyer.Position)<3;
        void OpenNegotiation(string id)
        {
            if(!CanNegotiate(id))return;var offer=Wallet.Pdm.ShowroomSale.Negotiation;if(offer==null || !offer.Inspected || offer.Accepted)return;
            ui.HideAll();ui.Begin(Name+":offer:"+id,ui.Close,false,()=>OpenNegotiation(id));menu.Name="Cliente / oferta $"+offer.Offer;
            ui.Add("Referencia de venda $"+offer.BasePrice,"A oferta considera a condicao e beneficios atuais. O comprador tem um orcamento; ha uma tentativa de contraproposta, com risco de perder o atendimento.",()=>{},false);
            ui.AddConfirm("Aceitar $"+offer.Offer,"Fixa esse preco. O caixa recebe somente depois de passar as chaves.",()=>{
                if(!CanNegotiate(id))return;if(Wallet.PdmShowroomAccept(id,out _))Agreement();else Notify.Show("Oferta mudou; confira novamente.");
            });
            if(!offer.Countered && (long)offer.BasePrice*110/100>offer.Offer)Add("Fazer contraproposta...","Valor inteiro acima da oferta, ate 10% acima da referencia. Se exceder o orcamento do cliente, ele pode recusar e sair.",()=>{
                int actor=Game.Player.Character.Handle;string text=Game.GetUserInput(WindowTitle.EnterMessage60,offer.BasePrice.ToString(),10);
                if(Game.Player.Character.Handle!=actor || !CanNegotiate(id))return;
                if(!PhoneBankRules.Amount(text,out int price) || price<=offer.Offer || price>Math.Min(3000000,(long)offer.BasePrice*110/100)){Notify.Show("Contraproposta fora do intervalo permitido.");return;}
                ui.Confirm("Propor $"+price,"Uma tentativa: o cliente pode encerrar a negociacao. Nenhum pagamento agora.",()=>{
                    if(!CanNegotiate(id))return;if(!Wallet.PdmShowroomCounter(id,price,out _)){Notify.Show("Proposta nao confirmada; confira o atendimento.");return;}
                    if(Wallet.Pdm.ShowroomSale.Negotiation.Accepted)Agreement();else{Notify.Show("Cliente recusou a contraproposta. Carro e caixa preservados.");cancelReason="Contraproposta recusada";ui.Close();}
                },()=>OpenNegotiation(id));
            });
            ui.AddConfirm("Recusar atendimento","Cliente vai embora; carro continua no estoque e na vaga, sem receita.",()=>{cancelReason="Oferta recusada";ui.Close();});
            Add("Voltar","Continuar conversando depois, sem aceitar nada.",ui.Close);ui.Show();
        }
        void Agreement()
        {
            Notify.Show("Preco combinado: $"+Wallet.Pdm.ShowroomSale.Price+". Aguarde o cliente na entrada e E passa as chaves.");
            inspectionPose=false;buyer.Task.ClearAll();JobActors.Walk(buyer,StartPosition);ui.Close();
        }
        void Depart()
        {
            displays.Remove(run.StockId);
            if (Matches(saleCar))
            {
                saleCar.Car.IsPositionFrozen = false; saleCar.Car.IsInvincible = false; Function.Call(Hash.SET_VEHICLE_DOORS_LOCKED, saleCar.Car, 1);
                if (!enterIssued && buyer != null && PdmAssets.Valid(buyer)) { JobActors.Enter(buyer, saleCar.Car, VehicleSeat.Driver); enterIssued = true; }
                if (buyer != null && PdmAssets.Valid(buyer) && saleCar.Car.Driver == buyer)
                { Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER, buyer, saleCar.Car, 10f, 786603); JobActors.Retire(buyer, saleCar.Car, true); JobVehicles.Retire(saleCar.Car); buyer = null; saleCar = null; ClearSale(); return; }
            }
            if (unchecked(Game.GameTime - departureAt) > 20000) { RetireSaleWorld(); ClearSale(); }
        }
        void Cancel()
        {
            StopPose(); if (unchecked(Game.GameTime - retryAt) < 0) return;
            if (!run.Cancel(() => Wallet.PdmShowroomCancel(run.Id, out _))) { retryAt = Game.GameTime + 5000; return; }
            if (buyer != null && PdmAssets.Valid(buyer)) buyer.Delete(); buyer = null; saleCar = null; ClearSale(); Notify.Show("Atendimento cancelado; bem continua na vaga/estoque, sem receita.");
        }
        static void Play(Ped p, string clip) { if (p != null && PdmAssets.Valid(p)) Function.Call(Hash.TASK_PLAY_ANIM, p, Anim, clip, 4f, -4f, -1, 49, 0f, false, false, false); }
        void StopPose()
        {
            if (pose) { if (animPlayer != null && PdmAssets.Valid(animPlayer)) Function.Call(Hash.STOP_ANIM_TASK, animPlayer, Anim, "givetake1_b", 2f); if (buyer != null && PdmAssets.Valid(buyer)) Function.Call(Hash.STOP_ANIM_TASK, buyer, Anim, "givetake1_a", 2f); }
            pose = false; animPlayer = null; if (requested) { Function.Call(Hash.REMOVE_ANIM_DICT, Anim); requested = false; }
        }
        void RetireSaleWorld()
        {
            StopPose(); if (buyer != null && PdmAssets.Valid(buyer)) buyer.Delete(); buyer = null;
            if (saleCar != null && Matches(saleCar)) { saleCar.Car.IsPositionFrozen = false; saleCar.Car.IsInvincible = false; if (Empty(saleCar)) Delete(saleCar); else JobVehicles.Retire(saleCar.Car); } saleCar = null;
        }
        void ClearSale() { try { if (buyerBlip != null && buyerBlip.Exists()) buyerBlip.Delete(); } finally { buyerBlip = null; run = null;inspection=null;inspectionPose=false; cancelReason = null; gate.Release(Name); menu.Visible = false; } }
        void Bump(int profit) { try { achievements.Bump("pdm_sales"); if (profit > 0) achievements.Bump("cash_earned", profit); achievements.Save(); } catch (Exception ex) { Log.Error("showroom achievement after checkpoint", ex); } }
        void PriceCards()
        {
            foreach(var d in displays.Values){
                if(!Matches(d) || Game.Player.Character.Position.DistanceTo(d.Car.Position)>12)continue;var stock=Wallet.Pdm.Stock.Find(v=>v.Id==d.StockId);if(stock==null)continue;
                using(var x=OutputArgument.Init(0UL))using(var y=OutputArgument.Init(0UL)){
                    var p=d.Car.Position+new Vector3(0,0,1.1f);
                    if(!Function.Call<bool>(Hash.GET_SCREEN_COORD_FROM_WORLD_COORD,p.X,p.Y,p.Z,x,y))continue;
                    int price=Wallet.Pdm.ShowroomSale?.StockId==stock.Id?Wallet.Pdm.ShowroomSale.Price:PdmStaffTransactions.Quote(Wallet.Pdm,stock,stock.Engine,stock.Body);
                    Visuals.Text(stock.Name+" / $"+price,x.GetResult<float>()*Visuals.Width-80,y.GetResult<float>()*1080,.23f,Color.White);
                }
            }
        }
        public void Tick()
        {
            ui.Tick(PdmMenuRuntime.Allowed);
            try
            {
                if (Active) SaleTick();
                if (unchecked(Game.GameTime - nextWorld) >= 0) { nextWorld = Game.GameTime + 5000; SyncWorld(); }
                if (Wallet.Pdm.Valid && InputAllowed) PriceCards();
            }
            catch (Exception ex) { retryAt = Game.GameTime + 5000; Log.Error("pdm showroom tick", ex); }
        }
        void ReleaseWorld()
        {
            StopPose(); if (buyer != null && PdmAssets.Valid(buyer)) buyer.Delete(); buyer = null;
            if (run?.Stage == PdmShowroomStage.Complete) { displays.Remove(run.StockId); RetireSaleWorld(); }
            foreach (var d in displays.Values) if (!Delete(d) && Matches(d)) { d.Car.IsPositionFrozen = false; d.Car.IsInvincible = false; JobVehicles.Retire(d.Car); }
            displays.Clear(); saleCar = null; ClearSale();
        }
        void Status(string text) { if (!Visuals.TryJobHeader("pdm", "PDM / comprador", Color.White)) Visuals.Text("PDM / comprador", Visuals.Width / 2 - 200, 24, .34f, Color.White); float x = Visuals.Width / 2 - 390; Visuals.Rect(x, 900, 780, 80, Color.FromArgb(210, 8, 12, 18)); Visuals.Text(text, x + 12, 910, .29f, Color.White); Visuals.Text("End: cancelar antes da venda / estoque e vaga preservados", x + 12, 945, .23f, Color.White); }
        public void PollCancel() { if (!Active || !InputAllowed || !Game.IsKeyPressed(Keys.End)) { cancelSince = -1; return; } if (cancelSince < 0) cancelSince = Game.GameTime; if (unchecked(Game.GameTime - cancelSince) >= 1000) cancelReason = "Cancelado pelo jogador"; }
        public void Save() { }
        public void Reload() { ui.Reset(); ReleaseWorld(); profile = Profile.Id; RecoverIntent(); nextWorld = Game.GameTime; }
        public void OnKey(KeyEventArgs e) { }
        public void Dispose() { ui.Reset(); try { if (Active && profile == Profile.Id && run.Stage != PdmShowroomStage.Complete) Wallet.PdmShowroomCancel(run.Id, out _); } finally { ReleaseWorld(); } }
    }
}
