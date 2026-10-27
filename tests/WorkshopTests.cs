using System;
using Newtonsoft.Json;
using Xunit;
namespace RPCore.Tests
{
    public class WorkshopTests
    {
        [Theory]
        [InlineData(0,-1,1,true)] [InlineData(49,0,1,true)] [InlineData(23,19,20,true)]
        [InlineData(-1,0,1,false)] [InlineData(50,0,1,false)] [InlineData(0,-2,1,false)]
        [InlineData(0,1,1,false)] [InlineData(0,-1,0,false)] [InlineData(17,0,1,false)]
        [InlineData(18,0,1,false)] [InlineData(20,0,1,false)] [InlineData(22,0,1,false)]
        [InlineData(0,0,2049,false)]
        public void OnlySupportedIndexedMods(int slot,int index,int count,bool expected)
            => Assert.Equal(expected,WorkshopRules.Mod(slot,index,count));
        [Fact] public void PaymentAndSavedVehicleCommitTogether()
        {
            var w=new WalletData{Bank=1000,Cash=42};int live=0,saves=0;
            Assert.True(WorkshopRules.Buy(w,200,()=>true,()=>live==1,()=>live=1,()=>live==1,()=>live=0,()=>{Assert.Equal(800,w.Bank);Assert.Equal(1,live);saves++;}));
            Assert.Equal(800,w.Bank);Assert.Equal(42,w.Cash);Assert.Equal(1,saves);
            Assert.False(WorkshopRules.Buy(w,200,()=>true,()=>live==1,()=>throw new Exception(),()=>true,()=>{},()=>saves++));
            Assert.Equal(800,w.Bank);Assert.Equal(1,saves);
        }
        [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(1001)]
        public void InvalidCostAndInsufficientBankCannotMutate(int cost)
        {var w=new WalletData{Bank=1000};Assert.False(WorkshopRules.Buy(w,cost,()=>true,()=>false,()=>throw new Exception(),()=>true,()=>throw new Exception(),()=>throw new Exception()));Assert.Equal(1000,w.Bank);}
        [Theory] [InlineData(false,false)] [InlineData(true,true)]
        public void InaccessibleOrInstalledHasNoEffect(bool access,bool same)
        {var w=new WalletData{Bank=500};Assert.False(WorkshopRules.Buy(w,200,()=>access,()=>same,()=>throw new Exception(),()=>true,()=>throw new Exception(),()=>throw new Exception()));Assert.Equal(500,w.Bank);}
        [Fact] public void NativeRefusalRevertsWithoutPayment()
        {var w=new WalletData{Bank=500};int live=0;Assert.False(WorkshopRules.Buy(w,200,()=>true,()=>false,()=>live=1,()=>false,()=>live=0,()=>throw new Exception()));Assert.Equal(0,live);Assert.Equal(500,w.Bank);}
        [Fact] public void LostAccessDuringApplicationReverts()
        {var w=new WalletData{Bank=500};bool access=true;int live=0;Assert.False(WorkshopRules.Buy(w,200,()=>access,()=>false,()=>{live=1;access=false;},()=>true,()=>live=0,()=>throw new Exception()));Assert.Equal(0,live);Assert.Equal(500,w.Bank);}
        [Fact] public void DiskFailureRestoresBankAndNativeState()
        {var w=new WalletData{Bank=500};int live=0;Assert.Throws<InvalidOperationException>(()=>WorkshopRules.Buy(w,200,()=>true,()=>false,()=>live=1,()=>true,()=>live=0,()=>throw new InvalidOperationException("disk")));Assert.Equal(500,w.Bank);Assert.Equal(0,live);}
        [Fact] public void NativeExceptionRestoresWithoutSaving()
        {var w=new WalletData{Bank=500};int live=0;Assert.Throws<InvalidOperationException>(()=>WorkshopRules.Buy(w,200,()=>true,()=>false,()=>{live=1;throw new InvalidOperationException();},()=>true,()=>live=0,()=>throw new Exception()));Assert.Equal(500,w.Bank);Assert.Equal(0,live);}
        [Fact] public void LegacySnapshotLeavesTireProtectionUnspecified()
        {var r=JsonConvert.DeserializeObject<SavedVehicle>("{\"Model\":123,\"Plate\":\"ABC\",\"Future\":42}");Assert.Null(r.TiresCanBurst);Assert.Contains("Future",JsonConvert.SerializeObject(r));}
        [Theory] [InlineData(true)] [InlineData(false)]
        public void TireProtectionRoundTrips(bool canBurst)
        {var r=new SavedVehicle{TiresCanBurst=canBurst};Assert.Equal(canBurst,JsonConvert.DeserializeObject<SavedVehicle>(JsonConvert.SerializeObject(r)).TiresCanBurst);}
    }
}
