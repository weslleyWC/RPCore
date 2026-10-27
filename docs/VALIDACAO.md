# Validação 0.92.0 — inventário de armas e munição

3482 testes aprovados (3449 anteriores +33 novos), Release net48 sem erros/avisos. DataTests-0.92.0.trx. PSRP.dll analisada estaticamente; nenhuma execução GTA neste ambiente.

Cobertura: migração de armas/munição sem soma duplicada de pools, confirmação depois de gravar, falhas preservam objeto anterior, delta real de munição com rajada/reload/coleta/limites, aliases de metadados isolados, acessórios/containers preservados em transferência, pilhas de granadas e última unidade, estados inválidos protegidos, contexto de apartamento permitido e outros trabalhos bloqueados.

Integração compilada: InventorySystem parcial, registro de definições sem alterar configurações existentes, ícones PSRP_Inventory, equipar/guardar sem Give250 ou RemoveAll, munição nativa aplicada depois da confirmação, preferência de roda em F11GameSettings, guardas de veículos/perfis/missões/shared gate e empréstimos temporários. Ações nativas/JSON não são uma transação crash-atomic; possível perda da observação do último frame em crash. Ferramentas utilitárias continuam nativas e não há loja de armas paralela. Roteiro e diferenças: REFINAMENTO-ARMAS-0.92.0.md.

Instalador PowerShell7/Linux em GTA simulado: duas atualizações sobre0.91.0 preservaram197 arquivos byte por byte, incluindo estados de armamento legado/importado/guardado/arremessável/inválido, acessórios e metadados, needs/sono/recibos, slots antigos/containers, saldos/preferências e arquivo de outro mod. DLL final coincide com o build e DLL antiga foi salva no backup. Relatório installer-validation-0.92.0.json. Os109 JSON/INI do payload anterior e áudios originais devem coincidir byte por byte na conferência final do pacote.
