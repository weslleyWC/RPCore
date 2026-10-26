# Fase6.5AD — motorista particular VIP — 0.72.0

## Instalação

Baixe RPCore-update-0.72.0.zip, extraia e execute Install-RPCore.ps1 **da pasta extraída**, com -GtaPath apontando à pasta real do GTA5.exe. GTA V Legacy, SHVDN Enhanced1.1.0.6 e LemonUI2.2 SHVDN3. Preserva saves/configs, celularF2, teleportF11 e recursos visuais instalados. PSRP.dll é referência estática, não dependência para instalar junto.

## Teste no GTA

1. Sua gangue precisa possuir o **território10**. F4 → Comercio dos territorios → área10 → **GPS: carro de luxo VIP**.
2. A pé a até8m do início(-83.87819,905.1129,234.9008), seguro/sem procurado e sem outra atividade/pendência, escolha **Iniciar motorista VIP**. Gratuito. Afaste-se3m da vaga e deixe o ponto do VIP livre; não apaga outros carros/pedestres.
3. Entre como motorista no carro fornecido. Um dos sete modelos é escolhido e salvo; também ficam salvos VIP, destino e perfil(normal/exigente/atrasado). Aviso mostra perfil/destino.
4. GPS muda ao VIP(-132.5137,963.5354,236.0194). Pare a até12m, abaixo de1,5m/s, e aguarde ele entrar pela porta do passageiro. Não há teleporte de embarque. Deixe a porta acessível e aproxime mais se necessário.
5. Dirija ao destino entre15 locais da PSRP, antes do prazo. Contador mostra tempo, satisfação e condição. Evite colisões e acelerações/freadas bruscas.
6. Pare a até20m do destino, abaixo de1m/s. Aguarde o VIP sair e caminhar por5s ativos. **Não se afaste mais de22m durante desembarque.** Entrega fica salva.
7. **E no destino** confirma avaliação/bônus. Pode estar dirigindo o carro próprio parado ou a pé, a até20m. Não precisa devolver carro ao início. Para liberar o carro após conclusão, saia e deixe-o parado; não é apagado ocupado/em movimento/conectado.

## Avaliação e prazo

Prazo ativo de180…300s a partir de150s+95ms por metro3D entre início e destino; atrasado reduz20s(mínimo150s). Preparação de modelos/vaga não consome prazo. Pausa/menus/suspensão/jogo fechado não consomem nem geram renda.

- **Exigente:** reclama acima de75km/h; espera após30s tira1,5ponto/s; batidas e condução brusca têm penalidades maiores.
- **Normal:** limite de conforto105km/h; espera após30s tira0,8ponto/s.
- **Atrasado:** tolera135km/h, mas parado abaixo de3km/h a mais de35m do destino perde0,5ponto/s; prazo menor.

A cada1s, perda de carroceria de pelo menos8 pontos tira0,12 por ponto de dano(0,18 exigente). Excesso de velocidade tira1ponto/s(2,2 exigente). Aceleração/desaceleração acima de8m/s², amostrada a cada250ms e a mais de25m do destino, tira0,8(1,8 exigente). Danos/tempo/satisfação são preservados; reparo externo ou retomada não melhoram a condição registrada.

Reprova ao esgotar prazo, satisfação≤15%, condição≤45%, perder carro/VIP, VIP sair antes da entrega ou afastar-se antes do desembarque. Reprovação fica salva sem prêmio: **cancele a pendência no F4** para contratar outra corrida.

## Recompensa

Depois de entrega real e confirmaçãoE:

| Avaliação | Satisfação | Carroceria | Bônus por60min reais |
|---|---:|---:|---:|
| Excelente | ≥85% | ≥85% | 2X |
| Boa | ≥60% | ≥65% | 1,5X |
| Aceitável | Acima da reprovação | Acima da reprovação | 1,25X |

As duas condições da linha precisam ser atendidas. Bônus acelera processamento automático **na área10**, não paga dinheiro pessoal/cria receita/estoque e não liga automático sozinho. Com receita suficiente e opt-in ativo, lote padrão até$500 passa de60s para48s(1,25X),40s(1,5X) ou30s(2X) ativos.85% líquido ao caixa e15% taxa permanecem. Dez atividades usam máximo2X, sem multiplicação. Sem produção offline. JanelaUTC do bônus inclui jogo fechado; nova corrida concluída renova por60min com a nova nota, replay não estende.

## Suspender e retomar

**END suspende**. Volte ao F4 para GPS/retomada/cancelamento. Nota/condição/tempo são salvos a cada1s ativo e também ao suspender normalmente; término abrupto do processo pode perder o último intervalo ainda não gravado.

