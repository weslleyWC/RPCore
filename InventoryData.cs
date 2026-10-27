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
        public uint? Weapon;
        public string Icon;
    }

    public class InvData : ExtensibleData
    {
        public NeedsConsumption Consumption;
        public InventoryArmory Armory;
        public Dictionary<string, int> Items = new Dictionary<string, int>();
        public Dictionary<string, Dictionary<string, int>> Containers = new Dictionary<string, Dictionary<string, int>>();
        public List<string> GridSlots = new List<string>();
    }

}

namespace RPCore
{
    public static class InventoryEdits
    {
        static bool Apply(ref InvData data,System.Func<InvData,bool> change,System.Action<InvData> save)
        {if(data==null || save==null)return false;var copy=Newtonsoft.Json.JsonConvert.DeserializeObject<InvData>(Newtonsoft.Json.JsonConvert.SerializeObject(data));if(!change(copy))return false;save(copy);data=copy;return true;}
        public static bool Drop(ref InvData data,string id,int amount,System.Action<InvData> save)=>Apply(ref data,n=>{if(string.IsNullOrWhiteSpace(id) || amount<=0 || n.Items==null || !n.Items.TryGetValue(id,out int held) || held<amount)return false;if(held==amount)n.Items.Remove(id);else n.Items[id]=held-amount;return true;},save);
        public static bool Move(ref InvData data,int from,int to,System.Action<InvData> save)=>Apply(ref data,n=>InventoryLayout.Swap(n.GridSlots,from,to),save);
    }
}
