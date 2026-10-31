using System;
namespace RPCore
{
    public static class GameplayContextRules
    {
        public static bool SceneBlocks(bool active,bool playing,bool control)=>playing || active && !control;
        public static bool PreparedOnly(bool active,bool playing,bool control)=>active && !playing && control;
        public static bool HideHud(bool paused,bool sceneBlocks,bool nativeHidden,bool preparedOnly)=>paused || sceneBlocks || nativeHidden && !preparedOnly;
    }
    public sealed class AreaWarmup
    {
        int actor,last,stable;string profile;float x,y,z;bool seen,near;
        public bool Ready{get;private set;}
        public void Update(int now,int currentActor,string currentProfile,float px,float py,float pz,bool inArea,bool safe,bool collision)
        {
            bool finite=!float.IsNaN(px) && !float.IsInfinity(px) && !float.IsNaN(py) && !float.IsInfinity(py) && !float.IsNaN(pz) && !float.IsInfinity(pz);
            int elapsed=unchecked(now-last);double dx=(double)px-x,dy=(double)py-y,dz=(double)pz-z;
            bool same=seen && actor==currentActor && currentActor!=0 && profile==currentProfile && near==inArea && elapsed>=0 && elapsed<=1000 && finite && dx*dx+dy*dy+dz*dz<=1600;
            stable=same && inArea && safe && collision?Math.Min(4000,stable+elapsed):0;
            Ready=inArea && safe && collision && finite && stable>=4000;
            seen=true;actor=currentActor;profile=currentProfile;last=now;x=px;y=py;z=pz;near=inArea;
        }
    }
}
