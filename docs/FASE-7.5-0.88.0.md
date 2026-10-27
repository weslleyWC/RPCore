# 0.88.0 — contratar motorista e controlar clima/horário

## Táxi / limousine como passageiro

Abra **F2 → Contacts → Taxi / limousine**, **F11 → Transporte / taxi e limousine** ou o menu central F10. Chame a pé, fora de procurado/missão/atividade. O serviço cria apenas carro/motorista próprios e marca o veículo no mapa; não toma um táxi do trânsito.

O banco reserva **$500 no táxi / $2.000 na limousine** antes da chamada. A tarifa é **$2 +$5/15s** no táxi e **$150 +$1/15s** na limousine, contada somente após embarcar. A reserva limita o gasto; não cria dívida. Cancelar antes de embarcar devolve tudo. Depois de embarcar, encerrar devolve apenas o saldo não gasto. A tarifa exibida é a mesma cobrada, corrigindo a inconsistência da referência PSRP para limousine.

1. Anote banco/dinheiro e chame táxi. Aguarde a chegada à rua próxima do ponto onde chamou. Aproxime-se a pé e use **E**, com o veículo parado, para solicitar embarque. Pode entrar normalmente como passageiro. O motorista não teleporta nem força você para dentro.
2. Marque um waypoint **de rua** no mapa. O motorista deve dirigir até lá e parar; saia para encerrar ou marque outro waypoint para continuar. O relógio do taxímetro continua embarcado/parado, inclusive sob menus LemonUI; ESC pausa de verdade. Entrada apenas até3min; viagem até30min/config ou limite financeiro.
3. Use F11 → Transporte para pedir velocidade normal/mais rápida (20/30m/s) ou encerrar. **END também encerra**. Encerrar manda o motorista frear e não expulsa o jogador. F2 continua respeitando a restrição anterior para passageiros; use F11/END durante a corrida.
4. Verifique uma corrida de pelo menos30s, a devolução, o histórico, saída antecipada e ausência de nova cobrança ao reabrir/recarregar. Repita na limousine. O serviço só registra períodos completos de15s; não recupera tempo offline.
5. Recarregue scripts durante a chamada e durante a viagem: a reserva pendente deve encerrar como interrompida/devolver o restante, sem recriar carro/motorista ou repetir devolução. O carro ocupado fica no mundo; não é deletado/ejetado. Saia para liberar a limpeza/departida natural. Se o banco estiver próximo de int.MaxValue e não couber a devolução, ela permanece pendente: reduza o saldo e encerre pelo menu.
6. Teste saldo insuficiente, missão/procurado/morte, troca de personagem, carro sem assento ou modelo ausente. Cancelamento/falha sem embarque devolve toda reserva. Fechamento deve liberar o gate para outro trabalho.
7. Como motorista de um carro pessoal parado/vazio, escolha **Usar este carro como modelo de taxi**. A próxima chamada normal usa cópia visual, com outra placa, sem remover/cadastrar de graça o carro pessoal. Teste modelo base e DLC; se o modelo ficar indisponível, restaure o padrão no menu. Limousine usa modelo separado. Não há teleporte para pular a corrida; pathfinding/trânsito precisam de teste GTA.

`Services/Taxi.json` define modelos, reservas e limites; mudanças valem para novas chamadas. Progresso/escrow/template/histórico ficam no campo opcional `Wallet.Passenger`. Banco e recibo são gravados juntos com rollback de falha normal. Falha de arquivo mantém a reserva para recuperação. Este ambiente não executa GTA nem comprova o comportamento da IA.

## Clima e horário

Abra **F11/F10 → Mundo / clima e horario**. Em saves antigos tudo começa **Off**, sem sincronizar relógio. Preferências são globais em `World/Settings.json`; não mudam com o personagem. `World/Defaults.json` serve de padrão quando faltam preferências.

1. Escolha clima manual (ex.: Clear, Raining, ThunderStorm). Confira o visual. Se houver missão/atividade, aplicação aguarda o personagem ficar livre. Off encerra a automação/libera a persistência aplicada por este sistema quando o clima atual ainda coincide; não rebobina clima ou horário.
2. Teste hora manual **HH:mm** e sincronização com hora local do Windows. Hora manual desliga sincronização. Verifique que nenhuma opção congela permanentemente o relógio. Desligue sincronização antes de testar trabalhos noturnos.
3. Ative Dynamic: muda gradualmente a cada8min ativos por padrão. Pausa, missão/atividade e grandes travamentos não acumulam tempo atrasado. Não declara a mesma matriz de probabilidades PSRP.
4. Para Real, informe latitude/longitude com ponto decimal, nome do local e C/F. Padrão Los Angeles usa longitude **negativa**. Ativar consulta Open-Meteo por HTTPS e repete a cada5min ativos. Não obtém localização automaticamente.
5. Confira notificação/resultado reabrindo o menu. Desconectado ou com resposta inválida, deve manter clima atual e permitir jogar; a consulta tem timeout8s/limite64KiB e tenta novamente no próximo intervalo. Trocar modo/coordenadas descarta resultado antigo. A consulta online não pôde ser homologada: o proxy deste ambiente bloqueou a tentativa HTTP com403.
6. Teste reabrir/recarregar preferências, troca de perfil e coexistência com seus mods de clima. Dados inválidos ou alterados externamente ficam protegidos contra sobrescrita; após corrigir, recarregue scripts. Off e relógio desligado preservam o controle nativo a partir daí.

F2, teleporteF11, oficina0.87, editor e demais atividades mantidos. Próximo fechamento: classificar diferenças remanescentes de gangues/polícia; não declarar paridade total ou iniciar refinamentos8/9 sem essa separação explícita.
