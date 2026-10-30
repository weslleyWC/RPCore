# Validação0.105.0 — abastecimento e cinto

4038 testes aprovados (3996 anteriores +42), nenhum falhou/ignorado. Release net48 sem erros/avisos. DataTests-0.105.0.trx. Não executa natives/GTA.

Novos casos verificam combustível/custo/odômetro, cobrança acumulada em passos pequenos, crédito de arredondamento já pago sem saldo extra, saldo zero, IO/repetição/rollback de setter parcial/rollback incerto, reentrância, vários preços/quantidades, capacidade, parse ptBR/invariant, valores/extremos inválidos, não persistência de ordem, identidade de ped/carro/placa/pool, restauração condicional do cinto e abastecimento durante contratos sem liberar a atividade. SDK Enhanced APIs usadas pelo runtime foram compiladas; áudio, HUD, controles e ejeção não executados.

456arquivos fonte/config fora do escopo byte-idênticos0b8474d/0.104.5. Personagens/roupas/funcão de troca/inventário/trabalhos intactos; Main só injeta gate no Fuel e muda versão; Wallet só adiciona bridge interno da transação e referência canônica.26postos padrão, esquema/identidade/consumo do tanque iguais. VEHICLE-SERVICE-SCOPE-0.105.0.json registra a comparação. PSRP.Vehicle_Insurance/DrivingFeatures/UiSystem revisados estaticamente; não se declara paridade de props/animação/electric charging.

Instalador duas vezes sobre0.104.5 preserva 248 arquivos existentes byte a byte, incluindo postos/exclusões/combustível/odômetro/metadados/backups e saves anteriores. Relatório installer-validation-0.105.0.json; DLL anterior em backup e nova confere, RPF externo de teste intacto.111 payloads JSON/INI e áudio anteriores comparados; CRC/manifestos/fonte/DLL/audit/TRX verificados no pacote final.

No GTA testarG no posto com motor desligado, confirmar/cancelar, litros/valor/full, dinheiro e combustível após interrupção e reinício; contratos continuam durante abastecimento. Testar cinto/atalhos/radial/ícone/sair/trocar carro/perfil e restauração, sem herdar trava. Guia ABASTECIMENTO-CINTO-0.105.0.md. Não se considera roupa0.104.5 aprovada nativamente sem relato de teste detalhado; esta entrega não muda essa parte nem inicia os ciclos da fase9.
