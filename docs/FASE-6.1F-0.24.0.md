# Fase 6.1F — RPCore 0.24.0: inadimplência e retomada PDM

Novos financiamentos podem entrar em atraso e exigir recuperação do próprio veículo com Packer/TRFlat. Esta entrega cobre a variante **carro estacionado** da referência. Contratos já criados na 0.23.0 permanecem adimplentes, sem impor risco retroativo. Mantém caixa/estoque/pessoal/coleta de valores/entregas e os saves existentes. Fases 8 e 9 continuam na ordem acordada.

## Instalação

Feche o GTA, extraia RPCore-update-0.24.0.zip e abra o PowerShell **na pasta onde está Install-RPCore.ps1**:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Use seu caminho real. DLL pronta, backup da anterior, configurações/saves preservados. Acrescenta Jobs/PdmRepo.json só se ausente. Não apague carteira ou contratos. Não reinstala/edita OIV/YTD/RPF/DLC. F2/celular e F11/teleporte continuam.

## Risco e contratos antigos

**Contratos antigos da 0.23.0 não entram em atraso:** o campo novo ausente tem risco zero. Novos financiamentos pela interface guardam o risco configurado em PdmRepo.json, padrão **20% por parcela**, informado no menu. Condições e risco ficam fixos na confirmação da entrega do carro ao cliente. Alterar o arquivo depois não altera empréstimos já criados.

Quando uma parcela vence, a decisão é estável por contrato/parcela: salvar/recarregar ou falhar na gravação não sorteia novamente. Se há atraso, essa parcela não credita dinheiro e os recebimentos automáticos desse contrato param. O cliente fica **[EM ATRASO]** em Financiamentos. Entrada e parcelas já recebidas ficam no caixa; não são pagas outra vez. O contrato continua ocupando limite de financiamento até resolver a retomada.

Não há cobrança offline nem alteração de dinheiro pessoal. Temporizadores, juros, entrada e parcelas dos demais contratos continuam conforme a fase anterior. No padrão de 20%, nem todo contrato vai falhar; pode quitar normalmente.

### Teste sem depender do risco padrão

Com o jogo fechado, após instalar, edite **GTA\scripts\RPCore\Jobs\PdmRepo.json** e coloque `"DefaultPercent": 100`. Inicie o jogo e faça **um financiamento novo** pelo estoque PDM. Após entregar o carro ao cliente, a primeira parcela, em 120 segundos ativos no padrão financeiro, entrará em atraso. Isso não concede dinheiro nem cria carro gratuito: exige estoque comprado/coletado e entrada/contrato normais.

Depois do teste, pode voltar o arquivo a 20 com jogo fechado para novas vendas. O contrato criado com 100 mantém seu snapshot. Zero desliga o risco apenas para novas vendas. Não apague carteira nem edite saldos para testar.

## Roteiro de retomada no GTA

