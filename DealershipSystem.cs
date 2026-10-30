using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
namespace RPCore
{
    public class DealershipSystem : ISystem
    {
        public string Name=>"dealership";
        readonly ObjectPool pool;
        readonly NativeMenu menu=new NativeMenu("RPCore Motors","");
        readonly PersonalServiceMenu ui;
        readonly AchievementSystem ach;
        readonly ShiftGate gate;
        readonly DealershipDoorRuntime doors=new DealershipDoorRuntime();
        readonly List<VehicleOffer> offers;
        readonly List<GarageDefinition> garages;
        readonly List<Delivered> created=new List<Delivered>();
        readonly Random random=new Random();
        readonly Blip blip;
        readonly Vector3 entrance=new Vector3(-56.8f,-1096.7f,26.4f),delivery=new Vector3(-28.5f,-1083.5f,26.6f);
        string search="";bool byPrice,lease;
        Purchase pending;
        sealed class Delivered {public Vehicle Car;public long Address;public string Key;public int Model;}
        sealed class Purchase
        {
            public VehiclePurchase Quote;public VehicleOffer Offer;public GarageDefinition Garage;
            public Model Model;public TransportData Transport;public string Profile;public int Actor,ActorModel,Start;public long Address;
        }
        bool Safe=>Cfg.EnableGarages && PersonalServiceMenu.SafeFoot && !InterfaceOptions.ModalOpen;
        bool Nearby=>Safe && Game.Player.Character.Position.DistanceTo(entrance)<20;
        public DealershipSystem(ObjectPool pool,AchievementSystem ach,ShiftGate gate)
        {
            this.pool=pool;this.ach=ach;this.gate=gate;
            offers=Store.LoadDef("Dealership/Vehicles.json",DefaultOffers).Where(o=>o!=null && !string.IsNullOrWhiteSpace(o.Model) && o.Price>0).ToList();
            garages=Store.LoadDef("Garages/Locations.json",GarageSystem.DefaultGarages).Where(GarageSystem.ValidDefinition).GroupBy(g=>g.Id).Select(g=>g.First()).ToList();
            ui=new PersonalServiceMenu(menu,ex=>{CancelPurchase();ui.Close();Log.Error("dealership",ex);Notify.Show("Compra interrompida. Confira carteira e garagem antes de tentar novamente.");});
            pool.Add(menu);Visuals.Register(menu,"dealership");blip=MapBlips.Create(entrance,"RPCore Motors",BlipSprite.PersonalVehicleCar,BlipColor.Blue,.8f);
        }
        public static List<VehicleOffer> DefaultOffers()=>new List<VehicleOffer>{
            new VehicleOffer{Model="faggio",Name="Pegassi Faggio",Price=3500},
            new VehicleOffer{Model="asea",Name="Declasse Asea",Price=12000},
            new VehicleOffer{Model="blista",Name="Dinka Blista",Price=18000},
            new VehicleOffer{Model="dominator",Name="Vapid Dominator",Price=45000},
            new VehicleOffer{Model="sultan",Name="Karin Sultan",Price=55000}};
        void Add(string title,string description,Action action,bool enabled=true)=>ui.Add(title,description,action,enabled);
        void Root()
        {
            ui.Begin("root");menu.Name="Concessionaria / carteira $"+Wallet.Cash;
            Add("GPS: RPCore Motors","Visite a concessionaria para comprar; entrega no patio externo.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,entrance.X,entrance.Y);ui.Close();});
            Add("Catalogo / "+offers.Count+" ofertas","Compra pela carteira. Escolha o modelo e uma garagem propria com vaga.",()=>Catalog(0),Nearby);
            Add("Garagem e entrega","Veiculo entregue fora da loja, ja registrado na garagem escolhida. Seguro e separado e gratuito; a franquia e paga pelo banco.",()=>Notify.Show(Cfg.GarageKey+": garagens. Seguro: menu de servicos do RPCore."));
            Add("Fechar","Voltar ao jogo.",ui.Close);menu.Visible=true;
        }
        void Catalog(int page)
        {
            if(!Nearby){Root();return;}
            var filtered=offers.Where(o=>(o.Name??o.Model).IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0 || o.Model.IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0);
            var list=(byPrice?filtered.OrderBy(o=>o.Price).ThenBy(o=>o.Name??o.Model):filtered.OrderBy(o=>o.Name??o.Model)).ToList();
            int pages=Math.Max(1,(list.Count+9)/10);page=Math.Max(0,Math.Min(page,pages-1));int selectedPage=page;
            ui.Begin("catalog:"+page,Root);menu.Name="Catalogo "+(page+1)+"/"+pages+" / $"+Wallet.Cash;
            Add("Buscar: "+(search.Length==0?"todos":search),"Buscar por nome ou modelo. Texto vazio remove o filtro.",()=>{string value=Game.GetUserInput(WindowTitle.EnterMessage60,search,40);if(value!=null && Nearby){search=value.Trim();Catalog(0);}});
            Add("Ordenar: "+(byPrice?"menor preco":"nome"),"Alternar ordenacao do catalogo configurado.",()=>{byPrice=!byPrice;Catalog(0);});
            foreach(var offer in list.Skip(page*10).Take(10)){var o=offer;Add((o.Name??o.Model)+" / $"+o.Price,"Modelo: "+o.Model+". Escolher garagem e revisar antes do pagamento.",()=>Garages(o,selectedPage));}
            if(list.Count==0)Add("Nenhuma oferta encontrada","Altere o filtro de busca.",()=>{},false);
            if(page>0)Add("Pagina anterior","Ofertas anteriores.",()=>Catalog(selectedPage-1));
            if(page+1<pages)Add("Proxima pagina","Mais ofertas.",()=>Catalog(selectedPage+1));
            Add("Voltar","Concessionaria.",Root);menu.Visible=true;
        }
        void Garages(VehicleOffer offer,int page)
        {
            if(!Nearby){Root();return;}var data=Wallet.Transport;
            ui.Begin("garages:"+offer.Model,()=>Catalog(page));menu.Name=(offer.Name??offer.Model)+" / $"+offer.Price;
            foreach(var g in garages.Where(g=>data.Garages.Contains(g.Id))){var garage=g;int free=Math.Max(0,g.Capacity-data.Vehicles.Count(v=>v.GarageId==g.Id));Add(g.Name+" / "+free+" vagas","Registrar nesta garagem. Veiculo entregue no patio externo da concessionaria.",()=>Confirm(offer,garage,page),free>0);}
            if(!garages.Any(g=>data.Garages.Contains(g.Id)))Add("Compre uma garagem primeiro",Cfg.GarageKey+" abre garagens. Nenhuma cobranca.",()=>{},false);
            Add("Voltar","Catalogo.",()=>Catalog(page));menu.Visible=true;
        }
        Purchase Make(VehicleOffer offer,GarageDefinition garage)
        {
            var ped=Game.Player.Character;var model=new Model(offer.Model);var data=Wallet.Transport;
            return new Purchase{Offer=offer,Garage=garage,Model=model,Transport=data,Profile=Profile.Id,Actor=ped.Handle,ActorModel=ped.Model.Hash,Address=ped.MemoryAddress.ToInt64(),Quote=new VehiclePurchase(data,Wallet.Cash,model.Hash,offer.Price,garage.Id,garage.Capacity)};
        }
        bool Context(Purchase p)
        {
            var ped=Game.Player.Character;
            return Nearby && p.Profile==Profile.Id && p.Actor==ped.Handle && p.ActorModel==ped.Model.Hash && p.Address!=0 && p.Address==ped.MemoryAddress.ToInt64() && p.Quote.Current(Wallet.TransportReference,Wallet.Cash);
        }
        void Confirm(VehicleOffer offer,GarageDefinition garage,int page)
        {
            if(!Nearby)return;var p=Make(offer,garage);
            ui.Begin("confirm",()=>Garages(offer,page),false);menu.Name="Confirmar compra / $"+offer.Price;
            Add("Cancelar","Nenhum pagamento. Voltar a escolha da garagem.",()=>Garages(offer,page));
            Add("Comprar "+(offer.Name??offer.Model),"Carteira $"+Wallet.Cash+" -> $"+Math.Max(0,Wallet.Cash-offer.Price)+". Garagem: "+garage.Name+". O patio deve estar livre; sem seguro automatico.",()=>Start(p),p.Quote.Current(Wallet.TransportReference,Wallet.Cash));
            menu.Visible=true;
        }
        bool FreeDelivery=>World.GetNearbyVehicles(delivery,5).Length==0 && World.GetNearbyPeds(delivery,2).Length==0;
        void Start(Purchase p)
        {
            if(pending!=null || !Context(p)){Notify.Show("Saldo, garagem ou contexto mudou. Confira a compra novamente.");Root();return;}
            if(!p.Model.IsValid || !p.Model.IsInCdImage || !p.Model.IsVehicle || !(p.Model.IsCar || p.Model.IsBike || p.Model.IsQuadBike) || p.Model.IsBicycle){Notify.Show("Modelo indisponivel; nenhuma cobranca.");return;}
            if(!FreeDelivery){Notify.Show("Patio de entrega ocupado. Afaste o veiculo/aguarde os pedestres; nenhuma cobranca.");return;}
            if(!gate.Acquire(Name)){Notify.Show("Encerre a atividade de "+gate.Owner+" antes de comprar.");return;}
            lease=true;pending=p;p.Start=Game.GameTime;
            Function.Call(Hash.REQUEST_MODEL,p.Model.Hash);
            ui.Begin("loading",()=>{CancelPurchase();Root();},false);menu.Name="Preparando entrega";
            Add("Cancelar compra","Carregando "+(p.Offer.Name??p.Offer.Model)+". Nada foi cobrado. Prazo de carregamento: 10 segundos.",()=>{CancelPurchase();Root();});menu.Visible=true;
        }
        void Finish(Purchase p)
        {
            Vehicle car=null;long address=0;bool paid=false;
            try{
                if(!Context(p) || !FreeDelivery){Notify.Show("Compra cancelada: saldo, vaga, contexto ou patio mudou. Nada cobrado.");return;}
                car=World.CreateVehicle(p.Model,delivery,70);
                if(car==null || !car.Exists())throw new InvalidOperationException("Vehicle creation failed.");
                address=car.MemoryAddress.ToInt64();if(address==0)throw new InvalidOperationException("Vehicle identity unavailable.");
                var worldKeys=new HashSet<string>(World.GetAllVehicles().Where(v=>v!=null && v.Exists()).Select(v=>VehicleIdentity.Key(v.Model.Hash,Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT,v))));
                string plate=null;
                for(int i=0;i<100;i++){string candidate="RP"+random.Next(100000,1000000);string key=VehicleIdentity.Key(p.Model.Hash,candidate);if(p.Transport.FindIdentity(p.Model.Hash,candidate)==null && !p.Transport.Tanks.ContainsKey(key) && !worldKeys.Contains(key)){plate=candidate;break;}}
                if(plate==null)throw new InvalidOperationException("Unique plate unavailable.");
                Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT,car,plate);
                if(VehicleIdentity.Key(car.Model.Hash,Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT,car))!=VehicleIdentity.Key(p.Model.Hash,plate))throw new InvalidOperationException("Plate readback failed.");
                float capacity=car.PetrolTankVolume;capacity=VehicleInsuranceRules.Finite(capacity) && capacity>0 && capacity<=10000?capacity:60;
                car.FuelLevel=capacity;Function.Call(Hash.SET_VEHICLE_ENGINE_ON,car,false,true,false);car.IsPersistent=true;
                var record=VehicleSnapshot.Capture(car);record.GarageId=p.Garage.Id;record.Name=p.Offer.Name??p.Offer.Model;
                if(!Context(p) || !Wallet.PurchaseVehicle(p.Quote,record,capacity)){Notify.Show("Compra recusada; confira carteira/vaga. Nada cobrado.");return;}
                paid=true;created.Add(new Delivered{Car=car,Address=address,Key=VehicleIdentity.Key(record.Model,record.Plate),Model=record.Model});
                try{ach.Bump("cars_bought");ach.Save();}catch(Exception ex){Log.Error("dealership achievement after purchase",ex);}
                Notify.Show("Comprado: "+record.Name+" / $"+p.Offer.Price+". Retire no patio. Garagem: "+p.Garage.Name+".");
            }finally{
                try{if(!paid && car!=null && car.Exists() && address!=0 && car.MemoryAddress.ToInt64()==address && car.Model.Hash==p.Model.Hash && car.Occupants.Length==0 && !Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,car))car.Delete();}
                finally{CancelPurchase();ui.Close();}
            }
        }
        void CancelPurchase()
        {
            var p=pending;pending=null;
            try{if(p!=null)p.Model.MarkAsNoLongerNeeded();}finally{if(lease){lease=false;gate.Release(Name);}}
        }
        public void Tick()
        {
            doors.Tick();
            ui.Tick(Safe);
            try{
                var p=pending;
                if(p!=null){
                    if(!menu.Visible || !Context(p) || gate.Owner!=Name || unchecked((uint)(Game.GameTime-p.Start))>=10000){CancelPurchase();ui.Close();Notify.Show("Compra interrompida antes do pagamento.");}
                    else if(p.Model.IsLoaded)Finish(p);
                }
            }catch(Exception ex){CancelPurchase();ui.Close();Log.Error("dealership delivery",ex);Notify.Show("Entrega interrompida. Confira carteira e garagem; detalhes no RPCore.log.");}
            if(Nearby && !pool.AreAnyVisible && gate.Owner==null)Prompt.Help("RPCore Motors: "+Cfg.DealerKey+" carros pessoais. Empresa PDM: ~INPUT_CONTEXT~ no marcador externo.");
            if(Cfg.EnableGarages && !Game.IsMissionActive && Game.Player.Character.Position.DistanceTo(entrance)<35)World.DrawMarker(MarkerType.Cylinder,entrance-new Vector3(0,0,1),Vector3.Zero,Vector3.Zero,new Vector3(2,2,1),Color.FromArgb(90,80,200,100));
        }
        public void OnKey(KeyEventArgs e)
        {
            if(e.KeyCode!=Cfg.DealerKey || e.Modifiers!=Keys.None)return;
            if(menu.Visible){CancelPurchase();ui.Close();}else if(!pool.AreAnyVisible && Safe)Root();
        }
        void ReleaseCreated()
        {
            foreach(var item in created){var car=item.Car;if(car!=null && car.Exists() && car.MemoryAddress.ToInt64()==item.Address && car.Model.Hash==item.Model && Key(car)==item.Key && car.Occupants.Length==0 && !Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,car))car.MarkAsNoLongerNeeded();}created.Clear();
        }
        static string Key(Vehicle v)=>VehicleIdentity.Key(v.Model.Hash,Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT,v));
        public void Reload(){CancelPurchase();ui.Reset();ReleaseCreated();doors.Release();search="";}
        public void Save(){}
        public void Dispose(){Reload();if(blip!=null && blip.Exists())blip.Delete();}
    }
}
