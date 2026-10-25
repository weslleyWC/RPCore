# RPCore0.55.0 — fase6.5M: frete para o armazém criminal

## Instalar

Baixe e extraia RPCore-update-0.55.0.zip da branch rpcore-entrega. Com GTA fechado, abra PowerShell na pasta que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

DLL pronta, GTA V Legacy, SHVDN Enhanced1.1.0.6 e LemonUI2.2. Não reinstala .oiv/.rpf e não executa PSRP.dll junto. CriminalSupply.json só é acrescentado quando ausente. Configurações/saves/contratos anteriores, estoques/receita/tesouraria, celularF2 e F11 mantidos.

## Iniciar a encomenda

1. Filie-se a uma gangue cadastrada e controle uma área. Resolva disputas/iniciações pendentes e encerre/estorne outro frete reservado.
2. Use F4 → Comercio dos territorios → GPS da área. **Leve seu carro/van**, pare a atéWarehouse.Radius+5m do ponto (17m padrão) e fique como motorista. Moto/avião, veículo ocupado por outro motorista ou reservado de emprego não são aceitos.
3. **F4 → Comercio dos territorios → Armazem criminal: área**. Escolha **Frete criminal** do produto. Exemplo padrão:10 maconhas por$360 (preço de compra40×10 com10% desconto). Não precisa ter pacotes pessoais; é compra nova debitada do banco. Outro produto usa Buy do Dealer.json.
4. O débito ocorre uma vez e fica reservado no pedido. **Nenhum pacote entra ainda**. O armazém precisa comportar o lote, incluindo peso, limite999 por SKU e contas válidas.

O contrato guarda preço/quantidade/rota/capacidade/raio/prazo. Uma reserva por perfil compartilhada com o frete legal; não pode iniciar os dois juntos. Reservas antigas sem tipo continuam legais.

## Coletar e entregar

Siga os três pontos de GPS, nesta ordem padrão:

- La Mesa:(887.2639,-991.1592,32.74689).
- Centro:(400.9214,-354.4539,46.81522).
- Oeste:(-1631.082,-124.8462,57.98261).

As posições vêm das três candidatas DrugSupplyRunLocations do primeiro território PSRP. Este frete usa todas como rota própria; acesso/chão/estacionamento precisam ser validados no GTA.

Em cada ponto, pare o mesmo veículo a até35m, desça, **E pega a caixa** junto do marcador. Leve à traseira e **E carrega**. Faça as três coletas. Caixa em mão reutiliza DeliveryCargo e suas flags visuais/ups_box/fallback de cardbox. Não cria caixas adicionais no chão/veículo.

Com carga3/3, **dirija de volta à área**, pare a até25m do centro e desça. **E na traseira retira uma caixa**, caminhe ao ponto e **E entrega**. Repita três vezes. Depois da descarga3/3, **E mais uma vez no ponto confirma o lote**. O armazém criminal recebe exatamente a quantidade contratada: **as três caixas representam o lote inteiro**, não um lote por caixa.

No exemplo, banco fica−360 e só no fim o armazém ganha10 weed. Estoque pessoal do tráfico, mercadorias legais e receita suja/tesouraria não recebem nada por essa entrega. O histórico do armazém mostra **frete/Supplied**, separado de depositado pessoal. Depois pode retirar ou usar Vender do armazem; venda/processamento85% continuam conforme0.54/0.53.

O frete legal do menu principal continua abastecendo seu estoque legal anterior e conserva sua rota/configuração/preço. O pedido mostra se é criminal e qual SKU foi reservado.

## Interrupções, retomada e estorno

**END** ou Suspender frete conserva o pedido pago. Reload, morte, combate/procurado, missão/cutscene, perda de posse/afiliação, troca de perfil/ator, carro perdido/destruído/identidade alterada, entrar em veículo carregando caixa, afastar-se150m do carro ou prazo ativo1800s também suspendem, sem pacotes parciais e sem estorno automático. Pausa/menu/modal e gaps maiores que2s não contam no prazo.

**Retomar frete reservado** aparece no armazém e no menu principal do comércio. Contrato parcial exige carro/van próprio parado no território como motorista; refaz as três cargas/descargas físicas sem recobrar. Pode usar outro carro entre sessões. Se já descarregou3/3, retome a pé perto do destino e confirme E, sem refazer o percurso.

