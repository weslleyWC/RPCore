using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class BarberOptions : ExtensibleData
    {
        public int Price = 150;
        public List<JobPoint> Shops = new List<JobPoint>();
        [JsonIgnore] public bool Valid => Price >= 0 && Price <= 10000 && Shops != null && Shops.Count >= 1 && Shops.Count <= 30 && Shops.All(p=>p!=null && p.Valid);
    }
    public static class BarberDefaults
    {
        public static BarberOptions Load()
        { using(var s=typeof(BarberDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.Barber.json")) using(var r=new System.IO.StreamReader(s)) return JsonConvert.DeserializeObject<BarberOptions>(r.ReadToEnd()); }
    }
    public sealed class BarberLook : ExtensibleData
    {
        public int Model,Hair,Texture,Palette,Eye;
        public bool HasEye;
        [JsonIgnore] public bool Valid => Model!=0 && Hair>=0 && Hair<=10000 && Texture>=0 && Texture<=10000 && Palette>=0 && Palette<=3 && (HasEye?Eye>=0 && Eye<=31:Eye==0);
        public bool Same(BarberLook b) => b!=null && Model==b.Model && Hair==b.Hair && Texture==b.Texture && Palette==b.Palette && HasEye==b.HasEye && Eye==b.Eye;
    }
    public sealed class BarberData : ExtensibleData
    {
        public List<BarberLook> Looks = new List<BarberLook>();
        public List<string> Receipts = new List<string>();
        public long Spent;
        public int Services;
        [JsonIgnore] public bool Valid => Looks!=null && Looks.Count<=32 && Looks.All(l=>l!=null && l.Valid) && Looks.Select(l=>l.Model).Distinct().Count()==Looks.Count && Receipts!=null && Receipts.Count<=128 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=100) && Receipts.Distinct().Count()==Receipts.Count && Spent>=0 && Services>=0;
    }
    public static class BarberTransactions
    {
        public static bool Confirm(WalletData w,BarberLook before,BarberLook after,int price,string receipt,Action save,out bool changed)
        {
            changed=false;
            if(w==null || before==null || after==null || !before.Valid || !after.Valid || before.Model!=after.Model || before.HasEye!=after.HasEye || before.Same(after) || price<0 || price>10000 || string.IsNullOrWhiteSpace(receipt) || receipt.Length>100 || save==null || w.Bank<0 || w.Barber!=null && !w.Barber.Valid)return false;
            var old=w.Barber;if(old?.Receipts.Contains(receipt)==true)return true;
            if(w.Bank<price)return false;var next=old==null?new BarberData():JsonConvert.DeserializeObject<BarberData>(JsonConvert.SerializeObject(old));
            if(next.Services==int.MaxValue || next.Spent>long.MaxValue-price || next.Looks.Count>=32 && !next.Looks.Any(l=>l.Model==after.Model))return false;
            var record=next.Looks.Find(l=>l.Model==after.Model);
            if(record==null)next.Looks.Add(JsonConvert.DeserializeObject<BarberLook>(JsonConvert.SerializeObject(after)));
            else { record.Hair=after.Hair;record.Texture=after.Texture;record.Palette=after.Palette;record.HasEye=after.HasEye;record.Eye=after.Eye; }
            next.Services++;next.Spent+=price;next.Receipts.Add(receipt);if(next.Receipts.Count>128)next.Receipts.RemoveAt(0);int bank=w.Bank;
            try{w.Barber=next;w.Bank-=price;save();changed=true;return true;}catch{w.Barber=old;w.Bank=bank;throw;}
        }
    }
}
