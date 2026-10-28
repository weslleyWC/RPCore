# Cobertura do refinamento integral

Diretriz permanente confirmada pelo usuário: revisar tudo que foi implementado, incluindo todos os menus e suas ações; consultar a PSRP.dll antes de cada grupo. Base funcional/teste anterior não significam refinamento concluído.

Aceite mais recente: usuário testou a0.96 e confirmou o funcionamento de lojas e consumíveis, reforçando a revisão de todos os sistemas. Fase8 continua; fase9 cobre todos os trabalhos, missões, gangues e seus ciclos físicos/recompensas.

Este inventário lista os sistemas registrados pelo código e impede tratar só os menus principais como escopo completo. Cada revisão inclui entrada/navegação/saída, ações reais, input/modais, apresentação, persistência, interrupção, recursos instalados e regressão. Tipos de dados/helpers não são declarados sistemas entregues por nome.

| Sistema | Arquivo | Estado do refinamento |
|---|---|---|
| AchievementSystem | [AchievementSystem.cs](../AchievementSystem.cs) | Pendente de revisão completa; preservar base existente |
| BarberSystem | [BarberSystem.cs](../BarberSystem.cs) | Pendente de revisão completa; preservar base existente |
| CharacterSystem | [CharacterSystem.cs](../CharacterSystem.cs) | Pendente de revisão completa; preservar base existente |
| CoreMenu | [CoreMenu.cs](../CoreMenu.cs) | Grupo0.98: banco/fluxos/perfis/manutencao revisados; aguarda teste GTA; emprestimos PSRP nao portados |
| CraftingSystem | [CraftingSystem.cs](../CraftingSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| CustodySystem | [CustodySystem.cs](../CustodySystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| DealerJob | [DealerJob.cs](../DealerJob.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| DealershipSystem | [DealershipSystem.cs](../DealershipSystem.cs) | Pendente de revisão completa; preservar base existente |
| DeliveryJobSystem | [DeliveryJobSystem.cs](../DeliveryJobSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| DeliveryJobsMenu | [DeliveryJobsMenu.cs](../DeliveryJobsMenu.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| DmvSystem | [DmvSystem.cs](../DmvSystem.cs) | Pendente de revisão completa; preservar base existente |
| DragJob | [DragJob.cs](../DragJob.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| DrivingJobSystem | [DrivingJobSystem.cs](../DrivingJobSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| FishingJob | [FishingJob.cs](../FishingJob.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| FuelDeliveryJob | [FuelDeliveryJob.cs](../FuelDeliveryJob.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| FuelSystem | [FuelSystem.cs](../FuelSystem.cs) | Pendente de revisão completa; preservar base existente |
| GangSystem | [GangSystem.cs](../GangSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| GarageSystem | [GarageSystem.cs](../GarageSystem.cs) | Grupo0.99: acesso/registro atual/remocao/retirada/servicos revisados; aguarda teste GTA |
| GroomingSystem | [GroomingSystem.cs](../GroomingSystem.cs) | Pendente de revisão completa; preservar base existente |
| HalloweenSystem | [HalloweenSystem.cs](../HalloweenSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| HomeStorageSystem | [HomeStorageSystem.cs](../HomeStorageSystem.cs) | Pendente de revisão completa; preservar base existente |
| HousingSystem | [HousingSystem.cs](../HousingSystem.cs) | Pendente de revisão completa; preservar base existente |
| HuntingJob | [HuntingJob.cs](../HuntingJob.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| InventorySystem | [InventorySystem.cs](../InventorySystem.cs) | Grupo0.91–0.96 testado pelo usuário; ferramentas/intoxicação/integrações restantes continuam abertas |
| MapEditorSystem | [MapEditorSystem.cs](../MapEditorSystem.cs) | Pendente de revisão completa; preservar base existente |
| NeedsSystem | [NeedsSystem.cs](../NeedsSystem.cs) | Revisão0.90–0.93 e correção do sono; continuar validação integrada |
| PassengerSystem | [PassengerSystem.cs](../PassengerSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| PdmCompany | [PdmCompany.cs](../PdmCompany.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| PdmFinanceSystem | [PdmFinanceSystem.cs](../PdmFinanceSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| PdmLogistics | [PdmLogistics.cs](../PdmLogistics.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| PdmPremisesSystem | [PdmPremisesSystem.cs](../PdmPremisesSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| PdmRentalSystem | [PdmRentalSystem.cs](../PdmRentalSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| PdmRepoSystem | [PdmRepoSystem.cs](../PdmRepoSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| PdmRiskSystem | [PdmRiskSystem.cs](../PdmRiskSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| PdmSafeSystem | [PdmSafeSystem.cs](../PdmSafeSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| PdmSecuritySystem | [PdmSecuritySystem.cs](../PdmSecuritySystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| PdmShowroomSystem | [PdmShowroomSystem.cs](../PdmShowroomSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| PdmStaffSystem | [PdmStaffSystem.cs](../PdmStaffSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| PdmSurveillanceSystem | [PdmSurveillanceSystem.cs](../PdmSurveillanceSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| PhoneSystem | [PhoneSystem.cs](../PhoneSystem.cs) | Grupo0.98: navegacao/mouse/historico/notas/relogio/servicos revisados; aguarda teste GTA; chamadas/SMS externos nao integrados |
| PilotJob | [PilotJob.cs](../PilotJob.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| PrisonTransportJob | [PrisonTransportJob.cs](../PrisonTransportJob.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| PropPresentation | [PropPresentation.cs](../PropPresentation.cs) | Duplicação de consumíveis corrigida/testada0.96; telefone recebe propriedade/limpeza identificada0.98, aguarda GTA |
| RadialSystem | [RadialSystem.cs](../RadialSystem.cs) | Grupo0.97 implementado; aguarda teste GTA e não conclui todas as páginas da referência |
| RadioSystem | [RadioSystem.cs](../RadioSystem.cs) | Pendente de revisão completa; preservar base existente |
| RoleplayBootstrap | [RoleplayBootstrap.cs](../RoleplayBootstrap.cs) | Pendente de revisão completa; preservar base existente |
| SeatbeltSystem | [SeatbeltSystem.cs](../SeatbeltSystem.cs) | Pendente de revisão completa; preservar base existente |
| SettingsSystem | [SettingsSystem.cs](../SettingsSystem.cs) | Grupo0.97 implementado; aguarda teste GTA e não conclui todas as páginas da referência |
| ShopSystem | [ShopSystem.cs](../ShopSystem.cs) | Conveniências/Grocery0.96 testados pelo usuário; lojas especializadas/venda permanecem abertas |
| StockMarketSystem | [StockMarketSystem.cs](../StockMarketSystem.cs) | Pendente de revisão completa; preservar base existente |
| StorageSystem | [StorageSystem.cs](../StorageSystem.cs) | Pendente de revisão completa; preservar base existente |
| StreamerJob | [StreamerJob.cs](../StreamerJob.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| StudioHeistSystem | [StudioHeistSystem.cs](../StudioHeistSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| TrafficSystem | [TrafficSystem.cs](../TrafficSystem.cs) | Menus/indicadores na fase8; ciclo completo, variantes e realismo na fase9 |
| VehicleControlSystem | [VehicleControlSystem.cs](../VehicleControlSystem.cs) | Grupo0.99: menus/acoes/radial revisados; aguarda teste GTA |
| VehicleInsuranceSystem | [VehicleInsuranceSystem.cs](../VehicleInsuranceSystem.cs) | Pendente de revisão completa; preservar base existente |
| VisualSystem | [VisualSystem.cs](../VisualSystem.cs) | Pendente de revisão completa; preservar base existente |
| WardrobeSystem | [WardrobeSystem.cs](../WardrobeSystem.cs) | Pendente de revisão completa; preservar base existente |
| WorldSystem | [WorldSystem.cs](../WorldSystem.cs) | Pendente de revisão completa; preservar base existente |

Componentes fora do registro ISystem também estão no escopo: WorkshopSystem, WaypointTeleport, Visuals/menu skins, Wallet/persistência, páginas internas do celular/radial e grades de inventário/depósito/loja. Menus privados/submenus fazem parte da revisão do sistema correspondente.

A sequência continuará por grupos completos: celular/F10 e serviços pessoais; veículos/garagens/oficina; finanças/rádio/conquistas; interfaces de gangues/polícia/atividades/editor/mundo e o restante de inventário/necessidades. Quando a fase8 estiver coberta, revisar os ciclos da9; esta lista não declara todas essas frentes prontas.

Grupo0.99 inclui WorkshopSystem: categorias, navegacao, RGB personalizado e persistencia/rollback existentes; teste nativo GTA pendente. SeatbeltSystem mantem a mecanica atual e recebe acesso direto por radial/menu, sem afirmar revisao fisica integral de cinto/ejeção.
