# PDM / empresa — refinamento do grupo0.107.0

O usuário autorizou adiantar o conjunto PDM completo, inclusive revisão funcional dos contratos. Foram revisados os12sistemas empresariais existentes juntos, comparando PSRP.PDMCompany (gestão, estoque, showroom, equipe, finanças, retomada, locadora e valores). A implementação mantém os saves/arquitetura do RPCore e usa código próprio. PSRP-PDM-REVIEW-0.107.0.json relaciona as famílias e diferenças; PDM-SCOPE-0.107.0.json registra os arquivos preservados.

## Entrada e navegação

**E no escritório externo PDM** ou acesso à empresa nos trabalhos. A RPCore Motors/F12 continua sendo a compra de carros pessoais no mesmo prédio. Preços, coordenadas, modelos, capacidade, funcionários e preferências dos arquivos atuais não são substituídos. Se já possui a empresa, não precisa comprá-la novamente.

O painel da empresa separa resumo, finanças, estoque, showroom, equipe, segurança e trabalhos. O resumo informa caixa, banco pessoal, custo do estoque, salários/acertos, recebíveis, operação pendente e resultado operacional. Aportes/saques não são receita nem lucro; recebíveis não são saldo disponível. O cálculo usa decimal para não estourar ao apresentar contadores longos.

Todas as páginas PDM usam ações adiadas, retorno e proteção contra tecla/click segurado ou troca de ator/perfil. Listas grandes são paginadas; Voltar permanece acessível. Compra, gasto, reserva, cancelamento e recuperação têm confirmação começando por Cancelar. A confirmação usa termos da página exibida e recusa saldo/estoque/obrigações/contrato alterados ou mais de60s de jogo; contadores de tempo sem mudança financeira não invalidam sozinhos. Ao recusar, atualiza a página. O banner padrão usa o mesmo dicionário vanilla Premium Deluxe Motorsport da referência, com fallback; configuração de banner PDM personalizada continua respeitada.

## Caixa, fornecedor e estoque

Finanças oferece aportar/sacar valor digitado ou o máximo, com resumo antes/depois e confirmação. **Retirado o limite artificial de$1milhão da transferência administrativa**; continuam valendo saldo, reservas, espaço do destino e inteiros do save. Banco/carteira pessoal e caixa da empresa não se confundem. A coleta física de valores mantém seu limite contratual de$1milhão; não é a mesma operação.

Fornecedor mostra custo, preço base e margem antes de confirmar a encomenda. Unidade comprada aguarda coleta no porto; não é cobrada novamente. Estoque tem detalhe por unidade, condição, sujeira, status, exposição, entrega à vista/financiada e reparo. Reparo saudável e melhoria já adquirida ficam indisponíveis. A cotação de reparo é conferida novamente antes de pagar.

Coleta no porto permite marcar uma ou duas unidades específicas. Ainda há coleta rápida do primeiro pedido. Preparação do Packer/TRFlat é cancelável; depois engate, vá ao porto, aproxime os carros da carreta, pare, saia e useE para carregar. Retorne, pare e descarregue. Dano/perda de uma unidade na sessão não vira reparo grátis ao cancelar; recuperação de uma sessão anterior não inventa dano só porque seu veículo temporário não existe. Unidades já removidas na limpeza de uma tentativa não recebem dano novo num retry.

## Showroom e negociação

Vagas e modelos configurados permanecem. Não limpa carros/pedestres do ambiente para abrir espaço e só usa vaga interior com piso/interior disponível. Aproximar-se carrega os modelos sem espera bloqueante; criação de carros/funcionários/câmeras de fundo é distribuída, em vez de carregar tudo numa atualização.

Ao iniciar atendimento de um carro exposto, um comprador vai inspecioná-lo. Acompanhe o cliente, deixe o carro vazio e aguarde seis segundos de inspeção válida. Perto/desarmado, **E abre a conversa**. Pode aceitar a oferta, fazer uma contraproposta ou recusar. A oferta inicial é95% da cotação; o orçamento do comprador varia por atendimento de95a105%. Há uma tentativa de contraproposta, limitada a10% acima da cotação, que pode ser recusada. Não há novo sorteio no mesmo pedido após recarregar os dados.

