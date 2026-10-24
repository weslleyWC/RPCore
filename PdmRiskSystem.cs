using System;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
namespace RPCore
{
    public sealed class PdmRiskSystem : ISystem
    {
        public string Name => "pdmrisk";
        readonly ObjectPool pool; readonly NativeMenu menu; readonly Vector3 office; readonly Func<bool> officeAction; readonly Action back;
        readonly PdmRiskOptions options;
        string profile, stepId; int last, retryAt, pending, stepSeconds; bool away, stepAway;
        bool Running => Wallet.Pdm.Valid && Wallet.Pdm.Owned && (Wallet.Pdm.Risk.SafeOpen || Wallet.Pdm.Risk.Policy != null);
        public PdmRiskSystem(ObjectPool pool, Vector3 office, Func<bool> officeAction, Action back)
        {
            this.pool = pool; this.office = office; this.officeAction = officeAction; this.back = back;
            var defaults = PdmRiskDefaults.Load(); options = Store.LoadDef("Jobs/PdmRisk.json", () => defaults);
            if (options == null || !options.Valid) { Log.Write("Invalid PdmRisk.json; embedded defaults used, original retained."); options = defaults; }
            menu = new NativeMenu("PDM / cofre e seguro", "PROTECAO / APOLICE / OCORRENCIAS"); pool.Add(menu); Visuals.Register(menu, "pdm"); Reload();
        }
        void Sample()
        {
            int now = Game.GameTime, elapsed = unchecked(now - last); last = now;
            if (profile != Profile.Id || !Running) { pending = 0; stepId = null; return; }
            bool currentAway = Game.Player.Character.Position.DistanceTo(office) >= 150;
            if (currentAway != away)
            {
                Drain();
                pending = 0; away = currentAway;
            }
            if (!currentAway && Wallet.Pdm.Risk.ExposedSeconds > 0 && !Wallet.PdmRiskReturn(Guid.NewGuid().ToString("N"), out _)) throw new InvalidOperationException("PDM return checkpoint refused.");
            if (!Game.IsPaused && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && !Game.Player.Character.IsDead && elapsed > 0) pending += Math.Min(2000, elapsed);
        }
        void Drain()
        {
            while (pending >= 1000)
            {
                if (!Running) { pending = 0; stepId = null; return; }
                if (stepId == null) { stepId = Guid.NewGuid().ToString("N"); stepSeconds = Math.Min(60, pending / 1000); stepAway = away; }
                long stolen = Wallet.Pdm.Risk.Stolen, premiums = Wallet.Pdm.Risk.Premiums; bool insured = Wallet.Pdm.Risk.Policy != null;
                if (!Wallet.PdmRiskWork(options, stepSeconds, stepAway, stepId, out bool changed)) throw new InvalidOperationException("PDM risk checkpoint refused; time retained.");
                pending -= stepSeconds * 1000; stepId = null;
                if (changed && Wallet.Pdm.Risk.Stolen > stolen)
                { var incident = Wallet.Pdm.Risk.Incidents.Last(); Notify.Show("PDM: cofre aberto sofreu perda de $" + incident.Loss + "; seguro restituiu $" + incident.Reimbursed + ". Cofre fechado. Consulte ocorrencias."); }
                if (changed && insured && Wallet.Pdm.Risk.Policy == null) Notify.Show("PDM: seguro encerrado por falta de caixa/limite contabil. Contrate novamente no escritorio.");
                else if (changed && Wallet.Pdm.Risk.Premiums > premiums) Notify.Show("PDM: seguro renovado pelo caixa empresarial.");
            }
        }
        public void Flush() { if (profile != Profile.Id) return; Sample(); Drain(); }
        void Add(string title, string description, Action action)
        { var row = new NativeItem(title, description); row.Activated += (s, e) => { try { action(); } catch (Exception ex) { Log.Error("pdm risk action", ex); Notify.Show("Acao nao confirmada; dados preservados."); } }; menu.Add(row); }
        void Change(Func<bool> action)
        { Flush(); if (!officeAction()) return; if (!action()) Notify.Show("Confira caixa, apolice, dados atualizados ou operacao pendente."); OpenMenu(); }
        public void OpenMenu()
        {
            Flush(); pool.HideAll(); menu.Clear(); if (!Wallet.Pdm.Valid) { Notify.Show("Dados PDM invalidos; original preservado."); return; }
            var d = Wallet.Pdm; var r = d.Risk; var policy = r.Policy;
            Add(r.SafeOpen ? "Fechar cofre / interromper risco" : "Abrir cofre / habilitar risco ao se afastar", "Abertura VOLUNTARIA: ao ficar a 150m por " + options.ExposureSeconds + "s ativos, com caixa acima de $" + options.MinimumCash + ", pode perder " + options.LossPercent + "%. Usa o caixa existente, sem criar saldo de cofre separado.", () => Change(() => Wallet.PdmSetSafe(!r.SafeOpen, Guid.NewGuid().ToString("N"), out _)));
            Add("Protecao " + PdmRiskTransactions.Security(d) + "/100 / chance " + PdmRiskTransactions.Chance(d, options) + "%", "Cameras: 30; cada seguranca contratado e sem atraso: 35. A chance vale por periodo de exposicao, somente com cofre aberto. Fechado: sem este risco.", () => { });
            if (policy != null)
            {
                Add(policy.Name + " / cobertura $" + policy.Limit, "Renova por $" + policy.Premium + " em " + (policy.PeriodSeconds - policy.Seconds) + "s ativos. Caixa insuficiente encerra cobertura. Sem debito no banco pessoal.", () => { });
                Add("Cancelar seguro / sem estorno", "Encerra cobertura e renovacoes futuras, sem devolver o premio do periodo.", () => Change(() => Wallet.PdmCancelInsurance(Guid.NewGuid().ToString("N"), out _)));
            }
            foreach (var offer in options.Plans)
            {
                var plan = offer;
                Add((policy?.PlanId == plan.Id ? "Ativo: " : "Contratar/trocar: ") + plan.Name + " / $" + plan.Premium, "Paga AGORA e renova por esse valor a cada " + options.PeriodSeconds + "s ativos. Cobertura integral somente se a perda nao exceder $" + plan.Limit + "; acima do limite, sem indenizacao parcial. Troca paga premio completo, sem estorno anterior.", () => Change(() => Wallet.PdmInsure(options, plan.Id, Guid.NewGuid().ToString("N"), out _)));
            }
            Add("Premios $" + r.Premiums + " / perdas $" + r.Stolen + " / restituidos $" + r.Reimbursed, "Historico de ate 20 ocorrencias. Reembolso nunca excede a perda real; nao ha botao de indenizacao livre.", () => { });
            foreach (var incident in r.Incidents.AsEnumerable().Reverse()) Add("Perda $" + incident.Loss + " / seguro $" + incident.Reimbursed, "Protecao no evento " + incident.Security + "/100; apolice " + (incident.PolicyName ?? "nenhuma") + ", limite $" + incident.Limit + ". Cofre fechado automaticamente apos a ocorrencia.", () => { });
            Add("GPS: escritorio PDM", "Abrir/fechar/contratar apenas no escritorio externo.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, office.X, office.Y); menu.Visible = false; });
            Add("Voltar", "Empresa PDM.", back); menu.Visible = true;
        }
        public void Tick()
        { if (unchecked(Game.GameTime - retryAt) < 0) return; try { Sample(); if (pending >= 5000) Drain(); } catch (Exception ex) { retryAt = Game.GameTime + 5000; Log.Error("pdm risk checkpoint", ex); } }
        public void Save() { if (profile == Profile.Id) Flush(); }
        public void Reload() { profile = Profile.Id; pending = 0; stepId = null; last = retryAt = Game.GameTime; away = Game.Player.Character.Position.DistanceTo(office) >= 150; menu.Visible = false; }
        public void OnKey(KeyEventArgs e) { }
        public void Dispose() { try { Save(); } finally { menu.Visible = false; } }
    }
}
