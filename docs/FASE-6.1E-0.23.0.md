# Fase 6.1E — RPCore 0.23.0: venda financiada para clientes da PDM

Acrescenta a opção de entregar estoque pronto ao cliente com entrada e parcelas. Usa a entrega física existente e cria contrato financeiro somente depois da passagem das chaves confirmada. Esta subfase cobre **clientes adimplentes**. Inadimplência e retomada ficam para a próxima entrega da PDM; não se declara esse conjunto inteiro concluído. Fases 8/9 mantidas.

## Instalação

Feche o GTA, extraia RPCore-update-0.23.0.zip e abra o PowerShell **na pasta onde está Install-RPCore.ps1**:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Use seu caminho real. DLL pronta; backup da anterior, saves/configurações preservados e Jobs/PdmFinance.json instalado só se ausente. Não apague carteira/estoque. F2/celular, F11/teleporte e recursos OpenIV instalados permanecem; sem reinstalar OIV/YTD/RPF/DLC.

## Roteiro completo no GTA

1. Vá ao escritório externo da PDM a pé, sem procurado/missão/combate e sem outro trabalho físico ativo. A empresa precisa ser sua. Use **F10 → Trabalhos → PDM / empresa e entregas**.
2. Tenha uma unidade **[PRONTO]** no estoque. Compras novas ainda precisam da coleta no porto; esta atualização não pula nem cobra novamente essa etapa. Estoque anterior e carros pessoais permanecem separados.
3. **Estoque → carro → Entregar financiado / entrada estimada**. A opção **Entregar ao cliente** continua sendo a venda à vista anterior. Confira total/entrada estimados na descrição. Há limite padrão de seis financiamentos simultâneos.
4. Retire o carro empresarial azul e dirija ao cliente amarelo, como antes. Ao chegar, pare, saia, guarde a arma, aproxime-se e **E** passa as chaves durante três segundos. O cliente embarca e leva o carro.
5. Na confirmação das chaves, a unidade sai do estoque e entra no livro de contratos. O caixa recebe **só a entrada**. O total final considera condição e divulgação elegível naquele momento, mais o acréscimo contratual; depois disso, o contrato fica fixo.
6. Abra **PDM → Financiamentos / parcelas**. Confira modelo/placa, total, entrada, recebido, saldo, parcelas pagas, próximo valor e segundos ativos restantes. Reabra/Atualizar para atualizar a consulta.
7. Jogue normalmente. A cada dois minutos ativos, uma parcela entra no **caixa empresarial**, com notificação e histórico. Pode fazer outras atividades enquanto os contratos seguem. Não entra no banco pessoal automaticamente; saque/coleta de valores continuam sendo ações separadas.
8. Após quatro parcelas, aparece **[QUITADO]**, com saldo zero. Não devolve o veículo ao estoque nem conta uma nova venda. Teste salvar/recarregar no meio e depois da quitação: não deve repetir entrada/parcela.

### Exemplo padrão: Asea, sem danos e sem bônus de divulgação

| Etapa | Recebido no caixa | Acumulado desse contrato | Saldo a receber |
| --- | ---: | ---: | ---: |
| Preço à vista de referência | — | — | 11200 |
| Total financiado: +10% | — | — | 12320 |
| Entrada de 25%, na entrega | 3080 | 3080 | 9240 |
| Após 2 minutos ativos | 2310 | 5390 | 6930 |
| Após 4 minutos ativos | 2310 | 7700 | 4620 |
| Após 6 minutos ativos | 2310 | 10010 | 2310 |
| Após 8 minutos ativos | 2310 | 12320 | 0 |

O carro custou 8000 de estoque. Ao receber tudo, margem antes de outros gastos = **4320**. Salários, contratação, reparos e melhorias continuam sendo custos separados. Na entrada, a receita recebida é 3080 e o custo vendido de 8000 já é registrado; o resultado por caixa pode ficar negativo até as parcelas cobrirem o custo. O saldo a receber não é dinheiro disponível antecipadamente.

Com bônus/danos/configurações diferentes, os valores mudam. Parcelas iniciais usam divisão inteira; a última recebe o resto exato, sem criar ou perder dinheiro. Exemplo de total 12321: entrada 3080, três parcelas de 2310 e última de 2311. O contrato guarda condições/preço próprios; mudar divulgação, reparar outros carros ou editar padrões depois não reescreve parcelas existentes.

## Cancelamento, persistência e limites

Antes da passagem de chaves: cancelamento/falha/reload usa a recuperação anterior da entrega. Devolve o carro pago ao estoque com danos, sem entrada, sem contrato e sem dívida artificial. A operação ativa guarda Financed para impedir tratá-la por engano como venda à vista integral.

Depois da passagem de chaves: a venda financiada foi confirmada; estoque já não está disponível e a entrada já foi recebida. O contrato continua até quitação, mesmo após limpeza/saída do cliente ou reinício do jogo. Não há cancelamento financeiro com devolução fictícia do mesmo carro. Financed ausente em entrega antiga significa **à vista**, e Loans ausente significa livro vazio; não converte vendas antigas em contratos.

