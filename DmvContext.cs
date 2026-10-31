using System.Linq;
using GTA;
using GTA.Native;
namespace RPCore
{
    public sealed partial class DmvSystem
    {
        readonly AreaWarmup localTerrain=new AreaWarmup();bool localSurfaceReady;
        DmvAccessBlock MenuBlock=>DmvAccessRules.Menu(Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.Player.Character.IsRagdoll && !Game.Player.Character.IsFalling && !Game.Player.Character.IsSwimming && !Game.Player.Character.IsClimbing,Game.Player.CanControlCharacter,Profile.Detect()==Profile.Id,GameplayContext.Active,GameplayContext.Playing,Game.IsPaused,Game.IsMissionActive,InterfaceOptions.ModalOpen,Game.Player.Character.IsInCombat,Game.Player.Wanted.WantedLevel);
        void SampleLocalTerrain()
        {
            var p=Game.Player.Character;bool near=preparing || drive!=null || new[]{DmvClass.C,DmvClass.M1,DmvClass.A}.Any(c=>p.Position.DistanceTo(Pos(Desk(c)))<120);
            bool switching=Function.Call<bool>(Hash.IS_PLAYER_SWITCH_IN_PROGRESS);
            bool collision=near && Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY,p);
            localSurfaceReady=DmvAccessRules.DrivingReady(Game.Player.CanControlCharacter,GameplayContext.SceneBlocking,GTA.UI.Screen.IsFadedIn,switching,collision);
            var at=p.Position;localTerrain.Update(Game.GameTime,p.Handle,Profile.Id,at.X,at.Y,at.Z,near,localSurfaceReady && !Game.IsPaused && !Game.IsMissionActive && p.IsAlive && Profile.Detect()==Profile.Id,collision);
        }
        bool PrepareHere()
        {
            SampleLocalTerrain();
            if(DmvAccessRules.CanPrepare(MenuBlock,!Game.Player.Character.IsInVehicle(),Game.Player.Character.Position.DistanceTo(Pos(options.Entry)),localTerrain.Ready))return true;
            if(!CheckDesk())return false;Notify.Show("DMV: aguarde alguns segundos para o piso local carregar. Nenhuma taxa nova.");return false;
        }
    }
}
