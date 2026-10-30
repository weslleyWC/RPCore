# Troca de personagem com ped preparado — 0.104.2

## Evidência desta ocorrência

A espera da0.104.1 não resolveu o caso. O novo RPCore.log chega a `character transition: SET_PLAYER_MODEL -1667301416` às14:12:29, sem `model returned`. RuntimeState.bin tem Version0.104.1.0, Stage6020 e AbortedHookReached=false. Os dois registros concordam: a chamada nativa de substituir Franklin pelo freemode feminino não retornou antes do encerramento. Roupas/rosto/inventário ainda não tinham sido aplicados. O número negativo é a representação signed do hash válido de mp_f_freemode_01, não um erro por si.

Isso localiza a fronteira do travamento. Não fornece stacktrace interna do GTA para explicar por que esse native travou, nem prova problema em determinado RPF, outro plugin ou arquivo de personagem. Não se aumenta novamente a espera como se resolvesse esse caso. Os arquivos originais não são publicados; hashes/evidência sanitizada em ANALISE-LOGS-0.104.2.json.

## Alteração do fluxo

O caminho de carregar/criar/voltar personagens deixa de chamar SET_PLAYER_MODEL. O modelo continua solicitado e validado, mas o RPCore cria um ped local separado com CREATE_PED, identifica-o por handle/modelo/token de decor e o mantém brevemente invisível, imóvel, sem colisão e invulnerável enquanto aguarda uma etapa posterior. As flags pertencem somente a esse ped temporário; não são aplicadas a Franklin.

Após pelo menos500ms, revalida candidato/origem/contexto e conserva a sequência de salvar perfil anterior, registrar aparência pendente e gravar seleção. Transfere o controle ao candidato com CHANGE_PLAYER_PED, verifica o handle do jogador pelo native GET_PLAYER_PED, confirma o modelo e libera as flags temporárias. Só então recarrega o perfil e segue o preparo de corpo/rosto já existente. Não há fallback automático para SET_PLAYER_MODEL se a transferência falhar.

A API pública de CHANGE_PLAYER_PED foi conferida na declaração nativa PLAYER/ChangePlayerPed (0x048189FAC643DEEE). Os dois parâmetros booleanos usam false; a documentação disponível não descreve seus detalhes e não se atribui semântica inventada a eles. Referência PSRP.MultiplayerCharacter usa Player.ChangeModel; o SDK Enhanced inspecionado mostra que esse wrapper volta ao mesmo SET_PLAYER_MODEL que não retornou. Por isso só trocar a chamada pelo nome do wrapper não seria uma correção diferente. Este fluxo alternativo é código próprio e ainda precisa do teste nativo no GTA.

## Cancelamento e dados

Antes de transferir, cancelar, perder contexto ou exceder prazo só remove o candidato identificado e ainda não controlado. A remoção exige handle, modelo e token iguais; o handle do jogador é consultado diretamente e nunca é alvo de DELETE_PED. Após confirmar controle, o lease não é descartável, mesmo que a aparência precise ser retomada. Entidades do GTA/outros scripts não são apagadas por esse fluxo; a antiga entidade de Franklin pode permanecer no mundo sob o ciclo normal do jogo. O RPCore não varre/apaga protagonistas antigos para esconder a troca.

Todos os slots, nomes, sexo/modelo, rosto, roupas, inventário, armas, saldos e registros pendentes existentes continuam nos mesmos arquivos. Não é necessário recriar a personagem feminina nem trocar para masculino. A área onde ela aparecia não é apagada/resetada. O carregamento continua usando o local atual, como antes; não foi acrescentada persistência de posição ou spawn automático nesta correção.

A abertura opcional F11, proteções de entrada/manual0.104.1 e armazenamento0.104 seguem incluídos. A carência existente de entrada continua, mas a mudança principal deste hotfix é remover a chamada identificada do carregamento, e não adicionar outra demora.

## Diagnóstico e teste

Novas fases:6030create-ped (criação/preparo inicial),6031stage-ped,6032change-player-ped e6033handoff-returned. Os registros de salvamento/preparo/aparência continuam. Etapa é contexto de execução, não stacktrace de causa interna. Se outra chamada nativa falhar, o registro deve distinguir preparação, transferência e aplicação.

1. Fechar o GTA e instalar só a0.104.2. Não reinstalar OIV/RPF, não apagar saves/pendências e não recriar a personagem.
2. Entrar, deixar terminar a estabilização e carregar a mesma personagem feminina. Conferir controle, visibilidade, movimento, colisão, rosto, roupa, inventário e dinheiro/banco. A preparação temporária não pode deixar o jogador invisível, congelado ou invulnerável.
3. Testar cancelar antes da transferência: Franklin deve continuar controlado, sem ped temporário preso no local. Depois de um carregamento bem-sucedido, testar a volta ao protagonista pelo menu quando disponível, e conferir perfis/saldos separados.
4. Se aparecer recusa/interrupção, conservar os arquivos e enviar RPCore.log. Se crashar novamente, enviar RuntimeState.bin e RPCore.log antes de reabrir. Não repetir vários crashes para coleta: uma tentativa já registra o ponto.

4029 testes aprovados, Release net48 sem erros/avisos.18 casos novos exercitam prova de propriedade, impedimento de excluir jogador, confirmação única, falhas/pendência preservada e nomes de fases. Nenhum teste cloud executa CREATE_PED/CHANGE_PLAYER_PED ou reproduz o GTA. Esta entrega oferece uma implementação alternativa revisada; o crash só pode ser considerado resolvido após o teste real do usuário.
