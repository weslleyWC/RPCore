using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace RPCore
{
    // Names/dimensions/bindings only. Image bytes continue to come from the game's mounted YTDs.
    public static class VisualAssetDefaults
    {
        public const string GameArchive = @"mods\update\update.rpf\x64\data\cdimages\scaleform_generic.rpf";
        static T Read<T>(string name)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RPCore.VisualDefaults." + name + ".json"))
            {
                if (stream == null) throw new InvalidDataException("Missing built-in visual metadata: " + name);
                using (var reader = new StreamReader(stream)) return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
            }
        }
        public static VisualCatalog Catalog(VisualCatalog custom = null)
        {
            var result = Read<VisualCatalog>("Catalog");
            if (custom?.Dictionaries != null) foreach (var pair in custom.Dictionaries)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null) continue;
                string dictionary = result.Dictionaries.Keys.FirstOrDefault(k => string.Equals(k,pair.Key,StringComparison.OrdinalIgnoreCase)) ?? pair.Key;
                List<TextureInfo> old;
                if (!result.Dictionaries.TryGetValue(dictionary,out old)) old = new List<TextureInfo>();
                var entries = old.Where(t=>t!=null).ToDictionary(t=>t.Name,StringComparer.OrdinalIgnoreCase);
                foreach (var t in pair.Value.Where(t=>t!=null && !string.IsNullOrWhiteSpace(t.Name) && t.Width>0 && t.Height>0)) entries[t.Name]=t;
                result.Dictionaries[dictionary]=entries.Values.ToList();
            }
            if (custom?.Models != null) result.Models = result.Models.Concat(custom.Models.Where(m=>!string.IsNullOrWhiteSpace(m))).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return result;
        }
        public static VisualBindings Bindings(VisualBindings custom = null)
        {
            var result = Read<VisualBindings>("Bindings");
            result.Banners = Merge(result.Banners,custom?.Banners);
            result.ItemIcons = Merge(result.ItemIcons,custom?.ItemIcons);
            result.ItemProps = Merge(result.ItemProps,custom?.ItemProps);
            return result;
        }
        static Dictionary<string,T> Merge<T>(Dictionary<string,T> original,Dictionary<string,T> custom)
        {
            var result = new Dictionary<string,T>(original,StringComparer.OrdinalIgnoreCase);
            if (custom!=null) foreach(var pair in custom) result[pair.Key]=pair.Value;
            return result;
        }
    }
    public class TextureProbe
    {
        public string Dictionary, Texture, Error;
        public int ExpectedWidth, ExpectedHeight;
        public float Width, Height;
        public bool DictionaryLoaded;
        public string Status
        {
            get
            {
                if (Error!=null) return "query_error";
                if (!DictionaryLoaded) return "dictionary_not_loaded";
                if (float.IsNaN(Width)||float.IsNaN(Height)||float.IsInfinity(Width)||float.IsInfinity(Height)||Width<=0||Height<=0) return "invalid_resolution";
                return Math.Abs(Width-ExpectedWidth)<.5f && Math.Abs(Height-ExpectedHeight)<.5f ? "dimensions_match" : "different_resolution";
            }
        }
    }
    public class VisualDiagnosticReport
    {
        public string Version = "0.24.0", Archive = VisualAssetDefaults.GameArchive;
        public bool Enabled, Active, NeedsHud, WalletHud, VehicleHud, PhoneEnabled, MenuBanners;
        public float AspectRatio;
        public List<TextureProbe> Textures = new List<TextureProbe>();
        public Dictionary<string,bool> Dictionaries = new Dictionary<string,bool>();
        public Dictionary<string,bool> Models = new Dictionary<string,bool>();
        public string Limits = "Native dictionary/resolution checks only. Matching dimensions do not prove matching pixels or correct screen composition.";
    }
}
