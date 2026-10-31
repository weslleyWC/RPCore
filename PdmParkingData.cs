using System;
using System.Collections.Generic;
using System.Linq;
namespace RPCore
{
    public sealed class PdmParkingRegion
    {
        public string Id;public float MinX,MinY,MinZ,MaxX,MaxY,MaxZ;
        public bool Same(PdmParkingRegion other)=>other!=null && Id==other.Id && MinX==other.MinX && MinY==other.MinY && MinZ==other.MinZ && MaxX==other.MaxX && MaxY==other.MaxY && MaxZ==other.MaxZ;
    }
    public static class PdmParkingRules
    {
        public static List<PdmParkingRegion> Regions(PdmShowroomOptions options,PdmData data)
        {
            if(options?.Valid!=true || data?.Valid!=true || !data.Owned)return new List<PdmParkingRegion>();
            return options.Slots.Where(s=>s.Enabled && !s.Interior).Select(slot=>{
                var entry=data.Showroom.Find(e=>e.SlotId==slot.Id);
                float x=slot.Point.X+(entry?.OffsetX??0),y=slot.Point.Y+(entry?.OffsetY??0),z=slot.Point.Z+(entry?.OffsetZ??0);
                double angle=(slot.Point.Heading+(entry?.OffsetHeading??0))*Math.PI/180;
                float width=(float)(Math.Abs(Math.Cos(angle))*1.6+Math.Abs(Math.Sin(angle))*3.2),length=(float)(Math.Abs(Math.Sin(angle))*1.6+Math.Abs(Math.Cos(angle))*3.2);
                return new PdmParkingRegion{Id=slot.Id,MinX=x-width,MaxX=x+width,MinY=y-length,MaxY=y+length,MinZ=z-1,MaxZ=z+3};
            }).ToList();
        }
    }
    public interface IPdmParkingBackend
    {
        int BlockVehicleScenarios(PdmParkingRegion region);
        void RemoveBlock(int id);
        void SetGenerators(PdmParkingRegion region,bool active);
    }
    // Own only the scenario handles allocated here. Never remove another script's blocking areas.
    public sealed class PdmParkingLeases
    {
        sealed class Lease {public PdmParkingRegion Region;public int Block=-1;public bool GeneratorsDisabled;}
        readonly IPdmParkingBackend backend;readonly Dictionary<string,Lease> owned=new Dictionary<string,Lease>();
        public PdmParkingLeases(IPdmParkingBackend backend){this.backend=backend??throw new ArgumentNullException(nameof(backend));}
        public void Update(IEnumerable<PdmParkingRegion> regions)
        {
            var wanted=regions.ToDictionary(r=>r.Id);
            foreach(var id in owned.Keys.ToArray())if(!wanted.TryGetValue(id,out var region) || !owned[id].Region.Same(region))Release(id);
            foreach(var region in wanted.Values){
                if(!owned.TryGetValue(region.Id,out var lease)){lease=new Lease{Region=region};owned.Add(region.Id,lease);}
                if(lease.Block<0)lease.Block=backend.BlockVehicleScenarios(region);
                backend.SetGenerators(region,false);lease.GeneratorsDisabled=true;
            }
        }
        void Release(string id)
        {
            var lease=owned[id];
            if(lease.Block>=0){backend.RemoveBlock(lease.Block);lease.Block=-1;}
            if(lease.GeneratorsDisabled){backend.SetGenerators(lease.Region,true);lease.GeneratorsDisabled=false;}
            owned.Remove(id);
        }
        public void Clear()
        {
            Exception failure=null;foreach(var id in owned.Keys.ToArray())try{Release(id);}catch(Exception ex){failure=ex;}
            if(failure!=null)throw failure;
        }
    }
}
