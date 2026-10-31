using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
namespace RPCore
{
    internal sealed class PdmMenuGroup
    {
        static readonly ConditionalWeakTable<ObjectPool,PdmMenuGroup> groups=new ConditionalWeakTable<ObjectPool,PdmMenuGroup>();
        readonly ExclusiveMenuGroup<PdmMenuView> focus=new ExclusiveMenuGroup<PdmMenuView>();
        readonly HashSet<NativeMenu> menus=new HashSet<NativeMenu>();readonly ObjectPool pool;
        PdmMenuGroup(ObjectPool pool){this.pool=pool;}
        public static PdmMenuGroup For(ObjectPool pool)=>groups.GetValue(pool,p=>new PdmMenuGroup(p));
        public void Register(PdmMenuView view,NativeMenu menu,Action close){menus.Add(menu);focus.Register(view,close,()=>menu.Visible);}
        public bool Owns(PdmMenuView view)=>focus.Owns(view);
        public bool Activate(PdmMenuView view)=>focus.Activate(view);
        public void Release(PdmMenuView view)=>focus.Release(view);
        public void HideAll(){focus.CloseAll();pool.HideAll();}
        public static void BeforeProcess(ObjectPool pool)
        {
            if(!groups.TryGetValue(pool,out var group))return;
            bool visible=group.menus.Any(m=>m.Visible);
            if(!visible)return;
            if(Game.IsKeyPressed(Keys.Escape)){
                group.focus.CloseAll();Log.Write("PDM menus closed with Escape; queued actions discarded.");return;
            }
            if(pool.OfType<NativeMenu>().Any(m=>m.Visible && !group.menus.Contains(m))){group.focus.CloseAll();Log.Write("PDM menus yielded to another interface.");return;}
            int closed=group.focus.Reconcile();if(closed>0)Log.Write("PDM menu ownership recovered: "+closed+" stale views closed.");
        }
        public void Waypoint(GTA.Math.Vector3 destination,string name)
        {
            if(float.IsNaN(destination.X) || float.IsInfinity(destination.X) || float.IsNaN(destination.Y) || float.IsInfinity(destination.Y) || float.IsNaN(destination.Z) || float.IsInfinity(destination.Z)){
                Notify.Show("Destino PDM invalido; confira a configuracao.");return;
            }
            focus.CloseAll();
            float distance=Game.Player.Character.Position.DistanceTo(destination);
            Function.Call(Hash.SET_NEW_WAYPOINT,destination.X,destination.Y);
            Notify.Show(distance<=5?"GPS: voce ja esta em "+name+".":"GPS: "+name+" / "+Math.Ceiling(distance)+" m. Marcado no mapa.");
        }
    }
}
