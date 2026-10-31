# 0.107.5 — correção de Invalid rig anchor na PDM

O log fornecido confirma a referência a540d1b0: após criar a carreta no escritório, PdmLogistics.Begin chama Spawn/FindVehicle/RigSpawnSearch.Candidates para o caminhão e recebe ArgumentException: Invalid rig anchor. Posição do personagem próxima de -63.38,-1092.86,26.54; nenhuma cena estava bloqueando a interface. Não é inferência de pátio ocupado: o ponto temporário do caminhão não tinha Name, exigido por JobPoint.Valid.

A imagem posterior no porto mostra Coleta pendente / ArgumentException, referência5e6357d9. Essa referência não está no log recebido e seu stack específico não foi confirmado. Foi encontrada e corrigida a mesma omissão no ponto usado por SpawnCargo, etapa compatível com essa imagem; não se declara confirmação do novo stack sem o log correspondente.

## Alterações

Introduzido PdmSpawnPoints para construir pontos transitórios completos, incluindo Name e validação das coordenadas. Aplicado em quatro caminhos: caminhão da retirada iniciada no escritório, carros de encomendas no porto, caminhão da locadora e caminhão da retomada. Mantidas as fórmulas anteriores:19m em relação à carreta usando o heading configurado; carros no porto em Dock.X/Dock.Y+8*índice, com Z/heading originais. As posições não foram movidas para outro local para contornar a exceção.

JobPoint.Valid e RigSpawn/RigSpawnSearch continuam idênticos à0.107.4. Não afrouxa a validação nem ignora colisão/chão; corrige os objetos que eram fornecidos ao validador. Pontos inválidos continuam recusados. A âncora já nomeada do início no porto continua igual.

Wallet e dados/transações de compra, retirada, locadora e retomada idênticos. Nenhuma migração, cobrança, reembolso ou remoção de pedido. HUD, marcador do porto, menus exclusivos, personagens, roupas e Motors preservados. Escopo: PDM-ANCHOR-SCOPE-0.107.5.json.

## Testar no GTA

1. Fechar GTA, instalar o update e confirmar0.107.5 noRPCore.log. Não apagar saves/configurações nem comprar as encomendas novamente.
2. Abrir Retirar pedidos. Se houver contrato interrompido, usar Recuperar coleta interrompida conforme o menu; parar e sair dos veículos se solicitado. Depois selecionar o pedido existente e iniciar a retirada.
3. Repetir o início pelo escritório: devem ser criados carreta e caminhão sem Invalid rig anchor. Engatar, ir ao porto e conferir carros/ícones.
4. Testar também pelo marcador do porto: E, Retirar primeiro pedido, confirmar; aguardar a preparação e os carros em sequência. Verificar uma e depois duas unidades, carregamento e retorno à PDM.
5. Conferir que somente unidades recebidas deixam de aguardar coleta e que não houve novo débito. Testar locadora/retomada quando houver estoque/contrato apropriado; a correção cobre as posições dos caminhões desses fluxos, sem mudar suas regras.
6. Se ainda houver falha, enviar a referência eRPCore.log da mesma sessão. O erro a540d1b0 tem causa confirmada; outras falhas nativas/ocupação de vagas exigem sua própria evidência.

## Validação

4282 testes aprovados,13 novos. O teste de regressão recria o objeto sem Name, reproduz ArgumentException no próprio RigSpawnSearch.Candidates, e passa o ponto corrigido pela busca real de35vagas e por SpawnBox. Também cobre os dois carros no porto, preservação de coordenadas/heading/configuração, orientação do caminhão e recusa de coordenadas/pontos inválidos. Não é apenas um teste que procura Name no texto do código.

Release net48 sem erros/avisos. GTA não executado no cloud: criação física dos veículos, posição do pátio e engate ainda precisam do teste local acima. Não precisa reinstalarOIV/RPF/YTD. O log completo do usuário não foi publicado no repositório.
