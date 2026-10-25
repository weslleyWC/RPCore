using System;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangBulkTests
    {
        static readonly Action Save=()=>{};
        static GangTurfDefinition Area()=>GangTurfOptions.Defaults().Areas[0];
        static GangState S()=>new GangState{Gang=Area().DefaultOwner,Reputation=800,Rank=3};
        static DealerOptions C()=>DealerDefaults.Load();
        static WalletData W()
        {var w=new WalletData{Bank=10000,Cash=123,Dealer=new DealerData{Stock={{"weed",5}}},GangWarehouse=new GangWarehouseData()};w.GangWarehouse.Accounts.Add(new GangWarehouseAccount{Gang=S().Gang,Area=Area().Id,Stock={{"weed",50},{"mdma",50}},Deposited={{"weed",50},{"mdma",50}}});return w;}
        static GangBulkAttempt P(int roll=0)=>new GangBulkAttempt{Id="bulk",Gang=S().Gang,Buyer="Other",Product="weed",Model="g_m_y_famca_01",InitialAttitude="Hate",Units=25,Value=2625,RelationRoll=roll,Area=Area(),Site=new GangBulkOptions().Sites[0],Options=new GangBulkOptions()};
        static bool Begin(WalletData w,GangBulkAttempt p=null,Action save=null)=>GangBulkTransactions.Begin(w,S(),p??P(),C(),save??Save,out _);
        static WalletData Started(int roll=0){var w=W();Assert.True(Begin(w,P(roll)));return w;}
        static WalletData Ready(int roll=0){var w=Started(roll);Assert.True(GangBulkTransactions.Load(w,"bulk",Save,out _));Assert.True(GangBulkTransactions.Ready(w,"bulk",Save,out _));return w;}
        static bool Complete(WalletData w,Action save=null,DealerOptions c=null)=>GangBulkTransactions.Complete(w,S(),Area(),c??C(),"bulk",save??Save,out _);
        [Fact] public void DefaultsUseSixReferenceDestinationsBoundedValueAndQuantities()
        {var c=new GangBulkOptions();Assert.True(c.Valid);Assert.Equal(new[]{25,50,100,200,500},c.Quantities);Assert.Equal(6,c.Sites.Count);Assert.Equal(75000,c.MaxValue);Assert.Equal(1200,c.SecondsLimit);Assert.Equal(3,c.HandoverSeconds);Assert.True(P().Valid);}
        [Fact] public void QuoteUsesIntegerOneAndHalfDealerPriceWithCap()
        {Assert.Equal(2625,GangBulkTransactions.Quote(C(),"weed",25));Assert.Equal(75000,GangBulkTransactions.Quote(C(),"mdma",500));Assert.Equal(0,GangBulkTransactions.Quote(C(),"cocaine",500));Assert.Equal(0,GangBulkTransactions.Quote(C(),"missing",25));Assert.Equal(0,GangBulkTransactions.Quote(C(),"weed",0));Assert.Equal(0,GangBulkTransactions.Quote(C(),"weed",501));var c=C();c.Products[0].Sell=71;Assert.Equal(2662,GangBulkTransactions.Quote(c,"weed",25));}
        [Fact] public void StartingLocksWithoutDebitingStockMoneyOrCreatingIncome()
        {var w=W();var stock=w.GangWarehouse;var dealer=w.Dealer;Assert.True(Begin(w));Assert.Same(stock,w.GangWarehouse);Assert.Same(dealer,w.Dealer);Assert.Equal(50,stock.Accounts[0].Stock["weed"]);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);Assert.Null(w.GangDiplomacy);Assert.True(GangBulkTransactions.Blocked(w));Assert.Equal(1,w.GangBulk.Attempts);}
        [Fact] public void CallerCannotRerollSnapshotAfterBegin()
        {var w=W();var p=P();Begin(w,p);p.RelationRoll=99;p.Value=1;p.Site.X=0;p.Options.HandoverSeconds=10;p.Area.Name="changed";Assert.Equal(0,w.GangBulk.Pending.RelationRoll);Assert.Equal(2625,w.GangBulk.Pending.Value);Assert.NotEqual(0,w.GangBulk.Pending.Site.X);Assert.Equal(3,w.GangBulk.Pending.Options.HandoverSeconds);Assert.NotEqual("changed",w.GangBulk.Pending.Area.Name);}
        [Fact] public void InsufficientStockOrWrongQuoteCannotStart()
        {var w=W();w.GangWarehouse.Accounts[0].Stock["weed"]=24;w.GangWarehouse.Accounts[0].Deposited["weed"]=24;Assert.False(Begin(w));var p=P();p.Value++;Assert.False(Begin(W(),p));Assert.Null(w.GangBulk);}
        [Fact] public void BeginRequiresCurrentOwnerAffiliationAndNoPhysicalProof()
        {var w=W();var s=S();s.Gang="Other";Assert.False(GangBulkTransactions.Begin(w,s,P(),C(),Save,out _));var p=P();p.Loaded=true;Assert.False(Begin(w,p));p=P();p.Buyer=p.Gang;Assert.False(Begin(w,p));Assert.Null(w.GangBulk);}
        [Fact] public void SnapshotRejectsInvalidRollAndQuantities()
        {var p=P();p.RelationRoll=100;Assert.False(p.Valid);p.RelationRoll=-1;Assert.False(p.Valid);p=P();p.Units=26;Assert.False(p.Valid);p=P();p.Ready=true;Assert.False(p.Valid);}
        [Fact] public void LoadingAndHandoverAreSeparateRequiredCheckpoints()
        {var w=Started();Assert.False(Complete(w));Assert.False(GangBulkTransactions.Ready(w,"bulk",Save,out _));Assert.True(GangBulkTransactions.Load(w,"bulk",Save,out _));Assert.False(Complete(w));Assert.True(GangBulkTransactions.Ready(w,"bulk",Save,out _));Assert.Null(w.GangIllicit);Assert.Equal(50,w.GangWarehouse.Accounts[0].Stock["weed"]);Assert.True(Complete(w));}
        [Fact] public void CompleteDebitsOnlyLocalProductAndCreditsDirtyRevenue()
        {var w=Ready();var dealer=w.Dealer;Assert.True(Complete(w));Assert.Null(w.GangBulk.Pending);Assert.Equal(1,w.GangBulk.Completed);Assert.Equal(2625,w.GangBulk.Revenue);Assert.Equal(25,w.GangWarehouse.Accounts[0].Stock["weed"]);Assert.Equal(25,w.GangWarehouse.Accounts[0].Sold["weed"]);Assert.Equal(50,w.GangWarehouse.Accounts[0].Stock["mdma"]);var income=GangIllicitTransactions.Account(w.GangIllicit,S().Gang,Area().Id);Assert.Equal(2625,income.Dirty);Assert.Equal(2625,income.Proceeds);Assert.Same(dealer,w.Dealer);Assert.Equal(5,dealer.Stock["weed"]);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangTreasury);Assert.True(w.GangWarehouse.Valid);Assert.True(w.GangIllicit.Valid);Assert.False(GangBulkTransactions.Blocked(w));}
        [Theory][InlineData(0,"Neutral")][InlineData(29,"Neutral")][InlineData(30,"Hate")][InlineData(99,"Hate")]
        public void SavedRollGivesThirtyPercentBoundaryAndCountsEveryDeal(int roll,string expected)
        {var w=Ready(roll);Assert.True(Complete(w));var a=GangDiplomacyRules.Account(w.GangDiplomacy,S().Gang,"Other");Assert.Equal(expected,a.Attitude);Assert.Equal(1,a.BulkDeals);Assert.Equal(0,a.Agreements);Assert.Equal(0,a.Paid);}
        [Fact] public void ExistingAttitudeOverridesContractInitialAndRespectsUpperBound()
        {var w=Ready();w.GangDiplomacy=new GangDiplomacyData();w.GangDiplomacy.Accounts.Add(new GangDiplomacyAccount{Gang=S().Gang,Rival="Other",Attitude="Respect",StolenLoads=3});Complete(w);Assert.Equal("Respect",w.GangDiplomacy.Accounts[0].Attitude);Assert.Equal(3,w.GangDiplomacy.Accounts[0].StolenLoads);}
        [Fact] public void DeliveryReplayAfterReloadCannotDebitOrPayTwice()
        {var w=Ready();Complete(w);w=CharacterRules.Copy(w);Assert.True(Complete(w,()=>throw new Exception()));Assert.Equal(25,w.GangWarehouse.Accounts[0].Stock["weed"]);Assert.Equal(2625,w.GangIllicit.Accounts[0].Dirty);Assert.Equal(1,w.GangDiplomacy.Accounts[0].BulkDeals);Assert.False(GangBulkTransactions.Cancel(w,"bulk",Save,out _));}
        [Fact] public void PartialRestartRequiresFreshLoadWithSameQuoteDestinationRollAndAttempt()
        {var w=Started(29);GangBulkTransactions.Load(w,"bulk",Save,out _);Assert.True(GangBulkTransactions.Restart(w,"bulk",Save,out _));Assert.False(w.GangBulk.Pending.Loaded);Assert.Equal(29,w.GangBulk.Pending.RelationRoll);Assert.Equal(2625,w.GangBulk.Pending.Value);Assert.Equal(1,w.GangBulk.Attempts);Assert.False(GangBulkTransactions.Ready(w,"bulk",Save,out _));Assert.True(GangBulkTransactions.Load(w,"bulk",Save,out bool changed));Assert.True(changed);}
        [Fact] public void ReadySurvivesReloadWithoutFreshCarProofOrReroll()
        {var w=CharacterRules.Copy(Ready(30));Assert.True(w.GangBulk.Pending.Ready);Assert.False(GangBulkTransactions.Restart(w,"bulk",Save,out _));Assert.True(Complete(w));Assert.Equal("Hate",w.GangDiplomacy.Accounts[0].Attitude);}
        [Theory][InlineData(false)][InlineData(true)]
        public void CancelBeforeOrAfterReadyOnlyUnlocksStock(bool ready)
        {var w=ready?Ready():Started();var stock=w.GangWarehouse;Assert.True(GangBulkTransactions.Cancel(w,"bulk",Save,out _));Assert.False(GangBulkTransactions.Blocked(w));Assert.Same(stock,w.GangWarehouse);Assert.Equal(50,w.GangWarehouse.Accounts[0].Stock["weed"]);Assert.Null(w.GangIllicit);Assert.Null(w.GangDiplomacy);Assert.Equal(1,w.GangBulk.Cancelled);Assert.Equal(10000,w.Bank);Assert.True(GangBulkTransactions.Cancel(w,"bulk",()=>throw new Exception(),out bool changed));Assert.False(changed);}
        [Fact] public void CancelRemainsPossibleWithoutAffiliationOrAreaAndEvenIfCatalogRemoved()
        {var w=Ready();Assert.True(GangBulkTransactions.Cancel(w,"bulk",Save,out _));Assert.Equal(50,w.GangWarehouse.Accounts[0].Stock["weed"]);}
        [Fact] public void LossOfOwnershipOrWrongAreaBlocksFinalCreditWithoutDestroyingReadyCargo()
        {var w=Ready();var s=S();s.Gang="Other";Assert.False(GangBulkTransactions.Complete(w,s,Area(),C(),"bulk",Save,out _));Assert.False(GangBulkTransactions.Complete(w,S(),GangTurfOptions.Defaults().Areas[1],C(),"bulk",Save,out _));Assert.True(w.GangBulk.Pending.Ready);Assert.Null(w.GangIllicit);}
        [Fact] public void ChangedPriceDoesNotRepriceCapturedDealButMissingSkuBlocksIt()
        {var w=Ready();var c=C();c.Products.Find(p=>p.Id=="weed").Sell=1000;Assert.True(Complete(w,c:c));Assert.Equal(2625,w.GangIllicit.Accounts[0].Dirty);w=Ready();c=C();c.Products.RemoveAll(p=>p.Id=="weed");Assert.False(Complete(w,c:c));Assert.True(w.GangBulk.Pending.Ready);}
        [Fact] public void StockIsRevalidatedBeforeFinalizationRatherThanMadeNegative()
        {var w=Ready();var a=w.GangWarehouse.Accounts[0];a.Stock["weed"]=0;a.Deposited["weed"]=0;Assert.True(w.GangWarehouse.Valid);Assert.False(Complete(w));Assert.Null(w.GangIllicit);Assert.True(w.GangBulk.Pending.Ready);}
        [Fact] public void FailedFinalSaveRestoresAllFourOriginalBooksAndPersonalData()
        {var w=Ready();var b=w.GangBulk;var a=w.GangWarehouse;var dealer=w.Dealer;Assert.Throws<Exception>(()=>Complete(w,()=>{Assert.Null(w.GangBulk.Pending);Assert.Equal(25,w.GangWarehouse.Accounts[0].Stock["weed"]);Assert.Equal(2625,w.GangIllicit.Accounts[0].Dirty);Assert.Equal(1,w.GangDiplomacy.Accounts[0].BulkDeals);throw new Exception();}));Assert.Same(b,w.GangBulk);Assert.Same(a,w.GangWarehouse);Assert.Same(dealer,w.Dealer);Assert.Null(w.GangIllicit);Assert.Null(w.GangDiplomacy);Assert.True(w.GangBulk.Pending.Ready);Assert.Equal(50,a.Accounts[0].Stock["weed"]);Assert.Equal(10000,w.Bank);}
        [Fact] public void FailedFinalSaveAlsoRestoresExistingIncomeAndDiplomacyReferences()
        {var w=Ready();w.GangIllicit=new GangIllicitData();w.GangIllicit.Accounts.Add(new GangIllicitAccount{Gang=S().Gang,Area=Area().Id,Dirty=100,Proceeds=100});w.GangDiplomacy=new GangDiplomacyData();w.GangDiplomacy.Accounts.Add(new GangDiplomacyAccount{Gang=S().Gang,Rival="Other",Attitude="War"});var d=w.GangIllicit;var r=w.GangDiplomacy;Assert.Throws<Exception>(()=>Complete(w,()=>throw new Exception()));Assert.Same(d,w.GangIllicit);Assert.Same(r,w.GangDiplomacy);Assert.Equal("War",r.Accounts[0].Attitude);Assert.Equal(100,d.Accounts[0].Dirty);}
        [Fact] public void FailedStartSaveRestoresAbsentBook()
        {var w=W();Assert.Throws<Exception>(()=>Begin(w,save:()=>throw new Exception()));Assert.Null(w.GangBulk);Assert.Equal(50,w.GangWarehouse.Accounts[0].Stock["weed"]);}
        [Theory][InlineData("load")][InlineData("ready")][InlineData("restart")][InlineData("cancel")]
        public void FailedCheckpointRestoresOriginalContract(string step)
        {var w=Started();if(step=="ready" || step=="restart")GangBulkTransactions.Load(w,"bulk",Save,out _);var old=w.GangBulk;Action fail=()=>throw new Exception();Assert.Throws<Exception>(()=>{if(step=="load")GangBulkTransactions.Load(w,"bulk",fail,out _);if(step=="ready")GangBulkTransactions.Ready(w,"bulk",fail,out _);if(step=="restart")GangBulkTransactions.Restart(w,"bulk",fail,out _);if(step=="cancel")GangBulkTransactions.Cancel(w,"bulk",fail,out _);});Assert.Same(old,w.GangBulk);}
        [Fact] public void WrongIdsNeverConfirmDifferentContract()
        {var w=Started();Assert.False(GangBulkTransactions.Load(w,"other",Save,out _));Assert.False(GangBulkTransactions.Ready(w,"other",Save,out _));Assert.False(GangBulkTransactions.Cancel(w,"other",Save,out _));Assert.False(GangBulkTransactions.Restart(w,"other",Save,out _));Assert.Equal(0,w.GangBulk.Completed);}
        [Fact] public void PendingFreightOrRaidPreventsBeginningBulk()
        {var w=W();w.GangBusiness=new GangBusinessData{Supply=new GangSupplyOrder()};Assert.False(Begin(w));w.GangBusiness=null;w.GangRaid=new GangRaidData{Attempts=-1};Assert.False(Begin(w));Assert.Null(w.GangBulk);}
        [Fact] public void PendingBulkPreventsAnotherBulkLegalCriminalFreightAndRaid()
        {var w=Started();Assert.True(Begin(w));var next=P();next.Id="second";Assert.False(Begin(w,next));Assert.Equal(1,w.GangBulk.Attempts);Assert.False(GangSupplyTransactions.Begin(w,S(),Area(),new GangBusinessOptions(),new GangSupplyOptions(),"legal",Save,out _));Assert.False(GangSupplyTransactions.BeginCriminal(w,S(),Area(),C(),new GangWarehouseOptions(),new GangCriminalSupplyOptions(),"weed","criminal",Save,out _));var p=new GangRaidAttempt{Id="raid",Gang=S().Gang,Rival="Other",Area=Area(),Options=new GangRaidOptions(),Site=new GangRaidOptions().Sites[0],Capacity=30,Models=new[]{"g_m_y_famca_01","g_m_y_famca_01","g_m_y_famca_01"},Loot={{"weed",35},{"mdma",35}}};Assert.True(p.Valid);Assert.False(GangRaidTransactions.Begin(w,S(),p,C(),Save,out _));}
        [Fact] public void PendingBulkBlocksLocalTransferSaleProcessingTradeAndAgreement()
        {var w=Started();Assert.False(GangWarehouseTransactions.Transfer(w,S(),Area(),C(),new GangWarehouseOptions(),"weed",1,false,"out",Save,out _));Assert.False(GangWarehouseTransactions.Sale(w,S(),Area(),C(),"weed",1,"DAVIS",false,70,false,"sale",Save,out _));Assert.False(GangIllicitTransactions.Sale(w,S(),Area(),C(),"weed",1,"DAVIS",false,70,false,"personal",Save,out _));Assert.False(GangIllicitTransactions.Process(w,S(),Area(),new GangLaunderingOptions(),100,new DateTime(2013,9,17),"process",Save,out _));Assert.False(GangBusinessTransactions.Trade(w,S(),Area(),new GangBusinessOptions(),false,new DateTime(2013,9,17),"trade",Save,out _));Assert.False(GangDiplomacyTransactions.Negotiate(w,S(),"Other","Hate",new GangDiplomacyOptions(),new DateTime(2013,9,17),"agreement",Save,out _));}
        [Fact] public void BulkProceedsUseExistingEightyFivePercentProcessing()
        {var w=Ready();Complete(w);Assert.True(GangIllicitTransactions.Process(w,S(),Area(),new GangLaunderingOptions(),500,new DateTime(2013,9,17),"process",Save,out _));Assert.Equal(2125,w.GangIllicit.Accounts[0].Dirty);Assert.Equal(425,GangTreasuryTransactions.Account(w.GangTreasury,S().Gang).Balance);Assert.Equal(10000,w.Bank);}
        [Fact] public void DirtyBalanceOverflowKeepsStockAndPendingUnchanged()
        {var w=Ready();w.GangIllicit=new GangIllicitData();w.GangIllicit.Accounts.Add(new GangIllicitAccount{Gang=S().Gang,Area=Area().Id,Dirty=int.MaxValue,Proceeds=int.MaxValue});var old=w.GangWarehouse;Assert.False(Complete(w));Assert.Same(old,w.GangWarehouse);Assert.True(w.GangBulk.Pending.Ready);}
        [Fact] public void HistoryOrDiplomacyOverflowCannotGivePartialPayment()
        {var w=Ready();w.GangBulk.Revenue=long.MaxValue;Assert.False(Complete(w));w=Ready();w.GangDiplomacy=new GangDiplomacyData();w.GangDiplomacy.Accounts.Add(new GangDiplomacyAccount{Gang=S().Gang,Rival="Other",BulkDeals=long.MaxValue});Assert.False(Complete(w));Assert.Equal(50,w.GangWarehouse.Accounts[0].Stock["weed"]);Assert.Null(w.GangIllicit);}
        [Fact] public void CorruptBooksAreProtectedAndNotReplaced()
        {var w=Ready();w.GangDiplomacy=new GangDiplomacyData{Accounts=null};Assert.False(Complete(w));Assert.True(w.GangBulk.Pending.Ready);w=W();w.GangBulk=new GangBulkData{Attempts=-1};Assert.True(GangBulkTransactions.Blocked(w));Assert.False(Begin(w));Assert.False(GangBulkTransactions.Cancel(w,"bulk",Save,out _));}
        [Fact] public void FutureFieldsSurviveAllFourBookUpdates()
        {var w=Ready();var j=JObject.FromObject(w);j["Future"]="wallet";j["GangBulk"]["Future"]="bulk";j["GangBulk"]["Pending"]["Future"]="order";j["GangWarehouse"]["Accounts"][0]["Future"]="stock";w=j.ToObject<WalletData>();w.GangIllicit=JObject.Parse("{Accounts:[],Receipts:[],Future:'dirty'}").ToObject<GangIllicitData>();w.GangDiplomacy=JObject.Parse("{Accounts:[],Receipts:[],Future:'relations'}").ToObject<GangDiplomacyData>();Assert.Equal("order",(string)JObject.FromObject(w)["GangBulk"]["Pending"]["Future"]);Complete(w);j=JObject.FromObject(w);Assert.Equal("wallet",(string)j["Future"]);Assert.Equal("bulk",(string)j["GangBulk"]["Future"]);Assert.Equal("stock",(string)j["GangWarehouse"]["Accounts"][0]["Future"]);Assert.Equal("dirty",(string)j["GangIllicit"]["Future"]);Assert.Equal("relations",(string)j["GangDiplomacy"]["Future"]);}
        [Fact] public void LegacyWalletAndRelationsDoNotNeedMigration()
        {var w=JObject.Parse("{Bank:1,Future:'keep'}").ToObject<WalletData>();Assert.Null(w.GangBulk);Assert.False(GangBulkTransactions.Blocked(w));var a=JObject.Parse("{Gang:'Mine',Rival:'Other',Attitude:'Hate'}").ToObject<GangDiplomacyAccount>();Assert.True(a.Valid);Assert.Equal(0,a.BulkDeals);}
        [Fact] public void ConfigArraysReplaceDefaultsAndInvalidBoundsFail()
        {var c=JObject.Parse("{MaxValue:75000,SecondsLimit:1200,HandoverSeconds:3,Quantities:[25],Sites:[{Name:'One',X:100,Y:100,Z:30}]}").ToObject<GangBulkOptions>();Assert.True(c.Valid);Assert.Single(c.Quantities);Assert.Single(c.Sites);c.MaxValue=75001;Assert.False(c.Valid);c.MaxValue=75000;c.Quantities.Add(25);Assert.False(c.Valid);}
        [Fact] public void LoadingAndReadyReplaysDoNotSaveAgain()
        {var w=Ready();Assert.True(GangBulkTransactions.Load(w,"bulk",()=>throw new Exception(),out bool loaded));Assert.False(loaded);Assert.True(GangBulkTransactions.Ready(w,"bulk",()=>throw new Exception(),out bool ready));Assert.False(ready);Assert.Equal(50,w.GangWarehouse.Accounts[0].Stock["weed"]);}
        [Fact] public void ProfilesHaveIndependentContractsAndRevenue()
        {var w=Ready();var other=W();Complete(w);Assert.Null(other.GangBulk);Assert.Null(other.GangIllicit);Assert.Equal(50,other.GangWarehouse.Accounts[0].Stock["weed"]);}
        [Fact] public void WarehouseLifetimeSalesOverflowDoesNotConsumeStockOrPay()
        {var w=Ready();var a=w.GangWarehouse.Accounts[0];a.Deposited["weed"]=long.MaxValue-20;a.Sold["weed"]=long.MaxValue-20;a.Raided["weed"]=50;Assert.True(w.GangWarehouse.Valid);Assert.False(Complete(w));Assert.Equal(50,a.Stock["weed"]);Assert.Null(w.GangIllicit);Assert.True(w.GangBulk.Pending.Ready);}
    }
}
