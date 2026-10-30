using System;
using System.IO;
using Newtonsoft.Json.Linq;
using RPCore;
using Xunit;
namespace RPCoreTests
{
    public class RuntimeTraceTests
    {
        [Fact] public void ReopenReportsThePreviousStageUsingThePreviousVersionsSystemMap()
        {
            string dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));
            try{
                using(var first=new RuntimeTrace(dir,"old",new[]{"vehiclecontrols","phone"})){Assert.Null(first.Previous);first.Mark(1000);first.Heartbeat();}
                using(var second=new RuntimeTrace(dir,"new",new[]{"phone","vehiclecontrols"})){Assert.Equal("old",second.Previous.Version);Assert.Equal("tick:vehiclecontrols",second.Previous.Stage);Assert.False(second.Previous.AbortedHookReached);Assert.Equal(1,second.Previous.Sequence);}
                Assert.Equal("tick:vehiclecontrols",(string)JObject.Parse(File.ReadAllText(Path.Combine(dir,"RuntimeLastSession.json")))["Stage"]);
            }finally{Directory.Delete(dir,true);}
        }
        [Fact] public void AbortedHookIsRecordedSeparatelyAndDisposeIsIdempotent()
        {
            string dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));
            try{var first=new RuntimeTrace(dir,"v",new[]{"phone"});first.Mark(2000);first.Aborted();first.Dispose();first.Dispose();first.Mark(1000);using(var next=new RuntimeTrace(dir,"v",new[]{"phone"})){Assert.True(next.Previous.AbortedHookReached);Assert.Equal("OnAborted",next.Previous.Stage);}}finally{Directory.Delete(dir,true);}
        }
        [Fact] public void MalformedDiagnosticFileDoesNotTouchSavesAndDoesNotInventAPreviousStage()
        {
            string dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            try{File.WriteAllText(Path.Combine(dir,"RuntimeState.bin"),"invalid");File.WriteAllText(Path.Combine(dir,"Wallet.json"),"keep");using(var trace=new RuntimeTrace(dir,"v",new[]{"x"})){Assert.Null(trace.Previous);}Assert.Equal("keep",File.ReadAllText(Path.Combine(dir,"Wallet.json")));}finally{Directory.Delete(dir,true);}
        }
    }
}
