# Fase 5.1 — RPCore 0.13.0: caça

O usuário confirmou piloto e teleporte funcionando e pediu concluir as funcionalidades restantes antes do refinamento dos trabalhos. Esta entrega começa a fase 5 com caça: licença/equipamento, iscas, animais próprios, coleta com qualidade, bolsa persistente e venda. Os oito trabalhos anteriores e o teleporte permanecem como aprovados. Pesca e crafting/comércio serão subentregas seguintes; tráfico, streamer e arrancada permanecem no roteiro da fase 5. Não há paridade completa com todos os animais/sistemas da PSRP nesta entrega.

## Como jogar

1. F10 → Trabalhos / contratos → Caca → GPS: acougue / equipamento. O ponto público (-42.14478, -1474.272, 31.21704) é o açougue da referência PSRP. F11 → Teleportar para o waypoint pode agilizar a chegada. E no ponto abre o mesmo menu.
2. A pé no açougue, compre Licenca + equipamento por 750 do banco. O direito de equipamento fica salvo por perfil. O kit fornece faca e mosquete se ausentes, e garante pelo menos 20 munições no mosquete; suas outras armas não são retiradas. A opção Retirar equipamento licenciado permite repor esse kit no açougue sem pagar a licença novamente. Não é uma autorização para modificar a polícia nativa.
3. Compre cinco iscas por 100 (20 cada) e selecione Iniciar expedicao. Precisa de licença, ao menos uma isca e personagem a pé sem procurado/missão/combate. Uma atividade por vez, compartilhando a trava dos oito empregos anteriores.
4. Siga o GPS verde para Paleto Forest (-769.8, 5595.5, 33.5), zona de 450 metros, definida pelo RPCore. Você pode usar o teleporte a pé até o waypoint para testar. Na floresta, E coloca uma isca à frente, cria um cervo/javali/coelho próprio numa posição livre próxima e mostra seu blip laranja. Só debita a isca quando o animal foi criado e o checkpoint foi salvo. Se faltar posição, tente outro ponto da área; não perde isca por falha de spawn.
5. Afaste-se da isca e acompanhe o blip. O animal tenta aproximar-se; tiros ou aproximação a menos de 14 metros o assustam. Pode fugir e ser perdido se sair mais de 200 metros da isca; abandonar a área da isca em mais de 250 metros também libera outra tentativa com nova isca. Não há remoção/captura de animais de ambiente: só os gerados pela expedição são processados.
6. Abata o animal com sua arma. Ao chegar a menos de três metros da carcaça, E seleciona a faca e inicia coleta de cinco segundos. Fique próximo, a pé, fora de combate, com a faca selecionada. Movimento para longe, arma diferente, veículo, menu/modal ou morte interrompem. A coleta só é confirmada após o tempo e salvamento; uma mesma carcaça não duplica material.
7. Complete três coletas por expedição (ou cancele antes, mantendo as já salvas). Abra Caca → Bolsa de caca para consultar carne/peles/troféus. Volte ao açougue e escolha Vender materiais conhecidos. O crédito bancário e a retirada de materiais são um checkpoint único. Não há bônus de dinheiro só por acabar a expedição: o ganho vem da venda.

## Bolsa, qualidade e valores

A bolsa de caça é própria desta atividade, acessível pelo menu Caca, com 25 kg padrão. Materiais, licença, iscas, receitas confirmadas e contadores ficam em Wallet.json/Hunting do perfil, junto ao saldo bancário. Não é necessário converter/apagar Inventory.json, e os itens pessoais/porta-malas anteriores permanecem iguais. A integração ampla de crafting/comércio fica para subetapas seguintes.

- Cervo: quatro carnes e uma galhada; javali: quatro carnes; coelho: duas carnes e um pé. Cada um também rende uma pele quando aproveitável.
- Mosquete, faca e rifles sniper/marksman geram pele perfeita; pistolas indicadas geram pele boa; demais causas comuns geram pele danificada. Impacto de veículo, explosão e fogo reconhecidos inutilizam a carcaça: coleta conta, mas não rende material. Esta regra usa causa de morte, não uma simulação de quantidade/local de tiros.
- Preços por unidade: carne de cervo 45, javali 40, coelho 25; pele danificada 15, boa 45, perfeita 90; galhada 60; pé de coelho 25. Um cervo perfeito rende 330 na venda padrão. Peso e preços podem ser personalizados.
- Bolsa cheia rejeita o pacote inteiro e mantém a carcaça disponível enquanto a expedição seguir ativa. Falha de gravação mantém a coleta pendente com tentativa posterior; não entrega material antes de salvar. Se cancelar ou trocar perfil, a carcaça própria ainda não coletada é retirada, sem alterar o estoque já salvo.
- Venda preserva materiais desconhecidos de outros pacotes, não ultrapassa limites de saldo e não paga duas vezes por estoque já retirado. Falha no salvamento restaura bolsa e banco em memória. Os recibos recentes são limitados a 512 entradas.

