# RPCore 0.58.0 — fase 6.5P: venda em lote entre gangues

## Instalação

Extraia RPCore-update-0.58.0.zip da branch rpcore-entrega. Com GTA fechado, abra PowerShell na pasta de Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

DLL pronta para GTA Legacy, SHVDN Enhanced 1.1.0.6/LemonUI 2.2. Preserva saves/configurações existentes, .oiv/.rpf/.ytd/props, celular F2 e teleport F11. Bulk.json só é instalado quando ausente. Não instale PSRP.dll junto.

## Preparar e contratar

Esteja afiliado a uma gangue cadastrada e controle uma área. Resolva disputas/iniciações pendentes e conclua/cancele roubo ou conclua/estorne fretes. Abasteça o **armazém criminal local** por depósito, frete criminal ou roubo. Estoque pessoal Dealer e inventário comum não complementam lotes.

Pare seu carro/van atual, como motorista, perto do centro da área (Warehouse.Radius +5m, padrão17m), sem procurado/combate/missão/atividade. **F4 → Comercio dos territorios → Venda em lote: área**. Escolha um produto/quantidade disponível, depois uma gangue compradora com modelo de membro válido. A escolha da compradora confirma o contrato. Não fornece veículo nem exige compra em garagem; usa o carro atual que não esteja reservado a outro trabalho.

Quantidades padrão **25, 50, 100, 200 ou 500**. Cada contrato vende **um SKU**. Só oferece lotes com estoque suficiente e valor dentro do teto. Valor = floor(Dealer.Sell × quantidade × 3 / 2), até **$75.000**. Exemplo: 25 maconhas com Sell70 = $2.625 de receita suja. O preço Dealer é do RPCore; não são os valores de rua menores da PSRP.

Ao iniciar, captura gangue, área, produto/quantidade, valor, comprador/modelo, destino/opções, atitude inicial e um sorteio0…99. Nada é cobrado/pago. O estoque fica **comprometido no próprio armazém**, ainda sem débito; até concluir/cancelar, as operações territoriais de compra/venda/transferência/processamento, novos fretes/roubos/lotes e acordos diplomáticos ficam bloqueadas. Outras atividades, banco e estoques pessoais não são globalmente bloqueados por um contrato suspenso.

## Carregar, transportar e entregar

1. Ao contratar, saia do carro. No marcador do centro da sua área, **E pega a caixa**; na traseira do veículo parado, **E carrega**. Carro deve permanecer a até30m do centro. O prop usa DeliveryCargo existente e fallback de caixa nativa; a mecânica funciona mesmo se visuais/props estiverem desligados.
2. Siga o GPS no **mesmo veículo** ao encontro. Escolhe um dos seis locais da referência, preferindo candidatos a pelo menos1200m da área; se não houver, usa a lista completa. Não move/repara/apaga/reemplaca seu carro.
3. Perto do local, prepara **um comprador próprio**, desarmado e com identidade temporária. Modelo/chão/espaço precisam estar disponíveis; falha suspende o contrato. Sem carro ou acompanhantes do comprador nesta etapa.
4. Chegue dirigindo o carro vinculado e pare a até25m do ponto. Saia e pressione **E na traseira para retirar a caixa**. Mantenha o veículo parado a até30m do encontro.
5. Leve a caixa ao comprador. A pé, a até2,5m, pressione **E** e permaneça junto dele por **3 segundos ativos**. Afastar-se, abrir menu/pausa, ragdoll ou mover o carro interrompe a contagem; recomece com E. Entrar no carro carregando suspende. Comprador morto/desaparecido/em combate suspende sem pagamento.
6. A entrega física fica salva. O NPC/prop próprios são limpos e o marcador volta ao centro do encontro. **E no ponto amarelo finaliza a venda**. Só então debita o produto do armazém e credita receita suja na área. Sem banco/cash, tesouraria, estoque pessoal, reputação/patente ou salário direto.
7. Volte à área para usar o processamento85% já existente. Uma venda de $2.625 permite processar a receita em lotes conforme configuração; não ganha mais dinheiro por confirmação repetida.

Não há item transferível no inventário comum. A caixa representa o lote contratual. Identidade do carro é verificada em execução por handle/modelo/placa, sem restaurar handles salvos. Reserva JobVehicles é liberada ao suspender/confirmar entrega física; nunca usa Retire/Delete no veículo emprestado. Só o comprador marcado pelo próprio contrato pode ser removido na limpeza.

## Relações

Na **confirmação final**, conta BulkDeals e tem **30% de chance de melhorar um nível**: Guerra → Hostilidade → Neutro → Respeito. Respeito permanece Respeito. Usa a atitude atual salva, ou a inicial capturada se o par não tiver histórico. A melhoria não é garantida: sorteios0…29 melhoram;30…99 não. O sorteio fica no contrato e não muda por reload/retomada parcial. Antes de confirmar, entregar fisicamente/cancelar não muda relação.

Consulte **F4 → Relacoes / acordos**: mostra lotes vendidos junto do histórico anterior. Não aplica grupos nativos globais nem ataques automáticos. O cooldown/valores de acordos pagos anteriores permanecem separados, sem alteração pela venda.

## Suspender, retomar e cancelar

**END** suspende conservando contrato/estoque. No comércio principal aparecem status, **GPS do lote pendente**, retomar/suspender e cancelar.

