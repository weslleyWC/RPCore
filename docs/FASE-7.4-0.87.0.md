# 0.87.0 — personalização de veículos e revisão de paridade

A oficina passa a oferecer personalização funcional além de reparo/lavagem. A comparação estática de `MechanicShop.cs` confirma peças, desempenho, rodas, pinturas, equipamentos e cobranças bancárias na PSRP. Implementação própria, sem executar a DLL de referência.

## Teste no GTA

1. Leve um carro pessoal a uma garagem comprada, parado como motorista, sem passageiros/procurado/missão. Abra pelo atalho de garagem e escolha **Personalizar veículo**. Alternativamente use uma oficina salva no Map Builder e pressione E.
2. Instale uma peça, turbo, rodas e uma cor. Somente Enter compra; navegar não altera nada. Confira o preço indicado e o débito no banco. Recomprar a mesma opção não cobra. As ações de ligar/desligar equipamentos cobram por cada inversão.
3. Confira pintura secundária/perolado/interior/painel, vidros, estilo de placa, liveries, neon/lados/cores/fumaça, extras e proteção de pneus. Slots de peças só aparecem quando o modelo declara opções. Natives de equipamentos podem recusar opções em veículos sem suporte; a verificação deve evitar cobrança.
4. Em carro já cadastrado, guarde/retire, recarregue e teste a recuperação pelo seguro: peças/cores/proteção devem persistir. Carro ainda não cadastrado recebe alterações no mundo, mas precisa ser guardado para persistir após desaparecer. Não há cadastro gratuito automático.
5. Tente sem saldo, com passageiros, em missão/trabalho, saindo do carro/local ou trocando perfil: a compra deve ser bloqueada/fechada. Confira que o próximo trabalho consegue adquirir o gate. Fechar por F11/telefone não deve deixar a oficina reservada.
6. Confira combustível, danos e os serviços antigos de reparo/lavagem, F2, teleporte F11 e oficinas do editor. Teste carro base, moto e um veículo modificado. Um modelo que não suporta um equipamento não deve ser considerado aprovado por testes de dados.

Pagamento e snapshot do carro cadastrado são gravados juntos no Wallet. Erros normais de aplicação/gravação desfazem banco/registro/opção; arquivo JSON e estado nativo não são uma transação resistente a encerramento abrupto do GTA. Não foi possível executar GTA aqui.

## Diferenças explícitas

Não copia portas do ambiente, invencibilidade, burnout, polícia ignorando jogador, câmera ou previews da PSRP. Não instala automaticamente seis oficinas nem converte MapBuilderData antigo. Não altera o texto da placa, preservando identidade de garagem/seguro. RGB livre, seleção de cor xenon e pneus de drift não estão nesta versão; há paletas, xenon ligado/desligado e proteção de pneus. Preços próprios indicados antes da compra; não declara equivalência de todos os preços/opções da PSRP.

A revisão encontrou também ausências inteiras: chamar táxi/limousine **como passageiro** (não é o trabalho Uber) e controles de clima/tempo. Esses blocos ainda precisam implementação; não serão chamados de refinamento concluído. Ver `REVISAO-PARIDADE-0.87.0.md`. Fase8 continua reservada para interfaces/mecânicas e9 para realismo dos trabalhos.
