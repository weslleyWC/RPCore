using System;
using GTA;
using GTA.Math;
using GTA.Native;
namespace RPCore
{
    internal static class CharacterHandoffRuntime
    {
        public static int PlayerHandle=>Function.Call<int>(Hash.GET_PLAYER_PED,Game.Player.Handle);
        const string Decor="RPCoreCharPed";
        public static CharacterHandoffLease Create(int model,Vector3 position,float heading)
        {
            if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);
            if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))throw new InvalidOperationException("Candidate ownership tag unavailable.");
            int handle=Function.Call<int>(Hash.CREATE_PED,model==CharacterRules.ModelHash("mp_f_freemode_01")?5:4,model,position.X,position.Y,position.Z,heading,false,false);
            if(handle==0 || handle==PlayerHandle)throw new InvalidOperationException("Candidate ped was not created.");
            var actor=Entity.FromHandle(handle) as Ped;int token=Math.Max(1,Guid.NewGuid().GetHashCode() & int.MaxValue);
            var lease=new CharacterHandoffLease(handle,model,token);
            // Tag immediately; later cleanup needs handle + model + token and excludes the player.
            if(actor==null || !actor.Exists() || actor.Model.Hash!=model || !Function.Call<bool>(Hash.DECOR_SET_INT,actor,Decor,token)){
                // The just-created handle has not crossed a frame. Never delete an unproven/reused entity later.
                if(actor!=null && actor.Exists() && actor.Model.Hash==model && actor.Handle!=PlayerHandle)actor.MarkAsNoLongerNeeded();
                throw new InvalidOperationException("Cannot prove candidate ownership.");
            }
            try{
                actor.IsPersistent=true;actor.IsVisible=false;actor.IsCollisionEnabled=false;actor.IsPositionFrozen=true;actor.IsInvincible=true;actor.BlockPermanentEvents=true;
                Function.Call(Hash.SET_PED_DEFAULT_COMPONENT_VARIATION,actor);
                return lease;
            }catch{Discard(lease);throw;}
        }
        public static bool Own(CharacterHandoffLease lease)
        {
            if(lease==null)return false;var p=Entity.FromHandle(lease.Handle) as Ped;
            return p!=null && p.Exists() && p.Model.Hash==lease.Model && Function.Call<bool>(Hash.DECOR_EXIST_ON,p,Decor) && lease.Matches(p.Handle,p.Model.Hash,Function.Call<int>(Hash.DECOR_GET_INT,p,Decor));
        }
        public static void ReleaseStagingFlags(CharacterHandoffLease lease)
        {
            if(lease==null || !lease.HandedOff || PlayerHandle!=lease.Handle)return;
            var p=Entity.FromHandle(lease.Handle) as Ped;
            if(p==null || !p.Exists())return;
            // One failed restore must not leave the remaining staging overrides enabled.
            foreach(Action restore in new Action[]{()=>p.IsVisible=true,()=>p.IsCollisionEnabled=true,()=>p.IsPositionFrozen=false,()=>p.IsInvincible=false,()=>p.BlockPermanentEvents=false})
                try{restore();}catch(Exception ex){Log.Error("character staging restore",ex);}
        }
        public static void Discard(CharacterHandoffLease lease)
        {
            if(lease==null || lease.HandedOff || !Own(lease))return;var p=Entity.FromHandle(lease.Handle) as Ped;
            if(p==null || !p.Exists())return;
            int current=PlayerHandle,token=Function.Call<int>(Hash.DECOR_GET_INT,p,Decor);
            if(!lease.CanDiscard(p.Handle,p.Model.Hash,token,current))return;
            using(var arg=OutputArgument.Init((ulong)(uint)p.Handle))Function.Call(Hash.DELETE_PED,arg);
        }
    }
}
