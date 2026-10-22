using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Xunit;

namespace RPCore.Tests
{
    public class ShopDataTests
    {
        static List<ShopDef> Legacy() => new List<ShopDef> {
            new ShopDef { Name = "24/7 Strawberry", X = 25.7f, Y = -1347.3f, Z = 29.5f },
            new ShopDef { Name = "LTD Davis", X = -47f, Y = -1758.7f, Z = 29.4f },
            new ShopDef { Name = "LTD Mirror Park", X = 1163.4f, Y = -323.8f, Z = 69.2f }
        };
        [Fact] public void UpgradeUnmodifiedOldConfigAddsAllTwentyStoresWithoutMutatingInput()
        {
            var original = Legacy(); var bytes = JsonConvert.SerializeObject(original);
            var resolved = ShopLocations.Resolve(original);
            Assert.Equal(20, resolved.Count);
            Assert.Equal(bytes, JsonConvert.SerializeObject(original));
            Assert.Equal(20, resolved.Select(s => s.Name).Distinct().Count());
        }
        [Theory]
        [InlineData("LTD Davis", -54.7f, -1749.089f, 29.421f)]
        [InlineData("24/7 Strawberry", 33.312f, -1346.681f, 29.497f)]
        [InlineData("LTD Mirror Park", 1155.941f, -320.24f, 69.205f)]
        public void KnownLegacyPointsUseExactReferenceCustomerSideCoordinates(string name, float x, float y, float z)
        {
            var shop = ShopLocations.Resolve(Legacy()).Single(s => s.Name == name);
            Assert.Equal(x, shop.X); Assert.Equal(y, shop.Y); Assert.Equal(z, shop.Z);
        }
        [Fact] public void CustomizedCoordinatesPreventAutomaticExpansionAndAreRetained()
        {
            var input = Legacy(); input[1].X = -52;
            var output = ShopLocations.Resolve(input);
            Assert.Equal(3, output.Count); Assert.Equal(-52, output[1].X);
            Assert.Equal(33.312f, output[0].X); Assert.Equal(1155.941f, output[2].X);
            Assert.Equal(25.7f, input[0].X);
        }
        [Fact] public void CustomStoreAtOldCoordinateIsNotRenamedOrMoved()
        {
            var input = Legacy(); input[1].Name = "My shop";
            var output = ShopLocations.Resolve(input);
            Assert.Equal(3, output.Count); Assert.Equal(-47, output[1].X); Assert.Equal("My shop", output[1].Name);
        }
        [Fact] public void DeliberatelyEmptyListRemainsEmpty() => Assert.Empty(ShopLocations.Resolve(new List<ShopDef>()));
        [Fact] public void NullEntriesDoNotCreateMarkersAtOrigin()
        {
            var input = new List<ShopDef> { null, new ShopDef { Name = "Custom", X = 123, Y = 456, Z = 789 } };
            var output = ShopLocations.Resolve(input);
            Assert.Single(output); Assert.Equal(123, output[0].X);
        }
        [Fact] public void ResolvedConfigurationIsIdempotent()
        {
            var first = ShopLocations.Resolve(Legacy());
            Assert.Equal(JsonConvert.SerializeObject(first), JsonConvert.SerializeObject(ShopLocations.Resolve(first)));
        }
        [Fact] public void DuplicateLegacyNamesDoNotGetMistakenForStockConfig()
        {
            var input = Legacy(); input[2] = input[0];
            Assert.Equal(3, ShopLocations.Resolve(input).Count);
        }
        [Fact] public void ShippedJsonMatchesRuntimeDefaults()
        {
            var json = JsonConvert.DeserializeObject<List<ShopDef>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Shops.json")));
            Assert.Equal(JsonConvert.SerializeObject(ShopLocations.Defaults()), JsonConvert.SerializeObject(json));
        }
        [Fact] public void ChangingResolvedCustomPointCannotMutateUserConfiguration()
        {
            var custom = new ShopDef { Name = "Custom", X = 123, Y = 456, Z = 789 };
            var resolved = ShopLocations.Resolve(new List<ShopDef> { custom }); resolved[0].X = 0;
            Assert.Equal(123, custom.X);
        }
    }
}
