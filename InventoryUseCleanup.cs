using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;
namespace RPCore
{
    public partial class InventorySystem
    {
        sealed class UsePropLease { public int Handle,Token,Model; }
        readonly List<UsePropLease> retiredUseProps=new List<UsePropLease>();
        UsePropLease activeUseProp;
        bool recoveredUseProps;int cleanupNext;
        bool PrepareUseDecor()
        {
            if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,UseDecor,3))Function.Call(Hash.DECOR_REGISTER,UseDecor,3);
            return Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,UseDecor,3);
        }
        void RetireUseProp()
        {
            // Keep the original handle: SHVDN Delete can invalidate its wrapper before the game
            // actually removes the entity. Never drop the retry record just because Delete returned.
            if(activeUseProp!=null)retiredUseProps.Add(activeUseProp);
            activeUseProp=null;useProp=null;cleanupNext=Game.GameTime;CleanupUseProps();
        }
        void CleanupUseProps()
        {
            if(unchecked(Game.GameTime-cleanupNext)<0)return;cleanupNext=Game.GameTime+250;
            try
            {
                if(!recoveredUseProps)
                {
                    recoveredUseProps=true;
                    // Recover only RPCore-tagged leftovers after script reload. Never search by
                    // nearby model name: another script or the world may own identical objects.
                    foreach(var prop in World.GetAllProps())
                        if(Function.Call<bool>(Hash.DECOR_EXIST_ON,prop,UseDecor))
                            retiredUseProps.Add(new UsePropLease{Handle=prop.Handle,Token=Function.Call<int>(Hash.DECOR_GET_INT,prop,UseDecor),Model=prop.Model.Hash});
                }
                for(int i=retiredUseProps.Count-1;i>=0;i--)
                {
                    var lease=retiredUseProps[i];int h=lease.Handle;
                    if(!Function.Call<bool>(Hash.DOES_ENTITY_EXIST,h) || Function.Call<int>(Hash.GET_ENTITY_MODEL,h)!=lease.Model || !Function.Call<bool>(Hash.DECOR_EXIST_ON,h,UseDecor) || Function.Call<int>(Hash.DECOR_GET_INT,h,UseDecor)!=lease.Token){retiredUseProps.RemoveAt(i);continue;}
                    Function.Call(Hash.DETACH_ENTITY,h,true,true);
                    Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY,h,true,true);
                    var handle=new OutputArgument(h);Function.Call(Hash.DELETE_OBJECT,handle);
                    if(!Function.Call<bool>(Hash.DOES_ENTITY_EXIST,h))retiredUseProps.RemoveAt(i);
                }
            }
            catch(Exception ex){Log.Error("inventory owned prop retry",ex);}
        }
    }
}
