# Validação — RPCore 0.12.2

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI SHVDN3 2.2: zero erros e zero avisos. DLL SHA256 `e012cfcce89ac196cb610731be1c85087868aeffa5f90438a50560458e4ce823`.

396 testes de regressão aprovados, zero falhas/ignorados. Evidências DataTests.trx/DataTests-0.12.2.trx. Não foram adicionados testes que pretendam executar nativos de teleporte/streaming fora do GTA. A nova ação foi inspecionada e compilada; teste real em TELEPORTE-0.12.2.md.

F11: opção no topo. Carrega terreno por ticks, verifica espaço/chão/água, congela temporariamente o sujeito, move apenas depois de posição válida e confirma o chão novamente antes de liberar. Estado anterior de congelamento/foco temporário são restaurados no fim/cancelamento/troca de perfil/abort. Timeout cancela; se já moveu, restaura origem. A pé move o jogador; carro/moto/ônibus parado como motorista move o veículo com ocupantes. Exclui aeronaves, barcos/trens e carreta engatada desta ação. Não executa tarefas de saída nem apaga NPCs/veículos de ambiente. Não altera procurado/qualificação/recibos/dinheiro.

Auditoria PSRP-SYSTEMS-AUDIT-0.12.2.json atualizada para o binário atual e baseline 0.12.1. Coordenadas/configurações de trabalhos iguais à versão anterior, incluindo a base pública própria Grapeseed e os seis destinos de piloto mantidos da referência. Nenhum OIV/YTD/RPF/DLC/dlclist foi alterado. Não houve transplante de fonte decompilado.

O piloto ainda aguarda teste completo no GTA após a correção de acesso. A nova ferramenta foi solicitada para acelerar a chegada aos pontos de contratação; não altera as exigências de voo e entrega dos contratos. Saves/configurações/F2 permanecem iguais. Teste nativo necessário: distante a pé, veículo como motorista, sem waypoint, água/ocupação, cancelamento e restauração de congelamento/streaming.

Instalador executado duas vezes com PowerShell 7/Linux sobre GTA simulado com pacote 0.12.1: 93 arquivos existentes preservados byte a byte, incluindo saves, licença, Pilot.json, INIs e F2; backup da DLL anterior e DLL instalada igual ao build. Evidência installer-validation-0.12.2.json. Não executa GTA/Windows real.
