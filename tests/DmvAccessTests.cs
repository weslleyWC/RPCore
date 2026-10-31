using System;
using Xunit;
namespace RPCore.Tests
{
    public class DmvAccessTests
    {
        static DmvAccessBlock Free()=>DmvAccessRules.Menu(true,true,true,false,false,false,false,false,false,0);
        [Theory] [InlineData(DmvClass.C)] [InlineData(DmvClass.M1)] [InlineData(DmvClass.A)]
        public void RealDmvLocationsOpenAndPrepareEvenThoughPdmWarmupNeverBecomesReady(DmvClass category)
        {
            var options=category==DmvClass.C?DmvDefaults.Load():category==DmvClass.M1?DmvDefaults.Motorcycle():DmvDefaults.Truck();var p=options.Entry;double dx=p.X+30,dy=p.Y+1104;Assert.True(dx*dx+dy*dy>220*220);
            var pdm=new AreaWarmup();var dmv=new AreaWarmup();for(int now=0;now<=5000;now+=1000){pdm.Update(now,10,"Franklin",p.X,p.Y,p.Z,false,false,false);dmv.Update(now,10,"Franklin",p.X,p.Y,p.Z,true,true,true);}
            Assert.False(pdm.Ready);Assert.True(dmv.Ready);Assert.False(pdm.Ready && Free()==DmvAccessBlock.None); // the old Calm predicate was impossible here
            Assert.Equal(DmvAccessBlock.None,Free());Assert.True(DmvAccessRules.CanPrepare(Free(),true,0,dmv.Ready));
        }
        [Fact] public void F10ConsultationDoesNotRequireBeingNearEitherBusinessOrHavingLoadedTerrain()
        {
            var cold=new AreaWarmup();cold.Update(0,10,"Franklin",1000,1000,50,false,false,false);Assert.False(cold.Ready);Assert.Equal(DmvAccessBlock.None,Free());Assert.False(DmvAccessRules.CanPrepare(Free(),true,0,cold.Ready));
        }
        [Theory] [InlineData(true,false,true,DmvAccessBlock.None)] [InlineData(true,true,true,DmvAccessBlock.Scene)] [InlineData(true,false,false,DmvAccessBlock.Scene)] [InlineData(false,false,false,DmvAccessBlock.Loading)]
        public void PreparedStorySceneWithPlayerControlDoesNotLockDmv(bool active,bool playing,bool control,DmvAccessBlock expected)
        {Assert.Equal(expected,DmvAccessRules.Menu(true,control,true,active,playing,false,false,false,false,0));}
        [Theory] [InlineData("character",DmvAccessBlock.Character)] [InlineData("control",DmvAccessBlock.Loading)] [InlineData("profile",DmvAccessBlock.Profile)] [InlineData("pause",DmvAccessBlock.Paused)] [InlineData("mission",DmvAccessBlock.Mission)] [InlineData("modal",DmvAccessBlock.Modal)] [InlineData("combat",DmvAccessBlock.Combat)] [InlineData("wanted",DmvAccessBlock.Wanted)]
        public void LegitimateRestrictionsHaveAnExplicitUserMessage(string reason,DmvAccessBlock expected)
        {
            var block=DmvAccessRules.Menu(reason!="character",reason!="control",reason!="profile",false,false,reason=="pause",reason=="mission",reason=="modal",reason=="combat",reason=="wanted"?1:0);Assert.Equal(expected,block);Assert.False(string.IsNullOrWhiteSpace(DmvAccessRules.Explain(block)));Assert.False(DmvAccessRules.CanPrepare(block,true,0,true));
        }
        [Theory] [InlineData(true,0,true,true)] [InlineData(true,3,true,true)] [InlineData(true,3.01f,true,false)] [InlineData(false,0,true,false)] [InlineData(true,0,false,false)] [InlineData(true,float.NaN,true,false)] [InlineData(true,-1,true,false)]
        public void PhysicalPreparationStillRequiresFootDistanceAndOwnTerrain(bool foot,float distance,bool terrain,bool allowed)=>Assert.Equal(allowed,DmvAccessRules.CanPrepare(Free(),foot,distance,terrain));
        [Fact] public void LocalWarmupWaitsOnlyForOwnCollisionAndResetsOnProfileChange()
        {
            var own=new AreaWarmup();for(int t=0;t<=4000;t+=1000)own.Update(t,10,"one",-544,-204,38,true,true,true);Assert.True(own.Ready);own.Update(5000,10,"two",-544,-204,38,true,true,true);Assert.False(own.Ready);Assert.Equal(DmvAccessBlock.None,Free());Assert.False(DmvAccessRules.CanPrepare(Free(),true,0,own.Ready));for(int t=6000;t<=9000;t+=1000)own.Update(t,10,"two",-544,-204,38,true,true,true);Assert.True(own.Ready);
        }
        [Theory] [InlineData(true,false,true,false,true,true)] [InlineData(false,false,true,false,true,false)] [InlineData(true,true,true,false,true,false)] [InlineData(true,false,false,false,true,false)] [InlineData(true,false,true,true,true,false)] [InlineData(true,false,true,false,false,false)]
        public void ExamUsesCurrentLocalCollisionAndLoadingState(bool control,bool scene,bool faded,bool switching,bool collision,bool allowed)=>Assert.Equal(allowed,DmvAccessRules.DrivingReady(control,scene,faded,switching,collision));
    }
}
