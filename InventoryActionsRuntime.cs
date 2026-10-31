using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using Newtonsoft.Json;

namespace RPCore
{
    public partial class InventorySystem
    {
        Ped useActor;string useProfile,useDict,useClip;int useRemaining,useLast,useDeadline;
        bool useAnimation,usePropTried,useReopen;Prop useProp;Model useModel;int useToken;
        ConsumptionPresentationPlan usePresentation;int useFallbackAt,useModelIndex;
        const string UseDecor="RPCoreInvUse";
        public bool UsingItem=>useActor!=null;
        public string Describe(ItemDef item)=>Energy(item)>0?(item.Desc??"").Replace("restores hydration only in RPCore.","restaura hidratacao e energia."):item.Desc;
        public float Energy(ItemDef item)=>item.Energy??(InventoryReference.Energy.TryGetValue(item.Id,out float value)?value:0);
        public static bool ReservedShortcut(Keys key)
        {
            return new[]{Cfg.InventoryKey,Cfg.JobKey,Cfg.DumpKey,Cfg.AchKey,Cfg.SeatbeltKey,Cfg.CoreKey,Cfg.StorageKey,Cfg.StocksKey,Cfg.GangsKey,Cfg.RadioKey,Cfg.EngineKey,Cfg.CruiseKey,Cfg.LeftSignalKey,Cfg.RightSignalKey,Cfg.HazardKey,Cfg.VehicleMenuKey,Cfg.FuelKey,Cfg.GarageKey,Cfg.DealerKey,InterfaceOptions.RadialKey,InterfaceOptions.SettingsKey,Visuals.PhoneKey}.Contains(key);
        }
        bool CanUseItem()
        {
            var p=Game.Player.Character;
            return p!=null && p.Exists() && p.IsHuman && !p.IsDead && !p.IsInVehicle() && !Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE,p) && !p.IsRagdoll && !p.IsFalling && !p.IsClimbing && !p.IsSwimming && !Game.IsPaused && !Game.IsMissionActive && !GameplayContext.SceneBlocking && Game.Player.CanControlCharacter && Profile.Detect()==Profile.Id && armoryGate?.Owner==null && !UsingItem && (!pool.AreAnyVisible || menu.Visible) && (!InterfaceOptions.ModalOpen || grid.Visible) && !grid.StorageOpen && retiredUseProps.Count<16;
        }
        public bool TryQuickUse(KeyEventArgs e)
        {
            int slot=InventoryShortcuts.Slot((int)e.KeyCode,e.Modifiers!=Keys.None);
            if(slot<0 || ReservedShortcut(e.KeyCode))return false;
            if(!CanUseItem() || grid.Editing)return true;
            ReconcileLayout();string id=InventoryShortcuts.Item(d,slot);
            if(id!=null)UseSelected(id);return true;
        }
        void BlockNumberControls()
        {
            if(Game.IsKeyPressed(Keys.ShiftKey) || Game.IsKeyPressed(Keys.ControlKey) || Game.IsKeyPressed(Keys.Menu) || Game.IsPaused || Game.IsMissionActive || Profile.Detect()!=Profile.Id)return;
            var p=Game.Player.Character;if(p.IsInVehicle() || !Game.Player.CanControlCharacter || pool.AreAnyVisible || InterfaceOptions.ModalOpen && !grid.Visible || !InventoryArmoryRules.ActivityAllows(armoryGate?.Owner))return;
            if(!Enumerable.Range(49,6).Any(k=>!ReservedShortcut((Keys)k) && Game.IsKeyPressed((Keys)k)))return;
            foreach(var c in new[]{GTA.Control.SelectWeaponUnarmed,GTA.Control.SelectWeaponMelee,GTA.Control.SelectWeaponHandgun,GTA.Control.SelectWeaponShotgun,GTA.Control.SelectWeaponSmg,GTA.Control.SelectWeaponAutoRifle,GTA.Control.SelectWeaponSniper,GTA.Control.SelectWeaponHeavy})Game.DisableControlThisFrame(c);
        }
        void BeginUsing(ItemDef item)
        {
            useReopen=grid.Visible || menu.Visible;grid.Close();menu.Visible=false;
            useActor=Game.Player.Character;useProfile=Profile.Id;useRemaining=6500;useLast=Game.GameTime;useDeadline=Game.GameTime+2500;useToken=(Guid.NewGuid().GetHashCode()&int.MaxValue)|1;usePropTried=false;
            usePresentation=ConsumptionPresentationRules.Resolve(item,Visuals.Bindings.ItemProps);
            bool drink=usePresentation.Drink;
            useDict=drink?"mp_player_intdrink":item.Health>0?"amb@medic@standing@tendtodead@idle_a":"mp_player_inteat@burger";
            useClip=drink?"loop_bottle":item.Health>0?"idle_a":"mp_player_int_eat_burger";
            Function.Call(Hash.REQUEST_ANIM_DICT,useDict);
            useModel=default(Model);useModelIndex=0;useFallbackAt=Game.GameTime+1200;
            if(Visuals.Active && Visuals.UseProps)RequestUseModel();
        }

