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
        sealed class Display { public string StockId, Model, Plate; public Vehicle Car; }
        public string Name => "pdmshowroom";
        public string Label => "PDM / showroom e atendimento";
        public bool Active => run != null;
        public Vector3 StartPosition { get; }
        readonly ObjectPool pool; readonly ShiftGate gate; readonly Func<bool> office, clearLegacy; readonly Action back; readonly AchievementSystem achievements;
        readonly PdmShowroomOptions options; readonly NativeMenu menu; readonly Dictionary<string, Display> displays = new Dictionary<string, Display>(); readonly Random random = new Random();
        PdmShowroomRun run; Display saleCar; Ped buyer, animPlayer; Blip buyerBlip; string profile, cancelReason; int began, retryAt, nextWorld, departureAt, cancelSince = -1; bool pose, requested, enterIssued;
        const string Anim = "mp_common";
        static Vector3 Pos(JobPoint p) => new Vector3(p.X, p.Y, p.Z);
        static bool Exists(Vehicle v) => v != null && v.Exists();
        static bool Matches(Display d) => d != null && Exists(d.Car) && d.Car.Model.Hash == new Model(d.Model).Hash && string.Equals(Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, d.Car)?.Trim(), d.Plate, StringComparison.OrdinalIgnoreCase);
        static bool Empty(Display d) => !Matches(d) || d.Car.Occupants.Length == 0 && d.Car.Speed < .5f;
        bool InputAllowed => !Game.IsPaused && !InterfaceOptions.ModalOpen && !pool.AreAnyVisible;
        bool Foot => Game.Player.Character.IsHuman && !Game.Player.Character.IsDead && !Game.Player.Character.IsInVehicle() && !Game.Player.Character.IsSwimming && !Game.Player.Character.IsRagdoll && !Game.Player.Character.IsInCombat;
        public PdmShowroomSystem(ObjectPool pool, ShiftGate gate, JobPoint point, Func<bool> office, Func<bool> clearLegacy, AchievementSystem achievements, Action back)
        {
            this.pool = pool; this.gate = gate; this.office = office; this.clearLegacy = clearLegacy; this.achievements = achievements; this.back = back; StartPosition = Pos(point);
            var defaults = PdmShowroomDefaults.Load(); options = Store.LoadDef("Jobs/PdmShowroom.json", () => defaults);
            if (options == null || !options.Valid) { Log.Write("Invalid PdmShowroom.json; embedded defaults used, original retained."); options = defaults; }
            menu = new NativeMenu("PDM / showroom", "VAGAS / ESTOQUE EXPOSTO / ATENDIMENTO"); pool.Add(menu); Visuals.Register(menu, "pdm"); profile = Profile.Id; RecoverIntent();
        }
        void Add(string title, string text, Action action)
        { var item = new NativeItem(title, text); item.Activated += (s, e) => { try { action(); } catch (Exception ex) { Log.Error("pdm showroom action", ex); Notify.Show("Showroom: operacao nao confirmada. Dados preservados."); } }; menu.Add(item); }
        public void OpenMenu()
        {
            pool.HideAll(); menu.Clear(); if (!Wallet.Pdm.Valid) { Notify.Show("Dados PDM invalidos; original preservado."); return; }
            Add("GPS: escritorio / showroom PDM", "Gerencie vagas a pe no escritorio externo. Vagas internas precisam de interior/chao carregados na sua instalacao.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, StartPosition.X, StartPosition.Y); menu.Visible = false; });
            foreach (var s in options.Slots)
            {
                var slot = s; var entry = Wallet.Pdm.Showroom.Find(e => e.SlotId == slot.Id); var stock = entry == null ? null : Wallet.Pdm.Stock.Find(v => v.Id == entry.StockId);
                Add(slot.Name + (slot.Enabled ? "" : " [DESATIVADA]") + (stock == null ? " / livre" : " / " + stock.Name), "Posicao fixa, sem mover carros do ambiente. " + (slot.Interior ? "Salao: acesso/portas conforme recursos instalados." : "Patio externo."), () => OpenSlot(slot.Id));
            }
            Add("Recolher todos os carros expostos", "Remove apenas temporarios reconhecidos e libera as atribuicoes; estoque/dinheiro permanecem.", () => { if (office()) ClearAll(); OpenMenu(); });
            if (Active) Add("Cancelar atendimento", "Antes do pagamento: cliente sai, carro continua no estoque e na vaga.", () => { cancelReason = "Cancelado"; menu.Visible = false; });
            if (!Active && Wallet.Pdm.ShowroomSale != null) Add("Recuperar atendimento interrompido", "Limpa apenas a intencao de venda anterior, sem cobrar ou dar baixa no bem.", () => { RecoverIntent(); OpenMenu(); });
            Add("Voltar", "Empresa PDM.", back); menu.Visible = true;
        }
        void OpenSlot(string slotId)
        {
            pool.HideAll(); menu.Clear(); var slot = options.Slots.Find(s => s.Id == slotId); var entry = Wallet.Pdm.Showroom.Find(e => e.SlotId == slotId); menu.Name = slot.Name;
            if (entry != null)
            {
                var id = entry.StockId; var stock = Wallet.Pdm.Stock.Find(s => s.Id == id);
                Add("Atender comprador / $" + PdmStaffTransactions.Quote(Wallet.Pdm, stock, stock.Engine, stock.Body), "Cliente aguarda no escritorio externo. E passa as chaves; venda usa o proprio carro exposto. Preco fixado ao iniciar atendimento.", () => BeginSale(id));
                Add("Recolher " + stock.Name, "Mantem bem no estoque. Veiculo ocupado/andando precisa ficar livre/parado.", () => { if (office()) Prepare(id); OpenSlot(slotId); });
            }
            else foreach (var s in Wallet.Pdm.Stock.Where(s => !s.AwaitingCollection && s.Engine >= 300 && s.Body >= 300 && !Wallet.Pdm.Showroom.Any(e => e.StockId == s.Id)))
            { var stock = s; Add("Expor " + stock.Name + " / " + stock.Plate, "Carro pago do estoque, sem compra/pagamento. Se vaga bloqueada/interior indisponivel, nao confirma atribuicao.", () => { if (office()) Assign(stock.Id, slotId); OpenSlot(slotId); }); }
            Add("Voltar", "Vagas.", OpenMenu); menu.Visible = true;
        }
        Display Find(string id)
        {
            Display d; if (displays.TryGetValue(id, out d) && Matches(d)) return d;
            var entry = Wallet.Pdm.Showroom.Find(e => e.StockId == id); var stock = Wallet.Pdm.Stock.Find(s => s.Id == id);
            if (entry == null || stock == null || entry.Handle <= 0) return null;
            var v = Entity.FromHandle(entry.Handle) as Vehicle; d = new Display { StockId = id, Model = stock.Model, Plate = stock.Plate, Car = v };
            if (!Matches(d)) return null; displays[id] = d; JobVehicles.Reserve(v); return d;
        }
        Display Spawn(PdmStock stock, PdmShowroomSlot slot)
        {
            if (!slot.Enabled) return null; var model = new Model(stock.Model);
            try
            {
                if (!model.IsValid || !model.IsInCdImage || !model.IsCar) return null;
                var point = RigSpawn.FindDisplayVehicle(slot.Point, model, slot.Interior); if (point == null) return null;
                var v = World.CreateVehicle(model, Pos(point), point.Heading); if (!Exists(v)) return null;
                v.IsPersistent = true; v.IsStolen = false; v.NeedsToBeHotwired = false; v.EngineHealth = stock.Engine; v.BodyHealth = stock.Body;
                Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, v, stock.Plate); v.DirtLevel = stock.Dirt; v.IsPositionFrozen = true; v.IsInvincible = true; Function.Call(Hash.SET_VEHICLE_DOORS_LOCKED, v, 2); JobVehicles.Reserve(v);
                return new Display { StockId = stock.Id, Model = stock.Model, Plate = stock.Plate, Car = v };
            }
            finally { model.MarkAsNoLongerNeeded(); }
        }
        static bool Delete(Display d)
        {
            if (!Matches(d)) return true; if (!Empty(d)) return false;
            d.Car.IsPositionFrozen = false; d.Car.IsInvincible = false; d.Car.Delete(); if (d.Car.Exists()) return false; JobVehicles.Release(d.Car); return true;
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
            if (!Wallet.Pdm.Valid || Wallet.Pdm.ShowroomSale != null && !Active) return;
            bool near = Game.Player.Character.Position.DistanceTo(StartPosition) < 180;
            foreach (var entry in Wallet.Pdm.Showroom.ToList())
            {
                if (Active && run.StockId == entry.StockId || Wallet.Pdm.PremisesService?.StockId == entry.StockId) continue;
                var d = Find(entry.StockId);
                if (!near)
                { if (d != null && Delete(d)) { displays.Remove(entry.StockId); if (entry.Handle != 0) Wallet.PdmShowroomTrack(entry.StockId, 0, out _); } continue; }
                if (d != null) { var condition = Wallet.Pdm.Stock.Find(s => s.Id == entry.StockId); if (Empty(d)) { d.Car.EngineHealth = condition.Engine; d.Car.BodyHealth = condition.Body; d.Car.DirtLevel = condition.Dirt; } continue; } var slot = options.Slots.Find(s => s.Id == entry.SlotId); if (slot == null || !slot.Enabled) continue;
                var stock = Wallet.Pdm.Stock.Find(s => s.Id == entry.StockId); var created = Spawn(stock, slot); if (created == null) continue;
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
                buyer = JobActors.Create(point, random); if (buyer == null || !buyer.Exists()) return; JobActors.Walk(buyer, StartPosition);
                string id = Guid.NewGuid().ToString("N"); if (!Wallet.PdmShowroomBegin(stockId, id, out _)) return;
                saleCar = d; run = new PdmShowroomRun(id, stockId, options.HandoverSeconds); profile = Profile.Id; began = retryAt = Game.GameTime; cancelReason = null; enterIssued = false;
                buyerBlip = buyer.AddBlip(); buyerBlip.Name = "RPCore: comprador do showroom"; buyerBlip.Color = BlipColor.Yellow;
                menu.Visible = false; ready = true; Notify.Show("Comprador aguarda na entrada externa PDM. Aproxime-se a pe/desarmado e E passa as chaves.");
            }
            finally { if (!ready) { if (buyer != null && buyer.Exists()) buyer.Delete(); buyer = null; gate.Release(Name); } }
        }
        bool Eligible => InputAllowed && Foot && Game.Player.Character.Weapons.Current.Hash == WeaponHash.Unarmed && buyer != null && buyer.Exists() && !buyer.IsDead && !buyer.IsInCombat && buyer.Position.DistanceTo(StartPosition) < 8 && Game.Player.Character.Position.DistanceTo(buyer.Position) < 3 && Matches(saleCar) && Empty(saleCar);
        void SaleTick()
        {
            if (run.Stage == PdmShowroomStage.Complete) { Depart(); return; }
            if (cancelReason != null) { Cancel(); return; }
            if (Game.Player.Character.IsDead || Game.IsMissionActive || Game.Player.Wanted.WantedLevel > 0 || !Matches(saleCar) || saleCar.Car.IsDead || buyer == null || !buyer.Exists() || buyer.IsDead || unchecked(Game.GameTime - began) > options.BuyerTimeoutSeconds * 1000 || Game.Player.Character.Position.DistanceTo(StartPosition) > 100)
            { cancelReason = "Atendimento interrompido"; Cancel(); return; }
            Status("comprador na entrada / a pe e desarmado / E passa as chaves");
            if (run.Stage == PdmShowroomStage.Waiting) { if (Eligible) { Prompt.Help("~INPUT_CONTEXT~: fechar venda do showroom / passar chaves"); if (Game.IsControlJustPressed(GTA.Control.Context)) { run.Handover(Game.GameTime, true); Function.Call(Hash.REQUEST_ANIM_DICT, Anim); requested = true; } } return; }
            if (!Eligible) { StopPose(); run.Commit(Game.GameTime, false, () => false); return; }
            if (!pose && Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, Anim)) { animPlayer = Game.Player.Character; Play(animPlayer, "givetake1_b"); Play(buyer, "givetake1_a"); pose = true; }
            if (unchecked(Game.GameTime - retryAt) < 0) return;
            var sale = Wallet.Pdm.ShowroomSale; var stock = Wallet.Pdm.Stock.Find(s => s.Id == run.StockId); bool changed = false, attempted = false;
            if (run.Commit(Game.GameTime, true, () => { attempted = true; return Wallet.PdmShowroomSell(run.Id, out changed); }))
            {
                departureAt = Game.GameTime; StopPose(); if (changed) { Notify.Show("Showroom: +$" + sale.Price + " no caixa. Carro vendido uma unica vez."); Bump(Math.Max(0, sale.Price - stock.Cost)); } Depart();
            }
            else if (attempted) { retryAt = Game.GameTime + 5000; Notify.Show("Venda pendente: limite de caixa/dados. Pode cancelar atendimento para resolver caixa e tentar novamente."); }
        }
        void Depart()
        {
            displays.Remove(run.StockId);
            if (Matches(saleCar))
            {
                saleCar.Car.IsPositionFrozen = false; saleCar.Car.IsInvincible = false; Function.Call(Hash.SET_VEHICLE_DOORS_LOCKED, saleCar.Car, 1);
                if (!enterIssued && buyer != null && buyer.Exists()) { JobActors.Enter(buyer, saleCar.Car, VehicleSeat.Driver); enterIssued = true; }
                if (buyer != null && buyer.Exists() && saleCar.Car.Driver == buyer)
                { Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER, buyer, saleCar.Car, 10f, 786603); JobActors.Retire(buyer, saleCar.Car, true); JobVehicles.Retire(saleCar.Car); buyer = null; saleCar = null; ClearSale(); return; }
            }
            if (unchecked(Game.GameTime - departureAt) > 20000) { RetireSaleWorld(); ClearSale(); }
        }
        void Cancel()
        {
            StopPose(); if (unchecked(Game.GameTime - retryAt) < 0) return;
            if (!run.Cancel(() => Wallet.PdmShowroomCancel(run.Id, out _))) { retryAt = Game.GameTime + 5000; return; }
            if (buyer != null && buyer.Exists()) buyer.Delete(); buyer = null; saleCar = null; ClearSale(); Notify.Show("Atendimento cancelado; bem continua na vaga/estoque, sem receita.");
        }
        static void Play(Ped p, string clip) { if (p != null && p.Exists()) Function.Call(Hash.TASK_PLAY_ANIM, p, Anim, clip, 4f, -4f, -1, 49, 0f, false, false, false); }
        void StopPose()
        {
            if (pose) { if (animPlayer != null && animPlayer.Exists()) Function.Call(Hash.STOP_ANIM_TASK, animPlayer, Anim, "givetake1_b", 2f); if (buyer != null && buyer.Exists()) Function.Call(Hash.STOP_ANIM_TASK, buyer, Anim, "givetake1_a", 2f); }
            pose = false; animPlayer = null; if (requested) { Function.Call(Hash.REMOVE_ANIM_DICT, Anim); requested = false; }
        }
        void RetireSaleWorld()
        {
            StopPose(); if (buyer != null && buyer.Exists()) buyer.Delete(); buyer = null;
            if (saleCar != null && Matches(saleCar)) { saleCar.Car.IsPositionFrozen = false; saleCar.Car.IsInvincible = false; if (Empty(saleCar)) Delete(saleCar); else JobVehicles.Retire(saleCar.Car); } saleCar = null;
        }
        void ClearSale() { try { if (buyerBlip != null && buyerBlip.Exists()) buyerBlip.Delete(); } finally { buyerBlip = null; run = null; cancelReason = null; gate.Release(Name); menu.Visible = false; } }
        void Bump(int profit) { try { achievements.Bump("pdm_sales"); if (profit > 0) achievements.Bump("cash_earned", profit); achievements.Save(); } catch (Exception ex) { Log.Error("showroom achievement after checkpoint", ex); } }
        void PriceCards()
        {
            foreach (var d in displays.Values)
            {
                if (!Matches(d) || Game.Player.Character.Position.DistanceTo(d.Car.Position) > 12) continue; var s = Wallet.Pdm.Stock.Find(x => x.Id == d.StockId); if (s == null) continue;
                var x = new OutputArgument(); var y = new OutputArgument(); var p = d.Car.Position + new Vector3(0, 0, 1.1f);
                if (!Function.Call<bool>(Hash.GET_SCREEN_COORD_FROM_WORLD_COORD, p.X, p.Y, p.Z, x, y)) continue;
                int price = Wallet.Pdm.ShowroomSale?.StockId == s.Id ? Wallet.Pdm.ShowroomSale.Price : PdmStaffTransactions.Quote(Wallet.Pdm, s, s.Engine, s.Body);
                Visuals.Text(s.Name + " / $" + price, x.GetResult<float>() * Visuals.Width - 80, y.GetResult<float>() * 1080, .23f, Color.White);
            }
        }
        public void Tick()
        {
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
            StopPose(); if (buyer != null && buyer.Exists()) buyer.Delete(); buyer = null;
            if (run?.Stage == PdmShowroomStage.Complete) { displays.Remove(run.StockId); RetireSaleWorld(); }
            foreach (var d in displays.Values) if (!Delete(d) && Matches(d)) { d.Car.IsPositionFrozen = false; d.Car.IsInvincible = false; JobVehicles.Retire(d.Car); }
            displays.Clear(); saleCar = null; ClearSale();
        }
        void Status(string text) { if (!Visuals.TryJobHeader("pdm", "PDM / comprador", Color.White)) Visuals.Text("PDM / comprador", Visuals.Width / 2 - 200, 24, .34f, Color.White); float x = Visuals.Width / 2 - 390; Visuals.Rect(x, 900, 780, 80, Color.FromArgb(210, 8, 12, 18)); Visuals.Text(text, x + 12, 910, .29f, Color.White); Visuals.Text("End: cancelar antes da venda / estoque e vaga preservados", x + 12, 945, .23f, Color.White); }
        public void PollCancel() { if (!Active || !InputAllowed || !Game.IsKeyPressed(Keys.End)) { cancelSince = -1; return; } if (cancelSince < 0) cancelSince = Game.GameTime; if (unchecked(Game.GameTime - cancelSince) >= 1000) cancelReason = "Cancelado pelo jogador"; }
        public void Save() { }
        public void Reload() { ReleaseWorld(); profile = Profile.Id; RecoverIntent(); nextWorld = Game.GameTime; }
        public void OnKey(KeyEventArgs e) { }
        public void Dispose() { try { if (Active && profile == Profile.Id && run.Stage != PdmShowroomStage.Complete) Wallet.PdmShowroomCancel(run.Id, out _); } finally { ReleaseWorld(); } }
    }
}
