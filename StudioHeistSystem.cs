using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
namespace RPCore
{
    public sealed class StudioHeistSystem : ISystem
    {
        sealed class WorldPending : Exception { public WorldPending(string message):base(message){} }
        public string Name=>"studio_heist";const string Decor="RPCore_StudioOwner";
        readonly ObjectPool pool;readonly ShiftGate gate;readonly Func<bool> gangFree;readonly NativeMenu menu;readonly StudioHeistOptions defaults;readonly Random random=new Random();
        readonly Dictionary<string,Model> models=new Dictionary<string,Model>();readonly List<Entity> owned=new List<Entity>();readonly List<Tuple<Entity,int>> retired=new List<Tuple<Entity,int>>();readonly List<Ped> guards=new List<Ped>();readonly HashSet<int> guardPoints=new HashSet<int>();
        StudioHeistFlow flow;StudioHeistOptions c;StudioLockpick lockpick;Ped actor,director,contact,buyer,gateGuard;Vehicle car,trailer,directorCar,boat,buyerBoat;Prop cargo;
        string profile,id,probeKey;int probeSince,token,last,elapsed,unsaved,modelAt,hold,taskAt,directorIndex,diversionTime;bool active,loading,pressed,eligibleBefore,alerted,transferring;Blip route;
        StudioObjective Objective=>flow.Objective;
        bool Identity=>profile==Profile.Id && actor!=null && actor.Exists() && actor==Game.Player.Character;
        bool Input=>!Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
        bool WorldFree=>Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool SafeStart=>WorldFree && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && !Game.IsPaused && !InterfaceOptions.ModalOpen && Game.Player.Wanted.WantedLevel==0 && gate.Owner==null && Wallet.GangWarEconomyFree && gangFree();
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        static bool Tagged(Entity e,int owner)=>e!=null && e.Exists() && e.Handle!=Game.Player.Character.Handle && Function.Call<bool>(Hash.DECOR_EXIST_ON,e,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,e,Decor)==owner;
        bool Own(Entity e)=>Tagged(e,token);
        void Track(Entity e){if(e==null || !e.Exists())throw new InvalidOperationException("Recurso da missao indisponivel.");if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);if(!Function.Call<bool>(Hash.DECOR_SET_INT,e,Decor,token)){e.MarkAsNoLongerNeeded();throw new InvalidOperationException("Nao foi possivel marcar recurso proprio.");}e.IsPersistent=true;owned.Add(e);if(e is Vehicle v)JobVehicles.Reserve(v);}
        bool Driver(Vehicle v)=>Own(v) && Identity && actor.CurrentVehicle==v && v.Driver==actor;
        bool NearFoot(Vector3 point,float radius=2.5f)=>StudioHeistRules.Foot(Identity,actor.IsOnFoot,actor.Velocity.Length(),actor.Position.DistanceTo(point),radius);
        bool NoPolice=>Game.Player.Wanted.WantedLevel==0;
        public StudioHeistSystem(ObjectPool pool,ShiftGate gate,Func<bool> gangFree=null)
        {
            this.pool=pool;this.gate=gate;this.gangFree=gangFree??(()=>true);defaults=Store.LoadDef("Missions/StudioHeist.json",StudioHeistOptions.Defaults);if(defaults?.Valid!=true){defaults=StudioHeistOptions.Defaults();Log.Write("Invalid studio config retained; defaults used.");}
            menu=new NativeMenu("Contratos / Lester","THE HOLLYWOOD ACT");pool.Add(menu);Visuals.Register(menu,"core");
        }
        static readonly string[] Acts={"1: Suprimentos / van","2: Documentos (21h-5h)","3: Carro de fuga","4: Barco / reboque","5: Distracao","6: Final / dispositivo e comprador"};
        void Add(string title,string description,Action action,bool enabled=true){var row=new NativeItem(title,description){Enabled=enabled};row.Activated+=(s,e)=>{try{action();}catch(Exception ex){Log.Error("studio menu",ex);Notify.Show("Operacao nao gravada; confira log e tente novamente. Progresso/saldo conservados.");}};menu.Add(row);}
        public void OpenMenu()
        {
            pool.HideAll();menu.Clear();string owner=Profile.Id;var b=Wallet.StudioHeist;
            if(b!=null && !b.Valid){Add("Progresso protegido","Registro invalido em Wallet.json; nao sera substituido.",()=>{});menu.Visible=true;return;}
            Add("The Hollywood Act","Cinco preparacoes e final. NPCs/veiculos proprios, risco de combate e policia nativa. Sem apagar estrelas ou desligar policia. Preparacoes nao pagam; final deposita recompensa200-450mil uma vez.",()=>{});
            for(int i=0;i<6;i++){int act=i;Add(((b?.Setups??0)>i?"CONCLUIDO / ":"")+Acts[i],"Inicia somente o proximo ato. Sem trabalho/missao/procurado ou outra atividade. Ato2 inicia de21h ate antes5h. END suspende; retomar reinicia este ato e mantem os anteriores.",()=>{if(owner==Profile.Id)Begin(act);},!active && b?.Pending==null && (b?.Setups??0)==i);}
            if(b?.Pending!=null){var pending=b.Pending;string caseId=pending.Id;
                Add(pending.Ready?"Receber / confirmar ato concluido":active?"Ato ativo / "+(flow==null?"carregando":Label(Objective)):"Retomar "+Acts[pending.Act],"Tempo ativo salvo "+pending.Elapsed/1000+"s de"+pending.Options.SecondsLimit+"s. Recompensa definida $"+pending.Reward+". Retomada nao pula etapas fisicas nem reinicia o prazo; apos entrega pronta pode confirmar sem recriar NPCs.",()=>{if(owner!=Profile.Id || Wallet.StudioHeist?.Pending?.Id!=caseId)return;if(pending.Ready)Collect(caseId);else if(!active)Begin(pending.Act);});
                Add("Cancelar este ato","CONFIRMA desistir do ato/recompensa pendente. Conserva preparacoes ja concluidas. Nao apaga policia nativa nem recursos do ambiente.",()=>{if(owner!=Profile.Id)return;if(active)Suspend();if(Wallet.StudioFinish(caseId,"cancelled",out _))OpenMenu();});
            }
            if(active && flow?.Act==5 && (Objective==StudioObjective.EscapeStudio || Objective==StudioObjective.LosePolice) && b?.Pending?.DiversionUsed==false)Add(b.Pending.DiversionRequested?"Distracao solicitada / aguarde":"Acionar distracao preparada","Confirma chamada de distracao. Apos20s ativos, apenas guardas deste contrato recuam para portaria. Nao apaga estrelas, nao altera dispatch nem explode objetos do mundo.",()=>{if(owner==Profile.Id && Identity && WorldFree && !Game.IsPaused && !InterfaceOptions.ModalOpen && Wallet.StudioDiversion(flow,false,out _)){pool.HideAll();Notify.Show("Distracao solicitada. Aguarde20s ativos; policia nativa continua independente.");}},!b.Pending.DiversionRequested);
            if(active)Add("Suspender ato","Salva prazo e conserva preparacoes. Recolhe somente recursos proprios; retomar reinicia as etapas deste ato.",()=>{Suspend();OpenMenu();});
            Add("Finais "+(b?.Heists??0)+" / recebidos $"+(b?.Earned??0),"Preparacoes "+(b?.Setups??0)+"/5; falhas "+(b?.Failed??0)+"; desistencias "+(b?.Cancelled??0)+". Sem renda offline.",()=>{});
            Add("Fechar","Voltar ao jogo.",()=>menu.Visible=false);menu.Visible=true;
        }
        void Begin(int act)
        {
            if(active || !SafeStart || Wallet.StudioHeist!=null && !Wallet.StudioHeist.Valid || act<0 || act>5){Notify.Show("Encerre atividade/procurado/missao antes de aceitar o contrato.");return;}
            var p=Wallet.StudioHeist?.Pending;
            if(p?.Ready==true || (p==null?(Wallet.StudioHeist?.Setups??0)!=act:p.Act!=act) || act==1 && !StudioHeistRules.Night(GTA.Chrono.GameClock.Hour)){Notify.Show("Confira ordem dos atos; documentos somente21h-5h. Entrega pronta e confirmada pelo menu.");return;}
            if(!gate.Acquire(Name))return;
            try{if(p==null){string newId=Guid.NewGuid().ToString("N");if(!Wallet.StudioBegin(newId,act,defaults,act==5?random.Next(defaults.MinReward,defaults.MaxReward+1):0,out _)){gate.Release(Name);return;}p=Wallet.StudioHeist.Pending;}
                profile=Profile.Id;actor=Game.Player.Character;id=p.Id;c=p.Options;elapsed=p.Elapsed;unsaved=diversionTime=0;token=random.Next(1,int.MaxValue);flow=new StudioHeistFlow(id,act);lockpick=new StudioLockpick();directorIndex=random.Next(c.Directors.Count);active=loading=true;modelAt=last=Game.GameTime;eligibleBefore=false;pool.HideAll();
                foreach(string name in RequiredModels(act).Distinct()){var m=new Model(name);if(!m.IsValid || !m.IsInCdImage)throw new InvalidOperationException("Modelo indisponivel: "+name);m.Request();models.Add(name,m);}SetRoute(Target());Notify.Show("Contrato aceito: "+Acts[act]+". Siga GPS; E interage, END suspende. Recursos aparecem ao se aproximar.");
            }catch{Suspend();throw;}
        }
        IEnumerable<string> RequiredModels(int act){yield return c.GuardModel;if(act==0){yield return c.VanModel;yield return "v_ind_cf_boxes";}if(act==1)yield return c.DocumentsModel;if(act==2)yield return c.GetawayModel;if(act==3){yield return c.TruckModel;yield return c.TrailerModel;yield return c.ContactModel;}if(act==4){yield return c.ContactModel;yield return "prop_carrier_bag_01";}if(act==5){foreach(var s in new[]{c.GetawayModel,c.DirectorModel,c.DirectorCarModel,c.EscapeBoatModel,c.BuyerBoatModel,c.BuyerModel})yield return s;}}
        bool Free(Vector3 point,float radius)=>!World.GetNearbyVehicles(point,radius).Any(v=>v.Exists() && Math.Abs(v.Position.Z-point.Z)<3) && !World.GetNearbyPeds(point,1.5f).Any(p=>p.Exists() && Math.Abs(p.Position.Z-point.Z)<3);
        Vector3 Ground(JobPoint point){var p=Pos(point);Function.Call(Hash.REQUEST_COLLISION_AT_COORD,p.X,p.Y,p.Z);float z;if(!World.GetGroundHeight(p+new Vector3(0,0,3),out z,GetGroundHeightMode.Normal) || Math.Abs(z-p.Z)>4)throw new WorldPending("Piso indisponivel: "+point.Name);return new Vector3(p.X,p.Y,z+.15f);}
        bool Car(ref Vehicle v,string model,JobPoint point,bool water=false)
        {
            if(v!=null){if(!Own(v) || v.IsDead)throw new InvalidOperationException("Veiculo do contrato perdido/destruido.");return true;}if(actor.Position.DistanceTo(Pos(point))>140)return false;
            Vector3 p;if(water){var h=new OutputArgument();if(!Function.Call<bool>(Hash.GET_WATER_HEIGHT,point.X,point.Y,point.Z+5,h))throw new WorldPending("Agua indisponivel no ponto do barco.");p=new Vector3(point.X,point.Y,h.GetResult<float>()+.2f);}else p=Ground(point);
            if(!Free(p,4))throw new WorldPending("Vaga do contrato ocupada; afaste-se do ponto e libere os veiculos. Nenhum ambiente removido.");
            v=World.CreateVehicle(models[model],p,point.Heading);Track(v);if(!water)Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY,v);v.IsEngineRunning=true;return true;
        }
        Ped Person(string model,JobPoint point,bool armed=false){var p=Ground(point)+new Vector3(0,0,1);if(!Free(p,.8f))throw new WorldPending("Ponto de NPC ocupado: "+point.Name);var ped=World.CreatePed(models[model],p,point.Heading);Track(ped);ped.BlockPermanentEvents=true;ped.RelationshipGroup=new RelationshipGroup((int)RelationshipGroupHash.CivilianMale);Function.Call(Hash.SET_PED_KEEP_TASK,ped,true);Function.Call(Hash.SET_PED_MONEY,ped,0);if(armed){ped.Weapons.Give(WeaponHash.Pistol,120,true,true);ped.Accuracy=35;ped.Armor=50;}return ped;}
        void SpawnGuards(List<JobPoint> points){for(int i=0;i<points.Count;i++){if(guardPoints.Contains(i) || actor.Position.DistanceTo(Pos(points[i]))>110)continue;var p=Person(c.GuardModel,points[i],true);guards.Add(p);guardPoints.Add(i);if(alerted)p.Task.Combat(actor);else p.Task.StandStill(-1);}}
        void Alert(){if(alerted)return;alerted=true;foreach(var p in guards)if(Own(p) && p.IsAlive)p.Task.Combat(actor);Notify.Show("Seguranca alertada! Somente guardas do contrato reagem; perca a policia antes da entrega.");}
        void Detect(){if(alerted)return;foreach(var p in guards){if(!Own(p) || p.IsDead)continue;Vector3 d=actor.Position-p.Position;if(d.Length()<30 && Vector3.Dot(p.ForwardVector,d.Normalized)>.707f && p.HasClearLineOfSightTo(actor,IntersectFlags.Everything)){Alert();break;}}}
        static JobPoint Offset(JobPoint p,Vector3 offset,string name)=>new JobPoint{Name=name,X=p.X+offset.X,Y=p.Y+offset.Y,Z=p.Z+offset.Z,Heading=p.Heading};
        JobPoint Target()
        {
            switch(Objective){case StudioObjective.TakeVan:return c.Van;case StudioObjective.DeliverVan:return c.Lockup;case StudioObjective.Lockpick:case StudioObjective.TakeDocuments:return c.Documents;case StudioObjective.DeliverDocuments:return c.DocumentsDrop;case StudioObjective.TakeGetaway:return c.Getaway;case StudioObjective.DeliverGetaway:return c.Garage;case StudioObjective.TakeKeys:case StudioObjective.TakeTruck:return c.Truck;case StudioObjective.DeliverTrailer:return c.BoatDrop;case StudioObjective.TakeBag:return c.BagContact;case StudioObjective.PlantBag:return c.Plant;case StudioObjective.TakePreparedCar:return c.PreparedCar;case StudioObjective.GatePass:return c.Gate;case StudioObjective.Park:return c.Parking;case StudioObjective.FindDirector:return c.Directors[directorIndex];case StudioObjective.FollowDirector:case StudioObjective.QuietTakedown:case StudioObjective.CollectKeys:return c.DirectorWalk;case StudioObjective.TakeDevice:case StudioObjective.EscapeStudio:return c.DirectorCar;case StudioObjective.LosePolice:case StudioObjective.BoardEscapeBoat:return c.EscapeBoat;default:return c.BuyerBoat;}
        }
        static string Label(StudioObjective s){switch(s){case StudioObjective.TakeVan:return "Roube a van marcada";case StudioObjective.DeliverVan:return "Entregue a van sem procurado";case StudioObjective.Lockpick:return "Abra o porta-malas: tres acertos";case StudioObjective.TakeDocuments:return "Pegue os documentos no porta-malas";case StudioObjective.DeliverDocuments:return "Entregue os documentos a pe";case StudioObjective.TakeGetaway:return "Roube o Locust marcado";case StudioObjective.DeliverGetaway:return "Leve o carro a oficina";case StudioObjective.TakeKeys:return "Obtenha a chave do dono na marina";case StudioObjective.TakeTruck:return "Dirija a Bison marcada";case StudioObjective.DeliverTrailer:return "Entregue Bison e reboque juntos";case StudioObjective.TakeBag:return "Recolha a bolsa do contato";case StudioObjective.PlantBag:return "Deixe a bolsa no local marcado";case StudioObjective.TakePreparedCar:return "Dirija o carro preparado";case StudioObjective.GatePass:return "Pare na portaria e apresente documentos";case StudioObjective.Park:return "Estacione e desembarque";case StudioObjective.FindDirector:return "Encontre o diretor marcado";case StudioObjective.FollowDirector:return "Siga o diretor: mantenha5-80m";case StudioObjective.QuietTakedown:return "Neutralize o diretor sem disparar";case StudioObjective.CollectKeys:return "Recolha a chave do diretor";case StudioObjective.TakeDevice:return "Vasculhe o porta-malas do diretor";case StudioObjective.EscapeStudio:return "Afaste-se400m do estudio";case StudioObjective.LosePolice:return "Despiste a policia; nenhuma estrela sera apagada";case StudioObjective.BoardEscapeBoat:return "Dirija o barco de fuga";case StudioObjective.DeliverDevice:return "Leve o dispositivo ao comprador no mar";default:return "Entrega concluida";}}
        void SetRoute(JobPoint p){if(route!=null && route.Exists())route.Delete();route=World.CreateBlip(Pos(p));route.Color=BlipColor.Yellow;route.Name=Label(Objective);route.ShowRoute=true;}
        void Advance(){var before=Objective;if(flow.Advance(before,true)){hold=0;transferring=false;taskAt=0;if(Objective!=StudioObjective.Ready)SetRoute(Target());Notify.Show(Label(Objective));}}
        bool Delivered(Vehicle v,JobPoint p)=>StudioHeistRules.Delivery(Driver(v),Own(v),NoPolice,v?.Speed??float.NaN,actor.Position.DistanceTo(Pos(p)));
        bool Attached(){if(!Own(car) || !Own(trailer))return false;var t=new OutputArgument();return Function.Call<bool>(Hash.GET_VEHICLE_TRAILER_VEHICLE,car,t) && t.GetResult<int>()==trailer.Handle;}
        void PrepTick(int dt,bool key)
        {
            switch(Objective){
            case StudioObjective.TakeVan:
                if(!Car(ref car,c.VanModel,c.Van))return;SpawnGuards(c.SupplyGuards);Detect();if(cargo==null){cargo=World.CreateProp(models["v_ind_cf_boxes"],car.Position,false,false);Track(cargo);Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY,cargo,car,Math.Max(0,car.Bones["chassis"].Index),0f,-1.45f,.33f,0f,0f,0f,false,false,false,false,2,true);}if(Driver(car))Advance();break;
            case StudioObjective.DeliverVan:if(!Own(car) || car.IsDead){Fail("Van perdida.");return;}if(key && Delivered(car,c.Lockup))Advance();break;
            case StudioObjective.Lockpick:
                if(!Car(ref car,c.DocumentsModel,c.Documents))return;car.LockStatus=VehicleLockStatus.CannotEnter;if(!NearFoot(car.GetOffsetPosition(new Vector3(0,-2.8f,0)))){hold=0;return;}
                if(hold==0){if(key){hold=1;Notify.Show("E quando a barra estiver entre80% e88%. Tres acertos, no maximo cinco erros.");}return;}
                int failures=lockpick.Failures;lockpick.Tick(dt,true);GTA.UI.Screen.ShowSubtitle("FECHADURA ["+new string('|',(int)(lockpick.Position*25))+">] "+(int)(lockpick.Position*100)+"% / alvo80-88 / "+lockpick.Solved+" de3 / erros"+lockpick.Failures,100);
                if(key && lockpick.Failures==failures)lockpick.Press();if(lockpick.Failed){Fail("Cinco erros na fechadura.");return;}if(lockpick.Complete){car.Doors[VehicleDoorIndex.Trunk].Open();Advance();}break;
            case StudioObjective.TakeDocuments:if(!Own(car) || car.IsDead){Fail("Veiculo dos documentos perdido.");return;}if(key && Own(car) && NearFoot(car.GetOffsetPosition(new Vector3(0,-2.8f,0)))){car.Doors[VehicleDoorIndex.Trunk].Close();Advance();}break;
            case StudioObjective.DeliverDocuments:if(key && NoPolice && NearFoot(Pos(c.DocumentsDrop)))Advance();break;
            case StudioObjective.TakeGetaway:if(!Car(ref car,c.GetawayModel,c.Getaway))return;if(guards.Count==0){var p=Person(c.GuardModel,Offset(c.Getaway,new Vector3(4,2,0),"Vigia"),true);guards.Add(p);}Detect();if(Driver(car))Advance();break;
            case StudioObjective.DeliverGetaway:if(!Own(car) || car.IsDead){Fail("Carro perdido.");return;}if(key && Delivered(car,c.Garage))Advance();break;
            case StudioObjective.TakeKeys:
                if(!Car(ref car,c.TruckModel,c.Truck))return;
                if(trailer==null){var q=car.GetOffsetPosition(new Vector3(0,-8.5f,0));var p=new JobPoint{Name="Reboque",X=q.X,Y=q.Y,Z=q.Z,Heading=car.Heading};if(!Car(ref trailer,c.TrailerModel,p))return;car.AttachToTrailer(trailer);}
                SpawnGuards(c.DockGuards);if(contact==null)contact=Person(c.ContactModel,Offset(c.Truck,new Vector3(1.5f,4,0),"Dono / chave"));
                if(key && Own(contact) && NearFoot(contact.Position,2) && (contact.IsDead || actor.Weapons.Current.Hash!=WeaponHash.Unarmed && Function.Call<bool>(Hash.IS_PLAYER_FREE_AIMING,Game.Player))){Alert();Advance();Notify.Show("Chave obtida. Entre na Bison; entrega exige reboque proprio engatado.");}break;
            case StudioObjective.TakeTruck:if(!Own(car) || car.IsDead || !Own(trailer) || trailer.IsDead){Fail("Bison/reboque perdidos.");return;}if(Driver(car))Advance();break;
            case StudioObjective.DeliverTrailer:
                if(!Own(car) || !Own(trailer) || car.IsDead || trailer.IsDead){Fail("Bison/reboque perdidos.");return;}
                if(!Attached()){GTA.UI.Screen.ShowSubtitle("Reengate o reboque proprio. E ao volante, parado, a menos10m dele tenta o engate.",100);if(key && Driver(car) && car.Speed<.5f && car.Position.DistanceTo(trailer.Position)<10)car.AttachToTrailer(trailer);return;}
                if(key && Delivered(car,c.BoatDrop) && trailer.Position.DistanceTo(Pos(c.BoatDrop))<=15)Advance();break;
            case StudioObjective.TakeBag:
                if(actor.Position.DistanceTo(Pos(c.BagContact))>120)return;if(contact==null)contact=Person(c.ContactModel,c.BagContact);
                if(key && Own(contact) && NearFoot(contact.Position,2)){cargo=World.CreateProp(models["prop_carrier_bag_01"],actor.Position,false,false);Track(cargo);cargo.IsCollisionEnabled=false;cargo.AttachTo(actor.Bones[Bone.SkelRightHand],new Vector3(.28f,-.1f,.03f),new Vector3(1.2556f,-90.802f,-34.4621f));Advance();}break;
            case StudioObjective.PlantBag:
                if(!Own(cargo)){Fail("Bolsa perdida.");return;}cargo.Opacity=actor.IsInVehicle()?0:255;
                if(key && NoPolice && NearFoot(Pos(c.Plant))){cargo.Detach();cargo.Position=Pos(c.Plant);Advance();}break;
            }
        }
        void FinaleTick(int dt,bool key)
        {
            var attempt=Wallet.StudioHeist.Pending;
            if((Objective==StudioObjective.EscapeStudio || Objective==StudioObjective.LosePolice) && attempt.DiversionRequested && !attempt.DiversionUsed){diversionTime+=dt;if(diversionTime>=20000 && Wallet.StudioDiversion(flow,true,out bool diverted) && diverted){foreach(var guard in guards)if(Own(guard) && guard.IsAlive){guard.Task.ClearAll();JobActors.Walk(guard,Pos(c.Gate));}Notify.Show("Distracao confirmada. Segurancas do contrato recuam para portaria; despiste a policia nativa normalmente.");}}

            if(Objective>=StudioObjective.GatePass && Objective<=StudioObjective.TakeDevice){if(!Own(car) || car.IsDead){Fail("Carro preparado perdido.");return;}SpawnGuards(c.StudioGuards);if(actor.IsShooting || !NoPolice){Fail("Alarme antes de obter o dispositivo.");return;}}
            switch(Objective){
            case StudioObjective.TakePreparedCar:if(Car(ref car,c.GetawayModel,c.PreparedCar) && Driver(car))Advance();break;
            case StudioObjective.GatePass:
                if(!Own(car) || car.IsDead){Fail("Carro preparado perdido.");return;}if(actor.Position.DistanceTo(Pos(c.Gate))>110)return;
                if(gateGuard==null)gateGuard=Person(c.GuardModel,Offset(c.Gate,new Vector3(1.8f,-5.3f,0),"Porteiro"));
                if(Delivered(car,c.Gate)){if(taskAt==0 || unchecked(Game.GameTime-taskAt)>=0){var q=car.GetOffsetPosition(new Vector3(-1.8f,0,0));Function.Call(Hash.TASK_FOLLOW_NAV_MESH_TO_COORD,gateGuard,q.X,q.Y,q.Z,1f,12000,.6f,0,actor.Heading);taskAt=Game.GameTime+4000;}if(key && gateGuard.Position.DistanceTo(actor.Position)<=3)Advance();}break;
            case StudioObjective.Park:
                if(!Own(car) || car.IsDead){Fail("Carro preparado perdido.");return;}
                if(car.Position.DistanceTo(Pos(c.Parking))<=5 && car.Speed<.5f && actor.IsOnFoot && actor.Position.DistanceTo(car.Position)<6)Advance();break;
            case StudioObjective.FindDirector:
                if(actor.Position.DistanceTo(Pos(c.Directors[directorIndex]))>120)return;if(director==null)director=Person(c.DirectorModel,c.Directors[directorIndex]);
                if(director.IsDead){Fail("Diretor perdido antes de segui-lo.");return;}if(actor.Position.DistanceTo(director.Position)<=15){JobActors.Walk(director,Pos(c.DirectorWalk));Advance();}break;
            case StudioObjective.FollowDirector:
                if(!Own(director) || director.IsDead){Fail("Diretor perdido.");return;}float d=actor.Position.DistanceTo(director.Position);if(d<=5 || d>80){Fail("Distancia do diretor incorreta.");return;}
                if(director.Position.DistanceTo(Pos(c.DirectorWalk))<=20)Advance();else if(taskAt==0 || unchecked(Game.GameTime-taskAt)>=0){JobActors.Walk(director,Pos(c.DirectorWalk));taskAt=Game.GameTime+15000;}break;
            case StudioObjective.QuietTakedown:
                if(!Own(director)){Fail("Diretor perdido.");return;}
                if(director.IsDead){if(!director.HasBeenDamagedBy(actor)){Fail("Diretor atingido por terceiros.");return;}Advance();}break;
            case StudioObjective.CollectKeys:
                if(!Own(director)){Fail("Corpo do diretor perdido.");return;}if(key && director.IsDead && NearFoot(director.Position,2))Advance();break;
            case StudioObjective.TakeDevice:
                if(!Car(ref directorCar,c.DirectorCarModel,c.DirectorCar))return;directorCar.LockStatus=VehicleLockStatus.CannotEnter;
                if(key && NearFoot(directorCar.GetOffsetPosition(new Vector3(0,-2.8f,0)))){directorCar.Doors[VehicleDoorIndex.Trunk].Open();Advance();Alert();int current=Game.Player.Wanted.WantedLevel;int maximum=Function.Call<int>(Hash.GET_MAX_WANTED_LEVEL);int target=Math.Min(3,maximum);if(current<target && target>0){Game.Player.Wanted.SetWantedLevel(target,false);Game.Player.Wanted.ApplyWantedLevelChangeNow(false);}Notify.Show("Dispositivo obtido. Fuja400m e despiste a policia; RPCore nao apaga estrelas.");}break;
            case StudioObjective.EscapeStudio:if(actor.Position.DistanceTo(Pos(c.DirectorCar))>=400)Advance();break;
            case StudioObjective.LosePolice:if(NoPolice)Advance();break;
            case StudioObjective.BoardEscapeBoat:if(Car(ref boat,c.EscapeBoatModel,c.EscapeBoat,true) && Driver(boat))Advance();break;
            case StudioObjective.DeliverDevice:
                if(!Own(boat) || boat.IsDead){Fail("Barco de fuga perdido.");return;}
                if(!Car(ref buyerBoat,c.BuyerBoatModel,c.BuyerBoat,true))return;
                if(buyer==null){buyer=World.CreatePed(models[c.BuyerModel],buyerBoat.Position+new Vector3(0,0,2));Track(buyer);buyer.BlockPermanentEvents=true;buyer.SetIntoVehicle(buyerBoat,VehicleSeat.Driver);buyer.Weapons.RemoveAll();}
                if(!Own(buyer) || buyer.IsDead){Fail("Comprador perdido.");return;}
                bool valid=StudioHeistRules.Sea(Driver(boat),Own(boat),Own(buyer) && buyer.IsAlive && buyer.CurrentVehicle==buyerBoat,NoPolice,boat.Speed,boat.Position.DistanceTo(buyerBoat.Position));
                if(!valid){hold=0;transferring=false;return;}if(key)transferring=true;if(transferring){hold+=dt;GTA.UI.Screen.ShowSubtitle("Transferindo dispositivo... "+Math.Min(5,hold/1000)+"/5s",100);if(hold>=5000)Advance();}break;
            }
        }
        void Flush(){if(!active || !Identity || Wallet.StudioHeist?.Pending?.Id!=id || Wallet.StudioHeist.Pending.Ready)return;if(!Wallet.StudioCheckpoint(id,elapsed,out _))throw new InvalidOperationException("Checkpoint recusado.");unsaved=0;}
        void CompleteStage(){Flush();if(!Wallet.StudioReady(flow,out _))throw new InvalidOperationException("Comprovacao de entrega recusada.");int act=flow.Act;string done=id;Suspend();if(act<5){if(Wallet.StudioFinish(done,"prepared",out _))Notify.Show("Preparacao salva! Proximo ato no F11 > Contratos / estudio.");}else Notify.Show("Dispositivo entregue e salvo! F11 > Contratos / estudio > Receber para creditar recompensa uma vez.");}
        void Collect(string caseId){if(!SafeStart){Notify.Show("Receba sem procurado, missao ou outra atividade.");return;}var p=Wallet.StudioHeist?.Pending;if(p==null || p.Id!=caseId || !p.Ready)return;string outcome=p.Act==5?"paid":"prepared";if(Wallet.StudioFinish(caseId,outcome,out bool changed))Notify.Show(changed?(p.Act==5?"Assalto concluido! Banco +$"+p.Reward:"Preparacao confirmada."):"Ato ja confirmado; sem pagamento repetido.");else Notify.Show("Saldo no limite ou registro protegido; entrega pronta conservada.");OpenMenu();}
        void Fail(string reason){string failed=id;Flush();if(!Wallet.StudioFinish(failed,"failed",out _))throw new InvalidOperationException("Falha nao gravada.");Suspend();Notify.Show("ATO FALHOU: "+reason+" Preparacoes anteriores mantidas.");}
        public void Tick()
        {
            Drain();if(!active)return;
            try{
                bool eligible=Identity && Input;int dt=CustodyRules.Credit(unchecked(Game.GameTime-last),eligible && eligibleBefore);last=Game.GameTime;eligibleBefore=eligible;
                if(!Identity || gate.Owner!=Name || !WorldFree || Wallet.StudioHeist?.Valid!=true || Wallet.StudioHeist.Pending?.Id!=id || !Wallet.GangWarEconomyFree || !gangFree()){if(Identity && actor.IsDead)Fail("Morte.");else Suspend();return;}if(!eligible){pressed=false;return;}
                bool key=pressed || Game.IsControlJustPressed(GTA.Control.Context);pressed=false;
                if(loading){if(models.Values.All(m=>m.IsLoaded))loading=false;else{if(unchecked(Game.GameTime-modelAt)>10000)throw new InvalidOperationException("Modelos nao carregaram.");return;}}
                elapsed=Math.Min(c.SecondsLimit*1000,elapsed+dt);unsaved+=dt;if(elapsed>=c.SecondsLimit*1000){Fail("Prazo ativo esgotado.");return;}if(unsaved>=5000)Flush();
                if(Objective==StudioObjective.Ready){CompleteStage();return;}
                var target=Target();World.DrawMarker(MarkerType.Cylinder,Pos(target)-new Vector3(0,0,1),Vector3.Zero,Vector3.Zero,new Vector3(1.2f,1.2f,.3f),Color.FromArgb(150,240,200,30));GTA.UI.Screen.ShowSubtitle(Label(Objective)+" | E: interagir | END: suspender",100);
                if(flow.Act==1 && (!NoPolice || actor.IsShooting)){Fail("Documentos exigem silencio e ausencia de procurado.");return;}
                if(flow.Act<5)PrepTick(dt,key);else FinaleTick(dt,key);
                if(active && Objective==StudioObjective.Ready)CompleteStage();probeKey=null;
            }catch(WorldPending ex){if(probeKey!=ex.Message){probeKey=ex.Message;probeSince=Game.GameTime;}if(unchecked(Game.GameTime-probeSince)>=5000){Suspend();Notify.Show("Contrato suspenso: "+ex.Message+" Retome no F11 apos liberar/carregar o ponto.");}else GTA.UI.Screen.ShowSubtitle("Aguardando piso/agua/vaga (ate5s): "+ex.Message,100);
            }catch(Exception ex){Log.Error("studio contract",ex);try{Suspend();}catch(Exception cleanup){Log.Error("studio checkpoint",cleanup);}Notify.Show("Contrato suspenso: "+ex.Message+" Retome pelo F11; preparacoes/saldo conservados.");}
        }
        void Suspend()
        {
            try{Flush();}finally{
                foreach(var e in owned){if(e is Ped p && Own(p) && p.IsAlive){p.Weapons.RemoveAll();p.Task.ClearAll();p.BlockPermanentEvents=false;Function.Call(Hash.SET_PED_KEEP_TASK,p,false);if(p.IsInVehicle())JobActors.Leave(p,p.CurrentVehicle);else p.Task.Wander();}if(e is Prop prop && Own(prop) && prop.IsAttached())prop.Detach();retired.Add(Tuple.Create(e,token));}owned.Clear();guards.Clear();guardPoints.Clear();foreach(var m in models.Values)m.MarkAsNoLongerNeeded();models.Clear();if(route!=null && route.Exists())route.Delete();route=null;
                active=loading=pressed=eligibleBefore=alerted=transferring=false;flow=null;actor=director=contact=buyer=gateGuard=null;car=trailer=directorCar=boat=buyerBoat=null;cargo=null;profile=id=probeKey=null;c=null;gate.Release(Name);
            }
        }
        static bool Connected(Vehicle v){var t=new OutputArgument();return Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,v) || Function.Call<bool>(Hash.GET_VEHICLE_TRAILER_VEHICLE,v,t);}
        void Drain()
        {
            for(int i=retired.Count-1;i>=0;i--){var r=retired[i];var e=r.Item1;if(!Tagged(e,r.Item2)){if(e is Vehicle lost)JobVehicles.Release(lost);retired.RemoveAt(i);continue;}
                if(e is Vehicle v){
                    var occupants=v.Occupants;
                    if(occupants.Length>0 && occupants.All(p=>Tagged(p,r.Item2) && p.IsDead) && v.Speed<.5f && !v.IsInAir && !Connected(v)){foreach(var p in occupants)p.MarkAsNoLongerNeeded();v.MarkAsNoLongerNeeded();JobVehicles.Release(v);retired.RemoveAt(i);continue;}
                    bool towed=false;foreach(var candidate in World.GetNearbyVehicles(v.Position,25)){if(candidate==v)continue;var tr=new OutputArgument();if(Function.Call<bool>(Hash.GET_VEHICLE_TRAILER_VEHICLE,candidate,tr) && tr.GetResult<int>()==v.Handle){towed=true;if(Tagged(candidate,r.Item2) && candidate.Occupants.Length==0 && v.Occupants.Length==0 && candidate.Speed<.5f && v.Speed<.5f && !candidate.IsInAir && !v.IsInAir)Function.Call(Hash.DETACH_VEHICLE_FROM_TRAILER,candidate);break;}}
                    if(towed)continue;
                    if(v.Occupants.Length!=0 || v.Speed>=.5f || v.IsInAir){continue;}if(Connected(v)){var t=new OutputArgument();if(Function.Call<bool>(Hash.GET_VEHICLE_TRAILER_VEHICLE,v,t)){var ownTrailer=retired.Select(x=>x.Item1).OfType<Vehicle>().FirstOrDefault(x=>x.Handle==t.GetResult<int>() && Tagged(x,r.Item2));if(ownTrailer!=null && ownTrailer.Occupants.Length==0 && ownTrailer.Speed<.5f && !ownTrailer.IsInAir)Function.Call(Hash.DETACH_VEHICLE_FROM_TRAILER,v);}continue;}JobVehicles.Release(v);v.Delete();}
                else if(e is Ped p){if(p.IsInVehicle()){p.MarkAsNoLongerNeeded();retired.RemoveAt(i);continue;}p.Delete();}else e.Delete();retired.RemoveAt(i);
            }
        }
        public void OnKey(KeyEventArgs e){if(!active)return;if(e.KeyCode==Keys.E)pressed=true;if(e.KeyCode==Keys.End){Suspend();Notify.Show("Ato suspenso. Preparacoes e prazo salvos; retomar reinicia somente este ato.");}}
        public void Save(){Flush();}
        public void Reload(){menu.Visible=false;Suspend();}
        public void Dispose(){menu.Visible=false;Suspend();foreach(var r in retired){if(Tagged(r.Item1,r.Item2))r.Item1.MarkAsNoLongerNeeded();if(r.Item1 is Vehicle v)JobVehicles.Release(v);}retired.Clear();}
    }
}
