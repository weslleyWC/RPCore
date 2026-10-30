using System.IO;
using System.Windows.Forms;
using GTA;

namespace RPCore
{
    // Separate configuration avoids overwriting users' existing Settings/Keys/Visuals files.
    public static class InterfaceOptions
    {
        public static Keys RadialKey = Keys.J, SettingsKey = Keys.F11;
        public static bool RadialEnabled = true, HideRadarOnFoot;
        public static bool GridInventory = true, MetricSpeed, RespectSafeZone, LocationHud = true;
        public static bool ModalOpen;
        public static bool AdvancedMenus;
        public static bool HideWeaponWheel = true;
        public static bool AnimateSettingsBanner = true;
        public static bool CharacterSelectionOnStart = true;
        static string PathName { get { return Path.Combine(Cfg.Dir, "Interface.ini"); } }
        public static void Load()
        {
            var s = ScriptSettings.Load(PathName);
            CharacterSelectionOnStart=s.GetValue("Interface","CharacterSelectionOnStart",true);
            AdvancedMenus=s.GetValue("Interface","AdvancedMenus",false);
            HideWeaponWheel=s.GetValue("Interface","HideWeaponWheel",true);
            RadialKey = s.GetValue("Keys", "Radial", Keys.J); SettingsKey = s.GetValue("Keys", "Settings", Keys.F11);
            RadialEnabled = s.GetValue("Interface", "Radial", true); HideRadarOnFoot = s.GetValue("Interface", "HideRadarOnFoot", false);
            GridInventory = s.GetValue("Interface", "GridInventory", true);
            MetricSpeed = s.GetValue("VehicleHud", "Metric", false); LocationHud = s.GetValue("VehicleHud", "Location", true);
            RespectSafeZone = s.GetValue("Layout", "RespectSafeZone", false);
            AnimateSettingsBanner = s.GetValue("Interface", "AnimateSettingsBanner", true);
        }
        public static void Set<T>(string file, string section, string key, T value)
        {
            MenuFileRules.Write(Path.Combine(Cfg.Dir,file),temp=>{var s=ScriptSettings.Load(temp);s.SetValue(section,key,value);return s.Save();});
        }
    }
}
