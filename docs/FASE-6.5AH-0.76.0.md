# Fase6.5AH — manutenção Del Perro Pier — 0.76.0

## Instalar e testar

Extraia RPCore-update-0.76.0.zip e execute Install-RPCore.ps1 **da pasta extraída**, apontando -GtaPath à pasta real do GTA5.exe. GTA Legacy, SHVDN Enhanced1.1.0.6 e LemonUI2.2 SHVDN3. Preserva saves/configs, celularF2, teleportF11 e recursos visuais. PSRP.dll foi referência estática, não dependência para instalar junto.

1. Sua gangue precisa possuir o **território14**. F4 → Comercio dos territorios → área14 → **GPS: manutencao Del Perro**.
2. A pé a até8m do primeiro ponto(-1637.986,-1093.15,13.0274), seguro e sem outra atividade/pendência, escolha **Manutencao do pier /21 tarefas**. Gratuito.
3. Siga o GPS ao ponto marcado. **E a pé a até2m** inicia a tarefa. Permaneça perto e deixe a animação executar por**três segundos contínuos**.
4. As **duas primeiras tarefas repõem materiais** com animação de reparo. As **19 seguintes regam plantas**, com animação de jardinagem e regador na mão direita quando props visuais estão ativos.
5. Cada confirmação fica salva e o GPS muda ao próximo ponto. Complete as **21 tarefas em cinco minutos ativos**. A mensagem final da PSRP fala em sete, mas sua lista e seu loop executam21; RPCore segue os21 pontos e rumos reais.
6. Ao terminar, **E no último ponto** confirma o contrato e libera processamento**2X na área14 por60minutos reais**. Não é preciso voltar ao início.

Não congela/teleporta o jogador: afastar-se, entrar em veículo, abrir menu ou interromper a animação cancela somente a ação ainda não confirmada; tenteE novamente. Ajusta o rumo do jogador ao iniciar a ação como na fonte. Contagem dos3s começa depois que a animação inicia de fato, com tolerância de carregamento inicial1,5s.

## Suspender e retomar

**END suspende**. Tarefas concluídas e tempo gasto permanecem. F4 permite GPS/retomada/cancelamento.

- Parcial: retome **a pé na área do píer**, a até250m do primeiro ponto, com posse/segurança. Continua a próxima tarefa, sem refazer as confirmadas ou reiniciar prazo. GPS aponta ao próximo ponto salvo. Ação parcial de3s não é persistida e reinicia.
- Pronto21/21: retome **a pé a até2m do último ponto** e confirmeE. Sem repetir trabalho.
- Prazo esgotado: reprovação salva, sem prêmio; cancele gratuitamente a pendência no F4 para novo contrato.
- Cancelar sem prêmio parcial, disponível sem posse/filiação em segurança. Bônus anterior mantém vencimento.
- Save falhou: mantém índice/tempo anteriores; finalização conserva21/21 pronto para tentar novamente.

Tempo salvo a cada1s ativo, antes de confirmar tarefa e na suspensão normal; fechamento abrupto pode perder o último intervalo não gravado. Pausa/menus/suspensão/GTA fechado e preparação de modelos/animações não gastam prazo. Os3s da ação contam dentro do prazo; após21 confirmadas, o prazo congela para confirmaçãoE.

Morte/procurado/combate/ragdoll/missão/cutscene/perda de posse/filiação/perfil/ator, outra pendência incompatível ou afastamento250m suspendem conservando provas. Perder o regador próprio interrompe a ação sem confirmar; tenteE. Pendência suspensa/reprovada bloqueia outras operações econômicas territoriais até concluir/cancelar; trava global fica livre após suspensão.

## Recompensa

Bônus2X não liga automático sozinho nem paga dinheiro pessoal/cria receita ou estoque. Com receita suficiente e automático ligado na área14, lote padrão até$500 passa de60s para aproximadamente30s ativos, mantendo85% líquido ao caixa e15% taxa. Quatorze atividades usam máximo2X, sem multiplicação; bônus fracionários anteriores preservados. ValidadeUTC inclui GTA fechado; nova conclusão real renova60min sem somar duração, replay não estende. Sem produção offline.

## Arquitetura e recursos

GangPierData.cs/GangPier.cs e **Gangs/Pier.json**. Defaults21 pontos/rumos da PSRP; BoostMinutes60(1…180), StartRadius8(3…15), SecondsLimit300(120…900), TaskMilliseconds3000(1000…5000), WaterModel prop_wateringcan,3…50 pontos distintos a até200m do primeiro. Primeiros dois sempre reposições, demais regas. Config inválida mantida usa defaults em memória; mudanças só afetam novos contratos, sem alterar snapshot atual.

Wallet.GangPier opcional/extensível: Pending Id/Gang/Area14/Options/Progress(Elapsed,Faulted)/Done; contas Completed/StartedUtcTicks/ExpiresUtcTicks por gangue/área14;640 contas/256 recibos. Sem handles/props/animação/timers nativos persistidos. Begin/Update/Work/Cancel/Complete clonam livro e gravam uma Wallet com rollback/replay/posse/ordem/alcance/duração/limites/overflow e campos futuros preservados. Tempo/reprovação não retrocedem; todas as tarefas e contrato não reprovado exigidos antes da conclusão. Remoção da pendência+prêmio atômicos; sem banco/cash/inventário/estoques/receita suja/tesouraria/reputação/diplomacia/MissionWin/MissionFail.

Regador próprio recebe DECOR, sem colisão, mão direita57005 com offset0,1/0,02/-0,03 e rotação-80/0/20 da referência. Respeita Visuals.Enabled/UseProps: sem cosmético, exige os mesmos pontos, animações e tempo. Reposição mini@repair/fixing_a_ped; rega amb@world_human_gardener_plant@male@base/base. Modelos/animações assíncronos10s. Interrompe só o clip específico no jogador rastreado, remove só seu regador e libera referência ao modelo. Não usa ClearAll nem congela o jogador. Não remove dicionários globais de animação ou altera câmera/veículos/NPCs/polícia/relações/scripts ou arquivosRPF.

## Comparação e validação

PSRP/GangSpawner.cs StartTurfWork/StartPierMaintenance/UpdatePierMaintenance/StartPierRestockAnimation/StartPierWateringAnimation/FinishPierMaintenanceTask/CompletePierMaintenance conferidos: atividade na área14 pelo dispatcher com posse,21 posições/rumos,2 reposições+19 regas,2m/a pé,3000ms/300000ms, animações/regador/mão/offset e2XUTC60min. Código próprio. O helper de início da fonte é genérico; RPCore impõe área14 e posse explicitamente. Mensagem da fonte incorretamente diz7 tarefas.

Diferenças: persistência por tarefa/tempo, END/retomada, finalE, prazo ativo sem preparo/menus, sem congelar jogador/ClearAll, prop opcional e limpeza de animação específica. HUD nativo simples; fases8(interface/necessidades) e9(realismo dos trabalhos) continuam reservadas.

**2797 testes aprovados**(2759 anteriores +38 novos), build Release net48 sem erros/avisos. Ordem/duração/alcance/a pé, prazo exato/rollover/pausa, snapshots/campos futuros, rollback/replay, monotonicidade/limites/bloqueios/UTC e bônus anteriores. **GTA não executado na nuvem**: testar21 pontos/rumos/acesso, animações e regador,3s/interrupções, rota de5min, END/reload parcial/pronto, cancelamento/timeout/perfil e convivência com recursos/bônus anteriores.
