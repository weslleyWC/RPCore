using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
using Newtonsoft.Json;
namespace RPCore
{
    public class FuelStation
    {
        public string Name;public float X,Y,Z,Radius=18;
        [JsonIgnore] public Vector3 Position=>new Vector3(X,Y,Z);
    }
    public class FuelSystem:ISystem
    {
        public string Name=>"fuel";
        readonly ObjectPool pool;readonly AchievementSystem ach;readonly ShiftGate gate;readonly Func<IEnumerable<EditorPoint>> editorPoints;
        readonly List<FuelStation> stations;readonly HashSet<int> exclusions=new HashSet<int>();readonly List<Blip> blips=new List<Blip>();
        readonly NativeMenu menu=new NativeMenu("Abastecimento RPCore","");readonly PersonalServiceMenu ui;
        TransportData cachedData;
        TransportData Data{get{if(cachedData==null || !ReferenceEquals(cachedData,Wallet.TransportReference))cachedData=Wallet.Transport;return cachedData;}}
        Vehicle vehicle;FuelTank tank;string activeKey;bool pumping,warned,exclusionsValid=true;string activityOwner;
        int last,nextPump;FuelOrder order;VehicleServiceIdentity identity;
        public FuelSystem(ObjectPool pool,AchievementSystem ach,ShiftGate gate,Func<IEnumerable<EditorPoint>> editorPoints=null)
        {
            this.pool=pool;this.ach=ach;this.gate=gate;this.editorPoints=editorPoints;pool.Add(menu);Visuals.Register(menu,"vehicle");
            ui=new PersonalServiceMenu(menu,ex=>{StopPump("Operacao interrompida");Log.Error("fuel menu",ex);Notify.Show("Abastecimento nao concluido; confira RPCore.log.");});
            stations=(Store.LoadDef("Fuel/Stations.json",DefaultStations)??new List<FuelStation>()).Where(s=>s!=null && s.Radius>0 && s.Radius<=50 && new[]{s.X,s.Y,s.Z,s.Radius}.All(n=>!float.IsNaN(n) && !float.IsInfinity(n))).ToList();
            string excluded=Path.Combine(Cfg.SaveDir,"ExcludedGasList.json");if(!File.Exists(excluded))excluded=Path.Combine(Cfg.Dir,"Legacy","SaveData","ExcludedGasList.json");
            try{if(File.Exists(excluded))foreach(string model in JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(excluded))??new List<string>())if(!string.IsNullOrWhiteSpace(model))exclusions.Add(new Model(model).Hash);}
            catch(Exception ex){exclusionsValid=false;Log.Error("fuel exclusions retained; automatic fuel suspended",ex);}
            foreach(var station in stations)blips.Add(MapBlips.Create(station.Position,"RPCore Fuel: "+station.Name,BlipSprite.JerryCan,BlipColor.Yellow,.65f));Reload();
        }
        public static List<FuelStation> DefaultStations()
        {
            float[][] points = {
                new[] { 49.4f, 2778.8f, 58.0f }, new[] { 263.9f, 2606.5f, 45.0f }, new[] { 1039.9f, 2671.1f, 39.6f },
                new[] { 1207.3f, 2660.2f, 37.9f }, new[] { 2539.7f, 2594.2f, 37.9f }, new[] { 2679.8f, 3263.9f, 55.2f },
                new[] { 2005.1f, 3773.9f, 32.4f }, new[] { 1687.2f, 4929.9f, 42.1f }, new[] { 1701.3f, 6416.0f, 32.8f },
                new[] { 179.8f, 6602.8f, 31.9f }, new[] { -94.5f, 6419.1f, 31.5f }, new[] { -2554.9f, 2334.4f, 33.1f },
                new[] { -1800.4f, 803.6f, 138.7f }, new[] { -1437.6f, -276.7f, 46.2f }, new[] { -2096.2f, -320.3f, 13.2f },
                new[] { -724.6f, -935.2f, 19.2f }, new[] { -526.0f, -1211.0f, 18.2f }, new[] { -70.2f, -1761.8f, 29.5f },
                new[] { 265.6f, -1261.3f, 29.3f }, new[] { 819.7f, -1028.8f, 26.4f }, new[] { 1208.9f, -1402.6f, 35.2f },
                new[] { 1181.4f, -330.8f, 69.3f }, new[] { 620.8f, 269.0f, 103.1f }, new[] { 2581.3f, 362.0f, 108.5f },
                new[] { 1785.6f, 3330.4f, 41.4f }, new[] { -319.3f, -1471.7f, 30.5f }
            };
            return points.Select((p, i) => new FuelStation { Name = "RPCore Fuel " + (i + 1), X = p[0], Y = p[1], Z = p[2] }).ToList();
        }

