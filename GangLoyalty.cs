using System;
using System.Linq;
using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
namespace RPCore
{
    internal sealed class GangLoyalty : IDisposable
    {
        const string Owner="gang_loyalty";readonly ShiftGate gate;readonly ObjectPool pool;readonly GangPresence presence;readonly Func<GangState> state;readonly Action<GangState> save;readonly GangLoyaltyOptions options;readonly Random random=new Random();
        bool active,loading;string profile;Ped contact,handoverPed;Model model;Blip route;int step=-1,requestAt,retryAt,handAt;bool handing;
        GangLoyaltyAttempt Pending=>state().Loyalty?.Pending;
        public bool Active=>active;
        public bool HasPending=>Pending!=null;
        bool Calm=>!Game.IsPaused && !Game.IsMissionActive && !Game.Player.Character.IsDead && !Game.Player.Character.IsInCombat && Game.Player.Wanted.WantedLevel==0 && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        static Vector3 Pos(GangSpot p)=>new Vector3(p.X,p.Y,p.Z);
        public GangLoyalty(ShiftGate gate,ObjectPool pool,GangPresence presence,Func<GangState> state,Action<GangState> save)
        {this.gate=gate;this.pool=pool;this.presence=presence;this.state=state;this.save=save;options=Store.LoadDef("Gangs/Loyalty.json",GangLoyaltyOptions.Defaults);if(options==null || !options.Valid){options=GangLoyaltyOptions.Defaults();Log.Write("Invalid loyalty definitions; embedded defaults used, original retained.");}}
        bool AtLeader(string gang){var leader=presence.Leader(gang);return Calm && !InterfaceOptions.ModalOpen && Game.Player.Character.IsOnFoot && leader!=null && Game.Player.Character.Position.DistanceTo(leader.Position)<=3;}
        public bool Begin(GangDefinition gang)
        {
            if(active || HasPending || state().Driving?.Pending!=null || !AtLeader(gang.Name) || !GangLoyaltyRules.Unaffiliated(state().Gang) || !gate.Acquire(Owner))return false;
            try{
                var candidates=options.Contacts.Where(a=>options.Contacts.Any(b=>GangPresenceRules.Distance(a,b)>=3)).ToArray();if(candidates.Length==0)return false;var first=candidates[random.Next(candidates.Length)];var others=options.Contacts.Where(p=>GangPresenceRules.Distance(p,first)>=3).ToArray();
                var leader=presence.Leader(gang.Name);var p=leader.Position;
                var attempt=new GangLoyaltyAttempt{Id=Guid.NewGuid().ToString("N"),Gang=gang.Name,Leader=GangPresenceRules.Position(gang.LeaderSpawnPostion)?new GangSpot{X=gang.LeaderSpawnPostion.X,Y=gang.LeaderSpawnPostion.Y,Z=gang.LeaderSpawnPostion.Z,Heading=gang.LeaderSpawnHeading}:new GangSpot{X=p.X,Y=p.Y,Z=p.Z,Heading=leader.Heading},Contacts=new System.Collections.Generic.List<GangSpot>{first,others[random.Next(others.Length)]}};
                if(!GangLoyaltyTransactions.Begin(state(),attempt,save,out _))return false;
                active=true;profile=Profile.Id;pool.HideAll();Route();return true;
            }finally{if(!active)gate.Release(Owner);}
        }
        public bool Resume()
        {if(active || Pending==null || state().Driving?.Pending!=null || state().Strength?.Pending!=null || state().Loyalty.Valid!=true || !GangLoyaltyRules.Unaffiliated(state().Gang) || !AtLeader(Pending.Gang) || !gate.Acquire(Owner))return false;active=true;profile=Profile.Id;pool.HideAll();Route();return true;}
        public bool Cancel(){if(Pending==null)return false;if(!GangLoyaltyTransactions.Cancel(state(),Pending.Id,save,out _))return false;Suspend();return true;}
        void ClearContact(){StopHandover();if(contact!=null && contact.Exists())contact.Delete();contact=null;if(model.Hash!=0)model.MarkAsNoLongerNeeded();model=default(Model);loading=false;}
        void StopHandover(){if(handing && handoverPed!=null && handoverPed.Exists())Function.Call(Hash.STOP_ANIM_TASK,handoverPed,"mp_common","givetake1_a",-4f);handing=false;handoverPed=null;}
        public void Suspend(){ClearContact();if(route!=null && route.Exists())route.Delete();route=null;active=false;profile=null;step=-1;gate.Release(Owner);}
        Vector3 Target=>Pending.Collected<2?Pos(Pending.Contacts[Pending.Collected]):Pos(Pending.Leader);
        void Route(){ClearContact();if(route!=null && route.Exists())route.Delete();route=World.CreateBlip(Target);route.Name="RPCore lealdade: "+(Pending.Collected<2?"pacote "+(Pending.Collected+1):"voltar ao lider");route.Sprite=BlipSprite.Package;route.Color=BlipColor.Yellow;route.ShowRoute=true;step=Pending.Collected;retryAt=0;}
        public void Interact()
        {
            if(!active || Pending==null || !Calm || InterfaceOptions.ModalOpen || pool.AreAnyVisible || !Game.Player.Character.IsOnFoot || handing)return;
            if(Pending.Collected==2){if(!AtLeader(Pending.Gang))return;if(!GangLoyaltyTransactions.Complete(state(),Pending.Id,save,out _))return;Suspend();Notify.Show("Iniciacao concluida. Filiacao gravada; bem-vindo a "+state().Gang+".");return;}
            if(contact==null || !contact.Exists() || contact.IsDead || Game.Player.Character.Position.DistanceTo(contact.Position)>2.5f)return;
            handAt=Game.GameTime;handing=true;handoverPed=Game.Player.Character;Function.Call(Hash.REQUEST_ANIM_DICT,"mp_common");
            if(Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED,"mp_common")){Function.Call(Hash.TASK_PLAY_ANIM,handoverPed,"mp_common","givetake1_a",4f,-4f,2500,0,0f,false,false,false);Function.Call(Hash.TASK_PLAY_ANIM,contact,"mp_common","givetake1_b",4f,-4f,2500,0,0f,false,false,false);}
        }
        public void Tick()
        {
            if(!active)return;
            if(profile!=Profile.Id || Pending==null || !state().PresenceEnabled || !GangLoyaltyRules.Unaffiliated(state().Gang)){Suspend();return;}
            if(Game.IsPaused){if(handing)handAt=Game.GameTime;return;}
            if(!Calm){Suspend();Notify.Show("Iniciacao suspensa; retome pelo F4 junto ao lider. Pacotes confirmados conservados.");return;}
            if(Pending.Collected!=step)Route();
            var player=Game.Player.Character;
            if(handing){
                float distance=contact!=null && contact.Exists() && !contact.IsDead?player.Position.DistanceTo(contact.Position):float.PositiveInfinity;
                bool eligible=player==handoverPed && player.IsOnFoot && !InterfaceOptions.ModalOpen && !pool.AreAnyVisible;
                if(!eligible || distance>2.5f){StopHandover();return;}
                if(GangLoyaltyRules.Handover(distance,unchecked(Game.GameTime-handAt)/1000f,eligible)){
                    if(!GangLoyaltyTransactions.Collect(state(),Pending.Id,Pending.Collected,save,out _))throw new InvalidOperationException("Loyalty pickup not saved.");Route();Notify.Show("Pacote confirmado: "+Pending.Collected+"/2.");
                }return;
            }
            if(Pending.Collected<2){
                float distance=player.Position.DistanceTo(Target);
                if(contact!=null && contact.Exists() && contact.IsDead){Cancel();Notify.Show("Contato morreu; iniciacao cancelada sem alterar filiacao.");return;}
                if(distance>100){ClearContact();}
                else if(distance<65 && (contact==null || !contact.Exists()) && unchecked(Game.GameTime-retryAt)>=0){
                    if(!loading){model=new Model("a_m_y_business_01");Function.Call(Hash.REQUEST_MODEL,model.Hash);Function.Call(Hash.REQUEST_COLLISION_AT_COORD,Target.X,Target.Y,Target.Z);loading=true;requestAt=Game.GameTime;}
                    if(unchecked(Game.GameTime-requestAt)>10000){ClearContact();retryAt=Game.GameTime+10000;}
                    else if(model.IsLoaded){
                        float floor;if(!World.GetGroundHeight(Target+new Vector3(0,0,2),out floor,GetGroundHeightMode.Normal) || Math.Abs(floor-Target.Z)>2 || !JobActors.Free(Target,2) || !GangPresence.Space(new Vector3(Target.X,Target.Y,floor)) || distance<4){ClearContact();retryAt=Game.GameTime+3000;}
                        else{contact=World.CreatePed(model,new Vector3(Target.X,Target.Y,floor+.05f),Pending.Contacts[Pending.Collected].Heading);if(contact==null || !contact.Exists())throw new InvalidOperationException("Loyalty contact unavailable.");contact.IsPersistent=true;contact.BlockPermanentEvents=true;contact.Weapons.RemoveAll();Function.Call(Hash.SET_PED_DROPS_WEAPONS_WHEN_DEAD,contact,false);Function.Call(Hash.SET_PED_MONEY,contact,0);contact.RelationshipGroup=new RelationshipGroup((int)RelationshipGroupHash.CivilianMale);contact.Task.StandStill(-1);model.MarkAsNoLongerNeeded();model=default(Model);loading=false;}
                    }
                }
                if(contact!=null && contact.Exists() && player.Position.DistanceTo(contact.Position)<=2.5f && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen)GTA.UI.Screen.ShowHelpTextThisFrame("E: receber pacote / permaneça perto por 2,5s");
            }else if(AtLeader(Pending.Gang) && !pool.AreAnyVisible)GTA.UI.Screen.ShowHelpTextThisFrame("E: entregar os dois pacotes e confirmar filiacao");
            GTA.UI.Screen.ShowSubtitle("Lealdade / "+Pending.Gang+" / pacotes "+Pending.Collected+"/2 / "+(Pending.Collected<2?"encontre o contato marcado":"volte ao lider")+" / END suspende",100);
            if(player.Position.DistanceTo(Target)<65)World.DrawMarker(MarkerType.Cylinder,Target-new Vector3(0,0,1),Vector3.Zero,Vector3.Zero,new Vector3(1.5f,1.5f,1),Color.FromArgb(100,150,80,220));
        }
        public void Dispose()=>Suspend();
    }
}
