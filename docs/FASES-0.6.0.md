# RPCore × PSRP — comparação e fases (0.6.0)

Andamento 0.12.0: transporte prisional regular aprovado pelo usuário; [piloto](FASE-4.4-0.12.0.md) implementado e aguardando teste real. Dirigível/publicidade e suborno/emboscada continuam pendentes. As tabelas abaixo são históricas.

Andamento 0.11.0: LS GAS/correção de vagas confirmados pelo usuário. [Transporte prisional regular](FASE-4.3-0.11.0.md) implementado e aguardando teste real. Desvio/suborno/emboscada e piloto continuam pendentes; as tabelas abaixo são históricas.

Andamento atual: pizza/Post OP, Uber/valet e reposição foram implementados e aprovados pelo usuário no GTA. A [fase 4.2 — transporte de combustível 0.10.0](FASE-4.2-0.10.0.md) foi implementada e aguarda teste real. Transporte prisional e piloto continuam seguintes na fase 4. A tabela histórica abaixo conserva a comparação original da 0.6.0.

Histórico da fase 1. A 0.6.0 foi posteriormente confirmada funcionando pelo usuário. A fase 2 foi implementada na 0.7.0: [Post OP, pizza e teste atual](FASE-2-0.7.0.md). A tabela abaixo registra o estado na 0.6.0.

A 0.5.1 foi confirmada funcionando no GTA pelo usuário. Esta entrega implementa a fase 1: conveniências e mapa. Os demais jobs ainda precisam ser desenvolvidos. A PSRP.dll foi inspecionada estaticamente, sem execução no GTA; encontrar uma classe, um menu ou um pagamento não comprova que o sistema inteiro funciona.

## Evidência da comparação

Comparados os tipos do binário RPCore 0.6.0, o binário 0.5.1 efetivamente distribuído e a PSRP.dll enviada. Foram indexados os 75 arquivos C# exportados da PSRP, métodos, pagamentos e chamadas no Main. [Relatório com hashes e referências](PSRP-SYSTEMS-AUDIT-0.6.0.json). A exportação da referência serve para análise; não foi adicionada como código do RPCore. O relatório pode ser reproduzido com scripts/audit-psrp-systems.py e os caminhos indicados em --help.

Referências abaixo são nomes de arquivos/métodos da exportação ILSpy. Índices de linha e hashes correspondem à exportação desta auditoria, não ao código-fonte original do autor.

## Fase 1 implementada: compra e localização

Os pontos vinham de memória no ShopSystem antigo. Agora vêm dos 20 pontos de `NewInventorySystem.shopLocations` da PSRP (linhas 539–562 da exportação). Exemplos:

| Loja | Ponto antigo RPCore (X, Y, Z) | Ponto PSRP usado agora (X, Y, Z) |
| --- | --- | --- |
| LTD Davis | -47, -1758.7, 29.4 | -54.7, -1749.089, 29.421 |
| 24/7 Strawberry | 25.7, -1347.3, 29.5 | 33.312, -1346.681, 29.497 |
| LTD Mirror Park | 1163.4, -323.8, 69.2 | 1155.941, -320.24, 69.205 |

O círculo foi movido para o ponto de interação da referência, afastado dos caixas antigos. A posição final com os interiores instalados precisa do teste no jogo. A compra mantém E, peso, preços, carteira, itens e conquistas. O código não modifica a polícia, não apaga o atendente e não interfere nos scripts de roubo da loja. Uma ocorrência policial por outra causa continua possível.

Instalações novas recebem as 20 lojas. O `Shops.json` anterior com exatamente as três lojas e coordenadas padrão é reconhecido em memória e recebe os 20 pontos, sem regravar o arquivo. Em listas personalizadas, apenas uma entrada com nome e coordenadas antigos reconhecidos é corrigida; as outras posições, nomes e lojas são mantidos. Lista vazia continua vazia. Não é necessário apagar saves nem configurações.

| Categoria funcional | Ícone nativo | Cor / escala | Comportamento |
| --- | --- | --- | --- |
| Conveniências | Store (52) | Verde / 0.70 | 20 pontos no catálogo padrão; E compra |
| Combustível | JerryCan (361) | Amarelo / 0.65 | Estações configuradas, sistema já existente |
| Garagens | Garage (357) | Azul / 0.80 | Compra, guardar e retirar existentes |
| Concessionária | PersonalVehicleCar (225) | Azul / 0.80 | RPCore Motors existente |
| Depósitos | Package (351) | Azul / 0.70 | Antes sem ícone; F8 no depósito configurado |
| HQ de gangue | GangHighlight (268) | Cor da gangue / 0.75 | Inclui HQ criada pelo jogador |
| Trabalho de pizza | LuckyWheel (681) | Azul / 0.80 | Mesmo ícone/ponto da PSRP: 536.945, 100.5983, 96.51432 |
| Entrega ativa de pizza | Package (351) | Amarelo / 0.80 | Destino com rota; removido ao terminar |

