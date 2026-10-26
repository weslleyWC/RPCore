using System;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;

namespace RPCore
{
    public sealed class CustodySystem : ISystem
    {
        public string Name=>"custody";
        readonly ObjectPool pool;readonly ShiftGate gate;readonly Func<bool> gangFree;readonly NativeMenu menu;readonly CustodyOptions options;
        Ped actor;string profile,id;int stars,last,waiting,held,remaining,unsaved;bool preparing,active,surrender,eligibleBefore;Vector3 origin;
        CustodyOptions plan;
        static Vector3 Position(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        bool Identity=>profile==Profile.Id && actor!=null && actor.Exists() && actor==Game.Player.Character;
        bool Foot=>Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && Game.Player.Character.IsOnFoot && !Game.Player.Character.IsRagdoll && !Game.Player.Character.IsFalling;
        bool WorldFree=>!Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && !Game.IsPaused && !InterfaceOptions.ModalOpen;
        bool GangFree=>Wallet.GangWarEconomyFree && gangFree();
        bool Inside=>Identity && plan!=null && actor.Position.DistanceTo(Position(plan.Cell))<=plan.Radius && Function.Call<int>(Hash.GET_INTERIOR_FROM_ENTITY,actor)>0;
        bool OfficerSeen()=>World.GetNearbyPeds(Game.Player.Character,15).Any(p=>p!=Game.Player.Character && p.Exists() && p.IsAlive && p.IsOnFoot && Function.Call<int>(Hash.GET_PED_TYPE,p)==6 && p.HasClearLineOfSightTo(Game.Player.Character,IntersectFlags.Everything));
        bool CanSurrender()=>CustodyRules.Surrender(Game.Player.Character.IsHuman,Game.Player.Character.IsAlive,Game.Player.Character.IsOnFoot,Game.Player.Character.Weapons.Current.Hash==WeaponHash.Unarmed,Game.Player.Character.IsShooting || Function.Call<bool>(Hash.IS_PLAYER_FREE_AIMING,Game.Player),Game.Player.Character.IsRagdoll || Game.Player.Character.IsFalling,Game.IsMissionActive || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE),Game.Player.Wanted.WantedLevel,Game.Player.Character.Velocity.Length(),OfficerSeen());
        public CustodySystem(ObjectPool pool,ShiftGate gate,Func<bool> gangFree=null)
        {
            this.pool=pool;this.gate=gate;this.gangFree=gangFree??(()=>true);options=Store.LoadDef("Police/Custody.json",CustodyOptions.Defaults);if(options?.Valid!=true){options=CustodyOptions.Defaults();Log.Write("Invalid custody options retained; defaults used.");}
            menu=new NativeMenu("Custodia policial","RENDICAO / FICHAMENTO / PENA / FIANCA");pool.Add(menu);Visuals.Register(menu,"core");
        }
        void Add(string title,string description,Action action,bool enabled=true){var row=new NativeItem(title,description){Enabled=enabled};row.Activated+=(s,e)=>{try{action();}catch(Exception ex){Log.Error("custody action",ex);Notify.Show("Operacao nao gravada. Registro e saldo conservados; tente novamente.");}};menu.Add(row);}
        public void OpenMenu()
        {
            pool.HideAll();menu.Clear();string owner=Profile.Id;var b=Wallet.Custody;
            if(b!=null && !b.Valid){Add("Registro protegido","Wallet.json contem custodia invalida; nao sera substituida.",()=>{});menu.Visible=true;return;}
            Add("Custodia: "+(b?.Enabled==true?"LIGADA":"DESLIGADA"),"Opcional. Perseguicao e policia continuam nativas. Rendicao explicita perto de policial, pena ativa na cela e fianca pelo banco. Nao confisca armas/inventario.",()=>{if(owner!=Profile.Id)return;if(Wallet.CustodyConfigure(b?.Enabled!=true,out _))OpenMenu();else Notify.Show("Conclua a pena ou pague fianca antes de desligar.");});
            var c=b?.Pending;
            if(c==null)Add("Render-se a policial proximo","CONFIRMA transferencia para cela. Com 1 a 5 estrelas, pare a pe e desarmado perto de policial a pe (15m/visao). Permaneça assim 3s. Fichamento antes de limpar apenas a perseguicao entregue. Sem trabalho ativo.",()=>{if(owner==Profile.Id)Begin(true);},b?.Enabled==true && !preparing && !active);
            else{
                string caseId=c.Id;Add("Processo "+c.Id.Substring(0,Math.Min(8,c.Id.Length))+" / "+c.Stars+" estrela(s)","Restante salvo: "+Math.Ceiling(c.RemainingMilliseconds/1000d)+"s. Fianca $"+c.Bail+". Sem tempo offline. Fuga, morte, missao ou recarga suspendem; multas de transito continuam separadas.",()=>{});
                Add(active?"Custodia em andamento":"Retomar cumprimento da pena","CONFIRMA retorno a cela. Somente a pe, sem procurado/missao/atividade. Pena anterior e fianca mantidas; nao remove novas estrelas.",()=>{if(owner==Profile.Id && Wallet.Custody?.Pending?.Id==caseId)Begin(false);},!active && !preparing);
                Add("Concluir pena e sair","Somente com tempo cumprido, a pe na cela e sem novas estrelas. Saida verificada antes de gravar a liberacao.",()=>{if(owner==Profile.Id)Release(caseId,false);},c.RemainingMilliseconds==0);
                Add("Pagar fianca $"+c.Bail+" e sair","CONFIRMA debito exato pelo banco, uma vez. Somente na cela, sem novas estrelas. Insuficiencia conserva pena e saldo. Nao paga multas de transito.",()=>{if(owner==Profile.Id)Release(caseId,true);});
            }
            var lastRelease=b?.History.LastOrDefault();
            if(c==null && lastRelease!=null)Add("Recuperar saida ja liberada","Se houve falha/reload depois de gravar liberacao e voce ainda esta na cela, sai sem pagar outra vez. So funciona dentro da cela do ultimo processo, sem procurado ou atividade.",()=>{if(owner==Profile.Id)RecoverExit(lastRelease.Id);});
            if(active || preparing)Add("Suspender custodia (teste)","Grava tempo cumprido e solta reserva da atividade. Nao perdoa pena nem teleporta. Retome por este menu; END tambem suspende.",()=>{Suspend();OpenMenu();});
            Add("Fichamentos "+(b?.Booked??0)+" / penas "+(b?.Served??0)+" / fiancas "+(b?.Bailed??0),"Fiancas pagas $"+(b?.BailPaid??0)+". Historico dos ultimos 32 processos; totais preservados.",()=>{});
            foreach(var item in (b?.History??new System.Collections.Generic.List<CustodyCase>()).AsEnumerable().Reverse())Add(item.Stars+" estrela(s) / "+(item.Outcome=="served"?"PENA CUMPRIDA":"FIANCA PAGA"),item.Id+" / fianca definida $"+item.Bail,()=>{});
            Add("Fechar","Voltar ao jogo.",()=>menu.Visible=false);menu.Visible=true;
        }
        void Begin(bool arrest)
        {
            if(preparing || active || Wallet.Custody?.Valid!=true || !Wallet.Custody.Enabled || !WorldFree || !Foot || !GangFree || gate.Owner!=null || (arrest?Wallet.Custody.Pending!=null || !CanSurrender():Wallet.Custody.Pending==null || Game.Player.Wanted.WantedLevel!=0 || Game.Player.Character.IsInCombat)){Notify.Show("Confira sistema ligado, a pe, sem outra atividade e condicoes da rendicao/retomada.");return;}
            if(!gate.Acquire(Name))return;actor=Game.Player.Character;profile=Profile.Id;origin=actor.Position;stars=Game.Player.Wanted.WantedLevel;surrender=arrest;plan=arrest?options:Wallet.Custody.Pending.Options;id=arrest?Guid.NewGuid().ToString("N"):Wallet.Custody.Pending.Id;preparing=true;waiting=held=0;last=Game.GameTime;eligibleBefore=false;pool.HideAll();
            Notify.Show(arrest?"Rendicao solicitada. Pare desarmado perto do policial por 3s. END cancela antes do fichamento.":"Verificando acesso seguro a cela. END cancela.");
        }
        bool Ready(JobPoint point,bool cell,out Vector3 safe)
        {
            var p=Position(point);safe=p;Function.Call(Hash.REQUEST_COLLISION_AT_COORD,p.X,p.Y,p.Z);
            if(cell){int interior=Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS,p.X,p.Y,p.Z);if(interior==0 || !Function.Call<bool>(Hash.IS_INTERIOR_READY,interior))return false;}
            var hit=World.Raycast(p+new Vector3(0,0,.4f),p-new Vector3(0,0,1.5f),IntersectFlags.Map | IntersectFlags.Objects,actor);
            if(!hit.DidHit || hit.SurfaceNormal.Z<.7f || Math.Abs(hit.HitPosition.Z-(p.Z-1))>1.1f || !JobActors.Free(p,.7f))return false;
            safe=new Vector3(p.X,p.Y,hit.HitPosition.Z+1);return true;
        }
        void Checkpoint()
        {
            if(!active || !Identity || Wallet.Custody?.Pending?.Id!=id)return;
            if(!Wallet.CustodyCheckpoint(id,remaining,out _))throw new InvalidOperationException("Custody checkpoint rejected.");unsaved=0;
        }
        public void Tick()
        {
            if(!active && !preparing)return;
            try{TickActive();}catch(Exception ex){Log.Error("custody checkpoint/runtime",ex);Notify.Show("Custodia suspensa; ultimo checkpoint e saldo conservados.");Reset();}
        }
        void TickActive()
        {
            bool eligible=Identity && WorldFree && !pool.AreAnyVisible;int dt=CustodyRules.Credit(unchecked(Game.GameTime-last),eligible && eligibleBefore);last=Game.GameTime;eligibleBefore=eligible;
            if(!Identity || Wallet.Custody?.Valid!=true || !Foot || !GangFree || gate.Owner!=Name || Game.IsMissionActive || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE)){Suspend();return;}
            if(preparing){
                if(surrender?Game.Player.Wanted.WantedLevel!=stars || !CanSurrender():Game.Player.Wanted.WantedLevel!=0 || Wallet.Custody.Pending?.Id!=id){Suspend();Notify.Show("Transferencia cancelada; condicoes mudaram antes do fichamento.");return;}
                if(actor.Position.DistanceTo(origin)>2){Suspend();return;}
                if(!eligible){held=0;return;}waiting+=dt;held+=dt;
                if(waiting>10000){Suspend();Notify.Show("Cela indisponivel/ocupada. Confira interior Mission Row ou Police/Custody.json. Nenhuma estrela ou saldo alterado.");return;}
                if(held<3000 || !Ready(plan.Cell,true,out Vector3 cell))return;
                if(surrender && !Wallet.CustodyBook(id,stars,plan,out _)){Suspend();return;}
                remaining=Wallet.Custody.Pending.RemainingMilliseconds;active=true;preparing=false;unsaved=0;
                // This one-shot amnesty belongs only to the explicitly accepted, saved surrender.
                if(surrender)Function.Call(Hash.CLEAR_PLAYER_WANTED_LEVEL,Game.Player);
                actor.Position=cell;actor.Heading=plan.Cell.Heading;eligibleBefore=false;
                Notify.Show("Fichamento confirmado. Pena "+Math.Ceiling(remaining/1000d)+"s; F11 > Policia/transito > Custodia para fianca/liberacao. END suspende sem perdoar.");return;
            }
            if(Wallet.Custody.Pending?.Id!=id || Game.Player.Wanted.WantedLevel!=0 || !Inside || actor.IsInCombat){Suspend();Notify.Show("Custodia interrompida; restante salvo. Novas estrelas continuam nativas. Retome no F11.");return;}
            if(!eligible)return;remaining=Math.Max(0,remaining-dt);unsaved+=dt;if(unsaved>=5000 || remaining==0 && Wallet.Custody.Pending.RemainingMilliseconds>0)Checkpoint();
            GTA.UI.Screen.ShowSubtitle(remaining>0?"CUSTODIA: "+Math.Ceiling(remaining/1000d)+"s restantes. F11: fianca; END: suspender teste.":"PENA CUMPRIDA: F11 > Policia/transito > Custodia > Concluir pena e sair.",100);
        }
        void Release(string caseId,bool bail)
        {
            var c=Wallet.Custody?.Pending;if(c==null || c.Id!=caseId || Wallet.Custody.Valid!=true || !WorldFree || !Foot || Game.Player.Wanted.WantedLevel!=0 || Game.Player.Character.IsInCombat || !GangFree || gate.Owner!=null && gate.Owner!=Name){Notify.Show("Libere a pe na cela, sem procurado ou outra atividade.");return;}
            // Reload never transfers the player automatically; explicit release can recover an in-cell save.
            var p=Game.Player.Character;if(p.Position.DistanceTo(Position(c.Options.Cell))>c.Options.Radius || Function.Call<int>(Hash.GET_INTERIOR_FROM_ENTITY,p)==0){Notify.Show("Retome a custodia e use a liberacao dentro da cela.");return;}
            actor=p;profile=Profile.Id;plan=c.Options;
            if(!Ready(c.Options.Exit,false,out Vector3 exit)){Notify.Show("Saida indisponivel/ocupada. Nada cobrado; tente novamente.");return;}
            if(active)Checkpoint();
            if(!Wallet.CustodyRelease(caseId,bail,out bool changed)){Notify.Show(bail?"Saldo bancario insuficiente ou processo protegido; nada cobrado.":"Ainda ha tempo de pena a cumprir.");return;}
            if(changed){p.Position=exit;p.Heading=c.Options.Exit.Heading;}
            Reset();menu.Visible=false;Notify.Show(bail?"Fianca paga pelo banco. Liberacao concluida.":"Pena cumprida. Liberacao sem cobranca.");
        }
        void RecoverExit(string caseId)
        {
            var b=Wallet.Custody;var c=b?.History.LastOrDefault();
            if(b?.Valid!=true || b.Pending!=null || c?.Id!=caseId || !WorldFree || !Foot || !GangFree || gate.Owner!=null || Game.Player.Wanted.WantedLevel!=0 || Game.Player.Character.IsInCombat || Game.Player.Character.Position.DistanceTo(Position(c.Options.Cell))>c.Options.Radius || Function.Call<int>(Hash.GET_INTERIOR_FROM_ENTITY,Game.Player.Character)==0){Notify.Show("Recuperacao disponivel apenas dentro da cela, apos liberacao gravada.");return;}
            actor=Game.Player.Character;
            if(!Ready(c.Options.Exit,false,out Vector3 exit)){actor=null;Notify.Show("Saida indisponivel; nada cobrado.");return;}
            actor.Position=exit;actor.Heading=c.Options.Exit.Heading;actor=null;menu.Visible=false;Notify.Show("Saida recuperada. Sem nova cobranca ou alteracao de processo.");
        }
        void Reset(){gate.Release(Name);active=preparing=surrender=eligibleBefore=false;actor=null;profile=id=null;plan=null;unsaved=0;}
        void Suspend(){try{Checkpoint();}finally{Reset();}}
        public void OnKey(KeyEventArgs e){if(e.KeyCode==Keys.End && (active || preparing)){Suspend();Notify.Show("Custodia suspensa; pena pendente conservada no F11.");}}
        public void Reload(){menu.Visible=false;Reset();}
        public void Save(){Checkpoint();}
        public void Dispose(){menu.Visible=false;Suspend();}
    }
}
