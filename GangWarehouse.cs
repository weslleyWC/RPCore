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
    internal sealed class GangWarehouse
    {
        readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Func<IEnumerable<GangDefinition>> gangs;
        readonly GangSupply supply;readonly NativeMenu menu;readonly GangWarehouseOptions options;GangTurfDefinition area;
        public DealerJob Dealer;
        public GangWarehouse(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs,GangSupply supply)
        {this.supply=supply;this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;options=Store.LoadDef("Gangs/Warehouse.json",()=>new GangWarehouseOptions());if(options==null || !options.Valid){options=new GangWarehouseOptions();Log.Write("Invalid warehouse config; original retained, defaults used.");}menu=new NativeMenu("Armazem criminal","PACOTES / TRANSFERENCIAS / VENDAS");pool.Add(menu);Visuals.Register(menu,"gangs");}
        static Vector3 Pos(GangTurfDefinition a)=>new Vector3(a.Center.X,a.Center.Y,a.Center.Z);
        bool Safe=>area!=null && GangBusinessTransactions.Owns(state(),area) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang)) && gate.Owner==null && Wallet.GangBusiness?.Supply==null && Game.Player.Character.IsHuman && Game.Player.Character.IsOnFoot && !Game.Player.Character.IsDead && !Game.Player.Character.IsInCombat && Game.Player.Wanted.WantedLevel==0 && !Game.IsPaused && !Game.IsMissionActive && !InterfaceOptions.ModalOpen && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && Game.Player.Character.Position.DistanceTo(Pos(area))<=options.Radius;
        void Add(string title,string description,Action action=null)
        {var row=new NativeItem(title,description);row.Activated+=(s,e)=>{try{action?.Invoke();}catch(Exception ex){Log.Error("gang warehouse",ex);Notify.Show("Operacao interrompida; confira estoque. Falha de gravacao conserva dados anteriores.");}};menu.Add(row);}
        public void Open(GangTurfDefinition target)
        {
            area=target;pool.HideAll();menu.Clear();var c=Dealer?.Catalog;
            if(c==null || !c.Valid || !Wallet.Dealer.Valid || Wallet.GangWarehouse!=null && !Wallet.GangWarehouse.Valid){Add("Dados indisponiveis/protegidos","Confira catalogo e livros; nenhum estoque sera substituido.");menu.Visible=true;return;}
            if(!GangBusinessTransactions.Owns(state(),area)){Add("Area indisponivel","Confira posse, filiacao e pendencias. Estoques antigos conservados.");menu.Visible=true;return;}
            supply.Append(Add,()=>Open(area));
            var a=GangWarehouseTransactions.Account(Wallet.GangWarehouse,state().Gang,area.Id);menu.Name=area.Name+" / "+state().Gang;
            Add("Estoque local: "+GangWarehouseTransactions.Weight(a,c).ToString("0.0")+"/"+options.Capacity+" kg","Armazem separado por gangue/area/perfil. Guardar/retirar move pacotes existentes, sem dinheiro, compra ou recompensa.");
            foreach(var product in c.Products)
            {
                var p=product;int held=GangWarehouseAccount.Get(Wallet.Dealer.Stock,p.Id),stored=a==null?0:GangWarehouseAccount.Get(a.Stock,p.Id);
                Add(p.Name+" / pessoal "+held+" / local "+stored,"Peso "+p.Weight+" kg/un. Depositado "+(a==null?0:GangWarehouseAccount.Get(a.Deposited,p.Id))+" / retirado "+(a==null?0:GangWarehouseAccount.Get(a.Withdrawn,p.Id))+" / vendido "+(a==null?0:GangWarehouseAccount.Get(a.Sold,p.Id))+" / frete "+(a==null?0:GangWarehouseAccount.Get(a.Supplied,p.Id))+".");
                supply.AddCriminal(area,c,options,p.Id,Add);
                foreach(int n in new[]{1,5})
                {
                    int count=n;
                    Add("Guardar "+count+" / "+p.Name,"A pe na area, sem procurado/atividade/frete pendente. Sai do estoque pessoal do trafico e entra neste armazem.",()=>Transfer(c,p.Id,count,true));
                    Add("Retirar "+count+" / "+p.Name,"Respeita peso do estoque pessoal e limite999/un por produto. Sem alterar banco ou inventario pessoal do personagem.",()=>Transfer(c,p.Id,count,false));
                }
            }
            if(a!=null && a.Stock.Any(p=>p.Value>0 && !c.Products.Any(v=>v.Id==p.Key)))Add("Produtos fora do catalogo preservados","Volte a disponibilizar o mesmo ID no Dealer.json para gerenciar. Peso conservador1kg/un; nada e apagado.");
            Add("Vender do armazem","Inicia compradores do trafico usando apenas estoque local. Requer pacotes para turno; receita suja fica nesta area. Estoque pessoal nao complementa faltas.",()=>{if(!Safe || !Dealer.BeginTerritory(state,area,options.SaleRadius,options))Notify.Show("Confira estoque local para turno, posse, proximidade a pe/desarmado e atividade.");});
            Add("GPS do territorio","Marca o ponto de atendimento presencial.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,area.Center.X,area.Center.Y);menu.Visible=false;});
            Add("Fechar","Voltar ao jogo.",()=>menu.Visible=false);menu.Visible=true;
        }
        void Transfer(DealerOptions c,string product,int count,bool deposit)
        {if(!Safe || !Wallet.GangWarehouseTransfer(state(),area,c,options,product,count,deposit,Guid.NewGuid().ToString("N"),out _))Notify.Show("Confira proximidade/posse, quantidade, peso, limites e frete/atividade.");Open(area);}
        public void Close(){menu.Visible=false;area=null;}
    }
}
