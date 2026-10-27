using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public partial class GangDefinition : ExtensibleData
    {
        public string Name,Description,GangColor,LeaderModel;
        public float HQX,HQY,HQZ,LeaderSpawnHeading;
        public GangSpot LeaderSpawnPostion;
        public string[] PedModels;
        public List<GangSpot> PedSpawnSpots;
        public Dictionary<string,string> Relationships;
    }
    public class GangSpot : ExtensibleData { public float X,Y,Z,Heading; }
    public class GangState : ExtensibleData { public string Gang;public int Reputation,Rank;public bool PresenceEnabled;public GangLoyaltyData Loyalty;public GangStrengthData Strength;public GangDrivingData Driving;public bool TerritoriesEnabled;public GangTurfData Turf;public GangWarData Warfare;public GangDriveByData DriveBys; }
    public sealed class GangPresenceOptions : ExtensibleData
    {
        public int MaxActors=12,PerHQ=6,SpawnRadius=80,DespawnRadius=160,RespawnSeconds=120;
        [JsonIgnore] public bool Valid=>MaxActors>=1 && MaxActors<=24 && PerHQ>=1 && PerHQ<=8 && PerHQ<=MaxActors && SpawnRadius>=20 && SpawnRadius<=120 && DespawnRadius>=SpawnRadius+20 && DespawnRadius<=220 && RespawnSeconds>=30 && RespawnSeconds<=1800;
    }
    public sealed class GangPost
    {
        public string Key,Model;public GangSpot Spot;public bool Leader;
    }
    public static class GangPresenceRules
    {
        public static bool Finite(float n)=>!float.IsNaN(n) && !float.IsInfinity(n);
        public static bool ModelName(string name)=>!string.IsNullOrWhiteSpace(name) && name.Length<=64 && name.All(c=>c>='a' && c<='z' || c>='A' && c<='Z' || c>='0' && c<='9' || c=='_');
        public static bool Position(GangSpot p)=>p!=null && Finite(p.X) && Finite(p.Y) && Finite(p.Z) && Finite(p.Heading) && Math.Abs(p.X)<=10000 && Math.Abs(p.Y)<=10000 && p.Z>=-100 && p.Z<=2000;
        public static double Distance(GangSpot a,GangSpot b)=>Math.Sqrt(Math.Pow(a.X-b.X,2)+Math.Pow(a.Y-b.Y,2)+Math.Pow(a.Z-b.Z,2));
        public static GangSpot HQ(GangDefinition g)=>new GangSpot{X=g.HQX,Y=g.HQY,Z=g.HQZ};
        public static bool AtHQ(GangDefinition g,GangSpot p)=>g!=null && Position(HQ(g)) && Position(p) && Distance(HQ(g),p)<=70;
        public static string ContactModel(GangDefinition g)
        {
            if(g==null)return null;if(!string.IsNullOrWhiteSpace(g.LeaderModel))return ModelName(g.LeaderModel)?g.LeaderModel:null;
            if(g.Extra==null || !g.Extra.TryGetValue("CoLeaderModel",out var value) || value?.Type!=Newtonsoft.Json.Linq.JTokenType.String)return null;
            string co=(string)value;return ModelName(co)?co:null;
        }
        public static List<GangPost> Plan(GangDefinition g,int limit)
        {
            var posts=new List<GangPost>();if(g==null || string.IsNullOrWhiteSpace(g.Name) || limit<1 || limit>8 || !Position(HQ(g)))return posts;
            Action<string,string,GangSpot,bool> add=(key,model,spot,leader)=>{if(posts.Count<limit && ModelName(model) && AtHQ(g,spot) && posts.All(p=>Distance(p.Spot,spot)>=2))posts.Add(new GangPost{Key=key,Model=model,Spot=spot,Leader=leader});};
            add("leader",ContactModel(g),g.LeaderSpawnPostion==null?(string.IsNullOrWhiteSpace(g.LeaderModel) && ContactModel(g)!=null?new GangSpot{X=g.HQX+1.5f,Y=g.HQY,Z=g.HQZ,Heading=g.LeaderSpawnHeading}:null):new GangSpot{X=g.LeaderSpawnPostion.X,Y=g.LeaderSpawnPostion.Y,Z=g.LeaderSpawnPostion.Z,Heading=g.LeaderSpawnHeading},true);
            var models=(g.PedModels??Array.Empty<string>()).Where(ModelName).ToArray();
            if(models.Length>0 && g.PedSpawnSpots!=null)for(int i=0;i<g.PedSpawnSpots.Count && posts.Count<limit;i++)add("member:"+i,models[i%models.Length],g.PedSpawnSpots[i],false);
            return posts;
        }
        public static bool Keep(float distance,bool existing,GangPresenceOptions c)=>c!=null && c.Valid && Finite(distance) && distance>=0 && distance<=(existing?c.DespawnRadius:c.SpawnRadius);
        // Clone before editing. Only the explicit created-gang file is supplied by the caller.
        public static List<GangDefinition> Edit(List<GangDefinition> source,string name,GangSpot spot,bool leader,bool remove=false)
        {
            if(source==null || string.IsNullOrWhiteSpace(name))return null;
            var next=JsonConvert.DeserializeObject<List<GangDefinition>>(JsonConvert.SerializeObject(source));var matches=next.Where(g=>g!=null && string.Equals(g.Name,name,StringComparison.OrdinalIgnoreCase)).ToList();if(matches.Count!=1)return null;var g=matches[0];
            if(remove){if(g.PedSpawnSpots==null || g.PedSpawnSpots.Count==0)return null;g.PedSpawnSpots.RemoveAt(g.PedSpawnSpots.Count-1);return next;}
            if(!AtHQ(g,spot))return null;
            if(leader){if((g.PedSpawnSpots??new List<GangSpot>()).Any(p=>Position(p) && Distance(p,spot)<2))return null;g.LeaderSpawnPostion=g.LeaderSpawnPostion??new GangSpot();g.LeaderSpawnPostion.X=spot.X;g.LeaderSpawnPostion.Y=spot.Y;g.LeaderSpawnPostion.Z=spot.Z;g.LeaderSpawnHeading=spot.Heading;if(!ModelName(g.LeaderModel))g.LeaderModel="g_m_m_oggoon_01";}
            else{if(g.PedSpawnSpots==null)g.PedSpawnSpots=new List<GangSpot>();if(g.PedSpawnSpots.Count>=20 || g.PedSpawnSpots.Any(p=>Position(p) && Distance(p,spot)<2) || Position(g.LeaderSpawnPostion) && Distance(g.LeaderSpawnPostion,spot)<2)return null;g.PedSpawnSpots.Add(spot);if(g.PedModels==null || !g.PedModels.Any(ModelName))g.PedModels=new[]{"g_m_y_famca_01"};}
            return next;
        }
    }
}
