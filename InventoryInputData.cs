namespace RPCore
{
    // The input policy must not depend on save validity or successful ammunition scans.
    public static class InventoryInputRules
    {
        public static bool BlockWheel(bool enabled,bool human,bool alive,bool vehicle,bool boarding,bool paused,bool mission,bool cutscene,bool control)
            =>enabled && human && alive && !vehicle && !boarding && !paused && !mission && !cutscene && control;
    }
}
