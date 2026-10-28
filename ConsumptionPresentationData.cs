using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class ConsumptionPresentationPlan
    {
        public PropBinding Binding;
        public string[] Models;
        public bool Drink;
        public int Bone;
    }
    public static class ConsumptionPresentationRules
    {
        static bool Finite(float n)=>!float.IsNaN(n) && !float.IsInfinity(n) && Math.Abs(n)<=360;
        public static ConsumptionPresentationPlan Resolve(ItemDef item,IDictionary<string,PropBinding> configured)
        {
            bool drink=item!=null && item.Health<=0 && item.Thirst>item.Hunger;
            string id=item?.Id??"",generic=item?.Health>0?"prop_ld_health_pack":drink?"ba_prop_club_water_bottle":"prop_cs_burger_01";
            PropBinding binding=null;bool custom=configured!=null && configured.TryGetValue(id,out binding) && binding!=null;
            if(custom && new[]{binding.X,binding.Y,binding.Z,binding.RX,binding.RY,binding.RZ}.All(Finite))binding=JsonConvert.DeserializeObject<PropBinding>(JsonConvert.SerializeObject(binding));
            else
            {
                custom=false;string primary=generic,backup=generic;
                if(id=="psrp_coke"){primary="coke";backup="prop_ecola_can";}
                else if(id=="psrp_mountaindew"){primary="mtdew";backup="prop_ecola_can";}
                else if(id.StartsWith("psrp_shaker",StringComparison.Ordinal) && drink)primary="shaker";
                else if(id.StartsWith("psrp_sandwich",StringComparison.Ordinal))primary="prop_sandwich_01";
                else if(id=="psrp_donut")primary="prop_donut_02";
                else if(id=="psrp_bagel")primary="p_ing_bagel_01";
                else if(new[]{"psrp_hershey","psrp_hersheycookie","psrp_twix","psrp_snickers","psrp_reeses","psrp_milkyway","psrp_babyruth","psrp_galaxybar","psrp_marsbar"}.Contains(id))primary="prop_choc_meto";
                else if(id=="psrp_orange"){primary="orange";backup="prop_cs_orange_01";}
                else if(id=="psrp_lime"){primary="lime";backup="prop_cs_orange_01";}
                binding=new PropBinding{Model=primary,Fallback=backup,Drink=drink,X=0,Y=0,Z=id.StartsWith("psrp_shaker",StringComparison.Ordinal)?-.08f:-.05f,RX=0,RY=0,RZ=0};
            }
            return new ConsumptionPresentationPlan{Binding=binding,Drink=item?.Health<=0 && binding.Drink,Bone=custom?57005:60309,Models=new[]{binding.Model,binding.Fallback,generic}.Where(m=>!string.IsNullOrWhiteSpace(m)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()};
        }
    }
}
