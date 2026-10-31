using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
namespace RPCore
{
    public sealed partial class PdmEnterpriseSystem
    {
        readonly Dictionary<string,Prop> hardware=new Dictionary<string,Prop>();readonly Dictionary<int,Prop> trash=new Dictionary<int,Prop>();readonly List<Ped> visitors=new List<Ped>();
        sealed class Cover{public Vehicle Car;public Prop Prop;public int Opacity;public bool Collision,Frozen;}
        readonly Dictionary<string,Cover> covers=new Dictionary<string,Cover>();
        int cleanerAt;string cleaningBag;int cleaningSince;
        Prop MakeProp(string modelName,JobPoint p,float pitch=0,float roll=0,float yaw=0)
        {
            var model=PdmVehicleModel.Resolve(modelName);try{if(!model.IsValid || !model.IsInCdImage || !PdmStreaming.Ready(model) || !PdmStreaming.ClaimSpawn())return null;var prop=PdmAssets.Track(World.CreateProp(model,Pos(p),false,false));if(!Valid(prop))return null;prop.Rotation=new Vector3(pitch,roll,yaw);prop.IsPositionFrozen=true;prop.IsPersistent=true;return prop;}finally{PdmStreaming.Release(model);}
        }
        void Remove(Prop p){if(Valid(p)){if(Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,p))Function.Call(Hash.DETACH_ENTITY,p,true,true);p.Delete();}}
        void WorldTick()
        {
            if(!Local){ReleaseLocal();return;}
            foreach(var facility in options.Facilities){
                bool wanted=Data.Upgrades.Contains(facility.Id);if(!wanted){if(hardware.TryGetValue(facility.Id,out var old)){Remove(old);hardware.Remove(facility.Id);}continue;}
                if(!hardware.TryGetValue(facility.Id,out var prop) || !Valid(prop)){prop=MakeProp(facility.Model,facility.Point,facility.Pitch,facility.Roll,facility.Yaw);if(Valid(prop))hardware[facility.Id]=prop;}
            }
            foreach(var facility in options.Facilities.Where(f=>Data.Upgrades.Contains(f.Id)))for(int i=0;i<facility.Parts.Count;i++){
                string key=facility.Id+":"+i;var part=facility.Parts[i];if(!hardware.TryGetValue(key,out var prop) || !Valid(prop)){var made=MakeProp(part.Model,part.Point,part.Pitch,part.Roll,part.Yaw);if(Valid(made))hardware[key]=made;}
            }
            string safeModel=Wallet.Pdm.Risk.SafeOpen?"h4_prop_h4_safe_01b":"h4_prop_h4_safe_01a";
            if(hardware.TryGetValue("safe",out var safe) && (!Valid(safe) || safe.Model.Hash!=PdmVehicleModel.Resolve(safeModel).Hash)){Remove(safe);hardware.Remove("safe");}
            if(!hardware.ContainsKey("safe")){var p=new JobPoint{Name="Cofre PDM",X=-27.7943f,Y=-1104.083f,Z=25.4923f};var made=MakeProp(safeModel,p,0,0,-109.5536f);if(Valid(made))hardware["safe"]=made;}
            for(int i=0;i<4;i++){
                if(!Data.TrashSlots.Contains(i) || carryingTrash==i){if(trash.TryGetValue(i,out var prop)){Remove(prop);trash.Remove(i);}continue;}
                if(!trash.TryGetValue(i,out var bag) || !Valid(bag)){var created=MakeProp("p_binbag_01_s",options.Trash[i]);if(Valid(created))trash[i]=created;}
            }
            CoversTick();CleanerTick();AmbientTick();
            for(int batch=0;batch<2 && Data.RentalGeneratedDay<Data.Day && Data.RentalOrders.Count<10;batch++){
                var models=(RentalModels?.Invoke()??new[]{"asea","blista","dominator"}).ToArray();if(models.Length==0)break;int day=Data.RentalGeneratedDay+1;string model=models[day%models.Length];var record=Wallet.Pdm.Stock.Find(v=>string.Equals(v.Model,model,StringComparison.OrdinalIgnoreCase));string id=Guid.NewGuid().ToString("N");
                if(!PdmEnterpriseTransactions.RentalRequest(Wallet.EnterpriseWallet(),new PdmRentalRequest{Id=id,Model=model,Name=record?.Name??model},day,id,Wallet.Save,out _))break;
            }
            if(manualMode==null && Data.Open && PdmEnterpriseRules.Idle(Wallet.Pdm)){
                if(Data.SourceElapsed>=options.SourceSeconds && Data.Auto==null)TrySource();
                if(Data.SellerElapsed>=options.SellerSeconds && Data.Seller==null)CreateSeller();
                if(Data.SaleElapsed>=options.SaleSeconds && Data.Auto==null && Data.Seller==null)TeamSale();
            }
        }
        void CoversTick()
        {
            var shown=(Displays?.Invoke()??Enumerable.Empty<KeyValuePair<PdmStock,Vehicle>>()).ToList();int hour=GTA.Chrono.GameClock.Hour;bool night=hour>=22 || hour<=7 || !Data.Open;
            foreach(var pair in shown){var stock=pair.Key;var car=pair.Value;bool wanted=night && Valid(car) && car.Occupants.Length==0 && ExteriorStock?.Invoke(stock.Id)==true;
                if(wanted && !covers.ContainsKey(stock.Id)){
                    var p=new JobPoint{Name="Capa PDM",X=car.Position.X,Y=car.Position.Y,Z=car.Position.Z,Heading=car.Heading};var prop=MakeProp(car.ClassType==VehicleClass.SUVs?"imp_prop_covered_vehicle_07a":"prop_entityxf_covered",p,0,0,car.Heading);
                    if(Valid(prop)){covers[stock.Id]=new Cover{Car=car,Prop=prop,Opacity=car.Opacity,Collision=car.IsCollisionEnabled,Frozen=car.IsPositionFrozen};car.Opacity=0;car.IsCollisionEnabled=false;car.IsPositionFrozen=true;}
                }
            }
            foreach(var id in covers.Keys.ToArray())if(!night || !shown.Any(p=>p.Key.Id==id) || !Valid(covers[id].Car))Uncover(id);
        }
        void Uncover(string id){if(!covers.TryGetValue(id,out var c))return;Remove(c.Prop);if(Valid(c.Car)){c.Car.Opacity=c.Opacity;c.Car.IsCollisionEnabled=c.Collision;c.Car.IsPositionFrozen=c.Frozen;}covers.Remove(id);}
        void CleanerTick()
        {
            var ped=Cleaner?.Invoke();if(!Valid(ped) || !PdmEnterpriseRules.Paid(Wallet.Pdm,"cleaner") || carryingTrash>=0 || Wallet.Pdm.PremisesService!=null){cleaningBag=null;return;}
            var target=trash.FirstOrDefault(p=>Valid(p.Value));if(target.Value==null)return;string key=target.Key.ToString();
            if(cleaningBag!=key){cleaningBag=key;cleaningSince=0;JobActors.Walk(ped,target.Value.Position);return;}
            if(ped.Position.DistanceTo(target.Value.Position)>2){if(Game.GameTime-cleanerAt>5000){cleanerAt=Game.GameTime;JobActors.Walk(ped,target.Value.Position);}return;}
            if(cleaningSince==0){cleaningSince=Game.GameTime;ped.Task.StartScenarioInPlace("WORLD_HUMAN_JANITOR");}
            if(Game.GameTime-cleaningSince<6000)return;
            if(PdmEnterpriseTransactions.CleanTrash(Wallet.EnterpriseWallet(),Guid.NewGuid().ToString("N"),Wallet.Save,out _,target.Key)){Remove(target.Value);trash.Remove(target.Key);ped.Task.ClearAll();cleaningBag=null;cleaningSince=0;}
        }
        void AmbientTick()
        {
            visitors.RemoveAll(p=>!Valid(p) || p.IsDead);if(!Data.Open){foreach(var p in visitors)if(Valid(p))JobActors.Retire(p);visitors.Clear();return;}
            if(visitors.Count>=2)return;
            var point=JobActors.Sidewalk(new Vector3(-48,-1106,26.4f));if(point==Vector3.Zero || !JobActors.Free(point,2))return;
            var ped=PdmStreaming.Buyer(point,"ambient:"+visitors.Count);if(Valid(ped)){visitors.Add(ped);ped.Task.WanderAround(new Vector3(-48,-1106,26.4f),10);}
        }
        void TeamSale()
        {
            int chance=Math.Min(100,Math.Max(5,35+Data.Reputation*65/100-(100-Data.Cleanliness)/5-(100-Data.Mood)/5));var items=Wallet.Pdm.Stock.Where(s=>!s.AwaitingCollection && !PdmEnterpriseRules.Reserved(Wallet.Pdm,s.Id)).ToList();
            if(items.Count==0)return;string id=Guid.NewGuid().ToString("N");if(PdmRiskTransactions.Roll(id)%101>=chance){Wallet.PdmEnterpriseSaleAttempt(id);return;}
            var stock=items[PdmEnterpriseRules.Seed(id+"car")%items.Count];var car=GetDisplay?.Invoke(stock.Id);if(Valid(car) && car.Occupants.Length>0)return;
            if(!Wallet.Pdm.Showroom.Any(e=>e.StockId==stock.Id)){
                if(PdmEnterpriseTransactions.CustomerOrder(Wallet.EnterpriseWallet(),stock.Id,PdmRiskTransactions.Roll(id+"customer")%company.Customers.Count,id,Wallet.Save,out bool queued) && queued)Tell("Cliente aguardando",stock.Name+": pedido recebido. Abra Contratos para entregar; nenhum valor recebido antecipadamente.");return;
            }
            if(PdmEnterpriseTransactions.AutoSell(Wallet.EnterpriseWallet(),stock.Id,id,Wallet.Save,out bool changed) && changed){if(Valid(car))car.IsPositionFrozen=false;Tell("Venda",stock.Name+" vendido pela equipe. Receita no caixa empresarial.");}
        }
        void ReleaseLocal()
        {
            foreach(var id in covers.Keys.ToArray())Uncover(id);
            foreach(var p in hardware.Values)Remove(p);hardware.Clear();foreach(var p in trash.Values)Remove(p);trash.Clear();
            foreach(var p in visitors)if(Valid(p))JobActors.Retire(p);visitors.Clear();cleaningBag=null;cleaningSince=0;
        }
        void ReleaseWorld(){StopManual();ReleaseSeller();ReleaseAuto();ReleaseLocal();}
    }
}
