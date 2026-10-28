using System;
using System.Drawing;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed partial class GroomingSystem : ISystem
    {
        public string Name=>"grooming";
        readonly ObjectPool pool;readonly ShiftGate gate;readonly Func<bool> access;readonly Action back;readonly NativeMenu menu;readonly GroomingOptions options;
        readonly PersonalServiceMenu ui;int editingSection=-1;int editingOverlay=-1;GroomingLook draft;Ped actor;string profile;bool lease;
        static bool Freemode(int model)=>model==new Model("mp_m_freemode_01").Hash || model==new Model("mp_f_freemode_01").Hash;
        public GroomingSystem(ObjectPool pool,ShiftGate gate,Func<bool> access,Action back)
        {
            this.pool=pool;this.gate=gate;this.access=access;this.back=back;var defaults=GroomingDefaults.Load();options=Store.LoadDef("Jobs/Grooming.json",()=>defaults);
            if(options==null || !options.Valid){Log.Write("Invalid Grooming.json; embedded defaults used, original retained.");options=defaults;}
            menu=new NativeMenu("Barbearia / acabamento","SELECAO SEM PREVIA NO PERSONAGEM");pool.Add(menu);Visuals.Register(menu,"core");ui=new PersonalServiceMenu(menu,ex=>{End();ui.Close();Log.Error("grooming action",ex);Notify.Show("Operacao nao concluida; retome o pedido pendente sem nova cobranca.");});
        }
        void Add(string title,string text,Action action)
        {ui.Add(title,text,()=>{if(!access() || !Wallet.Grooming.Valid){End();ui.Close();Notify.Show("Use a barbearia com dados validos.");return;}action();});}
        public void OpenMenu()
        {
            End();if(!access()){Notify.Show("Va a pe a uma barbearia para acessar o acabamento.");return;}if(!Wallet.Grooming.Valid){Notify.Show("Dados de acabamento invalidos; original preservado.");return;}
            pool.HideAll();ui.Begin("root");menu.Name="Aparencia / rosto e acabamento";var p=Wallet.Grooming.Pending;
            if(p!=null)
            {
                string id=p.Id;Add("Retomar atendimento pendente / sem cobrar","Mesmo modelo salvo, na barbearia. Reaplica apenas os grupos selecionados e conclui o registro.",()=>Resume(id));
                if(!p.Started)Add("Cancelar pendente e estornar $"+p.Price,"Aplicacao ainda nao autorizada. Devolve a taxa paga ao banco e encerra pedido.",()=>{if(!Wallet.GroomingRefund(id,out _))Notify.Show("Estorno nao confirmado; confira limite do banco e estado do pedido.");OpenMenu();});
                else Add("Encerrar pendente sem estorno","Abre confirmacao. A aplicacao pode ja ter alterado o visual; nao ha backup completo do rosto anterior.",()=>ConfirmAbandon(id));
            }
            else
            {
                Add("Configurar novos valores / $"+options.Price,"Freemode apenas. Nenhuma mudanca no personagem durante selecao. Taxa por aplicacao; valores ja registrados podem ser reaplicados gratuitamente.",Begin);
                var known=Wallet.Grooming.Looks.Find(l=>l.Model==Game.Player.Character.Model.Hash);
                if(known!=null)Add("Reaplicar acabamento confirmado / gratis","Somente grupos que voce ja confirmou neste perfil/modelo. Sem aplicar automaticamente no load.",()=>Reapply(known.Model));
            }
            Add("Atendimentos "+Wallet.Grooming.Services+" / pagos $"+Wallet.Grooming.Spent,"Cortes/texturas e olhos continuam no menu anterior. Heranca/tracos so quando escolhidos; nao troca modelo ou roupas.",()=>{});
            Add("Voltar","Barbearia.",back);menu.Visible=true;
        }
        bool Acquire(){if(!gate.Acquire(Name)){Notify.Show("Encerre a atividade de "+gate.Owner+".");return false;}lease=true;return true;}
        void Begin()
        {
            if(Wallet.Grooming.Pending!=null || !Freemode(Game.Player.Character.Model.Hash) || !Function.Call<bool>(Hash.HAS_PED_HEAD_BLEND_FINISHED,Game.Player.Character)){Notify.Show("Use freemode com rosto carregado e encerre a pendencia anterior.");return;}
            if(!Acquire())return;actor=Game.Player.Character;profile=Profile.Id;
            var known=Wallet.Grooming.Looks.Find(l=>l.Model==actor.Model.Hash);
            draft=known==null?new GroomingLook{Model=actor.Model.Hash}:JsonConvert.DeserializeObject<GroomingLook>(JsonConvert.SerializeObject(known));
            draft.Hair=draft.Beard=draft.Brows=false;draft.Overlays.Clear();draft.Heritage=null;draft.Morphs.Clear();
            int beard=Function.Call<int>(Hash.GET_PED_HEAD_OVERLAY,actor,1),brows=Function.Call<int>(Hash.GET_PED_HEAD_OVERLAY,actor,2);
            if(known==null || !known.Beard)draft.BeardStyle=beard>=0 && beard<Function.Call<int>(Hash.GET_PED_HEAD_OVERLAY_NUM,1)?beard:255;
            if(known==null || !known.Brows)draft.BrowsStyle=brows>=0 && brows<Function.Call<int>(Hash.GET_PED_HEAD_OVERLAY_NUM,2)?brows:255;
            int beardCount=Function.Call<int>(Hash.GET_PED_HEAD_OVERLAY_NUM,1),browsCount=Function.Call<int>(Hash.GET_PED_HEAD_OVERLAY_NUM,2);
            if(draft.BeardStyle!=255 && draft.BeardStyle>=beardCount)draft.BeardStyle=255;
            if(draft.BrowsStyle!=255 && draft.BrowsStyle>=browsCount)draft.BrowsStyle=255;
            int colors=Colors;if(colors>0){if(draft.HairColor>=colors)draft.HairColor=0;if(draft.Highlight>=colors)draft.Highlight=0;if(draft.BeardColor>=colors)draft.BeardColor=0;if(draft.BrowsColor>=colors)draft.BrowsColor=0;}
            Build();
        }
        bool Selecting=>draft!=null && actor!=null && actor.Exists() && actor==Game.Player.Character && actor.Model.Hash==draft.Model && profile==Profile.Id && access();
        void Select(Action change){if(!Selecting){End();ui.Close();return;}change();Section(editingSection);}
        static int Cycle(int value,int delta,int count)=>WardrobeRules.Cycle(value,delta,count,false);
        int Colors=>Math.Min(64,Function.Call<int>(Hash.GET_NUM_PED_HAIR_TINTS));
        static int Style(int current,int delta,int overlay)
        {int count=Function.Call<int>(Hash.GET_PED_HEAD_OVERLAY_NUM,overlay);return PersonalServiceRules.OverlayStyle(current,delta,count);}
        void Build()
        {
            if(!Selecting)return;editingSection=-1;editingOverlay=-1;ui.Begin("draft",OpenMenu);menu.Name="Acabamento / escolha os grupos";
            Add("Cabelo / tintura e mechas"+(draft.Hair?" [selecionado]":""),"Defina somente os valores que deseja aplicar.",()=>Section(0));
            Add("Barba"+(draft.Beard?" [selecionado]":""),"Estilo, cor e opacidade.",()=>Section(1));
            Add("Sobrancelhas"+(draft.Brows?" [selecionado]":""),"Estilo, cor e opacidade.",()=>Section(2));
            Add("Pele / maquiagem / acabamento ("+draft.Overlays.Count+")","Cada grupo pode ser selecionado separadamente.",()=>Section(3));
            Add("Heranca facial"+(draft.Heritage==null?"":" [selecionada]"),"Origens e misturas novas; nenhuma mudanca antes de confirmar.",()=>HeritageMenu(false));
            Add("Tracos faciais: "+draft.Morphs.Count+" / 20","Apenas os tracos selecionados serao alterados.",()=>MorphMenu(false));
            bool free=draft.Valid && GroomingTransactions.SameSelected(Wallet.Grooming.Looks.Find(l=>l.Model==draft.Model),draft);
            Add(free?"Aplicar valores registrados / gratis":"Revisar e confirmar / $"+options.Price,"Confira os grupos antes de pagar; fechar/cancelar nao muda o personagem.",ConfirmApplication);
            Add("Cancelar selecao","Descarta o rascunho sem aplicar ou cobrar.",OpenMenu);menu.Visible=true;
        }
        void Section(int section)
        {
            if(!Selecting)return;
            if(section<0 || section>3){Build();return;}
            editingSection=section;editingOverlay=-1;ui.Begin("section:"+section,Build);
            menu.Name=section==0?"Tintura / mechas":section==1?"Barba":section==2?"Sobrancelhas":"Pele / maquiagem / acabamento";
            if(section==0){
                Add("Aplicar tintura: "+(draft.Hair?"SIM":"nao"),"Seleciona este grupo para a confirmacao; nao altera o personagem agora.",()=>Select(()=>draft.Hair=!draft.Hair));
                if(draft.Hair && Colors>0){
                    Add("Cor cabelo: "+draft.HairColor+" / anterior","Amostra de paleta; cor nova.",()=>Select(()=>draft.HairColor=Cycle(draft.HairColor,-1,Colors)));
                    Add("Proxima cor cabelo","Sem aplicar durante selecao.",()=>Select(()=>draft.HairColor=Cycle(draft.HairColor,1,Colors)));
                    Add("Mechas: "+draft.Highlight+" / anterior","Cor nova de mechas.",()=>Select(()=>draft.Highlight=Cycle(draft.Highlight,-1,Colors)));
                    Add("Proxima cor mechas","Sem aplicar durante selecao.",()=>Select(()=>draft.Highlight=Cycle(draft.Highlight,1,Colors)));
                }
            }else if(section==1 || section==2){
                bool brows=section==2;
                Add("Aplicar este grupo: "+((brows?draft.Brows:draft.Beard)?"SIM":"nao"),"Altera somente este grupo ao confirmar o atendimento.",()=>Select(()=>{if(brows)draft.Brows=!draft.Brows;else draft.Beard=!draft.Beard;}));
                if(brows?draft.Brows:draft.Beard)OverlayRows(brows);
            }else foreach(int id in CosmeticRules.Ids){
                int group=id;bool chosen=draft.Overlays.Exists(o=>o.Id==group);
                Add(CosmeticRules.Label(group)+(chosen?" [selecionado]":""),"Estilo, opacidade e cor quando disponivel.",()=>SelectOverlay(group));
            }
            Add("Voltar ao atendimento","Mantem escolhas no rascunho, sem cobrar.",Build);menu.Visible=true;
        }
        void ConfirmApplication()
        {
            if(!Selecting || !draft.Valid){Notify.Show("Selecione ao menos um grupo valido antes de confirmar.");return;}
            bool free=GroomingTransactions.SameSelected(Wallet.Grooming.Looks.Find(l=>l.Model==draft.Model),draft);
            ui.Begin("confirm",Build,false);menu.Name=free?"Reaplicar / gratis":"Confirmar atendimento / $"+options.Price;
            Add("Voltar e revisar","Conserva o rascunho sem cobrar.",Build);
            string groups=(draft.Hair?"Cabelo; ":"")+(draft.Beard?"Barba; ":"")+(draft.Brows?"Sobrancelhas; ":"")+(draft.Heritage!=null?"Heranca; ":"")+draft.Overlays.Count+" grupos de pele; "+draft.Morphs.Count+" tracos.";
            Add(free?"Confirmar reaplicacao gratis":"Confirmar e pagar $"+options.Price,groups,Commit);menu.Visible=true;
        }
        void OverlayRows(bool brows)
        {
            string label=brows?"Sobrancelhas":"Barba";int style=brows?draft.BrowsStyle:draft.BeardStyle,opacity=brows?draft.BrowsOpacity:draft.BeardOpacity,color=brows?draft.BrowsColor:draft.BeardColor;
            Add(label+" estilo "+(style==255?"sem":style.ToString())+" / anterior","A opcao sem remove somente este overlay ao confirmar.",()=>Select(()=>{int v=Style(style,-1,brows?2:1);if(brows)draft.BrowsStyle=v<0?255:v;else draft.BeardStyle=v<0?255:v;}));
            Add("Proximo estilo / "+label,"Sem mudar personagem durante selecao.",()=>Select(()=>{int v=Style(style,1,brows?2:1);if(brows)draft.BrowsStyle=v<0?255:v;else draft.BeardStyle=v<0?255:v;}));
            Add(label+" opacidade "+opacity+"% / menos","Passos de 10%; zero deixa invisivel.",()=>Select(()=>{if(brows)draft.BrowsOpacity=Math.Max(0,opacity-10);else draft.BeardOpacity=Math.Max(0,opacity-10);}));
            Add("Mais opacidade / "+label,"Ate 100%.",()=>Select(()=>{if(brows)draft.BrowsOpacity=Math.Min(100,opacity+10);else draft.BeardOpacity=Math.Min(100,opacity+10);}));
            Add(label+" cor "+color+" / anterior","Nova cor selecionada; amostra de paleta ao selecionar esta linha.",()=>Select(()=>{if(brows)draft.BrowsColor=Cycle(color,-1,Colors);else draft.BeardColor=Cycle(color,-1,Colors);}));
            Add("Proxima cor / "+label,"Nao aplica antes de confirmar.",()=>Select(()=>{if(brows)draft.BrowsColor=Cycle(color,1,Colors);else draft.BeardColor=Cycle(color,1,Colors);}));
        }
        void SelectOverlay(int id)
        {
            if(!Selecting)return;int selected=editingOverlay==id?menu.SelectedIndex:0;editingOverlay=id;var overlay=draft.Overlays.Find(o=>o.Id==id);
            ui.Begin("overlay:"+id,()=>Section(3));menu.Name=CosmeticRules.Label(id)+" / sem previa";
            Add("Alterar este grupo: "+(overlay!=null?"SIM":"nao"),"Desativar exclui apenas esta selecao do rascunho, sem remover o visual atual.",()=>{
                if(!Selecting)return;
                if(overlay!=null)draft.Overlays.Remove(overlay);
                else{
                    var known=Wallet.Grooming.Looks.Find(l=>l.Model==draft.Model)?.Overlays.Find(o=>o.Id==id);
                    var next=known==null?new CosmeticOverlay{Id=id,Style=Function.Call<int>(Hash.GET_PED_HEAD_OVERLAY,actor,id)}:JsonConvert.DeserializeObject<CosmeticOverlay>(JsonConvert.SerializeObject(known));
                    int count=Function.Call<int>(Hash.GET_PED_HEAD_OVERLAY_NUM,id);
                    if(next.Style<0 || next.Style>=count)next.Style=255;
                    if(next.Color>=OverlayColors(id))next.Color=0;
                    draft.Overlays.Add(next);
                }
                SelectOverlay(id);
            });
            if(overlay!=null)
            {
                Add("Estilo "+(overlay.Style==255?"sem":overlay.Style.ToString())+" / anterior","Sem remove apenas este overlay quando confirmar o atendimento.",()=>EditOverlay(id,()=>{int v=Style(overlay.Style,-1,id);overlay.Style=v<0?255:v;}));
                Add("Proximo estilo","Nao aplica durante selecao.",()=>EditOverlay(id,()=>{int v=Style(overlay.Style,1,id);overlay.Style=v<0?255:v;}));
                Add("Opacidade "+overlay.Opacity+"% / menos","Nova opacidade, em passos de 10%.",()=>EditOverlay(id,()=>overlay.Opacity=Math.Max(0,overlay.Opacity-10)));
                Add("Mais opacidade","Ate 100%.",()=>EditOverlay(id,()=>overlay.Opacity=Math.Min(100,overlay.Opacity+10)));
                if(CosmeticRules.ColorType(id)!=0){
                    Add("Cor "+overlay.Color+" / anterior","Amostra da paleta, nao uma previa aplicada no personagem.",()=>EditOverlay(id,()=>overlay.Color=Cycle(overlay.Color,-1,OverlayColors(id))));
                    Add("Proxima cor","Paleta de maquiagem ou pelos conforme grupo.",()=>EditOverlay(id,()=>overlay.Color=Cycle(overlay.Color,1,OverlayColors(id))));
                }
            }
            Add("Voltar a pele / maquiagem","Mantem selecao no rascunho. Confirmacao no atendimento.",()=>{if(Selecting)Section(3);});
            menu.SelectedIndex=Math.Max(0,Math.Min(selected,menu.Items.Count-1));menu.Visible=true;
        }
        void EditOverlay(int id,Action change){if(!Selecting)return;change();SelectOverlay(id);}
        static int OverlayColors(int id)=>Math.Min(64,Function.Call<int>(CosmeticRules.ColorType(id)==2?Hash.GET_NUM_PED_MAKEUP_TINTS:Hash.GET_NUM_PED_HAIR_TINTS));
        internal static bool Compatible(GroomingLook look)
        {
            if(look==null || !look.Valid || !Freemode(look.Model) || Game.Player.Character.Model.Hash!=look.Model || !Function.Call<bool>(Hash.HAS_PED_HEAD_BLEND_FINISHED,Game.Player.Character))return false;
            int colors=Function.Call<int>(Hash.GET_NUM_PED_HAIR_TINTS);
            return (!look.Hair || look.HairColor<colors && look.Highlight<colors) && (!look.Beard || (look.BeardStyle==255 || look.BeardStyle<Function.Call<int>(Hash.GET_PED_HEAD_OVERLAY_NUM,1)) && look.BeardColor<colors) && (!look.Brows || (look.BrowsStyle==255 || look.BrowsStyle<Function.Call<int>(Hash.GET_PED_HEAD_OVERLAY_NUM,2)) && look.BrowsColor<colors) && (look.Heritage==null || FaceRules.Compatible(look.Heritage,NativeParents())) && look.Overlays.TrueForAll(o=>(o.Style==255 || o.Style<Function.Call<int>(Hash.GET_PED_HEAD_OVERLAY_NUM,o.Id)) && (CosmeticRules.ColorType(o.Id)==0 || o.Color<OverlayColors(o.Id)));
        }
        internal static void ApplySurface(GroomingLook look)
        {
            if(!Compatible(look))throw new InvalidOperationException("Unsupported grooming target/model.");var ped=Game.Player.Character;
            foreach(var morph in look.Morphs)Function.Call(Hash.SET_PED_MICRO_MORPH,ped,morph.Index,morph.Value/100f);
            if(look.Hair)Function.Call(Hash.SET_PED_HAIR_TINT,ped,look.HairColor,look.Highlight);
            if(look.Beard){Function.Call(Hash.SET_PED_HEAD_OVERLAY,ped,1,look.BeardStyle,look.BeardStyle==255?0f:look.BeardOpacity/100f);Function.Call(Hash.SET_PED_HEAD_OVERLAY_TINT,ped,1,1,look.BeardColor,look.BeardColor);}
            if(look.Brows){Function.Call(Hash.SET_PED_HEAD_OVERLAY,ped,2,look.BrowsStyle,look.BrowsStyle==255?0f:look.BrowsOpacity/100f);Function.Call(Hash.SET_PED_HEAD_OVERLAY_TINT,ped,2,1,look.BrowsColor,look.BrowsColor);}
            foreach(var overlay in look.Overlays){
                Function.Call(Hash.SET_PED_HEAD_OVERLAY,ped,overlay.Id,overlay.Style,overlay.Style==255?0f:overlay.Opacity/100f);
                int type=CosmeticRules.ColorType(overlay.Id);
                if(type!=0)Function.Call(Hash.SET_PED_HEAD_OVERLAY_TINT,ped,overlay.Id,type,overlay.Color,0);
            }
        }
        void Commit()
        {
            if(!Selecting || !draft.Valid || !Compatible(draft)){Notify.Show("Selecione grupos validos no freemode atual.");return;}
            if(GroomingTransactions.SameSelected(Wallet.Grooming.Looks.Find(l=>l.Model==draft.Model),draft)){Execute(draft,null);return;}
            string id=Guid.NewGuid().ToString("N");if(!Wallet.GroomingBegin(draft,options.Price,id,out _)){Notify.Show("Confira banco, limite ou pendencia. Nada aplicado.");return;}
            Process(id);
        }
        void Process(string id)
        {
            var p=Wallet.Grooming.Pending;if(p==null || p.Id!=id){End();OpenMenu();return;}
            if(!Compatible(p.Look)){End();OpenMenu();Notify.Show("Volte ao mesmo ped freemode/recursos para retomar; nenhuma mudanca aplicada agora.");return;}
            if(!p.Started && !Wallet.GroomingStart(id,out _)){End();OpenMenu();Notify.Show("Aplicacao ainda nao autorizada; retome ou cancele para estornar.");return;}
            Execute(p.Look,id);
        }
        void Resume(string id){if(Acquire())Process(id);}
        void Reapply(int model)
        {var look=Wallet.Grooming.Looks.Find(l=>l.Model==model);if(look==null || !Compatible(look)){Notify.Show("Visual incompativel com ped/recursos atuais.");return;}if(Acquire())Execute(look,null);}
        void ConfirmAbandon(string id)
        {ui.Begin("abandon",OpenMenu,false);menu.Name="Encerrar sem estorno?";Add("Cancelar","Manter pedido para retomar.",OpenMenu);Add("Sim, encerrar pendencia paga","Nao restaura o visual anterior e nao devolve taxa; use retomar para concluir sem pagar de novo.",()=>{if(!Wallet.GroomingAbandon(id,out _))Notify.Show("Estado do pedido mudou; confira pendencia.");OpenMenu();});menu.Visible=true;}
        void End(){application=null;editingSection=-1;editingOverlay=-1;draft=null;actor=null;profile=null;if(lease)gate.Release(Name);lease=false;}
        Color Palette(int index,bool makeup=false)
        {using(var r=new OutputArgument())using(var g=new OutputArgument())using(var b=new OutputArgument()){Function.Call(makeup?Hash.GET_PED_MAKEUP_TINT_COLOR:Hash.GET_PED_HAIR_TINT_COLOR,index,r,g,b);return Color.FromArgb(255,Math.Max(0,Math.Min(255,r.GetResult<int>())),Math.Max(0,Math.Min(255,g.GetResult<int>())),Math.Max(0,Math.Min(255,b.GetResult<int>())));}}
        public void Tick()
        {
            ui.Tick(access());
            if(application!=null){TickApplication();return;}
            if(draft==null)return;
            if(!Selecting || !menu.Visible){End();ui.Close();return;}
            try{string title=menu.SelectedItem?.Title??"";int index=(title.StartsWith("Cor cabelo:") || title=="Proxima cor cabelo")?draft.HairColor:(title.StartsWith("Mechas:") || title=="Proxima cor mechas")?draft.Highlight:(title.StartsWith("Barba cor ") || title=="Proxima cor / Barba")?draft.BeardColor:(title.StartsWith("Sobrancelhas cor ") || title=="Proxima cor / Sobrancelhas")?draft.BrowsColor:-1;
                bool makeup=false;
                if(editingOverlay>=0 && (title.StartsWith("Cor ") || title=="Proxima cor")){
                    var overlay=draft.Overlays.Find(o=>o.Id==editingOverlay);if(overlay!=null){index=overlay.Color;makeup=CosmeticRules.ColorType(overlay.Id)==2;}
                }
                if(index>=0){Visuals.Rect(420,160,100,60,Palette(index,makeup));Visuals.Text("Amostra / cor "+index,420,225,.25f,Color.White);}}
            catch(Exception ex){Log.Error("grooming palette",ex);}
        }
        public void Save(){}
        public void OnKey(KeyEventArgs e){}
        public void Reload(){End();ui.Reset();}
        public void Dispose(){Reload();}
    }
}