A proposta aceita fixa o preço e **não paga ainda**. Cliente segue para a entrada; aproxime-se a pé/desarmado e useE para passar as chaves. Só então o caixa recebe e o estoque é baixado uma vez. Recusa, timeout ou cancelamento antes da venda conservam o carro/estoque. Inspeção/oferta/aceite são checkpoints retomáveis pelo código; ao reiniciar o plugin, a política existente encerra o atendimento interrompido sem receita e permite outro atendimento, preservando a unidade. Pendência antiga sem negociação mantém seu preço fixo/formato.

## Equipe, serviços e segurança

Equipe tem detalhe por cargo: contratação, salário, vencimento, pagamento, demissão e acerto. **Quitar folha e acertos** paga todos os valores vencidos numa transação; se o total mudar no vencimento antes de executar, exige outra confirmação. Não paga parcialmente um total que excede o caixa. Demissão transfere vencidos, proporcional até confirmar e multa ao acerto pendente, como já funcionava; marketing respeita a ordem dos cargos.

Oficina e divulgação mostram custo/efeito e não permitem comprar de novo. Bônus usa o maior cargo em dia, não a soma; zeladoria e guardas não geram bônus de venda. Mecânico/zeladoria precisam estar contratados e em dia, caminham ao próprio carro e fazem o serviço antes do efeito/cobrança. Ferramenta/animação carregam sem bloquear; limpeza/reparo interrompidos não concedem efeito parcial. Reparo físico conserva gasolina/óleo.

Segurança mostra situação de cada posto, salários, acesso à equipe e retorno ao posto sem teleporte. Defesa continua local, contra agressor NPC elegível; não ataca polícia/jogador/personagem protegido nem remove estrelas. Atraso ou acerto pendente suspendem o benefício. Salários são atualizados antes da decisão de serviço/defesa/venda.

## Contratos, locadora, valores e proteção

Financiamentos mostram lista e detalhe por contrato: entrada, acréscimo, parcelas, recebido, saldo e próxima data em tempo ativo. Inadimplentes levam à retomada; quitação/retomada ficam no histórico. Falta de espaço no caixa mantém parcela pendente. Retomada usa o patrimônio próprio e vaga de estoque, sem prêmio em dinheiro. Conserva coleta, danos, confirmação de chegada e recuperação anteriores.

Locadora mantém reserva de duas unidades próprias do mesmo modelo: engate, carregue, dirija ao pátio externo, buzine, espere o funcionário conferir e devolva o conjunto para receber. Revisadas confirmação/preparação/navegação/identidade e recuperação; entrega já aceita não reabre o estoque para outra venda. Recebimento bloqueado por caixa pode ser resolvido sacando no escritório e usando Recuperar.

Coleta de valores reserva caixa, aguarda Stockade/segurança, passa a maleta comE e compensa no banco. Antes da entrega, cancelamento devolve a reserva; depois da entrega, recuperação só conclui o pagamento uma vez. Nunca devolve e credita o mesmo valor. Seguro/cofre empresarial mantêm regras de exposição e cobertura, com confirmação para abrir/fechar, contratar/trocar/cancelar. Renovação continua pelo caixa/tempo ativo; não é seguro de carro pessoal.

Câmeras: instalação confirmada, equipamento reconhecido e preparação sem travar o script. Teclado e **controle/B/voltar** encerram a visão; esquerda/direita trocam. Perda de contexto, distância, dano, tempo máximo ou outra câmera encerra a sessão, sem tomar a câmera de outro sistema.

## Estabilidade e persistência

Sem Model.Request temporizado/Script.Wait nos12sistemas PDM: recursos são solicitados e conferidos ao longo dos frames. Preparação tem cancelamento e prazo de10s; andar mais de5m, fechar, perder contexto ou trocar ator/perfil cancela antes de iniciar. Não contrata/coleta/cobra só porque um modelo começou a carregar.

Cada relógio processa no máximo dois lotes por chamada. Tempo/recibo pendentes ficam para a próxima passagem, e ações que exigem contas em dia aguardam a sincronização. Evita descarregar uma fila grande de IO num único menu após uma falha longa. Troca de região do risco aguarda concluir o tempo anterior em vez de descartá-lo. Não adiciona salário/receita por tempo offline.

