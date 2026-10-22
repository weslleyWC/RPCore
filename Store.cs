using System;
using System.IO;
using Newtonsoft.Json;

namespace RPCore
{
    public static class Log
    {
        static readonly object Gate = new object();

        public static void Write(string msg)
        {
            try
            {
                lock (Gate)
                {
                    Directory.CreateDirectory(Cfg.Dir);
                    File.AppendAllText(Path.Combine(Cfg.Dir, "RPCore.log"),
                        DateTime.Now.ToString("HH:mm:ss ") + msg + Environment.NewLine);
                }
            }
            catch { }
        }

        public static void Error(string where, Exception ex) { Write(where + ": " + ex); }
    }

    public static class Store
    {
        static readonly JsonFiles Files = new JsonFiles();
        // SaveData/*: player progress. Atomic write so a crash mid-save can't corrupt it.
        public static T Load<T>(string name, Func<T> fallback)
        {
            string p = Path.Combine(Cfg.SaveDir, Profile.Id, name);
            try
            {
                if (File.Exists(p))
                {
                    T v = Files.Read(p, fallback);
                    if (v != null) return v;
                }
            }
            catch (Exception ex) { Log.Error("load " + name, ex); }
            return fallback();
        }

        public static void Write(string name, object data)
        {
            string p = Path.Combine(Cfg.SaveDir, Profile.SafeId(Profile.Id), name);
            try
            {
                Files.Write(p, data);
            }
            catch (Exception ex) { Log.Error("save " + name, ex); throw; }
        }

        // scripts/RPCore/*.json: user-editable definitions. Written once from defaults if missing.
        public static T LoadDef<T>(string name, Func<T> defaults)
        {
            return JsonDefinitions.Load(Path.Combine(Cfg.Dir, name), defaults, ex => Log.Error("def " + name, ex));
        }
    }
}
