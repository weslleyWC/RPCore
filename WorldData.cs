using System;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace RPCore
{
    public sealed class WorldOptions : ExtensibleData
    {
        public string Mode="Off",Weather="Clear",Location="Los Angeles";public bool SyncClock,Fahrenheit;
        public double Latitude=34.06778,Longitude=-118.24167;public int DynamicSeconds=480,RefreshSeconds=300;
        [JsonIgnore]public bool Valid=>new[]{"Off","Manual","Dynamic","Real"}.Contains(Mode) && WorldRules.Weathers.Contains(Weather) && !string.IsNullOrWhiteSpace(Location) && Location.Length<=60 && !Location.Any(char.IsControl) && WorldRules.Finite(Latitude) && Math.Abs(Latitude)<=90 && WorldRules.Finite(Longitude) && Math.Abs(Longitude)<=180 && DynamicSeconds>=60 && DynamicSeconds<=3600 && RefreshSeconds>=300 && RefreshSeconds<=3600;
        public static WorldOptions Defaults(){using(var s=typeof(WorldOptions).Assembly.GetManifestResourceStream("RPCore.JobDefaults.World.json"))using(var r=new System.IO.StreamReader(s))return JsonConvert.DeserializeObject<WorldOptions>(r.ReadToEnd());}
    }
    public sealed class WeatherReading {public int Code;public double Celsius;public string Weather;}
    public static class WorldRules
    {
        public static readonly string[] Weathers={"ExtraSunny","Clear","Clouds","Overcast","Raining","ThunderStorm","Foggy","Smog","Clearing","Snowlight","Snowing","Blizzard"};
        public static bool Finite(double n)=>!double.IsNaN(n) && !double.IsInfinity(n);
        public static string FromCode(int code)=>code==0?"ExtraSunny":code==1?"Clear":code==2?"Clouds":code==3 || code==48?"Overcast":code==45?"Foggy":new[]{51,53,55,56,57,61,63,65,66,67,80,81,82}.Contains(code)?"Raining":code==71 || code==77?"Snowlight":code==73 || code==85?"Snowing":code==75 || code==86?"Blizzard":new[]{95,96,99}.Contains(code)?"ThunderStorm":null;
        public static WeatherReading Parse(string json)
        {
            if(string.IsNullOrWhiteSpace(json) || json.Length>65536)throw new FormatException("Resposta meteorologica invalida.");
            var current=JObject.Parse(json)["current_weather"] as JObject;
            var code=current?["weathercode"];var temp=current?["temperature"];
            if(code?.Type!=JTokenType.Integer || temp==null || temp.Type!=JTokenType.Float && temp.Type!=JTokenType.Integer)throw new FormatException("Campos meteorologicos ausentes.");
            int n=code.Value<int>();double t=temp.Value<double>();string weather=FromCode(n);if(weather==null || !Finite(t) || t< -100 || t>70)throw new FormatException("Valores meteorologicos invalidos.");return new WeatherReading{Code=n,Celsius=t,Weather=weather};
        }
        public static string Url(double lat,double lon)
        {if(!Finite(lat) || Math.Abs(lat)>90 || !Finite(lon) || Math.Abs(lon)>180)throw new ArgumentException();return "https://api.open-meteo.com/v1/forecast?latitude="+lat.ToString("R",CultureInfo.InvariantCulture)+"&longitude="+lon.ToString("R",CultureInfo.InvariantCulture)+"&current_weather=true";}
        public static string Next(string current,int roll)
        {
            if(roll<0 || roll>=100)throw new ArgumentOutOfRangeException();
            string[] candidates=current=="Raining" || current=="ThunderStorm"?new[]{"Raining","Overcast","Clearing"}:current=="Overcast" || current=="Clouds"?new[]{"Clouds","Clear","Raining"}:new[]{"Clear","ExtraSunny","Clouds"};return candidates[roll<50?0:roll<80?1:2];
        }
        public static double Temperature(double c,bool fahrenheit)=>fahrenheit?c*9/5+32:c;
        public static WorldOptions Copy(WorldOptions v)=>JsonConvert.DeserializeObject<WorldOptions>(JsonConvert.SerializeObject(v));
        public static bool Configure(ref WorldOptions current,WorldOptions next,Action<WorldOptions> save)
        {if(next?.Valid!=true || save==null)return false;var copy=Copy(next);save(copy);current=copy;return true;}
    }
}
