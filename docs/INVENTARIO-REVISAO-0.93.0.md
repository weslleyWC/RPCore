# RPCore 0.93.0 — revisão agrupada do inventário

Pedido: incluir os atalhos 1 a 6 e verificar o inventário inteiro da PSRP.dll antes de continuar com mudanças pequenas. Esta entrega revisa o núcleo, seus controles e suas integrações existentes. A comparação também registra funcionalidades da referência que continuam distintas ou ausentes; não declara paridade integral nem homologação dentro do GTA.

A varredura estática indexa 315 métodos em cinco arquivos: Main (18), NewInventorySystem (96), NewStorageSystem (20), NewShopSystem (9) e ItemDatabase (172). Os fluxos de uso/atalhos, desenho/navegação, receitas, armas/munição, compra, transferência e persistência foram comparados. O índice com linhas e hashes está na auditoria PSRP-SYSTEMS-AUDIT-0.93.0.json. A referência foi lida, não executada.

| Área da referência | Situação no RPCore |
| --- | --- |
| Main.onKeyDown → HotkeySlot(0..5) | 1 a 6 acima das letras; primeira linha absoluta, independentemente da rolagem; uma execução por pressão, usando o filtro de teclas mantidas do Main. |
| Teclas e sobreposição de menus | NumPad/Alt/Ctrl/Shift não ativam atalhos; teclas configuradas para outros sistemas têm prioridade. Telefone, radial, edição, vigilância, menus, depósitos, uso em andamento, missão/atividade e troca de perfil impedem uso indevido. |
| Notificações de inventário | Usa o feed/UI existente do RPCore; a fila visual InventoryBoxNoti da referência ainda difere. |
| DrawInventoryGrid/HandleInput | Seis colunas, quatro linhas visíveis, rolagem por linha, barra de rolagem e peso, números na primeira linha, Home/End/PgUp/PgDn e controle. |
| SwapItem/held item | C/X movimenta ou troca a referência de posição; não retira o item do estoque durante a escolha. Fechar/cancelar conserva o original. |
| Unidade/pilha | DELETE/LB solto descarta uma unidade; segurar por 900 ms descarta a pilha. Descarte é definitivo, sem prop coletável no chão. |
| NewStorageSystem | Duas grades nas posições da referência; TAB/RB troca de lado; Enter/A transfere uma unidade, Shift+Enter/LB+A transfere a pilha. Depósitos, porta-malas e apartamento usam as mesmas quantidades salvas existentes. |
| Layout na transferência | Corrigido o caminho antigo de 25 posições; usa 24 por janela. Slots altos, lacunas dos atalhos, IDs desconhecidos e metadados são mantidos. O helper legado de dois argumentos continua compatível. |
| Save/load das transferências | Candidato inteiro gravado antes da atribuição; retirada a chamada de Save que podia reentrar no acompanhamento de munição enquanto o candidato estava temporariamente ativo. |
| WeaponDefinition | 63 nomes/ícones lidos da referência, incluindo StickyBomb → ícone c4; nomes novos não reescrevem os itens/saves existentes. |
| Aquisição nova após descarte | Transição nova observada durante acompanhamento contínuo pode recadastrar arma totalmente descartada; recarga/arsenal antigo não são prova de aquisição. Reserva nova comprovada é somada uma vez por pool, sem perder nem somar por arma. |
| Equipar/munição/arremessáveis | Mantém o núcleo0.92, sem Give250 ou desconto pelo simples botão de tiro; tipos nativos compartilhados e pilhas de arremessáveis. Bloqueio da roda agora inclui categorias desarmado/corpo a corpo. |
| ItemDatabase alimentos/bebidas | Acrescenta31 definições ausentes, sem duplicar19 IDs existentes; valores/nomes/pesos/preços personalizados anteriores continuam nos seus arquivos. |
| Energia | Café, energéticos e bebidas aplicáveis recebem a energia da referência, como incremento idempotente sobre o sono atual. Um recibo antigo não desfaz o sono recuperado. Energy explícito nos itens permite ajustar/desabilitar esse bônus. |
| Recipientes retornados | Beber água/leite retorna embalagem; beber shaker retorna shaker sujo. A devolução entra no mesmo candidato salvo que retira o consumível, sem duplicar em recarga. ReturnItem vazio explícito desabilita a devolução. |
| Uso e apresentação | 6,5 s, animação e no máximo um prop próprio, com carregamento assíncrono limitado. A tela fecha durante o uso e volta depois da conclusão normal se estava aberta. Uso rápido com a tela fechada não a abre depois. |
| Segurança durante uso | Pausa/stall não dá crédito de tempo. Veículo/embarque, morte, perfil/ator, missão, combate ou perda da atividade interrompem a apresentação. Sem ejetar, congelar ou limpar todas as tarefas. Item/benefício já confirmados não são repetidos ou reembolsados. |
| HandleItemAction Phone | Item Phone reutilizável abre o celular existente. F2 continua disponível sem exigir comprar um celular, preservando o comportamento anterior. |
| Receitas pessoais | Nove receitas: quatro sabores de whey com água/leite e lavagem de shaker sujo. Três seleções de ingrediente, resultado real, quantidade1..100 limitada pelos materiais, peso e overflow; gravação única antes da confirmação. |
| Preview/cancelamento de receita | Selecionar ingrediente apenas referencia o estoque; remover/cancelar/fechar não devolve cópias nem consome itens. Receita deve corresponder exatamente à combinação. |
| Compra em loja | Salva carteira e inventário imediatamente. Falha normal na gravação devolve o dinheiro em memória e tenta persistir o reembolso; efeitos de apresentação vêm depois da compra. Preço/meio de pagamento existentes são mantidos. |
| IsAnyShopOpen/IsAnyStorageOpen | Modais impedem uso rápido por baixo da loja/depósito. O fechamento da grade libera apenas o callback e a atividade de armazenamento que a abriu. |