Conta tempo com jogador vivo, fora de pausa/missão e no perfil ativo. Não recebe pelo tempo offline ou por trocar o relógio do computador. Menus sem pausa continuam contando. Amostras longas são limitadas a dois segundos; progresso é salvo aproximadamente a cada cinco segundos, em consultas, ações de gestão e salvamentos. Perda abrupta antes do checkpoint pode perder o intervalo ainda não gravado, sem duplicar a parcela já persistida.

Se faltar espaço no caixa/contador de receita, a parcela fica vencida para recebimento e não desaparece. Mostra zero segundos restantes e conserva saldo. Libere limite do caixa pelos meios existentes; o próximo checkpoint tenta receber. Enquanto isso, não acumula um segundo período em cima da parcela bloqueada. Durante coleta de valores, preserva espaço para devolver o reservado antes de aceitar parcela; não permite que recebimento inviabilize reembolso do carro-forte.

Entrada, baixa do estoque, venda/custo e criação do contrato são um salvamento único. Tempo, parcela, recebido, caixa e receita também são checkpoint único. Exceção restaura estado/saldos; retry mantém recibo. Campos desconhecidos são preservados. Históricos mantêm até 42 contratos, removendo apenas os quitados mais antigos ao abrir outro; totais acumulados da empresa não são reduzidos. Limite global de validação: dez ativos; padrão de contratação: seis. Configuração posterior com limite menor não apaga contratos existentes.

Não concede empréstimo ao jogador nem debita parcelas do seu banco: **a PDM vende a prazo a NPCs**. Estes são recebimentos de clientes de uma simulação local. Não há cobrança de serviço externo ou ligação bancária real. Clientes desta subfase pagam regularmente; ainda não há sorteio de atraso, abordagem ao devedor ou retomada.

## Configuração e código

Jobs/PdmFinance.json: DownPercent 25, InterestPercent 10, Installments 4, PeriodSeconds 120, MaxActive 6. Entrada aceita 10–80%, acréscimo 0–30%, 2–12 parcelas, 60–3600 segundos por período e 1–10 contratos ativos para novas vendas. Arquivo inválido usa defaults embutidos sem apagar o personalizado. Condições existentes são snapshots e continuam iguais após alterar o padrão.

PdmFinanceData.cs: opções, contratos, cotação/entrada, abertura e progresso/recebimento. PdmFinanceSystem.cs: tempo, notificações, consulta, estatística secundária de margem recebida. PdmCompany.cs: nova opção de entrega/contratos e ramo de confirmação; Main.cs/Wallet.cs fazem a integração. PdmData.cs acrescenta Loans e Active.Financed, mantém a assinatura anterior de Begin e impede dupla venda da entrega financiada. Identidades do bem entregue ficam no contrato, sem transformá-lo em veículo pessoal.

## Estudo da PSRP.dll e próximos subsistemas

Inspeção de PSRP/PDMCompany.cs: GenerateCarFinance, IsOnFinance, repoMoneyOwed, resolução por dias, crédito no PDMBusinessBankCurrency e TransactionType.Finance; UpdateDeliveryState para entrega; HandleRepoEvent e locais/variantes de retomada para delimitar o próximo trabalho.

A referência aumenta saldos por dias e sorteia continuidade/pagamento/inadimplência. RPCore entrega agora amortização com entrada, acréscimo e parcelas por tempo ativo, integrados à entrega física, com valores próprios. Não é alegado comportamento financeiro idêntico nem retomada implementada. Código próprio, sem transplantar a classe decompilada.

Próxima subentrega prevista: inadimplência/retomada, estudando os eventos físicos da referência. Aluguel/locadora, showroom completo, demais funcionários/IA, cofre interno/roubo/seguro e propriedade/transporte de cinco carros continuam em subentregas PDM. Refinamento global de interface/mecânicas na fase 8 e de todos os trabalhos na fase 9 permanece.

## Validação

Build net48 com Enhanced 1.1.0.6/LemonUI 2.2 sem erros/avisos. **860 testes aprovados**, incluindo 36 novos: entrada/estoque/contrato, quitação/último resto, limites e reserva do cofre, cancelamento anterior, replay/rollback/reload, snapshots/campos desconhecidos, contratos simultâneos/histórico e economia anterior. Instalador exercitado em diretório simulado; auditoria estática compara DLL/baseline e referência. GTA não executado nesta nuvem.

No GTA, conferir especialmente a nova opção, entrega ao cliente, entrada exata, primeira parcela e quatro parcelas, pausa/reload/perfil, limite de contratos e regressão de venda à vista, coleta de carros/valores, salários, garagem pessoal, F2 e F11. Valores do exemplo exigem Asea sem danos e sem bônus ativo.
