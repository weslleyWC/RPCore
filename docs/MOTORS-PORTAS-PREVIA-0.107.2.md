# 0.107.2 — acesso à Motors e prévia dos carros

O usuário confirmou que0.107.1 recuperou HUD, menus Motors e PDM. Restaram a entrada física bloqueada e o catálogo só com nomes.

## Portas

O código de portas ainda tratava IS_CUTSCENE_ACTIVE isoladamente como impedimento. Ele agora usa a mesma distinção de cena preparada/em execução da interface, mantendo bloqueio em missão, cena em execução/ativa sem controle e troca de personagem. Mantida a espera de quatro segundos com colisão/contexto estáveis antes de solicitar shr_int e retirar fakeint. Não interrompe cenas/scripts do GTA nem teleporta o jogador.

As quatro portas de pedestres já identificadas são destrancadas pelo sistema nativo existente; ao aproximar até2,5m, recebem abertura1 como na referência PSRP. Fora da aproximação preservam o ângulo atual válido. Não abre a porta de rolo/garagem nem portas de outras lojas. A confirmação de que isso corrige a entrada física depende do teste no GTA.

## Como ver antes de comprar

RPCore Motors > Catálogo > selecionar o modelo > **Ver veículo no showroom**. O modelo real é carregado sem bloquear o script, exposto no ponto interno de referência da PSRP e mostrado por câmera. **Girar carro /45 graus** permite examinar outros lados. **Voltar** encerra a prévia; **Escolher garagem e comprar** encerra a prévia e segue para a confirmação de compra existente. É possível visualizar sem possuir garagem ou ter dinheiro para comprar.

A prévia exige estar a pé perto da entrada, sem procurado/combate/missão/outra atividade ou câmera. Carregamento tem prazo de10s, incluindo interior e contexto prontos; visualização tem limite total de2min desde a solicitação. Fechar menu, voltar, afastar mais de5m, trocar perfil/ator, perder contexto ou outra câmera assumir encerra a demonstração. Quando ela termina com o menu ainda aberto e contexto válido, retorna ao detalhe do modelo.

A vaga precisa estar livre: não remove carro de cenário, de outro mod, de jogador ou do estoque PDM para abrir espaço. Modelo inválido, ausente ou grande demais para a vaga não é criado. Demonstração é um único carro temporário, trancado e imóvel, sem registro em garagem/estoque, pagamento ou seguro. Limpeza confere handle/modelo/endereço; não apaga entidade de outro dono. Se o carro temporário tiver sido ocupado/anexado por outro sistema, é liberado sem apagar ocupantes. A câmera própria só interrompe a renderização quando ainda é a dona; não para a câmera de outro sistema.

## Referência e preservação

Revisadas estaticamente PSRP.Dealerships (prévia, posição, direção e CreatePDMCamera) e MultiplayerCharacter.UnlockDoors (modelos e abertura). Código próprio: comando explícito, carregamento assíncrono e limpeza por identidade; não copia o comportamento PSRP de apagar o carro mais próximo nem sua espera síncrona. Não acrescenta test-drive ou todas as categorias PSRP neste hotfix.

O corpo existente de cotação/contexto/confirmação financeira/criação da entrega é idêntico à0.107.1. Banco, saves, catálogo/preços customizados, garagens, estoque/empresa PDM, personagens e correção do HUD preservados. Não precisa reinstalar OIV/RPF/YTD. Escopo e hashes em MOTORS-SCOPE-0.107.2.json.

## Testar no GTA

1. Instalar com GTA fechado. Ir à Motors e aguardar ao menos4s. Aproximar das entradas de pedestres e conferir abertura/interior, HUD, F11 e F2.
2. Abrir Motors pela tecla configurada no aviso; em Catálogo selecionar Asea, escolher Ver veículo, aguardar aparecer e girar. Voltar e repetir com outro carro e a Faggio. Dinheiro, garagem e estoque devem permanecer iguais.
3. Cancelar durante carregamento; fechar com tecla da Motors/Backspace; sair da região; reabrir. Não deve sobrar carro/câmera nem bloquear outros trabalhos. Conferir também retorno pelo controle.
4. Se a vaga já tiver carro, conferir aviso sem remover esse carro. Testar a prévia sem garagem/saldo e, depois, uma compra confirmada: preço debitado uma vez, entrega externa e registro na garagem como antes.
5. Reabrir a empresa PDM, F11, celular e inventário. Se a entrada continuar bloqueada, apertar F9 na porta e enviar RPCore.log da mesma sessão com imagem da entrada utilizada. Renderização, colisão e acesso nativos ainda precisam dessa validação local.

Build Release net48 limpo e4225 testes aprovados (36 novos de identidade, prazo, movimento, dimensões e aproximação de porta). Natives/câmera/modelos não executados no cloud. Correção de acesso implementada, mas não declarada validada dentro do GTA.
