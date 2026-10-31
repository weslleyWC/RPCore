using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using LemonUI.Menus;
namespace RPCore
{
    internal static class PdmMenuRuntime
    {
        public static bool WorldAllowed=>!Game.IsPaused && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && Game.Player.CanControlCharacter && Game.Player.Character.IsAlive && Profile.Detect()==Profile.Id;
        public static bool Allowed=>!Game.IsPaused && !Game.IsMissionActive && !InterfaceOptions.ModalOpen && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && Game.Player.CanControlCharacter && Game.Player.Character.IsAlive && Profile.Detect()==Profile.Id;
        public static bool Pressed(GTA.Control control)=>Game.IsControlJustPressed(control) || Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED,0,(int)control);
    }
    // Shared navigation/confirmation across the whole business, with the existing per-service input queue.
    internal sealed class PdmMenuView
    {
        sealed class Row {public string Title,Text;public Action Action;public bool Enabled;}
        sealed class Page {public string Key,Name;public Action Back,Refresh;public PdmCommandQuote Quote;public int Index;public bool Remember;public readonly List<Row> Rows=new List<Row>();}
        readonly NativeMenu menu;readonly PersonalServiceMenu ui;readonly Action<Exception> failed;readonly Dictionary<string,int> indices=new Dictionary<string,int>();Page page;
        public Action DefaultRefresh;
        Action loading,loadingBack;string loadingProfile;int loadingActor,loadingModel,loadingAt;long loadingAddress;GTA.Math.Vector3 loadingOrigin;Model[] models;
        public PdmMenuView(NativeMenu menu,Action<Exception> failed){this.menu=menu;this.failed=failed;ui=new PersonalServiceMenu(menu,failed);}
        public void Begin(string key,Action back=null,bool remember=true,Action refresh=null)
        {if(loading!=null && key!="preparing")StopLoading();int index=remember && indices.TryGetValue(key,out int cached)?cached:0;page=new Page{Key=key,Back=back,Refresh=refresh??DefaultRefresh,Index=index,Remember=remember};ui.Begin(key,back,remember);}
        public void Add(string title,string text,Action action,bool enabled=true)
        {if(page!=null)page.Rows.Add(new Row{Title=title,Text=text,Action=action,Enabled=enabled});}
        public void Show()
        {
            page.Name=menu.Name;Render(page,page.Index);
        }
        void Render(Page current,int index)
        {
            page=current;int count=current.Rows.Count;
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
        public void AddConfirm(string title,string text,Action action,bool enabled=true)
        {var quote=page.Quote??(page.Quote=new PdmCommandQuote(Wallet.Pdm,Wallet.Bank,Profile.Id,Game.GameTime));Add(title,text,()=>Confirm(title,text,action,null,quote),enabled);}
        public void Stream(IEnumerable<string> names,Action action,Action back)
        {
            var list=new List<Model>();foreach(var name in names){var model=new Model(name);if(!model.IsValid || !model.IsInCdImage){Notify.Show("Modelo indisponivel: "+name+". Nenhuma operacao confirmada.");return;}if(!list.Exists(m=>m.Hash==model.Hash))list.Add(model);}
            models=list.ToArray();loading=action;loadingBack=back;loadingProfile=Profile.Id;var ped=Game.Player.Character;loadingActor=ped.Handle;loadingModel=ped.Model.Hash;loadingAddress=ped.MemoryAddress.ToInt64();loadingOrigin=ped.Position;loadingAt=Game.GameTime;
            Begin("preparing",CancelLoading,false);menu.Name="Preparando recursos PDM";
            Add("Cancelar","Carregamento sem bloquear o jogo; nenhum contrato ou pagamento foi iniciado.",CancelLoading);Show();
        }
        void CancelLoading(){var back=loadingBack;StopLoading();ui.Close();back?.Invoke();}
        void StopLoading(){if(models!=null)foreach(var model in models)PdmStreaming.Release(model,true);models=null;loading=null;loadingBack=null;}
        public void Tick(bool allowed)
        {
            ui.Tick(allowed);
            if(loading==null)return;
            try{
            var ped=Game.Player.Character;
            if(!menu.Visible || !allowed || Profile.Id!=loadingProfile || ped.Handle!=loadingActor || ped.Model.Hash!=loadingModel || loadingAddress==0 || ped.MemoryAddress.ToInt64()!=loadingAddress || ped.Position.DistanceTo(loadingOrigin)>5 || unchecked((uint)(Game.GameTime-loadingAt))>10000){StopLoading();ui.Close();Notify.Show("Preparacao PDM interrompida; nenhuma operacao iniciada.");return;}
            bool ready=true;foreach(var model in models)if(!PdmStreaming.Ready(model))ready=false;
            if(!ready)return;var action=loading;loading=null;loadingBack=null;ui.Close();
            try{action();}finally{StopLoading();}
            }catch(Exception ex){StopLoading();ui.Close();failed(ex);}
        }
        public void Close(){StopLoading();ui.Close();}
        public void Reset(){StopLoading();ui.Reset();page=null;indices.Clear();}
    }
}
