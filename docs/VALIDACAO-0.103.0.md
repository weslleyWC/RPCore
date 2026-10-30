# Validação0.103.0 — seleção de personagens

3954 testes aprovados (3940 anteriores +14), nenhum ignorado/falhou. Release net48 sem erros/avisos. Relatório DataTests-0.103.0.trx. Os quatro casos Enhanced da versão anterior continuam usando a API gerenciada real, sem natives GTA.

Novos casos verificam primeiro acesso sem escrita, perfis legados sem cartão, ordenação e carregamento pendente, registros ausentes/corrompidos/identidade divergente protegidos, falha isolada,65 cartões existentes sem truncar na leitura, espera segura reiniciada quando inseguro/relógio inválido e abertura uma vez/consumo manual ou desabilitado. Foi ajustado um fixture que tentava criar cartão já pendente, proibido pelo fluxo existente; o teste agora cria válido e grava estado pendente como um carregamento interrompido real.

Integração compilada/revisada: F11 persiste preferência em Interface.ini, padrão true para arquivos antigos sem chave; menu manual F10 permanece. Filas não alteram itens durante Process, confirmam/cancelam e revalidam contexto/arquivo. Após SET_PLAYER_MODEL, rebind do menu ao novo ator/perfil mantém preparo de rosto. Nenhuma transação nova de banco/dinheiro, teleporte inicial ou reset de save. As correções0.102.1/0.102.2 permanecem.

Instalador executado duas vezes sobre0.102.2; 238 arquivos existentes; relatório installer-validation-0.103.0.json registra os dados preservados byte a byte, incluindo cartões válidos/pendentes/inválidos/backups, carteiras e configuração desativada. DLL anterior em backup, nova confere e RPF externo intocado. Comparação dos111 payloads JSON/INI e áudio anteriores, CRC/manifestos/DLL/fonte/audit/TRX concluída no empacotamento.

O usuário confirmou estabilidade inicial, portas de roupas e F10 na0.102.2. Isso não confirma todas as causas de crash nem o carregamento novo. Não há GTA executável neste ambiente. Testar no jogo seleção/criação, cancelamento/retomada, dinheiro/roupa/inventário entre perfis, opção desligada após reiniciar, contexto de veículo/atividade e F4 conforme PERSONAGENS-INICIO-0.103.0.md. Fases8/9 continuam.
