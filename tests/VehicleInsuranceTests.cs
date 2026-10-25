using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class VehicleInsuranceTests
    {
        static readonly Action Save=()=>{};
        static VehicleInsuranceOptions Options()=>new VehicleInsuranceOptions();
        static SavedVehicle Car()=>new SavedVehicle{Id="owned",Model=123,Plate="RP12345",Name="Meu carro",GarageId="home",EngineHealth=1000,BodyHealth=1000,TankHealth=1000,Fuel=17,Mods={{11,2}}};
        static WalletData W(){var w=new WalletData{Bank=50000,Cash=55,Transport=new TransportData()};w.Transport.Garages.Add("home");w.Transport.Vehicles.Add(Car());return w;}
        static SavedVehicle Wreck(){var v=Car();v.EngineHealth=-4000;v.BodyHealth=0;v.TankHealth=-1000;return v;}
        static void Enroll(WalletData w)=>Assert.True(VehicleInsuranceTransactions.Enroll(w,Options(),"owned",Save,out _));
        static void Begin(WalletData w,string id="recovery")=>Assert.True(VehicleInsuranceTransactions.Begin(w,"owned",Wreck(),42,3,true,id,Save,out _));
        static void Start(WalletData w,string id="recovery")=>Assert.True(VehicleInsuranceTransactions.Start(w,id,Save,out _));
        [Fact] public void DefaultsMatchReferenceExcessAndOldSavesDoNotEnrollOrCharge()
        {Assert.Equal(10000,VehicleInsuranceDefaults.Load().Excess);Assert.Equal(64,VehicleInsuranceDefaults.Load().MaxPolicies);var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":50000}");Assert.Null(w.VehicleInsurance);Assert.Equal(50000,w.Bank);}
        [Fact] public void EnrollmentIsVoluntaryFreeAndDuplicateCannotCreateSecondPolicy()
        {var w=W();Enroll(w);Assert.Equal(50000,w.Bank);Assert.Single(w.VehicleInsurance.Policies);Assert.False(VehicleInsuranceTransactions.Enroll(w,Options(),"owned",Save,out _));Assert.Single(w.Transport.Vehicles);}
        [Fact] public void UnregisteredVehicleUnownedGarageOrDuplicateRegistryCannotBeInsured()
        {var w=W();Assert.False(VehicleInsuranceTransactions.Enroll(w,Options(),"street",Save,out _));w.Transport.Garages.Clear();Assert.False(VehicleInsuranceTransactions.Enroll(w,Options(),"owned",Save,out _));w=W();w.Transport.Vehicles.Add(Car());Assert.False(VehicleInsuranceTransactions.Enroll(w,Options(),"owned",Save,out _));Assert.Null(w.VehicleInsurance);}
        [Fact] public void InvalidTransportOrBankCannotCreatePolicy()
        {var w=W();w.Transport.Tanks=null;Assert.False(VehicleInsuranceTransactions.Enroll(w,Options(),"owned",Save,out _));w=W();w.Bank=-1;Assert.False(VehicleInsuranceTransactions.Enroll(w,Options(),"owned",Save,out _));}
        [Fact] public void EnrollmentSaveFailureRollsBackBookWithoutChangingTransport()
        {var w=W();var t=w.Transport;Assert.Throws<Exception>(()=>VehicleInsuranceTransactions.Enroll(w,Options(),"owned",()=>throw new Exception(),out _));Assert.Null(w.VehicleInsurance);Assert.Same(t,w.Transport);Assert.Equal(50000,w.Bank);}
        [Fact] public void CancelRemovesOnlyPolicyAndPreservesGarageFuelStorageAndMoney()
        {var w=W();Enroll(w);var t=w.Transport;string before=JsonConvert.SerializeObject(t);Assert.True(VehicleInsuranceTransactions.Cancel(w,"owned",Save,out _));Assert.Empty(w.VehicleInsurance.Policies);Assert.Equal(before,JsonConvert.SerializeObject(t));Assert.Equal(50000,w.Bank);}
        [Fact] public void ExistingPolicyKeepsContractedExcessWhenConfigurationChanges()
        {var w=W();Enroll(w);var c=Options();c.Excess=1;Assert.Equal(10000,w.VehicleInsurance.Policies[0].Excess);Begin(w);Assert.Equal(40000,w.Bank);Assert.Equal(10000,w.VehicleInsurance.Pending.Excess);}
        [Fact] public void PaidRecoveryReservesFeeOnlyOnceAndCompletionUpdatesExistingRecord()
        {var w=W();Enroll(w);var t=w.Transport;Begin(w);Assert.Equal(40000,w.Bank);Assert.Equal(10000,w.VehicleInsurance.Spent);Assert.Equal(0,w.VehicleInsurance.Claims);Start(w);Assert.True(VehicleInsuranceTransactions.Complete(w,"recovery",Car(),Save,out _));Assert.Same(t,w.Transport);Assert.Single(w.Transport.Vehicles);Assert.Equal(40000,w.Bank);Assert.Equal(55,w.Cash);Assert.Equal(1,w.VehicleInsurance.Claims);Assert.Null(w.VehicleInsurance.Pending);}
        [Fact] public void InsufficientBalanceAndUninsuredCarCannotStartOrChangeVehicle()
        {var w=W();Assert.False(VehicleInsuranceTransactions.Begin(w,"owned",Wreck(),42,3,true,"request",Save,out _));Enroll(w);w.Bank=9999;Assert.False(VehicleInsuranceTransactions.Begin(w,"owned",Wreck(),42,3,true,"request",Save,out _));Assert.Null(w.VehicleInsurance.Pending);Assert.Equal(9999,w.Bank);}
        [Theory][InlineData(false,false,1000,1000,false)][InlineData(false,true,1000,1000,true)][InlineData(true,false,1000,1000,true)][InlineData(false,false,0,1000,true)][InlineData(false,false,1000,0,true)][InlineData(false,false,float.NaN,1000,false)]
        public void DestroyedDecisionDoesNotTreatHealthyEngineOffAsClaim(bool dead,bool considered,float engine,float tank,bool expected)=>Assert.Equal(expected,VehicleInsuranceRules.Destroyed(dead,considered,engine,tank));
        [Fact] public void HealthyStoredPendingRemovalAndWrongIdentityCannotStart()
        {var w=W();Enroll(w);Assert.False(VehicleInsuranceTransactions.Begin(w,"owned",Car(),42,3,false,"healthy",Save,out _));w.Transport.Vehicles[0].Stored=true;Assert.False(VehicleInsuranceTransactions.Begin(w,"owned",Wreck(),42,3,true,"stored",Save,out _));w.Transport.Vehicles[0].Stored=false;w.Transport.Vehicles[0].PendingRemoval=true;Assert.False(VehicleInsuranceTransactions.Begin(w,"owned",Wreck(),42,3,true,"pending",Save,out _));w.Transport.Vehicles[0].PendingRemoval=false;var wrong=Wreck();wrong.Plate="OTHER";Assert.False(VehicleInsuranceTransactions.Begin(w,"owned",wrong,42,3,true,"wrong",Save,out _));Assert.Equal(50000,w.Bank);}
        [Fact] public void PendingBlocksAnotherClaimAndPolicyCancellation()
        {var w=W();Enroll(w);Begin(w);Assert.False(VehicleInsuranceTransactions.Begin(w,"owned",Wreck(),42,3,true,"second",Save,out _));Assert.False(VehicleInsuranceTransactions.Cancel(w,"owned",Save,out _));Assert.Equal(40000,w.Bank);}
        [Fact] public void BeginSaveFailureRollsBackFeePendingAndSpent()
        {var w=W();Enroll(w);Assert.Throws<Exception>(()=>VehicleInsuranceTransactions.Begin(w,"owned",Wreck(),42,3,true,"recovery",()=>throw new Exception(),out _));Assert.Equal(50000,w.Bank);Assert.Null(w.VehicleInsurance.Pending);Assert.Equal(0,w.VehicleInsurance.Spent);}
        [Fact] public void StartSaveFailureKeepsUnstartedOrderRefundable()
        {var w=W();Enroll(w);Begin(w);Assert.Throws<Exception>(()=>VehicleInsuranceTransactions.Start(w,"recovery",()=>throw new Exception(),out _));Assert.False(w.VehicleInsurance.Pending.Started);Assert.True(VehicleInsuranceTransactions.Refund(w,"recovery",Save,out _));Assert.Equal(50000,w.Bank);}
        [Fact] public void FailedCompletionRestoresRegistryAndKeepsPaidStartedRequest()
        {var w=W();Enroll(w);Begin(w);Start(w);var t=w.Transport;var old=t.Vehicles[0];var done=Car();done.X=100;Assert.Throws<Exception>(()=>VehicleInsuranceTransactions.Complete(w,"recovery",done,()=>throw new Exception(),out _));Assert.Same(t,w.Transport);Assert.Same(old,t.Vehicles[0]);Assert.True(w.VehicleInsurance.Pending.Started);Assert.Equal(40000,w.Bank);Assert.Equal(0,w.VehicleInsurance.Claims);Assert.True(VehicleInsuranceTransactions.Complete(w,"recovery",done,Save,out _));Assert.Equal(100,t.Vehicles[0].X);Assert.Equal(1,w.VehicleInsurance.Claims);}
        [Fact] public void ReceiptReplaysAfterReloadCannotDebitOrCompleteTwice()
        {var w=W();Enroll(w);Begin(w);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(VehicleInsuranceTransactions.Begin(w,"owned",Wreck(),42,3,true,"recovery",()=>throw new Exception(),out bool changed));Assert.False(changed);Start(w);Assert.True(VehicleInsuranceTransactions.Complete(w,"recovery",Car(),Save,out _));Assert.True(VehicleInsuranceTransactions.Complete(w,"recovery",Car(),()=>throw new Exception(),out changed));Assert.False(changed);Assert.Equal(40000,w.Bank);Assert.Equal(1,w.VehicleInsurance.Claims);}
        [Fact] public void UnstartedRefundReturnsFeeOnceWithoutCountingClaim()
        {var w=W();Enroll(w);Begin(w);Assert.True(VehicleInsuranceTransactions.Refund(w,"recovery",Save,out _));Assert.True(VehicleInsuranceTransactions.Refund(w,"recovery",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(50000,w.Bank);Assert.Equal(0,w.VehicleInsurance.Spent);Assert.Equal(0,w.VehicleInsurance.Claims);}
        [Fact] public void RefundBankOverflowKeepsPendingAndFailureRollsBack()
        {var w=W();Enroll(w);Begin(w);w.Bank=int.MaxValue;Assert.False(VehicleInsuranceTransactions.Refund(w,"recovery",Save,out _));Assert.NotNull(w.VehicleInsurance.Pending);w.Bank=40000;Assert.Throws<Exception>(()=>VehicleInsuranceTransactions.Refund(w,"recovery",()=>throw new Exception(),out _));Assert.Equal(40000,w.Bank);Assert.NotNull(w.VehicleInsurance.Pending);}
        [Fact] public void StartedRequestCannotRefundButCanBeExplicitlyAbandoned()
        {var w=W();Enroll(w);Begin(w);Start(w);Assert.False(VehicleInsuranceTransactions.Refund(w,"recovery",Save,out _));Assert.True(VehicleInsuranceTransactions.Abandon(w,"recovery",Save,out _));Assert.Null(w.VehicleInsurance.Pending);Assert.Equal(40000,w.Bank);Assert.Equal(10000,w.VehicleInsurance.Spent);Assert.Equal(0,w.VehicleInsurance.Claims);}
        [Fact] public void WrongOrderIdCannotFinishRefundOrAbandonCurrentOrder()
        {var w=W();Enroll(w);Begin(w);Assert.False(VehicleInsuranceTransactions.Start(w,"wrong",Save,out _));Assert.False(VehicleInsuranceTransactions.Refund(w,"wrong",Save,out _));Start(w);Assert.False(VehicleInsuranceTransactions.Complete(w,"wrong",Car(),Save,out _));Assert.False(VehicleInsuranceTransactions.Abandon(w,"wrong",Save,out _));Assert.NotNull(w.VehicleInsurance.Pending);}
        [Fact] public void CannotCompleteUnstartedDamagedRefueledOrDifferentVehicle()
        {var w=W();Enroll(w);Begin(w);Assert.False(VehicleInsuranceTransactions.Complete(w,"recovery",Car(),Save,out _));Start(w);Assert.False(VehicleInsuranceTransactions.Complete(w,"recovery",Wreck(),Save,out _));var c=Car();c.Fuel=18;Assert.False(VehicleInsuranceTransactions.Complete(w,"recovery",c,Save,out _));c=Car();c.Plate="OTHER";Assert.False(VehicleInsuranceTransactions.Complete(w,"recovery",c,Save,out _));Assert.NotNull(w.VehicleInsurance.Pending);}
        [Fact] public void CompletionPreservesModsGarageAndUnknownVehicleFieldsWithoutChangingTank()
        {var w=W();w.Transport.Tanks["123:RP12345"]=new FuelTank{Liters=11,Capacity=60,DistanceKm=321};var j=JObject.FromObject(w);j["Transport"]["Vehicles"][0]["Future"]=7;w=j.ToObject<WalletData>();Enroll(w);Begin(w);Start(w);var c=Car();c.Fuel=11;c.Mods[11]=3;c.GarageId="foreign";Assert.True(VehicleInsuranceTransactions.Complete(w,"recovery",c,Save,out _));Assert.Equal("home",w.Transport.Vehicles[0].GarageId);Assert.Equal(3,w.Transport.Vehicles[0].Mods[11]);Assert.Equal(7,(int)JObject.FromObject(w)["Transport"]["Vehicles"][0]["Future"]);Assert.Equal(11,w.Transport.Tanks["123:RP12345"].Liters);Assert.Equal(321,w.Transport.Tanks["123:RP12345"].DistanceKm);}
        [Fact] public void PendingOriginalFuelAndOilAreDeepSnapshots()
        {var w=W();Enroll(w);var wreck=Wreck();Assert.True(VehicleInsuranceTransactions.Begin(w,"owned",wreck,42,3,true,"recovery",Save,out _));wreck.Fuel=60;wreck.Mods[11]=4;Assert.Equal(17,w.VehicleInsurance.Pending.Original.Fuel);Assert.Equal(2,w.VehicleInsurance.Pending.Original.Mods[11]);Assert.Equal(3,w.VehicleInsurance.Pending.Oil);}
        [Fact] public void UnknownBookPolicyPendingAndOtherSubsystemFieldsSurvive()
        {var w=W();Enroll(w);Begin(w);var j=JObject.FromObject(w);j["Future"]=1;j["VehicleInsurance"]["Future"]=2;j["VehicleInsurance"]["Policies"][0]["Future"]=3;j["VehicleInsurance"]["Pending"]["Future"]=4;w=j.ToObject<WalletData>();Start(w);Assert.Equal(4,(int)JObject.FromObject(w)["VehicleInsurance"]["Pending"]["Future"]);Assert.True(VehicleInsuranceTransactions.Complete(w,"recovery",Car(),Save,out _));j=JObject.FromObject(w);Assert.Equal(1,(int)j["Future"]);Assert.Equal(2,(int)j["VehicleInsurance"]["Future"]);Assert.Equal(3,(int)j["VehicleInsurance"]["Policies"][0]["Future"]);}
        [Theory][InlineData(0,float.NaN)][InlineData(42,-1)][InlineData(42,float.PositiveInfinity)]
        public void InvalidHandleOrOilCannotReserveMoney(int handle,float oil)
        {var w=W();Enroll(w);Assert.False(VehicleInsuranceTransactions.Begin(w,"owned",Wreck(),handle,oil,true,"recovery",Save,out _));Assert.Equal(50000,w.Bank);}
        [Fact] public void ZeroExcessAndIndependentProfilesRemainConsistent()
        {var a=W();var b=W();var c=Options();c.Excess=0;Assert.True(VehicleInsuranceTransactions.Enroll(a,c,"owned",Save,out _));Begin(a);Assert.Equal(50000,a.Bank);Start(a);Assert.True(VehicleInsuranceTransactions.Complete(a,"recovery",Car(),Save,out _));Assert.Equal(1,a.VehicleInsurance.Claims);Assert.Null(b.VehicleInsurance);Assert.Equal(50000,b.Bank);}
        [Fact] public void RegistryIdentityChangesDuringPendingCannotReplaceNewVehicleWithOldClaim()
        {var w=W();Enroll(w);Begin(w);Start(w);w.Transport.Vehicles[0].Model=456;Assert.False(VehicleInsuranceTransactions.Complete(w,"recovery",Car(),Save,out _));Assert.Equal(456,w.Transport.Vehicles[0].Model);Assert.NotNull(w.VehicleInsurance.Pending);Assert.Equal(40000,w.Bank);}
        [Fact] public void CompletionCannotRefillCanonicalTankAfterFuelWasConsumedDuringPending()
        {var w=W();w.Transport.Tanks["123:RP12345"]=new FuelTank{Liters=17,Capacity=60};Enroll(w);Begin(w);Start(w);w.Transport.Tanks["123:RP12345"].Liters=9;Assert.False(VehicleInsuranceTransactions.Complete(w,"recovery",Car(),Save,out _));var repaired=Car();repaired.Fuel=9;Assert.True(VehicleInsuranceTransactions.Complete(w,"recovery",repaired,Save,out _));Assert.Equal(9,w.Transport.Tanks["123:RP12345"].Liters);Assert.Equal(9,w.Transport.Vehicles[0].Fuel);}
        [Fact] public void ChangedPlateOrModelRequiresNewMatchingRegistration()
        {var w=W();Enroll(w);w.Transport.Vehicles[0].Model=456;Assert.False(VehicleInsuranceTransactions.Begin(w,"owned",Wreck(),42,3,true,"recovery",Save,out _));Assert.Equal(50000,w.Bank);}
        [Fact] public void LoweredPolicyCapacityRetainsExistingAndStillAllowsCancellation()
        {var w=W();Enroll(w);var c=Options();c.MaxPolicies=1;var other=Car();other.Id="other";other.Plate="OTHER";w.Transport.Vehicles.Add(other);Assert.False(VehicleInsuranceTransactions.Enroll(w,c,"other",Save,out _));Assert.Single(w.VehicleInsurance.Policies);Assert.True(VehicleInsuranceTransactions.Cancel(w,"owned",Save,out _));}
        [Fact] public void InvalidSavedInsuranceAndCounterOverflowCannotSpendMoney()
        {var w=W();w.VehicleInsurance=new VehicleInsuranceData{Spent=-1};Assert.False(VehicleInsuranceTransactions.Enroll(w,Options(),"owned",Save,out _));w=W();Enroll(w);w.VehicleInsurance.Claims=int.MaxValue;Assert.False(VehicleInsuranceTransactions.Begin(w,"owned",Wreck(),42,3,true,"recovery",Save,out _));Assert.Equal(50000,w.Bank);}
    }
}
