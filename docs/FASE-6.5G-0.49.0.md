# RPCore 0.49.0 — fase 6.5G: defesa de territórios

## Instalação e acesso

Baixe e extraia RPCore-update-0.49.0.zip da branch rpcore-entrega. Abra PowerShell na pasta que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Mantém GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Não reinstala .oiv/.rpf nem executa PSRP.dll junto. Acrescenta Gangs/Defense.json quando ausente. Territories.json da captura, donos/saves, gangues/iniciações/equipe, DMV C/M1/A, demais configurações/trabalhos e F2/F11 preservados.

**F4 → Territorios / mapa e disputas → mapa ligado → GPS de territorio da sua gangue → Defender: Territorio N**. Precisa de filiação registrada e posse da área. As gangues padrão têm cinco territórios iniciais cada; uma gangue criada pode capturar primeiro pelo fluxo da 0.48.0. Opção Defender só aparece habilitada nas áreas próprias, e Disputar continua para áreas rivais.

## Consequência da derrota

**Depois que a defesa começa, morrer, entrar em veículo, abandonar o raio ou esgotar o tempo transfere o território ao atacante.** O aviso aparece na descrição do botão e ao iniciar. Não perde dinheiro/equipamento; perde a posse daquela área. Pode recapturá-la depois pelo Disputar normal. Não restaura sua vida/armadura.

A defesa é **iniciada por você**, sem ataques automáticos, perdas fora de missão ou simulação offline nesta entrega. END/suspender/cancelar conservam o dono. Falta de modelo/piso/espaço, NPC desaparecido, falha de gravação, missão/cutscene ou troca de ped suspendem sem transferir posse. Uma posse alterada externamente também não é sobrescrita.

## Como defender

1. A pé dentro do raio do território (padrão **45 m**), sem procurado/combate/missão/cutscene/interface modal e sem atividade/exame/iniciação/disputa pendente, escolha **Defender**. O sistema escolhe uma gangue rival com modelos válidos; dá preferência ao dono original daquela área quando diferente da sua gangue.
2. Aguarde a preparação perto do centro. Carrega os modelos e prepara a primeira onda antes de registrar tentativa/iniciar combate. Se não houver posições seguras, não registra tentativa nova e não transfere posse.
3. Padrão: **duas ondas de três atacantes**, a pé, de uma borda a cerca de raio menos 8 m do centro. Eles se aproximam combatendo o jogador. São NPCs próprios/mortais, 200 de vida, sem armadura, pistolas e sem dinheiro/arma para saquear. Não toma NPCs/carros de outros scripts nem modifica grupos nativos/polícia.
4. Vença a primeira onda inteira. Só então aparece a segunda. Cada índice é confirmado uma vez; a contagem global vai até **6/6**. O mod conserva os checkpoints e os modelos necessários entre ondas. Piso/caminho/espaço da borda ainda dependem do mapa e precisam do teste no GTA.
5. Permaneça a pé na área durante a luta, no limite de tempo do território (padrão **10 minutos ativos**, pausa respeitada). Não use teleporte. Procurado/reação policial continuam nativos; não há imunidade ou limpeza de estrelas.
6. Vencidas todas as ondas, grava **vitória pronta** e remove os adversários próprios. Pressione **E** na área para confirmar a defesa. Mantém/grava a posse na sua gangue e incrementa Defesas uma vez, sem contar como captura ou conceder renda/reputação/arma.

A referência conta mortes dos adversários; não precisa eliminar pessoalmente todos. Desaparecimento sem morte confirmada suspende em vez de conceder vitória ou penalizar a posse. A equipe anterior pode acompanhar conforme suas próprias regras, mas esta etapa não cria guarnição aliada armada nem veículos de reforço.

## Suspender, salvar e retomar

