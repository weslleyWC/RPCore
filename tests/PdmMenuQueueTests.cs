using System;
using Xunit;
namespace RPCore.Tests
{
    public class PdmMenuQueueTests
    {
        [Fact] public void CompanyToCameraMenuDoesNotExecuteCameraDuringTheSameSelection()
        {
            var company=new ServiceActionQueue();var cameras=new ServiceActionQueue();int opens=0,views=0;
            company.Enqueue(()=>{opens++;cameras.NewPage();cameras.Hold();},1,123,"RP",10);
            Assert.Equal(0,opens);company.Run(true,true,1,123,"RP",11);Assert.Equal(1,opens);
            Assert.False(cameras.Enqueue(()=>views++,1,123,"RP",11));Assert.Equal(0,views);
            cameras.Release(false);Assert.True(cameras.Enqueue(()=>views++,1,123,"RP",20));Assert.Equal(0,views);
            cameras.Run(true,true,1,123,"RP",21);Assert.Equal(1,views);
        }
        [Fact] public void FailedCompanyActionDoesNotReplayOrBlockAnotherMenu()
        {
            var company=new ServiceActionQueue();var settings=new ServiceActionQueue();int attempts=0,opened=0;
            company.Enqueue(()=>{attempts++;throw new InvalidOperationException("bad page");},1,123,"RP",10);
            Assert.Throws<InvalidOperationException>(()=>company.Run(true,true,1,123,"RP",11));Assert.Equal(1,attempts);
            Assert.False(company.Run(true,true,1,123,"RP",12));Assert.True(settings.Enqueue(()=>opened++,1,123,"RP",12));
            settings.Run(true,true,1,123,"RP",13);Assert.Equal(1,opened);
        }
        [Fact] public void LeavingCompanyOrChangingPageDiscardsOldPurchaseClick()
        {
            var q=new ServiceActionQueue();int purchases=0;q.Enqueue(()=>purchases++,1,123,"RP",10);q.NewPage();
            Assert.False(q.Run(true,true,1,123,"RP",11));q.Release(false);q.Enqueue(()=>purchases++,1,123,"RP",12);
            Assert.False(q.Run(false,true,1,123,"RP",13));Assert.Equal(0,purchases);
        }
        [Fact] public void ProfileSwitchCannotApplyQueuedCompanyTransactionToAnotherWallet()
        {
            var q=new ServiceActionQueue();int oldBank=100,newBank=100;
            q.Enqueue(()=>oldBank-=50,1,123,"old",10);Assert.False(q.Run(true,true,1,123,"new",11));Assert.Equal(100,oldBank);Assert.Equal(100,newBank);
        }
    }
}
