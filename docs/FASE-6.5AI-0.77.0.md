# Fase6.5AI — turno de caixa 24/7 — 0.77.0

## Instalação e teste no GTA

Extraia RPCore-update-0.77.0.zip e execute Install-RPCore.ps1 **da pasta extraída**, apontando -GtaPath à pasta real do GTA5.exe. GTA Legacy, SHVDN Enhanced1.1.0.6 e LemonUI2.2 SHVDN3. Preserva saves/configs, celularF2, teleportF11 e recursos visuais. PSRP.dll foi referência estática, não dependência para instalar junto.

1. Sua gangue precisa possuir o **território15**. F4 → Comercio dos territorios → área15 → **GPS: trabalhar no caixa 24/7**.
2. Na loja(-3040.46,583.7421,7.908928), a pé a até8m do ponto e sem outra atividade/pendência, escolha **Turno caixa /5 clientes /$400**.
3. **E a pé a até2,5m do ponto do caixa** inicia atendimento. O atendente original é preservado. Pode trabalhar pelo **espaço livre no lado externo do balcão**, sem ocupar o posto do NPC. Início original da PSRP exige1,2m e apaga o atendente; RPCore amplia para2,5m e não o apaga/move.
4. Aguarde o cliente criado pelo RPCore entrar, visitar **duas a quatro prateleiras** e chegar ao balcão. Ele para1,5…3s em cada prateleira. Não é teleportado; deixe o caminho livre. Compras normais do RPCore ficam suspensas enquanto este turno usa a atividade, evitando conflito comE.
5. No caixa, **E registra um produto por vez**, com intervalo mínimo450ms. A cesta tem **dois a sete itens**, escolhidos dentre52 produtos/preços da PSRP. Subtitle mostra item, contagem e total. Permaneça a pé a até2,5m do caixa; cliente precisa estar a até0,65m de seu ponto.
6. Quando todos estiverem registrados, **E novamente recebe o pagamento do cliente**. Isso confirma um atendimento e acumula$80 de salário previsto, sem creditar ainda no seu dinheiro. Cliente sai; aguarde o próximo.
7. Complete **cinco clientes em dez minutos ativos**. Depois, **E no caixa conclui e recebe$400 em dinheiro pessoal**. Confira dinheiro/carteira/HUD e histórico no F4. Banco não muda.

**Esta atividade paga salário e não concede bônus de processamento.** A mensagem da PSRP anuncia laundering boost, mas CompleteCashierShift apenas soma cashierEarned ao Game.Player.Money e não define multiplicador/expiração. RPCore segue o comportamento real. Total dos produtos vendidos não é salário nem receita suja/inventário: paga-se80 por cliente,400 no turno padrão.

## Suspender, retomar, cancelar

**END suspende**. F4 permite GPS/retomada/cancelamento.

- Parcial: retome a pé no caixa com posse/segurança. Preserva atendimentos, cesta/modelo/rota do cliente atual, itens registrados e tempo gasto. Cliente atual refaz seu percurso físico nas prateleiras; não muda cesta nem perde registros já salvos. E no caixa inicia o atendimento da retomada.
- Pronto5/5: retome no caixa e confirmeE. Não recria cliente nem exige novo turno; salário ainda está pendente e só pode ser pago uma vez.
- Reprovação por prazo/cliente morto: cancele a pendência para contratar outro turno. Sem salário parcial, como na fonte.
- Cancelar gratuito e disponível sem posse/filiação em segurança; descarta somente turno ainda não pago, conservando dinheiro/histórico e bônus anteriores.
- Save falhou: conserva cesta/registro/atendimentos anteriores. Falha ao pagar salário restaura livro e dinheiro na sessão e mantém5/5 pronto para tentar novamente.

Relógio inclui espera/caminho/registro desde ativação, mas pausa/menus/suspensão/GTA fechado não contam. Salva a cada1s ativo e ao suspender/transições; encerramento abrupto pode perder o último intervalo não gravado. Prazo congela após quinto pagamento confirmado, aguardandoE final.

Morte/procurado/combate/ragdoll/missão/cutscene/perda de posse/filiação/perfil/ator, afastamento50m ou cliente entrando em veículo suspendem. Falha de navegação por90s ativos suspende preservando cesta/tempo; libere caminho e retome. Pendência suspensa/reprovada bloqueia outras operações econômicas territoriais até concluir/cancelar; trava global fica livre após suspensão.

