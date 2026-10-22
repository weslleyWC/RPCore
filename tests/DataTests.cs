using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace RPCore.Tests
{
    public class DataTests : IDisposable
    {
        readonly string root = Path.Combine(Path.GetTempPath(), "RPCore-tests-" + Guid.NewGuid().ToString("N"));
        public DataTests() { Directory.CreateDirectory(root); }
        string FileName(string name) { return Path.Combine(root, name); }
        public void Dispose() { Directory.Delete(root, true); }

        [Fact] public void AtomicSaveKeepsPreviousVersion()
        {
            var files = new JsonFiles(); string p = FileName("state.json");
            files.Write(p, new { Money = 10 }); files.Write(p, new { Money = 25 });
            Assert.Equal(25, files.Read<JObject>(p, () => null)["Money"]);
            Assert.Equal(10, JObject.Parse(File.ReadAllText(p + ".bak"))["Money"]);
            Assert.Empty(Directory.GetFiles(root, "*.tmp"));
        }
        [Theory] [InlineData("{")] [InlineData("null")] public void InvalidSaveCannotBeOverwrittenByDefaults(string text)
        {
            var files = new JsonFiles(); string p = FileName("broken.json"); File.WriteAllText(p, text);
            Assert.ThrowsAny<Exception>(() => files.Read<Portfolio>(p, () => new Portfolio()));
            Assert.Throws<IOException>(() => files.Write(p, new Portfolio()));
            Assert.Equal(text, File.ReadAllText(p));
        }
        [Fact] public void MissingSaveUsesFallbackWithoutWriting()
        {
            var files = new JsonFiles(); string p = FileName("missing.json");
            Assert.Equal(50, files.Read(p, () => new Portfolio { Cash = 50 }).Cash); Assert.False(File.Exists(p));
        }
        [Fact] public void ExtensionFieldsSurviveRoundTrip()
        {
            var value = JsonConvert.DeserializeObject<Portfolio>("{\"Cash\":100,\"FutureSystem\":{\"nested\":[1,2,3]}}");
            Assert.Equal(3, JObject.Parse(JsonConvert.SerializeObject(value))["FutureSystem"]["nested"].Count());
        }
        [Fact] public void FlatSavesMigrateOnceAndOriginalsRemainUnchanged()
        {
            string p = FileName("Inventory.json"); string text = "{\"Items\":{\"water\":3},\"Future\":true}"; File.WriteAllText(p, text);
            LegacyImport.MigrateFlat(root, "Franklin"); LegacyImport.MigrateFlat(root, "Michael");
            Assert.Equal(text, File.ReadAllText(p)); Assert.True(File.Exists(FileName("Franklin/Inventory.json")));
            Assert.False(File.Exists(FileName("Michael/Inventory.json")));
        }
        [Fact] public void FlatMigrationNeverReplacesExistingProfile()
        {
            File.WriteAllText(FileName("Needs.json"), "{\"Hunger\":80}"); Directory.CreateDirectory(FileName("Franklin"));
            File.WriteAllText(FileName("Franklin/Needs.json"), "{\"Hunger\":10}"); LegacyImport.MigrateFlat(root, "Franklin");
            Assert.Equal(10, JObject.Parse(File.ReadAllText(FileName("Franklin/Needs.json")))["Hunger"]);
        }
        [Theory] [InlineData("Protagonist")] [InlineData("Terry_Evans")] [InlineData("Tk_Santos")]
        public void RealPsrpSavesImportWithoutLosingAnyOriginalField(string name)
        {
            string source = Path.Combine(AppContext.BaseDirectory, "Fixtures", name + ".json");
            byte[] original = File.ReadAllBytes(source); string target = FileName(name);
            LegacyImport.ImportPlayer(source, target);
            Assert.Equal(original, File.ReadAllBytes(Path.Combine(target, "PSRP.original.json")));
            Assert.Equal(original, File.ReadAllBytes(source));
            var sourceData = JObject.Parse(File.ReadAllText(source)); var needs = JObject.Parse(File.ReadAllText(Path.Combine(target, "Needs.json")));
            Assert.Equal((float)sourceData["Hunger"], (float)needs["Hunger"]); Assert.Equal((float)sourceData["Hydration"], (float)needs["Thirst"]);
            Assert.Equal((float?)sourceData["Sleep"] ?? 100, (float)needs["Sleep"]);
            var wallet = JObject.Parse(File.ReadAllText(Path.Combine(target, "Wallet.json")));
            Assert.Equal((int)sourceData["PlayerBankBalance"], (int)wallet["Bank"]);
            Assert.Equal(151, JObject.Parse(File.ReadAllText(Path.Combine(target, "PSRP.original.json"))).Count);
            Assert.Throws<IOException>(() => LegacyImport.ImportPlayer(source, target));
        }
        [Fact] public void InvalidImportLeavesNoPartialProfile()
        {
            string source = FileName("invalid.json"); File.WriteAllText(source, "{\"PlayerMoneyBalance\":\"oops\"}");
            Assert.ThrowsAny<Exception>(() => LegacyImport.ImportPlayer(source, FileName("target")));
            Assert.False(Directory.Exists(FileName("target"))); Assert.Empty(Directory.GetDirectories(root));
        }
        [Fact] public void TransfersConserveItems()
        {
            var a = new Dictionary<string, int> { { "water", 5 } }; var b = new Dictionary<string, int>();
            Assert.True(ItemTransfer.Move(a, b, "water", 2, _ => .5f, 10));
            Assert.Equal(3, a["water"]); Assert.Equal(2, b["water"]);
            Assert.True(ItemTransfer.Move(b, a, "water", 2, _ => .5f, 10)); Assert.Equal(5, a["water"]); Assert.Empty(b);
        }
        [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(10)] public void InvalidTransferDoesNotMutateEitherInventory(int qty)
        {
            var a = new Dictionary<string, int> { { "water", 5 } }; var b = new Dictionary<string, int>();
            Assert.False(ItemTransfer.Move(a, b, "water", qty, _ => .5f, 10)); Assert.Equal(5, a["water"]); Assert.Empty(b);
        }
        [Fact] public void CapacityFailureDoesNotLoseItem()
        {
            var a = new Dictionary<string, int> { { "water", 5 } }; var b = new Dictionary<string, int> { { "water", 20 } };
            Assert.False(ItemTransfer.Move(a, b, "water", 1, _ => .5f, 10)); Assert.Equal(5, a["water"]); Assert.Equal(20, b["water"]);
        }
        [Fact] public void TransferRejectsIntegerOverflow()
        {
            var a = new Dictionary<string, int> { { "water", 1 } }; var b = new Dictionary<string, int> { { "water", int.MaxValue } };
            Assert.False(ItemTransfer.Move(a, b, "water", 1, _ => 0, 10)); Assert.Equal(1, a["water"]);
        }
        [Fact] public void UnknownCustomItemCanBeMovedWithoutDeletingIt()
        {
            var a = new Dictionary<string, int> { { "custom_mod_item", 3 } }; var b = new Dictionary<string, int>();
            Assert.True(ItemTransfer.Move(a, b, "custom_mod_item", 3, _ => .1f, 10)); Assert.Equal(3, b["custom_mod_item"]);
        }
        [Fact] public void BuyUsesWeightedCostAndSellConservesFunds()
        {
            var p = new Portfolio { Cash = 1000 };
            Assert.True(p.Buy("PIZ", 2, 10)); Assert.True(p.Buy("PIZ", 2, 20));
            Assert.Equal(15, p.AvgCost["PIZ"]); Assert.Equal(940, p.Cash);
            Assert.True(p.Sell("PIZ", 4, 25)); Assert.Equal(1040, p.Cash); Assert.Empty(p.Holdings); Assert.Empty(p.AvgCost);
        }
        [Theory] [InlineData(0, 10)] [InlineData(-1, 10)] [InlineData(1, 0)] [InlineData(1, -1)] [InlineData(1000, 10)]
        public void InvalidPurchaseDoesNotCreateShares(int quantity, int price)
        {
            var p = new Portfolio { Cash = 100 }; Assert.False(p.Buy("PIZ", quantity, price)); Assert.Equal(100, p.Cash); Assert.Empty(p.Holdings);
        }
        [Fact] public void CannotSellUnownedShares()
        {
            var p = new Portfolio(); Assert.False(p.Sell("PIZ", 1, 25)); Assert.Equal(0, p.Cash);
        }
        [Theory] [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)] [InlineData(-1f)]
        public void InvalidStorageCapacityCannotConsumeItems(float capacity)
        {
            var source = new Dictionary<string, int> { { "water", 3 } }; var target = new Dictionary<string, int>();
            Assert.False(ItemTransfer.Move(source, target, "water", 1, _ => 1, capacity)); Assert.Equal(3, source["water"]); Assert.Empty(target);
        }
        [Fact] public void CorruptDestinationCannotConsumeItems()
        {
            var source = new Dictionary<string, int> { { "water", 3 } }; var target = new Dictionary<string, int> { { "water", -10 } };
            Assert.False(ItemTransfer.Move(source, target, "water", 1, _ => 1, 100)); Assert.Equal(3, source["water"]); Assert.Equal(-10, target["water"]);
        }
        [Fact] public void OversizedTradeFailsWithoutOverflowOrMutation()
        {
            var p = new Portfolio { Cash = decimal.MaxValue };
            Assert.False(p.Buy("PIZ", 2, decimal.MaxValue)); Assert.Empty(p.Holdings);
            p.Holdings["PIZ"] = 2; Assert.False(p.Sell("PIZ", 2, decimal.MaxValue)); Assert.Equal(2, p.Holdings["PIZ"]);
        }
        [Fact] public void CustomWalletAndExchangeCommitTogether()
        {
            var p = new Portfolio { Cash = 100 }; p.Buy("PIZ", 2, 10);
            var files = new JsonFiles(); files.Write(FileName("Wallet.json"), new WalletData { Cash = 10, Bank = 500, Exchange = p });
            var restored = files.Read<WalletData>(FileName("Wallet.json"), () => null);
            Assert.Equal(10, restored.Cash); Assert.Equal(500, restored.Bank); Assert.Equal(80, restored.Exchange.Cash); Assert.Equal(2, restored.Exchange.Holdings["PIZ"]);
        }
    }
}
