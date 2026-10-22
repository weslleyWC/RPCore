using GTA;
using GTA.Math;

namespace RPCore
{
    // Callers retain ownership and delete only their own blips in Dispose.
    internal static class MapBlips
    {
        public static Blip Create(Vector3 position, string label, BlipSprite sprite, BlipColor color, float scale)
        {
            var blip = World.CreateBlip(position);
            blip.Sprite = sprite;
            blip.Color = color;
            blip.Scale = scale;
            blip.Name = label;
            blip.IsShortRange = true;
            return blip;
        }
    }
}
