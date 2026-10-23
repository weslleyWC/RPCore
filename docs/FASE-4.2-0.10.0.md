# Fase 4.2 — RPCore 0.10.0: transporte de combustível

Correção posterior: o usuário reportou bloqueio “Patio LS GAS ocupado”. A [0.10.1](CORRECAO-0.10.1.md) substitui os raios amplos por vagas alternativas e verificações do espaço dos modelos; o ciclo do trabalho abaixo continua igual. O turno completo ainda aguarda teste real.

Reposição de lojas 0.9.0 aprovada pelo usuário no GTA: cinco entregas e pagamento corretos. Pizza, Post OP, Uber e valet também já foram aprovados. Esta entrega acrescenta o contrato LS GAS, com implementação própria baseada no ciclo e pontos da PSRP. Transporte prisional e piloto ficam para próximas entregas.

## Como jogar

1. F10 → Trabalhos / contratos → Transporte de combustivel → GPS: deposito LS GAS. Vá a pé ao marcador no porto (481.9995, -2172.934, 5.918277) e pressione E para abrir o contrato. Escolha Iniciar contrato.
2. Entre no Hauler azul fornecido. O tanque aparece no pátio, no ponto da referência (524.8837, -2140.734, 5.986342; direção 170.0806). Nenhum caminhão pessoal é necessário. Se o pátio estiver ocupado, o contrato pede para liberar espaço; não apaga trânsito ou veículos pessoais.
3. Alinhe a traseira do Hauler com a frente do tanque e dê ré devagar para o engate nativo. E ajuda quando os dois estão parados, próximos (até 6 metros entre as extremidades) e alinhados (até 35 graus). O contrato só avança quando o GTA confirma que o tanque certo está engatado ao Hauler certo; não usa engate genérico de entidade nem teleporta o jogador.
4. Siga a rota até o posto. Cada contrato atende um posto sorteado entre os oito mais próximos da base, no catálogo de 21 pontos da referência. As distâncias usadas são em linha reta. CandidateStations pode ampliar a seleção para postos mais distantes.
5. Pare o conjunto perto do ponto: caminhão e tanque a até 25 metros, ambos abaixo de 0,5 m/s. Permaneça como motorista, mantenha engatado e desligue o motor com sua tecla configurada (X no padrão). Pressione E ou buzina para descarregar. Aguarde seis segundos parado. Movimento, desengate, saída do motorista, motor ligado ou abertura de menu interrompem e exigem reiniciar a descarga. Não é necessário deixar o tanque no posto.
6. A descarga concluída paga no banco 450 + parte inteira da distância base→posto × 0,08, uma única vez. O GPS aponta de volta à LS GAS. Leve o mesmo conjunto, pare os dois a até 40 metros da base e desligue o motor. E/buzina confirma a devolução e paga o bônus único de 250. Saia do caminhão para permitir a recolha dos dois, vazios e parados.

Se os controles de veículo estiverem desativados, o menu do contrato oferece Motor do caminhao: ligar/desligar. As instruções no posto/base mostram a tecla configurada ou essa alternativa. O abastecimento pessoal dos veículos continua com seu funcionamento anterior.

Desengatou no caminho? O GPS volta ao tanque; reengate o mesmo conjunto para continuar. O contrato não faz reengate automático à distância. Segure End por um segundo ou encerre pelo menu para cancelar. Descarga já paga permanece; bônus depende do retorno. Morte, missão, perda/destruição de uma das partes e troca de perfil encerram o contrato. O turno ativo não retoma após reload.

## Progresso, compatibilidade e limites

Wallet.json do perfil recebe progresso/recibos em Jobs/tanker, usando o checkpoint bancário existente. Falha de gravação mantém o pagamento pendente, sem avançar para retorno/conclusão; na descarga a tentativa é repetida enquanto o conjunto permanece elegível. Saldo/progresso anteriores e campos desconhecidos são preservados. O fluxo não altera inventário pessoal, litros vendidos nos postos ou estoque finito de comerciantes: representa o serviço de transporte de uma carga. Não há mangueira/fluido simulado nem explosões roteirizadas; dano e física são do GTA, sem invencibilidade forçada.

São criados somente Hauler/Tanker nativos e seus blips. O caminhão recebe combustível para começar o serviço. Garagem e armazenamento identificam ambas as partes como veículos de trabalho. A recolha espera os dois veículos vazios e parados; aguarda também se houver engate com veículo de terceiros. Ao descarregar o script, o conjunto ocupado é liberado para a gestão do jogo em vez de ser apagado sob o jogador. Somente recursos próprios são retirados.

