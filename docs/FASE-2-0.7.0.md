# Fase 2 — pizza e Post OP (RPCore 0.7.0)

A fase 1 (0.6.0) foi confirmada funcionando pelo usuário no GTA. Esta fase entrega o trabalho normal do Post OP e o contrato presencial de pizza, com código próprio e infraestrutura compartilhada de turnos. O jogo precisa confirmar spawn, acesso aos endereços e apresentação das caixas/animações.

## Onde abrir

- F10 → Trabalhos / contratos → Pizza ou Post OP.
- F11 → Game Settings → Trabalhos / contratos; J/F11 → GPS também oferece os trabalhos.
- F1 → Settings → Trabalhos / Post OP, ou Contacts → Trabalhos / Post OP.
- Perto da pizzaria ou do ponto Post OP: E abre o contrato.

O menu pode ser aberto de qualquer lugar para GPS, estatísticas ou cancelamento. Para contratar o modo normal é necessário estar a pé a até 3 metros do ponto, sem procurado e fora de missão/combate. Não há teleporte. Só um turno pode ficar ativo entre pizza e Post OP.

## Post OP: ciclo implementado

1. No menu Post OP, selecione GPS. Vá ao ponto `-230.7383, -913.2202, 32.3108`, abra com E e escolha Iniciar contrato.
2. Uma Boxville2 da empresa é disponibilizada na rua próxima ao depósito `-282.6413, -914.9976, 30.89793`. Entre como motorista no veículo azul. Se a área estiver ocupada, a contratação falha com aviso sem apagar carros/NPCs para liberar espaço.
3. Siga o destino amarelo. A rota padrão tem cinco endereços distintos, sorteados de 40 pontos da referência e ordenados pela distância em linha reta entre os pontos escolhidos. Isso não é cálculo ótimo por ruas.
4. Pare próximo do endereço. Saia, guarde a arma e vá à traseira da van. E retira uma caixa; as portas traseiras abrem. O modelo `ups_box` instalado é usado quando disponível, com fallback nativo `prop_cs_cardbox_01`.
5. Leve a caixa a pé ao marcador e pressione E. A entrega espera um segundo para a animação; afastar-se, abrir uma interface ou interromper o carregamento impede a confirmação naquele instante. Entrar em veículo, sacar arma, cair ou entrar em combate devolve a caixa à carga lógica: retire novamente, sem perder item de inventário.
6. Cada entrega concluída credita no **banco RPCore** `90 + piso(distância da etapa × 0,04)` dólares por padrão. A confirmação grava saldo, recibo e totais no mesmo Wallet.json. Não é preciso esperar o fim do turno para receber pelos pacotes.
7. Após o último pacote, o GPS aponta para o depósito. Volte dirigindo a mesma van, pare a até 8 metros do ponto e pressione E: recebe o bônus de retorno de $150 e conclui o turno. Desça para a empresa recolher o veículo; ele só é removido quando não tem ocupantes.

Os endereços, ponto de contratação, depósito, quantidade e pagamentos ficam em `scripts/RPCore/Jobs/PostOp.json`. Limites validados: 1–30 entregas, até $100.000 de base/bônus e até $10 por metro. JSON ausente recebe os defaults; arquivo inválido fica preservado e a sessão usa os defaults embutidos com aviso no log. O DLL sozinho também contém os 40 endereços. Recarregue os scripts para aplicar mudanças nesse JSON.

O raio de retirada da caixa considera o jogador até 45 metros do destino e a até 2,2 metros da traseira, com veículo parado. Entrega é a até 2,5 metros do ponto, permanecendo a até 3 metros durante a confirmação. Portões/interiores adicionados por outros mods podem exigir ajustar um endereço no JSON. Nenhum ped destinatário é exigido: a entrega acontece na porta configurada.

## Pizza: contrato novo e F6 preservado

