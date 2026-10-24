# Fase 6.1G — RPCore 0.25.0: pedidos da locadora

Acrescenta o fornecimento de **dois carros do mesmo modelo** à locadora do aeroporto: reservar estoque pronto, carregar Packer/TRFlat, entregar no pátio externo, buzinar para o funcionário, descarregar e voltar à PDM para receber. É uma venda de frota à locadora, conforme o fluxo estudado; não é aluguel de carro pessoal por diária. Mantém saves, empresa, financiamentos/retomada, pessoal, coletas e vendas anteriores. Fases 8 e 9 permanecem.

## Instalação

Feche o GTA, extraia RPCore-update-0.25.0.zip e abra o PowerShell **na pasta onde está Install-RPCore.ps1**:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Use seu caminho real. DLL pronta; backup da anterior; saves/configurações preservados. Jobs/PdmRental.json acrescentado só se ausente. Não apague carteira/estoque. Mantém celular F2, F11/teleporte e recursos OpenIV instalados, sem reinstalar OIV/YTD/RPF/DLC.

## Preparar o pedido

1. Empresa PDM precisa ser sua. Tenha **duas unidades [PRONTO] do mesmo modelo** no estoque, motor/carroceria >=300. Padrões aceitos: Asea, Blista e Dominator, adaptados ao catálogo atual do RPCore.
2. Compras novas ainda são pagas com caixa e exigem coleta no porto antes de ficarem prontas. Para o teste com dois Asea, o custo de estoque é **16000**, além de capital/aquisição já necessários à empresa. Não compra nem concede carros ao iniciar o pedido.
3. Vá a pé ao escritório externo PDM, sem missão/procurado/combate e sem outro trabalho físico/contrato PDM pendente. **F10 → Trabalhos → PDM → Pedidos da locadora / dois carros**. Também há entrada própria **PDM / pedidos da locadora** no menu de trabalhos.
4. Escolha **Fornecer 2x modelo / até valor**. Só aparece oferta quando existem duas unidades prontas elegíveis daquele modelo. Seleciona as duas primeiras elegíveis; não mistura modelos, não usa carro pessoal e não aceita unidade ainda no porto. Unidades ficam marcadas **[LOCADORA]**, reservadas até resolver o pedido.

## Entregar e retornar

1. Localize os blips azuis de Packer/TRFlat perto da PDM. Entre no caminhão, alinhe a traseira com a frente da carreta e pare; **E** auxilia o engate. Usa os modelos/ponto/slots da configuração de logística existente.
2. Após engatar, os dois carros do estoque aparecem nos pontos de retirada da PDM. Se vaga ocupada, afaste o conjunto da área de spawn e aguarde nova tentativa em cinco segundos; não apaga veículos do ambiente.
3. Dirija cada carro azul até a traseira da carreta, pare, **saia**, aproxime-se e **E** o carrega. Conjunto parado/engatado, carro vazio e jogador a pé. Repita no segundo. Carregamento é assistido nos dois slots já usados na coleta do porto.
4. Com ambos presos, siga o GPS para **Locadora / pátio externo** em **(-911.405, -2313.331, 5.8)**. Use o acesso por rua; o pedido não exige entrar nas pistas ou em área restrita do aeroporto. Não altera polícia global nem remove procurado.
5. Pare o conjunto perto do ponto, fique como motorista do Packer e **buzine** para chamar o funcionário amarelo. Ele caminha até o caminhão. Aguarde o atendimento mantendo o conjunto parado, engatado e os dois carros vazios/presos.
6. Quando o funcionário se aproxima, faz a conferência por **cinco segundos**. Abrir menu/pausa, mover/desengatar ou perder condições interrompe e exige chamar novamente. O funcionário precisa estar vivo/perto; carros destruídos ou sem motor suficiente não são aceitos.
7. A mensagem **Dois carros recebidos pela locadora** confirma o checkpoint de entrega. Os temporários transportados são removidos; preço fica fixado com os danos registrados. **Ainda não paga** e não libera os mesmos itens para outra venda. GPS passa ao retorno da PDM.
8. Volte com caminhão e carreta, pare no recebimento da PDM, **E** devolve o conjunto e espere **três segundos** elegíveis. O caixa recebe o pedido, as duas unidades são baixadas do estoque e a operação fecha no mesmo salvamento. Saia do conjunto; limpeza aguarda ficar vazio/parado.

