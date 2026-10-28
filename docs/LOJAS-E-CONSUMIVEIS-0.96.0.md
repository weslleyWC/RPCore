# Refinamento 0.96.0 — lojas por categorias e apresentação única dos consumíveis

## Referência e escopo

Foram revisados NewShopSystem.DrawCategories/DrawGrid/HandleInput/PurchaseSelectedItem, os catálogos e modelos de ItemDatabase e a integração do inventário da PSRP.dll. A referência usa seis colunas, quatro linhas visíveis, células de140 sobre passo130, catálogos por categoria e grid à direita em1040/230. O RPCore passa a usar essa organização com as texturas PSRP_Inventory instaladas. Não executamos a DLL de referência nem o GTA aqui.

Nesta entrega são refinadas as conveniências existentes e os pontos Grocery do editor. Não são anunciadas novas lojas especializadas, itens sem mecânica de uso, ferramentas, intoxicação ou reestruturação de estoque dos trabalhos. São mantidos os preços, o pagamento em dinheiro, os ícones do mapa e os pontos de atendimento já corrigidos. A referência usa banco em certas lojas; essa mudança de saldo não foi aplicada no RPCore.

## Lojas

Aproxime-se do círculo de atendimento e aperte E como antes. Com o modo de grade do inventário habilitado (padrão atual), a loja apresenta mochila à esquerda e catálogo por categoria à direita. A mochila nessa tela serve para consulta: comprar não usa o item automaticamente e não move/deleta itens que você já tem. A roda do mouse sobre a mochila permite consultar suas outras linhas. Categorias não vazias são ordenadas: bebidas, lanches, comida, frutas, suplementos, saúde, equipamentos, outros e categorias personalizadas.

- Setas: selecionar produto; PageUp/PageDown: página; Home/End: início/fim.
- Tab: categoria seguinte; Shift+Tab: anterior. Aparecem quatro abas por vez; a seleção muda a janela quando necessário. Não há limite fixo de sete categorias ou42/90 produtos.
- +/− (incluindo numérico): ajustar quantidade. M: quantidade máxima que cabe e pode pagar, limitada a100 por operação. O valor inicial é1.
- Enter: comprar a quantidade indicada. A quantidade não é reduzida silenciosamente após a confirmação; se o saldo/peso mudou, a operação é recusada.
- Mouse: clique num produto para selecionar; clique nas abas e nos botões −, + e MAX. Confirme pelo botão COMPRAR, que mostra o total. Clicar no produto não compra; arrastar/soltar fora do botão, cancelar com botão direito, tecla durante o clique ou trocar para controle não confirma. O botão mantido ao abrir não compra.
- Controle: direcional seleciona, LB/RB muda categoria, X/Y reduz/aumenta quantidade, A compra, B fecha. Abertura/troca de dispositivo aguardam soltar os botões antes de aceitar novas ações.
- Backspace, Escape ou tecla atual do inventário: sair. F2/J/F11 e atalhos1–6 não abrem/usam coisas por baixo da loja.

Os produtos mostram nome, textura e preço; a confirmação mostra quantidade/total, e o painel informa dinheiro, peso da mochila, descrição e resultado da compra. O limite considera dinheiro, peso atual, peso da unidade, capacidade e overflow de quantidade. Cada confirmação usa BuySaved, com as gravações e rollback de falhas normais já existentes. Carteira e inventário continuam repositórios separados: não existe garantia de atomicidade frente a crash entre os dois arquivos ou falha adicional de gravação do reembolso.

A loja ocupa seu próprio gate, liberado ao fechar, morrer, trocar personagem, entrar em veículo, afastar-se, combater, pausar ou interromper o contexto. Não congela nem teleporta o personagem. Movimento/combate são bloqueados apenas nos frames da tela. Pontos do editor são revalidados e deixam de funcionar quando removidos/desativados. O modo antigo de lista continua disponível quando a preferência existente GridInventory está desligada; não foi criada uma opção de ativação adicional.

