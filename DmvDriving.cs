using System;
using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;
namespace RPCore
{
    public sealed partial class DmvSystem
    {
        DmvAssessmentOptions examPolicy;DmvFaultMonitor faultMonitor;bool legacyAttempt;
        void AddFault(DmvFaultKind kind,int point=-1)
        {
            if(DmvAssessmentRules.Record(drive,kind,point))Notify.Show("DMV: falta "+drive.Faults+"/"+examOptions.MaxFaults+" / "+DmvAssessmentRules.FaultName(kind)+".");
        }
        void DriveTick()
        {
            if(!ActorBound){EndRuntime();ui.Close();return;}
            if(Game.IsPaused){last=Game.GameTime;return;}
            if(!GameplayContext.BackgroundReady || GameplayContext.SceneBlocking){Save();EndRuntime();ui.Close();Notify.Show("DMV: exame suspenso durante carregamento/cena. Retome sem nova taxa.");return;}
            if(Game.IsMissionActive || Game.Player.Wanted.WantedLevel>0 || Game.Player.Character.IsDead || car==null || !PdmAssets.Valid(car) || car.IsDead || examiner==null || !PdmAssets.Valid(examiner) || examiner.IsDead){Fail("exame interrompido/veiculo ou examinadora indisponivel");return;}
            if(Truck && (trailer==null || !PdmAssets.Valid(trailer) || trailer.IsDead || trailer.BodyHealth<500 || JobVehicles.TrailerHasOtherTractor(trailer,car) || Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER,car) && !Towing)){Fail("carreta indisponvel ou engate incorreto");return;}
            float dt=Math.Min(1,Math.Max(0,unchecked(Game.GameTime-last)/1000f));last=Game.GameTime;
            if(car.Position.DistanceTo(previous)>Math.Max(80,car.Speed*dt+30)){Fail("deslocamento discontinuo do veiculo");return;}previous=car.Position;
            float trailerMoved=0;
            if(Truck){trailerMoved=trailer.Position.DistanceTo(previousTrailer);if(trailerMoved>Math.Max(80,trailer.Speed*dt+30)){Fail("deslocamento discontinuo da carreta");return;}previousTrailer=trailer.Position;}
            if(pool.AreAnyVisible){if(car.Speed<=.5f && (!Truck || trailer.Speed<=.5f)){stopTime=0;return;}ui.Close();Notify.Show("Pare o conjunto antes de consultar o DMV. A avaliacao continua em movimento.");}
            if(drive.Stage!=DmvStage.AwaitDriver)drive.Seconds+=dt;
            if(Truck){
                if(drive.Coupled && !drive.Reversed && !Towing)disconnected+=dt;else disconnected=0;
                if(disconnected>=5){Fail("carreta desacoplada durante o percurso");return;}
                if(drive.Stage==DmvStage.Parking)reverseDistance=DmvRigRules.ReverseDistance(reverseDistance,trailerMoved,car.GetSpeedVector(true).Y,Towing,Driver && ExaminerSeated && trailer.Position.DistanceTo2D(Pos(bay))<=40 && trailer.GetSpeedVector(true).Y<-.2f && DmvRigRules.HeadingDifference(car.Heading,trailer.Heading)<=40,Protection);
                if(drive.Couple(Driver,ExaminerSeated,Protection,Towing,examOptions)){Save();Route();}
            }
            if(drive.Stage!=DmvStage.Report && Game.Player.Character.Position.DistanceTo(car.Position)>100){Fail("abandono do veiculo de exame");return;}
            if((int)drive.Stage>=(int)DmvStage.RightSignal && (int)drive.Stage<=(int)DmvStage.Parking && (!ExaminerSeated || Game.Player.Character.IsInVehicle() && !Driver)){Fail("use o veiculo correto com a examinadora no banco passageiro");return;}
            bool assessed=Driver && (drive.Stage==DmvStage.Route || drive.Stage==DmvStage.Parking);
            float currentDamage=car.BodyHealth+car.EngineHealth+(Truck?trailer.BodyHealth:0),loss=Math.Max(0,damage-currentDamage);
            var faults=faultMonitor.Update(dt,assessed && car.Speed*3.6f>SpeedLimit+5,assessed && car.Speed>1 && (examOptions.RequireSeatbelt || examOptions.RequireHelmet) && !Protection,assessed?loss:0,examPolicy);
            foreach(var kind in faults)AddFault(kind);if(loss>=examPolicy.CollisionDamage || currentDamage>damage)damage=currentDamage;
            if(car.EngineHealth<400 || car.BodyHealth<500){Fail("danos graves");return;}
            if(drive.Failed(examOptions)){Fail(drive.Faults>=examOptions.MaxFaults?"limite de faltas atingido":"tempo de exame esgotado");return;}
            var stage=drive.Stage;int point=drive.Point;
            bool inTarget=car.Position.DistanceTo2D(Target)<=7 && Math.Abs(car.Position.Z-Target.Z)<=4;
            if(drive.Stage==DmvStage.Route)stopTime=Driver && ExaminerSeated && inTarget && car.Speed<=.5f?stopTime+dt:0;
            switch(drive.Stage){
                case DmvStage.AwaitDriver:drive.Enter(Driver,ExaminerSeated);break;
                case DmvStage.RightSignal:case DmvStage.LeftSignal:drive.Signal(car.IsRightIndicatorLightOn,car.IsLeftIndicatorLightOn,Protection,Driver && car.Speed<=.5f,ExaminerSeated,examOptions);break;
                case DmvStage.Route:
                    if(drive.Point==examOptions.Route.Count && (!Truck || drive.Coupled))drive.Stage=DmvStage.Parking;
                    else if(drive.Checkpoint(Driver,ExaminerSeated,inTarget && (!(examOptions.RequireSeatbelt || examOptions.RequireHelmet) || Protection),car.Speed*3.6f<=SpeedLimit && (!Truck || Towing),stopTime>=examPolicy.StopSeconds,examOptions)){
                        int turn=examPolicy.RouteIndicators?DmvAssessmentRules.Turn(examOptions,point,examPolicy.TurnDegrees):0;
                        if(turn>0 && (!car.IsLeftIndicatorLightOn || car.IsRightIndicatorLightOn) || turn<0 && (!car.IsRightIndicatorLightOn || car.IsLeftIndicatorLightOn))AddFault(DmvFaultKind.Indicator,point);
                    }break;
                case DmvStage.Parking:
                    if(!Truck){
                        bool protection=!(examOptions.RequireSeatbelt || examOptions.RequireHelmet) || Protection;
                        bool parked=legacyAttempt?Driver && ExaminerSeated && protection && inTarget && car.Speed<=.5f:DmvAssessmentRules.Parking(car.Position.DistanceTo2D(Pos(examOptions.Spawn)),car.Position.Z-examOptions.Spawn.Z,car.Heading,examOptions.Spawn.Heading,car.Speed,Driver,ExaminerSeated,protection,examPolicy);
                        stopTime=parked?stopTime+dt:0;drive.Park(stopTime>=examPolicy.ParkingSeconds);
                    }else{
                        stopTime=InBay && Driver && ExaminerSeated && Protection?stopTime+dt:0;
                        if(drive.Reverse(Towing && reverseDistance>=examOptions.ReverseMeters && stopTime>=examPolicy.ParkingSeconds,examOptions))Save();
                        if(drive.Detach(!Towing && InBay && Driver && ExaminerSeated && Protection,examOptions))Save();
                        if(drive.Detached && !Towing && InBay)drive.Park(true);
                    }break;
                case DmvStage.Exit:drive.Leave(!Game.Player.Character.IsInVehicle() && car.Speed<.5f && Game.Player.Character.Position.DistanceTo(car.Position)<12);break;
            }
            if(stage!=drive.Stage || point!=drive.Point){
                stopTime=0;Save();
                if(stage==DmvStage.LeftSignal && drive.Stage==DmvStage.Route){Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS,car,0,false);Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS,car,1,false);}
                if(drive.Stage==DmvStage.Report){var desk=Desk(category);EndRuntime();ui.Close();Function.Call(Hash.SET_NEW_WAYPOINT,desk.X,desk.Y);Notify.Show("Percurso concluido. Volte ao atendimento para emitir a habilitacao sem nova taxa.");return;}
                if(stage==DmvStage.AwaitDriver)GTA.UI.Screen.ShowSubtitle("EXAMINADORA: bem-vindo. Com o veiculo parado, equipe a protecao e demonstre as setas.",4000);
                Route();
            }else if(faults.Count>0 || unchecked(Game.GameTime-saveAt)>=0)Save();
            if(drive.Failed(examOptions)){Fail("limite de faltas atingido");return;}
            if(routedStage!=drive.Stage || routedPoint!=drive.Point)Route();DrawDrive();
        }
        void DrawDrive()
        {
            string task=drive.Stage==DmvStage.AwaitDriver?"entre como motorista":drive.Stage==DmvStage.RightSignal?(Moto?"capacete / seta DIREITA":"cinto / seta DIREITA"):drive.Stage==DmvStage.LeftSignal?"seta ESQUERDA":drive.Stage==DmvStage.Parking?(Truck?(drive.Reversed?"carreta alinhada / desengate no DMV F10":"re "+reverseDistance.ToString("0.0")+"/"+examOptions.ReverseMeters+"m / alinhe e pare"):"alinhe na vaga / pare "+stopTime.ToString("0.0")+"/"+examPolicy.ParkingSeconds+"s"):drive.Stage==DmvStage.Exit?"pare e desembarque":Truck && !drive.Coupled?"engate a carreta marcada":examOptions.Stops.Contains(drive.Point)?"parada obrigatoria / "+stopTime.ToString("0.0")+"/"+examPolicy.StopSeconds+"s":"siga o ponto marcado";
            int turn=drive.Stage==DmvStage.Route && examPolicy.RouteIndicators?DmvAssessmentRules.Turn(examOptions,drive.Point,examPolicy.TurnDegrees):0;if(turn!=0)task+=" / sinalize "+(turn>0?"ESQUERDA":"DIREITA");
            if(!Visuals.TryJobHeader("core","DMV / classe "+ClassName,Color.White))Visuals.Text("DMV / "+ClassName,Visuals.Width/2-100,30,.4f,Color.White);
            float x=(Visuals.Width-850)/2;Visuals.Rect(x,875,850,110,Color.FromArgb(220,10,18,28));Visuals.Label(task,x+15,886,.34f,Color.White,wrap:820);
            Visuals.Text(drive.Point+"/"+examOptions.Route.Count+" pontos / faltas "+drive.Faults+"/"+examOptions.MaxFaults+" / limite "+SpeedLimit+" km/h / "+(int)drive.Seconds+"s / End: suspender",x+15,951,.28f,Color.FromArgb(140,210,240));
            if(drive.Stage==DmvStage.Route || drive.Stage==DmvStage.Parking){World.DrawMarker(MarkerType.Cylinder,Target-new Vector3(0,0,.8f),Vector3.Zero,Vector3.Zero,new Vector3(drive.Stage==DmvStage.Parking?3:5,5,1),Color.FromArgb(100,255,210,0));if(drive.Stage==DmvStage.Parking)World.DrawMarker(MarkerType.Chevron1,Target+new Vector3(0,0,1.5f),Vector3.Zero,new Vector3(0,0,Truck?bay.Heading:examOptions.Spawn.Heading),new Vector3(1,1,1),Color.FromArgb(180,80,210,140));}
        }
    }
}
