using System;
using GTA;
using System.IO;
using System.Linq;

namespace RPCore
{
    // Separate save slot per protagonist. Unknown models (custom peds, transitions) keep the last slot.
    public static class Profile
    {
        public static string Id = "Default";
        public static string SafeId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return "Default";
            string safe = new string(id.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
            return string.IsNullOrEmpty(safe) ? "Default" : safe;
        }

        public static bool IsProtagonist(string id) { return id == "Michael" || id == "Franklin" || id == "Trevor"; }

        public static string Detect()
        {
            try
            {
                Ped p = Game.Player.Character;
                if (p != null && p.Exists())
                {
                    int h = p.Model.Hash;
                    if (h == unchecked((int)PedHash.Michael)) return "Michael";
                    if (h == unchecked((int)PedHash.Franklin)) return "Franklin";
                    if (h == unchecked((int)PedHash.Trevor)) return "Trevor";
                    if (h == unchecked((int)PedHash.FreemodeMale01) || h == unchecked((int)PedHash.FreemodeFemale01)) return Cfg.CustomProfile;
                }
            }
            catch { }
            return Id;
        }
    }
}
