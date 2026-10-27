# Validação0.94.0 — mouse e sobrecarga funcional

3602 testes aprovados (3541 anteriores +61 novos); Release net48 sem erros/avisos. DataTests-0.94.0.trx. Sem execução GTA.

Novo: transformação de coordenadas usa mesma escala/insets da renderização; hit testing respeita tiles sobrepostos e índice absoluto da rolagem. Gesto mantém apenas referência ao slot/item, confirma por MoveSlotSaved e conserva quantidades. Cobertura de vazio/troca/mesmo slot/fora, clique sem drag, cancelamento, origem alterada, erro de gravação, commit único, bordas, resolução/área segura/escala e rolagem entre janelas. Input/cursor é código nativo GTA compilado, ainda não homologado no jogo; guia INVENTARIO-MOUSE-0.94.0.md contém roteiro.

Inspeção: mouse não consome/descarta/transfere itens, modal bloqueia combate/câmera por frame; soltar fora cancela, mudança de input cancela sem ativar controle no mesmo frame; fechar/reload limpa estado. Sem OS mouse hooks, teleportes, tarefas globais ou novos modelos de save. Transferências/receitas e teclas existentes preservadas.

Peso:34 casos adicionais cobrem limite/tolerância, excesso progressivo com piso que não imobiliza, normalização imediata, quantidades/fallback/contagens grandes e14 exclusões de contexto. Ctrl+Enter tem cálculo de quantidade que cabe, preserva restantes e respeita overflow/retirada para mochila cheia. Runtime compilado usa SET_PED_MOVE_RATE_OVERRIDE e bloqueios de sprint/salto apenas por frame seguro a pé; sem multiplicadores globais ou reset que interfira em outros sistemas. Peso é mecânica própria; referência consultada fornece cálculo/UI. Teste de locomoção GTA pendente.

Instalador PowerShell7/Linux: duas atualizacoes simuladas sobre0.93.0 preservaram199 arquivos existentes, incluindo saves/configuracoes personalizadas/receitas/metadados/backups e mod externo. DLL nova confere com build e anterior em backup. Sem gameplay GTA.
