using System;
using System.Collections.Generic;
using System.Linq;

namespace RPCore
{
    public class ShopDef
    {
        public string Name;
        public float X, Y, Z;
    }

    public static class ShopLocations
    {
        // Customer-side interaction points read from PSRP NewInventorySystem.shopLocations.
        public static List<ShopDef> Defaults() => new List<ShopDef>
        {
                new ShopDef { Name = "Rob’s Liquor Murrieta Heights", X = 1136.87f, Y = -978.955f, Z = 46.416f },
                new ShopDef { Name = "LTD Mirror Park", X = 1155.941f, Y = -320.24f, Z = 69.205f },
                new ShopDef { Name = "24/7 Strawberry", X = 33.312f, Y = -1346.681f, Z = 29.497f },
                new ShopDef { Name = "LTD Davis", X = -54.7f, Y = -1749.089f, Z = 29.421f },
                new ShopDef { Name = "Rob’s Liquor Vespucci", X = -1226.175f, Y = -906.743f, Z = 12.326f },
                new ShopDef { Name = "Rob’s Liquor Morningwood", X = -1486.774f, Y = -382.577f, Z = 40.163f },
                new ShopDef { Name = "24/7 Downtown Vinewood", X = 381.065f, Y = 324.738f, Z = 103.566f },
                new ShopDef { Name = "24/7 Palomino Freeway", X = 2557.164f, Y = 389.37f, Z = 108.623f },
                new ShopDef { Name = "LTD Banham Canyon", X = -1828.42f, Y = 792.017f, Z = 138.249f },
                new ShopDef { Name = "24/7 Ineseno Road", X = -3041.866f, Y = 592.298f, Z = 7.909f },
                new ShopDef { Name = "Rob’s Liquor Route 68", X = -2969.848f, Y = 388.746f, Z = 15.043f },
                new ShopDef { Name = "24/7 Chumash", X = -3241.843f, Y = 1009.127f, Z = 12.831f },
                new ShopDef { Name = "24/7 Harmony", X = 540.362f, Y = 2669.842f, Z = 42.156f },
                new ShopDef { Name = "Scoops Liquor Barn", X = 1168.984f, Y = 2707.549f, Z = 38.158f },
                new ShopDef { Name = "24/7 Grapeseed Highway", X = 1399.139f, Y = 3603.706f, Z = 34.981f },
                new ShopDef { Name = "24/7 Sandy Shores", X = 1966.941f, Y = 3744.537f, Z = 32.344f },
                new ShopDef { Name = "24/7 Senora Freeway", X = 2681.884f, Y = 3287.724f, Z = 55.241f },
                new ShopDef { Name = "LTD Grapeseed", X = 1706.255f, Y = 4927.944f, Z = 42.064f },
                new ShopDef { Name = "24/7 Mount Chiliad", X = 1735.854f, Y = 6412.169f, Z = 35.037f },
                new ShopDef { Name = "LTD Little Seoul", X = -714.1821f, Y = -909.6077f, Z = 19.21558f },
        };

        static readonly ShopDef[] Legacy = {
            new ShopDef { Name = "24/7 Strawberry", X = 25.7f, Y = -1347.3f, Z = 29.5f },
            new ShopDef { Name = "LTD Davis", X = -47f, Y = -1758.7f, Z = 29.4f },
            new ShopDef { Name = "LTD Mirror Park", X = 1163.4f, Y = -323.8f, Z = 69.2f }
        };
        static bool Matches(ShopDef a, ShopDef b) => a != null && b != null &&
            string.Equals(a.Name, b.Name, StringComparison.Ordinal) &&
            Math.Abs(a.X - b.X) < .001f && Math.Abs(a.Y - b.Y) < .001f && Math.Abs(a.Z - b.Z) < .001f;

        // Runtime overlay only: never rewrite Shops.json or discard custom shops/coordinates.
        public static List<ShopDef> Resolve(List<ShopDef> configured)
        {
            var defaults = Defaults();
            if (configured == null) return new List<ShopDef>();
            if (configured.Count == Legacy.Length && Legacy.All(old => configured.Count(s => Matches(s, old)) == 1))
                return defaults;
            return configured.Select(s => {
                var old = Legacy.FirstOrDefault(candidate => Matches(s, candidate));
                var point = old == null ? s : defaults.First(d => d.Name == old.Name);
                return point == null ? null : new ShopDef { Name = point.Name, X = point.X, Y = point.Y, Z = point.Z };
            }).Where(s => s != null).ToList();
        }
    }
}
