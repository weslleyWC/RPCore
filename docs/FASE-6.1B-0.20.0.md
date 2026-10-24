# Fase 6.1B — RPCore 0.20.0: coleta PDM no porto

A 0.19.0 foi testada e aprovada pelo usuário, que também pediu refinamento posterior. Esta subentrega acrescenta logística dos pedidos pagos da PDM e exposição externa de uma unidade. Mantém o ciclo anterior de empresa/caixa/cliente; não declara concluída toda a PDM. Fases 8 (interface e mecânicas dos indicadores/menus) e 9 (todos os trabalhos) permanecem na sequência acordada.

## Instalar

Feche o GTA. Extraia RPCore-update-0.20.0.zip e abra o PowerShell **na pasta extraída onde está Install-RPCore.ps1**:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Use seu caminho real. A DLL já está compilada. O instalador faz backup da anterior, preserva saves/configurações e acrescenta Jobs/PdmLogistics.json somente se ausente. F2/celular, F11/teleporte e recursos PSRP/OIV/YTD/RPF/DLC já instalados permanecem. Não precisa reinstalar a parte visual.

## Testar o percurso completo

1. **F10 → Trabalhos → PDM / empresa e entregas**. Use GPS e vá a pé ao escritório externo. Empresa já adquirida e caixa/estoque da 0.19.0 continuam seus. Quem ainda não adquiriu segue o guia da 0.19.0: aquisição 25000 do banco e aporte de capital.
2. **Comprar estoque / fornecedor**: novos pedidos agora aparecem **[PORTO]**. A compra debita o caixa uma única vez. Dois Asea custam 16000; precisa ter esse valor no caixa, além de espaço no estoque. Os carros comprados na 0.19.0 continuam **[PRONTO]** e não exigem coleta retroativa.
3. No escritório, **Coleta no porto / transportador → Coletar 1 pedido** ou **Coletar 2 pedidos**. Também há entrada própria no menu de trabalhos. Escolhe os primeiros pedidos pendentes com motor/carroceria >=300. Não cobra novamente. Não inicia junto de outro trabalho físico nem de entrega/coleta PDM pendente.
4. Localize o **Packer** e a **TRFlat** pelos blips azuis próximos da PDM. Entre no Packer. Dê ré alinhado: traseira do caminhão perto da frente da carreta. **E** auxilia o engate quando ambos estão parados/alinhados. O GPS muda para o porto quando o conjunto fica engatado.
5. Dirija o conjunto até o porto indicado. Os carros correspondentes aos pedidos pagos aparecem com blips azuis quando você e a carreta se aproximam. Se a vaga estiver ocupada, afaste o conjunto da área de spawn; tenta novamente a cada cinco segundos sem apagar carros do ambiente.
6. Estacione o conjunto engatado. Entre em **cada carro do pedido**, dirija-o até a traseira da carreta, pare e **saia**. Fique perto do carro, pressione **E** e ele é fixado no espaço da TRFlat. Repita no segundo carro. Não precisa dirigir sobre uma rampa: o carregamento é assistido, como etapa própria desta implementação.
7. Após carregar todos, o GPS marca o recebimento da PDM. Volte com o conjunto e pare perto do ponto. **E** inicia descarga de cinco segundos. Mantenha caminhão/carreta parados, engatados, com todos os carros presos e vazios. Abrir menu/pausa, mover/desengatar/interromper exige iniciar o recebimento de novo.
8. A confirmação transforma os pedidos em **[PRONTO]**, sem nova compra, sem pagamento fictício por transportar seus próprios carros e sem creditar venda. Carros físicos da coleta são removidos após checkpoint de chegada; conjunto aguarda ficar vazio/parado para ser recolhido. Saia dele antes de outro contrato.
9. No escritório, **Estoque → carro pronto → Expor no patio** exibe uma unidade no pátio externo. Ela continua no estoque, congelada/trancada/protegida e indisponível como carro pessoal. **Recolher exposicao externa** remove só esse temporário. Expor outro carro substitui a exposição anterior; entrega/reparo/coleta recolhem a exposição antes de usar o estoque. Não é o showroom completo com múltiplos espaços.
10. **Entregar ao cliente** usa o ciclo aprovado na 0.19.0: retirar o carro, ir ao cliente, parar/sair/desarmar e E para passar as chaves. Só essa venda credita a receita empresarial. Asea sem danos continua com venda de 11200; saque transfere caixa para banco como antes.

## Cancelamento e recuperação