- Antes de confirmar a entrega física: volte à sua área no seu carro/van parado e seguro. Retomar refaz carga/transporte/encontro, mantendo quantidade, preço, destino, comprador, sorteio e número de tentativas. Não restaura NPC/prop/carro de handles antigos.
- Depois da entrega física salva: GPS aponta o encontro. Volte a pé a até10m e retome; **E a até3m finaliza**, sem exigir carro ou recriar comprador.
- Cancelar desbloqueia estoque que ainda não foi debitado, sem dinheiro/estorno. Funciona mesmo após perder filiação/posse, em segurança e sem outra atividade ativa. Pode cancelar entrega pronta sem receber receita/relação.
- Reload, troca de perfil/ator, perda de posse, morte, procurado/combate/missão/cutscene, perda/avaria do veículo, afastar-se dele mais de150m, falha técnica ou prazo ativo **suspendem**. Não criam perda definitiva de estoque nesta etapa. Sem imunidade policial/restauração de vida/carro.
- Prazo padrão1200s ativos antes da entrega física; pausa/menu/gaps acima2s não contam. Retomada reinicia prazo. Depois de pronta não expira. Nenhuma progressão offline.

Catálogo sem SKU, estoque editado/insuficiente, dados inválidos, falta de posse ou overflow recusam confirmação conservando entrega pronta. Alterar preço depois de contratar não reprecifica o lote. Restaurar o SKU permite tentar novamente; cancelar permanece alternativa. Nenhuma reconciliação apaga pacotes/saldos antigos.

## Arquivos e integração

Novo **GangBulkData.cs** contém opções, contrato e transações; **GangBulk.cs** contém menu, veículo, caixa, comprador e checkpoints. GangBusiness integra controle/tick/E/END/perfil. Wallet/Economy incluem livro opcional GangBulk. Guardas de exclusividade nos sistemas territoriais e acordos protegem o estoque comprometido, inclusive com livro Bulk inválido. Assinaturas públicas existentes permanecem.

**Gangs/Bulk.json**: MaxValue1…75000 (75000); SecondsLimit120…1800 (1200); HandoverSeconds2…10 (3);1…10 Quantities distintas de1…500;1…30 Sites distintos válidos. Configuração inválida usa padrões em memória, sem substituir arquivo. Contrato captura opções; alterações posteriores valem para novos contratos.

**Wallet.GangBulk**: Pending, Attempts/Completed/Cancelled, Revenue e até256 recibos. Pending Loaded/Ready guarda progresso lógico. Restart parcial limpa prova de carga; Ready só após Loaded. Conclusão grava **Bulk + Warehouse + Illicit + Diplomacy na mesma Wallet**, com rollback das quatro referências em falha; estoque sai em Stock/Sold e receita entra em Dirty/Proceeds. Mantém a equação do armazém e campos desconhecidos. Novo BulkDeals opcional0 em conta diplomática. Legado sem os campos continua válido, sem migração destrutiva.

## Referência PSRP e o que ainda falta

Varredura PSRP.GangSpawner: BeginBulkDrugDelivery, GetWeaponSaleBuyerLocation, SpawnBulkDrugBuyerCrew, GetCurrentBulkDrugValue e CompleteBulkDrugDealSequence. Referência usa quantidades25/50/100/200/500,150% do preço de rua/teto75000, seis posições/headings e distância preferida1200m, crédito Turf.Cash e sorteio30% para melhorar relação.

Diferenças explícitas: PSRP monta carga com vários SKUs, retira estoque ao iniciar, cria GBurrito2, comprador de carro+3 acompanhantes, sequência longa, integridade da carga/preço proporcional a danos e ataques de sequestro, além de MissionWin. RPCore implementa **um SKU, um comprador a pé, seu veículo atual, estoque comprometido até débito final, valor fixo e receita suja/processamento85%**. Dano/preço, emboscadas, perda permanente, van/crew do comprador, mix de SKUs e reputação da missão continuam pendentes. Não se afirma paridade completa. Fases8 (interface/mecânicas de necessidades) e9 (refinamento de todos os trabalhos) continuam reservadas.

## Testes no GTA

- Com≥25 pacotes locais, anote banco/cash/estoques/receita/relação. Contrate25 maconhas, carregue/dirija/descarregue/entregue3s/E final. Antes do E final tudo financeiro igual; depois local−25/Sold+25 e Dirty+2625 no exemplo. Banco, cash e estoque pessoal iguais. Relação pode melhorar, não necessariamente.
- Teste interrupção da entrega por menu/distância, motorista/carro errado, veículo em movimento, comprador desaparecido e procurado: sem pagamento indevido. NPC/caixa próprios devem ser limpos sem mexer em outros NPCs/veículos.
- END/reload parcial refaz rota sem mudar contrato/sorteio; depois da entrega pronta retoma a pé no encontro via GPS e finaliza sem carro/NPC. E repetido/reload depois de finalizar não desconta/paga/melhora de novo.
- Cancelar antes e depois da entrega física conserva estoque e não gera receita/relação. Morte/procurado/avaria/prazo suspendem sem remover pacotes antigos. Perda de posse bloqueia finalizar, mas permite cancelar.
- Enquanto pendente, comércio/transferência/vendas/processamento/fretes/roubos/acordos territoriais recusam sem efeitos. Após concluir/cancelar voltam a funcionar. Confira processamento85%, relações/acordos anteriores, demais trabalhos, F2 e F11.

Build/dados/instalador não executam GTA. Acesso e chão dos seis encontros, raio/traseira/veículo/prop, decorator/streaming/IA do comprador, teclas/GPS e coexistência com mods precisam ser testados no jogo.
