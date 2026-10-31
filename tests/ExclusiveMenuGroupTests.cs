using System;
using Xunit;
namespace RPCore.Tests
{
    public class ExclusiveMenuGroupTests
    {
        sealed class View
        {
            public bool Visible,Parent;public int Navigations,Transactions;
            public readonly ServiceActionQueue Queue=new ServiceActionQueue();
            // Like PersonalServiceMenu.Close: remove parent before raising the native close event.
            public void Close(){Parent=false;Queue.NewPage();Visible=false;}
            public void Back(){if(Parent)Queue.Enqueue(()=>Navigations++,1,123,"RP",10);}
        }
        static View Add(ExclusiveMenuGroup<View> group)
        {var view=new View();group.Register(view,view.Close,()=>view.Visible);return view;}
        [Fact] public void ReturningFromShowroomToCompanyClosesOldPageAndDiscardsItsBack()
        {
            var g=new ExclusiveMenuGroup<View>();var company=Add(g);var showroom=Add(g);
            g.Activate(showroom);showroom.Visible=true;showroom.Parent=true;showroom.Back();
            g.Activate(company);company.Visible=true;
            Assert.False(showroom.Visible);Assert.False(showroom.Parent);Assert.True(g.Owns(company));
            Assert.False(showroom.Queue.Run(true,true,1,123,"RP",11));Assert.Equal(0,showroom.Navigations);
            Assert.True(company.Queue.Enqueue(()=>company.Transactions++,1,123,"RP",11));company.Queue.Run(true,g.Owns(company),1,123,"RP",12);Assert.Equal(1,company.Transactions);
        }
        [Fact] public void RapidNavigationNeverLeavesTwoViewsOrReplaysTransaction()
        {
            var g=new ExclusiveMenuGroup<View>();var views=new[]{Add(g),Add(g),Add(g)};
            for(int i=0;i<200;i++){
                var next=views[i%3];Assert.True(g.Activate(next));next.Visible=true;next.Parent=true;next.Queue.Release(false);
                next.Queue.Enqueue(()=>next.Transactions++,1,123,"RP",i);
                var other=views[(i+1)%3];g.Activate(other);other.Visible=true;
                foreach(var v in views){Assert.Equal(ReferenceEquals(v,other),v.Visible);v.Queue.Run(v.Visible,g.Owns(v),1,123,"RP",i);}
            }
            foreach(var view in views)Assert.Equal(0,view.Transactions);
        }
        [Fact] public void EmergencyCloseClearsEveryPageAndPendingAction()
        {
            var g=new ExclusiveMenuGroup<View>();var a=Add(g);var b=Add(g);g.Activate(a);a.Visible=true;b.Visible=true;a.Parent=b.Parent=true;
            a.Back();b.Back();g.CloseAll();Assert.Null(g.Owner);Assert.False(a.Visible);Assert.False(b.Visible);
            Assert.False(a.Queue.Run(true,true,1,123,"RP",11));Assert.False(b.Queue.Run(true,true,1,123,"RP",11));
            g.Activate(b);b.Queue.Release(false);Assert.True(b.Queue.Enqueue(()=>b.Transactions++,1,123,"RP",20));b.Queue.Run(true,g.Owns(b),1,123,"RP",21);Assert.Equal(1,b.Transactions);
        }
        [Fact] public void ReconcileRepairsLeakedVisibilityWithoutExecutingBack()
        {var g=new ExclusiveMenuGroup<View>();var a=Add(g);var b=Add(g);g.Activate(a);a.Visible=b.Visible=true;b.Parent=true;b.Back();Assert.Equal(1,g.Reconcile());Assert.True(a.Visible);Assert.False(b.Visible);Assert.False(b.Queue.Run(true,true,1,123,"RP",11));Assert.Equal(0,g.Reconcile());}
        [Fact] public void NoOwnerMeansOrphanedMenusAreClosed()
        {var g=new ExclusiveMenuGroup<View>();var a=Add(g);var b=Add(g);a.Visible=b.Visible=true;Assert.Equal(2,g.Reconcile());}
        [Fact] public void ClosingAnOlderViewDoesNotRevokeNewOwnersInput()
        {var g=new ExclusiveMenuGroup<View>();var a=Add(g);var b=Add(g);g.Activate(a);g.Activate(b);g.Release(a);Assert.True(g.Owns(b));g.Release(b);Assert.Null(g.Owner);}
        [Fact] public void ClosingCannotReenterAndOpenAParentDuringHandoff()
        {
            var g=new ExclusiveMenuGroup<View>();var a=Add(g);var b=new View();int nested=0;
            g.Register(b,()=>{b.Close();if(g.Activate(a))nested++;},()=>b.Visible);
            Assert.True(g.Activate(b));b.Visible=true;Assert.True(g.Owns(b));Assert.Equal(0,nested);g.CloseAll();Assert.Null(g.Owner);Assert.Equal(0,nested);
        }
        [Fact] public void IndependentPoolsNeverCloseEachOthersMenus()
        {var first=new ExclusiveMenuGroup<View>();var second=new ExclusiveMenuGroup<View>();var a=Add(first);var b=Add(second);first.Activate(a);a.Visible=true;second.Activate(b);b.Visible=true;first.CloseAll();Assert.True(b.Visible);Assert.True(second.Owns(b));}
        [Fact] public void DuplicateAndUnknownViewsCannotTakeOwnership()
        {var g=new ExclusiveMenuGroup<View>();var a=Add(g);Assert.Throws<InvalidOperationException>(()=>g.Register(a,a.Close,()=>a.Visible));Assert.Throws<InvalidOperationException>(()=>g.Activate(new View()));}
        [Fact] public void ACloseFailureDoesNotLeaveHalfAssignedOwnerOrPermanentlyLockGroup()
        {var g=new ExclusiveMenuGroup<View>();var a=Add(g);bool fail=true;var b=new View();g.Register(b,()=>{if(fail)throw new Exception();b.Close();},()=>b.Visible);Assert.Throws<Exception>(()=>g.Activate(a));Assert.Null(g.Owner);fail=false;Assert.True(g.Activate(a));Assert.True(g.Owns(a));}
        [Fact] public void HeldBackThatCannotQueueDoesNotClaimSuccessfulNavigation()
        {var q=new ServiceActionQueue();q.Hold();Assert.False(q.Enqueue(()=>throw new Exception(),1,123,"RP",1));q.Release(false);Assert.True(q.Enqueue(()=>{},1,123,"RP",2));}
    }
}
