using GTA;
using GTA.Native;
namespace RPCore
{
    public static class LeisureContext
    {
        // Store.Load normally falls back after a protected read error. These screens need
        // to distinguish a missing save from a damaged existing save and stay read-only.
        public static T LoadProgress<T>(string name,System.Func<T> missing) where T:class
        {
            string path=System.IO.Path.Combine(Cfg.SaveDir,Profile.SafeId(Profile.Id),name);
            return System.IO.File.Exists(path)?Store.Load<T>(name,()=>null):missing();
        }
        public static bool CanOpen=>!Game.IsPaused && !Game.IsMissionActive && !InterfaceOptions.ModalOpen && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && Game.Player.CanControlCharacter && Profile.Detect()==Profile.Id && Game.Player.Character.IsAlive && Game.Player.Character.IsHuman && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && !Game.Player.Character.IsFalling;
    }
}
