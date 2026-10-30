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
        public bool Commit(int player,int model){if(HandedOff)return false;if(player!=Handle || model!=Model || Handle==0 || Model==0 || Token<=0)return false;HandedOff=true;return true;}
    }
}
