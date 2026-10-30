# Correções dos logs e investigação de estabilidade — 0.102.2

O usuário relatou que as portas de roupas continuavam fechadas, travamentos em jogo normal e crash durante perseguição, possivelmente desde0.99; informou que ainda não havia testado0.102.1. Foram examinados os dois logs fornecidos e as mudanças desde0.98/0.99. Esta atualização corrige falhas confirmadas e reduz trabalho nativo desnecessário; **não declara identificada a causa de todos os crashes nativos**.

## Evidência dos logs

RPCore.log confirma `clothing door access: System.InvalidCastException: Unable to cast object of type 'System.Boolean' to native value` às10:55:39 e11:57:05. O jogador estava perto da entrada correta da Binco/Textile City nos registros POS. O erro ocorria antes da consulta/destravamento, não por falta de proximidade.

O mesmo log registra `coremenu.key: System.InvalidOperationException: There are no items in this menu`, em CoreMenu.Rebuild -> NativeMenu.set_SelectedIndex, por exemplo às11:39:35 e11:43:44. A reconstrução tentava definir seleção no menu de confirmação ainda vazio.

ScriptHookVDotNet.log informa Enhanced3.9.0.6(1.1.0.6), carrega as APIs e inicia RPCore.Main normalmente. Não contém stacktrace fatal ou motivo do fechamento do processo. Esses registros confirmam as duas falhas acima, mas não provam qual native/módulo causou o crash durante a fuga. Não foi atribuído o crash a outro mod nem à0.102.1 ainda não testada pelo usuário.

Os logs originais não foram publicados no repositório. Resumo técnico e hashes ficam em ANALISE-LOGS-0.102.2.json, sem caminhos pessoais do computador.

## Portas e ponteiros Enhanced

Foi conferido o código da dependência instalada: `OutputArgument(object)` aceita null, string ou INativeValue. Ele não aceita bool, float ou int boxed. Por isso `new OutputArgument(false)` da correção anterior falhava em runtime, embora compilasse.

Os outputs das portas passam a `OutputArgument.Init(0UL)`: buffer inicializado com8bytes, suficiente para o retorno escalar e para as leituras do SDK. Isso também evita ler bits superiores não inicializados quando um native escreve apenas os4bytes inferiores do BOOL/Hash. A consulta antiga conserva ângulo lido e destrava; quando há uma porta existente registrada no Door System, o código também solicita estado0/destravado nessa porta. Não registra porta nova, remove objetos ou força ângulo de abertura. Continuam os quatro modelos e14 entradas de roupas, proximidade/altura/contexto seguro e associação à loja configurada.

A varredura encontrou a mesma construção incompatível de OutputArgument com handle int na limpeza de props do celular/consumo. Ambos agora usam buffer tipado de8bytes e using/Dispose determinístico. A remoção continua limitada a handle/model/token/decor comprovadamente pertencentes ao RPCore; não apaga props iguais de outros scripts ou do cenário.

## F10 e ciclo de direção

CoreMenu não lê/restaura seleção de menus vazios. O menu de confirmação passa por sua criação normal antes de receber seleção; os grupos F10 permanecem iguais. Isso corrige a exceção registrada, sem redefinir perfis/saves.

Na0.99, Refresh passou a consultar os estados dos controles de portas/vidros/luzes enquanto o jogador dirigia mesmo com os menus fechados. Agora Refresh ocorre apenas com menu veicular visível. A sessão de controles passa a ser ligada por comando/abertura explícitos, não pela entrada automática em todo carro de trânsito; motor desligado manualmente, cinto e piloto automático existentes continuam com seus fluxos. Uma sessão já ligada ainda valida seu dono e libera overrides ao sair. Não muda handling, potência, física ou policiais.

O helper de menus também deixa de consultar botões/prova do ator quando o próprio menu está invisível e invalida ações pendentes ao ocultar. Os predicados de contexto que outros sistemas precisam consultar continuam existindo; isso não promete zerar todos os natives do jogo. A redução de chamadas é verificável no código, mas não é prova de causa/resolução do crash nativo.

