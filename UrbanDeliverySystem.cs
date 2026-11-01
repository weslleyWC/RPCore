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
    internal sealed partial class UrbanDeliverySystem
    {
        public event Action<string,string> Activity;
        void Announce(string text){try{Activity?.Invoke(Label,text);}catch(Exception ex){Log.Error(job+" phone receipt",ex);}}
        readonly ObjectPool pool;readonly AchievementSystem achievements;readonly ShiftGate gate;readonly string job;readonly DeliveryOptions options;readonly StoreDeliveryOptions freightOptions;readonly JobPoint office,depot;
        readonly Action quickStart,quickStop,updated;readonly Func<bool> quickActive;readonly NativeMenu menu;readonly PersonalServiceMenu ui;readonly Random random=new Random();readonly DeliveryCargo cargo=new DeliveryCargo();
        string page,id,profile;int selectedCount;bool personal;
        bool Pizza=>job=="pizza";bool Freight=>job=="supply";string Label=>Pizza?"Pizza This!":Freight?"Reposicao de lojas":"Post OP";
        public bool Active=>id!=null;public bool HasPending=>TryStats(out var s) && s.UrbanContract!=null;
        UrbanDeliveryContract Contract=>TryStats(out var s)?s.UrbanContract:null;
        bool TryStats(out DeliveryProgress stats)=>UrbanDeliveryTransactions.Read(Wallet.DeliveryWallet(),job,out stats);
        bool Consult=>!Game.IsPaused && !Game.IsMissionActive && !GameplayContext.SceneBlocking && Game.Player.CanControlCharacter && Profile.Detect()==Profile.Id && Game.Player.Character.IsAlive && !InterfaceOptions.ModalOpen;
        bool Foot=>PersonalServiceMenu.SafeFoot && Game.Player.Character.Weapons.Current.Hash==WeaponHash.Unarmed;
        bool AtOffice=>Foot && Game.Player.Character.Position.DistanceTo(Pos(Contract?.Office??office))<=3;
        static Vector3 Pos(JobPoint point)=>new Vector3(point.X,point.Y,point.Z);
        public UrbanDeliverySystem(ObjectPool pool,AchievementSystem achievements,ShiftGate gate,string job,DeliveryOptions options,StoreDeliveryOptions freightOptions,JobPoint office,JobPoint depot,Action quickStart,Func<bool> quickActive,Action quickStop,Action updated)
        {
            this.pool=pool;this.achievements=achievements;this.gate=gate;this.job=job;this.options=options;this.freightOptions=freightOptions;this.office=office;this.depot=depot;this.quickStart=quickStart;this.quickActive=quickActive;this.quickStop=quickStop;this.updated=updated;
            selectedCount=Pizza?Cfg.PizzaDrops:options.Drops;menu=new NativeMenu(Label,"CONTRATOS / CARGA / ROTA");ui=new PersonalServiceMenu(menu,Fault);ui.CanNavigate=()=>Consult;pool.Add(menu);Visuals.Register(menu,job);
        }
        void Fault(Exception ex){Log.Error(job+" urban delivery",ex);try{Suspend(false);}catch(Exception pending){Log.Error(job+" suspend checkpoint retained",pending);}ui.Close();Notify.Show(Label+": operacao suspensa; pagamentos e rota gravados preservados.");}
        void Page(string key,string title,Action back=null){page=key;ui.Begin("urban:"+job+":"+key,back);menu.Name=title;}
        void Add(string title,string description,Action action,bool enabled=true)=>ui.Add(title,description,action,enabled);
        public void OpenMenu()
        {
            if(!Consult){Notify.Show(Label+": feche outra interface e aguarde controle fora de missao/cena.");return;}pool.HideAll();Page("root",Label+" / ENTREGAS");
            if(!TryStats(out var stats)){Add("Progresso protegido","Dados invalidos conservados. Nenhum contrato/recebimento sera alterado.",()=>{},false);Add("Fechar","Voltar.",()=>ui.Close());menu.Visible=true;return;}
            var c=stats.UrbanContract;
            Add("GPS / atendimento","Va a pe ao local para iniciar ou retomar uma rota presencial.",()=>{var p=c?.Office??office;Function.Call(Hash.SET_NEW_WAYPOINT,p.X,p.Y);ui.Close();});
            Add("Entregas "+stats.Deliveries+" / turnos "+stats.CompletedShifts,"Recebido acumulado $"+stats.Earned+". "+"Contrato presencial paga no banco pessoal, como na referencia.",()=>{},false);
            if(preparing){Add("Preparando veiculo","Aguarde o modelo e uma vaga livre no atendimento. Nenhum pagamento por entrega foi feito.",()=>{},false);Add("Cancelar preparacao","Preserva eventual rota ja salva.",()=>Suspend(true));}
            else if(quickActive())Add("Encerrar modo rapido","Mantem pagamentos ja recebidos. Nao altera a rota presencial salva.",quickStop);
            else if(c!=null){
                Add("Rota / "+c.Delivered+" entregues / "+c.Skipped+" devolvidas",(c.Stops.Count-c.Index)+" restantes; qualidade "+c.Quality+"%; recebido $"+c.Paid+".",()=>OpenRoute());
                if(!Active)Add("Retomar rota salva","No atendimento, prepara veiculo/carga restantes. Sem repetir entregas ou zerar tempo/qualidade.",()=>Prepare(true));
                else{
                    Add("GPS / objetivo atual","Carga, endereco ou devolucao conforme a etapa.",()=>{SetRoute();ui.Close();});
                    if(Pizza && c.Returning)Add("Receber e continuar / novo lote","Na base, parado como condutor. Fecha o lote atual e registra a proxima rota junto do pagamento; mantem a scooter e recarrega novas pizzas.",()=>Confirm("Continuar outro lote","Recebe o bonus do lote atual e comeca uma nova rota, sem apagar o comprovante anterior.",ContinuePizza));
                    Add("Suspender turno","Preserva rota e pagamentos. Pode retomar no atendimento; End segurado tambem suspende.",()=>Suspend(true));
                    if(job=="postop" && !c.Returning && stage==UrbanStage.NeedPackage)Add("Devolver este pacote / pular endereco","Confirma a devolucao sem pagamento e sem contar como entrega. Bonus final proporcional somente a entregas reais.",()=>{int index=c.Index;Confirm("Devolver pacote","Nao recebe por este endereco; a proxima entrega permanece na rota.",()=>Skip(index));});
                }
                Add("Cancelar rota definitivamente","Confirma o encerramento sem bonus. Valores ja pagos permanecem.",()=>Confirm("Cancelar rota","Encerra a rota salva sem bonus de retorno.",()=>Cancel()));
            }else if(!quickActive()){
                if(job=="postop")foreach(int count in new[]{5,10,15,20,25,30}.Where(n=>n<=options.Addresses.Select(p=>p.X+":"+p.Y+":"+p.Z).Distinct().Count())){int amount=count;Add((selectedCount==count?"[X] ":"")+count+" encomendas","Selecionar quantidade do lote; pagamento por entrega confirmada.",()=>{selectedCount=amount;OpenMenu();});}
                if(job=="postop")Add("Veiculo / "+(personal?"meu veiculo":"van da empresa"),"Veiculo proprio: depois de contratar a pe, entre como condutor para identifica-lo. Ele nunca e apagado/devolvido como carro da empresa.",()=>{personal=!personal;OpenMenu();});
                Add("Revisar e iniciar / "+selectedCount+" destinos","Carga na base, retirada fisica e entrega. Rota/valores ficam salvos ao iniciar. Retorno encerra o turno.",()=>Confirm("Iniciar rota","Sem taxa de contratacao. Antes de iniciar, verifica contexto, modelo e vaga livre.",()=>Prepare(false)));
                if(Pizza)Add("Modo rapido legado / "+Cfg.JobKey,"Atalho de testes anterior, sem carga/cliente presencial. O refinamento completo e o contrato acima.",quickStart);
            }
            if(stats.UrbanReport!=null)Add("Ultimo turno / comprovante","Entregas reais, devolucoes, tempo, qualidade e pagamentos.",OpenReport);
            Add("Como trabalhar","Consultar etapas, controles e pagamentos.",OpenHelp);Add("Fechar","Voltar ao jogo.",()=>ui.Close());menu.Visible=true;
        }
        void Confirm(string title,string text,Action action)
        {
            string before=Newtonsoft.Json.JsonConvert.SerializeObject(Contract);Page("confirm",title,OpenMenu);Add("Voltar","Sem alteracao.",OpenMenu);Add("Confirmar",text,()=>{if(before!=Newtonsoft.Json.JsonConvert.SerializeObject(Contract)){Notify.Show("Rota atualizada; confira novamente.");OpenMenu();return;}action();});menu.Visible=true;
        }
        void OpenRoute()
        {
            var c=Contract;if(c==null)return;Page("route","ROTA / "+Label,OpenMenu);for(int i=0;i<c.Stops.Count;i++){var stop=c.Stops[i];var p=stop.Address;Add((i+1)+" / "+p.Name+" / "+(stop.Result==UrbanStopResult.Delivered?"entregue":stop.Result==UrbanStopResult.Skipped?"devolvido":i==c.Index?"atual":"pendente"),"Base prevista $"+stop.Pay+(Pizza?"; pontualidade ate $"+stop.TimelyBonus:"")+"; qualidade pode reduzir o pagamento. Selecionar marca GPS, sem pular etapas.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,p.X,p.Y);ui.Close();});}Add("Voltar","Contrato.",OpenMenu);menu.Visible=true;
        }
        void OpenReport()
        {
            if(!TryStats(out var stats) || stats.UrbanReport==null)return;var r=stats.UrbanReport;Page("report",r.Completed?"TURNO CONCLUIDO":"TURNO ENCERRADO",OpenMenu);Add(r.Delivered+" entregues / "+r.Skipped+" devolvidos / "+r.Total+" destinos",r.Reason,()=>{},false);Add("Recebido / $"+r.Paid,"Tempo ativo "+r.Seconds+"s; qualidade final "+r.Quality+"%. Este e um comprovante, nao um novo pagamento.",()=>{},false);Add("Voltar","Contrato.",OpenMenu);menu.Visible=true;
        }
        string infoTitle,infoText;
        void InfoRow(string title,string text)=>Add(title,text,()=>{Page("info",title,OpenHelp);infoTitle=title;infoText=text;Add("Voltar","Orientacoes.",OpenHelp);menu.Visible=true;});
        void DrawInfo(){if(!menu.Visible || page!="info")return;float x=560,w=Math.Min(850,Visuals.Width-x-30);if(w<340)return;Visuals.Rect(x,80,w,560,Color.FromArgb(235,15,22,32));Visuals.Label(infoTitle,x+24,100,.48f,Color.White,wrap:w-48);Visuals.Label(infoText,x+24,210,.38f,Color.White,wrap:w-48);}
        void OpenHelp()
        {
            Page("help","COMO TRABALHAR",OpenMenu);InfoRow("1 / Contratar e carregar","A pe, desarmado e sem procurado na base. Retire o veiculo, estacione no deposito e carregue junto a traseira com E.");InfoRow("2 / Retirar e entregar","No endereco, pare e retire a carga com E. Pizza: cliente presencial; se nao houver atendimento, a entrega na porta fica disponivel apos espera indicada. Caixas: leve ao marcador e confirme com E.");InfoRow("3 / Pagamento e cuidado","Recebe somente ao confirmar cada entrega. Colisoes reduzem qualidade; pizza atrasada recebe menos e perde a pontualidade. Tempo nao corre pausado ou consultando menus.");InfoRow("4 / Voltar ou suspender","Depois dos destinos, devolva o veiculo/conclua a rota na base. Bonus proporcional ao que foi entregue. End segurado suspende; cancelar definitivamente pede confirmacao.");Add("Voltar","Contrato.",OpenMenu);menu.Visible=true;
        }
    }
}
