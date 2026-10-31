using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
namespace RPCore
{
    public sealed class PdmStreaming : ISystem
    {
        public string Name=>"pdmstreaming";
        static readonly Dictionary<int,int> pending=new Dictionary<int,int>();
        static int spawnAt=-1;
        public static bool Ready(Model model)
        {
            if(!model.IsValid || !model.IsInCdImage)return false;
            if(model.IsLoaded)return true;
            if(!pending.ContainsKey(model.Hash)){Function.Call(Hash.REQUEST_MODEL,model.Hash);pending[model.Hash]=Game.GameTime;}
            return model.IsLoaded;
        }
        public static void Release(Model model,bool force=false)
        {
            // A failed asynchronous probe must not immediately cancel its own streaming request.
            if(!force && !model.IsLoaded)return;
            model.MarkAsNoLongerNeeded();pending.Remove(model.Hash);
        }
        public static bool ClaimSpawn()
        {if(spawnAt==Game.GameTime)return false;spawnAt=Game.GameTime;return true;}
        public static string BuyerModel(string id)=>new[]{"a_m_y_business_02","a_f_y_business_01","a_m_m_bevhills_01"}[PdmRiskTransactions.Roll(id??"pdm")%3];
        public static Ped Buyer(Vector3 point,string id)
        {
            var model=new Model(BuyerModel(id));
            try{if(!Ready(model))return null;var ped=PdmAssets.Track(World.CreatePed(model,point));if(ped!=null && ped.Exists()){ped.IsPersistent=true;ped.BlockPermanentEvents=true;}return ped;}
            finally{Release(model);}
        }
        public void Tick(){foreach(var pair in new List<KeyValuePair<int,int>>(pending))if(unchecked((uint)(Game.GameTime-pair.Value))>10000)Release(new Model(pair.Key),true);}
        public void Reload(){Dispose();}
        public void Save(){}
        public void OnKey(System.Windows.Forms.KeyEventArgs e){}
        public void Dispose(){foreach(int hash in new List<int>(pending.Keys))Release(new Model(hash),true);spawnAt=-1;}
    }
}
