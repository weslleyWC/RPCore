using System;
using System.Runtime.InteropServices;
namespace RPCore
{
    public sealed class RadioPlayback : IRadioPlayback
    {
        dynamic player;
        public RadioPlayback(){var type=Type.GetTypeFromProgID("WMPlayer.OCX");if(type==null)throw new NotSupportedException("Windows Media Player COM unavailable.");player=Activator.CreateInstance(type);}
        public int State=>(int)player.playState;
        public bool HasError=>(int)player.error.errorCount>0;
        public int Volume{set{player.settings.volume=value;}}
        public void Play(string url){player.URL=url;player.controls.play();}
        public void Pause()=>player.controls.pause();
        public void Resume()=>player.controls.play();
        public void Stop(){if(player!=null)player.controls.stop();}
        public void Dispose(){object old=player;player=null;if(old==null)return;try{((dynamic)old).close();}finally{if(Marshal.IsComObject(old))Marshal.FinalReleaseComObject(old);}}
    }
}
