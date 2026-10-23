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
    public sealed class FishingJob : ISystem, IWorkContract
    {
        public string Name => "fishing";
        public string Label => "Pesca";
        public bool Active => run != null;
        public Vector3 StartPosition => Pos(options.Vendor);
        readonly ObjectPool pool;
        readonly AchievementSystem achievements;
        readonly ShiftGate gate;
        readonly FishingOptions options;
        readonly NativeMenu menu;
        readonly List<Blip> blips = new List<Blip>();
        readonly Random random = new Random();
        FishingRun run;
        Prop rod;
        Ped owner;
        Vector3 castOrigin;
        int nextSave, cancelSince = -1;
        bool animRequested, animStarted;
        const string AnimDict = "amb@world_human_stand_fishing@idle_a", AnimClip = "idle_c";
        static Vector3 Pos(JobPoint p) => new Vector3(p.X, p.Y, p.Z);
        bool InputAllowed => !Game.IsPaused && !InterfaceOptions.ModalOpen && !pool.AreAnyVisible;
        bool SafeFoot => Game.Player.Character.IsHuman && !Game.Player.Character.IsDead && !Game.Player.Character.IsInVehicle()
            && !Game.Player.Character.IsSwimming && !Game.Player.Character.IsRagdoll && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsFalling;
        bool AtVendor => SafeFoot && Game.Player.Character.Position.DistanceTo(StartPosition) < 5;
        bool AtSpot => options.Spots.Any(p => Game.Player.Character.Position.DistanceTo2D(Pos(p)) <= options.SpotRadius);
        bool BusyCast => Active && (run.Stage == FishingStage.Waiting || run.Stage == FishingStage.Hook || run.Stage == FishingStage.Reeling);
        public FishingJob(ObjectPool pool, AchievementSystem achievements, ShiftGate gate)
        {
            this.pool = pool; this.achievements = achievements; this.gate = gate;
            var defaults = FishingDefaults.Load(); options = Store.LoadDef("Jobs/Fishing.json", () => defaults);
            if (options == null || !options.Valid) { Log.Write("Invalid Fishing.json; embedded defaults used, original retained."); options = defaults; }
            menu = new NativeMenu("Pesca", "EQUIPAMENTO / PONTOS / BOLSA"); pool.Add(menu); Visuals.Register(menu, Name);
            blips.Add(MapBlips.Create(StartPosition, "RPCore: pesca / equipamento / venda", (BlipSprite)410, BlipColor.Blue, .8f));
            foreach (var p in options.Spots) blips.Add(MapBlips.Create(Pos(p), "RPCore: pesca / " + p.Name, (BlipSprite)410, BlipColor.Green, .75f));
        }
        void Add(string title, string text, Action action)
        {
            var item = new NativeItem(title, text);
            item.Activated += (s, e) => { try { action(); } catch (Exception ex) { Log.Error("fishing action", ex); Notify.Show("Acao nao confirmada. Veja RPCore.log."); } };
            menu.Add(item);
        }
        static void Waypoint(Vector3 p) => Function.Call(Hash.SET_NEW_WAYPOINT, p.X, p.Y);
        public void OpenMenu()
        {
            pool.HideAll(); menu.Clear(); var f = Wallet.Fishing;
            if (!f.Valid) { Notify.Show("Dados de pesca invalidos. Arquivo preservado; confira RPCore.log."); Log.Write("Invalid Wallet.json/Fishing; no changes applied."); return; }
            menu.Name = "Pesca / " + f.Catches + " peixes / iscas " + f.Bait;
            Add("GPS: equipamento / venda", "Banca de pesca na entrada do pier Del Perro.", () => { Waypoint(StartPosition); menu.Visible = false; });
            foreach (var p in options.Spots) { var point = p; Add("GPS: " + point.Name, "Ponto aproximado. Fique em piso firme, olhando para agua aberta.", () => { Waypoint(Pos(point)); menu.Visible = false; }); }
            if (!f.RodOwned) Add("Comprar equipamento: $" + options.EquipmentPrice, "Compra permanente por perfil, debitada do banco. Vara nativa incluida.", () => {
                if (!VendorAction()) return;
                if (!Wallet.FishEquipment(options, Guid.NewGuid().ToString("N"), out _)) Notify.Show("Saldo bancario insuficiente ou dados no limite.");
                OpenMenu();
            });
            Add("Comprar 5 iscas: $" + options.BaitPrice * 5, "Requer equipamento e presenca na banca; debita do banco.", () => {
                if (!VendorAction()) return;
                if (!Wallet.FishBait(options, 5, Guid.NewGuid().ToString("N"), out _)) Notify.Show("Requer equipamento, saldo e espaco para iscas.");
                OpenMenu();
            });
            Add("Bolsa de pesca: " + FishingTransactions.Weight(f, options).ToString("0.0") + " / " + options.BagCapacity + " kg", "Peixes deste perfil. Venda na banca.", OpenBag);
            if (!Active) Add("Iniciar pescaria", options.FishPerTrip + " peixes por pescaria; requer equipamento e iscas.", Begin);
            else Add("Encerrar pescaria", "Mantem peixes salvos; isca lancada nao e devolvida.", () => End("Pescaria encerrada. Bolsa mantida."));
            menu.Visible = true;
        }
        void OpenBag()
        {
            pool.HideAll(); menu.Clear(); var f = Wallet.Fishing; if (!f.Valid) { OpenMenu(); return; } menu.Name = "Bolsa de pesca / " + FishingTransactions.Weight(f, options).ToString("0.0") + " kg";
            foreach (var p in f.Bag) if (p.Value > 0) { var m = options.Fish.Find(i => i.Id == p.Key); Add((m?.Name ?? p.Key) + " x" + p.Value, m == null ? "Material desconhecido preservado." : "Preco por unidade: $" + m.Price, () => { }); }
            int quote = FishingTransactions.Quote(f, options);
            Add("Vender peixes conhecidos: $" + Math.Max(0, quote), "Na banca, retira o estoque e credita o banco no mesmo salvamento.", () => {
                if (!VendorAction()) return;
                if (!Wallet.FishSell(options, Guid.NewGuid().ToString("N"), out bool changed)) { Notify.Show("Sem peixes vendaveis ou limite de saldo/dados."); return; }
                if (changed) { Notify.Show("~g~+$" + quote + " no banco~s~ / venda de pesca"); Bump("fishing_sales"); Bump("cash_earned", quote); }
                OpenBag();
            });
            Add("Voltar", "Menu de pesca.", OpenMenu); menu.Visible = true;
        }
        bool VendorAction() { if (AtVendor && !Game.IsMissionActive) return true; Notify.Show("Va a pe a banca de pesca, fora de missao/combate."); return false; }
        void Begin()
        {
            if (Active) return;
            if (!SafeFoot || Game.IsMissionActive || Game.Player.Wanted.WantedLevel > 0 || !Wallet.Fishing.RodOwned || Wallet.Fishing.Bait < 1) { Notify.Show("Requer equipamento, isca e personagem a pe sem procurado/missao/combate."); return; }
            if (!gate.Acquire(Name)) { Notify.Show("Encerre o trabalho de " + gate.Owner + " antes de pescar."); return; }
            run = new FishingRun(options.FishPerTrip, options.HookWindowMs, options.ReelWindowMs); cancelSince = -1; nextSave = 0; menu.Visible = false;
            var nearest = options.Spots.OrderBy(p => Pos(p).DistanceTo2D(Game.Player.Character.Position)).First(); Waypoint(Pos(nearest));
            Notify.Show("Va ao ponto verde. Em piso firme, olhe para agua e pressione E para lancar.");
        }
        bool WaterInFront()
        {
            var p = Game.Player.Character;
            foreach (float distance in new[] { 3f, 5f, 8f, 12f, 16f })
            {
                var q = p.Position + p.ForwardVector * distance; var water = new OutputArgument();
                if (!Function.Call<bool>(Hash.GET_WATER_HEIGHT, q.X, q.Y, p.Position.Z + 2, water)) continue;
                float surface = water.GetResult<float>();
                if (float.IsNaN(surface) || float.IsInfinity(surface) || p.Position.Z < surface || p.Position.Z - surface > 18) continue;
                float ground;
                if (!World.GetGroundHeight(new Vector3(q.X, q.Y, p.Position.Z + 20), out ground) || ground >= surface - .25f) continue;
                return true;
            }
            return false;
        }
        bool CreateRod()
        {
            CleanupPose(); owner = Game.Player.Character; var model = new Model("prop_fishing_rod_02");
            try
            {
                if (!model.IsValid || !model.IsInCdImage || !model.Request(1500)) return false;
                rod = World.CreateProp(model, owner.Position, false, false);
                if (rod == null || !rod.Exists()) return false;
                rod.IsPersistent = true; rod.IsCollisionEnabled = false;
                rod.AttachTo(owner.Bones[Bone.SkelLeftHand], new Vector3(.13f, .1f, .01f), new Vector3(180, 90, 70));
                Function.Call(Hash.REQUEST_ANIM_DICT, AnimDict); animRequested = true; return true;
            }
            finally { model.MarkAsNoLongerNeeded(); }
        }
        void Cast()
        {
            if (!SafeFoot || !AtSpot || Game.Player.Character.Weapons.Current.Hash != WeaponHash.Unarmed) { Notify.Show("Fique a pe no ponto de pesca e guarde a arma."); return; }
            if (!WaterInFront()) { Notify.Show("Olhe para agua aberta proxima, na borda do pier/margem, sem sair do piso firme."); return; }
            try
            {
                if (!CreateRod()) { CleanupPose(); Notify.Show("Vara indisponivel. Isca nao consumida."); return; }
                string fish = options.Pick(random.Next(options.Fish.Sum(f => f.Chance))).Id;
                if (!run.Cast(Game.GameTime, random.Next(options.WaitMinMs, options.WaitMaxMs + 1), fish, id => Wallet.FishUseBait(id, out _))) { CleanupPose(); Notify.Show("Sem iscas/dados disponiveis. Compre mais na banca."); return; }
                castOrigin = owner.Position;
            }
            catch (Exception ex) { CleanupPose(); Log.Error("fishing cast checkpoint", ex); Notify.Show("Lancamento nao salvo; isca preservada. Tente novamente."); }
        }
        public void Tick()
        {
            if (!Active)
            {
                if (InputAllowed && SafeFoot && Game.Player.Character.Position.DistanceTo(StartPosition) < 3) { Prompt.Help("~INPUT_CONTEXT~: equipamento / pesca / venda"); if (Game.IsControlJustPressed(GTA.Control.Context)) OpenMenu(); }
                return;
            }
            var player = Game.Player.Character;
            if (!player.IsHuman || player.IsDead || player.IsInVehicle() || player.IsSwimming || player.IsRagdoll || player.IsInCombat || Game.IsMissionActive || Game.Player.Wanted.WantedLevel > 0) { End("Pescaria encerrada. Peixes ja salvos mantidos."); return; }
            if (!InputAllowed || Game.Player.Character.IsFalling) { if (BusyCast) run.Interrupt(); CleanupPose(); return; }
            if (BusyCast && (owner == null || !owner.Exists() || owner != Game.Player.Character || rod == null || !rod.Exists()
                || Game.Player.Character.Position.DistanceTo(castOrigin) > 1.8f || Game.Player.Character.Weapons.Current.Hash != WeaponHash.Unarmed))
            { run.Interrupt(); CleanupPose(); Notify.Show("Lancamento interrompido. A isca usada nao retorna."); }
            if (BusyCast)
            {
                Game.DisableControlThisFrame(GTA.Control.Attack); Game.DisableControlThisFrame(GTA.Control.Aim); Game.DisableControlThisFrame(GTA.Control.SelectWeapon);
                if (!animStarted && Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, AnimDict)) { Function.Call(Hash.TASK_PLAY_ANIM, owner, AnimDict, AnimClip, 4f, -4f, -1, 49, 0f, false, false, false); animStarted = true; }
            }
            if (run.Advance(Game.GameTime)) { CleanupPose(); Notify.Show("O peixe escapou. Pressione E para tentar com outra isca."); }
            if (run.Stage == FishingStage.Ready)
            {
                Status(AtSpot ? "E: lancar / iscas " + Wallet.Fishing.Bait : "siga o GPS ate um ponto verde", false);
                if (AtSpot) { Prompt.Help("Guarde a arma. Olhe para agua aberta. ~INPUT_CONTEXT~: lancar isca"); if (Game.IsControlJustPressed(GTA.Control.Context)) Cast(); }
            }
            else if (run.Stage == FishingStage.Waiting) Status("Aguarde a fisgada... nao se afaste", false);
            else if (run.Stage == FishingStage.Hook || run.Stage == FishingStage.Reeling)
            {
                bool hook = run.Stage == FishingStage.Hook;
                Status(hook ? "FISGOU! E agora" : "E: recolher " + run.Reels + "/3 (intervalo minimo 0.6s)", true);
                Prompt.Help(hook ? "~INPUT_CONTEXT~: fisgar agora!" : "~INPUT_CONTEXT~: recolher (3 puxadas, sem apertar rapido demais)");
                if (Game.IsControlJustPressed(GTA.Control.Context)) run.Press(Game.GameTime);
            }
            else if (run.Stage == FishingStage.Landed)
            {
                CleanupPose(); Status("peixe recolhido / confirmando bolsa", false);
                if (unchecked(Game.GameTime - nextSave) < 0) return;
                try
                {
                    if (run.Confirm(Collect))
                    { if (run.Stage == FishingStage.Finished) End("Pescaria concluida. Venda os peixes na banca."); else Notify.Show("Peixe salvo. E para o proximo lancamento."); }
                    else { nextSave = Game.GameTime + 3000; Notify.Show("Bolsa cheia/dados no limite. Venda na banca ou encerre. Peixe ainda nao salvo."); }
                }
                catch (Exception ex) { nextSave = Game.GameTime + 5000; Log.Error("fishing catch checkpoint", ex); Notify.Show("Falha ao salvar peixe. Nova tentativa em 5 segundos."); }
            }
        }
        bool Collect(string fish, string receipt, bool last)
        {
            if (!Wallet.FishCatch(options, fish, receipt, last, out bool changed)) return false;
            if (changed) { Notify.Show("Na bolsa: " + options.Fish.Find(f => f.Id == fish).Name); Bump("fishing_catches"); if (last) Bump("fishing_trips"); }
            return true;
        }
        void Status(string text, bool urgent)
        {
            string title = "Pesca " + run.Caught + "/" + run.Total;
            if (!Visuals.TryJobHeader(Name, title, Color.White)) new TextElement(title, new PointF(GTA.UI.Screen.ScaledWidth / 2, 20), .36f, Color.White, GTA.UI.Font.ChaletLondon, Alignment.Center).Draw();
            float x = Visuals.Width / 2 - 380, y = 900;
            Visuals.Rect(x, y, 760, 90, Color.FromArgb(210, 8, 12, 18));
            Visuals.Text(text, x + 12, y + 8, .32f, urgent ? Color.Yellow : Color.White);
            Visuals.Text("End (segurar): encerrar / peixes salvos ficam na bolsa", x + 12, y + 37, .25f, Color.White);
        }
        void Bump(string stat, int amount = 1) { try { achievements.Bump(stat, amount); achievements.Save(); } catch (Exception ex) { Log.Error("fishing achievement after checkpoint", ex); } }
        void CleanupPose()
        {
            try { if (rod != null && rod.Exists()) rod.Delete(); if (animStarted && owner != null && owner.Exists()) Function.Call(Hash.STOP_ANIM_TASK, owner, AnimDict, AnimClip, 2f); }
            finally { rod = null; owner = null; animStarted = false; if (animRequested) { Function.Call(Hash.REMOVE_ANIM_DICT, AnimDict); animRequested = false; } }
        }
        void End(string text) { run?.Cancel(); try { CleanupPose(); } finally { run = null; gate.Release(Name); menu.Visible = false; } Notify.Show(text); }
        public void PollCancel() { if (!Active || !InputAllowed || !Game.IsKeyPressed(Keys.End)) { cancelSince = -1; return; } if (cancelSince < 0) cancelSince = Game.GameTime; if (unchecked(Game.GameTime - cancelSince) >= 1000) End("Pescaria cancelada. Bolsa mantida."); }
        public void OnKey(KeyEventArgs e) { }
        public void Reload() { if (Active) End("Pescaria encerrada na troca de perfil."); menu.Visible = false; }
        public void Save() { }
        public void Dispose() { try { CleanupPose(); } finally { run = null; gate.Release(Name); menu.Visible = false; foreach (var b in blips) if (b != null && b.Exists()) b.Delete(); } }
    }
}