**END**, Suspender disputa, desligar mapa, reload ou troca de perfil liberam os atores/ícones próprios e a trava, preservando a pendência. Cancelar descarta a pendência sem transferir posse. Todas as atividades continuam mutuamente exclusivas pela trava compartilhada e pendências protegidas.

Para retomar, volte afiliado à mesma gangue, a pé na área, com mapa ligado e sem procurado/combate ou atividade concorrente. Uma defesa parcial recria **todas as ondas e zera as derrotas físicas**, mantendo ID, atacante, modelos, raio, tempo e total de tentativas. Não cobra e não restaura vida. O relógio reinicia. Vitória já pronta retoma só para confirmar E, sem novos atacantes; morrer/sair da área depois de vencê-los não transforma a vitória pronta em perda.

No submenu, **Capturas / Defesas / Perdas** mostra os contadores deste perfil. Vitória mantém proprietário; derrota durante defesa ativa salva proprietário atacante e perda na mesma transação. O mapa atualiza nome/cor; recapturar segue o fluxo anterior. Sair da gangue não transfere as áreas automaticamente.

Campos adicionais em **GangState.json → Turf**: Defenses, LostDefenses e tentativa com Mode, Attacker, WaveSize. **Mode ausente continua Attack=0**; Defense=1 é adicional. Tentativas 0.48.0, IDs de donos futuros, metadados, capturas e recibos são preservados. Validação, cópia e save antes de publicar impedem resultado duplicado ou perda confirmada sem gravar. Não salva handles de NPCs.

Gangs/Defense.json configura Waves (1–2) e PerWave (1–3), total até seis. Não altera Territories.json nem as regras de captura antigas. Configuração inválida usa defaults em memória conservando o arquivo. Pendências guardam sua própria quantidade/modelos, mesmo se a configuração mudar depois.

## Comparação e limites

PSRP GangSpawner StartGangTurfAttack/SpawnGangTurfAttackers/UpdateGangTurfBattle: atacantes, DefenseWin/DefenseFail, manutenção ou transferência de TurfOwnership ao atacante. Auditoria PSRP-SYSTEMS-AUDIT-0.49.0.json registra os métodos, hashes e índice das 75 fontes.

Implementação própria desta etapa: defesa solicitada pelo jogador, 1–2 ondas a pé, perda por derrota/abandono/veículo/tempo do jogador e confirmação E. A referência também tem IA automática, veículos, guarnição e batalha entre gangues. Esses sistemas não estão concluídos aqui. Não há renda, comércio/estoque/lavagem, salário/rank, guerra automática ou integração policial. Interface/mecânicas continuam na fase 8; refinamento de todos os trabalhos na fase 9.

## Testes dentro do GTA

- Em uma área própria aberta, inicie Defender e confira o nome da gangue atacante, três NPCs na borda e segunda onda só após vencer a primeira.
- Vença seis, confirme E e recarregue: posse continua sua, Defesas aumenta uma vez e Capturas não muda. Repita com vitória pronta suspensa/reload: sem luta extra ou contador duplicado.
- Em um perfil/área que aceite perder para teste, abandone a área ou entre no veículo durante a defesa ativa. Confira proprietário atacante, Perdas +1 e possibilidade de recapturar pelo Disputar. Morte/timeout devem ter o mesmo resultado.
- Suspenda uma defesa parcial e retome: todas as ondas reiniciam, tentativa/atacante/regras se mantêm e ninguém duplica. Cancele e confirme que não perdeu posse.
- Teste piso bloqueado/modelo ausente/interferência de NPCs e início de missão: sem perda técnica. Falhas de save não podem confirmar vitória/perda ou trocar outro dono.
- Confira capturas 0.48.0, configurações personalizadas, perfis, lealdade/força/direção/equipe, DMV, seguro, trabalhos, inventário, F2 e F11.

Build sem erros/avisos e 1771 testes aprovados. Combate/ondas/piso/IA, momento real de morte e convivência com polícia/mods precisam do GTA; a nuvem não executa o jogo.
