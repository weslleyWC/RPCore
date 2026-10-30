# Validação0.100.1 — acesso físico às lojas de roupas

3855 testes aprovados (3845 anteriores +10), Release net48 sem erros/avisos. DataTests-0.100.1.trx. Sem execução GTA.

Casos novos:14 entradas externas distintas dos marcadores internos e próximas das respectivas lojas, whitelist dos quatro modelos de roupas excluindo Ammu-Nation/barbearia/concessionária/garagens, alcance horizontal/altura/NaN, configuração removida ou loja em outro andar, coordenadas inválidas. Runtime compilado com GET_STATE_OF_CLOSEST_DOOR_OF_TYPE e SET_STATE_OF_CLOSEST_DOOR_OF_TYPE, conserva ângulo e só destrava estados consultados como travados; contexto seguro e falha isolada. Não muda saves ou configurações.

Referência estática: MultiplayerCharacter.ShopsDoorsLocation/UnlockDoors/Update da PSRP.dll; nomes de modelos conferidos no ObjectList.ini público do gta-v-data-dumps. Nenhuma DLL executada dentro do GTA. Guia PORTAS-ROUPAS-0.100.1.md descreve restrições e roteiro nativo.

Instalador simulado duas vezes sobre0.100.0:208 arquivos existentes byte a byte preservados, DLL nova correspondente ao build, anterior em backup e RPF externo intocado. Relatório installer-validation-0.100.1.json. Configs JSON/INI e áudio anteriores comparados aos arquivos do pacote. Portas físicas, streaming, colisão e interferência de outros scripts ainda exigem teste GTA.
