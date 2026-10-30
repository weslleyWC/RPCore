# Correção0.106.1 — portas RPCore Motors e menus PDM

RPCore Motors e PDM / empresa são serviços separados no mesmo prédio. RPCore Motors vende carros pessoais (tecla Dealership configurada, padrão F12, também disponível pelo lado de fora). PDM administra caixa/estoque/equipe/contratos empresariais (E no marcador do escritório externo, ou acesso pelos trabalhos). Comprar um não compra o outro.

## Porta e showroom

A0.106.0 revisou compra/seguro, mas não cuidava da porta física da concessionária. Esta correção trata somente os dois modelos de portas de pedestres do showroom (v_ilev_csr_door_l/r, presentes em MultiplayerCharacter.UnlockDoors da referência PSRP), em seus quatro pontos conhecidos e apenas até6m/no mesmo piso. Destrava o sistema de portas já registrado e o estado legado; mantém o ângulo atual para o personagem empurrar normalmente. Não remove objetos, congela portas, altera portão de veículos nem destrava outras lojas.

O interior vanilla usa shr_int e a fachada falsa fakeint, como em PSRP.PDMCompany. O RPCore pede o showroom sem bloquear o script ao se aproximar; esconde somente fakeint depois de shr_int ficar ativo. Não mexe em IPLs customizados, não limpa mundo nem fixa interiores em memória. Ao se afastar restaura apenas estados que modificou. Durante missão/cutscene não modifica esse estado; ao recarregar perto/dentro da loja não descarrega o piso sob o jogador. O aviso local distingue F12/carros pessoais de E/empresa, respeita a tecla configurada e não sobrescreve instruções de um contrato ativo.

## HUD e menus da empresa

Relato do usuário: ao abrir PDM / empresa, HUD de necessidades desaparece e outros menus param até reiniciar o RPCore. Sem log atual do incidente e sem GTA no cloud, a causa nativa exata não foi reproduzida/confirmada.

Há um risco verificável no código: os12menus PDM executavam ações e reconstruíam páginas dentro do callback Activated, enquanto ObjectPool.Process ainda percorria e processava os menus. Uma página/monitoramento podia abrir durante a mesma seleção e receber a tecla/click ainda segurado. Agora empresa, estoque/logística, equipe, finanças, coletas, retomada, locadora, showroom, serviços, segurança, câmeras e risco usam PersonalServiceMenu, com fila após o processamento, consumo único, bloqueio até soltar a tecla/click, invalidação de página e vínculo ao ator/modelo/perfil. Reload/Dispose descartam ações antigas; erro de ação fecha o menu e mantém o log/pendências.

Falha ao aplicar um banner/ícone agora fica restrita àquele menu, que retorna ao banner normal; não deve desativar VisualSystem/HUD inteiro. O catch de despacho fecha menus individualmente, protegendo também contra erro ao fechar um deles. A abertura do menu principal PDM registra número de linhas/perfil em RPCore.log, além do rastreador de execução existente. Não força HUD de missões/cutscenes, libera todas as atividades nem religa sistemas de jogo arbitrariamente.

A comparação automática removeu apenas as alterações de despacho/limpeza da interface e conferiu os12arquivos PDM contra0.106.0: regras de contratos, pagamentos, folha, riscos, veículos e ciclos de Tick/Save permanecem iguais.450outros arquivos fonte/config também são idênticos. Personagens/roupas, inventário, banco, compra/seguro de carro pessoal, combustível/cinto e configurações são preservados. Não é início do refinamento de realismo dos trabalhos da fase9.

## Testar no GTA

1. Fechar completamente o GTA e instalar0.106.1. Ir a pé à RPCore Motors, esperar carregar o showroom e empurrar a porta. Conferir também a saída e a outra entrada de vidro. Portão de veículos não é destravado por esta correção.
2. Abrir F12 perto da loja pelo lado de fora: catálogo pessoal deve continuar acessível. A compra/garagem/seguro da0.106.0 não mudou.
3. No escritório externo, abrir E/PDM empresa e fechar com Backspace/B/Escape várias vezes. Entre cada tentativa, conferir HUD, F11, celular e inventário.
4. Se já tem empresa, navegar entre caixa, estoque, equipe, finanças, showroom, segurança, risco e câmeras, voltando ao menu principal. Segurar Enter/click não deve acionar uma opção de outra página automaticamente.
5. Entrar e sair de uma câmera PDM, conferir restauração da visão/controles e reabrir os outros menus. Abrir o menu da empresa não deve iniciar monitoramento sozinho.
6. Caso o HUD/menus ainda parem, preservar e enviar RPCore.log e ScriptHookVDotNet.log da mesma sessão, informando a última opção selecionada. Não afirmar que o travamento do relato foi reproduzido ou resolvido definitivamente só pelos testes de dados.

4105 testes aprovados (4079 anteriores +22limites de acesso/portas +4cenários de filas entre páginas). Release net48 sem erros/avisos. Testes cloud não executam portas/IPLs/HUD/LemonUI nativo; roteiro acima continua necessário.
