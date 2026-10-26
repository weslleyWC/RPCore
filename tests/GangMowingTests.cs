using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangMowingTests
    {
        static readonly Action Save=()=>{};static readonly DateTime Utc=new DateTime(2026,10,26,12,0,0,DateTimeKind.Utc);
        static GangTurfDefinition A()=>GangTurfOptions.Defaults().Areas.Single(a=>a.Id==8);
        static GangState S()=>new GangState{Gang=A().DefaultOwner};static WalletData W()=>new WalletData{Bank=10000,Cash=123};
        static bool Begin(WalletData w,string id="mow",GangMowingOptions c=null,Action save=null)=>GangMowingTransactions.Begin(w,S(),A(),c??new GangMowingOptions(),id,save??Save,out _);
        static bool Cut(WalletData w,int index=0,string id="mow",Action save=null)=>GangMowingTransactions.Cut(w,S(),A(),id,index,true,true,2,w.GangMowing.Pending.Options.Patches[index],save??Save,out _);
        static bool Complete(WalletData w,string id="mow",DateTime? utc=null,Action save=null)=>GangMowingTransactions.Complete(w,S(),A(),id,utc??Utc,save??Save,out _);
        static bool Cancel(WalletData w,string id="mow",Action save=null)=>GangMowingTransactions.Cancel(w,id,save??Save,out _);
        static WalletData Started(){var w=W();Assert.True(Begin(w));return w;}
        static WalletData Ready(){var w=Started();for(int i=0;i<57;i++)Assert.True(Cut(w,i));return w;}
        static GangMowingAccount Reward(WalletData w)=>GangMowingTransactions.Account(w.GangMowing,S().Gang);
        [Fact] public void DefaultsMatchSourceMowerGrassAndFiftySevenLocations()
        {var c=new GangMowingOptions();Assert.True(c.Valid);Assert.Equal(57,c.Patches.Count);Assert.Equal("mower",c.VehicleModel);Assert.Equal(992644101,c.GrassModel);Assert.Equal(1,c.MowRadius);Assert.Equal(60,c.BoostMinutes);Assert.Equal(1140.882f,c.Spawn.X);Assert.Equal(-671.9385f,c.Spawn.Y);Assert.Equal(102.5347f,c.Spawn.Heading);Assert.Equal(1157.133f,c.Patches[0].X);Assert.Equal(-97.8884f,c.Patches[56].Heading);}
        [Fact] public void BeginDoesNotCreateCashStockIncomeOrAutomaticOptIn()
        {var w=W();w.Bank=0;Assert.True(Begin(w));Assert.Equal(0,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangIllicit);Assert.Null(w.GangTreasury);Assert.Null(w.GangBusiness);Assert.Null(w.GangAutoLaunder);Assert.Empty(w.GangMowing.Accounts);}
        [Fact] public void RequiresOwnedTerritoryEight()
        {var w=W();var a=GangTurfOptions.Defaults().Areas[0];Assert.False(GangMowingTransactions.Begin(w,new GangState{Gang=a.DefaultOwner},a,new GangMowingOptions(),"bad",Save,out _));Assert.False(GangMowingTransactions.Begin(w,new GangState{Gang="Other"},A(),new GangMowingOptions(),"bad",Save,out _));Assert.Null(w.GangMowing);}
        [Fact] public void BeginSnapshotsConfigurationAndAllCoordinates()
        {var w=W();var c=new GangMowingOptions();Begin(w,c:c);c.Spawn.X=0;c.Patches[0].Y=0;c.Patches.RemoveAt(56);c.BoostMinutes=1;Assert.Equal(1140.882f,w.GangMowing.Pending.Options.Spawn.X);Assert.Equal(-720.8135f,w.GangMowing.Pending.Options.Patches[0].Y);Assert.Equal(57,w.GangMowing.Pending.Options.Patches.Count);Assert.Equal(60,w.GangMowing.Pending.Options.BoostMinutes);}
        [Fact] public void CutConfirmsOnlyOneDistinctPatchAndNoPartialReward()
        {var w=Started();Assert.True(Cut(w));Assert.Equal(new[]{0},w.GangMowing.Pending.Mowed);Assert.False(Complete(w));Assert.Empty(w.GangMowing.Accounts);Assert.Equal(10000,w.Bank);Assert.Null(w.GangIllicit);}
        [Fact] public void CutReplayDoesNotSaveOrAddCountAgain()
        {var w=Started();Cut(w);int saves=0;Assert.True(GangMowingTransactions.Cut(w,S(),A(),"mow",0,true,true,0,w.GangMowing.Pending.Options.Patches[0],()=>saves++,out bool changed));Assert.False(changed);Assert.Equal(0,saves);Assert.Single(w.GangMowing.Pending.Mowed);}
        [Fact] public void EveryDistinctPatchRequiredBeforeFinalAward()
        {var w=Started();for(int i=0;i<56;i++)Cut(w,i);Assert.False(Complete(w));Assert.False(w.GangMowing.Pending.Ready);Cut(w,56);Assert.True(w.GangMowing.Pending.Ready);Assert.Empty(w.GangMowing.Accounts);}
        [Fact] public void AwardAndPendingRemovalAreAtomicWithoutDirectMoney()
        {var w=Ready();int saves=0;Assert.True(Complete(w,save:()=>{saves++;Assert.Null(w.GangMowing.Pending);Assert.Equal(1,Reward(w).Completed);}));Assert.Equal(1,saves);Assert.Equal(Utc.Ticks,Reward(w).StartedUtcTicks);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);Assert.Equal(10000,w.Bank);Assert.Equal(123,w.Cash);Assert.Null(w.GangTreasury);Assert.Null(w.GangIllicit);Assert.Null(w.GangAutoLaunder);}
        [Fact] public void CompletionReplayNeverExtendsOrPaysAgain()
        {var w=Ready();Complete(w);var old=w.GangMowing;int saves=0;Assert.True(Complete(w,utc:Utc.AddMinutes(5),save:()=>saves++));Assert.Same(old,w.GangMowing);Assert.Equal(0,saves);Assert.Equal(1,Reward(w).Completed);Assert.Equal(Utc.AddHours(1).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void ReloadPreservesNonSequentialDistinctCutsAndSnapshot()
        {var w=Started();Cut(w,56);Cut(w,4);Cut(w,20);w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.True(w.GangMowing.Valid);Assert.Equal(new[]{56,4,20},w.GangMowing.Pending.Mowed);Assert.Equal(57,w.GangMowing.Pending.Options.Patches.Count);Assert.False(Complete(w));Assert.True(Cut(w,0));}
        [Fact] public void ReadyProofSurvivesReloadAndCanFinalizeWithoutNativeHandles()
        {var w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(Ready()));Assert.True(w.GangMowing.Pending.Ready);Assert.True(Complete(w));Assert.Null(w.GangMowing.Pending);}
        [Fact] public void FailedBeginRestoresMissingBook()
        {var w=W();Assert.Throws<InvalidOperationException>(()=>Begin(w,save:()=>throw new InvalidOperationException()));Assert.Null(w.GangMowing);Assert.Equal(10000,w.Bank);}
        [Fact] public void FailedCutRestoresOriginalProgressAndReceipt()
        {var w=Started();var old=w.GangMowing;Assert.Throws<InvalidOperationException>(()=>Cut(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangMowing);Assert.Empty(w.GangMowing.Pending.Mowed);Assert.DoesNotContain("mow:mowing-cut:0",w.GangMowing.Receipts);Assert.True(Cut(w));}
        [Fact] public void FailedFinalAwardPreservesReadyAndPreviousBonus()
        {var w=Ready();var old=w.GangMowing;Assert.Throws<InvalidOperationException>(()=>Complete(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangMowing);Assert.True(w.GangMowing.Pending.Ready);Assert.Empty(w.GangMowing.Accounts);Assert.True(Complete(w));}
        [Fact] public void CancelPartialIsFreeWithoutOwnershipAndDoesNotAward()
        {var w=Started();Cut(w);Assert.True(Cancel(w));Assert.Null(w.GangMowing.Pending);Assert.Empty(w.GangMowing.Accounts);Assert.Equal(10000,w.Bank);Assert.Null(w.GangIllicit);Assert.True(Cancel(w));}
        [Fact] public void FailedCancelPreservesExactPending()
        {var w=Started();Cut(w);var old=w.GangMowing;Assert.Throws<InvalidOperationException>(()=>Cancel(w,save:()=>throw new InvalidOperationException()));Assert.Same(old,w.GangMowing);Assert.Single(w.GangMowing.Pending.Mowed);}
        [Fact] public void CancelAnotherContractLeavesPriorBonusUnchanged()
        {var w=Ready();Complete(w);long expiry=Reward(w).ExpiresUtcTicks;Begin(w,"again");Cut(w,id:"again");Cancel(w,"again");Assert.Equal(expiry,Reward(w).ExpiresUtcTicks);Assert.Equal(1,Reward(w).Completed);}
        [Fact] public void RealNewCompletionRefreshesWindowWithoutStacking()
        {var w=Ready();Complete(w);Begin(w,"again");for(int i=0;i<57;i++)Cut(w,i,"again");Assert.True(Complete(w,"again",Utc.AddMinutes(10)));Assert.Equal(2,Reward(w).Completed);Assert.Equal(Utc.AddMinutes(70).Ticks,Reward(w).ExpiresUtcTicks);}
        [Fact] public void BonusUsesStrictUtcGangAreaAndWindow()
        {var w=Ready();Complete(w);var b=w.GangMowing;Assert.Equal(2,GangMowingTransactions.Multiplier(b,S().Gang.ToUpperInvariant(),8,Utc));Assert.Equal(2,GangMowingTransactions.Multiplier(b,S().Gang,8,Utc.AddMinutes(59)));Assert.Equal(1,GangMowingTransactions.Multiplier(b,S().Gang,8,Utc.AddHours(1)));Assert.Equal(1,GangMowingTransactions.Multiplier(b,S().Gang,8,Utc.AddTicks(-1)));Assert.Equal(1,GangMowingTransactions.Multiplier(b,"Other",8,Utc));Assert.Equal(1,GangMowingTransactions.Multiplier(b,S().Gang,7,Utc));Assert.Equal(1,GangMowingTransactions.Multiplier(b,S().Gang,8,DateTime.SpecifyKind(Utc,DateTimeKind.Local)));}
        [Fact] public void LossOfOwnershipPreventsCutAndCompletionButAllowsCancel()
        {var w=Started();var s=new GangState{Gang="Other"};Assert.False(GangMowingTransactions.Cut(w,s,A(),"mow",0,true,true,0,w.GangMowing.Pending.Options.Patches[0],Save,out _));Assert.False(GangMowingTransactions.Complete(w,s,A(),"mow",Utc,Save,out _));Assert.True(Cancel(w));}
        [Fact] public void LegacyQueriesDoNotCreateBookOrBonus()
        {var w=JsonConvert.DeserializeObject<WalletData>("{\"Bank\":100,\"Future\":42}");Assert.Null(w.GangMowing);Assert.False(GangMowingTransactions.Blocked(w));Assert.Null(GangMowingTransactions.Account(w.GangMowing,S().Gang));Assert.Equal(1,GangMowingTransactions.Multiplier(w.GangMowing,S().Gang,8,Utc));Assert.Null(w.GangMowing);}
        [Fact] public void UnknownBookPendingOptionsAndPointFieldsSurviveCut()
        {var w=Started();var j=JObject.FromObject(w);j["Future"]=42;j["GangMowing"]["FutureBook"]="keep";j["GangMowing"]["Pending"]["FuturePending"]="keep";j["GangMowing"]["Pending"]["Options"]["FutureOptions"]=true;j["GangMowing"]["Pending"]["Options"]["Patches"][0]["FuturePoint"]=123;w=j.ToObject<WalletData>();Cut(w);j=JObject.FromObject(w);Assert.Equal(42,(int)j["Future"]);Assert.Equal("keep",(string)j["GangMowing"]["FutureBook"]);Assert.Equal("keep",(string)j["GangMowing"]["Pending"]["FuturePending"]);Assert.True((bool)j["GangMowing"]["Pending"]["Options"]["FutureOptions"]);Assert.Equal(123,(int)j["GangMowing"]["Pending"]["Options"]["Patches"][0]["FuturePoint"]);}
        [Fact] public void InvalidDuplicateOutOfRangeOrMissingCutProofIsProtected()
        {var w=Started();w.GangMowing.Pending.Mowed.AddRange(new[]{0,0});Assert.False(w.GangMowing.Valid);Assert.False(Cut(w,1));Assert.False(Cancel(w));w=Started();w.GangMowing.Pending.Mowed.Add(57);Assert.False(w.GangMowing.Valid);w.GangMowing.Pending.Mowed=null;Assert.False(w.GangMowing.Valid);}
        [Fact] public void InvalidConfigurationNeverStartsOrOverwrites()
        {var w=W();var c=new GangMowingOptions();c.Patches.Add(c.Patches[0]);Assert.False(c.Valid);Assert.False(Begin(w,c:c));c=new GangMowingOptions{MaxSpeed=float.NaN};Assert.False(c.Valid);c=new GangMowingOptions{MowRadius=0};Assert.False(c.Valid);c=new GangMowingOptions();c.Patches[0].X+=1000;Assert.False(c.Valid);Assert.Null(w.GangMowing);}
        [Fact] public void MissingUnsafeOrWrongDriverObservationCannotCut()
        {var w=Started();var point=w.GangMowing.Pending.Options.Patches[0];Assert.False(GangMowingTransactions.Cut(w,S(),A(),"mow",0,false,true,1,point,Save,out _));Assert.False(GangMowingTransactions.Cut(w,S(),A(),"mow",0,true,false,1,point,Save,out _));Assert.Empty(w.GangMowing.Pending.Mowed);}
        [Fact] public void CuttingRequiresHorizontalRadiusAndHeightAtSamePatch()
        {var c=new GangMowingOptions();var p=c.Patches[0];var q=new JobPoint{Name="test",X=p.X+1,Y=p.Y,Z=p.Z};Assert.True(GangMowingRules.CanCut(true,true,0,q,p,c));q.X=p.X+1.01f;Assert.False(GangMowingRules.CanCut(true,true,0,q,p,c));q.X=p.X;q.Z=p.Z+3;Assert.True(GangMowingRules.CanCut(true,true,0,q,p,c));q.Z+=.01f;Assert.False(GangMowingRules.CanCut(true,true,0,q,p,c));}
        [Fact] public void FastOrInvalidSpeedAndInvalidCoordinatesNeverCut()
        {var c=new GangMowingOptions();var p=c.Patches[0];foreach(float speed in new[]{-1f,8.01f,float.NaN,float.PositiveInfinity})Assert.False(GangMowingRules.CanCut(true,true,speed,p,p,c));Assert.True(GangMowingRules.CanCut(true,true,8,p,p,c));Assert.False(GangMowingRules.CanCut(true,true,0,null,p,c));Assert.False(GangMowingRules.CanCut(true,true,0,new JobPoint{X=float.NaN},p,c));}
        [Fact] public void InvalidIndexWrongIdOrFarPositionCannotCreateProof()
        {var w=Started();Assert.False(GangMowingTransactions.Cut(w,S(),A(),"mow",57,true,true,0,w.GangMowing.Pending.Options.Spawn,Save,out _));Assert.False(GangMowingTransactions.Cut(w,S(),A(),"mow",-1,true,true,0,w.GangMowing.Pending.Options.Spawn,Save,out _));Assert.False(Cut(w,id:"other"));Assert.False(GangMowingTransactions.Cut(w,S(),A(),"mow",0,true,true,0,w.GangMowing.Pending.Options.Spawn,Save,out _));Assert.Empty(w.GangMowing.Pending.Mowed);}
        [Fact] public void PendingOrInvalidMowingBlocksOtherTerritorialFlowsWithoutRecursion()
        {var w=Started();Assert.True(GangBulkTransactions.Blocked(w));Assert.False(GangAutoArmsRules.Free(w));Assert.True(GangMowingTransactions.OtherFree(w));Assert.False(GangPopUpTransactions.OtherFree(w));Assert.False(GangMedicalTransactions.OtherFree(w));Assert.False(GangBusinessTransactions.Trade(w,S(),A(),new GangBusinessOptions(),false,Utc,"trade",Save,out _));Assert.False(GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"supply",Save,out _));Assert.False(GangAutoLaunderTransactions.Configure(w,S(),A(),true,"auto",Save,out _));w=W();w.GangMowing=new GangMowingData{Accounts=null};Assert.True(GangBulkTransactions.Blocked(w));Assert.False(Begin(w));}
        [Fact] public void OtherPendingOrInvalidBooksPreventMowingStart()
        {var w=W();w.GangPopUp=new GangPopUpData{Accounts=null};Assert.False(Begin(w));Assert.Null(w.GangMowing);w=W();Assert.True(GangSupplyTransactions.Begin(w,S(),A(),new GangBusinessOptions(),new GangSupplyOptions(),"paid",Save,out _));Assert.False(Begin(w));Assert.Equal(9850,w.Bank);}
        [Fact] public void HistoryOverflowAndFullAccountCapacityRefuseStart()
        {var w=Ready();Complete(w);Reward(w).Completed=long.MaxValue;Assert.False(Begin(w,"again"));w=W();w.GangMowing=new GangMowingData();for(int n=0;n<640;n++)w.GangMowing.Accounts.Add(new GangMowingAccount{Gang="Other"+n,Completed=1,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks});Assert.True(w.GangMowing.Valid);Assert.False(Begin(w));}
        [Fact] public void ReceiptsRemainBoundedAfterCutAndFinalAward()
        {var w=Ready();w.GangMowing.Receipts=Enumerable.Range(0,256).Select(n=>"old"+n).ToList();Assert.True(Complete(w));Assert.Equal(256,w.GangMowing.Receipts.Count);Assert.DoesNotContain("old0",w.GangMowing.Receipts);}
        [Fact] public void InvalidUtcOrOverflowCannotGrantUnboundedBonus()
        {var w=Ready();Assert.False(Complete(w,utc:DateTime.SpecifyKind(Utc,DateTimeKind.Unspecified)));Assert.False(Complete(w,utc:DateTime.SpecifyKind(DateTime.MaxValue,DateTimeKind.Utc)));Assert.True(w.GangMowing.Pending.Ready);Assert.Empty(w.GangMowing.Accounts);}
        [Fact] public void ExistingPopUpSalesDirtyAndOtherBonusesRemainUntouched()
        {var w=Started();w.GangPopUp=new GangPopUpData{Accounts={new GangPopUpAccount{Gang=S().Gang,Contracts=1,Sales=10,Revenue=400,StartedUtcTicks=Utc.Ticks,ExpiresUtcTicks=Utc.AddHours(1).Ticks}}};w.GangIllicit=new GangIllicitData{Accounts={new GangIllicitAccount{Gang=S().Gang,Area=7,Dirty=400,Proceeds=400}}};string pop=JsonConvert.SerializeObject(w.GangPopUp),dirty=JsonConvert.SerializeObject(w.GangIllicit);for(int i=0;i<57;i++)Cut(w,i);Complete(w);Assert.Equal(pop,JsonConvert.SerializeObject(w.GangPopUp));Assert.Equal(dirty,JsonConvert.SerializeObject(w.GangIllicit));Assert.Equal(2,GangPopUpTransactions.Multiplier(w.GangPopUp,S().Gang,7,Utc));Assert.Equal(2,GangMowingTransactions.Multiplier(w.GangMowing,S().Gang,8,Utc));}
        [Fact] public void SmallerFutureCatalogUsesCapturedGoalWithoutDefaultMerging()
        {var w=W();var c=new GangMowingOptions();c.Patches=c.Patches.Take(2).ToList();Assert.True(Begin(w,c:c));w=JsonConvert.DeserializeObject<WalletData>(JsonConvert.SerializeObject(w));Assert.Equal(2,w.GangMowing.Pending.Options.Patches.Count);Cut(w);Assert.False(w.GangMowing.Pending.Ready);Cut(w,1);Assert.True(Complete(w));}
    }
}
