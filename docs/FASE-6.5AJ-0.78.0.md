# 0.78.0 — guerras, diplomacia e reforços num pacote

Esta entrega agrupa quatro partes: declaração de guerra na HQ, alertas automáticos contra seus territórios, simulação distante entre rivais e reforços nas novas capturas. Código próprio após conferir GangSpawner.cs/PSRP.dll. Não conclui toda a fase6 nem antecipa fases8/9.

## Instalar

Com o GTA fechado, extraia RPCore-update-0.78.0.zip e execute Install-RPCore.ps1 da pasta extraída com seu caminho real do GTA. Não apague SaveData/configs. Não reinstale OIV/RPF/YTD; F2/celular e teleporte F11 preservados.

## Roteiro único no GTA

1. F4: confira afiliação e Territórios / mapa ligado. É necessário possuir ao menos uma área para receber ataques. Capturas existentes continuam disponíveis.
2. Vá a pé ao centro da sua HQ. F4 → Relações / acordos → **Declarar guerra a [rival]**. A linha avisa que selecionar confirma. Não cobra dinheiro; muda a opinião desse rival para Guerra neste perfil. Não altera relações nativas globais. Acordos existentes melhoram um nível por vez, com preço/intervalo já configurados.
3. F4 → **Guerras / alertas e reforços**: ligue Ataques aos seus territórios. Inicialmente todas as três opções estão desligadas nos saves antigos. Leia o aviso: ignorar um alerta por120 segundos ativos perde a área para o atacante.
4. Selecione **Solicitar alerta agora** para testar sem esperar4–13 minutos de mundo livre. Exige rival hostil/Guerra com modelo disponível, território próprio, mapa ligado e nenhuma atividade/contrato pendente. O alerta é real e tem o mesmo risco de perda. Não há botão de pagamento ou prêmio gratuito.
5. Use GPS do ataque, viaje até a área e abra **Responder ao ataque** a pé dentro dela. O prazo pausa enquanto menus, trabalho/pendência, missão, pausa, procurado ou combate estiverem ativos. O primeiro instante após fechar o menu também não cobra o tempo que ele ficou aberto. Em movimento livre/dirigindo, o prazo corre; teleporte F11 continua disponível para teste.
6. Elimine as ondas da defesa e confirme **E**. Vitória conserva a área/+25 reputação; perder a luta por morte/veículo/abandono/prazo segue a defesa anterior: área vai ao atacante e−20 reputação. **END** suspende; retome em Territórios / mapa e disputas. Cancelar uma defesa conserva dono, conforme comportamento anterior.
7. Faça outro alerta e teste desligar Ataques: descarta o aviso sem perda. Para testar derrota por omissão deliberadamente, deixe120 segundos ativos correrem: somente a área avisada muda de dono, sem pagamento ou desconto de dinheiro/reputação. Histórico marca alerta perdido separado de derrota em combate. Depois é possível recapturar normalmente.
8. Ligue **Reforços nas novas capturas** e dispute uma área rival. Até3 defensores iniciais, mais3 a pé ao cair metade dos iniciais (se todos caírem no mesmo frame, ainda entram reforços). Somente vencer todos liberaE. Nunca mais de6 inimigos nessa disputa. Configuração/tentativa antiga preserva o snapshot anterior; mudar a opção não altera luta já salva.
9. Ligue **Disputas entre rivais** e fique em mundo livre, longe dos territórios rivais. A cada5–15 minutos ativos há85% de chance de avaliar uma batalha entre duas facções que ainda tenham área. Se a área sorteada estiver a200m ou menos de você, nenhuma simulação ocorre. Só envolve NPCs: nunca captura/cede área da sua gangue. Resultado aparece no aviso, mapa e histórico. Se uma rival já perdeu todas as áreas, não é candidata.
10. Teste reload/perfil: aviso conserva segundos inteiros gravados; resposta e pendência de defesa ficam juntas; captura reforçada conserva a composição. Saldo, estoque, atividades territoriais e funcionalidades antigas devem continuar iguais.