        public IEnumerable<GpsDestination> GpsPoints=>stations.Select(s=>new GpsDestination{Name=s.Name,Kind="Fuel",X=s.X,Y=s.Y,Z=s.Z});
        bool Supported(Vehicle v)=>exclusionsValid && v!=null && v.Exists() && !v.IsDead && (v.Model.IsCar || v.Model.IsBike || v.Model.IsQuadBike) && !v.Model.IsBicycle && !v.Model.IsElectricVehicle && !exclusions.Contains(v.Model.Hash);
        string Key(Vehicle v)=>VehicleIdentity.Key(v.Model.Hash,Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT,v));
        void Attach(Vehicle v)
        {
            string key=Key(v);if(key==null)throw new InvalidOperationException("Vehicle requires a plate.");
            var data=Data;
            if(!data.Tanks.TryGetValue(key,out var saved)){
                float capacity=v.PetrolTankVolume;if(float.IsNaN(capacity) || float.IsInfinity(capacity) || capacity<=0 || capacity>10000)capacity=60;
                float native=v.FuelLevel;if(float.IsNaN(native) || float.IsInfinity(native))native=capacity;
                data.Tanks[key]=saved=new FuelTank{Capacity=capacity,Liters=Math.Max(0,Math.Min(capacity,native))};
            }
            saved.Validate();if(vehicle==v && activeKey==key && ReferenceEquals(tank,saved))return;
            if(vehicle!=v || activeKey!=key)warned=false;
            vehicle=v;activeKey=key;tank=saved;v.FuelLevel=tank.Liters;
        }
        public bool CanStart(Vehicle v)
        {if(!Cfg.EnableFuel || Game.IsMissionActive || !Supported(v))return true;string key=Key(v);return key==null || !Data.Tanks.TryGetValue(key,out var value) || value.Liters>.001f;}
        public FuelTank ViewTank(Vehicle v)
        {string key=Supported(v)?Key(v):null;return Cfg.EnableFuel && key!=null && Data.Tanks.TryGetValue(key,out var value)?value:null;}
        bool Safe=>Cfg.EnableFuel && !Game.IsPaused && !Game.IsMissionActive && !InterfaceOptions.ModalOpen && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && Game.Player.CanControlCharacter && Profile.Detect()==Profile.Id && Game.Player.Character.IsAlive && Game.Player.Character.IsHuman && !Game.Player.Character.IsSwimming && !Game.Player.Character.IsClimbing && !Game.Player.Character.IsFalling && !Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE,Game.Player.Character) && !Game.Player.Character.IsRagdoll && !Game.Player.Character.IsInCombat && Game.Player.Wanted.WantedLevel==0;
        bool AtStation(Vector3 pos)=>stations.Any(s=>pos.DistanceTo(s.Position)<s.Radius) || (editorPoints?.Invoke()??Enumerable.Empty<EditorPoint>()).Any(p=>p.Kind==EditorPointKind.Fuel && pos.DistanceTo(new Vector3(p.Pose.X,p.Pose.Y,p.Pose.Z))<4.5f);
        bool Bound()
        {
            if(identity==null || !Supported(vehicle))return false;var p=Game.Player.Character;
            return identity.Matches(Profile.Id,p.Handle,p.Model.Hash,p.MemoryAddress.ToInt64(),vehicle.Handle,vehicle.Model.Hash,vehicle.MemoryAddress.ToInt64(),Key(vehicle));
        }
        bool Accessible()
        {
            if(!Safe || !Bound() || !Data.Tanks.TryGetValue(activeKey,out var canonical) || !ReferenceEquals(tank,canonical) || vehicle.Speed>=.5f || vehicle.IsEngineRunning || !AtStation(vehicle.Position) || Game.Player.Character.Position.DistanceTo(vehicle.Position)>5)return false;
            var p=Game.Player.Character;
            if(p.IsInVehicle() && (p.CurrentVehicle!=vehicle || vehicle.Driver!=p))return false;
            if(vehicle.Driver!=null && vehicle.Driver.Exists() && vehicle.Driver!=p)return false;
            return gate.Owner==activityOwner && FuelOrder.ActivityAllows(activityOwner);
        }
        void Row(string title,string description,Action action,bool enabled=true)=>ui.Add(title,description,action,enabled);
        void Summary()
        {
            if(!Accessible()){StopPump("Acesso encerrado");return;}ui.Begin("summary");menu.Name="Posto / "+Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT,vehicle).Trim();
            Row(tank.Liters.ToString("0.00")+" / "+tank.Capacity.ToString("0.##")+" L","Tanque salvo por modelo/placa. Motor desligado.",()=>{},false);
            Row("Preco: $"+Cfg.FuelPrice+"/L / carteira: $"+Wallet.Cash.ToString("N0"),"Cobrado somente pelo combustivel entregue, com pagamento pela carteira.",()=>{},false);
            Row("Completar o que couber no saldo","Limita ao espaco livre e ao dinheiro disponivel.",()=>Confirm(tank.Capacity));
            Row("Informar litros","Exemplo:10,5. Ate duas casas decimais.",()=>Input(false));
            Row("Informar valor maximo","Valor inteiro; o pedido nao ultrapassa o limite escolhido.",()=>Input(true));
            Row("Fechar","Nenhuma nova cobranca.",()=>StopPump("Fechado"));menu.Visible=true;
        }
        void Input(bool budget)
        {
            if(!Accessible())return;string text=Game.GetUserInput(WindowTitle.EnterMessage60,budget?"100":"10",16);if(!Accessible())return;
            if(budget){if(PhoneViewRules.Amount(text,out int amount))Confirm((float)Math.Min(10000m,(decimal)amount/Cfg.FuelPrice));else if(!string.IsNullOrWhiteSpace(text))Notify.Show("Informe um valor inteiro positivo.");}
            else if(FuelOrder.ParseLiters(text,out float liters))Confirm(liters);else if(!string.IsNullOrWhiteSpace(text))Notify.Show("Informe litros positivos com ate duas casas, ex:10,5.");
        }
        void Confirm(float requested)
        {
            if(!Accessible())return;int cash=Wallet.Cash,price=Cfg.FuelPrice;float before=tank.Liters;
            var quote=FuelTank.Quote(tank.Capacity-before,requested,cash,price);if(quote.Liters<=0){Notify.Show("Tanque cheio ou saldo insuficiente.");return;}
            ui.Begin("confirm",Summary,false);menu.Name="Abastecer "+quote.Liters.ToString("0.00")+" L por ate $"+quote.Cost+"?";
            Row("Cancelar / voltar","Mantem o tanque e o saldo.",Summary);
            Row("Confirmar abastecimento","Motor deve continuar desligado. "+Cfg.FuelKey+" ou voltar interrompe, mantendo apenas litros ja pagos.",()=>{
                if(!Accessible() || Wallet.Cash!=cash || Cfg.FuelPrice!=price || Math.Abs(tank.Liters-before)>.001f){Notify.Show("Saldo/tanque/contexto mudou. Confira o pedido novamente.");Summary();return;}
                if(gate.Owner!=activityOwner || !FuelOrder.ActivityAllows(activityOwner)){Notify.Show("A atividade atual mudou; abra o posto novamente.");return;}
                order=new FuelOrder(quote.Liters,price);pumping=true;nextPump=Game.GameTime;
                ui.Begin("pumping");menu.Name="Abastecendo / $"+price+" por L";Row("Parar abastecimento","Cobrado apenas pelo que foi entregue.",()=>StopPump("Interrompido"));menu.Visible=true;Sfx.Loop("fuel");
            });menu.Visible=true;
        }
        void Pump()
        {
            // Always use the current canonical tank; another saved vehicle transaction may replace Transport.
            var data=Data;if(!data.Tanks.TryGetValue(activeKey,out var current) || !ReferenceEquals(current,tank)){StopPump("Dados do tanque mudaram");return;}
            try{
                if(!Wallet.DeliverFuel(order,tank,Cfg.PumpLitersPerSecond)){StopPump("Limite de saldo ou capacidade");return;}
            }catch(Exception ex){StopPump("Falha ao gravar");Log.Error("fuel payment",ex);Notify.Show("Falha na transacao. Confira o log antes de tentar novamente.");return;}
            vehicle.FuelLevel=tank.Liters;
            if(order.Delivered>0 && !credited){credited=true;try{ach.Bump("gas_filled");}catch(Exception ex){Log.Error("fuel achievement",ex);}}
            if(order.Finished || tank.Capacity-tank.Liters<.01f)StopPump("Pedido concluido");
        }
        bool credited;
        void StopPump(string reason)
        {
            bool was=pumping;var finished=order;pumping=false;
            try{try{Sfx.Stop("fuel");}catch(Exception ex){Log.Error("fuel sound stop",ex);}ui.Close();}
            finally{order=null;identity=null;credited=false;activityOwner=null;}
            if(was && finished!=null)Notify.Show(reason+": "+finished.Delivered.ToString("0.00")+" L / $"+finished.Paid);
        }
        public void Tick()
        {
            int now=Game.GameTime;float seconds=Math.Min(1,Math.Max(0,unchecked(now-last))/1000f);last=now;
            if(menu.Visible || pumping){
                if(!menu.Visible || !Accessible()){StopPump("Acesso encerrado");return;}
                ui.Tick(true);if(pumping){if(unchecked(now-nextPump)>=0){nextPump=unchecked(now+1000);Pump();}if(pumping)Draw();}return;
            }
            if(!Cfg.EnableFuel || Game.IsPaused || Game.IsMissionActive || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) || !Game.Player.CanControlCharacter || Game.Player.Character.IsDead || Profile.Detect()!=Profile.Id)return;
            var p=Game.Player.Character;var v=p.IsInVehicle()?p.CurrentVehicle:null;if(!Supported(v) || v.Driver!=p)return;
            Attach(v);if(v.IsEngineRunning)tank.Drive(seconds,v.Speed,Cfg.FuelPer100Km,Cfg.IdleFuelPerHour);v.FuelLevel=tank.Liters;
            if(tank.Liters<=.001f){if(v.IsEngineRunning)Function.Call(Hash.SET_VEHICLE_ENGINE_ON,v,false,true,false);Game.DisableControlThisFrame(GTA.Control.VehicleAccelerate);}
            if(tank.Liters/tank.Capacity>=.1f)warned=false;
            if(!warned && tank.Liters/tank.Capacity<.1f){warned=true;Notify.Show("Combustivel baixo. Posto / motor desligado / "+Cfg.FuelKey+".");}
            if(v.Speed<.5f && AtStation(p.Position) && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen && !Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED,0,(int)GTA.Control.VehicleExit))Prompt.Help("Motor desligado / "+Cfg.FuelKey+": abastecer / $"+Cfg.FuelPrice+" por L");Draw();
        }
        void Draw()
        {
            if(tank==null)return;
            if(pumping){float x=Visuals.Width/2;Visuals.Label("Abastecendo "+order.Delivered.ToString("0.00")+" / "+order.Requested.ToString("0.00")+" L / $"+order.Paid+" / "+Cfg.FuelKey+": parar",x,1080-Visuals.InsetY-215*Visuals.Scale,.28f,Color.White,GTA.UI.Font.ChaletLondon,GTA.UI.Alignment.Center);}
            if(Visuals.VehicleReady)return;
            new GTA.UI.TextElement("COMBUSTIVEL "+tank.Liters.ToString("0.00")+"/"+tank.Capacity.ToString("0.##")+" L / "+tank.DistanceKm.ToString("0.0")+" km",new PointF(GTA.UI.Screen.ScaledWidth-390,GTA.UI.Screen.Height-125),.32f,tank.Liters/tank.Capacity<.1f?Color.OrangeRed:Color.White).Draw();
        }
        public void OnKey(KeyEventArgs e)
        {
            if(e.KeyCode!=Cfg.FuelKey || e.Modifiers!=Keys.None || !Cfg.EnableFuel)return;
            if(pumping || menu.Visible){StopPump("Interrompido");return;}
            if(!Safe || pool.AreAnyVisible || !FuelOrder.ActivityAllows(gate.Owner))return;
            if(!exclusionsValid){Notify.Show("Confira ExcludedGasList.json; arquivo original preservado.");return;}
            var p=Game.Player.Character;var v=p.IsInVehicle()?p.CurrentVehicle:p.LastVehicle;
            if(!Supported(v) || p.Position.DistanceTo(v.Position)>5)v=World.GetNearbyVehicles(p,5).Where(Supported).OrderBy(c=>p.Position.DistanceTo(c.Position)).FirstOrDefault();
            if(!Supported(v) || Key(v)==null){Notify.Show("Use um veiculo a combustao com placa, parado no posto.");return;}
            Attach(v);activityOwner=gate.Owner;identity=new VehicleServiceIdentity(Profile.Id,p.Handle,p.Model.Hash,p.MemoryAddress.ToInt64(),v.Handle,v.Model.Hash,v.MemoryAddress.ToInt64(),activeKey);
            if(!Accessible()){StopPump("Indisponivel");Notify.Show("Pare no posto, desligue o motor e fique ao lado do carro ou no banco do motorista.");return;}Summary();
        }
        public void Reload(){StopPump("Troca de perfil");vehicle=null;tank=null;activeKey=null;identity=null;cachedData=null;last=Game.GameTime;}
        public void Save()=>Wallet.Save();
        public void Dispose(){StopPump("Sessao encerrada");foreach(var b in blips)if(b.Exists())b.Delete();blips.Clear();}
    }
}
