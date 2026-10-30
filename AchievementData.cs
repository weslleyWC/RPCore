using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public class AchDef { public string Id,Name,Desc,Stat;public int Target; }
    public class AchData : ExtensibleData
    {
        public Dictionary<string,int> Stats=new Dictionary<string,int>();
        public List<string> Unlocked=new List<string>();
    }
    public static class AchievementRules
    {
        public static bool Valid(AchData d)=>d?.Stats!=null && d.Unlocked!=null && d.Stats.All(s=>!string.IsNullOrWhiteSpace(s.Key) && s.Value>=0) && d.Unlocked.All(s=>!string.IsNullOrWhiteSpace(s));
        public static int Get(AchData d,string stat)=>stat!=null && d?.Stats!=null && d.Stats.TryGetValue(stat,out int n)?n:0;
        public static List<AchDef> Bump(ref AchData d,IEnumerable<AchDef> definitions,string stat,int amount,Action<AchData> save)
        {
            var unlocked=new List<AchDef>();if(!Valid(d) || definitions==null || string.IsNullOrWhiteSpace(stat) || amount<=0 || save==null)return unlocked;
            var next=JsonConvert.DeserializeObject<AchData>(JsonConvert.SerializeObject(d));
            next.Stats[stat]=(int)Math.Min(int.MaxValue,(long)Get(d,stat)+amount);
            foreach(var def in definitions.Where(a=>a!=null && !string.IsNullOrWhiteSpace(a.Id) && a.Target>0 && a.Stat==stat).GroupBy(a=>a.Id).Select(g=>g.First()))
                if(!next.Unlocked.Contains(def.Id) && next.Stats[stat]>=def.Target){next.Unlocked.Add(def.Id);unlocked.Add(def);}
            save(next);d=next;return unlocked;
        }
        public static IEnumerable<AchDef> Filter(IEnumerable<AchDef> definitions,AchData d,string filter)
        {
            if(!Valid(d))return Enumerable.Empty<AchDef>();
            return definitions.Where(a=>filter=="done"?d.Unlocked.Contains(a.Id):filter=="pending"?!d.Unlocked.Contains(a.Id) && !string.IsNullOrWhiteSpace(a.Stat):filter=="legacy"?string.IsNullOrWhiteSpace(a.Stat):true);
        }
        public static int Completed(IEnumerable<AchDef> definitions,AchData d)=>Valid(d)?definitions.Select(a=>a.Id).Distinct(StringComparer.Ordinal).Count(id=>d.Unlocked.Contains(id)):0;
    }
}
