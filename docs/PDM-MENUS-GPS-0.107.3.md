# 0.107.3 — menus PDM sem sobreposição e destinos da empresa

O usuário confirmou HUD/menus na região da Motors, mas encontrou painel da empresa e showroom sobrepostos, sem conseguir fechar. Também informou GPS do escritório/compra confusos e necessidade de destinos ligados à empresa.

## Correção da navegação

Confirmado no código LemonUI2.2: ObjectPool.HideAll atribui Visible=false e essa atribuição dispara Closing, que pode cancelar o fechamento. O handler RPCore de Voltar cancelava o fechamento ao detectar Back/Esc, mesmo quando sua fila recusava a navegação. Isso permitia manter a tela antiga aberta durante a troca. O handler agora cancela somente se conseguiu enfileirar a ação e, nos menus PDM, somente para a tela dona do foco.

Os12menus empresariais usam um coordenador por ObjectPool: antes de abrir outra página, fecha as telas anteriores com parent=null e descarta cliques/voltas pendentes. Não depende de HideAll conseguir fechar uma página cujo handler intercepta Voltar. Uma verificação antes de ObjectPool.Process remove telas PDM órfãs e cede a uma interface externa aberta. Backspace/B continuam como navegação; **Esc fecha todo o grupo PDM**, sem chamar os pais nem executar confirmações. Fechar a interface não cancela um contrato físico em andamento. Preparações ainda não iniciadas são canceladas ao mudar de sistema/fechar; a própria transição para Preparando preserva seu carregamento.

## Menu por tarefa

O painel principal reúne Resumo, Finanças, Comprar carros/fornecedor, Estoque/showroom/vendas, Equipe/melhorias, Segurança/proteção, Trabalhos/contratos e Locais e GPS. Showroom fica dentro de Estoque/showroom/vendas. Comprar carros é acesso direto ao fornecedor empresarial.

**PDM e RPCore Motors continuam separados:** o fornecedor PDM encomenda unidades para estoque, usando caixa da empresa; a Motors compra veículos pessoais usando a carteira e registra na garagem pessoal. Encomendar no RPCore é feito no menu do escritório; o porto é para retirar pedidos pagos, não um segundo balcão de compra.

Acesso à distância: **F11 > Game Settings > Empresa PDM**, ou **Fast GPS > Jobs GPS > PDM / locais e GPS**. Consulta e GPS funcionam fora do escritório; operações que já eram presenciais continuam exigindo estar lá. Abrir o painel de longe não libera compras, equipe ou contratos à distância.

## Locais e GPS

Os destinos são agrupados na ordem do fluxo empresarial:

- Escritório e compras: escritório externo, local da encomenda empresarial e Motors pessoal, com diferenças explicadas.
- Logística: retirada do carro de entrega, conjunto/carreta, porto/coleta e recebimento/devolução de estoque.
- Showroom: vagas habilitadas internas e externas.
- Equipe e segurança: mecânico, zeladoria, postos dos guardas e câmeras físicas.
- Contratos: locadora, carregamento de suas duas unidades, devolução do conjunto, entrega/parada/banco de valores e retorno da retomada.
- Contrato atual, quando existente: cliente da entrega e ponto da retomada em andamento.

Os pontos vêm dos objetos de configuração já validados pelos sistemas, respeitando as coordenadas instaladas. Não cria outro fornecedor em coordenadas inventadas. GPS fecha o grupo de menus antes de marcar o mapa e mostra confirmação com nome/distância. Se estiver até5m do destino, marca mesmo assim e informa que já está no local. Isso distingue chegar ao marcador de não responder ao clique. Demais GPS existentes dos menus PDM usam a mesma rotina.

Mapas/rotas não iniciam contrato, compram carro, contratam equipe nem movem o jogador. Vagas internas precisam do interior disponível; guardas/câmeras precisam estar contratados/comprados para aparecer, mesmo que o local seja consultável. Rota do banco de valores pertence ao carro-forte NPC; sua compensação é automática após a entrega.

## Revisão funcional dos12sistemas

Revisados os caminhos de abertura/volta, fila de ativação, confirmação e GPS de empresa/estoque, coleta, equipe, financiamentos, retomada, locadora, showroom, serviços, valores, segurança, câmeras e seguro/cofre. Os callbacks existentes continuam ligados a ações reais e seus pré-requisitos; linhas informativas permanecem desabilitadas. Esc/voltar não geram venda, pagamento ou contratação. Regras de saldo, estoque, salários, contratos e seguros não foram substituídas por atalhos de teste.

A referência PSRP.PDMCompany foi consultada para GPS ao centro, porto/coleta, retorno e organização de módulos; o fluxo do RPCore é explicitado, sem alegar reprodução integral da PSRP. Recursos extras já documentados no guia0.107.0 — reputação/cartões, vendedor particular, carreta de6carros, patrocínio/caridade e uso manual de lavadora/bomba/vidros/luzes — **continuam pendentes**. Não há botões vazios fingindo implementá-los. Esta entrega corrige a navegação/GPS relatados e organiza os12sistemas existentes; não declara todos os extras PSRP completos.

## Teste no GTA

1. Instalar com GTA fechado. Conferir0.107.3 no log. De longe, F11 > Game Settings > Empresa PDM > Locais e GPS > Escritório e compras > Escritório PDM. Conferir confirmação e waypoint. Repetir com compra/fornecedor e conferir que aponta para o escritório, não o porto.
2. No escritório, encomendar pelo fornecedor apenas se desejar testar a compra. Marcar GPS estando nele deve avisar que já chegou. Pedido pago segue aguardando coleta; GPS do porto não cobra novamente.
3. Ir a Estoque/showroom/vendas > Showroom/atender compradores. Abrir vagas, paginar, confirmar/cancelar e voltar repetidamente com Backspace e B. Deve haver somente uma tela, sem texto sobreposto. Esc em qualquer ponto deve fechar tudo e impedir ação pendente.
4. Reabrir e alternar os12grupos de serviço, inclusive retorno das câmeras. Abrir F11 ou outra interface deve ceder o foco sem ressuscitar a empresa atrás. Confirmar que ainda abre inventário, F2 e Motors pessoal com prévia.
5. Consultar porto/retorno, locadora e sua devolução, equipe, guardas, vagas/câmeras e eventual contrato atual. Conferir coordenadas personalizadas se existirem. GPS não deve iniciar atividade.
6. Cancelar preparação de recursos e voltar, inclusive segurando Enter/Back. Nenhuma confirmação deve passar para a próxima tela; caixa/estoque devem ficar iguais quando não confirmou negócio. Conferir uma operação já em andamento após fechar/reabrir menus.
7. Se reaparecer sobreposição, apertar Esc e depois F9 e enviar RPCore.log/ScriptHookVDotNet.log da sessão, informando o último botão. Não precisa reinstalar arquivos visuais.

## Validação e preservação

4245 testes aprovados,20 novos para transferência exclusiva de foco, descarte de cliques/voltas,200trocas repetidas, fechamento geral, recuperação de órfãos, isolamento de pools, reentrância/falha de fechamento, destinos customizados/inválidos/duplicados e agrupamento. Release net48 sem erros/avisos. Natives, entrada real do controle, waypoint e renderização LemonUI ainda dependem do teste GTA acima.

Wallet, dados empresariais PDM existentes, catálogo/preços, coordenadas de configuração, personagens, corpo da troca estável, HUD, portas e prévia da Motors preservados. Escopo PDM-NAVIGATION-SCOPE-0.107.3.json. Instalador preserva saves e preferências; não reinstala OIV/RPF/YTD.
