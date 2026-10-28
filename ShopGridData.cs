using System;
using System.Collections.Generic;
using System.Linq;
namespace RPCore
{
    public sealed class ShopCategory
    {
        public string Name;
        public List<ItemDef> Items;
    }
    public static class ShopCatalogRules
    {
        static readonly string[] Order={"Bebidas","Lanches","Comida","Frutas","Suplementos","Saude","Equipamentos","Outros"};
        static readonly HashSet<string> Fruit=new HashSet<string>(StringComparer.OrdinalIgnoreCase){"apple","orange","lime","pear","kiwi"};
        static readonly HashSet<string> Snacks=new HashSet<string>(StringComparer.OrdinalIgnoreCase){"chips","psrp_hershey","psrp_hersheycookie","psrp_twix","psrp_snickers","psrp_reeses","psrp_marsbar","psrp_babyruth","psrp_galaxybar","psrp_milkyway"};
        public static string Category(ItemDef item)
        {
            if(!string.IsNullOrWhiteSpace(item?.Category))return item.Category.Trim();
            string id=item?.Id??"";string basic=id.StartsWith("psrp_",StringComparison.Ordinal)?id.Substring(5):id;
            if(Fruit.Contains(basic))return "Frutas";
            if((id.StartsWith("psrp_whey",StringComparison.Ordinal) || id.StartsWith("psrp_protein",StringComparison.Ordinal) || id.StartsWith("psrp_protien",StringComparison.Ordinal)) || id.StartsWith("psrp_shaker",StringComparison.Ordinal))return "Suplementos";
            if(Snacks.Contains(id))return "Lanches";
            if(item?.Health>0)return "Saude";
            if(item?.Thirst>item?.Hunger)return "Bebidas";
            if(item?.Hunger>0)return "Comida";
            if(item?.Action!=null || item?.Weapon!=null)return "Equipamentos";
            return "Outros";
        }
        public static List<ShopCategory> Build(IEnumerable<ItemDef> items)
            =>(items??Enumerable.Empty<ItemDef>()).Where(i=>i!=null && !string.IsNullOrWhiteSpace(i.Id) && i.Price>0 && !float.IsNaN(i.Weight) && !float.IsInfinity(i.Weight) && i.Weight>=0).GroupBy(Category,StringComparer.Ordinal).Select(g=>new ShopCategory{Name=g.Key,Items=g.OrderBy(i=>i.Name??i.Id,StringComparer.OrdinalIgnoreCase).ThenBy(i=>i.Id,StringComparer.Ordinal).ToList()}).OrderBy(g=>Array.IndexOf(Order,g.Name)<0?Order.Length:Array.IndexOf(Order,g.Name)).ThenBy(g=>g.Name,StringComparer.OrdinalIgnoreCase).ToList();
        public static int Maximum(ItemDef item,int held,double currentWeight,double capacity,int cash)
        {
            if(item==null || item.Price<=0 || held<0 || cash<0 || double.IsNaN(currentWeight) || double.IsInfinity(currentWeight) || currentWeight<0 || double.IsNaN(capacity) || double.IsInfinity(capacity) || capacity<0 || float.IsNaN(item.Weight) || float.IsInfinity(item.Weight) || item.Weight<0 || currentWeight>capacity)return 0;
            int max=Math.Min(100,Math.Min(int.MaxValue-held,cash/item.Price));
            if(item.Weight>0)max=(int)Math.Min(max,Math.Max(0,Math.Floor((capacity-currentWeight)/item.Weight)));
            return max;
        }
        public static long Total(ItemDef item,int quantity)=>item==null || item.Price<=0 || quantity<1 || quantity>100?0:(long)item.Price*quantity;
        public static int WrapCategory(int index,int delta,int count)=>count<=0?0:(int)(((long)index+delta)%count+count)%count;
        // Logical geometry shared by drawing and pointer input; overlap is resolved by the
        // existing inventory hit tester. Only clicking the Buy button commits a mouse purchase.
        public static int ActionAt(float x,float y)
        {
            if(y<790 || y>=840)return -1;
            if(x>=1040 && x<1100)return 0;
            if(x>=1210 && x<1270)return 1;
            if(x>=1280 && x<1400)return 2;
            if(x>=1430 && x<1820)return 3;
            return -1;
        }
        public static int CategoryAt(float x,float y,int selected,int count)
        {
            if(x<1040 || x>=1820 || y<185 || y>=224 || count<=0)return -1;
            int result=selected/4*4+(int)((x-1040)/195);return result<count?result:-1;
        }
    }
}
