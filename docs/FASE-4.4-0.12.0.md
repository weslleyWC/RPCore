# Fase 4.4 — RPCore 0.12.0: piloto

Correção posterior: o usuário recebeu quatro estrelas ao entrar no LSIA para qualificar. A [0.12.1](CORRECAO-0.12.1.md) move a base padrão para Grapeseed, mantém Sandy Shores e cinco helipontos e ajusta o prazo padrão Arcadius para 240 segundos. Use as instruções novas; o restante abaixo registra a entrega original. Ainda não houve aceite dos três voos no jogo.

O usuário confirmou o transporte prisional regular da 0.11.0 funcionando. Esta entrega implementa os três ciclos funcionais encontrados em _JOB__Pilot da PSRP: qualificação, transporte de avião para Sandy Shores e transporte de helicóptero para cinco helipontos. Dirigível/publicidade e pontos de hover aparecem declarados na referência, sem ciclo completo conectado; não estão implementados nesta entrega. Suborno/emboscada do transporte prisional continuam pendentes.

## Começar pela qualificação

1. F10 → Trabalhos / contratos → Piloto → GPS: balcao LSIA. Vá a pé ao ponto (-1143.238, -2705.508, 13.95856), sem procurado/combate/missão, e selecione Voo de qualificacao. E também abre o contrato perto do balcão.
2. Retire o Velum azul no pátio. Spawn base da referência: -1023.876, -2903.852, 13.96453, direção 148.8467. A busca de 35 vagas próximas verifica dimensões, chão e cenário sem apagar veículos/peds de ambiente. Falha registra “Pilot aircraft spawn” em scripts/RPCore/RPCore.log.
3. Entre no assento do piloto, mantenha a aeronave parada em terra perto da retirada e pressione E. Decole. Para confirmar voo, avião deve estar no ar, com piloto correto, a pelo menos 20 metros do chão e velocidade de pelo menos 15 m/s durante dez segundos observados seguidos. Abrir menu/modal interrompe essa observação; retome o voo para contar novamente.
4. Voe até Sandy Shores (1698.431, 3249.649, 40.94578). Pouse e pare perto do marcador: raio horizontal de 30 metros, diferença de altura até oito metros, aeronave fora do ar, altura do chão inferior a quatro metros e velocidade abaixo de 0,5 m/s. Pressione E.
5. A qualificação é gravada no Wallet.json do perfil, em Jobs/pilot_training/CompletedShifts, com recibo/checkpoint e sem prêmio em dinheiro. Comercial só libera após esse salvamento. Não há teleporte do jogador: o GPS volta ao balcão LSIA. A aeronave ocupada permanece disponível para você voltar voando; só é recolhida vazia, parada e em terra. Se sair no Sandy Shores, precisará de transporte para voltar ao balcão. Qualificação permanece após reload/troca de perfil e vale só para o perfil em que foi concluída.

## Transporte de avião

Após qualificar, contrate Transporte de aviao no mesmo balcão. O padrão é Velum com dois passageiros próprios. E na aeronave parada prepara o manifesto; passageiros novos são inicializados em assentos livres e confirmados, como na partida da referência. Não há fila andando para embarcar e nenhum NPC de ambiente/jogador é movido para a aeronave.

Decole, confirme dez segundos de voo e leve os passageiros até o mesmo ponto de Sandy Shores. Pare no marcador e E confirma o pouso/inicia desembarque. O motor é desligado e há espera de dois segundos antes da ordem de saída. Aguarde os dois fora fisicamente, vivos, próximos do destino/aeronave. E tenta novamente a saída dos que ainda estiverem dentro; prazo de dois minutos para desembarque. Não há pagamento só por aproximar ou pairar sobre o marcador.

