using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
namespace RPCore
{
    public sealed class WorldSystem : ISystem
    {
        public string Name=>"world_controls";
        public event Action ClockChanged;
        readonly ObjectPool pool;readonly ShiftGate gate;readonly JsonFiles files=new JsonFiles();readonly Random random=new Random();
        readonly NativeMenu menu=new NativeMenu("Mundo","Clima e horario");readonly HttpClient client=new HttpClient{Timeout=TimeSpan.FromSeconds(8)};
        readonly string path;WorldOptions options;bool protectedFile;string signature,lastWeather,status="Sem consulta";int last,dynamicClock,realClock,clock,version,requestVersion;
        Task<WeatherReading> request;CancellationTokenSource cancel;WeatherReading reading;
        public WorldSystem(ObjectPool pool,ShiftGate gate)
        {
            this.pool=pool;this.gate=gate;path=Path.Combine(Cfg.Dir,"World","Settings.json");
            options=Store.LoadDef("World/Defaults.json",WorldOptions.Defaults);
            try{options=files.Read(path,()=>WorldRules.Copy(options));signature=Digest();if(options?.Valid!=true)throw new InvalidDataException("Configuracao de mundo invalida.");}
            catch(Exception ex){protectedFile=true;files.Protect(path);options=WorldOptions.Defaults();Log.Error("world settings protected",ex);}
            pool.Add(menu);Visuals.Register(menu,"reference-settings");last=Game.GameTime;realClock=options.RefreshSeconds*1000;dynamicClock=0;
        }
        string Digest(){if(!File.Exists(path))return null;using(var sha=System.Security.Cryptography.SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));}
        bool Safe=>!Game.IsMissionActive && !Game.IsPaused && !Game.Player.Character.IsDead && gate.Owner==null && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        void Row(string title,string desc,Action action){var item=new NativeItem(title,desc);item.Activated+=(s,e)=>{try{action();}catch(Exception ex){Log.Error("world menu",ex);Notify.Show("Configuracao nao alterada; confira o log.");}};menu.Add(item);}
        void Change(Action<WorldOptions> action)
        {
            var next=WorldRules.Copy(options);action(next);
            if(!WorldRules.Configure(ref options,next,n=>{if(protectedFile || Digest()!=signature)throw new IOException("World/Settings.json protegido/alterado externamente; recarregue scripts.");files.Write(path,n);signature=Digest();}))throw new InvalidDataException("Opcao de mundo invalida.");
            version++;cancel?.Cancel();dynamicClock=0;realClock=options.RefreshSeconds*1000;clock=1000;
            if(options.Mode=="Off")ReleaseWeather();
            if(options.Mode=="Manual" && Safe)ApplyWeather(options.Weather);
            OpenMenu();
        }
        public void OpenMenu()
        {
            pool.HideAll();menu.Clear();
            Row("Clima: "+options.Mode,"Off preserva o controle nativo. Automaticos sao opcionais e pausam durante missoes/atividades. Preferencias globais, nao por personagem.",()=>{});
            foreach(string mode in new[]{"Off","Manual","Dynamic","Real"}){string value=mode;Row("Modo "+value,value=="Real"?"Consulta Open-Meteo por HTTPS para coordenadas configuradas, a cada5min ativos; sem usar localizacao automatica.":"Manual usa a escolha abaixo; Dynamic varia gradualmente a cada8min ativos.",()=>Change(n=>n.Mode=value));}
            foreach(string weather in WorldRules.Weathers){string value=weather;Row("Aplicar: "+value,"Seleciona clima manual e grava preferencia. Nao altera durante atividade/missao; aguarda ficar livre.",()=>Change(n=>{n.Weather=value;n.Mode="Manual";}));}
            Row("Relogio real: "+(options.SyncClock?"ON":"OFF"),"Usa hora local do Windows; independente do clima. Desligado nos saves antigos.",()=>Change(n=>n.SyncClock=!n.SyncClock));
            Row("Ajustar hora manual","Digite HH:mm (00:00..23:59). Desliga sincronizacao; aplica somente fora de missao/atividade.",()=>{if(!Safe){Notify.Show("Encerre a atividade antes de alterar a hora.");return;}var value=Game.GetUserInput(WindowTitle.EnterMessage60,"12:00",5);if(!DateTime.TryParseExact(value,"HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out var time)){Notify.Show("Use HH:mm.");return;}Change(n=>n.SyncClock=false);ClockChanged?.Invoke();Function.Call(Hash.SET_CLOCK_TIME,time.Hour,time.Minute,0);});
            Row("Latitude: "+options.Latitude.ToString(CultureInfo.InvariantCulture),"Graus -90..90. Use ponto decimal.",()=>Number(true));
            Row("Longitude: "+options.Longitude.ToString(CultureInfo.InvariantCulture),"Graus -180..180. Oeste e negativo; use ponto decimal.",()=>Number(false));
            Row("Local: "+options.Location,"Nome exibido no resultado; nao geocodifica nem descobre sua posicao.",()=>{string name=Game.GetUserInput(WindowTitle.EnterMessage60,options.Location,60);if(!string.IsNullOrWhiteSpace(name))Change(n=>n.Location=name.Trim());});
            Row("Temperatura: "+(options.Fahrenheit?"F":"C"),"Altera apenas exibicao.",()=>Change(n=>n.Fahrenheit=!n.Fahrenheit));
            Row("Consulta: "+status,reading==null?"Falha/offline mantem clima atual e tenta novamente no proximo intervalo.":options.Location+": "+WorldRules.Temperature(reading.Celsius,options.Fahrenheit).ToString("0.#",CultureInfo.InvariantCulture)+(options.Fahrenheit?" F / ":" C / ")+reading.Weather,()=>{});
            Row("Fechar","Voltar ao jogo.",()=>menu.Visible=false);menu.Visible=true;
        }
        void Number(bool latitude){string value=Game.GetUserInput(WindowTitle.EnterMessage60,(latitude?options.Latitude:options.Longitude).ToString(CultureInfo.InvariantCulture),24);if(!double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out var n) || !WorldRules.Finite(n) || Math.Abs(n)>(latitude?90:180)){Notify.Show("Coordenada invalida.");return;}Change(o=>{if(latitude)o.Latitude=n;else o.Longitude=n;});}
        void ApplyWeather(string weather)
        {if(!Safe)return;World.Weather=(Weather)Enum.Parse(typeof(Weather),weather);lastWeather=weather;}
        void ReleaseWeather()
        {if(lastWeather!=null && World.Weather.ToString()==lastWeather){Function.Call(Hash.CLEAR_OVERRIDE_WEATHER);Function.Call(Hash.CLEAR_WEATHER_TYPE_PERSIST);}lastWeather=null;}
        async Task<WeatherReading> Fetch(double latitude,double longitude,CancellationToken token)
        {
            using(var response=await client.GetAsync(WorldRules.Url(latitude,longitude),HttpCompletionOption.ResponseHeadersRead,token).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();if(response.Content.Headers.ContentLength>65536)throw new InvalidDataException("Resposta meteorologica grande.");
                using(var stream=await response.Content.ReadAsStreamAsync().ConfigureAwait(false))using(var buffer=new MemoryStream())
                {var chunk=new byte[4096];int count;while((count=await stream.ReadAsync(chunk,0,chunk.Length,token).ConfigureAwait(false))>0){if(buffer.Length+count>65536)throw new InvalidDataException("Resposta meteorologica grande.");buffer.Write(chunk,0,count);}return WorldRules.Parse(System.Text.Encoding.UTF8.GetString(buffer.ToArray()));}
            }
        }
        public void Tick()
        {
            int now=Game.GameTime;int dt=PassengerRules.Delta(now,last,Safe);last=now;if(!Safe)return;
            if(request!=null && request.IsCompleted)
            {
                var done=request;request=null;
                try{var result=done.GetAwaiter().GetResult();if(requestVersion==version && options.Mode=="Real"){reading=result;status="Atualizado";ApplyWeather(result.Weather);Notify.Show(options.Location+": "+result.Weather+" / "+WorldRules.Temperature(result.Celsius,options.Fahrenheit).ToString("0.#")+(options.Fahrenheit?" F":" C"));}}
                catch(Exception ex){if(requestVersion==version && options.Mode=="Real"){status="Indisponivel; clima mantido";Log.Error("weather request",ex);}}
                cancel?.Dispose();cancel=null;
            }
            clock=Math.Min(1000,clock+dt);if(options.SyncClock && clock>=1000 && Function.Call<bool>(Hash.IS_PLAYER_CONTROL_ON,Game.Player) && !Function.Call<bool>(Hash.IS_SCREEN_FADED_OUT) && !Function.Call<bool>(Hash.IS_SCREEN_FADING_OUT) && !Function.Call<bool>(Hash.IS_SCREEN_FADING_IN)){var local=DateTime.Now;ClockChanged?.Invoke();Function.Call(Hash.SET_CLOCK_TIME,local.Hour,local.Minute,local.Second);clock=0;}
            if(options.Mode=="Manual"){if(lastWeather!=options.Weather)ApplyWeather(options.Weather);}
            else if(options.Mode=="Dynamic"){dynamicClock+=dt;if(dynamicClock>=options.DynamicSeconds*1000){ApplyWeather(WorldRules.Next(World.Weather.ToString(),random.Next(100)));dynamicClock=0;}}
            else if(options.Mode=="Real")
            {realClock=Math.Min(options.RefreshSeconds*1000,realClock+dt);if(request==null && realClock>=options.RefreshSeconds*1000){cancel=new CancellationTokenSource(TimeSpan.FromSeconds(8));requestVersion=version;request=Fetch(options.Latitude,options.Longitude,cancel.Token);status="Consultando";realClock=0;}}
        }
        public void OnKey(KeyEventArgs e){}
        public void Save(){}
        public void Reload(){menu.Visible=false;last=Game.GameTime;}
        public void Dispose(){menu.Visible=false;version++;cancel?.Cancel();if(request!=null)request.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);cancel?.Dispose();client.Dispose();ReleaseWeather();}
    }
}
