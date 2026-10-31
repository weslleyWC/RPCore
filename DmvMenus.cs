using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Native;
namespace RPCore
{
    public sealed partial class DmvSystem
    {
        string page;readonly DmvTheoryAnswerQueue answers=new DmvTheoryAnswerQueue();bool stopTheory;
        DmvData ReadBook(DmvClass c)=>c==DmvClass.C?Wallet.Dmv:c==DmvClass.M1?Wallet.DmvM1:Wallet.DmvA;
        DmvOptions Config(DmvClass c)=>c==DmvClass.C?carOptions:c==DmvClass.M1?motorcycleOptions:truckOptions;
        JobPoint Desk(DmvClass c){var saved=ReadBook(c).Pending?.Options;return saved?.Valid==true && saved.Category==c?saved.Entry:Config(c).Entry;}
        public bool ConsumesKeyboard=>menu.Visible && page=="quiz" && lease && Book.Pending?.Practical==false;
        void MenuError(Exception ex){EndRuntime();ui.Close();Log.Error("DMV menu",ex);Notify.Show("DMV: operacao suspensa. Documentos e tentativa gravada preservados; consulte o DMV antes de repetir.");}
        void Page(string id,string title,Action back=null){page=id;answers.Clear();ui.Begin("dmv:"+category+":"+id,back);menu.Name=title;}
        void Add(string title,string description,Action action,bool enabled=true)=>ui.Add(title,description,action,enabled);
        bool Acquire(){if(lease)return owner==Profile.Id;if(!gate.Acquire(Name)){Notify.Show("Encerre a atividade de "+gate.Owner+".");return false;}lease=true;owner=Profile.Id;actorHandle=Game.Player.Character.Handle;actorModel=Game.Player.Character.Model.Hash;actorAddress=Game.Player.Character.MemoryAddress.ToInt64();return true;}
        bool ActorBound=>owner==Profile.Id && actorHandle==Game.Player.Character.Handle && actorModel==Game.Player.Character.Model.Hash && actorAddress==Game.Player.Character.MemoryAddress.ToInt64();
        int actorHandle,actorModel;long actorAddress;
        public void OpenMenu()
        {
            if(!Calm)return;if(Running){OpenClass(category);return;}pool.HideAll();Page("root","DMV / HABILITACOES");
            foreach(DmvClass c in Enum.GetValues(typeof(DmvClass))){var chosen=c;var book=ReadBook(c);Add((c==DmvClass.C?"C / carros":c==DmvClass.M1?"M1 / motocicletas":"A / caminhoes e carretas")+" / "+(!DmvBookRules.Matches(book,c)?"dados protegidos":book.Licensed?"emitida":book.Pending!=null?"tentativa pendente":book.TheoryPassed?"pratico liberado":"teoria pendente"),"Abrir documentos, material de estudo, provas, resultados e GPS desta categoria.",()=>OpenClass(chosen));}
            Add("Como funciona","Estude, confirme a taxa bancaria, conclua teoria e pratique. Aprovacao exige voltar ao atendimento para emitir. Suspender conserva o pagamento. Documentos sao separados por perfil e categoria.",OpenHelp);
            Add("Fechar","Voltar ao jogo.",()=>ui.Close());menu.Visible=true;
        }
        void OpenClass(DmvClass selected)
        {
            if((Running || lease) && selected!=category){Notify.Show("Suspenda a tentativa antes de trocar de categoria.");return;}category=selected;pool.HideAll();Page("class","DMV / CLASSE "+ClassName,Running?(Action)(()=>ui.Close()):OpenMenu);
            if(!DmvBookRules.Matches(Book,category)){Add("Documentos protegidos","Dados invalidos conservados. Nenhuma taxa ou alteracao sera feita nesta categoria.",()=>{},false);Add("Voltar","Categorias.",OpenMenu);menu.Visible=true;return;}
            Add("Banco disponivel / $"+Wallet.Bank,"Taxas saem do banco pessoal. Dinheiro na carteira e caixa de empresa nao sao debitados pela autoescola.",()=>{},false);
            Add("GPS / atendimento da classe "+ClassName,"Atendimento presencial no local indicado. Pendencias conservam o local/valores contratados.",()=>{var desk=Desk(category);Function.Call(Hash.SET_NEW_WAYPOINT,desk.X,desk.Y);ui.Close();});
            Add("Material de estudo / orientacoes","Regras de circulacao, preparacao, manobras, faltas e uso dos controles. Consulta gratuita.",OpenHelp);
            Add("Teoria: "+(Book.TheoryPassed?"aprovada":"pendente")+" / documento: "+(Book.Licensed?"emitido":"pendente"),"Tentativas teoria "+Book.TheoryAttempts+", pratico "+Book.PracticalAttempts+"; taxas $"+Book.Spent+"; ultima nota "+(Book.LastTheoryScore<0?"nao realizada":Book.LastTheoryScore+"/15")+".",()=>{},false);
            if(Running){
                Add("Continuar exame","Voltar ao percurso.",()=>ui.Close());
                if(Truck)Add("Desengatar a carreta","Apos a re e alinhamento, conjunto parado. Somente a carreta do exame.",Uncouple,drive?.Reversed==true);
                if(Moto)Add("Vestir capacete do exame","Pare como condutor. Guarda e restaura o adereco anterior somente se o emprestimo ainda for reconhecido.",EquipHelmet);
                if(!Cfg.EnableVehicleControls){Add("Demonstrar seta direita","Pare como condutor; apenas o veiculo de exame.",()=>Signal(false));Add("Demonstrar seta esquerda","Pare como condutor; apenas o veiculo de exame.",()=>Signal(true));}
                Add("Faltas / avaliacao atual","Consultar motivos ja registrados. Pare antes de consultar o menu.",OpenEvaluation);
                Add("Suspender e retomar depois","Mantem faltas, tempo, pontos ja confirmados e pagamento. Retomada exige preparacao no DMV, sem taxa repetida.",Suspend);
            }else{
                var pending=Book.Pending;
                if(pending!=null){string id=pending.Id;
                    Add(pending.Practical?(pending.Drive.Stage==DmvStage.Report?"Emitir habilitacao aprovada":"Retomar exame pratico pago"):"Retomar prova teorica paga","Atendimento presencial. Usa a tentativa existente; nao cobra novamente.",()=>{if(!CheckDesk())return;if(pending.Practical){if(pending.Drive.Stage==DmvStage.Report)Issue(id);else Prepare();}else Theory(id);});
                    if(!pending.Started)Add("Cancelar / devolver $"+pending.Price,"Permitido antes de iniciar; estorna a taxa para o banco.",()=>Confirm("Cancelar tentativa","Devolve $"+pending.Price+" ao banco e encerra apenas esta tentativa.",()=>{if(!CheckDesk())return;if(!Wallet.DmvCancel(id,out _,category))throw new InvalidOperationException("DMV refund refused.");OpenClass(category);}));
                    else Add("Abandonar tentativa","Confirmacao obrigatoria; mantem certificados e documentos anteriores, sem estorno de tentativa iniciada.",()=>ConfirmAbandon(id));
                }else if(!Book.TheoryPassed)Add("Contratar teoria / $"+options.TheoryPrice,"15 questoes, minimo "+options.PassScore+" acertos; pagamento pelo banco apos confirmar, sem emitir habilitacao automaticamente.",()=>ConfirmFee(options.TheoryPrice,"Prova teorica / $"+options.TheoryPrice,"Banco apos: $"+(Wallet.Bank-options.TheoryPrice)+". Pode suspender e retomar sem outra taxa.",()=>{
                    if(!CheckDesk() || !Acquire())return;string id=Guid.NewGuid().ToString("N");if(!Wallet.DmvBeginRefined(options,refinement,false,id,out _,category)){EndRuntime();Notify.Show("DMV: saldo/documentos mudaram. Nenhuma prova nova confirmada.");OpenClass(category);return;}Theory(id);
                }));
                else if(!Book.Licensed)Add("Contratar exame pratico / $"+options.PracticalPrice,(Truck?"Phantom, carreta, engate, percurso, re, alinhamento e desengate.":Moto?"PCJ 600, examinadora, capacete, setas, percurso e estacionamento.":"Asterope, examinadora, cinto, setas, percurso e estacionamento.")+" Debita somente apos preparar veiculo/equipe e validar vaga.",()=>ConfirmFee(options.PracticalPrice,"Pratico / $"+options.PracticalPrice,"Banco apos: $"+(Wallet.Bank-options.PracticalPrice)+". Limite de "+options.MaxFaults+" faltas e "+options.SecondsLimit/60+" minutos ativos.",Prepare));
            }
            if(Book.TheoryReview!=null)Add("Revisar ultima prova teorica","Consultar suas respostas, acertos e explicacoes. Disponivel depois de concluir a prova.",OpenTheoryReview);
            if(Book.PracticalReport!=null)Add("Ultimo resultado pratico","Motivo, faltas, pontos e tempo da ultima tentativa encerrada.",OpenLastReport);
            Add("Voltar","Categorias; durante o exame apenas fecha o menu.",Running?(Action)(()=>ui.Close()):OpenMenu);menu.Visible=true;
        }
        bool CheckDesk(){if(!AtDesk){Notify.Show("Va ao atendimento desta categoria a pe, sem procurado/missao/combate.");return false;}return true;}
        void ConfirmFee(int price,string title,string text,Action action){if(Wallet.Bank<price){Notify.Show("DMV: faltam $"+(price-Wallet.Bank)+" no banco para esta prova.");return;}Confirm(title,text,action);}
        void Confirm(string title,string text,Action action)
        {
            string profile=Profile.Id,book=Newtonsoft.Json.JsonConvert.SerializeObject(Book);int bank=Wallet.Bank;Page("confirm","CONFIRMAR / "+title,()=>OpenClass(category));
            Add("Cancelar / voltar","Mantem o estado anterior.",()=>OpenClass(category));
            Add("Confirmar",text,()=>{if(profile!=Profile.Id || bank!=Wallet.Bank || book!=Newtonsoft.Json.JsonConvert.SerializeObject(Book)){Notify.Show("Saldo ou documentos mudaram. Confira novamente.");OpenClass(category);return;}action();});menu.Visible=true;
        }
        void ConfirmAbandon(string id)=>Confirm("Abandonar tentativa","Encerra esta tentativa iniciada, sem devolver sua taxa.",()=>{if(!CheckDesk())return;if(!Wallet.DmvAbandon(id,out _,category))throw new InvalidOperationException("DMV abandon refused.");EndRuntime();OpenClass(category);});
        void Theory(string id)
        {if(!CheckDesk() || !lease && !Acquire())return;attempt=id;if(!Book.Pending.Started && !Wallet.DmvStart(id,out _,category))throw new InvalidOperationException("Theory start not saved.");Quiz();}
        void Quiz()
        {
            var pending=Book.Pending;if(pending==null || pending.Practical){EndRuntime();OpenClass(category);return;}
            int index=pending.Answers.Count;var q=DmvQuestions.For(category)[index];Page("quiz","TEORIA "+ClassName+" / "+(index+1)+" DE 15",SuspendTheory);
            Add("Pagina "+(index/3+1)+"/5 / questao "+(index%3+1)+"/3",q.Text,()=>{},false);
            for(int i=0;i<4;i++){int answer=i;Add(((char)('A'+i))+" / "+q.Answers[i],q.Text+" Escolha com Enter/clique ou A, B, C, D.",()=>SubmitAnswer(pending.Id,index,answer));}
            Add("Suspender prova","Respostas ja confirmadas e pagamento permanecem para retomar no atendimento.",SuspendTheory);menu.Visible=true;
        }
        void SubmitAnswer(string id,int index,int answer)
        {
            if(!AtDesk || !ActorBound || Book.Pending?.Id!=id || Book.Pending.Answers.Count!=index || unchecked(Game.GameTime-answerAt)<350)return;
            if(!Wallet.DmvAnswer(id,index,answer,out _,category))throw new InvalidOperationException("Answer not saved.");answerAt=Game.GameTime;
            if(Book.Pending==null){int score=Book.LastTheoryScore;bool pass=Book.TheoryPassed;EndRuntime();OpenClass(category);Notify.Show("Teoria: "+score+"/15 / "+(pass?"aprovada; pratico liberado.":"reprovada. Consulte suas respostas antes de tentar novamente."));}else Quiz();
        }
        void SuspendTheory(){EndRuntime();ui.Close();Notify.Show("Prova suspensa; respostas e pagamento preservados.");}
        void Suspend(){Save();EndRuntime();ui.Close();Notify.Show("Exame suspenso; progresso gravado e pagamento preservados.");}
        void OpenEvaluation()=>ShowEvaluation(drive,examOptions,"AVALIACAO ATUAL",()=>OpenClass(category));
        void OpenLastReport(){var report=Book.PracticalReport;if(report==null)return;ShowEvaluation(report.Drive,report.Options,report.Passed?"PRATICO / APROVADO":"PRATICO / REPROVADO",()=>OpenClass(category),report.Reason);}
        void ShowEvaluation(DmvDrive state,DmvOptions c,string title,Action back,string reason=null)
        {
            if(state==null || c==null)return;Page("evaluation",title,back);Add("Faltas "+state.Faults+"/"+c.MaxFaults+" / "+state.Point+"/"+c.Route.Count+" pontos","Tempo ativo: "+(int)state.Seconds+" segundos. Faltas anteriores sem motivo detalhado continuam no total.",()=>{},false);
            if(reason!=null)Add("Motivo do resultado",reason,()=>ShowLesson("RESULTADO PRATICO",reason,OpenLastReport));
            foreach(var e in state.Events){var item=e;Add(DmvAssessmentRules.FaultName(e.Kind)+" / etapa "+(e.Point+1),"Registrada aos "+(int)e.Seconds+" segundos de prova.",()=>ShowLesson(DmvAssessmentRules.FaultName(item.Kind),"Etapa "+(item.Point+1)+". Registrada aos "+(int)item.Seconds+" segundos ativos de prova.",()=>ShowEvaluation(state,c,title,back,reason)));}Add("Voltar","Atendimento.",back);menu.Visible=true;
        }
        void OpenTheoryReview()
        {
            var review=Book.TheoryReview;if(review==null)return;Page("review","REVISAO / "+review.Score+" DE 15",()=>OpenClass(category));
            var bank=DmvQuestions.For(review.Category);for(int i=0;i<15;i++){int question=i;var q=bank[i];Add((i+1)+" / "+(review.Answers[i]==q.Correct?"acerto":"revisar"),q.Text,()=>ShowLesson("QUESTAO "+(question+1),q.Text+"~n~~n~Sua resposta: "+q.Answers[review.Answers[question]]+"~n~~n~Resposta correta: "+q.Answers[q.Correct],OpenTheoryReview));}Add("Voltar","Categoria.",()=>OpenClass(category));menu.Visible=true;
        }
        void OpenHelp()
        {
            Page("help","DMV / ESTUDO E ORIENTACOES",()=>OpenClass(category));
            LessonRow("Antes de sair","Ajuste cinto ou capacete. Demonstre seta direita e depois esquerda com o veiculo parado. Ligue o motor com "+Cfg.EngineKey+"; setas "+Cfg.LeftSignalKey+" / "+Cfg.RightSignalKey+"; cinto "+Cfg.SeatbeltKey+".");
            LessonRow("Circulacao e preferencia","Dirija pela direita, respeite velocidade e distancia. Pare para pedestres e emergencia. Planeje a conversao antes do cruzamento; pare em local seguro para usar o telefone.");
            LessonRow("Paradas e conversoes","Nos pontos de parada, mantenha o veiculo parado pelo tempo indicado. A instrucao no percurso informa conversoes que devem ser sinalizadas. Nao precisa acelerar para atravessar marcadores.");
            LessonRow("Causas de falta","Colisao/dano, excesso persistente de velocidade, conduzir sem cinto/capacete e conversao sem sinalizacao quando exigida. A notificacao informa o motivo; o menu guarda a avaliacao.");
            LessonRow("Excesso continuo","Um excesso continuo gera uma falta. E preciso voltar a uma condicao segura por alguns segundos para encerrar a ocorrencia. Ainda e necessario reduzir para validar cada trecho.");
            LessonRow("Estacionamento",Truck?"Carreta na vaga indicada, conjunto alinhado e parado; recue a distancia exigida, desengate e desembarque.":"Retorne a vaga indicada, alinhe com o sentido mostrado e pare por alguns segundos. Moto: mantenha capacete ate parar. Depois desembarque e volte ao atendimento.");
            LessonRow("Particularidades "+ClassName,Truck?"Curvas largas, maior distancia de frenagem e verificacao de engate. A re so conta acoplado, com condutor/examinadora e cinto. Evite efeito canivete.":Moto?"Capacete, equilibrio e baixa velocidade nos primeiros trechos. Distancia de frenagem e aderencia exigem cuidado especial na chuva.":"Distancia segura, curvas sem brusquidao, sinalizacao antecipada e cuidado com pedestres/cruzamentos.");
            LessonRow("Suspender / recuperar","End ou menu conserva a tentativa. Sair e retomar prepara outro conjunto sem repetir a taxa; etapas fisicas de preparacao precisam ser demonstradas de novo, mantendo pontos/faltas/tempo confirmados.");
            LessonRow("Documentos e taxas","Aprovacao teorica e habilitacao sao documentos separados por classe e perfil. Emitir requer concluir o percurso e retornar ao atendimento. Cancelamento antes de iniciar pode estornar; abandono iniciado nao estorna.");
            Add("Voltar","Atendimento.",()=>OpenClass(category));menu.Visible=true;
        }
        string lessonTitle,lessonText;
        void LessonRow(string title,string text)=>Add(title,text,()=>ShowLesson(title,text,OpenHelp));
        void ShowLesson(string title,string text,Action back){Page("lesson",title,back);lessonTitle=title;lessonText=text;Add("Voltar","Retornar a lista.",back);menu.Visible=true;}
        void DrawLesson()
        {
            if(!menu.Visible || page!="lesson")return;float x=560,w=Math.Min(900,Visuals.Width-x-30);if(w<340)return;
            Visuals.Rect(x,70,w,610,Color.FromArgb(235,15,22,32));Visuals.Label(lessonTitle,x+24,90,.48f,Color.White,wrap:w-48);Visuals.Label(lessonText,x+24,210,.38f,Color.FromArgb(225,235,245),wrap:w-48);
        }
        void DrawTheoryCard()
        {
            if(!ConsumesKeyboard)return;var pending=Book.Pending;if(pending==null)return;var q=DmvQuestions.For(category)[pending.Answers.Count];
            float x=560,w=Math.Min(900,Visuals.Width-x-30);if(w<340)return;Visuals.Rect(x,60,w,680,Color.FromArgb(235,15,22,32));Visuals.Label("DMV / PROVA TEORICA "+ClassName,x+24,82,.52f,Color.White);
            Visuals.Label(q.Text,x+24,160,.42f,Color.White,wrap:w-48);for(int i=0;i<4;i++)Visuals.Label(((char)('A'+i))+". "+q.Answers[i],x+24,290+i*83,.36f,Color.FromArgb(230,235,240),wrap:w-48);
            Visuals.Label("A / B / C / D ou menu: responder. Esc / End: suspender.",x+24,650,.3f,Color.FromArgb(140,210,240),wrap:w-48);
            foreach(var control in new[]{GTA.Control.MoveLeftRight,GTA.Control.MoveUpDown,GTA.Control.Attack,GTA.Control.Attack2,GTA.Control.Jump,GTA.Control.Enter,GTA.Control.Cover})Game.DisableControlThisFrame(control);
        }
    }
}
