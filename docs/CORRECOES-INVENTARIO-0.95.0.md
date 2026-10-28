# Inventário 0.95.0 — consumíveis, roda de armas e itens no chão

Correção baseada na revisão de NewInventorySystem (estados UsingDrink/UsingFood/Closed, remoção de item/pilha) e SettingsMenu.OnTick da PSRP.dll fornecida. A referência apaga o objeto ao terminar e aplica o bloqueio da roda a cada frame. Não foi localizado nesse fluxo um sistema de itens descartados recuperáveis: o recurso abaixo é implementação própria. Nenhuma DLL de referência foi executada.

## Consumíveis

Ao concluir ou interromper o consumo, para somente a animação iniciada pelo RPCore; desanexa o objeto próprio, assume a entidade para remoção e solicita DELETE_OBJECT. Guarda handle, modelo e token de propriedade antes da chamada e confirma que a entidade deixou de existir. Se continuar existindo, tenta novamente a cada 250 ms, sem perder a referência porque o wrapper SHVDN foi invalidado. Consumos novos aguardam se a fila de limpeza crescer. Ao iniciar/recarregar, procura somente objetos com o decorador RPCoreInvUse para recolher sobras da versão anterior. Não apaga objetos pelo modelo ou proximidade, não limpa tarefas globais e não modifica props de outros mods.

Todos os consumíveis RPCore usam esse caminho comum. A causa exata da sobra no GTA ainda precisa de confirmação: não reproduzimos o jogo neste ambiente. O código anterior descartava a referência após uma tentativa de Delete, sem verificar a remoção. Registro de propriedade rejeitado não gera um objeto anexado sem identificação.

## Roda e atalhos

A política de input é aplicada ao final do Tick principal, independentemente do sucesso da sincronização de munição, do gate de trabalho e até de uma falha isolada do sistema de inventário. Usa a preferência existente F11 → Game Settings → Ocultar roda de armas do GTA; não altera Interface.ini. Enquanto habilitada, bloqueia os controles da roda e atalhos nativos a pé e oculta o componente visual da roda. O bloqueio numérico existente continua separado. Missões nativas, veículos/embarque, pausa, cutscene e ausência de controle do personagem mantêm os controles protegidos. Não ativa/desativa controles globalmente.

## Largar e recuperar

- No inventário pessoal: Delete larga uma unidade; segure por 0,9 s para largar a pilha. LB no controle faz o mesmo. Delete passa a largar, em vez de excluir definitivamente.
- Uma marca azul indica a posição. A representação é um marcador genérico, sem prop físico ou modelo específico por item.
- Perto da marca, abra o inventário e pressione R para recolher uma unidade; Shift+R recolhe o que couber. Recuperação nesta versão usa teclado; o controle mantém a ação de largar.
- Alcance de 1,8 m e interior igual; a distância considera altura, para não recolher em outro andar. A posição usa um ponto à frente dos pés, sem teleportar o personagem ou buscar um piso de outro andar.
- Quantidades, posição, interior e identificador ficam no campo opcional GroundItems do mesmo Inventory.json por personagem. Inventários antigos continuam válidos. Não há expiração nem remoção automática ao atingir o limite de 128 pilhas; o próximo descarte é recusado.
- Cada operação grava em uma única cópia candidata do inventário antes de trocar o estado em memória. Falha normal de gravação preserva a operação anterior; recolher repetidamente o mesmo identificador não duplica itens. Peso, capacidade e overflow são verificados antes de recolher.
- Armas mantêm pintura, acessórios e registro de munição; uma arma no chão não pode ser reimportada como uma nova aquisição. Largar arma não larga a reserva de munição: largue o item de munição separadamente.
- Itens de definição ausente permanecem salvos no chão até sua definição voltar. Registros inválidos não são apagados/reparados automaticamente: o sistema recusa alterações nesses registros. Não modifica estoques de trabalhos nem depósitos.
- Não permite largar/recolher dentro de veículos, combate, quedas, água, atividades, missões, cutscenes ou outros menus. Não há coleta automática, comando global novo ou disputa pela tecla E das lojas.

## Teste dentro do GTA

1. Atualize usando o instalador do ZIP; reinicie os plugins. Confirme F2/J/F11 e os saves anteriores.
2. Coma dois alimentos diferentes, beba água e outra bebida, use item médico; confira remoção ao terminar, ao interromper e ao entrar em veículo. Recarregue plugins durante o uso e confira se a sobra própria some. Props iguais de outros mods devem continuar intactos.
3. Com roda oculta, teste Tab, scroll e teclas 1–6. Comece/termine um trabalho, abra/feche depósito e menus, troque perfil e volte ao modo livre. Confira que a roda continua oculta; desligue e ligue a opção para verificar a preferência. No veículo e em missão nativa os controles são preservados.
4. Largue uma unidade e uma pilha. Afaste-se/volte; recolha com R e Shift+R. Com mochila cheia, confirme recusa ou recuperação parcial sem perder o restante. Repita com arma, munição e item customizado.
5. Largue itens, recarregue plugins/reinicie o jogo, volte ao ponto e confira quantidades. Troque personagem: cada perfil vê seus próprios registros. Teste interior e andares diferentes.
6. Confirme novamente mouse, peso acima de 30 kg, normalização após aliviar carga e desconto por tiros.

Compilação e testes automatizados verificam lógica de dados e integração compilada. Limpeza de entidade/animação, controles nativos, marcador e posição física precisam desta validação no GTA.
