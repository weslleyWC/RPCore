using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Xunit;

namespace RPCore.Tests
{
    public class VisualDataTests
    {
        VisualCatalog Catalog() { return JsonConvert.DeserializeObject<VisualCatalog>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Visuals", "Catalog.json"))); }
        VisualBindings Bindings() { return JsonConvert.DeserializeObject<VisualBindings>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Visuals", "Bindings.json"))); }
        [Fact] public void ActualUploadedDictionariesContain679NamedTextures()
        {
            var c = Catalog(); Assert.Equal(8, c.Dictionaries.Count); Assert.Equal(679, c.Dictionaries.Sum(p => p.Value.Count)); Assert.Equal(27, c.Models.Count);
            foreach (var group in c.Dictionaries.Values) { Assert.Equal(group.Count, group.Select(t => t.Name.ToLowerInvariant()).Distinct().Count()); Assert.All(group, t => { Assert.False(string.IsNullOrWhiteSpace(t.Name)); Assert.True(t.Width > 0 && t.Height > 0); }); }
        }
        [Fact] public void EveryProgressRingExistsInProvidedYtd()
        {
            var c = Catalog(); for (int i = 0; i <= 100; i++) Assert.NotNull(c.Find("PSRP", "OutBar" + i));
        }
        [Fact] public void EveryMenuBannerAndItemIconMapsToAnActualProvidedTexture()
        {
            var c = Catalog(); var b = Bindings();
            Assert.All(b.Banners, p => Assert.NotNull(c.Find("PSRP_Banners", p.Value)));
            Assert.All(b.ItemIcons, p => Assert.NotNull(c.Find("PSRP_Inventory", p.Value)));
        }
        [Fact] public void PhoneAndVehicleResourcesExistWithTheirExactNames()
        {
            var c = Catalog();
            foreach (var n in new[] { "PhoneTexture", "PhoneWallpaper1", "WalletIcon", "IconID", "IconCar", "StockIconPhone", "MusicIcon", "GangsIcon", "JobsIcon", "SettingsIcon", "IconHome" }) Assert.NotNull(c.Find("PSRP_PHONE", n));
            foreach (var n in new[] { "RectangleBorder", "CarFuelIcon", "CarSeatbeltIcon", "CarCruiseIcon", "CarHeadLightIcon", "CarOilIcon", "FlatTireIcon", "SpeedoLine", "LBlinker", "Rblinker", "MoneyIcon", "HealthIcon", "FoodIcon", "DrinkIcon" }) Assert.NotNull(c.Find("PSRP", n));
            Assert.Equal(1920, c.Find("PSRP_PHONE", "PhoneTexture").Width); Assert.Equal(1080, c.Find("PSRP_PHONE", "PhoneTexture").Height);
        }
        [Fact] public void CustomConsumableModelsAreInTheUploadedDlcCatalog()
        {
            var c = Catalog();
            foreach (var p in Bindings().ItemProps.Values.Where(p => !p.Model.StartsWith("prop_") && !p.Model.StartsWith("p_"))) Assert.Contains(p.Model, c.Models);
            Assert.Contains("phonesmart", c.Models);
        }
        [Theory] [InlineData(-2f, 0)] [InlineData(101f, 100)] [InlineData(49.6f, 50)] [InlineData(float.NaN, 0)] [InlineData(float.PositiveInfinity, 0)]
        public void GaugePercentageCannotRequestNonexistentRing(float value, int expected) { Assert.Equal(expected, HudMath.Percent(value)); }
        [Fact] public void UnknownOrMissingCatalogEntriesSafelyReturnNull()
        {
            var c = Catalog(); Assert.Null(c.Find("missing", "FoodIcon")); Assert.Null(c.Find("PSRP", "missing")); Assert.Null(c.Find(null, null));
            Assert.Equal("FoodIcon", c.Find("psrp", "foodicon").Name); c.Dictionaries = null; Assert.Null(c.Find("PSRP", "FoodIcon"));
        }
        [Fact] public void UltrawideAndSafeZoneLayoutPreserveAspectAndBounds()
        {
            Assert.InRange(HudMath.CanvasWidth(16f / 9), 1919.9f, 1920.1f);
            Assert.InRange(HudMath.CanvasWidth(32f / 9), 3839.9f, 3840.1f);
            Assert.Equal(1920, HudMath.CanvasWidth(float.NaN)); Assert.Equal(0, HudMath.SafeInset(1920, 1));
            Assert.InRange(HudMath.SafeInset(1920, .9f), 95.99f, 96.01f); Assert.InRange(HudMath.SafeInset(1080, -1), 107.99f, 108.01f);
        }
        [Fact] public void PhoneNavigationDoesNotWrapRowsOrLeaveGrid()
        {
            Assert.Equal(0, HudMath.MoveGrid(0, -1, 0, 3, 9)); Assert.Equal(2, HudMath.MoveGrid(2, 1, 0, 3, 9));
            Assert.Equal(3, HudMath.MoveGrid(0, 0, 1, 3, 9)); Assert.Equal(0, HudMath.MoveGrid(3, 0, -1, 3, 9));
            Assert.Equal(8, HudMath.MoveGrid(8, 0, 1, 3, 9)); Assert.Equal(0, HudMath.MoveGrid(5, 1, 0, 0, 0));
        }
    }
}
