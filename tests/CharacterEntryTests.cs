using System;
using System.IO;
using System.Linq;
using Xunit;
namespace RPCore.Tests
{
 public sealed class CharacterEntryTests:IDisposable
 {
  readonly string root=Path.Combine(Path.GetTempPath(),"rpcore-entry-"+Guid.NewGuid().ToString("N"));
  public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
  static CharacterCard Card(string id,string name)=>new CharacterCard{Id=id,Name=name,Model="mp_m_freemode_01",BaseFace=new FaceHeritage{ShapeFirst=0,ShapeSecond=21,SkinFirst=0,SkinSecond=21}};
  CharacterCatalogue Read()=>CharacterCatalogue.Read(root,new CharacterFiles(root).Read);
  [Fact] public void MissingRootOpensCreationWithoutCreatingFiles(){Assert.True(Read().OpenCreation);Assert.False(Directory.Exists(root));}
  [Fact] public void LegacyProfilesDoNotBecomeSyntheticCharacters(){Directory.CreateDirectory(Path.Combine(root,"Franklin"));Directory.CreateDirectory(Path.Combine(root,"Default"));Directory.CreateDirectory(Path.Combine(root,"PSRP_Test"));Assert.True(Read().OpenCreation);Assert.Empty(Read().Characters);}
  [Fact] public void ValidCharactersSortedAndPendingLoadVisible(){var f=new CharacterFiles(root);f.Create(Card("RP_1111111111111111","Zeca"));var c=Card("RP_2222222222222222","Ana");f.Create(c);c.ApplyPending=true;f.Write(c);var list=Read();Assert.False(list.OpenCreation);Assert.Equal(new[]{"Ana","Zeca"},list.Characters.Select(x=>x.Name));Assert.True(list.Characters[0].ApplyPending);}
  [Fact] public void MalformedSlotProtectedAndDoesNotTriggerEmptyCreation(){string dir=Path.Combine(root,"RP_1111111111111111");Directory.CreateDirectory(dir);string path=Path.Combine(dir,"Character.json");File.WriteAllText(path,"{broken");var list=Read();Assert.False(list.OpenCreation);Assert.Single(list.ProtectedIds);Assert.Equal("{broken",File.ReadAllText(path));}
  [Fact] public void MissingCardProtectsExistingSlot(){Directory.CreateDirectory(Path.Combine(root,"RP_1111111111111111"));Assert.Single(Read().ProtectedIds);Assert.False(Read().OpenCreation);}
  [Fact] public void MismatchedIdentityCannotEnterCatalogue(){Directory.CreateDirectory(Path.Combine(root,"RP_1111111111111111"));var list=CharacterCatalogue.Read(root,id=>Card("RP_2222222222222222","Other"));Assert.Empty(list.Characters);Assert.Single(list.ProtectedIds);}
  [Fact] public void ReaderFailureIsIsolatedFromValidSlot(){var f=new CharacterFiles(root);f.Create(Card("RP_1111111111111111","Valid"));Directory.CreateDirectory(Path.Combine(root,"RP_2222222222222222"));var list=Read();Assert.Single(list.Characters);Assert.Single(list.ProtectedIds);}
  [Fact] public void ExistingCardsAreNotTruncatedAtCreationLimit(){var f=new CharacterFiles(root);for(int i=0;i<65;i++)f.Create(Card("RP_"+i.ToString("x16"),"Name "+i));Assert.Equal(65,Read().Characters.Count);}
  [Fact] public void StableSafeTimeRequiredAndOnlyOnce(){var g=new CharacterEntryGate();Assert.False(g.Advance(500,true));Assert.False(g.Advance(499,true));Assert.True(g.Advance(1,true));Assert.False(g.Advance(1000,true));}
  [Fact] public void UnsafeFrameResetsWait(){var g=new CharacterEntryGate();g.Advance(900,true);g.Advance(10,false);Assert.False(g.Advance(999,true));Assert.True(g.Advance(1,true));}
  [Theory][InlineData(-1)][InlineData(1001)][InlineData(int.MaxValue)] public void ClockJumpsCannotOpenEntry(int delta){var g=new CharacterEntryGate();g.Advance(900,true);Assert.False(g.Advance(delta,true));Assert.False(g.Advance(999,true));Assert.True(g.Advance(1,true));}
  [Fact] public void DisabledOrManualEntryConsumesAutomaticOpening(){var g=new CharacterEntryGate();g.Consume();Assert.False(g.Advance(1000,true));Assert.True(g.Consumed);}
 }
}
