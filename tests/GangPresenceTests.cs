using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class GangPresenceTests
    {
        static GangSpot P(float x=10,float y=20,float z=30)=>new GangSpot{X=x,Y=y,Z=z,Heading=90};
        static GangDefinition G()=>new GangDefinition{Name="Mine",HQX=10,HQY=20,HQZ=30,LeaderModel="g_m_m_oggoon_01",LeaderSpawnPostion=P(),PedModels=new[]{"g_m_y_famca_01"},PedSpawnSpots=new List<GangSpot>{P(15),P(20)}};
        static List<GangDefinition> L()=>new List<GangDefinition>{G()};
        [Fact] public void LegacyGangStateDoesNotAutoEnablePresenceOrChangeAffiliation()
        {var s=JsonConvert.DeserializeObject<GangState>("{\"Gang\":\"Mine\",\"Reputation\":42,\"Future\":7}");Assert.False(s.PresenceEnabled);Assert.Equal("Mine",s.Gang);Assert.Equal(42,s.Reputation);Assert.Equal(7,(int)JObject.FromObject(s)["Future"]);}
        [Fact] public void PresencePreferenceIsIsolatedAndRetainsUnknownState()
        {var a=JsonConvert.DeserializeObject<GangState>("{\"Gang\":\"Mine\",\"Reputation\":10,\"Future\":{\"keep\":true}}");var b=new GangState();var next=CharacterRules.Copy(a);next.PresenceEnabled=true;Assert.False(a.PresenceEnabled);Assert.False(b.PresenceEnabled);Assert.Equal(10,next.Reputation);Assert.True((bool)JObject.FromObject(next)["Future"]["keep"]);}
        [Fact] public void LeaderAndMembersUseRecordedPostsAndDeterministicKeys()
        {var p=GangPresenceRules.Plan(G(),6);Assert.Equal(3,p.Count);Assert.True(p[0].Leader);Assert.Equal("leader",p[0].Key);Assert.Equal(90,p[1].Spot.Heading);Assert.Equal("member:0",p[1].Key);Assert.Equal("member:1",p[2].Key);Assert.False(p[1].Leader);}
        [Theory][InlineData(1,1)][InlineData(2,2)][InlineData(6,3)][InlineData(0,0)][InlineData(9,0)]
        public void PerGangCapCannotBeExceeded(int max,int count)=>Assert.Equal(count,GangPresenceRules.Plan(G(),max).Count);
        [Fact] public void MissingModelsAndMissingLeaderAreSkippedWithoutInventedSpawnPoints()
        {var g=G();g.LeaderSpawnPostion=null;g.PedModels=null;Assert.Empty(GangPresenceRules.Plan(g,6));g.PedModels=new[]{"g_m_y_famca_01"};Assert.Equal(2,GangPresenceRules.Plan(g,6).Count);}
        [Fact] public void OverlappingRemoteAndInvalidPostsDoNotDuplicateNPCs()
        {var g=G();g.PedSpawnSpots=new List<GangSpot>{P(),P(11),P(12),P(1000),P(float.NaN),null};var p=GangPresenceRules.Plan(g,6);Assert.Equal(2,p.Count);Assert.Equal("member:2",p[1].Key);}
        [Fact] public void InvalidGangHQCannotGenerateAnyActors()
        {var g=G();g.HQX=float.PositiveInfinity;Assert.Empty(GangPresenceRules.Plan(g,6));Assert.Empty(GangPresenceRules.Plan(null,6));}
        [Theory][InlineData("g_m_y_famca_01",true)][InlineData("You the player",false)][InlineData("../ped",false)][InlineData("",false)][InlineData(null,false)][InlineData("G_M_M_OGGOON_01",true)]
        public void ModelNamesAreValidatedBeforeNativeRequests(string name,bool valid)=>Assert.Equal(valid,GangPresenceRules.ModelName(name));
        [Theory][InlineData(80,false,true)][InlineData(81,false,false)][InlineData(81,true,true)][InlineData(160,true,true)][InlineData(161,true,false)][InlineData(-1,true,false)][InlineData(float.NaN,true,false)]
        public void SpawnDespawnHysteresisMatchesReferenceRadii(float distance,bool exists,bool keep)=>Assert.Equal(keep,GangPresenceRules.Keep(distance,exists,new GangPresenceOptions()));
        [Fact] public void ConfigRequiresBoundedActorCountsAndSeparateRadii()
        {Assert.True(new GangPresenceOptions().Valid);Assert.False(new GangPresenceOptions{MaxActors=0}.Valid);Assert.False(new GangPresenceOptions{MaxActors=25}.Valid);Assert.False(new GangPresenceOptions{PerHQ=9}.Valid);Assert.False(new GangPresenceOptions{SpawnRadius=80,DespawnRadius=90}.Valid);Assert.False(new GangPresenceOptions{RespawnSeconds=0}.Valid);Assert.False(new GangPresenceOptions{PerHQ=8,MaxActors=4}.Valid);}
        [Fact] public void RecordedLeaderAndMemberModelsArePreserved()
        {var source=L();var next=GangPresenceRules.Edit(source,"mine",P(25),true);Assert.NotNull(next);Assert.Equal("g_m_m_oggoon_01",next[0].LeaderModel);Assert.Equal(25,next[0].LeaderSpawnPostion.X);Assert.Equal(10,source[0].LeaderSpawnPostion.X);Assert.Equal(90,next[0].LeaderSpawnHeading);}
        [Fact] public void NewCreatedGangCanGainAFunctionalLeaderAndMembers()
        {var source=new List<GangDefinition>{new GangDefinition{Name="Mine",HQX=10,HQY=20,HQZ=30}};source=GangPresenceRules.Edit(source,"Mine",P(),true);source=GangPresenceRules.Edit(source,"Mine",P(15),false);var p=GangPresenceRules.Plan(source[0],6);Assert.Equal(2,p.Count);Assert.Equal("g_m_m_oggoon_01",p[0].Model);Assert.Equal("g_m_y_famca_01",p[1].Model);}
        [Fact] public void EditingUnknownOrDuplicateGangCannotMutateDefinitions()
        {var s=L();Assert.Null(GangPresenceRules.Edit(s,"Other",P(25),true));s.Add(G());Assert.Null(GangPresenceRules.Edit(s,"Mine",P(25),true));Assert.Equal(10,s[0].LeaderSpawnPostion.X);}
        [Theory][InlineData(10)][InlineData(11)][InlineData(15)][InlineData(1000)][InlineData(float.NaN)]
        public void CollidingOrRemoteMemberPostsAreRejectedWithoutMutatingSource(float x)
        {var s=L();Assert.Null(GangPresenceRules.Edit(s,"Mine",P(x),false));Assert.Equal(2,s[0].PedSpawnSpots.Count);}
        [Fact] public void LeaderCannotBePlacedOnMemberPost()
        {Assert.Null(GangPresenceRules.Edit(L(),"Mine",P(15),true));}
        [Fact] public void MemberCapAppliesWithoutDroppingExistingEntries()
        {var s=L();s[0].PedSpawnSpots=Enumerable.Range(0,20).Select(i=>P(12+i*2)).ToList();Assert.Null(GangPresenceRules.Edit(s,"Mine",P(65),false));Assert.Equal(20,s[0].PedSpawnSpots.Count);}
        [Fact] public void RemovalOnlyDropsLastMemberAndKeepsLeaderAndSourceIntact()
        {var s=L();var next=GangPresenceRules.Edit(s,"Mine",null,false,true);Assert.Single(next[0].PedSpawnSpots);Assert.Equal(2,s[0].PedSpawnSpots.Count);Assert.Equal(10,next[0].LeaderSpawnPostion.X);}
        [Fact] public void EmptyMemberRemovalIsNoOp()
        {var s=L();s[0].PedSpawnSpots=null;Assert.Null(GangPresenceRules.Edit(s,"Mine",null,false,true));}
        [Fact] public void EditingPreservesUnknownGangSpotAndOtherGangMetadata()
        {var j=JObject.FromObject(G());j["Future"]="gang";j["CoLeaderModel"]="a_m_y_business_01";j["Vehicles"]=new JArray(new JObject{{"Future",7}});j["LeaderSpawnPostion"]["Normalized"]=new JObject{{"keep",true}};j["PedSpawnSpots"][0]["Future"]="post";var s=new List<GangDefinition>{j.ToObject<GangDefinition>(),new GangDefinition{Name="Other"}};var next=GangPresenceRules.Edit(s,"Mine",P(25),true);var result=JObject.FromObject(next[0]);Assert.Equal("gang",(string)result["Future"]);Assert.Equal("a_m_y_business_01",(string)result["CoLeaderModel"]);Assert.Equal(7,(int)result["Vehicles"][0]["Future"]);Assert.True((bool)result["LeaderSpawnPostion"]["Normalized"]["keep"]);Assert.Equal("post",(string)result["PedSpawnSpots"][0]["Future"]);Assert.Equal("Other",next[1].Name);}
        [Fact] public void FailedAtomicWriteLeavesCreatedGangFileUntouched()
        {var dir=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"rpcore-gang-"+Guid.NewGuid().ToString("N"));System.IO.Directory.CreateDirectory(dir);try{var p=System.IO.Path.Combine(dir,"gangs.json");System.IO.File.WriteAllText(p,"{");var files=new JsonFiles();Assert.ThrowsAny<Exception>(()=>files.Read(p,()=>L()));Assert.ThrowsAny<Exception>(()=>files.Write(p,L()));Assert.Equal("{",System.IO.File.ReadAllText(p));}finally{System.IO.Directory.Delete(dir,true);}}
    }
}
