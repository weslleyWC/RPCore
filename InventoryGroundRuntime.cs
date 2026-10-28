using System;
using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;
namespace RPCore
{
    public partial class InventorySystem
    {
        bool GroundSafe()
        {
            var p=Game.Player.Character;
            return p!=null && p.Exists() && p.IsHuman && !p.IsDead && p.IsOnFoot && !p.IsInVehicle() && !Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE,p) && !p.IsFalling && !Function.Call<bool>(Hash.IS_ENTITY_IN_AIR,p) && !p.IsRagdoll && !p.IsSwimming && !p.IsClimbing && !p.IsInCombat && !Game.IsPaused && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && Game.Player.CanControlCharacter && Profile.Detect()==Profile.Id && armoryGate?.Owner==null && !UsingItem && !pool.AreAnyVisible && (!InterfaceOptions.ModalOpen || grid.Visible) && !grid.StorageOpen;
        }
        internal bool DropOnGround(string id,int quantity)
        {
            if(!GroundSafe()){Notify.Show("Largue itens a pe, em local seguro e fora de atividades.");return false;}
            try
            {
                if(!PrepareArmoryEdit(id))return false;
                var p=Game.Player.Character;var pos=p.Position+p.ForwardVector*.8f-new Vector3(0,0,.9f);
                bool done=InventoryGroundRules.Drop(ref d,id,quantity,pos.X,pos.Y,pos.Z,Function.Call<int>(Hash.GET_INTERIOR_FROM_ENTITY,p),n=>Store.Write("Inventory.json",n));
                if(done){dirty=true;ApplyArmoryEdit(id);}else Notify.Show("Item nao largado. Confira quantidade, limite de 128 pilhas e o save.");return done;
            }
            catch(Exception ex){Log.Error("inventory ground drop",ex);Notify.Show("Nao foi possivel gravar; confira o inventario e RPCore.log.");return false;}
        }
        internal void PickupGround(bool stack)
        {
            if(!GroundSafe())return;
            try
            {
                var p=Game.Player.Character;var pos=p.Position;var row=InventoryGroundRules.Nearest(d,pos.X,pos.Y,pos.Z,Function.Call<int>(Hash.GET_INTERIOR_FROM_ENTITY,p),1.8f);
                if(row==null){Notify.Show("Nenhum item ao alcance.");return;}
                if(!Defs.ContainsKey(row.Item)){Notify.Show("Definicao ausente; item conservado no chao.");return;}
                if(!PrepareArmoryEdit(row.Item))return;
                int amount=InventoryGroundRules.Fits(d,row.Id,Cfg.MaxWeight,id=>Defs.TryGetValue(id,out var def)?def.Weight:.1f);if(!stack)amount=Math.Min(1,amount);
                if(amount==0){Notify.Show("Sem capacidade para recolher este item.");return;}
                if(InventoryGroundRules.Pickup(ref d,row.Id,amount,Cfg.MaxWeight,id=>Defs.TryGetValue(id,out var def)?def.Weight:.1f,n=>Store.Write("Inventory.json",n)))
                {dirty=true;ApplyArmoryEdit(row.Item);Notify.Show("Recolhido: "+Defs[row.Item].Name+" x"+amount);}
            }
            catch(Exception ex){Log.Error("inventory ground pickup",ex);Notify.Show("Recolhimento nao confirmado. Confira RPCore.log.");}
        }
        void GroundTick()
        {
            if(!GroundSafe() || !InventoryGroundRules.Valid(d) || d.GroundItems==null)return;
            var p=Game.Player.Character;int interior=Function.Call<int>(Hash.GET_INTERIOR_FROM_ENTITY,p);
            foreach(var row in d.GroundItems)
            {
                var pos=new Vector3(row.X,row.Y,row.Z);if(row.Interior!=interior || p.Position.DistanceToSquared(pos)>900)continue;
                World.DrawMarker(MarkerType.Cylinder,pos,Vector3.Zero,Vector3.Zero,new Vector3(.35f,.35f,.12f),Color.FromArgb(160,65,180,255));
            }
            var near=InventoryGroundRules.Nearest(d,p.Position.X,p.Position.Y,p.Position.Z,interior,1.8f);
            if(near!=null)GTA.UI.Screen.ShowHelpTextThisFrame((Defs.TryGetValue(near.Item,out var def)?def.Name:near.Item)+" x"+near.Quantity+"~n~Abra o inventario ("+Cfg.InventoryKey+") e pressione R para recolher 1; SHIFT+R: o que couber.");
        }
    }
}
