# Fase 5.6 — RPCore 0.18.0: arrancada

Atualização: usuário confirmou a corrida funcionando e pediu seguir à [fase 6.1A — empresa PDM](FASE-6.1-0.19.0.md). Refinamento da arrancada continua previsto para a fase 9. Texto abaixo registra a entrega original.

Última atividade base prevista na fase 5: arrancada um contra um na pista de Sandy Shores, com carro do jogador, adversário NPC, alinhamento manual, contagem, queima de largada, chegada cronometrada, aposta opcional e recuperação de valores pendentes. Caça, pesca, crafting, tráfico e streamer continuam incluídos. Não se declara paridade completa de todos os sistemas da PSRP: as diferenças e os refinamentos permanecem no roteiro.

Depois do teste, a sequência principal segue à fase 6 (empresa PDM/moradia/personagem/seguros/licenças/gangues/polícia, em subentregas), depois fase 7. A **fase 8** continua reservada a toda a interface e mecânicas associadas ao HUD/menus, inclusive fome/sede; a **fase 9**, ao refinamento de todos os trabalhos com varredura aprofundada da PSRP.dll. Esta entrega não antecipa essas revisões.

## Instalação

Feche o GTA, extraia RPCore-update-0.18.0.zip e execute o instalador da pasta extraída com seu caminho real:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

DLL pronta. Backup e Jobs/Drag.json acrescentado só se ausente. Saves/configurações/F2/teleporte, licenças e progressos anteriores preservados. Não reinstala/modifica OIV/YTD/RPF/DLC/dlclist.

## Como realizar a corrida

1. Abra **F10 → Trabalhos / contratos → Arrancada / Sandy Shores → GPS: encontro / inscricao**. F11/J → GPS também tem esse destino. O encontro fica em **(1736.146, 3286.22, 40.74411)**, na pista pública de Sandy Shores. Use F11/teleporte para chegar **antes de começar a corrida**.
2. Traga **seu próprio carro**, funcionando e sem carreta. Deve estar como motorista, parado, sem procurado/missão, a até 12 m do encontro. Carros de trabalhos ativos/reservados não são aceitos; não há veículo emprestado nesta entrega. Carros muito grandes (comprimento >=8 m ou altura >=3 m), motos e aeronaves não participam.
3. Pressione **E** no encontro. Escolha o adversário (Dominator, Gauntlet ou Sultan) e **Treino sem aposta** ou uma aposta de **250/500/1000** do banco. A entrada só reserva valor depois de encontrar posição livre e criar carro/motorista do rival. Faixa ocupada/falha de modelo ou gravação não confirma cobrança.
4. O NPC aguarda na faixa dele. Dirija até a marca **verde da sua largada**, alinhado para a pista/chegada. Pare a menos de dois metros da marca, com a frente no sentido da corrida, e pressione **E**.
5. **Segure o freio** durante a contagem **3, 2, 1**. O RPCore não congela nem teleporta seu carro. Avançar/acelerar antes do **JA** queima a largada e perde a aposta. Se a gravação da largada falhar, mantenha o freio até confirmar: o adversário também aguarda.
6. Ao aparecer **JA**, solte o freio e acelere até a marca amarela da chegada, aproximadamente 450 m adiante. O rival dirige de verdade usando a tarefa nativa em linha reta; não é um cronômetro que sorteia vencedor.
7. Cruze a linha antes do NPC e confira resultado, tempo e retorno bancário. A chegada mede cruzamento do plano da faixa e interpola dentro do frame para decidir corridas próximas. Diferença de até 10 ms é empate. Mantenha-se no corredor da pista e passe pela sua chegada.
8. Abra novamente o menu para repetir ou ver vitórias/derrotas, melhor tempo e valores acumulados. **Streamer pode continuar transmitindo em paralelo**. A corrida ocupa a mesma trava de atividade dos demais trabalhos físicos.

## Aposta e resultados

