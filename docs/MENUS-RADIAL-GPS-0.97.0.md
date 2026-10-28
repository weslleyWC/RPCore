# Refinamento0.97 — F11, radial, GPS e gestos

O usuário confirmou a0.96 funcionando e reiterou revisão de todos os sistemas/menus. Esta atualização entrega o grupo abaixo e registra o restante em COBERTURA-REFINAMENTO.md; não declara a fase8 inteira concluída. Fase9 mantém todos os trabalhos/atividades/missões para a revisão dos ciclos e realismo.

Referência consultada: SettingsMenu (hierarquia, NativeCheckboxItem e preferências); RadialMenu (root/inventory, paginação6+2, GPS por tipo e lista de emotes). Foram preservadas as oito categorias do F11 e os oito setores do radial, as texturas instaladas e os atalhos personalizados. A fonte tem entrada Emotes marcada WIP; esta atualização oferece dois gestos seguros dessa lista e os dois anteriores do RPCore. Não é portagem dos crimes/terceiro olho, de todas as páginas de carro/roupa, de lojas ainda ausentes ou de toggles globais de polícia/imunidade.

## F11

Preferências passam a usar caixas de seleção nativas com estado atualizado, em vez de texto ON/OFF acionado sem tratamento. Ajustes técnicos usam listas esquerda/direita e conservam valores personalizados. A gravação precede a aplicação em memória e trata também o retorno false de ScriptSettings.Save. InterfaceOptions.Set escreve em arquivo temporário no mesmo diretório e só substitui o destino após sucesso, guardando o anterior em .bak; falha normal conserva o original e o estado da opção. Arquivos existentes não são alterados pela instalação, somente por uma escolha explícita.

São preferências de interface/ajustes já existentes: não foram criados novos comandos de ativar mecânicas de RP. F11 e seus submenus fecham em pausa/missão/cutscene/morte/perda de controle ou troca de personagem. Teleporte para waypoint continua no Cheat Menu; não muda sua lógica. F2 continua celular e J continua radial.

## Radial J

- Q/E ou esquerda/direita: selecionar. Enter confirma; Backspace volta uma página; J fecha. No controle: direcional/A/B.
- Mouse: mover sobre um setor seleciona; pressionar/soltar no mesmo setor confirma. Soltar fora ou no centro não executa. Botão direito volta; troca para controle cancela o clique e espera soltar os botões. Centro e instruções ficam fora da área clicável.
- Inventory abre página com Abrir inventário, Bebidas, Comida, Saúde, Armas, Ferramentas, Roupas e Voltar. As categorias mostram apenas itens possuídos com definição e ação implementada; seis itens por página, mais anterior/voltar e próxima. Não limitam o uso aos seis atalhos de teclado.
- Usar item/equipar/guardar passa pelo mesmo UseSelected do inventário: peso, quantidade, munição, recibos, animação e bloqueios de segurança existentes continuam válidos. Não cria estoque/cópias separados. Ferramentas lista atualmente Phone reutilizável; materiais sem uso direto não são apresentados como ferramenta funcional.
- Roupas abre o guarda-roupa existente e respeita acesso ao Eclipse. Não permite trocar roupas remotamente nem implementar itens/clothing indisponíveis por um ícone.
- Ações que fecham o radial são verificadas e executadas no próximo Tick elegível, depois de sair do modal. Troca de perfil/ped, pausa, missão, outro menu ou atraso excessivo cancela a ação. Navegar entre páginas permanece imediato.
- Abertura/uso protege combate e seleção de armas por frame, incluindo teclas diretas e armas de veículo; não altera direção, vida, tempo, arma ou coordenadas do jogador globalmente. Uso de itens continua exigindo contexto seguro a pé. Mouse/geometria usa a mesma escala do desenho e os centros da referência, sem fazer o cursor parado desfazer a seleção por teclado.

## GPS e marcadores

Fast GPS tem Serviços próximos, Trabalhos e Meus locais. Serviços usam posições dos sistemas em execução, incluindo conveniências, postos, barbearias, garagens, oficinas em garagens próprias e pontos ativos do editor. O radial oferece seis acessos rápidos e Todos os destinos. Rota é revalidada ao confirmar; não cria serviços por existirem blips ou nomes na referência.

A referência tem Clothing Shop, enquanto o RPCore oferece guarda-roupa no apartamento: nesse setor de GPS foi colocado Garagem, um serviço real. Butcher/contato/crafting/etc mantêm seus acessos no grupo Trabalhos. A distância exibida é aproximada em linha reta/3D, sem promessa de distância rodoviária.

Meus locais separa selecionar rota de gerenciar marcadores. Cada ponto tem rota, renomear e Excluir marcador...; a exclusão exige uma segunda confirmação, com Cancelar selecionado primeiro. Os antigos pontos de MapPins.json continuam no mesmo formato por personagem; posições/nomes duplicados e dados extras são preservados. Add/rename/delete usam cópia candidata, gravação e atribuição só após sucesso. Confirmação antiga não pode remover um ponto alterado. Não há filtro/truncamento silencioso de saves inválidos/mais de100 pontos; ficam conservados e com edição bloqueada, com diagnóstico no log. Limite de criação segue100 por perfil.

## Gestos e interrupção

Emotes inclui acenar/aplaudir e mãos para cima em pé/ajoelhado, com duração curta de4s. Usa carregamento assíncrono limitado e para apenas a animação RPCore; andar, atirar, combater, cair, nadar, entrar em veículo, morrer, pausar, trocar ator/perfil ou iniciar missão encerra. O gate emote impede disputa com trabalho/repouso/consumo e é liberado ao terminar/cancelar. Não limpa todas as tarefas nem posiciona o personagem. Os gestos de encostar/sentar da lista WIP da referência ficam para uma interação com apoio/assento validado; não foram anunciados como poses ancoradas funcionais.

## Teste GTA

3725 testes aprovados (3686 anteriores +39 novos), build net48 sem erros/avisos. Testes cobrem dados de marcadores, falhas de escrita/staging/backup real em diretório temporário, confirmações antigas, geometrias e filtros/paginação. Natives, LemonUI, mouse, gestos e serviços físicos precisam dos testes abaixo.

1. Abra F11, percorra as oito categorias, altere uma preferência de HUD/roda/zona segura e um valor técnico. Feche/reabra/recarregue e confirme valor e backup. Confira teleporte e F2/J.
2. Abra J e navegue pelos oito setores. Entre em Inventory e use comida/bebida/bandagem; equipe/guarde arma e abra inventário completo. Quantidade e efeito devem ocorrer uma vez. Teste mais de seis tipos numa categoria e espaços vazios.
3. Teste mouse, soltar fora/centro/instruções, voltar com botão direito, teclado e troca de dispositivo com botão mantido; nunca deve consumir sozinho. Confira escala/área segura/resolução normal e ultrawide.
4. Use GPS de posto/loja/barbearia/garagem e ponto ativo do editor. Desative/remova um ponto e confirme que não recebe rota para serviço inativo. Confira contratos antigos no grupo Trabalhos.
5. Salve, renomeie, marque rota, cancele exclusão e confirme exclusão de um marcador. Recarregue e confira outros marcadores e outro perfil. Nome duplicado não deve apagar os dois.
6. Use cada gesto e interrompa andando/entrando em carro/pausando; depois abra inventário ou inicie um trabalho para confirmar a liberação do gate. Confira mãos/bones/pose de joelho no seu ped.
7. Repita compra em lote, props únicos/removidos, arraste, peso, munição, itens no chão, sono e teclas1–6 fora de modais. Não execute PSRP.dll junto do RPCore.
