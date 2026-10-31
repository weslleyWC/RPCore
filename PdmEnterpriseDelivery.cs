using System;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
namespace RPCore
{
    public sealed partial class PdmEnterpriseSystem
    {
        Vehicle autoTruck,autoCar;Ped autoDriver;string autoId;int autoUnit,autoSpawnedAt,autoNext;
        void ReleaseAuto()
        {
            if(Valid(autoCar) && autoCar.Occupants.Length==0)autoCar.Delete();autoCar=null;
            if(Valid(autoDriver))JobActors.Retire(autoDriver);autoDriver=null;
            if(Valid(autoTruck))JobVehicles.Retire(autoTruck);autoTruck=null;autoId=null;
        }
        void AutoTick()
        {
            var op=Data.Auto;if(op==null || !Local){ReleaseAuto();return;}
            if(!op.AtGate || Game.GameTime<autoNext)return;autoNext=Game.GameTime+1000;
            if(autoId!=op.Id || autoUnit!=op.Delivered){ReleaseAuto();autoId=op.Id;autoUnit=op.Delivered;}
            var stock=op.SourcedCar??Wallet.Pdm.Stock.Find(s=>s.Id==op.StockIds[op.Delivered]);if(stock==null)return;
            var truckModel=PdmVehicleModel.Resolve("flatbed");var driverModel=PdmVehicleModel.Resolve("s_m_m_dockwork_01");var carModel=PdmVehicleModel.Resolve(stock.Model);
            try{
                if(!truckModel.IsValid || !driverModel.IsValid || !carModel.IsValid)return;
                if(!PdmStreaming.Ready(truckModel) || !PdmStreaming.Ready(driverModel) || !PdmStreaming.Ready(carModel))return;
                if(!Valid(autoTruck)){
                    var start=new JobPoint{Name="Coletor / aproximacao",X=options.SourceArrival.X+45,Y=options.SourceArrival.Y+25,Z=options.SourceArrival.Z,Heading=options.SourceArrival.Heading};
                    var bay=RigSpawn.FindVehicle(start,truckModel,"PDM coletor");if(bay==null)return;
                    autoTruck=PdmAssets.Track(World.CreateVehicle(truckModel,Pos(bay),bay.Heading));if(!Valid(autoTruck))return;autoTruck.IsPersistent=true;autoTruck.LockStatus=VehicleLockStatus.CannotEnter;autoSpawnedAt=Game.GameTime;autoTruck.AddBlip().Name="PDM / entrega do coletor";return;
                }
                if(!Valid(autoCar)){
                    var anchor=new JobPoint{Name="Coletor / carro",X=autoTruck.Position.X+8,Y=autoTruck.Position.Y,Z=autoTruck.Position.Z,Heading=autoTruck.Heading};var free=RigSpawn.FindVehicle(anchor,carModel,"PDM coletor / carga");if(free==null)return;
                    autoCar=PdmAssets.Track(World.CreateVehicle(carModel,Pos(free),autoTruck.Heading));if(!Valid(autoCar))return;PdmStockPresentation.Apply(autoCar,stock);Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT,autoCar,stock.Plate);autoCar.IsPersistent=true;autoCar.LockStatus=VehicleLockStatus.CannotEnter;
                    autoCar.AttachTo(autoTruck,new Vector3(0,-1.8f,1.2f),Vector3.Zero,false,false,false,false,EulerRotationOrder.XYZ,true,false);autoCar.IsCollisionEnabled=false;return;
                }
                if(!Valid(autoDriver)){
                    autoDriver=PdmAssets.Track(World.CreatePed(driverModel,autoTruck.Position+new Vector3(2,0,0)));if(!Valid(autoDriver))return;autoDriver.IsPersistent=true;autoDriver.BlockPermanentEvents=true;JobActors.Enter(autoDriver,autoTruck,VehicleSeat.Driver);return;
                }
                if(autoTruck.IsDead || autoDriver.IsDead || autoCar.IsDead){if(PdmEnterpriseTransactions.InterruptAuto(Wallet.EnterpriseWallet(),op.Id,(int)Math.Max(0,Math.Min(1000,autoCar.EngineHealth)),(int)Math.Max(0,Math.Min(1000,autoCar.BodyHealth)),op.Id+":interrupted",Wallet.Save,out _)){Tell("Coletor","Entrega interrompida por dano. Carro pago e danos preservados nas encomendas; repare no estoque e retire manualmente ou solicite outra coleta.");ReleaseAuto();}return;}
                if(autoTruck.Driver!=autoDriver){JobActors.Enter(autoDriver,autoTruck,VehicleSeat.Driver);return;}
                if(autoTruck.Position.DistanceTo(Pos(options.SourceArrival))>12){Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE,autoDriver,autoTruck,options.SourceArrival.X,options.SourceArrival.Y,options.SourceArrival.Z,10f,786603,5f);return;}
                if(Game.GameTime-autoSpawnedAt<8000 || autoTruck.Speed>1 || autoCar.Occupants.Length>0)return;
                if(PdmEnterpriseTransactions.ReceiveAuto(Wallet.EnterpriseWallet(),op.Id,op.Id+":receive:"+op.Delivered,Wallet.Save,out bool changed) && changed){Tell("Entrega recebida",stock.Name+" recebido no estoque da empresa; sem novo debito do veiculo.");ReleaseAuto();autoNext=Game.GameTime+5000;}
            }finally{PdmStreaming.Release(truckModel);PdmStreaming.Release(driverModel);PdmStreaming.Release(carModel);}
        }
    }
}
