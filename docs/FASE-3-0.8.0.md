# Fase 3 — Uber e valet (RPCore 0.8.0)

Atualização de status: o usuário confirmou Uber e valet funcionando no GTA. A entrega seguinte está em [fase 4.1 — reposição de lojas](FASE-4.1-0.9.0.md). As informações de validação abaixo registram a entrega original.

O usuário confirmou no GTA os turnos completos de pizza e Post OP da 0.7.0 e seus pagamentos. A 0.8.0 implementa Uber e valet com código próprio, integrados ao celular, central de trabalhos e ao mesmo controle de turnos/pagamentos. Esta versão ainda depende da validação de passageiros, portas, tráfego e estacionamento no GTA.

## Uber: como trabalhar

1. Entre como **motorista em um carro vazio**, com banco de passageiro livre. Pare completamente, fora de missão/combate e sem procurado. O carro escolhido é seu veículo de trabalho durante o turno; o mod não cria um carro Uber para substituir o seu.
2. Abra **F1 → ícone Uber → Entrar em serviço**. F10 → Trabalhos / contratos → Uber também funciona. O celular agora abre no carro parado quando você é motorista e fecha ao começar a dirigir.
3. Aguarde a solicitação. Ela mostra origem, destino e tarifa, com GPS para a busca. Pare e aceite pelo aplicativo ou pressione E. A oferta expira em 30 segundos; recusar não gera pagamento e uma nova solicitação é tentada depois.
4. Dirija até o passageiro. Ele é criado em uma calçada próxima quando você chega à área, após consulta ao navmesh do jogo; não é retirado da população existente. Pare perto dele e pressione **E/buzina** para embarcar.
5. Espere o NPC entrar de verdade no banco do passageiro do mesmo carro. Só então a rota muda para o destino. Se a porta estiver bloqueada, aproxime/destrave o carro e tente E novamente após o aviso. Não há teleporte do passageiro para resolver embarque bloqueado.
6. No destino, pare no marcador e pressione **E**. Aguarde o passageiro sair. O pagamento só é confirmado depois do desembarque perto do destino; passageiro morto/ausente ou saída antes do destino cancela a corrida sem crédito.
7. O banco RPCore recebe a tarifa combinada mais gorjeta quando aplicável. Por padrão, o turno termina após **três corridas concluídas**, com bônus no último pagamento. Corridas recusadas/canceladas não contam para as três.

O aplicativo mostra estado real, ações de aceitar/recusar/cancelar, GPS, total ganho e média das avaliações. Solicitações/aceites/conclusões também entram no histórico local do telefone. Isso é trabalho com NPCs dentro do jogo, sem conexão com serviços de transporte externos ou SMS reais.

O carro é mantido durante todo o turno. Sair dele dá 60 segundos para voltar como motorista; trocar para outro carro não transfere a corrida. A busca tem limite de dez minutos. Cancelar o atendimento atual mantém o turno online; Encerrar turno ou End encerra o serviço. Se não houver locais atendidos próximos, vá a uma região coberta pelos pontos ou encerre o turno.

O carro é destravado temporariamente se necessário no início. Ao encerrar, o estado original é restaurado se ainda estiver destravado; mudanças posteriores de trava feitas pelo jogador/outro sistema são respeitadas. Não se altera persistência, modelo, aparência ou combustível do carro pessoal, nem se remove o veículo no encerramento.

## Valet: receber, estacionar e devolver

1. **F10 → Trabalhos / contratos → Valet → GPS**; também em F11 → GPS. Vá a pé ao ponto do hotel `-1217.306, -197.5938, 39.32496` e pressione **E → Iniciar turno**.
2. Aguarde um cliente chegar dirigindo um carro gerado pelo trabalho, vindo de `-1235.399, -246.8429, 38.71352` até a recepção `-1227.787, -196.7568, 39.17067`. O jogo precisa completar o trajeto e o cliente sair do veículo. Se o acesso estiver ocupado, o sistema espera ou avisa; não apaga tráfego/NPCs para abrir espaço.
3. Entre no carro do cliente e siga o GPS para a área de estacionamento. Pare, saia e pressione **E próximo ao carro** para trancá-lo. O sistema exige que você tenha dirigido esse carro e que ele esteja vazio e parado.
4. Volte a pé ao ponto do hotel e pressione **E** para o cliente solicitar a devolução. O GPS aponta para **o mesmo carro**, que é destravado.
5. Busque o carro, traga à entrada do hotel, pare e saia. Pressione **E** para devolver a chave. Aguarde o cliente entrar como motorista. Desobstrua as portas e tente novamente se houver aviso de bloqueio.
6. A devolução real ao cliente libera o pagamento no banco. O NPC dirige para fora e continua circulando para liberar a próxima chegada; seus recursos são recolhidos depois. Por padrão são **três clientes por turno**, com bônus na última devolução.

