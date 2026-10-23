# Fase 4.3 — RPCore 0.11.0: transporte prisional regular

Status posterior: o usuário confirmou este transporte funcionando no GTA. A [fase 4.4 — piloto](FASE-4.4-0.12.0.md) é a entrega seguinte. As informações abaixo registram a entrega original.

O usuário confirmou a 0.10.1 funcionando após a correção de vagas LS GAS. Esta entrega implementa o transporte regular Mission Row → Bolingbroke → Mission Row. Suborno, desvio para fuga, emboscada e cutscenes alternativas da PSRP não fazem parte deste contrato; continuam pendentes. Piloto permanece para a próxima entrega de empregos. Não se afirma paridade com todos os eventos da referência.

## Como trabalhar

1. F10 → Trabalhos / contratos → Transporte prisional → GPS: delegacia Mission Row. Vá a pé ao ponto público (424.1417, -1014.757, 29.28864), sem procurado/combate/missão. Pressione E e escolha Iniciar contrato.
2. Retire o PBus azul criado no estacionamento ou numa vaga próxima. O ponto-base do veículo é o da referência (449.2777, -1017.897, 28.78852; direção 89.13288). A busca tenta 35 posições próximas, verificando o tamanho do ônibus, chão e colisões; não apaga veículos estacionados. Se falhar, consulte “Prison bus spawn” no RPCore.log.
3. Entre como motorista, pare perto de onde o ônibus foi criado (até 25 metros) e pressione E. Aguarde quatro segundos parado para conferir a custódia. Sair do banco do motorista, mover/afastar o veículo, pausar ou abrir menu interrompe a conferência; pressione E novamente parado na retirada.
4. Quatro detentos próprios são inicializados em assentos livres do ônibus, como no fluxo de partida da referência. Não há fila de NPCs andando para embarcar nesta entrega. O contrato só libera a rota após confirmar que cada detento está vivo no assento correto do PBus. Nenhum NPC de ambiente ou jogador é colocado no veículo.
5. Siga o GPS até a recepção externa de Bolingbroke (1851.775, 2608.431, 45.89056). Pare a até 18 metros do ponto como motorista, com velocidade abaixo de 0,5 m/s; E/buzina inicia o desembarque. Use o lado público: não precisa entrar pelo portão da prisão. Não há imunidade nem limpeza automática de estrelas.
6. Aguarde os quatro saírem fisicamente do ônibus. A custódia só é aceita com todos vivos, fora de qualquer veículo, a menos de 45 metros do destino e 35 metros do ônibus; o motorista deve permanecer parado com o PBus a até 25 metros do ponto. E tenta novamente a saída dos que ainda estiverem no ônibus. Há até dois minutos para completar o desembarque; não há teleporte de saída nem fade para simular entrega.
7. A entrega confirmada paga no banco 1600 + parte inteira da distância base→prisão × 0,12. É um pagamento por transporte, não quatro pagamentos por detento. O GPS passa a Mission Row. Devolva o mesmo ônibus sem passageiros, parado a até 25 metros do ponto público e como motorista. E/buzina paga o bônus único de 400. Saia para a recolha segura do ônibus vazio.

Segure End por um segundo ou encerre pelo menu para cancelar. Custódia já paga permanece; cancelar não paga bônus. Sair do ônibus permite até 60 segundos para voltar como motorista. Morte, missão, perda/destruição do ônibus, perda/morte de detento antes da custódia, saída de detento antes do destino, troca de perfil e abandono encerram o contrato. Turno ativo não retoma após reload.

## Compatibilidade e configuração

Arquivo novo scripts/RPCore/Jobs/PrisonTransport.json, instalado apenas se ausente, com fallback embutido na DLL. Defaults: Prisoners 4 (aceita 1–8, limitado também aos assentos reais do PBus instalado), LoadSeconds 4 (2–15), BasePay 1600, ReturnBonus 400, PayPerMeter 0.12. Start/Spawn/Destination permitem coordenadas personalizadas. Valores financeiros têm limites compatíveis com os demais trabalhos. JSON inválido é preservado e usa fallback; válido mantém overrides/campos desconhecidos.

