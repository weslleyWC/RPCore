using System;
using Xunit;
namespace RPCore.Tests
{
 public class GameplayContextTests
 {
  [Theory]
  [InlineData(false,false,false,false,false)] [InlineData(false,false,true,false,false)]
  [InlineData(false,true,false,true,false)] [InlineData(false,true,true,true,false)]
  [InlineData(true,false,false,true,false)] [InlineData(true,false,true,false,true)]
  [InlineData(true,true,false,true,false)] [InlineData(true,true,true,true,false)]
  public void SceneLifecycle(bool active,bool playing,bool control,bool block,bool prepared)
  {Assert.Equal(block,GameplayContextRules.SceneBlocks(active,playing,control));Assert.Equal(prepared,GameplayContextRules.PreparedOnly(active,playing,control));}
  [Theory]
  [InlineData(false,false,false,false,false)] [InlineData(false,false,true,false,true)]
  [InlineData(false,false,true,true,false)] [InlineData(true,false,true,true,true)] [InlineData(false,true,true,true,true)]
  public void HudVisibility(bool pause,bool block,bool hidden,bool prepared,bool expected)
  {Assert.Equal(expected,GameplayContextRules.HideHud(pause,block,hidden,prepared));}
  static void Tick(AreaWarmup w,int t,int actor=1,string profile="Franklin",float x=0,bool near=true,bool safe=true,bool collision=true)
  {w.Update(t,actor,profile,x,0,0,near,safe,collision);}
  static AreaWarmup Ready(int start=0)
  {var w=new AreaWarmup();for(int i=0;i<=4;i++)Tick(w,unchecked(start+i*1000));Assert.True(w.Ready);return w;}
  [Fact] public void ArrivalWaitsFourSeconds()
  {var w=new AreaWarmup();for(int t=0;t<4000;t+=1000){Tick(w,t);Assert.False(w.Ready);}Tick(w,4000);Assert.True(w.Ready);Tick(w,5000);Assert.True(w.Ready);}
  [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
  public void ContextLossResetsWarmup(int cause)
  {var w=Ready();Tick(w,cause==0?6001:5000,actor:cause==1?2:cause==7?0:1,profile:cause==2?"Michael":"Franklin",x:cause==3?41:0,near:cause!=4,safe:cause!=5,collision:cause!=6);Assert.False(w.Ready);}
  [Fact] public void ReturningNeedsFullWarmup()
  {var w=Ready();Tick(w,5000,near:false);Tick(w,6000);Assert.False(w.Ready);for(int t=7000;t<=10000;t+=1000)Tick(w,t);Assert.True(w.Ready);Tick(w,11000,collision:false);Assert.False(w.Ready);}
  [Theory] [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)] [InlineData(float.NegativeInfinity)]
  public void InvalidCoordinatesBlockSpawning(float x)
  {var w=Ready();Tick(w,5000,x:x);Assert.False(w.Ready);Tick(w,6000);Assert.False(w.Ready);}
  [Fact] public void ClockWrapAndBackwardClock()
  {var w=Ready(int.MaxValue-2000);Tick(w,unchecked(int.MaxValue+3000));Assert.True(w.Ready);Tick(w,100);Assert.False(w.Ready);}
 }
}
