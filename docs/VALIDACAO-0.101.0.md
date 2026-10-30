# Validação0.101.0 — app RPCore Bank

3889 testes aprovados (3855 anteriores +34). Release net48 sem erros/avisos. DataTests-0.101.0.trx. Sem execução GTA.

Os34 casos novos incluem valores digitados simples/com milhares, quantias até o máximo numérico, rejeição de sinais/decimais/milhares malformados/overflow, depósitos e saques de1,5bilhão conservando o total e metadados, confirmação de uso único, troca de perfil/ator/modelo/saldos, falha de gravação com rollback dos dois saldos e sem replay, transferência recusada, capacidade máxima, origem inválida/sem saldo, hit area do novo app sem mover os12 anteriores, navegação do banco para a grade, botões/rodapé e exibição do valor completo.

Integração compilada: app renderizado dentro do PhoneSystem, tema roxo/branco próprio, quatro operações e conferência com cancelar selecionado, comprovante após gravação, rotas bancárias de contatos/busca/settings sem abrir F10, guardas de botão mantido/fechamento/perfil/contexto. Usa os métodos Wallet/BankTransfer anteriores; nenhuma migração de saldo ou novo arquivo monetário. Conquistas atualizadas após commit, com falha isolada.

Sem cap diário ou valor fixo; mantido o limite numérico anterior int.MaxValue por saldo. Isso foi documentado em RPCORE-BANK-0.101.0.md. Testes nativos de renderização/input/teclado de valor/celular e confirmação de saldos dentro do GTA permanecem pendentes.

Instalador: duas atualizações simuladas sobre0.100.1 preservam208 arquivos existentes byte a byte, incluindo Wallet/Phone e todos os fixtures anteriores. DLL nova corresponde ao build, anterior em backup e RPF externo intocado; relatório installer-validation-0.101.0.json. JSON/INI e áudio anteriores comparados aos arquivos do pacote.
