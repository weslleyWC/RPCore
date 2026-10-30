using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;
namespace RPCore
{
    public class RadioStation { public string Name,Url; }
    public sealed class RadioPreferences : ExtensibleData
    {
        public int Volume=50;
        [JsonIgnore] public bool Valid=>Volume>=0 && Volume<=100;
    }
    public static class RadioRules
    {
        public static List<RadioStation> ReadStations(string path)
        {
            var result=new List<RadioStation>();if(!File.Exists(path))return result;
            var doc=new XmlDocument{XmlResolver=null};
            using(var reader=XmlReader.Create(path,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=1048576}))doc.Load(reader);
            foreach(XmlNode node in doc.SelectNodes("/*[local-name()='RadioSettings']/*[local-name()='Stations']/*[local-name()='Station' or local-name()='RadioStation']")){
                string url=node.Attributes?["url"]?.Value;string name=node.Attributes?["name"]?.Value;
                foreach(XmlNode child in node.ChildNodes)if(child.LocalName=="Url")url=child.InnerText;else if(child.LocalName=="Name")name=child.InnerText;
                if(url==null && !node.ChildNodes.Cast<XmlNode>().Any(n=>n.NodeType==XmlNodeType.Element))url=node.InnerText;
                if(Uri.TryCreate(url?.Trim(),UriKind.Absolute,out var uri) && (uri.Scheme=="http" || uri.Scheme=="https"))result.Add(new RadioStation{Name=string.IsNullOrWhiteSpace(name)?uri.Host:name.Trim(),Url=uri.AbsoluteUri});
            }
            return result;
        }
    }
    public interface IRadioPlayback : IDisposable
    {
        int State{get;}bool HasError{get;}int Volume{set;}
        void Play(string url);void Pause();void Resume();void Stop();
    }
    public enum RadioStatus { Stopped,Connecting,Playing,Paused,Failed }
    public sealed class RadioSession
    {
        IRadioPlayback playback;int connecting;bool credited,automaticPause;
        public RadioStatus Status{get;private set;}
        public string Url{get;private set;}
        public Action<Exception> Error;
        public bool Start(string url,int volume,Func<IRadioPlayback> create)
        {
            Stop();connecting=0;credited=false;automaticPause=false;Url=url;
            try{playback=create();playback.Volume=volume;playback.Play(url);Status=RadioStatus.Connecting;return true;}
            catch(Exception ex){Fail(ex);return false;}
        }
        public void Stop()
        {
            var old=playback;playback=null;Status=RadioStatus.Stopped;automaticPause=false;
            if(old==null)return;
            try{old.Stop();}catch(Exception ex){Error?.Invoke(ex);}finally{try{old.Dispose();}catch(Exception ex){Error?.Invoke(ex);}}
        }
        void Fail(Exception ex){Stop();Status=RadioStatus.Failed;if(ex!=null)Error?.Invoke(ex);}
        public void Volume(int value){if(playback==null)return;try{playback.Volume=value;}catch(Exception ex){Fail(ex);}}
        public void Toggle()
        {
            if(playback==null)return;
            try{if(Status==RadioStatus.Paused){playback.Resume();Status=RadioStatus.Connecting;connecting=0;}else{playback.Pause();Status=RadioStatus.Paused;}automaticPause=false;}
            catch(Exception ex){Fail(ex);}
        }
        public bool Tick(int milliseconds,bool paused)
        {
            if(playback==null)return false;
            try{
                if(paused){if(Status!=RadioStatus.Paused){playback.Pause();Status=RadioStatus.Paused;automaticPause=true;}return false;}
                if(automaticPause){automaticPause=false;playback.Resume();Status=RadioStatus.Connecting;connecting=0;}
                if(Status==RadioStatus.Paused)return false;
                if(playback.HasError){Fail(null);return false;}
                int state=playback.State;
                if(state==3){Status=RadioStatus.Playing;connecting=0;if(!credited){credited=true;return true;}return false;}
                if(state==8 || state==1 && credited){Stop();return false;}
                Status=RadioStatus.Connecting;if(milliseconds>=0 && milliseconds<=1000)connecting+=milliseconds;
                if(connecting>=30000)Fail(null);
            }catch(Exception ex){Fail(ex);}return false;
        }
    }
}
