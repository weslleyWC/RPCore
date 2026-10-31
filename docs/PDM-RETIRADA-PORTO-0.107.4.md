# 0.107.4 — retirada de encomendas PDM no porto

## Diagnóstico confirmado pelo fluxo e esclarecimento do usuário

O usuário confirmou que a compra e o débito no caixa empresarial estavam corretos. O que faltava era encontrar/coletar os carros no porto. No fluxo anterior, GPS apenas marcava o local: os carros eram criados durante uma coleta iniciada presencialmente no escritório. Ir direto ao ponto de GPS sem iniciar o contrato deixava o porto sem carros nem um marcador de atendimento. Não se trata aqui de corrigir dinheiro infinito ou alterar a origem do pagamento.

## Como retirar seus pedidos existentes

1. Instalar o update com GTA fechado. Os pedidos já pagos continuam no save; **não compre novamente** para testá-los.
2. Marcar o porto no GPS. Enquanto houver encomendas aguardando retirada, há um ícone laranja **RPCore: PDM / encomendas no porto**, visível no mapa mesmo de longe.
3. Ir ao ponto, sair do veículo e aguardar cerca de4s para a área/colisão carregar. Aproximar do marcador e apertar **E**.
4. Usar **Retirar primeiro pedido**, ou selecionar uma/duas unidades e **Iniciar retirada dos selecionados**. Confirmar. Isso não debita caixa, banco ou carteira novamente.
5. O Packer e a TRFlat são preparados num espaço verificado perto do porto. Os carros selecionados aparecem em sequência, com ícones azuis; o primeiro é tentado após1s e as próximas tentativas ocorrem a cada5s. Eles podem aparecer antes do engate, depois que a retirada foi iniciada.
6. Entrar no Packer, aproximar sua traseira da frente da carreta e usarE para auxiliar o engate. Dirigir cada carro azul até a traseira da carreta; parar, sair e usarE para carregar.
7. Voltar ao recebimento PDM indicado na rota, descarregar e aguardar a confirmação. Só então as unidades ficam disponíveis para showroom/entrega/venda. A retirada move patrimônio já comprado, sem receita ou débito novo.

A opção anterior continua: iniciar a retirada no escritório e buscar o conjunto na PDM antes de ir ao porto. O sistema decide pela posição atual validada: até5m do escritório ou até30m do porto. O marcador interativo do porto fica a3m; GPS de longe não inicia nem cria veículos.

## Refinamento implementado

- Marcador de atendimento e ícone do porto somente quando existem encomendas pendentes da empresa; limpeza ao receber todos os pedidos, trocar perfil ou descarregar o script. O marcador não interfere com a teclaE de carregamento/engate durante uma coleta ativa.
- Início da retirada pelo porto com verificação conjunta de vagas para caminhão e carreta. A âncora é derivada do ponto/heading configurado do porto; respeita locais personalizados. A busca testa chão, colisão e obstáculos. Não remove carros/objetos/pessoas para liberar espaço e não teleporta o jogador.
- Preparação solicita caminhão, carreta e modelos dos carros escolhidos. Para iniciar no porto, usa a colisão/estabilidade do próprio porto, sem esperar o estado da Motors distante. Prazo de10s; fechar/perder contexto cancela antes de criar o contrato.
- Carros de encomenda podem surgir na fase de engate quando o conjunto já está no porto; uma unidade por tentativa para distribuir a criação. Falta de modelo, modelo carregando e vaga/chão indisponíveis passam a ter avisos distintos. Identidade/registro e placas existentes continuam conferidos.
- Lista de pedidos informa que o custo saiu do caixa empresarial e que marcar GPS sozinho não inicia a coleta. Há instrução de sequência e acesso direto à retirada no fornecedor e nos detalhes do estoque. Confirmação de compra mostra caixa antes/depois e a notificação confirma o débito empresarial.
- Falhas de atendimento/encomenda/retirada têm tela com motivo e referência curta noRPCore.log. Exceções da empresa/coleta mostram o nome do erro, não só uma notificação passageira. Preparação distingue timeout de modelos e de colisão/chão. Não repete automaticamente um negócio que falhou; mensagens de diagnóstico não garantem sozinhas que um erro nativo foi reproduzido.

## Preservação financeira e de saves

**Wallet.cs, PdmData.cs e PdmLogisticsData.cs estão idênticos à0.107.3.** Mantidos débito da encomenda no caixa, compra atômica, recibos idempotentes, reserva de uma/duas unidades e recebimento sem novo pagamento. Não foi acrescentado débito automático no banco/carteira, nova moeda, migração de pedidos ou novos campos persistidos. Pedidos antigos são aceitos pelo marcador sem cobrar de novo.

A confirmação de coleta ainda exige empresa válida, pedido pendente em condição mínima, ausência de outro contrato incompatível e controle seguro do personagem. Em caso de interrupção, usar Encerrar/Recuperar conforme o estado do contrato. Pedidos aguardando coleta continuam ocupando vagas no estoque.

Personagens/troca estável, banco, inventário, HUD, exclusividade dos menus0.107.3 e portas/prévia Motors preservados. O helper compartilhado RigSpawn.cs não foi modificado; os demais trabalhos mantêm sua lógica. Não precisa reinstalarOIV/RPF/YTD. Escopo: PDM-PICKUP-SCOPE-0.107.4.json.

## Teste integrado no GTA

1. Carregar o save com os pedidos atuais. Conferir caixa/banco/carteira e quantidade antes de mexer. Ir ao ícone do porto, usarE, Retirar primeiro pedido e confirmar. Conferir conjunto, carro e ícones; se a área estiver ocupada, estacionar longe do ponto e tentar de novo. Nenhum carro de terceiros deve sumir.
2. Concluir uma retirada e conferir que só a unidade selecionada deixou de aguardar coleta. Conferir que nenhum saldo foi debitado/creditado pela retirada. Expor/vender essa unidade somente depois de recebê-la.
3. Repetir com duas unidades e também pelo escritório. Comparar mapa/engate/carregamento/retorno; permitir alguns segundos para os carros aparecerem em sequência.
4. Marcar GPS sem iniciar: deve haver marcador/atendimento, mas nenhum contrato/carregamento espontâneo. Cancelar confirmação ou preparação não consome pedidos nem dinheiro.
5. Usar perfil sem pedidos e depois voltar ao perfil atual: ícone/pedidos não devem migrar para outra empresa. Se há coleta interrompida, recuperar/encerrar antes de tentar reservar a mesma unidade novamente.
6. Testar Esc/Backspace/B, F11, F2 e Motors/showroom; a correção de sobreposição deve continuar. Conferir um trabalho externo à PDM.
7. Se houver falha, preservar a referência da tela e enviarRPCore.log/ScriptHookVDotNet.log da mesma sessão, além de imagem do ponto. Modelos, colisão, engate, IA e ícones ainda dependem desta validação dentro do GTA.

## Validação e limites

4269 testes aprovados,24 novos para escolha escritório/porto/distância inválida, seleção/estado dos pedidos, não alterar saldos ao planejar/retirar, recebimento único, erro de gravação, pedido antigo recarregado e âncora em porto/heading personalizados. Release net48 sem erros/avisos. Native GTA não executado no cloud; não se declara teste local de posição física/criação dos veículos aprovado.

Comparação estática PSRP PDM anterior continua válida para escritório/coleta no porto/carregamento/retorno. **Iniciar pelo marcador do porto é uma melhoria própria do RPCore**, não alegada como cópia exata da PSRP. Extras empresariais ainda pendentes documentados em PDM-MENUS-GPS-0.107.3.md não foram simulados com menus vazios nesta entrega.
