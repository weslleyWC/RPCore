using System;
using GTA;

namespace RPCore
{
    public static class Wallet
    {
        static WalletData data = new WalletData();
        public static int Cash
        {
            get { return Profile.IsProtagonist(Profile.Id) ? Game.Player.Money : data.Cash; }
            private set { if (Profile.IsProtagonist(Profile.Id)) Game.Player.Money = value; else data.Cash = value; }
        }
        public static int Bank { get { return data.Bank; } }
        public static Portfolio Exchange { get { return data.Exchange; } set { data.Exchange = value; } }
        public static TransportData Transport { get { if (data.Transport == null) data.Transport = new TransportData(); data.Transport.Validate(); return data.Transport; } }
        public static void Reload() { data = Store.Load("Wallet.json", () => new WalletData()); data.Cash = Math.Max(0, data.Cash); data.Bank = Math.Max(0, data.Bank); }
        public static void Save() { Store.Write("Wallet.json", data); }
        public static bool Debit(int amount) { if (amount < 0 || Cash < amount) return false; Cash -= amount; return true; }
        public static void Credit(int amount) { if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount)); Cash = checked(Cash + amount); }
        public static CraftingData Crafting => data.Crafting ?? new CraftingData();
        public static int CraftAvailable(CraftIngredient ingredient)=>CraftingTransactions.Available(data,ingredient);
        public static bool CraftPreview(CraftingOptions c,string recipe,int count,out string reason)=>CraftingTransactions.Preview(data,c,recipe,count,out reason);
        public static bool Craft(CraftingOptions c,string recipe,int count,string id,out bool changed,out string reason)=>CraftingTransactions.Craft(data,c,recipe,count,id,Save,out changed,out reason);
        public static bool CraftSell(CraftingOptions c,string id,out bool changed)=>CraftingTransactions.Sell(data,c,id,Save,out changed);
        public static FishingData Fishing => data.Fishing ?? new FishingData();
        public static bool FishEquipment(FishingOptions c,string id,out bool changed)=>FishingTransactions.Equipment(data,c,id,Save,out changed);
        public static bool FishBait(FishingOptions c,int count,string id,out bool changed)=>FishingTransactions.BuyBait(data,c,count,id,Save,out changed);
        public static bool FishUseBait(string id,out bool changed)=>FishingTransactions.UseBait(data,id,Save,out changed);
        public static bool FishCatch(FishingOptions c,string fish,string id,bool complete,out bool changed)=>FishingTransactions.Catch(data,c,fish,id,complete,Save,out changed);
        public static bool FishSell(FishingOptions c,string id,out bool changed)=>FishingTransactions.Sell(data,c,id,Save,out changed);
        public static HuntingData Hunting => data.Hunting ?? new HuntingData();
        public static bool HuntLicense(HuntingOptions c,string id,out bool changed)=>HuntingTransactions.License(data,c,id,Save,out changed);
        public static bool HuntBait(HuntingOptions c,int count,string id,out bool changed)=>HuntingTransactions.BuyBait(data,c,count,id,Save,out changed);
        public static bool HuntUseBait(string id,out bool changed)=>HuntingTransactions.UseBait(data,id,Save,out changed);
        public static bool HuntHarvest(HuntingOptions c,System.Collections.Generic.Dictionary<string,int> loot,string id,bool complete,out bool changed)=>HuntingTransactions.Harvest(data,c,loot,id,complete,Save,out changed);
        public static bool HuntSell(HuntingOptions c,string id,out bool changed)=>HuntingTransactions.Sell(data,c,id,Save,out changed);
        public static bool PilotQualified => FlightRules.Qualified(data.Jobs);
        public static DeliveryProgress JobProgress(string job) { if (data.Jobs == null) data.Jobs = new JobLedger(); return data.Jobs.Get(job); }
        public static bool PayDelivery(string job, string session, int step, int amount, bool bank, bool delivery, bool complete, out bool credited, int rating = 0)
        {
            return DeliveryPayments.Commit(data, () => Cash, n => Cash = n, job, session, step, amount, bank, delivery, complete, Save, out credited, rating);
        }
        public static void SeedPizzaStats(PizzaStats legacy)
        {
            if (data.Jobs == null) data.Jobs = new JobLedger();
            DeliveryProgress existing;
            if (data.Jobs.Jobs != null && data.Jobs.Jobs.TryGetValue("pizza", out existing) && existing != null) return;
            var stats = data.Jobs.Get("pizza"); stats.Deliveries = Math.Max(0, legacy.Deliveries); stats.Earned = Math.Max(0, legacy.Earned);
        }
        public static bool Deposit(int amount)
        {
            if (amount <= 0 || data.Bank > int.MaxValue - amount || !Debit(amount)) return false;
            data.Bank += amount; return true;
        }
        public static bool Withdraw(int amount)
        {
            if (amount <= 0 || data.Bank < amount || Cash > int.MaxValue - amount) return false;
            Credit(amount); data.Bank -= amount; return true;
        }
    }
}
