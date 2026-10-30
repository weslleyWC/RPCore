using System;
using GTA;
using GTA.Math;
using GTA.Native;
namespace RPCore
{
    internal static class CharacterHandoffRuntime
    {
        public static int PlayerHandle=>Function.Call<int>(Hash.GET_PLAYER_PED,Game.Player.Handle);
        static readonly CharacterHandoffRegistry registry=new CharacterHandoffRegistry();
        public static CharacterHandoffLease Create(int model,Vector3 position,float heading)
        {
            int handle=Function.Call<int>(Hash.CREATE_PED,model==CharacterRules.ModelHash("mp_f_freemode_01")?5:4,model,position.X,position.Y,position.Z,heading,false,false);
            if(handle==0 || handle==PlayerHandle)throw new InvalidOperationException("Candidate ped was not created.");
            var actor=Entity.FromHandle(handle) as Ped;
            if(actor==null || !actor.Exists() || actor.Model.Hash!=model || actor.MemoryAddress==IntPtr.Zero){
                if(actor!=null && actor.Exists() && actor.Model.Hash==model && actor.Handle!=PlayerHandle)actor.MarkAsNoLongerNeeded();
                throw new InvalidOperationException("Cannot prove newly created candidate identity.");
            }
            // Retain exact lease object + handle + model + pool address. Never attach to an old ped by searching.
            var lease=registry.Register(handle,model,Math.Max(1,Guid.NewGuid().GetHashCode() & int.MaxValue),actor.MemoryAddress.ToInt64());
            try{
                actor.IsPersistent=true;actor.IsVisible=false;actor.IsCollisionEnabled=false;actor.IsPositionFrozen=true;actor.IsInvincible=true;actor.BlockPermanentEvents=true;
                Function.Call(Hash.SET_PED_DEFAULT_COMPONENT_VARIATION,actor);
                return lease;
            }catch{Discard(lease);throw;}
        }
        public static bool Own(CharacterHandoffLease lease)
        {
            if(lease==null)return false;var p=Entity.FromHandle(lease.Handle) as Ped;
            return p!=null && p.Exists() && registry.Owns(lease,p.Handle,p.Model.Hash,p.MemoryAddress.ToInt64());
        }
        static void RestoreFlags(Ped p)
        {
            foreach(Action restore in new Action[]{()=>p.IsVisible=true,()=>p.IsCollisionEnabled=true,()=>p.IsPositionFrozen=false,()=>p.IsInvincible=false,()=>p.BlockPermanentEvents=false})
                try{restore();}catch(Exception ex){Log.Error("character staging restore",ex);}
        }
        public static void ReleaseStagingFlags(CharacterHandoffLease lease)
        {
            if(lease==null || !lease.HandedOff)return;
            try{var p=Entity.FromHandle(PlayerHandle) as Ped;if(p!=null && p.Exists() && lease.CanReleaseStaging(p.Handle,p.Model.Hash))RestoreFlags(p);}
            finally{registry.Forget(lease);}
        }
        public static void Discard(CharacterHandoffLease lease)
        {
            if(lease==null)return;
            try{
                if(lease.HandedOff || !Own(lease))return;var p=Entity.FromHandle(lease.Handle) as Ped;
                if(p==null || !p.Exists() || !lease.CanDiscard(p.Handle,p.Model.Hash,lease.Token,PlayerHandle))return;
                using(var arg=OutputArgument.Init((ulong)(uint)p.Handle))Function.Call(Hash.DELETE_PED,arg);
            }
            finally{
                // If removal was declined, release only this proven, uncontrolled temporary entity.
                try{if(!lease.HandedOff && Own(lease) && PlayerHandle!=lease.Handle){var p=Entity.FromHandle(lease.Handle) as Ped;if(p!=null && p.Exists()){RestoreFlags(p);p.MarkAsNoLongerNeeded();}}}
                finally{registry.Forget(lease);}
            }
        }
    }
}
