# 0.109.0 — conjunto autoescola / DMV: C, M1 e A

Esta entrega revisa o grupo inteiro de habilitações existente: seleção de categoria, atendimento/GPS, estudo, teoria, prático, avaliação, suspensão, retomada, emissão e resultados. Não exige apagar licenças ou personagens e não acrescenta bloqueios por documento aos trabalhos/veículos que já funcionavam.

## Atendimento e menus

Use F10 → DMV ou E no atendimento indicado no mapa. A tela inicial mostra C (carros), M1 (motos) e A (caminhões/carretas), com o estado de cada documento. C/M1 ficam no DMV urbano; A tem pátio industrial. Selecione a categoria para acessar GPS, estudo, provas, pendências e resultados. Uma tentativa pendente mantém o endereço/valores contratados, mesmo se os padrões forem personalizados depois.

Todos os handlers deste grupo passam pela fila de ações depois de ObjectPool.Process, com vínculo ao personagem/perfil e página, retorno por Esc, seleção conservada e descarte de cliques antigos. Confirmar taxa verifica novamente saldo/documentos. Nenhuma alteração de menu é executada durante a enumeração do clique. O grupo usa o contexto de cena já corrigido, sem alterar a lógica global do HUD ou o controle do personagem.

## Teoria e estudo

Material de estudo gratuito por categoria, com páginas de leitura sobre proteção, circulação, distância, paradas, conversões, estacionamento, manobras, faltas e documentos. A teoria continua com 15 questões próprias em português por categoria, quatro alternativas e mínimo configurado (padrão 12). A organização segue cinco páginas de três perguntas, uma pergunta ativa por vez; o painel mostra pergunta e alternativas por extenso. Pode responder pelo menu ou A/B/C/D. Atalhos de outros sistemas ficam suspensos enquanto responde; teclado/menu não devem registrar duas respostas na mesma página.

A taxa é confirmada antes de começar e debitada pelo banco. Esc/End ou Suspender guardam respostas já confirmadas e pagamento. Retome no atendimento sem recomprar a tentativa. Não é permitido responder fora do atendimento ou após troca de personagem/contexto. Ao concluir, fica uma revisão persistente com respostas dadas, corretas, acertos e resultado; não revela respostas futuras durante a prova. Os certificados continuam separados por categoria/perfil.

## Preparação do prático

Após teoria aprovada, confirme o prático. Modelos, piso, vaga, veículo e examinadora devem estar preparados antes da nova taxa. C usa Asterope; M1, PCJ 600 com examinadora na garupa; A, Phantom e carreta. A busca de vaga livre também vale para carros/motos, evitando criar sobre veículos ou pessoas. O conjunto A mantém busca validada e engate físico, sem limpar o pátio.

Somente entidades criadas pelo exame recebem registro de identidade por handle/modelo/endereço. Retirada/limpeza valida esse registro; não varre ou apaga veículos/pedestres do mapa. O capacete emprestado mantém o adereço anterior e verifica também a identidade de memória antes de restaurar ou filtrar a captura de roupa. A troca de personagem que foi estabilizada na 0.102.2 permanece idêntica.

Entre como condutor; demonstre proteção e seta direita/esquerda com veículo parado. O HUD informa as teclas configuradas. Ao começar o percurso, apaga apenas as duas setas demonstradas do veículo de exame. A avaliação acompanha o veículo certo e a examinadora, sem invulnerabilidade, exclusão de tráfego ou suspensão de polícia.

## Percurso, faltas e estacionamento

Pontos e limites configurados anteriormente são mantidos: C/M1 com 36 pontos, A com 16. Novas provas exigem as paradas pelo tempo indicado e sinalização das conversões identificadas pela geometria do percurso; o HUD indica esquerda/direita. Não avalia automaticamente cor de semáforos nem atribui culpa de trânsito a partir da IA. Se perder uma conversão, siga com segurança e retorne ao marcador.

Faltas mostram motivo e ficam registradas com etapa/tempo: colisão/dano durante condução avaliada, excesso de velocidade persistente, condução sem cinto/capacete e conversão sem sinalização correta. Um excesso contínuo conta como uma ocorrência; precisa recuperar a condição segura por dois segundos para encerrar essa ocorrência. Colisões têm intervalo mínimo entre registros. Ainda é obrigatório reduzir e usar proteção para validar os pontos. O menu mostra o total e o histórico; faltas antigas sem detalhe permanecem no total, sem inventar motivo retroativo.

Tempo de exame conta nas etapas após entrar como condutor, sem cobrar preparação por modelos nem pausa do jogo. Consultar menu com conjunto parado pausa o contador e reinicia a espera de parada; em movimento, a avaliação continua e fecha o próprio menu. Teleporte/deslocamento discontinuo continua reprovando, inclusive se o menu estiver aberto.

