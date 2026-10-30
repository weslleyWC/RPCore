# Grupo abastecimento e cinto — 0.105.0

Revisão conjunta de FuelSystem e SeatbeltSystem, seus acessos, acompanhamento, pagamento/persistência e interrupções. Referência estática: PSRP.Vehicle_Insurance abastecimento/total/som/interrupção e PSRP.DrivingFeatures.ToggleSeatbelt/UiSystem.CarSeatbeltIcon. O RPCore mantém litros reais, pagamento pela carteira e dados próprios, em vez da escala de270/gallons da referência. Não muda personagens/roupas nem os ciclos de trabalhos da fase9.

## Abastecimento

Atalho padrão **G**, ou a tecla Refuel configurada. Estacione em um posto marcado, desligue o motor e fique próximo do carro/moto. Pode abrir a pé ou no banco do motorista, como já era permitido. Dá preferência ao veículo atual/último próximo; não abastece carro com outro motorista. Elétricos, bicicletas e modelos excluídos continuam fora do sistema.

O menu traz tanque/capacidade, preço por litro, dinheiro da carteira, completar conforme o saldo, informar litros ou valor máximo. Litros aceitam ponto ou vírgula e até2casas, ex10,5; valor é inteiro. A confirmação começa em Cancelar e mostra limite do pedido. Mudança de saldo/preço/tanque/contexto exige conferir novamente. Os26postos e suas coordenadas, pontos Fuel do editor e preferências locais são mantidos.

A entrega ocorre por pulsos de no máximo uma atualização por segundo; não acumula combustível por tempo em pausa/loading/lag. Mostra litros entregues/pedido e custo acumulado. **G ou voltar** interrompe; mover/ligar o carro, se afastar, mudar de ator/perfil/modelo/placa, entrar outra pessoa como motorista, cutscene/missão ou perder controle/interação também interrompem. Cobrado somente o entregue; saldo/combustível já confirmados permanecem. O resumo final informa litros e total pago.

Cada pulso salva tanque e carteira no mesmo checkpoint Wallet. Falha de gravação restaura tanque/saldo antes de publicar o avanço da sessão. Falha também na restauração gera exceção agregada/log e não é apresentada como sucesso. Não há retomada automática de pedido após reiniciar o mod/jogo.

O preço acumulado é o teto do valor total entregue, sem arredondar cada pulso separadamente. Exemplo com1L a$7 em10pulsos de0,1L: total$7, e não$10. Uma fração adicional pode não exigir nova cobrança quando já coberta pelo arredondamento anterior; não cria combustível sem pagamento correspondente. Valor máximo e saldo limitam o pedido. Mantém o preço inteiro configurado e a precisão de0,01L da compra.

Tanques continuam em Transport.Tanks por modelo/placa normalizada, com capacidade e hodômetro existentes; FuelTank.Quote/Drive e as taxas de consumo não mudam. A leitura usa referência canônica atual do Wallet, evitando continuar num tanque antigo após outra transação veicular. Validação completa ocorre ao trocar essa referência, evitando percorrer todo o transporte a cada frame. Tanque vazio mantém bloqueio de motor/aceleração anterior; carro elétrico continua com comportamento nativo.

A sessão observa a atividade atual e não rouba/libera a reserva de trabalhos. Assim pode abastecer durante contrato de motorista/entrega sem cancelá-lo. Operações de personagem/sono/consumíveis/serviços pessoais são excluídas; trocar a atividade encerra o abastecimento. O áudio usa o PetrolPump.wav já instalado em loop e para ao encerrar. Falha de áudio/conquista não repete cobrança nem mantém o serviço ocupado.

Arquivo de exclusões inválido é preservado e a simulação automática de combustível é suspensa nesse caso, sem sobrescrever o arquivo ou cobrar modelos desconhecidos. Restaurar o arquivo válido e reiniciar o mod reativa o comportamento normal. Não há mudança automática das configurações válidas do usuário.

## Cinto

Mantém tecla configurada, acesso pelo radial/menu do veículo e ícone PSRP. Funciona em carros/caminhões/ônibus suportados pelo modelo IsCar, inclusive como passageiro; moto/bicicleta/barco/aeronave não recebem cinto. Avisos agora mostram a tecla real em português.

Ao colocar, captura o flag32 original e impede ejeção pelo para-brisa pelo caminho nativo. Enquanto colocado, bloqueia o comando de sair; ao tentar sair, informa a tecla para soltar o cinto. Sai/troca carro, troca ped/perfil/modelo ou inicia cutscene/missão: libera o estado pertencente à sessão. A identificação inclui ator, modelo, endereço da entidade e veículo, impedindo restaurar flag em handle reaproveitado.

A restauração exige a mesma entidade e o valor false que o RPCore aplicou; se outro sistema já mudou o flag para true, não o força de volta ao valor antigo. Mantém o original em vez de impor sempre true ao soltar, preservando outras configurações verificáveis. Não simula colisão/ragdoll/ejeção própria nem garante previsão de acidente: a física de colisão continua a do GTA e mods instalados. Cinto não é salvo entre veículos/sessões. Falha de conquista não desativa o cinto.

## Teste no GTA

1. Ir de carro a um posto, desligar motor e usarG. Testar Cancelar, completar,10,5L e orçamento. Conferir carteira antes/depois, tanque/HUD e resumo. Se saldo/espaco limita, deve entregar só o que cabe/pode pagar.
2. Interromper comG/B/Escape, afastar-se ou ligar/mover o carro. Não deve continuar som/cobrança. Reabrir deve mostrar tanque atualizado; fechar/reabrir GTA deve preservar combustível e carteira sem duplicar banco.
3. Testar motorista e a pé; elétrico/bicicleta e carro ocupado por outro motorista devem ser recusados. Verificar um posto personalizado do editor se usar.
4. Durante um contrato de entrega/motorista, abastecer sem cancelar o trabalho; conferir continuidade e pagamento normal do trabalho. Refinamento dos ciclos do trabalho fica na fase9.
5. Colocar/soltar cinto pelo atalho e radial, conferir ícone/som e tentativa de sair. Trocar carro, sair e voltar, trocar perfil ou reiniciar plugins: não pode ficar bloqueado nem herdar cinto em outro carro. Conferir como passageiro se usar.
6. Conferir personagem/roupa0.104.5, armazenamento, celularF2 e menus existentes. Não se declara que roupa já foi aprovada pelo usuário só porque pediu avançar.

4038 testes aprovados (3996 anteriores +42), Release net48 sem erros/avisos. Testes de dados verificam pagamento/rollback/rounding/limites/reentrância/identidade/flags; o ambiente cloud não executa abastecimento, áudio, HUD ou acidentes no GTA. Roteiro acima é necessário para validar comportamento nativo. Nenhuma nova dependência/asset/save schema ou configuração obrigatória foi adicionada.
