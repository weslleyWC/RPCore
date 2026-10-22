using System.Collections.Generic;

namespace RPCore
{
    public class ItemDef
    {
        public string Id;
        public string Name;
        public string Desc = "";
        public float Weight = 0.1f;
        public int Price;          // 0 = not sold in shops
        public float Hunger;
        public float Thirst;
        public int Health;
    }

    public class InvData : ExtensibleData
    {
        public Dictionary<string, int> Items = new Dictionary<string, int>();
        public Dictionary<string, Dictionary<string, int>> Containers = new Dictionary<string, Dictionary<string, int>>();
        public List<string> GridSlots = new List<string>();
    }

}