## Registro leve para a próxima ocorrência

`RuntimeState.bin`, em scripts/RPCore, é um arquivo próprio de8KB mapeado em memória. Marca a etapa do loop, menus, input, saves e callbacks de sistema, com versão e mapa de nomes da sessão. Não regrava um JSON a cada native/frame e não faz flush síncrono por frame. Não contém saldo, inventário ou perfil e não é um save do jogador.

Ao iniciar novamente, o RPCore lê a etapa anterior e gera `scripts/RPCore/RuntimeLastSession.json`; também registra uma linha Previous runtime stage no RPCore.log. A flag AbortedHookReached significa que o callback OnAborted foi alcançado, **não** que o jogo certamente terminou sem erro. A etapa é contexto de execução, não uma atribuição automática de culpa: um crash do motor ou de outro componente pode ocorrer enquanto uma etapa RPCore estava ativa. O registro não substitui stacktrace/dump do processo e não cobre queda de energia.

Se não puder criar o diagnóstico, ele é desativado/logado sem impedir o mod. O binário tem tamanho fixo, o mapa/JSON é produzido no startup e os recursos são liberados no encerramento. Arquivo anterior inválido é tratado como diagnóstico indisponível, sem tocar em SaveData. Não há novo botão de ativação/desativação nos menus.

## Instalar e testar

1. Feche o GTA, extraia0.102.2 e rode o instalador da pasta extraída com seu caminho real. Esta versão já inclui a correção de persistência bancária0.102.1; não é necessário instalar ambas em sequência. Saves/configs/OIV/RPF/YTD permanecem preservados.
2. A pé, sem procurado/missão, teste a porta da Binco/Textile City onde o log mostrou o erro; atravesse, abra roupas por E, feche e saia. Repita em Suburban e Ponsonbys, incluindo streaming após afastar/reaproximar.
3. Abra/feche F10 várias vezes, banco e demais grupos, cancelar/confirmar quando aplicável e voltar. Não deve haver a exceção de menu vazio. Confira F11/F2/J e mouse/controle.
4. Teste direção normal e perseguição como no relato, primeiro com menus fechados e depois controles veiculares. Confira motor, cinto, setas, luzes, portas/vidros parado e piloto automático, inclusive sair/trocar de carro. Não execute teste de perseguição com atividade de trabalho pendente.
5. Teste celular/consumíveis a pé; os props próprios devem sair corretamente. Confira inventário, peso, saldos e sistemas anteriores.
6. Faça o teste bancário de fechar/reabrir totalmente o GTA descrito em BANCO-PERSISTENCIA-0.102.1.md: os400mil depositados não devem voltar duplicados e saque deve persistir. Valores já perdidos/duplicados anteriormente não são ajustados automaticamente.
7. Se travar/cair novamente, feche o processo se necessário e reabra uma vez para gerar RuntimeLastSession.json. Envie esse JSON, RPCore.log, ScriptHookVDotNet.log e ScriptHookV.log junto da ação/local em que ocorreu. Se não conseguir reabrir, envie RuntimeState.bin diretamente. Não apague os arquivos antes de coletar.

## Validação e limites

3940 testes aprovados, Release net48 sem erros/avisos. São3933 regressões anteriores,4 testes do buffer usando a DLL Enhanced real (sem executar natives) e3 testes do registro de sessão em arquivo mapeado. Os testes do SDK validam a criação/leitura do buffer corrigido; a exceção da forma antiga é comprovada pelos logs do usuário e pela implementação da DLL, não por executar GTA neste ambiente.

Instalador executado duas vezes sobre0.102.1 com229 arquivos existentes preservados byte a byte, incluindo os diagnósticos existentes, bancos/checkpoints e todo o conjunto anterior. Manifestos, DLL, fonte, configs e áudio conferidos. Nenhuma sessão GTA/portas físicas/perseguição foi executada aqui. A resolução definitiva do crash aleatório precisa da validação nativa e, se persistir, do novo contexto/stacktrace.
