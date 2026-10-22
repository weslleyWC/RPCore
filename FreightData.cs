using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RPCore
{
    public class StoreDeliveryOptions : DeliveryOptions
    {
        public int CandidateStores = 8, LoadSeconds = 3;
        [JsonIgnore] public bool ValidFreight => Valid && CandidateStores >= Drops && CandidateStores <= 100 && LoadSeconds >= 2 && LoadSeconds <= 20;
    }
    public static class FreightDefaults
    {
        public static StoreDeliveryOptions Load()
        {
            using (var stream = typeof(FreightDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.StoreDelivery.json"))
            using (var reader = new System.IO.StreamReader(stream)) return JsonConvert.DeserializeObject<StoreDeliveryOptions>(reader.ReadToEnd());
        }
        public static List<JobPoint> Route(StoreDeliveryOptions config, Random random)
        {
            if (config == null || !config.ValidFreight) throw new ArgumentException("Invalid store delivery configuration.");
            var nearby = config.Addresses.GroupBy(p => new { p.X, p.Y, p.Z }).Select(g => g.First())
                .OrderBy(p => DeliveryRoute.Distance(config.Start,p)).Take(config.CandidateStores);
            return DeliveryRoute.Select(nearby,config.Start,config.Drops,random);
        }
    }
    public sealed class ContextReservation
    {
        string owner;
        JobPoint point;
        float radius;
        public void Set(string owner, JobPoint point, float radius)
        {
            if (string.IsNullOrWhiteSpace(owner) || point == null || !point.Valid || float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0 || radius > 30) throw new ArgumentException("Invalid context reservation.");
            this.owner = owner; this.point = new JobPoint { Name=point.Name,X=point.X,Y=point.Y,Z=point.Z }; this.radius = radius;
        }
        public bool Contains(float x, float y, float z) => owner != null &&
            Math.Pow(x-point.X,2)+Math.Pow(y-point.Y,2)+Math.Pow(z-point.Z,2) <= radius*radius;
        public void Clear(string owner) { if (this.owner == owner) { this.owner = null; point = null; } }
    }
    public static class WorkContext
    {
        public static readonly ContextReservation ShopDelivery = new ContextReservation();
    }
    public static class KeyBindingRules
    {
        // Windows virtual key codes: O, F2, NumPad0. No local INI is rewritten.
        public static int Garage(int requested, int phone, int settings, bool phoneEnabled)
        {
            if (requested != settings && (!phoneEnabled || requested != phone)) return requested;
            foreach (int candidate in new[] { 0x4F, 0x71, 0x60 })
                if (candidate != settings && (!phoneEnabled || candidate != phone)) return candidate;
            return requested;
        }
    }
}
