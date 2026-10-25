using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;

namespace RPCore
{
    internal sealed class GangBusiness
    {
        const string OwnerKey="gang_business";
        readonly ShiftGate gate; readonly ObjectPool pool; readonly Func<GangState> state;
        readonly Func<IEnumerable<GangDefinition>> gangs; readonly GangTurfOptions territories;
        readonly GangBusinessOptions options; readonly NativeMenu menu;readonly GangSupply supply;readonly GangLaundering laundering;readonly GangWarehouse warehouse;readonly GangRaid raid;
        public void ConfigureDealer(DealerJob dealer){laundering.Dealer=dealer;warehouse.Dealer=dealer;supply.Dealer=dealer;raid.Dealer=dealer;}
        GangBusinessService service; GangTurfDefinition area; string profile,gang,id; int player;
        public string SuspensionMessage=>raid.Active?"Roubo suspenso. F4 > Comercio: retomar ou cancelar contrato.":supply.Active?"Frete suspenso. Reserva paga salva; F4 retoma ou estorna.":laundering.Active?"Processamento suspenso; receita nao consumida. Reinicie pelo F4.":"Atendimento encerrado; nenhum estoque nao confirmado foi consumido.";
        public bool Active=>service!=null || supply.Active || laundering.Active || raid.Active;
        static Vector3 Pos(GangTurfDefinition a)=>new Vector3(a.Center.X,a.Center.Y,a.Center.Z);
        public GangBusiness(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs)
        {
            this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;
            territories=Store.LoadDef("Gangs/Territories.json",GangTurfOptions.Defaults);
            if(territories==null || !territories.Valid)territories=GangTurfOptions.Defaults();
            options=Store.LoadDef("Gangs/Business.json",()=>new GangBusinessOptions());
            if(options==null || !options.Valid){options=new GangBusinessOptions();Log.Write("Invalid gang business config; original retained, defaults used.");}
            laundering=new GangLaundering(gate,pool,state,gangs);
            supply=new GangSupply(gate,pool,state,gangs,()=>territories.Areas,options);
            warehouse=new GangWarehouse(gate,pool,state,gangs,supply);
            raid=new GangRaid(gate,pool,state,gangs,()=>territories.Areas);
            menu=new NativeMenu("Comercio territorial","ESTOQUE / ATENDIMENTO / RECEITA");pool.Add(menu);Visuals.Register(menu,"gangs");
        }
        bool Safe(GangTurfDefinition a)
        {
            var p=Game.Player.Character;var s=state();
            return GangBusinessTransactions.Owns(s,a) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,s.Gang)) && p.IsHuman && !p.IsDead && p.IsOnFoot && !p.IsInCombat && Game.Player.Wanted.WantedLevel==0 && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && p.Position.DistanceTo(Pos(a))<=options.Radius;
        }
        void Add(string title,string description,Action action=null)
        {
            var row=new NativeItem(title,description);row.Activated+=(s,e)=>{try{action?.Invoke();}catch(Exception ex){Suspend();Log.Error("gang business",ex);Notify.Show("Acao interrompida; confira estoque/pendencia no F4. Falha de gravacao conserva o estado anterior.");}};menu.Add(row);
        }
        public void Open()
        {
            pool.HideAll();menu.Clear();
            if(Wallet.GangRaid!=null && !Wallet.GangRaid.Valid || Wallet.GangWarehouse!=null && !Wallet.GangWarehouse.Valid || Wallet.GangIllicit!=null && !Wallet.GangIllicit.Valid || Wallet.GangBusiness!=null && !Wallet.GangBusiness.Valid || Wallet.GangTreasury!=null && !Wallet.GangTreasury.Valid){Add("Dados protegidos","Confira Wallet.json/backup; nenhum estoque ou saldo sera substituido.");menu.Visible=true;return;}
            Add("Banco: $"+Wallet.Bank,"Comercio proprio, presencial. Receita vai ao caixa da gangue; nao ao bolso. Sem vendas offline.");
            raid.Controls(Add,Open);supply.Append(Add,Open);laundering.Controls(Add,Open);
            if(service!=null)Add("Encerrar atendimento atual","Tempo nao confirmado e descartado; nenhuma mercadoria e consumida.",()=>{Suspend();Open();});
            var available=territories.Areas.Where(a=>GangBusinessTransactions.Owns(state(),a)).ToList();
            if(available.Count==0)Add("Nenhum territorio disponivel","Filie-se e controle uma area pelo F4 > Territorios. Resolva disputas/iniciacoes pendentes antes de comerciar.");
            foreach(var a in available)
            {
                var target=a;var account=GangBusinessTransactions.Account(Wallet.GangBusiness,state().Gang,a.Id);
                Add(a.Name+" / estoque "+(account?.Stock??0)+"/"+options.Capacity,"Receita historica: $"+(account?.Revenue??0)+". Compra $"+options.UnitCost+"/un; venda $"+options.UnitPrice+"/un.");
                Add("GPS: "+a.Name,"Atendimento a pe, a ate "+options.Radius+"m do ponto do territorio.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,target.Center.X,target.Center.Y);menu.Visible=false;});
                Add("Repor "+options.PurchaseUnits+" unidades / $"+(options.PurchaseUnits*options.UnitCost),"Debita banco e acrescenta estoque local. Compra direta opcional. O frete abaixo exige buscar e descarregar caixas com desconto.",()=>{if(Active || gate.Owner!=null || !Safe(target) || Game.IsPaused || InterfaceOptions.ModalOpen || !Wallet.GangTrade(state(),target,options,false,GangTreasury.Clock(),Guid.NewGuid().ToString("N"),out _))Notify.Show("Confira posse, proximidade, banco, capacidade e atividade.");Open();});
                raid.AddStart(target,Add);
                supply.AddStart(target,Add);
                laundering.Append(target,Add);
                Add("Armazem criminal: "+target.Name,"Guardar/retirar pacotes do trafico e vender do estoque local; separado de mercadorias legais.",()=>warehouse.Open(target));
                Add("Atender "+options.SaleUnits+" unidades / $"+(options.SaleUnits*options.UnitPrice),"Permaneca no local por "+options.ServiceSeconds+"s ativos e confirme E. Pausa/menu nao contam. END cancela sem consumir estoque.",()=>{if(!Begin(target))Notify.Show("Confira estoque, posse, proximidade e atividade.");});
            }
            Add("Fechar","Voltar ao jogo.",()=>menu.Visible=false);menu.Visible=true;
        }
        bool Begin(GangTurfDefinition target)
        {
            if(Active || Wallet.GangRaid?.Pending!=null || Wallet.GangBusiness?.Supply!=null || !Safe(target) || Game.IsPaused || InterfaceOptions.ModalOpen || Wallet.GangBusiness!=null && !Wallet.GangBusiness.Valid || Wallet.GangTreasury!=null && !Wallet.GangTreasury.Valid || (GangBusinessTransactions.Account(Wallet.GangBusiness,state().Gang,target.Id)?.Stock??0)<options.SaleUnits || !gate.Acquire(OwnerKey))return false;
            area=target;profile=Profile.Id;gang=state().Gang;player=Game.Player.Character.Handle;id=Guid.NewGuid().ToString("N");service=new GangBusinessService(Game.GameTime);pool.HideAll();
            Notify.Show("Atendimento iniciado. Permaneça no ponto; E confirma ao terminar, END encerra.");return true;
        }
        bool Current=>service!=null && profile==Profile.Id && player==Game.Player.Character.Handle && GangCrewRules.SameGang(gang,state().Gang) && gate.Owner==OwnerKey && Safe(area);
        public void Tick()
        {
            raid.Tick();laundering.Tick();supply.Tick();
            if(service==null)return;
            if(!Current){Suspend();Notify.Show("Atendimento encerrado; estoque e receita nao alterados.");return;}
            bool working=!Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
            service.Step(Game.GameTime,working);
            if(working){GTA.UI.Screen.ShowSubtitle(area.Name+" / atendimento "+Math.Min(options.ServiceSeconds,service.Milliseconds/1000)+"/"+options.ServiceSeconds+"s / END encerra",100);if(service.Ready(options.ServiceSeconds))GTA.UI.Screen.ShowHelpTextThisFrame("E: confirmar venda e enviar receita para a tesouraria");}
        }
        public void Interact()
        {
            raid.Interact();laundering.Interact();supply.Interact();
            if(!Current || Game.IsPaused || pool.AreAnyVisible || InterfaceOptions.ModalOpen || !service.Ready(options.ServiceSeconds))return;
            if(Wallet.GangTrade(state(),area,options,true,GangTreasury.Clock(),id,out bool changed))
            {Suspend();Notify.Show(changed?"Venda gravada. Receita enviada ao caixa da gangue; confira F4 > Tesouraria.":"Venda ja confirmada; nenhum pagamento repetido.");}
            else{Suspend();Notify.Show("Venda recusada. Confira estoque, posse e limites do caixa; sem debito parcial.");}
        }
        public void Suspend(){raid.Suspend();laundering.Suspend();supply.Suspend();service=null;area=null;profile=gang=id=null;gate.Release(OwnerKey);}
        public void Close(){warehouse.Close();Suspend();menu.Visible=false;}
    }
}
