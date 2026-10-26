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
    public sealed class HalloweenSystem : ISystem
    {
        public string Name=>"halloween";const string Decor="RPCore_HalloweenOwner",Audio="Seasonal/Halloween1";
        sealed class Pursuer{public Vehicle Car;public Ped Driver;public int TaskAt,RamAt;}
        sealed class Retired{public Vehicle Car;public Ped Driver;public int Token,TaskAt;}
        readonly ObjectPool pool;readonly ShiftGate gate;readonly Func<bool> gangFree;readonly HalloweenOptions defaults;readonly NativeMenu menu;readonly Random random=new Random();
        readonly Dictionary<string,Model> models=new Dictionary<string,Model>();readonly List<Pursuer> pursuers=new List<Pursuer>();readonly List<Retired> retired=new List<Retired>();
        HalloweenRun run;HalloweenOptions c;Vehicle hearse;Ped actor;string profile,id;Blip start,route;int token,last,modelAt,spawnWait,awaited,unsaved,waveAt,spawned,nextScare,scareAt=-1,pulseAt=-1,recoverAt;bool active,loading,worldBefore,pulsePressed;Vector3 pulseOrigin;
        bool Identity=>profile==Profile.Id && actor!=null && actor.Exists() && actor==Game.Player.Character;
        bool Input=>!Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
        bool WorldFree=>Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool StartSafe=>WorldFree && Game.Player.Character.IsOnFoot && !Game.Player.Character.IsRagdoll && !Game.Player.Character.IsInCombat && Game.Player.Wanted.WantedLevel==0 && gate.Owner==null && Wallet.GangWarEconomyFree && gangFree() && !Game.IsPaused && !InterfaceOptions.ModalOpen;
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        static bool Tagged(Entity e,int t)=>e!=null && e.Exists() && e.Handle!=Game.Player.Character.Handle && Function.Call<bool>(Hash.DECOR_EXIST_ON,e,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,e,Decor)==t;
        bool Own(Entity e)=>Tagged(e,token);
        void Mark(Entity e){if(e==null || !e.Exists())throw new InvalidOperationException("Recurso Halloween indisponivel.");if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);if(!Function.Call<bool>(Hash.DECOR_SET_INT,e,Decor,token)){e.MarkAsNoLongerNeeded();throw new InvalidOperationException("Marcacao de recurso recusada.");}e.IsPersistent=true;}
        bool Driver=>Own(hearse) && Identity && actor.CurrentVehicle==hearse && hearse.Driver==actor;
        static bool Connected(Vehicle v){var t=new OutputArgument();return Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,v) || Function.Call<bool>(Hash.GET_VEHICLE_TRAILER_VEHICLE,v,t);}
        bool Pair(Pursuer a)=>Own(a.Car) && Own(a.Driver) && !a.Car.IsDead && a.Car.IsDriveable && a.Driver.IsAlive && a.Car.Driver==a.Driver && a.Car.Occupants.All(p=>p==a.Driver) && !Connected(a.Car);
        public HalloweenSystem(ObjectPool pool,ShiftGate gate,Func<bool> gangFree=null)
        {
            this.pool=pool;this.gate=gate;this.gangFree=gangFree??(()=>true);defaults=Store.LoadDef("Missions/Halloween.json",HalloweenOptions.Defaults);if(defaults?.Valid!=true){defaults=HalloweenOptions.Defaults();Log.Write("Invalid Halloween config retained; defaults used.");}
            menu=new NativeMenu("Halloween","THE LAST RIDE");pool.Add(menu);Visuals.Register(menu,"core");recoverAt=Game.GameTime;
        }
        void Add(string title,string description,Action action,bool enabled=true){var item=new NativeItem(title,description){Enabled=enabled};item.Activated+=(s,e)=>{try{action();}catch(Exception ex){Log.Error("Halloween menu",ex);Notify.Show("Acao nao gravada; registro e saldos conservados.");}};menu.Add(item);}
        public void OpenMenu()
        {
            pool.HideAll();menu.Clear();string owner=Profile.Id;var b=Wallet.Halloween;
            if(b!=null && !b.Valid){Add("Registro protegido","Confira Halloween em Wallet.json/backup; nenhum progresso sera sobrescrito.",()=>{});menu.Visible=true;return;}
            Add("GPS: The Last Ride","Vá ao ponto Halloween indicado no mapa. A corrida exige iniciar a pe, ate3,5m do ponto, sem trabalho/missao/procurado.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,defaults.Start.X,defaults.Start.Y);Notify.Show("Halloween marcado no mapa.");});
            Add("Sustos / imagem e audio: "+(b?.Scares!=false?"LIGADOS":"DESLIGADOS"),"Inclui imagem JumpScareOne do YTD instalado e Halloween1.wav. Pode desligar antes de aceitar a missao. Sem mudar clima/relogio/camera globais.",()=>{if(owner==Profile.Id && Wallet.HalloweenConfigure(b?.Scares==false,out _))OpenMenu();},!active && b?.Pending==null);
            Add("Iniciar The Last Ride","CONFIRMA corrida de"+defaults.SecondsLimit+"s com carros fantasmas proprios perseguindo/colidindo. Dirija o Romero ate cemiterio, pare5m do destino. E/polegar direito: pulso"+defaults.PulseRadius+"m a cada"+defaults.PulseCooldownSeconds+"s, somente contra perseguidores proprios. Nao paga dinheiro, conforme PSRP. Menus comuns nao pausam o mundo; ESC pausa.",()=>{if(owner==Profile.Id)Begin();},!active && b?.Pending==null);
            if(active)Add("Encerrar tentativa","Marca interrupcao, recolhe apenas recursos proprios. Nao apaga estrelas/danos do jogador, nao devolve tempo nem concede recompensa. Proxima tentativa comeca do inicio.",()=>{End("interrupted","Tentativa interrompida.");OpenMenu();});
            Add("Concluidas "+(b?.Completed??0)+" / falhas "+(b?.Failed??0),"Iniciadas "+(b?.Started??0)+" / interrompidas "+(b?.Interrupted??0)+" / pulsos "+(b?.TotalPulses??0)+". Historico32; banco/dinheiro/inventario mantidos.",()=>{});
            foreach(var h in (b?.History??new List<HalloweenRecord>()).AsEnumerable().Reverse().Take(8))Add(h.Outcome+" / "+Math.Ceiling(h.Elapsed/1000d)+"s / "+h.Condition.ToString("0")+"%",h.Id+" / pulsos"+h.Pulses,()=>{});
            Add("Fechar","Voltar ao jogo.",()=>menu.Visible=false);menu.Visible=true;
        }
        void Begin()
        {
            if(active || !StartSafe || Game.Player.Character.Position.DistanceTo(Pos(defaults.Start))>3.5f || Wallet.Halloween!=null && (!Wallet.Halloween.Valid || Wallet.Halloween.Pending!=null)){Notify.Show("Va a pe ao ponto Halloween, sem procurado/missao/atividade. Aguarde recuperar registro antigo, se houver.");return;}
            if(!gate.Acquire(Name))return;
            try{string attempt=Guid.NewGuid().ToString("N");if(!Wallet.HalloweenBegin(attempt,defaults,out _)){gate.Release(Name);return;}var p=Wallet.Halloween.Pending;
                profile=Profile.Id;actor=Game.Player.Character;id=p.Id;c=p.Options;run=new HalloweenRun(id,c,p.Progress);token=random.Next(1,int.MaxValue);active=loading=true;modelAt=last=Game.GameTime;awaited=unsaved=spawnWait=spawned=0;waveAt=5000;worldBefore=false;nextScare=random.Next(30000,75001);pool.HideAll();
                foreach(var name in c.AttackerModels.Concat(new[]{c.HearseModel,c.DriverModel}).Distinct()){var m=new Model(name);if(!m.IsValid || !m.IsInCdImage || (name==c.DriverModel?!m.IsPed:!m.IsCar))throw new InvalidOperationException("Modelo invalido: "+name);m.Request();models.Add(name,m);}
                Notify.Show("The Last Ride: aguarde o Romero; prazo de corrida comeca somente ao assumir o volante. END encerra.");
            }catch{try{End("interrupted","Falha ao preparar Halloween.");}catch{Close();}throw;}
        }
        void SpawnHearse(int dt)
        {
            var p=Pos(c.Hearse);Function.Call(Hash.REQUEST_COLLISION_AT_COORD,p.X,p.Y,p.Z);float floor=0;
            bool free=JobActors.Free(p,4) && World.GetGroundHeight(p+new Vector3(0,0,3),out floor,GetGroundHeightMode.Normal);
            if(!free || Math.Abs(floor-p.Z)>3){spawnWait+=dt;if(spawnWait>=5000)End("interrupted","Vaga/piso do Romero indisponivel. Libere o ponto e tente novamente.");return;}
            hearse=World.CreateVehicle(models[c.HearseModel],new Vector3(p.X,p.Y,floor+.1f),c.Hearse.Heading);Mark(hearse);JobVehicles.Reserve(hearse);Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY,hearse);hearse.IsEngineRunning=true;
            if(route!=null && route.Exists())route.Delete();route=hearse.AddBlip();route.Sprite=BlipSprite.Ghost;route.Color=BlipColor.Yellow;route.Name="The Last Ride / Romero";loading=false;Notify.Show("Entre como motorista no Romero marcado. Apos sair com ele, abandono reprova a corrida.");
        }
        bool SpawnAttacker()
        {
            Vector3? position=null;float heading=0;
            for(int i=0;i<20;i++){double angle=i*2.4;var guess=hearse.Position+hearse.Velocity*1.5f+new Vector3((float)Math.Cos(angle)*80,(float)Math.Sin(angle)*80,0);var n=new OutputArgument();var h=new OutputArgument();if(!Function.Call<bool>(Hash.GET_CLOSEST_VEHICLE_NODE_WITH_HEADING,guess.X,guess.Y,guess.Z,n,h,1,3f,0))continue;var p=n.GetResult<Vector3>();float d=p.DistanceTo(hearse.Position),floor;if(d<55 || d>140 || Math.Abs(p.Z-hearse.Position.Z)>6 || !JobActors.Free(p,5) || !World.GetGroundHeight(p+new Vector3(0,0,3),out floor,GetGroundHeightMode.Normal) || Math.Abs(floor-p.Z)>2)continue;if(Function.Call<bool>(Hash.IS_SPHERE_VISIBLE,p.X,p.Y,p.Z,4f))continue;position=new Vector3(p.X,p.Y,floor+.1f);heading=h.GetResult<float>();break;}
            if(!position.HasValue)return false;
            var a=new Pursuer();pursuers.Add(a);string name=c.AttackerModels[random.Next(c.AttackerModels.Count)];a.Car=World.CreateVehicle(models[name],position.Value,heading);Mark(a.Car);JobVehicles.Reserve(a.Car);Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY,a.Car);a.Car.IsEngineRunning=true;
            a.Driver=World.CreatePed(models[c.DriverModel],position.Value+new Vector3(0,0,2));Mark(a.Driver);if(!a.Driver.IsHuman)throw new InvalidOperationException("Motorista Halloween precisa ser humano.");a.Driver.RelationshipGroup=new RelationshipGroup((int)RelationshipGroupHash.CivilianMale);a.Driver.BlockPermanentEvents=true;a.Driver.Weapons.RemoveAll();Function.Call(Hash.SET_PED_KEEP_TASK,a.Driver,true);Function.Call(Hash.SET_PED_MONEY,a.Driver,0);Function.Call(Hash.SET_PED_CAN_SWITCH_WEAPON,a.Driver,false);a.Driver.SetIntoVehicle(a.Car,VehicleSeat.Driver);if(a.Car.Driver!=a.Driver)throw new InvalidOperationException("Assento do perseguidor indisponivel.");Function.Call(Hash.SET_ENTITY_VISIBLE,a.Driver,false,false);spawned++;return true;
        }
        void Traffic()
        {
            int time=run.Progress.Elapsed;
            for(int i=pursuers.Count-1;i>=0;i--){var a=pursuers[i];if(!Pair(a) || a.Car.Position.DistanceTo(hearse.Position)>260){Retire(a);pursuers.RemoveAt(i);continue;}if(time<a.TaskAt || a.Car.IsInAir)continue;a.TaskAt=time+c.RetaskMilliseconds;
                Function.Call(Hash.TASK_VEHICLE_CHASE,a.Driver,actor);Function.Call(Hash.SET_DRIVE_TASK_DRIVING_STYLE,a.Driver,786468);Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS,a.Driver,c.SecondsLimit*1000-time<60000?1f:.85f);Function.Call(Hash.SET_DRIVER_ABILITY,a.Driver,1f);Function.Call(Hash.SET_TASK_VEHICLE_CHASE_IDEAL_PURSUIT_DISTANCE,a.Driver,0f);
                float distance=a.Car.Position.DistanceTo(hearse.Position);if(distance>6 && distance<c.RamRange && time>=a.RamAt && a.Car.Speed>3 && random.NextDouble()<.8){a.RamAt=time+c.RamCooldownMilliseconds;var d=hearse.Position+hearse.Velocity*.35f-a.Car.Position;d.Z=0;if(d.Length()>.1f){d.Normalize();float speed=Math.Max(17,Math.Min(29,a.Car.Speed+7));var v=a.Car.Velocity;a.Car.Velocity=new Vector3(v.X*.3f+d.X*speed*.7f,v.Y*.3f+d.Y*speed*.7f,v.Z);}}
            }
            if(time>=waveAt && spawned<c.MaxSpawns && pursuers.Count<c.MaxAttackers){waveAt=time+5000;if(SpawnAttacker())waveAt=time+c.SpawnSeconds*1000;}
        }
        void Pulse()
        {
            var before=new HalloweenRun(id,c,run.Progress);if(!run.Pulse())return;
            try{if(!Wallet.HalloweenCheckpoint(id,run.Progress,out bool saved) || !saved){run=before;return;}unsaved=0;}catch{run=before;throw;}
            pulseAt=run.Progress.Elapsed;pulseOrigin=hearse.Position;
            foreach(var a in pursuers){if(!Pair(a))continue;var d=a.Car.Position-pulseOrigin;float distance=d.Length();if(!HalloweenRules.PulseTarget(Own(a.Car),Own(a.Driver) && a.Driver.IsAlive,a.Car.Driver==a.Driver,a.Car.Occupants.Any(p=>p!=a.Driver),Connected(a.Car),a.Car.IsInAir,distance,c))continue;var direction=distance>.1f?d/distance:hearse.ForwardVector;float speed=HalloweenRules.PulseSpeed(c,distance);a.Car.Velocity=new Vector3(direction.X*speed,direction.Y*speed,Math.Max(3.5f,direction.Z*speed+4));a.Driver.Task.ClearAll();a.TaskAt=run.Progress.Elapsed+1600;}
        }
        void Presentation(bool input)
        {
            int time=run.Progress.Elapsed;if(Wallet.Halloween.Scares && input){Visuals.Ready("PSRP_Banners");if(time>=nextScare){nextScare=time+random.Next(45000,75001);scareAt=time;Sfx.Play(Audio);}}
            if(!input){scareAt=-1;return;}
            if(scareAt>=0){int age=time-scareAt;if(age>=800 || !Wallet.Halloween.Scares)scareAt=-1;else{int alpha=(int)(255*(age<500?age/500f:(800-age)/300f));if(!Visuals.Sprite("PSRP_Banners","JumpScareOne",0,0,Visuals.Width,1080,Color.FromArgb(Math.Max(0,Math.Min(255,alpha)),Color.White)))GTA.UI.Screen.ShowSubtitle("Uma sombra passa pelo retrovisor...",100);}}
            if(pulseAt>=0){int age=time-pulseAt;if(age>=650)pulseAt=-1;else{float t=age/650f,r=.6f+c.PulseRadius*t;Function.Call(Hash.DRAW_MARKER,25,pulseOrigin.X,pulseOrigin.Y,pulseOrigin.Z-.45f,0f,0f,0f,0f,0f,0f,r*2,r*2,.15f,55,185,255,(int)(180*(1-t)),false,false,2,false,0,0,false);}}
        }
        void Hud()
        {
            int remain=c.SecondsLimit*1000-run.Progress.Elapsed,seconds=(remain+999)/1000,cooldown=Math.Max(0,run.Progress.NextPulse-run.Progress.Elapsed);string objective=run.Stage==HalloweenStage.AwaitDriver?"ENTRE COMO MOTORISTA":"CHEGUE AO CEMITERIO E PARE";
            if(!Visuals.Active){GTA.UI.Screen.ShowSubtitle("THE LAST RIDE | "+seconds/60+":"+(seconds%60).ToString("00")+" | "+objective+" | E: pulso / "+cooldown/1000+"s",100);return;}
            float x=Visuals.Width-Visuals.InsetX-360,y=250+Visuals.InsetY;Visuals.Rect(x,y,330,240,Color.FromArgb(215,10,11,15));Visuals.Rect(x,y,330,7,Color.FromArgb(255,195,76,38));Visuals.Label("THE LAST RIDE",x+15,y+18,.38f,Color.White);Visuals.Label(seconds/60+":"+(seconds%60).ToString("00"),x+230,y+18,.38f,seconds<=30?Color.OrangeRed:Color.White);Visuals.Label(objective,x+15,y+62,.24f,Color.White);Visuals.Label("Distancia: "+hearse.Position.DistanceTo(Pos(c.Destination)).ToString("0")+"m",x+15,y+98,.28f,Color.White);Visuals.Label("Romero: "+HalloweenRules.Condition(hearse.BodyHealth,hearse.EngineHealth).ToString("0")+"% / fantasmas: "+pursuers.Count,x+15,y+135,.28f,Color.Orange);Visuals.Label(cooldown==0?"E / polegar direito: PULSO PRONTO":"Pulso: "+(cooldown/1000f).ToString("0.0")+"s",x+15,y+173,.26f,cooldown==0?Color.LightGreen:Color.LightGray);Visuals.Label("END encerra | Sem premio em dinheiro",x+15,y+208,.22f,Color.Gray);
        }
        void Flush(){if(!active || !Identity || Wallet.Halloween?.Pending?.Id!=id)return;if(!Wallet.HalloweenCheckpoint(id,run.Progress,out _))throw new InvalidOperationException("Checkpoint Halloween recusado.");unsaved=0;}
        void End(string outcome,string reason)
        {
            try{if(active && profile==Profile.Id && Wallet.Halloween?.Pending?.Id==id){Flush();float condition=Own(hearse)?HalloweenRules.Condition(hearse.BodyHealth,hearse.EngineHealth):0;if(!Wallet.HalloweenFinish(id,outcome,condition,outcome=="completed"?run:null,out _))throw new InvalidOperationException("Resultado Halloween nao gravado.");}}finally{Close();}Notify.Show(reason+(outcome=="completed"?" Conclusao registrada; sem premio em dinheiro, conforme PSRP.":" Sem alterar banco/dinheiro/inventario."));
        }
        public void Tick()
        {
            Drain();try{
                if(!active){if(Wallet.Halloween?.Valid==true && Wallet.Halloween.Pending!=null && unchecked(Game.GameTime-recoverAt)>=0){recoverAt=Game.GameTime+2000;Wallet.HalloweenFinish(Wallet.Halloween.Pending.Id,"interrupted",0,null,out _);}
                    if(start==null || !start.Exists()){start=World.CreateBlip(Pos(defaults.Start));start.Sprite=BlipSprite.Ghost;start.Color=BlipColor.Orange;start.Name="Halloween / The Last Ride";}
                    if(StartSafe && Input && Game.Player.Character.Position.DistanceTo(Pos(defaults.Start))<=25){World.DrawMarker(MarkerType.Cylinder,Pos(defaults.Start)-new Vector3(0,0,1),Vector3.Zero,Vector3.Zero,new Vector3(1.8f,1.8f,.3f),Color.Orange);if(Game.Player.Character.Position.DistanceTo(Pos(defaults.Start))<=3.5f){GTA.UI.Screen.ShowHelpTextThisFrame("Pressione ~INPUT_CONTEXT~ para Halloween / The Last Ride.");if(Game.IsControlJustPressed(GTA.Control.Context))OpenMenu();}}return;
                }
                bool world=Identity && !Game.IsPaused;int dt=CustodyRules.Credit(unchecked(Game.GameTime-last),world && worldBefore);last=Game.GameTime;worldBefore=world;
                if(!Identity || gate.Owner!=Name || !WorldFree || !Wallet.GangWarEconomyFree || !gangFree() || Wallet.Halloween?.Valid!=true || Wallet.Halloween.Pending?.Id!=id){End(Identity && actor.IsDead?"failed":"interrupted","Corrida encerrada por morte/missao/troca de perfil ou atividade.");return;}
                if(Game.IsPaused){pulsePressed=false;return;}
                if(loading){pulsePressed=false;if(models.Values.All(m=>m.IsLoaded))SpawnHearse(dt);else if(unchecked(Game.GameTime-modelAt)>10000)End("interrupted","Modelos Halloween nao carregaram.");return;}
                if(!Own(hearse) || hearse.IsDead || !hearse.IsDriveable){End("failed","Romero perdido/destruido.");return;}if(Connected(hearse)){End("interrupted","Romero anexado a outro recurso; corrida interrompida.");return;}
                if(run.Stage==HalloweenStage.AwaitDriver){awaited+=dt;if(Driver){run.StartDriving(true);Flush();if(route!=null && route.Exists())route.Delete();route=World.CreateBlip(Pos(c.Destination));route.Name="The Last Ride / cemiterio";route.Color=BlipColor.Orange;route.ShowRoute=true;}else if(awaited>=c.AwaitDriverSeconds*1000){End("failed","Tempo para assumir o Romero esgotado.");return;}pulsePressed=false;if(Input)Hud();return;}
                if(!Driver){End("failed","Voce abandonou o volante do Romero.");return;}
                run.Tick(dt,true);unsaved+=dt;if(run.Stage==HalloweenStage.Failed){End("failed","Tempo da corrida esgotado.");return;}if(unsaved>=5000)Flush();
                if(run.Deliver(Driver,hearse.IsDriveable,hearse.Speed,hearse.Position.DistanceTo(Pos(c.Destination)))){End("completed","ENTREGA CONCLUIDA!");return;}
                bool key=Input && (pulsePressed || Game.IsControlJustPressed(GTA.Control.PhoneCameraSelfie) || Game.IsControlJustPressed(GTA.Control.Context));pulsePressed=false;if(key)Pulse();Traffic();Presentation(Input);if(Input)Hud();
            }catch(Exception ex){Log.Error("Halloween",ex);try{End("interrupted","Tentativa interrompida por falha tecnica; ultimo registro conservado.");}catch(Exception cleanup){Log.Error("Halloween cleanup",cleanup);Close();}}
        }
        void Retire(Pursuer a){if(a==null)return;retired.Add(new Retired{Car=a.Car,Driver=a.Driver,Token=token,TaskAt=Game.GameTime});if(Own(a.Driver)){Function.Call(Hash.SET_ENTITY_VISIBLE,a.Driver,true,false);Function.Call(Hash.SET_PED_CAN_SWITCH_WEAPON,a.Driver,true);Function.Call(Hash.SET_PED_KEEP_TASK,a.Driver,false);a.Driver.BlockPermanentEvents=false;a.Driver.Task.ClearAll();}}
        void Close()
        {
            try{foreach(var a in pursuers){try{Retire(a);}catch(Exception ex){Log.Error("Halloween retire",ex);}}if(hearse!=null)retired.Add(new Retired{Car=hearse,Token=token});}finally{pursuers.Clear();hearse=null;actor=null;profile=id=null;run=null;c=null;active=loading=pulsePressed=worldBefore=false;scareAt=pulseAt=-1;Sfx.Stop(Audio);foreach(var m in models.Values)m.MarkAsNoLongerNeeded();models.Clear();if(route!=null && route.Exists())route.Delete();route=null;gate.Release(Name);}
        }
        void Drain()
        {
            for(int i=retired.Count-1;i>=0;i--){var r=retired[i];bool p=Tagged(r.Driver,r.Token),v=Tagged(r.Car,r.Token);
                if(p){Function.Call(Hash.SET_ENTITY_VISIBLE,r.Driver,true,false);if(r.Driver.IsInVehicle() && (!v || !r.Driver.IsInVehicle(r.Car))){r.Driver.MarkAsNoLongerNeeded();r.Driver=null;p=false;}
                if(p && r.Driver.IsInVehicle()){if(v && r.Car.Occupants.All(o=>o==r.Driver) && r.Driver.IsDead && r.Car.Speed<.5f && !r.Car.IsInAir && !Connected(r.Car)){r.Driver.MarkAsNoLongerNeeded();r.Car.MarkAsNoLongerNeeded();JobVehicles.Release(r.Car);retired.RemoveAt(i);continue;}
                    if(v && r.Car.Speed<.5f && unchecked(Game.GameTime-r.TaskAt)>=0){JobActors.Leave(r.Driver,r.Car);r.TaskAt=Game.GameTime+4000;}else if(v && r.Car.Driver==r.Driver && r.Car.Occupants.All(o=>o==r.Driver) && unchecked(Game.GameTime-r.TaskAt)>=0){Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER,r.Driver,r.Car,10f,786603);r.TaskAt=Game.GameTime+4000;}
                    if(v && r.Car.Occupants.All(o=>o==r.Driver) && r.Car.Position.DistanceTo(Game.Player.Character.Position)>200 && !Connected(r.Car)){r.Driver.MarkAsNoLongerNeeded();r.Car.MarkAsNoLongerNeeded();JobVehicles.Release(r.Car);retired.RemoveAt(i);}continue;
                }if(p)r.Driver.Delete();r.Driver=null;}
                if(v){if(r.Car.Occupants.Length!=0 || r.Car.Speed>=.5f || r.Car.IsInAir || Connected(r.Car))continue;r.Car.Delete();}JobVehicles.Release(r.Car);retired.RemoveAt(i);
            }
        }
        public void OnKey(KeyEventArgs e){if(!active)return;if(e.KeyCode==Keys.E && Input)pulsePressed=true;if(e.KeyCode==Keys.End)End("interrupted","Tentativa encerrada pelo jogador.");}
        public void Save(){Flush();}
        public void Reload(){menu.Visible=false;Close();recoverAt=Game.GameTime;}
        public void Dispose(){menu.Visible=false;try{if(active)End("interrupted","Halloween encerrado.");}finally{Close();if(start!=null && start.Exists())start.Delete();foreach(var r in retired){if(Tagged(r.Driver,r.Token)){Function.Call(Hash.SET_ENTITY_VISIBLE,r.Driver,true,false);r.Driver.MarkAsNoLongerNeeded();}if(Tagged(r.Car,r.Token))r.Car.MarkAsNoLongerNeeded();JobVehicles.Release(r.Car);}retired.Clear();}}
    }
}
