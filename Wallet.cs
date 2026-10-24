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
        public static PdmData Pdm => data.Pdm ?? new PdmData();
        public static bool PdmHire(PdmStaffOptions c,string role,string id,out bool changed)=>PdmStaffTransactions.Hire(data,c,role,id,Save,out changed);
        public static bool PdmWork(int seconds,string id,out bool changed)=>PdmStaffTransactions.Work(data,seconds,id,Save,out changed);
        public static bool PdmPay(string role,string id,out bool changed)=>PdmStaffTransactions.Pay(data,role,id,Save,out changed);
        public static bool PdmFire(string employee,string id,out bool changed)=>PdmStaffTransactions.Fire(data,employee,id,Save,out changed);
        public static bool PdmUpgrade(PdmStaffOptions c,string upgrade,string id,out bool changed)=>PdmStaffTransactions.Upgrade(data,c,upgrade,id,Save,out changed);
        public static bool PdmSafeBegin(PdmSafeTransfer t,out bool changed)=>PdmSafeTransactions.Begin(data,t,Save,out changed);
        public static bool PdmSafeHand(string id,out bool changed)=>PdmSafeTransactions.Hand(data,id,Save,out changed);
        public static bool PdmSafeRefund(string id,out bool changed)=>PdmSafeTransactions.Refund(data,id,Save,out changed);
        public static bool PdmSafeCredit(string id,out bool changed)=>PdmSafeTransactions.Credit(data,id,Save,out changed);
        public static bool PdmBeginFinanced(PdmOptions c,string stockId,string id,int destination,int handle,out bool changed)=>PdmTransactions.Begin(data,c,stockId,id,destination,handle,Save,out changed,true);
        public static bool PdmFinanceOpen(PdmFinanceOptions c,string id,int engine,int body,out bool changed)=>PdmFinanceTransactions.Open(data,c,id,engine,body,Save,out changed);
        public static bool PdmFinanceWork(int seconds,string id,out bool changed)=>PdmFinanceTransactions.Work(data,seconds,id,Save,out changed);
        public static bool PdmPurchase(PdmOptions c,string id,out bool changed)=>PdmTransactions.Purchase(data,c,id,Save,out changed);
        public static bool PdmTransfer(int amount,bool deposit,string id,out bool changed)=>PdmTransactions.Transfer(data,amount,deposit,id,Save,out changed);
        public static bool PdmBuy(PdmOptions c,string offer,string stockId,string receipt,out bool changed)=>PdmTransactions.Buy(data,c,offer,stockId,receipt,Save,out changed);
        public static bool PdmOrder(PdmOptions c,string offer,string stockId,string receipt,out bool changed)=>PdmTransactions.Buy(data,c,offer,stockId,receipt,Save,out changed,true);
        public static bool PdmCollect(string id,System.Collections.Generic.List<string> ids,PdmEntityRef truck,PdmEntityRef trailer,out bool changed)=>PdmCollectionTransactions.Begin(data,id,ids,truck,trailer,Save,out changed);
        public static bool PdmCargo(string id,string stockId,int handle,out bool changed)=>PdmCollectionTransactions.Register(data,id,stockId,handle,Save,out changed);
        public static bool PdmCargoHealth(string id,System.Collections.Generic.Dictionary<string,int[]> health,out bool changed)=>PdmCollectionTransactions.Remember(data,id,health,Guid.NewGuid().ToString("N"),Save,out changed);
        public static bool PdmArrive(string id,out bool changed)=>PdmCollectionTransactions.Arrive(data,id,Save,out changed);
        public static bool PdmReceive(string id,out bool changed)=>PdmCollectionTransactions.Complete(data,id,Save,out changed);
        public static bool PdmCollectCancel(string id,out bool changed)=>PdmCollectionTransactions.Cancel(data,id,Save,out changed);
        public static bool PdmRepair(PdmOptions c,string stockId,string receipt,out bool changed)=>PdmTransactions.Repair(data,c,stockId,receipt,Save,out changed);
        public static bool PdmBegin(PdmOptions c,string stockId,string operationId,int destination,int handle,out bool changed)=>PdmTransactions.Begin(data,c,stockId,operationId,destination,handle,Save,out changed);
        public static bool PdmRemember(string id,int engine,int body,string receipt,out bool changed)=>PdmTransactions.Remember(data,id,engine,body,receipt,Save,out changed);
        public static bool PdmCancel(string id,int engine,int body,out bool changed)=>PdmTransactions.Cancel(data,id,engine,body,Save,out changed);
        public static bool PdmSell(string id,int engine,int body,out bool changed)=>PdmTransactions.Sell(data,id,engine,body,Save,out changed);
        public static DragData Drag => data.Drag ?? new DragData();
        public static bool DragEnter(DragOptions c,int stake,string id,out bool changed)=>DragTransactions.Enter(data,c,stake,id,Save,out changed);
        public static bool DragGreen(string id,out bool changed)=>DragTransactions.Green(data,id,Save,out changed);
        public static bool DragRecord(string id,DragOutcome result,int ms,out bool changed)=>DragTransactions.Record(data,id,result,ms,Save,out changed);
        public static bool DragSettle(string id,out bool changed)=>DragTransactions.Settle(data,id,Save,out changed);
        public static StreamerData Streamer => data.Streamer ?? new StreamerData();
        public static bool StreamSettings(string channel,string title,string id,out bool changed)=>StreamerTransactions.Settings(data,channel,title,id,Save,out changed);
        public static bool StreamStart(string id,out bool changed)=>StreamerTransactions.Start(data,id,Save,out changed);
        public static bool StreamBlock(StreamerOptions c,StreamReward reward,string id,int step,bool last,out bool changed)=>StreamerTransactions.Block(data,c,reward,id,step,last,Save,out changed);
        public static bool StreamStop(string id,string reason,out bool changed)=>StreamerTransactions.Stop(data,id,reason,Save,out changed);
        public static DealerData Dealer => data.Dealer ?? new DealerData();
        public static bool DealerBuy(DealerOptions c,string product,int count,string id,out bool changed)=>DealerTransactions.Buy(data,c,product,count,id,Save,out changed);
        public static bool DealerSale(DealerOptions c,string product,int count,string zone,bool premium,int quoted,bool last,string id,out bool changed)=>DealerTransactions.Sale(data,c,product,count,zone,premium,quoted,last,id,Save,out changed);
        public static bool DealerEncounter(DealerOutcome result,string zone,string id,out bool changed)=>DealerTransactions.Encounter(data,result,zone,id,Save,out changed);
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
