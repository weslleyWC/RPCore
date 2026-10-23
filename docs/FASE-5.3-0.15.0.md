# Fase 5.3 — RPCore 0.15.0: crafting e feira

Pesca 0.14.0 foi confirmada funcionando pelo usuário. Esta entrega liga as bolsas de caça/pesca a nove receitas de fabricação e à venda de sete produtos. Inclui quantidade/tempo/estoque, receitas em cadeia e salvamento único dos ingredientes/produto. Tráfico, streamer e arrancada continuam seguintes na fase 5. Refinamentos dos trabalhos aprovados permanecem para depois das fases principais.

## Instalação

Feche o GTA, extraia RPCore-update-0.15.0.zip e execute o instalador **da pasta extraída**, com seu caminho real:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Contém DLL pronta. O instalador faz backup e acrescenta Jobs/Crafting.json só se ausente; não apague saves/configurações. Preserva F2, caça, pesca, licença de piloto, saldo, inventário e empregos anteriores. Não reinstala OIV/YTD/RPF/DLC. A mensagem final do instalador foi corrigida para mostrar o padrão atual F2/celular e garagens/configuração, sem alterar arquivos de teclas existentes.

## Teste rápido e funcionamento

1. Traga **matérias-primas sem vendê-las**: duas sardinhas, duas cavalas ou um robalo na Bolsa de pesca; ou duas carnes de cervo/javali ou peles na Bolsa de caça. Os materiais que já foram vendidos não ficam disponíveis para fabricar.
2. Abra **F10 → Trabalhos / contratos → Bancada / crafting e feira → GPS: bancada / feira do pier**. F11/J → GPS também tem esse destino. O ponto é (-1694.761, -1072.5194, 12.012508), junto de um ponto de comida da referência no pier Del Perro. F11 → Teleportar para o waypoint pode agilizar a chegada.
3. A pé, sem arma na mão/procurado/missão/combate e perto do ponto, **E** abre a bancada. Vá em **Receitas / fabricar**. Cada receita mostra quantidades disponíveis/necessárias e a origem: caça, pesca ou estoque fabricado.
4. Escolha uma receita e **Fabricar 1 lote**. Não cobra dinheiro: só usa os ingredientes indicados. Há também cinco lotes, que exigem cinco vezes os materiais e o tempo. Fique dentro de quatro metros da bancada, sem abrir menus, até terminar.
5. Ao confirmar o salvamento, os ingredientes são retirados da bolsa correspondente e o resultado entra no **Estoque** da bancada. Esse é um único checkpoint em Wallet.json: falha ao salvar não perde matéria-prima nem entrega produto duplicado.
6. Reabra a bancada e selecione **Estoque → Vender produtos conhecidos**. Na feira, retira produtos e credita o banco no mesmo salvamento. Uma sardinha defumada rende 110; um robalo defumado rende 200. Venda sem estoque não paga.
7. Para testar a cadeia, fabrique uma carne seca de cervo, uma sardinha defumada e um couro; depois use **Montar kit de campo**. Esses três produtos viram um kit de campo vendido por 400.

## Receitas padrão

Cada linha é um lote, gerando uma unidade. Fabricação não tem taxa bancária; o custo é a matéria-prima.

| Receita | Ingredientes | Origem | Tempo | Venda do produto |
| --- | --- | --- | --- | --- |
| Carne seca de cervo | 2 carnes de cervo | Caça | 6 s | 125 |
| Carne seca de javali | 2 carnes de javali | Caça | 6 s | 110 |
| Sardinha defumada | 2 sardinhas | Pesca | 6 s | 110 |
| Cavala defumada | 2 cavalas | Pesca | 6 s | 220 |
| Robalo defumado | 1 robalo | Pesca | 6 s | 200 |
| Couro tratado | 3 peles danificadas | Caça | 8 s | 100 |
| Couro tratado | 2 peles boas | Caça | 8 s | 100 |
| Couro tratado | 1 pele perfeita | Caça | 8 s | 100 |
| Kit de campo | 1 carne seca de cervo + 1 sardinha defumada + 1 couro | Estoque fabricado | 10 s | 400 |

Estoque padrão: 25 kg. Produtos/pesos/preços: carne seca de cervo/javali 0,5 kg, sardinha defumada 0,3 kg, cavala defumada 0,6 kg, robalo defumado 1,2 kg, couro 0,5 kg e kit 1,2 kg. Receita em cadeia desconta os produtos usados antes de avaliar o peso final. Se faltar ingrediente ou espaço, o lote inteiro é rejeitado, sem remoção parcial.

## Salvamentos e interrupções

**Wallet.json/Crafting** é um campo aditivo por perfil: Stock, Made, Batches, Sales, Earned e até 512 recibos recentes. A fabricação pode retirar de Hunting.Bag, Fishing.Bag e Crafting.Stock no mesmo arquivo/salvamento; restaura todas as referências anteriores em exceção. Confere os materiais novamente ao terminar, sem confiar só no valor exibido no início. Licenças/iscas/progresso de caça/pesca não são consumidos pela receita. Campos/materiais desconhecidos são conservados; dados semanticamente inválidos não são zerados silenciosamente.

