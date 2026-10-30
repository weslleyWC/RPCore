# Correção0.100.1 — portas das lojas de roupas

O usuário confirmou que não conseguia atravessar a porta, antes de chegar ao marcador interno da0.100.0. Faltava o tratamento de portas presente em `MultiplayerCharacter.ShopsDoorsLocation/UnlockDoors/Update` da PSRP.dll. O GPS e o editor internos existiam, mas isso não bastava para acessar a loja com portas travadas pelo jogo.

## Correção

`WardrobeSystem.Tick` agora executa o acesso físico antes dos retornos de distância/menus. São os14 pontos de **entrada** das lojas de roupas extraídos da referência, separados dos14 marcadores **internos** já existentes. Enquanto o jogador está a pé em contexto seguro, o RPCore consulta as portas na entrada próxima e remove a trava, conservando o ângulo lido. A consulta se repete a cada frame de proximidade para acompanhar streaming e retravas do script nativo.

A rotina foi limitada aos quatro modelos de porta de roupas: `v_ilev_ch_glassdoor`(-1922281023), `v_ilev_clothmiddoor`(1780022985), `v_ilev_cs_door01`(868499217) e `v_ilev_cs_door01_r`(-1148826190). Os nomes/hashes também foram conferidos no catálogo público ObjectList.ini de DurtyFree/gta-v-data-dumps. A rotina mais ampla da PSRP também contém Ammu-Nation, barbearias, concessionária e garagens; esses modelos não foram incluídos nesta correção.

Atuação até6m da entrada e diferença de altura até3m, apenas quando existe uma loja configurada válida até25m dessa entrada. Uma loja removida/deslocada para longe em Services/Clothing.json não continua destravando sua entrada antiga. Lojas personalizadas em outros imóveis não recebem portas presumidas. Usa o sistema nativo de portas existentes, sem registrar portas novas, remover objetos, abrir ângulo fixo ou editar RPF/OIV/YTD. Não há tecla nova, flag global ou gravação de estado de porta nos saves.

A elegibilidade segue SafeFoot dos serviços pessoais: fora de missões/cutscenes/pausa, vivo, humano, com controle e perfil correto, sem procurado/combate/veículo/ragdoll/queda/escalada/nado/entrada em veículo. Fora desse contexto a correção deixa de intervir; não restaura uma trava antiga por conta própria. Outros mods ou scripts podem continuar controlando suas portas. Erro nativo de porta é registrado uma vez no RPCore.log e não derruba o editor inteiro.

## Instalar e testar no GTA

1. Feche o GTA, extraia a atualização e execute o instalador da pasta extraída, usando o caminho real do jogo. Não precisa reinstalar OIV/RPF/YTD nem apagar Services/Clothing.json.
2. Vá **a pé**, sem procurado ou missão, a uma loja de roupas indicada no J/F11. A porta deve permitir passagem ao se aproximar; ela não é removida nem forçada a ficar aberta.
3. Dentro, aproxime-se do marcador e aperte **E** para abrir roupas/conjuntos RPCore, como na0.100.0. Teste entrar e sair novamente, inclusive após recarregar plugins ou afastar para forçar novo streaming.
4. Confira Binco/Discount, Suburban e Ponsonbys, incluindo porta dupla quando presente e lojas fora da cidade. Confira também a travessia de dentro para fora.
5. Confira roupas/conjuntos anteriores e saldo. Ammu-Nation/barbearias/garagens não devem receber alteração de portas desta rotina. Serviços e prévias da0.100.0 permanecem conforme seu guia.

Sem GTA neste ambiente: a compilação e os testes de alcance/modelos não comprovam colisão, streaming, interação com scripts nativos ou todos os mapas modificados. O teste acima ainda precisa ser feito no jogo.
