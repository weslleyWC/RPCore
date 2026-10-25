# RPCore 0.46.0 — fase 6.5D: iniciação por força

## Instalação e acesso

Baixe e extraia RPCore-update-0.46.0.zip da branch rpcore-entrega. Abra PowerShell na pasta que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Mantém GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Não reinstala .oiv/.rpf ou carrega PSRP.dll junto. Novo Gangs/Strength.json é instalado quando ausente. Presença/equipe/lealdade, documentos C/M1/A, demais saves/configurações e celular F2/teleporte F11 preservados.

**F4 → presença ligada → GPS da gangue → Iniciacao por forca: nome**. Fique a pé a até 3 m do líder vivo gerado pelo RPCore, **sem filiação**, desarmado, com vida acima de 100, sem procurado/combate/missão/cutscene/interface modal. Não pode haver outra iniciação pendente ou atividade/exame reservando a trava compartilhada.

É uma alternativa opcional ao Join anterior e à prova de lealdade. Não exige prova de força para quem já é filiado. Para testar mantendo a filiação/reputação principal, use outro perfil/personagem sem filiação. O Leave antigo continua zerando reputação quando escolhido. Gangues criadas precisam de modelo/posto de líder e modelos de membros configurados pelas etapas anteriores.

## Preparação e luta

O sistema carrega os modelos da gangue sem espera bloqueante e procura três vagas de piso/espaço próximas ao ponto de início. Usa como base os offsets (+4,0), (-3,+3), (-3,-3) da referência e tenta outras orientações quando ocupados. Não remove terceiros nem força acesso a interiores. Modelos inválidos, espaço bloqueado, afastar-se durante a preparação ou timeout de 10s cancelam a preparação sem registrar tentativa nova.

Só depois dos três NPCs próprios estarem preparados grava a tentativa e inicia o combate. São **3 desafiantes desarmados simultâneos**, padrão **150 de vida/0 armadura**, mortais, sem dinheiro/armas para saquear. Cada um tem ícone vermelho. Recebem tarefa de combate contra você; não há mudança de relação global ou ordem para atacar terceiros.

- Use **somente os punhos**. O mod não remove suas armas nem força a seleção: sacar outra arma reprova a tentativa.
- Fique a pé, com **vida acima de 100**, dentro de **35 m do ponto de início**. Entrar em veículo, sair da área, morte/vida baixa, procurado, missão/cutscene ou troca do ped durante a luta reprovam. Não há restauração de vida/armadura antes/depois. Não use teleporte durante o teste.
- Tempo padrão: **5 minutos ativos por luta**, com pausa respeitada. O combate esperado pode terminar antes; não precisa esperar o relógio.
- Um desafiante rende-se ao chegar a **100 de vida ou menos**, ou conta como derrotado se morrer, desde que haja sinal nativo recente de dano do jogador e ele esteja desarmado. NPC que desaparece ou cai sem essa atribuição reprova; não concede vitória por remover entidades/usar outros NPCs.
- Cada índice conta uma única vez. O desafiante vivo vencido recebe tarefa de rendição e deixa de ter ícone. O checkpoint é gravado antes de publicar a nova contagem.

A atribuição usa flags nativas de dano recente, não um histórico perfeito de eventos de outros scripts. Se um nocaute legítimo for recusado, envie log, vida dos envolvidos e situação do golpe para ajustar após o teste real. Polícia e reações do mundo continuam nativas; não há imunidade nem limpeza de procurado.

## Aprovação e filiação

Ao vencer os três, grava uma **aprovação pendente** e retira somente os desafiantes próprios. A rota amarela indica o líder. Volte a pé, sem procurado/combate/missão, feche os menus e pressione **E** junto ao líder vivo para confirmar a filiação. Só essa confirmação grava gangue/conclusão, uma vez, com reputação inicial zero como no Join anterior. Não concede dinheiro, arma ou item.

