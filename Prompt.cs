using GTA;
using GTA.Native;

namespace RPCore
{
    public static class Prompt
    {
        // One-frame help text in the top-left. Call every frame while it should show.
        public static void Help(string text)
        {
            Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_HELP, "CELL_EMAIL_BCON");
            Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text);
            Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_HELP, 0, false, false, -1);
        }
    }
}
