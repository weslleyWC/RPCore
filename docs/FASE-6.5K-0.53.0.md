# RPCore0.53.0 — fase6.5K: vendas territoriais e processamento de receita

## Instalação

Baixe e extraia RPCore-update-0.53.0.zip da branch rpcore-entrega. Com GTA fechado, abra PowerShell na pasta que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

DLL pronta; GTA V Legacy, SHVDN Enhanced1.1.0.6 e LemonUI2.2. Não reinstala .oiv/.rpf e não executa PSRP.dll junto. Laundering.json só é acrescentado se ausente. Configurações/saves/estoques/contratos anteriores, celularF2 e F11 mantidos.

## Preparar o estoque

O modo novo usa **pacotes do tráfico existente**, guardados em Wallet.Dealer.Stock. Não usa as mercadorias legais do estoque territorial, inventário pessoal ou caixas do frete.

Abra F11 → Trabalhos → Trafico / vendas de rua (ou o menu de trabalhos usado nas versões anteriores). Marque GPS do contato, vá até ele e compre pacotes a pé/desarmado, sem procurado/missão/combate e fora de turno. O contato padrão fica em(-1309.499,-770.0988,19.86772). Compre pelo menos o número de pacotes do turno, padrão3; os botões compram5. A compra continua debitando banco e respeitando peso/capacidade do Dealer.

Não inicie as vendas normais nesse menu se deseja receita territorial; volte à área da gangue e use a opção do F4 abaixo. Saldos de vendas anteriores continuam onde estavam.

## Vender na área

1. Esteja afiliado a uma gangue cadastrada e controle um território. Resolva disputas/iniciações e **conclua ou estorne qualquer frete reservado**.
2. **F4 → Comercio dos territorios → GPS** da área. A pé, desarmado, em segurança e até12m do centro, selecione **Vendas ilegais: nome da área**.
3. O menu fecha e inicia o Dealer existente com destino de receita territorial. Aguarde parado numa calçada acessível; o comprador laranja se aproxima. **E negocia**; preço normal ou pedir mais seguem as regras de reputação/recusa/denúncia já existentes. Entrega leva3s junto do comprador por padrão.
4. Cada venda confirmada retira um pacote do estoque e acrescenta o valor em **Dinheiro sujo da área**, na mesma gravação. Não credita banco/cash/tesouraria nessa etapa. O aviso e o menu do comprador indicam o destino da receita.
5. Permaneça no raio padrão45m do território durante o turno; é possível mover-se dentro dele para uma calçada melhor. Troca de gangue/perfil/personagem, perda de posse, cutscene, missão, morte, carro/combate/procurado ou sair da área encerram a atividade. Estoque restante e vendas já confirmadas permanecem. Segurar END pelo tempo do Dealer (1s) também encerra.

Reputação por bairro, estatísticas de venda e risco de denúncia continuam funcionando. Uma denúncia pode acrescentar uma estrela pela regra já existente; o mod não concede imunidade nem remove polícia. Dealer.Earned é receita bruta de ambos os modos. O gatilho cash_earned anterior continua no modo bancário; uma receita suja não é tratada como dinheiro bancário recebido antecipadamente.

**Modo anterior preservado:** iniciar Vendas de rua pelo menu normal do Dealer continua pagando no banco. O modo territorial vale só para o turno explicitamente iniciado pelo F4; terminar/reload limpa essa seleção. Não há conversão retroativa de saldo bancário, itens ou histórico. Receita territorial é isolada por gangue/área/perfil; um rival não recebe o dinheiro de outra gangue.

## Processar e coletar

Encerre o turno e volte a pé ao ponto do território. Em **F4 → Comercio dos territorios**, confira Dinheiro sujo e escolha **Processar ate $500 /85% ao caixa**. Se houver menos de500, usa apenas o saldo disponível.

Permaneça a até12m do centro por **60 segundos ativos** e depois pressione **E**, sem menu aberto. Exemplo:500 sujos viram425 na tesouraria e75 de taxa. O banco e o cash não mudam. A contabilidade acumula arredondamento: processar100 de uma vez ou em100 operações de1 termina com85 líquidos e15 de taxa. Um primeiro dólar isolado arredonda para0 líquido/1 taxa; a compensação de arredondamento aparece nos próximos dólares da mesma conta. Não há dinheiro limpo sem consumir receita suja.

