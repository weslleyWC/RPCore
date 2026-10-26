using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
namespace RPCore
{
    internal sealed class GangPier
    {
        const string Owner="gang_pier",Decor="RPCore_PierOwner",Repair="mini@repair",RepairClip="fixing_a_ped",Water="amb@world_human_gardener_plant@male@base",WaterClip="base";
        readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Func<IEnumerable<GangDefinition>> gangs;readonly Func<IEnumerable<GangTurfDefinition>> areas;readonly GangPierOptions options;readonly Random random=new Random();
        Prop can;Model model;Ped animOwner;Blip route;GangPierClock clock;GangPierTask task;string profile,id,anim,clip;int player,token,last,requested,index,animAt;long elapsed,nextSave;bool render,loaded,begun;public bool Active{get;private set;}
        GangPierAttempt Pending=>Wallet.GangPier?.Pending;GangTurfDefinition Current=>areas().FirstOrDefault(a=>a.Id==14);
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        static bool Tagged(Entity e,int t)=>e!=null && e.Exists() && e.Handle!=Game.Player.Character.Handle && Function.Call<bool>(Hash.DECOR_EXIST_ON,e,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,e,Decor)==t;
        bool Mark(Entity e){try{if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);return Function.Call<bool>(Hash.DECOR_SET_INT,e,Decor,token);}catch{return false;}}
        bool Safe=>Game.Player.Character!=null && Game.Player.Character.Exists() && Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && Game.Player.Wanted.WantedLevel==0 && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool Input=>Safe && !Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
        bool Owns=>Pending!=null && GangBusinessTransactions.Owns(state(),Current) && GangCrewRules.SameGang(Pending.Gang,state().Gang) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang));
        bool Near(JobPoint p,float r)=>Safe && !Game.IsPaused && !InterfaceOptions.ModalOpen && Game.Player.Character.IsOnFoot && Game.Player.Character.Position.DistanceTo(Pos(p))<=r;
        JobPoint Target=>Pending.Options.Points[Pending.Ready?Pending.Options.Points.Count-1:Pending.Done];
        public GangPier(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs,Func<IEnumerable<GangTurfDefinition>> areas)
        {this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;this.areas=areas;options=Store.LoadDef("Gangs/Pier.json",()=>new GangPierOptions());if(options?.Valid!=true){options=new GangPierOptions();Log.Write("Invalid pier config; defaults in memory, original retained.");}}
        public void Controls(Action<string,string,Action> add,Action reopen)
        {
            var p=Pending;if(p==null)return;
            add("Manutencao do pier / "+(p.Ready?"concluida":p.Failed?"reprovada":"pendente"),"Tarefas "+p.Done+"/"+p.Options.Points.Count+" / restante "+Math.Max(0,p.Options.SecondsLimit-p.Progress.Elapsed/1000)+"s. Duas reposicoes, depois regas.",null);
            if(!p.Failed)add(Active?"Suspender manutencao do pier":"Retomar manutencao do pier",p.Ready?"A pe no ultimo ponto, retome e E confirma bonus sem repetir tarefas.":"A pe na area do pier, com posse/seguranca. Continua a proxima tarefa e conserva tempo gasto; somente acao ainda nao confirmada reinicia.",()=>{if(Active){Suspend();reopen();}else if(!Resume())Notify.Show("Confira area/ultimo ponto a pe, posse, seguranca e atividade/pendencias.");});
            add("GPS: pier pendente","Proxima tarefa salva ou confirmacao final.",()=>{var q=p.Options.Points[p.Ready?p.Options.Points.Count-1:p.Done];Function.Call(Hash.SET_NEW_WAYPOINT,q.X,q.Y);pool.HideAll();});
            add("Cancelar manutencao do pier","Gratis/sem premio parcial, pode sem posse/filiacao em seguranca. Reprovada exige cancelar; bonus anterior preservado.",()=>{if(!Safe || Game.IsPaused || InterfaceOptions.ModalOpen || gate.Owner!=null && gate.Owner!=Owner)return;Suspend();if(!Wallet.GangPierCancel(p.Id,out _))Notify.Show("Cancelamento nao confirmado; contrato conservado.");reopen();});
        }
        public void Append(GangTurfDefinition area,Action<string,string,Action> add)
        {
            if(area.Id!=14)return;var a=GangPierTransactions.Account(Wallet.GangPier,state().Gang);var utc=DateTime.UtcNow;int mult=GangPierTransactions.Multiplier(Wallet.GangPier,state().Gang,14,utc);
            add("Pier / bonus2X: "+(mult==2?"ATIVO / "+Math.Ceiling((a.ExpiresUtcTicks-utc.Ticks)/(double)TimeSpan.TicksPerMinute)+"min":"inativo"),"Manutencoes: "+(a?.Completed??0)+". Acelera processamento na area14, sem renda pessoal ou automatico ligado sozinho.",null);
            add("GPS: manutencao Del Perro","A pe a ate"+options.StartRadius+"m do primeiro ponto para iniciar.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,options.Points[0].X,options.Points[0].Y);pool.HideAll();});
            add("Manutencao do pier / "+options.Points.Count+" tarefas","E em cada ponto, permaneça a pe e perto por"+(options.TaskMilliseconds/1000.0)+"s. Duas reposicoes e demais regas, prazo"+options.SecondsLimit+"s ativos. E final libera2X/"+options.BoostMinutes+"min.",()=>{if(!Begin(area))Notify.Show("Confira posse da area14, primeiro ponto a pe, seguranca e atividade/pendencias.");});
        }
        bool Begin(GangTurfDefinition a)
        {if(Active || gate.Owner!=null || !Near(options.Points[0],options.StartRadius) || !gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang)) || !gate.Acquire(Owner))return false;try{if(!Wallet.GangPierBegin(state(),a,options,Guid.NewGuid().ToString("N"),out bool saved) || !saved){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}}
        bool Resume(){if(Active || gate.Owner!=null || Wallet.GangPier?.Valid!=true || Pending==null || Pending.Failed || !Owns || !Wallet.GangPierOtherFree || !(Pending.Ready?Near(Target,2):Near(Pending.Options.Points[0],250)) || !gate.Acquire(Owner))return false;Activate();return true;}
        void Activate(){Active=true;profile=Profile.Id;id=Pending.Id;player=Game.Player.Character.Handle;token=random.Next(1,int.MaxValue);last=requested=Game.GameTime;elapsed=0;loaded=false;clock=null;task=null;render=Visuals.Enabled && Visuals.UseProps;pool.HideAll();if(!Pending.Ready){Function.Call(Hash.REQUEST_ANIM_DICT,Repair);Function.Call(Hash.REQUEST_ANIM_DICT,Water);if(render){model=new Model(Pending.Options.WaterModel);if(!model.IsInCdImage){Suspend();Notify.Show("Regador salvo indisponivel; contrato conservado.");return;}model.Request();}}Route();Notify.Show(Pending.Ready?"Todas as tarefas salvas. E no ultimo ponto confirma bonus.":"Manutencao ativa. E em cada ponto e permaneça perto durante3s. END suspende; tempo nao reinicia.");}
        void Route(){if(route!=null && route.Exists())route.Delete();route=MapBlips.Create(Pos(Target),"RPCore: "+(Pending.Ready?"confirmar manutencao":Pending.Done<2?"repor materiais":"regar plantas"),BlipSprite.Standard,BlipColor.Green,.85f);route.ShowRoute=true;}
        bool Load()
        {if(Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED,Repair) && Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED,Water) && (!render || model.IsLoaded)){loaded=true;clock=new GangPierClock(Game.GameTime,Pending.Options.SecondsLimit*1000,Pending.Progress);nextSave=elapsed+1000;return true;}if(unchecked((uint)(Game.GameTime-requested))>10000){Suspend();Notify.Show("Animacoes/regador nao carregaram; contrato conservado.");}return false;}
        bool Flush(){if(clock==null || profile!=Profile.Id || Pending?.Id!=id || Pending.Ready || Wallet.GangPier?.Valid!=true)return true;var a=clock.Progress;var b=Pending.Progress;if(a.Elapsed==b.Elapsed && a.Faulted==b.Faulted)return true;return Wallet.GangPierUpdate(state(),Current,id,a,out _);}
        void StopTask(){task=null;begun=false;if(animOwner!=null && animOwner.Exists() && anim!=null)Function.Call(Hash.STOP_ANIM_TASK,animOwner,anim,clip,1f);animOwner=null;anim=clip=null;if(Tagged(can,token))can.Delete();can=null;}
        bool CanWork=>GangPierRules.Near(Input,Game.Player.Character.IsOnFoot,Game.Player.Character.Position.DistanceTo(Pos(Target)));
        public void Tick()
        {
            if(!Active)return;
            if(Wallet.GangPier?.Valid!=true || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Safe || !Owns || !Wallet.GangPierOtherFree || Game.Player.Character.Position.DistanceTo(Pos(Pending.Options.Points[0]))>250){Suspend();Notify.Show("Pier suspenso. Tarefas e tempo salvos; F4 retoma ou cancela.");return;}
            uint d=unchecked((uint)(Game.GameTime-last));last=Game.GameTime;if(!Input){clock?.Tick(Game.GameTime,false);StopTask();return;}if(d<=2000)elapsed+=d;var p=Pending;var c=p.Options;
            if(p.Ready){if(Near(Target,2))GTA.UI.Screen.ShowHelpTextThisFrame("E: confirmar manutencao e bonus2X");return;}
            if(!loaded && !Load())return;clock.Tick(Game.GameTime,true);if(elapsed>=nextSave){if(!Flush()){Suspend();return;}nextSave=elapsed+1000;}
            if(clock.Progress.Elapsed>=c.SecondsLimit*1000){clock.Progress.Faulted=true;Flush();Suspend();Notify.Show("Prazo da manutencao esgotado. Cancele pendencia no F4 para novo contrato; sem premio.");return;}
            GTA.UI.Screen.ShowSubtitle("Pier / "+p.Done+"/"+c.Points.Count+" tarefas / reposicoes "+Math.Min(p.Done,2)+"/2 / regas "+Math.Max(0,p.Done-2)+"/"+(c.Points.Count-2)+" / "+Math.Max(0,c.SecondsLimit-clock.Progress.Elapsed/1000)+"s / END suspende",100);
            World.DrawMarker(MarkerType.Cylinder,Pos(Target),Vector3.Zero,Vector3.Zero,new Vector3(1.5f,1.5f,.45f),Color.FromArgb(150,91,219,135));
            if(task==null){if(CanWork)GTA.UI.Screen.ShowHelpTextThisFrame(p.Done<2?"E: repor materiais / permaneça perto durante a acao.":"E: regar plantas / permaneça perto durante a acao.");return;}
            if(index!=p.Done || !CanWork){StopTask();return;}
            if(!Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM,Game.Player.Character,anim,clip,3)){if(!begun && unchecked((uint)(Game.GameTime-animAt))<1500)return;StopTask();return;}
            if(!begun){begun=true;task=new GangPierTask(Game.GameTime);return;}
            if(index>=2 && render && (!Tagged(can,token) || !Function.Call<bool>(Hash.IS_ENTITY_ATTACHED_TO_ENTITY,can,Game.Player.Character))){StopTask();Notify.Show("Regador indisponivel; nenhuma tarefa confirmada. E tenta novamente.");return;}
            if(!task.Step(Game.GameTime,true,c.TaskMilliseconds))return;
            int worked=task.Milliseconds;if(!Flush() || !Wallet.GangPierWork(state(),Current,id,index,true,true,Game.Player.Character.Position.DistanceTo(Pos(Target)),worked,out bool saved) || !saved){Suspend();Notify.Show("Tarefa nao confirmada; progresso anterior conservado.");return;}
            StopTask();Route();if(Pending.Ready){clock=null;Notify.Show("Todas as tarefas confirmadas! E aqui confirma bonus2X.");}
        }
        public void Interact()
        {
            if(!Active || !Input || !Owns || !Wallet.GangPierOtherFree || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner)return;
            if(Pending.Ready){int mins=Pending.Options.BoostMinutes;if(Near(Target,2) && Wallet.GangPierComplete(state(),Current,id,DateTime.UtcNow,out bool saved)){Suspend();Notify.Show(saved?"Manutencao concluida! Processamento2X na area14 por"+mins+"min reais. Ative separadamente.":"Contrato ja confirmado.");}return;}
            if(!loaded || task!=null || !CanWork)return;index=Pending.Done;anim=index<2?Repair:Water;clip=index<2?RepairClip:WaterClip;animOwner=Game.Player.Character;animOwner.Heading=Target.Heading;
            if(index>=2 && render){can=World.CreateProp(model,animOwner.Position,false,false);if(can==null || !can.Exists()){can=null;StopTask();return;}if(!Mark(can)){can.MarkAsNoLongerNeeded();can=null;StopTask();return;}can.IsPersistent=true;can.IsCollisionEnabled=false;Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY,can,animOwner,Function.Call<int>(Hash.GET_PED_BONE_INDEX,animOwner,57005),.1f,.02f,-.03f,-80f,0f,20f,false,false,false,false,2,true);}
            Function.Call(Hash.TASK_PLAY_ANIM,animOwner,anim,clip,4f,-4f,-1,49,0f,false,false,false);task=new GangPierTask(Game.GameTime);begun=false;animAt=Game.GameTime;
        }
        public void Suspend(){try{if(Active)Flush();}finally{clock=null;StopTask();if(model!=null)model.MarkAsNoLongerNeeded();model=null;if(route!=null && route.Exists())route.Delete();route=null;Active=false;profile=id=null;gate.Release(Owner);}}
        public void Dispose(){Suspend();}
    }
}
