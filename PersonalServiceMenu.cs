using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using LemonUI.Menus;

namespace RPCore
{
    // ObjectPool.Process finishes before service Tick dispatches activations.
    public sealed class PersonalServiceMenu
    {
        readonly NativeMenu menu;
        readonly ServiceActionQueue actions=new ServiceActionQueue();
        readonly Dictionary<string,int> selections=new Dictionary<string,int>();
        readonly Action<Exception> failed;
        string page,profile;
        int actor,model,selection;
        Action parent;
        bool wasVisible;
        public PersonalServiceMenu(NativeMenu menu,Action<Exception> failed)
        {
            this.menu=menu;this.failed=failed;menu.Width=420;
            menu.Closing+=(s,e)=>{
                if(parent!=null && Bound && (Game.IsControlJustPressed(GTA.Control.FrontendCancel) || Game.IsControlJustReleased(GTA.Control.FrontendCancel) || Game.IsKeyPressed(Keys.Back) || Game.IsKeyPressed(Keys.Escape))){e.Cancel=true;Queue(parent);}
            };
        }
        bool Bound=>actor!=0 && actor==Game.Player.Character.Handle && model==Game.Player.Character.Model.Hash && profile==Profile.Id && Profile.Detect()==profile;
        public void Begin(string key,Action back=null,bool remember=true)
        {
            if(page!=null)selections[page]=menu.SelectedIndex;
            if(selections.Count>128)selections.Clear();
            page=key;parent=back;selection=remember && selections.TryGetValue(key,out int i)?i:0;
            actor=Game.Player.Character.Handle;model=Game.Player.Character.Model.Hash;profile=Profile.Id;
            actions.NewPage();if(Pressed)actions.Hold();menu.Clear();
        }
        public void Add(string title,string description,Action action,bool enabled=true)
        {
            var row=new NativeItem(title,description){Enabled=enabled};
            int revision=actions.Revision;
            row.Activated+=(s,e)=>{if(actions.Revision==revision)Queue(action);};menu.Add(row);menu.SelectedIndex=Math.Max(0,Math.Min(selection,menu.Items.Count-1));
        }
        void Queue(Action action){if(Bound)actions.Enqueue(action,actor,model,profile,Game.GameTime);}
        static bool Pressed=>Game.IsKeyPressed(Keys.Enter) || Game.IsKeyPressed(Keys.LButton) || Game.IsKeyPressed(Keys.Back) || Game.IsKeyPressed(Keys.Escape) || Game.IsControlPressed(GTA.Control.FrontendAccept) || Game.IsControlPressed(GTA.Control.FrontendCancel) || Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED,0,(int)GTA.Control.FrontendAccept) || Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED,0,(int)GTA.Control.FrontendCancel);
        public void Tick(bool allowed)
        {
            if(!menu.Visible){if(wasVisible)actions.NewPage();wasVisible=false;return;}
            wasVisible=true;
            allowed=allowed && !InterfaceOptions.ModalOpen;actions.Release(Pressed);
            if(menu.Visible)Game.DisableControlThisFrame(GTA.Control.Context);
            try{actions.Run(menu.Visible,allowed && Bound,Game.Player.Character.Handle,Game.Player.Character.Model.Hash,Profile.Id,Game.GameTime);}
            catch(Exception ex){failed(ex);}
            if(menu.Visible && (!allowed || !Bound))Close();
        }
        // Programmatic closure must not be intercepted as navigation when Escape is held.
        public void Close(){parent=null;actions.NewPage();menu.Visible=false;}
        public void Reset(){actions.NewPage();parent=null;page=null;actor=0;profile=null;selections.Clear();wasVisible=false;menu.Visible=false;}
        public static bool SafeFoot=>!Game.IsPaused && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && Game.Player.CanControlCharacter && Profile.Detect()==Profile.Id && Game.Player.Character.IsAlive && Game.Player.Character.IsHuman && !Game.Player.Character.IsInVehicle() && !Game.Player.Character.IsRagdoll && !Game.Player.Character.IsFalling && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsSwimming && !Game.Player.Character.IsClimbing && !Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE,Game.Player.Character) && Game.Player.Wanted.WantedLevel==0;
    }
}