Se o líder se afastou/reagiu à luta, aguarde o retorno normal da presença ou use a retomada depois. Não cria líder extra nem força câmera/cutscene/controle. Uma aprovação completa continua emitível depois de reload, sem lutar outra vez.

## Suspender, retomar e cancelar

**END** ou **F4 → Suspender prova de forca** encerra os atores da luta e libera a atividade. Reload, desligar presença ou trocar perfil também suspendem, preservando a tentativa já salva. Não grava handles de NPCs.

Para retomar, volte desarmado e a pé ao líder, sem filiação e com vida acima de 100. **F4 → Retomar prova de forca**. Se a luta era parcial, prepara **três NPCs novos e zera as derrotas físicas** antes do combate, mantendo ID/regras/centro e sem contar outra tentativa ou cobrar taxa. O relógio da nova luta começa do zero; não restaura sua vida. Precisa estar também a até raio da arena menos 8 m do centro original para preparar a retomada. Se já havia aprovação dos três, a retomada só permite confirmar E no líder, sem novos desafiantes.

**Cancelar prova de forca pendente** descarta aquela tentativa; uma reprovação também a encerra. Pode contratar nova prova gratuitamente. Enquanto houver prova de força ou lealdade pendente, Join e edição de postos ficam bloqueados até concluir/cancelar. As duas iniciações não podem ficar pendentes simultaneamente pelas ações do mod.

Save opcional em **GangState.json → Strength**: tentativas/conclusões, regras/modelos/centro, índices derrotados, aprovação e recibos limitados. Transações clonam e gravam antes de publicar. Falha ao gravar suspende e conserva o último estado persistido; não emite filiação sem save. Campos desconhecidos, livro de lealdade e demais dados são conservados. Gangs/Strength.json pode ser ajustado com jogo fechado; valores inválidos usam defaults em memória sem substituir o arquivo.

## Comparação e limites

PSRP GangSpawner StartStrengthInitiation/UpdateStrengthInitiation/CompleteStrengthInitiation: três desafiantes, 150 de vida, sem armas, limites 100/35 m e controle de derrotas distintas. A referência força punhos e cutscene de vitória. RPCore usa os punhos escolhidos pelo jogador, centro na posição de início, limite próprio de 300s, guarda de dano/atividade e aprovação persistida com confirmação no líder. Auditoria em PSRP-SYSTEMS-AUDIT-0.46.0.json, com índice das 75 fontes e hashes.

Não conclui iniciação de direção, diálogo completo com líder, territórios, guerras, economia ou polícia. Interface/mecânicas continuam na fase 8; refinamento de todos os trabalhos na fase 9. Não promete precisão universal de flags de dano com outros mods.

## Testes no GTA

1. Sem filiação, ative presença e procure líder em piso aberto. Inicie desarmado; confira os três modelos/ícones, início só após preparação, combate corpo a corpo e rendição/contagem.
2. Vença os três, confira rota ao líder e pressione E: filiação/conclusão uma vez, sem dinheiro/armas. Salve/recarregue com aprovação pendente e emita sem nova luta.
3. Suspenda após uma derrota e retome: três desafiantes novos, contagem reiniciada e mesmo total de tentativas. Sem NPCs/ícones acumulados. Confira regras/centro salvos.
4. Teste arma, veículo, abandono, vida baixa, procurado e desaparecimento/interferência de terceiros: deve reprovar sem conceder filiação. Seu inventário/armas/vida/armadura não devem ser artificialmente repostos/removidos.
5. Teste preparação ocupada/modelo inválido e concorrência com lealdade/jobs/DMV: sem luta parcial, tentativa extra ou atividade paralela. Cancelar libera Join/postos.
6. Confira regressões de lealdade, presença/equipe, troca de perfil, DMV C/M1/A, seguro, trabalhos, inventário, personagens/roupas, F2 e F11.

Build sem erros/avisos e 1683 testes aprovados. A nuvem não executa GTA: luta, rendição, flags de golpes, piso/IA e convivência com mods precisam destes testes reais.
