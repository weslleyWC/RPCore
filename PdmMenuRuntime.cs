using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using LemonUI.Menus;
namespace RPCore
{
    internal static class PdmMenuRuntime
    {
        public static bool WorldAllowed=>!Game.IsPaused && !Game.IsMissionActive && !GameplayContext.SceneBlocking && Game.Player.CanControlCharacter && Game.Player.Character.IsAlive && Profile.Detect()==Profile.Id;
        public static bool Allowed=>!Game.IsPaused && !Game.IsMissionActive && !InterfaceOptions.ModalOpen && !GameplayContext.SceneBlocking && Game.Player.CanControlCharacter && Game.Player.Character.IsAlive && Profile.Detect()==Profile.Id;
        public static bool Pressed(GTA.Control control)=>Game.IsControlJustPressed(control) || Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED,0,(int)control);
    }
    // Shared navigation/confirmation across the whole business, with the existing per-service input queue.
    internal sealed class PdmMenuView
    {
        sealed class Row {public string Title,Text;public Action Action;public bool Enabled;}
        sealed class Page {public string Key,Name;public Action Back,Refresh;public PdmCommandQuote Quote;public int Index;public bool Remember;public readonly List<Row> Rows=new List<Row>();}
        readonly PdmMenuGroup group;readonly NativeMenu menu;readonly PersonalServiceMenu ui;readonly Action<Exception> failed;readonly Dictionary<string,int> indices=new Dictionary<string,int>();Page page;
        public Action DefaultRefresh;bool preserveLoading;
        string loadingNames;Func<bool> loadingTerrain;Action loading,loadingBack;string loadingProfile;int loadingActor,loadingModel,loadingAt;long loadingAddress;GTA.Math.Vector3 loadingOrigin;Model[] models;
        public PdmMenuView(LemonUI.ObjectPool pool,NativeMenu menu,Action<Exception> failed){this.menu=menu;this.failed=failed;ui=new PersonalServiceMenu(menu,failed);group=PdmMenuGroup.For(pool);group.Register(this,menu,SuspendPage);ui.CanNavigate=()=>group.Owns(this);}
        public void Begin(string key,Action back=null,bool remember=true,Action refresh=null)
        {if(loading!=null && key!="preparing")StopLoading();preserveLoading=key=="preparing" && loading!=null;try{if(!group.Activate(this))throw new InvalidOperationException("PDM navigation is already changing.");}finally{preserveLoading=false;}int index=remember && indices.TryGetValue(key,out int cached)?cached:0;page=new Page{Key=key,Back=back,Refresh=refresh??DefaultRefresh,Index=index,Remember=remember};ui.Begin(key,back,remember);}
        public void Add(string title,string text,Action action,bool enabled=true)
        {if(page!=null)page.Rows.Add(new Row{Title=title,Text=text,Action=action,Enabled=enabled});}
        public void Show()
        {
            page.Name=menu.Name;Render(page,page.Index);
        }
        void Render(Page current,int index)
        {
            if(!group.Owns(this))return;page=current;int count=current.Rows.Count;
            bool footer=count>0 && (current.Rows[count-1].Title.StartsWith("Voltar") || current.Rows[count-1].Title=="Fechar");
            int content=count-(footer?1:0),pages=Math.Max(1,(content+11)/12);index=Math.Max(0,Math.Min(index,pages-1));current.Index=index;if(indices.Count>128)indices.Clear();indices[current.Key]=index;
            ui.Begin(current.Key+":"+index,current.Back,current.Remember);menu.Name=current.Name+(pages>1?" / "+(index+1)+"/"+pages:"");
            for(int i=index*12;i<Math.Min(content,(index+1)*12);i++){var row=current.Rows[i];ui.Add(row.Title,row.Text,row.Action,row.Enabled);}
            if(index>0)ui.Add("Pagina anterior","Itens anteriores.",()=>Render(current,index-1));
            if(index+1<pages)ui.Add("Proxima pagina","Mais itens.",()=>Render(current,index+1));
            if(footer){var row=current.Rows[count-1];ui.Add(row.Title,row.Text,row.Action,row.Enabled);}
            menu.Visible=true;
        }
        void Restore(Page previous)
        {
            Begin(previous.Key,previous.Back,previous.Remember,previous.Refresh);menu.Name=previous.Name;
            foreach(var row in previous.Rows)Add(row.Title,row.Text,row.Action,row.Enabled);Show();
        }
        public void Confirm(string title,string text,Action action,Action refresh=null,PdmCommandQuote quoted=null)
        {
            var previous=page;previous.Name=menu.Name;
            var quote=quoted??new PdmCommandQuote(Wallet.Pdm,Wallet.Bank,Profile.Id,Game.GameTime);
            Action cancel=refresh??previous.Refresh??(()=>Restore(previous));
            if(!quote.Matches(Wallet.Pdm,Wallet.Bank,Profile.Id,Game.GameTime)){Notify.Show("Condicoes atualizadas. Confira novamente.");cancel();return;}
            Begin("confirm:"+previous.Key,cancel,false);menu.Name="Confirmar / "+title;
            Add("Cancelar / voltar","Nenhuma operacao foi confirmada.",cancel);
            Add("Confirmar: "+title,text+" Banco pessoal $"+Wallet.Bank+"; caixa empresarial $"+Wallet.Pdm.Cash+".",()=>{
                if(!quote.Take(Wallet.Pdm,Wallet.Bank,Profile.Id,Game.GameTime)){Notify.Show("Saldo, estoque ou contrato mudou. Confira novamente.");cancel();return;}action();
            });Show();
        }
        public void Problem(string title,string reason,Exception error=null,Action back=null)
        {
            var previous=page;var goBack=back??previous?.Refresh??DefaultRefresh??Close;
            string reference=Guid.NewGuid().ToString("N").Substring(0,8);
            if(error!=null)Log.Error("PDM failure ["+reference+"] "+title,error);else Log.Write("PDM refusal ["+reference+"] "+title+" / "+reason);
            Begin("problem",goBack,false);menu.Name="PDM / "+title;
            Add("Motivo / "+reference,reason,()=>{},false);
            if(error!=null)Add("Erro: "+error.GetType().Name,"Referencia "+reference+" no RPCore.log. "+(error.Message??"").Replace("~"," "),()=>{},false);
            Add("Voltar / conferir dados","Reabrir o atendimento. Operacoes nao sao repetidas automaticamente.",goBack);Show();
            Notify.Show("PDM: "+title+" / "+reference+". Motivo na tela.");
        }
        public void AddConfirm(string title,string text,Action action,bool enabled=true)
        {var quote=page.Quote??(page.Quote=new PdmCommandQuote(Wallet.Pdm,Wallet.Bank,Profile.Id,Game.GameTime));Add(title,text,()=>Confirm(title,text,action,null,quote),enabled);}
        public void Stream(IEnumerable<string> names,Action action,Action back,Func<bool> terrainReady=null)
        {
            var list=new List<Model>();var labels=new List<string>();foreach(var name in names){var model=new Model(name);if(!model.IsValid || !model.IsInCdImage){Problem("Modelo indisponivel","Recurso "+name+" nao esta instalado/disponivel. Pedidos existentes preservados; nenhuma operacao nova iniciada.",null,back);return;}if(!list.Exists(m=>m.Hash==model.Hash)){list.Add(model);labels.Add(name);}}
            loadingNames=string.Join(", ",labels);models=list.ToArray();loadingTerrain=terrainReady;loading=action;loadingBack=back;loadingProfile=Profile.Id;var ped=Game.Player.Character;loadingActor=ped.Handle;loadingModel=ped.Model.Hash;loadingAddress=ped.MemoryAddress.ToInt64();loadingOrigin=ped.Position;loadingAt=Game.GameTime;
            Begin("preparing",CancelLoading,false);menu.Name="Preparando recursos PDM";
            Add("Cancelar","Verificando modelos e colisao/chao do local: "+loadingNames+". Pedidos ja pagos continuam salvos. Nenhum contrato ou pagamento novo foi iniciado.",CancelLoading);Show();
        }
        void CancelLoading(){var back=loadingBack;StopLoading();ui.Close();back?.Invoke();}
        void StopLoading(){if(models!=null)foreach(var model in models)PdmStreaming.Release(model,true);models=null;loadingNames=null;loadingTerrain=null;loading=null;loadingBack=null;}
        public void Tick(bool allowed)
        {
            if(!group.Owns(this)){if(menu.Visible || loading!=null)ClosePage();return;}
            ui.Tick(allowed);
            if(!menu.Visible && loading==null){group.Release(this);return;}
            if(loading==null)return;
            try{
            var ped=Game.Player.Character;
            if(!menu.Visible || !allowed || Profile.Id!=loadingProfile || ped.Handle!=loadingActor || ped.Model.Hash!=loadingModel || loadingAddress==0 || ped.MemoryAddress.ToInt64()!=loadingAddress || ped.Position.DistanceTo(loadingOrigin)>5){StopLoading();ui.Close();Notify.Show("Preparacao PDM interrompida; nenhuma operacao iniciada.");return;}
            bool ready=true;foreach(var model in models)if(!PdmStreaming.Ready(model))ready=false;
            bool terrain=loadingTerrain?.Invoke()??GameplayContext.TerrainReady;
            if(unchecked((uint)(Game.GameTime-loadingAt))>10000){
                var back=loadingBack;string reason=!ready?"Modelos nao carregaram a tempo: "+loadingNames:"Colisao/chao do local ainda nao ficou pronto. Aguarde parado junto ao marcador e tente novamente.";
                StopLoading();Problem("Preparacao nao concluida",reason+" Pedidos pagos preservados; nenhum debito novo.",null,back);return;
            }
            if(!ready || !terrain)return;var action=loading;loading=null;loadingBack=null;ui.Close();
            try{action();}finally{StopLoading();}
            }catch(Exception ex){StopLoading();ui.Close();failed(ex);}
        }
        // Preserve preparation only while this same view builds its preparation page.
        // Every transfer/escape cancels the old request before another menu receives input.
        void SuspendPage(){if(!preserveLoading)StopLoading();ui.Close();}
        void ClosePage(){StopLoading();ui.Close();}
        public void HideAll(){group.HideAll();}
        public void Waypoint(GTA.Math.Vector3 destination,string name)=>group.Waypoint(destination,name);
        public void Close(){ClosePage();group.Release(this);}
        public void Reset(){Close();ui.Reset();page=null;indices.Clear();}
    }
}
