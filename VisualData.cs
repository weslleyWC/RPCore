using System;
using System.Collections.Generic;
using System.Linq;

namespace RPCore
{
    public static class ReferenceInterface
    {
        public static readonly string[] RadialLabels = { "Phone", "Inventory", "Emotes", "Vehicle Control", "Fast GPS", "Gangs", "Interactions", "Settings" };
        public static readonly string[] RadialIcons = { "PhoneIconRad", "InventoryIconRad", "EmoteIconRad", "CarIconRad", "GpsIconRad", "GangIconRad", "InteractionIconRad", "SettingsIconRad" };
        public const string SectorDictionary = "PSRP_Inventory", SettingsBanner = "PSRPBANNER2";
    }
    public class TextureInfo { public string Name; public int Width, Height; }
    public class VisualCatalog
    {
        public Dictionary<string, List<TextureInfo>> Dictionaries = new Dictionary<string, List<TextureInfo>>();
        public List<string> Models = new List<string>();
        public TextureInfo Find(string dictionary, string texture)
        {
            if (Dictionaries == null || string.IsNullOrEmpty(dictionary) || string.IsNullOrEmpty(texture)) return null;
            var entry = Dictionaries.FirstOrDefault(p => string.Equals(p.Key, dictionary, StringComparison.OrdinalIgnoreCase));
            return entry.Value?.FirstOrDefault(t => t != null && t.Width > 0 && t.Height > 0 && string.Equals(t.Name, texture, StringComparison.OrdinalIgnoreCase));
        }
    }
    public class PropBinding
    {
        public string Model, Fallback;
        public bool Drink;
        public float X = .12f, Y = .02f, Z = -.02f, RX = -90, RY, RZ;
    }
    public class VisualBindings
    {
        public Dictionary<string, string> ItemIcons = new Dictionary<string, string>();
        public Dictionary<string, PropBinding> ItemProps = new Dictionary<string, PropBinding>();
        public Dictionary<string, string> Banners = new Dictionary<string, string>();
    }
    public static class HudMath
    {
        public static string CompactCash(long value) { return value < 1000 ? value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) : CompactMoney(value); }
        public static int MoveRadial(int index, int delta) { return ((index + delta) % 8 + 8) % 8; }
        public static string CompactMoney(long value)
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            if (value >= 1000000000) return (value / 1000000000d).ToString("0.#", culture) + "B";
            if (value >= 1000000) return (value / 1000000d).ToString("0.#", culture) + "M";
            if (value >= 1000) return (value / 1000d).ToString("0.#", culture) + "K";
            return value.ToString(culture);
        }
        public static int Percent(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0 : (int)Math.Round(Math.Max(0, Math.Min(100, value)));
        }
        public static float CanvasWidth(float aspectRatio)
        {
            return float.IsNaN(aspectRatio) || float.IsInfinity(aspectRatio) ? 1920 : 1080 * Math.Max(1, Math.Min(4, aspectRatio));
        }
        public static float SafeInset(float dimension, float safeZone)
        {
            if (float.IsNaN(safeZone) || float.IsInfinity(safeZone)) safeZone = 1;
            return dimension * (1 - Math.Max(.8f, Math.Min(1, safeZone))) / 2;
        }
        public static int MoveGrid(int index, int dx, int dy, int columns, int count)
        {
            if (columns <= 0 || count <= 0) return 0;
            index = Math.Max(0, Math.Min(count - 1, index));
            int candidate = index + dx + dy * columns;
            return candidate >= 0 && candidate < count && (dy != 0 || candidate / columns == index / columns) ? candidate : index;
        }
    }
}