A entrega paga no banco 1400 + parte inteira da distância base→Sandy × 0,10. Religue o motor com sua tecla de motor (X no padrão) ou pelo menu do contrato. Retorne voando com a mesma aeronave ao ponto em que foi criada no LSIA: exige outra confirmação de voo de dez segundos. Pouse, pare vazio a até 40 metros e E paga o bônus único de 500. Saia para a recolha segura.

## Transporte de helicóptero

Contrate Transporte de helicoptero após a qualificação. O padrão é Frogger com um passageiro, no spawn da referência (-1111.612, -2883.941, 13.94604; direção 146.4589). E parado prepara o manifesto. Decole e mantenha ao menos dez metros do chão, três m/s e seis segundos observados de voo.

O destino é um dos cinco pontos da referência: Arcadius Business Center, Majestic Hotel, Maze Bank, Lombank ou Golf Course. Siga a altitude do heliponto: passar pela rua abaixo não conta. Pouse e pare num raio horizontal de 12 metros, diferença vertical até oito metros, fora do ar e a menos de quatro metros do chão. E inicia desembarque; motor é desligado e a ordem de saída aguarda seis segundos para reduzir o giro. O passageiro precisa sair vivo e próximo. Não force a saída enquanto estiver pairando.

Arcadius é o voo executivo com prazo padrão de 150 segundos a partir de E/preparação, até confirmar o pouso; o contador aparece no cabeçalho. Passado o prazo sem confirmação, não paga. Os outros quatro destinos não têm prazo de viagem. No JSON, HeliTimeLimitSeconds=0 ou TimedHelipadIndex=-1 desativa esse prazo.

A entrega paga no banco 3500 + parte inteira da distância base→heliponto × 0,25. Religue o motor com X/tecla configurada ou menu, retorne com o mesmo helicóptero, confirme seis segundos de voo de retorno e pouse vazio a até 18 metros da retirada LSIA. E paga 750 de bônus. Passageiros desembarcados ficam parados para recolha, sem ordem para caminhar em direção à borda do telhado.

## Estados, falhas e compatibilidade

Um contrato de piloto por vez, compartilhando exclusão com os sete trabalhos anteriores. Segure End por um segundo ou cancele pelo menu. Cancelar mantém etapas já pagas, sem bônus pendente. Não gera qualificação em teste incompleto. Sair do piloto permite até 60 segundos para voltar; morte, missão, perda de aeronave/passageiro ou dano grave encerram o contrato. EngineHealth/BodyHealth abaixo de 400 por padrão são considerados dano grave; não há invencibilidade forçada. Em cancelamento, somente passageiros próprios ainda ativos são retirados; não recebem ordem para saltar de aeronave em voo.

Aeronaves de trabalho ficam protegidas contra garagem/armazenamento. A recolha espera aeronave vazia, fora do ar, parada e próxima do chão; não apaga aeronave ocupada. Ao descarregar o script, uma aeronave ainda ocupada/em voo é liberada à gestão do GTA. Turnos ativos não retomam após reload; qualificação e créditos/recibos persistem por perfil.

Piloto usa X/EngineKey para alternar motor apenas parado em terra; durante desembarque/confirmação aguarda concluir. O menu oferece a mesma ação. F2 e configurações pessoais continuam iguais. Não há limpeza automática de estrelas, alteração de relações/polícia globais ou autorização artificial de acesso ao aeroporto; regras nativas e outros mods continuam valendo. Nenhum OIV/YTD/RPF/DLC/dlclist foi alterado. Banner PSRPBANNER1 e fallback usam recursos já instalados.

## Configuração e progresso

Novo scripts/RPCore/Jobs/Pilot.json, instalado só se ausente, com fallback embutido. PlaneModel/HeliModel permitem modelo próprio da categoria correta, sujeito a assentos e colisão reais. PlanePassengers aceita 1–8 e HeliPassengers 1–3. Defaults Velum/2 e Frogger/1. Start/PlaneSpawn/PlaneDestination/HeliSpawn/Helipads podem ser personalizados; preserve altitude correta dos telhados. TimedHelipadIndex usa índice baseado em zero; padrão 0/Arcadius e HeliTimeLimitSeconds 150 (0–1800). Modelos ausentes/errados ou capacidade insuficiente impedem iniciar e liberam o turno. JSON inválido é preservado e usa fallback; arquivo válido mantém overrides/unknowns.

