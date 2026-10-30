# Validação0.104.1 — entrada e troca de personagem

4011 testes aprovados (3988 anteriores +23), nenhum ignorado/falhou. Release net48 sem erros/avisos. Relatório DataTests-0.104.1.trx. Não executa GTA ou SET_PLAYER_MODEL.

Novos casos: carência inicial30s, sequência RPCore→Franklin→casa relatada,10s após nova indisponibilidade, mudança de ator/modelo/perfil, teleporte e movimento normal, relógio inválido sem avanço offline, identidade/posição ausentes, pausa sem avanço, integração com gate de abertura única e cinco nomes de fases lidos do arquivo mapeado da sessão anterior. Integração de controle/colisão nativa, revalidação após SaveAll e aplicação diferida compilada/revisada; não são testes de crash nativo.

Logs examinados confirmam troca de perfil às13:37:46 logo após início0.103. Não há erro fatal/nativo novo. RuntimeLastSession enviado é0.102.2 anterior, não a etapa do crash da0.103. Não se atribuiu culpa a outro script nem se publicou os logs originais. Evidência sanitizada/hashes em ANALISE-LOGS-0.104.1.json. Limitação e roteiro em PERSONAGEM-ESTABILIDADE-0.104.1.md.

Instalador executado duas vezes sobre0.104.0, com preservação byte a byte de 243 arquivos de dados/config existentes, incluindo ActiveProfile, retorno pendente do protagonista/backup, cartões pendentes/inválidos/carteiras e armazenamento personalizado. Relatório installer-validation-0.104.1.json; DLL nova confere, anterior em backup e RPF externo de teste intacto.111 payloads JSON/INI e áudio anteriores comparados, CRC/manifestos/DLL/fonte/audit/TRX conferidos no pacote final.

As correções de banco/portas/estabilidade e o armazenamento0.104 permanecem. O hotfix protege riscos encontrados no fluxo de entrada e acrescenta diagnóstico granular, mas o motivo fatal exato continua sem stacktrace. Testar o caso real antes de retomar refinamento normal.
