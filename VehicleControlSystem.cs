using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
namespace RPCore
{
    public sealed class VehicleControlSystem : ISystem
    {
        public string Name=>"vehiclecontrols";
        readonly ObjectPool pool;readonly Func<Vehicle,bool> hasFuel;
        readonly NativeMenu menu=new NativeMenu("Veiculo","Controles"),doors=new NativeMenu("Veiculo","Portas"),windows=new NativeMenu("Veiculo","Vidros"),lights=new NativeMenu("Veiculo","Luzes");
        NativeMenu[] Menus=>new[]{menu,doors,windows,lights};
        readonly List<Action> refresh=new List<Action>();readonly bool?[] windowDown=new bool?[4];readonly bool[] neonInstalled=new bool[4];
        Vehicle controlled;Ped actor;string profile;bool engineOff,cruise,left,right,indicatorsChanged,lightsChanged;int originalIndicators;float cruiseSpeed;
        Action seatbelt;Func<bool> belted;
        public bool IsCruising=>cruise;
        string CruiseDisplay=>ReferenceLayout.Speed(cruiseSpeed,InterfaceOptions.MetricSpeed).ToString("0")+(InterfaceOptions.MetricSpeed?" km/h":" mph");
        public VehicleControlSystem(ObjectPool pool,Func<Vehicle,bool> hasFuel)
        {
            this.pool=pool;this.hasFuel=hasFuel;foreach(var m in Menus){pool.Add(m);Visuals.Register(m,"vehicle");m.Width=420;m.Offset=new PointF(40,8);}
            Add(menu,"Motor",VehicleAction.Engine,()=>controlled.IsEngineRunning?"Ligado":"Desligado");
            Add(menu,"Cinto de seguranca",()=>seatbelt?.Invoke(),()=>belted?.Invoke()==true?"Colocado":"Solto");
            Add(menu,"Piloto automatico",VehicleAction.Cruise,()=>cruise?CruiseDisplay:"Desligado");
            menu.AddSubMenu(lights);var doorLink=menu.AddSubMenu(doors);var windowLink=menu.AddSubMenu(windows);refresh.Add(()=>{doorLink.Enabled=controlled.Model.IsCar;windowLink.Enabled=controlled.Model.IsCar;});
            Add(lights,"Farois",VehicleAction.ExteriorLights,()=>controlled.AreLightsOn?"Ligados":"Desligados");
            Add(lights,"Luz interna",VehicleAction.InteriorLights,()=>controlled.IsInteriorLightOn?"Ligada":"Desligada");
            Add(lights,"Seta esquerda",VehicleAction.LeftSignal,()=>controlled.IsLeftIndicatorLightOn?"Ligada":"Desligada");
            Add(lights,"Seta direita",VehicleAction.RightSignal,()=>controlled.IsRightIndicatorLightOn?"Ligada":"Desligada");
            Add(lights,"Pisca-alerta",VehicleAction.Hazards,()=>controlled.IsLeftIndicatorLightOn && controlled.IsRightIndicatorLightOn?"Ligado":"Desligado");
            Add(lights,"Neon instalado",VehicleAction.Neons,()=>Enumerable.Range(0,4).Any(i=>Function.Call<bool>(Hash.GET_VEHICLE_NEON_ENABLED,controlled,i))?"Ligado":"Desligado");
            string[] doorNames={"Capo","Porta-malas","Porta dianteira esquerda","Porta dianteira direita","Porta traseira esquerda","Porta traseira direita"};
            VehicleAction[] actions={VehicleAction.Hood,VehicleAction.Trunk,VehicleAction.DoorFrontLeft,VehicleAction.DoorFrontRight,VehicleAction.DoorRearLeft,VehicleAction.DoorRearRight};
            for(int i=0;i<actions.Length;i++){var action=actions[i];Add(doors,doorNames[i],action,()=>Function.Call<float>(Hash.GET_VEHICLE_DOOR_ANGLE_RATIO,controlled,VehicleUiRules.Door(action))>.1f?"Aberta":"Fechada");}
            for(int i=0;i<4;i++){int index=i;Add(windows,new[]{"Dianteiro esquerdo","Dianteiro direito","Traseiro esquerdo","Traseiro direito"}[i],(VehicleAction)((int)VehicleAction.WindowFrontLeft+i),()=>windowDown[index]==true?"Subir":"Baixar");}
            Add(windows,"Subir todos",VehicleAction.WindowsUp);Add(windows,"Baixar todos",VehicleAction.WindowsDown);
        }
        public void ConfigureSeatbelt(Action toggle,Func<bool> state){seatbelt=toggle;belted=state;}
        void Add(NativeMenu target,string name,VehicleAction action,Func<string> state=null)=>Add(target,name,()=>Execute(action),state);
        void Add(NativeMenu target,string name,Action action,Func<string> state=null)
        {
            var row=new NativeItem(name,"Somente no veiculo conduzido pelo personagem. Portas e vidros exigem estar parado.");target.Add(row);
            row.Activated+=(s,e)=>{if(!Bound()){CloseMenus();return;}try{action();}catch(Exception ex){Log.Error("vehicle action",ex);Notify.Show("Comando nao concluido.");}};
            if(state!=null)refresh.Add(()=>row.AltTitle=state());
        }
        bool Safe()=>Cfg.EnableVehicleControls && !Game.IsPaused && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && Game.Player.CanControlCharacter && Profile.Detect()==Profile.Id && !Game.Player.Character.IsDead;
        bool Bound()
        {
            var p=Game.Player.Character;return Safe() && p!=null && p.Exists() && controlled!=null && controlled.Exists() && !controlled.IsDead && p.IsInVehicle() && VehicleUiRules.SameOwner(actor?.Handle??0,p.Handle,controlled.Handle,p.CurrentVehicle.Handle,profile,Profile.Id,controlled.Driver==p);
        }
        bool Attach()
        {
            var p=Game.Player.Character;var v=p.IsInVehicle()?p.CurrentVehicle:null;
            if(!Safe() || v==null || !v.Exists() || v.IsDead || v.Driver!=p || v.Model.IsBicycle || !(v.Model.IsCar || v.Model.IsBike || v.Model.IsQuadBike))return false;
            if(controlled==null || !controlled.Exists() || controlled.Handle!=v.Handle || actor?.Handle!=p.Handle || profile!=Profile.Id)
            {
                Release();controlled=v;actor=p;profile=Profile.Id;originalIndicators=(v.IsLeftIndicatorLightOn?1:0)|(v.IsRightIndicatorLightOn?2:0);left=(originalIndicators&1)!=0;right=(originalIndicators&2)!=0;Array.Clear(windowDown,0,4);
                for(int i=0;i<4;i++)neonInstalled[i]=Function.Call<bool>(Hash.GET_VEHICLE_NEON_ENABLED,v,i);
            }
            return true;
        }
        public void OpenMenu(){if(!Attach()){Notify.Show("Entre como motorista em um veiculo de rua.");return;}pool.HideAll();menu.Name=controlled.LocalizedName;menu.Visible=true;Refresh();}
        public void Execute(VehicleAction action)
        {
            if(Menus.Any(m=>m.Visible)?!Bound():!Attach())return;
            if(action==VehicleAction.OpenMenu){OpenMenu();return;}
            int door=VehicleUiRules.Door(action),window=VehicleUiRules.Window(action);
            if(door>=0){Door(door);return;}if(window>=0){Window(window,windowDown[window]!=true);return;}
            switch(action)
            {
                case VehicleAction.Engine:ToggleEngine();break;case VehicleAction.Cruise:ToggleCruise();break;
                case VehicleAction.LeftSignal:Signal(true);break;case VehicleAction.RightSignal:Signal(false);break;case VehicleAction.Hazards:bool on=!(controlled.IsLeftIndicatorLightOn && controlled.IsRightIndicatorLightOn);left=right=on;SetSignals();break;
                case VehicleAction.ExteriorLights:Function.Call(Hash.SET_VEHICLE_LIGHTS,controlled,controlled.AreLightsOn?1:2);lightsChanged=true;break;
                case VehicleAction.InteriorLights:controlled.IsInteriorLightOn=!controlled.IsInteriorLightOn;break;
                case VehicleAction.WindowsUp:case VehicleAction.WindowsDown:
                    if(!VehicleUiRules.BodySafe(controlled.Model.IsCar,controlled.Speed)){Notify.Show("Pare o carro para mexer nos vidros.");break;}
                    int changed=0;for(int i=0;i<4;i++)if(Window(i,action==VehicleAction.WindowsDown,true))changed++;if(changed==0)Notify.Show("Nenhum vidro disponivel neste modelo.");break;
                case VehicleAction.Neons:
                    bool any=Enumerable.Range(0,4).Any(i=>Function.Call<bool>(Hash.GET_VEHICLE_NEON_ENABLED,controlled,i));int observed=0;
                    for(int i=0;i<4;i++)if(Function.Call<bool>(Hash.GET_VEHICLE_NEON_ENABLED,controlled,i) || neonInstalled[i])observed|=1<<i;
                    string key=VehicleIdentity.Key(controlled.Model.Hash,Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT,controlled));
                    int mask=key==null?observed:VehicleUiRules.RememberNeons(Wallet.Transport,key,observed,Wallet.Save);
                    if(mask<=0){Notify.Show("Neon nao instalado/comprovado. Instale na oficina.");break;}
                    for(int i=0;i<4;i++)if((mask&(1<<i))!=0){neonInstalled[i]=true;Function.Call(Hash.SET_VEHICLE_NEON_ENABLED,controlled,i,!any);}break;
            }
        }
        void ToggleEngine()
        {
            if(controlled.IsEngineRunning){engineOff=true;cruise=false;Function.Call(Hash.SET_VEHICLE_ENGINE_ON,controlled,false,true,true);}
            else{if(!hasFuel(controlled)){Notify.Show("Tanque vazio. Abasteca primeiro.");return;}engineOff=false;Function.Call(Hash.SET_VEHICLE_ENGINE_ON,controlled,true,true,false);}
        }
        void ToggleCruise()
        {
            if(cruise){CancelCruise();return;}
            if(!controlled.IsEngineRunning || !hasFuel(controlled) || controlled.Speed<8 || controlled.GetSpeedVector(true).Y<=0 || !controlled.IsOnAllWheels){Notify.Show("Piloto automatico: avance acima de"+ReferenceLayout.Speed(8,InterfaceOptions.MetricSpeed).ToString("0")+(InterfaceOptions.MetricSpeed?" km/h":" mph")+", com rodas no chao.");return;}
            cruiseSpeed=controlled.Speed;cruise=true;Notify.Show("Piloto automatico: "+CruiseDisplay+". Freio/acelerador cancelam.");
        }
        void CancelCruise(){if(cruise)Notify.Show("Piloto automatico cancelado.");cruise=false;}
        void Signal(bool useLeft)
        {bool was=useLeft?controlled.IsLeftIndicatorLightOn:controlled.IsRightIndicatorLightOn;left=useLeft && !was;right=!useLeft && !was;SetSignals();}
        void SetSignals(){Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS,controlled,1,left);Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS,controlled,0,right);indicatorsChanged=true;}
        void Door(int index)
        {
            if(!VehicleUiRules.BodySafe(controlled.Model.IsCar,controlled.Speed) || !Function.Call<bool>(Hash.GET_IS_DOOR_VALID,controlled,index) || Function.Call<bool>(Hash.IS_VEHICLE_DOOR_DAMAGED,controlled,index)){Notify.Show("Pare o carro e use uma porta existente e sem avaria.");return;}
            if(Function.Call<float>(Hash.GET_VEHICLE_DOOR_ANGLE_RATIO,controlled,index)>.1f)Function.Call(Hash.SET_VEHICLE_DOOR_SHUT,controlled,index,false);else Function.Call(Hash.SET_VEHICLE_DOOR_OPEN,controlled,index,false,false);
        }
        bool Window(int index,bool down,bool silent=false)
        {
            if(!VehicleUiRules.BodySafe(controlled.Model.IsCar,controlled.Speed)){if(!silent)Notify.Show("Pare o carro para mexer nos vidros.");return false;}
            string bone=new[]{"window_lf","window_rf","window_lr","window_rr"}[index];if(Function.Call<int>(Hash.GET_ENTITY_BONE_INDEX_BY_NAME,controlled,bone)<0){if(!silent)Notify.Show("Vidro indisponivel neste modelo.");return false;}
            Function.Call(down?Hash.ROLL_DOWN_WINDOW:Hash.ROLL_UP_WINDOW,controlled,index);windowDown[index]=down;return true;
        }
        void Refresh(){if(!Bound())return;foreach(var update in refresh)update();}
        public void Tick()
        {
            if(Game.IsPaused){cruise=false;CloseMenus();return;}
            if(Menus.Any(m=>m.Visible) && !Bound()){Release();return;}
            if(!Attach()){Release();return;}Refresh();
            if(engineOff)Function.Call(Hash.SET_VEHICLE_ENGINE_ON,controlled,false,true,true);
            if(!cruise)return;
            if(pool.AreAnyVisible || InterfaceOptions.ModalOpen || !controlled.IsEngineRunning || !hasFuel(controlled) || !controlled.IsOnAllWheels || controlled.HasCollided || controlled.Speed<3 || Game.GetControlValueNormalized(GTA.Control.VehicleBrake)>.05f || Game.GetControlValueNormalized(GTA.Control.VehicleHandbrake)>.05f || Game.GetControlValueNormalized(GTA.Control.VehicleAccelerate)>.1f){CancelCruise();return;}
            Game.SetControlValueNormalized(GTA.Control.VehicleAccelerate,Math.Max(0,Math.Min(1,.2f+(cruiseSpeed-controlled.Speed)*.18f)));
            if(!Visuals.VehicleReady)new GTA.UI.TextElement("PILOTO "+CruiseDisplay,new PointF(GTA.UI.Screen.ScaledWidth-300,GTA.UI.Screen.Height-150),.32f,Color.LightGreen).Draw();
        }
        public void OnKey(KeyEventArgs e)
        {
            if(e.KeyCode==Cfg.VehicleMenuKey && Menus.Any(m=>m.Visible)){CloseMenus();return;}
            if(pool.AreAnyVisible || InterfaceOptions.ModalOpen || !Safe())return;
            if(e.KeyCode==Cfg.VehicleMenuKey){OpenMenu();return;}
            if(e.KeyCode==Cfg.EngineKey)Execute(VehicleAction.Engine);else if(e.KeyCode==Cfg.CruiseKey)Execute(VehicleAction.Cruise);else if(e.KeyCode==Cfg.LeftSignalKey)Execute(VehicleAction.LeftSignal);else if(e.KeyCode==Cfg.RightSignalKey)Execute(VehicleAction.RightSignal);else if(e.KeyCode==Cfg.HazardKey)Execute(VehicleAction.Hazards);
        }
        void CloseMenus(){foreach(var m in Menus)m.Visible=false;}
        void Release()
        {
            try{if(controlled!=null && controlled.Exists()){if(engineOff)Function.Call(Hash.SET_VEHICLE_ENGINE_ON,controlled,controlled.IsEngineRunning,true,false);if(lightsChanged)Function.Call(Hash.SET_VEHICLE_LIGHTS,controlled,0);if(indicatorsChanged){Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS,controlled,1,(originalIndicators&1)!=0);Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS,controlled,0,(originalIndicators&2)!=0);}}}
            finally{CloseMenus();controlled=null;actor=null;profile=null;engineOff=cruise=indicatorsChanged=lightsChanged=false;}
        }
        public void Reload()=>Release();public void Save(){}public void Dispose()=>Release();
    }
}
