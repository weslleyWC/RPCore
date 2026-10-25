# RPCore0.56.0 — fase6.5N: roubo de suprimentos rivais

## Instalação

Baixe e extraia RPCore-update-0.56.0.zip da branch rpcore-entrega. Com GTA fechado, abra PowerShell na pasta que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

DLL pronta. Mantém GTA V Legacy, SHVDN Enhanced1.1.0.6/LemonUI2.2, .oiv/.rpf instalados, celularF2 e F11. Não use PSRP.dll junto. Raids.json só é acrescentado quando ausente. Configurações/saves/contratos e estoques anteriores não são substituídos.

## Contratar o roubo

Esteja afiliado a uma gangue cadastrada, controle uma área e tenha outro grupo cadastrado com modelos de membros válidos para ser rival. Resolva disputas/iniciações e conclua/estorne qualquer frete reservado.

Vá a pé, em segurança e sem procurado, a atéWarehouse.Radius do centro (12m padrão). **F4 → Comercio dos territorios → Roubar suprimentos: área**. Não cobra banco/cash e não exige pacotes pessoais. Precisa de espaço no armazém para **dois produtos do catálogo**, com limite999/SKU e capacidade contratada, padrão30kg.

O contrato captura gangue/rival/área, um local entre os três candidatos, modelos, dois SKUs/quantidades e opções/capacidade. Padrão **35–90 pacotes de cada produto**, sem multiplicador por quantidade de territórios rivais. Não concede estoque ao iniciar; o lote não muda ao recarregar ou refazer o encontro parcial.

O rival prefere o dono padrão da área quando diferente da sua gangue; caso contrário escolhe outro grupo cadastrado com modelos válidos. Não exige que o jogador visite a HQ rival nem transfere posse de território.

## Combate e revista

1. Siga o GPS amarelo ao local escolhido. Pode dirigir seu veículo; o mod não fornece, apaga ou modifica carros para essa missão.
2. Ao chegar a cerca de70m, o script prepara os modelos e procura chão/espaço para **um traficante e dois guardas próprios**. Primeiro spawn aguarda os menus fecharem. Falha de modelo/espaço/tag de entidade suspende o contrato, sem recompensa; F4 permite retomar/cancelar.
3. Os três rivais atacam o jogador. Têm pistolas/ammo45, vida200, precisão30, armor25 no traficante e35 nos guardas. Não tomam seu veículo. Nenhum grupo/relacionamento global é alterado; cada NPC próprio recebe identidade temporária para reconhecimento/limpeza.
4. **Elimine os três**. Cada abate precisa de flag nativa de dano do jogador antes da morte; não é uma perícia completa de autoria de tiro. Se outro ator matar sem essa prova, a missão suspende para refazer, sem conceder estoque. Provas distintas aparecem0/3…3/3 e são salvas.
5. Após os três, siga o marcador até o corpo do traficante. A pé, a até2,5m, pressione **E para revistar** e permaneça ali por2s. Usa animação upper-body própria se disponível. Afastar-se, entrar em carro, ragdoll ou abrir menus interrompe a revista; recomece com E. Se corpo/ator desaparece, retome a missão parcial para refazer o encontro.
6. Ao concluir a revista, a carga fica salva e o GPS marca sua área. Os NPCs próprios são limpos. A carga é registro de missão no contrato, **não item/prop transferível no inventário comum**. Não retira pacotes pessoais e não pode gerar dinheiro diretamente.

Os pontos padrão são os candidatos de supply do primeiro território PSRP:(887.2639,-991.1592,32.74689),(400.9214,-354.4539,46.81522),(-1631.082,-124.8462,57.98261). Usa um deles por contrato. Chão/acesso/corpo/IA nesses locais precisam ser testados no GTA.

## Retornar e entregar

Depois de recuperar a carga, despiste a polícia nativa. Wanted não encerra automaticamente o roubo nem é removido pelo script; a revista pode ocorrer com procurado, mas **a entrega exige wanted0**, sem combate, a pé e até3m do ponto da sua área.

Pressione **E para entregar**. Só então os dois produtos entram em Warehouse.Stock e no histórico **roubo/Raided**, o contador Completed aumenta e o contrato termina. Banco/cash, estoque Dealer pessoal, mercadorias legais, receita suja, tesouraria e reputação da gangue não recebem prêmio nessa etapa.

Depois use Armazem criminal → Vender do armazem e o processamento85%/tesouraria existentes. Não há crédito dobrado ao repetir E/reabrir/reload. Se catálogo/estoque/limite impedir a entrega, a carga continua salva: confira configuração/espaço ou cancele. SKU removido do catálogo bloqueia essa entrega até restaurá-lo; não é convertido em produto diferente.

## Suspender, falhar e cancelar

**END** ou Suspender roubo no F4 conserva o contrato. Para retomar um encontro **sem carga recuperada**, volte a pé e em segurança à sua área: os três rivais são recriados, as provas físicas são refeitas e a mesma carga/rival/local permanecem. Não soma nova tentativa ou rerrola o lote.

