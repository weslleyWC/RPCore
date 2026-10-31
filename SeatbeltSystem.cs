using System;
using System.Drawing;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using GTA.UI;
namespace RPCore
{
    public class SeatbeltSystem:ISystem
    {
        public string Name=>"seatbelt";
        readonly AchievementSystem ach;Ped owner;Vehicle vehicle;VehicleServiceIdentity identity;SeatbeltOverride safety;
        public bool IsBelted=>safety!=null;
        public SeatbeltSystem(AchievementSystem ach){this.ach=ach;}
        static bool HasBelt(Vehicle v)=>v!=null && v.Exists() && !v.IsDead && v.Model.IsCar;
        bool Safe=>!Game.IsPaused && !Game.IsMissionActive && !GameplayContext.SceneBlocking && Game.Player.CanControlCharacter && Profile.Detect()==Profile.Id;
        bool Bound()
        {
            var p=Game.Player.Character;return identity!=null && p!=null && p.Exists() && HasBelt(vehicle) && p.IsInVehicle(vehicle) && identity.Matches(Profile.Id,p.Handle,p.Model.Hash,p.MemoryAddress.ToInt64(),vehicle.Handle,vehicle.Model.Hash,vehicle.MemoryAddress.ToInt64(),null);
        }
        public void Tick()
        {
            var p=Game.Player.Character;
            if(p==null || !p.Exists() || !p.IsHuman || p.IsDead || safety!=null && !Bound()){Reset();return;}
            if(Game.IsPaused)return;
            if(!Safe){Reset();return;}
            var v=p.IsInVehicle()?p.CurrentVehicle:null;if(!HasBelt(v)){Reset();return;}
            if(safety!=null){
                if(Function.Call<bool>(Hash.GET_PED_CONFIG_FLAG,p.Handle,32,true)){Function.Call(Hash.SET_PED_CONFIG_FLAG,p.Handle,32,false);safety.Applied();}
                Game.DisableControlThisFrame(GTA.Control.VehicleExit);
                if(Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED,0,(int)GTA.Control.VehicleExit))GTA.UI.Screen.ShowHelpTextThisFrame("Solte o cinto com ~b~"+Cfg.SeatbeltKey+"~s~ antes de sair.");
            }else if(v.Speed>8f && !InterfaceOptions.ModalOpen)
                new TextElement("COLOQUE O CINTO / "+Cfg.SeatbeltKey,new PointF(GTA.UI.Screen.ScaledWidth/2f,40f),.35f,Color.OrangeRed,GTA.UI.Font.ChaletLondon,Alignment.Center).Draw();
        }
        public void OnKey(KeyEventArgs e)
        {
            if(e.KeyCode!=Cfg.SeatbeltKey || e.Modifiers!=Keys.None || !Safe || InterfaceOptions.ModalOpen)return;
            var p=Game.Player.Character;if(p==null || !p.Exists() || !p.IsHuman || p.IsDead || !p.IsInVehicle() || !HasBelt(p.CurrentVehicle))return;
            if(safety!=null){Reset();Sfx.Play("belt_off");Notify.Show("Cinto solto.");return;}
            owner=p;vehicle=p.CurrentVehicle;identity=new VehicleServiceIdentity(Profile.Id,p.Handle,p.Model.Hash,p.MemoryAddress.ToInt64(),vehicle.Handle,vehicle.Model.Hash,vehicle.MemoryAddress.ToInt64(),null);
            safety=new SeatbeltOverride(Function.Call<bool>(Hash.GET_PED_CONFIG_FLAG,p.Handle,32,true));
            Function.Call(Hash.SET_PED_CONFIG_FLAG,p.Handle,32,false);safety.Applied();Sfx.Play("belt_on");Notify.Show("Cinto colocado.");
            try{ach.Bump("belts_on");}catch(Exception ex){Log.Error("seatbelt achievement",ex);}
        }
        void Reset()
        {
            try{
                if(safety!=null && owner!=null && owner.Exists() && identity!=null){
                    bool same=identity.OwnsActor(owner.Handle,owner.Model.Hash,owner.MemoryAddress.ToInt64());
                    if(same && safety.CanRestore(true,Function.Call<bool>(Hash.GET_PED_CONFIG_FLAG,owner.Handle,32,true)))Function.Call(Hash.SET_PED_CONFIG_FLAG,owner.Handle,32,safety.Original);
                }
            }finally{safety=null;owner=null;vehicle=null;identity=null;}
        }
        public void Reload()=>Reset();public void Save(){}public void Dispose()=>Reset();
    }
}
