using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class PdmCameraPoint : ExtensibleData
    {
        public string Id, Name;
        public JobPoint Mount, View, LookAt;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(Id) && Id.Length <= 40 && !string.IsNullOrWhiteSpace(Name) && Mount != null && Mount.Valid && View != null && View.Valid && LookAt != null && LookAt.Valid
            && Distance(Mount, View) <= 4 && Distance(View, LookAt) >= 2 && Distance(View, LookAt) <= 100;
        static double Distance(JobPoint a, JobPoint b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2) + Math.Pow(a.Z - b.Z, 2));
    }
    public sealed class PdmSurveillanceOptions : ExtensibleData
    {
        public int Price = 15000, FieldOfView = 65, MaxViewSeconds = 120;
        public string Model = "prop_cctv_cam_04c";
        public List<PdmCameraPoint> Cameras = new List<PdmCameraPoint>();
        [JsonIgnore] public bool Valid => Price > 0 && Price <= 1000000 && FieldOfView >= 40 && FieldOfView <= 90 && MaxViewSeconds >= 30 && MaxViewSeconds <= 300 && !string.IsNullOrWhiteSpace(Model) && Model.Length <= 60 && Cameras != null && Cameras.Count >= 1 && Cameras.Count <= 6 && Cameras.All(c => c != null && c.Valid) && Cameras.Select(c => c.Id).Distinct().Count() == Cameras.Count;
    }
    public static class PdmSurveillanceDefaults
    {
        public static PdmSurveillanceOptions Load()
        { using (var s = typeof(PdmSurveillanceDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.PdmSurveillance.json")) using (var r = new System.IO.StreamReader(s)) return JsonConvert.DeserializeObject<PdmSurveillanceOptions>(r.ReadToEnd()); }
    }
    public sealed class PdmSurveillanceData : ExtensibleData
    {
        public bool Purchased;
        public int Paid;
        [JsonIgnore] public bool Valid => Paid >= 0 && Paid <= 1000000 && (Purchased ? Paid > 0 : Paid == 0);
    }
    public static class PdmSurveillanceTransactions
    {
        public static bool Purchase(WalletData w, PdmSurveillanceOptions c, string receipt, Action save, out bool changed)
        {
            changed = false; if (c == null || !c.Valid) return false;
            return PdmTransactions.Apply(w, receipt, 0, d => {
                if (!PdmShowroomTransactions.Idle(d) || d.Surveillance.Purchased || d.Cash < c.Price || d.Staff.UpgradeSpent > long.MaxValue - c.Price) return false;
                d.Cash -= c.Price; d.Staff.UpgradeSpent += c.Price; d.Surveillance.Purchased = true; d.Surveillance.Paid = c.Price;
                PdmTransactions.Entry(d, "Cameras externas / monitoramento", -c.Price); return true;
            }, save, out changed);
        }
    }
    public sealed class PdmMonitorSession
    {
        public int Index { get; private set; }
        public bool Active { get; private set; } = true;
        readonly int count, began, duration; bool renderConfirmed;
        public PdmMonitorSession(int count, int index, int now, int seconds)
        { if (count < 1 || count > 6 || index < 0 || index >= count || seconds < 30 || seconds > 300) throw new ArgumentException(); this.count = count; Index = index; began = now; duration = seconds * 1000; }
        public bool Rendering(int now, bool ours, bool another) { if (ours) renderConfirmed = true; return Active && (ours || !renderConfirmed && !another && unchecked(now - began) >= 0 && unchecked(now - began) < 500); }
        public bool Continue(int now, bool eligible) { if (!eligible || unchecked(now - began) >= duration) Active = false; return Active; }
        public bool Switch(int direction) { if (!Active || direction != -1 && direction != 1) return false; Index = (Index + direction + count) % count; return true; }
        public void Close() { Active = false; }
    }
}