- Parcial: saia do carro anterior e deixe-o parado para VIP sair e veículo liberar. Retome **a pé no ponto inicial**, com posse/segurança. Mesmo carro/modelo/VIP/destino/perfil/opções; tempo restante e notas conservados. Recria entidades próprias e **refaz embarque/viagem físicos**, sem reiniciar prazo/nota nem reparar gratuitamente a carroceria.
- Entregue: retome **a pé a até20m do destino**, confirmeE sem respawn nem nova viagem. Nota final já está salva.
- Reprovado: não permite retomada; cancele gratuitamente para iniciar outra corrida.
- Cancelamento sem prêmio parcial, disponível sem posse/filiação em segurança. Bônus anterior mantém validade original.

Morte/procurado/combate/ragdoll/missão/cutscene/perda de posse/filiação/perfil/ator,150m de separação do carro ou ocupante/conexão inesperados suspendem. Não apaga veículo pessoal/outro recurso para resolver obstrução. Pendência suspensa/reprovada bloqueia outras operações econômicas territoriais até concluir/cancelar; trava global fica livre após suspensão.

## Arquitetura e compatibilidade

GangChauffeurData.cs implementa defaults/regras/medidor/transações; GangChauffeur.cs implementa runtime/menus/GPS/spawn/embarque/desembarque/limpeza. Config **Gangs/Chauffeur.json**: sete carros, dez modelosVIP,15 destinos e pontos/rumos exatos da referência. Opções BoostMinutes60(1…180), StartRadius8(3…15),1…50 modelos/destinos válidos. Config inválida mantém arquivo e usa defaults em memória. Mudanças só afetam novas contratações, não snapshots existentes.

Wallet.GangChauffeur opcional/extensível: Pending Id/Gang/Area10/Options/Vehicle/Passenger/Destination/Personality/Progress(Elapsed,Satisfaction,BodyHealth)/Boarded/Delivered; contas por gangue Completed/Bonus/StartedUtcTicks/ExpiresUtcTicks;640 contas/256 recibos. Sem handles/timers nativos. Legado não ganha livro/opt-in ao consultar; campos futuros preservados inclusive nos samples.

Begin/Update/Board/Deliver/Cancel/Complete clonam livro e fazem uma gravaçãoWallet com rollback/replay/posse/limites/overflow. Samples não podem retroceder tempo nem melhorar satisfação/carroceria. Delivered exige Boarded e corrida não reprovada. Conclusão remove pendência+premia atomicamente; não altera banco/cash/inventário/estoques/receita suja/tesouraria/reputação/diplomacia nem MissionWin/MissionFail entre arquivos.

Relógio compartilhado de automáticos aceita fator finito1…2 em double, conservando frações de milissegundo. Vendas automáticas de armas continuam1X e bônus anteriores2X continuam iguais. Nenhum ajuste na divisão financeira85/15, lotes ou renda manual.

DECOR só no carro/VIP próprios; JobVehicles reserva veículo. Modelos assíncronos10s, sem invencibilidade/teleporte/polícia imune/relações globais/scripts encerrados. VIP embarca por IA, sem WarpIfDoorIsBlocked da fonte. Limpeza adiada pede saída natural do NPC do carro próprio parado; não apaga NPC dentro de veículo nem ejeta jogador. Carro só removido vazio/parado/sem conexão; fila drena mesmo inativa, início/retomada aguardam liberação anterior. Fechamento libera naturalmente entidades protegidas.

## Comparação e validação

PSRP/GangSpawner.cs StartPrivateChauffeur/UpdatePrivateChauffeur/UpdateChauffeurDifficulty/CompletePrivateChauffeur conferidos: área10, modelos/destinos/pontos, três perfis, prazo, amostras250ms/1s, penalidades, embarque12m/desembarque20m/5s e notas1,25/1,5/2X por1h. Código próprio; PSRP não executada.

Diferenças: persistência de performance e escolha, retomada repete físicos com nota/prazo preservados, pausa/menu congela, preparo não gasta prazo, carroceria não melhora por reparo/respawn, embarque sem warp, entrega salva/finalE e limpeza protegida. HUD nativo simples; fases8(interface/necessidades) e9(realismo dos trabalhos) continuam reservadas.

**2640 testes aprovados**(2588 anteriores +52 novos). Build Release net48 sem erros/avisos. Testes de thresholds, penalidades, amostras/pausa/rollover, clock fracionário, snapshots/limites/campos futuros, rollback/replay e falhas. **GTA não executado na nuvem:** validar spawn nas colinas, porta/embarque/desembarque/5s, destinos/trânsito/prazo/notas, END/reload/reprovação/cancelamento/perfil, limpeza ocupada, intervalos48/40/30s e coexistência com trabalhos/bônus anteriores.
