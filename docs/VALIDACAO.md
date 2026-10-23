# Validação — RPCore 0.15.0

Build Release net48 com SHVDN Enhanced 1.1.0.6 e LemonUI SHVDN3 2.2: zero erros/avisos. DLL SHA256 `f38413e949b4302f65d157badb9d1deca89916319f473daea9cf63bc6cb85425`.

518 testes aprovados, zero falhas/ignorados: 477 anteriores e 41 novos. Cobrem nove receitas/cadeia, quantidades/tempos, prévia sem mutação, consumo exato de caça/pesca/estoque, checkpoint de múltiplas bolsas e restauração de referências em falha, recibos/reload, capacidade/limites, venda/banco/rollback, unknowns e emprego/licença anteriores, interrupção/timer/overflow/retry, cinco lotes e exclusão entre onze atividades. Relatórios DataTests.trx/DataTests-0.15.0.trx. Testes não executam GTA/nativos/animação.

Crafting é um campo aditivo de Wallet.json. Ingredientes são descontados de Hunting/Fishing/Stock e resultado criado na mesma gravação; em exceção, referências anteriores são restauradas. Venda remove conhecidos e credita banco atomicamente no mesmo arquivo, sem mexer em matérias-primas ou itens pessoais. Produtos são mercadorias do estoque da bancada; não são consumíveis do inventário I. Inventory.json/baús/porta-malas não foram convertidos. Unknowns conservados, recibos recentes limitados a 512, dados inválidos não zerados. Fabricação ativa não retoma após reload; estoque/progresso persistem por perfil.

Auditoria PSRP-SYSTEMS-AUDIT-0.15.0.json: DLL/baseline 0.14.0 e 75 fontes de referência. NewInventorySystem tem CraftingRecipe/seleção/quantidade e comércio. Ponto corresponde a burgerStand da referência; função de bancada/feira e nove receitas/sete produtos são próprias. Não se afirma catálogo completo/mesmas receitas da PSRP; drogas e penhor/sucata ficam para suas etapas/revisão. Sem transplante de fonte ou alteração de OIV/YTD/RPF/DLC/dlclist.

Inspeção nativa: só fabrica no ponto público a pé/desarmado/sem procurado/missão/combate. Modal/pausa/afastamento/morte/veículo/troca de perfil cancelam antes de confirmar. Animação mini@repair tem timeout/fallback; só para o próprio clipe no ped original. Sem ClearAllTasks/congelamento ou limpeza de NPCs. Trava compartilhada com as dez atividades anteriores. Mensagem do instalador corrigida para os atalhos atuais, sem reescrever INIs.

Pesca 0.14.0 foi aprovada pelo usuário. Crafting/feira 0.15.0 não foi executado no GTA nesta nuvem: testar ponto/menu/animação, receita de pesca/caça, cadeia/estoque/venda/reload, cancelamento e regressão dos sistemas anteriores. Roteiro em FASE-5.3-0.15.0.md. Refinamentos dos trabalhos após fases principais; tráfico/streamer/arrancada continuam no roteiro.

Instalador: duas execuções via PowerShell 7/Linux em diretório GTA simulado, sem gameplay. Os 95 arquivos existentes ficaram byte a byte iguais, incluindo saves, caça/pesca e F2. Crafting.json foi acrescentado na primeira execução e personalização preservada na segunda; mensagem F2/celular confirmada, DLL anterior em backup e nova DLL igual ao build. Relatório installer-validation-0.15.0.json.
