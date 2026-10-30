# Correção0.102.1 — duplicação ou perda de carteira após reabrir o GTA

Relato: depositar os400mil da carteira, fechar o jogo e reabrir mantinha400mil no banco RPCore, mas o GTA restaurava também os400mil da carteira do protagonista. Isso permitia depositar novamente o mesmo dinheiro.

## Causa confirmada no código

Para Michael/Franklin/Trevor, Wallet.Cash lia/escrevia Game.Player.Money, enquanto Wallet.Save gravava o banco em Wallet.json sem um checkpoint confiável da carteira nativa. Um save do GTA mais antigo restaurava a carteira anterior sem desfazer o depósito já salvo no RPCore. O campo Cash antigo do Wallet não era uma prova confiável para protagonistas e frequentemente era zero. Perfis MP já usavam a carteira do próprio Wallet.

Foi examinada a implementação de Player.Money na ScriptHookVDotNet3.dll instalada no ambiente: Michael usa SP0_TOTAL_CASH, Franklin SP1_TOTAL_CASH e Trevor SP2_TOTAL_CASH. O getter/setter escolhia a estatística pelo modelo atual do ator. A correção usa as mesmas estatísticas nativas, escolhidas explicitamente pelo perfil proprietário, para também evitar capturar a carteira de outro protagonista durante uma troca de modelo/perfil.

## Correção e preservação

Wallet.json recebe um campo **NativeCash** opcional, com Version1, Owner e Amount. Protagonistas passam a salvar a carteira nativa juntamente com o saldo bancário no mesmo checkpoint Wallet, e Cash espelha esse valor. O campo tem identificação explícita do proprietário; zero é um valor legítimo e precisa ser restaurado quando a carteira foi totalmente depositada. Metadados desconhecidos do checkpoint e do restante do Wallet são preservados.

Ao carregar um checkpoint válido do mesmo protagonista, o RPCore usa o valor confiável para suas operações enquanto aguarda o jogador/controle/tela prontos e o fim da tela de carregamento. Antes de processar menus e sistemas no Tick, aplica esse valor à estatística nativa específica e verifica a leitura. É uma restauração de uso único por carregamento/perfil, não um loop que força sempre o mesmo dinheiro. Depois disso, ganhos/gastos nativos normais são lidos do GTA e entram nos próximos saves. Se uma operação RPCore ocorrer enquanto a restauração está pendente, ela usa e atualiza o valor confiável; a restauração posterior usa o saldo novo, não o anterior.

A escrita mantém o pipeline BankTransfer existente. O snapshot de carteira é preparado junto com o Wallet e, se a gravação falhar, restaura o campo Cash e a referência anterior de NativeCash; a transação restaura banco e dinheiro nativo. A prova do perfil impede aplicar um checkpoint de Michael em Franklin/Trevor. Checkpoint inválido ou de outro proprietário não é silenciosamente sobrescrito.

Perfis MP continuam usando Cash no próprio Wallet e não ganham um checkpoint de protagonista. Conta, inventário, posições da bolsa, veículos e outros dados não são migrados para outra pasta ou zerados. A correção é central no Wallet e cobre celular, F10 e outras movimentações que usam os métodos de carteira/salvamento, sem mudar o visual do banco.

**Primeiro uso em save antigo:** sem NativeCash confiável, o valor antigo de Cash não é restaurado sobre a carteira do GTA. O RPCore adota a carteira nativa atual somente quando o jogador está pronto e cria o primeiro checkpoint. Assim, não zera uma carteira legítima porque o Cash antigo era zero. Depois de atualizar, novos depósitos/saques recebem o checkpoint correto.

**Dinheiro já duplicado antes desta atualização não é retirado automaticamente.** Não há histórico anterior suficiente para distinguir quantias duplicadas de dinheiro legítimo. Se no primeiro carregamento ainda aparecer saldo anteriormente duplicado, isso não é um novo depósito; o teste da correção é efetuar uma nova operação e verificar que o mesmo dinheiro não reaparece após fechar e reabrir.

O segundo relato também é coberto: sacar1,8milhão, deixar banco0, fechar e o GTA carregar os400mil antigos. O checkpoint novo restaura1,8milhão de carteira e mantém banco0. Durante fechamento/carregamento sem controle/tela prontos, um valor nativo resetado não substitui o checkpoint já confirmado; escrita RPCore explícita ainda usa o último valor nativo verificado.

Quantias que já sumiram em versões anteriores também não são recriadas automaticamente a partir de um saldo sem histórico confiável.

Não há atomicidade absoluta entre estatística nativa e arquivo contra queda abrupta do processo. A correção cobre persistência RPCore e restauração após o GTA carregar saldo antigo, com rollback de falhas comuns de gravação; não apaga/reescreve o save de história do GTA nem outros stats.

## Teste obrigatório no GTA

1. Feche o GTA, extraia0.102.1 e rode o instalador dessa pasta com seu caminho real. Não apague Wallet.json nem a pasta SaveData. O instalador preserva os saves e cria backup da DLL.
2. Entre com o protagonista usado no teste anterior e aguarde o controle normal do personagem. No primeiro uso com arquivo antigo, confira o saldo atual sem exigir que dinheiro já duplicado seja removido.
3. Anote banco e carteira. No F2 > RPCore Bank, deposite toda a carteira (por exemplo400mil). Confira carteira0 e banco acrescido do mesmo valor.
4. **Feche totalmente o GTA**, abra novamente e carregue o mesmo save de história antigo, como no relato. Aguarde o controle normal. Banco deve manter o depósito e carteira deve continuar0. Não pode depositar de novo os mesmos400mil.
5. Repita fechamento/abertura pelo menos duas vezes. O total banco+carteira não deve crescer. Recarregar só plugins é um teste complementar e não substitui esse fechamento completo.
6. Saque parte do saldo, feche/reabra e confira as duas quantias. Faça também depósito parcial e repita. Ganhe/gaste dinheiro normalmente no GTA depois da restauração e confira o salvamento.
7. Troque entre Michael/Franklin/Trevor e confira que as carteiras não se misturam. Teste também personagem RPCore MP, onde a carteira permanece no Wallet.
8. Confira F10, bolsa, trabalhos e compras: saídas/entradas de carteira e banco continuam normais. O grupo0.102 e o app aprovado permanecem incluídos.

Sem GTA disponível neste ambiente. A sequência de400mil foi reproduzida em teste serializando o Wallet, restaurando uma carteira nativa simulada antiga e recriando a sessão. O mapeamento/stat API foi conferido na DLL real e o projeto foi compilado contra ela; ainda é necessária a confirmação nativa dos passos acima.
