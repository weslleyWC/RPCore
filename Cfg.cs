using System.IO;
using System.Windows.Forms;
using GTA;

namespace RPCore
{
    public static class Cfg
    {
        public static string Dir = Path.Combine("scripts", "RPCore");
        public static string SaveDir { get { return Path.Combine(Dir, "SaveData"); } }

        public static Keys InventoryKey, JobKey, DumpKey, AchKey, SeatbeltKey;
        public static Keys CoreKey, StorageKey, StocksKey, GangsKey, RadioKey;
        public static string CustomProfile;
        public static bool EnableGangs, EnableRadio;
        public static float HungerPerMin, ThirstPerMin, SprintMult, MaxWeight, HudX, HudY;
        public static int AutosaveMs;
        public static int PizzaBase, PizzaOnTimeBonus, PizzaShiftBonus, PizzaDrops;
        public static float PizzaPerMeter, PizzaMinDist, PizzaMaxDist;

        static T Get<T>(ScriptSettings s, string sec, string key, T def)
        {
            T v = s.GetValue(sec, key, def);
            s.SetValue(sec, key, v); // writes defaults back so the ini is self-documenting
            return v;
        }

        public static void Load()
        {
            Directory.CreateDirectory(SaveDir);
            string path = Path.Combine(Dir, "Settings.ini");
            bool create = !File.Exists(path);
            if (create) File.WriteAllText(path, "");
            ScriptSettings s = ScriptSettings.Load(path);

            InventoryKey = Get(s, "Keys", "Inventory", Keys.I);
            JobKey       = Get(s, "Keys", "PizzaShift", Keys.F6);
            DumpKey      = Get(s, "Keys", "DumpPosition", Keys.F9);
            AchKey       = Get(s, "Keys", "Achievements", Keys.F7);
            SeatbeltKey  = Get(s, "Keys", "Seatbelt", Keys.B);
            CoreKey = Get(s, "Keys", "CoreMenu", Keys.F10);
            StorageKey = Get(s, "Keys", "Storage", Keys.F8);
            StocksKey = Get(s, "Keys", "Stocks", Keys.F5);
            GangsKey = Get(s, "Keys", "Gangs", Keys.F4);
            RadioKey = Get(s, "Keys", "Radio", Keys.F3);
            CustomProfile = Profile.SafeId(Get(s, "Core", "CustomProfile", "Default"));
            string selected = Path.Combine(Dir, "ActiveProfile.json");
            if (File.Exists(selected))
            {
                try { CustomProfile = Profile.SafeId(new JsonFiles().Read<string>(selected, () => CustomProfile)); }
                catch (System.Exception ex) { Log.Error("active profile", ex); }
            }
            EnableGangs = Get(s, "Features", "Gangs", true);
            EnableRadio = Get(s, "Features", "Radio", true);
            string keysPath = Path.Combine(Dir, "Keys.ini");
            if (File.Exists(keysPath))
            {
                var keys = ScriptSettings.Load(keysPath);
                InventoryKey = keys.GetValue("SETTINGS", "Inventory Key", InventoryKey);
                SeatbeltKey = keys.GetValue("SETTINGS", "Seatbelt Key", SeatbeltKey);
            }

            HungerPerMin = Get(s, "Needs", "HungerPerMin", 1.1f);
            ThirstPerMin = Get(s, "Needs", "ThirstPerMin", 1.6f);
            SprintMult   = Get(s, "Needs", "SprintMult", 2.2f);
            HudX         = Get(s, "Needs", "HudX", 14f);
            HudY         = Get(s, "Needs", "HudY", 14f);

            MaxWeight    = Get(s, "Inventory", "MaxWeightKg", 30f);
            AutosaveMs   = Get(s, "Core", "AutosaveMs", 60000);

            PizzaDrops       = Get(s, "Pizza", "DropsPerShift", 5);
            PizzaBase        = Get(s, "Pizza", "BasePay", 40);
            PizzaPerMeter    = Get(s, "Pizza", "PayPerMeter", 0.08f);
            PizzaOnTimeBonus = Get(s, "Pizza", "OnTimeBonus", 25);
            PizzaShiftBonus  = Get(s, "Pizza", "ShiftBonus", 150);
            PizzaMinDist     = Get(s, "Pizza", "MinDistance", 350f);
            PizzaMaxDist     = Get(s, "Pizza", "MaxDistance", 900f);

            HungerPerMin = Positive(HungerPerMin, 1.1f);
            ThirstPerMin = Positive(ThirstPerMin, 1.6f);
            SprintMult = Positive(SprintMult, 2.2f);
            MaxWeight = Positive(MaxWeight, 30f);
            AutosaveMs = System.Math.Max(5000, AutosaveMs);
            PizzaDrops = System.Math.Max(1, System.Math.Min(100, PizzaDrops));
            PizzaMinDist = System.Math.Max(50, Positive(PizzaMinDist, 350));
            PizzaMaxDist = System.Math.Max(PizzaMinDist, Positive(PizzaMaxDist, 900));
            PizzaBase = System.Math.Max(0, PizzaBase);
            PizzaOnTimeBonus = System.Math.Max(0, PizzaOnTimeBonus);
            PizzaShiftBonus = System.Math.Max(0, PizzaShiftBonus);
            PizzaPerMeter = Positive(PizzaPerMeter, 0.08f);
            if (create) s.Save();
        }

        static float Positive(float v, float fallback) { return float.IsNaN(v) || float.IsInfinity(v) || v < 0 ? fallback : v; }
    }
}
