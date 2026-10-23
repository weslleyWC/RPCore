# RPCore 0.12.2 — teleporte para testes

## Usar

1. Marque um waypoint no mapa, ou use GPS do trabalho para criar o waypoint.
2. Abra F11 e selecione **Teleportar para o waypoint**, primeira opção do menu.
3. Aguarde o carregamento do terreno. A pé leva o personagem; em carro/moto/ônibus parado, como motorista, leva o veículo com seus ocupantes. Para cancelar enquanto aguarda, abra F11 e selecione a opção novamente.

Serve para chegar ao ponto de contratação e agilizar os testes. Aeronaves, barcos, trens e veículos com carreta engatada não são movidos por esta opção: nesses casos faça o teleporte a pé, ou desengate a carreta. No banco de passageiro é preciso sair ou assumir a direção. Durante missão nativa fica indisponível.

O ponto pode ser ajustado em até cinco metros por eixo para evitar veículos, peds e cenário. Terreno/colisão são solicitados em ticks; sem chão válido, posição livre ou carregamento consistente, cancela após até dez segundos e mantém/restaura a origem. O estado de congelamento anterior é restaurado e o foco temporário de streaming liberado no fim/cancelamento/troca de perfil/abort. Água detectada acima do chão é rejeitada. Não usa teleporte para uma altura arbitrária quando o chão falha. Interiores, telhados e colisões de mapas modificados precisam de teste real.

Não remove procurado, cura, credita dinheiro nem muda qualificação. Timers e condições dos trabalhos continuam; teleporte não substitui a observação de voo exigida pelo piloto. A correção de base pública de Grapeseed da 0.12.1 continua incluída. Saves/configurações/F2 e recursos OIV/YTD/RPF/DLC permanecem iguais.

## Arquivos alterados e validação

WaypointTeleport.cs implementa a ação, carregamento, reposicionamento e cancelamento. SettingsSystem.cs acrescenta a opção no topo do F11 e integra Tick/Reload/Dispose. RigSpawn.cs acrescenta a checagem do espaço de teleporte, reutilizando limites dos modelos e sondagens; os spawns anteriores mantêm o parâmetro de exclusão padrão nulo. AssemblyInfo.cs/Main.cs/CoreMenu.cs/VisualAssetDefaults.cs atualizam a versão para 0.12.2. Documentação e distribuição atualizadas.

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI SHVDN3 2.2: zero erros/avisos. Os 396 testes anteriores de regressão passaram. Não foram acrescentados testes que simulem falsamente streaming/teleporte: esta ação depende dos nativos do GTA, inspecionados e compilados aqui, mas precisa do teste no jogo.

Teste solicitado: waypoint distante a pé; carro/moto parados como motorista; sem waypoint; destino ocupado/água; cancelamento durante carregamento; troca de perfil; preservar F2 e contratar um trabalho após chegar. Confira que personagem/veículo não ficam congelados e o cenário volta a carregar normalmente. Para piloto, marque a base pública pelo GPS e teleporte a pé até Grapeseed; depois faça o voo normalmente. Os voos da 0.12.1 ainda aguardam seu aceite real.
