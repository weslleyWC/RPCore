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
                    using(var locked=new OutputArgument(false))
                    using(var angle=new OutputArgument(0f))
                    {
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
