# Refinamento do grupo de armazenamento — 0.104.0

Grupo revisado: StorageSystem (depósitos públicos e porta-malas), HomeStorageSystem (apartamento), seus menus em lista e os dois painéis da InventoryGridView. Referência estática: PSRP.NewStorageSystem, NewInventorySystem e Vehicle_Insurance.OpenVehicleTrunk/CloseVehicleTrunk da DLL fornecida. A PSRP utiliza painéis de seis colunas, quatro linhas visíveis, transferência de unidade/pilha e identidade do porta-malas por modelo/placa. O RPCore conserva essa base visual e implementa suas próprias verificações e transferências em um checkpoint Inventory.json. Esta entrega não conclui os grupos restantes da fase8 nem inicia o realismo dos trabalhos da fase9.

## Acesso

- **F8**, ou seu atalho Storage personalizado, próximo de um porta-malas destrancado e parado ou depósito configurado. Depósitos mantêm ícone e recebem marcador/ajuda próximos com o atalho real.
- **Apartamento**: entrar na propriedade comprada, usar E no ponto do armazenamento ou a opção já existente de moradia. O depósito continua privado por perfil/propriedade.
- O acesso exige estar a pé, com controle, fora de missão/cutscene/procurado/combate e outras atividades. Veículos de trabalho continuam excluídos. A sessão fecha ao perder contexto, se afastar, mudar ator/modelo/perfil, placa/modelo do veículo, trancar/mover/destruir o carro ou perder acesso ao apartamento.
- Para carros com bone `boot`, a proximidade acompanha o porta-malas real do modelo. Modelos sem esse bone conservam o ponto traseiro anterior. Carros adicionais precisam de teste GTA quanto ao posicionamento do bone.

## Grade

Mantém os sprites `PSRP_Inventory/Background`, `Top`, `InventoryBox` e ícones dos itens instalados. Não precisa reinstalar OIV/RPF/YTD.

| Ação | Teclado/mouse | Controle |
|---|---|---|
| Selecionar lado/item | Mouse, TAB e setas | RB e direcional |
| Transferir1 | Enter ou arrastar para o outro painel | A |
| Transferir pilha inteira | Shift+Enter ou Shift ao soltar no outro painel | LB+A |
| Transferir o que couber | Ctrl+Enter ou Ctrl ao soltar no outro painel | X |
| Informar quantidade | Q, informar inteiro positivo | Usar teclado na entrada GTA ou opções da lista |
| Fechar | Backspace/Escape/atalho inventário ou armazenamento | B |

Os dois cabeçalhos mostram peso/capacidade com barras; a faixa inferior informa item, quantidade, peso unitário e quantas unidades cabem. Rodinha/PageUp/PageDown continuam com rolagem. Clique simples seleciona. Arrastar dentro da mochila conserva a reorganização dos slots; arrastar entre painéis transfere quantidade, sem trocar a identidade dos itens. A posição dos itens no depósito é de apresentação da sessão, como antes; não foi criado um segundo inventário/arquivo de slots persistente para ele.

A transferência por gesto valida o item/slot e a quantidade de origem na soltura, consome o gesto uma única vez e cancela se a origem mudou. Soltar fora dos painéis, clicar sem arrastar, trocar para controle ou cancelar não transfere. Shift/Ctrl podem ser pressionados durante o arraste. Operações de pilha/quantidade exata são inteiras: se não couber, nada é dividido automaticamente. A opção explícita “o que couber” escolhe a quantidade que cabe.

## Menu em lista

Ao usar a preferência existente de inventário sem grade, o armazenamento agora tem resumo dos dois pesos, Guardar, Retirar, lista ordenada por nome, detalhes da quantidade,1/até5/pilha/o que couber/quantidade digitada, voltar e fechar. Listas vazias mostram mensagem e itens sem definição conservam seu ID e peso padrão0,1kg. Opções que excedem espaço ficam desabilitadas; o salvamento ainda revalida os limites. Ações são despachadas depois de ObjectPool.Process, sem reconstruir itens dentro dos eventos LemonUI, e não leem SelectedIndex de menu vazio na primeira abertura.

## Persistência e integração

Nenhuma migração ou renomeação de containers. Chaves existentes `stash:<id>`, `trunk:<model>:<placa aparada>` e `home:eclipse` permanecem, com distinção de maiúsculas da placa. Dois veículos do mesmo modelo e mesma placa continuam compartilhando essa chave legada; não se promete identificador global único. Alterar uma placa fecha a sessão sem transferir conteúdo para a nova chave.

Mochila e depósito continuam gravados juntos por InventoryTransfers.Move; falha de gravação não publica candidato modificado. Campos desconhecidos, outros depósitos, itens legados e metadados de arma são conservados. Não há mudança de saldos do banco. A atividade de armazenamento tem reserva própria para não concorrer com trabalhos/consumíveis; a armaria reconhece explicitamente storage e homestorage, mantendo sincronização de arma/munição e acessórios. As demais atividades continuam excluídas.

Configurações de locais não finitos/inválidos deixam de gerar blips/markers; arquivo original é conservado. Capacidades e coordenadas personalizadas finitas existentes não são redefinidas. O marcador do apartamento não oferece interação enquanto outra atividade está ocupada. A rotina não acrescenta animação ou controle automático da tampa do porta-malas; as rotinas PSRP de Wait/ClearAllImmediately não foram copiadas para este fluxo.

## Teste no GTA

1. Com itens na mochila, ir ao depósito Strawberry ou ao seu depósito configurado. Abrir F8. Testar unidade/pilha/quantidade digitada/o que couber nos dois sentidos, com mouse e teclado; testar controle se usar.
2. Arrastar da mochila para o depósito e de volta. Pressionar Shift/Ctrl durante o arraste. Clicar sem arrastar e soltar fora não deve mover/consumir. Reorganizar a mochila deve continuar funcionando.
3. Encher o depósito ou a mochila: pilha que não cabe deve falhar inteira; “o que couber” deve respeitar o limite. Conferir peso/sobrepeso e soma total dos itens antes/depois.
4. Fazer o mesmo num carro pessoal destrancado e parado. Testar posição do porta-malas em modelos diferentes, afastar-se/entrar no carro/trancar pelo recurso existente. Cada perda de acesso deve fechar a tela sem item sumir ou duplicar.
5. No apartamento comprado, repetir pelo E e pelo menu de moradia. Sair, voltar, salvar e reabrir o GTA; conferir os itens. Outro perfil não deve ganhar os itens do primeiro.
6. Guardar/retirar arma e munição separadas. Conferir acessórios/tinta e munição real, gastar algumas balas e repetir: nenhuma restauração da munição antiga.
7. Em F11 → UI Settings, desativar temporariamente Inventário em grade. Abrir cada tipo de depósito em lista, navegar resumo/guardar/retirar/quantidade/voltar/fechar. Restaurar a preferência depois.
8. Conferir que F2/J/F10/F11 e a seleção inicial opcional0.103 continuam funcionando. Após fechar o depósito, deve ser possível começar trabalho/consumir item normalmente.

O ambiente cloud compila/testa dados e simula instalação, mas não executa GTA, animações/natives ou o mouse real dentro do jogo. O roteiro acima é a validação nativa necessária.
