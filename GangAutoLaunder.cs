using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
namespace RPCore
{
    internal sealed class GangAutoLaunder
    {
        readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Func<IEnumerable<GangDefinition>> gangs;readonly Func<IEnumerable<GangTurfDefinition>> areas;
        readonly GangAutoLaunderOptions options;readonly GangLaunderingOptions manual;
        readonly Dictionary<string,GangAutoArmsClock> clocks=new Dictionary<string,GangAutoArmsClock>();readonly HashSet<string> faulted=new HashSet<string>();string profile;int player;
        static string Key(string gang,int area)=>gang.ToUpperInvariant()+"|"+area;
        public GangAutoLaunder(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs,Func<IEnumerable<GangTurfDefinition>> areas)
        {
            this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;this.areas=areas;
            options=Store.LoadDef("Gangs/AutoLaunder.json",()=>new GangAutoLaunderOptions());if(options?.Valid!=true){options=new GangAutoLaunderOptions();Log.Write("Invalid automatic laundering config; defaults in memory, original retained.");}
            manual=Store.LoadDef("Gangs/Laundering.json",()=>new GangLaunderingOptions());if(manual?.Valid!=true)manual=new GangLaunderingOptions();
        }
        void Reset(string gang,int area){string key=Key(gang,area);clocks.Remove(key);faulted.Remove(key);}
        public void Controls(Action<string,string,Action> add,Action reopen)
        {
            if(Wallet.GangAutoLaunder!=null && !Wallet.GangAutoLaunder.Valid){add("Processamento automatico: dados protegidos","Confira Wallet/backup; nenhum saldo sera substituido.",null);return;}
            foreach(var entry in Wallet.GangAutoLaunder?.Accounts??new List<GangAutoLaunderAccount>())
            {
                if(!entry.Enabled)continue;var a=entry;
                add("Auto processar / "+a.Gang+" / area "+a.Area,"Ate $"+options.BatchAmount+" a cada "+options.IntervalSeconds+"s ativos / 85% ao caixa. Historico processado $"+a.Processed+", liquido $"+a.Net+", taxa $"+a.Fees+".",null);
                add("Desligar auto processar / "+a.Gang+" / area "+a.Area,"Desliga e zera tempo em memoria; conserva receita suja, caixa e historico. Disponivel mesmo sem posse/filiacao.",()=>{if(Wallet.GangAutoLaunderStop(a.Gang,a.Area,Guid.NewGuid().ToString("N"),out _)){Reset(a.Gang,a.Area);reopen();}else Notify.Show("Desligamento nao confirmado; dados conservados.");});
            }
        }
        public void Append(GangTurfDefinition area,Action<string,string,Action> add,Action reopen)
        {
            string gang=state().Gang,currentProfile=Profile.Id;var a=GangAutoLaunderTransactions.Account(Wallet.GangAutoLaunder,gang,area.Id);bool enabled=a?.Enabled==true;
            add("Processamento automatico: "+(enabled?"LIGADO":"DESLIGADO"),"Selecionar "+(enabled?"desliga":"ativa")+" nesta area. Ate $"+options.BatchAmount+" de receita suja por "+options.IntervalSeconds+"s ativos,85% ao caixa. Historico automatico: processado $"+(a?.Processed??0)+" / liquido $"+(a?.Net??0)+" / taxa $"+(a?.Fees??0)+". Ative a pe na area, seguro e sem atividade/contratos.",()=>{
                if(currentProfile!=Profile.Id || !GangCrewRules.SameGang(gang,state().Gang)){Notify.Show("Perfil/filiacao mudou; reabra o menu.");return;}
                var actor=Game.Player.Character;
                if(!enabled && (gate.Owner!=null || !Wallet.GangAutoArmsFree || !SafeWorld || Game.IsPaused || InterfaceOptions.ModalOpen || !actor.IsOnFoot || actor.IsRagdoll || actor.Position.DistanceTo(new Vector3(area.Center.X,area.Center.Y,area.Center.Z))>manual.Radius)){Notify.Show("Ative a pe e em seguranca na area, sem atividade/contratos.");return;}
                if(Wallet.GangAutoLaunderConfigure(state(),area,!enabled,Guid.NewGuid().ToString("N"),out _)){Reset(gang,area.Id);Notify.Show(enabled?"Processamento automatico desligado; saldos conservados.":"Processamento automatico ativado. Primeiro intervalo inicia do zero.");reopen();}else Notify.Show("Configuracao nao confirmada; dados conservados.");
            });
        }
        bool SafeWorld=>Game.Player.Character!=null && Game.Player.Character.Exists() && Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.Player.Character.IsInCombat && Game.Player.Wanted.WantedLevel==0 && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        public void Tick()
        {
            var actor=Game.Player.Character;if(profile!=Profile.Id || actor==null || player!=actor.Handle){Clear();profile=Profile.Id;player=actor?.Handle??0;}
            if(Wallet.GangCashier!=null && !Wallet.GangCashier.Valid || Wallet.GangPier!=null && !Wallet.GangPier.Valid || Wallet.GangOffshore!=null && !Wallet.GangOffshore.Valid || Wallet.GangBmx!=null && !Wallet.GangBmx.Valid || Wallet.GangDock!=null && !Wallet.GangDock.Valid || Wallet.GangChauffeur!=null && !Wallet.GangChauffeur.Valid || Wallet.GangTheatre!=null && !Wallet.GangTheatre.Valid || Wallet.GangMowing!=null && !Wallet.GangMowing.Valid || Wallet.GangPopUp!=null && !Wallet.GangPopUp.Valid || Wallet.GangMedical!=null && !Wallet.GangMedical.Valid || Wallet.GangCashRun!=null && !Wallet.GangCashRun.Valid || Wallet.GangTow!=null && !Wallet.GangTow.Valid || Wallet.GangMechanic!=null && !Wallet.GangMechanic.Valid || Wallet.GangPromotion!=null && !Wallet.GangPromotion.Valid || Wallet.GangCafe!=null && !Wallet.GangCafe.Valid || !SafeWorld || gate.Owner!=null || !Wallet.GangAutoArmsFree || Wallet.GangAutoLaunder?.Valid!=true || Wallet.GangIllicit?.Valid!=true || Wallet.GangTreasury!=null && !Wallet.GangTreasury.Valid || !gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang))){clocks.Clear();return;}
            bool working=!Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;var live=new HashSet<string>();
            foreach(var area in areas().Where(a=>GangBusinessTransactions.Owns(state(),a)))
            {
                string key=Key(state().Gang,area.Id);var a=GangAutoLaunderTransactions.Account(Wallet.GangAutoLaunder,state().Gang,area.Id);var income=GangIllicitTransactions.Account(Wallet.GangIllicit,state().Gang,area.Id);
                if(a?.Enabled!=true || income==null || income.Dirty<1 || faulted.Contains(key)){clocks.Remove(key);continue;}live.Add(key);
                if(!clocks.TryGetValue(key,out var clock)){clocks[key]=new GangAutoArmsClock(Game.GameTime);continue;}
                var utc=DateTime.UtcNow;
                double multiplier=Math.Max(GangPierTransactions.Multiplier(Wallet.GangPier,state().Gang,area.Id,utc),Math.Max(GangOffshoreTransactions.Multiplier(Wallet.GangOffshore,state().Gang,area.Id,utc),Math.Max(GangBmxTransactions.Multiplier(Wallet.GangBmx,state().Gang,area.Id,utc),Math.Max(GangDockTransactions.Multiplier(Wallet.GangDock,state().Gang,area.Id,utc),Math.Max(GangChauffeurTransactions.Multiplier(Wallet.GangChauffeur,state().Gang,area.Id,utc),Math.Max(GangTheatreTransactions.Multiplier(Wallet.GangTheatre,state().Gang,area.Id,utc),Math.Max(GangMowingTransactions.Multiplier(Wallet.GangMowing,state().Gang,area.Id,utc),Math.Max(GangPopUpTransactions.Multiplier(Wallet.GangPopUp,state().Gang,area.Id,utc),Math.Max(GangMedicalTransactions.Multiplier(Wallet.GangMedical,state().Gang,area.Id,utc),Math.Max(GangCashRunTransactions.Multiplier(Wallet.GangCashRun,state().Gang,area.Id,utc),Math.Max(GangTowTransactions.Multiplier(Wallet.GangTow,state().Gang,area.Id,utc),Math.Max(GangMechanicTransactions.Multiplier(Wallet.GangMechanic,state().Gang,area.Id,utc),Math.Max(GangCafeTransactions.Multiplier(Wallet.GangCafe,state().Gang,area.Id,utc),GangPromotionTransactions.Multiplier(Wallet.GangPromotion,state().Gang,area.Id,utc))))))))))))));
                if(!clock.Step(Game.GameTime,working,options.IntervalSeconds,multiplier))continue;
                try{long before=a.Net;if(Wallet.GangAutoLaunderProcess(state(),area,options,GangTreasury.Clock(),clock.Receipt,out bool changed) && changed){long credit=GangAutoLaunderTransactions.Account(Wallet.GangAutoLaunder,state().Gang,area.Id).Net-before;Notify.Show("Auto processar / "+area.Name+": $"+credit+" liquidos no caixa da gangue.");}clocks.Remove(key);}
                catch(Exception ex){clocks.Remove(key);faulted.Add(key);Log.Error("automatic territorial processing",ex);Notify.Show("Processamento automatico suspenso nesta area. Confira log/dados e desligue/reative; saldos anteriores conservados.");}
            }
            foreach(var key in clocks.Keys.Where(k=>!live.Contains(k)).ToList())clocks.Remove(key);
        }
        public void Clear(){clocks.Clear();faulted.Clear();profile=null;player=0;}
    }
}
