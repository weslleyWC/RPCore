using System;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public static class PdmEnterpriseTransactions
    {
        static PdmEnterpriseData State(PdmData d)=>d.Enterprise??(d.Enterprise=new PdmEnterpriseData());
        static bool Spend(PdmData d,PdmEnterpriseData e,int cost,string description)
        {if(cost<0 || d.Cash<cost || e.Expenses>long.MaxValue-cost)return false;d.Cash-=cost;e.Expenses+=cost;PdmTransactions.Entry(d,description,-cost);return true;}
        public static bool Order(WalletData wallet,PdmOptions c,string id,int mileage,string receipt,Action save,out bool changed)
        {
            changed=false;if(c?.Valid!=true || c.Catalog.Count!=1 || string.IsNullOrWhiteSpace(id) || id.Length<6 || id.Length>100 || mileage<0 || mileage>200000)return false;
            return PdmTransactions.Apply(wallet,receipt,0,d=>{
                if(!PdmEnterpriseRules.Idle(d) || d.Stock.Count>=c.Capacity)return false;var offer=c.Catalog[0];string plate="PD"+id.Substring(0,6).ToUpperInvariant();
                if(d.Cash<offer.Cost || d.Bought==int.MaxValue || d.StockSpent>long.MaxValue-offer.Cost || d.Stock.Any(v=>v.Id==id || v.Plate==plate) || d.Loans.Any(v=>v.Asset.Id==id || v.Asset.Plate==plate))return false;
                d.Stock.Add(new PdmStock{Id=id,Model=offer.Model,Name=offer.Name,Cost=offer.Cost,Price=offer.Price,Plate=plate,Mileage=mileage,AwaitingCollection=true,FuelFraction=.4f});d.Cash-=offer.Cost;d.StockSpent+=offer.Cost;d.Bought++;PdmTransactions.Entry(d,"Encomenda: "+offer.Name,-offer.Cost);return true;
            },save,out changed);
        }
        public static bool OrderBatch(WalletData wallet,PdmOptions c,System.Collections.Generic.List<PdmStock> cars,string receipt,Action save,out bool changed)
        {
            changed=false;if(c?.Valid!=true || cars==null || cars.Count<1 || cars.Count>6 || cars.Any(v=>v?.Valid!=true) || cars.Select(v=>v.Id).Distinct().Count()!=cars.Count || cars.Select(v=>v.Plate).Distinct().Count()!=cars.Count)return false;
            long total=cars.Sum(v=>(long)v.Cost);if(total>int.MaxValue)return false;
            return PdmTransactions.Apply(wallet,receipt,0,d=>{
                if(!PdmEnterpriseRules.Idle(d) || d.Stock.Count+cars.Count>c.Capacity || d.Cash<total || d.Bought>int.MaxValue-cars.Count || d.StockSpent>long.MaxValue-total || cars.Any(v=>d.Stock.Any(s=>s.Id==v.Id || s.Plate==v.Plate) || d.Loans.Any(l=>l.Asset.Id==v.Id || l.Asset.Plate==v.Plate)))return false;
                var copies=JsonConvert.DeserializeObject<System.Collections.Generic.List<PdmStock>>(JsonConvert.SerializeObject(cars));foreach(var v in copies)v.AwaitingCollection=true;d.Stock.AddRange(copies);d.Cash-=(int)total;d.StockSpent+=total;d.Bought+=cars.Count;PdmTransactions.Entry(d,"Pedido de "+cars.Count+" unidades",-(int)total);return true;
            },save,out changed);
        }
        public static bool Upgrade(WalletData w,PdmEnterpriseOptions c,string upgrade,string receipt,Action save,out bool changed)
        {
            changed=false;if(c?.Valid!=true)return false;var f=c.Facilities.Find(v=>v.Id==upgrade);int cost=f?.Price??(upgrade=="truck"?200000:upgrade=="carrier2"?65000:upgrade=="carrier6"?150000:0);if(cost<=0)return false;
            return PdmTransactions.Apply(w,receipt,0,d=>{var e=State(d);if(!PdmEnterpriseRules.Idle(d) || e.Upgrades.Contains(upgrade) || !Spend(d,e,cost,"Melhoria PDM: "+(f?.Name??upgrade)))return false;e.Upgrades.Add(upgrade);return true;},save,out changed);
        }
        public static bool ToggleOpen(WalletData w,string receipt,Action save,out bool changed)=>PdmTransactions.Apply(w,receipt,0,d=>{if(!d.Owned)return false;var e=State(d);e.Open=!e.Open;return true;},save,out changed);
        public static bool Donate(WalletData w,int amount,string receipt,Action save,out bool changed)
        {
            changed=false;if(!new[]{100,500,2500,10000,25000,100000}.Contains(amount))return false;
            return PdmTransactions.Apply(w,receipt,0,d=>{var e=State(d);if(!PdmEnterpriseRules.Idle(d) || e.Donations>long.MaxValue-amount || !Spend(d,e,amount,"Doacao beneficente PDM"))return false;e.Donations+=amount;e.CharityPending=Math.Min(5,e.CharityPending+Math.Max(1,amount/2500));return true;},save,out changed);
        }
        public static bool Sponsor(WalletData w,PdmEnterpriseOptions c,bool renew,string receipt,Action save,out bool changed)
        {
            changed=false;if(c?.Valid!=true)return false;
            return PdmTransactions.Apply(w,receipt,0,d=>{var e=State(d);if(!PdmEnterpriseRules.Idle(d))return false;if(!renew){e.SponsorRemaining=e.SponsorPayment=0;return true;}if(e.SponsorRemaining>0 || e.SponsorSpent>long.MaxValue-7500 || !Spend(d,e,7500,"Patrocinio equipe de corrida"))return false;e.SponsorSpent+=7500;e.SponsorRemaining=c.DaySeconds*10;e.SponsorPayment=c.DaySeconds*7;return true;},save,out changed);
        }
        public static bool BeginCards(WalletData w,PdmEnterpriseOptions c,string receipt,Action save,out bool changed)
        {changed=false;if(c?.Valid!=true)return false;return PdmTransactions.Apply(w,receipt,0,d=>{var e=State(d);if(!PdmEnterpriseRules.Idle(d) || e.CardsLeft>0 || !Spend(d,e,c.CardCost,"Impressao de cartoes PDM"))return false;e.CardsLeft=c.CardCount;e.CardSession=receipt;e.CardTargets.Clear();return true;},save,out changed);}
        public static bool Card(WalletData w,string session,string target,string receipt,Action save,out bool changed)
        {return PdmTransactions.Apply(w,receipt,0,d=>{var e=State(d);if(!d.Owned || e.CardSession!=session || e.CardsLeft<1 || string.IsNullOrWhiteSpace(target) || e.CardTargets.Contains(target) || e.CardsGiven==int.MaxValue)return false;e.CardTargets.Add(target);e.CardsLeft--;e.CardsGiven++;if(e.CardsLeft==0){e.Reputation=Math.Min(100,e.Reputation+2);e.CardSession=null;}return true;},save,out changed);}
        public static bool CleanTrash(WalletData w,string receipt,Action save,out bool changed,int slot=-1)=>PdmTransactions.Apply(w,receipt,0,d=>{var e=State(d);if(!d.Owned || e.TrashCount<1 || slot>=0 && !e.TrashSlots.Contains(slot))return false;e.TrashSlots.Remove(slot>=0?slot:e.TrashSlots.Last());e.Cleanliness=Math.Min(100,e.Cleanliness+10);e.Mood=Math.Min(100,e.Mood+1);return true;},save,out changed);
        public static bool Budget(WalletData w,int amount,string receipt,Action save,out bool changed)
        {changed=false;if(amount<0 || amount>1000000)return false;return PdmTransactions.Apply(w,receipt,0,d=>{if(!d.Owned || State(d).Auto!=null)return false;State(d).ProcurementBudget=amount;return true;},save,out changed);}
        public static bool Advance(WalletData w,PdmEnterpriseOptions c,int seconds,string receipt,Action save,out bool changed)
        {
            changed=false;if(c?.Valid!=true || seconds<1 || seconds>60)return false;
            return PdmTransactions.Apply(w,receipt,0,d=>{
                if(!d.Owned)return false;var e=State(d);e.DayElapsed+=seconds;e.SaleElapsed=Math.Min(86400,e.SaleElapsed+seconds);e.SellerElapsed=Math.Min(86400,e.SellerElapsed+seconds);e.SourceElapsed=Math.Min(86400,e.SourceElapsed+seconds);e.TrashElapsed+=seconds;
                if(e.Auto!=null){e.Auto.Seconds=Math.Min(86400,e.Auto.Seconds+seconds);if(e.Auto.Seconds>=120)e.Auto.AtGate=true;}
                if(e.TrashElapsed>=360){e.TrashElapsed-=360;if(e.TrashCount<4)e.TrashSlots.Add(Enumerable.Range(0,4).First(i=>!e.TrashSlots.Contains(i)));e.Cleanliness=Math.Max(0,100-e.TrashCount*20);}
                if(e.SponsorRemaining>0){e.SponsorRemaining=Math.Max(0,e.SponsorRemaining-seconds);e.SponsorPayment=Math.Max(0,e.SponsorPayment-seconds);if(e.SponsorRemaining>0 && e.SponsorPayment==0){if(e.SponsorSpent<=long.MaxValue-7500 && Spend(d,e,7500,"Renovacao de patrocinio")){e.SponsorSpent+=7500;e.SponsorPayment=c.DaySeconds*7;}else e.SponsorRemaining=0;}}
                if(e.DayElapsed>=c.DaySeconds){e.DayElapsed-=c.DaySeconds;e.Day=(int)Math.Min(int.MaxValue,(long)e.Day+1);e.Reputation=Math.Min(100,e.Reputation+e.CharityPending+(e.SponsorRemaining>0?1:0));e.CharityPending=0;e.Mood=Math.Max(0,Math.Min(100,e.Mood+(d.Staff.Employees.Any(v=>v.Owed>0)?-2:1)));}
                return true;
            },save,out changed);
        }
        public static bool RentalRequest(WalletData w,PdmRentalRequest request,int day,string receipt,Action save,out bool changed)
        {changed=false;if(request?.Valid!=true)return false;return PdmTransactions.Apply(w,receipt,0,d=>{var e=State(d);if(!d.Owned || day!=e.RentalGeneratedDay+1 || day>e.Day || e.RentalOrders.Count>=10 || e.RentalOrders.Any(r=>r.Id==request.Id))return false;e.RentalGeneratedDay=day;e.RentalOrders.Add(request);return true;},save,out changed);}
        public static bool BeginAuto(WalletData w,PdmEnterpriseOptions c,string[] ids,string receipt,Action save,out bool changed)
        {
            changed=false;if(c?.Valid!=true || ids==null || ids.Length<1 || ids.Length>6 || ids.Distinct().Count()!=ids.Length)return false;
            return PdmTransactions.Apply(w,receipt,0,d=>{var e=State(d);if(!PdmEnterpriseRules.Idle(d) || !PdmEnterpriseRules.Paid(d,"collector") || e.Seller!=null || ids.Any(id=>!d.Stock.Any(s=>s.Id==id && s.AwaitingCollection)) || !Spend(d,e,(e.Upgrades.Contains("truck") && e.Upgrades.Contains("carrier2")?0:c.AutoPickupFee*ids.Length),"Servico de coleta automatica"))return false;e.Auto=new PdmAutoDelivery{Id=receipt,StockIds=ids.ToList(),Fee=e.Upgrades.Contains("truck") && e.Upgrades.Contains("carrier2")?0:c.AutoPickupFee*ids.Length};return true;},save,out changed);
        }
        public static bool BeginSource(WalletData w,PdmOptions cfg,PdmStock stock,string receipt,Action save,out bool changed)
        {
            changed=false;if(cfg?.Valid!=true || stock?.Valid!=true)return false;
            return PdmTransactions.Apply(w,receipt,0,d=>{var e=State(d);if(!PdmEnterpriseRules.Idle(d) || !PdmEnterpriseRules.Paid(d,"collector") || d.Stock.Count>=cfg.Capacity || e.Seller!=null || stock.Cost>e.ProcurementBudget || d.Stock.Any(s=>s.Id==stock.Id || s.Plate==stock.Plate) || d.Bought==int.MaxValue || d.StockSpent>long.MaxValue-stock.Cost || d.Cash<stock.Cost)return false;d.Cash-=stock.Cost;d.StockSpent+=stock.Cost;d.Bought++;e.SourceElapsed=0;e.Auto=new PdmAutoDelivery{Id=receipt,SourcedCar=JsonConvert.DeserializeObject<PdmStock>(JsonConvert.SerializeObject(stock))};PdmTransactions.Entry(d,"Coletor / compra: "+stock.Name,-stock.Cost);return true;},save,out changed);
        }
        public static bool ReceiveAuto(WalletData w,string id,string receipt,Action save,out bool changed)
        {return PdmTransactions.Apply(w,receipt,0,d=>{var e=State(d);var a=e.Auto;if(a==null || a.Id!=id || !a.AtGate)return false;if(a.SourcedCar!=null){if(d.Stock.Count>=20)return false;a.SourcedCar.AwaitingCollection=false;d.Stock.Add(a.SourcedCar);e.Auto=null;}else{if(a.Delivered>=a.StockIds.Count)return false;var stock=d.Stock.Find(s=>s.Id==a.StockIds[a.Delivered] && s.AwaitingCollection);if(stock==null)return false;stock.AwaitingCollection=false;a.Delivered++;if(a.Delivered==a.StockIds.Count)e.Auto=null;else {a.Seconds=60;a.AtGate=false;}}return true;},save,out changed);}
        public static bool CancelAuto(WalletData w,string id,string receipt,Action save,out bool changed)
        {return PdmTransactions.Apply(w,receipt,0,d=>{var e=State(d);if(e.Auto?.Id!=id)return false;if(e.Auto.SourcedCar!=null){e.Auto.SourcedCar.AwaitingCollection=true;if(d.Stock.Count>=20)return false;d.Stock.Add(e.Auto.SourcedCar);}e.Auto=null;return true;},save,out changed);}
        public static bool InterruptAuto(WalletData w,string id,int engine,int body,string receipt,Action save,out bool changed)
        {
            changed=false;if(engine<0 || engine>1000 || body<0 || body>1000)return false;
            return PdmTransactions.Apply(w,receipt,0,d=>{var e=State(d);var a=e.Auto;if(a?.Id!=id)return false;var car=a.SourcedCar??d.Stock.Find(v=>v.Id==a.StockIds[a.Delivered]);if(car==null)return false;car.Engine=Math.Min(car.Engine,engine);car.Body=Math.Min(car.Body,body);car.AwaitingCollection=true;if(a.SourcedCar!=null){if(d.Stock.Count>=20)return false;d.Stock.Add(car);}e.Auto=null;PdmTransactions.Entry(d,"Coleta interrompida / pedido e danos preservados",0);return true;},save,out changed);
        }
        public static bool Seller(WalletData w,PdmPrivateSeller seller,string receipt,Action save,out bool changed)
        {changed=false;if(seller?.Valid!=true)return false;return PdmTransactions.Apply(w,receipt,0,d=>{var e=State(d);if(!PdmEnterpriseRules.Idle(d) || e.Seller!=null)return false;e.Seller=JsonConvert.DeserializeObject<PdmPrivateSeller>(JsonConvert.SerializeObject(seller));e.SellerElapsed=0;return true;},save,out changed);}
        public static bool InspectSeller(WalletData w,string id,SavedVehicle appearance,string receipt,Action save,out bool changed)
        {return PdmTransactions.Apply(w,receipt,0,d=>{var s=State(d).Seller;if(!d.Owned || s?.Id!=id)return false;s.Inspected=true;if(appearance!=null){if(!AppearanceValid(appearance))return false;s.Car.Appearance=JsonConvert.DeserializeObject<SavedVehicle>(JsonConvert.SerializeObject(appearance));s.Car.Engine=(int)Math.Max(0,Math.Min(1000,appearance.EngineHealth));s.Car.Body=(int)Math.Max(0,Math.Min(1000,appearance.BodyHealth));s.Car.Dirt=(int)Math.Max(0,Math.Min(15,appearance.Dirt));}return true;},save,out changed);}
        public static bool CounterSeller(WalletData w,string id,int amount,string receipt,Action save,out bool changed)
        {return PdmTransactions.Apply(w,receipt,0,d=>{var s=State(d).Seller;if(s?.Id!=id || !s.Inspected || s.Countered || amount<1 || amount>=s.Asking)return false;s.Countered=true;s.Counter=amount;if(amount<s.Minimum)s.Rejected=true;else s.Asking=amount;return true;},save,out changed);}
        public static bool BuySeller(WalletData w,PdmOptions cfg,string id,string receipt,Action save,out bool changed)
        {changed=false;if(cfg?.Valid!=true)return false;return PdmTransactions.Apply(w,receipt,0,d=>{var e=State(d);var s=e.Seller;if(!PdmEnterpriseRules.Idle(d) || s?.Id!=id || !s.Inspected || s.Rejected || d.Cash<s.Asking || d.Stock.Count>=cfg.Capacity || d.Stock.Any(v=>v.Id==s.Car.Id || v.Plate==s.Car.Plate) || d.Bought==int.MaxValue || d.StockSpent>long.MaxValue-s.Asking)return false;s.Car.Cost=s.Asking;s.Car.Price=PdmEnterpriseRules.SellingPrice(s.Asking,e.Reputation);s.Car.AwaitingCollection=false;d.Stock.Add(s.Car);d.Cash-=s.Asking;d.StockSpent+=s.Asking;d.Bought++;PdmTransactions.Entry(d,"Vendedor particular: "+s.Car.Name,-s.Asking);e.Seller=null;return true;},save,out changed);}
        public static bool DismissSeller(WalletData w,string id,string receipt,Action save,out bool changed)=>PdmTransactions.Apply(w,receipt,0,d=>{var e=State(d);if(e.Seller?.Id!=id)return false;e.Seller=null;return true;},save,out changed);
        public static bool CustomerOrder(WalletData w,string stockId,int destination,string receipt,Action save,out bool changed)
        {
            changed=false;if(destination<0 || destination>=30)return false;
            return PdmTransactions.Apply(w,receipt,0,d=>{var e=State(d);var s=d.Stock.Find(v=>v.Id==stockId);if(!PdmEnterpriseRules.Idle(d) || !e.Open || s==null || s.AwaitingCollection || PdmEnterpriseRules.Reserved(d,stockId) || d.Showroom.Any(v=>v.StockId==stockId))return false;e.CustomerOrders.Add(new PdmCustomerOrder{Id=receipt,StockId=stockId,Destination=destination});e.SaleElapsed=0;PdmTransactions.Entry(d,"Cliente aguardando entrega: "+s.Name,0);return true;},save,out changed);
        }
        public static bool AutoSell(WalletData w,string stockId,string receipt,Action save,out bool changed)
        {return PdmTransactions.Apply(w,receipt,0,d=>{var e=State(d);var s=d.Stock.Find(v=>v.Id==stockId);if(!PdmEnterpriseRules.Idle(d) || !e.Open || s==null || s.AwaitingCollection || PdmEnterpriseRules.Reserved(d,s.Id))return false;int price=PdmStaffTransactions.Quote(d,s,s.Engine,s.Body);if((long)d.Cash+price>int.MaxValue || d.Sold==int.MaxValue || d.Revenue>long.MaxValue-price || d.SoldCost>long.MaxValue-s.Cost)return false;d.Showroom.RemoveAll(v=>v.StockId==s.Id);d.Stock.Remove(s);d.Cash+=price;d.Sold++;d.Revenue+=price;d.SoldCost+=s.Cost;e.SaleElapsed=0;e.Reputation=Math.Min(100,e.Reputation+1);PdmTransactions.Entry(d,"Venda pela equipe: "+s.Name,price);return true;},save,out changed);}
        public static bool AppearanceValid(SavedVehicle v)=>v!=null && new[]{v.EngineHealth,v.BodyHealth,v.TankHealth,v.Dirt,v.Fuel,v.X,v.Y,v.Z,v.Heading}.All(n=>!float.IsNaN(n) && !float.IsInfinity(n)) && v.Dirt>=0 && v.Dirt<=15 && v.Fuel>=0 && v.Mods!=null && v.Toggles!=null;
        public static bool Service(WalletData w,string stockId,string kind,int cost,SavedVehicle appearance,string receipt,Action save,out bool changed)
        {return PdmTransactions.Apply(w,receipt,0,d=>{var e=State(d);var s=d.Stock.Find(v=>v.Id==stockId);if(!PdmEnterpriseRules.Idle(d) || s==null || s.AwaitingCollection || PdmEnterpriseRules.Reserved(d,s.Id) || !new[]{"wash","fuel","tune"}.Contains(kind) || cost<0 || !AppearanceValid(appearance) || kind=="wash" && (cost!=0 || s.Dirt==0) || kind=="fuel" && (s.FuelFraction>=1 || cost==0) || kind=="tune" && (cost==0 || s.Appearance!=null && JsonConvert.SerializeObject(s.Appearance)==JsonConvert.SerializeObject(appearance)) || e.ServicesSpent>long.MaxValue-cost || kind=="wash" && !e.Upgrades.Contains("washer") || kind=="fuel" && !e.Upgrades.Contains("pump") || kind=="tune" && (!d.Staff.Workshop || !PdmEnterpriseRules.Paid(d,"mechanic")) || !Spend(d,e,cost,"Servico "+kind+": "+s.Name))return false;e.ServicesSpent+=cost;if(kind=="wash")s.Dirt=0;if(kind=="fuel")s.FuelFraction=1;s.Appearance=JsonConvert.DeserializeObject<SavedVehicle>(JsonConvert.SerializeObject(appearance));return true;},save,out changed);}
    }
}
