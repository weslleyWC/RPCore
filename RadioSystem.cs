using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GTA;
using LemonUI;
using LemonUI.Menus;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class RadioSystem : ISystem
    {
        public string Name=>"radio";
        readonly ObjectPool pool;readonly NativeMenu menu=new NativeMenu("RPCore Radio","ESTACOES / REPRODUCAO");readonly PersonalServiceMenu ui;readonly AchievementSystem ach;
        readonly RadioSession session=new RadioSession();List<RadioStation> stations=new List<RadioStation>();RadioPreferences preferences;
        int selected,lastClock;bool stationsPage,volumePage;string profile;RadioStatus displayed;
        public static List<RadioStation> ReadStations(string path)=>RadioRules.ReadStations(path);
        public RadioSystem(ObjectPool pool,AchievementSystem ach){this.pool=pool;this.ach=ach;pool.Add(menu);Visuals.Register(menu,"radio");ui=new PersonalServiceMenu(menu,e=>{Log.Error("radio menu",e);Notify.Show("Radio: acao nao concluida.");ui.Close();});session.Error=e=>Log.Error("radio playback",e);Reload();}
        void Row(string title,string desc,Action action,bool enabled=true)=>ui.Add(title,desc,action,enabled);
        string Status=>session.Status==RadioStatus.Playing?"Tocando":session.Status==RadioStatus.Paused?"Pausado":session.Status==RadioStatus.Connecting?"Conectando":session.Status==RadioStatus.Failed?"Falha no stream":"Parado";
        void Root()
        {
            stationsPage=volumePage=false;ui.Begin("root");menu.Name=Status+" / "+(stations.Count>0?stations[Math.Min(selected,stations.Count-1)].Name:"nenhuma estacao");
            Row("Estacoes ("+stations.Count+")","Selecionar uma estacao inicia reproducao; destacar nao inicia nada.",Stations);
            Row(session.Status==RadioStatus.Paused?"Continuar":"Pausar","Mantem a mesma conexao; sem nova conquista ao retomar.",()=>{session.Toggle();Root();},session.Status==RadioStatus.Playing || session.Status==RadioStatus.Paused);
            Row("Anterior","Toca a estacao anterior da sua lista.",()=>Play(-1),stations.Count>0);
            Row("Proxima","Toca a proxima estacao da sua lista.",()=>Play(1),stations.Count>0);
            Row("Volume: "+(preferences?.Valid==true?preferences.Volume.ToString():"dados protegidos"),"Preferencia do perfil, sem editar a lista XML.",Volume);
            Row("Parar","Fecha e libera somente o player criado pelo RPCore.",()=>{session.Stop();Root();});
            Row("Recarregar lista XML","Preserva stream atual se a URL continuar na lista. XML invalido conserva a lista anterior.",()=>{LoadStations(true);Root();});
            Row("Fechar menu","A reproducao continua ao fechar o menu.",()=>ui.Close());menu.Visible=true;displayed=session.Status;
        }
        void Stations()
        {
            stationsPage=true;volumePage=false;ui.Begin("stations",Root);menu.Name="Estacoes / "+Status;
            for(int i=0;i<stations.Count;i++){int index=i;var station=stations[i];Row((session.Url==station.Url && session.Status!=RadioStatus.Stopped?"[atual] ":"")+station.Name,station.Url,()=>{selected=index;Play(0);Stations();});}
            if(stations.Count==0)Row("Nenhuma URL valida","Configure stream direto http/https em CustomRadioStream.xml; pagina HTML nao e audio.",()=>{},false);
            Row("Voltar","Controles de reproducao.",Root);menu.Visible=true;
        }
        void Play(int delta)
        {
            if(stations.Count==0)return;selected=(selected+delta+stations.Count)%stations.Count;
            int volume=preferences?.Valid==true?preferences.Volume:50;
            if(!session.Start(stations[selected].Url,volume,()=>new RadioPlayback()))Notify.Show("Radio indisponivel. Confira Windows Media Player e RPCore.log.");
            if(!stationsPage)Root();
        }
        void Volume()
        {
            volumePage=true;stationsPage=false;ui.Begin("volume",Root);menu.Name="Volume / "+(preferences?.Volume.ToString()??"--");
            Row("Diminuir 10","Faixa 0 a100; salva antes de aplicar ao player.",()=>SetVolume(-10),preferences?.Valid==true);
            Row("Aumentar 10","Preserva valores personalizados intermediarios.",()=>SetVolume(10),preferences?.Valid==true);
            Row("Voltar","Controles.",Root);menu.Visible=true;
        }
        void SetVolume(int delta)
        {
            if(preferences?.Valid!=true)return;var next=JsonConvert.DeserializeObject<RadioPreferences>(JsonConvert.SerializeObject(preferences));next.Volume=Math.Max(0,Math.Min(100,next.Volume+delta));
            Store.Write("RadioPreferences.json",next);preferences=next;session.Volume(next.Volume);Volume();
        }
        void LoadStations(bool notify)
        {
            try{
                var next=ReadStations(Path.Combine(Cfg.Dir,"CustomRadioStream.xml"));string playing=session.Url;
                int index=next.FindIndex(s=>s.Url==playing);if(playing!=null && index<0)session.Stop();stations=next;selected=index>=0?index:0;
            }catch(Exception e){Log.Error("radio XML",e);if(notify)Notify.Show("XML invalido; lista anterior e arquivo original preservados.");}
        }
        public void Reload(){session.Stop();ui.Reset();stations=new List<RadioStation>();selected=0;LoadStations(false);try{preferences=LeisureContext.LoadProgress("RadioPreferences.json",()=>new RadioPreferences());}catch(Exception e){preferences=null;Log.Error("radio preferences retained",e);}profile=Profile.Id;lastClock=Game.GameTime;}
        public void Tick()
        {
            ui.Tick(LeisureContext.CanOpen);int now=Game.GameTime,delta=NeedsRules.Delta(now,lastClock,!Game.IsPaused);lastClock=now;
            if(profile!=Profile.Id || Profile.Detect()!=Profile.Id || Game.Player.Character.IsDead || Game.IsMissionActive || GTA.Native.Function.Call<bool>(GTA.Native.Hash.IS_CUTSCENE_ACTIVE)){session.Stop();return;}
            var before=session.Status;if(session.Tick(delta,Game.IsPaused)){ach.Bump("radio_started");}
            if(before!=RadioStatus.Failed && session.Status==RadioStatus.Failed)Notify.Show("Falha no stream. Use URL de audio direta e confira RPCore.log.");
            if(menu.Visible && displayed!=session.Status && !stationsPage && !volumePage)Root();
        }
        public void OnKey(KeyEventArgs e){if(e.KeyCode!=Cfg.RadioKey)return;if(menu.Visible)ui.Close();else if(!pool.AreAnyVisible && LeisureContext.CanOpen)Root();}
        public void Save(){}
        public void Dispose(){ui.Reset();session.Stop();}
    }
}
