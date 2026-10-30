# Correção do bloqueio de identificação do ped — 0.104.3

RPCore.log da0.104.2 confirma quatro tentativas que chegam a create replacement ped e falham com `System.InvalidOperationException: Candidate ownership tag unavailable`, em CharacterHandoffRuntime.Create → CharacterSystem.Swap → Tick. A exceção acontece no teste de registro de decor, antes do CREATE_PED. Não é aviso para esperar mais, problema demonstrado no save ou falha de roupa/rosto; a transferência de controle não chegou a ser executada nessas tentativas.

A0.104.2 exigia registrar uma marca global de entidade após o GTA já estar carregado. Esse registro não foi aceito no ambiente do usuário. O log não distingue se o registro estava bloqueado, cheio ou com conflito de tipo; por isso não se declara uma causa interna específica nem se desbloqueia/escreve memória do registro global do GTA.

## Correção

CharacterHandoffRuntime não usa mais DECOR_REGISTER/SET/GET/EXIST nem depende da disponibilidade de decor. Logo após CREATE_PED, valida que o resultado é um ped novo, diferente do jogador, do modelo esperado e com endereço de entidade válido. Registra apenas o objeto de lease daquela operação num registro gerenciado privado da sessão, associado ao handle, modelo e endereço da entidade no pool. O token continua local à operação. Esse registro não adota entidades do mundo por nome/tag, não é salvo em JSON e não persiste entre sessões.

Cada uso revalida a entidade e seu endereço. Copiar campos do lease ou obter o mesmo handle/modelo com outro endereço não autoriza o acesso; uma cópia de lease ou lease de outra sessão não pertence ao registro. A limpeza consulta GET_PLAYER_PED: não remove o jogador atual, mesmo antes da flag de confirmação. Após transferência, o candidato confirmado nunca é descartado. A liberação das flags exige confirmação e handle/modelo do jogador atual, mesmo se o motor mudar seu endereço interno durante a transferência; isso não autoriza apagar essa entidade. As flags temporárias são liberadas e o registro é esquecido. Em cancelamento, só o ped temporário comprovado pode ser removido; se a remoção for recusada, libera as flags desse mesmo ped e o entrega ao ciclo normal do motor. Nenhuma varredura de outros peds ou remoção de Franklin foi acrescentada.

O restante do caminho0.104.2 continua: ped preparado → CHANGE_PLAYER_PED → confirmação → aplicação de corpo/rosto e dados existentes. SET_PLAYER_MODEL continua ausente desse carregamento; não há fallback para a chamada que travou na0.104.1. A correção atual remove o bloqueio confirmado antes da criação. Ainda não houve teste nativo bem-sucedido da transferência no GTA do usuário; não se declara o crash anterior resolvido por este log.

## Saves e instalação

Instale a0.104.3 com o GTA fechado. Inclui armazenamento0.104, proteções de entrada0.104.1 e caminho de transferência0.104.2. Os mesmos personagens, modelo feminino, roupas, inventário, saldos, backups e flags ApplyPending continuam nos arquivos atuais. Não apague/recrie personagem e não reinstale OIV/RPF para corrigir este erro.

Carregue a personagem existente após a entrada estabilizar. A mensagem Candidate ownership tag unavailable não deve mais ocorrer por esse código, que não chama decor. Confira visibilidade, controle/movimento/colisão, roupa/rosto, itens e banco/carteira. Se houver recusa, envie RPCore.log da tentativa. Se ocorrer crash, envie também RuntimeState.bin antes de reabrir. Não repetir várias tentativas para coleta; uma falha já registra a etapa.

4047 testes aprovados,18 novos para registro sem decor, identidade/endereço/lease de outra sessão, invalidação, confirmação e proteção do jogador. Release net48 sem erros/avisos. Os testes exercitam a política gerenciada de propriedade, não CREATE_PED/CHANGE_PLAYER_PED nem gameplay. Validação GTA permanece necessária. Guia anterior PERSONAGEM-HANDOFF-0.104.2.md descreve o caminho geral; a exigência de decor daquele texto é substituída por este registro de sessão na0.104.3.
