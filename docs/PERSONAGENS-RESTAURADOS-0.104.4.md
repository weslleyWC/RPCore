# Personagens restaurados para0.102.2 — 0.104.4

A pedido do usuário, reverte **somente o sistema de personagens** para o comportamento da0.102.2. O restante continua atualizado: armazenamento/porta-malas/apartamento e grade/menu da0.104, banco, celular F2, inventário/armas/peso, F10/F11, portas/diagnóstico e demais funcionalidades.

## O que foi restaurado

CharacterSystem.cs é byte a byte o arquivo da0.102.2, commit6bae73d. A função Main.SwitchCharacter também é exatamente a daquela versão. Criação, lista/manual F10, confirmações, carregamento, aparência/equipamento, voltar ao protagonista e registro pendente voltam à implementação anterior. CharacterData, CharacterBodyRuntime, Profile, Cfg e Wallet continuam os mesmos da base confirmada. Não foi acrescentado novo fluxo alternativo ao rollback.

Saem a seleção automática ao entrar, o botão de ativá-la no F11, a espera inicial de30segundos e os caminhos de ped candidato/CREATE_PED/CHANGE_PLAYER_PED/registro de decor ou memória. Helpers e testes exclusivos desses caminhos removidos deixam de ser compilados. A chave CharacterSelectionOnStart já existente no Interface.ini do usuário é preservada pelo instalador, mas fica sem efeito; não precisa editar arquivos para desativar o recurso removido.

O carregamento manual volta a usar a rotina original de troca de modelo da0.102.2. Os decodificadores de etapas históricas em RuntimeTrace permanecem para interpretar registros já existentes, sem executar o fluxo removido. Não há varredura ou apagamento de peds do mundo para tentar limpar duplicatas.

## Instalar e testar

1. **Fechar completamente o GTA**, não apenas reiniciar plugins pelo F4. Isso descarrega os peds temporários da sessão com duplicatas. Instalar a0.104.4 pelo mesmo instalador de antes, sem apagar saves ou reinstalar OIV/RPF.
2. Entrar normalmente: não deve abrir lista automática nem haver a opção de seleção inicial no F11.
3. Usar **F10 → Personagens e perfis → Personagens / criar e carregar**, como antes. Carregar o personagem existente e conferir rosto, roupa, inventário, arma/munição, carteira/banco e volta ao protagonista.
4. Conferir porta-malas/depósito/apartamento e transferência por mouse da0.104: devem continuar atualizados. Conferir F2/J/F11 e configurações personalizadas.

A atualização não apaga/recria personagem, não muda saldos, não converte slots e não limpa ApplyPending automaticamente. Mantém a recuperação manual que já existia na0.102.2. Se algo recusar carregar, enviar o RPCore.log da tentativa; não apagar backups para forçar. O rollback restaura a versão solicitada, não representa prova de ausência de qualquer crash nativo no GTA.

## Validação

3974 testes ativos aprovados e Release net48 sem erros/avisos. A contagem anterior4047 incluía73casos dos recursos de personagens agora retirados (entrada/espera/handoff/registry); esses testes foram removidos junto com os recursos, não desativados para ocultar falha. Mantidos os casos da base0.102.2 e os34 do armazenamento0.104.

Comparação exata de CharacterSystem e Main.SwitchCharacter com6bae73d e comparação dos demais arquivos fonte/config com254817d registradas em CHARACTER-ROLLBACK-0.104.4.json. Instalador aplicado duas vezes sobre0.104.3 com dados byte-idênticos, DLL anterior em backup e nova confere. Relatório installer-validation-0.104.4.json. Assets/configs anteriores e áudio comparados. Gameplay de troca e desaparecimento das duplicatas após fechar/reabrir ainda dependem do teste GTA.
