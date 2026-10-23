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
    public sealed class CraftingSystem : ISystem, IWorkContract
    {
        public string Name => "crafting";
        public string Label => "Bancada / crafting e feira";
        public bool Active => run != null;
        public Vector3 StartPosition => new Vector3(options.Station.X, options.Station.Y, options.Station.Z);
        readonly ObjectPool pool;
        readonly AchievementSystem achievements;
        readonly ShiftGate gate;
        readonly CraftingOptions options;
        readonly NativeMenu menu;
        readonly Blip blip;
        CraftingRun run;
        Ped owner;
        bool animRequested, animStarted;
        int nextSave, cancelSince = -1, animationDeadline;
        const string AnimDict = "mini@repair", AnimClip = "fixing_a_ped";
        bool InputAllowed => !Game.IsPaused && !InterfaceOptions.ModalOpen && !pool.AreAnyVisible;
        bool SafeFoot => Game.Player.Character.IsHuman && !Game.Player.Character.IsDead && !Game.Player.Character.IsInVehicle()
            && !Game.Player.Character.IsSwimming && !Game.Player.Character.IsFalling && !Game.Player.Character.IsRagdoll && !Game.Player.Character.IsInCombat;
        bool AtStation => Game.Player.Character.Position.DistanceTo(StartPosition) < 4;
        public CraftingSystem(ObjectPool pool, AchievementSystem achievements, ShiftGate gate)
        {
            this.pool = pool; this.achievements = achievements; this.gate = gate;
            var defaults = CraftingDefaults.Load(); options = Store.LoadDef("Jobs/Crafting.json", () => defaults);
            if (options == null || !options.Valid) { Log.Write("Invalid Crafting.json; embedded defaults used, original retained."); options = defaults; }
            menu = new NativeMenu("Bancada / feira", "RECEITAS / ESTOQUE / VENDA"); pool.Add(menu); Visuals.Register(menu, Name);
            blip = MapBlips.Create(StartPosition, "RPCore: bancada / crafting / feira", BlipSprite.Store, BlipColor.Orange, .8f);
        }
        void Add(string title, string text, Action action)
        {
            var item = new NativeItem(title, text); item.Activated += (s, e) => { try { action(); } catch (Exception ex) { Log.Error("crafting action", ex); Notify.Show("Acao nao confirmada. Veja RPCore.log."); } }; menu.Add(item);
        }
        bool ValidStock()
        {
            if (Wallet.Crafting.Valid) return true;
            Notify.Show("Estoque de crafting invalido. Arquivo preservado; confira RPCore.log."); Log.Write("Invalid Wallet.json/Crafting; original retained."); return false;
        }
        public void OpenMenu()
        {
            pool.HideAll(); menu.Clear(); if (!ValidStock()) return;
            var d = Wallet.Crafting; menu.Name = "Bancada / " + d.Made + " produtos / " + d.Earned + " vendidos";
            Add("GPS: bancada / feira do pier", "Receitas e venda na area publica do pier Del Perro.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, StartPosition.X, StartPosition.Y); menu.Visible = false; });
            Add("Receitas / fabricar", "Usa materiais das bolsas de caca/pesca e produtos do estoque. Nenhum custo bancario para fabricar.", OpenRecipes);
            Add("Estoque: " + CraftingTransactions.Weight(d, options).ToString("0.0") + " / " + options.Capacity + " kg", "Produtos persistentes desta bancada, separados do inventario pessoal I.", OpenStock);
            if (Active) Add("Cancelar fabricacao", "Ingredientes so sao retirados ao confirmar o produto; cancelar antes nao gasta materiais.", () => End("Fabricacao cancelada. Ingredientes preservados."));
            Add("Como funciona", "Traga materias-primas sem vende-las. Guarde a arma, fabrique na bancada e venda o produto na feira. Kit de campo usa produtos ja fabricados.", () => Notify.Show("Receitas mostram ingredientes e quantidades disponiveis. Fabrique, espere e venda o estoque na feira."));
            menu.Visible = true;
        }
        void OpenRecipes()
        {
            pool.HideAll(); menu.Clear(); if (!ValidStock()) return; menu.Name = "Receitas / bolsas de caca e pesca";
            foreach (var r in options.Recipes)
            {
                var recipe = r; bool can = Wallet.CraftPreview(options, r.Id, 1, out _);
                Add(r.Name + (can ? " / disponivel" : " / faltam materiais"), Ingredients(r) + " Resultado: " + options.Products.Find(p => p.Id == r.Product).Name + " x" + r.Quantity + ".", () => OpenRecipe(recipe));
            }
            Add("Voltar", "Bancada / feira.", OpenMenu); menu.Visible = true;
        }
        string Ingredients(CraftRecipe r) => string.Join(" | ", r.Ingredients.Select(i => i.Name + " " + Wallet.CraftAvailable(i) + "/" + i.Quantity + " [" + (i.Source == "hunting" ? "caca" : i.Source == "fishing" ? "pesca" : "estoque") + "]"));
        void OpenRecipe(CraftRecipe r)
        {
            pool.HideAll(); menu.Clear(); menu.Name = r.Name;
            foreach (var i in r.Ingredients) Add(i.Name + ": " + Wallet.CraftAvailable(i) + " / " + i.Quantity, "Por lote. Origem: " + i.Source + ".", () => { });
            var product = options.Products.Find(p => p.Id == r.Product);
            Add("Resultado: " + product.Name + " x" + r.Quantity, "Venda por unidade: $" + product.Price + ". Peso: " + product.Weight + " kg. Nao e consumivel no inventario I.", () => { });
            Add("Fabricar 1 lote / " + r.Seconds + "s", "Requer estar na bancada, desarmado e a pe. Nao cobra dinheiro.", () => Begin(r, 1));
            Add("Fabricar 5 lotes / " + r.Seconds * 5 + "s", "Exige cinco vezes cada ingrediente e espaco para o resultado inteiro.", () => Begin(r, 5));
            Add("Voltar", "Todas as receitas.", OpenRecipes); menu.Visible = true;
        }
        void OpenStock()
        {
            pool.HideAll(); menu.Clear(); if (!ValidStock()) return; var d = Wallet.Crafting;
            menu.Name = "Estoque / " + CraftingTransactions.Weight(d, options).ToString("0.0") + " kg";
            foreach (var p in d.Stock) if (p.Value > 0) { var def = options.Products.Find(x => x.Id == p.Key); Add((def?.Name ?? p.Key) + " x" + p.Value, def == null ? "Produto desconhecido preservado." : "Venda por unidade: $" + def.Price, () => { }); }
            int quote = CraftingTransactions.Quote(d, options);
            Add("Vender produtos conhecidos: $" + Math.Max(0, quote), "Na feira: retira apenas produtos fabricados e credita o banco no mesmo salvamento.", () => {
                if (!StationAction()) return;
                if (!Wallet.CraftSell(options, Guid.NewGuid().ToString("N"), out bool changed)) { Notify.Show("Sem produtos vendaveis ou limite de saldo/dados."); return; }
                if (changed) { Notify.Show("~g~+$" + quote + " no banco~s~ / produtos da bancada"); Bump("crafting_sales"); Bump("cash_earned", quote); }
                OpenStock();
            });
            Add("Voltar", "Bancada / feira.", OpenMenu); menu.Visible = true;
        }
        bool StationAction()
        {
            if (AtStation && SafeFoot && !Game.IsMissionActive && Game.Player.Wanted.WantedLevel == 0) return true;
            Notify.Show("Va a pe a bancada/feira do pier, sem procurado/missao/combate."); return false;
        }
        void Begin(CraftRecipe r, int count)
        {
            if (Active) { Notify.Show("Uma fabricacao ja esta em andamento."); return; }
            if (!StationAction()) return;
            if (Game.Player.Character.Weapons.Current.Hash != WeaponHash.Unarmed) { Notify.Show("Guarde a arma para fabricar."); return; }
            if (!Wallet.CraftPreview(options, r.Id, count, out string reason)) { Notify.Show(reason); return; }
            if (!gate.Acquire(Name)) { Notify.Show("Encerre a atividade de " + gate.Owner + " antes de fabricar."); return; }
            run = new CraftingRun(r.Id, count, r.Seconds, Game.GameTime); owner = Game.Player.Character;
            nextSave = Game.GameTime; cancelSince = -1; menu.Visible = false;
            Function.Call(Hash.REQUEST_ANIM_DICT, AnimDict); animRequested = true; animationDeadline = Game.GameTime + 2500;
            Notify.Show("Fabricando " + r.Name + " x" + count + ". Fique na bancada; End cancela sem gastar ingredientes.");
        }
        public void Tick()
        {
            if (!Active)
            {
                if (InputAllowed && SafeFoot && AtStation) { Prompt.Help("~INPUT_CONTEXT~: bancada / receitas / feira"); if (Game.IsControlJustPressed(GTA.Control.Context)) OpenMenu(); } return;
            }
            bool eligible = InputAllowed && SafeFoot && AtStation && !Game.IsMissionActive && Game.Player.Wanted.WantedLevel == 0
                && owner != null && owner.Exists() && owner == Game.Player.Character && owner.Weapons.Current.Hash == WeaponHash.Unarmed;
            if (!eligible) { End("Fabricacao interrompida. Ingredientes preservados."); return; }
            Game.DisableControlThisFrame(GTA.Control.Attack); Game.DisableControlThisFrame(GTA.Control.Aim);
            if (animRequested && !animStarted && Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, AnimDict)) { Function.Call(Hash.TASK_PLAY_ANIM, owner, AnimDict, AnimClip, 4f, -4f, -1, 49, 0f, false, false, false); animStarted = true; }
            else if (animRequested && !animStarted && unchecked(Game.GameTime - animationDeadline) >= 0) { Log.Write("Crafting animation unavailable; timer/checkpoint unaffected."); Function.Call(Hash.REMOVE_ANIM_DICT, AnimDict); animRequested = false; }
            string label = "Fabricando / " + run.Count + " lote(s) / " + run.RemainingSeconds(Game.GameTime) + "s";
            if (!Visuals.TryJobHeader(Name, label, Color.White)) Visuals.Text(label, Visuals.Width / 2 - 220, 24, .34f, Color.White);
            Prompt.Help("Fique na bancada. Segure End para cancelar sem gastar ingredientes.");
            if (unchecked(Game.GameTime - nextSave) < 0) return;
            try
            {
                if (run.Commit(Game.GameTime, true, () => Wallet.Craft(options, run.Recipe, run.Count, run.Receipt, out _, out _)))
                {
                    int count = run.Count; var r = options.Recipes.Find(p => p.Id == run.Recipe);
                    Bump("crafting_batches", count); Bump("crafting_products", r.Quantity * count);
                    End("Fabricacao salva. Veja o estoque e venda na feira.");
                }
                else if (run.RemainingSeconds(Game.GameTime) == 0)
                {
                    Wallet.CraftPreview(options, run.Recipe, run.Count, out string reason);
                    End((reason ?? "Fabricacao nao confirmada.") + " Ingredientes preservados.");
                }
            }
            catch (Exception ex) { nextSave = Game.GameTime + 5000; Log.Error("crafting checkpoint", ex); Notify.Show("Falha ao salvar. Ingredientes/produto nao alterados; nova tentativa em 5s."); }
        }
        void Bump(string stat, int n = 1) { try { achievements.Bump(stat, n); achievements.Save(); } catch (Exception ex) { Log.Error("crafting achievement after checkpoint", ex); } }
        void StopPose()
        {
            try { if (animStarted && owner != null && owner.Exists()) Function.Call(Hash.STOP_ANIM_TASK, owner, AnimDict, AnimClip, 2f); }
            finally { owner = null; animStarted = false; if (animRequested) { Function.Call(Hash.REMOVE_ANIM_DICT, AnimDict); animRequested = false; } }
        }
        void End(string text) { run?.Cancel(); try { StopPose(); } finally { run = null; gate.Release(Name); menu.Visible = false; } Notify.Show(text); }
        public void PollCancel() { if (!Active || !InputAllowed || !Game.IsKeyPressed(Keys.End)) { cancelSince = -1; return; } if (cancelSince < 0) cancelSince = Game.GameTime; if (unchecked(Game.GameTime - cancelSince) >= 1000) End("Fabricacao cancelada. Ingredientes preservados."); }
        public void OnKey(KeyEventArgs e) { }
        public void Reload() { if (Active) End("Fabricacao cancelada na troca de perfil. Ingredientes preservados."); menu.Visible = false; }
        public void Save() { }
        public void Dispose() { try { StopPose(); } finally { run = null; gate.Release(Name); menu.Visible = false; if (blip != null && blip.Exists()) blip.Delete(); } }
    }
}
