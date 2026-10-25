# RPCore 0.47.0 — fase 6.5E: iniciação de direção

## Instalação e acesso

Baixe e extraia RPCore-update-0.47.0.zip da branch rpcore-entrega. Abra PowerShell na pasta que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Mantém GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Não reinstala .oiv/.rpf nem carrega PSRP.dll junto. Gangs/Driving.json é acrescentado quando ausente; mantém presença/equipe/lealdade/força, saves/configurações, DMV C/M1/A e F2/F11.

**F4 → presença ligada → GPS da gangue → Iniciacao por direcao: nome**. Exige personagem sem filiação, a pé a até 3 m do líder vivo próprio, sem procurado/combate/missão/cutscene/interface modal. Precisa de modelos de membros na gangue escolhida e modelos válidos em outra gangue para o alvo rival. As definições originais já têm outras gangues; listas personalizadas devem conservar pelo menos uma rival válida.

Não pode haver outra iniciação pendente nem atividade/exame reservando a trava compartilhada. Para preservar sua filiação/reputação principal, teste com outro perfil/personagem sem filiação. Join continua disponível sem pendência; não cria requisito retroativo para filiados existentes.

## Integrante e carro

A preparação carrega os dois modelos de forma assíncrona e procura piso/espaço para **um integrante próprio**, perto de você. Fique junto ao líder até terminar. Só registra início após criar o integrante com sucesso; se modelo/local não estiver pronto, não conta tentativa nova.

Use preferencialmente um **sedã convencional com janelas**, vazio, parado e em boas condições. Entre como motorista e deixe o **banco dianteiro do passageiro livre**. O integrante segue você a pé e entra fisicamente quando o carro está parado abaixo de 0,5 m/s e a até 25 m dele. Aguarde o embarque antes de acelerar. Não teleporta para o banco, não destranca portas nem expulsa ocupantes: usa DontJackAnyone/BlockSeatShuffling. Motos/aeronaves/barcos e veículos reservados de trabalho/exame não são aceitos.

Ao confirmar o passageiro, fixa a referência desse carro para a missão. Até voltar à gangue, exige **você como motorista e o integrante no banco dianteiro do mesmo carro**. Trocar de carro, desembarcar ou perder o integrante reprova; é uma regra mais estrita que a referência, para manter o ciclo verificável. O mod não compra, move, repara, abastece, reserva como carro empresarial ou apaga esse veículo. Sua propriedade/garagem/combustível continuam nos sistemas anteriores.

## Alvo, ataque e fuga

1. Com integrante embarcado, siga a rota para um dos **25 pontos de alvo da PSRP**, guardado no snapshot da tentativa. O alvo usa modelo de outra gangue. Não escolhe ped nativo/aleatório do mundo.
2. O alvo próprio aparece a até **115 m**, com chão/espaço carregados. Se chegar em cima do marcador com o carro, afaste-o alguns metros para liberar o ponto. Ponto bloqueado aguarda nova tentativa e avisa; não remove terceiros ou força piso/interior.
3. Aproxime o carro a até **55 m do ponto**, com janela/visada adequadas. **O integrante atira; você dirige.** Ele recebe MicroSMG/1500 munições para esta missão, como na referência. O alvo tem 200 de vida, sem armadura/arma/invencibilidade. Tarefa de tiro é direcionada apenas ao alvo próprio. Não concede arma/munição ao jogador nem deixa dinheiro/armas para saquear.
4. **Não atire nem atropele o alvo.** Qualquer dano nativo atribuído ao jogador ou ao carro reprova. A morte só passa quando GET_PED_SOURCE_OF_DEATH aponta exatamente para o integrante próprio. Morte por terceiro, desaparecimento ou sair além de **160 m** antes da eliminação também reprovam.
5. Após o integrante eliminar o alvo, ele para o ataque e perde a arma temporária. Dirija com ele até ficar a pelo menos **150 m do ponto do alvo**. Essa etapa é física; não use teleporte durante a missão.
6. A rota então marca a HQ. Volte com o mesmo carro/integrante, **perca o procurado naturalmente** e estacione: jogador a até **15 m da HQ**, integrante até **20 m**, ambos no carro e velocidade abaixo de 0,5 m/s. Não há imunidade, limpeza de procurado ou mudança da polícia.
7. O retorno grava **aprovação pendente** e retira somente os atores próprios. Desembarque, vá ao líder vivo e pressione **E**, sem menus/procurado/combate, para confirmar a filiação uma vez. Sem dinheiro/arma/item de recompensa e sem cutscene forçada.

