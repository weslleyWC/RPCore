# Validação0.100.0 — roupas/barbearia/acabamento/moradia

3845 testes aprovados (3811 anteriores +34), Release net48 sem erros/avisos. DataTests-0.100.0.trx. Sem execução GTA.

Os34 casos novos cobrem despacho após ativação/uma única execução, botão mantido entre páginas, abertura com botão já pressionado, menu oculto/contexto inválido/ator/modelo/perfil/timeout, troca de página, segunda ação recusada, exceção sem replay, wrap de GameTime e dono inválido;14 pontos de roupa da referência e configurações inválidas; snapshot completo contra confirmação obsoleta/metadados, igualdade visual independente de nome/id/ordem, bloqueio de snapshot nas prévias e sentinela de overlay.

Integração compilada/revisada: lojas e GPS integrados aos sistemas existentes; biblioteca mantida, editor/renomeação/substituição/exclusão com validação de contexto/registro; menu compartilhado com ações fora do callback LemonUI; cabelo/roupa cancelam sem sobrescrever alterações externas; Character.json não captura prévia. Acabamento reorganizado e resumo antes de pagar; pipeline pago retomável anterior preservado. Compra de moradia confirmada e propriedade revalidada durante carregamento. CanUseInterior continua independente da flag ModalOpen para não fechar o inventário privado.

Guia SERVICOS-PESSOAIS-0.100.0.md descreve acesso, diferenças e testes GTA necessários. Não foi acrescentada câmera dedicada de roupa/barbearia; criação completa de personagens, DMV e seguro ainda não receberam esta revisão de menus. Não há afirmação de paridade total, todos os bugs resolvidos ou fase8 encerrada.

Instalador: resultado da simulação sobre0.99.0 registrado em installer-validation-0.100.0.json após duas execuções:208 arquivos existentes preservados byte a byte. Arquivos existentes comparados byte a byte, incluindo roupas/barbearia/acabamento/moradia e suas pendências/metadata, configurações de lojas, inventário, saldos e RPF externo. Execução nativa dentro do GTA permanece pendente.
