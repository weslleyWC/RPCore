using System;
using System.Linq;
using Newtonsoft.Json;
using Xunit;
namespace RPCore.Tests
{
    public class PdmPickupTests
    {
        static PdmOptions Options=>PdmDefaults.Load();
        static WalletData Pending()
        {
            var w=new WalletData{Cash=777,Bank=100000};
            Assert.True(PdmTransactions.Purchase(w,Options,"own",()=>{},out _));
            Assert.True(PdmTransactions.Transfer(w,50000,true,"capital",()=>{},out _));
            Assert.True(PdmTransactions.Buy(w,Options,"asea","unit0","order0",()=>{},out _,true));
            Assert.True(PdmTransactions.Buy(w,Options,"asea","unit1","order1",()=>{},out _,true));return w;
        }
        [Theory] [InlineData(0,9999,false,true)] [InlineData(5,9999,false,true)] [InlineData(5.01,9999,false,false)] [InlineData(9999,0,true,true)] [InlineData(9999,30,true,true)] [InlineData(9999,30.01,false,false)]
        public void PickupCanStartAtOfficeOrPortButNotByOnlySettingDistantGps(double office,double port,bool atPort,bool allowed)
        {var w=Pending();var p=PdmPickupRules.Plan(w.Pdm,new[]{"unit0"},office,port);Assert.Equal(allowed,p.Allowed);Assert.Equal(atPort,p.AtPort);if(!allowed)Assert.Contains("GPS",p.Reason);}
        [Theory] [InlineData(double.NaN,0)] [InlineData(0,double.NaN)] [InlineData(double.PositiveInfinity,0)] [InlineData(0,double.PositiveInfinity)] [InlineData(-1,0)] [InlineData(0,-1)]
        public void InvalidDistancesNeverAuthorizeCreatingVehicles(double office,double port)
        {Assert.False(PdmPickupRules.Plan(Pending().Pdm,new[]{"unit0"},office,port).Allowed);}
        [Fact] public void PlanningAndGpsDoNotAlterMoneyOrCreateACollection()
        {var w=Pending();string before=JsonConvert.SerializeObject(w);Assert.True(PdmPickupRules.Plan(w.Pdm,new[]{"unit0","unit1"},9000,0).Allowed);Assert.Null(w.Pdm.Collection);Assert.Equal(before,JsonConvert.SerializeObject(w));Assert.Equal(777,w.Cash);Assert.Equal(25000,w.Bank);Assert.Equal(34000,w.Pdm.Cash);}
        [Theory] [InlineData(0)] [InlineData(3)]
        public void CannotReserveZeroOrMoreThanTwoUnits(int count)
        {Assert.False(PdmPickupRules.Plan(Pending().Pdm,Enumerable.Repeat("unit0",count),0,9000).Allowed);}
        [Fact] public void MissingDuplicatesReceivedAndDamagedOrdersAreRejected()
        {
            var w=Pending();Assert.False(PdmPickupRules.Plan(w.Pdm,new[]{"missing"},9000,0).Allowed);Assert.False(PdmPickupRules.Plan(w.Pdm,new[]{"unit0","unit0"},9000,0).Allowed);
            w.Pdm.Stock[0].AwaitingCollection=false;Assert.False(PdmPickupRules.Plan(w.Pdm,new[]{"unit0"},9000,0).Allowed);
            w.Pdm.Stock[0].AwaitingCollection=true;w.Pdm.Stock[0].Engine=299;Assert.False(PdmPickupRules.Plan(w.Pdm,new[]{"unit0"},9000,0).Allowed);
            w.Pdm.Stock[0].Engine=1000;w.Pdm.Stock[0].Body=299;Assert.False(PdmPickupRules.Plan(w.Pdm,new[]{"unit0"},9000,0).Allowed);
        }
        static PdmEntityRef Vehicle(string model,int h)=>new PdmEntityRef{Model=model,Handle=h,Plate="RP"+h};
        [Fact] public void APortCollectionPaysNoSecondTimeAndOnlyReceivesTheChosenUnit()
        {
            var w=Pending();var original=new[]{w.Cash,w.Bank,w.Pdm.Cash};
            var p=PdmPickupRules.Plan(w.Pdm,new[]{"unit0"},9000,0);Assert.True(p.Allowed);Assert.True(p.AtPort);
            Assert.True(PdmCollectionTransactions.Begin(w,"pickup",p.StockIds,Vehicle("packer",100),Vehicle("trflat",200),()=>{},out _));
            Assert.False(PdmPickupRules.Plan(w.Pdm,new[]{"unit1"},9000,0).Allowed);
            Assert.True(PdmCollectionTransactions.Register(w,"pickup","unit0",300,()=>{},out _));
            Assert.True(PdmCollectionTransactions.Arrive(w,"pickup",()=>{},out _));Assert.True(PdmCollectionTransactions.Complete(w,"pickup",()=>{},out _));
            Assert.False(w.Pdm.Stock[0].AwaitingCollection);Assert.True(w.Pdm.Stock[1].AwaitingCollection);Assert.Equal(original,new[]{w.Cash,w.Bank,w.Pdm.Cash});
            Assert.True(PdmCollectionTransactions.Complete(w,"pickup",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(original,new[]{w.Cash,w.Bank,w.Pdm.Cash});
        }
        [Fact] public void FailedCollectionSaveKeepsPaidOrdersAndOriginalBalances()
        {var w=Pending();string before=JsonConvert.SerializeObject(w);Assert.Throws<Exception>(()=>PdmCollectionTransactions.Begin(w,"pickup",new System.Collections.Generic.List<string>{"unit0"},Vehicle("packer",100),Vehicle("trflat",200),()=>throw new Exception(),out _));Assert.Equal(before,JsonConvert.SerializeObject(w));}
        [Fact] public void LegacyPaidOrdersSurviveReloadWithoutNewPaymentFields()
        {var w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(Pending()));Assert.True(PdmPickupRules.Plan(w.Pdm,new[]{"unit0"},9000,0).Allowed);Assert.True(w.Pdm.Stock[0].AwaitingCollection);Assert.Equal(34000,w.Pdm.Cash);}
        [Theory] [InlineData(0,0,-24)] [InlineData(90,24,0)] [InlineData(180,0,24)] [InlineData(270,-24,0)]
        public void RigAnchorFollowsConfiguredPortAndHeadingAwayFromCargo(float heading,float dx,float dy)
        {var dock=new JobPoint{Name="Custom port",X=100,Y=200,Z=12,Heading=heading};var anchor=PdmPickupRules.PortRigAnchor(dock);Assert.InRange(anchor.X,100+dx-.001,100+dx+.001);Assert.InRange(anchor.Y,200+dy-.001,200+dy+.001);Assert.Equal(12,anchor.Z);Assert.Equal(heading,anchor.Heading);Assert.Equal(100,dock.X);}
        [Fact] public void InvalidPortCannotProduceAWorldAnchor()=>Assert.Throws<ArgumentException>(()=>PdmPickupRules.PortRigAnchor(null));
    }
}
