using System;
using System.Collections.Generic;
using Newtonsoft.Json;
namespace RPCore
{
    public enum GangSupplyCargo { Legal,Criminal }
    public sealed class GangCriminalSupplyOptions : ExtensibleData
    {
        public int Units=10,DiscountPercent=10;
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)]
        public GangSupplyOptions Route=new GangSupplyOptions{Suppliers=new List<JobPoint>{
            new JobPoint{Name="Carga criminal / La Mesa",X=887.2639f,Y=-991.1592f,Z=32.74689f},
            new JobPoint{Name="Carga criminal / centro",X=400.9214f,Y=-354.4539f,Z=46.81522f},
            new JobPoint{Name="Carga criminal / oeste",X=-1631.082f,Y=-124.8462f,Z=57.98261f}}};
        [JsonIgnore] public bool Valid=>Units>=1 && Units<=100 && DiscountPercent>=0 && DiscountPercent<=50 && Route!=null && Route.Valid;
    }
}
