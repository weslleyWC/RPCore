using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangDiplomacyTests
    {
        static readonly Action Save=()=>{};
        static readonly DateTime Now=new DateTime(2013,9,17,12,0,0);
        static GangState S(string gang="Mine")=>new GangState{Gang=gang,Reputation=800,Rank=3};
        static WalletData W()=>new WalletData{Bank=10000,Cash=123,Dealer=new DealerData{Stock={{"weed",5}}}};
        static bool Deal(WalletData w,string initial="Neutral",string id="deal",DateTime? now=null,Action save=null,string gang="Mine",string rival="Other",GangDiplomacyOptions c=null)=>GangDiplomacyTransactions.Negotiate(w,S(gang),rival,initial,c??new GangDiplomacyOptions(),now??Now,id,save??Save,out _);
        static GangDiplomacyAccount A(WalletData w)=>GangDiplomacyRules.Account(w.GangDiplomacy,"Mine","Other");
        static WalletData RaidReady()
        {
            var area=GangTurfOptions.Defaults().Areas[0];var state=S(area.DefaultOwner);var w=W();
            var p=new GangRaidAttempt{Id="raid",Gang=state.Gang,Rival="Other",Area=area,Options=new GangRaidOptions(),Site=new GangRaidOptions().Sites[0],Capacity=30,Models=Enumerable.Repeat("g_m_y_famca_01",3).ToArray(),Loot={{"weed",40},{"mdma",50}}};
            Assert.True(GangRaidTransactions.Begin(w,state,p,DealerDefaults.Load(),Save,out _));for(int i=0;i<3;i++)Assert.True(GangRaidTransactions.Defeat(w,"raid",i,Save,out _));return w;
        }
        [Theory]
        [InlineData("Respect","Neutral","Respect")][InlineData("Neutral","Hate","Respect")][InlineData("Hate","War","Neutral")][InlineData("War","War","Hate")]
        public void FourReferenceTiersHaveBoundedTransitions(string state,string worse,string better)
        {Assert.Equal(worse,GangDiplomacyRules.Worsen(state));Assert.Equal(better,GangDiplomacyRules.Improve(state));Assert.True(GangDiplomacyRules.Known(state));}
        [Fact] public void DefaultsAreBoundedAndBadConfigIsRejected()
        {var c=new GangDiplomacyOptions();Assert.True(c.Valid);Assert.Equal(1000,c.AgreementPrice);Assert.Equal(24,c.CooldownHours);c.AgreementPrice=0;Assert.False(c.Valid);c.AgreementPrice=1000001;Assert.False(c.Valid);c.AgreementPrice=1;c.CooldownHours=0;Assert.False(c.Valid);c.CooldownHours=169;Assert.False(c.Valid);}
        [Fact] public void InitialAttitudeReadsRivalTowardOwnWithoutEditingDefinition()
        {var g=new GangDefinition{Name="Other",Relationships=new System.Collections.Generic.Dictionary<string,string>{{"mInE","Hate"}}};Assert.Equal("Hate",GangDiplomacyRules.Initial(g,"Mine"));Assert.Equal("Neutral",GangDiplomacyRules.Initial(g,"Missing"));Assert.Equal("Hate",g.Relationships["mInE"]);}
        [Fact] public void AbsentUnknownOrAmbiguousInitialAttitudeIsNeutralWithoutDiscardingData()
        {Assert.Equal("Neutral",GangDiplomacyRules.Initial(null,"Mine"));var g=JObject.Parse("{Name:'Other',Relationships:{Mine:'Future',MINE:'War'},Future:'keep'}").ToObject<GangDefinition>();Assert.Equal("Neutral",GangDiplomacyRules.Initial(g,"Mine"));Assert.Equal("Future",g.Relationships["Mine"]);Assert.Equal("keep",(string)JObject.FromObject(g)["Future"]);}
        [Fact] public void AgreementChargesOnlyBankAndCreatesDirectedHistory()
        {var w=W();var d=w.Dealer;Assert.True(Deal(w,"War"));Assert.Equal(9000,w.Bank);Assert.Equal(123,w.Cash);Assert.Same(d,w.Dealer);Assert.Null(w.GangWarehouse);Assert.Null(w.GangTreasury);Assert.Null(w.GangIllicit);Assert.Equal("Hate",A(w).Attitude);Assert.Equal(1,A(w).Agreements);Assert.Equal(1000,A(w).Paid);Assert.Equal(Now.Ticks,A(w).LastAgreementTicks);Assert.Null(GangDiplomacyRules.Account(w.GangDiplomacy,"Other","Mine"));}
        [Fact] public void ReplayAfterReloadHasNoSecondDebitOrSave()
        {var w=W();Deal(w,"War");w=CharacterRules.Copy(w);Assert.True(Deal(w,"War",save:()=>throw new Exception()));Assert.Equal(9000,w.Bank);Assert.Equal(1,A(w).Agreements);}
        [Fact] public void EachStepNeedsFullCooldownAndNoOfflineCompounding()
        {var w=W();Assert.True(Deal(w,"War"));Assert.False(Deal(w,id:"early",now:Now.AddHours(24).AddTicks(-1)));Assert.True(Deal(w,id:"next",now:Now.AddHours(24)));Assert.Equal("Neutral",A(w).Attitude);Assert.True(Deal(w,id:"late",now:Now.AddYears(1)));Assert.Equal("Respect",A(w).Attitude);Assert.Equal(3,A(w).Agreements);Assert.Equal(7000,w.Bank);Assert.False(Deal(w,id:"beyond",now:Now.AddYears(2)));}
        [Fact] public void RewindingClockDoesNotAdvanceAgreement()
        {var w=W();Deal(w,"War");Assert.False(Deal(w,id:"rewind",now:Now.AddDays(-1)));Assert.Equal(9000,w.Bank);}
        [Fact] public void CannotPayForRespectOrWithInsufficientFunds()
        {var w=W();Assert.False(Deal(w,"Respect"));Assert.Null(w.GangDiplomacy);w.Bank=999;Assert.False(Deal(w));Assert.Equal(999,w.Bank);Assert.Null(w.GangDiplomacy);}
        [Theory][InlineData(null)][InlineData("")][InlineData("None")][InlineData("Unassigned")][InlineData("Other")]
        public void InvalidOwnAffiliationOrSameGangIsRejected(string gang)
        {var w=W();Assert.False(Deal(w,gang:gang));Assert.Null(w.GangDiplomacy);Assert.Equal(10000,w.Bank);}
        [Fact] public void InvalidIdPriceAndInitialNeverMutateWallet()
        {var w=W();Assert.False(Deal(w,id:new string('a',81)));Assert.False(Deal(w,"Future"));Assert.False(Deal(w,c:new GangDiplomacyOptions{AgreementPrice=-1}));Assert.Null(w.GangDiplomacy);}
        [Fact] public void PairIdentityIsCaseInsensitiveAndCooldownShared()
        {var w=W();Deal(w,"War");Assert.False(Deal(w,id:"case",gang:"mINE",rival:"oTHER"));Assert.True(Deal(w,id:"case",gang:"mINE",rival:"oTHER",now:Now.AddHours(24)));Assert.Single(w.GangDiplomacy.Accounts);Assert.Equal("Neutral",A(w).Attitude);}
        [Fact] public void OtherPairAndProfileHaveIndependentHistory()
        {var w=W();Deal(w,"War");Assert.True(Deal(w,"War",id:"otherpair",rival:"Third"));var other=W();Assert.Null(other.GangDiplomacy);Assert.Equal(10000,other.Bank);Assert.Equal(2,w.GangDiplomacy.Accounts.Count);}
        [Fact] public void FailedSaveRestoresOriginalBookAndBankReferences()
        {var w=W();Deal(w,"War");var old=w.GangDiplomacy;Assert.Throws<Exception>(()=>Deal(w,id:"fail",now:Now.AddHours(24),save:()=>throw new Exception()));Assert.Same(old,w.GangDiplomacy);Assert.Equal("Hate",A(w).Attitude);Assert.Equal(9000,w.Bank);}
        [Fact] public void FailedFirstSaveRestoresAbsentLedger()
        {var w=W();Assert.Throws<Exception>(()=>Deal(w,save:()=>throw new Exception()));Assert.Null(w.GangDiplomacy);Assert.Equal(10000,w.Bank);}
        [Fact] public void UnknownWalletBookAndAccountFieldsSurviveAgreement()
        {var w=W();Deal(w,"War");var j=JObject.FromObject(w);j["Future"]="wallet";j["GangDiplomacy"]["Future"]="book";j["GangDiplomacy"]["Accounts"][0]["Future"]="pair";w=j.ToObject<WalletData>();Deal(w,id:"next",now:Now.AddHours(24));j=JObject.FromObject(w);Assert.Equal("wallet",(string)j["Future"]);Assert.Equal("book",(string)j["GangDiplomacy"]["Future"]);Assert.Equal("pair",(string)j["GangDiplomacy"]["Accounts"][0]["Future"]);}
        [Fact] public void LegacyWalletDoesNotCreateLedgerWhenRead()
        {var w=JObject.Parse("{Bank:123,Cash:42,Future:'keep'}").ToObject<WalletData>();Assert.Null(w.GangDiplomacy);Assert.Equal(123,w.Bank);Assert.True(new GangDiplomacyData().Valid);}
        [Fact] public void MalformedLedgerIsProtectedRatherThanReplaced()
        {var w=W();Deal(w,"War");A(w).Attitude="Future";var old=w.GangDiplomacy;Assert.False(Deal(w,id:"new",now:Now.AddHours(24)));Assert.Same(old,w.GangDiplomacy);Assert.Equal(9000,w.Bank);}
        [Fact] public void DuplicatePairOrNegativeHistoryIsInvalid()
        {var b=new GangDiplomacyData();b.Accounts.Add(new GangDiplomacyAccount{Gang="Mine",Rival="Other"});Assert.True(b.Valid);b.Accounts.Add(new GangDiplomacyAccount{Gang="mine",Rival="OTHER"});Assert.False(b.Valid);b.Accounts.RemoveAt(1);b.Accounts[0].StolenLoads=-1;Assert.False(b.Valid);}
        [Fact] public void PendingRaidOrFreightBlocksPaymentEvenWhenSuspended()
        {var w=RaidReady();Assert.False(Deal(w));w.GangRaid=null;w.GangBusiness=new GangBusinessData{Supply=new GangSupplyOrder()};Assert.False(Deal(w));Assert.Null(w.GangDiplomacy);}
        [Fact] public void ThreeDefeatsAloneDoNotChangeRelations()
        {var w=RaidReady();Assert.Null(w.GangDiplomacy);Assert.False(w.GangRaid.Pending.Package);}
        [Theory][InlineData("Respect","Neutral")][InlineData("Neutral","Hate")][InlineData("Hate","War")][InlineData("War","War")]
        public void CollectionAtomicallySavesCargoAndOneWorseAttitude(string initial,string expected)
        {var w=RaidReady();int saves=0;Assert.True(GangRaidTransactions.Collect(w,"raid",()=>{saves++;Assert.True(w.GangRaid.Pending.Package);Assert.Equal(expected,w.GangDiplomacy.Accounts[0].Attitude);},out bool changed,initial));Assert.True(changed);Assert.Equal(1,saves);Assert.Equal(1,w.GangDiplomacy.Accounts[0].StolenLoads);Assert.Null(w.GangWarehouse);Assert.Equal(10000,w.Bank);}
        [Fact] public void CollectReplayAfterReloadDoesNotWorsenTwice()
        {var w=RaidReady();GangRaidTransactions.Collect(w,"raid",Save,out _);w=CharacterRules.Copy(w);Assert.True(GangRaidTransactions.Collect(w,"raid",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Equal("Hate",w.GangDiplomacy.Accounts[0].Attitude);Assert.Equal(1,w.GangDiplomacy.Accounts[0].StolenLoads);}
        [Fact] public void FailedCollectionRestoresBothOriginalBooksAndNoPackage()
        {var w=RaidReady();var old=w.GangRaid;Assert.Throws<Exception>(()=>GangRaidTransactions.Collect(w,"raid",()=>throw new Exception(),out _));Assert.Same(old,w.GangRaid);Assert.False(w.GangRaid.Pending.Package);Assert.Null(w.GangDiplomacy);}
        [Theory][InlineData(false)][InlineData(true)]
        public void CancelOrFailAfterTheftDoesNotUndoOffense(bool failed)
        {var w=RaidReady();GangRaidTransactions.Collect(w,"raid",Save,out _);var old=w.GangDiplomacy;Assert.True(GangRaidTransactions.End(w,"raid",failed,Save,out _));Assert.Same(old,w.GangDiplomacy);Assert.Equal("Hate",old.Accounts[0].Attitude);Assert.Null(w.GangWarehouse);}
        [Fact] public void ExistingCollectedLegacyCargoIsNotPenalizedRetroactively()
        {var w=RaidReady();w.GangRaid.Pending.Package=true;w.GangRaid.Receipts.Add("raid:raid-package");Assert.True(GangRaidTransactions.Collect(w,"raid",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.Null(w.GangDiplomacy);}
        [Fact] public void ExistingAttitudeWinsOverChangedDefinitionAndRollbackPreservesIt()
        {var w=RaidReady();string gang=w.GangRaid.Pending.Gang;Assert.True(Deal(W(),"War"));w.GangDiplomacy=new GangDiplomacyData();w.GangDiplomacy.Accounts.Add(new GangDiplomacyAccount{Gang=gang,Rival="Other",Attitude="Hate"});var old=w.GangDiplomacy;Assert.Throws<Exception>(()=>GangRaidTransactions.Collect(w,"raid",()=>throw new Exception(),out _,"Respect"));Assert.Same(old,w.GangDiplomacy);Assert.True(GangRaidTransactions.Collect(w,"raid",Save,out _,"Respect"));Assert.Equal("War",w.GangDiplomacy.Accounts[0].Attitude);Assert.Equal("Hate",old.Accounts[0].Attitude);}
        [Fact] public void OverflowOrCorruptRelationsCannotCreateCargoWithoutConsequence()
        {var w=RaidReady();w.GangDiplomacy=new GangDiplomacyData();w.GangDiplomacy.Accounts.Add(new GangDiplomacyAccount{Gang=w.GangRaid.Pending.Gang,Rival="Other",StolenLoads=long.MaxValue});Assert.False(GangRaidTransactions.Collect(w,"raid",Save,out _));Assert.False(w.GangRaid.Pending.Package);w.GangDiplomacy.Accounts[0].Attitude="Future";Assert.False(GangRaidTransactions.Collect(w,"raid",Save,out _));}
        [Fact] public void CompletedDeliveryDoesNotWorsenRelationsAgain()
        {var w=RaidReady();var area=w.GangRaid.Pending.Area;GangRaidTransactions.Collect(w,"raid",Save,out _);var old=w.GangDiplomacy;Assert.True(GangRaidTransactions.Complete(w,S(area.DefaultOwner),area,DealerDefaults.Load(),"raid",Save,out _));Assert.Same(old,w.GangDiplomacy);Assert.Equal(1,old.Accounts[0].StolenLoads);}
        [Fact] public void ReceiptListStaysBoundedWhileHistoryPreventsOldCooldownReplay()
        {var w=W();Deal(w,"War");for(int i=0;i<256;i++)w.GangDiplomacy.Receipts.Add("old"+i);w.GangDiplomacy.Receipts.RemoveAt(0);Assert.True(w.GangDiplomacy.Valid);Assert.True(Deal(w,id:"next",now:Now.AddHours(24)));Assert.Equal(256,w.GangDiplomacy.Receipts.Count);Assert.False(Deal(w,id:"deal",now:Now.AddHours(24)));Assert.Equal(8000,w.Bank);}
        [Fact] public void FullAccountBookBlocksNewPairWithoutCharging()
        {var w=W();w.GangDiplomacy=new GangDiplomacyData();for(int i=0;i<1024;i++)w.GangDiplomacy.Accounts.Add(new GangDiplomacyAccount{Gang="Mine",Rival="Other"+i});Assert.True(w.GangDiplomacy.Valid);Assert.False(Deal(w));Assert.Equal(10000,w.Bank);}
        [Fact] public void InvalidActivityBooksAreProtectedBeforeNegotiation()
        {var w=W();w.GangRaid=new GangRaidData{Attempts=-1};Assert.False(Deal(w));w.GangRaid=null;w.GangBusiness=new GangBusinessData{Accounts=null};Assert.False(Deal(w));Assert.Equal(10000,w.Bank);Assert.Null(w.GangDiplomacy);}
        [Fact] public void PaymentHistoryOverflowDoesNotChargeOrChangeAttitude()
        {var w=W();Deal(w,"War");A(w).Paid=long.MaxValue;Assert.False(Deal(w,id:"next",now:Now.AddHours(24)));Assert.Equal(9000,w.Bank);Assert.Equal("Hate",A(w).Attitude);}
        [Fact] public void AgreementsCounterOverflowDoesNotCharge()
        {var w=W();Deal(w,"War");var a=A(w);a.Agreements=int.MaxValue;a.Paid=int.MaxValue;Assert.True(w.GangDiplomacy.Valid);Assert.False(Deal(w,id:"next",now:Now.AddHours(24)));Assert.Equal(9000,w.Bank);}
    }
}
