using System;
using GTA;
using GTA.Native;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed partial class GroomingSystem
    {
        sealed class FaceApplication
        {
            public Ped Actor;
            public string Profile,Order;
            public GroomingLook Look;
            public int Started;
        }
        FaceApplication application;
        static int[] NativeParents()
        {
            var ranges=new FaceHeadRange[4];
            for(int i=0;i<ranges.Length;i++)ranges[i]=new FaceHeadRange(Function.Call<int>(Hash.GET_PED_HEAD_BLEND_FIRST_INDEX,i),Function.Call<int>(Hash.GET_PED_HEAD_BLEND_NUM_HEADS,i));
            return FaceRules.Parents(ranges);
        }
        GroomingLook Known=>Wallet.Grooming.Looks.Find(l=>l.Model==draft.Model);
        void BackToDraft(){if(Selecting){menu.SelectedIndex=0;Build();}}
        void HeritageMenu(bool retain)
        {
            if(!Selecting)return;int selected=retain?menu.SelectedIndex:0;editingOverlay=-1;menu.Clear();menu.Name="Heranca / novos valores sem previa";
            Add("Alterar heranca: "+(draft.Heritage!=null?"SIM":"nao"),"Ativar substitui origens e misturas de formato/pele ao confirmar. Desativar conserva a heranca atual.",()=>{
                if(!Selecting)return;
                if(draft.Heritage!=null)draft.Heritage=null;
                else{
                    var parents=NativeParents();if(parents.Length==0){Notify.Show("Indices de heranca indisponiveis; nenhuma mudanca.");return;}
                    var known=Known?.Heritage;
                    draft.Heritage=known==null?new FaceHeritage{ShapeFirst=parents[0],SkinFirst=parents[0],ShapeSecond=parents[Math.Min(1,parents.Length-1)],SkinSecond=parents[Math.Min(1,parents.Length-1)]}:JsonConvert.DeserializeObject<FaceHeritage>(JsonConvert.SerializeObject(known));
                }
                HeritageMenu(true);
            });
            var h=draft.Heritage;
            if(h!=null)
            {
                ParentRows("Formato origem A",()=>h.ShapeFirst,v=>h.ShapeFirst=v);
                ParentRows("Formato origem B",()=>h.ShapeSecond,v=>h.ShapeSecond=v);
                ParentRows("Pele origem A",()=>h.SkinFirst,v=>h.SkinFirst=v);
                ParentRows("Pele origem B",()=>h.SkinSecond,v=>h.SkinSecond=v);
                MixRows("Mistura formato",()=>h.ShapeMix,v=>h.ShapeMix=v);
                MixRows("Mistura pele",()=>h.SkinMix,v=>h.SkinMix=v);
            }
            Add("Voltar ao atendimento","Mantem o rascunho; confirmar/pagar ou cancelar no atendimento.",BackToDraft);
            menu.SelectedIndex=Math.Max(0,Math.Min(selected,menu.Items.Count-1));menu.Visible=true;
        }
        void ParentRows(string label,Func<int> read,Action<int> write)
        {
            Add(label+": "+read()+" / anterior","Indices de origens do GTA instalado, incluindo DLC. Formato e pele independentes. Nao representa leitura da sua heranca atual.",()=>ChangeParent(read,write,-1));
            Add("Proxima / "+label,"Muda apenas a selecao para a confirmacao posterior.",()=>ChangeParent(read,write,1));
        }
        void ChangeParent(Func<int> read,Action<int> write,int delta)
        {if(!Selecting)return;var parents=NativeParents();if(parents.Length==0){Notify.Show("Origens indisponiveis; selecao mantida.");return;}write(FaceRules.CycleParent(read(),delta,parents));HeritageMenu(true);}
        void MixRows(string label,Func<int> read,Action<int> write)
        {
            Add(label+": "+read()+"% B / menos","0% usa origem A; 100% usa B. Valores novos, em passos de 5%.",()=>ChangeMix(read,write,-5));
            Add("Mais / "+label,"Sem alterar personagem durante selecao.",()=>ChangeMix(read,write,5));
        }
        void ChangeMix(Func<int> read,Action<int> write,int delta){if(!Selecting)return;write(FaceRules.Step(read(),delta,true));HeritageMenu(true);}
        void MorphMenu(bool retain)
        {
            if(!Selecting)return;int selected=retain?menu.SelectedIndex:0;editingOverlay=-1;menu.Clear();menu.Name="Tracos / "+draft.Morphs.Count+" selecionados";
            for(int i=0;i<20;i++){
                int index=i;var morph=draft.Morphs.Find(m=>m.Index==index);
                Add(FaceRules.Label(index)+": "+(morph==null?"nao":morph.Value.ToString()),"Abra para selecionar este traco e definir novo valor. Outros tracos nao sao zerados.",()=>MorphEditor(index,false));
            }
            Add("Voltar ao atendimento","Mantem selecoes sem aplicar nem cobrar.",BackToDraft);menu.SelectedIndex=Math.Max(0,Math.Min(selected,menu.Items.Count-1));menu.Visible=true;
        }
        void MorphEditor(int index,bool retain)
        {
            if(!Selecting)return;int selected=retain?menu.SelectedIndex:0;editingOverlay=-1;menu.Clear();menu.Name=FaceRules.Label(index)+" / sem previa";var morph=draft.Morphs.Find(m=>m.Index==index);
            Add("Alterar este traco: "+(morph==null?"nao":"SIM"),"Ativar substitui somente este indice. Desativar descarta a selecao sem alterar o rosto atual.",()=>{
                if(!Selecting)return;
                if(morph!=null)draft.Morphs.Remove(morph);
                else{var known=Known?.Morphs.Find(m=>m.Index==index);draft.Morphs.Add(known==null?new FaceMorph{Index=index}:JsonConvert.DeserializeObject<FaceMorph>(JsonConvert.SerializeObject(known)));}
                MorphEditor(index,true);
            });
            if(morph!=null){
                Add("Valor "+morph.Value+" / diminuir 10","Intervalo -100 a +100. Zero escolhe valor neutro NOVO; nao e leitura do rosto atual.",()=>ChangeMorph(morph,-10));
                Add("Aumentar 10","Permite os dois sentidos do morph nativo: -1 a +1.",()=>ChangeMorph(morph,10));
            }
            Add("Voltar aos tracos","Mantem selecao. Confirmacao no atendimento.",()=>MorphMenu(false));menu.SelectedIndex=Math.Max(0,Math.Min(selected,menu.Items.Count-1));menu.Visible=true;
        }
        void ChangeMorph(FaceMorph morph,int delta){if(!Selecting)return;morph.Value=FaceRules.Step(morph.Value,delta,false);MorphEditor(morph.Index,true);}
        void Execute(GroomingLook look,string order)
        {
            if(!Compatible(look))throw new InvalidOperationException("Appearance preflight failed.");
            if(look.Heritage==null){ApplySurface(look);FinishApplication(order);return;}
            // Authorization is already durable for paid orders. Keep the lease across ticks.
            // Never block the script thread waiting for an asynchronous head blend.
            var h=look.Heritage;
            application=new FaceApplication{Actor=Game.Player.Character,Profile=Profile.Id,Order=order,Look=JsonConvert.DeserializeObject<GroomingLook>(JsonConvert.SerializeObject(look)),Started=Game.GameTime};
            draft=null;editingOverlay=-1;
            Function.Call(Hash.SET_PED_HEAD_BLEND_DATA,application.Actor,h.ShapeFirst,h.ShapeSecond,0,h.SkinFirst,h.SkinSecond,0,h.ShapeMix/100f,h.SkinMix/100f,0f,false);
            menu.Clear();menu.Name="Aplicando heranca / aguarde ate 10s";
            Add("Fechar atendimento","A heranca pode ja ter mudado. Pedido pago permanece retomavel sem nova cobranca; nao desfaz o rosto anterior.",()=>{End();menu.Visible=false;});
            menu.SelectedIndex=0;menu.Visible=true;
        }
        void FinishApplication(string order)
        {
            if(order!=null && !Wallet.GroomingComplete(order,out _))throw new InvalidOperationException("Appearance applied; paid completion pending.");
            End();OpenMenu();Notify.Show(order==null?"Aparencia registrada reaplicada sem cobrar.":"Aparencia aplicada e registrada; sem segunda cobranca.");
        }
        void TickApplication()
        {
            try{
                var pending=application;
                bool same=pending.Actor!=null && pending.Actor.Exists() && pending.Actor==Game.Player.Character && pending.Actor.Model.Hash==pending.Look.Model && pending.Profile==Profile.Id;
                bool allowed=same && access();
                bool ready=allowed && Function.Call<bool>(Hash.HAS_PED_HEAD_BLEND_FINISHED,pending.Actor);
                var state=FaceRules.Wait(same,allowed,menu.Visible,ready,unchecked((uint)(Game.GameTime-pending.Started)));
                if(state==FaceWait.Waiting)return;
                if(state==FaceWait.Interrupted){bool paid=pending.Order!=null;End();menu.Visible=false;Notify.Show(paid?"Atendimento interrompido. Retome na barbearia sem cobrar de novo.":"Reaplicacao interrompida; reaplique o registro quando estiver pronto.");return;}
                ApplySurface(pending.Look);FinishApplication(pending.Order);
            }
            catch(Exception ex){End();menu.Visible=false;Log.Error("appearance head blend",ex);Notify.Show("Aplicacao nao concluida. Confira pendencia e retome na barbearia sem pagar outra vez.");}
        }
    }
}
