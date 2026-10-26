# 0.84.0 — The Hollywood Act, seis atos

Acesso: **F11 → Contratos / assalto ao estúdio**, menu centralF10 ou **F2 → Contatos → Lester / Hollywood Act**. Sem ativação automática/alteração de saves antigos. Aceite sem trabalho/missão/procurado. Cada ato reserva a atividade compartilhada; NPCs/veículos/props são próprios. Há risco de combate e polícia nativa. Não há teleporte, outfit forçado, confisco de itens ou supressão global da polícia.

## Roteiro dos seis atos

1. **Suprimentos:** siga GPS para a vanBurrito3 emLaMesa. Guardas próprios podem detectar e atacar. Roube a van real e leve ao depósito. Pare ao volante até5m do ponto, sem procurado; **E** entrega. Não paga preparação.
2. **Documentos:** aceite entre21h e antes5h. Siga oJugular e vá a pé para trás dele. **E** inicia fechadura; pressioneE quando a barra estiver entre80% e88%. Três acertos consecutivos, cinco erros reprovam; velocidade depende de tempo, nãoFPS. Depois **E** pega documentos no porta-malas. Vá ao ponto de entrega, pare a pé e **E**, sem procurado. Tiros/estrelas reprovam este ato. Horário só restringe o início/retomada, não exige mudar o relógio do jogo.
3. **Carro de fuga:** roube oLocust indicado, com vigia próprio. O método daPSRP se chamaCreateBallerVehicle, mas criaLocust; usamos o modelo real. Leve à oficina, pare ao volante e **E** sem procurado.
4. **Barco/reboque:** siga GPS àBison na marina. Obtenha chave do dono próprio: próximo a pé, **E** enquanto aponta uma arma para intimidar, ou **E** no corpo após neutralizá-lo. Guardas reagem à retirada. Entre naBison e entregue comBoatTrailer2 próprio engatado. Se soltar, aproxime parado ao volante e **E** a menos10m do reboque tenta reengatar. Entrega exige ambos vivos, Bison parada até5m, reboque até15m e sem procurado; **E** confirma.
5. **Distração:** recolha a bolsa com **E** a pé junto ao contato. Leve ao local do concerto e deixe com **E**, parado a pé/sem procurado. Bolsa aparece na mão e se oculta dentro do veículo. Preparação fica salva para o final.
6. **Final:** dirija o carro preparado; pare no GPS da portaria, aguarde o porteiro chegar e **E** apresenta documentos. Entre e estacione no GPS, então desembarque próximo do carro. Portões/barreiras do mapa não são alterados: se outro mod bloquear entrada, use acesso livre ou ajuste os pontos antes de novo contrato.

No final, encontre o diretor marcado (um de quatro pontos). Ao chegar15m ele caminha; siga entre5e80m. Perto do destino, neutralize sem disparar e recolha chave no corpo com **E**. A morte precisa ter dano do jogador, sem tiros/procurado antes de obter o dispositivo. Abra o porta-malas doSchafter marcado com **E** a pé: obtém dispositivo e alerta guardas, podendo solicitar até3estrelas nativas, limitado pelo máximo do jogo, sem reduzir estrelas existentes.

Afaste-se400m do estúdio e despiste a polícia normalmente. **F11 → Contratos → Acionar distração preparada** durante fuga solicita distração; após20segundos ativos, guardas do contrato recuam à portaria. Uso salvo uma vez. Diferença deliberada daPSRP: não reduz estrelas, explode objetos ou muda dispatch. Se interrupção acontecer antes da confirmação, o pedido persiste e os20s recomeçam quando a retomada alcançar a fuga; um uso já confirmado não repete o efeito.

Dirija oDinghy3 marcado. Vá aoLongfin do comprador; aproxime a até12m, fique abaixo1,5m/s ao volante e sem procurado. **E** inicia transferência de5s contínuos. Se afastar/mover/perder comprador, reinicia a transferência. Entrega pronta fica salva; **F11 → Contratos → Receber** deposita a recompensa definida ao aceitar o final (padrão$200.000–$450.000). Saldo bancário no limite conserva o recebimento pronto para tentar depois; preparação não paga. Após pagamento único, os cinco preparativos reiniciam para novo ciclo.

## Interrupções e preservação

- **END/Suspender/recarregar:** conserva preparações concluídas, recompensa e prazo ativo salvo; retomar reinicia somente as etapas físicas do ato atual. Não reconstitui NPCs/veículos destruídos de uma sessão anterior nem pula etapas. Pronto para receber não recria missão.
- Prazo padrão30min ativos por ato. Menus/modais/pausa e carregamento não consomem; não há tempo offline. Checkpoint a cada5s e ao suspender/salvar. Queda abrupta pode perder até5s ainda não gravados. Expirar/morrer/falhar não apaga preparações anteriores.
- **Cancelar** confirma desistência do ato/recompensa pendente, conserva preparações anteriores. Não apaga estrelas. Banco/recibo/conclusão gravados juntos com rollback; recepção repetida não paga de novo.
- Configuração completa`Missions/StudioHeist.json`, com modelos, posições, guardas, prazo e faixa de prêmio. O contrato captura snapshot: editar arquivo não muda tentativa existente. Cancele a tentativa e reinicie o script para usar novos parâmetros. Config inválida é preservada e usa defaults em memória; save inválido fica protegido.
- Spawns são locais, com modelos assíncronos/limite10s, piso/água/vaga verificados, com espera de até5s para o ponto. Se ponto estiver ocupado ou não carregar, suspende sem remover ambiente; afaste-se/libere vaga e retome. Veículos próprios ocupados/em movimento/anexados não são apagados; saia quando seguro para permitir limpeza. F2, teleporteF11, empregos, multas/custódia eOIV/RPF/YTD anteriores mantidos.

## Validação e diferenças

3157 testes aprovados, Release net48 sem erros/avisos. Auditoria compara49pontos/guardas/diretores, modelos reais, cinco preparações/final, noite21–5, fechadura3/5, distância5/400, distração20s e prêmio da referência. Testes cobrem ordem/provas, snapshots, interrupção/recarga, prazo, recibos, pagamento único, rollback, overflow, metadados e reserva exclusiva.

GTA não foi executado: testar todos os atos, pisos/portões/mapa instalado, engate de reboque, diretor/navmesh/stealth, polícia e mar/comprador/limpeza. A apresentação/câmeras/gestos/dificuldade e física fina serão refinadas nas fases8/9. Não são copiados efeitos globais da referência(MaxWanted0, gangues ambiente hostis, imunidade/controle bloqueado, rotação de barreiras, apagar estrelas ao terminar). Halloween/editor/revisão final ainda pendentes na fase7.