O estacionamento mantém a área 3D de 50 metros em torno de `-1292.911, -217.8813, 60.65302` usada na referência. Não representa uma única vaga. O marcador usa a altura atual do carro para orientar o acesso à área, que inclui a garagem; a condição de aceitação é a distância 3D do centro configurado. Esse acesso precisa do teste com os seus mapas/interiores instalados.

Os carros do valet são novos veículos de trabalho. Recebem permissão para dirigir sem ligação direta/flag de roubo; o código não desliga a polícia nem altera flags dos carros pessoais. Carros de ambiente, de garagem ou de outros mods não são cadastrados como clientes. Não há captura de veículo por placa. Você deve devolver a instância criada para o atendimento.

## Pagamento, gorjeta e avaliação

| Trabalho | Padrão por atendimento | Gorjeta | Bônus do turno |
| --- | --- | --- | --- |
| Uber | $65 + piso(distância em linha reta entre os pontos de rua × $0,10) | $20 com avaliação 4 ou 5 | $100 na terceira corrida concluída |
| Valet | $120 por carro devolvido ao cliente | $40 com avaliação 4 ou 5 | $100 no terceiro cliente atendido |

Todos os valores desta fase entram no **banco RPCore**. A tarifa Uber é definida ao gerar a oferta; dar voltas não aumenta o pagamento. Os 48 pontos de origem/destino vieram da PSRP, e o motor do jogo resolve ruas/calçadas próximas. A seleção usa pontos a até 2.500 metros do motorista e pares separados por 500–6.000 metros antes do ajuste à rua.

A avaliação mede a maior perda de BodyHealth desde o embarque do passageiro ou a entrega do carro vazio ao manobrista. Perda até 10: 5 estrelas; até 75: 4; até 200: 3; até 400: 2; acima: 1. Reparar o carro durante o serviço não apaga dano já medido. Não é uma avaliação completa de legislação de trânsito, velocidade ou conforto do passageiro.

Crédito, recibo, contagem de serviços, término do turno e soma/quantidade de avaliações são gravados juntos em Wallet.json. Repetir E ou tentar uma confirmação novamente não repete esses efeitos. Erro de gravação reverte as mudanças e mantém a confirmação pendente. As conquistas de primeira corrida Uber (id 4) e primeiro valet (id 34) recebem os gatilhos reais; seu arquivo é salvo em uma tentativa separada após o crédito.

## Configuração e preservação

- `scripts/RPCore/Jobs/Uber.json`: corridas por turno, tarifa base, valor por metro, gorjeta, bônus, distância máxima de busca e locais.
- `scripts/RPCore/Jobs/Valet.json`: clientes por turno, pagamento/gorjeta/bônus, pontos do hotel/estacionamento/saída, raio e modelos de carro.
- Defaults dos dois trabalhos também vêm embutidos na DLL. Arquivos ausentes recebem os defaults; arquivos inválidos são mantidos e o log informa o uso dos defaults embutidos. Alterações nesses JSONs exigem recarregar scripts.
- Wallet.json recebe campos adicionais de avaliação no progresso por trabalho. PizzaStats.json, inventário, bancos, garagens, configurações e campos desconhecidos anteriores são preservados. Os campos de avaliação antigos/ausentes começam em zero; estatísticas de pizza/Post OP não são zeradas.
- Pizza, Post OP, Uber e valet compartilham um único turno ativo. Estar online esperando corrida também ocupa esse turno. End segurado por um segundo, com menus fechados, encerra o trabalho ativo; os menus oferecem a mesma ação.
- Cancelamento, morte, veículo perdido, troca de perfil ou início de missão encerram o trabalho. Serviços já confirmados continuam pagos; atendimento incompleto não recebe crédito nem bônus. O turno não é retomado após reload/reabrir o jogo; o progresso financeiro confirmado permanece.
- Garagem e depósito em porta-malas ficam bloqueados para o carro de trabalho durante o serviço. O carro pessoal volta ao uso normal depois do Uber. Carros de cliente gerados são recolhidos vazios; nunca são removidos sob o jogador ou outros ocupantes.
- Passageiros/clientes criados pelo RPCore são os únicos NPCs controlados/retirados pela limpeza. Não há ClearAllTasks do jogador, remoção de ped de ambiente, edição de relacionamentos globais ou limpeza de áreas. Em cancelamento, o passageiro recebe ordem de sair; há recolhimento posterior do NPC próprio se a saída não concluir.
- As imagens continuam nos YTDs já instalados: `BoltBanner`, `Valet` e `UberIcon`. Nenhum OIV, YTD, RPF, DLC, dlclist ou pacote visual novo é necessário.

