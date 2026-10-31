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
        readonly PdmMenuView ui;
        public Action<string> OpenRepo;
        public int NewDefaultPercent;
        public bool Synced=>pending<1000;
        public string Name => "pdmfinance";
        readonly ObjectPool pool; readonly NativeMenu menu; readonly Action back; readonly AchievementSystem achievements; readonly PdmFinanceOptions options;
        string profile, stepId; long pending; int last, stepSeconds, retryAt;
        public PdmFinanceSystem(ObjectPool pool, PdmFinanceOptions options, AchievementSystem achievements, Action back)
        {
            this.pool = pool; this.options = options; this.achievements = achievements; this.back = back;
            menu = new NativeMenu("PDM / financiamentos", "ENTRADAS / PARCELAS / CONTRATOS"); ui=new PdmMenuView(pool,menu,ex=>{ui.Close();Log.Error(Name+" menu dispatch",ex);Notify.Show("Menu PDM interrompido; dados preservados. Tente abrir novamente.");}); ui.DefaultRefresh=OpenMenu; pool.Add(menu); Visuals.Register(menu, "pdm"); Reload();
        }
        void Sample()
        {
            int now = Game.GameTime, elapsed = unchecked(now - last); last = now;
            if (profile != Profile.Id || !Wallet.Pdm.Valid || !Wallet.Pdm.Owned || !Wallet.Pdm.Loans.Any(l => l.Collecting)) { pending = 0; stepId = null; return; }
            if (PdmMenuRuntime.WorldAllowed && elapsed > 0) pending += Math.Min(2000, elapsed);
        }
        public void Flush()
        {
            Sample(); if (profile != Profile.Id || !Wallet.Pdm.Valid) return;
            for(int batch=0;pending>=1000 && batch<2;batch++)
            {
                if (!Wallet.Pdm.Loans.Any(l => l.Collecting)) { pending = 0; stepId = null; return; }
                if (stepId == null) { stepId = Guid.NewGuid().ToString("N"); stepSeconds = (int)Math.Min(60, pending / 1000); }
                int oldDefaults = Wallet.Pdm.Loans.Count(l => l.Defaulted && !l.Settled);
                long revenue = Wallet.Pdm.Revenue; int profit = Wallet.Pdm.Loans.Sum(l => l.Profit);
                if (!Wallet.PdmFinanceWork(stepSeconds, stepId, out bool changed)) throw new InvalidOperationException("PDM finance checkpoint refused; elapsed time retained.");
                pending -= stepSeconds * 1000; stepId = null;
                if (!changed) continue;
                if (Wallet.Pdm.Loans.Count(l => l.Defaulted && !l.Settled) > oldDefaults) Notify.Show("PDM: cliente inadimplente. Abra Financiamentos / Retomada para recuperar o carro.");
                long received = Wallet.Pdm.Revenue - revenue;
                if (received > 0) Notify.Show("PDM: +$" + received + " em parcelas no caixa da empresa.");
                int earned = Wallet.Pdm.Loans.Sum(l => l.Profit) - profit;
                if (earned > 0) try { achievements.Bump("cash_earned", earned); achievements.Save(); } catch (Exception ex) { Log.Error("pdm finance achievement after checkpoint", ex); }
            }
        }
        void Add(string title, string text, Action action)
        { ui.Add(title, text, () => { try { action(); } catch (Exception ex) { ui.Close(); Log.Error("pdm finance menu", ex); Notify.Show("Consulta de financiamentos pendente; dados preservados."); } }); }
        public void OpenMenu()
        {
            Flush();if(!Wallet.Pdm.Valid){Notify.Show("Dados PDM invalidos; original preservado.");return;}
            ui.HideAll();ui.Begin(Name,back,true,OpenMenu);var d=Wallet.Pdm;
            menu.Name="Contratos / a receber $"+PdmBusinessRules.Receivable(d);
            ui.Add("Novos contratos / "+options.DownPercent+"% entrada","Acrescimo "+options.InterestPercent+"%, "+options.Installments+" parcelas a cada "+options.PeriodSeconds+"s ativos. Limite "+options.MaxActive+"; risco por parcela "+NewDefaultPercent+"%. Inicie pelo estoque.",()=>{},false);
            foreach(var loan in d.Loans.AsEnumerable().Reverse()){
                string id=loan.Id,state=loan.Repossessed?"RETOMADO":loan.Complete?"QUITADO":loan.Defaulted?"EM ATRASO":"ATIVO";
                Add("["+state+"] "+loan.Asset.Name+" / "+loan.Asset.Plate,"Recebido $"+loan.Paid+"; a receber $"+loan.Receivable+". Selecione para consultar as condicoes.",()=>Details(id));
            }
            if(d.Loans.Count==0)ui.Add("Nenhum contrato","Entregas financiadas confirmadas aparecem aqui.",()=>{},false);
            Add("Atualizar contratos","Recebimentos apenas pelo tempo de jogo ativo; nao gera parcelas offline.",OpenMenu);
            Add("Voltar","Empresa.",back);ui.Show();
        }
        void Details(string id)
        {
            Flush();var loan=Wallet.Pdm.Loans.Find(l=>l.Id==id);if(loan==null){OpenMenu();return;}
            ui.HideAll();ui.Begin(Name+":"+id,OpenMenu,true,()=>Details(id));menu.Name=loan.Asset.Name+" / "+loan.Asset.Plate;
            ui.Add("Total contratado: $"+loan.Total,"Entrada $"+loan.DownPayment+" ("+loan.DownPercent+"%); acrescimo "+loan.InterestPercent+"%; "+loan.Installments+" parcelas.",()=>{},false);
            ui.Add("Recebido: $"+loan.Paid+" / a receber $"+loan.Receivable,"Parcelas pagas "+loan.Payments+"/"+loan.Installments+". Valores recebidos entram no caixa empresarial uma unica vez.",()=>{},false);
            string state=loan.Repossessed?"Retomado / recebivel encerrado":loan.Complete?"Quitado":loan.Defaulted?"Inadimplente / parcelas suspensas":"Ativo / proxima parcela $"+loan.Payment;
            ui.Add(state,loan.Collecting?"Vencimento em "+(loan.PeriodSeconds-loan.Elapsed)+"s ativos. Risco contratado por parcela "+loan.DefaultPercent+"%. Falta de espaco no caixa adia recebimento, sem perder o valor.":"Contrato preservado no historico. Retomada nao gera um premio de dinheiro.",()=>{},false);
            if(loan.Defaulted && !loan.Settled && OpenRepo!=null)Add("Iniciar retomada","Revise a operacao, reserve vaga no estoque e transporte o proprio veiculo de volta.",()=>OpenRepo(id));
            Add("Voltar","Contratos.",OpenMenu);ui.Show();
        }
        public void Tick()
        {
            ui.Tick(PdmMenuRuntime.Allowed); Sample(); if (pending < 5000 || unchecked(Game.GameTime - retryAt) < 0) return; try { Flush(); } catch (Exception ex) { retryAt = Game.GameTime + 5000; Log.Error("pdm finance checkpoint pending", ex); } }
        public void Save() { if (profile == Profile.Id) Flush(); }
        public void Reload() { ui.Reset(); profile = Profile.Id; last = Game.GameTime; retryAt = last; pending = 0; stepId = null; menu.Visible = false; }
        public void OnKey(KeyEventArgs e) { }
        public void Dispose() { ui.Reset(); try { Save(); } finally { menu.Visible = false; } }
    }
}
