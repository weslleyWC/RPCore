using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class HomeStorageOptions : ExtensibleData
    {
        public string PropertyId = "eclipse", ContainerId = "home:eclipse";
        public JobPoint Point;
        public float Capacity = 200;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(PropertyId) && PropertyId.Length <= 40 && !string.IsNullOrWhiteSpace(ContainerId) && ContainerId.StartsWith("home:",StringComparison.Ordinal) && ContainerId.Length <= 100 && Point != null && Point.Valid && !float.IsNaN(Capacity) && !float.IsInfinity(Capacity) && Capacity > 0 && Capacity <= 10000;
    }
    public static class HomeStorageDefaults
    {
        public static HomeStorageOptions Load()
        { using(var s=typeof(HomeStorageDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.HomeStorage.json")) using(var r=new System.IO.StreamReader(s)) return JsonConvert.DeserializeObject<HomeStorageOptions>(r.ReadToEnd()); }
    }
    public static class InventoryTransfers
    {
        public static int Fits(InvData data,string container,string id,bool deposit,float capacity,float maxWeight,Func<string,float> weight)
        {
            if(data?.Items==null || data.Containers==null || string.IsNullOrWhiteSpace(container) || string.IsNullOrWhiteSpace(id) || weight==null)return 0;
            var box=Snapshot(data,container);var source=deposit?data.Items:box;var target=deposit?box:data.Items;
            if(!source.TryGetValue(id,out int available) || available<=0)return 0;
            target.TryGetValue(id,out int held);if(held<0)return 0;
            double limit=deposit?capacity:maxWeight,total=Weight(target,weight);float unit=weight(id);
            if(double.IsNaN(limit) || double.IsInfinity(limit) || limit<0 || double.IsNaN(total) || float.IsNaN(unit) || float.IsInfinity(unit) || unit<0 || total>limit)return 0;
            int count=Math.Min(available,int.MaxValue-held);if(unit==0)return count;
            return (int)Math.Min(count,Math.Max(0,Math.Floor((limit-total)/unit)));
        }
        public static Dictionary<string,int> Snapshot(InvData d,string container)
        { Dictionary<string,int> items; return d?.Containers != null && d.Containers.TryGetValue(container,out items) && items != null ? new Dictionary<string,int>(items) : new Dictionary<string,int>(); }
        public static double Weight(Dictionary<string,int> items,Func<string,float> weight)
        {
            if(items==null || weight==null)return double.NaN;double total=0;
            foreach(var pair in items){float unit=weight(pair.Key);if(pair.Value<0 || float.IsNaN(unit) || float.IsInfinity(unit) || unit<0)return double.NaN;total+=(double)unit*pair.Value;}
            return total;
        }
        // Save a complete candidate in one Inventory.json checkpoint; never mutate the live input on failure.
        public static bool Move(InvData current,string container,string item,int quantity,bool deposit,float capacity,float maxWeight,Func<string,float> weight,Action<InvData> save,out InvData result)
        {
            result=current;
            if(current==null || current.Items==null || current.Containers==null || string.IsNullOrWhiteSpace(container) || container.Length>160 || string.IsNullOrWhiteSpace(item) || quantity<=0 || weight==null || save==null || float.IsNaN(capacity) || float.IsInfinity(capacity) || capacity<0 || float.IsNaN(maxWeight) || float.IsInfinity(maxWeight) || maxWeight<0)return false;
            var next=JsonConvert.DeserializeObject<InvData>(JsonConvert.SerializeObject(current));
            Dictionary<string,int> box;if(!next.Containers.TryGetValue(container,out box) || box==null) next.Containers[container]=box=new Dictionary<string,int>();
            if(double.IsNaN(Weight(next.Items,weight)) || double.IsNaN(Weight(box,weight)))return false;
            if(!ItemTransfer.Move(deposit?next.Items:box,deposit?box:next.Items,item,quantity,weight,deposit?capacity:maxWeight))return false;
            next.GridSlots=InventoryLayout.Reconcile(next.GridSlots,next.Items.Where(p=>p.Value>0).Select(p=>p.Key),24);
            save(next);result=next;return true;
        }
    }
}
