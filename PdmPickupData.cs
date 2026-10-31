using System;
using System.Collections.Generic;
using System.Linq;
namespace RPCore
{
    public sealed class PdmPickupPlan
    {
        public string Reason;public bool AtPort;public List<string> StockIds;
        public bool Allowed=>Reason==null;
    }
    public static class PdmPickupRules
    {
        public static PdmPickupPlan Plan(PdmData d,IEnumerable<string> selected,double officeDistance,double portDistance)
        {
            var p=new PdmPickupPlan{StockIds=selected?.ToList()??new List<string>(),AtPort=portDistance<=30 && portDistance>=0};
            if(d==null || !d.Valid || !d.Owned)p.Reason="Empresa indisponivel ou dados invalidos. Arquivo original preservado.";
            else if(d.Active!=null || d.Collection!=null || d.SafeTransfer!=null || d.Repo!=null || d.Rental!=null || d.ShowroomSale!=null || d.PremisesService!=null)p.Reason="Encerre ou recupere a operacao pendente: "+PdmBusinessRules.Pending(d)+".";
            else if(p.StockIds.Count<1 || p.StockIds.Count>2 || p.StockIds.Distinct().Count()!=p.StockIds.Count)p.Reason="Selecione uma ou duas encomendas para a TRFlat, ou use Retirar primeiro pedido.";
            else if(p.StockIds.Any(id=>!d.Stock.Any(s=>s.Id==id && s.AwaitingCollection && s.Engine>=300 && s.Body>=300)))p.Reason="Pedido indisponivel ou danificado. Atualize os pedidos; itens recebidos nao podem ser coletados novamente.";
            else if(double.IsNaN(officeDistance) || double.IsInfinity(officeDistance) || officeDistance<0 || double.IsNaN(portDistance) || double.IsInfinity(portDistance) || portDistance<0 || officeDistance>5 && portDistance>30)p.Reason="Inicie a retirada a pe no marcador do porto ou no escritorio PDM. Marcar o GPS sozinho nao inicia a coleta.";
            return p;
        }
        public static JobPoint PortRigAnchor(JobPoint dock)
        {
            if(dock==null || !dock.Valid)throw new ArgumentException("Invalid port pickup point.");
            double heading=dock.Heading*Math.PI/180;
            return new JobPoint{Name="Porto / conjunto de retirada",X=dock.X+(float)Math.Sin(heading)*24,Y=dock.Y-(float)Math.Cos(heading)*24,Z=dock.Z,Heading=dock.Heading};
        }
    }
}
