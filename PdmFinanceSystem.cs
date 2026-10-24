using System;
using System.Linq;
using System.Windows.Forms;
using GTA;
using LemonUI;
using LemonUI.Menus;
namespace RPCore
{
    public sealed class PdmFinanceSystem : ISystem
    {
        public string Name => "pdmfinance";
        readonly ObjectPool pool; readonly NativeMenu menu; readonly Action back; readonly AchievementSystem achievements; readonly PdmFinanceOptions options;
        string profile, stepId; long pending; int last, stepSeconds, retryAt;
        public PdmFinanceSystem(ObjectPool pool, PdmFinanceOptions options, AchievementSystem achievements, Action back)
        {
            this.pool = pool; this.options = options; this.achievements = achievements; this.back = back;
            menu = new NativeMenu("PDM / financiamentos", "ENTRADAS / PARCELAS / CONTRATOS"); pool.Add(menu); Visuals.Register(menu, "pdm"); Reload();
        }
        void Sample()
        {
            int now = Game.GameTime, elapsed = unchecked(now - last); last = now;
            if (profile != Profile.Id || !Wallet.Pdm.Valid || !Wallet.Pdm.Owned || !Wallet.Pdm.Loans.Any(l => !l.Complete)) { pending = 0; stepId = null; return; }
            if (!Game.IsPaused && !Game.IsMissionActive && !Game.Player.Character.IsDead && elapsed > 0) pending += Math.Min(2000, elapsed);
        }
        public void Flush()
        {
            Sample(); if (profile != Profile.Id || !Wallet.Pdm.Valid) return;
            while (pending >= 1000)
            {
                if (!Wallet.Pdm.Loans.Any(l => !l.Complete)) { pending = 0; stepId = null; return; }
                if (stepId == null) { stepId = Guid.NewGuid().ToString("N"); stepSeconds = (int)Math.Min(60, pending / 1000); }
                long revenue = Wallet.Pdm.Revenue; int profit = Wallet.Pdm.Loans.Sum(l => l.Profit);
                if (!Wallet.PdmFinanceWork(stepSeconds, stepId, out bool changed)) throw new InvalidOperationException("PDM finance checkpoint refused; elapsed time retained.");
                pending -= stepSeconds * 1000; stepId = null;
                if (!changed) continue;
                long received = Wallet.Pdm.Revenue - revenue;
                if (received > 0) Notify.Show("PDM: +$" + received + " em parcelas no caixa da empresa.");
                int earned = Wallet.Pdm.Loans.Sum(l => l.Profit) - profit;
                if (earned > 0) try { achievements.Bump("cash_earned", earned); achievements.Save(); } catch (Exception ex) { Log.Error("pdm finance achievement after checkpoint", ex); }
            }
        }
        void Add(string title, string text, Action action)
        { var item = new NativeItem(title, text); item.Activated += (s, e) => { try { action(); } catch (Exception ex) { Log.Error("pdm finance menu", ex); Notify.Show("Consulta de financiamentos pendente; dados preservados."); } }; menu.Add(item); }
        public void OpenMenu()
        {
            Flush(); pool.HideAll(); menu.Clear(); if (!Wallet.Pdm.Valid) { Notify.Show("Dados PDM invalidos. Original preservado."); return; }
            var d = Wallet.Pdm; long owed = d.Loans.Sum(l => (long)l.Total - l.Paid);
            menu.Name = "Financiamentos / a receber $" + owed;
            Add("Novos contratos: " + options.DownPercent + "% entrada / " + options.Installments + " parcelas", "Acrescimo contratual " + options.InterestPercent + "% sobre cotacao com danos/divulgacao. Parcelas a cada " + options.PeriodSeconds + "s ativos; ate " + options.MaxActive + " contratos simultaneos. Abra Estoque e escolha Entregar financiado.", back);
            foreach (var l in d.Loans.AsEnumerable().Reverse())
            {
                var loan = l; Add((loan.Complete ? "[QUITADO] " : "[ATIVO] ") + loan.Asset.Name + " / " + loan.Asset.Plate,
                    "Total $" + loan.Total + " / entrada $" + loan.DownPayment + " / recebido $" + loan.Paid + " / a receber $" + (loan.Total - loan.Paid) + ". Parcelas " + loan.Payments + "/" + loan.Installments + (loan.Complete ? "." : "; proxima $" + loan.Payment + " em " + (loan.PeriodSeconds - loan.Elapsed) + "s ativos. No vencimento sem espaco no caixa: pagamento pendente ate liberar limite."), () => { });
            }
            Add("Atualizar contratos", "Tempo apenas no jogo ativo, sem receber pelo tempo offline. Parcelas entram no caixa empresarial, nao no banco pessoal. Esta subfase atende clientes adimplentes; inadimplencia/retomada fica para proxima entrega.", OpenMenu);
            Add("Voltar", "Empresa PDM / estoque.", back); menu.Visible = true;
        }
        public void Tick()
        { Sample(); if (pending < 5000 || unchecked(Game.GameTime - retryAt) < 0) return; try { Flush(); } catch (Exception ex) { retryAt = Game.GameTime + 5000; Log.Error("pdm finance checkpoint pending", ex); } }
        public void Save() { if (profile == Profile.Id) Flush(); }
        public void Reload() { profile = Profile.Id; last = Game.GameTime; retryAt = last; pending = 0; stepId = null; menu.Visible = false; }
        public void OnKey(KeyEventArgs e) { }
        public void Dispose() { try { Save(); } finally { menu.Visible = false; } }
    }
}
