using System;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
namespace RPCore
{
    public sealed class VehicleInsuranceSystem : ISystem
    {
        public string Name=>"vehicleinsurance";
        readonly ObjectPool pool;readonly ShiftGate gate;readonly NativeMenu menu;readonly VehicleInsuranceOptions options;
        bool Safe=>Cfg.EnableGarages && !Game.IsPaused && !Game.IsMissionActive && !InterfaceOptions.ModalOpen && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && Game.Player.Wanted.WantedLevel==0 && !Game.Player.Character.IsDead && !Game.Player.Character.IsInCombat;
        public VehicleInsuranceSystem(ObjectPool pool,ShiftGate gate)
        {
            this.pool=pool;this.gate=gate;var defaults=VehicleInsuranceDefaults.Load();options=Store.LoadDef("Jobs/VehicleInsurance.json",()=>defaults);
            if(options==null || !options.Valid){Log.Write("Invalid VehicleInsurance.json; embedded defaults used, original retained.");options=defaults;}
            menu=new NativeMenu("Seguro veicular","CADASTRO / LOCALIZAR / RECUPERAR");pool.Add(menu);Visuals.Register(menu,"insurance");
        }
        void Add(string title,string description,Action action)
        {var item=new NativeItem(title,description);item.Activated+=(s,e)=>{try{if(!Safe || !Wallet.VehicleInsurance.Valid){menu.Visible=false;Notify.Show("Use seguro sem missao/procurado/combate e com dados validos.");return;}action();}catch(Exception ex){gate.Release(Name);menu.Visible=false;Log.Error("vehicle insurance",ex);Notify.Show("Operacao nao concluida; confira pedido pendente. Retomar nao cobra novamente.");}};menu.Add(item);}
        public void OpenMenu()
        {
            if(!Safe){Notify.Show("Seguro requer garagens habilitadas e jogador sem missao/procurado/combate/outra interface.");return;}
            if(!Wallet.VehicleInsurance.Valid){Notify.Show("Dados de seguro invalidos; original preservado.");return;}
            pool.HideAll();menu.Clear();menu.Name="Seguro / banco $"+Wallet.Bank;
            Add("Cadastrar veiculo atual / gratis","Motorista, parado, carro/moto registrado em sua garagem e em condicoes de rodar. Franquia por recuperacao: $"+options.Excess+". Sem mensalidade.",Enroll);
            foreach(var policy in Wallet.VehicleInsurance.Policies){string id=policy.VehicleId;var car=Wallet.Transport.Vehicles.Find(v=>v.Id==id);Add((car?.Name??"Veiculo")+" ["+policy.Plate+"]","Franquia contratada $"+policy.Excess+". Localizar, recuperar ou cancelar sem apagar o registro da garagem.",()=>PolicyMenu(id));}
            Add("Recuperacoes "+Wallet.VehicleInsurance.Claims+" / franquias $"+Wallet.VehicleInsurance.Spent,"Recupera o mesmo veiculo destruido no local, sem criar copia nem pagar indenizacao em dinheiro. Carro desaparecido: use a localizacao/garagem existente.",()=>{});
            Add("Como registrar / garagens","Compre uma garagem, guarde e retire seu veiculo. "+Cfg.GarageKey+" abre garagens. Veiculos de trabalho/missao nao entram no seguro.",()=>Notify.Show("Use "+Cfg.GarageKey+" para comprar/usar uma garagem; depois cadastre o veiculo retirado."));
            Add("Fechar","Voltar ao jogo.",()=>menu.Visible=false);menu.Visible=true;
        }
        static string Key(Vehicle v)=>VehicleIdentity.Key(v.Model.Hash,Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT,v));
        static bool Road(Vehicle v)=>v!=null && v.Exists() && (v.Model.IsCar || v.Model.IsBike || v.Model.IsQuadBike) && !v.Model.IsBicycle && !JobVehicles.Contains(v);
        static bool Destroyed(Vehicle v)=>VehicleInsuranceRules.Destroyed(v.IsDead,v.IsConsideredDestroyed,v.EngineHealth,v.PetrolTankHealth);
        static Vehicle Unique(VehiclePolicy policy)
        {var cars=World.GetAllVehicles().Where(v=>v!=null && v.Exists() && Key(v)==VehicleIdentity.Key(policy.Model,policy.Plate)).ToArray();return cars.Length==1?cars[0]:null;}
        void Enroll()
        {
            var ped=Game.Player.Character;var v=ped.IsInVehicle()?ped.CurrentVehicle:null;
            if(!Road(v) || v.Driver!=ped || v.Speed>.5f || Destroyed(v) || !v.IsDriveable || v.PassengerCount>0 || Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,v)){Notify.Show("Use seu carro/moto registrado, parado, saudavel, como motorista e sem passageiros/anexos.");return;}
            var record=Wallet.Transport.FindIdentity(v.Model.Hash,Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT,v));
            if(record==null || record.Stored || record.PendingRemoval){Notify.Show("Registre e retire o veiculo na sua garagem antes de cadastrar seguro.");return;}
            if(!gate.Acquire(Name)){Notify.Show("Encerre a atividade de "+gate.Owner+".");return;}
            try{if(Wallet.VehicleInsure(options,record.Id,out _))Notify.Show("Seguro cadastrado gratuitamente; franquia $"+options.Excess+" por recuperacao.");else Notify.Show("Confira cadastro existente, garagem e limite de apolices.");}finally{gate.Release(Name);}OpenMenu();
        }
        void PolicyMenu(string id)
        {
            var policy=Wallet.VehicleInsurance.Policies.Find(p=>p.VehicleId==id);if(policy==null){OpenMenu();return;}
            menu.Clear();menu.Name="Seguro ["+policy.Plate+"] / $"+policy.Excess;
            Add("Localizar veiculo / GPS","Veiculo carregado e identidade unica: GPS atual. Caso contrario, ultima posicao do registro, sem criar/teleportar nada.",()=>{
                var live=Unique(policy);var saved=Wallet.Transport.Vehicles.Find(v=>v.Id==id);
                if(live!=null)Function.Call(Hash.SET_NEW_WAYPOINT,live.Position.X,live.Position.Y);
                else if(saved!=null)Function.Call(Hash.SET_NEW_WAYPOINT,saved.X,saved.Y);else{Notify.Show("Registro nao encontrado.");return;}
                menu.Visible=false;Notify.Show(live==null?"GPS da ultima posicao registrada, sem confirmar veiculo presente.":"GPS do veiculo atual.");
            });
            var pending=Wallet.VehicleInsurance.Pending;
            if(pending?.VehicleId==id){
                string request=pending.Id;
                Add("Retomar recuperacao / sem nova cobranca","Perto do mesmo veiculo vazio, no mesmo perfil. Reaplica reparo e conclui o registro pago.",()=>Resume(request));
                if(!pending.Started)Add("Cancelar pedido e estornar $"+pending.Excess,"Reparo ainda nao autorizado; devolve franquia ao banco.",()=>{if(!Wallet.VehicleRecoveryRefund(request,out _))Notify.Show("Estorno recusado; confira estado e limite do banco.");PolicyMenu(id);});
                else Add("Encerrar pedido sem estorno","Abre confirmacao. Reparo pode ja ter ocorrido; prefira retomar para concluir sem cobrar novamente.",()=>AbandonMenu(id,request));
            }
            else if(pending==null)Add("Recuperar destruido / $"+policy.Excess,"Saia do veiculo e fique ate 30m. Exige o mesmo carro destruido, vazio e desanexado. Repara no local, preservando combustivel/oleo; sem reposicao ou guincho.",()=>Recover(id));
            if(pending?.VehicleId!=id)Add("Cancelar seguro","Abre confirmacao. Cancela somente a apolice; nao apaga carro, garagem, tanque ou porta-malas.",()=>CancelMenu(id));
            Add("Voltar","Lista de apolices.",OpenMenu);menu.Visible=true;
        }
        bool Target(Vehicle v,VehiclePolicy policy,bool requireDestroyed)
        {
            var ped=Game.Player.Character;var saved=Wallet.Transport.Vehicles.Find(r=>r.Id==policy.VehicleId);
            return Safe && !ped.IsInVehicle() && Road(v) && v.Speed<.5f && v.Occupants.Length==0 && !Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,v) && ped.Position.DistanceTo(v.Position)<=30 && saved!=null && !saved.Stored && !saved.PendingRemoval && VehicleInsuranceRules.Same(policy,saved) && Key(v)==VehicleIdentity.Key(policy.Model,policy.Plate) && (!requireDestroyed || Destroyed(v));
        }
        void Recover(string id)
        {
            var p=Wallet.VehicleInsurance.Policies.Find(v=>v.VehicleId==id);if(p==null)return;var v=Unique(p);
            if(!Target(v,p,true)){Notify.Show("Perto de um unico veiculo segurado destruido, vazio e parado, a pe. Nenhuma cobranca.");return;}
            if(!gate.Acquire(Name)){Notify.Show("Encerre a atividade de "+gate.Owner+".");return;}
            try{
                var original=VehicleSnapshot.Capture(v);original.Id=id;original.GarageId=Wallet.Transport.Vehicles.Find(r=>r.Id==id).GarageId;
                // The existing RPCore tank is authoritative when present; native fire damage must not create a refill.
                if(Wallet.Transport.Tanks.TryGetValue(Key(v),out var knownTank))original.Fuel=knownTank.Liters;
                string request=Guid.NewGuid().ToString("N");
                if(!Wallet.VehicleRecoveryBegin(id,original,v.Handle,v.OilLevel,true,request,out _)){Notify.Show("Confira banco/registro/pendencia. Nada reparado.");return;}
                Process(request);
            }finally{gate.Release(Name);}PolicyMenu(id);
        }
        void Resume(string id){if(!gate.Acquire(Name)){Notify.Show("Encerre a atividade de "+gate.Owner+".");return;}string vehicle=Wallet.VehicleInsurance.Pending?.VehicleId;try{Process(id);}finally{gate.Release(Name);}if(vehicle!=null)PolicyMenu(vehicle);else OpenMenu();}
        void Process(string id)
        {
            var pending=Wallet.VehicleInsurance.Pending;if(pending==null || pending.Id!=id)return;var policy=Wallet.VehicleInsurance.Policies.Find(p=>p.VehicleId==pending.VehicleId);if(policy==null)return;
            var v=Unique(policy);
            if(v==null || v.Handle!=pending.Handle || !Target(v,policy,!pending.Started)){Notify.Show("Mesmo veiculo indisponivel/ocupado/longe. Pedido conservado; retome quando puder.");return;}
            if(!pending.Started && !Wallet.VehicleRecoveryStart(id,out _)){Notify.Show("Inicio nao gravado. Retome ou cancele antes do reparo.");return;}
            float fuel=pending.Original.Fuel;
            if(Wallet.Transport.Tanks.TryGetValue(Key(v),out var tank))fuel=Math.Min(fuel,tank.Liters);
            Function.Call(Hash.STOP_ENTITY_FIRE,v);v.Repair();Function.Call(Hash.SET_ENTITY_RENDER_SCORCHED,v,false);v.Health=1000;v.EngineHealth=1000;v.BodyHealth=1000;v.PetrolTankHealth=1000;v.DirtLevel=0;v.IsUndriveable=false;v.FuelLevel=fuel;v.OilLevel=pending.Oil;
            Function.Call(Hash.SET_VEHICLE_ENGINE_ON,v,false,true,false);
            if(!VehicleInsuranceRules.Finite(v.FuelLevel) || !VehicleInsuranceRules.Finite(v.OilLevel) || Destroyed(v) || !v.IsDriveable || v.EngineHealth<900 || v.BodyHealth<900 || v.PetrolTankHealth<900 || Math.Abs(v.FuelLevel-fuel)>.05f || Math.Abs(v.OilLevel-pending.Oil)>.05f)throw new InvalidOperationException("Vehicle recovery readback failed; paid pending preserved.");
            var snapshot=VehicleSnapshot.Capture(v);snapshot.Id=pending.VehicleId;
            if(!Wallet.VehicleRecoveryComplete(id,snapshot,out _))throw new InvalidOperationException("Repair applied; completion pending.");
            Notify.Show("Veiculo recuperado no local; franquia cobrada uma vez. Combustivel e oleo preservados.");
        }
        void CancelMenu(string id){menu.Clear();menu.Name="Cancelar apolice?";Add("Sim, cancelar seguro","Registro da garagem, carro e itens permanecem.",()=>{if(!Wallet.VehicleInsuranceCancel(id,out _))Notify.Show("Cancelamento recusado; confira pendencia.");OpenMenu();});Add("Voltar","Manter seguro.",()=>PolicyMenu(id));menu.Visible=true;}
        void AbandonMenu(string vehicle,string id){menu.Clear();menu.Name="Encerrar sem estorno?";Add("Sim, encerrar pedido pago","Nao desfaz reparo nem devolve franquia. Use retomar para concluir sem cobrar novamente.",()=>{if(!Wallet.VehicleRecoveryAbandon(id,out _))Notify.Show("Estado do pedido mudou.");PolicyMenu(vehicle);});Add("Voltar","Manter pedido.",()=>PolicyMenu(vehicle));menu.Visible=true;}
        public void Tick(){if(menu.Visible && !Safe)menu.Visible=false;}
        public void Save(){}
        public void OnKey(KeyEventArgs e){}
        public void Reload(){menu.Visible=false;gate.Release(Name);}
        public void Dispose(){Reload();}
    }
}
