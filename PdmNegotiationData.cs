using System;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class PdmNegotiation : ExtensibleData
    {
        public int BasePrice,Offer,Budget,Counter;
        public bool Inspected,Accepted,Countered,Rejected;
        [JsonIgnore] public bool Valid=>BasePrice>=1 && BasePrice<=3000000 && Offer>=1 && Offer<=3000000 && Budget>=Offer && Budget<=3000000 && Counter>=0 && Counter<=3000000 && Budget<=Math.Max(1,Math.Min(3000000,(long)BasePrice*105/100)) && Offer>=Math.Max(1,(long)BasePrice*95/100) && Offer<=Math.Min(3000000,(long)BasePrice*110/100)
            && (Countered || Counter==0) && (!Accepted || Offer<=Budget && (!Countered || Counter==Offer)) && (!Rejected || Counter>Budget)
            && (!Accepted || Inspected && !Rejected) && (!Rejected || Countered && !Accepted) && (!Countered || Inspected && Counter>0);
    }
    public static class PdmNegotiationTransactions
    {
        public static PdmNegotiation Create(int price,string id)
        {
            if(price<1 || price>3000000 || string.IsNullOrWhiteSpace(id))return null;
            int offer=Math.Max(1,(int)((long)price*95/100));
            return new PdmNegotiation{BasePrice=price,Offer=offer,Budget=Math.Min(3000000,Math.Max(offer,(int)((long)price*(95+PdmRiskTransactions.Roll(id)%11)/100)))};
        }
        public static bool Inspect(WalletData w,string id,Action save,out bool changed)
        {
            return PdmTransactions.Apply(w,id+":inspection",0,d=>{
                var n=d.ShowroomSale?.Negotiation;if(d.ShowroomSale?.Id!=id || n==null || n.Inspected)return false;n.Inspected=true;return true;
            },save,out changed);
        }
        public static bool Accept(WalletData w,string id,Action save,out bool changed)
        {
            return PdmTransactions.Apply(w,id+":accept-offer",0,d=>{
                var sale=d.ShowroomSale;var n=sale?.Negotiation;if(sale?.Id!=id || n==null || !n.Inspected || n.Accepted || n.Rejected)return false;
                n.Accepted=true;sale.Price=n.Offer;return true;
            },save,out changed);
        }
        public static bool CounterOffer(WalletData w,string id,int price,Action save,out bool changed)
        {
            changed=false;if(price<1 || price>3000000)return false;
            return PdmTransactions.Apply(w,id+":counter-offer",0,d=>{
                var sale=d.ShowroomSale;var n=sale?.Negotiation;
                if(sale?.Id!=id || n==null || !n.Inspected || n.Accepted || n.Rejected || n.Countered || price<=n.Offer || price>Math.Min(3000000,(long)n.BasePrice*110/100))return false;
                n.Countered=true;n.Counter=price;n.Accepted=price<=n.Budget;n.Rejected=!n.Accepted;
                if(n.Accepted){n.Offer=price;sale.Price=price;}return true;
            },save,out changed);
        }
    }
}
