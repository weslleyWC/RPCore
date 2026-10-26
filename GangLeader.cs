using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
namespace RPCore
{
    internal sealed class GangLeader
    {
        readonly ShiftGate gate;readonly ObjectPool pool;readonly GangPresence presence;readonly Func<GangState> state;readonly Action<GangState> save;readonly Func<IEnumerable<GangDefinition>> gangs;
        readonly Func<GangDefinition,bool> initiate,resume;readonly Action territories,commerce,treasury,diplomacy,warfare,assault;readonly NativeMenu menu;
        GangDefinition selected;Ped leader,player;string profile,affiliation;
        public GangLeader(ShiftGate gate,ObjectPool pool,GangPresence presence,Func<GangState> state,Action<GangState> save,Func<IEnumerable<GangDefinition>> gangs,Func<GangDefinition,bool> initiate,Func<GangDefinition,bool> resume,Action territories,Action commerce,Action treasury,Action diplomacy,Action warfare,Action assault)
        {
            this.gate=gate;this.pool=pool;this.presence=presence;this.state=state;this.save=save;this.gangs=gangs;this.initiate=initiate;this.resume=resume;this.territories=territories;this.commerce=commerce;this.treasury=treasury;this.diplomacy=diplomacy;this.warfare=warfare;this.assault=assault;
            menu=new NativeMenu("Lider da gangue","CONVERSA PRESENCIAL");pool.Add(menu);Visuals.Register(menu,"gangs");
        }
        bool Safe=>state().PresenceEnabled && gate.Owner==null && Wallet.GangWarEconomyFree && (state().Turf==null || state().Turf.Valid && state().Turf.Pending==null) && (state().Warfare==null || state().Warfare.Valid && state().Warfare.Alert==null) && (state().DriveBys==null || state().DriveBys.Valid && state().DriveBys.Pending==null) && Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && Game.Player.Character.IsOnFoot && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && Game.Player.Wanted.WantedLevel==0 && !Game.IsPaused && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && !InterfaceOptions.ModalOpen;
        bool Bound=>selected!=null && Safe && profile==Profile.Id && player==Game.Player.Character && string.Equals(affiliation,state().Gang,StringComparison.OrdinalIgnoreCase) && leader!=null && leader.Exists() && !leader.IsDead && presence.Leader(selected.Name)==leader && GangLeaderRules.Near(player.Position.DistanceTo(leader.Position),true);
        GangDefinition Closest()=>Safe?gangs().Where(g=>{var p=presence.Leader(g.Name);return p!=null && GangLeaderRules.Near(Game.Player.Character.Position.DistanceTo(p.Position),false);}).OrderBy(g=>Game.Player.Character.Position.DistanceTo(presence.Leader(g.Name).Position)).FirstOrDefault():null;
        void Add(string title,string description,Action action,bool enabled=true)
        {
            var row=new NativeItem(title,description){Enabled=enabled};row.Activated+=(s,e)=>{try{if(!Bound){Close();Notify.Show("Conversa encerrada; aproxime-se do lider novamente.");return;}action();}catch(Exception ex){Close();Log.Error("gang leader dialogue",ex);Notify.Show("Acao nao concluida; dados anteriores conservados.");}};menu.Add(row);
        }
        bool PendingFor(string name)=>GangCrewRules.SameGang(state().Loyalty?.Pending?.Gang,name) || GangCrewRules.SameGang(state().Strength?.Pending?.Gang,name) || GangCrewRules.SameGang(state().Driving?.Pending?.Gang,name);
        void Forward(Action action){Close();action();}
        public bool TryOpen()
        {
            if(pool.AreAnyVisible)return false;var g=Closest();if(g==null)return false;
            selected=g;leader=presence.Leader(g.Name);player=Game.Player.Character;profile=Profile.Id;affiliation=state().Gang;menu.Clear();menu.Name=g.Name;
            bool free=GangLoyaltyRules.Unaffiliated(affiliation),own=GangCrewRules.SameGang(affiliation,g.Name);int type=GangLeaderRules.Initiation(g);
            Add(own?"Voce ja e um dos nossos.":free?"Quer entrar? Primeiro prove seu valor.":"Voce ja esta com outra gangue.",g.Description??"Converse com o lider para conhecer os caminhos da gangue.",()=>{});
            if(PendingFor(g.Name))Add("Retomar iniciacao desta gangue","Conserva provas/etapas salvas. As regras de retomada sao as da iniciacao existente. Se pronta, retome e confirmeE no lider.",()=>{var gang=selected;Close();if(!resume(gang))Notify.Show("Confira iniciacao, proximidade, filiacao e pendencias.");});
            else if(free){string label=type==0?"lealdade":type==1?"forca":type==2?"direcao":"indisponivel";
                bool otherPending=state().Loyalty?.Pending!=null || state().Strength?.Pending!=null || state().Driving?.Pending!=null;
                string detail=otherPending?"Conclua ou cancele a iniciacao pendente de outra gangue pelo F4.":type==0?"Busque dois pacotes e volte ao lider. Voce precisa continuar sem outra filiacao.":type==1?"Esteja desarmado e com vida acima de100. Enfrente tres desafiantes para provar sua forca.":type==2?"Iniciacao com passageiro, alvo rival, fuga e retorno. Voce conduz um carro proprio; o membro fornece apoio.":"Esta gangue nao possui uma iniciacao valida configurada.";
                Add("Pedir iniciacao: "+label,detail,()=>{var gang=selected;Close();if(!initiate(gang))Notify.Show("Confira proximidade do lider e os requisitos da iniciacao.");},type>=0 && !otherPending);
            }
            if(own){
                Add("Territorios / situacao","Mapa, donos, captura e defesa existentes.",()=>Forward(territories));Add("Planejar assalto organizado","Abre o comboio0.79; inicio continua exigindo area propria e vagas livres.",()=>Forward(assault));
                Add("Negocios / trabalho territorial","Estoque, contratos e atividades existentes.",()=>Forward(commerce));Add("Tesouraria / pagamentos","Consulta e operacoes financeiras existentes; requisitos de HQ/banco mantidos.",()=>Forward(treasury));Add("Relacoes / acordos","Declaracao e acordos existentes. Se o lider estiver afastado do centro da HQ, aproxime-se do ponto administrativo para confirmar operacoes.",()=>Forward(diplomacy));Add("Guerras / alertas","Preferencias e alertas existentes; nada e ligado automaticamente.",()=>Forward(warfare));
                Add("Sair da gangue — confirmar","CONFIRMA AO SELECIONAR. Como o Leave existente: remove afiliacao e zera reputacao/patente atual. Nao apaga donos, historicos, dinheiro ou estoque. Exige encerrar iniciacoes, disputas, contratos e alertas.",()=>{var name=selected.Name;if(!Wallet.GangWarEconomyFree || !GangLeaderRules.Leave(state(),name,save,out _)){Notify.Show("Conclua/cancele as pendencias antes de sair.");return;}Close();Notify.Show("Voce deixou "+name+". Saldos, estoque e registros conservados.");},GangLeaderRules.CanLeave(state(),g.Name));
            }
            Add("Encerrar conversa","Voltar ao jogo sem alterar a filiacao.",Close);menu.Visible=true;return true;
        }
        public void Tick()
        {
            if(menu.Visible){if(!Bound)Close();return;}if(pool.AreAnyVisible)return;
            var g=Closest();if(g!=null)GTA.UI.Screen.ShowHelpTextThisFrame("E: conversar com o lider de "+g.Name);
        }
        public void Close(){menu.Visible=false;selected=null;leader=player=null;profile=affiliation=null;}
    }
}
