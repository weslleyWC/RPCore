using System;
namespace RPCore
{
    public static class BankTransfer
    {
        public static bool Commit(WalletData w,Func<int> cash,Action<int> setCash,int amount,bool deposit,Action save)
        {
            if(w==null || cash==null || setCash==null || save==null || amount<=0 || w.Bank<0)return false;
            int held=cash();if(held<0 || (deposit?(held<amount || w.Bank>int.MaxValue-amount):(w.Bank<amount || held>int.MaxValue-amount)))return false;
            int bank=w.Bank;
            try{w.Bank+=deposit?amount:-amount;setCash(held+(deposit?-amount:amount));save();return true;}
            catch{w.Bank=bank;setCash(held);throw;}
        }
    }
}