Depois vá à **HQ → F4 → Tesouraria / pagamentos** para retirar o saldo ou receber participação conforme patente e prazo já existentes. Receita processada entra no mesmo caixa financiado por depósitos e comércio legal. O primeiro caixa novo ancora o prazo24h do jogo; uma conta já existente mantém sua referência. Não existe crédito automático no banco nem salário adicional por processar.

Pausa, menus/modal e gaps maiores que2s não acrescentam trabalho. **END** ou **Suspender processamento atual** descarta só o tempo não confirmado; não consome receita e não cobra taxa. Morte, carro, abandono, combate/procurado, missão/cutscene, troca de perfil/ator/gangue ou perda de posse também suspendem. Reload/jogo fechado perde apenas tempo não confirmado, sem processamento offline. Pode iniciar de novo no local. Um limite de caixa/dados ou falha ao salvar recusa a operação sem débito parcial; confira registro e saldos antes de tentar novamente.

A operação não aceita depositar banco/cash como dinheiro sujo; a fonte é a venda territorial real. Frete pendente bloqueia esse ciclo até concluir/estornar. Serviços compartilham ShiftGate e o contexto de E é reservado para não abrir uma loja junto da interação do trabalho.

## Dados e configuração

Novo **Wallet.GangIllicit**: Accounts por Gang/Area, Dirty/Proceeds/Processed/Net/Fees e recibos limitados. Proceeds−Processed=Dirty; Net=floor(Processed×85/100) e Net+Fees=Processed. Venda grava Dealer+GangIllicit juntos, processamento grava GangIllicit+GangTreasury juntos. Falha restaura os livros originais; outros sistemas da carteira e valores desconhecidos são conservados.

**GangTreasury.Accounts → LaunderedIncome** opcional0 em contas antigas. Balance=Deposited+BusinessIncome+LaunderedIncome−Withdrawn−Salary. Esta versão não cria livros/contas/saldos no startup, não apaga fonte ao perder posse e não importa Cash/CleanCash PSRP. Perder a área bloqueia processamento até a mesma gangue recuperar a posse; dinheiro antigo fica na conta original.

**Gangs/Laundering.json**: BatchAmount1–10000 (padrão500), Seconds10–600 (60), Radius5–30 (12), SaleRadius20–100 (45). Taxa fixa15% e líquido85%, conforme referência. Para teste rápido, com jogo fechado pode ajustar Seconds para10 e reiniciar scripts. Configuração inválida usa defaults em memória sem substituir arquivo. Dealer.json permanece igual; estoque e chances do tráfico são os já configurados.

## Comparação com PSRP

GangSpawner.CompleteStreetDrugSale consome estoque Drugs do turf e credita Cash. UpdateAutomaticTurfLaundering processa500/min a85%, usa UTC, catchup de até7200s e separa CleanCash/coleta/salário. Auditoria PSRP-SYSTEMS-AUDIT-0.53.0.json registra evidências/hash e diferenças.

O RPCore oferece opção territorial ligada ao Dealer pessoal e processamento ativo/manual direto na tesouraria. Ainda faltam armazém criminal específico por território, armas/vendas automáticas, lavagem offline, boost do café, logística criminal/convoys e simulação completa. Não copia o estoque ou caixa PSRP e não substitui IA policial. Guerras/diálogo/polícia e fases8 (interface/mecânicas) e9 (todos os trabalhos) continuam pendentes.

## Teste no GTA

- Compre pacotes, anote estoque/banco e inicie modo territorial. Venda: pacote−1, dinheiro sujo+valor, banco/cash iguais. Reabrir/reload não repete receita. Teste também uma venda normal: crédito bancário anterior continua.
- Processe receita, confira Dirty−lote, Processed+mesmo lote, Net85% cumulativo, Fees15% e caixa+líquido. E repetido/reload não deve repetir. Sem receita não inicia; caixa cheio não deve consumir fonte.
- Antes do E final, END/pausa/menu/reload/abandono/carro: sem crédito/taxa/consumo; tempo recomeça ao iniciar outra vez. Menus/pausa não avançam o serviço.
- Troque perfil/gangue/área ou perca posse: fonte original preservada e acesso bloqueado para rival. Não existe depósito bancário como fonte. Confira saque/participação da tesouraria na HQ.
- Confira frete pendente/estorno/retomada, compra e atendimento legais, estoque/caixa antigos, trabalhos/defesas/iniciações, celularF2 e F11.

1901 testes aprovados e build sem erros/avisos. Nuvem não executa GTA; compradores/caminhos/calçadas/área, risco policial, input/animações/HUD e coexistência com mods precisam do seu teste.
