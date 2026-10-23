using System;
using System.Drawing;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using LemonUI;
using LemonUI.Menus;

namespace RPCore
{
    public sealed class HuntingJob : ISystem,IWorkContract
    {
        public string Name=>"hunting";
        public string Label=>"Caca";
        public bool Active=>run!=null;
        public Vector3 StartPosition=>Pos(options.Vendor);
        public Vector3 ForestPosition=>Pos(options.Forest);
        readonly ObjectPool pool;readonly AchievementSystem achievements;readonly ShiftGate gate;readonly HuntingOptions options;readonly NativeMenu menu;readonly Blip vendorBlip,forestBlip;
        readonly Random random=new Random();
        HuntingRun run;Ped animal,skinOwner;Prop bait;Blip targetBlip;Vector3 baitPoint;HuntSpecies species;int cancelSince=-1,nextThink,fleeUntil,nextSave;
        const string SkinDict="amb@medic@standing@kneel@base",SkinAnim="base";bool animStarted,animRequested;
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        bool InputAllowed=>!Game.IsPaused && !InterfaceOptions.ModalOpen && !pool.AreAnyVisible;
        bool AtVendor=>!Game.Player.Character.IsInVehicle() && Game.Player.Character.Position.DistanceTo(StartPosition)<5;
        bool SafeFoot=>Game.Player.Character.IsHuman && !Game.Player.Character.IsInVehicle() && !Game.Player.Character.IsDead && !Game.Player.Character.IsRagdoll && !Game.Player.Character.IsInCombat;
        bool InForest=>Game.Player.Character.Position.DistanceTo2D(ForestPosition)<options.ZoneRadius;
        public HuntingJob(ObjectPool pool,AchievementSystem achievements,ShiftGate gate)
        {
            this.pool=pool;this.achievements=achievements;this.gate=gate;
            var defaults=HuntingDefaults.Load();options=Store.LoadDef("Jobs/Hunting.json",()=>defaults);
            if(options==null || !options.Valid){Log.Write("Invalid Hunting.json; embedded defaults used, original retained.");options=defaults;}
            menu=new NativeMenu("Caca","LICENCA / FLORESTA / BOLSA");pool.Add(menu);Visuals.Register(menu,Name);
            vendorBlip=MapBlips.Create(StartPosition,"RPCore: acougue / caca",BlipSprite.Beasted,BlipColor.Orange,.8f);
            forestBlip=MapBlips.Create(ForestPosition,"RPCore: area de caca",BlipSprite.Beasted,BlipColor.Green,.75f);
        }
        void Add(string title,string text,Action action){var item=new NativeItem(title,text);item.Activated+=(s,e)=>{try{action();}catch(Exception ex){Log.Error("hunting action",ex);Notify.Show("Acao nao confirmada. Veja RPCore.log.");}};menu.Add(item);}
        public void OpenMenu()
        {
            pool.HideAll();menu.Clear();var h=Wallet.Hunting;
            menu.Name="Caca / "+h.Harvests+" coletas / iscas "+h.Bait;
            Add("GPS: acougue / equipamento","Licenca, kit, iscas e venda da bolsa.",()=>{Waypoint(StartPosition);menu.Visible=false;});
            Add("GPS: floresta de caca","Use E a pe na area para colocar isca durante a expedicao.",()=>{Waypoint(ForestPosition);menu.Visible=false;});
            if(!h.Licensed)Add("Licenca + equipamento: $"+options.LicensePrice,"Credito bancario necessario. Inclui direito a faca e mosquete; retirar no acougue.",()=>{
                if(!VendorAction())return;bool changed;if(!Wallet.HuntLicense(options,Guid.NewGuid().ToString("N"),out changed)){Notify.Show("Saldo bancario insuficiente ou dados indisponiveis.");return;}GiveEquipment();OpenMenu();});
            else Add("Retirar equipamento licenciado","Fornece faca/mosquete e garante 20 municoes no mosquete.",()=>{if(VendorAction()){GiveEquipment();menu.Visible=false;}});
            Add("Comprar 5 iscas: $"+(5*options.BaitPrice),"Debita do banco; requer licenca e presenca no acougue.",()=>{if(!VendorAction())return;bool changed;if(!Wallet.HuntBait(options,5,Guid.NewGuid().ToString("N"),out changed))Notify.Show("Licenca, saldo ou limite de iscas insuficiente.");OpenMenu();});
            Add("Bolsa de caca: "+HuntingLoot.Weight(h,options).ToString("0.0")+" / "+options.BagCapacity+" kg","Materiais persistentes deste perfil. Abra para ver e vender no acougue.",OpenBag);
            if(!Active)Add("Iniciar expedicao",options.AnimalsPerTrip+" coletas por expedicao; requer licenca e iscas.",Begin);
            else Add("Encerrar expedicao","Mantem coletas ja salvas; retira apenas animal/isca do contrato.",()=>End("Expedicao encerrada. Bolsa preservada."));
            menu.Visible=true;
        }
        public void OpenBag()
        {
            pool.HideAll();menu.Clear();var h=Wallet.Hunting;menu.Name="Bolsa / "+HuntingLoot.Weight(h,options).ToString("0.0")+" kg";
            foreach(var p in h.Bag)if(p.Value>0){var material=options.Materials.Find(m=>m.Id==p.Key);Add((material?.Name??p.Key)+" x"+p.Value,material==null?"Material desconhecido preservado.":"Preco de venda por unidade: $"+material.Price,()=>{});}
            int quote=HuntingTransactions.Quote(h,options);
            Add("Vender materiais conhecidos: $"+Math.Max(0,quote),"No acougue, credita o banco e retira materiais no mesmo checkpoint.",()=>{
                if(!VendorAction())return;bool changed;if(!Wallet.HuntSell(options,Guid.NewGuid().ToString("N"),out changed)){Notify.Show("Sem materiais vendaveis ou limite de saldo/dados.");return;}
                if(changed){Notify.Show("~g~+$"+quote+" no banco~s~ / venda de caca");Bump("hunting_sales");Bump("cash_earned",quote);}OpenBag();});
            Add("Voltar","Menu de caca.",OpenMenu);menu.Visible=true;
        }
        bool VendorAction(){if(!AtVendor || Game.IsMissionActive || !SafeFoot){Notify.Show("Va a pe ao acougue, fora de missao/combate.");return false;}return true;}
        void GiveEquipment()
        {
            if(!Wallet.Hunting.Licensed)return;var weapons=Game.Player.Character.Weapons;
            if(!weapons.HasWeapon(WeaponHash.Knife))weapons.Give(WeaponHash.Knife,1,false,true);
            if(!weapons.HasWeapon(WeaponHash.Musket))weapons.Give(WeaponHash.Musket,20,false,true);
            int ammo=weapons[WeaponHash.Musket].Ammo;if(ammo<20)Function.Call(Hash.ADD_AMMO_TO_PED,Game.Player.Character,(uint)WeaponHash.Musket,20-ammo);
            Notify.Show("Equipamento disponivel. Mosquete com pelo menos 20 municoes; outras armas preservadas.");
        }
        void Begin()
        {
            if(Active)return;
            if(!Wallet.Hunting.Licensed || Wallet.Hunting.Bait<1 || !SafeFoot || Game.IsMissionActive || Game.Player.Wanted.WantedLevel>0){Notify.Show("Requer licenca, isca e personagem a pe, sem procurado/missao/combate.");return;}
            if(!gate.Acquire(Name)){Notify.Show("Encerre o trabalho de "+gate.Owner+" antes de iniciar outro.");return;}
            run=new HuntingRun(options.AnimalsPerTrip,options.HarvestSeconds);cancelSince=-1;nextSave=0;menu.Visible=false;Waypoint(ForestPosition);Notify.Show("Va a floresta verde. A pe, E coloca isca e inicia o rastreamento.");
        }
        static void Waypoint(Vector3 p)=>Function.Call(Hash.SET_NEW_WAYPOINT,p.X,p.Y);
        bool SpawnAnimal()
        {
            species=(HuntSpecies)random.Next(3);var model=new Model(species==HuntSpecies.Deer?"a_c_deer":species==HuntSpecies.Boar?"a_c_boar":"a_c_rabbit_01");
            try
            {
                if(!model.Request(2500))return false;
                for(int i=0;i<14;i++)
                {
                    double angle=random.NextDouble()*Math.PI*2;float radius=50+random.Next(25),x=baitPoint.X+(float)Math.Cos(angle)*radius,y=baitPoint.Y+(float)Math.Sin(angle)*radius;
                    float ground;if(!World.GetGroundHeight(new Vector3(x,y,baitPoint.Z+100),out ground) || Math.Abs(ground-baitPoint.Z)>45)continue;
                    var water=new OutputArgument();if(Function.Call<bool>(Hash.GET_WATER_HEIGHT_NO_WAVES,x,y,ground+20,water) && water.GetResult<float>()>ground)continue;
                    var point=new JobPoint{Name="wildlife",X=x,Y=y,Z=ground+1,Heading=random.Next(360)};
                    if(!RigSpawn.ClearForTeleport(point,model,Game.Player.Character,Game.Player.Character))continue;
                    animal=World.CreatePed(model,Pos(point),point.Heading);
                    if(animal==null || !animal.Exists())continue;
                    animal.IsPersistent=true;animal.BlockPermanentEvents=true;nextThink=0;fleeUntil=0;return true;
                }
                return false;
            }
            finally{model.MarkAsNoLongerNeeded();}
        }
        void PlaceBait()
        {
            if(!SafeFoot || !InForest || Wallet.Hunting.Bait<1){Notify.Show("Requer uma isca e estar a pe na area de caca.");return;}
            var p=Game.Player.Character;var front=p.Position+p.ForwardVector*2;float ground;
            if(!World.GetGroundHeight(front+new Vector3(0,0,5),out ground)){Notify.Show("Chao indisponivel para isca.");return;}
            baitPoint=new Vector3(front.X,front.Y,ground);
            if(!SpawnAnimal()){CleanupAnimal();Notify.Show("Sem posicao livre para animal. Isca nao consumida; tente outro ponto da floresta.");return;}
            try
            {
                bool changed;if(!Wallet.HuntUseBait(Guid.NewGuid().ToString("N"),out changed)){CleanupAnimal();Notify.Show("Isca/dados indisponiveis.");return;}
            }
            catch(Exception ex){CleanupAnimal();Log.Error("hunting bait checkpoint",ex);Notify.Show("Falha ao salvar isca. Nada consumido; tente novamente.");return;}
            run.Track();targetBlip=World.CreateBlip(animal.Position);targetBlip.Name="Caca: rastrear animal";targetBlip.Color=BlipColor.Orange;targetBlip.Scale=.7f;
            var model=new Model("prop_cs_steak");try{if(model.IsValid && model.IsInCdImage && model.Request(300)){bait=World.CreateProp(model,baitPoint,false,true);if(bait!=null){bait.IsPersistent=true;bait.IsCollisionEnabled=false;}}}catch(Exception ex){Log.Error("hunting cosmetic bait",ex);}finally{model.MarkAsNoLongerNeeded();}
            Notify.Show("Isca colocada. Afaste-se e observe o animal laranja. Chegar perto ou atirar assusta.");
        }
        public void Tick()
        {
            if(!Active)
            {
                if(InputAllowed && SafeFoot && Game.Player.Character.Position.DistanceTo(StartPosition)<3){Prompt.Help("~INPUT_CONTEXT~: acougue / caca / bolsa");if(Game.IsControlJustPressed(GTA.Control.Context))OpenMenu();}return;
            }
            if(Game.Player.Character.IsDead || Game.IsMissionActive){End("Expedicao encerrada. Coletas ja salvas preservadas.");return;}
            if(!InputAllowed){StopAnimation();run.Interrupt();return;}
            var player=Game.Player.Character;
            if(run.Stage==HuntingStage.Ready)
            {
                Status(InForest?"E: colocar isca / iscas "+Wallet.Hunting.Bait:"siga o GPS ate a floresta");
                if(InForest && SafeFoot){Prompt.Help("~INPUT_CONTEXT~: colocar uma isca");if(Game.IsControlJustPressed(GTA.Control.Context))PlaceBait();}return;
            }
            if(animal==null || !animal.Exists()){CleanupAnimal();run.LostAnimal();Notify.Show("Animal perdido. Use outra isca.");return;}
            if(targetBlip!=null && targetBlip.Exists())targetBlip.Position=animal.Position;
            if(run.Stage==HuntingStage.Tracking)
            {
                if(run.FoundCarcass(animal.IsDead)){Notify.Show("Aproxime-se da carcaca com a faca disponivel e pressione E.");return;}
                if(animal.Position.DistanceTo(baitPoint)>200 || player.Position.DistanceTo(baitPoint)>250){CleanupAnimal();run.LostAnimal();Notify.Show("Animal/area abandonados. Isca consumida; nova tentativa disponivel.");return;}
                Status("rastreie o animal laranja / "+species);
                if(unchecked(Game.GameTime-nextThink)>=0)
                {
                    nextThink=Game.GameTime+1500;
                    if(player.IsShooting || player.Position.DistanceTo(animal.Position)<14){Function.Call(Hash.TASK_SMART_FLEE_PED,animal,player,120f,20000,false,false);fleeUntil=Game.GameTime+12000;}
                    else if(unchecked(Game.GameTime-fleeUntil)>=0){JobActors.Walk(animal,baitPoint);}
                }
                return;
            }
            bool eligible=SafeFoot && animal.IsDead && player.Position.DistanceTo(animal.Position)<3 && !player.IsShooting && player.Weapons.HasWeapon(WeaponHash.Knife);
            if(run.Stage==HuntingStage.Carcass)
            {
                Status("aproxime-se e colete com E");
                if(eligible){Prompt.Help("~INPUT_CONTEXT~: coletar animal ("+options.HarvestSeconds+"s)");if(Game.IsControlJustPressed(GTA.Control.Context)){player.Weapons.Select(WeaponHash.Knife);Function.Call(Hash.REQUEST_ANIM_DICT,SkinDict);animRequested=true;run.BeginHarvest(Game.GameTime,true);}}
            }
            else if(run.Stage==HuntingStage.Harvesting)
            {
                if(!eligible || player.Weapons.Current.Hash!=WeaponHash.Knife){StopAnimation();run.Interrupt();return;}
                if(!animStarted && Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED,SkinDict)){Function.Call(Hash.TASK_PLAY_ANIM,player,SkinDict,SkinAnim,4f,-4f,-1,49,0f,false,false,false);skinOwner=player;animStarted=true;}
                Status("coletando... permaneça junto a carcaca");
                if(run.Commit(Game.GameTime,true,Collect)){StopAnimation();CleanupAnimal();if(run.Stage==HuntingStage.Finished)End("Expedicao concluida. Venda sua bolsa no acougue.");else Notify.Show("Coleta salva. Proxima isca disponivel.");}
            }
        }
        int Quality()
        {
            uint cause=Function.Call<uint>(Hash.GET_PED_CAUSE_OF_DEATH,animal);
            if(cause==(uint)WeaponHash.Musket || cause==(uint)WeaponHash.SniperRifle || cause==(uint)WeaponHash.MarksmanRifle || cause==(uint)WeaponHash.Knife)return 3;
            if(cause==(uint)WeaponHash.Pistol || cause==(uint)WeaponHash.CombatPistol)return 2;
            // Vehicle impacts, explosions and fire spoil the carcass; these are GTA native cause hashes.
            if(cause==0xA36D413E || cause==0x07FC7D7A || cause==0x2024F4E8 || cause==0xDF8E89EB)return 0;
            return 1;
        }
        bool Collect(int step,bool complete)
        {
            if(unchecked(Game.GameTime-nextSave)<0)return false;
            try
            {
                bool changed;var loot=HuntingLoot.Create(species,Quality());
                if(!Wallet.HuntHarvest(options,loot,run.Id+":harvest:"+step,complete,out changed)){nextSave=Game.GameTime+3000;Notify.Show("Bolsa cheia ou dados no limite. Venda materiais e tente novamente.");return false;}
                if(changed){Notify.Show(loot.Count==0?"Carcaca danificada: sem materiais aproveitaveis.":"Materiais adicionados a bolsa de caca.");Bump("hunting_harvests");if(complete)Bump("hunting_trips");}return true;
            }
            catch(Exception ex){nextSave=Game.GameTime+5000;Log.Error("hunting harvest checkpoint",ex);Notify.Show("Falha ao salvar coleta. Permaneça junto da carcaca para repetir.");return false;}
        }
        void Bump(string stat,int amount=1){try{achievements.Bump(stat,amount);achievements.Save();}catch(Exception ex){Log.Error("hunting achievement after checkpoint",ex);}}
        void StopAnimation(){if(animStarted && skinOwner!=null && skinOwner.Exists())Function.Call(Hash.STOP_ANIM_TASK,skinOwner,SkinDict,SkinAnim,2f);animStarted=false;skinOwner=null;if(animRequested){Function.Call(Hash.REMOVE_ANIM_DICT,SkinDict);animRequested=false;}}
        void CleanupAnimal(){StopAnimation();if(animal!=null && animal.Exists())animal.Delete();animal=null;if(bait!=null && bait.Exists())bait.Delete();bait=null;if(targetBlip!=null && targetBlip.Exists())targetBlip.Delete();targetBlip=null;}
        void End(string text){run?.Cancel();try{CleanupAnimal();}finally{run=null;gate.Release(Name);menu.Visible=false;}Notify.Show(text);}
        void Status(string text){string label="Caca "+run.Collected+"/"+run.Total+" / "+text;if(!Visuals.TryJobHeader(Name,label,Color.White))new TextElement(label,new PointF(GTA.UI.Screen.ScaledWidth/2,20),.36f,Color.White,GTA.UI.Font.ChaletLondon,Alignment.Center).Draw();}
        public void PollCancel(){if(!Active || !InputAllowed || !Game.IsKeyPressed(Keys.End)){cancelSince=-1;return;}if(cancelSince<0)cancelSince=Game.GameTime;if(unchecked(Game.GameTime-cancelSince)>=1000)End("Expedicao cancelada. Bolsa mantida.");}
        public void OnKey(KeyEventArgs e){}
        public void Reload(){if(Active)End("Expedicao encerrada na troca de perfil.");menu.Visible=false;}
        public void Save(){}
        public void Dispose(){try{CleanupAnimal();}finally{run=null;gate.Release(Name);menu.Visible=false;if(vendorBlip!=null && vendorBlip.Exists())vendorBlip.Delete();if(forestBlip!=null && forestBlip.Exists())forestBlip.Delete();}}
    }
}
