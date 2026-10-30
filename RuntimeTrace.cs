using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using Newtonsoft.Json;

namespace RPCore
{
    public sealed class RuntimeTrace : IDisposable
    {
        const int Capacity=8192,Magic=0x52505452;
        sealed class Header { public string Version;public string[] Systems; }
        public sealed class Snapshot
        {
            public string Version,Stage;
            public long Sequence,UtcTicks;
            public bool AbortedHookReached;
        }
        readonly MemoryMappedFile file;readonly MemoryMappedViewAccessor view;
        long sequence,nextClock;bool disposed;
        public Snapshot Previous{get;}
        public RuntimeTrace(string directory,string version,string[] systems)
        {
            Directory.CreateDirectory(directory);string path=Path.Combine(directory,"RuntimeState.bin");
            Previous=ReadPrevious(path);
            if(Previous!=null)new JsonFiles().Write(Path.Combine(directory,"RuntimeLastSession.json"),Previous);
            byte[] names=Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Header{Version=version,Systems=systems}));
            if(names.Length>Capacity-128)throw new InvalidDataException("Runtime trace header too large.");
            var stream=new FileStream(path,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.ReadWrite);
            try{stream.SetLength(Capacity);file=MemoryMappedFile.CreateFromFile(stream,null,Capacity,MemoryMappedFileAccess.ReadWrite,HandleInheritability.None,false);}
            catch{stream.Dispose();throw;}
            try{view=file.CreateViewAccessor(0,Capacity,MemoryMappedFileAccess.ReadWrite);view.Write(0,Magic);view.Write(4,1);view.Write(8,0);view.Write(12,0);view.Write(16,0L);view.Write(24,DateTime.UtcNow.Ticks);view.Write(32,names.Length);view.WriteArray(128,names,0,names.Length);}
            catch{view?.Dispose();file.Dispose();throw;}
        }
        static Snapshot ReadPrevious(string path)
        {
            if(!File.Exists(path) || new FileInfo(path).Length!=Capacity)return null;
            try{
                byte[] bytes=File.ReadAllBytes(path);if(BitConverter.ToInt32(bytes,0)!=Magic || BitConverter.ToInt32(bytes,4)!=1)return null;
                int count=BitConverter.ToInt32(bytes,32);if(count<=0 || count>Capacity-128)return null;
                var header=JsonConvert.DeserializeObject<Header>(Encoding.UTF8.GetString(bytes,128,count));if(header?.Systems==null)return null;
                return new Snapshot{Version=header.Version,Stage=Resolve(BitConverter.ToInt32(bytes,8),header.Systems),Sequence=BitConverter.ToInt64(bytes,16),UtcTicks=BitConverter.ToInt64(bytes,24),AbortedHookReached=BitConverter.ToInt32(bytes,12)==1};
            }catch(Exception){return null;}
        }
        static string Resolve(int stage,string[] systems)
        {
            if(stage==0)return "initializing";if(stage==1)return "frame";if(stage==2)return "wallet-sync";if(stage==3)return "menus";if(stage==4)return "idle";if(stage==5)return "key-dispatch";if(stage==6)return "wallet-save";if(stage==7)return "wallet-load";if(stage==8)return "inventory-input";if(stage==9000)return "OnAborted";
            if(stage>=6001 && stage<=6009)return "character:"+new[]{"begin","request-model","origin-body","pending-checkpoint","wait-actor","apply-body","head-blend","surface","capture-commit"}[stage-6001];
            if(stage==6010)return "character:wallet-save";if(stage==6020)return "character:set-player-model";if(stage==6021)return "character:model-returned";if(stage==6022)return "character:wallet-reload";
            int group=stage/1000,index=stage%1000;
            if(group>=1 && group<=5 && index<systems.Length)return new[]{"","tick:","save:","key:","reload:","dispose:"}[group]+systems[index];
            return "stage:"+stage;
        }
        public void Mark(int stage)
        {
            if(disposed)return;view.Write(8,stage);view.Write(16,++sequence);
        }
        public void Heartbeat()
        {
            if(disposed)return;long now=Stopwatch.GetTimestamp();if(now<nextClock)return;
            nextClock=now+Stopwatch.Frequency;view.Write(24,DateTime.UtcNow.Ticks);
            // No per-frame file writes or synchronous flush: these are fixed mapped-memory slots.
        }
        public void Aborted(){if(disposed)return;view.Write(12,1);Mark(9000);}
        public void Dispose(){if(disposed)return;disposed=true;try{view.Flush();}finally{view.Dispose();file.Dispose();}}
    }
}
