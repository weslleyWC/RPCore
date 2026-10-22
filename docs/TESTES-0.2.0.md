# Teste da DLL 0.2.0 no GTA Legacy

A base anterior foi confirmada em jogo pelo usuário. Este roteiro cobre as adições atuais; a compilação/testes de dados não executaram o GTA. Não altere seus saves para começar: use o instalador e confirme o backup fora de `scripts`.

## Atualizar

1. Feche o GTA. Baixe `RPCore-update-0.2.0.zip` da branch `rpcore-entrega` e extraia fora da pasta de scripts.
2. Execute `Install-RPCore.ps1 -GtaPath 'D:\Games\Grand Theft Auto V'` pelo PowerShell nessa pasta. Ele conserva Settings.ini, Keys.ini e todos os saves existentes, copia os novos arquivos ausentes e faz backup da versão anterior.
3. Inicie modo história. F10 deve indicar **0.2.0** e o log deve registrar **15 sistemas**, se todos estiverem habilitados e inicializados. Confirme que inventário, pizza, conquistas, cinto e perfil da base continuam funcionando.
4. Se já usa outro mod de combustível, edite o novo `scripts/RPCore/Vehicles.ini`: `[Features] Fuel = False` e reinicie o script. `Controls` e `Garages` também podem ser desligados independentemente; a concessionária acompanha `Garages`.

## A — Motor, setas, cruise e menu

1. Entre como motorista em um carro comum. X deve desligar/ligar o motor uma vez por pressionamento. K abre o menu; pare e teste capô e porta-malas. Sair do veículo deve fechar o menu.
2. B: seta esquerda; N: direita; H: alerta. Repetir desliga. Teste sair, mudar de carro, trocar de personagem e recarregar o script; o RPCore deve restaurar os indicadores anteriores e liberar o controle de auto-start que alterou.
3. Dirija acima de 29 km/h em rua plana e pressione C sem segurar o acelerador. Deve manter aproximadamente a velocidade por aceleração. Freie para cancelar. Repita cancelando com acelerador, C, freio de mão, colisão leve, menu, motor desligado e saída do veículo. Não teste primeiro junto a tráfego denso. O cruise não dirige nem freia automaticamente em descidas.
4. Teste como passageiro e em bicicleta: as ações de motorista não devem ser aplicadas. Durante uma missão GTA ativa, estes controles ficam suspensos.

## B — Posto e combustível

1. Vá ao posto da Little Seoul, perto de **(-724.6, -935.2, 19.2)**; use o blip. Pare dentro da área, desligue motor com X e pressione G. O HUD deve mostrar os litros subindo e o dinheiro caindo. G interrompe. Confira o som e a conquista de primeiro abastecimento.
2. Ligue o motor ou afaste-se durante o abastecimento: deve parar. Tente com motor ligado, carro andando, saldo insuficiente e tanque cheio: não deve haver cobrança indevida. Pode parar a pé a até 5 m do carro e iniciar G, se ele estiver vazio e desligado.
3. Rode e confira pequena queda de combustível. Pausar/carregar não deve consumir o tempo inteiro como direção. F10 → Save now, recarregue e confira tanque/distância por placa e perfil.
4. Para acelerar o teste **numa instalação de teste**, aumente temporariamente `LitersPer100Km` em Vehicles.ini (máximo 100) e use um carro com pouco combustível. Ao zerar, o motor deve parar; reabastecer no posto deve permitir ligar novamente. Restaure o consumo depois.
5. Teste elétrico e bicicleta: não devem receber o consumo nem bloqueio RPCore. A lista original ExcludedGasList.json é respeitada. Teste outro carro com outra placa: tanque independente.
6. Se o marcador/raio de algum posto não estiver adequado ao seu mapa, use F9 e informe as coordenadas; são editáveis em Fuel/Stations.json. Verifique alturas e se o raio não permite abastecer do outro lado de uma parede.

