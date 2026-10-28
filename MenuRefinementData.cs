using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public class MapPin : ExtensibleData { public string Name; public float X,Y,Z; }
    public class GpsDestination
    {
        public string Name,Kind;public float X,Y,Z;
        public bool Valid=>!string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Kind) && MapPinRules.Coordinates(X,Y,Z);
    }
    public static class MapPinRules
    {
        public static bool Coordinates(float x,float y,float z)=>new[]{x,y,z}.All(n=>!float.IsNaN(n) && !float.IsInfinity(n));
        public static bool Valid(List<MapPin> pins)=>pins!=null && pins.Count<=100 && pins.All(p=>p!=null && !string.IsNullOrWhiteSpace(p.Name) && Coordinates(p.X,p.Y,p.Z));
        static List<MapPin> Copy(List<MapPin> pins)=>JsonConvert.DeserializeObject<List<MapPin>>(JsonConvert.SerializeObject(pins));
        static bool Match(List<MapPin> pins,int index,MapPin expected)=>Valid(pins) && index>=0 && index<pins.Count && expected!=null && JsonConvert.SerializeObject(pins[index])==JsonConvert.SerializeObject(expected);
        public static bool Add(ref List<MapPin> pins,string name,float x,float y,float z,Action<List<MapPin>> save)
        {
            if(!Valid(pins) || pins.Count>=100 || string.IsNullOrWhiteSpace(name) || name.Trim().Length>32 || !Coordinates(x,y,z) || save==null)return false;
            var next=Copy(pins);next.Add(new MapPin{Name=name.Trim(),X=x,Y=y,Z=z});save(next);pins=next;return true;
        }
        public static bool Rename(ref List<MapPin> pins,int index,MapPin expected,string name,Action<List<MapPin>> save)
        {
            if(!Match(pins,index,expected) || string.IsNullOrWhiteSpace(name) || name.Trim().Length>32 || save==null)return false;
            var next=Copy(pins);next[index].Name=name.Trim();save(next);pins=next;return true;
        }
        public static bool Delete(ref List<MapPin> pins,int index,MapPin expected,Action<List<MapPin>> save)
        {
            if(!Match(pins,index,expected) || save==null)return false;var next=Copy(pins);next.RemoveAt(index);save(next);pins=next;return true;
        }
        public static GpsDestination Nearest(IEnumerable<GpsDestination> entries,string kind,float x,float y,float z)
        {
            if(entries==null || !Coordinates(x,y,z))return null;
            return entries.Where(p=>p?.Valid==true && p.Kind==kind).OrderBy(p=>Distance(p,x,y,z)).ThenBy(p=>p.Name,StringComparer.Ordinal).FirstOrDefault();
        }
        public static double Distance(GpsDestination p,float x,float y,float z){double dx=(double)p.X-x,dy=(double)p.Y-y,dz=(double)p.Z-z;return Math.Sqrt(dx*dx+dy*dy+dz*dz);}
    }
    public enum RadialPage { Root,Inventory,Drinks,Food,Health,Weapons,Tools,Gps,Vehicle,VehicleCore,VehicleLights,VehicleDoors,VehicleWindows }
    public static class RadialMenuRules
    {
        public static readonly PointF[] Centers={new PointF(966,360),new PointF(1090,420),new PointF(1136,545),new PointF(1086,675),new PointF(952,720),new PointF(825,665),new PointF(780,525),new PointF(836,410)};
        public static float Scale(float width,float insetX,float insetY,float requested)=>Math.Max(.1f,Math.Min(Math.Min(1.2f,requested),Math.Min((width-2*insetX)/640,(1080-2*insetY)/760)));
        public static int Hit(PointF p)
        {
            double x=p.X-960,y=p.Y-540,r=x*x+y*y;if(double.IsNaN(r) || r<65*65 || r>245*245)return -1;
            double angle=Math.Atan2(y,x),best=double.MaxValue;int selected=-1;
            for(int i=0;i<8;i++){double target=Math.Atan2(Centers[i].Y-540,Centers[i].X-960),delta=Math.Abs(Math.Atan2(Math.Sin(angle-target),Math.Cos(angle-target)));if(delta<best){best=delta;selected=i;}}
            return selected;
        }
        public static List<string> Items(RadialPage page,IDictionary<string,ItemDef> defs,IDictionary<string,int> counts,IEnumerable<string> layout)
        {
            if(defs==null || counts==null)return new List<string>();
            return (layout??counts.Keys).Where(id=>id!=null && counts.TryGetValue(id,out int n) && n>0 && defs.ContainsKey(id)).Distinct(StringComparer.Ordinal).Where(id=>{
                var i=defs[id];if(i.Weapon!=null)return page==RadialPage.Weapons;
                if(i.Id.StartsWith("ammo_",StringComparison.Ordinal))return false;
                if(i.Health>0)return page==RadialPage.Health;
                if(i.Action=="phone")return page==RadialPage.Tools;
                if(i.Thirst>i.Hunger)return page==RadialPage.Drinks;
                return i.Hunger>0 && page==RadialPage.Food;
            }).ToList();
        }
        public static int Pages(int count)=>Math.Max(1,(Math.Max(0,count)+5)/6);
    }
    public sealed class MenuEmote
    {
        public string Name,Dictionary,Clip;
        public MenuEmote(string name,string dictionary,string clip){Name=name;Dictionary=dictionary;Clip=clip;}
    }
    public static class ReferenceEmotes
    {
        public static readonly MenuEmote[] All={
            new MenuEmote("Maos para cima","missminuteman_1ig_2","handsup_base"),
            new MenuEmote("Maos para cima / ajoelhado","random@arrests","kneeling_arrest_idle"),
            new MenuEmote("Acenar","gestures@m@standing@casual","gesture_hello"),
            new MenuEmote("Aplaudir","anim@mp_player_intcelebrationmale@slow_clap","slow_clap")};
    }
}

namespace RPCore
{
    public static class MenuFileRules
    {
        public static void Write(string path,Func<string,bool> write)
        {
            if(string.IsNullOrWhiteSpace(path) || write==null)throw new ArgumentException("Setting path/writer required.");
            string temp=path+".tmp."+Guid.NewGuid().ToString("N");
            try
            {
                if(System.IO.File.Exists(path))System.IO.File.Copy(path,temp);else System.IO.File.WriteAllText(temp,"");
                if(!write(temp))throw new System.IO.IOException("Setting writer rejected the update.");
                if(System.IO.File.Exists(path))System.IO.File.Replace(temp,path,path+".bak");else System.IO.File.Move(temp,path);
            }
            finally{if(System.IO.File.Exists(temp))System.IO.File.Delete(temp);}
        }
        public static float[] Choices(float current,float step)=>Enumerable.Range(0,6).Select(i=>i*step).Concat(new[]{current}).Where(v=>!float.IsNaN(v) && !float.IsInfinity(v) && v>=0).Distinct().OrderBy(v=>v).ToArray();
    }
}
