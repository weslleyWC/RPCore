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

namespace RPCore
{
    // A loaded/faded-in frame is not proof that story scripts finished restoring their actor/location.
    public sealed class CharacterStartupStability
    {
        int age,stable,actor,model;string profile;float x,y,z;
        public bool Ready=>age>=30000 && stable>=10000;
        public void Observe(int elapsed,bool ready,int ped,int pedModel,string owner,float px,float py,float pz)
        {
            if(elapsed<0 || elapsed>1000){stable=0;return;}
            age=System.Math.Min(30000,age+elapsed);
            bool finite=!float.IsNaN(px) && !float.IsInfinity(px) && !float.IsNaN(py) && !float.IsInfinity(py) && !float.IsNaN(pz) && !float.IsInfinity(pz);
            double dx=(double)px-x,dy=(double)py-y,dz=(double)pz-z;
            bool same=ped!=0 && pedModel!=0 && !string.IsNullOrEmpty(owner) && actor==ped && model==pedModel && profile==owner && dx*dx+dy*dy+dz*dz<=625;
            actor=ped;model=pedModel;profile=owner;x=px;y=py;z=pz;
            stable=ready && finite && same?System.Math.Min(10000,stable+elapsed):0;
        }
    }
}