## Como usar

1. Instale o ZIP completo0.93.0 pelo Install-RPCore.ps1 incluído. Não adicione PSRP.dll ao lado do RPCore. Dependências continuam SHVDN Enhanced1.1.0.6 e LemonUI2.2 SHVDN3.
2. Abra o inventário com sua tecla atual (I por padrão). Use C para pegar uma posição, navegue e confirme com C/Enter. Arrume os seis primeiros slots como quiser.
3. Feche. As teclas1,2,3,4,5,6 da parte superior usam essas posições. Slot vazio não usa o próximo item; não importa qual linha estava visível quando fechou. Material sem uso direto apenas explica seu papel. Arma equipa/guarda; comida consome uma unidade. Espere a animação terminar antes de outro uso.
4. Na grade, T seleciona até três ingredientes, TAB muda o foco para a área de receitas. Setas horizontais escolhem ingrediente/resultado. Enter remove uma seleção ou fabrica quando estiver no resultado; cima/baixo ajusta a quantidade, Shift+Enter fabrica o máximo permitido por operação (até100). No controle: Y seleciona, RB alterna foco, A confirma, direcional ajusta.
5. Exemplo: compre Shaker Bottle, Whey Strawberry e água/leite. Selecione os três e fabrique. Beba o resultado e confira sede/energia e Shaker Dirty. Combine Shaker Dirty + água para lavar. Os outros três sabores seguem a mesma combinação.
6. F8 (ou a sua tecla de armazenamento), a pé no depósito ou atrás do seu carro: TAB alterna inventário/depósito. Enter move1; Shift+Enter move a pilha. No controle RB/A/LB+A. Backspace/B fecha. O apartamento conserva o acesso pela casa e sua atividade própria. Não guarde itens em veículos de trabalho.
7. Roda de armas continua em F11 → Game Settings. F2, J, F11 e teleporte conservam suas funções. A opção antiga de inventário em lista continua respeitada; a área de receitas e o depósito lado a lado usam o modo de grade.

## Persistência e limites reais

Novos arquivos de definição: Inventory/ReferenceItems.json e InventoryRecipes.json. Os arquivos antigos de Items, Keys, Settings, Visuals, Shops e demais definições não foram reescritos. Itens antigos continuam com seus valores; Energy e ReturnItem novos podem ser definidos explicitamente. O limite de peso configurado do RPCore é conservado; não é substituído pelos75 kg hardcoded da referência. Os preços dos31 novos itens são padrões próprios editáveis, não uma declaração de equivalência de todas as lojas PSRP. O RPCore mantém pagamento em dinheiro nas conveniências; não muda os saldos para imitar compras bancárias da referência.

Items e Containers continuam dicionários agregados do save atual. Não há conversão para pilhas independentes por slot, limite destrutivo de90 slots ou migração automática dos estoques separados de caça/pesca/dealer/gangue/bancada. Esses estoques e seus pagamentos/progresso permanecem próprios. Ferramentas e receitas diretamente ligadas a atividades exigem o refinamento da fase9. Isso evita misturar bolsas/recibos já existentes com o inventário pessoal.