O contrato presencial abre com E na pizzaria `536.945, 100.5983, 96.51432`. Retire a Faggio marcada, dirija ao endereço, pare, saia, pegue a pizza com E na traseira da scooter e entregue a pé com E na porta. Usa `prop_pizza_box_02`, com fallback caso o modelo não esteja disponível. Depois da última entrega, retorne à pizzaria na scooter, pare a até 12 metros e pressione E para o bônus de turno.

Quantidade, pagamento por distância, prazo, bônus por pontualidade e bônus final continuam vindo da seção Pizza do Settings.ini. O contrato usa o catálogo de endereços do PostOp.json e limita a quantidade ao número de pontos distintos disponíveis. Pizzas atrasadas mantêm a redução de pagamento. Os valores financeiros descartam ruído binário do float ao arredondar o coeficiente para seis casas decimais antes de calcular dólares inteiros.

**F6 / PizzaShift continua sendo o modo rápido anterior:** começa de qualquer lugar, usa seu veículo se você for motorista ou fornece scooter se estiver a pé, escolhe destinos de rua e entrega automaticamente ao parar perto deles. O bônus final entra na última entrega, sem exigir retorno. F6 também cancela o turno de pizza ativo. Em ambos os modos, a moto/carro pessoal usado no turno rápido não é removido nem marcado como veículo criado pela empresa.

## Cancelamento, falhas e saves

- Segure **End por um segundo**, com os menus fechados, ou abra o contrato ativo e selecione Cancelar turno.
- Cancelamento, morte, destruição do veículo, troca de perfil e início de missão encerram o turno. Entregas já confirmadas permanecem pagas; não há bônus por um retorno não concluído.
- Turnos em andamento não são retomados após recarregar scripts/reabrir o jogo. Contrate outro. Os pagamentos/totais confirmados persistem, e pacotes não viram itens duplicados de inventário.
- `Wallet.json` ganha o campo adicional `Jobs`, com totais por trabalho e recibos de confirmação dos últimos 64 turnos de cada trabalho. As sessões ativas são geradas em memória com IDs novos, nunca reaproveitadas após reload. Repetir E ou tentar novamente a mesma confirmação não paga duas vezes.
- Se a gravação de Wallet.json falha, o saldo e o recibo são revertidos em memória e a etapa não avança. Arquivo danificado protegido pela camada existente continua protegido. O trabalho pode ser cancelado; o log informa a falha.
- Saldo de banco/caixa de perfil customizado e recibos ficam no mesmo arquivo atômico. Dinheiro nativo de Michael/Franklin/Trevor é estado do GTA: a reversão cobre falhas de gravação na sessão, mas o mod não torna um crash do jogo e o save nativo uma única transação. Não há promessa de recuperação atômica entre dois processos de save distintos.
- `PizzaStats.json` continua existindo, com campos/estatísticas/extension data anteriores. Valores são usados para inicializar o progresso novo quando necessário e espelhados sem zerar o legado. Demais saves/configurações são mantidos.
- Conquista PSRP de primeira entrega Post OP (id 6) recebe o gatilho real. Conquistas são salvas após pagamento em tentativa separada; uma falha nessa gravação é registrada e não reexecuta o crédito.
- Veículos de trabalho são bloqueados temporariamente para guardar em garagem ou depositar itens em porta-malas. Isso também protege o veículo pessoal enquanto o turno rápido está ativo; volta ao uso normal após encerrar. Baús e demais veículos continuam funcionando.
- Limpeza usa apenas os blips, props, modelos, animações nomeadas e veículos que o sistema criou. Carros fornecidos são recolhidos depois que todos saem; ao abortar o script com ocupantes, são liberados ao motor do GTA, sem apagar o veículo sob o jogador. Nenhum ClearAllTasks, remoção de NPCs ou edição de RPF/OIV/DLC foi adicionada.

## O que mudou no projeto

