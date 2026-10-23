using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RPCore
{
    public sealed class HuntMaterial
    {
        public string Id,Name;public float Weight;public int Price;
    }
    public sealed class HuntingOptions : ExtensibleData
    {
        public JobPoint Vendor,Forest;
        public float ZoneRadius=450,BagCapacity=25;
        public int LicensePrice=750,BaitPrice=20,AnimalsPerTrip=3,HarvestSeconds=5;
        public List<HuntMaterial> Materials=new List<HuntMaterial>();
        [JsonIgnore] public bool Valid=>Vendor!=null && Vendor.Valid && Forest!=null && Forest.Valid && !float.IsNaN(ZoneRadius) && !float.IsInfinity(ZoneRadius) && ZoneRadius>=100 && ZoneRadius<=1500 && !float.IsNaN(BagCapacity) && !float.IsInfinity(BagCapacity) && BagCapacity>=5 && BagCapacity<=500 && LicensePrice>=0 && LicensePrice<=100000 && BaitPrice>=1 && BaitPrice<=10000 && AnimalsPerTrip>=1 && AnimalsPerTrip<=10 && HarvestSeconds>=2 && HarvestSeconds<=15 && Materials!=null && Materials.Count>=8 && Materials.All(m=>m!=null && !string.IsNullOrWhiteSpace(m.Id) && !string.IsNullOrWhiteSpace(m.Name) && !float.IsNaN(m.Weight) && !float.IsInfinity(m.Weight) && m.Weight>0 && m.Weight<=10 && m.Price>=0 && m.Price<=100000) && Materials.Select(m=>m.Id).Distinct().Count()==Materials.Count && HuntingLoot.Ids.All(id=>Materials.Any(m=>m.Id==id));
    }
    public static class HuntingDefaults
    {
        public static HuntingOptions Load()
        {using(var s=typeof(HuntingDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.Hunting.json"))using(var r=new System.IO.StreamReader(s))return JsonConvert.DeserializeObject<HuntingOptions>(r.ReadToEnd());}
    }
    public sealed class HuntingData : ExtensibleData
    {
        public bool Licensed;
        public int Bait,Harvests,Trips;
        public long Earned;
        public Dictionary<string,int> Bag=new Dictionary<string,int>();
        public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>Bait>=0 && Bait<=999 && Harvests>=0 && Trips>=0 && Earned>=0 && Bag!=null && Bag.All(p=>!string.IsNullOrWhiteSpace(p.Key) && p.Value>=0 && p.Value<=1000000) && Receipts!=null;
    }
    public enum HuntSpecies { Deer,Boar,Rabbit }
    public static class HuntingLoot
    {
        public static readonly string[] Ids={"deer_meat","boar_meat","rabbit_meat","hide_rough","hide_good","hide_perfect","antler","rabbit_foot"};
        public static Dictionary<string,int> Create(HuntSpecies species,int quality)
        {
            if(!Enum.IsDefined(typeof(HuntSpecies),species) || quality<0 || quality>3)throw new ArgumentOutOfRangeException();
            var loot=new Dictionary<string,int>();if(quality==0)return loot;
            loot[species==HuntSpecies.Deer?"deer_meat":species==HuntSpecies.Boar?"boar_meat":"rabbit_meat"]=species==HuntSpecies.Deer?4:species==HuntSpecies.Boar?4:2;
            loot[quality==3?"hide_perfect":quality==2?"hide_good":"hide_rough"]=1;
            if(species==HuntSpecies.Deer)loot["antler"]=1;if(species==HuntSpecies.Rabbit)loot["rabbit_foot"]=1;return loot;
        }
        public static double Weight(HuntingData d,HuntingOptions c)=>d.Bag.Sum(p=>p.Value*(double)(c.Materials.FirstOrDefault(m=>m.Id==p.Key)?.Weight??1f));
    }
    public static class HuntingTransactions
    {
        static bool Apply(WalletData w,string receipt,Func<HuntingData,bool> change,int delta,Action save,out bool changed)
        {
            changed=false;if(w==null || string.IsNullOrWhiteSpace(receipt) || receipt.Length>160 || w.Bank<0)return false;
            var old=w.Hunting;
            if(old!=null && !old.Valid)return false;
            if(old?.Receipts.Contains(receipt)==true)return true;
            if((long)w.Bank+delta<0 || (long)w.Bank+delta>int.MaxValue)return false;
            var next=old==null?new HuntingData():JsonConvert.DeserializeObject<HuntingData>(JsonConvert.SerializeObject(old));
            if(!change(next))return false;
            next.Receipts.Add(receipt);if(next.Receipts.Count>512)next.Receipts.RemoveRange(0,next.Receipts.Count-512);
            int before=w.Bank;
            try{w.Hunting=next;w.Bank=before+delta;save();changed=true;return true;}
            catch{w.Hunting=old;w.Bank=before;throw;}
        }
        public static bool License(WalletData w,HuntingOptions c,string receipt,Action save,out bool changed)
        {if(c==null || !c.Valid){changed=false;return false;}return Apply(w,receipt,h=>{if(h.Licensed)return false;h.Licensed=true;return true;},-c.LicensePrice,save,out changed);}
        public static bool BuyBait(WalletData w,HuntingOptions c,int count,string receipt,Action save,out bool changed)
        {if(c==null || !c.Valid || count<1 || count>20){changed=false;return false;}return Apply(w,receipt,h=>{if(!h.Licensed || h.Bait>999-count)return false;h.Bait+=count;return true;},-c.BaitPrice*count,save,out changed);}
        public static bool UseBait(WalletData w,string receipt,Action save,out bool changed)
        {return Apply(w,receipt,h=>{if(!h.Licensed || h.Bait<1)return false;h.Bait--;return true;},0,save,out changed);}
        public static bool Harvest(WalletData w,HuntingOptions c,Dictionary<string,int> loot,string receipt,bool tripComplete,Action save,out bool changed)
        {
            if(c==null || !c.Valid || loot==null || loot.Any(p=>p.Value<1 || p.Value>30 || !c.Materials.Any(m=>m.Id==p.Key))){changed=false;return false;}
            return Apply(w,receipt,h=>{
                if(!h.Licensed || h.Harvests==int.MaxValue || (tripComplete && h.Trips==int.MaxValue))return false;
                foreach(var p in loot){h.Bag.TryGetValue(p.Key,out int n);if(n>1000000-p.Value)return false;h.Bag[p.Key]=n+p.Value;}
                if(HuntingLoot.Weight(h,c)>c.BagCapacity+.0001)return false;
                h.Harvests++;if(tripComplete)h.Trips++;return true;
            },0,save,out changed);
        }
        public static int Quote(HuntingData h,HuntingOptions c)
        {
            if(h==null || !h.Valid || c==null || !c.Valid)return 0;
            long total=0;foreach(var p in h.Bag){var m=c.Materials.FirstOrDefault(i=>i.Id==p.Key);if(m!=null)total+=(long)m.Price*p.Value;}
            return total>1000000?-1:(int)total;
        }
        public static bool Sell(WalletData w,HuntingOptions c,string receipt,Action save,out bool changed)
        {
            int total=Quote(w?.Hunting,c);if(total<=0){changed=false;return false;}
            return Apply(w,receipt,h=>{if(!h.Licensed || h.Earned>long.MaxValue-total)return false;foreach(var id in c.Materials.Select(m=>m.Id))h.Bag.Remove(id);h.Earned+=total;return true;},total,save,out changed);
        }
    }
    public enum HuntingStage { Ready,Tracking,Carcass,Harvesting,Finished,Cancelled }
    public sealed class HuntingRun
    {
        public readonly string Id=Guid.NewGuid().ToString("N");public readonly int Total;
        public int Collected {get;private set;}public HuntingStage Stage {get;private set;}=HuntingStage.Ready;
        int began;readonly int duration;
        public HuntingRun(int total,int seconds){if(total<1 || total>10 || seconds<2 || seconds>15)throw new ArgumentOutOfRangeException();Total=total;duration=seconds*1000;}
        public bool Track(){if(Stage!=HuntingStage.Ready)return false;Stage=HuntingStage.Tracking;return true;}
        public bool FoundCarcass(bool dead){if(Stage!=HuntingStage.Tracking || !dead)return false;Stage=HuntingStage.Carcass;return true;}
        public bool BeginHarvest(int now,bool eligible){if(Stage!=HuntingStage.Carcass || !eligible)return false;began=now;Stage=HuntingStage.Harvesting;return true;}
        public void Interrupt(){if(Stage==HuntingStage.Harvesting)Stage=HuntingStage.Carcass;}
        public bool Commit(int now,bool eligible,Func<int,bool,bool> collect)
        {if(Stage!=HuntingStage.Harvesting)return false;if(!eligible){Interrupt();return false;}if(unchecked(now-began)<duration || !collect(Collected+1,Collected+1==Total))return false;Collected++;Stage=Collected==Total?HuntingStage.Finished:HuntingStage.Ready;return true;}
        public void LostAnimal(){if(Stage==HuntingStage.Tracking || Stage==HuntingStage.Carcass || Stage==HuntingStage.Harvesting)Stage=HuntingStage.Ready;}
        public void Cancel(){if(Stage!=HuntingStage.Finished)Stage=HuntingStage.Cancelled;}
    }
}
