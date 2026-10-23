# Fase 5.2 — RPCore 0.14.0: pesca

Atualização: o usuário confirmou esta entrega funcionando antes de avançar para a [fase 5.3 — crafting e feira](FASE-5.3-0.15.0.md). O texto abaixo registra a entrega original.

Esta subentrega implementa pesca com código próprio: equipamento permanente por perfil, iscas, lançamento em água, fisgada, três puxadas, bolsa persistente e venda. Caça 0.13.0 continua incluída, sem declarar um teste no GTA que o usuário não descreveu. Os oito trabalhos já aprovados e o teleporte permanecem; refinamentos desses trabalhos continuam para depois das fases principais. Crafting/comércio, tráfico, streamer e arrancada seguem pendentes na fase 5.

## Instalação

Feche o GTA. Extraia RPCore-update-0.14.0.zip e execute Install-RPCore.ps1 **dentro da pasta extraída**, usando o caminho real da sua instalação:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

O update contém DLL pronta; não precisa compilar. O instalador faz backup da DLL e acrescenta Fishing.json somente se ausente. Não apague Saves/SaveData/configurações. Telefone F2, teleporte F11, Hunting.json e carteira/itens/trabalhos anteriores são preservados. Não reinstala nem modifica OIV/YTD/RPF/DLC/dlclist.

## Como jogar

1. **F10 → Trabalhos / contratos → Pesca → GPS: equipamento / venda**. A banca fica na entrada pública do pier Del Perro (-1604.1, -1048.2, 13.0). E próximo ao ponto abre o menu. Pode usar o teleporte do F11 para agilizar o teste.
2. A pé na banca, compre equipamento por **350 do banco** e cinco iscas por **50** (10 cada): total inicial **400**. O direito à vara fica salvo por perfil; não é necessário comprar novamente após reload. Não altera suas armas.
3. Selecione **Iniciar pescaria**: requer equipamento, ao menos uma isca e personagem a pé, sem procurado/missão/combate. Bloqueia outras atividades até encerrar. Marca o ponto de pesca mais próximo; se quiser outro, escolha o GPS correspondente depois de iniciar.
4. Há três áreas verdes: **pier Del Perro, pier Chumash e marina Alamo Sea**. São posições aproximadas próprias do RPCore, configuráveis em Fishing.json, não coordenadas atribuídas à PSRP. Dentro da área de 60 m, fique em piso firme e guarde a arma. Olhe para água aberta próxima à borda e pressione **E**. Não precisa entrar na água.
5. O lançamento verifica água à frente (3–16 m), diferença de altura até 18 m e chão sob a água. Só gasta uma isca depois de criar a vara nativa e confirmar o salvamento. Se disser para olhar para água, reposicione/rote na borda do pier, mantendo-se em piso firme. Falha de modelo ou gravação não gasta isca.
6. Espere **8–15 segundos**. Quando aparecer **FISGOU! E agora**, pressione **E em até 2,5 segundos**. Antes disso, apertar E não antecipa a captura.
7. Depois da fisgada, pressione **E mais três vezes**, com intervalo de **pelo menos 0,6 segundo** e **menos de 3 segundos entre puxadas**. O painel mostra o contador. Pressionar rápido demais é ignorado; esperar além do prazo perde o peixe e libera outro lançamento com nova isca.
8. O peixe recolhido só entra na **Bolsa de pesca** após salvar. Complete **três peixes** para encerrar a pescaria ou segure **End por um segundo** para cancelar antes. Volte à banca e abra **Bolsa de pesca → Vender peixes conhecidos**. O ganho vem da venda; não há bônus automático ao completar três peixes.

## Bolsa, preços e persistência

A bolsa desta atividade fica em **Wallet.json/Fishing** do perfil, com equipamento, iscas, peixes, capturas, pescarias, ganhos e recibos recentes. É acessada no menu Pesca, separada do inventário pessoal I e da bolsa de caça. Não converte nem regrava Inventory.json. Integração ampla com crafting/comércio fica para a próxima subfase.

| Peixe | Peso por unidade | Venda por unidade | Chance padrão |
| --- | --- | --- | --- |
| Sardinha | 0,4 kg | 40 | 55% |
| Cavala | 0,8 kg | 85 | 30% |
| Robalo | 1,5 kg | 160 | 15% |

Capacidade padrão: **20 kg**. O peixe da tentativa é escolhido uma única vez; falha ao salvar não sorteia outro nem duplica recompensa. Venda remove os peixes conhecidos e credita o banco no mesmo checkpoint. Falha de gravação restaura banco/bolsa em memória. Saldo/contadores têm limites; materiais e campos desconhecidos são preservados. Histórico de recibos limitado a 512 entradas.

Bolsa cheia mantém a captura pendente enquanto a pescaria estiver ativa, sem conceder item antes de salvar. Você pode vender estoque na banca para liberar espaço; a confirmação será tentada novamente após fechar o menu. Cancelar/recarregar/trocar perfil antes de salvar uma captura pendente descarta somente esse peixe; peixes já salvos permanecem. Pescaria/temporizadores ativos não retomam após reload.

