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
        public static void Reload() { data = Store.Load("Wallet.json", () => new WalletData()); data.Cash = Math.Max(0, data.Cash); data.Bank = Math.Max(0, data.Bank); }
        public static void Save() { Store.Write("Wallet.json", data); }
        public static bool Debit(int amount) { if (amount < 0 || Cash < amount) return false; Cash -= amount; return true; }
        public static void Credit(int amount) { if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount)); Cash = checked(Cash + amount); }
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
