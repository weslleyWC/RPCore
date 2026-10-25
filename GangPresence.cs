using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
namespace RPCore
{
    internal sealed partial class GangPresence : IDisposable
    {
        sealed class Actor { public Ped Ped;public string Gang;public Blip Blip;public Vector3 Post;public int NextReturn;public bool Returning,Leader,Crew,Waiting,Departing;public Vector3 WaitPost;public Vehicle Boarding;public int Seat=-2,BoardUntil,NextTask,Formation,DepartAt; }
        readonly Dictionary<string,Actor> actors=new Dictionary<string,Actor>();readonly Dictionary<string,int> retry=new Dictionary<string,int>();
        readonly GangPresenceOptions options;Model model;GangPost pending;string pendingKey,pendingGang;int requested,next;string profile;
        public GangPresence(){options=Store.LoadDef("Gangs/Presence.json",()=>new GangPresenceOptions());if(options==null || !options.Valid){Log.Write("Invalid Gangs/Presence.json; original retained, defaults used.");options=new GangPresenceOptions();}profile=Profile.Id;}
        static Vector3 Pos(GangSpot p)=>new Vector3(p.X,p.Y,p.Z);
        public int Count=>actors.Count;
        public int Posts(GangDefinition g)=>GangPresenceRules.Plan(g,options.PerHQ).Count;
        void ReleasePending(){if(model.Hash!=0)model.MarkAsNoLongerNeeded();model=default(Model);pending=null;pendingKey=pendingGang=null;}
        static void Delete(Actor a){if(a.Blip!=null && a.Blip.Exists())a.Blip.Delete();if(a.Ped!=null && a.Ped.Exists())a.Ped.Delete();}
        static bool Space(Vector3 pos)
        {
            foreach(float height in new[]{.5f,1.3f})foreach(var axis in new[]{new Vector3(.45f,0,0),new Vector3(0,.45f,0)}){
                var center=pos+new Vector3(0,0,height);var probe=ShapeTest.StartExpensiveSyncTestLOSProbe(center-axis,center+axis,IntersectFlags.Map|IntersectFlags.Objects);
                ShapeTestResult hit;if(probe.GetResult(out hit)!=ShapeTestStatus.Ready || hit.DidHit)return false;
            }return true;
        }
        public void Clear(){ReleasePending();foreach(var a in actors.Values)Delete(a);actors.Clear();retry.Clear();}
        public void Tick(IEnumerable<GangDefinition> gangs,bool enabled,string affiliation)
        {
            if(profile!=Profile.Id){Clear();profile=Profile.Id;}
            if(Game.IsPaused)return;
            var player=Game.Player.Character;
            if(!enabled || player.IsDead || Game.IsMissionActive || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) ){Clear();return;}
            bool canSpawn=!player.IsInCombat && Game.Player.Wanted.WantedLevel==0;
            int now=Game.GameTime;if(unchecked(now-next)<0)return;next=now+500;
            var near=gangs.Where(g=>GangPresenceRules.Position(GangPresenceRules.HQ(g))).OrderBy(g=>player.Position.DistanceTo(g.Position)).ToList();
            foreach(var pair in actors.ToArray()){
                var g=near.Find(x=>x.Name==pair.Value.Gang);var a=pair.Value;
                bool dead=a.Ped==null || !a.Ped.Exists() || a.Ped.IsDead;
                if(a.Crew && !dead && GangCrewRules.Keep(affiliation,a.Gang,player.Position.DistanceTo(a.Ped.Position),true)){UpdateCrew(a,now);continue;}
                if(a.Crew){Delete(a);actors.Remove(pair.Key);retry[pair.Key]=now+options.RespawnSeconds*1000;continue;}
                if(a.Departing && !dead && a.Ped.IsInVehicle()){
                    if(unchecked(now-a.DepartAt)>=60000){if(a.Blip!=null && a.Blip.Exists())a.Blip.Delete();a.Ped.MarkAsNoLongerNeeded();actors.Remove(pair.Key);retry[pair.Key]=now+options.RespawnSeconds*1000;continue;}
                    if(GangCrewRules.CanExit(a.Ped.CurrentVehicle.Speed) && unchecked(now-a.NextTask)>=0){JobActors.Leave(a.Ped,a.Ped.CurrentVehicle);a.NextTask=now+6000;}continue;
                }
                a.Departing=false;
                if(dead || g==null || !GangPresenceRules.Keep(player.Position.DistanceTo(g.Position),true,options) || a.Ped.Position.DistanceTo(g.Position)>options.DespawnRadius){Delete(a);actors.Remove(pair.Key);if(dead)retry[pair.Key]=now+options.RespawnSeconds*1000;}
                else if(!a.Ped.IsInCombat && !a.Ped.IsInVehicle()){
                    float distance=a.Ped.Position.DistanceTo(a.Post);
                    if(a.Returning && distance<1.5f){a.Ped.Task.StandStill(-1);a.Returning=false;}
                    else if(distance>6 && unchecked(now-a.NextReturn)>=0){JobActors.Walk(a.Ped,a.Post);a.NextReturn=now+15000;a.Returning=true;}
                }
            }
            if(!canSpawn){ReleasePending();return;}
            if(pending!=null){
                var g=near.Find(x=>x.Name==pendingGang);
                if(g==null || !GangPresenceRules.Keep(player.Position.DistanceTo(g.Position),false,options) || unchecked(now-requested)>10000){retry[pendingKey]=now+30000;ReleasePending();return;}
                if(!model.IsLoaded)return;
                try{
                    var pos=Pos(pending.Spot);float ground;bool floor=World.GetGroundHeight(pos+new Vector3(0,0,2),out ground,GetGroundHeightMode.Normal);
                    if(!floor || float.IsNaN(ground) || Math.Abs(ground-pos.Z)>2 || player.Position.DistanceTo(pos)<4 || !JobActors.Free(pos,2) || !Space(new Vector3(pos.X,pos.Y,ground))){retry[pendingKey]=now+3000;return;}
                    var ped=World.CreatePed(model,new Vector3(pos.X,pos.Y,ground+.05f),pending.Spot.Heading);
                    if(ped==null || !ped.Exists()){retry[pendingKey]=now+30000;return;}
                    // Only this owned ped gets a civilian group; no relationship matrix or vanilla actors are changed.
                    var actor=new Actor{Ped=ped,Gang=pendingGang,Leader=pending.Leader,Post=new Vector3(pos.X,pos.Y,ground)};actors.Add(pendingKey,actor);
                    if(!ped.IsHuman){Delete(actor);actors.Remove(pendingKey);retry[pendingKey]=now+60000;return;}
                    ped.IsPersistent=true;ped.Weapons.RemoveAll();Function.Call(Hash.SET_PED_DROPS_WEAPONS_WHEN_DEAD,ped,false);ped.RelationshipGroup=new RelationshipGroup((int)RelationshipGroupHash.CivilianMale);ped.Task.StandStill(-1);
                    if(pending.Leader){actor.Blip=MapBlips.Create(ped.Position,"RPCore: lider / "+pendingGang,BlipSprite.Friend,BlipColor.White,.5f);}
                }finally{ReleasePending();}return;
            }
            if(actors.Count>=options.MaxActors)return;
            foreach(var g in near){
                if(!GangPresenceRules.Keep(player.Position.DistanceTo(g.Position),false,options))continue;
                foreach(var post in GangPresenceRules.Plan(g,options.PerHQ)){
                    string key=g.Name+"\n"+post.Key;int wait;if(actors.ContainsKey(key) || retry.TryGetValue(key,out wait) && unchecked(now-wait)<0)continue;
                    model=new Model(post.Model);
                    if(!model.IsValid || !model.IsInCdImage || !model.IsPed){retry[key]=now+60000;model.MarkAsNoLongerNeeded();model=default(Model);continue;}
                    pending=post;pendingKey=key;pendingGang=g.Name;requested=now;Function.Call(Hash.REQUEST_MODEL,model.Hash);Function.Call(Hash.REQUEST_COLLISION_AT_COORD,post.Spot.X,post.Spot.Y,post.Spot.Z);return;
                }
            }
        }
        public void Dispose()=>Clear();
    }
}