## Comparação com a referência

GangSpawner.GetAiTurfAttackInterval/UpdateTurfAttackByAi:780,720,660,600,540,480,420,360,300,240 segundos por quantidade de áreas; GangTurfDefenceTime120000ms. UpdateAiVsAiTurfAttacks:300–900s/85%, exclui jogador, sorteia atacante/defensor com território; chance de captura55% se atacante tem menos áreas,45% se tem mais,50% empatado. CheckTurfReinforcements chama apoio quando sobra até metade da força. Evidências e hashes em PSRP-SYSTEMS-AUDIT-0.78.0.json.

Adaptações: opções separadas por perfil, sem ativação retroativa. Relógios de sessão e só tempo ativo seguro/livre; nenhum catch-up offline. Hostilidade/Guerra considera os acordos/roubos já salvos, sem exceção que autorize atacar rivais respeitosos por domínio de75% do mapa. Declaração de guerra é ação própria explícita na HQ. Reforços próprios chegam a pé em postos livres, em vez do veículo da PSRP. Simulação distante resolve apenas dados, sem NPCs ou combate artificial ao redor do jogador. Desligar não cancela luta já iniciada; suspenda/cancele por Territórios.

## Persistência e compatibilidade

GangState.Warfare opcional/extensível: três preferências, alerta com snapshot de área/facções/Elapsed/Limit, contadores Unanswered/Responded/RivalBattles e histórico limitado a32 entradas. Sem modificação de saves até a ação explícita ou evento habilitado. Alertas têm checkpoints de1 segundo; reload pode devolver menos de1 segundo ainda não gravado. Intervalos entre eventos reiniciam na sessão e não recuperam tempo offline.

Vencimento grava dono/histórico/contador na mesma GangState, sem cruzar Wallet. Responder consome alerta e inicia a defesa na mesma gravação GangTurfTransactions.Begin; erro de save conserva ambos no estado anterior. Resultado de combate usa transações existentes. Simulação rival valida dono esperado e exclui ambas as facções do jogador; grava resultado/dono juntos, sem dinheiro/reputação. Declaração usa livro diplomático/recibo na Wallet, incrementa Declarations, sem débito; rollback síncrono e reexecução protegida. Campos desconhecidos e histórias anteriores preservados.

GangTurfAttempt.InitialDefenders e WarAlertId opcionais; tentativas antigas assumem0/null e conservam regras. Composição reforçada é snapshot, persistência/reset de prova seguem a captura anterior. Só entidades próprias; não altera matriz global de relações, polícia, scripts externos, câmeras, carros de ambiente ou recursos visuais. Limpeza não deleta defensor dentro de veículo; libera naturalmente esse NPC próprio. Nenhum cash/banco/tesouraria/estoque/produção/boost extra.

Gangs/Warfare.json: ResponseSeconds120(60…900), RivalMinSeconds300/RivalMaxSeconds900(60…3600), RemoteRadius200(150…1000). Valores inválidos preservados em disco e defaults em memória. Configuração de área/defesa anterior mantida.

## Validação e escopo restante

**2891 testes aprovados**:2832 anteriores+59 novos, build Release net48 sem erros/avisos. Cobrem alerta/snapshot/prazo/pausa/rollover, vencimento seletivo, resposta atômica/replay/erro de save, defesa/conclusão, simulação sem envolvimento do jogador, história limitada/campos futuros, configuração inválida/overflow e declaração/acordo/rollback. Instalador verificado separadamente sobre versão anterior.

GTA não executado nesta nuvem. Navegação/combate/modelos, posições dos reforços, pausa dos relógios em situações reais, mapa após transferência, integração com mods e todos os passos acima precisam do teste no GTA.

Comboios motorizados, assalto territorial organizado, drive-bys e diálogos completos continuam ausentes. Polícia/prisão e fase7 continuam pendentes. Interface/necessidades na fase8; realismo de todos os trabalhos na fase9. Ver PENDENCIAS-ANTES-REFINAMENTO.md para a fila agrupada.
