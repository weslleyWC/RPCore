# Fases 8 e 9 — escopo definido pelo usuário

Após testar a 0.16.0, o usuário confirmou o funcionamento dos trabalhos e pediu reservar duas fases para refinamento profundo. Os ciclos atuais são a base funcional; vários trabalhos ainda estão simples/irreais. Também relatou problemas no funcionamento das mecânicas de fome/sede e demais indicadores, distinguindo isso da imagem dos ícones.

A ordem permanece: concluir as atividades restantes da fase 5 (streamer e arrancada), entregar fases 6 e 7 e então executar **8: interface/mecânicas** e **9: todos os trabalhos**. Este registro não implementa nem declara corrigidos os problemas relatados.

## Fase 8 — interface inteira e mecânicas associadas

Abrange HUD e todos os menus/interfaces existentes ao chegar a esta fase: indicadores de fome, sede e demais estados, F11, radial J, celular F2, inventário, painéis e telas dos sistemas implementados. Não se limita a mudar sprites, cores ou posições.

Para cada indicador, acompanhar o ciclo inteiro: origem do valor, atualização por tempo/estado, ações que consomem ou recuperam, efeitos no personagem, limites, salvamento/carregamento e representação visual. Identificar a causa dos problemas de mecânica relatados antes de corrigir. Uma textura que aparece corretamente não comprova que o sistema funciona.

Para cada menu, conferir navegação, hierarquia, entradas, ações reais, atalhos, modal/pausa, layout e comportamento em diferentes situações e resoluções. Preservar F2 para o celular, teleporte do F11 e personalizações do usuário.

A referência é a PSRP.dll fornecida, cruzada com YTD/OIV/DLC instalados quando necessários à apresentação. Registrar o caminho entre menus, handlers, estados e arquivos usados pela referência e confrontar com o RPCore. Metadados/decompilação são evidência estática; o comportamento final precisa ser validado no GTA.

Critério de entrega: mecânicas e HUD coerentes entre si, ações de menu completas, valores/efeitos/persistência testados e roteiro de verificação no jogo para cada grupo corrigido.

## Fase 9 — refinamento de todos os trabalhos e atividades

Abrange todos os trabalhos implementados até lá, incluindo os atuais de pizza, Post OP, Uber, valet, reposição de lojas, combustível, transporte prisional e piloto; caça, pesca, crafting/feira e tráfico; e os sistemas que forem acrescentados nas fases seguintes. O refinamento não deve ficar restrito a um ou dois trabalhos.

Antes de alterar cada trabalho, estudar a implementação correspondente na PSRP.dll: inicialização/chamadas, menus, requisitos, estados/transições, posições, veículos, NPCs, objetos/animações, interações, progressão, pagamentos, eventos/riscos, falhas e encerramento. Confrontar isso com o ciclo atual do RPCore e registrar as diferenças concretas, inclusive o que ainda não estiver implementado. Não presumir que um método/classe presente na referência equivale a uma função completa ou ligada ao jogo.

Usar essa comparação para implementar, com código próprio, as interações e etapas que tornem o trabalho mais completo e coerente com a referência. Não tratar alterações apenas de pagamento, coordenadas ou prazo como refinamento completo quando faltarem etapas de gameplay. Diferenças deliberadas de arquitetura/compatibilidade devem ser documentadas.

Entregar em subfases por trabalho/grupo, com DLL compilada, testes pertinentes e teste real pelo usuário: começar, cumprir todas as etapas, receber corretamente, repetir, cancelar, falhar, morrer e trocar de perfil. Conferir limpeza somente dos recursos próprios, compatibilidade com mods e continuidade dos saves. Preservar dinheiro, estoque, licenças, reputações e demais progressos existentes.

## Regras comuns

- Varrer a PSRP.dll e os recursos pertinentes antes de implementar os refinamentos; reutilizar a análise já feita e aprofundar os métodos de cada sistema.
- Separar evidência encontrada na referência, comportamento confirmado no GTA e decisão de implementação do RPCore.
- Registrar o que mudou, diferenças restantes e exatamente o que o usuário precisa testar.
- Manter arquitetura, saves, configurações e recursos instalados; não reiniciar o progresso para viabilizar um ajuste.
- O aceite do ciclo básico de uma fase anterior não remove seu trabalho da revisão da fase 9.

## Aceite mais recente

Núcleo PDM 0.19.0 testado/aprovado pelo usuário, com pedido explícito de refinamento posterior. A 0.20.0 acrescenta coleta de um/dois pedidos pagos e uma exposição externa; não conclui toda a empresa nem antecipa fases 8/9. Incluir logística/empresa/cliente no refinamento dos trabalhos da fase 9.

Continuação 0.21.0: gestão de mecânico/divulgação, salários/acerto e melhorias econômicas. Incluir na fase 9 comparação da IA dos funcionários, calendário/dívidas/efeitos e serviços completos; esta subentrega não declara IA física ou paridade empresarial completa. Pedido de avançar após a 0.20.0 não substitui teste detalhado da logística.

Continuação 0.22.0: coleta física de valores da empresa com Stockade, segurança/maleta, reserva/entrega/compensação e recuperação. Incluir chegada/embarque/navegação e diferenças de cofre/seguro/roubo na comparação da fase 9; não se declara a PDM inteira concluída.

Continuação 0.23.0: entrega financiada para clientes adimplentes, entrada/parcelas por tempo ativo e livro de contratos. Inadimplência/retomada não são declaradas implementadas nesta subfase; são a próxima entrega prevista da PDM. Comparar economia/calendário/interações/risco ao aprofundar a fase 9, mantendo a fase 8 para interfaces e mecânicas.

Continuação 0.24.0: risco apenas em novos financiamentos e retomada da variante estacionada, com Packer/TRFlat e retorno do bem ao estoque. Preserva risco zero da 0.23.0. Variantes com devedor dirigindo/confronto, negociação/cobrança e física detalhada de guincho/rampas ficam pendentes para subentregas/refinamento; não são declaradas concluídas. Incluir acesso aos locais, contabilidade de custo/saldo encerrado e danos na fase 9; interfaces na fase 8.

Continuação 0.25.0: fornecimento de dois carros à locadora, carga/entrega/buzina/funcionário/retorno/pagamento e recuperação. Não confundir com aluguel pessoal por diária. Fila aleatória de pedidos, áudio/diálogo e física detalhada continuam diferenças para refinamento; não se declara a PDM inteira concluída. Pedido “continue” durante o trabalho não comprova teste no GTA.

Continuação 0.26.0: showroom com 19 posições de referência, atribuição/temporários/preços, recolhimento seletivo e comprador na entrada externa. Câmeras, coberturas, venda automática por reputação e reformas físicas permanecem diferenças. Validar pisos/acesso por vaga, navegação/saída do comprador e integração de estoque na fase 9; revisão global de interfaces segue na fase 8.
