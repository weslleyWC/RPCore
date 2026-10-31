using GTA;
namespace RPCore
{
    internal static class PdmCustomerInput
    {
        static readonly PdmCustomerPulse pulse=new PdmCustomerPulse();
        public static void BeginFrame()=>pulse.Sample(Game.FrameCount,Game.IsKeyPressed(Cfg.PdmInteractionKey),Game.LastInputMethod==InputMethod.GamePad && PdmMenuRuntime.Pressed(Control.Context));
        public static void BlockFrame()=>pulse.Block();
        public static bool Pressed{get{BeginFrame();return pulse.Take(true);}}
        public static string Button=>Game.LastInputMethod==InputMethod.GamePad?"~INPUT_CONTEXT~":Cfg.PdmInteractionKey.ToString();
    }
}
