using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
namespace RPCore
{
    internal sealed class GangLaundering
    {
        const string Owner="gang_launder";readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Func<IEnumerable<GangDefinition>> gangs;
        readonly GangLaunderingOptions options;GangBusinessService service;GangTurfDefinition area;string profile,gang,id;int player,amount;
        public DealerJob Dealer;public bool Active=>service!=null;
        public GangLaundering(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs)
        {this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;options=Store.LoadDef("Gangs/Laundering.json",()=>new GangLaunderingOptions());if(options==null || !options.Valid){options=new GangLaunderingOptions();Log.Write("Invalid laundering config; original retained, defaults used.");}}
        static Vector3 Pos(GangTurfDefinition a)=>new Vector3(a.Center.X,a.Center.Y,a.Center.Z);
        bool Safe(GangTurfDefinition a)=>GangBusinessTransactions.Owns(state(),a) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang)) && Wallet.GangBusiness?.Supply==null && !Wallet.GangBulkBlocked && Wallet.GangRaid?.Pending==null && Game.Player.Character.IsHuman && Game.Player.Character.IsOnFoot && !Game.Player.Character.IsDead && !Game.Player.Character.IsInCombat && Game.Player.Wanted.WantedLevel==0 && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && Game.Player.Character.Position.DistanceTo(Pos(a))<=options.Radius;
        public void Controls(Action<string,string,Action> add,Action reopen){if(Active)add("Suspender processamento atual","Descarta apenas tempo nao confirmado; conserva dinheiro sujo e caixa.",()=>{Suspend();reopen();});}
        public void Append(GangTurfDefinition target,Action<string,string,Action> add)
        {
            var a=GangIllicitTransactions.Account(Wallet.GangIllicit,state().Gang,target.Id);
            add("Dinheiro sujo: $"+(a?.Dirty??0)+" / "+target.Name,"Vendas territoriais usam estoque do trafico; receita ainda nao pode ser retirada. Processado $"+(a?.Processed??0)+" / liquido $"+(a?.Net??0)+" / taxa $"+(a?.Fees??0)+".",null);
            add("Vendas ilegais: "+target.Name,"Requer pacotes comprados no contato de trafico. Compradores/negociacao/riscos existentes; fique na area de "+options.SaleRadius+"m. Receita suja fica nesta gangue/area/perfil.",()=>{if(Dealer==null || gate.Owner!=null || !Safe(target) || Game.IsPaused || InterfaceOptions.ModalOpen || !Dealer.BeginTerritory(state,target,options.SaleRadius))Notify.Show("Confira estoque do trafico, posse, reserva/atividade e proximidade a pe/desarmado.");});
            add("Processar ate $"+options.BatchAmount+" / 85% ao caixa","Fique a pe no ponto por "+options.Seconds+"s ativos, depois E. Taxa15%; usa apenas receita suja ja vendida. END suspende sem debito.",()=>{if(!Begin(target))Notify.Show("Confira receita suja, posse, reserva/atividade e proximidade em seguranca.");});
        }
        bool Begin(GangTurfDefinition target)
        {
            var a=GangIllicitTransactions.Account(Wallet.GangIllicit,state().Gang,target.Id);
            if(Active || !Safe(target) || Game.IsPaused || InterfaceOptions.ModalOpen || a==null || a.Dirty<1 || Wallet.GangTreasury!=null && !Wallet.GangTreasury.Valid || !gate.Acquire(Owner))return false;
            amount=Math.Min(a.Dirty,options.BatchAmount);area=CharacterRules.Copy(target);profile=Profile.Id;gang=state().Gang;player=Game.Player.Character.Handle;id=Guid.NewGuid().ToString("N");service=new GangBusinessService(Game.GameTime);pool.HideAll();
            WorkContext.ShopDelivery.Set(Owner,new JobPoint{Name="gang processing",X=area.Center.X,Y=area.Center.Y,Z=area.Center.Z},options.Radius);Notify.Show("Processamento iniciado. E ao terminar; END suspende sem consumir receita.");return true;
        }
        bool Current=>Active && profile==Profile.Id && player==Game.Player.Character.Handle && GangCrewRules.SameGang(gang,state().Gang) && gate.Owner==Owner && Safe(area);
        public void Tick()
        {
            if(!Active)return;if(!Current){Suspend();Notify.Show("Processamento suspenso; receita anterior conservada.");return;}
            bool working=!Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;service.Step(Game.GameTime,working);
            if(working){GTA.UI.Screen.ShowSubtitle(area.Name+" / processar $"+amount+" / "+Math.Min(options.Seconds,service.Milliseconds/1000)+"/"+options.Seconds+"s / END suspende",100);if(service.Ready(options.Seconds))GTA.UI.Screen.ShowHelpTextThisFrame("E: processar receita e enviar85% para o caixa da gangue");}
        }
        public void Interact()
        {
            if(!Current || Game.IsPaused || pool.AreAnyVisible || InterfaceOptions.ModalOpen || !service.Ready(options.Seconds))return;
            if(Wallet.GangLaunder(state(),area,options,amount,GangTreasury.Clock(),id,out bool changed)){Suspend();Notify.Show(changed?"Receita processada. Liquido no caixa da gangue; confira F4 > Tesouraria.":"Operacao ja confirmada, sem credito repetido.");}
            else{Suspend();Notify.Show("Processamento recusado; confira dados/limites. Sem debito parcial.");}
        }
        public void Suspend(){service=null;area=null;profile=gang=id=null;WorkContext.ShopDelivery.Clear(Owner);gate.Release(Owner);}
    }
}
