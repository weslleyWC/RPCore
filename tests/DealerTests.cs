using System;
using Newtonsoft.Json;
using Xunit;

namespace RPCore.Tests
{
    public class DealerTests
    {
        static DealerOptions Options()=>DealerDefaults.Load();
        static WalletData Stock(int bank=2000)=>new WalletData{Bank=bank,Cash=500,Dealer=new DealerData{Stock={ ["weed"]=5,["cocaine"]=5,["mdma"]=5,["lean"]=5,["crack"]=5 }}};
        static DealerRun Run()=>new DealerRun(3,120,3);
        static int Price(WalletData w,string id="weed",bool premium=false)=>DealerRules.UnitPrice(Options().Products.Find(p=>p.Id==id),DealerRules.Reputation(w.Dealer,"DAVIS"),premium);
        static bool Sale(WalletData w,string receipt="s",bool last=false)=>DealerTransactions.Sale(w,Options(),"weed",1,"DAVIS",false,Price(w),last,receipt,()=>{},out _);
        [Fact] public void DefaultsAreValidAndHaveFiveReferenceCategories()
        {var c=Options();Assert.True(c.Valid);Assert.Equal(5,c.Products.Count);Assert.Equal(-1309.499f,c.Supplier.X);Assert.Equal(3,c.SalesPerSession);}
        [Theory] [InlineData("weed",200)] [InlineData("cocaine",500)] [InlineData("mdma",300)] [InlineData("lean",400)] [InlineData("crack",375)]
        public void PurchaseDebitsExactBankAmountAndPersistsStock(string product,int cost)
        {var w=new WalletData{Bank=2000,Cash=500};Assert.True(DealerTransactions.Buy(w,Options(),product,5,"b",()=>{},out bool changed));Assert.True(changed);Assert.Equal(2000-cost,w.Bank);Assert.Equal(500,w.Cash);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.Equal(5,w.Dealer.Stock[product]);}
        [Fact] public void FailedPurchaseSaveRestoresNullStockAndBank()
        {var w=new WalletData{Bank=2000};string old=JsonConvert.SerializeObject(w);Assert.Throws<Exception>(()=>DealerTransactions.Buy(w,Options(),"weed",5,"b",()=>throw new Exception(),out _));Assert.Null(w.Dealer);Assert.Equal(old,JsonConvert.SerializeObject(w));}
        [Fact] public void PurchaseReplayDoesNotRequireFundsAgain()
        {var w=new WalletData{Bank=200};Assert.True(DealerTransactions.Buy(w,Options(),"weed",5,"b",()=>{},out _));Assert.True(DealerTransactions.Buy(w,Options(),"weed",5,"b",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(0,w.Bank);Assert.Equal(5,w.Dealer.Stock["weed"]);}
        [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(21)]
        public void InvalidPurchaseCountDoesNotMutate(int count)
        {var w=Stock();string old=JsonConvert.SerializeObject(w);Assert.False(DealerTransactions.Buy(w,Options(),"weed",count,"b",()=>throw new Exception(),out _));Assert.Equal(old,JsonConvert.SerializeObject(w));}
        [Fact] public void MissingFundsUnknownProductAndCapacityRejectWholePurchase()
        {var w=Stock(0);Assert.False(DealerTransactions.Buy(w,Options(),"weed",5,"b",()=>{},out _));Assert.False(DealerTransactions.Buy(w,Options(),"other",1,"b",()=>{},out _));w=Stock();w.Dealer.Stock["custom_mod"]=10;string old=JsonConvert.SerializeObject(w);Assert.False(DealerTransactions.Buy(w,Options(),"weed",5,"b",()=>{},out _));Assert.Equal(old,JsonConvert.SerializeObject(w));}
        [Fact] public void SaleRemovesOneCreditsBankAndReputationInSameCheckpoint()
        {var w=Stock();int calls=0;Assert.True(DealerTransactions.Sale(w,Options(),"weed",1,"DAVIS",false,70,false,"s",()=>{calls++;Assert.Equal(4,w.Dealer.Stock["weed"]);Assert.Equal(2070,w.Bank);Assert.Equal(5,w.Dealer.ZoneRep["DAVIS"]);},out _));Assert.Equal(1,calls);Assert.Equal(1,w.Dealer.Sales);Assert.Equal(1,w.Dealer.Units);Assert.Equal(70,w.Dealer.Earned);Assert.Equal(0,w.Dealer.Sessions);}
        [Fact] public void SaleFailureRestoresStockBankAndReputationReferences()
        {var w=Stock();var old=w.Dealer;string json=JsonConvert.SerializeObject(w);Assert.Throws<Exception>(()=>DealerTransactions.Sale(w,Options(),"weed",1,"DAVIS",false,70,true,"s",()=>throw new Exception(),out _));Assert.Same(old,w.Dealer);Assert.Equal(json,JsonConvert.SerializeObject(w));Assert.True(Sale(w,"s",true));Assert.Equal(1,w.Dealer.Sessions);}
        [Fact] public void SaleReplayAfterReputationPriceChangeIsAcknowledgedWithoutDoublePay()
        {var w=Stock();w.Dealer.ZoneRep["DAVIS"]=5;Assert.True(DealerTransactions.Sale(w,Options(),"cocaine",1,"DAVIS",false,160,false,"s",()=>{},out _));Assert.Equal(10,w.Dealer.ZoneRep["DAVIS"]);Assert.Equal(161,Price(w,"cocaine"));w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(DealerTransactions.Sale(w,Options(),"cocaine",1,"DAVIS",false,160,false,"s",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(2160,w.Bank);Assert.Equal(4,w.Dealer.Stock["cocaine"]);}
        [Fact] public void ForgedQuoteCannotMintMoneyOrTakeStock()
        {var w=Stock();Assert.False(DealerTransactions.Sale(w,Options(),"weed",1,"DAVIS",false,999,false,"s",()=>throw new Exception(),out _));Assert.Equal(5,w.Dealer.Stock["weed"]);Assert.Equal(2000,w.Bank);}
        [Fact] public void MissingStockCannotPay()
        {var w=Stock();w.Dealer.Stock.Remove("weed");Assert.False(Sale(w));Assert.Equal(2000,w.Bank);Assert.Equal(0,w.Dealer.Sales);}
        [Fact] public void LastSaleCountsSessionOnce()
        {var w=Stock();Sale(w,"a");Sale(w,"b");Sale(w,"c",true);Assert.Equal(3,w.Dealer.Sales);Assert.Equal(1,w.Dealer.Sessions);Assert.True(Sale(w,"c",true));Assert.Equal(1,w.Dealer.Sessions);}
        [Theory] [InlineData(DealerOutcome.Refused,1,0)] [InlineData(DealerOutcome.Police,0,1)]
        public void RefusalAndDenunciationDoNotConsumeStockOrPay(DealerOutcome outcome,int refused,int incidents)
        {var w=Stock();w.Dealer.ZoneRep["DAVIS"]=10;string stock=JsonConvert.SerializeObject(w.Dealer.Stock);Assert.True(DealerTransactions.Encounter(w,outcome,"DAVIS","e",()=>{},out _));Assert.Equal(stock,JsonConvert.SerializeObject(w.Dealer.Stock));Assert.Equal(2000,w.Bank);Assert.Equal(refused,w.Dealer.Refused);Assert.Equal(incidents,w.Dealer.Incidents);Assert.Equal(outcome==DealerOutcome.Police?7:9,w.Dealer.ZoneRep["DAVIS"]);}
        [Fact] public void EncounterFailureDoesNotApplyReputationPenalty()
        {var w=Stock();string old=JsonConvert.SerializeObject(w);Assert.Throws<Exception>(()=>DealerTransactions.Encounter(w,DealerOutcome.Police,"DAVIS","e",()=>throw new Exception(),out _));Assert.Equal(old,JsonConvert.SerializeObject(w));}
        [Fact] public void EncounterReplayCannotRepeatPenalty()
        {var w=Stock();w.Dealer.ZoneRep["DAVIS"]=10;DealerTransactions.Encounter(w,DealerOutcome.Police,"DAVIS","e",()=>{},out _);Assert.True(DealerTransactions.Encounter(w,DealerOutcome.Police,"DAVIS","e",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal(1,w.Dealer.Incidents);Assert.Equal(7,w.Dealer.ZoneRep["DAVIS"]);}
        [Fact] public void ReputationClampsAtZeroAndOneHundred()
        {var w=Stock();w.Dealer.ZoneRep["DAVIS"]=99;Sale(w);Assert.Equal(100,w.Dealer.ZoneRep["DAVIS"]);w.Dealer.ZoneRep["DAVIS"]=0;DealerTransactions.Encounter(w,DealerOutcome.Police,"DAVIS","e",()=>{},out _);Assert.Equal(0,w.Dealer.ZoneRep["DAVIS"]);}
        [Theory] [InlineData(false,0,70)] [InlineData(false,100,77)] [InlineData(true,0,84)] [InlineData(true,100,92)]
        public void ReputationAndPremiumQuotesAreBounded(bool premium,int rep,int expected)=>Assert.Equal(expected,DealerRules.UnitPrice(Options().Products[0],rep,premium));
        [Theory] [InlineData(false,7,99,DealerOutcome.Police)] [InlineData(false,8,9,DealerOutcome.Refused)] [InlineData(false,8,10,DealerOutcome.Sale)] [InlineData(true,11,99,DealerOutcome.Police)] [InlineData(true,12,34,DealerOutcome.Refused)] [InlineData(true,12,35,DealerOutcome.Sale)]
        public void EncounterRollBoundariesAreStable(bool premium,int police,int refuse,DealerOutcome outcome)=>Assert.Equal(outcome,DealerRules.Outcome(Options(),0,premium,police,refuse));
        [Fact] public void ExplicitZeroRiskConfigCanBeUsedForRepeatableGameTesting()
        {var c=Options();c.PolicePercent=c.RefusePercent=c.PremiumRefusePercent=0;Assert.True(c.Valid);Assert.Equal(DealerOutcome.Sale,DealerRules.Outcome(c,0,true,0,0));}
        [Fact] public void BankOverflowAndCounterOverflowCannotRemoveStock()
        {var w=Stock(int.MaxValue);Assert.False(Sale(w));Assert.Equal(5,w.Dealer.Stock["weed"]);w.Bank=100;w.Dealer.Sales=int.MaxValue;Assert.False(Sale(w));Assert.Equal(100,w.Bank);}
        [Fact] public void UnknownFieldsAndOtherActivitiesSurviveBuyAndSale()
        {
            var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":2000,\"Future\":true,\"Dealer\":{\"Stock\":{\"weed\":5,\"mod_product\":1},\"FutureDealer\":42},\"Hunting\":{\"Licensed\":true,\"Bait\":3},\"Fishing\":{\"RodOwned\":true,\"Bag\":{\"sardine\":2}},\"Crafting\":{\"Stock\":{\"leather\":1}},\"Jobs\":{\"Jobs\":{\"pilot_training\":{\"CompletedShifts\":1}}}}");
            string hunt=JsonConvert.SerializeObject(w.Hunting),fish=JsonConvert.SerializeObject(w.Fishing),craft=JsonConvert.SerializeObject(w.Crafting),jobs=JsonConvert.SerializeObject(w.Jobs);Assert.True(Sale(w));w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.Equal(1,w.Dealer.Stock["mod_product"]);Assert.Equal(42,(int)w.Dealer.Extra["FutureDealer"]);Assert.True((bool)w.Extra["Future"]);Assert.Equal(hunt,JsonConvert.SerializeObject(w.Hunting));Assert.Equal(fish,JsonConvert.SerializeObject(w.Fishing));Assert.Equal(craft,JsonConvert.SerializeObject(w.Crafting));Assert.Equal(jobs,JsonConvert.SerializeObject(w.Jobs));Assert.True(FlightRules.Qualified(w.Jobs));
        }
        [Fact] public void InvalidSavedStockIsPreservedRatherThanReset()
        {var w=Stock();w.Dealer.Stock["weed"]=-1;string old=JsonConvert.SerializeObject(w);Assert.False(Sale(w));Assert.False(DealerTransactions.Buy(w,Options(),"weed",5,"b",()=>{},out _));Assert.Equal(old,JsonConvert.SerializeObject(w));}
        [Fact] public void InvalidConfigurationAndUnknownIdCannotBePurchased()
        {var w=Stock();var c=Options();c.Products[0].Weight=float.NaN;Assert.False(c.Valid);Assert.False(DealerTransactions.Buy(w,c,"weed",5,"b",()=>throw new Exception(),out _));c=Options();c.Products[1].Id=c.Products[0].Id;Assert.False(c.Valid);}
        [Fact] public void TimerRequiresOfferAcceptanceProximityAndFullHandover()
        {var r=Run();Assert.False(r.Commit(10000,true,_=>throw new Exception()));Assert.True(r.Offer("weed","DAVIS",0,0,99,99));Assert.True(r.Choose(Options(),false,100,_=>throw new Exception()));Assert.False(r.Commit(3099,true,_=>throw new Exception()));Assert.False(r.Commit(3100,false,_=>throw new Exception()));Assert.True(r.Commit(3100,true,last=>!last));Assert.Equal(1,r.Sold);}
        [Fact] public void OfferRollIsNotRerolledOnRetryAfterFailedCheckpoint()
        {var r=Run();r.Offer("weed","DAVIS",0,0,0,99);Assert.False(r.Choose(Options(),false,100,o=>{Assert.Equal(DealerOutcome.Police,o);return false;}));Assert.Equal(DealerStage.Offer,r.Stage);Assert.True(r.Choose(Options(),true,200,o=>o==DealerOutcome.Police));Assert.Equal(DealerStage.Cancelled,r.Stage);}
        [Fact] public void RefusalReleasesNextBuyerWithoutIncrementingSales()
        {var r=Run();r.Offer("weed","DAVIS",0,0,99,0);Assert.True(r.Choose(Options(),false,100,o=>o==DealerOutcome.Refused));Assert.Equal(DealerStage.Waiting,r.Stage);Assert.Equal(0,r.Sold);Assert.True(r.Offer("weed","DAVIS",0,1000,99,99));}
        [Fact] public void ExpiredOfferCannotBeAcceptedOrRevived()
        {var r=Run();r.Offer("weed","DAVIS",0,0,99,99);Assert.True(r.Expired(120000));Assert.False(r.Choose(Options(),false,120000,_=>throw new Exception()));Assert.False(r.Commit(123000,true,_=>throw new Exception()));}
        [Fact] public void NegotiationDoesNotExtendOriginalBuyerDeadline()
        {var r=Run();r.Offer("weed","DAVIS",0,0,99,99);r.Choose(Options(),false,119000,_=>true);Assert.False(r.Commit(122000,true,_=>throw new Exception()));}
        [Fact] public void FailedOrThrowingSaleRetainsSameReceiptForRetry()
        {var r=Run();r.Offer("weed","DAVIS",0,0,99,99);r.Choose(Options(),false,0,_=>true);string id=r.Receipt;Assert.False(r.Commit(3000,true,_=>false));Assert.Throws<Exception>(()=>r.Commit(4000,true,_=>throw new Exception()));Assert.Equal(id,r.Receipt);Assert.Equal(DealerStage.Handover,r.Stage);Assert.True(r.Commit(9000,true,_=>true));}
        [Fact] public void ThreeSalesCompleteOneSessionWithNoExtraFinishPayment()
        {var r=Run();var w=Stock();for(int i=0;i<3;i++){int now=i*10000;int rep=DealerRules.Reputation(w.Dealer,"DAVIS");r.Offer("weed","DAVIS",rep,now,99,99);r.Choose(Options(),false,now,_=>true);Assert.True(r.Commit(now+3000,true,last=>Sale(w,r.Receipt,last)));}Assert.Equal(DealerStage.Finished,r.Stage);Assert.Equal(3,w.Dealer.Sales);Assert.Equal(1,w.Dealer.Sessions);Assert.Equal(2210,w.Bank);Assert.False(r.Commit(40000,true,_=>throw new Exception()));}
        [Fact] public void SkipOrCancelCannotAwardStockOrMoney()
        {var r=Run();r.Offer("weed","DAVIS",0,0,99,99);r.Choose(Options(),false,0,_=>true);r.Skip();Assert.Equal(DealerStage.Waiting,r.Stage);Assert.False(r.Commit(3000,true,_=>throw new Exception()));r.Cancel();Assert.False(r.Offer("weed","DAVIS",0,5000,99,99));Assert.Equal(0,r.Sold);}
        [Fact] public void OffersAreBoundedToPreventAnEndlessSession()
        {var r=Run();for(int i=0;i<12;i++){Assert.True(r.Offer("weed","DAVIS",0,i*1000,99,99));r.Skip();}Assert.False(r.Offer("weed","DAVIS",0,13000,99,99));}
        [Fact] public void HandoverClockHandlesOverflow()
        {int start=int.MaxValue-1000;var r=Run();r.Offer("weed","DAVIS",0,start,99,99);r.Choose(Options(),false,start,_=>true);Assert.True(r.Commit(unchecked(start+3000),true,_=>true));}
        [Fact] public void TwelveActivitiesShareExclusiveGate()
        {var g=new ShiftGate();foreach(var n in new[]{"pizza","postop","uber","valet","supply","tanker","prison","pilot","hunting","fishing","crafting","dealer"}){Assert.True(g.Acquire(n));Assert.False(g.Acquire("other"));g.Release(n);}}
    }
}
