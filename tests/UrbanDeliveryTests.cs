using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class UrbanDeliveryTests
    {
        static readonly Action Save=()=>{};
        static string Json(object value)=>JsonConvert.SerializeObject(value);
        static WalletData W()=>new WalletData{Cash=500,Bank=1000};
        static UrbanDeliveryContract Plan(string job="postop",int stops=2,string id="route")
        {
            var c=new UrbanDeliveryContract{Id=id,Job=job,Office=new JobPoint{Name="Office",X=10,Y=20,Z=30},Depot=new JobPoint{Name="Depot",X=20,Y=30,Z=30},ReturnBonus=100};
            for(int i=0;i<stops;i++)c.Stops.Add(new UrbanStop{Address=new JobPoint{Name="Stop "+i,X=100+i*100,Y=30,Z=30},Pay=100,TimelyBonus=20,SecondsAllowed=60});return c;
        }
        static UrbanDeliveryContract C(WalletData w,string job)=>w.Jobs.Get(job).UrbanContract;
        static void Begin(WalletData w,string job="postop",int count=2){Assert.True(UrbanDeliveryTransactions.Begin(w,Plan(job,count),Save,out _));Assert.True(UrbanDeliveryTransactions.Load(w,job,"route",Save,out _));}
        static bool Drop(WalletData w,string job,int index,bool skip=false)=>UrbanDeliveryTransactions.Drop(w,()=>w.Cash,n=>w.Cash=n,job,"route",index,skip,Save,out _);
        static void Rollback(WalletData w,Action<Action> action){string before=Json(w);Assert.Throws<System.IO.IOException>(()=>action(()=>throw new System.IO.IOException()));Assert.Equal(before,Json(w));}
        [Theory] [InlineData("pizza")] [InlineData("postop")] [InlineData("supply")]
        public void EntireRouteUsesCorrectAccountAndSingleReceiptsIncludingReturn(string job)
        {
            var w=W();Begin(w,job);Assert.Equal(500,w.Cash);Assert.Equal(1000,w.Bank);Assert.True(Drop(w,job,0));int unit=job=="pizza"?120:100;
            Assert.True(UrbanDeliveryTransactions.Drop(w,()=>w.Cash,n=>w.Cash=n,job,"route",0,false,()=>throw new Exception(),out bool repeated));Assert.False(repeated);Assert.Equal(1,C(w,job).Index);
            Assert.True(Drop(w,job,1));Assert.True(C(w,job).Returning);int before=w.Bank;
            Rollback(w,save=>UrbanDeliveryTransactions.Return(w,()=>w.Cash,n=>w.Cash=n,job,"route",save,out _));Assert.True(UrbanDeliveryTransactions.Return(w,()=>w.Cash,n=>w.Cash=n,job,"route",Save,out _));Assert.Null(C(w,job));
            Assert.Equal(before+100,w.Bank);Assert.Equal(500,w.Cash);Assert.Equal(1000+2*unit+100,w.Bank);var stats=w.Jobs.Get(job);Assert.Equal(2,stats.Deliveries);Assert.Equal(1,stats.CompletedShifts);Assert.True(stats.UrbanReport.Completed);Assert.Equal(2*unit+100,stats.UrbanReport.Paid);Assert.True(stats.UrbanReport.Valid);
            Assert.True(UrbanDeliveryTransactions.Drop(w,()=>w.Cash,n=>w.Cash=n,job,"route",1,false,()=>throw new Exception(),out repeated));Assert.False(repeated);
        }
        [Theory] [InlineData("pizza")] [InlineData("postop")] [InlineData("supply")]
        public void FailedDeliverySaveRestoresMoneyProgressAndPackageTogether(string job)
        {var w=W();Begin(w,job);Rollback(w,save=>UrbanDeliveryTransactions.Drop(w,()=>w.Cash,n=>w.Cash=n,job,"route",0,false,save,out _));Assert.Equal(UrbanStopResult.Pending,C(w,job).Stops[0].Result);Assert.True(Drop(w,job,0));}
        [Fact] public void PersonalVehicleSelectionIsOnlyAllowedForPostOpAndSurvivesReload()
        {var w=W();var c=Plan();c.PersonalVehicle=true;Assert.True(UrbanDeliveryTransactions.Begin(w,c,Save,out _));var loaded=JsonConvert.DeserializeObject<WalletData>(Json(w));Assert.True(C(loaded,"postop").PersonalVehicle);c=Plan("pizza");c.PersonalVehicle=true;Assert.False(c.Valid);Assert.False(UrbanDeliveryTransactions.Begin(W(),c,Save,out _));}
        [Fact] public void PostOpSkipDoesNotPayCountAsDeliveryOrAllowSkippedBonusFarm()
        {
            var w=W();Begin(w);Assert.True(Drop(w,"postop",0,true));Assert.Equal(1000,w.Bank);Assert.Equal(0,w.Jobs.Get("postop").Deliveries);Assert.True(Drop(w,"postop",1,true));Assert.Equal(0,UrbanDeliveryRules.Bonus(C(w,"postop")));Assert.True(UrbanDeliveryTransactions.Return(w,()=>w.Cash,n=>w.Cash=n,"postop","route",Save,out _));Assert.Equal(1000,w.Bank);Assert.Equal(0,w.Jobs.Get("postop").CompletedShifts);Assert.Equal(2,w.Jobs.Get("postop").UrbanReport.Skipped);
        }
        [Fact] public void MixedDeliveredAndSkippedBatchPaysOnlyRealDeliveryAndProportionalBonus()
        {var w=W();Begin(w);Drop(w,"postop",0);Drop(w,"postop",1,true);Assert.Equal(50,UrbanDeliveryRules.Bonus(C(w,"postop")));Assert.True(UrbanDeliveryTransactions.Return(w,()=>w.Cash,n=>w.Cash=n,"postop","route",Save,out _));Assert.Equal(1150,w.Bank);Assert.Equal(1,w.Jobs.Get("postop").Deliveries);}
        [Theory] [InlineData("pizza")] [InlineData("supply")]
        public void SkipCannotBeUsedOnOtherJobs(string job)
        {var w=W();Begin(w,job);string before=Json(w);Assert.False(Drop(w,job,0,true));Assert.Equal(before,Json(w));}
        [Fact] public void ResumeKeepsExactStopsPaidAmountsQualityAndTimeWithoutDuplicateCargoCredit()
        {
            var w=W();Begin(w,"pizza",3);Assert.True(UrbanDeliveryTransactions.Advance(w,"pizza","route",30,20,Save,out _));Assert.True(Drop(w,"pizza",0));Assert.True(UrbanDeliveryTransactions.Advance(w,"pizza","route",25,0,Save,out _));int cash=w.Cash;
            var loaded=JsonConvert.DeserializeObject<WalletData>(Json(w));Assert.True(UrbanDeliveryTransactions.ReloadCargo(loaded,"pizza","route",Save,out _));Assert.False(C(loaded,"pizza").Loaded);Assert.Equal(25,C(loaded,"pizza").StopSeconds);Assert.Equal(80,C(loaded,"pizza").Quality);Assert.Equal(1,C(loaded,"pizza").Index);Assert.False(Drop(loaded,"pizza",1));
            Assert.True(UrbanDeliveryTransactions.Advance(loaded,"pizza","route",10,0,Save,out _));Assert.Equal(25,C(loaded,"pizza").StopSeconds);Assert.True(UrbanDeliveryTransactions.Load(loaded,"pizza","route",Save,out _));Assert.Equal(cash,loaded.Cash);Assert.Equal(65,C(loaded,"pizza").ElapsedSeconds);
        }
        [Theory] [InlineData("pizza",100,60,120)] [InlineData("pizza",100,61,50)] [InlineData("pizza",0,61,25)] [InlineData("postop",50,61,75)] [InlineData("supply",0,0,50)]
        public void QualityAndPizzaTimingUsePersistedTerms(string job,int quality,int seconds,int amount)
        {var c=Plan(job);c.Quality=quality;c.StopSeconds=c.ElapsedSeconds=seconds;Assert.Equal(amount,UrbanDeliveryRules.Quote(c));}
        [Fact] public void LastStepAndReturnAreSeparateAndReturnCannotBePaidEarly()
        {var w=W();Begin(w);Assert.False(UrbanDeliveryTransactions.Return(w,()=>w.Cash,n=>w.Cash=n,"postop","route",Save,out _));Drop(w,"postop",0);Assert.False(UrbanDeliveryTransactions.Return(w,()=>w.Cash,n=>w.Cash=n,"postop","route",Save,out _));Drop(w,"postop",1);Assert.True(C(w,"postop").Returning);Assert.Equal(0,w.Jobs.Get("postop").CompletedShifts);}
        [Fact] public void PizzaContinuationClosesOldBatchAndCreatesFreshOneInOnePaymentCheckpoint()
        {
            var w=W();Begin(w,"pizza");Drop(w,"pizza",0);Drop(w,"pizza",1);var next=Plan("pizza",3,"next");int cash=w.Bank;Rollback(w,save=>UrbanDeliveryTransactions.Return(w,()=>w.Cash,n=>w.Cash=n,"pizza","route",save,out _,next));Assert.Equal("route",C(w,"pizza").Id);
            Assert.True(UrbanDeliveryTransactions.Return(w,()=>w.Cash,n=>w.Cash=n,"pizza","route",Save,out _,next));Assert.Equal(cash+100,w.Bank);Assert.Equal("next",C(w,"pizza").Id);Assert.False(C(w,"pizza").Loaded);Assert.Equal(3,C(w,"pizza").Stops.Count);Assert.Equal("route",w.Jobs.Get("pizza").UrbanReport.Id);Assert.Equal(1,w.Jobs.Get("pizza").CompletedShifts);next.Stops.Clear();Assert.Equal(3,C(w,"pizza").Stops.Count);
        }
        [Fact] public void StartAndCancelAreAtomicAndNeverRefundOrErasePreviousEarnings()
        {
            var w=W();Rollback(w,save=>UrbanDeliveryTransactions.Begin(w,Plan(),save,out _));Assert.Null(w.Jobs);Begin(w);Drop(w,"postop",0);int bank=w.Bank;Rollback(w,save=>UrbanDeliveryTransactions.Cancel(w,"postop","route","cancelled",save,out _));Assert.True(UrbanDeliveryTransactions.Cancel(w,"postop","route","cancelled",Save,out _));Assert.Null(C(w,"postop"));Assert.Equal(bank,w.Bank);Assert.False(w.Jobs.Get("postop").UrbanReport.Completed);Assert.Equal(1,w.Jobs.Get("postop").UrbanReport.Delivered);
        }
        [Fact] public void PackageAndPaymentCannotAdvanceOutOfOrderOrBeforeLoading()
        {var w=W();Assert.True(UrbanDeliveryTransactions.Begin(w,Plan(),Save,out _));Assert.False(Drop(w,"postop",0));Assert.True(UrbanDeliveryTransactions.Load(w,"postop","route",Save,out _));Assert.False(Drop(w,"postop",1));Assert.False(UrbanDeliveryTransactions.Begin(w,Plan(id:"other"),Save,out _));Assert.Equal(0,C(w,"postop").Index);}
        [Fact] public void SaveFailureForTimeOrDamageDoesNotPartiallyReduceQuality()
        {var w=W();Begin(w);Rollback(w,save=>UrbanDeliveryTransactions.Advance(w,"postop","route",10,20,save,out _));Assert.Equal(100,C(w,"postop").Quality);Assert.Equal(0,C(w,"postop").ElapsedSeconds);}
        [Fact] public void NativeCashOrBankCeilingCannotRemovePackage()
        {
            var w=W();Begin(w,"pizza");C(w,"pizza").Bank=false;w.Cash=int.MaxValue;string before=Json(w);Assert.False(Drop(w,"pizza",0));Assert.Equal(before,Json(w));w=W();Begin(w);w.Bank=int.MaxValue;before=Json(w);Assert.False(Drop(w,"postop",0));Assert.Equal(before,Json(w));
        }
        [Fact] public void WalletFutureMetadataAndOtherJobsSurviveAllUrbanChanges()
        {
            var w=W();w.Jobs=new JobLedger();w.Jobs.Get("uber").Deliveries=12;var j=JObject.Parse(Json(w));j["Jobs"]["Future"]="ledger";j["Jobs"]["Jobs"]["uber"]["Future"]="rides";w=j.ToObject<WalletData>();Begin(w);j=JObject.Parse(Json(w));j["Jobs"]["Jobs"]["postop"]["Future"]="stats";j["Jobs"]["Jobs"]["postop"]["UrbanContract"]["Future"]="route";w=j.ToObject<WalletData>();Drop(w,"postop",0);j=JObject.Parse(Json(w));Assert.Equal("ledger",(string)j["Jobs"]["Future"]);Assert.Equal("rides",(string)j["Jobs"]["Jobs"]["uber"]["Future"]);Assert.Equal("stats",(string)j["Jobs"]["Jobs"]["postop"]["Future"]);Assert.Equal("route",(string)j["Jobs"]["Jobs"]["postop"]["UrbanContract"]["Future"]);Assert.Equal(12,w.Jobs.Get("uber").Deliveries);
        }
        [Fact] public void MalformedNewStateIsProtectedAndCannotGetNewContract()
        {var w=W();Begin(w);C(w,"postop").Stops[0].Result=UrbanStopResult.Delivered;string before=Json(w);Assert.False(UrbanDeliveryTransactions.Read(w,"postop",out _));Assert.False(UrbanDeliveryTransactions.Begin(w,Plan(id:"new"),Save,out _));Assert.Equal(before,Json(w));}
        [Fact] public void PizzaHonorsConfiguredHundredStopsWhilePostOpRetainsThirtyLimit()
        {Assert.True(Plan("pizza",100).Valid);Assert.False(Plan("postop",31).Valid);}
        [Fact] public void ExistingGenericQuickAndDrivingPaymentsKeepTheirOriginalBehaviorAndUrbanData()
        {var w=W();Begin(w);string saved=Json(C(w,"postop"));Assert.True(DeliveryPayments.Commit(w,()=>w.Cash,n=>w.Cash=n,"uber","ride",1,200,true,true,true,Save,out _));Assert.Equal(saved,Json(C(w,"postop")));Assert.Equal(1200,w.Bank);Assert.True(Drop(w,"postop",0));Assert.Equal(1300,w.Bank);}
        [Fact] public void ExplicitCashContractUsesNativeCashRollbackWithoutTouchingBank()
        {
            var w=W();var plan=Plan("pizza");plan.Bank=false;Assert.True(UrbanDeliveryTransactions.Begin(w,plan,Save,out _));Assert.True(UrbanDeliveryTransactions.Load(w,"pizza","route",Save,out _));Rollback(w,save=>UrbanDeliveryTransactions.Drop(w,()=>w.Cash,n=>w.Cash=n,"pizza","route",0,false,save,out _));Assert.True(Drop(w,"pizza",0));Assert.Equal(620,w.Cash);Assert.Equal(1000,w.Bank);
        }
    }
}