| Resultado | Retorno ao banco | Regra |
| --- | --- | --- |
| Vitória | 2× aposta | Inclui a aposta reservada mais o lucro de igual valor |
| Derrota | 0 | Rival cruzou primeiro |
| Empate | Aposta devolvida | Chegadas praticamente simultâneas |
| Queima de largada | 0 | Saiu/acelerou antes do sinal |
| Cancelamento antes do sinal | Aposta devolvida | End, timeout de alinhamento ou interrupção antes de correr |
| Abandono após o sinal | 0 | End, menu aberto, saída do carro/pista ou demais falhas do jogador |
| Corrida anulada | Aposta devolvida | Rival indisponível/teleportado, falha técnica ou timeout sem chegada válida |

Exemplo com banco 2000 e aposta 250: inscrição deixa 1750; vitória devolve 500, ficando **2250**; derrota/queima/abandono deixam 1750; cancelamento pré-sinal/empate/anulação deixam 2000. Não há prêmio adicional no treino gratuito, mas ele registra resultado/tempo. A conquista de vitória também pode ser obtida no treino. cash_earned conta apenas o lucro, sem contabilizar o reembolso como ganho.

Alinhamento tem prazo de **120 segundos**; corrida, **90 segundos**. Timeout sem vencedor válido anula/reembolsa em vez de inventar resultado. Procurado/missão/morte/veículo indisponível ou sair do carro interrompem; antes do sinal devolvem aposta, depois contam como abandono. Abrir um menu durante a contagem cancela/reembolsa; abrir durante a corrida abandona. Durante alinhamento pode consultar menus dentro do prazo. Pausa nativa não abandona automaticamente.

**End segurado por um segundo** encerra conforme a regra antes/depois do sinal. Teleporte/saída do corredor depois do JA não dá vitória: saltos incompatíveis de posição e saída lateral são detectados. O carro do jogador não é apagado, reparado, abastecido, movido ou ajustado pelo sistema; continua sujeito a combustível e controles instalados. Prepare-o antes da corrida.

## Salvamentos e recuperação

Novo campo **Wallet.json/Drag** por perfil: estatísticas, Staked/Returned, melhor/último tempo, último resultado, recibos recentes e **Pending** da aposta. Reserva inicial debita banco e cria Pending no mesmo checkpoint. O sinal grava Started antes de liberar o NPC. O resultado é persistido **antes** da liquidação; depois o checkpoint de pagamento atualiza banco/estatísticas e remove Pending juntos. Falhas restauram referências/saldo; um recibo confirmado não paga duas vezes.

Se o pagamento falhar ou não couber no banco, o resultado continua pendente. A rotina tenta novamente após cinco segundos; o menu tem **Resolver aposta pendente**. Libere espaço no banco pelo sistema existente, se necessário. Não inicia nova inscrição enquanto houver pendência. O resultado já confirmado não é convertido em derrota apenas por reabrir o menu.

Após reload/troca de perfil/interrupção dos scripts, não retoma a corrida física. Ao carregar o perfil, resolve Pending:

- Resultado já gravado: honra esse resultado e liquida uma vez.
- Sem resultado e sem Started: cancela e devolve aposta.
- Sem resultado, mas sinal já salvo: registra abandono e não devolve aposta.

Isso evita perder uma aposta ainda não iniciada ou ganhar reembolso saindo do jogo depois da largada. Inventário, bolsas, estoque do tráfico, estatísticas do streamer, licenças e empregos anteriores não são consumidos/convertidos. Campos desconhecidos são preservados; dados semanticamente inválidos não são zerados. Histórico de recibos limitado a 512.

## Recursos e referência