**Cancelar frete e estornar** devolve somente o custo pago uma vez, sem pacotes. Pode cancelar mesmo após perder gangue/posse, desde que em segurança e sem outra atividade; use o menu principal do comércio se o armazém da área ficar indisponível. Pode estornar um pedido pronto antes da confirmação. Concluído o lote, não há estorno daquele pedido. Banco no limite/falha de gravação conservam a reserva até poder estornar.

Transferências do armazém, vendas/processamento territoriais e comércio legal ficam bloqueados enquanto houver reserva, evitando concorrência de capacidade. Seu carro é apenas reservado enquanto ativo e liberado ao suspender; não é apagado, reparado, movido ou reemplacado.

## Catálogo, capacidade e saves

A conclusão exige catálogo Dealer atual válido para verificar peso contra a **capacidade capturada no contrato**. Alterar preço/quantidade não recobra nem muda o lote pago. Se o peso atual deixar o lote sem espaço, o pedido continua pronto; resolva a configuração ou estorne. Um SKU removido do catálogo continua no pedido e pode ser guardado com peso conservador1kg/un, se couber; depois permanece preservado/bloqueado para gerenciamento até o mesmo ID voltar ao catálogo. Não converte SKU em mercadoria legal ou produto diferente.

**GangBusiness.Supply** ganha CargoKind0=Legal padrão/1=Criminal, ProductId/ProductWeight/WarehouseCapacity/StartRadius. Opções/área/quantidade/custo/provas anteriores continuam. **GangWarehouse.Accounts.Supplied** opcional registra lotes entregues: Stock=Deposited+Supplied−Withdrawn−Sold. Ausente em saves antigos equivale a vazio/zero. APIs de conclusão antigas são mantidas para frete legal; a conclusão criminal usa catálogo atual.

Conclusão grava limpeza do pedido e crédito do armazém na mesma Wallet, com recibos/limites e rollback das duas referências em falha. Não usa dois arquivos para tentar coordenar dinheiro/estoque. Dados inválidos, tipo desconhecido ou overflow bloqueiam sem apagar. Outros livros/metadados existentes são conservados.

**Gangs/CriminalSupply.json**: Units1–100 (10), DiscountPercent0–50 (10), Route.TimeLimitSeconds300–3600 (1800) e exatamente três Route.Suppliers distintos. Route.DiscountPercent pertence ao esquema reutilizado da rota e não é somado ao desconto externo; preço usa apenas DiscountPercent externo. Peso/capacidade/raio seguem Dealer/Warehouse; novas configurações não reprecificam encomenda paga. Configuração inválida usa defaults em memória sem substituir arquivo.

## Referência e limites

PSRP rouba carga de rivais, gera duas drogas aleatórias35–90 escaladas por territórios inimigos, usa DrugPackage e concede MissionWin ao devolver. Aqui o código é próprio: compra de SKU escolhido, desconto e três pontos, carro do jogador e reserva recuperável. Não há roubo/guardas/emboscada/penalidade de relações, saque aleatório ou reputação por esse frete. Auditoria PSRP-SYSTEMS-AUDIT-0.55.0.json registra coordenadas/evidências/diferenças.

Roubo rival/logística com guardas/convoys, armas/automação, café/boost, guerras/diálogo/polícia e fases8 (interface/mecânicas) e9 (todos os trabalhos) continuam pendentes.

## Teste no GTA

- Anote banco/estoques. Reserve10 weed por360: banco−360, estoques iguais. Faça três cargas e três descargas; antes do E final ainda sem pacotes. Confirme: armazém criminal+10/Supplied+10, pessoal/legal iguais, sem nova cobrança.
- E repetido/reabrir/reload não duplicam lote. Teste outro produto, sem dinheiro/capacidade e outro frete pendente: recusa sem débito parcial.
- END/reload no meio; retome no destino e refaça carga sem recobrar. Suspenda após3/3, reload e confirme a pé. Estorne no início/pronto: banco retorna o valor uma vez sem estoque; estorno indisponível após concluir.
- Confira o bloqueio após perda de posse e estorno pelo comércio principal. Confira preço capturado, catálogo/peso alterado, estoque antigo e produtos desconhecidos preservados.
- Retire/venda pacotes entregues e processe receita; confira o frete legal antigo, tesouraria, trabalhos/defesas/iniciações, F2 e F11.

1967 testes aprovados e build sem erros/avisos. GTA não roda nesta nuvem; rota/acesso/traseira, carga/descarga/props/teclas e coexistência com mods precisam do seu teste.
