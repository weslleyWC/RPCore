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
    public sealed class DealerJob : ISystem, IWorkContract
    {
        public string Name => "dealer";
        public string Label => "Trafico / vendas de rua";
        public bool Active => run != null;
        public Vector3 StartPosition => new Vector3(options.Supplier.X, options.Supplier.Y, options.Supplier.Z);
        readonly ObjectPool pool;
        readonly AchievementSystem achievements;
        readonly ShiftGate gate;
        readonly DealerOptions options;
        readonly NativeMenu menu, offerMenu;
        readonly Blip supplierBlip;
        readonly Random random = new Random();
        bool disposed;DealerRun run;GangWarehouseOptions warehouseMode;
        internal DealerOptions Catalog=>disposed?null:options;
        int KnownUnits=>warehouseMode==null?DealerTransactions.KnownUnits(Wallet.Dealer,options):GangWarehouseTransactions.KnownUnits(Wallet.GangWarehouse,territoryGang,territory.Id,options);
        Dictionary<string,int> CurrentStock=>warehouseMode==null?Wallet.Dealer.Stock:GangWarehouseTransactions.Account(Wallet.GangWarehouse,territoryGang,territory.Id)?.Stock??new Dictionary<string,int>();
        GangTurfDefinition territory;Func<GangState> territoryState;string territoryGang,territoryProfile;int territoryRadius,territoryPlayer;
        bool TerritoryValid=>!Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && territory!=null && territoryState!=null && territoryProfile==Profile.Id && territoryPlayer==Game.Player.Character.Handle && GangCrewRules.SameGang(territoryGang,territoryState().Gang) && GangBusinessTransactions.Owns(territoryState(),territory) && Wallet.GangBusiness?.Supply==null && Wallet.GangRaid?.Pending==null && (warehouseMode==null || Wallet.GangWarehouse?.Valid==true) && Game.Player.Character.Position.DistanceTo(new Vector3(territory.Center.X,territory.Center.Y,territory.Center.Z))<=territoryRadius;
        void TerritoryContext(){if(territory==null || !Active)return;var p=Game.Player.Character.Position;WorkContext.ShopDelivery.Set("dealer_territory",new JobPoint{Name="territorial buyer",X=p.X,Y=p.Y,Z=p.Z},5);}
        void ClearTerritory(){WorkContext.ShopDelivery.Clear("dealer_territory");territory=null;territoryState=null;territoryGang=territoryProfile=null;warehouseMode=null;}
        public bool BeginTerritory(Func<GangState> current,GangTurfDefinition area,int radius,GangWarehouseOptions warehouse=null)
        {
            if(warehouse!=null && !warehouse.Valid)return false;
            if(disposed || Active || current==null || radius<20 || radius>100 || !GangBusinessTransactions.Owns(current(),area) || Wallet.GangBusiness?.Supply!=null || Wallet.GangRaid?.Pending!=null || Wallet.GangIllicit!=null && !Wallet.GangIllicit.Valid)return false;
            warehouseMode=warehouse==null?null:CharacterRules.Copy(warehouse);territory=CharacterRules.Copy(area);territoryState=current;territoryGang=current().Gang;territoryRadius=radius;territoryProfile=Profile.Id;territoryPlayer=Game.Player.Character.Handle;
            if(!TerritoryValid){ClearTerritory();return false;}Begin();if(!Active){ClearTerritory();return false;}
            pool.HideAll();TerritoryContext();Notify.Show(warehouseMode!=null?"Vendas do armazem: pacotes saem do estoque local; receita suja fica na area.":"Vendas territoriais: pacotes saem do estoque pessoal.");Notify.Show("Modo territorial: receita vai ao dinheiro sujo da area. F4 > Comercio para processar85% ao caixa.");return true;
        }
        Ped buyer, animationOwner;
        Blip buyerBlip;
        bool animRequested, animStarted;
        int nextBuyer, nextFollow, nextSave, cancelSince = -1, spawnSince;
        const string AnimDict = "mp_common", AnimClip = "givetake1_a";
        bool InputAllowed => !Game.IsPaused && !InterfaceOptions.ModalOpen && !pool.AreAnyVisible;
        bool SafeFoot => Game.Player.Character.IsHuman && !Game.Player.Character.IsDead && !Game.Player.Character.IsInVehicle() && !Game.Player.Character.IsSwimming && !Game.Player.Character.IsFalling && !Game.Player.Character.IsRagdoll && !Game.Player.Character.IsInCombat;
        bool SafeSale => SafeFoot && !Game.IsMissionActive && Game.Player.Wanted.WantedLevel == 0 && Game.Player.Character.Weapons.Current.Hash == WeaponHash.Unarmed;
        bool BuyerNear => buyer != null && buyer.Exists() && !buyer.IsDead && buyer.Position.DistanceTo(Game.Player.Character.Position) < 3;
        string Zone()
        {
            var p = Game.Player.Character.Position; string z = Function.Call<string>(Hash.GET_NAME_OF_ZONE, p.X, p.Y, p.Z);
            return DealerRules.ZoneValid(z) ? z : "UNKNOWN";
        }
        public DealerJob(ObjectPool pool, AchievementSystem achievements, ShiftGate gate)
        {
            this.pool = pool; this.achievements = achievements; this.gate = gate;
            var defaults = DealerDefaults.Load(); options = Store.LoadDef("Jobs/Dealer.json", () => defaults);
            if (options == null || !options.Valid) { Log.Write("Invalid Dealer.json; embedded defaults used, original retained."); options = defaults; }
            menu = new NativeMenu("Trafico", "CONTATO / ESTOQUE / REPUTACAO"); offerMenu = new NativeMenu("Comprador", "PROPOSTA / NEGOCIACAO");
            pool.Add(menu); pool.Add(offerMenu); Visuals.Register(menu, Name); Visuals.Register(offerMenu, Name);
            supplierBlip = MapBlips.Create(StartPosition, "RPCore: contato / estoque", BlipSprite.Safehouse, BlipColor.Orange, .8f);
        }
        void Add(NativeMenu target, string title, string text, Action action)
        {
            var row = new NativeItem(title, text); row.Activated += (s, e) => { try { action(); } catch (Exception ex) { Log.Error("dealer action", ex); Notify.Show("Acao nao confirmada. Veja RPCore.log; pode tentar novamente."); } }; target.Add(row);
        }
        public void OpenMenu()
        {
            pool.HideAll(); menu.Clear(); var d = Wallet.Dealer;
            if (!d.Valid) { Log.Write("Invalid Wallet.json/Dealer; no changes applied."); Notify.Show("Dados de trafico invalidos. Arquivo preservado."); return; }
            string zone = Zone(); menu.Name = "Trafico / " + d.Sales + " vendas / rep " + DealerRules.Reputation(d, zone) + "/100 em " + zone;
            Add(menu, "Destino da receita: "+(territory==null?"banco":"dinheiro sujo / "+territory.Name),"Modo territorial e iniciado pelo F4 na area da sua gangue. Vendas normais mantem pagamento bancario.",()=>{});
            Add(menu, "GPS: contato / estoque", "Contato externo da referencia. Compra por debito bancario.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, StartPosition.X, StartPosition.Y); menu.Visible = false; });
            Add(menu, "Estoque: " + DealerTransactions.Weight(d, options).ToString("0.0") + " / " + options.Capacity + " kg", "Mercadorias desta atividade em Wallet.json/Dealer. Nenhum item pessoal e removido.", () => { });
            foreach (var p in options.Products)
            {
                var product = p; d.Stock.TryGetValue(p.Id, out int held);
                Add(menu, p.Name + " / estoque " + held + " / comprar 5: $" + p.Buy * 5, "Preco normal inicial por pacote: $" + p.Sell + ". Compra so no contato, fora de turno.", () => {
                    if (Active || !SafeSale || Game.Player.Character.Position.DistanceTo(StartPosition) > 5) { Notify.Show("Encerre o turno e va ao contato a pe/desarmado, sem procurado/missao/combate."); return; }
                    if (!Wallet.DealerBuy(options, product.Id, 5, Guid.NewGuid().ToString("N"), out _)) Notify.Show("Saldo bancario, capacidade ou limite de estoque insuficiente.");
                    OpenMenu();
                });
            }
            if (!Active) Add(menu, "Iniciar vendas de rua", options.SalesPerSession + " vendas. Requer pelo menos esse numero de pacotes; NPCs proprios se aproximam na calcada.", Begin);
            else Add(menu, "Encerrar vendas", "Preserva estoque restante, pagamentos e reputacao ja salvos.", () => End("Turno encerrado. Estoque e vendas salvas preservados."));
            Add(menu, "Reputacao por bairro", "Vendas +5, recusa -1, denuncia -3; intervalo 0 a 100. Pode melhorar o preco normal em ate 10%.", () => {
                pool.HideAll(); menu.Clear(); menu.Name = "Reputacao por bairro";
                foreach (var z in Wallet.Dealer.ZoneRep.OrderByDescending(p => p.Value)) Add(menu, z.Key + ": " + z.Value + "/100", "Reputacao persistente deste perfil.", () => { });
                Add(menu, "Voltar", "Trafico.", OpenMenu); menu.Visible = true;
            });
            menu.Visible = true;
        }
        void Begin()
        {
            if (Active) return;
            if (!SafeSale || !Wallet.Dealer.Valid || KnownUnits < options.SalesPerSession) { Notify.Show("Requer estoque para o turno e personagem a pe/desarmado, sem procurado/missao/combate."); return; }
            if (!gate.Acquire(Name)) { Notify.Show("Encerre a atividade de " + gate.Owner + " antes de iniciar."); return; }
            run = new DealerRun(options.SalesPerSession, options.BuyerSeconds, options.HandoverSeconds); nextBuyer = spawnSince = Game.GameTime; nextSave = Game.GameTime; cancelSince = -1; menu.Visible = false;
            Notify.Show("Vendas iniciadas. Aguarde a pe e parado em uma calcada. Comprador laranja se aproxima; E negocia.");
        }
        bool SpawnBuyer()
        {
            var player = Game.Player.Character;
            for (int i = 0; i < 10; i++)
            {
                double angle = random.NextDouble() * Math.PI * 2; float distance = 12 + random.Next(15);
                var origin = player.Position + new Vector3((float)Math.Cos(angle) * distance, (float)Math.Sin(angle) * distance, 0);
                var p = JobActors.Sidewalk(origin);
                if (p == Vector3.Zero || p.DistanceTo(player.Position) < 8 || p.DistanceTo(player.Position) > 40 || !JobActors.Free(p, 2.5f)) continue;
                buyer = JobActors.Create(p, random); if (buyer == null || !buyer.Exists()) continue;
                var available = options.Products.Where(m => CurrentStock.TryGetValue(m.Id, out int n) && n > 0).ToList();
                if (available.Count == 0) { CleanupBuyer(); return false; }
                var product = available[random.Next(available.Count)]; string zone = Zone();
                if (!run.Offer(product.Id, zone, DealerRules.Reputation(Wallet.Dealer, zone), Game.GameTime, random.Next(100), random.Next(100))) { CleanupBuyer(); return false; }
                buyerBlip = World.CreateBlip(p); buyerBlip.Name = "RPCore: comprador"; buyerBlip.Color = BlipColor.Orange; buyerBlip.Scale = .75f;
                nextFollow = Game.GameTime; return true;
            }
            return false;
        }
        void OpenOffer()
        {
            if (run == null || run.Stage != DealerStage.Offer || !BuyerNear || !SafeSale) return;
            pool.HideAll(); offerMenu.Clear(); var p = options.Products.Find(m => m.Id == run.Product);
            int regular = DealerRules.UnitPrice(p, run.Reputation, false), premium = DealerRules.UnitPrice(p, run.Reputation, true);
            offerMenu.Name = "1 pacote / " + p.Name+(warehouseMode!=null?" / armazem":territory==null?" / banco":" / dinheiro sujo");
            Add(offerMenu, "Vender pelo preco normal: $" + regular, "Risco base de recusa/denuncia. Entrega de " + options.HandoverSeconds + " segundos junto do comprador.", () => Choose(false));
            Add(offerMenu, "Pedir mais: $" + premium, "Aumenta o preco em 20%, com maior chance de recusa/denuncia. Reabrir menu nao sorteia novo resultado.", () => Choose(true));
            Add(offerMenu, "Dispensar comprador", "Nao retira estoque nem paga; outro contato em alguns segundos.", () => Next("Comprador dispensado. Estoque preservado.")); offerMenu.Visible = true;
        }
        void Choose(bool premium)
        {
            if (run == null || run.Stage != DealerStage.Offer || !SafeSale || !BuyerNear || run.Expired(Game.GameTime)) { offerMenu.Visible = false; return; }
            DealerOutcome result = DealerOutcome.Sale;
            if (!run.Choose(options, premium, Game.GameTime, outcome => {
                result = outcome; return Wallet.DealerEncounter(outcome, run.Zone, run.Receipt + ":event", out _);
            })) { Notify.Show("Proposta nao confirmada. Dados no limite; tente novamente ou dispense."); return; }
            offerMenu.Visible = false;
            if (result == DealerOutcome.Police)
            {
                // A game encounter can add one star; it never removes existing stars or replaces police AI.
                Game.Player.Wanted.SetWantedLevelNoDrop(Math.Max(1, Game.Player.Wanted.WantedLevel), false);
                Game.Player.Wanted.ApplyWantedLevelChangeNow(false);
                End("O comprador denunciou. Turno encerrado; estoque preservado. Fuja da policia nativa."); return;
            }
            if (result == DealerOutcome.Refused) { Next("Proposta recusada. Nenhum pacote retirado; aguardando outro comprador."); return; }
            Function.Call(Hash.REQUEST_ANIM_DICT, AnimDict); animRequested = true; nextSave = Game.GameTime;
            Notify.Show("Acordo aceito. Fique a pe junto do comprador para entregar.");
        }
        public void Tick()
        {
            if (!Active)
            {
                if (InputAllowed && SafeFoot && Game.Player.Character.Position.DistanceTo(StartPosition) < 3) { Prompt.Help("~INPUT_CONTEXT~: contato / estoque / trafico"); if (Game.IsControlJustPressed(GTA.Control.Context)) OpenMenu(); } return;
            }
            if(territory!=null && !TerritoryValid){End("Venda territorial encerrada: confira posse, filiacao e limite da area. Vendas ja confirmadas conservadas.");return;}
            TerritoryContext();var player = Game.Player.Character;
            if (player.IsDead || !player.IsHuman || player.IsInVehicle() || player.IsSwimming || player.IsRagdoll || player.IsInCombat || Game.IsMissionActive || Game.Player.Wanted.WantedLevel > 0) { End("Turno interrompido. Estoque e vendas confirmadas preservados."); return; }
            if (Game.IsPaused) return;
            if (run.Stage == DealerStage.Waiting)
            {
                Status("aguarde parado em uma calcada");
                if (!InputAllowed || !SafeSale || player.Speed > 1.2f || unchecked(Game.GameTime - nextBuyer) < 0) return;
                if (run.Sold >= run.Total || run.Offers >= 12 || KnownUnits == 0) { End("Turno encerrado. Consulte estoque e vendas salvas."); return; }
                if (!SpawnBuyer())
                {
                    nextBuyer = Game.GameTime + 5000;
                    if (unchecked(Game.GameTime - spawnSince) >= 60000) End("Nao foi possivel encontrar calcada livre. Estoque preservado; tente outro local.");
                }
                return;
            }
            if (buyer == null || !buyer.Exists() || buyer.IsDead || buyer.Position.DistanceTo(player.Position) > 80) { End("Comprador perdido. Sem retirada de estoque desta tentativa."); return; }
            if (buyerBlip != null && buyerBlip.Exists()) buyerBlip.Position = buyer.Position;
            if (run.Expired(Game.GameTime)) { Next("Comprador desistiu por demora. Estoque preservado."); return; }
            if (unchecked(Game.GameTime - nextFollow) >= 0) { nextFollow = Game.GameTime + 2000; if (!BuyerNear) JobActors.Walk(buyer, player.Position); }
            if (run.Stage == DealerStage.Offer)
            {
                Status(BuyerNear ? "E: negociar com comprador" : "aguarde o comprador laranja se aproximar");
                if (InputAllowed && SafeSale && BuyerNear) { Prompt.Help("~INPUT_CONTEXT~: negociar / preco normal ou pedir mais"); if (Game.IsControlJustPressed(GTA.Control.Context)) OpenOffer(); }
            }
            else if (run.Stage == DealerStage.Handover)
            {
                if (!InputAllowed || !SafeSale || !BuyerNear) { Next("Entrega interrompida. Pacote preservado."); return; }
                if (animRequested && !animStarted && Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, AnimDict)) { animationOwner = player; Function.Call(Hash.TASK_PLAY_ANIM, player, AnimDict, AnimClip, 4f, -4f, -1, 49, 0f, false, false, false); animStarted = true; }
                Status("entregando... fique junto do comprador");
                if (unchecked(Game.GameTime - nextSave) < 0) return;
                try
                {
                    if (run.Commit(Game.GameTime, true, Sell))
                    { if (run.Stage == DealerStage.Finished) End(territory==null?"Vendas concluidas / turno finalizado. Pagamentos ja creditados no banco.":"Vendas territoriais concluidas. Receita suja salva na area; F4 > Comercio para processar."); else Next("Venda confirmada. Aguardando outro comprador."); }
                }
                catch (Exception ex) { nextSave = Game.GameTime + 5000; Log.Error("dealer sale checkpoint", ex); Notify.Show("Falha ao salvar venda. Estoque/pagamento nao alterados; nova tentativa em 5s."); }
            }
        }
        bool Sell(bool last)
        {
            var p = options.Products.Find(m => m.Id == run.Product); int unit = DealerRules.UnitPrice(p, run.Reputation, run.Premium);
            bool changed=false;bool accepted;
            if(warehouseMode!=null)accepted=TerritoryValid && Wallet.GangWarehouseSale(territoryState(),territory,options,p.Id,1,run.Zone,run.Premium,unit,last,run.Receipt+":sale",out changed);
            else accepted=territory==null?Wallet.DealerSale(options,p.Id,1,run.Zone,run.Premium,unit,last,run.Receipt+":sale",out changed):TerritoryValid && Wallet.DealerTerritorySale(territoryState(),territory,options,p.Id,1,run.Zone,run.Premium,unit,last,run.Receipt+":sale",out changed);
            if (!accepted) { nextSave = Game.GameTime + 3000; Notify.Show("Venda nao confirmada: estoque/saldo/dados no limite. Pode encerrar com End."); return false; }
            if (changed) { Notify.Show("~g~+$" + unit + (territory==null?" no banco":" sujo no territorio")+"~s~ / um pacote vendido"); Bump("dealer_sales"); if(territory==null)Bump("cash_earned", unit); if (last) Bump("dealer_sessions"); } return true;
        }
        void Status(string text)
        {
            string label = "Trafico " + run.Sold + "/" + run.Total;
            if (!Visuals.TryJobHeader(Name, label, Color.White)) Visuals.Text(label, Visuals.Width / 2 - 200, 24, .34f, Color.White);
            float x = Visuals.Width / 2 - 380;
            Visuals.Rect(x, 900, 760, 90, Color.FromArgb(210, 8, 12, 18));
            Visuals.Text(text, x + 12, 908, .32f, Color.White);
            Visuals.Text("End (segurar): encerrar / estoque e vendas salvas ficam", x + 12, 945, .25f, Color.White);
        }
        void Bump(string stat, int n = 1) { try { achievements.Bump(stat, n); achievements.Save(); } catch (Exception ex) { Log.Error("dealer achievement after checkpoint", ex); } }
        void CleanupBuyer()
        {
            try
            {
                if (animStarted && animationOwner != null && animationOwner.Exists()) Function.Call(Hash.STOP_ANIM_TASK, animationOwner, AnimDict, AnimClip, 2f);
                if (buyer != null && buyer.Exists()) { JobActors.Retire(buyer); buyer = null; }
                if (buyerBlip != null && buyerBlip.Exists()) buyerBlip.Delete();
            }
            finally { buyer = null; buyerBlip = null; animStarted = false; animationOwner = null; if (animRequested) { Function.Call(Hash.REMOVE_ANIM_DICT, AnimDict); animRequested = false; } offerMenu.Visible = false; }
        }
        void Next(string text) { run?.Skip(); CleanupBuyer(); nextBuyer = Game.GameTime + 5000; spawnSince = Game.GameTime; Notify.Show(text); }
        void End(string text) { run?.Cancel(); try { CleanupBuyer(); } finally { run = null;ClearTerritory(); gate.Release(Name); menu.Visible = false; } Notify.Show(text); }
        public void PollCancel() { if (!Active || !InputAllowed || !Game.IsKeyPressed(Keys.End)) { cancelSince = -1; return; } if (cancelSince < 0) cancelSince = Game.GameTime; if (unchecked(Game.GameTime - cancelSince) >= 1000) End("Vendas encerradas. Estoque e pagamentos salvos preservados."); }
        public void OnKey(KeyEventArgs e) { }
        public void Reload() { if (Active) End("Vendas encerradas na troca de perfil."); menu.Visible = offerMenu.Visible = false; }
        public void Save() { }
        public void Dispose() { disposed=true;try { CleanupBuyer(); } finally { run = null;ClearTerritory(); gate.Release(Name); menu.Visible = offerMenu.Visible = false; if (supplierBlip != null && supplierBlip.Exists()) supplierBlip.Delete(); } }
    }
}
