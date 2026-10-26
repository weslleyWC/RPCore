using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
namespace RPCore
{
    internal sealed class GangDiplomacy
    {
        readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Func<IEnumerable<GangDefinition>> gangs;readonly GangDiplomacyOptions options;readonly NativeMenu menu;
        public GangDiplomacy(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs)
        {
            this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;
            options=Store.LoadDef("Gangs/Diplomacy.json",()=>new GangDiplomacyOptions());if(options?.Valid!=true){options=new GangDiplomacyOptions();Log.Write("Invalid diplomacy config; original retained, defaults used.");}
            menu=new NativeMenu("Relacoes das gangues","ACORDOS / CONSEQUENCIAS");pool.Add(menu);Visuals.Register(menu,"gangs");
        }
        void Add(string title,string description,Action action=null){var item=new NativeItem(title,description);if(action!=null)item.Activated+=(s,e)=>{try{action();}catch(Exception ex){Log.Error("gang diplomacy",ex);Notify.Show("Acordo nao confirmado. Banco e relacoes anteriores conservados.");}};menu.Add(item);}
        bool Safe(GangDefinition own)
        {
            var p=Game.Player.Character;
            return own!=null && GangPresenceRules.Position(GangPresenceRules.HQ(own)) && p!=null && p.Exists() && p.IsHuman && p.IsAlive && p.IsOnFoot && !p.IsInCombat && !p.IsRagdoll && Game.Player.Wanted.WantedLevel==0 && !Game.IsPaused && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && !InterfaceOptions.ModalOpen && gate.Owner==null && !Wallet.GangBulkBlocked && Wallet.GangRaid?.Pending==null && Wallet.GangBusiness?.Supply==null && p.Position.DistanceTo(own.Position)<=5;
        }
        public void Open()
        {
            pool.HideAll();menu.Clear();var s=state();var own=gangs().FirstOrDefault(g=>GangCrewRules.SameGang(g.Name,s?.Gang));
            if(own==null || !GangProgressionRules.Valid(s) || Wallet.GangDiplomacy!=null && !Wallet.GangDiplomacy.Valid){Add("Relacoes indisponiveis/protegidas","Confira afiliacao e dados. Nenhum arquivo sera substituido.");menu.Visible=true;return;}
            string profile=Profile.Id,affiliation=s.Gang;menu.Name=affiliation+" / banco $"+Wallet.Bank;
            Add("Acordo: $"+options.AgreementPrice+" / "+options.CooldownHours+"h do jogo","Na sua HQ a pe, sem procurado/atividade/contrato pendente. Cada acordo sobe um nivel; preco sai do banco. Sem reembolso, salario ou reputacao.");
            Add("Roubar carga piora a relacao","Na revista confirmada: Respeito > Neutro > Hostilidade > Guerra. Cancelar depois nao desfaz a ofensa. Ataques por hostilidade/guerra dependem da opcao no menu Guerras; desligada em saves antigos.");
            foreach(var target in gangs().Where(g=>!GangCrewRules.SameGang(g.Name,affiliation)))
            {
                var rival=target;var a=GangDiplomacyRules.Account(Wallet.GangDiplomacy,affiliation,rival.Name);string attitude=GangDiplomacyRules.Attitude(Wallet.GangDiplomacy,affiliation,rival);
                Add(rival.Name+": "+GangDiplomacyRules.Label(attitude),"Opiniao desta gangue sobre a sua. Cargas roubadas: "+(a?.StolenLoads??0)+" / lotes vendidos: "+(a?.BulkDeals??0)+" / acordos: "+(a?.Agreements??0)+" / gasto: $"+(a?.Paid??0)+(a?.Agreements>0?" / ultimo: "+new DateTime(a.LastAgreementTicks).ToString("yyyy-MM-dd HH:mm"):"")+". Sem mudanca global na IA.");
                if(attitude!="War"){
                    string declarationId=Guid.NewGuid().ToString("N");
                    Add("Declarar guerra a "+rival.Name,"CONFIRMA AO SELECIONAR na sua HQ. Define Guerra sem cobrar dinheiro. Habilita este rival para alertas se voce ligar ataques no F4. Acordos posteriores melhoram um nivel por vez.",()=>{
                        if(profile!=Profile.Id || !GangCrewRules.SameGang(affiliation,state()?.Gang) || !Safe(own)){Notify.Show("Va a pe a sua HQ e encerre atividades/pendencias.");return;}
                        if(Wallet.GangDeclareWar(state(),rival.Name,GangDiplomacyRules.Initial(rival,affiliation),declarationId,out bool changed))Notify.Show(changed?"Guerra declarada. Ataques seguem a preferencia do menu Guerras.":"Declaracao ja registrada.");else Notify.Show("Declaracao nao registrada; confira relacoes e pendencias.");Open();
                    });
                }
                if(attitude=="Respect")continue;string id=Guid.NewGuid().ToString("N");
                Add("Negociar com "+rival.Name+" por $"+options.AgreementPrice,"Proximo nivel: "+GangDiplomacyRules.Label(GangDiplomacyRules.Improve(attitude))+". Confirma a compra ao selecionar. Intervalo por par de gangues/perfil; voltar o relogio nao antecipa acordo.",()=>{
                    if(profile!=Profile.Id || !GangCrewRules.SameGang(affiliation,state()?.Gang) || !Safe(own)){Notify.Show("Va a pe a sua HQ, sem procurado/atividade e encerre contratos pendentes.");return;}
                    if(!Wallet.GangNegotiate(state(),rival.Name,GangDiplomacyRules.Initial(rival,affiliation),options,GangTreasury.Clock(),id,out bool changed))Notify.Show("Confira banco, intervalo e dados; acordo nao confirmado.");
                    else Notify.Show(changed?"Acordo registrado. Relacao melhorou um nivel; preco debitado do banco.":"Acordo ja registrado; sem debito repetido.");Open();
                });
            }
            Add("GPS da sua HQ","Atendimento diplomatico presencial no centro da sua HQ.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,own.HQX,own.HQY);menu.Visible=false;});
            Add("Fechar","Voltar ao jogo.",()=>menu.Visible=false);menu.Visible=true;
        }
        public void Close()=>menu.Visible=false;
    }
}
