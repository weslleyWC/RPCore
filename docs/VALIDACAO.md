# Validação0.87.0 — oficina/personalização

3255 testes aprovados (3229 anteriores +26 novos), Release net48 sem erros/avisos. TRX DataTests-0.87.0.trx. Auditoria PSRP-SYSTEMS-AUDIT-0.87.0.json usa DLLs apenas como metadados e fonte exportada.

Testes novos verificam limites/slots não indexados, custos inválidos, saldo insuficiente, opção já instalada sem cobrança, bloqueio de acesso, recusa nativa simulada, perda de acesso durante aplicação, falhas de aplicação e disco com rollback e compatibilidade nullable de proteção de pneus/metadados antigos. São testes das regras e callbacks: não executam natives GTA, LemonUI, modelos, garagem ou seguro físicos.

Guia FASE-7.4-0.87.0.md cobre personalização, saldos, repetição, persistência/recuperação, exclusões, perfis, fechamento/gate e regressão dos serviços. Native world + JSON não são crash-atômicos. Nenhum OIV/RPF/YTD/áudio/default antigo foi alterado. Revisão REVISAO-PARIDADE-0.87.0.md registra táxi/limousine passageiro e clima/tempo ausentes, sem antecipar refinamentos8/9.

Instalador PowerShell7/Linux executado duas vezes sobre0.86.0 em diretório GTA simulado: **472 arquivos existentes preservados byte por byte**, incluindo carro personalizado, proteção de pneus, banco, garagem/seguro, editor/exports e configurações/tecla F2. Relatório installer-validation-0.87.0.json.
