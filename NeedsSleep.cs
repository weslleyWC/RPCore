using System;
using GTA;
using GTA.Math;
using GTA.Native;
namespace RPCore
{
    public partial class NeedsSystem
    {
        const string SleepOwner="needs_sleep";
        readonly NativeBedSleepMeter bedSleep=new NativeBedSleepMeter();
        readonly Func<bool> menusOpen;
        static readonly Vector3[] homes={new Vector3(-9,-1441,31),new Vector3(8,536,176),new Vector3(-813,179,72),new Vector3(1973,3817,33),new Vector3(-1151,-1518,10)};
        Ped sleeper;string sleeperProfile;bool sleeping;int sleepRetry,sleepSave,bedClockLast,ignoreBedClock,vehicleGrace;
        public bool Sleeping=>sleeping;
        public void IgnoreNativeBedClock(){ignoreBedClock=Game.GameTime+250;bedSleep.Reset();}
        void ResetSleep()
        {StopSleeping();bedSleep.Reset();bedClockLast=Game.GameTime;ignoreBedClock=sleepRetry=vehicleGrace=Game.GameTime;sleepSave=0;}
        bool SleepSafe(Ped p)=>SleepRecoveryRules.Safe(p.IsHuman,p.IsAlive,p.IsOnFoot,p.IsInVehicle(),Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE,p),p.IsInWater,p.IsInAir || p.IsFalling || p.IsClimbing,p.IsInCombat,p.IsRagdoll,Game.IsMissionActive || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE),Game.Player.Wanted.WantedLevel>0,InterfaceOptions.ModalOpen || (menusOpen?.Invoke()??false),Function.Call<bool>(Hash.IS_PLAYER_CONTROL_ON,Game.Player),p.Speed);
        void TrySleep(Ped p)
        {
            if(unchecked(Game.GameTime-sleepRetry)<0 || unchecked(Game.GameTime-vehicleGrace)<0 || !SleepSafe(p) || gate!=null && gate.Owner!=null)return;
            if(gate!=null && !gate.Acquire(SleepOwner))return;
            try
            {
                if(d.SleepRecovery==null && !SleepRecoveryRules.Begin(ref d,Guid.NewGuid().ToString("N"),n=>Store.Write("Needs.json",n))){gate?.Release(SleepOwner);return;}
                if(d.SleepRecovery?.Valid!=true){gate?.Release(SleepOwner);return;}
                if(p!=Game.Player.Character || Profile.Detect()!=Profile.Id){gate?.Release(SleepOwner);return;}
                sleeper=p;sleeperProfile=Profile.Id;sleeping=true;resting=false;sleepSave=0;
                // One bounded native ragdoll per run; never repeatedly knock the player down.
                if(d.SleepRecovery.Remaining>0)p.Ragdoll(d.SleepRecovery.Remaining);
                Notify.Show("Voce desmaiou de sono. Aguarde alguns segundos para se recuperar.");
            }
            catch(Exception ex){StopSleeping();sleepRetry=Game.GameTime+5000;Log.Error("sleep begin retained for retry",ex);}
        }
        void SleepTick(Ped p,int delta)
        {
            if(sleeperProfile!=Profile.Id || sleeper!=p || p.IsDead || p.IsInVehicle() || Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE,p) || p.IsInWater || p.IsInCombat || Game.IsMissionActive || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) || Game.Player.Wanted.WantedLevel>0 || gate!=null && gate.Owner!=SleepOwner)
            {if(sleeperProfile==Profile.Id)try{Save();}catch(Exception ex){Log.Error("sleep suspend",ex);}StopSleeping();return;}
            SleepRecoveryRules.Advance(d,delta,true);sleepSave+=delta;
            if(d.SleepRecovery?.Valid!=true){StopSleeping();return;}
            if(d.SleepRecovery.Remaining==0)
            {
                if(unchecked(Game.GameTime-sleepRetry)<0)return;
                try{if(SleepRecoveryRules.Wake(ref d,n=>Store.Write("Needs.json",n))){StopSleeping();wasSprinting=lowSleep=false;hurtClock=0;Notify.Show("Voce acordou descansado. Sono e folego cheios.");}}
                catch(Exception ex){StopSleeping();sleepRetry=Game.GameTime+5000;Log.Error("sleep wake pending save",ex);}
                return;
            }
            GTA.UI.Screen.ShowSubtitle("Desmaiado de sono... "+((d.SleepRecovery.Remaining+999)/1000)+"s");
            if(sleepSave>=5000){sleepSave=0;try{Save();}catch(Exception ex){Log.Error("sleep checkpoint",ex);}}
        }
        void StopSleeping(){sleeping=false;sleeper=null;sleeperProfile=null;gate?.Release(SleepOwner);}
        void NativeBedTick(int now)
        {
            int elapsed=unchecked(now-bedClockLast);bedClockLast=now;var p=Game.Player.Character;
            if(p==null || !p.Exists()){bedSleep.Reset();return;}
            if(p.IsInVehicle() || Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE,p))vehicleGrace=now+3000;
            bool near=false;foreach(var home in homes)if(p.Position.DistanceTo(home)<40 && Math.Abs(p.Position.Z-home.Z)<18){near=true;break;}
            bool scene=Function.Call<bool>(Hash.IS_SCREEN_FADED_OUT) || Function.Call<bool>(Hash.IS_SCREEN_FADING_OUT) || Function.Call<bool>(Hash.IS_SCREEN_FADING_IN) || !Function.Call<bool>(Hash.IS_PLAYER_CONTROL_ON,Game.Player);
            int minute=GTA.Chrono.GameClock.Hour*60+GTA.Chrono.GameClock.Minute;
            bool excluded=p.IsDead || p.IsInVehicle() || Game.IsMissionActive || gate!=null && gate.Owner!=null || unchecked(now-ignoreBedClock)<0;
            if(!bedSleep.Observe(minute,elapsed,near,scene,excluded))return;
            try{if(SleepRecoveryRules.Wake(ref d,n=>Store.Write("Needs.json",n),true)){wasSprinting=lowSleep=false;hurtClock=0;Notify.Show("Descanso na cama reconhecido. Sono e folego recuperados.");}}
            catch(Exception ex){Log.Error("native bed recovery",ex);}
        }
    }
}
