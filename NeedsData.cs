using System;
using Newtonsoft.Json;
namespace RPCore
{
    public class NeedsData : ExtensibleData
    {
        public float Hunger=100f,Thirst=100f,Sleep=100f,Stamina=100f;
        public int StaminaCooldown;
        public SleepRecovery SleepRecovery;
        public string LastConsumptionId;
    }
    public sealed class NeedsConsumption : ExtensibleData
    {
        public string Id;public float Hunger,Thirst,Sleep;
        [JsonIgnore]public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && NeedsRules.Percent(Hunger) && NeedsRules.Percent(Thirst) && NeedsRules.Percent(Sleep);
    }
    public static class NeedsRules
    {
        public static bool Percent(float n)=>!float.IsNaN(n) && !float.IsInfinity(n) && n>=0 && n<=100;
        public static float Clamp(float n)=>float.IsNaN(n) || float.IsInfinity(n)?100:Math.Max(0,Math.Min(100,n));
        public static int Delta(int now,int before,bool active){int d=unchecked(now-before);return active && d>=0 && d<=1000?d:0;}
        public static float Rate(float value,float fallback)=>float.IsNaN(value) || float.IsInfinity(value) || value<=0?fallback:Math.Min(value,10);
        public static void Advance(NeedsData d,int milliseconds,float hunger,float thirst,float multiplier,bool rest)
        {if(d==null || milliseconds<0 || milliseconds>1000)return;float minutes=milliseconds/60000f,m=Rate(multiplier,1);d.Hunger=Clamp(d.Hunger-Rate(hunger,1)*m*minutes);d.Thirst=Clamp(d.Thirst-Rate(thirst,1)*m*minutes);d.Sleep=Clamp(d.Sleep+(rest?10:-2f/3f)*minutes);}
        public static NeedsConsumption Food(NeedsData d,string id,float hunger,float thirst)
        {if(d==null || string.IsNullOrWhiteSpace(id) || float.IsNaN(hunger) || float.IsInfinity(hunger) || float.IsNaN(thirst) || float.IsInfinity(thirst) || Math.Abs(hunger)>100 || Math.Abs(thirst)>100)return null;var r=new NeedsConsumption{Id=id,Hunger=Clamp(d.Hunger+hunger),Thirst=Clamp(d.Thirst+thirst),Sleep=Clamp(d.Sleep)};return r.Valid?r:null;}
        public static bool Apply(ref NeedsData d,NeedsConsumption receipt,Action<NeedsData> save)
        {
            if(d==null || receipt?.Valid!=true || save==null)return false;if(d.LastConsumptionId==receipt.Id)return true;
            var next=JsonConvert.DeserializeObject<NeedsData>(JsonConvert.SerializeObject(d));next.Hunger=receipt.Hunger;next.Thirst=receipt.Thirst;next.LastConsumptionId=receipt.Id;save(next);d=next;return true;
        }
        public static InvData Consume(InvData original,ItemDef item,NeedsConsumption receipt)
        {
            if(original?.Items==null || item==null || string.IsNullOrWhiteSpace(item.Id) || receipt?.Valid!=true || !original.Items.TryGetValue(item.Id,out int count) || count<=0)return null;
            var next=JsonConvert.DeserializeObject<InvData>(JsonConvert.SerializeObject(original));if(count==1)next.Items.Remove(item.Id);else next.Items[item.Id]=count-1;next.Consumption=receipt;return next;
        }
        public static void Stamina(NeedsData d,int milliseconds,bool sprinting,bool previouslySprinting)
        {
            if(d==null || milliseconds<0 || milliseconds>1000)return;
            d.Stamina=Clamp(d.Stamina);d.StaminaCooldown=Math.Max(0,Math.Min(5000,d.StaminaCooldown));
            if(!sprinting && previouslySprinting && d.Stamina<30 && d.StaminaCooldown==0)d.StaminaCooldown=5000;
            if(d.StaminaCooldown>0){d.StaminaCooldown=Math.Max(0,d.StaminaCooldown-milliseconds);d.Stamina=Clamp(d.Stamina+10*milliseconds/1000f);return;}
            if(sprinting){d.Stamina=Clamp(d.Stamina-7*(1-Clamp(d.Sleep)/200)*milliseconds/1000f);if(d.Stamina<=0)d.StaminaCooldown=5000;}
            else d.Stamina=Clamp(d.Stamina+15*milliseconds/1000f);
        }
        public static float Health(int health,int maximum)=>Clamp(maximum>100?(health-100)*100f/(maximum-100):health*100f/Math.Max(1,maximum));
    }
}
