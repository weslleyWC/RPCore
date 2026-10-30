# Validação0.106.0 — concessionária e seguro

4079 testes aprovados (4038 anteriores +41), zero falhas/ignorados. Release net48 sem erros/avisos. DataTests-0.106.0.trx. Nenhum native/GTA executado.

Novos testes verificam compra com carteira/registro/tanque no mesmo save, cópia de snapshot, preservação de metadados/referências, IO e rollback, setter de dinheiro parcialmente falho, falha também na restauração, reentrância/repetição, nova confirmação após falha, mudança de saldo/garagem/capacidade/wallet, identidade/tanque já existente, capacidade/snapshot inválidos, saldo exato, e confirmação do seguro com banco/apólice/pedido/início alterados. Testes anteriores do seguro continuam cobrindo cobrança/retomada/estorno e rollback.

459 arquivos fonte/config fora do escopo byte-idênticos à0.105.0/9787ac5. Main só injeta ShiftGate na concessionária e muda versão. Wallet só acrescenta bridge interno para compra. Personagens/roupas/inventário, combustível/cinto, transações e schema do seguro, defaults JSON/INI, garagens e trabalhos intactos. DEALERSHIP-INSURANCE-SCOPE-0.106.0.json registra comparação.

Instalador executado duas vezes sobre0.105.0 em GTA simulado com PowerShell7: 252 arquivos existentes preservados byte a byte, incluindo catálogo/garagens personalizados, seguros/pedidos iniciados e não iniciados/recibos/metadados/backups e todos os cenários anteriores. DLL nova confere, DLL anterior em backup e RPF externo de teste intacto. installer-validation-0.106.0.json registra os resultados. Comparação final inclui111 payloads JSON/INI e áudio anteriores, CRC/manifestos/fonte/DLL/audit/TRX; não executa gameplay.

Referência estática PSRP.Dealerships/Vehicle_Insurance; diferenças explícitas no guia CONCESSIONARIA-SEGURO-0.106.0.md e auditoria PSRP-SYSTEMS-AUDIT-0.106.0.json. Entrega/colisões/GPS/interface/controles nativos precisam do teste no GTA. Não se declara aprovação do grupo0.105 apenas por pedido de continuar; personagens/roupas anteriores permanecem iguais, sem retorno dos experimentos de carregamento automático. Fase9 dos ciclos de trabalho não iniciada.
