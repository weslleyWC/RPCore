using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using LemonUI;
namespace RPCore
{
    public sealed partial class PhoneSystem : ISystem
    {
        public string Name=>"phone";
        readonly ObjectPool pool;readonly Action<Keys> dispatch;readonly PropPresentation props;readonly InventorySystem inventory;readonly NeedsSystem needs;
        readonly Action jobs,studio,passenger;readonly UberJob uber;readonly StreamerJob streamer;
        readonly string[] shortcuts={"RPCore Bank","Inventario","Garagens","Bolsa de valores","Radio","Gangues","Turno de pizza","Configuracoes F11","Trabalhos / contratos"};
        Dictionary<int,Action> services=new Dictionary<int,Action>();
        PhoneData data;bool readOnly;
        readonly PhoneClockState clock=new PhoneClockState();
        int selection,row,detailPage,noteIndex,lastClock,pressed=-1;string page="Home",query="",result="0",noteExpected,detailReturn;
        List<string> detail=new List<string>();
        bool mouseReady,mouseHeld,controllerReady;Action pending;int pendingActor,pendingAt;string pendingProfile;
        public bool Visible{get;private set;}
        public PhoneSystem(ObjectPool pool,Action<Keys> dispatch,PropPresentation props,InventorySystem inventory,NeedsSystem needs,Action jobs=null,UberJob uber=null,StreamerJob streamer=null,Action studio=null,Action passenger=null)
        {
            this.pool=pool;this.dispatch=dispatch;this.props=props;this.inventory=inventory;this.needs=needs;this.jobs=jobs;this.uber=uber;this.streamer=streamer;this.studio=studio;this.passenger=passenger;
            if(uber!=null)uber.Activity+=JobActivity;if(streamer!=null)streamer.Activity+=JobActivity;needs.Alert+=JobActivity;inventory.Used+=ItemUsed;Reload();
        }
        public void ConfigureServices(Dictionary<int,Action> callbacks){services=callbacks??new Dictionary<int,Action>();}
        void JobActivity(string title,string body){try{if(!readOnly)PhoneEdits.Append(ref data,title,body,title=="Uber" || title=="Twitch" || title=="Streamer"?"message":"notification",n=>Store.Write("Phone.json",n));}catch(Exception ex){Log.Error("phone activity",ex);}}
        void ItemUsed(ItemDef item)=>JobActivity("Inventario","Usou "+item.Name);
        bool Safe()
        {
            var p=Game.Player.Character;return p!=null && p.Exists() && p.IsHuman && !p.IsDead && !p.IsRagdoll && !p.IsFalling && !p.IsSwimming && !p.IsClimbing && !p.IsInCombat && !Game.IsPaused && !Game.IsMissionActive && !GameplayContext.SceneBlocking && Game.Player.CanControlCharacter && Profile.Detect()==Profile.Id && !inventory.UsingItem && (!p.IsInVehicle() || p.CurrentVehicle.Driver==p && p.CurrentVehicle.Speed<=.5f);
        }
        void Close(bool cancel=true){bankOperation=null;bankReceipt=null;Visible=false;props?.ClosePhone();page="Home";mouseReady=false;controllerReady=false;pressed=-1;if(cancel)pending=null;}
        void OpenPage(string name){page=name;row=0;detailPage=0;pressed=-1;}
        int[] Matches()=>Enumerable.Range(0,shortcuts.Length).Where(i=>shortcuts[i].IndexOf(query??"",StringComparison.OrdinalIgnoreCase)>=0).ToArray();
        void Queue(Action action){Close(false);pending=action;pendingActor=Game.Player.Character.Handle;pendingProfile=Profile.Id;pendingAt=Game.GameTime;}
        void Shortcut(int index)
        {
            if(index==0){OpenBank();return;}
            if((index==2 && !Cfg.EnableGarages) || (index==4 && !Cfg.EnableRadio) || (index==5 && !Cfg.EnableGangs)){Notify.Show("Servico desativado na configuracao.");return;}
            if(services.TryGetValue(index,out var action)){Queue(action);return;}
            if(index==8){Queue(jobs);return;}
            Keys key=index==0?Cfg.CoreKey:index==1?Cfg.InventoryKey:index==2?Cfg.GarageKey:index==3?Cfg.StocksKey:index==4?Cfg.RadioKey:index==5?Cfg.GangsKey:index==6?Cfg.JobKey:InterfaceOptions.SettingsKey;Queue(()=>dispatch(key));
        }
        string Input(string initial,int length)
        {
            string profile=Profile.Id,origin=page;int actor=Game.Player.Character.Handle;string text=Game.GetUserInput(WindowTitle.EnterMessage60,initial??"",length);
            return Visible && Safe() && profile==Profile.Id && actor==Game.Player.Character.Handle && origin==page?text:null;
        }
        bool Edit(Func<bool> edit)
        {
            if(readOnly){Notify.Show("Dados do celular indisponiveis. Confira RPCore.log.");return false;}
            try{if(edit())return true;Notify.Show("Alteracao nao confirmada.");}catch(Exception ex){Log.Error("phone edit",ex);Notify.Show("Alteracao nao gravada; dados anteriores conservados.");}return false;
        }
        void Activate()
        {
            if(PhoneBankRules.IsPage(page) || page=="Home" && selection==PhoneBankRules.App){if(bankHeld)return;bankHeld=true;}
            try{ActivateCore();}catch(Exception ex){Log.Error("phone action",ex);Notify.Show("Acao nao concluida. Confira RPCore.log.");}
        }
        void ActivateCore()
        {
            if(!Visible || !Safe())return;
            if(page=="Home"){if(selection==PhoneBankRules.App){OpenBank();return;}OpenPage(ReferenceLayout.PhoneLabels[selection]);if(page=="Radio")Shortcut(4);return;}
            if(PhoneBankRules.IsPage(page)){BankAction();return;}
            if(page=="Twitch"){int i=row;Queue(()=>streamer?.PhoneAction(i));return;}
            if(page=="Uber"){int i=row;Queue(()=>uber?.PhoneAction(i));return;}
            if(page=="Settings"){if(row==0)Edit(()=>PhoneEdits.Change(ref data,n=>{n.Wallpaper=n.Wallpaper%10+1;return true;},n=>Store.Write("Phone.json",n)));else Shortcut(row-1);return;}
            if(page=="Search"){if(row==0){string text=Input(query,32);if(text!=null)query=text;}else{var matches=Matches();if(row-1<matches.Length)Shortcut(matches[row-1]);}return;}
            if(page=="Contacts")
            {
                if(row<5)Shortcut(new[]{0,2,6,5,8}[row]);else if(row==5 && studio!=null)Queue(studio);else if(passenger!=null)Queue(passenger);return;
            }
            if(page=="Notes")
            {
                if(row==0){string text=Input("",120);if(text!=null)Edit(()=>PhoneEdits.Note(ref data,-1,null,text,n=>Store.Write("Phone.json",n)));return;}
                noteIndex=row-1;if(noteIndex>=data.Notes.Count)return;noteExpected=data.Notes[noteIndex];detail=PhoneViewRules.Pages(noteExpected);OpenPage("NoteView");return;
            }
            if(page=="NoteView")
            {
                if(row==0){string text=Input(noteExpected,120);if(text!=null && Edit(()=>PhoneEdits.Note(ref data,noteIndex,noteExpected,text,n=>Store.Write("Phone.json",n))))OpenPage("Notes");}
                else if(row==1)OpenPage("DeleteNote");else OpenPage("Notes");return;
            }
            if(page=="DeleteNote")
            {
                if(row==0)OpenPage("NoteView");else if(Edit(()=>PhoneEdits.DeleteNote(ref data,noteIndex,noteExpected,n=>Store.Write("Phone.json",n))))OpenPage("Notes");return;
            }
            if(page=="Messages" || page=="Notifications")
            {
                var entries=PhoneEdits.History(data,page=="Messages");if(row>=entries.Length)return;int index=entries[row];var entry=data.History[index];
                if(!entry.Read && !Edit(()=>PhoneEdits.Read(ref data,index,entry,n=>Store.Write("Phone.json",n))))return;
                detailReturn=page;detail=PhoneViewRules.Pages((entry.Title??"RPCore")+"~n~~n~"+(entry.Body??""));OpenPage("HistoryView");return;
            }
            if(page=="Clock"){OpenPage(row==0?"Stopwatch":"Timer");return;}
            if(page=="Stopwatch"){if(row==0)clock.Stopwatch();else if(row==1)clock.ResetStopwatch();else OpenPage("Clock");return;}
            if(page=="Timer")
            {
                if(row==0){string text=Input("5",4);if(text!=null && (!PhoneViewRules.Amount(text,out int minutes) || !clock.SetMinutes(minutes)))Notify.Show("Escolha de1 a1440 minutos.");}
                else if(row==1)clock.Timer();else if(row==2)clock.ResetTimer();else OpenPage("Clock");return;
            }
            if(page=="Calculator"){string expression=Input("",60);if(expression!=null){PhoneMath.Calculate(expression,out var answer);result=expression+"~n~="+answer;}}
        }
        string[] Rows()
        {
            switch(page)
            {
                case "Bank":return new[]{"Depositar","Sacar","Depositar tudo","Sacar tudo"};
                case "BankConfirm":return new[]{"Cancelar / voltar","Confirmar "+(bankOperation?.Deposit==true?"deposito":"saque")};
                case "BankReceipt":return new[]{"Voltar a conta"};
                case "Notes":return new[]{"+ Nova nota"}.Concat(data.Notes).ToArray();
                case "NoteView":return new[]{"Editar","Excluir...","Voltar"};
                case "DeleteNote":return new[]{"Cancelar / manter nota","Confirmar exclusao"};
                case "Settings":return new[]{"Papel de parede: "+data.Wallpaper}.Concat(shortcuts).ToArray();
                case "Search":return new[]{"Buscar: "+query}.Concat(Matches().Select(i=>shortcuts[i])).ToArray();
                case "Contacts":return new[]{"RPCore Bank","Mecanico / garagens","Pizza This / trabalho","Gangue","Trabalhos / contratos"}.Concat(studio==null?Array.Empty<string>():new[]{"Lester / Hollywood Act"}).Concat(passenger==null?Array.Empty<string>():new[]{"Taxi / limousine"}).ToArray();
                case "Messages":case "Notifications":return PhoneEdits.History(data,page=="Messages").Select(i=>(data.History[i].Read?"":"* ")+(data.History[i].Title??"RPCore")+": "+(data.History[i].Body??"")).ToArray();
                case "Clock":return new[]{"Cronometro","Temporizador"};
                case "Stopwatch":return new[]{clock.StopwatchRunning?"Pausar":"Iniciar / continuar","Zerar","Voltar"};
                case "Timer":return new[]{"Definir minutos",clock.TimerRunning?"Pausar":"Iniciar / continuar","Zerar","Voltar"};
                case "Uber":return uber?.PhoneRows()??new[]{"Uber indisponivel"};
                case "Twitch":return streamer?.PhoneRows()??new[]{"Streamer indisponivel"};
                default:return Array.Empty<string>();
            }
        }
        float RowTop=>page=="Bank"?220:page=="BankConfirm" || page=="BankReceipt"?303:page=="NoteView"?275:page=="Stopwatch" || page=="Timer"?220:78;
        void Navigate(int dx,int dy)
        {
            pressed=-1;if(page=="Home")selection=PhoneBankRules.MoveHome(selection,dx,dy);
            else if(page=="HistoryView")detailPage=Math.Max(0,Math.Min(detail.Count-1,detailPage+dy+dx));
            else if(page=="NoteView" && dx!=0)detailPage=Math.Max(0,Math.Min(detail.Count-1,detailPage+dx));
            else row=Math.Max(0,Math.Min(Math.Max(0,Rows().Length-1),row+dy));
        }
        void Back(){if(page=="BankConfirm" || page=="BankReceipt"){OpenBank();return;}if(page=="Bank"){bankOperation=null;bankReceipt=null;OpenPage("Home");return;}if(page=="Home")Close();else if(page=="NoteView" || page=="DeleteNote")OpenPage("Notes");else if(page=="HistoryView")OpenPage(detailReturn??"Notifications");else if(page=="Timer" || page=="Stopwatch")OpenPage("Clock");else OpenPage("Home");}
        public void OnKey(KeyEventArgs e)
        {
            if(e.KeyCode==Visuals.PhoneKey){if(Visible)Close();else if(Visuals.Enabled && Visuals.PhoneEnabled && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen && Safe()){Visible=true;OpenPage("Home");props?.ShowPhone();mouseReady=false;controllerReady=false;}return;}
            if(!Visible || e.Modifiers!=Keys.None)return;pressed=-1;
            if(e.KeyCode==Keys.Escape || e.KeyCode==Keys.Back)Back();else if(e.KeyCode==Keys.Left)Navigate(-1,0);else if(e.KeyCode==Keys.Right)Navigate(1,0);else if(e.KeyCode==Keys.Up)Navigate(0,-1);else if(e.KeyCode==Keys.Down)Navigate(0,1);else if(e.KeyCode==Keys.Enter)Activate();
            else if(e.KeyCode==Keys.Delete && page=="Notes" && row>0 && row<=data.Notes.Count){noteIndex=row-1;noteExpected=data.Notes[noteIndex];detail=PhoneViewRules.Pages(noteExpected);OpenPage("DeleteNote");}
        }
        public void Tick()
        {
            int now=Game.GameTime,delta=NeedsRules.Delta(now,lastClock,!Game.IsPaused && !Game.IsMissionActive && Profile.Detect()==Profile.Id);lastClock=now;
            if(clock.Advance(delta)){Notify.Show("Temporizador RPCore concluido.");JobActivity("Relogio","Temporizador concluido.");}
            if(pending!=null){var action=pending;pending=null;if(Safe() && pendingActor==Game.Player.Character.Handle && pendingProfile==Profile.Id && !InterfaceOptions.ModalOpen && !pool.AreAnyVisible && unchecked(now-pendingAt)>=0 && unchecked(now-pendingAt)<1000)try{action();}catch(Exception ex){Log.Error("phone service",ex);Notify.Show("Servico nao aberto. Confira RPCore.log.");}}
            if(!Visible)return;
            if(!Game.IsKeyPressed(Keys.Enter) && !Game.IsKeyPressed(Keys.LButton) && !Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED,0,(int)GTA.Control.CursorAccept) && !Game.IsControlPressed(GTA.Control.FrontendAccept) && !Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED,0,(int)GTA.Control.FrontendAccept))bankHeld=false;
            if(!Visuals.Enabled || !Visuals.PhoneEnabled || !Safe() || pool.AreAnyVisible || inventory.GridVisible){Close();return;}
            foreach(var c in new[]{GTA.Control.Attack,GTA.Control.Attack2,GTA.Control.Aim,GTA.Control.Phone,GTA.Control.SelectWeapon,GTA.Control.SelectWeaponUnarmed,GTA.Control.SelectWeaponMelee,GTA.Control.SelectWeaponHandgun,GTA.Control.SelectWeaponShotgun,GTA.Control.SelectWeaponSmg,GTA.Control.SelectWeaponAutoRifle,GTA.Control.SelectWeaponSniper,GTA.Control.SelectWeaponHeavy,GTA.Control.NextWeapon,GTA.Control.PrevWeapon,GTA.Control.FrontendPause,GTA.Control.FrontendPauseAlternate})Game.DisableControlThisFrame(c);
            if(Function.Call<bool>(Hash.IS_USING_KEYBOARD_AND_MOUSE,0)){controllerReady=false;MouseTick();}
            else
            {
                mouseReady=false;pressed=-1;
                if(!controllerReady)controllerReady=!new[]{GTA.Control.FrontendAccept,GTA.Control.FrontendCancel,GTA.Control.FrontendLeft,GTA.Control.FrontendRight,GTA.Control.FrontendUp,GTA.Control.FrontendDown}.Any(Game.IsControlPressed);
                else{if(Game.IsControlJustPressed(GTA.Control.FrontendLeft))Navigate(-1,0);if(Game.IsControlJustPressed(GTA.Control.FrontendRight))Navigate(1,0);if(Game.IsControlJustPressed(GTA.Control.FrontendUp))Navigate(0,-1);if(Game.IsControlJustPressed(GTA.Control.FrontendDown))Navigate(0,1);if(Game.IsControlJustPressed(GTA.Control.FrontendAccept)){Activate();return;}if(Game.IsControlJustPressed(GTA.Control.FrontendCancel)){Back();return;}}
            }
            if(Visible)Draw();
        }
        public void Reload()
        {
            Close();data=Store.Load("Phone.json",()=>new PhoneData());readOnly=!PhoneEdits.Valid(data);if(readOnly){Log.Write("Invalid Phone data retained; editing blocked.");data=new PhoneData();}
            selection=PhoneBankRules.App;row=detailPage=0;query="";result="0";clock.ResetStopwatch();clock.ResetTimer();lastClock=Game.GameTime;
        }
        public void Save(){if(readOnly)throw new InvalidOperationException("Original Phone data retained.");Store.Write("Phone.json",data);}
        public void Dispose(){if(uber!=null)uber.Activity-=JobActivity;if(streamer!=null)streamer.Activity-=JobActivity;inventory.Used-=ItemUsed;needs.Alert-=JobActivity;Close();}
    }
}
