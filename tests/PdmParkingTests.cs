using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class PdmParkingTests
    {
        sealed class Backend:IPdmParkingBackend
        {
            public List<int> Removed=new List<int>();public List<Tuple<string,bool>> Generators=new List<Tuple<string,bool>>();public int Allocated;public bool FailRemove;
            public int BlockVehicleScenarios(PdmParkingRegion r)=>Allocated++;
            public void RemoveBlock(int id){if(FailRemove){FailRemove=false;throw new InvalidOperationException();}Removed.Add(id);}
            public void SetGenerators(PdmParkingRegion r,bool active)=>Generators.Add(Tuple.Create(r.Id,active));
        }
        static PdmParkingRegion Area(string id="one",float x=0)=>new PdmParkingRegion{Id=id,MinX=x,MaxX=x+3,MinY=0,MaxY=6,MinZ=0,MaxZ=4};
        static PdmShowroomOptions Options(float heading=0)=>new PdmShowroomOptions{Slots=new List<PdmShowroomSlot>{new PdmShowroomSlot{Id="outside",Name="Outside",Point=new JobPoint{Name="Outside",X=10,Y=20,Z=30,Heading=heading}}}};
        [Fact] public void OnlyEnabledExteriorSlotsOfOwnedValidCompanyAreProtected()
        {
            var c=PdmShowroomDefaults.Load();var d=new PdmData{Owned=true};var regions=PdmParkingRules.Regions(c,d);
            Assert.Equal(c.Slots.Count(s=>s.Enabled && !s.Interior),regions.Count);Assert.All(regions,r=>Assert.Contains(c.Slots,s=>s.Id==r.Id && !s.Interior));Assert.Empty(PdmParkingRules.Regions(c,new PdmData()));c.Slots.ForEach(s=>s.Enabled=false);Assert.Empty(PdmParkingRules.Regions(c,d));Assert.Empty(PdmParkingRules.Regions(null,d));d.Stock=null;Assert.Empty(PdmParkingRules.Regions(Options(),d));
        }
        [Theory] [InlineData(0,1.6f,3.2f)] [InlineData(90,3.2f,1.6f)] [InlineData(180,1.6f,3.2f)] [InlineData(270,3.2f,1.6f)]
        public void ProtectionFollowsSlotHeadingAndStaysWithinParkingFootprint(float heading,float dx,float dy)
        {
            var r=Assert.Single(PdmParkingRules.Regions(Options(heading),new PdmData{Owned=true}));Assert.InRange(r.MinX,10-dx-.001,10-dx+.001);Assert.InRange(r.MaxX,10+dx-.001,10+dx+.001);Assert.InRange(r.MinY,20-dy-.001,20-dy+.001);Assert.InRange(r.MaxY,20+dy-.001,20+dy+.001);Assert.Equal(29,r.MinZ);Assert.Equal(33,r.MaxZ);
        }
        [Fact] public void ProtectionTracksSavedShowroomPositionInsteadOfBlockingOriginalEmptyLocation()
        {
            var d=new PdmData{Owned=true};d.Stock.Add(new PdmStock{Id="stock",Model="asea",Name="Asea",Plate="ONE",Cost=1000,Price=2000});d.Showroom.Add(new PdmShowroomEntry{StockId="stock",SlotId="outside",OffsetX=2,OffsetY=-1,OffsetZ=.5f,OffsetHeading=90});var r=Assert.Single(PdmParkingRules.Regions(Options(),d));Assert.InRange(r.MinX,8.799,8.801);Assert.InRange(r.MaxY,20.599,20.601);Assert.Equal(29.5f,r.MinZ);Assert.Equal(33.5f,r.MaxZ);
        }
        [Fact] public void RepeatedFramesDoNotDuplicateBlocksAndLeavingRestoresOnlyOwnedRegions()
        {
            var backend=new Backend();var leases=new PdmParkingLeases(backend);leases.Update(new[]{Area(),Area("two",20)});leases.Update(new[]{Area(),Area("two",20)});Assert.Equal(2,backend.Allocated);Assert.Empty(backend.Removed);leases.Clear();Assert.Equal(new[]{0,1},backend.Removed.OrderBy(i=>i));Assert.Equal(2,backend.Generators.Count(p=>p.Item2));leases.Clear();Assert.Equal(2,backend.Removed.Count);
        }
        [Fact] public void AdjustedSlotReleasesOriginalBlockAndRegistersNewRegionOnce()
        {
            var backend=new Backend();var leases=new PdmParkingLeases(backend);leases.Update(new[]{Area()});leases.Update(new[]{Area(x:2)});Assert.Equal(2,backend.Allocated);Assert.Equal(new[]{0},backend.Removed);Assert.Single(backend.Generators,p=>p.Item2);leases.Update(Array.Empty<PdmParkingRegion>());Assert.Equal(new[]{0,1},backend.Removed);Assert.Equal(2,backend.Generators.Count(p=>p.Item2));
        }
        [Fact] public void CleanupFailureDoesNotLoseHandleAndStillReleasesOtherSlots()
        {
            var backend=new Backend();var leases=new PdmParkingLeases(backend);leases.Update(new[]{Area(),Area("two",20)});backend.FailRemove=true;Assert.Throws<InvalidOperationException>(()=>leases.Clear());Assert.Single(backend.Removed);leases.Clear();Assert.Equal(new[]{0,1},backend.Removed.OrderBy(i=>i));Assert.Equal(2,backend.Generators.Count(p=>p.Item2));
        }
    }
}
