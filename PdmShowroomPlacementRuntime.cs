using System;
using GTA.Math;
namespace RPCore
{
    public sealed partial class PdmShowroomSystem
    {
        void OpenPlacement(string id)
        {
            var entry=Wallet.Pdm.Showroom.Find(e=>e.StockId==id);var display=Find(id);if(entry==null || !Matches(display)){OpenMenu();return;}
            ui.HideAll();ui.Begin(Name+":placement:"+id,()=>OpenSlot(entry.SlotId),true,()=>OpenPlacement(id));menu.Name="POSICAO / "+display.Car.LocalizedName;
            foreach(var move in new[]{new{Text="Direita",X=.25f,Y=0f,Z=0f,H=0f},new{Text="Esquerda",X=-.25f,Y=0f,Z=0f,H=0f},new{Text="Frente",X=0f,Y=.25f,Z=0f,H=0f},new{Text="Tras",X=0f,Y=-.25f,Z=0f,H=0f},new{Text="Subir",X=0f,Y=0f,Z=.1f,H=0f},new{Text="Descer",X=0f,Y=0f,Z=-.1f,H=0f},new{Text="Girar / +5",X=0f,Y=0f,Z=0f,H=5f},new{Text="Girar / -5",X=0f,Y=0f,Z=0f,H=-5f}}){var step=move;Add(step.Text,"Move somente o carro exposto reconhecido. Limite3m horizontal/1m vertical; testa colisao e salva a posicao.",()=>{
                if(!office() || !Matches(display) || !Empty(display))return;
                float x=entry.OffsetX+step.X,y=entry.OffsetY+step.Y,z=entry.OffsetZ+step.Z,h=entry.OffsetHeading+step.H;
                if(!PdmShowroomPlacement.Valid(x,y,z,h)){Notify.Show("Limite de ajuste da vaga atingido.");return;}
                var pos=display.Car.Position+new Vector3(step.X,step.Y,step.Z);
                var point=new JobPoint{Name="Ajuste de vaga PDM",X=pos.X,Y=pos.Y,Z=pos.Z,Heading=display.Car.Heading+step.H};
                if(!RigSpawn.ClearForTeleport(point,display.Car.Model,display.Car,GTA.Game.Player.Character)){Notify.Show("Posicao bloqueada por veiculo/pessoa/colisao. Ajuste nao salvo.");return;}
                if(PdmShowroomPlacement.Save(Wallet.EnterpriseWallet(),id,x,y,z,h,Guid.NewGuid().ToString("N"),Wallet.Save,out _)){display.Car.Position=pos;display.Car.Heading=point.Heading;}OpenPlacement(id);
            });}
            Add("Voltar","Vaga.",()=>OpenSlot(entry.SlotId));ui.Show();
        }
    }
}
