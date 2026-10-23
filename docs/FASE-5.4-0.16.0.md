# Fase 5.4 — RPCore 0.16.0: tráfico / vendas de rua

Atualização após entrega: usuário confirmou funcionamento no GTA e definiu [fase 8 (interface e mecânicas) e fase 9 (refinamento dos trabalhos)](REFINAMENTOS-FASES-8-9.md). O texto abaixo registra o escopo e a validação da entrega original; funcionamento básico não significa refinamento concluído.

Esta entrega implementa o ciclo de comércio de rua previsto na fase 5: compra de estoque no contato, compradores próprios, negociação normal/premium, entrega temporizada, reputação por bairro, recusas e denúncia à polícia nativa. Estoque/saldo/reputação persistem por perfil no mesmo checkpoint. Caça, pesca, crafting, trabalhos e teleporte continuam incluídos. Streamer e arrancada são as próximas atividades; refinamento dos trabalhos fica depois das fases principais.

## Instalação

Feche o GTA, extraia RPCore-update-0.16.0.zip e execute o instalador **da pasta extraída**, com seu caminho real:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Contém DLL pronta. Faz backup e acrescenta Jobs/Dealer.json só se ausente. Não apague saves/configurações. Mantém celular F2, teleporte F11, inventário/bolsas/progresso anteriores. Não modifica/reinstala OIV/YTD/RPF/DLC/dlclist.

## Como testar

1. **F10 → Trabalhos / contratos → Trafico / vendas de rua → GPS: contato / estoque**. F11/J → GPS também tem esse destino. O contato é externo, no ponto da Stash House da referência: **(-1309.499, -770.0988, 19.86772)**. Não é preciso entrar em interior. E próximo abre o menu. Pode usar o teleporte do F11 para agilizar.
2. A pé/desarmado, fora de turno, sem procurado/missão/combate, compre **cinco pacotes de maconha por 200 do banco**. As outras quatro categorias também estão disponíveis no menu. Unidade é um pacote abstrato da atividade no jogo; não há produção/manipulação de substâncias nesta entrega.
3. Escolha **Iniciar vendas de rua**. Requer pelo menos três pacotes conhecidos e a trava livre de outros trabalhos/caça/pesca/crafting. Pode iniciar em uma calçada pública de outro bairro depois de comprar estoque; não precisa permanecer no contato.
4. Fique a pé/desarmado e parado numa calçada. O RPCore procura uma posição livre de pedestre a 8–40 m, cria **um comprador próprio** e mostra o blip laranja. Ele caminha até você. Se não houver calçada livre, reposicione; tenta por até um minuto sem consumir estoque. Não toma controle de NPCs de outros mods.
5. Quando o comprador estiver a menos de três metros, pressione **E**. Escolha **preço normal** ou **pedir mais**. Cada proposta compra **um pacote**; usa um produto disponível no seu estoque. Pedir mais aumenta o preço em 20%, com maior risco de recusa e denúncia. Fechar/reabrir o menu não sorteia outro resultado para esse comprador.
6. Se aceitar, fique junto, a pé/desarmado, por **três segundos**. O pacote só é retirado quando a venda é salva; saldo e reputação são creditados no mesmo checkpoint. Abrir menu/telefone/inventário ou afastar-se durante essa entrega interrompe a tentativa sem retirar o pacote. Vendas anteriores continuam salvas.
7. Complete **três vendas** para terminar o turno. Há espera de cinco segundos entre compradores. Não tem bônus de dinheiro ao terminar: cada venda já foi paga **no banco**. Estoque restante fica para o próximo turno. End segurado por um segundo ou Encerrar vendas cancela mantendo estoque e pagamentos confirmados.

## Preços e negociação

Preços são próprios do RPCore, em moeda do jogo, por pacote. Compra em lotes de cinco no contato; estoque de 10 kg padrão, 0,1 kg por unidade.

