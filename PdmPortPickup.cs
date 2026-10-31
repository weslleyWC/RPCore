using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
namespace RPCore
{
    public sealed partial class PdmLogistics
    {
        readonly AreaWarmup portWarmup=new AreaWarmup();Blip portBlip;bool portReported,worldProblemReported;
        void PortTick()
        {
            var player=Game.Player.Character;var p=player.Position;
            bool near=p.DistanceTo(Pos(options.Dock))<150;
            bool safe=near && PdmMenuRuntime.WorldAllowed && PersonalServiceMenu.SafeFoot && GTA.UI.Screen.IsFadedIn;
            portWarmup.Update(Game.GameTime,player.Handle,Profile.Id,p.X,p.Y,p.Z,near,safe,safe && Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY,player));
            bool pending=Wallet.Pdm.Valid && Wallet.Pdm.Owned && Wallet.Pdm.Stock.Any(s=>s.AwaitingCollection);
            if(pending && (portBlip==null || !portBlip.Exists())){portBlip=MapBlips.Create(Pos(options.Dock),"RPCore: PDM / encomendas no porto",BlipSprite.PersonalVehicleCar,BlipColor.Orange,.85f);portBlip.IsShortRange=false;}
            if(!pending){ClearPort();return;}
            if(!near || Active || !PdmMenuRuntime.WorldAllowed)return;
            World.DrawMarker(MarkerType.Cylinder,Pos(options.Dock)-new Vector3(0,0,1),Vector3.Zero,Vector3.Zero,new Vector3(2.5f,2.5f,1),Color.FromArgb(110,255,145,40));
            if(InputAllowed && PersonalServiceMenu.SafeFoot && p.DistanceTo(Pos(options.Dock))<3){
                Prompt.Help("~INPUT_CONTEXT~: PDM / retirar encomendas. Escolha o pedido e inicie a retirada aqui.");
                if(Game.IsControlJustPressed(GTA.Control.Context))OpenMenu();
            }
        }
        void ClearPort(){if(portBlip!=null && portBlip.Exists())portBlip.Delete();portBlip=null;}
        PdmPickupPlan PickupPlan(IEnumerable<string> ids)=>PdmPickupRules.Plan(Wallet.Pdm,ids,Game.Player.Character.Position.DistanceTo(StartPosition),Game.Player.Character.Position.DistanceTo(Pos(options.Dock)));
        void PreparePickup(List<string> ids)
        {
            var plan=PickupPlan(ids);
            if(!plan.Allowed){ui.Problem("Retirada nao iniciada",plan.Reason,null,OpenMenu);return;}
            if(!PersonalServiceMenu.SafeFoot){ui.Problem("Retirada nao iniciada","Fique a pe, sem procurado, combate ou outra atividade, junto ao marcador.",null,OpenMenu);return;}
            var models=new[]{options.TruckModel,options.TrailerModel}.Concat(plan.StockIds.Select(id=>Wallet.Pdm.Stock.Find(s=>s.Id==id).Model));
            ui.Stream(models,()=>Begin(plan.StockIds.Count,plan.StockIds,plan.AtPort),OpenMenu,()=>plan.AtPort?portWarmup.Ready:GameplayContext.TerrainReady);
        }
    }
}
