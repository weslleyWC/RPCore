using System;
using System.Collections.Generic;
using System.Linq;
namespace RPCore
{
    public sealed class PdmLocation
    {
        public string Id,Group,Name,Description;public JobPoint Point;
        public bool Valid=>!string.IsNullOrWhiteSpace(Id) && !string.IsNullOrWhiteSpace(Group) && !string.IsNullOrWhiteSpace(Name) && Point?.Valid==true;
        public static PdmLocation At(string id,string group,string name,string description,JobPoint point)=>new PdmLocation{Id=id,Group=group,Name=name,Description=description,Point=point};
        public static List<string> Groups(IEnumerable<PdmLocation> source)
        {
            var order=new[]{"Escritorio e compras","Logistica","Showroom","Equipe e seguranca","Contratos","Contrato atual"};
            return Available(source).Select(p=>p.Group).Distinct(StringComparer.Ordinal).OrderBy(g=>{int index=Array.IndexOf(order,g);return index<0?order.Length:index;}).ThenBy(g=>g,StringComparer.OrdinalIgnoreCase).ToList();
        }
        public static List<PdmLocation> Available(IEnumerable<PdmLocation> source)=>(source??Enumerable.Empty<PdmLocation>()).Where(p=>p?.Valid==true).GroupBy(p=>p.Id,StringComparer.Ordinal).Select(g=>g.First()).OrderBy(p=>p.Name,StringComparer.OrdinalIgnoreCase).Take(256).ToList();
    }
}
