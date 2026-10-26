using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangMowingOptions : ExtensibleData
    {
        public int BoostMinutes=60,StartRadius=8,GrassModel=992644101;
        public float MowRadius=1,MaxHeight=3,MaxSpeed=8;
        public string VehicleModel="mower";
        public JobPoint Spawn=new JobPoint{Name="Cortador Mirror Park",X=1140.882f,Y=-671.9385f,Z=56.51244f,Heading=102.5347f};
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public List<JobPoint> Patches=new List<JobPoint>{
            new JobPoint{Name="Grama 1",X=1157.133f,Y=-720.8135f,Z=55.72286f,Heading=75.39448f},
            new JobPoint{Name="Grama 2",X=1155.638f,Y=-721.8282f,Z=55.90508f,Heading=94.70795f},
            new JobPoint{Name="Grama 3",X=1153.69f,Y=-722.4634f,Z=56.03939f,Heading=-68.78673f},
            new JobPoint{Name="Grama 4",X=1149.27f,Y=-720.4521f,Z=55.84843f,Heading=1.370351f},
            new JobPoint{Name="Grama 5",X=1152.482f,Y=-723.6695f,Z=56.11326f,Heading=-81.34536f},
            new JobPoint{Name="Grama 6",X=1150.642f,Y=-716.8466f,Z=55.59871f,Heading=167.6395f},
            new JobPoint{Name="Grama 7",X=1132.815f,Y=-636.1116f,Z=55.92793f,Heading=4.348813f},
            new JobPoint{Name="Grama 8",X=1151.454f,Y=-681.891f,Z=56.11595f,Heading=-21.14242f},
            new JobPoint{Name="Grama 9",X=1134.388f,Y=-638.0372f,Z=55.8699f,Heading=-17.6511f},
            new JobPoint{Name="Grama 10",X=1156.157f,Y=-723.8207f,Z=56.17295f,Heading=-114.8983f},
            new JobPoint{Name="Grama 11",X=1149.341f,Y=-718.9081f,Z=55.69608f,Heading=30.43912f},
            new JobPoint{Name="Grama 12",X=1154.09f,Y=-720.4219f,Z=55.87249f,Heading=-75.05632f},
            new JobPoint{Name="Grama 13",X=1148.578f,Y=-677.5601f,Z=55.9958f,Heading=-0.2697125f},
            new JobPoint{Name="Grama 14",X=1084.159f,Y=-744.6563f,Z=56.22635f,Heading=130.2899f},
            new JobPoint{Name="Grama 15",X=1151.061f,Y=-719.0829f,Z=55.77216f,Heading=-7.844318f},
            new JobPoint{Name="Grama 16",X=1084.956f,Y=-746.0696f,Z=56.34259f,Heading=141.0297f},
            new JobPoint{Name="Grama 17",X=1133.328f,Y=-639.9659f,Z=55.70145f,Heading=-3.099748f},
            new JobPoint{Name="Grama 18",X=1087.607f,Y=-746.0865f,Z=56.25781f,Heading=157.7443f},
            new JobPoint{Name="Grama 19",X=1154.945f,Y=-719.2582f,Z=55.68201f,Heading=65.00165f},
            new JobPoint{Name="Grama 20",X=1088.147f,Y=-747.6478f,Z=56.37098f,Heading=165.1057f},
            new JobPoint{Name="Grama 21",X=1153.63f,Y=-718.2964f,Z=55.63919f,Heading=-163.0843f},
            new JobPoint{Name="Grama 22",X=1149.982f,Y=-678.5647f,Z=56.04425f,Heading=-30.84445f},
            new JobPoint{Name="Grama 23",X=1132.862f,Y=-638.8675f,Z=55.76547f,Heading=7.640225f},
            new JobPoint{Name="Grama 24",X=1148.546f,Y=-678.6489f,Z=55.97554f,Heading=0.4232008f},
            new JobPoint{Name="Grama 25",X=1149.876f,Y=-680.1031f,Z=56.03018f,Heading=-57.43496f},
            new JobPoint{Name="Grama 26",X=1147.517f,Y=-679.439f,Z=55.92527f,Heading=34.80906f},
            new JobPoint{Name="Grama 27",X=1147.207f,Y=-717.9334f,Z=55.63028f,Heading=-70.06063f},
            new JobPoint{Name="Grama 28",X=1087.675f,Y=-744.8999f,Z=56.09204f,Heading=154.1063f},
            new JobPoint{Name="Grama 29",X=1151.566f,Y=-722.7067f,Z=56.07423f,Heading=-56.92034f},
            new JobPoint{Name="Grama 30",X=1148.582f,Y=-721.6281f,Z=55.94584f,Heading=17.13465f},
            new JobPoint{Name="Grama 31",X=1150.082f,Y=-721.9327f,Z=56.00248f,Heading=-18.11727f},
            new JobPoint{Name="Grama 32",X=1157.054f,Y=-722.6756f,Z=55.97519f,Heading=113.85f},
            new JobPoint{Name="Grama 33",X=1152.566f,Y=-719.5705f,Z=55.84283f,Heading=-46.12759f},
            new JobPoint{Name="Grama 34",X=1148.868f,Y=-716.4595f,Z=55.61837f,Heading=147.6323f},
            new JobPoint{Name="Grama 35",X=1132.973f,Y=-637.3286f,Z=55.87065f,Heading=3.482667f},
            new JobPoint{Name="Grama 36",X=1085.913f,Y=-744.2867f,Z=56.04477f,Heading=137.7373f},
            new JobPoint{Name="Grama 37",X=1148.386f,Y=-717.7557f,Z=55.69659f,Heading=33.64442f},
            new JobPoint{Name="Grama 38",X=1134.716f,Y=-636.6951f,Z=55.94261f,Heading=-16.61176f},
            new JobPoint{Name="Grama 39",X=1147.495f,Y=-720.8206f,Z=55.78631f,Heading=29.26015f},
            new JobPoint{Name="Grama 40",X=1085.983f,Y=-745.0926f,Z=56.20356f,Heading=142.5018f},
            new JobPoint{Name="Grama 41",X=1147.743f,Y=-719.288f,Z=55.63368f,Heading=56.07708f},
            new JobPoint{Name="Grama 42",X=1086.619f,Y=-746.902f,Z=56.35656f,Heading=153.5871f},
            new JobPoint{Name="Grama 43",X=1149.532f,Y=-677.0668f,Z=56.10212f,Heading=-14.04138f},
            new JobPoint{Name="Grama 44",X=1134.774f,Y=-639.6f,Z=55.7724f,Heading=-36.79271f},
            new JobPoint{Name="Grama 45",X=1156.095f,Y=-720.0236f,Z=55.71206f,Heading=68.03264f},
            new JobPoint{Name="Grama 46",X=1152.334f,Y=-721.3746f,Z=55.95555f,Heading=-47.04672f},
            new JobPoint{Name="Grama 47",X=1150.781f,Y=-720.5151f,Z=55.8617f,Heading=-21.40907f},
            new JobPoint{Name="Grama 48",X=1148.694f,Y=-679.8876f,Z=55.97581f,Heading=-7.112273f},
            new JobPoint{Name="Grama 49",X=1154.338f,Y=-723.548f,Z=56.15086f,Heading=-83.16457f},
            new JobPoint{Name="Grama 50",X=1158.243f,Y=-721.6478f,Z=55.76892f,Heading=96.00825f},
            new JobPoint{Name="Grama 51",X=1152.293f,Y=-717.4599f,Z=55.59558f,Heading=-173.9114f},
            new JobPoint{Name="Grama 52",X=1151.569f,Y=-679.5269f,Z=56.10984f,Heading=-16.3785f},
            new JobPoint{Name="Grama 53",X=1150.301f,Y=-681.2018f,Z=56.05869f,Heading=-98.48989f},
            new JobPoint{Name="Grama 54",X=1147.051f,Y=-716.412f,Z=55.24933f,Heading=-58.10828f},
            new JobPoint{Name="Grama 55",X=1084.6f,Y=-743.6209f,Z=56.0132f,Heading=126.2182f},
            new JobPoint{Name="Grama 56",X=1150.352f,Y=-717.9007f,Z=55.64866f,Heading=6.706836f},
            new JobPoint{Name="Grama 57",X=1153.455f,Y=-724.7128f,Z=56.15927f,Heading=-97.8884f}};
        [JsonIgnore] public bool Valid=>BoostMinutes>=1 && BoostMinutes<=180 && StartRadius>=3 && StartRadius<=15 && GrassModel!=0 && GangPresenceRules.ModelName(VehicleModel) && Spawn?.Valid==true && GangPresenceRules.Finite(MowRadius) && MowRadius>=.5f && MowRadius<=2 && GangPresenceRules.Finite(MaxHeight) && MaxHeight>=1 && MaxHeight<=4 && GangPresenceRules.Finite(MaxSpeed) && MaxSpeed>=1 && MaxSpeed<=12 && Patches!=null && Patches.Count>=1 && Patches.Count<=200 && Patches.All(p=>p?.Valid==true && DeliveryRoute.Distance(Spawn,p)<=250) && Patches.Select(p=>p.X+"|"+p.Y+"|"+p.Z).Distinct().Count()==Patches.Count;
    }
    public sealed class GangMowingAttempt : ExtensibleData
    {
        public string Id,Gang;public GangTurfDefinition Area;public GangMowingOptions Options;public List<int> Mowed=new List<int>();
        [JsonIgnore] public bool Ready=>Options?.Valid==true && Mowed?.Count==Options.Patches.Count;
        [JsonIgnore] public bool Valid=>!string.IsNullOrWhiteSpace(Id) && Id.Length<=80 && !GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area?.Valid==true && Area.Id==8 && Options?.Valid==true && Mowed!=null && Mowed.Count<=Options.Patches.Count && Mowed.All(i=>i>=0 && i<Options.Patches.Count) && Mowed.Distinct().Count()==Mowed.Count;
    }
    public sealed class GangMowingAccount : ExtensibleData
    {
        public string Gang;public int Area=8;public long Completed,StartedUtcTicks,ExpiresUtcTicks;
        [JsonIgnore] public bool Valid=>!GangLoyaltyRules.Unaffiliated(Gang) && Gang.Length<=100 && Area==8 && Completed>0 && StartedUtcTicks>0 && ExpiresUtcTicks<=DateTime.MaxValue.Ticks && ExpiresUtcTicks>StartedUtcTicks && ExpiresUtcTicks-StartedUtcTicks<=TimeSpan.FromMinutes(180).Ticks;
    }
    public sealed class GangMowingData : ExtensibleData
    {
        public GangMowingAttempt Pending;public List<GangMowingAccount> Accounts=new List<GangMowingAccount>();public List<string> Receipts=new List<string>();
        [JsonIgnore] public bool Valid=>(Pending==null || Pending.Valid) && Accounts!=null && Accounts.Count<=640 && Accounts.All(a=>a?.Valid==true) && Accounts.Select(a=>a.Gang).Distinct(StringComparer.OrdinalIgnoreCase).Count()==Accounts.Count && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=120) && Receipts.Distinct().Count()==Receipts.Count;
    }
    public static class GangMowingRules
    {
        public static bool CanCut(bool safe,bool ownDriver,float speed,JobPoint position,JobPoint patch,GangMowingOptions c)
        {if(!safe || !ownDriver || c?.Valid!=true || position?.Valid!=true || patch?.Valid!=true || !GangPresenceRules.Finite(speed) || speed<0 || speed>c.MaxSpeed)return false;double x=(double)position.X-patch.X,y=(double)position.Y-patch.Y;return x*x+y*y<=c.MowRadius*c.MowRadius && Math.Abs(position.Z-patch.Z)<=c.MaxHeight;}
    }
    public static class GangMowingTransactions
    {
        static T Copy<T>(T v)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v));
        static bool Id(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=80;
        public static bool Blocked(WalletData w)=>w?.GangMowing!=null && (!w.GangMowing.Valid || w.GangMowing.Pending!=null);
        public static bool OtherFree(WalletData w)=>w!=null && !GangPopUpTransactions.Blocked(w) && !GangMedicalTransactions.Blocked(w) && !GangCashRunTransactions.Blocked(w) && !GangTowTransactions.Blocked(w) && !GangMechanicTransactions.Blocked(w) && !GangPromotionTransactions.Blocked(w) && (w.GangBulk==null || w.GangBulk.Valid && w.GangBulk.Pending==null) && (w.GangRaid==null || w.GangRaid.Valid && w.GangRaid.Pending==null) && (w.GangBusiness==null || w.GangBusiness.Valid && w.GangBusiness.Supply==null);
        public static GangMowingAccount Account(GangMowingData b,string gang)=>b?.Valid==true?b.Accounts.FirstOrDefault(a=>GangCrewRules.SameGang(a.Gang,gang)):null;
        public static int Multiplier(GangMowingData b,string gang,int area,DateTime utc)
        {var a=Account(b,gang);return area==8 && utc.Kind==DateTimeKind.Utc && a!=null && utc.Ticks>=a.StartedUtcTicks && utc.Ticks<a.ExpiresUtcTicks?2:1;}
        static bool Apply(WalletData w,string receipt,Func<GangMowingData,bool> mutate,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || save==null || w.GangMowing!=null && !w.GangMowing.Valid)return false;
            var old=w.GangMowing;if(old?.Receipts.Contains(receipt)==true)return true;var b=old==null?new GangMowingData():Copy(old);
            if(!mutate(b) || !b.Valid)return false;b.Receipts.Add(receipt);if(b.Receipts.Count>256)b.Receipts.RemoveAt(0);
            try{w.GangMowing=b;save();changed=true;return true;}catch{w.GangMowing=old;throw;}
        }
        static bool Owns(GangState s,GangTurfDefinition area,GangMowingAttempt p)=>p!=null && area?.Id==8 && GangBusinessTransactions.Owns(s,area) && GangCrewRules.SameGang(s.Gang,p.Gang);
        public static bool Begin(WalletData w,GangState s,GangTurfDefinition area,GangMowingOptions c,string id,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || area?.Id!=8 || !GangBusinessTransactions.Owns(s,area) || c?.Valid!=true || !OtherFree(w))return false;
            return Apply(w,id+":mowing-begin",b=>{if(b.Pending!=null)return false;var a=Account(b,s.Gang);if(a?.Completed==long.MaxValue || a==null && b.Accounts.Count>=640)return false;b.Pending=new GangMowingAttempt{Id=id,Gang=s.Gang,Area=Copy(area),Options=Copy(c)};return true;},save,out changed);
        }
        public static bool Cut(WalletData w,GangState s,GangTurfDefinition area,string id,int index,bool safe,bool ownDriver,float speed,JobPoint position,Action save,out bool changed)
        {
            changed=false;var p=w?.GangMowing?.Pending;if(!Id(id) || p?.Id!=id || !OtherFree(w) || !Owns(s,area,p) || w.GangMowing.Valid!=true || index<0 || index>=p.Options.Patches.Count || !GangMowingRules.CanCut(safe,ownDriver,speed,position,p.Options.Patches[index],p.Options))return false;
            return Apply(w,id+":mowing-cut:"+index,b=>{if(b.Pending.Mowed.Contains(index))return false;b.Pending.Mowed.Add(index);return true;},save,out changed);
        }
        public static bool Cancel(WalletData w,string id,Action save,out bool changed)
        {changed=false;if(!Id(id))return false;return Apply(w,id+":mowing-cancel",b=>{if(b.Pending?.Id!=id)return false;b.Pending=null;return true;},save,out changed);}
        public static bool Complete(WalletData w,GangState s,GangTurfDefinition area,string id,DateTime utc,Action save,out bool changed)
        {
            changed=false;if(!Id(id) || utc.Kind!=DateTimeKind.Utc || utc.Ticks==0 || area?.Id!=8 || !GangBusinessTransactions.Owns(s,area) || !OtherFree(w))return false;
            return Apply(w,id+":mowing-complete",b=>{var p=b.Pending;if(p?.Id!=id || !p.Ready || !Owns(s,area,p) || utc.Ticks>DateTime.MaxValue.Ticks-TimeSpan.FromMinutes(p.Options.BoostMinutes).Ticks)return false;
                var a=Account(b,p.Gang);if(a==null){if(b.Accounts.Count>=640)return false;a=new GangMowingAccount{Gang=p.Gang};b.Accounts.Add(a);}if(a.Completed==long.MaxValue)return false;
                a.Completed++;a.StartedUtcTicks=utc.Ticks;a.ExpiresUtcTicks=utc.AddMinutes(p.Options.BoostMinutes).Ticks;b.Pending=null;return true;},save,out changed);
        }
    }
}
