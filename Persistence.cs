using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RPCore
{
    // A failed read protects its file for the rest of the session. Never autosave defaults over a damaged save.
    public sealed class JsonFiles
    {
        readonly HashSet<string> blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public T Read<T>(string path, Func<T> fallback)
        {
            if (!File.Exists(path)) return fallback();
            try
            {
                var value = JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
                if (value == null) throw new InvalidDataException("JSON null: " + path);
                return value;
            }
            catch { blocked.Add(Path.GetFullPath(path)); throw; }
        }

        public void Write(string path, object data)
        {
            path = Path.GetFullPath(path);
            if (blocked.Contains(path)) throw new IOException("Save protected after failed read: " + path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temp, JsonConvert.SerializeObject(data, Formatting.Indented));
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
                else File.Move(temp, path);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        public void Protect(string path) { blocked.Add(Path.GetFullPath(path)); }
    }

    public class ExtensibleData
    {
        [JsonExtensionData] public IDictionary<string, JToken> Extra;
    }

    public static class LegacyImport
    {
        // Copy old RPCore flat saves once, into the first active profile; leave originals untouched.
        public static void MigrateFlat(string root, string profile)
        {
            var marker = Path.Combine(root, "FlatMigration.json");
            if (File.Exists(marker)) return;
            var files = new JsonFiles();
            foreach (string name in new[] { "Inventory.json", "Needs.json", "PizzaStats.json" })
            {
                var source = Path.Combine(root, name);
                var target = Path.Combine(root, profile, name);
                if (!File.Exists(source) || File.Exists(target)) continue;
                var value = files.Read<JObject>(source, () => new JObject());
                files.Write(target, value);
            }
            files.Write(marker, new { Profile = profile, ImportedUtc = DateTime.UtcNow });
        }

        public static void ImportPlayer(string source, string targetDirectory)
        {
            if (Directory.Exists(targetDirectory)) throw new IOException("Profile already exists; import cannot overwrite it.");
            var files = new JsonFiles();
            var player = files.Read<JObject>(source, () => { throw new FileNotFoundException(source); });
            // Retain ALL unknown PSRP fields as an immutable copy; never write back to the source.
            string stage = targetDirectory + ".import-" + Guid.NewGuid().ToString("N");
            try
            {
                Directory.CreateDirectory(stage);
                File.Copy(source, Path.Combine(stage, "PSRP.original.json"));
                files.Write(Path.Combine(stage, "Needs.json"), new { Hunger = Percent(player, "Hunger"), Thirst = Percent(player, "Hydration") });
                files.Write(Path.Combine(stage, "Wallet.json"), new { Cash = Math.Max(0, (int?)player["PlayerMoneyBalance"] ?? 0), Bank = Math.Max(0, (int?)player["PlayerBankBalance"] ?? 0) });
                files.Write(Path.Combine(stage, "GangState.json"), new { Gang = (string)player["PlayerGangAffiliation"], Reputation = Math.Max(0, (int?)player["PlayerGangReputation"] ?? 0) });
                Directory.Move(stage, targetDirectory);
            }
            finally { if (Directory.Exists(stage)) Directory.Delete(stage, true); }
        }

        static float Percent(JObject d, string key)
        {
            float value = (float?)d[key] ?? 100;
            return float.IsNaN(value) || float.IsInfinity(value) ? 100 : Math.Max(0, Math.Min(100, value));
        }
    }
}
