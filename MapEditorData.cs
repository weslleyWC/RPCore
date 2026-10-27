using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class EditorOptions : ExtensibleData
    {
        public int MaxMaps=12,MaxObjects=64,MaxPoints=32;public float LoadRadius=120,UnloadRadius=160;
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)]public List<string> Models=new List<string>();
        [JsonIgnore]public bool Valid=>MaxMaps>=1 && MaxMaps<=24 && MaxObjects>=1 && MaxObjects<=128 && MaxPoints>=1 && MaxPoints<=64 && GangPresenceRules.Finite(LoadRadius) && LoadRadius>=40 && LoadRadius<=200 && GangPresenceRules.Finite(UnloadRadius) && UnloadRadius>=LoadRadius+20 && UnloadRadius<=300 && Models!=null && Models.Count>=1 && Models.Count<=64 && Models.All(GangPresenceRules.ModelName);
        public static EditorOptions Defaults(){using(var s=typeof(EditorOptions).Assembly.GetManifestResourceStream("RPCore.JobDefaults.MapEditor.json"))using(var r=new System.IO.StreamReader(s))return JsonConvert.DeserializeObject<EditorOptions>(r.ReadToEnd());}
    }
    public sealed class EditorPose : ExtensibleData
    {
        public float X,Y,Z,RX,RY,RZ;
        [JsonIgnore]public bool Valid=>new[]{X,Y,Z,RX,RY,RZ}.All(GangPresenceRules.Finite) && Math.Abs(X)<15000 && Math.Abs(Y)<15000 && Z> -100 && Z<2000 && new[]{RX,RY,RZ}.All(v=>Math.Abs(v)<=3600);
    }
    public sealed class EditorObject : ExtensibleData
    {
        public string Id,Model;public EditorPose Pose;public bool Collision;
        [JsonIgnore]public bool Valid=>EditorRules.Id(Id) && GangPresenceRules.ModelName(Model) && Pose?.Valid==true;
    }
    public enum EditorPointKind { Marker,Grocery,Fuel,Workshop }
    public sealed class EditorPoint : ExtensibleData
    {
        public string Id,Name;public EditorPose Pose;public EditorPointKind Kind;public int Sprite=1,Color=3;public bool ShortRange=true,Legend=true;
        [JsonIgnore]public bool Valid=>EditorRules.Id(Id) && EditorRules.Label(Name) && Pose?.Valid==true && Enum.IsDefined(typeof(EditorPointKind),Kind) && Sprite>=0 && Sprite<=1000 && Color>=0 && Color<=85;
    }
    public sealed class EditorMap : ExtensibleData
    {
        public int Format=1;public string Id,Name;public long Revision;public List<EditorObject> Objects=new List<EditorObject>();public List<EditorPoint> Points=new List<EditorPoint>();
        [JsonIgnore]public bool Valid=>Format==1 && EditorRules.Id(Id) && EditorRules.Label(Name) && Revision>=0 && Objects!=null && Objects.Count<=128 && Objects.All(o=>o?.Valid==true) && Points!=null && Points.Count<=64 && Points.All(p=>p?.Valid==true) && Objects.Select(o=>o.Id).Concat(Points.Select(p=>p.Id)).Distinct().Count()==Objects.Count+Points.Count;
    }
    public sealed class MapEditorData : ExtensibleData
    {
        public int Format=1;public bool AutoLoad;public string Selected;public List<EditorMap> Maps=new List<EditorMap>();
        [JsonIgnore]public bool Valid=>Format==1 && Maps!=null && Maps.Count<=24 && Maps.All(m=>m?.Valid==true) && Maps.Select(m=>m.Id).Distinct().Count()==Maps.Count && (Selected==null || Maps.Any(m=>m.Id==Selected)) && (!AutoLoad || Selected!=null);
    }
    public static class EditorRules
    {
        public static bool Id(string id)=>id!=null && Regex.IsMatch(id,@"\A[a-zA-Z0-9_-]{1,80}\z");
        public static bool Label(string s)=>!string.IsNullOrWhiteSpace(s) && s.Length<=60 && !s.Any(char.IsControl);
        public static bool ModelNameSafe(string s)=>GangPresenceRules.ModelName(s);
        public static bool FileName(string s)=>s!=null && Regex.IsMatch(s,@"\A[a-zA-Z0-9_-]{1,40}\z");
        public static T Copy<T>(T value)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        public static float Angle(float value)=>(value%360+540)%360-180;
        public static bool Translate(EditorPose p,float x,float y,float z,float pitch=0,float roll=0,float yaw=0)
        {if(p?.Valid!=true || new[]{x,y,z,pitch,roll,yaw}.Any(n=>!GangPresenceRules.Finite(n) || Math.Abs(n)>20))return false;var n=Copy(p);n.X+=x;n.Y+=y;n.Z+=z;n.RX=Angle(n.RX+pitch);n.RY=Angle(n.RY+roll);n.RZ=Angle(n.RZ+yaw);if(!n.Valid)return false;p.X=n.X;p.Y=n.Y;p.Z=n.Z;p.RX=n.RX;p.RY=n.RY;p.RZ=n.RZ;return true;}
        public static EditorMap ImportCopy(EditorMap map)
        {if(map?.Valid!=true)return null;var n=Copy(map);n.Id=Guid.NewGuid().ToString("N");n.Revision=0;foreach(var o in n.Objects)o.Id=Guid.NewGuid().ToString("N");foreach(var p in n.Points)p.Id=Guid.NewGuid().ToString("N");return n;}
        public static bool CanAddPoint(EditorMap map,EditorPoint point,int limit)=>map?.Valid==true && point?.Valid==true && map.Points.Count<limit && !map.Objects.Any(o=>o.Id==point.Id) && !map.Points.Any(p=>p.Id==point.Id || p.Kind==point.Kind && Math.Sqrt(Math.Pow(p.Pose.X-point.Pose.X,2)+Math.Pow(p.Pose.Y-point.Pose.Y,2)+Math.Pow(p.Pose.Z-point.Pose.Z,2))<5);
    }
    public static class EditorTransactions
    {
        public static bool Apply(ref MapEditorData data,Func<MapEditorData,bool> mutate,Action<MapEditorData> save)
        {if(data?.Valid!=true || mutate==null || save==null)return false;var n=EditorRules.Copy(data);if(!mutate(n) || !n.Valid)return false;save(n);data=n;return true;}
        public static bool Put(ref MapEditorData data,EditorMap draft,long expected,EditorOptions options,Action<MapEditorData> save)
        {if(draft?.Valid!=true || options?.Valid!=true || draft.Objects.Count>options.MaxObjects || draft.Points.Count>options.MaxPoints)return false;return Apply(ref data,b=>{var old=b.Maps.FirstOrDefault(m=>m.Id==draft.Id);if(old==null?(expected!=-1 || b.Maps.Count>=options.MaxMaps):old.Revision!=expected || old.Revision==long.MaxValue)return false;var n=EditorRules.Copy(draft);n.Revision=old==null?1:old.Revision+1;if(old!=null)b.Maps.Remove(old);b.Maps.Add(n);b.Selected=n.Id;return true;},save);}
        public static bool Delete(ref MapEditorData data,string id,long expected,Action<MapEditorData> save)=>Apply(ref data,b=>{var m=b.Maps.FirstOrDefault(v=>v.Id==id);if(m==null || m.Revision!=expected)return false;b.Maps.Remove(m);if(b.Selected==id){b.Selected=null;b.AutoLoad=false;}return true;},save);
    }
}
