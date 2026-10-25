# RPCore 0.52.0 — fase 6.5J: frete de reposição territorial

## Instalar

Baixe RPCore-update-0.52.0.zip da branch rpcore-entrega e extraia. Com GTA fechado, abra PowerShell na pasta que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

DLL pronta. Mantém GTA V Legacy, SHVDN Enhanced1.1.0.6/LemonUI2.2, .oiv/.rpf instalados, celularF2 e F11. Não use PSRP.dll junto. Supply.json só é acrescentado quando ausente; Business.json/Treasury.json e demais configurações/saves anteriores não são substituídos.

## Rota completa

1. Filie-se a uma gangue cadastrada e controle uma área. Resolva disputas/iniciações pendentes. Pelo F4 → Comercio dos territorios → GPS escolha a área.
2. Leve **seu carro ou van**, pare no território a até **Radius+5m** do centro (17m padrão), permaneça como motorista e abra **F4 → Comercio dos territorios → Frete:10 unidades / $150** da área correspondente. Não aceita moto, avião ou veículo reservado de emprego/atividade. O carro precisa estar utilizável e parado.
3. O banco perde **$150** uma vez (lote padrão de10 unidades, com25% de desconto sobre a compra direta de200). O estoque ainda não aumenta. Uma reserva por perfil, bloqueando compras/vendas até concluir ou estornar.
4. Siga o blip verde para **Fornecedor Davis**, depois Little Seoul e Burton. Estacione a até35m de cada ponto, desça, caminhe ao marcador e pressione **E para pegar uma caixa**. Leve-a à **traseira do mesmo veículo** e pressione **E para carregar**. O contador avança e o GPS indica o próximo fornecedor. Faça as três coletas.
5. Com carga3/3, **dirija o mesmo veículo de volta ao território** e estacione a até25m do centro. É necessário chegar como motorista. Desça e vá à traseira; **E retira uma caixa**. Leve-a ao marcador do território e **E entrega**. Repita para as três caixas.
6. Após descarga3/3, pressione **E no ponto mais uma vez para confirmar o lote**. Agora estoque+10 e Purchased+10; sem nova cobrança, salário ou receita automática. Depois o atendimento de45s e a tesouraria da0.51 continuam disponíveis normalmente.

A traseira usa offset de3,2m atrás do centro do veículo; prefira um carro comum/van e deixe espaço livre atrás. A coleta a pé exige proximidade do ponto e veículo parado/próximo, sem outro motorista. Descarga exige veículo ainda utilizável e próximo; não é possível retirar estoque apenas visitando os três pontos sem carregar e devolver as caixas.

As caixas em mão reutilizam DeliveryCargo, respeitando Visuals.Enabled/UseProps. Preferem ups_box instalado e usam prop_cs_cardbox_01 como fallback. Se props estiverem desligados ou indisponíveis, os marcadores e a lógica continuam funcionando. Não há caixas extras no chão nem props montados no veículo nesta etapa. O ponto ativo reserva temporariamente o contexto de interação para impedir que uma conveniência próxima abra seu menu ao apertar E; a reserva é liberada ao suspender/encerrar. Nenhuma pessoa/carro alheio é apagado ou alterado; seu carro é somente reservado temporariamente para evitar concorrência com outros sistemas RPCore, depois liberado.

## Suspender, retomar e cancelar

**END** ou Suspender frete no menu conserva a reserva paga. Morte, combate/procurado, missão/cutscene, troca de personagem/perfil/afiliação, perda da posse, carro destruído/desaparecido/identidade alterada, entrar em veículo carregando caixa, afastar-se150m do carro ou expirar o prazo ativo suspendem a rota. Não há estorno automático nem estoque parcial. Prazo padrão1800 segundos ativos; pausa/menus/modal e gaps de frame maiores que2s não contam.

**Retomar frete reservado**: se ainda faltar descarregar alguma caixa, traga um carro/van próprio parado ao território do contrato e fique como motorista. A retomada refaz as três coletas e descargas físicas, mantendo os$150 já pagos; não precisa preservar o mesmo veículo entre sessões. O snapshot financeiro e pontos do contrato permanecem. Nenhum handle de carro/prop é restaurado de save.

Se as três caixas já foram descarregadas, a reserva permanece **pronta** após reload. Retome a pé perto do destino e confirme E, sem refazer rota ou exigir carro.