        void RequestUseModel()
        {
            if(useModel.Hash!=0)useModel.MarkAsNoLongerNeeded();useModel=default(Model);
            while(usePresentation!=null && useModelIndex<usePresentation.Models.Length)
            {
                var model=new Model(usePresentation.Models[useModelIndex++]);if(!model.IsInCdImage || !model.IsValid)continue;useModel=model;useModel.Request();break;
            }
        }
        void UsingTick()
        {
            if(!UsingItem)return;var p=Game.Player.Character;int now=Game.GameTime,delta=NeedsRules.Delta(now,useLast,!Game.IsPaused);useLast=now;
            if(p==null || !p.Exists() || p.Handle!=useActor.Handle || useProfile!=Profile.Id || p.IsDead || p.IsInVehicle() || Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE,p) || p.IsRagdoll || p.IsFalling || p.IsClimbing || p.IsSwimming || p.IsInCombat || !Game.IsPaused && !Game.Player.CanControlCharacter || Game.IsMissionActive || GameplayContext.SceneBlocking || armoryGate!=null && armoryGate.Owner!="inventory_use"){StopUsing();return;}
            if(Game.IsPaused)return;
            useRemaining=Math.Max(0,useRemaining-delta);if(useRemaining==0){StopUsing(true);return;}
            foreach(var c in new[]{GTA.Control.Attack,GTA.Control.Attack2,GTA.Control.Aim,GTA.Control.SelectWeapon,GTA.Control.NextWeapon,GTA.Control.PrevWeapon})Game.DisableControlThisFrame(c);
            if(unchecked(now-useDeadline)<0)
            {
                if(!usePropTried && Visuals.Active && Visuals.UseProps && !useModel.IsLoaded && usePresentation!=null && useModelIndex<usePresentation.Models.Length && unchecked(now-useFallbackAt)>=0){RequestUseModel();useFallbackAt=now+600;}
                if(!useAnimation && Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED,useDict)){p.Task.PlayAnimation(useDict,useClip,4,-4,useRemaining,(AnimationFlags)49,0);useAnimation=true;}
                if(!usePropTried && Visuals.Active && Visuals.UseProps && useModel.IsLoaded && PrepareUseDecor())
                {
                    usePropTried=true;
                    var prop=World.CreateProp(useModel,p.Position,false,false);
                    if(prop!=null && prop.Exists())
                    {
                        if(Function.Call<bool>(Hash.DECOR_SET_INT,prop,UseDecor,useToken)){useProp=prop;activeUseProp=new UsePropLease{Handle=prop.Handle,Token=useToken,Model=useModel.Hash};prop.IsPersistent=true;prop.IsCollisionEnabled=false;var pose=usePresentation.Binding;prop.AttachTo(p.Bones[(Bone)usePresentation.Bone],new Vector3(pose.X,pose.Y,pose.Z),new Vector3(pose.RX,pose.RY,pose.RZ));}else { prop.Delete(); Log.Write("item prop ownership registration rejected"); }
                    }
                }
            }
        }
        void StopUsing(bool completed=false)
        {
            bool reopen=completed && useReopen && useProfile==Profile.Id && useActor!=null && useActor.Exists() && Game.Player.Character?.Handle==useActor.Handle;
            try{if(useActor!=null && useActor.Exists() && useAnimation && Game.Player.Character?.Handle==useActor.Handle && useProfile==Profile.Id)Function.Call(Hash.STOP_ANIM_TASK,useActor,useDict,useClip,-4f);}catch(Exception ex){Log.Error("item animation cleanup",ex);}
            try{RetireUseProp();}catch(Exception ex){Log.Error("item prop cleanup",ex);}
            try{if(useModel.Hash!=0)useModel.MarkAsNoLongerNeeded();if(useDict!=null)Function.Call(Hash.REMOVE_ANIM_DICT,useDict);}catch(Exception ex){Log.Error("item model cleanup",ex);}
            useActor=null;useProfile=null;useDict=null;useClip=null;useAnimation=false;useRemaining=0;useModel=default(Model);usePresentation=null;
            armoryGate?.Release("inventory_use");
            useReopen=false;
            if(reopen && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen){if(InterfaceOptions.GridInventory)grid.Open();else{Rebuild();menu.Visible=true;}}
        }
        public InventoryRecipe Recipe(IEnumerable<string> selection)=>Recipes.FirstOrDefault(r=>PersonalCrafting.Matches(r,selection));
        public int CraftMaximum(InventoryRecipe recipe)=>PersonalCrafting.Maximum(d,recipe);
        public bool CraftSaved(InventoryRecipe recipe,int batches)
        {
            if(!CanUseItem() || !Recipes.Contains(recipe))return false;
            try{bool done=PersonalCrafting.Craft(ref d,recipe,batches,Cfg.MaxWeight,id=>Defs.TryGetValue(id,out var item)?item.Weight:.1f,n=>Store.Write("Inventory.json",n));dirty|=done;if(done)Notify.Show(recipe.Name+" x"+batches);return done;}
            catch(Exception ex){Log.Error("personal crafting",ex);Notify.Show("Fabricacao nao gravada; materiais preservados.");return false;}
        }
        public int PurchaseMaximum(ItemDef item)=>item==null?0:ShopCatalogRules.Maximum(item,Count(item.Id),InventoryTransfers.Weight(d.Items,id=>Defs.TryGetValue(id,out var def)?def.Weight:.1f),Cfg.MaxWeight,Wallet.Cash);
        public bool BuySaved(ItemDef item,int count=1)
        {
            try{bool done=InventoryPurchases.Buy(ref d,item,count,Cfg.MaxWeight,id=>Defs.TryGetValue(id,out var def)?def.Weight:.1f,()=>Wallet.Cash,Wallet.Debit,Wallet.Credit,Wallet.Save,n=>Store.Write("Inventory.json",n));dirty|=done;return done;}
            catch(Exception ex){Log.Error("inventory purchase",ex);Notify.Show("Compra nao concluida. Confira saldo/inventario e RPCore.log.");return false;}
        }
        public void OpenStorageView(string label,float capacity,Func<Dictionary<string,int>> contents,Func<string,bool,int,bool> transfer,Func<bool> access,Action closed,Func<string,bool,int> fitting=null)
        {ReconcileLayout();grid.OpenStorage(label,capacity,contents,transfer,access,closed,fitting);}
        public void CloseStorageView(){if(grid.StorageOpen)grid.Close();}
    }
}
