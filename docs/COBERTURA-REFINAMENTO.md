# Cobertura do refinamento integral

Diretriz permanente confirmada pelo usuário: revisar tudo que foi implementado, incluindo todos os menus e suas ações; consultar a PSRP.dll antes de cada grupo. Base funcional/teste anterior não significam refinamento concluído.

Estado atual:0.107 revisa os12sistemas PDM juntos por autorização explícita para adiantar seu refinamento funcional. Confirmações, navegação, caixa/folha, estoque/inspeção/negociação, contratos, câmera e recursos/entidades foram revistos. GTA pendente; não se infere teste aprovado da0.106.1. Outros trabalhos permanecem no grupo de refinamento correspondente; não se declara paridade integral PSRP.

Este inventário lista os sistemas registrados pelo código e impede tratar só os menus principais como escopo completo. Cada revisão inclui entrada/navegação/saída, ações reais, input/modais, apresentação, persistência, interrupção, recursos instalados e regressão. Tipos de dados/helpers não são declarados sistemas entregues por nome.

| Sistema | Arquivo | Estado do refinamento |
|---|---|---|
| AchievementSystem | [AchievementSystem.cs](../AchievementSystem.cs) | 0.102 revisa grupo completo: resumo/filtros/detalhes/legados/extras, persistência antes de anúncio, overflow/IO/listeners e proteção de dados inválidos. Teste GTA pendente. |
| BarberSystem | [BarberSystem.cs](../BarberSystem.cs) | 0.100: E local, despacho/retorno/seleção, contexto e cancelamento que conserva alterações externas; corte/olhos/taxa existentes mantidos. Câmera dedicada/validação GTA pendentes. |
| CharacterSystem | [CharacterSystem.cs](../CharacterSystem.cs) | 0.104.5 corrige conclusão da aparência/roupa após head blend e revisão de checkpoint; troca de modelo mantém0.102.2, sem menu inicial/handoff. Validação GTA pendente. |
| CoreMenu | [CoreMenu.cs](../CoreMenu.cs) | Grupo0.98: banco/fluxos/perfis/manutencao revisados; aguarda teste GTA; emprestimos PSRP nao portados |
| CraftingSystem | [CraftingSystem.cs](../CraftingSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| CustodySystem | [CustodySystem.cs](../CustodySystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| DealerJob | [DealerJob.cs](../DealerJob.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| DealershipSystem | [DealershipSystem.cs](../DealershipSystem.cs) | Grupo0.106: catálogo/busca/ordem/páginas, garagem escolhida, confirmação, preparação cancelável e compra atômica/rollback; entrega nativa GTA pendente. |
| DeliveryJobSystem | [DeliveryJobSystem.cs](../DeliveryJobSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| DeliveryJobsMenu | [DeliveryJobsMenu.cs](../DeliveryJobsMenu.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| DmvSystem | [DmvSystem.cs](../DmvSystem.cs) | Pendente de revisão completa; preservar base existente |
| DragJob | [DragJob.cs](../DragJob.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| DrivingJobSystem | [DrivingJobSystem.cs](../DrivingJobSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| FishingJob | [FishingJob.cs](../FishingJob.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| FuelDeliveryJob | [FuelDeliveryJob.cs](../FuelDeliveryJob.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| FuelSystem | [FuelSystem.cs](../FuelSystem.cs) | Grupo0.105: menu litros/orçamento/completar, confirmação/contexto/rounding cumulativo/save/rollback, progressos e interrupção; postos/tanques/consumo preservados. GTA pendente. |
| GangSystem | [GangSystem.cs](../GangSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| GarageSystem | [GarageSystem.cs](../GarageSystem.cs) | Grupo0.99: acesso/registro atual/remocao/retirada/servicos revisados; aguarda teste GTA |
| GroomingSystem | [GroomingSystem.cs](../GroomingSystem.cs) | 0.100: categorias de acabamento/face, resumo antes de confirmar, navegação/despacho, cancelamento seguro de pendência; aplicação retomável anterior mantida. Validação GTA pendente. |
| HalloweenSystem | [HalloweenSystem.cs](../HalloweenSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| HomeStorageSystem | [HomeStorageSystem.cs](../HomeStorageSystem.cs) | Grupo0.104: armazenamento privado usa ambas as apresentações revisadas, propriedade/contexto/gate e saída conservados; marcador não compete com atividade. Teste GTA pendente. |
| HousingSystem | [HousingSystem.cs](../HousingSystem.cs) | 0.100: compra confirmada, menus com despacho/retorno/contexto, propriedade revalidada no streaming; saída e armazenamento preservados. IPL/piso/transição dependem de teste GTA. |
| HuntingJob | [HuntingJob.cs](../HuntingJob.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| InventorySystem | [InventorySystem.cs](../InventorySystem.cs) | Grupo0.91–0.96 testado pelo usuário; ferramentas/intoxicação/integrações restantes continuam abertas |
| MapEditorSystem | [MapEditorSystem.cs](../MapEditorSystem.cs) | Pendente de revisão completa; preservar base existente |
| NeedsSystem | [NeedsSystem.cs](../NeedsSystem.cs) | Revisão0.90–0.93 e correção do sono; continuar validação integrada |
| PassengerSystem | [PassengerSystem.cs](../PassengerSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| PdmCompany | [PdmCompany.cs](../PdmCompany.cs) | Grupo0.107: revisão integrada da empresa, menus/ações/contexto/persistência/recuperação e recursos físicos; guia PDM-EMPRESA-0.107.0.md. Native GTA pendente; diferenças PSRP registradas. |
| PdmFinanceSystem | [PdmFinanceSystem.cs](../PdmFinanceSystem.cs) | Grupo0.107: revisão integrada da empresa, menus/ações/contexto/persistência/recuperação e recursos físicos; guia PDM-EMPRESA-0.107.0.md. Native GTA pendente; diferenças PSRP registradas. |
| PdmLogistics | [PdmLogistics.cs](../PdmLogistics.cs) | Grupo0.107: revisão integrada da empresa, menus/ações/contexto/persistência/recuperação e recursos físicos; guia PDM-EMPRESA-0.107.0.md. Native GTA pendente; diferenças PSRP registradas. |
| PdmPremisesSystem | [PdmPremisesSystem.cs](../PdmPremisesSystem.cs) | Grupo0.107: revisão integrada da empresa, menus/ações/contexto/persistência/recuperação e recursos físicos; guia PDM-EMPRESA-0.107.0.md. Native GTA pendente; diferenças PSRP registradas. |
| PdmRentalSystem | [PdmRentalSystem.cs](../PdmRentalSystem.cs) | Grupo0.107: revisão integrada da empresa, menus/ações/contexto/persistência/recuperação e recursos físicos; guia PDM-EMPRESA-0.107.0.md. Native GTA pendente; diferenças PSRP registradas. |
| PdmRepoSystem | [PdmRepoSystem.cs](../PdmRepoSystem.cs) | Grupo0.107: revisão integrada da empresa, menus/ações/contexto/persistência/recuperação e recursos físicos; guia PDM-EMPRESA-0.107.0.md. Native GTA pendente; diferenças PSRP registradas. |
| PdmRiskSystem | [PdmRiskSystem.cs](../PdmRiskSystem.cs) | Grupo0.107: revisão integrada da empresa, menus/ações/contexto/persistência/recuperação e recursos físicos; guia PDM-EMPRESA-0.107.0.md. Native GTA pendente; diferenças PSRP registradas. |
| PdmSafeSystem | [PdmSafeSystem.cs](../PdmSafeSystem.cs) | Grupo0.107: revisão integrada da empresa, menus/ações/contexto/persistência/recuperação e recursos físicos; guia PDM-EMPRESA-0.107.0.md. Native GTA pendente; diferenças PSRP registradas. |
| PdmSecuritySystem | [PdmSecuritySystem.cs](../PdmSecuritySystem.cs) | Grupo0.107: revisão integrada da empresa, menus/ações/contexto/persistência/recuperação e recursos físicos; guia PDM-EMPRESA-0.107.0.md. Native GTA pendente; diferenças PSRP registradas. |
| PdmShowroomSystem | [PdmShowroomSystem.cs](../PdmShowroomSystem.cs) | Grupo0.107: revisão integrada da empresa, menus/ações/contexto/persistência/recuperação e recursos físicos; guia PDM-EMPRESA-0.107.0.md. Native GTA pendente; diferenças PSRP registradas. |
| PdmStaffSystem | [PdmStaffSystem.cs](../PdmStaffSystem.cs) | Grupo0.107: revisão integrada da empresa, menus/ações/contexto/persistência/recuperação e recursos físicos; guia PDM-EMPRESA-0.107.0.md. Native GTA pendente; diferenças PSRP registradas. |
| PdmSurveillanceSystem | [PdmSurveillanceSystem.cs](../PdmSurveillanceSystem.cs) | Grupo0.107: revisão integrada da empresa, menus/ações/contexto/persistência/recuperação e recursos físicos; guia PDM-EMPRESA-0.107.0.md. Native GTA pendente; diferenças PSRP registradas. |
| PhoneSystem | [PhoneSystem.cs](../PhoneSystem.cs) | 0.102.1 corrige persistência nativa compartilhada da carteira/banco; teste GTA de fechamento pendente.0.101 acrescenta RPCore Bank nativo no celular, com quatro operações, cotação de uso único e mesmo Wallet; teste GTA pendente. Grupo0.98: navegacao/mouse/historico/notas/relogio/servicos revisados; aguarda teste GTA; chamadas/SMS externos nao integrados |
| PilotJob | [PilotJob.cs](../PilotJob.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| PrisonTransportJob | [PrisonTransportJob.cs](../PrisonTransportJob.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| PropPresentation | [PropPresentation.cs](../PropPresentation.cs) | Duplicação de consumíveis corrigida/testada0.96; telefone recebe propriedade/limpeza identificada0.98, aguarda GTA |
| RadialSystem | [RadialSystem.cs](../RadialSystem.cs) | Grupo0.97 implementado; aguarda teste GTA e não conclui todas as páginas da referência |
| RadioSystem | [RadioSystem.cs](../RadioSystem.cs) | 0.102 revisa grupo completo: estações/volume/pausa/prev-next/stop/reload XML, contexto/COM próprio e estados/falhas/prefs. Fake player validado; áudio real/GTA pendente. |
| RoleplayBootstrap | [RoleplayBootstrap.cs](../RoleplayBootstrap.cs) | Pendente de revisão completa; preservar base existente |
| SeatbeltSystem | [SeatbeltSystem.cs](../SeatbeltSystem.cs) | Grupo0.105: identidade/sessão/flag original/saída/contexto/avisos revistos, radial/HUD/sons mantidos. Física nativa; teste GTA pendente. |
| SettingsSystem | [SettingsSystem.cs](../SettingsSystem.cs) | Grupo0.97 implementado; aguarda teste GTA e não conclui todas as páginas da referência |
| ShopSystem | [ShopSystem.cs](../ShopSystem.cs) | Conveniências/Grocery0.96 testados pelo usuário; lojas especializadas/venda permanecem abertas |
| StockMarketSystem | [StockMarketSystem.cs](../StockMarketSystem.cs) | 0.102 revisa grupo completo: resumo/listas/busca/ordenação/custo/resultado, ordens/funding confirmados com rollback e checkpoint Wallet, legado/unknowns e relógio de mercado. Teste GTA pendente; mantém simulação local. |
| StorageSystem | [StorageSystem.cs](../StorageSystem.cs) | Grupo0.104: depósitos/porta-malas/apartamento, menus completos/diferidos, quantidade/fit, mouse entre painéis, identidade ator/perfil/carro/placa, armaria e saves. Teste GTA pendente; tampa/animação automática não portada. |
| StreamerJob | [StreamerJob.cs](../StreamerJob.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| StudioHeistSystem | [StudioHeistSystem.cs](../StudioHeistSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| TrafficSystem | [TrafficSystem.cs](../TrafficSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| VehicleControlSystem | [VehicleControlSystem.cs](../VehicleControlSystem.cs) | Grupo0.99: menus/acoes/radial revisados; aguarda teste GTA |
| VehicleInsuranceSystem | [VehicleInsuranceSystem.cs](../VehicleInsuranceSystem.cs) | Grupo0.106: navegação/estado/páginas e confirmações de franquia/estorno/cancelamento; mantém mesmo veículo/retomada/transações. GTA pendente. |
| VisualSystem | [VisualSystem.cs](../VisualSystem.cs) | Pendente de revisão completa; preservar base existente |
| WardrobeSystem | [WardrobeSystem.cs](../WardrobeSystem.cs) | 0.104.5 acrescenta confirmação imediata de roupa ao salvar/vestir/concluir, revisão por modelo e guarda da captura no teclado. Mantém lojas/portas/edição da0.100; câmera dedicada/validação GTA pendentes. |
| WorldSystem | [WorldSystem.cs](../WorldSystem.cs) | Pendente de revisão completa; preservar base existente |

Componentes fora do registro ISystem também estão no escopo: WorkshopSystem, WaypointTeleport, Visuals/menu skins, Wallet/persistência, páginas internas do celular/radial e grades de inventário/depósito/loja. Menus privados/submenus fazem parte da revisão do sistema correspondente.

A sequência continuará por grupos completos: celular/F10 e serviços pessoais; veículos/garagens/oficina; finanças/rádio/conquistas; interfaces de gangues/polícia/atividades/editor/mundo e o restante de inventário/necessidades. Quando a fase8 estiver coberta, revisar os ciclos da9; esta lista não declara todas essas frentes prontas.

Grupo0.99 inclui WorkshopSystem: categorias, navegacao, RGB personalizado e persistencia/rollback existentes; teste nativo GTA pendente. SeatbeltSystem mantem a mecanica atual e recebe acesso direto por radial/menu, sem afirmar revisao fisica integral de cinto/ejeção.

Diretriz de entrega atual: uma publicação por grupo completo de interfaces/sistemas relacionado; correções urgentes de bugs relatados são distintas do avanço normal de refinamento.0.102 concentra bolsa, rádio e conquistas em uma entrega.

0.102.2: falhas de portas/F10 confirmadas por logs foram corrigidas; enumeração de estados veiculares oculta reduzida; cleanup de props usa buffer Enhanced tipado. Registro RuntimeState/RuntimeLastSession coleta contexto sem concluir causa do crash nativo. Não atribuir a0.102.1 ainda não testada.