## Interrupção e compatibilidade

Sair mais de 1,8 m do lançamento, selecionar arma ou abrir menu/celular/inventário interrompe a tentativa em andamento; a isca já usada não retorna. Pausa também interrompe a tentativa e mantém a sessão disponível. Uma captura já recolhida e pendente de salvamento é conservada durante menus. Morte, veículo, natação, combate, ragdoll, missão, procurado e troca de perfil encerram a sessão, mantendo peixes salvos. Caída/teleporte interrompe o lançamento, sem encerrar a sessão apenas pela transição no ar.

Somente a vara e o clipe de animação criados pelo RPCore são retirados/parados; não há ClearAllTasks, congelamento, limpeza de NPCs, alteração de polícia ou criação de peixe-objeto de ambiente. Vara nativa **prop_fishing_rod_02**, anexada à mão, e animação nativa de pesca. O menu usa **ShopBanner** dos YTD instalados; HUD de fisgada/recolhimento é próprio, com instruções de Context/E (controle também funciona com INPUT_CONTEXT). As imagens FishingCatchKeyboard/Controller citadas no código de referência não constam no catálogo de texturas enviado e não foram inventadas como dependência.

Arquivo novo **scripts/RPCore/Jobs/Fishing.json**, também embutido como fallback. Configuração válida preserva pontos, preços, catálogo, pesos, probabilidades, limite, número de peixes e tempos personalizados. Inválida é preservada e usa o padrão embutido. Dados de pesca semanticamente inválidos não são zerados silenciosamente. Não exige PSRP.dll rodando em paralelo.

## Comparação com a PSRP e arquivos alterados

Inspeção estática de PSRP/_Activity__Hunting.cs: há FishingStates, vara e verificação de água; **SetUpHookPhase está vazio**, e CheckFishing aparece apenas na própria declaração, sem chamada no OnTick. Não existe nesse trecho um ciclo completo de captura/venda a transplantar. Os três pontos, peixes, preços e dinâmica desta entrega são próprios. Auditoria reproduzível em PSRP-SYSTEMS-AUDIT-0.14.0.json, com hashes e 75 fontes indexadas.

- **FishingData.cs / FishingJob.cs / config/Jobs/Fishing.json**: catálogo, configuração, estados, água/vara/animação, prazos, três puxadas, bolsa, compras, venda e cancelamento.
- **Economy.cs / Wallet.cs**: campo aditivo Fishing e adaptadores para o mesmo checkpoint bancário; não substituem Hunting/Jobs/Inventory.
- **Main.cs / SettingsSystem.cs**: décima atividade na central e GPS da banca no F11/J; exclusão compartilhada entre dez atividades.
- **AchievementSystem.cs**: três conquistas novas de peixe/pescaria/venda; venda incrementa cash_earned após o checkpoint.
- **Visuals.cs / config/Visuals/Bindings.json**: banner de pesca e fallback; defaults são combinados em memória com configurações locais.
- **RPCore.csproj / tests/RPCore.Tests.csproj**: Fishing.json embutido e lógica real vinculada aos testes.
- **tests/FishingTests.cs**: 47 novos casos; versões em AssemblyInfo.cs/Main.cs/CoreMenu.cs/VisualAssetDefaults.cs atualizadas para 0.14.0. Auditoria, documentação e pacotes atualizados.

## Validação e roteiro no GTA

Build Release net48 com SHVDN Enhanced **1.1.0.6** e LemonUI SHVDN3 **2.2**: zero erros e zero avisos. **477 testes aprovados**, 430 anteriores mais 47 novos: compras/banco/replay/rollback, limites/peso/venda, campos desconhecidos e saves anteriores, prazos/fisgada/puxadas/overflow do relógio, três peixes, cancelamento, falha de gravação, retry e exclusão de dez atividades. Não executam nativos/renderização/água no GTA.

1. Instale sobre a 0.13.0; confira F2, caça, licença de piloto, saldo e inventário anteriores.
2. Na banca compre kit e cinco iscas: débito de 400 no banco. Recarregue scripts e confira equipamento/iscas.
3. Inicie, siga ao pier, guarde a arma e lance olhando para água. Confira modelo da vara, animação e uma isca consumida. Tente olhando para terra: não deve consumir.
4. Deixe expirar uma fisgada, erre uma puxada e confirme que não ganha peixe. Lance novamente e faça E na fisgada, depois três E espaçados; confira um peixe salvo.
5. Complete três peixes, venda na banca e confira preço/quantidade, crédito único, bolsa vazia e persistência após reload. Clicar em vender sem estoque não paga.
6. Teste menu/pausa/cancelamento/morte, troca de perfil e bolsa cheia; confirme limpeza da vara e conservação dos peixes salvos. Faça regressão rápida de caça, trabalhos e teleporte.

Posições das bordas, detecção de água/chão, vara/animação, leitura do painel e conforto dos prazos precisam de teste real com seus recursos instalados. O GTA não foi executado nesta nuvem.
