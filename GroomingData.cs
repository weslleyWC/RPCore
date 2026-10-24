using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GroomingOptions : ExtensibleData
    {
        public int Price = 120;
        [JsonIgnore] public bool Valid => Price>=0 && Price<=10000;
    }
    public static class GroomingDefaults
    {
        public static GroomingOptions Load()
        {using(var s=typeof(GroomingDefaults).Assembly.GetManifestResourceStream("RPCore.JobDefaults.Grooming.json"))using(var r=new System.IO.StreamReader(s))return JsonConvert.DeserializeObject<GroomingOptions>(r.ReadToEnd());}
    }
    public sealed class GroomingLook : ExtensibleData
    {
        public int Model;
        public bool Hair,Beard,Brows;
        public int HairColor,Highlight,BeardStyle=255,BeardOpacity=100,BeardColor,BrowsStyle=255,BrowsOpacity=100,BrowsColor;
        [JsonIgnore] public bool Valid => Model!=0 && (Hair || Beard || Brows) && HairColor>=0 && HairColor<=63 && Highlight>=0 && Highlight<=63 && BeardStyle>=0 && BeardStyle<=255 && BeardOpacity>=0 && BeardOpacity<=100 && BeardColor>=0 && BeardColor<=63 && BrowsStyle>=0 && BrowsStyle<=255 && BrowsOpacity>=0 && BrowsOpacity<=100 && BrowsColor>=0 && BrowsColor<=63;
    }
    public sealed class GroomingOrder : ExtensibleData
    {
        public string Id;
        public int Price;
        public bool Started;
        public GroomingLook Look;
        [JsonIgnore] public bool Valid => !string.IsNullOrWhiteSpace(Id) && Id.Length<=100 && Price>=0 && Price<=10000 && Look!=null && Look.Valid;
    }
    public sealed class GroomingData : ExtensibleData
    {
        public List<GroomingLook> Looks=new List<GroomingLook>();
        public List<string> Receipts=new List<string>();
        public GroomingOrder Pending;
        public int Services;
        public long Spent;
        [JsonIgnore] public bool Valid => Looks!=null && Looks.Count<=32 && Looks.All(l=>l!=null && l.Valid) && Looks.Select(l=>l.Model).Distinct().Count()==Looks.Count && Receipts!=null && Receipts.Count<=256 && Receipts.All(r=>!string.IsNullOrWhiteSpace(r) && r.Length<=120) && Receipts.Distinct().Count()==Receipts.Count && Services>=0 && Spent>=0 && (Pending==null || Pending.Valid && Services>0 && Spent>=Pending.Price && (Looks.Count<32 || Looks.Any(l=>l.Model==Pending.Look.Model)));
    }
    public static class GroomingTransactions
    {
        static T Copy<T>(T value)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        static bool Apply(WalletData w,string receipt,Func<GroomingData,int?> mutation,Action save,out bool changed)
        {
            changed=false;if(w==null || w.Bank<0 || string.IsNullOrWhiteSpace(receipt) || receipt.Length>120 || save==null || w.Grooming!=null && !w.Grooming.Valid)return false;
            var old=w.Grooming;if(old?.Receipts.Contains(receipt)==true)return true;
            var next=old==null?new GroomingData():Copy(old);int? delta=mutation(next);
            if(delta==null || (long)w.Bank+delta.Value<0 || (long)w.Bank+delta.Value>int.MaxValue || !next.Valid)return false;
            next.Receipts.Add(receipt);if(next.Receipts.Count>256)next.Receipts.RemoveAt(0);int bank=w.Bank;
            try{w.Grooming=next;w.Bank+=delta.Value;save();changed=true;return true;}catch{w.Grooming=old;w.Bank=bank;throw;}
        }
        public static GroomingLook Merge(GroomingLook old,GroomingLook patch)
        {
            if(patch==null || !patch.Valid || old!=null && (!old.Valid || old.Model!=patch.Model))throw new ArgumentException();
            var result=old==null?Copy(patch):Copy(old);
            if(patch.Hair){result.Hair=true;result.HairColor=patch.HairColor;result.Highlight=patch.Highlight;}
            if(patch.Beard){result.Beard=true;result.BeardStyle=patch.BeardStyle;result.BeardOpacity=patch.BeardOpacity;result.BeardColor=patch.BeardColor;}
            if(patch.Brows){result.Brows=true;result.BrowsStyle=patch.BrowsStyle;result.BrowsOpacity=patch.BrowsOpacity;result.BrowsColor=patch.BrowsColor;}
            return result;
        }
        public static bool SameSelected(GroomingLook known,GroomingLook patch)
        { return known!=null && patch!=null && known.Valid && patch.Valid && known.Model==patch.Model && (!patch.Hair || known.Hair && known.HairColor==patch.HairColor && known.Highlight==patch.Highlight) && (!patch.Beard || known.Beard && known.BeardStyle==patch.BeardStyle && known.BeardOpacity==patch.BeardOpacity && known.BeardColor==patch.BeardColor) && (!patch.Brows || known.Brows && known.BrowsStyle==patch.BrowsStyle && known.BrowsOpacity==patch.BrowsOpacity && known.BrowsColor==patch.BrowsColor); }
        public static bool Begin(WalletData w,GroomingLook look,int price,string id,Action save,out bool changed)
        {
            changed=false;if(look==null || !look.Valid || price<0 || price>10000 || string.IsNullOrWhiteSpace(id) || id.Length>100)return false;
            return Apply(w,id+":begin",d=>{
                if(d.Pending!=null || SameSelected(d.Looks.Find(l=>l.Model==look.Model),look) || d.Services==int.MaxValue || d.Spent>long.MaxValue-price || d.Looks.Count>=32 && !d.Looks.Any(l=>l.Model==look.Model))return null;
                d.Pending=new GroomingOrder{Id=id,Look=Copy(look),Price=price};d.Services++;d.Spent+=price;return -price;
            },save,out changed);
        }
        public static bool Start(WalletData w,string id,Action save,out bool changed)
        {return Apply(w,id+":start",d=>{if(d.Pending==null || d.Pending.Id!=id)return null;d.Pending.Started=true;return 0;},save,out changed);}
        public static bool Complete(WalletData w,string id,Action save,out bool changed)
        {
            return Apply(w,id+":complete",d=>{
                var p=d.Pending;if(p==null || p.Id!=id || !p.Started)return null;
                var old=d.Looks.Find(l=>l.Model==p.Look.Model);var result=Merge(old,p.Look);
                if(old==null)d.Looks.Add(result);else d.Looks[d.Looks.IndexOf(old)]=result;d.Pending=null;return 0;
            },save,out changed);
        }
        public static bool Refund(WalletData w,string id,Action save,out bool changed)
        {return Apply(w,id+":refund",d=>{var p=d.Pending;if(p==null || p.Id!=id || p.Started)return null;int amount=p.Price;d.Spent-=amount;d.Services--;d.Pending=null;return amount;},save,out changed);}
        public static bool Abandon(WalletData w,string id,Action save,out bool changed)
        {return Apply(w,id+":abandon",d=>{if(d.Pending==null || d.Pending.Id!=id || !d.Pending.Started)return null;d.Pending=null;return 0;},save,out changed);}
    }
}
