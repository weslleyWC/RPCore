# RPCore 0.40.0 — fase 6.4B: DMV / habilitação classe C

## Instalação e acesso

Extraia RPCore-update-0.40.0.zip da branch rpcore-entrega. Abra PowerShell na pasta extraída que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Requer GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Não precisa reinstalar .oiv/.rpf nem executar PSRP.dll junto. Acrescenta Jobs/Dmv.json quando ausente; conserva saves/configs e recursos anteriores, incluindo personagens, seguro, F2/F11.

Abra **F10 → DMV / habilitacao C → GPS: DMV**. O blip branco fica no ponto Court/DMV da referência, perto de -544.56, -204.25, 38.22. Contrate a pé, parado perto do atendimento, sem procurado/missão/combate/cutscene/outra interface e sem outra atividade reservada. F11/teleporte pode ajudar a chegar ao DMV; durante o percurso o carro precisa ser dirigido normalmente.

Esta etapa é de **classe C (carro)**. Classes A e M1 ainda não são oferecidas. O documento é informativo por perfil: não bloqueia dirigir, garagens, seguros, empregos ou a qualificação de piloto já existente. Não aplica multas, nova polícia ou importação automática de documentos PSRP.

## Prova teórica

Taxa padrão **$30 do banco RPCore**. São 15 perguntas próprias em português, quatro alternativas por questão e mínimo de **12 acertos**. Use o menu e confirme uma alternativa; o enunciado também aparece na descrição da linha. Há intervalo entre respostas para evitar repetição involuntária da tecla. Não usa as páginas gráficas/alternativas da PSRP nem bloqueia os controles globais do jogador.

Cada resposta é salva antes de passar à próxima. Sair do menu, afastar ou abrir outra interface conserva as respostas e a taxa: volte ao DMV e Retomar prova teorica paga. Não cobra de novo. Terminar com nota suficiente grava o certificado teórico e libera o exame prático; não emite habilitação ainda. Reprovar encerra a tentativa e uma nova prova tem nova taxa. Não é possível comprar/refazer uma teoria já aprovada por este menu.

Pedido cuja autorização de início não foi salva pode ser cancelado/estornado uma vez. Depois do início há retomada ou Abandonar tentativa sem estorno, com confirmação. Abandonar não devolve a taxa nem apaga certificado anterior. Banco no limite inteiro conserva pedido se não puder receber o estorno.

## Exame prático

Taxa padrão **$60 do banco RPCore**, após a teoria aprovada. O mod prepara um **Asterope e uma examinadora** em vaga da referência, perto de -485.30, -203.58, 36.37. Modelos são carregados por ticks, até dez segundos. Vaga ocupada, chassi/chão/modelo/NPC indisponível ou saldo insuficiente impedem nova cobrança. Não apaga carros/pedestres para liberar a vaga.

Quando o carro/equipe estão prontos, grava taxa/pedido e autorização de início. Depois:

1. Entre como **motorista** do carro marcado, com a examinadora no banco passageiro.
2. Coloque o cinto pela tecla configurada; demonstre **seta direita**, depois **seta esquerda**, com cinto e sem pisca-alerta. As teclas reais de cinto/setas/motor aparecem no aviso. Se controles veiculares estiverem desativados, o menu DMV oferece demonstração das setas no carro parado.
3. Siga os **36 pontos em ordem**, todos com coordenadas da PSRP. Cada ponto pede motorista/examinadora corretos, proximidade, altura compatível e velocidade de até **50 km/h**. Não pula pontos.
4. Nos pontos **5, 17 e 29**, pare completamente por pelo menos um segundo na marca. São avaliações explícitas de parada, não reconhecimento de placas/semafóros do cenário.
5. Depois do último ponto, retorne à vaga inicial e fique parado por **dois segundos**. Retire o cinto e saia perto do carro. Volte a pé ao DMV.
6. **F10 → DMV → Receber habilitacao aprovada** emite a classe C sem taxa adicional. O estado de aprovação fica salvo, inclusive para emitir depois de reiniciar, sem dirigir tudo novamente se já concluiu o desembarque.

O limite de velocidade tem avaliação de excesso acima de 55 km/h por três segundos; persistir no excesso gera novas faltas. Ficar sem cinto por cinco segundos também gera falta, além de impedir passar os pontos enquanto ele for exigido. Danos acumulados de pelo menos 25 unidades geram falta. **Cinco faltas reprovam**; danos graves, morte/perda da examinadora/carro, procurado/missão, veículo errado, abandono ou deslocamento descontínuo do carro também interrompem/reprovam. Limite padrão: **20 minutos ativos** incluindo preparação no carro e retorno ao atendimento. Pausa do jogo não acumula tempo; não há contagem offline.

Carro e examinadora não são invencíveis. Não há teleporte, câmera forçada, congelar o jogador ou ignorar a polícia. O motor fica inicialmente desligado; use a tecla de motor configurada se necessário. Cinto/setas são integrados aos sistemas existentes.

## Retomada, reprovação e limpeza

