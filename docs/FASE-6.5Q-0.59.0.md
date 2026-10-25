# RPCore 0.59.0 — fase 6.5Q: arsenal comercial e entrega de armas

## Instalação

Baixe e extraia RPCore-update-0.59.0.zip da branch rpcore-entrega. DLL pronta; com GTA fechado, abra PowerShell na pasta de Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

GTA V Legacy, SHVDN Enhanced 1.1.0.6/LemonUI 2.2. Preserva saves/configurações existentes, .oiv/.rpf/.ytd/props, celular F2 e teleport F11. Gangs/Arms.json só é acrescentado quando ausente. Não use PSRP.dll junto.

## Arsenal comercial separado

Afiliado a uma gangue cadastrada e controlando uma área, abra **F4 → Comercio dos territorios → Arsenal / armas: área**. Resolva disputas/iniciações e conclua/cancele roubo/lote ou conclua/estorne frete pendente antes de comprar/contratar entrega.

O arsenal é estoque comercial por **gangue, área e perfil**. Não é inventário de armas do personagem: não equipa pistola, não concede munição utilizável e não retira suas armas pessoais. Também não usa pacotes de drogas ou mercadorias legais.

Cinco categorias padrão, com preço por unidade:

| Categoria | Compra no banco | Venda física / receita suja |
|---|---:|---:|
| Pistolas | $900 | $1.200 |
| SMGs | $1.800 | $2.500 |
| Fuzis | $3.300 | $4.500 |
| Escopetas | $2.400 | $3.200 |
| Munição | $2 | $3 |

**A compra paga é uma mecânica própria do RPCore.** A PSRP abastece armas roubando carregamento rival; essa missão de roubo de van ainda não foi implementada. Os cinco preços de venda acima vêm de GetWeaponSalePrice da referência.

## Comprar

Vá a pé ao centro da sua área, a até Warehouse.Radius (12m padrão), em segurança, sem procurado/combate/ragdoll/missão/cutscene/atividade/contrato pendente. Selecionar **Comprar quantidade / categoria / preço** confirma a compra.

Quantidade padrão1/5/10/25/50; limite999 por SKU. Debita somente o banco e acrescenta estoque/Purchased/Spent do arsenal na mesma gravação. Sem mudança em cash, tesouraria, Dealer/armazém criminal, reputação ou armas nativas do personagem. Saldo insuficiente/lotação/posse/dados/limites recusam sem débito. O menu mostra estoque, comprados/vendidos e histórico financeiro.

## Contratar e realizar a entrega

1. Depois de comprar, entre no seu carro/van atual como motorista e pare na mesma área (Warehouse.Radius +5m,17m padrão). Não fornece veículo nem exige compra em garagem; carro não pode estar reservado a outro trabalho.
2. Abra o arsenal novamente e selecione **Entregar quantidade / categoria / valor**. Só mostra lotes com estoque/valor/limite válidos. Cada contrato contém um SKU; não mistura armas e munição.
3. Saia do carro. **E no ponto da área pega a caixa; E na traseira carrega**. Carro parado a até30m do centro. Prop usa DeliveryCargo existente e fallback; mecânica continua com props/visuais desligados.
4. Siga o GPS no mesmo veículo. Usa os seis encontros PSRP, preferindo locais≥1200m da área. Ao chegar, um comprador civil próprio é preparado com modelo/chão/espaço e tag de propriedade. Pare dirigindo a até25m do ponto.
5. Saia e **E na traseira retira a caixa**. Leve ao comprador a pé; **E a até2,5m** inicia entrega de3s ativos. Mantenha o carro parado a até30m. Menu/pausa/afastamento/ragdoll/movimento interrompem contagem; recomece E. Entrar no carro carregando suspende.
6. Depois dos3s, a entrega física fica salva e comprador/caixa próprios são limpos. **E no marcador amarelo do encontro finaliza a venda**. Só então desconta arsenal/Sold e credita a receita suja da área.
7. Volte ao território e use o processamento85% existente. Não paga direto no banco/cash/tesouraria e não concede reputação ou melhoria de relação.

**Exemplo:** compre5 pistolas por$4.500. Entregue o lote: arsenal−5 e receita suja+6.000. Não devolve os$4.500 ao vender/cancelar; compra e venda são operações separadas. Processar toda essa receita gera$5.100 no caixa, conforme a rotina existente e seus lotes configurados.

## Quantidade, preço e slot de contrato

Preço da entrega é **Sell × quantidade**, sem multiplicador150% das drogas. Teto compartilhado de$75.000. Limite capturado de **10 vagas por área controlada**, até150: cada arma usa1; munição (`ammo`) usa ceil(unidades/100). Com uma área, até10 armas por entrega; cinquenta munições usam uma vaga. Quantidades padrão acima e estoque ainda limitam o menu. Capacidade é capturada no contrato; perder outra área durante viagem não recalcula esse limite, mas precisa continuar dono da área de origem para concluir.

Armas e drogas compartilham **um contrato de entrega**: `GangBulk.Pending.Kind`0=Drogas (legado),1=Armas; tipo/limite/preço/produto/quantidade/destino/modelo/opções capturados. Sem débito de arsenal ao iniciar: estoque fica comprometido até E final. Pendente bloqueia compras de armas e operações territoriais de compra/venda/transferência/processamento, novos fretes/roubos/lotes/acordos, como na fase anterior. Não bloqueia globalmente todo trabalho/banco/estoque pessoal quando suspenso.

A venda de armas **não usa sorteio30%**, não cria conta diplomática nem aumenta BulkDeals. Esse bônus permanece exclusivamente nas vendas de drogas. O contador geral de entregas/receita em GangBulk inclui os dois tipos; o arsenal tem seus próprios Sold/Revenue.

