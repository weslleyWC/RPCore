using Xunit;
namespace RPCore.Tests
{
 public sealed class CharacterHandoffTests
 {
  static CharacterHandoffLease Lease()=>new CharacterHandoffLease(100,-1667301416,123);
  [Fact] public void PreparedOwnedCandidateCanBeDiscardedWithoutTouchingSource(){var x=Lease();Assert.True(x.CanDiscard(100,-1667301416,123,50));Assert.False(x.CanDiscard(50,-1667301416,123,50));}
  [Theory][InlineData(101,-1667301416,123)][InlineData(100,1885233650,123)][InlineData(100,-1667301416,124)] public void ReusedHandleModelOrDecorNeverAuthorizesDeletion(int handle,int model,int token){Assert.False(Lease().CanDiscard(handle,model,token,50));}
  [Fact] public void ActualPlayerIsNeverDeletedEvenBeforeCommitFlag(){Assert.False(Lease().CanDiscard(100,-1667301416,123,100));}
  [Fact] public void SuccessfulHandoffCommitsOnlyOnce(){var x=Lease();Assert.True(x.Commit(100,-1667301416));Assert.True(x.HandedOff);Assert.False(x.Commit(100,-1667301416));Assert.False(x.CanDiscard(100,-1667301416,123,50));}
  [Theory][InlineData(50,-1667301416)][InlineData(100,1885233650)][InlineData(0,-1667301416)] public void WrongPlayerOrModelDoesNotCommit(int player,int model){var x=Lease();Assert.False(x.Commit(player,model));Assert.False(x.HandedOff);Assert.True(x.CanDiscard(100,-1667301416,123,50));}
  [Theory][InlineData(0,1,1)][InlineData(1,0,1)][InlineData(1,1,0)][InlineData(1,1,-1)] public void MissingProofCannotCommitOrDelete(int h,int m,int token){var x=new CharacterHandoffLease(h,m,token);Assert.False(x.Commit(h,m));Assert.False(x.CanDiscard(h,m,token,99));}
  [Fact] public void PendingCharacterCardSurvivesFailedControlHandoff(){var x=Lease();var c=new CharacterCard{Id="RP_1234567890abcdef",Name="Saved",Model="mp_f_freemode_01",BaseFace=new FaceHeritage{ShapeFirst=0,ShapeSecond=21,SkinFirst=0,SkinSecond=21},ApplyPending=true};string before=Newtonsoft.Json.JsonConvert.SerializeObject(c);Assert.False(x.Commit(50,1885233650));Assert.Equal(before,Newtonsoft.Json.JsonConvert.SerializeObject(c));Assert.True(c.ApplyPending);}
 }
}