O mod cria dois grupos de relação temporários para seus dois NPCs e define hostilidade apenas entre esses grupos. Não muda relações dos grupos nativos, polícia ou demais gangues. Durante a missão, seus NPCs são mortais. Perda/morte do jogador/integrante, carro inválido, missão/cutscene, abandono do integrante além de 180 m ou limite padrão de **20 minutos ativos** reprovam. Pausa não conta. Procurado pode surgir durante o ataque/fuga e deve ser perdido antes de aprovar o retorno.

## Suspensão, retomada e saves

**END** ou **F4 → Suspender iniciacao de direcao** remove os atores/ícones próprios e libera a trava, conservando a tentativa. Reload, troca de perfil ou desligar presença também suspendem. Não salva handles de NPCs/carros. Se dispensar um integrante em veículo em movimento, retira suas tarefas/armas, restaura relação civil individual e libera o ped para o GTA, sem apagar/mover o carro.

Para retomar, ligue presença e vá a pé ao líder, sem filiação. **F4 → Retomar iniciacao de direcao**. Uma missão parcial refaz **embarque, alvo, fuga e retorno**, com atores novos, mesmos modelos/locais/regras/ID e sem contar outra tentativa ou taxa. Pode usar outro carro ao reiniciar o ciclo. O relógio recomeça, sem reparar o carro ou restaurar vida/armadura. A antiga referência do carro não é carregada do save.

Se o retorno já estava aprovado, retome só para confirmar E no líder, sem recriar alvo, ataque ou passageiro. **Cancelar iniciacao de direcao pendente** descarta a tentativa sem alterar sua filiação/reputação atual. Enquanto houver iniciação pendente, Join e edição de postos ficam protegidos; força/lealdade/direção não podem iniciar em paralelo.

Livro opcional em **GangState.json → Driving**, com etapas Boarding/Target/Escape/Return/Report, snapshots e recibos limitados. Grava por cópia antes de publicar; falha de save suspende preservando o último checkpoint. Confirmação final grava filiação e contador juntos. Metadados desconhecidos e livros anteriores são conservados. Gangs/Driving.json permite ajustar locais/limites com jogo fechado; dados inválidos usam defaults em memória sem substituir arquivo.

## Comparação e limites

PSRP GangSpawner StartDrivingInitiation/SpawnDrivingCrew/UpdateDrivingCrewBoarding/SpawnDrivingTargets/UpdateDrivingPassengerCombat/UpdateDrivingEscapeAndReturn/CompleteDrivingInitiation: integrante MicroSMG, alvo rival em 25 pontos, autoria da morte pelo integrante, reprovação por motorista/atropelamento, fuga150m e volta com integrante. Coordenadas conferidas em PSRP-SYSTEMS-AUDIT-0.47.0.json com índice das 75 fontes e hashes atuais.

Diferenças próprias: banco dianteiro/mesmo motorista e carro até retorno, carro parado e sem procurado para aprovar retorno, limite1200s, guarda de atividade/save/retomada e E no líder sem cutscene. Flags nativas de dano/morte não são histórico perfeito de interferência de outros scripts. Tiro pela janela depende do modelo, portas/vidros, lado e visada; não garante funcionamento em todo veículo add-on.

Não conclui diálogo completo com líder, territórios, guerras, negócios ou polícia. Interface/mecânicas continuam na fase 8; refinamento de todos os trabalhos na fase 9.

## Testes dentro do GTA

- Sem filiação e perto do líder, inicie e teste o embarque num sedã vazio/parado. Teste banco ocupado, portas travadas e tentativa de usar veículo de trabalho: sem expulsar ou teleportar ped, sem alterar/apagar carro.
- Vá ao alvo, dê visada ao passageiro e confira tiro, identificação da morte, fuga de 150 m e retorno juntos. Perca procurado, pare na HQ, desembarque e confirme E no líder; filiação/conclusão uma vez, sem recompensa duplicada.
- Teste dano do jogador/atropelamento, terceiro matando alvo, integrante morto, troca/desembarque do carro e abandono da área: reprovam sem filiação. Sua arma, dinheiro e carro não devem ser removidos/repostos.
- Suspenda após embarque/ataque/fuga e retome: repete o ciclo com mesmos locais/regras e total de tentativas. Suspenda após retorno aprovado e confirme depois de reload sem novo ataque.
- Confira limpeza dos NPCs/ícones/grupos próprios, troca de perfil, configurações personalizadas, força/lealdade/equipe, DMV, seguro, trabalhos, F2 e F11.

Build sem erros/avisos e 1720 testes aprovados. A nuvem não executa GTA; embarque, tiros/visada, piso, autoria e convivência com mods precisam destes testes reais.
