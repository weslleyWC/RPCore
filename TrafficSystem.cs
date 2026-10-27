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
        readonly Random bribeRandom=new Random();
        readonly TrafficWitnessMeter witnessMeter=new TrafficWitnessMeter();readonly List<Retired> retired=new List<Retired>();readonly ObjectPool pool;readonly ShiftGate gate;readonly Func<bool> belt,gangFree;readonly TrafficBehaviourMeter behaviour=new TrafficBehaviourMeter();readonly TrafficPursuitOptions pursuitOptions;TrafficPursuitOptions pursuitPlan;TrafficPursuitClock pursuitClock;readonly Action custody;readonly TrafficOptions options;readonly NativeMenu menu;
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
            pursuitOptions=Store.LoadDef("Police/TrafficPursuit.json",TrafficPursuitOptions.Defaults);if(pursuitOptions?.Valid!=true){pursuitOptions=TrafficPursuitOptions.Defaults();Log.Write("Invalid traffic pursuit options retained; defaults used.");}
            menu=new NativeMenu("Policia / transito","FISCALIZACAO / ABORDAGENS / MULTAS");pool.Add(menu);Visuals.Register(menu,"core");last=Game.GameTime;
        }
        void Add(string title,string description,Action action,bool enabled=true){var row=new NativeItem(title,description){Enabled=enabled};row.Activated+=(s,e)=>{try{action();}catch(Exception ex){Close("interrupted");Log.Error("traffic menu",ex);Notify.Show("Operacao nao concluida; saldo e registros gravados conservados.");}};menu.Add(row);}
        public void OpenMenu()
        {
            pool.HideAll();menu.Clear();string menuProfile=Profile.Id;var book=Wallet.Traffic;if(book!=null && !book.Valid){Add("Dados de transito protegidos","Confira Wallet.json/backup. Nenhuma multa ou saldo sera substituido.",()=>{});if(custody!=null)Add("Custodia / prisao / fianca","Sistema independente das autuacoes protegidas.",custody);menu.Visible=true;return;}
            Add("Suborno: "+(book?.BribesEnabled==true?"LIGADO":"DESLIGADO"),"Uma tentativa por abordagem presencial. Banco:1.5x multa se aceito; rejeicao conserva multa e pode gerar BOLO. Ate2estrelas somente com perseguicao/reforco ja habilitados. Desligado em saves antigos.",()=>{if(menuProfile!=Profile.Id)return;if(Wallet.PoliceResolutionConfigure(book?.BribesEnabled!=true,book?.BoloEnabled==true))OpenMenu();else Notify.Show("Encerre a abordagem antes de mudar regras.");},!Active);
            Add("BOLO por fuga: "+(book?.BoloEnabled==true?"LIGADO":"DESLIGADO"),"Registro persistente por modelo+placa ao fugir/rejeitar suborno. Nao acrescenta multa automatica nem altera seguro/policia nativa. Regularize apos resolver autuacoes de origem.",()=>{if(menuProfile!=Profile.Id)return;if(Wallet.PoliceResolutionConfigure(book?.BribesEnabled==true,book?.BoloEnabled!=true))OpenMenu();},!Active);
            if(CanBribe)Add("Oferecer suborno / $"+BribeQuote,"Tentativa unica ao agente proximo; banco exige1.5x multa. Resultado salvo antes de encerrar a abordagem.",OfferBribe);
            foreach(var entry in book?.Bolos??new List<TrafficBolo>()){var b=entry;Add((b.Active?"BOLO ATIVO: ":"BOLO REGULARIZADO: ")+b.Plate,b.Reason+" / modelo "+b.Model+". Regularizacao sem taxa extra; exige resolver autuacoes de origem.",()=>{if(menuProfile!=Profile.Id || Active || !Safe || gate.Owner!=null)return;if(Wallet.TrafficClearBolo(b.Model,b.Plate))Notify.Show("Registro regularizado; seguro e estrelas nativas preservados.");else Notify.Show("Resolva primeiro as autuacoes que originaram este BOLO.");OpenMenu();});}
            if(custody!=null)Add("Custodia / prisao / fianca","Rendicao voluntaria a policial na perseguicao nativa, fichamento, cumprimento de pena e liberacao.",custody);
            Add("Fiscalizacao: "+(book?.Enabled==true?"LIGADA":"DESLIGADA"),"Opcional. Observa infracoes reais vistas por policial em viatura proxima; resposta usa somente agente/viatura proprios. Sem assumir policiais do ambiente; reforco opcional separado. Comeca desligada em saves antigos.",()=>{bool enabled=Wallet.Traffic?.Enabled!=true;if(Wallet.TrafficConfigure(enabled,out _)){if(!enabled)Close("interrupted");OpenMenu();}});
            Add("Infracoes adicionais: "+(book?.AdvancedOffences==true?"LIGADAS":"DESLIGADAS"),"Celular nativo $100, distancia insegura $120, arma apontada $350 e buzina continua10s $125. Exigem testemunha/evidencia real. Celular F2 e menus continuam protegidos. Sem alterar multas antigas.",()=>{if(menuProfile!=Profile.Id)return;if(Wallet.TrafficPreferences(book?.AdvancedOffences!=true,book?.PursuitEnabled==true,out _))OpenMenu();else Notify.Show("Encerre a abordagem antes de mudar regras.");},!Active);
            Add("Perseguicao / reforco: "+(book?.PursuitEnabled==true?"LIGADOS":"DESLIGADOS"),"CONFIRMA estrelas NATIVAS por fuga. Patrulha propria persegue carro/pe; fuga ativa"+pursuitOptions.VehicleSeconds+"s/"+pursuitOptions.FootSeconds+"s ou"+pursuitOptions.EscapeDistance+"m solicita ate"+pursuitOptions.BackupStars+"estrelas, respeitando limite do jogo. Nao apaga procurado. Desligado em saves antigos; END/reload nao chama reforco.",()=>{if(menuProfile!=Profile.Id)return;if(Wallet.TrafficPreferences(book?.AdvancedOffences==true,book?.PursuitEnabled!=true,out _))OpenMenu();else Notify.Show("Encerre a abordagem antes de mudar regras.");},!Active);
            Add("Solicitar patrulha observadora (teste)","Dirija seu carro/moto fora de missao/trabalho/procurado. Patrulha segue por ate"+options.ObserverSeconds+"s e so autua se observar infracao real. Usar cinto/limites evita autuacao. Sem multa inventada pelo botao.",()=>{if(Begin(null))menu.Visible=false;else Notify.Show("Confira fiscalizacao ligada, motorista, carro/moto pessoal, area externa e ausencia de atividade/pendencias.");},!Active && book?.Enabled==true);
            if(Active)Add("Encerrar patrulha / abordagem","Recolhe apenas recursos proprios. Infracao ja registrada continua consultavel e nao e perdoada por encerrar/desligar.",()=>{Close("interrupted");OpenMenu();});
            Add("Documentos C / M1 / A",(Wallet.Dmv.Licensed?"C: emitida":"C: nao emitida")+" / "+(Wallet.DmvM1.Licensed?"M1: emitida":"M1: nao emitida")+" / "+(Wallet.DmvA.Licensed?"A: emitida":"A: nao emitida")+". Consulta do DMV existente; nenhuma nova exigencia para empregos antigos.",()=>{});
            Add("Autuacoes "+(book?.Issued??0)+" / pagas "+(book?.Paid??0)+" / banco $"+Wallet.Bank,"Total pago $"+(book?.Collected??0)+". Multas nao debitam automaticamente; pagamento exato pelo banco, sem juros offline ou desconto parcial de dinheiro.",()=>{});
            foreach(var t in (book?.Tickets??new List<TrafficTicket>()).AsEnumerable().Reverse().Where(t=>!t.Paid).Concat((book?.Tickets??new List<TrafficTicket>()).AsEnumerable().Reverse().Where(t=>t.Paid).Take(20))){var invoice=t;Add((t.Bribe?.Accepted==true?"ACORDO: ":t.Paid?"PAGA: ":"PENDENTE: ")+t.Plate+" / $"+t.Total,t.Street+" / "+Outcome(t.Outcome)+(t.BackupLevel>0?" / reforco solicitado: "+t.BackupLevel+" estrela(s)":"")+". "+string.Join("; ",t.Lines.Select(l=>TrafficRules.Label(l.Code)+" $"+l.Amount)),()=>Invoice(invoice.Id));}
            Add("Fechar","Voltar ao jogo.",()=>menu.Visible=false);menu.Visible=true;
        }
        static string Outcome(string s)=>s=="served"?"abordagem concluida":s=="evaded"?"nao parou / registro por placa":s=="interrupted"?"registro apos interrupcao":"abordagem em andamento";
        void Invoice(string id)
        {
            var t=Wallet.Traffic?.Tickets.FirstOrDefault(i=>i.Id==id);if(t==null)return;string payerProfile=Profile.Id;pool.HideAll();menu.Clear();Add("Placa "+t.Plate+" / $"+t.Total,t.Street+" / "+Outcome(t.Outcome),()=>{});
            foreach(var line in t.Lines){var l=line;Add(TrafficRules.Label(l.Code)+" / $"+l.Amount,l.Code==TrafficOffence.Speeding?"Excesso observado "+l.Value.ToString("0.0")+"mph; limite "+t.LimitMph+"mph ("+(t.LimitMph*1.609344).ToString("0")+"km/h).":"Infracao capturada no momento da observacao.",()=>{});}
            if(t.Bribe!=null)Add(t.Bribe.Accepted?"Suborno aceito / $"+t.Bribe.Price:"Tentativa de suborno recusada","Recibo persistente; nao ha nova tentativa nem pagamento duplicado.",()=>{});
            Add(PoliceResolutionRules.Settled(t)?"Ja resolvida":"Pagar $"+t.Total+" pelo banco","CONFIRMA AO SELECIONAR. Cobra exatamente uma vez. Saldo insuficiente conserva a divida; dinheiro em maos nao e debitado. Aguarde encerrar a abordagem atual.",()=>{if(payerProfile!=Profile.Id || !Safe || Game.IsPaused || InterfaceOptions.ModalOpen || gate.Owner!=null){Notify.Show("Pague sem abordagem/outra atividade, procurado ou missao.");return;}if(Wallet.TrafficPay(id,out bool changed))Notify.Show(changed?"Multa paga pelo banco.":"Pagamento ja registrado; sem debito repetido.");else Notify.Show("Confira saldo bancario e estado da autuacao.");Invoice(id);},!PoliceResolutionRules.Settled(t) && t.Outcome!="open");
            Add("Voltar","Lista de autuacoes.",OpenMenu);menu.Visible=true;
        }
        TrafficFrame Frame(Vehicle v)
        {
            bool burst=false;foreach(int i in new[]{0,1,2,3,4,5,45,47})if(Function.Call<bool>(Hash.IS_VEHICLE_TYRE_BURST,v,i,false)){burst=true;break;}
            bool tailgating=false;
            if(Wallet.Traffic?.AdvancedOffences==true){var hit=World.Raycast(v.Position,v.Position+v.ForwardVector*10f,IntersectFlags.Vehicles,v);var front=hit.HitEntity as Vehicle;if(hit.DidHit && front!=null && front.Exists()){var a=v.ForwardVector;var b=front.ForwardVector;tailgating=TrafficPursuitRules.Tailgate(v.Speed,v.Position.DistanceTo(hit.HitPosition),a.X*b.X+a.Y*b.Y+a.Z*b.Z,front.Position.Z-v.Position.Z);}}
            int hour=GTA.Chrono.GameClock.Hour;return new TrafficFrame{Car=v.Model.IsCar,Bike=v.Model.IsBike,Belt=belt(),Helmet=Game.Player.Character.IsWearingHelmet,Lights=v.AreLightsOn,Night=hour>=19 || hour<=6,Burnout=Function.Call<bool>(Hash.IS_VEHICLE_IN_BURNOUT,v),Burst=burst,Speed=v.Speed,Engine=v.EngineHealth,Street=World.GetStreetName(v.Position),Tailgating=tailgating,MobilePhone=Function.Call<bool>(Hash.IS_PED_RUNNING_MOBILE_PHONE_TASK,Game.Player.Character),AimingWeapon=Function.Call<bool>(Hash.IS_PLAYER_FREE_AIMING,Game.Player) && Game.Player.Character.Weapons.Current.Hash!=WeaponHash.Unarmed,HornMilliseconds=behaviour.HornMilliseconds};
        }
        bool Witness(Ped p)=>p!=null && p.Exists() && !p.IsDead && p.Position.DistanceTo(Game.Player.Character.Position)<=options.WitnessRadius && p.HasClearLineOfSightTo(Game.Player.Character,IntersectFlags.Everything);
        TrafficTicket Observe(bool own)
        {
            var p=Game.Player.Character;var v=p.CurrentVehicle;if(v==null || v.Driver!=p || !Road(v))return null;
            bool seen=own?Witness(officer):World.GetNearbyVehicles(p.Position,options.WitnessRadius).Any(c=>c!=v && c.Exists() && c.Model.IsLawEnforcementVehicle && Witness(c.Driver));if(!seen)return null;
            var frame=Frame(v);var lines=TrafficRules.Evaluate(frame,options,Wallet.Traffic?.AdvancedOffences==true);if(lines.Count==0)return null;
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
            t.BoloAtStop=Wallet.Traffic.BoloEnabled && PoliceResolutionRules.Bolo(Wallet.Traffic,t.Model,t.Plate);
            t.Pursuit=Wallet.Traffic.PursuitEnabled?pursuitOptions:null;
            if(!Wallet.TrafficBegin(t,out bool saved) || !saved)return false;ticketId=t.Id;pursuitPlan=t.Pursuit;pursuitClock=pursuitPlan==null?null:new TrafficPursuitClock();observer=false;elapsed=stopped=inspection=0;cruiser.IsSirenActive=true;taskAt=0;Notify.Show("FISCALIZACAO: encoste com seguranca e permaneca ao volante. Autuacao registrada por $"+t.Total+"; sem debito automatico.");return true;
        }
        void Sample(int delta)
        {
            sampleElapsed+=delta;if(scanAt!=0 && unchecked(Game.GameTime-scanAt)<0)return;int sampled=Math.Min(1000,sampleElapsed);sampleElapsed=0;scanAt=Game.GameTime+500;var t=Observe(Active);string key=t==null?null:t.Model+"|"+t.Plate+"|"+t.Street+"|"+string.Join(",",t.Lines.Select(l=>l.Code+":"+l.Amount));
            if(!witnessMeter.Observe(key,sampled,options.SampleMilliseconds) || t==null)return;witnessMeter.Reset();if(Active){if(!Register(t))Close("interrupted");}else Begin(t);
        }
        void Behaviour(int delta,bool eligible)
        {
            var v=Game.Player.Character.CurrentVehicle;bool actual=eligible && Wallet.Traffic?.AdvancedOffences==true && v!=null && v.Exists() && v.Driver==Game.Player.Character;
            behaviour.Observe(actual?v.Handle:0,actual && Function.Call<bool>(Hash.IS_HORN_ACTIVE,v),delta,actual);
        }
        int BribeQuote=>PoliceResolutionRules.Price(Wallet.Traffic?.Tickets.FirstOrDefault(t=>t.Id==ticketId)?.Total??0);
        bool CanBribe=>Active && !preparing && walking && ticketId!=null && profile==Profile.Id && Safe && Driver && Tagged(officer,token) && Tagged(cruiser,token) && officer.IsAlive && officer.IsOnFoot && !Connected(cruiser) && cruiser.Occupants.All(p=>p==officer) && vehicle.Speed<.5f && officer.Position.DistanceTo(actor.Position)<=2.5f && Wallet.Traffic?.BribesEnabled==true && Wallet.Traffic.Pending==ticketId && Wallet.Traffic.Tickets.Any(t=>t.Id==ticketId && t.Bribe==null);
        void OfferBribe()
        {
            if(!CanBribe){Notify.Show("Aguarde o agente a pe, junto ao carro parado, durante a abordagem.");return;}
            string oldProfile=profile;var oldActor=actor;string id=ticketId;
            int stars=pursuitPlan==null?0:TrafficPursuitRules.BackupLevel(0,Function.Call<int>(Hash.GET_MAX_WANTED_LEVEL),Math.Min(2,pursuitPlan.BackupStars));
            if(!Wallet.TrafficBribe(id,bribeRandom.Next(10000),stars,out bool changed) || !changed){Notify.Show("Sem tentativa: confira saldo bancario e autuacao atual.");return;}
            var receipt=Wallet.Traffic.Tickets.First(t=>t.Id==id).Bribe;Close(receipt.Accepted?"served":"evaded");pool.HideAll();
            if(!receipt.Accepted && receipt.NativeStars>0 && oldProfile==Profile.Id && oldActor==Game.Player.Character && Game.Player.Wanted.WantedLevel==0 && !Game.IsMissionActive && Function.Call<int>(Hash.GET_MAX_WANTED_LEVEL)>=receipt.NativeStars){Game.Player.Wanted.SetWantedLevel(receipt.NativeStars,delayLawResponse:false);Game.Player.Wanted.ApplyWantedLevelChangeNow(delayLawResponse:false);}
            Notify.Show(receipt.Accepted?"Suborno aceito. Banco -$"+receipt.Price+"; autuacao resolvida sem apagar historico.":"Suborno recusado; multa conservada. BOLO/reforco conforme opcoes habilitadas.");
        }
        void ReferAndClose(string reason)
        {
            string oldProfile=profile;Ped oldActor=actor;
            int level=TrafficPursuitRules.BackupLevel(Game.Player.Wanted.WantedLevel,Function.Call<int>(Hash.GET_MAX_WANTED_LEVEL),pursuitPlan?.BackupStars??0);bool requested=false;
            try{
                if(level>0 && pursuitPlan!=null && Wallet.TrafficRefer(ticketId,level,out bool changed))requested=changed;
            }finally{Close("evaded");}
            // A durable request precedes the native effect. Never replay it on load, clear stars or reduce somebody else's wanted level.
            if(requested && oldProfile==Profile.Id && oldActor==Game.Player.Character && Game.Player.Wanted.WantedLevel==0 && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && Function.Call<int>(Hash.GET_MAX_WANTED_LEVEL)>=level){Game.Player.Wanted.SetWantedLevel(level,delayLawResponse:false);Game.Player.Wanted.ApplyWantedLevelChangeNow(delayLawResponse:false);}
            Notify.Show(reason+(requested?" Reforco nativo solicitado; use custodia0.82 para rendicao a policial nativo.":" Autuacao conservada; estrelas existentes/limite de outros mods preservados."));
        }
        void FootTick(int dt)
        {
            if(!pursuitClock.OnFoot){pursuitClock.EnterFoot();walking=false;inspection=0;elapsed=0;taskAt=0;Notify.Show("PATRULHA: pare a pe, desarmado, e aguarde o agente para concluir a autuacao.");}
            if(actor.IsInVehicle()){if(actor.CurrentVehicle.Driver==actor)ReferAndClose("Retorno a veiculo durante fuga a pe.");else Close("interrupted");return;}
            if(!actor.IsOnFoot){Close("interrupted");return;}
            float distance=officer.Position.DistanceTo(actor.Position);bool compliant=actor.Velocity.Length()<.5f && actor.Weapons.Current.Hash==WeaponHash.Unarmed;
            if(distance>pursuitPlan.EscapeDistance && !compliant || pursuitClock.Advance(dt,!compliant,pursuitPlan)){ReferAndClose("Fuga da abordagem a pe.");return;}
            if(elapsed>=Math.Max(options.StopSeconds,pursuitPlan.FootSeconds*2)*1000){Close("interrupted");Notify.Show("Atendimento a pe expirou sem conclusao; autuacao conservada, sem estrelas por espera.");return;}
            if(officer.IsInVehicle()){
                if(cruiser.Speed<1.5f && (taskAt==0 || unchecked(Game.GameTime-taskAt)>=0)){JobActors.Leave(officer,cruiser);taskAt=Game.GameTime+3000;}
                else if(vehicle!=null && vehicle.Exists() && cruiser.Driver==officer && (taskAt==0 || unchecked(Game.GameTime-taskAt)>=0)){Function.Call(Hash.TASK_VEHICLE_ESCORT,officer,cruiser,vehicle,-1,12f,786603,10f,8,5f);taskAt=Game.GameTime+3000;}
            }else if(officer.IsOnFoot && (taskAt==0 || unchecked(Game.GameTime-taskAt)>=0)){Function.Call(Hash.TASK_GO_TO_ENTITY,officer,actor,-1,1.2f,2f,1073741824,0);taskAt=Game.GameTime+3000;}
            if(officer.IsOnFoot && Witness(officer) && TrafficPursuitRules.FootInspection(actor.IsOnFoot,actor.Weapons.Current.Hash==WeaponHash.Unarmed,actor.Velocity.Length(),distance))inspection+=dt;else inspection=0;
            if(inspection>=options.InspectionMilliseconds){Close("served");Notify.Show("Conferencia a pe concluida. Multa disponivel no F11; sem debito automatico.");return;}
            GTA.UI.Screen.ShowSubtitle("PATRULHA: pare e fique desarmado. Aguarde conferencia; END suspende sem apagar a multa.",100);
        }
        public void Tick()
        {
            Drain();if(!Active){
                if(Wallet.Traffic?.Valid==true && Wallet.Traffic.Pending!=null && recovered!=Profile.Id+"|"+Wallet.Traffic.Pending){recovered=Profile.Id+"|"+Wallet.Traffic.Pending;Wallet.TrafficFinish(Wallet.Traffic.Pending,"interrupted",out _);}
                bool ready=StartSafe && !pool.AreAnyVisible;int delta=GangWarRules.Delta(last,Game.GameTime,ready && eligibleBefore);last=Game.GameTime;eligibleBefore=ready;Behaviour(delta,ready);
                if(cooldown>0){cooldown=Math.Max(0,cooldown-delta);return;}if(ready)Sample(delta);else{sampleElapsed=0;witnessMeter.Reset();}return;
            }
            bool input=!Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;int dt=GangWarRules.Delta(last,Game.GameTime,input && eligibleBefore);last=Game.GameTime;eligibleBefore=input;
            bool footAllowed=ticketId!=null && pursuitPlan!=null && actor!=null && actor.Exists() && (actor.IsOnFoot || pursuitClock.OnFoot && actor.IsInVehicle());
            if(profile!=Profile.Id || actor!=Game.Player.Character || !Safe || !(Driver || footAllowed) || Wallet.Traffic?.Valid!=true || Wallet.Traffic.Enabled!=true || gate.Owner!=Name || !Wallet.GangWarEconomyFree || !gangFree() || Function.Call<int>(Hash.GET_INTERIOR_FROM_ENTITY,actor)!=0){Close("interrupted");return;}
            Behaviour(dt,input);if(!input)return;
            if(preparing){if(unchecked(Game.GameTime-requested)>10000){Close("interrupted");return;}if(carModel.IsLoaded && pedModel.IsLoaded)Spawn();return;}
            if(!Tagged(officer,token) || !Tagged(cruiser,token) || officer.IsDead || cruiser.IsDead || Connected(cruiser) || cruiser.Occupants.Any(p=>p!=officer)){Close("interrupted");return;}
            elapsed+=dt;
            if(observer){if(elapsed>=options.ObserverSeconds*1000){Close("interrupted");Notify.Show("Observacao encerrada sem nova autuacao.");return;}Sample(dt);if(!Active)return;}
            else{
                if(Wallet.Traffic.Pending!=ticketId){Close("interrupted");return;}
                if(pursuitPlan!=null && (actor.IsOnFoot || pursuitClock.OnFoot)){FootTick(dt);return;}
                if(pursuitPlan!=null && (actor.Position.DistanceTo(cruiser.Position)>pursuitPlan.EscapeDistance && vehicle.Speed>=.5f || pursuitClock.Advance(dt,vehicle.Speed>=.5f,pursuitPlan))){ReferAndClose("Fuga da abordagem de transito.");return;}
                if(elapsed>=(pursuitPlan==null?options.StopSeconds:Math.Max(options.StopSeconds,pursuitPlan.VehicleSeconds*2))*1000 || pursuitPlan==null && actor.Position.DistanceTo(cruiser.Position)>300){Close("evaded");Notify.Show("Abordagem nao concluida; autuacao por placa no F11. Sem novas estrelas por espera ou modo antigo.");return;}
                if(!walking){if(TrafficRules.Stopped(Driver,vehicle.Speed,cruiser.Speed,cruiser.Position.DistanceTo(vehicle.Position)))stopped+=dt;else stopped=0;if(stopped>=2000){walking=true;JobActors.Leave(officer,cruiser);taskAt=0;}}
                if(walking){if(officer.IsOnFoot && (taskAt==0 || unchecked(Game.GameTime-taskAt)>=0)){var point=vehicle.GetOffsetPosition(new Vector3(-1.6f,0,0));Function.Call(Hash.TASK_FOLLOW_NAV_MESH_TO_COORD,officer,point.X,point.Y,point.Z,1f,10000,.6f,0,actor.Heading);taskAt=Game.GameTime+4000;}
                    if(officer.IsOnFoot && TrafficRules.Inspect(Driver,!officer.IsDead,vehicle.Speed,officer.Position.DistanceTo(actor.Position)))inspection+=dt;else inspection=0;
                    if(CanBribe && Game.IsControlJustPressed(GTA.Control.Context)){OfferBribe();return;}
                    if(CanBribe)GTA.UI.Screen.ShowHelpTextThisFrame("~INPUT_CONTEXT~: oferecer suborno $"+BribeQuote+" pelo banco (tentativa unica)");
                    if(inspection>=options.InspectionMilliseconds){Close("served");Notify.Show("Abordagem concluida. Consulte e pague a autuacao em F11 > Policia / transito. Banco nao foi debitado automaticamente.");return;}
                }
                GTA.UI.Screen.ShowSubtitle(walking?"POLICIA: permaneça parado ao volante para conferencia dos documentos.":"POLICIA: encoste e pare com seguranca; aguarde o agente. END encerra sem apagar a autuacao.",100);
            }
            if(!walking && (taskAt==0 || unchecked(Game.GameTime-taskAt)>=0)){taskAt=Game.GameTime+4000;if(cruiser.Driver==officer){if(ticketId!=null && pursuitPlan!=null && vehicle.Speed>=3){Function.Call(Hash.TASK_VEHICLE_CHASE,officer,actor);Function.Call(Hash.SET_TASK_VEHICLE_CHASE_IDEAL_PURSUIT_DISTANCE,officer,25f);Function.Call(Hash.SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG,officer,32,true);}else Function.Call(Hash.TASK_VEHICLE_ESCORT,officer,cruiser,vehicle,-1,28f,786603,10f,8,5f);}}
        }
        static bool Connected(Vehicle v){var trailer=new OutputArgument();return Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,v) || Function.Call<bool>(Hash.GET_VEHICLE_TRAILER_VEHICLE,v,trailer);}
        void Close(string outcome)
        {
            try{if(profile==Profile.Id && ticketId!=null && Wallet.Traffic?.Pending==ticketId)Wallet.TrafficFinish(ticketId,outcome,out _);}finally{
                if(Tagged(cruiser,token))cruiser.IsSirenActive=false;if(officer!=null || cruiser!=null)retired.Add(new Retired{Car=cruiser,Officer=officer,Token=token,Last=Game.GameTime-4000});officer=null;cruiser=null;actor=null;vehicle=null;if(carModel.Hash!=0)carModel.MarkAsNoLongerNeeded();if(pedModel.Hash!=0)pedModel.MarkAsNoLongerNeeded();carModel=pedModel=default(Model);if(blip!=null && blip.Exists())blip.Delete();blip=null;ticketId=profile=null;detected=null;observer=walking=preparing=false;pursuitPlan=null;pursuitClock=null;behaviour.Reset();sampleElapsed=0;witnessMeter.Reset();cooldown=options.CooldownSeconds*1000;gate.Release(Name);last=Game.GameTime;eligibleBefore=false;
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
