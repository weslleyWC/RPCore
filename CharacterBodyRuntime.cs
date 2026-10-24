using System;
using System.Linq;
using GTA;
using GTA.Native;
namespace RPCore
{
    static class CharacterBodyRuntime
    {
        public static CharacterBody Capture(Ped p)
        {
            var body=new CharacterBody{Model=p.Model.Hash,Health=p.Health,MaxHealth=p.MaxHealth,Armor=p.Armor,SelectedWeapon=unchecked((int)(uint)p.Weapons.Current.Hash)};
            for(int i=0;i<12;i++)body.Components.Add(new WardrobePiece{Slot=i,Drawable=Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION,p,i),Texture=Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION,p,i),Palette=Function.Call<int>(Hash.GET_PED_PALETTE_VARIATION,p,i)});
            foreach(int i in WardrobeRules.PropSlots){int draw=Function.Call<int>(Hash.GET_PED_PROP_INDEX,p,i);body.Props.Add(new WardrobePiece{Slot=i,Drawable=draw,Texture=draw<0?0:Function.Call<int>(Hash.GET_PED_PROP_TEXTURE_INDEX,p,i)});}
            foreach(var weapon in p.Weapons){
                if(weapon.Hash==WeaponHash.Unarmed)continue;int hash=unchecked((int)(uint)weapon.Hash);
                var row=new CharacterWeapon{Hash=hash,Tint=(int)weapon.Tint,Clip=weapon.AmmoInClip,AmmoType=Function.Call<int>(Hash.GET_PED_AMMO_TYPE_FROM_WEAPON,p,hash)};
                foreach(var component in weapon.Components)if(component.Active)row.Components.Add(new CharacterAttachment{Hash=unchecked((int)(uint)component.ComponentHash),Tint=Function.Call<int>(Hash.GET_PED_WEAPON_COMPONENT_TINT_INDEX,p,hash,unchecked((int)(uint)component.ComponentHash))});
                body.Weapons.Add(row);if(row.AmmoType!=0)body.Ammo[row.AmmoType]=Function.Call<int>(Hash.GET_PED_AMMO_BY_TYPE,p,row.AmmoType);
            }
            if(!body.Valid)throw new InvalidOperationException("Current body/equipment cannot be safely recorded.");return body;
        }
        public static void Apply(Ped p,CharacterBody body)
        {
            if(body==null || !body.Valid || p.Model.Hash!=body.Model)throw new InvalidOperationException("Character body/model mismatch.");
            // Validate every resource before touching clothing or inventory. Missing mod resources keep the pending snapshot.
            foreach(var c in body.Components){int d=Function.Call<int>(Hash.GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS,p,c.Slot),t=Function.Call<int>(Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS,p,c.Slot,c.Drawable);if(c.Drawable>=Math.Max(1,d) || c.Texture>=Math.Max(1,t))throw new InvalidOperationException("Saved component resource unavailable.");}
            foreach(var c in body.Props)if(c.Drawable>=0 && (c.Drawable>=Function.Call<int>(Hash.GET_NUMBER_OF_PED_PROP_DRAWABLE_VARIATIONS,p,c.Slot) || c.Texture>=Function.Call<int>(Hash.GET_NUMBER_OF_PED_PROP_TEXTURE_VARIATIONS,p,c.Slot,c.Drawable)))throw new InvalidOperationException("Saved prop resource unavailable.");
            foreach(var w in body.Weapons){if(!Function.Call<bool>(Hash.IS_WEAPON_VALID,w.Hash) || w.Tint>=Math.Max(1,Function.Call<int>(Hash.GET_WEAPON_TINT_COUNT,w.Hash)) || w.Components.Any(c=>!Function.Call<bool>(Hash.DOES_WEAPON_TAKE_WEAPON_COMPONENT,w.Hash,c.Hash)))throw new InvalidOperationException("Saved weapon resource unavailable.");}
            foreach(var c in body.Components)Function.Call(Hash.SET_PED_COMPONENT_VARIATION,p,c.Slot,c.Drawable,c.Texture,c.Palette);
            foreach(var c in body.Props)if(c.Drawable<0)Function.Call(Hash.CLEAR_PED_PROP,p,c.Slot);else Function.Call(Hash.SET_PED_PROP_INDEX,p,c.Slot,c.Drawable,c.Texture,true);
            Function.Call(Hash.REMOVE_ALL_PED_WEAPONS,p,true);
            foreach(var w in body.Weapons){
                Function.Call(Hash.GIVE_WEAPON_TO_PED,p,w.Hash,0,false,false);
                foreach(var c in w.Components){Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_PED,p,w.Hash,c.Hash);if(c.Tint>=0)Function.Call(Hash.SET_PED_WEAPON_COMPONENT_TINT_INDEX,p,w.Hash,c.Hash,c.Tint);}
                Function.Call(Hash.SET_PED_WEAPON_TINT_INDEX,p,w.Hash,w.Tint);
                if(Function.Call<int>(Hash.GET_PED_AMMO_TYPE_FROM_WEAPON,p,w.Hash)!=w.AmmoType)throw new InvalidOperationException("Weapon ammo type changed.");
                if(w.AmmoType!=0 && w.Clip>0 && !Function.Call<bool>(Hash.SET_AMMO_IN_CLIP,p,w.Hash,w.Clip))throw new InvalidOperationException("Weapon clip not restored.");
            }
            foreach(var a in body.Ammo)Function.Call(Hash.SET_PED_AMMO_BY_TYPE,p,a.Key,a.Value);
            Function.Call(Hash.SET_CURRENT_PED_WEAPON,p,body.SelectedWeapon,true);p.MaxHealth=body.MaxHealth;p.Health=body.Health;p.Armor=body.Armor;
            if(!CharacterRules.Restored(body,Capture(p)))throw new InvalidOperationException("Character body/equipment readback differs from snapshot; pending preserved.");
        }
    }
}