Os ícones permanentes têm nomes RPCore e são de curto alcance: aparecem no mapa completo e no minimapa próximo. O código só controla os blips que criou. Ícones nativos ou de outros mods podem coexistir no mesmo local. F11/J → GPS inclui a pizzaria. F6 (ou JobKey personalizado) continua iniciando/encerrando pizza em qualquer lugar; não foi imposta uma restrição nova. Nenhum ícone de emprego ausente foi colocado como se o trabalho já estivesse disponível.

## Jobs: diferença real

| Trabalho/atividade na referência | Evidência na PSRP | Estado RPCore 0.6.0 | Falta implementar |
| --- | --- | --- | --- |
| Pizza | `_JOB__Pizza_Delivery.OnTick`, estados e entrega de caixas | Funcional básico: scooter/veículo, destinos, prazo, remuneração, estatísticas e cancelamento; ícone/GPS nesta fase | Contratação/menu no local, carregar pizza, animação de entrega e progressão equivalente |
| Post OP | `_JOB__Post_Op.OnTick`, GeneratePostOpRoute, cancelamento e DeliveredPackage | Ausente | Contratação, van, rota de pacotes, carregar/entregar, pagamento e encerramento |
| Reposição de lojas | `JobDeliveryDriver.Update` | Ausente | Caminhão, carga, roteiro, descarga e retorno |
| Transporte prisional | `JobDeliveryDriver.Update`, pontos de delegacia/prisão | Ausente | Passageiros, ônibus, rota, falha/fuga, pagamento |
| Uber | `_JOB__UberDriver.Update`, GenerateUberFair, EndUberJob | Ausente; tela de telefone ainda não oferece corridas | Serviço, passageiro, embarque, destino, tarifa e avaliações |
| Valet | `_JOB__Valet.OnTick`, entregas e pagamento | Ausente | Receber/estacionar/devolver veículos sem perder veículos de outros mods |
| Entrega de combustível | `_JOB__Gas_Delivery.OnTick`, estados e targetBlipGas | Ausente; abastecer o próprio carro já existe e é outro sistema | Cavalo/carreta, acoplar, rota, descarga, retorno e remuneração |
| Piloto | `_JOB__Pilot.Update`, variantes avião/helicóptero/dirigível e pagamento | Ausente | Contratação, aeronave, embarque, destinos, pouso e remuneração |
| Tráfico | `_JOB__Drug_Dealing.Update`, venda e reputação | Ausente; importar dados não ativa o trabalho | Estoque próprio, compradores, negociação, risco, reputação e venda |
| Streamer | `_JOB__LiveStreamer.Update`, contextos e recebimentos | Ausente | Iniciar live, audiência, eventos, receita e persistência |
| Caça | `_Activity__Hunting.OnTick`, iscas e animais | Ausente | Licença/equipamento, isca, rastreamento, coleta e venda |
| Arrancada | `_ACTIVITY__Drag_Racing.OnTick`, apostas e resultado | Ausente | Seleção, oponentes, largada, chegada e apostas |
| Negócio PDM | `PDMCompany.Update` e transações | Parcial só na compra de carro/garagem do RPCore; empresa ausente | Estoque, contratos, funcionários, finanças, repossessão e entregas |
| Assalto ao estúdio | `_Heist__TheMovieStudio.Update`, preparativos | Ausente | Preparação, objetivos, combate, fuga e recompensa |
| Missão Halloween | `HalloweenHearseMission.Load/Update`, estados Idle/Running/Result | Ausente | Missão sazonal separada com veículo, perseguidores e resultado |
| Guincho empresarial | `_JOB__Towing_Business` está vazia | Ausente em ambos como implementação dessa classe | Requer projeto próprio; não há trabalho pronto para transplantar dessa classe |

`Jobs.CreateJobs()` da PSRP também está vazio. `NewJobSystem` contém infraestrutura de menus; isso não equivale a vários empregos implementados. `TaxiService`/`TaxiRide` são serviço de transporte para o jogador, separados de trabalhar como motorista Uber; também não estão implementados no RPCore.

## Demais sistemas