## Suspender, retomar e cancelar

**END** suspende. Pelo comércio principal: status, GPS do lote pendente, retomar/suspender/cancelar.

- Parcial: volte à área no carro parado e seguro; retomar refaz carga/viagem/encontro, sem nova compra ou mudança de tipo/limite/preço/destino/quantidade. Reinicia prazo ativo.
- Entrega física pronta: GPS leva ao encontro. Retome a pé a até10m; E a até3m finaliza sem carro/comprador e sem depender do Dealer.json.
- Cancelar apenas desbloqueia arsenal ainda não consumido. Não gera receita nem estorna compras anteriores. Disponível em segurança mesmo após perder filiação/posse.
- Reload/troca de perfil/ator, perda de posse, morte, procurado/combate/missão/cutscene, comprador indisponível, carro perdido/avariado/abandonado>150m ou timeout suspendem, conservando estoque/contrato. Polícia nativa continua ativa; não há imunidade/reparo/reset de vida.
- Prazo padrão1200s ativos antes da entrega pronta; pausa/menu/gaps>2s não contam. Pronta não expira. Não há renda offline ou perda definitiva de estoque nesta base.

Catálogo sem SKU, estoque editado/insuficiente, dados inválidos ou overflow recusam o E final, conservando pronta. Restaure o mesmo ID ou cancele. Alterações de preço/limite do catálogo não reprecificam o contrato. Nenhum estoque antigo é convertido/apagado.

## Dados e arquivos

Novo **GangArmsData.cs**: catálogo, conta e transações puras. Novo **Wallet.GangArms** opcional: Accounts por gangue/área com Stock/Purchased/Sold/Spent/Revenue e até256 recibos. **Stock=Purchased−Sold**, max999/SKU,40 chaves por dicionário e640 contas. Compra grava Bank+Arms juntos; conclusão grava **Bulk+Arms+Illicit** juntos. Save falho restaura referências/saldo; replay não repete débito/receita. Campos futuros e dados legados preservados.

**GangBulk.cs** compartilha menu/veículo/caixa/NPC/provas/timing/limpeza. **GangBulkData.cs** acrescenta Kind e ArmsLimit, com validação explícita para cada tipo; rotinas de drogas rejeitam carga de armas e vice-versa. Wallet/GangBusiness integram acesso/transações. Assinaturas públicas anteriores preservadas. Nenhuma mudança em mecânicas de armas nativas do jogador ou arquivos visuais.

Novo **Gangs/Arms.json**: MaxStock1…999, catálogo1…20 IDs distintos (modelo de nome até64 caracteres), Buy≥1/Sell≥Buy até100000; preços de compra próprios. Delivery contém Quantities/Sites/MaxValue/SecondsLimit/HandoverSeconds nos limites de GangBulkOptions. Padrões acima, seis destinos e1200s/3s/$75000. Arrays substituem padrões, sem duplicar. Configuração inválida usa padrões em memória sem substituir arquivo. Estoques com SKU removido permanecem até restaurar catálogo.

## Comparação PSRP e pendências

PSRP.GangSpawner: TurfWeaponType, GetWeaponSalePrice/Value, GetWeaponSaleMaxAmount/AmountUsed, StartWeaponSale/CompleteWeaponSaleMission, SpawnWeaponSupplyCrates/GenerateWeaponSupplyLoot. Confirma cinco preços, quantidades1/5/10/25/50,10 vagas por território/munição por100, seis encontros e créditoTurf.Cash. Auditoria PSRP-SYSTEMS-AUDIT-0.59.0.json registra hashes/evidências/diferenças.

Diferenças: abastecimento pago próprio; roubo de van/guardas e armas recuperadas ainda pendente. RPCore usa um SKU, um comprador a pé, seu veículo atual e estoque comprometido até confirmação; não criaGBurrito2/crew/carro comprador, troca de veículo, emboscadas/dano no preço/perda definitiva, vendas automáticas ou prêmioMissionWin. Receita vai ao dinheiro sujo/processamento85%, com teto compartilhado75000 próprio. Não se afirma paridade completa. Fases8 (interface/mecânicas de necessidades) e9 (refinamento de todos os trabalhos) continuam futuras.

## Teste no GTA

- Anote banco/cash, armas/munição do personagem, drogas e demais estoques. Compre5 pistolas: banco−4500/arsenal+5 e tudo mais igual. Longe/veículo/procurado/sem saldo/contrato pendente deve recusar compra sem efeitos.
- Contrate a entrega no carro parado. Faça caixa/traseira/viagem/comprador3s/E final: só no final arsenal−5/Sold+5/Dirty+6000. Armas pessoais/drogas/diplomacia iguais. Processamento85% deve continuar funcionando.
- Teste munição, limite de10 armas com uma área e preço sem150%. Teste uma venda de drogas depois: melhoria30% e demais regras anteriores mantidas.
- END/reload parcial mantém snapshot e refaz carga; pronta retoma a pé via GPS sem carro. E repetido/reload após concluir não desconta/paga de novo. Cancelar antes/depois de pronta conserva arsenal, sem estorno da compra.
- Confira interrupção3s, polícia, comprador perdido, perfil/posse, dados/catálogo/limites e bloqueios mútuos. NPC/caixa próprios devem ser limpos sem mexer no carro ou outros atores. Confira antigos contratos Kind ausente, saves, F2/F11.

Build/dados/instalador não executam GTA. Teclas/GPS, locais/chão, traseira/carro/caixa/NPC/tag/IA e convivência com mods precisam da sua validação no jogo.
