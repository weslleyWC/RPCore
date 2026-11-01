# 0.110.0 — grupo de entregas urbanas

Pizza This!, Post OP e reposição de lojas revistos juntos a partir da PSRP.dll fornecida. O menu existente de trabalhos continua sendo a entrada; use GPS/atendimento, vá a pé e desarmado à base e revise o contrato. O F6 rápido legado da pizza continua separado, com seu pagamento anterior em dinheiro. Os contratos presenciais deste grupo pagam no banco pessoal, como na referência.

## Ciclo completo

Contrate na base sem taxa, retire o veículo e carregue os volumes com E junto à traseira, parado e fora do veículo. No destino, retire uma pizza/caixa pela traseira, leve a pé e confirme com E. Pizza tem cliente presencial e passagem da caixa; se o cliente não puder aparecer, após 30 segundos há alternativa de entrega na porta. Post OP e lojas recebem no marcador do endereço. Colisões reduzem qualidade; pizza fora do prazo perde pontualidade e recebe menos. Não basta passar pelo destino para ganhar.

Após as entregas, retorne à base e confirme com E como condutor parado. O bônus considera as entregas reais e a qualidade. Saia para devolver o veículo da empresa. Pizza permite receber e continuar outro lote com a mesma scooter pelo menu, recarregando na base. Post OP permite selecionar 5/10/15/20/25/30 encomendas conforme endereços disponíveis, usar van da empresa ou identificar seu próprio carro/moto como condutor e devolver/pular um pacote sem receber nem contá-lo como entrega. O veículo pessoal é liberado ao encerrar, nunca apagado pelo contrato.

End segurado suspende. Reabra no atendimento para retomar: destinos, valores, entregas, tempo e qualidade ficam gravados; o veículo da empresa é preparado novamente e os volumes restantes precisam ser recarregados. Não existe tempo offline nem multa por ficar consultando menus. Cancelar definitivamente pede confirmação, encerra sem bônus e mantém pagamentos anteriores. Último turno mostra comprovante; telefone recebe avisos locais e conquistas continuam integradas.

## Referência e diferenças deliberadas

Revisados PSRP/_JOB__Pizza_Delivery.cs (OnLoad, OnTick, Start, GenerateRoute, DeliveredPizza), _JOB__Post_Op.cs (OnLoad, OnTick, Start, GenerateRoute, NextBlip, Delivered) e a seção de lojas de JobDeliveryDriver.cs (aproximadamente linhas 1–275; prisão pertence a outro grupo). Matriz de hashes: URBAN-REFERENCE-MATRIX-0.110.0.json. É mapeamento estático, sem alegar equivalência binária ou teste GTA.

A referência de pizza usa três entregas por lote e pagamentos aleatórios; RPCore conserva quantidade/preços configurados e cálculo por distância, acrescentando retomada e checkpoint atômico. Post OP da referência usa veículo pessoal, seleção de lotes e pular; a van continua opcional no RPCore. Corrigido o avanço duplo/contagem de pacote pulado presente na referência. As modalidades frágil/segurança da PSRP são botões ComingSoon; não são apresentadas como recursos prontos. A loja da referência paga um valor aleatório grande só no final; RPCore conserva preços existentes, pagamentos por entrega e bônus proporcional no retorno. A carga de caixa mantém modelo/fallback do RPCore e respeita a configuração de visuais.

Não são copiados Script.Wait bloqueantes, limpeza de tarefas do jogador, NPC invencível nem remoção de veículo ambiental próximo ao carregar o script. A preparação é assíncrona, verifica vaga local, rastreia somente os recursos criados pelo contrato e não depende da área da PDM. Personagens, PDM, DMV, inventário, preços/configurações existentes e transações dos demais trabalhos permanecem preservados.

## Persistência e validação

Cada entrega usa identificador do contrato/índice e recibo persistente. Saldo, contadores e avanço da rota são gravados juntos, com rollback quando a gravação falha; repetir confirmação não paga novamente. Campos novos são opcionais nos saves antigos. Conteúdo desconhecido dos outros trabalhos é preservado; estado inválido do grupo é protegido sem zerar progresso.

4449 testes aprovados, 28 novos casos: ciclos dos três trabalhos, pagamento bancário, alternativa explícita em dinheiro, rollback de gravação, replay, limite de saldo, devolução, qualidade/atraso, retomada, cancelamento, continuação da pizza e metadados. Build Release net48 sem erros/avisos. A confirmação de abertura do DMV 0.109.1 veio do usuário; esse código não foi alterado neste grupo. GTA não executado neste ambiente.

## Roteiro GTA

Feche o jogo antes de instalar. Confirme 0.110.0 no RPCore.log. Pelo menu de trabalhos marque cada base. Teste uma rota de cada trabalho, retirada/carga, cliente da pizza, pagamento no banco, retorno e comprovante. Em pizza use continuar outro lote. No Post OP teste van e veículo pessoal, pular um pacote e conferir que não recebe por ele. Suspenda/recarregue durante uma rota e confira destino, saldo e qualidade; reentregas antigas não devem ser pagas. Confira lojas normais perto de uma entrega e o DMV fora da PDM. Modelos, colisão, animações e controles nativos precisam dessa validação no jogo. Não apague saves nem reinstale OIV/RPF/YTD.