## C — Comprar e usar garagem

1. F11 longe de garagem deve oferecer GPS. Vá a Strawberry **(-79.8, -1826.4, 26.9)**; preço padrão $10.000 e 3 vagas. Comprar longe ou sem dinheiro não deve cobrar. Comprar novamente não deve cobrar de novo. Se necessário, configure preço menor na sua cópia de teste de Garages/Locations.json antes de reiniciar.
2. Como motorista, pare um carro/moto dentro da área, sem passageiros ou reboque. F11 → Store current vehicle. O script pede saída e aguarda até 15 segundos. Aguarde a notificação de armazenamento; o veículo deve desaparecer somente depois do save. Afaste-se ou volte ao veículo durante o processo: deve cancelar/aguardar sem perda de veículo.
3. Veículo reservado por missão/outro script é protegido por padrão. Para testar um carro do trainer, prefira primeiro comprar um carro na concessionária RPCore. Ative AllowMissionVehicles apenas se souber que o carro reservado pode ser entregue ao RPCore. Missões GTA ativas continuam bloqueadas.
4. A pé e com a saída livre, F11 → Retrieve. Deve criar um único veículo na saída **(-73.3, -1834.0, 26.8)**, com a placa/pintura/mods salvos, combustível e danos suportados. Entrar nele fica a seu cargo; o script não teleporta o jogador.
5. Acione Locate/recover enquanto ele já está no mundo: deve marcar GPS e não criar outra cópia. Guarde novamente; repetir não deve ocupar mais vagas. Encha a garagem; uma vaga conta também para carros associados que estão fora. Guardar outro carro deve ser recusado sem apagá-lo.
6. Modifique pintura RGB, rodas e motor; ative neon, fure pneu, quebre vidro/porta e suje o carro. Guarde/retire e compare. Geometria exata dos amassados/marcas de bala não é preservada.
7. F10 → Save now, reinicie o script e depois o jogo. Confira garagens, veículos e combustível. Para carro que ficou fora, Locate/recover procura primeiro no mundo e só recria se não existir. O estado é o último snapshot salvo; não há seguro implementado.
8. Num addon removido temporariamente, Retrieve deve avisar que o modelo está ausente e preservar o registro. Restaurar o addon deve permitir nova tentativa. Não remova addons/saves da instalação principal para esse teste.
9. Na garagem própria, parado como motorista, teste Repair ($500) e Wash ($25). Combustível não deve aumentar ao reparar. Sem saldo, o serviço deve ser recusado.

## D — Comprar veículo

1. Tenha garagem com vaga. F12 longe do PDM oferece GPS para **(-56.8, -1096.7, 26.4)**. Perto da entrada, abre o catálogo.
2. Compre Faggio ($3.500) ou outro modelo com saldo. Deve cobrar uma vez, registrar na primeira garagem sua com vaga e entregar fora em **(-28.5, -1083.5, 26.6)**, com placa RP. A conquista de primeiro carro deve avançar.
3. Teste falta de dinheiro, todas as vagas ocupadas e saída bloqueada: nenhuma cobrança/veículo novo. Um modelo inexistente adicionado ao catálogo deve avisar sem cobrar.
4. Guarde o veículo comprado, recupere e reinicie. Confira dinheiro, registro, placa, combustível e personalização. Repita com outro modelo: placas não devem coincidir.

## Reportar resultado

Informe A/B/C/D, ação, resultado esperado/observado e personagem/modelo usado. Em falha, envie RPCore.log e o log do SHVDN. Guarde o backup produzido pelo instalador. A versão anterior continua disponível no commit `68191401248ec0282dd0d6c50ad96c07625f8790` do repositório.

Esta entrega acrescenta o conjunto de veículos acima. Casas, telefone, empregos adicionais, polícia, seguro, gangues/territórios avançados e gestão empresarial PDM continuam no trabalho restante; não foram apresentados como concluídos.
