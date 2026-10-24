using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class WardrobeSystem : ISystem
    {
        public string Name => "wardrobe";
        readonly ObjectPool pool; readonly ShiftGate gate; readonly Func<bool> houseAccess; readonly Action back; readonly NativeMenu menu; readonly WardrobeOptions options;
        WardrobeOutfit preview; Ped actor; string previewProfile;
        Vector3 Point => new Vector3(options.Point.X,options.Point.Y,options.Point.Z);
        bool Access => houseAccess() && Game.Player.Character.Position.DistanceTo(Point) <= 2.5f;
        public WardrobeSystem(ObjectPool pool, ShiftGate gate, Func<bool> houseAccess, Action back)
        {
            this.pool = pool; this.gate = gate; this.houseAccess = houseAccess; this.back = back;
            var defaults = WardrobeDefaults.Load(); options = Store.LoadDef("Jobs/Wardrobe.json",()=>defaults);
            if (options == null || !options.Valid) { Log.Write("Invalid Wardrobe.json; embedded defaults used, original retained."); options = defaults; }
            menu = new NativeMenu("Guardarroupa", "ROUPAS / ACESSORIOS / CONJUNTOS"); pool.Add(menu); Visuals.Register(menu,"core");
        }
        void Add(string title,string description,Action action)
        {
            var row = new NativeItem(title,description); row.Activated += (s,e) => {
                try { if (!Access || !Wallet.Wardrobe.Valid) { CancelPreview(); Notify.Show("Use o guardarroupa do seu apartamento, com dados validos."); return; } action(); }
                catch (Exception ex) { try { CancelPreview(); } catch(Exception restore) { Log.Error("wardrobe restore",restore); } Log.Error("wardrobe action",ex); Notify.Show("Operacao nao concluida. Confira o log; biblioteca preservada em falha de gravacao."); }
            }; menu.Add(row);
        }
        static WardrobeOutfit Capture(Ped p,string id,string name)
        {
            var o = new WardrobeOutfit { Id=id,Name=name,Model=p.Model.Hash };
            foreach(int slot in WardrobeRules.ComponentSlots) o.Components.Add(new WardrobePiece { Slot=slot,Drawable=Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION,p,slot),Texture=Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION,p,slot),Palette=Function.Call<int>(Hash.GET_PED_PALETTE_VARIATION,p,slot) });
            foreach(int slot in WardrobeRules.PropSlots) { int draw=Function.Call<int>(Hash.GET_PED_PROP_INDEX,p,slot); o.Props.Add(new WardrobePiece { Slot=slot,Drawable=draw,Texture=draw<0?0:Function.Call<int>(Hash.GET_PED_PROP_TEXTURE_INDEX,p,slot) }); }
            if (!o.Valid) throw new InvalidOperationException("Unsupported clothing snapshot."); return o;
        }
        static int DrawCount(Ped p,int slot,bool prop) => Function.Call<int>(prop?Hash.GET_NUMBER_OF_PED_PROP_DRAWABLE_VARIATIONS:Hash.GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS,p,slot);
        static int TextureCount(Ped p,WardrobePiece piece,bool prop) => piece.Drawable<0?0:Function.Call<int>(prop?Hash.GET_NUMBER_OF_PED_PROP_TEXTURE_VARIATIONS:Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS,p,piece.Slot,piece.Drawable);
        static bool Compatible(Ped p,WardrobeOutfit o)
        {
            if (!o.Valid || p.Model.Hash!=o.Model) return false;
            foreach(var piece in o.Components) if (!WardrobeRules.Available(piece,false,DrawCount(p,piece.Slot,false),TextureCount(p,piece,false))) return false;
            foreach(var piece in o.Props) if (!WardrobeRules.Available(piece,true,DrawCount(p,piece.Slot,true),TextureCount(p,piece,true))) return false;
            return true;
        }
        static void Write(Ped p,WardrobeOutfit o)
        {
            if (p==null || !p.Exists() || p!=Game.Player.Character || p.Model.Hash!=o.Model) throw new InvalidOperationException("Clothing owner changed.");
            foreach(var piece in o.Components) Function.Call(Hash.SET_PED_COMPONENT_VARIATION,p,piece.Slot,piece.Drawable,piece.Texture,piece.Palette);
            foreach(var piece in o.Props) { if(piece.Drawable<0)Function.Call(Hash.CLEAR_PED_PROP,p,piece.Slot); else Function.Call(Hash.SET_PED_PROP_INDEX,p,piece.Slot,piece.Drawable,piece.Texture,true); }
            var actual=Capture(p,"verify","Verificacao");
            if (o.Components.Any(v=>!actual.Components.Any(a=>a.Slot==v.Slot && a.Drawable==v.Drawable && a.Texture==v.Texture && a.Palette==v.Palette)) || o.Props.Any(v=>!actual.Props.Any(a=>a.Slot==v.Slot && a.Drawable==v.Drawable && a.Texture==v.Texture))) throw new InvalidOperationException("Clothing variation did not apply.");
        }
        static void ApplyChecked(Ped p,WardrobeOutfit outfit)
        {
            if (!Compatible(p,outfit)) { Notify.Show("Conjunto incompativel com o ped atual ou roupas/DLC disponiveis. Nada alterado."); return; }
            var old=Capture(p,"rollback","Anterior");
            try { Write(p,outfit); } catch { Write(p,old); throw; }
        }
        bool Idle() { if(gate.Owner==null || gate.Owner==Name)return true; Notify.Show("Encerre a atividade de "+gate.Owner+".");return false; }
        public void OpenMenu()
        {
            CancelPreview();
            if (!Access || !Wallet.Wardrobe.Valid) { Notify.Show("Entre no seu Eclipse e aproxime-se do guardarroupa no andar superior."); return; }
            pool.HideAll();menu.Clear();menu.Name="Guardarroupa / "+Wallet.Wardrobe.Outfits.Count+"/"+options.Capacity;
            Add("Salvar roupa atual","Conjunto por perfil e modelo. Nome novo; substituicao apenas pela opcao explicita do conjunto.",()=>SaveCurrent(null));
            Add("Editar pecas e acessorios","Previa reversivel. Mantem cabeca/cabelo/modelo; usa variacoes realmente disponiveis no ped atual.",BeginEdit);
            foreach(var outfit in Wallet.Wardrobe.Outfits) { string id=outfit.Id; Add(outfit.Name+(outfit.Model==Game.Player.Character.Model.Hash?"":" / outro modelo"),"Vestir, substituir, renomear ou excluir. Nunca troca seu modelo de personagem.",()=>OpenOutfit(id)); }
            Add("Voltar a moradia","Apartamento do perfil ativo.",back);menu.Visible=true;
        }
        void OpenOutfit(string id)
        {
            var o=Wallet.Wardrobe.Outfits.Find(v=>v.Id==id);if(o==null)return;menu.Clear();menu.Name=o.Name;
            Add("Vestir conjunto","Valida todas as pecas antes de mudar. Incompativel nao altera nada.",()=>{if(!Idle() || !gate.Acquire(Name))return;try{var current=Wallet.Wardrobe.Outfits.Find(v=>v.Id==id);if(current!=null)ApplyChecked(Game.Player.Character,current);}finally{gate.Release(Name);}});
            Add("Substituir pela roupa atual","Substituicao explicita deste conjunto, preservando nome/id. Requer mesmo modelo.",()=>SaveCurrent(id));
            Add("Renomear","Nao muda a roupa vestida.",()=>Rename(id));
            Add("Excluir da biblioteca","Abre confirmacao; nao remove a roupa do personagem.",()=>ConfirmDelete(id));
            Add("Voltar","Biblioteca.",OpenMenu);menu.Visible=true;
        }
        void SaveCurrent(string replaceId)
        {
            if(!Idle())return;string profile=Profile.Id;var ped=Game.Player.Character;int model=ped.Model.Hash;
            var old=replaceId==null?null:Wallet.Wardrobe.Outfits.Find(o=>o.Id==replaceId);
            if(replaceId!=null && (old==null || old.Model!=model)){Notify.Show("Substituicao requer conjunto do mesmo modelo.");return;}
            string name=old?.Name ?? Game.GetUserInput(WindowTitle.EnterMessage60,"Roupa "+(Wallet.Wardrobe.Outfits.Count+1),32)?.Trim();
            if(!Access || profile!=Profile.Id || ped!=Game.Player.Character || model!=ped.Model.Hash || !WardrobeRules.ValidName(name))return;
            var outfit=Capture(ped,replaceId??Guid.NewGuid().ToString("N"),name);
            if(!Wallet.WardrobeSave(options,outfit,replaceId!=null,Guid.NewGuid().ToString("N"),out _)){Notify.Show("Confira nome duplicado, limite ou dados. Biblioteca nao alterada.");return;}
            FinishPreview();Notify.Show("Roupa salva.");OpenMenu();
        }
        void Rename(string id)
        {
            if(!Idle())return;var old=Wallet.Wardrobe.Outfits.Find(o=>o.Id==id);if(old==null)return;string profile=Profile.Id;
            string name=Game.GetUserInput(WindowTitle.EnterMessage60,old.Name,32)?.Trim();
            if(profile!=Profile.Id || !Access || !WardrobeRules.ValidName(name))return;
            if(!Wallet.WardrobeRename(id,name,Guid.NewGuid().ToString("N"),out _))Notify.Show("Nome indisponivel ou dados invalidos.");OpenMenu();
        }
        void ConfirmDelete(string id)
        {
            menu.Clear();menu.Name="Confirmar exclusao";Add("Sim, excluir conjunto","Apenas biblioteca; roupa vestida e dinheiro permanecem.",()=>{if(!Idle())return;if(!Wallet.WardrobeDelete(id,Guid.NewGuid().ToString("N"),out _))Notify.Show("Conjunto nao encontrado/dados invalidos.");OpenMenu();});Add("Cancelar","Manter conjunto.",()=>OpenOutfit(id));menu.Visible=true;
        }
        void BeginEdit()
        {
            if(!gate.Acquire(Name)){Notify.Show("Encerre a atividade de "+gate.Owner+".");return;}
            actor=Game.Player.Character;previewProfile=Profile.Id;preview=Capture(actor,"preview","Anterior");OpenSlots();
        }
        static string ComponentName(int slot) => new[]{"Cabeca","Mascara","Cabelo","Torso/bracos","Calcas","Bolsa","Sapatos","Acessorios","Camiseta","Colete","Decalques","Blusa/jaqueta"}[slot];
        static string PropName(int slot) => slot==0?"Chapeu":slot==1?"Oculos":slot==2?"Orelhas":slot==6?"Relogio":"Pulseira";
        void OpenSlots()
        {
            menu.Clear();menu.Name="Editar / previa";
            foreach(int s in WardrobeRules.ComponentSlots){int slot=s;Add(ComponentName(slot),"Mudar peca/textura do ped atual.",()=>OpenPiece(slot,false));}
            foreach(int s in WardrobeRules.PropSlots){int slot=s;Add(PropName(slot),"Inclui opcao sem acessorio.",()=>OpenPiece(slot,true));}
            Add("Salvar roupa editada","Salva biblioteca e mantem a previa ao confirmar.",()=>SaveCurrent(null));
            Add("Concluir edicao","Mantem roupa atual; salvar na biblioteca e uma acao separada.",()=>{FinishPreview();OpenMenu();});
            Add("Cancelar e restaurar","Esc/fechar ou sair do local tambem cancela a previa.",OpenMenu);menu.Visible=true;
        }
        void OpenPiece(int slot,bool prop)
        {
            var current=Capture(actor,"edit","Edicao");var piece=(prop?current.Props:current.Components).Find(p=>p.Slot==slot);menu.Clear();menu.Name=(prop?PropName(slot):ComponentName(slot))+" / "+piece.Drawable+" / textura "+piece.Texture;
            Add("Peca anterior","Previa; novas pecas iniciam com textura zero.",()=>EditPiece(slot,prop,-1,false));Add("Proxima peca","Previa; novas pecas iniciam com textura zero.",()=>EditPiece(slot,prop,1,false));
            Add("Textura anterior","Variacoes da peca selecionada.",()=>EditPiece(slot,prop,-1,true));Add("Proxima textura","Variacoes da peca selecionada.",()=>EditPiece(slot,prop,1,true));
            Add("Voltar as pecas","Continua a previa; cancelar restaura todo o conjunto original.",OpenSlots);menu.Visible=true;
        }
        void EditPiece(int slot,bool prop,int delta,bool texture)
        {
            if(preview==null || actor!=Game.Player.Character || preview.Model!=actor.Model.Hash)return;
            var current=Capture(actor,"edit","Edicao");var piece=(prop?current.Props:current.Components).Find(p=>p.Slot==slot);
            int count=texture?TextureCount(actor,piece,prop):DrawCount(actor,slot,prop);
            if(count==0 || texture && piece.Drawable<0){Notify.Show("Sem variacoes disponiveis nesta peca/ped.");return;}
            if(texture)piece.Texture=WardrobeRules.Cycle(piece.Texture,delta,count,false);
            else{piece.Drawable=WardrobeRules.Cycle(piece.Drawable,delta,count,prop);piece.Texture=0;piece.Palette=0;}
            ApplyChecked(actor,current);OpenPiece(slot,prop);
        }
        void FinishPreview(){preview=null;actor=null;previewProfile=null;gate.Release(Name);}
        void CancelPreview()
        { try{if(preview!=null && actor!=null && actor.Exists() && actor==Game.Player.Character && actor.Model.Hash==preview.Model)Write(actor,preview);}finally{FinishPreview();} }
        public void Tick()
        {
            try
            {
                if(preview!=null && (!Access || previewProfile!=Profile.Id || actor!=Game.Player.Character || actor.Model.Hash!=preview.Model || InterfaceOptions.ModalOpen || !menu.Visible)){CancelPreview();menu.Visible=false;}
                if(menu.Visible && !Access)menu.Visible=false;
                if(!Access || InterfaceOptions.ModalOpen || pool.AreAnyVisible)return;
                World.DrawMarker(MarkerType.Cylinder,Point-new Vector3(0,0,1),Vector3.Zero,Vector3.Zero,new Vector3(1,1,.2f),Color.FromArgb(130,70,150,240));
                GTA.UI.Screen.ShowHelpTextThisFrame("Pressione ~INPUT_CONTEXT~ para abrir o guardarroupa");if(Game.IsControlJustPressed(GTA.Control.Context))OpenMenu();
            }
            catch(Exception ex){try{CancelPreview();}finally{menu.Visible=false;}Log.Error("wardrobe tick",ex);}
        }
        public void OnKey(KeyEventArgs e){}
        public void Save(){}
        public void Reload(){try{CancelPreview();}finally{menu.Visible=false;}}
        public void Dispose(){Reload();}
    }
}
