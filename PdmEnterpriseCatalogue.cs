using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Native;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed partial class PdmEnterpriseSystem
    {
        sealed class Offer{public string Model,Name;public int Cost,Mileage,Kind;public bool Available;}
        readonly List<Offer> basket=new List<Offer>();int basketDay=-1;
        readonly Dictionary<int,List<Offer>> offers=new Dictionary<int,List<Offer>>();int offersDay=-1;
        static readonly string[] categories={"Super","Esportivos","SUVs","Coupes","Muscle","Compactos","Off-road","Sedans"};
        static readonly VehicleClass[] classes={VehicleClass.Super,VehicleClass.Sports,VehicleClass.SUVs,VehicleClass.Coupes,VehicleClass.Muscle,VehicleClass.Compacts,VehicleClass.OffRoad,VehicleClass.Sedans};
        readonly Queue<KeyValuePair<int,VehicleHash>> modelQueue=new Queue<KeyValuePair<int,VehicleHash>>();
        void EnsureCatalogue()
        {
            if(offersDay==Data.Day)return;offers.Clear();modelQueue.Clear();offersDay=Data.Day;
            for(int kind=0;kind<classes.Length;kind++){offers[kind]=new List<Offer>();foreach(var hash in Vehicle.GetAllModelsOfClass(classes[kind]))modelQueue.Enqueue(new KeyValuePair<int,VehicleHash>(kind,hash));}
        }
        void CatalogueTick()
        {
            EnsureCatalogue();for(int batch=0;batch<8 && modelQueue.Count>0;batch++){
                var entry=modelQueue.Dequeue();var hash=entry.Value;int kind=entry.Key;
                string name=Enum.IsDefined(typeof(VehicleHash),hash)?hash.ToString().ToLowerInvariant():"#"+unchecked((uint)hash).ToString("X8");var model=PdmVehicleModel.Resolve(name);
                if(!model.IsValid || !model.IsInCdImage || !model.IsCar)continue;
                int seed=PdmEnterpriseRules.Seed(Data.Day+":"+name);int mileage=1000+seed%24001;string display=Game.GetLocalizedString(Vehicle.GetModelDisplayName(hash));if(string.IsNullOrWhiteSpace(display) || display=="NULL")display=hash.ToString();
                offers[kind].Add(new Offer{Model=name,Name=display,Kind=kind,Mileage=mileage,Cost=PdmEnterpriseRules.OfferPrice(kind,mileage,seed%101),Available=seed%100<40});
            }
        }
        List<Offer> Offers(int kind){EnsureCatalogue();return offers[kind].OrderBy(v=>v.Name).ToList();}
        public void OpenCatalogue()
        {
            Page("catalogue","PDM / FORNECEDOR POR CATEGORIA");
            ui.Add("Ofertas do dia empresarial "+Data.Day,"Valores/quilometragem disponiveis ate a proxima virada de dia ativo; apenas modelos instalados e validos do GTA Legacy. Pedidos sao pagos pelo caixa empresarial e aguardam retirada.",()=>{},false);
            Add("Carrinho / "+basket.Count+" unidades","Revisar pedido de uma a seis unidades e pagar todas em uma transacao.",OpenBasket);
            for(int k=0;k<categories.Length;k++){int kind=k;Add(categories[k],"Listar ofertas da categoria, disponibilidade, quilometragem e cotacao.",()=>OpenCategory(kind));}
            Add("Pedidos / retirada", "Pedidos pagos ja existentes, sem recomprar.",()=>CollectionMenu?.Invoke());Add("Voltar","Operacao.",OpenMenu);ui.Show();
        }
        void OpenCategory(int kind)
        {
            Page("catalogue:"+kind,categories[kind]+" / DIA "+Data.Day,OpenCatalogue,()=>OpenCategory(kind));
            if(modelQueue.Count>0)Add("Atualizar lista / carregando modelos",modelQueue.Count+" modelos ainda em verificacao; processamento distribuido entre frames.",()=>OpenCategory(kind));
            foreach(var offer in Offers(kind)){var o=offer;Add(o.Name+" / $"+o.Cost+(o.Available?"":" / indisponivel hoje"),"Modelo: "+o.Model+"; "+o.Mileage+" mi. Estoque da empresa "+Wallet.Pdm.Stock.Count+"/"+company.Capacity+". Escolha para revisar.",()=>ReviewOffer(o),o.Available);}
            Add("Voltar","Categorias.",OpenCatalogue);ui.Show();
        }
        void ReviewOffer(Offer offer)
        {
            Page("offer:"+offer.Model,offer.Name+" / $"+offer.Cost,()=>OpenCategory(offer.Kind),()=>ReviewOffer(offer));
            Add("Adicionar ao carrinho","Quilometragem "+offer.Mileage+" mi. Nao debita ao adicionar; revise e confirme o pedido completo.",()=>{if(basketDay!=Data.Day){basket.Clear();basketDay=Data.Day;}if(basket.Count>=6){Tell("Carrinho","Limite de seis unidades por pedido.");return;}basket.Add(offer);OpenBasket();});
            ui.AddConfirm("Encomendar somente este carro","Custo $"+offer.Cost+" pelo caixa da empresa; exige vaga e retirada no porto.",()=>OrderSingle(offer));
            Add("Voltar","Categoria.",()=>OpenCategory(offer.Kind));ui.Show();
        }
        void OpenBasket()
        {
            if(basketDay!=Data.Day){basket.Clear();basketDay=Data.Day;}
            Page("basket","PDM / CARRINHO DE PEDIDOS",OpenCatalogue,OpenBasket);
            for(int i=0;i<basket.Count;i++){int slot=i;var o=basket[i];Add((i+1)+". "+o.Name+" / $"+o.Cost,"Quilometragem "+o.Mileage+" mi. Selecionar remove do carrinho sem cobranca.",()=>{basket.RemoveAt(slot);OpenBasket();});}
            long total=basket.Sum(o=>(long)o.Cost);int day=Data.Day;ui.AddConfirm("Pagar pedido / $"+total,"Debita o caixa e grava todas as unidades juntas, ou nao grava/cobra nenhuma. Depois retire manualmente ou pelo coletor.",()=>{
                if(!office() || Data.Day!=day || basket.Count==0)return;var chosen=basket.ToList();ui.Stream(chosen.Select(o=>o.Model),()=>{
                    if(!office() || Data.Day!=day)return;var cars=chosen.Select(o=>Stock(o,Guid.NewGuid().ToString("N"))).ToList();
                    if(!PdmEnterpriseTransactions.OrderBatch(Wallet.EnterpriseWallet(),company,cars,Guid.NewGuid().ToString("N"),Wallet.Save,out bool changed)){ui.Problem("Pedido recusado","Confira caixa e vagas para todas as unidades. Nenhuma compra parcial.",null,OpenBasket);return;}
                    basket.Clear();if(changed)Tell("Pedido",cars.Count+" unidades pagas pelo caixa; aguardam retirada.");OpenBasket();
                },OpenBasket);
            },basket.Count>0);Add("Continuar comprando","Categorias do dia.",OpenCatalogue);Add("Voltar","Fornecedor.",OpenCatalogue);ui.Show();
        }
        void OrderSingle(Offer offer)
        {
            if(!office())return;
            ui.Confirm("Encomendar "+offer.Name,"Custo $"+offer.Cost+" pelo caixa. Quilometragem "+offer.Mileage+" mi; preco base de venda $"+PdmEnterpriseRules.SellingPrice(offer.Cost,Data.Reputation)+". Vai aguardar retirada manual ou pelo coletor.",()=>{
                if(!office() || !offer.Available)return;
                var cfg=JsonConvert.DeserializeObject<PdmOptions>(JsonConvert.SerializeObject(company));cfg.Catalog=new List<PdmOffer>{new PdmOffer{Id="daily",Name=offer.Name,Model=offer.Model,Cost=offer.Cost,Price=PdmEnterpriseRules.SellingPrice(offer.Cost,Data.Reputation)}};
                string id=Guid.NewGuid().ToString("N");
                if(!Wallet.PdmDailyOrder(cfg,id,offer.Mileage,Guid.NewGuid().ToString("N"),out bool changed)){ui.Problem("Encomenda recusada","Confira capacidade, caixa empresarial e operacao pendente. Nenhum pedido novo foi confirmado.",null,()=>OpenCategory(offer.Kind));return;}
                if(changed)Tell("Encomenda",offer.Name+" pago pelo caixa; aguarda retirada no porto.");OpenCategory(offer.Kind);
            },()=>OpenCategory(offer.Kind));
        }
        Offer RandomOffer(string salt)
        {
            int hash=PdmEnterpriseRules.Seed(salt);for(int step=0;step<8;step++){var list=Offers((hash%8+step)%8);if(list.Count>0)return list[hash%list.Count];}return null;
        }
        PdmStock Stock(Offer offer,string id)=>new PdmStock{Id=id,Model=offer.Model,Name=offer.Name,Plate="PD"+id.Substring(0,6).ToUpperInvariant(),Cost=offer.Cost,Price=PdmEnterpriseRules.SellingPrice(offer.Cost,Data.Reputation),Mileage=offer.Mileage,Engine=1000,Body=1000,Dirt=2,FuelFraction=.4f};
        void TrySource(bool manual=false)
        {
            if(!PdmEnterpriseRules.Idle(Wallet.Pdm) || Data.Seller!=null || !PdmEnterpriseRules.Paid(Wallet.Pdm,"collector")){if(manual)Tell("Coletor","Contrate/pague o coletor e conclua os contratos pendentes.");return;}
            var choices=Enumerable.Range(0,8).SelectMany(Offers).Where(o=>o.Available && o.Cost<=Data.ProcurementBudget && o.Cost<=Wallet.Pdm.Cash).ToList();
            if(choices.Count==0 || Wallet.Pdm.Stock.Count>=company.Capacity){if(manual)Tell("Coletor","Sem oferta dentro do orcamento/caixa ou sem vaga no estoque. Nenhum debito.");return;}
            string id=Guid.NewGuid().ToString("N");var offer=choices[PdmEnterpriseRules.Seed(id)%choices.Count];var stock=Stock(offer,id);
            if(PdmEnterpriseTransactions.BeginSource(Wallet.EnterpriseWallet(),company,stock,id,Wallet.Save,out bool changed) && changed)Tell("Coletor",offer.Name+" adquirido por $"+stock.Cost+"; entrega a caminho. Veja o extrato.");
        }
    }
}
