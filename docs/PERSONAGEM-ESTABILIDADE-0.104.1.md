# Proteção da entrada e carregamento de personagem — 0.104.1

O usuário relatou na0.103 personagem RPCore ao entrar, retorno a Franklin/casa e crash ao carregar pelo menu. A0.104 inclui aquele fluxo; este hotfix inclui o grupo armazenamento0.104 e trata a entrada/troca, sem avançar o refinamento normal.

## O que os arquivos confirmam

RPCore.log registra0.103 no perfil RPCore às13:37:44 e `profile -> Franklin` às13:37:46. Confirma mudança de perfil logo na entrada, mas não identifica por si qual script a iniciou. Não há stacktrace novo nem registro de transição concluída após esse ponto. ScriptHookV/SHVDN mostram início normal, GTA3788 e Enhanced3.9.0.6(1.1.0.6), sem stacktrace fatal. Outros scripts aparecem carregados, mas não há prova para atribuir o crash a eles; nenhum foi removido/desativado.

RuntimeLastSession.json pertence à0.102.2, etapa idle, e foi gerado na entrada da0.103. Descreve a sessão anterior, não a etapa do crash desta0.103. Não é correto afirmar que o crash aconteceu em idle ou que todos os crashes foram explicados. Hashes e evidência sanitizada em ANALISE-LOGS-0.104.1.json; os logs pessoais originais não são distribuídos.

## Alterações

A0.103 aceitava um segundo com tela liberada/controle como entrada pronta; não verificava IS_PLAYER_SWITCH_IN_PROGRESS nem estabilidade do ator/local. Agora há uma carência inicial de30segundos do relógio do jogo observado pelo script e uma janela de10segundos com ator/modelo/perfil e mundo estáveis. Troca de ped/perfil, carregamento, troca de protagonista, falta de controle/colisão ou deslocamento brusco entre observações reinicia essa janela. Movimento normal não exige dez segundos parado; o menu só abre quando também estiver parado a pé, em contexto seguro como antes.

As verificações de motor/mundo incluem tela carregada, ausência de troca de protagonista/cutscene/missão, controle e colisão carregada ao redor do ped, sem espera de colisão do mundo. A observação de motor fora do menu/operação é limitada a4vezes por segundo; durante carregamento revalida por etapa. A janela é uma proteção adicional verificável em código, não uma garantia de que qualquer script externo terminou seu trabalho.

Carregar pelo F10 também respeita essa estabilidade: não contorna a proteção da entrada. Pode abrir a lista antes, mas a troca será recusada com mensagem para aguardar. A configuração F11 → Game Settings → Seleção de personagens ao iniciar permanece persistida; desativar a abertura não desativa as proteções da troca manual. F4 reinicializa o script e a carência de30segundos.

Antes de SET_PLAYER_MODEL, salva os sistemas/carteira pelo caminho existente e revalida novamente a origem, modelo, perfil, posição, mundo e modelo solicitado carregado. Se a origem mudou durante o salvamento, cancela a substituição. Mantém checkpoints pendentes e rollback do ActiveProfile anterior quando a substituição é recusada. Não deduz/recria dinheiro, não zera personagens, não teleporta para casa e não tenta impor o modelo em loop contra o GTA.

A aplicação de corpo/equipamento/rosto deixa de acontecer no mesmo frame do SET_PLAYER_MODEL: aguarda pelo menos500ms e novo ped válido com controle/colisão/contexto seguro, até10segundos. A indisponibilidade breve após trocar modelo não dispara imediatamente aplicação nativa nem cancela por falta momentânea de controle. Identidade/modelo/perfil diferentes ou prazo vencido interrompem com registro pendente para retomar. Aplicação e readback anteriores permanecem, sem mudança do formato de saves. A referência PSRP.MultiplayerCharacter.LoadCharacterToPed/ChangeModel e a API Enhanced foram revisadas estaticamente; o wrapper oficial usa o mesmo SET_PLAYER_MODEL e não é por si uma cura de crash.

## Diagnóstico da troca

RPCore.log agora registra fases antes de pedir modelo, capturar origem, gravar pendência, salvar sistemas/carteira, chamar SET_PLAYER_MODEL, retornar do native, recarregar dados, aguardar novo ator, aplicar corpo, cabeça e aparência, capturar e concluir. RuntimeState.bin marca essas fases e cada Save/Reload de sistema. Não há JSON/log por frame; somente fases da operação. O resumo da próxima sessão traduz os códigos character:... em nomes. Etapa é contexto, não prova automática de culpado.

## Teste no GTA

1. Fechar o GTA e instalar a0.104.1. Manter todos os saves/configs. Entrar normalmente e deixar o GTA terminar sua entrada/volta ao protagonista. Aguardar pelo menos30segundos após iniciar o RPCore; após troca/teleporte de entrada pode precisar mais10segundos estáveis.
2. Parar a pé. A lista automática deve aparecer só depois da estabilização, se habilitada. Carregar o personagem e conferir rosto, roupa, equipamentos, inventário, carteira e banco. O preparo pode levar alguns frames adicionais.
3. Cancelar deve continuar com o personagem atual. Desativar a opção no F11, reiniciar, aguardar e carregar manualmente pelo F10. A opção continua desativada e a proteção continua ativa.
4. Não alterar/apagar ApplyPending para forçar troca. Se houver pendência após interrupção, usar Retomar na lista quando estável. Conferir que nenhum saldo migrou entre personagens.
5. Se ainda travar/crashar, enviar **RuntimeState.bin e RPCore.log antes de reabrir**, mais ScriptHookV.log e ScriptHookVDotNet.log. Se já reabriu, enviar também RuntimeLastSession.json; conferir a Version para distinguir a sessão. O arquivo binário atual ajuda mesmo sem reabrir. Não é necessário repetir vários crashes para coletar.

4011 testes aprovados, Release net48 sem erros/avisos. Os23 casos novos simulam a sequência de entrada, janela/relógio/trocas/teleporte/movimento e nomes de fases no arquivo de diagnóstico. Eles não executam SET_PLAYER_MODEL nem reproduzem um crash nativo. A causa fatal específica continua sem stacktrace; a correção precisa da validação do caso real pelo usuário.
