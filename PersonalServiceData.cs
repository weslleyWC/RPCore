using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RPCore
{
    // An activation is tied to the page and character that displayed it, never to a later menu.
    public sealed class ServiceActionQueue
    {
        Action pending;
        int revision, actor, model, queued;
        string profile;
        bool held;
        public void NewPage() { revision++; pending=null; }
        public void Release(bool pressed) { if(!pressed)held=false; }
        public void Hold() { held=true; }
        public bool Enqueue(Action action,int ped,int pedModel,string owner,int now)
        {
            if(held || pending!=null || action==null || ped==0 || pedModel==0 || string.IsNullOrEmpty(owner))return false;
            pending=action;actor=ped;model=pedModel;profile=owner;queued=now;held=true;return true;
        }
        public bool Run(bool visible,bool allowed,int ped,int pedModel,string owner,int now)
        {
            var action=pending;pending=null;
            if(action==null || !visible || !allowed || actor!=ped || model!=pedModel || profile!=owner || unchecked((uint)(now-queued))>1000)return false;
            action();return true;
        }
        public int Revision=>revision;
    }
    public sealed class ClothingOptions : ExtensibleData
    {
        public List<JobPoint> Shops=new List<JobPoint>();
        [JsonIgnore] public bool Valid=>Shops!=null && Shops.Count>0 && Shops.Count<=100 && Shops.TrueForAll(p=>p!=null && p.Valid);
    }
    public static class PersonalServiceRules
    {
        public static bool CanSnapshot(string activity)=>activity!="wardrobe" && activity!="barber" && activity!="grooming";
        public static bool Matches(WardrobeOutfit current,WardrobeOutfit expected)=>current!=null && expected!=null && current.Valid && expected.Valid && JsonConvert.SerializeObject(current)==JsonConvert.SerializeObject(expected);
        // Native overlay uses 255 for none, while the variation cycling helper uses -1.
        public static int OverlayStyle(int current,int delta,int count)
        {
            if(count<0 || count>255 || delta!=1 && delta!=-1)throw new ArgumentException();
            if(count==0)return 255;
            int value=WardrobeRules.Cycle(current==255?-1:current,delta,count,true);
            return value<0?255:value;
        }
        public static bool SameClothes(WardrobeOutfit a,WardrobeOutfit b)
        {
            if(a==null || b==null || !a.Valid || !b.Valid || a.Model!=b.Model)return false;
            foreach(var p in a.Components){var q=b.Components.Find(v=>v.Slot==p.Slot);if(q==null || q.Drawable!=p.Drawable || q.Texture!=p.Texture || q.Palette!=p.Palette)return false;}
            foreach(var p in a.Props){var q=b.Props.Find(v=>v.Slot==p.Slot);if(q==null || q.Drawable!=p.Drawable || q.Texture!=p.Texture)return false;}
            return true;
        }
        public static ClothingOptions ClothingDefaults()
        {
            return new ClothingOptions{Shops=new List<JobPoint>{
                new JobPoint{Name="Binco / Textile City",X=427.162f,Y=-800.7566f,Z=29.49112f},
                new JobPoint{Name="Binco / Vespucci",X=-827.7328f,Y=-1075.052f,Z=11.32811f},
                new JobPoint{Name="Suburban / Del Perro",X=-1189.267f,Y=-769.175f,Z=17.32458f},
                new JobPoint{Name="Suburban / Harmony",X=617.4923f,Y=2765.14f,Z=42.08815f},
                new JobPoint{Name="Suburban / Hawick",X=122.1724f,Y=-224.2614f,Z=54.55782f},
                new JobPoint{Name="Suburban / Chumash",X=-3174.153f,Y=1043.574f,Z=20.8632f},
                new JobPoint{Name="Ponsonbys / Rockford Hills",X=-717.5438f,Y=-148.5343f,Z=37.41512f},
                new JobPoint{Name="Ponsonbys / Burton",X=-161.3419f,Y=-311.9883f,Z=39.73326f},
                new JobPoint{Name="Ponsonbys / Morningwood",X=-1448.8f,Y=-229.0768f,Z=49.80111f},
                new JobPoint{Name="Discount / Strawberry",X=73.19008f,Y=-1398.669f,Z=29.37784f},
                new JobPoint{Name="Discount / Grapeseed",X=1694.687f,Y=4828.967f,Z=42.06306f},
                new JobPoint{Name="Discount / Route 68",X=-1105.882f,Y=2707.643f,Z=19.10741f},
                new JobPoint{Name="Discount / Grand Senora",X=1191.001f,Y=2710.907f,Z=38.22207f},
                new JobPoint{Name="Discount / Paleto Bay",X=9.976532f,Y=6515.144f,Z=31.87786f}
            }};
        }
    }
}