Diferenças ainda abertas, registradas nesta revisão: intoxicação completa por álcool/drogas e condições médicas; todos os produtos/receitas de drogas/bong; guarda-chuva reutilizável; chave de fenda e interações de parquímetro/placa; galão e seu fluxo de abastecimento; iscas/matérias-primas de caça/pesca na mochila pessoal; todas as categorias/grades de lojas especializadas e venda da referência. As ações próprias das atividades ficam9; ferramentas e intoxicação que não dependem de trabalho seguem no restante da8. Não são anunciadas como entregues por existirem nomes/ícones.

Tintas/acessórios e munição continuam no modelo0.92, com correção de aquisição nova observável após descarte. Por ser um cadastro agregado por tipo, uma cópia guardada em outro container impede clonar esse tipo por observação nativa. Aquisições em contextos interrompidos/sem prova contínua (como certas interfaces nativas que suspendem controle) são tratadas conservadoramente; comprar novamente na Ammu-Nation nessas condições precisa teste específico e não é declarado resolvido em todos os fluxos. Ferramentas nativas como galão/extintor/paraquedas não viraram itens de compra funcionais nesta entrega. A apresentação de consumo usa props genéricos seguros do GTA (garrafa, hambúrguer ou kit), não todos os modelos/animações específicos de cada alimento da referência. Não há durabilidade inventada na ficha do item ou arrastar com mouse. O texto de ajuda mostra as teclas implementadas, em vez de reaproveitar imagens antigas com atalhos diferentes.

Compra combina JSON da carteira/inventário e, em protagonistas, dinheiro nativo. São repositórios distintos: rollback cobre falhas normais; crash entre gravações ou falha adicional ao salvar um reembolso não é uma transação atômica. A observação de munição também depende do próximo frame. Consumo usa recibo idempotente para necessidades/retorno de recipientes; cura nativa já confirmada não é reaplicada em recarga. Não há garantia de atomicidade com o save nativo do GTA. Arquivos malformados continuam protegidos pelo mecanismo existente.

## Roteiro de teste no GTA

- Posicione comida, arma, Phone, um slot vazio e material nos primeiros seis slots. Teste1..6 acima das letras com a tela fechada/aberta, tecla mantida e soltura. Confira que NumPad e Alt/Ctrl/Shift+número não consomem itens; teste também uma tecla numérica já personalizada para outro sistema.
- Durante F11, F2, radial, loja, depósito, editor e vigilância, tente usar1..6: nenhum item deve ser usado por baixo. Troque protagonista/perfil e confirme a mochila correspondente.
- Navegue até além da quarta linha. A janela deve rolar uma linha sem deslocar os atalhos absolutos. Mova itens, feche/recarregue e confira posições, incluindo saves com slots altos.
- Consuma comida/bebida/bandagem. Confira retirada de uma unidade e efeito, animação limitada, prop na mão e bloqueio de novo uso. Use a partir da tela aberta e fechada e confira o retorno da interface. Pause e tente embarcar: sem eject/congelamento ou repetição do benefício.
- Compare sono antes/depois de café/energético e de repouso na casa: bônus soma ao valor atual, respeita100 e não desfaz o descanso. Água/leite devem devolver embalagem; shaker devolve shaker sujo uma vez, inclusive depois de recarga.
- Faça as nove receitas, varie quantidade, tente sem materiais/sem espaço, remova ingredientes e feche sem fabricar. Não deve ganhar nem perder itens no preview/cancelamento. A bancada/feira antiga de caça/pesca deve continuar independente.
- Teste depósito, porta-malas e apartamento: os dois lados visíveis, unidade/pilha, capacidade, armas com acessórios e munição, fechar/reabrir e voltar a iniciar outra atividade. Nenhuma transferência parcial se a quantidade não couber.
- Compre nas lojas normais e em ponto de conveniência do editor. Confira dinheiro, quantidade e recarga imediata. Sair da loja/veículo/missão deve impedir compra. Fonte de erro de gravação só com cópia de teste dos saves; não altere permissões dos seus arquivos principais.
- Descarte completamente um tipo de arma sem deixar cópia no depósito; teste nova aquisição nativa em livre exploração, aguarde a varredura e confira uma arma e a munição nova uma vez. Compare também compra na Ammu-Nation, recarga de save antigo e original guardado, observando os limites conservadores acima.
- Faça regressão de tiros isolados/rajadas/reload, última granada, roda ativada/desativada, veículo e equipamento temporário de trabalho, F2/J/F11/teleporte e recuperação de sono0.91.

Validação automática:3541 testes aprovados (3482 anteriores +59 novos) e Release net48 sem erros/avisos. Um teste antigo de transferência foi atualizado de25 para24 para refletir a janela6x4; novos casos verificam preservação de posição alta e metadados. Layout, input do controle, animação, props e natives exigem o roteiro GTA acima; não foram executados neste ambiente.
