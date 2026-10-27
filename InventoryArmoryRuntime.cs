using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Native;

namespace RPCore
{
    public partial class InventorySystem
    {
        readonly ShiftGate armoryGate;
        readonly Dictionary<int,int> ammoBaseline=new Dictionary<int,int>();
        readonly HashSet<int> temporaryWeapons=new HashSet<int>();
        bool armoryTracking,armoryApplyPending;
        int armoryScan,armoryError;
        int armoryActor;
        void RegisterWeapons()
        {
            foreach(WeaponHash hash in Enum.GetValues(typeof(WeaponHash)))
            {
                if(hash==WeaponHash.Unarmed)continue;string id=InventoryArmoryRules.WeaponId(unchecked((int)(uint)hash));
                if(!Defs.ContainsKey(id))Defs[id]=new ItemDef{Id=id,Name=hash.ToString(),Icon=hash.ToString().ToLowerInvariant(),Weapon=(uint)hash,Weight=0,Desc="ENTER: equipar / guardar. Municao separada no inventario."};
            }
        }
        void RegisterAmmo()
        {
            if(d.Armory?.Valid!=true)return;
            foreach(string id in d.Armory.Stacked)if(Defs.TryGetValue(id,out var def))def.Desc="ENTER: equipar / guardar. Uma unidade por lancamento real; quantidade acompanha o GTA.";
            foreach(var pair in d.Armory.Weapons)
                if(!Defs.ContainsKey(pair.Key))Defs[pair.Key]=new ItemDef{Id=pair.Key,Name="Arma "+unchecked((uint)pair.Value.Hash).ToString("x8"),Weapon=unchecked((uint)pair.Value.Hash),Weight=0,Desc="ENTER: equipar / guardar. Recurso da arma precisa estar instalado."};
            foreach(var w in d.Armory.Weapons.Where(p=>!d.Armory.Stacked.Contains(p.Key)).Select(p=>p.Value).Where(w=>w.AmmoType!=0))
            {
                string id=InventoryArmoryRules.AmmoId(w.AmmoType);if(Defs.ContainsKey(id))continue;
                string name=((WeaponHash)unchecked((uint)w.Hash)).ToString();string icon="pistolammo";
                var group=(WeaponGroup)Function.Call<uint>(Hash.GET_WEAPONTYPE_GROUP,w.Hash);
                if(group==WeaponGroup.AssaultRifle){name="Rifles";icon="rifleammo";}
                else if(group==WeaponGroup.MG){name="Metralhadoras";icon="rifleammo";}
                else if(group==WeaponGroup.SMG){name="Submetralhadoras";icon="smgammo";}
                else if(group==WeaponGroup.Shotgun){name="Escopetas";icon="shotgunammo";}
                else if(group==WeaponGroup.Sniper){name="Precisao";icon="sniperammo";}
                else if(group==WeaponGroup.Heavy)icon=name.IndexOf("Grenade",StringComparison.OrdinalIgnoreCase)>=0?"glammo":"rocketammo";
                Defs[id]=new ItemDef{Id=id,Name="Municao / "+name,Icon=icon,Weight=.05f,Desc="Reserva compartilhada por armas com este tipo de municao. Nao consome ao equipar; desconta os disparos reais."};
            }
        }
        void ResetArmory(){ammoBaseline.Clear();temporaryWeapons.Clear();armoryTracking=false;armoryApplyPending=true;armoryActor=0;armoryScan=Game.GameTime;armoryError=Game.GameTime;RegisterAmmo();}
        bool ArmorySafe()
        {
            var p=Game.Player.Character;
            return p!=null && p.Exists() && p.IsHuman && !p.IsDead && !Game.IsPaused && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && Game.Player.CanControlCharacter && Profile.Detect()==Profile.Id && InventoryArmoryRules.ActivityAllows(armoryGate?.Owner);
        }
        bool Managed(string id)=>id!=null && (id.StartsWith("weapon_",StringComparison.Ordinal) || id.StartsWith("ammo_",StringComparison.Ordinal));
        void ReportArmory(Exception ex){if(unchecked(Game.GameTime-armoryError)<0)return;armoryError=Game.GameTime+5000;Log.Error("inventory armory",ex);Notify.Show("Armas: sincronizacao pendente. Confira RPCore.log.");}
        // Throwables are stacks of the weapon item, not a gun plus a fictitious bullet pool.
        bool Supported(Weapon w)
        {
            var group=(WeaponGroup)Function.Call<uint>(Hash.GET_WEAPONTYPE_GROUP,(uint)w.Hash);
            return group==WeaponGroup.Melee || group==WeaponGroup.Pistol || group==WeaponGroup.SMG || group==WeaponGroup.AssaultRifle || group==WeaponGroup.MG || group==WeaponGroup.Shotgun || group==WeaponGroup.Sniper || group==WeaponGroup.Stungun || group==WeaponGroup.Heavy || group==WeaponGroup.Thrown;
        }
        CharacterWeapon CaptureWeapon(Weapon w)
        {
            var p=Game.Player.Character;int hash=unchecked((int)(uint)w.Hash);
            var row=new CharacterWeapon{Hash=hash,Tint=(int)w.Tint,Clip=w.AmmoInClip,AmmoType=Function.Call<int>(Hash.GET_PED_AMMO_TYPE_FROM_WEAPON,p,hash)};
            foreach(var c in w.Components)if(c.Active)row.Components.Add(new CharacterAttachment{Hash=unchecked((int)(uint)c.ComponentHash),Tint=Function.Call<int>(Hash.GET_PED_WEAPON_COMPONENT_TINT_INDEX,p,hash,unchecked((int)(uint)c.ComponentHash))});
            return row;
        }
        void ArmoryTick()
        {
            var p=Game.Player.Character;
            if(!ArmorySafe())
            {
                // Do not import a temporary firearm acquired during an activity when it ends.
                if(d.Armory!=null && p!=null && p.Exists() && !p.IsDead && (armoryGate?.Owner!=null && armoryGate.Owner!="needs_sleep" || Game.IsMissionActive))
                    foreach(var w in p.Weapons)if(d.Armory?.Weapons.ContainsKey(InventoryArmoryRules.WeaponId(unchecked((int)(uint)w.Hash)))!=true)temporaryWeapons.Add(unchecked((int)(uint)w.Hash));
                armoryTracking=false;ammoBaseline.Clear();return;
            }
            try
            {
                if(armoryActor!=0 && armoryActor!=p.Handle){armoryTracking=false;ammoBaseline.Clear();temporaryWeapons.Clear();}armoryActor=p.Handle;
                if(d.Armory!=null && !d.Armory.Valid)throw new InvalidOperationException("Armory save invalid; original retained.");
                if(armoryTracking && !armoryApplyPending)TrackAmmo();
                if(!armoryTracking || armoryApplyPending || unchecked(Game.GameTime-armoryScan)>=0)
                {
                    armoryScan=Game.GameTime+500;
                    temporaryWeapons.RemoveWhere(h=>!p.Weapons.HasWeapon((WeaponHash)unchecked((uint)h)));
                    var observations=p.Weapons.Where(w=>Supported(w) && !temporaryWeapons.Contains(unchecked((int)(uint)w.Hash)) && d.Armory?.Weapons.ContainsKey(InventoryArmoryRules.WeaponId(unchecked((int)(uint)w.Hash)))!=true).Select(w=>{var row=CaptureWeapon(w);return new ArmoryObservation{Weapon=row,Ammo=row.AmmoType==0?0:Function.Call<int>(Hash.GET_PED_AMMO_BY_TYPE,p,row.AmmoType),Stacked=(WeaponGroup)Function.Call<uint>(Hash.GET_WEAPONTYPE_GROUP,row.Hash)==WeaponGroup.Thrown};}).ToList();
                    var before=d;
                    if(!InventoryArmoryRules.Import(ref d,observations,n=>Store.Write("Inventory.json",n)))throw new InvalidOperationException("Armory import rejected.");
                    if(!ReferenceEquals(before,d)){armoryApplyPending=true;RegisterAmmo();dirty=true;}
                    if(!armoryTracking || armoryApplyPending)ApplyNativeInventory();
                    armoryTracking=true;
                }
                if(InterfaceOptions.HideWeaponWheel && !p.IsInVehicle() && !Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE,p))
                    foreach(var c in new[]{GTA.Control.SelectWeapon,GTA.Control.SelectPrevWeapon,GTA.Control.SelectNextWeapon,GTA.Control.SelectWeaponAutoRifle,GTA.Control.SelectWeaponHandgun,GTA.Control.SelectWeaponHeavy,GTA.Control.SelectWeaponShotgun,GTA.Control.SelectWeaponSmg,GTA.Control.SelectWeaponSniper,GTA.Control.NextWeapon,GTA.Control.PrevWeapon})Game.DisableControlThisFrame(c);
            }
            catch(Exception ex){Game.DisableControlThisFrame(GTA.Control.Attack);Game.DisableControlThisFrame(GTA.Control.Attack2);ReportArmory(ex);}
        }
        void TrackAmmo()
        {
            var p=Game.Player.Character;
            foreach(int type in ammoBaseline.Keys.ToArray())
            {
                int current=Function.Call<int>(Hash.GET_PED_AMMO_BY_TYPE,p,type);
                // GTA removes a shared ammo pool when its final weapon is removed. Storage/drop handles this explicitly.
                if(!d.Armory.Stacked.Any(id=>d.Armory.Weapons[id].AmmoType==type) && !d.Armory.Weapons.Values.Any(w=>w.AmmoType==type && p.Weapons.HasWeapon((WeaponHash)unchecked((uint)w.Hash))))continue;
                if(current==ammoBaseline[type])continue;
                int next=InventoryArmoryRules.ReconcileAmmo(Count(InventoryArmoryRules.PoolItem(d.Armory,type)),ammoBaseline[type],current);
                if(!InventoryArmoryRules.SetAmmo(ref d,type,next,n=>Store.Write("Inventory.json",n)))throw new InvalidOperationException("Ammo change rejected.");
                ammoBaseline[type]=current;dirty=true;
            }
        }
        void ApplyNativeInventory()
        {
            if(d.Armory?.Valid!=true)return;
            armoryApplyPending=true;var p=Game.Player.Character;
            foreach(var pair in d.Armory.Weapons)
                if(Count(pair.Key)==0 && p.Weapons.HasWeapon((WeaponHash)unchecked((uint)pair.Value.Hash)))p.Weapons.Remove((WeaponHash)unchecked((uint)pair.Value.Hash));
            foreach(int type in d.Armory.Weapons.Values.Select(w=>w.AmmoType).Where(t=>t!=0).Distinct())
            {
                if(!d.Armory.Weapons.Values.Any(w=>w.AmmoType==type && p.Weapons.HasWeapon((WeaponHash)unchecked((uint)w.Hash))))
                {
                    if(d.Armory.Stacked.Any(id=>d.Armory.Weapons[id].AmmoType==type)){Function.Call(Hash.SET_PED_AMMO_BY_TYPE,p,type,0);ammoBaseline[type]=0;}
                    else ammoBaseline.Remove(type);
                    continue;
                }
                int amount=Count(InventoryArmoryRules.PoolItem(d.Armory,type));Function.Call(Hash.SET_PED_AMMO_BY_TYPE,p,type,amount);
                ammoBaseline[type]=Function.Call<int>(Hash.GET_PED_AMMO_BY_TYPE,p,type);
            }
            armoryApplyPending=false;
        }
        bool PrepareArmoryEdit(string id)
        {
            if(!Managed(id))return true;
            var player=Game.Player.Character;
            if(!ArmorySafe() || !armoryTracking || armoryApplyPending || player.IsInVehicle() || Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE,player)){Notify.Show("Armas: aguarde estar a pe, fora de missao/trabalho e com o controle do personagem.");return false;}
            TrackAmmo();
            if(d.Armory?.Weapons.TryGetValue(id,out var row)==true && Game.Player.Character.Weapons.HasWeapon((WeaponHash)unchecked((uint)row.Hash)))
            {
                var next=Newtonsoft.Json.JsonConvert.DeserializeObject<InvData>(Newtonsoft.Json.JsonConvert.SerializeObject(d));
                var snapshot=CaptureWeapon(Game.Player.Character.Weapons[(WeaponHash)unchecked((uint)row.Hash)]);snapshot.Extra=row.Extra;
                foreach(var component in snapshot.Components)component.Extra=row.Components.Find(c=>c.Hash==component.Hash)?.Extra;
                if(snapshot.AmmoType!=0 && snapshot.AmmoType!=row.AmmoType){string ammo=InventoryArmoryRules.AmmoId(snapshot.AmmoType);next.Items.TryGetValue(ammo,out int held);next.Items[ammo]=Math.Max(held,Function.Call<int>(Hash.GET_PED_AMMO_BY_TYPE,Game.Player.Character,snapshot.AmmoType));}
                next.Armory.Weapons[id]=snapshot;Store.Write("Inventory.json",next);d=next;
                if(snapshot.AmmoType!=row.AmmoType)armoryApplyPending=true;
                RegisterAmmo();
            }
            return true;
        }
        void ApplyArmoryEdit(string id){if(!Managed(id))return;try{ApplyNativeInventory();}catch(Exception ex){ReportArmory(ex);}}
        void EquipInventoryWeapon(ItemDef def)
        {
            if(!PrepareArmoryEdit(def.Id) || Count(def.Id)<1 || d.Armory?.Weapons.TryGetValue(def.Id,out var row)!=true)return;
            var p=Game.Player.Character;var hash=(WeaponHash)unchecked((uint)row.Hash);
            if(p.Weapons.Current.Hash==hash){p.Weapons.Select(WeaponHash.Unarmed);return;}
            if(!Function.Call<bool>(Hash.IS_WEAPON_VALID,row.Hash) || row.Tint>=Math.Max(1,Function.Call<int>(Hash.GET_WEAPON_TINT_COUNT,row.Hash)) || row.Components.Any(c=>!Function.Call<bool>(Hash.DOES_WEAPON_TAKE_WEAPON_COMPONENT,row.Hash,c.Hash)))throw new InvalidOperationException("Saved weapon/component resource unavailable.");
            if(!p.Weapons.HasWeapon(hash))
            {
                p.Weapons.Give(hash,0,false,false);
                foreach(var c in row.Components){Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_PED,p,row.Hash,c.Hash);if(c.Tint>=0)Function.Call(Hash.SET_PED_WEAPON_COMPONENT_TINT_INDEX,p,row.Hash,c.Hash,c.Tint);}
                Function.Call(Hash.SET_PED_WEAPON_TINT_INDEX,p,row.Hash,row.Tint);
                if(Function.Call<int>(Hash.GET_PED_AMMO_TYPE_FROM_WEAPON,p,row.Hash)!=row.AmmoType)throw new InvalidOperationException("Saved ammunition type unavailable.");
                // Restoring a stored gun cannot mint ammo, including its old magazine.
                if(row.AmmoType!=0)Function.Call(Hash.SET_AMMO_IN_CLIP,p,row.Hash,Math.Min(row.Clip,Count(InventoryArmoryRules.PoolItem(d.Armory,row.AmmoType))));
            }
            ApplyNativeInventory();p.Weapons.Select(hash);
            grid.Close();menu.Visible=false;Notify.Show(def.Name+" equipada. Use o item novamente para guardar.");
        }
    }
}
