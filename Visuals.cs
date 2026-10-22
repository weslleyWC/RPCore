using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using GTA;
using GTA.Native;
using LemonUI.Elements;
using LemonUI.Menus;

namespace RPCore
{
    public static class Visuals
    {
        class MenuSkin
        {
            public NativeMenu Menu;
            public string Key, Text;
            public string AppliedTexture;
            public I2Dimensional Original;
            public bool Applied;
            public readonly Dictionary<NativeItem, string> Items = new Dictionary<NativeItem, string>();
        }
        static readonly List<MenuSkin> menus = new List<MenuSkin>();
        static readonly HashSet<string> requested = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<string, int> retry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public static VisualCatalog Catalog = new VisualCatalog();
        public static VisualBindings Bindings = new VisualBindings();
        public static bool Enabled, NeedsHud, VehicleHud, WalletHud, MenuBanners, ItemIcons, UseProps, PhoneEnabled, Running;
        public static float Scale = 1, NeedsOffsetY = 12;
        public static System.Windows.Forms.Keys PhoneKey = System.Windows.Forms.Keys.F1;
        static bool failed;

        public static void Load()
        {
            var options = ScriptSettings.Load(Path.Combine(Cfg.Dir, "Visuals.ini"));
            Enabled = options.GetValue("Visuals", "Enabled", true);
            NeedsHud = options.GetValue("Visuals", "NeedsHud", true); VehicleHud = options.GetValue("Visuals", "VehicleHud", true);
            WalletHud = options.GetValue("Visuals", "WalletHud", true); MenuBanners = options.GetValue("Visuals", "MenuBanners", true);
            ItemIcons = options.GetValue("Visuals", "ItemIcons", true); UseProps = options.GetValue("Visuals", "ConsumableProps", true);
            PhoneEnabled = options.GetValue("Visuals", "Phone", true); PhoneKey = options.GetValue("Keys", "Phone", System.Windows.Forms.Keys.F1);
            Scale = options.GetValue("Layout", "Scale", 1f); if (float.IsNaN(Scale) || float.IsInfinity(Scale)) Scale = 1;
            Scale = Math.Max(.6f, Math.Min(1.4f, Scale));
            NeedsOffsetY = options.GetValue("Layout", "NeedsTop", 12f);
            if (float.IsNaN(NeedsOffsetY) || float.IsInfinity(NeedsOffsetY)) NeedsOffsetY = 12;
            if (NeedsOffsetY == 60) NeedsOffsetY = 12; // recognize the 0.3 default without rewriting its INI
            NeedsOffsetY = Math.Max(10, Math.Min(700, NeedsOffsetY));
            failed = false;
            try
            {
                Catalog = new JsonFiles().Read(Path.Combine(Cfg.Dir, "Visuals", "Catalog.json"), () => new VisualCatalog());
                Bindings = new JsonFiles().Read(Path.Combine(Cfg.Dir, "Visuals", "Bindings.json"), () => new VisualBindings());
            }
            catch (Exception ex) { Fail(ex); }
        }
        public static bool Active { get { return Running && Enabled && !failed; } }
        public static float Width { get { return HudMath.CanvasWidth(GTA.UI.Screen.AspectRatio); } }
        public static float InsetX { get { return HudMath.SafeInset(Width, Function.Call<float>(Hash.GET_SAFE_ZONE_SIZE)); } }
        public static float InsetY { get { return HudMath.SafeInset(1080, Function.Call<float>(Hash.GET_SAFE_ZONE_SIZE)); } }

