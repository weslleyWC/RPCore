using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
namespace RPCore
{
    internal sealed class GangStrength : IDisposable
    {
        const string Owner="gang_strength";readonly ShiftGate gate;readonly ObjectPool pool;readonly GangPresence presence;readonly Func<GangState> state;readonly Action<GangState> save;readonly GangStrengthOptions options;
        readonly List<Ped> fighters=new List<Ped>();readonly List<Blip> blips=new List<Blip>();Model[] models;GangStrengthAttempt draft;string profile;bool active,preparing;int started,last;float seconds;Ped player;Blip reportRoute;
        GangStrengthAttempt Pending=>state().Strength?.Pending;
        public bool Active=>active || preparing;
        public bool HasPending=>Pending!=null;
        bool Safe=>Game.Player.Character.IsHuman && !Game.IsMissionActive && !Game.Player.Character.IsDead && Game.Player.Wanted.WantedLevel==0 && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool Unarmed=>Game.Player.Character.Weapons.Current.Hash==WeaponHash.Unarmed;
        static Vector3 Pos(GangSpot p)=>new Vector3(p.X,p.Y,p.Z);
        bool AtLeader(string gang){var leader=presence.Leader(gang);return Safe && !Game.IsPaused && !InterfaceOptions.ModalOpen && !Game.Player.Character.IsInCombat && Game.Player.Character.IsOnFoot && leader!=null && Game.Player.Character.Position.DistanceTo(leader.Position)<=3;}
        public GangStrength(ShiftGate gate,ObjectPool pool,GangPresence presence,Func<GangState> state,Action<GangState> save)
        {this.gate=gate;this.pool=pool;this.presence=presence;this.state=state;this.save=save;options=Store.LoadDef("Gangs/Strength.json",()=>new GangStrengthOptions());if(options==null || !options.Valid){options=new GangStrengthOptions();Log.Write("Invalid strength options; original retained, defaults used.");}}
        public bool Begin(GangDefinition gang)
        {
            if(Active || HasPending || state().Driving?.Pending!=null || state().Loyalty?.Pending!=null || !GangLoyaltyRules.Unaffiliated(state().Gang) || !AtLeader(gang.Name) || !Unarmed || Game.Player.Character.Health<=100)return false;
            var choices=(gang.PedModels??Array.Empty<string>()).Where(GangPresenceRules.ModelName).ToArray();if(choices.Length==0)return false;
            var p=Game.Player.Character.Position;var attempt=new GangStrengthAttempt{Id=Guid.NewGuid().ToString("N"),Gang=gang.Name,Center=new GangSpot{X=p.X,Y=p.Y,Z=p.Z},Models=Enumerable.Range(0,3).Select(i=>choices[i%choices.Length]).ToArray(),Options=CharacterRules.Copy(options)};
            return Prepare(attempt);
        }
        public bool Resume()
        {
            if(Active || Pending==null || state().Driving?.Pending!=null || state().Strength.Valid!=true || state().Loyalty?.Pending!=null || !GangLoyaltyRules.Unaffiliated(state().Gang) || !AtLeader(Pending.Gang))return false;
            if(Pending.Report){if(!gate.Acquire(Owner))return false;active=true;profile=Profile.Id;pool.HideAll();ReportRoute();return true;}
            if(!Unarmed || Game.Player.Character.Health<=100 || Game.Player.Character.Position.DistanceTo(Pos(Pending.Center))>Pending.Options.ArenaRadius-8)return false;
            return Prepare(CharacterRules.Copy(Pending));
        }
        bool Prepare(GangStrengthAttempt p)
        {
            if(!p.Valid || !gate.Acquire(Owner))return false;draft=p;profile=Profile.Id;player=Game.Player.Character;models=p.Models.Select(n=>new Model(n)).ToArray();
            if(models.Any(m=>!m.IsInCdImage || !m.IsPed)){Suspend();return false;}
            foreach(var m in models)Function.Call(Hash.REQUEST_MODEL,m.Hash);preparing=true;started=Game.GameTime;pool.HideAll();Notify.Show("Preparando tres desafiantes. Aguarde desarmado e a pe; nenhuma luta inicia antes da equipe pronta.");return true;
        }
        Vector3? Spot(int index)
        {
            var center=Pos(draft.Center);var offsets=new[]{new Vector3(4,0,0),new Vector3(-3,3,0),new Vector3(-3,-3,0)};
            for(int n=0;n<8;n++){
                double angle=n*Math.PI/4;var o=offsets[index];var p=center+new Vector3((float)(o.X*Math.Cos(angle)-o.Y*Math.Sin(angle)),(float)(o.X*Math.Sin(angle)+o.Y*Math.Cos(angle)),0);
                float z;if(!World.GetGroundHeight(p+new Vector3(0,0,2),out z,GetGroundHeightMode.Normal) || Math.Abs(z-p.Z)>2 || !JobActors.Free(p,2) || !GangPresence.Space(new Vector3(p.X,p.Y,z)))continue;
                if(fighters.Any(f=>f.Position.DistanceTo(p)<2.5f))continue;return new Vector3(p.X,p.Y,z+.05f);
            }return null;
        }
        void Spawn()
        {
            for(int i=0;i<3;i++){
                var spot=Spot(i);if(!spot.HasValue){Suspend();Notify.Show("Area da luta sem tres vagas seguras; escolha piso aberto junto ao lider.");return;}
                var f=World.CreatePed(models[i],spot.Value);if(f==null || !f.Exists())throw new InvalidOperationException("Strength fighter unavailable.");fighters.Add(f);
                if(!f.IsHuman)throw new InvalidOperationException("Strength fighter is not human.");
                f.IsPersistent=true;f.BlockPermanentEvents=true;f.MaxHealth=Math.Max(200,draft.Options.FighterHealth);f.Health=draft.Options.FighterHealth;f.Armor=0;f.Weapons.RemoveAll();f.RelationshipGroup=new RelationshipGroup((int)RelationshipGroupHash.CivilianMale);f.Task.StandStill(-1);
                Function.Call(Hash.SET_PED_DROPS_WEAPONS_WHEN_DEAD,f,false);Function.Call(Hash.SET_PED_MONEY,f,0);Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES,f,(int)CombatAttributes.CanCommandeerVehicles,false);Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES,f,46,true);Function.Call(Hash.SET_PED_COMBAT_ABILITY,f,1);
                var b=f.AddBlip();blips.Add(b);b.Name="RPCore: desafiante "+(i+1);b.Sprite=BlipSprite.Enemy;b.Color=BlipColor.Red;
            }
            if(Pending==null){if(!GangStrengthTransactions.Begin(state(),draft,save,out _))throw new InvalidOperationException("Strength attempt not saved.");}
            else if(!GangStrengthTransactions.Restart(state(),Pending.Id,save,out _))throw new InvalidOperationException("Strength restart not saved.");
            preparing=false;active=true;last=Game.GameTime;seconds=0;
            foreach(var f in fighters){Function.Call(Hash.CLEAR_ENTITY_LAST_DAMAGE_ENTITY,f);Function.Call(Hash.TASK_COMBAT_PED,f,player,0,16);}ReleaseModels();
            Notify.Show("Enfrente os tres usando apenas os punhos. Vida acima de 100, a pe e dentro da area. END suspende.");
        }
        void ReportRoute()
        {
            var leader=presence.Leader(Pending.Gang);var point=leader!=null?leader.Position:Pos(Pending.Center);
            if(reportRoute==null || !reportRoute.Exists()){reportRoute=World.CreateBlip(point);reportRoute.Name="RPCore: voltar ao lider / forca";reportRoute.Color=BlipColor.Yellow;reportRoute.ShowRoute=true;}
            else reportRoute.Position=point;
        }
        void ReleaseModels(){if(models!=null)foreach(var m in models)m.MarkAsNoLongerNeeded();models=null;}
        void ClearFighters(){foreach(var b in blips)if(b!=null && b.Exists())b.Delete();blips.Clear();foreach(var f in fighters)if(f!=null && f.Exists())f.Delete();fighters.Clear();ReleaseModels();}
        public void Suspend(){if(reportRoute!=null && reportRoute.Exists())reportRoute.Delete();reportRoute=null;ClearFighters();draft=null;player=null;profile=null;active=preparing=false;gate.Release(Owner);}
        public bool Cancel(){if(Pending==null)return false;if(!GangStrengthTransactions.End(state(),Pending.Id,false,save,out _))return false;Suspend();return true;}
        void Fail(string reason){if(Pending!=null && !GangStrengthTransactions.End(state(),Pending.Id,true,save,out _))throw new InvalidOperationException("Strength failure not saved.");Suspend();Notify.Show("Iniciacao reprovada: "+reason+". Filiacao anterior conservada.");}
        public void Interact(){if(!active || Pending?.Report!=true || !AtLeader(Pending.Gang) || pool.AreAnyVisible)return;if(!GangStrengthTransactions.Complete(state(),Pending.Id,save,out _))throw new InvalidOperationException("Strength membership not saved.");Suspend();Notify.Show("Prova de forca concluida. Filiacao gravada em "+state().Gang+".");}
        public void Tick()
        {
            if(!Active)return;
            if(profile!=Profile.Id || !state().PresenceEnabled || !GangLoyaltyRules.Unaffiliated(state().Gang)){Suspend();return;}
            if(Game.IsPaused){last=Game.GameTime;return;}
            if(preparing){
                if(!Safe || !Unarmed || !Game.Player.Character.IsOnFoot || player!=Game.Player.Character || player.IsInCombat || player.Position.DistanceTo(Pos(draft.Center))>(Pending==null?3:draft.Options.ArenaRadius-8) || unchecked(Game.GameTime-started)>10000){Suspend();Notify.Show("Preparacao interrompida, sem nova tentativa iniciada.");return;}
                if(models.All(m=>m.IsLoaded))Spawn();return;
            }
            if(Pending==null){Suspend();return;}
            if(Pending.Report){if(!Safe){Suspend();return;}ReportRoute();if(AtLeader(Pending.Gang) && !pool.AreAnyVisible)GTA.UI.Screen.ShowHelpTextThisFrame("E: confirmar filiacao apos a prova de forca");return;}
            seconds+=Math.Min(1,Math.Max(0,unchecked(Game.GameTime-last)/1000f));last=Game.GameTime;
            if(player!=Game.Player.Character || !GangStrengthRules.PlayerCanFight(player.Health,player.Position.DistanceTo(Pos(Pending.Center)),Unarmed,player.IsOnFoot,Safe,Pending.Options) || seconds>=Pending.Options.SecondsLimit){Fail("vida, arma, veiculo, procurado, tempo ou abandono da area");return;}
            for(int i=0;i<3;i++){
                if(Pending.Defeated.Contains(i))continue;var f=fighters[i];if(f==null || !f.Exists()){Fail("desafiante desapareceu");return;}
                bool hit=Function.Call<bool>(Hash.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY,f,player,true);
                bool down=f.IsDead || f.Health<=Pending.Options.YieldHealth;
                if(down){
                    if(!GangStrengthRules.Defeated(f.Health,f.IsDead,hit,Unarmed,Pending.Options)){Fail("desafiante vencido sem golpe do jogador nesta avaliacao");return;}
                    // Stop this owned fighter immediately; only publish the knockout after the checkpoint saves.
                    f.Task.ClearAll();f.BlockPermanentEvents=true;if(!f.IsDead){Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES,f,46,false);Function.Call(Hash.TASK_HANDS_UP,f,60000,player,-1,true);}
                    if(!GangStrengthTransactions.Defeat(state(),Pending.Id,i,save,out _))throw new InvalidOperationException("Strength defeat not saved.");
                    if(blips[i]!=null && blips[i].Exists())blips[i].Delete();
                }
                Function.Call(Hash.CLEAR_ENTITY_LAST_DAMAGE_ENTITY,f);
            }
            if(Pending.Report){ClearFighters();ReportRoute();Notify.Show("Tres desafiantes vencidos. Volte ao lider e pressione E para confirmar filiacao.");return;}
            GTA.UI.Screen.ShowSubtitle("Forca / "+Pending.Gang+" / "+Pending.Defeated.Count+"/3 / apenas punhos / area "+Pending.Options.ArenaRadius+"m / END suspende",100);
        }
        public void Dispose()=>Suspend();
    }
}
