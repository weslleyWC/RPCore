using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Xunit;
namespace RPCore.Tests
{
    public class PdmEnterpriseTests
    {
        static PdmEnterpriseOptions C=>PdmEnterpriseDefaults.Load();
        static PdmOptions Options=>PdmDefaults.Load();
        static PdmStaffOptions Staff=>PdmEnterpriseDefaults.Merge(PdmSecurityDefaults.Merge(PdmPremisesDefaults.Merge(PdmStaffDefaults.Load(),PdmPremisesDefaults.Load()),PdmSecurityDefaults.Load()),C);
        static WalletData Wallet()
        {
            var w=new WalletData{Cash=777,Bank=2000000};
            Assert.True(PdmTransactions.Purchase(w,Options,"own",()=>{},out _));
            Assert.True(PdmTransactions.Transfer(w,1500000,true,"capital",()=>{},out _));
            return w;
        }
        static PdmStock Car(string id="unit01",bool pending=true)=>new PdmStock{Id=id,Model="asea",Name="Asea",Plate=id.ToUpperInvariant(),Cost=8000,Price=10000,Mileage=5000,AwaitingCollection=pending};
        static void Collector(WalletData w){Assert.True(PdmStaffTransactions.Hire(w,Staff,"collector","collector",()=>{},out _));}
        static string Json(object value)=>JsonConvert.SerializeObject(value);
        static void Tick(WalletData w,int seconds)
        {while(seconds>0){int n=Math.Min(60,seconds);Assert.True(PdmEnterpriseTransactions.Advance(w,C,n,Guid.NewGuid().ToString("N"),()=>{},out _));seconds-=n;}}
        static void Rollback(WalletData w,Action<Action> action)
        {string before=Json(w);Assert.Throws<System.IO.IOException>(()=>action(()=>throw new System.IO.IOException("disk")));Assert.Equal(before,Json(w));}
        [Fact] public void DefaultsMergeWithoutMutatingExistingCustomStaffAndFutureMetadata()
        {
            var original=PdmStaffDefaults.Load();string before=Json(original);var merged=PdmEnterpriseDefaults.Merge(original,C);
            Assert.True(C.Valid);Assert.True(merged.Valid);Assert.Equal(before,Json(original));Assert.Equal(original.Roles.Count+1,merged.Roles.Count);Assert.Single(merged.Roles,v=>v.Id=="collector");
            Assert.Equal(Json(merged),Json(PdmEnterpriseDefaults.Merge(merged,C)));
        }
        [Theory] [InlineData("glass",4000)] [InlineData("lights",12000)] [InlineData("washer",2000)] [InlineData("pump",25000)] [InlineData("truck",200000)] [InlineData("carrier2",65000)] [InlineData("carrier6",150000)]
        public void FacilityChargesCompanyExactlyOnceAndIsAtomic(string upgrade,int cost)
        {
            var w=Wallet();int cash=w.Pdm.Cash,bank=w.Bank;var token="upgrade:"+upgrade;int saves=0;
            Assert.True(PdmEnterpriseTransactions.Upgrade(w,C,upgrade,token,()=>saves++,out bool changed));Assert.True(changed);Assert.Equal(cash-cost,w.Pdm.Cash);Assert.Equal(bank,w.Bank);Assert.Equal(777,w.Cash);
            Assert.True(PdmEnterpriseTransactions.Upgrade(w,C,upgrade,token,()=>throw new Exception(),out changed));Assert.False(changed);Assert.Equal(1,saves);
            Assert.False(PdmEnterpriseTransactions.Upgrade(w,C,upgrade,token+"again",()=>{},out _));Assert.Equal(cost,w.Pdm.Enterprise.Expenses);
            var fresh=Wallet();Rollback(fresh,save=>PdmEnterpriseTransactions.Upgrade(fresh,C,upgrade,token,save,out _));
        }
        [Fact] public void InsolventUpgradeCannotCreateFacilityOrDebitPersonalBalances()
        {var w=Wallet();w.Pdm.Cash=0;string before=Json(w);Assert.False(PdmEnterpriseTransactions.Upgrade(w,C,"glass","upgrade",()=>{},out _));Assert.Equal(before,Json(w));}
        [Fact] public void PaidOrdersHaveOneServiceFeeAndSequentialDeliveryWithoutRepurchase()
        {
            var w=Wallet();Collector(w);w.Pdm.Stock.Add(Car());w.Pdm.Stock.Add(Car("unit02"));int cash=w.Pdm.Cash;
            Assert.True(PdmEnterpriseTransactions.BeginAuto(w,C,new[]{"unit01","unit02"},"auto",()=>{},out _));Assert.Equal(cash-500,w.Pdm.Cash);
            Assert.False(PdmEnterpriseTransactions.ReceiveAuto(w,"auto","too-soon",()=>{},out _));
            Assert.False(PdmTransactions.Begin(w,Options,"unit01","manual",0,100,()=>{},out _));
            Tick(w,120);Assert.True(PdmEnterpriseTransactions.ReceiveAuto(w,"auto","receive0",()=>{},out _));Assert.False(w.Pdm.Stock[0].AwaitingCollection);Assert.True(w.Pdm.Stock[1].AwaitingCollection);Assert.False(w.Pdm.Enterprise.Auto.AtGate);
            Assert.False(PdmEnterpriseTransactions.ReceiveAuto(w,"auto","too-soon2",()=>{},out _));Tick(w,60);
            Rollback(w,save=>PdmEnterpriseTransactions.ReceiveAuto(w,"auto","receive1",save,out _));
            Assert.True(PdmEnterpriseTransactions.ReceiveAuto(w,"auto","receive1",()=>{},out _));Assert.Null(w.Pdm.Enterprise.Auto);Assert.All(w.Pdm.Stock,s=>Assert.False(s.AwaitingCollection));Assert.Equal(cash-500,w.Pdm.Cash);
            Assert.True(PdmEnterpriseTransactions.ReceiveAuto(w,"auto","receive1",()=>throw new Exception(),out bool changed));Assert.False(changed);
        }
        [Theory] [InlineData(false)] [InlineData(true)]
        public void CollectorSourcesBudgetedCarOnceAndCancellationPreservesPaidAsset(bool receive)
        {
            var w=Wallet();Collector(w);var car=Car();int cash=w.Pdm.Cash;
            Rollback(w,save=>PdmEnterpriseTransactions.BeginSource(w,Options,car,"source",save,out _));
            Assert.True(PdmEnterpriseTransactions.BeginSource(w,Options,car,"source",()=>{},out _));Assert.Equal(cash-car.Cost,w.Pdm.Cash);Assert.Empty(w.Pdm.Stock);Assert.Equal(1,w.Pdm.Bought);
            car.Name="external mutation";Assert.Equal("Asea",w.Pdm.Enterprise.Auto.SourcedCar.Name);
            if(receive){Tick(w,120);Assert.True(PdmEnterpriseTransactions.ReceiveAuto(w,"source","receive",()=>{},out _));}
            else Assert.True(PdmEnterpriseTransactions.CancelAuto(w,"source","cancel",()=>{},out _));
            Assert.Null(w.Pdm.Enterprise.Auto);Assert.Single(w.Pdm.Stock);Assert.Equal(!receive,w.Pdm.Stock[0].AwaitingCollection);Assert.Equal(cash-8000,w.Pdm.Cash);
        }
        [Theory] [InlineData("worker")] [InlineData("debt")] [InlineData("budget")] [InlineData("cash")] [InlineData("full")] [InlineData("duplicate")]
        public void SourceRejectsUnaffordableOrUnavailablePurchasesWithoutMutation(string reason)
        {
            var w=Wallet();Collector(w);w.Pdm.Enterprise=new PdmEnterpriseData();
            if(reason=="worker")w.Pdm.Staff.Employees.Clear();if(reason=="debt")w.Pdm.Staff.Employees[0].Owed=1;if(reason=="budget")w.Pdm.Enterprise.ProcurementBudget=1;if(reason=="cash")w.Pdm.Cash=1;
            if(reason=="full")for(int i=0;i<Options.Capacity;i++)w.Pdm.Stock.Add(Car("unit"+i));if(reason=="duplicate")w.Pdm.Stock.Add(Car());
            string before=Json(w);Assert.False(PdmEnterpriseTransactions.BeginSource(w,Options,Car(),"source",()=>{},out _));Assert.Equal(before,Json(w));
        }
        [Fact] public void OwnedTwoCarFleetEliminatesOnlyServiceFeeAndSixFleetEnablesSixSlots()
        {
            var w=Wallet();Collector(w);w.Pdm.Stock.Add(Car());
            foreach(string upgrade in new[]{"truck","carrier2","carrier6"})Assert.True(PdmEnterpriseTransactions.Upgrade(w,C,upgrade,upgrade,()=>{},out _));
            Assert.Equal(6,PdmEnterpriseRules.Capacity(w.Pdm));int cash=w.Pdm.Cash;Assert.True(PdmEnterpriseTransactions.BeginAuto(w,C,new[]{"unit01"},"auto",()=>{},out _));Assert.Equal(cash,w.Pdm.Cash);
            Assert.Equal(new[]{5f,0f,-5f,5f,0f,-5f},Enumerable.Range(0,6).Select(i=>PdmEnterpriseRules.Slot(6,i).Y));Assert.Equal(new[]{3f,3f,3f,1f,1f,1f},Enumerable.Range(0,6).Select(i=>PdmEnterpriseRules.Slot(6,i).Z));
        }
        [Fact] public void CardsRequireDistinctRecipientsAndCompleteCampaignBeforeReputation()
        {
            var w=Wallet();int cash=w.Pdm.Cash;Assert.True(PdmEnterpriseTransactions.BeginCards(w,C,"campaign",()=>{},out _));Assert.Equal(cash-C.CardCost,w.Pdm.Cash);
            Assert.False(PdmEnterpriseTransactions.Card(w,"wrong","ped0","bad",()=>{},out _));
            for(int i=0;i<C.CardCount;i++){Assert.True(PdmEnterpriseTransactions.Card(w,"campaign","ped"+i,"card"+i,()=>{},out _));Assert.False(PdmEnterpriseTransactions.Card(w,"campaign","ped"+i,"repeat"+i,()=>{},out _));if(i<C.CardCount-1)Assert.Equal(0,w.Pdm.Enterprise.Reputation);}
            Assert.Equal(2,w.Pdm.Enterprise.Reputation);Assert.Equal(0,w.Pdm.Enterprise.CardsLeft);Assert.Null(w.Pdm.Enterprise.CardSession);
        }
        [Fact] public void DonationIsExpenseNowReputationNextDayAndNoOfflineMint()
        {
            var w=Wallet();int cash=w.Pdm.Cash;Assert.True(PdmEnterpriseTransactions.Donate(w,2500,"donate",()=>{},out _));Assert.Equal(cash-2500,w.Pdm.Cash);Assert.Equal(0,w.Pdm.Enterprise.Reputation);
            var loaded=JsonConvert.DeserializeObject<WalletData>(Json(w));Assert.Equal(0,loaded.Pdm.Enterprise.Day);Tick(loaded,C.DaySeconds);Assert.Equal(1,loaded.Pdm.Enterprise.Reputation);Assert.Equal(0,loaded.Pdm.Enterprise.CharityPending);Assert.Equal(cash-2500,loaded.Pdm.Cash);
        }
        [Theory] [InlineData(true)] [InlineData(false)]
        public void SponsorshipRenewsOnlyWithCompanyCashAndCanStop(bool funds)
        {
            var w=Wallet();Assert.True(PdmEnterpriseTransactions.Sponsor(w,C,true,"sponsor",()=>{},out _));int initial=w.Pdm.Cash;if(!funds)w.Pdm.Cash=0;Tick(w,C.DaySeconds*7);
            Assert.Equal(funds?15000:7500,w.Pdm.Enterprise.SponsorSpent);Assert.Equal(funds?initial-7500:0,w.Pdm.Cash);if(!funds)Assert.Equal(0,w.Pdm.Enterprise.SponsorRemaining);
            Assert.True(PdmEnterpriseTransactions.Sponsor(w,C,false,"stop",()=>{},out _));int cash=w.Pdm.Cash;Tick(w,C.DaySeconds*8);Assert.Equal(cash,w.Pdm.Cash);
        }
        [Fact] public void TrashPersistsExactSlotsSoCleaningMiddleBagDoesNotDeleteAnother()
        {
            var w=Wallet();Tick(w,1080);Assert.Equal(new[]{0,1,2},w.Pdm.Enterprise.TrashSlots);
            Assert.True(PdmEnterpriseTransactions.CleanTrash(w,"clean",()=>{},out _,1));Assert.Equal(new[]{0,2},w.Pdm.Enterprise.TrashSlots);Assert.False(PdmEnterpriseTransactions.CleanTrash(w,"invalid",()=>{},out _,1));Tick(w,360);Assert.Equal(new[]{0,2,1},w.Pdm.Enterprise.TrashSlots);
        }
        static PdmPrivateSeller Seller()=>new PdmPrivateSeller{Id="seller",Car=Car("seller01",false),Asking=10000,Minimum=8500};
        [Theory] [InlineData(9000,true)] [InlineData(8000,false)]
        public void SellerMustBeInspectedAndAcceptCounterBeforeCompanyPurchase(int counter,bool accepted)
        {
            var w=Wallet();int cash=w.Pdm.Cash;Assert.True(PdmEnterpriseTransactions.Seller(w,Seller(),"offer",()=>{},out _));Assert.False(PdmEnterpriseTransactions.BuySeller(w,Options,"seller","buyearly",()=>{},out _));
            var look=new SavedVehicle{Model=123,EngineHealth=850,BodyHealth=700,Dirt=3,Mods=new Dictionary<int,int>{{11,2}}};Assert.True(PdmEnterpriseTransactions.InspectSeller(w,"seller",look,"inspect",()=>{},out _));
            Assert.True(PdmEnterpriseTransactions.CounterSeller(w,"seller",counter,"counter",()=>{},out _));Assert.False(PdmEnterpriseTransactions.CounterSeller(w,"seller",8500,"countertwice",()=>{},out _));
            Assert.Equal(accepted,PdmEnterpriseTransactions.BuySeller(w,Options,"seller","buy",()=>{},out _));Assert.Equal(accepted?cash-counter:cash,w.Pdm.Cash);
            if(accepted){Assert.Null(w.Pdm.Enterprise.Seller);Assert.Equal(850,w.Pdm.Stock[0].Engine);Assert.Equal(2,w.Pdm.Stock[0].Appearance.Mods[11]);look.Mods[11]=0;Assert.Equal(2,w.Pdm.Stock[0].Appearance.Mods[11]);}
        }
        [Fact] public void SellerPurchaseDiskFailurePreservesOfferAndMoney()
        {var w=Wallet();Assert.True(PdmEnterpriseTransactions.Seller(w,Seller(),"offer",()=>{},out _));Assert.True(PdmEnterpriseTransactions.InspectSeller(w,"seller",null,"inspect",()=>{},out _));Rollback(w,save=>PdmEnterpriseTransactions.BuySeller(w,Options,"seller","buy",save,out _));}
        [Fact] public void SalesOrdersReserveCarWithoutCreditingMoneyAndConsumeOnlyAfterHandover()
        {
            var w=Wallet();w.Pdm.Stock.Add(Car(pending:false));int cash=w.Pdm.Cash;Assert.True(PdmEnterpriseTransactions.CustomerOrder(w,"unit01",0,"order",()=>{},out _));Assert.Equal(cash,w.Pdm.Cash);Assert.True(PdmEnterpriseRules.Reserved(w.Pdm,"unit01"));
            Assert.False(PdmEnterpriseTransactions.AutoSell(w,"unit01","other-sale",()=>{},out _));Assert.True(PdmTransactions.Begin(w,Options,"unit01","delivery",0,123,()=>{},out _));Assert.True(PdmTransactions.Cancel(w,"delivery",1000,1000,()=>{},out _));Assert.Single(w.Pdm.Enterprise.CustomerOrders);
            Assert.True(PdmTransactions.Begin(w,Options,"unit01","delivery2",0,123,()=>{},out _));Rollback(w,save=>PdmTransactions.Sell(w,"delivery2",1000,1000,save,out _));Assert.True(PdmTransactions.Sell(w,"delivery2",1000,1000,()=>{},out _));Assert.Empty(w.Pdm.Enterprise.CustomerOrders);Assert.Empty(w.Pdm.Stock);Assert.Equal(cash+10000,w.Pdm.Cash);
        }
        [Fact] public void AutomaticSaleRequiresOpenBusinessAndRealReceivedStock()
        {
            var w=Wallet();w.Pdm.Stock.Add(Car());Assert.False(PdmEnterpriseTransactions.AutoSell(w,"unit01","pending",()=>{},out _));w.Pdm.Stock[0].AwaitingCollection=false;
            Assert.True(PdmEnterpriseTransactions.ToggleOpen(w,"close",()=>{},out _));Assert.False(PdmEnterpriseTransactions.AutoSell(w,"unit01","closed",()=>{},out _));Assert.True(PdmEnterpriseTransactions.ToggleOpen(w,"open",()=>{},out _));
            Rollback(w,save=>PdmEnterpriseTransactions.AutoSell(w,"unit01","sale",save,out _));Assert.True(PdmEnterpriseTransactions.AutoSell(w,"unit01","sale",()=>{},out _));Assert.Equal(1,w.Pdm.Sold);Assert.Empty(w.Pdm.Stock);
        }
        [Theory] [InlineData("wash","washer",0)] [InlineData("fuel","pump",150)] [InlineData("tune","",1000)]
        public void ServicesRequireEquipmentAndPreserveMoneyAndSnapshotOnDiskFailure(string kind,string equipment,int cost)
        {
            var w=Wallet();var car=Car(pending:false);car.Dirt=10;car.FuelFraction=.2f;w.Pdm.Stock.Add(car);var look=new SavedVehicle{Model=123,Dirt=0,Fuel=60,Mods=new Dictionary<int,int>{{11,3}}};
            Assert.False(PdmEnterpriseTransactions.Service(w,"unit01",kind,cost,look,"no-equipment",()=>{},out _));
            if(kind=="tune"){Assert.True(PdmStaffTransactions.Hire(w,Staff,"mechanic","mechanic",()=>{},out _));w.Pdm.Staff.Workshop=true;}else Assert.True(PdmEnterpriseTransactions.Upgrade(w,C,equipment,equipment,()=>{},out _));
            int cash=w.Pdm.Cash;Rollback(w,save=>PdmEnterpriseTransactions.Service(w,"unit01",kind,cost,look,"service",save,out _));Assert.True(PdmEnterpriseTransactions.Service(w,"unit01",kind,cost,look,"service",()=>{},out _));Assert.Equal(cash-cost,w.Pdm.Cash);Assert.Equal(3,w.Pdm.Stock[0].Appearance.Mods[11]);
            look.Mods[11]=1;Assert.Equal(3,w.Pdm.Stock[0].Appearance.Mods[11]);Assert.True(PdmEnterpriseTransactions.Service(w,"unit01",kind,cost,look,"service",()=>throw new Exception(),out bool changed));Assert.False(changed);
        }
        [Fact] public void BatchCheckoutIsAtomicAndNeverLeavesPartialPaidOrders()
        {
            var w=Wallet();var cars=Enumerable.Range(1,6).Select(i=>Car("unit0"+i)).ToList();int cash=w.Pdm.Cash;Rollback(w,save=>PdmEnterpriseTransactions.OrderBatch(w,Options,cars,"batch",save,out _));Assert.True(PdmEnterpriseTransactions.OrderBatch(w,Options,cars,"batch",()=>{},out _));Assert.Equal(cash-48000,w.Pdm.Cash);Assert.Equal(6,w.Pdm.Bought);Assert.Equal(6,w.Pdm.Stock.Count);Assert.All(w.Pdm.Stock,s=>Assert.True(s.AwaitingCollection));
            string before=Json(w);Assert.False(PdmEnterpriseTransactions.OrderBatch(w,Options,cars,"duplicate",()=>{},out _));Assert.Equal(before,Json(w));
        }
        [Theory] [InlineData("cash")] [InlineData("former")] [InlineData("payroll")] [InlineData("duplicate")]
        public void GuardHiringExplainsRefusalInsteadOfSilentlyFailing(string reason)
        {
            var w=Wallet();if(reason=="cash")w.Pdm.Cash=0;if(reason=="former")w.Pdm.Staff.FormerDebt=100;if(reason=="payroll"){Collector(w);w.Pdm.Staff.Employees[0].Owed=1;}if(reason=="duplicate")Assert.True(PdmStaffTransactions.Hire(w,Staff,"security1","guard",()=>{},out _));
            Assert.False(string.IsNullOrWhiteSpace(PdmStaffTransactions.HireReason(w.Pdm,Staff,"security1")));string before=Json(w);Assert.False(PdmStaffTransactions.Hire(w,Staff,"security1","guard2",()=>{},out _));Assert.Equal(before,Json(w));
        }
        [Fact] public void CustomerButtonIsOnePulsePerPressAndOnlyOneConsumerEvenAtOffice()
        {
            var p=new PdmCustomerPulse();p.Sample(1,true,false);Assert.True(p.Take(true));Assert.False(p.Take(true));p.Sample(1,true,false);Assert.False(p.Take(true));p.Sample(2,true,false);Assert.False(p.Take(true));p.Sample(3,false,false);p.Sample(4,true,false);p.Block();Assert.False(p.Take(true));p.Sample(5,false,true);Assert.False(p.Take(false));Assert.True(p.Take(true));
        }
        [Theory] [InlineData(3,3,1,180,true)] [InlineData(3.1f,0,0,0,false)] [InlineData(0,0,1.1f,0,false)] [InlineData(0,0,0,181,false)] [InlineData(float.NaN,0,0,0,false)]
        public void ShowroomOffsetsCannotMoveCarsOutsideBoundedSpace(float x,float y,float z,float h,bool valid)=>Assert.Equal(valid,PdmShowroomPlacement.Valid(x,y,z,h));
        [Fact] public void OptionalEnterpriseDataKeepsOldWalletAndUnknownFutureFields()
        {
            var w=Wallet();Assert.Null(w.Pdm.Enterprise);var root=Newtonsoft.Json.Linq.JObject.Parse(Json(w));root["Pdm"]["Future"]="keep";w=JsonConvert.DeserializeObject<WalletData>(root.ToString());Assert.True(PdmEnterpriseTransactions.Donate(w,100,"donate",()=>{},out _));Assert.Equal("keep",Newtonsoft.Json.Linq.JObject.Parse(Json(w))["Pdm"]["Future"]);Assert.True(w.Pdm.Valid);
        }
        [Fact] public void SixCarCollectionReceivesExactlySixWithoutNewVehicleCharge()
        {
            var w=Wallet();for(int i=1;i<=6;i++)w.Pdm.Stock.Add(Car("unit0"+i));var ids=w.Pdm.Stock.Select(v=>v.Id).ToList();var truck=new PdmEntityRef{Handle=10,Model="packer",Plate="TRUCK"};var trailer=new PdmEntityRef{Handle=20,Model="tr2",Plate="TRAILER"};
            Assert.False(PdmCollectionTransactions.Begin(w,"no-gear",ids,truck,trailer,()=>{},out _));foreach(var upgrade in new[]{"truck","carrier6"})Assert.True(PdmEnterpriseTransactions.Upgrade(w,C,upgrade,upgrade,()=>{},out _));int cash=w.Pdm.Cash;
            Assert.True(PdmCollectionTransactions.Begin(w,"six",ids,truck,trailer,()=>{},out _));Assert.Equal(6,w.Pdm.Collection.Capacity);
            Assert.False(PdmCollectionTransactions.Arrive(w,"six",()=>{},out _));for(int i=0;i<6;i++)Assert.True(PdmCollectionTransactions.Register(w,"six",ids[i],100+i,()=>{},out _));Assert.True(PdmCollectionTransactions.Arrive(w,"six",()=>{},out _));
            Rollback(w,save=>PdmCollectionTransactions.Complete(w,"six",save,out _));Assert.True(PdmCollectionTransactions.Complete(w,"six",()=>{},out _));Assert.All(w.Pdm.Stock,v=>Assert.False(v.AwaitingCollection));Assert.Equal(cash,w.Pdm.Cash);Assert.Equal(6,w.Pdm.Stock.Count);
        }
        [Fact] public void CustomerOrderCannotBeStolenByRentalAndFinancedDeliveryConsumesOrder()
        {
            var w=Wallet();w.Pdm.Stock.Add(Car(pending:false));w.Pdm.Stock.Add(Car("unit02",false));Assert.True(PdmEnterpriseTransactions.CustomerOrder(w,"unit01",0,"order",()=>{},out _));
            Assert.False(PdmRentalTransactions.Begin(w,PdmRentalDefaults.Load(),"rental",new List<string>{"unit01","unit02"},new PdmEntityRef{Handle=10,Model="packer",Plate="TRUCK"},new PdmEntityRef{Handle=20,Model="trflat",Plate="TRAILER"},()=>{},out _));
            Assert.True(PdmTransactions.Begin(w,Options,"unit01","finance",0,123,()=>{},out _,true));Assert.True(PdmFinanceTransactions.Open(w,PdmFinanceDefaults.Load(),"finance",1000,1000,()=>{},out _));Assert.Empty(w.Pdm.Enterprise.CustomerOrders);Assert.Single(w.Pdm.Stock);Assert.Single(w.Pdm.Loans);Assert.True(w.Pdm.Valid);
        }
        [Theory] [InlineData("session")] [InlineData("recipient")] [InlineData("appearance")] [InlineData("order")]
        public void InvalidNewStateNeverOverwritesOriginalWallet(string invalid)
        {
            var w=Wallet();w.Pdm.Enterprise=new PdmEnterpriseData();if(invalid=="session")w.Pdm.Enterprise.CardsLeft=1;if(invalid=="recipient")w.Pdm.Enterprise.CardTargets=null;if(invalid=="appearance"){var v=Car();v.Appearance=new SavedVehicle{Dirt=float.NaN};w.Pdm.Stock.Add(v);}if(invalid=="order")w.Pdm.Enterprise.CustomerOrders.Add(new PdmCustomerOrder{Id="order",StockId="missing"});Assert.False(w.Pdm.Valid);string before=Json(w);Assert.False(PdmEnterpriseTransactions.Donate(w,100,"donation",()=>throw new Exception(),out _));Assert.Equal(before,Json(w));
        }
        [Fact] public void DailyOfferSeedSpansRealMileageAndDoesNotOnlySelectFirstHundredModels()
        {
            var values=Enumerable.Range(0,500).Select(i=>PdmEnterpriseRules.Seed("model:"+i)).ToArray();Assert.All(values,v=>Assert.InRange(v,0,int.MaxValue));Assert.Contains(values,v=>v%24001>20000);Assert.True(values.Select(v=>v%500).Distinct().Count()>200);Assert.Equal(PdmEnterpriseRules.Seed("day1:model"),PdmEnterpriseRules.Seed("day1:model"));
        }
        [Fact] public void FireAllKeepsWageAndSeveranceObligationsAndRollsBackTogether()
        {
            var w=Wallet();Collector(w);Assert.True(PdmStaffTransactions.Hire(w,Staff,"security1","guard",()=>{},out _));foreach(var e in w.Pdm.Staff.Employees){e.Owed=100;e.Seconds=30;}
            long due=w.Pdm.Staff.Employees.Sum(e=>100L+((long)e.Seconds*e.Wage+e.PeriodSeconds-1)/e.PeriodSeconds+e.Severance);int cash=w.Pdm.Cash;Rollback(w,save=>PdmStaffTransactions.FireAll(w,"fire",save,out _));Assert.True(PdmStaffTransactions.FireAll(w,"fire",()=>{},out _));Assert.Empty(w.Pdm.Staff.Employees);Assert.Equal(due,w.Pdm.Staff.FormerDebt);Assert.Equal(cash,w.Pdm.Cash);
        }
        [Theory] [InlineData(0,true)] [InlineData(1,true)] [InlineData(2,true)] [InlineData(-1,false)] [InlineData(3,false)]
        public void RepoVariantsPersistWithoutDroppingLoanOrCreatingMoney(int variant,bool allowed)
        {
            var w=Wallet();w.Pdm.Stock.Add(Car(pending:false));Assert.True(PdmTransactions.Begin(w,Options,"unit01","loan",0,123,()=>{},out _,true));Assert.True(PdmFinanceTransactions.OpenRisky(w,PdmFinanceDefaults.Load(),"loan",1000,1000,100,()=>{},out _));w.Pdm.Loans[0].Defaulted=true;w.Pdm.Loans[0].Elapsed=w.Pdm.Loans[0].PeriodSeconds;
            int cash=w.Pdm.Cash;var op=new PdmRepoOperation{Id="repo",LoanId="loan",Variant=variant,Location=0,Truck=new PdmEntityRef{Handle=10,Model="packer",Plate="TRUCK"},Trailer=new PdmEntityRef{Handle=20,Model="trflat",Plate="TRAILER"}};
            Assert.Equal(allowed,PdmRepoTransactions.Begin(w,Options,PdmRepoDefaults.Load(),op,()=>{},out _));Assert.Equal(cash,w.Pdm.Cash);Assert.Empty(w.Pdm.Stock);if(allowed){var loaded=JsonConvert.DeserializeObject<WalletData>(Json(w));Assert.True(loaded.Pdm.Valid);Assert.Equal(variant,loaded.Pdm.Repo.Variant);Assert.True(PdmRepoTransactions.Cancel(w,"repo",()=>{},out _));Assert.True(w.Pdm.Loans[0].Defaulted);}
        }
        [Fact] public void DestroyedCollectorCannotRespawnFreeRepairedCarOrLosePaidSource()
        {
            var w=Wallet();Collector(w);Assert.True(PdmEnterpriseTransactions.BeginSource(w,Options,Car(),"source",()=>{},out _));int cash=w.Pdm.Cash;Rollback(w,save=>PdmEnterpriseTransactions.InterruptAuto(w,"source",0,100,"damage",save,out _));Assert.True(PdmEnterpriseTransactions.InterruptAuto(w,"source",0,100,"damage",()=>{},out _));Assert.Null(w.Pdm.Enterprise.Auto);Assert.Single(w.Pdm.Stock);Assert.True(w.Pdm.Stock[0].AwaitingCollection);Assert.Equal(0,w.Pdm.Stock[0].Engine);Assert.Equal(cash,w.Pdm.Cash);Assert.True(PdmTransactions.Repair(w,Options,"unit01","repair",()=>{},out _));Assert.Equal(cash-Options.RepairPrice,w.Pdm.Cash);
        }
    }
}
