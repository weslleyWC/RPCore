using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class FaceHeritage : ExtensibleData
    {
        public int ShapeFirst,ShapeSecond,SkinFirst,SkinSecond,ShapeMix=50,SkinMix=50;
        [JsonIgnore] public bool Valid => Parent(ShapeFirst) && Parent(ShapeSecond) && Parent(SkinFirst) && Parent(SkinSecond) && ShapeMix>=0 && ShapeMix<=100 && SkinMix>=0 && SkinMix<=100;
        static bool Parent(int id)=>id>=0 && id<=255;
        public bool Same(FaceHeritage other)=>other!=null && ShapeFirst==other.ShapeFirst && ShapeSecond==other.ShapeSecond && SkinFirst==other.SkinFirst && SkinSecond==other.SkinSecond && ShapeMix==other.ShapeMix && SkinMix==other.SkinMix;
        public void SetValues(FaceHeritage source)
        {ShapeFirst=source.ShapeFirst;ShapeSecond=source.ShapeSecond;SkinFirst=source.SkinFirst;SkinSecond=source.SkinSecond;ShapeMix=source.ShapeMix;SkinMix=source.SkinMix;}
    }
    public sealed class FaceMorph : ExtensibleData
    {
        public int Index,Value;
        [JsonIgnore] public bool Valid=>Index>=0 && Index<20 && Value>=-100 && Value<=100;
    }
    public struct FaceHeadRange
    {
        public int First,Count;
        public FaceHeadRange(int first,int count){First=first;Count=count;}
    }
    public enum FaceWait { Waiting,Ready,Interrupted }
    public static class FaceRules
    {
        static readonly string[] labels={"Largura do nariz","Altura do nariz","Comprimento do nariz","Ponte do nariz","Ponta do nariz","Desvio do nariz","Altura das sobrancelhas","Profundidade das sobrancelhas","Altura das macas do rosto","Largura das macas do rosto","Volume das bochechas","Abertura dos olhos","Espessura dos labios","Largura da mandibula","Formato da mandibula","Altura do queixo","Comprimento do queixo","Largura do queixo","Cova do queixo","Largura do pescoco"};
        public static string Label(int index){if(index<0 || index>=labels.Length)throw new ArgumentOutOfRangeException(nameof(index));return labels[index];}
        public static int[] Parents(IEnumerable<FaceHeadRange> ranges)
        {
            if(ranges==null)return Array.Empty<int>();var result=new SortedSet<int>();
            foreach(var range in ranges){
                if(range.Count==0)continue;
                if(range.Count<0 || range.First<0 || range.First>255 || range.Count>256 || (long)range.First+range.Count>256)return Array.Empty<int>();
                for(int i=0;i<range.Count;i++)result.Add(range.First+i);
            }
            return result.ToArray();
        }
        public static bool Compatible(FaceHeritage h,int[] parents)=>h!=null && h.Valid && parents!=null && new[]{h.ShapeFirst,h.ShapeSecond,h.SkinFirst,h.SkinSecond}.All(id=>Array.IndexOf(parents,id)>=0);
        public static int CycleParent(int current,int delta,int[] parents)
        {if(parents==null || parents.Length==0)throw new ArgumentException("No native head indices.");int i=Array.IndexOf(parents,current);return parents[i<0?0:(int)(((long)i+delta%parents.Length+parents.Length)%parents.Length)];}
        public static int Step(int current,int delta,bool mix)=>(int)Math.Max(mix?0L:-100L,Math.Min(100L,(long)current+delta));
        public static FaceWait Wait(bool sameOwner,bool access,bool menuVisible,bool ready,uint elapsed)
        {if(!sameOwner || !access || !menuVisible || elapsed>=10000)return FaceWait.Interrupted;return ready && elapsed>0?FaceWait.Ready:FaceWait.Waiting;}
    }
}
