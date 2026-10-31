# RPCore 0.108.0 — conjunto PDM e auxílio de testes

F11 → Cheats → Adicionar $1.000.000 substitui o crédito anterior de $1.000. Cada acionamento credita a carteira do perfil ativo e salva pelo caminho existente. Banco e caixa empresarial continuam separados; para capitalizar a PDM, deposite no banco e use Aportar na empresa.

## Acesso e controles

No escritório externo, abra Empresa PDM e entre em Operação / recursos da empresa. Também há acesso pelo contato PDM no celular e atalho 9 da página de serviços. Compras/contratos continuam exigindo presença, contexto e condições adequadas; o celular permite consultar e marcar destinos, sem liberar compras de qualquer lugar.

E continua nos pontos de serviço, engate e carregamento. Conversar com comprador/vendedor/equipe e entregar as chaves usa Y por padrão (controle: direcional direito). É configurável em Keys/PdmInteraction. O pulso é consumido uma única vez; o atendimento de comprador próximo tem prioridade sobre o escritório, evitando abrir o menu em vez de entregar a chave. End encerra teste, ferramentas e atividades manuais, preservando compras já confirmadas. No transporte antigo, segure End conforme o aviso.

Os postos de segurança agora abrem a contratação/gestão do cargo correto; GPS tem opção separada. Recusas de contratação mostram caixa faltante, folha/acertos ou requisito. Salários continuam pelo caixa empresarial; contratar não quita vencimentos futuros.

## Operação empresarial

- Fornecedor de oito categorias: Super, Esportivos, SUVs, Coupes, Muscle, Compactos, Off-road e Sedans. Modelos instalados são enumerados, verificados gradualmente e cotados por dia ativo, classe e quilometragem (1.000–25.000 mi). Ofertas indisponíveis ficam identificadas. Carrinho de até seis unidades, revisão e pagamento integral atômico; falha não deixa compra parcial. Modelos adicionais usam hash explícito quando não constam no enum do SDK.
- Coletor: contratar e manter salário em dia; orçamento por compra pode ser zerado. Compra novos carros com caixa empresarial e traz ao pátio. Também permite solicitar retirada de pedidos já pagos, sem recomprar. A viagem progride em tempo ativo, com caminhão/NPC/carga na chegada; o recebimento depende da chegada física. Packer + TRFlat próprios eliminam a taxa adicional de coleta de pedidos. Cancelamento conserva a unidade adquirida como encomenda paga. Destruição interrompe a operação e conserva carro/danos para reparo e nova retirada, sem reposição reparada gratuita.
- Frota: Packer $200.000, TRFlat $65.000 e TR2 $150.000. Packer + TR2 habilitam lotes de três a seis unidades no porto, com os seis pontos de fixação da referência. O fluxo anterior de uma/duas unidades permanece disponível, preservando os contratos existentes.
- Vendedor particular: carro e NPC no pátio, inspeção/test-drive, retorno estacionado, uma contraproposta e compra pelo caixa. Aparência, modificações, quilometragem e estado observado seguem no registro de estoque. Negociação exige proximidade do vendedor, não do escritório.
- Vendas da equipe: reputação, limpeza e humor influenciam a chance. Unidade exposta pode ser vendida e creditada atomicamente; unidade no estoque gera pedido de entrega ao cliente, com aviso no celular. Nesse pedido o pagamento só ocorre depois da entrega real. Reserva impede a locadora ou outra venda de consumir o mesmo carro. Entrega financiada encerra a reserva e abre o contrato existente.
- Locadora: pedidos diários de duas unidades do mesmo modelo, associados aos contratos físicos existentes. Pedido consumido ao aceitar a entrega; pagamento, retorno do conjunto e recuperação continuam no fluxo já existente. A locadora recebe carros da PDM; quem traz compras à PDM é o coletor.
- Retomadas: opção estacionada, interceptação em circulação e devedor com resistência. Na circulação, aproxime-se e use Y/direcional para solicitar parada; na resistência, o devedor pode reagir quando você entra no carro. As variantes usam o mesmo contrato inadimplente e retorno pelo conjunto. Não criam outro patrimônio nem prêmio pela recuperação. É uma adaptação: o PSRP usa retorno direto em uma das variantes.