Wallet.json do perfil recebe Jobs/prison, recibos, quantidade de transportes e turnos concluídos. Usa o checkpoint existente de saldo bancário/progresso; falha de gravação mantém o pagamento pendente. No destino, mantenha a custódia e o ônibus no ponto até a confirmação. Não altera inventário pessoal, saves nativos ou os formatos anteriores. Não há contagem de estrelas/avaliação Uber para esse emprego.

Só são criados o ônibus, os detentos e os blips do contrato. Propriedade/combustível são atribuídos ao ônibus criado, sem roubo/arrombamento. O veículo fica reservado contra garagem/armazenamento. Detentos criados são desarmados, têm eventos temporários bloqueados e relacionamento civil aplicado individualmente para a custódia. Não muda relações globais, polícia, procurado, tarefas do jogador ou NPCs de ambiente. Ônibus/detentos não são tornados invencíveis.

Após a entrega, NPCs próprios se afastam e são retirados pelo coletor existente. No cancelamento recebem ordem de saída antes da recolha. O ônibus não é apagado enquanto ocupado. Celular F2, resolução de conflito de garagem, contratos anteriores e recursos OIV/YTD/RPF/DLC continuam iguais. Banner PSRPBANNER1 usa as texturas instaladas; sem recurso, o fallback textual permanece.

## Arquivos alterados

- PrisonData.cs, PrisonTransportJob.cs e config/Jobs/PrisonTransport.json: configuração, estados, conferência de quatro assentos, NPCs próprios, destino, desembarque físico, pagamento, retorno, cancelamento e limpeza.
- RigSpawn.cs: nova seleção para um veículo com a geometria e as sondagens já usadas. A busca de pares LS GAS permanece igual.
- Main.cs/SettingsSystem.cs: registro do sétimo contrato, central e GPS F11.
- AchievementSystem.cs: conquistas da primeira custódia e do retorno, acrescentadas quando ausentes.
- Visuals.cs/config/Visuals/Bindings.json: banner prisional com textura existente e fallback.
- AssemblyInfo.cs/Main.cs/CoreMenu.cs/VisualAssetDefaults.cs: versão 0.11.0.
- RPCore.csproj/tests/RPCore.Tests.csproj: default embutido e vínculo da lógica real aos testes; tests/PrisonTests.cs adiciona 31 casos.
- scripts/audit-psrp-systems.py: comparação dos três pontos com JobDeliveryDriver.cs da PSRP; documentação, relatórios e distribuição atualizados. Não houve transplante do fonte decompilado.

## Validação e teste solicitado no GTA

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI SHVDN3 2.2: zero erros/avisos. 340 testes passaram, sendo 309 anteriores e 31 novos. Novos casos: tempo obrigatório da conferência, interrupção, assentos incompletos, origem/destino, desembarque parcial, elegibilidade de motorista/velocidade/distância, cancelamento em seis estados, retry/rollback/replay financeiro, um pagamento e um bônus, preservação do progresso de empregos anteriores e exclusão entre sete contratos. Auditoria estática confere os três pontos no fonte PSRP. Testes de dados não executam IA ou nativos do GTA.

1. Contrate em Mission Row, confira marcador/GPS/banner, vaga do ônibus e espaço para sair sem colisão. Veja os quatro detentos dentro após E e conferência parada.
2. Interrompa a conferência movendo/saindo/abrindo F2 e retome: nenhum NPC ou pagamento duplicado. Faça o trajeto até a recepção externa de Bolingbroke.
3. Pare, E/buzina, espere os quatro saírem pelas portas. Se houver bloqueio, libere o lado da porta e use E novamente. Confira que não paga enquanto alguém estiver dentro; confirme o crédito bancário uma única vez ao completar.
4. Retorne vazio a Mission Row, confirme 400 de bônus uma vez, saia e confira recolha. Verifique saldo/progresso após recarregar o script.
5. Teste cancelamento antes/depois da entrega, abandono, morte, perda do ônibus/detento e troca de perfil. Não deve restar turno preso nem mexer no veículo pessoal.
6. Faça regressão dos seis trabalhos anteriores, especialmente engate/vagas LS GAS, além do telefone F2 e saves. Se PBus personalizado mudar assentos/portas ou outro mod alterar a área policial, avise: capacidade, IA/portas, trânsito, colisões e comportamento policial dependem deste teste real no seu GTA.
