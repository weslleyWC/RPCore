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
    public sealed class GroomingSystem : ISystem
    {
        public string Name=>"grooming";
        readonly ObjectPool pool;readonly ShiftGate gate;readonly Func<bool> access;readonly Action back;readonly NativeMenu menu;readonly GroomingOptions options;
        int editingOverlay=-1;GroomingLook draft;Ped actor;string profile;bool lease;
        static bool Freemode(int model)=>model==new Model("mp_m_freemode_01").Hash || model==new Model("mp_f_freemode_01").Hash;
        public GroomingSystem(ObjectPool pool,ShiftGate gate,Func<bool> access,Action back)
        {
            this.pool=pool;this.gate=gate;this.access=access;this.back=back;var defaults=GroomingDefaults.Load();options=Store.LoadDef("Jobs/Grooming.json",()=>defaults);
            if(options==null || !options.Valid){Log.Write("Invalid Grooming.json; embedded defaults used, original retained.");options=defaults;}
            menu=new NativeMenu("Barbearia / acabamento","SELECAO SEM PREVIA NO PERSONAGEM");pool.Add(menu);Visuals.Register(menu,"core");
        }
        void Add(string title,string text,Action action)
        {var row=new NativeItem(title,text);row.Activated+=(s,e)=>{try{if(!access() || !Wallet.Grooming.Valid){End();menu.Visible=false;Notify.Show("Use a barbearia com dados validos.");return;}action();}catch(Exception ex){End();Log.Error("grooming action",ex);Notify.Show("Operacao nao concluida; confira atendimento pendente. Retomar nao cobra outra vez.");OpenMenu();}};menu.Add(row);}
        public void OpenMenu()
        {
            End();if(!access()){Notify.Show("Va a pe a uma barbearia para acessar o acabamento.");return;}if(!Wallet.Grooming.Valid){Notify.Show("Dados de acabamento invalidos; original preservado.");return;}
            pool.HideAll();menu.Clear();menu.Name="Acabamento / pele e maquiagem";var p=Wallet.Grooming.Pending;
            if(p!=null)
            {
                string id=p.Id;Add("Retomar atendimento pendente / sem cobrar","Mesmo modelo salvo, na barbearia. Reaplica apenas os grupos selecionados e conclui o registro.",()=>Resume(id));
                if(!p.Started)Add("Cancelar pendente e estornar $"+p.Price,"Aplicacao ainda nao autorizada. Devolve a taxa paga ao banco e encerra pedido.",()=>{if(!Wallet.GroomingRefund(id,out _))Notify.Show("Estorno nao confirmado; confira limite do banco e estado do pedido.");OpenMenu();});
                else Add("Encerrar pendente sem estorno","Abre confirmacao. A aplicacao pode ja ter alterado cores; nao ha backup das cores originais.",()=>ConfirmAbandon(id));
            }
            else
            {
                Add("Configurar novos valores / $"+options.Price,"Freemode apenas. Nenhuma mudanca no personagem durante selecao. Taxa por aplicacao; valores ja registrados podem ser reaplicados gratuitamente.",Begin);
                var known=Wallet.Grooming.Looks.Find(l=>l.Model==Game.Player.Character.Model.Hash);
                if(known!=null)Add("Reaplicar acabamento confirmado / gratis","Somente grupos que voce ja confirmou neste perfil/modelo. Sem aplicar automaticamente no load.",()=>Reapply(known.Model));
            }
            Add("Atendimentos "+Wallet.Grooming.Services+" / pagos $"+Wallet.Grooming.Spent,"Cortes/texturas e olhos continuam no menu anterior. Este servico nao muda heranca facial, modelo ou roupas.",()=>{});
            Add("Voltar","Barbearia.",back);menu.Visible=true;
        }
        bool Acquire(){if(!gate.Acquire(Name)){Notify.Show("Encerre a atividade de "+gate.Owner+".");return false;}lease=true;return true;}
        void Begin()
        {
            if(Wallet.Grooming.Pending!=null || !Freemode(Game.Player.Character.Model.Hash) || !Function.Call<bool>(Hash.HAS_PED_HEAD_BLEND_FINISHED,Game.Player.Character)){Notify.Show("Use freemode com rosto carregado e encerre a pendencia anterior.");return;}
            if(!Acquire())return;actor=Game.Player.Character;profile=Profile.Id;
            var known=Wallet.Grooming.Looks.Find(l=>l.Model==actor.Model.Hash);
            draft=known==null?new GroomingLook{Model=actor.Model.Hash}:JsonConvert.DeserializeObject<GroomingLook>(JsonConvert.SerializeObject(known));
            draft.Hair=draft.Beard=draft.Brows=false;draft.Overlays.Clear();
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
        void Select(Action change){if(!Selecting){End();menu.Visible=false;return;}change();Build();}
        static int Cycle(int value,int delta,int count)=>WardrobeRules.Cycle(value,delta,count,false);
        int Colors=>Math.Min(64,Function.Call<int>(Hash.GET_NUM_PED_HAIR_TINTS));
        static int Style(int current,int delta,int overlay)
        {int count=Function.Call<int>(Hash.GET_PED_HEAD_OVERLAY_NUM,overlay);return count<=0?255:(WardrobeRules.Cycle(current==255?-1:current,delta,count,true));}
        void Build()
        {
            editingOverlay=-1;int selected=menu.SelectedIndex;menu.Clear();menu.Name="Rascunho / sem mudar personagem";
            Add("Alterar tintura: "+(draft.Hair?"SIM":"nao"),"Cabelo e mechas; valores NOVOS escolhidos, sem leitura das tintas atuais.",()=>Select(()=>draft.Hair=!draft.Hair));
            if(draft.Hair)
            {
                Add("Cor cabelo: "+draft.HairColor+" / anterior","Indice de paleta; amostra de cor ao selecionar esta linha.",()=>Select(()=>draft.HairColor=Cycle(draft.HairColor,-1,Colors)));
                Add("Proxima cor cabelo","Nao aplica antes de confirmar.",()=>Select(()=>draft.HairColor=Cycle(draft.HairColor,1,Colors)));
                Add("Mechas: "+draft.Highlight+" / anterior","Substitui mechas somente ao confirmar tintura.",()=>Select(()=>draft.Highlight=Cycle(draft.Highlight,-1,Colors)));
                Add("Proxima cor mechas","Nao aplica antes de confirmar.",()=>Select(()=>draft.Highlight=Cycle(draft.Highlight,1,Colors)));
            }
            Add("Alterar barba: "+(draft.Beard?"SIM":"nao"),"Ao ativar escolhe estilo, opacidade e cor da barba; outros grupos continuam intactos.",()=>Select(()=>draft.Beard=!draft.Beard));
            if(draft.Beard)OverlayRows(false);
            Add("Alterar sobrancelhas: "+(draft.Brows?"SIM":"nao"),"Ao ativar escolhe estilo, opacidade e cor; nao muda estrutura do rosto.",()=>Select(()=>draft.Brows=!draft.Brows));
            if(draft.Brows)OverlayRows(true);
            foreach(int id in CosmeticRules.Ids)
            {
                int group=id;bool chosen=draft.Overlays.Exists(o=>o.Id==group);
                Add(CosmeticRules.Label(group)+": "+(chosen?"SIM / editar":"nao / configurar"),"Estilo, opacidade e cor quando disponivel. So altera o grupo marcado ao confirmar o atendimento.",()=>SelectOverlay(group));
            }
            bool free=draft.Valid && GroomingTransactions.SameSelected(Wallet.Grooming.Looks.Find(l=>l.Model==draft.Model),draft);
            Add(free?"Aplicar valores ja registrados / gratis":"Confirmar aplicacao / $"+options.Price,"Aplica APENAS grupos marcados SIM. Taxa unica pelo banco RPCore; antes deste botao, fechar/cancelar nao muda o visual.",Commit);
            Add("Cancelar selecao","Descarta rascunho sem mudar personagem nem banco.",OpenMenu);menu.SelectedIndex=Math.Max(0,Math.Min(selected,menu.Items.Count-1));menu.Visible=true;
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
            menu.Clear();menu.Name=CosmeticRules.Label(id)+" / sem previa";
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
            Add("Voltar ao atendimento","Mantem selecao no rascunho. Confirmacao e cancelamento no menu anterior.",()=>{if(Selecting){menu.SelectedIndex=0;Build();}});
            menu.SelectedIndex=Math.Max(0,Math.Min(selected,menu.Items.Count-1));menu.Visible=true;
        }
        void EditOverlay(int id,Action change){if(!Selecting)return;change();SelectOverlay(id);}
        static int OverlayColors(int id)=>Math.Min(64,Function.Call<int>(CosmeticRules.ColorType(id)==2?Hash.GET_NUM_PED_MAKEUP_TINTS:Hash.GET_NUM_PED_HAIR_TINTS));
        static bool Compatible(GroomingLook look)
        {
            if(look==null || !look.Valid || !Freemode(look.Model) || Game.Player.Character.Model.Hash!=look.Model || !Function.Call<bool>(Hash.HAS_PED_HEAD_BLEND_FINISHED,Game.Player.Character))return false;
            int colors=Function.Call<int>(Hash.GET_NUM_PED_HAIR_TINTS);
            return (!look.Hair || look.HairColor<colors && look.Highlight<colors) && (!look.Beard || (look.BeardStyle==255 || look.BeardStyle<Function.Call<int>(Hash.GET_PED_HEAD_OVERLAY_NUM,1)) && look.BeardColor<colors) && (!look.Brows || (look.BrowsStyle==255 || look.BrowsStyle<Function.Call<int>(Hash.GET_PED_HEAD_OVERLAY_NUM,2)) && look.BrowsColor<colors) && look.Overlays.TrueForAll(o=>(o.Style==255 || o.Style<Function.Call<int>(Hash.GET_PED_HEAD_OVERLAY_NUM,o.Id)) && (CosmeticRules.ColorType(o.Id)==0 || o.Color<OverlayColors(o.Id)));
        }
        static void Apply(GroomingLook look)
        {
            if(!Compatible(look))throw new InvalidOperationException("Unsupported grooming target/model.");var ped=Game.Player.Character;
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
            if(GroomingTransactions.SameSelected(Wallet.Grooming.Looks.Find(l=>l.Model==draft.Model),draft)){Apply(draft);End();Notify.Show("Valores registrados reaplicados sem cobrar.");OpenMenu();return;}
            string id=Guid.NewGuid().ToString("N");if(!Wallet.GroomingBegin(draft,options.Price,id,out _)){Notify.Show("Confira banco, limite ou pendencia. Nada aplicado.");return;}
            Process(id);End();OpenMenu();
        }
        void Process(string id)
        {
            var p=Wallet.Grooming.Pending;if(p==null || p.Id!=id)return;
            if(!Compatible(p.Look)){Notify.Show("Volte ao mesmo ped freemode/recursos para retomar; nenhuma mudanca aplicada agora.");return;}
            if(!p.Started && !Wallet.GroomingStart(id,out _)){Notify.Show("Aplicacao ainda nao autorizada; retome ou cancele para estornar.");return;}
            Apply(p.Look);
            if(!Wallet.GroomingComplete(id,out _))throw new InvalidOperationException("Grooming applied, completion pending.");
            Notify.Show("Acabamento aplicado e registrado; sem segunda cobranca.");
        }
        void Resume(string id){if(!Acquire())return;try{Process(id);}finally{End();}OpenMenu();}
        void Reapply(int model)
        {var look=Wallet.Grooming.Looks.Find(l=>l.Model==model);if(look==null || !Compatible(look)){Notify.Show("Visual incompativel com ped/recursos atuais.");return;}if(!Acquire())return;try{Apply(look);Notify.Show("Acabamento confirmado reaplicado sem cobrar.");}finally{End();}}
        void ConfirmAbandon(string id)
        {menu.Clear();menu.Name="Encerrar sem estorno?";Add("Sim, encerrar pendencia paga","Nao restaura cores anteriores e nao devolve taxa; use retomar para concluir sem pagar de novo.",()=>{if(!Wallet.GroomingAbandon(id,out _))Notify.Show("Estado do pedido mudou; confira pendencia.");OpenMenu();});Add("Voltar","Manter pedido para retomar.",OpenMenu);menu.Visible=true;}
        void End(){editingOverlay=-1;draft=null;actor=null;profile=null;if(lease)gate.Release(Name);lease=false;}
        Color Palette(int index,bool makeup=false)
        {using(var r=new OutputArgument())using(var g=new OutputArgument())using(var b=new OutputArgument()){Function.Call(makeup?Hash.GET_PED_MAKEUP_TINT_COLOR:Hash.GET_PED_HAIR_TINT_COLOR,index,r,g,b);return Color.FromArgb(255,Math.Max(0,Math.Min(255,r.GetResult<int>())),Math.Max(0,Math.Min(255,g.GetResult<int>())),Math.Max(0,Math.Min(255,b.GetResult<int>())));}}
        public void Tick()
        {
            if(draft==null)return;
            if(!Selecting || !menu.Visible){End();menu.Visible=false;return;}
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
        public void Reload(){End();menu.Visible=false;}
        public void Dispose(){Reload();}
    }
}