## Configuração, compatibilidade e limites

Novo scripts/RPCore/Jobs/Hunting.json, criado/instalado só se ausente e embutido como fallback na DLL. Licença 750, isca 20, AnimalsPerTrip 3, HarvestSeconds 5, ZoneRadius 450, BagCapacity 25. Vendor/Forest, oito materiais com peso/preço e estes parâmetros aceitam personalização dentro dos limites validados. Arquivo inválido é preservado e usa fallback; arquivo válido mantém overrides. Posições próprias da floresta não são atribuídas à PSRP.

O ponto de licença/equipamento foi combinado com o açougue para este ciclo independente; a referência vende licença/isca na Ammu-Nation. O banner ButcherBanner usa os YTD instalados. O prop da isca é cosmético e tem fallback para o marcador/rastreamento se o modelo não estiver disponível. Não depende de alterar OIV/YTD/RPF/DLC. Os animais não são invencíveis e não há limpeza de procurado, substituição de IA global, remoção de animais/veículos de outros mods ou captura de NPCs existentes.

End segurado por um segundo ou Encerrar expedicao cancela. Morte, missão e troca de perfil também encerram, preservando coletas salvas e iscas restantes. Animais/iscas/blips próprios são retirados. A animação só para o clipe iniciado pelo RPCore no ped que o iniciou; não há ClearAllTasks. Expedição ativa não retoma após reload; bolsa/licença/contadores persistem por perfil. F2 e teleporte seguem iguais. Refinamento dos trabalhos já aprovados continua para depois das fases principais.

## Arquivos alterados

- HuntingData.cs/HuntingJob.cs/config/Jobs/Hunting.json: configuração, ciclo, qualidade, bolsa, transações bancárias e recibos, fauna própria, atração/fuga, animação, venda e limpeza.
- Economy.cs/Wallet.cs: campo aditivo Hunting e adaptadores de checkpoint; preservam campos anteriores e extension data.
- Main.cs/SettingsSystem.cs: nona atividade, central/GPS do açougue e floresta.
- AchievementSystem.cs: três conquistas novas e gatilho legado 27 ligado à coleta real. Vendas contam no progresso cash_earned.
- Visuals.cs/config/Visuals/Bindings.json: banner original ButcherBanner/fallback.
- AssemblyInfo.cs/CoreMenu.cs/Main.cs/VisualAssetDefaults.cs: versão 0.13.0.
- RPCore.csproj/tests/RPCore.Tests.csproj: default embutido e lógica real vinculada aos testes; tests/HuntingTests.cs acrescenta 34 casos.
- scripts/audit-psrp-systems.py: ponto do açougue conferido em NewInventorySystem.cs e comparação de comportamento com _Activity__Hunting.cs; relatórios, instruções e ZIPs atualizados. Não foi transplantado o fonte PSRP.

## Validação e teste no GTA

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI SHVDN3 2.2: zero erros/avisos. 430 testes aprovados: 396 anteriores e 34 novos. Cobrem licença/iscas/saldo, falta de recursos, checkpoint e rollback, qualidade/material, capacidade, carcaça/tempo/interrupção, coleta e venda sem duplicação, recibos após reload, dados desconhecidos/empregos anteriores/licença de piloto, limites de banco, perda de animal, cancelamento e exclusão entre nove atividades. Não executam nativos/IA/animações no GTA.

Teste solicitado:

1. No açougue, compre licença/kit e cinco iscas. Confira débito de 850 do banco, faca/mosquete e preservação de outras armas/saves/F2. Reabra após reload e confira licença/iscas.
2. Inicie expedição e use GPS/teleporte para a floresta. E coloca isca e consome uma unidade; confira blip, modelo e reação/atração/fuga. Falha de espaço não deve gastar isca.
3. Abata, E próximo e espere cinco segundos. Interrompa uma coleta saindo/abrindo inventário e retome. Confira materiais uma vez por carcaça, inclusive qualidade diferente com outra arma. Complete as três coletas.
4. Volte ao açougue, veja a bolsa, venda e confira crédito/retirada únicos. Clique novamente sem estoque: não deve pagar. Recarregue e confira saldo/progresso/bolsa.
5. Teste cancelamento/morte/perda de animal/troca de perfil e bolsa cheia. Os recursos já salvos ficam; só recursos próprios de mundo são retirados. Faça regressão rápida dos oito trabalhos e teleporte. Terreno/posição da isca, modelos, navegação de animais, fuga, tiro e animação dependem do seu teste real no GTA.
