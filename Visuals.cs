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
            public bool Applied, Failed;
            public readonly Dictionary<NativeItem, string> Items = new Dictionary<NativeItem, string>();
        }
        static readonly List<MenuSkin> menus = new List<MenuSkin>();
        static readonly HashSet<string> requested = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<string, int> retry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<string, int> requestedAt = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        static int bannerStarted = -1;
        public static VisualCatalog Catalog = new VisualCatalog();
        public static VisualBindings Bindings = new VisualBindings();
        public static bool Enabled, NeedsHud, VehicleHud, WalletHud, MenuBanners, ItemIcons, UseProps, PhoneEnabled, Running;
        public static float Scale = 1, NeedsOffsetY = 20;
        public static System.Windows.Forms.Keys PhoneKey = System.Windows.Forms.Keys.F2;
        static bool failed;

        public static void Load()
        {
            var options = ScriptSettings.Load(Path.Combine(Cfg.Dir, "Visuals.ini"));
            Enabled = options.GetValue("Visuals", "Enabled", true);
            NeedsHud = options.GetValue("Visuals", "NeedsHud", true); VehicleHud = options.GetValue("Visuals", "VehicleHud", true);
            WalletHud = options.GetValue("Visuals", "WalletHud", true); MenuBanners = options.GetValue("Visuals", "MenuBanners", true);
            ItemIcons = options.GetValue("Visuals", "ItemIcons", true); UseProps = options.GetValue("Visuals", "ConsumableProps", true);
            PhoneEnabled = options.GetValue("Visuals", "Phone", true); PhoneKey = options.GetValue("Keys", "Phone", System.Windows.Forms.Keys.F2);
            Scale = options.GetValue("Layout", "Scale", 1f); if (float.IsNaN(Scale) || float.IsInfinity(Scale)) Scale = 1;
            Scale = Math.Max(.6f, Math.Min(1.4f, Scale));
            NeedsOffsetY = options.GetValue("Layout", "NeedsTop", 20f);
            if (float.IsNaN(NeedsOffsetY) || float.IsInfinity(NeedsOffsetY)) NeedsOffsetY = 20;
            if (NeedsOffsetY == 60 || NeedsOffsetY == 12) NeedsOffsetY = 20; // recognize the previous defaults without rewriting their INIs
            NeedsOffsetY = Math.Max(10, Math.Min(700, NeedsOffsetY));
            failed = false;
            VisualCatalog localCatalog = null; VisualBindings localBindings = null;
            try { localCatalog = new JsonFiles().Read<VisualCatalog>(Path.Combine(Cfg.Dir,"Visuals","Catalog.json"),()=>null); }
            catch (Exception ex) { Log.Error("visual catalog override ignored; original file retained",ex); }
            try { localBindings = new JsonFiles().Read<VisualBindings>(Path.Combine(Cfg.Dir,"Visuals","Bindings.json"),()=>null); }
            catch (Exception ex) { Log.Error("visual bindings override ignored; original file retained",ex); }
            try
            {
                Catalog = VisualAssetDefaults.Catalog(localCatalog); Bindings = VisualAssetDefaults.Bindings(localBindings);
                Log.Write("VISUAL metadata: built-in defaults + local overrides; image source: GTA streamed YTDs in " + VisualAssetDefaults.GameArchive);
            }
            catch (Exception ex) { Fail(ex); }
        }
        public static bool Active { get { return Running && Enabled && !failed; } }
        public static float Width { get { return HudMath.CanvasWidth(GTA.UI.Screen.AspectRatio); } }
        public static float InsetX { get { return InterfaceOptions.RespectSafeZone ? HudMath.SafeInset(Width, Function.Call<float>(Hash.GET_SAFE_ZONE_SIZE)) : 0; } }
        public static float InsetY { get { return InterfaceOptions.RespectSafeZone ? HudMath.SafeInset(1080, Function.Call<float>(Hash.GET_SAFE_ZONE_SIZE)) : 0; } }

        public static bool Ready(string dictionary) { return Request(dictionary,false); }
        public static bool RequestForDiagnostic(string dictionary) { return Request(dictionary,true); }
        public static bool RequestTimedOut(string dictionary)
        { int at; return requestedAt.TryGetValue(dictionary,out at) && unchecked(Game.GameTime-at)>10000; }
        static bool Request(string dictionary,bool force)
        {
            if (!Active && !force) return false;
            try
            {
                if (Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, dictionary)) return true;
                int next;
                if (!retry.TryGetValue(dictionary, out next) || unchecked(Game.GameTime - next) >= 0)
                {
                    Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, dictionary, true); // matches LemonUI 2.2 ScaledTexture.Request
                    if (!requestedAt.ContainsKey(dictionary)) requestedAt[dictionary]=Game.GameTime;
                    requested.Add(dictionary); retry[dictionary] = Game.GameTime + 1000;
                }
            }
            catch (Exception ex) { Fail(ex); }
            return false;
        }
        public static bool Has(string dictionary, string texture) { return Catalog.Find(dictionary, texture) != null && Ready(dictionary); }
        public static bool VehicleReady { get { return Active && VehicleHud && Has("PSRP", "CarFuelIcon"); } }
        public static void SpriteVisible(string dictionary, string texture, RectangleF visible, RectangleF alphaBounds, Color color)
        {
            var info = Catalog.Find(dictionary, texture); if (info == null) return;
            var full = ReferenceLayout.FitTexture(visible, alphaBounds, info.Width, info.Height);
            Sprite(dictionary, texture, full.X, full.Y, full.Width, full.Height, color);
        }
        public static void Label(string text, float x, float y, float scale, Color color, GTA.UI.Font font = GTA.UI.Font.ChaletLondon, GTA.UI.Alignment alignment = GTA.UI.Alignment.Left, float wrap = 0)
        {
            new ScaledText(new PointF(x, y), text ?? "", scale, font) { Color = color, Alignment = alignment, WordWrap = wrap }.Draw();
        }
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
            bool animate = InterfaceOptions.AnimateSettingsBanner && menus.Exists(s=>s.Key=="reference-settings" && s.Menu.Visible);
            if (!animate) bannerStarted=-1;
            else if (bannerStarted<0) bannerStarted=Game.GameTime;
            foreach (var skin in menus)
            {
                if(skin.Failed)continue;
                try{
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
                if(skin.Key=="pdm" && Active && MenuBanners && (Bindings.Banners==null || !Bindings.Banners.TryGetValue("pdm",out var customPdmBanner) || customPdmBanner=="PSRPBANNER2")){
                    const string brand="shopui_title_premium_deluxe_motorsport";
                    bool loaded=Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED,brand);
                    if(!loaded && (!retry.TryGetValue(brand,out int due) || unchecked(Game.GameTime-due)>=0)){
                        Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT,brand,true);requested.Add(brand);retry[brand]=Game.GameTime+1000;
                    }
                    if(loaded){
                        if(!skin.Applied || skin.AppliedTexture!="pdm-brand"){skin.Menu.Banner=new ScaledTexture(PointF.Empty,new SizeF(skin.Menu.Width,skin.Menu.Width/4),brand,brand);skin.Menu.BannerText.Text="";skin.Applied=true;skin.AppliedTexture="pdm-brand";}
                        ApplyBadges(skin);continue;
                    }
                }
                string texture = skin.Key=="reference-actions" ? ReferenceInterface.SettingsBanner : skin.Key == "reference-settings" ? (animate ? ReferenceInterface.AnimatedSettingsBanner(unchecked(Game.GameTime-bannerStarted)) : ReferenceInterface.SettingsBanner) : null;
                if (texture == null && Bindings.Banners != null) Bindings.Banners.TryGetValue(skin.Key, out texture);
                if(texture==null && skin.Key=="insurance" && (Bindings.Banners==null || !Bindings.Banners.ContainsKey("insurance")))texture="InsuranceBanner";
                bool use = Active && MenuBanners && Has("PSRP_Banners", texture);
                if (use && (!skin.Applied || skin.AppliedTexture != texture))
                {
                    float height=skin.Key=="reference-settings" ? 128 : skin.Menu.Width/4;
                    skin.Menu.Banner = new ScaledTexture(PointF.Empty, new SizeF(skin.Menu.Width,height), "PSRP_Banners", texture);
                    skin.Menu.BannerText.Text = ""; skin.Applied = true; skin.AppliedTexture = texture;
                }
                else if (!use && skin.Applied) { skin.Menu.Banner = skin.Original; skin.Menu.BannerText.Text = skin.Text; skin.Applied = false; }
                ApplyBadges(skin);
                }catch(Exception ex){
                    skin.Failed=true;Log.Error("menu skin: "+skin.Key,ex);
                    // Leave this menu usable with its normal banner; never disable all HUD rendering.
                    try{skin.Menu.Banner=skin.Original;skin.Menu.BannerText.Text=skin.Text;skin.Applied=false;}catch(Exception cleanup){Log.Error("menu skin restore: "+skin.Key,cleanup);}
                }
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
                if (!Active || !NeedsHud || !Has("PSRP", "OutBar100") || !Has("PSRP", "FoodIcon") || !Has("PSRP", "DrinkVisual") || !Has("PSRP","HealthIcon") || !Has("PSRP","ArmorIcon") || !Has("PSRP","LungIcon") || !Has("PSRP","SleepIcon") || !Has("PSRP","CircleWhite")) return false;
                float unit = 47 * Scale, x = ReferenceLayout.StatusX(Width,Scale,InsetX), y = InsetY + NeedsOffsetY * Scale;
                Gauge("HealthIcon", health, x, y, Color.FromArgb(255,255,51,51));
                Gauge("ArmorIcon", armor, x + unit, y, Color.FromArgb(255,102,102,255));
                Gauge("DrinkVisual", thirst, x + unit * 2, y, Color.FromArgb(255,0,174,255));
                Gauge("FoodIcon", hunger, x + unit * 3, y, Color.FromArgb(255,255,153,51));
                Gauge("LungIcon", stamina, x + unit * 4, y, Color.FromArgb(255,0,204,102));
                Gauge("SleepIcon", sleep, x + unit * 5, y, Color.FromArgb(255,196,183,2));
                return true;
            }
            catch (Exception ex) { Fail(ex); return false; }
        }
        public static bool TryPizzaHeader(string status, Color color) => TryJobHeader("pizza", status, color);
        public static bool TryJobHeader(string key, string status, Color color)
        {
            try
            {
                string banner;
                if (Bindings.Banners == null || !Bindings.Banners.TryGetValue(key, out banner)) banner = key == "pdm" ? "PSRPBANNER2" : key == "drag" ? "DragBanner" : key == "streamer" ? "PSRPBANNER1" : key == "dealer" ? "DrugDealingBanner" : key == "crafting" ? "ShopBanner" : key == "fishing" ? "ShopBanner" : key == "hunting" ? "ButcherBanner" : key == "pilot" ? "PSRPBANNER1" : key == "prison" ? "PSRPBANNER1" : key == "tanker" ? "PlaceholderBanner" : key == "supply" ? "PostOpBanner" : key == "postop" ? "PostOpBanner" : key == "uber" ? "BoltBanner" : key == "valet" ? "Valet" : "PizzaBanner";
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
            Sprite("PSRP", "CircleWhite", x, y, size, size, color);
            Sprite("PSRP", "OutBar" + HudMath.Percent(amount), x, y, size, size, Color.FromArgb(255, Math.Min(255, color.R + 70), Math.Min(255, color.G + 70), Math.Min(255, color.B + 70)));
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
            menus.Clear(); requested.Clear(); retry.Clear(); requestedAt.Clear(); bannerStarted=-1;
        }
    }
}
