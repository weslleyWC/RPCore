using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Native;

namespace RPCore
{
    // Quantities remain in InventorySystem; selections and recipe ingredients are references only.
    public sealed class InventoryGridView
    {
        readonly InventorySystem inventory;readonly NeedsSystem needs;
        readonly InventoryNavigation personal=new InventoryNavigation(),storage=new InventoryNavigation();
        readonly List<string> ingredients=new List<string>();
        List<string> storageLayout=new List<string>();Dictionary<string,int> stored=new Dictionary<string,int>();
        Func<Dictionary<string,int>> contents;Func<string,bool,int,bool> transfer;Func<bool> access;Action closed;
        string storageLabel,dropping;float storageCapacity;int moving=-1,dropStarted,craftIndex,craftAmount=1;bool storageSide,craftFocus,controllerDrop;
        public bool Visible{get;private set;}
        public bool StorageOpen=>Visible && contents!=null;
        public bool Editing=>moving>=0 || dropping!=null || craftFocus || StorageOpen;
        InventoryNavigation Nav=>storageSide?storage:personal;
        List<string> Slots=>storageSide?storageLayout:inventory.Layout;
        string Selected=>Nav.Selected>=0 && Nav.Selected<Slots.Count?Slots[Nav.Selected]:null;
        public InventoryGridView(InventorySystem inventory,NeedsSystem needs){this.inventory=inventory;this.needs=needs;}
        public void Open(){Visible=true;InterfaceOptions.ModalOpen=true;moving=-1;dropping=null;craftFocus=false;ingredients.Clear();personal.Select(personal.Selected,inventory.Layout.Count);}
        public void OpenStorage(string label,float capacity,Func<Dictionary<string,int>> snapshot,Func<string,bool,int,bool> move,Func<bool> permitted,Action onClose)
        {
            if(snapshot==null || move==null || permitted==null || onClose==null || !permitted())throw new InvalidOperationException("Storage unavailable.");
            if(Visible)Close();Open();storageLabel=label;storageCapacity=capacity;contents=snapshot;transfer=move;access=permitted;closed=onClose;storageSide=false;RefreshStorage();
        }
        public void Close(bool save=true)
        {
            var callback=closed;closed=null;Visible=false;contents=null;transfer=null;access=null;storageSide=false;stored.Clear();storageLayout.Clear();ingredients.Clear();dropping=null;moving=-1;craftFocus=false;
            try{if(save)inventory.Save();}catch(Exception ex){Log.Error("inventory close save",ex);Notify.Show("Nao foi possivel gravar o inventario; confira RPCore.log.");}
            finally{callback?.Invoke();}
        }
        void RefreshStorage(){stored=contents();storageLayout=InventoryLayout.Reconcile(storageLayout,stored.Where(p=>p.Value>0).Select(p=>p.Key),24);storage.Select(storage.Selected,storageLayout.Count);}
        void Move(int dx,int dy){if(craftFocus){if(craftIndex==3 && dy!=0)craftAmount=Math.Max(1,Math.Min(Math.Max(1,inventory.CraftMaximum(inventory.Recipe(ingredients))),craftAmount-dy));else craftIndex=Math.Max(0,Math.Min(3,craftIndex+dx));return;}Nav.Move(dx,dy,Slots.Count);}
        void Relocate(){if(StorageOpen || craftFocus)return;if(moving<0){if(Selected!=null)moving=personal.Selected;return;}inventory.MoveSlotSaved(moving,personal.Selected);moving=-1;}
        void AddIngredient(){if(StorageOpen || craftFocus || moving>=0 || Selected==null || ingredients.Count==3 || ingredients.Contains(Selected))return;ingredients.Add(Selected);craftAmount=1;}
        void Activate(bool stack=false)
        {
            if(StorageOpen){if(Selected==null)return;int n=stack?(storageSide?stored[Selected]:inventory.Count(Selected)):1;if(transfer(Selected,!storageSide,n))RefreshStorage();return;}
            if(craftFocus)
            {
                if(craftIndex<3){if(craftIndex<ingredients.Count)ingredients.RemoveAt(craftIndex);return;}
                var recipe=inventory.Recipe(ingredients);if(recipe!=null)inventory.CraftSaved(recipe,stack?inventory.CraftMaximum(recipe):craftAmount);return;
            }
            if(moving>=0){Relocate();return;}if(Selected!=null)inventory.UseSelected(Selected);
        }
        void Discard(bool stack){string id=dropping;dropping=null;if(id!=null && inventory.DropSaved(id,stack?inventory.Count(id):1))Notify.Show("Descartado: "+id);}
        public void OnKey(KeyEventArgs e)
        {
            if(e.KeyCode==Cfg.InventoryKey || e.KeyCode==Keys.Back || e.KeyCode==Keys.Escape || StorageOpen && e.KeyCode==Cfg.StorageKey){Close();return;}
            if(dropping!=null)return;
            if(!Editing && inventory.TryQuickUse(e))return;
            if(e.KeyCode==Keys.Tab){if(StorageOpen)storageSide=!storageSide;else{craftFocus=!craftFocus;moving=-1;}return;}
            if(e.KeyCode==Keys.Left)Move(-1,0);else if(e.KeyCode==Keys.Right)Move(1,0);else if(e.KeyCode==Keys.Up)Move(0,-1);else if(e.KeyCode==Keys.Down)Move(0,1);
            else if(e.KeyCode==Keys.PageDown)Nav.Select(Nav.Selected+24,Slots.Count);else if(e.KeyCode==Keys.PageUp)Nav.Select(Nav.Selected-24,Slots.Count);
            else if(e.KeyCode==Keys.Home)Nav.Select(0,Slots.Count);else if(e.KeyCode==Keys.End)Nav.Select(Slots.Count-1,Slots.Count);
            else if(e.KeyCode==Keys.Enter)Activate(e.Shift);
            else if(e.KeyCode==Keys.C)Relocate();else if(e.KeyCode==Keys.T)AddIngredient();
            else if(e.KeyCode==Keys.Delete && !StorageOpen && !craftFocus && moving<0 && Selected!=null){dropping=Selected;dropStarted=Game.GameTime;controllerDrop=false;}
        }
        public void Tick()
        {
            if(!Visible)return;var p=Game.Player.Character;
            personal.Select(personal.Selected,inventory.Layout.Count);
            if(p.IsDead || Game.IsPaused || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) || StorageOpen && !access()){Close();return;}
            if(StorageOpen)RefreshStorage();
            foreach(var c in new[]{GTA.Control.Attack,GTA.Control.Attack2,GTA.Control.Aim,GTA.Control.SelectWeapon,GTA.Control.NextWeapon,GTA.Control.PrevWeapon,GTA.Control.Context,GTA.Control.Phone,GTA.Control.FrontendPause,GTA.Control.FrontendPauseAlternate})Game.DisableControlThisFrame(c);
            if(dropping!=null){bool held=controllerDrop?Game.IsControlPressed(GTA.Control.FrontendLb):Game.IsKeyPressed(Keys.Delete);if(!held)Discard(false);else if(unchecked(Game.GameTime-dropStarted)>=900)Discard(true);}
            if(!Function.Call<bool>(Hash.IS_USING_KEYBOARD_AND_MOUSE,0))
            {
                if(dropping==null){if(Game.IsControlJustPressed(GTA.Control.FrontendLeft))Move(-1,0);if(Game.IsControlJustPressed(GTA.Control.FrontendRight))Move(1,0);if(Game.IsControlJustPressed(GTA.Control.FrontendUp))Move(0,-1);if(Game.IsControlJustPressed(GTA.Control.FrontendDown))Move(0,1);
                if(Game.IsControlJustPressed(GTA.Control.FrontendAccept))Activate(Game.IsControlPressed(GTA.Control.FrontendLb));
                if(Game.IsControlJustPressed(GTA.Control.FrontendX))Relocate();if(Game.IsControlJustPressed(GTA.Control.FrontendY))AddIngredient();
                if(Game.IsControlJustPressed(GTA.Control.FrontendRb)){if(StorageOpen)storageSide=!storageSide;else{craftFocus=!craftFocus;moving=-1;}}
                if(Game.IsControlJustPressed(GTA.Control.FrontendLb) && !StorageOpen && !craftFocus && moving<0 && Selected!=null){dropping=Selected;dropStarted=Game.GameTime;controllerDrop=true;}}
                if(Game.IsControlJustPressed(GTA.Control.FrontendCancel)){Close();return;}
            }
            if(Visible)Draw();
        }
        void Draw()
        {
            float design=StorageOpen?1920:1320;
            float s=Math.Min(Math.Min(1,Visuals.Scale),Math.Min((Visuals.Width-2*Visuals.InsetX)/design,(1080-2*Visuals.InsetY)/1080)),ox=Visuals.InsetX,oy=Visuals.InsetY;
            Action<string,float,float,float,Color,GTA.UI.Alignment,float> text=(t,x,y,z,c,a,w)=>Visuals.Label(t,ox+x*s,oy+y*s,z*s,c,GTA.UI.Font.ChaletLondon,a,w*s);
            if(!Visuals.Sprite("PSRP_Inventory","Background",ox,oy,1920*s,1080*s,Color.FromArgb(245,17,17,20)))Visuals.Rect(ox+107*s,oy+126*s,(StorageOpen?1770:1200)*s,780*s,Color.FromArgb(235,17,17,20));
            Visuals.Sprite("PSRP_Inventory","Top",ox,oy,1920*s,1080*s,Color.FromArgb(255,25,25,31));
            text(Profile.Id.ToUpperInvariant(),130,150,.38f,Color.White,GTA.UI.Alignment.Left,600);
            text(inventory.Weight.ToString("0.##")+" / "+Cfg.MaxWeight.ToString("0.##")+" kg",930,150,.32f,Color.White,GTA.UI.Alignment.Right,0);
            Visuals.Rect(ox+130*s,oy+188*s,810*s,20*s,Color.FromArgb(255,45,45,50));
            float fraction=Cfg.MaxWeight>0?Math.Min(1,inventory.Weight/Cfg.MaxWeight):1;
            Visuals.Rect(ox+130*s,oy+188*s,810*s*fraction,20*s,fraction>.9f?Color.OrangeRed:Color.FromArgb(255,70,180,120));
            DrawGrid(false,140,personal,inventory.Layout,s,ox,oy,text);
            if(StorageOpen)
            {
                text(storageLabel+" / "+stored.Sum(k=>(inventory.Defs.TryGetValue(k.Key,out var def)?def.Weight:.1f)*k.Value).ToString("0.##")+" / "+storageCapacity+" kg",1040,150,.32f,Color.White,GTA.UI.Alignment.Left,800);
                DrawGrid(true,1040,storage,storageLayout,s,ox,oy,text);
                text("TAB / RB: alternar lado   ENTER / A: transferir 1   SHIFT+ENTER / LB+A: pilha",140,815,.27f,Color.White,GTA.UI.Alignment.Left,1650);
                text("BACKSPACE / B: fechar armazenamento",140,855,.27f,Color.White,GTA.UI.Alignment.Left,1650);return;
            }
            if(Selected!=null)
            {
                inventory.Defs.TryGetValue(Selected,out var def);Visuals.Rect(ox+1000*s,oy+230*s,270*s,450*s,Color.FromArgb(200,0,0,0));
                text(def?.Name??Selected,1014,245,.32f,Color.White,GTA.UI.Alignment.Left,246);
                text("Quantidade: "+inventory.Count(Selected),1014,292,.27f,Color.White,GTA.UI.Alignment.Left,246);
                text("Descricao: "+(def!=null?inventory.Describe(def):"Definicao ausente; item preservado."),1014,330,.27f,Color.White,GTA.UI.Alignment.Left,246);
                if(def!=null && def.Weapon==null && !Selected.StartsWith("ammo_",StringComparison.Ordinal))text("Fome +"+def.Hunger+"~n~Sede +"+def.Thirst+"~n~Sono +"+inventory.Energy(def)+"~n~Vida +"+def.Health,1014,490,.27f,Color.White,GTA.UI.Alignment.Left,246);
            }
            DrawCraft(s,ox,oy,text);
            bool keyboard=Function.Call<bool>(Hash.IS_USING_KEYBOARD_AND_MOUSE,0);
            text(keyboard?"1-6: primeira linha   ENTER: usar   C: mover   T: ingrediente   TAB: receitas":"A: usar   X: mover   Y: ingrediente   RB: receitas   B: fechar",140,920,.25f,Color.White,GTA.UI.Alignment.Left,1150);
            text(keyboard?"DELETE: descartar 1 / segure: pilha   PGUP/PGDN: rolar   BACKSPACE: fechar":"LB: descartar 1 / segure: pilha   Direcional: navegar / quantidade",140,953,.25f,Color.White,GTA.UI.Alignment.Left,1150);
            if(inventory.UsingItem)text("Usando item...",1000,720,.3f,Color.Yellow,GTA.UI.Alignment.Left,270);
        }
        void DrawGrid(bool container,float start,InventoryNavigation nav,List<string> slots,float s,float ox,float oy,Action<string,float,float,float,Color,GTA.UI.Alignment,float> text)
        {
            for(int local=0;local<24;local++)
            {
                int index=nav.FirstRow*6+local;float x=start+local%6*130,y=230+local/6*130;
                bool active=container==storageSide && !craftFocus;Color color=active && index==nav.Selected?Color.FromArgb(150,60,120,255):Color.FromArgb(255,24,27,36);
                if(!Visuals.Sprite("PSRP_Inventory","InventoryBox",ox+x*s,oy+y*s,140*s,140*s,color))Visuals.Rect(ox+x*s,oy+y*s,140*s,140*s,color);
                if(!container && index<6)text((index+1).ToString(),x+120,y+4,.25f,Color.Yellow,GTA.UI.Alignment.Right,0);
                string id=index<slots.Count?slots[index]:null;if(id==null)continue;
                inventory.Defs.TryGetValue(id,out var def);int count=container?stored[id]:inventory.Count(id);
                Visuals.Label((def?.Name??id).ToUpperInvariant(),ox+(x+67.5f)*s,oy+(y+14.5f)*s,.32f*s,Color.White,GTA.UI.Font.ChaletComprimeCologne,GTA.UI.Alignment.Center,118*s);
                string icon=def?.Icon;if(icon==null && (Visuals.Bindings.ItemIcons==null || !Visuals.Bindings.ItemIcons.TryGetValue(id,out icon)))icon=id;
                if(!Visuals.Sprite("PSRP_Inventory",icon,ox+(x+28)*s,oy+(y+34)*s,80*s,80*s,Color.White))text("?",x+67.5f,y+58,.7f,Color.Gray,GTA.UI.Alignment.Center,0);
                text("x"+count,x+17,y+107,.23f,Color.White,GTA.UI.Alignment.Left,0);text(((def?.Weight??.1f)*count).ToString("0.##")+"kg",x+122,y+107,.23f,Color.White,GTA.UI.Alignment.Right,0);
                if(!container && moving==index)Visuals.Rect(ox+x*s,oy+(y+136)*s,140*s,4*s,Color.Yellow);
            }
            int rows=Math.Max(4,(slots.Count+5)/6);if(rows>4){float x=start+790;Visuals.Rect(ox+x*s,oy+250*s,10*s,500*s,Color.DimGray);Visuals.Rect(ox+x*s,oy+(250+400f*nav.FirstRow/(rows-4))*s,10*s,100*s,Color.White);}
        }
        void DrawCraft(float s,float ox,float oy,Action<string,float,float,float,Color,GTA.UI.Alignment,float> text)
        {
            var recipe=inventory.Recipe(ingredients);
            craftAmount=Math.Max(1,Math.Min(craftAmount,Math.Max(1,inventory.CraftMaximum(recipe))));
            for(int i=0;i<4;i++)
            {
                float x=141+i*120+(i==3?40:0),y=775;string id=i<3?(i<ingredients.Count?ingredients[i]:null):recipe?.Result;
                var color=craftFocus && craftIndex==i?Color.FromArgb(150,60,120,255):Color.FromArgb(255,24,27,36);
                if(!Visuals.Sprite("PSRP_Inventory","InventoryBox",ox+x*s,oy+y*s,120*s,120*s,color))Visuals.Rect(ox+x*s,oy+y*s,120*s,120*s,color);
                if(id!=null && inventory.Defs.TryGetValue(id,out var def)){Visuals.Sprite("PSRP_Inventory",def.Icon??id,ox+(x+25)*s,oy+(y+20)*s,70*s,70*s,Color.White);text(def.Name,x+60,y+92,.23f,Color.White,GTA.UI.Alignment.Center,112);}
                else if(i==3 && ingredients.Count>=2)Visuals.Sprite("PSRP_Inventory","erroricon",ox+(x+25)*s,oy+(y+20)*s,70*s,70*s,Color.White);
            }
            text("+",251,810,.5f,Color.White,GTA.UI.Alignment.Left,0);text("+",371,810,.5f,Color.White,GTA.UI.Alignment.Left,0);text("=",511,810,.5f,Color.White,GTA.UI.Alignment.Left,0);
            text(recipe==null?"Selecione ate 3 materiais. Alterne para receitas e remova com Enter/A.":recipe.Name+"~n~Disponivel: "+inventory.CraftMaximum(recipe)+" / lote: "+craftAmount+"~n~Cima/baixo: quantidade~n~ENTER/A: fabricar",710,785,.25f,Color.White,GTA.UI.Alignment.Left,250);
        }
    }
}
