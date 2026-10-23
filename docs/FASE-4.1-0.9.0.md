# Fase 4.1 — RPCore 0.9.0: reposição de lojas

Status posterior: o usuário concluiu as cinco entregas e confirmou pagamento correto no GTA. Próxima entrega: [fase 4.2 — transporte de combustível](FASE-4.2-0.10.0.md). O restante registra a entrega original.

Pizza, Post OP, Uber e valet foram confirmados pelo usuário no GTA. Esta entrega acrescenta o contrato de reposição; transporte de combustível, transporte prisional e piloto continuam para próximas entregas. Nenhuma mudança no OIV, YTD, RPF, DLC ou dlclist.

## Como trabalhar

1. F10 → Trabalhos / contratos → Reposicao → GPS. Vá a pé ao depósito de Davis (30.10219, -1761.373, 29.53956) e inicie com E pelo menu do contrato.
2. Entre como motorista no Mule2 criado para o trabalho. Pare na base, a até 25 metros do ponto, saia e vá à traseira desarmado. E inicia o carregamento: permaneça por três segundos. São cinco caixas lógicas do contrato; não ocupam nem alteram seu inventário pessoal.
3. Siga o GPS. A rota padrão seleciona cinco lojas distintas entre as oito mais próximas do depósito, no catálogo de 20 conveniências, e ordena as paradas por proximidade. As distâncias são em linha reta; não há otimização de ruas.
4. Pare o caminhão perto do destino (até 45 metros). Na traseira, E retira uma caixa. Leve-a ao ponto público da loja e pressione E; permaneça durante a descarga de um segundo. Cada entrega paga no banco 180 + parte inteira da distância do trecho × 0,06.
5. Após cinco descargas, devolva o mesmo caminhão à base. Pare como motorista a até 15 metros e pressione E para receber o bônus único de 300. Saia para permitir a recolha segura do caminhão vazio.

Segure End por um segundo ou cancele no menu do contrato. Entregas já pagas permanecem; cancelamento não paga bônus. Só um trabalho pode estar ativo. Sessões ativas não são retomadas após recarregar o script; créditos e recibos ficam no Wallet.json do perfil, no progresso supply.

No ponto de descarga ativo, até 4,5 metros, E dá prioridade ao trabalho para não abrir a compra da loja. Após a descarga, a compra normal volta; outras lojas continuam disponíveis. O contrato não implementa estoque finito dos comerciantes.

## Teclas e configurações preservadas

O padrão novo do telefone é F2 e sua configuração F2 existente é preservada. F1 explicitamente configurado em outro perfil continua valendo. Se a garagem estiver na mesma tecla do telefone ou das configurações, usa O em memória; em conflito também com O, tenta F2 e NumPad0. O INI não é regravado. Teclas personalizadas sem conflito continuam iguais. As instruções do telefone/Uber mostram a tecla configurada.

Configuração nova: scripts/RPCore/Jobs/StoreDelivery.json, instalada só se ausente, com fallback embutido na DLL. Defaults: Drops 5, BasePay 180, ReturnBonus 300, PayPerMeter 0.06, CandidateStores 8 e LoadSeconds 3. CandidateStores deve ser pelo menos Drops e no máximo 100; LoadSeconds entre 2 e 20; Drops entre 1 e 30, com endereços distintos suficientes. JSON inválido fica preservado e usa fallback. O catálogo deste contrato é independente de Shops.json: personalizações de coordenadas devem ser feitas também no JSON do trabalho.

## Arquivos alterados

- FreightData.cs e config/Jobs/StoreDelivery.json: defaults, rota, reserva local da interação e resolução de conflito de teclas.
- JobData.cs: etapa opcional de carregamento, mantendo os estados e o fluxo anterior dos outros contratos.
- DeliveryJobSystem.cs: adaptador StoreDeliveryJob, Mule2, carga na base, caixa na mão, entregas, retorno, crédito e limpeza limitada aos recursos criados pelo contrato. Proteções de garagem/porta-malas existentes também abrangem o caminhão.
- Main.cs e SettingsSystem.cs: registro do quinto trabalho na central e GPS.
- ShopSystem.cs: prioridade da descarga somente no destino reservado.
- AchievementSystem.cs: duas conquistas de reposição, acrescentadas quando ausentes.
- Cfg.cs, Visuals.cs, config/Visuals.ini e config/Vehicles.ini: telefone F2, garagem O como padrão novo e resolução em memória de conflitos existentes.
- PhoneSystem.cs e UberJob.cs: instruções com tecla real e acesso genérico aos contratos.
- config/Visuals/Bindings.json: banner de reposição usando PostOpBanner já instalado; fallback embutido em Visuals.cs.
- AssemblyInfo.cs, CoreMenu.cs, Main.cs e VisualAssetDefaults.cs: versão 0.9.0.
- RPCore.csproj, tests/RPCore.Tests.csproj e tests/FreightTests.cs: configuração embutida e 30 novos casos de teste.
- scripts/audit-psrp-systems.py e docs: comparação das coordenadas, validação e instruções. Fontes de referência não foram transplantados.

Não há exclusão de carros de trânsito para abrir espaço, remoção de NPCs do ambiente, alteração global da polícia ou substituição de saves. A posição de spawn procura espaço próximo da base; pode ser necessário desobstruir o local no jogo. O caminhão criado recebe combustível e propriedade para dirigir sem arrombamento; o veículo pessoal não é usado.

## Validação e teste no GTA

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI SHVDN3 2.2: zero erros/avisos. Todos os 256 testes passaram, incluindo 30 novos: carga obrigatória, rota, cinco entregas, recuperação de caixa, falha de pagamento, cancelamento, um único bônus, preservação de progresso e conflitos de tecla. Auditoria estática: os 20 pontos de loja e o depósito estão presentes na referência PSRP. Isso não executa nativos nem comprova o comportamento visual no jogo.

Teste solicitado:

1. Confirme telefone F2 e, se antes também estava no F2, garagem em O; F11/J/I continuam funcionando.
2. Inicie o contrato em Davis, confira ícone/GPS/banner e spawn sem colisão. Entre no caminhão, carregue na traseira e teste interromper o carregamento andando/entrando no veículo.
3. Faça as cinco lojas: retirar caixa, animação/prop, ponto do lado público, E sem compra acidental e compra normal depois de entregar. Confira crédito bancário uma vez por descarga.
4. Volte à base: confirme os 300 uma vez, saia do caminhão e confira recolha sem afetar veículos pessoais.
5. Teste cancelamento antes/depois de uma descarga, morte, perda do caminhão e troca de perfil; nenhum turno ou bloqueio de loja deve ficar preso. Confira saldo/progresso após recarregar.
6. Faça regressão de pizza/Post OP/Uber/valet e confirme que seus saves e os recursos visuais instalados continuam corretos. Portas, alcance da traseira, IA, trânsito e interação com outros mods dependem deste teste real.