END ou Suspender / retomar depois conserva taxa e progresso já salvo. Respostas teóricas e progresso prático são persistentes; o prático registra pontos, faltas e tempo periodicamente e nas mudanças de etapa. No DMV, Retomar exame pratico pago prepara carro/equipe novamente, repete preparação de cinto/setas e continua do ponto gravado, mantendo faltas/tempo e sem nova cobrança. Se já concluiu todos os pontos, só precisa completar as etapas restantes; relatório de aprovação pronto permite emissão direta no atendimento.

Reprovar no prático mantém o certificado teórico, mas exige nova taxa prática para outra tentativa. Falha de gravação conserva o último estado persistido para retomar; não emite licença por mera falha/simples recarga. Licença já emitida não pode ser cobrada novamente. A atualização e o carregamento de perfil nunca iniciam prova ou concedem documento automaticamente.

Só os atores temporários criados para o exame são retirados. Carro é reservado como veículo de atividade, impedindo cadastro em garagem/seguro. Ao encerrar, sua remoção normal espera ficar vazio e parado, sem conexão estrangeira; não desaparece com o jogador dentro. Examinadora recebe saída/retirada controlada. No abort do script há limpeza dos próprios atores e liberação do veículo ocupado/móvel, sem forçar retirada do jogador ou apagar veículos alheios. Vaga pode continuar ocupada até liberar o carro anterior.

## Configuração e comparação

Jobs/Dmv.json define preços, nota, limite de velocidade/faltas/tempo, exigência de cinto, entrada, spawn, rota e pontos de parada. A tentativa paga guarda uma cópia das regras/preço/rota; mudar config depois não reprecifica nem facilita a tentativa existente. Config inválida usa padrão embutido em memória e conserva o arquivo. Custos zero podem ser configurados para testes em tentativas novas, com o jogo fechado.

Adicionados DmvData.cs (regras/questões/estado/transações), DmvSystem.cs (DMV, modelos, avaliação e cleanup), nova seção opcional Wallet.Dmv e integração no F10. Certificado/licença, tentativas, nota, taxas, respostas, progresso e recibos ficam por perfil. Banco/livro são gravados juntos com rollback; preserva metadados desconhecidos. Sistemas de trabalho/veículos e qualificação de piloto anteriores não foram condicionados à licença C.

Analisado TheoryTests.cs inteiro da PSRP, incluindo custos 30/60, nota mínima 12, atendimento, Asterope/examinadora, cinto/setas e 36 pontos. A referência tem botão prático vazio, nenhum chamador da função que o inicia e uma progressão que fica no ponto 2. RPCore implementa o ciclo completo com código próprio, perguntas próprias e avaliação adicional, sem invencibilidade ou teleporte de encerramento. Classes A/M1 estão marcadas como futuras na referência; não foram adicionados menus vazios para elas.

Restam classes A/M1, integração de fiscalização/documentos físicos, importação de certificados PSRP e refinamentos de interface/avaliação do trânsito. Reconhecimento automático de semáforos/placas/faixas não faz parte desta entrega. Fase 8 de interface/mecânicas e fase 9 de todos os trabalhos permanecem previstas.

## Testes no GTA

1. Chegue pelo GPS/teleporte e abra o menu a pé. Sem saldo suficiente ou durante outra atividade/procurado/missão, não deve iniciar/cobrar. Prova teórica nova cobra $30 uma vez; responda algumas questões, suspenda/reinicie e retome sem novo débito.
2. Teste aprovação com pelo menos 12 acertos e reprovação abaixo de 12. Só certificado teórico deve ser concedido; habilitação ainda pendente. Confira nota e tentativas por perfil.
3. Inicie prático com certificado e $60. Vaga ocupada/modelo indisponível não deve cobrar nova tentativa. Entre no banco motorista, coloque cinto, faça direita/esquerda e confira a mudança para a rota. Pisca-alerta/passageiro/sem cinto não devem passar a preparação.
4. Dirija os 36 pontos, incluindo paradas 5/17/29 e retorno/estacionamento/desembarque. Não use teleporte no carro. Confira cada incremento e depois a emissão presencial sem taxa adicional.
5. Teste excesso prolongado, falta de cinto, colisão/danos graves, veículo errado e suspensão por END. Retomada paga deve manter pontos/faltas/tempo sem cobrar; reprovação mantém teoria e nova tentativa custa $60.
6. Ao cancelar/suspender, o carro ocupado/móvel não deve sumir. Examinadora/carro temporários não devem virar propriedade registrada/segurada. Libere a vaga para nova preparação.
7. Troque perfil/reinicie: documentos não devem vazar nem provas iniciarem sozinhas. Confira personagens/retorno ao protagonista, seguro/garagem/fuel/storage, F2/F11/J, moradia/PDM e todos os trabalhos. Nenhum job aprovado deve passar a exigir a nova licença.

Build Release sem erros/avisos; 1450 testes passaram (38 novos). Auditoria PSRP-SYSTEMS-AUDIT-0.40.0.json verifica os 36 pontos contra a referência. Não foi executado GTA na nuvem: vaga, percurso/tráfego, UI, cinto/setas e comportamento dos atores precisam do teste real.
