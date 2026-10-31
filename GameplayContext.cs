using System;
using GTA;
using GTA.Native;
namespace RPCore
{
    // Loading/activating a scene is not the same as displaying it to the player.
    public static class GameplayContext
    {
        static int frame=-1;static bool active,playing,control;
        static readonly AreaWarmup warmup=new AreaWarmup();
        static void Sample()
        {
            int now=Game.FrameCount;if(now==frame)return;frame=now;
            active=Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
            playing=Function.Call<bool>(Hash.IS_CUTSCENE_PLAYING);
            control=Game.Player.CanControlCharacter;
        }
        public static bool Active{get{Sample();return active;}}
        public static bool Playing{get{Sample();return playing;}}
        public static bool Control{get{Sample();return control;}}
        public static bool SceneBlocking{get{Sample();return GameplayContextRules.SceneBlocks(active,playing,control);}}
        public static bool PreparedOnly{get{Sample();return GameplayContextRules.PreparedOnly(active,playing,control);}}
        public static bool HudHidden=>GameplayContextRules.HideHud(Game.IsPaused,SceneBlocking,Function.Call<bool>(Hash.IS_HUD_HIDDEN),PreparedOnly);
        public static bool TerrainReady=>warmup.Ready;
        // Ambient PDM entities wait for both settled terrain and an unblocked scene lifecycle.
        public static bool BackgroundReady=>TerrainReady && !SceneBlocking;
        public static void Update(Ped player,bool near)
        {
            var pos=player.Position;
            bool safe=near && !Game.IsPaused && !Game.IsMissionActive && !SceneBlocking && Control && player.IsAlive && GTA.UI.Screen.IsFadedIn && !Function.Call<bool>(Hash.IS_PLAYER_SWITCH_IN_PROGRESS);
            bool collision=safe && Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY,player);
            warmup.Update(Game.GameTime,player.Handle,Profile.Id,pos.X,pos.Y,pos.Z,near,safe,collision);
        }
    }
}
