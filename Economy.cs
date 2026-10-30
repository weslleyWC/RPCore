using System;
using System.Collections.Generic;

namespace RPCore
{
    public class WalletData : ExtensibleData
    {
        public int RoleplayVersion;
        public int Cash;
        public int Bank;
        public NativeCashCheckpoint NativeCash;
        public Portfolio Exchange;
        public PassengerData Passenger;
        public HalloweenData Halloween;
        public StudioHeistData StudioHeist;
        public TrafficData Traffic;
        public CustodyData Custody;
        public TransportData Transport;
        public JobLedger Jobs;
        public HuntingData Hunting;
        public FishingData Fishing;
        public CraftingData Crafting;
        public DealerData Dealer;
        public StreamerData Streamer;
        public DragData Drag;
        public PdmData Pdm;
        public HousingData Housing;
        public WardrobeData Wardrobe;
        public BarberData Barber;
        public GroomingData Grooming;
        public VehicleInsuranceData VehicleInsurance;
        public DmvData Dmv;
        public DmvData DmvM1;
        public DmvData DmvA;
        public GangTreasuryData GangTreasury;
        public GangBusinessData GangBusiness;
        public GangIllicitData GangIllicit;
        public GangWarehouseData GangWarehouse;
        public GangRaidData GangRaid;
        public GangDiplomacyData GangDiplomacy;
        public GangBulkData GangBulk;
        public GangArmsData GangArms;
        public GangAutoArmsData GangAutoArms;
        public GangAutoLaunderData GangAutoLaunder;
        public GangCafeData GangCafe;
        public GangPromotionData GangPromotion;
        public GangMechanicData GangMechanic;
        public GangTowData GangTow;
        public GangCashRunData GangCashRun;
        public GangMedicalData GangMedical;
        public GangPopUpData GangPopUp;
        public GangMowingData GangMowing;
        public GangTheatreData GangTheatre;
        public GangChauffeurData GangChauffeur;
        public GangDockData GangDock;
        public GangBmxData GangBmx;
        public GangOffshoreData GangOffshore;
        public GangPierData GangPier;
        public GangCashierData GangCashier;
    }

    public class Portfolio : ExtensibleData
    {
        public decimal Cash;
        public Dictionary<string, int> Holdings = new Dictionary<string, int>();
        public Dictionary<string, decimal> AvgCost = new Dictionary<string, decimal>();
        public List<string> Watchlist = new List<string>();

        public bool Buy(string symbol, int quantity, decimal price)
        {
            if (string.IsNullOrEmpty(symbol) || quantity <= 0 || price <= 0 || price > decimal.MaxValue / quantity) return false;
            int held; Holdings.TryGetValue(symbol, out held);
            decimal cost = price * quantity;
            if (held < 0 || held > int.MaxValue - quantity || Cash < cost) return false;
            decimal average; AvgCost.TryGetValue(symbol, out average);
            if(average<0 || held>0 && !AvgCost.ContainsKey(symbol))return false;
            decimal nextAverage;
            try{nextAverage=held==0?price:average+(price-average)*(quantity/(decimal)(held+quantity));}
            catch(OverflowException){return false;}
            AvgCost[symbol] = nextAverage;
            Holdings[symbol] = held + quantity; Cash -= cost;
            return true;
        }

        public bool Sell(string symbol, int quantity, decimal price)
        {
            int held;
            if (string.IsNullOrEmpty(symbol) || quantity <= 0 || price <= 0 || price > decimal.MaxValue / quantity || !Holdings.TryGetValue(symbol, out held) || held < quantity || Cash > decimal.MaxValue - price * quantity) return false;
            Cash += price * quantity;
            if (held == quantity) { Holdings.Remove(symbol); AvgCost.Remove(symbol); }
            else Holdings[symbol] = held - quantity;
            return true;
        }
    }

    public static class ItemTransfer
    {
        // One in-memory transaction: no remove-before-capacity-check item loss.
        public static bool Move(Dictionary<string, int> source, Dictionary<string, int> target, string id, int quantity, Func<string, float> weight, float capacity)
        {
            int have, held;
            if (id == null || quantity <= 0 || float.IsNaN(capacity) || float.IsInfinity(capacity) || capacity < 0 || !source.TryGetValue(id, out have) || have < quantity) return false;
            target.TryGetValue(id, out held);
            if (held < 0 || held > int.MaxValue - quantity) return false;
            double total = 0;
            foreach (var pair in target)
            {
                float existingWeight = weight(pair.Key);
                if (pair.Value < 0 || existingWeight < 0 || float.IsNaN(existingWeight) || float.IsInfinity(existingWeight)) return false;
                total += pair.Value * (double)existingWeight;
            }
            float unit = weight(id);
            if (float.IsNaN(unit) || unit < 0 || total + unit * (double)quantity > capacity) return false;
            if (have == quantity) source.Remove(id); else source[id] = have - quantity;
            target[id] = held + quantity;
            return true;
        }
    }
}
