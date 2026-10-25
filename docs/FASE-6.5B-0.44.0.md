# RPCore 0.44.0 — fase 6.5B: recrutar e comandar equipe da gangue

## Instalação e preparação

Baixe e extraia RPCore-update-0.44.0.zip da branch rpcore-entrega. Abra PowerShell na pasta que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2 mantidos. Sem mudança de .oiv/.rpf, CustomGangs.json, RPCoreCreated.json ou Gangs/Presence.json. Não carrega PSRP.dll junto. Conserva saves, DMV C/M1/A, seguro, personagens/roupas, trabalhos e F2/F11.

**F4 → Presenca nas HQs → LIGADA**. Use **GPS** da gangue e afilie-se pelo **Join** na HQ, como antes. Aguarde os membros dos postos existentes aparecerem. Uma gangue sem postos pode receber os postos pelos botões da 6.5A se estiver em RPCoreCreated.json. Líderes não são candidatos à equipe.

## Recrutar e acompanhar

A pé, sem procurado/combate/missão/cutscene/interface modal, chegue a até **3 m de um membro** e selecione **F4 → Recrutar membro mais proximo**. Limite de **3**, pertencentes à gangue afiliada. Escolhe o candidato elegível mais próximo; não recruta líder, NPC de outro mod/nativo, rival, morto, passageiro ou membro já recrutado. Não exige nova reputação ou taxa e não altera sua filiação/reputação.

Usa o **mesmo NPC existente**, conservando a chave do posto para não criar uma cópia enquanto acompanha. Cada integrante recebe um ícone azul próprio e segue a pé em posições diferentes atrás do jogador. Não é teleportado e não entra no grupo nativo do jogador. Assim não substitui outros seguidores instalados.

Os recrutados continuam fora do raio da HQ enquanto estiverem a até **180 m de você** e na mesma filiação. A equipe conta para o limite total de NPCs da presença. Se um integrante morrer/desaparecer, se perder além de 180 m ou a filiação mudar, ele sai da equipe e o posto recebe cooldown antes de voltar a gerar um membro comum. Não há substituto automático recrutado, ressurreição ou teleporte.

## Ordens no F4

- **Equipe: seguir**: retoma caminhada e o acompanhamento como passageiro. Para mudar ordens com integrantes embarcados, pare o veículo.
- **Equipe: aguardar aqui**: guarda a posição atual de cada integrante e manda aguardar. Se estiverem no carro, desembarcam quando parado; depois aguardam perto do local. Use Seguir para continuar. Se você se afastar mais de 180 m, os integrantes são liberados.
- **Dispensar equipe**: remove os ícones da equipe e o recrutamento. Perto da HQ retornam aos postos a pé; longe dela são retirados como atores próprios. Passageiros dispensados esperam o carro parar antes de desembarcar. Se ele continuar em movimento por 60 s, o ped é marcado como dispensável para o GTA, sem remover o veículo.

O contador do F4 informa equipe 0/3 a 3/3. Reabrir o menu mostra a situação atual. Desligar a presença, editar postos, trocar perfil, recarregar/abortar scripts, morrer ou iniciar missão/cutscene encerra a equipe da sessão. NPCs/veículos não são serializados nem recrutados automaticamente no carregamento. Filiação, reputação e preferência de presença continuam salvas normalmente.

## Carros e passageiros

Entre como **motorista de um carro**, pare e espere os integrantes próximos embarcarem fisicamente em bancos livres. Cada tentativa reserva um **banco de passageiro distinto por até 15 s**, em até oito bancos suportados pelo carro. Não escolhe banco do motorista, não expulsa ocupantes, não destranca portas e não teleporta para o banco. Usa flags nativas DontJackAnyone e BlockSeatShuffling. Atributo para não comandear veículos é aplicado apenas nos recrutados próprios; flag adicional de auto-shuffle é documentada no SDK como MP-only e não é garantia de controlar toda IA nativa no Legacy.

Exige carro utilizável, sem reserva de trabalho, parado abaixo de 0,5 m/s, a até **25 m do integrante**, e você no volante. NPCs aguardam quando não há vaga. Motos, barcos, aeronaves e veículos registrados em JobVehicles (incluindo exames/trabalhos) não recebem novos embarques da equipe. Não modifica seu veículo ou paga qualquer serviço. Antes de um trabalho com veículos próprios, pode mandar a equipe aguardar ou dispensá-la.

Se o carro começar a se mover durante a aproximação, a vaga ficar ocupada, você sair ou a tentativa expirar, cancela/reavalia o embarque sem expulsar ninguém. Espere todos sentarem antes de acelerar. Ao você desembarcar, eles saem quando o veículo estiver parado e voltam a seguir. Portas travadas ou caminhos bloqueados podem impedir a entrada; o código não força portas/acesso.

## Comparação e limites

PSRP GangSpawner RecruitExistingGangMember/DismissRecruitedGangMember: limite 3, membros da gangue e exclusão do líder, remoção dos postos, grupo nativo e atributos de combate. A interação da referência é via Third Eye; esta entrega usa F4. O RPCore mantém propriedade do NPC/posto e usa tarefas locais, sem tomar o grupo do jogador nem gerar outro NPC para segui-lo. Ordens de espera, gestão de bancos e desembarque são implementação própria.

A equipe permanece **desarmada**, sem ordens de ataque, invencibilidade ou alteração da polícia/relações globais. Em combate, as ordens automáticas cedem à resposta nativa. Não representa escolta armada nem sistema completo de gangues. Iniciações/diálogo com líder, territórios, guerras, economia e integração policial continuam pendentes. Interface/mecânicas seguem na fase 8 e refinamento de todos os trabalhos na fase 9.

## Teste dentro do GTA

1. Ative presença, afilie-se e recrute três membros da HQ. Confira ícones/contador e que o quarto, líder e rival são recusados. Sem NPC duplicado no posto de um recrutado.
2. Caminhe e afaste-se mais de 160 m da HQ, mantendo a equipe próxima: deve continuar acompanhando. Teste Aguardar e depois Seguir; afaste-se mais de 180 m de um integrante e confira que não reaparece ao seu lado.
3. Use um sedã de quatro lugares, vazio e parado: três passageiros entram em bancos distintos; espere todos sentarem, dirija, pare e saia. Repita com uma vaga ocupada e carro de dois lugares: sem expulsar ninguém ou tomar volante. Teste portas travadas e começar a dirigir antes do embarque.
4. Confira que não embarcam em moto/aeronave nem veículo reservado de trabalho/exame. Dispensar embarcado deve esperar parada; dispensar perto da HQ deve liberar/retornar os NPCs próprios. Nunca deve apagar seu carro.
5. Teste morte/perda de integrante, Leave/Join outra gangue, desligar presença, reload e troca de perfil: sem equipe/ícones acumulados ou nova cobrança. Filiação/reputação/metadados e configurações antigas devem continuar.
6. Confira regressões dos postos, DMV C/M1/A, seguro, trabalhos, inventário, personagens/roupas, F2 e F11.

Build sem erros/avisos e 1617 testes de dados aprovados. Caminhos, portas, animações/assentos, reação ao combate e coexistência com mods precisam de GTA real. A nuvem não executa o jogo.
