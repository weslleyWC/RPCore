# Seleção de personagens ao iniciar — 0.103.0

Ao iniciar o RPCore, o jogo aguarda pelo menos um segundo contínuo em contexto seguro: tela carregada, controle liberado, personagem parado a pé, sem missão/cutscene/procurado/combate ou outra atividade/interface. Só então abre a lista de personagens RPCore salvos. Não escolhe um personagem por conta própria. Se não houver registros, abre a criação existente, com nome e modelo masculino/feminino; a aparência detalhada continua disponível nos serviços de barbearia. Não foi implementado um novo editor completo de rosto nesta entrega.

## Ativar / desativar

Ativado por padrão. Em **F11 → Game Settings → Seleção de personagens ao iniciar**, desmarque para não abrir automaticamente na próxima sessão. A opção fica em `scripts/RPCore/Interface.ini`, seção `[Interface]`, chave `CharacterSelectionOnStart=false`. Não precisa editar o arquivo se usar o F11. Reativar vale na próxima inicialização do mod. A lista permanece acessível em **F10 → Personagens e perfis → Personagens / criar e carregar**. Fechar/continuar não reabre o menu a cada frame, save ou troca de perfil; reiniciar plugins pelo F4 inicia uma nova sessão e pode abri-lo novamente se habilitado.

O menu espera sair do carro/perseguição/missão em vez de expulsar o jogador ou cancelar a atividade. Se abrir durante uma situação incompatível, fecha e interrompe o carregamento. Não trava o jogador permanentemente.

## Personagens e saves

A lista apresenta cartões válidos de `SaveData/RP_.../Character.json`, ordenados por nome. Mostra ativo e carregamentos pendentes para retomar. Pastas inválidas são exibidas como protegidas; não são substituídas por novos cartões nem consideradas ausência de personagem. Perfis legados Default/PSRP sem Character.json permanecem no fluxo de perfis legados do F10; não se inventa modelo/carteira para eles.

Criar gera um novo perfil com saldo zero e pede confirmação antes de trocar o personagem. Carregar salva o perfil anterior pelo caminho existente, troca modelo e perfil, restaura aparência/equipamento e usa os dados daquele perfil. Dinheiro de outro perfil não é copiado. O local atual é mantido; não se adicionou um spawn/casa automático. A volta ao protagonista registrado, quando disponível, continua na lista. Se uma aplicação já mudou o modelo e foi interrompida, o registro ApplyPending permanece recuperável.

As ações passam pela fila de menus para não reconstruir itens durante ObjectPool.Process. Confirmações começam em Cancelar, mantêm prova de ator/modelo/perfil e revalidam os arquivos antes do carregamento. Após trocar o ped, o menu de preparação é vinculado ao novo ator, evitando cancelar por referência ao anterior.

## Referência PSRP

Revisadas as rotinas MultiplayerCharacter.RefreshCharacterNames, CreatorMenu e LoadCharacterToPed da DLL fornecida, por análise estática da fonte decompilada. A referência oferece lista e criação/carregamento; a abertura segura na entrada e o controle F11 são implementação própria. Não se executa PSRP.dll junto do RPCore. O sistema não importa automaticamente a câmera, dinheiro inicial, teleporte ou remoção de veículos do criador PSRP.

## Teste no GTA

1. Com personagem RPCore salvo, fechar e reabrir o GTA. Após liberar controle e ficar parado a pé, conferir lista e nome; cancelar deve continuar sem troca ou reaparecimento.
2. Selecionar um personagem, conferir confirmação, roupas, arma/munição, inventário, carteira e banco. Salvar, fechar e repetir; os saldos não podem duplicar ou migrar entre perfis.
3. Criar outro personagem com nome próprio e outro modelo; confirmar perfil separado com saldo zero. Personalizar no barbeiro/roupas, salvar e testar novo carregamento.
4. Desativar no F11, fechar e reabrir: seleção não deve aparecer. Abrir manualmente pelo F10. Reativar, reiniciar e conferir retorno da abertura automática.
5. Se iniciar dentro de veículo/em missão/procurado, confirmar que o menu aguarda contexto seguro. Testar Escape/voltar, navegação, interrupção de carregamento e F4 sem menu preso.
6. Com backup dos saves, verificar que registro inválido aparece protegido e que perfis legados permanecem acessíveis. Não apagar saves reais para simular primeiro acesso; use uma instalação/pasta de teste separada.

Build e testes no ambiente cloud não executam o GTA. As correções0.102.1/0.102.2 e os assets instalados continuam incluídos. O usuário confirmou portas/F10 e estabilidade inicial da0.102.2; isso não prova ausência de todos os crashes nativos nem substitui o teste de fechamento completo do banco.