        public static bool Ready(string dictionary)
        {
            if (!Active) return false;
            try
            {
                if (Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, dictionary)) return true;
                int next;
                if (!retry.TryGetValue(dictionary, out next) || unchecked(Game.GameTime - next) >= 0)
                {
                    Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, dictionary, false);
                    requested.Add(dictionary); retry[dictionary] = Game.GameTime + 1000;
                }
            }
            catch (Exception ex) { Fail(ex); }
            return false;
        }
        public static bool Has(string dictionary, string texture) { return Catalog.Find(dictionary, texture) != null && Ready(dictionary); }
        public static bool VehicleReady { get { return Active && VehicleHud && Has("PSRP", "RectangleBorder") && Has("PSRP", "CarFuelIcon"); } }
        public static bool Sprite(string dictionary, string texture, float x, float y, float w, float h, Color color, float rotation = 0)
        {
            if (!Has(dictionary, texture)) return false;
            // UI coordinates are physical-aspect 1080-high virtual pixels; sprites remain square on ultrawide screens.
            Function.Call(Hash.DRAW_SPRITE, dictionary, Catalog.Find(dictionary, texture).Name, (x + w / 2) / Width, (y + h / 2) / 1080, w / Width, h / 1080, rotation, color.R, color.G, color.B, color.A, false, 0);
            return true;
        }
        public static void Rect(float x, float y, float w, float h, Color color)
        {
            Function.Call(Hash.DRAW_RECT, (x + w / 2) / Width, (y + h / 2) / 1080, w / Width, h / 1080, color.R, color.G, color.B, color.A, false);
        }
        public static void Text(string text, float x, float y, float scale, Color color)
        {
            // Reuse SHVDN text rendering on its documented 720-high scaled canvas.
            new GTA.UI.TextElement(text, new PointF(x * 2 / 3, y * 2 / 3), scale, color).ScaledDraw();
        }
        public static void Register(NativeMenu menu, string key)
        {
            menus.Add(new MenuSkin { Menu = menu, Key = key, Original = menu.Banner, Text = menu.BannerText.Text });
        }
        public static void ClearItems(NativeMenu menu) { var skin = menus.Find(m => m.Menu == menu); skin?.Items.Clear(); }
        public static void Badge(NativeMenu menu, NativeItem item, string id)
        {
            var skin = menus.Find(m => m.Menu == menu);
            if (skin != null) skin.Items[item] = id;
        }
        public static void TickMenus()
        {
            foreach (var skin in menus)
            {
                if (!skin.Menu.Visible && !skin.Applied) continue;
                // The screenshot's personal inventory uses a blue handwritten header,
                // whereas ShopBanner is specifically the shop inventory artwork.
                if (skin.Key == "inventory" && Active && MenuBanners)
                {
                    if (!skin.Applied || skin.AppliedTexture != "inventory-blue")
                    {
                        skin.Menu.Banner = new ScaledRectangle(PointF.Empty, new SizeF(skin.Menu.Width, 108)) { Color = Color.FromArgb(255, 45, 100, 180) };
                        skin.Menu.BannerText.Text = "Inventory"; skin.Applied = true; skin.AppliedTexture = "inventory-blue";
                    }
                    ApplyBadges(skin); continue;
                }
                string texture = skin.Key == "reference-settings" ? ReferenceInterface.SettingsBanner : null;
                if (texture == null && Bindings.Banners != null) Bindings.Banners.TryGetValue(skin.Key, out texture);
                bool use = Active && MenuBanners && Has("PSRP_Banners", texture);
                if (use && (!skin.Applied || skin.AppliedTexture != texture))
                {
                    skin.Menu.Banner = new ScaledTexture(PointF.Empty, new SizeF(skin.Menu.Width, skin.Menu.Width / 4), "PSRP_Banners", texture);
                    skin.Menu.BannerText.Text = ""; skin.Applied = true; skin.AppliedTexture = texture;
                }
                else if (!use && skin.Applied) { skin.Menu.Banner = skin.Original; skin.Menu.BannerText.Text = skin.Text; skin.Applied = false; }
                ApplyBadges(skin);
            }
        }
        static void ApplyBadges(MenuSkin skin)
        {
            if (!skin.Menu.Visible) return;
            foreach (var item in skin.Items)
                {
                    string icon;
                    if (Active && ItemIcons && Bindings.ItemIcons != null && Bindings.ItemIcons.TryGetValue(item.Value, out icon) && Has("PSRP_Inventory", icon))
                    {
                        var badge = item.Key.LeftBadge as ScaledTexture;
                        if (badge == null || badge.Texture != icon) item.Key.LeftBadge = new ScaledTexture(PointF.Empty, SizeF.Empty, "PSRP_Inventory", icon);
                    }
                    else item.Key.LeftBadge = null;
                }
        }
        public static bool TryNeeds(float hunger, float thirst, float health, float armor, float stamina, float sleep)
        {
            try
            {
                if (!Active || !NeedsHud || !Has("PSRP", "OutBar100") || !Has("PSRP", "FoodIcon") || !Has("PSRP", "DrinkIcon")) return false;
                float unit = 47 * Scale, x = Width - InsetX - unit * 6 - 10, y = InsetY + NeedsOffsetY * Scale;
                Gauge("HealthIcon", health, x, y, Color.FromArgb(255, 245, 60, 75));
                Gauge("ArmorIcon", armor, x + unit, y, Color.FromArgb(255, 105, 80, 245));
                Gauge("DrinkVisual", thirst, x + unit * 2, y, Color.FromArgb(255, 0, 180, 245));
                Gauge("FoodIcon", hunger, x + unit * 3, y, Color.FromArgb(255, 245, 160, 50));
                Gauge("LungIcon", stamina, x + unit * 4, y, Color.FromArgb(255, 0, 210, 100));
                Gauge("SleepIcon", sleep, x + unit * 5, y, Color.FromArgb(255, 200, 210, 0));
                return true;
            }
            catch (Exception ex) { Fail(ex); return false; }
        }
        public static bool TryPizzaHeader(string status, Color color)
        {
            try
            {
                string banner;
                if (Bindings.Banners == null || !Bindings.Banners.TryGetValue("pizza", out banner)) banner = "PizzaBanner";
                if (!Active || !MenuBanners || !Has("PSRP_Banners", banner)) return false;
                float w = 320 * Scale, x = (Width - w) / 2, y = InsetY + 8;
                Sprite("PSRP_Banners", banner, x, y, w, w / 4, Color.White);
                Rect(x, y + w / 4, w, 30 * Scale, Color.FromArgb(180, 8, 12, 18));
                Text(status, x + 12 * Scale, y + w / 4 + 2, .29f, color); return true;
            }
            catch (Exception ex) { Fail(ex); return false; }
        }
        static void Gauge(string icon, float amount, float x, float y, Color color)
        {
            float size = 40 * Scale;
            Sprite("PSRP", "CircleWhite", x, y, size, size, Color.FromArgb(150, color));
            Sprite("PSRP", "OutBar" + HudMath.Percent(amount), x, y, size, size, color);
            Sprite("PSRP", icon, x + 2 * Scale, y + 2 * Scale, 36 * Scale, 36 * Scale, Color.White);
        }
        public static void Fail(Exception ex)
        {
            if (!failed) Log.Error("visual fallback enabled", ex);
            failed = true;
        }
        public static void Dispose()
        {
            Running = false;
            foreach (var skin in menus)
            {
                if (skin.Applied) { skin.Menu.Banner = skin.Original; skin.Menu.BannerText.Text = skin.Text; }
                foreach (var item in skin.Items.Keys) item.LeftBadge = null;
            }
            foreach (var dictionary in requested) Function.Call(Hash.SET_STREAMED_TEXTURE_DICT_AS_NO_LONGER_NEEDED, dictionary);
            menus.Clear(); requested.Clear(); retry.Clear();
        }
    }
}