| Categoria | Compra por pacote | Compra de cinco | Venda normal inicial |
| --- | --- | --- | --- |
| Maconha | 40 | 200 | 70 |
| Cocaína | 100 | 500 | 160 |
| MDMA | 60 | 300 | 100 |
| Lean | 80 | 400 | 130 |
| Crack | 75 | 375 | 120 |

Reputação por zona nativa do GTA (DAVIS, DELPE etc.), de 0 a 100: venda +5, recusa -1 e denúncia -3. O preço normal ganha até 10% com reputação alta, arredondado para inteiro; premium aplica mais 20% sobre esse preço. Exemplos no início: maconha 70 normal/84 premium. Três vendas normais iniciais desse produto rendem 210, consomem três dos cinco pacotes e deixam dois no estoque. O preço mostrado na proposta é validado ao confirmar o pagamento.

## Risco, desistência e polícia

No padrão, risco de denúncia é **8%** por proposta normal e **12%** por proposta premium. Denúncia tem prioridade sobre recusa. Sem denúncia, recusa começa em **10%** normal/35% premium e diminui com reputação. Os sorteios são fixados ao criar a proposta, sem repetição a cada frame/clique.

- **Recusa:** não retira pacote nem paga; registra a recusa e reduz um ponto de reputação. Libera outro comprador.
- **Denúncia:** não retira pacote nem paga; registra o evento e reduz três de reputação. Aciona uma estrela via API do SHVDN Enhanced, sem reduzir estrelas existentes, e encerra o turno. A perseguição é a polícia nativa do GTA. Não limpa procurado nem oferece imunidade. Espere resolver o procurado antes de iniciar novamente.
- **Dispensar comprador / prazo de 120 s:** não retira pacote nem paga. Oferta/entrega expira sem reiniciar o prazo ao abrir menu ou negociar. Outro comprador pode surgir. Turno limitado a 12 propostas, mesmo sem completar três vendas; somente completar as vendas conta como turno concluído.
- **Comprador morto/desaparecido, distância maior que 80 m, morte/natação/veículo/ragdoll/combate/missão/procurado/troca de perfil:** encerra sem consumir estoque da tentativa pendente. Ficar armado impede comprar/iniciar/negociar; durante entrega também interrompe a tentativa.

Para um teste determinístico do ciclo sem recusas/denúncias, é possível ajustar **PolicePercent, RefusePercent e PremiumRefusePercent para 0** em scripts/RPCore/Jobs/Dealer.json, com o jogo fechado, e depois restaurar 8/10/35. É uma configuração opcional do gameplay, não uma mudança na polícia global. Não precisa fazer isso para o funcionamento normal.

## Persistência e compatibilidade

**Wallet.json/Dealer** é um campo aditivo: Stock, ZoneRep, Sales, Units, Sessions, Refused, Incidents, Earned e até 512 recibos recentes. Compra e venda usam cópia do estado e salvamento único com Bank. Falha de gravação restaura banco e referência anterior; a entrega tenta novamente após cinco segundos enquanto continuar próxima/elegível e dentro do prazo. Recibo já confirmado não paga/retira estoque de novo, inclusive após mudança do preço pela reputação. Não há débito antes de salvar nem apreensão automática do estoque.

Estoque da atividade é separado de Inventory.json e das bolsas de caça/pesca/crafting. Esses arquivos/campos não são convertidos nem consumidos. Saldo em dinheiro na mão não é modificado; a referência paga em cash, enquanto o RPCore usa banco para manter estoque/pagamento no mesmo checkpoint. Licenças de piloto/caça, equipamento de pesca e progressos anteriores são preservados. Campos/produtos desconhecidos ficam no save; dados semanticamente inválidos não são zerados. Turno/NPC/proposta ativa não retoma após reload, mas estoque/ganhos/reputação persistem.

**Jobs/Dealer.json** é criado/instalado só se ausente e embutido como fallback. Permite personalizar contato, produtos/preços/pesos, capacidade, número de vendas, prazos e probabilidades dentro de limites validados. Configuração inválida permanece no disco e usa o padrão; válida mantém personalizações. A trava é compartilhada entre **doze atividades**. Ler o menu não inicia atividade.

