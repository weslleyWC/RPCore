using System;
using System.Collections.Generic;
using System.Drawing;

namespace RPCore
{
    public static class InventoryMouseLayout
    {
        public static float Scale(bool storage,float width,float insetX,float insetY,float requested)
            =>Math.Min(Math.Min(1,requested),Math.Min((width-2*insetX)/(storage?1920:1320),(1080-2*insetY)/1080));
        public static bool Point(float normalizedX,float normalizedY,float width,float insetX,float insetY,float scale,out PointF point)
        {
            point=PointF.Empty;
            if(float.IsNaN(normalizedX) || float.IsNaN(normalizedY) || float.IsNaN(scale) || float.IsInfinity(scale) || scale<=0 || normalizedX<0 || normalizedX>1 || normalizedY<0 || normalizedY>1 || float.IsNaN(width) || float.IsInfinity(width) || width<=0 || float.IsNaN(insetX) || float.IsInfinity(insetX) || float.IsNaN(insetY) || float.IsInfinity(insetY))return false;
            point=new PointF((normalizedX*width-insetX)/scale,(normalizedY*1080-insetY)/scale);return true;
        }
        public static int Hit(PointF point,bool storage,int firstRow,int count)
        {
            if(firstRow<0 || count<=0)return -1;
            // Source sprites are140px on a130px pitch: later-drawn cells own the10px overlap.
            for(int local=23;local>=0;local--)
            {
                int index=firstRow*6+local;if(index<0 || index>=count)continue;
                float x=(storage?1040:140)+local%6*130,y=230+local/6*130;
                if(point.X>=x && point.X<x+140 && point.Y>=y && point.Y<y+140)return index;
            }
            return -1;
        }
    }
    public sealed class InventoryMouseDrag
    {
        public int Source{get;private set;}=-1;
        public string Item{get;private set;}
        public bool Active=>Source>=0;
        public bool Dragging{get;private set;}
        PointF start;
        public void Begin(int index,string item,PointF point)
        {Cancel();if(index<0 || string.IsNullOrEmpty(item))return;Source=index;Item=item;start=point;}
        public void Move(PointF point){if(Active && Math.Abs(point.X-start.X)+Math.Abs(point.Y-start.Y)>=6)Dragging=true;}
        public bool Drop(IList<string> slots,int target,Func<int,int,bool> saveMove)
        {
            try{return Dragging && slots!=null && Source<slots.Count && slots[Source]==Item && target>=0 && target<slots.Count && target!=Source && saveMove!=null && saveMove(Source,target);}
            finally{Cancel();}
        }
        public void Cancel(){Source=-1;Item=null;Dragging=false;}
    }
}
