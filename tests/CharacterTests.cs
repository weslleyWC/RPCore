using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace RPCore.Tests
{
    public sealed class CharacterTests : IDisposable
    {
        readonly string root=Path.Combine(Path.GetTempPath(),"rpcore-character-test-"+Guid.NewGuid().ToString("N"));
        const string Id="RP_1234567890abcdef";
        static CharacterCard Card()=>new CharacterCard{Id=Id,Name="Joao Teste",Model="mp_m_freemode_01",BaseFace=new FaceHeritage{ShapeFirst=0,ShapeSecond=21,SkinFirst=0,SkinSecond=21}};
        static CharacterBody Body()=>new CharacterBody{Model=1885233650,Health=175,MaxHealth=200,Armor=35,SelectedWeapon=CharacterRules.Unarmed,Components=Enumerable.Range(0,12).Select(i=>new WardrobePiece{Slot=i}).ToList(),Props=WardrobeRules.PropSlots.Select(i=>new WardrobePiece{Slot=i,Drawable=-1}).ToList()};
        public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
        [Fact] public void NewProfileHasZeroBalancesAndDoesNotCopyOrModifyExistingProgress()
        {Directory.CreateDirectory(Path.Combine(root,"Franklin"));string old=Path.Combine(root,"Franklin","Wallet.json");File.WriteAllText(old,"{\"Bank\":2000000,\"Future\":true}");var files=new CharacterFiles(root);files.Create(Card());var wallet=JsonConvert.DeserializeObject<WalletData>(File.ReadAllText(Path.Combine(root,Id,"Wallet.json")));Assert.Equal(0,wallet.Bank);Assert.Equal(0,wallet.Cash);Assert.Null(wallet.Housing);Assert.Null(wallet.Grooming);Assert.Equal("{\"Bank\":2000000,\"Future\":true}",File.ReadAllText(old));Assert.Null(files.Read(Id).Body);Assert.False(files.Read(Id).ApplyPending);Assert.Empty(Directory.GetDirectories(root,".character-*"));}
        [Fact] public void ExistingFolderIsNeverOverwrittenEvenWithoutCharacterJson()
        {Directory.CreateDirectory(Path.Combine(root,Id));string p=Path.Combine(root,Id,"custom.json");File.WriteAllText(p,"keep");Assert.Throws<IOException>(()=>new CharacterFiles(root).Create(Card()));Assert.Equal("keep",File.ReadAllText(p));Assert.False(File.Exists(Path.Combine(root,Id,"Character.json")));}
        [Fact] public void DuplicateCreateCannotResetWalletOrBody()
        {var f=new CharacterFiles(root);f.Create(Card());File.WriteAllText(Path.Combine(root,Id,"Wallet.json"),"{\"Bank\":1234}");Assert.Throws<IOException>(()=>f.Create(Card()));Assert.Equal("{\"Bank\":1234}",File.ReadAllText(Path.Combine(root,Id,"Wallet.json")));}
        [Theory][InlineData(null)][InlineData("")][InlineData("Franklin")][InlineData("Default")][InlineData("../Franklin")][InlineData("RP_1234567890abcdeg")][InlineData("RP_1234567890ABCDEf")][InlineData("RP_1234/67890abcdef")]
        public void SlotIdsCannotAddressOldProfilesOrEscapeSaveRoot(string id)
        {Assert.False(CharacterRules.ValidId(id));var c=Card();c.Id=id;Assert.False(c.Valid);Assert.ThrowsAny<Exception>(()=>new CharacterFiles(root).Create(c));Assert.False(Directory.Exists(root));}
        [Theory][InlineData(null)][InlineData("")][InlineData(" ")][InlineData(" Joao")][InlineData("Joao ")][InlineData("Nome\nOutro")][InlineData("Nome\tOutro")]
        public void InvalidNamesCannotCreateCharacter(string name)
        {var c=Card();c.Name=name;Assert.False(c.Valid);Assert.Throws<InvalidDataException>(()=>new CharacterFiles(root).Create(c));}
        [Fact] public void UnicodeDisplayNameDoesNotBecomeFilesystemPath()
        {var c=Card();c.Name="Joao / Maria: acao e coracao ♥";Assert.True(c.Valid);var f=new CharacterFiles(root);f.Create(c);Assert.Equal(c.Name,f.Read(Id).Name);Assert.Single(Directory.GetDirectories(root));c.Name=new string('a',41);Assert.False(c.Valid);}
        [Theory][InlineData("mp_m_freemode_01",1885233650)][InlineData("mp_f_freemode_01",-1667301416)]
        public void BothWhitelistedFreemodeModelsHavePinnedSdkHashes(string model,int hash)
        {Assert.Equal(hash,CharacterRules.ModelHash(model));var c=Card();c.Model=model;Assert.True(c.Valid);Assert.True(CharacterRules.CanTransition(Id,hash));}
        [Theory][InlineData("player_one")][InlineData("s_m_y_cop_01")][InlineData("MP_M_FREEMODE_01")][InlineData(null)]
        public void CustomPedModelInjectionIsRejected(string model)
        {var c=Card();c.Model=model;Assert.False(c.Valid);Assert.Equal(0,CharacterRules.ModelHash(model));}
        [Fact] public void ProtagonistsOnlyMatchTheirOwnModelsAndNeverFreemodeIds()
        {Assert.True(CharacterRules.CanTransition("Michael",225514697));Assert.True(CharacterRules.CanTransition("Franklin",unchecked((int)2602752943)));Assert.True(CharacterRules.CanTransition("Trevor",unchecked((int)2608926626)));Assert.False(CharacterRules.CanTransition("Franklin",1885233650));Assert.False(CharacterRules.CanTransition(Id,225514697));Assert.False(CharacterRules.CanTransition("Default",1885233650));Assert.False(CharacterRules.CanTransition("Michael",0));}
        [Fact] public void BodyHasAllTwelveComponentsAndFivePropsIncludingHairAndHead()
        {var b=Body();Assert.True(b.Valid);Assert.Contains(b.Components,p=>p.Slot==0);Assert.Contains(b.Components,p=>p.Slot==2);b.Components.RemoveAt(0);Assert.False(b.Valid);b=Body();b.Props.RemoveAt(0);Assert.False(b.Valid);b=Body();b.Components[0].Slot=1;Assert.False(b.Valid);b=Body();b.Props[0].Drawable=-2;Assert.False(b.Valid);}
        [Fact] public void InvalidHealthOrArmorNeverProducesLoadableSnapshot()
        {var b=Body();b.Health=0;Assert.False(b.Valid);b=Body();b.Health=201;Assert.False(b.Valid);b=Body();b.MaxHealth=int.MaxValue;Assert.False(b.Valid);b=Body();b.Armor=-1;Assert.False(b.Valid);}
        [Fact] public void WeaponsUseOneSharedAmmoPoolAndPreserveClipComponentsAndTint()
        {var b=Body();b.Weapons.Add(new CharacterWeapon{Hash=123,Tint=3,Clip=12,AmmoType=456,Components={new CharacterAttachment{Hash=789,Tint=4}}});b.Weapons.Add(new CharacterWeapon{Hash=124,Tint=1,Clip=8,AmmoType=456});b.Ammo[456]=100;b.SelectedWeapon=123;Assert.True(b.Valid);var round=CharacterRules.Copy(b);Assert.Single(round.Ammo);Assert.Equal(100,round.Ammo[456]);Assert.Equal(12,round.Weapons[0].Clip);Assert.Equal(4,round.Weapons[0].Components[0].Tint);Assert.Equal(123,round.SelectedWeapon);}
        [Fact] public void MissingAmmoPoolAndDuplicateWeaponOrAttachmentAreInvalid()
        {var b=Body();var w=new CharacterWeapon{Hash=123,AmmoType=456};b.Weapons.Add(w);Assert.False(b.Valid);b.Ammo[456]=100;Assert.True(b.Valid);b.Weapons.Add(new CharacterWeapon{Hash=123});Assert.False(b.Valid);b.Weapons.RemoveAt(1);w.Components.Add(new CharacterAttachment{Hash=7});w.Components.Add(new CharacterAttachment{Hash=7});Assert.False(b.Valid);}
        [Fact] public void CorruptResourceValuesOrUnknownSelectedWeaponCannotLoad()
        {var b=Body();b.SelectedWeapon=123;Assert.False(b.Valid);b=Body();b.Components[0].Drawable=-1;Assert.False(b.Valid);b=Body();b.Weapons.Add(new CharacterWeapon{Hash=123,Tint=-1});Assert.False(b.Valid);b=Body();b.Ammo[1]=int.MaxValue;Assert.False(b.Valid);}
        [Fact] public void BodyCannotCrossModelsAndPendingLoadBlocksAutosaveOverwrite()
        {var c=Card();c.Body=Body();Assert.True(c.Valid);Assert.True(CharacterRules.CanCapture(c,Id,1885233650,true,false));c.ApplyPending=true;Assert.False(CharacterRules.CanCapture(c,Id,1885233650,true,false));c.ApplyPending=false;Assert.False(CharacterRules.CanCapture(c,"Franklin",1885233650,true,false));Assert.False(CharacterRules.CanCapture(c,Id,1885233650,false,false));Assert.False(CharacterRules.CanCapture(c,Id,1885233650,true,true));c.Body.Model=42;Assert.False(c.Valid);}
        [Fact] public void PendingSnapshotAndUnknownFieldsSurviveReloadAndAtomicWrites()
        {var f=new CharacterFiles(root);f.Create(Card());var c=f.Read(Id);c.Body=Body();c.ApplyPending=true;var j=JObject.FromObject(c);j["Future"]=1;j["Body"]["Future"]=2;j["BaseFace"]["Future"]=3;c=j.ToObject<CharacterCard>();f.Write(c);c=f.Read(Id);Assert.True(c.ApplyPending);Assert.Equal(175,c.Body.Health);j=JObject.FromObject(c);Assert.Equal(1,(int)j["Future"]);Assert.Equal(2,(int)j["Body"]["Future"]);Assert.Equal(3,(int)j["BaseFace"]["Future"]);Assert.True(File.Exists(Path.Combine(root,Id,"Character.json.bak")));}
        [Fact] public void FailedBodyCaptureCannotReplaceOldSnapshot()
        {var f=new CharacterFiles(root);f.Create(Card());var c=f.Read(Id);string before=File.ReadAllText(Path.Combine(root,Id,"Character.json"));c.Body=Body();c.Body.Health=-1;Assert.Throws<InvalidDataException>(()=>f.Write(c));Assert.Equal(before,File.ReadAllText(Path.Combine(root,Id,"Character.json")));}
        [Fact] public void DamagedReadProtectsItsCardAgainstLaterDefaultsWrite()
        {var f=new CharacterFiles(root);f.Create(Card());string p=Path.Combine(root,Id,"Character.json");File.WriteAllText(p,"{");Assert.ThrowsAny<Exception>(()=>f.Read(Id));Assert.Throws<IOException>(()=>f.Write(Card()));Assert.Equal("{",File.ReadAllText(p));}
        [Fact] public void MisidentifiedCardCannotOverwriteAnotherProfile()
        {var f=new CharacterFiles(root);f.Create(Card());string p=Path.Combine(root,Id,"Character.json");var j=JObject.Parse(File.ReadAllText(p));j["Id"]="RP_aaaaaaaaaaaaaaaa";File.WriteAllText(p,j.ToString());Assert.Throws<InvalidDataException>(()=>f.Read(Id));Assert.Throws<IOException>(()=>f.Write(Card()));}
        [Fact] public void ProtagonistReturnRequiresMatchingModelAndKeepsRecordedEquipment()
        {var r=new CharacterReturn{Profile="Franklin",Body=Body()};Assert.False(r.Valid);r.Body.Model=unchecked((int)2602752943);Assert.True(r.Valid);var copy=CharacterRules.Copy(r);Assert.Equal(35,copy.Body.Armor);Assert.Equal("Franklin",copy.Profile);r.Profile="RP_1234567890abcdef";Assert.False(r.Valid);}
        [Fact] public void ReadbackMismatchCannotBeReportedAsRestoredEquipment()
        {var original=Body();var actual=CharacterRules.Copy(original);Assert.True(CharacterRules.Restored(original,actual));actual.Health--;Assert.False(CharacterRules.Restored(original,actual));actual=CharacterRules.Copy(original);actual.Components[2].Texture++;Assert.False(CharacterRules.Restored(original,actual));actual=CharacterRules.Copy(original);actual.Weapons.Add(new CharacterWeapon{Hash=123});Assert.False(CharacterRules.Restored(original,actual));}
        [Fact] public void ReadbackChecksSharedAmmoAndWeaponAttachmentsWithoutDependingOnOrder()
        {var original=Body();original.Weapons.Add(new CharacterWeapon{Hash=123,Clip=4,AmmoType=456,Components={new CharacterAttachment{Hash=789,Tint=3}}});original.Ammo[456]=50;var actual=CharacterRules.Copy(original);actual.Components.Reverse();Assert.True(CharacterRules.Restored(original,actual));actual.Ammo[456]=51;Assert.False(CharacterRules.Restored(original,actual));actual.Ammo[456]=50;actual.Weapons[0].Components[0].Tint=4;Assert.False(CharacterRules.Restored(original,actual));}
        [Fact] public void RefreshingSnapshotPreservesUnknownBodyPieceWeaponAndAttachmentFields()
        {var old=Body();old.Weapons.Add(new CharacterWeapon{Hash=123,Components={new CharacterAttachment{Hash=789}}});var j=JObject.FromObject(old);j["Future"]=1;j["Components"][0]["Future"]=2;j["Weapons"][0]["Future"]=3;j["Weapons"][0]["Components"][0]["Future"]=4;old=j.ToObject<CharacterBody>();var actual=Body();actual.Health=160;actual.Weapons.Add(new CharacterWeapon{Hash=123,Components={new CharacterAttachment{Hash=789}}});var next=CharacterRules.MergeBody(old,actual);j=JObject.FromObject(next);Assert.Equal(1,(int)j["Future"]);Assert.Equal(2,(int)j["Components"][0]["Future"]);Assert.Equal(3,(int)j["Weapons"][0]["Future"]);Assert.Equal(4,(int)j["Weapons"][0]["Components"][0]["Future"]);Assert.Equal(160,next.Health);Assert.Equal(175,old.Health);}
        [Fact] public void ReturnPendingFlagPersistsUntilSuccessfulRestoration()
        {var r=new CharacterReturn{Profile="Michael",Body=Body(),ApplyPending=true};r.Body.Model=225514697;var copy=CharacterRules.Copy(r);Assert.True(copy.Valid);Assert.True(copy.ApplyPending);Assert.Equal(175,copy.Body.Health);}
        [Fact] public void NewSlotRejectsSuppliedEquipmentOrPendingState()
        {var c=Card();c.Body=Body();Assert.Throws<InvalidDataException>(()=>new CharacterFiles(root).Create(c));c=Card();c.ApplyPending=true;Assert.Throws<InvalidDataException>(()=>new CharacterFiles(root).Create(c));Assert.False(Directory.Exists(root));}
    }
}