Banner **DrugDealingBanner**, presente nos YTD fornecidos. Animação nativa de entrega mp_common/givetake1_a; só para o clipe iniciado pelo RPCore no ped original. NPCs criados são próprios, sem invencibilidade; ao liberar, entram na limpeza de JobActors. Não altera relações/IA global, não cria rivais armados nem sequestra NPCs de ambiente. Contato é um ponto de menu, sem feirante/interior novo. Não exige PSRP.dll em paralelo.

## Referência e limites desta entrega

Análise estática de PSRP/_JOB__Drug_Dealing.cs: cinco categorias, pedidos/compradores, reputação por zona, comércio, Stash House externa e risco de uma estrela. Ponto externo e banner foram conferidos na referência; ciclo, preços, negociações, transações e NPCs são código próprio do RPCore.

Esta subentrega fecha **compra → comprador → negociação → entrega → pagamento/reputação**. Não inclui os outros sistemas encontrados nessa classe: interiores/hotéis, cozinha/receitas, gangues/rivais, emboscadas, roubos e venda de placas. Seguem como diferenças na revisão de paridade/etapas correspondentes; não foram apresentados como já implementados.

## Arquivos alterados

- DealerData.cs/DealerJob.cs/config/Jobs/Dealer.json: configuração, estoque, compras, propostas/sorteios fixos, negociação, comprador de calçada, prazo, entrega, polícia nativa, reputação, checkpoint e cancelamento.
- Economy.cs/Wallet.cs: campo Dealer aditivo e adaptadores; preservam campos/arquivos anteriores.
- Main.cs/SettingsSystem.cs: décima segunda atividade na central e GPS do contato no F11/J.
- AchievementSystem.cs: três conquistas ligadas a vendas/turnos reais; cash_earned após crédito confirmado.
- Visuals.cs/config/Visuals/Bindings.json: DrugDealingBanner e fallback, mesclados aos bindings locais em memória.
- RPCore.csproj/tests/RPCore.Tests.csproj: padrão embutido e lógica real vinculada aos testes; tests/DealerTests.cs: 49 casos novos.
- AssemblyInfo.cs/Main.cs/CoreMenu.cs/VisualAssetDefaults.cs: versão 0.16.0; scripts/audit-psrp-systems.py: comparação de contato/categorias/reputação/polícia; documentação, relatórios e pacotes.

## Validação e roteiro no GTA

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI SHVDN3 2.2: zero erros/avisos. **567 testes aprovados** (518 anteriores +49 novos): cinco compras, débito/estoque, replay/rollback, preço validado/premium/reputação, recusa/denúncia sem retirar estoque, prazos/sorteios fixos, entrega elegível/tempo, três vendas/um turno, limites, unknowns/sistemas anteriores/licença de piloto e exclusão entre doze atividades. Não executam GTA/IA/polícia.

1. Confira F2, teleporte e saves anteriores. No contato, compre cinco pacotes por 200; confira banco e estoque após reload.
2. Inicie numa calçada pública: confira NPC/blip/caminhada, E e preços. Complete três vendas normais e confira quantidades, crédito bancário e reputação.
3. Tente premium e observe aceitação/recusa/denúncia. Recusa e denúncia não retiram estoque; denúncia deve dar uma estrela e encerrar. Não é falha do mod. Use a configuração opcional de riscos zero se quiser separar o teste do ciclo do teste policial.
4. Teste timeout, dispensar, afastar-se/abrir menu durante entrega, End, morte e troca de perfil. Estoque e pagamentos confirmados devem permanecer; NPCs/blips próprios devem ser limpos.
5. Recarregue após uma venda e confira estoque/ganhos/reputação sem novo pagamento. Faça regressão rápida dos empregos, caça, pesca, crafting e teleporte.

Navmesh/calçadas, aproximação dos NPCs, contato/menu/banner, animação e reação policial dependem do seu teste real no GTA. O jogo não foi executado nesta nuvem.
