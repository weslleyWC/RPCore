# Fase6.5AE — frete do porto — 0.73.0

## Instalação

Baixe RPCore-update-0.73.0.zip, extraia e execute Install-RPCore.ps1 **da pasta extraída**, com -GtaPath apontando à pasta real do GTA5.exe. GTA V Legacy, SHVDN Enhanced1.1.0.6 e LemonUI2.2 SHVDN3. Preserva saves/configs, celularF2, teleportF11 e recursos visuais instalados. PSRP.dll é referência estática, não dependência para instalar junto.

## Teste no GTA

1. Sua gangue precisa possuir o **território11**. F4 → Comercio dos territorios → área11 → **GPS: caminhao do porto**.
2. A pé a até8m do início(-779.6579,-2662.85,13.88363), seguro e sem outra atividade/pendência, escolha **Transportar carga /7 paletes**. Gratuito. Afaste-se4m e deixe livres as vagas do caminhão e da carreta; não remove veículos existentes para abrir espaço.
3. Entre no **Phantom fornecido**. GPS indica a **trflat própria**(-816.874,-2627.552,13.88067). Dê ré para engatar fisicamente. Se estiver alinhado, bem próximo e parado, **E ajuda o engate** usando a mesma regra segura da LS GAS; não aproxima/teleporta os veículos à distância.
4. Vá à baia(-765.6149,-2603.296,13.31192). Estacione o **centro da CARRETA a até5m do ponto**, com o caminhão certo engatado e você ao volante. Caminhão e carreta precisam ficar abaixo de0,5m/s.
5. **Carrega sozinho:3s contínuos por palete**, sete ao todo. Alterna sacos de cimento/blocos, presos à plataforma. Cada confirmação é salva. Mover/desengatar/sair do volante/abrir menu interrompe o intervalo ainda não confirmado.
6. Com7/7, siga GPS até entrega(1377.557,-743.3366,66.7329). Estacione o **centro da carreta a até9m**, engatada e parada, permanecendo ao volante.
7. **Descarrega sozinho:2s contínuos por palete**, removendo do último ao primeiro. Ao completar7/7 descarregados, **E confirma o frete**. Pode continuar no caminhão parado/engatado com a carreta dentro da área, ou sair e confirmar a pé a até9m do ponto.
8. Confira bônus no F4. Saia e deixe o conjunto parado para liberar os veículos após conclusão; não desaparecem ocupados, em movimento ou conectados a terceiros.

## Prazo, condição e recompensa

Prazo padrão de**10minutos ativos** após preparar os veículos. Pausa/menus/suspensão/GTA fechado não contam. Tempo e danos são salvos a cada1s ativo e ao suspender normalmente; término abrupto pode perder o último intervalo ainda não gravado.

A condição segue a fórmula da PSRP: **100−(1000−carroceria)/30**, com carroceria nominal inicial1000 e valor mínimo registrado preservado. Ela reduz o dano a um terço e **não é a porcentagem bruta da carroceria**. Assim, carroceria700 mostra90%,100 mostra70% e0 mostra aproximadamente66,67%. O corte50% da fonte não é alcançável só por carroceria não negativa nessa base; equipamento destruído/indisponível também reprova. Reparo externo/retomada não aumenta nota registrada.

| Condição da PSRP | Bônus por60min reais |
|---|---:|
| ≥90% | 2X |
| ≥70% | 1,5X |
| Abaixo de70%, contrato válido | 1,25X |

Prazo esgotado, condição abaixo50%, caminhão perdido/morto/sem condições de dirigir ou carreta perdida/destruída reprovam sem recompensa. Reprovação fica salva: **cancele a pendência no F4** antes de contratar outro frete.

Bônus acelera processamento automático **na área11**. Não paga dinheiro pessoal, cria receita/estoque ou liga automático sozinho. Com receita suficiente e automático ligado, lote padrão até$500 passa de60s para48s(1,25X),40s(1,5X) ou30s(2X) ativos.85% líquido ao caixa/15% taxa continuam. Onze atividades usam máximo2X, sem multiplicação e sem renda offline. ValidadeUTC inclui jogo fechado; nova conclusão real renova janela com nova nota, replay não estende.

## Suspender, retomar e cancelar

**END suspende** e preserva contagens/carga restante/tempo/danos. F4 permite GPS/retomada/cancelamento.

