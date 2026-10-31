namespace RPCore
{
    public enum DmvAccessBlock { None,Paused,Scene,Loading,Profile,Character,Mission,Modal,Combat,Wanted }
    public static class DmvAccessRules
    {
        // Menu access does not depend on the PDM area or terrain streaming. Physics is checked locally when needed.
        public static DmvAccessBlock Menu(bool characterReady,bool control,bool sameProfile,bool cutsceneActive,bool cutscenePlaying,bool paused,bool mission,bool modal,bool combat,int wanted)
        {
            if(paused)return DmvAccessBlock.Paused;
            if(GameplayContextRules.SceneBlocks(cutsceneActive,cutscenePlaying,control))return DmvAccessBlock.Scene;
            if(!control)return DmvAccessBlock.Loading;
            if(!sameProfile)return DmvAccessBlock.Profile;
            if(!characterReady)return DmvAccessBlock.Character;
            if(mission)return DmvAccessBlock.Mission;
            if(modal)return DmvAccessBlock.Modal;
            if(combat)return DmvAccessBlock.Combat;
            if(wanted>0)return DmvAccessBlock.Wanted;
            return DmvAccessBlock.None;
        }
        public static bool CanPrepare(DmvAccessBlock menu,bool onFoot,float distance,bool ownTerrainReady)=>menu==DmvAccessBlock.None && onFoot && DmvDrive.Finite(distance) && distance>=0 && distance<=3 && ownTerrainReady;
        public static bool DrivingReady(bool control,bool sceneBlocking,bool fadedIn,bool switching,bool localCollision)=>control && !sceneBlocking && fadedIn && !switching && localCollision;
        public static string Explain(DmvAccessBlock block)=>block==DmvAccessBlock.Paused?"Feche a pausa para consultar o DMV.":block==DmvAccessBlock.Scene?"Aguarde a cena terminar e o controle voltar.":block==DmvAccessBlock.Loading?"Aguarde o jogo devolver o controle do personagem.":block==DmvAccessBlock.Profile?"Aguarde a troca de personagem/perfil terminar.":block==DmvAccessBlock.Character?"Aguarde o personagem ficar de pe e em condicoes de atendimento.":block==DmvAccessBlock.Mission?"Conclua ou saia da missao antes de usar o DMV.":block==DmvAccessBlock.Modal?"Feche o celular, inventario ou outra interface aberta.":block==DmvAccessBlock.Combat?"Saia do combate antes de usar o DMV.":block==DmvAccessBlock.Wanted?"Despiste a policia antes de usar o DMV.":null;
    }
}