Segure End por um segundo, fora de menus, ou escolha Encerrar coleta. Pare e saia do caminhão/carreta e dos carros do pedido. Aguarda liberação dos veículos; grava danos antes da limpeza. Não remove veículos pessoais/ambiente nem veículos ocupados. Pedidos continuam pagos e **[PORTO]**, sem reembolso porque continuam pertencendo à empresa. Danos registrados reduzem a condição; se necessário, repare pelo estoque antes de nova coleta.

Morte, procurado/missão, tempo de 30 minutos, perda/destruição do conjunto/carga ou desprendimento de carro carregado solicitam o mesmo encerramento. Desengatar a carreta mantém o percurso pendente para reengatar, sem liberar descarga. Uma conexão com trator/carreta alheios impede limpar esse conjunto até desfazê-la.

Collection em Wallet.json/Pdm guarda ID, unidades selecionadas, caminhão/carreta com handle/modelo/placa, handles dos carros e Arrived. A recuperação só reconhece veículos com identidade correspondente. Se ocupados/movendo, pare/saia e use **Recuperar coleta interrompida**. Se não houve chegada confirmada, mantém pedidos pagos no porto; se a chegada já foi salva, conclui recebimento e libera estoque. Falha de gravação conserva a operação para retry. Ausência de entidades depois de reiniciar o mundo usa a condição já persistida; não reconstrói danos que não chegaram a ser salvos.

Confirma chegada antes de remover carga, e remove carga antes de marcar estoque pronto. Evita liberar o item para venda enquanto sua representação da coleta ainda existe. Transferências, venda anterior e recebimento são operações distintas. IDs de recibos recentes impedem repetir operações; exceções restauram o snapshot anterior.

## Código e referência

- PdmLogistics.cs: menu, spawn livre do conjunto, engate, coleta/carga, retorno/descarga, cancelamento/recuperação, exposição externa.
- PdmLogisticsData.cs: opções/slots, identidades, transações e máquina de estados testáveis fora do jogo.
- PdmData.cs: campos aditivos AwaitingCollection/Collection e bloqueios de integração. AwaitingCollection ausente é false, preservando estoque antigo pronto.
- Wallet.cs, Main.cs, PdmCompany.cs: integração e compras novas como pedido pendente; menu geral recebe o contrato de logística. Pdm.json anterior permanece igual.
- Jobs/PdmLogistics.json: posições, modelos, offsets, tempo de descarga/prazo. Configuração inválida usa padrões embutidos sem apagar o arquivo personalizado.

Estudo estático de PSRP/PDMCompany.cs: SpawnOrderVehicles, TwoCarCollectionStates, Packer/TRFlat, offsetFront/offsetback, retorno e estacionamento externo. Porto (1280.554, -3209.737, 5.489246), carreta (-50, -1080, 26.9), retorno (-48.12541, -1075.025, 26.7742), exposição (-50.69996, -1116.535, 25.44), offsets (0, ±3, 1.055) são da referência. Caminhão fica à frente da carreta com busca de vaga orientada, adaptação para evitar sobreposição. Busca de chão/colisão pode ajustar a posição real. Implementação própria, sem transplante do código decompilado.

Limites: coleta de um/dois carros, fixação assistida, uma exposição externa. Não inclui TR2/cinco carros, rampas ajustáveis, showroom inteiro, equipe/salários/upgrades, financiamento/repossessão, aluguel ou logística de cofre. Esses subsistemas continuam em próximas subentregas da fase 6. Não altera IA/polícia, scripts de outros mods ou arquivos OpenIV.

## Validação e regressão no GTA

Build net48 com Enhanced 1.1.0.6/LemonUI 2.2 sem erros/avisos. 745 testes aprovados, incluindo 51 novos de pedidos/coleta/recebimento/replay/rollback/compatibilidade/tempo. Auditoria estática verifica referência/configuração; instalador exercitado em diretório simulado. Nenhum desses testes executa GTA.

No GTA, conferir especialmente: espaço/engate do conjunto na PDM; acesso rodoviário ao porto; spawn e dirigibilidade dos dois carros; altura/alinhamento dos slots; segunda carga; retorno/descarga; liberação/remoção do conjunto somente vazio; repetição de contrato; cancelamento carregado; reload antes/depois do recebimento; exposição sem bloquear pedestres e entrega do mesmo item após recolher. Testar estoque antigo, danos/reparo, compra/caixa/venda/saque anteriores, garagem pessoal rejeitando ativos de trabalho, outro trabalho físico bloqueado, streamer paralelo e F2/F11. Os pontos físicos precisam da sua validação na instalação real.
