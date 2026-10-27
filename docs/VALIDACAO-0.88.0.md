# Validação0.88.0 — transporte de passageiro e controles de mundo

3340 testes aprovados (3255 anteriores +85 novos); Release net48 sem erros/avisos. DataTests-0.88.0.trx e PSRP-SYSTEMS-AUDIT-0.88.0.json. Referências TaxiService/TaxiRide/TaxiVehicleData e WeatherSystem inspecionadas estaticamente; nenhuma DLL PSRP executada.

Cobertura nova: reserva/cancelamento/embarque/tarifa de táxi e limousine, devolução única, replay, períodos exatos15s, limites/saldo/overflow, falha de disco com rollback de banco/livro, opções imutáveis/metadados, IDs inválidos, histórico limitado, pausa/travamento/wrap do timer. Clima: padrão desativado, modos/coordenadas/intervalos inválidos, gravação antes de ativar, metadados, URL fixa/números invariantes, códigosWMO, conversãoC/F, respostas inválidas/grandes e transições suportadas.

Testes executam regras/callbacks de dados, não GTA/natives/LemonUI/IA. Guia FASE-7.5-0.88.0.md cobre chegada/embarque/rota/trânsito/desembarque/saldo/recarga/modelo próprio/cleanup, menus, clima/hora e falhas. HTTP real tentado por ferramenta separada: proxy do ambiente retornou403; integração online permanece sem validação. Implementação tem timeout assíncrono8s, limite64KiB, descarte de resposta obsoleta e preservação do clima em erro.

Novo campo Wallet.Passenger é opcional; preferências globais de mundo são separadas do Wallet. Defaults antigos/visuais/OIV/RPF/YTD/áudio permanecem idênticos; config nova não migra automaticamente estados PSRP. Banco/recibo do transporte são uma gravação com rollback; natives e arquivo não são transação resistente a encerramento abrupto do processo. GTA pendente.

Instalador PowerShell7/Linux: duas atualizacoes sobre0.87.0 em GTA simulado; **479 arquivos existentes preservados byte por byte**, incluindo reservas pre-embarque/embarcadas/taximetro, historico/liquidacao, banco/dinheiro/metadados, preferencias globais/coordenadas/unidades/backup e F2. Perfil antigo sem Passenger permanece sem campo/cobranca. Relatorio installer-validation-0.88.0.json.
