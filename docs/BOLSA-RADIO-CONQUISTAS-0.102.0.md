# Grupo0.102 — bolsa, rádio e conquistas

Entrega única do grupo completo de economia/entretenimento atual: todas as telas e ações de StockMarketSystem, RadioSystem e AchievementSystem foram revisadas. O usuário confirmou o RPCore Bank0.101 funcionando e pediu refinamentos agrupados; o app bancário aprovado e as correções anteriores permanecem incluídos. Esta entrega não marca todas as interfaces ou a fase8 inteira como concluídas.

Referência examinada: StockMarketMod.StockMarketScript (views All/Holdings/Watchlist, busca/ordenação, quantidade, custo médio, resultado e compras/vendas), PortfolioService; PSRP.RadioPlayerIRL/RadioSettings/RadioStation (estações, volume, player, falhas e limpeza); PSRP.Achievements.AchievementManager (desbloqueio, persistência e anúncio). Nenhuma DLL foi executada no GTA aqui.

## Bolsa — F5 por padrão / celular

O menu completo contém resumo de patrimônio, busca por código, todas as cotações, carteira, favoritos e movimentação do caixa da bolsa. As listas ordenam por código, cotação ou resultado, nos dois sentidos. Abrir um ativo mostra posição, custo médio e resultado não realizado, e oferece compra/venda por quantidade, comprar máximo, vender tudo e favoritos. Destaque não executa ordens.

Ordens mostram quantidade/preço/total e começam em Cancelar. Confirmação exige a mesma cotação e carteira mostradas. Se o mercado, saldo, posição ou metadados mudarem, a ordem antiga é recusada e precisa ser revisada. Fluxo vinculado a ator/modelo/perfil e fechado em contexto incompatível. Não vende mais do que possui, não corta silenciosamente uma quantidade digitada e não opera ativo sem cotação positiva.

Depósito e saque deixam de ser fixos em$100: valor escolhido ou todo disponível entre **carteira e caixa da bolsa**. A conta do RPCore Bank é independente e não é debitada aqui. Saque é em unidades inteiras porque a carteira do jogo é inteira; centavos ficam no caixa da bolsa. Previews de movimentação começam em Cancelar, com prova dos saldos atuais. Não há taxa nova ou capital inicial gratuito.

`Wallet.Exchange` permanece o checkpoint canônico. Toda alteração prepara clone, valida e salva antes de confirmar; falha de gravação restaura a carteira anterior e dinheiro no erro comum. A exportação `StockMarket/<perfil>.portfolio.json` é uma cópia: se ela falhar depois do Wallet salvo, a operação não é repetida; o log/aviso informa que a cópia está pendente. O autosave tenta exportar novamente. Wallet existente tem prioridade sobre a cópia legada; não importa duas vezes nem soma os dois arquivos.

Custo médio evita multiplicação de um custo anterior extremo pela quantidade. Posições antigas sem custo conhecido permanecem vendáveis se houver cotação; compras adicionais são bloqueadas para não inventar esse custo. Ativos sem cotação continuam na carteira/favoritos e não são apagados. Valores extremos sem avaliação segura mostram ausência de avaliação, preservando as ações. Dados inválidos permanecem protegidos, sem criar dicionários vazios ou zerar saldo automaticamente.

O mercado continua a **simulação local do RPCore**, não Yahoo/BAWSAQ ou cotações reais. Atualiza após60s de jogo ativo, sem contar pausa, missão ou intervalos grandes/stalls. Mantém o movimento anterior de até±2% para cotações na faixa padrão1–1.000.000. Cotações customizadas fora dela e zero/delistadas ficam intactas, sem impor novo piso/teto a seu arquivo. Não há catch-up offline, renda automática ou dívida.

## Rádio — F3 por padrão / celular

Controles completos: estações, pausar/continuar, anterior/próxima, volume, parar, recarregar XML e fechar menu. A reprodução continua ao fechar o menu. Escolher uma estação inicia o stream; apenas destacar não inicia nada. Volume0–100 é salvo por perfil em `RadioPreferences.json` quando alterado, preservando metadados/valores intermediários. A lista `CustomRadioStream.xml` é preservada e não é regravada pela interface.

O parser aceita tanto o formato legado `<Station name="Nome">URL</Station>` como o DataContract da referência com elementos Name/Url e namespaces. Rejeita XML malformado/entidades externas, sem destruir o arquivo nem substituir uma lista válida que já estava carregada. Recarregar mantém o stream se a URL atual ainda existir; removê-la da lista para a reprodução. Somente http/https são usados.

