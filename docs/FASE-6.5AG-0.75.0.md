# Fase6.5AG — entrega em alto-mar — 0.75.0

## Instalar e testar

Extraia RPCore-update-0.75.0.zip e execute Install-RPCore.ps1 **da pasta extraída**, apontando -GtaPath à pasta real do GTA5.exe. GTA Legacy, SHVDN Enhanced1.1.0.6 e LemonUI2.2 SHVDN3. Preserva saves/configs, celularF2, teleportF11 e recursos visuais. PSRP.dll foi referência estática, não dependência para instalar junto.

1. Sua gangue precisa possuir o **território13**. F4 → Comercio dos territorios → área13 → **GPS: pacote Vespucci**.
2. A pé a até8m da coleta(-1065.109,-1158.42,1.383517), seguro e sem outra atividade/pendência, selecione **Entregar pacote em alto-mar**. Gratuito.
3. Com ponto aquático livre, aparece o Seashark(-1103.408,-1152.185,-0.5149324). O prazo de**cinco minutos ativos começa quando ele aparece**.
4. **E a pé a até2m da coleta** confirma o pacote. Pacote é carga abstrata do contrato como na fonte; não adiciona/retira item ou dinheiro pessoal.
5. Entre como motorista no **Seashark fornecido** e siga o GPS ao comprador(-2279.427,-959.0692,-0.5984672). O Squalo com comprador empresarial é criado quando você fica a até300m do encontro, com ponto livre.
6. Aproxime-se a **até5m do centro do barco**, abaixo de**1,5m/s**, permanecendo no Seashark próprio por**cinco segundos contínuos**. Afastar/acelerar/desembarcar/abrir menu interrompe a transferência ainda não confirmada. As ondas e o posicionamento precisam de teste no jogo.
7. Entrega fica salva. **E ali no Seashark**, próximo e em baixa velocidade, confirma o contrato e libera **processamento2X na área13 por60minutos reais**. Também pode confirmarE a pé no ponto original de coleta em terra depois da entrega salva.

Mantém câmera e controles normais durante a transferência, com indicação nativa. Não repete a câmera cinematográfica, congelamento do Seashark ou bloqueio de saída/aceleração da PSRP. Apenas o barco próprio do comprador fica ancorado/congelado enquanto ativo.

## Suspender e retomar

**END suspende**. F4 mostra GPS/retomada/cancelamento.

- Parcial: saia do Seashark anterior e deixe-o em baixa velocidade para liberar. Retome **a pé no ponto de coleta em terra**, com posse/segurança. Mantém pacote já coletado e tempo gasto; reconstrói embarcações próprias e refaz embarque/viagem, sem repetir a coleta confirmada ou reiniciar prazo.
- Entregue: retome **a pé a até2m da coleta em terra** e confirmeE. Não recria embarcações nem exige nova viagem ao mar; a prova de entrega está salva.
- Prazo esgotado ou equipamento/comprador perdido/destruído: reprovação salva, sem prêmio. Cancele a pendência no F4 para contratar outra entrega.
- Cancelar gratuito, sem prêmio parcial e disponível sem posse/filiação em segurança; conserva bônus anterior.
- Save falhou: conserva etapa anterior; coleta não confirmada pode repetir, entrega conserva coleta/embarque e finalização conserva entrega pronta.

Tempo salvo a cada1s ativo e nas transições/suspensão normal; fechamento abrupto pode perder o último intervalo não gravado. Pausa/menus/suspensão/GTA fechado e preparação de modelos/vaga não contam. Os cinco segundos completos de transferência precisam terminar antes do prazo, diferentemente do estágio cinematográfico da fonte que ignora deadline depois de iniciado.

Morte/procurado/combate/ragdoll/missão/cutscene/perda de posse/filiação/perfil/ator,150m de separação do Seashark, ocupante ou conexão inesperados suspendem com provas conservadas. Pendência suspensa/reprovada bloqueia outras operações econômicas territoriais até concluir/cancelar; trava global fica livre após suspensão.

## Recompensa e saves

Bônus2X não liga automático sozinho nem cria receita/estoque/pagamento pessoal. Com receita suficiente e automático ligado, padrão até$500 passa de60s para aproximadamente30s ativos, mantendo85% líquido ao caixa e15% taxa. Treze atividades usam máximo2X, sem multiplicação; bônus fracionários anteriores preservados. ValidadeUTC inclui GTA fechado; nova conclusão renova60min sem somar duração, replay não estende. Sem produção offline.

GangOffshoreData.cs/GangOffshore.cs e **Gangs/Offshore.json**. Opções BoostMinutes60(1…180), StartRadius8(3…15), SecondsLimit300(60…900), modelos e três pontos/rumos da fonte. Config inválida é mantida e usa defaults em memória; alterações só afetam novos contratos, preservando snapshot atual.

Wallet.GangOffshore opcional/extensível: Pending Id/Gang/Area13/Options/Progress(Elapsed,Faulted)/Collected/Boarded/Delivered; contas Completed/StartedUtcTicks/ExpiresUtcTicks por gangue/área13;640 contas/256 recibos. Sem handles/câmera/timers nativos. Boarded exige Collected e Delivered exige ambos/contrato não reprovado. Begin/Update/Collect/Board/Deliver/Cancel/Complete clonam livro e gravam uma Wallet com rollback/replay/posse/limites/overflow, monotonicidade do tempo/reprovação e campos futuros preservados. Remoção da pendência+prêmio atômicos; sem mutação de banco/cash/inventário/estoques/receita suja/tesouraria/reputação/diplomacia/MissionWin/MissionFail.

## Recursos e limitações

Só Seashark/Squalo/comprador próprios têm DECOR; reserva veículos em JobVehicles. Modelos assíncronos10s e spawns livres4m/7m sem apagar outras embarcações/pedestres. Comprador próprio desarmado/sem dinheiro, criado e colocado no próprio barco, motor desligado. Limpeza descongela só o barco próprio mesmo ocupado, e libera naturalmente comprador+barco quando não há ocupantes externos; não apaga NPC dentro de veículo. Seashark só removido vazio/lento/no chão ou água/sem conexão; fila drena inativa e novo início/retomada aguardam liberação anterior. Fechamento libera naturalmente entidades protegidas. Não move jogador/carros/barcos pessoais, muda câmera, polícia, relações globais ou scripts.

PSRP/GangSpawner.cs StartOffshoreDeal/UpdateOffshoreDeal/StartOffshoreDealCinematic/CompleteOffshoreDeal conferidos: área13, seashark/squalo/a_m_y_business_01, pontos/rumos, coleta2m, prazo300000ms, embarcação específica,5m/<1,5m/s, transferência5000ms e2XUTC60min. Código próprio. Diferenças: persistência/retomada em terra, prazo ativo inclui transferência, comprador próximo, finalE e limpeza protegida; sem câmera/controles bloqueados. HUD nativo simples; fases8(interface/necessidades) e9(realismo dos trabalhos) continuam reservadas.

**2759 testes aprovados**(2726 anteriores +33 novos), build Release net48 sem erros/avisos. Etapas/prazo exato/transferência contínua/raio/velocidade, rollback/replay, snapshots/campos futuros, monotonicidade/bloqueios/limites/UTC. **GTA não executado na nuvem**: testar acesso ao Seashark/água, comprador/streaming, ondas/5m/5s, E/END/reload parcial/pronto, tempo/cancelamento/perfil, ocupação/liberação de barcos e coexistência com bônus/trabalhos anteriores.
