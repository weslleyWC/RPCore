# RPCore 0.62.0 — fase 6.5T: processamento automático da receita suja

## Instalação

Extraia RPCore-update-0.62.0.zip da branch rpcore-entrega. DLL pronta; feche GTA e abra PowerShell na pasta de Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

GTA Legacy, SHVDN Enhanced1.1.0.6/LemonUI2.2. Preserva saves/configurações existentes, .oiv/.rpf/.ytd/props, celular F2 e teleport F11. AutoLaunder.json só é acrescentado quando ausente. Não use PSRP.dll junto.

## Ativar

O processamento automático começa **desligado**. Carregar/consultar save antigo não ativa nem cria saldo.

1. Tenha receita suja na sua área por venda territorial, lote, entrega de armas ou venda automática anteriores. Pode ativar com saldo zero e aguardar receita futura; isso não cria dinheiro.
2. Afiliado a uma gangue cadastrada e controlando a área, vá a pé ao centro, a até Laundering.Radius (12m padrão), seguro e sem procurado/combate/ragdoll/missão/cutscene/atividade/contrato pendente.
3. **F4 → Comercio dos territorios → Processamento automatico: DESLIGADO**, na seção da área desejada. Selecionar ativa a preferência, sem cobrar ou processar naquele momento.
4. Feche os menus e jogue em segurança, sem outro trabalho/serviço/contrato pendente. Pode sair da área e dirigir; não exige permanência depois de ativar.
5. Após **60 segundos ativos**, consome até **$500 de receita suja existente** e envia **85% ao caixa da gangue**, com15% de taxa. Não cai no banco/cash do personagem.

Cada área tem relógio próprio. Exemplo: Dirty1500 → Dirty1000, Processed+500, líquido+425/taxa+75 e tesouraria+425. Se restarem$125, processa apenas125: líquido106/taxa19 considerando o arredondamento global. Saldo zero não cria lote nem crédito.

O dinheiro no caixa segue as rotinas de tesouraria/retirada/participação já existentes. Não concede salário imediato, reputação, estoque, armas pessoais ou melhoria diplomática.

## Manual, automático e arredondamento

O processamento manual anterior continua disponível, com seu tempo/lote e confirmaçãoE. Enquanto ele ou outro serviço segura o ShiftGate, o automático não avança nem consome a mesma receita.

Ambos usam **o mesmo histórico cumulativo de85%** da conta ilícita. Net=floor(Processed×85/100); a diferença acumulada é creditada ao caixa, preservando frações quando intercala manual/automático, inclusive lotes de$1. Por isso o líquido/taxa de um ciclo pequeno pode variar1 unidade conforme o histórico anterior; o total combinado continua exato. Desligar/reativar não reinicia esse arredondamento.

Venda automática de armas e processamento automático são preferências independentes. A primeira pode gerar$500, e a segunda pode processar esse saldo após seu intervalo apto. Ter os dois ligados não multiplica dinheiro nem altera preço da arma. Com saldo inicialmente zero, o relógio de processamento começa quando chega receita positiva. Compras/vendas físicas/roubos anteriores continuam funcionando.

## Tempo e condições

- Só opera com jogador humano/vivo, seguro, sem procurado/combate/missão/cutscene, filiação cadastrada e posse válidas, sem disputa/iniciação pendente.
- ShiftGate precisa estar livre; atividade em andamento reinicia intervalo. Fretes, roubos e lotes pendentes bloqueiam mesmo suspensos.
- **Pausa, menus e modal congelam tempo**; gaps>2s não contam. Não processa dentro dos menus, mesmo se o relógio já estiver pronto.
- Perda das condições/posse/filiação, receita zerada ou dados inválidos zera o tempo em memória. Preferência permanece salva e reinicia intervalo completo quando voltar a ficar apto.
- Reload, mudança de perfil/ator e alteração da preferência iniciam do zero. Sem compensação offline/catch-up, acúmulo de intervalos ausentes ou atalho por avançar o relógio do GTA.
- Cada ciclo confirma uma única transação. Se um limite impedir crédito, conserva receita e inicia novo intervalo; não faz débito parcial.

Para teste mais rápido, com GTA fechado pode configurar IntervalSeconds30 em AutoLaunder.json e reabrir. Padrão60. Esperar com F4/F11 aberto não conta.

## Desligar e acompanhar

