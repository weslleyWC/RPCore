using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
namespace RPCore
{
    // Phone only. Consumables remain exclusively owned by InventorySystem.
    public sealed class PropPresentation : ISystem
    {
        public string Name=>"visualprops";
        const string Decor="RPCorePhone";
        sealed class Owned {public int Handle,Model,Token;}
        readonly List<Owned> retired=new List<Owned>();
        Owned active;Ped owner;Model model;PropBinding binding;string profile;
        bool waiting,animationStarted,dictRequested;int deadline,cleanupAt;
        const string Dict="cellphone@",Clip="cellphone_text_read_base";
        public PropPresentation(InventorySystem inventory)
        {
            foreach(var p in World.GetAllProps())if(Function.Call<bool>(Hash.DECOR_EXIST_ON,p,Decor))retired.Add(new Owned{Handle=p.Handle,Model=p.Model.Hash,Token=Function.Call<int>(Hash.DECOR_GET_INT,p,Decor)});
        }
        public void ShowPhone()
        {
            Stop();if(retired.Count>=16 || !Visuals.Active || !Visuals.UseProps)return;
            var p=Game.Player.Character;if(p==null || !p.Exists() || !p.IsHuman || p.IsDead || p.IsInVehicle() || p.IsRagdoll || p.IsInCombat || p.Weapons.Current.Hash!=WeaponHash.Unarmed || Game.IsMissionActive)return;
            if(Visuals.Bindings.ItemProps==null || !Visuals.Bindings.ItemProps.TryGetValue("_phone",out var requested) || requested==null)requested=new PropBinding{Model="phonesmart",Fallback="prop_npc_phone_02",X=.01f,Y=.01f,Z=0,RX=0,RY=0,RZ=180};
            binding=PhonePropRules.Resolve(requested);
            model=new Model(binding.Model??"");if(!model.IsValid || !model.IsInCdImage)model=new Model(binding.Fallback??"");if(!model.IsValid || !model.IsInCdImage)return;
            owner=p;profile=Profile.Id;model.Request();Function.Call(Hash.REQUEST_ANIM_DICT,Dict);dictRequested=true;waiting=true;deadline=Game.GameTime+2500;
        }
        public void ClosePhone()=>Stop();
        public void Tick()
        {
            Cleanup();if(owner==null)return;
            var p=Game.Player.Character;
            if(!Visuals.Active || !Visuals.UseProps || !owner.Exists() || p==null || p.Handle!=owner.Handle || profile!=Profile.Id || Profile.Detect()!=Profile.Id || p.IsDead || p.IsInVehicle() || Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE,p) || p.IsRagdoll || p.IsInCombat || p.IsSwimming || Game.IsPaused || Game.IsMissionActive || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) || p.Weapons.Current.Hash!=WeaponHash.Unarmed){Stop();return;}
            if(!waiting)return;
            if(unchecked(Game.GameTime-deadline)>=0){Stop();return;}
            if(!model.IsLoaded || !Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED,Dict))return;
            if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);
            if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3)){Stop();return;}
            var prop=World.CreateProp(model,p.Position,false,false);if(prop==null || !prop.Exists()){Stop();return;}
            int token=(Guid.NewGuid().GetHashCode()&int.MaxValue)|1;
            if(!Function.Call<bool>(Hash.DECOR_SET_INT,prop,Decor,token)){prop.Delete();Stop();return;}
            active=new Owned{Handle=prop.Handle,Model=model.Hash,Token=token};prop.IsPersistent=true;prop.IsCollisionEnabled=false;
            prop.AttachTo(p.Bones[(Bone)57005],new Vector3(binding.X,binding.Y,binding.Z),new Vector3(binding.RX,binding.RY,binding.RZ));
            Function.Call(Hash.TASK_PLAY_ANIM,p,Dict,Clip,4f,-4f,-1,49,0f,false,false,false);animationStarted=true;waiting=false;model.MarkAsNoLongerNeeded();
        }
        void Stop()
        {
            try{if(animationStarted && owner!=null && owner.Exists())Function.Call(Hash.STOP_ANIM_TASK,owner,Dict,Clip,2f);}
            finally
            {
                if(active!=null)retired.Add(active);active=null;owner=null;profile=null;waiting=false;animationStarted=false;cleanupAt=Game.GameTime;Cleanup();
                if(model.Hash!=0)model.MarkAsNoLongerNeeded();model=default(Model);if(dictRequested){dictRequested=false;Function.Call(Hash.REMOVE_ANIM_DICT,Dict);}
            }
        }
        void Cleanup()
        {
            if(unchecked(Game.GameTime-cleanupAt)<0)return;cleanupAt=Game.GameTime+250;
            for(int i=retired.Count-1;i>=0;i--)
            {
                try
                {
                    var p=retired[i];int h=p.Handle;
                    if(!Function.Call<bool>(Hash.DOES_ENTITY_EXIST,h) || Function.Call<int>(Hash.GET_ENTITY_MODEL,h)!=p.Model || !Function.Call<bool>(Hash.DECOR_EXIST_ON,h,Decor) || Function.Call<int>(Hash.DECOR_GET_INT,h,Decor)!=p.Token){retired.RemoveAt(i);continue;}
                    Function.Call(Hash.DETACH_ENTITY,h,true,true);Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY,h,true,true);var handle=new OutputArgument(h);Function.Call(Hash.DELETE_OBJECT,handle);
                    if(!Function.Call<bool>(Hash.DOES_ENTITY_EXIST,h))retired.RemoveAt(i);
                }
                catch(Exception ex){Log.Error("phone owned prop cleanup",ex);}
            }
        }
        public void OnKey(KeyEventArgs e){}
        public void Reload()=>Stop();
        public void Save(){}
        public void Dispose()=>Stop();
    }
}
