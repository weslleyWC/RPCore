using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RPCore
{
    public static class InventoryShortcuts
    {
        public static int Slot(int virtualKey,bool modified=false)=>!modified && virtualKey>=49 && virtualKey<=54?virtualKey-49:-1;
        public static string Item(InvData data,int slot)
        {
            if(data?.GridSlots==null || data.Items==null || slot<0 || slot>=6 || slot>=data.GridSlots.Count)return null;
            string id=data.GridSlots[slot];return id!=null && data.Items.TryGetValue(id,out int count) && count>0?id:null;
        }
    }
    public sealed class InventoryNavigation
    {
        public int Selected { get; private set; }
        public int FirstRow { get; private set; }
        public void Select(int index,int count)
        {
            Selected=Math.Max(0,Math.Min(index,Math.Max(0,count-1)));
            int row=Selected/6;if(row<FirstRow)FirstRow=row;else if(row>=FirstRow+4)FirstRow=row-3;
            FirstRow=Math.Max(0,Math.Min(FirstRow,Math.Max(0,(count+5)/6-4)));
        }
        public void Move(int dx,int dy,int count)
        {int row=Selected/6,col=Selected%6;Select(Math.Max(0,Math.Min(Math.Max(0,(count-1)/6),row+dy))*6+Math.Max(0,Math.Min(5,col+dx)),count);}
    }
    public sealed class InventoryRecipe : ExtensibleData
    {
        public string Id,Name,Result;
        public int Quantity=1;
        public Dictionary<string,int> Ingredients=new Dictionary<string,int>();
        public HashSet<string> Reusable=new HashSet<string>();
        [JsonIgnore]public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && !string.IsNullOrWhiteSpace(Result) && Result.Length<=80 && Quantity>0 && Quantity<=1000 && Ingredients!=null && Ingredients.Count>=2 && Ingredients.Count<=3 && Ingredients.All(p=>!string.IsNullOrWhiteSpace(p.Key) && p.Key.Length<=80 && p.Value>0 && p.Value<=1000) && Reusable!=null && Reusable.All(Ingredients.ContainsKey) && Ingredients.Keys.Any(k=>!Reusable.Contains(k));
    }
    public static class PersonalCrafting
    {
        public static int Maximum(InvData data,InventoryRecipe recipe)
        {
            if(data?.Items==null || recipe?.Valid!=true)return 0;int max=100;
            foreach(var i in recipe.Ingredients){data.Items.TryGetValue(i.Key,out int n);if(n<i.Value)return 0;if(!recipe.Reusable.Contains(i.Key))max=Math.Min(max,n/i.Value);}return max;
        }
        public static bool Matches(InventoryRecipe r,IEnumerable<string> selection)=>r?.Valid==true && selection!=null && r.Ingredients.Keys.OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(selection.OrderBy(x=>x,StringComparer.Ordinal));
        public static bool Craft(ref InvData data,InventoryRecipe recipe,int batches,float capacity,Func<string,float> weight,Action<InvData> save)
        {
            if(recipe?.Valid!=true || batches<1 || batches>Maximum(data,recipe) || float.IsNaN(capacity) || float.IsInfinity(capacity) || capacity<0 || weight==null || save==null)return false;
            var next=JsonConvert.DeserializeObject<InvData>(JsonConvert.SerializeObject(data));
            foreach(var i in recipe.Ingredients)if(!recipe.Reusable.Contains(i.Key)){int n=next.Items[i.Key]-i.Value*batches;if(n==0)next.Items.Remove(i.Key);else next.Items[i.Key]=n;}
            next.Items.TryGetValue(recipe.Result,out int old);long result=(long)old+recipe.Quantity*batches;if(old<0 || result>int.MaxValue)return false;next.Items[recipe.Result]=(int)result;
            double before=InventoryTransfers.Weight(data.Items,weight),after=InventoryTransfers.Weight(next.Items,weight);
            if(double.IsNaN(before) || double.IsNaN(after) || after>capacity && after>before)return false;
            next.GridSlots=InventoryLayout.Reconcile(next.GridSlots,next.Items.Where(p=>p.Value>0).Select(p=>p.Key),24);
            save(next);data=next;return true;
        }
    }
    public static class InventoryPurchases
    {
        // Cash/native wallet and Inventory.json are separate stores. Roll back ordinary failures;
        // a process crash or a second write failure during refund is not a cross-store transaction.
        public static bool Buy(ref InvData data,ItemDef item,int count,float capacity,Func<string,float> weight,Func<int> cash,Func<int,bool> debit,Action<int> credit,Action saveWallet,Action<InvData> saveInventory)
        {
            if(data?.Items==null || item==null || string.IsNullOrWhiteSpace(item.Id) || item.Price<=0 || count<1 || count>100 || weight==null || cash==null || debit==null || credit==null || saveWallet==null || saveInventory==null || float.IsNaN(capacity) || float.IsInfinity(capacity) || capacity<0)return false;
            long cost=(long)item.Price*count;data.Items.TryGetValue(item.Id,out int held);if(cost>int.MaxValue || held<0 || held>int.MaxValue-count || cash()<cost)return false;
            var next=JsonConvert.DeserializeObject<InvData>(JsonConvert.SerializeObject(data));next.Items[item.Id]=held+count;double total=InventoryTransfers.Weight(next.Items,weight);if(double.IsNaN(total) || total>capacity)return false;
            next.GridSlots=InventoryLayout.Reconcile(next.GridSlots,next.Items.Where(p=>p.Value>0).Select(p=>p.Key),24);
            if(!debit((int)cost))return false;
            try{saveWallet();saveInventory(next);}catch{credit((int)cost);saveWallet();throw;}
            data=next;return true;
        }
    }
}