Na seção da área, selecione **Processamento automatico: LIGADO** para desligar. Também há no comércio principal **Desligar auto processar / gangue / área**, disponível para registros ativos mesmo depois de perder posse/filiação. Não exige voltar à área antiga para parar uma preferência.

Desligar conserva receita suja/caixa/histórico e descarta apenas tempo não confirmado. A seção mostra Processed/Net/Fees automáticos; conta geral de dinheiro sujo continua mostrando manual+automático. Cada ciclo confirma por notificação o líquido ao caixa.

Falha de save restaura os três livros e suspende a área na sessão até desligar/reativar ou recarregar, depois de conferir log/dados. Estado inválido, overflow ou tesouraria cheia não apagam/compensam dinheiro. Arquivos inválidos são protegidos; não há reset de saldo para permitir continuar.

## Dados e arquivos

Novos **GangAutoLaunderData.cs** (opções/conta/transações) e **GangAutoLaunder.cs** (menu/condições/relógios). Wallet/Economy acrescentam livro opcional **GangAutoLaunder**, com Accounts por Gang/Area e Enabled/Batches/Processed/Net/Fees; até640 registros e256 recibos. Campos futuros preservados. Consulta não cria preferência; legado permanece desligado.

GangBusiness integra controles por área/tick/limpeza por perfil. Reutiliza GangAutoArmsClock como relógio puro de sessão, com recibo estável por ciclo e sem estado de tempo persistido. Preferências e saldos persistem, tempo não.

A transação reaproveita GangIllicitTransactions.Process num Wallet sombra sem IO e depois grava **AutoLaunder + Illicit + Treasury em uma única Wallet real**. Falha restaura as três referências; replay não repete. Mantém Dirty=Proceeds−Processed, Net global=floor(Processed×85/100), Net+Fees=Processed e conservação da tesouraria. Auto histórico registra apenas os deltas reais de cada ciclo. Preserva âncora anterior da participação; só cria âncora quando a rotina existente cria nova conta com crédito positivo.

Novo **Gangs/AutoLaunder.json**: BatchAmount1…10000 (500), IntervalSeconds30…1800 (60). Configuração inválida usa padrões em memória sem substituir arquivo. Valores carregados valem para próximos ciclos, sem reprecificar histórico. Laundering.json e demais configs/assinaturas anteriores permanecem.

## Referência PSRP e diferenças

Varredura PSRP.GangSpawner.UpdateAutomaticTurfLaundering: taxa base500/minuto, fator85% e restante fracionário, retiraTurf.Cash e creditaCleanCash. Usa UTC, até7200s por atualização e multiplicador temporário de boost. Auditoria PSRP-SYSTEMS-AUDIT-0.62.0.json registra75 fontes, hashes e evidências.

RPCore usa **opt-in, lotes após intervalo completo de sessão ativa, segurança/trava livres, sem catch-up/offline** e envia líquido à tesouraria existente, em vez de outro saldoCleanCash. Preserva manual e arredondamento cumulativo. Missão de abastecimento do café/boost2X ainda pendente; editar taxa não implementa essa missão. Não se afirma paridade completa. Demais atividades/refinamentos/diálogo/guerras/polícia e fases8 (interface/necessidades) e9 (refinamento de todos os trabalhos) continuam futuras.

## Teste no GTA

- Anote Dirty e caixa. Antes de ativar, espere um intervalo: não processa. Ative com≥500 sujos, feche menus e espere60s aptos: Dirty−500/caixa+425, banco/cash iguais. Reabra e confira histórico.
- Abra F4/F11/pausa antes de completar intervalo; esse tempo não conta e não deve confirmar dentro do menu. Inicie trabalho/serviço ou mantenha contrato suspenso/procurado: só reinicia intervalo depois de ficar livre/seguro.
- Desligue/reative e recarregue após ciclo confirmado: sem pagamento repetido/offline. Pare após perder posse/filiado pelo controle principal e confirme preservação de saldo.
- Teste saldo<500, saldo zero e intercalação de processamento manual, sem crédito maior que85% cumulativo. Teste venda automática de uma arma gerando500 e depois processamento425, sem consumo concorrente de trabalhos/estoque.
- Confira tesouraria/participação/retirada anteriores, roubo de van e entregas, saves/configs antigos e F2/F11.

Build/dados/instalador não executam GTA. Tempo/menu/pausa/trava/perfil e convivência com trabalhos, receitas e processamento manual precisam da sua validação no jogo.
