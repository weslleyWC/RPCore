# Validação0.90.0 — refinamento de roleplay, necessidades e menus

3415 testes aprovados (3372 anteriores +43novos); Release net48 sem erros/avisos. TRX DataTests-0.90.0.trx. Auditoria PSRP-SYSTEMS-AUDIT-0.90.0.json registra fonte/fluxos e diferenças, sem executar DLL PSRP.

Cobertura nova: clock com pausa/stall/wrap, fome/sede/sono/descanso por tempo e independência de FPS, taxas inválidas/limites, vida utilizável, stamina/sono/recuperação/cooldown, retirada exata de item/containers/metadados, recibo idempotente/rollback e IO real entre dois arquivos com recarga; preset RP uma vez, escolhas posteriores, pendência/corrupção/rollback, gangue/reputação/dinheiro preservados; depósito/saque/overflow/rollback; grade6x4/página24 e preservação de item nas últimas posições legadas.

Mudanças runtime verificadas por compilação e inspeção: F11 oito categorias, F10 quatro grupos fixos/sem recriar pools, testes/toggles avançados, consumo antes de benefício nas duas interfaces, avisos Fitness, oxigênio e efeitos de exaustão seguros, chave de perfil antes de hotkey. São verificações estáticas; natives, layout, modelos/IA, percepção visual e os demais fluxos no GTA precisam do guia FASE-8.1-0.90.0.md.

Instalação não migra progresso. No primeiro uso runtime autorizado pelo pedido do usuário, RoleplayVersion1 habilita regras por perfil sem mexer em dinheiro/itens/territórios; aguarda atividade ativa ou journals invalidos; pendencias validas conservam seus snapshots e preserva escolhas feitas após a migração. Preferências globais INI e assets permanecem intocados pela atualização. Recibos recuperam necessidades, não repetem cura nativa; não há alegação de atomicidade entre arquivos/natives nem de ausência de todo bug exclusivo do jogo.

Instalador PowerShell7/Linux: duas atualizacoes sobre0.89.0 em GTA simulado, **494 arquivos existentes preservados byte por byte**, incluindo recibos pendente/aplicado/legado, fome/sede/sono/stamina/cooldown, itens/containers/slots e marcadores/preferencias posteriores de RP. A copia de arquivos nao executa o preset; a migracao runtime e coberta pelos testes de regras. Relatorio installer-validation-0.90.0.json.
