using System;
using System.Collections.Generic;
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
        public static GangRaidData GangRaid=>data.GangRaid;
        public static bool GangWeaponRaidBegin(GangState s,GangRaidAttempt p,GangArmsOptions c,out bool changed)=>GangWeaponRaidTransactions.Begin(data,s,p,c,Save,out changed);
        public static bool GangWeaponRaidCapture(string id,out bool changed)=>GangWeaponRaidTransactions.Capture(data,id,Save,out changed);
        public static bool GangWeaponRaidDeliver(string id,out bool changed)=>GangWeaponRaidTransactions.Deliver(data,id,Save,out changed);
        public static bool GangWeaponRaidRestart(string id,out bool changed)=>GangWeaponRaidTransactions.Restart(data,id,Save,out changed);
        public static bool GangWeaponRaidComplete(GangState s,GangTurfDefinition area,GangArmsOptions c,string id,out bool changed)=>GangWeaponRaidTransactions.Complete(data,s,area,c,id,Save,out changed);
        public static bool GangRaidBegin(GangState s,GangRaidAttempt p,DealerOptions c,out bool changed)=>GangRaidTransactions.Begin(data,s,p,c,Save,out changed);
        public static bool GangRaidDefeat(string id,int index,out bool changed)=>GangRaidTransactions.Defeat(data,id,index,Save,out changed);
        public static bool GangRaidCollect(string id,out bool changed)=>GangRaidCollect(id,out changed,"Neutral");
        public static bool GangRaidCollect(string id,out bool changed,string initial)=>GangRaidTransactions.Collect(data,id,Save,out changed,initial);
        public static GangDiplomacyData GangDiplomacy=>data.GangDiplomacy;
        public static GangBulkData GangBulk=>data.GangBulk;
        public static GangArmsData GangArms=>data.GangArms;
        public static GangAutoArmsData GangAutoArms=>data.GangAutoArms;
        public static GangTowData GangTow=>data.GangTow;
        public static bool GangTowOtherFree=>GangTowTransactions.OtherFree(data);
        public static bool GangTowBegin(GangState s,GangTurfDefinition area,GangTowOptions c,IEnumerable<GangTowDispatch> d,string id,out bool changed)=>GangTowTransactions.Begin(data,s,area,c,d,id,Save,out changed);
        public static bool GangTowDeliver(GangState s,GangTurfDefinition area,string id,int index,float damage,out bool changed)=>GangTowTransactions.Deliver(data,s,area,id,index,damage,Save,out changed);
        public static bool GangTowCancel(string id,out bool changed)=>GangTowTransactions.Cancel(data,id,Save,out changed);
        public static bool GangTowComplete(GangState s,GangTurfDefinition area,string id,DateTime utc,out bool changed)=>GangTowTransactions.Complete(data,s,area,id,utc,Save,out changed);
        public static GangMechanicData GangMechanic=>data.GangMechanic;
        public static bool GangMechanicOtherFree=>GangMechanicTransactions.OtherFree(data);
        public static bool GangMechanicBegin(GangState s,GangTurfDefinition area,GangMechanicOptions c,IEnumerable<string> models,string id,out bool changed)=>GangMechanicTransactions.Begin(data,s,area,c,models,id,Save,out changed);
        public static bool GangMechanicRepair(GangState s,GangTurfDefinition area,string id,int index,out bool changed)=>GangMechanicTransactions.Repair(data,s,area,id,index,Save,out changed);
        public static bool GangMechanicCancel(string id,out bool changed)=>GangMechanicTransactions.Cancel(data,id,Save,out changed);
        public static bool GangMechanicComplete(GangState s,GangTurfDefinition area,string id,DateTime utc,out bool changed)=>GangMechanicTransactions.Complete(data,s,area,id,utc,Save,out changed);
        public static GangPromotionData GangPromotion=>data.GangPromotion;
        public static bool GangPromotionOtherFree=>GangPromotionTransactions.OtherFree(data);
        public static bool GangPromotionBegin(GangState s,GangTurfDefinition area,GangPromotionOptions c,string id,out bool changed)=>GangPromotionTransactions.Begin(data,s,area,c,id,Save,out changed);
        public static bool GangPromotionGive(GangState s,GangTurfDefinition area,string id,string customer,out bool changed)=>GangPromotionTransactions.Give(data,s,area,id,customer,Save,out changed);
        public static bool GangPromotionRestart(string id,out bool changed)=>GangPromotionTransactions.Restart(data,id,Save,out changed);
        public static bool GangPromotionCancel(string id,out bool changed)=>GangPromotionTransactions.Cancel(data,id,Save,out changed);
        public static bool GangPromotionComplete(GangState s,GangTurfDefinition area,string id,DateTime utc,out bool changed)=>GangPromotionTransactions.Complete(data,s,area,id,utc,Save,out changed);
        public static GangCafeData GangCafe=>data.GangCafe;
        public static bool GangCafeBegin(GangState s,GangTurfDefinition area,GangCafeOptions c,string id,out bool changed)=>GangCafeTransactions.Begin(data,s,area,c,id,Save,out changed);
        public static bool GangCafeComplete(GangState s,GangTurfDefinition area,string id,DateTime utc,out bool changed)=>GangCafeTransactions.Complete(data,s,area,id,utc,Save,out changed);
        public static GangAutoLaunderData GangAutoLaunder=>data.GangAutoLaunder;
        public static bool GangAutoLaunderConfigure(GangState s,GangTurfDefinition area,bool enabled,string id,out bool changed)=>GangAutoLaunderTransactions.Configure(data,s,area,enabled,id,Save,out changed);
        public static bool GangAutoLaunderStop(string gang,int area,string id,out bool changed)=>GangAutoLaunderTransactions.Stop(data,gang,area,id,Save,out changed);
        public static bool GangAutoLaunderProcess(GangState s,GangTurfDefinition area,GangAutoLaunderOptions c,DateTime now,string id,out bool changed)=>GangAutoLaunderTransactions.Process(data,s,area,c,now,id,Save,out changed);
        public static bool GangAutoArmsFree=>GangAutoArmsRules.Free(data);
        public static List<string> GangAutoArmsAvailable(string gang,int area,GangArmsOptions c)=>GangAutoArmsRules.Available(data,gang,area,c);
        public static bool GangAutoArmsConfigure(GangState s,GangTurfDefinition area,GangArmsOptions c,string product,bool enabled,string id,out bool changed)=>GangAutoArmsTransactions.Configure(data,s,area,c,product,enabled,id,Save,out changed);
        public static bool GangAutoArmsStop(string gang,int area,string id,out bool changed)=>GangAutoArmsTransactions.Stop(data,gang,area,id,Save,out changed);
        public static bool GangAutoArmsSale(GangState s,GangTurfDefinition area,GangArmsOptions catalog,GangAutoArmsOptions c,string product,string id,out bool changed)=>GangAutoArmsTransactions.Sale(data,s,area,catalog,c,product,id,Save,out changed);
        public static bool GangArmsBuy(GangState s,GangTurfDefinition a,GangArmsOptions c,string product,int units,string id,out bool changed)=>GangArmsTransactions.Buy(data,s,a,c,product,units,id,Save,out changed);
        public static bool GangArmsBegin(GangState s,GangBulkAttempt p,GangArmsOptions c,out bool changed)=>GangArmsTransactions.BeginDelivery(data,s,p,c,Save,out changed);
        public static bool GangArmsComplete(GangState s,GangTurfDefinition a,GangArmsOptions c,string id,out bool changed)=>GangArmsTransactions.CompleteDelivery(data,s,a,c,id,Save,out changed);
        public static bool GangBulkBlocked=>GangBulkTransactions.Blocked(data);
        public static bool GangBulkBegin(GangState s,GangBulkAttempt p,DealerOptions c,out bool changed)=>GangBulkTransactions.Begin(data,s,p,c,Save,out changed);
        public static bool GangBulkLoad(string id,out bool changed)=>GangBulkTransactions.Load(data,id,Save,out changed);
        public static bool GangBulkReady(string id,out bool changed)=>GangBulkTransactions.Ready(data,id,Save,out changed);
        public static bool GangBulkRestart(string id,out bool changed)=>GangBulkTransactions.Restart(data,id,Save,out changed);
        public static bool GangBulkCancel(string id,out bool changed)=>GangBulkTransactions.Cancel(data,id,Save,out changed);
        public static bool GangBulkComplete(GangState s,GangTurfDefinition area,DealerOptions c,string id,out bool changed)=>GangBulkTransactions.Complete(data,s,area,c,id,Save,out changed);
        public static bool GangNegotiate(GangState state,string rival,string initial,GangDiplomacyOptions c,DateTime now,string id,out bool changed)=>GangDiplomacyTransactions.Negotiate(data,state,rival,initial,c,now,id,Save,out changed);
        public static bool GangRaidRestart(string id,out bool changed)=>GangRaidTransactions.Restart(data,id,Save,out changed);
        public static bool GangRaidEnd(string id,bool failed,out bool changed)=>GangRaidTransactions.End(data,id,failed,Save,out changed);
        public static bool GangRaidComplete(GangState s,GangTurfDefinition a,DealerOptions c,string id,out bool changed)=>GangRaidTransactions.Complete(data,s,a,c,id,Save,out changed);
        public static GangWarehouseData GangWarehouse=>data.GangWarehouse;
        public static bool GangWarehouseTransfer(GangState s,GangTurfDefinition a,DealerOptions c,GangWarehouseOptions o,string product,int count,bool deposit,string id,out bool changed)=>GangWarehouseTransactions.Transfer(data,s,a,c,o,product,count,deposit,id,Save,out changed);
        public static bool GangWarehouseSale(GangState s,GangTurfDefinition a,DealerOptions c,string product,int count,string zone,bool premium,int unit,bool last,string id,out bool changed)=>GangWarehouseTransactions.Sale(data,s,a,c,product,count,zone,premium,unit,last,id,Save,out changed);
        public static GangIllicitData GangIllicit=>data.GangIllicit;
        public static bool DealerTerritorySale(GangState s,GangTurfDefinition a,DealerOptions c,string product,int count,string zone,bool premium,int unit,bool last,string id,out bool changed)=>GangIllicitTransactions.Sale(data,s,a,c,product,count,zone,premium,unit,last,id,Save,out changed);
        public static bool GangLaunder(GangState s,GangTurfDefinition a,GangLaunderingOptions c,int amount,DateTime now,string id,out bool changed)=>GangIllicitTransactions.Process(data,s,a,c,amount,now,id,Save,out changed);
        public static GangBusinessData GangBusiness=>data.GangBusiness;
        public static bool GangSupplyBegin(GangState s,GangTurfDefinition a,GangBusinessOptions b,GangSupplyOptions c,string id,out bool changed)=>GangSupplyTransactions.Begin(data,s,a,b,c,id,Save,out changed);
        public static bool GangSupplyBeginCriminal(GangState s,GangTurfDefinition a,DealerOptions c,GangWarehouseOptions o,GangCriminalSupplyOptions r,string product,string id,out bool changed)=>GangSupplyTransactions.BeginCriminal(data,s,a,c,o,r,product,id,Save,out changed);
        public static bool GangSupplyLoad(string id,int index,out bool changed)=>GangSupplyTransactions.Load(data,id,index,Save,out changed);
        public static bool GangSupplyDeliver(string id,int number,out bool changed)=>GangSupplyTransactions.Deliver(data,id,number,Save,out changed);
        public static bool GangSupplyRestart(string id,out bool changed)=>GangSupplyTransactions.Restart(data,id,Save,out changed);
        public static bool GangSupplyComplete(GangState s,GangTurfDefinition a,string id,out bool changed)=>GangSupplyComplete(s,a,id,out changed,null);
        public static bool GangSupplyComplete(GangState s,GangTurfDefinition a,string id,out bool changed,DealerOptions catalog)=>GangSupplyTransactions.Complete(data,s,a,id,Save,out changed,catalog);
        public static bool GangSupplyRefund(string id,out bool changed)=>GangSupplyTransactions.Refund(data,id,Save,out changed);
        public static bool GangTrade(GangState s,GangTurfDefinition area,GangBusinessOptions c,bool sale,DateTime now,string id,out bool changed)=>GangBusinessTransactions.Trade(data,s,area,c,sale,now,id,Save,out changed);
        public static GangTreasuryData GangTreasury=>data.GangTreasury;
        public static bool GangDeposit(string gang,int amount,DateTime now,GangTreasuryOptions c,string id,out bool changed)=>GangTreasuryTransactions.Deposit(data,gang,amount,now,c,id,Save,out changed);
        public static bool GangWithdraw(string gang,int amount,GangTreasuryOptions c,string id,out bool changed)=>GangTreasuryTransactions.Withdraw(data,gang,amount,c,id,Save,out changed);
        public static bool GangPayday(string gang,int rank,DateTime now,GangTreasuryOptions c,string id,out bool changed)=>GangTreasuryTransactions.Payday(data,gang,rank,now,c,id,Save,out changed);
        public static DmvData DmvA=>data.DmvA??new DmvData();
        public static DmvData DmvM1=>data.DmvM1??new DmvData();
        public static DmvData Dmv=>data.Dmv??new DmvData();
        public static bool DmvBegin(DmvOptions c,bool practical,string id,out bool changed,DmvClass category=DmvClass.C)=>DmvTransactions.Begin(data,c,practical,id,Save,out changed,category);
        public static bool DmvStart(string id,out bool changed,DmvClass category=DmvClass.C)=>DmvTransactions.Start(data,id,Save,out changed,category);
        public static bool DmvAnswer(string id,int question,int answer,out bool changed,DmvClass category=DmvClass.C)=>DmvTransactions.Answer(data,id,question,answer,Save,out changed,category);
        public static bool DmvProgress(string id,DmvDrive state,out bool changed,DmvClass category=DmvClass.C)=>DmvTransactions.Progress(data,id,state,Save,out changed,category);
        public static bool DmvRestart(string id,out bool changed,DmvClass category=DmvClass.C)=>DmvTransactions.Restart(data,id,Save,out changed,category);
        public static bool DmvFinish(string id,bool passed,out bool changed,DmvClass category=DmvClass.C)=>DmvTransactions.Finish(data,id,passed,Save,out changed,category);
        public static bool DmvCancel(string id,out bool changed,DmvClass category=DmvClass.C)=>DmvTransactions.Cancel(data,id,Save,out changed,category);
        public static bool DmvAbandon(string id,out bool changed,DmvClass category=DmvClass.C)=>DmvTransactions.Abandon(data,id,Save,out changed,category);
        public static VehicleInsuranceData VehicleInsurance=>data.VehicleInsurance??new VehicleInsuranceData();
        public static bool VehicleInsure(VehicleInsuranceOptions c,string vehicle,out bool changed)=>VehicleInsuranceTransactions.Enroll(data,c,vehicle,Save,out changed);
        public static bool VehicleInsuranceCancel(string vehicle,out bool changed)=>VehicleInsuranceTransactions.Cancel(data,vehicle,Save,out changed);
        public static bool VehicleRecoveryBegin(string vehicle,SavedVehicle original,int handle,float oil,bool destroyed,string id,out bool changed)=>VehicleInsuranceTransactions.Begin(data,vehicle,original,handle,oil,destroyed,id,Save,out changed);
        public static bool VehicleRecoveryStart(string id,out bool changed)=>VehicleInsuranceTransactions.Start(data,id,Save,out changed);
        public static bool VehicleRecoveryComplete(string id,SavedVehicle repaired,out bool changed)=>VehicleInsuranceTransactions.Complete(data,id,repaired,Save,out changed);
        public static bool VehicleRecoveryRefund(string id,out bool changed)=>VehicleInsuranceTransactions.Refund(data,id,Save,out changed);
        public static bool VehicleRecoveryAbandon(string id,out bool changed)=>VehicleInsuranceTransactions.Abandon(data,id,Save,out changed);
        public static GroomingData Grooming => data.Grooming ?? new GroomingData();
        public static bool GroomingBegin(GroomingLook look,int price,string id,out bool changed)=>GroomingTransactions.Begin(data,look,price,id,Save,out changed);
        public static bool GroomingStart(string id,out bool changed)=>GroomingTransactions.Start(data,id,Save,out changed);
        public static bool GroomingComplete(string id,out bool changed)=>GroomingTransactions.Complete(data,id,Save,out changed);
        public static bool GroomingRefund(string id,out bool changed)=>GroomingTransactions.Refund(data,id,Save,out changed);
        public static bool GroomingAbandon(string id,out bool changed)=>GroomingTransactions.Abandon(data,id,Save,out changed);
        public static BarberData Barber => data.Barber ?? new BarberData();
        public static bool BarberConfirm(BarberLook before,BarberLook after,int price,string id,out bool changed) => BarberTransactions.Confirm(data,before,after,price,id,Save,out changed);
        public static WardrobeData Wardrobe => data.Wardrobe ?? new WardrobeData();
        public static bool WardrobeSave(WardrobeOptions c,WardrobeOutfit o,bool replace,string id,out bool changed) => WardrobeTransactions.Save(data,c,o,replace,id,Save,out changed);
        public static bool WardrobeRename(string outfit,string name,string id,out bool changed) => WardrobeTransactions.Rename(data,outfit,name,id,Save,out changed);
        public static bool WardrobeDelete(string outfit,string id,out bool changed) => WardrobeTransactions.Delete(data,outfit,id,Save,out changed);
        public static HousingData Housing => data.Housing ?? new HousingData();
        public static bool HousingPurchase(HousingOptions c, string id, out bool changed) => HousingTransactions.Purchase(data, c, id, Save, out changed);
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
        public static bool PdmFinanceOpenRisky(PdmFinanceOptions c,string id,int engine,int body,int risk,out bool changed)=>PdmFinanceTransactions.OpenRisky(data,c,id,engine,body,risk,Save,out changed);
        public static bool PdmRepoBegin(PdmOptions c,PdmRepoOptions r,PdmRepoOperation a,out bool changed)=>PdmRepoTransactions.Begin(data,c,r,a,Save,out changed);
        public static bool PdmRepoCar(string id,int handle,out bool changed)=>PdmRepoTransactions.Register(data,id,handle,Save,out changed);
        public static bool PdmRepoHealth(string id,int engine,int body,out bool changed)=>PdmRepoTransactions.Remember(data,id,engine,body,Guid.NewGuid().ToString("N"),Save,out changed);
        public static bool PdmRepoArrive(string id,out bool changed)=>PdmRepoTransactions.Arrive(data,id,Save,out changed);
        public static bool PdmRepoReceive(PdmOptions c,string id,out bool changed)=>PdmRepoTransactions.Complete(data,c,id,Save,out changed);
        public static bool PdmRepoCancel(string id,out bool changed)=>PdmRepoTransactions.Cancel(data,id,Save,out changed);
        public static bool PdmRentalBegin(PdmRentalOptions c,string id,System.Collections.Generic.List<string> ids,PdmEntityRef truck,PdmEntityRef trailer,out bool changed)=>PdmRentalTransactions.Begin(data,c,id,ids,truck,trailer,Save,out changed);
        public static bool PdmRentalCar(string id,string stockId,int handle,out bool changed)=>PdmRentalTransactions.Register(data,id,stockId,handle,Save,out changed);
        public static bool PdmRentalHealth(string id,System.Collections.Generic.Dictionary<string,int[]> health,out bool changed)=>PdmRentalTransactions.Remember(data,id,health,Guid.NewGuid().ToString("N"),Save,out changed);
        public static bool PdmRentalDeliver(string id,out bool changed)=>PdmRentalTransactions.Deliver(data,id,Save,out changed);
        public static bool PdmRentalReturn(string id,out bool changed)=>PdmRentalTransactions.Return(data,id,Save,out changed);
        public static bool PdmRentalSettle(string id,bool recovery,out bool changed)=>PdmRentalTransactions.Settle(data,id,recovery,Save,out changed);
        public static bool PdmRentalCancel(string id,out bool changed)=>PdmRentalTransactions.Cancel(data,id,Save,out changed);
        public static bool PdmShowroomAssign(PdmShowroomOptions c,string stock,string slot,int handle,out bool changed)=>PdmShowroomTransactions.Assign(data,c,stock,slot,handle,Guid.NewGuid().ToString("N"),Save,out changed);
        public static bool PdmShowroomTrack(string stock,int handle,out bool changed)=>PdmShowroomTransactions.Track(data,stock,handle,Guid.NewGuid().ToString("N"),Save,out changed);
        public static bool PdmShowroomRemove(string stock,out bool changed)=>PdmShowroomTransactions.Remove(data,stock,Guid.NewGuid().ToString("N"),Save,out changed);
        public static bool PdmShowroomBegin(string stock,string id,out bool changed)=>PdmShowroomTransactions.BeginSale(data,stock,id,Save,out changed);
        public static bool PdmShowroomSell(string id,out bool changed)=>PdmShowroomTransactions.Sell(data,id,Save,out changed);
        public static bool PdmShowroomCancel(string id,out bool changed)=>PdmShowroomTransactions.Cancel(data,id,Save,out changed);
        public static bool PdmRiskReturn(string id, out bool changed) => PdmRiskTransactions.ReturnToPremises(data, id, Save, out changed);
        public static bool PdmSetSafe(bool open, string id, out bool changed) => PdmRiskTransactions.SetSafe(data, open, id, Save, out changed);
        public static bool PdmInsure(PdmRiskOptions c, string plan, string id, out bool changed) => PdmRiskTransactions.Insure(data, c, plan, id, Save, out changed);
        public static bool PdmCancelInsurance(string id, out bool changed) => PdmRiskTransactions.Cancel(data, id, Save, out changed);
        public static bool PdmRiskWork(PdmRiskOptions c, int seconds, bool away, string id, out bool changed) => PdmRiskTransactions.Work(data, c, seconds, away, id, Save, out changed);
        public static bool PdmSurveillancePurchase(PdmSurveillanceOptions c, string id, out bool changed) => PdmSurveillanceTransactions.Purchase(data, c, id, Save, out changed);
        public static bool PdmServiceBegin(PdmOptions c,string stock,string kind,string id,out bool changed)=>PdmPremisesTransactions.Begin(data,c,stock,kind,id,Save,out changed);
        public static bool PdmServiceComplete(string id,out bool changed)=>PdmPremisesTransactions.Complete(data,id,Save,out changed);
        public static bool PdmServiceCancel(string id,out bool changed)=>PdmPremisesTransactions.Cancel(data,id,Save,out changed);
        public static bool PdmDirtWork(PdmPremisesOptions c,int seconds,string id,out bool changed)=>PdmPremisesTransactions.Work(data,c,seconds,id,Save,out changed);
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
