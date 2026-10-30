# Validação0.102.2 — portas/F10 e investigação de crashes

3940 testes aprovados (3933 anteriores +7). Release net48 sem erros/avisos. DataTests-0.102.2.trx. Sem execução GTA.

Falhas confirmadas pelos logs: Bool boxed em OutputArgument na rotina de portas e SelectedIndex em menu vazio no F10. Corrigidos buffers tipados8bytes, integração com porta existente registrada e guarda de seleção vazia. Encontrados/corrigidos também handles boxed do cleanup de props próprios. Em direção, remove polling de estados de menu fechado e bind automático de carros; o helper de menu evita input/prova do ator enquanto oculto.

Quatro casos novos exercitam OutputArgument.Init/Marshal da DLL Enhanced real no host de testes, sem chamadas GTA; três exercitam arquivo mapeado/etapa anterior/versão/aborted/dispose/arquivo inválido e preservação de saves. As exceções originais são evidência do log e do código da API. Não se executou construtor antigo dentro do GTA nem jogo/perseguição. Guardas F10 e caminhos nativos foram compilados/revisados; o teste de dados não comprova travessia física de portas ou cura de crash nativo.

RuntimeState.bin de8KB registra contexto em memória mapeada, sem JSON/flush síncrono por frame, e startup produz RuntimeLastSession.json. Esse contexto não é prova do causador do crash. Falha do registro é isolada do mod. Os logs fornecidos não incluem causa fatal do fechamento; investigação continua se o problema nativo persistir. Guia STABILIDADE-0.102.2.md contém roteiro/coleta. Hashes/resumo sanitizado em ANALISE-LOGS-0.102.2.json; logs pessoais não publicados.

Instalador duas vezes sobre0.102.1:229 arquivos existentes preservados byte a byte, incluindo RuntimeState.bin/RuntimeLastSession.json e backup, bancos/checkpoints válidos/legados/inválidos, saves/configs anteriores. DLL nova confere, anterior em backup e RPF externo intocado. Relatório installer-validation-0.102.2.json.111 payloads JSON/INI e áudio anteriores comparados; fonte/manifestos/audit/DLL/TRX verificados.
