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
        readonly PdmMenuView ui;
        public PdmPremisesOptions Premises { get; }
        public PdmSecurityOptions Security { get; }
        public PdmEnterpriseOptions Enterprise {get;}
        public Action OpenServices, OpenSecurity;
        public bool Synced=>pending<1000;
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
            var enterpriseDefault=PdmEnterpriseDefaults.Load();Enterprise=Store.LoadDef("Jobs/PdmEnterprise.json",()=>enterpriseDefault);
            if(Enterprise?.Valid!=true){Log.Write("Invalid PdmEnterprise.json; original preserved; embedded defaults used.");Enterprise=enterpriseDefault;}
            options=PdmEnterpriseDefaults.Merge(options,Enterprise);
            menu = new NativeMenu("PDM / equipe", "SALARIOS / OFICINA / DIVULGACAO"); ui=new PdmMenuView(pool,menu,ex=>ui.Problem("Falha na equipe","Confira folha e funcionarios antes de repetir a operacao.",ex,OpenMenu)); ui.DefaultRefresh=OpenMenu; pool.Add(menu); Visuals.Register(menu, "pdm"); Reload();
        }
        void Sample()
        {
            int now = Game.GameTime, elapsed = unchecked(now - last); last = now;
            if (profile != Profile.Id || !Wallet.Pdm.Valid || !Wallet.Pdm.Owned || Wallet.Pdm.Staff.Employees.Count == 0) { pending = 0; stepId = null; return; }
            if (PdmMenuRuntime.WorldAllowed && elapsed > 0) pending += Math.Min(2000, elapsed);
        }
        public void Flush()
        {
            Sample();
            if (profile != Profile.Id || !Wallet.Pdm.Valid || Wallet.Pdm.Staff.Employees.Count == 0) return;
            for(int batch=0;pending>=1000 && batch<2;batch++)
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
            ui.Add(title, text, () => { try { action(); } catch (Exception ex) { ui.Problem("Falha na equipe","Confira folha e funcionarios antes de repetir a operacao.",ex,OpenMenu); } });
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
            Flush();if(!Wallet.Pdm.Valid){Notify.Show("Dados PDM invalidos; original preservado.");return;}
            ui.HideAll();ui.Begin(Name,back,true,OpenMenu);var d=Wallet.Pdm;
            menu.Name="Equipe / caixa $"+d.Cash+" / divida $"+PdmBusinessRules.Wages(d);
            if(!d.Owned){Add("Adquira a empresa primeiro","Voltar a gestao.",back);ui.Show();return;}
            foreach(var offer in options.Roles){var role=offer;var employee=d.Staff.Employees.Find(e=>e.Role==role.Id);
                Add(role.Name+" / "+(employee==null?"nao contratado":employee.Owed>0?"salario pendente $"+employee.Owed:"em dia"),"Salario $"+role.Wage+" por "+options.PeriodSeconds+"s ativos. Selecione para consultar, contratar ou gerir.",()=>Role(role.Id));}
            long debt=PdmBusinessRules.Wages(d);
            ui.AddConfirm("Quitar folha e acertos / $"+debt,"Paga todos os salarios vencidos e ex-funcionarios em uma unica operacao do caixa empresarial.",()=>Change(()=>Wallet.PdmPayAll(Guid.NewGuid().ToString("N"),out _,debt)),debt>0 && debt<=d.Cash);
            ui.AddConfirm("Demitir toda a equipe","Encerra todos os cargos juntos; salarios, proporcionais e rescisao ficam como acerto a pagar, sem perdao de divida.",()=>Change(()=>PdmStaffTransactions.FireAll(Wallet.EnterpriseWallet(),Guid.NewGuid().ToString("N"),Wallet.Save,out _)),d.Staff.Employees.Count>0);
            Add("Melhorias / oficina e divulgacao","Custos e efeitos, sem cobranca repetida de melhoria adquirida.",Upgrades);
            if(OpenSecurity!=null)Add("Segurancas / postos","Efetivo e defesa local.",OpenSecurity);
            if(OpenServices!=null)Add("Mecanico / zeladoria","Atendimento presencial no showroom.",OpenServices);
            Add("Voltar","Gestao da empresa.",back);ui.Show();
        }
        public void OpenRole(string roleId)=>Role(roleId);
        void Role(string roleId)
        {
            Flush();var role=options.Roles.Find(r=>r.Id==roleId);if(role==null){OpenMenu();return;}
            ui.HideAll();ui.Begin(Name+":"+roleId,OpenMenu,true,()=>Role(roleId));var d=Wallet.Pdm;var e=d.Staff.Employees.Find(x=>x.Role==roleId);
            menu.Name=role.Name;
            if(e==null){
                ui.Add("Salario $"+role.Wage+" / "+options.PeriodSeconds+"s ativos","Sem cobranca offline. Atraso suspende beneficios; salarios sao pagos pelo caixa.",()=>{},false);
                string required=role.Requires==null?"Sem cargo previo.":"Requer "+options.Roles.Find(r=>r.Id==role.Requires)?.Name+" contratado e em dia.";
                string refusal=PdmStaffTransactions.HireReason(d,options,roleId);
                Add("Contratar / $"+role.HiringCost,refusal??(required+" Debita caixa empresarial. Contratacao nao paga salarios futuros."),()=>{
                    Flush();if(!office())return;
                    string reason=PdmStaffTransactions.HireReason(Wallet.Pdm,options,roleId);
                    if(reason!=null){ui.Problem("Contratacao recusada",reason,null,()=>Role(roleId));return;}
                    ui.Confirm("Contratar "+role.Name,"Custo $"+role.HiringCost+" pelo caixa; salario $"+role.Wage+" por "+options.PeriodSeconds+"s ativos. "+required,()=>{
                        Flush();if(!office())return;var problem=PdmStaffTransactions.HireReason(Wallet.Pdm,options,roleId);
                        if(problem!=null){ui.Problem("Contratacao recusada",problem,null,()=>Role(roleId));return;}
                        if(!Wallet.PdmHire(options,roleId,Guid.NewGuid().ToString("N"),out bool changed)){ui.Problem("Contratacao recusada","Confira caixa, folha e cargo novamente; nenhum funcionario novo confirmado.",null,()=>Role(roleId));return;}
                        if(changed)Notify.Show("Contratado: "+role.Name+". O funcionario chega ao posto quando a area estiver carregada.");Role(roleId);
                    },()=>Role(roleId));
                });
            }else{
                string id=e.Id;int due=e.Owed;
                ui.Add("Salario devido: $"+due,"Proximo vencimento em "+(e.PeriodSeconds-e.Seconds)+"s ativos. Divida de ex-funcionarios: $"+d.Staff.FormerDebt+".",()=>{},false);
                ui.AddConfirm("Pagar salario / $"+due,"Quita o saldo vencido, sem adiantar ou cobrar outro periodo.",()=>Change(()=>PayExpected(roleId,due)),due>0 && d.Cash>=due);
                long estimate=(long)e.Owed+((long)e.Seconds*e.Wage+e.PeriodSeconds-1)/e.PeriodSeconds+e.Severance;
                ui.AddConfirm("Demitir "+e.Name,"Acerto estimado $"+estimate+": salarios vencidos, proporcional ate confirmar e multa de $"+e.Severance+". Vira divida de ex-funcionarios; nao sai dinheiro agora. Marketing: demitir do maior nivel ao menor.",()=>Change(()=>Wallet.PdmFire(id,Guid.NewGuid().ToString("N"),out _)));
            }
            ui.Add("Beneficio comercial ativo: +"+PdmStaffTransactions.BonusPercent(d)+"%","Usa o maior nivel em dia; nao soma todos os cargos. Mecanico e oficina reduzem o custo de reparo. Zeladoria e seguranca nao geram bonus de venda.",()=>{},false);
            Add("Voltar","Equipe.",OpenMenu);ui.Show();
        }
        void Upgrades()
        {
            Flush();ui.HideAll();ui.Begin(Name+":upgrades",OpenMenu,true,Upgrades);var d=Wallet.Pdm;var s=d.Staff;menu.Name="Melhorias / caixa $"+d.Cash;
            ui.AddConfirm(s.Workshop?"Oficina adquirida":"Oficina / $"+options.WorkshopCost,"Com mecanico em dia, reduz pela metade o reparo do estoque. Compra unica.",()=>Change(()=>Wallet.PdmUpgrade(options,"workshop",Guid.NewGuid().ToString("N"),out _)),!s.Workshop && d.Cash>=options.WorkshopCost);
            ui.AddConfirm(s.Advertising?"Divulgacao adquirida":"Divulgacao / $"+options.AdvertisingCost,"Acrescenta 2% ao bonus de marketing em dia. Compra unica; nao cria venda ou dinheiro automaticamente.",()=>Change(()=>Wallet.PdmUpgrade(options,"advertising",Guid.NewGuid().ToString("N"),out _)),!s.Advertising && d.Cash>=options.AdvertisingCost);
            ui.AddConfirm("Quitar ex-funcionarios / $"+s.FormerDebt,"Acerto pendente bloqueia contratacoes e beneficios ate pagamento integral.",()=>Change(()=>PayExpected("former",s.FormerDebt)),s.FormerDebt>0 && d.Cash>=s.FormerDebt);
            ui.Add("Historico da equipe","Salarios pagos $"+s.Paid+"; contratacoes $"+s.HiringSpent+"; melhorias $"+s.UpgradeSpent+".",()=>{},false);
            Add("Voltar","Equipe.",OpenMenu);ui.Show();
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
