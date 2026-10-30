using System;
using Xunit;
namespace RPCore.Tests
{
 public sealed class CharacterHandoffRegistryTests
 {
  [Fact] public void NewlyCreatedIdentityIsRecognizedWithoutAnyDecoratorAPI(){var r=new CharacterHandoffRegistry();var lease=r.Register(10,-1667301416,123,456);Assert.True(r.Owns(lease,10,-1667301416,456));Assert.True(lease.CanDiscard(10,-1667301416,123,20));}
  [Theory][InlineData(11,-1667301416,456)][InlineData(10,1885233650,456)][InlineData(10,-1667301416,457)][InlineData(10,-1667301416,0)]
  public void ReusedHandleChangedModelOrPoolAddressRefusesOwnership(int handle,int model,long address){var r=new CharacterHandoffRegistry();var lease=r.Register(10,-1667301416,123,456);Assert.False(r.Owns(lease,handle,model,address));}
  [Fact] public void CopyingAllLeaseFieldsDoesNotAdoptForeignOrOldEntity(){var r=new CharacterHandoffRegistry();var lease=r.Register(10,20,30,40);Assert.False(r.Owns(new CharacterHandoffLease(10,20,30),10,20,40));Assert.False(new CharacterHandoffRegistry().Owns(lease,10,20,40));}
  [Fact] public void ForgottenIdentityCannotAuthorizeLaterDeletion(){var r=new CharacterHandoffRegistry();var lease=r.Register(10,20,30,40);r.Forget(lease);r.Forget(lease);Assert.False(r.Owns(lease,10,20,40));var next=r.Register(10,20,30,40);Assert.False(r.Owns(lease,10,20,40));Assert.True(r.Owns(next,10,20,40));}
  [Theory][InlineData(0,20,30,40)][InlineData(10,0,30,40)][InlineData(10,20,0,40)][InlineData(10,20,-1,40)][InlineData(10,20,30,0)]
  public void MissingIdentityCannotBeRegistered(int handle,int model,int token,long address){Assert.Throws<ArgumentException>(()=>new CharacterHandoffRegistry().Register(handle,model,token,address));}
  [Fact] public void ControlledPedStillCannotBeDiscardedWithoutCommit(){var r=new CharacterHandoffRegistry();var lease=r.Register(10,20,30,40);Assert.True(r.Owns(lease,10,20,40));Assert.False(lease.CanDiscard(10,20,30,10));}
  [Fact] public void CommittedPedOwnershipCanBeForgottenWithoutMakingItDisposable(){var r=new CharacterHandoffRegistry();var lease=r.Register(10,20,30,40);Assert.True(lease.Commit(10,20));r.Forget(lease);Assert.False(r.Owns(lease,10,20,40));Assert.False(lease.CanDiscard(10,20,30,99));}
  [Theory][InlineData(10,20,true)][InlineData(11,20,false)][InlineData(10,21,false)]
  public void StagingReleaseRequiresCommittedPlayerEvenIfPoolAddressChanges(int player,int model,bool allowed){var r=new CharacterHandoffRegistry();var lease=r.Register(10,20,30,40);Assert.False(lease.CanReleaseStaging(player,model));Assert.True(lease.Commit(10,20));Assert.False(r.Owns(lease,10,20,41));Assert.Equal(allowed,lease.CanReleaseStaging(player,model));Assert.False(lease.CanDiscard(10,20,30,99));}
  [Fact] public void ForgettingOneCandidateDoesNotDiscardAnother(){var r=new CharacterHandoffRegistry();var a=r.Register(10,20,30,40);var b=r.Register(11,20,31,41);r.Forget(a);Assert.True(r.Owns(b,11,20,41));Assert.False(r.Owns(a,10,20,40));}
 }
}
