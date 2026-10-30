using System;
using System.Linq;
using System.Windows.Forms;
using GTA;
using LemonUI;
using LemonUI.Menus;

namespace RPCore
{
    public sealed class PdmStaffSystem : ISystem
    {
        readonly PersonalServiceMenu ui;
        public PdmPremisesOptions Premises { get; }
        public PdmSecurityOptions Security { get; }
        public Action OpenServices, OpenSecurity;
        public string Name => "pdmstaff";
        readonly ObjectPool pool; readonly NativeMenu menu; readonly Func<bool> office; readonly Action back;
        readonly PdmStaffOptions options;
        string profile, stepId; int last, retryAt, stepSeconds; long pending;
        public PdmStaffSystem(ObjectPool pool, Func<bool> office, Action back)
        {
            this.pool = pool; this.office = office; this.back = back;
            var defaults = PdmStaffDefaults.Load(); options = Store.LoadDef("Jobs/PdmStaff.json", () => defaults);
            if (options == null || !options.Valid) { Log.Write("Invalid PdmStaff.json; embedded defaults used, original retained."); options = defaults; }
            var premisesDefaults = PdmPremisesDefaults.Load(); Premises = Store.LoadDef("Jobs/PdmPremises.json", () => premisesDefaults);
            if (Premises == null || !Premises.Valid) { Log.Write("Invalid PdmPremises.json; embedded defaults used, original retained."); Premises = premisesDefaults; }
            options = PdmPremisesDefaults.Merge(options, Premises);
            var securityDefaults = PdmSecurityDefaults.Load(); Security = Store.LoadDef("Jobs/PdmSecurity.json", () => securityDefaults);
            if (Security == null || !Security.Valid) { Log.Write("Invalid PdmSecurity.json; embedded defaults used, original retained."); Security = securityDefaults; }
            options = PdmSecurityDefaults.Merge(options, Security);
            menu = new NativeMenu("PDM / equipe", "SALARIOS / OFICINA / DIVULGACAO"); ui=new PersonalServiceMenu(menu,ex=>{ui.Close();Log.Error(Name+" menu dispatch",ex);Notify.Show("Menu PDM interrompido; dados preservados. Tente abrir novamente.");}); pool.Add(menu); Visuals.Register(menu, "pdm"); Reload();
        }
        void Sample()
        {
            int now = Game.GameTime, elapsed = unchecked(now - last); last = now;
            if (profile != Profile.Id || !Wallet.Pdm.Valid || !Wallet.Pdm.Owned || Wallet.Pdm.Staff.Employees.Count == 0) { pending = 0; stepId = null; return; }
            if (!Game.IsPaused && !Game.IsMissionActive && !Game.Player.Character.IsDead && elapsed > 0) pending += Math.Min(2000, elapsed);
        }
        public void Flush()
        {
            Sample();
            if (profile != Profile.Id || !Wallet.Pdm.Valid || Wallet.Pdm.Staff.Employees.Count == 0) return;
            while (pending >= 1000)
            {
                if (stepId == null) { stepId = Guid.NewGuid().ToString("N"); stepSeconds = (int)Math.Min(60, pending / 1000); }
                int oldOwed = Wallet.Pdm.Staff.Employees.Count(e => e.Owed > 0);
                if (!Wallet.PdmWork(stepSeconds, stepId, out bool changed)) throw new InvalidOperationException("PDM payroll checkpoint refused; accrued time retained.");
                pending -= stepSeconds * 1000; stepId = null;
                if (changed && Wallet.Pdm.Staff.Employees.Count(e => e.Owed > 0) > oldOwed) Notify.Show("PDM: salarios venceram. Quite pelo menu Equipe para reativar os beneficios.");
            }
        }
        void Add(string title, string text, Action action)
        {
            ui.Add(title, text, () => { try { action(); } catch (Exception ex) { ui.Close(); Log.Error("pdm staff action", ex); Notify.Show("Operacao de equipe nao confirmada. Dados preservados."); } });
        }
        void Change(Func<bool> mutation)
        {
            Flush(); if (!office()) return;
            if (!mutation()) Notify.Show("Confira caixa, salarios/acertos pendentes, requisitos ou contrato ativo.");
            OpenMenu();
        }
        bool PayExpected(string role, int expected)
        {
            int owed = role == "former" ? Wallet.Pdm.Staff.FormerDebt : Wallet.Pdm.Staff.Employees.Find(e => e.Role == role)?.Owed ?? 0;
            if (owed != expected) { Notify.Show("Saldo de salarios atualizado. Confira o novo valor antes de pagar."); return false; }
            return Wallet.PdmPay(role, Guid.NewGuid().ToString("N"), out _);
        }
        public void OpenMenu()
        {
            Flush(); pool.HideAll(); ui.Begin("pdm",null,false);
            if (!Wallet.Pdm.Valid) { Notify.Show("Dados PDM invalidos. Original preservado."); return; }
            var d = Wallet.Pdm; var s = d.Staff;
            menu.Name = "Equipe / caixa $" + d.Cash + " / divida $" + ((long)s.FormerDebt + s.Employees.Sum(e => (long)e.Owed));
            if (!d.Owned) { Add("Adquira a empresa primeiro", "Voltar a gestao PDM.", back); menu.Visible = true; return; }
            foreach (var offer in options.Roles)
            {
                var role = offer; var employee = s.Employees.Find(e => e.Role == role.Id);
                if (employee == null) Add("Contratar " + role.Name + ": $" + role.HiringCost, "Salario $" + role.Wage + " a cada " + options.PeriodSeconds + " segundos ativos. " + (role.Requires == null ? "" : "Requer " + role.Requires + ". ") + (role.Id == "mechanic" ? "Com oficina, reparos do estoque pela metade." : PdmSecurityPolicy.IsGuard(role.Id) ? "Posto presencial e defesa local; salario atrasado suspende o atendimento." : role.Id == "cleaner" ? "Limpeza presencial dos carros expostos, sem taxa adicional por servico." : "Bonus de venda: " + role.SaleBonusPercent + "% (maior nivel, sem somar)."), () => Change(() => Wallet.PdmHire(options, role.Id, Guid.NewGuid().ToString("N"), out _)));
                else
                {
                    string id = employee.Id;
                    Add(employee.Name + " / salario devido $" + employee.Owed, "Proximo vencimento em " + (employee.PeriodSeconds - employee.Seconds) + "s ativos; salario $" + employee.Wage + ". Sem debito automatico no banco pessoal.", () => { });
                    Add("Pagar " + employee.Name + ": $" + employee.Owed, "Quita o saldo devido com caixa empresarial e reativa o beneficio.", () => Change(() => PayExpected(role.Id, employee.Owed)));
                    Add("Demitir " + employee.Name, "Transfere salarios devidos, proporcional do periodo e multa de $" + employee.Severance + " ao acerto pendente. Divulgacao: demita do maior nivel para o menor.", () => Change(() => Wallet.PdmFire(id, Guid.NewGuid().ToString("N"), out _)));
                }
            }
            if (OpenSecurity != null) Add("Seguranca / postos presenciais", "Consultar efetivo e retornar os guardas aos postos.", OpenSecurity);
            if (OpenServices != null) Add("Equipe presencial / servicos do showroom", "Solicitar mecanico/zeladoria nos carros expostos.", OpenServices);
            Add("Quitar ex-funcionarios: $" + s.FormerDebt, "Acerto pendente bloqueia novas contratacoes e beneficios ate o pagamento integral.", () => Change(() => PayExpected("former", s.FormerDebt)));
            Add(s.Workshop ? "Oficina: adquirida" : "Melhorar oficina: $" + options.WorkshopCost, "Mecanico contratado e sem atraso + oficina = metade do custo de reparar estoque. Nao repara carro pessoal.", () => Change(() => Wallet.PdmUpgrade(options, "workshop", Guid.NewGuid().ToString("N"), out _)));
            Add(s.Advertising ? "Divulgacao: adquirida" : "Melhorar divulgacao: $" + options.AdvertisingCost, "Acrescenta 2% a venda quando ha profissional de divulgacao sem atraso. Nao gera dinheiro/cliente automaticamente.", () => Change(() => Wallet.PdmUpgrade(options, "advertising", Guid.NewGuid().ToString("N"), out _)));
            Add("Beneficios ativos: venda +" + PdmStaffTransactions.BonusPercent(d) + "%", "Pagos: $" + s.Paid + " / contratacoes: $" + s.HiringSpent + " / melhorias: $" + s.UpgradeSpent + ". Tempo so durante jogo ativo, sem cobranca offline. Reabra para atualizar os valores.", OpenMenu);
            Add("Voltar", "Gestao da empresa.", back); menu.Visible = true;
        }
        public void Tick()
        {
            ui.Tick(PdmMenuRuntime.Allowed);
            Sample(); if (pending < 5000 || unchecked(Game.GameTime - retryAt) < 0) return;
            try { Flush(); } catch (Exception ex) { retryAt = Game.GameTime + 5000; Log.Error("pdm payroll pending", ex); }
        }
        public void Save() { if (profile == Profile.Id) Flush(); }
        public void Reload() { ui.Reset(); menu.Visible = false; profile = Profile.Id; last = Game.GameTime; retryAt = last; pending = 0; stepId = null; }
        public void OnKey(KeyEventArgs e) { }
        public void Dispose() { ui.Reset(); try { Save(); } finally { menu.Visible = false; } }
    }
}
