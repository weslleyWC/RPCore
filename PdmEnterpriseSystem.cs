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
    public sealed partial class PdmEnterpriseSystem : ISystem
    {
        public string Name=>"pdmenterprise";
        public event Action<string,string> Activity;
        readonly ObjectPool pool;readonly ShiftGate gate;readonly PdmOptions company;readonly PdmEnterpriseOptions options;readonly Func<bool> office;readonly Action back;
        readonly NativeMenu menu;readonly PdmMenuView ui;
        public Func<IEnumerable<KeyValuePair<PdmStock,Vehicle>>> Displays;
        public Func<string,Vehicle> GetDisplay;
        public Func<Ped> Cleaner;
        public Func<IEnumerable<KeyValuePair<string,Ped>>> RolePeds;public Action<string> StaffRole;
        public Func<string,bool> PrepareStock;
        public Func<string,bool> ExteriorStock;
        public Func<IEnumerable<string>> RentalModels;
        public Action StaffMenu,CollectionMenu,ShowroomMenu,RentalMenu;
        int last,nextWorld,nextTick;long pendingSeconds;string profile,tickReceipt;int tickSeconds;
        PdmEnterpriseData Data=>PdmEnterpriseRules.View(Wallet.Pdm);
        bool Safe=>PdmMenuRuntime.Allowed && PersonalServiceMenu.SafeFoot;
        bool Local=>GameplayContext.BackgroundReady && PdmMenuRuntime.WorldAllowed && Game.Player.Character.Position.DistanceTo(Pos(company.Office))<180;
        bool Valid(Entity e)=>e!=null && PdmAssets.Valid(e);
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        public PdmEnterpriseSystem(ObjectPool pool,ShiftGate gate,PdmOptions company,PdmEnterpriseOptions options,Func<bool> office,Action back)
        {
            this.pool=pool;this.gate=gate;this.company=company;this.options=options;this.office=office;this.back=back;
            menu=new NativeMenu("PDM / empresa","GESTAO E OPERACAO");ui=new PdmMenuView(pool,menu,ex=>ui.Problem("Operacao empresarial pendente","Confira estoque e extrato antes de repetir.",ex,OpenMenu));ui.DefaultRefresh=OpenMenu;pool.Add(menu);Visuals.Register(menu,"pdm");profile=Profile.Id;last=Game.GameTime;
        }
        void Tell(string title,string text){Notify.Show("PDM: "+text);Activity?.Invoke("PDM / "+title,text);}
        void Page(string id,string title,Action parent=null,Action refresh=null){ui.HideAll();ui.Begin(Name+":"+id,parent??OpenMenu,true,refresh??OpenMenu);menu.Name=title;}
        void Add(string title,string description,Action action,bool enabled=true)=>ui.Add(title,description,()=>{try{action();}catch(Exception ex){ui.Problem("Falha na empresa","Confira estoque, contratos e extrato.",ex,OpenMenu);}},enabled);
        void Change(Func<bool> action,Action refresh=null)
        {if(!office())return;if(!action()){ui.Problem("Operacao recusada","Confira caixa, estoque, salario do coletor e contratos pendentes. Nada e repetido automaticamente.",null,refresh??OpenMenu);return;}(refresh??OpenMenu)();}
        public void OpenMenu()
        {
            if(!PdmMenuRuntime.Allowed)return;Page("root","PDM / OPERACAO COMPLETA",back);
            ui.Add("CEO: "+Profile.Id+" / reputacao "+Data.Reputation+"/100","Valor estimado $"+PdmEnterpriseRules.Valuation(Wallet.Pdm)+". Humor "+Data.Mood+"/100; limpeza "+Data.Cleanliness+"/100; dia empresarial "+Data.Day+". Caixa $"+Wallet.Pdm.Cash+".",()=>{},false);
            Add("Fornecedor / categorias / quilometragem","Ofertas diarias de carros instalados; selecionar, revisar e encomendar para retirada.",()=>OpenCatalogue());
            Add("Coletor / compras e retirada automatica","Funcionario pode adquirir carros e trazer encomendas para a empresa. Orçamento e extrato explicitos.",OpenAutomation);
            Add("Melhorias / veiculos e instalacoes","Caminhao, carretas de duas/seis unidades, vidro, luzes, lavadora e bomba.",OpenFacilities);
            Add("Promocao / reputacao","Cartoes, doacoes e patrocinio de corrida.",OpenPromotion);
            Add("Oficina / lavar / abastecer / preparar","Servico manual e modificacoes dos veiculos proprios da empresa.",OpenServices);
            Add("Vendedor particular","Inspecionar/testar, negociar e comprar veiculo presencial.",OpenSeller);
            Add("Locadora / pedidos diarios",Data.RentalOrders.Count+" pedidos; precisa fornecer duas unidades proprias do mesmo modelo.",OpenRentalOrders);
            ui.AddConfirm(Data.Open?"Fechar atendimento da loja":"Abrir atendimento da loja","Controla vendas automaticas e atendimento do showroom; nao impede consultar/administrar a empresa.",()=>Change(()=>PdmEnterpriseTransactions.ToggleOpen(Wallet.EnterpriseWallet(),Guid.NewGuid().ToString("N"),Wallet.Save,out _)));
            Add("Equipe / salarios", "Contratar coletor, guardas, mecanico, zeladoria e marketing.",()=>StaffMenu?.Invoke());
            Add("Voltar","Empresa.",back);ui.Show();
        }
        public void OpenAutomation()
        {
            Page("auto","PDM / COLETOR E ENTREGAS");
            var worker=Wallet.Pdm.Staff.Employees.Find(v=>v.Role=="collector");
            ui.Add(worker==null?"Coletor: nao contratado":"Coletor: "+(worker.Owed>0?"salario devido $"+worker.Owed:"em dia"),"Contratacao/salario ficam em Equipe. A cada "+options.SourceSeconds+"s ativos procura carro novo, respeitando orcamento e capacidade. Compras saem do caixa.",()=>{},false);
            Add("Contratar / pagar coletor","Abrir equipe; cargo Coletor de veiculos.",()=>StaffMenu?.Invoke());
            Add("Orcamento por compra: $"+Data.ProcurementBudget,"Zero pausa somente compras novas autonomas. Coleta de encomendas ja pagas pode ser solicitada separadamente.",()=>{string input=Game.GetUserInput(WindowTitle.EnterMessage60,Data.ProcurementBudget.ToString(),10);if(PhoneBankRules.Amount(input,out int amount) || input=="0")Change(()=>PdmEnterpriseTransactions.Budget(Wallet.EnterpriseWallet(),input=="0"?0:amount,Guid.NewGuid().ToString("N"),Wallet.Save,out _),OpenAutomation);});
            if(Data.Auto==null){
                var pending=Wallet.Pdm.Stock.Where(v=>v.AwaitingCollection).Take(6).Select(v=>v.Id).ToArray();
                ui.AddConfirm("Solicitar retirada automatica / "+pending.Length,"Traz os pedidos ja pagos. Taxa de servico $"+((Data.Upgrades.Contains("truck") && Data.Upgrades.Contains("carrier2"))?0:options.AutoPickupFee*pending.Length)+" pelo caixa; nao recompra as unidades. Entrega em sequencia ao patio.",()=>Change(()=>PdmEnterpriseTransactions.BeginAuto(Wallet.EnterpriseWallet(),options,pending,Guid.NewGuid().ToString("N"),Wallet.Save,out _),OpenAutomation),pending.Length>0);
                Add("Procurar veiculo agora","Mesmas regras automaticas: coletor em dia, caixa, orcamento e vaga. O valor exato fica no extrato.",()=>{TrySource(true);OpenAutomation();});
            }else{
                var auto=Data.Auto;ui.Add("Entrega: "+(auto.AtGate?"chegando ao patio":"em viagem"),auto.SourcedCar!=null?auto.SourcedCar.Name+" / compra $"+auto.SourcedCar.Cost:"Unidades recebidas: "+auto.Delivered+"/"+auto.StockIds.Count,()=>{},false);
                ui.AddConfirm("Cancelar entrega automatica","Taxa ja executada nao e devolvida. Carro ja adquirido permanece como encomenda paga para retirada manual; nao desaparece dinheiro/patrimonio.",()=>Change(()=>PdmEnterpriseTransactions.CancelAuto(Wallet.EnterpriseWallet(),auto.Id,Guid.NewGuid().ToString("N"),Wallet.Save,out _),OpenAutomation));
            }
            Add("Retirada manual no porto","Alternativa usando caminhão/carretas.",()=>CollectionMenu?.Invoke());Add("Voltar","Operacao.",OpenMenu);ui.Show();
        }
        void OpenFacilities()
        {
            Page("facilities","PDM / INSTALACOES E FROTA");
            foreach(var f in options.Facilities){var item=f;ui.AddConfirm(Data.Upgrades.Contains(f.Id)?f.Name+" / adquirido":f.Name+" / $"+f.Price,"Instala o recurso fisico; verifica o modelo antes de cobrar. Modelo: "+f.Model,()=>ui.Stream(new[]{item.Model}.Concat(item.Parts.Select(p=>p.Model)),()=>Change(()=>PdmEnterpriseTransactions.Upgrade(Wallet.EnterpriseWallet(),options,item.Id,Guid.NewGuid().ToString("N"),Wallet.Save,out _),OpenFacilities),OpenFacilities),!Data.Upgrades.Contains(f.Id));}
            foreach(var f in new[]{new{Id="truck",Name="Caminhao Packer",Cost=200000,Model="packer"},new{Id="carrier2",Name="Carreta TRFlat / duas unidades",Cost=65000,Model="trflat"},new{Id="carrier6",Name="Carreta TR2 / seis unidades",Cost=150000,Model="tr2"}}){var item=f;ui.AddConfirm(f.Name+(Data.Upgrades.Contains(f.Id)?" / adquirido":" / $"+f.Cost),"Compra unica de equipamento empresarial. Packer+TRFlat proprios eliminam a taxa de coleta automatica. Packer+TR2 habilitam lotes de tres a seis encomendas; o fluxo antigo de duas continua.",()=>ui.Stream(new[]{item.Model},()=>Change(()=>PdmEnterpriseTransactions.Upgrade(Wallet.EnterpriseWallet(),options,item.Id,Guid.NewGuid().ToString("N"),Wallet.Save,out _),OpenFacilities),OpenFacilities),!Data.Upgrades.Contains(f.Id));}
            Add("Voltar","Operacao.",OpenMenu);ui.Show();
        }
        void OpenPromotion()
        {
            Page("promotion","PDM / PROMOCAO E REPUTACAO");
            ui.Add("Reputacao: "+Data.Reputation+"/100","Aumenta por campanhas/cartoes/patrocinio e melhora precos/chance de venda. Limpeza e salarios influenciam o humor.",()=>{},false);
            if(Data.CardsLeft>0)Add("Distribuir cartoes / faltam "+Data.CardsLeft,"A pe, desarmado, aproxime-se de pessoas e use "+Cfg.PdmInteractionKey+". Uma pessoa por campanha. End encerra a atividade sem apagar os cartoes.",StartCards);
            else ui.AddConfirm("Imprimir "+options.CardCount+" cartoes / $"+options.CardCost,"Debita o caixa; reputacao sobe ao completar a distribuicao real, nao apenas por imprimir.",()=>Change(()=>PdmEnterpriseTransactions.BeginCards(Wallet.EnterpriseWallet(),options,Guid.NewGuid().ToString("N"),Wallet.Save,out _),OpenPromotion));
            foreach(int value in new[]{100,500,2500,10000,25000,100000}){int amount=value;ui.AddConfirm("Doar $"+amount,"Debita o caixa agora; resultado de reputacao no proximo dia empresarial, sem dinheiro de retorno.",()=>Change(()=>PdmEnterpriseTransactions.Donate(Wallet.EnterpriseWallet(),amount,Guid.NewGuid().ToString("N"),Wallet.Save,out _),OpenPromotion));}
            ui.AddConfirm(Data.SponsorRemaining>0?"Encerrar patrocinio":"Patrocinar equipe de corrida / $7500","Campanha de dez dias empresariais, cobranca inicial e renovacao semanal enquanto ativa. Sem caixa, suspende; pode encerrar para impedir renovacao.",()=>Change(()=>PdmEnterpriseTransactions.Sponsor(Wallet.EnterpriseWallet(),options,Data.SponsorRemaining==0,Guid.NewGuid().ToString("N"),Wallet.Save,out _),OpenPromotion));
            Add("Voltar","Operacao.",OpenMenu);ui.Show();
        }
        void OpenRentalOrders()
        {
            Page("rentals","PDM / PEDIDOS DA LOCADORA");
            foreach(var request in Data.RentalOrders)ui.Add(request.Name+" / 2 unidades","Pedido "+request.Id+". Tenha duas unidades recebidas do modelo "+request.Model+" e use o contrato da locadora para carregar, entregar e devolver o conjunto.",()=>{},false);
            Add("Atender locadora","Abrir contratos reais de entrega de duas unidades.",()=>RentalMenu?.Invoke());Add("Voltar","Operacao.",OpenMenu);ui.Show();
        }
        public void Tick()
        {
            int now=Game.GameTime,delta=unchecked(now-last);last=now;
            if(profile!=Profile.Id || !Wallet.Pdm.Valid || !Wallet.Pdm.Owned){ReleaseWorld();return;}
            if(PdmMenuRuntime.WorldAllowed && delta>0)pendingSeconds+=Math.Min(2000,delta);
            if(PdmMenuRuntime.WorldAllowed)CatalogueTick();
            ui.Tick(PdmMenuRuntime.Allowed);
            try{
                if(pendingSeconds>=30000 && unchecked(now-nextTick)>=0){tickSeconds=(int)Math.Min(60,pendingSeconds/1000);if(tickReceipt==null)tickReceipt=Guid.NewGuid().ToString("N");if(PdmEnterpriseTransactions.Advance(Wallet.EnterpriseWallet(),options,tickSeconds,tickReceipt,Wallet.Save,out _)){pendingSeconds-=tickSeconds*1000;tickReceipt=null;}nextTick=now+5000;}
                if(unchecked(now-nextWorld)>=0){nextWorld=now+1000;WorldTick();}
                ManualTick();SellerTick();AutoTick();StaffInteraction();
            }catch(Exception ex){nextWorld=nextTick=now+5000;Log.Error("PDM enterprise tick",ex);}
        }
        void StaffInteraction()
        {
            if(!Safe || manualMode!=null || sellerTesting || pool.AreAnyVisible || Wallet.Pdm.PremisesService!=null)return;
            var target=(RolePeds?.Invoke()??Enumerable.Empty<KeyValuePair<string,Ped>>()).FirstOrDefault(p=>Valid(p.Value) && !p.Value.IsDead && !p.Value.IsInCombat && p.Value.Position.DistanceTo(Game.Player.Character.Position)<2);
            if(target.Value==null)return;Prompt.Help(PdmCustomerInput.Button+": conversar / equipe PDM");if(PdmCustomerInput.Pressed)StaffRole?.Invoke(target.Key);
        }
        public void Save(){if(profile==Profile.Id && Wallet.Pdm.Valid && Wallet.Pdm.Owned && pendingSeconds>=1000){int seconds=(int)Math.Min(60,pendingSeconds/1000);var id=tickReceipt??(tickReceipt=Guid.NewGuid().ToString("N"));if(PdmEnterpriseTransactions.Advance(Wallet.EnterpriseWallet(),options,seconds,id,Wallet.Save,out _)){pendingSeconds-=seconds*1000;tickReceipt=null;}}}
        public void Reload(){ui.Reset();ReleaseWorld();profile=Profile.Id;last=Game.GameTime;pendingSeconds=0;tickReceipt=null;offers.Clear();offersDay=-1;basket.Clear();basketDay=-1;}
        public void OnKey(KeyEventArgs e){if(e.KeyCode==Keys.End && e.Modifiers==Keys.None)StopManual();}
        public void Dispose(){ui.Reset();try{Save();}finally{ReleaseWorld();}}
    }
}