Os cinco pontos (encontro, duas largadas e duas chegadas) foram conferidos em **PSRP/_ACTIVITY__Drag_Racing.cs**. Usa **DragBanner** dos YTD existentes e modelos base do GTA. Se o rival continuar praticamente parado nos primeiros dez metros após oito segundos, anula/reembolsa em vez de apresentar a falha de IA como corrida normal. Solicita colisão ao longo da pista sem mudar foco/câmera global; IA/collision streaming precisam do teste no jogo. O spawn usa limites orientados para não tratar carros estacionados ao lado como ocupação da faixa. Só recursos próprios são limpos. Se outro ocupante estiver no carro do rival, ele é liberado em vez de apagado com o ocupante.

Implementação própria de um contra um. Não inclui o encontro completo de múltiplos pilotos, apostas como espectador, câmeras, largada por sinalizador, aquecimento de pneus/grip, nitro ou alterações de potência presentes na referência. Não controla/apaga carros do ambiente, nem remove estrelas. O refinamento e essas diferenças serão examinados na fase 9; não são apresentados como já implementados.

**Jobs/Drag.json** personaliza encontro/largadas/chegadas, heading, prazos, velocidade solicitada à IA, corredor, apostas e modelos. Criado/instalado só se ausente e embutido como fallback. Configuração inválida fica preservada e usa padrão; válida mantém overrides. Valores de apostas: 0–10000; IA: 20–80 m/s solicitados, com aceleração/velocidade efetivas dependentes do veículo. Não força a velocidade do jogador nem muda seu grip. Fases 8/9 e problemas de mecânicas de fome/sede permanecem como registrados.

## Arquivos alterados

- DragData.cs/DragJob.cs/config/Jobs/Drag.json: geometria/contagem/corrida, NPC/carro próprios, alinhamento manual, resultado, aposta reservada, liquidação/recovery e limpeza.
- Economy.cs/Wallet.cs: campo Drag aditivo e adaptadores bancários, sem conversão dos dados anteriores.
- Main.cs/SettingsSystem.cs: 14ª entrada na central, 13ª atividade física exclusiva e GPS; streamer permanece paralelo.
- AchievementSystem.cs: corrida/vitória e lucro confirmado em cash_earned.
- Visuals.cs/config/Visuals/Bindings.json: DragBanner/fallback mesclado aos bindings locais em memória.
- RPCore.csproj/tests/RPCore.Tests.csproj: configuração embutida e lógica real nos testes; tests/DragTests.cs: 39 casos novos.
- AssemblyInfo.cs/Main.cs/CoreMenu.cs/VisualAssetDefaults.cs: versão 0.18.0. Auditoria, documentação e pacotes atualizados.

## Validação e roteiro no GTA

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI SHVDN3 2.2: zero erros/avisos. **653 testes aprovados** (614 anteriores +39 novos): reserva/prática, sinal, resultado/pagamento, rollback/replay, interrupção/recuperação, banco cheio, unknowns/sistemas anteriores/licença, linha de chegada com overshoot/fração/empate, corredor/teleporte/timeout e relógio com overflow. Não executam direção IA/colisões/renderização no GTA.

1. Comece com treino gratuito. Confira encontro/GPS, carro do rival, alinhamento/marca verde, contagem e largada. Seu carro deve permanecer sob seu controle.
2. Corra normalmente e observe o NPC acelerar em direção à chegada. Teste vitória e derrota; compare resultado/tempo. Se o NPC não dirigir corretamente, relate modelo e ponto em que parou: não há resultado sorteado para encobrir uma falha de IA.
3. Aposte 250 e confira débito/retorno conforme tabela. Repita, reabra o menu/recarregue e confirme que não paga duas vezes.
4. Queime largada, cancele antes/depois do sinal, abra menu na contagem/corrida, saia da pista e teste interrupção/reload. Antes do sinal deve devolver; depois registra abandono se não houve resultado salvo.
5. Confira melhor tempo/histórico, preservação do carro próprio e limpeza do rival. Faça uma live durante a corrida e regressão rápida dos trabalhos, inventário e teleporte.

O jogo não foi executado nesta nuvem. Posições/markers, IA/streaming, aceleração/chegada real e convivência com recursos instalados precisam do seu teste no GTA.
