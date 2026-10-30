using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class CharacterAttachment : ExtensibleData { public int Hash,Tint=-1; }
    public sealed class CharacterWeapon : ExtensibleData
    {
        public int Hash,Tint,Clip,AmmoType;
        public List<CharacterAttachment> Components=new List<CharacterAttachment>();
        [JsonIgnore] public bool Valid=>Hash!=0 && Tint>=0 && Tint<=255 && Clip>=0 && Clip<=1000000 && Components!=null && Components.Count<=128 && Components.All(c=>c!=null && c.Hash!=0 && c.Tint>=-1 && c.Tint<=255) && Components.Select(c=>c.Hash).Distinct().Count()==Components.Count;
    }
    public sealed class CharacterBody : ExtensibleData
    {
        public int Model,Health,MaxHealth,Armor,SelectedWeapon;
        public List<WardrobePiece> Components=new List<WardrobePiece>(),Props=new List<WardrobePiece>();
        public List<CharacterWeapon> Weapons=new List<CharacterWeapon>();
        public Dictionary<int,int> Ammo=new Dictionary<int,int>();
        [JsonIgnore] public bool Valid=>Model!=0 && Health>0 && Health<=MaxHealth && MaxHealth>=100 && MaxHealth<=10000 && Armor>=0 && Armor<=10000 && Components!=null && Components.Count==12 && Components.All(c=>c!=null && c.Slot>=0 && c.Slot<12 && c.Drawable>=0 && c.Drawable<=10000 && c.Texture>=0 && c.Texture<=10000 && c.Palette>=0 && c.Palette<=3) && Components.Select(c=>c.Slot).Distinct().Count()==12
            && Props!=null && Props.Count==5 && Props.All(p=>p!=null && WardrobeRules.PropSlots.Contains(p.Slot) && p.Drawable>=-1 && p.Drawable<=10000 && p.Texture>=0 && p.Texture<=10000 && p.Palette==0 && (p.Drawable>=0 || p.Texture==0)) && Props.Select(p=>p.Slot).Distinct().Count()==5
            && Weapons!=null && Weapons.Count<=256 && Weapons.All(w=>w!=null && w.Valid) && Weapons.Select(w=>w.Hash).Distinct().Count()==Weapons.Count && (SelectedWeapon==CharacterRules.Unarmed || Weapons.Any(w=>w.Hash==SelectedWeapon)) && Ammo!=null && Ammo.Count<=256 && Ammo.All(a=>a.Key!=0 && a.Value>=0 && a.Value<=1000000) && Weapons.All(w=>w.AmmoType==0 || Ammo.ContainsKey(w.AmmoType));
    }
    public sealed class CharacterCard : ExtensibleData
    {
        public string Id,Name,Model;
        public FaceHeritage BaseFace;
        public CharacterBody Body;
        public bool ApplyPending;
        [JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)] public long BodyClothingRevision;
        [JsonIgnore] public bool Valid=>BodyClothingRevision>=0 && CharacterRules.ValidId(Id) && CharacterRules.ValidName(Name) && CharacterRules.ModelHash(Model)!=0 && BaseFace!=null && BaseFace.Valid && (Body==null || Body.Valid && Body.Model==CharacterRules.ModelHash(Model));
    }
    public sealed class CharacterReturn : ExtensibleData
    {
        public string Profile;
        public bool ApplyPending;
        public CharacterBody Body;
        [JsonIgnore] public bool Valid=>Body!=null && Body.Valid && CharacterRules.ProtagonistHash(Profile)!=0 && Body.Model==CharacterRules.ProtagonistHash(Profile);
    }
    public static class CharacterRules
    {
        public const int Unarmed=unchecked((int)0xA2719263);
        public static int ModelHash(string model)=>model=="mp_m_freemode_01"?1885233650:model=="mp_f_freemode_01"?unchecked((int)2627665880):0;
        public static int ProtagonistHash(string id)=>id=="Michael"?225514697:id=="Franklin"?unchecked((int)2602752943):id=="Trevor"?unchecked((int)2608926626):0;
        public static bool ValidId(string id)=>id!=null && id.Length==19 && id.StartsWith("RP_",StringComparison.Ordinal) && id.Skip(3).All(c=>c>='0' && c<='9' || c>='a' && c<='f');
        public static bool ValidName(string name)=>!string.IsNullOrWhiteSpace(name) && name.Length<=40 && name==name.Trim() && !name.Any(char.IsControl);
        public static bool CanTransition(string profile,int model)=>ProtagonistHash(profile)!=0?ProtagonistHash(profile)==model:ValidId(profile) && (model==ModelHash("mp_m_freemode_01") || model==ModelHash("mp_f_freemode_01"));
        public static bool SameBodyOwner(CharacterCard card,string profile,int model)=>card!=null && card.Valid && card.Id==profile && ModelHash(card.Model)==model;
        public static bool AppearanceInterrupted(bool swapped,bool same,bool safe,bool visible,bool cancelled,uint elapsed)=>!same || cancelled || !swapped && (!safe || !visible) || elapsed>=10000;
        public static bool CanCapture(CharacterCard card,string profile,int model,bool alive,bool applying)=>SameBodyOwner(card,profile,model) && alive && !applying && !card.ApplyPending;
        public static CharacterBody MergeBody(CharacterBody old,CharacterBody actual)
        {
            if(actual==null || !actual.Valid || old!=null && (!old.Valid || old.Model!=actual.Model))throw new ArgumentException("Invalid body merge.");
            var next=Copy(actual);if(old==null)return next;
            next.Extra=Copy(old.Extra);
            foreach(var piece in next.Components)piece.Extra=Copy(old.Components.Find(p=>p.Slot==piece.Slot)?.Extra);
            foreach(var piece in next.Props)piece.Extra=Copy(old.Props.Find(p=>p.Slot==piece.Slot)?.Extra);
            foreach(var weapon in next.Weapons){var previous=old.Weapons.Find(w=>w.Hash==weapon.Hash);if(previous==null)continue;weapon.Extra=Copy(previous.Extra);foreach(var attachment in weapon.Components)attachment.Extra=Copy(previous.Components.Find(c=>c.Hash==attachment.Hash)?.Extra);}
            return next;
        }
        public static bool Restored(CharacterBody expected,CharacterBody actual)
        {
            if(expected==null || actual==null || !expected.Valid || !actual.Valid || expected.Model!=actual.Model || expected.Health!=actual.Health || expected.MaxHealth!=actual.MaxHealth || expected.Armor!=actual.Armor || expected.SelectedWeapon!=actual.SelectedWeapon)return false;
            bool pieces=expected.Components.All(e=>actual.Components.Any(a=>a.Slot==e.Slot && a.Drawable==e.Drawable && a.Texture==e.Texture && a.Palette==e.Palette)) && expected.Props.All(e=>actual.Props.Any(a=>a.Slot==e.Slot && a.Drawable==e.Drawable && a.Texture==e.Texture));
            return pieces && expected.Weapons.Count==actual.Weapons.Count && expected.Weapons.All(e=>actual.Weapons.Any(a=>a.Hash==e.Hash && a.Tint==e.Tint && a.Clip==e.Clip && a.AmmoType==e.AmmoType && a.Components.Count==e.Components.Count && e.Components.All(c=>a.Components.Any(d=>d.Hash==c.Hash && (c.Tint<0 || d.Tint==c.Tint))))) && expected.Ammo.Count==actual.Ammo.Count && expected.Ammo.All(e=>actual.Ammo.TryGetValue(e.Key,out var value) && value==e.Value);
        }
        public static T Copy<T>(T value)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
    }
    // A new slot is assembled beside the destination and moved atomically; never overwrite any save directory.
    public sealed class CharacterFiles
    {
        readonly string root;readonly JsonFiles files=new JsonFiles();
        public CharacterFiles(string root){this.root=root;}
        string PathFor(string id){if(!CharacterRules.ValidId(id))throw new ArgumentException("Invalid character slot.");return Path.Combine(root,id,"Character.json");}
        public CharacterCard Read(string id)
        {var card=files.Read<CharacterCard>(PathFor(id),()=>null);if(card==null || !card.Valid || card.Id!=id){files.Protect(PathFor(id));throw new InvalidDataException("Invalid character card; preserved.");}return card;}
        public void Write(CharacterCard card)
        {if(card==null || !card.Valid)throw new InvalidDataException("Invalid character card.");files.Write(PathFor(card.Id),card);}
        public void Create(CharacterCard card)
        {
            if(card==null || !card.Valid || card.Body!=null || card.ApplyPending)throw new InvalidDataException("Invalid new character.");
            Directory.CreateDirectory(root);string target=Path.Combine(root,card.Id);if(Directory.Exists(target) || File.Exists(target))throw new IOException("Slot already exists.");
            string stage=Path.Combine(root,".character-"+Guid.NewGuid().ToString("N"));
            try{Directory.CreateDirectory(stage);files.Write(Path.Combine(stage,"Character.json"),card);files.Write(Path.Combine(stage,"Wallet.json"),new WalletData());Directory.Move(stage,target);}
            finally{if(Directory.Exists(stage))Directory.Delete(stage,true);}
        }
    }
}
