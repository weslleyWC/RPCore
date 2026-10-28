# Validação 0.95.0 — inventário: limpeza, roda e descarte recuperável

3639 testes aprovados: 3602 da versão anterior e 37 novos de descarte/recuperação e política de input. Release net48 compilada sem erros ou avisos. Resultado em DataTests-0.95.0.trx. Não houve execução do GTA neste ambiente.

Cobertura nova: gravação única com quantidades/posição/interior/metadados; falha de gravação de descarte e coleta mantendo o estado anterior; repetição sem duplicação; quantidade/coordenadas/interior inválidos; limite de 128 pilhas sem expirar antigas; distância 3D e interior; coleta parcial conforme peso e capacidade; overflow; peso/capacidade inválidos; perfil separado; armas no chão preservando acessórios/clip/munição e bloqueando reimportação; dados corrompidos conservados. Política da roda sem dependência do estado de armaria/atividade e preservando contextos nativos protegidos.

Revisão estática: limpeza conserva identificador/modelo/token da entidade própria até a confirmação de remoção; desanexa antes de DELETE_OBJECT e repete a cada250ms. Somente decorador RPCoreInvUse é recuperado após reload; sem remoção por modelo/proximidade e sem limpar tarefas globais. Bloqueio da roda no final do Tick principal fora do try da sincronização de armas e do conjunto de sistemas com falha. Delete/LB chamam descarte recuperável; R/Shift+R só no inventário pessoal, perto da pilha e em contexto seguro.

GroundItems é opcional no Inventory.json existente, por perfil. Clone/gravação antes da atribuição da operação; sem arquivo financeiro separado. Registro inválido recusa edição; desconhecidos conservados. Novos marcadores não geram entidades físicas nem recompensas. Sem alterações de configs JSON/INI, preços, saves instalados, trabalhos, modelos RPF/OIV ou áudio. Testes anteriores de mouse, peso, munição, receitas e consumos continuam aprovados.

Guia CORRECOES-INVENTARIO-0.95.0.md contém validação manual necessária: entidade e animação em todos os consumíveis, interrupções/reload, roda após atividades/menus/perfil, controles protegidos, posição física do marcador, coleta e persistência. A revisão da fonte PSRP é estática e não afirma paridade total.

Instalador PowerShell7/Linux: duas atualizações simuladas sobre0.94.0 preservaram199 arquivos byte a byte, incluindo saves com GroundItems/metadados, armaria e episódios de sono, configs personalizadas e receitas. Mod RPF externo intocado; DLL nova confere com a compilada e anterior fica no backup. Relatório installer-validation-0.95.0.json.
