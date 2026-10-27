# RPCore0.94.0 — mouse e peso no inventário

Abra a mochila com I (ou sua tecla). Clique para selecionar; segure o botão esquerdo sobre um item, arraste e solte em outro slot da própria mochila. Slot vazio recebe a pilha; slot ocupado troca as duas posições. As quantidades continuam iguais e os atalhos1..6 passam a usar a nova ordem salva. Clique sem arrastar apenas seleciona; usar continua em Enter/1..6, sem consumo por clique acidental.

Botão direito cancela. Soltar fora da mochila ou no mesmo slot conserva o original. A roda rola as linhas sob o cursor e pode ser usada enquanto segura o item; PgUp/PgDn também funcionam durante arraste. Outras teclas cancelam o gesto antes de agir. C/teclado e os controles anteriores continuam funcionando. Um cursor parado não desfaz a seleção feita com as setas.

Na tela de armazenamento, o mouse pode selecionar os dois lados e reorganizar a mochila. Arrastar entre mochila e depósito não transfere: Enter/A e Shift+Enter/LB+A mantêm as operações existentes. Não há arraste das posições do depósito, ingredientes com mouse ou uso por clique duplo. O item nunca é retirado do estoque para desenhar o ícone de arraste.

A posição só é confirmada pelo MoveSlotSaved existente, que grava uma cópia antes de alterar o estado em memória. Falha mantém a posição anterior. Fonte que mudou/esvaziou durante o gesto é rejeitada. Fechar, recarregar perfil, pausar ou mudar para controle cancela o gesto; mudar para controle não usa o item no mesmo frame. Abrir com botão já segurado exige soltá-lo e pressionar de novo. Cursor e bloqueios de câmera/tiro só são solicitados nos frames com a grade ativa, sem manipular o cursor do Windows.

A referência PSRP consultada usa navegação por controles; esta é uma extensão própria, usando a mesma geometria: seis colunas/quatro linhas visíveis, tiles140px/passo130px, origem140/230 e depósito1040/230. Conversão do cursor usa largura proporcional, escala e inset da própria renderização, incluindo área segura e ultrawide. As áreas sobrepostas dos sprites pertencem ao último tile desenhado.

Nenhum arquivo de configuração, quantidade, carteira, trabalho ou save foi substituído. Apenas a disposição muda quando você solta em destino válido. Os limites e diferenças já registrados na revisão0.93 continuam válidos; não declara paridade integral da PSRP.

Teste no GTA: mover para vazio e ocupado; usar1..6 depois e recarregar para confirmar posição; clique simples sem consumo; soltar fora/direito/Esc; arrastar com roda/PgDn até outra linha; trocar teclado/controle; depósito/apartamento sem transferência acidental; variar escala/área segura,16:9/ultrawide e janela/tela cheia. Confira cursor alinhado, câmera parada durante a grade e normal depois de fechar.

Validação:3602 testes aprovados (3541 anteriores +61 novos) e Release net48 sem erros/avisos. Cobertura nova: coordenadas/escala/insets, bordas/sobreposição/rolagem, clique sem arraste, troca e vazio, quantidades, salvamento único, cancelamento, origem alterada e erro de gravação. Renderização/inputs nativos ainda precisam do teste dentro do GTA.

## Peso funcional

O total considera peso unitário x quantidade dos itens pessoais, incluindo munição e fallback de0,1kg para ID sem definição. Depósitos/estoques de trabalhos não entram na mochila. Pesos definidos e MaxWeightKg existentes são preservados; armas continuam com as definições atuais (zero no cadastro automático de referência), sem inventar novas massas ou apagar equipamento legado.

Acima do limite configurado, o personagem tem movimento progressivamente reduzido e sprint/salto bloqueados, automaticamente. A solicitação de taxa ao GTA varia de aproximadamente0,8 até um piso de0,45 conforme o excesso: continua possível andar para aliviar a carga. Uma tolerância de1g evita falsas sobrecargas por arredondamento. O aviso aparece ao entrar/sair da condição e a mochila mostra o estado. Não causa dano, ragdoll ou desmaio.

O efeito só é solicitado a cada frame seguro a pé, para humano vivo/do perfil atual e sob controle. Veículo/embarque, água/ar/queda, ragdoll/escalada, pausa/cutscene/missão nativa ficam excluídos. Não altera handling, multiplicadores globais de corrida/natação, tarefas ou velocidade de outros peds. Ao baixar o peso, depositar/descartar/consumir itens, trocar perfil ou entrar em contexto excluído, o código deixa de solicitar a restrição. Outros sistemas, como fome/fôlego, conservam seus próprios limites; o peso não força a ativação de controles que outro sistema desativou.

Para grandes estoques importados: no depósito, Ctrl+Enter calcula e transfere o máximo que cabe no destino, preservando o restante. Shift+Enter continua pedindo a pilha inteira e falhando sem transferência parcial se não couber. Valores/limites são revalidados na transferência; não há descarte automático por excesso.

Na PSRP consultada o peso aparece no cálculo/cabeçalho/barra; a busca por restrição de carga não encontrou movimento ligado a ele. A penalidade é implementação própria conforme o pedido do usuário. O limite hardcoded75kg da referência não substitui sua configuração.

Teste GTA: fique acima do seu limite e confirme aviso, lentidão, falta de sprint e salto; alivie no depósito com Ctrl+Enter e confirme retorno sem relogar. Repita ao descartar/consumir, recarregar perfil, entrar/sair de carro/moto e em pausa/missão. Confirme que o carro acelera normalmente e que necessidades/sono continuam independentes. Intensidade real da locomoção e restauração do native por frame ainda precisam homologação no jogo.
