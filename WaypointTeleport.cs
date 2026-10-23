using System;
using GTA;
using GTA.Math;
using GTA.Native;

namespace RPCore
{
    // Runs over normal ticks so distant terrain can stream without blocking the script.
    internal sealed class WaypointTeleport
    {
        Ped owner;
        Entity subject;
        Vector3 origin,waypoint,destination;
        bool wasFrozen,focus,moved,vehicle;
        int began,nextProbe,probe,movedAt;
        float targetGround;
        static readonly int[] slots={4,0,1,2,3,5,6,7,8};
        static readonly float[] heights={50,200,500,900,1400,1900};
        public bool Active=>subject!=null;
        public void Begin()
        {
            if(Active){Cancel(true);return;}
            if(!Game.IsWaypointActive){Notify.Show("Marque um waypoint no mapa primeiro.");return;}
            var p=Game.Player.Character;
            if(p.IsDead || p.IsRagdoll || Game.IsMissionActive){Notify.Show("Teleporte indisponivel neste momento.");return;}
            Entity selected=p;
            if(p.IsInVehicle())
            {
                var v=p.CurrentVehicle;
                if(v.Driver!=p){Notify.Show("Saia do veiculo ou assuma o banco do motorista.");return;}
                if(!(v.Model.IsCar || v.Model.IsBike || v.Model.IsQuadBike) || v.IsInAir || v.Speed>=.5f)
                {Notify.Show("Use a pe ou em carro/moto/onibus parado em terra.");return;}
                if(Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER,v))
                {Notify.Show("Para conjunto com carreta, desengate ou teleporte a pe.");return;}
                selected=v;
            }
            waypoint=World.WaypointPosition;
            if(float.IsNaN(waypoint.X) || float.IsNaN(waypoint.Y) || float.IsInfinity(waypoint.X) || float.IsInfinity(waypoint.Y) || Math.Abs(waypoint.X)>15000 || Math.Abs(waypoint.Y)>15000)
            {Notify.Show("Waypoint invalido. Marque outro ponto.");return;}
            owner=p;subject=selected;origin=subject.Position;wasFrozen=subject.IsPositionFrozen;vehicle=selected is Vehicle;
            began=Game.GameTime;nextProbe=began;probe=0;moved=false;
            try{subject.IsPositionFrozen=true;Notify.Show("Carregando terreno do waypoint... selecione novamente para cancelar.");}
            catch{Cancel(false);throw;}
        }
        public void Tick()
        {
            if(!Active)return;
            try
            {
                if(!subject.Exists() || !owner.Exists() || owner!=Game.Player.Character || owner.IsDead || Game.IsMissionActive || (vehicle && (!owner.IsInVehicle((Vehicle)subject) || ((Vehicle)subject).Driver!=owner)) || (!vehicle && owner.IsInVehicle()))
                {Cancel(false);return;}
                if(Game.IsPaused)return;
                if(unchecked(Game.GameTime-began)>10000){Cancel(false);Notify.Show("Terreno indisponivel ou sem espaco livre. Escolha outro waypoint.");return;}
                if(moved)
                {
                    Streaming.RequestCollisionAt(destination);
                    float currentGround;
                    if(unchecked(Game.GameTime-movedAt)<500 || !World.GetGroundHeight(destination+new Vector3(0,0,1000),out currentGround))return;
                    if(Math.Abs(currentGround-targetGround)>.6f){Cancel(false);Notify.Show("Terreno mudou durante o carregamento. Escolha outro ponto.");return;}
                    subject.Velocity=Vector3.Zero;Finish();Notify.Show("Teleporte concluido.");return;
                }
                if(unchecked(Game.GameTime-nextProbe)<0)return;
                nextProbe=Game.GameTime+150;
                var streamPoint=new Vector3(waypoint.X,waypoint.Y,heights[probe++%heights.Length]);
                focus=true;Function.Call(Hash.SET_FOCUS_POS_AND_VEL,streamPoint.X,streamPoint.Y,streamPoint.Z,0f,0f,0f);
                Streaming.RequestCollisionAt(streamPoint);
                for(int i=0;i<9;i++)
                {
                    float x=waypoint.X+(slots[i]%3-1)*5,y=waypoint.Y+(slots[i]/3-1)*5;
                    float ground;if(!World.GetGroundHeight(new Vector3(x,y,2000),out ground))continue;
                    var water=new OutputArgument();
                    if(Function.Call<bool>(Hash.GET_WATER_HEIGHT_NO_WAVES,x,y,2000f,water) && water.GetResult<float>()>ground+.1f)continue;
                    Vector3 min,max;subject.Model.GetDimensions(out min,out max);
                    var point=new JobPoint{Name="waypoint",X=x,Y=y,Z=ground-min.Z+.15f,Heading=subject.Heading};
                    if(!RigSpawn.ClearForTeleport(point,subject.Model,subject,owner))continue;
                    destination=new Vector3(point.X,point.Y,point.Z);targetGround=ground;Streaming.RequestCollisionAt(destination);
                    Function.Call(Hash.SET_FOCUS_POS_AND_VEL,destination.X,destination.Y,destination.Z,0f,0f,0f);
                    moved=true;movedAt=Game.GameTime;subject.Velocity=Vector3.Zero;subject.Position=destination;return;
                }
            }
            catch(Exception ex){Log.Error("waypoint teleport",ex);Cancel(false);Notify.Show("Teleporte cancelado. Veja RPCore.log.");}
        }
        void Finish()
        {
            try{if(subject!=null && subject.Exists())subject.IsPositionFrozen=wasFrozen;}
            finally
            {
                subject=null;owner=null;moved=false;
                if(focus){focus=false;Function.Call(Hash.CLEAR_FOCUS);}
            }
        }
        public void Cancel(bool notify)
        {
            if(!Active)return;
            try{if(moved && subject.Exists()){subject.Position=origin;subject.Velocity=Vector3.Zero;Streaming.RequestCollisionAt(origin);}}
            finally{Finish();}
            if(notify)Notify.Show("Teleporte cancelado.");
        }
    }
}