| Grupo | Já existe no RPCore | Diferença restante frente à referência |
| --- | --- | --- |
| Interface | HUD, velocímetro, radial J, F11, grade de inventário, telefone, banners/YTD | O usuário confirmou a interface funcionando; isso não confirma toda lógica de cada aplicativo |
| Inventário e comércio | Peso, compra/consumo, grade, baús e porta-malas persistentes | Crafting, durabilidade, catálogo completo, feiras/açougue/penhor/sucata, pesca e comércio especializados |
| Sobrevivência | Fome, sede, sono, consumo e indicadores | Expandir efeitos, ações e tratamentos da referência |
| Economia | Carteira, banco, bolsa local e importação de parte do legado | Bolsa Yahoo em tempo real, agências/ATMs e negócios especializados |
| Veículos | Combustível, controles, cinto, garagens, compra, reparo/lavagem básicos | Seguros, licenças/testes, serviços de mecânico completos e gerenciamento empresarial |
| Personagem e moradia | Perfis isolados e importação conservadora | Editor completo de personagem/roupas e apartamentos/hotéis |
| Gangues | Cadastro de HQ, filiação, reputação básica | Spawn organizado, territórios, guerras e economia de gangues |
| Telefone | Notas, calculadora, relógio, pesquisa e atalhos funcionais | Corridas, chamadas, SMS e apps conectados aos empregos pendentes |
| Polícia/prisão | Nenhuma substituição de IA policial | BetterPoliceSystem, PrisonSystem e painel LSPDFR; integração depende da instalação do usuário |
| Mundo e editor | Pontos GPS próprios, rádio por streams, props visuais do inventário | Editor de objetos completo, eventos, clima persistente e demais interações |
| Conquistas | Registro/importação e gatilhos dos sistemas existentes | Conquistas dos empregos ausentes não possuem o gatilho de gameplay correspondente |

Os arquivos YTD são recursos de imagem, e o DLC contém modelos. Eles não implementam os trabalhos. O `psrp_props/dlc.rpf` já instalado permanece como está; a captura do OpenIV confirma o recurso disponível, não exige movê-lo nesta fase.

## Sequência de entrega e aceite

Cada fase termina com DLL compilada, atualização compatível com saves e uma lista curta para o usuário testar no GTA. A fase seguinte deve incorporar os problemas observados no teste anterior. Os números são ordem proposta, não promessa de tudo estar incluído na 0.6.0.

| Fase | Entrega | Critério de teste dentro do GTA |
| --- | --- | --- |
| 1 — entregue 0.6.0 | Conveniências, 20 pontos padrão, ícones funcionais e comparação | Comprar em Davis/Strawberry/Mirror Park do lado público; conferir mapa, depósito e pizza; saves mantidos |
| 2 — próximo pacote | Estrutura compartilhada de turnos, completar interação de pizza e implementar Post OP | Iniciar, retirar veículo/carga, entregar, receber uma vez, cancelar, morrer e trocar de perfil sem perder dinheiro/itens/veículo próprio |
| 3 | Uber, telefone e valet | Passageiro embarca/desembarca; tarifa correta; cancelamento e devolução de veículo; mapa e app coerentes |
| 4 | Reposição de lojas, combustível, transporte prisional e piloto, em subentregas | Carga/ocupantes corretos, destinos acessíveis, conclusão/falha, retorno e limpeza dos recursos próprios |
| 5 | Caça/pesca, crafting/comércio, tráfico, streamer e arrancada, em subentregas | Itens/recompensas/reputação persistem; estoque/pagamento não duplicam; cada atividade passa pelo ciclo completo |
| 6 | Empresa PDM, moradia, personagem, seguros/licenças, gangues e polícia, em subentregas | Cada sistema aceita/rejeita ações corretamente, preserva saves e convive com os recursos instalados |
| 7 | Assalto, sazonal, editor e revisão de paridade | Missões e edição completos; confronto final das funções com a referência e regressão dos sistemas anteriores |

## Teste solicitado agora

1. Instale a 0.6.0 pelo mesmo instalador usado na atualização anterior, com o jogo fechado. Não apague Shops.json nem SaveData.
2. Sem procurado e sem missão ativa, entre na LTD Davis. O círculo deve estar no ponto público de interação, afastado do antigo ponto atrás do caixa. Pressione E e compre água; confira dinheiro, peso e item.
3. Repita em Strawberry e Mirror Park; teste também uma loja fora dessas três, por exemplo Sandy Shores. Com configuração personalizada, consulte a regra de preservação acima.
4. Abra o mapa completo: confira conveniências verdes, postos amarelos, garagens/deposito/pizzaria azuis e HQs com suas cores. A roda usada para pizza é a escolha da própria PSRP; a entrega ativa usa pacote amarelo.
5. J/F11 → GPS → trabalho de pizza. F6 inicia; faça uma entrega, confira pagamento e cancele com F6. O atalho anterior fora da pizzaria continua disponível.
6. No depósito Strawberry, use F8, deposite um item, salve/recarregue e retire. Confira também saldo/itens/garagem já existentes. Recarregar scripts não deve deixar ícones antigos do RPCore acumulados.

Validação automatizada cobre build, dados/migração e instalador; navegação, interiores, NPCs, polícia, renderização e convivência com outros scripts precisam do GTA real.
