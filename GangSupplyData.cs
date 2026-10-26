using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RPCore
{
    public sealed class GangSupplyOptions : ExtensibleData
    {
        public int DiscountPercent=25,TimeLimitSeconds=1800;
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)]
        public List<JobPoint> Suppliers=new List<JobPoint>{
            new JobPoint{Name="Fornecedor Davis",X=55.15895f,Y=-1737.911f,Z=29.57429f,Heading=259.3541f},
            new JobPoint{Name="Fornecedor Little Seoul",X=-677.5007f,Y=-854.6947f,Z=24.0892f,Heading=183.2677f},
            new JobPoint{Name="Fornecedor Burton",X=-151.7666f,Y=-290.0251f,Z=40.10313f,Heading=47.94599f}};
        [JsonIgnore] public bool Valid=>DiscountPercent>=0 && DiscountPercent<=50 && TimeLimitSeconds>=300 && TimeLimitSeconds<=3600 && Suppliers!=null && Suppliers.Count==3 && Suppliers.All(p=>p!=null && p.Valid) && Suppliers.Select(p=>p.X+"|"+p.Y+"|"+p.Z).Distinct().Count()==3;
    }
    public sealed class GangSupplyOrder : ExtensibleData
    {
        public GangCafeOptions Cafe;
        public GangSupplyCargo CargoKind;public string ProductId;public float ProductWeight,WarehouseCapacity;public int StartRadius;
        public string Id,Gang;public GangTurfDefinition Area;public GangSupplyOptions Options;
        public int Units,Cost,Capacity,Delivered;public List<int> Loaded=new List<int>();
        [JsonIgnore] public bool Ready=>Delivered==3;
        [JsonIgnore] public bool Valid=>Enum.IsDefined(typeof(GangSupplyCargo),CargoKind) && (CargoKind!=GangSupplyCargo.Criminal || !string.IsNullOrWhiteSpace(ProductId) && ProductId.Length<=80 && GangPresenceRules.Finite(ProductWeight) && ProductWeight>0 && ProductWeight<=5 && GangPresenceRules.Finite(WarehouseCapacity) && WarehouseCapacity>=1 && WarehouseCapacity<=1000 && StartRadius>=5 && StartRadius<=30) && !string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && !GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area!=null && Area.Valid && Options!=null && Options.Valid && Units>=1 && Units<=100 && Capacity>=Units && Capacity<=1000 && (CargoKind==GangSupplyCargo.Cafe?Cost==0 && Units==3 && Capacity==3 && Area.Id==4 && Cafe?.Valid==true:Cost>=1 && Cost<=1000000) && Loaded!=null && Loaded.Count<=3 && Loaded.All(i=>i>=0 && i<3) && Loaded.Distinct().Count()==Loaded.Count && Delivered>=0 && Delivered<=3 && (Delivered==0 || Loaded.Count==3);
    }
    public static class GangSupplyTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        static bool Id(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=80;
        static bool Apply(WalletData w,string receipt,Func<GangBusinessData,int?> mutate,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || w.GangBusiness!=null && !w.GangBusiness.Valid)return false;
            var old=w.GangBusiness;if(old?.Receipts.Contains(receipt)==true)return true;
            var next=old==null?new GangBusinessData():Copy(old);int? delta=mutate(next);
            if(delta==null || (long)w.Bank+delta.Value<0 || (long)w.Bank+delta.Value>int.MaxValue || !next.Valid)return false;
            next.Receipts.Add(receipt);if(next.Receipts.Count>256)next.Receipts.RemoveAt(0);int bank=w.Bank;
            try{w.GangBusiness=next;w.Bank+=delta.Value;save();changed=true;return true;}
            catch{w.GangBusiness=old;w.Bank=bank;throw;}
        }
        public static bool Begin(WalletData w,GangState s,GangTurfDefinition area,GangBusinessOptions business,GangSupplyOptions options,string id,Action save,out bool changed)
        {
            changed=false;if(GangBulkTransactions.Blocked(w) || w?.GangRaid?.Pending!=null || !Id(id) || !GangBusinessTransactions.Owns(s,area) || business==null || !business.Valid || options==null || !options.Valid)return false;
            int cost=Math.Max(1,business.PurchaseUnits*business.UnitCost*(100-options.DiscountPercent)/100);
            return Apply(w,id+":supply-begin",b=>{
                if(b.Supply!=null)return null;var a=GangBusinessTransactions.Account(b,s.Gang,area.Id);
                if(a==null && b.Accounts.Count>=640 || (a?.Stock??0)>business.Capacity-business.PurchaseUnits)return null;
                b.Supply=new GangSupplyOrder{Id=id,Gang=s.Gang,Area=Copy(area),Options=Copy(options),Units=business.PurchaseUnits,Cost=cost,Capacity=business.Capacity};return -cost;
            },save,out changed);
        }
        public static bool BeginCriminal(WalletData w,GangState state,GangTurfDefinition area,DealerOptions catalog,GangWarehouseOptions warehouse,GangCriminalSupplyOptions c,string product,string id,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || !GangBusinessTransactions.Owns(state,area) || catalog==null || !catalog.Valid || warehouse==null || !warehouse.Valid || c==null || !c.Valid || string.IsNullOrWhiteSpace(product) || product.Length>80 || w==null || GangBulkTransactions.Blocked(w) || w.GangRaid?.Pending!=null || w.GangWarehouse!=null && !w.GangWarehouse.Valid)return false;
            var item=catalog.Products.Find(p=>p.Id==product);if(item==null)return false;var a=GangWarehouseTransactions.Account(w.GangWarehouse,state.Gang,area.Id);
            if(a==null && w.GangWarehouse?.Accounts.Count>=640 || (a==null?0:GangWarehouseAccount.Get(a.Stock,product))>999-c.Units || GangWarehouseTransactions.Weight(a,catalog)+c.Units*(double)item.Weight>warehouse.Capacity+.0001)return false;
            int cost=Math.Max(1,item.Buy*c.Units*(100-c.DiscountPercent)/100);
            return Apply(w,id+":supply-begin",b=>{if(b.Supply!=null)return null;b.Supply=new GangSupplyOrder{Id=id,Gang=state.Gang,Area=Copy(area),Options=Copy(c.Route),Units=c.Units,Cost=cost,Capacity=c.Units,CargoKind=GangSupplyCargo.Criminal,ProductId=product,ProductWeight=item.Weight,WarehouseCapacity=warehouse.Capacity,StartRadius=warehouse.Radius};return -cost;},save,out changed);
        }
        public static bool Load(WalletData w,string id,int index,Action save,out bool changed)
        {changed=false;if(!Id(id) || index<0 || index>=3)return false;return Apply(w,id+":supply-load:"+index,b=>{var p=b.Supply;if(p==null || p.Id!=id || p.Delivered!=0 || p.Loaded.Contains(index))return null;p.Loaded.Add(index);return 0;},save,out changed);}
        public static bool Deliver(WalletData w,string id,int number,Action save,out bool changed)
        {changed=false;if(!Id(id) || number<1 || number>3)return false;return Apply(w,id+":supply-deliver:"+number,b=>{var p=b.Supply;if(p==null || p.Id!=id || p.Loaded.Count!=3 || p.Delivered!=number-1)return null;p.Delivered=number;return 0;},save,out changed);}
        public static bool Restart(WalletData w,string id,Action save,out bool changed)
        {
            changed=false;if(!Id(id))return false;
            return Apply(w,Guid.NewGuid().ToString("N")+":supply-restart",b=>{var p=b.Supply;if(p==null || p.Id!=id || p.Ready)return null;p.Loaded.Clear();p.Delivered=0;
                // Physical cargo is not restored from entity handles. Drop checkpoint receipts too,
                // otherwise a repeated pickup after reload could be mistaken for an old load.
                b.Receipts.RemoveAll(r=>r.StartsWith(id+":supply-load:",StringComparison.Ordinal) || r.StartsWith(id+":supply-deliver:",StringComparison.Ordinal));return 0;},save,out changed);
        }
        public static bool Complete(WalletData w,GangState s,GangTurfDefinition currentArea,string id,Action save,out bool changed)=>Complete(w,s,currentArea,id,save,out changed,null);
        public static bool Complete(WalletData w,GangState s,GangTurfDefinition currentArea,string id,Action save,out bool changed,DealerOptions catalog)
        {
            changed=false;if(!Id(id) || !GangBusinessTransactions.Owns(s,currentArea))return false;
            if(w?.GangBusiness?.Supply?.CargoKind==GangSupplyCargo.Cafe)return false; // Cafe completion must atomically grant its earned bonus.
            if(w?.GangBusiness?.Supply?.CargoKind==GangSupplyCargo.Criminal)return CompleteCriminal(w,s,currentArea,id,catalog,save,out changed);
            return Apply(w,id+":supply-complete",b=>{var p=b.Supply;if(p==null || p.Id!=id || !p.Ready || p.Area.Id!=currentArea.Id || !GangCrewRules.SameGang(p.Gang,s.Gang))return null;
                var a=GangBusinessTransactions.Account(b,p.Gang,p.Area.Id);if(a==null){if(b.Accounts.Count>=640)return null;a=new GangBusinessAccount{Gang=p.Gang,Area=p.Area.Id};b.Accounts.Add(a);}
                if(a.Stock>p.Capacity-p.Units || a.Purchased>long.MaxValue-p.Units)return null;
                a.Stock+=p.Units;a.Purchased+=p.Units;b.Supply=null;return 0;},save,out changed);
        }
        static bool CompleteCriminal(WalletData w,GangState state,GangTurfDefinition area,string id,DealerOptions catalog,Action save,out bool changed)
        {
            changed=false;if(w.Bank<0 || save==null || w.GangBusiness?.Valid!=true || w.GangWarehouse!=null && !w.GangWarehouse.Valid || catalog==null || !catalog.Valid)return false;
            string receipt=id+":supply-complete";if(w.GangBusiness.Receipts.Contains(receipt))return true;
            var original=w.GangBusiness;var oldWarehouse=w.GangWarehouse;var book=Copy(original);var p=book.Supply;
            if(p==null || p.Id!=id || !p.Ready || p.Area.Id!=area.Id || !GangCrewRules.SameGang(p.Gang,state.Gang))return false;
            var storage=oldWarehouse==null?new GangWarehouseData():Copy(oldWarehouse);var a=GangWarehouseTransactions.Account(storage,p.Gang,p.Area.Id);
            if(a==null){if(storage.Accounts.Count>=640)return false;a=new GangWarehouseAccount{Gang=p.Gang,Area=p.Area.Id};storage.Accounts.Add(a);}
            int held=GangWarehouseAccount.Get(a.Stock,p.ProductId);long supplied=GangWarehouseAccount.Get(a.Supplied,p.ProductId);
            if(held>999-p.Units || supplied>long.MaxValue-p.Units)return false;
            a.Stock[p.ProductId]=held+p.Units;a.Supplied[p.ProductId]=supplied+p.Units;
            if(GangWarehouseTransactions.Weight(a,catalog)>p.WarehouseCapacity+.0001 || !storage.Valid)return false;
            book.Supply=null;book.Receipts.Add(receipt);if(book.Receipts.Count>256)book.Receipts.RemoveAt(0);storage.Receipts.Add(id+":criminal-supply");if(storage.Receipts.Count>256)storage.Receipts.RemoveAt(0);
            try{w.GangBusiness=book;w.GangWarehouse=storage;save();changed=true;return true;}catch{w.GangBusiness=original;w.GangWarehouse=oldWarehouse;throw;}
        }
        public static bool Refund(WalletData w,string id,Action save,out bool changed)
        {changed=false;if(!Id(id))return false;return Apply(w,id+":supply-refund",b=>{var p=b.Supply;if(p==null || p.Id!=id)return null;int cost=p.Cost;b.Supply=null;return cost;},save,out changed);}
    }
}
