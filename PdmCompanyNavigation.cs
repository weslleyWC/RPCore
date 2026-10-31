using System.Collections.Generic;
using System.Linq;
using GTA.Math;
namespace RPCore
{
    public sealed partial class PdmCompany
    {
        List<PdmLocation> Places()=>PdmLocation.Available(new[]{
            PdmLocation.At("office","Escritorio e compras","Escritorio PDM","Marcador externo: E abre a empresa. Caixa, equipe e contratos sao presenciais.",options.Office),
            PdmLocation.At("supplier","Escritorio e compras","Compra de estoque / fornecedor","No RPCore a encomenda e feita em Comprar carros / fornecedor, no escritorio. Pedidos pagos sao coletados no porto.",options.Office),
            PdmLocation.At("pickup","Logistica","Retirada de carro / entrega ao cliente","Ponto configurado para retirar o carro de uma entrega ao cliente; inicie o contrato no escritorio.",options.Spawn),
            PdmLocation.At("motors","Escritorio e compras","RPCore Motors / carros pessoais","Loja separada: carros pessoais vao para a sua garagem. Nao abastece estoque da empresa.",new JobPoint{Name="RPCore Motors",X=-56.8f,Y=-1096.7f,Z=26.4f})}
            .Concat(ConnectedLocations?.Invoke()??Enumerable.Empty<PdmLocation>())
            .Concat(Active && destination!=null?new[]{PdmLocation.At("sale:active","Contrato atual","Entrega / cliente atual","Cliente da entrega empresarial em andamento.",destination)}:Enumerable.Empty<PdmLocation>()));
        public void OpenLocations()
        {
            if(!PdmMenuRuntime.Allowed)return;
            Page("locations","PDM / LOCAIS E GPS",OpenMenu,OpenLocations);
            foreach(var name in PdmLocation.Groups(Places())){
                string group=name;Add(group,"Escolha o destino. Marcar GPS nao compra carros nem inicia contrato.",()=>OpenLocationGroup(group));
            }
            Add("Voltar","Empresa PDM.",OpenMenu);ui.Show();
        }
        void OpenLocationGroup(string group)
        {
            if(!PdmMenuRuntime.Allowed)return;
            Page("locations:"+group,"GPS / "+group,OpenLocations,()=>OpenLocationGroup(group));
            foreach(var point in Places().Where(p=>p.Group==group)){
                var place=point;var p=place.Point;int distance=(int)System.Math.Ceiling(GTA.Game.Player.Character.Position.DistanceTo(new Vector3(p.X,p.Y,p.Z)));
                Add(place.Name+" / "+distance+" m",place.Description,()=>{
                    var current=Places().FirstOrDefault(v=>v.Id==place.Id);if(current!=null){var target=current.Point;ui.Waypoint(new Vector3(target.X,target.Y,target.Z),current.Name);}
                });
            }
            Add("Voltar","Grupos de destinos.",OpenLocations);ui.Show();
        }
    }
}
