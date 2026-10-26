# Fase6.5AF — circuito promocional BMX — 0.74.0

## Instalação

Baixe RPCore-update-0.74.0.zip, extraia e execute Install-RPCore.ps1 **da pasta extraída**, com -GtaPath apontando à pasta real do GTA5.exe. GTA V Legacy, SHVDN Enhanced1.1.0.6 e LemonUI2.2 SHVDN3. Preserva saves/configs, celularF2, teleportF11 e recursos visuais instalados. PSRP.dll é referência estática, não dependência para instalar junto.

## Teste no GTA

1. Sua gangue precisa possuir o **território12**. F4 → Comercio dos territorios → área12 → **GPS: BMX skatepark**.
2. A pé a até8m do início(-1375.902,-1407.651,5.033009), seguro e sem outra atividade/pendência, escolha **Circuito BMX /11 pontos**. Gratuito. Afaste-se2m e deixe a vaga livre; não remove outros carros/pedestres.
3. Monte na **BMX fornecida**. O prazo de**75 segundos ativos começa quando ela aparece**, incluindo o tempo para montar e o tempo fora da bicicleta. A pé ou em outra bicicleta não conta.
4. Passe pelos **11 anéis verdes em ordem**. GPS indica o ponto atual. Precisa estar conduzindo a BMX própria, a até2,5m em planoXY e com diferença vertical de até3m do ponto. Não exige manobra extra ou salto que não existe na validação da PSRP.
5. Cada checkpoint fica salvo antes de avançar. Complete o último **antes do prazo**; ao atingir75s exatos, reprova. Depois dos11 confirmados, o prazo congela.
6. **Pare e pressioneE no último ponto**, ainda na BMX abaixo de1m/s ou a pé, para confirmar o contrato. Libera processamento2X na área12 por60minutos reais.
7. Confira linha BMX /bonus2X no F4. Saia e deixe a bicicleta parada no chão para liberação; não é apagada ocupada, em movimento, no ar ou presa a outra entidade.

## Suspender, retomar e cancelar

**END suspende**. Tempo e checkpoints confirmados permanecem salvos. F4 permite GPS, retomada e cancelamento.

- Parcial: saia da bicicleta anterior e deixe-a parada para liberar. Retome **a pé no início**, com posse/segurança. Recria uma bicicleta própria e mantém os checkpoints anteriores e o **tempo já gasto**. Continue do próximo anel, sem refazer pontos confirmados ou reiniciar prazo.
- Pronto11/11: retome **a pé no último checkpoint**, a até2,5m, e confirmeE. Sem respawn nem repetição do circuito.
- Reprovado por prazo/bicicleta perdida ou destruída: não permite retomar; cancele a pendência gratuitamente no F4 para contratar outro circuito.
- Cancelar sem prêmio parcial, disponível sem posse/filiação em segurança. Bônus anterior mantém vencimento.
- Save falhou: mantém índice/tempo anteriores; não avança checkpoint sem prova e conclusão conserva11/11 pronto para tentar novamente.

Tempo é salvo a cada1s ativo, antes dos checkpoints e ao suspender normalmente. Fechamento abrupto pode perder o último intervalo ainda não gravado. Pausa/menus/suspensão/GTA fechado e preparação de modelo/vaga não gastam prazo nem produzem renda.

Morte/procurado/combate/ragdoll/missão/cutscene/perda de posse/filiação/perfil/ator,100m de separação da bicicleta,250m de afastamento do início ou ocupação/conexão inesperadas suspendem. Regra compartilhada de segurança suspende também após queda/ragdoll; retome pelo F4. Pendência suspensa/reprovada bloqueia outras operações econômicas territoriais até concluir/cancelar; trava global fica livre após suspensão.

## Recompensa

Processamento automático **2X na área12 por60minutos reais**. Não liga automático sozinho nem paga dinheiro pessoal/cria receita ou estoque. Com receita suficiente e automático ligado, lote padrão até$500 passa de60s para aproximadamente30s ativos;85% líquido ao caixa/15% taxa permanecem. Doze atividades usam máximo2X, sem multiplicação. ValidadeUTC inclui GTA fechado; nova conclusão real renova60min sem somar duração, replay não estende e relógio anterior à concessão não ativa. Sem produção offline.

## Arquitetura e compatibilidade

Novos GangBmxData.cs/GangBmx.cs e **Gangs/Bmx.json**. Defaults bmx, início/rumo e11 pontos exatos da PSRP; BoostMinutes60(1…180), StartRadius8(3…15), SecondsLimit75(30…300), Radius2,5(1…4), Height3(1…4),1…50 pontos distintos a até150m do início. Config inválida mantida no disco usa defaults em memória. Mudanças afetam próximo contrato, sem alterar snapshot atual.

Wallet.GangBmx opcional/extensível: Pending Id/Gang/Area12/Options/Progress(Elapsed,Faulted)/Cleared; contas Completed/StartedUtcTicks/ExpiresUtcTicks por gangue/área12;640 contas/256 recibos. Sem handles/timers/bicicleta nativa persistidos. Legado não ganha livro/bônus ao consultar; campos futuros preservados.

Begin/Update/Pass/Cancel/Complete clonam livro e fazem uma gravaçãoWallet com rollback/replay/posse/ordem/geometria/limites/overflow. Tempo não retrocede, reprovação não pode ser apagada por sample. Ready exige todos os pontos e contrato não reprovado. Remoção da pendência e bônus são atômicos. Não altera banco/cash/inventário/estoques/receita suja/tesouraria/reputação/diplomacia ou MissionWin/MissionFail entre arquivos. Clock fracionário e bônus anteriores permanecem.

DECOR só na bicicleta própria, reservada em JobVehicles; modelo assíncrono com limite10s, sem forçar montagem/teleportar/ejetar. Não apaga outros veículos/pedestres/props para abrir vaga nem muda bicicleta pessoal, polícia, relações globais ou scripts. Limpeza adiada própria drena inativa, só apaga bicicleta vazia/parada/no chão/sem conexão; início/retomada aguardam liberação anterior. Fechamento do script libera naturalmente entidades protegidas. Não depende de props opcionais para registrar o circuito: bicicleta é equipamento funcional.

## Comparação e validação

PSRP/GangSpawner.cs StartBmxPromotion/UpdateBmxPromotion/CompleteBmxPromotion conferidos: área12, bmx, início/rumo352,8152,11 coordenadas, prazo75000ms, bicicleta específica, ordem, raioXY2,5m/altura3m, anéis e bônus2XUTC60min. Código próprio; DLL de referência não executada.

Diferenças: provas persistidas por checkpoint e tempo, END/retomada com tempo preservado, finalE, prazo ativo sem menus/preparo, condutor exigido e segurança compartilhada, limpeza protegida. HUD nativo simples; fases8(interface/necessidades) e9(realismo dos trabalhos) continuam reservadas.

**2726 testes aprovados**(2688 anteriores +38 novos), build Release net48 sem erros/avisos. Ordem/raio/altura/condutor, prazo exato/rollover/pausa, snapshots/campos futuros, rollback/replay, falha/cancelamento/limites/bloqueios/UTC e preservação de bônus anteriores. **GTA não executado na nuvem**: testar acesso ao skatepark/ramps/anéis, montar e completar11 em75s, END/reload parcial/pronto, queda/timeout/cancelamento/perfil, liberação ocupada/movendo/no ar e convivência com trabalhos/bônus anteriores.
