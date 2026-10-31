using GTA;
using GTA.Native;

namespace RPCore
{
    public partial class InventorySystem
    {
        bool weightWarned;
        public bool Overweight=>InventoryBurdenRules.MoveRate(Weight,Cfg.MaxWeight)<1;
        void BurdenTick()
        {
            var p=Game.Player.Character;if(p==null || !p.Exists())return;
            float rate=InventoryBurdenRules.MoveRate(Weight,Cfg.MaxWeight);
            if(rate>=1)
            {
                if(weightWarned){weightWarned=false;if(!p.IsDead && !Game.IsPaused && Profile.Detect()==Profile.Id)Notify.Show("Peso normalizado. Limitacao de carga removida.");}
                return;
            }
            if(!InventoryBurdenRules.Safe(p.IsHuman,!p.IsDead,p.IsOnFoot,p.IsInVehicle(),Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE,p),p.IsSwimming,Function.Call<bool>(Hash.IS_ENTITY_IN_AIR,p) || p.IsFalling,p.IsRagdoll,p.IsClimbing,Game.IsPaused,Game.IsMissionActive,GameplayContext.SceneBlocking,Game.Player.CanControlCharacter,Profile.Detect()==Profile.Id))return;
            if(!weightWarned){weightWarned=true;Notify.Show("Sobrepeso: movimento reduzido, sem sprint/salto. Guarde itens no deposito ou porta-malas.");}
            Game.DisableControlThisFrame(GTA.Control.Sprint);
            Game.DisableControlThisFrame(GTA.Control.Jump);
            // This native is a frame override. Stop calling when unburdened or unsafe;
            // never overwrite a global run/swim multiplier or enable another system's controls.
            Function.Call(Hash.SET_PED_MOVE_RATE_OVERRIDE,p,rate);
        }
    }
}
