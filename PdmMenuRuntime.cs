using GTA;
using GTA.Native;
namespace RPCore
{
    internal static class PdmMenuRuntime
    {
        // UI validity only. Each business action keeps its own office/contract/payment guards.
        public static bool Allowed=>!Game.IsPaused && !Game.IsMissionActive && !InterfaceOptions.ModalOpen && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && Game.Player.CanControlCharacter && Game.Player.Character.IsAlive && Profile.Detect()==Profile.Id;
    }
}
