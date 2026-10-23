using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using LemonUI;
using LemonUI.Menus;

namespace RPCore
{
    public sealed class PrisonTransportJob : ISystem,IWorkContract
    {
        public string Name=>"prison";
        public string Label=>"Transporte prisional";
        public bool Active=>run!=null;
        public Vector3 StartPosition=>Pos(options.Start);
        readonly ObjectPool pool;
        readonly AchievementSystem achievements;
        readonly ShiftGate gate;
        readonly PrisonOptions options;
        readonly NativeMenu menu;
        readonly Blip startBlip;
        readonly List<Ped> prisoners=new List<Ped>();
        readonly HashSet<int> waitingOutside=new HashSet<int>();
        readonly Random random=new Random();
        Vehicle bus;
        Blip busBlip,targetBlip;
        PrisonRun run;
        Vector3 loadingPoint;
        int cancelSince=-1,nextPay,unloadUntil,awaySince=-1;
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        bool InputAllowed=>!Game.IsPaused && !InterfaceOptions.ModalOpen && !pool.AreAnyVisible;
        bool Driver=>bus!=null && bus.Exists() && Game.Player.Character.IsInVehicle(bus) && bus.Driver==Game.Player.Character;
        bool At(Vector3 point,float radius)=>PrisonRules.Stopped(Driver,bus.Speed,bus.Position.DistanceTo(point),radius);
        int Seated=>prisoners.Where((p,i)=>p.Exists() && !p.IsDead && p.IsInVehicle(bus) && bus.GetPedOnSeat((VehicleSeat)i)==p).Count();
        int Outside=>prisoners.Count(p=>p.Exists() && !p.IsDead && !p.IsInVehicle() && p.Position.DistanceTo(Pos(options.Destination))<45 && p.Position.DistanceTo(bus.Position)<35);
        public PrisonTransportJob(ObjectPool pool,AchievementSystem achievements,ShiftGate gate)
        {
            this.pool=pool;this.achievements=achievements;this.gate=gate;
            var defaults=PrisonDefaults.Load();options=Store.LoadDef("Jobs/PrisonTransport.json",()=>defaults);
            if(options==null || !options.Valid){Log.Write("Invalid PrisonTransport.json; embedded defaults used, original retained.");options=defaults;}
            menu=new NativeMenu(Label,"LSPD / CUSTODIA");pool.Add(menu);Visuals.Register(menu,Name);
            startBlip=MapBlips.Create(StartPosition,"RPCore Job: transporte prisional",BlipSprite.Truck,BlipColor.Blue,.8f);
        }
        void Add(string title,string description,Action action)
        {
            var row=new NativeItem(title,description);
            row.Activated+=(s,e)=>{try{action();}catch(Exception ex){Log.Error("prison action",ex);Notify.Show("Acao nao concluida. Veja RPCore.log.");}};menu.Add(row);
        }
        public void OpenMenu()
        {
            pool.HideAll();menu.Clear();var progress=Wallet.JobProgress(Name);
            menu.Name="LSPD / "+progress.Deliveries+" transportes / $"+progress.Earned;
            Add("GPS: delegacia Mission Row","Contratacao a pe no lado publico da delegacia.",()=>{Waypoint(StartPosition);menu.Visible=false;});
            if(!Active)Add("Iniciar contrato","Retire o onibus, confira "+options.Prisoners+" detentos, leve a Bolingbroke e retorne.",Begin);
            else
            {
                Add("GPS: objetivo atual","Onibus, penitenciaria ou devolucao a delegacia.",()=>{Gps();menu.Visible=false;});
                Add("Encerrar contrato","Mantem custodia ja paga; cancela o restante sem bonus.",()=>End("Contrato prisional cancelado."));
            }
            menu.Visible=true;
        }
        static void Waypoint(Vector3 p)=>Function.Call(Hash.SET_NEW_WAYPOINT,p.X,p.Y);
        Vector3 Target=>!Active?StartPosition:run.Stage==PrisonStage.Returning?StartPosition:run.Stage==PrisonStage.AwaitDriver || run.Stage==PrisonStage.Loading?loadingPoint:Pos(options.Destination);
        public void Gps()=>Waypoint(Target);
        void Route(string name)
        {
            ClearRoute();targetBlip=World.CreateBlip(Target);targetBlip.Name="LSPD: "+name;
            targetBlip.Sprite=run.Stage==PrisonStage.Returning?BlipSprite.Garage:BlipSprite.Truck;targetBlip.Color=BlipColor.Yellow;targetBlip.Scale=.8f;targetBlip.ShowRoute=true;
        }
        void Begin()
        {
            var p=Game.Player.Character;
            if(Active)return;
            if(p.IsDead || !p.IsHuman || p.IsInVehicle() || p.IsRagdoll || p.IsInCombat || p.Position.DistanceTo(StartPosition)>6 || Game.IsMissionActive || Game.IsPaused || Game.Player.Wanted.WantedLevel>0)
            {Notify.Show("Contrate a pe em Mission Row, sem procurado, combate ou missao.");return;}
            if(!gate.Acquire(Name)){Notify.Show("Encerre o trabalho de "+gate.Owner+" antes de iniciar outro.");return;}
            bool started=false;
            var model=new Model(VehicleHash.PBus);
            try
            {
                var spawn=RigSpawn.FindVehicle(options.Spawn,model,"Prison bus");
                if(spawn==null){Notify.Show("Nao encontramos vaga para o onibus. Veja Prison bus spawn no RPCore.log.");return;}
                bus=World.CreateVehicle(model,Pos(spawn),spawn.Heading);
                if(bus==null || !bus.Exists()){Notify.Show("Onibus prisional indisponivel.");return;}
                bus.IsPersistent=true;bus.IsStolen=false;bus.NeedsToBeHotwired=false;bus.LockStatus=VehicleLockStatus.Unlocked;
                Function.Call(Hash.SET_VEHICLE_HAS_BEEN_OWNED_BY_PLAYER,bus,true);
                Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT,bus,"PD"+random.Next(100000,999999));
                float capacity=bus.PetrolTankVolume;bus.FuelLevel=capacity>0 && !float.IsNaN(capacity) && !float.IsInfinity(capacity)?Math.Min(capacity,10000):100;
                if(bus.PassengerCapacity<options.Prisoners){Notify.Show("O modelo instalado nao tem assentos suficientes para este contrato.");return;}
                if(p!=Game.Player.Character || p.IsDead || Game.IsMissionActive)return;
                JobVehicles.Reserve(bus);loadingPoint=bus.Position;
                busBlip=World.CreateBlip(bus.Position);busBlip.Name="LSPD: onibus do contrato";busBlip.Sprite=BlipSprite.Truck;busBlip.Color=BlipColor.Blue;
                run=new PrisonRun(options.Prisoners,options.LoadSeconds);cancelSince=awaySince=-1;nextPay=0;
                Route("retirar onibus");menu.Visible=false;started=true;
                Notify.Show("Entre como motorista no onibus azul. Pare na retirada e pressione E para conferir a custodia.");
            }
            finally
            {
                model.MarkAsNoLongerNeeded();
                if(!started){try{Cleanup();}finally{run=null;gate.Release(Name);}}
            }
        }
        bool FillManifest()
        {
            // Like the reference, initialize only newly created inmates in the supplied bus. Never move existing peds/player.
            var model=new Model(PedHash.Prisoner01SMY);
            try
            {
                if(!model.IsValid || !model.IsInCdImage || !model.Request(2500))return false;
                for(int i=0;i<options.Prisoners;i++)
                {
                    if(!bus.IsSeatFree((VehicleSeat)i))return false;
                    var inmate=World.CreatePed(model,bus.GetOffsetPosition(new Vector3(4,0,1)));
                    if(inmate==null || !inmate.Exists())return false;
                    prisoners.Add(inmate);inmate.IsPersistent=true;inmate.BlockPermanentEvents=true;
                    inmate.Weapons.RemoveAll();inmate.CanBeDraggedOutOfVehicle=false;inmate.StaysInVehicleWhenJacked=true;
                    // Only this owned NPC's relationship changes; police/wanted state and global relationships stay intact.
                    Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH,inmate,(int)RelationshipGroupHash.CivilianMale);
                    inmate.SetIntoVehicle(bus,(VehicleSeat)i);
                }
                return true;
            }
            finally{model.MarkAsNoLongerNeeded();}
        }
        public void Tick()
        {
            if(!Active)
            {
                if(Game.Player.Character.IsDead || Game.IsMissionActive || !InputAllowed)return;
                Marker(StartPosition,2);
                if(!Game.Player.Character.IsInVehicle() && Game.Player.Character.Position.DistanceTo(StartPosition)<3)
                {Prompt.Help("~INPUT_CONTEXT~: contrato de transporte prisional");if(Game.IsControlJustPressed(GTA.Control.Context))OpenMenu();}
                return;
            }
            if(Game.Player.Character.IsDead || Game.IsMissionActive || !bus.Exists() || bus.IsDead || !bus.IsDriveable)
            {End("Transporte encerrado: motorista ou onibus indisponivel.");return;}
            if(prisoners.Any(p=>!p.Exists() || p.IsDead)){End("Custodia perdida. Transporte encerrado sem novo pagamento.");return;}
            if(Game.IsPaused || InterfaceOptions.ModalOpen || pool.AreAnyVisible){run.InterruptLoading();return;}
            if(busBlip!=null && busBlip.Exists()){busBlip.Position=bus.Position;busBlip.Alpha=Driver?0:255;}
            if(!Driver)
            {
                run.InterruptLoading();if(awaySince<0)awaySince=Game.GameTime;
                if(unchecked(Game.GameTime-awaySince)>60000){End("Onibus abandonado. Contrato encerrado.");return;}
                Status("volte ao banco do motorista do onibus azul");return;
            }
            awaySince=-1;
            switch(run.Stage)
            {
                case PrisonStage.AwaitDriver:
                    Status("pare na retirada e confira a custodia");Marker(loadingPoint,3);
                    if(At(loadingPoint,25)){Prompt.Help("~INPUT_CONTEXT~: conferir e receber "+options.Prisoners+" detentos");if(Confirm())run.BeginLoading(Game.GameTime,true);}
                    break;
                case PrisonStage.Loading:
                    Status("conferindo custodia; mantenha parado");
                    if(!run.LoadReady(Game.GameTime,At(loadingPoint,25)))return;
                    if(!FillManifest()){End("Nao foi possivel formar a custodia no onibus. Tente outro contrato.");return;}
                    if(!run.ManifestConfirmed(At(loadingPoint,25),Seated)){End("Conferencia de assentos falhou. Sem pagamento.");return;}
                    Route("Bolingbroke / recepcao");Notify.Show("Custodia confirmada: "+options.Prisoners+" detentos. Siga o GPS ate a recepcao externa de Bolingbroke.");
                    break;
                case PrisonStage.Transporting:
                    if(Seated!=options.Prisoners){End("Detento saiu do assento antes do destino. Contrato encerrado.");return;}
                    Status("leve "+Seated+" detentos a Bolingbroke");Marker(Pos(options.Destination),4);
                    if(At(Pos(options.Destination),18))
                    {Prompt.Help("~INPUT_CONTEXT~ / buzina: entregar custodia e desembarcar");if(Confirm() && run.BeginUnload(true,Seated)){OrderExit();unloadUntil=Game.GameTime+120000;}}
                    break;
                case PrisonStage.Unloading:
                    Status("aguarde o desembarque "+Outside+"/"+options.Prisoners);
                    foreach(var inmate in prisoners)if(!inmate.IsInVehicle() && waitingOutside.Add(inmate.Handle))Function.Call(Hash.TASK_STAND_STILL,inmate,-1);
                    if(run.ConfirmUnloaded(At(Pos(options.Destination),25),Outside))break;
                    if(unchecked(Game.GameTime-unloadUntil)>=0){End("Tempo de desembarque esgotado. Custodia nao confirmada; sem pagamento.");return;}
                    if(At(Pos(options.Destination),25)){Prompt.Help("Aguarde os detentos sairem. ~INPUT_CONTEXT~: tentar desembarque novamente");if(Confirm())OrderExit();}
                    else Prompt.Help("Retorne ao ponto de entrega e pare para concluir a custodia.");
                    break;
                case PrisonStage.PaymentPending:
                    Status("confirmando custodia e pagamento");
                    if(run.PayCustody(At(Pos(options.Destination),25) && Outside==options.Prisoners,step=>Pay(step,DeliveryRoute.Pay(options.BasePay,options.PayPerMeter,DeliveryRoute.Distance(options.Start,options.Destination)),true,false)))
                    {ReleasePrisoners();Route("devolver onibus em Mission Row");Notify.Show("Custodia recebida e paga. Devolva o onibus vazio em Mission Row para o bonus.");}
                    break;
                case PrisonStage.Returning:
                    Status("devolva o onibus vazio em Mission Row");Marker(StartPosition,4);
                    if(At(StartPosition,25) && bus.PassengerCount==0)
                    {Prompt.Help("~INPUT_CONTEXT~ / buzina: devolver onibus e receber bonus");if(Confirm() && run.Return(true,step=>Pay(step,options.ReturnBonus,false,true)))End("Transporte concluido. Desca para recolhermos o onibus.");}
                    break;
            }
        }
        void OrderExit(){foreach(var inmate in prisoners)if(inmate.Exists() && inmate.IsInVehicle(bus))JobActors.Leave(inmate,bus);}
        bool Confirm()=>InputAllowed && (Game.IsControlJustPressed(GTA.Control.Context)||Game.IsControlJustPressed(GTA.Control.VehicleHorn));
        bool Pay(int step,int amount,bool delivery,bool complete)
        {
            if(unchecked(Game.GameTime-nextPay)<0)return false;
            bool credited;
            try
            {
                if(!Wallet.PayDelivery(Name,run.Id,step,amount,true,delivery,complete,out credited)){nextPay=Game.GameTime+3000;Notify.Show("Saldo/progresso no limite. Pagamento pendente.");return false;}
            }
            catch(Exception ex){nextPay=Game.GameTime+5000;Log.Error("prison payment",ex);Notify.Show("Falha ao salvar. Pagamento pendente; permaneca no destino.");return false;}
            if(credited)
            {
                Notify.Show("~g~+$"+amount+" no banco~s~ / transporte prisional");
                try{Sfx.Play("delivered");if(delivery)achievements.Bump("prison_transports");if(complete)achievements.Bump("prison_shifts");achievements.Bump("cash_earned",amount);achievements.Save();}
                catch(Exception ex){Log.Error("prison presentation after payment",ex);}
            }
            return true;
        }
        void ReleasePrisoners()
        {
            foreach(var inmate in prisoners)if(inmate.Exists())
            {
                inmate.CanBeDraggedOutOfVehicle=true;inmate.StaysInVehicleWhenJacked=false;
                if(!inmate.IsInVehicle())JobActors.Walk(inmate,inmate.Position+new Vector3(3,-3,0));
                JobActors.Retire(inmate);
            }
            prisoners.Clear();waitingOutside.Clear();
        }
        void ClearRoute(){if(targetBlip!=null && targetBlip.Exists())targetBlip.Delete();targetBlip=null;}
        void Cleanup()
        {
            ReleasePrisoners();ClearRoute();if(busBlip!=null && busBlip.Exists())busBlip.Delete();busBlip=null;
            JobVehicles.Retire(bus);bus=null;
        }
        void End(string message){run?.Cancel();try{Cleanup();}finally{run=null;gate.Release(Name);menu.Visible=false;}Notify.Show(message);}
        void Status(string text){if(!Visuals.TryJobHeader(Name,"LSPD / "+text,Color.White))new TextElement("LSPD / "+text,new PointF(GTA.UI.Screen.ScaledWidth/2,20),.36f,Color.White,GTA.UI.Font.ChaletLondon,Alignment.Center).Draw();}
        static void Marker(Vector3 p,float size){if(Game.Player.Character.Position.DistanceTo(p)<65)World.DrawMarker(MarkerType.Cylinder,p-new Vector3(0,0,1),Vector3.Zero,Vector3.Zero,new Vector3(size,size,1),Color.FromArgb(110,80,160,255));}
        public void PollCancel(){if(!Active || !InputAllowed || !Game.IsKeyPressed(Keys.End)){cancelSince=-1;return;}if(cancelSince<0)cancelSince=Game.GameTime;if(unchecked(Game.GameTime-cancelSince)>=1000)End("Contrato prisional cancelado. Pagamentos confirmados mantidos.");}
        public void OnKey(KeyEventArgs e){}
        public void Reload(){if(Active)End("Contrato prisional encerrado na troca de perfil.");menu.Visible=false;}
        public void Save(){}
        public void Dispose(){try{Cleanup();}finally{run=null;gate.Release(Name);menu.Visible=false;if(startBlip!=null && startBlip.Exists())startBlip.Delete();}}
    }
}
