using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
namespace RPCore
{
    public sealed partial class PdmEnterpriseSystem
    {
        int nextSpray;
        string manualMode,serviceStock,manualProfile;Vehicle serviceCar;Prop handTool;int manualActor,manualModel,serviceStarted,lastWork;long manualAddress;int carryingTrash=-1;PdmInspection workTime;readonly HashSet<string> recipients=new HashSet<string>();
        bool ManualBound=>manualActor==Game.Player.Character.Handle && manualModel==Game.Player.Character.Model.Hash && manualAddress!=0 && manualAddress==Game.Player.Character.MemoryAddress.ToInt64() && manualProfile==Profile.Id;
        void BindManual(string mode){manualMode=mode;manualProfile=Profile.Id;var p=Game.Player.Character;manualActor=p.Handle;manualModel=p.Model.Hash;manualAddress=p.MemoryAddress.ToInt64();serviceStarted=Game.GameTime;lastWork=serviceStarted;}
        void StartCards(){if(Data.CardsLeft<1 || !Safe || !gate.Acquire(Name))return;BindManual("cards");Function.Call(Hash.REQUEST_ANIM_DICT,"mp_common");ui.Close();recipients.Clear();Tell("Cartoes","Entregue aos pedestres proximos usando "+Cfg.PdmInteractionKey+". End encerra sem perder os cartoes restantes.");}
        void OpenServices()
        {
            Page("services","PDM / OFICINA E SERVICOS");
            foreach(var stock in Wallet.Pdm.Stock.Where(s=>!s.AwaitingCollection)){var id=stock.Id;Add(stock.Name+" / "+stock.Plate,"Sujeira "+stock.Dirt+"/15; combustivel "+(int)(stock.FuelFraction*100)+"%; "+stock.Mileage+" mi. Preparar no box para lavar, abastecer ou modificar.",()=>PrepareService(id));}
            if(!Wallet.Pdm.Stock.Any(s=>!s.AwaitingCollection))ui.Add("Nenhum carro recebido","Receba pedidos no porto ou compre um particular/coletor antes de atender.",()=>{},false);
            Add("Limpeza do estabelecimento","Lixo atual: "+Data.TrashCount+"/4. Aproxime-se de uma sacola e use "+Cfg.PdmInteractionKey+"; leve-a a cacamba. Zeladoria em dia tambem limpa presencialmente.",()=>{ui.Close();Tell("Limpeza","Use "+Cfg.PdmInteractionKey+" perto das sacolas. Depois leve a cacamba marcada atras da empresa.");});
            Add("Voltar","Operacao.",OpenMenu);ui.Show();
        }
        void PrepareService(string id)
        {
            if(!office() || !PdmEnterpriseRules.Idle(Wallet.Pdm))return;var stock=Wallet.Pdm.Stock.Find(v=>v.Id==id);if(stock==null || stock.AwaitingCollection)return;
            ui.Stream(new[]{stock.Model},()=>{
                if(!office() || !gate.Acquire(Name)){Tell("Oficina","Encerre a outra atividade e permaneça no escritorio.");return;}
                try{
                    if(PrepareStock!=null && !PrepareStock(id))return;
                    var model=PdmVehicleModel.Resolve(stock.Model);var bay=RigSpawn.FindDisplayVehicle(options.ServiceBay,model,false);
                    if(bay==null){ui.Problem("Box ocupado","Afaste seu veiculo da vaga de servico; estoque preservado.",null,OpenServices);return;}
                    StopServiceCar();serviceCar=PdmAssets.Track(World.CreateVehicle(model,Pos(bay),bay.Heading));if(!Valid(serviceCar))return;serviceCar.IsPersistent=true;serviceCar.LockStatus=VehicleLockStatus.CannotEnter;Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT,serviceCar,stock.Plate);PdmStockPresentation.Apply(serviceCar,stock);serviceCar.IsPositionFrozen=true;serviceStock=id;BindManual("service");OpenPreparedService();
                }finally{if(!Valid(serviceCar))gate.Release(Name);}
            },OpenServices);
        }
        void OpenPreparedService()
        {
            if(!Valid(serviceCar) || serviceStock==null){StopManual();OpenServices();return;}
            var stock=Wallet.Pdm.Stock.Find(v=>v.Id==serviceStock);if(stock==null){StopManual();return;}
            Page("prepared","OFICINA / "+stock.Name,()=>{StopManual();OpenServices();},OpenPreparedService);
            Add("Lavar com mangueira","Requer lavadora comprada. Va ate o carro, mantenha o ataque/clique para lavar. Progresso somente junto ao alvo; End cancela.",()=>StartTool("wash"),Data.Upgrades.Contains("washer"));
            Add("Abastecer / $"+(int)Math.Ceiling((1-stock.FuelFraction)*Math.Max(1,serviceCar.PetrolTankVolume)*3),"Requer bomba comprada. Vai preencher o tanque do carro empresarial; custo e confirmado antes e debitado ao concluir.",()=>{
                int cost=(int)Math.Ceiling((1-stock.FuelFraction)*Math.Max(1,serviceCar.PetrolTankVolume)*3);ui.Confirm("Abastecer / $"+cost,"Cobra o caixa ao concluir o abastecimento fisico no box. End cancela antes da conclusao.",()=>StartTool("fuel"),OpenPreparedService);
            },Data.Upgrades.Contains("pump") && stock.FuelFraction<1);
            Add("Modificar veiculo","Mecanico contratado e em dia, oficina adquirida. Modificacoes ficam no registro do carro e sao reaplicadas ao expor/entregar.",OpenTuning,PdmEnterpriseRules.Paid(Wallet.Pdm,"mechanic") && Wallet.Pdm.Staff.Workshop);
            Add("Finalizar / recolher carro","Recolhe apenas o temporario reconhecido. Salva aparencia/modificacoes apenas quando uma operacao for confirmada.",()=>{StopManual();OpenServices();});ui.Show();
        }
        void OpenTuning()
        {
            if(!Valid(serviceCar)){StopManual();return;}Page("tuning","PDM / MODIFICACOES",OpenPreparedService,OpenTuning);
            foreach(int type in Enumerable.Range(0,50)){
                int count=Function.Call<int>(Hash.GET_NUM_VEHICLE_MODS,serviceCar,type);if(count<1)continue;int mod=type;int current=Function.Call<int>(Hash.GET_VEHICLE_MOD,serviceCar,mod);
                string[] names={"Aerofolio","Para-choque dianteiro","Para-choque traseiro","Saias","Escape"};string label=type<names.Length?names[type]:type==11?"Motor":type==12?"Freios":type==13?"Transmissao":type==15?"Suspensao":type==16?"Blindagem":type==23?"Rodas":"Peca "+type;
                for(int level=-1;level<count;level++){int chosen=level;int cost=chosen<0?250:500*(chosen+1);ui.AddConfirm(label+" / nivel "+chosen+" / $"+cost,"Aplica ao carro empresarial e debita caixa uma vez; nao muda veiculos pessoais.",()=>{
                    if(!Valid(serviceCar) || !PdmEnterpriseRules.Paid(Wallet.Pdm,"mechanic") || !Wallet.Pdm.Staff.Workshop)return;
                    var before=VehicleSnapshot.Capture(serviceCar);try{Function.Call(Hash.SET_VEHICLE_MOD_KIT,serviceCar,0);Function.Call(Hash.SET_VEHICLE_MOD,serviceCar,mod,chosen,false);var after=VehicleSnapshot.Capture(serviceCar);
                    if(!PdmEnterpriseTransactions.Service(Wallet.EnterpriseWallet(),serviceStock,"tune",cost,after,Guid.NewGuid().ToString("N"),Wallet.Save,out _)){VehicleSnapshot.Apply(serviceCar,before);ui.Problem("Modificacao recusada","Confira caixa, contrato e mecanico. Aparencia anterior restaurada.",null,OpenTuning);return;}}catch{if(Valid(serviceCar))VehicleSnapshot.Apply(serviceCar,before);throw;}OpenTuning();
                },chosen!=current);}
            }
            foreach(int type in new[]{17,18,19,20,21,22}){int toggle=type;bool enabled=Function.Call<bool>(Hash.IS_TOGGLE_MOD_ON,serviceCar,toggle);ui.AddConfirm("Opcional "+toggle+" / "+(enabled?"remover":"instalar")+" / $1500","Aplica o opcional suportado pelo carro (turbo, xenon e efeitos), registra e debita uma vez.",()=>{
                if(!Valid(serviceCar) || !PdmEnterpriseRules.Paid(Wallet.Pdm,"mechanic"))return;var before=VehicleSnapshot.Capture(serviceCar);try{Function.Call(Hash.TOGGLE_VEHICLE_MOD,serviceCar,toggle,!enabled);if(Function.Call<bool>(Hash.IS_TOGGLE_MOD_ON,serviceCar,toggle)==enabled){Tell("Oficina","Opcional nao suportado; nenhuma cobranca.");return;}var after=VehicleSnapshot.Capture(serviceCar);if(!PdmEnterpriseTransactions.Service(Wallet.EnterpriseWallet(),serviceStock,"tune",1500,after,Guid.NewGuid().ToString("N"),Wallet.Save,out _)){VehicleSnapshot.Apply(serviceCar,before);return;}}catch{if(Valid(serviceCar))VehicleSnapshot.Apply(serviceCar,before);throw;}OpenTuning();});}
            foreach(int tint in Enumerable.Range(0,7)){int value=tint;ui.AddConfirm("Pelicula "+value+" / $500","Registra a pelicula no carro empresarial.",()=>TuneAppearance(()=>Function.Call(Hash.SET_VEHICLE_WINDOW_TINT,serviceCar,value),500),Function.Call<int>(Hash.GET_VEHICLE_WINDOW_TINT,serviceCar)!=value);}
            int liveries=Function.Call<int>(Hash.GET_VEHICLE_LIVERY_COUNT,serviceCar);for(int livery=0;livery<liveries;livery++){int value=livery;ui.AddConfirm("Pintura especial "+value+" / $1500","Usa as pinturas disponiveis no modelo.",()=>TuneAppearance(()=>Function.Call(Hash.SET_VEHICLE_LIVERY,serviceCar,value),1500),Function.Call<int>(Hash.GET_VEHICLE_LIVERY,serviceCar)!=value);}
            foreach(var color in new[]{new{Id=0,Name="Preto"},new{Id=111,Name="Branco"},new{Id=27,Name="Vermelho"},new{Id=64,Name="Azul"},new{Id=55,Name="Verde"},new{Id=145,Name="Roxo"}}){var paint=color;ui.AddConfirm("Pintura "+paint.Name+" / $1000","Repinta as duas cores; preserva modificacoes e danos.",()=>TuneAppearance(()=>{Function.Call(Hash.CLEAR_VEHICLE_CUSTOM_PRIMARY_COLOUR,serviceCar);Function.Call(Hash.CLEAR_VEHICLE_CUSTOM_SECONDARY_COLOUR,serviceCar);Function.Call(Hash.SET_VEHICLE_COLOURS,serviceCar,paint.Id,paint.Id);},1000));}
            Add("Voltar","Box.",OpenPreparedService);ui.Show();
        }
        void TuneAppearance(Action apply,int cost)
        {
            if(!Valid(serviceCar) || !PdmEnterpriseRules.Paid(Wallet.Pdm,"mechanic"))return;var before=VehicleSnapshot.Capture(serviceCar);
            try{apply();var after=VehicleSnapshot.Capture(serviceCar);if(!PdmEnterpriseTransactions.Service(Wallet.EnterpriseWallet(),serviceStock,"tune",cost,after,Guid.NewGuid().ToString("N"),Wallet.Save,out _)){VehicleSnapshot.Apply(serviceCar,before);Tell("Oficina","Nenhuma alteracao confirmada. Confira caixa e mecanico.");}}catch{if(Valid(serviceCar))VehicleSnapshot.Apply(serviceCar,before);throw;}OpenTuning();
        }
        void StartTool(string mode)
        {
            if(!Valid(serviceCar) || !ManualBound || Game.Player.Character.Position.DistanceTo(serviceCar.Position)>15)return;
            string id=mode=="wash"?"washer":"pump";if(!Data.Upgrades.Contains(id))return;
            var model=PdmVehicleModel.Resolve(mode=="wash"?"prop_hose_nozzle":"prop_cs_fuel_nozle");
            ui.Stream(new[]{mode=="wash"?"prop_hose_nozzle":"prop_cs_fuel_nozle"},()=>{
                handTool=PdmAssets.Track(World.CreateProp(model,Game.Player.Character.Position,false,false));if(!Valid(handTool))return;handTool.AttachTo(Game.Player.Character.Bones[Bone.SkelRightHand],new Vector3(.15f,.05f,0),Vector3.Zero);manualMode=mode;workTime=new PdmInspection(mode=="wash"?8:10);ui.Close();Tell("Servico","Aproxime-se do carro e mantenha o botao de ataque/clique. End cancela.");
            },OpenPreparedService);
        }
        void ManualTick()
        {
            if(manualMode==null){TrashInteraction();return;}
            if(!ManualBound || !PdmMenuRuntime.WorldAllowed || Game.Player.Character.IsDead || Game.Player.Character.IsRagdoll || Game.Player.Character.IsFalling || Game.Player.Character.IsInCombat || Game.Player.Wanted.WantedLevel>0 || Game.GameTime-serviceStarted>600000){StopManual();return;}
            if((manualMode=="cards" || manualMode=="trash") && (pool.AreAnyVisible || InterfaceOptions.ModalOpen))return;
            if(manualMode=="cards"){
                if(Data.CardsLeft<1){Tell("Campanha","Cartoes entregues; reputacao aumentada.");StopManual();return;}
                Prompt.Help("Cartoes PDM: "+Data.CardsLeft+" restantes. "+PdmCustomerInput.Button+": entregar a pedestre. End: parar.");
                var target=World.GetNearbyPeds(Game.Player.Character,2).FirstOrDefault(p=>p!=null && p.Exists() && p.IsHuman && !p.IsDead && !p.IsInVehicle() && !p.IsInCombat && p.Handle!=Game.Player.Character.Handle && p.PedType!=PedType.Cop);if(target==null)return;
                string token=target.Handle+":"+target.Model.Hash+":"+target.MemoryAddress.ToInt64();if(Data.CardTargets.Contains(token) || !Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED,"mp_common"))return;
                if(PdmCustomerInput.Pressed){
                    if(PdmEnterpriseTransactions.Card(Wallet.EnterpriseWallet(),Data.CardSession,token,Data.CardSession+":"+Data.CardsLeft,Wallet.Save,out bool changed) && changed){Function.Call(Hash.REQUEST_ANIM_DICT,"mp_common");if(Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED,"mp_common"))Function.Call(Hash.TASK_PLAY_ANIM,Game.Player.Character,"mp_common","givetake1_b",4f,-4f,1200,49,0f,false,false,false);}
                }return;
            }
            if(manualMode=="trash"){
                World.DrawMarker(MarkerType.Cylinder,Pos(options.Dumpster)-new Vector3(0,0,1),Vector3.Zero,Vector3.Zero,new Vector3(2,2,1),System.Drawing.Color.FromArgb(120,80,220,100));
                Prompt.Help("Leve a sacola a cacamba. "+PdmCustomerInput.Button+": descartar. End: devolver.");
                if(Game.Player.Character.Position.DistanceTo(Pos(options.Dumpster))<2 && PdmCustomerInput.Pressed){if(PdmEnterpriseTransactions.CleanTrash(Wallet.EnterpriseWallet(),Guid.NewGuid().ToString("N"),Wallet.Save,out _,carryingTrash)){carryingTrash=-1;StopManual();Tell("Limpeza","Sacola descartada; limpeza melhorada.");}}return;
            }
            if(!Valid(serviceCar) || !Wallet.Pdm.Stock.Any(s=>s.Id==serviceStock) || serviceCar.Occupants.Length>0){StopManual();return;}
            if(manualMode=="service"){if(!pool.AreAnyVisible)Prompt.Help("Box PDM preparado. End: recolher carro e encerrar o servico.");return;}
            bool close=Game.Player.Character.Position.DistanceTo(serviceCar.Position)<5 && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
            Game.DisableControlThisFrame(Control.Attack);bool attacking=Game.IsControlPressed(Control.Attack) || Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED,0,(int)Control.Attack);
            Prompt.Help((manualMode=="wash"?"Lavar":"Abastecer")+": mantenha ataque/clique junto ao carro. End cancela.");
            if(close && attacking && manualMode=="wash" && Game.GameTime>=nextSpray){
                nextSpray=Game.GameTime+150;Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET,"core");
                if(Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED,"core")){var p=serviceCar.Position+new Vector3(0,0,.5f);Function.Call(Hash.USE_PARTICLE_FX_ASSET,"core");Function.Call(Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD,"ent_sht_water",p.X,p.Y,p.Z,0f,0f,0f,.7f,false,false,false);}
            }
            if(workTime.Update(Game.GameTime,close && attacking)){
                var stock=Wallet.Pdm.Stock.Find(s=>s.Id==serviceStock);int cost=manualMode=="fuel"?(int)Math.Ceiling((1-stock.FuelFraction)*Math.Max(1,serviceCar.PetrolTankVolume)*3):0;
                var appearance=VehicleSnapshot.Capture(serviceCar);if(manualMode=="wash")appearance.Dirt=0;else appearance.Fuel=serviceCar.PetrolTankVolume;
                if(PdmEnterpriseTransactions.Service(Wallet.EnterpriseWallet(),serviceStock,manualMode,cost,appearance,Guid.NewGuid().ToString("N"),Wallet.Save,out _)){if(manualMode=="wash")serviceCar.DirtLevel=0;else serviceCar.FuelLevel=serviceCar.PetrolTankVolume;Remove(handTool);handTool=null;manualMode="service";Tell("Servico","Concluido e salvo no carro empresarial.");OpenPreparedService();}else{Remove(handTool);handTool=null;manualMode="service";ui.Problem("Servico recusado","Confira caixa e estoque. Nenhum novo servico confirmado.",null,OpenPreparedService);}
            }
        }
        void TrashInteraction()
        {
            if(!Local || !Safe || pool.AreAnyVisible || Data.TrashCount<1)return;
            var pair=trash.OrderByDescending(v=>v.Key).FirstOrDefault(v=>Valid(v.Value) && v.Value.Position.DistanceTo(Game.Player.Character.Position)<2);if(pair.Value==null)return;
            Prompt.Help(PdmCustomerInput.Button+": recolher lixo da empresa");if(!PdmCustomerInput.Pressed || !gate.Acquire(Name))return;
            carryingTrash=pair.Key;handTool=pair.Value;trash.Remove(pair.Key);handTool.IsPositionFrozen=false;handTool.AttachTo(Game.Player.Character.Bones[Bone.SkelRightHand],new Vector3(.1f,0,-.2f),Vector3.Zero);BindManual("trash");
        }
        void StopServiceCar(){if(Valid(serviceCar)){if(serviceCar.Occupants.Length==0)serviceCar.Delete();else{serviceCar.IsPositionFrozen=false;JobVehicles.Retire(serviceCar);}}serviceCar=null;serviceStock=null;}
        void StopManual()
        {
            bool owned=manualMode!=null && ManualBound;
            Remove(handTool);handTool=null;StopServiceCar();carryingTrash=-1;manualMode=null;workTime=null;manualActor=0;manualAddress=0;
            if(sellerTesting){sellerTesting=false;sellerInspection=null;}gate.Release(Name);
            if(owned && Game.Player.Character!=null && Game.Player.Character.Exists())Function.Call(Hash.STOP_ANIM_TASK,Game.Player.Character,"mp_common","givetake1_b",2f);
        }
    }
}
