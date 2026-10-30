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
    public class StorageSystem : ISystem
    {
        public string Name=>"storage";
        readonly InventorySystem inventory;readonly AchievementSystem ach;readonly ObjectPool pool;readonly ShiftGate gate;
        readonly NativeMenu menu=new NativeMenu("Armazenamento","");readonly PersonalServiceMenu ui;
        readonly List<StorageLocation> locations;readonly List<Blip> blips=new List<Blip>();
        Vehicle vehicle;string container,privateLabel;Vector3 origin;float capacity;bool dirty,gridOpen,closing,lease;
        Func<bool> privateAccess;Action privateClosed;StorageIdentity identity;bool depositPage=true;
        public StorageSystem(ObjectPool pool,InventorySystem inventory,AchievementSystem ach,ShiftGate gate)
        {
            this.pool=pool;this.inventory=inventory;this.ach=ach;this.gate=gate;pool.Add(menu);Visuals.Register(menu,"storage");
            ui=new PersonalServiceMenu(menu,ex=>{CloseCurrent();Log.Error("storage menu",ex);Notify.Show("Armazenamento interrompido; confira o log. Itens preservados.");});
            locations=Store.LoadDef("Storage/Locations.json",()=>new List<StorageLocation>{new StorageLocation{Id="franklin_house",Name="Strawberry stash",X=-14.1f,Y=-1440.2f,Z=31.1f}})??new List<StorageLocation>();
            foreach(var stash in locations.Where(s=>s?.Valid==true))blips.Add(MapBlips.Create(new Vector3(stash.X,stash.Y,stash.Z),"RPCore Storage: "+stash.Name,BlipSprite.Package,BlipColor.Blue,.7f));
        }
        static Vector3 Boot(Vehicle car)
        {int bone=Function.Call<int>(Hash.GET_ENTITY_BONE_INDEX_BY_NAME,car,"boot");return bone>=0?Function.Call<Vector3>(Hash.GET_WORLD_POSITION_OF_ENTITY_BONE,car,bone):car.GetOffsetPosition(new Vector3(0,-2.5f,0));}
        string VehicleKey(Vehicle car)=>StorageRules.TrunkKey(car.Model.Hash,Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT,car));
        bool Safe=>PersonalServiceMenu.SafeFoot;
        public void OnKey(KeyEventArgs e)
        {
            if(e.KeyCode!=Cfg.StorageKey || e.Modifiers!=Keys.None)return;
            if(menu.Visible || gridOpen){CloseCurrent();return;}
            if(pool.AreAnyVisible || InterfaceOptions.ModalOpen || !Safe || inventory.UsingItem)return;
            if(gate.Owner!=null){Notify.Show("Encerre a atividade de "+gate.Owner+" antes de abrir o deposito.");return;}
            CloseCurrent();var p=Game.Player.Character;
            var stash=locations.Where(s=>s?.Valid==true).OrderBy(s=>p.Position.DistanceTo(new Vector3(s.X,s.Y,s.Z))).FirstOrDefault(s=>p.Position.DistanceTo(new Vector3(s.X,s.Y,s.Z))<3 && Math.Abs(p.Position.Z-s.Z)<2);
            if(stash!=null){privateLabel="Deposito / "+stash.Name;container="stash:"+stash.Id;capacity=stash.Capacity;origin=new Vector3(stash.X,stash.Y,stash.Z);}
            else{
                vehicle=World.GetNearbyVehicles(p,6).Where(v=>v.Exists() && !v.IsDead && v.Model.IsCar && v.Speed<1 && p.Position.DistanceTo(Boot(v))<3).OrderBy(v=>p.Position.DistanceTo(Boot(v))).FirstOrDefault();
                if(vehicle==null){Notify.Show("Aproxime-se de um porta-malas ou deposito marcado no mapa.");return;}
                if(JobVehicles.Contains(vehicle)){vehicle=null;Notify.Show("Veiculo de trabalho: use seu veiculo pessoal para guardar itens.");return;}
                if(Function.Call<int>(Hash.GET_VEHICLE_DOOR_LOCK_STATUS,vehicle)>1){vehicle=null;Notify.Show("Veiculo trancado.");return;}
                container=VehicleKey(vehicle);if(container==null){vehicle=null;Notify.Show("Placa valida necessaria para guardar itens neste veiculo.");return;}
                privateLabel="Porta-malas / "+Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT,vehicle).Trim();capacity=100;
            }
            if(!gate.Acquire(Name)){CloseCurrent();return;}lease=true;
            Bind();try{ShowStorage();CreditOpen();}catch{CloseCurrent();throw;}
        }
        void Bind(){var p=Game.Player.Character;identity=new StorageIdentity(Profile.Id,p.Handle,p.Model.Hash,vehicle?.Handle??0,vehicle==null?null:container);}
        void CreditOpen(){try{ach.Bump("storage_used");}catch(Exception ex){Log.Error("storage achievement",ex);}}
        void ShowStorage()
        {
            if(!Accessible()){CloseCurrent();return;}
            if(!InterfaceOptions.GridInventory){Rebuild();menu.Visible=true;return;}
            ui.Close();gridOpen=true;
            try{inventory.OpenStorageView(privateLabel??container,capacity,()=>inventory.ContainerSnapshot(container),TransferSaved,Accessible,()=>{gridOpen=false;CloseCurrent();},(id,deposit)=>inventory.TransferFits(container,id,deposit,capacity));}
            catch{gridOpen=false;CloseCurrent();throw;}
        }
        string ItemName(string id)=>inventory.Defs.TryGetValue(id,out var def)?def.Name??id:id;
        string Weight(double value)=>double.IsNaN(value)?"dados invalidos":value.ToString("0.##")+" kg";
        void Add(string title,string description,Action action,bool enabled=true)=>ui.Add(title,description,action,enabled);
        void Rebuild()
        {
            ui.Begin("summary");menu.Name=privateLabel??"Armazenamento";
            Add("Mochila: "+Weight(inventory.Weight)+" / "+Cfg.MaxWeight+" kg","Peso carregado pelo personagem; retiradas precisam caber.",()=>{},false);
            Add("Deposito: "+Weight(inventory.ContainerWeight(container))+" / "+capacity+" kg","Inventario e deposito sao gravados juntos; nenhum item e apagado ao fechar.",()=>{},false);
            Add("Guardar itens / "+inventory.Snapshot().Count(p=>p.Value>0),"Escolha o item da mochila e a quantidade.",()=>Items(true));
            Add("Retirar itens / "+inventory.ContainerSnapshot(container).Count(p=>p.Value>0),"Escolha o item guardado e a quantidade.",()=>Items(false));
            Add("Fechar armazenamento","Conserva itens e libera a atividade.",CloseCurrent);dirty=false;menu.Visible=true;
        }
        void Items(bool deposit)
        {
            if(!Accessible()){CloseCurrent();return;}depositPage=deposit;ui.Begin(deposit?"deposit":"withdraw",Rebuild);menu.Name=deposit?"Guardar / mochila para deposito":"Retirar / deposito para mochila";
            var items=deposit?inventory.Snapshot():inventory.ContainerSnapshot(container);
            foreach(var pair in items.Where(p=>p.Value>0).OrderBy(p=>ItemName(p.Key),StringComparer.OrdinalIgnoreCase).ThenBy(p=>p.Key,StringComparer.Ordinal)){
                string id=pair.Key;int n=pair.Value;int fitting=inventory.TransferFits(container,id,deposit,capacity);
                Add(ItemName(id)+" / x"+n,"Cabem "+fitting+" unidades. "+(inventory.Defs.ContainsKey(id)?"Escolha a quantidade; transferencia sem perda ou divisao automatica.":"Definicao ausente: item preservado, peso padrao0,1 kg."),()=>Quantities(id,deposit));
            }
            if(!items.Any(p=>p.Value>0))Add("Nenhum item neste lado","Transfira pelo outro lado ou volte ao resumo.",()=>{},false);
            Add("Voltar ao resumo","Pesos e opcoes do deposito.",Rebuild);dirty=false;menu.Visible=true;
        }
        void Quantities(string id,bool deposit)
        {
            if(!Accessible()){CloseCurrent();return;}var snapshot=deposit?inventory.Snapshot():inventory.ContainerSnapshot(container);
            if(!snapshot.TryGetValue(id,out int available) || available<=0){Items(deposit);return;}
            int fitting=inventory.TransferFits(container,id,deposit,capacity);ui.Begin((deposit?"put:":"take:")+id,()=>Items(deposit));menu.Name=(deposit?"Guardar: ":"Retirar: ")+ItemName(id);
            Add("Disponivel: "+available+" / cabem: "+fitting,"O peso e a quantidade sao revalidados antes de gravar.",()=>{},false);
            foreach(int value in StorageRules.Quantities(available,fitting)){
                int quantity=value;Add("Transferir "+quantity+(quantity==available?" / pilha inteira":""),"Operacao unica. Se nao couber, nada e transferido.",()=>{TransferSaved(id,deposit,quantity);Items(deposit);},quantity<=fitting);
            }
            Add("Informar quantidade","Numero inteiro positivo, ate o disponivel e o que couber.",()=>{
                string text=Game.GetUserInput(WindowTitle.EnterMessage60,"1",16);if(!Accessible())return;
                if(PhoneViewRules.Amount(text,out int amount)){TransferSaved(id,deposit,amount);Items(deposit);}else if(!string.IsNullOrWhiteSpace(text))Notify.Show("Informe uma quantidade inteira positiva.");
            });
            Add("Voltar aos itens","Nao transfere nada.",()=>Items(deposit));dirty=false;menu.Visible=true;
        }
        public bool OpenPrivate(string id,string label,float maxWeight,Func<bool> access,Action closed)
        {
            if(string.IsNullOrWhiteSpace(id) || !id.StartsWith("home:",StringComparison.Ordinal) || id.Length>100 || string.IsNullOrWhiteSpace(label) || float.IsNaN(maxWeight) || float.IsInfinity(maxWeight) || maxWeight<=0 || access==null || closed==null || !access() || !Safe || inventory.UsingItem || gate.Owner!="homestorage")return false;
            CloseCurrent();pool.HideAll();container=id;privateLabel=label;capacity=maxWeight;privateAccess=access;privateClosed=closed;vehicle=null;Bind();
            try{ShowStorage();if(container==null)return false;CreditOpen();return true;}catch{CloseCurrent();throw;}
        }
        public void ClosePrivate(){if(privateAccess!=null)CloseCurrent();}
        void CloseCurrent()
        {
            if(closing)return;closing=true;
            try{if(gridOpen){gridOpen=false;inventory.CloseStorageView();}var callback=privateClosed;privateClosed=null;privateAccess=null;ui.Reset();vehicle=null;container=null;identity=null;privateLabel=null;dirty=false;if(lease)gate.Release(Name);lease=false;callback?.Invoke();}
            finally{closing=false;}
        }
        bool TransferSaved(string id,bool deposit,int quantity)
        {
            if(!Accessible()){CloseCurrent();return false;}
            try{bool done=inventory.Transfer(container,id,deposit,capacity,quantity);if(done)Notify.Show((deposit?"Guardado: ":"Retirado: ")+quantity+"x "+ItemName(id));else Notify.Show("Sem espaco ou quantidade indisponivel. Nada transferido.");dirty=true;return done;}
            catch(Exception ex){dirty=true;Log.Error("storage transfer",ex);Notify.Show("Falha ao gravar: transferencia nao concluida; confira o log.");return false;}
        }
        bool Accessible()
        {
            if(container==null || identity==null || !Safe || inventory.UsingItem || InterfaceOptions.ModalOpen && !(gridOpen && inventory.StorageGridVisible))return false;
            var p=Game.Player.Character;
            if(vehicle==null){if(!identity.Matches(Profile.Id,p.Handle,p.Model.Hash))return false;}
            else if(!vehicle.Exists() || vehicle.IsDead || JobVehicles.Contains(vehicle) || !identity.Matches(Profile.Id,p.Handle,p.Model.Hash,vehicle.Handle,VehicleKey(vehicle)))return false;
            if(privateAccess!=null)return gate.Owner=="homestorage" && privateAccess();
            if(!lease || gate.Owner!=Name)return false;
            if(vehicle==null)return p.Position.DistanceTo(origin)<4 && Math.Abs(p.Position.Z-origin.Z)<2;
            return vehicle.Speed<1 && Function.Call<int>(Hash.GET_VEHICLE_DOOR_LOCK_STATUS,vehicle)<=1 && p.Position.DistanceTo(Boot(vehicle))<3;
        }
        void NearbyHelp()
        {
            if(gate.Owner!=null || pool.AreAnyVisible || InterfaceOptions.ModalOpen)return;
            var p=Game.Player.Character.Position;var stash=locations.FirstOrDefault(s=>s?.Valid==true && p.DistanceTo(new Vector3(s.X,s.Y,s.Z))<3 && Math.Abs(p.Z-s.Z)<2);
            if(stash==null || !Safe)return;
            World.DrawMarker(MarkerType.Cylinder,new Vector3(stash.X,stash.Y,stash.Z-1),Vector3.Zero,Vector3.Zero,new Vector3(1,1,.2f),System.Drawing.Color.FromArgb(130,80,170,220));
            GTA.UI.Screen.ShowHelpTextThisFrame("Pressione ~b~"+Cfg.StorageKey+"~s~ para abrir "+stash.Name);
        }
        public void Tick()
        {
            if(container==null){NearbyHelp();return;}
            if(gridOpen){if(!inventory.StorageGridVisible || !Accessible())CloseCurrent();return;}
            if(!menu.Visible || !Accessible()){CloseCurrent();return;}
            ui.Tick(true);if(container!=null && menu.Visible && dirty)Items(depositPage);
        }
        public void Reload()=>CloseCurrent();
        public void Save(){}
        public void Dispose(){CloseCurrent();foreach(var b in blips)if(b.Exists())b.Delete();blips.Clear();}
    }
}
