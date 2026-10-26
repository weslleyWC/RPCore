using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
namespace RPCore
{
    public sealed class TrafficSystem : ISystem
    {
        public string Name=>"traffic";const string Decor="RPCore_TrafficOwner";
        sealed class Retired{public Vehicle Car;public Ped Officer;public int Token,Last;}
        readonly TrafficWitnessMeter witnessMeter=new TrafficWitnessMeter();readonly List<Retired> retired=new List<Retired>();readonly ObjectPool pool;readonly ShiftGate gate;readonly Func<bool> belt,gangFree;readonly Action custody;readonly TrafficOptions options;readonly NativeMenu menu;
        Vehicle cruiser,vehicle;Ped officer,actor;Model carModel,pedModel;Blip blip;string profile,ticketId,recovered;TrafficTicket detected;int token,requested,last,elapsed,scanAt,taskAt,stopped,inspection,cooldown,sampleElapsed;bool preparing,observer,walking,eligibleBefore;
        bool Active=>preparing || officer!=null;bool Driver=>actor!=null && actor.Exists() && actor==Game.Player.Character && vehicle!=null && vehicle.Exists() && actor.CurrentVehicle==vehicle && vehicle.Driver==actor;
        static bool Tagged(Entity e,int t)=>e!=null && e.Exists() && e.Handle!=Game.Player.Character.Handle && Function.Call<bool>(Hash.DECOR_EXIST_ON,e,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,e,Decor)==t;
        bool Mark(Entity e){if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);return Function.Call<bool>(Hash.DECOR_SET_INT,e,Decor,token);}
        static bool Road(Vehicle v)=>v!=null && v.Exists() && (v.Model.IsCar || v.Model.IsBike) && !v.Model.IsBicycle && !v.Model.IsLawEnforcementVehicle && !JobVehicles.Contains(v);
        bool Safe=>Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && Game.Player.Wanted.WantedLevel==0;
        bool StartSafe=>Wallet.Traffic?.Enabled==true && Wallet.Traffic.Valid && Wallet.Traffic.Pending==null && Safe && !Game.IsPaused && !InterfaceOptions.ModalOpen && gate.Owner==null && Wallet.GangWarEconomyFree && gangFree() && retired.Count==0 && Game.Player.Character.IsInVehicle() && Road(Game.Player.Character.CurrentVehicle) && Game.Player.Character.CurrentVehicle.Driver==Game.Player.Character && Function.Call<int>(Hash.GET_INTERIOR_FROM_ENTITY,Game.Player.Character)==0;
        public TrafficSystem(ObjectPool pool,ShiftGate gate,Func<bool> belt,Func<bool> gangFree=null,Action custody=null)
        {
            this.custody=custody;this.pool=pool;this.gate=gate;this.belt=belt;this.gangFree=gangFree??(()=>true);options=Store.LoadDef("Police/Traffic.json",TrafficOptions.Defaults);if(options?.Valid!=true){options=TrafficOptions.Defaults();Log.Write("Invalid traffic options retained; embedded defaults used.");}
            menu=new NativeMenu("Policia / transito","FISCALIZACAO / ABORDAGENS / MULTAS");pool.Add(menu);Visuals.Register(menu,"core");last=Game.GameTime;
        }
        void Add(string title,string description,Action action,bool enabled=true){var row=new NativeItem(title,description){Enabled=enabled};row.Activated+=(s,e)=>{try{action();}catch(Exception ex){Close("interrupted");Log.Error("traffic menu",ex);Notify.Show("Operacao nao concluida; saldo e registros gravados conservados.");}};menu.Add(row);}
        public void OpenMenu()
        {
            pool.HideAll();menu.Clear();var book=Wallet.Traffic;if(book!=null && !book.Valid){Add("Dados de transito protegidos","Confira Wallet.json/backup. Nenhuma multa ou saldo sera substituido.",()=>{});if(custody!=null)Add("Custodia / prisao / fianca","Sistema independente das autuacoes protegidas.",custody);menu.Visible=true;return;}
            if(custody!=null)Add("Custodia / prisao / fianca","Rendicao voluntaria a policial na perseguicao nativa, fichamento, cumprimento de pena e liberacao.",custody);
            Add("Fiscalizacao: "+(book?.Enabled==true?"LIGADA":"DESLIGADA"),"Opcional. Observa infracoes reais vistas por policial em viatura proxima; resposta usa somente agente/viatura proprios. Sem alterar policia nativa. Comeca desligada em saves antigos.",()=>{bool enabled=Wallet.Traffic?.Enabled!=true;if(Wallet.TrafficConfigure(enabled,out _)){if(!enabled)Close("interrupted");OpenMenu();}});
            Add("Solicitar patrulha observadora (teste)","Dirija seu carro/moto fora de missao/trabalho/procurado. Patrulha segue por ate"+options.ObserverSeconds+"s e so autua se observar infracao real. Usar cinto/limites evita autuacao. Sem multa inventada pelo botao.",()=>{if(Begin(null))menu.Visible=false;else Notify.Show("Confira fiscalizacao ligada, motorista, carro/moto pessoal, area externa e ausencia de atividade/pendencias.");},!Active && book?.Enabled==true);
            if(Active)Add("Encerrar patrulha / abordagem","Recolhe apenas recursos proprios. Infracao ja registrada continua consultavel e nao e perdoada por encerrar/desligar.",()=>{Close("interrupted");OpenMenu();});
            Add("Documentos C / M1 / A",(Wallet.Dmv.Licensed?"C: emitida":"C: nao emitida")+" / "+(Wallet.DmvM1.Licensed?"M1: emitida":"M1: nao emitida")+" / "+(Wallet.DmvA.Licensed?"A: emitida":"A: nao emitida")+". Consulta do DMV existente; nenhuma nova exigencia para empregos antigos.",()=>{});
            Add("Autuacoes "+(book?.Issued??0)+" / pagas "+(book?.Paid??0)+" / banco $"+Wallet.Bank,"Total pago $"+(book?.Collected??0)+". Multas nao debitam automaticamente; pagamento exato pelo banco, sem juros offline ou desconto parcial de dinheiro.",()=>{});
            foreach(var t in (book?.Tickets??new List<TrafficTicket>()).AsEnumerable().Reverse().Where(t=>!t.Paid).Concat((book?.Tickets??new List<TrafficTicket>()).AsEnumerable().Reverse().Where(t=>t.Paid).Take(20))){var invoice=t;Add((t.Paid?"PAGA: ":"PENDENTE: ")+t.Plate+" / $"+t.Total,t.Street+" / "+Outcome(t.Outcome)+". "+string.Join("; ",t.Lines.Select(l=>TrafficRules.Label(l.Code)+" $"+l.Amount)),()=>Invoice(invoice.Id));}
            Add("Fechar","Voltar ao jogo.",()=>menu.Visible=false);menu.Visible=true;
        }
        static string Outcome(string s)=>s=="served"?"abordagem concluida":s=="evaded"?"nao parou / registro por placa":s=="interrupted"?"registro apos interrupcao":"abordagem em andamento";
        void Invoice(string id)
        {
            var t=Wallet.Traffic?.Tickets.FirstOrDefault(i=>i.Id==id);if(t==null)return;string payerProfile=Profile.Id;pool.HideAll();menu.Clear();Add("Placa "+t.Plate+" / $"+t.Total,t.Street+" / "+Outcome(t.Outcome),()=>{});
            foreach(var line in t.Lines){var l=line;Add(TrafficRules.Label(l.Code)+" / $"+l.Amount,l.Code==TrafficOffence.Speeding?"Excesso observado "+l.Value.ToString("0.0")+"mph; limite "+t.LimitMph+"mph ("+(t.LimitMph*1.609344).ToString("0")+"km/h).":"Infracao capturada no momento da observacao.",()=>{});}
            Add(t.Paid?"Ja paga":"Pagar $"+t.Total+" pelo banco","CONFIRMA AO SELECIONAR. Cobra exatamente uma vez. Saldo insuficiente conserva a divida; dinheiro em maos nao e debitado. Aguarde encerrar a abordagem atual.",()=>{if(payerProfile!=Profile.Id || !Safe || Game.IsPaused || InterfaceOptions.ModalOpen || gate.Owner!=null){Notify.Show("Pague sem abordagem/outra atividade, procurado ou missao.");return;}if(Wallet.TrafficPay(id,out bool changed))Notify.Show(changed?"Multa paga pelo banco.":"Pagamento ja registrado; sem debito repetido.");else Notify.Show("Confira saldo bancario e estado da autuacao.");Invoice(id);},!t.Paid && t.Outcome!="open");
            Add("Voltar","Lista de autuacoes.",OpenMenu);menu.Visible=true;
        }
        TrafficFrame Frame(Vehicle v)
        {
            bool burst=false;foreach(int i in new[]{0,1,2,3,4,5,45,47})if(Function.Call<bool>(Hash.IS_VEHICLE_TYRE_BURST,v,i,false)){burst=true;break;}
            int hour=GTA.Chrono.GameClock.Hour;return new TrafficFrame{Car=v.Model.IsCar,Bike=v.Model.IsBike,Belt=belt(),Helmet=Game.Player.Character.IsWearingHelmet,Lights=v.AreLightsOn,Night=hour>=19 || hour<=6,Burnout=Function.Call<bool>(Hash.IS_VEHICLE_IN_BURNOUT,v),Burst=burst,Speed=v.Speed,Engine=v.EngineHealth,Street=World.GetStreetName(v.Position)};
        }
        bool Witness(Ped p)=>p!=null && p.Exists() && !p.IsDead && p.Position.DistanceTo(Game.Player.Character.Position)<=options.WitnessRadius && p.HasClearLineOfSightTo(Game.Player.Character,IntersectFlags.Everything);
        TrafficTicket Observe(bool own)
        {
            var p=Game.Player.Character;var v=p.CurrentVehicle;if(v==null || v.Driver!=p || !Road(v))return null;
            bool seen=own?Witness(officer):World.GetNearbyVehicles(p.Position,options.WitnessRadius).Any(c=>c!=v && c.Exists() && c.Model.IsLawEnforcementVehicle && Witness(c.Driver));if(!seen)return null;
            var frame=Frame(v);var lines=TrafficRules.Evaluate(frame,options);if(lines.Count==0)return null;
            string plate=Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT,v)?.Trim();if(string.IsNullOrWhiteSpace(plate))return null;
            return new TrafficTicket{Id=Guid.NewGuid().ToString("N"),Model=v.Model.Hash,Plate=plate,Street=string.IsNullOrWhiteSpace(frame.Street)?"Rua sem nome":frame.Street,LimitMph=TrafficRules.Limit(options,frame.Street),Lines=lines};
        }
        bool Begin(TrafficTicket evidence)
        {
            if(Active || !StartSafe)return false;carModel=new Model(options.CruiserModel);pedModel=new Model(options.OfficerModel);if(!carModel.IsInCdImage || !carModel.IsCar || !pedModel.IsInCdImage || !pedModel.IsPed || !gate.Acquire(Name))return false;
            profile=Profile.Id;actor=Game.Player.Character;vehicle=actor.CurrentVehicle;detected=evidence;observer=evidence==null;token=new Random().Next(1,int.MaxValue);carModel.Request();pedModel.Request();preparing=true;requested=last=Game.GameTime;elapsed=stopped=inspection=sampleElapsed=0;scanAt=0;witnessMeter.Reset();eligibleBefore=false;pool.HideAll();return true;
        }
        void Spawn()
        {
            Vector3? spot=null;float heading=0;for(int i=0;i<24;i++){
                double angle=i*2.4;var q=actor.Position+new Vector3((float)Math.Cos(angle)*85,(float)Math.Sin(angle)*85,0);var node=new OutputArgument();var h=new OutputArgument();if(!Function.Call<bool>(Hash.GET_CLOSEST_VEHICLE_NODE_WITH_HEADING,q.X,q.Y,q.Z,node,h,1,3f,0))continue;var point=node.GetResult<Vector3>();float d=point.DistanceTo(actor.Position),z;
                if(d<55 || d>130 || Math.Abs(point.Z-actor.Position.Z)>6 || !JobActors.Free(point,5) || !World.GetGroundHeight(point+new Vector3(0,0,3),out z,GetGroundHeightMode.Normal) || Math.Abs(z-point.Z)>2)continue;spot=new Vector3(point.X,point.Y,z+.1f);heading=h.GetResult<float>();break;
            }if(!spot.HasValue){Close("interrupted");Notify.Show("Sem vaga livre para resposta policial; nenhum veiculo ambiente foi removido.");return;}
            cruiser=World.CreateVehicle(carModel,spot.Value,heading);if(cruiser==null || !cruiser.Exists())throw new InvalidOperationException("Traffic cruiser unavailable.");if(!Mark(cruiser)){cruiser.MarkAsNoLongerNeeded();cruiser=null;throw new InvalidOperationException("Traffic cruiser ownership unavailable.");}cruiser.IsPersistent=true;Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY,cruiser);cruiser.IsEngineRunning=true;JobVehicles.Reserve(cruiser);
            officer=World.CreatePed(pedModel,spot.Value+new Vector3(0,0,2));if(officer==null || !officer.Exists())throw new InvalidOperationException("Traffic officer unavailable.");if(!officer.IsHuman || !Mark(officer)){officer.Delete();officer=null;throw new InvalidOperationException("Traffic officer ownership unavailable.");}
            officer.IsPersistent=true;officer.BlockPermanentEvents=true;officer.RelationshipGroup=new RelationshipGroup((int)RelationshipGroupHash.CivilianMale);officer.Weapons.RemoveAll();Function.Call(Hash.SET_PED_KEEP_TASK,officer,true);Function.Call(Hash.SET_PED_MONEY,officer,0);officer.SetIntoVehicle(cruiser,VehicleSeat.Driver);if(cruiser.Driver!=officer)throw new InvalidOperationException("Traffic officer seat unavailable.");
            carModel.MarkAsNoLongerNeeded();pedModel.MarkAsNoLongerNeeded();preparing=false;last=Game.GameTime;elapsed=0;taskAt=0;blip=cruiser.AddBlip();blip.Color=BlipColor.Blue;blip.Name="RPCore: fiscalizacao de transito";
            if(detected!=null){if(!Register(detected)){Close("interrupted");return;}}else Notify.Show("Patrulha observadora pronta. Dirija normalmente; sem infracao observada, sem multa. END encerra.");
        }
        bool Register(TrafficTicket t)
        {
            if(!Wallet.TrafficBegin(t,out bool saved) || !saved)return false;ticketId=t.Id;observer=false;elapsed=stopped=inspection=0;cruiser.IsSirenActive=true;taskAt=0;Notify.Show("FISCALIZACAO: encoste com seguranca e permaneca ao volante. Autuacao registrada por $"+t.Total+"; sem debito automatico.");return true;
        }
        void Sample(int delta)
        {
            sampleElapsed+=delta;if(scanAt!=0 && unchecked(Game.GameTime-scanAt)<0)return;int sampled=Math.Min(1000,sampleElapsed);sampleElapsed=0;scanAt=Game.GameTime+500;var t=Observe(Active);string key=t==null?null:t.Model+"|"+t.Plate+"|"+t.Street+"|"+string.Join(",",t.Lines.Select(l=>l.Code+":"+l.Amount));
            if(!witnessMeter.Observe(key,sampled,options.SampleMilliseconds) || t==null)return;witnessMeter.Reset();if(Active){if(!Register(t))Close("interrupted");}else Begin(t);
        }
        public void Tick()
        {
            Drain();if(!Active){
                if(Wallet.Traffic?.Valid==true && Wallet.Traffic.Pending!=null && recovered!=Profile.Id+"|"+Wallet.Traffic.Pending){recovered=Profile.Id+"|"+Wallet.Traffic.Pending;Wallet.TrafficFinish(Wallet.Traffic.Pending,"interrupted",out _);}
                bool ready=StartSafe && !pool.AreAnyVisible;int delta=GangWarRules.Delta(last,Game.GameTime,ready && eligibleBefore);last=Game.GameTime;eligibleBefore=ready;if(cooldown>0){cooldown=Math.Max(0,cooldown-delta);return;}if(ready)Sample(delta);else{sampleElapsed=0;witnessMeter.Reset();}return;
            }
            bool input=!Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;int dt=GangWarRules.Delta(last,Game.GameTime,input && eligibleBefore);last=Game.GameTime;eligibleBefore=input;
            if(profile!=Profile.Id || !Safe || !Driver || Wallet.Traffic?.Valid!=true || Wallet.Traffic.Enabled!=true || gate.Owner!=Name || !Wallet.GangWarEconomyFree || !gangFree() || Function.Call<int>(Hash.GET_INTERIOR_FROM_ENTITY,actor)!=0){Close("interrupted");return;}if(!input)return;
            if(preparing){if(unchecked(Game.GameTime-requested)>10000){Close("interrupted");return;}if(carModel.IsLoaded && pedModel.IsLoaded)Spawn();return;}
            if(!Tagged(officer,token) || !Tagged(cruiser,token) || officer.IsDead || cruiser.IsDead || Connected(cruiser) || cruiser.Occupants.Any(p=>p!=officer)){Close("interrupted");return;}
            elapsed+=dt;
            if(observer){if(elapsed>=options.ObserverSeconds*1000){Close("interrupted");Notify.Show("Observacao encerrada sem nova autuacao.");return;}Sample(dt);if(!Active)return;}
            else{
                if(Wallet.Traffic.Pending!=ticketId){Close("interrupted");return;}
                if(elapsed>=options.StopSeconds*1000 || actor.Position.DistanceTo(cruiser.Position)>300){Close("evaded");Notify.Show("Abordagem nao concluida; autuacao por placa disponivel no F11. Sem novas estrelas pelo RPCore.");return;}
                if(!walking){if(TrafficRules.Stopped(Driver,vehicle.Speed,cruiser.Speed,cruiser.Position.DistanceTo(vehicle.Position)))stopped+=dt;else stopped=0;if(stopped>=2000){walking=true;JobActors.Leave(officer,cruiser);taskAt=0;}}
                if(walking){if(officer.IsOnFoot && (taskAt==0 || unchecked(Game.GameTime-taskAt)>=0)){var point=vehicle.GetOffsetPosition(new Vector3(-1.6f,0,0));Function.Call(Hash.TASK_FOLLOW_NAV_MESH_TO_COORD,officer,point.X,point.Y,point.Z,1f,10000,.6f,0,actor.Heading);taskAt=Game.GameTime+4000;}
                    if(officer.IsOnFoot && TrafficRules.Inspect(Driver,!officer.IsDead,vehicle.Speed,officer.Position.DistanceTo(actor.Position)))inspection+=dt;else inspection=0;
                    if(inspection>=options.InspectionMilliseconds){Close("served");Notify.Show("Abordagem concluida. Consulte e pague a autuacao em F11 > Policia / transito. Banco nao foi debitado automaticamente.");return;}
                }
                GTA.UI.Screen.ShowSubtitle(walking?"POLICIA: permaneça parado ao volante para conferencia dos documentos.":"POLICIA: encoste e pare com seguranca; aguarde o agente. END encerra sem apagar a autuacao.",100);
            }
            if(!walking && (taskAt==0 || unchecked(Game.GameTime-taskAt)>=0)){taskAt=Game.GameTime+4000;if(cruiser.Driver==officer)Function.Call(Hash.TASK_VEHICLE_ESCORT,officer,cruiser,vehicle,-1,28f,786603,10f,8,5f);}
        }
        static bool Connected(Vehicle v){var trailer=new OutputArgument();return Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,v) || Function.Call<bool>(Hash.GET_VEHICLE_TRAILER_VEHICLE,v,trailer);}
        void Close(string outcome)
        {
            try{if(profile==Profile.Id && ticketId!=null && Wallet.Traffic?.Pending==ticketId)Wallet.TrafficFinish(ticketId,outcome,out _);}finally{
                if(Tagged(cruiser,token))cruiser.IsSirenActive=false;if(officer!=null || cruiser!=null)retired.Add(new Retired{Car=cruiser,Officer=officer,Token=token,Last=Game.GameTime-4000});officer=null;cruiser=null;actor=null;vehicle=null;if(carModel.Hash!=0)carModel.MarkAsNoLongerNeeded();if(pedModel.Hash!=0)pedModel.MarkAsNoLongerNeeded();carModel=pedModel=default(Model);if(blip!=null && blip.Exists())blip.Delete();blip=null;ticketId=profile=null;detected=null;observer=walking=preparing=false;sampleElapsed=0;witnessMeter.Reset();cooldown=options.CooldownSeconds*1000;gate.Release(Name);last=Game.GameTime;eligibleBefore=false;
            }
        }
        void Drain()
        {
            for(int i=retired.Count-1;i>=0;i--){var r=retired[i];bool p=Tagged(r.Officer,r.Token),v=Tagged(r.Car,r.Token);
                if(p && r.Officer.IsInVehicle()){if(r.Officer.IsDead && v && r.Car.Occupants.All(a=>a==r.Officer) && r.Car.Speed<.5f && !r.Car.IsInAir && !Connected(r.Car)){r.Officer.MarkAsNoLongerNeeded();r.Car.MarkAsNoLongerNeeded();JobVehicles.Release(r.Car);retired.RemoveAt(i);continue;}if(r.Officer.IsDead)r.Officer.MarkAsNoLongerNeeded();else if(v && r.Officer.IsInVehicle(r.Car) && r.Car.Speed<.5f && unchecked(Game.GameTime-r.Last)>=4000){JobActors.Leave(r.Officer,r.Car);r.Last=Game.GameTime;}else if(v && r.Car.Driver==r.Officer && r.Car.Occupants.Length==1 && unchecked(Game.GameTime-r.Last)>=4000){Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER,r.Officer,r.Car,10f,786603);r.Last=Game.GameTime;}
                    if(v && r.Car.Occupants.All(a=>a==r.Officer) && r.Car.Position.DistanceTo(Game.Player.Character.Position)>200 && !Connected(r.Car)){Function.Call(Hash.SET_PED_KEEP_TASK,r.Officer,false);r.Officer.MarkAsNoLongerNeeded();r.Car.MarkAsNoLongerNeeded();JobVehicles.Release(r.Car);retired.RemoveAt(i);}continue;
                }
                if(p)r.Officer.Delete();if(v && (r.Car.Occupants.Length!=0 || r.Car.Speed>=.5f || r.Car.IsInAir || Connected(r.Car)))continue;JobVehicles.Release(r.Car);if(v)r.Car.Delete();retired.RemoveAt(i);
            }
        }
        public void OnKey(KeyEventArgs e){if(e.KeyCode==Keys.End && Active){Close("interrupted");Notify.Show("Fiscalizacao encerrada; autuacoes ja registradas permanecem.");}}
        public void Reload(){menu.Visible=false;Close("interrupted");recovered=null;cooldown=0;}
        public void Save(){}
        public void Dispose(){menu.Visible=false;Close("interrupted");foreach(var r in retired){if(Tagged(r.Officer,r.Token)){Function.Call(Hash.SET_PED_KEEP_TASK,r.Officer,false);r.Officer.MarkAsNoLongerNeeded();}if(Tagged(r.Car,r.Token))r.Car.MarkAsNoLongerNeeded();JobVehicles.Release(r.Car);}retired.Clear();}
    }
}
