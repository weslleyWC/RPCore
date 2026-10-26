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
    internal sealed class GangTheatre
    {
        const string Owner="gang_theatre",Decor="RPCore_TheatreOwner",Anim="anim@heists@box_carry@",Clip="idle",Ipl="redCarpet";
        readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Func<IEnumerable<GangDefinition>> gangs;readonly Func<IEnumerable<GangTurfDefinition>> areas;readonly GangTheatreOptions options;readonly Random random=new Random();
        readonly Dictionary<string,Model> models=new Dictionary<string,Model>(StringComparer.OrdinalIgnoreCase);readonly Dictionary<int,Prop> installed=new Dictionary<int,Prop>();readonly HashSet<int> borrowed=new HashSet<int>();readonly GangTheatreIplLease scene=new GangTheatreIplLease();
        Prop crate,carry;Ped animOwner;Blip route;GangPromotionClock hand;bool animated,render,installing;string profile,id;int player,token,requestAt,last;long elapsed,inputAt;public bool Active{get;private set;}
        GangTheatreAttempt Pending=>Wallet.GangTheatre?.Pending;GangTurfDefinition Current=>areas().FirstOrDefault(a=>a.Id==9);
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        static bool Tagged(Entity e,int value)=>e!=null && e.Exists() && e.Handle!=Game.Player.Character.Handle && Function.Call<bool>(Hash.DECOR_EXIST_ON,e,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,e,Decor)==value;
        bool Mark(Entity e){try{if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);return Function.Call<bool>(Hash.DECOR_SET_INT,e,Decor,token);}catch{return false;}}
        bool Safe=>Game.Player.Character!=null && Game.Player.Character.Exists() && Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && Game.Player.Wanted.WantedLevel==0 && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool Input=>Safe && !Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
        bool Owns=>Pending!=null && GangBusinessTransactions.Owns(state(),Current) && GangCrewRules.SameGang(Pending.Gang,state().Gang) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang));
        bool Near(JobPoint p,float radius)=>Safe && !Game.IsPaused && !InterfaceOptions.ModalOpen && Game.Player.Character.IsOnFoot && Game.Player.Character.Position.DistanceTo(Pos(p))<=radius;
        JobPoint Target=>Pending.Ready?Pending.Options.Crate:Pending.Carrying?Pending.Options.Items[Pending.Installed].Point:Pending.Options.Crate;
        bool AtTarget=>GangTheatreRules.Interact(Input,Game.Player.Character.IsOnFoot,Game.Player.Character.Position.DistanceTo(Pos(Target)),Pending.Options);
        public GangTheatre(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs,Func<IEnumerable<GangTurfDefinition>> areas)
        {this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;this.areas=areas;options=Store.LoadDef("Gangs/Theatre.json",()=>new GangTheatreOptions());if(options?.Valid!=true){options=new GangTheatreOptions();Log.Write("Invalid theatre options; defaults in memory, original retained.");}}
        public void Controls(Action<string,string,Action> add,Action reopen)
        {
            var p=Pending;if(p==null)return;
            add("Estreia pendente / "+p.Installed+"/"+p.Options.Items.Count,(p.Carrying?"Equipamento coletado salvo; retome a pe para reconstruir a caixa na mao.":"Instalacoes concluidas salvas.")+" Mesmo contrato e opcoes.",null);
            add(Active?"Suspender montagem da estreia":"Retomar montagem da estreia",p.Ready?"A pe na caixa inicial, retome e E confirma bonus sem refazer instalacoes.":"A pe a ate115m da caixa inicial, com posse/seguranca. Reconstroi pecas instaladas e caixa coletada; nao repete entregas.",()=>{if(Active){Suspend();reopen();}else if(!Resume())Notify.Show("Confira local a pe, posse, seguranca, atividade e pendencias.");});
            add("GPS: estreia pendente","Proximo ponto salvo do contrato.",()=>{var q=p.Ready?p.Options.Crate:p.Carrying?p.Options.Items[p.Installed].Point:p.Options.Crate;Function.Call(Hash.SET_NEW_WAYPOINT,q.X,q.Y);pool.HideAll();});
            add("Cancelar contrato da estreia","Gratis, sem premio parcial; conserva bonus anterior. Pode cancelar sem posse/filiacao em seguranca.",()=>{if(!Safe || Game.IsPaused || InterfaceOptions.ModalOpen || gate.Owner!=null && gate.Owner!=Owner || !Wallet.GangTheatreCancel(p.Id,out _)){Notify.Show("Cancelamento nao confirmado; contrato conservado.");return;}Suspend();reopen();});
        }
        public void Append(GangTurfDefinition area,Action<string,string,Action> add)
        {
            if(area.Id!=9)return;var a=GangTheatreTransactions.Account(Wallet.GangTheatre,state().Gang);var utc=DateTime.UtcNow;string status=GangTheatreTransactions.Multiplier(Wallet.GangTheatre,state().Gang,9,utc)==2?"ATIVO / "+Math.Ceiling((a.ExpiresUtcTicks-utc.Ticks)/(double)TimeSpan.TicksPerMinute)+"min":"inativo";
            add("Estreia / bonus2X: "+status,"Montagens concluidas: "+(a?.Completed??0)+". Acelera processamento na area9, sem pagamento pessoal ou automatico ligado sozinho.",null);
            add("GPS: equipamentos Oriental Theatre","A pe a ate"+options.StartRadius+"m da caixa para iniciar.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,options.Crate.X,options.Crate.Y);pool.HideAll();});
            add("Montar estreia / "+options.Items.Count+" pecas","E coleta caixa, leve a pe ao ponto e E instala; repita para cada peca. E final confirma2X/"+options.BoostMinutes+"min e tapete vermelho por10min de jogo. END suspende.",()=>{if(!Begin(area))Notify.Show("Exige posse da area9, caixa a pe em seguranca e nenhuma atividade/pendencia.");});
        }
        bool Begin(GangTurfDefinition area)
        {if(Active || gate.Owner!=null || !Near(options.Crate,options.StartRadius) || !gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang)) || !gate.Acquire(Owner))return false;try{if(!Wallet.GangTheatreBegin(state(),area,options,Guid.NewGuid().ToString("N"),out bool saved) || !saved){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}}
        bool Resume(){if(Active || gate.Owner!=null || Wallet.GangTheatre?.Valid!=true || !Owns || !Wallet.GangTheatreOtherFree || !Near(Pending.Options.Crate,Pending.Ready?Pending.Options.InteractionDistance:115) || !gate.Acquire(Owner))return false;Activate();return true;}
        void Activate(){Active=true;profile=Profile.Id;id=Pending.Id;player=Game.Player.Character.Handle;token=random.Next(1,int.MaxValue);last=requestAt=Game.GameTime;elapsed=0;inputAt=750;render=Visuals.Enabled && Visuals.UseProps;pool.HideAll();if(!Pending.Ready){Function.Call(Hash.REQUEST_ANIM_DICT,Anim);if(render)foreach(string name in Pending.Options.Items.Select(i=>i.Model).Concat(new[]{Pending.Options.CrateModel}).Distinct(StringComparer.OrdinalIgnoreCase)){var model=new Model(name);models[name]=model;if(!model.IsInCdImage){Suspend();Notify.Show("Modelo salvo indisponivel; contrato conservado.");return;}model.Request();}}Route();Notify.Show(Pending.Ready?"Montagem salva. E na caixa inicial confirma estreia e bonus.":"Montagem ativa. E coleta/instala cada equipamento; mantenha-se a pe carregando. END suspende.");}
        void Route(){if(route!=null && route.Exists())route.Delete();route=MapBlips.Create(Pos(Target),"RPCore: "+(Pending.Ready?"confirmar estreia":Pending.Carrying?"instalar equipamento":"coletar equipamento"),BlipSprite.Standard,BlipColor.Green,.8f);route.ShowRoute=true;}
        bool Loaded()
        {if((!render || models.Values.All(m=>m.IsLoaded)) && Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED,Anim))return true;if(unchecked((uint)(Game.GameTime-requestAt))>10000){Suspend();Notify.Show("Modelos/animacao nao carregaram; contrato conservado.");}return false;}
        Prop Create(string name,JobPoint point,bool ground)
        {var prop=World.CreateProp(models[name],Pos(point),false,ground);if(prop==null || !prop.Exists())return null;if(!Mark(prop)){prop.MarkAsNoLongerNeeded();return null;}prop.Heading=point.Heading;prop.IsPersistent=true;prop.IsPositionFrozen=true;prop.IsCollisionEnabled=false;return prop;}
        bool Equipment(int index)
        {
            if(!render || borrowed.Contains(index))return true;
            if(installed.TryGetValue(index,out var old))return Tagged(old,token);
            var item=Pending.Options.Items[index];var pos=Pos(item.Point);int hash=models[item.Model].Hash;
            if(World.GetNearbyProps(pos,2).Any(p=>p!=null && p.Exists() && p.Model.Hash==hash && !Tagged(p,token) && p.Position.DistanceTo2D(pos)<.4f)){borrowed.Add(index);return true;}
            var prop=Create(item.Model,item.Point,true);if(prop==null)return false;installed[index]=prop;return true;
        }
        bool Carry()
        {
            if(!render || Tagged(carry,token))return true;
            carry=Create(Pending.Options.CrateModel,new JobPoint{Name="Caixa na mao",X=Game.Player.Character.Position.X,Y=Game.Player.Character.Position.Y,Z=Game.Player.Character.Position.Z},false);
            if(carry==null)return false;carry.IsPositionFrozen=false;
            Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY,carry,Game.Player.Character,Function.Call<int>(Hash.GET_PED_BONE_INDEX,Game.Player.Character,57005),.12f,.04f,-.03f,0f,90f,0f,false,false,false,false,2,true);return true;
        }
        void ClearCarry(){if(Tagged(carry,token))carry.Delete();carry=null;}
        void StopAnim(){if(animated && animOwner!=null && animOwner.Exists())Function.Call(Hash.STOP_ANIM_TASK,animOwner,Anim,Clip,1f);animated=false;animOwner=null;}
        void Animate(){if(animated && animOwner!=null && animOwner.Exists() && Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM,animOwner,Anim,Clip,3))return;animOwner=Game.Player.Character;Function.Call(Hash.TASK_PLAY_ANIM,animOwner,Anim,Clip,4f,-4f,-1,49,0f,false,false,false);animated=true;}
        bool Restore()
        {
            if(!render)return true;
            if(crate==null){crate=Create(Pending.Options.CrateModel,Pending.Options.Crate,false);if(crate==null)return false;}else if(!Tagged(crate,token))return false;
            for(int i=0;i<Pending.Installed;i++)if(!Equipment(i))return false;
            if(Pending.Carrying && !Carry())return false;return true;
        }
        void StopHand(){hand=null;if(Pending?.Carrying!=true)StopAnim();}
        void TickHand()
        {
            if(!AtTarget || Pending.Ready || Pending.Carrying!=installing){StopHand();return;}
            Animate();if(!hand.Step(Game.GameTime,true,Pending.Options.HandoverMilliseconds))return;var p=Pending;int index=p.Installed;
            if(installing)
            {
                if(!Equipment(index) || !Wallet.GangTheatreInstall(state(),Current,id,index,out bool saved) || !saved){Suspend();Notify.Show("Instalacao nao confirmada; coleta e progresso anteriores conservados.");return;}
                ClearCarry();StopAnim();Notify.Show("Instalacao salva / "+Pending.Installed+"/"+p.Options.Items.Count+".");
            }
            else
            {
                if(!Carry() || !Wallet.GangTheatreCollect(state(),Current,id,index,out bool saved) || !saved){Suspend();Notify.Show("Coleta nao confirmada; etapa anterior conservada.");return;}
                Notify.Show("Equipamento coletado. Leve a pe ate o ponto marcado e E instala.");
            }
            hand=null;inputAt=elapsed+600;Route();
            if(Pending.Ready)Notify.Show("Todas as pecas instaladas! E aqui ou na caixa inicial confirma estreia e bonus.");
        }
        public void Tick()
        {
            if(scene.Tick(Game.GameTime,!Game.IsPaused))Function.Call(Hash.REMOVE_IPL,Ipl);
            if(!Active)return;
            if(Wallet.GangTheatre?.Valid!=true || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Safe || !Owns || !Wallet.GangTheatreOtherFree || Game.Player.Character.Position.DistanceTo(Pos(Pending.Options.Crate))>115){Suspend();Notify.Show("Montagem suspensa. Coletas e instalacoes salvas; F4 retoma ou cancela.");return;}
            uint d=unchecked((uint)(Game.GameTime-last));last=Game.GameTime;
            if(!Input){hand=null;StopAnim();return;}if(d<=2000)elapsed+=d;var p=Pending;
            GTA.UI.Screen.ShowSubtitle("Oriental Theatre / "+p.Installed+"/"+p.Options.Items.Count+" instalados / END suspende",100);
            if(p.Ready){if(CanFinish)GTA.UI.Screen.ShowHelpTextThisFrame("E: confirmar estreia e bonus2X");return;}
            if(!Loaded())return;
            if(p.Carrying && !Game.Player.Character.IsOnFoot){Suspend();Notify.Show("Carregamento exige ir a pe. Coleta salva; retome a pe perto do teatro.");return;}
            if(p.Carrying && render && carry!=null && (!Tagged(carry,token) || !Function.Call<bool>(Hash.IS_ENTITY_ATTACHED_TO_ENTITY,carry,Game.Player.Character))){Suspend();Notify.Show("Caixa propria perdida; coleta salva para retomar.");return;}
            if(!Restore()){Suspend();Notify.Show("Equipamentos proprios indisponiveis; progresso salvo para retomar.");return;}
            if(p.Carrying)Animate();
            World.DrawMarker(MarkerType.Cylinder,Pos(Target),Vector3.Zero,Vector3.Zero,new Vector3(.75f,.75f,.35f),Color.FromArgb(175,85,220,120));
            if(hand!=null){TickHand();return;}
            if(AtTarget)GTA.UI.Screen.ShowHelpTextThisFrame("E: "+(p.Carrying?"instalar ":"coletar ")+p.Options.Items[p.Installed].Name+" / permaneça perto durante a troca");
        }
        bool CanFinish=>Pending.Ready && (Near(Pending.Options.Crate,Pending.Options.InteractionDistance) || Near(Pending.Options.Items.Last().Point,Pending.Options.InteractionDistance));
        public void Interact()
        {
            if(!Active || !Input || !Owns || !Wallet.GangTheatreOtherFree || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || elapsed<inputAt || hand!=null)return;
            if(Pending.Ready)
            {
                if(!CanFinish)return;var c=Pending.Options;if(!Wallet.GangTheatreComplete(state(),Current,id,DateTime.UtcNow,out bool saved))return;
                Suspend();if(saved){if(scene.Celebrate(Function.Call<bool>(Hash.IS_IPL_ACTIVE,Ipl),Game.GameTime))Function.Call(Hash.REQUEST_IPL,Ipl);Notify.Show("Estreia pronta! Automatico2X na area9 por"+c.BoostMinutes+"min reais; ative separadamente. Tapete vermelho solicitado por10min de jogo.");}return;
            }
            if(!Loaded() || !AtTarget)return;installing=Pending.Carrying;hand=new GangPromotionClock(Game.GameTime);Animate();
        }
        public void Suspend()
        {
            hand=null;StopAnim();ClearCarry();if(Tagged(crate,token))crate.Delete();crate=null;
            foreach(var prop in installed.Values)if(Tagged(prop,token))prop.Delete();installed.Clear();borrowed.Clear();foreach(var m in models.Values)m.MarkAsNoLongerNeeded();models.Clear();
            if(route!=null && route.Exists())route.Delete();route=null;Active=false;profile=id=null;gate.Release(Owner);
        }
        public void Dispose(){Suspend();if(scene.Release())Function.Call(Hash.REMOVE_IPL,Ipl);}
    }
}
