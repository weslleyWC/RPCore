# RPCore 0.45.0 — fase 6.5C: iniciação por lealdade

## Instalação

Baixe e extraia RPCore-update-0.45.0.zip da branch rpcore-entrega. Abra PowerShell na pasta que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Mantém GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Não reinstala .oiv/.rpf nem executa PSRP.dll junto. Acrescenta Gangs/Loyalty.json quando ausente; mantém presença/equipe, configurações antigas, saves, DMV C/M1/A, seguro, personagem/roupas, empregos, celular F2 e teleporte F11.

## Preparação e início

**F4 → Presenca nas HQs → LIGADA → GPS da gangue.** A iniciação requer personagem **sem filiação**, líder vivo gerado pelo RPCore, jogador a pé a até **3 m do líder**, sem procurado/combate/missão/cutscene/interface modal e sem outra atividade reservada. O ícone pequeno do líder identifica seu posto; não basta ficar no círculo da HQ se o líder estiver distante.

Use **F4 → Iniciacao por lealdade: nome da gangue**. A atividade usa a mesma trava dos trabalhos/exames: encerre ou suspenda a atividade anterior. Não cobra taxa nem altera a filiação ao começar. O botão Join anterior continua disponível sem prova pendente; esta prova é uma alternativa opcional, sem novo requisito para quem já é filiado.

Se já estiver afiliado, use outro perfil/personagem para testar conservando a filiação/reputação principal. O Leave antigo continua saindo e zerando a reputação quando você o escolhe. Uma gangue criada no RPCore precisa ter um posto/modelo de líder configurado antes de iniciar; use os recursos da fase 6.5A no perfil afiliado e depois teste sem filiação. Definições de gangue são compartilhadas entre perfis.

## Buscar os dois pacotes

1. O GPS marca o **primeiro contato**, escolhido entre **15 posições/rumos da PSRP**. Os dois locais selecionados são diferentes e ficam guardados na tentativa. Pode usar seu carro no deslocamento; a coleta exige estar a pé.
2. O contato a_m_y_business_01 aparece ao chegar a menos de **65 m**, se houver modelo/chão/espaço. Se você teleportar exatamente sobre o marcador antes de ele aparecer, afaste-se pelo menos **4 m** e aguarde a nova tentativa. Não remove ped/veículo alheio para abrir espaço; local bloqueado permanece pendente.
3. Aproxime-se a até **2,5 m**, feche os menus e pressione **E**. Permaneça a pé e perto por **2,5 segundos**, sem combate/procurado/interface. Afastar-se, entrar no veículo ou abrir outro menu interrompe a entrega sem confirmar pacote. A animação de troca é solicitada sem bloquear o carregamento; a confirmação depende da interação/tempo, não de forçar a animação a existir.
4. O pacote só avança após gravar. O contador muda para **1/2** e o GPS marca o segundo contato. Repita; **2/2** marca a volta ao líder. Os pacotes são objetivos persistentes da missão, não itens negociáveis colocados no inventário.
5. Volte ao **líder vivo da mesma gangue**, a pé, a até 3 m. Feche os menus e pressione **E** para entregar os dois e confirmar a filiação. Grava uma única conclusão, zera a reputação inicial como no Join existente e libera a atividade. Não entrega dinheiro/arma nem recompensa duplicada.

Não há teleporte/câmera/cutscene obrigatória, congelamento ou limpeza geral das tarefas do jogador. O contato é próprio, desarmado, sem invencibilidade e sem dinheiro/arma para saquear; mudanças de relação são somente nesse NPC. Morte do contato cancela a tentativa, conservando a filiação anterior. Fonte PSRP usa contato invencível e cutscene de vitória; são diferenças intencionais desta implementação.

## Suspender, retomar e cancelar

**END** ou **F4 → Suspender iniciacao** remove contato/rota próprios e libera a trava, conservando os pacotes já confirmados. Morte do jogador, procurado, combate, missão/cutscene, desligar presença, trocar perfil ou recarregar scripts também suspendem; não concedem a filiação automaticamente. Tentativa parcial de entrega não é conservada; pacotes confirmados são.

Para retomar, ligue a presença e volte a pé ao líder da gangue escolhida; **F4 → Retomar iniciacao**. Reusa locais e contagem sem taxa. Com dois pacotes salvos, basta voltar ao líder e confirmar E. Reload não cria contatos/rotas automaticamente.

**Cancelar iniciacao pendente** descarta aquela tentativa sem mudar filiação/reputação. Enquanto houver tentativa pendente, Join e edição de postos ficam bloqueados para evitar trocar gangue/líder no meio da atividade. Conclua ou cancele antes. Opções antigas continuam funcionando normalmente sem pendência.

Estado opcional em **GangState.json → Loyalty**: tentativa com ID/gangue/líder/dois contatos/contagem, recibos limitados e total de conclusões. Não grava handles de NPCs. Transações clonam e gravam antes de publicar o novo estado; falha de save suspende conservando o último progresso persistido. Não regrava definições antigas ou carteira/inventário. JSON inválido é protegido contra sobrescrita.

## Comparação e limites

GangSpawner StartLoyaltyInitiation/SpawnLoyaltyContact/UpdateLoyaltyHandover/UpdateLoyaltyInitiation/CompleteLoyaltyInitiation: dois contatos diferentes, 15 pontos, entrega de 2500 ms, volta à gangue e filiação final. Coordenadas e headings estão conferidos na auditoria PSRP-SYSTEMS-AUDIT-0.45.0.json, com índice das 75 fontes e hashes atuais.

O RPCore implementa o fluxo com código próprio, F4 opcional, guarda de atividade, contato mortal e persistência/recibos/retomada. Não força a cutscene, não substitui Join nem aplica o InitiationType Strength/Driving das definições da referência. Não conclui diálogo completo com líder, iniciação de força/direção, territórios, guerras, negócios ou polícia. Interface/mecânicas continuam na fase 8; refinamento de todos os trabalhos na fase 9.

## Testes no GTA

- Sem filiação e com presença ligada, vá ao líder, inicie pelo F4 e confirme que sem líder/proximidade ou com outro trabalho/exame ativo o início é recusado.
- Visite os dois contatos, teste E a pé, afastamento/menu interrompendo entrega e a contagem 1/2 → 2/2. Confira chão, modelo e animação sem remover terceiros.
- Suspenda após o primeiro pacote, reinicie scripts/jogo e retome no líder: mantenha a mesma contagem/locais. Suspenda com dois pacotes e confirme que só o E junto ao líder grava filiação.
- Confira nova filiação e contador de conclusão uma única vez, sem dinheiro/arma/item duplicado. Teste cancelamento e morte do contato, sem perder filiação/reputação anterior por uma confirmação incompleta.
- Confira que Join/postos ficam bloqueados apenas quando há pendência, e voltam após cancelar/concluir. Confira F4 presença/equipe, troca de perfil, DMV C/M1/A, trabalhos, seguro, inventário, F2 e F11.

Build sem erros/avisos e 1650 testes aprovados. A nuvem não executa GTA; navegação, piso, animação, teclas e convivência com mods ainda dependem destes testes reais.
