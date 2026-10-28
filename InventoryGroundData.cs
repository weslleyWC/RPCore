using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public class InventoryGroundItem : ExtensibleData
    {
        public string Id,Item;
        public int Quantity,Interior;
        public float X,Y,Z;
        public bool Valid=>!string.IsNullOrWhiteSpace(Id) && !string.IsNullOrWhiteSpace(Item) && Quantity>0 && Interior>=0 && Finite(X) && Finite(Y) && Finite(Z);
        static bool Finite(float n)=>!float.IsNaN(n) && !float.IsInfinity(n) && Math.Abs(n)<100000;
    }
    public static class InventoryGroundRules
    {
        public const int Limit=128;
        public static bool Valid(InvData d)=>d?.Items!=null && (d.GroundItems==null || d.GroundItems.Count<=Limit && d.GroundItems.All(g=>g?.Valid==true) && d.GroundItems.Select(g=>g.Id).Distinct(StringComparer.Ordinal).Count()==d.GroundItems.Count);
        static InvData Copy(InvData d)=>JsonConvert.DeserializeObject<InvData>(JsonConvert.SerializeObject(d));
        public static bool Drop(ref InvData d,string item,int quantity,float x,float y,float z,int interior,Action<InvData> save)
        {
            if(!Valid(d) || save==null || string.IsNullOrWhiteSpace(item) || quantity<=0 || !d.Items.TryGetValue(item,out int held) || held<quantity || (d.GroundItems?.Count??0)>=Limit)return false;
            var row=new InventoryGroundItem{Id=Guid.NewGuid().ToString("N"),Item=item,Quantity=quantity,X=x,Y=y,Z=z,Interior=interior};if(!row.Valid)return false;
            var n=Copy(d);if(held==quantity)n.Items.Remove(item);else n.Items[item]=held-quantity;
            if(n.GroundItems==null)n.GroundItems=new List<InventoryGroundItem>();n.GroundItems.Add(row);
            save(n);d=n;return true;
        }
        public static int Fits(InvData d,string id,double capacity,Func<string,float> weight)
        {
            if(!Valid(d) || weight==null || double.IsNaN(capacity) || double.IsInfinity(capacity) || capacity<=0)return 0;
            var row=d.GroundItems?.FirstOrDefault(g=>g.Id==id);if(row==null)return 0;
            d.Items.TryGetValue(row.Item,out int held);if(held<0)return 0;
            float unit=weight(row.Item);if(float.IsNaN(unit) || float.IsInfinity(unit) || unit<0)return 0;
            int count=Math.Min(row.Quantity,int.MaxValue-held);
            double left=capacity-InventoryBurdenRules.Total(d.Items,weight);
            if(left<-.001)return 0;
            if(unit>0)count=(int)Math.Min(count,Math.Max(0,Math.Floor((left+.000001)/unit)));
            return count;
        }
        public static bool Pickup(ref InvData d,string id,int quantity,double capacity,Func<string,float> weight,Action<InvData> save)
        {
            if(save==null || quantity<=0 || quantity>Fits(d,id,capacity,weight))return false;
            var n=Copy(d);var row=n.GroundItems.First(g=>g.Id==id);n.Items.TryGetValue(row.Item,out int held);n.Items[row.Item]=held+quantity;
            row.Quantity-=quantity;if(row.Quantity==0)n.GroundItems.Remove(row);
            save(n);d=n;return true;
        }
        public static InventoryGroundItem Nearest(InvData d,float x,float y,float z,int interior,float range)
        {
            if(!Valid(d) || d.GroundItems==null || range<=0 || float.IsNaN(range) || float.IsInfinity(range))return null;
            double limit=(double)range*range;InventoryGroundItem found=null;
            foreach(var row in d.GroundItems)
            {
                if(row.Interior!=interior)continue;
                double dx=(double)row.X-x,dy=(double)row.Y-y,dz=(double)row.Z-z,distance=dx*dx+dy*dy+dz*dz;
                if(distance<=limit+.00001){limit=distance;found=row;}
            }
            return found;
        }
    }
}