Wallet.json acrescenta Jobs/pilot_training, pilot_plane e pilot_heli. Carreira conta entregas de passageiros; turnos completos contam retornos. Qualificação usa CompletedShifts de pilot_training. Não modifica o formato dos saves anteriores nem importa automaticamente uma licença antiga exclusiva da PSRP. Crédito e recibo compartilham o checkpoint existente; falha de gravação mantém confirmação pendente. Distância paga é em linha reta, não soma de trajetória real. Inventário pessoal permanece igual.

## Arquivos alterados

- FlightData.cs/PilotJob.cs/config/Jobs/Pilot.json: três modos, qualificação, manifesto, confirmação de voo/pouso/desembarque, prazo executivo, crédito, retorno e limpeza.
- Wallet.cs: consulta de qualificação na própria lógica testada, sem formato novo de licença.
- DeliveryCargo.cs/JobVehicles: fila de recolha de aeronaves, preservando os fluxos anteriores de carros/ônibus e conjunto LS GAS.
- Main.cs/SettingsSystem.cs: oitavo contrato, central e GPS F11.
- AchievementSystem.cs: três conquistas (qualificação, entrega e retorno), acrescentadas quando ausentes.
- Visuals.cs/config/Visuals/Bindings.json: banner com textura existente e fallback.
- AssemblyInfo.cs/CoreMenu.cs/Main.cs/VisualAssetDefaults.cs: versão 0.12.0.
- RPCore.csproj/tests/RPCore.Tests.csproj: default embutido e vínculo da lógica; tests/FlightTests.cs adiciona 45 casos.
- scripts/audit-psrp-systems.py: auditoria das nove posições da referência. Documentação, relatórios e ZIPs atualizados; fontes PSRP não foram transplantados.

## Teste solicitado no GTA

Build Release net48, SHVDN Enhanced 1.1.0.6/LemonUI SHVDN3 2.2: zero erros/avisos. 385 testes passaram (340 anteriores + 45 novos). Testam observação de voo, tempo/overflow/interrupção, pouso versus hover/rua abaixo, licença/checkpoint, manifesto, desembarque, retry/replay financeiro, um bônus após voo de retorno, dano, prazo executivo, recolha segura, preservação e exclusão entre oito contratos. A auditoria estática confere nove posições no fonte PSRP. Não executa física/IA/nativos nesta nuvem.

1. Conclua o teste LSIA→Sandy e confira que não aceita taxi/hover, salva a licença, libera comercial e mantém a licença após reload. Confira que a aeronave não some enquanto ocupada.
2. Faça um contrato de avião completo: dois passageiros no manifesto, voo, pouso, saída dos dois, pagamento, motor X/menu, retorno voando e bônus único. Saia para recolher.
3. Faça um contrato de helicóptero: destino na altura correta, pouso real, motor/parada/saída sem queda do passageiro, pagamento, retorno e bônus. Se sair Arcadius, confira contador de 150 segundos e falha sem pagamento ao expirar.
4. Teste cancelamento em terra e em voo, abandono, dano, passageiro perdido e troca de perfil: não deve mover jogador/veículo pessoal nem apagar aeronave ocupada. Confira saldo/progresso após reload.
5. Faça regressão dos sete trabalhos anteriores, telefone F2, tecla de motor e saves. Acesso ao aeroporto, colisões/asfalto, comportamento em telhados, assentos/portas e física dos modelos instalados dependem do teste no seu GTA. Se não criar aeronave, envie o trecho “Pilot aircraft spawn” do log.