No estacionamento novo de carro/moto, conferir posição, altura, sentido do veículo, proteção, examinadora e tempo parado (padrão três segundos). O marcador/indicação mostra o sentido esperado. No A, a ré passa a medir deslocamento da própria carreta, acoplada e alinhada ao cavalo, com condutor/examinadora/cinto, perto da vaga; mover apenas o cavalo em manobra de canivete não deve contar. Depois alinhe/pare, desengate pelo menu e desembarque.

Quando conclui percurso, estacionamento e desembarque, a aprovação fica gravada e marca o atendimento no GPS. A partir daí a emissão não depende da existência física do veículo/examinadora e não perde aprovação porque esses recursos saíram do mapa. Volte a pé ao atendimento e emita, sem taxa adicional. O resultado final guarda percurso, faltas, motivos e tempo; uma gravação repetida não deve emitir/cobrar outra vez.

## Suspender, falhar e compatibilidade

End ou Suspender conserva a tentativa paga, pontos/faltas/tempo confirmados. Retomar prepara outro veículo/conjunto sem repetir taxa, exige novamente as etapas físicas de preparação e mantém o histórico. Cancelamento com estorno só existe antes de iniciar; abandono iniciado pede confirmação e não devolve taxa. Morte/procurado/missão, abandono, danos graves, deslocamento descontínuo, limite de faltas/tempo ou perda do conjunto podem reprovar com motivo. Carregamento/cena e falha técnica suspendem conservando o último checkpoint gravado.

Provas antigas sem o novo campo Assessment conservam preço, opções, pontos e certificado. Não recebem exigência retroativa de setas nas conversões nem o alinhamento novo de carro/moto; os tempos de parada/estacionamento anteriores continuam nessa tentativa. Provas novas recebem cópia da política de avaliação no pagamento. Alterar Jobs/DmvRefinement.json só afeta as novas tentativas; não reescreve uma política já contratada. Os três arquivos Dmv.json/DmvM1.json/DmvA.json existentes continuam iguais. Metadados desconhecidos, banco, carteira e progressos dos outros sistemas são preservados.

## PSRP.dll: evidência e diferenças

Revisado o arquivo de 1109 linhas PSRP/TheoryTests.cs e as chamadas OnLoad/Update em PSRP/Main.cs. Há teoria C com cinco páginas, alternativas A–D, nota 12/15, taxa $30, ponto DMV, Asterope, examinadora, comandos de setas/cinto e lista de 36 pontos. O handler público do prático C está vazio e a atualização não progride além do ponto 2 nessa exportação. M1/A são anunciadas como futuras. Por isso, não se declara que um ciclo completo de M1/A foi copiado de um ciclo inexistente na referência.

RPCore conserva esses elementos da referência e completa seus próprios ciclos das três categorias, questões em português, rota industrial A, ré, recuperação, avaliação e emissão. Usa painel adaptativo em português em vez de desenhar imagens Page1..6 com perguntas/alternativas incompatíveis. Os WAV de fala ClassCTest referidos no código PSRP não estão no conjunto de áudio recebido; não é anunciada dublagem desses arquivos. As instruções ficam no HUD/subtítulos. Não copia DisableAllControls/EveryoneIgnorePlayer, invulnerabilidade ou exclusões de mundo da referência. Matriz e hashes: DMV-REFERENCE-MATRIX-0.109.0.json.

## Validação e teste GTA

4391 testes aprovados, 30 novos casos; Release net48 sem erros/avisos. Os testes cobrem os três ciclos completos, cobrança única, política contratada/legado, revisão teórica, resultados finais, IO/rollback, retomada/tempo/faltas, limites físicos de estacionamento, orientação das conversões, ocorrência contínua, descarte de resposta antiga e metadados. Não executam o GTA: criação/posição de veículos, garupa, engate, colisões, marcador e legibilidade dependem do teste local.

Instale com GTA fechado e confirme 0.109.0 no log. Não apague Wallet ou licenças. Categorias já emitidas continuam emitidas, sem recomprar exames ou criar relatórios fictícios para provas antigas. Para repetir os exames de uma categoria já emitida, use outro perfil/personagem de teste. Teste o grupo:

1. F10/DMV, três categorias, GPS, E local e páginas de estudo; voltar/fechar e reabrir sem sobreposição.
2. Contratar teoria, responder por menu e letras, suspender após algumas respostas, retomar e concluir; conferir revisão e um único débito.
3. Preparar prático com vaga livre e ocupada; não deve cobrar antes de preparar. Entrar/proteger/setas, acompanhar motivos de falta e conversões.
4. C e M1: percurso, paradas, estacionamento fora/alinhado, desembarque e emissão. M1: capacete/garupa e restauração do adereço. A: engate, percurso, ré da carreta, alinhamento e desengate.
5. Suspender, recarregar/trocar perfil e retomar sem taxa repetida. Testar reprovação, abandono confirmado e emissão após sair/voltar ao jogo.
6. Regressão: PDM, salários de 1 hora, vagas externas, F11 $1 milhão, inventário, banco, roupas e troca manual de personagem continuam como na versão aprovada.
