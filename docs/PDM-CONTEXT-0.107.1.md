# 0.107.1 — interface na região da Motors

## Alterações

A interface distinguia apenas IS_CUTSCENE_ACTIVE, que também pode indicar uma cena preparada. Agora os menus principais, HUD, inventário e controles relacionados bloqueiam durante cena em execução ou cena ativa sem controle do jogador. Pausa e verificações de missão existentes continuam. Quando há apenas cena preparada e o jogador tem controle, o HUD próprio deixa de herdar o bloqueio nativo dessa cena. Não interrompe cutscenes nem força controle/câmera/HUD global do GTA.

Recursos ambientais da PDM (showroom, equipe, guardas e câmeras físicas) aguardam quatro segundos de contexto estável e colisão carregada na chegada. Teleporte acima de40m entre amostras, troca de ator/perfil, intervalo acima de1s e perda de contexto reiniciam a espera. Preparação de operações físicas também aguarda essa condição. As portas/IPLs mantêm a proteção conservadora de cenas ativas, além dessa espera; não se remove uma cena para abrir a loja.

RPCore.log recebe UI context ao mudar o estado perto da empresa ou ao pressionar F9 e teclas dos menus principais: cena ativa/em execução, controle, HUD oculto, missão, câmera, menus visíveis, modais e sistemas com falha. Amostragem automática limitada a uma por segundo e escrita só quando muda; não grava a cada frame. Log é diagnóstico, não captura de crash nativo.

## Evidências e limites

Os logs fornecidos mostram0.107.0 funcionando e posições na Motors, sem exceção recente que explique o desaparecimento. O usuário relata recuperação ao afastar cerca de200m, movimento normal e F6 iniciando turno. Isso é compatível com um bloqueio de contexto, mas os estados nativos não estavam nos logs antigos: a causa ainda não está confirmada. O crash posterior ao dirigir não está documentado pelos logs fornecidos. Este hotfix não declara esse crash resolvido.

Build Release net48 sem avisos/erros;4189 testes aprovados, incluindo27 novos casos de cena, HUD e carregamento estável. Não há execução do GTA no ambiente cloud. Personagens/troca de modelo, regras financeiras, saves, configurações e demais ciclos de trabalhos preservados. Não precisa reinstalar OIV/RPF/YTD.

## Testar no GTA

1. Fechar GTA e instalar este update. Confirmar0.107.1 no RPCore.log.
2. Ir dirigindo até a Motors; conferir HUD, F11, F2, J e inventário. Aguardar quatro segundos antes de testar recursos físicos da empresa.
3. Sair mais de220m, voltar e repetir também por teleporte. Abrir/fechar empresa, entrar/sair da concessionária e testar câmera se instalada. Não deve precisar reload dos plugins.
4. Dirigir fora da região para verificar o crash relatado. Se HUD sumir, pressionar F9 antes de sair da região e novamente fora dela.
5. Se persistir ou crashar, enviar RPCore.log, ScriptHookVDotNet.log, ScriptHookV.log e RPCore/RuntimeLastSession.json da mesma sessão, antes de outro reload. Informar se chegou dirigindo ou por teleporte e a última ação.
