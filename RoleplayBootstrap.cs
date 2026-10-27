using System;
using System.Windows.Forms;
using GTA;
namespace RPCore
{
    internal sealed class RoleplayBootstrap : ISystem
    {
        public string Name=>"roleplay_defaults";readonly ShiftGate gate;int next;
        public RoleplayBootstrap(ShiftGate gate){this.gate=gate;next=Game.GameTime;}
        public void Tick(){if(gate.Owner!=null || Game.IsPaused || unchecked(Game.GameTime-next)<0)return;next=Game.GameTime+5000;try{Wallet.RoleplayDefaults();}catch(Exception ex){Log.Error("roleplay defaults retained for retry",ex);}}
        public void Reload(){next=Game.GameTime;}
        public void Save(){}
        public void OnKey(KeyEventArgs e){}
        public void Dispose(){}
    }
}
