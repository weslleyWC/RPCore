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
    internal sealed class GangPromotion
    {
        const string Owner="gang_promotion",Anim="mp_common",Clip="givetake1_a";
        readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;
        readonly Func<IEnumerable<GangDefinition>> gangs;readonly Func<IEnumerable<GangTurfDefinition>> areas;readonly GangPromotionOptions options;
        readonly HashSet<int> used=new HashSet<int>(),gangModels=new HashSet<int>();
        Ped target,animOwner;Blip route;GangPromotionClock hand;bool animated;string profile,id,customer;int player,scanAt,requestedAt;public bool Active{get;private set;}
        GangPromotionAttempt Pending=>Wallet.GangPromotion?.Pending;
        GangTurfDefinition Current=>Pending==null?null:areas().FirstOrDefault(a=>a.Id==1);
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        bool Safe=>Game.Player.Character!=null && Game.Player.Character.Exists() && Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && Game.Player.Wanted.WantedLevel==0 && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool Input=>Safe && Game.Player.Character.IsOnFoot && !Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
        bool Owns=>Pending!=null && GangBusinessTransactions.Owns(state(),Current) && GangCrewRules.SameGang(state().Gang,Pending.Gang) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang));
        public GangPromotion(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs,Func<IEnumerable<GangTurfDefinition>> areas)
        {
            this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;this.areas=areas;
            options=Store.LoadDef("Gangs/Promotion.json",()=>new GangPromotionOptions());if(options?.Valid!=true){options=new GangPromotionOptions();Log.Write("Invalid tattoo promotion config; defaults in memory, original retained.");}
        }
        public void Controls(Action<string,string,Action> add,Action reopen)
        {
            var p=Pending;if(p==null)return;
            add("Divulgacao pendente / "+p.Customers.Count+"/"+p.Options.Goal,"Loja de tatuagem / "+p.Gang+" / area1. Sem custo ou recompensa parcial.",null);
            add(Active?"Suspender divulgacao":"Retomar divulgacao",p.Ready?"Cinco entregas prontas: volte a pe na loja e confirme E. Nao refaz panfletos.":"Retome a pe na loja; refaz os panfletos para garantir pedestres diferentes nesta sessao. Snapshot e contrato conservados.",()=>{if(Active){Suspend();reopen();}else if(!Resume())Notify.Show("Retome a pe na loja de tatuagem, com posse e seguranca, sem outro contrato/atividade.");});
            add("GPS: divulgacao pendente","Rota para a loja da tentativa salva.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,p.Options.Shop.X,p.Options.Shop.Y);pool.HideAll();});
            add("Cancelar divulgacao","Sem cobranca/estorno/bonus. Pode cancelar sem filiacão ou posse, em seguranca. Bonus anterior conserva validade.",()=>{if(!Safe || Game.IsPaused || InterfaceOptions.ModalOpen || gate.Owner!=null && gate.Owner!=Owner || !Wallet.GangPromotionCancel(p.Id,out _)){Notify.Show("Cancelamento nao confirmado; tentativa conservada.");return;}Suspend();reopen();});
        }
        public void Append(GangTurfDefinition area,Action<string,string,Action> add)
        {
            if(area.Id!=1)return;var a=GangPromotionTransactions.Account(Wallet.GangPromotion,state().Gang);var utc=DateTime.UtcNow;
            string status=GangPromotionTransactions.Multiplier(Wallet.GangPromotion,state().Gang,1,utc)==2?"ATIVO / "+Math.Ceiling((a.ExpiresUtcTicks-utc.Ticks)/(double)TimeSpan.TicksPerMinute)+"min":"inativo";
            add("Tatuagem / bonus2X: "+status,"Divulgacoes concluidas: "+(a?.Completed??0)+". Acelera somente processamento automatico da area1, taxa85% igual. Validade real/offline; nao liga automatico.",null);
            add("GPS: loja de tatuagem","Va a pe a ate"+options.ReturnRadius+"m da loja para iniciar.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,options.Shop.X,options.Shop.Y);pool.HideAll();});
            add("Divulgar loja / "+options.Goal+" panfletos","Gratis. Entregue a pedestres civis diferentes entre"+options.MinDistance+" e"+options.SearchRadius+"m da loja. E a ate"+options.InteractionDistance+"m por"+(options.GiveMilliseconds/1000.0)+"s. Volte e E confirma2X por"+options.BoostMinutes+"min reais.",()=>{if(!Begin(area))Notify.Show("Exige posse da area1, a pe na loja, seguro e sem atividade/contrato pendente.");});
        }
        bool At(JobPoint shop,int radius)=>Safe && !Game.IsPaused && !InterfaceOptions.ModalOpen && Game.Player.Character.IsOnFoot && Game.Player.Character.Position.DistanceTo(Pos(shop))<=radius;
        bool Begin(GangTurfDefinition area)
        {
            if(Active || gate.Owner!=null || !At(options.Shop,options.ReturnRadius) || !gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang)) || !gate.Acquire(Owner))return false;
            try{if(!Wallet.GangPromotionBegin(state(),area,options,Guid.NewGuid().ToString("N"),out bool changed) || !changed){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}
        }
        bool Resume()
        {
            if(Active || gate.Owner!=null || Wallet.GangPromotion?.Valid!=true || !Owns || !Wallet.GangPromotionOtherFree || !At(Pending.Options.Shop,Pending.Options.ReturnRadius) || !gate.Acquire(Owner))return false;
            try{if(!Pending.Ready && !Wallet.GangPromotionRestart(Pending.Id,out _)){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}
        }
        void Activate()
        {
            Active=true;profile=Profile.Id;player=Game.Player.Character.Handle;id=Pending.Id;used.Clear();gangModels.Clear();
            foreach(var g in gangs())foreach(var name in g.PedModels??Array.Empty<string>())gangModels.Add(new Model(name).Hash);
            scanAt=Game.GameTime;pool.HideAll();Route(Pos(Pending.Options.Shop),Pending.Ready?"Voltar a loja de tatuagem":"Procurar pedestres na vizinhanca");
            Notify.Show("Divulgacao ativa: siga o cliente marcado. E entrega panfleto; END suspende. Sem clientes, caminhe pela vizinhanca.");
        }
        bool Eligible(Ped p,bool selecting)
        {
            if(p==null || !p.Exists() || p.Handle==player || Pending==null)return false;
            var c=new GangPromotionCustomer{Human=p.IsHuman,Alive=p.IsAlive,OnFoot=p.IsOnFoot,Combat=p.IsInCombat,Ragdoll=p.IsRagdoll,Ambient=!p.IsPersistent && !Function.Call<bool>(Hash.IS_ENTITY_A_MISSION_ENTITY,p),Grouped=Function.Call<bool>(Hash.IS_PED_IN_GROUP,p),KnownGang=gangModels.Contains(p.Model.Hash),Used=used.Contains(p.Handle),PedType=(int)p.PedType,ShopDistance=p.Position.DistanceTo(Pos(Pending.Options.Shop)),PlayerDistance=p.Position.DistanceTo(Game.Player.Character.Position)};
            return GangPromotionTargetRules.Eligible(c,Pending.Options,selecting);
        }
        void Route(Vector3 point,string title)
        {if(route!=null && route.Exists())route.Delete();route=MapBlips.Create(point,"RPCore: "+title,BlipSprite.Standard,BlipColor.Yellow,.8f);route.ShowRoute=true;}
        void StopHand()
        {if(animated && animOwner!=null && animOwner.Exists())Function.Call(Hash.STOP_ANIM_TASK,animOwner,Anim,Clip,-4f);animated=false;animOwner=null;hand=null;}
        void ClearTarget()
        {StopHand();target=null;customer=null;if(route!=null && route.Exists())route.Delete();route=null;}
        public void Tick()
        {
            if(!Active)return;
            if(Wallet.GangPromotion?.Valid!=true || Pending==null || Pending.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Safe || !Owns || !Wallet.GangPromotionOtherFree || Game.Player.Character.Position.DistanceTo(Pos(Pending.Options.Shop))>Pending.Options.SearchRadius+100)
            {Suspend();Notify.Show("Divulgacao suspensa. Contrato conservado; F4 retoma ou cancela.");return;}
            if(!Input){StopHand();return;}
            var p=Pending;GTA.UI.Screen.ShowSubtitle("Divulgacao / "+p.Customers.Count+"/"+p.Options.Goal+" panfletos / END suspende",100);
            if(p.Ready)
            {if(route==null || !route.Exists())Route(Pos(p.Options.Shop),"Voltar a loja de tatuagem");World.DrawMarker(MarkerType.Cylinder,Pos(p.Options.Shop)-new Vector3(0,0,1),Vector3.Zero,Vector3.Zero,new Vector3(1,1,.4f),Color.FromArgb(140,60,200,110));if(At(p.Options.Shop,p.Options.ReturnRadius))GTA.UI.Screen.ShowHelpTextThisFrame("E: concluir divulgacao e liberar bonus2X");return;}
            if(target!=null && !Eligible(target,false)){ClearTarget();scanAt=Game.GameTime+1500;}
            if(target==null && unchecked(Game.GameTime-scanAt)>=0)
            {
                scanAt=Game.GameTime+3000;target=World.GetNearbyPeds(Pos(p.Options.Shop),p.Options.SearchRadius).Where(a=>Eligible(a,true)).OrderBy(a=>a.Position.DistanceTo(Game.Player.Character.Position)).FirstOrDefault();
                if(target!=null){customer=Guid.NewGuid().ToString("N");Route(target.Position,"Cliente para panfleto");}
            }
            if(target==null){GTA.UI.Screen.ShowHelpTextThisFrame("Procure civis a "+p.Options.MinDistance+"-"+p.Options.SearchRadius+"m da loja. Aguardando pedestre disponivel.");return;}
            if(route!=null && route.Exists())route.Position=target.Position;
            World.DrawMarker(MarkerType.Chevron1,target.Position+new Vector3(0,0,1.3f),Vector3.Zero,Vector3.Zero,new Vector3(.4f,.4f,.4f),Color.Yellow);
            float distance=target.Position.DistanceTo(Game.Player.Character.Position);
            if(hand==null){if(distance<=p.Options.InteractionDistance)GTA.UI.Screen.ShowHelpTextThisFrame("E: entregar panfleto da loja de tatuagem");return;}
            if(distance>p.Options.InteractionDistance){StopHand();return;}
            if(!animated)
            {
                if(unchecked((uint)(Game.GameTime-requestedAt))>10000){StopHand();Notify.Show("Animacao indisponivel; tente E novamente. Nenhum panfleto confirmado.");return;}
                if(!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED,Anim))return;
                animOwner=Game.Player.Character;Function.Call(Hash.TASK_PLAY_ANIM,animOwner,Anim,Clip,4f,-4f,p.Options.GiveMilliseconds,48,0f,false,false,false);animated=true;hand=new GangPromotionClock(Game.GameTime);
            }
            if(!hand.Step(Game.GameTime,true,p.Options.GiveMilliseconds))return;
            if(Wallet.GangPromotionGive(state(),Current,p.Id,customer,out bool changed) && changed)
            {used.Add(target.Handle);ClearTarget();scanAt=Game.GameTime+2500;Notify.Show("Panfleto confirmado / "+Pending.Customers.Count+"/"+Pending.Options.Goal);if(Pending.Ready){Route(Pos(Pending.Options.Shop),"Voltar a loja de tatuagem");Notify.Show("Todos os panfletos entregues. Volte a loja e confirme E.");}}
            else{Suspend();Notify.Show("Entrega nao confirmada. Contrato conservado; retome pelo F4.");}
        }
        public void Interact()
        {
            if(!Active || !Input || !Owns || !Wallet.GangPromotionOtherFree || Pending==null || Pending.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner)return;
            var p=Pending;
            if(p.Ready){if(At(p.Options.Shop,p.Options.ReturnRadius) && Wallet.GangPromotionComplete(state(),Current,p.Id,DateTime.UtcNow,out bool changed)){Suspend();Notify.Show(changed?"Divulgacao concluida! Processamento automatico2X na area1 por"+p.Options.BoostMinutes+"min reais. Ative o automatico separadamente.":"Divulgacao ja confirmada; bonus nao repetido.");}return;}
            if(hand!=null || !Eligible(target,false) || target.Position.DistanceTo(Game.Player.Character.Position)>p.Options.InteractionDistance)return;
            Function.Call(Hash.REQUEST_ANIM_DICT,Anim);requestedAt=Game.GameTime;hand=new GangPromotionClock(Game.GameTime);
        }
        public void Suspend()
        {ClearTarget();used.Clear();gangModels.Clear();Active=false;profile=id=null;player=0;gate.Release(Owner);}
    }
}
