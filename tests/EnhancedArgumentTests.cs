using System;
using GTA.Native;
using Xunit;
namespace RPCoreTests
{
    // Executes only the installed SDK's managed/marshal buffer API, never GTA natives.
    public class EnhancedArgumentTests
    {
        [Fact] public void InitializedEightByteDoorOutputsAreAcceptedAndStartAtZero()
        {using(var output=OutputArgument.Init(0UL)){Assert.Equal(0UL,output.GetResultAsBlittableStruct<ulong>());}}
        [Theory][InlineData(123)][InlineData(-123)][InlineData(int.MaxValue)]
        public void DeletionHandleRetainsItsLow32BitsInAnEightByteOwnedBuffer(int handle)
        {using(var output=OutputArgument.Init((ulong)(uint)handle))Assert.Equal((uint)handle,(uint)output.GetResultAsBlittableStruct<ulong>());}
    }
}
