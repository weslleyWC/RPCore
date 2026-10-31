using System;
using System.Collections.Generic;
namespace RPCore
{
    // One owner per UI pool. Closing a page must discard its pending actions, not navigate back.
    public sealed class ExclusiveMenuGroup<T> where T:class
    {
        sealed class Entry{public T View;public Action Close;public Func<bool> Visible;}
        readonly List<Entry> entries=new List<Entry>();bool changing;
        public T Owner{get;private set;}
        public void Register(T view,Action close,Func<bool> visible)
        {
            if(view==null || close==null || visible==null)throw new ArgumentNullException();
            if(entries.Exists(e=>ReferenceEquals(e.View,view)))throw new InvalidOperationException("Menu already registered.");
            entries.Add(new Entry{View=view,Close=close,Visible=visible});
        }
        public bool Owns(T view)=>view!=null && ReferenceEquals(Owner,view);
        public bool Activate(T view)
        {
            if(changing)return false;
            if(!entries.Exists(e=>ReferenceEquals(e.View,view)))throw new InvalidOperationException("Unknown menu.");
            changing=true;Owner=null;
            try{foreach(var entry in entries)entry.Close();Owner=view;return true;}
            finally{changing=false;}
        }
        public void Release(T view){if(Owns(view))Owner=null;}
        public void CloseAll()
        {
            if(changing)return;changing=true;Owner=null;
            try{foreach(var entry in entries)entry.Close();}finally{changing=false;}
        }
        public int Reconcile()
        {
            if(changing)return 0;changing=true;int closed=0;
            try{foreach(var entry in entries)if(entry.Visible() && !Owns(entry.View)){entry.Close();closed++;}return closed;}
            finally{changing=false;}
        }
    }
}
