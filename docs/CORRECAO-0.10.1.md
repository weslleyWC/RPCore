# RPCore 0.10.1 — correção do pátio LS GAS

Status posterior: o usuário confirmou esta correção funcionando no GTA e solicitou a próxima fase. A [0.11.0](FASE-4.3-0.11.0.md) acrescenta transporte prisional regular; as informações abaixo registram a entrega original.

O teste do usuário na 0.10.0 bloqueou a contratação com “Patio LS GAS ocupado”. O código anterior tentava um único par de posições e rejeitava qualquer veículo a 12 metros do tanque/8 metros do cavalo, mesmo fora das vagas. O print não identifica qual entidade disparou o bloqueio.

## O que mudou

- FuelDeliveryJob.cs chama a nova seleção de vagas em RigSpawn.cs/RigSpawnData.cs. Tenta primeiro a posição Trailer já configurada, depois alternativas próximas, sem regravar FuelDelivery.json. São 35 pares de posições, com deslocamento do tanque de até 40 metros do ponto original e cavalo 19 metros à frente. Mesma direção configurada e dois espaços aprovados antes de criar o conjunto.
- A verificação de ocupação usa os limites reais dos modelos, orientados conforme a direção do veículo, com margem de 0,5 metro. Um veículo/pedestre ao lado não bloqueia toda uma área circular. Veículos/pessoas/props que ocupem de fato o espaço continuam bloqueando a vaga, inclusive o jogador.
- Verificação do chão no centro e cantos, com rejeição de desníveis maiores que 0,6 metro e altura incompatível com o pátio. A altura de criação considera a parte inferior do modelo para não inserir as rodas no chão.
- Sondagens síncronas de colisão em três alturas verificam paredes, pilares e cenário estático antes da criação. Resultado inválido não é tratado como espaço livre. Não se presume conclusão imediata de uma sondagem assíncrona.
- Se nenhuma posição passar, o contrato continua sem criar veículos, libera o turno e registra no RPCore.log as contagens occupied/ground/world e a posição escolhida quando houver. Não apaga nem desloca trânsito, veículos pessoais ou objetos do cenário.
- Versão 0.10.1 em AssemblyInfo.cs/CoreMenu.cs/Main.cs/VisualAssetDefaults.cs. tests/RPCore.Tests.csproj vincula a geometria real; tests/RigSpawnTests.cs acrescenta 14 casos. Documentação, auditoria e pacote atualizados.

Estados, engate, descarga, rota, pagamento, bônus e recolha da 0.10.0 permanecem iguais. F2, saves e configuração existente são preservados. Não precisa apagar FuelDelivery.json nem reinstalar OIV/YTD/DLC. Instruções do trabalho em [fase 4.2](FASE-4.2-0.10.0.md).

## Validação

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI SHVDN3 2.2: zero erros/avisos. 309 testes passaram (295 anteriores e 14 novos). Casos de regressão: carro/pedestre ao lado dentro do raio antigo sem ocupar vaga, veículo cruzado na vaga, direções, alturas, limites de modelo assimétricos, primeiro ponto bloqueado com alternativa livre, todas as alternativas bloqueadas, limite de tentativas, distância do par e preservação da configuração. Os testes validam geometria/seleção usadas pelo runtime; não executam física/colisões nativas do GTA.

## Teste no GTA

1. Feche o jogo e instale esta atualização pelo instalador da pasta extraída, usando seu caminho real do GTA. Mantenha os INIs/JSONs/saves existentes.
2. No mesmo local do print: F10 → Trabalhos → Transporte de combustivel → Iniciar contrato. Confira os blips do Hauler e tanque; eles podem aparecer numa vaga próxima à original. Confirme ausência de colisões/veículo preso e espaço para manobra.
3. Engate e faça uma descarga/retorno completo para confirmar os pagamentos e a recolha. Se a vaga original estiver ocupada por um veículo próximo, tente iniciar novamente e confira a escolha de alternativa, sem apagar o veículo existente.
4. Se ainda não criar o conjunto, envie o trecho “LS GAS spawn:” do arquivo scripts/RPCore/RPCore.log. Ele agora distingue ocupação, chão e colisão do cenário. As colisões do seu GTA/mods e o acesso das vagas precisam deste teste real.
