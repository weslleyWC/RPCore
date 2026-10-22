using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace RPCore.Tests
{
    public class VehicleDataTests
    {
        [Fact] public void FuelUsageMatchesDistanceAndIdleTime()
        {
            var tank = new FuelTank { Liters = 60, Capacity = 60 };
            for (int i = 0; i < 100; i++) tank.Drive(1, 10, 8, .8f);
            Assert.Equal(1, tank.DistanceKm, 6);
            Assert.InRange(tank.Liters, 59.897f, 59.899f);
        }
        [Fact] public void LoadingTimeIsNotBilledAsDrivingTime()
        {
            var tank = new FuelTank { Liters = 50 };
            tank.Drive(600, 0, 8, 3.6f);
            Assert.InRange(tank.Liters, 49.998f, 50f);
        }
        [Fact] public void RunningOutOfFuelCannotGoNegative()
        {
            var tank = new FuelTank { Liters = .001f };
            tank.Drive(1, 100, 100, 20);
            Assert.Equal(0, tank.Liters);
        }
        [Theory] [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)] [InlineData(-1f)]
        public void InvalidElapsedTimeCannotChangeFuel(float time)
        {
            var tank = new FuelTank { Liters = 25 };
            tank.Drive(time, 20, 8, .8f); Assert.Equal(25, tank.Liters); Assert.Equal(0, tank.DistanceKm);
        }
        [Theory] [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)] [InlineData(-1f)]
        public void CorruptSavedTankIsRejectedBeforeUse(float liters)
        {
            var tank = new FuelTank { Liters = liters }; Assert.Throws<InvalidOperationException>(() => tank.Validate());
        }
        [Fact] public void RefuelingQuoteRespectsSpaceAndMoney()
        {
            var q = FuelTank.Quote(2.5f, 10, 100, 7); Assert.Equal(2.5f, q.Liters); Assert.Equal(18, q.Cost);
            q = FuelTank.Quote(60, 10, 10, 7); Assert.InRange(q.Liters, 1.41f, 1.43f); Assert.Equal(10, q.Cost);
        }
        [Fact] public void RefuelingNeverGrantsFuelWithoutPayment()
        {
            Assert.Equal(0, FuelTank.Quote(60, 3, 0, 7).Liters);
            Assert.Equal(0, FuelTank.Quote(0, 3, 100, 7).Cost);
            Assert.Equal(0, FuelTank.Quote(60, 3, 100, 0).Liters);
            Assert.Equal(0, FuelTank.Quote(float.NaN, 3, 100, 7).Liters);
        }
        [Fact] public void RefuelingQuotesNeverExceedFundsOrSpaceAcrossBudgets()
        {
            for (int cash = 0; cash < 250; cash++) for (int price = 1; price < 30; price++)
            {
                var q = FuelTank.Quote(1.37f, 3, cash, price);
                Assert.InRange(q.Cost, 0, cash); Assert.InRange(q.Liters, 0, 1.37001f);
                Assert.True(q.Liters == 0 || q.Cost >= q.Liters * price - .0001f);
            }
        }
        [Fact] public void PlateIdentityIsStableAndMissingPlatesAreRejected()
        {
            Assert.Equal(VehicleIdentity.Key(123, " TEST123 "), VehicleIdentity.Key(123, "test123"));
            Assert.NotEqual(VehicleIdentity.Key(123, "A"), VehicleIdentity.Key(456, "A"));
            Assert.Null(VehicleIdentity.Key(123, "  "));
        }
        [Fact] public void UnownedOrFullGarageRejectsStorageWithoutMutatingFleet()
        {
            var d = new TransportData(); var car = new SavedVehicle { Model = 10, Plate = "A", GarageId = "home" };
            Assert.False(d.CanStore("home", 1, car)); d.Garages.Add("home"); Assert.True(d.CanStore("home", 1, car)); d.Vehicles.Add(car);
            Assert.False(d.CanStore("home", 1, new SavedVehicle { Model = 10, Plate = "B" })); Assert.Single(d.Vehicles);
            Assert.True(d.CanStore("home", 1, car));
        }
        [Fact] public void DuplicateModelAndPlateCannotCreateSecondRecord()
        {
            var d = new TransportData(); d.Garages.Add("home"); d.Vehicles.Add(new SavedVehicle { Model = 10, Plate = "ABC", GarageId = "home" });
            Assert.False(d.CanStore("home", 10, new SavedVehicle { Model = 10, Plate = "abc" }));
            Assert.NotNull(d.FindIdentity(10, " ABC ")); Assert.Single(d.Vehicles);
        }
        [Fact] public void ChangingGarageChecksDestinationCapacity()
        {
            var d = new TransportData(); d.Garages.UnionWith(new[] { "a", "b" });
            var own = new SavedVehicle { Model = 10, Plate = "A", GarageId = "a" }; d.Vehicles.Add(own);
            d.Vehicles.Add(new SavedVehicle { Model = 10, Plate = "B", GarageId = "b" });
            Assert.False(d.CanStore("b", 1, own)); Assert.True(d.CanStore("b", 2, own));
        }
        [Fact] public void FullVehicleSnapshotAndUnknownFieldsSurviveSaveRoundTrip()
        {
            var d = new TransportData(); d.Garages.Add("home"); d.Tanks["10:ABC"] = new FuelTank { Liters = 17, DistanceKm = 123.45 };
            var car = new SavedVehicle { Model = 10, Plate = "ABC", GarageId = "home", Stored = true, PendingRemoval = true, CustomPrimary = new[] { 1, 2, 3 }, EngineHealth = 750, Fuel = 17 };
            car.Mods[11] = 3; car.Toggles[18] = true; car.Extras[1] = false; car.BurstTires.Add(4);
            car.Extra = new System.Collections.Generic.Dictionary<string, JToken> { { "FutureTuning", JObject.Parse("{\"value\":42}") } }; d.Vehicles.Add(car);
            var wallet = new WalletData { Cash = 1000, Transport = d };
            var loaded = JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(wallet)); loaded.Transport.Validate();
            var restored = loaded.Transport.Vehicles.Single(); Assert.Equal(3, restored.Mods[11]); Assert.True(restored.Toggles[18]);
            Assert.False(restored.Extras[1]); Assert.Contains(4, restored.BurstTires); Assert.Equal(new[] { 1, 2, 3 }, restored.CustomPrimary);
            Assert.True(restored.PendingRemoval); Assert.Equal(17, loaded.Transport.Tanks["10:ABC"].Liters); Assert.Equal(42, restored.Extra["FutureTuning"]["value"]);
        }
        [Fact] public void OldWalletLoadsWithoutRequiringMigrationOrDroppingUnknownData()
        {
            var wallet = JsonConvert.DeserializeObject<WalletData>("{\"Cash\":123,\"Bank\":456,\"MyOtherSystem\":true}");
            Assert.Null(wallet.Transport); wallet.Transport = new TransportData();
            var roundTrip = JObject.Parse(JsonConvert.SerializeObject(wallet));
            Assert.Equal(123, roundTrip["Cash"]); Assert.Equal(456, roundTrip["Bank"]); Assert.True((bool)roundTrip["MyOtherSystem"]);
        }
        [Fact] public void InvalidVehicleLedgerRejectsDuplicateIds()
        {
            var d = new TransportData(); d.Vehicles.Add(new SavedVehicle { Id = "same" }); d.Vehicles.Add(new SavedVehicle { Id = "same" });
            Assert.Throws<InvalidOperationException>(() => d.Validate());
        }
    }
}