**Cancelar frete e estornar** devolve somente o custo reservado ao banco e remove o pedido, sem gerar estoque. Pode cancelar inclusive após sair da gangue, perder a área ou remover a definição do território, desde que em segurança e sem outra atividade. Pode cancelar uma entrega pronta ainda não confirmada; não recebe mercadoria e dinheiro juntos. Se o banco estiver no limite int.MaxValue, libere espaço para o estorno. Falha de gravação ou limite não apaga a reserva.

## Persistência e compatibilidade

Wallet.json → GangBusiness.Supply é opcional. Guarda Id/Gang, snapshot da área e fornecedores/opções, Units/Cost/Capacity, Loaded e Delivered. Saves0.51 sem Supply continuam válidos. O campo é removido ao completar/estornar; estoque/receita/contas/recibos antigos e metadados de outros sistemas são conservados.

Cada checkpoint clona somente GangBusiness e confirma por uma gravação de Wallet; banco/livro originais são restaurados se falhar. Replay/reload não repete cobrança, carga/descarga, estoque ou estorno. Retomada parcial limpa provas físicas e respectivos recibos para que as novas caixas sejam verificadas novamente. Posse/afiliação/área atuais são rechecadas antes do lote final; um rival não herda sua reserva.

Gangs/Supply.json configura DiscountPercent0–50 (padrão25), TimeLimitSeconds300–3600 (padrão1800) e exatamente três Suppliers distintos com Name/X/Y/Z/Heading. Pontos padrão e headings vêm de cafeSupplyParcelLocations da PSRP. Configuração inválida usa padrões em memória sem substituir arquivo. O contrato captura custo/quantidade/capacidade/pontos/prazo ao reservar; editar configurações não reprecifica pedidos pagos. Se uma configuração mudou o ponto do território, pedidos anteriores continuam usando o destino capturado; cancelar/estornar continua disponível.

## Comparação com PSRP e limites

GangSpawner implementa Pickup/Load/Unload/DeliverCafeSupplyParcel, três fornecedores e Burrito fornecido para o território4. CompleteCafeSupplyContract dá2x lavagem por60 minutos reais e MissionWin. Aqui o código é próprio: veículo do jogador, lote comercial pago com desconto, qualquer área configurada sob sua posse e reserva recuperável. Não implementa bônus de lavagem nem reputação dessa missão, não cria salário ou dinheiro sujo e não importa contratos PSRP. Auditoria PSRP-SYSTEMS-AUDIT-0.52.0.json registra coordenadas e diferenças.

Comércio criminal/estoques específicos/lavagem, clientes, simulação completa do café, guerras automáticas/diálogo/polícia continuam pendentes. Fases8 (interface/mecânicas) e9 (refinar todos os trabalhos) mantidas.

## Teste no GTA

- Anote banco/estoque, reserve: banco−150/estoque igual. Faça três coletas/carregamentos e três descargas; antes da confirmação final ainda sem estoque. Confirme: estoque+10 sem novo débito. Reabra/reload/E repetido não duplicam lote.
- Tente iniciar sem capacidade/dinheiro, em moto, longe, como passageiro ou com outro trabalho: sem cobrança parcial. Tente descarregar sem todas as coletas ou sem chegar dirigindo o veículo: recusado.
- No meio da rota, END/reload; retome no destino com carro parado e refaça carga sem novo débito. Suspenda após descarga3/3, reload, retome a pé e confirme sem refazer a rota.
- Cancele no início/meio/pronto: estorno150 uma vez, estoque igual. Depois de completar, não existe estorno do pedido encerrado. Confira reserva após morte/procurado/perda de posse e cancelamento mesmo sem filiação.
- Confira que seu carro, placa, danos, combustível e ocupantes continuam intactos pelo instalador/cancelamento/limpeza do script (o uso normal no GTA continua consumindo combustível e podendo causar danos). Depois confirme que volta a ser utilizável por outros sistemas RPCore.
- Teste compra direta/atendimento e receita na tesouraria após encerrar frete; confira estoque/caixa anteriores, trabalhos/territórios/defesas/iniciações, F2 e F11.

1867 testes aprovados e build sem erros/avisos. Nuvem não executa GTA: navegação, pontos de chão/traseira, inputs/props/animações e coexistência com mods precisam do teste real.
