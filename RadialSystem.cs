using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using LemonUI;

namespace RPCore
{
    // Own input/action controller; artwork is supplied by the original OIV.
    public sealed class RadialSystem : ISystem
    {
        public string Name { get { return "radial"; } }
        readonly ObjectPool pool;
        readonly Action<int> activate;
        static readonly string[] labels = ReferenceInterface.RadialLabels;
        static readonly string[] icons = ReferenceInterface.RadialIcons;
        static readonly PointF[] centers=RadialMenuRules.Centers;
        sealed class Slot { public string Label,Icon;public Action Action;public Func<string> Detail; }
        readonly Slot[] slots=new Slot[8];
        InventorySystem inventory;Action clothing;Func<IEnumerable<GpsDestination>> destinations;
        RadialPage page;int itemPage;List<string> itemIds=new List<string>();
        Action<VehicleAction> vehicleAction;Action seatbelt;Func<bool> vehicleAvailable;int vehiclePageHandle,pendingVehicle;
        Action pending;int pendingActor,pendingTime;string pendingProfile;
        bool mouseReady,mouseHeld,controllerReady;PointF mousePoint;int pressedSlot=-1;
        int selected;
        public bool Visible { get; private set; }
        public RadialSystem(ObjectPool pool, Action<int> activate) { this.pool = pool; this.activate = activate; }
        public void Configure(InventorySystem inventory,Action clothing,Func<IEnumerable<GpsDestination>> destinations){this.inventory=inventory;this.clothing=clothing;this.destinations=destinations;}
        public void ConfigureVehicle(Func<bool> available,Action<VehicleAction> action,Action seatbelt){vehicleAvailable=available;vehicleAction=action;this.seatbelt=seatbelt;}
        void Close(bool cancel=true){Visible=false;mouseReady=false;controllerReady=false;pressedSlot=-1;if(cancel)pending=null;}
        void Put(int index,string label,string icon,Action action,Func<string> detail=null)=>slots[index]=new Slot{Label=label,Icon=icon,Action=action,Detail=detail};
        void Build(RadialPage next)
        {
            page=next;selected=0;pressedSlot=-1;Array.Clear(slots,0,8);
            if(page==RadialPage.Root)
            {
                for(int i=0;i<8;i++){int action=i;Put(i,labels[i],icons[i],()=>activate(action));}
                if(inventory!=null)slots[1].Action=()=>Build(RadialPage.Inventory);
                if(destinations!=null)slots[4].Action=()=>Build(RadialPage.Gps);
                if(vehicleAvailable!=null)slots[3].Action=()=>{if(vehicleAvailable()){vehiclePageHandle=Game.Player.Character.CurrentVehicle.Handle;Build(RadialPage.Vehicle);}else Notify.Show("Entre em um veiculo para abrir os controles.");};
                return;
            }
            if(page==RadialPage.Inventory)
            {
                Put(0,"Abrir inventario","DocumentsIconRad",()=>activate(1));
                var pages=new[]{RadialPage.Drinks,RadialPage.Food,RadialPage.Health,RadialPage.Weapons,RadialPage.Tools};
                var names=new[]{"Bebidas","Comida","Saude","Armas","Ferramentas"};var textures=new[]{"DrinkIconRad","FoodIconRad","HealthIconRad","WeaponIconRad","ToolIconRad"};
                for(int i=0;i<5;i++){var target=pages[i];Put(i+1,names[i],textures[i],()=>{itemPage=0;itemIds=RadialMenuRules.Items(target,inventory.Defs,inventory.Snapshot(),inventory.Layout);Build(target);});}
                if(clothing!=null)Put(6,"Roupas / guarda-roupa","ClothingItemRad",clothing);
                Put(7,"Voltar","PreviousPageIconRad",()=>Build(RadialPage.Root));return;
            }
            if(page==RadialPage.Gps)
            {
                var kinds=new[]{"Fuel","Grocery","Workshop","Garage","Barber","PDM"};var names=new[]{"Posto mais proximo","Conveniencia mais proxima","Oficina mais proxima","Garagem mais proxima","Barbearia mais proxima","PDM"};var images=new[]{"FuelIconRad","ShopIconRad","MechanicIconRad","ClothingItemRad","BarberIconRad","PDMrad"};
                for(int i=0;i<6;i++){string kind=kinds[i];Put(i,names[i],images[i],()=>{var p=Game.Player.Character.Position;var target=MapPinRules.Nearest(destinations(),kind,p.X,p.Y,p.Z);if(target==null){Notify.Show("Nenhum local disponivel nesta categoria.");return;}Function.Call(Hash.SET_NEW_WAYPOINT,target.X,target.Y);Notify.Show("GPS: "+target.Name);});}
                Put(6,"Voltar","PreviousPageIconRad",()=>Build(RadialPage.Root));Put(7,"Todos os destinos","NextPageIconRad",()=>activate(4));return;
            }
            if(page>=RadialPage.Vehicle){BuildVehicle();return;}
            itemPage=Math.Max(0,Math.Min(itemPage,RadialMenuRules.Pages(itemIds.Count)-1));
            for(int i=0;i<6;i++){int index=itemPage*6+i;if(index>=itemIds.Count)break;string id=itemIds[index];var def=inventory.Defs[id];Put(i,def.Name,def.Icon??id,()=>inventory.UseSelected(id),()=>"x"+inventory.Count(id));}
            Put(6,itemPage==0?"Voltar":"Pagina anterior","PreviousPageIconRad",()=>{if(itemPage==0)Build(RadialPage.Inventory);else{itemPage--;Build(page);}});
            if(itemPage+1<RadialMenuRules.Pages(itemIds.Count))Put(7,"Proxima pagina","NextPageIconRad",()=>{itemPage++;Build(page);});
        }
        void VehicleSlot(int slot,string name,string icon,VehicleAction action)=>Put(slot,name,icon,()=>vehicleAction(action));
        void BuildVehicle()
        {
            if(page==RadialPage.Vehicle)
            {
                Put(0,"Motor e conducao","CarSettingRad",()=>Build(RadialPage.VehicleCore));Put(1,"Luzes","HeadlightIconRad",()=>Build(RadialPage.VehicleLights));
                if(Game.Player.Character.CurrentVehicle.Model.IsCar){Put(2,"Portas","DoorIconRightRad",()=>Build(RadialPage.VehicleDoors));Put(3,"Vidros","WindowRightRad",()=>Build(RadialPage.VehicleWindows));}
                Put(6,"Inicio","HomeIconRad",()=>Build(RadialPage.Root));VehicleSlot(7,"Menu completo","SettingsIconRad",VehicleAction.OpenMenu);return;
            }
            if(page==RadialPage.VehicleCore)
            {VehicleSlot(0,"Motor","EngineRad",VehicleAction.Engine);Put(1,"Cinto","SeatbeltIconRad",seatbelt);VehicleSlot(2,"Piloto automatico","CruiseIconRad",VehicleAction.Cruise);}
            else if(page==RadialPage.VehicleLights)
            {
                var actions=new[]{VehicleAction.ExteriorLights,VehicleAction.InteriorLights,VehicleAction.LeftSignal,VehicleAction.RightSignal,VehicleAction.Hazards,VehicleAction.Neons};
                var names=new[]{"Farois","Luz interna","Seta esquerda","Seta direita","Pisca-alerta","Neon instalado"};var icons=new[]{"HeadlightIconRad","InteriorLightIconRad","BlinkerLeftRad","BlinkerRightRad","HazardRad","UnderglowRad"};
                for(int i=0;i<6;i++)VehicleSlot(i,names[i],icons[i],actions[i]);VehicleSlot(6,"Menu completo","SettingsIconRad",VehicleAction.OpenMenu);Put(7,"Veiculo","CarIconRad",()=>Build(RadialPage.Vehicle));return;
            }
            else if(page==RadialPage.VehicleDoors)
            {
                var actions=new[]{VehicleAction.Hood,VehicleAction.Trunk,VehicleAction.DoorFrontLeft,VehicleAction.DoorFrontRight,VehicleAction.DoorRearLeft,VehicleAction.DoorRearRight};
                var names=new[]{"Capo","Porta-malas","Dianteira esquerda","Dianteira direita","Traseira esquerda","Traseira direita"};for(int i=0;i<6;i++)VehicleSlot(i,names[i],i<2?"HoodRad":i%2==0?"DoorIconLeftRad":"DoorIconRightRad",actions[i]);
            }
            else
            {
                var names=new[]{"Dianteiro esquerdo","Dianteiro direito","Traseiro esquerdo","Traseiro direito"};for(int i=0;i<4;i++)VehicleSlot(i,names[i],i%2==0?"WindowLeftRad":"WindowRightRad",(VehicleAction)((int)VehicleAction.WindowFrontLeft+i));
                VehicleSlot(4,"Subir todos","WindowLeftRad",VehicleAction.WindowsUp);VehicleSlot(5,"Baixar todos","WindowRightRad",VehicleAction.WindowsDown);
            }
            Put(6,"Veiculo","CarIconRad",()=>Build(RadialPage.Vehicle));
        }
        bool NavigationSlot(int i)=>page>=RadialPage.Vehicle?(page==RadialPage.Vehicle?(i<4 || i==6):page==RadialPage.VehicleLights?i==7:i==6):page==RadialPage.Root && vehicleAvailable!=null && i==3 || page==RadialPage.Root && inventory!=null && i==1 || page==RadialPage.Root && destinations!=null && i==4 || page==RadialPage.Inventory && i>=1 && i<=5 || page!=RadialPage.Root && page!=RadialPage.Inventory && page!=RadialPage.Gps && i>=6 || page==RadialPage.Inventory && i==7 || page==RadialPage.Gps && i==6;
        void Back(){if(page==RadialPage.Root)Close();else Build(page>=RadialPage.Vehicle?(page==RadialPage.Vehicle?RadialPage.Root:RadialPage.Vehicle):page==RadialPage.Inventory || page==RadialPage.Gps?RadialPage.Root:RadialPage.Inventory);}
        void Select()
        {
            var slot=slots[selected];if(slot==null)return;
            if(NavigationSlot(selected)){slot.Action();return;}
            var action=slot.Action;pendingVehicle=page>=RadialPage.Vehicle?vehiclePageHandle:0;Close(false);pending=action;pendingActor=Game.Player.Character.Handle;pendingProfile=Profile.Id;pendingTime=Game.GameTime;
        }
        public void OnKey(KeyEventArgs e)
        {
            if(e.KeyCode==InterfaceOptions.RadialKey)
            {
                if(Visible)Close();else if(InterfaceOptions.RadialEnabled && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen && !Game.IsMissionActive && Game.Player.CanControlCharacter){Visible=true;Build(RadialPage.Root);mouseReady=false;controllerReady=false;}return;
            }
            if(!Visible || e.Modifiers!=Keys.None)return;
            pressedSlot=-1;
            if(e.KeyCode==Keys.Escape || e.KeyCode==Keys.Back)Back();
            else if(e.KeyCode==Keys.Q || e.KeyCode==Keys.Left)selected=HudMath.MoveRadial(selected,-1);
            else if(e.KeyCode==Keys.E || e.KeyCode==Keys.Right)selected=HudMath.MoveRadial(selected,1);
            else if(e.KeyCode==Keys.Enter)Select();
        }
        public void Tick()
        {
            var p = Game.Player.Character;
            if(pending!=null)
            {
                var action=pending;pending=null;
                if(p!=null && p.Exists() && !p.IsDead && p.Handle==pendingActor && (pendingVehicle==0 || p.IsInVehicle() && p.CurrentVehicle.Handle==pendingVehicle) && Profile.Id==pendingProfile && Profile.Detect()==Profile.Id && !Game.IsPaused && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && !InterfaceOptions.ModalOpen && !pool.AreAnyVisible && Game.Player.CanControlCharacter && unchecked(Game.GameTime-pendingTime)>=0 && unchecked(Game.GameTime-pendingTime)<1000)try{action();}catch(Exception ex){Log.Error("radial action",ex);Notify.Show("Acao nao concluida. Confira RPCore.log.");}
            }
            if(!Visible)return;
            if(page>=RadialPage.Vehicle && (!p.IsInVehicle() || p.CurrentVehicle.Handle!=vehiclePageHandle)){Build(RadialPage.Root);return;}
            if (!InterfaceOptions.RadialEnabled || Game.IsPaused || p.IsDead || !Game.Player.CanControlCharacter || Profile.Detect()!=Profile.Id || Game.IsMissionActive || pool.AreAnyVisible || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE)) { Close(); return; }
            foreach (var control in new[] { GTA.Control.Attack, GTA.Control.Attack2, GTA.Control.Aim, GTA.Control.VehicleAttack, GTA.Control.VehicleAttack2, GTA.Control.VehicleAim, GTA.Control.SelectWeapon, GTA.Control.SelectWeaponUnarmed, GTA.Control.SelectWeaponMelee, GTA.Control.SelectWeaponHandgun, GTA.Control.SelectWeaponShotgun, GTA.Control.SelectWeaponSmg, GTA.Control.SelectWeaponAutoRifle, GTA.Control.SelectWeaponSniper, GTA.Control.SelectWeaponHeavy, GTA.Control.NextWeapon, GTA.Control.PrevWeapon, GTA.Control.Cover, GTA.Control.Context, GTA.Control.Phone, GTA.Control.FrontendPause, GTA.Control.FrontendPauseAlternate }) Game.DisableControlThisFrame(control);
            bool keyboard=Function.Call<bool>(Hash.IS_USING_KEYBOARD_AND_MOUSE,0);
            float s=RadialMenuRules.Scale(Visuals.Width,Visuals.InsetX,Visuals.InsetY,Visuals.Scale),x=(Visuals.Width-1920*s)/2,y=(1080-1080*s)/2;
            if(keyboard)
            {
                controllerReady=false;GTA.UI.Hud.ShowCursorThisFrame();
                foreach(var c in new[]{GTA.Control.LookLeftRight,GTA.Control.LookUpDown,GTA.Control.CursorAccept,GTA.Control.CursorCancel})Game.DisableControlThisFrame(c);
                bool held=Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED,0,(int)GTA.Control.CursorAccept);
                if(InventoryMouseLayout.Point(Function.Call<float>(Hash.GET_CONTROL_NORMAL,0,(int)GTA.Control.CursorX),Function.Call<float>(Hash.GET_CONTROL_NORMAL,0,(int)GTA.Control.CursorY),Visuals.Width,x,y,s,out var point))
                {
                    bool changed=Math.Abs(point.X-mousePoint.X)+Math.Abs(point.Y-mousePoint.Y)>.25f;mousePoint=point;
                    if(!mouseReady){mouseReady=true;mouseHeld=held;pressedSlot=-1;}
                    else
                    {
                        bool pressed=held && !mouseHeld,released=!held && mouseHeld;mouseHeld=held;int hit=RadialMenuRules.Hit(point);
                        if((changed || pressed) && hit>=0)selected=hit;
                        if(pressed)pressedSlot=hit;
                        if(Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED,0,(int)GTA.Control.CursorCancel)){pressedSlot=-1;Back();return;}
                        if(released && pressedSlot>=0){int start=pressedSlot;pressedSlot=-1;if(start==hit){selected=hit;Select();return;}}
                    }
                }else{mouseReady=false;pressedSlot=-1;}
            }
            else
            {
                mouseReady=false;pressedSlot=-1;
                if(!controllerReady){controllerReady=!new[]{GTA.Control.FrontendAccept,GTA.Control.FrontendCancel,GTA.Control.FrontendLeft,GTA.Control.FrontendRight}.Any(Game.IsControlPressed);}
                else
                {
                    if(Game.IsControlJustPressed(GTA.Control.FrontendLeft))selected=HudMath.MoveRadial(selected,-1);
                    if(Game.IsControlJustPressed(GTA.Control.FrontendRight))selected=HudMath.MoveRadial(selected,1);
                    if(Game.IsControlJustPressed(GTA.Control.FrontendAccept)){Select();return;}
                    if(Game.IsControlJustPressed(GTA.Control.FrontendCancel)){Back();return;}
                }
            }
            bool ready = Visuals.Has("PSRP_Inventory", "RadMenu8");
            if (ready)
            {
                for (int i = 0; i < 8; i++) Visuals.Sprite("PSRP_Inventory", "RadMenu" + (i + 1), x, y, 1920 * s, 1080 * s, i == selected && slots[i]!=null ? Color.FromArgb(210,255,0,0) : Color.FromArgb(slots[i]==null?80:170,0,0,0));
                Visuals.Sprite("PSRP_Inventory", "RadMiddle", x, y, 1920 * s, 1080 * s, Color.FromArgb(225,0,0,0));
            }
            else Visuals.Rect(Visuals.Width / 2 - 280 * s, 280, 560 * s, 490 * s, Color.FromArgb(190,0,0,0));
            for (int i = 0; i < 8; i++)
            {
                if(slots[i]==null)continue;
                float ix = x + (centers[i].X - 50) * s, iy = y + (centers[i].Y - 50) * s;
                if (!ready || !Visuals.Sprite("PSRP_Inventory", slots[i].Icon, ix, iy, 100 * s, 100 * s, Color.White))
                    Visuals.Text((i == selected ? "~r~" : "") + slots[i].Label, ix - 20 * s, iy + 15 * s, .23f, Color.White);
            }
            Visuals.Label((slots[selected]?.Label??"Vazio")+(slots[selected]?.Detail==null?"":"~n~"+slots[selected].Detail()),x+958*s,y+520*s,.5f*s,Color.White,GTA.UI.Font.Pricedown,GTA.UI.Alignment.Center,200*s);
            Visuals.Label(keyboard?"~b~Q / E~s~: navegar   ~b~Enter~s~: selecionar~n~Mouse: selecionar / clicar   Direito: voltar~n~Backspace: voltar   "+InterfaceOptions.RadialKey+": fechar":"Direcional: navegar   A: selecionar   B: voltar",x+960*s,y+800*s,.5f*s,Color.White,GTA.UI.Font.ChaletLondon,GTA.UI.Alignment.Center);
        }
        public void Reload() { Close(); }
        public void Save() { }
        public void Dispose() { Close(); }
    }
}
