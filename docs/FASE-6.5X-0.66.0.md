# Fase6.5X — recuperação com guincho territorial — 0.66.0

## Instalar e iniciar

Baixe RPCore-update-0.66.0.zip da branch rpcore-entrega, extraia e execute Install-RPCore.ps1 **da pasta extraída**, passando a pasta real do GTA Legacy que contém GTA5.exe. Mesmo procedimento anterior, SHVDN Enhanced1.1.0.6 e LemonUI2.2 SHVDN3. Saves/configs/F2/teleportF11 e visuais .oiv/.rpf/YTD preservados. PSRP.dll é referência de análise, não dependência para instalar junto.

1. Controle **território3** com sua gangue. F4 → Comercio dos territorios → seção da área3 → **GPS: patio de recuperacao**.
2. Vá a pé ao pátio, a até**8m** do ponto(-44.64281,-1683.975,29.38501), em segurança, sem procurado/combate/ragdoll/missão/cutscene/modal ou atividade/contrato de frete/roubo/lote/divulgação/oficina/guincho pendente.
3. Deixe livre a vaga do guincho(-48.79256,-1690.796,29.41298), estacione seu carro longe dela e selecione **Recuperar veiculos / 2 chamados**. Gratuito, sem prêmio ao iniciar. O guincho é fornecido pelo contrato; entre no banco do motorista do veículo marcado.

## Buscar e engatar

O GPS aponta o chamado. O contrato já salva modelo/local/cliente de cada recuperação, a partir dos126 carros/24 modelos de cliente/15 locais da PSRP. O carro e cliente próprios aparecem quando você estiver a até120m e o ponto estiver livre/carregado. Sem espaço ou passeio seguro para o cliente, aguarda; não remove carros/props de terceiros. Carro avariado: motor200, carroceria800 e pisca-alertas.

**Controles com NumLock ligado:** NUM8 sobe o braço; NUM2 desce. Altura0…100%; para engatar/descarregar precisa de**20% ou menos**. Movimento segue0,025 a cada35ms ativos, com limites.

1. No volante do guincho fornecido, alinhe sua traseira com o carro do chamado. O ponto cerca de4m atrás do centro do guincho deve ficar a até**3m do carro**, com velocidade abaixo**1m/s** e braço baixo.
2. Pressione **E** para o engate físico nativo. Só aceita o par próprio do contrato, carro desocupado e sem conexão com outro guincho. O GPS muda ao pátio quando confirma o engate real.
3. Volte dirigindo com o carro preso ao guincho. Não troque por um carro pessoal/ambiente. Se soltar no caminho, GPS volta ao mesmo carro: reengate, sem contar entrega.

O mod usa os nativos reais de braço/engate; posição e direção de aproximação importam. Pode ser necessário reposicionar o guincho e baixar mais o braço. Não teleporta veículos nem usa carregamento abstrato.

## Descarregar e concluir

No pátio, pare com **guincho a até12m** e **carro rebocado a até16m** do ponto de entrega. Velocidade abaixo1m/s, braço até20%, no volante e carro rebocado vazio. **E descarrega**. Mantenha-se parado no volante enquanto o carro desacopla e assenta; há até10s ativos para conferir as condições, com carro abaixo1m/s.

Só descarga comandada comE e par próprio/condições válidas conta. Se soltar sozinho, mesmo dentro do pátio, reengate e descarregue comE. Saída da área/veículos em movimento/ocupação/conexão externa não concede entrega. Sem prova válida, conserva o chamado e pede reconexão.

A entrega salva a recuperação uma vez e mostra a condição baseada na perda de carroceria desde800: **excelente <75**, **boa de75 até abaixo200**, **ruim a partir200**. A avaliação é informativa, não pagamento ou multiplicador adicional. Veículo destruído/ocupado suspende sem contar; retome o mesmo chamado pelo F4.

Faça as **duas recuperações**. Depois, saia do guincho, vá a pé ao ponto do pátio e **E confirma o contrato**. Se não aparecer ajuda, aproxime-se do ponto a até8m. O bônus não é dado ao engatar ou entregar só o primeiro carro.

## Bônus e convivência

Confirmação final libera **processamento automático2X na área3 por60minutos reais**. Linha **Guincho / bonus2X** no F4 mostra validade/histórico; reabra para atualizar.

Não liga automático sozinho. Com receita suja e processamento automático ligado na área3, jogando ativo/seguro com trava livre e sem contratos, padrão consome até**$500 a cada aproximadamente30s ativos**, com**$425 ao caixa** num lote inteiro e15% de taxa/arredondamento cumulativo existentes. Sem saldo sujo não cria renda. Banco/cash/estoque/reputação/diplomacia não mudam ao premiar.

Mantém tempos/valores do processamento manual e da venda automática de armas. Café(area4), tatuagem(area1), oficina(area2) e guincho(area3) têm livros/validade separados; usa **máximo2X**, nunca4X/8X. Exige posse/filiação/perfil corretos para produzir.

Validade realUTC inclui menus/pausa/GTA fechado, como na PSRP. Produção somente no jogo ativo/apto; sem renda offline/catch-up. Expirado volta à velocidade normal. Outra rota completa renova60min a partir da nova confirmação, sem somar duração restante. Replay/E repetido não estende. Horário do GTA não renova eUTC anterior à concessão não ativa; não há relógio externo antitrapaça.

**H dos pisca-alertas permanece.** Descarga éE, para não conflitar com esse atalho. Não encerra controller_towing nem qualquer script nativo/mod instalado. Suprime apenas Context enquanto dirige o guincho próprio com este serviço ativo; eventosE do RPCore executam o engate/descarga. A convivência efetiva com guinchos/serviços do GTA depende do teste no jogo.

