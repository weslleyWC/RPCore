using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;

namespace RPCore
{
    internal sealed class GangSupply
    {
        const string OwnerKey="gang_supply";
        readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;
        readonly Func<IEnumerable<GangDefinition>> gangs;readonly Func<IEnumerable<GangTurfDefinition>> areas;
        readonly GangBusinessOptions business;readonly GangSupplyOptions options;
        readonly DeliveryCargo cargo=new DeliveryCargo();
        Vehicle vehicle;int model,player,last,elapsed,picked=-1;string plate,profile,orderId;bool carrying,arrived,reserved;
        Blip objective;public bool Active { get; private set; }
        GangSupplyOrder Pending=>Wallet.GangBusiness?.Supply;
        GangTurfDefinition CurrentArea=>Pending==null?null:areas().FirstOrDefault(a=>a.Id==Pending.Area.Id);
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        static Vector3 Pos(GangTurfDefinition a)=>new Vector3(a.Center.X,a.Center.Y,a.Center.Z);
        bool Safe=>Game.Player.Character.IsHuman && !Game.Player.Character.IsDead && !Game.Player.Character.IsInCombat && Game.Player.Wanted.WantedLevel==0 && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool Owns=>GangBusinessTransactions.Owns(state(),CurrentArea) && Pending!=null && GangCrewRules.SameGang(Pending.Gang,state().Gang) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang));
        bool Input=>Safe && !Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
        bool Car=>vehicle!=null && vehicle.Exists() && vehicle.Model.Hash==model && vehicle.Mods.LicensePlate==plate && !vehicle.IsDead && vehicle.IsDriveable;
        bool Stopped=>Car && vehicle.Speed<.5f && (vehicle.Driver==null || !vehicle.Driver.Exists() || vehicle.Driver==Game.Player.Character);
        Vector3 Rear=>vehicle.GetOffsetPosition(new Vector3(0,-3.2f,0));
        public GangSupply(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs,Func<IEnumerable<GangTurfDefinition>> areas,GangBusinessOptions business)
        {
            this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;this.areas=areas;this.business=business;
            options=Store.LoadDef("Gangs/Supply.json",()=>new GangSupplyOptions());
            if(options==null || !options.Valid){options=new GangSupplyOptions();Log.Write("Invalid gang supply config; original retained, defaults used.");}
        }
        public void Append(Action<string,string,Action> add,Action reopen)
        {
            var p=Pending;if(p==null)return;
            add("Frete reservado: "+p.Area.Name+" / $"+p.Cost,"Carga "+p.Loaded.Count+"/3, descarga "+p.Delivered+"/3. Lote de "+p.Units+" unidades. Conclua ou estorne antes de comprar/vender outro lote.",null);
            add(Active?"Suspender frete":"Retomar frete reservado",p.Ready?"A pe no destino, E confirma estoque. Carga pronta permanece pronta apos reload.":"A bordo de carro/van proprio parado no territorio de destino. Retomada refaz as tres caixas, sem pagar novamente.",()=>{if(Active){Suspend();reopen();}else if(!Resume())Notify.Show("Confira posse, carro proprio parado no destino, atividade e pendencias.");});
            add("Cancelar frete e estornar $"+p.Cost,"Sem estoque parcial. Estorna somente o custo reservado ao banco; pode cancelar mesmo depois de perder filiacao/posse. Sem combate/procurado/missao.",()=>{if(!Safe || Game.IsPaused || InterfaceOptions.ModalOpen || gate.Owner!=null && gate.Owner!=OwnerKey || !Wallet.GangSupplyRefund(p.Id,out _)){Notify.Show("Estorno recusado; confira seguranca e limite do banco. Reserva conservada.");return;}Suspend();reopen();});
        }
        public void AddStart(GangTurfDefinition area,Action<string,string,Action> add)
        {
            int cost=Math.Max(1,business.PurchaseUnits*business.UnitCost*(100-options.DiscountPercent)/100);
            add("Frete: "+business.PurchaseUnits+" unidades / $"+cost,"Carro/van proprio parado no territorio. Busque/carregue tres caixas, retorne e descarregue. "+options.DiscountPercent+"% desconto; sem estoque antes da entrega. Usa seu veiculo sem apaga-lo.",()=>{if(!Begin(area))Notify.Show("Confira posse, capacidade, banco, reserva/atividade e carro proprio parado no territorio.");});
        }
        bool Bind(GangTurfDefinition target)
        {
            var p=Game.Player.Character;var v=p.CurrentVehicle;
            if(!Safe || Game.IsPaused || InterfaceOptions.ModalOpen || v==null || !v.Exists() || !v.Model.IsCar || v.Driver!=p || v.Speed>=.5f || v.IsDead || !v.IsDriveable || JobVehicles.Contains(v) || p.Position.DistanceTo(Pos(target))>business.Radius+5)return false;
            vehicle=v;model=v.Model.Hash;plate=v.Mods.LicensePlate;return true;
        }
        void Activate()
        {
            Active=true;profile=Profile.Id;player=Game.Player.Character.Handle;orderId=Pending.Id;last=Game.GameTime;elapsed=0;carrying=arrived=false;picked=-1;
            if(vehicle!=null){JobVehicles.Reserve(vehicle);reserved=true;}pool.HideAll();Route();
            Notify.Show("Frete ativo: siga o GPS, E para pegar/carregar/descarregar. END suspende; reserva fica salva.");
        }
        bool Begin(GangTurfDefinition target)
        {
            if(Active || gate.Owner!=null || Pending!=null || !GangBusinessTransactions.Owns(state(),target) || !gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang)) || !Bind(target) || !gate.Acquire(OwnerKey))return false;
            try{if(!Wallet.GangSupplyBegin(state(),target,business,options,Guid.NewGuid().ToString("N"),out bool changed) || !changed){Suspend();return false;}Activate();return true;}
            catch{Suspend();throw;}
        }
        bool Resume()
        {
            if(Active || gate.Owner!=null || Pending==null || Wallet.GangBusiness?.Valid!=true || !Owns || !Safe || Game.IsPaused || InterfaceOptions.ModalOpen)return false;
            bool ready=Pending.Ready;
            if(ready){vehicle=null;if(!Game.Player.Character.IsOnFoot || Game.Player.Character.Position.DistanceTo(Pos(Pending.Area))>business.Radius)return false;}
            else if(!Bind(Pending.Area))return false;
            if(!gate.Acquire(OwnerKey))return false;
            try{if(!ready && !Wallet.GangSupplyRestart(Pending.Id,out _)){Suspend();return false;}Activate();return true;}
            catch{Suspend();throw;}
        }
        int Next=>Enumerable.Range(0,3).First(i=>!Pending.Loaded.Contains(i));
        Vector3 Target=>Pending.Ready?Pos(Pending.Area):carrying?(picked>=0?Rear:Pos(Pending.Area)):Pending.Loaded.Count<3?Pos(Pending.Options.Suppliers[Next]):arrived?Rear:Pos(Pending.Area);
        string Instruction=>Pending.Ready?"E: confirmar lote no estoque":carrying?(picked>=0?"E: guardar caixa na traseira":"E: entregar caixa no territorio"):Pending.Loaded.Count<3?"E: pegar caixa do fornecedor":arrived?"E: retirar caixa pela traseira":"Dirija o veiculo carregado ate o territorio e estacione";
        void Route()
        {
            if(objective!=null && objective.Exists())objective.Delete();objective=null;
            if(!Active || Pending==null)return;
            var point=Game.Player.Character.Position;WorkContext.ShopDelivery.Set(OwnerKey,new JobPoint{Name="gang supply",X=point.X,Y=point.Y,Z=point.Z},5);
            objective=MapBlips.Create(Target,"RPCore: reposicao / "+Pending.Area.Name,BlipSprite.Standard,BlipColor.Green,.8f);objective.ShowRoute=true;
        }
        public void Tick()
        {
            if(!Active)return;
            if(Pending==null || Wallet.GangBusiness?.Valid!=true || Pending.Id!=orderId || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=OwnerKey || !Safe || !Owns || !Pending.Ready && (!Car || Game.Player.Character.Position.DistanceTo(vehicle.Position)>150 || carrying && Game.Player.Character.IsInVehicle()))
            {Suspend();Notify.Show("Frete suspenso. Reserva conservada; retome ou cancele com estorno pelo F4.");return;}
            uint delta=unchecked((uint)(Game.GameTime-last));last=Game.GameTime;
            if(Input && delta<=2000)elapsed+=(int)delta;
            if(!Pending.Ready && elapsed>=Pending.Options.TimeLimitSeconds*1000){Suspend();Notify.Show("Prazo ativo encerrado. Retome o frete ou estorne a reserva no F4.");return;}
            if(!Pending.Ready && Pending.Loaded.Count==3 && !arrived && Stopped && Game.Player.Character.IsInVehicle(vehicle) && vehicle.Driver==Game.Player.Character && vehicle.Position.DistanceTo(Pos(Pending.Area))<=25){arrived=true;Route();}
            cargo.Tick();
            var context=Game.Player.Character.Position;WorkContext.ShopDelivery.Set(OwnerKey,new JobPoint{Name="gang supply",X=context.X,Y=context.Y,Z=context.Z},5);
            if(!Input)return;
            World.DrawMarker(MarkerType.Cylinder,Target-new Vector3(0,0,1),Vector3.Zero,Vector3.Zero,new Vector3(1.2f,1.2f,.5f),Color.FromArgb(130,60,200,110));
            GTA.UI.Screen.ShowSubtitle("Reposicao / carga "+Pending.Loaded.Count+"/3 / descarga "+Pending.Delivered+"/3 / END suspende",100);
            if(Game.Player.Character.IsOnFoot && Game.Player.Character.Position.DistanceTo(Target)<=3)GTA.UI.Screen.ShowHelpTextThisFrame(Instruction);
        }
        public void Interact()
        {
            if(!Active || !Input || !Owns || Pending==null || Pending.Id!=orderId || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=OwnerKey || !Game.Player.Character.IsOnFoot)return;
            var p=Pending;var character=Game.Player.Character;
            if(p.Ready)
            {
                if(character.Position.DistanceTo(Pos(p.Area))>3)return;
                if(Wallet.GangSupplyComplete(state(),CurrentArea,p.Id,out bool changed)){Suspend();Notify.Show(changed?"Frete entregue. Lote acrescentado ao estoque; sem nova cobranca.":"Frete ja confirmado; nenhum lote repetido.");}return;
            }
            if(!Car || !Stopped || character.Position.DistanceTo(vehicle.Position)>40)return;
            if(carrying)
            {
                if(picked>=0)
                {
                    if(character.Position.DistanceTo(Rear)>3 || vehicle.Position.DistanceTo(Pos(p.Options.Suppliers[picked]))>35)return;
                    if(!Wallet.GangSupplyLoad(p.Id,picked,out _))return;
                }
                else
                {
                    if(!arrived || character.Position.DistanceTo(Pos(p.Area))>3 || vehicle.Position.DistanceTo(Pos(p.Area))>30)return;
                    if(!Wallet.GangSupplyDeliver(p.Id,p.Delivered+1,out _))return;
                }
                cargo.Stop();carrying=false;picked=-1;Route();
                Notify.Show(Pending.Ready?"Tres caixas entregues. E no ponto confirma o lote no estoque.":Pending.Loaded.Count==3 && !arrived?"Carga completa. Dirija o mesmo veiculo ao territorio e estacione a ate 25m do ponto.":"Caixa confirmada. Siga o proximo ponto do GPS.");return;
            }
            if(p.Loaded.Count<3)
            {
                int index=Next;var point=Pos(p.Options.Suppliers[index]);
                if(character.Position.DistanceTo(point)>3 || vehicle.Position.DistanceTo(point)>35)return;
                picked=index;carrying=true;cargo.Start(character,false);Route();
            }
            else if(arrived && vehicle.Position.DistanceTo(Pos(p.Area))<=25 && character.Position.DistanceTo(Rear)<=3)
            {picked=-1;carrying=true;cargo.Start(character,false);Route();}
        }
        public void Suspend()
        {
            WorkContext.ShopDelivery.Clear(OwnerKey);cargo.Stop();if(objective!=null && objective.Exists())objective.Delete();objective=null;
            // This is a borrowed player vehicle. Never retire, delete, repair, replate or move it.
            if(reserved)JobVehicles.Release(vehicle);reserved=false;vehicle=null;Active=carrying=arrived=false;profile=orderId=plate=null;picked=-1;gate.Release(OwnerKey);
        }
    }
}
