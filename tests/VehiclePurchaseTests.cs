using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class VehiclePurchaseTests
    {
        static TransportData Data(){var d=new TransportData();d.Garages.Add("home");return d;}
        static SavedVehicle Car()=>new SavedVehicle{Id="bought",Model=123,Plate="RP123456",GarageId="home",Fuel=60,EngineHealth=1000,BodyHealth=1000,TankHealth=1000,Mods={{11,2}}};
        static VehiclePurchase Quote(TransportData d,int cash=50000,int model=123,int price=12000,string garage="home",int capacity=3)=>new VehiclePurchase(d,cash,model,price,garage,capacity);
        [Fact] public void PurchaseCommitsCashVehicleAndFullCanonicalTankInOneSave()
        {
            var d=Data();int cash=50000,saves=0;var record=Car();
            Assert.True(Quote(d).Commit(d,record,60,()=>cash,v=>cash=v,()=>{Assert.Equal(38000,cash);Assert.Single(d.Vehicles);Assert.Equal(60,d.Tanks["123:RP123456"].Liters);saves++;}));
            Assert.Equal(1,saves);Assert.False(d.Vehicles.Single().Stored);Assert.Equal("home",d.Vehicles.Single().GarageId);Assert.Equal(2,d.Vehicles.Single().Mods[11]);
            record.Mods[11]=0;Assert.Equal(2,d.Vehicles.Single().Mods[11]);
        }
        [Fact] public void DiskFailureRestoresExactCashAndKeepsExistingGraphAndUnknownFields()
        {
            var d=JsonConvert.DeserializeObject<TransportData>("{\"Garages\":[\"home\"],\"Future\":17}");var tanks=d.Tanks;var vehicles=d.Vehicles;int cash=50000;
            Assert.Throws<Exception>(()=>Quote(d).Commit(d,Car(),60,()=>cash,v=>cash=v,()=>throw new Exception("disk")));
            Assert.Equal(50000,cash);Assert.Same(tanks,d.Tanks);Assert.Same(vehicles,d.Vehicles);Assert.Empty(d.Tanks);Assert.Empty(d.Vehicles);Assert.Equal(17,(int)JObject.FromObject(d)["Future"]);
        }
        [Fact] public void NativeCashWriteFailureRestoresBeforeAnyVehicleIsPublished()
        {
            var d=Data();int cash=50000,writes=0;
            Assert.Throws<Exception>(()=>Quote(d).Commit(d,Car(),60,()=>cash,v=>{cash=v;if(++writes==1)throw new Exception("native");},()=>Assert.Fail()));
            Assert.Equal(50000,cash);Assert.Empty(d.Vehicles);Assert.Empty(d.Tanks);Assert.Equal(2,writes);
        }
        [Fact] public void RollbackFailureIsReportedAlongWithOriginalFailure()
        {
            var d=Data();int cash=50000,writes=0;
            var e=Assert.Throws<AggregateException>(()=>Quote(d).Commit(d,Car(),60,()=>cash,v=>{if(++writes>1)throw new Exception("rollback");cash=v;},()=>throw new Exception("disk")));
            Assert.Equal(2,e.InnerExceptions.Count);Assert.Empty(d.Vehicles);Assert.Empty(d.Tanks);
        }
        [Fact] public void RepeatedOrReentrantConfirmationCannotChargeAgain()
        {
            var d=Data();var q=Quote(d);int cash=50000;Action<int> write=v=>cash=v;
            Assert.True(q.Commit(d,Car(),60,()=>cash,write,()=>Assert.False(q.Commit(d,Car(),60,()=>cash,write,()=>Assert.Fail()))));
            Assert.False(q.Commit(d,Car(),60,()=>cash,write,()=>Assert.Fail()));Assert.Equal(38000,cash);Assert.Single(d.Vehicles);
        }
        [Fact] public void FailedAttemptRequiresAnotherConfirmation()
        {
            var d=Data();var q=Quote(d);int cash=50000;
            Assert.Throws<Exception>(()=>q.Commit(d,Car(),60,()=>cash,v=>cash=v,()=>throw new Exception()));
            Assert.False(q.Commit(d,Car(),60,()=>cash,v=>cash=v,()=>Assert.Fail()));Assert.Equal(50000,cash);
        }
        [Theory][InlineData(49999)][InlineData(50001)][InlineData(0)]
        public void CashChangesInvalidateQuote(int cash){var d=Data();Assert.False(Quote(d).Commit(d,Car(),60,()=>cash,v=>Assert.Fail(),()=>Assert.Fail()));}
        [Fact] public void ReplacementWalletOrLostGarageOrLastSlotInvalidatesQuote()
        {
            var d=Data();var q=Quote(d,capacity:1);Assert.False(q.Current(Data(),50000));d.Garages.Clear();Assert.False(q.Current(d,50000));d.Garages.Add("home");d.Vehicles.Add(Car());Assert.False(q.Current(d,50000));
        }
        [Theory][InlineData(0,12000,3)][InlineData(123,0,3)][InlineData(123,-1,3)][InlineData(123,50001,3)][InlineData(123,12000,0)]
        public void InvalidQuoteCannotDebit(int model,int price,int capacity){var d=Data();Assert.False(Quote(d,model:model,price:price,capacity:capacity).Current(d,50000));}
        [Theory][InlineData("model")][InlineData("garage")][InlineData("id")][InlineData("plate")][InlineData("fuel")][InlineData("health")][InlineData("stored")][InlineData("removal")][InlineData("nan")]
        public void InvalidDeliveryNeverChangesMoneyOrRegistry(string invalid)
        {
            var d=Data();var v=Car();switch(invalid){case "model":v.Model=124;break;case "garage":v.GarageId="other";break;case "id":v.Id=null;break;case "plate":v.Plate="TOO-LONG-PLATE";break;case "fuel":v.Fuel=61;break;case "health":v.EngineHealth=0;break;case "stored":v.Stored=true;break;case "removal":v.PendingRemoval=true;break;case "nan":v.X=float.NaN;break;}
            Assert.False(Quote(d).Commit(d,v,60,()=>50000,n=>Assert.Fail(),()=>Assert.Fail()));Assert.Empty(d.Vehicles);Assert.Empty(d.Tanks);
        }
        [Theory][InlineData(0)][InlineData(-1)][InlineData(10001)][InlineData(float.NaN)][InlineData(float.PositiveInfinity)]
        public void InvalidTankCapacityCannotBePurchased(float capacity){var d=Data();Assert.False(Quote(d).Commit(d,Car(),capacity,()=>50000,v=>Assert.Fail(),()=>Assert.Fail()));}
        [Theory][InlineData("id")][InlineData("plate")][InlineData("tank")]
        public void ExistingIdentitiesAndTanksCannotBeOverwritten(string conflict)
        {
            var d=Data();if(conflict=="tank")d.Tanks["123:RP123456"]=new FuelTank{Liters=7};else{var old=Car();if(conflict=="id")old.Plate="DIFFERNT";else old.Id="other";d.Vehicles.Add(old);}
            string before=JsonConvert.SerializeObject(d);Assert.False(Quote(d).Commit(d,Car(),60,()=>50000,v=>Assert.Fail(),()=>Assert.Fail()));Assert.Equal(before,JsonConvert.SerializeObject(d));
        }
        [Fact] public void ExactBalanceAndZeroFuelAndOneSlotAreValidWithoutTouchingOtherGarage()
        {
            var d=Data();d.Garages.Add("other");var old=Car();old.Id="old";old.Plate="OTHER";old.GarageId="other";d.Vehicles.Add(old);var car=Car();car.Fuel=0;int cash=12000;
            Assert.True(Quote(d,cash:12000,capacity:1).Commit(d,car,60,()=>cash,v=>cash=v,()=>{}));Assert.Equal(0,cash);Assert.Same(old,d.Vehicles[0]);Assert.Equal(0,d.Tanks["123:RP123456"].Liters);
        }
        static VehiclePolicy Policy()=>new VehiclePolicy{VehicleId="bought",Model=123,Plate="RP123456",Excess=10000};
        static VehicleInsuranceData Insurance()=>new VehicleInsuranceData{Policies={Policy()}};
        [Fact] public void InsuranceConfirmationCanOnlyBeConsumedOnce(){var d=Insurance();var q=new InsuranceConfirmation(Policy(),null,50000);Assert.True(q.Take(d,50000));Assert.False(q.Take(d,50000));}
        [Theory][InlineData("cash")][InlineData("policy")][InlineData("plate")][InlineData("excess")][InlineData("pending")]
        public void InsuranceConfirmationRefusesStaleState(string change)
        {
            var d=Insurance();var q=new InsuranceConfirmation(Policy(),null,50000);int bank=50000;
            switch(change){case "cash":bank++;break;case "policy":d.Policies.Clear();break;case "plate":d.Policies[0].Plate="OTHER";break;case "excess":d.Policies[0].Excess=5;break;case "pending":d.Spent=10000;d.Pending=new VehicleRecovery{Id="claim",VehicleId="bought",Handle=7,Excess=10000,Original=Car()};break;}
            Assert.False(q.Take(d,bank));
        }
        [Fact] public void StartedOrReplacedRecoveryCannotInheritOldRefundConfirmation()
        {
            var d=Insurance();d.Spent=10000;d.Pending=new VehicleRecovery{Id="claim",VehicleId="bought",Handle=7,Excess=10000,Original=Car()};Assert.True(d.Valid);
            var q=new InsuranceConfirmation(Policy(),d.Pending,50000);d.Pending.Started=true;Assert.False(q.Take(d,50000));
            q=new InsuranceConfirmation(Policy(),d.Pending,50000);d.Pending.Id="another";Assert.False(q.Take(d,50000));
        }
        [Fact] public void NullOrMalformedInsuranceStateCannotExecuteConfirmation(){Assert.False(new InsuranceConfirmation(Policy(),null,0).Take(null,0));var d=Insurance();d.Policies=null;Assert.False(new InsuranceConfirmation(Policy(),null,0).Take(d,0));}
    }
}
