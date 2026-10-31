# Validação0.107.0 — grupo PDM / empresa

4162 testes aprovados (4105 anteriores +57), zero falhas/ignorados. Release net48 sem erros/avisos. DataTests-0.107.0.trx. Natives/GTA não executados.

Novos testes verificam termos de confirmação e expiração/troca de perfil, alterações financeiras versus timers/handles, transferências acima de1milhão e limites/reserva/IO/replay, folha conjunta/vencimento/IO/metadados/overflow, resultado contábil sem estouro, negociação/inspeção/contraproposta aceita ou recusada/IO/preço fixado/pagamento único, save antigo sem negociação e identidade de entidade. Testes empresariais anteriores continuam aprovados.

450 arquivos fonte/config fora do escopo idênticos à0.106.1/6afb714. Troca de personagem comparada exatamente. Personagens/roupas/inventário/banco/compra e seguro pessoal/combustível/cinto/ciclos dos demais trabalhos preservados. Helpers compartilhados só acrescentam prova para entidades rastreadas da PDM e inicialização/liberação do argumento de carreta. PDM-SCOPE-0.107.0.json. Configurações e áudio permanecem iguais.

Instalador duas vezes sobre0.106.1 com PowerShell7/GTA simulado: 276 arquivos existentes preservados byte a byte, incluindo empresas em12cenarios (saldo/estoque/equipe/emprestimos/entrega/coleta/valores/retomada/locadora/negociacao/servico/original malformado), metadados/backups e todos os cenarios anteriores. DLL nova confere, anterior em backup e RPF externo intacto. installer-validation-0.107.0.json. Conferencia final compara111 payloads JSON/INI e audio anteriores, CRC/manifestos, fonte, DLL, auditoria e TRX; nao executa gameplay.

Foram revisadas estaticamente PSRP.PDMCompany e famílias relacionadas, sem executar a DLL. PSRP-PDM-REVIEW-0.107.0.json separa referência, implementação e diferenças; auditoria PSRP-SYSTEMS-AUDIT-0.107.0.json registra SHA da DLL final. Grupo dos12sistemas atuais, sem alegar paridade com todos os extras PSRP. Roteiro integrado e limites em PDM-EMPRESA-0.107.0.md. O travamento nativo de HUD/menus não foi reproduzido no cloud e exige teste/logs da mesma sessão.

O pacote retém DataTests.trx e o TRX da versão atual. Relatórios históricos permanecem no repositório; exclusão é apenas dos ZIPs, sem excluir fonte, configurações, dados ou áudio de jogo.
