using System;
using System.Drawing;
using System.Windows.Forms;
using GTA;
using GTA.Math;
namespace RPCore
{
    public sealed class HomeStorageSystem : ISystem
    {
        public string Name => "homestorage";
        readonly LemonUI.ObjectPool pool;readonly ShiftGate gate;readonly StorageSystem storage;readonly Func<bool> houseAccess;readonly HomeStorageOptions options;
        bool Access => houseAccess() && Wallet.Housing.Valid && Wallet.Housing.Owns(options.PropertyId);
        public HomeStorageSystem(LemonUI.ObjectPool pool,ShiftGate gate,StorageSystem storage,Func<bool> houseAccess)
        {
            this.pool=pool;this.gate=gate;this.storage=storage;this.houseAccess=houseAccess;
            var defaults=HomeStorageDefaults.Load();options=Store.LoadDef("Jobs/HomeStorage.json",()=>defaults);
            if(options==null || !options.Valid){Log.Write("Invalid HomeStorage.json; embedded defaults used, original retained.");options=defaults;}
        }
        public void OpenMenu()
        {
            if(!Access || InterfaceOptions.ModalOpen){Notify.Show("Entre no seu apartamento para usar o armazenamento privado.");return;}
            if(gate.Owner==Name)storage.ClosePrivate();
            if(!gate.Acquire(Name)){Notify.Show("Encerre a atividade de "+gate.Owner+".");return;}
            bool ready=false;
            try{ready=storage.OpenPrivate(options.ContainerId,"Apartamento / armazenamento",options.Capacity,()=>Access,()=>gate.Release(Name));}
            catch(Exception ex){Log.Error("home storage open",ex);Notify.Show("Armazenamento indisponivel; itens preservados.");}
            finally{if(!ready)gate.Release(Name);}
        }
        public void Tick()
        {
            if(!Access || InterfaceOptions.ModalOpen || pool.AreAnyVisible || gate.Owner!=null)return;
            var point=new Vector3(options.Point.X,options.Point.Y,options.Point.Z);
            if(Game.Player.Character.Position.DistanceTo(point)>2)return;
            World.DrawMarker(MarkerType.Cylinder,point-new Vector3(0,0,1),Vector3.Zero,Vector3.Zero,new Vector3(1,1,.2f),Color.FromArgb(130,80,170,220));
            GTA.UI.Screen.ShowHelpTextThisFrame("Pressione ~INPUT_CONTEXT~ para abrir o armazenamento do apartamento");
            if(Game.IsControlJustPressed(GTA.Control.Context))OpenMenu();
        }
        public void OnKey(KeyEventArgs e){}
        public void Save(){}
        public void Reload(){if(gate.Owner==Name)storage.ClosePrivate();}
        public void Dispose(){Reload();}
    }
}
