using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI.Menus;
namespace RPCore
{
    public sealed partial class SettingsSystem
    {
        Func<IEnumerable<GpsDestination>> destinations;
        ShiftGate actionGate;
        MapPin selectedPin;string selectedPinProfile;
        public void ConfigureNavigation(Func<IEnumerable<GpsDestination>> source,ShiftGate gate=null){destinations=source;actionGate=gate;}
        bool CanOpenActions()
        {
            var p=Game.Player.Character;return p!=null && p.Exists() && !p.IsDead && !Game.IsPaused && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && Game.Player.CanControlCharacter && Profile.Detect()==Profile.Id;
        }
        void SavePin()
        {
            if(!CanOpenActions())return;
            if(!MapPinRules.Valid(map)){Notify.Show("Marcadores indisponiveis. Confira RPCore.log.");return;}
            string profile=Profile.Id;int actor=Game.Player.Character.Handle;
            string name=Game.GetUserInput(WindowTitle.EnterCustomTeamName,"Ponto "+(map.Count+1),32);
            if(!CanOpenActions() || Profile.Id!=profile || Game.Player.Character.Handle!=actor)return;
            var p=Game.Player.Character.Position;
            if(MapPinRules.Add(ref map,name,p.X,p.Y,p.Z,n=>Store.Write("MapPins.json",n))){RefreshPins();Notify.Show("Ponto salvo.");}
            else if(!string.IsNullOrWhiteSpace(name))Notify.Show("Ponto nao salvo. Limite:100 marcadores por personagem.");
        }
        void RefreshPins()
        {
            foreach(var b in pins)if(b.Exists())b.Delete();pins.Clear();
            if(!MapPinRules.Valid(map))return;
            foreach(var point in map){var b=World.CreateBlip(new Vector3(point.X,point.Y,point.Z));b.Name=point.Name;b.IsShortRange=true;b.Color=BlipColor.Blue;pins.Add(b);}
        }
        void BuildNearbyGps()
        {
            gpsNearest.Clear();var entries=(destinations?.Invoke()??Enumerable.Empty<GpsDestination>()).Where(p=>p?.Valid==true).ToList();
            var kinds=new[]{"Fuel","Grocery","Workshop","Garage","Clothing","Barber","PDM"};var names=new[]{"Posto","Conveniencia","Oficina","Garagem","Loja de roupas","Barbearia","PDM"};
            var position=Game.Player.Character.Position;
            for(int i=0;i<kinds.Length;i++)
            {
                string kind=kinds[i];var near=MapPinRules.Nearest(entries,kind,position.X,position.Y,position.Z);if(near==null)continue;
                Add(gpsNearest,names[i]+" / "+near.Name,"Destino mais proximo nesta categoria. Aproximadamente "+MapPinRules.Distance(near,position.X,position.Y,position.Z).ToString("0")+" m.",()=>{
                    if(!CanOpenActions())return;var p=Game.Player.Character.Position;var actual=MapPinRules.Nearest(destinations?.Invoke(),kind,p.X,p.Y,p.Z);if(actual==null){Notify.Show("Destino indisponivel.");return;}Function.Call(Hash.SET_NEW_WAYPOINT,actual.X,actual.Y);pool.HideAll();Notify.Show("GPS: "+actual.Name);
                });
            }
            Shortcut(gpsNearest,"Garagens / gerenciar",()=>Cfg.GarageKey,()=>Cfg.EnableGarages,()=>GarageRequested);
            Shortcut(gpsNearest,"Concessionaria / GPS",()=>Cfg.DealerKey,()=>Cfg.EnableGarages,()=>DealerRequested);
            Shortcut(gpsNearest,"Gangues / GPS",()=>Cfg.GangsKey,()=>Cfg.EnableGangs);
        }
        void BuildPersonalGps()
        {
            gpsPersonal.Clear();Add(gpsPersonal,"Salvar local atual","Marcador pessoal por personagem, sem alterar o save nativo.",()=>{SavePin();BuildPersonalGps();});
            if(!MapPinRules.Valid(map)){gpsPersonal.Add(new NativeItem("Marcadores indisponiveis. Confira RPCore.log."){Enabled=false});return;}
            foreach(var value in map)
            {
                var point=value;var row=gpsPersonal.AddSubMenu(pinDetails,"GPS, renomear ou excluir com confirmacao.");row.Title=point.Name;row.Activated+=(s,e)=>OpenPin(point);
            }
        }
        void OpenPin(MapPin point)
        {
            selectedPin=point;selectedPinProfile=Profile.Id;pinDetails.Clear();pinConfirm.Clear();pinDetails.Name=point.Name;
            Add(pinDetails,"Marcar rota","Define o waypoint sem mover o personagem.",()=>{if(!PinContext())return;Function.Call(Hash.SET_NEW_WAYPOINT,point.X,point.Y);pool.HideAll();});
            Add(pinDetails,"Renomear","Mantem posicao e demais dados do marcador.",()=>{
                if(!PinContext())return;int actor=Game.Player.Character.Handle;string name=Game.GetUserInput(WindowTitle.EnterCustomTeamName,point.Name,32);
                if(!PinContext() || Game.Player.Character.Handle!=actor)return;
                if(MapPinRules.Rename(ref map,map.IndexOf(point),point,name,n=>Store.Write("MapPins.json",n)))FinishPinEdit();
            });
            var confirmation=pinDetails.AddSubMenu(pinConfirm,"Remove apenas este marcador RPCore.");confirmation.Title="Excluir marcador...";
            Add(pinConfirm,"Cancelar / manter marcador","Nada sera removido.",()=>{pinConfirm.Visible=false;pinDetails.Visible=true;});
            Add(pinConfirm,"Confirmar exclusao: "+point.Name,"Nao altera outros pontos nem blips de outros mods.",()=>{if(PinContext() && MapPinRules.Delete(ref map,map.IndexOf(point),point,n=>Store.Write("MapPins.json",n)))FinishPinEdit();});
        }
        bool PinContext()=>CanOpenActions() && selectedPinProfile==Profile.Id && MapPinRules.Valid(map) && map.Contains(selectedPin);
        void FinishPinEdit(){RefreshPins();pool.HideAll();BuildPersonalGps();gpsPersonal.Visible=true;selectedPin=null;}
    }
}