- Parcial: saia e pare o conjunto anterior para liberar. Retome **a pé no início**, com posse/segurança. Recria caminhão/carreta próprios e apenas os paletes restantes. Contagens de carregamento e descarga já confirmadas permanecem; refaz engate e viagem, sem reiniciar prazo ou reparar danos.
- Se faltava carregar, volte à baia para os paletes restantes. Se carga já estava completa, siga à entrega; descarga anterior não é repetida nem creditada novamente.
- Pronto7/7 descarregados: retome **a pé a até9m da entrega** e confirmeE. Não precisa recriar veículos/carga ou repetir viagem.
- Cancelamento gratuito, sem prêmio parcial e disponível sem posse/filiação em segurança. Bônus anterior mantém vencimento.
- Falha no save conserva estado anterior; palete não recebe prova parcial e finalização mantém descarga pronta. Props são removidos só depois da confirmação da descarga.

Morte/procurado/combate/ragdoll/missão/cutscene/perda de posse/filiação/perfil/ator,150m de separação do caminhão ou ocupante/conexão inesperados suspendem. Perda de palete visual próprio suspende e conserva provas; retomada reconstrói carga restante. Pendência suspensa/reprovada bloqueia outras operações econômicas territoriais até concluir/cancelar. Trava global fica livre após suspensão.

## Arquitetura e compatibilidade

Novos GangDockData.cs/GangDock.cs e **Gangs/DockCargo.json**. Defaults phantom/trflat, dois modelos de paletes, sete offsets e quatro pontos/rumos da fonte. BoostMinutes60(1…180), StartRadius8(3…15), SecondsLimit600(120…1200),1…20 slots distintos. Config inválida é mantida no disco e usa defaults em memória; mudanças afetam próximo contrato e preservam snapshot atual.

Wallet.GangDock opcional/extensível: Pending Id/Gang/Area11/Options/Progress(Elapsed,BodyHealth,Faulted)/Loaded/Unloaded; contas Completed/Bonus/UTC;640 contas/256 recibos. Sem handles/timers/carga nativa persistidos.0≤Unloaded≤Loaded≤meta; descarregar exige carga completa; pronto exige todos descarregados e contrato não reprovado. Legado consultado não ganha livro/bônus.

Begin/Update/Load/Unload/Cancel/Complete clonam livro e fazem uma gravaçãoWallet com rollback/replay/posse/limites/overflow. Tempo não retrocede, carroceria não melhora, reprovação não pode ser apagada por sample. Conclusão remove pendência e concede bônus atomicamente. Não altera banco/cash/inventário/estoques/receita suja/tesouraria/reputação/diplomacia ou MissionWin/MissionFail entre arquivos. Clock fracionário e divisão85/15 existentes permanecem.

DECOR só no conjunto/paletes próprios; JobVehicles reserva ambos os veículos. Modelos assíncronos10s; spawn com espaço livre de7m para caminhão/9m para carreta e jogador a4m. Não limpa carros/pedestres/props do mapa para abrir vaga. Verifica **GET_VEHICLE_TRAILER_VEHICLE** para exigir a carreta exata, evitando carregar outra carreta.

Visuals.Enabled/UseProps capturados: props cosméticos opcionais, mas mesmos sete transfers/tempos/contagens exigidos sem eles. Paletes próprios sem colisão, presos nos offsets da referência. Limpeza adiada só remove conjunto vazio/parado/sem conexão externa e desengata apenas par próprio verificado; tokens alterados não autorizam apagar veículo de terceiro. Fila drena inativa e início/retomada aguardam liberação anterior. Fechamento marca liberação natural sem ejetar/teleportar jogador. Não altera polícia, relações globais, scripts ou arquivosRPF.

## Comparação e validação

PSRP/GangSpawner.cs StartDockCargoContract/UpdateDockAutomaticLoading/UpdateDockCargoContract/UpdateDockAutomaticUnloading/GetDockTrailerCondition/CompleteDockCargoContract conferidos: área11, modelos/pontos/rumos/slots, dez minutos, carga3s/5m, descarga2s/9m, velocidade0,5m/s, fórmula de condição e tiers90/70 para2/1,5/1,25XUTC1h. Código próprio; DLL de referência não executada.

Diferenças: provas persistidas por palete, retomada parcial com entidades reconstruídas, prazo ativo exclui preparo/menus, condutor exigido nas duas operações, carreta exata, finalE, END e limpeza protegida. Base nominal1000 de carroceria, danos preservados em respawn. HUD nativo simples; fases8(interface/necessidades) e9(realismo dos trabalhos) continuam reservadas.

**2688 testes aprovados**(2640 anteriores +48 novos), build Release net48 sem erros/avisos. Casos de ordem/carga/descarga, rollback/replay, nota/fórmula/prazo, tempo/danos/snapshots/campos futuros, bloqueios/limites/UTC e timers de transferência. **GTA não executado na nuvem**: testar spawn/engate físico/E auxiliar, centro da carreta nas duas áreas, paletes/offsets, tempos3s/2s, rota de dez minutos, retomada parcial/pronta, falha/cancelamento/perfil, limpeza ocupada/conexões e bônus anteriores.