Entidades criadas recebem prova de handle/modelo/endereço somente em memória; handle reciclado não autoriza controlar/apagar outra entidade. Helpers compartilhados aplicam a prova somente quando existe, mantendo o comportamento nativo de entidades não rastreadas da PDM. Não introduz registro global de personagens, clone de jogador, novo decorador ou varredura para deletar NPCs. Buffer de consulta de carreta e etiquetas de preço são inicializados/liberados.

Personagens/roupas, troca estável de modelo, inventário, banco do celular, combustível/cinto, compra/seguro pessoal e ciclos dos demais trabalhos permanecem. Instalação preserva os arquivos atuais e gera backup da DLL; não precisa reinstalarOIV/RPF/YTD. O ZIP conserva relatório de testes atual; relatórios antigos continuam no repositório, reduzindo o download sem retirar código, áudio ou recursos de jogo.

## Teste integrado no GTA

1. Fechar GTA, instalar e conferir o HUD. Abrir/fechar empresa10vezes comE/Backspace/B/Escape, navegar por todos os grupos e depois abrirF11, celularF2 e inventário. Não deve precisar reiniciar plugins para recuperar os menus.
2. Conferir resumo com seu caixa/estoque/salários. Cancelar compra/transferência não altera saldos. Testar aporte/saque digitado e máximo, incluindo valor acima de1milhão se houver saldo; reiniciar GTA e conferir ambos os saldos.
3. Encomendar dois carros, selecionar quais vão ao porto, cancelar preparação e tentar pátio ocupado/modelo ausente. Nada deve ser apagado/cobrado novamente. Concluir coleta e conferir unidades, placas e danos no estoque.
4. Expor uma unidade; testar vaga bloqueada e uma vaga interior, se instalada. Iniciar atendimento, acompanhar inspeção, abrir conversa, recusar e conferir estoque. Em outro atendimento, aceitar/contrapropor, passar chaves e conferir receita única. Testar cancelamento antes da entrega e recuperação após reiniciar.
5. Contratar mecânico/zeladoria/marketing/guardas que desejar; testar pré-requisitos, salários, quitar folha, demissão/acerto e melhorias já compradas. Pedir reparo/limpeza e interromper antes de concluir. Conferir efeito/custo e combustível preservado.
6. Conferir guardas, GPS de postos e retorno. Não provocar polícia para exigir defesa: com procurado ela é suspensa. Instalar/ver câmeras, trocar e sair tanto pelo teclado quanto pelo controle. Reabrir os outros menus/HUD.
7. Entregar carro à vista e financiado, conferir entrada/parcelas/caixa/histórico. Usar um contrato inadimplente para retomada, se existir, conferindo retorno ao estoque sem prêmio.
8. Fornecer duas unidades à locadora, buzinar/conferir, retornar e receber uma vez. Testar interrupção antes e depois da aceitação. Coleta de valores: testar cancelamento antes da maleta e compensação após entregar.
9. Conferir apólice/renovação/cofre, cancelar uma troca e revisar extrato. Carregar outro perfil e voltar: nenhuma operação/câmera/seleção deve migrar para a outra empresa.
10. Testar aparência/roupa, inventário, banco, abastecimento/cinto e um trabalho externo à PDM. Se ocorrer travamento nativo, preservar RPCore.log, ScriptHookVDotNet.log e RuntimeLastSession.json da mesma sessão, informando a última ação.

## Validação e limites

4162 testes de dados aprovados (4105 anteriores +57), Release net48 sem erros/avisos. Cobrem confirmação/expiração, limites/destinos, transferências grandes, folha atômica/IO/vencimento, cálculo sem overflow, negociação/recusa/aceite/IO/idempotência/formato antigo, inspeção e identidade. Natives, IA, navegação, renderização e controles precisam do teste GTA acima; não se declara reprodução/resolução definitiva do travamento nativo relatado só com esses testes.

Este é o refinamento dos12sistemas empresariais implementados, com adiantamento funcional autorizado da PDM. **Não é paridade integral com todos os recursos PSRP**: reputação/cartões/charidade/patrocínio, evento de vendedor particular, carreta de seis carros e melhorias visuais/uso manual de lavadora/bomba/vidro/luzes não foram acrescentados. Não há menus vazios para simular esses recursos. Preços/lista de fornecedor/escala de tempo próprios foram mantidos. Refinamento dos outros trabalhos continua reservado ao grupo correspondente.
