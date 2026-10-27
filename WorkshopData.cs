using System;

namespace RPCore
{
    public static class WorkshopRules
    {
        public static bool Mod(int slot, int index, int count) => slot >= 0 && slot < 50 && !(slot >= 17 && slot <= 22) && count > 0 && count <= 2048 && index >= -1 && index < count;
        public static int Price(int slot) => slot == 11 || slot == 12 || slot == 13 || slot == 16 ? 1500 : slot == 23 || slot == 24 ? 700 : 350;
        // No preview: an option must change the live vehicle and be verified before payment.
        // Native world state cannot be crash-atomic with a JSON file. Ordinary failures roll back both.
        public static bool Buy(WalletData wallet, int cost, Func<bool> access, Func<bool> already, Action apply, Func<bool> verified, Action rollback, Action save)
        {
            if (wallet == null || cost <= 0 || wallet.Bank < cost || !access() || already()) return false;
            int bank = wallet.Bank;
            try
            {
                apply();
                if (!access() || !verified()) { rollback(); return false; }
                wallet.Bank -= cost;
                save();
                return true;
            }
            catch { wallet.Bank = bank; rollback(); throw; }
        }
    }
}
