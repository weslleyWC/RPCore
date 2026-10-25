using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using LemonUI;
using LemonUI.Menus;
namespace RPCore
{
    internal sealed class GangTreasury
    {
        readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Func<IEnumerable<GangDefinition>> gangs;readonly GangTreasuryOptions options;readonly NativeMenu menu;
        public GangTreasury(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs)
        {this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;options=Store.LoadDef("Gangs/Treasury.json",()=>new GangTreasuryOptions());if(options==null || !options.Valid){options=new GangTreasuryOptions();Log.Write("Invalid treasury options; original retained, defaults used.");}menu=new NativeMenu("Tesouraria da gangue","BANCO / CAIXA / PAGAMENTOS");pool.Add(menu);Visuals.Register(menu,"gangs");}
        internal static DateTime Clock()=>new DateTime(GTA.Native.Function.Call<int>(GTA.Native.Hash.GET_CLOCK_YEAR),GTA.Native.Function.Call<int>(GTA.Native.Hash.GET_CLOCK_MONTH)+1,GTA.Native.Function.Call<int>(GTA.Native.Hash.GET_CLOCK_DAY_OF_MONTH),GTA.Native.Function.Call<int>(GTA.Native.Hash.GET_CLOCK_HOURS),GTA.Native.Function.Call<int>(GTA.Native.Hash.GET_CLOCK_MINUTES),GTA.Native.Function.Call<int>(GTA.Native.Hash.GET_CLOCK_SECONDS));
        bool AtHQ()
        {var s=state();var p=Game.Player.Character;var g=gangs().FirstOrDefault(a=>GangCrewRules.SameGang(a.Name,s.Gang));return g!=null && GangProgressionRules.Valid(s) && !GangLoyaltyRules.Unaffiliated(s.Gang) && gate.Owner==null && p.IsOnFoot && !p.IsDead && !p.IsInCombat && Game.Player.Wanted.WantedLevel==0 && !Game.IsPaused && !Game.IsMissionActive && !InterfaceOptions.ModalOpen && !GTA.Native.Function.Call<bool>(GTA.Native.Hash.IS_CUTSCENE_ACTIVE) && p.Position.DistanceTo(g.Position)<=5;}
        void Add(string label,string description,Func<bool> action=null)
        {var item=new NativeItem(label,description);item.Activated+=(s,e)=>{if(action==null)return;try{if(!AtHQ()){Notify.Show("Va a pe a HQ da sua gangue, sem procurado/atividade.");return;}if(!action())Notify.Show("Confira saldo, prazo, limites e dados. Nenhuma operacao confirmada.");Open();}catch(Exception ex){Log.Error("gang treasury",ex);Notify.Show("Operacao nao gravada; saldos anteriores conservados.");}};menu.Add(item);}
        public void Open()
        {
            pool.HideAll();menu.Clear();var s=state();
            if(GangLoyaltyRules.Unaffiliated(s.Gang) || !GangProgressionRules.Valid(s) || Wallet.GangTreasury!=null && !Wallet.GangTreasury.Valid){Add("Tesouraria indisponivel","Confira filiacao/dados; saldos protegidos.");menu.Visible=true;return;}
            int rank=GangProgressionRules.Rank(s);var a=GangTreasuryTransactions.Account(Wallet.GangTreasury,s.Gang);menu.Name=s.Gang+" / banco $"+Wallet.Bank;
            Add("Caixa: $"+(a?.Balance??0)+" / "+GangProgressionRules.Name(rank),"Participacao: "+GangProgressionRules.Share(rank)+"% do saldo atual. Depositos, comercio e receita processada financiam este caixa; participacao sai dele e entra no banco.");
            Add("Prazo: "+options.PaydayHours+" horas do jogo",a==null?"Primeiro deposito inicia o prazo. Nao ha salario automatico ou pagamento retroativo.":"Ultima referencia: "+new DateTime(a.LastPaydayTicks).ToString("yyyy-MM-dd HH:mm")+". Voltar o relogio nao antecipa pagamento.");
            foreach(int n in new[]{100,1000,10000}){int amount=n;if(amount>options.MaxTransfer)continue;
                Add("Depositar $"+amount+" do banco","Operacao presencial na HQ; dinheiro real do seu banco vai ao caixa desta gangue/perfil.",()=>Wallet.GangDeposit(state().Gang,amount,Clock(),options,Guid.NewGuid().ToString("N"),out _));
                Add("Retirar $"+amount+" para o banco","Debita somente saldo disponivel do caixa. Sem taxa; nao reinicia o prazo.",()=>Wallet.GangWithdraw(state().Gang,amount,options,Guid.NewGuid().ToString("N"),out _));
            }
            Add("Receber participacao da patente","Apos prazo, recebe uma porcentagem do caixa disponivel no banco. Uma coleta por intervalo; sem multiplicar periodos offline.",()=>Wallet.GangPayday(state().Gang,GangProgressionRules.Rank(state()),Clock(),options,Guid.NewGuid().ToString("N"),out _));
            var gps=new NativeItem("GPS da HQ","Marca sua HQ para atendimento presencial.");gps.Activated+=(x,y)=>{var g=gangs().FirstOrDefault(v=>GangCrewRules.SameGang(v.Name,state().Gang));if(g!=null){GTA.Native.Function.Call(GTA.Native.Hash.SET_NEW_WAYPOINT,g.HQX,g.HQY);menu.Visible=false;}};menu.Add(gps);var close=new NativeItem("Fechar","Voltar ao jogo.");close.Activated+=(x,y)=>menu.Visible=false;menu.Add(close);menu.Visible=true;
        }
        public void Close()=>menu.Visible=false;
    }
}
