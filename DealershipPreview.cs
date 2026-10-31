using System;
using GTA;
using GTA.Math;
using GTA.Native;
namespace RPCore
{
    public sealed class DealershipPreview
    {
        const string Owner="dealership_preview";
        readonly ShiftGate gate;
        readonly Vector3 spot=new Vector3(-55.21414f,-1111.252f,26.43579f);
        Model model;Vehicle car;Camera camera;long carAddress;int carHandle,carModel;
        Vector3 origin;DealershipPreviewTicket ticket;bool requested,rendering,lease;
        public bool Active=>ticket!=null;
        public bool Displayed=>ValidCar && camera!=null && camera.Exists();
        bool ValidCar=>car!=null && car.Exists() && car.Handle==carHandle && car.Model.Hash==carModel && carAddress!=0 && car.MemoryAddress.ToInt64()==carAddress;
        public DealershipPreview(ShiftGate gate){this.gate=gate;}
        public bool Begin(VehicleOffer offer)
        {
            Close();
            if(!PersonalServiceMenu.SafeFoot || InterfaceOptions.ModalOpen || !Function.Call<bool>(Hash.IS_GAMEPLAY_CAM_RENDERING)){
                Notify.Show("Feche a outra camera/atividade antes de visualizar.");return false;
            }
            model=new Model(offer.Model);
            if(!model.IsValid || !model.IsInCdImage || !model.IsVehicle || !(model.IsCar || model.IsBike || model.IsQuadBike) || model.IsBicycle){Notify.Show("Modelo indisponivel para visualizacao. Nada cobrado.");return false;}
            if(!gate.Acquire(Owner)){Notify.Show("Encerre a atividade de "+gate.Owner+" antes de visualizar.");return false;}
            lease=true;
            try{
                var ped=Game.Player.Character;origin=ped.Position;
                ticket=new DealershipPreviewTicket(ped.Handle,ped.Model.Hash,ped.MemoryAddress.ToInt64(),Profile.Id,Game.GameTime);
                Function.Call(Hash.REQUEST_MODEL,model.Hash);requested=true;return true;
            }catch{Close();throw;}
        }
        public void Tick(bool visible,bool allowed)
        {
            if(!Active)return;
            try{
                var ped=Game.Player.Character;var delta=ped.Position-origin;
                if(!visible || !allowed || gate.Owner!=Owner || !ticket.Current(ped.Handle,ped.Model.Hash,ped.MemoryAddress.ToInt64(),Profile.Id,Game.GameTime,car!=null,delta.LengthSquared())){Close();return;}
                if(car!=null){
                    if(!ValidCar || ped.IsInCombat || camera==null || !camera.Exists() || ScriptCameraDirector.RenderingCam?.Handle!=camera.Handle){Close();return;}
                    return;
                }
                if(!Function.Call<bool>(Hash.IS_GAMEPLAY_CAM_RENDERING)){Close();return;}
                if(!model.IsLoaded || !GameplayContext.TerrainReady)return;
                int interior=Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS,spot.X,spot.Y,spot.Z);
                if(interior==0 || !Function.Call<bool>(Hash.IS_INTERIOR_READY,interior))return;
                using(var min=new OutputArgument())using(var max=new OutputArgument()){
                    Function.Call(Hash.GET_MODEL_DIMENSIONS,model.Hash,min,max);var size=max.GetResult<Vector3>()-min.GetResult<Vector3>();
                    if(!DealershipPreviewTicket.Fits(size.X,size.Y,size.Z)){Close();Notify.Show("Modelo grande demais para a vaga do showroom. Nada cobrado.");return;}
                }
                if(World.GetNearbyVehicles(spot,4).Length!=0 || World.GetNearbyPeds(spot,2).Length!=0){Close();Notify.Show("Vaga de demonstracao ocupada. Nenhum veiculo foi removido; nada cobrado.");return;}
                car=World.CreateVehicle(model,spot,78.40496f);
                if(car==null || !car.Exists())throw new InvalidOperationException("Showroom preview creation failed.");
                carHandle=car.Handle;carModel=car.Model.Hash;carAddress=car.MemoryAddress.ToInt64();
                if(carAddress==0)throw new InvalidOperationException("Showroom preview identity unavailable.");
                car.IsPersistent=true;car.LockStatus=VehicleLockStatus.CannotEnter;car.IsInvincible=true;car.CanBeVisiblyDamaged=false;
                car.PlaceOnGround();car.IsPositionFrozen=true;car.DirtLevel=0;
                Function.Call(Hash.SET_VEHICLE_ENGINE_ON,car,false,true,false);
                camera=Camera.Create(ScriptedCameraNameHash.DefaultScriptedCamera,true);
                if(camera==null || !camera.Exists())throw new InvalidOperationException("Showroom preview camera unavailable.");
                camera.Position=new Vector3(-51.17035f,-1110.218f,26.73582f);camera.FieldOfView=65;camera.PointAt(spot);
                rendering=true;ScriptCameraDirector.StartRendering();
                ReleaseModel();Notify.Show("Demonstracao: carro temporario, sem cobranca. Use Girar ou Voltar.");
            }catch(Exception ex){Close();Log.Error("dealership preview",ex);Notify.Show("Visualizacao interrompida; nenhuma compra realizada.");}
        }
        public void Rotate(){if(Displayed)car.Heading=(car.Heading+45)%360;}
        void ReleaseModel(){if(!requested)return;requested=false;model.MarkAsNoLongerNeeded();}
        public void Close()
        {
            ticket=null;
            try{
                var current=ScriptCameraDirector.RenderingCam;
                if(rendering && (current==null || current.Handle==camera?.Handle))ScriptCameraDirector.StopRendering();
            }finally{
                try{if(camera!=null && camera.Exists())camera.Delete();}
                finally{
                    camera=null;rendering=false;
                    try{if(ValidCar){if(car.Occupants.Length==0 && !Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,car))car.Delete();else{car.IsPositionFrozen=false;car.IsInvincible=false;car.CanBeVisiblyDamaged=true;car.LockStatus=VehicleLockStatus.Unlocked;car.MarkAsNoLongerNeeded();}}}
                    finally{car=null;carAddress=0;carHandle=carModel=0;try{ReleaseModel();}finally{if(lease){lease=false;gate.Release(Owner);}}}
                }
            }
        }
    }
}
