namespace RPCore
{
    // Ownership is independent of profile data. A live player is never a disposable candidate.
    public sealed class CharacterHandoffLease
    {
        public readonly int Handle,Model,Token;
        public bool HandedOff{get;private set;}
        public CharacterHandoffLease(int handle,int model,int token){Handle=handle;Model=model;Token=token;}
        public bool Matches(int handle,int model,int token)=>Handle!=0 && Model!=0 && Token>0 && Handle==handle && Model==model && Token==token;
        public bool CanDiscard(int handle,int model,int token,int currentPlayer)=>!HandedOff && currentPlayer!=Handle && Matches(handle,model,token);
        public bool CanReleaseStaging(int player,int model)=>HandedOff && Handle==player && Model==model;
        public bool Commit(int player,int model){if(HandedOff)return false;if(player!=Handle || model!=Model || Handle==0 || Model==0 || Token<=0)return false;HandedOff=true;return true;}
    }
}

namespace RPCore
{
    // Session-only proof, populated exclusively immediately after CREATE_PED returns.
    // No decor registration or adoption of existing entities is needed.
    public sealed class CharacterHandoffRegistry
    {
        readonly System.Collections.Generic.Dictionary<CharacterHandoffLease,long> entries=new System.Collections.Generic.Dictionary<CharacterHandoffLease,long>();
        public CharacterHandoffLease Register(int handle,int model,int token,long address)
        {
            if(handle==0 || model==0 || token<=0 || address==0)throw new System.ArgumentException("Invalid candidate identity.");
            var lease=new CharacterHandoffLease(handle,model,token);entries.Add(lease,address);return lease;
        }
        public bool Owns(CharacterHandoffLease lease,int handle,int model,long address)
            =>lease!=null && address!=0 && entries.TryGetValue(lease,out long expected) && expected==address && lease.Matches(handle,model,lease.Token);
        public void Forget(CharacterHandoffLease lease){if(lease!=null)entries.Remove(lease);}
    }
}
