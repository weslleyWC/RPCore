using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace RPCore
{
    public sealed class CharacterCatalogue
    {
        public readonly List<CharacterCard> Characters=new List<CharacterCard>();
        public readonly List<string> ProtectedIds=new List<string>();
        public bool ScanFailed;
        public bool OpenCreation=>!ScanFailed && Characters.Count==0 && ProtectedIds.Count==0;
        public static CharacterCatalogue Read(string root,Func<string,CharacterCard> read,Action<string,Exception> report=null)
        {
            var result=new CharacterCatalogue();
            try{
                if(!Directory.Exists(root))return result;
                foreach(string directory in Directory.GetDirectories(root).OrderBy(p=>p,StringComparer.Ordinal)){
                    string id=Path.GetFileName(directory);if(!CharacterRules.ValidId(id))continue;
                    try{var card=read(id);if(card==null || !card.Valid || card.Id!=id)throw new InvalidDataException("Invalid character slot.");result.Characters.Add(card);}
                    catch(Exception ex){result.ProtectedIds.Add(id);report?.Invoke(id,ex);}
                }
                result.Characters.Sort((a,b)=>{int name=StringComparer.OrdinalIgnoreCase.Compare(a.Name,b.Name);return name!=0?name:StringComparer.Ordinal.Compare(a.Id,b.Id);});
            }catch(Exception ex){result.ScanFailed=true;report?.Invoke("catalogue",ex);}
            return result;
        }
    }
    public sealed class CharacterEntryGate
    {
        int readyTime;
        public bool Consumed{get;private set;}
        public void Consume(){Consumed=true;}
        public bool Advance(int elapsed,bool ready)
        {
            if(Consumed)return false;
            if(!ready || elapsed<0 || elapsed>1000){readyTime=0;return false;}
            readyTime+=elapsed;if(readyTime<1000)return false;Consumed=true;return true;
        }
    }
}