1. Crie um financiamento novo e aguarde **[EM ATRASO]**. Os contratos antigos de risco zero não servem para esse teste. Abra **F10 → Trabalhos → PDM → Financiamentos / parcelas**; selecionar o contrato em atraso abre a retomada com esse carro priorizado.
2. Também há **PDM → Retomada / contratos em atraso** e entrada própria **PDM / retomada** no menu de trabalhos. Use GPS e vá a pé ao escritório externo, sem procurado/missão/combate. Encerre outro trabalho físico e tenha **uma vaga livre no estoque empresarial**.
3. Escolha **Retomar modelo / placa**. Não cobra outro carro nem paga recompensa. Cria somente o conjunto temporário da empresa e registra o contrato em andamento. Se o carro da entrega original ainda existir, aguarde sua retirada: não cria cópia enquanto ele estiver no mundo.
4. Entre no **Packer azul**, alinhe a traseira com a frente da **TRFlat** e pare. **E** auxilia o engate. Usa os modelos/slots e a área de saída já configurados em PdmLogistics.json. GPS passa ao último local conhecido do alvo.
5. Dirija até o endereço. O carro correspondente ao contrato aparece com blip azul ao se aproximar com a carreta. Modelo/placa/condição vêm do bem financiado, sem pegar veículo aleatório do ambiente. Spawn verifica espaço/chão/colisão; se ocupado, afaste o conjunto da vaga e aguarde nova tentativa em cinco segundos.
6. Entre no carro alvo usando as chaves disponíveis, aproxime-o da traseira da carreta, pare e **saia**. Fique perto do carro e **E** o carrega. Caminhão/carreta devem estar parados e engatados; alvo vazio. Se estiver sem motor/avariado, aproxime a traseira do conjunto do alvo e use o carregamento assistido, sem precisar dirigir o carro sobre uma rampa.
7. Volte ao recebimento da PDM pelo GPS. Pare o conjunto e pressione **E**, aguardando cinco segundos. Precisa do próprio carro carregado, vazio e preso à própria carreta. Menus/pausa, movimento ou desengate interrompem e exigem iniciar descarga novamente.
8. Recebimento confirmado remove a representação transportada, **devolve a unidade ao estoque com os danos** e marca o contrato **[RETOMADO]**. Saldo restante deixa de ser cobrado; não credita parcela fictícia, bônus ou valor total da dívida. Conjunto é recolhido quando vazio/parado; saia dele antes de novo trabalho.
9. Consulte **Estoque**. Se necessário, pague reparo pelo sistema existente e entregue/venda o carro novamente. O histórico do contrato retomado permanece; o veículo recuperado não vira carro pessoal nem entra na garagem pessoal.

## Dinheiro e custo do carro

Retomada é recuperação de patrimônio, não pagamento em dinheiro. O campo **FinanceWriteOff** registra o valor contratual que deixou de ser cobrado, separado de caixa/receita; não é uma segunda despesa descontada do dinheiro já recebido.

O custo original volta a pertencer ao estoque e é retirado de SoldCost no mesmo checkpoint de retorno. Quando revender, esse custo será reconhecido uma vez novamente. Isso evita contar duas vezes a compra de um único carro. Número de vendas mantém os contratos realizados; não confunde quantidade de negócios com quantidade de carros comprados.

Exemplo, Asea sem danos/divulgação: compra 8000; financiamento total 12320; entrada recebida 3080; atraso na primeira parcela. Após retomada, caixa não muda, estoque volta com custo 8000 e saldo encerrado é 9240. Ao revender à vista por 11200, receita acumulada dos dois negócios é **14280**, custo vendido final **8000**, margem antes de outros gastos **6280**. A compra original continua contabilizada uma única vez em StockSpent. Salários/reparo/melhorias continuam sendo gastos separados.

Se o atraso acontecer depois de parcelas recebidas, conserva esses recebimentos e encerra **só o saldo que faltava**. Não restaura condição perfeita: dano salvo permanece até reparar no estoque. Retomar encerra a obrigação financeira deste contrato na simulação; não mantém cobrança residual do mesmo veículo depois de recolhê-lo.

## Cancelamento e recuperação

End segurado por um segundo ou Encerrar retomada solicita encerramento. **Pare e saia do caminhão/carreta e do alvo**. Aguarda veículos vazios/parados e ausência de ligação com trator/carreta alheios. Salva danos antes de remover somente os temporários reconhecidos. Contrato continua em atraso e carro não entra no estoque; pode tentar novamente. Morte, procurado/missão, prazo de 30 minutos, perda do conjunto/alvo ou desprendimento da carga usam o mesmo fluxo.

Alvo que ainda existe pode ser carregado avariado pelo guincho assistido. Se sumiu durante a operação, grava condição zero; nova tentativa pode exigir aproximar a carreta do carro avariado. Reiniciar o mundo sem temporários usa a condição já salva, pois não comprova destruição.

