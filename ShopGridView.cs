using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Native;
namespace RPCore
{
    // A shop is a read-only view of inventory and a separately selected sale catalogue.
    // Quantity selection never removes cash or items; purchase commits only through BuySaved.
    public sealed class ShopGridView
    {
        readonly InventorySystem inventory;
        readonly Func<bool> accessible;
        readonly Func<ItemDef,int,bool> buy;
        readonly InventoryNavigation nav=new InventoryNavigation(),personal=new InventoryNavigation();
        List<ShopCategory> categories=new List<ShopCategory>();
        string label,feedback;int category,quantity=1,feedbackEnd,clickAction=-1,clickCategory=-1,opened;
        bool mouseReady,mouseHeld,clickReady,controllerReady;
        public bool Visible{get;private set;}
        List<ItemDef> Items=>categories.Count==0?new List<ItemDef>():categories[category].Items;
        ItemDef Selected=>nav.Selected<Items.Count?Items[nav.Selected]:null;
        public ShopGridView(InventorySystem inventory,Func<bool> accessible,Func<ItemDef,int,bool> buy){this.inventory=inventory;this.accessible=accessible;this.buy=buy;}
        public void Open(string name)
        {
            categories=ShopCatalogRules.Build(inventory.Defs.Values);label=name;category=0;quantity=1;feedback=null;nav.Select(0,Items.Count);personal.Select(0,inventory.Layout.Count);
            mouseReady=false;clickReady=false;controllerReady=false;clickAction=clickCategory=-1;opened=Game.GameTime;Visible=true;InterfaceOptions.ModalOpen=true;
        }
        public void Close(){Visible=false;mouseReady=false;clickReady=false;controllerReady=false;clickAction=clickCategory=-1;}
        void SetCategory(int value){if(categories.Count==0)return;category=ShopCatalogRules.WrapCategory(value,0,categories.Count);nav.Select(0,Items.Count);quantity=1;clickAction=clickCategory=-1;}
        void Move(int dx,int dy){int before=nav.Selected;nav.Move(dx,dy,Items.Count);if(before!=nav.Selected)quantity=1;}
        int Maximum()=>inventory.PurchaseMaximum(Selected);
        void ChangeQuantity(int delta)=>quantity=Math.Max(1,Math.Min(Math.Max(1,Maximum()),quantity+delta));
        void Purchase()
        {
            if(!Visible || !accessible())return;
            var item=Selected;int maximum=Maximum();
            if(item==null || quantity>maximum){Message("Sem saldo, capacidade ou quantidade valida.");return;}
            if(buy(item,quantity))Message("Comprado: "+item.Name+" x"+quantity+" / $"+ShopCatalogRules.Total(item,quantity));
            else Message("Compra nao confirmada. Confira saldo, peso e RPCore.log.");
            // Keep the explicit quantity selected; never silently buy fewer than requested.
        }
        void Message(string value){feedback=value;feedbackEnd=Game.GameTime+6000;}
        public void OnKey(KeyEventArgs e)
        {
            if(!Visible)return;
            if(e.KeyCode==Keys.Back || e.KeyCode==Keys.Escape || e.KeyCode==Cfg.InventoryKey){Close();return;}
            clickAction=clickCategory=-1;
            if((e.Modifiers & ~Keys.Shift)!=Keys.None)return;
            if(e.KeyCode==Keys.Tab)SetCategory(ShopCatalogRules.WrapCategory(category,e.Shift?-1:1,categories.Count));
            else if(e.KeyCode==Keys.Left)Move(-1,0);else if(e.KeyCode==Keys.Right)Move(1,0);else if(e.KeyCode==Keys.Up)Move(0,-1);else if(e.KeyCode==Keys.Down)Move(0,1);
            else if(e.KeyCode==Keys.PageDown){nav.Select(nav.Selected+24,Items.Count);quantity=1;}else if(e.KeyCode==Keys.PageUp){nav.Select(nav.Selected-24,Items.Count);quantity=1;}
            else if(e.KeyCode==Keys.Home){nav.Select(0,Items.Count);quantity=1;}else if(e.KeyCode==Keys.End){nav.Select(Items.Count-1,Items.Count);quantity=1;}
            else if(e.KeyCode==Keys.Oemplus || e.KeyCode==Keys.Add)ChangeQuantity(1);
            else if(e.KeyCode==Keys.OemMinus || e.KeyCode==Keys.Subtract)ChangeQuantity(-1);
            else if(e.KeyCode==Keys.M)quantity=Math.Max(1,Maximum());
            else if(e.KeyCode==Keys.Enter)Purchase();
        }
        public void Tick()
        {
            if(!Visible)return;
            if(!accessible()){Close();return;}
            foreach(var control in new[]{GTA.Control.MoveLeftRight,GTA.Control.MoveUpDown,GTA.Control.Sprint,GTA.Control.Jump,GTA.Control.Attack,GTA.Control.Attack2,GTA.Control.Aim,GTA.Control.SelectWeapon,GTA.Control.SelectPrevWeapon,GTA.Control.SelectNextWeapon,GTA.Control.SelectWeaponUnarmed,GTA.Control.SelectWeaponMelee,GTA.Control.SelectWeaponHandgun,GTA.Control.SelectWeaponShotgun,GTA.Control.SelectWeaponSmg,GTA.Control.SelectWeaponAutoRifle,GTA.Control.SelectWeaponSniper,GTA.Control.SelectWeaponHeavy,GTA.Control.NextWeapon,GTA.Control.PrevWeapon,GTA.Control.Context,GTA.Control.Phone,GTA.Control.Reload,GTA.Control.FrontendPause,GTA.Control.FrontendPauseAlternate})Game.DisableControlThisFrame(control);
            bool keyboard=Function.Call<bool>(Hash.IS_USING_KEYBOARD_AND_MOUSE,0);
            if(keyboard){controllerReady=false;MouseTick();}
            else
            {
                mouseReady=false;clickReady=false;clickAction=clickCategory=-1;
                // Opening Context (right d-pad) must not immediately move/purchase. A handover
                // also waits for fresh buttons rather than treating a held A as confirmation.
                if(!controllerReady){controllerReady=unchecked(Game.GameTime-opened)>200 && !new[]{GTA.Control.FrontendAccept,GTA.Control.FrontendCancel,GTA.Control.FrontendLeft,GTA.Control.FrontendRight,GTA.Control.FrontendUp,GTA.Control.FrontendDown,GTA.Control.FrontendLb,GTA.Control.FrontendRb,GTA.Control.FrontendX,GTA.Control.FrontendY}.Any(Game.IsControlPressed);}
                else
                {
                    if(Game.IsControlJustPressed(GTA.Control.FrontendCancel)){Close();return;}
                    if(Game.IsControlJustPressed(GTA.Control.FrontendLb))SetCategory(ShopCatalogRules.WrapCategory(category,-1,categories.Count));
                    if(Game.IsControlJustPressed(GTA.Control.FrontendRb))SetCategory(ShopCatalogRules.WrapCategory(category,1,categories.Count));
                    if(Game.IsControlJustPressed(GTA.Control.FrontendLeft))Move(-1,0);if(Game.IsControlJustPressed(GTA.Control.FrontendRight))Move(1,0);
                    if(Game.IsControlJustPressed(GTA.Control.FrontendUp))Move(0,-1);if(Game.IsControlJustPressed(GTA.Control.FrontendDown))Move(0,1);
                    if(Game.IsControlJustPressed(GTA.Control.FrontendX))ChangeQuantity(-1);if(Game.IsControlJustPressed(GTA.Control.FrontendY))ChangeQuantity(1);
                    if(Game.IsControlJustPressed(GTA.Control.FrontendAccept))Purchase();
                }
            }
            if(Visible)Draw(keyboard);
        }
        void MouseTick()
        {
            GTA.UI.Hud.ShowCursorThisFrame();
            foreach(var c in new[]{GTA.Control.LookLeftRight,GTA.Control.LookUpDown,GTA.Control.CursorAccept,GTA.Control.CursorCancel,GTA.Control.CursorScrollUp,GTA.Control.CursorScrollDown})Game.DisableControlThisFrame(c);
            bool held=Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED,0,(int)GTA.Control.CursorAccept);
            float s=InventoryMouseLayout.Scale(true,Visuals.Width,Visuals.InsetX,Visuals.InsetY,Visuals.Scale);
            if(!InventoryMouseLayout.Point(Function.Call<float>(Hash.GET_CONTROL_NORMAL,0,(int)GTA.Control.CursorX),Function.Call<float>(Hash.GET_CONTROL_NORMAL,0,(int)GTA.Control.CursorY),Visuals.Width,Visuals.InsetX,Visuals.InsetY,s,out var point)){mouseReady=false;clickReady=false;return;}
            if(!mouseReady){mouseReady=true;mouseHeld=held;clickReady=false;return;}
            bool pressed=held && !mouseHeld,released=!held && mouseHeld;mouseHeld=held;
            if(Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED,0,(int)GTA.Control.CursorCancel)){clickReady=false;clickAction=clickCategory=-1;return;}
            int hit=InventoryMouseLayout.Hit(point,true,nav.FirstRow,Items.Count);
            int scroll=Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED,0,(int)GTA.Control.CursorScrollDown)?1:Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED,0,(int)GTA.Control.CursorScrollUp)?-1:0;
            if(scroll!=0)
            {
                if(point.X>=1040 && point.X<1840 && point.Y>=230 && point.Y<770){nav.Scroll(scroll,Items.Count);hit=InventoryMouseLayout.Hit(point,true,nav.FirstRow,Items.Count);quantity=1;}
                else if(point.X>=140 && point.X<940 && point.Y>=230 && point.Y<770)personal.Scroll(scroll,inventory.Layout.Count);
                clickReady=false;
            }
            if(pressed)
            {
                clickReady=true;clickAction=ShopCatalogRules.ActionAt(point.X,point.Y);clickCategory=ShopCatalogRules.CategoryAt(point.X,point.Y,category,categories.Count);
                if(hit>=0){nav.Select(hit,Items.Count);quantity=1;clickReady=false;}
            }
            if(released && clickReady)
            {
                clickReady=false;
                if(clickCategory>=0 && clickCategory==ShopCatalogRules.CategoryAt(point.X,point.Y,category,categories.Count)){SetCategory(clickCategory);return;}
                int action=ShopCatalogRules.ActionAt(point.X,point.Y);if(action<0 || action!=clickAction)return;
                if(action==0)ChangeQuantity(-1);else if(action==1)ChangeQuantity(1);else if(action==2)quantity=Math.Max(1,Maximum());else Purchase();
            }
        }
        void Draw(bool keyboard)
        {
            float s=InventoryMouseLayout.Scale(true,Visuals.Width,Visuals.InsetX,Visuals.InsetY,Visuals.Scale),ox=Visuals.InsetX,oy=Visuals.InsetY;
            Action<float,float,float,float,Color> rect=(x,y,w,h,c)=>Visuals.Rect(ox+x*s,oy+y*s,w*s,h*s,c);
            Action<string,float,float,float,Color,GTA.UI.Alignment,float> text=(t,x,y,z,c,a,w)=>Visuals.Label(t,ox+x*s,oy+y*s,z*s,c,GTA.UI.Font.ChaletLondon,a,w*s);
            if(!Visuals.Sprite("PSRP_Inventory","Background",ox,oy,1920*s,1080*s,Color.FromArgb(245,17,17,20)))rect(107,126,1770,870,Color.FromArgb(240,17,17,20));
            Visuals.Sprite("PSRP_Inventory","Top",ox,oy,1920*s,1080*s,Color.FromArgb(255,25,25,31));
            text("MOCHILA / "+Profile.Id.ToUpperInvariant(),140,145,.35f,Color.White,GTA.UI.Alignment.Left,600);
            text(inventory.Weight.ToString("0.##")+" / "+Cfg.MaxWeight.ToString("0.##")+" kg",930,150,.3f,inventory.Overweight?Color.OrangeRed:Color.White,GTA.UI.Alignment.Right,0);
            text(label,1040,145,.35f,Color.White,GTA.UI.Alignment.Left,780);
            for(int i=category/4*4;i<Math.Min(categories.Count,category/4*4+4);i++)
            {float x=1040+(i%4)*195;rect(x,185,190,39,i==category?Color.FromArgb(200,60,120,255):Color.FromArgb(180,25,27,35));text(categories[i].Name,x+95,192,.25f,Color.White,GTA.UI.Alignment.Center,180);}
            DrawGrid(false,personal,inventory.Layout.Count,s,ox,oy,text);DrawGrid(true,nav,Items.Count,s,ox,oy,text);
            var item=Selected;
            text("Dinheiro: $"+Wallet.Cash,140,795,.35f,Color.LightGreen,GTA.UI.Alignment.Left,760);
            text("Compra com dinheiro. Produtos nao sao usados automaticamente.",140,845,.27f,Color.White,GTA.UI.Alignment.Left,780);
            text(categories.Count==0?"Nenhum produto a venda.":"Categoria "+(category+1)+" / "+categories.Count+"   Produto "+(Items.Count==0?0:nav.Selected+1)+" / "+Items.Count,140,890,.27f,Color.Gray,GTA.UI.Alignment.Left,780);
            rect(1040,790,60,50,Color.DimGray);rect(1210,790,60,50,Color.DimGray);rect(1280,790,120,50,Color.DimGray);
            rect(1430,790,390,50,item!=null && quantity<=Maximum()?Color.FromArgb(220,50,150,90):Color.FromArgb(200,85,40,40));
            text("-",1070,799,.36f,Color.White,GTA.UI.Alignment.Center,0);text("x"+quantity,1150,800,.3f,Color.White,GTA.UI.Alignment.Center,0);text("+",1240,799,.36f,Color.White,GTA.UI.Alignment.Center,0);
            text("MAX",1340,800,.3f,Color.White,GTA.UI.Alignment.Center,0);text("COMPRAR / $"+ShopCatalogRules.Total(item,quantity),1625,800,.3f,Color.White,GTA.UI.Alignment.Center,370);
            if(item!=null){text(item.Name+"  /  "+item.Weight.ToString("0.##")+" kg por unidade",1040,856,.29f,Color.White,GTA.UI.Alignment.Left,780);text(item.Desc??"",1040,895,.24f,Color.Gray,GTA.UI.Alignment.Left,780);}
            text(keyboard?"Setas: produto   TAB / SHIFT+TAB: categoria   +/-: quantidade   M: maximo   ENTER: comprar   BACKSPACE: sair":"Direcional: produto   LB/RB: categoria   X/Y: quantidade   A: comprar   B: sair",140,952,.25f,Color.White,GTA.UI.Alignment.Left,1680);
            if(feedback!=null && unchecked(Game.GameTime-feedbackEnd)<0)text(feedback,140,1000,.27f,Color.Yellow,GTA.UI.Alignment.Left,1680);
        }
        void DrawGrid(bool sale,InventoryNavigation navigation,int count,float s,float ox,float oy,Action<string,float,float,float,Color,GTA.UI.Alignment,float> text)
        {
            float start=sale?1040:140;
            for(int local=0;local<24;local++)
            {
                int index=navigation.FirstRow*6+local;float x=start+local%6*130,y=230+local/6*130;
                Color color=sale && index==navigation.Selected?Color.FromArgb(150,60,120,255):Color.FromArgb(255,24,27,36);
                if(!Visuals.Sprite("PSRP_Inventory","InventoryBox",ox+x*s,oy+y*s,140*s,140*s,color))Visuals.Rect(ox+x*s,oy+y*s,140*s,140*s,color);
                if(index>=count)continue;
                string id=sale?Items[index].Id:inventory.Layout[index];if(id==null)continue;
                inventory.Defs.TryGetValue(id,out var item);string icon=item?.Icon;
                if(icon==null && (Visuals.Bindings.ItemIcons==null || !Visuals.Bindings.ItemIcons.TryGetValue(id,out icon)))icon=id;
                Visuals.Label((item?.Name??id).ToUpperInvariant(),ox+(x+67.5f)*s,oy+(y+14.5f)*s,.32f*s,Color.White,GTA.UI.Font.ChaletComprimeCologne,GTA.UI.Alignment.Center,118*s);
                if(!Visuals.Sprite("PSRP_Inventory",icon,ox+(x+28)*s,oy+(y+34)*s,80*s,80*s,Color.White))text("?",x+68,y+58,.7f,Color.Gray,GTA.UI.Alignment.Center,0);
                text(sale?"$"+item.Price:"x"+inventory.Count(id),x+68,y+107,.25f,Color.White,GTA.UI.Alignment.Center,125);
            }
            int rows=Math.Max(4,(count+5)/6);if(rows>4){float x=start+790;Visuals.Rect(ox+x*s,oy+250*s,10*s,500*s,Color.DimGray);Visuals.Rect(ox+x*s,oy+(250+400f*navigation.FirstRow/(rows-4))*s,10*s,100*s,Color.White);}
        }
    }
}
