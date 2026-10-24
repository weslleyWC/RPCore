using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class PdmGuardPost : ExtensibleData
    {
        public PdmStaffOffer Offer;
        public JobPoint Point;
        public string Model;
        [JsonIgnore] public bool Valid => Offer != null && Offer.Valid && PdmSecurityPolicy.IsGuard(Offer.Id) && Offer.Requires == null && Offer.SaleBonusPercent == 0 && Point != null && Point.Valid && !string.IsNullOrWhiteSpace(Model) && Model.Length <= 60;
    }
    public sealed class PdmSecurityOptions : ExtensibleData
    {
        public List<PdmGuardPost> Posts = new List<PdmGuardPost>();
        public int DefenseRadius = 20, RespawnSeconds = 60, EngagementSeconds = 20;
        public bool EnableDefense = true;
        [JsonIgnore] public bool Valid => Posts != null && Posts.Count == 2 && Posts.All(p => p != null && p.Valid) && Posts.Select(p => p.Offer.Id).Distinct().Count() == 2 && DefenseRadius >= 10 && DefenseRadius <= 30 && RespawnSeconds >= 30 && RespawnSeconds <= 300 && EngagementSeconds >= 5 && EngagementSeconds <= 30;
    }
    public static class PdmSecurityDefaults
    {
        public static PdmSecurityOptions Load()
        { using (var s = typeof(PdmSecurityDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.PdmSecurity.json")) using (var r = new System.IO.StreamReader(s)) return JsonConvert.DeserializeObject<PdmSecurityOptions>(r.ReadToEnd()); }
        public static PdmStaffOptions Merge(PdmStaffOptions staff, PdmSecurityOptions security)
        {
            if (staff == null || !staff.Valid || security == null || !security.Valid) throw new ArgumentException();
            var copy = JsonConvert.DeserializeObject<PdmStaffOptions>(JsonConvert.SerializeObject(staff));
            foreach (var post in security.Posts) if (!copy.Roles.Any(r => r.Id == post.Offer.Id)) copy.Roles.Add(JsonConvert.DeserializeObject<PdmStaffOffer>(JsonConvert.SerializeObject(post.Offer)));
            return copy;
        }
    }
    // Shared by payroll eligibility, native target selection and tests. This never changes world relationships.
    public static class PdmSecurityPolicy
    {
        public static bool IsGuard(string role) => role == "security1" || role == "security2";
        public static bool Paid(PdmData d, string role) => IsGuard(role) && d != null && d.Valid && d.Owned && d.Staff.FormerDebt == 0 && d.Staff.Employees.Any(e => e.Role == role && e.Owed == 0);
        public static bool Local(float distance, int radius) => !float.IsNaN(distance) && distance >= 0 && distance <= radius;
        public static bool CanDefend(bool enabled, bool paid, bool worldAllowed, bool human, bool alive, bool player, bool police, bool missionEntity, bool protectedPerson, bool attacking, bool visible, float targetDistance, float guardDistance, int radius)
            => enabled && paid && worldAllowed && human && alive && !player && !police && !missionEntity && !protectedPerson && attacking && visible && Local(targetDistance, radius) && Local(guardDistance, radius);
    }
}