Os produtos são **mercadorias de crafting**, acessíveis pelo estoque da bancada. Não são alimentos consumíveis no inventário pessoal I nesta entrega. Inventory.json/baús/porta-malas continuam no formato atual, sem importação/reescrita ou transferência entre dois arquivos. Catálogo completo do legado, receitas de drogas, penhor/sucata e comércio de todos os itens continuam para suas respectivas etapas/revisão de paridade; esta entrega fecha o ciclo de processamento/comércio da caça e pesca.

Ingredientes só são retirados após o tempo e o salvamento. **End segurado por um segundo**, afastar-se da bancada, abrir menu/inventário/celular, pausar, selecionar arma, entrar em veículo, nadar, morrer, combate/procurado/missão ou trocar perfil cancelam a fabricação antes da confirmação sem gastar materiais. Em falha de gravação, tenta novamente após cinco segundos enquanto você continuar elegível no local; não repete o débito ao reprocessar um recibo já salvo. Fabricação ativa não retoma após reload, mas estoque/progresso permanecem.

A trava de atividade é compartilhada entre onze sistemas. Não inicia fabricação enquanto outro trabalho/caça/pesca estiver ativo. Ler o menu/receitas não começa uma atividade. Venda de matérias-primas continua nos locais anteriores; a feira vende apenas produtos fabricados conhecidos. Ganhos são bancários, sem bônus adicional no fim da fabricação.

## Recursos e referência

Análise estática de PSRP/NewInventorySystem.cs: catálogo de CraftingRecipe, seleção por ingredientes, GetMaxCraftAmount e comércio especializado. As nove receitas, preços, estoque e processamento desta entrega são próprios do RPCore. A posição foi tirada da lista burgerStands da referência; a função de bancada/feira nesse local é própria do RPCore. Não se afirma que esse era o ponto de crafting da PSRP.

Usa **ShopBanner** dos YTD instalados e uma animação nativa de trabalho, mini@repair/fixing_a_ped. Animação tem fallback: se não carregar, o tempo e o checkpoint continuam. Para apenas o clipe iniciado pelo RPCore no ped original, sem ClearAllTasks, congelar jogador, criar objetos globais ou modificar polícia/NPCs. Não depende de executar PSRP.dll junto. O ponto atende por menu, sem colocar uma bancada/feirante novo no mundo.

**Jobs/Crafting.json** é instalado/criado somente se ausente e embutido como fallback. Personaliza ponto, capacidade, catálogo de produtos, receitas, quantidade, tempo e ingredientes por origem. Configuração válida é preservada; inválida permanece no disco e usa o padrão embutido. Máximo cinco lotes por operação. IDs duplicados/quantidades inválidas não passam pela validação.

## Arquivos alterados

- CraftingData.cs, CraftingSystem.cs e config/Jobs/Crafting.json: receitas, prévia/estoque, consumo entre bolsas, timer/cancelamento, venda e rollback.
- Economy.cs/Wallet.cs: campo Crafting aditivo e adaptadores do checkpoint; não substituem os salvamentos anteriores.
- Main.cs/SettingsSystem.cs: central de trabalhos com décima primeira atividade e GPS no F11/J.
- AchievementSystem.cs: conquistas de produto/lotes/venda; cash_earned cresce após venda confirmada.
- Visuals.cs/config/Visuals/Bindings.json: ShopBanner para crafting, com defaults mesclados em memória.
- RPCore.csproj/tests/RPCore.Tests.csproj: padrão embutido e lógica real vinculada aos testes; tests/CraftingTests.cs: 41 casos novos.
- AssemblyInfo.cs/Main.cs/CoreMenu.cs/VisualAssetDefaults.cs: versão 0.15.0; scripts/Install-RPCore.ps1: texto final das teclas corrigido.
- scripts/audit-psrp-systems.py: comparação de receitas/ponto da referência; documentação, relatórios e ZIPs atualizados.

## Validação e teste no GTA

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI SHVDN3 2.2: zero erros/avisos. **518 testes aprovados** (477 anteriores e 41 novos): todas as receitas básicas, quantidade/tempo, cadeia/estoque cheio, checkpoint de múltiplas bolsas, rollback/recibos/reload, venda/banco/limites, campos desconhecidos, licença de piloto e exclusão entre onze atividades. Não executam nativos do GTA.

1. Instale sobre a 0.14.0 e confira F2, saldo, bolsas de caça/pesca, licença de piloto e inventário anteriores.
2. Faça uma pescaria sem vender o resultado, encerre e leve o pescado à bancada. Verifique quantidades mostradas, fabrique um lote e confira retirada exata/resultado único.
3. Venda e confira quantidade/preço/crédito bancário. Tente vender novamente sem estoque: não deve pagar. Recarregue e confira persistência.
4. Traga carne/peles, teste receitas de caça e monte um kit com os três produtos indicados. Confira que matérias-primas e produtos usados não ficam duplicados.
5. Cancele antes de terminar, saia da área e abra um menu durante a fabricação: deve conservar os materiais. Teste cinco lotes, falta de ingredientes e estoque cheio.
6. Faça regressão rápida de pesca, caça, trabalhos e teleporte. Ponto público/borda do local, animação, menus e convivência com recursos instalados precisam de validação no seu GTA.

O jogo não foi executado nesta nuvem. A próxima atividade principal é tráfico, seguida de streamer e arrancada; ajustes finos dos trabalhos continuam após as fases principais.
