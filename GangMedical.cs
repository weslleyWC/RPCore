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
    internal sealed class GangMedical
    {
        const string Owner="gang_medical",Decor="RPCore_MedicalOwner",Anim="mp_common";
        readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Func<IEnumerable<GangDefinition>> gangs;readonly Func<IEnumerable<GangTurfDefinition>> areas;readonly GangMedicalOptions options;readonly Random random=new Random();
        Ped doctor,animOwner;Prop documents;Model doctorModel,documentModel;Blip route;GangPromotionClock hand;bool animated,delivery,render;string playerClip,profile,id;int player,token,requestAt,handAt,last;long elapsed,retry;public bool Active{get;private set;}
        GangMedicalAttempt Pending=>Wallet.GangMedical?.Pending;GangTurfDefinition Current=>areas().FirstOrDefault(a=>a.Id==6);
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        static bool Tagged(Entity e,int tag)=>e!=null && e.Exists() && e.Handle!=Game.Player.Character.Handle && Function.Call<bool>(Hash.DECOR_EXIST_ON,e,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,e,Decor)==tag;
        bool Mark(Entity e){try{if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);return Function.Call<bool>(Hash.DECOR_SET_INT,e,Decor,token);}catch{return false;}}
        bool Safe=>Game.Player.Character!=null && Game.Player.Character.Exists() && Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && Game.Player.Wanted.WantedLevel==0 && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool Input=>Safe && !Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
        bool Owns=>Pending!=null && GangBusinessTransactions.Owns(state(),Current) && GangCrewRules.SameGang(state().Gang,Pending.Gang) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang));
        bool Near(JobPoint p,int radius)=>Safe && !Game.IsPaused && !InterfaceOptions.ModalOpen && Game.Player.Character.IsOnFoot && Game.Player.Character.Position.DistanceTo(Pos(p))<=radius;
        public GangMedical(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs,Func<IEnumerable<GangTurfDefinition>> areas)
        {this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;this.areas=areas;options=Store.LoadDef("Gangs/MedicalCourier.json",()=>new GangMedicalOptions());if(options?.Valid!=true){options=new GangMedicalOptions();Log.Write("Invalid medical courier config; defaults in memory, original retained.");}}
        public void Controls(Action<string,string,Action> add,Action reopen)
        {
            var p=Pending;if(p==null)return;
            add("Documentos pendentes / "+(p.Delivered?"entregues":p.Collected?"coletados":"aguardando coleta"),"Documentos sao carga de missao, nao dinheiro/estoque pessoal. Progresso e opcoes salvos.",null);
            add(Active?"Suspender entrega medica":"Retomar entrega medica",p.Delivered?"A pe no centro medico, retome e E confirma bonus sem respawn.":p.Collected?"A pe em seguranca, retome de onde estiver. Recria so documento proprio; coleta salva permanece feita.":"A pe no ponto do medico com posse/seguranca. Mesmo contrato e opcoes.",()=>{if(Active){Suspend();reopen();}else if(!Resume())Notify.Show("Confira ponto/etapa a pe, posse, seguranca e atividade/contratos.");});
            add("GPS: documentos pendentes","Destino da proxima etapa salva.",()=>{var q=p.Collected?p.Options.Destination:p.Options.Doctor;Function.Call(Hash.SET_NEW_WAYPOINT,q.X,q.Y);pool.HideAll();});
            add("Cancelar entrega medica","Gratis/sem premio parcial, pode cancelar sem posse/filiacao em seguranca. Bonus anterior conserva validade.",()=>{if(!Safe || Game.IsPaused || InterfaceOptions.ModalOpen || gate.Owner!=null && gate.Owner!=Owner || !Wallet.GangMedicalCancel(p.Id,out _)){Notify.Show("Cancelamento nao confirmado; contrato conservado.");return;}Suspend();reopen();});
        }
        public void Append(GangTurfDefinition area,Action<string,string,Action> add)
        {
            if(area.Id!=6)return;var a=GangMedicalTransactions.Account(Wallet.GangMedical,state().Gang);var utc=DateTime.UtcNow;string status=GangMedicalTransactions.Multiplier(Wallet.GangMedical,state().Gang,6,utc)==2?"ATIVO / "+Math.Ceiling((a.ExpiresUtcTicks-utc.Ticks)/(double)TimeSpan.TicksPerMinute)+"min":"inativo";
            add("Documentos / bonus2X: "+status,"Entregas concluidas: "+(a?.Completed??0)+". Acelera automatico da area6,85% ao caixa igual. Validade real, nao ativa automatico.",null);
            add("GPS: medico / documentos","A pe a ate"+options.StartRadius+"m do medico para iniciar. Deixe livre o ponto do NPC.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,options.Doctor.X,options.Doctor.Y);pool.HideAll();});
            add("Entregar documentos medicos","Receba documentos com E do medico, viaje a pe ou em veiculo e entregue com E no centro medico. Troca de"+(options.HandoverMilliseconds/1000.0)+"s; E final libera2X/"+options.BoostMinutes+"min reais. Gratis.",()=>{if(!Begin(area))Notify.Show("Exige posse da area6, ponto do medico a pe/seguro e nenhuma atividade/pendencia.");});
        }
        bool Begin(GangTurfDefinition area)
        {if(Active || gate.Owner!=null || !Near(options.Doctor,options.StartRadius) || !gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang)) || !gate.Acquire(Owner))return false;try{if(!Wallet.GangMedicalBegin(state(),area,options,Guid.NewGuid().ToString("N"),out bool changed) || !changed){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}}
        bool Resume()
        {if(Active || gate.Owner!=null || Wallet.GangMedical?.Valid!=true || !Owns || !Wallet.GangMedicalOtherFree || !Safe || Game.IsPaused || InterfaceOptions.ModalOpen || !Game.Player.Character.IsOnFoot || (Pending.Delivered?!Near(Pending.Options.Destination,2):!Pending.Collected && !Near(Pending.Options.Doctor,Pending.Options.StartRadius)) || !gate.Acquire(Owner))return false;Activate();return true;}
        void Activate(){Active=true;profile=Profile.Id;id=Pending.Id;player=Game.Player.Character.Handle;token=random.Next(1,int.MaxValue);render=Visuals.Enabled && Visuals.UseProps;last=Game.GameTime;elapsed=retry=0;pool.HideAll();Route(Pos(Pending.Collected?Pending.Options.Destination:Pending.Options.Doctor));Notify.Show(Pending.Delivered?"Entrega salva. E no centro medico confirma bonus.":Pending.Collected?"Coleta salva. Documentos restaurados para continuar ate o centro medico.":"Entrega medica ativa: E com medico para coletar, END suspende.");}
        void Route(Vector3 p){if(route!=null && route.Exists())route.Delete();route=MapBlips.Create(p,"RPCore: documentos medicos",BlipSprite.Standard,BlipColor.Green,.8f);route.ShowRoute=true;}
        void Idle(){if(Tagged(doctor,token) && doctor.IsAlive && doctor.IsOnFoot)Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE,doctor,"WORLD_HUMAN_CLIPBOARD",0,true);}
        bool Prepare()
        {
            var c=Pending.Options;if(doctorModel==null){doctorModel=new Model(c.DoctorModel);documentModel=new Model(c.DocumentModel);if(!doctorModel.IsInCdImage || !doctorModel.IsPed || render && !documentModel.IsInCdImage){Suspend();Notify.Show("Modelo salvo indisponivel; contrato conservado.");return false;}doctorModel.Request();if(render)documentModel.Request();requestAt=Game.GameTime;}
            if(doctorModel.IsLoaded && (!render || documentModel.IsLoaded))return true;if(unchecked((uint)(Game.GameTime-requestAt))>10000){Suspend();Notify.Show("Modelos nao carregaram; contrato conservado.");}return false;
        }
        bool CreateDocuments()
        {
            if(!render || Tagged(documents,token))return true;documents=World.CreateProp(documentModel,Game.Player.Character.Position,false,false);if(documents==null || !documents.Exists()){documents=null;return false;}if(!Mark(documents)){documents.MarkAsNoLongerNeeded();documents=null;return false;}
            documents.IsPersistent=true;documents.IsCollisionEnabled=false;documents.AttachTo(Game.Player.Character.Bones[(Bone)28422],new Vector3(.1f,0,0),Vector3.Zero);return true;
        }
        void ClearDocuments(){if(Tagged(documents,token))documents.Delete();documents=null;}
        void ClearDoctor(){if(Tagged(doctor,token)){if(doctor.IsInVehicle())doctor.MarkAsNoLongerNeeded();else doctor.Delete();}doctor=null;}
        void StopHand(){if(animated){if(animOwner!=null && animOwner.Exists())Function.Call(Hash.STOP_ANIM_TASK,animOwner,Anim,playerClip,2f);if(!delivery && Tagged(doctor,token))Function.Call(Hash.STOP_ANIM_TASK,doctor,Anim,"givetake1_a",2f);Idle();}hand=null;animated=false;animOwner=null;}
        void StartHand(bool drop){delivery=drop;playerClip=drop?"givetake1_a":"givetake1_b";hand=new GangPromotionClock(Game.GameTime);handAt=Game.GameTime;Function.Call(Hash.REQUEST_ANIM_DICT,Anim);}
        void TickHand()
        {
            bool valid=delivery?GangMedicalRules.Interact(Input,Game.Player.Character.IsOnFoot,Pending.Collected && !Pending.Delivered,Game.Player.Character.Position.DistanceTo(Pos(Pending.Options.Destination))):Tagged(doctor,token) && doctor.IsAlive && doctor.IsOnFoot && GangMedicalRules.Interact(Input,Game.Player.Character.IsOnFoot,!Pending.Collected,Game.Player.Character.Position.DistanceTo(doctor.Position));
            if(!valid){StopHand();return;}
            if(!animated){if(!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED,Anim)){if(unchecked((uint)(Game.GameTime-handAt))>10000){StopHand();Notify.Show("Animacao indisponivel; nenhuma etapa confirmada.");}return;}animOwner=Game.Player.Character;if(!delivery){Function.Call(Hash.CLEAR_PED_TASKS,doctor);Function.Call(Hash.TASK_PLAY_ANIM,doctor,Anim,"givetake1_a",4f,-4f,Pending.Options.HandoverMilliseconds,48,0f,false,false,false);}Function.Call(Hash.TASK_PLAY_ANIM,animOwner,Anim,playerClip,4f,-4f,Pending.Options.HandoverMilliseconds,48,0f,false,false,false);animated=true;hand=new GangPromotionClock(Game.GameTime);}
            if(!hand.Step(Game.GameTime,true,Pending.Options.HandoverMilliseconds))return;bool drop=delivery;StopHand();
            if(drop){if(!Wallet.GangMedicalDeliver(state(),Current,id,out bool saved) || !saved){Suspend();Notify.Show("Entrega nao confirmada; coleta anterior conservada.");return;}ClearDocuments();Route(Pos(Pending.Options.Destination));Notify.Show("Documentos entregues e prova salva. E novamente confirma o bonus.");}
            else{if(!CreateDocuments()){Suspend();Notify.Show("Documento nao carregou; contrato conservado.");return;}if(!Wallet.GangMedicalCollect(state(),Current,id,out bool saved) || !saved){Suspend();Notify.Show("Coleta nao confirmada; contrato conservado.");return;}ClearDoctor();Route(Pos(Pending.Options.Destination));Notify.Show("Documentos coletados e salvos. Viaje ao centro medico; entrega a pe com E.");}
        }
        public void Tick()
        {
            if(!Active)return;if(Wallet.GangMedical?.Valid!=true || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Safe || !Owns || !Wallet.GangMedicalOtherFree){Suspend();Notify.Show("Entrega medica suspensa; progresso salvo no F4.");return;}
            uint delta=unchecked((uint)(Game.GameTime-last));last=Game.GameTime;if(!Input){StopHand();return;}if(delta<=2000)elapsed+=delta;var c=Pending.Options;
            GTA.UI.Screen.ShowSubtitle("Documentos / "+(Pending.Delivered?"entregues":Pending.Collected?"coletados":"aguardando medico")+" / END suspende",100);
            if(Pending.Delivered){if(Near(c.Destination,2))GTA.UI.Screen.ShowHelpTextThisFrame("E: concluir entrega medica e liberar bonus2X");return;}
            if(!Prepare())return;
            if(Pending.Collected)
            {
                if(render && documents==null && !CreateDocuments()){Suspend();Notify.Show("Documento nao pode ser restaurado; coleta salva conservada.");return;}
                if(render){if(!Tagged(documents,token) || !Function.Call<bool>(Hash.IS_ENTITY_ATTACHED_TO_ENTITY,documents,Game.Player.Character)){Suspend();Notify.Show("Prop de documento perdido; coleta salva. Retome a pe pelo F4.");return;}Function.Call(Hash.SET_ENTITY_VISIBLE,documents,Game.Player.Character.IsOnFoot,false);}
                World.DrawMarker(MarkerType.Cylinder,Pos(c.Destination)-new Vector3(0,0,.8f),Vector3.Zero,Vector3.Zero,new Vector3(1.5f,1.5f,.4f),Color.FromArgb(140,40,200,110));
            }
            else
            {
                if(!Tagged(doctor,token) && Game.Player.Character.Position.DistanceTo(Pos(c.Doctor))<70 && elapsed>=retry){retry=elapsed+3000;if(Game.Player.Character.Position.DistanceTo(Pos(c.Doctor))>=1.5f && World.GetNearbyPeds(Pos(c.Doctor),1.2f).All(p=>p.Handle==player) && World.GetNearbyVehicles(Pos(c.Doctor),1).Length==0){doctor=World.CreatePed(doctorModel,Pos(c.Doctor),c.Doctor.Heading);if(doctor!=null && doctor.Exists()){if(!Mark(doctor)){doctor.MarkAsNoLongerNeeded();doctor=null;Suspend();return;}doctor.IsPersistent=true;doctor.BlockPermanentEvents=true;doctor.Weapons.RemoveAll();doctor.RelationshipGroup=new RelationshipGroup((int)RelationshipGroupHash.CivilianMale);Function.Call(Hash.SET_PED_MONEY,doctor,0);Idle();}}}
                if(Tagged(doctor,token) && (!doctor.IsAlive || !doctor.IsOnFoot)){Suspend();Notify.Show("Medico indisponivel. Contrato conservado.");return;}
            }
            if(hand!=null){TickHand();return;}
            if(Pending.Collected)GTA.UI.Screen.ShowHelpTextThisFrame("Viaje ao centro medico. E a pe a ate2m do ponto: entregar documentos.");
            else GTA.UI.Screen.ShowHelpTextThisFrame(Tagged(doctor,token)?"E a pe a ate2m do medico: coletar documentos":"Aguardando medico: deixe o ponto livre e afaste-se2m.");
        }
        public void Interact()
        {
            if(!Active || !Input || !Owns || !Wallet.GangMedicalOtherFree || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Game.Player.Character.IsOnFoot || hand!=null)return;var c=Pending.Options;
            if(Pending.Delivered){if(Near(c.Destination,2) && Wallet.GangMedicalComplete(state(),Current,id,DateTime.UtcNow,out bool saved)){Suspend();Notify.Show(saved?"Entrega medica concluida! Automatico2X na area6 por"+c.BoostMinutes+"min reais. Ative processamento separadamente.":"Contrato ja confirmado; bonus nao repetido.");}return;}
            if(!Pending.Collected){if(Tagged(doctor,token) && doctor.IsAlive && doctor.IsOnFoot && Game.Player.Character.Position.DistanceTo(doctor.Position)<=2)StartHand(false);}
            else if(Near(c.Destination,2) && (!render || Tagged(documents,token)))StartHand(true);
        }
        public void Suspend(){StopHand();ClearDocuments();ClearDoctor();foreach(var m in new[]{doctorModel,documentModel})if(m!=null)m.MarkAsNoLongerNeeded();doctorModel=documentModel=null;if(route!=null && route.Exists())route.Delete();route=null;Active=false;profile=id=null;gate.Release(Owner);}
    }
}