| Arquivos | Alteração |
| --- | --- |
| JobData.cs | Máquina de estados, exclusão entre turnos, rotas, defaults embutidos, cálculo de pagamento, recibos e checkpoint com rollback |
| DeliveryJobSystem.cs / PizzaJob.cs | Ciclo compartilhado e adaptadores pizza/Post OP, menus, spawn validado, carga, GPS, prazo, retorno e encerramento |
| DeliveryCargo.cs | Prop/animação de carga e entrega com timeout; registro de veículos de trabalho e recolhimento sem ocupantes |
| DeliveryJobsMenu.cs | Central de contratos e cancelamento com End |
| Wallet.cs / Economy.cs | Campo adicional Jobs, pagamento persistente e migração conservadora dos totais de pizza |
| Main.cs / CoreMenu.cs / SettingsSystem.cs / PhoneSystem.cs | Registro dos sistemas e acesso por F10/F11/GPS/celular |
| GarageSystem.cs / StorageSystem.cs | Proteção contra guardar veículo de trabalho ou depositar itens em seu porta-malas |
| AchievementSystem.cs | Gatilho real postop_drops para a conquista id 6 |
| Visuals.cs / Bindings.json | Banner PostOpBanner original no menu/cabeçalho, preservando a composição existente |
| RPCore.csproj / AssemblyInfo.cs / VisualAssetDefaults.cs | Recurso de defaults de jobs embutido e versão 0.7.0 |
| tests/JobDataTests.cs / projeto de testes | 42 novos casos; 166 no total |

## Teste no GTA desta entrega

1. Instale a atualização com o jogo fechado, usando o mesmo instalador. Não apague Settings.ini, Shops.json, PizzaStats.json ou Wallet.json. OIV/YTD/DLC existentes continuam os mesmos.
2. F10 → Trabalhos → Post OP → GPS. No local, E → Iniciar contrato. Confira a van azul sem sobreposição com trânsito; entre como motorista e siga a rota amarela.
3. No endereço, pare, saia, retire a caixa com E atrás da van e entregue com E na porta. Confira o crédito **no banco**, o contador e o próximo destino. Apertar E repetidamente não deve duplicar valor.
4. Complete as cinco entregas. Retorne à base, pare, E e desça da van. Confira um único bônus, total de cinco entregas, um turno concluído e recolhimento do veículo vazio.
5. Faça outro turno e cancele depois de uma entrega segurando End. Confira que essa entrega continua paga e que não há bônus. Teste também morte, veículo destruído e troca de perfil: nenhum pagamento deve migrar para outro perfil.
6. Faça o contrato da pizzaria com E: scooter, pizza na mão, porta e retorno. Depois teste F6 usando um carro seu, entregue e cancele: carro e itens precisam continuar disponíveis.
7. Durante um turno, tente começar o outro trabalho: deve recusar. Abra celular/inventário/F11 durante uma entrega, feche e continue; E não deve confirmar sob uma interface aberta. Teste depósito/garagem antes e depois do turno.
8. Recarregue scripts: o turno anterior encerra, sem duplicar pagamentos/ícones. Saldo bancário, totais de entregas e seus saves antigos devem permanecer. Confira também a compra nas conveniências corrigidas na fase 1.

Build/testes de dados e instalador foram executados na nuvem. Renderização/animação, tráfego, portas e recursos nativos são o aceite dentro do GTA. Se um destino estiver bloqueado, registre o nome mostrado e a posição F9 no RPCore.log.

## Próxima fase

Fase 3 continua sendo Uber/valet e a ligação com os aplicativos do telefone. Esta DLL ainda não implementa esses trabalhos, entregas especiais frágeis/segurança, toda a empresa PDM ou o restante da PSRP. A comparação histórica está em [FASES-0.6.0.md](FASES-0.6.0.md); o [índice binário atualizado](PSRP-SYSTEMS-AUDIT-0.7.0.json) registra as classes adicionadas e os 40 pontos usados.
