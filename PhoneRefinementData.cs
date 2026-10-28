using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public static class PhoneEdits
    {
        public static bool Valid(PhoneData d)=>d!=null && d.Wallpaper>=1 && d.Wallpaper<=10 && d.Notes!=null && d.Notes.Count<=30 && d.Notes.All(n=>n!=null) && d.History!=null && d.History.Count<=64 && d.History.All(h=>h!=null);
        public static bool Change(ref PhoneData d,Func<PhoneData,bool> change,Action<PhoneData> save)
        {if(!Valid(d) || change==null || save==null)return false;var next=JsonConvert.DeserializeObject<PhoneData>(JsonConvert.SerializeObject(d));if(!change(next) || !Valid(next))return false;save(next);d=next;return true;}
        public static bool Note(ref PhoneData d,int index,string expected,string text,Action<PhoneData> save)
            =>Change(ref d,n=>{if(string.IsNullOrWhiteSpace(text) || text.Length>120)return false;if(index==-1){if(n.Notes.Count>=30)return false;n.Notes.Insert(0,text);return true;}if(index<0 || index>=n.Notes.Count || n.Notes[index]!=expected)return false;n.Notes[index]=text;return true;},save);
        public static bool DeleteNote(ref PhoneData d,int index,string expected,Action<PhoneData> save)
            =>Change(ref d,n=>{if(index<0 || index>=n.Notes.Count || n.Notes[index]!=expected)return false;n.Notes.RemoveAt(index);return true;},save);
        public static bool Read(ref PhoneData d,int index,PhoneEntry expected,Action<PhoneData> save)
            =>Change(ref d,n=>{if(index<0 || index>=n.History.Count || expected==null || JsonConvert.SerializeObject(n.History[index])!=JsonConvert.SerializeObject(expected))return false;n.History[index].Read=true;return true;},save);
        public static bool Append(ref PhoneData d,string title,string body,string kind,Action<PhoneData> save)
            =>Change(ref d,n=>{n.History.Insert(0,new PhoneEntry{Title=title??"RPCore",Body=body??"",Kind=kind});if(n.History.Count>64)n.History.RemoveRange(64,n.History.Count-64);return true;},save);
        public static int[] History(PhoneData d,bool messages)=>d?.History==null?Array.Empty<int>():Enumerable.Range(0,d.History.Count).Where(i=>d.History[i]!=null && (messages?d.History[i].Kind==null || d.History[i].Kind=="message":d.History[i].Kind!="message")).ToArray();
    }
    public static class PhoneViewRules
    {
        public static int First(int selected)=>Math.Max(0,selected-6);
        public static int HomeHit(PointF p){for(int i=0;i<12;i++)if(new RectangleF(30.5f+i%3*65,154+i/3*65,48,48).Contains(p))return i;return -1;}
        public static int RowHit(PointF p,int first,int count,float top=78)
        {if(p.X<17 || p.X>=225 || p.Y<top)return -1;int display=(int)((p.Y-top)/39);if(display>=7 || p.Y>=top+display*39+35)return -1;int row=first+display;return row>=0 && row<count?row:-1;}
        public static List<string> Pages(string text)
        {text=text??"";var pages=new List<string>();for(int i=0;i<text.Length;i+=180)pages.Add(text.Substring(i,Math.Min(180,text.Length-i)));if(pages.Count==0)pages.Add("");return pages;}
        public static bool Amount(string text,out int value)=>int.TryParse(text?.Trim(),NumberStyles.None,CultureInfo.InvariantCulture,out value) && value>0;
        public static int BankMaximum(int cash,int bank,bool deposit)=>cash<0 || bank<0?0:Math.Min(deposit?cash:bank,int.MaxValue-(deposit?bank:cash));
    }
    public sealed class PhoneClockState
    {
        public long Elapsed,Remaining;public bool StopwatchRunning,TimerRunning;
        public void Stopwatch(){StopwatchRunning=!StopwatchRunning;}
        public void ResetStopwatch(){StopwatchRunning=false;Elapsed=0;}
        public bool SetMinutes(int minutes){if(minutes<1 || minutes>1440)return false;Remaining=minutes*60000L;TimerRunning=false;return true;}
        public void Timer(){if(Remaining>0)TimerRunning=!TimerRunning;}
        public void ResetTimer(){TimerRunning=false;Remaining=0;}
        public bool Advance(int milliseconds)
        {if(milliseconds<0 || milliseconds>1000)return false;if(StopwatchRunning)Elapsed=Math.Min(359999000,Elapsed+milliseconds);if(!TimerRunning)return false;Remaining=Math.Max(0,Remaining-milliseconds);if(Remaining>0)return false;TimerRunning=false;return true;}
        public static string Format(long ms){ms=Math.Max(0,Math.Min(359999000,ms));return (ms/3600000).ToString("00")+":"+(ms/60000%60).ToString("00")+":"+(ms/1000%60).ToString("00");}
    }
}

namespace RPCore
{
    public static class PhonePropRules
    {
        public static PropBinding Resolve(PropBinding requested)
        {
            if(requested==null || new[]{requested.X,requested.Y,requested.Z,requested.RX,requested.RY,requested.RZ}.Any(v=>float.IsNaN(v) || float.IsInfinity(v) || Math.Abs(v)>360))return new PropBinding{Model="phonesmart",Fallback="prop_npc_phone_02",X=.01f,Y=.01f,Z=0,RX=0,RY=0,RZ=180};
            return JsonConvert.DeserializeObject<PropBinding>(JsonConvert.SerializeObject(requested));
        }
    }
}
