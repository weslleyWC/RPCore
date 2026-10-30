# Concessionária e seguro — 0.106.0

Grupo completo dos dois serviços existentes: navegação, acesso, confirmação, compra/registro e pedidos de seguro. Referência estática: PSRP.Dealerships (catálogo, seleção, preço e entrega) e PSRP.Vehicle_Insurance (cadastro, GPS, gerenciamento e franquia de $10000). Implementação própria sobre os saves RPCore; nenhum código PSRP é executado pelo mod.

## Concessionária

Abra pela tecla Dealership configurada (padrão F12) ou pelo atalho Concessionária/GPS no F11. A compra exige estar a pé, próximo do marcador RPCore Motors, fora de missão/procurado/combate e com controle do personagem. Longe da loja, o menu oferece GPS. Catálogo tem busca por nome/modelo, ordenação por nome/preço e páginas de dez ofertas. Os cinco modelos/preços padrão e o arquivo de ofertas personalizado foram preservados.

Selecione uma oferta, depois a garagem de sua propriedade. O menu mostra vagas livres; uma vaga inclui também veículos retirados vinculados à garagem. A confirmação começa em Cancelar e informa preço, carteira e garagem. É pagamento pela carteira, como já funcionava no RPCore; não muda para banco nem contrata seguro automaticamente.

Depois da confirmação há uma preparação cancelável por até dez segundos, sem laço bloqueando o script. Cancelar, fechar menu, sair da região, entrar em veículo, missão/cutscene, trocar ator/perfil/modelo, perder controle ou mudar o saldo/vaga/carteira canônica interrompe antes da compra. O pátio é verificado novamente após carregar o modelo. Não apaga carros/pedestres para liberar espaço. Modelo inválido, indisponível ou criação sem identidade não cobram.

O carro criado recebe placa livre nas identidades carregadas/salvas e nos tanques. Um checkpoint Wallet grava dinheiro, registro na garagem escolhida e tanque inicial juntos. Snapshot é copiado para não depender de alterações posteriores no objeto. Uma confirmação não autoriza outra tentativa após erro/duplo clique. Falha de gravação restaura saldo e remove apenas o registro/tanque da tentativa; falha também na restauração é registrada como incerta, nunca apresentada como sucesso. A limpeza de uma tentativa abortada exige entidade criada, mesmo endereço/modelo e sem ocupantes/anexos. Conquista com erro depois do pagamento não refaz compra. Veículo comprado não é apagado ao fechar o menu.

## Seguro

Use Seguro veicular / recuperação em Servicos e trabalho do menu RPCore (CoreMenu configurado, padrão F10). Cadastro gratuito: motorista do seu carro/moto registrado, retirado da garagem, parado, saudável, sem passageiros ou anexos. Sem mensalidade, mantendo franquia definida no cadastro (padrão $10000) e limite do arquivo Jobs/VehicleInsurance.json. Modelos de trabalho/bicicletas continuam excluídos.

Lista paginada mostra placa, nome, guardado/retirado/pedido pendente, limite e banco. Cada apólice mantém GPS, recuperação e cancelamento. GPS usa entidade única carregada ou avisa que é apenas a última posição registrada. Não cria carro para localizar.

Recuperação exige ficar a pé a até 30m do mesmo veículo destruído, vazio, parado e desanexado. Agora abre confirmação com banco antes/depois e Cancelar primeiro. Valida novamente banco/apólice/pedido e identidade física antes de iniciar. Franquia continua saindo do banco. Reparo continua no local, no mesmo veículo, preservando combustível e óleo; não adiciona entrega/guincho/indenização/cópia. Regras de transação e formato dos saves existentes permanecem iguais.

Pedido pago interrompido pode ser retomado sem nova cobrança. Antes de autorizar reparo, cancelamento abre confirmação de estorno; depois de iniciado, só encerramento explícito sem estorno. Cancelar apólice, abandonar e estornar têm Voltar primeiro e recusam confirmação de outro estado/saldo/pedido. Nenhuma dessas ações apaga garagem/porta-malas. Navegação usa a fila de ações dos outros serviços, após o processamento LemonUI, com proteção contra tecla segurada/dupla seleção e troca de personagem. Reload do seguro não libera reservas de atividades que ele não adquiriu.

## Testes no GTA

1. Fechar GTA e instalar a atualização preservando arquivos atuais. Abrir concessionária longe: GPS. Ir a pé ao marcador, buscar e ordenar catálogo; conferir ofertas/preços personalizados.
2. Ter duas garagens, uma lotada: escolher a outra. Cancelar confirmação não deve cobrar/criar. Confirmar uma compra deve descontar uma vez, entregar fora da loja e registrar na garagem escolhida, com combustível correto. Guardar/retirar e reiniciar GTA devem preservar placa, tanque e dinheiro.
3. Bloquear o pátio com um carro: compra recusada sem apagar ninguém. Durante preparação, cancelar/fechar/afastar-se: sem débito ou carro solto. Modelo customizado ausente: mensagem sem cobrança. Tentar Enter segurado: nenhuma compra duplicada.
4. Cadastrar seguro grátis como motorista, conferir franquia e GPS. Sair do carro e testar um destruído próprio vazio: Cancelar primeiro deve manter banco; confirmar deve cobrar uma vez e reparar o mesmo carro, conservando combustível/óleo.
5. Carro saudável, guardado, de trabalho, ocupado, distante ou saldo insuficiente: nada deve ser cobrado. Cancelar apólice mantendo carro/garagem/itens. Se houver pedido pendente, retomar sem nova cobrança; estorno só antes de iniciar reparo, mediante confirmação.
6. Voltar/B/Escape em todas as páginas, troca de perfil e reabrir serviços. Conferir banco no celular, roupas/personagens, abastecimento/cinto e um contrato existente.

4079 testes aprovados (4038 anteriores +41), Release net48 sem erros/avisos. Casos novos cobrem compra atômica, IO, setter de dinheiro parcialmente falho, falha na restauração, reentrância/repetição, identidade, saldo/vaga/garagem alterados, tanques inválidos/duplicados e confirmação do seguro obsoleta. Natives, entrega física e interfaces não são executadas pelos testes cloud. Testes existentes do seguro continuam cobrindo débito, pedido, reparo lógico, retomada, estorno e rollback.

Não se declara paridade total com a referência: catálogo amplo PSRP, previews/câmera/test-drive, vendedores e entrega/recuperação no estacionamento da seguradora não foram adicionados nesta revisão. Mantém os contratos e a arquitetura do RPCore; refinamento de realismo dos trabalhos permanece na fase9. Personagens continuam na solução estável e roupas0.104.5 não foram alteradas.
