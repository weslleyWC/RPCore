using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RPCore
{
    public sealed class InventoryArmory : ExtensibleData
    {
        public Dictionary<string,CharacterWeapon> Weapons = new Dictionary<string,CharacterWeapon>();
        public HashSet<string> Stacked = new HashSet<string>();
        [JsonIgnore] public bool Valid => Weapons!=null && Weapons.Count<=256 && Weapons.All(p=>p.Value!=null && p.Value.Valid && p.Key==InventoryArmoryRules.WeaponId(p.Value.Hash)) && Stacked!=null && Stacked.All(id=>Weapons.ContainsKey(id) && Weapons[id].AmmoType!=0 && Weapons.Values.Count(w=>w.AmmoType==Weapons[id].AmmoType)==1);
    }
    public sealed class ArmoryObservation
    {
        public CharacterWeapon Weapon;
        public int Ammo;
        public bool Stacked;
        public bool Reacquired,NewAmmo;
    }
    public static class InventoryArmoryRules
    {
        public const int MaxAmmo=1000000;
        public static string WeaponId(int hash)=>"weapon_"+unchecked((uint)hash).ToString("x8");
        public static string AmmoId(int hash)=>"ammo_"+unchecked((uint)hash).ToString("x8");
        public static bool ActivityAllows(string owner)=>owner==null || owner=="homestorage" || owner=="inventory_use";
        public static string PoolItem(InventoryArmory data,int type)=>data.Stacked.FirstOrDefault(id=>data.Weapons[id].AmmoType==type)??AmmoId(type);
        static InvData Copy(InvData data)=>JsonConvert.DeserializeObject<InvData>(JsonConvert.SerializeObject(data));
        public static bool CanReacquire(InvData data,string id)
        {
            if(data?.Items==null || data.Containers==null || data.Armory?.Valid!=true || id==null || !data.Armory.Weapons.ContainsKey(id))return false;
            if(data.Items.TryGetValue(id,out int count) && count!=0)return false;
            return !(data.GroundItems?.Any(item=>item!=null && item.Item==id && item.Quantity>0)??false) && !data.Containers.Values.Any(box=>box!=null && box.TryGetValue(id,out int n) && n!=0);
        }
        public static bool Import(ref InvData data,IEnumerable<ArmoryObservation> observations,Action<InvData> save)
        {
            if(data?.Items==null || data.Armory!=null && !data.Armory.Valid || observations==null || save==null)return false;
            var rows=observations.ToList();
            if(rows.Count>256 || rows.Any(o=>o?.Weapon?.Valid!=true || o.Ammo<0 || o.Ammo>MaxAmmo || o.Stacked && o.Weapon.AmmoType==0))return false;
            var n=Copy(data);bool changed=n.Armory==null;n.Armory=n.Armory??new InventoryArmory();
            var accepted=new List<ArmoryObservation>();
            // A fresh native ownership transition may replace a discarded gun only when no copy
            // remains in personal/container stock. Reloading an old native arsenal is not proof.
            foreach(var o in rows){string id=WeaponId(o.Weapon.Hash);bool known=n.Armory.Weapons.ContainsKey(id);if(known && (!o.Reacquired || !CanReacquire(n,id)))continue;
                var previous=known?n.Armory.Weapons[id]:null;
                var snapshot=JsonConvert.DeserializeObject<CharacterWeapon>(JsonConvert.SerializeObject(o.Weapon));if(previous!=null)snapshot.Extra=previous.Extra;
                n.Armory.Weapons[id]=snapshot;
                if(o.Stacked){n.Armory.Stacked.Add(id);n.Items.TryGetValue(id,out int held);n.Items[id]=Math.Max(held,o.Ammo);}
                else{n.Armory.Stacked.Remove(id);if(!n.Items.TryGetValue(id,out int count) || count<1)n.Items[id]=1;accepted.Add(o);}changed=true;}
            // One actual native pool may appear on multiple guns. MAX for initial/ambiguous
            // observations; add one observed new pool only during continuous live tracking.
            foreach(var group in accepted.Where(o=>o.Weapon.AmmoType!=0).GroupBy(o=>o.Weapon.AmmoType))
            {string ammo=AmmoId(group.Key);n.Items.TryGetValue(ammo,out int held);int native=group.Max(o=>o.Ammo);long total=data.Armory!=null && group.Any(o=>o.NewAmmo)?(long)held+native:Math.Max(held,native);if(held<0 || total>MaxAmmo)return false;n.Items[ammo]=(int)total;}
            if(!n.Armory.Valid)return false;
            if(changed){save(n);data=n;}return true;
        }
        public static bool SetAmmo(ref InvData data,int type,int amount,Action<InvData> save)
        {
            if(data?.Items==null || data.Armory?.Valid!=true || type==0 || amount<0 || amount>MaxAmmo || save==null || !data.Armory.Weapons.Values.Any(w=>w.AmmoType==type))return false;
            string id=PoolItem(data.Armory,type);data.Items.TryGetValue(id,out int old);if(old==amount)return true;
            var n=Copy(data);n.Items[id]=amount;save(n);data=n;return true;
        }
        public static int ReconcileAmmo(int stored,int previous,int current)
        {
            if(stored<0 || stored>MaxAmmo || previous<0 || previous>MaxAmmo || current<0 || current>MaxAmmo)throw new ArgumentOutOfRangeException();
            return (int)Math.Max(0,Math.Min(MaxAmmo,(long)stored+current-previous));
        }
    }
}