Se a revista já confirmou a carga, retome a pé em qualquer local seguro, sem procurado/combate e com posse/filiação válidas. O GPS aponta o retorno; não refaz o combate. Reload/cutscene/troca de perfil ou ator/perda de posse/falha técnica suspendem conservando o que foi confirmado. Não restaura NPC/corpo a partir de handles salvos.

**Morte**, abandonar o encontro já iniciado a mais deRadius do site (100m padrão) **antes da coleta**, ou acabar o prazo ativo (900s padrão) **falham**: contrato/carga não entregue são descartados e Failed aumenta. Estoque antigo permanece. Antes de aparecerem os rivais não há limite de abandono do site, mas o prazo já corre; depois da carga recuperada não há limite de distância do encontro, apenas retorno/prazo.

**Cancelar roubo pendente** remove somente a carga de missão não entregue, sem taxa ou estorno porque não houve pagamento. Pode cancelar depois de perder gangue/posse pelo menu principal do comércio. Carga perdida/cancelada nunca entra no armazém; pacotes anteriores não são confiscados.

Pausa/menu/modal e gaps maiores que2s não acrescentam prazo. Abrir um menu não pausa o mundo nem o combate já iniciado; use END para suspender. O primeiro spawn espera menus fecharem. Suspender/reload zera o cronômetro ativo da retomada e mantém snapshot/progresso confirmado. Pendente bloqueia comércio/estoque/vendas/processamento territoriais e novos fretes até concluir/cancelar; outras posses e saldos não são bloqueados ou apagados por isso.

## Dados/configuração

Novo **Wallet.GangRaid**: Pending, Attempts/Completed/Failed e recibos limitados. Pending guarda área/rival/site/modelos/opções/capacidade/Loot, Defeated e Package. Contratos cancelados contam em Attempts mas não em Failed. Estado legado sem GangRaid segue sem contrato/conta criada no startup.

**GangWarehouse.Accounts.Raided** opcional0: Stock=Deposited+Supplied+Raided−Withdrawn−Sold. Conclusão grava Raid+Warehouse juntos na Wallet, com rollback das referências em falha. Demais livros/metadados são mantidos. Dados inválidos/overflow bloqueiam sem apagar ou compensar estoque.

**Gangs/Raids.json**: MinLoot1…MaxLoot≤90 (35/90), SecondsLimit120–1800 (900), SearchSeconds2–10 (2), Radius40–150 (100) e1–30 Sites distintos. O contrato captura opções e carga ao iniciar. Dealer.json fornece catálogo/pesos; Warehouse.json fornece capacidade/raio inicial. Alterar peso/catálogo pode bloquear crédito até resolver. Opções inválidas usam padrões em memória sem substituir arquivo.

## Referência e diferenças

PSRP.GangSpawner.SpawnDrugSupplyRunEncounter cria traficante e2–4 guardas; body search demora2000ms, recupera DrugPackage e dois tipos aleatórios35–89 escalados por territórios rivais; retorno credita estoque e MissionWin. Também usa relações/armas da gangue e aggro por mira/dano.

RPCore usa cap3 pistolas, ataque imediato, prova nos três abates,35–90 inclusivos sem escala, registro de carga sem item real e sem penalidade permanente de relações ou prêmio de reputação. Não inicia guerra automática nem altera IA policial. Auditoria PSRP-SYSTEMS-AUDIT-0.56.0.json registra evidências/hash/diferenças. Convoys/armas/boost/diálogo/polícia e fases8 (interface/mecânicas) e9 (todos os trabalhos) permanecem pendentes.

## Testes no GTA

- Anote banco/cash/estoques. Inicie, siga o GPS e confira somente3 rivais próprios. Faça os três abates, revista E2s e retorno sem procurado; antes do retorno nenhum pacote deve entrar. Depois, apenas os dois produtos capturados/Raided aumentam, pessoal/legais/dinheiro iguais.
- Reabra/reload/E repetido após entregar: sem crédito extra. Venda/processamento dos pacotes roubados deve usar o ciclo anterior.
- Suspenda no combate e retome na área: refaz os três com mesma carga. Suspenda/reload após a revista: retoma retorno sem respawn. Menu/distância/veículo devem interromper a revista sem criar carga.
- Teste morte/abandono/prazo e cancelamento depois da revista: sem estoque novo e sem apagar anterior; Failed só nos casos de falha. Perda de posse bloqueia entrega, mas cancelar continua possível no comércio principal.
- Confira atores/blips/animação próprios limpos, polícia nativa ainda funcionando, sem troca/recuperacao forcada de veiculo, armas ou vida do jogador; dano e efeitos nativos do combate continuam. Tag/streaming/terceiros interferindo devem suspender, não conceder loot.
- Confira frete legal/criminal e retomada/estorno após encerrar roubo, armazém/estoque/receita/tesouraria antigos, trabalhos/defesas/iniciações, F2 e F11.

2002 testes aprovados, build sem erros/avisos. GTA não executa nesta nuvem: combate/dano/tag, revista/corpo, locais/chão/polícia e compatibilidade precisam da sua validação real.