Itens personalizados mantêm seus nomes, preços, pesos e definições. Um campo opcional Category na definição permite agrupamento próprio, sem modificar seus arquivos atuais; sem ele, a classificação é inferida dos IDs/efeitos. Não são vendidos itens de preço zero nem definições de peso inválido. Materiais e ferramentas existentes não ganham novas ações apenas por terem uma categoria.

## Consumíveis: integração corrigida

A revisão encontrou uma segunda origem de objetos: InventorySystem criava seu prop, e o assinante antigo PropPresentation.Used criava outro com animação/tempo próprios. A versão0.95 reforçou a remoção do primeiro objeto, mas ainda não havia retirado essa segunda origem. Agora PropPresentation cuida da apresentação do telefone; o inventário é o único criador dos props de consumo. O evento Used permanece para outros callbacks, sem a assinatura duplicada interna.

O fluxo único usa os modelos/fallbacks/posições/rotações já configurados em Visuals/Bindings.json e respeita ConsumableProps. Quando não há binding, usa os modelos da referência para refrigerantes adicionais, shaker, sanduíches, chocolates, donut/bagel e frutas. A escolha do binding é copiada em memória sem alterar a configuração. Transforms inválidos usam defaults seguros. Binding existente conserva mão/offsets do caminho antigo; modelos novos da referência usam PHLeftHand e os offsets compatíveis.

Carregamento continua assíncrono: valida modelos, tenta o fallback caso não carregue a tempo e não trava o script. Sem modelo disponível, o consumo já confirmado não é desfeito nem repetido. O único prop criado recebe token de propriedade e usa a remoção confirmada/repetida da0.95. O telefone F2 mantém a apresentação própria. Álcool/drogas e condições médicas completas continuam pendentes, sem declaração de que ícones/modelos implementem suas mecânicas.

Para o teste desta atualização, reinicie o GTA uma vez. Objetos antigos criados pelo apresentador duplicado não tinham marcador de propriedade; não removemos objetos sem identificação por proximidade/modelo, pois podem pertencer a outros mods. Objetos novos de consumo passam pelo fluxo identificado.

## Validação e roteiro GTA

3686 testes aprovados (3639 anteriores +47 novos), Release net48 compilada sem erros/avisos. Testes cobrem catalogação/customização, preservação de todos os produtos, limites de dinheiro/peso/contador, compra em lote e recusa integral, rollback preservando itens no chão/metadados, regiões clicáveis/abas, rolagem e planos de modelos/fallbacks/transforms. Não simulam GTA nativo, desenho ou cursor.

1. Abra duas conveniências e um ponto Grocery do editor. Confira círculo no lado do cliente, mochila/catálogo, categorias, preço personalizado, ícones e descrição. No editor, remova/desative o ponto e confira encerramento.
2. Compre1, mais de1 e máximo. Confira saldo descontado pelo total exato, itens sem consumo automático, peso e slots salvos. Recarregue os plugins e confirme as quantidades.
3. Sem dinheiro, com mochila cheia, sobrepeso e quantidade maior que o limite, tente comprar: nenhum débito/item parcial. Se a última compra reduziu o limite, a quantidade anterior deve exigir ajuste explícito.
4. Teste mouse/clique/arraste para fora/cancelamento, roda em ambas as grades, teclado, controle e troca de dispositivo com A ou botão do mouse mantido. Confira se há apenas uma compra por confirmação.
5. Tente1–6, F2/J/F11 e atacar durante a tela. Afaste-se por ação externa, morra, pause ou troque personagem: tela fecha e o gate é liberado. Fora da loja, controles e atalhos voltam.
6. Teste resolução16:9 e ultrawide, área segura e escala. Modelos/texturas ausentes devem usar o fallback visual sem perder a compra.
7. Após reiniciar o GTA, consuma água, bebida customizada, leite, fruta, comida, chocolate, shaker e bandagem. Deve aparecer no máximo um prop, com a limpeza ao concluir/interromper/recarregar. Confira o telefone F2 e o item Phone, sem regressão de animação.
8. Confira regressão: arraste na mochila, peso, tiros e munição, roda oculta, largar/recolher itens e recuperação de sono. Refinamento dos trabalhos continua reservado à fase9.
