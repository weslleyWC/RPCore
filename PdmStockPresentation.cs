using GTA;
namespace RPCore
{
    internal static class PdmStockPresentation
    {
        public static void Apply(Vehicle car,PdmStock stock)
        {
            if(car==null || !PdmAssets.Valid(car) || stock==null)return;
            if(stock.Appearance!=null && stock.Appearance.Model==car.Model.Hash){
                var appearance=Newtonsoft.Json.JsonConvert.DeserializeObject<SavedVehicle>(Newtonsoft.Json.JsonConvert.SerializeObject(stock.Appearance));appearance.Plate=stock.Plate;VehicleSnapshot.Apply(car,appearance);
            }
            car.EngineHealth=stock.Engine;car.BodyHealth=stock.Body;car.DirtLevel=stock.Dirt;
            float capacity=car.PetrolTankVolume;if(capacity>0 && capacity<10000)car.FuelLevel=stock.FuelFraction*capacity;
        }
    }
}
