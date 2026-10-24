# Validação — RPCore 0.24.0

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI 2.2: zero erros/avisos. DLL SHA256 `8b23ab623e21e2c931b618ead093f67578d6276b92353d486698c906ce2fa5c6`.

898 testes aprovados, zero falhas/ignorados: 860 anteriores e 38 novos. Cobrem risco no vencimento/snapshot/reload/rollback, contrato antigo adimplente, ausência de crédito da parcela perdida, operação/identidades/carga, dano mínimo, chegada/retorno/recovery, revenda sem duplicar custo, atraso tardio com parcelas recebidas, capacidade/limites, replay e campos desconhecidos. Relatórios DataTests.trx/DataTests-0.24.0.trx. Não executam GTA.

Estado aditivo: Pdm.Repo, FinanceWriteOff e campos DefaultPercent/Defaulted/Repossessed/DeliveredHandle em Loans. Campos ausentes mantêm contratos anteriores com risco zero. Novas vendas nativas usam risco da configuração de retomada; helper anterior de abertura regular continua compatível. Decisão estável por ID/parcela, sem novo resultado ao recarregar ou repetir gravação. Em atraso não recebe mais parcelas automaticamente; retomado é encerrado, com recebido anterior preservado.

Retorno ao estoque atômico: copia bem com condição salva, fecha recebível em aberto, registra FinanceWriteOff nominal e reverte custo original de SoldCost para estoque. Não paga bônus nem cria receita artificial, não desconta saldo nominal de caixa, não conta o custo duas vezes ao revender. Replay não devolve dois carros. Capacidade/contador inválidos conservam chegada para retry. Exceções restauram snapshot.

Integração física: Packer/TRFlat com busca de vaga e engate, alvo próprio do contrato em 11 locais da referência, carga vazia assistida, retorno e tempo de recebimento. Rigs/veículo reservados para impedir garagem pessoal. Identidade por handle/modelo/placa; alvo da entrega original ainda no mundo bloqueia nova representação. Cleanup não apaga veículos ocupados/movendo/ligados a trator alheio. Antes da chegada, cancelar mantém atraso/danos; após chegada, recuperação conclui estoque. Estado financeiro e limpeza física seguem checkpoints separados para evitar duplicações.

Contratos em atraso não avançam relógio; demais parcelas/pessoal continuam. Gestão de estoque/pessoal/coleta de carros/valores bloqueia operação conflitante. Mantém arquivos padrão anteriores de Pdm/Finance/Logistics/Staff/Safe; novo PdmRepo.json contém risco e locais. Sem alterar recursos OpenIV, atalhos, polícia global ou Transport pessoal.

Auditoria PSRP-SYSTEMS-AUDIT-0.24.0.json: 75 fontes indexadas, baseline 0.23.0 e identidade da DLL atual. NormalRepoLocations com 11 coordenadas/headings, HandleRepoEvent/onNormalRepo, Packer/TRFlat/offsetFront e retorno conferidos. Variante estacionada implementada com código próprio. Risco/calendário/contabilidade são regras próprias; não declara devedor dirigindo/confronto/negociação ou toda a PDM concluídos. Locadora e outros subsistemas permanecem; fases 8/9 mantidas.

Roteiro GTA: FASE-6.1F-0.24.0.md, inclusive DefaultPercent=100 para testar novo financiamento sem depender do risco padrão. Testar endereço/spawn/engate/carga/altura/retorno, único bem no estoque, reparo/revenda/custo, cancelamento/reload/ocupantes e regressão de contratos antigos, coletas/pessoal/garagem/F2/F11. Jogo não executado nesta nuvem. Pedido de avançar não é comprovação de teste detalhado anterior.

Instalador executado duas vezes via PowerShell 7/Linux sobre instalação simulada da 0.23.0: 104 arquivos existentes preservados byte a byte, incluindo saves/configurações/F2 e empresa. PdmRepo.json acrescentado só na primeira execução; personalização conservada na segunda. Backup da DLL anterior e DLL instalada idêntica ao build. Relatório installer-validation-0.24.0.json. Não é teste de gameplay.
