# 0.83.0 — infrações adicionais e perseguição da patrulha

Entrega agrupada do núcleo policial independente. Mantém fiscalização0.81 e custódia0.82; adiciona os quatro checks ativos restantes daBetterPoliceSystem e perseguição própria com pedido de reforço nativo opcional. Não declara equivalência totalPSRP: suborno, BOLO automático no seguro e painel/integraçãoLSPDFR não foram portados. As duas infrações de para-choque existem como enum da referência, sem check ativo emEvaluateViolations.

## Ativação e configuração

**F11 → Polícia/trânsito e multas**: ligue Fiscalização. Duas opções por perfil, desligadas em saves antigos:

- **Infrações adicionais**: celular nativo$100, distância insegura do veículo à frente$120, arma apontada$350 e buzina prolongada$125.
- **Perseguição/reforço**: confirma que fuga pode solicitar estrelas nativas. A patrulha continua usando somente agente/viatura próprios. Configuração nova`Police/TrafficPursuit.json`:35segundos de fuga ativa de carro,45a pé,300m de distância, até3estrelas. A autuação captura seus parâmetros; alterações futuras não reescrevem dívidas anteriores.

As sete regras anteriores continuam disponíveis com as opções novas desligadas. Não é possível mudar regras durante uma abordagem. Fiscalização desligada não cria patrulha nem multa; pagar dívidas antigas continua permitido.

## Um roteiro de teste no GTA

1. Com carro/moto pessoal, sem trabalho/missão/procurado, solicite **Patrulha observadora (teste)**. O botão não fabrica infração. Use o cinto e evite outras infrações para isolar os novos checks.
2. Para buzina, mantenha-a acionada continuamente por10segundos, depois mantenha a condição durante a janela de evidência configurada (padrão2s), com testemunha dentro50m/visão. Soltar buzina, trocar carro ou abrir menu/pausar reinicia a contagem. Confira item$125 na multa. A patrulha observa por90s e precisa se aproximar.
3. Distância insegura exige conduzir acima de10mph(~16km/h), veículo à frente atingido pelo raio10m, sentido semelhante e altura compatível; requer observação contínua. Cruzamento rápido/veículo oposto/viaduto não deve gerar esse item. Teste celular NATIVO e arma apontada separadamente. Celular RPCoreF2/modal continua protegido, inclusive suas restrições anteriores; esta entrega não habilita celular enquanto dirige. Apontar arma pode provocar a polícia nativa antes da autuação RPCore; não há supressão dela.
4. Na abordagem, encoste e aguarde ao volante: conferência/multa como antes. Parado aguardando agente/viatura não acumula fuga. Espera longa expira atendimento sem estrelas por demora.
5. Com Perseguição ligada, em outra abordagem já autuada, saia do carro. A viatura reduz/para por tarefa de condução, o agente desembarca e segue a pé. Pare desarmado, espere o agente chegar até2,5m com visão;7,5s contínuos de conferência concluem a multa sem débito automático. Não há prisão automática por uma infração de trânsito.
6. Em novo teste, continue fugindo de carro35segundos ativos, a pé45segundos não cooperando, ou ultrapasse300m enquanto se move/foge. Voltar ao volante durante fuga a pé também solicita reforço. A multa é conservada, com recibo de pedido; pode chamar até3estrelas NATIVAS. Pedido é gravado antes do efeito e não garante que outro controlador policial deixe o jogo aplicá-lo. Nível máximo0 de outro mod é respeitado; estrelas já existentes nunca são apagadas/reduzidas.
7. Encerre/recarregue durante uma abordagem: registro fica interrompido, sem recriar perseguidor ou reaplicar reforço. END e menus de teste preservam dívidas e não solicitam estrelas. Após reforço, a patrulha RPCore encerra e a perseguição é nativa; use a rendição/custódia0.82 perto de policial nativo a pé, conforme guia anterior.
8. Confira banco/dinheiro/multas, troca de perfil, histórico pago/não pago, empregos antigos, F2 e teleporteF11. Não há multa extra por fuga, débito automático, nova exigência de habilitação ou confisco de itens/armas.

## Persistência e limites

Códigos antigos0..6 preservados, novos7..10, máximo11linhas distintas. Preferências independentes por perfil; snapshot de perseguição e BackupLevel na autuação. BackupLevel registra nível solicitado, não prova que o motor/outro mod aceitou a resposta. Gravação atômica/rollback e repetição idempotente, sem saldo alterado pela chamada. Não há perseguição/tempo offline.

Espera máxima do atendimento no modo novo é o maior entre StopSeconds e duas vezes o limite de fuga da etapa; vence sem estrelas por espera. Cronômetros pausam em menus/modais/pausa e não recuperam tempo após travamentos. Perseguidor não rouba agente/viatura do ambiente, não força velocidade do carro, não usa arma contra jogador nem apaga procurado à distância. Cleanup mantém as proteções de ocupantes/veículos em movimento/anexos. Não integra automaticamente outros controladores policiais.

3111 testes aprovados, Release net48 sem erros/avisos. Sem GTA neste ambiente: direção/pathing/ray/telefone/buzina, transição a pé, estrelas e convivência com mods precisam deste roteiro. Próximo bloco: fase7(missões/editor/revisão de paridade); fases8/9 continuam reservadas.
