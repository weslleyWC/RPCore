using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;

namespace RPCore
{
    public sealed class ClothingDoorRuntime
    {
        readonly JobPoint[] entrances=ClothingDoorRules.Entrances();
        readonly int[] models=ClothingDoorRules.Models();
        bool reported;
        public void Tick(IEnumerable<JobPoint> shops)
        {
            // Recheck each frame because the base game's shop script can relock streamed doors.
            // No persistent lock state, door registrations, global unlock or forced opening angle.
            if(!PersonalServiceMenu.SafeFoot)return;
            var position=Game.Player.Character.Position;
            try
            {
                foreach(var entrance in entrances)
                {
                    if(!ClothingDoorRules.Near(entrance,position.X,position.Y,position.Z) || !ClothingDoorRules.Active(entrance,shops))continue;
                    foreach(int model in models)
                    // Enhanced does not accept boxed bool/float in OutputArgument(object).
                    // Use eight initialized bytes: BOOL natives may write only the low DWORD.
                    using(var locked=OutputArgument.Init(0UL))
                    using(var angle=OutputArgument.Init(0UL))
                    {
                        using(var existing=OutputArgument.Init(0UL))
                        {
                            if(Function.Call<bool>(Hash.DOOR_SYSTEM_FIND_EXISTING_DOOR,entrance.X,entrance.Y,entrance.Z,model,existing))
                            {
                                int door=existing.GetResult<int>();
                                if(Function.Call<int>(Hash.DOOR_SYSTEM_GET_DOOR_STATE,door)!=0)
                                    Function.Call(Hash.DOOR_SYSTEM_SET_DOOR_STATE,door,0,false,true);
                            }
                        }
                        Function.Call(Hash.GET_STATE_OF_CLOSEST_DOOR_OF_TYPE,model,entrance.X,entrance.Y,entrance.Z,locked,angle);
                        float current=angle.GetResult<float>();
                        if(locked.GetResult<bool>() && !float.IsNaN(current) && !float.IsInfinity(current))
                            Function.Call(Hash.SET_STATE_OF_CLOSEST_DOOR_OF_TYPE,model,entrance.X,entrance.Y,entrance.Z,false,current,false);
                    }
                }
            }
            catch(Exception ex){if(!reported){reported=true;Log.Error("clothing door access",ex);}}
        }
    }
}
