using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
namespace RPCore
{
    internal sealed class GangMowing
    {
        const string Owner="gang_mowing",Decor="RPCore_MowingOwner";
        sealed class Retiring { public Vehicle Car;public int Token; }
        readonly List<Retiring> retiring=new List<Retiring>();
        readonly Dictionary<int,Prop> grass=new Dictionary<int,Prop>();
        readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Func<IEnumerable<GangDefinition>> gangs;readonly Func<IEnumerable<GangTurfDefinition>> areas;readonly GangMowingOptions options;readonly Random random=new Random();
        Vehicle mower;Model vehicleModel,grassModel;Blip mowerBlip,patchBlip;string profile,id;int player,token,last,nearest=-1;long elapsed,prepareAt,blipAt,warningAt;bool render,spawned;public bool Active{get;private set;}
        GangMowingAttempt Pending=>Wallet.GangMowing?.Pending;GangTurfDefinition Current=>areas().FirstOrDefault(a=>a.Id==8);
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        static JobPoint Point(Vector3 p)=>new JobPoint{Name="Cortador",X=p.X,Y=p.Y,Z=p.Z};
        bool Safe=>Game.Player.Character!=null && Game.Player.Character.Exists() && Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && Game.Player.Wanted.WantedLevel==0 && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool Input=>Safe && !Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
        bool Owns=>Pending!=null && GangBusinessTransactions.Owns(state(),Current) && GangCrewRules.SameGang(Pending.Gang,state().Gang) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang));
        bool At(GangMowingOptions c)=>Safe && !Game.IsPaused && !InterfaceOptions.ModalOpen && Game.Player.Character.IsOnFoot && Game.Player.Character.Position.DistanceTo(Pos(c.Spawn))<=c.StartRadius;
        static bool Tagged(Entity e,int value)=>e!=null && e.Exists() && e.Handle!=Game.Player.Character.Handle && Function.Call<bool>(Hash.DECOR_EXIST_ON,e,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,e,Decor)==value;
        bool Mark(Entity e){try{if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);return Function.Call<bool>(Hash.DECOR_SET_INT,e,Decor,token);}catch{return false;}}
        bool OwnMower=>Tagged(mower,token);
        bool Driver=>OwnMower && Game.Player.Character.IsInVehicle(mower) && mower.Driver==Game.Player.Character;
        bool Foreign(Vehicle v)=>v!=null && v.Exists() && (Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,v) || Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER,v) || v.Occupants.Any(p=>p!=null && p.Exists() && p.Handle!=player));
        public GangMowing(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs,Func<IEnumerable<GangTurfDefinition>> areas)
        {this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;this.areas=areas;options=Store.LoadDef("Gangs/Mowing.json",()=>new GangMowingOptions());if(options?.Valid!=true){options=new GangMowingOptions();Log.Write("Invalid mowing config; defaults in memory, original retained.");}}
        public void Controls(Action<string,string,Action> add,Action reopen)
        {
            var p=Pending;if(p==null)return;
            add("Jardinagem pendente / "+p.Mowed.Count+"/"+p.Options.Patches.Count,"Pontos cortados salvos. Retomada recria somente a grama restante e um cortador proprio.",null);
            add(Active?"Suspender jardinagem":"Retomar jardinagem",p.Ready?"Todos os pontos salvos. A pe no ponto inicial, retome e E confirma bonus.":"Saia do cortador anterior, volte a pe ao ponto inicial com posse e retome. Sem refazer os pontos salvos.",()=>{if(Active){Suspend();reopen();}else if(!Resume())Notify.Show("Confira ponto inicial a pe, posse, seguranca, atividade e cortador anterior liberado.");});
            add("GPS: jardinagem pendente","Ponto inicial salvo do contrato.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,p.Options.Spawn.X,p.Options.Spawn.Y);pool.HideAll();});
            add("Cancelar contrato de jardinagem","Gratis, sem premio parcial. Conserva bonus anterior; pode cancelar sem posse em seguranca. Nao ejeta ocupantes do cortador.",()=>{if(!Safe || Game.IsPaused || InterfaceOptions.ModalOpen || gate.Owner!=null && gate.Owner!=Owner || !Wallet.GangMowingCancel(p.Id,out _)){Notify.Show("Cancelamento nao confirmado; contrato conservado.");return;}Suspend();reopen();});
        }
        public void Append(GangTurfDefinition area,Action<string,string,Action> add)
        {
            if(area.Id!=8)return;var a=GangMowingTransactions.Account(Wallet.GangMowing,state().Gang);var utc=DateTime.UtcNow;string status=GangMowingTransactions.Multiplier(Wallet.GangMowing,state().Gang,8,utc)==2?"ATIVO / "+Math.Ceiling((a.ExpiresUtcTicks-utc.Ticks)/(double)TimeSpan.TicksPerMinute)+"min":"inativo";
            add("Jardinagem / bonus2X: "+status,"Contratos: "+(a?.Completed??0)+". Acelera processamento na area8, sem premio em cash ou estoque e sem ligar automatico.",null);
            add("GPS: cortador Mirror Park","A pe a ate"+options.StartRadius+"m do ponto, com espaco livre para o cortador.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,options.Spawn.X,options.Spawn.Y);pool.HideAll();});
            add("Cortar grama / "+options.Patches.Count+" pontos","Dirija o cortador fornecido sobre todos os pontos verdes. Cada corte e salvo; ao acabar, saia e E perto do cortador ou ponto inicial confirma2X/"+options.BoostMinutes+"min. END suspende.",()=>{if(!Begin(area))Notify.Show("Exige posse da area8, ponto inicial a pe, seguranca e nenhuma atividade/pendencia.");});
        }
        bool Begin(GangTurfDefinition area)
        {if(Active || gate.Owner!=null || retiring.Count!=0 || !At(options) || !gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang)) || !gate.Acquire(Owner))return false;try{if(!Wallet.GangMowingBegin(state(),area,options,Guid.NewGuid().ToString("N"),out bool saved) || !saved){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}}
        bool Resume(){if(Active || gate.Owner!=null || retiring.Count!=0 || Wallet.GangMowing?.Valid!=true || !Owns || !Wallet.GangMowingOtherFree || !At(Pending.Options) || !gate.Acquire(Owner))return false;Activate();return true;}
        void Activate(){Active=true;profile=Profile.Id;id=Pending.Id;player=Game.Player.Character.Handle;token=random.Next(1,int.MaxValue);last=Game.GameTime;elapsed=prepareAt=blipAt=warningAt=0;nearest=-1;render=Visuals.Enabled && Visuals.UseProps;spawned=false;pool.HideAll();if(!Pending.Ready){vehicleModel=new Model(Pending.Options.VehicleModel);grassModel=new Model(Pending.Options.GrassModel);if(!vehicleModel.IsInCdImage || !vehicleModel.IsVehicle || render && !grassModel.IsInCdImage){Suspend();Notify.Show("Modelos indisponiveis; contrato conservado.");return;}vehicleModel.Request();if(render)grassModel.Request();}Notify.Show("Jardinagem ativa. Dirija o cortador marcado sobre cada ponto verde. END suspende e conserva cortes.");}
        bool Free(Vector3 point)=>Game.Player.Character.Position.DistanceTo(point)>=3 && World.GetNearbyVehicles(point,4).Length==0 && World.GetNearbyPeds(point,2).All(p=>p==null || !p.Exists());
        void Prepare()
        {
            if(elapsed<prepareAt)return;var c=Pending.Options;
            if(!vehicleModel.IsLoaded || render && !grassModel.IsLoaded){if(elapsed>10000){Suspend();Notify.Show("Modelos nao carregaram; contrato conservado para retomar.");}return;}
            if(!Free(Pos(c.Spawn))){prepareAt=elapsed+1500;if(elapsed>=warningAt){warningAt=elapsed+10000;Notify.Show("Afaste-se3m do ponto do cortador e deixe a vaga livre. Nenhum veiculo sera removido.");}return;}
            mower=World.CreateVehicle(vehicleModel,Pos(c.Spawn),c.Spawn.Heading);if(mower==null || !mower.Exists()){mower=null;prepareAt=elapsed+2000;return;}
            if(!Mark(mower)){mower.MarkAsNoLongerNeeded();mower=null;Suspend();return;}mower.IsPersistent=true;JobVehicles.Reserve(mower);vehicleModel.MarkAsNoLongerNeeded();vehicleModel=null;spawned=true;
            mowerBlip=MapBlips.Create(mower.Position,"RPCore: cortador de grama",BlipSprite.Standard,BlipColor.Green,.8f);mowerBlip.ShowRoute=true;
        }
        bool Decorate()
        {
            if(!render)return true;var p=Pending;int created=0;
            for(int i=0;i<p.Options.Patches.Count;i++)
            {
                if(p.Mowed.Contains(i))continue;
                if(grass.TryGetValue(i,out var old)){if(!Tagged(old,token)){Suspend();Notify.Show("Grama propria indisponivel; cortes anteriores conservados.");return false;}continue;}
                var point=p.Options.Patches[i];var prop=World.CreateProp(grassModel,Pos(point),false,false);
                if(prop==null || !prop.Exists()){Suspend();Notify.Show("Grama nao carregou; contrato conservado. Retome para tentar novamente.");return false;}
                if(!Mark(prop)){prop.MarkAsNoLongerNeeded();Suspend();return false;}
                prop.Rotation=new Vector3(0,0,point.Heading);prop.IsPersistent=true;prop.IsPositionFrozen=true;prop.IsCollisionEnabled=false;grass[i]=prop;
                if(++created==8)return false;
            }
            return true;
        }
        void Route()
        {
            if(elapsed<blipAt)return;blipAt=elapsed+1200;var p=Pending;var origin=Driver?mower.Position:Game.Player.Character.Position;
            int n=Enumerable.Range(0,p.Options.Patches.Count).Where(i=>!p.Mowed.Contains(i)).OrderBy(i=>origin.DistanceTo2D(Pos(p.Options.Patches[i]))).FirstOrDefault();
            if(nearest==n && patchBlip!=null && patchBlip.Exists())return;nearest=n;
            if(patchBlip!=null && patchBlip.Exists())patchBlip.Delete();patchBlip=MapBlips.Create(Pos(p.Options.Patches[n]),"RPCore: grama para cortar",BlipSprite.Standard,BlipColor.Green,.7f);patchBlip.ShowRoute=Driver;
        }
        public void Tick()
        {
            Drain();if(!Active)return;
            if(Wallet.GangMowing?.Valid!=true || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Safe || !Owns || !Wallet.GangMowingOtherFree || Game.Player.Character.Position.DistanceTo(Pos(Pending.Options.Spawn))>300){Suspend();Notify.Show("Jardinagem suspensa. Cortes salvos; F4 retoma ou cancela.");return;}
            uint d=unchecked((uint)(Game.GameTime-last));last=Game.GameTime;if(!Input)return;if(d<=2000)elapsed+=d;var p=Pending;
            GTA.UI.Screen.ShowSubtitle("Mirror Park / grama "+p.Mowed.Count+"/"+p.Options.Patches.Count+" / END suspende",100);
            if(p.Ready){if(CanFinish)GTA.UI.Screen.ShowHelpTextThisFrame("E: confirmar jardinagem e liberar bonus2X");else GTA.UI.Screen.ShowHelpTextThisFrame("Saia do cortador parado e aproxime-se para confirmar E; ou volte a pe ao ponto inicial.");return;}
            if(!spawned){Prepare();return;}
            if(!OwnMower || mower.IsDead || !mower.IsDriveable || Foreign(mower) || Game.Player.Character.Position.DistanceTo(mower.Position)>150){Suspend();Notify.Show("Cortador indisponivel ou afastado. Pontos concluidos conservados.");return;}
            if(!Decorate())return;
            if(mowerBlip!=null && mowerBlip.Exists()){mowerBlip.Position=mower.Position;mowerBlip.ShowRoute=!Driver;}
            Route();if(patchBlip!=null && patchBlip.Exists())patchBlip.ShowRoute=Driver;
            if(nearest>=0){var pt=Pos(p.Options.Patches[nearest]);World.DrawMarker(MarkerType.Chevron1,pt+new Vector3(0,0,.6f),Vector3.Zero,Vector3.Zero,new Vector3(.5f,.5f,.5f),System.Drawing.Color.FromArgb(190,80,220,120));}
            if(!Driver){GTA.UI.Screen.ShowHelpTextThisFrame("Entre como motorista no cortador marcado. Somente ele corta a grama do contrato.");return;}
            var observed=Point(mower.Position);
            if(!GangPresenceRules.Finite(mower.Speed) || mower.Speed>p.Options.MaxSpeed)return;
            for(int i=0;i<p.Options.Patches.Count;i++)
            {
                if(p.Mowed.Contains(i))continue;var patch=p.Options.Patches[i];double dx=(double)observed.X-patch.X,dy=(double)observed.Y-patch.Y;
                if(dx*dx+dy*dy>p.Options.MowRadius*p.Options.MowRadius || Math.Abs(observed.Z-patch.Z)>p.Options.MaxHeight)continue;
                if(!Wallet.GangMowingCut(state(),Current,id,i,true,true,mower.Speed,observed,out bool saved) || !saved){Suspend();Notify.Show("Corte nao confirmado; ponto permanece para nova tentativa.");return;}
                if(grass.TryGetValue(i,out var prop)){if(Tagged(prop,token))prop.Delete();grass.Remove(i);}blipAt=0;nearest=-1;
                if(Pending.Ready){if(patchBlip!=null && patchBlip.Exists())patchBlip.Delete();patchBlip=null;if(mowerBlip!=null && mowerBlip.Exists())mowerBlip.ShowRoute=false;Notify.Show("Todos os pontos cortados! Pare, saia e E perto do cortador confirma bonus.");}return;
            }
        }
        bool CanFinish=>At(Pending.Options) || Safe && Game.Player.Character.IsOnFoot && OwnMower && !Foreign(mower) && mower.Speed<.5f && mower.Occupants.Length==0 && Game.Player.Character.Position.DistanceTo(mower.Position)<=4;
        public void Interact()
        {if(!Active || !Input || !Owns || !Wallet.GangMowingOtherFree || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Pending.Ready || !CanFinish)return;var c=Pending.Options;if(Wallet.GangMowingComplete(state(),Current,id,DateTime.UtcNow,out bool saved)){Suspend();Notify.Show(saved?"Jardinagem concluida! Automatico2X na area8 por"+c.BoostMinutes+"min reais; ative processamento separadamente.":"Contrato ja confirmado; bonus nao repetido.");}}
        void Drain()
        {
            for(int i=retiring.Count-1;i>=0;i--)
            {
                var r=retiring[i];if(!Tagged(r.Car,r.Token)){JobVehicles.Release(r.Car);retiring.RemoveAt(i);continue;}
                if(r.Car.Occupants.Length!=0 || r.Car.Speed>=.5f || Foreign(r.Car))continue;
                JobVehicles.Release(r.Car);r.Car.Delete();retiring.RemoveAt(i);
            }
        }
        public void Suspend()
        {
            foreach(var prop in grass.Values)if(Tagged(prop,token))prop.Delete();grass.Clear();
            if(mower!=null)retiring.Add(new Retiring{Car=mower,Token=token});mower=null;
            if(vehicleModel!=null)vehicleModel.MarkAsNoLongerNeeded();if(grassModel!=null)grassModel.MarkAsNoLongerNeeded();vehicleModel=grassModel=null;
            if(mowerBlip!=null && mowerBlip.Exists())mowerBlip.Delete();if(patchBlip!=null && patchBlip.Exists())patchBlip.Delete();mowerBlip=patchBlip=null;
            Active=false;profile=id=null;nearest=-1;gate.Release(Owner);Drain();
        }
        public void Dispose(){Suspend();foreach(var r in retiring){if(Tagged(r.Car,r.Token))r.Car.MarkAsNoLongerNeeded();JobVehicles.Release(r.Car);}retiring.Clear();}
    }
}
