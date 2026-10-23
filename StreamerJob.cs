using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;

namespace RPCore
{
    public sealed class StreamerJob : ISystem, IWorkContract
    {
        public string Name => "streamer";
        public string Label => "Streamer / transmissao";
        public bool Active => run != null;
        public event Action<string, string> Activity;
        readonly ObjectPool pool;
        readonly AchievementSystem achievements;
        readonly NeedsSystem needs;
        readonly ShiftGate jobs;
        readonly StreamerOptions options;
        readonly NativeMenu menu;
        readonly Random random = new Random();
        readonly Queue<string> chat = new Queue<string>();
        StreamerRun run;
        StreamReward pending;
        int lastFrame, nextChat, nextSave, viewers = 2;
        string stopReason, profileAtStart;
        bool showOverlay;
        public StreamerJob(ObjectPool pool, AchievementSystem achievements, NeedsSystem needs, ShiftGate jobs)
        {
            this.pool = pool; this.achievements = achievements; this.needs = needs; this.jobs = jobs;
            var defaults = StreamerDefaults.Load(); options = Store.LoadDef("Jobs/Streamer.json", () => defaults);
            if (options == null || !options.Valid) { Log.Write("Invalid Streamer.json; embedded defaults used, original retained."); options = defaults; }
            showOverlay = options.Overlay; menu = new NativeMenu("Streamer", "CANAL / LIVE / ESTATISTICAS"); pool.Add(menu); Visuals.Register(menu, Name); lastFrame = Game.GameTime;
        }
        void Add(string title, string text, Action action)
        {
            var row = new NativeItem(title, text); row.Activated += (s, e) => { try { action(); } catch (Exception ex) { Log.Error("streamer action", ex); Notify.Show("Acao nao confirmada. Veja RPCore.log."); } }; menu.Add(row);
        }
        bool ValidData()
        {
            if (Wallet.Streamer.Valid) return true;
            Log.Write("Invalid Wallet.json/Streamer; original retained."); Notify.Show("Dados de streamer invalidos. Arquivo preservado."); return false;
        }
        public void OpenMenu()
        {
            pool.HideAll(); menu.Clear(); if (!ValidData()) return; var d = Wallet.Streamer;
            menu.Name = d.Channel + " / " + d.Followers + " seguidores";
            Add(Active ? "Encerrar live" : "Iniciar live / " + options.DurationSeconds + "s", "Transmissao local do jogo. Pode acompanhar outro trabalho; pagamentos por blocos completos de " + options.TickSeconds + "s.", () => { if (Active) RequestStop("Encerrada pelo jogador"); else Begin(); });
            Add("Canal: " + d.Channel, "3 a 20 letras/numeros/underscore. Altere fora de live.", () => Edit(true));
            Add("Titulo: " + d.Title, "Ate 60 caracteres. Altere fora de live.", () => Edit(false));
            Add("Estatisticas / ultima live", "Seguidores, inscritos, bits, doacoes, receita bancaria e historico.", OpenStats);
            Add(showOverlay ? "Ocultar painel/chat" : "Mostrar painel/chat", "Ocultar nao encerra a live. Preferencia desta sessao; padrao em Streamer.json.", () => { showOverlay = !showOverlay; OpenMenu(); });
            menu.Visible = true;
        }
        void Edit(bool channel)
        {
            if (Active) { Notify.Show("Encerre a live antes de editar canal/titulo."); return; }
            var d = Wallet.Streamer;
            string input = Game.GetUserInput(WindowTitle.EnterMessage60, channel ? d.Channel : d.Title, channel ? 20 : 60)?.Trim();
            if (string.IsNullOrEmpty(input)) return;
            if (!Wallet.StreamSettings(channel ? input : d.Channel, channel ? d.Title : input, Guid.NewGuid().ToString("N"), out _)) { Notify.Show("Canal/titulo invalido ou dados indisponiveis."); return; }
            OpenMenu();
        }
        void OpenStats()
        {
            pool.HideAll(); menu.Clear(); if (!ValidData()) return; var d = Wallet.Streamer; menu.Name = "Canal " + d.Channel;
            Add("Seguidores: " + d.Followers + " / inscritos: " + d.Subscribers, "Progresso permanente do perfil.", () => { });
            Add("Lives: " + d.Sessions + " / completas: " + d.Completed, "Live encerrada antes do limite conserva blocos pagos; nao conta como completa.", () => { });
            Add("Receita total: $" + d.Earned + " / bits: " + d.Bits, "Doacoes acumuladas: $" + d.Donations + ". Tempo salvo: " + d.Seconds + "s. Pico: " + d.Peak, () => { });
            if (d.Last != null)
            {
                var s = d.Last;
                Add("Ultima: " + s.Title, (Active ? "Ao vivo" : s.Reason) + " / " + s.Seconds + "s / $" + s.Income + " / +" + s.Followers + " seguidores / " + s.Subscribers + " inscritos / " + s.Bits + " bits / $" + s.Donations + " doacoes.", () => { });
            }
            Add("Voltar", "Canal / live.", OpenMenu); menu.Visible = true;
        }
        public string[] PhoneRows() => new[] { Active ? "Encerrar live" : "Iniciar live", "Canal: " + Wallet.Streamer.Channel, "Titulo: " + Wallet.Streamer.Title, "Estatisticas", showOverlay ? "Ocultar chat" : "Mostrar chat" };
        public void PhoneAction(int row)
        {
            try { if (row == 0) { if (Active) RequestStop("Encerrada pelo jogador"); else Begin(); } else if (row == 1) Edit(true); else if (row == 2) Edit(false); else if (row == 3) OpenStats(); else if (row == 4) showOverlay = !showOverlay; }
            catch (Exception ex) { Log.Error("streamer phone action", ex); Notify.Show("Acao de live nao confirmada. Veja RPCore.log."); }
        }
        void Begin()
        {
            if (Active || !ValidData()) return;
            var p = Game.Player.Character;
            if (!p.IsHuman || p.IsDead || Game.IsMissionActive || Game.IsPaused || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE)) { Notify.Show("Inicie com personagem vivo, fora de missao/cutscene."); return; }
            var next = new StreamerRun(options.DurationSeconds, options.TickSeconds);
            if (!Wallet.StreamStart(next.Id, out _)) { Notify.Show("Nao foi possivel salvar o inicio da live."); return; }
            // A broadcast observes gameplay and intentionally does not acquire/release the work gate.
            run = next; profileAtStart = Profile.Id; stopReason = null; pending = null; viewers = StreamerRules.Viewers(Wallet.Streamer.Followers, Context(), random.Next(100));
            chat.Clear(); Push("RPCore: live iniciada!"); lastFrame = nextChat = nextSave = Game.GameTime; menu.Visible = false;
            Notify.Show("Live iniciada. Pode dirigir ou trabalhar. F2 / Twitch ou F10 / Streamer encerra; End continua reservado aos trabalhos.");
        }
        StreamContext Context()
        {
            var p = Game.Player.Character;
            if (Game.Player.Wanted.WantedLevel > 0) return StreamContext.Police;
            if (p.IsSwimming) return StreamContext.Swimming;
            if (p.Health < p.MaxHealth * .6f) return StreamContext.Injured;
            if (!string.IsNullOrEmpty(jobs.Owner)) return StreamContext.Working;
            if (p.IsInVehicle()) return StreamContext.Driving;
            if (World.Weather == Weather.Raining || World.Weather == Weather.ThunderStorm) return StreamContext.Rain;
            return p.Speed > .8f ? StreamContext.Walking : StreamContext.Idle;
        }
        string ContextMessage(StreamContext context)
        {
            if (context == StreamContext.Working) return "ta trabalhando na live!";
            if (context == StreamContext.Driving) return "passeio por Los Santos";
            if (context == StreamContext.Police) return "a policia entrou na historia!";
            if (context == StreamContext.Swimming) return "agora virou live de natacao";
            if (context == StreamContext.Injured) return "cuidado com a saude!";
            if (context == StreamContext.Rain) return "essa chuva mudou o passeio";
            if (needs.Thirst < 20) return "vai beber uma agua!";
            if (needs.Hunger < 20) return "parada pra comer?";
            return context == StreamContext.Walking ? "mostra mais desse bairro" : "salve chat de Los Santos!";
        }
        void Push(string line) { chat.Enqueue(line.Length > 35 ? line.Substring(0, 35) : line); while (chat.Count > 9) chat.Dequeue(); }
        public void Tick()
        {
            int now = Game.GameTime, delta = unchecked(now - lastFrame); lastFrame = now;
            if (!Active) return;
            if (stopReason != null) { if (unchecked(now - nextSave) >= 0) TryStop(); return; }
            var p = Game.Player.Character;
            if (p.IsDead || !p.IsHuman || Game.IsMissionActive) { RequestStop(p.IsDead ? "Morte do personagem" : "Missao ou personagem alterado"); return; }
            bool eligible = !Game.IsPaused && !InterfaceOptions.ModalOpen && !pool.AreAnyVisible && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
            if (!eligible) return;
            if (pending == null) run.Advance(delta, true);
            if (run.Due && unchecked(now - nextSave) >= 0)
            {
                if (pending == null) pending = StreamerRules.Create(options, Wallet.Streamer.Followers, Context(), random.Next(100), random.Next(100), random.Next(1000000), random.Next(100));
                try
                {
                    bool changed = false;
                    if (run.Confirm((step, last) => Wallet.StreamBlock(options, pending, run.Id, step, last, out changed)))
                    {
                        var reward = pending; pending = null; viewers = reward.Viewers;
                        if (changed)
                        {
                            Push("+" + reward.Followers + " seguidor(es)");
                            if (reward.Event == StreamEvent.Subscription) Push("Chat: nova inscricao!");
                            if (reward.Event == StreamEvent.Bits) Push("Chat enviou " + reward.Amount + " bits!");
                            if (reward.Event == StreamEvent.Donation) Push("Doacao: $" + reward.Amount);
                            Push("Banco +$" + reward.Revenue(options)); Bump("stream_blocks"); Bump("cash_earned", reward.Revenue(options));
                            History("Live: bloco pago", "+$" + reward.Revenue(options) + " no banco / +" + reward.Followers + " seguidores");
                        }
                        if (run.Stage == StreamerStage.Finished) { Bump("stream_completed"); Finish("Live concluida. Confira estatisticas e saldo bancario."); return; }
                    }
                    else { nextSave = now + 5000; Notify.Show("Bloco nao confirmado: limite de saldo/dados. Live aguarda salvamento."); }
                }
                catch (Exception ex) { nextSave = now + 5000; Log.Error("streamer block checkpoint", ex); Notify.Show("Falha ao salvar live. Bloco pendente, sem novo sorteio; tentativa em 5s."); }
            }
            if (unchecked(now - nextChat) >= 0) { nextChat = now + 4000 + random.Next(3000); Push("LS_" + random.Next(10, 99) + ": " + ContextMessage(Context())); }
            if (showOverlay && !Function.Call<bool>(Hash.IS_HUD_HIDDEN)) Draw();
        }
        void Draw()
        {
            float scale = Visuals.Scale, x = Visuals.InsetX + 21 * scale, y = Visuals.InsetY + 233 * scale;
            bool original = Visuals.Active && Visuals.Has("PSRP_PHONE", "TwitchOverlay");
            if (original) Visuals.SpriteVisible("PSRP_PHONE", "TwitchOverlay", new RectangleF(x, y, 325 * scale, 428 * scale), new RectangleF(21, 233, 325, 428), Color.White);
            if (!original) { Visuals.Rect(x, y, 325 * scale, 428 * scale, Color.FromArgb(225, 73, 40, 135)); Visuals.Rect(x + 14 * scale, y + 87 * scale, 290 * scale, 323 * scale, Color.FromArgb(245, 20, 20, 20)); }
            Visuals.Label("" + viewers, x + 60 * scale, y + 17 * scale, .35f * scale, Color.White);
            Visuals.Label("LIVE / " + run.ElapsedMs / 1000 + "s", x + 173 * scale, y + 29 * scale, .20f * scale, Color.White);
            var d = Wallet.Streamer;
            Visuals.Label("Seg " + d.Followers + " / Sub " + d.Subscribers + " / $" + (d.Last?.Income ?? 0), x + 14 * scale, y + 62 * scale, .20f * scale, Color.White);
            int row = 0; foreach (string line in chat) { Visuals.Label(line, x + 20 * scale, y + (100 + row * 30) * scale, .23f * scale, Color.White); row++; }
        }
        void RequestStop(string reason) { if (!Active) return; stopReason = reason; pending = null; nextSave = Game.GameTime; TryStop(); }
        void TryStop()
        {
            try
            {
                if (!Wallet.StreamStop(run.Id, stopReason, out _)) { nextSave = Game.GameTime + 5000; Notify.Show("Encerramento pendente; confira dados/salvamento."); return; }
                Finish("Live encerrada. Blocos ja pagos preservados.");
            }
            catch (Exception ex) { nextSave = Game.GameTime + 5000; Log.Error("streamer stop checkpoint", ex); Notify.Show("Falha ao salvar encerramento. Live sem novos ganhos; nova tentativa em 5s."); }
        }
        void Finish(string message) { run?.Cancel(); run = null; pending = null; stopReason = null; chat.Clear(); menu.Visible = false; History("Live encerrada", message); Notify.Show(message); }
        void History(string title, string body) { try { Activity?.Invoke(title, body); } catch (Exception ex) { Log.Error("streamer phone history", ex); } }
        void Bump(string stat, int n = 1) { try { achievements.Bump(stat, n); achievements.Save(); } catch (Exception ex) { Log.Error("streamer achievement after checkpoint", ex); } }
        public void PollCancel() { } // End remains dedicated to physical jobs when streaming in parallel.
        public void OnKey(KeyEventArgs e) { }
        public void Reload() { run?.Cancel(); run = null; pending = null; stopReason = null; chat.Clear(); menu.Visible = false; lastFrame = Game.GameTime; }
        public void Save() { }
        public void Dispose()
        {
            try { if (Active && profileAtStart == Profile.Id) Wallet.StreamStop(run.Id, "Scripts encerrados", out _); }
            catch (Exception ex) { Log.Error("streamer dispose checkpoint", ex); }
            finally { Reload(); }
        }
    }
}
