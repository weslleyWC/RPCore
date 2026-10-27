using System;
using Newtonsoft.Json;
namespace RPCore
{
    public static class RoleplayDefaults
    {
        // One-time profile upgrade requested by the user; subsequent explicit choices survive reloads.
        public static bool Wallet(WalletData w,Action save)
        {
            if(w==null || save==null || w.RoleplayVersion<0)return false;if(w.RoleplayVersion>=1)return true;
            if(w.Traffic!=null && !w.Traffic.Valid || w.Custody!=null && !w.Custody.Valid)return false;
            var traffic=w.Traffic;var custody=w.Custody;var t=traffic==null?new TrafficData():JsonConvert.DeserializeObject<TrafficData>(JsonConvert.SerializeObject(traffic));var c=custody==null?new CustodyData():JsonConvert.DeserializeObject<CustodyData>(JsonConvert.SerializeObject(custody));
            t.Enabled=t.AdvancedOffences=t.PursuitEnabled=t.BribesEnabled=t.BoloEnabled=true;c.Enabled=true;
            try{w.Traffic=t;w.Custody=c;w.RoleplayVersion=1;save();return true;}catch{w.Traffic=traffic;w.Custody=custody;w.RoleplayVersion=0;throw;}
        }
        public static bool Gang(GangState s,Action<GangState> save,out GangState result)
        {
            result=s;if(s==null || save==null || s.RoleplayVersion<0)return false;if(s.RoleplayVersion>=1)return true;
            if(s.Loyalty!=null && !s.Loyalty.Valid || s.Strength!=null && !s.Strength.Valid || s.Driving!=null && !s.Driving.Valid || s.Turf!=null && !s.Turf.Valid || s.Warfare!=null && !s.Warfare.Valid || s.DriveBys!=null && !s.DriveBys.Valid)return false;
            var n=JsonConvert.DeserializeObject<GangState>(JsonConvert.SerializeObject(s));n.PresenceEnabled=n.TerritoriesEnabled=true;n.Warfare=n.Warfare??new GangWarData();n.Warfare.AttacksEnabled=n.Warfare.RivalsEnabled=n.Warfare.Reinforcements=true;n.DriveBys=n.DriveBys??new GangDriveByData();n.DriveBys.Enabled=true;n.RoleplayVersion=1;save(n);result=n;return true;
        }
    }
}
