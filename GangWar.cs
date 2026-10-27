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
    internal sealed class GangWar
    {
        readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Action<GangState> save;readonly Func<IEnumerable<GangDefinition>> gangs;readonly GangTerritories turf;
        readonly GangTurfOptions areas;readonly GangWarOptions options;readonly NativeMenu menu;readonly Random random=new Random();
        string profile,affiliation,alertId;int previous,attackElapsed,rivalElapsed,rivalNext,unsavedAlert;bool eligibleBefore;
        GangWarData Book=>state().Warfare;
        static Vector3 Pos(GangSpot p)=>new Vector3(p.X,p.Y,p.Z);
        public GangWar(ShiftGate gate,ObjectPool pool,Func<GangState> state,Action<GangState> save,Func<IEnumerable<GangDefinition>> gangs,GangTerritories turf)
        {
            this.gate=gate;this.pool=pool;this.state=state;this.save=save;this.gangs=gangs;this.turf=turf;
            areas=Store.LoadDef("Gangs/Territories.json",GangTurfOptions.Defaults);if(areas?.Valid!=true)areas=GangTurfOptions.Defaults();
            options=Store.LoadDef("Gangs/Warfare.json",()=>new GangWarOptions());if(options?.Valid!=true){options=new GangWarOptions();Log.Write("Invalid warfare options retained; defaults in memory.");}
            menu=new NativeMenu("Gangues","GUERRAS / ALERTAS / REFORCOS");pool.Add(menu);Visuals.Register(menu,"gangs");turf.OpenWarfare=Open;Reset();
        }
        void Add(string title,string description,Action action,bool enabled=true)
        {var row=new NativeItem(title,description){Enabled=enabled};row.Activated+=(s,e)=>{try{action();}catch(Exception ex){Reset();Log.Error("gang war menu",ex);Notify.Show("Acao nao concluida; donos e dados gravados conservados.");}};menu.Add(row);}
        void Toggle(string feature,bool enabled)
        {if(GangWarTransactions.Configure(state(),feature,enabled,save,out _)){Reset();Open();}}
        public void Open()
        {
            pool.HideAll();menu.Clear();
            if(Book!=null && !Book.Valid || state().Turf!=null && !state().Turf.Valid){Add("Dados de guerra protegidos","Confira GangState.json/backup; nao sera substituido.",()=>{});menu.Visible=true;return;}
            if(InterfaceOptions.AdvancedMenus)Add("Ataques aos seus territorios: "+(Book?.AttacksEnabled==true?"LIGADOS":"DESLIGADOS"),"OPCIONAL: rivais hostis enviam alerta. Ignorar por "+options.ResponseSeconds+" segundos ATIVOS transfere a area ao atacante. Menus, trabalhos, procurado e pausa suspendem o prazo. Desligar descarta o alerta sem perda.",()=>Toggle("attacks",Book?.AttacksEnabled!=true));
            if(InterfaceOptions.AdvancedMenus)Add("Disputas entre rivais: "+(Book?.RivalsEnabled==true?"LIGADAS":"DESLIGADAS"),"OPCIONAL: simula uma batalha distante entre duas gangues de NPCs a cada 5-15 minutos ativos por padrao. Nunca toma area da sua gangue. Atualiza mapa e historico.",()=>Toggle("rivals",Book?.RivalsEnabled!=true));
            if(InterfaceOptions.AdvancedMenus)Add("Reforcos nas novas capturas: "+(Book?.Reinforcements==true?"LIGADOS":"DESLIGADOS"),"Novas capturas: ate tres defensores iniciais e tres reforcos quando metade cai. Maximo seis. Tentativas antigas conservam regras salvas; reforcos chegam a pe.",()=>Toggle("reinforcements",Book?.Reinforcements!=true));
            var a=Book?.Alert;
            if(a!=null){
                Add("ALERTA: "+a.Area.Name+" / "+a.Attacker,"Restam "+((a.Limit-a.Elapsed+999)/1000)+"s ativos. Viaje ate a area, a pe, e responda neste menu. Sem transferencia antes do prazo.",()=>{});
                Add("GPS do ataque","Marca a area sob ameaca.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,a.Area.Center.X,a.Area.Center.Y);menu.Visible=false;});
                Add("Responder ao ataque","Na area, a pe e sem outra atividade. Inicia as ondas da defesa existente. Depois elimine todos e confirme E. END suspende a luta; retome em Territorios.",()=>{if(turf.RespondToAlert())menu.Visible=false;else Notify.Show("Va a area a pe; confira mapa ligado, gangue, modelos e atividade.");});
            }
            else Add("Solicitar alerta agora","Para testar sem esperar o intervalo: exige mapa e ataques ligados, territorio proprio, rival hostil e nenhuma atividade/pendencia. Cria o mesmo alerta real, com risco de perda se ignorado.",()=>{if(!Request())Notify.Show("Confira posse, mapa, ataques ligados, rival hostil e pendencias.");Open();},Book?.AttacksEnabled==true);
            Add("Historico: "+(Book?.RivalBattles??0)+" batalhas rivais / "+(Book?.Responded??0)+" respostas / "+(Book?.Unanswered??0)+" nao atendidos","Alertas nao atendidos ficam separados das derrotas em defesa efetivamente iniciada.",()=>{});
            foreach(var record in (Book?.History??new List<GangWarRecord>()).AsEnumerable().Reverse().Take(10))
            {var r=record;Add("Area "+r.Area+" / "+Label(r.Result),r.Attacker+" contra "+r.Defender,()=>{});}
            Add("Voltar aos territorios","Mapa, capturas e retomar disputas.",()=>turf.Open());menu.Visible=true;
        }
        static string Label(string result)=>result=="unanswered"?"alerta perdido":result=="responded"?"defesa iniciada":result=="dismissed"?"alerta descartado":result=="rival_capture"?"captura rival":"defesa rival";
        bool Free=>GangWarRules.Free(state()) && gate.Owner==null && !turf.Active && Wallet.GangWarEconomyFree;
        bool Ready=>state().TerritoriesEnabled && (Book==null || Book.Valid) && (Wallet.GangDiplomacy==null || Wallet.GangDiplomacy.Valid) && !GangLoyaltyRules.Unaffiliated(state().Gang) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang));
        bool Safe=>Game.Player.Character.IsHuman && !Game.Player.Character.IsDead && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && Game.Player.Wanted.WantedLevel==0 && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll;
        List<GangTurfDefinition> Owned()=>areas.Areas.Where(a=>GangCrewRules.SameGang(GangTurfRules.Owner(state().Turf,a),state().Gang)).ToList();
        List<GangDefinition> Hostiles()=>gangs().Where(g=>GangDiplomacyRules.Pair(state().Gang,g.Name) && GangWarRules.Hostile(GangDiplomacyRules.Attitude(Wallet.GangDiplomacy,state().Gang,g)) && (g.PedModels??Array.Empty<string>()).Any(n=>{if(!GangPresenceRules.ModelName(n))return false;var m=new Model(n);return m.IsInCdImage && m.IsPed;})).ToList();
        bool Request()
        {
            if(!Ready || !Safe || !Free || Book?.AttacksEnabled!=true || Book.Alert!=null)return false;
            var owned=Owned();var rivals=Hostiles();if(owned.Count==0 || rivals.Count==0)return false;
            var a=new GangWarAlert{Id=Guid.NewGuid().ToString("N"),Gang=state().Gang,Attacker=rivals[random.Next(rivals.Count)].Name,Area=owned[random.Next(owned.Count)],Limit=options.ResponseSeconds*1000};
            if(!GangWarTransactions.Alert(state(),a,save,out _))return false;attackElapsed=0;unsavedAlert=0;alertId=a.Id;
            Notify.Show("ATAQUE IMINENTE: "+a.Attacker+" / "+a.Area.Name+". F4 > Guerras: GPS e responder. "+options.ResponseSeconds+"s ativos; ignorar perde a area.");return true;
        }
        void RivalBattle()
        {
            if(Book?.RivalsEnabled!=true || Book.Alert!=null)return;
            var candidates=gangs().Where(g=>!GangCrewRules.SameGang(g.Name,state().Gang) && areas.Areas.Any(a=>GangCrewRules.SameGang(GangTurfRules.Owner(state().Turf,a),g.Name))).ToList();if(candidates.Count<2 || random.Next(100)>=85)return;
            var attacker=candidates[random.Next(candidates.Count)];candidates.Remove(attacker);var defender=candidates[random.Next(candidates.Count)];
            var targets=areas.Areas.Where(a=>GangCrewRules.SameGang(GangTurfRules.Owner(state().Turf,a),defender.Name)).ToList();var area=targets[random.Next(targets.Count)];
            if(Game.Player.Character.Position.DistanceTo(Pos(area.Center))<=options.RemoteRadius)return;
            int count=areas.Areas.Count(a=>GangCrewRules.SameGang(GangTurfRules.Owner(state().Turf,a),attacker.Name));bool won=random.Next(100)<GangWarRules.WinChance(count,targets.Count);
            if(GangWarTransactions.Rival(state(),area,attacker.Name,defender.Name,won,Guid.NewGuid().ToString("N"),save,out _))Notify.Show(won?attacker.Name+" tomou "+area.Name+" de "+defender.Name+".":defender.Name+" defendeu "+area.Name+" contra "+attacker.Name+".");
        }
        public void Tick()
        {
            if(profile!=Profile.Id || !GangCrewRules.SameGang(affiliation,state().Gang)){Reset();return;}
            bool eligible=Ready && Safe && Free && !Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
            int delta=GangWarRules.Delta(previous,Game.GameTime,eligible && eligibleBefore);previous=Game.GameTime;eligibleBefore=eligible;if(!eligible || delta==0)return;
            var a=Book?.Alert;
            if(a!=null){
                if(!GangCrewRules.SameGang(a.Gang,state().Gang) || !GangCrewRules.SameGang(GangTurfRules.Owner(state().Turf,a.Area),a.Gang))return;
                if(alertId!=a.Id){unsavedAlert=0;alertId=a.Id;}
                unsavedAlert+=delta;if(unsavedAlert>=1000 || (long)a.Elapsed+unsavedAlert>=a.Limit){int elapsed=(int)Math.Min(a.Limit,(long)a.Elapsed+unsavedAlert);if(GangWarTransactions.Advance(state(),a.Id,elapsed,save,out _)){unsavedAlert=0;if(Book.Alert==null)Notify.Show("Alerta nao atendido: "+a.Area.Name+" passou para "+a.Attacker+". Recapture pelo F4.");}}return;
            }
            unsavedAlert=0;alertId=null;
            if(Book?.AttacksEnabled==true){attackElapsed+=delta;if(attackElapsed>=GangWarRules.AttackSeconds(Owned().Count)*1000){attackElapsed=0;if(Request())return;}}
            if(Book?.RivalsEnabled==true){rivalElapsed+=delta;if(rivalElapsed>=rivalNext){rivalElapsed=0;rivalNext=random.Next(options.RivalMinSeconds,options.RivalMaxSeconds+1)*1000;RivalBattle();}}
        }
        public void Reset(){previous=Game.GameTime;eligibleBefore=false;profile=Profile.Id;affiliation=state().Gang;attackElapsed=rivalElapsed=unsavedAlert=0;alertId=null;rivalNext=random.Next(options.RivalMinSeconds,options.RivalMaxSeconds+1)*1000;}
        public void Close(){menu.Visible=false;Reset();}
    }
}
