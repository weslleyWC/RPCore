using System;
using GTA;
using GTA.Native;
namespace RPCore
{
    public sealed class DealershipDoorRuntime
    {
        readonly DealershipDoor[] doors=DealershipDoorRules.Doors();
        bool session,addedShowroom,hiddenFacade,reported;
        int nextRequest;
        static bool Active(string name)=>Function.Call<bool>(Hash.IS_IPL_ACTIVE,name);
        public void Tick()
        {
            // Never stream/unlock during a story mission, cutscene or a character handover.
            if(Game.IsPaused)return;
            if(Game.IsMissionActive || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE)){session=false;addedShowroom=hiddenFacade=false;return;}
            if(!Game.Player.CanControlCharacter || Profile.Detect()!=Profile.Id)return;
            var ped=Game.Player.Character;var pos=ped.Position;
            if(!Cfg.EnableGarages || !DealershipDoorRules.ShowroomRange(pos.X,pos.Y,pos.Z,70)){Release();return;}
            if(!GameplayContext.BackgroundReady)return;
            try{
                if(!session && DealershipDoorRules.ShowroomRange(pos.X,pos.Y,pos.Z,35)){session=true;nextRequest=Game.GameTime;}
                if(!session)return;
                // The PSRP showroom uses shr_int instead of the fake exterior interior.
                // Request asynchronously; do not pin interiors, clear the world or remove custom IPLs.
                if(unchecked(Game.GameTime-nextRequest)>=0){
                    nextRequest=Game.GameTime+1000;
                    if(!Active("shr_int")){Function.Call(Hash.REQUEST_IPL,"shr_int");addedShowroom=true;}
                    if(Active("shr_int") && Active("fakeint")){Function.Call(Hash.REMOVE_IPL,"fakeint");hiddenFacade=true;}
                }
                if(!PersonalServiceMenu.SafeFoot)return;
                foreach(var door in doors){
                    if(!DealershipDoorRules.Near(door,pos.X,pos.Y,pos.Z))continue;
                    using(var existing=OutputArgument.Init(0UL)){
                        if(Function.Call<bool>(Hash.DOOR_SYSTEM_FIND_EXISTING_DOOR,door.X,door.Y,door.Z,door.Model,existing)){
                            int id=existing.GetResult<int>();
                            if(Function.Call<int>(Hash.DOOR_SYSTEM_GET_DOOR_STATE,id)!=0)Function.Call(Hash.DOOR_SYSTEM_SET_DOOR_STATE,id,0,false,true);
                        }
                    }
                    using(var locked=OutputArgument.Init(0UL))
                    using(var angle=OutputArgument.Init(0UL)){
                        Function.Call(Hash.GET_STATE_OF_CLOSEST_DOOR_OF_TYPE,door.Model,door.X,door.Y,door.Z,locked,angle);
                        float current=angle.GetResult<float>();
                        if(locked.GetResult<bool>() && !float.IsNaN(current) && !float.IsInfinity(current))Function.Call(Hash.SET_STATE_OF_CLOSEST_DOOR_OF_TYPE,door.Model,door.X,door.Y,door.Z,false,current,false);
                    }
                }
            }catch(Exception ex){if(!reported){reported=true;Log.Error("dealership door access",ex);}}
        }
        public void Release()
        {
            if(!session)return;
            // Story scripts may take ownership of the IPLs while the service is suspended.
            // Do not alter those scripts' current world state on shutdown/profile switch.
            var pos=Game.Player.Character.Position;
            if(Game.IsMissionActive || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) || DealershipDoorRules.ShowroomRange(pos.X,pos.Y,pos.Z,35)){session=false;addedShowroom=hiddenFacade=false;return;}
            try{
                if(hiddenFacade && !Active("fakeint")){Function.Call(Hash.REQUEST_IPL,"fakeint");hiddenFacade=false;}
                if(addedShowroom){Function.Call(Hash.REMOVE_IPL,"shr_int");addedShowroom=false;}
                session=false;addedShowroom=hiddenFacade=false;
            }catch(Exception ex){if(!reported){reported=true;Log.Error("dealership showroom release",ex);}}
        }
    }
}