## Arquitetura e compatibilidade

GangCashierData.cs/GangCashier.cs e **Gangs/Cashier.json**. Defaults5 clientes/80 salário/600s,52 produtos,40 modelos, seis prateleiras/rumos, dois spawns e pontos de caixa/cliente/saída da fonte. Opções Goal1…20, Wage1…1000, SecondsLimit120…1800, StartRadius3…15; listas válidas com limites. Modelos não disponíveis são filtrados somente ao escolher novo cliente; pedido salvo indisponível é conservado e suspenso. Config inválida mantida usa defaults em memória. Mudanças só afetam novos contratos, sem alterar snapshot atual.

Wallet.GangCashier opcional/extensível: Pending Id/Gang/Area15/Options/Progress(Elapsed,Faulted)/Served/Customer(Id,Model,Basket,Route,Scanned); Accounts Gang/Area15/Completed/Customers/Earned;640 contas/256 recibos. Sem handles/timers de NPC. Cesta2…7 permite produtos repetidos; rota2…4 exige prateleiras distintas; Scanned não excede cesta. Ready exige meta cumprida/sem pedido/sem reprovação.

Begin/Update/Customer/Scan/Pay/Cancel clonam livro e gravam uma Wallet com rollback/replay/posse/ordem/limites/overflow. Salário final publica livro novo, credita Cash real via delegates, remove pendência e salva uma vez. Em personagemMP, cash e livro ficam na mesma Wallet.json. Em protagonistas, dinheiro nativo do GTA é estado externo: há rollback de falhas síncronas e recibo contra replay, mas não é possível garantir atomicidade entre GTA e Wallet em encerramento abrupto entre gravações. Segue o modelo existente DeliveryPayments. Não substitui dinheiroMP ao operar protagonista; banco, inventário, estoques, receita suja, tesouraria, reputação/diplomacia e14 bônus anteriores permanecem.

DECOR só no cliente próprio, desarmado/sem dinheiro, modelos assíncronos10s e spawns livres. Navmesh com raio0,5m nas prateleiras/0,35m no caixa, retry5s; cenário de espera só no NPC próprio. Preserva atendente/pedestres/veículos de outros recursos; não usa HideOriginalCashier nem encerra scripts/zera procurado. Cliente próprio sai e é liberado naturalmente; limpeza não apaga NPC dentro de veículo. Não modifica câmera, relações globais ou propriedades do jogador.

ShopSystem recebe condição específica da trava gang_cashier para suprimir apenas menu/marker de compras RPCore durante turno ativo; ao suspender/concluir volta automaticamente. Outros trabalhos e lojas mantêm comportamento anterior. F2/F11/saves e RPF/YTD não mudam.

## Validação e limites

PSRP/GangSpawner.cs StartCashierShift/SpawnNextCashierCustomer/GetRandomCashierShoppingRoute/GenerateCashierBasket/GetCashierItemPrice/UpdateCashierCheckout/CompleteCashierShift conferidos: área15, pontos/rumos,40 modelos/52 itens e preços,2…4 prateleiras/2…7 itens, cinco clientes,450ms scans,80 por cliente,600000ms e dinheiro pessoal ao terminar; ausência de atribuição real de bônus. Código próprio.

Diferenças: preserva atendente original e permite lado externo, persistência por pedido/item/cliente/tempo, END/retomada, finalE, prazo ativo e navegação protegida sem warp; nomes legíveis por ID/subtitle, sem painel completo de texturas da PSRP. Fases8(interface/necessidades) e9(realismo dos trabalhos) continuam reservadas.

**2832 testes aprovados**(2797 anteriores +35 novos), build Release net48 sem erros/avisos. Cesta/ordem/scans, salário exato/MP/protagonista/replay/overflow/rollback, snapshots/campos futuros, limites/bloqueios/prazo. **GTA não executado na nuvem:** testar acesso ao balcão/lado externo/convivência com atendente e reação do jogo, supressão do menu de compras, navegação nas prateleiras/caixa, scans/sons/pagamento/saída, cinco clientes em10min, cash real, END/reload parcial/pronto/falha/cancelamento/perfil e recursos anteriores.
