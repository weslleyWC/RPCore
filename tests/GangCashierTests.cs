using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangCashierTests
    {
        static readonly Action Save=()=>{};static GangTurfDefinition A()=>GangTurfOptions.Defaults().Areas.Single(a=>a.Id==15);static GangState S()=>new GangState{Gang=A().DefaultOwner};static WalletData W()=>new WalletData{Bank=10000,Cash=123};
        static bool Begin(WalletData w,string id="till",GangCashierOptions c=null,Action save=null)=>GangCashierTransactions.Begin(w,S(),A(),c??new GangCashierOptions(),id,save??Save,out _);
        static GangCashierOrder O(string id="c0")=>new GangCashierOrder{Id=id,Model="a_m_y_business_01",Basket={0,37},Route={0,2}};
        static bool Customer(WalletData w,string id="c0",Action save=null)=>GangCashierTransactions.Customer(w,S(),A(),"till",O(id),save??Save,out _);
        static bool Scan(WalletData w,int index=0,string id="c0",Action save=null)=>GangCashierTransactions.Scan(w,S(),A(),"till",id,index,save??Save,out _);
        static bool Pay(WalletData w,string id="c0",Action save=null)=>GangCashierTransactions.Pay(w,S(),A(),"till",id,save??Save,out _);
        static bool Update(WalletData w,int elapsed=10000,bool faulted=false,Action save=null)=>GangCashierTransactions.Update(w,S(),A(),"till",new GangCashierProgress{Elapsed=elapsed,Faulted=faulted},save??Save,out _);
        static bool Complete(WalletData w,Action save=null)=>GangCashierTransactions.Complete(w,S(),A(),"till",()=>w.Cash,n=>w.Cash=n,save??Save,out _);
        static bool Cancel(WalletData w,Action save=null)=>GangCashierTransactions.Cancel(w,"till",save??Save,out _);
        static WalletData Started(){var w=W();Assert.True(Begin(w));return w;}
        static WalletData Ordered(){var w=Started();Assert.True(Customer(w));return w;}
        static WalletData Ready(){var w=Started();for(int i=0;i<5;i++){string id="c"+i;Assert.True(Customer(w,id));Assert.True(Scan(w,0,id));Assert.True(Scan(w,1,id));Assert.True(Pay(w,id));}return w;}
        [Fact] public void DefaultsMatchSourceCatalogModelsShelvesAndCounterPositions()
        {var c=new GangCashierOptions();Assert.True(c.Valid);Assert.Equal(52,c.Products.Count);Assert.Equal(40,c.Customers.Count);Assert.Equal(6,c.Shelves.Count);Assert.Equal(2,c.Spawns.Count);Assert.Equal(5,c.Goal);Assert.Equal(80,c.Wage);Assert.Equal(600,c.SecondsLimit);Assert.Equal(-3040.46f,c.Stand.X);Assert.Equal(26.97503f,c.Stand.Heading);Assert.Equal(-3041.154f,c.Till.X);Assert.Equal(205.6297f,c.Till.Heading);Assert.Equal(600,c.Products.Single(p=>p.Id=="Phone").Price);Assert.Equal(2,c.Products.Single(p=>p.Id=="Water").Price);Assert.Equal(50,c.Products.Single(p=>p.Id=="FuelCan").Price);}
        [Fact] public void BeginIsFreeAndDoesNotCreateSalaryDirtyOrBonus()
        {var w=W();w.Bank=0;Assert.True(Begin(w));Assert.Equal(123,w.Cash);Assert.Null(w.GangAutoLaunder);Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);Assert.Empty(w.GangCashier.Accounts);}
        [Fact] public void RequiresOwnedTerritoryFifteenAndCapturesConfiguration()
        {var w=W();Assert.False(GangCashierTransactions.Begin(w,new GangState{Gang="Other"},A(),new GangCashierOptions(),"bad",Save,out _));var c=new GangCashierOptions();Begin(w,c:c);c.Products[0].Price=999;c.Shelves[0].X=0;c.Wage=1000;Assert.Equal(2,w.GangCashier.Pending.Options.Products[0].Price);Assert.Equal(-3042.437f,w.GangCashier.Pending.Options.Shelves[0].X);Assert.Equal(80,w.GangCashier.Pending.Options.Wage);}
        [Fact] public void BasketAllowsDuplicatesButRouteMustBeUniqueAndBounded()
        {var c=new GangCashierOptions();var o=O();o.Basket[1]=0;Assert.True(o.Matches(c));o.Route[1]=0;Assert.False(o.Matches(c));o=O();o.Basket.AddRange(Enumerable.Repeat(0,6));Assert.False(o.Matches(c));o=O();o.Basket[0]=52;Assert.False(o.Matches(c));}
        [Fact] public void OrderIsCapturedAndCannotRerollWhilePending()
        {var w=Started();var o=O();Assert.True(GangCashierTransactions.Customer(w,S(),A(),"till",o,Save,out _));o.Basket[0]=20;o.Route[0]=5;Assert.Equal(0,w.GangCashier.Pending.Customer.Basket[0]);Assert.Equal(0,w.GangCashier.Pending.Customer.Route[0]);Assert.False(Customer(w,"new"));}
        [Fact] public void EveryItemMustBeScannedInOrderBeforePayment()
        {var w=Ordered();Assert.False(Pay(w));Assert.False(Scan(w,1));Assert.True(Scan(w));Assert.False(Pay(w));Assert.True(Scan(w,1));Assert.True(Pay(w));Assert.Equal(1,w.GangCashier.Pending.Served);Assert.Null(w.GangCashier.Pending.Customer);Assert.Equal(123,w.Cash);Assert.False(Complete(w));}
        [Fact] public void ScanAndPaymentReplayNeverIncrementOrPayTwice()
        {var w=Ordered();Scan(w);int saves=0;Assert.True(Scan(w,0,save:()=>saves++));Assert.Equal(1,w.GangCashier.Pending.Customer.Scanned);Scan(w,1);Pay(w);Assert.True(Pay(w,save:()=>saves++));Assert.Equal(0,saves);Assert.Equal(1,w.GangCashier.Pending.Served);Assert.Equal(123,w.Cash);}
        [Fact] public void FullFiveCustomersReadyStillNeedSalaryConfirmation()
        {var w=Ready();Assert.True(w.GangCashier.Pending.Ready);Assert.Equal(123,w.Cash);Assert.Empty(w.GangCashier.Accounts);Assert.Null(w.GangAutoLaunder);Assert.False(Customer(w,"extra"));}
        [Fact] public void FinalPaysExactlyFourHundredAndClearsPendingInOneSave()
        {var w=Ready();int saves=0;Assert.True(Complete(w,()=>{saves++;Assert.Null(w.GangCashier.Pending);Assert.Equal(523,w.Cash);Assert.Equal(400,w.GangCashier.Accounts[0].Earned);}));Assert.Equal(1,saves);Assert.Equal(523,w.Cash);Assert.Equal(10000,w.Bank);Assert.Equal(5,w.GangCashier.Accounts[0].Customers);Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);Assert.Null(w.GangAutoLaunder);}
        [Fact] public void SalaryReplayDoesNotWriteCashOrSaveAgain()
        {var w=Ready();Complete(w);var old=w.GangCashier;Assert.True(GangCashierTransactions.Complete(w,S(),A(),"till",()=>throw new Exception(),n=>throw new Exception(),()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Same(old,w.GangCashier);Assert.Equal(523,w.Cash);Assert.Equal(1,w.GangCashier.Accounts[0].Completed);}
        [Fact] public void NativeCashUsesDelegatesWithoutOverwritingSavedMpCashOrBank()
        {var w=Ready();int native=99;Assert.True(GangCashierTransactions.Complete(w,S(),A(),"till",()=>native,n=>native=n,Save,out _));Assert.Equal(499,native);Assert.Equal(123,w.Cash);Assert.Equal(10000,w.Bank);}
        [Fact] public void SaveFailureRollsBackSalaryAndExactBookForMpCash()
        {var w=Ready();var old=w.GangCashier;Assert.Throws<InvalidOperationException>(()=>Complete(w,()=>throw new InvalidOperationException()));Assert.Same(old,w.GangCashier);Assert.Equal(123,w.Cash);Assert.True(w.GangCashier.Pending.Ready);Assert.True(Complete(w));Assert.Equal(523,w.Cash);}
        [Fact] public void SaveFailureRollsBackExternalNativeCashAsWell()
        {var w=Ready();int native=99;var old=w.GangCashier;Assert.Throws<InvalidOperationException>(()=>GangCashierTransactions.Complete(w,S(),A(),"till",()=>native,n=>native=n,()=>throw new InvalidOperationException(),out _));Assert.Equal(99,native);Assert.Same(old,w.GangCashier);Assert.Equal(123,w.Cash);}
        [Fact] public void CashSetterFailureRestoresBookAndOriginalCash()
        {var w=Ready();var old=w.GangCashier;int native=99;Assert.Throws<InvalidOperationException>(()=>GangCashierTransactions.Complete(w,S(),A(),"till",()=>native,n=>{if(n>99)throw new InvalidOperationException();native=n;},Save,out _));Assert.Equal(99,native);Assert.Same(old,w.GangCashier);}
        [Fact] public void CashOverflowDoesNotConsumeReadyTurn()
        {var w=Ready();w.Cash=int.MaxValue-399;Assert.False(Complete(w));Assert.True(w.GangCashier.Pending.Ready);Assert.Empty(w.GangCashier.Accounts);w.Cash=int.MaxValue-400;Assert.True(Complete(w));Assert.Equal(int.MaxValue,w.Cash);}
        [Fact] public void ReloadPreservesBasketRouteScanCountTimeAndServedCustomers()
        {var w=Ordered();Scan(w);Update(w,50000);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(w.GangCashier.Valid);Assert.Equal(new[]{0,37},w.GangCashier.Pending.Customer.Basket);Assert.Equal(new[]{0,2},w.GangCashier.Pending.Customer.Route);Assert.Equal(1,w.GangCashier.Pending.Customer.Scanned);Assert.Equal(50000,w.GangCashier.Pending.Progress.Elapsed);Assert.True(Scan(w,1));Assert.True(Pay(w));}
        [Fact] public void ReadyReloadPaysOnceWithoutNativeCustomer()
        {var w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(Ready()));Assert.True(Complete(w));Assert.Equal(523,w.Cash);Assert.Null(w.GangCashier.Pending);}
        [Fact] public void DeadlineAndFaultCannotBeRewoundOrCleared()
        {var w=Ordered();Assert.True(Update(w,600000));Assert.True(w.GangCashier.Pending.Failed);Assert.False(Scan(w));Assert.False(Update(w,0));Assert.False(Complete(w));Assert.True(Cancel(w));w=Started();Update(w,10000,true);Assert.False(Update(w,10001));Assert.True(Cancel(w));}
        [Fact] public void ProgressSupportsWholeTenMinuteShiftBeyondFiveMinutes()
        {var w=Started();Assert.True(Update(w,599999));Assert.False(w.GangCashier.Pending.Failed);Assert.True(w.GangCashier.Valid);}
        [Fact] public void FailedBeginOrderAndScanRestoreOriginalBooks()
        {var w=W();Assert.Throws<InvalidOperationException>(()=>Begin(w,save:()=>throw new InvalidOperationException()));Assert.Null(w.GangCashier);w=Started();var old=w.GangCashier;Assert.Throws<InvalidOperationException>(()=>Customer(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangCashier);Customer(w);old=w.GangCashier;Assert.Throws<InvalidOperationException>(()=>Scan(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangCashier);Assert.Equal(0,w.GangCashier.Pending.Customer.Scanned);}
        [Fact] public void FailedPaymentSampleAndCancelKeepProgressAndCash()
        {var w=Ordered();Scan(w);Scan(w,1);var old=w.GangCashier;Assert.Throws<InvalidOperationException>(()=>Pay(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangCashier);Assert.Throws<InvalidOperationException>(()=>Update(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangCashier);Assert.Throws<InvalidOperationException>(()=>Cancel(w,()=>throw new InvalidOperationException()));Assert.Same(old,w.GangCashier);Assert.Equal(123,w.Cash);}
        [Fact] public void CancelDiscardsOnlyUnpaidShiftAndKeepsPriorHistory()
        {var w=Ready();Complete(w);Begin(w,"again");Assert.True(GangCashierTransactions.Cancel(w,"again",Save,out _));Assert.Equal(523,w.Cash);Assert.Equal(400,w.GangCashier.Accounts[0].Earned);Assert.Equal(1,w.GangCashier.Accounts[0].Completed);}
        [Fact] public void CancellingPartialCustomerPaymentsGivesNoSalary()
        {var w=Ordered();Scan(w);Scan(w,1);Pay(w);Assert.True(Cancel(w));Assert.Equal(123,w.Cash);Assert.Empty(w.GangCashier.Accounts);Assert.True(Cancel(w));}
        [Fact] public void LegacyQueriesDoNotCreateBookOrEconomicSideEffects()
        {var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":100,\"Cash\":23}");Assert.Null(w.GangCashier);Assert.False(GangCashierTransactions.Blocked(w));Assert.Null(GangCashierTransactions.Account(w.GangCashier,S().Gang));Assert.Null(w.GangCashier);}
        [Fact] public void UnknownFieldsSurviveSamplesScansAndFinalHistory()
        {var w=Ordered();var j=JObject.FromObject(w);j["Future"]=42;j["GangCashier"]["FutureBook"]="keep";j["GangCashier"]["Pending"]["Progress"]["FutureClock"]=123;j["GangCashier"]["Pending"]["Customer"]["FutureBasket"]=true;w=j.ToObject<WalletData>();Update(w);Scan(w);j=JObject.FromObject(w);Assert.Equal(123,(int)j["GangCashier"]["Pending"]["Progress"]["FutureClock"]);Assert.True((bool)j["GangCashier"]["Pending"]["Customer"]["FutureBasket"]);Scan(w,1);Pay(w);for(int i=1;i<5;i++){string id="c"+i;Customer(w,id);Scan(w,0,id);Scan(w,1,id);Pay(w,id);}Complete(w);Assert.Equal("keep",(string)JObject.FromObject(w)["GangCashier"]["FutureBook"]);Assert.Equal(42,(int)JObject.FromObject(w)["Future"]);}
        [Fact] public void InvalidOrderAndMissingProgressAreProtected()
        {var w=Ordered();w.GangCashier.Pending.Customer.Scanned=8;Assert.False(w.GangCashier.Valid);Assert.False(Cancel(w));w=Started();w.GangCashier.Pending.Progress=null;Assert.False(Update(w));var c=new GangCashierOptions();c.Products.Add(c.Products[0]);Assert.False(c.Valid);}
        [Fact] public void PendingCashierBlocksOtherEconomicsWithoutRecursiveValidation()
        {var w=Started();Assert.True(GangBulkTransactions.Blocked(w));Assert.False(GangAutoArmsRules.Free(w));Assert.True(GangCashierTransactions.OtherFree(w));Assert.False(GangPierTransactions.OtherFree(w));Assert.False(GangOffshoreTransactions.OtherFree(w));Assert.False(GangBusinessTransactions.Trade(w,S(),A(),new GangBusinessOptions(),false,new DateTime(2026,10,26),"trade",Save,out _));}
        [Fact] public void OtherInvalidBookAndPaidContractPreventStartingShift()
        {var w=W();w.GangPier=new GangPierData{Accounts=null};Assert.False(Begin(w));w=W();GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"paid",Save,out _);Assert.False(Begin(w));Assert.Equal(9850,w.Bank);}
        [Fact] public void LostOwnershipPreventsScansAndSalaryButAllowsCancel()
        {var w=Ordered();var state=new GangState{Gang="Other"};Assert.False(GangCashierTransactions.Scan(w,state,A(),"till","c0",0,Save,out _));Assert.False(GangCashierTransactions.Complete(w,state,A(),"till",()=>w.Cash,n=>w.Cash=n,Save,out _));Assert.True(Cancel(w));}
        [Fact] public void HistoryOverflowAndAccountCapacityPreventNewShifts()
        {var w=Ready();Complete(w);w.GangCashier.Accounts[0].Completed=long.MaxValue;w.GangCashier.Accounts[0].Customers=long.MaxValue;w.GangCashier.Accounts[0].Earned=long.MaxValue;Assert.False(Begin(w,"again"));w=W();w.GangCashier=new GangCashierData();for(int i=0;i<640;i++)w.GangCashier.Accounts.Add(new GangCashierAccount{Gang="Other"+i,Completed=1,Customers=5,Earned=400});Assert.True(w.GangCashier.Valid);Assert.False(Begin(w));}
        [Fact] public void RepeatedSamplesBoundReceiptsWithoutMovingMoney()
        {var w=Started();for(int i=0;i<270;i++)Assert.True(Update(w,i));Assert.Equal(256,w.GangCashier.Receipts.Count);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);}
        [Fact] public void SourceBasketPricesDoNotBecomePersonalPayout()
        {var w=Ordered();Assert.Equal(602,w.GangCashier.Pending.Customer.Basket.Sum(i=>w.GangCashier.Pending.Options.Products[i].Price));Scan(w);Scan(w,1);Pay(w);Assert.Equal(1,w.GangCashier.Pending.Served);Assert.Equal(123,w.Cash);}
        [Fact] public void CounterAndCustomerDistancesMatchSource()
        {Assert.True(GangCashierRules.AtTill(true,true,true,2.5f,.65f));Assert.False(GangCashierRules.AtTill(true,true,true,2.51f,.65f));Assert.False(GangCashierRules.AtTill(true,true,true,2,.66f));Assert.False(GangCashierRules.AtTill(true,false,true,2,.5f));Assert.False(GangCashierRules.AtTill(true,true,false,2,.5f));}
        [Fact] public void ClockSupportsTenMinuteDeadlineAndPausedRollover()
        {int now=int.MaxValue-100;var c=new GangCashierClock(now,600000,new GangCashierProgress{Elapsed=599000});c.Tick(unchecked(now+500),true);c.Tick(unchecked(now+1500),false);Assert.Equal(599500,c.Progress.Elapsed);c.Tick(unchecked(now+2500),true);Assert.Equal(600000,c.Progress.Elapsed);}
        [Fact] public void ConfiguredWageAndGoalSnapshotDeterminesOneFinalPayout()
        {var w=W();var c=new GangCashierOptions{Goal=1,Wage=100};Begin(w,c:c);Customer(w);Scan(w);Scan(w,1);Pay(w);Assert.True(Complete(w));Assert.Equal(223,w.Cash);Assert.Equal(100,w.GangCashier.Accounts[0].Earned);}
    }
}
