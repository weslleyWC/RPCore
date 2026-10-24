# Validação — RPCore 0.17.0

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI SHVDN3 2.2: zero erros/avisos. DLL SHA256 `aa15fe402d613e60be3b6adb7bd64473ff8c2c99dec5fb1ce1efdf7dc545e46a`.

614 testes aprovados, zero falhas/ignorados: 567 anteriores e 47 novos. Cobrem canal/título/início, sorteios/eventos/limites/audiência, crédito e estatísticas em checkpoint, replay/rollback/ordem, seis blocos/um término, pausa/carregamento, retry, encerramento, limites/unknowns/sistemas anteriores/licença de piloto. Verificação de catálogo atualizada para 12 ícones originais do telefone. Relatórios DataTests.trx/DataTests-0.17.0.trx. Não executam GTA/entrada de telefone/renderização.

Streamer é campo aditivo em Wallet.json, com canal/título/totais/último resumo/recibos recentes limitados a 512. Início sem taxa; blocos completos geram banco e stats no mesmo checkpoint. Falha restaura referência/saldo e mantém mesmo sorteio para retry. Encerramento preserva pagos e não paga fração. Transmissão não retoma após reload/perfil; resumo indica interrupção se não houve fechamento. Phone.json/histórico é secundário e não controla o crédito. Inventário, licenças, bolsas e estoques anteriores não são convertidos/consumidos.

Auditoria PSRP-SYSTEMS-AUDIT-0.17.0.json: DLL atual/baseline 0.16.0, 75 fontes; _JOB__LiveStreamer e CustomPhone mostram app/controles/chat/contextos/pagamentos bancários. TwitchOverlay e TwitchIcon presentes no YTD fornecido; overlay inspecionado/decodificado, alfa (21,233)–(346,661). RPCore usa código próprio, oito contextos gerais, economia limitada e transmissão finita. Contextos detalhados/economia da referência continuam para fase 9. Sem conexão externa, gravação, transplante de fonte ou edição de OIV/YTD/RPF/DLC.

Inspeção nativa: observa contexto/ShiftGate sem adquirir/liberar; não modifica trabalhos, IA, polícia, personagens/veículos. End continua exclusivo dos trabalhos. App novo é o 12º, preservando os 11 índices anteriores. Painel original recortado/escala/zona segura com fallback; oculta em menus/cutscenes/pausa. Tempo só em gameplay elegível e sem catch-up de carregamento. F2 e F11 preservados. Fome/sede apenas lidas para contexto; problemas dessas mecânicas não foram alterados nesta fase.

Não executado no GTA nesta nuvem: testar app/canal/título, painel/chat/eventos, seis pagamentos/encerramento, live durante trabalho, pausa, falha/reload/perfis e regressão de telefone/inventário/trabalhos/teleporte. Guia FASE-5.5-0.17.0.md. Próxima atividade: arrancada. Fases 8 e 9 mantidas como solicitado.

Instalador: duas execuções via PowerShell 7/Linux em diretório GTA simulado, sem gameplay. Os 97 arquivos existentes ficaram byte a byte iguais, incluindo saves/configurações/F2 e dados anteriores de streamer. Streamer.json acrescentado na primeira execução e personalização mantida na segunda; backup da DLL anterior e nova DLL idêntica ao build. Relatório installer-validation-0.17.0.json.