## END, retomada e cancelamento

END suspende. Morte/procurado/combate/ragdoll/missão/cutscene/perda de posse/filiação/perfil/ator/atividade incompatível suspendem. Guincho destruído/indisponível ou afastamento maior150m dele, carro do chamado destruído/ocupado ou conexão com veículo externo também suspendem. Pausa/menu/modal congelam braço/tempo/ações; não geram entrega ou recuperação ausente.

F4 mostra **Guincho pendente**, retomada, GPS e cancelamento:

- **Parcial0…1/2:** saia do guincho antigo e retorne a pé ao pátio, seguro/com posse e sem outra atividade. Recria o guincho e a recuperação atual, mantendo **o mesmo modelo, cliente, local/opções e entregas/avaliações anteriores**. Refaça engate/viagem do chamado atual; handles/engate em andamento não são salvos.
- **Pronto2/2:** retome a pé no pátio e confirmeE sem gerar outro guincho/carro.
- **Cancelar:** grátis, sem estorno/pagamento/prêmio parcial, mesmo sem posse/filiação em segurança. Bônus anterior conquistado conserva vencimento. Falha de save mantém estado anterior.

Pendência, inclusive suspensa, bloqueia economia territorial/automáticos até concluir/cancelar; outras atividades podem usar a trava global após suspensão. Não sobrescreve outros contratos anteriores.

## Limpeza e segurança dos veículos

Truck/target/customer têm DECOR/token próprios e veículos reservados como trabalho. Só veículos próprios, vazios, parados abaixo0,5m/s e sem engate externo são retirados. Se o par próprio ainda está unido, desacopla somente quando ambos estão vazios/parados. Nunca ejeta jogador, apaga carro ocupado/ambiente ou desfaz ligação com guincho de terceiros.

Ocupação/movimento/conexão externa adiam a limpeza em fila, drenada mesmo inativo. Há limite8 grupos com reserva de espaço para o contrato ativo; novos spawns/ativações aguardam liberação segura. Se vaga ficar bloqueada após suspender dentro do guincho, saia e aguarde parar/liberar antes de retomar. Encerramento do script só marca para liberação natural os objetos ainda protegidos/ocupados e solta reservas, sem remoção forçada.

Cliente próprio é liberado naturalmente, sem apagar NPCs ambiente. Não usa clear-area, teleporte, imunidade policial ou encerramento de threads GTA.

## Checklist no GTA

Teste vaga/chegada aos chamados/cliente no passeio, engate real e geometria/NUM8-NUM2, retorno físico, E de descarga/assentamento e ambas as avaliações. Teste soltura acidental no caminho e no pátio, alvo ocupado, outros carros/guinchos próximos, END dentro/fora do guincho e liberação após sair, reload parcial/pronto, cancelamento sem posse, troca de perfil e bônus. Verifique H/serviços nativos de guincho, pátio com props instalados e convivência com café/tatuagem/oficina/auto armas/manual. **Esses testes não foram executados no GTA nesta nuvem.**

## Dados, referência e diferenças

Novos **GangTowData.cs**, **GangTow.cs**, **Gangs/TowRecovery.json**. Wallet.GangTow opcional guarda Pending Id/Gang/Area3/Options/Dispatches(Model/Customer/Site)/Damage por entrega; contas Completed/StartedUtcTicks/ExpiresUtcTicks por gangue/área3, até640 contas/256 recibos/campos futuros. Sem handles, hook ou relógio salvo; legado não ganha livro/bônus ao consultar.

Begin/Deliver/Cancel/Complete clonam um livro e salvam numa Wallet. Índice/recibo impedem repetição; só final remove pendência+premia numa gravação, rollback da referência em falha. Qualidade é perda finita0…800. Trava territorial compartilhada/Bulk/Arms/OtherFree da oficina e divulgação incluem pendência/invalidez sem recursão. Bônus integrado por máximo ao relógio automático, com85%/15% intactos.

Config: Goal2(1…10), BoostMinutes60(1…180), ReturnRadius8(3…15), TruckModel(towtruck/towtruck2), TruckSpawn/Yard até30m entre si em plano; Models126(1…160 únicos), Customers24(1…40 únicos), Sites15(1…30 distintos). Defaults usam dados exatos da referência. Opções inválidas usam defaults em memória e conservam original; mudanças só afetam próxima contratação/snapshots anteriores mantidos. Modelos/locais sorteados uma vez, sem rerrolar após reload.

Análise de PSRP/GangSpawner.cs confirma área3,15 pontos/rumos,126 carros,24 clientes, posições do guincho/pátio, braço/engate/descarga nativos, distâncias3/12/16, dano200/800, notas75/200 e2X/UTC1h. **A lógica eHUD da PSRP encerram em2 recuperações; a notificação final diz3 por inconsistência. RPCore segue2.** Código próprio; PSRP.dll não executada.

Diferenças: E descarrega em vez deH; não encerra scripts nativos; prova de descarga comandada/par vazio assentado em vez de qualquer soltura acidental no pátio; finalE a pé salvo e retomada, limpeza sem forçar saída/deletar ocupados; HUD nativo simplificado/sem MissionWin cruzando arquivos. Demais trabalhos territoriais/diálogo/guerras/polícia/refinamentos e fases8(interface/necessidades)/9(realismo dos trabalhos) permanecem futuros.

**2388 testes aprovados**:2348 anteriores +40 de guincho/dados/regras/braço/integração. Build Release net48 zero erros/avisos. Auditoria e relatórios acompanham o pacote.