## Valores e exemplo

Preço padrão: **95% da cotação base de cada carro após danos**. Não usa bônus de divulgação nem juros de financiamento: é um acordo de fornecimento em lote, com regra própria. A descrição mostra valor máximo previsto. Danos no transporte podem reduzi-lo; não aumenta acima do acordado no início. Após aceite na locadora, fica fixo.

Dois Asea intactos: 11200 ×95% = **10640 cada**, total **21280 no caixa** ao retornar. Custo original: 8000 cada, total **16000**. Margem antes de outros gastos: **5280**. Com caixa inicial de 50000, depois de comprar os dois fica 34000; depois do pedido completo fica **55280**. Outras parcelas/receitas simultâneas podem alterar o saldo total; confira a linha e a notificação específicas do pedido. Banco pessoal e dinheiro na mão não recebem automaticamente. Saque/coleta de valores continuam separados.

Exemplo de dano: um Asea com carroceria 500 usa base 9800 e valor locadora 9310; outro intacto 10640; total **19950**. A condição usada é a pior salva/atual, para não restaurar preço por recarga. Motor abaixo de 100 na conferência não é aceito. Reparo antes de iniciar continua sendo o sistema de estoque existente.

Receita, custo vendido e quantidade são lançados juntos ao receber: duas vendas, custo original das duas unidades, valor efetivo acordado. Não cobra os carros de novo, não dá bônus extra por dirigir e não cria um empréstimo ou dívida de locação pessoal. Outras unidades do estoque não são removidas.

## Cancelamento e recuperação

**Antes do aceite:** End segurado por um segundo ou Encerrar pedido. Pare e saia dos carros e do conjunto. Salva danos antes da limpeza, remove só temporários reconhecidos e libera as duas unidades pagas no estoque. Não recebe nada e não perde o custo de compra, pois continua dono dos bens. Morte, procurado/missão, prazo de 30 minutos, perda/avaria grave do conjunto/carga ou desprendimento solicitam o mesmo encerramento.

**Depois do aceite:** não permite voltar a tratar os carros como estoque livre. End/Suspender retorno mantém pedido entregue e obrigação de recebimento; pare/saia e depois use **Recuperar / continuar pedido**. Isso não devolve as unidades nem paga duas vezes.

Ao recarregar:

- Antes do aceite: confere identidades, limpa só temporários próprios e encerra a reserva com estoque/danos preservados. Veículo ocupado/movendo/ligado a conjunto alheio mantém pendência; pare/saia e use Recuperar.
- Entregue com conjunto reconhecido e utilizável: retoma a rota de retorno à PDM. Não recria os carros já aceitos e não paga antes do retorno normal.
- Entregue com conjunto indisponível após interrupção/reinício do mundo: recupera o recebimento pelo checkpoint de aceite, sem exigir recriar bens já entregues. Registra recebimento recuperado, uma vez. É uma regra de recuperação para preservar valores, não prova de que o trajeto foi realizado durante a recarga.
- Devolução do conjunto já salva, pagamento ainda pendente: tenta somente liquidar o pedido, sem repetir entrega/estoque.

Identidade de veículos: handle/modelo/placa exclusivos lembrados. Não pega ou apaga tráfego/carro pessoal. Nunca remove veículo ocupado para finalizar. Durante a entrega, os veículos temporários são reservados no controle de trabalhos e rejeitados pela garagem pessoal.