Mantém o backend Windows Media Player do RPCore, sem instalar NAudio ou bibliotecas novas. Cada troca/falha/stop/profile reload libera somente o player criado pelo mod, inclusive quando Stop lança erro. Connecting/Playing/Paused/Stopped/Failed são acompanhados; conexão expira após30s de deltas ativos, sem contar pausas/stalls. Pausa do jogo pausa o player e retoma apenas se a pausa foi automática; uma pausa escolhida no menu continua pausada. Morte, missão, cutscene ou troca de perfil encerram o stream. Continua permitido ouvir a pé, como no RPCore anterior, sem impor a restrição veicular da PSRP.

A conquista de rádio só recebe progresso quando o player realmente entra em Playing; pausa/retomada/buffering não contam como novo início. Falha de stream ou COM não derruba os outros sistemas. O rádio nativo do GTA não é alterado globalmente.

**É necessário configurar URL direta de áudio compatível.** A URL legada online-radio.eu fornecida nos arquivos é uma página, não uma garantia de MP3 tocável. O mod conserva esse XML do usuário e informa falha em vez de trocar sua estação ou fingir reprodução. Streams e COM reais só podem ser validados no Windows/GTA.

## Conquistas — F7 por padrão

Menu completo com resumo, todas, em progresso, concluídas, registros legados e IDs desbloqueados sem definição atual. Cada meta abre detalhes, descrição, estado e contador real quando existe. Registros sem rastreador são identificados como legados, sem simular progresso. O resumo conta definições conhecidas desbloqueadas, sem inflar o percentual com IDs extras/duplicados; esses registros continuam salvos.

Progresso e desbloqueios são preparados em clone e gravados antes da notificação/som. Erro de gravação conserva o objeto anterior e não anuncia conquista não salva; registra no log, sem lançar erro que faça o trabalho/pagamento repetir. O incremento não é automaticamente reexecutado depois de uma falha. Counters saturam em int.MaxValue, não ficam negativos por overflow. Eventos de progresso são emitidos após salvar, com falhas dos listeners isoladas. Não há garantia atômica entre todos os arquivos de trabalho/dinheiro/gangue; nenhum ciclo da fase9 foi reimplementado.

Definições, metas, nomes personalizados, desbloqueios antigos e campos extras são mantidos. Definições com mesmo ID são apresentadas uma vez; isso não apaga registros salvos. Dados de progresso inválidos ficam sem mutação automática. As telas atualizam progresso sem exigir fechar/reabrir. Não há pagamento ou desbloqueio retroativo inventado para metas sem evento implementado.

## Navegação e preservação

Os três sistemas usam o despacho de menus após ObjectPool.Process, seleção por página, retorno por Back/Escape/B, prova de página/ator/modelo/perfil, botão mantido e fechamento programático seguro. Fluxos com digitação revalidam visibilidade/contexto ao retornar do teclado nativo. Chaves personalizadas e banners/recursos instalados continuam sendo usados. Sem alteração de OIV/RPF/YTD, preços/configs anteriores, saldos bancários, saves de trabalhos ou inventário.

## Teste do grupo no GTA

1. Atualize com o jogo fechado. Confira F2/RPCore Bank e seus saldos, itens, posições da bolsa, favoritos, conquistas e estações anteriores.
2. F5: deposite valor escolhido, cancele confirmação e confira nada alterado; confirme, compre quantidade e máximo, venda quantidade e tudo, saque mantendo centavos. Confira caixa/carteira/banco separado e persistência após reload.
3. Confira todas/carteira/favoritos, busca, ordenações e detalhes. Ativo antigo sem cotação permanece; ativo sem custo médio não aceita compra adicional. Cotação/saldo alterado durante conferência deve recusar a ordem antiga. Teste quantidade sem saldo, zero, negativa e maior que posição; segurar confirmação não pode repetir.
4. Pause/afaste o controle/troque perfil durante menu/digitação. Nenhuma operação antiga deve atingir outro perfil. Aguarde preço mudar em jogo ativo; pausa longa não deve causar várias atualizações instantâneas.
5. F3 com URL direta válida: iniciar, pausar/retomar, anterior/próxima, volume inclusive0/100, fechar menu ouvindo, recarregar XML sem mudar URL, remover URL tocando, stream inválido, XML inválido e reload/profile swap. Confira ausência de reprodução duplicada e volume após reload. Compare pausa manual com pausa automática do jogo.
6. F7: filtros, detalhes, progresso, metas já concluídas e registros legados/extras. Complete ação real (por exemplo rádio realmente tocando) e recarregue: conquista salva não deve anunciar de novo. Confira também que os trabalhos continuam pagando e que metas/gangue registram progresso normal.
7. Teste mouse, teclado e controle nos três menus, voltar em cada nível, cliques mantidos, resolução e banners. Sem GTA aqui, os testes de dados/fake player não comprovam renderização, COM, streaming ou dinheiro nativo.

Validação técnica em VALIDACAO-0.102.0.md. Esta versão fecha a revisão de código do grupo acima, aguardando seus testes nativos; o roteiro geral permanece em COBERTURA-REFINAMENTO.md.