Celular F2 e resolução de conflito de garagem da 0.9.0 permanecem. Nenhum OIV/YTD/RPF/DLC/dlclist foi alterado. O menu usa PlaceholderBanner já instalado, como a referência; sem textura disponível mantém o fallback. Os cinco contratos anteriores continuam registrados no mesmo controle de turno exclusivo.

## Configuração

Arquivo novo scripts/RPCore/Jobs/FuelDelivery.json, instalado apenas se ausente, com defaults também embutidos na DLL. Defaults: Drops 1, BasePay 450, ReturnBonus 250, PayPerMeter 0.08, CandidateStations 8 e UnloadSeconds 6. CandidateStations aceita 1–100; a lista real limita a quantidade disponível. UnloadSeconds aceita 3–30. Drops deve permanecer 1; os demais limites financeiros/coordenadas seguem DeliveryOptions. Start/Depot/Trailer e Addresses podem ser personalizados. O cavalo é criado 19 metros à frente do ponto Trailer, com a mesma direção; mantenha espaço livre para ambos ao editar.

Arquivo inválido é preservado e usa fallback; válido mantém overrides/campos desconhecidos. FuelDelivery.json é independente do catálogo de abastecimento Fuel/Stations.json: mudar um não reescreve o outro. Não há migração destrutiva de saves/configurações.

## Arquivos alterados

- TankerData.cs, FuelDeliveryJob.cs e config/Jobs/FuelDelivery.json: configuração, seleção de posto, estados testáveis, criação do conjunto, confirmação de engate real, descarga, pagamentos, retorno, cancelamento e limpeza.
- DeliveryCargo.cs / JobVehicles: múltiplos veículos reservados; recolha do conjunto sem apagar uma parte enquanto a outra está ocupada/movendo ou engatada a terceiros. Fluxo anterior de veículos individuais mantido.
- Main.cs, SettingsSystem.cs e CoreMenu.cs: sexto contrato, GPS e descrições atualizadas.
- AchievementSystem.cs: conquistas de primeira descarga e retorno, acrescentadas quando ausentes.
- Visuals.cs e config/Visuals/Bindings.json: banner LS GAS com recurso original e fallback.
- AssemblyInfo.cs, Main.cs, CoreMenu.cs e VisualAssetDefaults.cs: versão 0.10.0.
- RPCore.csproj e tests/RPCore.Tests.csproj: default embutido e vínculo da lógica real aos testes; tests/TankerTests.cs acrescenta 39 casos.
- scripts/audit-psrp-systems.py e documentação: auditoria dos 21 pontos/postos e depósito, relatórios, instruções e pacote. A referência decompilada não foi transplantada.

## Validação e teste solicitado no GTA

Build Release net48 com SHVDN Enhanced 1.1.0.6 e LemonUI SHVDN3 2.2: zero erros/avisos. 295 testes aprovados, sendo 256 anteriores e 39 novos. Cobrem engate obrigatório, descarga temporizada/interrompida, relógio com overflow, elegibilidade de motorista/motor/duas partes, assistência de engate, cancelamento em cinco estados, retry/rollback/replay de pagamentos, um bônus, progresso anterior e exclusão entre seis trabalhos. A auditoria estática confere todos os 21 postos, contratação, retorno e spawn do tanque na fonte PSRP. Nativos/física não foram executados nesta nuvem.

1. Contrate na LS GAS e confira banner, blips, spawn, porta do Hauler e espaço para manobra. Faça um engate de ré e teste a ajuda de E alinhado/parado.
2. Faça uma viagem completa, desligue o motor, descarregue seis segundos e confira o crédito bancário. Retorne com os dois, confirme o bônus uma vez e saia para recolher. Confira que o tanque não desaparece enquanto você dirige.
3. Durante a descarga, teste ligar o motor, mover o caminhão, sair, abrir F2/F10 e desengatar. Nenhum desses casos deve pagar carga incompleta. Recomece a descarga parado.
4. Desengate no trajeto e reengate; o GPS deve voltar ao posto/base depois do engate. Confirme que outro veículo/carretas não substituem o conjunto do contrato.
5. Teste cancelamento antes e depois da descarga: paga apenas o já confirmado. Teste morte, destruição, troca de perfil e reload; nenhum turno deve bloquear o próximo trabalho. Confira saves/progresso após recarregar.
6. Faça regressão rápida dos cinco empregos anteriores, F2/celular, garagem, combustível pessoal e porta-malas. Confira que veículos pessoais permanecem intactos. Layout de postos, trânsito, rampas/altura, engate e física de carreta dependem deste teste real no seu GTA.