Se o limite do caixa impedir pagamento, mantém pedido e recibo pendentes. Depois do aceite, a gestão permite **sacar saldo já disponível**, a pé no escritório, para liberar limite; depois Recuperar/aguardar retry. Essa exceção não libera estoque, nova contratação, aporte ou outro contrato durante a reserva. Limite de receita/counters também conserva a pendência, sem sumir com carros/valor. Não zera arquivos para resolver problemas de dados.

## Persistência e configuração

Campo novo aditivo **Wallet.json/Pdm/Rental**, ausente = nenhum pedido. Guarda ID, identidades do conjunto/carga, IDs exatos do estoque, preços por unidade, percentual, Delivered e Returned. Estoque fica reservado até o checkpoint final para não permitir venda dupla. Antes de remover carga, grava aceite; antes de pagar, grava retorno. Pagamento junta caixa/receita/custo/baixa/fechamento, com rollback e recibos idempotentes.

Jobs/PdmRental.json: três modelos iniciais, percentual 95, funcionário cs_carbuyer, dois pontos de retirada, entrega/funcionário/retorno e tempos. Percentual aceita 80–120, tempos de conferência/retorno 2–15 segundos, prazo 300–3600 segundos. Configuração inválida usa padrões embutidos sem apagar o original. Contrato guarda percentual/preços; mudanças posteriores de padrão não reescrevem o pedido ativo.

Pdm/Finance/Repo/Logistics/Staff/Safe.json anteriores permanecem iguais. Salários e demais parcelas continuam seguindo seus ciclos; não alteram o preço do lote. Gestão impede usar o mesmo estoque em financiamento, retomada, coleta ou outra entrega simultânea. F2/F11 e outros atalhos permanecem.

## Referência e limites

Estudo de **PSRP/PDMCompany.cs**: GenerateRentalOrder e pedido de duas unidades, HandleRentalDelivery, Packer/TRFlat, slots dianteiro/traseiro, pontos de retirada **(-20.6272, -1084.351, 25.5196)** e **(-14.3133, -1086.896, 26.26005)**, entrega **(-911.405, -2313.331, 5.8)**, funcionário **(-905.882, -2337.717, 6.709037)**, buzina, retorno **(-48.12541, -1075.025, 26.7742)** e pagamento posterior. Modelo de funcionário corresponde a CarBuyerCutscene. Código próprio.

Diferenças deliberadas: catálogo inicial e preço próprios; ofertas disponíveis quando há duas unidades prontas, em vez da fila aleatória diária do legado; preço ajustado por danos; fixação de carro vazio; sem invencibilidade, fades/bloqueio global de controles ou limpeza de ambiente. Spawn pode ajustar posição para vaga/chão seguros. Não inclui áudio/diálogo personalizado, fila diária de mensagens, física detalhada de rampas ou aluguel pessoal por diária/caução/devolução.

Esta é uma subentrega da PDM. Showroom completo, demais funcionários/IA, melhorias físicas, cofre interno/roubo/seguro, transporte de cinco carros/propriedade dos transportadores, variantes de retomada e demais diferenças seguem pendentes. Interface/mecânicas na fase 8 e refinamento de todos os trabalhos na fase 9 continuam.

## Validação e teste real

Build net48 Enhanced 1.1.0.6/LemonUI 2.2 sem erros/avisos; **934 testes aprovados**, incluindo 36 novos. Cobrem reserva de dois bens, identidades, danos/preço, entrega/retorno/pagamento, rollback/replay/recovery, dinheiro/counters, saque para liberar limite e máquina de estados/tempo. Auditoria estática compara cinco posições e fluxo da referência; instalador exercitado em diretório simulado. GTA não executado aqui.

No GTA, testar duas cargas e altura na TRFlat, acesso externo da locadora, funcionário/buzina/navegação, aceite e retorno, valor único de 21280 no exemplo, cancelamento antes do aceite, suspensão/reload depois, danos, ocupantes, estoque extra preservado e regressão de vendas/financiamentos/retomada/coletas/pessoal/garagem/F2/F11. A chamada “continue” durante a implementação mantém esta entrega; não é registrada como teste ou aceite no GTA.
