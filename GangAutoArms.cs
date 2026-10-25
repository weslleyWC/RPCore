using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
namespace RPCore
{
    internal sealed class GangAutoArms
    {
        readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Func<IEnumerable<GangDefinition>> gangs;readonly Func<IEnumerable<GangTurfDefinition>> areas;
        readonly GangAutoArmsOptions options;readonly GangArmsOptions arms;readonly GangWarehouseOptions warehouse;readonly Random random=new Random();
        readonly Dictionary<string,GangAutoArmsClock> clocks=new Dictionary<string,GangAutoArmsClock>();readonly HashSet<string> faulted=new HashSet<string>();string profile;int player;
        static string Key(string gang,int area)=>gang.ToUpperInvariant()+"|"+area;
        public GangAutoArms(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs,Func<IEnumerable<GangTurfDefinition>> areas)
        {
            this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;this.areas=areas;
            options=Store.LoadDef("Gangs/AutoArms.json",()=>new GangAutoArmsOptions());if(options?.Valid!=true){options=new GangAutoArmsOptions();Log.Write("Invalid automatic arms config; defaults in memory, original retained.");}
            arms=Store.LoadDef("Gangs/Arms.json",()=>new GangArmsOptions());if(arms?.Valid!=true)arms=new GangArmsOptions();
            warehouse=Store.LoadDef("Gangs/Warehouse.json",()=>new GangWarehouseOptions());if(warehouse?.Valid!=true)warehouse=new GangWarehouseOptions();
        }
        void Reset(string gang,int area){var key=Key(gang,area);clocks.Remove(key);faulted.Remove(key);}
        public void Controls(Action<string,string,Action> add,Action reopen)
        {
            if(Wallet.GangAutoArms!=null && !Wallet.GangAutoArms.Valid){add("Automacao de armas: dados protegidos","Confira Wallet/backup; nenhuma configuracao ou estoque sera substituido.",null);return;}
            foreach(var item in Wallet.GangAutoArms?.Accounts??new List<GangAutoArmsAccount>())
            {
                if(item.Products.Count==0)continue;var a=item;
                add("Auto / "+a.Gang+" / area "+a.Area+": "+string.Join(", ",a.Products),"Uma arma por "+options.IntervalSeconds+"s ativos / $"+options.UnitPrice+" de receita suja. Historico "+a.Sales+" vendas / $"+a.Revenue+". Requer sua filiacao/posse atual e jogo seguro/livre. Sem catch-up offline.",null);
                add("Desligar auto / "+a.Gang+" / area "+a.Area,"Desliga todas as categorias nesta area e zera tempo em memoria. Disponivel mesmo apos perder posse/filiacao; conserva estoque/historico.",()=>{if(Wallet.GangAutoArmsStop(a.Gang,a.Area,Guid.NewGuid().ToString("N"),out _)){Reset(a.Gang,a.Area);reopen();}else Notify.Show("Desligamento nao confirmado; dados conservados.");});
            }
        }
        public void Append(GangTurfDefinition area,Action<string,string,Action> add,Action reopen)
        {
            string gang=state().Gang,currentProfile=Profile.Id;var a=GangAutoArmsRules.Account(Wallet.GangAutoArms,gang,area.Id);
            add("Auto armas: "+(a?.Sales??0)+" vendas / $"+(a?.Revenue??0),"Configuracao por categoria/perfil/gangue/area. Receita fixa $"+options.UnitPrice+" por uma arma a cada "+options.IntervalSeconds+"s ativos; municao excluida. Inicialmente desligada.",null);
            foreach(var product in arms.Products.Where(p=>GangAutoArmsRules.Product(p.Id)))
            {
                var p=product;bool on=a?.Products.Contains(p.Id)==true;
                add("Auto "+p.Name+": "+(on?"LIGADA":"DESLIGADA"),"Selecionar "+(on?"desliga":"ativa")+" esta categoria. Receita automatica $"+options.UnitPrice+"/un; compra atual $"+p.Buy+"/un; entrega presencial $"+p.Sell+"/un. Ativar exige a pe na area, seguro e sem atividade/contratos. Desligar conserva estoque.",()=>{
                    if(currentProfile!=Profile.Id || !GangCrewRules.SameGang(gang,state().Gang)){Notify.Show("Perfil/filiacao mudou; reabra o menu.");return;}
                    var actor=Game.Player.Character;
                    if(!on && (gate.Owner!=null || !Wallet.GangAutoArmsFree || !SafeWorld || Game.IsPaused || InterfaceOptions.ModalOpen || !actor.IsOnFoot || actor.IsRagdoll || actor.Position.DistanceTo(new Vector3(area.Center.X,area.Center.Y,area.Center.Z))>warehouse.Radius)){Notify.Show("Ative a pe e em seguranca na area, sem atividade/contratos.");return;}
                    if(Wallet.GangAutoArmsConfigure(state(),area,arms,p.Id,!on,Guid.NewGuid().ToString("N"),out _)){Reset(gang,area.Id);Notify.Show(on?"Categoria automatica desligada. Estoque conservado.":"Categoria automatica ativada. Intervalo inicia do zero; receita por arma $"+options.UnitPrice+".");reopen();}else Notify.Show("Configuracao nao confirmada; dados conservados.");
                });
            }
        }
        bool SafeWorld=>Game.Player.Character!=null && Game.Player.Character.Exists() && Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.Player.Character.IsInCombat && Game.Player.Wanted.WantedLevel==0 && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        public void Tick()
        {
            var actor=Game.Player.Character;if(profile!=Profile.Id || actor==null || player!=actor.Handle){Clear();profile=Profile.Id;player=actor?.Handle??0;}
            if(!SafeWorld || gate.Owner!=null || !Wallet.GangAutoArmsFree || Wallet.GangAutoArms?.Valid!=true || Wallet.GangArms?.Valid!=true || Wallet.GangIllicit!=null && !Wallet.GangIllicit.Valid || !gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang))){clocks.Clear();return;}
            bool working=!Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;var live=new HashSet<string>();
            foreach(var area in areas().Where(a=>GangBusinessTransactions.Owns(state(),a)))
            {
                string key=Key(state().Gang,area.Id);var available=Wallet.GangAutoArmsAvailable(state().Gang,area.Id,arms);if(available.Count==0 || faulted.Contains(key)){clocks.Remove(key);continue;}live.Add(key);
                if(!clocks.TryGetValue(key,out var clock)){clocks[key]=new GangAutoArmsClock(Game.GameTime);continue;}
                if(!clock.Step(Game.GameTime,working,options.IntervalSeconds))continue;string product=available[random.Next(available.Count)];
                try{if(Wallet.GangAutoArmsSale(state(),area,arms,options,product,clock.Receipt,out bool changed) && changed)Notify.Show("Auto armas / "+area.Name+": 1x "+product+" vendido por $"+options.UnitPrice+" de receita suja.");clocks.Remove(key);}
                catch(Exception ex){clocks.Remove(key);faulted.Add(key);Log.Error("automatic arms sale",ex);Notify.Show("Venda automatica suspensa nesta area. Confira log/dados e desligue/reative; saldos anteriores conservados.");}
            }
            foreach(var key in clocks.Keys.Where(k=>!live.Contains(k)).ToList())clocks.Remove(key);
        }
        public void Clear(){clocks.Clear();faulted.Clear();profile=null;player=0;}
    }
}
