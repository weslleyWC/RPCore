using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
namespace RPCore
{
    internal sealed class GangPopUp
    {
        const string Owner="gang_pop_up",Decor="RPCore_PopUpOwner";
        readonly ShiftGate gate;readonly ObjectPool pool;readonly Func<GangState> state;readonly Func<IEnumerable<GangDefinition>> gangs;readonly Func<IEnumerable<GangTurfDefinition>> areas;readonly GangPopUpOptions options;readonly Random random=new Random();
        readonly List<Prop> props=new List<Prop>();readonly HashSet<int> decorDone=new HashSet<int>();readonly Dictionary<int,Model> decorModels=new Dictionary<int,Model>();
        Ped customer,animOwner;Model model;Blip route,customerBlip;GangPopUpCheckout checkout;string profile,id,orderId,feedback;int player,token,last,requestAt;long elapsed,nextCustomer,pathAt,spawnAt,feedbackUntil;bool ready,render,played;public bool Active{get;private set;}
        GangPopUpAttempt Pending=>Wallet.GangPopUp?.Pending;GangTurfDefinition Current=>areas().FirstOrDefault(a=>a.Id==7);
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        static bool Tagged(Entity e,int value)=>e!=null && e.Exists() && e.Handle!=Game.Player.Character.Handle && Function.Call<bool>(Hash.DECOR_EXIST_ON,e,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,e,Decor)==value;
        bool Mark(Entity e){try{if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);return Function.Call<bool>(Hash.DECOR_SET_INT,e,Decor,token);}catch{return false;}}
        bool Safe=>Game.Player.Character!=null && Game.Player.Character.Exists() && Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && Game.Player.Wanted.WantedLevel==0 && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);
        bool Input=>Safe && !Game.IsPaused && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen;
        bool Owns=>Pending!=null && GangBusinessTransactions.Owns(state(),Current) && GangCrewRules.SameGang(Pending.Gang,state().Gang) && gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang));
        bool At(GangPopUpOptions c)=>Safe && !Game.IsPaused && !InterfaceOptions.ModalOpen && Game.Player.Character.IsOnFoot && Game.Player.Character.Position.DistanceTo(Pos(c.Shop))<=c.StartRadius;
        bool Serving=>Input && Game.Player.Character.IsOnFoot && Tagged(customer,token) && customer.IsAlive && customer.IsOnFoot && ready && customer.Position.DistanceTo(Pos(Pending.Options.CustomerSpot))<=2.5f && Game.Player.Character.Position.DistanceTo(customer.Position)<=4.5f;
        public GangPopUp(ShiftGate gate,ObjectPool pool,Func<GangState> state,Func<IEnumerable<GangDefinition>> gangs,Func<IEnumerable<GangTurfDefinition>> areas)
        {this.gate=gate;this.pool=pool;this.state=state;this.gangs=gangs;this.areas=areas;options=Store.LoadDef("Gangs/PopUpShop.json",()=>new GangPopUpOptions());if(options?.Valid!=true){options=new GangPopUpOptions();Log.Write("Invalid pop-up shop options; defaults in memory, original retained.");}}
        public void Controls(Action<string,string,Action> add,Action reopen)
        {
            var p=Pending;if(p==null)return;
            add("Loja temporaria pendente / "+p.Sales+"/"+p.Options.Goal,"Receita confirmada neste contrato: $"+p.Revenue+" sujos na area7. Pedido atual permanece salvo; selecao/barra reiniciam ao retomar.",null);
            add(Active?"Suspender loja temporaria":"Retomar loja temporaria",p.Ready?"Meta concluida salva. A pe na banca, retome e E confirma bonus.":"A pe na banca com posse. Vendas/receita anteriores conservadas; mesmo pedido/cupom/alvo do cliente atual.",()=>{if(Active){Suspend();reopen();}else if(!Resume())Notify.Show("Confira banca a pe, posse, seguranca, atividade/contratos e dados.");});
            add("GPS: banca pendente","Local salvo do contrato.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,p.Options.Shop.X,p.Options.Shop.Y);pool.HideAll();});
            add("Cancelar contrato da loja","Gratis, sem estorno de vendas. Receita suja ja confirmada permanece; nenhum bonus parcial. Pode cancelar sem posse/filiacao em seguranca.",()=>{if(!Safe || Game.IsPaused || InterfaceOptions.ModalOpen || gate.Owner!=null && gate.Owner!=Owner || !Wallet.GangPopUpCancel(p.Id,out _)){Notify.Show("Cancelamento nao confirmado; contrato conservado.");return;}Suspend();reopen();});
        }
        public void Append(GangTurfDefinition area,Action<string,string,Action> add)
        {
            if(area.Id!=7)return;var a=GangPopUpTransactions.Account(Wallet.GangPopUp,state().Gang);var utc=DateTime.UtcNow;string status=GangPopUpTransactions.Multiplier(Wallet.GangPopUp,state().Gang,7,utc)==2?"ATIVO / "+Math.Ceiling((a.ExpiresUtcTicks-utc.Ticks)/(double)TimeSpan.TicksPerMinute)+"min":"inativo";
            add("Loja temporaria / bonus2X: "+status,"Contratos: "+(a?.Contracts??0)+" / vendas: "+(a?.Sales??0)+" / receita: $"+(a?.Revenue??0)+" sujos da area7. Nao e dinheiro pessoal.",null);
            add("GPS: loja temporaria Legion","A pe na banca e ponto do cliente livre.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,options.Shop.X,options.Shop.Y);pool.HideAll();});
            add("Abrir loja / "+options.Goal+" vendas","Clientes chegam a pe. E atende, setas esquerda/direita escolhem peca e cupom, E confirma, depois acerte E na faixa verde. Venda gera receita suja; meta libera2X/"+options.BoostMinutes+"min.",()=>{if(!Begin(area))Notify.Show("Confira posse da area7, banca a pe, seguranca, espaco da receita suja e atividade/pendencias.");});
        }
        bool Begin(GangTurfDefinition area)
        {if(Active || gate.Owner!=null || !At(options) || !gangs().Any(g=>GangCrewRules.SameGang(g.Name,state().Gang)) || !gate.Acquire(Owner))return false;try{if(!Wallet.GangPopUpBegin(state(),area,options,Guid.NewGuid().ToString("N"),out bool changed) || !changed){Suspend();return false;}Activate();return true;}catch{Suspend();throw;}}
        bool Resume(){if(Active || gate.Owner!=null || Wallet.GangPopUp?.Valid!=true || !Owns || !Wallet.GangPopUpOtherFree || !At(Pending.Options) || !gate.Acquire(Owner))return false;Activate();return true;}
        void Activate(){Active=true;profile=Profile.Id;id=Pending.Id;player=Game.Player.Character.Handle;token=random.Next(1,int.MaxValue);last=Game.GameTime;elapsed=0;nextCustomer=3000;render=Visuals.Enabled && Visuals.UseProps;pool.HideAll();route=MapBlips.Create(Pos(Pending.Options.Shop),"RPCore: loja temporaria",BlipSprite.Store,BlipColor.Yellow,.8f);route.ShowRoute=true;Notify.Show("Loja aberta. E inicia atendimento; setas escolhem, E confirma peca/cupom e para barra. END suspende.");}
        void Decorate()
        {
            if(!render)return;var list=Pending.Options.Props;
            for(int i=0;i<list.Count;i++)
            {
                if(decorDone.Contains(i))continue;var d=list[i];var pos=Pos(d.Point);
                if(World.GetNearbyProps(pos,2).Any(p=>p.Exists() && p.Model.Hash==d.Model && !Tagged(p,token) && p.Position.DistanceTo2D(pos)<.4f)){decorDone.Add(i);continue;}
                if(!decorModels.TryGetValue(i,out var m)){m=new Model(d.Model);if(!m.IsInCdImage){decorDone.Add(i);Log.Write("Pop-up prop unavailable: "+d.Model);continue;}m.Request();decorModels[i]=m;}
                if(!m.IsLoaded){if(elapsed>10000){m.MarkAsNoLongerNeeded();decorModels.Remove(i);decorDone.Add(i);}continue;}
                if(Game.Player.Character.Position.DistanceTo(pos)<1.2f || World.GetNearbyPeds(pos,1).Any(p=>p.Handle!=player))continue;
                var prop=World.CreateProp(m,pos,false,true);if(prop==null || !prop.Exists())continue;if(!Mark(prop)){prop.MarkAsNoLongerNeeded();decorDone.Add(i);continue;}
                prop.Rotation=new Vector3(d.Pitch,d.Roll,d.Yaw);prop.IsPersistent=true;prop.IsPositionFrozen=true;prop.IsCollisionEnabled=false;props.Add(prop);m.MarkAsNoLongerNeeded();decorModels.Remove(i);decorDone.Add(i);
            }
        }
        void NewOrder()
        {
            var c=Pending.Options;var models=c.Customers.Where(n=>new Model(n).IsInCdImage && new Model(n).IsPed).ToArray();if(models.Length==0){Suspend();Notify.Show("Nenhum modelo de cliente disponivel; contrato conservado.");return;}
            var o=new GangPopUpOrder{Id=Guid.NewGuid().ToString("N"),Model=models[random.Next(models.Length)],Item=random.Next(c.Products.Count),Discount=random.Next(4)*5,Target=.2f+(float)random.NextDouble()*.6f};
            if(!Wallet.GangPopUpCustomer(state(),Current,id,o,out bool saved) || !saved){Suspend();Notify.Show("Pedido nao gravado; contrato conservado.");}
        }
        void Spawn()
        {
            var o=Pending.Customer;if(o==null)return;
            if(model==null){model=new Model(o.Model);if(!model.IsInCdImage || !model.IsPed){Suspend();Notify.Show("Modelo salvo indisponivel; contrato conservado.");return;}model.Request();requestAt=Game.GameTime;}
            if(!model.IsLoaded){if(unchecked((uint)(Game.GameTime-requestAt))>10000){Suspend();Notify.Show("Cliente nao carregou; contrato conservado.");}return;}
            if(elapsed<nextCustomer)return;nextCustomer=elapsed+3000;double angle=random.NextDouble()*Math.PI*2;var center=Pos(Pending.Options.Shop);var around=center+new Vector3((float)Math.Cos(angle)*50,(float)Math.Sin(angle)*50,0);var road=World.GetNextPositionOnStreet(around,false);var point=JobActors.Sidewalk(road);
            if(point==Vector3.Zero || point.DistanceTo(center)<15 || point.DistanceTo(center)>80 || World.GetNearbyPeds(point,2).Length!=0 || World.GetNearbyVehicles(point,2).Length!=0)return;
            customer=World.CreatePed(model,point);if(customer==null || !customer.Exists()){customer=null;return;}if(!Mark(customer)){customer.MarkAsNoLongerNeeded();customer=null;Suspend();return;}
            customer.IsPersistent=true;customer.BlockPermanentEvents=true;customer.Weapons.RemoveAll();customer.RelationshipGroup=new RelationshipGroup((int)RelationshipGroupHash.CivilianMale);Function.Call(Hash.SET_PED_MONEY,customer,0);JobActors.Walk(customer,Pos(Pending.Options.CustomerSpot));spawnAt=elapsed;pathAt=elapsed+5000;orderId=o.Id;ready=false;model.MarkAsNoLongerNeeded();model=null;
            customerBlip=MapBlips.Create(customer.Position,"RPCore: cliente de roupas",BlipSprite.Standard,BlipColor.Green,.7f);
        }
        void ClearCustomer(bool depart)
        {
            if(played && animOwner!=null && animOwner.Exists())Function.Call(Hash.STOP_ANIM_TASK,animOwner,"mp_common","givetake1_a",2f);played=false;animOwner=null;
            if(customerBlip!=null && customerBlip.Exists())customerBlip.Delete();customerBlip=null;
            if(Tagged(customer,token)){if(depart && customer.IsAlive && customer.IsOnFoot){customer.BlockPermanentEvents=false;customer.Task.Wander();customer.MarkAsNoLongerNeeded();}else if(customer.IsInVehicle())customer.MarkAsNoLongerNeeded();else customer.Delete();}customer=null;checkout=null;ready=false;orderId=null;if(model!=null)model.MarkAsNoLongerNeeded();model=null;
        }
        void Display()
        {
            var o=Pending.Customer;var c=Pending.Options;string order="Pedido: "+c.Products[o.Item].Name+" / $"+c.Products[o.Item].Price+" / cupom "+o.Discount+"% / liquido $"+GangPopUpTransactions.Quote(c,o);
            GTA.UI.Screen.ShowSubtitle("Loja / "+Pending.Sales+"/"+c.Goal+" / receita suja $"+Pending.Revenue+"~n~"+order,100);
            if(checkout==null){GTA.UI.Screen.ShowHelpTextThisFrame(ready?"E perto do cliente: iniciar atendimento":"Cliente a caminho do balcao.");return;}
            if(checkout.Locked){GTA.UI.Screen.ShowHelpTextThisFrame(feedback??"Atendimento confirmado.");return;}
            if(checkout.Step==0)GTA.UI.Screen.ShowHelpTextThisFrame("Setas esquerda/direita: peca ["+c.Products[checkout.Item].Name+"] / E confirma");
            else if(checkout.Step==1)GTA.UI.Screen.ShowHelpTextThisFrame("Setas esquerda/direita: desconto ["+checkout.Discount+"%] / E confirma cupom");
            else{const float left=.70f,width=.24f,y=.80f;Function.Call(Hash.DRAW_RECT,left+width/2,y,width,.025f,35,38,42,230);Function.Call(Hash.DRAW_RECT,left+width*o.Target,y,width*.18f,.025f,60,200,110,255);Function.Call(Hash.DRAW_RECT,left+width*(float)checkout.Cursor,y,.0035f,.035f,255,255,255,255);GTA.UI.Screen.ShowHelpTextThisFrame("E: pare o cursor na faixa verde para concluir venda");}
            if(elapsed<feedbackUntil && !string.IsNullOrWhiteSpace(feedback))GTA.UI.Screen.ShowSubtitle(order+"~n~"+feedback,100);
        }
        public void Tick()
        {
            if(!Active)return;
            if(Wallet.GangPopUp?.Valid!=true || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner || !Safe || !Owns || !Wallet.GangPopUpOtherFree || Game.Player.Character.Position.DistanceTo(Pos(Pending.Options.Shop))>35){Suspend();Notify.Show("Loja suspensa. Vendas e receita confirmadas conservadas; F4 retoma ou cancela.");return;}
            uint d=unchecked((uint)(Game.GameTime-last));last=Game.GameTime;if(!Input){checkout?.Tick(Game.GameTime,false);return;}if(d<=2000)elapsed+=d;Decorate();var p=Pending;
            if(checkout?.Locked==true){checkout.Tick(Game.GameTime,false);GTA.UI.Screen.ShowHelpTextThisFrame(feedback??"Atendimento confirmado.");if(elapsed>=feedbackUntil){ClearCustomer(true);nextCustomer=elapsed+random.Next(4000,7000);}return;}
            if(p.Ready){if(At(p.Options))GTA.UI.Screen.ShowHelpTextThisFrame("E: confirmar meta de vendas e liberar bonus2X");return;}
            if(p.Customer==null){if(elapsed>=nextCustomer)NewOrder();return;}
            if(!Tagged(customer,token)){Spawn();if(Active)GTA.UI.Screen.ShowHelpTextThisFrame("Aguardando cliente / mantenha o acesso ao balcao livre.");return;}
            if(!customer.IsAlive || !customer.IsOnFoot){Suspend();Notify.Show("Cliente indisponivel; pedido e vendas conservados.");return;}
            if(customerBlip!=null && customerBlip.Exists())customerBlip.Position=customer.Position;
            float distance=customer.Position.DistanceTo(Pos(p.Options.CustomerSpot));if(!ready && distance<=1.25f){Function.Call(Hash.CLEAR_PED_TASKS,customer);Function.Call(Hash.TASK_TURN_PED_TO_FACE_ENTITY,customer,Game.Player.Character,700);ready=true;}
            if(ready && distance>2.5f){ready=false;checkout=null;pathAt=0;}
            if(!ready){if(elapsed-spawnAt>p.Options.ArrivalSeconds*1000){Suspend();Notify.Show("Cliente nao conseguiu chegar; pedido salvo, retome com acesso livre.");return;}if(elapsed>=pathAt){JobActors.Walk(customer,Pos(p.Options.CustomerSpot));pathAt=elapsed+5000;}}
            bool working=Serving;checkout?.Tick(Game.GameTime,working);if(working && checkout!=null && checkout.Step<2){if(Game.IsKeyPressed(System.Windows.Forms.Keys.Right))checkout.Move(1,p.Options.Products.Count);else if(Game.IsKeyPressed(System.Windows.Forms.Keys.Left))checkout.Move(-1,p.Options.Products.Count);}Display();
        }
        public void Interact()
        {
            if(!Active || !Input || !Owns || !Wallet.GangPopUpOtherFree || Pending?.Id!=id || profile!=Profile.Id || player!=Game.Player.Character.Handle || gate.Owner!=Owner)return;
            if(Pending.Ready){var c=Pending.Options;if(At(c) && Wallet.GangPopUpComplete(state(),Current,id,DateTime.UtcNow,out bool saved)){Suspend();Notify.Show(saved?"Loja concluida! Automatico2X na area7 por"+c.BoostMinutes+"min reais. Receita suja permanece na area; ative automatico separadamente.":"Contrato ja confirmado; bonus nao repetido.");}return;}
            if(!Serving || Pending.Customer?.Id!=orderId)return;
            if(checkout==null){checkout=new GangPopUpCheckout(Game.GameTime);Function.Call(Hash.REQUEST_ANIM_DICT,"mp_common");return;}
            checkout.Tick(Game.GameTime,true);if(!checkout.Confirm(Pending.Customer,Pending.Options.Products.Count,out string message)){if(!string.IsNullOrEmpty(message)){feedback=message;feedbackUntil=elapsed+1200;}return;}
            if(!checkout.Locked)return;
            string customerId=Pending.Customer.Id;int value=GangPopUpTransactions.Quote(Pending.Options,Pending.Customer);
            if(checkout.Success)
            {
                if(!Wallet.GangPopUpSale(state(),Current,id,customerId,out bool saved) || !saved){Suspend();Notify.Show("Venda nao confirmada; nenhum debito/credito parcial.");return;}
                feedback="Venda confirmada / +$"+value+" de receita suja na area7.";
                if(Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED,"mp_common")){animOwner=Game.Player.Character;Function.Call(Hash.TASK_PLAY_ANIM,animOwner,"mp_common","givetake1_a",4f,-4f,1500,48,0f,false,false,false);Function.Call(Hash.TASK_PLAY_ANIM,customer,"mp_common","givetake1_b",4f,-4f,1500,48,0f,false,false,false);played=true;}
            }
            else{if(!Wallet.GangPopUpFail(id,customerId,out _)){Suspend();return;}feedback="Pagamento nao confirmado: cursor fora da faixa. Nenhuma venda ou receita; proximo cliente.";}
            feedbackUntil=elapsed+1600;Notify.Show(feedback);
        }
        public void Suspend(){ClearCustomer(false);foreach(var p in props)if(Tagged(p,token))p.Delete();props.Clear();foreach(var m in decorModels.Values)if(m!=null)m.MarkAsNoLongerNeeded();decorModels.Clear();decorDone.Clear();if(route!=null && route.Exists())route.Delete();route=null;Active=false;profile=id=null;feedback=null;gate.Release(Owner);}
    }
}