Chegada salva é uma confirmação durável: depois dela não permite cancelar como se nunca tivesse recuperado. Ao recarregar, **Recuperar retomada interrompida** conclui retorno ao estoque; antes da chegada, apenas encerra a operação e mantém inadimplência. Verifica handle/modelo/placa do conjunto e do alvo, sem apagar veículo ocupado ou de outro mod. Nunca cria outro alvo automaticamente sobre operação persistida.

Se reduzir a capacidade configurada do estoque enquanto há retomada recebida pendente, pode bloquear a confirmação. Restaure capacidade suficiente e use Recuperar; não apague estado. Falha de gravação mantém checkpoint para retry e não duplica estoque/baixa. Alterações financeiras e retorno do item são um salvamento único.

## Configuração e referência

**PdmRepo.json**: DefaultPercent 20, UnloadSeconds 5, TimeLimitSeconds 1800, retorno PDM e 11 endereços/rumos de retomada normal. Valores válidos: risco 0–100%, descarga 2–15 segundos, prazo 300–3600 segundos, 1–30 locais. Arquivo inválido usa padrões embutidos, preservando original. Modelos, ponto da carreta e slot dianteiro vêm da configuração de logística existente; não reescreve PdmFinance/Logistics/Staff/Safe.json.

Estudo de **PSRP/PDMCompany.cs**: GenerateCarFinance/resolução por dias, repoIndex, SpawnRepoEventVehicle, NormalRepoLocations e HandleRepoEvent/onNormalRepo. Conferidos os 11 locais com heading, Packer/TRFlat, offsetFront e retorno (-48.12541, -1075.025, 26.7742). Implementação própria.

Diferenças deliberadas: risco determinístico por vencimento/contrato com snapshot; sem invencibilidade, teleporte de veículos, remoção de ambiente ou carregamento com jogador dentro do carro. Usa o ciclo nativo de engate e fixação assistida já existente; não simula cabos/rampa com física detalhada. Spawn pode ajustar posição para vaga segura. Não altera polícia global; se ficar procurado, encerra sem dar baixa falsa no contrato.

Escopo físico desta subfase: **alvo estacionado**, com chaves e recuperação. Ainda não inclui devedor dirigindo, confronto com devedor, negociação/cobrança alternativa, locadora, showroom inteiro, demais funcionários/IA, cofre interno/roubo/seguro e transportador de cinco carros/propriedade dos veículos. Esses pontos seguem para subentregas e refinamento; fases 8/9 mantidas.

## Arquivos e validação

PdmRepoData.cs: opções, operação, registro do alvo, danos, chegada/retorno/cancelamento. PdmRepoSystem.cs: menu, conjunto/alvo, engate/carregamento/retorno, cleanup/recuperação. FinanceData/System acrescentam risco, estados de atraso/retomado e consulta. PdmData/Wallet/Main/Company integram estado/transações/menus; coleções/segurança/pessoal bloqueiam ações incompatíveis com retomada. Estado novo aditivo: Pdm.Repo, Pdm.FinanceWriteOff e campos de risco/estado/handle em Loans.

Build net48 Enhanced 1.1.0.6/LemonUI 2.2 sem erros/avisos; **898 testes aprovados**, incluindo 38 novos. Testes cobrem risco/reload/rollback, dívida e parcelas, operação/identidades/dano/chegada, devolução de estoque, revenda/custo, capacidade/limites e campos desconhecidos. Reutiliza máquina de carga já testada. Auditoria estática e instalador simulado não executam GTA.

No GTA, conferir novo contrato em atraso, acesso ao endereço/geração do alvo, engate/altura/carga, retorno/baixa única, revenda e custo, cancelar/reload antes/depois da chegada, ocupantes/danos, contratos antigos adimplentes, parcelas de outros contratos, coletas/pessoal/garagem/F2/F11. Trânsito, acesso aos 11 pontos e compatibilidade física dos slots precisam da sua validação na instalação real.
