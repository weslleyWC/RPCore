using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class HousingTests
    {
        static readonly Action Save = () => { };
        static HousingOptions C => HousingDefaults.Load();
        static WalletData W() => new WalletData { Bank=2000000, Cash=123 };
        [Fact] public void DefaultsMatchReferencePriceEntranceInteriorExitAndIpl()
        { var c=C; Assert.True(c.Valid); Assert.Equal(1425000,c.Price); Assert.Equal(-773.2661f,c.Entrance.X); Assert.Equal(85.6981f,c.Entrance.Z); Assert.Equal(-786.8663f,c.Interior.X); Assert.Equal(187.9136f,c.Interior.Z); Assert.Equal(269.61f,c.Interior.Heading); Assert.Equal(-773.6778f,c.Exit.X); Assert.Equal(180.4528f,c.Exit.Heading); Assert.Equal("apa_v_mp_h_01_c",c.Ipl); }
        [Fact] public void LegacyMissingHousingDoesNotCreateOwnershipOrCharge()
        { var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":2000000,\"Cash\":123}"); Assert.Null(w.Housing); Assert.Equal(2000000,w.Bank); Assert.Empty(new HousingData().Properties); }
        [Fact] public void PurchaseChargesBankExactlyOnceAndSavesOwnershipSnapshot()
        { var w=W(); Assert.True(HousingTransactions.Purchase(w,C,"buy",Save,out bool changed)); Assert.True(changed); Assert.Equal(575000,w.Bank); Assert.Equal(123,w.Cash); var home=Assert.Single(w.Housing.Properties); Assert.Equal("eclipse",home.Id); Assert.Equal(1425000,home.Paid); Assert.True(w.Housing.Valid); }
        [Fact] public void SaveFailureRestoresOriginalBankAndNullState()
        { var w=W(); Assert.Throws<Exception>(()=>HousingTransactions.Purchase(w,C,"buy",()=>throw new Exception(),out _)); Assert.Equal(2000000,w.Bank); Assert.Null(w.Housing); }
        [Fact] public void SaveFailureWithExistingUnknownDataRestoresExactOriginalObject()
        { var w=W(); w.Housing=JsonConvert.DeserializeObject<HousingData>("{\"Properties\":[],\"Future\":42}"); var original=w.Housing; string before=JsonConvert.SerializeObject(w); Assert.Throws<Exception>(()=>HousingTransactions.Purchase(w,C,"buy",()=>throw new Exception(),out _)); Assert.Same(original,w.Housing); Assert.Equal(before,JsonConvert.SerializeObject(w)); }
        [Fact] public void ReplayAfterReloadDoesNotChargeAgainEvenIfBalanceBelowPrice()
        { var w=W(); HousingTransactions.Purchase(w,C,"buy",Save,out _); w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w)); Assert.True(HousingTransactions.Purchase(w,C,"buy",()=>throw new Exception(),out bool changed)); Assert.False(changed); Assert.Equal(575000,w.Bank); Assert.Single(w.Housing.Properties); Assert.False(HousingTransactions.Purchase(w,C,"second",Save,out _)); }
        [Theory] [InlineData(0)] [InlineData(1424999)]
        public void InsufficientBankLeavesWalletUntouched(int bank)
        { var w=W(); w.Bank=bank; Assert.False(HousingTransactions.Purchase(w,C,"buy",Save,out _)); Assert.Equal(bank,w.Bank); Assert.Null(w.Housing); }
        [Fact] public void ExactPriceCanBePaidWithoutBankUnderflow()
        { var w=W(); w.Bank=C.Price; Assert.True(HousingTransactions.Purchase(w,C,"buy",Save,out _)); Assert.Equal(0,w.Bank); }
        [Fact] public void PriceChangeDoesNotRewriteOwnershipOrPermitDuplicatePurchase()
        { var w=W(); HousingTransactions.Purchase(w,C,"buy",Save,out _); var c=C; c.Price=1; Assert.False(HousingTransactions.Purchase(w,c,"another",Save,out _)); Assert.Equal(1425000,w.Housing.Properties[0].Paid); Assert.Equal(575000,w.Bank); }
        [Fact] public void PurchaseDoesNotTouchBusinessTransportJobsOrUnknownWalletFields()
        { var w=W(); var j=JObject.Parse(JsonConvert.SerializeObject(w)); j["Future"]=42; j["Pdm"]=JObject.Parse("{\"Owned\":true,\"Cash\":321,\"Future\":9}"); w=j.ToObject<WalletData>(); var pdm=w.Pdm; Assert.True(HousingTransactions.Purchase(w,C,"buy",Save,out _)); Assert.Same(pdm,w.Pdm); Assert.Equal(321,w.Pdm.Cash); Assert.Equal(42,(int)JObject.Parse(JsonConvert.SerializeObject(w))["Future"]); }
        [Fact] public void UnknownHousingFieldsSurvivePurchaseAndExistingPropertiesAreRetained()
        { var w=W(); w.Housing=JsonConvert.DeserializeObject<HousingData>("{\"Future\":42,\"Properties\":[{\"Id\":\"custom\",\"Name\":\"Other\",\"Paid\":100,\"Future\":7}]}"); Assert.True(HousingTransactions.Purchase(w,C,"buy",Save,out _)); var j=JObject.Parse(JsonConvert.SerializeObject(w.Housing)); Assert.Equal(42,(int)j["Future"]); Assert.Equal(7,(int)j["Properties"][0]["Future"]); Assert.Equal(2,w.Housing.Properties.Count); }
        [Fact] public void InvalidHousingDataCannotBeOverwrittenByPurchase()
        { var w=W(); w.Housing=new HousingData(); w.Housing.Properties.Add(new HousingProperty{Id="broken",Name="Broken",Paid=-1}); Assert.False(HousingTransactions.Purchase(w,C,"buy",Save,out _)); Assert.Equal(2000000,w.Bank); Assert.Equal(-1,w.Housing.Properties[0].Paid); }
        [Theory] [InlineData(0)] [InlineData(100000001)]
        public void InvalidPriceCannotCharge(int price)
        { var c=C; c.Price=price; var w=W(); Assert.False(HousingTransactions.Purchase(w,c,"buy",Save,out _)); Assert.Null(w.Housing); }
        [Fact] public void InvalidCoordinatesAndDuplicateOwnedIdsAreRejected()
        { var c=C; c.Interior.Z=float.NaN; Assert.False(c.Valid); var d=new HousingData(); var home=new HousingProperty{Id="eclipse",Name="Eclipse",Paid=100}; d.Properties.Add(home); d.Properties.Add(home); Assert.False(d.Valid); }
        [Theory] [InlineData(187.9136f,186.9136f,1f,true)] [InlineData(187.9136f,185f,1f,false)] [InlineData(187.9136f,190f,1f,false)] [InlineData(187.9136f,186.9136f,.2f,false)] [InlineData(float.NaN,186.9136f,1f,false)] [InlineData(187.9136f,float.PositiveInfinity,1f,false)]
        public void FloorProbeRejectsMissingWrongHeightSteepOrNonfiniteSurface(float target,float hit,float normal,bool expected)
        { Assert.Equal(expected,HousingPlacement.Floor(target,hit,normal)); }
        [Fact] public void TransitWaitsForLoadedFloorThenMovesOnlyOnce()
        { var t=new HousingTransit(0,15); int moves=0; Assert.False(t.Update(100,true,false,()=>moves++)); Assert.True(t.Update(200,true,true,()=>moves++)); Assert.False(t.Update(300,true,true,()=>moves++)); Assert.Equal(1,moves); }
        [Fact] public void TimeoutOrLeavingEligibilityNeverMovesPlayer()
        { var t=new HousingTransit(0,15); Assert.False(t.Update(15000,true,true,()=>throw new Exception())); Assert.Equal(HousingTransitStage.Cancelled,t.Stage); t=new HousingTransit(0,15); Assert.False(t.Update(1,false,true,()=>throw new Exception())); Assert.Equal(HousingTransitStage.Cancelled,t.Stage); }
        [Fact] public void TransitHandlesGameTimeOverflowAndFailedMoveIsNotCommitted()
        { int now=int.MaxValue-10; var t=new HousingTransit(now,15); Assert.Throws<Exception>(()=>t.Update(unchecked(now+100),true,true,()=>throw new Exception())); Assert.Equal(HousingTransitStage.Waiting,t.Stage); Assert.True(t.Update(unchecked(now+200),true,true,()=>{})); }
        [Fact] public void SeparateProfilesHaveIndependentOwnershipAndBalances()
        { var a=W(); var b=W(); HousingTransactions.Purchase(a,C,"buy",Save,out _); Assert.Null(b.Housing); Assert.Equal(2000000,b.Bank); Assert.True(a.Housing.Owns("eclipse")); }
    }
}
