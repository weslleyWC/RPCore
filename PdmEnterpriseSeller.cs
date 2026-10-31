using System;
using GTA;
using GTA.Math;
using GTA.Native;
namespace RPCore
{
    public sealed partial class PdmEnterpriseSystem
    {
        Vehicle sellerCar;Ped sellerPed;Blip sellerBlip;string sellerId;PdmInspection sellerInspection;int sellerTestAt,sellerNext;bool sellerTesting;
        public bool HandlesNearbyInput=>manualMode!=null || Valid(sellerPed) && Game.Player.Character.Position.DistanceTo(sellerPed.Position)<3;
        void CreateSeller()
        {
            if(Data.Seller!=null || !Data.Open || !PdmEnterpriseRules.Idle(Wallet.Pdm))return;
            string id=Guid.NewGuid().ToString("N");var offer=RandomOffer(id);if(offer==null)return;var stock=Stock(offer,id);int asking=Math.Max(1,stock.Cost*95/100);
            var sale=new PdmPrivateSeller{Id=id,Car=stock,Asking=asking,Minimum=asking*(85+PdmRiskTransactions.Roll(id)%11)/100};
            if(PdmEnterpriseTransactions.Seller(Wallet.EnterpriseWallet(),sale,id,Wallet.Save,out bool changed) && changed)Tell("Vendedor","Um particular aguarda na PDM para oferecer "+stock.Name+" por $"+asking+".");
        }
        bool SellerAction()
        {
            if(!Safe || !PdmEnterpriseRules.Idle(Wallet.Pdm) || sellerTesting || !Valid(sellerPed) || Game.Player.Character.Position.DistanceTo(sellerPed.Position)>5){Tell("Vendedor","Aproxime-se do vendedor a pe, encerre o teste e saia do carro antes de negociar.");return false;}return true;
        }
        void SellerChange(Func<bool> action){if(!SellerAction())return;if(!action()){ui.Problem("Negociacao recusada","Confira caixa e proposta. Nenhuma compra nova confirmada.",null,OpenSeller);return;}OpenSeller();}
        void OpenSeller()
        {
            Page("seller","PDM / VENDEDOR PARTICULAR");var seller=Data.Seller;
            if(seller==null){Add("Procurar particular","Agenda um vendedor com carro instalado. Nao compra ao agendar.",()=>{if(office()){CreateSeller();OpenSeller();}});}
            else{
                ui.Add(seller.Car.Name+" / $"+seller.Asking,"Quilometragem "+seller.Car.Mileage+" mi. Inspecione o carro presencialmente antes de comprar. Estado da negociacao: "+(seller.Rejected?"recusada":seller.Inspected?"inspecionado":"aguardando inspecao"),()=>{},false);
                Add("GPS / vendedor","Aguardando no patio da empresa.",()=>ui.Waypoint(Pos(options.SellerPed),"vendedor particular PDM"));
                Add("Inspecionar / test-drive","Perto do vendedor: circule o carro por seis segundos ou entre para testar. Retorne em ate dois minutos; danos/destruicao encerram a oferta.",()=>StartSellerInspection());
                if(seller.Inspected && !seller.Rejected){
                    if(!seller.Countered)Add("Negociar preco...","Uma contraproposta; abaixo do minimo do vendedor encerra o negocio.",()=>{var id=seller.Id;string text=Game.GetUserInput(WindowTitle.EnterMessage60,(seller.Asking*90/100).ToString(),10);if(PhoneBankRules.Amount(text,out int amount))SellerChange(()=>PdmEnterpriseTransactions.CounterSeller(Wallet.EnterpriseWallet(),id,amount,Guid.NewGuid().ToString("N"),Wallet.Save,out _));});
                    ui.AddConfirm("Comprar por $"+seller.Asking,"Debita caixa empresarial e registra o carro inspecionado no estoque, incluindo aparencia e quilometragem. Nao vira veiculo pessoal.",()=>{if(!SellerAction())return;if(!Valid(sellerCar) || sellerCar.IsDead || sellerCar.Occupants.Length>0){ui.Problem("Compra recusada","Pare e saia do carro do vendedor; o veiculo deve estar presente e inteiro.",null,OpenSeller);return;}if(PdmEnterpriseTransactions.BuySeller(Wallet.EnterpriseWallet(),company,seller.Id,Guid.NewGuid().ToString("N"),Wallet.Save,out bool changed)){if(changed)Tell("Compra particular",seller.Car.Name+" adquirido para o estoque.");ReleaseSeller();OpenSeller();}else ui.Problem("Compra recusada","Confira caixa, vagas e contexto. Nenhuma compra nova confirmada.",null,OpenSeller);});
                }
                ui.AddConfirm("Recusar vendedor","Encerra a oferta; nao debita caixa nem adiciona estoque.",()=>SellerChange(()=>PdmEnterpriseTransactions.DismissSeller(Wallet.EnterpriseWallet(),seller.Id,Guid.NewGuid().ToString("N"),Wallet.Save,out _)));
            }
            Add("Voltar","Operacao.",OpenMenu);ui.Show();
        }
        void StartSellerInspection()
        {
            if(!Safe || !Valid(sellerCar) || !Valid(sellerPed) || Game.Player.Character.Position.DistanceTo(sellerPed.Position)>5){Tell("Inspecao","Va a pe ao vendedor no patio e aguarde o carro aparecer.");return;}
            if(!gate.Acquire(Name)){Tell("Inspecao","Encerre a outra atividade antes de testar.");return;}
            sellerInspection=new PdmInspection();sellerTesting=true;sellerTestAt=Game.GameTime;ui.Close();sellerCar.LockStatus=VehicleLockStatus.Unlocked;Tell("Inspecao","Observe o carro por seis segundos ou entre para testar. Volte, estacione e saia para concluir a inspecao. End cancela o teste.");
        }
        void SellerTick()
        {
            var sale=Data.Seller;if(sale==null || !Local && !sellerTesting){ReleaseSeller();return;}
            if(sellerId!=sale.Id){ReleaseSeller();sellerId=sale.Id;}
            var model=PdmVehicleModel.Resolve(sale.Car.Model);try{
                if(!PdmStreaming.Ready(model))return;
                if(!Valid(sellerCar)){if(Game.GameTime<sellerNext)return;sellerNext=Game.GameTime+5000;var bay=RigSpawn.FindVehicle(options.SellerCar,model,"PDM vendedor");if(bay==null)return;sellerCar=PdmAssets.Track(World.CreateVehicle(model,Pos(bay),bay.Heading));if(!Valid(sellerCar))return;sellerCar.IsPersistent=true;Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT,sellerCar,sale.Car.Plate);PdmStockPresentation.Apply(sellerCar,sale.Car);return;}
                if(!Valid(sellerPed)){if(Game.GameTime<sellerNext)return;sellerNext=Game.GameTime+5000;sellerPed=PdmStreaming.Buyer(Pos(options.SellerPed),sale.Id);if(Valid(sellerPed)){sellerPed.BlockPermanentEvents=true;sellerBlip=sellerPed.AddBlip();sellerBlip.Name="PDM / vendedor particular";}return;}
                if(sellerTesting){
                    if(!PdmMenuRuntime.WorldAllowed || sellerCar.IsDead || sellerPed.IsDead || Game.Player.Character.IsDead || Game.Player.Wanted.WantedLevel>0 || Game.GameTime-sellerTestAt>120000 || sellerCar.Position.DistanceTo(Pos(options.SellerCar))>600){sellerTesting=false;sellerInspection=null;gate.Release(Name);Tell("Inspecao","Teste interrompido. Retorne ao vendedor; nenhuma compra foi feita.");return;}
                    bool returned=sellerCar.Position.DistanceTo(Pos(options.SellerCar))<15 && sellerCar.Speed<.5f && sellerCar.Occupants.Length==0 && Game.Player.Character.Position.DistanceTo(sellerCar.Position)<5;
                    if(sellerInspection.Update(Game.GameTime,returned)){
                        var appearance=VehicleSnapshot.Capture(sellerCar);
                        if(PdmEnterpriseTransactions.InspectSeller(Wallet.EnterpriseWallet(),sale.Id,appearance,sale.Id+":inspection",Wallet.Save,out _)){sellerTesting=false;gate.Release(Name);Tell("Inspecao","Inspecao concluida. Aproxime-se do vendedor e use "+Cfg.PdmInteractionKey+" para negociar.");}
                    }
                    else Prompt.Help("Teste/inspecao PDM: retorne, pare e saia; observe o carro por seis segundos. End: cancelar.");
                }else if(Safe && !pool.AreAnyVisible && Game.Player.Character.Position.DistanceTo(sellerPed.Position)<3){Prompt.Help(PdmCustomerInput.Button+": conversar com vendedor particular");if(PdmCustomerInput.Pressed)OpenSeller();}
            }finally{PdmStreaming.Release(model);}
        }
        void ReleaseSeller()
        {
            if(sellerTesting){sellerTesting=false;gate.Release(Name);}sellerInspection=null;
            if(sellerBlip!=null && sellerBlip.Exists())sellerBlip.Delete();sellerBlip=null;
            if(Valid(sellerPed))JobActors.Retire(sellerPed);sellerPed=null;
            if(Valid(sellerCar)){if(sellerCar.Occupants.Length==0)sellerCar.Delete();else JobVehicles.Retire(sellerCar);}sellerCar=null;sellerId=null;
        }
    }
}
