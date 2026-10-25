using System;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
namespace RPCore
{
    internal sealed partial class GangPresence
    {
        public int CrewCount=>actors.Values.Count(a=>a.Crew);
        bool CrewCalm=>!Game.IsPaused && !Game.IsMissionActive && !InterfaceOptions.ModalOpen && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && !Game.Player.Character.IsDead && !Game.Player.Character.IsInCombat && Game.Player.Wanted.WantedLevel==0;
        public bool RecruitNearest(string affiliation)
        {
            var player=Game.Player.Character;
            var candidate=actors.Values.Where(a=>a.Ped!=null && a.Ped.Exists() && GangCrewRules.Recruit(affiliation,a.Gang,CrewCount,a.Ped.Position.DistanceTo(player.Position),true,a.Leader,a.Crew,!a.Ped.IsDead,a.Ped.IsOnFoot && player.IsOnFoot,CrewCalm)).OrderBy(a=>a.Ped.Position.DistanceTo(player.Position)).FirstOrDefault();
            if(candidate==null)return false;
            candidate.Formation=Enumerable.Range(0,3).First(i=>actors.Values.All(a=>!a.Crew || a.Formation!=i));candidate.Crew=true;candidate.Waiting=false;candidate.Returning=false;candidate.NextTask=0;candidate.Ped.Task.ClearAll();
            Function.Call(Hash.SET_PED_CONFIG_FLAG,candidate.Ped,(int)PedConfigFlagToggles.PreventAutoShuffleToDriversSeat,true);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES,candidate.Ped,(int)CombatAttributes.CanCommandeerVehicles,false);
            candidate.Blip=MapBlips.Create(candidate.Ped.Position,"RPCore: equipe / "+candidate.Gang,BlipSprite.Friend,BlipColor.Blue,.5f);candidate.Blip.IsShortRange=false;
            return true;
        }
        public bool Order(bool wait)
        {
            if(!CrewCalm || CrewCount==0 || actors.Values.Any(a=>a.Crew && a.Ped!=null && a.Ped.Exists() && a.Ped.IsInVehicle() && !GangCrewRules.CanExit(a.Ped.CurrentVehicle.Speed)))return false;
            foreach(var a in actors.Values.Where(a=>a.Crew)){
                a.Waiting=wait;a.WaitPost=a.Ped.Position;a.NextTask=0;a.Boarding=null;a.Seat=-2;
                if(!a.Ped.IsInVehicle())a.Ped.Task.ClearAll();
            }return true;
        }
        public void DismissCrew()
        {
            foreach(var pair in actors.Where(p=>p.Value.Crew).ToArray()){
                var a=pair.Value;a.Crew=false;a.Waiting=false;a.Returning=false;a.Departing=true;a.DepartAt=Game.GameTime;a.Boarding=null;a.Seat=-2;a.NextTask=0;
                if(a.Blip!=null && a.Blip.Exists())a.Blip.Delete();a.Blip=null;
                if(a.Ped!=null && a.Ped.Exists() && !a.Ped.IsInVehicle())a.Ped.Task.ClearAll();
            }
        }
        void UpdateCrew(Actor a,int now)
        {
            var ped=a.Ped;var player=Game.Player.Character;
            if(a.Blip!=null && a.Blip.Exists())a.Blip.Position=ped.Position;
            if(ped.IsInCombat || player.IsInCombat)return;
            if(a.Boarding!=null && (unchecked(now-a.BoardUntil)>=0 || !a.Boarding.Exists() || !player.IsInVehicle(a.Boarding) || !GangCrewRules.Board(a.Boarding.Driver==player,a.Boarding.Model.IsCar,JobVehicles.Contains(a.Boarding),!a.Boarding.IsDead && a.Boarding.IsDriveable,a.Boarding.Speed,ped.Position.DistanceTo(a.Boarding.Position)) || a.Seat>=0 && !a.Boarding.IsSeatFree((VehicleSeat)a.Seat) && a.Boarding.GetPedOnSeat((VehicleSeat)a.Seat)!=ped)){
                if(!ped.IsInVehicle())ped.Task.ClearAll();a.Boarding=null;a.Seat=-2;a.NextTask=0;
            }
            if(ped.IsInVehicle()){
                var car=ped.CurrentVehicle;
                if(!a.Waiting && player.IsInVehicle(car))return;
                if(GangCrewRules.CanExit(car.Speed) && unchecked(now-a.NextTask)>=0){JobActors.Leave(ped,car);a.NextTask=now+6000;}
                return;
            }
            if(unchecked(now-a.NextTask)<0)return;
            if(a.Waiting){
                if(ped.Position.DistanceTo(a.WaitPost)>2)JobActors.Walk(ped,a.WaitPost);else ped.Task.StandStill(-1);
                a.NextTask=now+8000;return;
            }
            var target=player.CurrentVehicle;
            if(target!=null && target.Exists()){
                if(a.Boarding!=null && a.Boarding==target && unchecked(now-a.BoardUntil)<0)return;
                if(!GangCrewRules.Board(target.Driver==player,target.Model.IsCar,JobVehicles.Contains(target),!target.IsDead && target.IsDriveable,target.Speed,ped.Position.DistanceTo(target.Position)))return;
                var held=actors.Values.Where(x=>x!=a && x.Crew && x.Boarding==target && unchecked(now-x.BoardUntil)<0).Select(x=>x.Seat);
                int seat=GangCrewRules.Seat(Enumerable.Range(0,Math.Max(0,Math.Min(8,target.PassengerCapacity))).Where(i=>target.IsSeatFree((VehicleSeat)i)),held);
                if(seat<0)return;
                a.Boarding=target;a.Seat=seat;a.BoardUntil=now+15000;Function.Call(Hash.TASK_ENTER_VEHICLE,ped,target,15000,seat,1f,(int)(EnterVehicleFlags.DontJackAnyone|EnterVehicleFlags.BlockSeatShuffling),0);a.NextTask=now+1000;return;
            }
            if(ped.Position.DistanceTo(player.Position)>2.5f){Function.Call(Hash.TASK_FOLLOW_TO_OFFSET_OF_ENTITY,ped,player,a.Formation==0?-1.5f:a.Formation==1?1.5f:0f,a.Formation==2?-3.5f:-2f,0f,2f,-1,2f,true);a.NextTask=now+5000;}
        }
    }
}
