using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
namespace RPCore
{
    public class StorageLocation
    {
        public string Id,Name;
        public float X,Y,Z,Capacity=200;
        [Newtonsoft.Json.JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=150 && Finite(X) && Finite(Y) && Finite(Z) && Finite(Capacity) && Capacity>0;
        static bool Finite(float value)=>!float.IsNaN(value) && !float.IsInfinity(value);
    }
    public static class StorageRules
    {
        // Keep the established model + trimmed, case-sensitive plate key. Never rename existing containers.
        public static string TrunkKey(int model,string plate)
        {plate=plate?.Trim();return model==0 || string.IsNullOrEmpty(plate) || plate.Length>32 || plate.Any(char.IsControl)?null:"trunk:"+model+":"+plate;}
        public static IEnumerable<int> Quantities(int available,int fitting)
            =>new[]{1,Math.Min(5,available),available,Math.Min(available,fitting)}.Where(n=>n>0 && n<=available).Distinct();
    }
    public sealed class StorageIdentity
    {
        readonly string profile,key;readonly int actor,model,vehicle;
        public StorageIdentity(string profile,int actor,int model,int vehicle=0,string key=null)
        {this.profile=profile;this.actor=actor;this.model=model;this.vehicle=vehicle;this.key=key;}
        public bool Matches(string owner,int ped,int pedModel,int car=0,string carKey=null)
            =>actor!=0 && model!=0 && !string.IsNullOrEmpty(profile) && profile==owner && actor==ped && model==pedModel && vehicle==car && key==carKey;
    }
    // A cross-panel gesture is one transaction and is invalidated if its source changes before release.
    public sealed class StorageMouseTransfer
    {
        public bool Active{get;private set;}
        public bool Dragging{get;private set;}
        public bool FromStorage{get;private set;}
        public int Source{get;private set;}
        public string Item{get;private set;}
        int count;PointF start;
        public void Begin(bool storage,int index,string item,int available,PointF point)
        {Cancel();if(index<0 || string.IsNullOrEmpty(item) || available<=0)return;Active=true;FromStorage=storage;Source=index;Item=item;count=available;start=point;}
        public void Move(PointF point){if(Active && Math.Abs(point.X-start.X)+Math.Abs(point.Y-start.Y)>=6)Dragging=true;}
        public bool Release(bool targetStorage,bool validTarget,string currentItem,int currentCount,int fitting,bool stack,bool fit,Func<string,bool,int,bool> transfer)
        {
            try{
                if(!Active || !Dragging || !validTarget || FromStorage==targetStorage || currentItem!=Item || currentCount!=count || transfer==null)return false;
                int quantity=fit?Math.Min(count,fitting):stack?count:1;
                return quantity>0 && transfer(Item,!FromStorage,quantity);
            }finally{Cancel();}
        }
        public void Cancel(){Active=false;Dragging=false;Item=null;count=0;Source=-1;}
    }
}