## Instalações, manutenção e divulgação

Vidraça, iluminação nos pontos da referência, lavadora e bomba são recursos físicos comprados pelo caixa. Vidro e luzes também melhoram o índice de segurança. Cofre tem representação visual aberta/fechada; câmeras, defesa local, seguro, transporte de valores e os contratos anteriores permanecem integrados.

Oficina prepara apenas um carro empresarial reconhecido no box. Lavagem com mangueira/efeito de água exige proximidade e ação contínua; abastecimento considera o tanque do modelo e cobra ao concluir. Modificações exigem mecânico em dia e oficina: peças suportadas, opcionais, películas, pinturas especiais disponíveis e cores. Falha na gravação restaura a aparência anterior; cancelar ferramenta não conclui o serviço. End recolhe o carro preparado. Modificações são reaplicadas ao expor/retirar/entregar, sem alterar carros pessoais.

Lixo surge em quatro posições persistentes. Recolha uma sacola com Y e leve à caçamba; End devolve sem apagar a sujeira. Zeladoria contratada e em dia também caminha até o lixo e o limpa. Limpeza melhora o ambiente. Ao fechar/à noite, capas aparecem nas unidades externas reconhecidas e restauram suas condições anteriores ao remover; visitantes são NPCs próprios.

Showroom mantém as vagas existentes e acrescenta ajuste limitado de posição/altura/rotação com verificação de colisão, persistência e cartão com quilometragem. Não move veículos de terceiros nem apaga carros/pedestres do mapa.

Cartões precisam ser entregues a pessoas distintas para concluir campanha e ganhar reputação. Doações debitam agora e geram publicidade no próximo dia empresarial. Patrocínio cobra inicialmente e renova semanalmente durante os dez dias empresariais, suspende sem caixa e pode ser encerrado. Humor, limpeza, reputação, despesas e valor estimado aparecem na gestão. Demitir toda a equipe mantém salários, proporcionais e rescisões como acertos a pagar.

Tempos são de jogo ativo (dia padrão: 1.800 segundos), sem cobrança por horas offline. Opções novas ficam em Jobs/PdmEnterprise.json; preços/salários/pontos anteriores personalizados são mantidos. O campo opcional Enterprise e os novos metadados do estoque não exigem apagar Wallet, personagens, roupas ou inventário. Nenhuma compensação automática de dinheiro de bugs antigos.

## Comparação com PSRP.dll

A matriz PDM-REFERENCE-MATRIX-0.108.0.json relaciona as famílias e métodos de PDMCompany às implementações RPCore. A referência foi consultada para regras, posições, modelos e ciclos. A entrega implementa as famílias listadas; não declara equivalência binária, reprodução de todos os detalhes visuais ou aprovação de gameplay sem rodar GTA.

Mantidas diferenças deliberadas: preços anteriores configuráveis da empresa, nenhum capital aleatório gratuito, contexto seguro, gravação atômica, tempo ativo, estoque identificado, busca de vaga livre e limpeza somente de entidades próprias. Não copia World.Clear, exclusão global de geradores, esperas bloqueantes, teleporte forçado do caminhão nem suspensão de polícia da referência.

## Validação e teste local

4340 testes aprovados (58 novos casos), Release net48 sem erros/avisos. Cobertura nova: débitos únicos, rollback de IO, coleta sequencial, orçamento, preservação de compra/danos, seis cargas, reservas/vendas/financiamento, equipe/folha, campanhas e timers, serviços/snapshots, legado, validação e input. O GTA não está disponível neste ambiente; direção de NPC, engate/TR2, interior/colisão, partículas, animações e apresentação precisam do teste local.

Feche o GTA, instale o ZIP e confirme 0.108.0 no log. Não apague os arquivos de personagem nem recompre pedidos antigos. Teste em grupo: F11/crédito e aporte; segurança/folha; fornecedor/coletor e cancelamento; um lote pequeno e depois seis carros; vendedor/inspeção; venda/reserva/locadora; lavagem/bomba/tuning; campanhas/lixo/capas; retomadas. Termine cada contrato antes de iniciar outro e confira caixa/estoque após sair e abrir novamente. O instalador é validado duas vezes sobre a versão anterior e preserva configurações/saves existentes.
