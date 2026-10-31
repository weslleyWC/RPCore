# 0.109.1 — correção da abertura do DMV

Causa identificada no código da 0.109.0: DmvSystem.Calm exigia GameplayContext.BackgroundReady. Esse estado usa AreaWarmup atualizado pelo Main somente nas proximidades da PDM (220 m). Os atendimentos C/M1 e A ficam fora dessa região; portanto a condição permanecia falsa. OpenMenu retornava silenciosamente e o Tick não mostrava o atendimento. DriveTick também usava a mesma dependência indevida e poderia suspender um exame fora da PDM.

A correção separa consulta de menu, preparação física e avaliação. F10 pode consultar categorias/GPS/documentos sem exigir proximidade de empresa ou terreno carregado. O DMV passa a usar uma checagem local de piso/colisão para preparar veículos, aguardando quatro segundos no próprio atendimento; não altera GameplayContext, HUD ou regras da PDM. No exame em curso, verifica colisão/carregamento em torno do jogador, independentemente da PDM. Perfil, controle, cena realmente em exibição, pausa, missão, outras interfaces, combate e procurado continuam protegidos. Cena apenas preparada em segundo plano com controle liberado não impede o menu.

Quando uma abertura manual for recusada, mostra o motivo na tela e registra o bloqueio no RPCore.log. O atendimento orienta usar o GPS correto; aguardar piso local não cria nova tentativa nem cobra taxa. A imagem recebida não mostra coordenadas nem estado do script; a causa da dependência geográfica foi confirmada por leitura e regressão de código, sem alegar que o GTA foi executado neste ambiente.

Não altera teoria, nota, taxas, documentos, avaliação, percurso, emissão, banco, carteira, personagens, inventário ou empresa. A lógica original de carregamento da PDM fica idêntica. A correção não depende de apagar save/configuração, demitir funcionários, recomprar prova ou voltar à PDM para liberar outro serviço.

## Instalar e testar

Feche o GTA, instale o update e confirme 0.109.1 no RPCore.log. Abra F10 → DMV longe da PDM: devem aparecer as categorias e GPS. Marque o atendimento C/M1, vá a pé e pressione E. Repita no atendimento industrial A. Para o prático, aguarde alguns segundos no próprio local se o piso ainda estiver carregando. Retome provas pagas pelo botão correspondente, sem comprar uma nova.

Se continuar sem abrir, o bloqueio agora deve mostrar um aviso; envie o texto e RPCore.log da mesma sessão para distinguir outra restrição. Não se pede reinstalar OIV/RPF/YTD nem apagar documentos. Teste depois uma prova e um trecho do percurso para conferir que não suspende por afastamento da PDM. As demais funcionalidades do grupo seguem no guia DMV-GRUPO-0.109.0.md.

## Validação

4421 testes aprovados, 30 novos. Regressão executa AreaWarmup nos pontos reais C/M1/A, confirma que os três estão fora dos 220 m e que o estado PDM permanece falso, enquanto a consulta é liberada e a preparação local fica pronta. Também cobre consulta remota, cena preparada versus em exibição, avisos por restrição, distância/piso, troca de perfil e carregamento local durante a prova. Os 4391 casos anteriores continuam aprovados. Release net48 sem erros/avisos. GTA não executado; F10/E/modelos e colisão nativa precisam da confirmação local.
