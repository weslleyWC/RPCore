# Validação0.104.3 — identificação do ped da sessão

4047 testes aprovados (4029 anteriores +18), nenhum falhou/ignorado. Release net48 sem erros/avisos. DataTests-0.104.3.trx. Não executa GTA, CREATE_PED ou CHANGE_PLAYER_PED.

Log fornecido confirma Candidate ownership tag unavailable em quatro tentativas0.104.2, durante decor availability em CharacterHandoffRuntime.Create, antes do CREATE_PED. Esperar não resolve essa exceção. Não há prova de save inválido; a transferência nativa ainda não ocorreu nessas tentativas. Evidência/hashes em ANALISE-LOGS-0.104.3.json, sem publicar original/caminhos pessoais.

Runtime de personagens não chama decor APIs nem exige registro global. Registro local valida referência exata do lease, handle, modelo e endereço do ped recém-criado; não adota entidades antigas/copias. Remoção continua condicionada a identidade exata e a não ser jogador/lease confirmado. Flags de preparo só liberadas no jogador confirmado por handle/modelo, sem autorizar sua exclusão; endereço pode mudar legitimamente no handoff. Falha/cancelamento esquece prova e libera apenas candidato comprovado se remoção recusada. Não há desbloqueio/escrita da memória do registro global do GTA.

Novos18casos testam registro sem decor, handle/modelo/endereço alterados, objeto copiado/registro de outra sessão, invalidação, dados insuficientes, jogador atual/confirmado excluído da limpeza, restauração só no jogador confirmado e leases independentes. C# runtime/assinaturas Enhanced compilados/revisados; chamadas nativas não testadas no host.

Instalador duas vezes sobre0.104.2 preserva byte a byte 243 arquivos existentes, incluindo personagem feminina pendente, ActiveProfile/retorno/backups, carteiras e armazenamento. Relatório installer-validation-0.104.3.json; DLL nova confere, anterior em backup e RPF de teste intocado.111 payloads JSON/INI e áudio anteriores comparados; CRC/manifestos/fonte/DLL/TRX/audit conferidos na verificação final.

Carregar a mesma personagem no GTA e conferir controle/movimento/colisão/visibilidade, aparência, itens e saldos. Guia PERSONAGEM-REGISTRO-0.104.3.md. A correção elimina o bloqueio identificado de decor; não declara a transferência0.104.2 ou o crash0.104.1 resolvidos sem teste nativo. Refinamento normal permanece pausado.
