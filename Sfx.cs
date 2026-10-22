using System.Collections.Generic;
using System.IO;
using System.Media;

namespace RPCore
{
    // Plays scripts\RPCore\Audio\<name>.wav if it exists, silent otherwise. Ships with no audio of its own.
    public static class Sfx
    {
        static readonly Dictionary<string, SoundPlayer> Cache = new Dictionary<string, SoundPlayer>();
        static readonly Dictionary<string, string> Aliases = new Dictionary<string, string> {
            { "belt_on", "Vehicles/seatbelt.wav" }, { "belt_off", "Vehicles/seatbeltunhook.wav" },
            { "achievement", "Phone/Noti1.wav" }, { "buy", "Phone/Noti2.wav" }, { "delivered", "Phone/Noti3.wav" },
            { "fuel", "Vehicles/PetrolPump.wav" }
        };

        public static void Play(string name)
        {
            try
            {
                SoundPlayer sp;
                if (!Cache.TryGetValue(name, out sp))
                {
                    string relative; if (!Aliases.TryGetValue(name, out relative)) relative = name + ".wav";
                    string path = Path.Combine(Cfg.Dir, "Audio", relative);
                    sp = File.Exists(path) ? new SoundPlayer(path) : null;
                    Cache[name] = sp;
                }
                if (sp != null) sp.Play();
            }
            catch (System.Exception ex) { Log.Error("sfx " + name, ex); }
        }
        public static void Dispose() { foreach (var sp in Cache.Values) if (sp != null) { sp.Stop(); sp.Dispose(); } Cache.Clear(); }
        public static void Stop(string name) { SoundPlayer sp; if (Cache.TryGetValue(name, out sp) && sp != null) sp.Stop(); }
    }
}
