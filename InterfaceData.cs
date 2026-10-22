using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;

namespace RPCore
{
    public static class ReferenceLayout
    {
        public const int InventoryColumns = 5, InventoryRows = 5, InventoryPageSize = 25;
        public static readonly RectangleF InventoryBackgroundBounds = new RectangleF(107, 126, 853, 758);
        public static readonly RectangleF CellBounds = new RectangleF(11, 11, 106, 106);
        public static readonly RectangleF PhoneBounds = new RectangleF(1662, 621, 242, 441);
        public static readonly string[] PhoneIcons = { "ContactIcon", "MessagesIcon", "NotificationsIcon", "UberIcon", "NotesIcon", "FitnessIcon", "ClockIcon", "MusicIcon", "CalculatorIcon", "IconSearch", "SettingsIcon" };
        public static readonly string[] PhoneLabels = { "Contacts", "Messages", "Notifications", "Uber", "Notes", "Fitness", "Clock", "Radio", "Calculator", "Search", "Settings" };
        public static float StatusX(float width, float scale, float inset) { return width-inset-10-275*scale; }
        public static RectangleF Cell(int localIndex)
        { return new RectangleF(34 + localIndex % 5 * 166, 164 + localIndex / 5 * 166, 156, 156); }
        public static RectangleF FitTexture(RectangleF visible, RectangleF bounds, float textureWidth, float textureHeight)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0 || visible.Width <= 0 || visible.Height <= 0) return RectangleF.Empty;
            float sx = visible.Width / bounds.Width, sy = visible.Height / bounds.Height;
            return new RectangleF(visible.X - bounds.X * sx, visible.Y - bounds.Y * sy, textureWidth * sx, textureHeight * sy);
        }
        public static float Speed(float metersPerSecond, bool metric) { return Math.Max(0, metersPerSecond) * (metric ? 3.6f : 2.2369363f); }
    }

    public static class InventoryLayout
    {
        // Layout is supplementary: item counts and containers remain in the same save and transaction.
        public static List<string> Reconcile(IEnumerable<string> saved, IEnumerable<string> itemIds)
        {
            var ids = new HashSet<string>(itemIds.Where(id => !string.IsNullOrEmpty(id)), StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var slots = (saved ?? Enumerable.Empty<string>()).Take(Math.Max(25, ids.Count * 25)).Select(id => id != null && ids.Contains(id) && seen.Add(id) ? id : null).ToList();
            foreach (var id in ids.OrderBy(id => id, StringComparer.Ordinal))
            {
                if (seen.Contains(id)) continue;
                int gap = slots.IndexOf(null); if (gap >= 0) slots[gap] = id; else slots.Add(id); seen.Add(id);
            }
            while (slots.Count > 25 && slots.Skip(slots.Count - 25).All(id => id == null) && slots.Count % 25 == 0) slots.RemoveRange(slots.Count - 25, 25);
            while (slots.Count < 25 || slots.Count % 25 != 0) slots.Add(null);
            return slots;
        }
        public static bool Swap(List<string> slots, int a, int b)
        {
            if (slots == null || a < 0 || b < 0 || a >= slots.Count || b >= slots.Count || a == b) return false;
            string item = slots[a]; slots[a] = slots[b]; slots[b] = item; return true;
        }
    }
    public class PhoneEntry : ExtensibleData { public string Title, Body; public bool Read; }
    public class PhoneData : ExtensibleData
    {
        public List<string> Notes = new List<string>();
        public List<PhoneEntry> History = new List<PhoneEntry>();
        public int Wallpaper = 3;
    }
    public static class PhoneMath
    {
        // Two operands and one operator, no scripting/eval or network execution.
        public static bool Calculate(string input, out string answer)
        {
            answer = "Invalid expression";
            if (string.IsNullOrWhiteSpace(input)) return false;
            input = input.Replace(" ", "").Replace(',', '.');
            for (int i = 1; i < input.Length; i++)
            {
                char op = input[i]; if (op != '+' && op != '-' && op != '*' && op != '/') continue;
                double a, b;
                if (!double.TryParse(input.Substring(0, i), NumberStyles.Float, CultureInfo.InvariantCulture, out a) || !double.TryParse(input.Substring(i + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out b)) continue;
                double value = op == '+' ? a + b : op == '-' ? a - b : op == '*' ? a * b : b == 0 ? double.NaN : a / b;
                if (double.IsNaN(value) || double.IsInfinity(value)) { answer = "Undefined / overflow"; return false; }
                answer = value.ToString("G10", CultureInfo.InvariantCulture); return true;
            }
            return false;
        }
    }
}
