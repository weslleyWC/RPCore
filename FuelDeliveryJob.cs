using System;
using System.Drawing;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using LemonUI;
using LemonUI.Menus;

namespace RPCore
{
    public sealed class FuelDeliveryJob : ISystem, IWorkContract
    {
        public string Name => "tanker";
        public string Label => "Transporte de combustivel";
        public bool Active => run != null;
        public Vector3 StartPosition => Pos(options.Start);
        readonly ObjectPool pool;
        readonly AchievementSystem achievements;
        readonly ShiftGate gate;
        readonly TankerOptions options;
        readonly NativeMenu menu;
        readonly Random random = new Random();
        readonly Blip startBlip;
        Vehicle truck, trailer;
        Blip truckBlip, trailerBlip, targetBlip;
        JobPoint station;
        TankerRun run;
        bool routeToTrailer;
        int cancelSince = -1, nextPay;
        static Vector3 Pos(JobPoint p) => new Vector3(p.X,p.Y,p.Z);
        bool InputAllowed => !Game.IsPaused && !InterfaceOptions.ModalOpen && !pool.AreAnyVisible;
        bool Driver => truck != null && truck.Exists() && Game.Player.Character.IsInVehicle(truck) && truck.Driver == Game.Player.Character;
        bool Coupled => JobVehicles.Towing(truck,trailer);
        public FuelDeliveryJob(ObjectPool pool, AchievementSystem achievements, ShiftGate gate)
        {
            this.pool=pool;this.achievements=achievements;this.gate=gate;
            var defaults=TankerDefaults.Load();options=Store.LoadDef("Jobs/FuelDelivery.json",()=>defaults);
            if(options==null || !options.ValidTanker){Log.Write("Invalid FuelDelivery.json; embedded defaults used, original retained.");options=defaults;}
            menu=new NativeMenu(Label,"LS GAS / CONTRATO");pool.Add(menu);Visuals.Register(menu,Name);
            startBlip=MapBlips.Create(StartPosition,"RPCore Job: LS GAS",BlipSprite.Truck,BlipColor.Yellow,.8f);
        }
        void Add(string title,string description,Action action)
        {
            var row=new NativeItem(title,description);
            row.Activated+=(s,e)=>{try{action();}catch(Exception ex){Log.Error(Name+" action",ex);Notify.Show("Acao nao concluida. Veja RPCore.log.");}};
            menu.Add(row);
        }
        public void OpenMenu()
        {
            pool.HideAll();menu.Clear();var progress=Wallet.JobProgress(Name);
            menu.Name="LS GAS / "+progress.Deliveries+" entregas / $"+progress.Earned;
            Add("GPS: deposito LS GAS","Local de contratacao no porto.",()=>{Waypoint(StartPosition);menu.Visible=false;});
            if(!Active)Add("Iniciar contrato","Retire o cavalo mecanico e engate o tanque. Descarregue no posto e devolva os dois.",Begin);
            else
            {
                Add("GPS: objetivo atual","Carreta, posto de destino ou retorno ao deposito.",()=>{Gps();menu.Visible=false;});
                if(!Cfg.EnableVehicleControls)Add("Motor do caminhao: ligar/desligar","Alternativa quando os controles de veiculo estao desativados.",()=>{
                    if(!Driver || truck.Speed>=.5f){Notify.Show("Pare como motorista no Hauler do contrato.");return;}
                    bool on=!truck.IsEngineRunning;Function.Call(Hash.SET_VEHICLE_ENGINE_ON,truck,on,true,!on);menu.Visible=false;
                });
                Add("Encerrar turno","Mantem descarga ja paga; nao paga bonus de retorno.",()=>End("Contrato cancelado."));
            }
            menu.Visible=true;
        }
        static void Waypoint(Vector3 p) => Function.Call(Hash.SET_NEW_WAYPOINT,p.X,p.Y);
        public void Gps(){Waypoint(Active ? (routeToTrailer ? trailer.Position : run.Stage==TankerStage.Returning ? Pos(options.Depot) : Pos(station)) : StartPosition);}
        void Begin()
        {
            var p=Game.Player.Character;
            if(Active)return;
            if(p.IsDead || !p.IsHuman || p.IsInVehicle() || p.IsRagdoll || p.IsInCombat || p.Position.DistanceTo(StartPosition)>6 || Game.IsMissionActive || Game.IsPaused || Game.Player.Wanted.WantedLevel>0)
            {Notify.Show("Contrate a pe no deposito LS GAS, sem combate, missao ou procurado.");return;}
            if(!gate.Acquire(Name)){Notify.Show("Encerre o trabalho de "+gate.Owner+" antes de iniciar outro.");return;}
            bool started=false;
            try
            {
                station=TankerDefaults.Select(options,random);
                var slot=RigSpawn.Find(options.Trailer);
                if(slot==null)
                {Notify.Show("Nao encontramos duas vagas livres no patio LS GAS. Veja RPCore.log para o diagnostico e tente novamente.");return;}
                var trailerPoint=Pos(slot.Trailer);var tractorPoint=Pos(slot.Truck);
                trailer=Create(VehicleHash.Tanker,trailerPoint,options.Trailer.Heading);
                if(trailer==null || !trailer.Exists()){Notify.Show("Modelo do tanque indisponivel.");return;}
                truck=Create(VehicleHash.Hauler,tractorPoint,options.Trailer.Heading);
                if(truck==null || !truck.Exists()){Notify.Show("Modelo do cavalo mecanico indisponivel.");return;}
                if(p!=Game.Player.Character || p.IsDead || Game.IsMissionActive)return;
                JobVehicles.Reserve(truck);JobVehicles.Reserve(trailer);
                truckBlip=World.CreateBlip(truck.Position);truckBlip.Name="LS GAS: cavalo mecanico";truckBlip.Sprite=BlipSprite.Truck;truckBlip.Color=BlipColor.Blue;
                trailerBlip=World.CreateBlip(trailer.Position);trailerBlip.Name="LS GAS: carreta-tanque";trailerBlip.Sprite=BlipSprite.Truck;trailerBlip.Color=BlipColor.Yellow;
                run=new TankerRun(options.UnloadSeconds);nextPay=0;cancelSince=-1;
                SetRoute(true);menu.Visible=false;started=true;
                Notify.Show("LS GAS: entre no Hauler azul, alinhe a traseira com a frente do tanque e engate. E ajuda quando alinhado e parado.");
            }
            finally
            {
                if(!started){try{Cleanup();}finally{run=null;gate.Release(Name);}}
            }
        }
        Vehicle Create(VehicleHash hash,Vector3 point,float heading)
        {
            var model=new Model(hash);
            try
            {
                if(!model.IsValid || !model.IsInCdImage || !model.Request(2500))return null;
                var v=World.CreateVehicle(model,point,heading);
                if(v==null || !v.Exists())return null;
                // Assign immediately, so a native exception below still leaves cleanup an owner.
                if(hash==VehicleHash.Tanker)trailer=v;else truck=v;
                v.IsPersistent=true;v.IsStolen=false;v.NeedsToBeHotwired=false;
                Function.Call(Hash.SET_VEHICLE_HAS_BEEN_OWNED_BY_PLAYER,v,true);
                Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT,v,"LG"+random.Next(100000,999999));
                float capacity=v.PetrolTankVolume;v.FuelLevel=capacity>0 && !float.IsNaN(capacity) && !float.IsInfinity(capacity)?Math.Min(10000,capacity):100;
                return v;
            }
            finally{model.MarkAsNoLongerNeeded();}
        }
        void ClearRoute(){if(targetBlip!=null && targetBlip.Exists())targetBlip.Delete();targetBlip=null;}
        void SetRoute(bool toTrailer)
        {
            routeToTrailer=toTrailer;ClearRoute();
            var point=toTrailer?trailer.Position:run.Stage==TankerStage.Returning?Pos(options.Depot):Pos(station);
            targetBlip=World.CreateBlip(point);targetBlip.Sprite=toTrailer?BlipSprite.Truck:run.Stage==TankerStage.Returning?BlipSprite.Garage:BlipSprite.JerryCan;
            targetBlip.Color=BlipColor.Yellow;targetBlip.Scale=.8f;targetBlip.ShowRoute=true;
            targetBlip.Name=toTrailer?"LS GAS: engatar tanque":run.Stage==TankerStage.Returning?"LS GAS: devolver conjunto":"LS GAS: "+station.Name;
        }
        bool StoppedAt(Vector3 target,float radius) => TankerRules.Stopped(Driver,Coupled,truck!=null && truck.Exists() && !truck.IsEngineRunning,
            truck.Speed,trailer.Speed,truck.Position.DistanceTo(target),trailer.Position.DistanceTo(target),radius);
        void TryCouple()
        {
            if(!Driver || !InputAllowed)return;
            bool hasTrailer=Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER,truck) || JobVehicles.TrailerHasOtherTractor(trailer,truck);
            float delta=Math.Abs(truck.Heading-trailer.Heading)%360;delta=Math.Min(delta,360-delta);
            bool ready=TankerRules.CanCouple(true,hasTrailer,truck.RearPosition.DistanceTo(trailer.FrontPosition),delta,truck.Speed,trailer.Speed);
            Prompt.Help(ready?"~INPUT_CONTEXT~: engatar tanque da LS GAS":"Alinhe a traseira do Hauler com a frente do tanque; engate dando re devagar.");
            if(ready && Game.IsControlJustPressed(GTA.Control.Context))Function.Call(Hash.ATTACH_VEHICLE_TO_TRAILER,truck,trailer,6f);
        }
        public void Tick()
        {
            if(!Active)
            {
                if(Game.Player.Character.IsDead || Game.IsMissionActive)return;
                if(Game.Player.Character.Position.DistanceTo(StartPosition)<40)Marker(StartPosition,2);
                if(InputAllowed && !Game.Player.Character.IsInVehicle() && Game.Player.Character.Position.DistanceTo(StartPosition)<3)
                {Prompt.Help("~INPUT_CONTEXT~: contrato de transporte de combustivel LS GAS");if(Game.IsControlJustPressed(GTA.Control.Context))OpenMenu();}
                return;
            }
            if(Game.Player.Character.IsDead || Game.IsMissionActive || !truck.Exists() || truck.IsDead || !trailer.Exists() || trailer.IsDead)
            {End("Contrato encerrado: motorista ou conjunto indisponivel.");return;}
            if(Game.IsPaused || InterfaceOptions.ModalOpen || pool.AreAnyVisible){run.Interrupt();return;}
            if(truckBlip!=null && truckBlip.Exists()){truckBlip.Position=truck.Position;truckBlip.Alpha=Driver?0:255;}
            if(trailerBlip!=null && trailerBlip.Exists())trailerBlip.Position=trailer.Position;
            bool coupled=Coupled;
            if(run.ObserveCoupling(coupled))SetRoute(false);
            if(!coupled)
            {
                run.Interrupt();if(!routeToTrailer)SetRoute(true);
                if(targetBlip!=null && targetBlip.Exists())targetBlip.Position=trailer.Position;
                Marker(trailer.Position,2);Status("engate o tanque ao Hauler azul");TryCouple();return;
            }
            if(routeToTrailer)SetRoute(false);
            var target=run.Stage==TankerStage.Returning?Pos(options.Depot):Pos(station);
            Marker(target,4);
            bool eligible=StoppedAt(target,run.Stage==TankerStage.Returning?40:25);
            if(run.Stage==TankerStage.Returning)
            {
                Status("retorne com o tanque ao deposito");
                if(eligible)
                {Prompt.Help("~INPUT_CONTEXT~ / buzina: devolver conjunto e receber bonus");if(Confirm() && run.Return(true,step=>Pay(step,options.ReturnBonus,false,true)))End("Contrato concluido. Desca para recolhermos o conjunto.");}
                else if(truck.Position.DistanceTo(target)<60)Prompt.Help("Pare o conjunto na base e desligue o motor ("+(Cfg.EnableVehicleControls?Cfg.EngineKey.ToString():"menu do contrato")+") para devolver.");
                return;
            }
            Status(run.Stage==TankerStage.Unloading?"descarregando; mantenha parado":run.Stage==TankerStage.PaymentPending?"descarga concluida; confirmar pagamento":"leve o tanque a "+station.Name);
            if(run.Stage==TankerStage.Delivering)
            {
                if(eligible){Prompt.Help("~INPUT_CONTEXT~ / buzina: iniciar descarga de combustivel");if(Confirm())run.BeginUnload(Game.GameTime,true);}
                else if(truck.Position.DistanceTo(target)<65)Prompt.Help("Pare o conjunto junto ao posto e desligue o motor ("+(Cfg.EnableVehicleControls?Cfg.EngineKey.ToString():"menu do contrato")+").");
            }
            if(run.Stage==TankerStage.Unloading)
            {
                if(!eligible){run.Interrupt();Notify.Show("Descarga interrompida. Pare e desligue o motor para reiniciar.");}
                else{Prompt.Help("Descarregando... mantenha o conjunto parado, engatado e com motor desligado.");run.Advance(Game.GameTime,true);}
            }
            if(run.Stage==TankerStage.PaymentPending && eligible && run.PayUnload(true,step=>Pay(step,DeliveryRoute.Pay(options.BasePay,options.PayPerMeter,DeliveryRoute.Distance(options.Start,station)),true,false)))
            {SetRoute(false);Notify.Show("Descarga confirmada. Retorne com o mesmo conjunto para o bonus.");}
        }
        bool Confirm()=>InputAllowed && (Game.IsControlJustPressed(GTA.Control.Context)||Game.IsControlJustPressed(GTA.Control.VehicleHorn));
        bool Pay(int step,int amount,bool delivery,bool complete)
        {
            if(unchecked(Game.GameTime-nextPay)<0)return false;
            bool credited;
            try
            {
                if(!Wallet.PayDelivery(Name,run.Id,step,amount,true,delivery,complete,out credited))
                {nextPay=Game.GameTime+3000;Notify.Show("Saldo/progresso no limite. Pagamento pendente.");return false;}
            }
            catch(Exception ex){nextPay=Game.GameTime+5000;Log.Error("LS GAS payment",ex);Notify.Show("Falha ao salvar. Pagamento pendente; permaneça no ponto para tentar novamente.");return false;}
            if(credited)
            {
                Notify.Show("~g~+$"+amount+" no banco~s~ / LS GAS");
                try{Sfx.Play("delivered");if(delivery)achievements.Bump("tanker_drops");if(complete)achievements.Bump("tanker_shifts");achievements.Bump("cash_earned",amount);achievements.Save();}
                catch(Exception ex){Log.Error("LS GAS presentation after payment",ex);}
            }
            return true;
        }
        void Status(string text)
        {
            if(!Visuals.TryJobHeader(Name,"LS GAS / "+text,Color.White))new TextElement("LS GAS / "+text,new PointF(GTA.UI.Screen.ScaledWidth/2,20),.36f,Color.White,GTA.UI.Font.ChaletLondon,Alignment.Center).Draw();
        }
        static void Marker(Vector3 p,float radius){if(Game.Player.Character.Position.DistanceTo(p)<80)World.DrawMarker(MarkerType.Cylinder,p-new Vector3(0,0,1),Vector3.Zero,Vector3.Zero,new Vector3(radius,radius,1),Color.FromArgb(110,255,190,0));}
        public void PollCancel()
        {
            if(!Active || !InputAllowed || !Game.IsKeyPressed(Keys.End)){cancelSince=-1;return;}
            if(cancelSince<0)cancelSince=Game.GameTime;
            if(unchecked(Game.GameTime-cancelSince)>=1000)End("Contrato cancelado. Descarga ja paga foi mantida.");
        }
        void Cleanup()
        {
            ClearRoute();
            if(truckBlip!=null && truckBlip.Exists())truckBlip.Delete();truckBlip=null;
            if(trailerBlip!=null && trailerBlip.Exists())trailerBlip.Delete();trailerBlip=null;
            JobVehicles.RetireRig(truck,trailer);truck=trailer=null;station=null;
        }
        void End(string message){run?.Cancel();try{Cleanup();}finally{run=null;gate.Release(Name);menu.Visible=false;}Notify.Show(message);}
        public void OnKey(KeyEventArgs e){}
        public void Reload(){if(Active)End("Contrato LS GAS encerrado na troca de perfil.");menu.Visible=false;}
        public void Save(){}
        public void Dispose(){try{Cleanup();}finally{run=null;gate.Release(Name);menu.Visible=false;if(startBlip!=null && startBlip.Exists())startBlip.Delete();}}
    }
}
