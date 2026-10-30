using System;
using System.Drawing;
using System.Globalization;
using System.Linq;

namespace RPCore
{
    public static class PhoneBankRules
    {
        public const int App=12;
        public static bool IsPage(string page)=>page=="Bank" || page=="BankConfirm" || page=="BankReceipt";
        public static string Money(int amount)=>"$ "+amount.ToString("N0",CultureInfo.GetCultureInfo("pt-BR"));
        public static bool Amount(string text,out int amount)
        {
            amount=0;text=text?.Trim();if(string.IsNullOrEmpty(text))return false;
            var groups=text.Split('.');
            if(groups.Length>1 && (groups[0].Length<1 || groups[0].Length>3 || groups.Skip(1).Any(g=>g.Length!=3)))return false;
            return PhoneViewRules.Amount(string.Concat(groups),out amount);
        }
        public static int HomeHit(PointF point)=>new RectangleF(23,85,196,49).Contains(point)?App:PhoneViewRules.HomeHit(point);
        public static int MoveHome(int current,int dx,int dy)
        {
            if(current==App)return dy>0?0:App;
            if(current<0 || current>=12)return App;
            if(current<3 && dy<0)return App;
            return HudMath.MoveGrid(current,dx,dy,3,12);
        }
    }
    public enum PhoneBankResult { Completed, Stale, Rejected, Used }
    public sealed class PhoneBankOperation
    {
        public int Amount{get;}public int Cash{get;}public int Bank{get;}public bool Deposit{get;}
        public int CashAfter=>Cash+(Deposit?-Amount:Amount);
        public int BankAfter=>Bank+(Deposit?Amount:-Amount);
        readonly string profile;readonly int actor,model;bool used;
        PhoneBankOperation(string profile,int actor,int model,int cash,int bank,int amount,bool deposit)
        {this.profile=profile;this.actor=actor;this.model=model;Cash=cash;Bank=bank;Amount=amount;Deposit=deposit;}
        public static PhoneBankOperation Create(string profile,int actor,int model,int cash,int bank,int amount,bool deposit)
        {
            if(string.IsNullOrEmpty(profile) || actor==0 || model==0 || amount<=0 || amount>PhoneViewRules.BankMaximum(cash,bank,deposit))return null;
            return new PhoneBankOperation(profile,actor,model,cash,bank,amount,deposit);
        }
        public PhoneBankResult Commit(string currentProfile,int currentActor,int currentModel,int cash,int bank,Func<int,bool,bool> transfer)
        {
            if(used)return PhoneBankResult.Used;used=true;
            if(profile!=currentProfile || actor!=currentActor || model!=currentModel || cash!=Cash || bank!=Bank)return PhoneBankResult.Stale;
            return transfer!=null && transfer(Amount,Deposit)?PhoneBankResult.Completed:PhoneBankResult.Rejected;
        }
    }
}
