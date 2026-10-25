using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
namespace RPCore
{
    internal sealed class GangBulk
    {
        const string Owner="gang_bulk",Decor="RPCore_BulkOwner";
        readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Func<IEnumerable<GangDefinition>> gangs;readonly Func<IEnumerable<GangTurfDefinition>> areas;
        readonly GangBulkOptions options;readonly GangAutoArms autoArms;readonly GangArmsOptions arms;readonly GangWarehouseOptions warehouse;readonly NativeMenu menu;
        readonly Random random=new Random();readonly DeliveryCargo cargo=new DeliveryCargo();readonly GangRaidSearch handover=new GangRaidSearch();
        Vehicle vehicle;Ped buyer;Model buyerModel;Blip objective;int model,player,token,last,elapsed,prepareAt;string plate,profile,id;bool reserved,carrying,arrived,preparing;
        public DealerJob Dealer;public bool Active {get;private set;}
        GangBulkAttempt Pending=>Wallet.GangBulk?.Pending;
        GangTurfDefinition CurrentArea=>Pending==null?null:areas().FirstOrDefault(a=>a.Id==Pending.Area.Id);
        bool Owned=>Pending!=null && GangCrewRules.SameGang(Pending.Gang,state().Gang) && GangBusinessTransactions.Owns(state(),CurrentArea) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang));
        bool Safe=>Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.Player.Character.IsInCombat && Game.Player.Wanted.WantedLevel==0 && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool Input=>Safe && !Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
        bool Car=>vehicle!=null && vehicle.Exists() && vehicle.Model.Hash==model && vehicle.Mods.LicensePlate==plate && vehicle.IsDriveable && !vehicle.IsDead;
        bool Stopped=>Car && vehicle.Speed<.5f && (vehicle.Driver==null || !vehicle.Driver.Exists() || vehicle.Driver==Game.Player.Character);
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        static Vector3 Pos(GangTurfDefinition p)=>new Vector3(p.Center.X,p.Center.Y,p.Center.Z);
        Vector3 Rear=>vehicle.GetOffsetPosition(new Vector3(0,-3.2f,0));
        static bool ModelValid(string name){if(!GangPresenceRules.ModelName(name))return false;var m=new Model(name);return m.IsInCdImage && m.IsPed;}
        bool OwnBuyer=>buyer!=null && buyer.Exists() && buyer.Handle!=Game.Player.Character.Handle && Function.Call<bool>(Hash.DECOR_EXIST_ON,buyer,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,buyer,Decor)==token;
        public GangBulk(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs,Func<IEnumerable<GangTurfDefinition>> areas):this(gate,pool,state,gangs,areas,null) {}
        public GangBulk(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs,Func<IEnumerable<GangTurfDefinition>> areas,GangAutoArms autoArms)
        {
            this.autoArms=autoArms;this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;this.areas=areas;
            options=Store.LoadDef("Gangs/Bulk.json",()=>new GangBulkOptions());if(options?.Valid!=true){options=new GangBulkOptions();Log.Write("Invalid bulk config; defaults in memory, original preserved.");}
            warehouse=Store.LoadDef("Gangs/Warehouse.json",()=>new GangWarehouseOptions());if(warehouse?.Valid!=true)warehouse=new GangWarehouseOptions();
            arms=Store.LoadDef("Gangs/Arms.json",()=>new GangArmsOptions());if(arms?.Valid!=true){arms=new GangArmsOptions();Log.Write("Invalid arms config; defaults in memory, original preserved.");}
            menu=new NativeMenu("Venda em lote","ARMAZEM / COMPRADOR / ENTREGA");pool.Add(menu);Visuals.Register(menu,"gangs");
        }
        void Add(string title,string description,Action action=null){var row=new NativeItem(title,description);row.Activated+=(s,e)=>{try{action?.Invoke();}catch(Exception ex){Suspend();Log.Error("gang bulk",ex);Notify.Show("Venda suspensa; contrato e valores confirmados conservados.");}};menu.Add(row);}
        public void Controls(Action<string,string,Action> add,Action reopen)
        {
            add("Lotes vendidos: "+(Wallet.GangBulk?.Completed??0)+" / receita suja $"+(Wallet.GangBulk?.Revenue??0),"Venda em lote usa estoque local, sem pagamento direto no banco/cash.",null);
            var p=Pending;if(p==null)return;
            add((p.Kind==GangBulkKind.Arms?"Armas pendentes: ":"Lote pendente: ")+p.Units+"x "+p.Product+" / "+p.Buyer+" / $"+p.Value,p.Ready?"Entrega fisica confirmada. Retome a pe no encontro para E finalizar receita/estoque.":"Estoque comprometido fica no livro ate finalizar. Retomada parcial refaz carga na sua area, com mesmo preco/destino/comprador.",null);
            add("GPS do lote pendente",p.Ready?"Volte ao encontro para finalizar a entrega pronta.":"Volte a sua area para retomar com carro e refazer a carga parcial.",()=>{var target=p.Ready?Pos(p.Site):Pos(p.Area);Function.Call(Hash.SET_NEW_WAYPOINT,target.X,target.Y);pool.HideAll();});
            add(Active?"Suspender venda em lote":"Retomar venda em lote","END suspende. Parcial: seu carro parado na area. Entrega confirmada: a pe no encontro. Sem rerrolar relacao ou criar estoque.",()=>{if(Active){Suspend();reopen();}else if(!Resume())Notify.Show("Confira posse, catalogo, seguranca, carro na area ou ponto de entrega pronta.");});
            add("Cancelar lote pendente","Desbloqueia estoque que ainda nao foi debitado. Sem dinheiro/estorno. Disponivel mesmo apos perder posse/filiacao.",()=>{if(!Safe || Game.IsPaused || InterfaceOptions.ModalOpen || gate.Owner!=null && gate.Owner!=Owner || !Wallet.GangBulkCancel(p.Id,out _)){Notify.Show("Cancelamento nao confirmado; dados conservados.");return;}Suspend();reopen();});
        }
        public void Open(GangTurfDefinition area)
        {
            pool.HideAll();menu.Clear();var c=Dealer?.Catalog;
            if(!GangBusinessTransactions.Owns(state(),area) || c?.Valid!=true || Wallet.GangWarehouse?.Valid!=true || Wallet.GangBulkBlocked){Add("Venda indisponivel","Confira posse, catalogo, estoque e contrato pendente no comercio principal.");menu.Visible=true;return;}
            var a=GangWarehouseTransactions.Account(Wallet.GangWarehouse,state().Gang,area.Id);menu.Name=area.Name;
            Add("Lote: 150% do preco Dealer","Valor total ate $"+options.MaxValue+". Debito de estoque e receita suja somente na confirmacao final. Usa seu carro/van, sem veiculo fornecido.");
            int count=0;foreach(var product in c.Products.Where(v=>v.Id.Length<=80))foreach(int amount in options.Quantities)
            {
                int units=amount;var p=product;int value=GangBulkTransactions.Quote(c,p.Id,units);
                if(value==0 || value>options.MaxValue || a==null || GangWarehouseAccount.Get(a.Stock,p.Id)<units)continue;count++;
                Add(units+"x "+p.Name+" / $"+value,"Escolha a gangue compradora. No seu carro/van parado na area, carregar caixa, dirigir e entregar ao comprador.",()=>ChooseBuyer(area,p.Id,units));
            }
            if(count==0)Add("Nenhum lote disponivel","Abasteca o armazem com frete, deposito ou roubo. Quantidade minima padrao25; confira teto de valor.");
            Add("Fechar","Voltar ao jogo.",()=>menu.Visible=false);menu.Visible=true;
        }
        public void OpenArms(GangTurfDefinition area)
        {
            pool.HideAll();menu.Clear();var own=state().Gang;string currentProfile=Profile.Id;
            if(!GangBusinessTransactions.Owns(state(),area) || Wallet.GangArms!=null && !Wallet.GangArms.Valid){Add("Arsenal indisponivel/protegido","Confira filiacao/posse e dados; estoque anterior conservado.");menu.Visible=true;return;}
            autoArms?.Append(area,Add,()=>OpenArms(area));
            var a=GangArmsTransactions.Account(Wallet.GangArms,own,area.Id);menu.Name="Arsenal / "+area.Name;
            int limit=Math.Min(150,Math.Max(1,areas().Count(t=>GangBusinessTransactions.Owns(state(),t)))*10);
            Add("Banco $"+Wallet.Bank+" / limite de entrega "+limit,"Estoque comercial separado de armas pessoais. Municao usa uma vaga por100 unidades, arredondando para cima. Compra paga na area; venda fisica usa seu carro.");
            Add("Comprado $"+(a?.Spent??0)+" / vendido $"+(a?.Revenue??0),"Compra e uma mecanica propria do RPCore. Precos de venda seguem cinco categorias PSRP. Nada equipa o jogador ou retira suas armas.");
            foreach(var product in arms.Products)
            {
                var p=product;int stock=a==null?0:GangWarehouseAccount.Get(a.Stock,p.Id);
                Add(p.Name+": "+stock+"/"+arms.MaxStock,"Comprado "+(a==null?0:GangWarehouseAccount.Get(a.Purchased,p.Id))+" / recuperado "+(a==null?0:GangWarehouseAccount.Get(a.Recovered,p.Id))+" / vendido "+(a==null?0:GangWarehouseAccount.Get(a.Sold,p.Id))+". Compra $"+p.Buy+"/un, venda $"+p.Sell+"/un sem multiplicador.");
                foreach(int amount in arms.Delivery.Quantities)
                {
                    int units=amount;string receipt=Guid.NewGuid().ToString("N");
                    if(stock<=arms.MaxStock-units)Add("Comprar "+units+" / "+p.Name+" / $"+((long)p.Buy*units),"Selecionar confirma compra. A pe na area, sem procurado/atividade/contratos. Debita banco e credita apenas arsenal comercial.",()=>{
                        var actor=Game.Player.Character;
                        if(currentProfile!=Profile.Id || !GangCrewRules.SameGang(own,state().Gang) || !Safe || Game.IsPaused || InterfaceOptions.ModalOpen || gate.Owner!=null || !actor.IsOnFoot || actor.IsRagdoll || actor.Position.DistanceTo(Pos(area))>warehouse.Radius || !Wallet.GangArmsBuy(state(),area,arms,p.Id,units,receipt,out _))Notify.Show("Compra recusada. Confira area a pe, banco, capacidade e pendencias.");OpenArms(area);
                    });
                    int value=GangArmsTransactions.Quote(arms,p.Id,units);if(stock<units || value==0 || (p.Id=="ammo"?(units+99)/100:units)>limit)continue;
                    Add("Entregar "+units+" / "+p.Name+" / $"+value,"Confirma contrato de armas: carro parado na area, caixa/traseira/viagem/comprador/E final. Receita suja; sem sorteio de relacao.",()=>{if(currentProfile!=Profile.Id || !GangCrewRules.SameGang(own,state().Gang) || !BeginArms(area,p.Id,units,limit))Notify.Show("Confira carro parado na area, posse, arsenal e contratos/atividade.");});
                }
            }
            if(a!=null && a.Stock.Any(p=>p.Value>0 && !arms.Products.Any(v=>v.Id==p.Key)))Add("Produtos fora do catalogo conservados","Restaure o mesmo ID no Arms.json para gerenciar; nada e convertido ou apagado.");
            Add("GPS da area","Compra presencial no centro da area.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,area.Center.X,area.Center.Y);menu.Visible=false;});
            Add("Fechar","Voltar ao jogo.",()=>menu.Visible=false);menu.Visible=true;
        }
        bool BeginArms(GangTurfDefinition area,string product,int units,int limit)
        {
            if(Active || Pending!=null || gate.Owner!=null || !gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang)) || !GangBusinessTransactions.Owns(state(),area) || !Bind(area))return false;
            var sites=arms.Delivery.Sites.Where(p=>Pos(p).DistanceTo(Pos(area))>=1200).ToList();if(sites.Count==0)sites=arms.Delivery.Sites;
            string customer=GangCrewRules.SameGang(state().Gang,"Comprador particular")?"Cliente de armas":"Comprador particular";
            var attempt=new GangBulkAttempt{Kind=GangBulkKind.Arms,ArmsLimit=limit,Id=Guid.NewGuid().ToString("N"),Gang=state().Gang,Buyer=customer,Product=product,Units=units,Value=GangArmsTransactions.Quote(arms,product,units),Model="a_m_m_bevhills_02",Area=CharacterRules.Copy(area),Site=CharacterRules.Copy(sites[random.Next(sites.Count)]),Options=CharacterRules.Copy(arms.Delivery)};
            if(!gate.Acquire(Owner))return false;try{if(!Wallet.GangArmsBegin(state(),attempt,arms,out bool changed) || !changed){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}
        }
        void ChooseBuyer(GangTurfDefinition area,string product,int units)
        {
            pool.HideAll();menu.Clear();string own=state().Gang,currentProfile=Profile.Id;menu.Name="Escolher comprador";
            foreach(var g in gangs().Where(g=>!GangCrewRules.SameGang(g.Name,own) && (g.PedModels??Array.Empty<string>()).Any(ModelValid)))
            {var rival=g;Add(rival.Name+" / "+GangDiplomacyRules.Label(GangDiplomacyRules.Attitude(Wallet.GangDiplomacy,own,g)),"Confirma contrato. Chance30% de melhorar um nivel ao finalizar. Sem custo; estoque local comprometido ate concluir/cancelar.",()=>{if(currentProfile!=Profile.Id || !GangCrewRules.SameGang(own,state().Gang) || !Begin(area,product,units,rival))Notify.Show("Confira posse, estoque, dados/pendencias e seu carro parado na area, sem procurado/atividade.");});}
            Add("Voltar aos lotes","Revisar produto/quantidade.",()=>Open(area));menu.Visible=true;
        }
        bool Bind(GangTurfDefinition area)
        {
            var p=Game.Player.Character;var v=p.CurrentVehicle;
            if(!Safe || Game.IsPaused || InterfaceOptions.ModalOpen || v==null || !v.Exists() || !v.Model.IsCar || v.Driver!=p || v.Speed>=.5f || v.IsDead || !v.IsDriveable || JobVehicles.Contains(v) || p.Position.DistanceTo(Pos(area))>warehouse.Radius+5)return false;
            vehicle=v;model=v.Model.Hash;plate=v.Mods.LicensePlate;return true;
        }
        bool Begin(GangTurfDefinition area,string product,int units,GangDefinition rival)
        {
            if(Active || Pending!=null || gate.Owner!=null || !gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang)) || !GangBusinessTransactions.Owns(state(),area) || Dealer?.Catalog?.Valid!=true || !Bind(area))return false;
            var sites=options.Sites.Where(p=>Pos(p).DistanceTo(Pos(area))>=1200).ToList();if(sites.Count==0)sites=options.Sites;
            var attempt=new GangBulkAttempt{Id=Guid.NewGuid().ToString("N"),Gang=state().Gang,Buyer=rival.Name,Product=product,Units=units,Value=GangBulkTransactions.Quote(Dealer.Catalog,product,units),Model=rival.PedModels.First(ModelValid),InitialAttitude=GangDiplomacyRules.Initial(rival,state().Gang),RelationRoll=random.Next(100),Area=CharacterRules.Copy(area),Site=CharacterRules.Copy(sites[random.Next(sites.Count)]),Options=CharacterRules.Copy(options)};
            if(!gate.Acquire(Owner))return false;try{if(!Wallet.GangBulkBegin(state(),attempt,Dealer.Catalog,out bool changed) || !changed){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}
        }
        bool Resume()
        {
            if(Active || Pending==null || Wallet.GangBulk?.Valid!=true || gate.Owner!=null || !Owned || !Safe || Game.IsPaused || InterfaceOptions.ModalOpen || Pending.Kind==GangBulkKind.Drugs && Dealer?.Catalog?.Valid!=true || Pending.Kind==GangBulkKind.Arms && !arms.Valid)return false;
            if(Pending.Ready){if(!Game.Player.Character.IsOnFoot || Game.Player.Character.Position.DistanceTo(Pos(Pending.Site))>10)return false;}
            else if(!Bind(Pending.Area))return false;
            if(!gate.Acquire(Owner))return false;try{if(!Pending.Ready && !Wallet.GangBulkRestart(Pending.Id,out _)){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}
        }
        void Activate(){Active=true;profile=Profile.Id;player=Game.Player.Character.Handle;id=Pending.Id;token=random.Next(1,int.MaxValue);last=Game.GameTime;elapsed=0;carrying=arrived=false;if(vehicle!=null){JobVehicles.Reserve(vehicle);reserved=true;}pool.HideAll();Route(Pending.Ready?Pos(Pending.Site):Pos(Pending.Area));Notify.Show(Pending.Ready?"Entrega salva. E no ponto do encontro confirma a venda.":"Saia do carro, E no ponto da area para pegar a caixa e E na traseira para carregar. END suspende.");}
        void Route(Vector3 pos){if(objective!=null && objective.Exists())objective.Delete();objective=MapBlips.Create(pos,"RPCore: venda em lote",BlipSprite.Standard,BlipColor.Yellow,.8f);objective.ShowRoute=true;}
        void SpawnBuyer()
        {
            if(!preparing){buyerModel=new Model(Pending.Model);if(!ModelValid(Pending.Model)){Suspend();return;}buyerModel.Request();prepareAt=Game.GameTime;preparing=true;}
            if(!buyerModel.IsLoaded){if(unchecked(Game.GameTime-prepareAt)>10000){Suspend();Notify.Show("Modelo do comprador indisponivel. Contrato conservado.");}return;}
            Vector3? spot=null;var center=Pos(Pending.Site);for(int i=0;i<12;i++){double angle=i*Math.PI/6;var p=center+new Vector3((float)Math.Cos(angle)*4,(float)Math.Sin(angle)*4,0);float z;if(World.GetGroundHeight(p+new Vector3(0,0,2),out z,GetGroundHeightMode.Normal) && Math.Abs(z-center.Z)<=3 && JobActors.Free(new Vector3(p.X,p.Y,z),2)){spot=new Vector3(p.X,p.Y,z+.05f);break;}}
            if(spot==null){Suspend();Notify.Show("Sem chao/espaco para comprador. Retome ou cancele no F4.");return;}
            var ped=World.CreatePed(buyerModel,spot.Value,Pending.Site.Heading);if(ped==null || !ped.Exists()){Suspend();return;}
            bool marked=false;try{if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);marked=Function.Call<bool>(Hash.DECOR_SET_INT,ped,Decor,token);}finally{if(!marked)ped.Delete();}
            if(!marked){Suspend();Notify.Show("Identidade do comprador indisponivel; contrato conservado.");return;}buyer=ped;
            buyer.IsPersistent=true;buyer.BlockPermanentEvents=true;buyer.Weapons.RemoveAll();buyer.RelationshipGroup=new RelationshipGroup((int)RelationshipGroupHash.CivilianMale);Function.Call(Hash.SET_PED_MONEY,buyer,0);Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES,buyer,(int)CombatAttributes.CanCommandeerVehicles,false);Function.Call(Hash.SET_PED_DROPS_WEAPONS_WHEN_DEAD,buyer,false);Function.Call(Hash.TASK_STAND_STILL,buyer,-1);buyerModel.MarkAsNoLongerNeeded();preparing=false;
        }
        bool HandoverEligible=>Active && Pending!=null && Pending.Loaded && !Pending.Ready && carrying && arrived && Input && Stopped && vehicle.Position.DistanceTo(Pos(Pending.Site))<=30 && OwnBuyer && buyer.IsAlive && !buyer.IsInCombat && Game.Player.Character.IsOnFoot && !Game.Player.Character.IsRagdoll && Game.Player.Character.Position.DistanceTo(buyer.Position)<=2.5f;
        public void Tick()
        {
            if(!Active)return;
            if(Pending==null || Wallet.GangBulk?.Valid!=true || Pending.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Owned || !Safe || !Pending.Ready && (!Car || Game.Player.Character.Position.DistanceTo(vehicle.Position)>150 || carrying && Game.Player.Character.IsInVehicle())){Suspend();Notify.Show("Lote suspenso. Estoque/contrato conservados; F4 retoma ou cancela.");return;}
            uint delta=unchecked((uint)(Game.GameTime-last));last=Game.GameTime;if(Input && delta<=2000)elapsed+=(int)delta;
            if(!Pending.Ready && elapsed>=Pending.Options.SecondsLimit*1000){Suspend();Notify.Show("Prazo ativo encerrado. Contrato conservado para retomar/cancelar.");return;}
            var context=Game.Player.Character.Position;WorkContext.ShopDelivery.Set(Owner,new JobPoint{Name="bulk handover",X=context.X,Y=context.Y,Z=context.Z},5);cargo.Tick();
            if(preparing && !Input)prepareAt=Game.GameTime;
            if(!Pending.Ready && Pending.Loaded && Input && buyer==null && vehicle.Position.DistanceTo(Pos(Pending.Site))<=60)SpawnBuyer();
            if(!Active)return;
            if(buyer!=null && (!OwnBuyer || buyer.IsDead || buyer.IsInCombat)){Suspend();Notify.Show("Comprador indisponivel; venda suspensa sem debito/pagamento.");return;}
            if(Pending.Loaded && !Pending.Ready && !arrived && Input && Stopped && vehicle.Driver==Game.Player.Character && vehicle.Position.DistanceTo(Pos(Pending.Site))<=25){arrived=true;Notify.Show("Estacione, saia e E na traseira para retirar o lote. Leve a caixa ao comprador.");}
            if(handover.Step(Game.GameTime,HandoverEligible,Pending.Options.HandoverSeconds))
            {if(!Wallet.GangBulkReady(id,out _))throw new InvalidOperationException("Bulk handover checkpoint failed");cargo.Stop();carrying=false;ClearBuyer();ReleaseCar();Route(Pos(Pending.Site));Notify.Show("Entrega fisica confirmada. E no ponto amarelo finaliza estoque e receita suja.");return;}
            if(!Input)return;
            Vector3 target=Pending.Ready?Pos(Pending.Site):!Pending.Loaded?(carrying?Rear:Pos(Pending.Area)):arrived?(carrying && OwnBuyer?buyer.Position:Rear):Pos(Pending.Site);
            World.DrawMarker(MarkerType.Cylinder,target-new Vector3(0,0,1),Vector3.Zero,Vector3.Zero,new Vector3(1.2f,1.2f,.5f),Color.FromArgb(130,200,170,50));
            GTA.UI.Screen.ShowSubtitle("Lote / "+Pending.Units+"x "+Pending.Product+" / "+Pending.Buyer+" / END suspende",100);
            if(Game.Player.Character.IsOnFoot && Game.Player.Character.Position.DistanceTo(target)<=3)GTA.UI.Screen.ShowHelpTextThisFrame(Pending.Ready?"E: confirmar venda e receita suja":!Pending.Loaded?(carrying?"E: carregar caixa na traseira":"E: pegar caixa do lote"):carrying?(handover.Searching?"Entregando... permaneca junto ao comprador":"E: entregar caixa ao comprador"):"E: retirar caixa na traseira");
        }
        public void Interact()
        {
            if(!Active || !Input || !Owned || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Game.Player.Character.IsOnFoot)return;
            var p=Game.Player.Character;
            if(Pending.Ready){if(p.Position.DistanceTo(Pos(Pending.Site))>3)return;int value=Pending.Value;string purchaser=Pending.Buyer;bool weapons=Pending.Kind==GangBulkKind.Arms;bool changed;bool ok=weapons?Wallet.GangArmsComplete(state(),CurrentArea,arms,id,out changed):Wallet.GangBulkComplete(state(),CurrentArea,Dealer?.Catalog,id,out changed);if(ok){Suspend();Notify.Show(changed?(weapons?"Armas vendidas: $"+value+" de receita suja na area. Processe85% pelo F4; sem premio de relacao.":"Lote vendido: $"+value+" de receita suja na area. Confira relacao com "+purchaser+" e processe85% pelo F4."):"Venda ja confirmada; nenhum credito repetido.");}else Notify.Show("Venda nao confirmada. Confira estoque/catalogo/limites; entrega continua salva.");return;}
            if(!Car || !Stopped || p.IsRagdoll)return;
            if(!Pending.Loaded)
            {
                if(!carrying && p.Position.DistanceTo(Pos(Pending.Area))<=3 && vehicle.Position.DistanceTo(Pos(Pending.Area))<=30){carrying=true;cargo.Start(p,false);return;}
                if(carrying && p.Position.DistanceTo(Rear)<=3 && vehicle.Position.DistanceTo(Pos(Pending.Area))<=30){if(!Wallet.GangBulkLoad(id,out _))return;cargo.Stop();carrying=false;Route(Pos(Pending.Site));Notify.Show("Lote carregado. Dirija este carro ao encontro e estacione a ate25m.");}return;
            }
            if(!arrived || !OwnBuyer || vehicle.Position.DistanceTo(Pos(Pending.Site))>30)return;
            if(!carrying && p.Position.DistanceTo(Rear)<=3){carrying=true;cargo.Start(p,false);return;}
            if(HandoverEligible && !handover.Searching)handover.Start(Game.GameTime);
        }
        void ClearBuyer(){if(OwnBuyer)buyer.Delete();buyer=null;if(buyerModel!=null)buyerModel.MarkAsNoLongerNeeded();buyerModel=null;preparing=false;}
        void ReleaseCar(){if(reserved)JobVehicles.Release(vehicle);reserved=false;vehicle=null;}
        public void Suspend(){cargo.Stop();handover.Step(Game.GameTime,false,2);ClearBuyer();ReleaseCar();if(objective!=null && objective.Exists())objective.Delete();objective=null;WorkContext.ShopDelivery.Clear(Owner);Active=carrying=arrived=false;profile=id=plate=null;token=0;gate.Release(Owner);}
        public void Close(){Suspend();menu.Visible=false;}
    }
}
