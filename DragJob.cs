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
    public sealed class DragJob : ISystem, IWorkContract
    {
        public string Name => "drag";
        public string Label => "Arrancada / Sandy Shores";
        public bool Active => run != null;
        public Vector3 StartPosition => Pos(options.Entry);
        readonly ObjectPool pool;
        readonly AchievementSystem achievements;
        readonly ShiftGate gate;
        readonly DragOptions options;
        readonly NativeMenu menu;
        readonly Blip entryBlip;
        DragRun run;
        Vehicle playerCar, rivalCar;
        Ped rival;
        Blip targetBlip, rivalBlip;
        int selectedRival, began, racingAt, nextSave, nextCollision, cancelSince = -1;
        string ownerProfile;
        static Vector3 Pos(JobPoint p) => new Vector3(p.X, p.Y, p.Z);
        static DragPosition Sample(Entity e) => new DragPosition(e.Position.X, e.Position.Y, e.Position.Z);
        bool InputAllowed => !Game.IsPaused && !InterfaceOptions.ModalOpen && !pool.AreAnyVisible;
        bool Driver => playerCar != null && playerCar.Exists() && Game.Player.Character.IsInVehicle(playerCar) && playerCar.Driver == Game.Player.Character;
        public DragJob(ObjectPool pool, AchievementSystem achievements, ShiftGate gate)
        {
            this.pool = pool; this.achievements = achievements; this.gate = gate;
            var defaults = DragDefaults.Load(); options = Store.LoadDef("Jobs/Drag.json", () => defaults);
            if (options == null || !options.Valid) { Log.Write("Invalid Drag.json; embedded defaults used, original retained."); options = defaults; }
            menu = new NativeMenu("Arrancada", "PISTA / ADVERSARIO / APOSTA"); pool.Add(menu); Visuals.Register(menu, Name);
            entryBlip = MapBlips.Create(StartPosition, "RPCore: arrancada / Sandy Shores", (BlipSprite)315, BlipColor.Orange, .8f);
            Recover();
        }
        void Add(string title, string text, Action action)
        {
            var item = new NativeItem(title, text); item.Activated += (s, e) => { try { action(); } catch (Exception ex) { Log.Error("drag action", ex); Notify.Show("Acao nao confirmada. Veja RPCore.log."); } }; menu.Add(item);
        }
        public void OpenMenu()
        {
            pool.HideAll(); menu.Clear(); var d = Wallet.Drag;
            if (!d.Valid) { Log.Write("Invalid Wallet.json/Drag; original retained."); Notify.Show("Dados de arrancada invalidos. Arquivo preservado."); return; }
            menu.Name = "Arrancada / " + d.Wins + " vitorias / " + d.Losses + " derrotas";
            Add("GPS: encontro / inscricao", "Pista publica de Sandy Shores. Traga seu carro; sem teleporte automatico.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, StartPosition.X, StartPosition.Y); menu.Visible = false; });
            Add("Adversario: " + options.Rivals[selectedRival], "Enter alterna o modelo do NPC. Sem ajuste de potencia do carro do jogador.", () => { selectedRival = (selectedRival + 1) % options.Rivals.Count; OpenMenu(); });
            if (!Active && d.Pending == null) foreach (int b in options.Bets) { int stake = b; Add(stake == 0 ? "Treino sem aposta" : "Inscrever / apostar $" + stake, "Como motorista no encontro, parado. Valor reservado no banco. Vitoria devolve 2x; derrota/queima perde a aposta.", () => Begin(stake)); }
            if (Active) Add("Encerrar corrida", "Antes do sinal: devolve aposta. Depois do sinal: abandono perde aposta. Queima de largada tambem perde.", () => Finish(run.Stage == DragStage.Racing ? DragOutcome.Abandoned : DragOutcome.Cancelled));
            if (d.Pending != null) Add("Resolver aposta pendente", "Recupera resultado gravado; se houve interrupcao sem resultado, reembolsa antes do sinal ou registra abandono depois dele.", () => { if (Active && run.Stage == DragStage.Result) TrySettle(); else if (!Active) Recover(); else Notify.Show("Encerre a corrida ativa primeiro."); OpenMenu(); });
            Add("Historico / melhor tempo", "Melhor: " + (d.BestMs > 0 ? (d.BestMs / 1000f).ToString("0.000") + "s" : "--") + " / ultima: " + d.LastResult + " / entrada total: $" + d.Staked + " / retorno total: $" + d.Returned + ". Treino tambem registra resultado.", () => { });
            menu.Visible = true;
        }
        bool EligibleCar(Vehicle v)
        {
            if (v == null || !v.Exists() || !v.Model.IsCar || v.IsDead || v.EngineHealth < 300 || v.BodyHealth < 300 || v.IsInAir || JobVehicles.Contains(v)) return false;
            Vector3 min, max; v.Model.GetDimensions(out min, out max);
            return max.Y - min.Y < 8 && max.Z - min.Z < 3 && !Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER, v);
        }
        bool SpawnRival()
        {
            var model = new Model(options.Rivals[selectedRival]);
            try
            {
                if (!model.IsValid || !model.IsInCdImage || !model.IsCar || !model.Request(2500)) return false;
                var point = new JobPoint { Name = "rival grid", X = options.RivalStart.X, Y = options.RivalStart.Y, Z = options.RivalStart.Z, Heading = options.RivalStart.Heading };
                float ground; if (!World.GetGroundHeight(Pos(point) + new Vector3(0, 0, 10), out ground) || Math.Abs(ground - point.Z) > 3) return false;
                Vector3 min, max, pmin, pmax; model.GetDimensions(out min, out max); point.Z = ground - min.Z + .12f;
                playerCar.Model.GetDimensions(out pmin, out pmax);
                var current = new JobPoint { Name = "player", X = playerCar.Position.X, Y = playerCar.Position.Y, Z = playerCar.Position.Z, Heading = playerCar.Heading };
                if (new SpawnBox(point, min.X, max.X, min.Y, max.Y, min.Z, max.Z).Overlaps(new SpawnBox(current, pmin.X, pmax.X, pmin.Y, pmax.Y, pmin.Z, pmax.Z))) return false;
                if (!RigSpawn.ClearForTeleport(point, model, playerCar, Game.Player.Character)) return false;
                rivalCar = World.CreateVehicle(model, Pos(point), point.Heading);
                if (rivalCar == null || !rivalCar.Exists()) return false;
                rivalCar.IsPersistent = true; rivalCar.IsEngineRunning = true; rivalCar.IsPositionFrozen = true;
                rival = JobActors.Create(Pos(point) + new Vector3(0, 0, 2), new Random()); if (rival == null || !rival.Exists()) return false;
                rival.SetIntoVehicle(rivalCar, VehicleSeat.Driver);
                Function.Call(Hash.SET_DRIVER_ABILITY, rival, 1f); Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, rival, .5f);
                return rivalCar.Driver == rival;
            }
            finally { model.MarkAsNoLongerNeeded(); }
        }
        void Begin(int stake)
        {
            if (Active) return; var p = Game.Player.Character; var v = p.CurrentVehicle;
            if (Wallet.Drag.Pending != null || !Wallet.Drag.Valid || p.IsDead || Game.IsMissionActive || Game.Player.Wanted.WantedLevel > 0 || v == null || v.Driver != p || !EligibleCar(v) || v.Speed > .5f || p.Position.DistanceTo(StartPosition) > 12)
            { Notify.Show("Va ao encontro como motorista de carro proprio, parado e sem procurado/missao. Resolva aposta pendente antes."); return; }
            if (!gate.Acquire(Name)) { Notify.Show("Encerre a atividade de " + gate.Owner + " antes da arrancada."); return; }
            bool ready = false; playerCar = v;
            try
            {
                var planned = new DragRun(options);
                if (!SpawnRival()) { Notify.Show("Largada do adversario ocupada ou modelo/chao indisponivel. Nada cobrado; libere a faixa e tente novamente."); return; }
                if (!Wallet.DragEnter(options, stake, planned.Id, out _)) { Notify.Show("Saldo bancario insuficiente ou dados/aposta indisponiveis. Nada confirmado."); return; }
                run = planned; ownerProfile = Profile.Id; began = nextSave = nextCollision = Game.GameTime; cancelSince = -1; menu.Visible = false;
                targetBlip = World.CreateBlip(Pos(options.PlayerStart)); targetBlip.Name = "RPCore: alinhar arrancada"; targetBlip.Color = BlipColor.Green;
                rivalBlip = World.CreateBlip(rivalCar.Position); rivalBlip.Name = "RPCore: adversario"; rivalBlip.Color = BlipColor.Orange;
                ready = true; Notify.Show("Alinhe na marca verde, sentido da pista, e pare. E inicia contagem; segure o freio ate JA. Seu carro nao e movido/congelado.");
            }
            finally { if (!ready) { try { Cleanup(); } finally { run = null; playerCar = null; gate.Release(Name); } Recover(); } }
        }
        void RequestTrack()
        {
            if (unchecked(Game.GameTime - nextCollision) < 0) return; nextCollision = Game.GameTime + 1000;
            for (int i = 0; i <= 8; i++)
            { float t = i / 8f; Function.Call(Hash.REQUEST_COLLISION_AT_COORD, options.PlayerStart.X + (options.PlayerFinish.X - options.PlayerStart.X) * t, options.PlayerStart.Y + (options.PlayerFinish.Y - options.PlayerStart.Y) * t, options.PlayerStart.Z + 2); }
        }
        public void Tick()
        {
            if (!Active)
            {
                if (InputAllowed && Game.Player.Character.Position.DistanceTo(StartPosition) < 12) { Prompt.Help("~INPUT_CONTEXT~: arrancada / inscricao / aposta"); if (Game.IsControlJustPressed(GTA.Control.Context)) OpenMenu(); } return;
            }
            if (run.Stage == DragStage.Result) { if (unchecked(Game.GameTime - nextSave) >= 0) TrySettle(); return; }
            if (Game.IsPaused) return;
            if (!Driver || playerCar.IsDead || playerCar.EngineHealth < 100 || Game.Player.Character.IsDead || Game.IsMissionActive || Game.Player.Wanted.WantedLevel > 0)
            { Finish(run.Stage == DragStage.Racing ? DragOutcome.Abandoned : DragOutcome.Cancelled); return; }
            if (rivalCar == null || !rivalCar.Exists() || rivalCar.IsDead || rival == null || !rival.Exists() || rival.IsDead || rivalCar.Driver != rival) { Finish(DragOutcome.Void); return; }
            RequestTrack(); if (rivalBlip != null && rivalBlip.Exists()) rivalBlip.Position = rivalCar.Position;
            if (run.Stage == DragStage.Align)
            {
                if (unchecked(Game.GameTime - began) >= options.StagingSeconds * 1000) { Finish(DragOutcome.Cancelled); return; }
                DrawPoint(options.PlayerStart, Color.LimeGreen); Status("alinhe no verde / pare e pressione E");
                float angle = Math.Abs((playerCar.Heading - options.PlayerStart.Heading % 360 + 540) % 360 - 180);
                if (InputAllowed && playerCar.Position.DistanceTo(Pos(options.PlayerStart)) < 2 && playerCar.Speed < .5f && angle < 15 && Game.IsControlJustPressed(GTA.Control.Context)) run.Countdown(Game.GameTime);
            }
            else if (run.Stage == DragStage.Countdown)
            {
                if (!InputAllowed) { Finish(DragOutcome.Cancelled); return; }
                var p = Sample(playerCar);
                if (DragGeometry.Progress(options.PlayerStart, options.PlayerFinish, p) > 2.5 || playerCar.Position.DistanceTo(Pos(options.PlayerStart)) > 4 || playerCar.Speed > 2.5f) { Finish(DragOutcome.FalseStart); return; }
                Status(run.CountdownLeft(Game.GameTime) > 0 ? "segure o freio... " + run.CountdownLeft(Game.GameTime) : "aguarde confirmacao da largada");
                if (unchecked(Game.GameTime - nextSave) < 0) return;
                try
                {
                    if (run.Green(Game.GameTime, p, Sample(rivalCar), () => Wallet.DragGreen(run.Id, out _)))
                    {
                        racingAt = Game.GameTime; rivalCar.IsPositionFrozen = false;
                        var direction = (Pos(options.RivalFinish) - Pos(options.RivalStart)).Normalized;
                        rival.Task.DriveTo(rivalCar, Pos(options.RivalFinish) + direction * 80, options.AiSpeed, VehicleDrivingFlags.ForceStraightLine, 2f);
                        if (targetBlip != null && targetBlip.Exists()) { targetBlip.Position = Pos(options.PlayerFinish); targetBlip.Name = "RPCore: chegada arrancada"; }
                        Notify.Show("~g~JA!~s~ Cruze a chegada antes do adversario.");
                    }
                }
                catch (Exception ex) { Log.Error("drag green checkpoint / rival start", ex); if (run.Stage == DragStage.Racing) { Finish(DragOutcome.Void); Notify.Show("Adversario nao iniciou. Corrida anulada; confira reembolso/pendencia."); } else { nextSave = Game.GameTime + 5000; Notify.Show("Largada nao salva; mantenha o freio. Nova tentativa em 5s."); } }
            }
            else if (run.Stage == DragStage.Racing)
            {
                if (!InputAllowed) { Finish(DragOutcome.Abandoned); return; }
                if (unchecked(Game.GameTime - racingAt) > 8000 && rivalCar.Speed < 1 && DragGeometry.Progress(options.RivalStart, options.RivalFinish, Sample(rivalCar)) < 10)
                { Finish(DragOutcome.Void); Notify.Show("Adversario nao saiu da largada. Corrida anulada; teste carregamento da pista."); return; }
                DrawPoint(options.PlayerFinish, Color.Yellow); Status("JA / acelere ate a chegada amarela");
                run.Sample(Game.GameTime, Sample(playerCar), Sample(rivalCar)); if (run.Stage == DragStage.Result) TrySettle();
            }
        }
        static void DrawPoint(JobPoint p, Color color)
        {
            float ground; var at = Pos(p);
            if (World.GetGroundHeight(at + new Vector3(0, 0, 10), out ground)) at.Z = ground + .08f;
            World.DrawMarker(MarkerType.Cylinder, at, Vector3.Zero, Vector3.Zero, new Vector3(3, 3, .3f), color);
        }
        void Status(string text)
        {
            if (!Visuals.TryJobHeader(Name, "Arrancada / " + (Wallet.Drag.Pending?.Stake ?? 0), Color.White)) Visuals.Text("Arrancada", Visuals.Width / 2 - 150, 24, .35f, Color.White);
            float x = Visuals.Width / 2 - 380; Visuals.Rect(x, 900, 760, 80, Color.FromArgb(210, 8, 12, 18)); Visuals.Text(text, x + 12, 910, .33f, Color.White); Visuals.Text("End (segurar): encerrar / abrir menus apos JA conta abandono", x + 12, 946, .23f, Color.White);
        }
        void Finish(DragOutcome outcome)
        {
            if (!Active) return;
            if (run.Stage != DragStage.Result) run.Finish(outcome);
            TrySettle();
        }
        void TrySettle()
        {
            if (!Active || run.Stage != DragStage.Result) return;
            try
            {
                if (!Wallet.DragRecord(run.Id, run.Result, run.TimeMs, out _)) { nextSave = Game.GameTime + 5000; Notify.Show("Resultado pendente. Aposta permanece reservada."); return; }
                var p = Wallet.Drag.Pending; int stake = p?.Stake ?? 0, returned = DragTransactions.Payout(p);
                if (!Wallet.DragSettle(run.Id, out bool changed)) { nextSave = Game.GameTime + 5000; Notify.Show("Liquidacao pendente: limite bancario/dados. Resolva pelo menu; resultado salvo."); return; }
                var result = run.Result;
                if (changed) { Bump("drag_finished"); if (result == DragOutcome.Win) Bump("drag_wins"); if (returned > stake) Bump("cash_earned", returned - stake); }
                Notify.Show("Arrancada: " + ResultName(result) + " / retorno $" + returned + " no banco" + (run.TimeMs > 0 ? " / " + (run.TimeMs / 1000f).ToString("0.000") + "s" : ""));
                Cleanup(); run = null; playerCar = null; gate.Release(Name); menu.Visible = false;
            }
            catch (Exception ex) { nextSave = Game.GameTime + 5000; Log.Error("drag result checkpoint", ex); Notify.Show("Falha ao salvar resultado/pagamento. Aposta pendente; nova tentativa em 5s."); }
        }
        static string ResultName(DragOutcome r) => r == DragOutcome.Win ? "vitoria" : r == DragOutcome.Loss ? "derrota" : r == DragOutcome.Tie ? "empate" : r == DragOutcome.FalseStart ? "queima de largada" : r == DragOutcome.Abandoned ? "abandono" : r == DragOutcome.Void ? "anulada / reembolso" : "cancelada / reembolso";
        void Recover()
        {
            try
            {
                var p = Wallet.Drag.Pending; if (p == null) return;
                if (p.Result == DragOutcome.None && !Wallet.DragRecord(p.Id, p.Started ? DragOutcome.Abandoned : DragOutcome.Cancelled, 0, out _)) return;
                if (Wallet.DragSettle(p.Id, out _)) Notify.Show("Aposta interrompida resolvida: " + Wallet.Drag.LastResult);
                else Notify.Show("Aposta de arrancada pendente. Confira saldo/dados no menu.");
            }
            catch (Exception ex) { Log.Error("drag escrow recovery", ex); Notify.Show("Aposta pendente preservada. Resolva pelo menu de arrancada."); }
        }
        void Cleanup()
        {
            if (rivalCar != null && rivalCar.Exists()) rivalCar.IsPositionFrozen = false;
            if (rival != null && rival.Exists()) rival.Delete(); rival = null;
            if (rivalCar != null && rivalCar.Exists()) { if (rivalCar.Occupants.Length == 0) rivalCar.Delete(); else rivalCar.MarkAsNoLongerNeeded(); } rivalCar = null;
            if (targetBlip != null && targetBlip.Exists()) targetBlip.Delete(); targetBlip = null;
            if (rivalBlip != null && rivalBlip.Exists()) rivalBlip.Delete(); rivalBlip = null;
        }
        void Bump(string stat, int amount = 1) { try { achievements.Bump(stat, amount); achievements.Save(); } catch (Exception ex) { Log.Error("drag achievement after checkpoint", ex); } }
        public void PollCancel() { if (!Active || !InputAllowed || !Game.IsKeyPressed(Keys.End)) { cancelSince = -1; return; } if (cancelSince < 0) cancelSince = Game.GameTime; if (unchecked(Game.GameTime - cancelSince) >= 1000) Finish(run.Stage == DragStage.Racing ? DragOutcome.Abandoned : DragOutcome.Cancelled); }
        public void OnKey(KeyEventArgs e) { }
        public void Reload() { try { Cleanup(); } finally { run = null; playerCar = null; gate.Release(Name); menu.Visible = false; } Recover(); }
        public void Save() { }
        public void Dispose()
        {
            try { if (Active && ownerProfile == Profile.Id) { if (run.Stage != DragStage.Result) run.Finish(run.Stage == DragStage.Racing ? DragOutcome.Abandoned : DragOutcome.Cancelled); Wallet.DragRecord(run.Id, run.Result, run.TimeMs, out _); Wallet.DragSettle(run.Id, out _); } }
            catch (Exception ex) { Log.Error("drag dispose escrow", ex); }
            finally { try { Cleanup(); } finally { run = null; gate.Release(Name); menu.Visible = false; if (entryBlip != null && entryBlip.Exists()) entryBlip.Delete(); } }
        }
    }
}
