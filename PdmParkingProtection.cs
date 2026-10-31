using GTA.Native;
namespace RPCore
{
    // Local prevention, without cancelling existing parking tasks, deleting vehicles or changing road traffic.
    internal sealed class PdmParkingBackend : IPdmParkingBackend
    {
        public int BlockVehicleScenarios(PdmParkingRegion r)=>Function.Call<int>(Hash.ADD_SCENARIO_BLOCKING_AREA,r.MinX,r.MinY,r.MinZ,r.MaxX,r.MaxY,r.MaxZ,false,false,false,true);
        public void RemoveBlock(int id)=>Function.Call(Hash.REMOVE_SCENARIO_BLOCKING_AREA,id,false);
        public void SetGenerators(PdmParkingRegion r,bool active)
        {
            Function.Call(Hash.SET_ALL_VEHICLE_GENERATORS_ACTIVE_IN_AREA,r.MinX,r.MinY,r.MinZ,r.MaxX,r.MaxY,r.MaxZ,active,false);
            Function.Call(Hash.SET_ALL_VEHICLE_GENERATORS_ACTIVE_IN_AREA,r.MinX,r.MinY,r.MinZ,r.MaxX,r.MaxY,r.MaxZ,active,true);
        }
    }
}
