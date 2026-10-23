using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace RPCore.Tests
{
    public class PilotAccessTests
    {
        static JobPoint P(string n,float x,float y,float z,float h=0)=>new JobPoint{Name=n,X=x,Y=y,Z=z,Heading=h};
        static PilotOptions Old()
        {
            var c=PilotDefaults.Load();c.Start=P("LSIA desk",-1143.238f,-2705.508f,13.95856f);
            c.PlaneSpawn=P("LSIA plane",-1023.876f,-2903.852f,13.96453f,148.8467f);
            c.HeliSpawn=P("LSIA heli",-1111.612f,-2883.941f,13.94604f,146.4589f);c.HeliTimeLimitSeconds=150;return c;
        }
        [Fact] public void ShippedLegacyBaseMovesInMemoryWithoutChangingLocalData()
        {
            var c=Old();string before=JsonConvert.SerializeObject(c);var effective=PilotAccess.Resolve(c,out bool moved);
            Assert.True(moved);Assert.Equal(2118f,effective.Start.X);Assert.Equal(2130.8f,effective.PlaneSpawn.X);Assert.Equal(2111f,effective.HeliSpawn.X);
            Assert.Equal(before,JsonConvert.SerializeObject(c));Assert.NotSame(c,effective);Assert.False(PilotAccess.LegacyBase(effective));
        }
        [Fact] public void MigrationRetainsPricesModelsDestinationHelipadsAndUnknownFields()
        {
            var c=Old();c.PlaneBasePay=2100;c.HeliModel="swift";c.HeliPassengers=2;c.Extra=new Dictionary<string,JToken>{{"Future",new JObject{{"keep",true}}}};
            string pads=JsonConvert.SerializeObject(c.Helipads);var e=PilotAccess.Resolve(c,out bool moved);
            Assert.True(moved);Assert.Equal(2100,e.PlaneBasePay);Assert.Equal("swift",e.HeliModel);Assert.Equal(2,e.HeliPassengers);Assert.True((bool)e.Extra["Future"]["keep"]);
            Assert.Equal(pads,JsonConvert.SerializeObject(e.Helipads));Assert.Equal(c.PlaneDestination.X,e.PlaneDestination.X);Assert.Equal(c.PlaneDestination.Y,e.PlaneDestination.Y);
        }
        [Theory] [InlineData("desk")] [InlineData("plane")] [InlineData("heli")] [InlineData("heading")]
        public void CustomBaseCoordinatesOrHeadingRemainUntouched(string change)
        {
            var c=Old();if(change=="desk")c.Start.X+=20;if(change=="plane")c.PlaneSpawn.Y+=20;if(change=="heli")c.HeliSpawn.X+=20;if(change=="heading")c.PlaneSpawn.Heading+=10;
            string before=JsonConvert.SerializeObject(c);var e=PilotAccess.Resolve(c,out bool moved);Assert.False(moved);Assert.Same(c,e);Assert.Equal(before,JsonConvert.SerializeObject(e));
        }
        [Fact] public void ExplicitLegacyAirportOptInPreservesCoordinatesAndDeadline()
        {var c=Old();c.KeepLegacyLsiaBase=true;var e=PilotAccess.Resolve(c,out bool moved);Assert.False(moved);Assert.Same(c,e);Assert.Equal(150,e.HeliTimeLimitSeconds);}
        [Fact] public void DefaultExecutiveDeadlineAdaptsToLongerRouteButCustomDeadlineDoesNot()
        {
            var c=Old();var e=PilotAccess.Resolve(c,out _);Assert.Equal(240,e.HeliTimeLimitSeconds);Assert.Equal(150,c.HeliTimeLimitSeconds);
            c.HeliTimeLimitSeconds=300;Assert.Equal(300,PilotAccess.Resolve(c,out _).HeliTimeLimitSeconds);
            c.HeliTimeLimitSeconds=0;Assert.Equal(0,PilotAccess.Resolve(c,out _).HeliTimeLimitSeconds);
        }
        [Fact] public void PublicDefaultNeedsNoMigrationAndKeepsOriginalSandyDestination()
        {var c=PilotDefaults.Load();Assert.True(c.Valid);Assert.Equal(1698.431f,c.PlaneDestination.X);Assert.Equal(3249.649f,c.PlaneDestination.Y);Assert.Same(c,PilotAccess.Resolve(c,out bool moved));Assert.False(moved);}
        [Fact] public void ResolvingTwiceDoesNotMoveBaseAgain()
        {var c=PilotAccess.Resolve(Old(),out bool first);var twice=PilotAccess.Resolve(c,out bool second);Assert.True(first);Assert.False(second);Assert.Same(c,twice);}
        [Fact] public void OmittedOptInInOldJsonStillRecognizesShippedAirport()
        {var j=JObject.FromObject(Old());j.Remove("KeepLegacyLsiaBase");var c=j.ToObject<PilotOptions>();Assert.True(PilotAccess.LegacyBase(c));PilotAccess.Resolve(c,out bool moved);Assert.True(moved);}
    }
}