## Arquivos alterados

| Arquivos | Implementação |
| --- | --- |
| DrivingJobData.cs | Estados de corrida e valet, controle de sequência do turno, seleção de oferta, medição de dano, pagamento e defaults testáveis |
| DrivingJobSystem.cs | Menus, ciclo ISystem, exclusão entre trabalhos, pagamento persistente, GPS, cancelamento e encerramento comuns |
| UberJob.cs | Solicitações, carro do motorista, NPC, embarque/desembarque, tarifa, gorjeta, progresso e ações do aplicativo |
| ValetJob.cs | Chegada do cliente, estacionamento/trava, solicitação de devolução, recuperação do mesmo carro, embarque do dono e saída |
| JobActors.cs | Criação de NPCs próprios, validação de rua/calçada, tarefas e limpeza posterior limitada aos recursos criados |
| Persistence.cs / Store.cs | Leitura de definicoes preserva arquivos invalidos ou JSON null, usa fallback e so cria arquivos ausentes |
| JobData.cs / Wallet.cs | Campos opcionais de avaliação incluídos no checkpoint existente, com rollback e proteção contra repetição |
| DeliveryJobSystem.cs / DeliveryJobsMenu.cs | Interface comum para os quatro trabalhos; pizza/Post OP mantêm seus ciclos validados |
| Main.cs / SettingsSystem.cs / CoreMenu.cs | Registro dos novos sistemas e acesso pela central/F11/GPS |
| PhoneSystem.cs | Aplicativo Uber funcional, histórico real de atividade e telefone no carro parado como motorista |
| AchievementSystem.cs | Gatilhos uber_trips e valet_returns para as conquistas correspondentes |
| Visuals.cs / VisualAssetDefaults.cs / Bindings.json | Banners originais para os novos trabalhos e versão de diagnóstico |
| RPCore.csproj / AssemblyInfo.cs / config/Jobs | Defaults embutidos, configurações e versão 0.8.0 |
| tests/DrivingJobTests.cs / projeto de testes | 60 casos novos; 226 no total, incluindo os 166 anteriores |

## Teste no GTA desta entrega

1. Feche o GTA, instale a atualização com o mesmo instalador e preserve os saves/configurações. Não reinstale os recursos visuais que já funcionam.
2. Uber: carro comum vazio, motorista, parado → F1 → Uber → Entrar em serviço. Confira a oferta, aceite, busque o passageiro, pare e E/buzina. Confirme que ele entra e que o destino aparece.
3. No destino, pare e E. Confira desembarque, **um crédito no banco** e uma avaliação. Complete três corridas para testar o bônus/término do turno; confira totais no aplicativo.
4. Recuse/cancele uma solicitação e uma corrida em andamento: sem crédito. Tente uma porta bloqueada, saia do carro e volte, e teste carro sem banco livre: sem teleporte/remoção de outro NPC. Seu carro, placa, combustível e itens devem continuar disponíveis após encerrar.
5. Valet: F10 → Trabalhos → Valet → GPS, contrate no hotel a pé. Aguarde a chegada e saída do cliente; estacione, saia e tranque com E. Volte ao hotel, solicite devolução, recupere o mesmo carro, retorne e entregue a chave.
6. Confira o cliente entrando como motorista, saída e pagamento único no banco. Faça três atendimentos, verificando se a entrada fica livre para o próximo e se só há um bônus final.
7. Faça um serviço com dano no carro e compare gorjeta/avaliação. Cancelar antes da devolução/chegada não deve pagar. Teste morte, carro destruído, troca de perfil e reload: sem pagamento ou estatística em outro perfil; atendimentos já confirmados devem permanecer.
8. Tente iniciar outro trabalho durante Uber/valet: deve recusar. Depois faça uma entrega de pizza e uma de Post OP para regressão, confira loja/baú/garagem e teste abrir interfaces sem E atuar por baixo.

Se um ponto/porta não for acessível, anote o nome da solicitação, estágio visível, posição F9 e trecho do RPCore.log. O build e os testes na nuvem não executam a IA, o navmesh, o trânsito ou o GTA real.

## Escopo e sequência

Esta fase entrega um passageiro por corrida e um carro por atendimento de valet, com três atendimentos por turno por padrão. Grupos de passageiros, variantes de luxo, uniformes e eventos de ladrão da referência continuam pendentes. A PSRP não foi executada; os pontos foram comparados estaticamente e a implementação é própria. O índice atualizado está em [PSRP-SYSTEMS-AUDIT-0.8.0.json](PSRP-SYSTEMS-AUDIT-0.8.0.json).

Após o aceite desta fase, a sequência anterior prevê reposição de lojas, entrega de combustível, transporte prisional e piloto em subentregas. Não estão implementados nesta DLL.
