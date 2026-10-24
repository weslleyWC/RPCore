# Validação — RPCore 0.21.0

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI 2.2: zero erros/avisos. DLL SHA256 `fdd1bea7da900e9541de55e28e4f43eb7fc67311b3f8cf366040f45f7aae7818`.

783 testes aprovados, zero falhas/ignorados: 745 anteriores e 38 novos. Cobrem vínculo/termos individuais, hierarquia/caixa, tempo/fronteira/vencimento/snapshot, dívida/overflow, pagamento exato e replay/rollback, demissão/proporcional/multa/acerto, melhorias, desconto efetivo em reparo, bônus efetivo em venda, bloqueios durante entrega/coleta e preservação de campos desconhecidos. Relatórios DataTests.trx/DataTests-0.21.0.trx. Não executam GTA.

Estado aditivo Wallet.json/Pdm/Staff: empregados, termos/segundos/devido, acerto de ex-funcionários, despesas pagas, contratações e melhorias. Ausência do campo mantém empresa sem funcionários, sem cobrança retroativa. Salários acumulam por período contratado enquanto o jogador está ativo, sem débito automático. Pagar reduz caixa e dívida no mesmo checkpoint. Demitir transfere vencido/proporcional/multa ao acerto. Benefícios suspendem por dívida; cobrança/preço que mudou desde abertura do menu exige revisar valor. Exceções restauram estado/saldo; recibo do tempo pendente é mantido para retry. Perfil e período offline não acumulam salários.

Integração: mecânico elegível + oficina reduz reparo de estoque; divulgação elegível aplica maior bônus e melhoria na venda real, junto da baixa do item/receita. Mantém regras de condição/estoque/garagem pessoal. Resultado subtrai despesas efetivamente pagas; dívida aberta aparece separada. Não modifica a coleta/exposição física da 0.20.0, seus pontos/slots/config, nem recursos OpenIV.

Auditoria PSRP-SYSTEMS-AUDIT-0.21.0.json: 75 fontes indexadas, baseline 0.20.0 e identidade da DLL atual; evidência de StaffManagement, quatro funções, RegisterWages e dívida de ex-funcionários na PDMCompany.cs. Diferenças documentadas: referência usa sete dias empregados; RPCore usa cinco minutos ativos e valores/efeitos próprios. Nenhuma alegação de paridade de IA física, economia semanal ou vendas automáticas.

Roteiro FASE-6.1C-0.21.0.md para GTA: contratar/requisitos, salários após cinco minutos, caixa insuficiente/pagamento/retorno de benefício, demissão/acerto, reparo 500/1000, Asea 11424 com júnior ou 11648 com júnior + divulgação, reload/perfil e regressão de estoque/coleta/exposição/cliente/F2/F11/garagem. Jogo não executado nesta nuvem. Próximas subentregas PDM permanecem; fases 8 e 9 mantidas.

Instalador executado duas vezes via PowerShell 7/Linux sobre instalação simulada da 0.20.0: 101 arquivos existentes preservados byte a byte, incluindo saves, configurações, F2 e dados PDM. PdmStaff.json acrescentado na primeira execução, personalização conservada na segunda; backup da DLL anterior e DLL instalada idêntica ao build. Relatório installer-validation-0.21.0.json. Não é teste de gameplay.
