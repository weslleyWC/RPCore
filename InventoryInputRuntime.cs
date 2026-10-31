using GTA;
using GTA.Native;
namespace RPCore
{
    public partial class InventorySystem
    {
        public void EnforceWeaponControls()
        {
            CleanupUseProps();
            var p=Game.Player.Character;if(p==null || !p.Exists())return;
            BlockNumberControls();
            if(!InventoryInputRules.BlockWheel(InterfaceOptions.HideWeaponWheel,p.IsHuman,!p.IsDead,p.IsInVehicle(),Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE,p),Game.IsPaused,Game.IsMissionActive,GameplayContext.SceneBlocking,Game.Player.CanControlCharacter))return;
            foreach(var c in new[]{GTA.Control.SelectWeapon,GTA.Control.SelectPrevWeapon,GTA.Control.SelectNextWeapon,GTA.Control.SelectWeaponUnarmed,GTA.Control.SelectWeaponMelee,GTA.Control.SelectWeaponAutoRifle,GTA.Control.SelectWeaponHandgun,GTA.Control.SelectWeaponHeavy,GTA.Control.SelectWeaponShotgun,GTA.Control.SelectWeaponSmg,GTA.Control.SelectWeaponSniper,GTA.Control.NextWeapon,GTA.Control.PrevWeapon})Game.DisableControlThisFrame(c);
            GTA.UI.Hud.HideComponentThisFrame(GTA.UI.HudComponent.WeaponWheel);
        }
    }
}
